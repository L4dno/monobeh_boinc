using UnityEngine;

[CreateAssetMenu(fileName = "ApplicationConfig", menuName = "Scriptable Objects/ApplicationConfig")]
public class ApplicationConfig : ScriptableObject
{
    public float ApplicationPercentage = 50.0f;

    public float TaskGflops  = 308430.0f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float InputFileSize  = 0.01f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float OutputFileSize  = 1.0f;

    
    public int InitialCreatedResults  = 2;

    public float WorkunitsNumber = 100000000000.0f;

    public int SleepTime = 0;

     
    public int DelayBound  = 1000000; 

    
    public int MinQuorum  = 2;
    
    
    public int MaxCreatedResults  = 20;
    
    
    public int MaxErrorResults  = 7;
    
    
    public int MaxSuccessResults  = 20;

    public int SuccessPercentage = 90;

    public int CanonicalPercentage = 90;
}
