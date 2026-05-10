using UnityEngine;

[CreateAssetMenu(fileName = "WorkunitConfig", menuName = "Scriptable Objects/WorkunitConfig")]
public class WorkunitConfig : ScriptableObject
{
    
    public Distribution WorkunitPowerDistri  = Distribution.Normal;

    
    public float MinWorkunitGflops  = 5040.0f;

    
    public float MaxWorkunitGflops  = 7040.0f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float InputFileSize  = 54.6f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float OutputFileSize  = 100.0f;

    
    public int InitialCreatedResults  = 2;

     
    public int DelayBound  = 1000000000; 

    
    public int MinQuorum  = 2;
    
    
    public int MaxCreatedResults  = 4;
    
    
    public int MaxErrorResults  = 2;
    
    
    public int MaxSuccessResults  = 3;
}
