namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for local civilian robbery during the active GCCZ scene.
/// AF adapters apply live Bannerlord gold, item, settlement, and relation side effects.
/// </summary>
public static class SiegeCivilianRobberyProfile
{
    public const string ActionName = "抢钱";

    public const int CommonerMinGold = SiegeLootAccountingProfile.NonHeroPlunderMinGold;

    public const int CommonerMaxGold = SiegeLootAccountingProfile.NonHeroPlunderMaxGold;

    public const float HeroGoldMinRatio = 0.50f;

    public const float HeroGoldMaxRatio = 0.75f;

    public const float MerchantGoldMinRatio = 0.10f;

    public const float MerchantGoldMaxRatio = 0.30f;

    public const int HeroFallbackGoldMin = 300;

    public const int HeroFallbackGoldMax = 750;

    public const float MarketInventoryMinRatio = 0.10f;

    public const float MarketInventoryMaxRatio = 0.30f;

    public const string MarketInventoryLootReason = "抢钱索取物资";

    public const string MemoryTitle = "抢钱";

    public const string GoldMemoryText = "玩家向当前战败民众或要人索取第纳尔；这是局部抢钱，不触发原版掠夺。";

    public const string GoodsMemoryText = "玩家向当前战败民众或要人索取物资；这是局部抢物资，不触发原版掠夺。";

}
