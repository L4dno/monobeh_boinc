using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;

public class ProjectModel : BaseActor, IProjectStats
{

    public event Action<int> OnWorkunitCreated;

    public event Action<int> OnWorkunitValid;

    public event Action OnWorkunitCompleted;

    private readonly ProjectDatabase _database;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;
    private GroupConfig GroupConfig => Container.Instance.ConfigProvider.SimConfig.GroupConfig;
    private IStatService StatService => Container.Instance.StatService;
    private const int MaxResultBuffer = 100;
    private bool _isTailMode;

    private IScheduler CommonScheduler => Container.Instance.CommonScheduler;
    private IScheduler TailScheduler => Container.Instance.TailScheduler;

    public ProjectModel(ProjectConfig config, int actorId, HostModel host) : base($"project{actorId}", host)
    {
        _database = new ProjectDatabase
        {
            Config = config
        };

        float theoreticalGflopsBudget = GroupConfig.CalculateTheoreticalGflopsBudget(SimManager.MaxSimulationTime);
        InitializeApplications(theoreticalGflopsBudget);
        if (config.GenerationMode == WorkunitGenerationMode.TailBudget)
        {
            StatService.RecordTailBudget(
                _database.TheoreticalGflopsBudget,
                _database.EffectiveGflopsBudget,
                _database.ApplicationTargets
            );
        }

        Container.Instance.StatService.RegisterProject(this);
    }

    public override IEnumerator MainLoop()
    {
        // возможно запоминать запущенные корутины и вызывать у них события?
        SimManager.StartSimulationCoroutine(WorkunitGeneratorLoop());
        SimManager.StartSimulationCoroutine(ValidatorLoop());
        SimManager.StartSimulationCoroutine(AssimilatorLoop());
        SimManager.StartSimulationCoroutine(RequestDispatcherLoop());
        // число зададч - это число реплик недостающих до кворума оставшихся воркюнитов
        // активным хостом считается тот, кто хоть раз за время симуляции посылал сообщение серву
        // может добавлять номера хостов в set?
        var schedule = SimManager.StartSimulationCoroutine(CommonScheduler.Run(_database, Push));
        yield return new WaitUntil(() => _database.ActiveHostIndexes.Count >= _database.NeededQuorumReplicas);
        SimManager.StopSimulationCoroutine(schedule);
        StatService.RecordTailStarted(TimeSystem.CurTick);
        _isTailMode = true;
        TryFinishTailSimulation();
        Debug.Log("switched to tail mode");
        yield return TailScheduler.Run(_database, Push);
    }

    // получает сообщения и помещает в очереди на обработку
    // _database.ClientRequests
    private IEnumerator RequestDispatcherLoop()
    {
        while (true)
        {
            IMessage message;
            while ((message = Receive()) != null)
            {
                if (message is ClientRequestData request)
                {
                    RecordActiveHost(request.RequesterName);
                    _database.ClientRequests.Enqueue(request);
                    _database.ClientRequestAvailableCondition.Signal();
                    StatService.RecordWorkRequestReceived();
                }
                else if (message is ClientReplyData reply)
                {
                    RecordActiveHost(reply.ClientName);
                    if (ShouldDiscardReplyWithoutStats(reply))
                    {
                        continue;
                    }

                    _database.CurrentValidations.Enqueue(reply);
                    _database.ValidationReplyAvailableCondition.Signal();
                    StatService.RecordResultReceived();
                }
            }

            yield return WaitForMessage();
        }
    }

    private void RecordActiveHost(string actorName)
    {
        if (SimManager.Actors.TryGetValue(actorName, out var actor))
        {
            _database.ActiveHostIndexes.Add(actor.Host.HostId);
        }
    }

    private bool ShouldDiscardReplyWithoutStats(ClientReplyData reply)
    {
        var key = GetResultKey(reply.WorkunitName, reply.ResultNumber);
        if (_database.ServerTimedOutResults.Contains(key))
        {
            _database.ServerTimedOutResults.Remove(key);
            return true;
        }

        if (_database.CurrentWorkunits.TryGetValue(reply.WorkunitName, out var workunit))
        {
            var result = workunit.Results.FirstOrDefault(w => w.resultNumber == reply.ResultNumber);
            if (result != null && (result.isServerTimedOut || result.isValidationCompleted))
            {
                return true;
            }
        }

        return false;
    }

    



