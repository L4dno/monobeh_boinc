using UnityEngine;

[CreateAssetMenu(fileName = "ApplicationConfig", menuName = "Scriptable Objects/ApplicationConfig")]
public class ApplicationConfig : ScriptableObject
{
    
    public float ApplicationPercentage = 50.0f;


    // берем
    public float TaskGflops  = 308430.0f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float InputFileSize  = 0.01f;

    
    [Tooltip("Size in Megabytes (MB)")]
    public float OutputFileSize  = 1.0f;

    // берем скок сейчас посланы и не вернулись
    public int InitialCreatedResults  = 2;

    public float WorkunitsNumber = 100000000000.0f;

    public int SleepTime = 0;

     
    // берем
    public int DelayBound  = 1000000; 

    // берем скок осталось
    public int MinQuorum  = 2;
    
    // берем скок осталось
    public int MaxCreatedResults  = 20;
    
    // берем
    public int MaxErrorResults  = 7;
    
    // берем
    public int MaxSuccessResults  = 20;

    public int SuccessPercentage = 90;

    public int CanonicalPercentage = 90;
}
