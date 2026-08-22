using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Scales partial massacre consequences from the complete plunder baseline to the legacy massacre anchors.
/// </summary>
public sealed class TownMassacreConsequenceDelta
{
    public const uint StopMessageColor = 0xFFB6F7A8u;

    public const int OrdinaryVictimWeight = 1;

    public const int NotableVictimWeight = 3;

    public const string SettlementPublicTrustReason = "gccz_massacre_ledger_settlement";

    public const string BoundVillagePublicTrustReason = "gccz_massacre_ledger_bound_village";

    public const string NotableRelationReason = "gccz_massacre_ledger_notable_relation";

    public const string NotableTrustReason = "gccz_massacre_ledger_notable_trust";

    private TownMassacreConsequenceDelta(
        int cumulativeVictimBasisPoints,
        int deltaVictimBasisPoints,
        bool appliedPlunderBaseline,
        int settlementPublicTrustDelta,
        int boundVillagePublicTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta)
    {
        CumulativeVictimBasisPoints = cumulativeVictimBasisPoints;
        DeltaVictimBasisPoints = deltaVictimBasisPoints;
        AppliedPlunderBaseline = appliedPlunderBaseline;
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        NotableRelationDelta = notableRelationDelta;
        NotableTrustDelta = notableTrustDelta;
    }

    public int CumulativeVictimBasisPoints { get; }

    public int DeltaVictimBasisPoints { get; }

    public bool AppliedPlunderBaseline { get; }

    public int SettlementPublicTrustDelta { get; }

    public int BoundVillagePublicTrustDelta { get; }

    public int NotableRelationDelta { get; }

    public int NotableTrustDelta { get; }

    public bool HasConsequences => SettlementPublicTrustDelta != 0
        || BoundVillagePublicTrustDelta != 0
        || NotableRelationDelta != 0
        || NotableTrustDelta != 0;

    public static TownMassacreConsequenceDelta FromProgressCommit(
        TownOperationVictimProgressCommit progress,
        int committedPlunderBasisPoints)
    {
        int cumulative = ClampBasisPoints(progress.CumulativeBasisPoints);
        int previous = ClampBasisPoints(cumulative - progress.DeltaBasisPoints);
        int plunderProgress = ClampBasisPoints(committedPlunderBasisPoints);
        return new TownMassacreConsequenceDelta(
            cumulative,
            cumulative - previous,
            progress.AppliesConsequenceBaseline,
            CalculateIncrement(
                TownOutcomeCompatibilityProfile.PlunderSettlementPublicTrustDelta,
                TownOutcomeCompatibilityProfile.MassacreSettlementPublicTrustDelta,
                plunderProgress,
                previous,
                cumulative,
                progress.AppliesConsequenceBaseline),
            CalculateIncrement(
                TownOutcomeCompatibilityProfile.PlunderBoundVillagePublicTrustDelta,
                TownOutcomeCompatibilityProfile.MassacreBoundVillagePublicTrustDelta,
                plunderProgress,
                previous,
                cumulative,
                progress.AppliesConsequenceBaseline),
            CalculateIncrement(
                TownOutcomeCompatibilityProfile.PlunderNotableRelationDelta,
                TownOutcomeCompatibilityProfile.MassacreNotableRelationDelta,
                plunderProgress,
                previous,
                cumulative,
                progress.AppliesConsequenceBaseline),
            CalculateIncrement(
                TownOutcomeCompatibilityProfile.PlunderNotableTrustDelta,
                TownOutcomeCompatibilityProfile.MassacreNotableTrustDelta,
                plunderProgress,
                previous,
                cumulative,
                progress.AppliesConsequenceBaseline));
    }

    public static TownMassacreConsequenceDelta FromLedgerToFinalAnchors(
        TownOperationLedgerSnapshot ledger,
        int settlementPublicTrustAnchor,
        int boundVillagePublicTrustAnchor,
        int notableRelationAnchor,
        int notableTrustAnchor)
    {
        TownOperationLedgerSnapshot safeLedger = ledger ?? new TownOperationLedger().Snapshot();
        return new TownMassacreConsequenceDelta(
            safeLedger.CommittedVictimProgressBasisPoints,
            0,
            false,
            settlementPublicTrustAnchor - ResolveCurrentAppliedValue(
                safeLedger,
                TownOutcomeCompatibilityProfile.PlunderSettlementPublicTrustDelta,
                TownOutcomeCompatibilityProfile.MassacreSettlementPublicTrustDelta),
            boundVillagePublicTrustAnchor - ResolveCurrentAppliedValue(
                safeLedger,
                TownOutcomeCompatibilityProfile.PlunderBoundVillagePublicTrustDelta,
                TownOutcomeCompatibilityProfile.MassacreBoundVillagePublicTrustDelta),
            notableRelationAnchor - ResolveCurrentAppliedValue(
                safeLedger,
                TownOutcomeCompatibilityProfile.PlunderNotableRelationDelta,
                TownOutcomeCompatibilityProfile.MassacreNotableRelationDelta),
            notableTrustAnchor - ResolveCurrentAppliedValue(
                safeLedger,
                TownOutcomeCompatibilityProfile.PlunderNotableTrustDelta,
                TownOutcomeCompatibilityProfile.MassacreNotableTrustDelta));
    }

    private static int ResolveCurrentAppliedValue(
        TownOperationLedgerSnapshot ledger,
        int plunderAnchor,
        int massacreAnchor)
    {
        if (ledger.VictimConsequenceBaselineCommitted)
        {
            return BuildMassacreCumulative(
                plunderAnchor,
                massacreAnchor,
                ledger.CommittedVictimProgressBasisPoints);
        }
        return ScaleCumulative(plunderAnchor, ledger.CommittedProgressBasisPoints);
    }

    private static int CalculateIncrement(
        int plunderAnchor,
        int massacreAnchor,
        int committedPlunderBasisPoints,
        int previousVictimBasisPoints,
        int cumulativeVictimBasisPoints,
        bool appliesBaseline)
    {
        int previousValue = appliesBaseline
            ? ScaleCumulative(plunderAnchor, committedPlunderBasisPoints)
            : BuildMassacreCumulative(plunderAnchor, massacreAnchor, previousVictimBasisPoints);
        int currentValue = BuildMassacreCumulative(plunderAnchor, massacreAnchor, cumulativeVictimBasisPoints);
        return currentValue - previousValue;
    }

    private static int BuildMassacreCumulative(int plunderAnchor, int massacreAnchor, int victimBasisPoints)
    {
        int clamped = ClampBasisPoints(victimBasisPoints);
        if (clamped == TownOperationLedger.FullProgressBasisPoints)
        {
            return massacreAnchor;
        }
        return plunderAnchor + ScaleCumulative(massacreAnchor - plunderAnchor, clamped);
    }

    private static int ScaleCumulative(int fullValue, int basisPoints)
    {
        int clamped = ClampBasisPoints(basisPoints);
        if (clamped == 0 || fullValue == 0)
        {
            return 0;
        }
        if (clamped == TownOperationLedger.FullProgressBasisPoints)
        {
            return fullValue;
        }
        decimal scaled = fullValue * (clamped / (decimal)TownOperationLedger.FullProgressBasisPoints);
        return (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
    }

    private static int ClampBasisPoints(int value)
    {
        return Math.Min(TownOperationLedger.FullProgressBasisPoints, Math.Max(0, value));
    }
}
