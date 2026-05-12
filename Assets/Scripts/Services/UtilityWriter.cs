using System.IO;

public class GridUtilizationWriter : IFileWriter
{
    private readonly string _filePath;

    public GridUtilizationWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            float onlinePower = 0;
            float idlePower = 0;

            for (int tick = 0; tick < data.SimulationDuration; tick++)
            {
                onlinePower += data.GridOnlinePowerDeltas[tick];
                idlePower += data.GridIdlePowerDeltas[tick];
                float busyPower = onlinePower - idlePower;
                float gridUtilization = onlinePower > 0 ? busyPower / onlinePower : 0;
                file.WriteLine(gridUtilization.ToCsv("0.000000"));
            }
        }
    }
}
