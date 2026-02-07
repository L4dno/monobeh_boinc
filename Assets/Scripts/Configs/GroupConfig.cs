using UnityEngine;

[CreateAssetMenu(fileName = "GroupConfig", menuName = "Scriptable Objects/GroupConfig")]
public class GroupConfig : ScriptableObject
{
   
    [field: SerializeField]
    public int NumberOfClients {get; private set;} = 10;

    [field: SerializeField]
    public RandomConfig RandomConfig {get; private set;}

    [field: SerializeField]
    public double MaxSpeed {get; private set;} = 117.71d;

    [field: SerializeField]
    public double MinSpeed {get; private set;} = 0.07d; // in GFlops

    [field: SerializeField]
    public int ConnectionInterval {get; private set;} = 20;


    [field: SerializeField]
    public double ServerLatency {get; private set;}

    [field: SerializeField]
    public double ServerBandwidth {get; private set;}

}
