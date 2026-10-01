using AnimusForge.Refactor.Runtime;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AnimusForge;

internal sealed class MemorySummaryPlanningOwner
{
    private readonly MemoryBusinessStateOwner _state;
    private readonly MemorySummaryPlanningPort _port;
    internal MemorySummaryPlanningOwner(MemoryBusinessStateOwner state, MemorySummaryPlanningPort port)
    { _state = state; _port = port; }
    

    

    internal MemorySummaryPlanEntry DescribeMemorySummaryJob(object job, int ordinal)
    {
        var entry = new MemorySummaryPlanEntry { Job = job, Ordinal = ordinal };
        if (job is MemorySummaryJob daily)
        {
            entry.Kind = 0; entry.HeroId = daily.HeroId; entry.Name = daily.HeroName;
            entry.Day = daily.GameDayIndex; entry.Key = daily.HeroId + "|" + daily.GameDayIndex;
        }
        else if (job is MajorActionSummaryJob major)
        {
            entry.Kind = 1; entry.HeroId = major.HeroId; entry.Name = major.HeroName;
            entry.Day = major.TriggerGameDayIndex; entry.Key = major.HeroId;
        }
        else if (job is MemoryOverviewJob overview)
        {
            entry.Kind = 2; entry.HeroId = overview.HeroId; entry.Name = overview.HeroName;
            entry.Day = overview.TriggerGameDayIndex; entry.Key = overview.HeroId;
        }
        else return null;
        entry.JobFingerprint = MemorySummaryPlanningRules.Fingerprint(job);
        return entry;
    }

    private static bool IsMemorySummaryQueueStructureCurrent<T>(List<T> source,
        List<T> current, ref List<T>.Enumerator probe)
    {
        if (!ReferenceEquals(source, current)) return false;
        try { probe.MoveNext(); return true; }
        catch (InvalidOperationException) { return false; }
    }

    // Classify at most the existing maintenance job allowance per operation. A
    // rejected slot is nulled immediately on the main thread, never from an old
    // cross-tick predicate. All persisted queue consumers already tolerate null.
    // Later compaction copies EVERY non-null reference; fields may change freely.
    private async Task<List<MemorySummaryPlanEntry>> ScanMemorySummaryQueueAsync<T>(
        long generation, Func<List<T>> getQueue, Action<List<T>> setQueue,
        Func<T, bool> isPending, Func<T, T> normalize, Func<T, string> getHeroId,
        HashSet<string> unavailableOwners, bool collectPlan = true, MemorySummaryRunOwner.Lease run = null) where T : class
    {
        List<T> source = null;
        var probe = default(List<T>.Enumerator);
        int cursor = 0, limit = 0;
        bool deferred = false, hasHoles = false;
        var entries = new List<MemorySummaryPlanEntry>();
        bool accepted = await _port.RunPhaseAsync(run, generation, delegate
        {
            source = getQueue();
            limit = source?.Count ?? 0;
            if (source != null) probe = source.GetEnumerator();
            return true;
        });
        if (!accepted) return null;
        while (cursor < limit && !deferred)
        {
            accepted = await _port.RunPhaseAsync(run, generation, delegate
            {
                if (!IsMemorySummaryQueueStructureCurrent(source, getQueue(), ref probe))
                { deferred = true; return true; }
                long started = Stopwatch.GetTimestamp();
                double budget = _port.GetBudgetMilliseconds()
                    - _port.ElapsedTicks() * 1000.0 / Stopwatch.Frequency;
                int visited = 0;
                while (cursor < limit && visited < _port.MaxJobs
                    && (visited == 0 || !MemorySummaryPlanningRules.IsBudgetExceeded(started, budget)))
                {
                    int index = cursor++;
                    T job = source[index];
                    visited++; // Count null slots too, not only expensive predicates.
                    if (job == null) { hasHoles = true; continue; }
                    bool pending = isPending(job);
                    if (!pending)
                    {
                        string id = MemoryRecordRules.NormalizeMemoryHeroId(getHeroId(job));
                        if (!string.IsNullOrEmpty(id) && !MemorySummaryPlanningRules.IsNonHero(id)
                            && !_port.IsEntityEligible(id)) unavailableOwners?.Add(id);
                    }
                    T normalized = pending ? normalize(job) : null;
                    if (normalized == null)
                    {
                        source[index] = null;
                        hasHoles = true;
                    }
                    else if (collectPlan) entries.Add(DescribeMemorySummaryJob(normalized, index));
                }
                // Only our own slot removals happened inside this non-yielding slice.
                probe = source.GetEnumerator();
                return true;
            });
            if (!accepted) return null;
        }
        if (deferred || !hasHoles || source == null) return entries;

        // No RemoveAt/RemoveRange per item: compaction is linear in references.
        // A concurrent structural write or list replacement abandons this buffer
        // once; current authoritative work remains for the next scheduling pass.
        var compact = new List<T>();
        cursor = 0;
        while (cursor < limit && !deferred)
        {
            accepted = await _port.RunPhaseAsync(run, generation, delegate
            {
                if (!IsMemorySummaryQueueStructureCurrent(source, getQueue(), ref probe))
                { deferred = true; return true; }
                long started = Stopwatch.GetTimestamp();
                double budget = _port.GetBudgetMilliseconds()
                    - _port.ElapsedTicks() * 1000.0 / Stopwatch.Frequency;
                int visited = 0;
                while (cursor < limit && visited < _port.MaxJobs
                    && (visited == 0 || !MemorySummaryPlanningRules.IsBudgetExceeded(started, budget)))
                {
                    T item = source[cursor++];
                    visited++;
                    if (item != null) compact.Add(item);
                }
                // Structural check and publication share the final slice. We do
                // not re-use old eligibility/key decisions to omit a live object.
                if (cursor == limit) setQueue(compact);
                return true;
            });
            if (!accepted) return null;
        }
        return entries;
    }

