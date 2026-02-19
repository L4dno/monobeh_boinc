using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class SimulationManager
{
    private class StatisticWriter
    {
    private readonly string _filePath;
    private const int Interval = 3600;

    public StatisticWriter(string filePath)
    {
        _filePath = filePath;
        InitializeFile();
    }

    private void InitializeFile()
    {
        try
        {
            
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            
            using (StreamWriter file = new StreamWriter(_filePath, false))
            {
                file.WriteLine("timestamp,project_name,tasks_total,tasks_inprogress,tasks_completed,tasks_valid,tasks_error");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing stats file: {ex.Message}");
        }
    }

    public IEnumerator WriteTaskCsv()
    {
        
        WriteStatsToFile();

        while (true)
        {
            yield return new WaitForTicks(Interval);
            WriteStatsToFile();
        }
    }

    public void WriteStats()
    {
        WriteStatsToFile();
    }

    private void WriteStatsToFile()
    {
        try
        {
            using (StreamWriter file = new StreamWriter(_filePath, true))
            {
                double timestamp = TimeTickSystem.Instance.CurTick;

                
                var projects = Instance.Actors.Values.OfType<ProjectModel>();

                foreach (var project in projects)
                {
                    
                    int inProgress = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.InProgress);
                    int validInDb = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.Valid);
                    int errorInDb = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.Error);

                    int valid = validInDb + project.StatTasksValid;
                    int error = errorInDb + project.StatTasksError;
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
}

}
