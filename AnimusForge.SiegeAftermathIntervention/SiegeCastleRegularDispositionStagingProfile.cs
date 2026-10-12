namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only classification and wording for ordinary-prisoner disposition groups.
/// Runtime owns the exact troop/count ledger and applies deferred groups at mission exit.
/// </summary>
public static class SiegeCastleRegularDispositionStagingProfile
{
    public static bool IsStagedAction(SiegeCastleActionKind action)
        => SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action);

    public static bool IsDeferredRosterAction(SiegeCastleActionKind action)
        => IsStagedAction(action) && action != SiegeCastleActionKind.SlaughterPrisoners;

    public static string Describe(SiegeCastleActionKind action)
        => Describe( action, false);

    public static string Describe(SiegeCastleActionKind action, bool isEnglish)
    {
        if (isEnglish)
        {
            return action switch
            {
                SiegeCastleActionKind.ReleasePrisoners => "Release regular prisoners",
                SiegeCastleActionKind.SellPrisoners => "Sell regular prisoners at native tavern prices",
                SiegeCastleActionKind.RecruitPrisonersVoluntary => "Voluntarily recruit regular prisoners",
                SiegeCastleActionKind.RecruitPrisonersForced => "Forcibly recruit regular prisoners",
                SiegeCastleActionKind.LaborPrisonersVoluntary => "Voluntary penal labor",
                SiegeCastleActionKind.LaborPrisonersForced => "Forced penal labor",
                SiegeCastleActionKind.RepairCastleLaborVoluntary => "Voluntary castle repair labor",
                SiegeCastleActionKind.RepairCastleLaborForced => "Forced castle repair labor",
                SiegeCastleActionKind.InstructorPrisonersVoluntary => "Voluntary instructor service",
                SiegeCastleActionKind.InstructorPrisonersForced => "Forced instructor service",
                SiegeCastleActionKind.SlaughterPrisoners => "Slaughter regular prisoners in the scene",
                _ => "No disposition specified"
            };
        }
        return action switch
        {
            SiegeCastleActionKind.ReleasePrisoners => CastleAftermathCompatibilityTexts.Raw("staging_release"),
            SiegeCastleActionKind.SellPrisoners => CastleAftermathCompatibilityTexts.Raw("staging_sell"),
            SiegeCastleActionKind.RecruitPrisonersVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_recruit_voluntary"),
            SiegeCastleActionKind.RecruitPrisonersForced => CastleAftermathCompatibilityTexts.Raw("staging_recruit_forced"),
            SiegeCastleActionKind.LaborPrisonersVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_labor_voluntary"),
            SiegeCastleActionKind.LaborPrisonersForced => CastleAftermathCompatibilityTexts.Raw("staging_labor_forced"),
            SiegeCastleActionKind.RepairCastleLaborVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_repair_voluntary"),
            SiegeCastleActionKind.RepairCastleLaborForced => CastleAftermathCompatibilityTexts.Raw("staging_repair_forced"),
            SiegeCastleActionKind.InstructorPrisonersVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_instructor_voluntary"),
            SiegeCastleActionKind.InstructorPrisonersForced => CastleAftermathCompatibilityTexts.Raw("staging_instructor_forced"),
            SiegeCastleActionKind.SlaughterPrisoners => CastleAftermathCompatibilityTexts.Raw("staging_slaughter"),
            _ => CastleAftermathCompatibilityTexts.Raw("staging_none")
        };
    }

}
