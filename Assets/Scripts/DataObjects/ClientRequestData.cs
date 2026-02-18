public class ClientRequestData : IMessage
{
    public readonly string RequesterName;

    public ClientRequestData(string requesterName)
    {
        RequesterName = requesterName;
    }

    public float GetByteSize()
    {
        return 10240; // 10 KB
    }
}

