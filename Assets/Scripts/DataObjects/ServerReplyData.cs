using System.Collections.Generic;
using System.Linq;

public class ServerReplyData : IMessage
{
    public readonly List<WorkunitData> workunits;

    public ServerReplyData(List<WorkunitData> workunits)
    {
        this.workunits = workunits;
    }

    public float GetByteSize()
    {
        return workunits.Where(workunit => workunit != null).Sum(workunit => workunit.inputByteSize);
    }
}