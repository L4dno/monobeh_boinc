public interface IStatService {
    void Initialize(int maxSimulationTime, SimConfig simConfig);
    void RegisterClient(IClientStats client);
    void RegisterProject(IProjectStats project);
    void RecordAvailability(float durationHours, int startTick, int durationTicks);
    void RecordUnavailability(float durationHours);
    void RecordHostPower(float power);
    void RecordWorkRequestReceived();
    void RecordResultCreated();
    void RecordResultsSent(int resultsNumber);
    void RecordResultReceived();
    void RecordResultAnalyzed(bool isTimeout, bool isSuccess);
    void RecordWorkunitReachedQuorum(int applicationIndex, int validResults, int credit);
    void RecordAdditionalValidResult(int credit);
    void RecordWorkunitAssimilated(bool isValid);
    void RecordTailBudget(float theoreticalGflopsBudget, float effectiveGflopsBudget, int[] applicationTargets);
    void RecordTailStarted(int tick);
    void RecordSimulationFinished(int tick);
    void RecordSentResults(int resultsNumber, int timestamp);
    void RecordGotResult(int isCorrect, int timestamp);
    StatsData GetStats();
}

// добавляем в реализацию подписки на новые события
// подсчитываем новые метрики и добавляем в гет статс
// перегружаем файловый записыватель и добавляем его в массив
// для хранителя директории
