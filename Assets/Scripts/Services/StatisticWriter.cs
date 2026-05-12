using UnityEngine;
using System.IO;

public class StatisticWriter : IStatSaver
{
    private readonly IStatService _statService;
    private readonly IFileWriter[] _fileWriters;

    const string TASK_DYNAMIC_FILE = "task_dynamic";
    const string WORKUNITS_CREATION_FILE = "workunits_creation";
    const string CLIENTS_DYNAMIC_FILE = "clients_dynamic";
    const string TASK_DYNAMIC_COMPLETED_FILE = "task_dynamic_completed";
    const string WORKUNITS_ALL_DYNAMIC_FILE = "workunits_all_dynamic";
    const string GRID_UTILIZATION_FILE = "grid_utilization";
    const string SPEED_STATISTICS_FILE = "speed_statistics";
    const string AVAILABILITY_FILE = "availability";
    const string UNAVAILABILITY_FILE = "unavailability";
    const string SENT_RESULTS_FILE = "sent_results";
    const string GOT_RESULTS_FILE = "got_results";
    const string GENERAL_FILE = "general";
    const string PARAMS_FILE = "parameters.json";

    // получает зависимости внешне тк не монобех
    public StatisticWriter(IConfigProvider configProvider, IStatService statService)
    {
        _statService = statService;
        string outputDir = CreateStatisticsDirectory(configProvider.SimConfig);
        _fileWriters = new IFileWriter[]
        {
            new TaskDynamicWriter(Path.Combine(outputDir, TASK_DYNAMIC_FILE)),
            new WorkunitsCreationWriter(Path.Combine(outputDir, WORKUNITS_CREATION_FILE)),
            new ClientsDynamicWriter(Path.Combine(outputDir, CLIENTS_DYNAMIC_FILE)),
            new TaskDynamicCompletedWriter(Path.Combine(outputDir, TASK_DYNAMIC_COMPLETED_FILE)),
            new WorkunitsAllDynamicWriter(Path.Combine(outputDir, WORKUNITS_ALL_DYNAMIC_FILE)),
            new GridUtilizationWriter(Path.Combine(outputDir, GRID_UTILIZATION_FILE)),
            new SpeedStatisticsWriter(Path.Combine(outputDir, SPEED_STATISTICS_FILE)),
            new AvailabilityWriter(Path.Combine(outputDir, AVAILABILITY_FILE)),
            new UnavailabilityWriter(Path.Combine(outputDir, UNAVAILABILITY_FILE)),
            new SentResultsWriter(Path.Combine(outputDir, SENT_RESULTS_FILE)),
            new GotResultsWriter(Path.Combine(outputDir, GOT_RESULTS_FILE)),
            new GeneralStatisticsWriter(Path.Combine(outputDir, GENERAL_FILE))
        };
        WriteParams(outputDir, configProvider.GetConfigsJson());
    }

    private string CreateStatisticsDirectory(SimConfig simConfig)
    {
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
        Debug.LogWarning($"Statistics dir path: {sessionDir}");
        return sessionDir;
    }

    private void WriteParams(string outputDir, string paramsJson)
    {
        string paramsPath = Path.Combine(outputDir, PARAMS_FILE);

        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(paramsPath))
        {
            file.WriteLine(paramsJson);
        }
    }

    public void Dump()
    {
        var stats = _statService.GetStats();
        foreach (var fileWriter in _fileWriters)
        {
            fileWriter.Dump(stats);
        }
    }
}
