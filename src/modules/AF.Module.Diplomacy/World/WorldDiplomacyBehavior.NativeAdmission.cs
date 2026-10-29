using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    [ThreadStatic] private static Stack<(Kingdom first, Kingdom second)> _eventPeaceScopes;
    [ThreadStatic] private static Stack<(Kingdom first, Kingdom second)> _eventWarScopes;

    internal static void RunAuthorizedEventWar(Kingdom first, Kingdom second, Action apply)
    {
        _eventWarScopes ??= new();
        _eventWarScopes.Push((first, second));
        try { apply(); }
        finally { _eventWarScopes.Pop(); }
    }

    internal static void RunAuthorizedEventPeace(Kingdom first, Kingdom second, Action apply)
    {
        _eventPeaceScopes ??= new();
        _eventPeaceScopes.Push((first, second));
        try { apply(); }
        finally { _eventPeaceScopes.Pop(); }
    }

    private static bool HasAuthorizedPeace(Kingdom first, Kingdom second) => _internalDiplomaticActionDepth > 0
        || VassalageBehavior.IsApplyingVassalageDiplomacy || MyBehavior.QuietPeaceActive
        || (_eventPeaceScopes?.Any(x => (x.first == first && x.second == second) || (x.first == second && x.second == first)) ?? false);

    private static bool AllowsNativePeace(Kingdom first, Kingdom second)
    {
        if (first == null || second == null || !IsWorldDiplomacyEnabled()) return true;
        var owner = ResolveInstance();
        if (HasAuthorizedPeace(first, second)) return true;
        if (owner == null) return false;
        return DiplomacyNativeAdmissionRules.AllowsPeace(true, false, IsPlayerKingdom(first), IsPlayerKingdom(second),
            WorldDiplomacyLiveRoundRules.Live(owner._storage), first.StringId, second.StringId);
    }

    private static bool Patch_EventPeace_Prefix(IFaction faction1, IFaction faction2)
        => AllowsNativePeace(faction1 as Kingdom, faction2 as Kingdom);

    private static bool Patch_EventWar_Prefix(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
    {
        if (!IsWorldDiplomacyEnabled() || _internalDiplomaticActionDepth > 0 || VassalageBehavior.IsApplyingVassalageDiplomacy
            || faction1 is not Kingdom first || faction2 is not Kingdom second) return true;
        if (_eventWarScopes?.Any(x => x.first == first && x.second == second) == true) return true;
        // Structural changes and player hostility are consequences of actual
        // world actions; routine political declarations belong to AF.
        if (declareWarDetail != DeclareWarAction.DeclareWarDetail.Default
            && declareWarDetail != DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision
            && declareWarDetail != DeclareWarAction.DeclareWarDetail.CausedByCallToWarAgreement) return true;
        var owner = ResolveInstance();
        owner?._orchestration.RecordNativeSignal(first.StringId, second.StringId, "declare_war", "native_political_war_request");
        return false;
    }

    private static void Patch_EventPeaceScore_Postfix(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, ref float __result)
    {
        if (!IsWorldDiplomacyEnabled() || factionDeclaresPeace is not Kingdom first
            || factionDeclaredPeace is not Kingdom second || IsPlayerKingdom(first) || IsPlayerKingdom(second)
            || HasAuthorizedPeace(first, second)) return;
        __result = DiplomacyNativeAdmissionRules.AdjustPeaceScore(__result, GetWarTendency());
    }

    private static void RegisterEventNativeAdmission(Harmony harmony)
    {
        var war = AccessTools.Method(typeof(DeclareWarAction), "ApplyInternal");
        var peace = AccessTools.Method(typeof(MakePeaceAction), "ApplyInternal");
        if (war == null || peace == null) throw new InvalidOperationException("diplomacy_native_action_targets_missing");
        var model = typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultDiplomacyModel);
        foreach (string method in new[] { "GetScoreOfDeclaringPeace", "GetScoreOfDeclaringPeaceForClan" })
        {
            var score = AccessTools.Method(model, method);
            if (score == null) throw new InvalidOperationException("diplomacy_peace_score_target_missing");
            harmony.Patch(score, postfix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_EventPeaceScore_Postfix)));
        }
        harmony.Patch(war, prefix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_EventWar_Prefix)));
        harmony.Patch(peace, prefix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_EventPeace_Prefix)));
    }
}
