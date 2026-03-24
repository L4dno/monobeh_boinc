public interface IConfigProvider {
    SimConfig SimConfig {get;}
    string GetConfigsJson();
}