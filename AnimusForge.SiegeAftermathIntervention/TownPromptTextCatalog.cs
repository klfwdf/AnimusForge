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

    public string PersonalityPriorityInstruction { get; set; }

    public string RelationshipAndWitnessInstruction { get; set; }

    public string SameCultureSecondaryInstruction { get; set; }

    public string ActionExpressionVariationInstruction { get; set; }

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

    public string SettlementRuleMemoryNarrativeTemplate { get; set; }

    public string SettlementRuleMemoryPreviousNarrativeTemplate { get; set; }

    public string SettlementRuleMemoryUnknownRuler { get; set; }

    public string SettlementRuleMemoryUnknownCulture { get; set; }

    public string SettlementRuleMemoryUnknownPersonality { get; set; }

    public string SettlementRuleMemoryDurationLessThanDay { get; set; }

    public string SettlementRuleMemoryDurationAtLeastTwoYears { get; set; }

    public string SettlementRuleMemoryDurationDaysTemplate { get; set; }

    public string SettlementRuleMemoryDurationWeeksTemplate { get; set; }

    public string SettlementRuleMemoryDurationYearsTemplate { get; set; }

    public string SettlementRuleMemoryEncyclopediaHeader { get; set; }

    public string SettlementRuleMemoryEncyclopediaCurrentTemplate { get; set; }

    public string SettlementRuleMemoryEncyclopediaPreviousTemplate { get; set; }

    public string SettlementRuleMemoryEncyclopediaGenerating { get; set; }

    public string SettlementRuleMemoryGenerationSystemPrompt { get; set; }

    public string SettlementRuleMemoryGenerationUserTemplate { get; set; }

    public string SettlementRuleMemoryGenerationOutputProtocol { get; set; }

    public string SettlementRuleMemoryDeveloperMenuOption { get; set; }

    public string SettlementRuleMemoryDeveloperSelectionTitle { get; set; }

    public string SettlementRuleMemoryDeveloperSelectionDescription { get; set; }

    public string SettlementRuleMemoryDeveloperEntryTemplate { get; set; }

    public string SettlementRuleMemoryDeveloperEditTitleTemplate { get; set; }

    public string SettlementRuleMemoryDeveloperEditHint { get; set; }

    public string SettlementRuleMemoryDeveloperSaveLabel { get; set; }

    public string SettlementRuleMemoryDeveloperCancelLabel { get; set; }

    public string SettlementRuleMemoryDeveloperRegenerateLabel { get; set; }

    public string PlunderLedgerContextTemplate { get; set; }

    public string PlunderLedgerActiveState { get; set; }

    public string PlunderLedgerStoppedState { get; set; }

    public string PlunderLedgerCompletedState { get; set; }

    public string MassacreLedgerContextTemplate { get; set; }

    public string MassacreLedgerActiveState { get; set; }

    public string MassacreLedgerStoppedState { get; set; }

    public string MassacreLedgerCompletedState { get; set; }

    public string MassacreStoppedMessageTemplate { get; set; }

    public string MassacreSoldierMemoryPerspective { get; set; }

    public string ConstructiveCultureChangeContextTemplate { get; set; }

    public string ConstructiveCultureChangeUnavailableContextTemplate { get; set; }

    public string ConstructiveCultureChangeSuccessMessageTemplate { get; set; }

    public string ConstructiveCultureChangeMemoryTitle { get; set; }

    public string ConstructiveCultureChangeMemoryTemplate { get; set; }

    public string OutcomeNoDecision { get; set; }

    public string OutcomeMassacreActive { get; set; }

    public string OutcomePlunderActive { get; set; }

    public string OutcomePendingTemplate { get; set; }

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
            PersonalityPriorityInstruction = Pick(source.PersonalityPriorityInstruction, fallback.PersonalityPriorityInstruction),
            RelationshipAndWitnessInstruction = Pick(source.RelationshipAndWitnessInstruction, fallback.RelationshipAndWitnessInstruction),
            SameCultureSecondaryInstruction = Pick(source.SameCultureSecondaryInstruction, fallback.SameCultureSecondaryInstruction),
            ActionExpressionVariationInstruction = Pick(source.ActionExpressionVariationInstruction, fallback.ActionExpressionVariationInstruction),
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
            SettlementRuleMemoryNarrativeTemplate = Pick(source.SettlementRuleMemoryNarrativeTemplate, fallback.SettlementRuleMemoryNarrativeTemplate),
            SettlementRuleMemoryPreviousNarrativeTemplate = Pick(source.SettlementRuleMemoryPreviousNarrativeTemplate, fallback.SettlementRuleMemoryPreviousNarrativeTemplate),
            SettlementRuleMemoryUnknownRuler = Pick(source.SettlementRuleMemoryUnknownRuler, fallback.SettlementRuleMemoryUnknownRuler),
            SettlementRuleMemoryUnknownCulture = Pick(source.SettlementRuleMemoryUnknownCulture, fallback.SettlementRuleMemoryUnknownCulture),
            SettlementRuleMemoryUnknownPersonality = Pick(source.SettlementRuleMemoryUnknownPersonality, fallback.SettlementRuleMemoryUnknownPersonality),
            SettlementRuleMemoryDurationLessThanDay = Pick(source.SettlementRuleMemoryDurationLessThanDay, fallback.SettlementRuleMemoryDurationLessThanDay),
            SettlementRuleMemoryDurationAtLeastTwoYears = Pick(source.SettlementRuleMemoryDurationAtLeastTwoYears, fallback.SettlementRuleMemoryDurationAtLeastTwoYears),
            SettlementRuleMemoryDurationDaysTemplate = Pick(source.SettlementRuleMemoryDurationDaysTemplate, fallback.SettlementRuleMemoryDurationDaysTemplate),
            SettlementRuleMemoryDurationWeeksTemplate = Pick(source.SettlementRuleMemoryDurationWeeksTemplate, fallback.SettlementRuleMemoryDurationWeeksTemplate),
            SettlementRuleMemoryDurationYearsTemplate = Pick(source.SettlementRuleMemoryDurationYearsTemplate, fallback.SettlementRuleMemoryDurationYearsTemplate),
            SettlementRuleMemoryEncyclopediaHeader = Pick(source.SettlementRuleMemoryEncyclopediaHeader, fallback.SettlementRuleMemoryEncyclopediaHeader),
            SettlementRuleMemoryEncyclopediaCurrentTemplate = Pick(source.SettlementRuleMemoryEncyclopediaCurrentTemplate, fallback.SettlementRuleMemoryEncyclopediaCurrentTemplate),
            SettlementRuleMemoryEncyclopediaPreviousTemplate = Pick(source.SettlementRuleMemoryEncyclopediaPreviousTemplate, fallback.SettlementRuleMemoryEncyclopediaPreviousTemplate),
            SettlementRuleMemoryEncyclopediaGenerating = Pick(source.SettlementRuleMemoryEncyclopediaGenerating, fallback.SettlementRuleMemoryEncyclopediaGenerating),
            SettlementRuleMemoryGenerationSystemPrompt = Pick(source.SettlementRuleMemoryGenerationSystemPrompt, fallback.SettlementRuleMemoryGenerationSystemPrompt),
            SettlementRuleMemoryGenerationUserTemplate = Pick(source.SettlementRuleMemoryGenerationUserTemplate, fallback.SettlementRuleMemoryGenerationUserTemplate),
            SettlementRuleMemoryGenerationOutputProtocol = Pick(source.SettlementRuleMemoryGenerationOutputProtocol, fallback.SettlementRuleMemoryGenerationOutputProtocol),
            SettlementRuleMemoryDeveloperMenuOption = Pick(source.SettlementRuleMemoryDeveloperMenuOption, fallback.SettlementRuleMemoryDeveloperMenuOption),
            SettlementRuleMemoryDeveloperSelectionTitle = Pick(source.SettlementRuleMemoryDeveloperSelectionTitle, fallback.SettlementRuleMemoryDeveloperSelectionTitle),
            SettlementRuleMemoryDeveloperSelectionDescription = Pick(source.SettlementRuleMemoryDeveloperSelectionDescription, fallback.SettlementRuleMemoryDeveloperSelectionDescription),
            SettlementRuleMemoryDeveloperEntryTemplate = Pick(source.SettlementRuleMemoryDeveloperEntryTemplate, fallback.SettlementRuleMemoryDeveloperEntryTemplate),
            SettlementRuleMemoryDeveloperEditTitleTemplate = Pick(source.SettlementRuleMemoryDeveloperEditTitleTemplate, fallback.SettlementRuleMemoryDeveloperEditTitleTemplate),
            SettlementRuleMemoryDeveloperEditHint = Pick(source.SettlementRuleMemoryDeveloperEditHint, fallback.SettlementRuleMemoryDeveloperEditHint),
            SettlementRuleMemoryDeveloperSaveLabel = Pick(source.SettlementRuleMemoryDeveloperSaveLabel, fallback.SettlementRuleMemoryDeveloperSaveLabel),
            SettlementRuleMemoryDeveloperCancelLabel = Pick(source.SettlementRuleMemoryDeveloperCancelLabel, fallback.SettlementRuleMemoryDeveloperCancelLabel),
            SettlementRuleMemoryDeveloperRegenerateLabel = Pick(source.SettlementRuleMemoryDeveloperRegenerateLabel, fallback.SettlementRuleMemoryDeveloperRegenerateLabel),
            PlunderLedgerContextTemplate = Pick(source.PlunderLedgerContextTemplate, fallback.PlunderLedgerContextTemplate),
            PlunderLedgerActiveState = Pick(source.PlunderLedgerActiveState, fallback.PlunderLedgerActiveState),
            PlunderLedgerStoppedState = Pick(source.PlunderLedgerStoppedState, fallback.PlunderLedgerStoppedState),
            PlunderLedgerCompletedState = Pick(source.PlunderLedgerCompletedState, fallback.PlunderLedgerCompletedState),
            MassacreLedgerContextTemplate = Pick(source.MassacreLedgerContextTemplate, fallback.MassacreLedgerContextTemplate),
            MassacreLedgerActiveState = Pick(source.MassacreLedgerActiveState, fallback.MassacreLedgerActiveState),
            MassacreLedgerStoppedState = Pick(source.MassacreLedgerStoppedState, fallback.MassacreLedgerStoppedState),
            MassacreLedgerCompletedState = Pick(source.MassacreLedgerCompletedState, fallback.MassacreLedgerCompletedState),
            MassacreStoppedMessageTemplate = Pick(source.MassacreStoppedMessageTemplate, fallback.MassacreStoppedMessageTemplate),
            MassacreSoldierMemoryPerspective = Pick(source.MassacreSoldierMemoryPerspective, fallback.MassacreSoldierMemoryPerspective),
            ConstructiveCultureChangeContextTemplate = Pick(source.ConstructiveCultureChangeContextTemplate, fallback.ConstructiveCultureChangeContextTemplate),
            ConstructiveCultureChangeUnavailableContextTemplate = Pick(source.ConstructiveCultureChangeUnavailableContextTemplate, fallback.ConstructiveCultureChangeUnavailableContextTemplate),
            ConstructiveCultureChangeSuccessMessageTemplate = Pick(source.ConstructiveCultureChangeSuccessMessageTemplate, fallback.ConstructiveCultureChangeSuccessMessageTemplate),
            ConstructiveCultureChangeMemoryTitle = Pick(source.ConstructiveCultureChangeMemoryTitle, fallback.ConstructiveCultureChangeMemoryTitle),
            ConstructiveCultureChangeMemoryTemplate = Pick(source.ConstructiveCultureChangeMemoryTemplate, fallback.ConstructiveCultureChangeMemoryTemplate),
            OutcomeNoDecision = Pick(source.OutcomeNoDecision, fallback.OutcomeNoDecision),
            OutcomeMassacreActive = Pick(source.OutcomeMassacreActive, fallback.OutcomeMassacreActive),
            OutcomePlunderActive = Pick(source.OutcomePlunderActive, fallback.OutcomePlunderActive),
            OutcomePendingTemplate = Pick(source.OutcomePendingTemplate, fallback.OutcomePendingTemplate),
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
            Version = 3,
            SceneSectionTitle = "[1. CURRENT SCENE]",
            SceneSummaryTemplate = "{settlement} was just captured by the player. Treat this as an occupied aftermath scene, not ordinary town life.",
            RoleSectionTitle = "[2. SPEAKER ROLE]",
            RoleInstructions = new Dictionary<string, string>
            {
                [TownDialogueRole.AccompanyingNoble.ToString()] = "An accompanying allied noble. Use AF personality, political interests, relationship with the player, the player's conduct, and witnessed scene events. Advise or criticize as a noble without pretending to execute ordinary soldier orders.",
                [TownDialogueRole.NoblePrisoner.ToString()] = "A noble prisoner. Use AF personality while emphasizing captivity, dignity, fear, ransom or bargaining value, personal risk, and judgment of the player's conduct. Do not speak as a free allied noble.",
                [TownDialogueRole.PlayerCompanion.ToString()] = "A player companion. Use AF personality first, then relationship with the player, shared experience, the player's current conduct, and witnessed scene events. Comment or advise without pretending to be an ordinary soldier.",
                [TownDialogueRole.SettlementNotable.ToString()] = "A settlement headman or notable. Use profession, local assets and networks, town rule memory, local interests, civilian risk, personal danger, and AF personality. Negotiate as a local representative without claiming victorious soldier authority.",
                [TownDialogueRole.OrdinarySoldier.ToString()] = "An ordinary soldier. Use current orders, allegiance, morale, witnessed scene events, troop identity, and the AF unnamed-character personality. Keep all GCCZ personal memory scene-local.",
                [TownDialogueRole.OrdinaryCivilian.ToString()] = "An ordinary civilian. Use occupation or social identity, town rule memory, the AF unnamed-character personality, fear, safety, witnessed harm, and the player's scene conduct. Keep all GCCZ personal memory scene-local.",
            },
            PersonalityPriorityInstruction = "Priority: live scene facts and witnessed events first; then AF personality and relationship; then role, occupation, and background culture. Never invent a trait that AF did not provide.",
            RelationshipAndWitnessInstruction = "Relationship changes trust, familiarity, restraint, and willingness to advise, but cannot erase captivity, command authority, witnessed harm, or another live scene fact.",
            SameCultureSecondaryInstruction = "Shared culture is secondary atmosphere only. It may change idiom, grief, shame, or sympathy, but never overrides AF personality, personal relationship, current authority, or scene causality.",
            ActionExpressionVariationInstruction = "Voice the same accepted action differently for different personalities and roles, while leaving its eligibility, requirements, completion, rewards, and consequences unchanged.",
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
            SettlementRuleMemoryNarrativeTemplate = "Local account under {ruler}: {narrative}",
            SettlementRuleMemoryPreviousNarrativeTemplate = "Retained local account under {ruler}: {narrative}",
            SettlementRuleMemoryUnknownRuler = "an unrecorded ruler",
            SettlementRuleMemoryUnknownCulture = "an unrecorded culture",
            SettlementRuleMemoryUnknownPersonality = "no reliable AF personality record",
            SettlementRuleMemoryDurationLessThanDay = "less than one day",
            SettlementRuleMemoryDurationAtLeastTwoYears = "at least two years",
            SettlementRuleMemoryDurationDaysTemplate = "{value} days",
            SettlementRuleMemoryDurationWeeksTemplate = "{value} weeks",
            SettlementRuleMemoryDurationYearsTemplate = "{value} years",
            SettlementRuleMemoryEncyclopediaHeader = "[Town memory]",
            SettlementRuleMemoryEncyclopediaCurrentTemplate = "Current ruler: {ruler}; {culture}; {duration}.\n{narrative}",
            SettlementRuleMemoryEncyclopediaPreviousTemplate = "Former ruler: {ruler}; {culture}; {duration}.\n{narrative}",
            SettlementRuleMemoryEncyclopediaGenerating = "The local account is being compiled.",
            SettlementRuleMemoryGenerationSystemPrompt = "Write one concise local town memory grounded only in the supplied facts. Treat the AF ruler personality as the main cause of public reputation. Do not invent named events, rewards, game mechanics, tags, or player biography.",
            SettlementRuleMemoryGenerationUserTemplate = "Town: {settlement}\nRuler: {ruler}\nCulture: {culture}\nRule duration: {duration}\nAF ruler personality and reputation traits: {personality}\nWrite about 100 Chinese characters from the shared perspective of local residents. Show how the ruler's reputation affected daily expectations, fear, trust, or public conduct.",
            SettlementRuleMemoryGenerationOutputProtocol = "OUTPUT: Return exactly one JSON object and nothing else: {\"memory\":\"80-120 Chinese characters\"}",
            SettlementRuleMemoryDeveloperMenuOption = "Developer: edit current town memory",
            SettlementRuleMemoryDeveloperSelectionTitle = "Town memory editor",
            SettlementRuleMemoryDeveloperSelectionDescription = "Select one of the retained ruler memories.",
            SettlementRuleMemoryDeveloperEntryTemplate = "{ruler} | {duration}",
            SettlementRuleMemoryDeveloperEditTitleTemplate = "Edit town memory: {ruler}",
            SettlementRuleMemoryDeveloperEditHint = "Edit the local memory article. Clearing it allows generation again.",
            SettlementRuleMemoryDeveloperSaveLabel = "Save",
            SettlementRuleMemoryDeveloperCancelLabel = "Cancel",
            SettlementRuleMemoryDeveloperRegenerateLabel = "Regenerate current memory",
            PlunderLedgerContextTemplate = "Plunder ledger: {state}; acquired value {acquired_value}/{available_value} ({progress}); merchants {merchant_count}, notables {notable_count}, civilians {civilian_count}; gold {gold}, item value {item_value}.",
            PlunderLedgerActiveState = "active",
            PlunderLedgerStoppedState = "stopped; prior loot and consequences remain",
            PlunderLedgerCompletedState = "completed",
            MassacreLedgerContextTemplate = "Massacre ledger: {state}; killed {killed_count}/{captured_count}, ordinary civilians {civilian_deaths}, notables {notable_deaths}, weighted progress {progress}. Only captured targets count.",
            MassacreLedgerActiveState = "active and interruptible",
            MassacreLedgerStoppedState = "stopped; prior deaths and consequences remain",
            MassacreLedgerCompletedState = "completed and locked",
            MassacreStoppedMessageTemplate = "Massacre stopped. {survivor_count} captured targets survive; prior deaths and consequences remain.",
            MassacreSoldierMemoryPerspective = "Massacre is active, but a stop order remains valid until every captured target is dead. Obedience may still sound tense or distressed.",
            ConstructiveCultureChangeContextTemplate = "Constructive culture administration is available for {settlement}: current culture {current_culture}, player governance culture {target_culture}. This changes only the town culture and does not imply killing, expulsion, colonization, or a settlement aftermath result.",
            ConstructiveCultureChangeUnavailableContextTemplate = "Constructive culture administration is not currently executable for {settlement}: current culture {current_culture}, player governance culture {target_culture}. Do not claim it was completed.",
            ConstructiveCultureChangeSuccessMessageTemplate = "{settlement} now uses {target_culture} culture through ordinary administration. No colonization, massacre, plunder, or settlement aftermath result was triggered.",
            ConstructiveCultureChangeMemoryTitle = "Constructive culture administration",
            ConstructiveCultureChangeMemoryTemplate = "The player changed {settlement} from {current_culture} culture to {target_culture} culture through ordinary administration without colonization or destructive settlement resolution.",
            OutcomeNoDecision = "No final aftermath has been selected.",
            OutcomeMassacreActive = "Massacre is active and may be stopped before every captured target is dead; prior deaths remain.",
            OutcomePlunderActive = "Plunder is active and remains reversible.",
            OutcomePendingTemplate = "Pending aftermath: {aftermath}",
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
            PostprocessTransitionRule = "Mercy may stop reversible plunder. An eligible stop action may interrupt massacre before every captured target is dead. Colonization commit depends only on runtime state.",
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
