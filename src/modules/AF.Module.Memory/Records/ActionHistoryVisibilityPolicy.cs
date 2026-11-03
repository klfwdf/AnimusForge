using System;

namespace AnimusForge;

internal static class ActionHistoryVisibilityPolicy
{
    // Participant AFEF and the debt ledger retain private terms; public history must not.
    internal static bool IsPrivateContract(string actionKind, string stableKey)
    {
        string kind = (actionKind ?? "").Trim();
        string key = (stableKey ?? "").Trim();
        return string.Equals(kind, "debt_recorded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "debt_payment", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("reward_action:debt_recorded:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("reward_action:debt_payment:", StringComparison.OrdinalIgnoreCase);
    }
}
