using UnityEngine;

public class LinkModel
{
    public float Latency {get; private set;} 
    public float Bandwidth {get; private set;} 

    public LinkModel(float latency, float bandwidth)
    {
        Latency = latency;
        Bandwidth = bandwidth;
    }
}