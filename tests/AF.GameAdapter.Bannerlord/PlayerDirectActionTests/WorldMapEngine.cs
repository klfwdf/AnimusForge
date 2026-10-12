using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using System.Threading;
namespace AnimusForge;

// Engine leaves only. Protocol, admission, preflight, append/STOP and hostile
// conversion execute complete production declarations extracted at build time.
public sealed partial class WorldMapPartyCommandBehavior
{
    public static WorldMapPartyCommandBehavior Instance=new();
    internal static MobileParty NonHeroParty;
    internal static void Reset() { MobileParty.MainParty.MemberRoster=new(); Instance=new(); NonHeroParty=null; Hero.AllAliveHeroes.Clear(); MobileParty.All.Clear(); Settlement.All.Clear(); }
    private const int DefaultHeroAttackDays=1,DefaultRaidAttackDays=5,DefaultSiegeAttackDays=15,GovernorGarrisonMinimumReserve=40,GovernorExpeditionMinimumTroops=10;
    private const float GovernorGarrisonReserveRatio=0.5f;
    private const string AttackModeAi="AI",AttackModeForce="FORCE",LegacyAttackModeRebellionForce="REBELLION_FORCE",PendingSafeExitStop="stop",GovernorExpeditionPhaseReturning="returning",GovernorExpeditionPhaseCleanup="cleanup";
    private static readonly Regex WorldMapOrderTagRegex=new("\\[ACTION:WORLDMAP_ORDER:[^\\]\\r\\n]*\\]",RegexOptions.IgnoreCase);
    internal enum CommandKind { GoToSettlement,PatrolSettlement,FollowHero,FollowParty,AttackHero,AttackParty,MergeToPlayer,ClearHideout }
    internal enum CommandStage { New }
    internal sealed class PartyCommandEntry { public string Kind,TargetType,TargetId,Mode; public int Days=1; public double HoldUntilDay=-1; public bool RequiresExistingWar; }
    internal sealed class PartyCommandQueueState { public string HeroId,ActorKey,ActorName,PartyStringId,Stage,SourceId,NonHeroMemoryId,NonHeroMemoryName; public int PartyIndex,CurrentIndex; public bool EngageCommitted; public List<PartyCommandEntry> Commands=new(); public string PendingSafeExitAction,FollowSiegeSettlementId; public bool PendingStop {get=>PendingSafeExitAction=="stop";set=>PendingSafeExitAction=value?"stop":null;} public bool FollowSiege {get=>FollowSiegeSettlementId!=null;set=>FollowSiegeSettlementId=value?"town":null;} }
    private sealed class PendingGovernorExpeditionRequest { public long RequestTicket; public string HeroId,OriginSettlementId,OriginClanId; public List<PartyCommandEntry> FollowUpCommands; }
    private sealed class PendingCreateCompanionPartyRequest {public string HeroId; public List<PartyCommandEntry> FollowUpCommands;}
    private sealed class GovernorTroopTransferPlan {public CharacterObject Character; public int Count,Xp;}
    private sealed class GovernorExpeditionRecord { public string Phase; }

    private readonly Dictionary<string,PartyCommandQueueState> _queues=new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string,PartyCommandQueueState> Queues=>_queues;
    private readonly object _queueLock=new();
    private readonly Dictionary<string,PendingCreateCompanionPartyRequest> _pendingCreatePartyRequests=new();
    private readonly Dictionary<string,PendingGovernorExpeditionRequest> _pendingGovernorExpeditionRequests=new();
    private readonly WorldMapPendingRequestCoordinator _governorExpeditionRequestTickets=new();
    private int _hasPendingCreateCompanionPartyRequests,_hasPendingGovernorExpeditionRequests;
    public sealed class WorldMapOrderApplyResult { public bool HadTag,Handled,StopApplied,CompanionPartyCreationQueued,GovernorExpeditionCreationQueued; public int AddedCommandCount; public bool NeedsChannelExit=>CompanionPartyCreationQueued||GovernorExpeditionCreationQueued; }
    private static Hero ResolveHeroById(string id)=>id==Hero.MainHero.StringId?Hero.MainHero:Hero.AllAliveHeroes.FirstOrDefault(h=>h.StringId==id);
    private static Settlement ResolveSettlementById(string id)=>Settlement.All.FirstOrDefault(s=>s.StringId==id);
    private static MobileParty ResolveMobilePartyById(string id)=>id==MobileParty.MainParty.StringId?MobileParty.MainParty:MobileParty.All.FirstOrDefault(p=>p.StringId==id);

