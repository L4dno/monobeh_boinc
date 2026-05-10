using System.Collections.Generic;
using System.Linq;

public class ServerReplyData : IMessage
{
    public readonly List<ResultData> results;

    public ServerReplyData(List<ResultData> results)
    {
        this.results = results;
    }

    public float GetByteSize()
    {
        return results.Where(result => result != null).Sum(result => result.inputByteSize);
    }
}
