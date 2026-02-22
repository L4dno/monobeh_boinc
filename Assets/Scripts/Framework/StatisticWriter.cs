using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class SimulationManager
{
    private class StatisticWriter
    {
    private readonly string _filePath;
    private const int Interval = 3600;

    public StatisticWriter(string filePath)
    {
        _filePath = filePath;
        InitializeFile();
    }

    private void InitializeFile()
    {
        try
        {
            
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            
            using (StreamWriter file = new StreamWriter(_filePath, false))
            {
                file.WriteLine("timestamp,project_name,tasks_total,tasks_inprogress,tasks_completed,tasks_valid,tasks_error");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing stats file: {ex.Message}");
        }
    }

    public IEnumerator WriteTaskCsv()
    {
        
        WriteStatsToFile();

        while (true)
        {
            yield return new WaitForTicks(Interval);
            WriteStatsToFile();
        }
    }

    public void WriteStats()
    {
        WriteStatsToFile();
    }

    private void WriteStatsToFile()
    {
        try
        {
            using (StreamWriter file = new StreamWriter(_filePath, true))
            {
                double timestamp = TimeTickSystem.Instance.CurTick;

                
                var projects = Instance.Actors.Values.OfType<ProjectModel>();

                foreach (var project in projects)
                {
                    
                    int inProgress = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.InProgress);
                    
                    int valid = GlobalStats.WorkunitsValid.ContainsKey(project.ProjectName) ? GlobalStats.WorkunitsValid[project.ProjectName] : 0;
                    int error = GlobalStats.WorkunitsError.ContainsKey(project.ProjectName) ? GlobalStats.WorkunitsError[project.ProjectName] : 0;

                    int completed = valid + error;
                    int totalTasks = inProgress + completed;

                    file.WriteLine($"{timestamp},{project.ProjectName},{totalTasks},{inProgress},{completed},{valid},{error}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error writing statistics: {ex.Message}");
        }
    }

    public SimConfigData CreateConfigData(SimConfig simConfig)
    {
        var simConfigData = new SimConfigData
        {
            SimLength = simConfig.SimLength,
            NumberOfProjects = simConfig.NumberOfProjects,
            NumberOfGroups = simConfig.NumberOfGroups,
            DeterministicSeed = simConfig.DeterministicSeed,
            StatisticsFileName = simConfig.StatisticsFileName
        };

        if (simConfig.ProjectConfig != null)
        {
            simConfigData.ProjectConfig = new ProjectConfigData
            {
                ProjectName = simConfig.ProjectConfig.ProjectName,
                Priority = simConfig.ProjectConfig.Priority,
                ProjectId = simConfig.ProjectConfig.ProjectId,
                ServerPowerGflops = simConfig.ProjectConfig.ServerPowerGflops,
                DelayBound = simConfig.ProjectConfig.DelayBound,
                MinQuorum = simConfig.ProjectConfig.MinQuorum,
                InitialTaskCount = simConfig.ProjectConfig.InitialTaskCount,
                SuccessPercentage = simConfig.ProjectConfig.SuccessPercentage,
                CanonicalPercentage = simConfig.ProjectConfig.CanonicalPercentage
            };

            if (simConfig.ProjectConfig.TaskConfig != null)
            {
                simConfigData.ProjectConfig.TaskConfig = new TaskConfigData
                {
                    TaskPowerDistri = simConfig.ProjectConfig.TaskConfig.TaskPowerDistri,
                    MinTaskGflops = simConfig.ProjectConfig.TaskConfig.MinTaskGflops,
                    MaxTaskGflops = simConfig.ProjectConfig.TaskConfig.MaxTaskGflops,
                    InputFileSize = simConfig.ProjectConfig.TaskConfig.InputFileSize,
                    OutputFileSize = simConfig.ProjectConfig.TaskConfig.OutputFileSize,
                    InitialCreatedWorkunits = simConfig.ProjectConfig.TaskConfig.InitialCreatedWorkunits,
                    MaxCreatedWorkunits = simConfig.ProjectConfig.TaskConfig.MaxCreatedWorkunits,
                    MaxErrorWorkunits = simConfig.ProjectConfig.TaskConfig.MaxErrorWorkunits,
                    MaxSuccessWorkunits = simConfig.ProjectConfig.TaskConfig.MaxSuccessWorkunits
                };
            }
        }

        if (simConfig.GroupConfig != null)
        {
            simConfigData.GroupConfig = new GroupConfigData
            {
                NumberOfClients = simConfig.GroupConfig.NumberOfClients,
                MaxSpeed = simConfig.GroupConfig.MaxSpeed,
                MinSpeed = simConfig.GroupConfig.MinSpeed,
                ConnectionInterval = simConfig.GroupConfig.ConnectionInterval,
                SchedulingInterval = simConfig.GroupConfig.SchedulingInterval,
                ServerLatency = simConfig.GroupConfig.ServerLatency,
                ServerBandwidth = simConfig.GroupConfig.ServerBandwidth
            };

            if (simConfig.GroupConfig.RandomConfig != null)
            {
                simConfigData.GroupConfig.RandomConfig = new RandomConfigData
                {
                    HostPowerDistri = simConfig.GroupConfig.RandomConfig.HostPowerDistri,
                    PowerA = simConfig.GroupConfig.RandomConfig.PowerA,
                    PowerB = simConfig.GroupConfig.RandomConfig.PowerB,
                    HostAvailabilityDistri = simConfig.GroupConfig.RandomConfig.HostAvailabilityDistri,
                    HostAvailabilityA = simConfig.GroupConfig.RandomConfig.HostAvailabilityA,
                    HostAvailabilityB = simConfig.GroupConfig.RandomConfig.HostAvailabilityB,
                    HostNonavailabilityDistri = simConfig.GroupConfig.RandomConfig.HostNonavailabilityDistri,
                    HostNonavailabilityA = simConfig.GroupConfig.RandomConfig.HostNonavailabilityA,
                    HostNonavailabilityB = simConfig.GroupConfig.RandomConfig.HostNonavailabilityB,
                    CpuAvailabilityDistri = simConfig.GroupConfig.RandomConfig.CpuAvailabilityDistri,
                    CpuAvailabilityA = simConfig.GroupConfig.RandomConfig.CpuAvailabilityA,
                    CpuAvailabilityB = simConfig.GroupConfig.RandomConfig.CpuAvailabilityB,
                    CpuNonavailabilityDistri = simConfig.GroupConfig.RandomConfig.CpuNonavailabilityDistri,
                    CpuNonavailabilityA = simConfig.GroupConfig.RandomConfig.CpuNonavailabilityA,
                    CpuNonavailabilityB = simConfig.GroupConfig.RandomConfig.CpuNonavailabilityB
                };
            }
        }

        return simConfigData;
    }
}

}
