using UnityEngine;

[CreateAssetMenu(fileName = "SimConfig", menuName = "Scriptable Objects/SimConfig")]
public class SimConfig : ScriptableObject
{
    [field: SerializeField]
    public int SimLength {get; private set;} = 96;
    public int NumberOfProjects {get; private set;} = 1;

    [field: SerializeField]
    public ProjectConfig ProjectConfig {get; private set;}

    [field: SerializeField]
    public int NumberOfGroups {get; private set;} = 1;

    [field: SerializeField]
    public GroupConfig GroupConfig {get; private set;}

}
