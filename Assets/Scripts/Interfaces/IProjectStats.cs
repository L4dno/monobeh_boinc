using System;
using System.Collections.Generic;

public interface IProjectStats
{
    // шаблон от подпроекта, который реплицируется
    event Action<int> OnWorkunitCreated;

    event Action<int> OnWorkunitValid;

    event Action OnWorkunitCompleted;
}
