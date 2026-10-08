using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
namespace AnimusForge {
// Game/UI leaves only; actual controller state and callback lifecycle are linked from production.
internal static class SceneInteractionContract {
 internal static SceneTradeController Create(ScenePresentationController panel, Func<List<ShoutBehavior.ShoutTradeResourceOption>> options, Action effects, Action<string,string,int?> accept, Func<bool> targetValid, Func<bool,string> facts=null) {
  return new SceneTradeController(new SceneTradeControllerPorts {
   GetPresentation=()=>panel, BuildShoutTradeOptions_L14160=()=>options(), GetShoutTradeTargetIneligibility_L33=mode=>null,
   EnsureShoutTradePrimaryTargetValidForCommit_L14056=require=>targetValid(), ApplyShoutGiveTransfer_L14619=()=>effects(),
   BuildShoutTradeFactText_L15241=give=>facts?.Invoke(give)??"observed", RecordShoutShownResources_L16847=()=>{}, EstimateShoutPendingShowTotalValue_L15395=()=>0,
   ShowShoutPendingDisplayValueMessage_L15421=value=>{}, OnShoutConfirmedWithContext_L17001=(text,fact,target)=>accept(text,fact,target),
   PauseGame_L21301=()=>{},ResumeGame_L21311=()=>{}, OnShoutCancelled_L21322=()=>{},
   RecordNativeConversationTradeActionFact_L14519=fact=>{}, BuildShoutTargetEncyclopediaAction_L13863=npc=>null,
   Get_activeShoutTargetingContext=()=>new ShoutTargetingContext(), Set_activeShoutTargetingContext=value=>{},
   BuildPresentationTargetingContext_L51=()=>new ShoutTargetingContext(), EnsurePresentationSessionForWheelAction_L119=()=>true,
   BeginShoutProcessing_L1570=why=>{},ActivateMultiSceneMovementSuppression_L18651=indices=>{}
  });
 }
 internal static SceneTradeBannerlordAdapter CreateTradeAdapter(SceneTradeController owner,ScenePresentationController panel) {
  return new SceneTradeBannerlordAdapter(new SceneTradeBannerlordAdapterPorts {
   GetPresentation=()=>panel,Get_activeShoutTargetingContext=()=>new ShoutTargetingContext(),Set_activeShoutTargetingContext=v=>{},
   GetAgentsForShoutTargetingContext_L1285=context=>new List<Agent>(TaleWorlds.MountAndBlade.Mission.Current.Agents),
   GetShoutTradeTargetAgentIndex_L14076=()=>owner.GetShoutTradeTargetAgentIndex(),
   ResolveHeroFromAgentIndex_L17635=i=>System.Linq.Enumerable.FirstOrDefault(TaleWorlds.MountAndBlade.Mission.Current.Agents,a=>a.Index==i)?.Character.HeroObject,
   AppendShoutTradeActionFactSequence_L14587=fact=>owner.AppendShoutTradeActionFactSequence(fact),
   RecordScenePrepaidTransfer_L14987=(key,gold)=>{},QueuePendingCurrentAfefFactForAgent_L17336=(i,fact)=>{},QueuePendingCurrentNativeAfefFactForKey_L17363=(key,fact)=>{},
   AppendActionAfefFactToSceneHistoryInOrder_L17395=(i,fact,mirror)=>{},PromotePersonalizedExtraFactInScenePrivateHistory_L17524=(fact,i)=>{},
   Get_shoutTradeOptions=()=>owner._shoutTradeOptions, Set_shoutTradeOptions=value=>owner._shoutTradeOptions=value,
   Get_shoutPendingTradeItems=()=>owner._shoutPendingTradeItems, Set_shoutPendingTradeItems=value=>owner._shoutPendingTradeItems=value,
   Get_shoutPendingTradeItemIndex=()=>owner._shoutPendingTradeItemIndex, Set_shoutPendingTradeItemIndex=value=>owner._shoutPendingTradeItemIndex=value,
   Get_shoutTradeMode=()=>owner._shoutTradeMode, Set_shoutTradeMode=value=>owner._shoutTradeMode=value,
   Get_shoutTradeTargetNpc=()=>owner._shoutTradeTargetNpc, Set_shoutTradeTargetNpc=value=>owner._shoutTradeTargetNpc=value,
   Get_shoutTradeNativeManager=()=>owner._shoutTradeNativeManager, Get_shoutTradeNativeMission=()=>owner._shoutTradeNativeMission, Get_shoutTradeNativeAgent=()=>owner._shoutTradeNativeAgent,
   Get_shoutTradeTargetAgentSnapshot=()=>owner._shoutTradeTargetAgentSnapshot, Set_shoutTradeTargetAgentSnapshot=value=>owner._shoutTradeTargetAgentSnapshot=value,
   Get_shoutTradeActionOnly=()=>owner._shoutTradeActionOnly, Set_shoutTradeActionOnly=value=>owner._shoutTradeActionOnly=value,
   Get_shoutTradeTargetHeroOverride=()=>owner._shoutTradeTargetHeroOverride, Set_shoutTradeTargetHeroOverride=value=>owner._shoutTradeTargetHeroOverride=value,
   Get_shoutTradeTargetCharacterOverride=()=>owner._shoutTradeTargetCharacterOverride, Set_shoutTradeTargetCharacterOverride=value=>owner._shoutTradeTargetCharacterOverride=value,
  });
 }

}
public sealed partial class ShoutBehavior {
 internal static ShoutBehavior CurrentInstance;
 internal SceneTradeController _j17SceneTradeController;
 internal void OpenNativeConversationGiveShowMenu(NpcDataPacket npc,Hero hero,CharacterObject character,Action done)=>_j17SceneTradeController.OpenNativeConversationGiveShowMenu(npc,hero,character,done);
 internal static bool TryResolveNativeConversationTarget(out Hero hero,out CharacterObject character,out string name){hero=null;character=null;name="NPC";return false;}
 internal static NpcDataPacket BuildNativeConversationNpcData(Hero hero,CharacterObject character)=>new();
 internal static int TryResolveNativeConversationAgentIndex(Hero hero,CharacterObject character)=>-1;
 internal static bool IsBannerlordMainThreadForNativeActions()=>true;
 internal static string GetNoShoutTradeOptionsMessage(ShoutChatMode mode)=>"none";
 internal static void BumpPresentation(){}
 internal static float GetApplicationTimeSafe()=>1;
	internal enum ShoutChatMode
	{
		Normal,
		Give,
		Show,
		GiveTroops,
		GivePrisoners,
		GiveSettlements
	}
	internal class ShoutTradeResourceOption
	{
		public bool IsGold;

