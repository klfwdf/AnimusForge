using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only action vocabulary. Canonical output always uses CASTLE_ prefixes;
/// a few historical aliases remain input-only for old saves/prompts.
/// </summary>
public static class SiegeCastleActionTagCatalog
{
    public const string ProposeRecruitPrisonersTag = "[ACTION:CASTLE_PROPOSE_RECRUIT_PRISONERS]";
    public const string ProposeSlaughterPrisonersTag = "[ACTION:CASTLE_PROPOSE_SLAUGHTER_PRISONERS]";
    public const string ProposeReleasePrisonersTag = "[ACTION:CASTLE_PROPOSE_RELEASE_PRISONERS]";
    public const string ProposeSellPrisonersTag = "[ACTION:CASTLE_PROPOSE_SELL_PRISONERS]";
    public const string ProposeLaborPrisonersTag = "[ACTION:CASTLE_PROPOSE_LABOR_PRISONERS]";
    public const string ProposeInstructorPrisonersTag = "[ACTION:CASTLE_PROPOSE_INSTRUCTOR_PRISONERS]";
    public const string TreatPrisonersTag = "[ACTION:CASTLE_TREAT_PRISONERS]";
    public const string ReceiveArmamentsTag = "[ACTION:CASTLE_RECEIVE_ARMAMENTS]";
    public const string SlaughterPrisonersTag = "[ACTION:CASTLE_SLAUGHTER_PRISONERS]";
    public const string ReleasePrisonersTag = "[ACTION:CASTLE_RELEASE_PRISONERS]";
    public const string SellPrisonersTag = "[ACTION:CASTLE_SELL_PRISONERS]";
    public const string RecruitPrisonersVoluntaryTag = "[ACTION:CASTLE_RECRUIT_PRISONERS_VOLUNTARY]";
    public const string RecruitPrisonersForcedTag = "[ACTION:CASTLE_RECRUIT_PRISONERS_FORCED]";
    public const string LaborPrisonersVoluntaryTag = "[ACTION:CASTLE_LABOR_PRISONERS_VOLUNTARY]";
    public const string LaborPrisonersForcedTag = "[ACTION:CASTLE_LABOR_PRISONERS_FORCED]";
    public const string InstructorPrisonersVoluntaryTag = "[ACTION:CASTLE_INSTRUCTOR_PRISONERS_VOLUNTARY]";
    public const string InstructorPrisonersForcedTag = "[ACTION:CASTLE_INSTRUCTOR_PRISONERS_FORCED]";
    public const string AppeaseSoldiersTag = "[ACTION:CASTLE_APPEASE_SOLDIERS]";
    public const string RecruitLordTag = "[ACTION:CASTLE_RECRUIT_LORD]";
    public const string ExecuteLordTag = "[ACTION:CASTLE_EXECUTE_LORD]";

    // Broad enough to strip stale castle-prefixed output, but TryParseName remains authoritative.
    public const string AnyActionTagPattern = @"\[ACTION:(?:(?:SIEGE_)?CASTLE_[A-Z_]+|城堡[^\]\r\n]+|善待俘虏|接收军械|收编领主|处决领主)\]";

