using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// сущность живущая во времени симуляции. Каждую секунду проверяет состояние.
public abstract class BaseActor
{
    protected readonly Queue<object> _mailBox = new Queue<object>();
    // Аналог помещения в xbt_queue_t очередь сообщений
    // родительский класс для сервера и клиента
    // так же отмечает себя как активного на родительском хосте
    public void Push(object message) => _mailBox.Enqueue(message);

    protected int _actorId;
    protected int _hostId;

    public abstract IEnumerator MainLoop(int hostId);

}