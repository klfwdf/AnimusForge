namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only actions recognized during the active post-capture intervention stage.
/// Town aftermath actions intentionally remain in <see cref="SiegeInterventionActionKind"/>.
/// </summary>
public enum SiegeCastleActionKind
{
    Unknown = 0,
    RecruitPrisoners = 1,
    SlaughterPrisoners = 2,
    AppeaseSoldiers = 3,
    ProposeRecruitPrisoners = 4,
    ProposeSlaughterPrisoners = 5
}

/// <summary>
/// Stable semantics for separating non-mutating proposals from settlement actions.
/// </summary>
public static class SiegeCastleActionKindProfile
{
    public static bool IsProposal(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.ProposeRecruitPrisoners
            || action == SiegeCastleActionKind.ProposeSlaughterPrisoners;
    }

    public static bool IsPrisonerDispositionSettlement(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.RecruitPrisoners
            || action == SiegeCastleActionKind.SlaughterPrisoners;
    }

    public static bool IsSettlement(SiegeCastleActionKind action)
    {
        return IsPrisonerDispositionSettlement(action)
            || action == SiegeCastleActionKind.AppeaseSoldiers;
    }
}
