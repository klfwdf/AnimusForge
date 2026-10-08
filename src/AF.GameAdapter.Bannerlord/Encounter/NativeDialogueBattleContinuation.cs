using System;
using HarmonyLib;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Restores only a confirmed vanilla combat ending opened from our custom encounter menu.
// Vanilla normally resumes encounter_meeting, whose init creates Battle. Our custom menu does not.
internal static class NativeDialogueBattleContinuation
{
    private static readonly EncounterPendingReturnOwner<PlayerEncounter, PartyBase> Scope = new();
    private static ConversationManager _manager;
    private static Hero _target;
    private static string _combatSentence;
    private static bool _pending;
    private static int _attempts, _revision;
    private static float _queuedAt, _nextCheck;
    private static string _waitReason;
    internal static bool IsCombatRequestedForCurrentEncounter => _combatSentence != null && IsCurrent(_manager);
    private static bool _sentencePatched, _endPatched;
    internal static bool IsResumingNativeBattleMenu { get; private set; }

    internal static void Begin(Hero target)
    {
        Cancel("new_conversation");
        if (!CanOwnHandoff(target)) { Log("handoff_rejected", "ineligible_context"); return; }
        if (!EnsurePatched()) { Log("handoff_rejected", "hooks_unavailable"); return; }
        _manager = Campaign.Current.ConversationManager;
        _target = target;
        Scope.Mark(PlayerEncounter.Current, PlayerEncounter.EncounteredParty, SaveRuntimeGuard.CaptureGeneration());
        Log("handoff_captured", "native_dialogue");
    }

    private static bool CanOwnHandoff(Hero target)
    {
        return target != null && Mission.Current == null && PlayerEncounter.Current != null
            && PlayerEncounter.EncounteredParty?.IsMobile == true
            && Campaign.Current?.ConversationManager != null
            && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter
            && Campaign.Current.CurrentMenuContext?.GameMenu?.StringId == "AnimusForge_lord_encounter"
            && !MapSeaContextGuard.IsCurrentPlayerEncounterAtSea(target)
            && !LordEncounterBehavior.IsNativeEncounterActivityContext(target)
            && !MeetingBattleRuntime.IsMeetingActive;
    }

    private static bool IsCurrent(ConversationManager manager)
        => ReferenceEquals(manager, _manager) && ReferenceEquals(manager, Campaign.Current?.ConversationManager)
            && Scope.IsCurrent(PlayerEncounter.Current, PlayerEncounter.EncounteredParty, SaveRuntimeGuard.CaptureGeneration());

    // Stable terminal NPC sentence IDs in both 1.3 and 1.4. Their output is close_window.
    // Only reaching the final fight response counts; merely opening demands/being at war does not.
    internal static bool IsCombatEnding(string id)
        => id == "player_turns_down_surrender" || id == "lord_attack_verify_commit"
            || id == "frivolous_surrender_demand_response";

    internal static void SentenceProcessed(ConversationManager __instance, ConversationSentenceOption __0, bool __runOriginal)
    {
        if (!__runOriginal || !Scope.IsPending) return;
        try
        {
            if (!IsCurrent(__instance)) return;
            _combatSentence = IsCombatEnding(__0.Id) ? __0.Id : null;
            if (_combatSentence != null)
            {
                LordEncounterBehavior.DiscardPeacefulCleanupForNativeCombat();
                Log("combat_sentence_confirmed", _combatSentence);
            }
        }
        catch (Exception ex) { Cancel("sentence_capture_failed"); Logger.Log("LordEncounter", "Native combat sentence capture failed: " + ex.Message); }
    }

    internal static void ConversationEnded(ConversationManager __instance, bool __runOriginal)
    {
        if (!__runOriginal || !Scope.IsPending) return;
        try
        {
            if (!ReferenceEquals(__instance, _manager)) return;
            if (!IsCurrent(__instance) || _combatSentence == null || !CanResumeCombat())
            {
                Cancel("ended_without_combat_continuation");
                return;
            }
            if (_pending) return;
            _pending = true;
            _queuedAt = Time.ApplicationTime;
            _nextCheck = _queuedAt;
            Log("combat_menu_queued", _combatSentence);
        }
        catch (Exception ex) { Cancel("end_capture_failed"); Logger.Log("LordEncounter", "Native combat end capture failed: " + ex.Message); }
    }

    private static bool CanResumeCombat()
        => PlayerEncounter.Current != null && !PlayerEncounter.LeaveEncounter && !PlayerEncounter.PlayerSurrender
            && !PlayerEncounter.EnemySurrender
            && (PlayerEncounter.Current.EncounterState == PlayerEncounterState.Begin || PlayerEncounter.Current.EncounterState == PlayerEncounterState.Wait)
            && !PlayerEncounterCompat.HasCampaignBattleResult() && !PlayerEncounterCompat.IsInPostBattleResultFlow()
            && !MapSeaContextGuard.IsCurrentPlayerEncounterAtSea(_target)
            && !LordEncounterBehavior.IsNativeEncounterActivityContext(_target)
            && !MeetingBattleRuntime.IsMeetingActive;

