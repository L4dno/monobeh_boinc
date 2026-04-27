using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SimConfig", menuName = "Scriptable Objects/SimConfig")]
public class SimConfig : ScriptableObject
{
    
    [Tooltip("in hours")]
    [FormerlySerializedAs("<SimLength>k__BackingField")]
    public int SimLength  = 120;
    [FormerlySerializedAs("<NumberOfProjects>k__BackingField")]
    public int NumberOfProjects  = 1;

    
    [FormerlySerializedAs("<ProjectConfig>k__BackingField")]
    public ProjectConfig ProjectConfig;

    [FormerlySerializedAs("<NumberOfGroups>k__BackingField")]
    public int NumberOfGroups  = 1;

    
    [FormerlySerializedAs("<GroupConfig>k__BackingField")]
    public GroupConfig GroupConfig;

    
    [Tooltip("Seed for deterministic run")]
    [FormerlySerializedAs("<DeterministicSeed>k__BackingField")]
    public int DeterministicSeed  = 6523446;

    
    [FormerlySerializedAs("<ExperimentFolderName>k__BackingField")]
    public string ExperimentFolderName  = "statistics";
}
