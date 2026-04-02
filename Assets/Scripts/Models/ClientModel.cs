using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public enum HostState
{
    Idle,
    Busy,
    Suspended
}

public class ClientModel : BaseActor, IClientStats
{
    public event Action<string, float> OnGoingOffline;
    public event Action<string, float> OnGoingOnline;
    public event Action<string, float> OnBusyMode;
    public event Action<string, float> OnIdleMode;

    private readonly GroupConfig _config;
    private readonly Dictionary<string, ClientProject> _projects = new Dictionary<string, ClientProject>();
    private double _sumPriority = 0;
    private double _totalShortfall;
    private double _lastWallTick;
    private bool _isOnline = true;
    private readonly List<(WorkunitData Workunit, ClientProject Project)> _deadlineMissedTasks = new List<(WorkunitData, ClientProject)>();
    private Coroutine _executorCoroutine;
    
    private HostState _currentState;
    private int _lastStateChangeTick;

    private readonly float _baseConnectionInterval;
    private float _currentConnectionInterval;
    private const float MAX_CONNECTION_INTERVAL = 86400;

    public ClientModel(GroupConfig config, ProjectConfig[] projectConfigs, int actorId, HostModel host) : base($"client{actorId}", host)
    {
        _config = config;
        _baseConnectionInterval = _config.ConnectionInterval;
        _currentConnectionInterval = _baseConnectionInterval;

        Container.Instance.StatService.RegisterClient(this);

        foreach (var projConfig in projectConfigs)
        {
            var clientProject = new ClientProject(projConfig);
            _projects.Add(clientProject.Name, clientProject);
            _sumPriority += clientProject.Priority;
        }

        _currentState = HostState.Suspended;
        _lastStateChangeTick = (int)TimeTickSystem.Instance.CurTick;
    }

    private void SetState(HostState newState)
    {
        if (_currentState == newState)
        {
            return;
        }

        int currentTick = (int)TimeTickSystem.Instance.CurTick;
        int duration = currentTick - _lastStateChangeTick;

        if (duration < 0)
        {
            return;
        }

        switch (_currentState)
        {
            case HostState.Idle:
                if (newState == HostState.Busy)
                {
                    OnBusyMode?.Invoke(this.ActorName, Host.HostPower);
                }
                else if (newState == HostState.Suspended)
                {
                    OnBusyMode?.Invoke(this.ActorName, Host.HostPower);
                    OnGoingOffline?.Invoke(this.ActorName, Host.HostPower);
                }
                break;
            case HostState.Busy:
                if (newState == HostState.Idle)
                {
                    OnIdleMode?.Invoke(this.ActorName, Host.HostPower);
                }
                else if (newState == HostState.Suspended)
                {
                    OnGoingOffline?.Invoke(this.ActorName, Host.HostPower);
                }
                break;
            case HostState.Suspended:
                if (newState == HostState.Idle)
                {
                    OnGoingOnline?.Invoke(this.ActorName, Host.HostPower);
                    OnIdleMode?.Invoke(this.ActorName, Host.HostPower);
                }
                else if (newState == HostState.Busy)
                {
                    OnGoingOnline?.Invoke(this.ActorName, Host.HostPower);
                }
                break;
        }

        _currentState = newState;
        _lastStateChangeTick = currentTick;
    }

    public override IEnumerator MainLoop()
    {
        yield return new WaitForTicks((int)RandomUtils.GetDistribution(Distribution.Uniform, 0, 3600));

        SimulationManager.Instance.StartCoroutine(AvailabilityLoop());
        SimulationManager.Instance.StartCoroutine(SchedulerLoop());
        SimulationManager.Instance.StartCoroutine(WorkFetchLoop());
        _executorCoroutine = SimulationManager.Instance.StartCoroutine(ExecutorLoop());
        yield break;
    }

