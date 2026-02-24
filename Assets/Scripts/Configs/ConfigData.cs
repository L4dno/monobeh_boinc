public class SimConfigData
{
    public int SimLength = 100;
    public int NumberOfProjects = 1;
    public ProjectConfigData ProjectConfig;
    public int NumberOfGroups = 1;
    public GroupConfigData GroupConfig;
    public int DeterministicSeed = 6523446;
    public string StatisticsFileName = "statistics.csv";
}

public class ProjectConfigData
{
    public string ProjectName = "ATLAS@home";
    public float Priority = 1.0f;
    public int ProjectId = 0;
    public TaskConfigData TaskConfig;
    public float ServerPowerGflops = 12.0f;
    public int InitialTaskCount = 100;
    public int SuccessPercentage = 95;
    public int CanonicalPercentage = 95;
}

public class GroupConfigData
{
    public int NumberOfClients = 1000;
    public RandomConfigData RandomConfig;
    public float MaxSpeed = 117.71f;
    public float MinSpeed = 0.07f;
    public int ConnectionInterval = 1;
    public int SchedulingInterval = 3600;
    public float ServerLatency = 1.0f;
    public float ServerBandwidth = 1.25f;
}

public class RandomConfigData
{
    public Distribution HostPowerDistri = Distribution.Exponential;
    public float PowerA = 0.1734f;
    public float PowerB = -1f;
    public Distribution HostAvailabilityDistri = Distribution.Weibull;
    public float HostAvailabilityA = 0.393f;
    public float HostAvailabilityB = 2.964f;
    public Distribution HostNonavailabilityDistri = Distribution.Lognormal;
    public float HostNonavailabilityA = 2.444f;
    public float HostNonavailabilityB = -0.586f;
    public Distribution CpuAvailabilityDistri = Distribution.Weibull;
    public float CpuAvailabilityA = 0.393f;
    public float CpuAvailabilityB = 2.964f;
    public Distribution CpuNonavailabilityDistri = Distribution.Lognormal;
    public float CpuNonavailabilityA = 2.844f;
    public float CpuNonavailabilityB = -0.586f;
}

public class TaskConfigData
{
    public Distribution TaskPowerDistri = Distribution.Normal;
    public float MinTaskGflops = 5040.0f;
    public float MaxTaskGflops = 7040.0f;
    public float InputFileSize = 54.6f;
    public float OutputFileSize = 100.0f;
    public int InitialCreatedWorkunits = 2;
    public int MaxCreatedWorkunits = 4;
    public int MaxErrorWorkunits = 2;
    public int MaxSuccessWorkunits = 3;
    public int DelayBound = 1000000000;
    public int MinQuorum = 2;
}
