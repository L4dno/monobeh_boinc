public class SimConfigData
{
    public int SimLength;
    public int NumberOfProjects;
    public ProjectConfigData ProjectConfig;
    public int NumberOfGroups;
    public GroupConfigData GroupConfig;
    public int DeterministicSeed;
    public string StatisticsFileName;
}

public class ProjectConfigData
{
    public string ProjectName;
    public float Priority;
    public int ProjectId;
    public TaskConfigData TaskConfig;
    public float ServerPowerGflops;
    public int InitialTaskCount;
    public int SuccessPercentage;
    public int CanonicalPercentage;
}

public class GroupConfigData
{
    public int NumberOfClients;
    public RandomConfigData RandomConfig;
    public float MaxSpeed;
    public float MinSpeed;
    public int ConnectionInterval;
    public int SchedulingInterval;
    public float ServerLatency;
    public float ServerBandwidth;
}

public class RandomConfigData
{
    public Distribution HostPowerDistri;
    public float PowerA;
    public float PowerB;
    public Distribution HostAvailabilityDistri;
    public float HostAvailabilityA;
    public float HostAvailabilityB;
    public Distribution HostNonavailabilityDistri;
    public float HostNonavailabilityA;
    public float HostNonavailabilityB;
    public Distribution CpuAvailabilityDistri;
    public float CpuAvailabilityA;
    public float CpuAvailabilityB;
    public Distribution CpuNonavailabilityDistri;
    public float CpuNonavailabilityA;
    public float CpuNonavailabilityB;
}

public class TaskConfigData
{
    public Distribution TaskPowerDistri;
    public float MinTaskGflops;
    public float MaxTaskGflops;
    public float InputFileSize;
    public float OutputFileSize;
    public int InitialCreatedWorkunits;
    public int MaxCreatedWorkunits;
    public int MaxErrorWorkunits;
    public int MaxSuccessWorkunits;
    public int DelayBound;
    public int MinQuorum;
}