    private static bool HasConfirmedBattle()
    {
        var battle = PlayerEncounterCompat.GetBattleSafe();
        if (battle == null || !ReferenceEquals(PartyBase.MainParty?.MapEvent, battle)
            || !ReferenceEquals(PlayerEncounter.EncounteredParty?.MapEvent, battle)) return false;
        if (Mission.Current != null) return Mission.Current.Mode == MissionMode.Battle;
        return Game.Current?.GameStateManager?.ActiveState is MapState map && !map.MapConversationActive
            && Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == "encounter";
    }

    private static void WaitFor(string reason)
    {
        if (_waitReason == reason) return;
        _waitReason = reason;
        Log("combat_wait", reason);
    }

    internal static void Tick()
    {
        // No game reads without work. Throttle pending checks and bound total lifetime.
        if (!_pending || IsResumingNativeBattleMenu) return;
        float now = Time.ApplicationTime;
        if (now < _nextCheck) return;
        _nextCheck = now + 0.25f;
        try
        {
            if (!IsCurrent(_manager)) { Cancel("scope_changed"); return; }
            if (PlayerEncounter.LeaveEncounter || PlayerEncounter.PlayerSurrender || PlayerEncounter.EnemySurrender)
            { Cancel("leave_or_surrender"); return; }
            if (HasConfirmedBattle())
            { Log("combat_menu_resumed", _combatSentence); Cancel(null); return; }
            if (!CanResumeCombat()) { Cancel("native_state_changed"); return; }
            if (now - _queuedAt >= 15f) { Cancel("continuation_timeout"); return; }
            if (_manager.IsConversationInProgress) { WaitFor("conversation_active"); return; }
            if (Mission.Current != null) { Cancel("mission_changed"); return; }
            if (!(Game.Current?.GameStateManager?.ActiveState is MapState map) || map.MapConversationActive)
            { WaitFor("map_conversation_teardown"); return; }
            string menu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
            if (!string.IsNullOrEmpty(menu) && menu != "AnimusForge_lord_encounter" && menu != "encounter")
            { WaitFor("other_menu:" + menu); return; }
            if (_attempts >= 3) { Cancel("activation_not_confirmed_after_3_attempts"); return; }

            // Keep ownership until readback succeeds. Scoped guard prevents callback reentry.
            _attempts++;
            _nextCheck = now + 0.5f;
            Log("combat_menu_attempt", _attempts.ToString());
            int revision = _revision;
            var manager = _manager;
            string sentence = _combatSentence;
            IsResumingNativeBattleMenu = true;
            try { GameMenu.ActivateGameMenu("encounter"); }
            finally { IsResumingNativeBattleMenu = false; }
            // Menu callbacks may load a save or replace the encounter/conversation.
            if (!_pending || revision != _revision || !ReferenceEquals(manager, _manager)) return;
            if (!IsCurrent(manager)) { Cancel("scope_changed_during_activation"); return; }
            if (PlayerEncounter.LeaveEncounter || PlayerEncounter.PlayerSurrender || PlayerEncounter.EnemySurrender)
            { Cancel("leave_or_surrender_during_activation"); return; }
            if (HasConfirmedBattle())
            { Log("combat_menu_resumed", sentence); Cancel(null); }
            else WaitFor("activation_not_confirmed");
        }
        catch (Exception ex)
        {
            // Unknown partial native effects must never be blindly retried.
            Cancel("resume_failed"); Logger.Log("LordEncounter", "Native combat continuation failed: " + ex);
        }
    }

    internal static void Cancel(string reason)
    {
        if (Scope.IsPending && reason != null) Log("combat_menu_cancelled", reason);
        unchecked { _revision++; }
        Scope.Clear(); _manager = null; _target = null; _combatSentence = null; _pending = false;
        _attempts = 0; _nextCheck = _queuedAt = 0; _waitReason = null;
    }

    private static void Log(string stage, string reason)
        => LordEncounterBehavior.LogEncounterDiagnostic("NativeDialogueCombat", stage + ":" + reason);

    private static bool EnsurePatched()
    {
        if (_sentencePatched && _endPatched) return true;
        try
        {
            var harmony = new Harmony("AnimusForge.native_dialogue_combat_continuation");
            if (!_sentencePatched)
            {
                harmony.Patch(AccessTools.Method(typeof(ConversationManager), nameof(ConversationManager.ProcessSentence), new[] { typeof(ConversationSentenceOption) }),
                    postfix: new HarmonyMethod(typeof(NativeDialogueBattleContinuation), nameof(SentenceProcessed)));
                _sentencePatched = true;
            }
            if (!_endPatched)
            {
                harmony.Patch(AccessTools.Method(typeof(ConversationManager), nameof(ConversationManager.EndConversation), Type.EmptyTypes),
                    postfix: new HarmonyMethod(typeof(NativeDialogueBattleContinuation), nameof(ConversationEnded)));
                _endPatched = true;
            }
            Logger.Log("LordEncounter", "Native dialogue combat continuation hooks installed.");
        }
        catch (Exception ex) { Logger.Log("LordEncounter", "Native dialogue combat continuation hooks failed: " + ex); }
        return _sentencePatched && _endPatched;
    }
}
