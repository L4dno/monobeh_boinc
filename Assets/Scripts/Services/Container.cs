using UnityEngine;

public class Container : MonoBehaviour
{
    // единый контейнер ссылок
    public IConfigProvider ConfigProvider {get; private set; }
    public IStatService StatService {get; private set;}
    public Coroutines Coroutines {get; private set;}

    public IStatSaver StatSaver {get; private set;}

    public static Container Instance {get; private set; }

    [field : SerializeField] // для монобехов, которые будут на сцене
    public TimeTickSystem TimeSystem {get; private set;}
    [field : SerializeField] 
    public SimulationManager SimManager {get; private set;}


    public void Bootstrap() {
        Instance = this;
        DontDestroyOnLoad(this);

        // создаем обычные классы
        ConfigProvider = new ConfigProvider();
        Coroutines = gameObject.AddComponent<Coroutines>();
        StatService = new StatService();
        StatSaver = new StatisticWriter(ConfigProvider, StatService);

        TimeSystem = Instantiate(TimeSystem);
        SimManager = Instantiate(SimManager);
    }
}
