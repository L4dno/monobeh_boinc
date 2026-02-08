using UnityEngine;
using System.Collections.Generic;

public enum ClientState
{
    Idle,
    Busy, 
    Suspended
}

public class ClientModel : BaseActor
{
    // ссылка на структуру базовых настроек
    private readonly GroupConfig _config;

    private readonly int _hostId;

    private ClientState _curState;
    

    protected override void Tick(int curTick)
    {
        Debug.Log($"Client {_hostId} called");

        // if (SimulationManager._hosts[_hostId].State==HostState.Off &&
        //     _curState == ClientState.Suspended)
        // {
        //     return;
        // }
        // if (SimulationManager._hosts[_hostId].State==HostState.Off)
        // {
        //     // first second of suspend
        //     _curSendBytes = 0;
        //     _curExecutedFlops = 0;
        //     _curState = ClientState.Suspended;
        //     return;
        // }
        // if (SimulationManager.Instance.CurSimulationTime >= _timeToConnect)
        // {
        //     Fetch();
        // }
        // Execute();
    }

    //private readonly Queue<object> _downloadQueue;
    //private readonly Queue<object> _uploadQueue;
    private int _timeToConnect = 0;

    private int _curSendBytes = 0;

    private void Fetch()
    {
        // send requests or replies to server
        // сначала репортим пока можем
        // потом качаем если надо

        // если ничего не осталось, то
        
    }

    //private readonly Queue<object> _computeQueue;
    private double _curExecutedFlops = 0;

    // private void Execute()
    // {
    //     // how to handle idling?
    //     // if has something than busy
    //     if (_computeQueue.Count == 0)
    //     {
    //         _curState = ClientState.Idle;
    //         return;
    //     }
    //     else
    //     {
    //         var curTask = _computeQueue.Peek();
    //         //if (curTask.Flops > _curExecutedFlops)
    //     }
    // }



    const int MIN_WARMUP_TIME = 0;
    const int MAX_WARMUP_TIME = 3600;

    public ClientModel(GroupConfig config, int hostId) : base()
    {
        _config = config;
        _hostId = hostId;

        _curState = ClientState.Idle;
        _timeToConnect = (int)RandomUtils.GetDistribution(Distribution.Uniform, 
                                                        MIN_WARMUP_TIME,
                                                        MAX_WARMUP_TIME);

    }
}
