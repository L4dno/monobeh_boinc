// client to server
public class ClientRequestData : IMessage
{
    public readonly int actorSender;
    public readonly int groupPower;
    public readonly float power;
    public readonly float percentage;

    public ClientRequestData(int actorSender, int groupPower, float power, float percentage)
    {
        this.actorSender = actorSender;
        this.groupPower = groupPower;
        this.power = power;
        this.percentage = percentage;
    }

    public float GetByteSize()
    {
        return 10240; // 10 KB
    }
}
