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
    private const float MAX_SHORT_TERM_DEBT = 86400;
    private const float WORK_FETCH_PERIOD = 60;
    private const float MAX_WORK_FETCH_MULTIPLICATOR = 86400.0f / WORK_FETCH_PERIOD;
    private const float CREDITS_CPU_S = 0.002315f;
    private float _sumPriority = 0;
    private float _totalShortfall;
    private bool _isOnline = true;
    private readonly List<(ClientTaskData Task, ClientProject Project)> _deadlineMissedResults = new List<(ClientTaskData, ClientProject)>();
    private Coroutine _executorCoroutine;
    
    private HostState _currentState;
    private int _lastStateChangeTick;

    private float _workFetchMultiplicator = 1.0f;
    private int _schedulerWakeVersion = 0;
    private int _workFetchWakeVersion = 0;
    private int _executorWakeVersion = 0;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;
    private IStatService StatService => Container.Instance.StatService;

    public ClientModel(GroupConfig config, ProjectConfig[] projectConfigs, int actorId, HostModel host) : base($"client{actorId}", host)
    {
        _config = config;

        Container.Instance.StatService.RegisterClient(this);

        foreach (var projConfig in projectConfigs)
        {
            var clientProject = new ClientProject(projConfig);
            _projects.Add(clientProject.Name, clientProject);
            _sumPriority += clientProject.Priority;
        }

        _currentState = HostState.Suspended;
        _lastStateChangeTick = TimeSystem.CurTick;
    }

    private void SetState(HostState newState)
    {
        if (_currentState == newState)
        {
            return;
        }

        int currentTick = TimeSystem.CurTick;
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
        int joinDelay = (int)RandomUtils.GetDistribution(
            Distribution.Uniform,
            0,
            Mathf.Min(24 * 3600, SimManager.MaxSimulationTime - TimeSystem.CurTick)
        );
        yield return new WaitForTicks(joinDelay);

        _executorCoroutine = SimManager.StartSimulationCoroutine(ExecutorLoop());
        SimManager.StartSimulationCoroutine(AvailabilityLoop());
        SimManager.StartSimulationCoroutine(SchedulerLoop());
        SimManager.StartSimulationCoroutine(WorkFetchLoop());
        yield break;
    }

    private IEnumerator AvailabilityLoop()
    {
        while (true)
        {
            // GOING ONLINE
            _isOnline = true;
            SetState(HostState.Idle);
            WakeScheduler();
            WakeWorkFetch();
            
            if (_executorCoroutine == null)
            {
                _executorCoroutine = SimManager.StartSimulationCoroutine(ExecutorLoop());
            }
            RunScheduler();
            var onlineDuration = Mathf.Max((int)(RandomUtils.GetDistribution(
                                       _config.RandomConfig.HostAvailabilityDistri,
                                       _config.RandomConfig.HostAvailabilityA,
                                       _config.RandomConfig.HostAvailabilityB
                                   ) * 3600), 0);
            int onlineRemainingTicks = Mathf.Max(SimManager.MaxSimulationTime - TimeSystem.CurTick, 0);
            int onlineRecordedDuration = Mathf.Min(onlineDuration, onlineRemainingTicks);
            StatService.RecordAvailability(onlineRecordedDuration / 3600f, TimeSystem.CurTick, onlineRecordedDuration);
            yield return new WaitForTicks(onlineDuration);

            var offlineDuration = Mathf.Max((int)(RandomUtils.GetDistribution(
                                        _config.RandomConfig.HostNonavailabilityDistri,
                                        _config.RandomConfig.HostNonavailabilityA,
                                        _config.RandomConfig.HostNonavailabilityB
                                    ) * 3600), 0);
            int offlineRemainingTicks = Mathf.Max(SimManager.MaxSimulationTime - TimeSystem.CurTick, 0);
            int offlineRecordedDuration = Mathf.Min(offlineDuration, offlineRemainingTicks);
            bool shouldUpdateGridState = TimeSystem.CurTick + offlineDuration <= SimManager.MaxSimulationTime;

            // GOING OFFLINE
            _isOnline = false;
            if (shouldUpdateGridState)
            {
                SetState(HostState.Suspended);
            }
            if (_executorCoroutine != null)
            {
                SimManager.StopSimulationCoroutine(_executorCoroutine);
                _executorCoroutine = null;

                foreach (var proj in _projects.Values)
                {
                    if (proj.RunningTask != null)
                    {
                        var orphanedTask = proj.RunningTask;
                        UpdateTaskProgress(orphanedTask, proj);
                        orphanedTask.Running = false;
                        orphanedTask.Scheduled = false;
                        proj.RunningTask = null;
                        proj.RunList.Remove(orphanedTask);
                        // Return result to the available queue to be rescheduled.
                        proj.Tasks.Add(orphanedTask);
                    }
                }
            }
            StatService.RecordUnavailability(offlineRecordedDuration / 3600f);

            yield return new WaitForTicks(offlineRecordedDuration);
        }
    }
    
    private IEnumerator SchedulerLoop()
    {
        while (true)
        {
            if (!_isOnline)
            {
                yield return new WaitWhile(() => !_isOnline);
            }

            RunScheduler();
            
            yield return WaitForSchedulerWakeOrTimeout(_config.SchedulingInterval);
            if (!_isOnline)
            {
                continue;
            }
        }
    }

    private IEnumerator WorkFetchLoop()
    {
        yield return new WaitForTicks((int)RandomUtils.GetDistribution(Distribution.Uniform, 0, 3600));

        while (true)
        {
            if (!_isOnline)
            {
                yield return new WaitWhile(() => !_isOnline);
            }

            if (!_isOnline)
            {
                continue;
            }

            if (TimeSystem.CurTick >= SimManager.MaxSimulationTime - WORK_FETCH_PERIOD)
            {
                yield break;
            }
            
            UpdateInProgressResults();
            UpdateShortfall();

            ClientProject selectedProj = null;
            float maxControl = float.MinValue;

            foreach (var proj in _projects.Values)
            {
                float control = proj.LongTermDebt + proj.Shortfall;
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

                if (_deadlineMissedResults.Count == 0 && workPercentage > 0)
                {
                    if (!selectedProj.On && selectedProj.CompletedTasks.Count == 0)
                    {
                        yield return new WaitForTicks((int)Mathf.Ceil((_workFetchMultiplicator - 1.0f) * WORK_FETCH_PERIOD));
                        if (!_isOnline || TimeSystem.CurTick >= SimManager.MaxSimulationTime - WORK_FETCH_PERIOD)
                        {
                            continue;
                        }
                    }

                    yield return AskForWork(selectedProj, (float)workPercentage);
                    yield return WaitForWorkFetchWakeOrTimeout((int)WORK_FETCH_PERIOD);
                    continue;
                }
            }

            yield return WaitForWorkFetchWakeOrTimeout(Mathf.Max(SimManager.MaxSimulationTime - TimeSystem.CurTick, 1));
        }
    }
    
    private IEnumerator ExecutorLoop()
    {
        while (true)
        {
            if (!_isOnline)
            {
                yield return new WaitWhile(() => !_isOnline);
            }

            var taskToExecute = DequeueReadyTask();
            if (!taskToExecute.HasValue)
            {
                yield return WaitForExecutorWakeOrTimeout(Mathf.Max(SimManager.MaxSimulationTime - TimeSystem.CurTick, 1));
                continue;
            }

            var (task, project) = taskToExecute.Value;
            project.RunningTask = task;
            SetState(HostState.Busy);
            task.ExecutionStartTick = TimeSystem.CurTick;
            task.Running = true;
            WakeWorkFetch();
            yield return new Activity(task.RemainingDurationInGflops, this.Host);
            UpdateTaskProgress(task, project);
            task.RemainingDurationInGflops = 0;
            task.Running = false;
            task.Scheduled = false;
            SetState(HostState.Idle);
            
            project.RunningTask = null;
            project.RunList.Remove(task);

            var applicationConfig = project.Config.ApplicationConfigs[task.ApplicationIndex];
            var status = ResultStatus.Fail;
            var value = ResultValue.Incorrect;
            if (RandomUtils.GetDistribution(Distribution.Uniform, 0, 100) < applicationConfig.SuccessPercentage)
            {
                status = ResultStatus.Success;
                if (RandomUtils.GetDistribution(Distribution.Uniform, 0, 100) < applicationConfig.CanonicalPercentage)
                {
                    value = ResultValue.Correct;
                }
            }
            
            var reply = new ClientReplyData(
                this.ActorName,
                status,
                value, 
                task.WorkunitName,
                task.ServerResultNumber,
                (int)(task.DurationInGflops * CREDITS_CPU_S),
                task.OutputFileSizeMb
            );
            project.CompletedTasks.Enqueue(reply);
            WakeScheduler();
            WakeWorkFetch();
        }
    }

    private IEnumerator AskForWork(ClientProject proj, float workPercentage)
    {
        while (proj.CompletedTasks.Count > 0)
        {
            var reply = proj.CompletedTasks.Dequeue();
            var replyComm = Push(proj.ProjectActorName, reply);
            if (!replyComm.IsDone)
            {
                yield return replyComm;
            }
        }

        var request = new ClientRequestData(this.ActorName, Host.HostPower, workPercentage);
        var requestComm = Push(proj.ProjectActorName, request);
        if (!requestComm.IsDone)
        {
            yield return requestComm;
        }
        
        ServerReplyData message = null;
        // Wait until we get a reply of the correct type.
        // This is safer than a fixed-time loop, as other messages might arrive.
        while (true) 
        {
            message = Receive<ServerReplyData>();
            if (message != null)
            {
                break;
            }
            yield return new WaitForTicks(1);
        }

        if (message != null)
        {
            if (message.tasks.Any())
            {
                proj.On = true;
                _workFetchMultiplicator = 1.0f;
                
                foreach (var task in message.tasks)
                {
                    task.Project = proj;
                    proj.Tasks.Add(task);
                }
                proj.TotalTasksReceived += message.tasks.Count;
                WakeScheduler();
            }
            else
            {
                proj.On = false;
                _workFetchMultiplicator = Mathf.Min(
                    MAX_WORK_FETCH_MULTIPLICATOR,
                    _workFetchMultiplicator * RandomUtils.GetDistribution(Distribution.Uniform, 1.9f, 2.1f)
                );
            }
            WakeScheduler();
        }
    }

    private void WakeScheduler()
    {
        _schedulerWakeVersion++;
    }

    private void WakeWorkFetch()
    {
        _workFetchWakeVersion++;
    }

    private void WakeExecutor()
    {
        _executorWakeVersion++;
    }

    private IEnumerator WaitForSchedulerWakeOrTimeout(int ticksToWait)
    {
        int wakeVersion = _schedulerWakeVersion;
        int targetTick = TimeSystem.CurTick + ticksToWait;
        yield return new WaitWhile(() => _isOnline && _schedulerWakeVersion == wakeVersion && TimeSystem.CurTick < targetTick);
    }

    private IEnumerator WaitForWorkFetchWakeOrTimeout(int ticksToWait)
    {
        int wakeVersion = _workFetchWakeVersion;
        int targetTick = TimeSystem.CurTick + ticksToWait;
        yield return new WaitWhile(() => _isOnline && _workFetchWakeVersion == wakeVersion && TimeSystem.CurTick < targetTick);
    }

    private IEnumerator WaitForExecutorWakeOrTimeout(int ticksToWait)
    {
        int wakeVersion = _executorWakeVersion;
        int targetTick = TimeSystem.CurTick + ticksToWait;
        yield return new WaitWhile(() => _isOnline && _executorWakeVersion == wakeVersion && TimeSystem.CurTick < targetTick);
    }

    private void RunScheduler()
    {
        if (!_isOnline)
        {
            return;
        }

        UpdateInProgressResults();
        UpdateDebt();
        UpdateDeadlineMissedResults();
        
        var resultToRun = SelectResultToRun();
        if (resultToRun != null)
        {
            ScheduleTask(resultToRun.Value.Task, resultToRun.Value.Project);
        }
    }

    private void UpdateInProgressResults()
    {
        foreach (var proj in _projects.Values)
        {
            foreach (var task in proj.RunList)
            {
                UpdateTaskProgress(task, proj);
            }
        }
    }

    private void ScheduleTask(ClientTaskData task, ClientProject project)
    {
        task.Project = project;
        if (!project.RunList.Contains(task))
        {
            project.RunList.Add(task);
        }

        if (!task.Scheduled)
        {
            project.ReadyTasks.Enqueue(task);
            task.Scheduled = true;
        }

        WakeExecutor();
    }

    private (ClientTaskData Task, ClientProject Project)? DequeueReadyTask()
    {
        foreach (var proj in _projects.Values)
        {
            if (proj.ReadyTasks.Count > 0)
            {
                return (proj.ReadyTasks.Dequeue(), proj);
            }
        }

        return null;
    }

    private void UpdateTaskProgress(ClientTaskData task, ClientProject project)
    {
        if (!task.Running)
        {
            return;
        }

        int elapsedTicks = TimeSystem.CurTick - task.ExecutionStartTick;
        if (elapsedTicks <= 0)
        {
            return;
        }

        float executedGflops = elapsedTicks * Host.HostPower;
        task.RemainingDurationInGflops -= executedGflops;
        if (task.RemainingDurationInGflops < 0)
        {
            task.RemainingDurationInGflops = 0;
        }

        project.WallCpuTime += elapsedTicks;
        task.ExecutionStartTick = TimeSystem.CurTick;
    }

    private void UpdateDebt()
    {
        float totalWallCpuTime = _projects.Values.Sum(p => p.WallCpuTime);
        float runnablePrioritySum = _projects.Values
            .Where(p => p.HasRunnableResults())
            .Sum(p => p.Priority);
        int runnableProjects = _projects.Values.Count(p => p.HasRunnableResults());
        float totalShortTermDebt = 0;

        foreach (var proj in _projects.Values)
        {
            float w = totalWallCpuTime * (proj.Priority / _sumPriority);
            float w_short = (runnablePrioritySum > 0) ? (totalWallCpuTime * (proj.Priority / runnablePrioritySum)) : 0;

            proj.LongTermDebt += w - proj.WallCpuTime;
            
            if (runnablePrioritySum > 0)
            {
                proj.ShortTermDebt += w_short - proj.WallCpuTime;
            }
            else
            {
                proj.ShortTermDebt = 0;
            }

            if (!proj.HasRunnableResults())
            {
                proj.ShortTermDebt = 0;
            }
            
            totalShortTermDebt += proj.ShortTermDebt;
            proj.WallCpuTime = 0;
        }

        if (runnableProjects == 0)
        {
            return;
        }

        float debtShift = totalShortTermDebt / runnableProjects;
        foreach (var proj in _projects.Values)
        {
            if (!proj.HasRunnableResults())
            {
                continue;
            }

            proj.ShortTermDebt -= debtShift;
            proj.ShortTermDebt = Mathf.Clamp(proj.ShortTermDebt, -MAX_SHORT_TERM_DEBT, MAX_SHORT_TERM_DEBT);
        }
    }

    private void UpdateShortfall()
    {
        _totalShortfall = 0;
        float totalWorkDuration = 0;

        foreach (var proj in _projects.Values)
        {
            float projectWorkDuration = 0;
            projectWorkDuration += proj.Tasks.Sum(t => t.GetRemainingDuration(Host.HostPower));
            projectWorkDuration += proj.RunList.Sum(t => t.GetRemainingDuration(Host.HostPower));

            proj.Shortfall = _config.ConnectionInterval * (proj.Priority / _sumPriority) - projectWorkDuration;
            if (proj.Shortfall < 0) proj.Shortfall = 0;
            
            totalWorkDuration += projectWorkDuration;
        }
        
        _totalShortfall = _config.ConnectionInterval - totalWorkDuration;
        if (_totalShortfall < 0) _totalShortfall = 0;
    }
    
    private void UpdateDeadlineMissedResults()
    {
        _deadlineMissedResults.Clear();

        var allResults = new List<(ClientTaskData, ClientProject)>();
        foreach (var proj in _projects.Values)
        {
            allResults.AddRange(proj.Tasks.Select(t => (t, proj)));
            allResults.AddRange(proj.RunList.Select(t => (t, proj)));
        }

        if (!allResults.Any()) return;

        var simResults = allResults.Select(t => new SimResult
        {
            Result = t,
            RemainingDuration = t.Item1.GetRemainingDuration(Host.HostPower)
        }).ToList();

        float clockSim = TimeSystem.CurTick;
        
        while (simResults.Any())
        {
            float sumPriority = _projects.Values.Where(proj => simResults.Any(t => t.Result.Item2 == proj)).Sum(p => p.Priority);
            (ClientTaskData Task, ClientProject Project) minResult = (null, null);
            float minFinishTime = float.MaxValue;

            foreach (var simResult in simResults)
            {
                var proj = simResult.Result.Item2;
                var resultsInProj = simResults.Count(t => t.Result.Item2 == proj);
                var finishTime = clockSim + (simResult.RemainingDuration / (proj.Priority / sumPriority)) * resultsInProj;

                if (finishTime < minFinishTime)
                {
                    minFinishTime = finishTime;
                    minResult = simResult.Result;
                }
            }

            if (minResult.Item1 != null && minFinishTime > minResult.Item1.DeadlineTick)
            {
                if(!_deadlineMissedResults.Contains(minResult))
                    _deadlineMissedResults.Add(minResult);
            }
            
            var durationToSubtract = minFinishTime - clockSim;
            simResults.ForEach(t =>
            {
                var proj = t.Result.Item2;
                var resultsInProj = simResults.Count(p => p.Result.Item2 == proj);
                t.RemainingDuration -= durationToSubtract * (proj.Priority / sumPriority) / resultsInProj;
            });
            
            simResults.RemoveAll(t => t.Result.Item1 == minResult.Item1);
            clockSim = minFinishTime;
        }
    }
    
    private (ClientTaskData Task, ClientProject Project)? SelectResultToRun()
    {
        if (_projects.Values.Any(p => p.RunningTask != null))
        {
            return null;
        }

        // EDF scheduler for results that might miss their deadline
        if (_deadlineMissedResults.Any())
        {
            var resultToRun = _deadlineMissedResults.OrderBy(t => t.Item1.DeadlineTick).First();

            if (resultToRun.Item1.Running || resultToRun.Item1.Scheduled)
            {
                _deadlineMissedResults.Remove(resultToRun);
                return null;
            }
            
            // Rebuild the queue without the selected result
            resultToRun.Item2.Tasks.Remove(resultToRun.Item1);
            resultToRun.Item2.RunList.Remove(resultToRun.Item1);
            _deadlineMissedResults.Remove(resultToRun);

            // Check if it will miss the deadline for sure
            var remainingTime = resultToRun.Item1.GetRemainingDuration(Host.HostPower);
            if (TimeSystem.CurTick + remainingTime > resultToRun.Item1.DeadlineTick)
            {
                // Result will be missed, discard it.
                resultToRun.Item2.TotalTasksMissed++;
                return null; 
            }
            
            return resultToRun;
        }

        // Highest-debt-first scheduler
        ClientProject bestProj = null;
        float maxDebt = float.MinValue;

        foreach (var proj in _projects.Values)
        {
            if (proj.Tasks.Count > 0 && proj.ShortTermDebt > maxDebt)
            {
                maxDebt = proj.ShortTermDebt;
                bestProj = proj;
            }
        }

        if (bestProj != null)
        {
            var task = bestProj.Tasks[0];
            bestProj.Tasks.RemoveAt(0);
            return (task, bestProj);
        }
        return null;
    }

    private class SimResult
    {
        public (ClientTaskData Task, ClientProject Project) Result;
        public float RemainingDuration;
    }
}
