using UnityEngine;

[CreateAssetMenu(fileName = "TaskConfig", menuName = "Scriptable Objects/TaskConfig")]
public class TaskConfig : ScriptableObject
{
    [field: SerializeField]
    public Distribution TaskPowerDistri {get; private set;} = Distribution.Normal;

    [field: SerializeField]
    public double MinTaskGflops {get; private set;} = 0.5;

    [field: SerializeField]
    public double MaxTaskGflops {get; private set;} = 0.7;

    [field: SerializeField]
    public double InputFileBytes {get; private set;}= 20d;

    [field: SerializeField]
    public double OutputFileBytes {get; private set;} = 20d;
    
    
}
