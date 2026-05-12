using UnityEngine;

public class Activity : CustomYieldInstruction
{
    private readonly int _targetTick;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

    public override bool keepWaiting => TimeSystem.CurTick < _targetTick;

    public Activity(float gflops, HostModel host)
    {
        int ticksToWait = (int)Mathf.Ceil(gflops / host.HostPower);
        _targetTick = TimeSystem.CurTick + ticksToWait;
    }
}
