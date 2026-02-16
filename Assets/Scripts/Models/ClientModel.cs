using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ClientState
{
    Idle,
    Busy, 
    Suspended
}

public class ClientModel : BaseActor
{

    const int RANDOM_TO_TICKS_FACTOR = 3600;
    // ссылка на структуру базовых настроек
    private readonly GroupConfig _config;

    const int PROJECT_ACTOR = 0;

    private ClientState _curState;

    //private CoroutineHandler _networking;
    //private CoroutineHandler _executing;

    private int _tickToConnect;

    public override IEnumerator MainLoop(int hostId)
    {
        _hostId = hostId;
        Debug.Log($"Client {_actorId} main loop started on host {hostId}");
        yield return null;
    }


    // protected override void Tick(int curTick)
    // {
        
    //     if (SimulationManager.Instance.hosts[_hostId].State==HostState.Off &&
    //         _curState != ClientState.Suspended)
    //     {
    //         // first second of suspend
    //         _curState = ClientState.Suspended;
    //         Debug.Log($"Client {_hostId} suspended on tick {curTick}");

    //         if (_networking != null)
    //         {
    //             SimulationManager.StopRoutine(_networking);
    //             _networking = null;
    //         }
    //         if (_executing != null)
    //         {
    //             SimulationManager.StopRoutine(_executing);
    //             _executing = null;
    //         }
    //     }
        
    //     if (SimulationManager.Instance.hosts[_hostId].State==HostState.On) {
            
    //         if (_computeQueue.Count > 0 && _curState != ClientState.Busy)
    //         {
    //             _curState = ClientState.Busy;
    //             Debug.Log($"Client {_hostId} busy on tick {curTick}");
    //         }
    //         else if (_curState != ClientState.Idle)
    //         {
    //             _curState = ClientState.Idle;
    //             Debug.Log($"Client {_hostId} idle on tick {curTick}");
    //         }

    //     }

    //     switch (_curState)
    //     {
    //         case ClientState.Suspended:
    //             break;
    //         case ClientState.Busy:
    //             HandleBusy();
    //             HandleIdle();
    //             break;
    //         case ClientState.Idle:
    //             HandleIdle();
    //             break;
    //     }
        
    // }

    public Queue<ServerReplyData> workToCompute = new Queue<ServerReplyData>();
    public float workAmountFlops = 0;

    public Queue<ClientReplyData> workToUpload = new Queue<ClientReplyData>();

    // private void HandleIdle()
    // {
    //     // проверить отсылку
    //     // проверить докачку
    //     // отправить запрос (поместить в очередь на отсылку)
    //     if (_networking != null)
    //     {
    //         return;
    //     }
    //     if (workToUpload.Count > 0)
    //     {
    //         var reply = workToUpload.Peek();
    //         int ticksToSend = reply.fileSize / _config.ServerBandwidth + _config.ServerLatency;
    //         _networking = SimulationManager.StartRoutine(
    //             NetworkRoutine(Mathf.Celling(ticksToSend))
    //             );
    //     }


    //     // if (_networking == null)
    //     // {
    //     //     _networking = SimulationManager.StartRoutine(NetworkRoutine());
    //     // }
    // }
    // private void HandleBusy()
    // {
    //     // server reply -> client reply
    //     if (_executing == null)
    //     {
    //         _executing = SimulationManager.StartRoutine(ExecuteRoutine());
    //     }
    // }

    // private IEnumerator NetworkRoutine(IMessage message)
    // {
    //     // обнули ссылку в конце
    //     yield return null;
    //     // нужен флаг завершенности
    // }

    // private IEnumerator ExecuteRoutine(IMessage message)
    // {
    //     yield return null;
    // }
    
    const int MIN_WARMUP_TIME = 0;
    const int MAX_WARMUP_TIME = 3600;

    public ClientModel(GroupConfig config, int actorId)
    {
        _config = config;
        _actorId = actorId;
        Debug.Log($"Client {_actorId} created");

        // _curState = ClientState.Idle;
        // _tickToConnect = (int)RandomUtils.GetDistribution(Distribution.Uniform, 
        //                                                 MIN_WARMUP_TIME,
        //                                                 MAX_WARMUP_TIME);

    }
}
