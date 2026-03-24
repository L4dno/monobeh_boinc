using System;
using System.Collections.Generic;

public interface IProjectStats
{
    // шаблон от подпроекта, который реплицируется
    event Action OnWorkunitCreated;

    event Action OnWorkunitCompleted;
}