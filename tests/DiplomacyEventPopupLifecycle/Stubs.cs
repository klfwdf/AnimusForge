using AnimusForge.Refactor.Contracts;
using TaleWorlds.ScreenSystem;

namespace AnimusForge
{
    internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static bool IsCurrentGeneration(long value) => value == Generation; }
    internal static class Logger { internal static void Log(string category, string text) { } }
    internal static class DiplomacyEventSprites
    {
        internal static bool Ready = true;
        internal static bool Prepare(string id) => Ready;
        internal static string Load(string id) => id;
        internal static void Clear() { }
    }
    internal static class DiplomacyEventIllustrations { internal static string Select(DiplomacyEventPresentation data, IReadOnlyList<string> recent) => "illustration"; }
    internal static class DiplomacyEventPreview { internal static void ProcessPending() { } }
    internal static class CourierLetterReplyPopup { internal static bool IsOpen; }
    internal static class CourierLetterInputPopup { internal static bool IsOpen; }
    internal static class DiplomacyPresentationBridge { internal static bool IsOpen; }
    internal static class ShoutTextInputPopup { internal static bool IsOpen; }
    internal static class AnimusForgeNativeConversationOverlay { internal static bool IsOpen; }
}
namespace TaleWorlds.Library
{
    public class ViewModel { public virtual void OnFinalize() { } }
    public class DataSourcePropertyAttribute : Attribute { }
    public class MBBindingList<T> : List<T> { }
    internal static class InformationManager { internal static bool Inquiry; internal static bool IsAnyInquiryActive() => Inquiry; }
}
namespace TaleWorlds.Localization { internal class TextObject { readonly string _value; internal TextObject(string value) => _value = value; public override string ToString() => _value; } }
namespace TaleWorlds.Core
{
    internal sealed class Game { internal static Game Current; internal GameStateManager GameStateManager = new(); }
    internal sealed class GameStateManager
    {
        internal object ActiveState;
        internal HashSet<object> Requests = new();
        internal bool ActiveStateDisabledByUser => Requests.Count != 0;
        internal void RegisterActiveStateDisableRequest(object owner) => Requests.Add(owner);
        internal void UnregisterActiveStateDisableRequest(object owner) => Requests.Remove(owner);
    }
}
namespace TaleWorlds.CampaignSystem
{
    internal sealed class Campaign { internal static Campaign Current; internal ConversationManager ConversationManager = new(); }
    internal sealed class ConversationManager { internal bool IsConversationInProgress, IsConversationFlowActive; }
}
namespace TaleWorlds.CampaignSystem.GameState { internal sealed class MapState { } }
namespace TaleWorlds.MountAndBlade { internal sealed class Mission { internal static Mission Current; } }
namespace TaleWorlds.InputSystem
{
    public enum InputUsageMask { All, None }
    public enum InputKey { Escape }
    internal static class HotKeyManager { internal static object GetCategory(string id) => null; }
    internal static class Input { internal static bool Escape; internal static bool IsKeyReleased(InputKey key) => Escape; }
    public class InputContext
    {
        public bool Escape, Exit;
        public void RegisterHotKeyCategory(object category) { }
        public bool IsHotKeyReleased(string key) => Exit;
        public bool IsKeyReleased(InputKey key) => Escape;
    }
    public class InputRestrictions
    {
        public bool MouseVisibility;
        public InputUsageMask Mask;
        public void SetInputRestrictions(bool mouse, InputUsageMask mask) { MouseVisibility = mouse; Mask = mask; }
        public void ResetInputRestrictions() { MouseVisibility = false; Mask = InputUsageMask.None; }
    }
}
namespace TaleWorlds.ScreenSystem
{
    public class ScreenBase
    {
        public bool IsFinalized;
        public List<ScreenLayer> Layers = new();
        public void AddLayer(ScreenLayer layer) => Layers.Add(layer);
        public void RemoveLayer(ScreenLayer layer) => Layers.Remove(layer);
        public bool HasLayer(ScreenLayer layer) => Layers.Contains(layer);
    }
    public class ScreenLayer
    {
        public bool IsFinalized, IsActive = true, IsFocusLayer = true, LastActiveState = true;
        public TaleWorlds.InputSystem.InputRestrictions InputRestrictions = new();
        public TaleWorlds.InputSystem.InputUsageMask InputUsageMask => InputRestrictions.Mask;
        public TaleWorlds.InputSystem.InputContext Input = new();
        public static event Action<ScreenLayer> OnLayerActiveStateChanged;
        internal void Suspend(bool suspend) { IsActive = !suspend; OnLayerActiveStateChanged?.Invoke(this); }
    }
    public static class ScreenManager
    {
        public static ScreenBase TopScreen;
        public static ScreenLayer FocusedLayer;
        public static void TrySetFocus(ScreenLayer layer) { if (layer.IsActive && layer.IsFocusLayer) FocusedLayer = layer; }
        public static void TryLoseFocus(ScreenLayer layer) { if (FocusedLayer == layer) FocusedLayer = null; }
        public static void SetSuspendLayer(ScreenLayer layer, bool isSuspended) => layer.Suspend(isSuspended);
    }
}
namespace SandBox.View.Map
{
    internal class MapScreen : ScreenBase
    {
        internal ScreenLayer SceneLayer = new();
        public bool IsReady = true;
        public bool IsInMenu, IsEscapeMenuOpened, IsInCampaignOptions, IsInBattleSimulation, IsInTownManagement,
            IsInHideoutTroopManage, IsInArmyManagement, IsInRecruitment, IsMarriageOfferPopupActive,
            IsHeirSelectionPopupActive, IsMapCheatsActive, IsMapIncidentActive, IsOverlayContextMenuEnabled;
        internal Encyclopedia EncyclopediaScreenManager = new();
    }
    internal class Encyclopedia { internal bool IsEncyclopediaOpen; }
}
namespace TaleWorlds.Engine.GauntletUI
{
    public class GauntletMovieIdentifier { }
    public class GauntletLayer : ScreenLayer
    {
        internal static GauntletLayer Last;
        internal AnimusForge.DiplomacyEventPopupVM Vm;
        public GauntletLayer(string name, int order, bool focus) { }
        public GauntletMovieIdentifier LoadMovie(string id, object vm) { Last = this; Vm = (AnimusForge.DiplomacyEventPopupVM)vm; return new(); }
        public void ReleaseMovie(GauntletMovieIdentifier movie) { }
    }
}
