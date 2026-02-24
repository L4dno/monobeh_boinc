using System.Collections.Generic;

public static class GlobalStats
{
    // Project-level statistics, keyed by project name
    public static Dictionary<string, int> MessagesReceived = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkRequests = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkunitsCreated = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkunitsSent = new Dictionary<string, int>();
    public static Dictionary<string, int> ValidWorkunitResults = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkunitResultsReceived = new Dictionary<string, int>();
    public static Dictionary<string, int> WorkunitResultsAnalyzed = new Dictionary<string, int>();
    public static Dictionary<string, int> SuccessfulWorkunitResults = new Dictionary<string, int>();
    public static Dictionary<string, int> ErrorWorkunitResults = new Dictionary<string, int>();
    public static Dictionary<string, int> LateWorkunitResults = new Dictionary<string, int>();
    public static Dictionary<string, long> TotalCredit = new Dictionary<string, long>();
    public static Dictionary<string, int> TasksCreated = new Dictionary<string, int>();
    public static Dictionary<string, int> TasksValid = new Dictionary<string, int>();
    public static Dictionary<string, int> TasksError = new Dictionary<string, int>();

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
        WorkunitsCreated[projectName] = 0;
        WorkunitsSent[projectName] = 0;
        ValidWorkunitResults[projectName] = 0;
        WorkunitResultsReceived[projectName] = 0;
        WorkunitResultsAnalyzed[projectName] = 0;
        SuccessfulWorkunitResults[projectName] = 0;
        ErrorWorkunitResults[projectName] = 0;
        LateWorkunitResults[projectName] = 0;
        TotalCredit[projectName] = 0;
        TasksCreated[projectName] = 0;
        TasksValid[projectName] = 0;
        TasksError[projectName] = 0;
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
        WorkunitsCreated.Clear();
        WorkunitsSent.Clear();
        ValidWorkunitResults.Clear();
        WorkunitResultsReceived.Clear();
        WorkunitResultsAnalyzed.Clear();
        SuccessfulWorkunitResults.Clear();
        ErrorWorkunitResults.Clear();
        LateWorkunitResults.Clear();
        TotalCredit.Clear();
        TasksValid.Clear();
        TasksError.Clear();
        TasksCreated.Clear();

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
