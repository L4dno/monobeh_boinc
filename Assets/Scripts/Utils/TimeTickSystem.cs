using UnityEngine;
using System;

public class TimeTickSystem : MonoBehaviour
{
    public static TimeTickSystem Instance { get; private set; }
    public static event Action<int> OnTick;
    private const double TICK_DURATION = 0.2d; // 200 ms
    public int CurTick {get; private set;} = 0;
    private double _tickTimer = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Update()
    {
        _tickTimer += Time.deltaTime;

        while (_tickTimer >= TICK_DURATION)
        {
            _tickTimer -= TICK_DURATION;
            CurTick++;
            
            OnTick?.Invoke(CurTick);
            //UnityEngine.Debug.Log($"Tick {CurTick}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnTick = null;
        }
    }
}