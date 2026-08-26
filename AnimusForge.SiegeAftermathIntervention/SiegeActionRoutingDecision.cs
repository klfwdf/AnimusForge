namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free routing decision for the current AI/player action-tag batch.
/// </summary>
public sealed class SiegeActionRoutingDecision
{
    public SiegeActionRoutingDecision(
        bool containsDestructiveAction,
        bool containsSoldierMediatedDestructiveAction,
        bool canApplySoldierMediatedDestructiveAction,
        bool containsCivilianRobberyAction,
        bool canApplyCivilianRobberyAction,
        bool shouldPromptSoldierForCivilianRobbery,
        bool shouldPromptSoldierDestructiveInquiry,
        bool hasMercyTrackAction,
        bool canApplyMercyTrack)
    {
        ContainsDestructiveAction = containsDestructiveAction;
        ContainsSoldierMediatedDestructiveAction = containsSoldierMediatedDestructiveAction;
        CanApplySoldierMediatedDestructiveAction = canApplySoldierMediatedDestructiveAction;
        ContainsCivilianRobberyAction = containsCivilianRobberyAction;
        CanApplyCivilianRobberyAction = canApplyCivilianRobberyAction;
        ShouldPromptSoldierForCivilianRobbery = shouldPromptSoldierForCivilianRobbery;
        ShouldPromptSoldierDestructiveInquiry = shouldPromptSoldierDestructiveInquiry;
        HasMercyTrackAction = hasMercyTrackAction;
        CanApplyMercyTrack = canApplyMercyTrack;
    }

    public bool ContainsDestructiveAction { get; }

    public bool ContainsSoldierMediatedDestructiveAction { get; }

    public bool CanApplySoldierMediatedDestructiveAction { get; }

    public bool ContainsCivilianRobberyAction { get; }

    public bool CanApplyCivilianRobberyAction { get; }

    public bool ShouldPromptSoldierForCivilianRobbery { get; }

    public bool ShouldPromptSoldierDestructiveInquiry { get; }

    public bool HasMercyTrackAction { get; }

    public bool CanApplyMercyTrack { get; }

}
