#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Unity.MLAgents;

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.ComponentModel;

public class SimulationManager : MonoBehaviour
{

    public event Action OnSimulationFinished;
    private IStatSaver _statisticWriter;
    private TimeTickSystem _timeSystem;
    private SimConfig _config;
    private bool _finishRequested;

    public Dictionary<string, BaseActor> Actors { get; private set; }
    private List<HostModel> hosts;
    private readonly List<Coroutine> _simulationCoroutines = new List<Coroutine>();
    public LinkModel Link {get; private set;} 
    public float GridTotalPower {get; private set;}

        // надо сделать зависимым от эпизода
    private int _maxSimulationTime;
    public int MaxSimulationTime => _maxSimulationTime;

    public Coroutine StartSimulationCoroutine(IEnumerator routine)
    {
        var coroutine = StartCoroutine(routine);
        _simulationCoroutines.Add(coroutine);
        return coroutine;
    }

    public void StopSimulationCoroutine(Coroutine coroutine)
    {
        if (coroutine == null)
        {
            return;
        }

        StopCoroutine(coroutine);
        _simulationCoroutines.Remove(coroutine);
    }

    public void ForgetSimulationCoroutine(Coroutine coroutine)
    {
        if (coroutine == null)
        {
            return;
        }

        _simulationCoroutines.Remove(coroutine);
    }

    public void RequestSimulationFinish()
    {
        _finishRequested = true;
    }
    
    public void RegisterActor(BaseActor actor)
    {
        if (!Actors.ContainsKey(actor.ActorName))
        {
            Actors.Add(actor.ActorName, actor);
        }
        else
        {
            Debug.LogError($"Actor with name {actor.ActorName} already registered.");
        }
    }

    // все перенесем в энтри поинт
    public void Initialize()
    {
        _timeSystem = Container.Instance.TimeSystem;
        var configProvider = Container.Instance.ConfigProvider;
        _config = configProvider.SimConfig;
        _finishRequested = false;
        _simulationCoroutines.Clear();

        Actors = new Dictionary<string, BaseActor>();
        hosts = new List<HostModel>();
        _statisticWriter = Container.Instance.StatSaver;
        Link = null;
        GridTotalPower = 0;

        
        // впоследствии здесь надо будет заменить на относительное время
        _maxSimulationTime = _config.SimLength * 3600;
        Container.Instance.StatService.Initialize(_maxSimulationTime, _config);
        CreatePlatform();
        CreateDeployment();
        
    }

    public IEnumerator StartSimulation()
    {
        foreach (var actor in Actors.Values)
        {
            StartSimulationCoroutine(actor.MainLoop());
        }
        yield return new WaitUntil(() => _finishRequested || _timeSystem.CurTick >= MaxSimulationTime);
        StopSimulationCoroutines();
        Container.Instance.StatService.RecordSimulationFinished(_timeSystem.CurTick);
        RecordTrainingStats();
        _statisticWriter.Dump();
        OnSimulationFinished?.Invoke();
    }

    private void RecordTrainingStats()
    {
        if (!EntryPoint.Instance.IsTrainingWorker)
        {
            return;
        }

        var stats = Container.Instance.StatService.GetStats();
        int tailStartTick = stats.TailStartTick >= 0 ? stats.TailStartTick : stats.FinishTick;
        int tailMakespan = Mathf.Max(stats.FinishTick - tailStartTick, 0);
        float deadlineMissRate = stats.ResultsAnalyzed > 0 ? stats.ResultsTooLate / (float)stats.ResultsAnalyzed : 0;
        Academy.Instance.StatsRecorder.Add("Tail/Makespan", tailMakespan, StatAggregationMethod.Average);
        Academy.Instance.StatsRecorder.Add("Tail/DeadlineMissRate", deadlineMissRate, StatAggregationMethod.Average);
        Academy.Instance.StatsRecorder.Add("Tail/ErrorWorkunits", stats.WorkunitsError, StatAggregationMethod.Average);
        Academy.Instance.StatsRecorder.Add("Tail/ValidWorkunits", stats.WorkunitsValid, StatAggregationMethod.Average);
    }

    private void StopSimulationCoroutines()
    {
        foreach (var coroutine in _simulationCoroutines.ToArray())
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        _simulationCoroutines.Clear();
    }



    void CreatePlatform()
    {
        int totalHosts = _config.NumberOfProjects + _config.GroupConfig.NumberOfClients;
        hosts = new List<HostModel>(totalHosts);
        Link = new LinkModel(_config.GroupConfig.ServerLatency, 
                        _config.GroupConfig.ServerBandwidth);

        int hostId = 0;

        for (int i = 0; i < _config.NumberOfProjects; i++, hostId++)
        {
            hosts.Add(new HostModel(_config.ProjectConfig.ServerPowerGflops, hostId));
        }
        
        GridTotalPower = 0;
        for (int i = 0; i < _config.GroupConfig.NumberOfClients; i++, hostId++)
        {
            float power = RandomUtils.GetDistribution(
                _config.GroupConfig.RandomConfig.HostPowerDistri,
                 _config.GroupConfig.RandomConfig.PowerA, 
                 _config.GroupConfig.RandomConfig.PowerB);
            power = Mathf.Clamp(power, _config.GroupConfig.MinSpeed, _config.GroupConfig.MaxSpeed);
            GridTotalPower += power;
            Container.Instance.StatService.RecordHostPower(power);
            // здесь отправляем событие для подсчета всей мощности грида
            hosts.Add(new HostModel(power, hostId));
        }
    }

    void CreateDeployment()
    {
        int actorId = 0;
        for (int i = 0; i < _config.NumberOfProjects; i++, actorId++)
        {
            new ProjectModel(_config.ProjectConfig, i, hosts[actorId]);
        }
        
        ProjectConfig[] projectConfigs = { _config.ProjectConfig };
        for (int i = 0; i < _config.GroupConfig.NumberOfClients; i++, actorId++)
        {
            new ClientModel(_config.GroupConfig, projectConfigs, i, hosts[actorId]);
        }
    }

}
