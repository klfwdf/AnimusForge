using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Builds semantic postprocess candidates for non-direct town reactions.
/// Suggestion tags never mutate settlement state.
/// </summary>
public static class TownAmbientReactionRuleCatalog
{
    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(
        SiegePostprocessRuleEligibilityFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        if (!facts.IsAmbientReaction
            || !TownDialogueAuthorityPolicy.CanOfferSuggestion(facts.DialogueRole, facts.IsAlliedSoldier))
        {
            return Array.Empty<SiegePostprocessRuleDefinition>();
        }

        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        var result = new List<SiegePostprocessRuleDefinition>();
        if (TownDialogueAuthorityPolicy.CanExpressSoldierDiscontent(facts.DialogueRole, facts.IsAlliedSoldier))
        {
            result.Add(new SiegePostprocessRuleDefinition(
                TownAmbientReactionTagCatalog.SoldierDiscontentTag,
                text.GetAmbientRuleDescription(TownAmbientReactionActionKind.SoldierDiscontent)));
        }

        foreach (SiegeInterventionActionKind action in SiegeActionTagCatalog.GetCanonicalOrder())
        {
            if (action == facts.AmbientReactionToAction
                || !IsSuggestionAvailable(action, facts)
                || !TownAmbientReactionTagCatalog.TryGetSuggestionKind(action, out TownAmbientReactionActionKind suggestionKind)
                || !TownAmbientReactionTagCatalog.TryGetCanonicalTag(suggestionKind, out string tag))
            {
                continue;
            }

            result.Add(new SiegePostprocessRuleDefinition(
                tag,
                text.GetAmbientRuleDescription(suggestionKind)));
        }
        return result;
    }

    public static bool IsSuggestionAvailable(
        SiegeInterventionActionKind action,
        SiegePostprocessRuleEligibilityFacts facts)
    {
        switch (action)
        {
            case SiegeInterventionActionKind.Mercy:
            case SiegeInterventionActionKind.Relief:
            case SiegeInterventionActionKind.Inspire:
            case SiegeInterventionActionKind.RallyOath:
                return !facts.DestructiveLocked;
            case SiegeInterventionActionKind.AppeaseSoldiers:
                return facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied;
            case SiegeInterventionActionKind.GatherCivilians:
                return !facts.MassacreActive;
            case SiegeInterventionActionKind.CivilianRobbery:
            case SiegeInterventionActionKind.Plunder:
            case SiegeInterventionActionKind.Massacre:
                return !facts.DestructiveLocked;
            case SiegeInterventionActionKind.CulturalRepopulation:
                return facts.ColonizationAvailable;
            case SiegeInterventionActionKind.StopMassacre:
                return facts.MassacreActive;
            case SiegeInterventionActionKind.ConstructiveCultureChange:
                return facts.ConstructiveCultureChangeAvailable;
            default:
                return false;
        }
    }
}
