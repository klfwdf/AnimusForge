using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

public enum VillageDispositionActionKind
{
    ForcedRansom, ForcedFood, ForcedGoods, ForcedLivestock,
    Plunder, StopPlunder, Enslave, ForcedRecruitment, MassacreAndSeize, Destroy,
    Mercy, VolunteerFollowers, VoluntaryRansom, VoluntaryFood, VoluntaryGoods, VoluntaryLivestock,
    MassacreConcealment, ConsentedConcealment,
    GatherVillagers, VillagerApproval,
}

public sealed class VillageDispositionActionSpec
{
    internal VillageDispositionActionSpec(VillageDispositionActionKind kind, string name,
        int outcomeLevel, int ownerPenalty, int notablePenalty, bool requiresNotable = false)
    {
        Kind = kind;
        Name = name;
        OutcomeLevel = outcomeLevel;
        OwnerPenalty = ownerPenalty;
        NotablePenalty = notablePenalty;
        RequiresNotable = requiresNotable;
    }

    public VillageDispositionActionKind Kind { get; }
    public string Name { get; }
    public string Tag => "[VILLAGE_DISPOSE:" + Name + "]";
    // Negative numbers denote confirmed negative levels; positive reward amounts remain undefined.
    public int OutcomeLevel { get; }
    public int OwnerPenalty { get; }
    public int NotablePenalty { get; }
    public int NotableReward => OutcomeLevel > 0 ? 10 : 0;
    public bool RequiresNotable { get; }
    public bool HasDefinedEffect => Kind != VillageDispositionActionKind.MassacreConcealment && Kind != VillageDispositionActionKind.ConsentedConcealment;
}

/// <summary>Independent foreign-village protocol. Pending effects are never advertised as executable.</summary>
public static class VillageDispositionActionCatalog
{
    public const int FollowerLimit = 12;
    public const string MercyTag = "[VILLAGE_DISPOSE:宽恕]";
    private static readonly VillageDispositionActionSpec[] Specs =
    {
        new(VillageDispositionActionKind.ForcedRansom, "强征赎金", -1, -10, -30),
        new(VillageDispositionActionKind.ForcedFood, "强征粮食", -1, -10, -30),
        new(VillageDispositionActionKind.ForcedGoods, "强征物资", -1, -10, -30),
        new(VillageDispositionActionKind.ForcedLivestock, "强征牲畜", -1, -10, -30),
        new(VillageDispositionActionKind.Plunder, "掠夺村庄", -2, -15, -50),
        new(VillageDispositionActionKind.StopPlunder, "停止掠夺", 0, 0, 0),
        new(VillageDispositionActionKind.Enslave, "抓取奴隶", -2, -15, -50),
        new(VillageDispositionActionKind.ForcedRecruitment, "强行征兵", -1, -10, -30),
        new(VillageDispositionActionKind.MassacreAndSeize, "屠村", -2, -15, -50),
        new(VillageDispositionActionKind.Destroy, "纯粹毁坏", -3, -25, -100),
        new(VillageDispositionActionKind.Mercy, "宽恕", 0, 0, 0),
        new(VillageDispositionActionKind.VolunteerFollowers, "村民自愿入伍", 1, 0, 0),
        new(VillageDispositionActionKind.VoluntaryRansom, "自愿提供金钱", 1, 0, 0),
        new(VillageDispositionActionKind.VoluntaryFood, "自愿给予粮食", 1, 0, 0),
        new(VillageDispositionActionKind.VoluntaryGoods, "自愿给予物资", 1, 0, 0),
        new(VillageDispositionActionKind.VoluntaryLivestock, "自愿给予牲畜", 1, 0, 0),
        new(VillageDispositionActionKind.MassacreConcealment, "屠村藏匿", 0, 0, 0),
        new(VillageDispositionActionKind.ConsentedConcealment, "同意藏匿", 0, 0, 0, requiresNotable: true),
        new(VillageDispositionActionKind.GatherVillagers, "召集村民", 0, 0, 0),
        new(VillageDispositionActionKind.VillagerApproval, "认可游说", 0, 0, 0),
    };

    private static readonly Regex ActionRegex = new Regex(
        @"\[VILLAGE_DISPOSE:(?<name>" + string.Join("|", Specs.Select(spec => Regex.Escape(spec.Name))) + @")\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AnyTagRegex = new Regex(@"\[VILLAGE_DISPOSE:[^\]\r\n]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly IReadOnlyDictionary<string, VillageDispositionActionSpec> ByName =
        Specs.ToDictionary(spec => spec.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<VillageDispositionActionSpec> All => Specs;

    public static IReadOnlyList<VillageDispositionActionSpec> Extract(string text)
    {
        var result = new List<VillageDispositionActionSpec>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        var seen = new HashSet<VillageDispositionActionKind>();
        foreach (Match match in ActionRegex.Matches(text))
        {
            VillageDispositionActionSpec spec = ByName[match.Groups["name"].Value];
            if (seen.Add(spec.Kind)) result.Add(spec);
        }
        return result;
    }

    public static string RemoveTags(string text) => string.IsNullOrWhiteSpace(text)
        ? string.Empty : AnyTagRegex.Replace(text, string.Empty).Trim();

    public static bool CanEnter(bool isVillage, bool isNormal, bool underBattleOrRaid,
        bool sameFaction, bool admittedAfterVictory)
    {
        return isVillage && isNormal && !underBattleOrRaid && !sameFaction && admittedAfterVictory;
    }

}
