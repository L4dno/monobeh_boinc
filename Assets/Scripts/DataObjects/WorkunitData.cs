
public readonly struct WorkunitData
{
    public readonly short parentTaskId;
    public readonly short workunitId;

    public readonly int deadlineTick;

    public readonly float durationInFlops;
    public readonly float byteSize;

    public WorkunitData(int parentTaskId, int workunitId, int deadlineTick, 
                        float durationInFlops, float byteSize)
    {
        this.parentTaskId = (short)parentTaskId;
        this.workunitId = (short)workunitId;
        this.deadlineTick = deadlineTick;
        this.durationInFlops = durationInFlops;
        this.byteSize = byteSize;
    }
}