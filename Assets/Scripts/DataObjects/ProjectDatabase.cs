using System;
using System.Collections.Generic;

public class ProjectDatabase
{
    public event Action<ResultCompletionData> OnResultCompleted;
    public event Action OnTailFinished;
    public ProjectConfig Config;
    public Dictionary<string, WorkunitModel> CurrentWorkunits = new Dictionary<string, WorkunitModel>();
    public Queue<ResultData> CurrentResults = new Queue<ResultData>();
    public Queue<ClientRequestData> ClientRequests = new Queue<ClientRequestData>();
    public Queue<ClientReplyData> CurrentValidations = new Queue<ClientReplyData>();
    public Queue<WorkunitModel> CurrentErrorResults = new Queue<WorkunitModel>();
    public Queue<WorkunitModel> CurrentAssimilations = new Queue<WorkunitModel>();
    public List<ApplicationRuntimeState> Applications = new List<ApplicationRuntimeState>();
    public CoroutineCondition ClientRequestAvailableCondition = new CoroutineCondition();
    public CoroutineCondition ResultAvailableCondition = new CoroutineCondition();
    public CoroutineCondition ResultBufferHasSpaceCondition = new CoroutineCondition();
    public CoroutineCondition ErrorWorkunitAvailableCondition = new CoroutineCondition();
    public CoroutineCondition ValidationReplyAvailableCondition = new CoroutineCondition();
    public CoroutineCondition AssimilationWorkunitAvailableCondition = new CoroutineCondition();
    public int WorkunitsCreated = 0;
    public int ValidWorkunits = 0;
    public int ErrorWorkunits = 0;
    public int NeededQuorumReplicas = 0;
    public HashSet<int> ActiveHostIndexes = new HashSet<int>();
    public HashSet<string> ServerTimedOutResults = new HashSet<string>();
    public Dictionary<int, int> HostSentResults = new Dictionary<int, int>();
    public Dictionary<int, int> HostReturnedResults = new Dictionary<int, int>();
    public Dictionary<int, int> HostValidResults = new Dictionary<int, int>();
    public int[] ApplicationTargets = new int[0];
    public float TheoreticalGflopsBudget = 0;
    public float EffectiveGflopsBudget = 0;

    public void RecordResultCompleted(ResultCompletionData data)
    {
        OnResultCompleted?.Invoke(data);
    }

    public void RecordTailFinished()
    {
        OnTailFinished?.Invoke();
    }

    public void RecordHostSentResults(int hostId, int resultsNumber)
    {
        if (resultsNumber <= 0)
        {
            return;
        }

        AddHostStatistic(HostSentResults, hostId, resultsNumber);
    }

    public void RecordHostReturnedResult(int hostId)
    {
        AddHostStatistic(HostReturnedResults, hostId, 1);
    }

    public void RecordHostValidResult(int hostId)
    {
        AddHostStatistic(HostValidResults, hostId, 1);
    }

    private void AddHostStatistic(Dictionary<int, int> statistics, int hostId, int value)
    {
        if (!statistics.ContainsKey(hostId))
        {
            statistics.Add(hostId, 0);
        }

        statistics[hostId] += value;
    }
}

public class ResultCompletionData
{
    public readonly string WorkunitName;
    public readonly int ResultNumber;
    public readonly string ResultKey;
    public readonly int HostId;
    public readonly int SendTick;
    public readonly int CompletionTick;
    public readonly int DeadlineTick;
    public readonly bool IsValid;
    public readonly bool IsServerTimeout;
    public readonly bool IsExtraResultAfterWorkunitValid;
    public readonly bool IsLearningResult;

    public ResultCompletionData(
        string workunitName,
        int resultNumber,
        string resultKey,
        int hostId,
        int sendTick,
        int completionTick,
        int deadlineTick,
        bool isValid,
        bool isServerTimeout,
        bool isExtraResultAfterWorkunitValid,
        bool isLearningResult)
    {
        WorkunitName = workunitName;
        ResultNumber = resultNumber;
        ResultKey = resultKey;
        HostId = hostId;
        SendTick = sendTick;
        CompletionTick = completionTick;
        DeadlineTick = deadlineTick;
        IsValid = isValid;
        IsServerTimeout = isServerTimeout;
        IsExtraResultAfterWorkunitValid = isExtraResultAfterWorkunitValid;
        IsLearningResult = isLearningResult;
    }
}

public class ApplicationRuntimeState
{
    public int ApplicationIndex;
    public bool IsOn;
    public int SuspendedUntil;
    public int CurrentPeriodWorkunits;
    public float WorkunitsNumber;
    public int SleepTime;
    public int TailTargetWorkunitsTotal;
}
