using System;
using System.Collections.Generic;
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
