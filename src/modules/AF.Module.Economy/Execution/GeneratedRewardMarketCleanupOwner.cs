using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

// Runtime-only, main-thread state. Each queued step visits one slot, not one whole market.
internal sealed class GeneratedRewardMarketCleanupOwner
{
    private sealed class Scan
    {
        internal int Version, Cursor;
        internal long Rules;
        internal bool Complete, HasTradableGeneratedItems;
        internal void Start(int version, long rules)
        { Version = version; Rules = rules; Cursor = 0; Complete = HasTradableGeneratedItems = false; }
    }
    private sealed class Work
    {
        internal Settlement Settlement;
        internal ItemRoster Roster;
        internal string Label, Reason;
    }
    private sealed class RosterComparer : IEqualityComparer<ItemRoster>
    {
        public bool Equals(ItemRoster x, ItemRoster y) => ReferenceEquals(x, y);
        public int GetHashCode(ItemRoster value) => RuntimeHelpers.GetHashCode(value);
    }
    private static readonly ConditionalWeakTable<ItemRoster, Scan>.CreateValueCallback CreateScan = _ => new Scan();
    private ConditionalWeakTable<ItemRoster, Scan> _scans = new ConditionalWeakTable<ItemRoster, Scan>();
    private readonly Queue<Work> _pending = new Queue<Work>();
    private readonly Dictionary<ItemRoster, Work> _queued = new Dictionary<ItemRoster, Work>(new RosterComparer());
    private readonly Dictionary<Settlement, Work> _bySettlement = new Dictionary<Settlement, Work>();
    private readonly Func<ItemObject, bool> _isGenerated, _isBlocked;
    private readonly Func<long> _generation, _rules;
    private readonly Func<bool> _isCurrent;
    private readonly Action<string, string, int, int> _removed;
    private readonly Action<string, string, Exception> _failure;
    private long _boundGeneration;
    private Func<IEnumerable<Settlement>> _worldSource;
    private IEnumerator<Settlement> _world;
    private string _worldReason;
    private bool _admitWorld = true, _worldFailureLogged;

    internal GeneratedRewardMarketCleanupOwner(Func<ItemObject, bool> isGenerated, Func<ItemObject, bool> isBlocked,
        Func<long> generation, Func<long> rules, Func<bool> isCurrent,
        Action<string, string, int, int> removed, Action<string, string, Exception> failure)
    {
        _isGenerated = isGenerated; _isBlocked = isBlocked; _generation = generation; _rules = rules;
        _isCurrent = isCurrent; _removed = removed; _failure = failure;
        _boundGeneration = generation();
    }
    internal int PendingCount => _queued.Count;
    internal bool HasWorldScan => _worldSource != null;
    internal bool HasWork => PendingCount > 0 || HasWorldScan;

    internal void Reset()
    {
        CloseWorld(); _worldSource = null;
        _pending.Clear(); _queued.Clear(); _bySettlement.Clear(); _scans = new ConditionalWeakTable<ItemRoster, Scan>();
        _boundGeneration = _generation(); _admitWorld = true; _worldFailureLogged = false;
    }
    private bool Admit()
    {
        if (!_isCurrent()) return false; // In particular, an off-thread caller may not mutate runtime cursors.
        if (_boundGeneration != _generation()) Reset();
        return true;
    }
    internal void BeginWorldScan(Func<IEnumerable<Settlement>> source, string reason)
    {
        if (!Admit()) return;
        CloseWorld();
        _worldSource = source; _worldReason = reason; _worldFailureLogged = false;
    }
    internal void Enqueue(Settlement settlement, string reason)
    {
        if (!Admit()) return;
        // A replacement is resolved when this existing job runs. Do not retain
        // a new historical inventory every time the same settlement admits work.
        if (settlement == null || _bySettlement.ContainsKey(settlement)) return;
        try
        {
            ItemRoster roster = settlement.ItemRoster;
            if (roster == null || _queued.ContainsKey(roster) || IsClean(roster)) return;
            var work = new Work { Settlement = settlement, Roster = roster, Label = settlement.StringId, Reason = reason };
            _queued.Add(roster, work); _bySettlement.Add(settlement, work); _pending.Enqueue(work);
        }
        catch (Exception error) { ReportFailure("settlement", reason, error); }
    }
    private bool IsClean(ItemRoster roster) => _scans.TryGetValue(roster, out var scan)
        && scan.Complete && !scan.HasTradableGeneratedItems && scan.Version == roster.VersionNo && scan.Rules == _rules();

