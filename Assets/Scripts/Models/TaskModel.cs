using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TaskModel
{
    public string Name { get; }
    private readonly ProjectConfig _config;

    private readonly float _taskSizeGflops;
    
    public List<WorkunitData> Workunits = new List<WorkunitData>();
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
        // normal gen of a size of a task

        float mean = (_config.TaskConfig.MinTaskGflops + _config.TaskConfig.MaxTaskGflops) / 2f;
        float stdDev = (_config.TaskConfig.MinTaskGflops - _config.TaskConfig.MaxTaskGflops) / 6f;

        _taskSizeGflops = Mathf.Clamp(
            RandomUtils.GetDistribution(_config.TaskConfig.TaskPowerDistri, mean, stdDev), 
            _config.TaskConfig.MinTaskGflops, 
            _config.TaskConfig.MaxTaskGflops);
    
    }

    public bool CanCreateMoreWork() => _workunitsCreated < _config.TaskConfig.MaxWorkunits;

    public WorkunitData CreateWorkunit()
    {
        if (!CanCreateMoreWork()) return null;

        // init workunit with task power field
        var workunit = new WorkunitData(
            Name, 
            _workunitsCreated,
            _taskSizeGflops,
            _config.TaskConfig.InputFileSize,
            TimeTickSystem.Instance.CurTick + _config.DelayBound
        );
        Workunits.Add(workunit);
        _workunitsCreated++;
        return workunit;
    }
}