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
