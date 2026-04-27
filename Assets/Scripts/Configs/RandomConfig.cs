using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "RandomConfig", menuName = "Scriptable Objects/RandomConfig")]
public class RandomConfig : ScriptableObject
{
    
    [Header("Host Parameters")]
    [Tooltip("Speed fit distribution")]
    
    [FormerlySerializedAs("<HostPowerDistri>k__BackingField")]
    public Distribution HostPowerDistri  = Distribution.Exponential;

    [Tooltip("A parameter for speed distribution")]
    
    [FormerlySerializedAs("<PowerA>k__BackingField")]
    public float PowerA  = 0.1667f;

    [Tooltip("B parameter for speed distribution")]
    
    [FormerlySerializedAs("<PowerB>k__BackingField")]
    public float PowerB  = -1f;

    [Header("Host Availability")]
    [Tooltip("Availability fit distribution")]
    
    [FormerlySerializedAs("<HostAvailabilityDistri>k__BackingField")]
    public Distribution HostAvailabilityDistri  = Distribution.Weibull;

    [Tooltip("A parameter for availability distribution")]
    
    [FormerlySerializedAs("<HostAvailabilityA>k__BackingField")]
    public float HostAvailabilityA  = 0.393f;

    [Tooltip("B parameter for availability distribution")]
    
    [FormerlySerializedAs("<HostAvailabilityB>k__BackingField")]
    public float HostAvailabilityB  = 2.964f;

    [Header("Non-Availability")]
    [Tooltip("Non-availability fit distribution")]
    
    [FormerlySerializedAs("<HostNonavailabilityDistri>k__BackingField")]
    public Distribution HostNonavailabilityDistri  = Distribution.Lognormal;

        [Tooltip("A parameter for non-availability distribution")]
        

        [FormerlySerializedAs("<HostNonavailabilityA>k__BackingField")]
        public float HostNonavailabilityA  = 2.444f;

        [Tooltip("B parameter for non-availability distribution")]

        

        [FormerlySerializedAs("<HostNonavailabilityB>k__BackingField")]
        public float HostNonavailabilityB  = -0.586f;

    [Header("CPU Availability")]
    [Tooltip("CPU availability fit distribution")]
    
    [FormerlySerializedAs("<CpuAvailabilityDistri>k__BackingField")]
    public Distribution CpuAvailabilityDistri   = Distribution.Weibull;

    [Tooltip("A parameter for X-availability distribution")]
    
    [FormerlySerializedAs("<CpuAvailabilityA>k__BackingField")]
    public float CpuAvailabilityA  = 0.393f;

    [Tooltip("B parameter for X-availability distribution")]
    
    [FormerlySerializedAs("<CpuAvailabilityB>k__BackingField")]
    public float CpuAvailabilityB  = 2.964f;

    [Header("CPU-Non-Availability")]
    [Tooltip("Non-availability fit distribution")]
    
    [FormerlySerializedAs("<CpuNonavailabilityDistri>k__BackingField")]
    public Distribution CpuNonavailabilityDistri  = Distribution.Lognormal;

    [Tooltip("A parameter for Y-non-availability distribution")]
    
    [FormerlySerializedAs("<CpuNonavailabilityA>k__BackingField")]
    public float CpuNonavailabilityA  = 2.444f;

    [Tooltip("B parameter for Y-non-availability distribution")]
    
    [FormerlySerializedAs("<CpuNonavailabilityB>k__BackingField")]
    public float CpuNonavailabilityB  = -0.586f;
}
