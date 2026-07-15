using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free normalizer for AI postprocess output tags in the active GCCZ scene.
/// AF adapters provide the currently allowed postprocess tags; this core owns alias matching,
/// canonical action order, de-duplication, and mood-tag preservation.
/// </summary>
public static class SiegePostprocessTagNormalizer
{
    private static readonly Regex MoodTagRegex = new Regex(
        @"\[ACTION:MOOD:[^\]\r\n]*\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Normalize(string raw, IEnumerable<string> allowedTags)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var allowed = BuildAllowedSet(allowedTags);
        var normalizedTags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string tag)
        {
            string normalized = (tag ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
            {
                normalizedTags.Add(normalized);
            }
        }

        string text = raw.Replace("\r", string.Empty);
        var extractedKinds = new HashSet<SiegeInterventionActionKind>(SiegeActionTagCatalog.ExtractKinds(text));
        foreach (var kind in SiegeActionTagCatalog.GetCanonicalOrder())
        {
            if (!extractedKinds.Contains(kind))
            {
                continue;
            }

            IReadOnlyList<string> aliases = SiegeActionTagCatalog.GetAliases(kind);
            if (aliases.Count > 0
                && AllowsAny(allowed, aliases)
                && SiegeActionTagCatalog.TryGetCanonicalTag(kind, out string canonicalTag))
            {
                Add(canonicalTag);
            }
        }

        var extractedCastleKinds = new HashSet<SiegeCastleActionKind>(SiegeCastleActionTagCatalog.ExtractKinds(text));
        foreach (SiegeCastleActionKind kind in SiegeCastleActionTagCatalog.GetCanonicalOrder())
        {
            if (!extractedCastleKinds.Contains(kind))
            {
                continue;
            }

            IReadOnlyList<string> aliases = SiegeCastleActionTagCatalog.GetAliases(kind);
            if (aliases.Count > 0
                && AllowsAny(allowed, aliases)
                && SiegeCastleActionTagCatalog.TryGetCanonicalTag(kind, out string canonicalTag))
            {
                Add(canonicalTag);
            }
        }


        string mood = string.Empty;
        foreach (Match moodMatch in MoodTagRegex.Matches(text))
        {
            mood = (moodMatch?.Value ?? string.Empty).Trim();
        }

        Add(mood);
        return string.Join("\n", normalizedTags).Trim();
    }

    private static HashSet<string> BuildAllowedSet(IEnumerable<string> allowedTags)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (allowedTags == null)
        {
            return allowed;
        }

        foreach (string allowedTag in allowedTags)
        {
            string tag = (allowedTag ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(tag))
            {
                allowed.Add(tag);
            }
        }

        return allowed;
    }

    private static bool AllowsAny(HashSet<string> allowed, IEnumerable<string> candidates)
    {
        if (allowed == null || allowed.Count == 0 || candidates == null)
        {
            return false;
        }

        foreach (string candidate in candidates)
        {
            string value = (candidate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (allowed.Contains(value))
            {
                return true;
            }

            string prefix = value.EndsWith("]", StringComparison.Ordinal)
                ? value.Substring(0, value.Length - 1)
                : value;
            foreach (string allowedTag in allowed)
            {
                if ((allowedTag ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
