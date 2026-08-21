using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Localized text consumed by the GCCZ town prompt composer.
/// Runtime adapters load this model from ModuleData and the core supplies an English fail-safe.
/// </summary>
public sealed class TownPromptTextCatalog
{
    public int Version { get; set; }

    public string SceneSectionTitle { get; set; }

    public string SceneSummaryTemplate { get; set; }

    public string RoleSectionTitle { get; set; }

    public Dictionary<string, string> RoleInstructions { get; set; }

    public string AlliedSoldierState { get; set; }

    public string DefeatedGuardState { get; set; }

    public string CivilianState { get; set; }

    public string MemorySectionTitle { get; set; }

    public string PersistentMemoryInstruction { get; set; }

    public string SceneLocalMemoryInstruction { get; set; }

    public string NoAdditionalMemory { get; set; }

    public string SettlementRuleMemoryCurrentTemplate { get; set; }

    public string SettlementRuleMemoryPersonalityTemplate { get; set; }

    public string SettlementRuleMemoryPreviousTemplate { get; set; }

    public string SettlementRuleMemoryPreviousCultureTemplate { get; set; }

    public string SettlementRuleMemoryUnknownRuler { get; set; }

    public string SettlementRuleMemoryUnknownCulture { get; set; }

    public string SettlementRuleMemoryDurationLessThanDay { get; set; }

    public string SettlementRuleMemoryDurationAtLeastTwoYears { get; set; }

    public string SettlementRuleMemoryDurationDaysTemplate { get; set; }

    public string SettlementRuleMemoryDurationWeeksTemplate { get; set; }

    public string SettlementRuleMemoryDurationYearsTemplate { get; set; }

    public string StateSectionTitle { get; set; }

    public string DefaultState { get; set; }

    public string PlunderState { get; set; }

    public string MassacreState { get; set; }

    public string SharedReliefTemplate { get; set; }

    public string SoldierAppeasementPendingState { get; set; }

    public string CandidateActionsSectionTitle { get; set; }

    public string CandidateActionsInstruction { get; set; }

    public string ForbiddenActionsSectionTitle { get; set; }

    public string ForbiddenActionsInstruction { get; set; }

    public string ReplyRequirementsSectionTitle { get; set; }

    public string ReplyRequirementsInstruction { get; set; }

    public string MainOutputProtocolSectionTitle { get; set; }

    public string MainOutputProtocol { get; set; }

    public string PostprocessSceneTemplate { get; set; }

    public string PostprocessRoleTemplate { get; set; }

    public string PostprocessDirectReplyTrue { get; set; }

    public string PostprocessDirectReplyFalse { get; set; }

    public string PostprocessNoMemory { get; set; }

    public string PostprocessStateTemplate { get; set; }

    public string PostprocessSharedReliefRule { get; set; }

    public string PostprocessTransitionRule { get; set; }

    public string PostprocessContractTitle { get; set; }

    public string PostprocessDecisionInstruction { get; set; }

    public string PostprocessCandidateTitle { get; set; }

    public string CandidateTagTemplate { get; set; }

    public string NoCandidateTags { get; set; }

    public string PostprocessPositiveExamplesTitle { get; set; }

    public string PostprocessPositiveExamples { get; set; }

    public string PostprocessNegativeExamplesTitle { get; set; }

    public string PostprocessNegativeExamples { get; set; }

    public string PostprocessOutputProtocolSectionTitle { get; set; }

    public string PostprocessOutputProtocol { get; set; }

