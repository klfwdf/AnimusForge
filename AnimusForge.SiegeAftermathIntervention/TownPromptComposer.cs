using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Composes short, fixed-order town prompts for low-attention language models.
/// All localized wording is supplied by TownPromptTextCatalog.
/// </summary>
public static class TownPromptComposer
{
    public static string BuildMainPrompt(
        SiegeRuntimePromptFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        facts ??= SiegeRuntimePromptFacts.Empty;
        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        TownDialogueRole role = TownDialogueRoleClassifier.NormalizeForRuntime(facts.DialogueRole);
        var prompt = new StringBuilder();

        AppendSection(
            prompt,
            text.SceneSectionTitle,
            ApplyTemplate(text.SceneSummaryTemplate, "settlement", NormalizeSettlementName(facts.SettlementName)));

        string roleInstruction = text.RoleInstructions.TryGetValue(role.ToString(), out string configuredRoleInstruction)
            ? configuredRoleInstruction
            : text.RoleInstructions[TownDialogueRoleClassifier.SafeFallbackRole.ToString()];
        var roleLines = new List<string>
        {
            TownDialogueRoleContextProfile.Build(role),
            roleInstruction,
        };
        if (facts.IsAlliedSoldier)
        {
            roleLines.Add(text.AlliedSoldierState);
        }
        else if (facts.IsGuardOrSoldier)
        {
            roleLines.Add(text.DefeatedGuardState);
        }
        else if (facts.IsCivilian)
        {
            roleLines.Add(text.CivilianState);
        }
        AppendSection(prompt, text.RoleSectionTitle, roleLines);

        var memoryLines = new List<string>
        {
            TownDialogueMemoryPolicy.ResolveScope(role) == TownDialogueMemoryScope.PersistentPersonal
                ? text.PersistentMemoryInstruction
                : text.SceneLocalMemoryInstruction,
        };
        AppendIfPresent(memoryLines, facts.MemoryContext);
        AppendIfPresent(memoryLines, facts.GatherContext);
        if (memoryLines.Count == 1)
        {
            memoryLines.Add(text.NoAdditionalMemory);
        }
        AppendSection(prompt, text.MemorySectionTitle, memoryLines);

        var stateLines = new List<string>
        {
            facts.MassacreStarted
                ? text.MassacreState
                : (facts.PlunderStarted ? text.PlunderState : text.DefaultState),
            ApplyTemplate(text.SharedReliefTemplate, "relief_pool", NormalizeReliefPool(facts.SharedReliefPoolDescription)),
        };
        if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied)
        {
            stateLines.Add(text.SoldierAppeasementPendingState);
        }
        AppendSection(prompt, text.StateSectionTitle, stateLines);

