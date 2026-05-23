using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using System;

[RequireComponent(typeof(BehaviorParameters))]
[RequireComponent(typeof(BufferSensorComponent))]
public class SchedulerAgent : Agent
{
    [SerializeField] private BufferSensorComponent taskBuffer;
    private const int MaxTasks = 32;

    public Action<int> onDecisionReady;
    public bool HasDecision { get; private set; }
    public int SelectedAction { get; private set; }
    private SchedulerDecisionContext _context;

    public void ConfigureComponents()
    {
        var behaviorParameters = GetComponent<BehaviorParameters>();
        if (behaviorParameters == null)
        {
            behaviorParameters = gameObject.AddComponent<BehaviorParameters>();
        }

        behaviorParameters.BrainParameters.VectorObservationSize = LearningObservationBuilder.HostObservationSize;
        behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(MaxTasks + 1);
        behaviorParameters.BehaviorName = "TailScheduler";

        taskBuffer = GetComponent<BufferSensorComponent>();
        if (taskBuffer == null)
        {
            taskBuffer = gameObject.AddComponent<BufferSensorComponent>();
        }

        taskBuffer.MaxNumObservables = MaxTasks;
        taskBuffer.ObservableSize = LearningObservationBuilder.TaskObservationSize;
        LogTraining($"configured behavior={behaviorParameters.BehaviorName} vectorSize={behaviorParameters.BrainParameters.VectorObservationSize} actionBranches={behaviorParameters.BrainParameters.ActionSpec.NumDiscreteActions} bufferSize={taskBuffer.ObservableSize} bufferMax={taskBuffer.MaxNumObservables}");
    }

    public void SetDecisionContext(SchedulerDecisionContext context)
    {
        _context = context;
        HasDecision = false;
        SelectedAction = 0;
        LogTraining($"context_set candidates={GetCandidatesCount()}");
    }

    public void ApplySchedulerReward(float reward)
    {
        LogTraining($"reward_applied value={reward}");
        AddReward(reward);
    }

    public void EndSchedulerEpisode()
    {
        LogTraining("episode_end");
        EndEpisode();
    }

    // 0 ничего не выдаем
    // иначе N - 1 индекс
    public override void OnActionReceived(ActionBuffers actions)
    {
        int index = actions.DiscreteActions[0];
        SelectedAction = Mathf.Clamp(index, 0, MaxTasks);
        HasDecision = true;
        LogTraining($"action_received raw={index} selected={SelectedAction} candidates={GetCandidatesCount()}");
        onDecisionReady?.Invoke(SelectedAction);
    }

    // маскировать штуки
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        int candidatesCount = _context != null ? _context.Candidates.Count : 0;
        for (int i = candidatesCount + 1; i <= MaxTasks; i++)
        {
            actionMask.SetActionEnabled(0, i, false);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // сюда 4 параметра клиента
        if (_context == null)
        {
            for (int i = 0; i < LearningObservationBuilder.HostObservationSize; i++)
            {
                sensor.AddObservation(0);
            }

            return;
        }

        for (int i = 0; i < LearningObservationBuilder.HostObservationSize; i++)
        {
            float value = i < _context.HostObservations.Length ? _context.HostObservations[i] : 0;
            sensor.AddObservation(value);
        }

        if (taskBuffer != null)
        {
            foreach (var taskObservation in _context.TaskObservations)
            {
                taskBuffer.AppendObservation(taskObservation);
            }
        }
    }

    private int GetCandidatesCount()
    {
        return _context != null ? _context.Candidates.Count : 0;
    }

    private void LogTraining(string message)
    {
        if (EntryPoint.Instance != null && EntryPoint.Instance.IsTrainingWorker)
        {
            Debug.Log($"[SchedulerAgent] {message}");
        }
    }
}
