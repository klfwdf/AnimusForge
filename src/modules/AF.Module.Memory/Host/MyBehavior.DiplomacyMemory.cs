using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    /// <summary>Request-time, ruler-scoped retrieval. Reuses local ONNX recall;
    /// never starts a second LLM/preprocess request or adds work to the frame tick.</summary>
    public static string BuildDiplomacyPersonalMemoryForExternal(Hero ruler, string topic, string counterpart)
    {
        MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        if (owner == null || ruler == null || ruler.IsDead) return "";
        try
        {
            return owner.BuildDiplomacyPersonalMemory(ruler, topic, counterpart);
        }
        catch (Exception ex)
        {
            Logger.Log("WorldDiplomacy", "personal_memory_read_failed ruler=" + ruler.StringId + " error=" + ex.Message);
            return "";
        }
    }

    private string BuildDiplomacyPersonalMemory(Hero ruler, string topic, string counterpart)
    {
        const int maximumChars = 12000;
        StringBuilder material = new StringBuilder(4096);
        material.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_055"));
        material.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_056") + ruler.StringId + MemoryTextResources.Prompt("MEM_Prompt_057"));
        material.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_058"));

        AppendDiplomacyMemorySection(material, BuildMemoryOverviewContext(ruler), 2600, maximumChars);

        // Recent promises must retain a budget even when compressed memories are long.
        List<ConversationMessage> recent = BuildUncompressedMemoryRoleMessages(ruler,
            includeCurrentActiveSceneSession: true);
        AppendDiplomacyMemorySection(material, DiplomacyMemoryMaterial.BuildRecentHistory(recent), 4800, maximumChars);
        string memoryId = GetMemoryHeroId(ruler);
        List<CompressedMemoryBlock> blocks = LoadCompressedMemoryBlocks(ruler);
        int recallCount = Math.Max(2, Math.Min(6, GetMemoryFinalInjectCountFromSettings()));
        if (blocks != null && blocks.Count > 0
            && TryBuildMemoryRecallCandidates(ruler, blocks, topic, counterpart,
                LoadDailyMemoryDraftsById(memoryId), recallCount, out List<MemoryRecallCandidate> candidates, out _, SaveRuntimeGuard.CaptureGeneration()))
        {
            foreach (MemoryRecallCandidate candidate in candidates)
            {
                CompressedMemoryBlock block = candidate?.Block;
                if (block == null) continue;
                AppendDiplomacyMemorySection(material,
                    MemoryTextResources.Prompt("MEM_Prompt_059") + block.GameDate + " " + block.RichTitle + MemoryTextResources.Prompt("AF_MEM_Prompt_SectionEnd") + block.Summary,
                    1800, maximumChars);
                if (block.AfefLines != null)
                    foreach (string fact in block.AfefLines.Take(8))
                        AppendDiplomacyMemorySection(material, fact, 600, maximumChars);
            }
        }

        return material.ToString().TrimEnd();
    }

    private static void AppendDiplomacyMemorySection(StringBuilder target, string text, int sectionLimit, int totalLimit)
    {
        DiplomacyMemoryMaterial.AppendSection(target, text, sectionLimit, totalLimit);
    }

    internal static string GetCurrentDiplomacyMemoryDateForExternal() => GetCurrentGameDateTextSafe();

    internal static MemoryCommitResult CommitDiplomacyFactForExternal(Hero hero, string sourceId,
        string fact, int day, string locationId, int recordedHour = -1, string recordedNpcName = null, string recordedDate = "")
    {
        if (hero == null || string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(fact))
            return new MemoryCommitResult(MemoryCommitStatus.Rejected, "diplomacy_memory_identity_invalid");
        string heroId = hero.StringId;
        string npcName = recordedNpcName ?? hero.Name?.ToString() ?? MemoryTextResources.Raw("MEM_Raw_005");
        InteractionMemoryCommit commit = new InteractionMemoryCommit(
            "diplomacy:" + sourceId + ":" + heroId, InteractionChannel.Domain,
            "diplomacy:" + sourceId, heroId, "", "",
            new[] { new FactRecord("diplomacy", heroId, MemoryTextResources.Raw("MEM_Raw_006") + fact) },
            0L, 0L, sourceId, Math.Max(0, day), recordedHour >= 0 ? recordedHour : GetCurrentMemoryGameHourForExternal(),
            locationId ?? "", -1, -1, npcName, recordedDate);
        return CommitExternalDialogueHistoryRecoverable(commit, false, npcName);
    }
}
