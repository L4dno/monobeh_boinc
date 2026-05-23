using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LearningScheduler : IScheduler
{
    private SchedulerAgent agent;
    private ProjectDatabase _database;
    private Func<string, IMessage, MessageComm> _sendMessage;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;
    private IStatService StatService => Container.Instance.StatService;
    private const int SchedulerResultWaitTimeoutTicks = 5;
    private const int MaxDecisionCandidates = 32;
    private const float DecisionPenalty = -0.01f;
    private readonly LearningObservationBuilder _observationBuilder = new LearningObservationBuilder();
    private readonly HashSet<string> _pendingLearningResults = new HashSet<string>();
    private readonly CoroutineCondition _pendingDecisionCompletedCondition = new CoroutineCondition();
    private Action<int> _pendingDecisionCallback;
    private float _rewardSum;
    private int _rewardCount;
    private bool _episodeEnded;
    private int _workRequestsHandled;
    private int _readyBypassRequests;
    private int _emptyResultRequests;
    private int _decisionRequests;
    private int _decisionResponses;
    private int _createdLearningResults;
    private int _noWorkActions;
    private int _invalidActions;
    private int _agentMissingRequests;
    private int _completedLearningResults;
    private int _rewardEvents;


    public LearningScheduler()
    {
        // когда заканчиваются воркюниты для создания и больше нельзя
        // ниоткуда взять начальных реплик и ошибочных пока нет
        // значит у нас в словаре содержатся все незавеншенные воркюниты
        // значит можно их все закинуть в очередь и уже крутить реплики
        // пока каждый воркюнит не будет удален из словаря и из очереди как следствие
    }

    public IEnumerator Run(ProjectDatabase database, Func<string, IMessage, MessageComm> sendMessage)
    {
        // еще здесь можно получать project model и подписываться на события

        _database = database;
        _sendMessage = sendMessage;
        _pendingLearningResults.Clear();
        _pendingDecisionCallback = null;
        _rewardSum = 0;
        _rewardCount = 0;
        _episodeEnded = false;
        _workRequestsHandled = 0;
        _readyBypassRequests = 0;
        _emptyResultRequests = 0;
        _decisionRequests = 0;
        _decisionResponses = 0;
        _createdLearningResults = 0;
        _noWorkActions = 0;
        _invalidActions = 0;
        _agentMissingRequests = 0;
        _completedLearningResults = 0;
        _rewardEvents = 0;
        _database.OnResultCompleted += OnResultCompleted;
        _database.OnTailFinished += OnTailFinished;
        Container.Instance.SimManager.OnSimulationFinished -= OnSimulationFinished;
        Container.Instance.SimManager.OnSimulationFinished += OnSimulationFinished;
        if (agent != null)
        {
            agent.onDecisionReady -= OnDecisionReady;
        }

        agent = Container.Instance.SchedulerAgent;
        if (agent == null)
        {
            agent = UnityEngine.Object.FindFirstObjectByType<SchedulerAgent>();
        }

        if (agent != null)
        {
            agent.onDecisionReady -= OnDecisionReady;
            agent.onDecisionReady += OnDecisionReady;
        }

        LogTraining($"run_start agentFound={agent != null} {GetDatabaseState()}");

        // здесь видимо надо создать очередь
        // но ведь могут еще быть основные реплики

        while (true)
        {
            while (_pendingDecisionCallback != null)
            {
                yield return _pendingDecisionCompletedCondition.Wait();
            }

            while (_database.ClientRequests.Count == 0)
            {
                // нет новых запросов
                yield return _database.ClientRequestAvailableCondition.Wait();
            }

            while (_database.ClientRequests.Count > 0 && _pendingDecisionCallback == null)
            {
                // берем новый запрос от клиента
                var request = _database.ClientRequests.Dequeue();
                _workRequestsHandled++;
                if (ShouldLogRequest())
                {
                    LogTraining($"request_start index={_workRequestsHandled} host={request.HostId} power={request.Power} percentage={request.Percentage} {GetDatabaseState()}");
                }

                // если пустое окно реплик
                if (_database.CurrentResults.Count == 0)
                {
                    _emptyResultRequests++;
                    LogTraining($"result_wait_start request={_workRequestsHandled} {GetDatabaseState()}");
                    // ждем создания новых или обрабатываем ответ с пустой очередью
                    yield return _database.ResultAvailableCondition.TimedWait(SchedulerResultWaitTimeoutTicks);
                    LogTraining($"result_wait_end request={_workRequestsHandled} {GetDatabaseState()}");
                }

                ProcessWorkRequest(request);
            }

        }
    }

    private void ProcessWorkRequest(ClientRequestData request)
    {
        var resultsToSend = new List<ResultData>();
        int resultsNumber = 1;
        // если есть реплики стартовые
        if (_database.CurrentResults.Count > 0)
        {
            var firstResult = _database.CurrentResults.Dequeue();
            resultsToSend.Add(firstResult);
            resultsNumber = request.CalculateResultsNumber(firstResult);

            // добавляем еще пока есть реплики
            while (resultsToSend.Count < resultsNumber && _database.CurrentResults.Count > 0)
            {
                resultsToSend.Add(_database.CurrentResults.Dequeue());
            }
            // реплики кончились, то дозаполняем
            if (_database.CurrentResults.Count == 0)
            {
                // то есть мы не ждем пока придут новые стартовые
                // а отправляем что есть?
                _database.ResultBufferHasSpaceCondition.Signal();
            }

            _readyBypassRequests++;
            if (ShouldLogRequest())
            {
                LogTraining($"ready_results_sent request={_workRequestsHandled} results={resultsToSend.Count} {GetDatabaseState()}");
            }
        }

        // впоследствии тут будет проверка что еще не набрали рюкзак ответа
        if (resultsToSend.Count < resultsNumber)
        {
            // берем ключи и случайно набираем 32 уникальных воркюнита
            var candidates = SelectCandidates();

            // вызываем сами
            if (agent != null)
            {
                ApplyAccumulatedReward();
                if (candidates.Count > 0)
                {
                    RecordReward(DecisionPenalty);
                }
                var context = _observationBuilder.Build(request, candidates, _database);
                _pendingDecisionCallback = action => CompleteDecision(request, resultsToSend, candidates, action);
                agent.SetDecisionContext(context);
                _decisionRequests++;
                LogTraining($"decision_request id={_decisionRequests} request={_workRequestsHandled} host={request.HostId} candidates={candidates.Count} {GetCandidateState(candidates)} {GetDatabaseState()}");
                agent.RequestDecision();
                return;
            }
            else
            {
                _agentMissingRequests++;
                LogTraining($"agent_missing request={_workRequestsHandled} candidates={candidates.Count} {GetDatabaseState()}");
            }
            // в методе по событию о решении можно отправить еще реквест

            // в другом классе здесь по ключам формируем вектор статы
            // для нейронки + инфа о хосте из реквеста
            // получаем обратно айди

            // также нужны подписки на события чтобы собирать награды между шагами

            // магическое условие если мы ничего подходящего не набрали
            // значит нужна доп реплика тк основных не осталось уже

            // достаем первый из очереди и смотрит в каком он состоянии
            // если уже ассимилирован, то берем некст
            // если не осталось то выходим
            // если нашли то реплицируем и добавляем в конец очереди
        }
        
        SendReply(request, resultsToSend);
    }

    private void OnDecisionReady(int action)
    {
        if (_pendingDecisionCallback != null)
        {
            _pendingDecisionCallback(action);
        }
    }

    private void CompleteDecision(ClientRequestData request, List<ResultData> resultsToSend, List<WorkunitModel> candidates, int action)
    {
        _decisionResponses++;
        LogTraining($"decision_response id={_decisionRequests} action={action} hasDecision={agent.HasDecision} candidates={candidates.Count}");

        if (action > 0 && action <= candidates.Count)
        {
            var selectedWorkunit = candidates[action - 1];
            if (selectedWorkunit.CurrentState == WorkunitModel.State.InProgress &&
                selectedWorkunit.CanCreateMoreResults())
            {
                var result = selectedWorkunit.CreateResult(TimeSystem.CurTick);
                if (result != null)
                {
                    result.isLearningResult = true;
                    resultsToSend.Add(result);
                    _pendingLearningResults.Add(GetResultKey(result.WorkunitName, result.resultNumber));
                    StatService.RecordResultCreated();
                    _createdLearningResults++;
                    LogTraining($"learning_result_created decision={_decisionRequests} workunit={result.WorkunitName} result={result.resultNumber} totalResults={selectedWorkunit.TotalResults}");
                }
            }
            else
            {
                _invalidActions++;
                LogTraining($"selected_workunit_unavailable decision={_decisionRequests} action={action} state={selectedWorkunit.CurrentState} totalResults={selectedWorkunit.TotalResults} maxCreated={selectedWorkunit.Config.MaxCreatedResults}");
            }
        }
        else if (action == 0)
        {
            _noWorkActions++;
            LogTraining($"no_work_action decision={_decisionRequests} candidates={candidates.Count}");
        }
        else
        {
            _invalidActions++;
            LogTraining($"invalid_action decision={_decisionRequests} action={action} candidates={candidates.Count}");
        }

        SendReply(request, resultsToSend);
        _pendingDecisionCallback = null;
        _pendingDecisionCompletedCondition.Signal();
    }

    private void SendReply(ClientRequestData request, List<ResultData> resultsToSend)
    {
        var tasksToSend = new List<ClientTaskData>();
        float inputTransferSizeMb = 0;
        foreach (var result in resultsToSend)
        {
            var applicationConfig = _database.Config.ApplicationConfigs[result.ApplicationIndex];
            result.sentTick = TimeSystem.CurTick;
            result.sentHostId = request.HostId;
            result.deadlineTick = TimeSystem.CurTick + applicationConfig.DelayBound;
            result.isSent = true;
            tasksToSend.Add(new ClientTaskData(
                result.WorkunitName,
                result.resultNumber,
                result.resultNumber,
                result.ApplicationIndex,
                result.durationInGflops,
                result.outputFileSizeMb,
                TimeSystem.CurTick,
                applicationConfig.DelayBound
            ));
            inputTransferSizeMb += result.inputFileSizeMb;

            if (_database.CurrentWorkunits.TryGetValue(result.WorkunitName, out var workunit))
            {
                workunit.SentResultNumbers.Enqueue(result.resultNumber);
                workunit.SentResults++;
            }
        }

        var reply = new ServerReplyData(tasksToSend, inputTransferSizeMb);
        _database.RecordHostSentResults(request.HostId, resultsToSend.Count);
        StatService.RecordResultsSent(resultsToSend.Count);
        StatService.RecordSentResults(tasksToSend.Count, TimeSystem.CurTick);
        if (resultsToSend.Count > 0 || ShouldLogRequest())
        {
            LogTraining($"reply_sent request={_workRequestsHandled} host={request.HostId} tasks={tasksToSend.Count} inputTransferSizeMb={inputTransferSizeMb} {GetDatabaseState()}");
        }
        _sendMessage(request.RequesterName, reply);
    }

    private List<WorkunitModel> SelectCandidates()
    {
        var workunits = _database.CurrentWorkunits.Values
            .Where(workunit =>
                workunit.CurrentState == WorkunitModel.State.InProgress &&
                workunit.CurrentErrorResults == 0 &&
                workunit.CanCreateMoreResults())
            .ToList();

        var candidates = new List<WorkunitModel>();
        while (candidates.Count < MaxDecisionCandidates && workunits.Count > 0)
        {
            int index = RandomUtils.GetRandomIndex(workunits.Count);
            candidates.Add(workunits[index]);
            workunits.RemoveAt(index);
        }

        return candidates;
    }

    private void OnResultCompleted(ResultCompletionData data)
    {
        if (!data.IsLearningResult)
        {
            return;
        }

        if (!_pendingLearningResults.Remove(data.ResultKey))
        {
            return;
        }

        float reward = -1f;
        int deadlineDuration = Mathf.Max(data.DeadlineTick - data.SendTick, 1);
        int activeCompletionTime = data.CompletionTick - data.SendTick;
        if (data.IsValid &&
            !data.IsServerTimeout &&
            !data.IsExtraResultAfterWorkunitValid &&
            activeCompletionTime <= deadlineDuration)
        {
            reward = 1f - (float)activeCompletionTime / deadlineDuration;
        }

        RecordReward(reward);
        _completedLearningResults++;
        LogTraining($"learning_result_completed key={data.ResultKey} host={data.HostId} valid={data.IsValid} serverTimeout={data.IsServerTimeout} extra={data.IsExtraResultAfterWorkunitValid} act={activeCompletionTime} reward={reward}");
    }

    private void OnTailFinished()
    {
        LogTraining($"tail_finished_event {GetSummary()} {GetDatabaseState()}");
        FinishLearningEpisode();
    }

    private void OnSimulationFinished()
    {
        LogTraining($"simulation_finished_event {GetSummary()}");
        FinishLearningEpisode();
    }

    private void FinishLearningEpisode()
    {
        if (_episodeEnded)
        {
            return;
        }

        _episodeEnded = true;
        _pendingDecisionCallback = null;
        _pendingDecisionCompletedCondition.Signal();
        LogTraining($"episode_finish {GetSummary()} rewardSum={_rewardSum} rewardCount={_rewardCount}");
        ApplyAccumulatedReward();
        if (agent != null)
        {
            agent.EndSchedulerEpisode();
        }
    }

    private void RecordReward(float reward)
    {
        _rewardSum += Mathf.Clamp(reward, -1f, 1f);
        _rewardCount++;
        _rewardEvents++;
    }

    private void ApplyAccumulatedReward()
    {
        if (agent == null || _rewardCount == 0)
        {
            return;
        }

        float reward = Mathf.Clamp(_rewardSum / _rewardCount, -1f, 1f);
        LogTraining($"reward_flush value={reward} sum={_rewardSum} count={_rewardCount}");
        agent.ApplySchedulerReward(reward);
        _rewardSum = 0;
        _rewardCount = 0;
    }

    private string GetResultKey(string workunitName, int resultNumber)
    {
        return $"{workunitName}:{resultNumber}";
    }

    private bool ShouldLogRequest()
    {
        return _workRequestsHandled <= 20 || _workRequestsHandled % 500 == 0;
    }

    private string GetDatabaseState()
    {
        return $"currentWorkunits={_database.CurrentWorkunits.Count} currentResults={_database.CurrentResults.Count} currentErrorResults={_database.CurrentErrorResults.Count} currentValidations={_database.CurrentValidations.Count} currentAssimilations={_database.CurrentAssimilations.Count} clientRequests={_database.ClientRequests.Count}";
    }

    private string GetCandidateState(List<WorkunitModel> candidates)
    {
        int outstanding = 0;
        int totalResults = 0;
        int validResults = 0;
        int errorResults = 0;
        int successResults = 0;
        foreach (var candidate in candidates)
        {
            outstanding += Mathf.Max(candidate.SentResults - candidate.ReceivedResults, 0);
            totalResults += candidate.TotalResults;
            validResults += candidate.ValidResults;
            errorResults += candidate.ErrorResults;
            successResults += candidate.SuccessResults;
        }

        return $"candidateOutstanding={outstanding} candidateTotalResults={totalResults} candidateValidResults={validResults} candidateErrorResults={errorResults} candidateSuccessResults={successResults}";
    }

    private string GetSummary()
    {
        return $"requests={_workRequestsHandled} readyBypass={_readyBypassRequests} emptyResultRequests={_emptyResultRequests} decisionRequests={_decisionRequests} decisionResponses={_decisionResponses} learningCreated={_createdLearningResults} noWorkActions={_noWorkActions} invalidActions={_invalidActions} agentMissing={_agentMissingRequests} learningCompleted={_completedLearningResults} rewardEvents={_rewardEvents} pendingLearning={_pendingLearningResults.Count}";
    }

    private void LogTraining(string message)
    {
        if (EntryPoint.Instance.IsTrainingWorker)
        {
            Debug.Log($"[LearningScheduler] tick={TimeSystem.CurTick} episode={EntryPoint.Instance.EpisodeIndex} {message}");
        }
    }
}

