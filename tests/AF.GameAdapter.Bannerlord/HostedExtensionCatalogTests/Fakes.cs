using System;
using System.Collections.Generic;
internal static class TestLeaves
{
    internal static string Failure;
    internal static bool UiResources = true;
    internal static List<string> Calls = new();
    internal static void Step(string name) { Calls.Add(name); if (Failure == name) throw new InvalidOperationException("private-path-secret"); }
}
namespace TaleWorlds.Core { public interface IGameStarter {} public class Game {} }
namespace TaleWorlds.MountAndBlade
{
    public class MBSubModuleBase
    {
        protected virtual void OnSubModuleLoad() {} protected virtual void OnSubModuleUnloaded() {}
        protected virtual void OnApplicationTick(float dt) {} protected virtual void InitializeGameStarter(TaleWorlds.Core.Game game,TaleWorlds.Core.IGameStarter starter) {}
        protected virtual void OnBeforeInitialModuleScreenSetAsRoot() {}
    }
}
namespace TaleWorlds.CampaignSystem
{
    public class CampaignGameStarter : TaleWorlds.Core.IGameStarter
    {
        public void AddBehavior(object behavior) => TestLeaves.Step("register."+behavior.GetType().Name);
        public void AddModel(object model) => TestLeaves.Step("register."+model.GetType().Name);
    }
}
namespace TaleWorlds.Library { public static class Debug { public static void Print(string s) {} } }
namespace TaleWorlds.Engine.GauntletUI { internal class Unused {} }
namespace HarmonyLib
{
    public class Harmony
    {
        public Harmony(string id) { TestLeaves.Step("harmony."+id); }
        public void UnpatchAll(string id) => TestLeaves.Step("unpatch."+id);
    }
}
namespace AnimusForge
{
    // Registration only checks these existing service leaves for availability.
    // The real TeamModuleRegistration and directory lifecycle remain source-linked.
    internal static class DiplomacyModuleServices
    {
        internal static object Conversation = new();
        internal static object World = new();
        internal static object Policy = new();
    }
    internal static class Logger { internal static void Log(string name,string message) {} }
    internal static class ShoutBehavior { internal static Func<bool> ScenePresentationSessionHook; internal static Func<bool> ScenePresentationBlocksHotkeysHook; }
    internal static class NoblePrisonerEscortBehavior { internal static object GetEscortedHeroesForExecution() => null; }
    internal static class ExecutionAddressLlm { internal static void Register()=>TestLeaves.Step("vengeance.address"); internal static void Unregister() {} }
}
namespace AnimusForge.Refactor.Modules
{
    internal static class TeamModuleServices { internal static object Policy=new(); internal static object Gathering=new(); internal static object Siege=new(); }
    internal static class CampaignComposition { internal static void Register(TaleWorlds.Core.IGameStarter s) {} }
}
namespace AnimusForge.Refactor.Runtime
{
    internal static class FeatureBridgeRuntime
    {
        internal static bool Disabled;
        internal static AnimusForge.Refactor.Contracts.FeatureBridgeDecision Evaluate(string id,int version)
            => new(id,Disabled ? AnimusForge.Refactor.Contracts.FeatureBridgeDecisionStatus.Disabled : AnimusForge.Refactor.Contracts.FeatureBridgeDecisionStatus.Allowed, AnimusForge.Refactor.Contracts.FeatureBridgeFallback.NoOp,Disabled?"bridge.disabled":"bridge.allowed");
    }
}
namespace AnimusForge.Illustrator { internal sealed class IllustratorCampaignBehavior {} }
namespace AnimusForge.Illustrator.Core
{
    internal static class IllustratorRuntime { internal static void Initialize()=>TestLeaves.Step("illustrator.start"); internal static void Tick(){} internal static void Shutdown(){} }
}
namespace AnimusForge.Illustrator.Engine { internal static class ScreenCaptureHelper { internal static void ObservePanoramaFrame(float dt){} } }
namespace AnimusForge.Illustrator.UI.Gallery { internal class Unused {} }
namespace AnimusForge.Illustrator.UI.Overlays { internal static class IllustrationCardPopup { internal static void ClearConversationSessionCache(){} } }
namespace AnimusForge.Illustrator.UI.Patches
{
    internal static class WeeklyReportPopupIllustrationPatch { internal static void Patch(HarmonyLib.Harmony h)=>TestLeaves.Step("illustrator.patch"); internal static void CloseOverlay(){} }
    internal static class EncyclopediaHeroIllustrationPatch { internal static void EnsurePatched(HarmonyLib.Harmony h){} internal static void Reset(){} }
    internal static class ConversationIllustrationPatch { internal static void EnsurePatched(HarmonyLib.Harmony h){} internal static void Reset(){} }
}
namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiRuntime
    {
        internal static bool Enabled;
        internal static void Initialize(){TestLeaves.Step("ui.start");Enabled=TestLeaves.UiResources;}
        internal static void Disable()=>Enabled=false;
        internal static void Log(string s){} internal static void LogOnce(string k,string v){}
    }
    internal static class DialogueUiSprites { internal static void Install(HarmonyLib.Harmony h)=>TestLeaves.Step("ui.sprites"); internal static void Shutdown(){} }
    internal static class PresentationRouter { internal static void Install(HarmonyLib.Harmony h)=>TestLeaves.Step("ui.presentation"); internal static void Shutdown()=>TestLeaves.Step("ui.shutdown"); }
}
namespace AnimusForge.DialogueUI.Native { internal static class NativeUiAdapter { internal static void Install(HarmonyLib.Harmony h){} internal static void Tick(float dt){} } }
namespace AnimusForge.DialogueUI.Shout { internal static class ShoutUiAdapter { internal static void Install(HarmonyLib.Harmony h){} internal static void Tick(){} } }
namespace AnimusForge.DialogueUI.Scene
{
    internal static class SceneWheel { internal static bool IsOpen; internal static void Install(HarmonyLib.Harmony h)=>TestLeaves.Step("ui.wheel"); internal static void Tick(){} internal static void Shutdown(){} }
    internal static class SceneSessionPanel { internal static bool IsAvailable=true; internal static void Install(HarmonyLib.Harmony h){} internal static void Tick(float dt){} internal static void Shutdown(){} }
}
namespace AnimusForge.CoupSystem
{
    internal static class SettlementEntryTroopSelectionBehavior { internal static bool IsAvailable=true; internal static void Register(HarmonyLib.Harmony h)=>TestLeaves.Step("coup.entry"); internal static void Reset(){} }
    internal static class CoupGuards { internal static bool MissionProtectionAvailable=true; internal static void Register(HarmonyLib.Harmony h)=>TestLeaves.Step("coup.guards"); internal static void Reset(){} }
    internal class CoupCampaignBehavior { internal static CoupCampaignBehavior Instance; internal void OnEngineTick(float dt){} }
    internal class CoupCaptivityBehavior {}
    internal class CoupRebellionBridge { internal static bool IsAvailable=true; internal static CoupRebellionBridge Instance; internal static void Initialize()=>TestLeaves.Step("coup.bridge"); internal void OnEngineTick(float dt){} }
}
namespace RichExecutions.Core
{
    internal static class VengeanceIntegration
    {
        internal static bool IsEmbeddedHostActive; internal static bool Enabled=true;
        internal static bool TryClaimEmbeddedHost(){ if(!Enabled||IsEmbeddedHostActive)return false;return IsEmbeddedHostActive=true; }
        internal static void ReleaseEmbeddedHost()=>IsEmbeddedHostActive=false;
    }
    internal static class RichExecutionApi { internal static object Service,Methods,Charges; internal static void InitializeDefaults()=>TestLeaves.Step("vengeance.defaults"); }
}
namespace RichExecutions.Scene { internal static class ExecutionContinuation { internal static Func<object> EscortedHeroesProvider; } }
namespace RichExecutions.Customization { internal static class ExecutionSitePresetStore { internal static void EnsureBuiltInPresetsForAllMethods()=>TestLeaves.Step("vengeance.presets"); } }
namespace RichExecutions.Diagnostics { internal static class RexLog { internal static void Info(string s){} internal static void Error(string s,Exception e){} } }
namespace RichExecutions.Campaign
{
    internal class ScopedExecutionRelationModel {}
    internal class RichExecutionCampaignBehavior { internal RichExecutionCampaignBehavior(object service,object methods,object charges){} }
}
namespace TaleWorlds.CampaignSystem.GameMenus { internal class Unused {} }
