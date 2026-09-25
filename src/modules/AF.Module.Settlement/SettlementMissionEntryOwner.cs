using System;

namespace AnimusForge.Refactor.Modules;

// Owns one transient settlement entry ticket; TaleWorlds Mission setup remains in the host.
internal sealed class SettlementMissionEntryOwner<TEntry> where TEntry : class
{
    private readonly Func<TEntry, string> _settlementId;
    private readonly Func<TEntry, bool> _villageAftermath;
    private readonly Func<TEntry, DateTime> _createdUtc;

    internal SettlementMissionEntryOwner(Func<TEntry, string> settlementId,
        Func<TEntry, bool> villageAftermath, Func<TEntry, DateTime> createdUtc)
    {
        _settlementId = settlementId;
        _villageAftermath = villageAftermath;
        _createdUtc = createdUtc;
    }

    internal TEntry Pending { get; private set; }

    internal void Set(TEntry entry) => Pending = entry;

    internal bool CancelVillageAftermath(string settlementId, out TEntry cancelled)
    {
        cancelled = Pending;
        if (cancelled == null || !_villageAftermath(cancelled)
            || !string.IsNullOrWhiteSpace(settlementId)
                && !string.Equals(_settlementId(cancelled), settlementId, StringComparison.OrdinalIgnoreCase))
        {
            cancelled = null;
            return false;
        }
        Pending = null;
        return true;
    }

    internal bool TryConsumeForMission(string currentSettlementId, out TEntry entry, out bool mismatch)
    {
        entry = Pending;
        mismatch = false;
        if (entry == null) return false;
        Pending = null;
        string expected = _settlementId(entry);
        mismatch = !string.IsNullOrWhiteSpace(expected)
            && !string.Equals(currentSettlementId, expected, StringComparison.Ordinal);
        return !mismatch;
    }

    internal bool TryExpire(DateTime utcNow, bool missionActive, TimeSpan lifetime, out TEntry expired)
    {
        expired = Pending;
        if (expired == null || missionActive || _createdUtc(expired) == DateTime.MinValue
            || (utcNow - _createdUtc(expired)) <= lifetime)
        {
            expired = null;
            return false;
        }
        Pending = null;
        return true;
    }
}
