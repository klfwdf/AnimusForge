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
    {
        return action switch
        {
            SiegeCastleActionKind.ReleasePrisoners => "释放普通战俘",
            SiegeCastleActionKind.SellPrisoners => "按原版酒馆价格贩卖普通战俘",
            SiegeCastleActionKind.RecruitPrisonersVoluntary => "自愿收编普通战俘",
            SiegeCastleActionKind.RecruitPrisonersForced => "强制收编普通战俘",
            SiegeCastleActionKind.LaborPrisonersVoluntary => "自愿劳役服刑",
            SiegeCastleActionKind.LaborPrisonersForced => "强制劳役服刑",
            SiegeCastleActionKind.RepairCastleLaborVoluntary => "自愿劳役修缮城堡",
            SiegeCastleActionKind.RepairCastleLaborForced => "强制劳役修缮城堡",
            SiegeCastleActionKind.InstructorPrisonersVoluntary => "自愿担任教官",
            SiegeCastleActionKind.InstructorPrisonersForced => "强制担任教官",
            SiegeCastleActionKind.SlaughterPrisoners => "现场屠戮普通战俘",
            _ => "未指定处置"
        };
    }

}
