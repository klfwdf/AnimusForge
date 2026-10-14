using System.Reflection;
using System.Xml.Linq;
using AnimusForge;
using AnimusForge.DialogueUI;
using AnimusForge.DialogueUI.Native;
using AnimusForge.DialogueUI.Shout;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

int checks = 0;
void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
Campaign.Current = new Campaign();
Campaign.Current.ConversationManager.IsConversationInProgress = true;
Campaign.Current.ConversationManager.OneToOneConversationCharacter = new CharacterObject { IsHero = true };
Mission.Current = new Mission();
int switches = 0, submissions = 0, manualTags = 0;
string submitted = null;
AnimusForgeNativeConversationOverlayVM host = null;
host = new(s => { submissions++; submitted = s; }, () => { switches++; host.SetInputVisible(!host.IsCustomAnswerVisible); }, null, null, null, null, onDirectActions: () => manualTags++);
Check(NativeUiAdapter.TryWrap(host, out var wrapper), "first native wrapping succeeds");
var vm = (NativeOverlayVM)wrapper;
Check(switches == 1 && vm.IsCustomAnswerVisible, "captured default applied once");
int manualVisibilityChanges = 0;
vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(NativeOverlayVM.IsDirectActionsVisible)) manualVisibilityChanges++; };
Check(!vm.IsDirectActionsVisible && !vm.CanOpenDirectActions, "displayed wrapper hides manual tags with developer mode off");
vm.OpenDirectActions();
Check(manualTags == 0, "hidden manual action cannot execute");
DuelSettings.GetSettings().EnableDevEditHistory = true;
host.RefreshDirectActionsVisibility();
Check(vm.IsDirectActionsVisible && vm.CanOpenDirectActions && manualVisibilityChanges == 1, "developer toggle publishes visibility on actual displayed wrapper");
DuelSettings.GetSettings().EnableDevEditHistory = false;
host.RefreshDirectActionsVisibility();
Check(!vm.IsDirectActionsVisible && !vm.CanOpenDirectActions && manualVisibilityChanges == 2, "developer disable refreshes wrapper visibility and command permission");
DuelSettings.GetSettings().EnableDevEditHistory = true;
host.RefreshDirectActionsVisibility();
var manualMethod = typeof(NativeOverlayVM).GetMethod("OpenDirectActions");
var manualEnabled = typeof(NativeOverlayVM).GetProperty("CanOpenDirectActions");
Check(manualMethod != null && manualEnabled != null, "displayed native wrapper exposes manual-tag command and binding");
manualMethod.Invoke(vm, null);
Check(manualTags == 1 && submissions == 0 && (bool)manualEnabled.GetValue(vm), "native manual button forwards only to existing tag menu, not chat");
host.CanOpenDirectActions = false; manualMethod.Invoke(vm, null);
Check(manualTags == 1 && !(bool)manualEnabled.GetValue(vm), "native wrapper honors busy host manual guard");
host.CanOpenDirectActions = true;
int sceneOpens = 0;
var sceneLayer = new AnimusForge.DialogueUI.Scene.SessionLayer();
Action finishScene = null;
ShoutBehavior.ManualMenu = finish => {
    sceneOpens++; finishScene = finish;
    Check(!sceneLayer.Interactive && sceneLayer._vm.Hidden, "scene layer releases focus and hides before native tag inquiry opens");
    return true;
};
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.ExecuteDirectActions();
Check(sceneOpens == 0, "scene manual click queues work, never reads game state in click callback");
sceneLayer._vm.Flush();
Check(sceneOpens == 1, "double click before tick coalesces to one manual menu");
finishScene(); Check(!sceneLayer._vm.Hidden && !sceneLayer.Interactive, "cancel/finish reveals panel but leaves focus released until next regular tick");
sceneLayer._vm._isBusy = true; sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(sceneOpens == 1 && !sceneLayer._vm.CanOpenDirectActions, "pending reply blocks scene manual command");
sceneLayer._vm._isBusy = false; sceneLayer._vm.Trade.IsOpen = true;
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(sceneOpens == 1, "trade editor does not race manual command");
sceneLayer._vm.Trade.IsOpen = false; InformationManager.Inquiry = true;
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(sceneOpens == 1, "active inquiry cannot be replaced by repeated scene click");
InformationManager.Inquiry = false;
ShoutBehavior.ManualMenu = _ => false;
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(!sceneLayer._vm.Hidden, "failed admission restores scene chrome");
ShoutBehavior.ManualMenu = _ => throw new InvalidOperationException("test menu failure");
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(!sceneLayer._vm.Hidden && DialogueUiRuntime.Logs.Any(x => x.Contains("test menu failure")), "opening exception restores scene chrome without retry");
ShoutBehavior.ManualMenu = finish => { sceneOpens++; finishScene = finish; return true; };
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Mission oldMission = Mission.Current; Mission.Current = new(); finishScene();
Check(sceneLayer._vm.Hidden, "stale finish does not restore another mission's controls");
int opened = sceneOpens; sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(sceneOpens == opened, "old UI layer rejects replacement mission");
Mission.Current = oldMission; sceneLayer._vm._released = true;
sceneLayer._vm.ExecuteDirectActions(); sceneLayer._vm.Flush();
Check(sceneOpens == opened, "disposed scene VM rejects delayed click");
foreach (string language in new[] { "English", "Chinese" })
{
    string languageDir = language == "English" ? "" : "CNs/";
    foreach (var catalog in new[] { ("AF.Module.Conversation", "afconv_ui_strings"), ("AnimusForge.DialogueUI", "afd_ui_strings") })
    {
        string name = languageDir + catalog.Item2 + (language == "English" ? ".xml" : "-zh-CN.xml");
        foreach (var row in XDocument.Load(Path.Combine(args[0], "content/modules", catalog.Item1, "ModuleData/Languages", name)).Descendants("string"))
            TaleWorlds.Localization.TextObject.Catalog[(string)row.Attribute("id")] = (string)row.Attribute("text");
    }
    string label = language == "English" ? "Trigger tags manually" : "手动触发标签";
    Check(vm.DirectActionsButtonText == label && sceneLayer._vm.DirectActionsButtonText == label, "actual native/scene readers resolve manual button in " + language);
}
int shieldChanges = 0;
int modeShieldChanges = 0;
vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(NativeOverlayVM.IsReplyPending)) shieldChanges++; };
vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(NativeOverlayVM.IsNativeContinueBlocked)) modeShieldChanges++; };
Check(!vm.IsReplyPending && vm.IsNativeContinueBlocked, "idle AI mode blocks native continue without pending reply");
host.SetBusy(true);
Check(vm.IsReplyPending && shieldChanges > 0, "busy change publishes shield binding");
int busyChanges = shieldChanges;
host.SetBusy(false);
Check(!vm.IsReplyPending && shieldChanges > busyChanges && vm.IsNativeContinueBlocked, "completion ends busy state but keeps AI continue guard");
host.SetBusy(true); host.SetInputVisible(false);
Check(!vm.IsReplyPending && !vm.IsNativeContinueBlocked, "ordinary mode releases guard despite stale busy flag");
Check(modeShieldChanges > 0, "mode switch publishes permanent shield binding");
host.SetInputVisible(true); Check(vm.IsReplyPending, "AI mode restores pending shield");
host.SetBusy(false);
vm.InputText = "draft\nsecond line";
vm.ShowLogView();
var movie = new FakeMovie();
PresentationRouter.Track(movie, host);
PresentationRouter.ReleaseForTest(movie);
Check(PresentationRouter.Count == 0 && vm.Finalizes == 0, "movie releases ownership but not live wrapper");
Check(NativeUiAdapter.TryWrap(host, out var reloaded) && ReferenceEquals(vm, reloaded), "resource reload reuses wrapper");
Check(vm.InputText == "draft\nsecond line" && vm.Auxiliary.IsOpen, "multiline draft and drawer survive reload");
int transitions = NativeUiAdapter.Transitions;
NativeUiAdapter.Restored(host, temporary: true);
NativeUiAdapter.Restored(host, closed: true);
Check(NativeUiAdapter.Transitions == transitions, "temporary or closed restore cannot reactivate UI");
NativeUiAdapter.Restored(host);
Check(!vm.IsToolbarVisible && !vm.IsCustomAnswerVisible, "restore keeps controls hidden while drawer is open");
Check(vm.IsNativeContinueBlocked, "history drawer keeps underlying AI mode protected");
vm.Auxiliary.IsOpen = false;
NativeUiAdapter.Restored(host);
Check(vm.IsToolbarVisible && vm.IsCustomAnswerVisible && NativeUiAdapter.Transitions == transitions + 2, "drawer closed restore refreshes toolbar and input");
host.SetInputVisible(false);
Check(NativeUiAdapter.TryWrap(host, out reloaded) && switches == 1 && !host.IsCustomAnswerVisible, "manual ordinary mode not overridden on reload");
Check(!NativeUiAdapter.AllowPendingOpening(host), "queued Hero opening cannot override manual ordinary mode");
host.SetInputVisible(true);
vm.InputText = "send\nmultiline";
vm.ExecuteSubmit();
Check(submissions == 1 && submitted == "send multiline", "original single-line submission contract preserved");
NativeUiAdapter.Closed(host);
NativeUiAdapter.Closed(host);
Check(vm.Finalizes == 1 && vm.Auxiliary.Finalizes == 1, "true close finalizes wrapper and drawer once");
Check(!vm.IsNativeContinueBlocked, "disposed wrapper releases native continue guard");
vm.InputText = "late"; vm.ExecuteSubmit();
Check(submissions == 1 && vm.InputText != "late", "disposed wrapper cannot mutate or submit");
Check(NativeUiAdapter.TryWrap(host, out reloaded) && !ReferenceEquals(vm, reloaded), "closed wrapper retired from dictionary");
NativeUiAdapter.Closed(host);

