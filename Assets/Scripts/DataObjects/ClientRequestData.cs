// client to server
public class ClientRequestData : IMessage
{
    public readonly float freeHostGflops;
    public readonly int ticksInterval;
    public readonly int actorSender;
}

