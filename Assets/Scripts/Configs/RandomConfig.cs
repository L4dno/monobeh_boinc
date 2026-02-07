using UnityEngine;

[CreateAssetMenu(fileName = "RandomConfig", menuName = "Scriptable Objects/RandomConfig")]
public class RandomConfig : ScriptableObject
{
    [field: SerializeField]
    [Tooltip("Seed for deterministic run")]
    public int DeterministicSeed {get; private set;} = 6523446;

    [Header("Host Parameters")]
    [Tooltip("Speed fit distribution")]
    [field: SerializeField]
    public Distribution HostPowerDistri {get; private set;} = Distribution.Exponential;

    [Tooltip("A parameter for speed distribution")]
    [field: SerializeField]
    public double PowerA {get; private set;} = 0.1734d;

    [Tooltip("B parameter for speed distribution")]
    [field: SerializeField]
    public double PowerB {get; private set;} = -1d;

    [Header("Host Availability")]
    [Tooltip("Availability fit distribution")]
    [field: SerializeField]
    public Distribution HostAvailabilityDistri {get; private set;} = Distribution.Weibull;

    [Tooltip("A parameter for availability distribution")]
    [field: SerializeField]
    public double HostAvailabilityA {get; private set;} = 0.393d;

    [Tooltip("B parameter for availability distribution")]
    [field: SerializeField]
    public double HostAvailabilityB {get; private set;} = 2.964d;

    [Header("Non-Availability")]
    [Tooltip("Non-availability fit distribution")]
    [field: SerializeField]
    public Distribution HostNonavailabilityDistri {get; private set;} = Distribution.Lognormal;

    [Tooltip("A parameter for non-availability distribution")]
    [field: SerializeField]
    public double HostNonavailabilityA {get; private set;} = 2.444d;

    [Tooltip("B parameter for non-availability distribution")]
    [field: SerializeField]
    public double HostNonavailabilityB {get; private set;} = -0.586d;

    [Header("CPU Availability")]
    [Tooltip("CPU availability fit distribution")]
    [field: SerializeField]
    public Distribution CpuAvailabilityDistri {get; private set;}  = Distribution.Weibull;

    [Tooltip("A parameter for X-availability distribution")]
    [field: SerializeField]
    public double CpuAvailabilityA {get; private set;} = 0.393d;

    [Tooltip("B parameter for X-availability distribution")]
    [field: SerializeField]
    public double CpuAvailabilityB {get; private set;} = 2.964d;

    [Header("CPU-Non-Availability")]
    [Tooltip("Non-availability fit distribution")]
    [field: SerializeField]
    public Distribution CpuNonavailabilityDistri {get; private set;} = Distribution.Lognormal;

    [Tooltip("A parameter for Y-non-availability distribution")]
    [field: SerializeField]
    public double CpuNonavailabilityA {get; private set;} = 2.844d;

    [Tooltip("B parameter for Y-non-availability distribution")]
    [field: SerializeField]
    public double CpuNonavailabilityB {get; private set;} = -0.586d;
}