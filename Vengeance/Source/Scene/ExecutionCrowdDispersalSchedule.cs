namespace RichExecutions.Scene;

// Only the already registered crowd is visited, once per interval. No catch-up
// loop after a long frame, and a broken agent cannot block everyone behind it.
internal sealed class ExecutionCrowdDispersalSchedule
{
    internal const float IntervalSeconds = 0.35f;
    internal const int MaximumAttempts = 3;
    private int _nextIndex;
    private int _attempts;
    private float _remaining;

    internal bool TryTakeNext(float dt, int count, out int index)
    {
        index = -1;
        if (_nextIndex >= count || float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0f)
            return false;
        _remaining -= dt;
        if (_remaining > 0f) return false;
        _remaining = IntervalSeconds;
        index = _nextIndex;
        return true;
    }

    internal void CompleteAttempt(bool completed)
    {
        if (completed || ++_attempts >= MaximumAttempts)
        {
            _nextIndex++;
            _attempts = 0;
        }
    }
}
