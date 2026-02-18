using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class SimulationManager
{
    private class StatisticWriter
    {
        private bool _headerWritten = false;
        private const int WaitTime = 3600;

        public IEnumerator WriteTaskCsv()
        {
            string filePath = Instance._config.StatisticsFileName;
            
            // Clear the file at the beginning of the simulation
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            while (true)
            {
                yield return new WaitForTicks(WaitTime);

                try
                {
                    using (StreamWriter file = new StreamWriter(filePath, true))
                    {
                        if (!_headerWritten)
                        {
                            file.WriteLine("timestamp,project_name,tasks_total,tasks_inprogress,tasks_completed,tasks_valid,tasks_error");
                            _headerWritten = true;
                        }

                        double timestamp = TimeTickSystem.Instance.CurTick;

                        var projects = Instance.Actors.Values.OfType<ProjectModel>();

                        foreach (var project in projects)
                        {
                            int totalTasks = project.TaskDatabase.Count;
                            int inProgressTasks = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.InProgress);
                            int validTasks = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.Valid);
                            int errorTasks = project.TaskDatabase.Values.Count(t => t.CurrentState == TaskModel.State.Error);
                            int completedTasks = validTasks + errorTasks;

                            file.WriteLine($"{timestamp},{project.ProjectName},{totalTasks},{inProgressTasks},{completedTasks},{validTasks},{errorTasks}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error writing statistics to file: {ex.Message}");
                }
            }
        }
    }
}
