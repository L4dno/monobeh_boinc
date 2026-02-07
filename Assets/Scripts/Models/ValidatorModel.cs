
using UnityEngine;
using System;
using System.Collections.Generic;


public class ValidatorModel : IActor
{

    private readonly int _hostId;

    private readonly Queue<IMessage> _mailBox;

    public void Push(IMessage message)
    {
        _mailBox.Enqueue(message);
    }

    public void Tick()
    {
        Debug.Log($"Validator {_hostId} is called");
    }

    public ValidatorModel(int hostId)
    {
        _hostId = hostId;
    }
}