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
    private readonly int _taskId;

    private readonly float _taskSizeBytes;

    private readonly List<int> _workunitDeadlineTicks;
    
    public TaskState CurState {get; set;}
    public int CurCreatedWorkunits {get; set;} = 0;
    public int CurWorkunitsReceived {get; set;} = 0;
    public int CurValidWorkunits {get; set;} = 0;
    public int CurSuccessWorkunits {get; set;} = 0;
    public int CurErrorWorkunits {get; set;} = 0;
    public int CurWorkunitsRecreated {get; set;} = 0;

    public bool isWorkunitInTime(int curTick, int wuId)
    {
        return curTick < _workunitDeadlineTicks[wuId];
    }

    public WorkunitData ReplicateTask(int deadlineTick)
    {
        _workunitDeadlineTicks.Add(deadlineTick);
        // increment tasks sent
        return new WorkunitData(
            _taskId,
            _workunitDeadlineTicks.Count - 1,
            deadlineTick,
            _taskGflops,
            _taskSizeBytes
        );
        // creatre wu
    }
    public TaskModel(TaskConfig config, int taskId)
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
        _taskId = taskId;
    }
}
