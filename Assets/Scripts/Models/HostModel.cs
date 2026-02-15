using UnityEngine;


public class HostModel
{

    public int HostId { get; private set; }

    public float HostPower {get; private set;} // in gflops setting in sim manager

    public HostModel(float power, int id)
    {
        HostId = id;
        HostPower = power;
    }


}
