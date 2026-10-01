using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// JSON field names and defaults are the existing PlayerExports wire format.
internal sealed class CompressedMemoryExportBundle
{
    public List<DailyMemoryDraft> DailyDrafts = new List<DailyMemoryDraft>();
    public List<CompressedMemoryBlock> Blocks = new List<CompressedMemoryBlock>();
    public List<MemorySummaryJob> SummaryQueue = new List<MemorySummaryJob>();
    public MemoryOverviewState Overview = new MemoryOverviewState();
    public List<MemoryOverviewJob> OverviewQueue = new List<MemoryOverviewJob>();
}

// A short-lived view over the campaign's existing authoritative containers, not a second store.
internal sealed class MemoryImportExportState
{
    internal Dictionary<string, List<DailyMemoryDraft>> DailyDrafts;
    internal Dictionary<string, List<CompressedMemoryBlock>> Blocks;
    internal List<MemorySummaryJob> SummaryQueue;
    internal Dictionary<string, MemoryOverviewState> Overviews;
    internal List<MemoryOverviewJob> OverviewQueue;
}

internal static class MemoryImportExportOwner
{
    // Legacy all-data import intentionally retains supplied list identity and key spelling.
    // This is a user-request-frequency commit over the existing campaign store.
    internal static void ApplyDialogueHistoryImports(MemoryBusinessStateOwner state,
        Dictionary<string, List<MyBehavior.DialogueDay>> imported, bool overwriteExisting)
    {
        if (imported == null) return;
        state.History ??= new Dictionary<string, List<MyBehavior.DialogueDay>>();
        foreach (var item in imported)
        {
            if (string.IsNullOrEmpty(item.Key) || item.Value == null) continue;
            if (!overwriteExisting && state.History.ContainsKey(item.Key)) continue;
            if (overwriteExisting) state.History.Remove(item.Key);
            state.History[item.Key] = item.Value;
        }
    }
    internal static CompressedMemoryExportBundle Build(string heroId, MemoryImportExportState state)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        var bundle = new CompressedMemoryExportBundle();
        if (string.IsNullOrWhiteSpace(id) || state == null) return bundle;

        // Preserve the main-thread sanitizer's in-place alias behavior for live records.
        if (state.DailyDrafts != null && state.DailyDrafts.TryGetValue(id, out var drafts) && drafts != null)
            bundle.DailyDrafts = MemoryRecordRules.SanitizeDailyMemoryDrafts(drafts);
        if (state.Blocks != null && state.Blocks.TryGetValue(id, out var blocks) && blocks != null)
            bundle.Blocks = MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks);
        bundle.SummaryQueue = MemoryRecordRules.SanitizeMemorySummaryQueue((state.SummaryQueue ?? new List<MemorySummaryJob>())
            .Where(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id));
        if (state.Overviews != null && state.Overviews.TryGetValue(id, out var overview) && overview != null)
            bundle.Overview = MemoryRecordRules.SanitizeMemoryOverviewState(overview.CopyForSummary());
        bundle.OverviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue((state.OverviewQueue ?? new List<MemoryOverviewJob>())
            .Where(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id));
        return bundle;
    }

    internal static bool HasData(string heroId, MemoryImportExportState state)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || state == null) return false;
        return (state.DailyDrafts?.ContainsKey(id) ?? false)
            || (state.Blocks?.ContainsKey(id) ?? false)
            || (state.SummaryQueue?.Any(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id) ?? false)
            || (state.Overviews?.ContainsKey(id) ?? false)
            || (state.OverviewQueue?.Any(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id) ?? false);
    }

    // Called only after the host's campaign/generation validation, on the campaign thread.
    internal static bool Apply(string heroId, CompressedMemoryExportBundle bundle, bool overwriteExisting,
        MemoryImportExportState state, Action<string> markOverviewDirty)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || bundle == null || state == null) return false;

        state.DailyDrafts ??= new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
        state.Blocks ??= new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
        state.SummaryQueue ??= new List<MemorySummaryJob>();
        state.Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        state.OverviewQueue ??= new List<MemoryOverviewJob>();
        var drafts = MemoryRecordRules.SanitizeDailyMemoryDrafts(bundle.DailyDrafts);
        var blocks = MemoryRecordRules.SanitizeCompressedMemoryBlocks(bundle.Blocks);
        var queue = MemoryRecordRules.SanitizeMemorySummaryQueue(bundle.SummaryQueue);
        var overview = MemoryRecordRules.SanitizeMemoryOverviewState(bundle.Overview);
        var overviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue(bundle.OverviewQueue);
        if (overwriteExisting)
        {
            state.DailyDrafts.Remove(id);
            state.Blocks.Remove(id);
            state.SummaryQueue.RemoveAll(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id);
            state.Overviews.Remove(id);
            state.OverviewQueue.RemoveAll(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id);
        }
        if (drafts.Count > 0 && (overwriteExisting || !state.DailyDrafts.ContainsKey(id)))
        {
            foreach (var draft in drafts) draft.HeroId = id;
            state.DailyDrafts[id] = drafts;
        }
        if (blocks.Count > 0 && (overwriteExisting || !state.Blocks.ContainsKey(id)))
        {
            foreach (var block in blocks)
            {
                block.HeroId = id;
                if (string.IsNullOrWhiteSpace(block.Id)) block.Id = MemoryRecordRules.BuildCompressedMemoryBlockId(id, block.GameDayIndex);
            }
            state.Blocks[id] = blocks;
            markOverviewDirty?.Invoke(id);
        }
        if (queue.Count > 0 && overwriteExisting)
        {
            foreach (var job in queue) { job.HeroId = id; state.SummaryQueue.Add(job); }
            state.SummaryQueue = MemoryRecordRules.SanitizeMemorySummaryQueue(state.SummaryQueue);
        }
        if (overview != null && (!string.IsNullOrWhiteSpace(overview.Summary) || !string.IsNullOrWhiteSpace(overview.LastError))
            && (overwriteExisting || !state.Overviews.ContainsKey(id)))
        {
            overview.HeroId = id;
            state.Overviews[id] = MemoryRecordRules.SanitizeMemoryOverviewState(overview);
        }
        if (overviewQueue.Count > 0 && overwriteExisting)
        {
            foreach (var job in overviewQueue) { job.HeroId = id; state.OverviewQueue.Add(job); }
            state.OverviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue(state.OverviewQueue);
        }
        return true;
    }
}
