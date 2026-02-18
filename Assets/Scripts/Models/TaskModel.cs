public class TaskModel
{
    public string Name { get; }
    private readonly ProjectConfig _config;
    
    public System.Collections.Generic.List<WorkunitData> Workunits = new System.Collections.Generic.List<WorkunitData>();
    private int _workunitsCreated = 0;
    
    public enum State { InProgress, Valid, Error }
    public State CurrentState = State.InProgress;

    public int ValidResults = 0;
    public int ErrorResults = 0;
    public int SuccessResults = 0;
    public int ReceivedResults = 0;


    public TaskModel(string name, ProjectConfig config)
    {
        Name = name;
        _config = config;
    }

    public bool CanCreateMoreWork() => _workunitsCreated < _config.TaskConfig.MaxWorkunits;

    public WorkunitData CreateWorkunit()
    {
        if (!CanCreateMoreWork()) return null;

        var workunit = new WorkunitData(
            Name, 
            _workunitsCreated,
            _config.TaskConfig.JobDuration,
            _config.TaskConfig.InputFileSize,
            TimeTickSystem.Instance.CurTick + _config.DelayBound
        );
        Workunits.Add(workunit);
        _workunitsCreated++;
        return workunit;
    }
}