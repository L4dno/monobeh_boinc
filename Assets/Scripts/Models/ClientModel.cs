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
    private readonly List<(ResultData Result, ClientProject Project)> _deadlineMissedResults = new List<(ResultData, ClientProject)>();
    private Coroutine _executorCoroutine;
    
    private HostState _currentState;
    private int _lastStateChangeTick;

    private readonly float _baseConnectionInterval;
    private float _currentConnectionInterval;
    private const float MAX_CONNECTION_INTERVAL = 86400;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

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
        yield return new WaitForTicks((int)RandomUtils.GetDistribution(Distribution.Uniform, 0, 3600));

        SimManager.StartCoroutine(AvailabilityLoop());
        SimManager.StartCoroutine(SchedulerLoop());
        SimManager.StartCoroutine(WorkFetchLoop());
        _executorCoroutine = SimManager.StartCoroutine(ExecutorLoop());
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
                _executorCoroutine = SimManager.StartCoroutine(ExecutorLoop());
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
                SimManager.StopCoroutine(_executorCoroutine);
                _executorCoroutine = null;

                foreach (var proj in _projects.Values)
                {
                    if (proj.InProgressResults.Any())
                    {
                        var orphanedResults = new List<ResultData>(proj.InProgressResults);
                        proj.InProgressResults.Clear();
                        foreach (var result in orphanedResults)
                        {
                            // Return result to the available queue to be rescheduled from scratch.
                            proj.AvailableResults.Enqueue(result);
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
        _lastWallTick = TimeSystem.CurTick;
        while (true)
        {
            if (!_isOnline)
            {
                yield return new WaitUntil(() => _isOnline);
            }
            
            yield return new WaitForTicks(_config.SchedulingInterval);
            UpdateDebt();
            UpdateDeadlineMissedResults();
            
            var resultToRun = SelectResultToRun();
            if (resultToRun != null)
            {
                Debug.Log($"[{ActorName}] Scheduler selected result {resultToRun.Value.Result.WorkunitName}/{resultToRun.Value.Result.resultNumber} to run.");
                resultToRun.Value.Project.ReadyToExecuteResults.Enqueue(resultToRun.Value.Result);
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

                Debug.Log($"[{ActorName}] Selected project '{selectedProj.Name}' to fetch work. Shortfall: {selectedProj.Shortfall}, WorkPercentage: {workPercentage}, DeadlineMissed: {_deadlineMissedResults.Count}");

                if (_deadlineMissedResults.Count == 0 && workPercentage > 0)
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
            (ResultData result, ClientProject project)? resultToExecute = null;

            foreach (var proj in _projects.Values)
            {
                if (proj.ReadyToExecuteResults.Count > 0)
                {
                    resultToExecute = (proj.ReadyToExecuteResults.Dequeue(), proj);
                    break;
                }
            }

            if (resultToExecute.HasValue)
            {
                var (result, project) = resultToExecute.Value;
                
                Debug.Log($"[{ActorName}] Starting execution of result {result.WorkunitName}/{result.resultNumber}.");
                project.InProgressResults.Add(result);
                SetState(HostState.Busy);
                yield return new Activity(result.durationInFlops, this.Host);
                SetState(HostState.Idle);
                
                project.InProgressResults.Remove(result);

                var wallTime = TimeSystem.CurTick - _lastWallTick;
                project.WallCpuTime += wallTime;
                _lastWallTick = TimeSystem.CurTick;

                var status = ResultStatus.Fail;
                var value = ResultValue.Incorrect;
                if (UnityEngine.Random.Range(0, 100) < project.Config.SuccessPercentage)
                {
                    status = ResultStatus.Success;
                    if (UnityEngine.Random.Range(0, 100) < project.Config.CanonicalPercentage)
                    {
                        value = ResultValue.Correct;
                    }
                }
                
                var reply = new ClientReplyData(
                    this.ActorName,
                    status,
                    value, 
                    result.WorkunitName,
                    result.resultNumber,
                    (int)(result.durationInFlops * 0.0001f),
                    result.outputByteSize
                );
                Debug.Log($"[{ActorName}] Finished execution of {result.WorkunitName}/{result.resultNumber}. Status: {status}, Value: {value}. Enqueuing reply.");
                project.CompletedResults.Enqueue(reply);
            }
            else
            {
                yield return new WaitForTicks(1);
            }
        }
    }

    private IEnumerator AskForWork(ClientProject proj, float workPercentage)
    {
        if (proj.CompletedResults.Count > 0)
        {
            Debug.Log($"[{ActorName}] Sending {proj.CompletedResults.Count} completed results to {proj.ProjectActorName}.");
        }
        while (proj.CompletedResults.Count > 0)
        {
            var reply = proj.CompletedResults.Dequeue();
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
            Debug.Log($"[{ActorName}] Received {serverReply.results.Count} new results from {proj.ProjectActorName}.");
            if (serverReply.results.Any())
            {
                _currentConnectionInterval /= 2;
                if (_currentConnectionInterval < _baseConnectionInterval)
                {
                    _currentConnectionInterval = _baseConnectionInterval;
                }
                
                foreach (var result in serverReply.results)
                {
                    proj.AvailableResults.Enqueue(result);
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
            .Where(p => p.AvailableResults.Any() || p.InProgressResults.Any())
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
            projectWorkDuration += proj.AvailableResults.Sum(t => t.durationInFlops / Host.HostPower);
            projectWorkDuration += proj.InProgressResults.Sum(t => (t.durationInFlops / Host.HostPower) - (TimeSystem.CurTick - _lastWallTick));

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

        var allResults = new List<(ResultData, ClientProject)>();
        foreach (var proj in _projects.Values)
        {
            allResults.AddRange(proj.AvailableResults.Select(t => (t, proj)));
            allResults.AddRange(proj.InProgressResults.Select(t => (t, proj)));
        }

        if (!allResults.Any()) return;

        var simResults = allResults.Select(t => new SimResult
        {
            Result = t,
            RemainingDuration = t.Item1.durationInFlops / Host.HostPower
        }).ToList();

        double clockSim = TimeSystem.CurTick;
        
        while (simResults.Any())
        {
            double sumPriority = simResults.Select(t => t.Result.Item2.Priority).Distinct().Sum();
            (ResultData Result, ClientProject Project) minResult = (null, null);
            double minFinishTime = double.MaxValue;

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

            if (minResult.Item1 != null && minFinishTime > minResult.Item1.deadlineTick)
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
    
    private (ResultData Result, ClientProject Project)? SelectResultToRun()
    {
        // EDF scheduler for results that might miss their deadline
        if (_deadlineMissedResults.Any())
        {
            var resultToRun = _deadlineMissedResults.OrderBy(t => t.Item1.deadlineTick).First();
            
            // Rebuild the queue without the selected result
            var newQueue = new Queue<ResultData>(resultToRun.Item2.AvailableResults.Where(t => t.resultNumber != resultToRun.Item1.resultNumber));
            resultToRun.Item2.AvailableResults = newQueue;
            _deadlineMissedResults.Remove(resultToRun);

            // Check if it will miss the deadline for sure
            var remainingTime = resultToRun.Item1.durationInFlops / Host.HostPower;
            if (TimeSystem.CurTick + remainingTime > resultToRun.Item1.deadlineTick)
            {
                // Result will be missed, discard it.
                return null; 
            }
            
            return resultToRun;
        }

        // Highest-debt-first scheduler
        ClientProject bestProj = null;
        double maxDebt = double.MinValue;

        foreach (var proj in _projects.Values)
        {
            if (proj.AvailableResults.Count > 0 && proj.ShortTermDebt > maxDebt)
            {
                maxDebt = proj.ShortTermDebt;
                bestProj = proj;
            }
        }

        if (bestProj != null)
        {
            return (bestProj.AvailableResults.Dequeue(), bestProj);
        }
        return null;
    }

    private class SimResult
    {
        public (ResultData Result, ClientProject Project) Result;
        public double RemainingDuration;
    }
}
