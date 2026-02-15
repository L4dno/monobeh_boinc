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

    private int _hostId = 0;
    private int _actorId = 0;

    [SerializeField] private SimConfig _simConfig;

    // inits 1 group
    private void InitGroup()
    {
        // var group = _simConfig.GroupConfig;
        // for (int i = 0;i<group.NumberOfClients;i++)
        // {
        //     actors[_actorId++] = new ClientModel(group, _hostId);
        //     hosts[_hostId++] = new HostModel(group);
        // }
    }

    private void InitProject()
    {
        //var project = _simConfig.ProjectConfig;
        
        //actors[_actorId++] = new ProjectModel(project, _hostId);
        //hosts[_hostId++] = new HostModel(project);
        // actors[_actorId++] = new ValidatorModel(_hostId);
        // actors[_actorId++] = new AssimilatorModel(_hostId);
        // actors[_actorId++] = new SchedulerModel(_hostId);
    }

    
    

    private void Start()
    {
        _maxSimulationTime = _simConfig.SimLength * 3600;
        //actors = new BaseActor[_simConfig.TotalActors];
        //hosts = new HostModel[_simConfig.TotalActors];
        //InitGroup();
        //InitProject();
    }

    private int _maxSimulationTime;

    private void Tick(int curTick)
    {
        Debug.Log($"tick: {curTick}");
        if (curTick == _maxSimulationTime)
            {
                QuitGame();
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
        
        TimeTickSystem.OnTick += Tick;

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
