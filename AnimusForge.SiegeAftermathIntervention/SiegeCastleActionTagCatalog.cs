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
    public const string ProposeRepairCastleLaborTag = "[ACTION:CASTLE_PROPOSE_REPAIR_CASTLE_LABOR]";
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
    public const string RepairCastleLaborVoluntaryTag = "[ACTION:CASTLE_REPAIR_CASTLE_LABOR_VOLUNTARY]";
    public const string RepairCastleLaborForcedTag = "[ACTION:CASTLE_REPAIR_CASTLE_LABOR_FORCED]";
    public const string InstructorPrisonersVoluntaryTag = "[ACTION:CASTLE_INSTRUCTOR_PRISONERS_VOLUNTARY]";
    public const string InstructorPrisonersForcedTag = "[ACTION:CASTLE_INSTRUCTOR_PRISONERS_FORCED]";
    public const string AppeaseSoldiersTag = "[ACTION:CASTLE_APPEASE_SOLDIERS]";
    public const string RecruitLordTag = "[ACTION:CASTLE_RECRUIT_LORD]";
    public const string SellLordTag = "[ACTION:CASTLE_SELL_LORD]";
    public const string ExecuteLordTag = "[ACTION:CASTLE_EXECUTE_LORD]";
    public const string DuelLordTag = "[ACTION:CASTLE_DUEL_LORD]";
    public const string SoldierDiscontentTag = "[ACTION:CASTLE_SOLDIER_DISCONTENT]";

    // Broad enough to strip stale castle-prefixed output, but TryParseName remains authoritative.
    public static readonly string AnyActionTagPattern = CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_6cdba8113fdf");

    private static readonly Regex ActionTagRegex = new Regex(
        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_c6542806b744"),
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, SiegeCastleActionKind> NameToKind =
        new Dictionary<string, SiegeCastleActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["CASTLE_PROPOSE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.ProposeRecruitPrisoners,
            ["SIEGE_CASTLE_PROPOSE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.ProposeRecruitPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_3ad9b185333e")] = SiegeCastleActionKind.ProposeRecruitPrisoners,
            ["CASTLE_PROPOSE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.ProposeSlaughterPrisoners,
            ["SIEGE_CASTLE_PROPOSE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.ProposeSlaughterPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_b0e06f007c32")] = SiegeCastleActionKind.ProposeSlaughterPrisoners,
            ["CASTLE_PROPOSE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ProposeReleasePrisoners,
            ["SIEGE_CASTLE_PROPOSE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ProposeReleasePrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_e1e8d0ec321c")] = SiegeCastleActionKind.ProposeReleasePrisoners,
            ["CASTLE_PROPOSE_SELL_PRISONERS"] = SiegeCastleActionKind.ProposeSellPrisoners,
            ["SIEGE_CASTLE_PROPOSE_SELL_PRISONERS"] = SiegeCastleActionKind.ProposeSellPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_c4d4879ce6a6")] = SiegeCastleActionKind.ProposeSellPrisoners,
            ["CASTLE_PROPOSE_LABOR_PRISONERS"] = SiegeCastleActionKind.ProposeLaborPrisoners,
            ["SIEGE_CASTLE_PROPOSE_LABOR_PRISONERS"] = SiegeCastleActionKind.ProposeLaborPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_f0e138755c13")] = SiegeCastleActionKind.ProposeLaborPrisoners,
            ["CASTLE_PROPOSE_REPAIR_CASTLE_LABOR"] = SiegeCastleActionKind.ProposeRepairCastleLabor,
            ["SIEGE_CASTLE_PROPOSE_REPAIR_CASTLE_LABOR"] = SiegeCastleActionKind.ProposeRepairCastleLabor,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_eed8ac428039")] = SiegeCastleActionKind.ProposeRepairCastleLabor,
            ["CASTLE_PROPOSE_INSTRUCTOR_PRISONERS"] = SiegeCastleActionKind.ProposeInstructorPrisoners,
            ["SIEGE_CASTLE_PROPOSE_INSTRUCTOR_PRISONERS"] = SiegeCastleActionKind.ProposeInstructorPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_63f16f094ef4")] = SiegeCastleActionKind.ProposeInstructorPrisoners,
            ["CASTLE_TREAT_PRISONERS"] = SiegeCastleActionKind.TreatPrisoners,
            ["SIEGE_CASTLE_TREAT_PRISONERS"] = SiegeCastleActionKind.TreatPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_41d84abbc250")] = SiegeCastleActionKind.TreatPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("effect_treat")] = SiegeCastleActionKind.TreatPrisoners,
            ["CASTLE_RECEIVE_ARMAMENTS"] = SiegeCastleActionKind.ReceiveArmaments,
            ["SIEGE_CASTLE_RECEIVE_ARMAMENTS"] = SiegeCastleActionKind.ReceiveArmaments,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_28520058eb74")] = SiegeCastleActionKind.ReceiveArmaments,
            [CastleAftermathCompatibilityTexts.Raw("effect_receive_armaments")] = SiegeCastleActionKind.ReceiveArmaments,
            ["CASTLE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.SlaughterPrisoners,
            ["SIEGE_CASTLE_SLAUGHTER_PRISONERS"] = SiegeCastleActionKind.SlaughterPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_9229735dd522")] = SiegeCastleActionKind.SlaughterPrisoners,
            ["CASTLE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ReleasePrisoners,
            ["SIEGE_CASTLE_RELEASE_PRISONERS"] = SiegeCastleActionKind.ReleasePrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_d8081144d3fe")] = SiegeCastleActionKind.ReleasePrisoners,
            ["CASTLE_SELL_PRISONERS"] = SiegeCastleActionKind.SellPrisoners,
            ["SIEGE_CASTLE_SELL_PRISONERS"] = SiegeCastleActionKind.SellPrisoners,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_7c3e3e39b338")] = SiegeCastleActionKind.SellPrisoners,
            ["CASTLE_RECRUIT_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.RecruitPrisonersVoluntary,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.RecruitPrisonersVoluntary,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_99feca36d1bd")] = SiegeCastleActionKind.RecruitPrisonersVoluntary,
            ["CASTLE_RECRUIT_PRISONERS_FORCED"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS_FORCED"] = SiegeCastleActionKind.RecruitPrisonersForced,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_ac1c8d09a775")] = SiegeCastleActionKind.RecruitPrisonersForced,
            // Historical ambiguous recruit input is treated conservatively as forced.
            ["CASTLE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["SIEGE_CASTLE_RECRUIT_PRISONERS"] = SiegeCastleActionKind.RecruitPrisonersForced,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_304e89d8bd65")] = SiegeCastleActionKind.RecruitPrisonersForced,
            ["CASTLE_LABOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.LaborPrisonersVoluntary,
            ["SIEGE_CASTLE_LABOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.LaborPrisonersVoluntary,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_f93211770df5")] = SiegeCastleActionKind.LaborPrisonersVoluntary,
            ["CASTLE_LABOR_PRISONERS_FORCED"] = SiegeCastleActionKind.LaborPrisonersForced,
            ["SIEGE_CASTLE_LABOR_PRISONERS_FORCED"] = SiegeCastleActionKind.LaborPrisonersForced,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_1ab90d3b7711")] = SiegeCastleActionKind.LaborPrisonersForced,
            ["CASTLE_REPAIR_CASTLE_LABOR_VOLUNTARY"] = SiegeCastleActionKind.RepairCastleLaborVoluntary,
            ["SIEGE_CASTLE_REPAIR_CASTLE_LABOR_VOLUNTARY"] = SiegeCastleActionKind.RepairCastleLaborVoluntary,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_88a5a926d1c7")] = SiegeCastleActionKind.RepairCastleLaborVoluntary,
            ["CASTLE_REPAIR_CASTLE_LABOR_FORCED"] = SiegeCastleActionKind.RepairCastleLaborForced,
            ["SIEGE_CASTLE_REPAIR_CASTLE_LABOR_FORCED"] = SiegeCastleActionKind.RepairCastleLaborForced,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_40ebb762faaa")] = SiegeCastleActionKind.RepairCastleLaborForced,
            ["CASTLE_INSTRUCTOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.InstructorPrisonersVoluntary,
            ["SIEGE_CASTLE_INSTRUCTOR_PRISONERS_VOLUNTARY"] = SiegeCastleActionKind.InstructorPrisonersVoluntary,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_bb67d8b038ef")] = SiegeCastleActionKind.InstructorPrisonersVoluntary,
            ["CASTLE_INSTRUCTOR_PRISONERS_FORCED"] = SiegeCastleActionKind.InstructorPrisonersForced,
            ["SIEGE_CASTLE_INSTRUCTOR_PRISONERS_FORCED"] = SiegeCastleActionKind.InstructorPrisonersForced,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_b897111ce69e")] = SiegeCastleActionKind.InstructorPrisonersForced,
            ["CASTLE_APPEASE_SOLDIERS"] = SiegeCastleActionKind.AppeaseSoldiers,
            ["SIEGE_CASTLE_APPEASE_SOLDIERS"] = SiegeCastleActionKind.AppeaseSoldiers,
            [CastleAftermathCompatibilityTexts.Raw("soldier_reaction_appease_memory")] = SiegeCastleActionKind.AppeaseSoldiers,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_349edb4ea9d3")] = SiegeCastleActionKind.AppeaseSoldiers,
            ["CASTLE_RECRUIT_LORD"] = SiegeCastleActionKind.RecruitLord,
            ["SIEGE_CASTLE_RECRUIT_LORD"] = SiegeCastleActionKind.RecruitLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_03def35d56d1")] = SiegeCastleActionKind.RecruitLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_a9fbf3d0d8a2")] = SiegeCastleActionKind.RecruitLord,
            ["CASTLE_SELL_LORD"] = SiegeCastleActionKind.SellLord,
            ["SIEGE_CASTLE_SELL_LORD"] = SiegeCastleActionKind.SellLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_5c807924ef58")] = SiegeCastleActionKind.SellLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_cf405b416406")] = SiegeCastleActionKind.SellLord,
            ["CASTLE_EXECUTE_LORD"] = SiegeCastleActionKind.ExecuteLord,
            ["SIEGE_CASTLE_EXECUTE_LORD"] = SiegeCastleActionKind.ExecuteLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_8263bb90a152")] = SiegeCastleActionKind.ExecuteLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_3d13b4e9dcf0")] = SiegeCastleActionKind.ExecuteLord,
            ["CASTLE_DUEL_LORD"] = SiegeCastleActionKind.DuelLord,
            ["SIEGE_CASTLE_DUEL_LORD"] = SiegeCastleActionKind.DuelLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_01eeb07aae69")] = SiegeCastleActionKind.DuelLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_d91a966d80d6")] = SiegeCastleActionKind.DuelLord,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_817b3596d279")] = SiegeCastleActionKind.DuelLord,
            ["CASTLE_SOLDIER_DISCONTENT"] = SiegeCastleActionKind.SoldierDiscontent,
            ["SIEGE_CASTLE_SOLDIER_DISCONTENT"] = SiegeCastleActionKind.SoldierDiscontent,
            [CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_action_tag_catalog_f964c9b9687f")] = SiegeCastleActionKind.SoldierDiscontent
        };

    private static readonly SiegeCastleActionKind[] CanonicalOrder =
    {
        SiegeCastleActionKind.ProposeRecruitPrisoners,
        SiegeCastleActionKind.ProposeSlaughterPrisoners,
        SiegeCastleActionKind.ProposeReleasePrisoners,
        SiegeCastleActionKind.ProposeSellPrisoners,
        SiegeCastleActionKind.ProposeLaborPrisoners,
        SiegeCastleActionKind.ProposeRepairCastleLabor,
        SiegeCastleActionKind.ProposeInstructorPrisoners,
        SiegeCastleActionKind.TreatPrisoners,
        SiegeCastleActionKind.ReceiveArmaments,
        SiegeCastleActionKind.ReleasePrisoners,
        SiegeCastleActionKind.SellPrisoners,
        SiegeCastleActionKind.RecruitPrisonersVoluntary,
        SiegeCastleActionKind.RecruitPrisonersForced,
        SiegeCastleActionKind.LaborPrisonersVoluntary,
        SiegeCastleActionKind.LaborPrisonersForced,
        SiegeCastleActionKind.RepairCastleLaborVoluntary,
        SiegeCastleActionKind.RepairCastleLaborForced,
        SiegeCastleActionKind.InstructorPrisonersVoluntary,
        SiegeCastleActionKind.InstructorPrisonersForced,
        SiegeCastleActionKind.SlaughterPrisoners,
        SiegeCastleActionKind.AppeaseSoldiers,
        SiegeCastleActionKind.RecruitLord,
        SiegeCastleActionKind.SellLord,
        SiegeCastleActionKind.DuelLord,
        SiegeCastleActionKind.ExecuteLord,
        SiegeCastleActionKind.SoldierDiscontent
    };

    private static readonly IReadOnlyDictionary<SiegeCastleActionKind, string> KindToCanonicalTag =
        new Dictionary<SiegeCastleActionKind, string>
        {
            [SiegeCastleActionKind.ProposeRecruitPrisoners] = ProposeRecruitPrisonersTag,
            [SiegeCastleActionKind.ProposeSlaughterPrisoners] = ProposeSlaughterPrisonersTag,
            [SiegeCastleActionKind.ProposeReleasePrisoners] = ProposeReleasePrisonersTag,
            [SiegeCastleActionKind.ProposeSellPrisoners] = ProposeSellPrisonersTag,
            [SiegeCastleActionKind.ProposeLaborPrisoners] = ProposeLaborPrisonersTag,
            [SiegeCastleActionKind.ProposeRepairCastleLabor] = ProposeRepairCastleLaborTag,
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
            [SiegeCastleActionKind.RepairCastleLaborVoluntary] = RepairCastleLaborVoluntaryTag,
            [SiegeCastleActionKind.RepairCastleLaborForced] = RepairCastleLaborForcedTag,
            [SiegeCastleActionKind.InstructorPrisonersVoluntary] = InstructorPrisonersVoluntaryTag,
            [SiegeCastleActionKind.InstructorPrisonersForced] = InstructorPrisonersForcedTag,
            [SiegeCastleActionKind.AppeaseSoldiers] = AppeaseSoldiersTag,
            [SiegeCastleActionKind.RecruitLord] = RecruitLordTag,
            [SiegeCastleActionKind.SellLord] = SellLordTag,
            [SiegeCastleActionKind.DuelLord] = DuelLordTag,
            [SiegeCastleActionKind.ExecuteLord] = ExecuteLordTag,
            [SiegeCastleActionKind.SoldierDiscontent] = SoldierDiscontentTag
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
