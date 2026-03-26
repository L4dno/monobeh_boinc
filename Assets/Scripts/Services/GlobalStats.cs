using System.Collections.Generic;

// затем надо реализовать интерфейс IStatService 
// также должен быть интерфейс для записи в файлы
// можно вынести все данные в объект хранилище для композиции
public static class GlobalStats
{
    // Project-level statistics, keyed by project name
    private static Dictionary<string, int> MessagesReceived = new Dictionary<string, int>();
    private static Dictionary<string, int> WorkRequests = new Dictionary<string, int>();
    private static Dictionary<string, int> WorkunitsCreated = new Dictionary<string, int>();
    private static Dictionary<string, int> WorkunitsSent = new Dictionary<string, int>();
    private static Dictionary<string, int> ValidWorkunitResults = new Dictionary<string, int>();
    private static Dictionary<string, int> WorkunitResultsReceived = new Dictionary<string, int>();
    private static Dictionary<string, int> SuccessfulWorkunitResults = new Dictionary<string, int>();
    private static Dictionary<string, int> ErrorWorkunitResults = new Dictionary<string, int>();
    private static Dictionary<string, int> LateWorkunitResults = new Dictionary<string, int>();
    private static Dictionary<string, int> TasksCreated = new Dictionary<string, int>();
    private static Dictionary<string, int> TasksValid = new Dictionary<string, int>();
    private static Dictionary<string, int> TasksError = new Dictionary<string, int>();

    // Client-level statistics per project, keyed by project name
    private static Dictionary<string, int> TotalTasksChecked = new Dictionary<string, int>();
    private static Dictionary<string, int> TotalTasksExecuted = new Dictionary<string, int>();
    private static Dictionary<string, int> TotalTasksReceived = new Dictionary<string, int>();
    private static Dictionary<string, int> TotalTasksMissed = new Dictionary<string, int>();

    // System-level statistics
    private static long TotalPower;
    private static float TotalAvailableTime;
    private static float TotalNotAvailableTime;

    private static Dictionary<int, int> TotalBusyTimeByHost = new Dictionary<int, int>();
    private static Dictionary<int, int> TotalIdleTimeByHost = new Dictionary<int, int>();
    private static Dictionary<int, int> TotalSuspendedTimeByHost = new Dictionary<int, int>();

    private static void InitProject(string projectName)
    {
        MessagesReceived[projectName] = 0;
        WorkRequests[projectName] = 0;
        WorkunitsCreated[projectName] = 0;
        WorkunitsSent[projectName] = 0;
        ValidWorkunitResults[projectName] = 0;
        WorkunitResultsReceived[projectName] = 0;
        SuccessfulWorkunitResults[projectName] = 0;
        ErrorWorkunitResults[projectName] = 0;
        LateWorkunitResults[projectName] = 0;
        TasksCreated[projectName] = 0;
        TasksValid[projectName] = 0;
        TasksError[projectName] = 0;
        TotalTasksChecked[projectName] = 0;
        TotalTasksExecuted[projectName] = 0;
        TotalTasksReceived[projectName] = 0;
        TotalTasksMissed[projectName] = 0;
    }

    private static void Reset()
    {
        MessagesReceived.Clear();
        WorkRequests.Clear();
        WorkunitsCreated.Clear();
        WorkunitsSent.Clear();
        ValidWorkunitResults.Clear();
        WorkunitResultsReceived.Clear();
        SuccessfulWorkunitResults.Clear();
        ErrorWorkunitResults.Clear();
        LateWorkunitResults.Clear();
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
    }
}