public class SchedulerDecisionContext
{
    public readonly ClientRequestData Request;
    public readonly List<WorkunitModel> Candidates;
    public readonly float[] HostObservations;
    public readonly List<float[]> TaskObservations;

    public SchedulerDecisionContext(
        ClientRequestData request,
        List<WorkunitModel> candidates,
        float[] hostObservations,
        List<float[]> taskObservations)
    {
        Request = request;
        Candidates = candidates;
        HostObservations = hostObservations;
        TaskObservations = taskObservations;
    }
}

public class LearningObservationBuilder
{
    public const int HostObservationSize = 4;
    public const int TaskObservationSize = 7;
    private GroupConfig GroupConfig => Container.Instance.ConfigProvider.SimConfig.GroupConfig;

    public SchedulerDecisionContext Build(ClientRequestData request, List<WorkunitModel> candidates, ProjectDatabase database)
    {
        return new SchedulerDecisionContext(
            request,
            candidates,
            BuildHostObservations(request, database),
            BuildTaskObservations(candidates, database)
        );
    }

    private float[] BuildHostObservations(ClientRequestData request, ProjectDatabase database)
    {
        int sentResults = GetStatistic(database.HostSentResults, request.HostId);
        int returnedResults = GetStatistic(database.HostReturnedResults, request.HostId);
        int validResults = GetStatistic(database.HostValidResults, request.HostId);
        float maxDelayBound = GetMaxDelayBound(database);

        return new[]
        {
            Normalize(request.Power, GroupConfig.MaxSpeed),
            Normalize(request.Percentage, maxDelayBound),
            sentResults > 0 ? Normalize(returnedResults, sentResults) : 0,
            returnedResults > 0 ? Normalize(validResults, returnedResults) : 0
        };
    }

