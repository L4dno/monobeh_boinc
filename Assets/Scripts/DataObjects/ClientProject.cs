using System.Collections.Generic;

// Represents the client's view of a project it's attached to.
public class ClientProject
{
    public string Name { get; }
    public float Priority { get; }
    public string ProjectActorName { get; }
    public ProjectConfig Config { get; }

    public float ShortTermDebt = 0;
    public float LongTermDebt = 0;
    public float WallCpuTime = 0;
    public float Shortfall = 0;
    public bool On = true;
    public bool ExecutorSuspended = true;
    public ClientTaskData RunningTask = null;
    public int TotalTasksChecked = 0;
    public int TotalTasksExecuted = 0;
    public int TotalTasksReceived = 0;
    public int TotalTasksMissed = 0;

    public List<ClientTaskData> Tasks = new List<ClientTaskData>();
    public List<ClientTaskData> RunList = new List<ClientTaskData>();
    public List<ClientTaskData> SimTasks = new List<ClientTaskData>();
    public Queue<ClientTaskData> ReadyTasks = new Queue<ClientTaskData>();
    public Queue<ClientReplyData> CompletedTasks = new Queue<ClientReplyData>();

    public ClientProject(ProjectConfig config)
    {
        Name = config.ProjectName;
        Priority = config.Priority;
        ProjectActorName = config.ProjectActorName;
        Config = config;
    }

    public bool HasRunnableResults()
    {
        return Tasks.Count > 0 || RunList.Count > 0;
    }
}
