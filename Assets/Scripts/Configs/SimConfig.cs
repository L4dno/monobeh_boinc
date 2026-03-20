using UnityEngine;

[CreateAssetMenu(fileName = "SimConfig", menuName = "Scriptable Objects/SimConfig")]
public class SimConfig : ScriptableObject
{
    
    [Tooltip("in hours")]
    public int SimLength  = 120;
    public int NumberOfProjects  = 1;

    
    public ProjectConfig ProjectConfig;

    public int NumberOfGroups  = 1;

    
    public GroupConfig GroupConfig;

    
    [Tooltip("Seed for deterministic run")]
    public int DeterministicSeed  = 6523446;

    
    public string ExperimentFolderName  = "statistics";
}
