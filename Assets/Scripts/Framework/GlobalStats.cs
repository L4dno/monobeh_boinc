using System.Collections.Generic;

public static class GlobalStats
{
    // Project-level statistics, keyed by project name
    public static Dictionary<string, int> MessagesReceived = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkRequests = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsCreated = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsSent = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsValid = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsReceived = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsAnalyzed = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsSuccess = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsError = new Dictionary<string, int>();
    public static Dictionary<string, int> ResultsLate = new Dictionary<string, int>();
    public static Dictionary<string, long> TotalCredit = new Dictionary<string, long>();
    public static Dictionary<string, int> WorkunitsCreated = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkunitsValid = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkunitsError = new Dictionary<string, int>();

    // Client-level statistics per project, keyed by project name
    public static Dictionary<string, int> TotalTasksChecked = new Dictionary<string, int>();
    public static Dictionary<string, int> TotalTasksExecuted = new Dictionary<string, int>();
    public static Dictionary<string, int> TotalTasksReceived = new Dictionary<string, int>();
    public static Dictionary<string, int> TotalTasksMissed = new Dictionary<string, int>();

    // System-level statistics
    public static long TotalPower;
    public static float TotalAvailableTime;
    public static float TotalNotAvailableTime;

    public static Dictionary<int, int> TotalBusyTimeByHost = new Dictionary<int, int>();
    public static Dictionary<int, int> TotalIdleTimeByHost = new Dictionary<int, int>();
    public static Dictionary<int, int> TotalSuspendedTimeByHost = new Dictionary<int, int>();
    
    public static Dictionary<string, int> DsUploads = new Dictionary<string, int>();
    public static Dictionary<string, Dictionary<int, int>> Rfiles = new Dictionary<string, Dictionary<int, int>>();

    public static void InitProject(string projectName)
    {
        MessagesReceived[projectName] = 0;
        WorkRequests[projectName] = 0;
        ResultsCreated[projectName] = 0;
        ResultsSent[projectName] = 0;
        ResultsValid[projectName] = 0;
        ResultsReceived[projectName] = 0;
        ResultsAnalyzed[projectName] = 0;
        ResultsSuccess[projectName] = 0;
        ResultsError[projectName] = 0;
        ResultsLate[projectName] = 0;
        TotalCredit[projectName] = 0;
        WorkunitsCreated[projectName] = 0;
        WorkunitsValid[projectName] = 0;
        WorkunitsError[projectName] = 0;
        TotalTasksChecked[projectName] = 0;
        TotalTasksExecuted[projectName] = 0;
        TotalTasksReceived[projectName] = 0;
        TotalTasksMissed[projectName] = 0;
        DsUploads[projectName] = 0;
        Rfiles[projectName] = new Dictionary<int, int>();
    }

    public static void Reset()
    {
        MessagesReceived.Clear();
        WorkRequests.Clear();
        ResultsCreated.Clear();
        ResultsSent.Clear();
        ResultsValid.Clear();
        ResultsReceived.Clear();
        ResultsAnalyzed.Clear();
        ResultsSuccess.Clear();
        ResultsError.Clear();
        ResultsLate.Clear();
        TotalCredit.Clear();
        WorkunitsValid.Clear();
        WorkunitsError.Clear();
        WorkunitsCreated.Clear();

        TotalTasksChecked.Clear();
        TotalTasksExecuted.Clear();
        TotalTasksReceived.Clear();
        TotalTasksMissed.Clear();
        
        TotalPower = 0;
        TotalAvailableTime = 0;
        TotalNotAvailableTime = 0;

        DsUploads.Clear();
        Rfiles.Clear();
    }
}
