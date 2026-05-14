using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LearningScheduler : IScheduler
{
    private ProjectDatabase _database;
    private Func<string, IMessage, MessageComm> _sendMessage;
    private TimeTickSystem TimeSystem => Container.Instance.TimeSystem;
    private IStatService StatService => Container.Instance.StatService;
    private const int SchedulerResultWaitTimeoutTicks = 5;


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

        // здесь видимо надо создать очередь
        // но ведь могут еще быть основные реплики

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

                // если пустое окно реплик
                if (_database.CurrentResults.Count == 0)
                {
                    // ждем создания новых или обрабатываем ответ с пустой очередью
                    yield return _database.ResultAvailableCondition.TimedWait(SchedulerResultWaitTimeoutTicks);
                }

                ProcessWorkRequest(request);
            }

        }
    }

    private void ProcessWorkRequest(ClientRequestData request)
    {
        var resultsToSend = new List<ResultData>();
        // если есть реплики стартовые
        if (_database.CurrentResults.Count > 0)
        {
            var firstResult = _database.CurrentResults.Dequeue();
            resultsToSend.Add(firstResult);
            int resultsNumber = request.CalculateResultsNumber(firstResult);

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
        }

        // впоследствии тут будет проверка что еще не набрали рюкзак ответа
        if (resultsToSend.Count < 1)
        {
            // берем ключи и случайно берем индекс реплики
            var keys = _database.CurrentWorkunits.Keys.ToList();
            int index = RandomUtils.GetRandomIndex(keys.Count);
            var key = keys[index];

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
