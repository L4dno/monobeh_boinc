// client to server
public readonly struct ClientRequestData : IMessage
{
    public readonly float freeHostGflops;
    public readonly int ticksInterval;
    public readonly int actorSender;
}

