public enum WorkunitStatus : byte
{
    Fail,
    Success
}

public enum WorkunitVerdict : byte
{
    Correct,
    Incorrect
}


public readonly struct ClientReplyData
{
    public readonly WorkunitStatus status;
    public readonly WorkunitVerdict verdict;
    public readonly int taskId;
    public readonly int workunitId;

}