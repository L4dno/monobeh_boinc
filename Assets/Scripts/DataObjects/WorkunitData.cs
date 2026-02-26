public class WorkunitData
{
    public readonly string ParentTaskName;
    public readonly int workunitId;
    public readonly float durationInFlops;
    public readonly float inputByteSize;
    public readonly float outputByteSize;
    public int deadlineTick;

    public WorkunitData(string parentTaskName, int workunitId, 
                        float durationInFlops, float inputByteSize, float outputByteSize, int deadlineTick)
    {
        this.ParentTaskName = parentTaskName;
        this.workunitId = workunitId;
        this.durationInFlops = durationInFlops;
        this.inputByteSize = inputByteSize;
        this.outputByteSize = outputByteSize;
        this.deadlineTick = deadlineTick;
    }
}