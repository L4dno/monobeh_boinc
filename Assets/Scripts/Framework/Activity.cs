using System.Collections;
using System;
using UnityEngine;

// базовый класс для обертки корутин
// чтобы был флаг завершенности и полиморфизм
// возможно запоминание состояния выполнения

public class Activity : IEnumerator
{
    public bool IsDone {get; private set;}
    public bool MoveNext() => !IsDone;
    public object Current {get;}
    public void Reset() {}

    public Activity(IEnumerator cor)
    {
        Current = SimulationManager.Instance.StartCoroutine(cor);
    }

    private IEnumerator Wrap(IEnumerator cor)
    {
        yield return cor;
        IsDone = true;
    }
}
