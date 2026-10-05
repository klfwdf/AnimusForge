using System;
using AnimusForge;

int passed = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException(label);
    passed++; Console.WriteLine("PASS " + label);
}
MockVm Vm(string text) => new MockVm { DialogText = text, CurrentCharacterNameLbl = "NPC" };
var a = new object(); var b = new object();
ConversationHelper.Clear();
var first = Vm("A opening"); ConversationHelper.SetCurrentVM(first);
ConversationHelper.BeginStreaming(a); ConversationHelper.UpdateDialogText("A fight reply"); ConversationHelper.Tick();
Check(first.DialogText == "A fight reply" && ConversationHelper.IsStreaming, "normal stream rendered");
first.DialogText = "native refresh"; ConversationHelper.SetCurrentVM(first); ConversationHelper.OnRefreshPostfix();
Check(first.DialogText == "A fight reply", "same VM refresh preserves stream");
var second = Vm("B opening"); ConversationHelper.SetCurrentVM(second); ConversationHelper.OnRefreshPostfix();
Check(second.DialogText == "B opening" && !ConversationHelper.IsStreaming, "different VM cannot replay old fight reply");
ConversationHelper.BeginStreaming(b); ConversationHelper.UpdateDialogText("B current"); ConversationHelper.Tick();
ConversationHelper.ClearForOwner(a); ConversationHelper.EndStreaming(a); ConversationHelper.OnRefreshPostfix();
Check(second.DialogText == "B current" && ConversationHelper.IsStreaming && ConversationHelper.HasActiveVM,
    "late close/end from old owner cannot retire new stream");
ConversationHelper.ClearForOwner(null);
Check(ConversationHelper.IsStreaming, "missing owner cannot clear owned stream");
ConversationHelper.EndStreaming(b); second.DialogText = "ordinary native line"; ConversationHelper.OnRefreshPostfix();
Check(second.DialogText == "ordinary native line" && !ConversationHelper.IsStreaming, "valid finish does not reapply on ordinary refresh");
ConversationHelper.BeginStreaming(b); ConversationHelper.UpdateDialogText("queued but not shown");
ConversationHelper.ClearForOwner(b); ConversationHelper.Tick();
Check(!ConversationHelper.HasActiveVM && !ConversationHelper.IsStreaming, "forced close clears queued text and VM");
ConversationHelper.SetCurrentVM(second); ConversationHelper.OnRefreshPostfix(); ConversationHelper.Tick();
Check(second.DialogText == "ordinary native line", "same VM reused after close does not inherit pending text");
ConversationHelper.BeginStreaming(b); ConversationHelper.StartTypewriterText("TTS old reply", 4f, waitForPlayback: true);
Check(ConversationHelper.IsTypewriterActive && ConversationHelper.IsTypewriterWaitingForPlayback, "TTS waiting typewriter preserved within conversation");
ConversationHelper.SetCurrentVM(Vm("C opening")); ConversationHelper.Tick(); ConversationHelper.OnRefreshPostfix();
Check(ConversationHelper.GetCurrentDialogText() == "C opening" && !ConversationHelper.IsTypewriterActive
    && !ConversationHelper.IsTypewriterWaitingForPlayback, "VM replacement also drops old TTS/pending state");
Check(!ConversationHelper.StartTypewriterPlaybackIfWaiting(), "old audio-start cannot revive retired typewriter");
ConversationHelper.BeginStreaming(b); ConversationHelper.UpdateDialogText("same-token old reply"); ConversationHelper.Tick();
ConversationHelper.Clear(); second.DialogText = "same-token new opening";
ConversationHelper.SetCurrentVM(second); ConversationHelper.OnRefreshPostfix(); ConversationHelper.Tick();
Check(second.DialogText == "same-token new opening", "real end then same-window/token reuse is clean");
ConversationHelper.BeginStreaming(); ConversationHelper.UpdateDialogText("legacy stream"); ConversationHelper.Tick();
Check(second.DialogText == "legacy stream", "parameterless helper API retained");
ConversationHelper.ClearForOwner(b);
Check(ConversationHelper.IsStreaming, "owned close cannot clear unrelated parameterless stream");
ConversationHelper.Clear();

