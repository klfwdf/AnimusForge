using System;
using System.Collections.Generic;

namespace RichExecutions.Core;

public readonly struct ExecutionMethodRule
{
    public ExecutionMethodRule(
        string stringId,
        int nobleLegitimacy,
        int commonerLegitimacy,
        float securityDelta,
        float loyaltyDelta,
        int mercyXpDelta,
        bool forcesPanic)
    {
        StringId = stringId ?? string.Empty;
        NobleLegitimacy = nobleLegitimacy;
        CommonerLegitimacy = commonerLegitimacy;
        SecurityDelta = securityDelta;
        LoyaltyDelta = loyaltyDelta;
        MercyXpDelta = mercyXpDelta;
        ForcesPanic = forcesPanic;
    }

    public string StringId { get; }
    public int NobleLegitimacy { get; }
    public int CommonerLegitimacy { get; }
    public float SecurityDelta { get; }
    public float LoyaltyDelta { get; }
    public int MercyXpDelta { get; }
    public bool ForcesPanic { get; }
}

public static class ExecutionMethodRules
{
    public const string Beheading = "beheading";
    public const string Hanging = "hanging";
    public const string Burning = "burning";
    public const string BreakingWheel = "breaking_wheel";
    public const string Impalement = "impalement";
    public const string Stoning = "stoning";
    public const string CrossbowExecution = "crossbow_execution";

    private static readonly ExecutionMethodRule[] RegisteredRules =
    {
        new(Beheading, 1, 0, 0f, 0f, 0, false),
        new(Hanging, -1, 1, 1f, 0f, 0, false),
        new(Burning, -2, -2, 1f, -2f, -100, true),
        new(BreakingWheel, -2, -1, 2f, -1f, -75, true),
        new(Impalement, -3, -2, 2f, -2f, -125, true),
        new(Stoning, -2, 0, 1f, -1f, -60, true),
        new(CrossbowExecution, -1, 0, 1f, 0f, -40, true)
    };

    public static IReadOnlyList<ExecutionMethodRule> All => RegisteredRules;

    public static bool TryGet(string methodId, out ExecutionMethodRule rule)
    {
        foreach (var candidate in RegisteredRules)
        {
            if (string.Equals(candidate.StringId, methodId, StringComparison.OrdinalIgnoreCase))
            {
                rule = candidate;
                return true;
            }
        }

        rule = default;
        return false;
    }
}
