public enum ResultStatus
{
    Fail,
    Success
}

public enum ResultValue
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
    public readonly float fileSizeMb;

    public ClientReplyData(string clientName, ResultStatus status, ResultValue value, string workunitName, int resultNumber, int credits, float fileSizeMb)
    {
        this.ClientName = clientName;
        this.status = status;
        this.value = value;
        this.WorkunitName = workunitName;
        this.ResultNumber = resultNumber;
        this.credits = credits;
        this.fileSizeMb = fileSizeMb;
    }

    public float GetSizeInMegabytes()
    {
        return 0.01f + fileSizeMb;
    }
}
