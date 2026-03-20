using UnityEngine;

[CreateAssetMenu(fileName = "GroupConfig", menuName = "Scriptable Objects/GroupConfig")]
public class GroupConfig : ScriptableObject
{
    public int NumberOfClients = 1000;

    
    public RandomConfig RandomConfig ;

    
    [Tooltip("is gflops")]
    public float MaxSpeed  = 117.71f;

    
    public float MinSpeed  = 0.07f;

    
    public int ConnectionInterval  = 150;

    
    public int SchedulingInterval  = 150;

    
    public float ServerLatency  = 1.0f;

    
    [Tooltip("Bandwidth in Megabytes per second (MB/s)")]
    public float ServerBandwidth  = 1.25f;
}