    // Trade boundaries finish only the selected roster. A partial background scan is
    // restarted, since an allowed item's mutable flags need a fresh admission decision.
    internal int CleanNow(ItemRoster roster, string label, string reason)
    {
        if (roster == null || !Admit()) return 0;
        Scan scan = null;
        int removed = 0, slots = 0;
        try
        {
            if (IsClean(roster)) return 0;
            scan = _scans.GetValue(roster, CreateScan);
            scan.Start(roster.VersionNo, _rules());
            long steps = 0, limit = 2L * roster.Count + 1;
            while (!scan.Complete && _isCurrent() && _boundGeneration == _generation())
            {
                // Reentrant producers must not trap trade opening in an endless
                // restart. Leave an invalid stamp/evidence; never declare partial work clean.
                if (++steps > limit) throw new InvalidOperationException("market_roster_changed_repeatedly_during_cleanup");
                removed += Step(roster, scan, ref slots);
            }
        }
        catch (Exception error) { if (scan != null) scan.Complete = false; ReportFailure(label, reason, error); }
        if (removed > 0) ReportRemoved(label, reason, removed, slots);
        return removed;
    }
    private int Step(ItemRoster roster, Scan scan, ref int removedSlots)
    {
        long rules = _rules();
        if (scan.Version != roster.VersionNo || scan.Rules != rules)
            scan.Start(roster.VersionNo, rules);
        if (scan.Cursor >= roster.Count) { scan.Complete = true; return 0; }
        ItemRosterElement element = roster.GetElementCopyAtIndex(scan.Cursor);
        bool generated = element.Amount > 0 && _isGenerated(element.EquipmentElement.Item);
        int removed = 0;
        if (generated && _isBlocked(element.EquipmentElement.Item))
        {
            int count = roster.Count;
            roster.AddToCounts(element.EquipmentElement, -element.Amount);
            if (roster.Count == count && roster.VersionNo == scan.Version)
                throw new InvalidOperationException("market_roster_remove_made_no_progress");
            if (roster.Count < count)
            {
                removed = element.Amount; removedSlots++;
                // AddToCounts moves the last slot into the removed slot. Visit it next.
            }
            else { scan.Cursor++; scan.HasTradableGeneratedItems = true; }
            int version = roster.VersionNo;
            if (version != unchecked(scan.Version + 1) || roster.Count != count - 1)
                scan.Start(version, _rules()); // A reentrant native update made additional changes.
            else scan.Version = version;
        }
        else
        {
            scan.Cursor++;
            scan.HasTradableGeneratedItems |= generated;
        }
        if (scan.Cursor >= roster.Count && scan.Rules == _rules()) scan.Complete = true;
        return removed;
    }

    // Metadata admissions, empty jobs and stale jobs also consume the unit cap.
    // Individual native roster operations remain atomic; elapsed time is a soft limit.
    internal void Tick(int maxUnits = 128, double budgetMs = 2.0)
    {
        if (!HasWork || !Admit()) return;
        long started = Stopwatch.GetTimestamp();
        for (int units = 0; units < maxUnits && HasWork
            && (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency < budgetMs; units++)
        {
            if (!_isCurrent()) return;
            if (_boundGeneration != _generation()) { Reset(); return; }
            if (HasWorldScan && (_admitWorld || _pending.Count == 0))
            { AdmitWorldMarket(); _admitWorld = false; continue; }
            _admitWorld = true;
            Work work = _pending.Dequeue();
            Scan scan = null;
            int removed = 0, slots = 0;
            try
            {
                if (!ReferenceEquals(work.Roster, work.Settlement?.ItemRoster))
                {
                    Retire(work); Enqueue(work.Settlement, work.Reason); continue;
                }
                if (IsClean(work.Roster)) { Retire(work); continue; }
                scan = _scans.GetValue(work.Roster, CreateScan);
                if (scan.Complete) scan.Start(work.Roster.VersionNo, _rules());
                removed = Step(work.Roster, scan, ref slots);
            }
            catch (Exception error)
            {
                if (scan != null) scan.Complete = false;
                Retire(work);
                ReportFailure(work.Label, work.Reason, error);
                continue; // The next daily/trade admission retries, never stamp a failed scan clean.
            }
            if (removed > 0) ReportRemoved(work.Label, work.Reason, removed, slots);
            if (scan.Complete) Retire(work);
            else _pending.Enqueue(work);
        }
    }
    private void AdmitWorldMarket()
    {
        try
        {
            if (_world == null) _world = (_worldSource()?.GetEnumerator()) ?? ((IEnumerable<Settlement>)EmptyWorld).GetEnumerator();
            if (_world.MoveNext()) { Enqueue(_world.Current, _worldReason); return; }
            CloseWorld(); _worldSource = null;
        }
        catch (Exception error)
        {
            CloseWorld();
            if (!_worldFailureLogged) { _worldFailureLogged = true; ReportFailure("world", _worldReason, error); }
            // A changed settlement collection is retried with a fresh enumerator.
            // Already admitted markets coalesce; previously completed clean markets are cached.
        }
    }
    private void Retire(Work work)
    { _queued.Remove(work.Roster); _bySettlement.Remove(work.Settlement); }
    private void CloseWorld()
    {
        var iterator = _world; _world = null;
        try { iterator?.Dispose(); }
        catch (Exception error) { ReportFailure("world", _worldReason, error); }
    }
    private void ReportRemoved(string label, string reason, int count, int slots)
    {
        try { _removed?.Invoke(label, reason, count, slots); }
        catch (Exception) { } // Diagnostic failure cannot strand an already modified inventory/queued scan.
    }
    private void ReportFailure(string label, string reason, Exception error)
    {
        try { _failure?.Invoke(label, reason, error); }
        catch (Exception) { } // Cleanup has already retained its invalid stamp; diagnostics are best effort.
    }
    private static readonly Settlement[] EmptyWorld = new Settlement[0];
}
