using UnityEngine;
using System.Collections;
using System.Globalization;
using System.IO;


public partial class SimulationManager
{
    private class StatisticWriter
    {
    private readonly string _outputDir;
    private const int Interval = 3600;
    private readonly IStatService _statService;

    const string WORKUNITS_FILE = "workunits.csv";
    const string PARAMS_FILE = "parameters.json";

    const string GRID_FILE = "grid.csv";

    private static string ToCsv(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string ToCsv(float value) => value.ToString(CultureInfo.InvariantCulture);


public StatisticWriter(SimConfig simConfig, IStatService statService)
    {
        _statService = statService;
        string outputRoot = Application.persistentDataPath;

        int runNumber = 1;
        string sessionDir;
        while (true)
        {
            string folderName = $"{simConfig.ExperimentFolderName}_{runNumber}";
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
        InitializeDirectory();
    }

    private void InitializeDirectory()
    {
        string workunitsPath = Path.Combine(_outputDir, WORKUNITS_FILE);
        string paramsPath = Path.Combine(_outputDir, PARAMS_FILE);
        string gridPath = Path.Combine(_outputDir, GRID_FILE);

        using (StreamWriter file = new StreamWriter(gridPath, false))
            {
                file.WriteLine("timestamp,grid_utilization");
            }

        using (StreamWriter file = new StreamWriter(workunitsPath, false))
        {
            file.WriteLine("timestamp,tasks_inprogress");
        }
        
        string paramsJson = Container.Instance.ConfigProvider.GetConfigsJson();
        using (StreamWriter file = new StreamWriter(paramsPath, false))
        {
            file.WriteLine(paramsJson);
        }


    }

    public IEnumerator WriteGridPowerCsv()
        {
            
            WriteGridPowerToFile();
        while (true)
        {
            yield return new WaitForTicks(Interval);
            WriteGridPowerToFile();
        }
        }

    private void WriteGridPowerToFile()
        {
            string filePath = Path.Combine(_outputDir, GRID_FILE);

        try {
            using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    var stats = _statService.GetStats();
                    float gridUtil = stats.OnlinePower > 0 ? (stats.OnlinePower - stats.IdlePower) / stats.OnlinePower : 0;
                    Debug.LogWarning($"current online power is {stats.OnlinePower}, idle power is {stats.IdlePower}");
                    int timestamp = TimeTickSystem.Instance.CurTick;
                    file.WriteLine($"{ToCsv(timestamp)},{ToCsv(gridUtil)}");
                }
        }
        catch (System.Exception ex)
            {
                Debug.LogError($"Error writing statistics: {ex.Message}");
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
    public void WriteStats()
    {
        WriteWorkStatsToFile();
        WriteGridPowerToFile();
            }

        private void WriteWorkStatsToFile()
        {
            string filePath = Path.Combine(_outputDir, WORKUNITS_FILE);

            try
            {
                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    int timestamp = TimeTickSystem.Instance.CurTick;
                    file.WriteLine($"{ToCsv(timestamp)},{ToCsv(_statService.GetStats().UnfinishedTasks)}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error writing statistics: {ex.Message}");
            }
        }

        }
}