		public string ItemId;

		public string Name;

		public int AvailableAmount;

		public ItemObject Item;

		public long InventoryTotalValue;

		public int InventoryUnitValue;

		public MyBehavior.PartyTransferPromptEntry PartyEntry;

		public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
	}
	internal class ShoutPendingTradeItem
	{
		public bool IsGold;

		public string ItemId;

		public string ItemName;

		public int Amount;

		public ItemObject Item;

		public int InventoryUnitValue;

		public MyBehavior.PartyTransferPromptEntry PartyEntry;

		// Discarded with the existing trade state; not a second outcome ledger.
		public string PartyTransferPartialFact;

		public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
	}
}
internal sealed class ShoutTargetingContext { internal HashSet<int> CandidateAgentIndices=new(); internal Dictionary<int,float> CandidatePlayerDistancesMeters=new(); }
internal static class CourierDeliveryBehavior {internal static string GetCourierLetterTransferDisplayTitleForExternal(string name)=>name;internal static string GetCourierLetterTransferFactDescriptionForExternal(string id,uint internalId,string name)=>name;}
internal static class ShoutTextInputPopup { internal static Action<string> Confirm;internal static Action Cancel;internal static bool Show(string title,string description,string prompt,string value,Action<string> confirm,Action cancel,Action encyclopedia){Confirm=confirm;Cancel=cancel;return true;} }
internal sealed class SceneMovementController {
 internal sealed class SceneSummonConversationSession {}
 internal HashSet<int> Following=new(),Guide=new(),Summon=new(),GuideHold=new(),GuideReturn=new();
 internal bool IsAgentBusyWithSceneGuideErrand(int i)=>Guide.Contains(i);
 internal bool HasGuideArrivalHold(int i)=>GuideHold.Contains(i);
 internal bool HasPendingGuideReturn(int i)=>GuideReturn.Contains(i);
 internal bool IsAgentBusyWithSceneSummonErrand(int i)=>Summon.Contains(i);
 internal bool IsAgentFollowingPlayerBySceneCommand(TaleWorlds.MountAndBlade.Agent a)=>a!=null&&Following.Contains(a.Index);
 internal SceneSummonConversationSession TryGetSceneSummonConversationSessionForAgentIndex(int i)=>null;
 internal Action<string> CompletionTrace;
 internal void FlushSceneFollowCommandAfterSpeech(int i)=>CompletionTrace?.Invoke("follow");
 internal void FlushSceneSummonReturnAfterSpeech(int i)=>CompletionTrace?.Invoke("summon");
 internal void FlushSceneGuideReturnAfterSpeech(int i)=>CompletionTrace?.Invoke("guide");
 internal void FlushPendingSceneSummonLaunches(int i)=>CompletionTrace?.Invoke("summon-launch");
 internal void FlushPendingSceneGuideLaunches(int i)=>CompletionTrace?.Invoke("guide-launch");
 internal void ClearPendingSummonLaunches(){}
 internal void CancelPendingSummonReturn(int i){}
 internal void ClearSceneSummonConversationInteractionTimers(SceneSummonConversationSession s){}
 internal void SetPendingSummonReturn(int i,SceneSummonConversationSession s,bool speaker){}
 internal bool ShouldReturnOnlySceneSummonSpeaker(SceneSummonConversationSession s,TaleWorlds.MountAndBlade.Agent a)=>false;
 internal void BeginSceneSummonConversationReturn(SceneSummonConversationSession s){}
}
internal static partial class MyBehavior {
 internal enum PartyTransferEntrySection { PlayerTroops,PlayerPrisoners }
 internal sealed class PartyTransferPromptEntry { internal string DisplayName="unit";internal int Count=1;internal int PromptIndex,WageDenarsPerDay,HirePriceDenarsPerUnit,BuyPriceDenarsPerUnit;internal bool IsHero;internal PartyTransferEntrySection Section;internal CharacterObject Character;internal object OwnerParty,SourceSettlement,VolunteerOwner; }
 internal enum SettlementTransferEntrySection {PlayerFiefs}
 internal sealed class SettlementTransferPromptEntry {internal SettlementTransferEntrySection Section; internal string TypeLabel,AssetKind;internal TaleWorlds.CampaignSystem.Settlements.Settlement Settlement;internal object Workshop,CaravanParty;internal int DailyIncomeDenars,GuidePriceDenars; }
 internal static string GetPartyTransferPrisonerSourceLabelForExternal(PartyTransferPromptEntry entry)=>"";
}
}
namespace TaleWorlds.Core {
 public sealed class ItemObject {public string StringId="item";public TaleWorlds.CampaignSystem.Text Name=new("item");public MBGUID Id;}
 public sealed class InquiryElement {public object Identifier;public InquiryElement(object id,string name,object image,bool isEnabled,string hint){Identifier=id;} }
 public sealed class MultiSelectionInquiryData { public static MultiSelectionInquiryData Latest;public Action<List<InquiryElement>> Confirm;public Action Cancel;public MultiSelectionInquiryData(string title,string description,List<InquiryElement> elements,bool isExitShown,int min,int max,string affirmative,string negative,Action<List<InquiryElement>> confirm,Action cancel,string sound,bool isSeachAvailable){Confirm=confirm;Cancel=cancel;Latest=this;} }
 public sealed class TextInquiryData {public static TextInquiryData Latest;public Action<string> Confirm;public Action Cancel;public TextInquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action<string> confirm,Action cancel){Confirm=confirm;Cancel=cancel;Latest=this;}}
 public static class MBInformationManager {public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data,bool pauseGameActiveState){} }
}
namespace TaleWorlds.CampaignSystem.Settlements { public sealed class Settlement {public static Settlement CurrentSettlement;public string StringId="settlement";public TaleWorlds.CampaignSystem.Roster.ItemRoster ItemRoster=new();public bool IsTown;} }

