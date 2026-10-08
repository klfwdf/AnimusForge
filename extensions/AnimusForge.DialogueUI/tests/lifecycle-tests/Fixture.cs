using System.ComponentModel;
using System.Reflection;

namespace TaleWorlds.Library
{
    public interface IViewModel { }
    [AttributeUsage(AttributeTargets.Property)] public class DataSourcePropertyAttribute : Attribute { }
    public class PropertyChangedWithValueEventArgs : EventArgs { public string PropertyName; }
    public class PropertyChangedWithBoolValueEventArgs : PropertyChangedWithValueEventArgs { }
    public class PropertyChangedWithIntValueEventArgs : PropertyChangedWithValueEventArgs { }
    public class PropertyChangedWithFloatValueEventArgs : PropertyChangedWithValueEventArgs { }
    public class ViewModel : IViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<PropertyChangedWithValueEventArgs> PropertyChangedWithValue;
        public event EventHandler<PropertyChangedWithBoolValueEventArgs> PropertyChangedWithBoolValue;
        public event EventHandler<PropertyChangedWithIntValueEventArgs> PropertyChangedWithIntValue;
        public event EventHandler<PropertyChangedWithFloatValueEventArgs> PropertyChangedWithFloatValue;
        public int Finalizes;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public void OnPropertyChangedWithValue<T>(T value, string name) => PropertyChangedWithValue?.Invoke(this, new() { PropertyName = name });
        public virtual void OnFinalize() { Finalizes++; }
    }
}
namespace TaleWorlds.CampaignSystem
{
    public class Campaign { public static Campaign Current = new(); public ConversationManager ConversationManager = new(); }
    public class CharacterObject { public bool IsHero; }
    public class ConversationManager { public bool IsConversationInProgress; public CharacterObject OneToOneConversationCharacter; public void EndConversation() { IsConversationInProgress = false; } }
}
namespace TaleWorlds.MountAndBlade
{
    public class Mission { public static Mission Current; public List<Agent> Agents = new(); }
    public class Agent { public Mission Mission; public int Index; public bool IsHuman = true; public bool Active = true; public bool IsActive() => Active; }
}
namespace HarmonyLib
{
    // Patch registration/engine calls are doubles; property-reader expression execution is real.
    public class Harmony { public int Patches; public void Patch(MethodInfo original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod finalizer = null) { Patches++; } }
    public class HarmonyMethod { public HarmonyMethod(Type type, string name) { } }
    public static class AccessTools
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public static Type TypeByName(string name) => typeof(AccessTools).Assembly.GetType(name);
        public static FieldInfo Field(Type type, string name) => type.GetField(name, Flags);
        public static PropertyInfo Property(Type type, string name) => type.GetProperty(name, Flags);
        public static MethodInfo Method(Type type, string name, Type[] parameters) => type.GetMethod(name, Flags, null, parameters, null);
    }
}
namespace AnimusForge
{
    public class NpcDataPacket { public int AgentIndex; }
    public class ShoutBehavior
    {
        public static int SceneIllustrationVersionForExternal => 0;
        private object _shoutTradeTargetNpc, _activeShoutTargetingContext = new();
        private TaleWorlds.MountAndBlade.Agent _shoutTradeTargetAgentSnapshot;
        private bool _shoutTradeActionOnly;
        private int _sceneConversationEpoch { get; set; }
        public void AdvanceEpoch() { _sceneConversationEpoch++; }
        private void OpenShoutTextInput(NpcDataPacket packet, string title, string subtitle) { }
        private void ShowShoutTradeChatInput() { }
        private static int GetCurrentSceneHistorySessionIdForExternal() => 1;
        private static List<string> GetAuxiliarySceneDialogueHistoryLinesForExternal(int agent, int limit) => new() { "history" };
    }
    public class ShoutTextInputPopup { private object _dataSource; private void Close(bool silent) { } }
    public class ShoutTextInputPopupVM : TaleWorlds.Library.ViewModel { public string SubtitleText => ""; public bool IsIllustrationVisible => false; public bool CanIllustrate => false; public string IllustrationButtonText => ""; public void ExecuteIllustrate() { } public void ExecuteOpenGallery() { } public void ExecuteSubmit() { } public void ExecuteCancel() { } }
    public static class AnimusForgeNativeConversationOverlay { public static void CloseActive() { } }
    public static class LordEncounterBehavior { public static void PreparePlayerRequestedNativeConversationLeave() { } }
}
namespace AnimusForge.DialogueUI
{
    public static class DialogueUiOptions { public static bool AutoEnterAiMode = true, AutoEnterAiModeHeroOnly = true; }
    public static class DialogueUiRuntime { public static bool Enabled = true; public static List<string> Logs = new(); public static void Log(string text) => Logs.Add(text); }
    public interface IGauntletMovie { }
    internal static partial class PresentationRouter
    {
        private static readonly Dictionary<IGauntletMovie, TaleWorlds.Library.IViewModel> OwnedMovies = new();
        internal static void Track(IGauntletMovie movie, TaleWorlds.Library.IViewModel source) => OwnedMovies.Add(movie, source);
        internal static void ReleaseForTest(IGauntletMovie movie) => ReleaseOwned(movie);
        internal static int Count => OwnedMovies.Count;
    }
}
namespace AnimusForge.DialogueUI.Native
{
    public static class IllustratorBridge { public static bool IsAvailable() => true; public static void Invoke() { } }
    public class DialogueAuxiliaryVM : TaleWorlds.Library.ViewModel
    {
        private readonly NativeOverlayVM _owner;
        public bool IsOpen, Suspended;
        public bool IsVisible => IsOpen && !Suspended;
        public DialogueAuxiliaryVM(NativeOverlayVM owner) { _owner = owner; }
        public void Open(bool history) { IsOpen = true; _owner.AuxiliaryStateChanged(); }
        public void RefreshInteraction() { }
    }
    public static partial class NativeUiAdapter
    {
        private static readonly Dictionary<AnimusForgeNativeConversationOverlayVM, NativeOverlayVM> Wrappers = new();
        private static bool _installed = true;
        private static Layout _overlay;
        public static int Transitions;
        private class Layout { public AnimusForgeNativeConversationOverlayVM Original; public void Dispose() { } }
        internal static void AuxiliaryStateChanged(NativeOverlayVM vm) { Transitions++; }
        public static void Closed(AnimusForgeNativeConversationOverlayVM host) => OverlayClosed(host);
        public static void Restored(AnimusForgeNativeConversationOverlayVM host, bool temporary = false, bool closed = false) => OverlayRestored(host, temporary, closed);
        public static bool AllowPendingOpening(AnimusForgeNativeConversationOverlayVM host) => NpcOpeningPrefix(host);
    }
}
