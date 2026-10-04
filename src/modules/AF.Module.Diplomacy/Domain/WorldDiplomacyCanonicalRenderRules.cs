using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain
{
    /// <summary>
    /// DPL-060AQ: pure canonical-history rendering/protected-fact rules extracted from the host.
    /// Token estimation arrives as a delegate so the engine-side Logger stays host-bound.
    /// </summary>
    public static class WorldDiplomacyCanonicalRenderRules
    {
    public static string RenderCanonicalHistoryEntry(WorldDiplomacyCanonicalHistoryEntry entry)
    {
        if (entry == null) return "";
        StringBuilder sb = new StringBuilder();
        sb.Append("[seq=").Append(entry.Sequence.ToString(CultureInfo.InvariantCulture))
            .Append("|kind=").Append(entry.Kind ?? "")
            .Append("|date=").Append(entry.GameDate ?? "")
            .Append("|source=").Append(entry.SourceId ?? "");
        if (!string.IsNullOrWhiteSpace(entry.RespondingToOfferDocumentId))
        {
            sb.Append("|responding_to=").Append(entry.RespondingToOfferDocumentId);
        }
        if (!string.IsNullOrWhiteSpace(entry.RespondingToThreatDocumentId))
        {
            sb.Append("|responding_to_threat=").Append(entry.RespondingToThreatDocumentId);
        }
        if (entry.ActionFacts?.Count > 0)
        {
            sb.Append("|actions=").Append(string.Join(",", entry.ActionFacts));
        }
        if (entry.AnsweredPlayerDocumentIds?.Count > 0)
            sb.Append("|answered_player=").Append(string.Join(",", entry.AnsweredPlayerDocumentIds));
        sb.Append("|author=").Append(entry.AuthorKingdomId ?? "")
            .Append("|targets=").Append(string.Join(",", entry.TargetKingdomIds ?? new List<string>()))
            .Append("|intent=").Append(entry.Intent ?? "")
            .Append("|commitment=").Append(entry.Commitment ?? "")
            .Append("|verified=").Append(entry.Verified ? "true" : "false").AppendLine("]");
        sb.Append(entry.Text ?? "");
        return sb.ToString().TrimEnd();
    }

    public static string ProtectedFactStableKey(WorldDiplomacyCanonicalProtectedFact fact)
    {
        if (fact == null) return "";
        string kind = (fact.Kind ?? "").Trim().ToLowerInvariant();
        string sourceKey = (fact.SourceKey ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(sourceKey)) return kind + ":" + sourceKey;
        return kind + ":" + (fact.SourceId ?? "").Trim() + ":" + (fact.RelatedSourceId ?? "").Trim();
    }

    public static WorldDiplomacyCanonicalProtectedFact CloneProtectedFact(WorldDiplomacyCanonicalProtectedFact fact)
    {
        if (fact == null) return null;
        return new WorldDiplomacyCanonicalProtectedFact
        {
            Kind = (fact.Kind ?? "").Trim().ToLowerInvariant(),
            SourceKey = (fact.SourceKey ?? "").Trim(),
            SourceId = (fact.SourceId ?? "").Trim(),
            RelatedSourceId = (fact.RelatedSourceId ?? "").Trim(),
            Sequence = Math.Max(0L, fact.Sequence),
            Day = Math.Max(0, fact.Day),
            GameDate = (fact.GameDate ?? "").Trim(),
            AuthorKingdomId = (fact.AuthorKingdomId ?? "").Trim(),
            TargetKingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(fact.TargetKingdomIds),
            Intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(fact.Intent),
            Commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(fact.Commitment),
            Text = WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(fact.Text)
        };
    }

    public static List<WorldDiplomacyCanonicalProtectedFact> SelectCanonicalProtectedFactsWithinTokenBudget(
        IEnumerable<WorldDiplomacyCanonicalProtectedFact> source,
        long tokenBudget, Func<string, long> estimateTokens)
    {
        if (tokenBudget <= 0L) return new List<WorldDiplomacyCanonicalProtectedFact>();
        List<WorldDiplomacyCanonicalProtectedFact> selected = new List<WorldDiplomacyCanonicalProtectedFact>();
        long estimated = 0L;
        foreach (WorldDiplomacyCanonicalProtectedFact fact in WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequenceDescending((source ?? Enumerable.Empty<WorldDiplomacyCanonicalProtectedFact>())
                .Where(x => x != null)))
        {
            long factTokens = estimateTokens(RenderCanonicalProtectedFacts(
                new[] { fact }, Enumerable.Empty<string>()));
            if (factTokens <= 0L || estimated + factTokens > tokenBudget) continue;
            selected.Add(fact);
            estimated += factTokens;
        }
        selected = WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence(selected).ToList();
        while (selected.Count > 0
            && estimateTokens(RenderCanonicalProtectedFacts(selected, Enumerable.Empty<string>())) > tokenBudget)
        {
            selected.RemoveAt(0);
        }
        return selected;
    }

    public static string RenderCanonicalProtectedFacts(
        IEnumerable<WorldDiplomacyCanonicalProtectedFact> protectedFacts,
        IEnumerable<string> preservedResultSourceIds)
    {
        List<WorldDiplomacyCanonicalProtectedFact> facts = WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence((protectedFacts ?? Enumerable.Empty<WorldDiplomacyCanonicalProtectedFact>())
                .Where(x => x != null)).ToList();
        HashSet<string> exactResultIds = new HashSet<string>(facts.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        List<string> legacyResultIds = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList((preservedResultSourceIds ?? Enumerable.Empty<string>())
            .Where(x => !exactResultIds.Contains(x)));
        if (facts.Count == 0 && legacyResultIds.Count == 0) return "";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("【确定性保留的外交硬事实；压缩摘要不得覆盖】");
        foreach (WorldDiplomacyCanonicalProtectedFact fact in facts)
        {
            if (string.Equals(fact.Kind, "response_link", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("[kind=response_link|source=").Append(fact.SourceId)
                    .Append("|responding_to=").Append(fact.RelatedSourceId)
                    .Append("|date=").Append(fact.GameDate ?? "")
                    .Append("|author=").Append(fact.AuthorKingdomId ?? "")
                    .Append("|targets=").Append(string.Join(",", fact.TargetKingdomIds ?? new List<string>()))
                    .Append("|intent=").Append(fact.Intent ?? "")
                    .Append("|commitment=").Append(fact.Commitment ?? "").AppendLine("]");
                continue;
            }
            sb.Append("[kind=diplomatic_result|source=").Append(fact.SourceId)
                .Append("|date=").Append(fact.GameDate ?? "")
                .Append("|author=").Append(fact.AuthorKingdomId ?? "")
                .Append("|targets=").Append(string.Join(",", fact.TargetKingdomIds ?? new List<string>()))
                .Append("|intent=").Append(fact.Intent ?? "")
                .Append("|commitment=").Append(fact.Commitment ?? "")
                .AppendLine("|verified=true]");
            sb.AppendLine(fact.Text ?? "");
        }
        foreach (string sourceId in legacyResultIds)
        {
            sb.Append("[kind=diplomatic_result_manifest|source=").Append(sourceId)
                .AppendLine("|verified=true|detail_in_compressed_summary=true]");
        }
        return sb.ToString().TrimEnd();
    }

    public static string RenderCanonicalSnapshotPayload(WorldDiplomacyCanonicalHistorySnapshot snapshot)
    {
        if (snapshot == null) return "";
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(snapshot.Content)) sb.AppendLine(snapshot.Content.Trim());
        string protectedFacts = RenderCanonicalProtectedFacts(snapshot.ProtectedFacts, snapshot.PreservedResultSourceIds);
        if (!string.IsNullOrWhiteSpace(protectedFacts)) sb.AppendLine(protectedFacts);
        return sb.ToString().TrimEnd();
    }
    }
}
