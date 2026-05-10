using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorkunitModel
{
    public string Name { get; }
    public int DelayBound => _config.DelayBound;
    public int MinQuorum => _config.MinQuorum;

    public WorkunitConfig Config => _config;
    private readonly WorkunitConfig _config;

    private readonly float _workunitSizeGflops;
    
    public List<ResultData> Results = new List<ResultData>();
    private int _resultsCreated = 0;
    
    public enum State { InProgress, Valid, Error }
    public State CurrentState = State.InProgress;

    public int ValidResults = 0;
    public int ErrorResults = 0;
    public int SuccessResults = 0;
    public int ReceivedResults = 0;


    public WorkunitModel(string name, WorkunitConfig config)
    {
        Name = name;
        _config = config;
        // normal gen of a size of a workunit

        float mean = (_config.MinWorkunitGflops + _config.MaxWorkunitGflops) / 2f;
        float stdDev = (_config.MaxWorkunitGflops - _config.MinWorkunitGflops) / 6f;

        _workunitSizeGflops = Mathf.Clamp(
            RandomUtils.GetDistribution(_config.WorkunitPowerDistri, mean, stdDev), 
            _config.MinWorkunitGflops, 
            _config.MaxWorkunitGflops);
    
    }

    public bool CanCreateInitialResults() => _resultsCreated < _config.InitialCreatedResults;
    public bool CanCreateMoreResults() => _resultsCreated < _config.MaxCreatedResults;

    public ResultData CreateResult()
    {
        if (!CanCreateMoreResults()) return null;

        // init result with workunit power field
        var result = new ResultData(
            Name, 
            _resultsCreated,
            _workunitSizeGflops,
            _config.InputFileSize,
            _config.OutputFileSize,
            0
        );
        Results.Add(result);
        _resultsCreated++;
        return result;
    }
}