        AppendSection(prompt, text.CandidateActionsSectionTitle, text.CandidateActionsInstruction);
        AppendSection(prompt, text.ForbiddenActionsSectionTitle, text.ForbiddenActionsInstruction);
        AppendSection(prompt, text.ReplyRequirementsSectionTitle, text.ReplyRequirementsInstruction);
        AppendSection(prompt, text.MainOutputProtocolSectionTitle, text.MainOutputProtocol);
        return prompt.ToString().Trim();
    }

    public static string BuildPostprocessContract(
        IEnumerable<string> eligibleTags,
        TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        var prompt = new StringBuilder();
        AppendSection(prompt, text.PostprocessContractTitle, text.PostprocessDecisionInstruction);

        var candidateLines = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawTag in eligibleTags ?? Array.Empty<string>())
        {
            string tag = (rawTag ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(tag) && seen.Add(tag))
            {
                candidateLines.Add(ApplyTemplate(text.CandidateTagTemplate, "tag", tag));
            }
        }
        if (candidateLines.Count == 0)
        {
            candidateLines.Add(text.NoCandidateTags);
        }
        AppendSection(prompt, text.PostprocessCandidateTitle, candidateLines);
        AppendSection(prompt, text.PostprocessPositiveExamplesTitle, text.PostprocessPositiveExamples);
        AppendSection(prompt, text.PostprocessNegativeExamplesTitle, text.PostprocessNegativeExamples);
        AppendSection(prompt, text.PostprocessOutputProtocolSectionTitle, text.PostprocessOutputProtocol);
        return prompt.ToString().Trim();
    }

    public static string BuildPostprocessContext(
        SiegePostprocessContextFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        var prompt = new StringBuilder();

        string scene = ApplyTemplate(
            text.PostprocessSceneTemplate,
            "settlement",
            string.IsNullOrWhiteSpace(facts.SettlementName)
                ? SiegePostprocessContextBuilder.DefaultSettlementName
                : facts.SettlementName.Trim());
        AppendSection(prompt, text.SceneSectionTitle, scene);

        string role = text.PostprocessRoleTemplate;
        role = ApplyTemplate(role, "role_marker", TownDialogueRoleContextProfile.Build(facts.DialogueRole));
        role = ApplyTemplate(role, "speaker", string.IsNullOrWhiteSpace(facts.SpeakerName) ? SiegePostprocessContextBuilder.DefaultSpeakerName : facts.SpeakerName.Trim());
        role = ApplyTemplate(role, "identity", string.IsNullOrWhiteSpace(facts.SpeakerIdentity) ? SiegePostprocessContextBuilder.DefaultSpeakerIdentity : facts.SpeakerIdentity.Trim());
        role = ApplyTemplate(role, "agent_index", facts.TargetAgentIndex.ToString());
        role = ApplyTemplate(role, "direct_reply", facts.ReplyIsDirectPlayerResponse ? text.PostprocessDirectReplyTrue : text.PostprocessDirectReplyFalse);
        AppendSection(prompt, text.RoleSectionTitle, role);

        var memoryLines = new List<string>();
        AppendIfPresent(memoryLines, facts.InterventionMemoryContext);
        AppendIfPresent(memoryLines, facts.CivilianGatherContext);
        if (memoryLines.Count == 0)
        {
            memoryLines.Add(text.PostprocessNoMemory);
        }
        AppendSection(prompt, text.MemorySectionTitle, memoryLines);

        string state = text.PostprocessStateTemplate;
        state = ApplyTemplate(state, "outcome", string.IsNullOrWhiteSpace(facts.CurrentOutcome) ? "none" : facts.CurrentOutcome.Trim());
        state = ApplyTemplate(state, "relief_pool", NormalizeReliefPool(facts.SharedReliefPoolDescription));
        state = ApplyTemplate(state, "destructive_allowed", facts.DestructiveAllowed ? "true" : "false");
        AppendSection(
            prompt,
            text.StateSectionTitle,
            new[] { state, text.PostprocessSharedReliefRule, text.PostprocessTransitionRule });
        return prompt.ToString().Trim();
    }

    public static string BuildSettlementRuleMemoryContext(
        SettlementRuleMemoryRecord record,
        int currentDay,
        TownPromptTextCatalog textCatalog)
    {
        if (record == null)
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        int elapsedDays = Math.Max(0, Math.Max(0, currentDay) - Math.Max(0, record.RuleStartDay));
        bool durationUsesMinimum = record.MinimumRuleDurationDays > elapsedDays;
        string currentDuration = FormatRuleDuration(
            SettlementRuleMemoryStore.GetEffectiveRuleDurationDays(record, currentDay),
            durationUsesMinimum,
            text);
        string current = text.SettlementRuleMemoryCurrentTemplate;
        current = ApplyTemplate(current, "ruler", NormalizeRuleMemoryValue(record.RulerName, record.RulerId, text.SettlementRuleMemoryUnknownRuler));
        current = ApplyTemplate(current, "settlement", NormalizeRuleMemoryValue(record.SettlementName, record.SettlementId));
        current = ApplyTemplate(current, "culture", NormalizeRuleMemoryValue(record.CultureName, record.CultureId, text.SettlementRuleMemoryUnknownCulture));
        current = ApplyTemplate(current, "duration", currentDuration);

        var lines = new List<string> { current };
        if (!string.IsNullOrWhiteSpace(record.RulerPersonality))
        {
            lines.Add(ApplyTemplate(
                text.SettlementRuleMemoryPersonalityTemplate,
                "personality",
                record.RulerPersonality.Trim()));
        }

        if (record.HasPreviousRule)
        {
            bool cultureOnlyTransition = IsSameRuleMemoryValue(
                record.RulerId,
                record.RulerName,
                record.PreviousRulerId,
                record.PreviousRulerName);
            string previous = cultureOnlyTransition
                ? text.SettlementRuleMemoryPreviousCultureTemplate
                : text.SettlementRuleMemoryPreviousTemplate;
            previous = ApplyTemplate(previous, "ruler", NormalizeRuleMemoryValue(record.PreviousRulerName, record.PreviousRulerId, text.SettlementRuleMemoryUnknownRuler));
            previous = ApplyTemplate(previous, "culture", NormalizeRuleMemoryValue(record.PreviousCultureName, record.PreviousCultureId, text.SettlementRuleMemoryUnknownCulture));
            previous = ApplyTemplate(
                previous,
                "duration",
                FormatRuleDuration(record.PreviousRuleDurationDays, record.PreviousDurationWasMinimum, text));
            lines.Add(previous);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildTownOperationLedgerContext(
        TownOperationLedgerSnapshot snapshot,
        TownPromptTextCatalog textCatalog)
    {
        if (snapshot == null
            || snapshot.Kind != TownOperationKind.Plunder
            || snapshot.State == TownOperationState.None)
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        string state = snapshot.State == TownOperationState.Stopped
            ? text.PlunderLedgerStoppedState
            : snapshot.State == TownOperationState.Completed
                ? text.PlunderLedgerCompletedState
                : text.PlunderLedgerActiveState;
        string progress = (snapshot.ProgressBasisPoints / 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%";
        string result = text.PlunderLedgerContextTemplate;
        result = ApplyTemplate(result, "state", state);
        result = ApplyTemplate(result, "acquired_value", snapshot.AcquiredValue.ToString(CultureInfo.InvariantCulture));
        result = ApplyTemplate(result, "available_value", snapshot.TotalAvailableValue.ToString(CultureInfo.InvariantCulture));
        result = ApplyTemplate(result, "progress", progress);
        result = ApplyTemplate(result, "merchant_count", snapshot.MerchantTargetCount.ToString(CultureInfo.InvariantCulture));
        result = ApplyTemplate(result, "notable_count", snapshot.NotableTargetCount.ToString(CultureInfo.InvariantCulture));
        result = ApplyTemplate(result, "civilian_count", snapshot.CivilianTargetCount.ToString(CultureInfo.InvariantCulture));
        result = ApplyTemplate(result, "gold", snapshot.AcquiredGold.ToString(CultureInfo.InvariantCulture));
        result = ApplyTemplate(result, "item_value", snapshot.AcquiredItemValue.ToString(CultureInfo.InvariantCulture));
        return result.Trim();
    }

    private static void AppendSection(StringBuilder prompt, string title, string content)
    {
        AppendSection(prompt, title, new[] { content });
    }

    private static void AppendSection(StringBuilder prompt, string title, IEnumerable<string> lines)
    {
        if (prompt.Length > 0)
        {
            prompt.AppendLine();
        }
        prompt.AppendLine((title ?? string.Empty).Trim());
        foreach (string rawLine in lines ?? Array.Empty<string>())
        {
            string line = (rawLine ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(line))
            {
                prompt.AppendLine(line);
            }
        }
    }

    private static void AppendIfPresent(List<string> lines, string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            lines.Add(normalized);
        }
    }

    private static string ApplyTemplate(string template, string key, string value)
    {
        return (template ?? string.Empty).Replace("{" + key + "}", value ?? string.Empty);
    }

    private static string FormatRuleDuration(int durationDays, bool usesMinimum, TownPromptTextCatalog text)
    {
        if (usesMinimum)
        {
            return text.SettlementRuleMemoryDurationAtLeastTwoYears;
        }

        int days = Math.Max(0, durationDays);
        if (days == 0)
        {
            return text.SettlementRuleMemoryDurationLessThanDay;
        }
        if (days < 7)
        {
            return ApplyTemplate(text.SettlementRuleMemoryDurationDaysTemplate, "value", days.ToString(CultureInfo.InvariantCulture));
        }
        if (days < SettlementRuleMemoryStore.MinimumFallbackRuleDays)
        {
            int weeks = Math.Max(1, days / 7);
            return ApplyTemplate(text.SettlementRuleMemoryDurationWeeksTemplate, "value", weeks.ToString(CultureInfo.InvariantCulture));
        }

        decimal years = Math.Round(days / 84m, 1, MidpointRounding.AwayFromZero);
        return ApplyTemplate(
            text.SettlementRuleMemoryDurationYearsTemplate,
            "value",
            years.ToString("0.#", CultureInfo.InvariantCulture));
    }

    private static string NormalizeRuleMemoryValue(string primary, string fallback, string unknown = "")
    {
        string normalized = (primary ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }
        string fallbackValue = (fallback ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(fallbackValue) ? (unknown ?? string.Empty).Trim() : fallbackValue;
    }

    private static bool IsSameRuleMemoryValue(
        string currentId,
        string currentName,
        string previousId,
        string previousName)
    {
        string normalizedCurrentId = (currentId ?? string.Empty).Trim();
        string normalizedPreviousId = (previousId ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalizedCurrentId) || !string.IsNullOrWhiteSpace(normalizedPreviousId))
        {
            return string.Equals(normalizedCurrentId, normalizedPreviousId, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            (currentName ?? string.Empty).Trim(),
            (previousName ?? string.Empty).Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSettlementName(string settlementName)
    {
        string normalized = (settlementName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? SiegeRuntimePromptProfile.DefaultSettlementName
            : normalized;
    }

    private static string NormalizeReliefPool(string reliefPool)
    {
        string normalized = (reliefPool ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "none" : normalized;
    }
}
