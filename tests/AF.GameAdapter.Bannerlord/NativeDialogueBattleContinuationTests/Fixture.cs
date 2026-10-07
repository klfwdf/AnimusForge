using System.Reflection;
using AnimusForge;
using TaleWorlds.CampaignSystem;

namespace TaleWorlds.CampaignSystem
{
    public sealed class Hero { }
    public enum ConversationContext { Default, PartyEncounter }
    public sealed class Campaign
    {
        public static Campaign Current;
        public Conversation.ConversationManager ConversationManager = new();
        public ConversationContext CurrentConversationContext = ConversationContext.PartyEncounter;
        public MenuContext CurrentMenuContext = new();
    }
    public sealed class MenuContext { public GameMenus.GameMenu GameMenu = new(); }
}
namespace TaleWorlds.CampaignSystem.Party { public sealed class PartyBase { public bool IsMobile = true; } }
namespace TaleWorlds.CampaignSystem.Conversation
{
    public struct ConversationSentenceOption { public string Id; }
    public sealed class ConversationManager
    {
        public bool IsConversationInProgress;
        public void ProcessSentence(ConversationSentenceOption option) => NativeDialogueBattleContinuation.SentenceProcessed(this, option, true);
        public void EndConversation()
        {
            IsConversationInProgress = false;
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            NativeDialogueBattleContinuation.ConversationEnded(this, true);
        }
    }
}
namespace TaleWorlds.CampaignSystem.Encounters
{
    public enum PlayerEncounterState { Begin, Wait, Battle, End }
    public sealed class PlayerEncounter
    {
        public static PlayerEncounter Current;
        public static Party.PartyBase EncounteredParty;
        public static bool LeaveEncounter, PlayerSurrender, EnemySurrender, BattleCreated;
        public PlayerEncounterState EncounterState = PlayerEncounterState.Begin;
    }
}
namespace TaleWorlds.CampaignSystem.GameState { public sealed class MapState { public bool MapConversationActive; } }
namespace TaleWorlds.Core
{
    public sealed class Game { public static Game Current; public StateManager GameStateManager = new(); }
    public sealed class StateManager { public object ActiveState = new TaleWorlds.CampaignSystem.GameState.MapState(); }
}
namespace TaleWorlds.MountAndBlade { public sealed class Mission { public static Mission Current; } }
namespace TaleWorlds.CampaignSystem.GameMenus
{
    public sealed class GameMenu
    {
        public string StringId = "AnimusForge_lord_encounter";
        public static int Activations;
        public static bool ThrowOnActivate, Reenter;
        public static void ActivateGameMenu(string id)
        {
            if (!NativeDialogueBattleContinuation.IsResumingNativeBattleMenu) throw new Exception("custom redirect would swallow native menu");
            if (ThrowOnActivate) throw new Exception("fixture menu error");
            Activations++;
            Campaign.Current.CurrentMenuContext.GameMenu.StringId = id;
            Encounters.PlayerEncounter.BattleCreated = true; // Native init boundary substitute, not a game battle.
            if (Reenter) NativeDialogueBattleContinuation.Tick();
        }
    }
}
namespace AnimusForge
{
    internal static class MapSeaContextGuard { internal static bool AtSea; internal static bool IsCurrentPlayerEncounterAtSea(Hero target) => AtSea; }
    internal static class LordEncounterBehavior
    {
        internal static bool NativeActivity;
        internal static readonly List<string> Logs = new();
        internal static bool IsNativeEncounterActivityContext(Hero target) => NativeActivity;
        internal static void LogEncounterDiagnostic(string stage, string reason) => Logs.Add(stage + ":" + reason);
    }
    internal static class MeetingBattleRuntime { internal static bool IsMeetingActive; }
    internal static class PlayerEncounterCompat
    {
        internal static bool Result, PostResult;
        internal static bool HasCampaignBattleResult() => Result;
        internal static bool IsInPostBattleResultFlow() => PostResult;
    }
    internal static class Logger { internal static void Log(string category, string text) => LordEncounterBehavior.Logs.Add(category + ":" + text); }
}
namespace HarmonyLib
{
    public sealed class Harmony
    {
        public static int Patches;
        public Harmony(string id) { }
        public void Patch(MethodInfo method, HarmonyMethod postfix)
        { if (method == null || postfix.Method == null) throw new Exception("missing patch contract"); Patches++; }
    }
    public sealed class HarmonyMethod
    {
        public MethodInfo Method;
        public HarmonyMethod(Type type, string name) => Method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
    }
    public static class AccessTools { public static MethodInfo Method(Type type, string name, Type[] args) => type.GetMethod(name, args); }
}
