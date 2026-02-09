using UnityEngine;

public class ProjectModel : BaseActor
{
    private readonly ProjectConfig _config;

    private readonly int _hostId;

    protected override void Tick(int curTick)
    {
        //Debug.Log($"Project called on host {_hostId}");
    }

    public ProjectModel(ProjectConfig config, int hostId)
    {
        _config = config;
        _hostId = hostId;
    }

}