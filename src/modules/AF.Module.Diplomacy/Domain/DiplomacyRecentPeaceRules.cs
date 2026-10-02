using System;
namespace AnimusForge;
internal static class DiplomacyRecentPeaceRules
{
    internal const double ProtectionSeconds = 45;
    internal static bool Expired(DateTime registered, DateTime now) => (now - registered).TotalSeconds > ProtectionSeconds;
    internal static string PairKey(string first, string second)
    {
        first = (first ?? "").Trim(); second = (second ?? "").Trim();
        if (first.Length == 0 || second.Length == 0 || string.Equals(first, second, StringComparison.OrdinalIgnoreCase)) return "";
        return string.Compare(first, second, StringComparison.OrdinalIgnoreCase) <= 0 ? first + "|" + second : second + "|" + first;
    }
}
