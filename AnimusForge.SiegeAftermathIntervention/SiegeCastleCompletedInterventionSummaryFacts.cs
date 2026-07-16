using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free final castle outcome facts supplied by the Bannerlord runtime bridge.
/// Counts cover only prisoners selected for the active castle GCCZ scene.
/// </summary>
public sealed class SiegeCastleCompletedInterventionSummaryFacts
{
    public SiegeCastleCompletedInterventionSummaryFacts(
        string settlementName,
        int recruitedRegularPrisoners,
        int slaughteredRegularPrisoners,
        int remainingRegularPrisoners,
        int retainedLordPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        bool soldierMoralePenaltyApplied)
    {
        SettlementName = settlementName;
        RecruitedRegularPrisoners = Math.Max(0, recruitedRegularPrisoners);
        SlaughteredRegularPrisoners = Math.Max(0, slaughteredRegularPrisoners);
        RemainingRegularPrisoners = Math.Max(0, remainingRegularPrisoners);
        RetainedLordPrisoners = Math.Max(0, retainedLordPrisoners);
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        SoldierMoralePenaltyApplied = soldierMoralePenaltyApplied;
    }

    public string SettlementName { get; }

    public int RecruitedRegularPrisoners { get; }

    public int SlaughteredRegularPrisoners { get; }

    public int RemainingRegularPrisoners { get; }

    public int RetainedLordPrisoners { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public bool SoldierMoralePenaltyApplied { get; }
}
