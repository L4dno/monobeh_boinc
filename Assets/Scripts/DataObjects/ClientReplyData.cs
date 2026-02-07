public class ClientReplyData : IMessage
{
    public IActor Sender {get;}

    public ClientReplyData(IActor sender)
    {
        Sender = sender;
    }

}