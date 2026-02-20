using UnityEngine;

public class Activity : CustomYieldInstruction
{
    private readonly int _targetTick;

    public override bool keepWaiting => TimeTickSystem.Instance.CurTick < _targetTick;

    public Activity(float gflops, HostModel host)
    {

        int ticksToWait = (int)Mathf.Ceil(gflops / host.HostPower);
        _targetTick = TimeTickSystem.Instance.CurTick + ticksToWait;
    }
}