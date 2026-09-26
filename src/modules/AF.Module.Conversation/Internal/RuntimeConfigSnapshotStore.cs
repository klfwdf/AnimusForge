using System;
using System.Threading;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Owns the process-local immutable configuration snapshot used by detached
/// interactions. Reload builds a complete replacement before publishing it;
/// an interaction that already captured the previous instance is therefore
/// isolated from later MCM/config changes.
/// </summary>
public sealed class RuntimeConfigSnapshotStore
{
    private readonly Func<RuntimeConfigSnapshot> _snapshotFactory;
    private RuntimeConfigSnapshot _current;

    public RuntimeConfigSnapshotStore(Func<RuntimeConfigSnapshot> snapshotFactory)
    {
        _snapshotFactory = snapshotFactory ?? throw new ArgumentNullException(nameof(snapshotFactory));
        RuntimeConfigSnapshot initial = BuildSnapshot();
        if (initial == null)
        {
            throw new InvalidOperationException("The runtime configuration factory returned no snapshot.");
        }
        _current = initial;
    }

    /// <summary>
    /// Returns the currently published immutable instance. This is a single
    /// atomic reference read and does not lock or rebuild configuration.
    /// </summary>
    public RuntimeConfigSnapshot Capture()
    {
        return Volatile.Read(ref _current);
    }

    /// <summary>
    /// Builds a replacement off the publication path and atomically publishes
    /// it only when the factory succeeds. A failed reload leaves the last good
    /// snapshot available to future requests.
    /// </summary>
    public bool TryReload(out RuntimeConfigSnapshot publishedSnapshot)
    {
        publishedSnapshot = null;
        RuntimeConfigSnapshot replacement;
        try
        {
            replacement = BuildSnapshot();
        }
        catch
        {
            return false;
        }
        if (replacement == null)
        {
            return false;
        }

        Interlocked.Exchange(ref _current, replacement);
        publishedSnapshot = replacement;
        return true;
    }

    private RuntimeConfigSnapshot BuildSnapshot()
    {
        return _snapshotFactory();
    }
}
