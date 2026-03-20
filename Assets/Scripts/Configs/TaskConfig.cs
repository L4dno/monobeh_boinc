using UnityEngine;

[CreateAssetMenu(fileName = "TaskConfig", menuName = "Scriptable Objects/TaskConfig")]
public class TaskConfig : ScriptableObject
{
    
    public Distribution TaskPowerDistri  = Distribution.Normal;

    
    public float MinTaskGflops  = 5040.0f;

    
    public float MaxTaskGflops  = 7040.0f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float InputFileSize  = 54.6f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float OutputFileSize  = 100.0f;

    
    public int InitialCreatedWorkunits  = 2;

     
    public int DelayBound  = 1000000000; 

    
    public int MinQuorum  = 2;
    
    
    public int MaxCreatedWorkunits  = 4;
    
    
    public int MaxErrorWorkunits  = 2;
    
    
    public int MaxSuccessWorkunits  = 3;
}