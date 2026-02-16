using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// сущность живущая во времени симуляции. Каждую секунду проверяет состояние.
public abstract class BaseActor
{
    protected readonly Queue<IMessage> _mailBox = new Queue<IMessage>();
    // Аналог помещения в xbt_queue_t очередь сообщений
    // родительский класс для сервера и клиента
    // так же отмечает себя как активного на родительском хосте
    public IEnumerator Push(IMessage message, LinkModel link)
    {
        var ticksToWait = (int)Mathf.Ceil(link.Latency + message.GetByteSize() / link.Bandwidth);
        if (ticksToWait > 0)
        {
            yield return new WaitForTicks(ticksToWait);
        }
        
        _mailBox.Enqueue(message);
    }
    

    protected int _actorId;
    protected int _hostId;

    public abstract IEnumerator MainLoop(int hostId);

}