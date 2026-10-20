using System;
using System.Globalization;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Only the native-decision owner's known generated fragments are projected.
// Native game explanations and unknown fragments retain their original text.
internal static class DiplomacyLegacyPromptProjection
{
    private static readonly string[] NativeStates = {
        "WarScore_WarReasonSufficient", "WarScore_WarReasonInsufficient",
        "PeaceScore_PeaceInclined", "PeaceScore_PeaceNotInclined"
    };
    private static readonly int[] RelationSamples = { -60, -20, 0, 20, 60 };

    internal static string NativeReason(string raw, Func<string, string, string> localize)
    {
        if (localize == null || string.IsNullOrEmpty(raw)) return raw;
        string[] parts = raw.Split(new[] { '\uFF1B' });
        for (int index = 0; index < parts.Length; index++)
        {
            string part = parts[index];
            bool projected = false;
            foreach (string key in NativeStates)
                if (part == DiplomacyCompatibilityTexts.Get(key))
                {
                    parts[index] = localize(key, part);
                    projected = true;
                    break;
                }
            if (projected) continue;
            string nativePrefix = DiplomacyCompatibilityTexts.Get("WarScore_NativeReasonPrefix");
            if (part.StartsWith(nativePrefix, StringComparison.Ordinal))
            {
                parts[index] = localize("WarScore_NativeReasonPrefix", "Native reason=") + part.Substring(nativePrefix.Length);
                continue;
            }
            string relationPrefix = DiplomacyCompatibilityTexts.Get("RulerScore_PrivateRelationPrefix");
            foreach (int sample in RelationSamples)
                if (part == relationPrefix + WorldDiplomacyTextRules.DescribeRulerRelation(sample))
                {
                    parts[index] = localize("RulerScore_PrivateRelationPrefix", "Rulers' personal relations=")
                        + WorldDiplomacyTextRules.DescribeRulerRelation(sample, localize);
                    break;
                }
            string claimsPrefix = DiplomacyCompatibilityTexts.Get("RulerScore_CultureClaimsPrefix");
            if (part.StartsWith(claimsPrefix, StringComparison.Ordinal)
                && int.TryParse(part.Substring(claimsPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int claims))
                parts[index] = localize("RulerScore_CultureClaimsPrefix", "Culturally affiliated fiefs held by the other kingdom=")
                    + claims.ToString(CultureInfo.InvariantCulture);
        }
        return string.Join("\uFF1B", parts);
    }

    internal static string BattleLocation(string raw, Func<string, string, string> localize)
    {
        if (localize != null && (raw == DiplomacyCompatibilityTexts.Get("Battle_Wilderness_Garbled")
            || raw == DiplomacyCompatibilityTexts.Get("Battle_Wilderness")))
            return localize("Battle_Wilderness", "wilderness");
        return raw;
    }
}
