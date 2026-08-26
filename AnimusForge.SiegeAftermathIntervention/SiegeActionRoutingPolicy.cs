using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free routing policy for postprocess action tags before AF applies side effects.
/// </summary>
public static class SiegeActionRoutingPolicy
{
    public static SiegeActionRoutingDecision Evaluate(SiegeActionRoutingFacts facts)
    {
        facts ??= new SiegeActionRoutingFacts(string.Empty, false, false, false);

        var kinds = SiegeActionTagCatalog.ExtractKinds(facts.RawActionText);
        return Evaluate(
            kinds,
            facts.DestructiveOutcomeLocked,
            facts.TargetIsAlliedSoldier,
            facts.HasSharedReliefPool,
            facts.ReplyIsDirectPlayerResponse);
    }

    public static SiegeActionRoutingDecision Evaluate(
        SiegeInterventionActionKind action,
        bool destructiveOutcomeLocked,
        bool targetIsAlliedSoldier,
        bool hasSharedReliefPool,
        bool replyIsDirectPlayerResponse)
    {
        SiegeInterventionActionKind[] actions = action == SiegeInterventionActionKind.Unknown
            ? System.Array.Empty<SiegeInterventionActionKind>()
            : new[] { action };
        return Evaluate(
            actions,
            destructiveOutcomeLocked,
            targetIsAlliedSoldier,
            hasSharedReliefPool,
            replyIsDirectPlayerResponse);
    }

    private static SiegeActionRoutingDecision Evaluate(
        System.Collections.Generic.IEnumerable<SiegeInterventionActionKind> actions,
        bool destructiveOutcomeLocked,
        bool targetIsAlliedSoldier,
        bool hasSharedReliefPool,
        bool replyIsDirectPlayerResponse)
    {
        var kinds = actions?.Distinct().ToArray() ?? System.Array.Empty<SiegeInterventionActionKind>();
        bool containsDestructiveAction = kinds.Any(SiegeInterventionActionRules.IsDestructive);
        bool containsSoldierMediatedDestructiveAction = kinds.Any(SiegeInterventionActionRules.IsSoldierMediatedDestructive);
        bool containsCivilianRobberyAction = kinds.Contains(SiegeInterventionActionKind.CivilianRobbery);
        bool canApplySoldierMediatedDestructiveAction = !containsSoldierMediatedDestructiveAction
            || (targetIsAlliedSoldier && replyIsDirectPlayerResponse);
        bool canApplyCivilianRobberyAction = containsCivilianRobberyAction
            && !targetIsAlliedSoldier
            && replyIsDirectPlayerResponse;
        bool shouldPromptSoldierForCivilianRobbery = containsCivilianRobberyAction
            && !canApplyCivilianRobberyAction;
        bool hasMercyTrackAction = kinds.Any(SiegeInterventionActionRules.IsMercyTrack);
        bool canApplyMercyTrack = !containsDestructiveAction && !destructiveOutcomeLocked;
        return new SiegeActionRoutingDecision(
            containsDestructiveAction,
            containsSoldierMediatedDestructiveAction,
            canApplySoldierMediatedDestructiveAction,
            containsCivilianRobberyAction,
            canApplyCivilianRobberyAction,
            shouldPromptSoldierForCivilianRobbery,
            shouldPromptSoldierDestructiveInquiry: containsSoldierMediatedDestructiveAction && !canApplySoldierMediatedDestructiveAction,
            hasMercyTrackAction,
            canApplyMercyTrack);
    }
}
