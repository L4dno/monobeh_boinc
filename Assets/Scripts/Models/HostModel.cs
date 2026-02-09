using UnityEngine;
using System;


public enum HostState
{
    On,
    Off
}

public class HostModel
{
    private RandomConfig _config;

    const int RANDOM_TO_TICKS_FACTOR = 3600;

    private int _tickToSwitch = 0;

    private HostState _state;
    public HostState State {
        get
        {
            if (TimeTickSystem.Instance.CurTick >= _tickToSwitch)
            {
                ChangeState();
                Debug.Log($"Host state changed to {_state}");
            }
            return _state;
        }
        
        private set => _state = value;
        
    }

    public double HostPower {get; private set;} // in gflops setting in constructor

    public HostModel(GroupConfig group)
    {
        _config = group.RandomConfig;
        _state = HostState.Off;
        ChangeState();
        double power = RandomUtils.GetDistribution(_config.HostPowerDistri, _config.PowerA, _config.PowerB);
        HostPower = System.Math.Clamp(power, group.MinSpeed, group.MaxSpeed);
    }

    public HostModel(ProjectConfig project)
    {
        _state = HostState.On;
        HostPower = project.ServerPowerGflops;
    }

    private void ChangeState()
{
    if (_state == HostState.Off)
    {
        _state = HostState.On;
        _tickToSwitch += (int)System.Math.Ceiling(RANDOM_TO_TICKS_FACTOR * 
            RandomUtils.GetDistribution(_config.HostAvailabilityDistri, 
                                        _config.HostAvailabilityA,
                                        _config.HostAvailabilityB));
    }
    else
    {
        _state = HostState.Off;
        _tickToSwitch += (int)System.Math.Ceiling(RANDOM_TO_TICKS_FACTOR * 
            RandomUtils.GetDistribution(_config.HostNonavailabilityDistri, 
                                        _config.HostNonavailabilityA,
                                        _config.HostNonavailabilityB));
    }
}

}
