public class StatService : IStatService
{
    private readonly StatsData data = new StatsData();
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

    public void Initialize(int maxSimulationTime, SimConfig simConfig)
    {
        int applicationCount = simConfig.ProjectConfig.ApplicationConfigs.Count;
        data.SimulationDuration = maxSimulationTime;
        data.ProjectName = simConfig.ProjectConfig.ProjectName;
        data.NumberOfClients = simConfig.GroupConfig.NumberOfClients;
        data.TailStageActive = simConfig.ProjectConfig.GenerationMode == WorkunitGenerationMode.TailBudget ? 1 : 0;
        data.TailStartTick = -1;
        data.FinishTick = maxSimulationTime;
        data.UtilizationSafety = simConfig.ProjectConfig.UtilizationSafety;
        data.TheoreticalGflopsBudget = 0;
        data.EffectiveGflopsBudget = 0;
        data.FirstApplicationInitialResults = applicationCount > 0 ? simConfig.ProjectConfig.ApplicationConfigs[0].InitialCreatedResults : 0;
        data.UnfinishedWorkunits = 0;
        data.CreatedWorkunits = 0;
        data.CompletedWorkunits = 0;
        data.MessagesReceived = 0;
        data.WorkRequestsReceived = 0;
        data.ResultsCreated = 0;
        data.ResultsSent = 0;
        data.ResultsReceived = 0;
        data.ResultsAnalyzed = 0;
        data.ResultsSuccess = 0;
        data.ResultsFailed = 0;
        data.ResultsTooLate = 0;
        data.ResultsValid = 0;
        data.WorkunitsValid = 0;
        data.WorkunitsValidButNotCompleted = 0;
        data.WorkunitsError = 0;
        data.TotalCredit = 0;
        data.Availability.Clear();
        data.Unavailability.Clear();
        data.SpeedStatistics.Clear();
        data.SentResults.Clear();
        data.GotResults.Clear();
        int arrayLength = maxSimulationTime + 1;

        data.GridOnlinePowerDeltas = new float[arrayLength];
        data.GridIdlePowerDeltas = new float[arrayLength];
        data.ClientsAvailability = new int[arrayLength];
        data.ValidCompletedWorkunitsTimestamps = new int[arrayLength];
        data.WorkunitTimestamps = new int[arrayLength];
        data.ValidWorkunitsTimestamps = new int[applicationCount][];
        data.CreationWorkunitTimestamps = new int[applicationCount][];
        data.ApplicationWorkunitsTotal = new int[applicationCount];
        data.ApplicationTailTargetWorkunitsTotal = new int[applicationCount];
        data.ApplicationInitialResults = new int[applicationCount];
        data.ApplicationTaskGflops = new float[applicationCount];

        for (int i = 0; i < applicationCount; i++)
        {
            data.ValidWorkunitsTimestamps[i] = new int[arrayLength];
            data.CreationWorkunitTimestamps[i] = new int[arrayLength];
            data.ApplicationInitialResults[i] = simConfig.ProjectConfig.ApplicationConfigs[i].InitialCreatedResults;
            data.ApplicationTaskGflops[i] = simConfig.ProjectConfig.ApplicationConfigs[i].TaskGflops;
        }
    }

    public StatsData GetStats()
    {
        // Ð’Ð¾Ð·Ð²Ñ€Ð°Ñ‰Ð°ÐµÐ¼ ÐºÐ¾Ð¿Ð¸ÑŽ, Ñ‡Ñ‚Ð¾Ð±Ñ‹ Ð²Ñ‹Ð·Ñ‹Ð²Ð°ÑŽÑ‰Ð¸Ð¹ ÐºÐ¾Ð´ Ð½Ðµ Ð¼Ð¾Ð³ Ð¸Ð·Ð¼ÐµÐ½Ð¸Ñ‚ÑŒ ÑÐ¾ÑÑ‚Ð¾ÑÐ½Ð¸Ðµ ÑÐµÑ€Ð²Ð¸ÑÐ°
        return data;
    }

    public void RegisterClient(IClientStats client)
    {
        client.OnBusyMode += OnBusyMode;
        client.OnIdleMode += OnIdleMode;
        client.OnGoingOnline += OnGoingOnline;
        client.OnGoingOffline += OnGoingOffline;
    }

    public void RegisterProject(IProjectStats project)
    {
        project.OnWorkunitCreated += OnWorkunitCreated;
        project.OnWorkunitValid += OnWorkunitValid;
        project.OnWorkunitCompleted += OnWorkunitCompleted;
    }

