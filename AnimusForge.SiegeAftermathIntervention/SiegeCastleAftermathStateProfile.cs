namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free accumulated state policy for castle aftermath actions.
/// This turns individual castle military tags into a game-facing running situation
/// that the AF bridge can expose to prompts/logs without reusing town civilian state.
/// </summary>
public static class SiegeCastleAftermathStateProfile
{
    public const string RuntimeStateHeader = "【城堡军务状态】";

    public const int MinRelativeDelta = -100;

    public const int MaxRelativeDelta = 100;

    public const int MinRansomWeight = -100;

    public const int MaxRansomWeight = 200;

    public static SiegeCastleAftermathStateSnapshot CreateDefault()
    {
        return new SiegeCastleAftermathStateSnapshot(
            lordRelationDelta: 0,
            castleLoyaltyDelta: 0,
            castleSecurityDelta: 0,
            boundVillageProductionBonusPercent: 0,
            recruitablePrisonerPercent: 0,
            laborPrisonerPercent: 0,
            ransomGoldWeight: 0,
            armoryReceiptWeight: 0,
            captiveTrustDelta: 0,
            captiveFearDelta: 0,
            playerTroopMoraleDelta: 0,
            playerTroopXpWeight: 0,
            defeatedLordRecruitmentIntentCount: 0,
            actionCount: 0,
            honorCaptivesChosen: false,
            armorySeized: false,
            lordRecruitmentProposed: false,
            destructiveOrderIssued: false,
            irreversibleOrderLocked: false);
    }

    public static SiegeCastleAftermathStateSnapshot ApplyAction(
        SiegeCastleAftermathStateSnapshot current,
        SiegeCastleAftermathActionKind kind,
        SiegeCastleAftermathEffectProfile effect)
    {
        return new SiegeCastleAftermathStateSnapshot(
            ClampRelativeDelta(current.LordRelationDelta + effect.LordRelationDelta),
            ClampRelativeDelta(current.CastleLoyaltyDelta + effect.CastleLoyaltyDelta),
            ClampRelativeDelta(current.CastleSecurityDelta + effect.CastleSecurityDelta),
            ClampRelativeDelta(current.BoundVillageProductionBonusPercent + effect.BoundVillageProductionBonusPercent),
            SiegeCastleAftermathProfile.ClampRecruitmentPercent(current.RecruitablePrisonerPercent + effect.RecruitablePrisonerPercent),
            SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(current.LaborPrisonerPercent + effect.LaborPrisonerPercent),
            ClampRansomWeight(current.RansomGoldWeight + effect.RansomGoldWeight),
            ClampRansomWeight(current.ArmoryReceiptWeight + effect.ArmoryReceiptWeight),
            ClampRelativeDelta(current.CaptiveTrustDelta + effect.CaptiveTrustDelta),
            ClampRelativeDelta(current.CaptiveFearDelta + effect.CaptiveFearDelta),
            ClampRelativeDelta(current.PlayerTroopMoraleDelta + effect.PlayerTroopMoraleDelta),
            ClampRelativeDelta(current.PlayerTroopXpWeight + effect.PlayerTroopXpWeight),
            System.Math.Max(0, current.DefeatedLordRecruitmentIntentCount) + (effect.RecruitsDefeatedLord ? 1 : 0),
            System.Math.Max(0, current.ActionCount) + 1,
            current.HonorCaptivesChosen || kind == SiegeCastleAftermathActionKind.HonorCaptives,
            current.ArmorySeized || kind == SiegeCastleAftermathActionKind.SeizeArmory,
            current.LordRecruitmentProposed || kind == SiegeCastleAftermathActionKind.RecruitLord,
            current.DestructiveOrderIssued || effect.IsDestructive,
            current.IrreversibleOrderLocked || effect.IsIrreversible);
    }

