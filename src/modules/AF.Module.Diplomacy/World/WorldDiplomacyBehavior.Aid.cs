using System;
using System.Globalization;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private static bool AidTakeoverActive => IsWorldDiplomacyEnabled() && !VassalageBehavior.IsApplyingVassalageDiplomacy;
    private static int _aidUnavailableLoggedHour = int.MinValue;
    private static bool CaptureNativeAid(Kingdom calling, Kingdom called, Kingdom enemy, string source,
        bool accepted = false, int legacyCost = 0, bool playerPaying = false)
    {
        if (!AidTakeoverActive) return false;
        var owner = ResolveInstance();
        if (owner == null || !owner.IsPersistenceAvailable)
        {
            int hour = CurrentHour();
            if (_aidUnavailableLoggedHour != hour)
            { _aidUnavailableLoggedHour = hour; Log("aid_takeover_blocked:persistence_unavailable; native action suppressed"); }
            return true;
        }
        if (calling == null || called == null || enemy == null) return true;
        var stance = calling.GetStanceWith(enemy);
        string warIdentity = stance?.WarStartDate.ToHours.ToString("R", CultureInfo.InvariantCulture) ?? "";
        owner._orchestration.CaptureAidRequest(calling.StringId, called.StringId, enemy.StringId, warIdentity,
            source, accepted, legacyCost, playerPaying);
        return true;
    }
    private static bool CaptureNativeAidDecision(Kingdom host, KingdomDecision decision)
    {
        if (!AidTakeoverActive) return false;
        // Sanitize catches this diagnostic and retains the native queue item.
        // Direct UI/action prefixes above suppress safely without throwing into UI.
        if (decision is ProposeCallToWarAgreementDecision or AcceptCallToWarAgreementDecision)
        {
            var owner = ResolveInstance();
            if (owner == null || !owner.IsPersistenceAvailable) throw new InvalidOperationException("aid_owner_unavailable");
        }
        if (decision is ProposeCallToWarAgreementDecision propose)
            return CaptureNativeAid(host, propose.CalledKingdom, propose.KingdomToCallToWarAgainst,
                IsPlayerKingdom(host) ? "player_suggestion" : "native_request");
        if (decision is AcceptCallToWarAgreementDecision accept)
            return CaptureNativeAid(accept.CallingKingdom, host, accept.KingdomToCallToWarAgainst, "native_request");
        return false;
    }
    private static bool Patch_Aid_ToPlayer_Prefix(Kingdom __0, Kingdom __1)
        => !CaptureNativeAid(__0, Clan.PlayerClan?.Kingdom, __1, "native_request");
    private static bool Patch_Aid_ByPlayerKingdom_Prefix(Kingdom __0, Kingdom __1)
        => !CaptureNativeAid(Clan.PlayerClan?.Kingdom, __0, __1, IsPlayerKingdom(Clan.PlayerClan?.Kingdom) ? "player_suggestion" : "native_request");
    private static bool Patch_Aid_ByPlayer_Prefix(Kingdom __0, Kingdom __1)
    {
        if (!AidTakeoverActive) return true;
        Kingdom caller = Clan.PlayerClan?.Kingdom, called = __0, enemy = __1;
        if (!IsPlayerKingdom(caller)) { WorldDiplomacyPresentation.OpenComposeFromTerminal(); return false; }
        long generation = SaveRuntimeGuard.CaptureGeneration();
        InformationManager.ShowInquiry(new InquiryData(
            DiplomacyUiLocalization.Text("AF_DIP_UI_Aid_RequestTitle", "Request allied aid"),
            DiplomacyUiLocalization.Format("AF_DIP_UI_Aid_RequestBody", "Ask {ALLY} to join the war against {ENEMY}? Your ally will decide separately. This request does not declare war for them or pay the native automatic call-to-war fee.",
                ("ALLY", called?.Name.ToString() ?? ""), ("ENEMY", enemy?.Name.ToString() ?? "")),
            true, true, DiplomacyUiLocalization.Text("AF_DIP_UI_Aid_Send", "Send request"),
            DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"),
            () => { if (SaveRuntimeGuard.IsCurrentGeneration(generation) && IsPlayerKingdom(caller)) CaptureNativeAid(caller, called, enemy, "player_request"); }, null), pauseGameActiveState: true);
        return false;
    }
    private static bool Patch_Aid_Start_Prefix(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
        => !CaptureNativeAid(callingKingdom, calledKingdom, kingdomToCallToWarAgainst, IsPlayerKingdom(callingKingdom) ? "player_suggestion" : "native_request");
    private static bool Patch_Aid_Deny_Prefix() => !AidTakeoverActive;
    private static bool Patch_Aid_AcceptOutcome_Prefix(AcceptCallToWarAgreementDecision __instance, DecisionOutcome chosenOutcome)
    {
        if (!AidTakeoverActive) return true;
        bool accepted = chosenOutcome is AcceptCallToWarAgreementDecision.AcceptCallToWarAgreementDecisionOutcome outcome && outcome.ShouldAcceptCallToWar;
        CaptureNativeAid(__instance.CallingKingdom, __instance.Kingdom, __instance.KingdomToCallToWarAgainst,
            accepted ? "legacy_accepted_decision" : "legacy_rejected_decision", accepted, accepted ? __instance.CallToWarCost : 0);
        if (!accepted)
        {
            var owner = ResolveInstance();
            owner?._orchestration.RecordNativeAidRefusal(__instance.CallingKingdom.StringId, __instance.Kingdom.StringId, __instance.KingdomToCallToWarAgainst.StringId);
        }
        return false;
    }
    private static void RegisterAidTakeover(Harmony harmony)
    {
        foreach (string name in new[] { "OnCallToWarAgreementProposedToPlayer", "OnCallToWarAgreementProposedToPlayerKingdom" })
            PatchAidMethod(harmony, typeof(AllianceCampaignBehavior), name, nameof(Patch_Aid_ToPlayer_Prefix));
        PatchAidMethod(harmony, typeof(AllianceCampaignBehavior), "OnCallToWarAgreementProposedByPlayer", nameof(Patch_Aid_ByPlayer_Prefix));
        PatchAidMethod(harmony, typeof(AllianceCampaignBehavior), "OnCallToWarAgreementProposedByPlayerKingdom", nameof(Patch_Aid_ByPlayerKingdom_Prefix));
        PatchAidMethod(harmony, typeof(AllianceCampaignBehavior), "StartCallToWarAgreement", nameof(Patch_Aid_Start_Prefix));
        PatchAidMethod(harmony, typeof(AllianceCampaignBehavior), "DenyCallToWarAgreement", nameof(Patch_Aid_Deny_Prefix));
        PatchAidMethod(harmony, typeof(AcceptCallToWarAgreementDecision), "ApplyChosenOutcome", nameof(Patch_Aid_AcceptOutcome_Prefix));
    }
    private static void PatchAidMethod(Harmony harmony, Type type, string method, string prefix)
    {
        var target = AccessTools.Method(type, method) ?? throw new InvalidOperationException("aid_patch_target_missing:" + method);
        harmony.Patch(target, prefix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), prefix));
    }
    private sealed partial class OrchestrationHost : IDiplomacyAidHost
    {
        public bool SettleLegacyAidCost(DiplomacyAidRequest request, out string reason)
        {
            reason = "";
            var caller = ResolveKingdom(request.CallingKingdomId); var called = ResolveKingdom(request.CalledKingdomId);
            if (caller == null || called == null) { reason = "legacy_aid_parties_missing"; return false; }
            if (request.LegacyPlayerPaying)
            {
                if (Hero.MainHero == null || Hero.MainHero.Gold < request.LegacyCost) { reason = "legacy_aid_payment_unavailable"; return false; }
                Hero.MainHero.ChangeHeroGold(-request.LegacyCost);
            }
            else caller.CallToWarWallet -= request.LegacyCost;
            called.CallToWarWallet += request.LegacyCost;
            return true;
        }
    }
}
