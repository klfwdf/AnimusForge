namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only morale consequence created by recruiting defeated garrison prisoners.
/// </summary>
public static class SiegeCastleSoldierReactionProfile
{
    public const int UnappeasedMoralePenalty = 30;

    public const uint NeedMessageColor = 0xFFFFD27Fu;

    public const uint AppeasedMessageColor = 0xFFB6F7A8u;

    public const uint PenaltyMessageColor = 0xFFFF7A7Au;

    public const string NeedMemoryTitle = "城堡收编后的军心不满";

    public const string AppeasementMemoryTitle = "城堡安兵";

    public const string PenaltyMemoryTitle = "城堡收编军心受损";

    public static bool ShouldRequireAppeasement(int recruitedRegularPrisoners, int alliedSoldiersPresent)
    {
        return recruitedRegularPrisoners > 0 && alliedSoldiersPresent > 0;
    }

    public static string BuildNeedMessage(int recruitedRegularPrisoners)
    {
        return "【城堡处置】收编 " + Clamp(recruitedRegularPrisoners)
            + " 名战俘引起己方士兵不满；离场前直接安抚己方士兵，否则部队士气 -30。";
    }

    public static string BuildNeedMemoryText(int recruitedRegularPrisoners)
    {
        return "玩家收编了 " + Clamp(recruitedRegularPrisoners)
            + " 名战败守军，随行士兵对此不满；若离场前未安抚，部队士气将降低 30。";
    }

    public static string BuildAppeasementMessage()
    {
        return "【城堡处置】己方士兵接受了玩家的解释与军令，本次收编不会扣除士气。";
    }

    public static string BuildAppeasementMemoryText()
    {
        return "玩家在城堡处置现场直接安抚了因收编战俘而不满的随行士兵，避免了士气损失。";
    }

    public static string BuildPenaltyMessage()
    {
        return "【城堡处置】收编战俘后未能安抚随行士兵，部队士气 -30。";
    }

    public static string BuildPenaltyMemoryText()
    {
        return "玩家收编战俘后离开城堡，未在现场安抚随行士兵，部队士气降低 30。";
    }

    private static int Clamp(int value)
    {
        return value < 0 ? 0 : value;
    }
}
