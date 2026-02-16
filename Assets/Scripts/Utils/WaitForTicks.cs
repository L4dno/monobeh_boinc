using UnityEngine;

// перегрузка инструкции для синхронизации с таймером на тиках

public class WaitForTicks : CustomYieldInstruction
{
    private int _targetTick;

    public override bool keepWaiting
    {
        get
        {
            return TimeTickSystem.Instance.CurTick < _targetTick;
        }
    }

    public WaitForTicks(int ticksToWait = 1)
    {
        _targetTick = TimeTickSystem.Instance.CurTick + ticksToWait;
    }
}