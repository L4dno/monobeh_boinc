#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System;

public partial class SimulationManager : MonoBehaviour
{

    public static SimulationManager Instance {get; private set;}

    public Dictionary<string, BaseActor> Actors { get; private set; }
    private List<HostModel> hosts;
    public LinkModel Link {get; private set;} 

    [SerializeField] private SimConfig _config;
    
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

    private StatisticWriter statisticWriter;
    private void Start()
    {
        
        statisticWriter = new StatisticWriter(_config.ExperimentFolderName, _config);
        StartCoroutine(statisticWriter.WriteTaskCsv());
        StartCoroutine(statisticWriter.WriteHostCsv());
    
        // start all actors main loop coroutine
        foreach (var actor in Actors.Values)
        {
            StartCoroutine(actor.MainLoop());
        }
        
        }

    private int _maxSimulationTime;
    public int MaxSimulationTime => _maxSimulationTime;
    public int TotalClientsCount
    {
        get
        {
            return _config.GroupConfig.NumberOfClients;
        }
    }

    public float GetMeanHostSpeedGflops()
    {
        return 1.0f / _config.GroupConfig.RandomConfig.PowerA;
    }

    private void Tick(int curTick)
    {
        if (curTick % 3600 == 0)
        {
            Debug.Log($"tick: {curTick/3600}");
        }
        if (curTick == _maxSimulationTime)
            {
                // print statistics
                StopAllCoroutines();
                statisticWriter.WriteStats();
                QuitGame();
            }
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
        
        GlobalStats.TotalPower = 0;
        for (int i = 0; i < _config.GroupConfig.NumberOfClients; i++, hostId++)
        {
            float power = RandomUtils.GetDistribution(
                _config.GroupConfig.RandomConfig.HostPowerDistri,
                 _config.GroupConfig.RandomConfig.PowerA, 
                 _config.GroupConfig.RandomConfig.PowerB);
            power = Mathf.Clamp(power, _config.GroupConfig.MinSpeed, _config.GroupConfig.MaxSpeed);
            GlobalStats.TotalPower += (long)power;
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

    void Awake()
    {

        if (Instance == null)  
        {        
            Instance = this;  
        }    
        else  
        {  
            Destroy(gameObject);  
        }
        
        Actors = new Dictionary<string, BaseActor>();
        hosts = new List<HostModel>();

        TimeTickSystem.OnTick += Tick;

        RandomUtils.SetSeed(_config.DeterministicSeed);
        _maxSimulationTime = _config.SimLength * 3600;
        CreatePlatform();
        CreateDeployment();

    }

    private void QuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}