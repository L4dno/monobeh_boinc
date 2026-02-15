using UnityEngine;

// перегрузка инструкции для синхронизации с таймером на тиках

public class WaitForTicks : CustomYieldInstruction
{
    // скорость сети и хоста константны
    // так что ждем просто N тиков, лио перезапускаем 
    private int _targetTick;
    //private int _assignedHostId;

    public override bool keepWaiting
    {
        get
        {
            return TimeTickSystem.Instance.CurTick < _targetTick;// ||
            //SimulationManager.Instance._hosts[_assignedHostId].State == HostState.Off;
        }
    }

    public WaitForTicks(int ticksToWait = 1)
    {
        _targetTick = TimeTickSystem.Instance.CurTick + ticksToWait;
    }
}