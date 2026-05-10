public class ResultData
{
    public readonly string WorkunitName;
    public readonly int resultNumber;
    public readonly float durationInFlops;
    public readonly float inputByteSize;
    public readonly float outputByteSize;
    public int deadlineTick;

    public ResultData(string workunitName, int resultNumber, 
                        float durationInFlops, float inputByteSize, float outputByteSize, int deadlineTick)
    {
        this.WorkunitName = workunitName;
        this.resultNumber = resultNumber;
        this.durationInFlops = durationInFlops;
        this.inputByteSize = inputByteSize;
        this.outputByteSize = outputByteSize;
        this.deadlineTick = deadlineTick;
    }
}
