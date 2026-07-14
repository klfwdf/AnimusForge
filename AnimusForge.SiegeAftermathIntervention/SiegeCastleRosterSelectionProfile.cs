using System;

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

    public const string AlliedSpawnCommandSource = "castle_allied_spawn";

    public const string AlliedCommandUiRefreshSource = "castle_allied_spawn_complete";

    public const int AlliedInitialGridColumns = 8;

    public const float AlliedInitialStartDepth = 4f;

    public const float AlliedInitialRowSpacing = 1.8f;

    public const float AlliedInitialLateralSpacing = 1.6f;

    public const string PrisonerSpawnCommandSource = "castle_prisoner_spawn";

    public const string PrisonerCommandUiRefreshSource = "castle_prisoner_spawn_complete";

    public const string AvailableRosterTitle = "可选随行士兵 / 战俘";

    public const string SelectedRosterTitle = "城堡处置队伍";

    public const string ScreenHeader = "选择进入城堡的士兵与俘虏";

    public const string SelectionCanceledSource = "castle_roster_selection_cancel";

    public const string SelectionFailedSource = "castle_roster_selection_failed";

    public const string DecisionPolicyMessage = "【城堡处置】进入场景后可与守城战败士兵和被俘领主交涉，并对战俘下达收编或屠戮命令；未明确处置直接离场时默认按宽恕结算。城镇民众的搜掠、宣抚、血洗和迁殖规则不适用于城堡。";

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

    public static int ResolveSelectedStackWounded(
        int originalNumber,
        int originalWounded,
        int holdingNumber,
        int holdingWounded)
    {
        int safeOriginalNumber = Math.Max(0, originalNumber);
        int safeHoldingNumber = Math.Min(safeOriginalNumber, Math.Max(0, holdingNumber));
        int selectedNumber = safeOriginalNumber - safeHoldingNumber;
        int safeOriginalWounded = Math.Min(safeOriginalNumber, Math.Max(0, originalWounded));
        int safeHoldingWounded = Math.Min(safeHoldingNumber, Math.Max(0, holdingWounded));
        return Math.Min(selectedNumber, Math.Max(0, safeOriginalWounded - safeHoldingWounded));
    }

    public static int ResolveSelectedStackXp(int originalXp, int holdingXp)
    {
        return Math.Max(0, Math.Max(0, originalXp) - Math.Max(0, holdingXp));
    }

    public static string BuildInstructionMessage()
    {
        return "【城堡处置】选择最多 " + MaxAlliedTroops + " 名我方士兵和 " + MaxPrisoners + " 名俘虏进入城堡；俘虏可包含被俘领主。";
    }

    public static string BuildDecisionPolicyMessage()
    {
        return DecisionPolicyMessage;
    }

    public static string BuildConfirmedMessage(int alliedTroopCount, int prisonerCount)
    {
        return "【城堡处置】已选择 " + ClampAlliedTroopCount(alliedTroopCount)
            + " 名我方士兵、" + ClampPrisonerCount(prisonerCount) + " 名俘虏。";
    }

    public static string BuildPrisonerSceneReadyMessage(int selectedCount, int activeCount)
    {
        int selected = ClampPrisonerCount(selectedCount);
        int active = ClampPrisonerCount(activeCount);
        if (active >= selected)
        {
            return "【城堡处置】已带入 " + active + " 名俘虏，可通过 7 号俘虏编队下令。";
        }

        return "【城堡处置】俘虏实际进入场景 " + active + "/" + selected + " 名；详情已写入日志。";
    }

    public static string BuildAlliedSceneReadyMessage(int selectedCount, int activeCount)
    {
        int selected = ClampAlliedTroopCount(selectedCount);
        int active = ClampAlliedTroopCount(activeCount);
        if (active >= selected)
        {
            return "【城堡处置】已带入 " + active + " 名我方士兵，可通过原版指挥系统下令。";
        }

        return "【城堡处置】我方士兵实际进入场景 " + active + "/" + selected + " 名；详情已写入日志。";
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