    private IEnumerator AvailabilityLoop()
    {
        while (true)
        {
            // GOING ONLINE
            _isOnline = true;
            SetState(HostState.Idle);
            
            if (_executorCoroutine == null)
            {
                _executorCoroutine = SimulationManager.Instance.StartCoroutine(ExecutorLoop());
            }
            var onlineDuration = (int)(RandomUtils.GetDistribution(
                                       _config.RandomConfig.HostAvailabilityDistri,
                                       _config.RandomConfig.HostAvailabilityA,
                                       _config.RandomConfig.HostAvailabilityB
                                   ) * 3600);
            yield return new WaitForTicks(onlineDuration);

            // GOING OFFLINE
            _isOnline = false;
            SetState(HostState.Suspended);
            if (_executorCoroutine != null)
            {
                SimulationManager.Instance.StopCoroutine(_executorCoroutine);
                _executorCoroutine = null;

                foreach (var proj in _projects.Values)
                {
                    if (proj.InProgressTasks.Any())
                    {
                        var orphanedTasks = new List<WorkunitData>(proj.InProgressTasks);
                        proj.InProgressTasks.Clear();
                        foreach (var task in orphanedTasks)
                        {
                            // Return task to the available queue to be rescheduled from scratch.
                            proj.AvailableTasks.Enqueue(task);
                        }
                    }
                }
            }
            var offlineDuration = (int)(RandomUtils.GetDistribution(
                                        _config.RandomConfig.HostNonavailabilityDistri,
                                        _config.RandomConfig.HostNonavailabilityA,
                                        _config.RandomConfig.HostNonavailabilityB
                                    ) * 3600);

            yield return new WaitForTicks(offlineDuration);
        }
    }
    
    private IEnumerator SchedulerLoop()
    {
        _lastWallTick = TimeTickSystem.Instance.CurTick;
        while (true)
        {
            if (!_isOnline)
            {
                yield return new WaitUntil(() => _isOnline);
            }
            
            yield return new WaitForTicks(_config.SchedulingInterval);
            UpdateDebt();
            UpdateDeadlineMissedTasks();
            
            var taskToRun = SelectTaskToRun();
            if (taskToRun != null)
            {
                Debug.Log($"[{ActorName}] Scheduler selected task {taskToRun.Value.Workunit.ParentTaskName}/{taskToRun.Value.Workunit.workunitId} to run.");
                taskToRun.Value.Project.ReadyToExecuteTasks.Enqueue(taskToRun.Value.Workunit);
            }
        }
    }

    private IEnumerator WorkFetchLoop()
    {
        while (true)
        {
            if (!_isOnline)
            {
                yield return new WaitUntil(() => _isOnline);
            }
            
            yield return new WaitForTicks((int)_currentConnectionInterval);
            
            UpdateShortfall();

            ClientProject selectedProj = null;
            double maxControl = double.MinValue;

            foreach (var proj in _projects.Values)
            {
                double control = proj.LongTermDebt + proj.Shortfall;
                if (control > maxControl)
                {
                    maxControl = control;
                    selectedProj = proj;
                }
            }

            if (selectedProj != null && selectedProj.Shortfall > 0)
            {
                var workPercentage = selectedProj.Shortfall > _totalShortfall / _sumPriority
                    ? selectedProj.Shortfall
                    : _totalShortfall / _sumPriority;

                Debug.Log($"[{ActorName}] Selected project '{selectedProj.Name}' to fetch work. Shortfall: {selectedProj.Shortfall}, WorkPercentage: {workPercentage}, DeadlineMissed: {_deadlineMissedTasks.Count}");

                if (_deadlineMissedTasks.Count == 0 && workPercentage > 0)
                {
                    yield return AskForWork(selectedProj, (float)workPercentage);
                }
            }
        }
    }
    
