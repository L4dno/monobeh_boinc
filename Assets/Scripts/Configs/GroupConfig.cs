using UnityEngine;

[CreateAssetMenu(fileName = "GroupConfig", menuName = "Scriptable Objects/GroupConfig")]
public class GroupConfig : ScriptableObject
{
    [field: SerializeField]
    public int NumberOfClients { get; private set; } = 1000;

    [field: SerializeField]
    public RandomConfig RandomConfig { get; private set; }

    [field: SerializeField]
    [Tooltip("is gflops")]
    public float MaxSpeed { get; private set; } = 117.71f;

    [field: SerializeField]
    public float MinSpeed { get; private set; } = 0.07f;

    [field: SerializeField]
    public int ConnectionInterval { get; private set; } = 150;

    [field: SerializeField]
    public int SchedulingInterval { get; private set; } = 150;

    [field: SerializeField]
    public float ServerLatency { get; private set; } = 1.0f;

    [field: SerializeField]
    [Tooltip("Bandwidth in Megabytes per second (MB/s)")]
    public float ServerBandwidth { get; private set; } = 1.25f;
}