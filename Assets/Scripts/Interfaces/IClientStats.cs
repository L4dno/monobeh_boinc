using System;
using System.Collections.Generic;


public interface IClientStats
{
    // присылает активную мощность в каунтер
    event Action<string, int> OnGoingOffline;

    event Action<string, int> OnGoingOnline;

    // присылает полезную мощность в каунтер
    event Action<string, int> OnBusyMode;

    event Action<string, int> OnIdleMode;
}