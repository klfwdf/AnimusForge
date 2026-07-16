namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only morale consequence for controversial prisoner treatment.
/// </summary>
public static class SiegeCastleSoldierReactionProfile
{
    public const int UnappeasedMoralePenalty = 30;

    public const uint NeedMessageColor = 0xFFFFD27Fu;

    public const uint AppeasedMessageColor = 0xFFB6F7A8u;

    public const uint PenaltyMessageColor = 0xFFFF7A7Au;

    public const string NeedMemoryTitle = "城堡战俘处置后的军心不满";

    public const string AppeasementMemoryTitle = "城堡安兵";

    public const string PenaltyMemoryTitle = "城堡战俘处置军心受损";

    public static bool ShouldRequireAppeasement(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int alliedSoldiersPresent,
        bool alliedCultureMatchesPrisoners)
    {
        if (affectedRegularPrisoners <= 0 || alliedSoldiersPresent <= 0)
        {
            return false;
        }
        return action == SiegeCastleActionKind.RecruitPrisonersVoluntary
            || action == SiegeCastleActionKind.RecruitPrisonersForced
            || action == SiegeCastleActionKind.LaborPrisonersForced
            || action == SiegeCastleActionKind.InstructorPrisonersForced
            || (action == SiegeCastleActionKind.SlaughterPrisoners
                && (alliedCultureMatchesPrisoners || affectedRegularPrisoners >= 20))
            || (action == SiegeCastleActionKind.SellPrisoners && alliedCultureMatchesPrisoners);
    }

    public static string BuildNeedMessage(SiegeCastleActionKind action, int affectedRegularPrisoners)
    {
        return "【城堡处置】" + DescribeConcernAction(action) + " " + Clamp(affectedRegularPrisoners)
            + " 名战俘引起随军士兵不满；离场前直接安抚，否则部队士气 -30（多项不满不叠加）。";
    }

    public static string BuildNeedMemoryText(SiegeCastleActionKind action, int affectedRegularPrisoners)
    {
        return "玩家在城堡处置现场以“" + DescribeConcernAction(action) + "”处理了 "
            + Clamp(affectedRegularPrisoners)
            + " 名战败守军，随行士兵对此不满；若离场前未安抚，部队士气降低 30，多个原因不叠加。";
    }

    public static string BuildAppeasementMessage()
    {
        return "【城堡处置】己方士兵接受了玩家的解释、补偿与军令，本次战俘处置不会额外扣除士气。";
    }

    public static string BuildAppeasementMemoryText()
    {
        return "玩家在城堡处置现场直接安抚了因战俘处置而不满的随行士兵，避免了额外士气损失。";
    }

    public static string BuildPenaltyMessage()
    {
        return "【城堡处置】战俘处置后未能安抚随行士兵，部队士气 -30。";
    }

    public static string BuildPenaltyMemoryText()
    {
        return "玩家完成战俘处置后离开城堡，未在现场安抚随行士兵，部队士气降低 30。";
    }

    private static int Clamp(int value)
    {
        return value < 0 ? 0 : value;
    }

    private static string DescribeConcernAction(SiegeCastleActionKind action)
    {
        return action switch
        {
            SiegeCastleActionKind.SlaughterPrisoners => "屠戮",
            SiegeCastleActionKind.SellPrisoners => "贩卖",
            SiegeCastleActionKind.LaborPrisonersForced => "强制劳役",
            SiegeCastleActionKind.InstructorPrisonersForced => "强迫担任教官",
            SiegeCastleActionKind.RecruitPrisonersVoluntary => "收编",
            SiegeCastleActionKind.RecruitPrisonersForced => "强制收编",
            _ => "处置"
        };
    }
}