    private IEnumerator WorkunitGeneratorLoop()
    {
        Debug.Assert(_database.Config.ApplicationConfigs.Any(), $"ProjectConfig '{_database.Config.name}' has no ApplicationConfigs assigned.");

        // TODO: расчитать долю каждого проекта

        // заполнить каждым конфигом массив соответствующего размера
        //InitialWorkunitsPerApp = new List<int>{2000};
        // TODO: сделать соотношение между несколькими проектами
        while (true)
        {
            while (_database.CurrentResults.Count >= MaxResultBuffer)
            {
                if (ProcessExpiredSentResults())
                {
                    continue;
                }

                if (TryGetTicksUntilNextSentResultDeadline(out var ticksUntilNextSentResultDeadline))
                {
                    yield return _database.ResultBufferHasSpaceCondition.TimedWait(ticksUntilNextSentResultDeadline);
                }
                else
                {
                    yield return _database.ResultBufferHasSpaceCondition.Wait();
                }
            }

            if (FillReadyResultsBuffer())
            {
                continue;
            }

            if (TryGetTicksUntilNextGeneratorWake(out var ticksUntilNextGeneratorWake))
            {
                yield return _database.ErrorWorkunitAvailableCondition.TimedWait(ticksUntilNextGeneratorWake);
            }
            else
            {
                yield return _database.ErrorWorkunitAvailableCondition.Wait();
            }
        }
    }

    private bool FillReadyResultsBuffer()
    {
        bool didWork = false;
        while (_database.CurrentResults.Count < MaxResultBuffer && _database.CurrentErrorResults.Count > 0)
        {
            var workunit = _database.CurrentErrorResults.Dequeue();
            didWork = true;
            if (workunit.CurrentErrorResults > 0)
            {
                workunit.CurrentErrorResults--;
            }

            if (workunit.CurrentState == WorkunitModel.State.InProgress && workunit.CanCreateMoreResults()) // Check against absolute max
            {
                var result = workunit.CreateResult(TimeSystem.CurTick);
                if (result != null)
                {
                    _database.CurrentResults.Enqueue(result);
                    _database.ResultAvailableCondition.Signal();
                    StatService.RecordResultCreated();
                }
            }

            TryQueueAssimilation(workunit);
            // Yield to process one per frame to avoid freezing if the error queue is large
        }

        if (ProcessExpiredSentResults())
        {
            didWork = true;
            if (_database.CurrentErrorResults.Count > 0)
            {
                return true;
            }
        }

        while (_database.CurrentResults.Count < MaxResultBuffer && TryCreateWeightedWorkunit(out var workunit))
        {
            didWork = true;
            while (workunit.CanCreateInitialResults())
            {
                var result = workunit.CreateResult(TimeSystem.CurTick);
                if (result != null)
                {
                    _database.CurrentResults.Enqueue(result);
                    _database.ResultAvailableCondition.Signal();
                    StatService.RecordResultCreated();
                    CountInitialReplicaCreated();
                }
            }
        }

        return didWork;
    }

    private bool ProcessExpiredSentResults()
    {
        bool didWork = false;
        var workunits = _database.CurrentWorkunits.Values.ToList();
        foreach (var workunit in workunits)
        {
            bool didWorkunitWork = false;
            while (workunit.SentResultNumbers.Count > 0)
            {
                var resultNumber = workunit.SentResultNumbers.Peek();
                var result = workunit.Results.FirstOrDefault(w => w.resultNumber == resultNumber);
                if (result == null || !result.isSent)
                {
                    workunit.SentResultNumbers.Dequeue();
                    didWork = true;
                    didWorkunitWork = true;
                    continue;
                }

                if (TimeSystem.CurTick < result.deadlineTick)
                {
                    break;
                }

                workunit.SentResultNumbers.Dequeue();
                didWork = true;
                didWorkunitWork = true;
                if (result.isValidationCompleted)
                {
                    continue;
                }

                if (HasPendingValidation(workunit.Name, result.resultNumber))
                {
                    continue;
                }

                ProcessResultValidation(workunit, result, ResultStatus.Fail, ResultValue.Incorrect, 0, true);
            }

            if (didWorkunitWork)
            {
                TryQueueAssimilation(workunit);
            }
        }

        return didWork;
    }

    private bool HasPendingValidation(string workunitName, int resultNumber)
    {
        foreach (var reply in _database.CurrentValidations)
        {
            if (reply.WorkunitName == workunitName && reply.ResultNumber == resultNumber)
            {
                return true;
            }
        }

        return false;
    }

