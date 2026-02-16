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
    public IEnumerator Push(IMessage message)
    {
        // calculate sending time based on single link
        yield return new WaitForTicks();
        _mailBox.Enqueue(message);
    }
    

    protected int _actorId;
    protected int _hostId;

    public abstract IEnumerator MainLoop(int hostId);

}