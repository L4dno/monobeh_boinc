using UnityEngine;

// перегрузка инструкции для синхронизации с таймером на тиках
// служит для зависимости от симуляционного времени

public class WaitForTicks : CustomYieldInstruction
{
    private int _targetTick;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

    public override bool keepWaiting
    {
        get
        {
            return TimeSystem.CurTick < _targetTick;
        }
    }

    public WaitForTicks(int ticksToWait = 1)
    {
        _targetTick = TimeSystem.CurTick + ticksToWait;
    }
}
