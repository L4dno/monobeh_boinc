using UnityEngine;

[CreateAssetMenu(fileName = "TaskConfig", menuName = "Scriptable Objects/TaskConfig")]
public class TaskConfig : ScriptableObject
{
    [field: SerializeField]
    public Distribution TaskPowerDistri {get; private set;} = Distribution.Normal;

    [field: SerializeField]
    public float MinTaskGflops {get; private set;} = 0.5f;

    [field: SerializeField]
    public float MaxTaskGflops {get; private set;} = 0.7f;

    [field: SerializeField]
    public float InputFileSize {get; private set;}= 20;

    [field: SerializeField]
    public float OutputFileSize {get; private set;} = 20;
    
    [field: SerializeField]
    public int MaxWorkunits {get; private set;} = 10;
    
    [field: SerializeField]
    public int MaxErrorWorkunits {get; private set;} = 4;
    
    [field: SerializeField]
    public int MaxSuccessWorkunits {get; private set;} = 6;
}
