using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
using AnimusForge;
namespace TaleWorlds.Core { public enum MissionMode { Conversation, StartUp, Battle, Deployment, Duel, Stealth, Tournament } }
namespace TaleWorlds.CampaignSystem
{
    public class CampaignBehaviorBase { }
    public class Campaign { public static Campaign Current; public ConversationManager ConversationManager=new(); public object GameMenuManager=new(); public Models Models=new(); public QuestManager QuestManager=new(); public T GetCampaignBehavior<T>() where T:class => WorldMapPartyCommandBehavior.Instance as T; }
    public class QuestManager { public Dictionary<object,object> TrackedObjects=new(); }
    public class Models { public Models PartySizeLimitModel=>this; public int GetAssumedPartySizeForLordParty(Hero hero,object f,Clan c)=>100; }
    public class CharacterObject { public Hero HeroObject; public bool IsHero=>HeroObject!=null; public string StringId; public int Tier; }
    public class Hero { public static Hero MainHero=new(){StringId="player"}; public static List<Hero> AllAliveHeroes=new(); public string Name="NPC",StringId="npc"; public MobileParty PartyBelongedTo,PartyBelongedToAsPrisoner; public CharacterObject CharacterObject; public Town GovernorOf; public Settlement CurrentSettlement; public Clan Clan; public bool IsActive=true,CanLead=true,IsDead,IsDisabled,IsPrisoner,IsTraveling,IsNotable; public Hero(){CharacterObject=new(){HeroObject=this};} public bool CanLeadParty()=>CanLead; public bool InMainParty {get=>MobileParty.MainParty.MemberRoster.Contains(CharacterObject);set{MobileParty.MainParty.MemberRoster.Elements.RemoveAll(x=>x.Character==CharacterObject);if(value)MobileParty.MainParty.MemberRoster.Elements.Add(new(){Character=CharacterObject,Number=1});}} public int Gold; public void ChangeHeroGold(int amount)=>Gold+=amount; }
    public class Clan { public static Clan PlayerClan=new(); public string StringId="clan"; public object MapFaction; }
}
namespace TaleWorlds.CampaignSystem.Conversation { public class ConversationManager { public int ActiveToken=1; public bool IsConversationInProgress=true; } }
namespace TaleWorlds.CampaignSystem.Party { public class PartyBase { public static PartyBase MainParty=>MobileParty.MainParty.Party; public object MapEvent; } public class MobileParty { public static MobileParty MainParty=new(){StringId="main"}; public static List<MobileParty> All=new(); public string StringId,Name="Party"; public Hero LeaderHero; public Settlement CurrentSettlement; public Roster.TroopRoster MemberRoster=new(); public bool IsActive=true,IsBandit,IsBanditBossParty; public PartyBase Party=new(); public object MapEvent; } }
namespace TaleWorlds.CampaignSystem.Roster { public class TroopRoster {public List<TroopRosterElement> Elements=new();public IEnumerable<TroopRosterElement> GetTroopRoster()=>Elements;public bool Contains(CharacterObject c)=>Elements.Any(x=>x.Character==c&&x.Number>0);} public struct TroopRosterElement {public CharacterObject Character;public int Number,WoundedNumber,Xp;} }
namespace TaleWorlds.CampaignSystem.Settlements { public class Town {public Settlement Settlement;public Hero Governor;public MobileParty GarrisonParty;} public class Time {public bool IsPast=true;} public class Hideout { public bool IsSpotted=true,IsInfested=true;public Time NextPossibleAttackTime=new(); } public class Settlement { public static Settlement CurrentSettlement; public static List<Settlement> All=new(); public string StringId,Name="Location"; public Clan OwnerClan; public bool IsTown,IsCastle,IsVillage,IsHideout,Hostile,IsUnderSiege,Battle,QuestBusy; public PartyBase Party=new();public List<MobileParty> Parties=new();public bool Ready {get=>Parties.Count>0;set{Parties.Clear();if(value)Parties.Add(new(){IsBandit=true});}} public bool IsSettlementBusy(object owner)=>QuestBusy; public Hideout Hideout=new(); } }
namespace TaleWorlds.CampaignSystem.Settlements.Locations
{
    public class Location { public string StringId="center"; }
    public class LocationComplex { public static LocationComplex Current=new(); public Location GetLocationWithId(string id)=>new(); }
}
namespace TaleWorlds.MountAndBlade
{
    public class Mission { public static Mission Current; public bool IsMissionEnding; public TaleWorlds.Core.MissionMode Mode; public List<Agent> Agents=new(); public List<object> Behaviors=new(); public T GetMissionBehavior<T>() where T:class=>Behaviors.OfType<T>().FirstOrDefault(); }
    public class Agent { public static Agent Main=new(); public int Index; public CharacterObject Character; public bool Active=true; public bool IsActive()=>Active; }
    public class CampaignMission { public static CampaignMission Current=new(); public TaleWorlds.CampaignSystem.Settlements.Locations.Location Location=new(); }
}
namespace AnimusForge.Refactor.Runtime { public static class SaveRuntimeGuard { public static long Generation; public static long CaptureGeneration()=>Generation; public static bool IsCurrentGeneration(long n)=>n==Generation; } }
namespace AnimusForge
{
    public static class Logger { public static void Log(string a,string b) { } }
    public class PartyTransferAuthorizationSnapshot
    {
        public List<MyBehavior.PartyTransferPromptEntry> IndexedTroops=new(),AllTroops=new(),AllPrisoners=new();
    }
    public static class PartyAssetTransferBannerlordAdapter
    {
        public static PartyTransferAuthorizationSnapshot Candidates=new();
        public static bool IsPartyTransferNotableRecruitEligible(Hero hero)=>hero!=null;
        public static PartyTransferAuthorizationSnapshot CapturePlayerDirectTransferCandidates(Hero h,CharacterObject c,int index)=>Candidates;
    }
    public static class MyBehavior
    {
        public static void AppendExternalDialogueHistory(Hero h,object a,object b,string fact) { }
        public static void AppendExternalNonHeroDialogueHistory(string id,string name,object a,object b,string fact) { }
        public class PartyTransferPromptEntry { public int PromptIndex,Count; public string DisplayName; public CharacterObject Character; public object OwnerParty,VolunteerOwner,SourceSettlement; }
        public class SettlementTransferPromptEntry { public string AssetId,TypeLabel,DisplayName; public object OwnerClan,OwnerHero,Settlement,Workshop,CaravanParty; }
        public static bool IsPartyTransferLordEligibleForExternal(Hero h,CharacterObject c)=>h!=null;
        public static bool IsWildernessNonHeroPartyTransferEligibleForExternal(Hero h,CharacterObject c,int i)=>false;
        public static bool IsSettlementTransferEntryValidForExternal(SettlementTransferPromptEntry e)=>true;
        public static string GetSettlementTransferAssetIdForExternal(SettlementTransferPromptEntry e)=>e.AssetId;
    }
    public partial class RewardSystemBehavior
    {
        public static RewardSystemBehavior Instance=new();
        private static int GetHeroGold(Hero hero)=>hero.Gold;
        private void ShowRewardMessage(string text, Hero hero) { }
        public class RewardItemInfo { public object Item; public int Count; public string PromptStringId,StringId,Name,ModifierStringId; public bool IsPrivateEquipment; }
        public class DebtLineExportEntry { public string DebtId,DebtNote,ItemId; public int RemainingAmount; public bool IsGold,IsDueUnlimited; }
        public List<RewardItemInfo> Items=new(); public List<DebtLineExportEntry> Debts=new(); public List<MyBehavior.SettlementTransferPromptEntry> Assets=new();
        public bool TryGetSettlementMerchantKind(CharacterObject c,out object kind) {kind=null;return false;}
        public List<RewardItemInfo> BuildHeroRewardPostprocessItems(Hero h)=>Items;
        public List<RewardItemInfo> BuildPartyRewardPostprocessItems(PartyBase p)=>Items;
        public List<RewardItemInfo> BuildSettlementMerchantPostprocessItems(CharacterObject c)=>Items;
        public List<MyBehavior.SettlementTransferPromptEntry> GetAllowedNpcSettlementTransferEntriesForPlayer(Hero h,CharacterObject c)=>Assets;
        public List<DebtLineExportEntry> GetPlayerDirectDebtCandidates(Hero h,CharacterObject c)=>Debts;
    }
    public static class GiveGoldAction { public static void ApplyBetweenCharacters(Hero giver, Hero receiver,int amount) {giver.Gold-=amount;receiver.Gold+=amount;} }
    public static class PromptListRetrievalService
    {
        public const string NpcRewardItemsAllSnapshotScope="hero",PartyRewardItemsAllSnapshotScope="party",SettlementMerchantItemsAllSnapshotScope="merchant",
            SettlementTransferNpcAssetsSnapshotScope="assets",SettlementTransferAllNpcAssetsSnapshotScope="assets_all";
        public static void PublishRewardItemSnapshot(string scope,Hero h,CharacterObject c,int i,IEnumerable<RewardSystemBehavior.RewardItemInfo> items) { }
        public static void PublishSettlementTransferSnapshot(string scope,Hero h,CharacterObject c,int i,IEnumerable<MyBehavior.SettlementTransferPromptEntry> items) { }
    }
    public static class VanillaIssueOfferBridge { public static string GetPlayerDirectIssueUnavailableReason(Hero h,bool turnIn)=>h!=null?null:"hero required"; }
    public class FakeNativeEffects
    {
        public Hero LastHero;
        public Action AfterDispatch;
        public WorldMapPartyCommandBehavior.WorldMapOrderApplyResult ResultOverride;
        public List<string> Tags=new(); public WorldMapPartyCommandBehavior.WorldMapOrderApplyResult ApplyNativeConversationActionTags(Hero h,CharacterObject c,ref string raw,int i,
            ConversationManager expectedConversationManager=null,int expectedConversationToken=int.MinValue,string actionChainName=null,string npcReplyTextOverride=null)
        { if(npcReplyTextOverride!="")throw new Exception("fake NPC speech"); LastHero=h; Tags.Add(raw); var result=ResultOverride??(raw.Contains("WORLDMAP_ORDER")?WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(h,c,i,ref raw):new()); AfterDispatch?.Invoke(); return result; }
    }
    public class FakeMovement { public int Calls; public bool TryExecuteDeferredSceneFollowTagsDirectly(ShoutBehavior.NpcDataPacket npc,string raw) {Calls++;return true;} }
    public partial class ShoutBehavior
    {
        public static ShoutBehavior CurrentInstance;
        public static Hero Target; public static CharacterObject TargetCharacter; public static bool MainThread=true,Busy;
        private int _sceneConversationEpoch;
        internal readonly FakeNativeEffects _nativeGameEffects=new(); internal readonly FakeMovement _sceneMovement=new();
        public int QueueCalls, CloseCalls, ExitCalls;
        private void ScheduleWorldMapMissionExitAfterSpeech(int i, object info) { }
        private void FlushWorldMapMissionExitAfterSpeech(int i) { if(i>=0){ExitCalls++;Mission.Current.IsMissionEnding=true;} }
        internal static bool IsActiveSceneConversationDuelCombat()=>MeetingBattleRuntime.Duel;
        internal readonly FakePresentation Presentation = new();
        private bool IsPresentationSubmitBlocked() => Busy;
        public int MovementReleases;
        private void DeactivateMultiSceneMovementSuppression() => MovementReleases++;
        public void NextEpoch()=>_sceneConversationEpoch++;
        public class NpcDataPacket { public int AgentIndex; public string Name; }
        private static bool IsBannerlordMainThreadForNativeActions()=>MainThread;
        private static bool CanSubmitNativeConversationForExternal()=>Campaign.Current?.ConversationManager.IsConversationInProgress==true;
        private static bool IsNativeConversationBackendBusyForUi()=>Busy;
        private static bool TryResolveNativeConversationTarget(out Hero h,out CharacterObject c,out string n) {h=Target;c=TargetCharacter;n="NPC";return c!=null;}
        private static int TryResolveNativeConversationAgentIndex(Hero h,CharacterObject c)=>Mission.Current?.Agents.FirstOrDefault(a=>ReferenceEquals(a.Character,c))?.Index??-1;
        private static bool IsNativeConversationResponseTargetAvailableForActionDispatch(int i,Hero h,CharacterObject c,out string reason) {reason=null;return i<0||Mission.Current?.Agents.Any(a=>a.Index==i && a.Active && ReferenceEquals(a.Character,c))==true;}
        private static bool TryResolveWildernessNonHeroRewardParty(Hero h,CharacterObject c,int i,out PartyBase p) {p=null;return false;}
        private static NpcDataPacket BuildNativeConversationNpcData(Hero h,CharacterObject c)=>new(){Name="NPC"};
        private bool TryQueueNativeSceneMechanismActionAfterConversationExit(NpcDataPacket npc,object a,object b,object c,ref string raw) {QueueCalls++;return true;}
        private static void CloseNativeConversationForSceneMechanism(string reason) {CurrentInstance.CloseCalls++;Campaign.Current.ConversationManager.IsConversationInProgress=false;}
    }
    // Engine membership boundary only. Admission, selected-target capture and execution are real source.
    internal sealed class FakePresentation
    {
        internal sealed class Member { internal Agent Agent; internal CharacterObject Character; internal int AgentIndex; internal string Name; internal bool InRange = true; }
        internal bool Active;
        internal int _presentationAddresseeIndex = -1;
        internal readonly List<Member> Members = new();
        internal bool IsPresentationSessionLive() => Active;
        internal Member FindPresentationMember(int index) => Members.FirstOrDefault(x => x.AgentIndex == index);
        internal bool IsPresentationAudience(Member member) => member != null && member.InRange && member.Agent.IsActive();
    }
}
namespace AnimusForge
{
    internal static class AIConfigHandler { internal static bool Restricted; internal static bool ShouldExcludeSceneMoveRuleForCurrentMission()=>Restricted||Mission.Current?.Mode==TaleWorlds.Core.MissionMode.Battle; }
    internal static class MeetingBattleRuntime { internal static bool IsMeetingActive,IsCombatEscalated,Duel; }
    internal static class LordEncounterBehavior { internal static bool IsEncounterMeetingMissionActive; }
    internal sealed class MeetingBattleLockMissionBehavior { }
    internal sealed class MilitaryExerciseMissionLogic { }
    internal static class MilitaryExerciseBehavior { internal static bool Active; internal static bool IsCurrentExerciseRuntime()=>Active; }
    internal static class TroopInspectionBehavior { internal static bool Active; internal static bool ShouldSuppressReinforcementSystem(Mission m)=>Active; }
    internal static class PlayerEncounterCompat { internal static bool Battle; internal static bool HasBattleOrEncounteredBattle()=>Battle; internal static bool HasCampaignBattleResult()=>false; }
}
namespace AnimusForge.Refactor.Adapters { internal static class PromptRuleCaptureBannerlordAdapter { internal static bool ShouldExcludeWorldMapCommandRuleForTarget(Hero h,CharacterObject c,int i)=>h?.IsNotable==true; } }
