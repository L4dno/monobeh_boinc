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
    public int TargetNumberOfResults {get; private set;} = 10;

    [field: SerializeField]
    public int MaxErrorResults {get; private set;} = 10;

    [field: SerializeField]
    public int MaxTotalResults {get; private set;} = 10;


    [field: SerializeField]
    public int MaxSuccessResults {get; private set;} = 10;

    [field: SerializeField]
    public int SuccessPercentage {get; private set;} = 95;

    [field: SerializeField]
    public int CanonicalPercentage {get; private set;} = 95;
}
