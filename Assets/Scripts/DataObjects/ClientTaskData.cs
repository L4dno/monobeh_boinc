public class ClientTaskData
{
    public readonly string WorkunitName;
    public readonly int ResultNumber;
    public readonly int ServerResultNumber;
    public readonly int ApplicationIndex;
    public readonly float DurationInGflops;
    public readonly float OutputFileSizeMb;
    public int StartTick;
    public int DeadlineDuration;
    public int DeadlineTick;
    public float RemainingDurationInGflops;
    public float SimFinish;
    public float SimRemainingDurationInGflops;
    public int ExecutionStartTick;
    public bool Scheduled;
    public bool Running;
    public ClientProject Project;

    public ClientTaskData(
        string workunitName,
        int resultNumber,
        int serverResultNumber,
        int applicationIndex,
        float durationInGflops,
        float outputFileSizeMb,
        int startTick,
        int deadlineDuration)
    {
        WorkunitName = workunitName;
        ResultNumber = resultNumber;
        ServerResultNumber = serverResultNumber;
        ApplicationIndex = applicationIndex;
        DurationInGflops = durationInGflops;
        OutputFileSizeMb = outputFileSizeMb;
        StartTick = startTick;
        DeadlineDuration = deadlineDuration;
        DeadlineTick = startTick + deadlineDuration;
        RemainingDurationInGflops = durationInGflops;
        SimFinish = 0;
        SimRemainingDurationInGflops = durationInGflops;
        ExecutionStartTick = 0;
        Scheduled = false;
        Running = false;
        Project = null;
    }

    public float GetRemainingDuration(float hostPower)
    {
        return RemainingDurationInGflops / hostPower;
    }
}