namespace AnimusForge.SceneActions.Core {}
namespace AnimusForge.SiegeAftermathIntervention {}
namespace AnimusForge.XihaiAction {}
namespace AnimusForge.Refactor.Adapters {}
namespace AnimusForge.Refactor.Contracts {}
namespace AnimusForge.Refactor.Modules {}
namespace AnimusForge.Refactor.Runtime {}
namespace RichExecutions.Core {}
namespace RichExecutions.Scene {}
namespace SandBox {}
namespace SandBox.Missions.AgentBehaviors {}
namespace SandBox.Missions.MissionLogics {}
namespace SandBox.Missions.MissionLogics.Towns {}
namespace SandBox.Objects.AnimationPoints {}
namespace SandBox.Objects.Usables {}
namespace TaleWorlds.CampaignSystem.Actions {}
namespace TaleWorlds.CampaignSystem.ComponentInterfaces {}
namespace TaleWorlds.CampaignSystem.Conversation {}
namespace TaleWorlds.CampaignSystem.Encounters {}
namespace TaleWorlds.CampaignSystem.Party {}
namespace TaleWorlds.CampaignSystem.Roster {}
namespace TaleWorlds.CampaignSystem.Settlements.Locations {}
namespace TaleWorlds.CampaignSystem.Siege {}
namespace TaleWorlds.Engine {}
namespace TaleWorlds.InputSystem {}
namespace TaleWorlds.Library {}
namespace TaleWorlds.Localization {}
namespace TaleWorlds.MountAndBlade.Missions {}
namespace AnimusForge {
public partial class ShoutBehavior {
internal sealed class SceneInteractionSession
	{
		public int TargetAgentIndex;

