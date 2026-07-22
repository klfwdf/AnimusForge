namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free wording and relation policy for SETS incidents inside player-owned
/// or ruler-attached settlements. AF adapters own Bannerlord menu registration and relation calls.
/// </summary>
public static class SetsOwnedSettlementIncidentProfile
{
    public const string MenuId = "AnimusForge_sets_owned_town_incident";

    public const string EntryOptionId = "AnimusForge_sets_owned_town_incident_enter";

    public const string LeaveOptionId = "AnimusForge_sets_owned_town_incident_leave";

    public const string EntryOptionText = "亲自进城决定";

    public const string LeaveOptionText = "收手离开";

    public const uint MessageColor = 0xFFB6F7A8u;

    public const uint WarningColor = 0xFFFF7777u;

    public const int MinorIncidentRelationPenalty = -10;

    public const int NotableKilledRelationPenalty = -30;

    public const int PlunderRelationPenalty = -50;

    public const int MassacreRelationPenalty = -70;

    public const int CulturalRepopulationRelationPenalty = -100;

    public static int ResolveOwnerRelationPenalty(
        SiegeAftermathResolutionKind aftermath,
        bool notableKilled,
        bool culturalRepopulation)
    {
        if (aftermath == SiegeAftermathResolutionKind.Pillage)
        {
            return PlunderRelationPenalty;
        }

        if (aftermath == SiegeAftermathResolutionKind.Devastate)
        {
            return culturalRepopulation
                ? CulturalRepopulationRelationPenalty
                : MassacreRelationPenalty;
        }

        return notableKilled
            ? NotableKilledRelationPenalty
            : MinorIncidentRelationPenalty;
    }

    public static int ResolveAdditionalPenaltyAfterNative(int desiredTotalPenalty, int nativePenalty)
    {
        int additionalPenalty = desiredTotalPenalty - nativePenalty;
        return additionalPenalty < 0 ? additionalPenalty : 0;
    }

    public static string BuildMenuText(string playerName, string settlementName)
    {
        return BuildMenuText(SetsSettlementSceneKind.Town, playerName, settlementName);
    }

    public static string BuildMenuText(SetsSettlementSceneKind kind, string playerName, string settlementName)
    {
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind);
        return NormalizeName(playerName, "玩家") + "在" + NormalizeName(settlementName, "这处" + noun) + "大开杀戒。接下来，剩余居民的命运由你决定。";
    }

    public static string BuildEntryInstruction()
    {
        return BuildEntryInstruction(SetsSettlementSceneKind.Town);
    }

    public static string BuildEntryInstruction(SetsSettlementSceneKind kind)
    {
        return "【SETS】这是自有/附属" + SetsSettlementEntryProfile.GetSettlementNoun(kind) + "事件：你可亲自进入继续处置，也可收手离开。原版毁坏、掠夺、宽恕选项已隐藏。";
    }

    public static string BuildLeaveMessage()
    {
        return BuildLeaveMessage(SetsSettlementSceneKind.Town);
    }

    public static string BuildLeaveMessage(SetsSettlementSceneKind kind)
    {
        return "【SETS】你收手离开，没有再处置" + SetsSettlementEntryProfile.GetSettlementNoun(kind) + "内的剩余居民。";
    }

    public static string BuildRelationPenaltyMessage(int penalty)
    {
        return "【SETS】附属领地所有者因你的处置与玩家关系变化 " + penalty + "。";
    }

    public static string BuildRuntimeContext(string playerName, string settlementName, bool alliedSoldier, bool civilian)
    {
        return BuildRuntimeContext(SetsSettlementSceneKind.Town, playerName, settlementName, alliedSoldier, civilian);
    }

    public static string BuildRuntimeContext(SetsSettlementSceneKind kind, string playerName, string settlementName, bool alliedSoldier, bool civilian)
    {
        string player = NormalizeName(playerName, "玩家");
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind);
        string settlement = NormalizeName(settlementName, "这处" + noun);
        string context = "【SETS自有/附属" + noun + "事件】当前不是普通攻城胜利后的陌生敌方定居点处置，而是" + player + "已经拥有或以国王身份统治的" + settlement + "内部事件。"
            + player + "仍是现场最高命令来源，但居民是其领民；若这是附属领主领地，所有者会因玩家的伤害、搜掠或血洗而怨恨玩家。";
        if (alliedSoldier)
        {
            context += "【己方士兵特殊认知】你是玩家带入" + noun + "的士兵/同伴，不是当地守军。玩家若命令搜掠或血洗自己的领民，你仍会服从直接命令，但应表现出震惊、迟疑、压低声音、担忧名声和附属领主反应；不要把这当成普通敌方战利权，也不要自行升级处置。";
        }
        if (civilian)
        {
            context += "【领民认知】你知道玩家是这处" + noun + "的主人或国王，不是路过强盗；你可以恐惧、求饶或控诉，但不要否认玩家的统治权。";
        }
        return context;
    }

    private static string NormalizeName(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