    public static string BuildRuntimeStateLine(SiegeCastleAftermathStateSnapshot state)
    {
        return RuntimeStateHeader
            + "领主好感 " + FormatSigned(ClampRelativeDelta(state.LordRelationDelta))
            + "，城堡忠诚 " + FormatSigned(ClampRelativeDelta(state.CastleLoyaltyDelta))
            + "，城堡治安 " + FormatSigned(ClampRelativeDelta(state.CastleSecurityDelta))
            + "，附属村庄产出 " + FormatSigned(ClampRelativeDelta(state.BoundVillageProductionBonusPercent)) + "%"
            + "，可收编俘虏 " + SiegeCastleAftermathProfile.ClampRecruitmentPercent(state.RecruitablePrisonerPercent)
            + "%，劳役战俘 " + SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(state.LaborPrisonerPercent)
            + "%，赎金权重 " + FormatSigned(ClampRansomWeight(state.RansomGoldWeight))
            + "，军械接收 " + FormatSigned(ClampRansomWeight(state.ArmoryReceiptWeight))
            + "，俘虏信任 " + FormatSigned(ClampRelativeDelta(state.CaptiveTrustDelta))
            + "，俘虏恐惧 " + FormatSigned(ClampRelativeDelta(state.CaptiveFearDelta))
            + "，己方士气 " + FormatSigned(ClampRelativeDelta(state.PlayerTroopMoraleDelta))
            + "，部队经验权重 " + FormatSigned(ClampRelativeDelta(state.PlayerTroopXpWeight))
            + "，领主收编意图 " + System.Math.Max(0, state.DefeatedLordRecruitmentIntentCount)
            + "，已执行军务 " + System.Math.Max(0, state.ActionCount) + " 项"
            + (state.HonorCaptivesChosen ? "，已优待战俘" : string.Empty)
            + (state.ArmorySeized ? "，已接收军械" : string.Empty)
            + (state.IrreversibleOrderLocked ? "，已有不可逆命令" : string.Empty);
    }

    public static string BuildActionStateMemoryText(SiegeCastleAftermathActionKind kind, SiegeCastleAftermathStateSnapshot state)
    {
        return "城堡军务状态因“" + SiegeCastleAftermathProfile.GetActionLabel(kind) + "”更新："
            + "领主好感 " + FormatSigned(ClampRelativeDelta(state.LordRelationDelta))
            + "，城堡忠诚 " + FormatSigned(ClampRelativeDelta(state.CastleLoyaltyDelta))
            + "，城堡治安 " + FormatSigned(ClampRelativeDelta(state.CastleSecurityDelta))
            + "，附属村庄产出 " + FormatSigned(ClampRelativeDelta(state.BoundVillageProductionBonusPercent)) + "%"
            + "，可收编俘虏 " + SiegeCastleAftermathProfile.ClampRecruitmentPercent(state.RecruitablePrisonerPercent) + "%"
            + "，劳役战俘 " + SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(state.LaborPrisonerPercent) + "%"
            + "，赎金权重 " + FormatSigned(ClampRansomWeight(state.RansomGoldWeight))
            + "，军械接收 " + FormatSigned(ClampRansomWeight(state.ArmoryReceiptWeight))
            + "，俘虏信任 " + FormatSigned(ClampRelativeDelta(state.CaptiveTrustDelta))
            + "，俘虏恐惧 " + FormatSigned(ClampRelativeDelta(state.CaptiveFearDelta))
            + "，己方士气 " + FormatSigned(ClampRelativeDelta(state.PlayerTroopMoraleDelta))
            + "，部队经验权重 " + FormatSigned(ClampRelativeDelta(state.PlayerTroopXpWeight))
            + "，领主收编意图 " + System.Math.Max(0, state.DefeatedLordRecruitmentIntentCount) + "。";
    }

    public static string BuildDiagnosticText(SiegeCastleAftermathActionKind kind, SiegeCastleAftermathStateSnapshot state)
    {
        return "action=" + kind
            + " lordRelation=" + ClampRelativeDelta(state.LordRelationDelta)
            + " castleLoyalty=" + ClampRelativeDelta(state.CastleLoyaltyDelta)
            + " castleSecurity=" + ClampRelativeDelta(state.CastleSecurityDelta)
            + " villageProduction=" + ClampRelativeDelta(state.BoundVillageProductionBonusPercent)
            + " recruitPrisonerPercent=" + SiegeCastleAftermathProfile.ClampRecruitmentPercent(state.RecruitablePrisonerPercent)
            + " laborPrisonerPercent=" + SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(state.LaborPrisonerPercent)
            + " ransomWeight=" + ClampRansomWeight(state.RansomGoldWeight)
            + " armoryWeight=" + ClampRansomWeight(state.ArmoryReceiptWeight)
            + " captiveTrust=" + ClampRelativeDelta(state.CaptiveTrustDelta)
            + " captiveFear=" + ClampRelativeDelta(state.CaptiveFearDelta)
            + " playerTroopMorale=" + ClampRelativeDelta(state.PlayerTroopMoraleDelta)
            + " playerTroopXpWeight=" + ClampRelativeDelta(state.PlayerTroopXpWeight)
            + " lordRecruitmentIntents=" + System.Math.Max(0, state.DefeatedLordRecruitmentIntentCount)
            + " honorCaptives=" + state.HonorCaptivesChosen
            + " armorySeized=" + state.ArmorySeized
            + " lordRecruitment=" + state.LordRecruitmentProposed
            + " actions=" + System.Math.Max(0, state.ActionCount)
            + " destructive=" + state.DestructiveOrderIssued
            + " irreversible=" + state.IrreversibleOrderLocked;
    }