    private void InitializeApplications(float theoreticalGflopsBudget)
    {
        _database.Applications.Clear();
        _database.ApplicationTargets = new int[_database.Config.ApplicationConfigs.Count];
        _database.NeededQuorumReplicas = 0;
        _database.TheoreticalGflopsBudget = theoreticalGflopsBudget;
        _database.EffectiveGflopsBudget = Mathf.Max(_database.TheoreticalGflopsBudget * _database.Config.UtilizationSafety, 0);

        for (int i = 0; i < _database.Config.ApplicationConfigs.Count; i++)
        {
            var applicationConfig = _database.Config.ApplicationConfigs[i];
            _database.Applications.Add(new ApplicationRuntimeState
            {
                ApplicationIndex = i,
                IsOn = true,
                SuspendedUntil = 0,
                CurrentPeriodWorkunits = 0,
                WorkunitsNumber = applicationConfig.WorkunitsNumber,
                SleepTime = applicationConfig.SleepTime,
                TailTargetWorkunitsTotal = 0
            });
        }

        if (_database.Config.GenerationMode == WorkunitGenerationMode.TailBudget)
        {
            ApplyTailBudget();
        }
    }

    private void ApplyTailBudget()
    {
        float totalPercentage = 0;
        foreach (var applicationConfig in _database.Config.ApplicationConfigs)
        {
            totalPercentage += Mathf.Max(applicationConfig.ApplicationPercentage, 0);
        }

        for (int i = 0; i < _database.Applications.Count; i++)
        {
            var applicationConfig = _database.Config.ApplicationConfigs[i];
            var state = _database.Applications[i];
            float appBudget = 0;
            float workunitCost = applicationConfig.InitialCreatedResults * applicationConfig.TaskGflops;
            if (totalPercentage > 0)
            {
                appBudget = _database.EffectiveGflopsBudget * Mathf.Max(applicationConfig.ApplicationPercentage, 0) / totalPercentage;
            }

            state.TailTargetWorkunitsTotal = workunitCost > 0 ? Mathf.FloorToInt(appBudget / workunitCost) : 0;
            state.WorkunitsNumber = state.TailTargetWorkunitsTotal;
            state.SleepTime = SimManager.MaxSimulationTime;
            _database.ApplicationTargets[i] = state.TailTargetWorkunitsTotal;
            _database.NeededQuorumReplicas += state.TailTargetWorkunitsTotal * applicationConfig.InitialCreatedResults;
        }
    }

    private bool TryCreateWeightedWorkunit(out WorkunitModel workunit)
    {
        workunit = null;
        RefreshApplicationStates();

        float totalPercentage = 0;
        foreach (var state in _database.Applications)
        {
            if (state.IsOn)
            {
                totalPercentage += Mathf.Max(_database.Config.ApplicationConfigs[state.ApplicationIndex].ApplicationPercentage, 0);
            }
        }

        if (totalPercentage <= 0)
        {
            return false;
        }

        float rand = RandomUtils.GetDistribution(Distribution.Uniform, 0, totalPercentage);
        float current = 0;
        for (int i = 0; i < _database.Applications.Count; i++)
        {
            var state = _database.Applications[i];
            if (!state.IsOn)
            {
                continue;
            }

            current += Mathf.Max(_database.Config.ApplicationConfigs[state.ApplicationIndex].ApplicationPercentage, 0);
            if (rand < current)
            {
                return TryCreateWorkunitForApplication(state, out workunit);
            }
        }

        return false;
    }

    private void RefreshApplicationStates()
    {
        for (int i = 0; i < _database.Applications.Count; i++)
        {
            var state = _database.Applications[i];
            if (state.IsOn)
            {
                if (state.CurrentPeriodWorkunits >= state.WorkunitsNumber)
                {
                    state.IsOn = false;
                    state.SuspendedUntil = Mathf.Min(TimeSystem.CurTick + state.SleepTime, SimManager.MaxSimulationTime);
                    state.CurrentPeriodWorkunits = 0;
                }
            }
            else
            {
                if (state.SuspendedUntil < TimeSystem.CurTick)
                {
                    state.IsOn = true;
                }
            }
        }
    }

    private bool TryGetTicksUntilNextApplicationWake(out int ticksToWait)
    {
        int nearestWake = SimManager.MaxSimulationTime;
        bool hasSleepingApplication = false;

        for (int i = 0; i < _database.Applications.Count; i++)
        {
            var state = _database.Applications[i];
            if (!state.IsOn && state.SuspendedUntil > TimeSystem.CurTick)
            {
                nearestWake = Mathf.Min(nearestWake, state.SuspendedUntil);
                hasSleepingApplication = true;
            }
        }

        if (!hasSleepingApplication)
        {
            ticksToWait = 0;
            return false;
        }

        ticksToWait = Mathf.Max(nearestWake - TimeSystem.CurTick, 1);
        return true;
    }

