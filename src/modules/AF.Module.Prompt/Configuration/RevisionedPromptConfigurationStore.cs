using System;
using System.Threading;

namespace AnimusForge;

internal sealed class PromptConfigurationRevision<T> where T : class
{
    internal long Revision { get; }
    internal T Value { get; }

    internal PromptConfigurationRevision(long revision, T value)
    {
        Revision = revision;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}

// Loading is serialized, but readers only perform one atomic reference read.
internal sealed class RevisionedPromptConfigurationStore<T> where T : class
{
    private readonly object _reloadGate = new object();
    private readonly AsyncLocal<PromptConfigurationRevision<T>> _captured = new AsyncLocal<PromptConfigurationRevision<T>>();
    private PromptConfigurationRevision<T> _current;

    internal RevisionedPromptConfigurationStore(T initial)
    {
        _current = new PromptConfigurationRevision<T>(1, initial);
    }

    internal PromptConfigurationRevision<T> Capture() => Volatile.Read(ref _current);

    internal PromptConfigurationRevision<T> Read() => _captured.Value ?? Capture();

    internal IDisposable BeginCapture()
    {
        var previous = _captured.Value;
        _captured.Value = Read();
        return new CaptureScope(this, previous);
    }

    private sealed class CaptureScope : IDisposable
    {
        private RevisionedPromptConfigurationStore<T> _owner;
        private readonly PromptConfigurationRevision<T> _previous;

        internal CaptureScope(RevisionedPromptConfigurationStore<T> owner, PromptConfigurationRevision<T> previous)
        {
            _owner = owner;
            _previous = previous;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            if (owner != null) owner._captured.Value = _previous;
        }
    }

    internal PromptConfigurationRevision<T> Reload(Func<T> load, Func<Exception, T> fallback)
    {
        if (load == null) throw new ArgumentNullException(nameof(load));
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        lock (_reloadGate)
        {
            T replacement;
            try { replacement = load(); }
            catch (Exception ex) { replacement = fallback(ex); }
            var next = new PromptConfigurationRevision<T>(_current.Revision + 1, replacement);
            Volatile.Write(ref _current, next);
            return next;
        }
    }
}
