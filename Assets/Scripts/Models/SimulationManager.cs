#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.Collections;

public class SimulationManager : MonoBehaviour
{
    public static Coroutine StartRoutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }
    
    public static void StopRoutine(Coroutine routine)
    {
        Instance.StopCoroutine(routine);
    }

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
        var group = _simConfig.GroupConfig;
        for (int i = 0;i<group.NumberOfClients;i++)
        {
            _actors[_actorId++] = new ClientModel(group, _hostId);
            _hosts[_hostId++] = new HostModel(group);
        }
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

    private void Tick(int curTick)
    {
        Debug.Log($"tick: {curTick}");
        if (curTick == _maxSimulationTime)
            {
                QuitGame();
                return;
            }
    }

    void Awake()
    {
        TimeTickSystem.OnTick += Tick;
    //    TimeTickSystem.OnTick += (int tick) => {
    //         Debug.Log($"tick: {tick}");
    //     };


        if (Instance == null)  
        {        
            Instance = this;  
            DontDestroyOnLoad(gameObject);  
        }    
        else  
        {  
            Destroy(gameObject);  
        }
        
        _maxSimulationTime = _simConfig.SimLength;
        _actors = new BaseActor[DEMONS_NUMBER + _simConfig.GroupConfig.NumberOfClients];
        _hosts = new HostModel[_simConfig.GroupConfig.NumberOfClients];
        RandomUtils.SetSeed(_simConfig.GroupConfig.RandomConfig.DeterministicSeed);
    
        InitGroup();
        //InitProject();

    }

    private void OnDestroy() {
    // Обязательно отписываемся при уничтожении
        TimeTickSystem.OnTick -= Tick;
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
