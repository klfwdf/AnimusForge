namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// The two high-risk ordinary-prisoner outcomes that allied soldiers may propose.
/// </summary>
public enum SiegeCastlePrisonerDispositionKind
{
    None = 0,
    Recruit = 1,
    Slaughter = 2
}

public static class SiegeCastlePrisonerDispositionKindProfile
{
    public static SiegeCastlePrisonerDispositionKind FromAction(SiegeCastleActionKind action)
    {
        return action switch
        {
            SiegeCastleActionKind.ProposeRecruitPrisoners => SiegeCastlePrisonerDispositionKind.Recruit,
            SiegeCastleActionKind.RecruitPrisonersVoluntary => SiegeCastlePrisonerDispositionKind.Recruit,
            SiegeCastleActionKind.RecruitPrisonersForced => SiegeCastlePrisonerDispositionKind.Recruit,
            SiegeCastleActionKind.ProposeSlaughterPrisoners => SiegeCastlePrisonerDispositionKind.Slaughter,
            SiegeCastleActionKind.SlaughterPrisoners => SiegeCastlePrisonerDispositionKind.Slaughter,
            _ => SiegeCastlePrisonerDispositionKind.None
        };
    }

    public static string Describe(SiegeCastlePrisonerDispositionKind disposition)
    {
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => "收编普通战俘",
            SiegeCastlePrisonerDispositionKind.Slaughter => "屠戮普通战俘",
            _ => "无"
        };
    }
}
