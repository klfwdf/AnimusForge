using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); checks++; }
ConversationManager Setup()
{
    NativeDialogueBattleContinuation.Cancel("fixture");
    Campaign.Current = new Campaign(); Game.Current = new Game(); Mission.Current = null;
    PlayerEncounter.Current = new PlayerEncounter(); PlayerEncounter.EncounteredParty = new PartyBase();
    PlayerEncounter.LeaveEncounter = PlayerEncounter.PlayerSurrender = PlayerEncounter.EnemySurrender = PlayerEncounter.BattleCreated = false;
    PlayerEncounterCompat.Result = PlayerEncounterCompat.PostResult = MapSeaContextGuard.AtSea = MeetingBattleRuntime.IsMeetingActive = LordEncounterBehavior.NativeActivity = false;
    GameMenu.Activations = 0; GameMenu.ThrowOnActivate = GameMenu.Reenter = false;
    LordEncounterBehavior.Logs.Clear();
    NativeDialogueBattleContinuation.Begin(new Hero());
    Campaign.Current.ConversationManager.IsConversationInProgress = true;
    ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = true;
    return Campaign.Current.ConversationManager;
}
void Ready() => ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = false;
foreach (string sentence in new[] { "player_turns_down_surrender", "lord_attack_verify_commit", "frivolous_surrender_demand_response" })
{
    var manager = Setup(); manager.ProcessSentence(new() { Id = sentence });
    NativeDialogueBattleContinuation.Tick(); Check(GameMenu.Activations == 0, "no battle while final reply still on screen: " + sentence);
    manager.EndConversation(); NativeDialogueBattleContinuation.Tick();
    Check(GameMenu.Activations == 0, "wait for map UI teardown: " + sentence);
    Ready(); GameMenu.Reenter = true; NativeDialogueBattleContinuation.Tick();
    Check(GameMenu.Activations == 1 && PlayerEncounter.BattleCreated && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "encounter", "confirmed combat resumes native menu: " + sentence);
    Check(!NativeDialogueBattleContinuation.IsResumingNativeBattleMenu, "temporary redirect bypass released: " + sentence);
    Check(LordEncounterBehavior.Logs.Any(x => x.Contains("combat_sentence_confirmed")) && LordEncounterBehavior.Logs.Any(x => x.Contains("combat_menu_queued")) && LordEncounterBehavior.Logs.Any(x => x.Contains("combat_menu_resumed")), "complete diagnostic chain: " + sentence);
    for (int i = 0; i < 20; i++) NativeDialogueBattleContinuation.Tick();
    manager.EndConversation(); NativeDialogueBattleContinuation.Tick();
    Check(GameMenu.Activations == 1, "reentry/repeated end cannot duplicate resume: " + sentence);
}
foreach (string sentence in new[] { "main_option_hostile_1_2", "player_threatens_enemy_lord", "player_cancels_attack_on_enemy_lord", "lord_attack_verify2", "barter_with_lord_postbarter_1", "lord_start", "hero_give_issue", null })
{
    var manager = Setup(); manager.ProcessSentence(new() { Id = sentence }); manager.EndConversation(); Ready(); NativeDialogueBattleContinuation.Tick();
    Check(GameMenu.Activations == 0, "noncombat/unfinished demand/farewell never fights: " + sentence);
}
foreach (string cancel in new[] { "leave", "surrender", "enemy_surrender", "result", "post_result", "sea", "siege", "meeting", "party", "encounter", "manager", "save", "state", "menu", "mission", "new_dialogue", "later_sentence" })
{
    var manager = Setup(); manager.ProcessSentence(new() { Id = "player_turns_down_surrender" }); manager.EndConversation(); Ready();
    switch (cancel)
    {
        case "leave": PlayerEncounter.LeaveEncounter = true; break;
        case "surrender": PlayerEncounter.PlayerSurrender = true; break;
        case "enemy_surrender": PlayerEncounter.EnemySurrender = true; break;
        case "result": PlayerEncounterCompat.Result = true; break;
        case "post_result": PlayerEncounterCompat.PostResult = true; break;
        case "sea": MapSeaContextGuard.AtSea = true; break;
        case "siege": LordEncounterBehavior.NativeActivity = true; break;
        case "meeting": MeetingBattleRuntime.IsMeetingActive = true; break;
        case "party": PlayerEncounter.EncounteredParty = new(); break;
        case "encounter": PlayerEncounter.Current = new(); break;
        case "manager": Campaign.Current.ConversationManager = new(); break;
        case "save": SaveRuntimeGuard.AdvanceGeneration("test"); break;
        case "state": PlayerEncounter.Current.EncounterState = PlayerEncounterState.End; break;
        case "menu": Campaign.Current.CurrentMenuContext.GameMenu.StringId = "encounter"; break;
        case "mission": Mission.Current = new(); break;
        case "new_dialogue": NativeDialogueBattleContinuation.Begin(new Hero()); break;
        case "later_sentence": manager.ProcessSentence(new() { Id = "lord_start" }); manager.EndConversation(); break;
    }
    NativeDialogueBattleContinuation.Tick(); Check(GameMenu.Activations == 0, "queued battle revalidates " + cancel);
}
foreach (string context in new[] { "scene", "sea", "siege", "custom_meeting", "settlement", "other_menu", "non_encounter" })
{
    var manager = Setup();
    switch (context)
    {
        case "scene": Mission.Current = new(); break;
        case "sea": MapSeaContextGuard.AtSea = true; break;
        case "siege": LordEncounterBehavior.NativeActivity = true; break;
        case "custom_meeting": MeetingBattleRuntime.IsMeetingActive = true; break;
        case "settlement": PlayerEncounter.EncounteredParty.IsMobile = false; break;
        case "other_menu": Campaign.Current.CurrentMenuContext.GameMenu.StringId = "encounter_meeting"; break;
        case "non_encounter": Campaign.Current.CurrentConversationContext = ConversationContext.Default; break;
    }
    NativeDialogueBattleContinuation.Begin(new Hero()); manager.ProcessSentence(new() { Id = "player_turns_down_surrender" });
    manager.EndConversation(); Ready(); NativeDialogueBattleContinuation.Tick(); Check(GameMenu.Activations == 0, "outside owned handoff never schedules: " + context);
}
{
    var manager = Setup(); NativeDialogueBattleContinuation.SentenceProcessed(manager, new() { Id = "player_turns_down_surrender" }, false);
    manager.EndConversation(); Ready(); NativeDialogueBattleContinuation.Tick(); Check(GameMenu.Activations == 0, "skipped ProcessSentence is not a fact");
    manager = Setup(); manager.ProcessSentence(new() { Id = "player_turns_down_surrender" });
    NativeDialogueBattleContinuation.ConversationEnded(manager, false); Ready(); NativeDialogueBattleContinuation.Tick(); Check(GameMenu.Activations == 0, "skipped EndConversation never queues");
    manager.EndConversation(); GameMenu.ThrowOnActivate = true; NativeDialogueBattleContinuation.Tick();
    Check(!NativeDialogueBattleContinuation.IsResumingNativeBattleMenu, "failed activation always restores redirect policy");
    GameMenu.ThrowOnActivate = false; NativeDialogueBattleContinuation.Tick(); Check(GameMenu.Activations == 0, "partial native failure is not replayed blindly");
    Check(HarmonyLib.Harmony.Patches == 2, "hooks installed only once across all conversations");
}
Console.WriteLine($"PASS {checks} production native combat continuation lifecycle checks (game/menu/Harmony fixtures).");
