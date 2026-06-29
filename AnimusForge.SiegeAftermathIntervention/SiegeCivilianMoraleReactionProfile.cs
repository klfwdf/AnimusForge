namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for civilian body reactions after GCCZ positive settlement actions.
/// AF/Bannerlord adapters own the live agent animation and movement side effects.
/// </summary>
public static class SiegeCivilianMoraleReactionProfile
{
    public const string StopPanicSource = "positive_settlement_stop_civilian_panic";

    public const string CheerSource = "positive_settlement_civilian_cheer";

    public static bool ShouldStopPanic(SiegeInterventionActionKind action)
    {
        return SiegeInterventionActionRules.IsMercyTrack(action);
    }

    public static bool ShouldCheer(SiegeInterventionActionKind action)
    {
        return action == SiegeInterventionActionKind.Inspire
            || action == SiegeInterventionActionKind.RallyOath;
    }
}
