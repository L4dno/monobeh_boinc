using UnityEngine;

[CreateAssetMenu(fileName = "ProjectConfig", menuName = "Scriptable Objects/ProjectConfig")]
public class ProjectConfig : ScriptableObject
{
    [field: SerializeField]
    public string ProjectName { get; private set; } = "ATLAS@home";

    [field: SerializeField]
    public float Priority { get; private set; } = 1.0f;

    [field: SerializeField]
    public int ProjectId { get; private set; } = 0;

    [field: SerializeField]
    public TaskConfig TaskConfig { get; private set; }

    [field: SerializeField]
    public float ServerPowerGflops { get; private set; } = 12.0f; 

    [field: SerializeField]
    public int DelayBound { get; private set; } = 1000000000; 

    [field: SerializeField]
    public int MinQuorum { get; private set; } = 2;

    [field: SerializeField]
    public int InitialTaskCount { get; private set; } = 3000; 

    [field: SerializeField]
    public int SuccessPercentage { get; private set; } = 95;

    [field: SerializeField]
    public int CanonicalPercentage { get; private set; } = 95;
}