foreach (bool enabled in new[] { true, false })
foreach (bool heroOnly in new[] { true, false })
{
    DialogueUiOptions.AutoEnterAiMode = enabled;
    DialogueUiOptions.AutoEnterAiModeHeroOnly = heroOnly;
    foreach (CharacterObject target in new[] { new CharacterObject { IsHero = true }, new CharacterObject { IsHero = false }, null })
    {
        Campaign.Current.ConversationManager.OneToOneConversationCharacter = target;
        int automaticSwitches = 0;
        AnimusForgeNativeConversationOverlayVM next = null;
        next = new(_ => { }, () => { automaticSwitches++; next.SetInputVisible(!next.IsCustomAnswerVisible); }, null, null, null, null);
        bool expected = enabled && (!heroOnly || target?.IsHero == true);
        Check(NativeUiAdapter.TryWrap(next, out var nextWrapper), "mode rule wraps current conversation");
        Check(next.IsCustomAnswerVisible == expected && automaticSwitches == (expected ? 1 : 0), "auto entry respects master switch and optional Hero filter");
        Check(NativeUiAdapter.AllowPendingOpening(next) == expected, "queued opening follows current mode, not unrestricted default");
        next.SetInputVisible(false);
        Check(!NativeUiAdapter.AllowPendingOpening(next), "ordinary mode remains ordinary after pending opening");
        next.SetInputVisible(true);
        Check(NativeUiAdapter.AllowPendingOpening(next), "manual AI works for Hero/non-Hero even with auto entry off");
        Check(NativeUiAdapter.TryWrap(next, out var nextReload) && ReferenceEquals(nextWrapper, nextReload) && automaticSwitches == (expected ? 1 : 0), "resource reload does not reapply default mode");
        NativeUiAdapter.Closed(next);
    }
}
DialogueUiOptions.AutoEnterAiMode = true;
DialogueUiOptions.AutoEnterAiModeHeroOnly = true;