    private bool TryGetTicksUntilNextSentResultDeadline(out int ticksToWait)
    {
        int nearestDeadline = SimManager.MaxSimulationTime;
        bool hasSentResult = false;

        foreach (var workunit in _database.CurrentWorkunits.Values)
        {
            if (workunit.SentResultNumbers.Count == 0)
            {
                continue;
            }

            var resultNumber = workunit.SentResultNumbers.Peek();
            var result = workunit.Results.FirstOrDefault(w => w.resultNumber == resultNumber);
            if (result == null || !result.isSent)
            {
                continue;
            }

            nearestDeadline = Mathf.Min(nearestDeadline, result.deadlineTick);
            hasSentResult = true;
        }

        if (!hasSentResult)
        {
            ticksToWait = 0;
            return false;
        }

        ticksToWait = Mathf.Max(nearestDeadline - TimeSystem.CurTick, 1);
        return true;
    }

    private bool TryGetTicksUntilNextGeneratorWake(out int ticksToWait)
    {
        bool hasWake = false;
        int nearestWake = SimManager.MaxSimulationTime;

        if (TryGetTicksUntilNextApplicationWake(out var ticksUntilNextApplicationWake))
        {
            nearestWake = Mathf.Min(nearestWake, TimeSystem.CurTick + ticksUntilNextApplicationWake);
            hasWake = true;
        }

        if (TryGetTicksUntilNextSentResultDeadline(out var ticksUntilNextSentResultDeadline))
        {
            nearestWake = Mathf.Min(nearestWake, TimeSystem.CurTick + ticksUntilNextSentResultDeadline);
            hasWake = true;
        }

        if (!hasWake)
        {
            ticksToWait = 0;
            return false;
        }

        ticksToWait = Mathf.Max(nearestWake - TimeSystem.CurTick, 1);
        return true;
    }

    private bool TryCreateWorkunitForApplication(ApplicationRuntimeState state, out WorkunitModel workunit)
    {
        var workunitName = $"Workunit-{_database.WorkunitsCreated}";
        var selectedApplicationConfig = _database.Config.ApplicationConfigs[state.ApplicationIndex];
        workunit = new WorkunitModel(workunitName, selectedApplicationConfig, state.ApplicationIndex, TimeSystem.CurTick);
        _database.CurrentWorkunits.Add(workunitName, workunit);
        state.CurrentPeriodWorkunits++;
        _database.WorkunitsCreated++;
        OnWorkunitCreated?.Invoke(state.ApplicationIndex);
        return true;
    }

    private void TryQueueAssimilation(WorkunitModel workunit)
    {
        if (workunit.QueuedForAssimilation)
        {
            return;
        }

        if (workunit.CurrentState != WorkunitModel.State.InProgress &&
            workunit.ReceivedResults >= workunit.TotalResults &&
            workunit.CurrentErrorResults == 0 &&
            workunit.SentResultNumbers.Count == 0)
        {
            workunit.QueuedForAssimilation = true;
            OnWorkunitCompleted?.Invoke();
            _database.CurrentAssimilations.Enqueue(workunit);
            _database.AssimilationWorkunitAvailableCondition.Signal();
        }
    }

    private IEnumerator ValidatorLoop()
    {
        while (true)
        {
            while (_database.CurrentValidations.Count == 0)
            {
                yield return _database.ValidationReplyAvailableCondition.Wait();
            }

            while (_database.CurrentValidations.Count > 0)
            {
                var reply = _database.CurrentValidations.Dequeue();
                if (_database.CurrentWorkunits.TryGetValue(reply.WorkunitName, out var workunit))
                {
                    if (workunit.QueuedForAssimilation)
                    {
                        RecordLateReply(reply);
                        continue;
                    }

                    var result = workunit.Results.FirstOrDefault(w => w.resultNumber == reply.ResultNumber);
                    if (result == null)
                    {
                        Debug.LogError($"Result with ResultNumber {reply.ResultNumber} not found for workunit '{workunit.Name}'.");
                        continue;
                    }

                    if (result.isValidationCompleted)
                    {
                        continue;
                    }

                    ProcessResultValidation(workunit, result, reply.status, reply.value, reply.credits, false);
                }
                else
                {
                    var key = GetResultKey(reply.WorkunitName, reply.ResultNumber);
                    if (_database.ServerTimedOutResults.Contains(key))
                    {
                        _database.ServerTimedOutResults.Remove(key);
                        continue;
                    }

                    RecordLateReply(reply);
                    // This is a late reply for an already assimilated workunit.
                }
            }

        }
    }