    public static TownPromptTextCatalog Resolve(TownPromptTextCatalog source)
    {
        TownPromptTextCatalog fallback = CreateEnglishFallback();
        if (source == null)
        {
            return fallback;
        }

        return new TownPromptTextCatalog
        {
            Version = source.Version > 0 ? source.Version : fallback.Version,
            SceneSectionTitle = Pick(source.SceneSectionTitle, fallback.SceneSectionTitle),
            SceneSummaryTemplate = Pick(source.SceneSummaryTemplate, fallback.SceneSummaryTemplate),
            RoleSectionTitle = Pick(source.RoleSectionTitle, fallback.RoleSectionTitle),
            RoleInstructions = ResolveRoleInstructions(source.RoleInstructions, fallback.RoleInstructions),
            AlliedSoldierState = Pick(source.AlliedSoldierState, fallback.AlliedSoldierState),
            DefeatedGuardState = Pick(source.DefeatedGuardState, fallback.DefeatedGuardState),
            CivilianState = Pick(source.CivilianState, fallback.CivilianState),
            MemorySectionTitle = Pick(source.MemorySectionTitle, fallback.MemorySectionTitle),
            PersistentMemoryInstruction = Pick(source.PersistentMemoryInstruction, fallback.PersistentMemoryInstruction),
            SceneLocalMemoryInstruction = Pick(source.SceneLocalMemoryInstruction, fallback.SceneLocalMemoryInstruction),
            NoAdditionalMemory = Pick(source.NoAdditionalMemory, fallback.NoAdditionalMemory),
            SettlementRuleMemoryCurrentTemplate = Pick(source.SettlementRuleMemoryCurrentTemplate, fallback.SettlementRuleMemoryCurrentTemplate),
            SettlementRuleMemoryPersonalityTemplate = Pick(source.SettlementRuleMemoryPersonalityTemplate, fallback.SettlementRuleMemoryPersonalityTemplate),
            SettlementRuleMemoryPreviousTemplate = Pick(source.SettlementRuleMemoryPreviousTemplate, fallback.SettlementRuleMemoryPreviousTemplate),
            SettlementRuleMemoryPreviousCultureTemplate = Pick(source.SettlementRuleMemoryPreviousCultureTemplate, fallback.SettlementRuleMemoryPreviousCultureTemplate),
            SettlementRuleMemoryUnknownRuler = Pick(source.SettlementRuleMemoryUnknownRuler, fallback.SettlementRuleMemoryUnknownRuler),
            SettlementRuleMemoryUnknownCulture = Pick(source.SettlementRuleMemoryUnknownCulture, fallback.SettlementRuleMemoryUnknownCulture),
            SettlementRuleMemoryDurationLessThanDay = Pick(source.SettlementRuleMemoryDurationLessThanDay, fallback.SettlementRuleMemoryDurationLessThanDay),
            SettlementRuleMemoryDurationAtLeastTwoYears = Pick(source.SettlementRuleMemoryDurationAtLeastTwoYears, fallback.SettlementRuleMemoryDurationAtLeastTwoYears),
            SettlementRuleMemoryDurationDaysTemplate = Pick(source.SettlementRuleMemoryDurationDaysTemplate, fallback.SettlementRuleMemoryDurationDaysTemplate),
            SettlementRuleMemoryDurationWeeksTemplate = Pick(source.SettlementRuleMemoryDurationWeeksTemplate, fallback.SettlementRuleMemoryDurationWeeksTemplate),
            SettlementRuleMemoryDurationYearsTemplate = Pick(source.SettlementRuleMemoryDurationYearsTemplate, fallback.SettlementRuleMemoryDurationYearsTemplate),
            StateSectionTitle = Pick(source.StateSectionTitle, fallback.StateSectionTitle),
            DefaultState = Pick(source.DefaultState, fallback.DefaultState),
            PlunderState = Pick(source.PlunderState, fallback.PlunderState),
            MassacreState = Pick(source.MassacreState, fallback.MassacreState),
            SharedReliefTemplate = Pick(source.SharedReliefTemplate, fallback.SharedReliefTemplate),
            SoldierAppeasementPendingState = Pick(source.SoldierAppeasementPendingState, fallback.SoldierAppeasementPendingState),
            CandidateActionsSectionTitle = Pick(source.CandidateActionsSectionTitle, fallback.CandidateActionsSectionTitle),
            CandidateActionsInstruction = Pick(source.CandidateActionsInstruction, fallback.CandidateActionsInstruction),
            ForbiddenActionsSectionTitle = Pick(source.ForbiddenActionsSectionTitle, fallback.ForbiddenActionsSectionTitle),
            ForbiddenActionsInstruction = Pick(source.ForbiddenActionsInstruction, fallback.ForbiddenActionsInstruction),
            ReplyRequirementsSectionTitle = Pick(source.ReplyRequirementsSectionTitle, fallback.ReplyRequirementsSectionTitle),
            ReplyRequirementsInstruction = Pick(source.ReplyRequirementsInstruction, fallback.ReplyRequirementsInstruction),
            MainOutputProtocolSectionTitle = Pick(source.MainOutputProtocolSectionTitle, fallback.MainOutputProtocolSectionTitle),
            MainOutputProtocol = Pick(source.MainOutputProtocol, fallback.MainOutputProtocol),
            PostprocessSceneTemplate = Pick(source.PostprocessSceneTemplate, fallback.PostprocessSceneTemplate),
            PostprocessRoleTemplate = Pick(source.PostprocessRoleTemplate, fallback.PostprocessRoleTemplate),
            PostprocessDirectReplyTrue = Pick(source.PostprocessDirectReplyTrue, fallback.PostprocessDirectReplyTrue),
            PostprocessDirectReplyFalse = Pick(source.PostprocessDirectReplyFalse, fallback.PostprocessDirectReplyFalse),
            PostprocessNoMemory = Pick(source.PostprocessNoMemory, fallback.PostprocessNoMemory),
            PostprocessStateTemplate = Pick(source.PostprocessStateTemplate, fallback.PostprocessStateTemplate),
            PostprocessSharedReliefRule = Pick(source.PostprocessSharedReliefRule, fallback.PostprocessSharedReliefRule),
            PostprocessTransitionRule = Pick(source.PostprocessTransitionRule, fallback.PostprocessTransitionRule),
            PostprocessContractTitle = Pick(source.PostprocessContractTitle, fallback.PostprocessContractTitle),
            PostprocessDecisionInstruction = Pick(source.PostprocessDecisionInstruction, fallback.PostprocessDecisionInstruction),
            PostprocessCandidateTitle = Pick(source.PostprocessCandidateTitle, fallback.PostprocessCandidateTitle),
            CandidateTagTemplate = Pick(source.CandidateTagTemplate, fallback.CandidateTagTemplate),
            NoCandidateTags = Pick(source.NoCandidateTags, fallback.NoCandidateTags),
            PostprocessPositiveExamplesTitle = Pick(source.PostprocessPositiveExamplesTitle, fallback.PostprocessPositiveExamplesTitle),
            PostprocessPositiveExamples = Pick(source.PostprocessPositiveExamples, fallback.PostprocessPositiveExamples),
            PostprocessNegativeExamplesTitle = Pick(source.PostprocessNegativeExamplesTitle, fallback.PostprocessNegativeExamplesTitle),
            PostprocessNegativeExamples = Pick(source.PostprocessNegativeExamples, fallback.PostprocessNegativeExamples),
            PostprocessOutputProtocolSectionTitle = Pick(source.PostprocessOutputProtocolSectionTitle, fallback.PostprocessOutputProtocolSectionTitle),
            PostprocessOutputProtocol = Pick(source.PostprocessOutputProtocol, fallback.PostprocessOutputProtocol),
        };
    }

