using System;
using System.Reflection;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.CoupSystem;

// All runtime gates are owned by the exact coup mission; no settlement-wide combat override.
internal static class CoupGuards
{
    private static bool _registered;
    private static Action<int, string> _interruptSpeech;
    private static Action<int, string> _cancelSpeech;
    private static Func<bool> _hasPendingAftermath;
    private static Func<bool> _hasArmedCarryover;
    private static bool _hostFlowProbeFailed;
    internal static bool MissionProtectionAvailable { get; private set; }
    internal static bool CaptivityProtectionAvailable => CoupCaptivityBehavior.AutomaticReleaseProtectionAvailable;

    internal static void Reset()
    {
        _registered = false;
        _interruptSpeech = null;
        _cancelSpeech = null;
        _hasPendingAftermath = null;
        _hasArmedCarryover = null;
        _hostFlowProbeFailed = false;
        MissionProtectionAvailable = false;
        CoupCaptivityBehavior.ResetPatches();
    }

    internal static bool HasBlockingHostFlow()
    {
        if (_hostFlowProbeFailed || _hasPendingAftermath == null || _hasArmedCarryover == null) return true;
        try { return _hasPendingAftermath() || _hasArmedCarryover(); }
        catch (Exception ex)
        {
            // Menu conditions may run repeatedly; fail closed and log this incompatibility once.
            _hostFlowProbeFailed = true;
            MissionProtectionAvailable = false;
            Logger.Log("Coup", "AF pending-flow query failed; coup entry disabled: " + ex.Message);
            return true;
        }
    }

    internal static void Register(Harmony harmony)
    {
        if (_registered || harmony == null) return;
        _registered = true;
        bool death = PatchPrefix(harmony,
            AccessTools.Method(typeof(Mission), "GetAgentState", new[] { typeof(Agent), typeof(Agent), typeof(DamageTypes), typeof(WeaponFlags) }),
            nameof(ProtectedHeroStatePrefix));
        Type conversationType = AccessTools.TypeByName("SandBox.Conversation.MissionLogics.MissionConversationLogic");
        bool conversation = PatchPrefix(harmony,
            conversationType == null ? null : AccessTools.Method(conversationType, "StartConversation", new[] { typeof(Agent), typeof(bool), typeof(bool) }),
            nameof(NativeConversationPrefix));
        Type alleyType = AccessTools.TypeByName("SandBox.Missions.MissionLogics.MissionAlleyHandler");
        bool alleyConversation = PatchPrefix(harmony,
            alleyType == null ? null : AccessTools.Method(alleyType, "CheckAndTriggerConversationWithRivalThug"), nameof(UnrelatedSceneFlowPrefix));
        bool alleyBattle = PatchPrefix(harmony,
            alleyType == null ? null : AccessTools.Method(alleyType, "StartCommonAreaBattle"), nameof(UnrelatedSceneFlowPrefix));
        bool afIsolation = RegisterAfGuards(harmony);
        bool customFight = PatchPrefix(harmony, AccessTools.Method(typeof(MissionFightHandler), "StartCustomFight"), nameof(UnrelatedSceneFlowPrefix));
        bool fistFight = PatchPrefix(harmony, AccessTools.Method(typeof(MissionFightHandler), "StartFistFight"), nameof(UnrelatedSceneFlowPrefix));
        bool lifecycle = RegisterLifecycleGuards(harmony);
        bool passage = PatchPrefix(harmony, AccessTools.Method(typeof(PassageUsePoint), "OnUse", new[] { typeof(Agent), typeof(sbyte) }), nameof(NativePassageUsePrefix));
        MissionProtectionAvailable = death && conversation && alleyConversation && alleyBattle && afIsolation && customFight && fistFight && lifecycle && passage;
        CoupCaptivityBehavior.RegisterPatches(harmony);
        Logger.Log("Coup", "Guards registered. mission=" + MissionProtectionAvailable + ", captivity=" + CaptivityProtectionAvailable);
    }

