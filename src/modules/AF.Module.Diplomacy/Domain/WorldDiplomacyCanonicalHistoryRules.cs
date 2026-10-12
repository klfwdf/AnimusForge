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
        long cutoff, Func<string, string, string> localize = null)
{
StringBuilder sb = new StringBuilder();
    sb.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_000", "[Global long-term diplomatic history]"));
    sb.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_001", "This archive is visible to all kingdoms. If current dynamic state conflicts with it, current dynamic state takes precedence. Proposals and declarations are not executed results; only a diplomatic_result with verified=true confirms that game mechanics changed reality."));
    string snapshotPayload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(history.Snapshot, localize);
    if (!string.IsNullOrWhiteSpace(snapshotPayload) && history.Snapshot.CoveredThroughSequence <= cutoff)
    {
        sb.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_002", "[Compressed history; covered through seq=") + history.Snapshot.CoveredThroughSequence.ToString(CultureInfo.InvariantCulture) + WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_003", "]"));
        sb.AppendLine(snapshotPayload);
    }
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(history.DeltaEntries, cutoff))
    {
        sb.AppendLine(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, localize));
    }
    if (string.IsNullOrWhiteSpace(snapshotPayload) && !history.DeltaEntries.Any(x => x != null && x.Sequence <= cutoff)) sb.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_004", "(No history recorded yet)"));
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
