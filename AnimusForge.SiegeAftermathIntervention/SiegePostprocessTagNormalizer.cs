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
        return Validate(raw, allowedTags).NormalizedTags;
    }

    public static SiegePostprocessValidationResult Validate(string raw, IEnumerable<string> allowedTags)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new SiegePostprocessValidationResult(string.Empty, null, 0, 0, false);
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
        SiegeInterventionActionKind? preferredTownKind = ResolvePreferredTownKind(extractedKinds, allowed);
        SiegeInterventionActionKind? selectedTownKind = null;
        foreach (var kind in SiegeActionTagCatalog.GetCanonicalOrder())
        {
            if (!extractedKinds.Contains(kind)
                || (preferredTownKind.HasValue && kind != preferredTownKind.Value))
            {
                continue;
            }

            IReadOnlyList<string> aliases = SiegeActionTagCatalog.GetAliases(kind);
            if (aliases.Count > 0
                && AllowsAny(allowed, aliases)
                && SiegeActionTagCatalog.TryGetCanonicalTag(kind, out string canonicalTag))
            {
                Add(canonicalTag);
                selectedTownKind = kind;
                // Town GCCZ numeric actions are mutually exclusive. Canonical order is the
                // conservative numeric priority, except that the explicit 9+10 pair is the
                // documented bloodbath-to-colonization upgrade and must keep action 10.
                break;
            }
        }

        IReadOnlyList<SiegeCastleActionKind> extractedCastleKinds = SiegeCastleActionTagCatalog.ExtractKinds(text);
        var matchedCastleKinds = new List<SiegeCastleActionKind>();
        foreach (SiegeCastleActionKind kind in extractedCastleKinds)
        {
            IReadOnlyList<string> aliases = SiegeCastleActionTagCatalog.GetAliases(kind);
            if (aliases.Count > 0
                && AllowsAny(allowed, aliases))
            {
                matchedCastleKinds.Add(kind);
            }
        }

        bool explicitCompoundDisposition = matchedCastleKinds.Count > 1;
        foreach (SiegeCastleActionKind kind in matchedCastleKinds)
        {
            explicitCompoundDisposition &= SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(kind);
        }

        if (explicitCompoundDisposition)
        {
            foreach (SiegeCastleActionKind kind in matchedCastleKinds)
            {
                if (SiegeCastleActionTagCatalog.TryGetCanonicalTag(kind, out string canonicalTag))
                {
                    Add(canonicalTag);
                }
            }
        }
        else
        {
            var matchedSet = new HashSet<SiegeCastleActionKind>(matchedCastleKinds);
            foreach (SiegeCastleActionKind kind in SiegeCastleActionTagCatalog.GetCanonicalOrder())
            {
                if (matchedSet.Contains(kind)
                    && SiegeCastleActionTagCatalog.TryGetCanonicalTag(kind, out string canonicalTag))
                {
                    Add(canonicalTag);
                    // Non-partitioned castle actions remain single-choice. This prevents a
                    // malformed model reply from stacking process, proposal or high-risk tags.
                    break;
                }
            }
        }


        string mood = string.Empty;
        foreach (Match moodMatch in MoodTagRegex.Matches(text))
        {
            mood = (moodMatch?.Value ?? string.Empty).Trim();
        }

        Add(mood);
        int rejectedTownActionCount = extractedKinds.Count - (selectedTownKind.HasValue ? 1 : 0);
        return new SiegePostprocessValidationResult(
            string.Join("\n", normalizedTags).Trim(),
            selectedTownKind,
            extractedKinds.Count,
            rejectedTownActionCount,
            LegacyTownTagAdapter.ContainsLegacyTag(text));
    }

    private static SiegeInterventionActionKind? ResolvePreferredTownKind(
        HashSet<SiegeInterventionActionKind> extractedKinds,
        HashSet<string> allowed)
    {
        if (extractedKinds == null || extractedKinds.Count == 0)
        {
            return null;
        }

        bool isExplicitRepopulationUpgrade = extractedKinds.Count == 2
            && extractedKinds.Contains(SiegeInterventionActionKind.Massacre)
            && extractedKinds.Contains(SiegeInterventionActionKind.CulturalRepopulation)
            && AllowsAny(allowed, SiegeActionTagCatalog.GetAliases(SiegeInterventionActionKind.CulturalRepopulation));
        if (isExplicitRepopulationUpgrade)
        {
            return SiegeInterventionActionKind.CulturalRepopulation;
        }

        foreach (SiegeInterventionActionKind kind in SiegeActionTagCatalog.GetCanonicalOrder())
        {
            if (extractedKinds.Contains(kind)
                && AllowsAny(allowed, SiegeActionTagCatalog.GetAliases(kind)))
            {
                return kind;
            }
        }

        return null;
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
        }

        return false;
    }

}
