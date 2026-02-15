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
        //GenerateTasks();
    }

    //protected override void Tick(int curTick)
   // {
        //Debug.Log($"Project called on host {_hostId}");
        // while (_mailBox.Count > 0)
        // {
        //     var mail = _mailBox.Dequeue();
        //     switch (mail)
        //     {
        //         case ClientReplyData reply:
        //             ProcessReply(reply);
        //             break;
        //         case ClientRequestData request:
        //             ProcessRequest(request);
        //             break;
        //     }
        // }
    //}

    // public void ProcessReply(ClientReplyData reply)
    // {

    //     // increase counters
    //     // check whether need to switch state
    //     // if in progress and has erroed result create new wu

    //     var task = _taskDatabase[reply.taskId];
    //     task.CurWorkunitsReceived += 1;

    //     if (!task.isWorkunitInTime(curTick, reply.workunitId))
    //     {
    //         // reply == Fail
    //         task.CurErrorWorkunits += 1;
    //         // project delay results +=1
    //     }
    //     else if (reply.status == WorkunitStatus.Success)
    //     {
    //         task.CurSuccessWorkunits +=1;
    //         // project success results +=1

    //         if (reply.value == WorkunitValue.Valid)
    //         {
    //             task.CurValidWorkunits += 1;

    //             // set credits
    //         }
    //     }
    //     else
    //     {
    //         // error wu
    //         task.CurErrorWorkunits += 1;
    //         // project errored results +=1
    //     }

    //     // project.nresultsanalyzed++;

    //     if (task.CurState == TaskState.InProgress)
    //     {
    //         if (task.CurValidWorkunits >= _config.MinQuorum)
    //         {
    //             task.CurState = TaskState.Valid;
    //             // project valid wu += task.validwu
    //             // project valid tasks += 1
    //         }
    //         else if (task.CurCreatedWorkunits >= _config.MaxCreatedWorkunits ||
    //                  task.CurErrorWorkunits >= _config.MaxErrorWorkunits ||
    //                  task.CurSuccessWorkunits >= _config.MaxSuccessWorkunits)
    //         {
    //             task.CurState = TaskState.Error;
    //             // project error tasks +=1
    //         }
    //     }
    //     // can project valid and credits increase from additional valids wu

    //     if (reply.status == WorkunitStatus.Fail)
    //     {
    //         if (task.CurState == TaskState.InProgress &&
    //             task.CurSuccessWorkunits < _config.MaxSuccessWorkunits &&
    //             task.CurErrorWorkunits < _config.MaxErrorWorkunits &&
    //             task.CurCreatedWorkunits < _config.MaxCreatedWorkunits)
    //         {
    //             _readyWork.Enqueue(task.ReplicateTask());
    //             task.CurWorkunitsRecreated += 1;
    //             task.CurCreatedWorkunits += 1;
    //             // project recreated results +=1
    //         }
    //     }


    // }

    // private readonly Dictionary<int, TaskModel> _taskDatabase 
    //                     = new Dictionary<int, TaskModel>();
    
    // private readonly Queue<WorkunitData> _readyWork = new Queue<WorkunitData>();
    
    // public void ProcessRequest(ClientRequestData request)
    // {
    //     // выбирает задачу по алгоритму
    //     // пока не наберет достаточную пачку задачи
    //     // или пока не сделает проверок, равное размеру очереди

    //     float requestedPayload = request.freeHostGflops * request.ticksInterval;
        
    //     int examinedTasksCount = 0;
    //     int maxExaminedTasks = _readyWork.Count;
    //     HashSet<int> pickedTasksIds = new HashSet<int>();

    //     while (requestedPayload > 0 && examinedTasksCount < maxExaminedTasks)
    //     {
    //         var wu = _readyWork.Dequeue();
    //         examinedTasksCount++;
    //         if (pickedTasksIds.Contains(wu.parentTaskId))
    //         {
    //             _readyWork.Enqueue(wu);
    //             continue;
    //         }          

    //         var task = _taskDatabase[wu.parentTaskId];

    //         requestedPayload -= task._taskGflops;
    //         SimulationManager.Instance.actors[request.actorSender].Push(
    //             new ServerReplyData(wu, curTick + _config.DelayBound)
    //         );
    //         task.RegisterSentUnit(wu.workunitId, curTick + _config.DelayBound);
    //         // если 
    //         // if (task.CurCreatedWorkunits < _config.TargetCountOfWorkunits)
    //         // {
    //         //     _tasksToSend.Enqueue(taskInd);
    //         // }
    //         // else if (task.CurWorkunitsToResend > 0)
    //         // {
    //         //     _tasksToSend.Enqueue(taskInd);
    //         //     task.CurWorkunitsToResend -= 1;
    //         // }
    //     }

    // }
    
    // // generator
    // private int _createdTasksCount = 0;
    // private void GenerateTasks()
    // {
    //     for (;_taskDatabase.Count < _config.InitialTaskCount; _createdTasksCount++)
    //     {
    //         var task = new TaskModel(_config.TaskConfig, _createdTasksCount);
    //         _taskDatabase.Add(_createdTasksCount, task);
    //         //_tasksToSend.Enqueue(_createdTasksCount);
    //         while (task.CurCreatedWorkunits < _config.TargetCountOfWorkunits)
    //         {
    //             _readyWork.Enqueue(task.ReplicateTask());
    //             task.CurCreatedWorkunits += 1;
    //         }
    //     }
    // }
    

}