using UnityEngine;
using System.Collections.Generic;
using System;


// должен быть синглтон
public class SchedulerModel : IActor
{

    private readonly int _hostId;

    private readonly Queue<IMessage> _mailBox;

    public void Push(IMessage message)
    {
        _mailBox.Enqueue(message);
    }

    public void Tick()
    {
        Debug.Log($"Scheduler {_hostId} is called");
    }

    //private readonly Dictionary<int, WorkunitModel> Workunits;

    //private readonly Queue<ResultModel> Results;

    public SchedulerModel(int hostId)
    {
        _hostId = hostId;
    }
}
