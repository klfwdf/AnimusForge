namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only actions recognized during the active post-capture intervention stage.
/// Town aftermath actions intentionally remain in <see cref="SiegeInterventionActionKind"/>.
/// </summary>
public enum SiegeCastleActionKind
{
    Unknown = 0,
    SlaughterPrisoners = 1,
    AppeaseSoldiers = 2,
    ProposeRecruitPrisoners = 3,
    ProposeSlaughterPrisoners = 4,
    TreatPrisoners = 5,
    ReceiveArmaments = 6,
    ReleasePrisoners = 7,
    SellPrisoners = 8,
    RecruitPrisonersVoluntary = 9,
    RecruitPrisonersForced = 10,
    LaborPrisonersVoluntary = 11,
    LaborPrisonersForced = 12,
    InstructorPrisonersVoluntary = 13,
    InstructorPrisonersForced = 14,
    RecruitLord = 15,
    ExecuteLord = 16,
    ProposeReleasePrisoners = 17,
    ProposeSellPrisoners = 18,
    ProposeLaborPrisoners = 19,
    ProposeInstructorPrisoners = 20,
    SellLord = 21,
    SoldierDiscontent = 22
}

/// <summary>
/// Stable semantics for separating non-mutating proposals from settlement actions.
/// </summary>
public static class SiegeCastleActionKindProfile
{
    public static bool IsProposal(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.ProposeRecruitPrisoners
            || action == SiegeCastleActionKind.ProposeSlaughterPrisoners
            || action == SiegeCastleActionKind.ProposeReleasePrisoners
            || action == SiegeCastleActionKind.ProposeSellPrisoners
            || action == SiegeCastleActionKind.ProposeLaborPrisoners
            || action == SiegeCastleActionKind.ProposeInstructorPrisoners;
    }

    public static bool IsProcess(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.TreatPrisoners
            || action == SiegeCastleActionKind.ReceiveArmaments;
    }

    public static bool IsRegularPrisonerTerminal(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.SlaughterPrisoners
            || action == SiegeCastleActionKind.ReleasePrisoners
            || action == SiegeCastleActionKind.SellPrisoners
            || action == SiegeCastleActionKind.RecruitPrisonersVoluntary
            || action == SiegeCastleActionKind.RecruitPrisonersForced
            || action == SiegeCastleActionKind.LaborPrisonersVoluntary
            || action == SiegeCastleActionKind.LaborPrisonersForced
            || action == SiegeCastleActionKind.InstructorPrisonersVoluntary
            || action == SiegeCastleActionKind.InstructorPrisonersForced;
    }

    public static bool IsLordTerminal(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.RecruitLord
            || action == SiegeCastleActionKind.ExecuteLord
            || action == SiegeCastleActionKind.SellLord;
    }

    public static bool IsVoluntary(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.RecruitPrisonersVoluntary
            || action == SiegeCastleActionKind.LaborPrisonersVoluntary
            || action == SiegeCastleActionKind.InstructorPrisonersVoluntary;
    }

    public static bool IsForced(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.RecruitPrisonersForced
            || action == SiegeCastleActionKind.LaborPrisonersForced
            || action == SiegeCastleActionKind.InstructorPrisonersForced;
    }

    public static bool IsRecruitment(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.RecruitPrisonersVoluntary
            || action == SiegeCastleActionKind.RecruitPrisonersForced;
    }

    public static bool RequiresRegularPrisoners(SiegeCastleActionKind action)
    {
        return IsProposal(action)
            || IsRegularPrisonerTerminal(action)
            || IsProcess(action);
    }

    public static bool IsSettlement(SiegeCastleActionKind action)
    {
        return action != SiegeCastleActionKind.Unknown
            && action != SiegeCastleActionKind.SoldierDiscontent
            && !IsProposal(action);
    }

    public static bool CanRegularPrisonerPropose(SiegeCastleActionKind action)
    {
        return action == SiegeCastleActionKind.ProposeRecruitPrisoners
            || action == SiegeCastleActionKind.ProposeReleasePrisoners
            || action == SiegeCastleActionKind.ProposeLaborPrisoners
            || action == SiegeCastleActionKind.ProposeInstructorPrisoners;
    }

    public static bool CanAlliedSoldierExecute(SiegeCastleActionKind action)
    {
        return IsProcess(action)
            || (IsRegularPrisonerTerminal(action) && !IsVoluntary(action));
    }
}
