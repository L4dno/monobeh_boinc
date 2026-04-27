using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ProjectConfig", menuName = "Scriptable Objects/ProjectConfig")]
public class ProjectConfig : ScriptableObject
{
    
    [FormerlySerializedAs("<ProjectName>k__BackingField")]
    public string ProjectName  = "ATLAS@home";

    
    [FormerlySerializedAs("<Priority>k__BackingField")]
    public float Priority  = 1.0f;

    
    [FormerlySerializedAs("<ProjectId>k__BackingField")]
    public int ProjectId  = 0;

    
    [FormerlySerializedAs("<TaskConfigs>k__BackingField")]
    public List<TaskConfig> TaskConfigs;

    
    [FormerlySerializedAs("<ServerPowerGflops>k__BackingField")]
    public float ServerPowerGflops  = 12.0f; 


    
    [FormerlySerializedAs("<SuccessPercentage>k__BackingField")]
    public int SuccessPercentage  = 95;

    
    [FormerlySerializedAs("<CanonicalPercentage>k__BackingField")]
    public int CanonicalPercentage  = 95;
}
