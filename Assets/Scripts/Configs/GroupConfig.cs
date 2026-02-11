using UnityEngine;

[CreateAssetMenu(fileName = "GroupConfig", menuName = "Scriptable Objects/GroupConfig")]
public class GroupConfig : ScriptableObject
{
   
    [field: SerializeField]
    public int NumberOfClients {get; private set;} = 10;

    [field: SerializeField]
    public RandomConfig RandomConfig {get; private set;}

    [field: SerializeField]
    public float MaxSpeed {get; private set;} = 117.71f;

    [field: SerializeField]
    public float MinSpeed {get; private set;} = 0.07f; // in GFlops

    [field: SerializeField]
    public int ConnectionInterval {get; private set;} = 20;


    [field: SerializeField]
    public float ServerLatency {get; private set;}

    [field: SerializeField]
    public float ServerBandwidth {get; private set;}

}
