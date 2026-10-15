using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private static readonly HashSet<MethodBase> GrowthHooks = new HashSet<MethodBase>();
    private void RegisterVillageGrowthHooks(CampaignGameStarter starter)
    {
        var models = Campaign.Current?.Models;
        if (models == null) return;
        var harmony = new Harmony("AnimusForge.gccz.village.growth");
        void Patch(object model, string name, Type[] signature, string callback)
        {
            MethodInfo method = AccessTools.Method(model.GetType(), name, signature);
            if (method == null || GrowthHooks.Contains(method)) return;
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(VillageAftermathBehavior), callback) { priority = Priority.Last });
            GrowthHooks.Add(method);
        }
        Patch(models.VolunteerModel, "GetDailyVolunteerProductionProbability", new[] { typeof(Hero), typeof(int), typeof(Settlement) }, nameof(VillageVolunteerPostfix));
        Patch(models.VillageProductionCalculatorModel, "CalculateDailyProductionAmount", new[] { typeof(Village), typeof(ItemObject) }, nameof(VillageProductionPostfix));
        Patch(models.VillageProductionCalculatorModel, "CalculateDailyFoodProductionAmount", new[] { typeof(Village) }, nameof(VillageFoodProductionPostfix));
        Patch(models.SettlementProsperityModel, "CalculateHearthChange", new[] { typeof(Village), typeof(bool) }, nameof(VillageHearthPostfix));
    }
    private static float PurgeMultiplier(Settlement village) => village?.IsVillage == true && EconomyOwner?._purgeUntil.TryGetValue(village.StringId, out int until) == true
        && CurrentDay < until ? 0.2f : 1f;
    private static void VillageVolunteerPostfix(Settlement settlement, ref float __result)
    {
        if (settlement?.IsVillage != true) return;
        float aid = EconomyOwner?._aidMultiplier.TryGetValue(settlement.StringId, out float value) == true ? value : 1f;
        // The native call gates both empty-slot generation and occupied-slot upgrading.
        __result = Math.Max(0f, Math.Min(1f, __result * aid * PurgeMultiplier(settlement)));
    }
    private static void VillageProductionPostfix(Village village, ref ExplainedNumber __result)
    {
        float factor = PurgeMultiplier(village?.Settlement);
        if (factor != 1f && __result.ResultNumber > 0f) __result.Add(__result.ResultNumber * (factor - 1f));
    }
    private static void VillageHearthPostfix(Village village, ref ExplainedNumber __result)
        => VillageProductionPostfix(village, ref __result);
    private static void VillageFoodProductionPostfix(Village village, ref float __result)
    {
        if (__result > 0f) __result *= PurgeMultiplier(village?.Settlement);
    }
}