    internal async Task<MemorySummaryPlan> BuildMemorySummaryPlanAsync(long generation,
        bool overviewOnly = false, HashSet<string> excludedOverviewIds = null, bool cleanupOnly = false, MemorySummaryRunOwner.Lease run = null)
    {
        var entries = new List<MemorySummaryPlanEntry>();
        var unavailableOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CultureInfo culture = null;
        if (!await _port.RunPhaseAsync(run, generation, () =>
        { culture = CultureInfo.CurrentCulture; return true; })) return null;
        if (!overviewOnly)
        {
            var daily = await ScanMemorySummaryQueueAsync(generation,
                () => _state.DailyQueue, x => _state.DailyQueue = x,
                _port.IsDailyPending,
                x => MemoryRecordRules.SanitizeMemorySummaryQueue(new[] { x }).FirstOrDefault(), x => x.HeroId, unavailableOwners, !cleanupOnly, run);
            if (daily == null) return null;
            entries.AddRange(daily);
            var major = await ScanMemorySummaryQueueAsync(generation,
                () => _state.MajorQueue, x => _state.MajorQueue = x,
                _port.IsMajorPending,
                x => MemoryRecordRules.SanitizeMajorActionSummaryQueue(new[] { x }).FirstOrDefault(), x => x.HeroId, unavailableOwners, !cleanupOnly, run);
            if (major == null) return null;
            entries.AddRange(major);
        }
        var overview = await ScanMemorySummaryQueueAsync(generation,
            () => _state.OverviewQueue, x => _state.OverviewQueue = x,
            _port.IsOverviewPending,
            x => MemoryRecordRules.SanitizeMemoryOverviewQueue(new[] { x }).FirstOrDefault(), x => x.HeroId, unavailableOwners, !cleanupOnly, run);
        if (overview == null) return null;
        entries.AddRange(overview);
        // This existing cancellation also owns derived states and candidate indexes.
        // Keep it outside scanner cursors and re-check eligibility at execution time.
        foreach (string id in unavailableOwners)
        {
            if (!await _port.RunPhaseAsync(run, generation, delegate
            {
                if (!_port.IsEntityEligible(id))
                    _port.CancelUnavailable(id, "queue_execute");
                return true;
            })) return null;
        }
        if (cleanupOnly) return new MemorySummaryPlan();
        return await Task.Run(delegate
        {
            // Pure frozen metadata only. Never inspect entry.Job on this worker.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unique = entries.Where(x => seen.Add(x.Kind + ":" + x.Key)).ToList();
            var dailyIds = new HashSet<string>(unique.Where(x => x.Kind == 0).Select(x => x.HeroId), StringComparer.OrdinalIgnoreCase);
            var ordered = unique.Where(x => x.Kind != 2 || (!dailyIds.Contains(x.HeroId)
                && !(excludedOverviewIds?.Contains(x.HeroId) ?? false)))
                .OrderBy(x => x.Kind).ThenBy(x => x.Day)
                .ThenBy(x => x.Name, StringComparer.Create(culture, false)).ThenBy(x => x.Ordinal).ToList();
            return new MemorySummaryPlan
            {
                Items = ordered.Cast<object>().ToList(),
                DailyCount = ordered.Count(x => x.Kind == 0),
                MajorCount = ordered.Count(x => x.Kind == 1),
                OverviewCount = ordered.Count(x => x.Kind == 2),
                OverviewIds = new HashSet<string>(ordered.Where(x => x.Kind == 2).Select(x => x.HeroId), StringComparer.OrdinalIgnoreCase)
            };
        }).ConfigureAwait(false);
    }
}

