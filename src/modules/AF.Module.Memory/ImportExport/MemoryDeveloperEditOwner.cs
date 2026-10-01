using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// The adapter retains identity/generation checks; UI retains only navigation and display.
// These operations mutate the campaign's existing Memory containers on its thread.
internal static partial class MemoryDeveloperEditOwner
{
    internal static bool DeleteBlock(string heroId, string blockId, MemoryImportExportState state,
        Action<string> markOverviewDirty)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || state?.Blocks == null || !state.Blocks.TryGetValue(id, out var blocks) || blocks == null) return false;
        int removed = blocks.RemoveAll(x => x != null && string.Equals(GetBlockId(x), (blockId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        if (removed == 0) return false;
        SaveBlocks(id, blocks, state, markOverviewDirty);
        return true;
    }

    internal static bool EditBlock(string heroId, string heroName, string blockId,
        Action<CompressedMemoryBlock> mutate, MemoryImportExportState state, Action<string> markOverviewDirty)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || state?.Blocks == null || !state.Blocks.TryGetValue(id, out var blocks) || blocks == null) return false;
        var block = blocks.FirstOrDefault(x => x != null && string.Equals(GetBlockId(x), (blockId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        if (block == null) return false;
        mutate?.Invoke(block);
        block.HeroId = id;
        block.HeroName = heroName ?? block.HeroName ?? "";
        SaveBlocks(id, blocks, state, markOverviewDirty);
        return true;
    }

    private static void SaveBlocks(string id, List<CompressedMemoryBlock> blocks,
        MemoryImportExportState state, Action<string> markOverviewDirty)
    {
        var sanitized = MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks);
        if (sanitized.Count > 0) state.Blocks[id] = sanitized;
        else state.Blocks.Remove(id);
        markOverviewDirty?.Invoke(id);
    }

    private static string GetBlockId(CompressedMemoryBlock block)
    {
        string id = (block.Id ?? "").Trim();
        return string.IsNullOrWhiteSpace(id)
            ? MemoryRecordRules.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex).Trim() : id;
    }

    internal static bool Clear(string heroId, MemoryImportExportState state)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || state == null) return false;
        state.DailyDrafts?.Remove(id);
        state.Blocks?.Remove(id);
        state.SummaryQueue?.RemoveAll(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id);
        state.Overviews?.Remove(id);
        state.OverviewQueue?.RemoveAll(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id);
        return true;
    }

    internal static void InvalidateOverview(string heroId, MemoryImportExportState state)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || state == null) return;
        state.Overviews?.Remove(id);
        state.OverviewQueue?.RemoveAll(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id);
    }

    internal static void InvalidateOverviewForManualEdit(string heroId, MemoryImportExportState state)
    {
        state.Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        state.OverviewQueue ??= new List<MemoryOverviewJob>();
        InvalidateOverview(heroId, state);
    }

    internal static bool SaveOverview(string heroId, string heroName, string input,
        IEnumerable<CompressedMemoryBlock> blocks, long updatedUtcTicks, MemoryImportExportState state)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
        if (string.IsNullOrWhiteSpace(id) || state == null) return false;
        state.Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        state.OverviewQueue ??= new List<MemoryOverviewJob>();
        string summary = NormalizeMultiline(input);
        if (string.IsNullOrWhiteSpace(summary))
        {
            InvalidateOverview(id, state);
            return false; // caller rechecks candidate queue after clear
        }
        var included = MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks)
            .Select(x => string.IsNullOrWhiteSpace(x.Id)
                ? MemoryRecordRules.BuildCompressedMemoryBlockId(x.HeroId, x.GameDayIndex) : x.Id.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var current = state.Overviews.TryGetValue(id, out var existing) && existing != null
            ? existing.CopyForSummary() : new MemoryOverviewState();
        current.HeroId = id;
        current.HeroName = heroName ?? current.HeroName ?? "NPC";
        current.Summary = summary;
        current.IncludedBlockIds = included;
        current.UpdatedUtcTicks = updatedUtcTicks;
        current.LastError = "";
        state.Overviews[id] = MemoryRecordRules.SanitizeMemoryOverviewState(current);
        state.OverviewQueue.RemoveAll(x => x != null && MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId) == id);
        return true;
    }

    internal static string NormalizeMultiline(string input) => (input ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();

    internal static List<string> ParseLineList(string input, int maxCount, bool ignoreCase)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        string text = NormalizeMultiline(input);
        if (string.IsNullOrWhiteSpace(text)) return result;
        foreach (string raw in text.Split('\n'))
        {
            string line = (raw ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(line) && seen.Add(line))
            {
                result.Add(line);
                if (result.Count >= maxCount) break;
            }
        }
        return result;
    }
}
