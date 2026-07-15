using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only action-tag vocabulary. It is deliberately separate from the ten town GCCZ tags.
/// </summary>
public static class SiegeCastleActionTagCatalog
{
    public const string ProposeRecruitPrisonersTag = "[ACTION:CASTLE_PROPOSE_RECRUIT_PRISONERS]";

    public const string ProposeSlaughterPrisonersTag = "[ACTION:CASTLE_PROPOSE_SLAUGHTER_PRISONERS]";

    public const string RecruitPrisonersTag = "[ACTION:CASTLE_RECRUIT_PRISONERS]";

    public const string SlaughterPrisonersTag = "[ACTION:CASTLE_SLAUGHTER_PRISONERS]";

    public const string AppeaseSoldiersTag = "[ACTION:CASTLE_APPEASE_SOLDIERS]";

    public const string ProposeRecruitPrisonersTagPattern = @"\[ACTION:(?:CASTLE_PROPOSE_RECRUIT_PRISONERS|SIEGE_CASTLE_PROPOSE_RECRUIT_PRISONERS|城堡提议收编战俘)\]";

    public const string ProposeSlaughterPrisonersTagPattern = @"\[ACTION:(?:CASTLE_PROPOSE_SLAUGHTER_PRISONERS|SIEGE_CASTLE_PROPOSE_SLAUGHTER_PRISONERS|城堡提议屠戮战俘)\]";

    public const string RecruitPrisonersTagPattern = @"\[ACTION:(?:CASTLE_RECRUIT_PRISONERS|SIEGE_CASTLE_RECRUIT_PRISONERS|城堡收编战俘)\]";

    public const string SlaughterPrisonersTagPattern = @"\[ACTION:(?:CASTLE_SLAUGHTER_PRISONERS|SIEGE_CASTLE_SLAUGHTER_PRISONERS|城堡屠戮战俘)\]";

    public const string AppeaseSoldiersTagPattern = @"\[ACTION:(?:CASTLE_APPEASE_SOLDIERS|SIEGE_CASTLE_APPEASE_SOLDIERS|城堡安兵)\]";

    public const string AnyActionTagPattern = @"\[ACTION:(?:CASTLE_(?:PROPOSE_(?:RECRUIT|SLAUGHTER)_PRISONERS|RECRUIT_PRISONERS|SLAUGHTER_PRISONERS|APPEASE_SOLDIERS)|SIEGE_CASTLE_(?:PROPOSE_(?:RECRUIT|SLAUGHTER)_PRISONERS|RECRUIT_PRISONERS|SLAUGHTER_PRISONERS|APPEASE_SOLDIERS)|城堡提议收编战俘|城堡提议屠戮战俘|城堡收编战俘|城堡屠戮战俘|城堡安兵)\]";

