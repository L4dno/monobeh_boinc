using UnityEngine;

[CreateAssetMenu(fileName = "SimConfig", menuName = "Scriptable Objects/SimConfig")]
public class SimConfig : ScriptableObject
{
    [field: SerializeField]
    [Tooltip("in hours")]
    public int SimLength {get; private set;} = 100;
    public int NumberOfProjects {get; private set;} = 1;

    [field: SerializeField]
    public ProjectConfig ProjectConfig {get; private set;}

    public int NumberOfGroups {get; private set;} = 1;

    [field: SerializeField]
    public GroupConfig GroupConfig {get; private set;}

    [field: SerializeField]
    [Tooltip("Seed for deterministic run")]
    public int DeterministicSeed {get; private set;} = 6523446;

    [field: SerializeField]
    public string StatisticsFileName { get; private set; } = "statistics";
}
