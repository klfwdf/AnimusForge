using System;

namespace AnimusForge;

// Derived configuration is reusable only within the revision that produced it.
internal sealed class PromptRevisionedDerivedCache<T> where T : class
{
    private readonly object _gate = new object();
    private long _revision = -1;
    private T _value;

    internal T GetOrBuild(long revision, Func<long> currentRevision, Func<T> build)
    {
        if (currentRevision == null) throw new ArgumentNullException(nameof(currentRevision));
        if (build == null) throw new ArgumentNullException(nameof(build));
        lock (_gate)
        {
            if (_revision == revision && _value != null) return _value;
            T replacement = build();
            // An old async request may finish after reload; it may use its local
            // result, but must never replace the current generation's cache.
            if (revision == currentRevision())
            {
                _value = replacement;
                _revision = revision;
            }
            return replacement;
        }
    }
}
