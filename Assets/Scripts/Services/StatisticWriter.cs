using UnityEngine;
using System.IO;

public class StatisticWriter : IStatSaver
{
    private readonly IStatService _statService;
    private readonly IFileWriter[] _fileWriters;

    const string WORKUNITS_FILE = "workunits.csv";
    const string PARAMS_FILE = "parameters.json";
    const string GRID_FILE = "grid.csv";

    // получает зависимости внешне тк не монобех
    public StatisticWriter(IConfigProvider configProvider, IStatService statService)
    {
        _statService = statService;
        string outputDir = CreateStatisticsDirectory(configProvider.SimConfig);
        _fileWriters = new IFileWriter[]
        {
            new WorkunitsWriter(Path.Combine(outputDir, WORKUNITS_FILE)),
            new GridUtilityWriter(Path.Combine(outputDir, GRID_FILE))
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

        using (StreamWriter file = new StreamWriter(paramsPath, false))
        {
            file.WriteLine(paramsJson);
        }
    }

    public void Dump()
    {
        var stats = _statService.GetStats();
        Debug.LogWarning($"current online power is {stats.OnlinePower}, idle power is {stats.IdlePower}");
        foreach (var fileWriter in _fileWriters)
        {
            fileWriter.Dump(stats);
        }
    }
}
