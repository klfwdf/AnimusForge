using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Low-coupling extraction of the current fused action-tag vocabulary.
/// Numeric tags are the compact prompt-facing vocabulary; Chinese tags remain the runtime canonical output.
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

    public const string MercyTagPattern = @"\[ACTION:(?:1|SIEGE_MERCY|宽恕)\]";

    public const string ReliefTagPattern = @"\[ACTION:(?:2|SIEGE_RELIEF|救济)\]";

    public const string InspireTagPattern = @"\[ACTION:(?:3|SIEGE_INSPIRE|宣抚)\]";

    public const string RallyOathTagPattern = @"\[ACTION:(?:4|SIEGE_RALLY_OATH|盟誓)\]";

    public const string SoldierAppeasementTagPattern = @"\[ACTION:(?:5|SIEGE_APPEASE_SOLDIERS|安兵)\]";

    public const string GatherCiviliansTagPattern = @"\[ACTION:(?:6|SIEGE_GATHER_CIVILIANS|召集)\]";

    public const string CivilianRobberyTagPattern = @"\[ACTION:(?:7|SIEGE_ROBBERY|抢钱)\]";

    public const string PlunderTagPattern = @"\[ACTION:(?:8|SIEGE_PLUNDER|搜掠)\]";

    public const string MassacreTagPattern = @"\[ACTION:(?:9|SIEGE_MASSACRE|血洗)\]";

    public const string CulturalRepopulationTagPattern = @"\[ACTION:(?:10|SIEGE_CULTURAL_REPOPULATION|SIEGE_PURGE_REPOPULATION|殖民)\]";

    public const string StopMassacreTagPattern = @"\[ACTION:(?:11|SIEGE_STOP_MASSACRE)\]";

    public const string AnyActionTagPattern = @"\[ACTION:(?:10|[1-9]|SIEGE_[A-Z_]+|宽恕|救济|宣抚|盟誓|安兵|召集|抢钱|搜掠|血洗|殖民)(?::\d+)?\]";

    private static readonly Regex ActionTagRegex = new Regex(
        @"\[ACTION:(?<name>10|[1-9]|SIEGE_[A-Z_]+|宽恕|救济|宣抚|盟誓|安兵|召集|抢钱|搜掠|血洗|殖民)(?::\d+)?\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StopMassacreActionTagRegex = new Regex(
        StopMassacreTagPattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, SiegeInterventionActionKind> NameToKind =
        new Dictionary<string, SiegeInterventionActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = SiegeInterventionActionKind.Mercy,
            ["SIEGE_MERCY"] = SiegeInterventionActionKind.Mercy,
            ["宽恕"] = SiegeInterventionActionKind.Mercy,
            ["2"] = SiegeInterventionActionKind.Relief,
            ["SIEGE_RELIEF"] = SiegeInterventionActionKind.Relief,
            ["救济"] = SiegeInterventionActionKind.Relief,
            ["3"] = SiegeInterventionActionKind.Inspire,
            ["SIEGE_INSPIRE"] = SiegeInterventionActionKind.Inspire,
            ["宣抚"] = SiegeInterventionActionKind.Inspire,
            ["4"] = SiegeInterventionActionKind.RallyOath,
            ["SIEGE_RALLY_OATH"] = SiegeInterventionActionKind.RallyOath,
            ["盟誓"] = SiegeInterventionActionKind.RallyOath,
            ["5"] = SiegeInterventionActionKind.AppeaseSoldiers,
            ["SIEGE_APPEASE_SOLDIERS"] = SiegeInterventionActionKind.AppeaseSoldiers,
            ["安兵"] = SiegeInterventionActionKind.AppeaseSoldiers,
            ["6"] = SiegeInterventionActionKind.GatherCivilians,
            ["SIEGE_GATHER_CIVILIANS"] = SiegeInterventionActionKind.GatherCivilians,
            ["召集"] = SiegeInterventionActionKind.GatherCivilians,
            ["7"] = SiegeInterventionActionKind.CivilianRobbery,
            ["SIEGE_ROBBERY"] = SiegeInterventionActionKind.CivilianRobbery,
            ["抢钱"] = SiegeInterventionActionKind.CivilianRobbery,
            ["8"] = SiegeInterventionActionKind.Plunder,
            ["SIEGE_PLUNDER"] = SiegeInterventionActionKind.Plunder,
            ["搜掠"] = SiegeInterventionActionKind.Plunder,
            ["9"] = SiegeInterventionActionKind.Massacre,
            ["SIEGE_MASSACRE"] = SiegeInterventionActionKind.Massacre,
            ["血洗"] = SiegeInterventionActionKind.Massacre,
            ["10"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["SIEGE_CULTURAL_REPOPULATION"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["SIEGE_PURGE_REPOPULATION"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["殖民"] = SiegeInterventionActionKind.CulturalRepopulation,
            ["11"] = SiegeInterventionActionKind.StopMassacre,
            ["SIEGE_STOP_MASSACRE"] = SiegeInterventionActionKind.StopMassacre,
        };

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
    };

    private static readonly IReadOnlyDictionary<SiegeInterventionActionKind, string> KindToCanonicalTag =
        new Dictionary<SiegeInterventionActionKind, string>
        {
            [SiegeInterventionActionKind.Mercy] = "[ACTION:宽恕]",
            [SiegeInterventionActionKind.Relief] = "[ACTION:救济]",
            [SiegeInterventionActionKind.Inspire] = "[ACTION:宣抚]",
            [SiegeInterventionActionKind.RallyOath] = "[ACTION:盟誓]",
            [SiegeInterventionActionKind.AppeaseSoldiers] = "[ACTION:安兵]",
            [SiegeInterventionActionKind.GatherCivilians] = "[ACTION:召集]",
            [SiegeInterventionActionKind.CivilianRobbery] = "[ACTION:抢钱]",
            [SiegeInterventionActionKind.Plunder] = "[ACTION:搜掠]",
            [SiegeInterventionActionKind.Massacre] = "[ACTION:血洗]",
            [SiegeInterventionActionKind.CulturalRepopulation] = "[ACTION:殖民]",
            [SiegeInterventionActionKind.StopMassacre] = "[ACTION:SIEGE_STOP_MASSACRE]",
        };

    private static readonly IReadOnlyDictionary<SiegeInterventionActionKind, string[]> KindToAliases =
        new Dictionary<SiegeInterventionActionKind, string[]>
        {
            [SiegeInterventionActionKind.Mercy] = new[] { MercyPromptTag, "[ACTION:宽恕]", "[ACTION:SIEGE_MERCY]" },
            [SiegeInterventionActionKind.Relief] = new[] { ReliefPromptTag, "[ACTION:救济]", "[ACTION:SIEGE_RELIEF]" },
            [SiegeInterventionActionKind.Inspire] = new[] { InspirePromptTag, "[ACTION:宣抚]", "[ACTION:SIEGE_INSPIRE]" },
            [SiegeInterventionActionKind.RallyOath] = new[] { RallyOathPromptTag, "[ACTION:盟誓]", "[ACTION:SIEGE_RALLY_OATH]" },
            [SiegeInterventionActionKind.AppeaseSoldiers] = new[] { SoldierAppeasementPromptTag, "[ACTION:安兵]", "[ACTION:SIEGE_APPEASE_SOLDIERS]" },
            [SiegeInterventionActionKind.GatherCivilians] = new[] { GatherCiviliansPromptTag, "[ACTION:召集]", "[ACTION:SIEGE_GATHER_CIVILIANS]" },
            [SiegeInterventionActionKind.CivilianRobbery] = new[] { CivilianRobberyPromptTag, "[ACTION:抢钱]", "[ACTION:SIEGE_ROBBERY]" },
            [SiegeInterventionActionKind.Plunder] = new[] { PlunderPromptTag, "[ACTION:搜掠]", "[ACTION:SIEGE_PLUNDER]" },
            [SiegeInterventionActionKind.Massacre] = new[] { MassacrePromptTag, "[ACTION:血洗]", "[ACTION:SIEGE_MASSACRE]" },
            [SiegeInterventionActionKind.CulturalRepopulation] = new[] { CulturalRepopulationPromptTag, "[ACTION:殖民]", "[ACTION:SIEGE_PURGE_REPOPULATION]", "[ACTION:SIEGE_CULTURAL_REPOPULATION]" },
            [SiegeInterventionActionKind.StopMassacre] = new[] { StopMassacrePromptTag, "[ACTION:SIEGE_STOP_MASSACRE]" },
        };

    public static bool TryParseName(string tagName, out SiegeInterventionActionKind kind)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            kind = SiegeInterventionActionKind.Unknown;
            return false;
        }

        return NameToKind.TryGetValue(tagName.Trim(), out kind);
    }

    public static bool TryGetCanonicalTag(SiegeInterventionActionKind kind, out string canonicalTag)
    {
        return KindToCanonicalTag.TryGetValue(kind, out canonicalTag);
    }

    public static IReadOnlyList<SiegeInterventionActionKind> GetCanonicalOrder()
    {
        return CanonicalOrder;
    }

    public static IReadOnlyList<string> GetAliases(SiegeInterventionActionKind kind)
    {
        return KindToAliases.TryGetValue(kind, out var aliases) ? aliases : Array.Empty<string>();
    }

    public static IReadOnlyList<SiegeInterventionActionKind> ExtractKinds(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<SiegeInterventionActionKind>();
        }

        var result = new List<SiegeInterventionActionKind>();
        var seen = new HashSet<SiegeInterventionActionKind>();
        foreach (Match match in ActionTagRegex.Matches(text))
        {
            var name = match.Groups["name"].Value;
            if (TryParseName(name, out var kind) && seen.Add(kind))
            {
                result.Add(kind);
            }
        }

        if (StopMassacreActionTagRegex.IsMatch(text) && seen.Add(SiegeInterventionActionKind.StopMassacre))
        {
            result.Add(SiegeInterventionActionKind.StopMassacre);
        }

        return result;
    }

    public static IReadOnlyList<string> NormalizeToCanonicalTags(string text)
    {
        var kinds = ExtractKinds(text);
        if (kinds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(kinds.Count);
        foreach (var kind in kinds)
        {
            if (TryGetCanonicalTag(kind, out var tag))
            {
                result.Add(tag);
            }
        }

        return result;
    }
}
