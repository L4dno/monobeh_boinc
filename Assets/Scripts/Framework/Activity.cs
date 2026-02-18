using UnityEngine;

public class Activity : CustomYieldInstruction
{
    private readonly int _targetTick;

    public override bool keepWaiting => TimeTickSystem.Instance.CurTick < _targetTick;

    /// <summary>
    /// Creates a computational activity that simulates work being done on a host.
    //  An actor can yield this object in a coroutine to wait for the computation to finish.
    /// </summary>
    /// <param name="gflops">The amount of computation to perform in Giga-flops.</param>
    /// <param name="host">The host on which the computation is running.</param>
    public Activity(float gflops, HostModel host)
    {
        if (host.HostPower <= 0)
        {
            Debug.LogError("Host power must be greater than zero.");
            _targetTick = TimeTickSystem.Instance.CurTick;
            return;
        }

        int ticksToWait = (int)Mathf.Ceil(gflops / host.HostPower);
        _targetTick = TimeTickSystem.Instance.CurTick + ticksToWait;
    }
}