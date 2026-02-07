using UnityEngine;
using System.Collections.Generic;

public abstract class BaseActor : MonoBehaviour
{
    private readonly Queue<object> _mailbox = new Queue<object>();
    // Аналог помещения в xbt_queue_t очередь сообщений
    // родительский класс для сервера и клиента
    // так же отмечает себя как активного на родительском хосте
    public void Push(object message) => _mailbox.Enqueue(message);

    // вызывается менеджером симуляции каждый тик таймера у всех акторов
    // при нужном состоянии забирает у хоста чуть мощности сам
    //protected virtual void OnTick();

    public void ProcessMailbox()
    {
        while (_mailbox.Count > 0)
        {
            var msg = _mailbox.Dequeue();
            OnReceive(msg);
        }
    }

    protected abstract void OnReceive(object message);
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
