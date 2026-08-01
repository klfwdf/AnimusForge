using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

public static class VillageAftermathActionTagCatalog
{
    public const string GatherEldersTag = "[VILLAGE_ACTION:召集长老]";
    public const string RestrainTroopsTag = "[VILLAGE_ACTION:约束军纪]";
    public const string PacifyTag = "[VILLAGE_ACTION:平息]";
    public const string ReliefTag = "[VILLAGE_ACTION:赈济]";
    public const string FineTag = "[VILLAGE_ACTION:罚赎]";
    public const string RequisitionFoodTag = "[VILLAGE_ACTION:征粮]";
    public const string RequisitionProduceTag = "[VILLAGE_ACTION:征收物产]";
    public const string RequisitionLivestockTag = "[VILLAGE_ACTION:征收牲畜]";
    public const string LevyRecruitsTag = "[VILLAGE_ACTION:征丁]";
    public const string PunishRingleaderTag = "[VILLAGE_ACTION:惩办首恶]";
    public const string ConfiscatePropertyTag = "[VILLAGE_ACTION:查抄村产]";
    public const string DestroyLivelihoodTag = "[VILLAGE_ACTION:毁坏生计]";
    public const string MassacreTag = "[VILLAGE_ACTION:屠村]";
    public const string CulturalReformTag = "[VILLAGE_ACTION:文化改造]";

    public const string AnyActionTagPattern = @"\[VILLAGE_ACTION:(?<name>召集长老|约束军纪|平息|赈济|罚赎|征粮|征收物产|征收牲畜|征丁|惩办首恶|查抄村产|毁坏生计|屠村|文化改造)\]";

    private static readonly Regex ActionRegex = new Regex(AnyActionTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, VillageAftermathActionKind> NameToKind =
        new Dictionary<string, VillageAftermathActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["召集长老"] = VillageAftermathActionKind.GatherElders,
            ["约束军纪"] = VillageAftermathActionKind.RestrainTroops,
            ["平息"] = VillageAftermathActionKind.Pacify,
            ["赈济"] = VillageAftermathActionKind.Relief,
            ["罚赎"] = VillageAftermathActionKind.Fine,
            ["征粮"] = VillageAftermathActionKind.RequisitionFood,
            ["征收物产"] = VillageAftermathActionKind.RequisitionProduce,
            ["征收牲畜"] = VillageAftermathActionKind.RequisitionLivestock,
            ["征丁"] = VillageAftermathActionKind.LevyRecruits,
            ["惩办首恶"] = VillageAftermathActionKind.PunishRingleader,
            ["查抄村产"] = VillageAftermathActionKind.ConfiscateProperty,
            ["毁坏生计"] = VillageAftermathActionKind.DestroyLivelihood,
            ["屠村"] = VillageAftermathActionKind.Massacre,
            ["文化改造"] = VillageAftermathActionKind.CulturalReform,
        };

    private static readonly IReadOnlyDictionary<VillageAftermathActionKind, string> KindToTag =
        new Dictionary<VillageAftermathActionKind, string>
        {
            [VillageAftermathActionKind.GatherElders] = GatherEldersTag,
            [VillageAftermathActionKind.RestrainTroops] = RestrainTroopsTag,
            [VillageAftermathActionKind.Pacify] = PacifyTag,
            [VillageAftermathActionKind.Relief] = ReliefTag,
            [VillageAftermathActionKind.Fine] = FineTag,
            [VillageAftermathActionKind.RequisitionFood] = RequisitionFoodTag,
            [VillageAftermathActionKind.RequisitionProduce] = RequisitionProduceTag,
            [VillageAftermathActionKind.RequisitionLivestock] = RequisitionLivestockTag,
            [VillageAftermathActionKind.LevyRecruits] = LevyRecruitsTag,
            [VillageAftermathActionKind.PunishRingleader] = PunishRingleaderTag,
            [VillageAftermathActionKind.ConfiscateProperty] = ConfiscatePropertyTag,
            [VillageAftermathActionKind.DestroyLivelihood] = DestroyLivelihoodTag,
            [VillageAftermathActionKind.Massacre] = MassacreTag,
            [VillageAftermathActionKind.CulturalReform] = CulturalReformTag,
        };

    private static readonly VillageAftermathActionKind[] CanonicalOrder =
    {
        VillageAftermathActionKind.GatherElders,
        VillageAftermathActionKind.RestrainTroops,
        VillageAftermathActionKind.Pacify,
        VillageAftermathActionKind.Relief,
        VillageAftermathActionKind.Fine,
        VillageAftermathActionKind.RequisitionFood,
        VillageAftermathActionKind.RequisitionProduce,
        VillageAftermathActionKind.RequisitionLivestock,
        VillageAftermathActionKind.LevyRecruits,
        VillageAftermathActionKind.PunishRingleader,
        VillageAftermathActionKind.ConfiscateProperty,
        VillageAftermathActionKind.DestroyLivelihood,
        VillageAftermathActionKind.Massacre,
        VillageAftermathActionKind.CulturalReform,
    };

    public static IReadOnlyList<VillageAftermathActionKind> ExtractKinds(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<VillageAftermathActionKind>();
        }

        var result = new List<VillageAftermathActionKind>();
        var seen = new HashSet<VillageAftermathActionKind>();
        foreach (Match match in ActionRegex.Matches(text))
        {
            if (NameToKind.TryGetValue(match.Groups["name"].Value, out VillageAftermathActionKind kind) && seen.Add(kind))
            {
                result.Add(kind);
            }
        }

        return result;
    }

    public static bool TryGetCanonicalTag(VillageAftermathActionKind kind, out string tag)
    {
        return KindToTag.TryGetValue(kind, out tag);
    }

    public static IReadOnlyList<VillageAftermathActionKind> GetCanonicalOrder()
    {
        return CanonicalOrder;
    }

    public static string RemoveTags(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : ActionRegex.Replace(text, string.Empty).Trim();
    }
}
