#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.Collections;

public class SimulationManager : MonoBehaviour
{

    public static SimulationManager Instance {get; private set;}

    [HideInInspector] public BaseActor[] actors;
    [HideInInspector] public HostModel[] hosts;

    [HideInInspector] public LinkModel link;

    [SerializeField] private SimConfig _config;
    

    private void Start()
    {
        // start all actors main loop coroutine

        _maxSimulationTime = _config.SimLength * 3600;
    }

    private int _maxSimulationTime;

    private void Tick(int curTick)
    {
        //Debug.Log($"tick: {curTick}");
        if (curTick == _maxSimulationTime)
            {
                // print statistics
                QuitGame();
            }
    }

    void CreatePlatform()
    {
        // hosts 
        // links
        int totalHosts = _config.NumberOfProjects + _config.GroupConfig.NumberOfClients;
        int totalLinks = _config.NumberOfGroups;

        hosts = new HostModel[totalHosts];
        link = new LinkModel(_config.GroupConfig.ServerLatency, 
                        _config.GroupConfig.ServerBandwidth);

        int hostId = 0;

        for (; hostId < _config.NumberOfProjects; hostId++)
        {
            hosts[hostId] = new HostModel(_config.ProjectConfig.ServerPowerGflops, hostId);
        }
        for (; hostId < _config.GroupConfig.NumberOfClients; hostId++)
        {
            float power = RandomUtils.GetDistribution(
                _config.GroupConfig.RandomConfig.HostPowerDistri,
                 _config.GroupConfig.RandomConfig.PowerA, 
                 _config.GroupConfig.RandomConfig.PowerB);
            power = Mathf.Clamp(power, _config.GroupConfig.MinSpeed, _config.GroupConfig.MaxSpeed);
            hosts[hostId] = new HostModel(power, hostId);
        }
    }

    void CreateDeployment()
    {
        
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
