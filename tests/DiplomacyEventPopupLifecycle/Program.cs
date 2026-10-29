using AnimusForge;
using AnimusForge.Refactor.Contracts;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

int checks = 0, acknowledged = 0;
MapScreen map = null;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
void Reset()
{
    DiplomacyEventPopup.CloseForCampaignEnd();
    SaveRuntimeGuard.Generation = 1;
    Campaign.Current = new(); Game.Current = new(); Mission.Current = null;
    Game.Current.GameStateManager.ActiveState = new MapState();
    map = new(); map.AddLayer(map.SceneLayer);
    ScreenManager.TopScreen = map; ScreenManager.FocusedLayer = map.SceneLayer;
    InformationManager.Inquiry = false;
    CourierLetterReplyPopup.IsOpen = CourierLetterInputPopup.IsOpen = DiplomacyPresentationBridge.IsOpen = false;
    ShoutTextInputPopup.IsOpen = AnimusForgeNativeConversationOverlay.IsOpen = false;
    DevHistoryEditPopup.IsOpen = CourierDeliveryBehavior.IsCourierInputOpen = false;
    Input.Escape = false; GauntletLayer.Last = null;
    DiplomacyEventSprites.Ready = true; acknowledged = 0;
}
bool Show() => DiplomacyEventPopup.TryShow(new DiplomacyEventPresentation("notice", 1, DiplomacyEventStage.Started,
    "Title", "Body", null, null), out _, () => acknowledged++);
void Refused(Action context, string name)
{
    Reset(); context(); int previousPauses = Game.Current.GameStateManager.Requests.Count;
    Check(!Show(), name);
    Check(acknowledged == 0 && !DiplomacyEventPopup.IsOpen && Game.Current.GameStateManager.Requests.Count == previousPauses,
        name + " leaves notice unread and does not pause");
}
Refused(() => Campaign.Current.ConversationManager.IsConversationInProgress = true, "native conversation");
Refused(() => Campaign.Current.ConversationManager.IsConversationFlowActive = true, "conversation transition");
Refused(() => ShoutTextInputPopup.IsOpen = true, "shout editor");
Refused(() => AnimusForgeNativeConversationOverlay.IsOpen = true, "free conversation overlay");
Refused(() => Mission.Current = new(), "mission");
Refused(() => Campaign.Current = null, "no campaign");
Refused(() => Game.Current.GameStateManager.ActiveState = new object(), "non-map state");
Refused(() => ScreenManager.TopScreen = new ScreenBase(), "non-map screen");
Refused(() => map.IsReady = false, "map loading");
Refused(() => InformationManager.Inquiry = true, "inquiry");
Refused(() => CourierLetterReplyPopup.IsOpen = true, "courier reply");
Refused(() => CourierLetterInputPopup.IsOpen = true, "courier editor");
Refused(() => DiplomacyPresentationBridge.IsOpen = true, "declaration editor");
Refused(() => map.IsFinalized = true, "finalized map");
Refused(() => ScreenManager.FocusedLayer = new GauntletLayer("global", 5000, false), "global modal outside map screen");

void MapPanel(Action context, string name)
{
    Reset(); context();
    var parent = new GauntletLayer(name, 4400, false);
    parent.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
    map.AddLayer(parent); ScreenManager.FocusedLayer = parent;
    object pause = new(); Game.Current.GameStateManager.RegisterActiveStateDisableRequest(pause);
    Check(Show(), name + " opens over map UI despite its pause/focus");
    Check(!parent.IsActive && !parent.IsFocusLayer && parent.InputUsageMask == InputUsageMask.None,
        name + " temporarily leases parent input");
    Check(Game.Current.GameStateManager.Requests.Count == 2, name + " retains original pause owner");
    DiplomacyEventPopup.OnApplicationTick();
    Check(DiplomacyEventPopup.IsOpen, name + " stays open while parent map flags remain set");
    Check(!EncyclopediaInputGuard.EncyclopediaDataOnTickPrefix(), name + " blocks underlying encyclopedia hotkeys");
    var layer = GauntletLayer.Last; layer.Input.Escape = true;
    DiplomacyEventPopup.OnApplicationTick();
    Check(DiplomacyEventPopup.IsOpen && !EncyclopediaInputGuard.EncyclopediaDataOnTickPrefix(),
        name + " consumes Escape release before restoring parent");
    layer.Input.Escape = false; DiplomacyEventPopup.OnApplicationTick();
    Check(!DiplomacyEventPopup.IsOpen && acknowledged == 1, name + " Escape closes once");
    Check(parent.IsActive && parent.IsFocusLayer && parent.InputUsageMask == InputUsageMask.All
        && parent.InputRestrictions.MouseVisibility && ScreenManager.FocusedLayer == parent,
        name + " restores parent focus, activation and input");
    Check(map.HasLayer(parent) && map.Layers.Count == 2
        && Game.Current.GameStateManager.Requests.SetEquals(new[] { pause }),
        name + " preserves original panel and pause");
    Check(EncyclopediaInputGuard.EncyclopediaDataOnTickPrefix(), name + " resumes encyclopedia after close");
    DiplomacyEventPopup.OnApplicationTick(); Check(acknowledged == 1, name + " cannot acknowledge twice");
}
foreach (var flag in typeof(MapScreen).GetFields().Where(x => x.DeclaringType == typeof(MapScreen)
    && x.FieldType == typeof(bool) && x.Name != "IsReady"))
    MapPanel(() => flag.SetValue(map, true), flag.Name);
