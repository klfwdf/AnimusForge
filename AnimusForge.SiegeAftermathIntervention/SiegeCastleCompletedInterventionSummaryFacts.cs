using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free final castle ledger supplied after native mercy and all staged groups finish.
/// </summary>
public sealed class SiegeCastleCompletedInterventionSummaryFacts
{
    public SiegeCastleCompletedInterventionSummaryFacts(
        string settlementName,
        string playerName,
        IReadOnlyList<SiegeCastleDispositionSummaryEntry> regularPrisonerOutcomes,
        int remainingRegularPrisoners,
        int retainedLordPrisoners,
        float loyaltyDelta,
        float securityDelta,
        float prosperityDelta,
        int settlementPublicTrustDelta,
        int boundVillagePublicTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        int soldierMoralePenaltyApplied,
        SiegeCastleActionKind soldierConcernAction = SiegeCastleActionKind.Unknown,
        bool treatedRegularPrisoners = false,
        bool receivedRegularArmaments = false,
        string lordOutcomeSummary = null,
        float constructionSpeedBonusPercent = 0f)
    {
        SettlementName = settlementName ?? string.Empty;
        PlayerName = playerName ?? string.Empty;
        RegularPrisonerOutcomes = regularPrisonerOutcomes ?? Array.Empty<SiegeCastleDispositionSummaryEntry>();
        RemainingRegularPrisoners = Math.Max(0, remainingRegularPrisoners);
        RetainedLordPrisoners = Math.Max(0, retainedLordPrisoners);
        LoyaltyDelta = loyaltyDelta;
        SecurityDelta = securityDelta;
        ProsperityDelta = prosperityDelta;
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        NotableRelationDelta = notableRelationDelta;
        NotableTrustDelta = notableTrustDelta;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        SoldierMoralePenaltyApplied = Math.Max(0, soldierMoralePenaltyApplied);
        SoldierConcernAction = soldierConcernAction;
        TreatedRegularPrisoners = treatedRegularPrisoners;
        ReceivedRegularArmaments = receivedRegularArmaments;
        LordOutcomeSummary = lordOutcomeSummary ?? string.Empty;
        ConstructionSpeedBonusPercent = Math.Max(0f, constructionSpeedBonusPercent);
    }

    public string SettlementName { get; }
    public string PlayerName { get; }
    public IReadOnlyList<SiegeCastleDispositionSummaryEntry> RegularPrisonerOutcomes { get; }
    public int RemainingRegularPrisoners { get; }
    public int RetainedLordPrisoners { get; }
    public float LoyaltyDelta { get; }
    public float SecurityDelta { get; }
    public float ProsperityDelta { get; }
    public int SettlementPublicTrustDelta { get; }
    public int BoundVillagePublicTrustDelta { get; }
    public int NotableRelationDelta { get; }
    public int NotableTrustDelta { get; }
    public bool SoldierAppeasementRequired { get; }
    public bool SoldierAppeasementApplied { get; }
    public int SoldierMoralePenaltyApplied { get; }
    public SiegeCastleActionKind SoldierConcernAction { get; }
    public bool TreatedRegularPrisoners { get; }
    public bool ReceivedRegularArmaments { get; }
    public string LordOutcomeSummary { get; }
    public float ConstructionSpeedBonusPercent { get; }
}

public sealed class SiegeCastleDispositionSummaryEntry
{
    public SiegeCastleDispositionSummaryEntry(SiegeCastleActionKind action, int affectedCount, int gold = 0)
    {
        Action = action;
        AffectedCount = Math.Max(0, affectedCount);
        Gold = Math.Max(0, gold);
    }

    public SiegeCastleActionKind Action { get; }
    public int AffectedCount { get; }
    public int Gold { get; }
}
