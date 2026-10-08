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
int switches = 0, submissions = 0;
string submitted = null;
AnimusForgeNativeConversationOverlayVM host = null;
host = new(s => { submissions++; submitted = s; }, () => { switches++; host.SetInputVisible(!host.IsCustomAnswerVisible); }, null, null, null, null);
Check(NativeUiAdapter.TryWrap(host, out var wrapper), "first native wrapping succeeds");
var vm = (NativeOverlayVM)wrapper;
Check(switches == 1 && vm.IsCustomAnswerVisible, "captured default applied once");
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
var shoutHost = new ShoutTextInputPopupVM();
Check(ShoutUiAdapter.TryWrap(shoutHost, out var shout), "property-backed context creates shout wrapper");
var shoutVm = (ShoutPresentationVM)shout;
Check(shoutVm.RefreshAvailability(), "initial context available");
behavior.AdvanceEpoch();
Check(!shoutVm.RefreshAvailability(), "new epoch invalidates old target without reflection lookup");
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
Console.WriteLine($"PASS: {checks} production-linked UI lifecycle, epoch-reader and XML checks; engine/patch dispatch/drawer are doubles, not visual acceptance.");
class FakeMovie : IGauntletMovie { }
