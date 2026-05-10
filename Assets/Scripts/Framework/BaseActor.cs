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

    public IEnumerator Push(string recipientName, IMessage message)
    {
        if (SimManager.Actors.TryGetValue(recipientName, out var recipient))
        {
            yield return recipient._mailbox.Put(message);
        }
        else
        {
            Debug.LogError($"Recipient actor '{recipientName}' not found.");
        }
    }

    public IMessage Receive()
    {
        return _mailbox.Get();
    }
}
