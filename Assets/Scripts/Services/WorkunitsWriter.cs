using System;
using System.IO;
using UnityEngine;

public class WorkunitsWriter : IFileWriter
{
    private readonly string _filePath;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

    public WorkunitsWriter(string filePath)
    {
        _filePath = filePath;

        using (StreamWriter file = new StreamWriter(_filePath, false))
        {
            file.WriteLine("timestamp,workunits_inprogress");
        }
    }

    public void Dump(StatsData data)
    {
        try
        {
            using (StreamWriter file = new StreamWriter(_filePath, true))
            {
                int timestamp = TimeSystem.CurTick;
                file.WriteLine($"{timestamp.ToCsv()},{data.UnfinishedWorkunits.ToCsv()}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error writing statistics: {ex.Message}");
        }
    }
}
