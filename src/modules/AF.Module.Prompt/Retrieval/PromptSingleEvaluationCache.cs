using System;

namespace AnimusForge;

// One-entry derived evaluation cache; publication is guarded against late old-revision work.
internal sealed class PromptSingleEvaluationCache<T> where T : class
{
    private readonly object _gate = new object();
    private string _key;
    private long _revision;
    private T _value;

    internal bool TryGet(string key, long revision, out T value)
    {
        lock (_gate)
        {
            value = _revision == revision && string.Equals(_key, key, StringComparison.Ordinal) ? _value : null;
            return value != null;
        }
    }

    // The caller holds the configuration publication gate while supplying liveRevision.
    internal void Publish(string key, T value, long computedRevision, long liveRevision)
    {
        if (value == null || computedRevision != liveRevision) return;
        lock (_gate)
        {
            _key = key;
            _value = value;
            _revision = computedRevision;
        }
    }

    internal void Clear()
    {
        lock (_gate)
        {
            _key = null;
            _value = null;
            _revision = 0;
        }
    }
}
