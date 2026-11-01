using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace AnimusForge;

// Taunt-owned adapter for an authoritative native sentence/payment/pardon.
// No general crime-decrease hook: natural decay must not be mistaken for payment.
[HarmonyPatch(typeof(PayForCrimeAction), nameof(PayForCrimeAction.Apply))]
internal static class Patch_SceneTaunt_CrimeSettlement
{
    internal static void Postfix(IFaction faction, CrimeModel.PaymentMethod paymentMethod, bool __runOriginal)
    {
        try
        {
            const CrimeModel.PaymentMethod completedMethods = CrimeModel.PaymentMethod.Gold
                | CrimeModel.PaymentMethod.Influence | CrimeModel.PaymentMethod.Punishment
                | CrimeModel.PaymentMethod.Execution | CrimeModel.PaymentMethod.ExMachina;
            var crimeModel = Campaign.Current?.Models?.CrimeModel;
            if (!__runOriginal || faction == null || crimeModel == null
                || (paymentMethod & completedMethods) == 0
                || !(faction.MainHeroCrimeRating <= crimeModel.GetCrimeRatingAfterPunishment() + 0.01f)) return;
            SceneTauntBehavior.ClearDeferredCrimeForExternal(faction, "native_crime_sentence_served");
        }
        catch (Exception ex)
        {
            Logger.Log("SceneTaunt", "Clearing deferred crime after native sentence failed: " + ex.Message);
        }
    }
}
