using UnityEngine;
using System;
using System.Collections.Generic;



public enum TaskState
{
    Error, // когда превышается один из 3х лимитов и юнит не годен больше
    Valid, // кворум одинаковых успешных результатов
    InProgress, // обычное состояние
}

public class TaskModel
{
    // поля параметров
    private readonly TaskConfig _config;

    // задается в конструкторе
    public Guid Id {get;}

    public TaskState CurState {get; private set;}

    public int CurCreatedResults {get; private set;}
    public int CurSentResults {get; private set;}
    public int CurResultsReceived {get; private set;}
    public int CurValidResults {get; private set;}
    public int CurSuccessResults {get; private set;}
    public int CurErrorResults {get; private set;}

    // n input files {get;}
    // List <str> input files {get;}


    public TaskModel(TaskConfig config, int id)
    {
        //_config = config;
        //Id = id;
        CurState = TaskState.InProgress;
    }
}