bool Match(bool pending = true, string reason = "scene_taunt_lord_scene", string pf = "amazonian",
    string ps = "town_S5", string jf = "amazonian", string js = "town_S5", bool town = true, bool same = true)
    => SceneTauntJudgmentHandoffRules.ShouldHandOff(pending, reason, pf, ps, jf, js, town, same);
Check(Match(), "same faction/settlement incident eligible for native judgment handoff");
Check(Match(pf: "AMAZONIAN", ps: "TOWN_s5"), "saved stable ids case-insensitive");
Check(!Match(pending: false), "no pending event is a no-op");
Check(!Match(reason: "unrelated_scene_action"), "unrelated incident reason preserved");
Check(!Match(reason: null), "missing incident provenance preserved");
Check(!Match(pf: "vlandia"), "different target faction preserved");
Check(!Match(ps: "town_S6"), "different target settlement preserved");
Check(!Match(pf: "") && !Match(ps: null), "incomplete legacy identity not guessed");
Check(!Match(town: false), "non-town judgment not incorrectly admitted");
Check(!Match(same: false), "enemy-faction consequences preserved");
Check(!Match(jf: null) && !Match(js: ""), "missing current target not admitted");

SceneTauntBehavior Reset()
{
    var faction = new Faction { StringId = "amazonian" };
    Settlement.Current = new Settlement { StringId = "town_S5", MapFaction = faction };
    PartyBase.MainParty = new PartyBase { MapFaction = faction };
    Campaign.Current.CurrentMenuContext = null; LordEncounterBehavior.HostileCalls = 0;
    SceneTauntBehavior.MapReady = false; Logger.Entries.Clear();
    var host = new SceneTauntBehavior(); SceneTauntBehavior.Instance = host; host.RestoreIncident(); return host;
}
var host = Reset();
Check(host.Judgment(Settlement.Current) && !host.Pending && host.SavedIds == "", "actual host handoff clears bool and all persisted identity fields");
Check(Logger.Entries.Exists(s => s.Contains("criminal_judgment_handoff")), "explicit handoff reason logged");
int logs = Logger.Entries.Count;
Check(!host.Judgment(Settlement.Current) && Logger.Entries.Count == logs, "judgment transfer is idempotent");
host.ReturnToMap(); Check(LordEncounterBehavior.HostileCalls == 0, "log replay defeat-to-judgment-to-map cannot expel/hostilize again");
host = Reset(); host.ReturnToMap();
Check(LordEncounterBehavior.HostileCalls == 1 && !host.Pending, "escape without judgment retains real diplomatic dispatch");
host.ReturnToMap(); Check(LordEncounterBehavior.HostileCalls == 1, "normal diplomacy commits at most once");
host = Reset(); host.RestoreIncident(settlement: "town_S6");
Check(!host.Judgment(Settlement.Current) && host.Pending, "actual host preserves different-settlement incident");
host = Reset(); PartyBase.MainParty.MapFaction = new Faction { StringId = "other" };
Check(!host.Judgment(Settlement.Current) && host.Pending, "actual host preserves foreign-faction incident");
host = Reset(); Check(!host.Judgment(null) && host.Pending, "null settlement preserves event");
host = Reset(); host.Menu("town_inside_criminal"); host.ReturnToMap();
Check(!host.Pending && LordEncounterBehavior.HostileCalls == 0, "actual native menu reopening repairs saved pending incident");
host = Reset(); host.Menu("town"); Check(host.Pending, "ordinary menu does not settle event");
host = Reset(); Campaign.Current.CurrentMenuContext = new MenuContext { GameMenu = new GameMenu { StringId = "town_inside_criminal" } };
host.Load(); host.ReturnToMap();
Check(!host.Pending && LordEncounterBehavior.HostileCalls == 0, "legacy load already on judgment screen settles once");
host = Reset(); Campaign.Current.CurrentMenuContext = new MenuContext { GameMenu = new GameMenu { StringId = "town_outside" } };
host.Load(); Check(host.Pending, "ambiguous outside-town save is not guessed to be paid");
host = Reset(); SceneTauntBehavior.HandOffDeferredLordSceneDiplomacyToCriminalJudgmentForExternal(Settlement.Current);
Check(!host.Pending, "Mission handoff uses actual same incident owner");
SceneTauntBehavior.Instance = null; SceneTauntBehavior.HandOffDeferredLordSceneDiplomacyToCriminalJudgmentForExternal(Settlement.Current);
Check(true, "absent campaign owner is harmless");

