using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // Private recall projections, never persisted or exposed as complete memory records.
    private sealed class HistoryPromptSnapshot
    {
        internal long Generation;
        internal int GameDay;
        internal string Scene;
        internal int FinalCount;
        internal int CandidateLimit;
        internal int PreprocessMode;
        internal int BlockCount;
        internal int DraftCount;
        internal string Overview;
        internal string RecallQuery;
        internal List<CompressedMemoryBlock> Blocks;
    }

    internal static Func<string> CaptureHistoryContextWorkForHero(Hero hero, string currentInput,
        string secondaryInput, bool includeCurrentActiveSceneSession, long generation)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("History capture requires the game thread.");
        return CaptureHistoryContextWorkById(GetMemoryHeroId(hero), hero?.Name?.ToString() ?? "NPC",
            currentInput, secondaryInput, includeCurrentActiveSceneSession, generation);
    }

    internal static Func<string> CaptureHistoryContextWorkById(string memoryId, string memoryName,
        string currentInput, string secondaryInput, bool includeCurrentActiveSceneSession, long generation)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("History capture requires the game thread.");
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return null;
        MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        string id = NormalizeMemoryHeroId(memoryId);
        if (owner == null || !IsMemoryEntityEligibleForCompressedMemory(id)) return () => "";
        if (!ReferenceEquals(Instance, owner)) return null;

        int blockCount = owner.LoadCompressedMemoryBlocksById(id).Count;
        List<DailyMemoryDraft> drafts = owner.LoadDailyMemoryDraftsById(id);
        int draftCount = drafts.Count;
        // Keep the original overview/sanitize order before projecting the blocks for recall.
        string overview = owner.BuildMemoryOverviewContextById(id);
        string scene = ResolveCurrentMemorySceneLabel();
        List<CompressedMemoryBlock> blocks = owner.LoadCompressedMemoryBlocksById(id);
        int finalCount = GetMemoryFinalInjectCountFromSettings();
        int candidateLimit = GetMemoryCandidateLimitFromSettings();
        HistoryPromptSnapshot snapshot = new HistoryPromptSnapshot
        {
            Generation = generation,
            GameDay = GetCurrentGameDayIndexSafe(),
            Scene = scene,
            FinalCount = finalCount,
            CandidateLimit = candidateLimit,
            PreprocessMode = GetMemoryPreprocessModeFromSettings(),
            BlockCount = blockCount,
            DraftCount = draftCount,
            Overview = overview,
            // Below this upper bound the existing recall path never requests embedding.
            RecallQuery = blocks.Count > Math.Max(finalCount, candidateLimit)
                ? BuildMemoryRecallQueryText(null, currentInput, secondaryInput, drafts, scene) : "",
            Blocks = blocks.Select(CopyHistoryRecallBlock).ToList()
        };
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return null;
        return () =>
        {
            if (!ReferenceEquals(Instance, owner) || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return "";
            return owner.BuildHistoryContextById(id, memoryName, 0, currentInput, secondaryInput,
                includeCurrentActiveSceneSession, snapshot);
        };
    }

    private static CompressedMemoryBlock CopyHistoryRecallBlock(CompressedMemoryBlock block)
    {
        if (block == null) return null;
        // Only fields read by the existing recall/selection/render path belong in this projection.
        // In particular, AFEF must not retain a mutable list owned by the campaign.
        return new CompressedMemoryBlock
        {
            Id = block.Id,
            HeroId = block.HeroId,
            HeroName = block.HeroName,
            GameDayIndex = block.GameDayIndex,
            GameDate = block.GameDate,
            StartHour = block.StartHour,
            EndHour = block.EndHour,
            CreatedUtcTicks = block.CreatedUtcTicks,
            RichTitle = block.RichTitle,
            Summary = block.Summary,
            AfefLines = block.AfefLines == null ? null : new List<string>(block.AfefLines)
        };
    }
}
