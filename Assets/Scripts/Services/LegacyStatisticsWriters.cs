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
            int outputDuration = data.GetOutputDuration();
            for (int applicationIndex = 0; applicationIndex < data.ValidWorkunitsTimestamps.Length; applicationIndex++)
            {
                var timestamps = data.ValidWorkunitsTimestamps[applicationIndex];
                for (int tick = 0; tick < outputDuration; tick++)
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
            int outputDuration = data.GetOutputDuration();
            for (int tick = 0; tick < outputDuration; tick++)
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
            int outputDuration = data.GetOutputDuration();
            for (int tick = 0; tick < outputDuration; tick++)
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
            int outputDuration = data.GetOutputDuration();
            for (int tick = 0; tick < outputDuration; tick++)
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
            float averageSpeed = data.GetAverage(data.SpeedStatistics);
            float availability = data.GetAvailability();
            int outputDuration = data.GetOutputDuration();
            float throughput = outputDuration > 0 ? data.MessagesReceived / (float)outputDuration : 0;
            int tailMakespan = data.TailStartTick >= 0 ? data.FinishTick - data.TailStartTick : -1;
            float tailIdleness = data.TailStartTick >= 0 ? CalculateTailIdleness(data) : -1;
            float deadlineMissRate = data.ResultsSent > 0 ? data.ResultsTooLate / (float)data.ResultsSent : 0;

            file.WriteLine($"Total number of clients: {data.NumberOfClients.ToCsv()}");
            file.WriteLine();
            file.WriteLine($"#################### {data.ProjectName} ####################");
            file.WriteLine();
            file.WriteLine($"Simulation ends in {(data.FinishTick / 3600).ToCsv()} h ({data.FinishTick.ToCsv()} sec)");
            file.WriteLine($"Simulation limit: {(data.SimulationDuration / 3600).ToCsv()} h ({data.SimulationDuration.ToCsv()} sec)");
            file.WriteLine();
            file.WriteLine($"Number of clients: {data.NumberOfClients.ToCsv()}");
            file.WriteLine($"Tail stage active: {data.TailStageActive.ToCsv()}");
            file.WriteLine($"Utilization safety: {data.UtilizationSafety.ToCsv("0.0000")}");
            file.WriteLine($"Theoretical GFLOP budget: {data.TheoreticalGflopsBudget.ToCsv("0.0")}");
            file.WriteLine($"Effective GFLOP budget: {data.EffectiveGflopsBudget.ToCsv("0.0")}");
            file.WriteLine($"Messages received: {data.MessagesReceived.ToCsv()}");
            file.WriteLine($"Work requests received: {data.WorkRequestsReceived.ToCsv()}");
            file.WriteLine($"Results created: {data.ResultsCreated.ToCsv()} ({data.Percent(data.ResultsCreated, data.WorkRequestsReceived).ToCsv("0.0")}%)");
            file.WriteLine($"Results sent: {data.ResultsSent.ToCsv()} ({data.Percent(data.ResultsSent, data.ResultsCreated).ToCsv("0.0")}%)");
            file.WriteLine($"Results received: {data.ResultsReceived.ToCsv()} ({data.Percent(data.ResultsReceived, data.ResultsCreated).ToCsv("0.0")}%)");
            file.WriteLine($"Results analyzed: {data.ResultsAnalyzed.ToCsv()} ({data.Percent(data.ResultsAnalyzed, data.ResultsReceived).ToCsv("0.0")}%)");
            file.WriteLine($"Results success: {data.ResultsSuccess.ToCsv()} ({data.Percent(data.ResultsSuccess, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Results failed: {data.ResultsFailed.ToCsv()} ({data.Percent(data.ResultsFailed, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Results too late: {data.ResultsTooLate.ToCsv()} ({data.Percent(data.ResultsTooLate, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Results valid: {data.ResultsValid.ToCsv()} ({data.Percent(data.ResultsValid, data.ResultsAnalyzed).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits total: {data.CreatedWorkunits.ToCsv()}");
            file.WriteLine($"Workunits completed: {completedWorkunits.ToCsv()} ({data.Percent(completedWorkunits, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits not completed: {notCompletedWorkunits.ToCsv()} ({data.Percent(notCompletedWorkunits, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits valid: {data.WorkunitsValid.ToCsv()} ({data.Percent(data.WorkunitsValid, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits valid but not completed: {data.WorkunitsValidButNotCompleted.ToCsv()} ({data.Percent(data.WorkunitsValidButNotCompleted, data.CreatedWorkunits).ToCsv("0.0")}%)");
            file.WriteLine($"Workunits error: {data.WorkunitsError.ToCsv()} ({data.Percent(data.WorkunitsError, data.CreatedWorkunits).ToCsv("0.0")}%)");
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
            file.WriteLine();
            file.WriteLine("#################### Tail metrics ####################");
            file.WriteLine($"Ttail: {data.TailStartTick.ToCsv()} sec");
            file.WriteLine($"Tfinish: {data.FinishTick.ToCsv()} sec");
            file.WriteLine($"TailMakespan: {tailMakespan.ToCsv()} sec");
            file.WriteLine($"TailIdleness: {tailIdleness.ToCsv("0.000000")}");
            file.WriteLine($"DeadlineMissRate: {deadlineMissRate.ToCsv("0.000000")}");
        }
    }

    private float CalculateTailIdleness(StatsData data)
    {
        int startTick = data.GetBoundedTick(data.TailStartTick);
        int finishTick = data.GetBoundedTick(data.FinishTick);
        float onlinePower = 0;
        float idlePower = 0;
        float tailOnlinePowerTime = 0;
        float tailIdlePowerTime = 0;

        for (int tick = 0; tick < finishTick; tick++)
        {
            onlinePower += data.GridOnlinePowerDeltas[tick];
            idlePower += data.GridIdlePowerDeltas[tick];

            if (tick >= startTick)
            {
                tailOnlinePowerTime += onlinePower;
                tailIdlePowerTime += idlePower;
            }
        }

        if (tailOnlinePowerTime <= 0)
        {
            return 0;
        }

        return tailIdlePowerTime / tailOnlinePowerTime;
    }

}
