#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.Collections;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{

    public static SimulationManager Instance {get; private set;}

    [HideInInspector] public Dictionary<string, MailBox> mails; 
    private List<BaseActor> actors;
    private List<HostModel> hosts;
    public LinkModel Link {get; private set;} 

    [SerializeField] private SimConfig _config;
    

    private void Start()
    {
        // start all actors main loop coroutine
        for (int i = 0; i < actors.Count; i++)
        {
            StartCoroutine(actors[i].MainLoop(hosts[i]));
        }
        
    }

    private int _maxSimulationTime;

    private void Tick(int curTick)
    {
        //Debug.Log($"tick: {curTick}");
        if (curTick == _maxSimulationTime)
            {
                // print statistics
                StopAllCoroutines();
                QuitGame();
            }
    }

    void CreatePlatform()
    {
        // hosts 
        // links

    //     hosts = new HostModel[totalHosts];
    //     link = new LinkModel(_config.GroupConfig.ServerLatency, 
    //                     _config.GroupConfig.ServerBandwidth);

    //     int hostId = 0;

    //     for (; hostId < _config.NumberOfProjects; hostId++)
    //     {
    //         hosts[hostId] = new HostModel(_config.ProjectConfig.ServerPowerGflops, hostId);
    //     }
    //     for (; hostId < _config.GroupConfig.NumberOfClients; hostId++)
    //     {
    //         float power = RandomUtils.GetDistribution(
    //             _config.GroupConfig.RandomConfig.HostPowerDistri,
    //              _config.GroupConfig.RandomConfig.PowerA, 
    //              _config.GroupConfig.RandomConfig.PowerB);
    //         power = Mathf.Clamp(power, _config.GroupConfig.MinSpeed, _config.GroupConfig.MaxSpeed);
    //         hosts[hostId] = new HostModel(power, hostId);
    //     }
    }

    void CreateDeployment()
    {
        // int actorId = 0;
        // for (; actorId < _config.NumberOfProjects; actorId++)
        // {
        //     actors[actorId] = new ProjectModel(_config.ProjectConfig, actorId);
        // }
        // for (;actorId < totalActors; actorId++)
        // {
        //     actors[actorId] = new ClientModel(_config.GroupConfig, actorId);
        // }

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
