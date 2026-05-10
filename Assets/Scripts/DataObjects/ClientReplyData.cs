public enum ResultStatus : byte
{
    Fail,
    Success
}

public enum ResultValue : byte
{
    Correct,
    Incorrect
}


public class ClientReplyData : IMessage
{
    public readonly string ClientName;
    public readonly ResultStatus status;
    public readonly ResultValue value;
    public readonly string WorkunitName;
    public readonly int ResultNumber;
    public readonly int credits;
    public readonly float fileSize;

    public ClientReplyData(string clientName, ResultStatus status, ResultValue value, string workunitName, int resultNumber, int credits, float fileSize)
    {
        this.ClientName = clientName;
        this.status = status;
        this.value = value;
        this.WorkunitName = workunitName;
        this.ResultNumber = resultNumber;
        this.credits = credits;
        this.fileSize = fileSize;
    }

    public float GetByteSize()
    {
        return fileSize;
    }
}
