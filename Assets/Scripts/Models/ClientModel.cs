// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;

// public class ClientModel : BaseActor
// {
//     // === CONFIGURATION ===
//     private readonly GroupConfig _config;
//     private readonly SimConfig _simConfig;

//     // === STATE ===
//     private HostModel _host;
//     private readonly long _powerGFlops;
//     private double _sumPriority;
//     private double _totalShortfall;
//     private double _lastWall;
//     private bool _isSuspended = false;

//     private readonly Dictionary<string, ClientProject> _projects = new Dictionary<string, ClientProject>();
//     private ClientTaskModel _activeTask = null;

//     // === CONSTANTS ===
//     private const int MAX_SHORT_TERM_DEBT = 86400;

//     public ClientModel(GroupConfig config, ProjectConfig[] projectConfigs, int actorId)
//     {
//         _config = config;
//         _actorId = actorId;

//         // Initialize client power from group config
//         float powerA = (_config.MaxSpeed + _config.MinSpeed) / 2;
//         float powerB = (_config.MaxSpeed - _config.MinSpeed) / 4;
//         _powerGFlops = (long)(RandomUtils.GetDistribution(config.RandomConfig.SpeedDistri, powerA, powerB) * 1_000_000_000);

//         // Initialize projects
//         foreach (var projConfig in projectConfigs)
//         {
//             var clientProject = new ClientProject(projConfig);
//             _projects.Add(clientProject.Name, clientProject);
//             _sumPriority += clientProject.Priority;
//         }

//         Debug.Log($"Client {_actorId} created with power {_powerGFlops / 1_000_000_000f} GFLOPS");
//     }

//     public override IEnumerator MainLoop(int hostId)
//     {
//         _hostId = hostId;
//         _host = SimulationManager.Instance.hosts[_hostId]; // Get host reference

//         // Initial warmup delay
//         int warmup = (int)RandomUtils.GetDistribution(Distribution.Uniform, 0, 3600);
//         yield return new WaitForTicks(warmup);

//         // Start background coroutines
//         SimulationManager.Instance.StartCoroutine(ExecuteTasks());
//         SimulationManager.Instance.StartCoroutine(FetchWork());
        
//         _lastWall = TimeTickSystem.Instance.CurrentTick;

//         // Main scheduling loop, analogous to the C client's main loop
//         while (true)
//         {
//             yield return new WaitForTicks(_config.SchedulingInterval);

//             if (_host.State == HostState.Off)
//             {
//                 if (!_isSuspended)
//                 {
//                     _isSuspended = true;
//                     Debug.Log($"Client {_actorId} suspended.");
//                 }
//                 continue; // Skip scheduling if host is off
//             }

//             if (_isSuspended)
//             {
//                 _isSuspended = false;
//                  _lastWall = TimeTickSystem.Instance.CurrentTick;
//                 Debug.Log($"Client {_actorId} resumed.");
//             }

//             UpdateDebt();
            
//             // Task selection logic based on debt
//             if (_activeTask == null)
//             {
//                 ClientTaskModel selectedTask = SelectTaskToRun();
//                 if (selectedTask != null)
//                 {
//                     selectedTask.Project.ReadyTasks.Enqueue(selectedTask);
//                 }
//             }
//         }
//     }

//     private IEnumerator ExecuteTasks()
//     {
//         while (true)
//         {
//             if (_isSuspended || _activeTask != null)
//             {
//                 yield return null;
//                 continue;
//             }

//             // Find a task in any project's ready queue
//             ClientTaskModel taskToExecute = null;
//             ClientProject projectOfTask = null;
//             foreach (var proj in _projects.Values)
//             {
//                 if (proj.ReadyTasks.Count > 0)
//                 {
//                     taskToExecute = proj.ReadyTasks.Dequeue();
//                     projectOfTask = proj;
//                     break;
//                 }
//             }

//             if (taskToExecute != null)
//             {
//                 _activeTask = taskToExecute;
//                 _activeTask.IsRunning = true;
                
//                 // Simulate execution
//                 long ticksToExecute = (long)(taskToExecute.Workunit.durationInFlops / _powerGFlops);
//                 if (ticksToExecute == 0) ticksToExecute = 1;
                
//                 yield return new WaitForTicks(ticksToExecute);

//                 // Finish execution
//                 projectOfTask.WallCpuTime += TimeTickSystem.Instance.CurrentTick - _lastWall;
//                 _lastWall = TimeTickSystem.Instance.CurrentTick;

//                 // Create reply and add to completed queue
//                 var reply = new ClientReplyData(
//                     _actorId,
//                     taskToExecute.Workunit.parentTaskId, 
//                     taskToExecute.Workunit.workunitId,
//                     taskToExecute.Workunit.byteSize,
//                     // Determine status based on configuration
//                     WorkunitStatus.Success, 
//                     WorkunitValue.Valid
//                 );
//                 projectOfTask.CompletedTasks.Enqueue(reply);
                
//                 Debug.Log($"Client {_actorId} finished task {taskToExecute.Workunit.workunitId} from project {projectOfTask.Name}");

//                 _activeTask.IsRunning = false;
//                 _activeTask = null;
//             }
//             else
//             {
//                 // No tasks ready, wait a bit
//                 yield return new WaitForTicks(1);
//             }
//         }
//     }

//     private IEnumerator FetchWork()
//     {
//         while (true)
//         {
//              yield return new WaitForTicks(_config.ConnectionInterval);

//             if (_isSuspended)
//             {
//                 continue;
//             }

