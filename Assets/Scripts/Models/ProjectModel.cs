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
    public IReadOnlyDictionary<string, WorkunitModel> WorkunitDatabase => _workunitDatabase;

    private readonly ProjectConfig _config;
    
    private readonly Dictionary<string, WorkunitModel> _workunitDatabase = new Dictionary<string, WorkunitModel>();
    private readonly Queue<ResultData> _readyResultsQueue = new Queue<ResultData>();
    private readonly Queue<ClientReplyData> _validationQueue = new Queue<ClientReplyData>();
    private readonly Queue<ResultData> _errorResultsQueue = new Queue<ResultData>();
    private readonly Queue<WorkunitModel> _assimilationQueue = new Queue<WorkunitModel>();
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;

    public int WorkunitsCreated = 0;

    public ProjectModel(ProjectConfig config, int actorId, HostModel host) : base($"project{actorId}", host)
    {
        _config = config;
        Container.Instance.StatService.RegisterProject(this);
    }

    public override IEnumerator MainLoop()
    {
        SimManager.StartCoroutine(WorkunitGeneratorLoop());
        SimManager.StartCoroutine(ResultGeneratorLoop());
        SimManager.StartCoroutine(ValidatorLoop());
        SimManager.StartCoroutine(AssimilatorLoop());
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
        var resultsToSend = new List<ResultData>();
        var sentWorkunitNames = new HashSet<string>();
        
        if (_readyResultsQueue.Count > 0)
        {
            float totalDuration = 0;

            foreach (var candidate in _readyResultsQueue)
            {
                if (sentWorkunitNames.Contains(candidate.WorkunitName))
                {
                    continue;
                }
                
                float resultDuration = candidate.durationInFlops / request.Power;

                if (totalDuration + resultDuration <= request.Percentage)
                {
                    totalDuration += resultDuration;
                    resultsToSend.Add(candidate);
                    sentWorkunitNames.Add(candidate.WorkunitName);
                }
            }
            
            if (resultsToSend.Count == 0 && _readyResultsQueue.Count > 0)
            {
                resultsToSend.Add(_readyResultsQueue.First());
            }
            
            if (resultsToSend.Any())
            {
                var resultsToSendSet = new HashSet<ResultData>(resultsToSend);
                var newQueue = new Queue<ResultData>(_readyResultsQueue.Where(w => !resultsToSendSet.Contains(w)));
                
                foreach (var result in resultsToSend)
                {
                    if (_workunitDatabase.TryGetValue(result.WorkunitName, out var workunit))
                    {
                        result.deadlineTick = TimeSystem.CurTick + workunit.DelayBound;
                    }
                }

                _readyResultsQueue.Clear();
                while (newQueue.Any())
                {
                    _readyResultsQueue.Enqueue(newQueue.Dequeue());
                }
            }
        }

        
        var reply = new ServerReplyData(resultsToSend);
        Debug.Log($"[{ActorName}] Sending {resultsToSend.Count} results to {request.RequesterName}. Ready queue size: {_readyResultsQueue.Count}");
        yield return Push(request.RequesterName, reply);
    }

    private IEnumerator WorkunitGeneratorLoop()
    {
        Debug.Assert(_config.WorkunitConfigs.Any(), $"ProjectConfig '{_config.name}' has no WorkunitConfigs assigned.");

        // TODO: расчитать долю каждого проекта

        int applicationsCount = _config.WorkunitConfigs.Count;
        Debug.LogWarning($"apps count is {applicationsCount}");

        int DAY_CYCLE_FACTOR = SimManager.MaxSimulationTime / 
        3600 / 24;

        float totalSimulatedGflops = SimManager.GridTotalPower * 
            SimManager.MaxSimulationTime / DAY_CYCLE_FACTOR;
        Debug.LogWarning($"total gflops for all {totalSimulatedGflops}");

        float gflopsPerApplication = totalSimulatedGflops / applicationsCount;
        Debug.LogWarning($"flops per app {gflopsPerApplication}");

        List<int> InitialWorkunitsPerApp = new List<int>();
        foreach (var workunitConfig in _config.WorkunitConfigs)
        {
            float mean = (workunitConfig.MinWorkunitGflops + workunitConfig.MaxWorkunitGflops) / 2f;
            int InitialWorkunitCount = Mathf.CeilToInt(gflopsPerApplication / mean / workunitConfig.InitialCreatedResults);
            InitialWorkunitsPerApp.Add(InitialWorkunitCount);
            Debug.LogWarning($"number of workunits for new group is {InitialWorkunitCount}");
        }

        // заполнить каждым конфигом массив соответствующего размера
        //InitialWorkunitsPerApp = new List<int>{2000};
        for (int i = 0; i < applicationsCount; i++)
        {
            for (int j = 0; j < InitialWorkunitsPerApp[i]; j++)
            {
                var workunitName = $"Workunit-{WorkunitsCreated}";
                // TODO: сделать соотношение между несколькими проектами
                var selectedWorkunitConfig = _config.WorkunitConfigs[i];
                var workunit = new WorkunitModel(workunitName, selectedWorkunitConfig);
                _workunitDatabase.Add(workunitName, workunit);
                WorkunitsCreated++;
                OnWorkunitCreated?.Invoke();
            }
            
        }
        yield break; 
    }

    private IEnumerator ResultGeneratorLoop()
    {
        
        yield return null;

       
        foreach (var workunit in _workunitDatabase.Values)
        {
           
            while (workunit.CanCreateInitialResults())
            {
                _readyResultsQueue.Enqueue(workunit.CreateResult());
            }
        }

       
        while (true)
        {
            if (_errorResultsQueue.Count > 0)
            {
                var resultToRecreate = _errorResultsQueue.Dequeue();
                if (_workunitDatabase.TryGetValue(resultToRecreate.WorkunitName, out var workunit))
                {
                    if (workunit.CanCreateMoreResults()) // Check against absolute max
                    {
                        _readyResultsQueue.Enqueue(workunit.CreateResult());
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

                if (_workunitDatabase.TryGetValue(reply.WorkunitName, out var workunit))
                {
                                    Debug.Log($"Searching for ResultNumber {reply.ResultNumber} in workunit '{workunit.Name}' which has {workunit.Results.Count} results: [{string.Join(", ", workunit.Results.Select(w => w.resultNumber))}]");
                                    var result = workunit.Results.FirstOrDefault(w => w.resultNumber == reply.ResultNumber);
                                    if (result == null)
                                    {
                                        Debug.LogError($"Result with ResultNumber {reply.ResultNumber} not found for workunit '{workunit.Name}'.");
                                        continue;
                                    }
                    workunit.ReceivedResults++;

                    var isTimeout = result.deadlineTick < TimeSystem.CurTick;
                    
                    if (isTimeout)
                    {
                    }

                    if (reply.status == ResultStatus.Success && !isTimeout)
                    {
                        workunit.SuccessResults++;
                        if (reply.value == ResultValue.Correct)
                        {
                            workunit.ValidResults++;
                        }
                    }
                    else
                    {
                        workunit.ErrorResults++;
                    }

                    if (workunit.CurrentState == WorkunitModel.State.InProgress)
                    {
                        bool isFinished = false;
                        if (workunit.ValidResults >= workunit.MinQuorum)
                        {
                            workunit.CurrentState = WorkunitModel.State.Valid;
                            isFinished = true;
                        }
                        else if (workunit.ErrorResults >= workunit.Config.MaxErrorResults ||
                                 workunit.SuccessResults >= workunit.Config.MaxSuccessResults ||
                                 workunit.Results.Count >= workunit.Config.MaxCreatedResults)
                        {
                            workunit.CurrentState = WorkunitModel.State.Error;
                            isFinished = true;
                        }

                        if (isFinished)
                        {
                            Debug.Log($"[{ActorName}] Workunit {workunit.Name} finished with state {workunit.CurrentState}. Enqueuing for assimilation.");
                            _assimilationQueue.Enqueue(workunit);
                        }
                        else if(isTimeout || reply.status == ResultStatus.Fail)
                        {
                           
                            _errorResultsQueue.Enqueue(result);
                        }
                    }
                    else if (workunit.CurrentState == WorkunitModel.State.Valid && reply.status == ResultStatus.Success && reply.value == ResultValue.Correct)
                    {
                    }
                }
                else
                {
                    // This is a late reply for an already assimilated workunit.
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
                var workunitToAssimilate = _assimilationQueue.Dequeue();
                Debug.Log($"[{ActorName}] Assimilating workunit {workunitToAssimilate.Name}. Firing OnWorkunitCompleted event.");

              
                if (workunitToAssimilate.CurrentState == WorkunitModel.State.Valid)
                {
                }
                else
                {
                }
                _workunitDatabase.Remove(workunitToAssimilate.Name);
                OnWorkunitCompleted?.Invoke();
            }
            
            yield return new WaitForTicks(100); 
        }
    }
}
