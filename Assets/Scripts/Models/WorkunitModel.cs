using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorkunitModel
{
    public string Name { get; }
    public int ApplicationIndex { get; }
    public int CreatedTick { get; }
    public int DelayBound => _config.DelayBound;
    public int MinQuorum => _config.MinQuorum;
    public int TotalResults => _resultsCreated;

    public ApplicationConfig Config => _config;
    private readonly ApplicationConfig _config;

    public List<ResultData> Results = new List<ResultData>();
    public Queue<int> SentResultNumbers = new Queue<int>();
    private int _resultsCreated = 0;
    
    public enum State { InProgress, Valid, Error }
    public State CurrentState = State.InProgress;

    public int ValidResults = 0;
    public int ErrorResults = 0;
    public int SuccessResults = 0;
    public int ReceivedResults = 0;
    public int SentResults = 0;
    public int CurrentErrorResults = 0;
    public int Credits = -1;
    public bool QueuedForAssimilation = false;


    public WorkunitModel(string name, ApplicationConfig config)
        : this(name, config, 0, 0)
    {
    }

    public WorkunitModel(string name, ApplicationConfig config, int applicationIndex, int createdTick)
    {
        Name = name;
        ApplicationIndex = applicationIndex;
        CreatedTick = createdTick;
        _config = config;
        // normal gen of a size of a workunit
    }

    public bool CanCreateInitialResults() => _resultsCreated < _config.InitialCreatedResults;
    public bool CanCreateMoreResults() => _resultsCreated < _config.MaxCreatedResults;

    public ResultData CreateResult()
    {
        return CreateResult(0);
    }

    public ResultData CreateResult(int createdTick)
    {
        if (!CanCreateMoreResults()) return null;

        // init result with workunit power field
        var result = new ResultData(
            Name, 
            _resultsCreated,
            ApplicationIndex,
            _config.TaskGflops,
            _config.InputFileSize,
            _config.OutputFileSize,
            createdTick,
            createdTick + _config.DelayBound
        );
        Results.Add(result);
        _resultsCreated++;
        return result;
    }
}
