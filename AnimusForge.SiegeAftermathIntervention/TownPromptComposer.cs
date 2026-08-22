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
        var lines = new List<string>();
        for (int index = 0; index < record.RulerMemories.Count; index++)
        {
            SettlementRuleMemoryEntry entry = record.RulerMemories[index];
            bool isCurrent = index == 0;
            lines.Add(BuildRuleEntryFacts(record, entry, currentDay, isCurrent, text));
            if (isCurrent && !string.IsNullOrWhiteSpace(entry.RulerPersonality))
            {
                lines.Add(ApplyTemplate(
                    text.SettlementRuleMemoryPersonalityTemplate,
                    "personality",
                    entry.RulerPersonality.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(entry.Narrative))
            {
                string narrativeTemplate = isCurrent
                    ? text.SettlementRuleMemoryNarrativeTemplate
                    : text.SettlementRuleMemoryPreviousNarrativeTemplate;
                string narrative = ApplyTemplate(narrativeTemplate, "ruler", ResolveRulerName(entry, text));
                lines.Add(ApplyTemplate(narrative, "narrative", entry.Narrative.Trim()));
            }
        }
        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildSettlementRuleMemoryEncyclopediaText(
        SettlementRuleMemoryRecord record,
        int currentDay,
        bool generationPending,
        TownPromptTextCatalog textCatalog)
    {
        if (record == null || record.CurrentRule == null)
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        var sections = new List<string> { text.SettlementRuleMemoryEncyclopediaHeader };
        for (int index = 0; index < record.RulerMemories.Count; index++)
        {
            SettlementRuleMemoryEntry entry = record.RulerMemories[index];
            bool isCurrent = index == 0;
            string template = isCurrent
                ? text.SettlementRuleMemoryEncyclopediaCurrentTemplate
                : text.SettlementRuleMemoryEncyclopediaPreviousTemplate;
            string section = ApplyRuleEntryTemplate(template, record, entry, currentDay, isCurrent, text);
            string narrative = entry.Narrative.Trim();
            if (isCurrent && string.IsNullOrWhiteSpace(narrative) && generationPending)
            {
                narrative = text.SettlementRuleMemoryEncyclopediaGenerating;
            }
            section = ApplyTemplate(section, "narrative", narrative);
            sections.Add(section.Trim());
        }
        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    public static SettlementRuleMemoryGenerationPrompt BuildSettlementRuleMemoryGenerationPrompt(
        SettlementRuleMemoryRecord record,
        int currentDay,
        TownPromptTextCatalog textCatalog)
    {
        if (record == null || record.CurrentRule == null)
        {
            return new SettlementRuleMemoryGenerationPrompt(string.Empty, string.Empty);
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        SettlementRuleMemoryEntry entry = record.CurrentRule;
        string userPrompt = ApplyRuleEntryTemplate(
            text.SettlementRuleMemoryGenerationUserTemplate,
            record,
            entry,
            currentDay,
            true,
            text);
        userPrompt = ApplyTemplate(
            userPrompt,
            "personality",
            NormalizeRuleMemoryValue(entry.RulerPersonality, string.Empty, text.SettlementRuleMemoryUnknownPersonality));
        userPrompt = userPrompt.Trim()
            + Environment.NewLine
            + Environment.NewLine
            + text.SettlementRuleMemoryGenerationOutputProtocol.Trim();
        return new SettlementRuleMemoryGenerationPrompt(
            text.SettlementRuleMemoryGenerationSystemPrompt.Trim(),
            userPrompt);
    }

    public static string BuildSettlementRuleMemoryDeveloperEntryText(
        SettlementRuleMemoryRecord record,
        int entryIndex,
        int currentDay,
        TownPromptTextCatalog textCatalog)
    {
        if (record == null || entryIndex < 0 || entryIndex >= record.RulerMemories.Count)
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        SettlementRuleMemoryEntry entry = record.RulerMemories[entryIndex];
        return ApplyRuleEntryTemplate(
            text.SettlementRuleMemoryDeveloperEntryTemplate,
            record,
            entry,
            currentDay,
            entryIndex == 0,
            text).Trim();
    }

    public static string BuildTownOperationLedgerContext(
        TownOperationLedgerSnapshot snapshot,
        TownPromptTextCatalog textCatalog)
    {
        if (snapshot == null || snapshot.State == TownOperationState.None)
        {
            return string.Empty;
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        if (snapshot.Kind == TownOperationKind.Massacre || snapshot.Kind == TownOperationKind.Colonization)
        {
            string massacreState = snapshot.State == TownOperationState.Stopped
                ? text.MassacreLedgerStoppedState
                : snapshot.State == TownOperationState.Completed
                    ? text.MassacreLedgerCompletedState
                    : text.MassacreLedgerActiveState;
            string victimProgress = (snapshot.VictimProgressBasisPoints / 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%";
            string massacreResult = text.MassacreLedgerContextTemplate;
            massacreResult = ApplyTemplate(massacreResult, "state", massacreState);
            massacreResult = ApplyTemplate(massacreResult, "killed_count", snapshot.KilledVictimCount.ToString(CultureInfo.InvariantCulture));
            massacreResult = ApplyTemplate(massacreResult, "captured_count", snapshot.CapturedVictimCount.ToString(CultureInfo.InvariantCulture));
            massacreResult = ApplyTemplate(massacreResult, "civilian_deaths", snapshot.KilledOrdinaryCivilianCount.ToString(CultureInfo.InvariantCulture));
            massacreResult = ApplyTemplate(massacreResult, "notable_deaths", snapshot.KilledNotableCount.ToString(CultureInfo.InvariantCulture));
            massacreResult = ApplyTemplate(massacreResult, "progress", victimProgress);
            return massacreResult.Trim();
        }
        if (snapshot.Kind != TownOperationKind.Plunder)
        {
            return string.Empty;
        }

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

    private static string BuildRuleEntryFacts(
        SettlementRuleMemoryRecord record,
        SettlementRuleMemoryEntry entry,
        int currentDay,
        bool isCurrent,
        TownPromptTextCatalog text)
    {
        string template = isCurrent
            ? text.SettlementRuleMemoryCurrentTemplate
            : text.SettlementRuleMemoryPreviousTemplate;
        return ApplyRuleEntryTemplate(template, record, entry, currentDay, isCurrent, text);
    }

    private static string ApplyRuleEntryTemplate(
        string template,
        SettlementRuleMemoryRecord record,
        SettlementRuleMemoryEntry entry,
        int currentDay,
        bool isCurrent,
        TownPromptTextCatalog text)
    {
        int elapsed = Math.Max(0, Math.Max(0, currentDay) - entry.RuleStartDay);
        bool usesMinimum = isCurrent
            ? entry.MinimumRuleDurationDays > elapsed
            : entry.DurationWasMinimum;
        string result = ApplyTemplate(template, "ruler", ResolveRulerName(entry, text));
        result = ApplyTemplate(
            result,
            "settlement",
            NormalizeRuleMemoryValue(record.SettlementName, record.SettlementId));
        result = ApplyTemplate(
            result,
            "culture",
            NormalizeRuleMemoryValue(entry.CultureName, entry.CultureId, text.SettlementRuleMemoryUnknownCulture));
        return ApplyTemplate(
            result,
            "duration",
            FormatRuleDuration(
                SettlementRuleMemoryStore.GetEffectiveRuleDurationDays(entry, currentDay, isCurrent),
                usesMinimum,
                text));
    }

    private static string ResolveRulerName(SettlementRuleMemoryEntry entry, TownPromptTextCatalog text)
    {
        return NormalizeRuleMemoryValue(
            entry?.RulerName,
            entry?.RulerId,
            text.SettlementRuleMemoryUnknownRuler);
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
