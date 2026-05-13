using System;
using System.Collections;

public interface IScheduler
{
    IEnumerator Run(ProjectDatabase database, Func<string, IMessage, MessageComm> sendMessage);
}
