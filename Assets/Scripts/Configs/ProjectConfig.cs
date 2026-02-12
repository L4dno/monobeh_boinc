using UnityEngine;

[CreateAssetMenu(fileName = "ProjectConfig", menuName = "Scriptable Objects/ProjectConfig")]
public class ProjectConfig : ScriptableObject
{

    [field: SerializeField]
    public TaskConfig TaskConfig {get;}

    [field: SerializeField]
    public float ServerPowerGflops { get; private set; }

    [field: SerializeField]
    public int DelayBound {get; private set;} = 20;

    [field: SerializeField]
    public int MinQuorum {get; private set;} = 2;

    [field: SerializeField]
    public int InitialTaskCount {get; private set;} = 3000;

    [field: SerializeField]
    public int TargetCountOfWorkunits {get; private set;} = 10;

    [field: SerializeField]
    public int MaxErrorWorkunits {get; private set;} = 10;

    [field: SerializeField]
    public int MaxCreatedWorkunits {get; private set;} = 10;


    [field: SerializeField]
    public int MaxSuccessWorkunits {get; private set;} = 10;

    [field: SerializeField]
    public int SuccessPercentage {get; private set;} = 95;

    [field: SerializeField]
    public int CanonicalPercentage {get; private set;} = 95;
}
