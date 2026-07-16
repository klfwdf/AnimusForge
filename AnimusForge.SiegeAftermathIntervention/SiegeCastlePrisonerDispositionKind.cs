namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Ordinary-prisoner disposition families that may be proposed and then explicitly approved.
/// </summary>
public enum SiegeCastlePrisonerDispositionKind
{
    None = 0,
    Recruit = 1,
    Slaughter = 2,
    Release = 3,
    Sell = 4,
    Labor = 5,
    Instructor = 6
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
            SiegeCastleActionKind.ProposeReleasePrisoners => SiegeCastlePrisonerDispositionKind.Release,
            SiegeCastleActionKind.ReleasePrisoners => SiegeCastlePrisonerDispositionKind.Release,
            SiegeCastleActionKind.ProposeSellPrisoners => SiegeCastlePrisonerDispositionKind.Sell,
            SiegeCastleActionKind.SellPrisoners => SiegeCastlePrisonerDispositionKind.Sell,
            SiegeCastleActionKind.ProposeLaborPrisoners => SiegeCastlePrisonerDispositionKind.Labor,
            SiegeCastleActionKind.LaborPrisonersVoluntary => SiegeCastlePrisonerDispositionKind.Labor,
            SiegeCastleActionKind.LaborPrisonersForced => SiegeCastlePrisonerDispositionKind.Labor,
            SiegeCastleActionKind.ProposeInstructorPrisoners => SiegeCastlePrisonerDispositionKind.Instructor,
            SiegeCastleActionKind.InstructorPrisonersVoluntary => SiegeCastlePrisonerDispositionKind.Instructor,
            SiegeCastleActionKind.InstructorPrisonersForced => SiegeCastlePrisonerDispositionKind.Instructor,
            _ => SiegeCastlePrisonerDispositionKind.None
        };
    }

    public static string Describe(SiegeCastlePrisonerDispositionKind disposition)
    {
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => "收编普通战俘",
            SiegeCastlePrisonerDispositionKind.Slaughter => "屠戮普通战俘",
            SiegeCastlePrisonerDispositionKind.Release => "释放普通战俘",
            SiegeCastlePrisonerDispositionKind.Sell => "贩卖普通战俘",
            SiegeCastlePrisonerDispositionKind.Labor => "劳役安置普通战俘",
            SiegeCastlePrisonerDispositionKind.Instructor => "安排普通战俘充当教官",
            _ => "无"
        };
    }
}
