using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free side-effect plan for castle GCCZ finalization.
/// The plan converts castle military state into safe game-facing numbers for future bridge-side
/// application without directly modifying Bannerlord rosters, gold, prisoners, walls, or market inventory.
/// </summary>
public static class SiegeCastleFinalSideEffectProfile
{
    public const string MemoryTitle = "城堡副作用计划";

    public const string DiagnosticCategory = "CastleFinalSideEffects";

    public const int MaxRecruitablePrisonerCount = SiegeCastleSceneRosterProfile.MaxSelectedPrisoners;

    public const int MaxLaborPrisonerCount = 120;

    public const int MaxCarriedRegularPrisonerSourceCount = SiegeCastleSceneRosterProfile.MaxSelectedPrisoners;

    public const int RansomGoldPerWeightPerLord = 150;

    public const int MaxRansomGold = 60000;

    public static SiegeCastleFinalSideEffectPlan BuildPlan(
        SiegeCastleFinalEffectPlan finalEffect,
        int surrenderedGarrisonSourceCount,
        int captiveLordSourceCount)
    {
        return BuildPlan(finalEffect, surrenderedGarrisonSourceCount, carriedRegularPrisonerSourceCount: 0, captiveLordSourceCount);
    }

    public static SiegeCastleFinalSideEffectPlan BuildPlan(
        SiegeCastleFinalEffectPlan finalEffect,
        int surrenderedGarrisonSourceCount,
        int carriedRegularPrisonerSourceCount,
        int captiveLordSourceCount)
    {
        return BuildPlan(finalEffect, surrenderedGarrisonSourceCount, carriedRegularPrisonerSourceCount, captiveLordSourceCount, default);
    }