		public string TargetName;

		public float LastActivityTime;

		public bool TimeoutArmed;

		public float TimeoutSeconds;

		public long InteractionToken;

		public bool ReturnSceneSummonOnTimeout;

		public float InitialPlayerDistanceMeters;

		public float PlayerReleaseRangeMeters;
	}
internal sealed class PendingInteractionTimeoutArm
	{
		public int AgentIndex;

		public long InteractionToken;

		public float ArmAtMissionTime;
	}
 internal static bool CanAgentParticipateInSceneSpeech(Agent a)=>a!=null&&a.IsHuman&&a.IsActive()&&a.Health>0;
 internal static bool IsAgentHostileToMainAgent(Agent a)=>a?.Hostile==true;
 internal static bool PreserveMeeting;
 internal static bool ShouldPreserveMeetingSceneAutonomy()=>PreserveMeeting;
 internal static bool TryGetPlayerPlanarDistanceMeters(Agent a,out float distance){distance=a==null?0:Math.Abs(a.Position.X-Agent.Main.Position.X);return a!=null;}
 internal static string SanitizeSceneSpeechText(string s)=>s;
 internal static string StripNpcNamePrefixSafely(string s,int n)=>s;
 internal static float EstimateBubbleTypingDurationSeconds(string s)=>1;
 internal static string GetPlayerDisplayNameForShout()=>"player";
}
internal struct NativeSpeechInteractionDiagnosticSnapshot {internal bool HasInteraction,TimeoutArmed,HasPendingArm;internal long InteractionToken;internal float ArmAtMissionTime;}
internal static class ShoutUtils {internal static NpcDataPacket ExtractNpcData(Agent a)=>a==null?null:new NpcDataPacket{AgentIndex=a.Index,Name="NPC"};}
}

