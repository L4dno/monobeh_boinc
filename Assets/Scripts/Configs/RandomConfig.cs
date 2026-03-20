using UnityEngine;

[CreateAssetMenu(fileName = "RandomConfig", menuName = "Scriptable Objects/RandomConfig")]
public class RandomConfig : ScriptableObject
{
    
    [Header("Host Parameters")]
    [Tooltip("Speed fit distribution")]
    
    public Distribution HostPowerDistri  = Distribution.Exponential;

    [Tooltip("A parameter for speed distribution")]
    
    public float PowerA  = 0.1667f;

    [Tooltip("B parameter for speed distribution")]
    
    public float PowerB  = -1f;

    [Header("Host Availability")]
    [Tooltip("Availability fit distribution")]
    
    public Distribution HostAvailabilityDistri  = Distribution.Weibull;

    [Tooltip("A parameter for availability distribution")]
    
    public float HostAvailabilityA  = 0.393f;

    [Tooltip("B parameter for availability distribution")]
    
    public float HostAvailabilityB  = 2.964f;

    [Header("Non-Availability")]
    [Tooltip("Non-availability fit distribution")]
    
    public Distribution HostNonavailabilityDistri  = Distribution.Lognormal;

        [Tooltip("A parameter for non-availability distribution")]
        

        public float HostNonavailabilityA  = 2.444f;

        [Tooltip("B parameter for non-availability distribution")]

        

        public float HostNonavailabilityB  = -0.586f;

    [Header("CPU Availability")]
    [Tooltip("CPU availability fit distribution")]
    
    public Distribution CpuAvailabilityDistri   = Distribution.Weibull;

    [Tooltip("A parameter for X-availability distribution")]
    
    public float CpuAvailabilityA  = 0.393f;

    [Tooltip("B parameter for X-availability distribution")]
    
    public float CpuAvailabilityB  = 2.964f;

    [Header("CPU-Non-Availability")]
    [Tooltip("Non-availability fit distribution")]
    
    public Distribution CpuNonavailabilityDistri  = Distribution.Lognormal;

    [Tooltip("A parameter for Y-non-availability distribution")]
    
    public float CpuNonavailabilityA  = 2.444f;

    [Tooltip("B parameter for Y-non-availability distribution")]
    
    public float CpuNonavailabilityB  = -0.586f;
}