using UnityEngine;

public class ProjectModel : BaseActor
{
    private readonly ProjectConfig _config;

    private readonly int _hostId;

    protected override void Tick(int curTick)
    {
        //Debug.Log($"Project called on host {_hostId}");
        while (_mailBox.Count > 0)
        {
            var mail = _mailBox.Dequeue();
            switch (mail)
            {
                case ClientReplyData reply:
                    ProcessReply(reply);
                    break;
                case ClientRequestData request:
                    ProcessRequest(request);
                    break;
            }
        }
    }

    // validator
    private void ProcessReply(ClientReplyData message)
    {
        
    }
    // scheduler
    //private readonly Queue<
    private void ProcessRequest(ClientRequestData message)
    {
        
    }
    // generator
    private void GenerateTasks()
    {
        // generate normally between [l;r] from config
        // must be clamped value

        // ParamA = (l+r)/2
        // ParamB = (r-l)/4
    }
    // assimilator
    private void AssimilateTask()
    {
        
    }

    public ProjectModel(ProjectConfig config, int hostId)
    {
        _config = config;
        _hostId = hostId;
    }

}