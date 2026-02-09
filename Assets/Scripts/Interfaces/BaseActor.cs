using UnityEngine;
using System.Collections.Generic;
using System;

// сущность живущая во времени симуляции. Каждую секунду проверяет состояние.
public abstract class BaseActor
{
    protected readonly Queue<object> _mailBox = new Queue<object>();
    // Аналог помещения в xbt_queue_t очередь сообщений
    // родительский класс для сервера и клиента
    // так же отмечает себя как активного на родительском хосте
    public void Push(object message) => _mailBox.Enqueue(message);

    public BaseActor()
    {
        TimeTickSystem.OnTick += Tick;
    }
    protected abstract void Tick(int curTick);

}