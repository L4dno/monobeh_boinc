using UnityEngine;

public class LinkModel
{
    public float Latency {get; private set;} 
    public float Bandwidth {get; private set;} 

    public LinkModel(float latency, float bandwidth)
    {
        Latency = latency;
        Bandwidth = bandwidth;
        Debug.Log($"Link created with latency {Latency} and bandwidth {Bandwidth}");
    }
}