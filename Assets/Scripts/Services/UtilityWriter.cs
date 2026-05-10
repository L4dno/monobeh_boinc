using System;
using System.IO;
using UnityEngine;

public class GridUtilityWriter : IFileWriter
{
    private readonly string _filePath;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

    public GridUtilityWriter(string filePath)
    {
        _filePath = filePath;

        using (StreamWriter file = new StreamWriter(_filePath, false))
        {
            file.WriteLine("timestamp,grid_utilization");
        }
    }

    public void Dump(StatsData data)
    {
        try
        {
            using (StreamWriter file = new StreamWriter(_filePath, true))
            {
                float gridUtil = data.OnlinePower > 0 ? (data.OnlinePower - data.IdlePower) / data.OnlinePower : 0;
                int timestamp = TimeSystem.CurTick;
                file.WriteLine($"{timestamp.ToCsv()},{gridUtil.ToCsv()}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error writing statistics: {ex.Message}");
        }
    }
}
