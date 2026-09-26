using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // Metadata grants account for individual lines, trigger binds and sort units.
    // A single text operation can still be atomic. The Campaign cycle shares caps
    // and deadline; standalone finite calls retain their own allowance.
    private DailyMemorySealState _dailyMemorySealState;
    // One-call completion receipt, not a feature switch: maintenance preserves
    // the old HasPast=true path even when cleanup consumes its last raw job.
    private bool _dailyMemorySealCompletedPass;

    private enum DailyMemorySealPhase { Owners, Probe, DailyIndex, MajorIndex, Drafts, DailyCleanup, MajorCleanup }

    private sealed class DailyMemorySealOwnerBinding
    {
        internal List<DailyMemoryDraft> Source;
        internal int Count;
        internal List<DailyMemoryDraft>.Enumerator Probe;
        internal void Bind(List<DailyMemoryDraft> source)
        { Source = source; Count = source?.Count ?? 0; Probe = source == null ? default(List<DailyMemoryDraft>.Enumerator) : source.GetEnumerator(); }
        internal bool Current(List<DailyMemoryDraft> source)
        {
            if (!ReferenceEquals(Source, source) || Count != (source?.Count ?? 0)) return false;
            if (source == null) return true;
            try { Probe.MoveNext(); return true; } catch (InvalidOperationException) { return false; }
        }
    }

    private sealed class DailyMemorySealIndex<T> where T : class
    {
        internal List<T> Source;
        internal int Count, Cursor;
        internal List<T>.Enumerator Probe;
        internal readonly Dictionary<string, List<T>> Entries = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);
        internal void Bind(List<T> source)
        { Source = source; Count = source?.Count ?? 0; Probe = source == null ? default(List<T>.Enumerator) : source.GetEnumerator(); }
        internal bool Current(List<T> source)
        {
            if (!ReferenceEquals(Source, source) || Count != (source?.Count ?? 0)) return false;
            if (source == null) return true;
            try { Probe.MoveNext(); return true; } catch (InvalidOperationException) { return false; }
        }
        internal void Add(string key, T job)
        {
            if (!Entries.TryGetValue(key, out var matches)) Entries[key] = matches = new List<T>();
            matches.Add(job);
        }
    }

    private sealed class DailyMemorySealQueueEntry<T> where T : class
    {
        internal T Value;
        internal T Frozen;
    }

    private sealed class DailyMemorySealQueueTail<T> where T : class
    {
        internal List<T> Source;
        internal int Count, Cursor;
        internal List<T>.Enumerator Probe;
        internal bool Deferred;
        internal readonly List<DailyMemorySealQueueEntry<T>> Entries = new List<DailyMemorySealQueueEntry<T>>();
        private CooperativeMemoryQueueSort<T> _sort;
        internal bool Step(Func<List<T>> readCurrent, Action<List<T>> publish, Func<T, bool> pending,
            Func<T, T> copy, Func<T, T, bool> same,
            Func<IEnumerable<T>, List<T>> normalize, Func<T, int> day, Func<T, string> name,
            MemoryMaintenanceWorkBudget budget)
        {
            Deferred = false;
            var current = readCurrent();
            if (Source == null)
            { Source = current; Count = current.Count; Cursor = 0; Probe = current.GetEnumerator(); }
            if (!Current(current) || (_sort != null && !_sort.IsCultureCurrent)) { Restart(); return false; }
            while (Cursor < Count)
            {
                T job = Source[Cursor];
                if (!budget.Take(job != null)) return false;
                int index = Cursor++;
                T survivor = job != null && pending(job) ? job : null;
                if (survivor == null) Source[index] = null;
                else Entries.Add(new DailyMemorySealQueueEntry<T> { Value = survivor, Frozen = copy(survivor) });
                // Nulling is immediate, not an old cross-tick deletion decision.
                Probe = Source.GetEnumerator();
            }
            if (_sort == null)
            {
                if (!budget.Take(false) || !BindingsCurrent(same)) return false;
                // Preserve the original in-place normalize/filter/dedupe order and
                // first surviving reference. Only this metadata mutation is visible
                // before sorting finishes; no summary is started by this tail.
                var prepared = normalize(Source);
                foreach (var entry in Entries) entry.Frozen = copy(entry.Value);
                _sort = new CooperativeMemoryQueueSort<T>(prepared, day, name);
            }
            if (!_sort.Step(budget) || !budget.Take(false)) return false;
            // Full scalar binding remains atomic O(N), but only at preparation and
            // publication, not at every sort slice. Re-read the actual owner's list.
            if (!Current(readCurrent()) || !_sort.IsCultureCurrent) { Restart(); return false; }
            if (!BindingsCurrent(same)) return false;
            publish(_sort.Result);
            return true;
        }
        private bool Current(List<T> current)
        {
            if (!ReferenceEquals(Source, current) || current == null || Count != current.Count) return false;
            try { Probe.MoveNext(); return true; } catch (InvalidOperationException) { return false; }
        }
        private bool BindingsCurrent(Func<T, T, bool> same)
        {
            foreach (var entry in Entries)
                if (!same(entry.Value, entry.Frozen)) { Restart(); return false; }
            return true;
        }
        private void Restart()
        { Source = null; Count = Cursor = 0; Entries.Clear(); _sort = null; Deferred = true; }
    }

    // Draft identity remains one expensive grant. Lines and trigger binds consume
    // shared metadata. Lines publish after completion; triggers publish after their
    // bind/sanitize phase. Neither path is a transaction for object field changes.
    private sealed class DailyMemoryDraftNormalization
    {
        private struct Binding
        {
            internal DailyMemoryDraft Source;
            internal string HeroId;
            internal int Day;
            internal bool EmptyWinner, Included;
        }
        private readonly List<DailyMemoryDraft> _source;
        private readonly int _count;
        private List<DailyMemoryDraft>.Enumerator _probe;
        private int _cursor;
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Binding> _bindings = new List<Binding>();
        private readonly List<DailyMemoryDraft> _result = new List<DailyMemoryDraft>();
        private CooperativeMemoryQueueSort<DailyMemoryDraft> _sort;
        private DailyMemoryDraftEntryNormalization _entry;
        private int _entrySeenCount;
        internal bool Invalidated { get; private set; }

        internal DailyMemoryDraftNormalization(List<DailyMemoryDraft> source)
        { _source = source; _count = source.Count; _probe = source.GetEnumerator(); }

        internal bool Step(Func<List<DailyMemoryDraft>> readCurrent, MemoryMaintenanceWorkBudget budget, out List<DailyMemoryDraft> result)
        {
            result = null;
            var current = readCurrent();
            if (!Current(current)) { Invalidated = true; return false; }
            while (_cursor < _count)
            {
                var source = _source[_cursor];
                if (_entry == null)
                {
                    if (!budget.Take(source != null)) return false;
                    _entrySeenCount = _seen.Count;
                    _entry = new DailyMemoryDraftEntryNormalization(source, _seen);
                }
                if (!_entry.Step(budget))
                {
                    if (_entry.Invalidated) Invalidated = true;
                    return false;
                }
                var normalized = _entry.Result;
                if (normalized != null) _result.Add(normalized);
                if (source != null) _bindings.Add(new Binding
                {
                    Source = source, HeroId = NormalizeMemoryHeroId(source.HeroId), Day = source.GameDayIndex,
                    EmptyWinner = _seen.Count > _entrySeenCount && normalized == null, Included = normalized != null
                });
                _cursor++;
                _entry = null;
            }
            if (_sort == null)
            {
                if (!budget.Take(false)) return false;
                if (!BindingsCurrent()) { Invalidated = true; return false; }
                // Original draft ordering uses day ONLY. A constant secondary key
                // reuses the real stable sort without introducing HeroName ordering.
                _sort = new CooperativeMemoryQueueSort<DailyMemoryDraft>(_result, x => x.GameDayIndex, x => (string)null);
            }
            if (!_sort.Step(budget) || !budget.Take(false)) return false;
            if (!Current(readCurrent()) || !BindingsCurrent()) { Invalidated = true; return false; }
            result = _sort.Result;
            return true;
        }
        private bool Current(List<DailyMemoryDraft> current)
        {
            if (!ReferenceEquals(current, _source) || current == null || current.Count != _count) return false;
            try { _probe.MoveNext(); return true; } catch (InvalidOperationException) { return false; }
        }
        private bool BindingsCurrent()
        {
            // Metadata-only atomic tail, not a hard 128-record bound. No stale
            // duplicate/empty decision may delete a retargeted or newly filled row.
            foreach (var entry in _bindings)
            {
                if (NormalizeMemoryHeroId(entry.Source.HeroId) != entry.HeroId || entry.Source.GameDayIndex != entry.Day) return false;
                int lines = entry.Source.Lines?.Count ?? 0;
                if (entry.EmptyWinner && lines > 0) return false;
                if (entry.Included && lines == 0) return false;
            }
            return true;
        }
    }

    private enum DailyMemoryDraftEntryPhase { BindTriggers, SanitizeTriggers, SanitizeLines, Finalize, Done }

    // Cooperative inner sanitizer for one draft. Identity already paid the expensive
    // grant. Trigger list sanitizer stays one metadata-backed atomic call.
    private sealed class DailyMemoryDraftEntryNormalization
    {
        private readonly DailyMemoryDraft _source;
        private readonly DailyMemoryDraft _draft;
        private readonly string _heroId;
        private readonly int _day;
        private readonly List<WeeklyMemoryMaterialTrigger> _triggerSource;
        private readonly int _boundTriggerCount;
        private readonly List<DailyMemoryLine> _lineSource;
        private readonly int _boundLineCount;
        private List<DailyMemoryLine>.Enumerator _lineStructureProbe;
        private List<WeeklyMemoryMaterialTrigger>.Enumerator _triggerStructureProbe;
        private readonly List<DailyMemoryLine> _lineResult = new List<DailyMemoryLine>();
        private DailyMemoryDraftEntryPhase _phase;
        private int _triggerIndex;
        private int _lineIndex;
        private bool _hasLlm;
        private bool _triggersPublished;
        private List<WeeklyMemoryMaterialTrigger> _publishedTriggers;
        internal bool Invalidated { get; private set; }
        internal DailyMemoryDraft Result { get; private set; }

        internal DailyMemoryDraftEntryNormalization(DailyMemoryDraft source, HashSet<string> seen)
        {
            _source = source;
            _draft = TWParallel.IsMainThread() ? source : CloneMemorySummarySource(source);
            _heroId = null;
            _day = -1;
            _triggerSource = null;
            _boundTriggerCount = 0;
            _lineSource = null;
            _boundLineCount = 0;
            if (_draft == null)
            {
                _phase = DailyMemoryDraftEntryPhase.Done;
                return;
            }
            string text = NormalizeMemoryHeroId(_draft.HeroId);
            if (string.IsNullOrWhiteSpace(text) || _draft.GameDayIndex < 0)
            {
                _phase = DailyMemoryDraftEntryPhase.Done;
                return;
            }
            if (!seen.Add(text + "|" + _draft.GameDayIndex))
            {
                _phase = DailyMemoryDraftEntryPhase.Done;
                return;
            }
            _draft.HeroId = text;
            _draft.HeroName = (_draft.HeroName ?? "").Trim();
            _draft.GameDate = (_draft.GameDate ?? "").Trim();
            _draft.LastSummaryError = (_draft.LastSummaryError ?? "").Trim();
            _heroId = text;
            _day = _draft.GameDayIndex;
            _triggerSource = _draft.WeeklyMaterialTriggers;
            _boundTriggerCount = _triggerSource?.Count ?? 0;
            _lineSource = _draft.Lines;
            _boundLineCount = _lineSource?.Count ?? 0;
            _lineStructureProbe = _lineSource == null ? default(List<DailyMemoryLine>.Enumerator) : _lineSource.GetEnumerator();
            _triggerStructureProbe = _triggerSource == null ? default(List<WeeklyMemoryMaterialTrigger>.Enumerator) : _triggerSource.GetEnumerator();
            _hasLlm = _draft.HasLlmDialogue;
            _phase = DailyMemoryDraftEntryPhase.BindTriggers;
        }

        internal bool Step(MemoryMaintenanceWorkBudget budget)
        {
            if (Invalidated) return false;
            if (_phase == DailyMemoryDraftEntryPhase.Done) return true;
            if (!InnerCurrent()) return false;
            if (_phase == DailyMemoryDraftEntryPhase.BindTriggers)
            {
                if (_triggerSource != null)
                {
                    while (_triggerIndex < _boundTriggerCount)
                    {
                        if (!budget.Take(false)) return false;
                        BindDailyMemoryDraftWeeklyTrigger(_triggerSource[_triggerIndex++], _heroId, _day, _draft.GameDate);
                    }
                }
                _phase = DailyMemoryDraftEntryPhase.SanitizeTriggers;
            }
            if (_phase == DailyMemoryDraftEntryPhase.SanitizeTriggers)
            {
                if (!budget.Take(false)) return false;
                if (!InnerCurrent()) return false;
                _draft.WeeklyMaterialTriggers = SanitizeWeeklyMemoryMaterialTriggers(_draft.WeeklyMaterialTriggers);
                _triggersPublished = true;
                _publishedTriggers = _draft.WeeklyMaterialTriggers;
                _phase = DailyMemoryDraftEntryPhase.SanitizeLines;
            }
            if (_phase == DailyMemoryDraftEntryPhase.SanitizeLines)
            {
                if (_lineSource != null)
                {
                    while (_lineIndex < _boundLineCount)
                    {
                        if (!budget.Take(false)) return false;
                        if (!InnerCurrent()) return false;
                        var line = SanitizeDailyMemoryDraftLine(_lineSource[_lineIndex++], _draft);
                        if (line == null) continue;
                        _lineResult.Add(line);
                        _hasLlm = _hasLlm || (line.IsLlmDialogue && !line.IsAfef);
                    }
                }
                _phase = DailyMemoryDraftEntryPhase.Finalize;
            }
            if (_phase == DailyMemoryDraftEntryPhase.Finalize)
            {
                if (!budget.Take(false)) return false;
                if (!InnerCurrent()) return false;
                _draft.Lines = _lineResult;
                _draft.HasLlmDialogue = _hasLlm;
                Result = _lineResult.Count > 0 ? _draft : null;
                _phase = DailyMemoryDraftEntryPhase.Done;
            }
            return true;
        }

        private bool InnerCurrent()
        {
            if (_draft == null) return true;
            if (NormalizeMemoryHeroId(_source.HeroId) != _heroId || _source.GameDayIndex != _day)
            {
                Invalidated = true;
                return false;
            }
            if (!_triggersPublished)
            {
                if (!ReferenceEquals(_source.WeeklyMaterialTriggers, _triggerSource) || (_source.WeeklyMaterialTriggers?.Count ?? 0) != _boundTriggerCount)
                {
                    Invalidated = true;
                    return false;
                }
            }
            else if (!ReferenceEquals(_draft.WeeklyMaterialTriggers, _publishedTriggers))
            {
                Invalidated = true;
                return false;
            }
            if (!ReferenceEquals(_source.Lines, _lineSource) || (_source.Lines?.Count ?? 0) != _boundLineCount)
            {
                Invalidated = true;
                return false;
            }
            // List enumerators check structural versions even after exhaustion:
            // same-count replacement/removal/reordering must invalidate a saved cursor.
            // O(1) checks, no reflection/full copy; object field edits are not covered.
            try
            {
                if (_lineSource != null) _lineStructureProbe.MoveNext();
                // Sanitization legitimately replaces the trigger list. Once published,
                // it is no longer an input cursor and must not check that obsolete list.
                if (!_triggersPublished && _triggerSource != null) _triggerStructureProbe.MoveNext();
            }
            catch (InvalidOperationException)
            {
                Invalidated = true;
                return false;
            }
            return true;
        }
    }

    private sealed class DailyMemorySealState
    {
        internal DailyMemorySealPhase Phase;
        internal bool RequirePendingProbe;
        internal Dictionary<string, List<DailyMemoryDraft>> Owners;
        internal int OwnerCount;
        internal Dictionary<string, List<DailyMemoryDraft>>.Enumerator OwnerEnumerator;
        internal Dictionary<string, List<DailyMemoryDraft>>.Enumerator OwnerProbe;
        internal DailyMemorySealOwnerBinding ActiveOwner;
        internal DailyMemoryDraftNormalization Normalization;
        internal readonly Dictionary<string, DailyMemorySealOwnerBinding> CompletedOwners = new Dictionary<string, DailyMemorySealOwnerBinding>(StringComparer.Ordinal);
        internal DailyMemorySealIndex<MemorySummaryJob> DailyIndex;
        internal DailyMemorySealIndex<MajorActionSummaryJob> MajorIndex;
        internal readonly DailyMemorySealQueueTail<MemorySummaryJob> DailyTail = new DailyMemorySealQueueTail<MemorySummaryJob>();
        internal readonly DailyMemorySealQueueTail<MajorActionSummaryJob> MajorTail = new DailyMemorySealQueueTail<MajorActionSummaryJob>();
        internal void BindOwners(Dictionary<string, List<DailyMemoryDraft>> owners)
        { Owners = owners; OwnerCount = owners?.Count ?? 0; OwnerProbe = owners == null ? default(Dictionary<string, List<DailyMemoryDraft>>.Enumerator) : owners.GetEnumerator(); }
        internal bool OwnersCurrent(Dictionary<string, List<DailyMemoryDraft>> owners)
        {
            if (!ReferenceEquals(Owners, owners) || OwnerCount != (owners?.Count ?? 0)) return false;
            if (owners == null) return true;
            try { OwnerProbe.MoveNext(); return true; } catch (InvalidOperationException) { return false; }
        }
    }

    private void BeginDailyMemorySeal(bool requirePendingProbe)
    {
        _dailyMemorySealCompletedPass = false;
        _dailyMemoryDraftSealOwnerKeys = new List<string>();
        _dailyMemoryDraftSealOwnerIndex = 0;
        _dailyMemoryDraftSealDraftIndex = -1;
        _dailyMemoryDraftSealTargetDay = (int)CampaignTime.Now.ToDays;
        _dailyMemoryDraftSealQueued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _dailyMemoryDraftSealQueuedMajor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _memorySummaryQueue = _memorySummaryQueue ?? new List<MemorySummaryJob>();
        _npcMajorActionSummaryQueue = _npcMajorActionSummaryQueue ?? new List<MajorActionSummaryJob>();
        _dailyMemorySealState = new DailyMemorySealState { RequirePendingProbe = requirePendingProbe };
        _dailyMemorySealState.BindOwners(_dailyMemoryDrafts);
        _dailyMemorySealState.OwnerEnumerator = _dailyMemoryDrafts.GetEnumerator();
    }

    private static string DailyMemorySealJobKey(MemorySummaryJob job)
        => NormalizeMemoryHeroId(job.HeroId) + "|" + job.GameDayIndex;

    private bool PrepareDailyMemorySealIndex<T>(DailyMemorySealIndex<T> index, List<T> current,
        HashSet<string> keys, Func<T, string> key, MemoryMaintenanceWorkBudget budget) where T : class
    {
        if (!index.Current(current)) return false;
        while (index.Cursor < index.Count)
        {
            if (!budget.Take(false)) return false;
            T job = current[index.Cursor++];
            if (job == null) continue;
            string value = key(job);
            keys.Add(value); index.Add(value, job);
        }
        return true;
    }

    private void RestartDailyMemorySealIndexes(DailyMemorySealState state)
    {
        _dailyMemoryDraftSealQueued.Clear();
        _dailyMemoryDraftSealQueuedMajor.Clear();
        state.DailyIndex = new DailyMemorySealIndex<MemorySummaryJob>();
        state.DailyIndex.Bind(_memorySummaryQueue);
        state.MajorIndex = new DailyMemorySealIndex<MajorActionSummaryJob>();
        state.MajorIndex.Bind(_npcMajorActionSummaryQueue);
        state.Phase = DailyMemorySealPhase.DailyIndex;
    }

    private static bool SameDailyMemorySealJob(MemorySummaryJob a, MemorySummaryJob b)
        => a.HeroId == b.HeroId && a.HeroName == b.HeroName && a.GameDayIndex == b.GameDayIndex
            && a.GameDate == b.GameDate && a.RetryCount == b.RetryCount && a.LastError == b.LastError;
    private static bool SameDailyMemorySealMajorJob(MajorActionSummaryJob a, MajorActionSummaryJob b)
        => a.HeroId == b.HeroId && a.HeroName == b.HeroName && a.TriggerGameDayIndex == b.TriggerGameDayIndex
            && a.TriggerGameDate == b.TriggerGameDate && a.RetryCount == b.RetryCount && a.LastError == b.LastError;

    private bool ContinueDailyMemorySeal(long startTimestamp, double budgetMs, bool requirePendingProbe)
    {
        var budget = _campaignMemoryMaintenanceBudget;
        if (budget == null || budget.Start != startTimestamp || budget.Milliseconds != budgetMs)
            budget = new MemoryMaintenanceWorkBudget(startTimestamp, budgetMs,
                DailyMemorySealMetadataPerSlice, DailyMaintenanceMaxJobsPerTick);
        if (_dailyMemorySealState == null) BeginDailyMemorySeal(requirePendingProbe);
        var state = _dailyMemorySealState;
        // An explicit synchronous caller must perform sealing, even if maintenance
        // had only started its non-mutating pending probe.
        if (!requirePendingProbe) state.RequirePendingProbe = false;
        bool retried = false;
        while (true)
        {
            if (!state.OwnersCurrent(_dailyMemoryDrafts))
            {
                BeginDailyMemorySeal(state.RequirePendingProbe);
                state = _dailyMemorySealState;
                if (!budget.Unbounded || retried) return false;
                retried = true;
            }
            switch (state.Phase)
            {
                case DailyMemorySealPhase.Owners:
                    while (true)
                    {
                        if (!budget.Take(false)) return false;
                        bool moved;
                        try { moved = state.OwnerEnumerator.MoveNext(); }
                        catch (InvalidOperationException)
                        {
                            BeginDailyMemorySeal(state.RequirePendingProbe);
                            if (!budget.Unbounded || retried) return false;
                            state = _dailyMemorySealState; retried = true;
                            continue;
                        }
                        if (!moved) break;
                        _dailyMemoryDraftSealOwnerKeys.Add(state.OwnerEnumerator.Current.Key);
                    }
                    state.Phase = DailyMemorySealPhase.Probe;
                    continue;
                case DailyMemorySealPhase.Probe:
                    if (state.RequirePendingProbe && _dailyMemoryDraftSealTargetDay > 0)
                    {
                        if (!RunDailyMemorySealProbe(state, budget, out bool found)) return false;
                        if (!found) { ResetDailyMemoryDraftSealSliceState(); return true; }
                    }
                    else if (state.RequirePendingProbe)
                    { ResetDailyMemoryDraftSealSliceState(); return true; }
                    state.RequirePendingProbe = false;
                    _dailyMemoryDraftSealOwnerIndex = 0;
                    _dailyMemoryDraftSealDraftIndex = -1;
                    state.ActiveOwner = null;
                    state.CompletedOwners.Clear();
                    RestartDailyMemorySealIndexes(state);
                    continue;
                case DailyMemorySealPhase.DailyIndex:
                case DailyMemorySealPhase.MajorIndex:
                    if (!state.DailyIndex.Current(_memorySummaryQueue) || !state.MajorIndex.Current(_npcMajorActionSummaryQueue))
                    {
                        RestartDailyMemorySealIndexes(state);
                        if (!budget.Unbounded || retried) return false;
                        retried = true;
                    }
                    if (state.Phase == DailyMemorySealPhase.DailyIndex)
                    {
                        if (!PrepareDailyMemorySealIndex(state.DailyIndex, _memorySummaryQueue, _dailyMemoryDraftSealQueued, DailyMemorySealJobKey, budget)) return false;
                        state.Phase = DailyMemorySealPhase.MajorIndex;
                    }
                    if (!PrepareDailyMemorySealIndex(state.MajorIndex, _npcMajorActionSummaryQueue, _dailyMemoryDraftSealQueuedMajor,
                        x => NormalizeMemoryHeroId(x.HeroId), budget)) return false;
                    state.Phase = DailyMemorySealPhase.Drafts;
                    continue;
                case DailyMemorySealPhase.Drafts:
                    if (!state.DailyIndex.Current(_memorySummaryQueue) || !state.MajorIndex.Current(_npcMajorActionSummaryQueue))
                    {
                        RestartDailyMemorySealIndexes(state);
                        if (!budget.Unbounded || retried) return false;
                        retried = true; continue;
                    }
                    if (!RunDailyMemorySealDrafts(state, budget))
                    {
                        if (state.Phase != DailyMemorySealPhase.Drafts && budget.Unbounded) continue;
                        return false;
                    }
                    // Dictionary value updates do not invalidate every CLR's
                    // enumerator. This atomic O(owner count) binding check is a
                    // remaining metadata tail, admitted against the same deadline.
                    if (!budget.Take(false)) return false;
                    if (!DailyMemorySealCompletedOwnersCurrent(state))
                    {
                        BeginDailyMemorySeal(false);
                        if (!budget.Unbounded || retried) return false;
                        state = _dailyMemorySealState; retried = true; continue;
                    }
                    state.Phase = DailyMemorySealPhase.DailyCleanup;
                    continue;
                case DailyMemorySealPhase.DailyCleanup:
                    if (!state.DailyTail.Step(() => _memorySummaryQueue, x => _memorySummaryQueue = x,
                        HasMemorySummaryJobStillPending, x => x.CopyForSummary(), SameDailyMemorySealJob,
                        NormalizeMemorySummaryQueue, x => x.GameDayIndex, x => x.HeroName, budget))
                    {
                        if (budget.Unbounded && state.DailyTail.Deferred && !retried) { retried = true; continue; }
                        return false;
                    }
                    state.Phase = DailyMemorySealPhase.MajorCleanup;
                    continue;
                case DailyMemorySealPhase.MajorCleanup:
                    if (!state.MajorTail.Step(() => _npcMajorActionSummaryQueue, x => _npcMajorActionSummaryQueue = x,
                        HasMajorActionSummaryJobStillPending, x => x.CopyForSummary(), SameDailyMemorySealMajorJob,
                        NormalizeMajorActionSummaryQueue, x => x.TriggerGameDayIndex, x => x.HeroName, budget))
                    {
                        if (budget.Unbounded && state.MajorTail.Deferred && !retried) { retried = true; continue; }
                        return false;
                    }
                    ResetDailyMemoryDraftSealSliceState();
                    _dailyMemorySealCompletedPass = true;
                    return true;
            }
        }
    }

    private bool RunDailyMemorySealProbe(DailyMemorySealState state, MemoryMaintenanceWorkBudget budget, out bool found)
    {
        found = false;
        while (_dailyMemoryDraftSealOwnerIndex < _dailyMemoryDraftSealOwnerKeys.Count)
        {
            if (!budget.Take(false)) return false;
            string ownerKey = _dailyMemoryDraftSealOwnerKeys[_dailyMemoryDraftSealOwnerIndex];
            _dailyMemoryDrafts.TryGetValue(ownerKey, out var list);
            string ownerMemoryId = NormalizeMemoryHeroId(ownerKey);
            if (list == null || !IsMemoryEntityEligibleForCompressedMemory(ownerMemoryId))
            { CompleteDailyMemorySealOwner(state, ownerKey, list); continue; }
            BindDailyMemorySealActiveOwner(state, list);
            while (_dailyMemoryDraftSealDraftIndex >= 0)
            {
                if (!budget.Take(true)) return false;
                DailyMemoryDraft draft = list[_dailyMemoryDraftSealDraftIndex--];
                if (draft != null && string.Equals(NormalizeMemoryHeroId(draft.HeroId), ownerMemoryId, StringComparison.OrdinalIgnoreCase)
                    && draft.SummaryRetryCount < 3 && draft.GameDayIndex < _dailyMemoryDraftSealTargetDay
                    && draft.HasLlmDialogue && CountDailyMemorySummarySourceChars(draft) > 0
                    && !HasCompressedMemoryBlock(ownerMemoryId, draft.GameDayIndex))
                { found = true; return true; }
            }
            CompleteDailyMemorySealOwner(state, ownerKey, list);
        }
        // Keep this current-value validation atomic; unlike the key collector,
        // it is not claimed to have a 128-owner hard cap.
        if (!budget.Take(false)) return false;
        if (!DailyMemorySealCompletedOwnersCurrent(state))
        { BeginDailyMemorySeal(true); return false; }
        return true;
    }

    private static bool DailyMemorySealHasCurrentJob(DailyMemorySealIndex<MemorySummaryJob> index, string key)
    {
        // List probes detect structural writes, not edits to a queued object's key.
        // A cached hit is only a hint: recheck the still-member reference's key.
        return index.Entries.TryGetValue(key, out var jobs)
            && jobs.Any(x => x != null && string.Equals(DailyMemorySealJobKey(x), key, StringComparison.OrdinalIgnoreCase));
    }

    private void BindDailyMemorySealActiveOwner(DailyMemorySealState state, List<DailyMemoryDraft> list)
    {
        if (state.ActiveOwner != null && state.ActiveOwner.Current(list)) return;
        state.Normalization = null;
        state.ActiveOwner = new DailyMemorySealOwnerBinding();
        state.ActiveOwner.Bind(list);
        _dailyMemoryDraftSealDraftIndex = list.Count - 1;
    }

    private void CompleteDailyMemorySealOwner(DailyMemorySealState state, string ownerKey, List<DailyMemoryDraft> list)
    {
        var completed = new DailyMemorySealOwnerBinding(); completed.Bind(list);
        state.CompletedOwners[ownerKey] = completed;
        _dailyMemoryDraftSealOwnerIndex++;
        _dailyMemoryDraftSealDraftIndex = -1;
        state.ActiveOwner = null;
        state.Normalization = null;
    }

    private bool DailyMemorySealCompletedOwnersCurrent(DailyMemorySealState state)
    {
        foreach (var owner in state.CompletedOwners)
        {
            _dailyMemoryDrafts.TryGetValue(owner.Key, out var current);
            if (!owner.Value.Current(current)) return false;
        }
        return true;
    }

    private bool RunDailyMemorySealDrafts(DailyMemorySealState state, MemoryMaintenanceWorkBudget budget)
    {
        while (_dailyMemoryDraftSealOwnerIndex < _dailyMemoryDraftSealOwnerKeys.Count)
        {
            if (!budget.Take(false)) return false;
            string ownerKey = _dailyMemoryDraftSealOwnerKeys[_dailyMemoryDraftSealOwnerIndex];
            if (!_dailyMemoryDrafts.TryGetValue(ownerKey, out var list) || list == null)
            { CompleteDailyMemorySealOwner(state, ownerKey, list); continue; }
            string ownerMemoryId = NormalizeMemoryHeroId(ownerKey);
            if (!IsMemoryEntityEligibleForCompressedMemory(ownerMemoryId))
            {
                if (!budget.Take(true)) return false;
                // Preserve raw drafts / weekly trigger facts; keep the original
                // atomic invalid-owner cancellation responsibility.
                CancelUnavailableHeroCompressionWorkById(ownerMemoryId, "seal_past_daily_drafts");
                CompleteDailyMemorySealOwner(state, ownerKey, list);
                RestartDailyMemorySealIndexes(state);
                return false;
            }
            BindDailyMemorySealActiveOwner(state, list);
            while (_dailyMemoryDraftSealDraftIndex >= 0)
            {
                if (!budget.Take(true)) return false;
                int draftIndex = _dailyMemoryDraftSealDraftIndex--;
                DailyMemoryDraft draft = list[draftIndex];
                if (draft == null || draft.GameDayIndex >= _dailyMemoryDraftSealTargetDay) continue;
                if (!string.Equals(NormalizeMemoryHeroId(draft.HeroId), ownerMemoryId, StringComparison.OrdinalIgnoreCase))
                {
                    draft.QueuedForSummary = false;
                    Logger.Log("CompressedMemory", "skipped mismatched daily memory draft owner=" + ownerMemoryId + " draftHero=" + NormalizeMemoryHeroId(draft.HeroId) + " day=" + draft.GameDayIndex);
                    continue;
                }
                draft.HeroId = ownerMemoryId;
                bool hasSummarySource = CountDailyMemorySummarySourceChars(draft) > 0;
                bool hasAfefLines = HasDailyMemoryDraftAfefLines(draft);
                if (!draft.HasLlmDialogue || !hasSummarySource)
                {
                    if (!hasAfefLines) { list.RemoveAt(draftIndex); state.ActiveOwner.Bind(list); }
                    continue;
                }
                // The legacy major helper rechecks a live existing row first.
                // Never let a stale cached hero suppress an absent live job.
                if (_dailyMemoryDraftSealQueuedMajor.Contains(ownerMemoryId)
                    && (!state.MajorIndex.Entries.TryGetValue(ownerMemoryId, out var majorJobs)
                        || !majorJobs.Any(x => x != null && string.Equals(NormalizeMemoryHeroId(x.HeroId), ownerMemoryId, StringComparison.OrdinalIgnoreCase))))
                    _dailyMemoryDraftSealQueuedMajor.Remove(ownerMemoryId);
                int majorCount = _npcMajorActionSummaryQueue.Count;
                TryEnqueueMajorActionSummaryForDraft(draft, _dailyMemoryDraftSealQueuedMajor, ownerAlreadyEligible: true);
                if (_npcMajorActionSummaryQueue.Count > majorCount)
                    state.MajorIndex.Add(ownerMemoryId, _npcMajorActionSummaryQueue[_npcMajorActionSummaryQueue.Count - 1]);
                state.MajorIndex.Bind(_npcMajorActionSummaryQueue);
                if (draft.SummaryRetryCount >= 3) { draft.QueuedForSummary = false; continue; }
                string key = ownerMemoryId + "|" + draft.GameDayIndex;
                if (HasCompressedMemoryBlock(ownerMemoryId, draft.GameDayIndex))
                { list.RemoveAt(draftIndex); state.ActiveOwner.Bind(list); continue; }
                if (!_dailyMemoryDraftSealQueued.Contains(key) || !DailyMemorySealHasCurrentJob(state.DailyIndex, key))
                {
                    var job = new MemorySummaryJob { HeroId = ownerMemoryId, HeroName = draft.HeroName, GameDayIndex = draft.GameDayIndex, GameDate = draft.GameDate };
                    _memorySummaryQueue.Add(job);
                    _dailyMemoryDraftSealQueued.Add(key);
                    state.DailyIndex.Add(key, job);
                    state.DailyIndex.Bind(_memorySummaryQueue);
                }
                draft.QueuedForSummary = true;
            }
            if (list.Count == 0)
            {
                if (!budget.Take(false)) return false;
            }
            else
            {
                if (state.Normalization == null) state.Normalization = new DailyMemoryDraftNormalization(list);
                if (!state.Normalization.Step(() => _dailyMemoryDrafts != null && _dailyMemoryDrafts.TryGetValue(ownerKey, out var active) ? active : null, budget, out var normalized))
                {
                    if (state.Normalization.Invalidated)
                    {
                        // A changed key can require a NEW seal job, not merely a
                        // re-sort. Revisit this owner and rebuild the live indexes.
                        state.ActiveOwner = null;
                        state.Normalization = null;
                        RestartDailyMemorySealIndexes(state);
                    }
                    return false;
                }
                list = normalized;
            }
            if (list.Count > 0) _dailyMemoryDrafts[ownerKey] = list;
            else { _dailyMemoryDrafts.Remove(ownerKey); list = null; }
            state.BindOwners(_dailyMemoryDrafts);
            CompleteDailyMemorySealOwner(state, ownerKey, list);
        }
        return true;
    }
}
