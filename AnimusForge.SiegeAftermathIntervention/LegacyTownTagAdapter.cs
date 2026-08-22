using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Maps historical GCCZ town tag names into the current semantic action model.
/// These aliases are accepted only inside explicit machine tags and never from dialogue text.
/// </summary>
public static class LegacyTownTagAdapter
{
    private static readonly Regex TaggedNameRegex = new Regex(
        @"\[ACTION:(?<name>[^:\]\r\n]+)(?::\d+)?\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, SiegeInterventionActionKind> NameToAction =
        new Dictionary<string, SiegeInterventionActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["SIEGE_MERCY"] = SiegeInterventionActionKind.Mercy,
            ["\u5bbd\u6055"] = SiegeInterventionActionKind.Mercy,
            ["SIEGE_RELIEF"] = SiegeInterventionActionKind.Relief,
            ["\u6551\u6d4e"] = SiegeInterventionActionKind.Relief,
            ["SIEGE_INSPIRE"] = SiegeInterventionActionKind.Inspire,
            ["\u5ba3\u629a"] = SiegeInterventionActionKind.Inspire,
            ["SIEGE_RALLY_OATH"] = SiegeInterventionActionKind.RallyOath,
            ["\u76df\u8a93"] = SiegeInterventionActionKind.RallyOath,
            ["SIEGE_APPEASE_SOLDIERS"] = SiegeInterventionActionKind.AppeaseSoldiers,
            ["\u5b89\u5175"] = SiegeInterventionActionKind.AppeaseSoldiers,
            ["SIEGE_GATHER_CIVILIANS"] = SiegeInterventionActionKind.GatherCivilians,
            ["\u53ec\u96c6"] = SiegeInterventionActionKind.GatherCivilians,
            ["SIEGE_ROBBERY"] = SiegeInterventionActionKind.CivilianRobbery,
            ["\u62a2\u94b1"] = SiegeInterventionActionKind.CivilianRobbery,
            ["SIEGE_PLUNDER"] = SiegeInterventionActionKind.Plunder,
            ["\u641c\u63a0"] = SiegeInterventionActionKind.Plunder,
            ["SIEGE_MASSACRE"] = SiegeInterventionActionKind.Massacre,
            ["\u8840\u6d17"] = SiegeInterventionActionKind.Massacre,
            ["SIEGE_CULTURAL_REPOPULATION"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["SIEGE_PURGE_REPOPULATION"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["\u6b96\u6c11"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["SIEGE_STOP_MASSACRE"] = SiegeInterventionActionKind.StopMassacre,
            ["SIEGE_CHANGE_CULTURE"] = SiegeInterventionActionKind.ConstructiveCultureChange,
        };

    private static readonly IReadOnlyDictionary<SiegeInterventionActionKind, string[]> ActionToTags =
        new Dictionary<SiegeInterventionActionKind, string[]>
        {
            [SiegeInterventionActionKind.Mercy] = Tags("SIEGE_MERCY", "\u5bbd\u6055"),
            [SiegeInterventionActionKind.Relief] = Tags("SIEGE_RELIEF", "\u6551\u6d4e"),
            [SiegeInterventionActionKind.Inspire] = Tags("SIEGE_INSPIRE", "\u5ba3\u629a"),
            [SiegeInterventionActionKind.RallyOath] = Tags("SIEGE_RALLY_OATH", "\u76df\u8a93"),
            [SiegeInterventionActionKind.AppeaseSoldiers] = Tags("SIEGE_APPEASE_SOLDIERS", "\u5b89\u5175"),
            [SiegeInterventionActionKind.GatherCivilians] = Tags("SIEGE_GATHER_CIVILIANS", "\u53ec\u96c6"),
            [SiegeInterventionActionKind.CivilianRobbery] = Tags("SIEGE_ROBBERY", "\u62a2\u94b1"),
            [SiegeInterventionActionKind.Plunder] = Tags("SIEGE_PLUNDER", "\u641c\u63a0"),
            [SiegeInterventionActionKind.Massacre] = Tags("SIEGE_MASSACRE", "\u8840\u6d17"),
            [SiegeInterventionActionKind.CulturalRepopulation] = Tags("SIEGE_PURGE_REPOPULATION", "SIEGE_CULTURAL_REPOPULATION", "\u6b96\u6c11"),
            [SiegeInterventionActionKind.StopMassacre] = Tags("SIEGE_STOP_MASSACRE"),
            [SiegeInterventionActionKind.ConstructiveCultureChange] = Tags("SIEGE_CHANGE_CULTURE"),
        };

    public static bool TryMapName(string tagName, out SiegeInterventionActionKind action)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            action = SiegeInterventionActionKind.Unknown;
            return false;
        }

        return NameToAction.TryGetValue(tagName.Trim(), out action);
    }

    public static IReadOnlyList<string> GetTags(SiegeInterventionActionKind action)
    {
        return ActionToTags.TryGetValue(action, out string[] tags)
            ? tags
            : Array.Empty<string>();
    }

    public static bool ContainsLegacyTag(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (Match match in TaggedNameRegex.Matches(text))
        {
            if (TryMapName(match.Groups["name"].Value, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] Tags(params string[] names)
    {
        var tags = new string[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            tags[index] = "[ACTION:" + names[index] + "]";
        }
        return tags;
    }
}
