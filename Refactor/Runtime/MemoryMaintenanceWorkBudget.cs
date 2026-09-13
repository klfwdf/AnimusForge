using System;
using System.Diagnostics;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// One cooperative maintenance window. Its real consumers share elapsed time and
/// operation grants; it cannot preempt a single large operation. No game objects.
/// </summary>
internal sealed class MemoryMaintenanceWorkBudget
{
    internal readonly long Start;
    internal readonly double Milliseconds;
    internal readonly bool Unbounded;
    internal int Metadata;
    internal int Expensive;

    internal MemoryMaintenanceWorkBudget(long start, double milliseconds, int metadata, int expensive)
    {
        Start = start;
        Milliseconds = milliseconds;
        Metadata = metadata;
        Expensive = expensive;
        // Keep the existing explicit synchronous/unlimited caller contract.
        Unbounded = start <= 0L || milliseconds <= 0.0 || milliseconds == double.MaxValue
            || double.IsInfinity(milliseconds) || double.IsNaN(milliseconds);
    }

    internal bool IsExceeded => !Unbounded
        && (Stopwatch.GetTimestamp() - Start) * 1000.0 / Stopwatch.Frequency >= Milliseconds;

    internal bool Take(bool expensive)
    {
        if (Unbounded) return true;
        if (IsExceeded) return false;
        if (expensive) { if (Expensive <= 0) return false; Expensive--; }
        else { if (Metadata <= 0) return false; Metadata--; }
        return true;
    }
}
