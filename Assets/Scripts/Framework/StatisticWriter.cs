using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;


public partial class SimulationManager
{
    private class StatisticWriter
    {
    private readonly string _outputDir;
    private const int Interval = 3600;

    private SimConfigData _outputParams;

    const string WORKUNITS_FILE = "workunits.csv";
    const string PARAMS_FILE = "parameters.json";

    const string HOST_UTILIZATION_FILE = "host_utilization.csv";


    public StatisticWriter(string experimentName, SimConfig simConfig)
    {
        string outputRoot = Application.persistentDataPath;

        int runNumber = 1;
        string sessionDir;
        while (true)
        {
            string folderName = $"{experimentName}_{runNumber}";
            sessionDir = Path.Combine(outputRoot, folderName);
            if (!Directory.Exists(sessionDir))
            {
                break;
            }
            runNumber++;
        }

        Directory.CreateDirectory(sessionDir);
        _outputDir = sessionDir;

        Debug.LogWarning($"Statistics dir path: {_outputDir}");
        _outputParams = CreateConfigData(simConfig);
        InitializeDirectory();
    }

    private void InitializeDirectory()
    {
        string workunitsPath = Path.Combine(_outputDir, WORKUNITS_FILE);
        string paramsPath = Path.Combine(_outputDir, PARAMS_FILE);
        string hostPath = Path.Combine(_outputDir, HOST_UTILIZATION_FILE);

     
        using (StreamWriter file = new StreamWriter(workunitsPath, false))
        {
            file.WriteLine("timestamp,project_name,tasks_total,tasks_inprogress,tasks_completed,tasks_valid,tasks_error");
        }
        
        string paramsJson = JsonConvert.SerializeObject(_outputParams);
        using (StreamWriter file = new StreamWriter(paramsPath, false))
        {
            file.WriteLine(paramsJson);
        }
        using (StreamWriter file = new StreamWriter(hostPath, false))
        {
            var header = new System.Text.StringBuilder("timestamp");
            for (int i = 0; i < SimulationManager.Instance.hosts.Count; i++)
            {
                header.Append($",host_{i}");
            }
            file.WriteLine(header.ToString());
        }


    }

    public IEnumerator WriteTaskCsv()
    {
        
        WriteWorkStatsToFile();

        while (true)
        {
            yield return new WaitForTicks(Interval);
            WriteWorkStatsToFile();
        }
    }
      public IEnumerator WriteHostCsv()
            {
                
                WriteHostStatsToFile();
    
                while (true)
                {
                    yield return new WaitForTicks(Interval * 2);
                    WriteHostStatsToFile();
                }
            }
    public void WriteStats()
    {
        WriteWorkStatsToFile();
        WriteHostStatsToFile();
    }

    private void WriteHostStatsToFile()
    {
        var wuSent = GlobalStats.WorkunitsSent.Values.Sum();
        var wuLate = GlobalStats.LateWorkunitResults.Values.Sum();
        var wuError = GlobalStats.ErrorWorkunitResults.Values.Sum();
        var wuValid = GlobalStats.ValidWorkunitResults.Values.Sum();
        var wuReceived = GlobalStats.WorkunitResultsReceived.Values.Sum();

        Debug.Log($"wu sent: {wuSent}");
        Debug.Log($"wu late: {wuLate}");
        Debug.Log($"wu error: {wuError}");
        Debug.Log($"wu valid: {wuValid}");
        Debug.Log($"wu received: {wuReceived}");


        string filePath = Path.Combine(_outputDir, HOST_UTILIZATION_FILE);

            using (StreamWriter file = new StreamWriter(filePath, true))
            {
                double timestamp = TimeTickSystem.Instance.CurTick;

                var hostUtilizations = new List<string>();
                int numberOfHosts = Instance.hosts.Count;

                for (int i = 0; i < numberOfHosts; i++)
                {
                    int busy = GlobalStats.TotalBusyTimeByHost.ContainsKey(i) ? GlobalStats.TotalBusyTimeByHost[i] : 0;
                    int idle = GlobalStats.TotalIdleTimeByHost.ContainsKey(i) ? GlobalStats.TotalIdleTimeByHost[i] : 0;
                    int suspended = GlobalStats.TotalSuspendedTimeByHost.ContainsKey(i) ? GlobalStats.TotalSuspendedTimeByHost[i] : 0;

                    int totalTime = busy + idle;

                    float utilization = 0;
                    if (totalTime > 0)
                    {
                        utilization = ((float)busy / totalTime);
                    }

                    hostUtilizations.Add(utilization.ToString("F2", CultureInfo.InvariantCulture));
                }

                string utils = string.Join(",", hostUtilizations);
                file.WriteLine($"{timestamp},{utils}");
            }
        }

        private void WriteWorkStatsToFile()
        {
            string filePath = Path.Combine(_outputDir, WORKUNITS_FILE);

            try
            {
                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    double timestamp = TimeTickSystem.Instance.CurTick;

                    
                    var projects = SimulationManager.Instance.Actors.Values.OfType<ProjectModel>();

                    foreach (var project in projects)
                    {
                        
                        int inProgress = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.InProgress);
                        
                        int valid = GlobalStats.TasksValid.ContainsKey(project.ProjectName) ? GlobalStats.TasksValid[project.ProjectName] : 0;
                        int error = GlobalStats.TasksError.ContainsKey(project.ProjectName) ? GlobalStats.TasksError[project.ProjectName] : 0;

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
                StatisticsFileName = simConfig.ExperimentFolderName
            };

            if (simConfig.ProjectConfig != null)
            {
                simConfigData.ProjectConfig = new ProjectConfigData
                {
                    ProjectName = simConfig.ProjectConfig.ProjectName,
                    Priority = simConfig.ProjectConfig.Priority,
                    ProjectId = simConfig.ProjectConfig.ProjectId,
                    ServerPowerGflops = simConfig.ProjectConfig.ServerPowerGflops,
                    SuccessPercentage = simConfig.ProjectConfig.SuccessPercentage,
                    CanonicalPercentage = simConfig.ProjectConfig.CanonicalPercentage
                };

                if (simConfig.ProjectConfig.TaskConfigs != null && simConfig.ProjectConfig.TaskConfigs.Any())
                {
                    var taskConfig = simConfig.ProjectConfig.TaskConfigs[0];
                    simConfigData.ProjectConfig.TaskConfig = new TaskConfigData
                    {
                        TaskPowerDistri = taskConfig.TaskPowerDistri,
                        MinTaskGflops = taskConfig.MinTaskGflops,
                        MaxTaskGflops = taskConfig.MaxTaskGflops,
                        InputFileSize = taskConfig.InputFileSize,
                        OutputFileSize = taskConfig.OutputFileSize,
                        InitialCreatedWorkunits = taskConfig.InitialCreatedWorkunits,
                        MaxCreatedWorkunits = taskConfig.MaxCreatedWorkunits,
                        MaxErrorWorkunits = taskConfig.MaxErrorWorkunits,
                        MaxSuccessWorkunits = taskConfig.MaxSuccessWorkunits,
                        DelayBound = taskConfig.DelayBound,
                        MinQuorum = taskConfig.MinQuorum
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
