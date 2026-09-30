using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public delegate bool CanonicalHistoryAppender(
    string kind,
    string sourceKey,
    string sourceId,
    int day,
    string gameDate,
    string authorKingdomId,
    IEnumerable<string> targetKingdomIds,
    string intent,
    string commitment,
    string content,
    bool verified,
    string respondingToOfferDocumentId = null,
    string respondingToThreatDocumentId = null,
    IEnumerable<string> actionFacts = null);

public static class WorldDiplomacyCanonicalHistoryRules
{



    public static string RenderCanonicalHistoryBlock(
        WorldDiplomacyCanonicalHistoryState history,
        long cutoff)
{
StringBuilder sb = new StringBuilder();
    sb.AppendLine("【全局长期外交历史】");
    sb.AppendLine("本档案对所有王国可见；动态当前状态与本档案冲突时，以动态当前状态为准。提议或宣言不等于已执行结果，只有 verified=true 的 diplomatic_result 表示游戏机制已确认改变现实状态。");
    string snapshotPayload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(history.Snapshot);
    if (!string.IsNullOrWhiteSpace(snapshotPayload) && history.Snapshot.CoveredThroughSequence <= cutoff)
    {
        sb.AppendLine("【已压缩历史；覆盖至seq=" + history.Snapshot.CoveredThroughSequence.ToString(CultureInfo.InvariantCulture) + "】");
        sb.AppendLine(snapshotPayload);
    }
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(history.DeltaEntries, cutoff))
    {
        sb.AppendLine(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry));
    }
    if (string.IsNullOrWhiteSpace(snapshotPayload) && !history.DeltaEntries.Any(x => x != null && x.Sequence <= cutoff)) sb.AppendLine("（暂无历史记录）");
    return sb.ToString().TrimEnd();
    }

    public static long ClampCanonicalHistoryThroughSequence(
        WorldDiplomacyCanonicalHistoryState history,
        long throughSequence)
    {
        return Math.Min(throughSequence,
            Math.Max(history.Snapshot.CoveredThroughSequence, history.NextSequence - 1L));
    }

    public static void StampCanonicalHistoryOnJob(
        WorldDiplomacyJob job,
        WorldDiplomacyCanonicalHistoryState history,
        string historyBlock)
    {
        job.HistoryRevision = history.Revision;
        job.HistoryEstimatedTokens = history.EstimatedTokens;
        job.HistorySnapshotThroughSequence = history.Snapshot.CoveredThroughSequence;
        job.HistorySnapshotHash = history.Snapshot.ContentHash ?? "";
        job.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair(job.SystemPrompt, historyBlock);
    }

}