    public static SiegeCastleFinalSideEffectPlan BuildPlan(
        SiegeCastleFinalEffectPlan finalEffect,
        int surrenderedGarrisonSourceCount,
        int carriedRegularPrisonerSourceCount,
        int captiveLordSourceCount,
        SiegeCastlePrisonerAllocationPlan explicitAllocation)
    {
        int safeGarrison = Math.Max(0, surrenderedGarrisonSourceCount);
        int safeCarriedRegularPrisoners = ClampCarriedRegularPrisonerSourceCount(carriedRegularPrisonerSourceCount);
        int militaryPrisonerSourceCount = safeGarrison + safeCarriedRegularPrisoners;
        int safeCaptiveLords = Math.Max(0, captiveLordSourceCount);
        bool destructive = finalEffect.Destructive;
        bool hasExplicitAllocation = explicitAllocation.HasExplicitAllocation;
        int honoredPrisonerCount = 0;
        int recruitableCount;
        int armoryPrisonerCount = 0;
        int laborPrisonerCount;
        int slaughteredPrisonerCount = 0;
        int soldPrisonerCount = 0;
        int unallocatedRegularPrisonerCount;
        if (hasExplicitAllocation)
        {
            honoredPrisonerCount = Math.Max(0, explicitAllocation.HonoredPrisonerCount);
            recruitableCount = Math.Max(0, explicitAllocation.RecruitedPrisonerCount);
            armoryPrisonerCount = Math.Max(0, explicitAllocation.ArmoryPrisonerCount);
            laborPrisonerCount = Math.Max(0, explicitAllocation.LaborPrisonerCount);
            slaughteredPrisonerCount = Math.Max(0, explicitAllocation.SlaughteredPrisonerCount);
            soldPrisonerCount = Math.Max(0, explicitAllocation.SoldPrisonerCount);
            unallocatedRegularPrisonerCount = Math.Max(0, explicitAllocation.UnallocatedRegularPrisonerCount);
        }
        else
        {
            recruitableCount = PercentOf(militaryPrisonerSourceCount, finalEffect.RecruitablePrisonerPercent);
            recruitableCount = Math.Min(MaxRecruitablePrisonerCount, recruitableCount);
            int laborSourceCount = Math.Max(0, militaryPrisonerSourceCount - recruitableCount);
            laborPrisonerCount = PercentOf(laborSourceCount, finalEffect.LaborPrisonerPercent);
            laborPrisonerCount = Math.Min(MaxLaborPrisonerCount, laborPrisonerCount);
            unallocatedRegularPrisonerCount = Math.Max(0, militaryPrisonerSourceCount - recruitableCount - laborPrisonerCount - soldPrisonerCount);
        }

        int positiveRansomWeight = destructive ? 0 : Math.Max(0, finalEffect.RansomGoldWeight);
        int ransomGold = safeCaptiveLords <= 0 ? 0 : positiveRansomWeight * safeCaptiveLords * RansomGoldPerWeightPerLord;
        ransomGold = Math.Min(MaxRansomGold, Math.Max(0, ransomGold));

        int castleLoyaltyDelta = destructive ? Math.Min(0, finalEffect.CastleLoyaltyDelta) : finalEffect.CastleLoyaltyDelta;
        int castleSecurityDelta = destructive ? Math.Min(0, finalEffect.CastleSecurityDelta) : finalEffect.CastleSecurityDelta;
        int villageProductionBonusPercent = destructive ? Math.Min(0, finalEffect.BoundVillageProductionBonusPercent) : finalEffect.BoundVillageProductionBonusPercent;
        int armoryReceiptWeight = destructive ? 0 : Math.Max(0, finalEffect.ArmoryReceiptWeight);

        return new SiegeCastleFinalSideEffectPlan(
            recruitableCount,
            honoredPrisonerCount,
            laborPrisonerCount,
            armoryPrisonerCount,
            slaughteredPrisonerCount,
            soldPrisonerCount,
            ransomGold,
            finalEffect.LordRelationDelta,
            castleLoyaltyDelta,
            castleSecurityDelta,
            villageProductionBonusPercent,
            armoryReceiptWeight,
            finalEffect.CaptiveFearDelta,
            finalEffect.PlayerTroopMoraleDelta,
            finalEffect.PlayerTroopXpWeight,
            safeGarrison,
            safeCarriedRegularPrisoners,
            militaryPrisonerSourceCount,
            safeCaptiveLords,
            unallocatedRegularPrisonerCount,
            hasExplicitAllocation,
            destructive,
            finalEffect.SkipTownMarketLoot);
    }

    public static string BuildMemoryText(SiegeCastleFinalSideEffectPlan plan, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return safeCastleName + "城堡副作用计划："
            + "可收编俘虏 " + Math.Max(0, plan.RecruitablePrisonerCount)
            + " / 可处置战败士兵俘虏 " + Math.Max(0, plan.SourceMilitaryPrisonerCount)
            + "（含玩家既有士兵俘虏计入 " + Math.Max(0, plan.SourceCarriedRegularPrisonerCount)
            + "，上限 " + MaxCarriedRegularPrisonerSourceCount + "）"
            + "，优待战俘 " + Math.Max(0, plan.HonoredPrisonerCount)
            + "，劳役战俘 " + Math.Max(0, plan.LaborPrisonerCount)
            + "，缴械军械 " + Math.Max(0, plan.ArmoryPrisonerCount)
            + "，屠戮战俘 " + Math.Max(0, plan.SlaughteredPrisonerCount)
            + "，贩卖战俘 " + Math.Max(0, plan.SoldPrisonerCount)
            + "，未分配战俘 " + Math.Max(0, plan.UnallocatedRegularPrisonerCount)
            + "，赎金金币 " + Math.Max(0, plan.RansomGold) + " / 被俘领主 " + Math.Max(0, plan.SourceCaptiveLordCount)
            + "，领主好感 " + FormatSigned(plan.LordRelationDelta)
            + "，城堡忠诚 " + FormatSigned(plan.CastleLoyaltyDelta)
            + "，城堡治安 " + FormatSigned(plan.CastleSecurityDelta)
            + "，附属村庄产出 " + FormatSigned(plan.BoundVillageProductionBonusPercent) + "%"
            + "，军械接收权重 " + Math.Max(0, plan.ArmoryReceiptWeight)
            + "，俘虏恐惧 " + FormatSigned(plan.CaptiveFearDelta)
            + "，己方士气 " + FormatSigned(plan.PlayerTroopMoraleDelta)
            + "，部队经验权重 " + FormatSigned(plan.PlayerTroopXpWeight)
            + (plan.ExplicitPrisonerAllocation ? "，使用显式数量分配" : "，使用百分比默认分配")
            + (plan.DestructivePrisonerLoss ? "，存在不可逆军务/处决或屠戮命令" : "，不触发城镇市场搜掠");
    }