    private IEnumerator ExecutorLoop()
    {
        while (true)
        {
            (WorkunitData workunit, ClientProject project)? taskToExecute = null;

            foreach (var proj in _projects.Values)
            {
                if (proj.ReadyToExecuteTasks.Count > 0)
                {
                    taskToExecute = (proj.ReadyToExecuteTasks.Dequeue(), proj);
                    break;
                }
            }

            if (taskToExecute.HasValue)
            {
                var (workunit, project) = taskToExecute.Value;
                
                Debug.Log($"[{ActorName}] Starting execution of workunit {workunit.ParentTaskName}/{workunit.workunitId}.");
                project.InProgressTasks.Add(workunit);
                SetState(HostState.Busy);
                yield return new Activity(workunit.durationInFlops, this.Host);
                SetState(HostState.Idle);
                
                project.InProgressTasks.Remove(workunit);

                var wallTime = TimeTickSystem.Instance.CurTick - _lastWallTick;
                project.WallCpuTime += wallTime;
                _lastWallTick = TimeTickSystem.Instance.CurTick;

                var status = WorkunitStatus.Fail;
                var result = WorkunitResult.Incorrect;
                if (UnityEngine.Random.Range(0, 100) < project.Config.SuccessPercentage)
                {
                    status = WorkunitStatus.Success;
                    if (UnityEngine.Random.Range(0, 100) < project.Config.CanonicalPercentage)
                    {
                        result = WorkunitResult.Correct;
                    }
                }
                
                var reply = new ClientReplyData(
                    this.ActorName,
                    status,
                    result, 
                    workunit.ParentTaskName,
                    workunit.workunitId,
                    (int)(workunit.durationInFlops * 0.0001f),
                    workunit.outputByteSize
                );
                Debug.Log($"[{ActorName}] Finished execution of {workunit.ParentTaskName}/{workunit.workunitId}. Status: {status}, Result: {result}. Enqueuing reply.");
                project.CompletedTasks.Enqueue(reply);
            }
            else
            {
                yield return new WaitForTicks(1);
            }
        }
    }

    private IEnumerator AskForWork(ClientProject proj, float workPercentage)
    {
        if (proj.CompletedTasks.Count > 0)
        {
            Debug.Log($"[{ActorName}] Sending {proj.CompletedTasks.Count} completed tasks to {proj.ProjectActorName}.");
        }
        while (proj.CompletedTasks.Count > 0)
        {
            var reply = proj.CompletedTasks.Dequeue();
            yield return Push(proj.ProjectActorName, reply);
        }

        Debug.Log($"[{ActorName}] Asking {proj.ProjectActorName} for work.");
        var request = new ClientRequestData(this.ActorName, (int)Host.HostPower, workPercentage);
        yield return Push(proj.ProjectActorName, request);
        
        IMessage message = null;
        // Wait until we get a reply of the correct type.
        // This is safer than a fixed-time loop, as other messages might arrive.
        while (true) 
        {
            message = Receive();
            if (message is ServerReplyData)
            {
                break;
            }
            yield return new WaitForTicks(1);
        }

        if (message is ServerReplyData serverReply)
        {
            Debug.Log($"[{ActorName}] Received {serverReply.workunits.Count} new workunits from {proj.ProjectActorName}.");
            if (serverReply.workunits.Any())
            {
                _currentConnectionInterval /= 2;
                if (_currentConnectionInterval < _baseConnectionInterval)
                {
                    _currentConnectionInterval = _baseConnectionInterval;
                }
                
                foreach (var workunit in serverReply.workunits)
                {
                    proj.AvailableTasks.Enqueue(workunit);
                }
            }
            else
            {
                _currentConnectionInterval *= 2;
                if (_currentConnectionInterval > MAX_CONNECTION_INTERVAL)
                {
                    _currentConnectionInterval = MAX_CONNECTION_INTERVAL;
                }
            }
        }
    }

    private void UpdateDebt()
    {
        double totalWallCpuTime = _projects.Values.Sum(p => p.WallCpuTime);
        double runnablePrioritySum = _projects.Values
            .Where(p => p.AvailableTasks.Any() || p.InProgressTasks.Any())
            .Sum(p => p.Priority);

        foreach (var proj in _projects.Values)
        {
            double w = totalWallCpuTime * (proj.Priority / _sumPriority);
            double w_short = (runnablePrioritySum > 0) ? (totalWallCpuTime * (proj.Priority / runnablePrioritySum)) : 0;

            proj.LongTermDebt += w - proj.WallCpuTime;
            
            if (runnablePrioritySum > 0)
                proj.ShortTermDebt += w_short - proj.WallCpuTime;
            else
                proj.ShortTermDebt = 0;
            
            proj.WallCpuTime = 0;
        }
    }

