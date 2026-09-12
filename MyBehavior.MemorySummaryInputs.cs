using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private class MemorySummaryInput
    {
        internal string SystemPrompt;
        internal string UserPrompt;
        // This lease is evaluated only inside the existing main-thread/generation boundary.
        internal Func<bool> SourceIsCurrent;
    }

    private sealed class DailyMemorySummaryInput : MemorySummaryInput
    {
        internal MemorySummaryJob Job;
        internal CompressedMemoryBlock Metadata;
        internal int EffectiveTrust;
        internal Func<string, string> RenderPlayerHistory;
    }

    private sealed class MajorActionSummaryInput : MemorySummaryInput
    {
        internal MajorActionSummaryJob Job;
        internal string HeroName;
        internal List<NpcActionEntry> AllActions;
        internal MajorActionSummaryState ReusableState;
    }

    private sealed class MemoryOverviewInput : MemorySummaryInput
    {
        internal MemoryOverviewJob Job;
        internal string HeroName;
        internal MemoryOverviewState ExistingState;
        internal List<CompressedMemoryBlock> SourceBlocks;
        internal MemoryOverviewState ReusableState;
    }

    // Full private data DTOs preserve AFEF, weekly receipt provenance and future fields.
    // Json.NET caches contracts here; no process-wide defaults, type names or game objects.
    // Work is per started job and retry/acceptance, never a world scan or per-frame snapshot.
    private static readonly JsonSerializerSettings MemorySummarySnapshotJson = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
        Culture = CultureInfo.InvariantCulture
    };

    private static string SerializeMemorySummarySource(object source)
    {
        using (var writer = new System.IO.StringWriter(CultureInfo.InvariantCulture))
        {
            JsonSerializer.Create(MemorySummarySnapshotJson).Serialize(writer, source);
            return writer.ToString();
        }
    }

    private static T CopyMemorySummarySource<T>(T source)
    {
        using (var reader = new JsonTextReader(new System.IO.StringReader(SerializeMemorySummarySource(source))))
        {
            return JsonSerializer.Create(MemorySummarySnapshotJson).Deserialize<T>(reader);
        }
    }

    private sealed class MemorySummarySourceStamp
    {
        private readonly object _source;
        private readonly object[] _members;
        private readonly string _content;

        internal MemorySummarySourceStamp(object source)
        {
            _source = source;
            _members = source is IList list ? list.Cast<object>().ToArray() : null;
            _content = SerializeMemorySummarySource(source);
        }

        internal bool Matches(object current)
        {
            if (!ReferenceEquals(_source, current)) return false;
            if (_members != null)
            {
                if (!(current is IList list) || list.Count != _members.Length) return false;
                for (int i = 0; i < _members.Length; i++)
                {
                    if (!ReferenceEquals(_members[i], list[i])) return false;
                }
            }
            return string.Equals(_content, SerializeMemorySummarySource(current), StringComparison.Ordinal);
        }
    }

    private static T GetMemorySummarySource<T>(Dictionary<string, T> sources, string id) where T : class
        => sources != null && sources.TryGetValue(id, out T value) ? value : null;

    private static Func<bool> CaptureMemorySummarySourceCheck<TJob>(TJob job, Func<IEnumerable<TJob>> queue,
        object source, Func<object> currentSource, object state, Func<object> currentState,
        Func<bool> pending) where TJob : class
    {
        var jobStamp = new MemorySummarySourceStamp(job);
        var sourceStamp = new MemorySummarySourceStamp(source);
        var stateStamp = new MemorySummarySourceStamp(state);
        return () => (queue()?.Any(candidate => ReferenceEquals(candidate, job)) ?? false)
            && jobStamp.Matches(job) && sourceStamp.Matches(currentSource())
            && stateStamp.Matches(currentState()) && pending();
    }

    private static bool IsMemorySummaryInputCurrent(MemorySummaryInput input)
        => TWParallel.IsMainThread() && input?.SourceIsCurrent != null && input.SourceIsCurrent();

    private DailyMemorySummaryInput CaptureDailyMemorySummaryInput(MemorySummaryJob job)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Summary capture requires the main thread.");
        if (job == null || !(_memorySummaryQueue?.Contains(job) ?? false) || !HasMemorySummaryJobStillPending(job)) return null;
        string id = NormalizeMemoryHeroId(job.HeroId);
        Hero hero = FindHeroById(id);
        DailyMemoryDraft source = FindMemoryDraft(job);
        if (source?.Lines == null || source.Lines.Count == 0) return null;
        DailyMemoryDraft draft = CopyMemorySummarySource(source);
        var ordered = draft.Lines.Where(x => x != null).OrderBy(x => x.GameHour).ToList();
        var input = new DailyMemorySummaryInput
        {
            Job = CopyMemorySummarySource(job),
            SystemPrompt = BuildMemorySummarySystemPrompt(draft),
            UserPrompt = BuildMemorySummaryUserPrompt(hero, draft),
            EffectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0,
            RenderPlayerHistory = PlayerNotorietyBehavior.CaptureMemorySummaryHistoryRenderer(),
            Metadata = new CompressedMemoryBlock
            {
                Id = BuildCompressedMemoryBlockId(id, draft.GameDayIndex),
                HeroId = id,
                HeroName = hero?.Name?.ToString() ?? draft.HeroName ?? "NPC",
                GameDayIndex = draft.GameDayIndex,
                GameDate = draft.GameDate ?? "",
                StartHour = ordered.Select(x => MBMath.ClampInt(x.GameHour, 0, 23)).DefaultIfEmpty(0).Min(),
                EndHour = ordered.Select(x => MBMath.ClampInt(x.GameHour, 0, 23)).DefaultIfEmpty(0).Max(),
                Scenes = ordered.Select(x => (x.Scene ?? "").Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToList(),
                AfefLines = ordered.Where(x => x.IsAfef).Select(BuildDailyMemoryLineForPrompt).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                WeeklyMaterialTriggers = SanitizeWeeklyMemoryMaterialTriggers(draft.WeeklyMaterialTriggers)
            }
        };
        input.SourceIsCurrent = CaptureMemorySummarySourceCheck(job, () => _memorySummaryQueue,
            source, () => FindMemoryDraft(input.Job), null, () => null,
            () => ReferenceEquals(FindHeroById(id), hero) && HasMemorySummaryJobStillPending(job));
        return input;
    }

    private MajorActionSummaryInput CaptureMajorActionSummaryInput(MajorActionSummaryJob job)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Summary capture requires the main thread.");
        if (job == null || !(_npcMajorActionSummaryQueue?.Contains(job) ?? false)) return null;
        string id = NormalizeMemoryHeroId(job.HeroId);
        Hero hero = FindHeroById(id);
        var raw = GetMemorySummarySource(_npcMajorActions, id);
        if (!IsHeroNpcEligibleForCompressedMemory(hero) || raw == null) return null;
        var allActions = SanitizeNpcActionEntries(CopyMemorySummarySource(raw), keepOnlyRecentWindow: false);
        if (allActions.Count == 0) return null;
        var state = GetMajorActionSummaryState(id);
        var existing = CopyMemorySummarySource(state);
        bool hasExisting = existing != null && !string.IsNullOrWhiteSpace(existing.Summary);
        var actions = hasExisting ? allActions.Where(x => IsNpcActionAfterSummaryCursor(x, existing)).ToList() : allActions;
        if (actions.Count == 0 && !hasExisting) return null;
        var input = new MajorActionSummaryInput
        {
            Job = CopyMemorySummarySource(job),
            HeroName = hero?.Name?.ToString() ?? job.HeroName ?? "NPC",
            AllActions = allActions,
            ReusableState = actions.Count == 0 ? existing : null
        };
        if (actions.Count > 0)
        {
            if (!HasMajorActionSummaryJobStillPending(job)) return null;
            int target = GetMajorActionSummaryTargetChars(existing, hero, actions);
            input.SystemPrompt = BuildMajorActionSummarySystemPrompt(target);
            input.UserPrompt = BuildMajorActionSummaryUserPrompt(hero, existing, actions, target);
        }
        input.SourceIsCurrent = CaptureMemorySummarySourceCheck(job, () => _npcMajorActionSummaryQueue,
            raw, () => GetMemorySummarySource(_npcMajorActions, id), state, () => GetMemorySummarySource(_npcMajorActionSummaries, id),
            () => ReferenceEquals(FindHeroById(id), hero) && IsHeroNpcEligibleForCompressedMemory(hero)
                && (input.ReusableState != null || HasMajorActionSummaryJobStillPending(job)));
        return input;
    }

    private MemoryOverviewInput CaptureMemoryOverviewInput(MemoryOverviewJob job)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Summary capture requires the main thread.");
        if (job == null || !(_memoryOverviewQueue?.Contains(job) ?? false)) return null;
        string id = NormalizeMemoryHeroId(job.HeroId);
        Hero hero = FindHeroById(id);
        var raw = GetMemorySummarySource(_compressedMemoryBlocks, id);
        if (!(IsNonHeroMemoryId(id) || IsHeroNpcEligibleForCompressedMemory(hero)) || raw == null) return null;
        var blocks = SanitizeCompressedMemoryBlocks(CopyMemorySummarySource(raw));
        if (blocks.Count < GetMemoryOverviewStartBlockCountFromSettings()) return null;
        var state = GetMemoryOverviewState(id);
        var existing = CopyMemorySummarySource(state) ?? new MemoryOverviewState { HeroId = id, HeroName = (job.HeroName ?? "").Trim() };
        bool hasExisting = !string.IsNullOrWhiteSpace(existing.Summary);
        var included = new HashSet<string>(hasExisting ? existing.IncludedBlockIds ?? new List<string>() : new List<string>(), StringComparer.OrdinalIgnoreCase);
        var sourceBlocks = hasExisting ? blocks.Where(block => !IsMemoryBlockIncludedInOverview(block, included)).ToList() : blocks;
        if (sourceBlocks.Count == 0 && !hasExisting) return null;
        var input = new MemoryOverviewInput
        {
            Job = CopyMemorySummarySource(job),
            HeroName = hero?.Name?.ToString() ?? job.HeroName ?? "NPC",
            ExistingState = existing,
            SourceBlocks = sourceBlocks,
            ReusableState = sourceBlocks.Count == 0 ? existing : null
        };
        if (sourceBlocks.Count > 0)
        {
            if (!HasMemoryOverviewJobStillPending(job)) return null;
            int target = GetMemoryOverviewTargetCharsFromSettings();
            input.SystemPrompt = BuildMemoryOverviewSummarySystemPrompt(target);
            input.UserPrompt = BuildMemoryOverviewSummaryUserPrompt(hero, existing, sourceBlocks, target);
        }
        input.SourceIsCurrent = CaptureMemorySummarySourceCheck(job, () => _memoryOverviewQueue,
            raw, () => GetMemorySummarySource(_compressedMemoryBlocks, id), state, () => GetMemorySummarySource(_memoryOverviewStates, id),
            () => ReferenceEquals(FindHeroById(id), hero) && (IsNonHeroMemoryId(id) || IsHeroNpcEligibleForCompressedMemory(hero))
                && (input.ReusableState != null || HasMemoryOverviewJobStillPending(job)));
        return input;
    }
}