    public void RecordAvailability(float durationHours, int startTick, int durationTicks)
    {
        int start = data.GetBoundedTick(startTick);
        int end = data.GetBoundedTick(startTick + durationTicks);
        data.ClientsAvailability[start] += 1;
        data.ClientsAvailability[end] -= 1;
        data.Availability.Add(new StatValueData(durationHours));
    }

    public void RecordUnavailability(float durationHours)
    {
        data.Unavailability.Add(new StatValueData(durationHours));
    }

    public void RecordHostPower(float power)
    {
        data.SpeedStatistics.Add(new StatValueData(power));
    }

    public void RecordWorkRequestReceived()
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.MessagesReceived += 1;
        data.WorkRequestsReceived += 1;
    }

    public void RecordResultCreated()
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.ResultsCreated += 1;
    }

    public void RecordResultsSent(int resultsNumber)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.ResultsSent += resultsNumber;
    }

    public void RecordResultReceived()
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.MessagesReceived += 1;
        data.ResultsReceived += 1;
    }

    public void RecordResultAnalyzed(bool isTimeout, bool isSuccess)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.ResultsAnalyzed += 1;
        if (isTimeout)
        {
            data.ResultsTooLate += 1;
        }
        else if (isSuccess)
        {
            data.ResultsSuccess += 1;
        }
        else
        {
            data.ResultsFailed += 1;
        }
    }

    public void RecordWorkunitReachedQuorum(int applicationIndex, int validResults, int credit)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.WorkunitsValidButNotCompleted += 1;
        data.ResultsValid += validResults;
        data.TotalCredit += credit * validResults;
    }

    public void RecordAdditionalValidResult(int credit)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.ResultsValid += 1;
        data.TotalCredit += credit;
    }

    public void RecordWorkunitAssimilated(bool isValid)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        data.CompletedWorkunits += 1;
        data.UnfinishedWorkunits -= 1;
        if (isValid)
        {
            data.WorkunitsValid += 1;
        }
        else
        {
            data.WorkunitsError += 1;
        }
    }

    public void RecordTailBudget(float theoreticalGflopsBudget, float effectiveGflopsBudget, int[] applicationTargets)
    {
        data.TheoreticalGflopsBudget = theoreticalGflopsBudget;
        data.EffectiveGflopsBudget = effectiveGflopsBudget;
        for (int i = 0; i < data.ApplicationTailTargetWorkunitsTotal.Length && i < applicationTargets.Length; i++)
        {
            data.ApplicationTailTargetWorkunitsTotal[i] = applicationTargets[i];
        }
    }

    public void RecordTailStarted(int tick)
    {
        if (data.TailStartTick < 0)
        {
            data.TailStartTick = data.GetBoundedTick(tick);
        }
    }

    public void RecordSimulationFinished(int tick)
    {
        data.FinishTick = data.GetBoundedTick(tick);
    }

    public void RecordSentResults(int resultsNumber, int timestamp)
    {
        if (timestamp >= data.SimulationDuration)
        {
            return;
        }

        data.SentResults.Add(new StatPairData(resultsNumber, timestamp));
    }

    public void RecordGotResult(int isCorrect, int timestamp)
    {
        if (timestamp >= data.SimulationDuration)
        {
            return;
        }

        data.GotResults.Add(new StatPairData(isCorrect, timestamp));
    }

    private void OnGoingOffline(string hostName, float power)
    {
        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.GridOnlinePowerDeltas[tick] -= power;
        data.OnlinePower -= power;
    }

    private void OnGoingOnline(string hostName, float power)
    {
        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.GridOnlinePowerDeltas[tick] += power;
        data.OnlinePower += power;
    }

    private void OnIdleMode(string hostName, float power)
    {
        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.GridIdlePowerDeltas[tick] += power;
        data.IdlePower += power;
    }

    private void OnBusyMode(string hostName, float power)
    {
        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.GridIdlePowerDeltas[tick] -= power;
        data.IdlePower -= power;
    }

    private void OnWorkunitCompleted()
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.ValidCompletedWorkunitsTimestamps[tick] += 1;
    }

    private void OnWorkunitCreated(int applicationIndex)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.CreationWorkunitTimestamps[applicationIndex][tick] += 1;
        data.UnfinishedWorkunits += 1;
        data.CreatedWorkunits += 1;
        data.ApplicationWorkunitsTotal[applicationIndex] += 1;
    }

    private void OnWorkunitValid(int applicationIndex)
    {
        if (!IsWithinSimulationStatsDuration())
        {
            return;
        }

        int tick = data.GetBoundedTick(TimeSystem.CurTick);
        data.ValidWorkunitsTimestamps[applicationIndex][tick] += 1;
    }

    private bool IsWithinSimulationStatsDuration()
    {
        return TimeSystem.CurTick < data.SimulationDuration;
    }

}
