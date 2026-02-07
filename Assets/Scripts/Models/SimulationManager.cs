#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance {get; private set;}

    const int DEMONS_NUMBER = 4;
    private BaseActor[] _actors;

    [HideInInspector] public HostModel[] _hosts;

    private int _hostId = 0;
    private int _actorId = 0;

    [SerializeField] private SimConfig _simConfig;

    // inits 1 group
    private void InitGroup()
    {
        // var group = _simConfig.GroupConfig;
        // var randomConfig = group.RandomConfig;
        // for (int i = 0;i<group.NumberOfClients;i++)
        // {
        //     _actors[_actorId++] = new ClientModel(group, _hostId);
        //     _hosts[_hostId++] = new HostModel(group);
        // }
    }

    private void InitProject()
    {
        // var project = _simConfig.ProjectConfig;
        // // generator, validator, assimilator, scheduler
        // _actors[_actorId++] = new WorkGeneratorModel(_hostId);
        // _actors[_actorId++] = new ValidatorModel(_hostId);
        // _actors[_actorId++] = new AssimilatorModel(_hostId);
        // _actors[_actorId++] = new SchedulerModel(_hostId);
    }

    
    private int _maxSimulationTime;

    void Awake()
    {
        if (Instance == null)  
        {        
            Instance = this;  
            DontDestroyOnLoad(gameObject);  
        }    
        else  
        {  
            Destroy(gameObject);  
        }
        
        _maxSimulationTime = _simConfig.SimLength * 60 * 60;
        _actors = new BaseActor[DEMONS_NUMBER + _simConfig.GroupConfig.NumberOfClients];
        _hosts = new HostModel[_simConfig.GroupConfig.NumberOfClients];
        RandomUtils.SetSeed(_simConfig.GroupConfig.RandomConfig.DeterministicSeed);
    
        //InitGroup();
        //InitProject();

    }


    // Update is called once per frame
    void Update()
    {
        
        // for (int i = 0; i < _ticksPerFrame; i++)
        // {
        //     if (_curSimulationTime == _maxSimulationTime)
        //     {
        //         QuitGame();
        //         return;
        //     }

        //     _curSimulationTime++;

        //     // for actors
            
        // }
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
