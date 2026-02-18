using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseActor
{
    public string ActorName { get; private set; }
    public HostModel Host { get; private set; }
    private readonly MailBox _mailbox;

    protected BaseActor(string actorName, HostModel host)
    {
        ActorName = actorName;
        Host = host;
        _mailbox = new MailBox();
        SimulationManager.Instance.RegisterActor(this);
    }

    public abstract IEnumerator MainLoop();

    public IEnumerator Push(string recipientName, IMessage message)
    {
        if (SimulationManager.Instance.Actors.TryGetValue(recipientName, out var recipient))
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