    private static bool RegisterLifecycleGuards(Harmony harmony)
    {
        // Mission.AfterStart/EndMissionInternal enumerate the behavior collection.
        // Keep the native owners attached, including their interaction cleanup, and
        // suppress only callbacks which would replace the coup's result or menu.
        bool ready = PatchPrefix(harmony, AccessTools.Method(typeof(LeaveMissionLogic), "OnMissionTick", new[] { typeof(float) }), nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchPrefix(harmony, AccessTools.Method(typeof(LeaveMissionLogic), "MissionEnded", new[] { typeof(MissionResult).MakeByRefType() }), nameof(NativeMissionEndedPrefix));
        ready &= PatchPrefix(harmony, AccessTools.Method(typeof(BasicLeaveMissionLogic), "MissionEnded", new[] { typeof(MissionResult).MakeByRefType() }), nameof(NativeMissionEndedPrefix));
        ready &= PatchPrefix(harmony, AccessTools.Method(typeof(BasicLeaveMissionLogic), "OnEndMissionRequest", new[] { typeof(bool).MakeByRefType() }), nameof(NativeLeaveRequestPrefix));
        ready &= PatchPrefix(harmony, AccessTools.Method(typeof(SandBoxMissionHandler), "OnAgentRemoved", new[] { typeof(Agent), typeof(Agent), typeof(AgentState), typeof(KillingBlow) }), nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchPrefix(harmony, AccessTools.Method(typeof(MissionCrimeHandler), "OnEndMission", Type.EmptyTypes), nameof(NativeOutcomeCallbackPrefix));
        // Initial population is built synchronously by the native controller's AfterStart.
        // Subsequent ticks move civilians between locations and spawn delayed arrivals;
        // disabling passage widgets alone does not stop that location-level traffic.
        ready &= PatchPrefix(harmony, AccessTools.Method(typeof(MissionAgentHandler), "OnMissionTick", new[] { typeof(float) }), nameof(NativeOutcomeCallbackPrefix));
        return ready;
    }

    private static bool RegisterAfGuards(Harmony harmony)
    {
        // AF is a separate, installed assembly. Resolve its private seam once at startup;
        // no reflection or call-stack inspection occurs on hit/tick paths.
        bool ready = true;
        try
        {
            _hasPendingAftermath = BindHostFlowQuery("SiegeAiInterventionBehavior", "IsInterventionMissionOpenOrPendingForExternal");
            _hasArmedCarryover = BindHostFlowQuery("SceneTauntBehavior", "HasArmedCarryoverForCurrentSettlement");
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "AF pending-flow query unavailable: " + ex.Message);
            ready = false;
        }
        try
        {
            Type shout = AccessTools.TypeByName("AnimusForge.ShoutBehavior");
            _interruptSpeech = (Action<int, string>)Delegate.CreateDelegate(typeof(Action<int, string>),
                AccessTools.Method(shout, "InterruptAgentSpeechForCombatExternal", new[] { typeof(int), typeof(string) }));
            _cancelSpeech = (Action<int, string>)Delegate.CreateDelegate(typeof(Action<int, string>),
                AccessTools.Method(shout, "CancelAgentSpeechForRemovalExternal", new[] { typeof(int), typeof(string) }));
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "AF speech cleanup seam unavailable: " + ex.Message);
            ready = false;
        }
        ready &= PatchAfPrefix(harmony, "SiegeAiInterventionBehavior", "OnMissionStarted", nameof(AftermathMissionPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntBehavior", "OnMissionStarted", nameof(AftermathMissionPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "OnMissionTick", nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "OnAgentHit", nameof(SceneTauntHitPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "OnScoreHit", nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "OnAgentRemoved", nameof(SceneTauntRemovedPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "CanStartConflict", nameof(BehaviorBooleanFalsePrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "ApplyArmedConflictStartCrimeForExternal", nameof(UnrelatedSceneFlowPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "TryApplyArmedNpcKnockdownConsequencesCore", nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "ShouldSuppressNativeMissionConversationExternal", nameof(MissionBooleanTruePrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "ShouldBlockSceneExitExternal", nameof(MissionBooleanFalsePrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "ShouldUseOwnedSettlementPassiveAttackForExternal", nameof(CurrentMissionBooleanFalsePrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntMissionBehavior", "ShouldUseFullCombatDamageExternal", nameof(CurrentMissionBooleanFalsePrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntPlayerDeathAgentStateDeciderLogic", "GetAgentState", nameof(SceneTauntStatePrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntConsequenceMissionLogic", "OnEndMissionRequest", nameof(NativeLeaveRequestPrefix));
        ready &= PatchAfPrefix(harmony, "SceneTauntConsequenceMissionLogic", "OnMissionTick", nameof(NativeOutcomeCallbackPrefix));

        ready &= PatchAfPrefix(harmony, "NoblePrisonerEscortBehavior", "OnMissionStarted", nameof(AftermathMissionPrefix));
        ready &= PatchAfPrefix(harmony, "NoblePrisonerEscortMissionBehavior", "OnMissionTick", nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchAfPrefix(harmony, "NoblePrisonerEscortBehavior", "ShouldInjectOrderViewsForExternal", nameof(MissionBooleanFalsePrefix));
        ready &= PatchAfPrefix(harmony, "TownAmbientDialogueMissionBehavior", "OnMissionTick", nameof(NativeOutcomeCallbackPrefix));
        ready &= PatchAfPrefix(harmony, "InterventionNativeTownCivilianPopulationMissionBehavior", "TryEnsureNativeTownCivilianMaximum", nameof(NativeOutcomeCallbackPrefix));
        return ready;
    }

    private static Func<bool> BindHostFlowQuery(string typeName, string methodName)
    {
        Type type = AccessTools.TypeByName("AnimusForge." + typeName);
        MethodInfo method = type == null ? null : AccessTools.Method(type, methodName, Type.EmptyTypes);
        if (method == null) throw new MissingMethodException(typeName + "." + methodName);
        return (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
    }

    private static bool PatchAfPrefix(Harmony harmony, string typeName, string methodName, string prefix)
    {
        Type type = AccessTools.TypeByName("AnimusForge." + typeName);
        MethodInfo target = type == null ? null : AccessTools.Method(type, methodName);
        if (target == null) Logger.Log("Coup", "Required AF seam missing: " + typeName + "." + methodName);
        return PatchPrefix(harmony, target, prefix);
    }

    private static bool PatchPrefix(Harmony harmony, MethodInfo target, string prefix)
    {
        try
        {
            if (target == null) throw new MissingMethodException(prefix + " target not found");
            harmony.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(CoupGuards), prefix)) { priority = Priority.First });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "Guard unavailable: " + prefix + "; " + ex.Message);
            return false;
        }
    }