    public static string BuildDiagnosticText(SiegeCastleFinalSideEffectPlan plan, string castleId)
    {
        return "castle=" + (string.IsNullOrWhiteSpace(castleId) ? "N/A" : castleId.Trim())
            + " recruitablePrisoners=" + Math.Max(0, plan.RecruitablePrisonerCount)
            + " laborPrisoners=" + Math.Max(0, plan.LaborPrisonerCount)
            + " honoredPrisoners=" + Math.Max(0, plan.HonoredPrisonerCount)
            + " armoryPrisoners=" + Math.Max(0, plan.ArmoryPrisonerCount)
            + " slaughteredPrisoners=" + Math.Max(0, plan.SlaughteredPrisonerCount)
            + " soldPrisoners=" + Math.Max(0, plan.SoldPrisonerCount)
            + " unallocatedRegularPrisoners=" + Math.Max(0, plan.UnallocatedRegularPrisonerCount)
            + " sourceGarrison=" + Math.Max(0, plan.SourceSurrenderedGarrisonCount)
            + " sourceCarriedRegularPrisoners=" + Math.Max(0, plan.SourceCarriedRegularPrisonerCount)
            + " sourceMilitaryPrisoners=" + Math.Max(0, plan.SourceMilitaryPrisonerCount)
            + " maxCarriedRegularPrisoners=" + MaxCarriedRegularPrisonerSourceCount
            + " ransomGold=" + Math.Max(0, plan.RansomGold)
            + " captiveLords=" + Math.Max(0, plan.SourceCaptiveLordCount)
            + " lordRelation=" + plan.LordRelationDelta
            + " castleLoyalty=" + plan.CastleLoyaltyDelta
            + " castleSecurity=" + plan.CastleSecurityDelta
            + " villageProduction=" + plan.BoundVillageProductionBonusPercent
            + " armoryWeight=" + Math.Max(0, plan.ArmoryReceiptWeight)
            + " captiveFear=" + plan.CaptiveFearDelta
            + " playerTroopMorale=" + plan.PlayerTroopMoraleDelta
            + " playerTroopXpWeight=" + plan.PlayerTroopXpWeight
            + " explicitAllocation=" + plan.ExplicitPrisonerAllocation
            + " destructiveLoss=" + plan.DestructivePrisonerLoss
            + " skipTownMarketLoot=" + plan.SkipTownMarketLoot;
    }

    public static int ResolveCarriedRegularPrisonerSourceCount(int selectedOrVisibleRegularPrisonerCount, int totalMainPartyRegularPrisonerCount)
    {
        int selected = ClampCarriedRegularPrisonerSourceCount(selectedOrVisibleRegularPrisonerCount);
        if (selected > 0)
        {
            return selected;
        }

        return ClampCarriedRegularPrisonerSourceCount(totalMainPartyRegularPrisonerCount);
    }

    private static int ClampCarriedRegularPrisonerSourceCount(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > MaxCarriedRegularPrisonerSourceCount ? MaxCarriedRegularPrisonerSourceCount : value;
    }

