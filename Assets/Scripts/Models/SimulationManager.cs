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
    // запуск дампа статистики надо вынести в энтри поинт
    private const int StatisticsDumpInterval = 3600;
    private IStatSaver _statisticWriter;

    private SimConfig _config;

    // мне надо передавать ссылку на себя во все хранящиеся внутри окружения акторы?
    public static SimulationManager Instance {get; private set;}

    public Dictionary<string, BaseActor> Actors { get; private set; }
    private List<HostModel> hosts;
    public LinkModel Link {get; private set;} 

    
    
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

    // разрешает зависимости внутренне тк монобех
    // все перенесем в энтри поинт
    private void Start()
    {
        var configProvider = Container.Instance.ConfigProvider;
        _config = configProvider.SimConfig;

        Actors = new Dictionary<string, BaseActor>();
        hosts = new List<HostModel>();

        TimeTickSystem.OnTick += Tick;

        RandomUtils.SetSeed(_config.DeterministicSeed);
        // впоследствии здесь надо будет заменить на относительное время
        _maxSimulationTime = _config.SimLength * 3600;
        CreatePlatform();
        CreateDeployment();

        // теперь в менеджере будет 1 почасовая корутина
        // которая вызывает дамп статов у стат сейвера
        // где надо выводить последний раз и закрывать файл??
        // там же где и создается файлы

        
        _statisticWriter = Container.Instance.StatSaver;
        StartCoroutine(DumpStatisticsLoop());
    
        // start all actors main loop coroutine
        foreach (var actor in Actors.Values)
        {
            StartCoroutine(actor.MainLoop());
        }
        
    }

    // надо сделать зависимым от эпизода
    private int _maxSimulationTime;
    public int MaxSimulationTime => _maxSimulationTime;

    // это вынесем
    private IEnumerator DumpStatisticsLoop()
    {
        _statisticWriter.Dump();
        while (true)
        {
            yield return new WaitForTicks(StatisticsDumpInterval);
            _statisticWriter.Dump();
        }
    }

    private void Tick(int curTick)
    {
        if (curTick == _maxSimulationTime)
            {
                // print statistics
                StopAllCoroutines();
                _statisticWriter.Dump();
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

    void Awake()
    {

        if (Instance == null)  
        {        
            Instance = this;  
        }    
        else  
        {  
            Destroy(gameObject);
            return;
        }
    }

    // это тоже убрать
    private void QuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