    private List<float[]> BuildTaskObservations(List<WorkunitModel> candidates, ProjectDatabase database)
    {
        var observations = new List<float[]>();
        float maxTaskGflops = GetMaxTaskGflops(database);
        float maxDelayBound = GetMaxDelayBound(database);

        foreach (var workunit in candidates)
        {
            observations.Add(new[]
            {
                Normalize(workunit.Config.TaskGflops, maxTaskGflops),
                Normalize(workunit.Config.DelayBound, maxDelayBound),
                Normalize(Mathf.Max(workunit.MinQuorum - workunit.ValidResults, 0), workunit.MinQuorum),
                Normalize(Mathf.Max(workunit.SentResults - workunit.ReceivedResults, 0), workunit.Config.MaxCreatedResults),
                Normalize(workunit.TotalResults, workunit.Config.MaxCreatedResults),
                Normalize(workunit.ErrorResults, workunit.Config.MaxErrorResults),
                Normalize(workunit.SuccessResults, workunit.Config.MaxSuccessResults)
            });
        }

        return observations;
    }

    private int GetStatistic(Dictionary<int, int> statistics, int hostId)
    {
        return statistics.TryGetValue(hostId, out var value) ? value : 0;
    }

    private float GetMaxTaskGflops(ProjectDatabase database)
    {
        float value = 0;
        foreach (var applicationConfig in database.Config.ApplicationConfigs)
        {
            value = Mathf.Max(value, applicationConfig.TaskGflops);
        }

        return value;
    }

    private float GetMaxDelayBound(ProjectDatabase database)
    {
        float value = 0;
        foreach (var applicationConfig in database.Config.ApplicationConfigs)
        {
            value = Mathf.Max(value, applicationConfig.DelayBound);
        }

        return value;
    }

    private float Normalize(float value, float maxValue)
    {
        if (maxValue <= 0)
        {
            return 0;
        }

        return Mathf.Clamp01(value / maxValue);
    }
}