    private static bool TryResolveNonHeroPartyActorForExternal(CharacterObject c,int i,out MobileParty p) {p=NonHeroParty;return p!=null;}

    private static bool IsPartyUsable(MobileParty p)=>p?.IsActive==true && p.Party!=null;

    private static bool IsKind(PartyCommandEntry c,CommandKind k)=>c?.Kind==k.ToString();
    private static bool IsSettlementTarget(PartyCommandEntry c)=>c?.TargetType=="settlement";

    private static string BuildPartyActorKey(MobileParty p,bool createGuid)=>p==null?null:"party:"+p.StringId;
    private static string BuildPartyMemoryId(MobileParty p)=>p.StringId;
    private static string NormalizeExternalSourceId(string s)=>s?.Trim();

    private static bool HasActiveMapEvent(MobileParty p)=>p?.MapEvent!=null&&p.MapEvent is not FinalizedMapEvent;

    private static MobileParty ResolvePartyForSafeExit(PartyCommandQueueState s,Hero h)=>h.PartyBelongedTo;
    private bool TryDeferNpcHideoutStop(MobileParty p,PartyCommandQueueState s,string reason,out string fact) {fact="";return false;}
    private static bool TryExitFollowSiegeControl(MobileParty p,PartyCommandQueueState s,bool detachPreexistingParticipation,string reason)=>false;
    private static void SetPendingSafeExit(PartyCommandQueueState s,string action,string reason)=>s.PendingStop=true;
    private void RequestFollowSiegeRefresh() { }
    private static void AbortCurrentCommandIfNeeded(MobileParty p,PartyCommandQueueState s) { }
    private bool BeginGovernorExpeditionReturn(Hero h,MobileParty p,string reason)=>false;
    private bool TryGetGovernorExpeditionForHero(string id,out GovernorExpeditionRecord r) {r=null;return false;}
    private static void LeaveArmyIfNeeded(MobileParty p) { }
    private static void ReleasePartyAi(MobileParty p) { }
    private void StartCurrentCommand(Hero h,MobileParty p,PartyCommandQueueState s) { }
    private void TryReactivateGovernorExpedition(Hero h,MobileParty p) { }
    private static int GetPartyIndexSafe(MobileParty p)=>0;
    private static string GetHeroName(Hero h)=>h?.Name;
    private static string GetPartyName(MobileParty p)=>p?.Name;
    private static string GetActorName(PartyCommandQueueState s,Hero h,MobileParty p)=>h?.Name??p?.Name;
    private static string BuildGoToSettlementConversionSummary(int a,int b)=>"";
    private static bool IsPartyHoldingInsideCommandSettlement(MobileParty p,PartyCommandEntry c)=>false;
    private static bool TryResolveHostileBesiegerParty(MobileParty p,Settlement s,out MobileParty b) {b=null;return false;}
    private static bool IsPartyAtWarWithSettlement(MobileParty p,Settlement s)=>s.Hostile;
    private bool IsRegisteredPlayerDetachment(Hero h,MobileParty p)=>false;
    private static bool IsForeignClanGuestHero(Hero h)=>false;
    private static bool TryValidateForeignClanGuestMerge(Hero h,MobileParty p,out string reason){reason="";return false;}
    private static bool HasActiveSettlementMapEvent(Settlement s)=>s.Battle;
    private static string GetSettlementName(Settlement s)=>s.Name;
    private static bool IsPartyScreenStillActive()=>false;
    private static bool CanOpenCreateCompanionPartyScreenNow(out string reason){reason="conversation";return false;}
    private bool OpenCreateCompanionPartyScreen(Hero h,List<PartyCommandEntry> c,out string reason){reason="engine UI not exercised";return false;}
    private bool TryCreateGovernorExpeditionNow(Hero h,string id,string clan,List<PartyCommandEntry> c,out string reason){reason="engine creation not exercised";return false;}
    internal bool HasPendingCompanion(string id)=>_pendingCreatePartyRequests.ContainsKey(id);
    internal bool HasPendingGovernor(string id)=>_pendingGovernorExpeditionRequests.ContainsKey(id);
    internal int PendingCompanionCommands(string id)=>_pendingCreatePartyRequests[id].FollowUpCommands.Count;
    internal int PendingGovernorCommands(string id)=>_pendingGovernorExpeditionRequests[id].FollowUpCommands.Count;
    internal static void SetGovernor(Hero hero)
    {
        hero.PartyBelongedTo=null;
        var origin=new Settlement{StringId="origin",IsTown=true,OwnerClan=hero.Clan};
        var garrison=new MobileParty{CurrentSettlement=origin};
        garrison.MemberRoster.Elements.Add(new(){Character=new(){StringId="regular"},Number=100});
        hero.GovernorOf=new(){Settlement=origin,Governor=hero,GarrisonParty=garrison};hero.CurrentSettlement=origin;
    }
    private static void Log(string s) { }
    private static string StripWorldMapOrderTags(string s)=>WorldMapOrderTagRegex.Replace(s??"","").Trim();
    private static string CommandMessageForDisplay(string s)=>s;
}
internal static class CourierDeliveryBehavior {internal static bool IsCourierParty(MobileParty party)=>false;}
internal sealed class FinalizedMapEvent { }
internal static class WorldMapNpcHideoutCompletionPatch {internal static bool IsReady=true;}
internal static class RomanceSystemBehavior {internal static bool IsPlayerCompanionOrFamily(Hero hero)=>hero.Clan==Clan.PlayerClan;}
internal static class WorldMapSourceTexts { internal static string Get(string key)=>key; }
internal static class WorldMapUiText
{
    internal static string Result(string key,string fallback,params (string Name,object Value)[] vars)=>fallback;
    internal static string CannotControlPlayerParty=>"player";
    internal static string NoValidCommands=>"invalid";
    internal static string NonHeroNoParty=>"nonhero invalid";
    internal static string NonHeroQueueUnavailable=>"nonhero queue invalid";
    internal static string NoControllableParty(string n)=>"no party";
    internal static string MergeEligibilityFailure(string r,string n)=>"merge invalid";
    internal static string WaitingForBattle(string n,bool replacing)=>"battle";
    internal static string IsolatedSource(string n)=>"protected";
    internal static string QueueAccepted(string n,int count,bool start)=>"queued: "+count;
    internal static string GovernorInvalidState=>"invalid governor";
    internal static string GovernorNotCurrent=>"not current governor";
    internal static string GovernorAway=>"governor away";
    internal static string GovernorClanMismatch=>"governor clan changed";
    internal static string GovernorAlreadyAttached=>"governor already attached";
    internal static string GovernorCannotLead=>"governor cannot lead";
    internal static string GovernorGarrisonUnavailableGeneric=>"garrison unavailable";
    internal static string GovernorPlanIncomplete=>"troop plan incomplete";
    internal static string GovernorNoTask=>"no expedition task";
    internal static string GovernorSettlementBusy(string s)=>"origin busy";
    internal static string GovernorGarrisonUnavailable(string s)=>"garrison unavailable";
    internal static string GovernorValidationFailed(string s)=>"validation failed: "+s;
    internal static string GovernorTroopsInsufficient(int n,int reserve)=>"insufficient troops";
    internal static string CompanionRequestQueued(string n,string reason)=>"companion request queued";
    internal static string GovernorRequestQueued(string n,string reason)=>"governor request queued";
}
