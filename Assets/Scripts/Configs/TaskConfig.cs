using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TaskConfig", menuName = "Scriptable Objects/TaskConfig")]
public class TaskConfig : ScriptableObject
{
    
    [FormerlySerializedAs("<TaskPowerDistri>k__BackingField")]
    public Distribution TaskPowerDistri  = Distribution.Normal;

    
    [FormerlySerializedAs("<MinTaskGflops>k__BackingField")]
    public float MinTaskGflops  = 5040.0f;

    
    [FormerlySerializedAs("<MaxTaskGflops>k__BackingField")]
    public float MaxTaskGflops  = 7040.0f;

    
    [Tooltip("Size in Megabytes (MB)")]
    [FormerlySerializedAs("<InputFileSize>k__BackingField")]
    public float InputFileSize  = 54.6f;

    
    [Tooltip("Size in Megabytes (MB)")]
    [FormerlySerializedAs("<OutputFileSize>k__BackingField")]
    public float OutputFileSize  = 100.0f;

    
    [FormerlySerializedAs("<InitialCreatedWorkunits>k__BackingField")]
    public int InitialCreatedWorkunits  = 2;

     
    [FormerlySerializedAs("<DelayBound>k__BackingField")]
    public int DelayBound  = 1000000000; 

    
    [FormerlySerializedAs("<MinQuorum>k__BackingField")]
    public int MinQuorum  = 2;
    
    
    [FormerlySerializedAs("<MaxCreatedWorkunits>k__BackingField")]
    public int MaxCreatedWorkunits  = 4;
    
    
    [FormerlySerializedAs("<MaxErrorWorkunits>k__BackingField")]
    public int MaxErrorWorkunits  = 2;
    
    
    [FormerlySerializedAs("<MaxSuccessWorkunits>k__BackingField")]
    public int MaxSuccessWorkunits  = 3;
}
