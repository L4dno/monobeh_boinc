public class StatService : IStatService
{
    public float ActivePower {get; private set;}
    public float IdlePower {get; private set;}

    public int UnfinishedWorkunits {get; private set;}


    public void RegisterClient(IClientStats client)
    {
        client.OnBusyMode += OnBusyMode;
        client.OnIdleMode += OnIdleMode;
        client.OnGoingOnline += OnGoingOnline;
        client.OnGoingOffline += OnGoingOffline;
    }
    public void RegisterProject(IProjectStats project)
    {
        project.OnWorkunitCreated += OnWorkunitCreated;
        project.OnWorkunitCompleted += OnWorkunitCompleted;
    }

    private void OnGoingOffline(string hostName, float power)
    {
        ActivePower -= power;
    }

    private void OnGoingOnline(string hostName, float power)
    {
        ActivePower += power;
    }

    private void OnIdleMode(string hostName, float power)
    {
        IdlePower += power;
    }

    private void OnBusyMode(string hostName, float power)
    {
        IdlePower -= power;
    }

    private void OnWorkunitCompleted()
    {
        UnfinishedWorkunits -= 1;
    }

    private void OnWorkunitCreated()
    {
        UnfinishedWorkunits += 1;
    }
}