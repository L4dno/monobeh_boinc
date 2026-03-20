using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ProjectConfig", menuName = "Scriptable Objects/ProjectConfig")]
public class ProjectConfig : ScriptableObject
{
    
    public string ProjectName  = "ATLAS@home";

    
    public float Priority  = 1.0f;

    
    public int ProjectId  = 0;

    
    public List<TaskConfig> TaskConfigs;

    
    public float ServerPowerGflops  = 12.0f; 


    
    public int SuccessPercentage  = 95;

    
    public int CanonicalPercentage  = 95;
}