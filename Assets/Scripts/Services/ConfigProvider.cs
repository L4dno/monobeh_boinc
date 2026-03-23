using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class ConfigProvider : IConfigProvider {

    public SimConfig SimConfig => _simConfig;

    private SimConfig _simConfig;

    public ConfigProvider(){
        _simConfig = Resources.Load<SimConfig>("Configs/SimConfig");
    }

    public string GetConfigsJson() {
        string paramsJson = JsonConvert.SerializeObject(_simConfig);
        return paramsJson;
    }
}