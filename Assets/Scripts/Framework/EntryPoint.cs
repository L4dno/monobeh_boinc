#if UNITY_EDITOR
using UnityEditor;
#endif
//using Unity.MLAgents;
using UnityEngine;
using System.Collections;

public class EntryPoint : MonoBehaviour
{
    private const string SimulationSeedParameterName = "simulation_seed";

    // единственный метод старт в игре
    private IEnumerator Start()
    {
        BindObjects();
        yield return CreateObjects();
        PrepareGame();
        yield return BeginGame();
        QuitGame();
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

    private void InitializeObjects()
    {
        // запуск сервисов раньше всего остального
        RandomUtils.ResetSeed(Container.Instance.ConfigProvider.SimConfig.DeterministicSeed);
        //RandomUtils.SetSeed(ResolveSimulationSeed());
    }

    // private int ResolveSimulationSeed()
    // {
    //     int defaultSeed = Container.Instance.ConfigProvider.SimConfig.DeterministicSeed;
    //     EnvironmentParameters parameters = Academy.Instance.EnvironmentParameters;
    //     float value = parameters.GetWithDefault(SimulationSeedParameterName, defaultSeed);
    //     return Mathf.RoundToInt(value);
    // }

    private IEnumerator CreateObjects()
    {
        // загрузка тяжелых объектов
        yield return null;
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
