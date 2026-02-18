using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public  class MailBox
{
    private readonly Queue<IMessage> _messageQueue = new Queue<IMessage>();

    public IEnumerator Put(IMessage message)
    {
        var link = SimulationManager.Instance.Link;
        var ticksToWait = (int)Mathf.Ceil(link.Latency + message.GetByteSize() / link.Bandwidth);

        yield return new WaitForTicks(ticksToWait);
        
        _messageQueue.Enqueue(message);
    }

    public IMessage Get()
    {
        if (_messageQueue.Count == 0)
        {
            return null;
        }
        return _messageQueue.Dequeue();
    }

    public bool Empty()
    {
        return _messageQueue.Count == 0;
    }
}