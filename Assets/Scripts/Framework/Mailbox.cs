using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public  class MailBox
{
    private readonly Queue<IMessage> _messageQueue = new Queue<IMessage>();
    private SimulationManager SimManager => Container.Instance.SimManager;

    public IEnumerator Put(IMessage message)
    {
        var link = SimManager.Link;
        var ticksToWait = (int)Mathf.Ceil(link.Latency + message.GetSizeInMegabytes() / link.Bandwidth);

        yield return new WaitForTicks(ticksToWait);
        
        _messageQueue.Enqueue(message);
    }

    public void PutNow(IMessage message)
    {
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

    public T Get<T>() where T : class, IMessage
    {
        T selectedMessage = null;
        int messagesCount = _messageQueue.Count;

        for (int i = 0; i < messagesCount; i++)
        {
            var message = _messageQueue.Dequeue();
            if (selectedMessage == null && message is T typedMessage)
            {
                selectedMessage = typedMessage;
            }
            else
            {
                _messageQueue.Enqueue(message);
            }
        }

        return selectedMessage;
    }

    public bool Empty()
    {
        return _messageQueue.Count == 0;
    }
}
