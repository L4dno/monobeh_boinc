using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// сущность живущая во времени симуляции. Каждую секунду проверяет состояние.
public abstract class BaseActor
{   

    private HostModel _host;

    public abstract IEnumerator MainLoop(HostModel host);

}