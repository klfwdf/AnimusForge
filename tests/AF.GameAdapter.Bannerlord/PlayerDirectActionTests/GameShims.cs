using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
namespace TaleWorlds.Core { public enum MissionMode { Conversation, StartUp } }
namespace TaleWorlds.CampaignSystem
{
    public class Campaign { public static Campaign Current; public ConversationManager ConversationManager=new(); public object GameMenuManager=new(); }
    public class CharacterObject { public Hero HeroObject; }
    public class Hero { public static Hero MainHero=new(); public string Name="NPC"; public int Gold; public void ChangeHeroGold(int amount)=>Gold+=amount; }
    public class Clan { public static Clan PlayerClan=new(); }
}
namespace TaleWorlds.CampaignSystem.Conversation { public class ConversationManager { public int ActiveToken=1; public bool IsConversationInProgress=true; } }
namespace TaleWorlds.CampaignSystem.Party { public class PartyBase { } public class MobileParty { public static MobileParty MainParty=new(); } }
namespace TaleWorlds.CampaignSystem.Settlements { public class Settlement { public static Settlement CurrentSettlement; public bool IsTown; } }
namespace TaleWorlds.CampaignSystem.Settlements.Locations
{
    public class Location { public string StringId="center"; }
    public class LocationComplex { public static LocationComplex Current=new(); public Location GetLocationWithId(string id)=>new(); }
}
namespace TaleWorlds.MountAndBlade
{
    public class Mission { public static Mission Current; public bool IsMissionEnding; public TaleWorlds.Core.MissionMode Mode; public List<Agent> Agents=new(); }
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
        public List<string> Tags=new(); public void ApplyNativeConversationActionTags(Hero h,CharacterObject c,ref string raw,int i,
            ConversationManager expectedConversationManager=null,int expectedConversationToken=int.MinValue,string actionChainName=null,string npcReplyTextOverride=null)
        { if(npcReplyTextOverride!="")throw new Exception("fake NPC speech"); Tags.Add(raw); }
    }
    public class FakeMovement { public int Calls; public bool TryExecuteDeferredSceneFollowTagsDirectly(ShoutBehavior.NpcDataPacket npc,string raw) {Calls++;return true;} }
    public partial class ShoutBehavior
    {
        public static ShoutBehavior CurrentInstance;
        public static Hero Target; public static CharacterObject TargetCharacter; public static bool MainThread=true,Busy;
        private int _sceneConversationEpoch;
        internal readonly FakeNativeEffects _nativeGameEffects=new(); internal readonly FakeMovement _sceneMovement=new();
        public int QueueCalls, CloseCalls;
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
}
