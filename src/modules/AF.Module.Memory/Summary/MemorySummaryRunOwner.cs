using System;
using System.Threading;

namespace AnimusForge.Refactor.Runtime;

// Owns only one summary run's authority. It never owns persisted memory, game objects,
// another dispatch queue or HTTP cancellation. Reset cannot undo an already entered write.
internal sealed class MemorySummaryRunOwner
{
    private Lease _current;
    internal bool IsRunning => Volatile.Read(ref _current) != null;

    internal Lease TryBegin(long generation)
    {
        var candidate = new Lease(this, generation);
        return Interlocked.CompareExchange(ref _current, candidate, null) == null ? candidate : null;
    }

    internal void Reset() => Interlocked.Exchange(ref _current, null);

    internal sealed class Lease : IDisposable
    {
        private readonly MemorySummaryRunOwner _owner;
        internal long Generation { get; }
        internal Lease(MemorySummaryRunOwner owner, long generation) { _owner = owner; Generation = generation; }
        internal bool IsCurrent => ReferenceEquals(Volatile.Read(ref _owner._current), this);
        public void Dispose() => Interlocked.CompareExchange(ref _owner._current, null, this);
    }
}
