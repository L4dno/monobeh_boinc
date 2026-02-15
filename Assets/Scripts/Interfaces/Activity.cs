using System.Collections;
using System;
using UnityEngine;

public interface IMessage
{
}

// базовый класс для обертки корутин
// чтобы был флаг завершенности и полиморфизм
// возможно запоминание состояния выполнения

// public class CoroutineHandler<T> where T : IMessage
// {
//     private Coroutine _timer;
//     private Func<T, IEnumerator> _timerFactory;
//     private T _message;

//     public bool isFinished { get; private set; }

//     public CoroutineHandler(Func<T, IEnumerator> factory)
//     {
//         _timerFactory = factory;
//     }

//     public void Shutdown()
//     {
//         if (_timer != null)
//         {
//             SimulationManager.Instance.StopRoutine(_timer);
//             _timer = null;
//         }
//         isFinished = false;
//     }

//     private IEnumerator WrapRoutine()
//     {
//         yield return _timerFactory(_message);
//         _timer = null;
//         isFinished = true;
//     }

//     public void Process(T message)
//     {
//         Shutdown();
//         _message = message;
//         _timer = SimulationManager.Instance.StartRoutine(WrapRoutine());
//     }

//     public void Resume()
//     {
//         Shutdown();
//         _timer = SimulationManager.Instance.StartRoutine(WrapRoutine());
//     }
// }
