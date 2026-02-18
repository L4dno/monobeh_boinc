using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public  class MailBox
{
    // высчитывает задержку между получателем и отправителем

    public IEnumerator Put(IMessage message)
    {
        var link = SimulationManager.Instance.Link;
        var ticksToWait = (int)Mathf.Ceil(link.Latency + message.GetByteSize() / link.Bandwidth);

        
        
        _mailBox.Enqueue(message);
    }
    public IMessage Get()
    {
        return _mailBox.Dequeue();
    }

    public bool Empty()
    {
        return _mailBox.Count == 0;
    }

    private readonly Queue<IMessage> _mailBox = new Queue<IMessage>();
    // Аналог помещения в xbt_queue_t очередь сообщений
    // родительский класс для сервера и клиента
    // так же отмечает себя как активного на родительском хосте

}