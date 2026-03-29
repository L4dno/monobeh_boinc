public class StatService : IStatService
{
    private readonly StatsData data = new StatsData();

    public StatsData GetStats()
    {
        // Возвращаем копию, чтобы вызывающий код не мог изменить состояние сервиса
        return new StatsData
        {
            ActivePower = data.ActivePower,
            IdlePower = data.IdlePower,
            UnfinishedWorkunits = data.UnfinishedWorkunits
        };
    }

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
        data.ActivePower -= power;
    }

    private void OnGoingOnline(string hostName, float power)
    {
        data.ActivePower += power;
    }

    private void OnIdleMode(string hostName, float power)
    {
        data.IdlePower += power;
    }

    private void OnBusyMode(string hostName, float power)
    {
        data.IdlePower -= power;
    }

    private void OnWorkunitCompleted()
    {
        data.UnfinishedWorkunits -= 1;
    }

    private void OnWorkunitCreated()
    {
        data.UnfinishedWorkunits += 1;
    }
}