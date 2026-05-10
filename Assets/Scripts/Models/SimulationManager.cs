#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.ComponentModel;

public class SimulationManager : MonoBehaviour
{

    private const int StatisticsDumpInterval = 3600;
    private IStatSaver _statisticWriter;
    private TimeTickSystem _timeSystem;
    private SimConfig _config;

    public Dictionary<string, BaseActor> Actors { get; private set; }
    private List<HostModel> hosts;
    public LinkModel Link {get; private set;} 
    public float GridTotalPower {get; private set;}

        // надо сделать зависимым от эпизода
    private int _maxSimulationTime;
    public int MaxSimulationTime => _maxSimulationTime;
    
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

    private IEnumerator DumpStatisticsLoop()
    {
        _statisticWriter.Dump();
        while (true)
        {
            yield return new WaitForTicks(StatisticsDumpInterval);
            _statisticWriter.Dump();
        }
    }

    // все перенесем в энтри поинт
    public void Initialize()
    {
        _timeSystem = Container.Instance.TimeSystem;
        var configProvider = Container.Instance.ConfigProvider;
        _config = configProvider.SimConfig;

        Actors = new Dictionary<string, BaseActor>();
        hosts = new List<HostModel>();
        _statisticWriter = Container.Instance.StatSaver;

        
        // впоследствии здесь надо будет заменить на относительное время
        _maxSimulationTime = _config.SimLength * 3600;
        CreatePlatform();
        CreateDeployment();
        
    }

    public IEnumerator StartSimulation()
    {
        foreach (var actor in Actors.Values)
        {
            StartCoroutine(actor.MainLoop());
        }
        StartCoroutine(DumpStatisticsLoop());
        yield return new WaitForTicks(MaxSimulationTime);
        _statisticWriter.Dump();
        StopAllCoroutines();
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