// Execute the current Overlay presentation partial and actual main-thread queue methods.
var scope = new ShoutBehavior.NativeConversationPresentationScope();
var ui = new AnimusForgeNativeConversationOverlay(scope);
var display = Vm("presentation opening"); ConversationHelper.SetCurrentVM(display); ui.Begin();
System.Threading.Tasks.Task.Run(() => ui.Queue("queued valid reply")).GetAwaiter().GetResult();
Check(display.DialogText == "presentation opening", "worker callback cannot write UI before main-thread queue drain");
ui.Drain(); ConversationHelper.Tick();
Check(display.DialogText == "queued valid reply" && ui.Busy, "real current presentation callback renders after main-thread drain");
Check(ui.Finish() && !ui.Busy && !ConversationHelper.IsStreaming, "real valid presentation finish ends only its owned stream");
scope = new ShoutBehavior.NativeConversationPresentationScope(); ui = new AnimusForgeNativeConversationOverlay(scope); ui.Begin();
System.Threading.Tasks.Task.Run(() => ui.Queue("queued stale reply")).GetAwaiter().GetResult();
var newerScope = new ShoutBehavior.NativeConversationPresentationScope();
var newer = new AnimusForgeNativeConversationOverlay(newerScope); newer.Begin();
ConversationHelper.UpdateDialogText("newer active reply"); ConversationHelper.Tick(); scope.Context = false;
ui.Drain(); ConversationHelper.Tick();
Check(display.DialogText == "newer active reply" && ConversationHelper.IsStreaming && !ui.Busy,
    "real old callback retirement cannot clear newer Overlay stream");
Check(!ui.Finish() && ConversationHelper.IsStreaming, "real old finally cannot end newer Overlay stream");
Check(newer.Finish() && !ConversationHelper.IsStreaming, "newer Overlay still owns normal completion");
scope = new ShoutBehavior.NativeConversationPresentationScope(); ui = new AnimusForgeNativeConversationOverlay(scope); ui.Begin();
ConversationHelper.StartTypewriterText("interrupted old audio", 4f, waitForPlayback: true); scope.Context = false; ui.Validate();
Check(!ui.Busy && !ConversationHelper.IsTypewriterActive && !ConversationHelper.HasActiveVM,
    "real stale-context retirement clears its own typewriter and old VM");
ConversationHelper.SetCurrentVM(display); scope = new ShoutBehavior.NativeConversationPresentationScope(); ui = new AnimusForgeNativeConversationOverlay(scope); ui.Begin();
System.Threading.Tasks.Task.Run(() => ui.Queue("old generation")).GetAwaiter().GetResult(); ui.Advance();
ui.Drain(); Check(ConversationHelper.IsStreaming && !ui.Finish(), "old UI generation cannot clear current same-Overlay request");
ConversationHelper.Clear();
Console.WriteLine($"{passed}/{passed} helper, judgment and actual presentation lifecycle cases passed; real Bannerlord/old-save acceptance NOT_RUN");
