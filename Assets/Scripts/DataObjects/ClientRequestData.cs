// client to server
public class ClientRequestData : IMessage
{
    public IActor Sender {get;}

    public ClientRequestData(IActor sender)
    {
        Sender = sender;
    }
}