MapPanel(() => map.EncyclopediaScreenManager.IsEncyclopediaOpen = true, "encyclopedia");
Check(map.EncyclopediaScreenManager.IsEncyclopediaOpen, "Escape closes only event, preserving encyclopedia page");
MapPanel(() => { map.IsInMenu = true; map.EncyclopediaScreenManager.IsEncyclopediaOpen = true; }, "encyclopedia during settlement wait");
Check(map.IsInMenu && map.EncyclopediaScreenManager.IsEncyclopediaOpen, "nested map menu and encyclopedia survive event");
MapPanel(() => map.SceneLayer.IsActive = false, "map scene input suspended by a panel");
Reset(); ScreenManager.FocusedLayer = null;
Check(Show(), "ready map without current focus can show");
Reset(); Game.Current.GameStateManager.RegisterActiveStateDisableRequest(new object());
Check(Show(), "pause alone does not change map eligibility");
Reset(); DevHistoryEditPopup.IsOpen = true;
Check(!EncyclopediaInputGuard.EncyclopediaDataOnTickPrefix(), "existing history editor guard preserved");
DevHistoryEditPopup.IsOpen = false; CourierDeliveryBehavior.IsCourierInputOpen = true;
Check(!EncyclopediaInputGuard.EncyclopediaDataOnTickPrefix(), "existing courier guard preserved");

Reset(); Check(Show(), "idle map opens");
Check(Game.Current.GameStateManager.Requests.Count == 1 && map.Layers.Count == 2, "open owns pause and one layer");
GauntletLayer.Last.Vm.ExecuteClose(); ScreenManager.FocusedLayer = new ScreenLayer();
DiplomacyEventPopup.OnApplicationTick();
Check(acknowledged == 1 && !DiplomacyEventPopup.IsOpen, "button closes even after losing focus");
Check(Game.Current.GameStateManager.Requests.Count == 0 && map.Layers.Count == 1, "button releases layer and pause");
DiplomacyEventPopup.OnApplicationTick(); Check(acknowledged == 1, "acknowledge once");

foreach (bool globalKey in new[] { false, true })
{
    Reset(); Check(Show(), "open for Escape");
    var layer = GauntletLayer.Last;
    if (globalKey) Input.Escape = true; else layer.Input.Escape = true;
    DiplomacyEventPopup.OnApplicationTick();
    Check(DiplomacyEventPopup.IsOpen && Game.Current.GameStateManager.Requests.Count == 1,
        "Escape retains ownership for release frame");
    Input.Escape = layer.Input.Escape = false;
    // A focus change between the key release and the deferred close must not strand the notice.
    ScreenManager.FocusedLayer = new ScreenLayer();
    DiplomacyEventPopup.OnApplicationTick();
    Check(!DiplomacyEventPopup.IsOpen && acknowledged == 1 && Game.Current.GameStateManager.Requests.Count == 0,
        "Escape closes and acknowledges without a second key press");
}
Reset(); Show(); GauntletLayer.Last.Input.Exit = true; DiplomacyEventPopup.OnApplicationTick();
GauntletLayer.Last.Input.Exit = false; DiplomacyEventPopup.OnApplicationTick();
Check(acknowledged == 1, "Exit hotkey closes");