Campaign.Current.ConversationManager.IsConversationInProgress = false;
var harmony = new HarmonyLib.Harmony();
ShoutUiAdapter.Install(harmony);
Check(harmony.Patches == 4 && !DialogueUiRuntime.Logs.Any(s => s.Contains("skin unavailable")), "real shout installation resolves property-backed epoch");
var behavior = new ShoutBehavior();
Campaign.Current.Behavior = behavior;
var packet = new NpcDataPacket { AgentIndex = 7 };
Mission.Current.Agents.Add(new Agent { Mission = Mission.Current, Index = 7 });
var prefix = typeof(ShoutUiAdapter).GetMethod("DirectInputPrefix", BindingFlags.NonPublic | BindingFlags.Static)!;
prefix.Invoke(null, new object[] { behavior._j17SceneShoutInputController, new object[] { packet }, null });
bool rewriteQueued = false;
var shoutHost = new ShoutTextInputPopupVM("", "", "", "", null, null, allowRewrite: true, onRewrite: () => rewriteQueued = true);
Check(ShoutUiAdapter.TryWrap(shoutHost, out var shout), "property-backed context creates shout wrapper");
var shoutVm = (ShoutPresentationVM)shout;
Check(shoutVm.RefreshAvailability(), "initial context available");
ShoutBehavior.CanRewriteSceneConversationForExternal = true;
shoutVm.RefreshAvailability();
shoutVm.ExecuteToggleHistory();
int historyReads = ShoutBehavior.AuxiliaryHistoryReads;
shoutHost.InputText = "protected draft";
shoutVm.ExecuteRewrite();
Check(!rewriteQueued, "replacement shout wrapper protects the live host draft");
shoutHost.InputText = "";
shoutVm.ExecuteRewrite();
Check(rewriteQueued && shoutHost.InputText == "", "replacement shout rewrite delegates to the original deferred popup command");
shoutHost.RewriteLastRound();
ShoutBehavior.AuxiliaryHistory = new() { "earlier history", "completed gift fact" };
shoutVm.RefreshAvailability();
Check(shoutVm.HistoryText.Contains("earlier history") && !shoutVm.HistoryText.Contains("old player line") && ShoutBehavior.AuxiliaryHistoryReads == historyReads + 1,
    "replacement shout cached history reloads once after successful rewrite");