    private static readonly Regex ActionTagRegex = new Regex(
        @"\[ACTION:(?<name>CASTLE_(?:PROPOSE_(?:RECRUIT|SLAUGHTER)_PRISONERS|RECRUIT_PRISONERS|SLAUGHTER_PRISONERS|APPEASE_SOLDIERS)|SIEGE_CASTLE_(?:PROPOSE_(?:RECRUIT|SLAUGHTER)_PRISONERS|RECRUIT_PRISONERS|SLAUGHTER_PRISONERS|APPEASE_SOLDIERS)|城堡提议收编战俘|城堡提议屠戮战俘|城堡收编战俘|城堡屠戮战俘|城堡安兵)\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, SiegeCastleActionKind> NameToKind =
        new Dictionary<string, SiegeCastleActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["CASTLE_PROPOSE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.ProposeRecruitPrisoners,
            ["SIEGE_CASTLE_PROPOSE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.ProposeRecruitPrisoners,
            ["城堡提议收编战俘"] = SiegeCastleActionKind.ProposeRecruitPrisoners,
            ["CASTLE_PROPOSE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.ProposeSlaughterPrisoners,
            ["SIEGE_CASTLE_PROPOSE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.ProposeSlaughterPrisoners,
            ["城堡提议屠戮战俘"] = SiegeCastleActionKind.ProposeSlaughterPrisoners,
            ["CASTLE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.RecruitPrisoners,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.RecruitPrisoners,
            ["城堡收编战俘"] = SiegeCastleActionKind.RecruitPrisoners,
            ["CASTLE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["SIEGE_CASTLE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["城堡屠戮战俘"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["CASTLE_APPEASE_SOLDIERS"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["SIEGE_CASTLE_APPEASE_SOLDIERS"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["城堡安兵"] = SiegeCastleActionKind.AppeaseSoldiers
        };

    private static readonly SiegeCastleActionKind[] CanonicalOrder =
    {
        SiegeCastleActionKind.ProposeRecruitPrisoners,
        SiegeCastleActionKind.ProposeSlaughterPrisoners,
        SiegeCastleActionKind.RecruitPrisoners,
        SiegeCastleActionKind.SlaughterPrisoners,
        SiegeCastleActionKind.AppeaseSoldiers
    };

    private static readonly IReadOnlyDictionary<SiegeCastleActionKind, string> KindToCanonicalTag =
        new Dictionary<SiegeCastleActionKind, string>
        {
            [SiegeCastleActionKind.ProposeRecruitPrisoners] = ProposeRecruitPrisonersTag,
            [SiegeCastleActionKind.ProposeSlaughterPrisoners] = ProposeSlaughterPrisonersTag,
            [SiegeCastleActionKind.RecruitPrisoners] = RecruitPrisonersTag,
            [SiegeCastleActionKind.SlaughterPrisoners] = SlaughterPrisonersTag,
            [SiegeCastleActionKind.AppeaseSoldiers] = AppeaseSoldiersTag
        };

    private static readonly IReadOnlyDictionary<SiegeCastleActionKind, string[]> KindToAliases =
        new Dictionary<SiegeCastleActionKind, string[]>
        {
            [SiegeCastleActionKind.ProposeRecruitPrisoners] = new[] { ProposeRecruitPrisonersTag, "[ACTION:SIEGE_CASTLE_PROPOSE_RECRUIT_PRISONERS]", "[ACTION:城堡提议收编战俘]" },
            [SiegeCastleActionKind.ProposeSlaughterPrisoners] = new[] { ProposeSlaughterPrisonersTag, "[ACTION:SIEGE_CASTLE_PROPOSE_SLAUGHTER_PRISONERS]", "[ACTION:城堡提议屠戮战俘]" },
            [SiegeCastleActionKind.RecruitPrisoners] = new[] { RecruitPrisonersTag, "[ACTION:SIEGE_CASTLE_RECRUIT_PRISONERS]", "[ACTION:城堡收编战俘]" },
            [SiegeCastleActionKind.SlaughterPrisoners] = new[] { SlaughterPrisonersTag, "[ACTION:SIEGE_CASTLE_SLAUGHTER_PRISONERS]", "[ACTION:城堡屠戮战俘]" },
            [SiegeCastleActionKind.AppeaseSoldiers] = new[] { AppeaseSoldiersTag, "[ACTION:SIEGE_CASTLE_APPEASE_SOLDIERS]", "[ACTION:城堡安兵]" }
        };

    public static bool TryParseName(string tagName, out SiegeCastleActionKind kind)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            kind = SiegeCastleActionKind.Unknown;
            return false;
        }

        return NameToKind.TryGetValue(tagName.Trim(), out kind);
    }

    public static bool TryGetCanonicalTag(SiegeCastleActionKind kind, out string canonicalTag)
    {
        return KindToCanonicalTag.TryGetValue(kind, out canonicalTag);
    }

    public static IReadOnlyList<SiegeCastleActionKind> GetCanonicalOrder()
    {
        return CanonicalOrder;
    }

    public static IReadOnlyList<string> GetAliases(SiegeCastleActionKind kind)
    {
        return KindToAliases.TryGetValue(kind, out string[] aliases) ? aliases : Array.Empty<string>();
    }

    public static IReadOnlyList<SiegeCastleActionKind> ExtractKinds(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<SiegeCastleActionKind>();
        }

        var result = new List<SiegeCastleActionKind>();
        var seen = new HashSet<SiegeCastleActionKind>();
        foreach (Match match in ActionTagRegex.Matches(text))
        {
            string name = match.Groups["name"].Value;
            if (TryParseName(name, out SiegeCastleActionKind kind) && seen.Add(kind))
            {
                result.Add(kind);
            }
        }

        return result;
    }

    public static IReadOnlyList<string> NormalizeToCanonicalTags(string text)
    {
        IReadOnlyList<SiegeCastleActionKind> kinds = ExtractKinds(text);
        if (kinds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(kinds.Count);
        foreach (SiegeCastleActionKind kind in kinds)
        {
            if (TryGetCanonicalTag(kind, out string tag))
            {
                result.Add(tag);
            }
        }

        return result;
    }
}