foreach (var interruption in new (string Name, Action Change)[] {
    ("conversation begins", () => Campaign.Current.ConversationManager.IsConversationInProgress = true),
    ("scene begins", () => Mission.Current = new()),
    ("screen changes", () => ScreenManager.TopScreen = new ScreenBase()),
    ("inquiry opens", () => InformationManager.Inquiry = true),
    ("layer deactivated", () => GauntletLayer.Last.IsActive = false),
    ("other modal takes focus", () => ScreenManager.FocusedLayer = new GauntletLayer("other", 5000, false)) })
{
    Reset(); Check(Show(), interruption.Name + " initial open");
    interruption.Change(); DiplomacyEventPopup.OnApplicationTick();
    Check(!DiplomacyEventPopup.IsOpen && acknowledged == 0 && Game.Current.GameStateManager.Requests.Count == 0,
        interruption.Name + " withdraws without acknowledgement");
    Campaign.Current.ConversationManager.IsConversationInProgress = false; Mission.Current = null;
    map.IsInMenu = false; InformationManager.Inquiry = false;
    ScreenManager.TopScreen = map; ScreenManager.FocusedLayer = map.SceneLayer;
    Check(Show(), interruption.Name + " pending notice can reopen on map");
    GauntletLayer.Last.Vm.ExecuteClose(); DiplomacyEventPopup.OnApplicationTick();
    Check(acknowledged == 1, interruption.Name + " acknowledged only after reading");
}
Reset(); Show(); map.IsInMenu = true; DiplomacyEventPopup.OnApplicationTick();
Check(DiplomacyEventPopup.IsOpen && acknowledged == 0, "opening a map menu does not discard visible notification");
Reset();
var menuParent = new GauntletLayer("settlement-menu", 200, false);
map.AddLayer(menuParent); ScreenManager.FocusedLayer = menuParent; Show();
var conversationLayer = new GauntletLayer("conversation", 5000, false);
map.AddLayer(conversationLayer); ScreenManager.FocusedLayer = conversationLayer;
Campaign.Current.ConversationManager.IsConversationInProgress = true;
DiplomacyEventPopup.OnApplicationTick();
Check(!DiplomacyEventPopup.IsOpen && acknowledged == 0, "dialogue interrupts event above a menu without consuming it");
Check(ScreenManager.FocusedLayer == conversationLayer, "restoring menu must not steal the new dialogue focus");
foreach (bool nullFocus in new[] { true, false })
{
    Reset(); Show(); ScreenManager.FocusedLayer = nullFocus ? null : map.SceneLayer;
    DiplomacyEventPopup.OnApplicationTick();
    Check(DiplomacyEventPopup.IsOpen && ScreenManager.FocusedLayer == GauntletLayer.Last, "recover unowned/map focus");
}
Reset(); Show(); SaveRuntimeGuard.Generation++;
DiplomacyEventPopup.OnApplicationTick();
Check(acknowledged == 0 && !DiplomacyEventPopup.IsOpen && Game.Current.GameStateManager.Requests.Count == 0,
    "retired campaign releases pause without acknowledging");
Reset(); Show(); DiplomacyEventPopup.CloseForCampaignEnd(); DiplomacyEventPopup.CloseForCampaignEnd();
Check(acknowledged == 0 && map.Layers.Count == 1 && Game.Current.GameStateManager.Requests.Count == 0,
    "campaign shutdown cleanup is idempotent");
Reset(); DiplomacyEventSprites.Ready = false;
Check(!Show() && DiplomacyEventPopup.IsPreparing && acknowledged == 0, "image preparation leaves notice pending");
Campaign.Current.ConversationManager.IsConversationInProgress = true; DiplomacyEventSprites.Ready = true;
Check(!Show() && !DiplomacyEventPopup.IsPreparing, "conversation beginning during preparation prevents popup");
Campaign.Current.ConversationManager.IsConversationInProgress = false; Check(Show(), "prepared notice waits for safe map");

Reset(); var terminal = new ScreenBase(); var terminalLayer = new GauntletLayer("terminal", 4000, false);
terminal.AddLayer(terminalLayer); ScreenManager.TopScreen = terminal; ScreenManager.FocusedLayer = terminalLayer;
Check(DiplomacyEventPopup.TryShowPreview(new DiplomacyEventPresentation("preview", 1, DiplomacyEventStage.Ended,
    "Preview", "Body", null, null), out _), "explicit terminal preview remains available");
Check(!terminalLayer.IsActive, "preview leases parent input");
GauntletLayer.Last.Vm.ExecuteClose(); DiplomacyEventPopup.OnApplicationTick();
Check(terminalLayer.IsActive && ScreenManager.FocusedLayer == terminalLayer && terminal.Layers.Count == 1,
    "preview restores terminal input");
Console.WriteLine($"PASS {checks} production popup/VM/input-lease lifecycle checks; native screen, input and rendering boundaries are fixtures, no live game.");