internal sealed class MemorySummaryPlanEntry
    {
        internal object Job;
        internal string JobFingerprint;
        internal string HeroId;
        internal string Key;
        internal string Name;
        internal int Day;
        internal int Kind;
        internal int Ordinal;
    }
internal sealed class MemorySummaryPlan
    {
        internal List<object> Items;
        internal int DailyCount;
        internal int MajorCount;
        internal int OverviewCount;
        internal HashSet<string> OverviewIds;
    }

internal sealed class MemorySummaryPlanningPort
{
 internal Func<MemorySummaryRunOwner.Lease,long,Func<bool>,Task<bool>> RunPhaseAsync;
 internal Func<double> GetBudgetMilliseconds;
 internal Func<long> ElapsedTicks;
 internal int MaxJobs;
 internal Func<string,bool> IsEntityEligible;
 internal Func<MemorySummaryJob,bool> IsDailyPending;
 internal Func<MajorActionSummaryJob,bool> IsMajorPending;
 internal Func<MemoryOverviewJob,bool> IsOverviewPending;
 internal Action<string,string> CancelUnavailable;
}
internal static class MemorySummaryPlanningRules
{
internal static int CountDailySourceChars(DailyMemoryDraft draft)
	{
		if (draft?.Lines == null)
		{
			return 0;
		}
		return draft.Lines.Where((DailyMemoryLine x) => x != null && !x.IsAfef && !string.IsNullOrWhiteSpace(x.Text)).Sum((DailyMemoryLine x) => x.Text.Trim().Length);
	}
internal static bool HasAfef(DailyMemoryDraft draft)
	{
		return draft?.Lines != null && draft.Lines.Any((DailyMemoryLine x) => x != null && x.IsAfef && !string.IsNullOrWhiteSpace(x.Text));
	}

 internal static bool IsNonHero(string id) => !string.IsNullOrWhiteSpace(id) && id.StartsWith("af_nonhero:",StringComparison.OrdinalIgnoreCase);
internal static bool IsBudgetExceeded(long startTimestamp, double budgetMs)
	{
		if (startTimestamp <= 0L || budgetMs <= 0.0 || double.IsInfinity(budgetMs) || double.IsNaN(budgetMs))
		{
			return false;
		}
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		return elapsedMs >= budgetMs;
	}
internal static string Fingerprint(object identity)
    {
        using (var hash = SHA256.Create())
        using (var crypto = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write))
        {
            using (var text = new StreamWriter(crypto, new UTF8Encoding(false), 4096, leaveOpen: true))
            using (var json = new JsonTextWriter(text))
            {
                JsonSerializer.CreateDefault().Serialize(json, identity);
            }
            crypto.FlushFinalBlock();
            return BitConverter.ToString(hash.Hash).Replace("-", "");
        }
    }
}
