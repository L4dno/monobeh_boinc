using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProjectModel : BaseActor
{
    public string ProjectName => _config.ProjectName;
    public IReadOnlyDictionary<string, TaskModel> TaskDatabase => _taskDatabase;

    public int StatTasksValid = 0;
    public int StatTasksError = 0;

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
        SimulationManager.Instance.StartCoroutine(TaskGeneratorLoop());
        SimulationManager.Instance.StartCoroutine(ResultGeneratorLoop());
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
        var sentTaskNames = new HashSet<string>();
        
        if (_readyWorkQueue.Count > 0)
        {
            float totalDuration = 0;

            foreach (var candidate in _readyWorkQueue)
            {
                if (sentTaskNames.Contains(candidate.ParentTaskName))
                {
                    continue;
                }
                
                float workunitDuration = candidate.durationInFlops / request.Power;

                if (totalDuration + workunitDuration <= request.Percentage)
                {
                    totalDuration += workunitDuration;
                    workToSend.Add(candidate);
                    sentTaskNames.Add(candidate.ParentTaskName);
                }
            }
            
            if (workToSend.Count == 0 && _readyWorkQueue.Count > 0)
            {
                workToSend.Add(_readyWorkQueue.First());
            }
            
            if (workToSend.Any())
            {
                var workToSendSet = new HashSet<WorkunitData>(workToSend);
                var newQueue = new Queue<WorkunitData>(_readyWorkQueue.Where(w => !workToSendSet.Contains(w)));
                
                _readyWorkQueue.Clear();
                while (newQueue.Any())
                {
                    _readyWorkQueue.Enqueue(newQueue.Dequeue());
                }
            }
        }

        
        var reply = new ServerReplyData(workToSend);
        yield return Push(request.RequesterName, reply);
    }

    private IEnumerator TaskGeneratorLoop()
    {
        while (_tasksCreated < _config.InitialTaskCount)
        {
            var taskName = $"Task-{_tasksCreated}";
            var task = new TaskModel(taskName, _config);
            _taskDatabase.Add(taskName, task);
            _tasksCreated++;
        }
        yield break; 
    }

    private IEnumerator ResultGeneratorLoop()
    {
        
        yield return null;

       
        foreach (var task in _taskDatabase.Values)
        {
           
            while (task.CanCreateInitialWork())
            {
                _readyWorkQueue.Enqueue(task.CreateWorkunit());
            }
        }

       
        while (true)
        {
            if (_errorWorkQueue.Count > 0)
            {
                var workunitToRecreate = _errorWorkQueue.Dequeue();
                if (_taskDatabase.TryGetValue(workunitToRecreate.ParentTaskName, out var task))
                {
                    if (task.CanCreateMoreWork()) // Check against absolute max
                    {
                        _readyWorkQueue.Enqueue(task.CreateWorkunit());
                    }
                }
                // Yield to process one per frame to avoid freezing if the error queue is large
                yield return null; 
            }
            else
            {
                
                yield return new WaitForTicks(10);
            }
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
                        bool isFinished = false;
                        if (task.ValidResults >= _config.MinQuorum)
                        {
                            task.CurrentState = TaskModel.State.Valid;
                            isFinished = true;
                        }
                        else if (task.ErrorResults >= _config.TaskConfig.MaxErrorWorkunits || 
                                 task.SuccessResults >= _config.TaskConfig.MaxSuccessWorkunits ||
                                 task.Workunits.Count >= _config.TaskConfig.MaxCreatedWorkunits)
                        {
                            task.CurrentState = TaskModel.State.Error;
                            isFinished = true;
                        }

                        if (isFinished)
                        {
                            _assimilationQueue.Enqueue(task);
                        }
                        else if(isTimeout || reply.status == WorkunitStatus.Fail)
                        {
                           
                            _errorWorkQueue.Enqueue(workunit);
                        }
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

              
                if (taskToAssimilate.CurrentState != TaskModel.State.InProgress &&
                    taskToAssimilate.ReceivedResults >= taskToAssimilate.Workunits.Count)
                {
                    if (taskToAssimilate.CurrentState == TaskModel.State.Valid)
                    {
                        StatTasksValid++;
                    }
                    else
                    {
                        StatTasksError++;
                    }
                    _taskDatabase.Remove(taskToAssimilate.Name);
                }
                else
                {
                
                    _assimilationQueue.Enqueue(taskToAssimilate);
                }
            }
            
            yield return new WaitForTicks(100); 
        }
    }
}