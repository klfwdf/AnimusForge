using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
namespace TaleWorlds.Localization { public class TextObject { readonly string text; public TextObject(string s) { text = s; } public override string ToString() => text; } }
namespace TaleWorlds.Engine { public static class Time { public static float ApplicationTime; } }
namespace TaleWorlds.MountAndBlade { public class Mission { public static Mission Current; } }
namespace TaleWorlds.Library
{
    public class InformationMessage { public string Text; public InformationMessage(string text) { Text = text; } }
    public static class InformationManager { public static bool Inquiry; public static bool IsAnyInquiryActive() => Inquiry; public static List<string> Messages = new(); public static void DisplayMessage(InformationMessage message) => Messages.Add(message.Text); }
}
namespace TaleWorlds.Core
{
    public class Game { public static Game Current = new(); public StateManager GameStateManager = new(); }
    public class StateManager { public object ActiveState = new MapState(); }
    public class InquiryElement { public object Identifier; public InquiryElement(object id, string text, object image, bool enabled, string hint) { Identifier = id; } }
    public class MultiSelectionInquiryData { public MultiSelectionInquiryData(string title, string hint, List<InquiryElement> choices, bool exit, int min, int max, string yes, string no, Action<List<InquiryElement>> accept, Action<List<InquiryElement>> cancel) { } }
    public static class MBInformationManager { public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data, bool pauseGameActiveState) { } }
}
namespace TaleWorlds.CampaignSystem.GameState { public class MapState { public bool MapConversationActive; } }
namespace TaleWorlds.CampaignSystem
{
    public enum CampaignTimeControlMode { Stop, UnstoppableFastForward }
    public enum ConversationContext { Default, PartyEncounter }
    public class Campaign { public static Campaign Current = new(); public ConversationManager ConversationManager = new(); public CampaignTimeControlMode TimeControlMode; public ConversationContext CurrentConversationContext; public MenuContext CurrentMenuContext = new(); }
    public class Clan { public Hero Leader; }
    public class Hero
    {
        public static Hero MainHero = new(); public static Hero OneToOneConversationHero;
        public bool IsAlive = true, IsActive = true, IsLord = true, IsPrisoner; public Clan Clan; public MobileParty PartyBelongedTo;
        public string StringId = "hero"; public TextObject Name = new("Hero");
        public CharacterObject CharacterObject; public Hero() { CharacterObject = new() { HeroObject = this }; }
        public PartyBase PartyBelongedToAsPrisoner; public Settlement CurrentSettlement;
    }
    public class CharacterObject { public Hero HeroObject; public static CharacterObject PlayerCharacter => Hero.MainHero.CharacterObject; }
    public readonly record struct CampaignTime(long Ticks);
    public static class PlayerCaptivity { public static CampaignTime CaptivityStartTime; public static PartyBase CaptorParty; public static bool IsCaptive => CaptorParty != null; }
    public class CampaignGameStarter
    {
        public List<string> MenuOptions = new();
        public Dictionary<string, (string text, Func<bool> condition)> Dialogs = new();
        public void AddGameMenuOption(string menu, string id, string text, Func<MenuCallbackArgs, bool> condition, Action<MenuCallbackArgs> consequence) { MenuOptions.Add(menu + ":" + id); }
        public void AddDialogLine(string id, string input, string output, string text, Func<bool> condition, Action consequence, int priority = 100) { Dialogs.Add(id, (text, condition)); }
        public void AddPlayerLine(string id, string input, string output, string text, Func<bool> condition, Action consequence) { }
    }
}
namespace TaleWorlds.CampaignSystem.Party
{
    public class Roster { readonly HashSet<CharacterObject> heroes = new(); public bool Contains(CharacterObject hero) => heroes.Contains(hero); public void AddPrisoner(CharacterObject hero) => heroes.Add(hero); public void Remove(CharacterObject hero) => heroes.Remove(hero); public int GetTroopCount(CharacterObject hero) => heroes.Contains(hero) ? 1 : 0; }
    public class PartyBase
    {
        public static PartyBase MainParty = new(); public bool IsActive = true;
        public MobileParty MobileParty; public Settlement Settlement; public Hero LeaderHero; public object MapEvent;
        public bool IsMobile => MobileParty != null; public bool IsSettlement => Settlement != null;
        public TextObject Name = new("Captor"); public Roster PrisonRoster = new(), MemberRoster = new();
    }
    public class MobileParty { public static MobileParty MainParty = new(); public PartyBase Party; public Hero LeaderHero; public object MapEvent; }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public class Settlement { public bool IsTown = true, IsCastle; public object SiegeEvent; public Clan OwnerClan; public PartyBase Party; public Town Town = new(); }
    public class Town { public Hero Governor; }
}
namespace TaleWorlds.CampaignSystem.GameMenus
{
    public class MenuContext { public GameMenu GameMenu = new(); }
    public class MenuCallbackArgs { public GameMenuOption.LeaveType optionLeaveType; public bool IsEnabled; public TextObject Tooltip; }
    public class GameMenuOption { public enum LeaveType { Conversation } }
    public class GameMenu { public string StringId; public static void ActivateGameMenu(string id) => Campaign.Current.CurrentMenuContext.GameMenu.StringId = id; }
}
namespace TaleWorlds.CampaignSystem.Conversation
{
    public class ConversationManager { public bool IsConversationInProgress; public event Action ConversationEnd; public void EndConversation() { ConversationEnd?.Invoke(); IsConversationInProgress = false; } }
    public class ConversationCharacterData
    {
        public CharacterObject Character; public PartyBase Party;
        public ConversationCharacterData(CharacterObject character, PartyBase party, bool noHorse = false, bool noWeapon = false, bool noBodyguards = false) { Character = character; Party = party; }
    }
    public static class CampaignMapConversation
    {
        public static int Calls; public static bool Fail; public static Action Opening;
        public static void OpenConversation(ConversationCharacterData player, ConversationCharacterData npc)
        {
            Calls++; if (Fail) throw new Exception("opening failure");
            Hero.OneToOneConversationHero = npc.Character.HeroObject;
            Campaign.Current.ConversationManager.IsConversationInProgress = true;
            ((MapState)TaleWorlds.Core.Game.Current.GameStateManager.ActiveState).MapConversationActive = true;
            Opening?.Invoke();
        }
    }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class EndCaptivityAction
    {
        public static int Calls; public static bool ThrowBefore, ThrowAfter; public static Hero Facilitator; public static Action AfterRelease;
        public static void ApplyByReleasedByChoice(Hero hero, Hero facilitator = null)
        {
            Calls++; Facilitator = facilitator;
            if (ThrowBefore) throw new Exception("before");
            hero.PartyBelongedToAsPrisoner.PrisonRoster.Remove(hero.CharacterObject);
            hero.IsPrisoner = false; hero.PartyBelongedToAsPrisoner = null; PlayerCaptivity.CaptorParty = null;
            PartyBase.MainParty.MemberRoster.AddPrisoner(hero.CharacterObject);
            AfterRelease?.Invoke();
            if (ThrowAfter) throw new Exception("after");
        }
    }
}
namespace AnimusForge
{
    // Scope-prefix stripping is unchanged; opening tests pass an unwrapped canonical fact.
    internal static class ConversationSpeechTextRules { internal static string StripAfefPromptScopeLabel(string text) => text; }
    public static class CompanionProactiveChatBehavior
    {
        public static int Probes;
        public static bool HasPendingNativeOpeningForCurrentConversation() { Probes++; return false; }
        public static bool TryConsumePendingNativeOpening(Hero hero, out string fact, out string prompt) { Probes++; fact = prompt = ""; return false; }
    }
    public static class ProactiveNpcRequestBehavior
    {
        public static int Probes;
        public static bool HasPendingNativeOpeningForCurrentConversation() { Probes++; return false; }
        public static bool TryConsumePendingNativeOpening(Hero hero, out string fact, out string prompt) { Probes++; fact = prompt = ""; return false; }
    }
    public static class Logger { public static void Log(string category, string text) { } }
    public static class PlayerEncounterCompat { public static bool PostBattle; public static bool IsInPostBattleResultFlow() => PostBattle; }
    public static class SaveRuntimeGuard { public static long Generation = 1; public static long CaptureGeneration() => Generation; public static bool IsCurrentGeneration(long generation) => generation == Generation; }
    public static class ShoutBehavior { public static void OpenNativeConversationInputSilentlyForExternal() { } }
    public static class AdapterPromptCaptureTexts { public static string PcapCompat(string key) => "[AFEF NPC行为补充]"; }
    public class PostprocessRuleEntry { public string Tag { get; set; } public string Description { get; set; } }
    public static class AIConfigHandler { public static string ActionPostprocessFallbackMoodTag => "[ACTION:MOOD:neutral]";
        public static List<PostprocessRuleEntry> GetGuardrailRulePostprocessRules(string id)
        {
            using var rules = System.Text.Json.JsonDocument.Parse(File.ReadAllText(AnimusForgeModulePaths.GetModuleDataFilePath("RuleBehaviorPrompts.json")));
            var rule = rules.RootElement.GetProperty("RulePrompts").EnumerateArray().Single(r => r.GetProperty("Id").GetString() == id);
            return System.Text.Json.JsonSerializer.Deserialize<List<PostprocessRuleEntry>>(rule.GetProperty("PostprocessRules").GetRawText());
        } }
    public static class MyBehavior { public static List<string> Facts = new(); public static void AppendExternalDialogueHistory(Hero hero, string player, string npc, string fact) => Facts.Add(fact); }
}

namespace Newtonsoft.Json { public static class JsonConvert { public static T DeserializeObject<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json); } }
namespace AnimusForge
{
    // Package/path and JSON adapters only; the readers and memory projection are production source.
    internal static class AnimusForgeModulePaths
    {
        internal static string Root = Environment.GetEnvironmentVariable("AF_CAPTIVITY_TEST_ROOT");
        internal static string Edition = Environment.GetEnvironmentVariable("AF_CAPTIVITY_TEST_EDITION") ?? "CN";
        internal static string GetModuleDataFilePath(string file)
        {
            string module = file.StartsWith("Encounter") ? "AF.Module.Encounter" : "AF.Module.Prompt";
            string suffix = Edition == "EN" ? ".en" : file == "PromptComposerTexts.json" ? ".zh-CN" : "";
            return Path.Combine(Root, "content/modules", module, "ModuleData", Path.GetFileNameWithoutExtension(file) + suffix + ".json");
        }
    }
    internal static class NobleGatheringTextProjection { internal static string ProjectFactBodyForPrompt(string text) => text; }
    internal static class WorldMapFactPromptProjection { internal static string ProjectBodyForPrompt(string text) => text; }
}
