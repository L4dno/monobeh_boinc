using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GroupConfig", menuName = "Scriptable Objects/GroupConfig")]
public class GroupConfig : ScriptableObject
{
    [FormerlySerializedAs("<NumberOfClients>k__BackingField")]
    public int NumberOfClients = 1000;

    
    [FormerlySerializedAs("<RandomConfig>k__BackingField")]
    public RandomConfig RandomConfig ;

    
    [Tooltip("is gflops")]
    [FormerlySerializedAs("<MaxSpeed>k__BackingField")]
    public float MaxSpeed  = 117.71f;

    
    [FormerlySerializedAs("<MinSpeed>k__BackingField")]
    public float MinSpeed  = 0.07f;

    
    [FormerlySerializedAs("<ConnectionInterval>k__BackingField")]
    public int ConnectionInterval  = 150;

    
    [FormerlySerializedAs("<SchedulingInterval>k__BackingField")]
    public int SchedulingInterval  = 150;

    
    [FormerlySerializedAs("<ServerLatency>k__BackingField")]
    public float ServerLatency  = 1.0f;

    
    [Tooltip("Bandwidth in Megabytes per second (MB/s)")]
    [FormerlySerializedAs("<ServerBandwidth>k__BackingField")]
    public float ServerBandwidth  = 1.25f;
}