    private void UpdateShortfall()
    {
        _totalShortfall = 0;
        double totalWorkDuration = 0;

        foreach (var proj in _projects.Values)
        {
            double projectWorkDuration = 0;
            projectWorkDuration += proj.AvailableTasks.Sum(t => t.durationInFlops / Host.HostPower);
            projectWorkDuration += proj.InProgressTasks.Sum(t => (t.durationInFlops / Host.HostPower) - (TimeTickSystem.Instance.CurTick - _lastWallTick));

            proj.Shortfall = _config.ConnectionInterval * (proj.Priority / _sumPriority) - projectWorkDuration;
            if (proj.Shortfall < 0) proj.Shortfall = 0;
            
            totalWorkDuration += projectWorkDuration;
        }
        
        _totalShortfall = _config.ConnectionInterval - totalWorkDuration;
        if (_totalShortfall < 0) _totalShortfall = 0;
    }
    
    private void UpdateDeadlineMissedTasks()
    {
        _deadlineMissedTasks.Clear();

        var allTasks = new List<(WorkunitData, ClientProject)>();
        foreach (var proj in _projects.Values)
        {
            allTasks.AddRange(proj.AvailableTasks.Select(t => (t, proj)));
            allTasks.AddRange(proj.InProgressTasks.Select(t => (t, proj)));
        }

        if (!allTasks.Any()) return;

        var simTasks = allTasks.Select(t => new SimTask
        {
            Task = t,
            RemainingDuration = t.Item1.durationInFlops / Host.HostPower
        }).ToList();

        double clockSim = TimeTickSystem.Instance.CurTick;
        
        while (simTasks.Any())
        {
            double sumPriority = simTasks.Select(t => t.Task.Item2.Priority).Distinct().Sum();
            (WorkunitData Workunit, ClientProject Project) minTask = (null, null);
            double minFinishTime = double.MaxValue;

            foreach (var simTask in simTasks)
            {
                var proj = simTask.Task.Item2;
                var tasksInProj = simTasks.Count(t => t.Task.Item2 == proj);
                var finishTime = clockSim + (simTask.RemainingDuration / (proj.Priority / sumPriority)) * tasksInProj;

                if (finishTime < minFinishTime)
                {
                    minFinishTime = finishTime;
                    minTask = simTask.Task;
                }
            }

            if (minTask.Item1 != null && minFinishTime > minTask.Item1.deadlineTick)
            {
                if(!_deadlineMissedTasks.Contains(minTask))
                    _deadlineMissedTasks.Add(minTask);
            }
            
            var durationToSubtract = minFinishTime - clockSim;
            simTasks.ForEach(t =>
            {
                var proj = t.Task.Item2;
                var tasksInProj = simTasks.Count(p => p.Task.Item2 == proj);
                t.RemainingDuration -= durationToSubtract * (proj.Priority / sumPriority) / tasksInProj;
            });
            
            simTasks.RemoveAll(t => t.Task.Item1 == minTask.Item1);
            clockSim = minFinishTime;
        }
    }
    
    private (WorkunitData Workunit, ClientProject Project)? SelectTaskToRun()
    {
        // EDF scheduler for tasks that might miss their deadline
        if (_deadlineMissedTasks.Any())
        {
            var taskToRun = _deadlineMissedTasks.OrderBy(t => t.Item1.deadlineTick).First();
            
            // Rebuild the queue without the selected task
            var newQueue = new Queue<WorkunitData>(taskToRun.Item2.AvailableTasks.Where(t => t.workunitId != taskToRun.Item1.workunitId));
            taskToRun.Item2.AvailableTasks = newQueue;
            _deadlineMissedTasks.Remove(taskToRun);

            // Check if it will miss the deadline for sure
            var remainingTime = taskToRun.Item1.durationInFlops / Host.HostPower;
            if (TimeTickSystem.Instance.CurTick + remainingTime > taskToRun.Item1.deadlineTick)
            {
                // Task will be missed, discard it.
                return null; 
            }
            
            return taskToRun;
        }

        // Highest-debt-first scheduler
        ClientProject bestProj = null;
        double maxDebt = double.MinValue;

        foreach (var proj in _projects.Values)
        {
            if (proj.AvailableTasks.Count > 0 && proj.ShortTermDebt > maxDebt)
            {
                maxDebt = proj.ShortTermDebt;
                bestProj = proj;
            }
        }

        if (bestProj != null)
        {
            return (bestProj.AvailableTasks.Dequeue(), bestProj);
        }
        return null;
    }

    private class SimTask
    {
        public (WorkunitData Workunit, ClientProject Project) Task;
        public double RemainingDuration;
    }
}