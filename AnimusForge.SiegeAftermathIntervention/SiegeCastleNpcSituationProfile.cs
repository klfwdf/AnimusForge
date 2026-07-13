namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only role facts injected into AF conversations during the active aftermath stage.
/// Bannerlord adapters resolve live agent identity and battle provenance.
/// </summary>
public static class SiegeCastleNpcSituationProfile
{
    public const string DefaultCastleName = "这座城堡";

    public const string DefaultPlayerName = "玩家";

    public static string BuildAlliedSoldierPrompt(string castleName, string playerName)
    {
        castleName = Normalize(castleName, DefaultCastleName);
        playerName = Normalize(playerName, DefaultPlayerName);
        return "【城堡战后身份·最高优先级】你是" + playerName + "从攻城胜利部队中亲自挑选并带入"
            + castleName + "的己方随行士兵。你参加了胜利方的攻城行动，现在仍受" + playerName
            + "直接指挥；你不是本城战败守军、俘虏或中立旁观者。";
    }

    public static string BuildDefeatedGarrisonPrisonerPrompt(string castleName, string playerName)
    {
        castleName = Normalize(castleName, DefaultCastleName);
        playerName = Normalize(playerName, DefaultPlayerName);
        return "【城堡战后身份·最高优先级】你按" + castleName + "守城战中被击败并缴械的普通守军处理。"
            + playerName + "是攻下城堡的胜利方首领和当前控制者；你是等待处置的战俘，不是他的部下或友军。"
            + "即使场景为了押解和站位把你放入玩家可指挥编队，也不代表你已被收编。你应承认守城失败、行动受限、命运尚待决定，不能把这里说成阅兵或普通检阅。";
    }

    public static string BuildCurrentCastleDefeatedLordPrompt(string castleName, string playerName)
    {
        castleName = Normalize(castleName, DefaultCastleName);
        playerName = Normalize(playerName, DefaultPlayerName);
        return "【城堡战后身份·最高优先级】本次围城参战记录确认：你作为敌方贵族在"
            + castleName + "的攻守战中被" + playerName + "一方击败并成为俘虏。现在你被带到刚陷落的城堡内等待处置；"
            + playerName + "是胜利方首领和当前控制者。你可以愤怒、不甘、傲慢、求饶或谈判，但不能否认自己在本城战败被俘。";
    }

    public static string BuildPreviouslyPlayerDefeatedLordPrompt(string castleName, string playerName)
    {
        castleName = Normalize(castleName, DefaultCastleName);
        playerName = Normalize(playerName, DefaultPlayerName);
        return "【城堡战后身份·最高优先级】AF既有战斗记录确认" + playerName
            + "此前曾在野外或另一场战斗中击败过你；但本次围城参战记录没有把你列为" + castleName + "的战败者。"
            + "你是此前已经落入胜利方手中的敌方贵族俘虏，现在被带入刚攻下的城堡等待处置。不要编造自己刚在本城守城战中被击败。";
    }

    public static string BuildPreviouslyCapturedLordPrompt(string castleName, string playerName)
    {
        castleName = Normalize(castleName, DefaultCastleName);
        playerName = Normalize(playerName, DefaultPlayerName);
        return "【城堡战后身份·最高优先级】当前可靠记录不支持你是在" + castleName + "的本次守城战中被击败。"
            + "你在进入此场景前已经是" + playerName + "一方控制的敌方贵族俘虏，现在被带入刚攻下的城堡等待处置。"
            + "你必须承认被俘和行动受限，但不要编造具体战败地点或声称自己是本城刚刚战败的守城领主。";
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
