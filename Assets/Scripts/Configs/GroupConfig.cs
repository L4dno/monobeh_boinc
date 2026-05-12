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
    public float MaxSpeed  = 337.0f;

    
    [FormerlySerializedAs("<MinSpeed>k__BackingField")]
    public float MinSpeed  = 3.0f;

    
    [FormerlySerializedAs("<ConnectionInterval>k__BackingField")]
    public int ConnectionInterval  = 1;

    
    [FormerlySerializedAs("<SchedulingInterval>k__BackingField")]
    public int SchedulingInterval  = 3600;

    
    [FormerlySerializedAs("<ServerLatency>k__BackingField")]
    public float ServerLatency  = 0.02f;

    
    [Tooltip("Bandwidth in Megabytes per second (MB/s)")]
    [FormerlySerializedAs("<ServerBandwidth>k__BackingField")]
    public float ServerBandwidth  = 125.0f;

    public float TailMeanSpeed = 53.651965f;

    public float TailAvailabilityPercent = 55.3f;
}
