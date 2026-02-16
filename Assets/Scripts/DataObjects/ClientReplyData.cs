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


public class ClientReplyData : IMessage
{
    public readonly WorkunitStatus status;
    public readonly WorkunitVerdict verdict;
    public readonly int taskId;
    public readonly int workunitId;
    public readonly int credits;
    public readonly float fileSize;

    public ClientReplyData(WorkunitStatus status, WorkunitVerdict verdict, int taskId, int workunitId, int credits, float fileSize)
    {
        this.status = status;
        this.verdict = verdict;
        this.taskId = taskId;
        this.workunitId = workunitId;
        this.credits = credits;
        this.fileSize = fileSize;
    }

    public float GetByteSize()
    {
        return fileSize;
    }
}