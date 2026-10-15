// Native boundaries only; the entry, confirmation, battle and defeat code is linked production source.
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
namespace HarmonyLib
{
    public class Harmony(string id) { public void Patch(MethodInfo m, HarmonyMethod prefix=null, HarmonyMethod postfix=null) { } }
    public class HarmonyMethod(Type t, string n) { }
    public static class AccessTools { public static MethodInfo Method(Type t, string n, Type[] p) => null; }
}
namespace TaleWorlds.Library { }
namespace TaleWorlds.Core { public class ItemRoster { } }
namespace TaleWorlds.CampaignSystem.Roster { public class TroopRoster { public static TroopRoster CreateDummyTroopRoster() => new(); } }
namespace TaleWorlds.Localization { public class TextObject(string s) { public override string ToString() => s; } }
namespace TaleWorlds.MountAndBlade { public class Mission { public static Mission Current; } }
namespace TaleWorlds.CampaignSystem
{
    public class Hero { public static Hero MainHero; public bool IsWounded; public int HitPoints; }
    public class Clan { public static Clan PlayerClan; public object MapFaction; }
    public class Campaign { public static Campaign Current; public object Owner; public T GetCampaignBehavior<T>() => (T)Owner; }
    public interface IDataStore { bool IsSaving { get; } void SyncData<T>(string key, ref T value); }
    public class Event<T> { public void AddNonSerializedListener(object owner, Action<T> handler) { } }
    public static class CampaignEvents {
        public static Event<CampaignGameStarter> OnSessionLaunchedEvent = new();
        public static Event<MapEvent> OnPlayerBattleEndEvent = new();
        public static Event<MenuCallbackArgs> GameMenuOpened = new();
    }
    public class CampaignGameStarter { public void AddGameMenuOption(string menu, string id, string text, Func<MenuCallbackArgs,bool> condition, Action<MenuCallbackArgs> consequence, bool isLeave, int order) { } }
    public static class FactionManager { public static bool AtWar; public static bool IsAtWarAgainstFaction(object a, object b) => AtWar; }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class BeHostileAction { public static int Calls; public static void ApplyEncounterHostileAction(PartyBase a, PartyBase b) { Calls++; FactionManager.AtWar = true; } }
}
namespace TaleWorlds.CampaignSystem.CampaignBehaviors { public class VillageHostileActionCampaignBehavior { } }
namespace TaleWorlds.CampaignSystem.Party
{
    public class Roster { public int TotalHealthyCount; }
    public class PartyBase { public static PartyBase MainParty; public MapEvent MapEvent; public int NumberOfHealthyMembers; }
    public class MobileParty { public static MobileParty MainParty; public Roster MemberRoster; public Settlement CurrentSettlement; }
}
namespace TaleWorlds.CampaignSystem.MapEvents
{
    public class Side { public int TroopCount; }
    public class MapEvent { public Settlement MapEventSettlement; public bool IsForcingSupplies, IsPlayerMapEvent, HasWinner; public int PlayerSide, WinningSide; public Side AttackerSide, DefenderSide; }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public class Village { public enum VillageStates { Normal, Looted } public VillageStates VillageState; }
    public class Settlement {
        public static Settlement CurrentSettlement;
        public bool IsVillage; public string StringId; public object MapFaction;
        public Village Village; public PartyBase Party;
        public static Settlement Find(string id) => CurrentSettlement?.StringId == id ? CurrentSettlement : null;
    }
}
namespace TaleWorlds.CampaignSystem.Encounters
{
    public class LocationEncounter { public Settlement Settlement; }
    public class PlayerEncounter {
        public static PlayerEncounter Current; public static Settlement EncounterSettlement; public static LocationEncounter LocationEncounter;
        public bool ForceSupplies; public static int Starts, Updates; public static Action OnStart;
        public static void StartBattle() { Starts++; OnStart?.Invoke(); }
        public static void Update() { Updates++; }
    }
}
namespace TaleWorlds.CampaignSystem.GameMenus
{
    public class GameMenuOption { public enum LeaveType { Submenu } }
    public class GameMenu { public static string LastMenu; public string StringId; public static void SwitchToMenu(string id) => LastMenu = id; }
    public class MenuContext { public GameMenu GameMenu; }
    public class MenuCallbackArgs { public bool IsEnabled = true; public TaleWorlds.Localization.TextObject Tooltip; public GameMenuOption.LeaveType optionLeaveType; public MenuContext MenuContext; }
}
namespace Helpers
{
    public static class MenuHelper {
        public static int ConditionCalls, AttackCalls; public static bool ConditionResult = true, EnableResult = true;
        public static bool EncounterAttackCondition(MenuCallbackArgs args) { ConditionCalls++; args.IsEnabled = EnableResult; return ConditionResult; }
        public static void EncounterAttackConsequence(MenuCallbackArgs args) { AttackCalls++; }
    }
}
namespace AnimusForge
{
    public static class Logger { public static void Log(string source, string message) { } }
    public static class GcczDiagnosticLog { public static List<string> Lines = new(); public static void Log(string source, string message) => Lines.Add(message); }
    public static class PlayerEncounterCompat { public static MapEvent Battle; public static MapEvent GetCurrentMapEventSafe() => Battle; }
    public static class SiegeAiInterventionBehavior { public static int FriendlyEntries; public static void EnterFriendlyVillageFromMenu(MenuCallbackArgs args) => FriendlyEntries++; }
    public class Ledger { public bool PrisonersPresented, ItemsPresented; public void RestorePresentation(bool a, bool b) { PrisonersPresented=a; ItemsPresented=b; } }
    public class SetsBattleRewardBatch {
        public bool VillageSuppliesPrepared, VillageDamageApplied, VillageSkillClaimed; public Ledger Ledger = new();
        public TaleWorlds.Core.ItemRoster Items, VillageSupplyItems;
        public TaleWorlds.CampaignSystem.Roster.TroopRoster Prisoners;
        public int VillageGold, VillageGoldPaid;
    }
    public sealed partial class SettlementEntryTroopSelectionBehavior {
        public static int Queued, Pumps;
        private class PendingSettlementVictoryMenuEntry {
            public string SettlementId, Source, PreviousOwnerClanId;
            public TaleWorlds.CampaignSystem.Roster.TroopRoster SurvivingRoster;
            public bool SkipOwnershipTransfer, SetsOwnedIncident, SetsTownRiotKilledNotable, MenuPresented;
            public SetsBattleRewardBatch BattleRewards;
        }
        private class PendingVillageAftermathEncounterExit { public string SettlementId; }
        private static PendingSettlementVictoryMenuEntry _pendingVictoryMenuEntry;
        private static PendingVillageAftermathEncounterExit _pendingVillageAftermathEncounterExit;
        public static string PendingVillage { get => _pendingVictoryMenuEntry?.SettlementId; set => _pendingVictoryMenuEntry = value == null ? null : new() { SettlementId=value }; }
        public static bool Presented { get => _pendingVictoryMenuEntry?.MenuPresented == true; set { if (_pendingVictoryMenuEntry != null) _pendingVictoryMenuEntry.MenuPresented=value; } }
        private static void PumpPendingPostMissionFlow(string source) { Pumps++; Presented=true; }
        public static void QueueExitForTest(string id) => _pendingVillageAftermathEncounterExit = id == null ? null : new() { SettlementId=id };
        public static SetsBattleRewardBatch Rewards;
        public static void QueueSettlementTakenMenuAfterVictory(string id, object o, string reason, bool skipOwnershipTransfer, SetsBattleRewardBatch battleRewards) { Queued++; PendingVillage = id; Rewards = battleRewards; }
    }
    public sealed partial class VillageAftermathBehavior
    {
        private string _dispositionPayoutVillage = "";
        private static VillageAftermathBehavior EconomyOwner => Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();
        private static bool SameFaction(Settlement village) => village.MapFaction == Clan.PlayerClan.MapFaction;
        private static bool CanEnterFriendlyVillage(Settlement village) => true;
        private static string VUi(string cn, string en) => en;
        public static string LastFailure;
        public static int PayoutPumps;
        internal static void PumpPendingPayoutForExternal(string villageId) { PayoutPumps++; }
        private static void TryDisplayFailureMessage(string text) => LastFailure = text;
        public static void InstallHooks() => _nativeDispositionHooksInstalled = true;
        public static bool Condition(MenuCallbackArgs args) => NativeDispositionEntryCondition(args);
        public static void Enter(MenuCallbackArgs args) => NativeDispositionEntryConsequence(args);
        public static bool Confirm(MenuCallbackArgs args) => NativeDispositionConfirmedPrefix(args);
        public static void Confirmed(MenuCallbackArgs args) => NativeDispositionConfirmedPostfix(args);
        public void BattleEnded(MapEvent battle) => OnNativeDispositionBattleEnd(battle);
        public void SetPending(string id) => _nativeDispositionVillageId = id;
        public string Pending => _nativeDispositionVillageId;
        public bool Victory => _nativeDispositionVictory;
        public static string BeforeSupplies() { NativeDispositionSuppliesPrefix(out string state); return state; }
        public static void AfterSupplies(string state) => NativeDispositionSuppliesPostfix(state);
    }
}
