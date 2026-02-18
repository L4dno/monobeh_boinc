public class WorkunitData
{
    public readonly string ParentTaskName;
    public readonly int workunitId;
    public readonly float durationInFlops;
    public readonly float byteSize;
    public readonly int deadlineTick;

    public WorkunitData(string parentTaskName, int workunitId, 
                        float durationInFlops, float byteSize, int deadlineTick)
    {
        this.ParentTaskName = parentTaskName;
        this.workunitId = workunitId;
        this.durationInFlops = durationInFlops;
        this.byteSize = byteSize;
        this.deadlineTick = deadlineTick;
    }
}