using System.IO;

public class TaskDynamicWriter : IFileWriter
{
    private readonly string _filePath;

    public TaskDynamicWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            for (int applicationIndex = 0; applicationIndex < data.ValidWorkunitsTimestamps.Length; applicationIndex++)
            {
                var timestamps = data.ValidWorkunitsTimestamps[applicationIndex];
                for (int tick = 0; tick < data.SimulationDuration; tick++)
                {
                    file.WriteLine($"{applicationIndex.ToCsv()} {timestamps[tick].ToCsv()}");
                }
            }
        }
    }
}

public class ClientsDynamicWriter : IFileWriter
{
    private readonly string _filePath;

    public ClientsDynamicWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            for (int tick = 0; tick < data.SimulationDuration; tick++)
            {
                file.WriteLine(data.ClientsAvailability[tick].ToCsv());
            }
        }
    }
}

public class SpeedStatisticsWriter : IFileWriter
{
    private readonly string _filePath;

    public SpeedStatisticsWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            foreach (var value in data.SpeedStatistics)
            {
                file.WriteLine(value.Value.ToCsv("0.0"));
            }
        }
    }
}

public class TaskDynamicCompletedWriter : IFileWriter
{
    private readonly string _filePath;

    public TaskDynamicCompletedWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            for (int tick = 0; tick < data.SimulationDuration; tick++)
            {
                file.WriteLine($"{data.FirstApplicationInitialResults.ToCsv()} {data.ValidCompletedWorkunitsTimestamps[tick].ToCsv()}");
            }
        }
    }
}

public class WorkunitsAllDynamicWriter : IFileWriter
{
    private readonly string _filePath;

    public WorkunitsAllDynamicWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            for (int tick = 0; tick < data.SimulationDuration; tick++)
            {
                file.WriteLine(data.WorkunitTimestamps[tick].ToCsv());
            }
        }
    }
}

public class AvailabilityWriter : IFileWriter
{
    private readonly string _filePath;

    public AvailabilityWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            foreach (var value in data.Availability)
            {
                file.WriteLine(value.Value.ToCsv("0.0"));
            }
        }
    }
}

public class UnavailabilityWriter : IFileWriter
{
    private readonly string _filePath;

    public UnavailabilityWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            foreach (var value in data.Unavailability)
            {
                file.WriteLine(value.Value.ToCsv("0.0"));
            }
        }
    }
}

public class SentResultsWriter : IFileWriter
{
    private readonly string _filePath;

    public SentResultsWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            foreach (var value in data.SentResults)
            {
                file.WriteLine($"{value.First.ToCsv()} {value.Second.ToCsv()}");
            }
        }
    }
}

public class GotResultsWriter : IFileWriter
{
    private readonly string _filePath;

    public GotResultsWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            foreach (var value in data.GotResults)
            {
                file.WriteLine($"{value.First.ToCsv()} {value.Second.ToCsv()}");
            }
        }
    }
}

public class GeneralStatisticsWriter : IFileWriter
{
    private readonly string _filePath;

