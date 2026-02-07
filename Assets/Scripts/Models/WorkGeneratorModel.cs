
using UnityEngine;
using System;
using System.Collections.Generic;


public class WorkGeneratorModel : IActor
{

    private readonly int _hostId;

    private readonly Queue<IMessage> _mailBox;

    public void Push(IMessage message)
    {
        _mailBox.Enqueue(message);
    }

    public void Tick()
    {
        Debug.Log($"Work Generator {_hostId} is called");
    }

    public WorkGeneratorModel(int hostId)
    {
        _hostId = hostId;
    }
}