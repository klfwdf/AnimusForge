using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// The host's legacy fields are save/engine projections of these same containers.
// Never clones a store and never invokes a whole host Apply/Mark business callback.
// All entry points execute inside the existing campaign-thread dispatcher.
internal sealed partial class MemoryBusinessStateOwner
{
    private MemorySealingOwner _sealing;
    internal MemorySealingOwner Sealing => _sealing ??= new MemorySealingOwner(this);

    internal Dictionary<string, List<DailyMemoryDraft>> Drafts = new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, List<CompressedMemoryBlock>> Blocks = new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, MemoryOverviewState> Overviews = new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, MajorActionSummaryState> MajorSummaries = new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
    internal List<WeeklyMemoryMaterialTrigger> PendingWeeklyTriggers = new List<WeeklyMemoryMaterialTrigger>();
    internal List<MemorySummaryJob> DailyQueue = new List<MemorySummaryJob>();
    internal List<MemoryOverviewJob> OverviewQueue = new List<MemoryOverviewJob>();
    internal List<MajorActionSummaryJob> MajorQueue = new List<MajorActionSummaryJob>();
    internal readonly HashSet<string> DirtyOverviewIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal readonly Queue<string> OverviewCandidateIds = new Queue<string>();
    internal readonly HashSet<string> OverviewCandidateIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    internal List<DailyMemoryDraft> LoadDrafts(string memoryId)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return new List<DailyMemoryDraft>();
        Drafts ??= new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
        return Drafts.TryGetValue(id, out var value) && value != null ? value : new List<DailyMemoryDraft>();
    }

    internal List<CompressedMemoryBlock> LoadBlocks(string memoryId)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return new List<CompressedMemoryBlock>();
        Blocks ??= new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
        return Blocks.TryGetValue(id, out var value) && value != null ? value : new List<CompressedMemoryBlock>();
    }

    internal void SaveDrafts(string memoryId, List<DailyMemoryDraft> drafts)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return;
        Drafts ??= new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
        var normalized = MemoryRecordRules.SanitizeDailyMemoryDrafts(drafts);
        if (normalized.Count > 0) Drafts[id] = normalized;
        else Drafts.Remove(id);
    }

    internal void SaveBlocks(string memoryId, List<CompressedMemoryBlock> blocks, Action<string> markDirty)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return;
        Blocks ??= new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
        var normalized = MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks);
        if (normalized.Count > 0) Blocks[id] = normalized;
        else Blocks.Remove(id);
        markDirty(id);
    }

    internal bool ApplyDaily(MemorySummaryJob job, CompressedMemoryBlock block, string memoryId,
        MemoryDailyCommitEffects effects)
    {
        if (job == null || block == null) return false;
        var blocks = LoadBlocks(memoryId);
        blocks.RemoveAll(x => x != null && x.GameDayIndex == job.GameDayIndex);
        blocks.Add(block);
        SaveBlocks(memoryId, blocks, effects.MarkOverviewDirty);
        var drafts = LoadDrafts(memoryId);
        drafts.RemoveAll(x => x != null && x.GameDayIndex == job.GameDayIndex);
        SaveDrafts(memoryId, drafts);
        // Preserve partial-write behavior: Native failure occurs before queue removal.
        effects.ClearNativeHistory(job.GameDayIndex);
        DailyQueue?.RemoveAll(x => x != null && Same(x.HeroId, memoryId) && x.GameDayIndex == job.GameDayIndex);
        effects.LogSuccess();
        if (!string.IsNullOrWhiteSpace(block.PlayerHistoryMaterial)) effects.RecordPublicMemory(block);
        effects.RecordPublicWeeklyMaterial(block);
        effects.RecordWeeklyMaterial(block);
        effects.EnqueueOverview(memoryId, job.HeroName, blocks);
        return true;
    }

    internal bool ApplyOverview(MemoryOverviewJob job, MemoryOverviewState state, string memoryId, Action log)
    {
        if (job == null || state == null || string.IsNullOrWhiteSpace(state.Summary) || string.IsNullOrWhiteSpace(memoryId)) return false;
        Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        state.HeroId = memoryId;
        if (string.IsNullOrWhiteSpace(state.HeroName)) state.HeroName = (job.HeroName ?? "").Trim();
        Overviews[memoryId] = MemoryRecordRules.SanitizeMemoryOverviewState(state);
        OverviewQueue?.RemoveAll(x => x != null && Same(x.HeroId, memoryId));
        log();
        return true;
    }

    internal bool ApplyMajor(MajorActionSummaryJob job, MajorActionSummaryState state, string memoryId, Action log)
    {
        if (job == null || state == null || string.IsNullOrWhiteSpace(state.Summary) || string.IsNullOrWhiteSpace(memoryId)) return false;
        MajorSummaries ??= new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
        state.HeroId = memoryId;
        if (string.IsNullOrWhiteSpace(state.HeroName)) state.HeroName = (job.HeroName ?? "").Trim();
        MajorSummaries[memoryId] = MemoryRecordRules.SanitizeMajorActionSummaryState(state);
        MajorQueue?.RemoveAll(x => x != null && Same(x.HeroId, memoryId));
        log();
        return true;
    }

    internal void FailDaily(MemorySummaryJob job, string error)
    {
        if (job == null || DailyQueue == null) return;
        string id = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
        foreach (var item in DailyQueue)
            if (item != null && Same(item.HeroId, id) && item.GameDayIndex == job.GameDayIndex)
            { item.RetryCount = 3; item.LastError = (error ?? "").Trim(); }
        var draft = Drafts != null && Drafts.TryGetValue(id, out var drafts)
            ? drafts?.FirstOrDefault(x => x != null && x.GameDayIndex == job.GameDayIndex && Same(x.HeroId, id)) : null;
        if (draft != null) { draft.SummaryRetryCount = 3; draft.LastSummaryError = (error ?? "").Trim(); }
    }

    internal void FailOverview(MemoryOverviewJob job, string error)
    {
        if (job == null) return;
        string id = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
        foreach (var item in OverviewQueue ?? Enumerable.Empty<MemoryOverviewJob>())
            if (item != null && Same(item.HeroId, id)) { item.RetryCount = 3; item.LastError = (error ?? "").Trim(); }
        if (string.IsNullOrWhiteSpace(id)) return;
        Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        var state = Overviews.TryGetValue(id, out var value) ? MemoryRecordRules.SanitizeMemoryOverviewState(value?.CopyForSummary()) : null;
        state ??= new MemoryOverviewState { HeroId = id, HeroName = (job.HeroName ?? "").Trim() };
        state.LastError = (error ?? "").Trim();
        Overviews[id] = MemoryRecordRules.SanitizeMemoryOverviewState(state);
    }

    internal void FailMajor(MajorActionSummaryJob job, string error)
    {
        if (job == null) return;
        string id = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
        foreach (var item in MajorQueue ?? Enumerable.Empty<MajorActionSummaryJob>())
            if (item != null && Same(item.HeroId, id)) { item.RetryCount = 3; item.LastError = (error ?? "").Trim(); }
        if (string.IsNullOrWhiteSpace(id)) return;
        MajorSummaries ??= new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
        var state = MajorSummaries.TryGetValue(id, out var value) ? MemoryRecordRules.SanitizeMajorActionSummaryState(value?.CopyForSummary()) : null;
        state ??= new MajorActionSummaryState { HeroId = id, HeroName = (job.HeroName ?? "").Trim() };
        state.LastError = (error ?? "").Trim();
        MajorSummaries[id] = MemoryRecordRules.SanitizeMajorActionSummaryState(state);
    }

    private static bool Same(string left, string normalizedRight) => string.Equals(
        MemoryRecordRules.NormalizeMemoryHeroId(left), normalizedRight, StringComparison.OrdinalIgnoreCase);
}

// Each effect is one engine/cross-domain operation, not a callback to host memory rules.
internal sealed class MemoryDailyCommitEffects
{
    internal Action<string> MarkOverviewDirty;
    internal Action<int> ClearNativeHistory;
    internal Action LogSuccess;
    internal Action<CompressedMemoryBlock> RecordPublicMemory;
    internal Action<CompressedMemoryBlock> RecordPublicWeeklyMaterial;
    internal Action<CompressedMemoryBlock> RecordWeeklyMaterial;
    internal Action<string, string, List<CompressedMemoryBlock>> EnqueueOverview;
}
