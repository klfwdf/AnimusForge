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
            [SiegeAftermathCompatibilityTexts.Raw("action_mercy")] = SiegeInterventionActionKind.Mercy,
            ["SIEGE_RELIEF"] = SiegeInterventionActionKind.Relief,
            [SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_ffd017edd459")] = SiegeInterventionActionKind.Relief,
            ["SIEGE_INSPIRE"] = SiegeInterventionActionKind.Inspire,
            [SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_d92e6f19cd41")] = SiegeInterventionActionKind.Inspire,
            ["SIEGE_RALLY_OATH"] = SiegeInterventionActionKind.RallyOath,
            [SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_96d322ea12a9")] = SiegeInterventionActionKind.RallyOath,
            ["SIEGE_APPEASE_SOLDIERS"] = SiegeInterventionActionKind.AppeaseSoldiers,
            [SiegeAftermathCompatibilityTexts.Raw("appease_done_title")] = SiegeInterventionActionKind.AppeaseSoldiers,
            ["SIEGE_GATHER_CIVILIANS"] = SiegeInterventionActionKind.GatherCivilians,
            [SiegeAftermathCompatibilityTexts.Raw("gather_memory_title")] = SiegeInterventionActionKind.GatherCivilians,
            ["SIEGE_ROBBERY"] = SiegeInterventionActionKind.CivilianRobbery,
            [SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_7d5ba7987540")] = SiegeInterventionActionKind.CivilianRobbery,
            ["SIEGE_PLUNDER"] = SiegeInterventionActionKind.Plunder,
            [SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_42f0a44b7d95")] = SiegeInterventionActionKind.Plunder,
            ["SIEGE_MASSACRE"] = SiegeInterventionActionKind.Massacre,
            [SiegeAftermathCompatibilityTexts.Raw("loot_action_massacre")] = SiegeInterventionActionKind.Massacre,
            ["SIEGE_CULTURAL_REPOPULATION"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["SIEGE_PURGE_REPOPULATION"] = SiegeInterventionActionKind.CulturalRepopulation,
            [SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_ee470e68a8ef")] = SiegeInterventionActionKind.CulturalRepopulation,
            ["SIEGE_STOP_MASSACRE"] = SiegeInterventionActionKind.StopMassacre,
            ["SIEGE_CHANGE_CULTURE"] = SiegeInterventionActionKind.ConstructiveCultureChange,
        };

    private static readonly IReadOnlyDictionary<SiegeInterventionActionKind, string[]> ActionToTags =
        new Dictionary<SiegeInterventionActionKind, string[]>
        {
            [SiegeInterventionActionKind.Mercy] = Tags("SIEGE_MERCY", SiegeAftermathCompatibilityTexts.Raw("action_mercy")),
            [SiegeInterventionActionKind.Relief] = Tags("SIEGE_RELIEF", SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_ffd017edd459")),
            [SiegeInterventionActionKind.Inspire] = Tags("SIEGE_INSPIRE", SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_d92e6f19cd41")),
            [SiegeInterventionActionKind.RallyOath] = Tags("SIEGE_RALLY_OATH", SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_96d322ea12a9")),
            [SiegeInterventionActionKind.AppeaseSoldiers] = Tags("SIEGE_APPEASE_SOLDIERS", SiegeAftermathCompatibilityTexts.Raw("appease_done_title")),
            [SiegeInterventionActionKind.GatherCivilians] = Tags("SIEGE_GATHER_CIVILIANS", SiegeAftermathCompatibilityTexts.Raw("gather_memory_title")),
            [SiegeInterventionActionKind.CivilianRobbery] = Tags("SIEGE_ROBBERY", SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_7d5ba7987540")),
            [SiegeInterventionActionKind.Plunder] = Tags("SIEGE_PLUNDER", SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_42f0a44b7d95")),
            [SiegeInterventionActionKind.Massacre] = Tags("SIEGE_MASSACRE", SiegeAftermathCompatibilityTexts.Raw("loot_action_massacre")),
            [SiegeInterventionActionKind.CulturalRepopulation] = Tags("SIEGE_PURGE_REPOPULATION", "SIEGE_CULTURAL_REPOPULATION", SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_ee470e68a8ef")),
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
