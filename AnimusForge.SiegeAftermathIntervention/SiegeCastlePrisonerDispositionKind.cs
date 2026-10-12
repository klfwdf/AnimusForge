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
    Instructor = 6,
    RepairCastle = 7
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
            SiegeCastleActionKind.ProposeRepairCastleLabor => SiegeCastlePrisonerDispositionKind.RepairCastle,
            SiegeCastleActionKind.RepairCastleLaborVoluntary => SiegeCastlePrisonerDispositionKind.RepairCastle,
            SiegeCastleActionKind.RepairCastleLaborForced => SiegeCastlePrisonerDispositionKind.RepairCastle,
            SiegeCastleActionKind.ProposeInstructorPrisoners => SiegeCastlePrisonerDispositionKind.Instructor,
            SiegeCastleActionKind.InstructorPrisonersVoluntary => SiegeCastlePrisonerDispositionKind.Instructor,
            SiegeCastleActionKind.InstructorPrisonersForced => SiegeCastlePrisonerDispositionKind.Instructor,
            _ => SiegeCastlePrisonerDispositionKind.None
        };
    }

    public static string Describe(SiegeCastlePrisonerDispositionKind disposition)
        => Describe(disposition, false);

    public static string Describe(SiegeCastlePrisonerDispositionKind disposition, bool isEnglish)
    {
        if (isEnglish)
        {
            return disposition switch
            {
                SiegeCastlePrisonerDispositionKind.Recruit => "Recruit regular prisoners",
                SiegeCastlePrisonerDispositionKind.Slaughter => "Slaughter regular prisoners",
                SiegeCastlePrisonerDispositionKind.Release => "Release regular prisoners",
                SiegeCastlePrisonerDispositionKind.Sell => "Sell regular prisoners",
                SiegeCastlePrisonerDispositionKind.Labor => "Penal labor for regular prisoners",
                SiegeCastlePrisonerDispositionKind.RepairCastle => "Castle repair labor",
                SiegeCastlePrisonerDispositionKind.Instructor => "Assign regular prisoners as instructors",
                _ => "None"
            };
        }
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => CastleAftermathCompatibilityTexts.Raw("disposition_recruit"),
            SiegeCastlePrisonerDispositionKind.Slaughter => CastleAftermathCompatibilityTexts.Raw("disposition_slaughter"),
            SiegeCastlePrisonerDispositionKind.Release => CastleAftermathCompatibilityTexts.Raw("disposition_release"),
            SiegeCastlePrisonerDispositionKind.Sell => CastleAftermathCompatibilityTexts.Raw("disposition_sell"),
            SiegeCastlePrisonerDispositionKind.Labor => CastleAftermathCompatibilityTexts.Raw("disposition_labor"),
            SiegeCastlePrisonerDispositionKind.RepairCastle => CastleAftermathCompatibilityTexts.Raw("disposition_repair_castle"),
            SiegeCastlePrisonerDispositionKind.Instructor => CastleAftermathCompatibilityTexts.Raw("disposition_instructor"),
            _ => CastleAftermathCompatibilityTexts.Raw("disposition_none")
        };
    }
}