    public static TownPromptTextCatalog CreateEnglishFallback()
    {
        return new TownPromptTextCatalog
        {
            Version = 1,
            SceneSectionTitle = "[1. CURRENT SCENE]",
            SceneSummaryTemplate = "{settlement} was just captured by the player. Treat this as an occupied aftermath scene, not ordinary town life.",
            RoleSectionTitle = "[2. SPEAKER ROLE]",
            RoleInstructions = new Dictionary<string, string>
            {
                [TownDialogueRole.AccompanyingNoble.ToString()] = "An accompanying allied noble. React through AF personality, politics, relationship, and witnessed events.",
                [TownDialogueRole.NoblePrisoner.ToString()] = "A noble prisoner. React through dignity, fear, ransom value, and personal risk.",
                [TownDialogueRole.PlayerCompanion.ToString()] = "A player companion. React through AF personality, relationship, and shared experience.",
                [TownDialogueRole.SettlementNotable.ToString()] = "A settlement notable. React through occupation, local interests, civilian risk, and survival.",
                [TownDialogueRole.OrdinarySoldier.ToString()] = "An ordinary soldier. Use only current orders, allegiance, morale, and witnessed scene events.",
                [TownDialogueRole.OrdinaryCivilian.ToString()] = "An ordinary civilian. Use only current fear, safety, and witnessed scene events.",
            },
            AlliedSoldierState = "The player is your direct commander. Obey valid scene orders without claiming to be a prisoner or defeated guard.",
            DefeatedGuardState = "You are a defeated or disarmed guard and no longer have enforcement authority.",
            CivilianState = "You are inside the defeated settlement and may fear, negotiate, request, or comply.",
            MemorySectionTitle = "[3. VALID MEMORY]",
            PersistentMemoryInstruction = "Use named AF personal memory, but current GCCZ facts override conflicting routine impressions.",
            SceneLocalMemoryInstruction = "Use current scene memory only. Do not retain it after scene exit.",
            NoAdditionalMemory = "No additional GCCZ event memory is available. Do not invent completed actions.",
            SettlementRuleMemoryCurrentTemplate = "Town rule memory: {ruler} has governed {settlement} for {duration}; the current ruling culture is {culture}.",
            SettlementRuleMemoryPersonalityTemplate = "Ruler personality recorded by AF: {personality}.",
            SettlementRuleMemoryPreviousTemplate = "Previous rule: {ruler}, {culture} culture, for {duration}.",
            SettlementRuleMemoryPreviousCultureTemplate = "Latest culture transition: {culture} culture was replaced while {ruler} remained ruler.",
            SettlementRuleMemoryUnknownRuler = "an unrecorded ruler",
            SettlementRuleMemoryUnknownCulture = "an unrecorded culture",
            SettlementRuleMemoryDurationLessThanDay = "less than one day",
            SettlementRuleMemoryDurationAtLeastTwoYears = "at least two years",
            SettlementRuleMemoryDurationDaysTemplate = "{value} days",
            SettlementRuleMemoryDurationWeeksTemplate = "{value} weeks",
            SettlementRuleMemoryDurationYearsTemplate = "{value} years",
            StateSectionTitle = "[4. CURRENT STATE]",
            DefaultState = "No massacre state is active. Use runtime facts for all other outcomes.",
            PlunderState = "Plunder is active but has not escalated to massacre.",
            MassacreState = "Massacre is active. Do not reset survivors to ordinary peaceful behavior.",
            SharedReliefTemplate = "Shared AF relief pool: {relief_pool}.",
            SoldierAppeasementPendingState = "Soldier appeasement is pending. Discontent may be expressed, but orders remain binding.",
            CandidateActionsSectionTitle = "[5. CANDIDATE ACTIONS]",
            CandidateActionsInstruction = "Answer naturally within current role authority. Action tags are selected only by the later postprocessor.",
            ForbiddenActionsSectionTitle = "[6. FORBIDDEN ACTIONS]",
            ForbiddenActionsInstruction = "Do not emit tags, infer actions from keywords, invent scene damage, or grant soldier authority to another role.",
            ReplyRequirementsSectionTitle = "[7. REPLY REQUIREMENTS]",
            ReplyRequirementsInstruction = "Follow AF personality, role, relationship, and witnessed events before answering directly.",
            MainOutputProtocolSectionTitle = "[8. MAIN OUTPUT]",
            MainOutputProtocol = "Output natural dialogue only. Do not output analysis, rules, headings, code fences, or action tags.",
            PostprocessSceneTemplate = "Settlement: {settlement}. This request belongs only to the active GCCZ town stage.",
            PostprocessRoleTemplate = "{role_marker}\nSpeaker: {speaker}; identity={identity}; AgentIndex={agent_index}; direct player reply={direct_reply}.",
            PostprocessDirectReplyTrue = "yes",
            PostprocessDirectReplyFalse = "no; this is an NPC echo or ambient reaction",
            PostprocessNoMemory = "No additional GCCZ event memory is available.",
            PostprocessStateTemplate = "Outcome: {outcome}. Shared relief pool: {relief_pool}. Destructive runtime gate={destructive_allowed}.",
            PostprocessSharedReliefRule = "AF transfers in this stage belong to the shared civilian relief pool.",
            PostprocessTransitionRule = "Mercy may stop reversible plunder. Massacre cannot be downgraded but may escalate to colonization.",
            PostprocessContractTitle = "[GCCZ SEMANTIC ACTION DECISION]",
            PostprocessDecisionInstruction = "Use full semantics and current authority. Select zero or one primary GCCZ action from the eligible list. Never use keyword matching as the decision.",
            PostprocessCandidateTitle = "[ELIGIBLE CANDIDATES]",
            CandidateTagTemplate = "- {tag}",
            NoCandidateTags = "- No GCCZ action is eligible.",
            PostprocessPositiveExamplesTitle = "[POSITIVE EXAMPLES]",
            PostprocessPositiveExamples = "A direct eligible order accepted in the latest reply may select its listed action tag.",
            PostprocessNegativeExamplesTitle = "[NEGATIVE EXAMPLES]",
            PostprocessNegativeExamples = "Mentions, questions, hypotheticals, refusals, echoes, and unlisted tags produce no GCCZ action.",
            PostprocessOutputProtocolSectionTitle = "[FINAL MACHINE OUTPUT]",
            PostprocessOutputProtocol = "With an action: one eligible action tag, then one mood tag. Without an action: one mood tag only. No prose or extra tags.",
        };
    }

    private static Dictionary<string, string> ResolveRoleInstructions(
        Dictionary<string, string> source,
        Dictionary<string, string> fallback)
    {
        var resolved = new Dictionary<string, string>(fallback, System.StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return resolved;
        }

        foreach (KeyValuePair<string, string> item in source)
        {
            string key = (item.Key ?? string.Empty).Trim();
            string value = (item.Value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                resolved[key] = value;
            }
        }

        return resolved;
    }

    private static string Pick(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
