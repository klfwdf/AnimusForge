namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free final effect intent for castle GCCZ.
/// It records what castle-specific systems should apply after the mission without borrowing
/// town civilian trust, town market plunder, or civilian massacre side effects.
/// </summary>
public static class SiegeCastleFinalEffectProfile
{
    public const string MemoryTitle = "城堡最终军务";

    public const string DiagnosticCategory = "CastleFinalEffect";

    public const string StableOccupationLabel = "稳定占领";

    public const string DestructiveOccupationLabel = "毁灭军务";

    public const string TownMarketLootSkippedReason = "castle_aftereffect_uses_armory_not_town_market";

    public static SiegeCastleFinalEffectPlan BuildPlan(SiegeCastleAftermathStateSnapshot state, bool finalAftermathIsDestructive)
    {
        bool destructive = finalAftermathIsDestructive || state.DestructiveOrderIssued || state.IrreversibleOrderLocked;
        int recruitPercent = ClampPercent(state.RecruitablePrisonerPercent);
        int laborPercent = SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(state.LaborPrisonerPercent);
        int ransomWeight = destructive ? Min(state.RansomGoldWeight, 0) : state.RansomGoldWeight;
        int armoryWeight = destructive ? Min(state.ArmoryReceiptWeight, 0) : state.ArmoryReceiptWeight;
        return new SiegeCastleFinalEffectPlan(
            destructive ? DestructiveOccupationLabel : StableOccupationLabel,
            state.LordRelationDelta,
            state.CastleLoyaltyDelta,
            state.CastleSecurityDelta,
            state.BoundVillageProductionBonusPercent,
            recruitPercent,
            laborPercent,
            ransomWeight,
            armoryWeight,
            state.CaptiveFearDelta,
            state.PlayerTroopMoraleDelta,
            state.PlayerTroopXpWeight,
            destructive,
            skipTownMarketLoot: true);
    }

    public static string BuildMemoryText(SiegeCastleFinalEffectPlan plan, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return safeCastleName + "城堡战后军务结算：" + plan.OutcomeLabel
            + "；领主好感 " + FormatSigned(plan.LordRelationDelta)
            + "，城堡忠诚 " + FormatSigned(plan.CastleLoyaltyDelta)
            + "，城堡治安 " + FormatSigned(plan.CastleSecurityDelta)
            + "，附属村庄产出 " + FormatSigned(plan.BoundVillageProductionBonusPercent) + "%"
            + "，可收编俘虏 " + ClampPercent(plan.RecruitablePrisonerPercent) + "%"
            + "，劳役战俘 " + SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(plan.LaborPrisonerPercent) + "%"
            + "，赎金权重 " + FormatSigned(plan.RansomGoldWeight)
            + "，军械接收 " + FormatSigned(plan.ArmoryReceiptWeight)
            + "，俘虏恐惧 " + FormatSigned(plan.CaptiveFearDelta)
            + "，己方士气 " + FormatSigned(plan.PlayerTroopMoraleDelta)
            + "，部队经验权重 " + FormatSigned(plan.PlayerTroopXpWeight)
            + "；城堡最终效果使用军械库/战俘/守军渠道，不触发城镇市场搜掠。";
    }

    public static string BuildDiagnosticText(SiegeCastleFinalEffectPlan plan, string castleId)
    {
        return "castle=" + (string.IsNullOrWhiteSpace(castleId) ? "N/A" : castleId.Trim())
            + " outcome=" + plan.OutcomeLabel
            + " lordRelation=" + plan.LordRelationDelta
            + " castleLoyalty=" + plan.CastleLoyaltyDelta
            + " castleSecurity=" + plan.CastleSecurityDelta
            + " villageProduction=" + plan.BoundVillageProductionBonusPercent
            + " recruitPrisonerPercent=" + ClampPercent(plan.RecruitablePrisonerPercent)
            + " laborPrisonerPercent=" + SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(plan.LaborPrisonerPercent)
            + " ransomWeight=" + plan.RansomGoldWeight
            + " armoryWeight=" + plan.ArmoryReceiptWeight
            + " captiveFear=" + plan.CaptiveFearDelta
            + " playerTroopMorale=" + plan.PlayerTroopMoraleDelta
            + " playerTroopXpWeight=" + plan.PlayerTroopXpWeight
            + " destructive=" + plan.Destructive
            + " skipTownMarketLoot=" + plan.SkipTownMarketLoot;
    }

    public static bool ShouldSkipTownMarketLoot(bool isCastleAftermath, SiegeCastleFinalEffectPlan plan)
    {
        return isCastleAftermath && plan.SkipTownMarketLoot;
    }

    private static int ClampPercent(int value)
    {
        return value < 0 ? 0 : (value > SiegeCastleAftermathProfile.MaxRecruitmentPercent ? SiegeCastleAftermathProfile.MaxRecruitmentPercent : value);
    }

    private static int Min(int left, int right)
    {
        return left < right ? left : right;
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}

public readonly struct SiegeCastleFinalEffectPlan
{
    public SiegeCastleFinalEffectPlan(
        string outcomeLabel,
        int lordRelationDelta,
        int castleLoyaltyDelta,
        int castleSecurityDelta,
        int boundVillageProductionBonusPercent,
        int recruitablePrisonerPercent,
        int laborPrisonerPercent,
        int ransomGoldWeight,
        int armoryReceiptWeight,
        int captiveFearDelta,
        int playerTroopMoraleDelta,
        int playerTroopXpWeight,
        bool destructive,
        bool skipTownMarketLoot)
    {
        OutcomeLabel = outcomeLabel ?? string.Empty;
        LordRelationDelta = lordRelationDelta;
        CastleLoyaltyDelta = castleLoyaltyDelta;
        CastleSecurityDelta = castleSecurityDelta;
        BoundVillageProductionBonusPercent = boundVillageProductionBonusPercent;
        RecruitablePrisonerPercent = recruitablePrisonerPercent;
        LaborPrisonerPercent = laborPrisonerPercent;
        RansomGoldWeight = ransomGoldWeight;
        ArmoryReceiptWeight = armoryReceiptWeight;
        CaptiveFearDelta = captiveFearDelta;
        PlayerTroopMoraleDelta = playerTroopMoraleDelta;
        PlayerTroopXpWeight = playerTroopXpWeight;
        Destructive = destructive;
        SkipTownMarketLoot = skipTownMarketLoot;
    }

    public string OutcomeLabel { get; }

    public int LordRelationDelta { get; }

    public int CastleLoyaltyDelta { get; }

    public int CastleSecurityDelta { get; }

    public int BoundVillageProductionBonusPercent { get; }

    public int RecruitablePrisonerPercent { get; }

    public int LaborPrisonerPercent { get; }

    public int RansomGoldWeight { get; }

    public int ArmoryReceiptWeight { get; }

    public int CaptiveFearDelta { get; }

    public int PlayerTroopMoraleDelta { get; }

    public int PlayerTroopXpWeight { get; }

    public bool Destructive { get; }

    public bool SkipTownMarketLoot { get; }
}
