using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free postprocess-rule filtering for the active GCCZ intervention scene.
/// AF adapters pass runtime state in; this core owns action-tag classification.
/// </summary>
public static class SiegePostprocessRuleFilter
{
    public static bool ShouldAllowTag(
        string tag,
        SiegePostprocessRuleEligibilityFacts facts)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        IReadOnlyList<TownAmbientReactionActionKind> ambientKinds = TownAmbientReactionTagCatalog.ExtractKinds(tag.Trim());
        if (ambientKinds.Count > 0)
        {
            if (!facts.IsAmbientReaction || facts.ReplyIsDirectPlayerResponse || ambientKinds.Count != 1)
            {
                return false;
            }

            TownAmbientReactionActionKind ambientKind = ambientKinds[0];
            if (ambientKind == TownAmbientReactionActionKind.SoldierDiscontent)
            {
                return TownDialogueAuthorityPolicy.CanExpressSoldierDiscontent(facts.DialogueRole, facts.IsAlliedSoldier);
            }

            return TownDialogueAuthorityPolicy.CanOfferSuggestion(facts.DialogueRole, facts.IsAlliedSoldier)
                && TownAmbientReactionTagCatalog.TryGetSuggestedAction(ambientKind, out SiegeInterventionActionKind suggestedAction)
                && suggestedAction != facts.AmbientReactionToAction
                && TownAmbientReactionRuleCatalog.IsSuggestionAvailable(suggestedAction, facts);
        }

        var kinds = SiegeActionTagCatalog.ExtractKinds(tag.Trim());
        if (kinds.Count == 0)
        {
            return true;
        }

        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return false;
        }

        bool mercyTrackTag = kinds.Any(SiegeInterventionActionRules.IsMercyTrack);
        if (facts.DestructiveLocked && mercyTrackTag)
        {
            return false;
        }

        foreach (SiegeInterventionActionKind kind in kinds)
        {
            if (SiegeInterventionActionRules.IsMercyTrack(kind)
                && !TownDialogueAuthorityPolicy.CanEmitPositiveSettlementOutcome(
                    facts.DialogueRole,
                    facts.IsAlliedSoldier,
                    kind))
            {
                return false;
            }
        }

        bool stopMassacreTag = kinds.Contains(SiegeInterventionActionKind.StopMassacre);
        if (stopMassacreTag && !facts.MassacreActive)
        {
            return false;
        }

        bool colonizationTag = kinds.Contains(SiegeInterventionActionKind.CulturalRepopulation);
        if (colonizationTag && !facts.ColonizationAvailable)
        {
            return false;
        }

        bool constructiveCultureChangeTag = kinds.Contains(SiegeInterventionActionKind.ConstructiveCultureChange);
        if (constructiveCultureChangeTag
            && (!facts.ConstructiveCultureChangeAvailable
                || !TownDialogueRoleClassifier.CanAuthorizeConstructiveCultureChange(facts.DialogueRole, facts.IsAlliedSoldier)))
        {
            return false;
        }

        bool soldierAppeasementTag = kinds.Contains(SiegeInterventionActionKind.AppeaseSoldiers);
        if (soldierAppeasementTag
            && (!TownDialogueRoleClassifier.CanExecuteAlliedSoldierOrders(facts.DialogueRole, facts.IsAlliedSoldier)
                || !facts.SoldierAppeasementRequired
                || facts.SoldierAppeasementApplied))
        {
            return false;
        }

        bool soldierMediatedDestructiveTag = kinds.Any(SiegeInterventionActionRules.IsSoldierMediatedDestructive);
        if (soldierMediatedDestructiveTag
            && !TownDialogueRoleClassifier.CanExecuteAlliedSoldierOrders(facts.DialogueRole, facts.IsAlliedSoldier))
        {
            return false;
        }

        bool civilianRobberyTag = kinds.Contains(SiegeInterventionActionKind.CivilianRobbery);
        if (civilianRobberyTag && !TownDialogueRoleClassifier.CanBeRobberyTarget(facts.DialogueRole))
        {
            return false;
        }

        return true;
    }
}
