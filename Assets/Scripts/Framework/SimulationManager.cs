#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.Collections;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
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

    private void Start()
    {
        // start all actors main loop coroutine
        foreach (var actor in Actors.Values)
        {
            StartCoroutine(actor.MainLoop());
        }
    }

    private int _maxSimulationTime;

    private void Tick(int curTick)
    {
        Debug.Log($"tick: {curTick}");
        if (curTick == _maxSimulationTime)
            {
                // print statistics
                StopAllCoroutines();
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
        for (int i = 0; i < _config.GroupConfig.NumberOfClients; i++, hostId++)
        {
            float power = RandomUtils.GetDistribution(
                _config.GroupConfig.RandomConfig.HostPowerDistri,
                 _config.GroupConfig.RandomConfig.PowerA, 
                 _config.GroupConfig.RandomConfig.PowerB);
            power = Mathf.Clamp(power, _config.GroupConfig.MinSpeed, _config.GroupConfig.MaxSpeed);
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