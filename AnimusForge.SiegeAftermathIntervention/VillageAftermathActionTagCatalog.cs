using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

public static class VillageAftermathActionTagCatalog
{
    public static readonly string GatherEldersTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_a58c06142f99");
    public static readonly string RestrainTroopsTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_74b12ce80e6c");
    public static readonly string PacifyTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_a97f30e3a3b1");
    public static readonly string ReliefTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_dbbda88e5b0d");
    public static readonly string FineTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_0668ff5c4c6b");
    public static readonly string RequisitionFoodTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_b968afab0e0a");
    public static readonly string RequisitionProduceTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_d5357c95d911");
    public static readonly string RequisitionLivestockTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_cb5008d0d4e8");
    public static readonly string LevyRecruitsTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_fdd24f29c48f");
    public static readonly string PunishRingleaderTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_6562809f2e10");
    public static readonly string ConfiscatePropertyTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_896c810ea0a9");
    public static readonly string DestroyLivelihoodTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_a66666f702e7");
    public static readonly string MassacreTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_d633546da871");
    public static readonly string CulturalReformTag = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_ef43f0e48a1f");

    public static readonly string AnyActionTagPattern = SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_103951f75bf9");

    private static readonly Regex ActionRegex = new Regex(AnyActionTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, VillageAftermathActionKind> NameToKind =
        new Dictionary<string, VillageAftermathActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            [SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_d328854d7a88")] = VillageAftermathActionKind.GatherElders,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_restrain")] = VillageAftermathActionKind.RestrainTroops,
            [SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_a707c61c167a")] = VillageAftermathActionKind.Pacify,
            [SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_f090d14830f0")] = VillageAftermathActionKind.Relief,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_fine")] = VillageAftermathActionKind.Fine,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_food")] = VillageAftermathActionKind.RequisitionFood,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_produce")] = VillageAftermathActionKind.RequisitionProduce,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_livestock")] = VillageAftermathActionKind.RequisitionLivestock,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_recruits")] = VillageAftermathActionKind.LevyRecruits,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_ringleader")] = VillageAftermathActionKind.PunishRingleader,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_property")] = VillageAftermathActionKind.ConfiscateProperty,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_livelihood")] = VillageAftermathActionKind.DestroyLivelihood,
            [SiegeAftermathCompatibilityTexts.Raw("village_action_massacre")] = VillageAftermathActionKind.Massacre,
            [SiegeAftermathCompatibilityTexts.Raw("compat_village_aftermath_action_tag_catalog_e2e515f8ff94")] = VillageAftermathActionKind.CulturalReform,
            ["GATHER_ELDERS"] = VillageAftermathActionKind.GatherElders,
            ["RESTRAIN_TROOPS"] = VillageAftermathActionKind.RestrainTroops,
            ["PACIFY"] = VillageAftermathActionKind.Pacify,
            ["RELIEF"] = VillageAftermathActionKind.Relief,
            ["FINE"] = VillageAftermathActionKind.Fine,
            ["REQUISITION_FOOD"] = VillageAftermathActionKind.RequisitionFood,
            ["REQUISITION_PRODUCE"] = VillageAftermathActionKind.RequisitionProduce,
            ["REQUISITION_LIVESTOCK"] = VillageAftermathActionKind.RequisitionLivestock,
            ["LEVY_RECRUITS"] = VillageAftermathActionKind.LevyRecruits,
            ["PUNISH_RINGLEADER"] = VillageAftermathActionKind.PunishRingleader,
            ["CONFISCATE_PROPERTY"] = VillageAftermathActionKind.ConfiscateProperty,
            ["DESTROY_LIVELIHOOD"] = VillageAftermathActionKind.DestroyLivelihood,
            ["MASSACRE"] = VillageAftermathActionKind.Massacre,
            ["CULTURAL_REFORM"] = VillageAftermathActionKind.CulturalReform,
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
