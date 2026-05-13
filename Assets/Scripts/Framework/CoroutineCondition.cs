using System.Collections;

public sealed class CoroutineCondition
{
    private int _waiters;
    private int _signals;

    public IEnumerator Wait()
    {
        _waiters++;

        while (_signals == 0)
        {
            yield return null;
        }

        _signals--;
        _waiters--;
    }

    public IEnumerator TimedWait(int ticksToWait)
    {
        int targetTick = Container.Instance.TimeSystem.CurTick + ticksToWait;
        _waiters++;

        while (_signals == 0 && Container.Instance.TimeSystem.CurTick < targetTick)
        {
            yield return null;
        }

        if (_signals > 0)
        {
            _signals--;
        }

        _waiters--;
    }

    public void Signal()
    {
        if (_signals < _waiters)
        {
            _signals++;
        }
    }
}
