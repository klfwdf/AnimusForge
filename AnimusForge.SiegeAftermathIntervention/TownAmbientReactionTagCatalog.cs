using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns non-mutating town reaction tags. Suggestion tags only notify the player;
/// the discontent tag only creates a one-time pending morale consequence.
/// </summary>
public static class TownAmbientReactionTagCatalog
{
	public const uint SuggestionMessageColor = 0xFFB8D8FFu;
    public const string SoldierDiscontentTag = "[ACTION:TOWN_SOLDIER_DISCONTENT]";
    public const string SuggestMercyTag = "[ACTION:TOWN_SUGGEST_MERCY]";
    public const string SuggestReliefTag = "[ACTION:TOWN_SUGGEST_RELIEF]";
    public const string SuggestInspireTag = "[ACTION:TOWN_SUGGEST_INSPIRE]";
    public const string SuggestRallyOathTag = "[ACTION:TOWN_SUGGEST_RALLY_OATH]";
    public const string SuggestAppeaseSoldiersTag = "[ACTION:TOWN_SUGGEST_APPEASE_SOLDIERS]";
    public const string SuggestGatherCiviliansTag = "[ACTION:TOWN_SUGGEST_GATHER_CIVILIANS]";
    public const string SuggestCivilianRobberyTag = "[ACTION:TOWN_SUGGEST_CIVILIAN_ROBBERY]";
    public const string SuggestPlunderTag = "[ACTION:TOWN_SUGGEST_PLUNDER]";
    public const string SuggestMassacreTag = "[ACTION:TOWN_SUGGEST_MASSACRE]";
    public const string SuggestCulturalRepopulationTag = "[ACTION:TOWN_SUGGEST_CULTURAL_REPOPULATION]";
    public const string SuggestStopMassacreTag = "[ACTION:TOWN_SUGGEST_STOP_MASSACRE]";
    public const string SuggestConstructiveCultureChangeTag = "[ACTION:TOWN_SUGGEST_CONSTRUCTIVE_CULTURE_CHANGE]";

    private static readonly Regex ActionTagRegex = new Regex(
        @"\[ACTION:(?<name>TOWN_(?:SOLDIER_DISCONTENT|SUGGEST_[A-Z_]+))\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, TownAmbientReactionActionKind> NameToKind =
        new Dictionary<string, TownAmbientReactionActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["TOWN_SOLDIER_DISCONTENT"] = TownAmbientReactionActionKind.SoldierDiscontent,
            ["TOWN_SUGGEST_MERCY"] = TownAmbientReactionActionKind.SuggestMercy,
            ["TOWN_SUGGEST_RELIEF"] = TownAmbientReactionActionKind.SuggestRelief,
            ["TOWN_SUGGEST_INSPIRE"] = TownAmbientReactionActionKind.SuggestInspire,
            ["TOWN_SUGGEST_RALLY_OATH"] = TownAmbientReactionActionKind.SuggestRallyOath,
            ["TOWN_SUGGEST_APPEASE_SOLDIERS"] = TownAmbientReactionActionKind.SuggestAppeaseSoldiers,
            ["TOWN_SUGGEST_GATHER_CIVILIANS"] = TownAmbientReactionActionKind.SuggestGatherCivilians,
            ["TOWN_SUGGEST_CIVILIAN_ROBBERY"] = TownAmbientReactionActionKind.SuggestCivilianRobbery,
            ["TOWN_SUGGEST_PLUNDER"] = TownAmbientReactionActionKind.SuggestPlunder,
            ["TOWN_SUGGEST_MASSACRE"] = TownAmbientReactionActionKind.SuggestMassacre,
            ["TOWN_SUGGEST_CULTURAL_REPOPULATION"] = TownAmbientReactionActionKind.SuggestCulturalRepopulation,
            ["TOWN_SUGGEST_STOP_MASSACRE"] = TownAmbientReactionActionKind.SuggestStopMassacre,
            ["TOWN_SUGGEST_CONSTRUCTIVE_CULTURE_CHANGE"] = TownAmbientReactionActionKind.SuggestConstructiveCultureChange,
        };

