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
    public float InputFileBytes {get; private set;}= 20;

    [field: SerializeField]
    public float OutputFileBytes {get; private set;} = 20;
    
}
