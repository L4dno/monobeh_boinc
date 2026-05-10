using UnityEngine;
using System;

public class TimeTickSystem : MonoBehaviour
{
    public event Action<int> OnTick;
    private const float TICK_DURATION = 0.0002f; // ms
    public int CurTick {get; private set;} = 0;
    private float _tickTimer = 0;

    private void Update()
    {
        _tickTimer += Time.deltaTime;

        while (_tickTimer >= TICK_DURATION)
        {
            _tickTimer -= TICK_DURATION;
            CurTick++;
            
            OnTick?.Invoke(CurTick);
        }
    }

}