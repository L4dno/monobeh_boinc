using System;
using System.Collections.Generic;


public interface IClientStats
{
    // присылает активную мощность в каунтер
    event Action<string, float> OnGoingOffline;

    event Action<string, float> OnGoingOnline;

    // присылает полезную мощность в каунтер
    event Action<string, float> OnBusyMode;

    event Action<string, float> OnIdleMode;
}