    private static readonly IReadOnlyDictionary<TownAmbientReactionActionKind, string> KindToTag =
        new Dictionary<TownAmbientReactionActionKind, string>
        {
            [TownAmbientReactionActionKind.SoldierDiscontent] = SoldierDiscontentTag,
            [TownAmbientReactionActionKind.SuggestMercy] = SuggestMercyTag,
            [TownAmbientReactionActionKind.SuggestRelief] = SuggestReliefTag,
            [TownAmbientReactionActionKind.SuggestInspire] = SuggestInspireTag,
            [TownAmbientReactionActionKind.SuggestRallyOath] = SuggestRallyOathTag,
            [TownAmbientReactionActionKind.SuggestAppeaseSoldiers] = SuggestAppeaseSoldiersTag,
            [TownAmbientReactionActionKind.SuggestGatherCivilians] = SuggestGatherCiviliansTag,
            [TownAmbientReactionActionKind.SuggestCivilianRobbery] = SuggestCivilianRobberyTag,
            [TownAmbientReactionActionKind.SuggestPlunder] = SuggestPlunderTag,
            [TownAmbientReactionActionKind.SuggestMassacre] = SuggestMassacreTag,
            [TownAmbientReactionActionKind.SuggestCulturalRepopulation] = SuggestCulturalRepopulationTag,
            [TownAmbientReactionActionKind.SuggestStopMassacre] = SuggestStopMassacreTag,
            [TownAmbientReactionActionKind.SuggestConstructiveCultureChange] = SuggestConstructiveCultureChangeTag,
        };

    private static readonly IReadOnlyDictionary<TownAmbientReactionActionKind, SiegeInterventionActionKind> SuggestionToAction =
        new Dictionary<TownAmbientReactionActionKind, SiegeInterventionActionKind>
        {
            [TownAmbientReactionActionKind.SuggestMercy] = SiegeInterventionActionKind.Mercy,
            [TownAmbientReactionActionKind.SuggestRelief] = SiegeInterventionActionKind.Relief,
            [TownAmbientReactionActionKind.SuggestInspire] = SiegeInterventionActionKind.Inspire,
            [TownAmbientReactionActionKind.SuggestRallyOath] = SiegeInterventionActionKind.RallyOath,
            [TownAmbientReactionActionKind.SuggestAppeaseSoldiers] = SiegeInterventionActionKind.AppeaseSoldiers,
            [TownAmbientReactionActionKind.SuggestGatherCivilians] = SiegeInterventionActionKind.GatherCivilians,
            [TownAmbientReactionActionKind.SuggestCivilianRobbery] = SiegeInterventionActionKind.CivilianRobbery,
            [TownAmbientReactionActionKind.SuggestPlunder] = SiegeInterventionActionKind.Plunder,
            [TownAmbientReactionActionKind.SuggestMassacre] = SiegeInterventionActionKind.Massacre,
            [TownAmbientReactionActionKind.SuggestCulturalRepopulation] = SiegeInterventionActionKind.CulturalRepopulation,
            [TownAmbientReactionActionKind.SuggestStopMassacre] = SiegeInterventionActionKind.StopMassacre,
            [TownAmbientReactionActionKind.SuggestConstructiveCultureChange] = SiegeInterventionActionKind.ConstructiveCultureChange,
        };

    public static IReadOnlyList<TownAmbientReactionActionKind> ExtractKinds(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<TownAmbientReactionActionKind>();
        }

        var result = new List<TownAmbientReactionActionKind>();
        var seen = new HashSet<TownAmbientReactionActionKind>();
        foreach (Match match in ActionTagRegex.Matches(text))
        {
            if (NameToKind.TryGetValue(match.Groups["name"].Value, out TownAmbientReactionActionKind kind)
                && seen.Add(kind))
            {
                result.Add(kind);
            }
        }
        return result;
    }

    public static bool ContainsRecognizedTag(string text)
    {
        return ExtractKinds(text).Count > 0;
    }

    public static bool TryGetCanonicalTag(TownAmbientReactionActionKind kind, out string tag)
    {
        return KindToTag.TryGetValue(kind, out tag);
    }

    public static bool TryGetSuggestedAction(
        TownAmbientReactionActionKind kind,
        out SiegeInterventionActionKind action)
    {
        return SuggestionToAction.TryGetValue(kind, out action);
    }

    public static bool TryGetSuggestionKind(
        SiegeInterventionActionKind action,
        out TownAmbientReactionActionKind kind)
    {
        foreach (KeyValuePair<TownAmbientReactionActionKind, SiegeInterventionActionKind> pair in SuggestionToAction)
        {
            if (pair.Value == action)
            {
                kind = pair.Key;
                return true;
            }
        }

        kind = TownAmbientReactionActionKind.Unknown;
        return false;
    }

    public static string StripRecognizedTags(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return ActionTagRegex.Replace(text, string.Empty);
    }
}
