public readonly struct ServerReplyData
{
    public readonly WorkunitData workunit;
    public readonly int deadlineTick;

    public ServerReplyData(WorkunitData workunit, int deadlineTick)
    {
        this.workunit = workunit;
        this.deadlineTick = deadlineTick;
    }
}