    private static int ClampRelativeDelta(int value)
    {
        return value < MinRelativeDelta ? MinRelativeDelta : (value > MaxRelativeDelta ? MaxRelativeDelta : value);
    }

    private static int ClampRansomWeight(int value)
    {
        return value < MinRansomWeight ? MinRansomWeight : (value > MaxRansomWeight ? MaxRansomWeight : value);
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}

public readonly struct SiegeCastleAftermathStateSnapshot
{
    public SiegeCastleAftermathStateSnapshot(
        int lordRelationDelta,
        int castleLoyaltyDelta,
        int castleSecurityDelta,
        int boundVillageProductionBonusPercent,
        int recruitablePrisonerPercent,
        int laborPrisonerPercent,
        int ransomGoldWeight,
        int armoryReceiptWeight,
        int captiveTrustDelta,
        int captiveFearDelta,
        int playerTroopMoraleDelta,
        int playerTroopXpWeight,
        int defeatedLordRecruitmentIntentCount,
        int actionCount,
        bool honorCaptivesChosen,
        bool armorySeized,
        bool lordRecruitmentProposed,
        bool destructiveOrderIssued,
        bool irreversibleOrderLocked)
    {
        LordRelationDelta = lordRelationDelta;
        CastleLoyaltyDelta = castleLoyaltyDelta;
        CastleSecurityDelta = castleSecurityDelta;
        BoundVillageProductionBonusPercent = boundVillageProductionBonusPercent;
        RecruitablePrisonerPercent = recruitablePrisonerPercent;
        LaborPrisonerPercent = laborPrisonerPercent;
        RansomGoldWeight = ransomGoldWeight;
        ArmoryReceiptWeight = armoryReceiptWeight;
        CaptiveTrustDelta = captiveTrustDelta;
        CaptiveFearDelta = captiveFearDelta;
        PlayerTroopMoraleDelta = playerTroopMoraleDelta;
        PlayerTroopXpWeight = playerTroopXpWeight;
        DefeatedLordRecruitmentIntentCount = defeatedLordRecruitmentIntentCount;
        ActionCount = actionCount;
        HonorCaptivesChosen = honorCaptivesChosen;
        ArmorySeized = armorySeized;
        LordRecruitmentProposed = lordRecruitmentProposed;
        DestructiveOrderIssued = destructiveOrderIssued;
        IrreversibleOrderLocked = irreversibleOrderLocked;
    }

    public int LordRelationDelta { get; }

    public int CastleLoyaltyDelta { get; }

    public int CastleSecurityDelta { get; }

    public int BoundVillageProductionBonusPercent { get; }

    public int RecruitablePrisonerPercent { get; }

    public int LaborPrisonerPercent { get; }

    public int RansomGoldWeight { get; }

    public int ArmoryReceiptWeight { get; }

    public int CaptiveTrustDelta { get; }

    public int CaptiveFearDelta { get; }

    public int PlayerTroopMoraleDelta { get; }

    public int PlayerTroopXpWeight { get; }

    public int DefeatedLordRecruitmentIntentCount { get; }

    public int ActionCount { get; }

    public bool HonorCaptivesChosen { get; }

    public bool ArmorySeized { get; }

    public bool LordRecruitmentProposed { get; }

    public bool DestructiveOrderIssued { get; }

    public bool IrreversibleOrderLocked { get; }
}
