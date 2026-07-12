using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for regional civilian conflict debt during an active GCCZ scene.
/// AF adapters apply live Bannerlord settlement, notable, and positive-effect side effects.
/// </summary>
public static class SiegeRegionalConflictProfile
{
    /// <summary>
    /// A regional conflict debt covers one 24m-diameter street area from the first local civilian victim.
    /// Further local player violence inside this circle should reuse the same debt stack.
    /// </summary>
    public const float ConflictAreaDiameter = 24f;

    public const float ConflictAreaRadius = ConflictAreaDiameter * 0.5f;

    public const int SettlementPublicTrustDeltaPerIncident = -1;

    public const int PositiveEffectPenaltyPerIncident = 5;

    public const float PositiveLoyaltyPenaltyPerIncident = 5f;

    public const string SettlementPublicTrustReason = "siege_ai_regional_conflict_settlement";

    public static bool IsInsideConflictAreaSquared(float distanceSquared)
    {
        return distanceSquared <= ConflictAreaRadius * ConflictAreaRadius;
    }

    public static int CalculatePositiveEffectPenalty(int incidentCount)
    {
        return Math.Max(0, incidentCount) * PositiveEffectPenaltyPerIncident;
    }

    public static float CalculatePositiveLoyaltyPenalty(int incidentCount)
    {
        return Math.Max(0, incidentCount) * PositiveLoyaltyPenaltyPerIncident;
    }

    public static int ReducePositiveIntDelta(int positiveDelta, int incidentCount)
    {
        if (positiveDelta <= 0)
        {
            return positiveDelta;
        }

        return Math.Max(0, positiveDelta - CalculatePositiveEffectPenalty(incidentCount));
    }

    public static float ReducePositiveFloatDelta(float positiveDelta, int incidentCount)
    {
        if (positiveDelta <= 0f)
        {
            return positiveDelta;
        }

        return Math.Max(0f, positiveDelta - CalculatePositiveLoyaltyPenalty(incidentCount));
    }

    public static string BuildConflictNoticeMessage(string targetName, bool victimDown)
    {
        string name = NormalizeTargetName(targetName, "一名平民");
        if (victimDown)
        {
            return "【区域冲突】" + name + "倒地，附近逃散。";
        }

        return "【区域冲突】" + name + "遭袭，附近恐慌。";
    }

    private static string NormalizeTargetName(string targetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();
    }
}
