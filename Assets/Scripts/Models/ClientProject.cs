// using System.Collections.Generic;

// public class ClientProject
// {
//     public string Name;
//     public int Priority;
//     public bool IsOn = true;

//     // Task management
//     public Queue<TaskModel> Tasks = new Queue<TaskModel>();
//     public Queue<TaskModel> ReadyTasks = new Queue<TaskModel>();
//     public List<TaskModel> RunningTasks = new List<TaskModel>();
//     public Queue<ClientReplyData> CompletedTasks = new Queue<ClientReplyData>();

//     // Statistics
//     public int TotalTasksChecked = 0;
//     public int TotalTasksExecuted = 0;
//     public int TotalTasksReceived = 0;

//     // Scheduling data
//     public double AnticipatedDebt;
//     public double ShortTermDebt;
//     public double LongTermDebt;
//     public double WallCpuTime;
//     public double Shortfall;

//     public ClientProject(ProjectConfig config)
//     {
//         Name = config.name;
//         // In the old code, priority was passed via command line. 
//         // Here, we'll assume a default or get it from config if available.
//         // For now, let's hardcode it to 1, as in the original project struct, 
//         // where it was used as a char.
//         Priority = 1; 
//     }
// }