    public GeneralStatisticsWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Dump(StatsData data)
    {
        using (StreamWriter file = FileWriterExtension.OpenLegacyWriter(_filePath))
        {
            int completedWorkunits = data.CompletedWorkunits;
            int notCompletedWorkunits = data.CreatedWorkunits - completedWorkunits;
            float averageSpeed = GetAverage(data.SpeedStatistics);
            float availability = GetAvailability(data);
            float throughput = data.SimulationDuration > 0 ? data.MessagesReceived / (float)data.SimulationDuration : 0;

            file.WriteLine($"Total number of clients: {data.NumberOfClients.ToCsv()}");
            file.WriteLine();
            file.WriteLine($"#################### {data.ProjectName} ####################");
            file.WriteLine();
            file.WriteLine($"Simulation ends in {(data.SimulationDuration / 3600).ToCsv()} h ({data.SimulationDuration.ToCsv()} sec)");
            file.WriteLine();
            file.WriteLine($"Number of clients: {data.NumberOfClients.ToCsv()}");
            file.WriteLine($"Tail stage active: {data.TailStageActive.ToCsv()}");
            file.WriteLine($"Utilization safety: {data.UtilizationSafety.ToCsv("0.0000")}");
            file.WriteLine($"Theoretical GFLOP budget: {data.TheoreticalGflopsBudget.ToCsv("0.0")}");
            file.WriteLine($"Effective GFLOP budget: {data.EffectiveGflopsBudget.ToCsv("0.0")}");
            file.WriteLine($"Messages received: {data.MessagesReceived.ToCsv()}");
            file.WriteLine($"Work requests received: {data.WorkRequestsReceived.ToCsv()}");
            file.WriteLine($"Results created: {data.ResultsCreated.ToCsv()} ({Percent(data.ResultsCreated, data.WorkRequestsReceived).ToCsv("0.0")}%)");
            file.WriteLine($"Results sent: {data.ResultsSent.ToCsv()} ({Percent(data.ResultsSent, data.ResultsCreated).ToCsv("0.0")}%)");
            file.WriteLine($"Results received: {data.ResultsReceived.ToCsv()} ({Percent(data.ResultsReceived, data.ResultsCreated).ToCsv("0.0")}%)");
            file.WriteLine($"Results analyzed: {data.ResultsAnalyzed.ToCsv()} ({Percent(data.ResultsAnalyzed, data.ResultsReceived).ToCsv("0.0")}%)");
            file.WriteLine($"Results success: {data.ResultsSuccess.ToCsv()} ({Percent(data.ResultsSuccess, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Results failed: {data.ResultsFailed.ToCsv()} ({Percent(data.ResultsFailed, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Results too late: {data.ResultsTooLate.ToCsv()} ({Percent(data.ResultsTooLate, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Results valid: {data.ResultsValid.ToCsv()} ({Percent(data.ResultsValid, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits total: {data.CreatedWorkunits.ToCsv()}");
            file.WriteLine($"Workunits completed: {completedWorkunits.ToCsv()} ({Percent(completedWorkunits, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits not completed: {notCompletedWorkunits.ToCsv()} ({Percent(notCompletedWorkunits, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits valid: {data.WorkunitsValid.ToCsv()} ({Percent(data.WorkunitsValid, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits valid but not completed: {data.WorkunitsValidButNotCompleted.ToCsv()} ({Percent(data.WorkunitsValidButNotCompleted, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits error: {data.WorkunitsError.ToCsv()} ({Percent(data.WorkunitsError, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Throughput: {throughput.ToCsv("0.0")} messages/s");
            file.WriteLine($"Credit granted: {data.TotalCredit.ToCsv()} credits");
            file.WriteLine();

            for (int i = 0; i < data.ApplicationWorkunitsTotal.Length; i++)
            {
                float workunitCost = data.ApplicationInitialResults[i] * data.ApplicationTaskGflops[i];
                file.WriteLine($"Application {i.ToCsv()}");
                file.WriteLine($"Workunits total: {data.ApplicationWorkunitsTotal[i].ToCsv()}");
                file.WriteLine($"Tail target workunits total: {data.ApplicationTailTargetWorkunitsTotal[i].ToCsv()}");
                file.WriteLine($"Workunit cost in GFLOP: {workunitCost.ToCsv("0.0")}");
                file.WriteLine();
            }

            file.WriteLine($"Clients. Average speed: {averageSpeed.ToCsv("0.000000")} GFLOPS. Available: {availability.ToCsv("0.0")}% Not available {(100 - availability).ToCsv("0.0")}%");
        }
    }

    private float Percent(int value, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return value / (float)total * 100;
    }

    private float GetAverage(System.Collections.Generic.List<StatValueData> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        float sum = 0;
        foreach (var value in values)
        {
            sum += value.Value;
        }

        return sum / values.Count;
    }

    private float GetAvailability(StatsData data)
    {
        float available = 0;
        foreach (var value in data.Availability)
        {
            available += value.Value;
        }

        float unavailable = 0;
        foreach (var value in data.Unavailability)
        {
            unavailable += value.Value;
        }

        float total = available + unavailable;
        if (total <= 0)
        {
            return 0;
        }

        return available / total * 100;
    }
}