    private static int PercentOf(int value, int percent)
    {
        int safeValue = Math.Max(0, value);
        int safePercent = percent < 0 ? 0 : (percent > 100 ? 100 : percent);
        return (safeValue * safePercent + 50) / 100;
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}

public readonly struct SiegeCastleFinalSideEffectPlan
{
    public SiegeCastleFinalSideEffectPlan(
        int recruitablePrisonerCount,
        int honoredPrisonerCount,
        int laborPrisonerCount,
        int armoryPrisonerCount,
        int slaughteredPrisonerCount,
        int soldPrisonerCount,
        int ransomGold,
        int lordRelationDelta,
        int castleLoyaltyDelta,
        int castleSecurityDelta,
        int boundVillageProductionBonusPercent,
        int armoryReceiptWeight,
        int captiveFearDelta,
        int playerTroopMoraleDelta,
        int playerTroopXpWeight,
        int sourceSurrenderedGarrisonCount,
        int sourceCarriedRegularPrisonerCount,
        int sourceMilitaryPrisonerCount,
        int sourceCaptiveLordCount,
        int unallocatedRegularPrisonerCount,
        bool explicitPrisonerAllocation,
        bool destructivePrisonerLoss,
        bool skipTownMarketLoot)
    {
        RecruitablePrisonerCount = recruitablePrisonerCount;
        HonoredPrisonerCount = honoredPrisonerCount;
        LaborPrisonerCount = laborPrisonerCount;
        ArmoryPrisonerCount = armoryPrisonerCount;
        SlaughteredPrisonerCount = slaughteredPrisonerCount;
        SoldPrisonerCount = soldPrisonerCount;
        RansomGold = ransomGold;
        LordRelationDelta = lordRelationDelta;
        CastleLoyaltyDelta = castleLoyaltyDelta;
        CastleSecurityDelta = castleSecurityDelta;
        BoundVillageProductionBonusPercent = boundVillageProductionBonusPercent;
        ArmoryReceiptWeight = armoryReceiptWeight;
        CaptiveFearDelta = captiveFearDelta;
        PlayerTroopMoraleDelta = playerTroopMoraleDelta;
        PlayerTroopXpWeight = playerTroopXpWeight;
        SourceSurrenderedGarrisonCount = sourceSurrenderedGarrisonCount;
        SourceCarriedRegularPrisonerCount = sourceCarriedRegularPrisonerCount;
        SourceMilitaryPrisonerCount = sourceMilitaryPrisonerCount;
        SourceCaptiveLordCount = sourceCaptiveLordCount;
        UnallocatedRegularPrisonerCount = unallocatedRegularPrisonerCount;
        ExplicitPrisonerAllocation = explicitPrisonerAllocation;
        DestructivePrisonerLoss = destructivePrisonerLoss;
        SkipTownMarketLoot = skipTownMarketLoot;
    }

    public int RecruitablePrisonerCount { get; }

    public int HonoredPrisonerCount { get; }

    public int LaborPrisonerCount { get; }

    public int ArmoryPrisonerCount { get; }

    public int SlaughteredPrisonerCount { get; }

    public int SoldPrisonerCount { get; }

    public int RansomGold { get; }

    public int LordRelationDelta { get; }

    public int CastleLoyaltyDelta { get; }

    public int CastleSecurityDelta { get; }

    public int BoundVillageProductionBonusPercent { get; }

    public int ArmoryReceiptWeight { get; }

    public int CaptiveFearDelta { get; }

    public int PlayerTroopMoraleDelta { get; }

    public int PlayerTroopXpWeight { get; }

    public int SourceSurrenderedGarrisonCount { get; }

    public int SourceCarriedRegularPrisonerCount { get; }

    public int SourceMilitaryPrisonerCount { get; }

    public int SourceCaptiveLordCount { get; }

    public int UnallocatedRegularPrisonerCount { get; }

    public bool ExplicitPrisonerAllocation { get; }

    public bool DestructivePrisonerLoss { get; }

    public bool SkipTownMarketLoot { get; }
}
