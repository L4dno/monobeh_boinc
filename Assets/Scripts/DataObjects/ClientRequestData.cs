using UnityEngine;

public class ClientRequestData : IMessage
{
    public readonly string RequesterName;
    public readonly int HostId;
    public readonly float Power;
    public readonly float Percentage;

    public ClientRequestData(string requesterName, int hostId, float power, float percentage)
    {
        RequesterName = requesterName;
        HostId = hostId;
        Power = power;
        Percentage = percentage;
    }

    public float GetSizeInMegabytes()
    {
        return 0.01f; // 10 KB
    }

    public int CalculateResultsNumber(ResultData result)
    {
        float taskDuration = result.durationInGflops / Power;
        int resultsNumber = Mathf.FloorToInt(Percentage / taskDuration);
        if (resultsNumber == 0)
        {
            resultsNumber = 1;
        }

        return resultsNumber;
    }
}
