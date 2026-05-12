using System.Collections.Generic;

public class StatsData
{
    public int SimulationDuration;
    public string ProjectName;
    public int NumberOfClients;
    public int TailStageActive;
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
