#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.MLAgents;
using UnityEngine;
using System.Collections;

public class EntryPoint : MonoBehaviour
{
    [SerializeField] private bool restartSimulationOnFinish;
    public static EntryPoint Instance { get; private set; }
    public bool RestartSimulationOnFinish => restartSimulationOnFinish;
    public bool IsTrainingWorker { get; private set; }
    public int EpisodeIndex { get; private set; }
    public int CurrentSeed { get; private set; }

    // единственный метод старт в игре
    private IEnumerator Start()
    {
        Instance = this;
        IsTrainingWorker = ResolveTrainingWorker();
        EpisodeIndex = 0;
        CurrentSeed = 0;

        BindObjects();
        ConfigureObjects();
        yield return CreateObjects();

        if (restartSimulationOnFinish)
        {
            while (restartSimulationOnFinish)
            {
                yield return RunEpisode();
                AdvanceEpisode();
                yield return null;
            }
        }
        else
        {
            yield return RunEpisode();
        }

        QuitGame();
    }

    private IEnumerator RunEpisode()
    {
        PrepareGame();
        yield return BeginGame();
    }

    private IEnumerator BeginGame()
    {
        // здесь запускаем таймер и циклы всех объектов
        yield return StartCoroutine(Container.Instance.SimManager.StartSimulation());
    }

    private void PrepareGame()
    {
        InitializeObjects();
        Container.Instance.SimManager.Initialize();
        // разместить созданные игровые объекты с параметрами на сцене
    }

    private void BindObjects()
    {
        // instantiate lightweight prefabs of game objects
        GetComponent<Container>().Bootstrap();
    }

    private void ConfigureObjects()
    {
        Container.Instance.SchedulerAgent.ConfigureComponents();
    }

    private void InitializeObjects()
    {
        // запуск сервисов раньше всего остального
        int seed = ResolveEpisodeSeed(Container.Instance.ConfigProvider.SimConfig);
        RandomUtils.ResetSeed(seed);
        Container.Instance.TimeSystem.ResetTicks();
        if (IsTrainingWorker)
        {
            Debug.Log($"[EntryPoint] episode_start episode={EpisodeIndex} seed={seed}");
        }
        //RandomUtils.SetSeed(ResolveSimulationSeed());
    }

    // private int ResolveSimulationSeed()
    // {
    //     int defaultSeed = Container.Instance.ConfigProvider.SimConfig.DeterministicSeed;
    //     return defaultSeed;
    // }

    private IEnumerator CreateObjects()
    {
        // загрузка тяжелых объектов
        yield return null;
    }


    private bool ResolveTrainingWorker()
    {
        return Academy.Instance.IsCommunicatorOn;
    }

    private int ResolveEpisodeSeed(SimConfig simConfig)
    {
        int fallbackSeed = IsTrainingWorker ? simConfig.DeterministicSeed + EpisodeIndex : simConfig.DeterministicSeed;
        CurrentSeed = fallbackSeed;

        return CurrentSeed;
    }

    private void AdvanceEpisode()
    {
        EpisodeIndex++;
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