    private void ProcessResultValidation(WorkunitModel workunit, ResultData result, ResultStatus status, ResultValue value, int credits, bool isServerTimeout)
    {
        if (result.isValidationCompleted)
        {
            return;
        }

        if (isServerTimeout)
        {
            result.isServerTimedOut = true;
            _database.ServerTimedOutResults.Add(GetResultKey(workunit.Name, result.resultNumber));
            StatService.RecordResultReceived();
        }

        result.isValidationCompleted = true;
        workunit.ReceivedResults++;

        var isTimeout = isServerTimeout || TimeSystem.CurTick - result.sentTick >= workunit.Config.DelayBound;
        var isFail = isTimeout || status == ResultStatus.Fail;
        var isSuccessIncorrect = !isFail && value == ResultValue.Incorrect;
        var shouldCreateReplacement = isFail || isSuccessIncorrect;
        StatService.RecordResultAnalyzed(isTimeout, status == ResultStatus.Success);
        StatService.RecordGotResult(value == ResultValue.Correct ? 1 : 0, TimeSystem.CurTick);

        if (!isFail)
        {
            workunit.SuccessResults++;
            if (value == ResultValue.Correct)
            {
                workunit.ValidResults++;
                if (workunit.Credits == -1)
                {
                    workunit.Credits = credits;
                }
                else if (credits < workunit.Credits)
                {
                    workunit.Credits = credits;
                }
            }
        }
        else
        {
            workunit.ErrorResults++;
        }

        if (workunit.CurrentState == WorkunitModel.State.InProgress)
        {
            if (workunit.ValidResults >= workunit.MinQuorum)
            {
                workunit.CurrentState = WorkunitModel.State.Valid;
                int credit = workunit.Credits > 0 ? workunit.Credits : 0;
                StatService.RecordWorkunitReachedQuorum(workunit.ApplicationIndex, workunit.ValidResults, credit);
                OnWorkunitValid?.Invoke(workunit.ApplicationIndex);
            }
            else if (workunit.TotalResults >= workunit.Config.MaxCreatedResults ||
                     workunit.ErrorResults >= workunit.Config.MaxErrorResults ||
                     workunit.SuccessResults >= workunit.Config.MaxSuccessResults)
            {
                workunit.CurrentState = WorkunitModel.State.Error;
            }

            if (shouldCreateReplacement &&
                workunit.CurrentState == WorkunitModel.State.InProgress &&
                workunit.SuccessResults < workunit.Config.MaxSuccessResults &&
                workunit.ErrorResults < workunit.Config.MaxErrorResults &&
                workunit.TotalResults < workunit.Config.MaxCreatedResults)
            {
                _database.CurrentErrorResults.Enqueue(workunit);
                workunit.CurrentErrorResults++;
                _database.ErrorWorkunitAvailableCondition.Signal();
            }
        }
        else if (workunit.CurrentState == WorkunitModel.State.Valid && !isFail && value == ResultValue.Correct)
        {
            int credit = workunit.Credits > 0 ? workunit.Credits : 0;
            StatService.RecordAdditionalValidResult(credit);
        }

        TryQueueAssimilation(workunit);
    }

    private string GetResultKey(string workunitName, int resultNumber)
    {
        return $"{workunitName}:{resultNumber}";
    }

    private void RecordLateReply(ClientReplyData reply)
    {
        StatService.RecordResultAnalyzed(false, reply.status == ResultStatus.Success);
        StatService.RecordGotResult(reply.value == ResultValue.Correct ? 1 : 0, TimeSystem.CurTick);
    }

    private void CountInitialReplicaCreated()
    {
        if (_database.NeededQuorumReplicas > 0)
        {
            _database.NeededQuorumReplicas--;
        }
    }

    private IEnumerator AssimilatorLoop()
    {
        while (true)
        {
            while (_database.CurrentAssimilations.Count == 0)
            {
                yield return _database.AssimilationWorkunitAvailableCondition.Wait();
            }

            while (_database.CurrentAssimilations.Count > 0)
            {
                var workunitToAssimilate = _database.CurrentAssimilations.Dequeue();
              
                if (workunitToAssimilate.CurrentState == WorkunitModel.State.Valid)
                {
                    _database.ValidWorkunits++;
                    StatService.RecordWorkunitAssimilated(true);
                }
                else
                {
                    _database.ErrorWorkunits++;
                    StatService.RecordWorkunitAssimilated(false);
                }
                _database.CurrentWorkunits.Remove(workunitToAssimilate.Name);
                TryFinishTailSimulation();
            }

        }
    }

    private void TryFinishTailSimulation()
    {
        if (_isTailMode && _database.CurrentWorkunits.Count == 0)
        {
            SimManager.RequestSimulationFinish();
        }
    }
}
