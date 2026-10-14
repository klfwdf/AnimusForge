using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

// Main-thread daily/load world pass: one complete market per application frame.
// No inventory cache, rule epoch or cross-frame slot cursor.
internal sealed class GeneratedRewardMarketCleanupOwner
{
    private readonly Func<ItemObject, bool> _isGenerated, _isBlocked;
    private readonly Func<long> _generation;
    private readonly Func<bool> _isCurrent;
    private readonly Action<string, string, int, int> _removed;
    private readonly Action<string, string, Exception> _failure;
    private readonly Action<string> _trace;
    private long _boundGeneration;
    private Func<IEnumerable<Settlement>> _worldSource;
    private List<Settlement> _markets;
    private int _nextMarket, _roundSlots, _roundRemoved, _roundFailures;
    private bool _rerun;
    private string _reason, _slowestMarket;
    private double _roundMs, _maxMarketMs;

    internal GeneratedRewardMarketCleanupOwner(Func<ItemObject, bool> isGenerated, Func<ItemObject, bool> isBlocked,
        Func<long> generation, Func<bool> isCurrent,
        Action<string, string, int, int> removed, Action<string, string, Exception> failure, Action<string> trace)
    {
        _isGenerated = isGenerated; _isBlocked = isBlocked; _generation = generation; _isCurrent = isCurrent;
        _removed = removed; _failure = failure; _trace = trace; _boundGeneration = generation();
    }
    internal bool HasWork => _worldSource != null;
    internal void Reset()
    {
        _worldSource = null; _markets = null; _nextMarket = 0; _rerun = false;
        _boundGeneration = _generation();
    }
    internal void BeginWorldScan(Func<IEnumerable<Settlement>> source, string reason)
    {
        if (!_isCurrent()) return;
        if (_boundGeneration != _generation()) Reset();
        if (HasWork) { _rerun = true; return; }
        _worldSource = source; _reason = reason;
    }

    internal int CleanNow(ItemRoster roster, string label, string reason) => CleanMarket(roster, label, reason, out _, out _);
    private int CleanMarket(ItemRoster roster, string label, string reason, out int visited, out bool failed)
    {
        visited = 0; failed = false;
        if (roster == null || !_isCurrent()) return 0;
        if (_boundGeneration != _generation()) Reset();
        long generation = _boundGeneration;
        int removed = 0, removedSlots = 0;
        List<ItemRosterElement> candidates = null;
        try
        {
            int count = roster.Count;
            int expectedVersion = roster.VersionNo;
            for (int i = 0; i < count; i++)
            {
                ItemRosterElement element = roster.GetElementCopyAtIndex(i);
                visited++;
                if (element.Amount > 0 && _isGenerated(element.EquipmentElement.Item)
                    && _isBlocked(element.EquipmentElement.Item))
                    (candidates ??= new List<ItemRosterElement>()).Add(element);
            }
            if (candidates != null)
                foreach (ItemRosterElement candidate in candidates)
                {
                    if (!_isCurrent() || generation != _generation()) break;
                    // Our own removals leave other candidate amounts unchanged.
                    // Only re-resolve after another handler changes the inventory;
                    // AddToCounts already performs the normal item/modifier lookup.
                    if (!_isBlocked(candidate.EquipmentElement.Item)) continue;
                    int amount = candidate.Amount;
                    if (roster.VersionNo != expectedVersion)
                    {
                        int index = roster.FindIndexOfElement(candidate.EquipmentElement);
                        if (index < 0) continue;
                        amount = roster.GetElementNumber(index);
                    }
                    if (amount <= 0) continue;
                    int version = roster.VersionNo;
                    roster.AddToCounts(candidate.EquipmentElement, -amount);
                    if (roster.VersionNo == version)
                        throw new InvalidOperationException("market_roster_remove_made_no_progress");
                    // Keep the mismatch after an external mutation: later snapshots
                    // may also be stale even if this particular deletion was normal.
                    if (version == expectedVersion) expectedVersion = unchecked(version + 1);
                    removed += amount; removedSlots++;
                }
        }
        catch (Exception error) { failed = true; ReportFailure(label, reason, error); }
        if (removed > 0)
        {
            try { _removed?.Invoke(label, reason, removed, removedSlots); }
            catch (Exception) { } // Logging cannot undo an inventory operation.
        }
        return removed;
    }

    internal void Tick()
    {
        if (!HasWork || !_isCurrent()) return;
        if (_boundGeneration != _generation()) { Reset(); return; }
        if (_markets == null)
        {
            // Copy settlement references once, never the world's goods. Added
            // settlements enter the next pass. Snapshot cost is included in workMs.
            long started = Stopwatch.GetTimestamp();
            try { _markets = _worldSource()?.ToList() ?? new List<Settlement>(); }
            catch (Exception error) { ReportFailure("world", _reason, error); Reset(); return; }
            _nextMarket = _roundSlots = _roundRemoved = _roundFailures = 0;
            _roundMs = ElapsedMs(started); _maxMarketMs = 0; _slowestMarket = "";
        }
        if (_nextMarket < _markets.Count)
        {
            Settlement settlement = _markets[_nextMarket++];
            long started = Stopwatch.GetTimestamp();
            string label = "unknown";
            try
            {
                label = settlement?.StringId ?? "none";
                _roundRemoved += CleanMarket(settlement?.ItemRoster, label, _reason, out int visited, out bool failed);
                _roundSlots += visited;
                if (failed) _roundFailures++;
            }
            catch (Exception error) { _roundFailures++; ReportFailure(label, _reason, error); }
            if (_boundGeneration != _generation()) { Reset(); return; }
            double elapsed = ElapsedMs(started);
            _roundMs += elapsed;
            if (elapsed > _maxMarketMs) { _maxMarketMs = elapsed; _slowestMarket = label; }
            if (elapsed >= 8.0) Trace("market_sweep_slow owner=" + label + " elapsedMs=" + Number(elapsed));
        }
        if (_nextMarket < _markets.Count) return;
        Trace("market_sweep_completed reason=" + _reason + " markets=" + _markets.Count + " slots=" + _roundSlots
            + " removed=" + _roundRemoved + " failures=" + _roundFailures + " workMs=" + Number(_roundMs)
            + " maxMarketMs=" + Number(_maxMarketMs) + " slowest=" + _slowestMarket);
        _markets = null; _nextMarket = 0;
        if (_rerun) { _rerun = false; _reason = "coalesced_daily_tick"; }
        else _worldSource = null;
    }
    private static double ElapsedMs(long start) => (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private void Trace(string message)
    {
        try { _trace?.Invoke(message); }
        catch (Exception) { } // Optional diagnostics must not stall the pass.
    }
    private void ReportFailure(string label, string reason, Exception error)
    {
        try { _failure?.Invoke(label, reason, error); }
        catch (Exception) { } // The next daily/trade entry can retry independently.
    }
}
