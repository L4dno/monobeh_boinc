public class WorkunitData
{
    public readonly int parentTaskId;
    public readonly int workunitId;
    public readonly float durationInFlops;
    public readonly float byteSize;
    public readonly int deadlineTick;

    public WorkunitData(int parentTaskId, int workunitId, 
                        float durationInFlops, float byteSize, int deadlineTick)
    {
        this.parentTaskId = parentTaskId;
        this.workunitId = workunitId;
        this.durationInFlops = durationInFlops;
        this.byteSize = byteSize;
        this.deadlineTick = deadlineTick;
    }
}