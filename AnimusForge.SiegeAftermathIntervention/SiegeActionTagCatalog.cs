using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the current numeric GCCZ town action protocol and delegates historical aliases
/// to the isolated compatibility adapter.
/// </summary>
public static class SiegeActionTagCatalog
{
    public const string MercyPromptTag = "[ACTION:1]";
    public const string ReliefPromptTag = "[ACTION:2]";
    public const string InspirePromptTag = "[ACTION:3]";
    public const string RallyOathPromptTag = "[ACTION:4]";
    public const string SoldierAppeasementPromptTag = "[ACTION:5]";
    public const string GatherCiviliansPromptTag = "[ACTION:6]";
    public const string CivilianRobberyPromptTag = "[ACTION:7]";
    public const string PlunderPromptTag = "[ACTION:8]";
    public const string MassacrePromptTag = "[ACTION:9]";
    public const string CulturalRepopulationPromptTag = "[ACTION:10]";
    public const string StopMassacrePromptTag = "[ACTION:11]";
    public const string ConstructiveCultureChangePromptTag = "[ACTION:12]";

    private static readonly Regex ActionTagRegex = new Regex(
        @"\[ACTION:(?<name>[^:\]\r\n]+)(?::\d+)?\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly SiegeInterventionActionKind[] CanonicalOrder =
    {
        SiegeInterventionActionKind.Mercy,
        SiegeInterventionActionKind.Relief,
        SiegeInterventionActionKind.Inspire,
        SiegeInterventionActionKind.RallyOath,
        SiegeInterventionActionKind.AppeaseSoldiers,
        SiegeInterventionActionKind.GatherCivilians,
        SiegeInterventionActionKind.CivilianRobbery,
        SiegeInterventionActionKind.Plunder,
        SiegeInterventionActionKind.Massacre,
        SiegeInterventionActionKind.CulturalRepopulation,
        SiegeInterventionActionKind.StopMassacre,
        SiegeInterventionActionKind.ConstructiveCultureChange,
    };

    private static readonly IReadOnlyDictionary<SiegeInterventionActionKind, string> ActionToCanonicalTag =
        new Dictionary<SiegeInterventionActionKind, string>
        {
            [SiegeInterventionActionKind.Mercy] = MercyPromptTag,
            [SiegeInterventionActionKind.Relief] = ReliefPromptTag,
            [SiegeInterventionActionKind.Inspire] = InspirePromptTag,
            [SiegeInterventionActionKind.RallyOath] = RallyOathPromptTag,
            [SiegeInterventionActionKind.AppeaseSoldiers] = SoldierAppeasementPromptTag,
            [SiegeInterventionActionKind.GatherCivilians] = GatherCiviliansPromptTag,
            [SiegeInterventionActionKind.CivilianRobbery] = CivilianRobberyPromptTag,
            [SiegeInterventionActionKind.Plunder] = PlunderPromptTag,
            [SiegeInterventionActionKind.Massacre] = MassacrePromptTag,
            [SiegeInterventionActionKind.CulturalRepopulation] = CulturalRepopulationPromptTag,
            [SiegeInterventionActionKind.StopMassacre] = StopMassacrePromptTag,
            [SiegeInterventionActionKind.ConstructiveCultureChange] = ConstructiveCultureChangePromptTag,
        };

    private static readonly IReadOnlyDictionary<SiegeInterventionActionKind, string[]> ActionToAliases = BuildAliases();

    public static bool TryParseName(string tagName, out SiegeInterventionActionKind action)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            action = SiegeInterventionActionKind.Unknown;
            return false;
        }

        string normalized = tagName.Trim();
        if (int.TryParse(normalized, out int actionNumber)
            && actionNumber >= 1
            && actionNumber <= CanonicalOrder.Length)
        {
            action = CanonicalOrder[actionNumber - 1];
            return true;
        }

        return LegacyTownTagAdapter.TryMapName(normalized, out action);
    }

    public static bool TryGetCanonicalTag(SiegeInterventionActionKind action, out string canonicalTag)
    {
        return ActionToCanonicalTag.TryGetValue(action, out canonicalTag);
    }

    public static IReadOnlyList<SiegeInterventionActionKind> GetCanonicalOrder()
    {
        return CanonicalOrder;
    }

    public static IReadOnlyList<string> GetAliases(SiegeInterventionActionKind action)
    {
        return ActionToAliases.TryGetValue(action, out string[] tags)
            ? tags
            : Array.Empty<string>();
    }

    public static IReadOnlyList<SiegeInterventionActionKind> ExtractKinds(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<SiegeInterventionActionKind>();
        }

        var actions = new List<SiegeInterventionActionKind>();
        var seen = new HashSet<SiegeInterventionActionKind>();
        foreach (Match match in ActionTagRegex.Matches(text))
        {
            if (TryParseName(match.Groups["name"].Value, out SiegeInterventionActionKind action)
                && seen.Add(action))
            {
                actions.Add(action);
            }
        }
        return actions;
    }

    public static bool ContainsRecognizedTag(string text)
    {
        return ExtractKinds(text).Count > 0;
    }

    public static string StripRecognizedTags(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return ActionTagRegex.Replace(text, match =>
            TryParseName(match.Groups["name"].Value, out _)
                ? string.Empty
                : match.Value);
    }

    public static IReadOnlyList<string> NormalizeToCanonicalTags(string text)
    {
        IReadOnlyList<SiegeInterventionActionKind> actions = ExtractKinds(text);
        if (actions.Count == 0)
        {
            return Array.Empty<string>();
        }

        var tags = new List<string>(actions.Count);
        foreach (SiegeInterventionActionKind action in actions)
        {
            if (TryGetCanonicalTag(action, out string tag))
            {
                tags.Add(tag);
            }
        }
        return tags;
    }

    private static IReadOnlyDictionary<SiegeInterventionActionKind, string[]> BuildAliases()
    {
        var aliases = new Dictionary<SiegeInterventionActionKind, string[]>();
        foreach (SiegeInterventionActionKind action in CanonicalOrder)
        {
            if (!ActionToCanonicalTag.TryGetValue(action, out string canonicalTag))
            {
                continue;
            }

            IReadOnlyList<string> legacyTags = LegacyTownTagAdapter.GetTags(action);
            var tags = new string[legacyTags.Count + 1];
            tags[0] = canonicalTag;
            for (int index = 0; index < legacyTags.Count; index++)
            {
                tags[index + 1] = legacyTags[index];
            }
            aliases[action] = tags;
        }
        return aliases;
    }
}
