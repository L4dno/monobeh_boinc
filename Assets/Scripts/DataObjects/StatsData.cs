using System.Collections.Generic;

public class StatsData
{
    public int SimulationDuration;
    public string ProjectName;
    public int NumberOfClients;
    public int TailStageActive;
    public int TailStartTick;
    public int FinishTick;
    public float UtilizationSafety;
    public float TheoreticalGflopsBudget;
    public float EffectiveGflopsBudget;
    public int FirstApplicationInitialResults;
    public float OnlinePower;
    public float IdlePower;
    public float OnlinePowerTime;
    public float IdlePowerTime;
    public int SampleDuration;
    public int UnfinishedWorkunits;
    public int CreatedWorkunits;
    public int CompletedWorkunits;
    public int MessagesReceived;
    public int WorkRequestsReceived;
    public int ResultsCreated;
    public int ResultsSent;
    public int ResultsReceived;
    public int ResultsAnalyzed;
    public int ResultsSuccess;
    public int ResultsFailed;
    public int ResultsTooLate;
    public int ResultsValid;
    public int WorkunitsValid;
    public int WorkunitsValidButNotCompleted;
    public int WorkunitsError;
    public int TotalCredit;
    public float[] GridOnlinePowerDeltas;
    public float[] GridIdlePowerDeltas;
    public int[] ClientsAvailability;
    public int[] ValidCompletedWorkunitsTimestamps;
    public int[] WorkunitTimestamps;
    public int[][] ValidWorkunitsTimestamps;
    public int[][] CreationWorkunitTimestamps;
    public int[] ApplicationWorkunitsTotal;
    public int[] ApplicationTailTargetWorkunitsTotal;
    public int[] ApplicationInitialResults;
    public float[] ApplicationTaskGflops;
    public List<StatValueData> Availability = new List<StatValueData>();
    public List<StatValueData> Unavailability = new List<StatValueData>();
    public List<StatValueData> SpeedStatistics = new List<StatValueData>();
    public List<StatPairData> SentResults = new List<StatPairData>();
    public List<StatPairData> GotResults = new List<StatPairData>();

    public float Percent(int value, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return value / (float)total * 100;
    }

    public float GetAverage(List<StatValueData> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        float sum = 0;
        foreach (var value in values)
        {
            sum += value.Value;
        }

        return sum / values.Count;
    }

    public float GetAvailability()
    {
        float available = 0;
        foreach (var value in Availability)
        {
            available += value.Value;
        }

        float unavailable = 0;
        foreach (var value in Unavailability)
        {
            unavailable += value.Value;
        }

        float total = available + unavailable;
        if (total <= 0)
        {
            return 0;
        }

        return available / total * 100;
    }

    public int GetBoundedTick(int tick)
    {
        if (tick < 0)
        {
            return 0;
        }

        if (tick > SimulationDuration)
        {
            return SimulationDuration;
        }

        return tick;
    }

    public int GetOutputDuration()
    {
        return GetBoundedTick(FinishTick);
    }
}

public class StatValueData
{
    public readonly float Value;

    public StatValueData(float value)
    {
        Value = value;
    }
}

public class StatPairData
{
    public readonly int First;
    public readonly int Second;

    public StatPairData(int first, int second)
    {
        First = first;
        Second = second;
    }
}
