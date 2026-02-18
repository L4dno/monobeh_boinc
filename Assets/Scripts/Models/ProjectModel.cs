using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProjectModel : BaseActor
{
    public string ProjectName => _config.ProjectName;
    public IReadOnlyDictionary<string, TaskModel> TaskDatabase => _taskDatabase;

    private readonly ProjectConfig _config;
    
    private readonly Dictionary<string, TaskModel> _taskDatabase = new Dictionary<string, TaskModel>();
    private readonly Queue<WorkunitData> _readyWorkQueue = new Queue<WorkunitData>();
    private readonly Queue<ClientReplyData> _validationQueue = new Queue<ClientReplyData>();
    private readonly Queue<WorkunitData> _errorWorkQueue = new Queue<WorkunitData>();
    private readonly Queue<TaskModel> _assimilationQueue = new Queue<TaskModel>();
    private int _tasksCreated = 0;

    public ProjectModel(ProjectConfig config, int actorId, HostModel host) : base($"project{actorId}", host)
    {
        _config = config;
    }

    public override IEnumerator MainLoop()
    {
        SimulationManager.Instance.StartCoroutine(WorkGeneratorLoop());
        SimulationManager.Instance.StartCoroutine(ValidatorLoop());
        SimulationManager.Instance.StartCoroutine(AssimilatorLoop());
        yield return RequestDispatcherLoop();
    }

    private IEnumerator RequestDispatcherLoop()
    {
        while (true)
        {
            var message = Receive();
            if (message != null)
            {
                if (message is ClientRequestData request)
                {
                    yield return ProcessWorkRequest(request);
                }
                else if (message is ClientReplyData reply)
                {
                    _validationQueue.Enqueue(reply);
                }
            }
            else
            {
                yield return new WaitForTicks(1);
            }
        }
    }

    private IEnumerator ProcessWorkRequest(ClientRequestData request)
    {
        var workToSend = new List<WorkunitData>();
        if (_readyWorkQueue.Count > 0)
        {
            workToSend.Add(_readyWorkQueue.Dequeue());
        }

        if (workToSend.Any())
        {
            var reply = new ServerReplyData(workToSend);
            yield return Push(request.RequesterName, reply);
        }
    }

    private IEnumerator WorkGeneratorLoop()
    {
        while (true)
        {
            while (_errorWorkQueue.Count > 0)
            {
                var workunitToRecreate = _errorWorkQueue.Dequeue();
                var task = _taskDatabase[workunitToRecreate.ParentTaskName];
                _readyWorkQueue.Enqueue(task.CreateWorkunit());
            }
            
            if (_tasksCreated < _config.InitialTaskCount)
            {
                var taskName = $"Task-{_tasksCreated}";
                var task = new TaskModel(taskName, _config);
                _taskDatabase.Add(taskName, task);
                _tasksCreated++;
            }

            foreach (var task in _taskDatabase.Values)
            {
                if (_readyWorkQueue.Count < _config.MaxReadyWork && task.CanCreateMoreWork())
                {
                    _readyWorkQueue.Enqueue(task.CreateWorkunit());
                }
            }
            
            yield return new WaitForTicks(10);
        }
    }

    private IEnumerator ValidatorLoop()
    {
        while (true)
        {
            if (_validationQueue.Count > 0)
            {
                var reply = _validationQueue.Dequeue();
                
                if (_taskDatabase.TryGetValue(reply.WorkunitName, out var task))
                {
                    var workunit = task.Workunits.FirstOrDefault(w => w.workunitId == reply.ResultId);
                    if (workunit == null) continue;

                    task.ReceivedResults++;

                    var isTimeout = workunit.deadlineTick < TimeTickSystem.Instance.CurTick;
                    
                    if (reply.status == WorkunitStatus.Success && !isTimeout)
                    {
                        task.SuccessResults++;
                        if (reply.result == WorkunitResult.Correct)
                        {
                            task.ValidResults++;
                        }
                    }
                    else
                    {
                        task.ErrorResults++;
                    }

                    if (task.CurrentState == TaskModel.State.InProgress)
                    {
                        if (task.ValidResults >= _config.MinQuorum)
                        {
                            task.CurrentState = TaskModel.State.Valid;
                        }
                        else if (task.ErrorResults >= _config.TaskConfig.MaxErrorWorkunits || 
                                 task.SuccessResults >= _config.TaskConfig.MaxSuccessWorkunits ||
                                 task.Workunits.Count >= _config.TaskConfig.MaxWorkunits)
                        {
                            task.CurrentState = TaskModel.State.Error;
                        }
                        else if(isTimeout || reply.status == WorkunitStatus.Fail)
                        {
                            // Re-issue work
                            _errorWorkQueue.Enqueue(workunit);
                        }
                    }
                    
                    // If the task is finished, queue it for assimilation
                    if (task.CurrentState != TaskModel.State.InProgress)
                    {
                        _assimilationQueue.Enqueue(task);
                    }
                }
            }
            else
            {
                yield return new WaitForTicks(1);
            }
        }
    }

    private IEnumerator AssimilatorLoop()
    {
        while (true)
        {
            if (_assimilationQueue.Count > 0)
            {
                var taskToAssimilate = _assimilationQueue.Dequeue();

                // A task is fully complete and can be removed if its state is final
                // AND all the workunits it ever created have reported back.
                if (taskToAssimilate.CurrentState != TaskModel.State.InProgress &&
                    taskToAssimilate.ReceivedResults >= taskToAssimilate.Workunits.Count)
                {
                    _taskDatabase.Remove(taskToAssimilate.Name);
                }
                else
                {
                    // Not all results are back yet, put it back in the queue for later checking.
                    _assimilationQueue.Enqueue(taskToAssimilate);
                }
            }
            
            yield return new WaitForTicks(100); // Check every 100 ticks
        }
    }
}