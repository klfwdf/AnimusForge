namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free limits and wording for the castle aftermath member/prisoner selector.
/// Bannerlord PartyScreen construction and roster snapshots remain in the fused AF adapter.
/// </summary>
public static class SiegeCastleRosterSelectionProfile
{
    public const int MaxAlliedTroops = 50;

    public const int MaxPrisoners = 200;

    public const int RegularPrisonerFormationIndex = 6;

    public const int LordPrisonerFormationIndex = 7;

    public const int PrisonerSpawnBatchSize = 20;

    public const string AvailableRosterTitle = "可选随行士兵 / 战俘";

    public const string SelectedRosterTitle = "城堡处置队伍";

    public const string ScreenHeader = "选择进入城堡的士兵与俘虏";

    public const string SelectionCanceledSource = "castle_roster_selection_cancel";

    public const string SelectionFailedSource = "castle_roster_selection_failed";

    public static bool IsWithinLimits(int alliedTroopCount, int prisonerCount)
    {
        return alliedTroopCount >= 0
            && prisonerCount >= 0
            && alliedTroopCount <= MaxAlliedTroops
            && prisonerCount <= MaxPrisoners;
    }

    public static int ClampAlliedTroopCount(int count)
    {
        return Clamp(count, MaxAlliedTroops);
    }

    public static int ClampPrisonerCount(int count)
    {
        return Clamp(count, MaxPrisoners);
    }

    public static string BuildInstructionMessage()
    {
        return "【城堡处置】选择最多 " + MaxAlliedTroops + " 名我方士兵和 " + MaxPrisoners + " 名俘虏进入城堡；俘虏可包含被俘领主。";
    }

    public static string BuildConfirmedMessage(int alliedTroopCount, int prisonerCount)
    {
        return "【城堡处置】已选择 " + ClampAlliedTroopCount(alliedTroopCount)
            + " 名我方士兵、" + ClampPrisonerCount(prisonerCount) + " 名俘虏。";
    }

    public static string BuildLimitMessage(int alliedTroopCount, int prisonerCount)
    {
        if (alliedTroopCount > MaxAlliedTroops)
        {
            return "我方随行士兵不能超过 " + MaxAlliedTroops + " 人。";
        }

        if (prisonerCount > MaxPrisoners)
        {
            return "带入城堡的俘虏不能超过 " + MaxPrisoners + " 人。";
        }

        return string.Empty;
    }

    private static int Clamp(int count, int max)
    {
        if (count <= 0)
        {
            return 0;
        }

        return count > max ? max : count;
    }
}
