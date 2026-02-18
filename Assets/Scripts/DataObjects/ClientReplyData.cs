public enum WorkunitStatus : byte
{
    Fail,
    Success
}

public enum WorkunitResult : byte
{
    Correct,
    Incorrect
}


public class ClientReplyData : IMessage
{
    public readonly string ClientName;
    public readonly WorkunitStatus status;
    public readonly WorkunitResult result;
    public readonly string WorkunitName;
    public readonly int ResultId;
    public readonly int credits;
    public readonly float fileSize;

    public ClientReplyData(string clientName, WorkunitStatus status, WorkunitResult result, string workunitName, int resultId, int credits, float fileSize)
    {
        this.ClientName = clientName;
        this.status = status;
        this.result = result;
        this.WorkunitName = workunitName;
        this.ResultId = resultId;
        this.credits = credits;
        this.fileSize = fileSize;
    }

    public float GetByteSize()
    {
        return fileSize;
    }
}