namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for local civilian robbery during the active GCCZ scene.
/// AF adapters apply live Bannerlord gold, item, settlement, and relation side effects.
/// </summary>
public static class SiegeCivilianRobberyProfile
{
    public const string ActionName = "Extortion";
    public const string ActionNameEn = "Extortion";
    
    public const string MarketInventoryLootReasonEn = "demand supplies by robbery";
    public const string MemoryTitleEn = "Robbery";
    public const string GoldMemoryTextEn = "The player demanded denars from the defeated civilians or notables; this is local robbery and does not trigger native pillage.";
    public const string GoodsMemoryTextEn = "The player demanded goods from the defeated civilians or notables; this is local supply seizure and does not trigger native pillage.";

    public static string GetActionName(bool isEnglish) => isEnglish ? ActionNameEn : ActionName;
    public static string GetMarketInventoryLootReason(bool isEnglish) => isEnglish ? MarketInventoryLootReasonEn : MarketInventoryLootReason;
    public static string GetMemoryTitle(bool isEnglish) => isEnglish ? MemoryTitleEn : MemoryTitle;
    public static string GetGoldMemoryText(bool isEnglish) => isEnglish ? GoldMemoryTextEn : GoldMemoryText;
    public static string GetGoodsMemoryText(bool isEnglish) => isEnglish ? GoodsMemoryTextEn : GoodsMemoryText;

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

    public const string MarketInventoryLootReason = "Extortion of supplies";

    public const string MemoryTitle = "Extortion";
    
    public const string GoldMemoryText = "The player demanded denars from the defeated civilian or notable; this is local extortion, not vanilla plunder.";

    public const string GoodsMemoryText = "The player demanded supplies from the defeated civilian or notable; this is local extortion, not vanilla plunder.";

}
