
using UnityEngine;
using System;
using System.Collections.Generic;


public class AssimilatorModel : IActor
{

    private readonly int _hostId;

    private readonly Queue<IMessage> _mailBox;

    public void Push(IMessage message)
    {
        _mailBox.Enqueue(message);
    }

    public void Tick()
    {
        Debug.Log($"Assimilator {_hostId} is called");
    }

    public AssimilatorModel(int hostId)
    {
        _hostId = hostId;
    }
}