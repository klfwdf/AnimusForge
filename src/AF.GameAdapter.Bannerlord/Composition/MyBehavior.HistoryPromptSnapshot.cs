using System;
using AnimusForge.Refactor.Adapters;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts _historyWorkCapturePorts;
    private SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts HistoryWorkCapturePorts => _historyWorkCapturePorts ??= new SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts
    {
        IsCurrentOwner = () => ReferenceEquals(Instance, this),
        LoadBlocks = _memoryBusinessState.LoadBlocks,
        LoadDrafts = _memoryBusinessState.LoadDrafts,
        Overview = id => MemorySummaryApplicationAdapter.BuildMemoryOverviewContextById(MemoryQueueState, id, MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory, LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings),
        BuildContext = (id, name, max, input, secondary, include, snapshot) => _memoryBusinessState.BuildHistoryContextById(_memoryHistoryContext, id, name, max, input, secondary, include, snapshot)
    };
    internal static SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts ResolveHistoryWorkCapturePorts()
    {
        MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        return owner?.HistoryWorkCapturePorts;
    }

    // Private recall projections, never persisted or exposed as complete memory records.
    internal sealed class HistoryPromptSnapshot
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
        return SharedPromptCaptureBannerlordAdapter.CaptureHistoryContextWorkForHero(ResolveHistoryWorkCapturePorts, hero, currentInput, secondaryInput, includeCurrentActiveSceneSession, generation);
    }

    internal static Func<string> CaptureHistoryContextWorkById(string memoryId, string memoryName,
        string currentInput, string secondaryInput, bool includeCurrentActiveSceneSession, long generation)
    {
        return SharedPromptCaptureBannerlordAdapter.CaptureHistoryContextWorkById(ResolveHistoryWorkCapturePorts, memoryId, memoryName, currentInput, secondaryInput, includeCurrentActiveSceneSession, generation);
    }

    private static CompressedMemoryBlock CopyHistoryRecallBlock(CompressedMemoryBlock block)
    {
        return SharedPromptCaptureBannerlordAdapter.CopyHistoryRecallBlock(block);
    }
}
