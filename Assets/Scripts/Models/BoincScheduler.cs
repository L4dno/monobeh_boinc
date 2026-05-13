using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BoincScheduler : IScheduler
{
    private ProjectDatabase _database;
    private Func<string, IMessage, MessageComm> _sendMessage;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;
    private IStatService StatService => Container.Instance.StatService;
    private const int SchedulerResultWaitTimeoutTicks = 5;

    public BoincScheduler()
    {
        // знает только текущий список воркюнитов и список реплик
        // пока можем сгенерировать новых воркюнитов пробуждаем поток
    }

    public IEnumerator Run(ProjectDatabase database, Func<string, IMessage, MessageComm> sendMessage)
    {
        _database = database;
        _sendMessage = sendMessage;
        while (true)
        {
            while (_database.ClientRequests.Count == 0)
            {
                // нет новых запросов
                yield return _database.ClientRequestAvailableCondition.Wait();
            }

            while (_database.ClientRequests.Count > 0)
            {
                // берем новый запрос от клиента
                var request = _database.ClientRequests.Dequeue();

                // ожидание генерации воркюнитов, лучше напрямую вызвать
                if (_database.CurrentResults.Count == 0)
                {
                    yield return _database.ResultAvailableCondition.TimedWait(SchedulerResultWaitTimeoutTicks);
                }

                ProcessWorkRequest(request);
            }

        }
    }

    private void ProcessWorkRequest(ClientRequestData request)
    {
        var resultsToSend = new List<ResultData>();

        if (_database.CurrentResults.Count > 0)
        {
            var firstResult = _database.CurrentResults.Dequeue();
            resultsToSend.Add(firstResult);
            int resultsNumber = request.CalculateResultsNumber(firstResult);

            while (resultsToSend.Count < resultsNumber && _database.CurrentResults.Count > 0)
            {
                resultsToSend.Add(_database.CurrentResults.Dequeue());
            }

            if (_database.CurrentResults.Count == 0)
            {
                _database.ResultBufferHasSpaceCondition.Signal();
            }
        }

        
        var tasksToSend = new List<ClientTaskData>();
        float inputTransferSizeMb = 0;
        foreach (var result in resultsToSend)
        {
            var applicationConfig = _database.Config.ApplicationConfigs[result.ApplicationIndex];
            result.sentTick = TimeSystem.CurTick;
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
        _sendMessage(request.RequesterName, reply);
    }

    
    
}
