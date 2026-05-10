using System.Collections.Generic;

// Represents the client's view of a project it's attached to.
public class ClientProject
{
    public string Name { get; }
    public float Priority { get; }
    public string ProjectActorName { get; }
    public ProjectConfig Config { get; }

    public double ShortTermDebt = 0;
    public double LongTermDebt = 0;
    public double WallCpuTime = 0;
    public double Shortfall = 0;

    public Queue<ResultData> AvailableResults = new Queue<ResultData>();
    public Queue<ResultData> ReadyToExecuteResults = new Queue<ResultData>();
    public Queue<ClientReplyData> CompletedResults = new Queue<ClientReplyData>();
    public List<ResultData> InProgressResults = new List<ResultData>();

    public ClientProject(ProjectConfig config)
    {
        Name = config.ProjectName;
        Priority = config.Priority;
        ProjectActorName = $"project{config.ProjectId}";
        Config = config;
    }
}
