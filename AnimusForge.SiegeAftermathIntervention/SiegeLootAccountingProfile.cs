namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI wording for GCCZ loot accounting.
/// AF adapters still own Bannerlord gold/item mutation, target selection, and display side effects.
/// </summary>
public static class SiegeLootAccountingProfile
{
    public const uint LootMessageColor = 0xFFFFC46Bu;

    public const uint DirectDevastateSettlementMessageColor = 0xFFFF7777u;

    public const uint DirectPlunderSettlementMessageColor = 0xFFFFC46Bu;

    public const string MassacreActionName = "血洗";

    public const string CulturalRepopulationActionName = "屠民迁殖";

    public const string PlunderSettlementLootReason = "搜掠结算";

    public const string MassacreSettlementLootReason = "血洗结算";

    public const string CivilianHeroFallbackGoldSource = "civilian_hero_fallback";

    public const string CivilianFlatGoldSource = "civilian_flat";

    public const string MarketGoldSource = "market_gold";

    public const int NonHeroPlunderMinGold = 5;

    public const int NonHeroPlunderMaxGold = 9;

    public const int NonHeroMassacreGold = 10;

    public const int HeroMassacreFallbackGold = 3000;

    public const float PlunderMarketInventoryMinRatio = 0.20f;

    public const float PlunderMarketInventoryMaxRatio = 0.50f;

    public const float MassacreMarketInventoryMinRatio = 0.90f;

    public const float MassacreMarketInventoryMaxRatio = 1.00f;

    public static string BuildDirectDevastateSettlementMessage(string actionName)
    {
        return "【攻城处置】" + NormalizeActionName(actionName, "血洗")
            + "已按毁坏处置结算；城镇受到进一步毁坏影响。即将进入战利品界面领取截获物资。";
    }

    public static string BuildDirectPlunderSettlementMessage()
    {
        return "【攻城处置】搜掠已按掠夺处置结算；即将进入战利品界面领取截获物资。";
    }

    public static string BuildLootCreditedSummaryMessage(int marketGold, int civilianGold, int itemTotal, int stackKinds)
    {
        return "【战利清点】金钱已入账：市场金库 " + ClampNonNegative(marketGold)
            + "，民众第纳尔 " + ClampNonNegative(civilianGold)
            + "；物资 " + ClampNonNegative(itemTotal)
            + " 件 / " + ClampNonNegative(stackKinds)
            + " 类。";
    }

    public static string BuildCivilianExitSettlementMessage(int recordedCivilianCount, int lootedCivilianCount, int gainedGold)
    {
        return "【战利清点】本次入城处置共记录 " + ClampNonNegative(recordedCivilianCount)
            + " 名普通民众；离场时结算剩余 " + ClampNonNegative(lootedCivilianCount)
            + " 名，共新增 " + ClampNonNegative(gainedGold)
            + " 第纳尔。";
    }

    public static string BuildCivilianLootMessage(string actorName, string targetName, int amount)
    {
        return "【战利清点】" + BuildCivilianLootLine(actorName, targetName, amount);
    }

    public static string BuildMarketGoldMessage(string reason, int amount)
    {
        return "【战利清点】" + NormalizeReason(reason) + "市场金库：获得 " + ClampNonNegative(amount) + " 第纳尔。";
    }

    public static string BuildMarketInventoryMessage(string reason, int itemTotal, int stackKinds, int itemValue)
    {
        return "【战利清点】" + NormalizeReason(reason) + "市场库存：截获 " + ClampNonNegative(itemTotal)
            + " 件货物（" + ClampNonNegative(stackKinds)
            + " 类，估值 " + ClampNonNegative(itemValue)
            + "）；离场后进入战利品界面领取。";
    }

    public static string BuildCivilianSpoilsMessage(int gold)
    {
        return "【战利清点】民众财物：取得 " + ClampNonNegative(gold) + " 第纳尔。";
    }

    public static string BuildCivilianLootLine(string actorName, string targetName, int amount)
    {
        string safeTargetName = string.IsNullOrWhiteSpace(targetName) ? "目标" : targetName.Trim();
        int safeAmount = ClampNonNegative(amount);
        if (string.IsNullOrWhiteSpace(actorName))
        {
            return "从 " + safeTargetName + " 取得 " + safeAmount + " 第纳尔。";
        }

        return actorName.Trim() + " 盘问 " + safeTargetName + " 后取得 " + safeAmount + " 第纳尔。";
    }

    private static string NormalizeReason(string reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
    }

    private static string NormalizeActionName(string actionName, string fallback)
    {
        return string.IsNullOrWhiteSpace(actionName) ? fallback : actionName.Trim();
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }
}
