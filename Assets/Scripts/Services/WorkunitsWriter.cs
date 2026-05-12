using System.IO;

public class WorkunitsCreationWriter : IFileWriter
{
    private readonly string _filePath;

    public WorkunitsCreationWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            for (int applicationIndex = 0; applicationIndex < data.CreationWorkunitTimestamps.Length; applicationIndex++)
            {
                var timestamps = data.CreationWorkunitTimestamps[applicationIndex];
                for (int tick = 0; tick < data.SimulationDuration; tick++)
                {
                    file.WriteLine($"{applicationIndex.ToCsv()} {timestamps[tick].ToCsv()}");
                }
            }
        }
    }
}
