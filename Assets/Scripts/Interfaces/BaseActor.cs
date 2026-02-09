using UnityEngine;
using System.Collections.Generic;
using System;

// сущность живущая во времени симуляции. Каждую секунду проверяет состояние.
public abstract class BaseActor
{
    private readonly Queue<object> _mailBox = new Queue<object>();
    // Аналог помещения в xbt_queue_t очередь сообщений
    // родительский класс для сервера и клиента
    // так же отмечает себя как активного на родительском хосте
    public void Push(object message) => _mailBox.Enqueue(message);

    public BaseActor()
    {
        TimeTickSystem.OnTick += Tick;
    }
    protected abstract void Tick(int curTick);

    // public void ProcessMailbox()
    // {
    //     while (_mailbox.Count > 0)
    //     {
    //         var msg = _mailbox.Dequeue();
    //         OnReceive(msg);
    //     }
    // }

    // protected abstract void OnReceive(object message);
}

// // Реализация
// public class FastGuard : FastActor
// {
//     protected override void OnReceive(object message)
//     {
//         // C# Pattern Matching - очень быстро
//         switch (message)
//         {
//             case MoveCmd move:
//                 HandleMove(move);
//                 break;
                
//             case AttackCmd attack:
//                 HandleAttack(attack);
//                 break;
                
//             default:
//                 // unhandled
//                 break;
//         }
//     }
    
//     void HandleMove(MoveCmd cmd) { /* ... */ }
//     void HandleAttack(AttackCmd cmd) { /* ... */ }
// }