//             UpdateShortfall();

//             // Select project to fetch work for
//             ClientProject selectedProj = null;
//             double maxControl = double.MinValue;

//             foreach (var proj in _projects.Values)
//             {
//                 if (!proj.IsOn || proj.Shortfall <= 0) continue;

//                 double control = proj.LongTermDebt + proj.Shortfall;
//                 if (control > maxControl)
//                 {
//                     maxControl = control;
//                     selectedProj = proj;
//                 }
//             }

//             if (selectedProj != null)
//             {
//                 // We don't have deadline missed logic from C code, so we just ask for work
//                 Debug.Log($"Client {_actorId} asking for work from project {selectedProj.Name}");
//                 SimulationManager.Instance.StartCoroutine(AskForWork(selectedProj));
//             }
//         }
//     }

//     private IEnumerator AskForWork(ClientProject proj)
//     {
//         // 1. Send completed work
//         while (proj.CompletedTasks.Count > 0)
//         {
//             var reply = proj.CompletedTasks.Dequeue();
//             // Assuming project actor ID is based on some convention. Using 0 for now.
//             Push(0, reply);
//         }

//         // 2. Request new work
//         float percentage = (float)(_totalShortfall > 0 ? proj.Shortfall / _totalShortfall : 0);
//         var request = new ClientRequestData(_actorId, (float)_powerGFlops, _config.ConnectionInterval);
//         Push(0, request);

//         // 3. Wait for reply
//         yield return new WaitUntil(() => _mailBox.Count > 0);

//         // 4. Process reply
//         var message = _mailBox.Dequeue();
//         if (message is ServerReplyData serverReply)
//         {
//             foreach (var workunit in serverReply.workunits)
//             {
//                 var newClientTask = new ClientTaskModel(workunit, proj);
//                 proj.Tasks.Enqueue(newClientTask);
//                 proj.TotalTasksReceived++;
//             }
//             Debug.Log($"Client {_actorId} received {serverReply.workunits.Count} new tasks from project {proj.Name}");
//         }
//     }

//     private void UpdateDebt()
//     {
//         double a = 0;
//         double sumPriorityRunProj = 0;
//         int numProjectShort = 0;
        
//         // Calculate 'a' and sum of priorities for runnable projects
//         foreach (var proj in _projects.Values)
//         {
//             a += proj.WallCpuTime;
//             if (proj.Tasks.Count > 0 || proj.RunningTasks.Count > 0)
//             {
//                 sumPriorityRunProj += proj.Priority;
//                 numProjectShort++;
//             }
//         }

//         // Update short and long term debt for each project
//         double totalDebtShort = 0;
//         foreach (var proj in _projects.Values)
//         {
//             double w = a * (proj.Priority / _sumPriority);
//             double w_short = (sumPriorityRunProj > 0) ? (a * (proj.Priority / sumPriorityRunProj)) : 0;

//             proj.ShortTermDebt += w_short - proj.WallCpuTime;
//             proj.LongTermDebt += w - proj.WallCpuTime;

//             if (proj.Tasks.Count == 0 && proj.RunningTasks.Count == 0)
//             {
//                 proj.ShortTermDebt = 0;
//             }
//             totalDebtShort += proj.ShortTermDebt;
//         }

//         // Normalize short-term debt
//         if (numProjectShort > 0)
//         {
//             foreach (var proj in _projects.Values)
//             {
//                 if (proj.Tasks.Count > 0 || proj.RunningTasks.Count > 0)
//                 {
//                     proj.ShortTermDebt -= (totalDebtShort / numProjectShort);
//                     if (proj.ShortTermDebt > MAX_SHORT_TERM_DEBT)
//                     {
//                         proj.ShortTermDebt = MAX_SHORT_TERM_DEBT;
//                     }
//                 }
//                 // Reset wall_cpu_time for the next interval
//                 proj.WallCpuTime = 0;
//             }
//         }
//     }

//     private void UpdateShortfall()
//     {
//         double totalTime = 0;
        
//         foreach (var proj in _projects.Values)
//         {
//             double totalTimeProj = 0;
            
//             // Sum remaining computation for tasks in the main queue and running tasks
//             totalTimeProj += proj.Tasks.Sum(task => task.RemainingGflops / _powerGFlops);
//             if (_activeTask != null && _activeTask.Project == proj)
//             {
//                 totalTimeProj += _activeTask.RemainingGflops / _powerGFlops;
//             }

//             totalTime += totalTimeProj;
            
//             proj.Shortfall = _config.ConnectionInterval * (proj.Priority / _sumPriority) - totalTimeProj;
//             if (proj.Shortfall < 0)
//             {
//                 proj.Shortfall = 0;
//             }
//         }

//         _totalShortfall = _config.ConnectionInterval - totalTime;
//         if (_totalShortfall < 0)
//         {
//             _totalShortfall = 0;
//         }
//     }

//     private ClientTaskModel SelectTaskToRun()
//     {
//         // This is a simplified version of the C code's task selection.
//         // It selects the task from the project with the highest debt.
//         ClientProject bestProj = null;
//         double maxDebt = double.MinValue;

//         foreach (var proj in _projects.Values)
//         {
//             if (proj.Tasks.Count > 0 && proj.LongTermDebt > maxDebt)
//             {
//                 maxDebt = proj.LongTermDebt;
//                 bestProj = proj;
//             }
//         }

//         if (bestProj != null && bestProj.Tasks.Count > 0)
//         {
//             return bestProj.Tasks.Dequeue();
//         }

//         return null;
//     }
// }