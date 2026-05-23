public class ResultData
{
    public readonly string WorkunitName;
    public readonly int resultNumber;
    public readonly int ApplicationIndex;
    public readonly float durationInGflops;
    public readonly float inputFileSizeMb;
    public readonly float outputFileSizeMb;
    public readonly int CreatedTick;
    public int sentTick;
    public int sentHostId;
    public int deadlineTick;
    public float remainingDurationInGflops;
    public int executionStartTick;
    public bool isRunning;
    public bool isSent;
    public bool isValidationCompleted;
    public bool isServerTimedOut;
    public bool isLearningResult;

    public ResultData(string workunitName, int resultNumber,
                        float durationInGflops, float inputFileSizeMb, float outputFileSizeMb, int deadlineTick)
        : this(workunitName, resultNumber, 0, durationInGflops, inputFileSizeMb, outputFileSizeMb, 0, deadlineTick)
    {
    }

    public ResultData(string workunitName, int resultNumber, int applicationIndex,
                        float durationInGflops, float inputFileSizeMb, float outputFileSizeMb, int createdTick, int deadlineTick)
    {
        this.WorkunitName = workunitName;
        this.resultNumber = resultNumber;
        this.ApplicationIndex = applicationIndex;
        this.durationInGflops = durationInGflops;
        this.inputFileSizeMb = inputFileSizeMb;
        this.outputFileSizeMb = outputFileSizeMb;
        this.CreatedTick = createdTick;
        this.sentTick = 0;
        this.sentHostId = -1;
        this.deadlineTick = deadlineTick;
        this.remainingDurationInGflops = durationInGflops;
        this.executionStartTick = 0;
        this.isRunning = false;
        this.isSent = false;
        this.isValidationCompleted = false;
        this.isServerTimedOut = false;
        this.isLearningResult = false;
    }
}
