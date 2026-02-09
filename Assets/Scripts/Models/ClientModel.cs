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
    // ссылка на структуру базовых настроек
    private readonly GroupConfig _config;

    private readonly int _hostId;

    private ClientState _curState;
    

    private readonly Queue<object> _computeQueue = new Queue<object>();
    private readonly Queue<object> _uploadQueue = new Queue<object>();

    private Coroutine _networking;
    private Coroutine _executing;

    private int _tickToConnect;

    protected override void Tick(int curTick)
    {
        
        if (SimulationManager.Instance.hosts[_hostId].State==HostState.Off &&
            _curState != ClientState.Suspended)
        {
            // first second of suspend
            _curState = ClientState.Suspended;
            Debug.Log($"Client {_hostId} suspended on tick {curTick}");

            if (_networking != null)
            {
                SimulationManager.StopRoutine(_networking);
                _networking = null;
            }
            if (_executing != null)
            {
                SimulationManager.StopRoutine(_executing);
                _executing = null;
            }
        }
        
        if (SimulationManager.Instance.hosts[_hostId].State==HostState.On) {
            
            if (_computeQueue.Count > 0 && _curState != ClientState.Busy)
            {
                _curState = ClientState.Busy;
                Debug.Log($"Client {_hostId} busy on tick {curTick}");
            }
            else if (_curState != ClientState.Idle)
            {
                _curState = ClientState.Idle;
                Debug.Log($"Client {_hostId} idle on tick {curTick}");
            }

        }

        switch (_curState)
        {
            case ClientState.Suspended:
                break;
            case ClientState.Busy:
                HandleBusy();
                HandleIdle();
                break;
            case ClientState.Idle:
                HandleIdle();
                break;
        }
        
    }

    private void HandleIdle()
    {
        if (_networking == null)
        {
            _networking = SimulationManager.StartRoutine(NetworkRoutine());
        }
    }
    private void HandleBusy()
    {
        if (_executing == null)
        {
            _executing = SimulationManager.StartRoutine(ExecuteRoutine());
        }
    }

    private IEnumerator NetworkRoutine()
    {
        // обнули ссылку в конце
        yield return null;
        _networking = null;
    }

    private IEnumerator ExecuteRoutine()
    {
        yield return null;
        _executing = null;
    }
    const int MIN_WARMUP_TIME = 0;
    const int MAX_WARMUP_TIME = 3600;

    public ClientModel(GroupConfig config, int hostId) : base()
    {
        _config = config;
        _hostId = hostId;

        _curState = ClientState.Idle;
        _tickToConnect = (int)RandomUtils.GetDistribution(Distribution.Uniform, 
                                                        MIN_WARMUP_TIME,
                                                        MAX_WARMUP_TIME);

    }
}
