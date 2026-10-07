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
    private static bool _sentencePatched, _endPatched;
    internal static bool IsResumingNativeBattleMenu { get; private set; }

    internal static void Begin(Hero target)
    {
        Cancel("new_conversation");
        if (!CanOwnHandoff(target) || !EnsurePatched()) return;
        _manager = Campaign.Current.ConversationManager;
        _target = target;
        Scope.Mark(PlayerEncounter.Current, PlayerEncounter.EncounteredParty, SaveRuntimeGuard.CaptureGeneration());
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
            if (_combatSentence != null) Log("combat_sentence_confirmed", _combatSentence);
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

    internal static void Tick()
    {
        // Existing application tick; no work, allocations or game reads without a queued ending.
        if (!_pending) return;
        try
        {
            if (!IsCurrent(_manager) || !CanResumeCombat()) { Cancel("stale_or_released"); return; }
            if (_manager.IsConversationInProgress) return;
            if (Mission.Current != null) { Cancel("mission_changed"); return; }
            if (!(Game.Current?.GameStateManager?.ActiveState is MapState map) || map.MapConversationActive) return;
            string menu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
            if (menu != "AnimusForge_lord_encounter") { Cancel("native_flow_already_resumed"); return; }

            // Consume before entering native menu init: it may reenter callbacks. Do not replay diplomacy,
            // force a mission, clear leave/surrender flags, or manufacture combat merely from hostility.
            string sentence = _combatSentence;
            Cancel(null);
            IsResumingNativeBattleMenu = true;
            try { GameMenu.ActivateGameMenu("encounter"); }
            finally { IsResumingNativeBattleMenu = false; }
            Log("combat_menu_resumed", sentence);
        }
        catch (Exception ex) { Cancel("resume_failed"); Logger.Log("LordEncounter", "Native combat continuation failed: " + ex); }
    }

    internal static void Cancel(string reason)
    {
        if (_pending && reason != null) Log("combat_menu_cancelled", reason);
        Scope.Clear(); _manager = null; _target = null; _combatSentence = null; _pending = false;
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