    // __1 is the victim in both supported signatures. No damage is cancelled and
    // ordinary troops retain the native killed/unconscious and surgery decisions.
    private static bool ProtectedHeroStatePrefix(Mission __instance, Agent __1, ref AgentState __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance)) return true;
        Hero hero = (__1?.Character as CharacterObject)?.HeroObject;
        if (hero == null || (hero != Hero.MainHero && hero.StringId != CoupCampaignBehavior.CurrentSession?.KingId)) return true;
        __result = AgentState.Unconscious;
        return false;
    }

    private static bool NativeConversationPrefix(Agent agent)
    {
        return !CoupCampaignBehavior.IsMissionActive(agent?.Mission ?? Mission.Current);
    }

    private static bool NativePassageUsePrefix(PassageUsePoint __instance, Agent userAgent)
    {
        Mission mission = userAgent?.Mission ?? Mission.Current;
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        // The native callback writes NextLocation and closes the scene immediately.
        // Own this event before that write; raw key polling cannot arbitrate the two paths.
        mission.GetMissionBehavior<CoupMissionBehavior>()?.HandlePassageUse(__instance, userAgent);
        return false;
    }

    private static bool UnrelatedSceneFlowPrefix()
    {
        return !CoupCampaignBehavior.IsMissionActive(Mission.Current);
    }

    private static bool AftermathMissionPrefix(IMission mission)
    {
        return !(mission is Mission concrete && CoupCampaignBehavior.IsMissionActive(concrete));
    }

    private static bool NativeOutcomeCallbackPrefix(MissionBehavior __instance)
    {
        return !CoupCampaignBehavior.IsMissionActive(__instance?.Mission);
    }

    private static bool NativeMissionEndedPrefix(MissionBehavior __instance, ref bool __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance?.Mission)) return true;
        __result = false;
        return false;
    }

    private static bool NativeLeaveRequestPrefix(MissionBehavior __instance, ref bool canPlayerLeave, ref InquiryData __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance?.Mission)) return true;
        // The coup owner records retreat on actual closure, not when Tab is polled.
        canPlayerLeave = true;
        __result = null;
        return false;
    }

    private static bool SceneTauntHitPrefix(MissionBehavior __instance, Agent affectedAgent, Agent affectorAgent)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance?.Mission)) return true;
        if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main)
            _interruptSpeech?.Invoke(affectedAgent.Index, affectorAgent == Agent.Main ? "coup_player_hit" : "coup_agent_hit");
        return false;
    }

    private static bool SceneTauntRemovedPrefix(MissionBehavior __instance, Agent affectedAgent)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance?.Mission)) return true;
        if (affectedAgent != null && affectedAgent.IsHuman) _cancelSpeech?.Invoke(affectedAgent.Index, "coup_agent_removed");
        return false;
    }

    private static bool SceneTauntStatePrefix(MissionBehavior __instance, ref bool usedSurgery, ref AgentState __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance?.Mission)) return true;
        usedSurgery = false;
        __result = AgentState.None;
        return false;
    }

    private static bool BehaviorBooleanFalsePrefix(MissionBehavior __instance, ref bool __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(__instance?.Mission)) return true;
        __result = false;
        return false;
    }

    private static bool MissionBooleanTruePrefix(Mission mission, ref bool __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        __result = true;
        return false;
    }

    private static bool MissionBooleanFalsePrefix(Mission mission, ref bool __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        __result = false;
        return false;
    }

    private static bool CurrentMissionBooleanFalsePrefix(ref bool __result)
    {
        return MissionBooleanFalsePrefix(Mission.Current, ref __result);
    }
}
