using System.Collections.Generic;

public class ServerReplyData : IMessage
{
    public readonly List<ClientTaskData> tasks;
    public readonly float inputTransferSizeMb;

    public ServerReplyData(List<ClientTaskData> tasks, float inputTransferSizeMb)
    {
        this.tasks = tasks;
        this.inputTransferSizeMb = inputTransferSizeMb;
    }

    public float GetSizeInMegabytes()
    {
        return 0.01f + inputTransferSizeMb;
    }
}
