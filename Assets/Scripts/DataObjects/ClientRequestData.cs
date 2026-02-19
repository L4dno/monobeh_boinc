public class ClientRequestData : IMessage
{
    public readonly string RequesterName;
    public readonly int Power;
    public readonly float Percentage;

    public ClientRequestData(string requesterName, int power, float percentage)
    {
        RequesterName = requesterName;
        Power = power;
        Percentage = percentage;
    }

    public float GetByteSize()
    {
        return 10240; // 10 KB
    }
}

