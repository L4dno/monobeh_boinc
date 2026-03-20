using UnityEngine;

public class ConfigProvider : IConfigProvider {
    public SimConfig SimConfig => _simConfig;

    private SimConfig _simConfig;

    public ConfigProvider(){
        _simConfig = Resources.Load<SimConfig>("Configs/SimConfig");
    }
}