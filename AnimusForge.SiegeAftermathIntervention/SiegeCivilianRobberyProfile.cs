namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for local civilian robbery during the active GCCZ scene.
/// AF adapters apply live Bannerlord gold, item, settlement, and relation side effects.
/// </summary>
public static class SiegeCivilianRobberyProfile
{
    public const string ActionName = "抢钱";

    public const string LocalPenaltyKey = "civilian_robbery";

    public const string FullPillagePenaltyKey = "civilian_robbery_full_pillage";

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

    public const int LocalSettlementPublicTrustDelta = -20;

    public const int LocalBoundVillagePublicTrustDelta = -10;

    public const int LocalNotableRelationDelta = -15;

    public const int FullPillagePenaltyRobbedTargetThreshold = 6;

    public const string LocalSettlementPublicTrustReason = "siege_ai_civilian_robbery_settlement";

    public const string LocalBoundVillagePublicTrustReason = "siege_ai_civilian_robbery_bound_village";

    public const string LocalNotableRelationReason = "siege_ai_civilian_robbery_notables";

    public const string EscalatedSettlementPublicTrustReason = "siege_ai_civilian_robbery_escalated_settlement";

    public const string EscalatedBoundVillagePublicTrustReason = "siege_ai_civilian_robbery_escalated_bound_village";

    public const string EscalatedNotableRelationReason = "siege_ai_civilian_robbery_escalated_notables";

    public const string MarketInventoryLootReason = "抢钱索取物资";

    public const string MemoryTitle = "抢钱";

    public const string GoldMemoryText = "玩家向当前战败民众或要人索取第纳尔；这是局部抢钱，不触发原版掠夺。";

    public const string GoodsMemoryText = "玩家向当前战败民众或要人索取物资；这是局部抢物资，不触发原版掠夺。";

    public static bool ShouldEscalateToFullPillagePenalty(int robbedTargetCount)
    {
        return robbedTargetCount >= FullPillagePenaltyRobbedTargetThreshold;
    }

    public static int EscalatedSettlementPublicTrustDelta => SiegeSettlementOutcomeProfile.BuildPlunder().SettlementPublicTrustDelta - LocalSettlementPublicTrustDelta;

    public static int EscalatedBoundVillagePublicTrustDelta => SiegeSettlementOutcomeProfile.BuildPlunder().BoundVillagePublicTrustDelta - LocalBoundVillagePublicTrustDelta;

    public static int EscalatedNotableRelationDelta => SiegeSettlementOutcomeProfile.BuildPlunder().NotableRelationDelta - LocalNotableRelationDelta;
}
