using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProjectModel : BaseActor, IProjectStats
{

    public event Action OnWorkunitCreated;

    public event Action OnWorkunitCompleted;
    public string ProjectName => _config.ProjectName;
    public IReadOnlyDictionary<string, TaskModel> TaskDatabase => _taskDatabase;

    private readonly ProjectConfig _config;
    
    private readonly Dictionary<string, TaskModel> _taskDatabase = new Dictionary<string, TaskModel>();
    private readonly Queue<WorkunitData> _readyWorkQueue = new Queue<WorkunitData>();
    private readonly Queue<ClientReplyData> _validationQueue = new Queue<ClientReplyData>();
    private readonly Queue<WorkunitData> _errorWorkQueue = new Queue<WorkunitData>();
    private readonly Queue<TaskModel> _assimilationQueue = new Queue<TaskModel>();

    public int TasksCreated = 0;

    public ProjectModel(ProjectConfig config, int actorId, HostModel host) : base($"project{actorId}", host)
    {
        _config = config;
        Container.Instance.StatService.RegisterProject(this);
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
                Debug.Log($"[{ActorName}] Received message: {message.GetType().Name}");
                if (message is ClientRequestData request)
                {
                    yield return ProcessWorkRequest(request);
                }
                else if (message is ClientReplyData reply)
                {
                    Debug.Log($"[{ActorName}] Received reply for {reply.WorkunitName} from {reply.ClientName}, enqueuing for validation.");
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
                
                foreach (var workunit in workToSend)
                {
                    if (_taskDatabase.TryGetValue(workunit.ParentTaskName, out var task))
                    {
                        workunit.deadlineTick = TimeTickSystem.Instance.CurTick + task.DelayBound;
                    }
                }

                _readyWorkQueue.Clear();
                while (newQueue.Any())
                {
                    _readyWorkQueue.Enqueue(newQueue.Dequeue());
                }
            }
        }

        
        var reply = new ServerReplyData(workToSend);
        Debug.Log($"[{ActorName}] Sending {workToSend.Count} workunits to {request.RequesterName}. Ready queue size: {_readyWorkQueue.Count}");
        yield return Push(request.RequesterName, reply);
    }

    private IEnumerator TaskGeneratorLoop()
    {
        Debug.Assert(_config.TaskConfigs.Any(), $"ProjectConfig '{_config.name}' has no TaskConfigs assigned.");

        // TODO: расчитать долю каждого проекта

        int applicationsCount = _config.TaskConfigs.Count;
        Debug.LogWarning($"apps count is {applicationsCount}");

        int DAY_CYCLE_FACTOR = SimulationManager.Instance.MaxSimulationTime / 
        3600 / 24;

        float totalSimulatedGflops = 1000;//SimulationManager.Instance.GridTotalPower * 
            //SimulationManager.Instance.MaxSimulationTime / DAY_CYCLE_FACTOR;
        Debug.LogWarning($"total gflops for all {totalSimulatedGflops}");

        float gflopsPerApplication = totalSimulatedGflops / applicationsCount;
        Debug.LogWarning($"flops per app {gflopsPerApplication}");

        List<int> InitialTasksPerApp = new List<int>();
        foreach (var taskConfig in _config.TaskConfigs)
        {
            float mean = (taskConfig.MinTaskGflops + taskConfig.MaxTaskGflops) / 2f;
            int InitialTaskCount = Mathf.CeilToInt(gflopsPerApplication / mean / taskConfig.InitialCreatedWorkunits);
            InitialTasksPerApp.Add(InitialTaskCount);
            Debug.LogWarning($"number of tasks for new group is {InitialTaskCount}");
        }

        // заполнить каждым конфигом массив соответствующего размера
        //InitialTasksPerApp = new List<int>{2000};
        for (int i = 0; i < applicationsCount; i++)
        {
            for (int j = 0; j < InitialTasksPerApp[i]; j++)
            {
                var taskName = $"Task-{TasksCreated}";
                // TODO: сделать соотношение между несколькими проектами
                var selectedTaskConfig = _config.TaskConfigs[i];
                var task = new TaskModel(taskName, selectedTaskConfig);
                _taskDatabase.Add(taskName, task);
                TasksCreated++;
                OnWorkunitCreated?.Invoke();
            }
            
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
                Debug.Log($"[{ActorName}] Validator dequeued reply for {reply.WorkunitName}.");

                if (_taskDatabase.TryGetValue(reply.WorkunitName, out var task))
                {
                                    Debug.Log($"Searching for ResultId {reply.ResultId} in task '{task.Name}' which has {task.Workunits.Count} workunits: [{string.Join(", ", task.Workunits.Select(w => w.workunitId))}]");
                                    var workunit = task.Workunits.FirstOrDefault(w => w.workunitId == reply.ResultId);
                                    if (workunit == null)
                                    {
                                        Debug.LogError($"Workunit with ResultId {reply.ResultId} not found for task '{task.Name}'.");
                                        continue;
                                    }
                    task.ReceivedResults++;

                    var isTimeout = workunit.deadlineTick < TimeTickSystem.Instance.CurTick;
                    
                    if (isTimeout)
                    {
                    }

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
                        if (task.ValidResults >= task.MinQuorum)
                        {
                            task.CurrentState = TaskModel.State.Valid;
                            isFinished = true;
                        }
                        else if (task.ErrorResults >= task.Config.MaxErrorWorkunits ||
                                 task.SuccessResults >= task.Config.MaxSuccessWorkunits ||
                                 task.Workunits.Count >= task.Config.MaxCreatedWorkunits)
                        {
                            task.CurrentState = TaskModel.State.Error;
                            isFinished = true;
                        }

                        if (isFinished)
                        {
                            Debug.Log($"[{ActorName}] Task {task.Name} finished with state {task.CurrentState}. Enqueuing for assimilation.");
                            _assimilationQueue.Enqueue(task);
                        }
                        else if(isTimeout || reply.status == WorkunitStatus.Fail)
                        {
                           
                            _errorWorkQueue.Enqueue(workunit);
                        }
                    }
                    else if (task.CurrentState == TaskModel.State.Valid && reply.status == WorkunitStatus.Success && reply.result == WorkunitResult.Correct)
                    {
                    }
                }
                else
                {
                    // This is a late reply for an already assimilated task.
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
                Debug.Log($"[{ActorName}] Assimilating task {taskToAssimilate.Name}. Firing OnWorkunitCompleted event.");

              
                if (taskToAssimilate.CurrentState == TaskModel.State.Valid)
                {
                }
                else
                {
                }
                _taskDatabase.Remove(taskToAssimilate.Name);
                OnWorkunitCompleted?.Invoke();
            }
            
            yield return new WaitForTicks(100); 
        }
    }
}