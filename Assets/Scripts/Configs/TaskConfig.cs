using UnityEngine;

[CreateAssetMenu(fileName = "TaskConfig", menuName = "Scriptable Objects/TaskConfig")]
public class TaskConfig : ScriptableObject
{
    [field: SerializeField]
    public Distribution TaskPowerDistri { get; private set; } = Distribution.Normal;

    [field: SerializeField]
    public float MinTaskGflops { get; private set; } = 5040.0f;

    [field: SerializeField]
    public float MaxTaskGflops { get; private set; } = 7040.0f;

    [field: SerializeField]
    [Tooltip("Size in Megabytes (MB)")]
    public float InputFileSize { get; private set; } = 54.6f;

    [field: SerializeField]
    [Tooltip("Size in Megabytes (MB)")]
    public float OutputFileSize { get; private set; } = 100.0f;
    
    [field: SerializeField]
    public int MaxWorkunits { get; private set; } = 4;
    
    [field: SerializeField]
    public int MaxErrorWorkunits { get; private set; } = 2;
    
    [field: SerializeField]
    public int MaxSuccessWorkunits { get; private set; } = 3;
}