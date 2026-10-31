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
        => RenderCanonicalHistoryEntry(entry, null);

    public static string RenderCanonicalHistoryEntry(WorldDiplomacyCanonicalHistoryEntry entry, Func<string, string, string> localize)
    {
        if (entry == null) return "";
        StringBuilder sb = new StringBuilder();
        sb.Append("[seq=").Append(entry.Sequence.ToString(CultureInfo.InvariantCulture))
            .Append("|kind=").Append(entry.Kind ?? "")
            .Append("|date=").Append(DiplomacyPromptTexts.ProjectCalendarDate(entry.GameDate, localize) ?? "")
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
        string text = entry.Text ?? "";
        if (localize != null)
        {
            text = ProjectCanonicalEntryText(entry.Kind, text, localize);
        }
        sb.Append(text);
        return sb.ToString().TrimEnd();
    }

    private static string ProjectCanonicalEntryText(string kind, string text, Func<string, string, string> localize)
    {
        if (string.IsNullOrWhiteSpace(text) || localize == null) return text ?? string.Empty;
        if (string.Equals(kind, "policy_published", StringComparison.OrdinalIgnoreCase) || string.Equals(kind, "policy_snapshot", StringComparison.OrdinalIgnoreCase))
        {
            // Stored prose may itself quote these labels. Only project known structural
            // positions; never rewrite the policy title, body, impact or effect values.
            int newline = text.IndexOf('\n');
            if (newline < 0) return text;
            string header = ProjectPolicyHeader(text.Substring(0, newline), localize) + "\n";
            string body = ProjectPrefix(text.Substring(newline + 1), "Render_Prefix_PolicyState",
                "HistoryProjection_PolicyStatus", "Policy status=", localize);
            return header + body;
        }
        if (string.Equals(kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectPrefix(text, "Render_Prefix_Confirmed", "HistoryProjection_Confirmed",
                "[Confirmed by game mechanics] ", localize);
        }
        if (string.Equals(kind, "world_weekly", StringComparison.OrdinalIgnoreCase))
        {
            // The correction prefix is checked first: the Chinese weekly prefix is a prefix of it.
            return TryProjectPrefix(text, "Render_Prefix_WeeklyCorrection", "HistoryProjection_WeeklyCorrection",
                    "[World Bulletin Final Edition (Correction)]", localize, out string corrected)
                ? corrected
                : ProjectPrefix(text, "Render_Prefix_Weekly", "HistoryProjection_Weekly",
                    "[World Bulletin Final Edition]", localize);
        }
        return text;
    }

    // Stored text was written in the game language of its time; accept the prefix in either language.
    private static string ProjectPrefix(string text, string sourceId, string key, string fallback,
        Func<string, string, string> localize)
        => TryProjectPrefix(text, sourceId, key, fallback, localize, out string projected) ? projected : text;

    private static bool TryProjectPrefix(string text, string sourceId, string key, string fallback,
        Func<string, string, string> localize, out string projected)
    {
        foreach (string source in DiplomacyCompatibilityTexts.Variants(sourceId))
        {
            if (source.Length != 0 && text.StartsWith(source, StringComparison.Ordinal))
            {
                projected = localize(key, fallback) + text.Substring(source.Length);
                return true;
            }
        }
        projected = text;
        return false;
    }

    // The publication owner emits this first-line schema from artifact fields, in the game language of
    // the time it was written; each language's labels are parsed as a set (Chinese first).
    // Ambiguous/legacy headers retain their original metadata; never parse the body.
    private static string ProjectPolicyHeader(string header, Func<string, string, string> localize)
    {
        string[] openings = DiplomacyCompatibilityTexts.Variants("Render_PolicyOpening");
        string[] issuerLabels = DiplomacyCompatibilityTexts.Variants("Render_IssuerLabel");
        string[] scopeLabels = DiplomacyCompatibilityTexts.Variants("Render_ScopeLabel");
        int sets = Math.Max(openings.Length, Math.Max(issuerLabels.Length, scopeLabels.Length));
        for (int i = 0; i < sets; i++)
        {
            string opening = openings[Math.Min(i, openings.Length - 1)];
            if (opening.Length == 0 || !header.StartsWith(opening, StringComparison.Ordinal)) continue;
            return ProjectPolicyHeader(header, opening, issuerLabels[Math.Min(i, issuerLabels.Length - 1)],
                scopeLabels[Math.Min(i, scopeLabels.Length - 1)], localize);
        }
        return header;
    }

    private static string ProjectPolicyHeader(string header, string opening, string issuerLabel, string scopeLabel,
        Func<string, string, string> localize)
    {
        string headingOnly = localize("HistoryProjection_Policy", "Policy \u300A") + header.Substring(opening.Length);
        int closing = header.IndexOf('\u300B', opening.Length);
        if (closing < 0 || closing != header.LastIndexOf('\u300B')) return headingOnly;
        string suffix = header.Substring(closing + 1);
        string issuer = null, scope = null;
        if (suffix.StartsWith(issuerLabel, StringComparison.Ordinal))
        {
            string values = suffix.Substring(issuerLabel.Length);
            if (values.IndexOf(issuerLabel, StringComparison.Ordinal) >= 0) return headingOnly;
            int scopeAt = values.IndexOf(scopeLabel, StringComparison.Ordinal);
            issuer = scopeAt < 0 ? values : values.Substring(0, scopeAt);
            if (scopeAt >= 0) scope = values.Substring(scopeAt + scopeLabel.Length);
        }
        else if (suffix.StartsWith(scopeLabel, StringComparison.Ordinal)) scope = suffix.Substring(scopeLabel.Length);
        else if (suffix.Length != 0) return headingOnly;
        if ((issuer != null && issuer.Length == 0) || (scope != null && (scope.Length == 0
            || scope.IndexOf(scopeLabel, StringComparison.Ordinal) >= 0 || scope.IndexOf(issuerLabel, StringComparison.Ordinal) >= 0))) return headingOnly;
        // Preserve every dynamic value and stable scope token; only labels use the package.
        var output = new StringBuilder();
        output.Append(localize("HistoryProjection_Policy", "Policy \u300A"))
            .Append(header, opening.Length, closing - opening.Length + 1);
        if (issuer != null) output.Append(localize("HistoryProjection_PolicyIssuer", "; issuing kingdom=")).Append(issuer);
        if (scope != null) output.Append(localize("HistoryProjection_PolicyScope", "; scope=")).Append(scope);
        return output.ToString();
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
        IEnumerable<string> preservedResultSourceIds, Func<string, string, string> localize = null)
    {
        List<WorldDiplomacyCanonicalProtectedFact> facts = WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence((protectedFacts ?? Enumerable.Empty<WorldDiplomacyCanonicalProtectedFact>())
                .Where(x => x != null)).ToList();
        HashSet<string> exactResultIds = new HashSet<string>(facts.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        List<string> legacyResultIds = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList((preservedResultSourceIds ?? Enumerable.Empty<string>())
            .Where(x => !exactResultIds.Contains(x)));
        if (facts.Count == 0 && legacyResultIds.Count == 0) return "";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryPrompt_005", "[Deterministically preserved diplomatic hard facts; compressed summaries must not override them]"));
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
            sb.AppendLine(ProjectCanonicalEntryText("diplomatic_result", fact.Text, localize));
        }
        foreach (string sourceId in legacyResultIds)
        {
            sb.Append("[kind=diplomatic_result_manifest|source=").Append(sourceId)
                .AppendLine("|verified=true|detail_in_compressed_summary=true]");
        }
        return sb.ToString().TrimEnd();
    }

    public static string RenderCanonicalSnapshotPayload(WorldDiplomacyCanonicalHistorySnapshot snapshot, Func<string, string, string> localize = null)
    {
        if (snapshot == null) return "";
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(snapshot.Content)) sb.AppendLine(snapshot.Content.Trim());
        string protectedFacts = RenderCanonicalProtectedFacts(snapshot.ProtectedFacts, snapshot.PreservedResultSourceIds, localize);
        if (!string.IsNullOrWhiteSpace(protectedFacts)) sb.AppendLine(protectedFacts);
        return sb.ToString().TrimEnd();
    }
    }
}