    private static readonly Regex ActionTagRegex = new Regex(
        @"\[ACTION:(?<name>(?:SIEGE_)?CASTLE_[A-Z_]+|城堡[^\]\r\n]+|善待俘虏|接收军械|收编领主|处决领主)\]",
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
            ["CASTLE_PROPOSE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ProposeReleasePrisoners,
            ["SIEGE_CASTLE_PROPOSE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ProposeReleasePrisoners,
            ["城堡提议释放战俘"] = SiegeCastleActionKind.ProposeReleasePrisoners,
            ["CASTLE_PROPOSE_SELL_PRISONERS"] = SiegeCastleActionKind.ProposeSellPrisoners,
            ["SIEGE_CASTLE_PROPOSE_SELL_PRISONERS"] = SiegeCastleActionKind.ProposeSellPrisoners,
            ["城堡提议贩卖战俘"] = SiegeCastleActionKind.ProposeSellPrisoners,
            ["CASTLE_PROPOSE_LABOR_PRISONERS"] = SiegeCastleActionKind.ProposeLaborPrisoners,
            ["SIEGE_CASTLE_PROPOSE_LABOR_PRISONERS"] = SiegeCastleActionKind.ProposeLaborPrisoners,
            ["城堡提议劳役战俘"] = SiegeCastleActionKind.ProposeLaborPrisoners,
            ["CASTLE_PROPOSE_INSTRUCTOR_PRISONERS"] = SiegeCastleActionKind.ProposeInstructorPrisoners,
            ["SIEGE_CASTLE_PROPOSE_INSTRUCTOR_PRISONERS"] = SiegeCastleActionKind.ProposeInstructorPrisoners,
            ["城堡提议战俘充当教官"] = SiegeCastleActionKind.ProposeInstructorPrisoners,
            ["CASTLE_TREAT_PRISONERS"] = SiegeCastleActionKind.TreatPrisoners,
            ["SIEGE_CASTLE_TREAT_PRISONERS"] = SiegeCastleActionKind.TreatPrisoners,
            ["城堡善待俘虏"] = SiegeCastleActionKind.TreatPrisoners,
            ["善待俘虏"] = SiegeCastleActionKind.TreatPrisoners,
            ["CASTLE_RECEIVE_ARMAMENTS"] = SiegeCastleActionKind.ReceiveArmaments,
            ["SIEGE_CASTLE_RECEIVE_ARMAMENTS"] = SiegeCastleActionKind.ReceiveArmaments,
            ["城堡接收军械"] = SiegeCastleActionKind.ReceiveArmaments,
            ["接收军械"] = SiegeCastleActionKind.ReceiveArmaments,
            ["CASTLE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["SIEGE_CASTLE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["城堡屠戮战俘"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["CASTLE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ReleasePrisoners,
            ["SIEGE_CASTLE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ReleasePrisoners,
            ["城堡释放战俘"] = SiegeCastleActionKind.ReleasePrisoners,
            ["CASTLE_SELL_PRISONERS"] = SiegeCastleActionKind.SellPrisoners,
            ["SIEGE_CASTLE_SELL_PRISONERS"] = SiegeCastleActionKind.SellPrisoners,
            ["城堡贩卖战俘"] = SiegeCastleActionKind.SellPrisoners,
            ["CASTLE_RECRUIT_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.RecruitPrisonersVoluntary,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.RecruitPrisonersVoluntary,
            ["城堡自愿收编战俘"] = SiegeCastleActionKind.RecruitPrisonersVoluntary,
            ["CASTLE_RECRUIT_PRISONERS_FORCED"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS_FORCED"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["城堡强制收编战俘"] = SiegeCastleActionKind.RecruitPrisonersForced,
            // Historical ambiguous recruit input is treated conservatively as forced.
            ["CASTLE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["城堡收编战俘"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["CASTLE_LABOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.LaborPrisonersVoluntary,
            ["SIEGE_CASTLE_LABOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.LaborPrisonersVoluntary,
            ["城堡自愿劳役服刑"] = SiegeCastleActionKind.LaborPrisonersVoluntary,
            ["CASTLE_LABOR_PRISONERS_FORCED"] = SiegeCastleActionKind.LaborPrisonersForced,
            ["SIEGE_CASTLE_LABOR_PRISONERS_FORCED"] = SiegeCastleActionKind.LaborPrisonersForced,
            ["城堡强制劳役服刑"] = SiegeCastleActionKind.LaborPrisonersForced,
            ["CASTLE_INSTRUCTOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.InstructorPrisonersVoluntary,
            ["SIEGE_CASTLE_INSTRUCTOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.InstructorPrisonersVoluntary,
            ["城堡自愿充当教官"] = SiegeCastleActionKind.InstructorPrisonersVoluntary,
            ["CASTLE_INSTRUCTOR_PRISONERS_FORCED"] = SiegeCastleActionKind.InstructorPrisonersForced,
            ["SIEGE_CASTLE_INSTRUCTOR_PRISONERS_FORCED"] = SiegeCastleActionKind.InstructorPrisonersForced,
            ["城堡强制充当教官"] = SiegeCastleActionKind.InstructorPrisonersForced,
            ["CASTLE_APPEASE_SOLDIERS"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["SIEGE_CASTLE_APPEASE_SOLDIERS"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["城堡安兵"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["城堡安抚随军士兵"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["CASTLE_RECRUIT_LORD"] = SiegeCastleActionKind.RecruitLord,
            ["SIEGE_CASTLE_RECRUIT_LORD"] = SiegeCastleActionKind.RecruitLord,
            ["城堡收编领主"] = SiegeCastleActionKind.RecruitLord,
            ["收编领主"] = SiegeCastleActionKind.RecruitLord,
            ["CASTLE_EXECUTE_LORD"] = SiegeCastleActionKind.ExecuteLord,
            ["SIEGE_CASTLE_EXECUTE_LORD"] = SiegeCastleActionKind.ExecuteLord,
            ["城堡处决领主"] = SiegeCastleActionKind.ExecuteLord,
            ["处决领主"] = SiegeCastleActionKind.ExecuteLord
        };

    private static readonly SiegeCastleActionKind[] CanonicalOrder =
    {
        SiegeCastleActionKind.ProposeRecruitPrisoners,
        SiegeCastleActionKind.ProposeSlaughterPrisoners,
        SiegeCastleActionKind.ProposeReleasePrisoners,
        SiegeCastleActionKind.ProposeSellPrisoners,
        SiegeCastleActionKind.ProposeLaborPrisoners,
        SiegeCastleActionKind.ProposeInstructorPrisoners,
        SiegeCastleActionKind.TreatPrisoners,
        SiegeCastleActionKind.ReceiveArmaments,
        SiegeCastleActionKind.ReleasePrisoners,
        SiegeCastleActionKind.SellPrisoners,
        SiegeCastleActionKind.RecruitPrisonersVoluntary,
        SiegeCastleActionKind.RecruitPrisonersForced,
        SiegeCastleActionKind.LaborPrisonersVoluntary,
        SiegeCastleActionKind.LaborPrisonersForced,
        SiegeCastleActionKind.InstructorPrisonersVoluntary,
        SiegeCastleActionKind.InstructorPrisonersForced,
        SiegeCastleActionKind.SlaughterPrisoners,
        SiegeCastleActionKind.AppeaseSoldiers,
        SiegeCastleActionKind.RecruitLord,
        SiegeCastleActionKind.ExecuteLord
    };

    private static readonly IReadOnlyDictionary<SiegeCastleActionKind, string> KindToCanonicalTag =
        new Dictionary<SiegeCastleActionKind, string>
        {
            [SiegeCastleActionKind.ProposeRecruitPrisoners] = ProposeRecruitPrisonersTag,
            [SiegeCastleActionKind.ProposeSlaughterPrisoners] = ProposeSlaughterPrisonersTag,
            [SiegeCastleActionKind.ProposeReleasePrisoners] = ProposeReleasePrisonersTag,
            [SiegeCastleActionKind.ProposeSellPrisoners] = ProposeSellPrisonersTag,
            [SiegeCastleActionKind.ProposeLaborPrisoners] = ProposeLaborPrisonersTag,
            [SiegeCastleActionKind.ProposeInstructorPrisoners] = ProposeInstructorPrisonersTag,
            [SiegeCastleActionKind.TreatPrisoners] = TreatPrisonersTag,
            [SiegeCastleActionKind.ReceiveArmaments] = ReceiveArmamentsTag,
            [SiegeCastleActionKind.SlaughterPrisoners] = SlaughterPrisonersTag,
            [SiegeCastleActionKind.ReleasePrisoners] = ReleasePrisonersTag,
            [SiegeCastleActionKind.SellPrisoners] = SellPrisonersTag,
            [SiegeCastleActionKind.RecruitPrisonersVoluntary] = RecruitPrisonersVoluntaryTag,
            [SiegeCastleActionKind.RecruitPrisonersForced] = RecruitPrisonersForcedTag,
            [SiegeCastleActionKind.LaborPrisonersVoluntary] = LaborPrisonersVoluntaryTag,
            [SiegeCastleActionKind.LaborPrisonersForced] = LaborPrisonersForcedTag,
            [SiegeCastleActionKind.InstructorPrisonersVoluntary] = InstructorPrisonersVoluntaryTag,
            [SiegeCastleActionKind.InstructorPrisonersForced] = InstructorPrisonersForcedTag,
            [SiegeCastleActionKind.AppeaseSoldiers] = AppeaseSoldiersTag,
            [SiegeCastleActionKind.RecruitLord] = RecruitLordTag,
            [SiegeCastleActionKind.ExecuteLord] = ExecuteLordTag
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

    public static IReadOnlyList<SiegeCastleActionKind> GetCanonicalOrder() => CanonicalOrder;

    public static IReadOnlyList<string> GetAliases(SiegeCastleActionKind kind)
    {
        var aliases = new List<string>();
        if (TryGetCanonicalTag(kind, out string canonicalTag))
        {
            aliases.Add(canonicalTag);
        }
        foreach (KeyValuePair<string, SiegeCastleActionKind> pair in NameToKind)
        {
            string tag = "[ACTION:" + pair.Key + "]";
            if (pair.Value == kind && !aliases.Contains(tag))
            {
                aliases.Add(tag);
            }
        }
        return aliases;
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
            if (TryParseName(match.Groups["name"].Value, out SiegeCastleActionKind kind) && seen.Add(kind))
            {
                result.Add(kind);
            }
        }
        return result;
    }

    public static IReadOnlyList<string> NormalizeToCanonicalTags(string text)
    {
        IReadOnlyList<SiegeCastleActionKind> kinds = ExtractKinds(text);
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
