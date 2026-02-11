using UnityEngine;
using System.Collections.Generic;

public class ProjectModel : BaseActor
{
    private readonly ProjectConfig _config;

    private readonly int _hostId;

    public ProjectModel(ProjectConfig config, int hostId)
    {
        _config = config;
        _hostId = hostId;
        GenerateTasks();
    }

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

    private readonly Dictionary<int, TaskModel> _taskDatabase 
                        = new Dictionary<int, TaskModel>();
    
    private readonly Queue<int> _tasksToSend = new Queue<int>();
    
    public void ProcessReply(ClientReplyData reply)
    {
        
    }
    public void ProcessRequest(ClientRequestData request)
    {
        // в конце метода перепроверь реплицируемость и помести в очередь
    }
    
    // generator
    private int _createdTasksCount = 0;
    private void GenerateTasks()
    {
        for (;_taskDatabase.Count < _config.InitialTaskCount; _createdTasksCount++)
        {
            _taskDatabase.Add(_createdTasksCount, new TaskModel(_config.TaskConfig));
            _tasksToSend.Enqueue(_createdTasksCount);
        }
    }
    

}