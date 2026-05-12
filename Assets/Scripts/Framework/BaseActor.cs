using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseActor
{
    public string ActorName { get; private set; }
    public HostModel Host { get; private set; }
    private readonly MailBox _mailbox;
    protected SimulationManager SimManager => Container.Instance.SimManager;

    protected BaseActor(string actorName, HostModel host)
    {
        ActorName = actorName;
        Host = host;
        _mailbox = new MailBox();
        SimManager.RegisterActor(this);
    }

    public abstract IEnumerator MainLoop();

    public MessageComm Push(string recipientName, IMessage message)
    {
        if (SimManager.Actors.TryGetValue(recipientName, out var recipient))
        {
            var link = SimManager.Link;
            int ticksToWait = (int)Mathf.Ceil(link.Latency + message.GetSizeInMegabytes() / link.Bandwidth);
            if (ticksToWait <= 1)
            {
                recipient.ReceiveInstant(message);
                return MessageComm.Completed();
            }

            return new MessageComm(SimManager, recipient._mailbox.Put(message));
        }

        Debug.LogError($"Recipient actor '{recipientName}' not found.");
        return MessageComm.Completed();
    }

    public IMessage Receive()
    {
        return _mailbox.Get();
    }

    public T Receive<T>() where T : class, IMessage
    {
        return _mailbox.Get<T>();
    }

    protected virtual void ReceiveInstant(IMessage message)
    {
        _mailbox.PutNow(message);
    }
}

public class MessageComm : CustomYieldInstruction
{
    private bool _isDone;
    private SimulationManager _simManager;
    private Coroutine _coroutine;

    public override bool keepWaiting => !_isDone;
    public bool IsDone => _isDone;

    private MessageComm()
    {
        _isDone = true;
    }

    public MessageComm(SimulationManager simManager, IEnumerator routine)
    {
        _simManager = simManager;
        _isDone = false;
        _coroutine = _simManager.StartSimulationCoroutine(Run(routine));
    }

    public static MessageComm Completed()
    {
        return new MessageComm();
    }

    public IEnumerator Wait()
    {
        while (!_isDone)
        {
            yield return null;
        }
    }

    private IEnumerator Run(IEnumerator routine)
    {
        yield return routine;
        _isDone = true;
        _simManager.ForgetSimulationCoroutine(_coroutine);
    }
}