namespace AnimusForge {
public partial class ShoutBehavior {
 internal static bool TryParsePresentationTradeMode(string mode,out ShoutChatMode result)=>SceneTradeController.TryParsePresentationTradeMode(mode,out result);
 internal static bool IsShoutPartyTransferMode(ShoutChatMode m)=>SceneTradeController.IsShoutPartyTransferMode(m);
 internal static bool IsShoutTroopTransferMode(ShoutChatMode m)=>SceneTradeController.IsShoutTroopTransferMode(m);
 internal static bool IsShoutSettlementTransferMode(ShoutChatMode m)=>SceneTradeController.IsShoutSettlementTransferMode(m);
 internal static bool IsShoutTradeShowMode(ShoutChatMode m)=>SceneTradeController.IsShoutTradeShowMode(m);
 internal static string NormalizeNativeConversationFactLineForPrompt(string fact,string label)=>fact;
 internal static bool ContainsPlayerCraftedAfefInspectionSuffix(string fact)=>false;
 internal static string StripPlayerCraftedAfefInspectionSuffix(string fact)=>fact;
 internal static string BuildNativeConversationHistoryKey(Hero hero,CharacterObject character,string name,int i,NpcDataPacket npc)=>"key";
 internal static void AppendNativeConversationSessionHistory(Hero h,CharacterObject c,string n,string speaker,string text,string kind,int targetAgentIndex,NpcDataPacket npc,bool bridgeToSceneHistory){}
 internal static bool TryResolveWildernessNonHeroRewardParty(Hero h,CharacterObject c,int i,out TaleWorlds.CampaignSystem.Party.PartyBase p){p=null;return false;}
 internal static TaleWorlds.CampaignSystem.Party.MobileParty TryResolveWildernessNonHeroMobileParty(int i)=>null;
 internal static string NormalizeWildernessNonHeroMemoryKeyPart(string s)=>s;
 internal static bool IsNativeConversationSelfTarget(Hero h,CharacterObject c)=>h!=null&&h==Hero.MainHero;
}
internal static partial class MyBehavior {
 internal static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesForExternal(Hero h,CharacterObject c,int i)=>new();
 internal static List<SettlementTransferPromptEntry> BuildSettlementTransferPromptEntriesForExternal(Hero h,CharacterObject c)=>new();
 internal static bool IsSettlementTransferEntryValidForExternal(SettlementTransferPromptEntry e)=>true;
 internal static string GetSettlementTransferAssetDisplayNameForExternal(SettlementTransferPromptEntry e)=>"asset";
 internal static int GetRemainingShowableGoldForExternal(Hero h,string key,int count)=>count;
 internal static int GetRemainingShowableItemCountForExternal(Hero h,string key,string id,int count)=>count;
 internal static string BuildRuleTargetKeyForExternal(Hero h,CharacterObject c,int i)=>"key";
 internal static string BuildPlayerPublicDisplayNameForExternal()=>"player";
 internal static bool IsPartyTransferLordEligibleForExternal(Hero h,CharacterObject c)=>true;
 internal static bool IsSettlementTransferLeaderEligibleForExternal(Hero h,CharacterObject c)=>true;
 internal static TaleWorlds.CampaignSystem.Party.PartyBase ResolvePartyTransferCounterpartyForExternal(Hero h,CharacterObject c,int i)=>new();
 internal static void RecordShownResourcesForExternal(Hero h,string key,int gold,Dictionary<string,int> items){}
 internal static void AppendExternalDialogueHistory(Hero h,string player,string reply,string fact){}
 internal static int RemoveItemsFromRosterByStringId(TaleWorlds.CampaignSystem.Roster.ItemRoster roster,string id,int amount,out TaleWorlds.Core.ItemObject item){item=System.Linq.Enumerable.FirstOrDefault(roster.Items.Keys,x=>x.StringId==id);if(item==null)return 0;int removed=Math.Min(amount,roster.GetItemNumber(item));roster.AddToCounts(item,-removed);return removed;}
 internal sealed class TestPartyEffect {internal int Delivered;}
 internal static TestPartyEffect TransferPlayerPartyEntryWithObservedEffects(Hero h,CharacterObject c,int i,PartyTransferPromptEntry entry,int amount)=>new(){Delivered=amount};
}
internal static class PartyTransferExecutionOwner {internal static string BuildPartialEffectFact(MyBehavior.PartyTransferPromptEntry e,MyBehavior.TestPartyEffect effect)=>"";}
internal static class AfGcczShoutBridge {
 internal static bool ShouldCaptureSharedReliefTransfer(int i)=>false;
 internal static bool CaptureSharedReliefGoldTransfer(int i,int amount)=>false;
 internal static bool CaptureSharedReliefItemTransfer(int i,string id,int amount,TaleWorlds.Core.ItemObject item,int value)=>false;
}
internal sealed class RewardSystemBehavior {
 internal bool Merchant,ThrowOnMerchantCapture;internal int RecordedGold;internal List<string> MerchantFacts=new();internal static RewardSystemBehavior Instance;internal enum SettlementMerchantKind {None,Merchant}
 internal bool TryGetSettlementMerchantKind(CharacterObject c,out SettlementMerchantKind kind){if(ThrowOnMerchantCapture)throw new Exception("synthetic capture exception");kind=Merchant?SettlementMerchantKind.Merchant:SettlementMerchantKind.None;return Merchant;}
 internal int GetInventoryActualItemUnitValueForExternal(TaleWorlds.Core.EquipmentElement e)=>1;
 internal bool TryApplyPlayerSettlementTransferForExternal(Hero h,MyBehavior.SettlementTransferPromptEntry e,out string status){status="";return true;}
 internal void RecordPlayerPrepaidTransfer(Hero h,int gold,string id,int amount){}
 internal int TransferGoldToSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s,Hero h,int amount){TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ApplyBetweenCharacters(h,null,amount,true);return amount;}
 internal int TransferGoldToParty(TaleWorlds.CampaignSystem.Party.PartyBase p,Hero h,int amount)=>0;
 internal int TransferItemToParty(TaleWorlds.CampaignSystem.Party.PartyBase p,Hero h,string id,int amount,out string name){name=id;return 0;}
 internal void RecordPlayerPrepaidTransferForMerchant(TaleWorlds.CampaignSystem.Settlements.Settlement s,SettlementMerchantKind k,int gold,string id,int amount){RecordedGold+=gold;}
 internal void AppendSettlementMerchantNpcFact(TaleWorlds.CampaignSystem.Settlements.Settlement s,SettlementMerchantKind k,string fact,string name){MerchantFacts.Add(fact);}
 internal string BuildSettlementItemValueFactSuffixForExternal(TaleWorlds.CampaignSystem.Settlements.Settlement s,object item,int amount)=>"";
 internal long EstimateSettlementItemValueForExternal(TaleWorlds.CampaignSystem.Settlements.Settlement s,string id,int amount)=>amount;
 internal string BuildItemValueFactSuffixForExternal(Hero h,string id,int amount)=>"";
 internal long EstimateItemValueForExternal(Hero h,string id,int amount)=>amount;
 internal string BuildInventoryActualItemValueFactSuffixForExternal(TaleWorlds.Core.ItemObject item,int amount,int value)=>"";
 internal long EstimateInventoryActualItemValueForExternal(TaleWorlds.Core.ItemObject item,int amount,int value)=>amount*value;
 internal static string DecoratePlayerCraftedAfefItemNameForExternal(string id,uint internalId,string name,Hero hero,CharacterObject character,string key,string mode,bool commit)=>name;
}
}

namespace AnimusForge {public partial class ShoutBehavior {
internal sealed class PendingNpcBubbleEntry
	{
		public Agent Agent;

		public string UiContent;

		public string NpcName;

		public float FallbackDurationSeconds;
	}
internal sealed class PendingSceneDialogueFeedEntry
	{
 public Mission SourceMission; public long RuntimeGeneration; public int ConversationEpoch=-1;
		public string SpeakerLabel;

		public string Content;

		public Color Color;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

 internal static string GetSceneNpcHistoryNameForPrompt(NpcDataPacket npc)=>npc?.Name;
}
internal struct NativeSpeechOutputDiagnosticSnapshot {internal int PendingBubbleCount,PendingDurationCount,PendingSpeechTokenCount;}
}
