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
        // The user will reimplement this method to create HostModel objects.
        // Example:
        // hosts = new List<HostModel>();
        // hosts.Add(new HostModel(...));
        // Link = new LinkModel(...);
    }

    void CreateDeployment()
    {
        // The user will reimplement this method to create actor instances.
        // Example:
        // new ProjectModel(_config.ProjectConfig, 0, hosts[0]);
        // new ClientModel(_config.GroupConfig, projectConfigs, 0, hosts[1]);
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