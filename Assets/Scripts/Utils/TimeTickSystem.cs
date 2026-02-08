using UnityEngine;
using System;

public class TimeTickSystem : MonoBehaviour
{
    public static event Action<int> OnTick;
    private const double TICK_DURATION = 0.2d; // 200 ms
    private int _curTick;
    private double _tickTimer;

    private void Awake()
    {
        _curTick = 0;
    }
    private void Update()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= TICK_DURATION)
        {
            _tickTimer -= TICK_DURATION;
            _curTick++;
            
            if (OnTick != null) OnTick(_curTick);
            //UnityEngine.Debug.Log($"Tick {_curTick}");
        }
    }
}