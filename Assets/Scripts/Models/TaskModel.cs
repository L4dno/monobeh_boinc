using UnityEngine;
using System.Collections.Generic;



public enum TaskState : byte
{
    Error, // когда превышается один из 3х лимитов и юнит не годен больше
    Valid, // кворум одинаковых успешных результатов
    InProgress, // обычное состояние
}

public class TaskModel
{
    private readonly float _taskGflops;

    private readonly float _taskSizeBytes;

    private readonly List<int> _workunitDeadlineTicks;
    
    //public TaskState CurState {get; private set;}
    public int CurCreatedWorkunits {get; set;} = 0;
    public int CurSentWorkunits {get; set;} = 0;
    public int CurWorkunitsReceived {get; set;} = 0;
    public int CurValidWorkunits {get; set;} = 0;
    public int CurSuccessWorkunits {get; set;} = 0;
    public int CurErrorWorkunits {get; set;} = 0;

    public WorkunitData ReplicateTask(int deadlineTick, short parentId)
    {
        _workunitDeadlineTicks.Add(deadlineTick);
        return new WorkunitData(
            parentId,
            _workunitDeadlineTicks.Count - 1,
            deadlineTick,
            _taskGflops,
            _taskSizeBytes
        );
        // creatre wu
    }
    public TaskModel(TaskConfig config)
    {
        // generate normally between [l;r] from config
        // must be clamped value

        // ParamA = (l+r)/2
        // ParamB = (r-l)/4

        float max = config.MaxTaskGflops;
        float min = config.MinTaskGflops;
        float a = (max + min) / 2;
        float b = (max - min) / 4;

        float value = RandomUtils.GetDistribution(
            config.TaskPowerDistri,a, b);
        
        _taskGflops = Mathf.Clamp(value, min, max);
        _taskSizeBytes = config.InputFileBytes;
        _workunitDeadlineTicks = new List<int>();
    }
}
