using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public enum WorkunitGenerationMode
{
    Normal,
    TailBudget
}

[CreateAssetMenu(fileName = "ProjectConfig", menuName = "Scriptable Objects/ProjectConfig")]
public class ProjectConfig : ScriptableObject
{
    
    [FormerlySerializedAs("<ProjectName>k__BackingField")]
    public string ProjectName  = "PROJECT1";

    
    [FormerlySerializedAs("<Priority>k__BackingField")]
    public float Priority  = 1.0f;

    
    [FormerlySerializedAs("<ProjectId>k__BackingField")]
    public int ProjectId  = 0;
    public string ProjectActorName => $"project{ProjectId}";

    
    [FormerlySerializedAs("WorkunitConfigs")]
    public List<ApplicationConfig> ApplicationConfigs;

    
    [FormerlySerializedAs("<ServerPowerGflops>k__BackingField")]
    public float ServerPowerGflops  = 250.0f; 

    public WorkunitGenerationMode GenerationMode = WorkunitGenerationMode.TailBudget;

    public float UtilizationSafety = 0.7f;
}