for (int i = 0; i < 500; i++) shoutVm.RefreshAvailability();
Check(ShoutBehavior.AuxiliaryHistoryReads == historyReads + 1, "idle replacement shout availability never rereads history");
rewriteQueued = false;
shoutHost.InputText = ""; ShoutBehavior.CanRewriteSceneConversationForExternal = true;
behavior.AdvanceEpoch();
Check(!shoutVm.RefreshAvailability(), "new epoch invalidates old target without reflection lookup");
shoutVm.ExecuteRewrite();
Check(!rewriteQueued, "replacement shout rejects queued rewrite after target context changes");
ShoutUiAdapter.Release(shoutHost);
Check(shoutVm.Host == null && shoutVm.Finalizes == 1, "shout close releases its wrapper");

string root = Path.GetFullPath(args[0]);
var xml = XDocument.Load(Path.Combine(root, "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml"));
XElement Id(string id) => xml.Descendants().Single(e => (string)e.Attribute("Id") == id);
XElement Resolve(XElement element, string path)
{
    foreach (string part in path.Split('\\')) element = part == ".." ? element.Parent!.Parent! : element.Element("Children")!.Elements().Single(e => (string)e.Attribute("Id") == part);
    return element;
}
var scroll = Id("AFDialogueInputScroll");
Check(Resolve(scroll, (string)scroll.Attribute("ClipRect")) == Id("AFDialogueInputClip"), "input clip path resolves");
Check(Resolve(scroll, (string)scroll.Attribute("InnerPanel")) == Id("AFDialogueInputEditor"), "input content path resolves");
Check(Resolve(scroll, (string)scroll.Attribute("VerticalScrollbar")) == Id("AFDialogueInputBar"), "scrollbar path resolves");
Check((string)Id("AFDialogueInputClip").Attribute("ClipContents") == "true" && (string)Id("AFDialogueInputEditor").Attribute("AutoScrollToCaret") == "true", "long input clips and enables caret scrolling");
AudienceDocketReplay.Run(root, Check);
RewriteUiReplay.Run(root, Check);
Console.WriteLine($"PASS: {checks} production-linked UI lifecycle, epoch-reader, audience rolling and XML checks; engine/patch dispatch/drawer are doubles, not visual acceptance.");
class FakeMovie : IGauntletMovie { }
