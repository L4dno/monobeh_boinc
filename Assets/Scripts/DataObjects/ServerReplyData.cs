using UnityEngine;
using System.Collections.Generic;



// server to client
public class ServerReplyData : IMessage
{
    public IActor Sender {get;}

    public TaskModel Workunit {get;}

    //public List<Task> Tasks {get;}

    // pubclic List InputFiles ??


    public ServerReplyData(IActor sender)
    {
        Sender = sender;
    }
}
