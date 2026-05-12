using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProjectModel : BaseActor, IProjectStats
{

    public event Action<int> OnWorkunitCreated;

    public event Action<int> OnWorkunitValid;

    public event Action OnWorkunitCompleted;
    public string ProjectName => _config.ProjectName;
    public IReadOnlyDictionary<string, WorkunitModel> WorkunitDatabase => _workunitDatabase;

    private readonly ProjectConfig _config;
    
    private readonly Dictionary<string, WorkunitModel> _workunitDatabase = new Dictionary<string, WorkunitModel>();
    private readonly Queue<ResultData> _readyResultsQueue = new Queue<ResultData>();
    private readonly Queue<ClientRequestData> _requestQueue = new Queue<ClientRequestData>();
    private readonly Queue<ClientReplyData> _validationQueue = new Queue<ClientReplyData>();
    private readonly Queue<WorkunitModel> _errorWorkunitsQueue = new Queue<WorkunitModel>();
    private readonly Queue<WorkunitModel> _assimilationQueue = new Queue<WorkunitModel>();
    private readonly List<ApplicationRuntimeState> _applicationStates = new List<ApplicationRuntimeState>();
    private int _workGeneratorWakeVersion = 0;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;
    private GroupConfig GroupConfig => Container.Instance.ConfigProvider.SimConfig.GroupConfig;
    private IStatService StatService => Container.Instance.StatService;
    private const int MaxResultBuffer = 100;

    public int WorkunitsCreated = 0;
    public int ValidWorkunits = 0;
    public int ErrorWorkunits = 0;

    public ProjectModel(ProjectConfig config, int actorId, HostModel host) : base($"project{actorId}", host)
    {
        _config = config;
        Container.Instance.StatService.RegisterProject(this);
    }

    public override IEnumerator MainLoop()
    {
        SimManager.StartSimulationCoroutine(WorkunitGeneratorLoop());
        SimManager.StartSimulationCoroutine(ValidatorLoop());
        SimManager.StartSimulationCoroutine(AssimilatorLoop());
        SimManager.StartSimulationCoroutine(SchedulingServerDispatcherLoop());
        yield return RequestDispatcherLoop();
    }

    private IEnumerator RequestDispatcherLoop()
    {
        while (true)
        {
            IMessage message;
            while ((message = Receive()) != null)
            {
                if (message is ClientRequestData request)
                {
                    _requestQueue.Enqueue(request);
                    StatService.RecordWorkRequestReceived();
                }
                else if (message is ClientReplyData reply)
                {
                    _validationQueue.Enqueue(reply);
                    StatService.RecordResultReceived();
                }
            }

            yield return new WaitForTicks(1);
        }
    }

    private IEnumerator SchedulingServerDispatcherLoop()
    {
        while (true)
        {
            while (_requestQueue.Count > 0)
            {
                var request = _requestQueue.Dequeue();

                if (_readyResultsQueue.Count == 0)
                {
                    int targetTick = TimeSystem.CurTick + 5;
                    while (_readyResultsQueue.Count == 0 && TimeSystem.CurTick < targetTick)
                    {
                        yield return new WaitForTicks(1);
                    }
                }

                ProcessWorkRequest(request);
            }

            yield return new WaitForTicks(1);
        }
    }

    private void ProcessWorkRequest(ClientRequestData request)
    {
        var resultsToSend = new List<ResultData>();

        if (_readyResultsQueue.Count > 0)
        {
            var firstResult = _readyResultsQueue.Dequeue();
            resultsToSend.Add(firstResult);
            int resultsNumber = CalculateResultsNumber(request, firstResult);

            while (resultsToSend.Count < resultsNumber && _readyResultsQueue.Count > 0)
            {
                resultsToSend.Add(_readyResultsQueue.Dequeue());
            }

            if (_readyResultsQueue.Count == 0)
            {
                WakeWorkGenerator();
            }
        }

        
        var tasksToSend = new List<ClientTaskData>();
        float inputTransferSizeMb = 0;
        foreach (var result in resultsToSend)
        {
            var applicationConfig = _config.ApplicationConfigs[result.ApplicationIndex];
            result.deadlineTick = TimeSystem.CurTick + applicationConfig.DelayBound;
            tasksToSend.Add(new ClientTaskData(
                result.WorkunitName,
                result.resultNumber,
                result.resultNumber,
                result.ApplicationIndex,
                result.durationInGflops,
                result.outputFileSizeMb,
                TimeSystem.CurTick,
                applicationConfig.DelayBound
            ));
            inputTransferSizeMb += result.inputFileSizeMb;

            if (_workunitDatabase.TryGetValue(result.WorkunitName, out var workunit))
            {
                workunit.SentResults++;
            }
        }

        var reply = new ServerReplyData(tasksToSend, inputTransferSizeMb);
        StatService.RecordResultsSent(resultsToSend.Count);
        StatService.RecordSentResults(tasksToSend.Count, TimeSystem.CurTick);
        Push(request.RequesterName, reply);
    }

    private int CalculateResultsNumber(ClientRequestData request, ResultData result)
    {
        float taskDuration = result.durationInGflops / request.Power;
        int resultsNumber = Mathf.FloorToInt(request.Percentage / taskDuration);
        if (resultsNumber == 0)
        {
            resultsNumber = 1;
        }

        return resultsNumber;
    }

    private IEnumerator WorkunitGeneratorLoop()
    {
        Debug.Assert(_config.ApplicationConfigs.Any(), $"ProjectConfig '{_config.name}' has no ApplicationConfigs assigned.");

        // TODO: расчитать долю каждого проекта

        InitializeApplicationStates();

        // заполнить каждым конфигом массив соответствующего размера
        //InitialWorkunitsPerApp = new List<int>{2000};
        // TODO: сделать соотношение между несколькими проектами
        while (true)
        {
            bool didWork = FillReadyResultsBuffer();

            if (_readyResultsQueue.Count >= MaxResultBuffer)
            {
                yield return WaitForWorkGeneratorWakeOrTimeout(Mathf.Max(SimManager.MaxSimulationTime - TimeSystem.CurTick, 1));
            }
            else if (!didWork)
            {
                yield return WaitForWorkGeneratorWakeOrTimeout(GetTicksUntilNextApplicationWake());
            }
            else
            {
                yield return new WaitForTicks(1);
            }
        }
    }

    private bool FillReadyResultsBuffer()
    {
        bool didWork = false;
        while (_readyResultsQueue.Count < MaxResultBuffer && _errorWorkunitsQueue.Count > 0)
        {
            var workunit = _errorWorkunitsQueue.Dequeue();
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
                    _readyResultsQueue.Enqueue(result);
                    StatService.RecordResultCreated();
                }
            }

            TryQueueAssimilation(workunit);
            // Yield to process one per frame to avoid freezing if the error queue is large
        }

        while (_readyResultsQueue.Count < MaxResultBuffer && TryCreateWeightedWorkunit(out var workunit))
        {
            didWork = true;
            while (workunit.CanCreateInitialResults())
            {
                var result = workunit.CreateResult(TimeSystem.CurTick);
                if (result != null)
                {
                    _readyResultsQueue.Enqueue(result);
                    StatService.RecordResultCreated();
                }
            }
        }

        return didWork;
    }

    private void InitializeApplicationStates()
    {
        _applicationStates.Clear();
        for (int i = 0; i < _config.ApplicationConfigs.Count; i++)
        {
            var applicationConfig = _config.ApplicationConfigs[i];
            _applicationStates.Add(new ApplicationRuntimeState
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

        if (_config.GenerationMode == WorkunitGenerationMode.TailBudget)
        {
            InitializeTailBudget();
        }
    }

    private void InitializeTailBudget()
    {
        float totalPercentage = 0;
        foreach (var applicationConfig in _config.ApplicationConfigs)
        {
            totalPercentage += Mathf.Max(applicationConfig.ApplicationPercentage, 0);
        }

        float meanSpeed = GroupConfig.TailMeanSpeed >= 0
            ? GroupConfig.TailMeanSpeed
            : GetClampedDistributionMean(
                GroupConfig.RandomConfig.HostPowerDistri,
                GroupConfig.RandomConfig.PowerA,
                GroupConfig.RandomConfig.PowerB,
                GroupConfig.MinSpeed,
                GroupConfig.MaxSpeed
            );

        float availability = GroupConfig.TailAvailabilityPercent >= 0
            ? GroupConfig.TailAvailabilityPercent / 100f
            : GetGroupAvailabilityMean(GroupConfig);

        if (availability > 1)
        {
            availability = 1;
        }

        float theoreticalGflopsBudget = GroupConfig.NumberOfClients * meanSpeed * availability * SimManager.MaxSimulationTime;
        float effectiveGflopsBudget = Mathf.Max(theoreticalGflopsBudget * _config.UtilizationSafety, 0);

        for (int i = 0; i < _applicationStates.Count; i++)
        {
            var applicationConfig = _config.ApplicationConfigs[i];
            var state = _applicationStates[i];
            float appBudget = 0;
            float workunitCost = applicationConfig.InitialCreatedResults * applicationConfig.TaskGflops;
            if (totalPercentage > 0)
            {
                appBudget = effectiveGflopsBudget * Mathf.Max(applicationConfig.ApplicationPercentage, 0) / totalPercentage;
            }
            state.TailTargetWorkunitsTotal = workunitCost > 0 ? Mathf.FloorToInt(appBudget / workunitCost) : 0;
            state.WorkunitsNumber = state.TailTargetWorkunitsTotal;
            state.SleepTime = SimManager.MaxSimulationTime;
        }

        int[] applicationTargets = new int[_applicationStates.Count];
        for (int i = 0; i < _applicationStates.Count; i++)
        {
            applicationTargets[i] = _applicationStates[i].TailTargetWorkunitsTotal;
        }
        StatService.RecordTailBudget(theoreticalGflopsBudget, effectiveGflopsBudget, applicationTargets);
    }

    private float GetGroupAvailabilityMean(GroupConfig groupConfig)
    {
        float online = Mathf.Max(GetDistributionMean(
            groupConfig.RandomConfig.HostAvailabilityDistri,
            groupConfig.RandomConfig.HostAvailabilityA,
            groupConfig.RandomConfig.HostAvailabilityB
        ), 0);
        float offline = Mathf.Max(GetDistributionMean(
            groupConfig.RandomConfig.HostNonavailabilityDistri,
            groupConfig.RandomConfig.HostNonavailabilityA,
            groupConfig.RandomConfig.HostNonavailabilityB
        ), 0);
        float total = online + offline;
        if (total <= 0)
        {
            return 0;
        }
        return online / total;
    }

    private float GetClampedDistributionMean(Distribution distribution, float a, float b, float minSpeed, float maxSpeed)
    {
        float mean = GetDistributionMean(distribution, a, b);

        if (maxSpeed < minSpeed)
        {
            float aux = minSpeed;
            minSpeed = maxSpeed;
            maxSpeed = aux;
        }

        if (distribution == Distribution.Exponential && a > 0)
        {
            float lower = Mathf.Max(minSpeed, 0);
            float upper = Mathf.Max(maxSpeed, lower);
            float pLow = 1f - Mathf.Exp(-a * lower);
            float pHigh = Mathf.Exp(-a * upper);
            float middle = (lower + 1f / a) * Mathf.Exp(-a * lower) - (upper + 1f / a) * Mathf.Exp(-a * upper);
            return lower * pLow + middle + upper * pHigh;
        }

        if (mean < minSpeed)
        {
            return minSpeed;
        }
        if (mean > maxSpeed)
        {
            return maxSpeed;
        }
        return mean;
    }

    private float GetDistributionMean(Distribution distribution, float a, float b)
    {
        switch (distribution)
        {
            case Distribution.Weibull:
                if (a <= 0)
                {
                    return 0;
                }
                return b * Gamma(1f + 1f / a);
            case Distribution.Gamma:
                return a * b;
            case Distribution.Lognormal:
                return Mathf.Exp(a + b * b / 2f);
            case Distribution.Normal:
                return a;
            case Distribution.Hyperx:
                return a;
            case Distribution.Exponential:
                if (a <= 0)
                {
                    return 0;
                }
                return 1f / a;
            case Distribution.One:
                return 1f;
            case Distribution.Zero:
                return 0;
            case Distribution.Uniform:
                return (a + b) / 2f;
            default:
                return 0;
        }
    }

    private float Gamma(float z)
    {
        if (z <= 0)
        {
            return 0;
        }

        float z2 = z * z;
        float z3 = z2 * z;
        float correction = 1f + 1f / (12f * z) + 1f / (288f * z2) - 139f / (51840f * z3);
        return Mathf.Sqrt(2f * Mathf.PI / z) * Mathf.Pow(z / Mathf.Exp(1f), z) * correction;
    }

    private bool TryCreateWeightedWorkunit(out WorkunitModel workunit)
    {
        workunit = null;
        RefreshApplicationStates();

        float totalPercentage = 0;
        foreach (var state in _applicationStates)
        {
            if (state.IsOn)
            {
                totalPercentage += Mathf.Max(_config.ApplicationConfigs[state.ApplicationIndex].ApplicationPercentage, 0);
            }
        }

        if (totalPercentage <= 0)
        {
            return false;
        }

        float rand = RandomUtils.GetDistribution(Distribution.Uniform, 0, totalPercentage);
        float current = 0;
        for (int i = 0; i < _applicationStates.Count; i++)
        {
            var state = _applicationStates[i];
            if (!state.IsOn)
            {
                continue;
            }

            current += Mathf.Max(_config.ApplicationConfigs[state.ApplicationIndex].ApplicationPercentage, 0);
            if (rand < current)
            {
                return TryCreateWorkunitForApplication(state, out workunit);
            }
        }

        return false;
    }

    private void RefreshApplicationStates()
    {
        for (int i = 0; i < _applicationStates.Count; i++)
        {
            var state = _applicationStates[i];
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

    private int GetTicksUntilNextApplicationWake()
    {
        int nearestWake = SimManager.MaxSimulationTime;
        bool hasSleepingApplication = false;

        for (int i = 0; i < _applicationStates.Count; i++)
        {
            var state = _applicationStates[i];
            if (!state.IsOn && state.SuspendedUntil > TimeSystem.CurTick)
            {
                nearestWake = Mathf.Min(nearestWake, state.SuspendedUntil);
                hasSleepingApplication = true;
            }
        }

        if (!hasSleepingApplication)
        {
            return 1;
        }

        return Mathf.Max(nearestWake - TimeSystem.CurTick, 1);
    }

    private void WakeWorkGenerator()
    {
        _workGeneratorWakeVersion++;
    }

    private IEnumerator WaitForWorkGeneratorWakeOrTimeout(int ticksToWait)
    {
        int wakeVersion = _workGeneratorWakeVersion;
        int targetTick = TimeSystem.CurTick + ticksToWait;
        yield return new WaitWhile(() => _workGeneratorWakeVersion == wakeVersion && TimeSystem.CurTick < targetTick);
    }

    private bool TryCreateWorkunitForApplication(ApplicationRuntimeState state, out WorkunitModel workunit)
    {
        var workunitName = $"Workunit-{WorkunitsCreated}";
        var selectedApplicationConfig = _config.ApplicationConfigs[state.ApplicationIndex];
        workunit = new WorkunitModel(workunitName, selectedApplicationConfig, state.ApplicationIndex, TimeSystem.CurTick);
        _workunitDatabase.Add(workunitName, workunit);
        state.CurrentPeriodWorkunits++;
        WorkunitsCreated++;
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
            workunit.ReceivedResults == workunit.TotalResults &&
            workunit.CurrentErrorResults == 0)
        {
            workunit.QueuedForAssimilation = true;
            OnWorkunitCompleted?.Invoke();
            _assimilationQueue.Enqueue(workunit);
        }
    }

    private IEnumerator ValidatorLoop()
    {
        while (true)
        {
            while (_validationQueue.Count > 0)
            {
                var reply = _validationQueue.Dequeue();
                if (_workunitDatabase.TryGetValue(reply.WorkunitName, out var workunit))
                {
                    var result = workunit.Results.FirstOrDefault(w => w.resultNumber == reply.ResultNumber);
                    if (result == null)
                    {
                        Debug.LogError($"Result with ResultNumber {reply.ResultNumber} not found for workunit '{workunit.Name}'.");
                        continue;
                    }
                    workunit.ReceivedResults++;

                    var isTimeout = TimeSystem.CurTick - result.CreatedTick >= workunit.Config.DelayBound;
                    var isFail = isTimeout || reply.status == ResultStatus.Fail;
                    StatService.RecordResultAnalyzed(isTimeout, reply.status == ResultStatus.Success);
                    StatService.RecordGotResult(reply.value == ResultValue.Correct ? 1 : 0, TimeSystem.CurTick);

                    if (!isFail)
                    {
                        workunit.SuccessResults++;
                        if (reply.value == ResultValue.Correct)
                        {
                            workunit.ValidResults++;
                            if (workunit.Credits == -1)
                            {
                                workunit.Credits = reply.credits;
                            }
                            else if (reply.credits < workunit.Credits)
                            {
                                workunit.Credits = reply.credits;
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

                        if (isFail &&
                            workunit.CurrentState == WorkunitModel.State.InProgress &&
                            workunit.SuccessResults < workunit.Config.MaxSuccessResults &&
                            workunit.ErrorResults < workunit.Config.MaxErrorResults &&
                            workunit.TotalResults < workunit.Config.MaxCreatedResults)
                        {
                            _errorWorkunitsQueue.Enqueue(workunit);
                            workunit.CurrentErrorResults++;
                            WakeWorkGenerator();
                        }
                    }
                    else if (workunit.CurrentState == WorkunitModel.State.Valid && !isFail && reply.value == ResultValue.Correct)
                    {
                        int credit = workunit.Credits > 0 ? workunit.Credits : 0;
                        StatService.RecordAdditionalValidResult(credit);
                    }

                    TryQueueAssimilation(workunit);
                }
                else
                {
                    // This is a late reply for an already assimilated workunit.
                }
            }

            yield return new WaitForTicks(1);
        }
    }

    private IEnumerator AssimilatorLoop()
    {
        while (true)
        {
            while (_assimilationQueue.Count > 0)
            {
                var workunitToAssimilate = _assimilationQueue.Dequeue();
              
                if (workunitToAssimilate.CurrentState == WorkunitModel.State.Valid)
                {
                    ValidWorkunits++;
                    StatService.RecordWorkunitAssimilated(true);
                }
                else
                {
                    ErrorWorkunits++;
                    StatService.RecordWorkunitAssimilated(false);
                }
                _workunitDatabase.Remove(workunitToAssimilate.Name);
            }

            yield return new WaitForTicks(1);
        }
    }

    private class ApplicationRuntimeState
    {
        public int ApplicationIndex;
        public bool IsOn;
        public int SuspendedUntil;
        public int CurrentPeriodWorkunits;
        public float WorkunitsNumber;
        public int SleepTime;
        public int TailTargetWorkunitsTotal;
    }
}
