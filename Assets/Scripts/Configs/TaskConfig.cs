using UnityEngine;

[CreateAssetMenu(fileName = "TaskConfig", menuName = "Scriptable Objects/TaskConfig")]
public class TaskConfig : ScriptableObject
{
    // как быть с ид разными для разных объектов?
    [field: SerializeField]
    public double TaskFlops {get; private set;} = 120d;

    [field: SerializeField]
    public double InputFileBytes {get; private set;}= 20d;

    [field: SerializeField]
    public double OutputFileBytes {get; private set;} = 20d;
    
    
}
