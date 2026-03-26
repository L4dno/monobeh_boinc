using UnityEngine;

public class Container : MonoBehaviour
{
    //[field: SirializeField]
    public IConfigProvider ConfigProvider {get; private set; }
    public IStatService StatService {get; private set;}
    public Coroutines Coroutines {get; private set;}

    public static Container Instance {get; private set; }

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(this);

        ConfigProvider = new ConfigProvider();
        Coroutines = gameObject.AddComponent<Coroutines>();
        StatService = new StatService();
    }
}