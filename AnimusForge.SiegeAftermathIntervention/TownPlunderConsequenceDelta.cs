using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Scales only the legacy GCCZ plunder consequence anchors by newly committed loot progress.
/// </summary>
public sealed class TownPlunderConsequenceDelta
{
    public const string SettlementPublicTrustReason = "gccz_plunder_ledger_settlement";
    public const string BoundVillagePublicTrustReason = "gccz_plunder_ledger_bound_village";
    public const string NotableRelationReason = "gccz_plunder_ledger_notable_relation";
    public const string NotableTrustReason = "gccz_plunder_ledger_notable_trust";

    private TownPlunderConsequenceDelta(
        int cumulativeBasisPoints,
        int deltaBasisPoints,
        int settlementPublicTrustDelta,
        int boundVillagePublicTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta)
    {
        CumulativeBasisPoints = cumulativeBasisPoints;
        DeltaBasisPoints = deltaBasisPoints;
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        NotableRelationDelta = notableRelationDelta;
        NotableTrustDelta = notableTrustDelta;
    }

    public int CumulativeBasisPoints { get; }

    public int DeltaBasisPoints { get; }

    public int SettlementPublicTrustDelta { get; }

    public int BoundVillagePublicTrustDelta { get; }

    public int NotableRelationDelta { get; }

    public int NotableTrustDelta { get; }

    public bool HasConsequences => SettlementPublicTrustDelta != 0
        || BoundVillagePublicTrustDelta != 0
        || NotableRelationDelta != 0
        || NotableTrustDelta != 0;

    public static TownPlunderConsequenceDelta FromProgressCommit(TownOperationProgressCommit progress)
    {
        int cumulative = ClampBasisPoints(progress.CumulativeBasisPoints);
        int previous = ClampBasisPoints(cumulative - progress.DeltaBasisPoints);
        return new TownPlunderConsequenceDelta(
            cumulative,
            cumulative - previous,
            ScaleIncrement(TownOutcomeCompatibilityProfile.PlunderSettlementPublicTrustDelta, previous, cumulative),
            ScaleIncrement(TownOutcomeCompatibilityProfile.PlunderBoundVillagePublicTrustDelta, previous, cumulative),
            ScaleIncrement(TownOutcomeCompatibilityProfile.PlunderNotableRelationDelta, previous, cumulative),
            ScaleIncrement(TownOutcomeCompatibilityProfile.PlunderNotableTrustDelta, previous, cumulative));
    }

    private static int ScaleIncrement(int fullValue, int previousBasisPoints, int cumulativeBasisPoints)
    {
        return ScaleCumulative(fullValue, cumulativeBasisPoints)
            - ScaleCumulative(fullValue, previousBasisPoints);
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
