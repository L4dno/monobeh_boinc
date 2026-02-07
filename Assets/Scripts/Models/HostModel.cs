using UnityEngine;
using System;


public enum HostState
{
    On,
    Off
}

public class HostModel
{
    private readonly RandomConfig _config;

    const int RANDOM_TO_TICKS_FACTOR = 3600;

    private int _timeToSwitch = 0;

    private HostState _state;
    public HostState State {
        get
        {
            // if (SimulationManager.Instance.CurSimulationTime == _timeToSwitch)
            // {
            //     ChangeState();
            //     Debug.Log($"Host state changed to {_state}");
            // }
            return _state;
        }
        
        private set => _state = value;
        
    }

    public double HostPower {get; private set;} // in gflops setting in constructor

    public HostModel(GroupConfig group)
    {
        _config = group.RandomConfig;
        State = HostState.Off;
        ChangeState();
        double power = RandomUtils.GetDistribution(_config.HostPowerDistri, _config.PowerA, _config.PowerB);
        HostPower = System.Math.Clamp(power, group.MinSpeed, group.MaxSpeed);
    }

    private void ChangeState()
{
    if (State == HostState.Off)
    {
        State = HostState.On;
        _timeToSwitch += (int)System.Math.Ceiling(RANDOM_TO_TICKS_FACTOR * 
            RandomUtils.GetDistribution(_config.HostAvailabilityDistri, 
                                        _config.HostAvailabilityA,
                                        _config.HostAvailabilityB));
    }
    else
    {
        State = HostState.Off;
        _timeToSwitch += (int)System.Math.Ceiling(RANDOM_TO_TICKS_FACTOR * 
            RandomUtils.GetDistribution(_config.HostNonavailabilityDistri, 
                                        _config.HostNonavailabilityA,
                                        _config.HostNonavailabilityB));
    }
}

}
