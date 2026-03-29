public interface IStatService {
    void RegisterClient(IClientStats client);
    void RegisterProject(IProjectStats project);
    StatsData GetStats();
}