using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge.CoupSystem;

// AF owns physical eligibility, naming and political execution. Coup owns the reason
// to oppose the new ruler; ordinary weekly rebellion relation thresholds do not apply.
internal sealed class CoupRebellionBridge : CampaignBehaviorBase
{
    private enum RequestState { Queued, Naming, Ready, NamingFailed, Completed, WarCreated }

    private sealed class Request
    {
        public string Id;
        public string KingdomId;
        public string RulingClanId;
        public string RulerId;
        public string ClanId;
        public string ClanLeaderId;
        public bool LoyalistSelection;
        public string FormerKingId;
        public string FormerRulingClanId;
        public bool TrackCivilWar;
        public bool RestoreDynasty;
        public string OriginalName;
        public string OriginalShortName;
        public string RebelKingdomId;
        public bool RegistrationBlocked;
        public List<string> Followers = new List<string>();
        public int Week;
        public int Relation;
        public int Towns;
        public int Castles;
        public RequestState State;
        public string NamingJson;
        public string Message;
    }

    private sealed class Outcome
    {
        public string ActorHeroId;
        public string HeroId;
        public string SettlementId;
        public string KingdomId;
        public bool Success;
        public bool Captured;
        public string NpcText;
        public string PlayerText;
        public int Day;
        public string Date;
        public bool HistoryQueued;
        public string RecoveryId;
        public string RecoveryHash;
        public bool NpcRecorded;
        public bool PlayerRecorded;
        public bool WeeklyRecorded;
        public bool BulletinHandled;
        public string BulletinText;
        public string OriginalKingdomId;
        public string ActorKingdomId;
    }

    private sealed class NamingCompletion
    {
        public string Id;
        public long Generation;
        public MyBehavior Owner;
        public Campaign Campaign;
        public string Json;
        public string Error;
    }

    private sealed class AfAccess
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        internal readonly MethodInfo Resolve = Method("ResolveKingdomRebellion", 4);
        internal readonly MethodInfo CanTrackCoupWar = Method("CanTrackCoupCivilWar", 1);
        internal readonly MethodInfo RegisterCoupWar = Method("TryRegisterCoupCivilWar", 9);
        internal readonly MethodInfo BuildPrompt = Method("BuildRebelKingdomNamingRequest", 6);
        internal readonly MethodInfo Generate = Method("GenerateRebelKingdomNamingFromPrompts", 4);
        internal readonly MethodInfo Execute = Method("TryExecuteKingdomRebellionWithNaming", 10);
        internal readonly MethodInfo ValidateClan = Method("TryValidateClanForKingdomRebellion", 7);
        internal readonly MethodInfo ValidateFollower = Method("TryValidateClanForRebelFollower", 9);
        internal readonly MethodInfo FollowerEligible = Method("IsEligibleRebelFollowerByStandardRules", 3);
        internal readonly MethodInfo NamingSucceeded = Method("IsRebelKingdomNamingSuccess", 1);
        internal readonly MethodInfo Weekly = Method("RecordEventSourceMaterial", 12);
        internal readonly MethodInfo Bulletin = Method("TryRecordCoupOutcomeWithParticipantsForBulletin", 8);
        internal readonly MethodInfo PrepareMemory = Method("TryPrepareExternalDialogueHistoryRecoveryIdentity", 6);
        internal readonly MethodInfo CommitMemory = Method("CommitExternalDialogueHistoryRecoverable", 3);
        internal readonly MethodInfo MemoryStatus = Method("GetExternalDialogueHistoryRecoveryStatus", 3);
        internal readonly Type NamingType = typeof(MyBehavior).GetNestedType("RebelKingdomNamingResult", BindingFlags.NonPublic)
            ?? throw new MissingMemberException("AF RebelKingdomNamingResult");
        internal readonly FieldInfo SelectedClan = ResultField("SelectedClan");
        internal readonly FieldInfo SelectedFollowers = ResultField("SelectedFollowerClans");
        internal readonly FieldInfo ResolutionMessage = ResultField("Message");
        internal readonly FieldInfo NamingFailure = typeof(MyBehavior).GetNestedType("RebelKingdomNamingResult", BindingFlags.NonPublic)?.GetField("FailureReason", All)
            ?? throw new MissingFieldException("AF naming result", "FailureReason");
        internal readonly int NamingAttempts = (int)(typeof(MyBehavior).GetField("RebelKingdomNamingMaxAttempts", All)
            ?? throw new MissingFieldException("AF", "RebelKingdomNamingMaxAttempts")).GetRawConstantValue();
        internal readonly Func<MyBehavior, bool> Busy = (Func<MyBehavior, bool>)Delegate.CreateDelegate(
            typeof(Func<MyBehavior, bool>), Method("HasBlockingRebellionFlowForCoup", 0));
        internal readonly ConstructorInfo MemoryConstructor = typeof(InteractionMemoryCommit).GetConstructor(All, null,
            new[] { typeof(string), typeof(InteractionChannel), typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(IEnumerable<FactRecord>), typeof(long), typeof(long), typeof(string), typeof(int), typeof(int),
                typeof(string), typeof(int), typeof(int), typeof(string) }, null)
            ?? throw new MissingMethodException("AF recoverable domain memory constructor");

        internal AfAccess()
        {
            Type byRefString = typeof(string).MakeByRefType(), byRefInt = typeof(int).MakeByRefType();
            Require(CanTrackCoupWar, typeof(bool), false, typeof(Kingdom));
            Require(RegisterCoupWar, typeof(bool), false, typeof(string), typeof(Kingdom), typeof(Kingdom), typeof(Clan), typeof(Hero), typeof(bool), typeof(string), typeof(string), byRefString);
            Require(Resolve, SelectedClan.DeclaringType, false, typeof(Kingdom), typeof(int), typeof(bool), typeof(bool));
            Require(BuildPrompt, typeof(void), false, typeof(Clan), typeof(Kingdom), typeof(int), typeof(IEnumerable<Clan>), byRefString, byRefString);
            Require(Generate, NamingType, false, typeof(string), typeof(string), typeof(string), typeof(int));
            Require(Execute, typeof(bool), false, typeof(Clan), typeof(Kingdom), typeof(int), typeof(bool), typeof(int), typeof(int), typeof(int), NamingType, typeof(List<Clan>), byRefString);
            Require(ValidateClan, typeof(bool), false, typeof(Clan), typeof(Kingdom), typeof(bool), byRefString, byRefInt, byRefInt, byRefInt);
            Require(ValidateFollower, typeof(bool), false, typeof(Clan), typeof(Kingdom), typeof(Clan), typeof(bool), byRefString, byRefInt, byRefInt, byRefInt, byRefInt);
            Require(FollowerEligible, typeof(bool), true, typeof(int), typeof(int), typeof(float));
            Require(NamingSucceeded, typeof(bool), true, NamingType);
            Require(Weekly, typeof(void), false, typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(int), typeof(string));
            Require(Bulletin, typeof(bool), false, typeof(string), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            Require(PrepareMemory, typeof(bool), true, typeof(InteractionMemoryCommit), typeof(bool), typeof(string), byRefString, byRefString, byRefString);
            Require(CommitMemory, typeof(MemoryCommitResult), true, typeof(InteractionMemoryCommit), typeof(bool), typeof(string));
            Require(MemoryStatus, MemoryStatus.ReturnType, true, typeof(string), typeof(string), typeof(string));
            if (!MemoryStatus.ReturnType.IsEnum || MemoryStatus.ReturnType.FullName != "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryLookupStatus"
                || SelectedClan.FieldType != typeof(Clan) || SelectedFollowers.FieldType != typeof(List<Clan>) || ResolutionMessage.FieldType != typeof(string)
                || NamingFailure.FieldType != typeof(string))
                throw new MissingMemberException("AF rebellion/memory result or busy-field shape changed");
        }

        private static void Require(MethodInfo method, Type result, bool isStatic, params Type[] parameters)
        {
            if (method.IsStatic != isStatic || method.ReturnType != result || !method.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameters))
                throw new MissingMethodException("AF signature changed: " + method.Name);
        }

        private static MethodInfo Method(string name, int count)
        {
            MethodInfo method = typeof(MyBehavior).GetMethods(All).SingleOrDefault(m => m.Name == name && m.GetParameters().Length == count);
            return method ?? throw new MissingMethodException("AF " + name + "/" + count);
        }
        private static FieldInfo ResultField(string name) => typeof(MyBehavior).GetNestedType("KingdomRebellionResolutionResult", BindingFlags.NonPublic)?.GetField(name, All)
            ?? throw new MissingFieldException("AF rebellion result", name);
        internal bool IsBusy(MyBehavior owner)
        {
            return owner == null || Busy(owner);
        }
    }

    private static AfAccess _af;
    private static long _epoch;
    internal static CoupRebellionBridge Instance { get; private set; }
    internal static bool IsAvailable { get; private set; }
    private Dictionary<string, Request> _requests = new Dictionary<string, Request>(StringComparer.Ordinal);
    private Dictionary<string, Outcome> _outcomes = new Dictionary<string, Outcome>(StringComparer.Ordinal);
    private readonly ConcurrentQueue<NamingCompletion> _completions = new ConcurrentQueue<NamingCompletion>();
    private string _requestsJson;
    private string _outcomesJson;
    private bool _saveValid = true;
    private bool _hasWork;
    private bool _hasRegistrationFailure;
    internal bool HasPendingWarRegistration => _hasRegistrationFailure;

    internal void RetryPendingWarRegistration()
    {
        foreach (Request request in _requests.Values)
            if (request.State == RequestState.WarCreated) request.RegistrationBlocked = false;
        _hasRegistrationFailure = false;
        _hasWork = _requests.Values.Any(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
        _tickDelay = 0f;
    }
    private long _generation;
    private float _tickDelay;
    private string _runningId;
    private string _progressId;
    private string _choiceId;
    private Campaign _campaign;
    private MyBehavior _owner;

    internal static bool Initialize()
    {
        if (_af != null) return IsAvailable;
        try
        {
            _af = new AfAccess();
            IsAvailable = true;
            Logger.Log("Coup", "Installed AF rebellion/memory seam ready; MVID=" + typeof(MyBehavior).Assembly.ManifestModule.ModuleVersionId);
        }
        catch (Exception ex) { IsAvailable = false; Logger.Log("Coup", "Installed AF seam unavailable: " + ex); }
        return IsAvailable;
    }

    public override void RegisterEvents()
    {
        Instance = this;
        Initialize();
        ResetRuntime();
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGame);
        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
    }

    private void ResetRuntime()
    {
        _generation = Interlocked.Increment(ref _epoch);
        _campaign = Campaign.Current;
        _owner = null;
        _runningId = _progressId = _choiceId = null;
        _tickDelay = 0f;
        while (_completions.TryDequeue(out _)) { }
    }

    private void OnNewGame(CampaignGameStarter starter)
    {
        ResetRuntime();
        _requests.Clear(); _outcomes.Clear();
        _requestsJson = _outcomesJson = null;
        _saveValid = true; _hasWork = false; _hasRegistrationFailure = false;
    }

    private void OnGameLoaded(CampaignGameStarter starter)
    {
        ResetRuntime();
        if (!_saveValid) { Show("政变后叛乱存档异常，已停用自动重放并保留原始记录。"); return; }
        foreach (Request request in _requests.Values)
            if (request.State == RequestState.Naming) request.State = RequestState.Queued;
        _hasRegistrationFailure = _requests.Values.Any(r => r.RegistrationBlocked);
        _hasWork = _requests.Values.Any(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
    }

    public override void SyncData(IDataStore store)
    {
        if (store.IsSaving && _saveValid)
        {
            _requestsJson = JsonConvert.SerializeObject(_requests);
            _outcomesJson = JsonConvert.SerializeObject(_outcomes);
        }
        if (store.IsLoading) { ResetRuntime(); _requestsJson = _outcomesJson = null; _hasWork = false; }
        store.SyncData("_afCoupRebellionBridge_v1", ref _requestsJson);
        store.SyncData("_afCoupOutcomeBridge_v1", ref _outcomesJson);
        if (!store.IsLoading) return;
        try
        {
            _requests = JsonConvert.DeserializeObject<Dictionary<string, Request>>(_requestsJson ?? "{}") ?? new Dictionary<string, Request>(StringComparer.Ordinal);
            _outcomes = JsonConvert.DeserializeObject<Dictionary<string, Outcome>>(_outcomesJson ?? "{}") ?? new Dictionary<string, Outcome>(StringComparer.Ordinal);
            _saveValid = _requests.All(p => p.Value != null && p.Key == p.Value.Id && !string.IsNullOrWhiteSpace(p.Value.KingdomId)
                && (!p.Value.LoyalistSelection || (!string.IsNullOrWhiteSpace(p.Value.FormerKingId) && !string.IsNullOrWhiteSpace(p.Value.FormerRulingClanId)))
                && (!p.Value.TrackCivilWar || (!string.IsNullOrWhiteSpace(p.Value.OriginalName) && !string.IsNullOrWhiteSpace(p.Value.OriginalShortName)))
                && (p.Value.State != RequestState.WarCreated || (p.Value.TrackCivilWar && !string.IsNullOrWhiteSpace(p.Value.RebelKingdomId)))
                && (!p.Value.RegistrationBlocked || p.Value.State == RequestState.WarCreated)
                && p.Value.Followers != null && Enum.IsDefined(typeof(RequestState), p.Value.State))
                && _outcomes.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value != null);
        }
        catch (Exception ex) { _saveValid = false; Logger.Log("Coup", "Bridge save rejected; original JSON retained: " + ex); }
    }

    internal bool TryQueueCoupRebellion(CoupSession session, Kingdom kingdom, out string message)
    {
        message = "";
        string coupId = session?.Id;
        MyBehavior owner = MyBehavior.Instance;
        if (!Ready(owner) || string.IsNullOrWhiteSpace(coupId) || kingdom == null)
        { message = "AF 叛乱接缝、存档或王国尚未就绪。"; return false; }
        coupId = coupId.Trim();
        if (_requests.TryGetValue(coupId, out Request existing))
        { message = existing.Message; return existing.KingdomId == kingdom.StringId; }
        try
        {
            int week = Math.Max(0, (int)CampaignTime.Now.ToDays / 7);
            bool restore = session.Disposition == CoupKingDisposition.Release;
            List<CoupLoyalistCandidate> candidates;
            if (!(bool)_af.CanTrackCoupWar.Invoke(owner, new object[] { kingdom }))
            { candidates = new List<CoupLoyalistCandidate>(); message = "内战系统已关闭，跳过政变后内战。"; }
            else candidates = SelectLoyalists(owner, kingdom, session.KingId, session.OriginalRulingClanId, restore, out message);
            Clan clan = candidates.Count == 0 ? null : kingdom.Clans.FirstOrDefault(c => c.StringId == candidates[0].ClanId);
            var request = new Request
            {
                LoyalistSelection = true, FormerKingId = session.KingId, FormerRulingClanId = session.OriginalRulingClanId,
                TrackCivilWar = true, RestoreDynasty = restore,
                OriginalName = string.IsNullOrWhiteSpace(session.OriginalKingdomName) ? kingdom.Name.ToString() : session.OriginalKingdomName,
                OriginalShortName = string.IsNullOrWhiteSpace(session.OriginalKingdomShortName) ? kingdom.InformalName.ToString() : session.OriginalKingdomShortName,
                Id = coupId, KingdomId = kingdom.StringId, RulingClanId = kingdom.RulingClan?.StringId,
                RulerId = kingdom.Leader?.StringId, ClanId = clan?.StringId, ClanLeaderId = clan?.Leader?.StringId,
                Week = week, State = clan == null ? RequestState.Completed : RequestState.Queued,
                Message = message,
                Followers = candidates.Skip(1).Select(c => c.ClanId).ToList()
            };
            if (clan != null && request.RestoreDynasty && CanJoinRestoration(request, kingdom, out Clan dynasty)
                && dynasty != clan && !request.Followers.Contains(dynasty.StringId)) request.Followers.Add(dynasty.StringId);
            _requests.Add(coupId, request);
            if (clan != null) { _hasWork = true; request.Message = "旧王支持者已集结：" + clan.Name + "将领导反抗，另有" + request.Followers.Count + "个家族响应；将使用 AF 原命名与建国流程，忙时顺序等待。"; }
            message = request.Message;
            Logger.Log("Coup", "rebellion_registered coup=" + coupId + " state=" + request.State + " leader=" + request.ClanId + " followers=" + request.Followers.Count + " reason=" + request.Message);
            return true;
        }
        catch (Exception ex) { message = "政变后叛乱登记失败：" + Error(ex); Logger.Log("Coup", message); return false; }
    }

    private List<CoupLoyalistCandidate> SelectLoyalists(MyBehavior owner, Kingdom kingdom, string formerKingId, string formerClanId, bool allowFormerClan, out string message)
    {
        var candidates = new List<CoupLoyalistCandidate>();
        message = "旧王家族及更支持旧王的家族中，目前没有具备带地起兵条件的候选人。";
        if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
        { message = "AF 王国稳定度与叛乱已关闭，跳过政变后反叛。"; return candidates; }
        if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
        { message = "玩家王国叛乱免疫已开启，跳过政变后反叛。"; return candidates; }
        Hero formerKing = MBObjectManager.Instance.GetObject<Hero>(formerKingId);
        if (formerKing == null || kingdom?.Leader == null)
            throw new InvalidOperationException("政变前后统治者身份缺失，不能判定支持关系。");
        foreach (Clan candidate in kingdom.Clans)
        {
            object[] args = { candidate, kingdom, true, null, 0, 0, 0 };
            bool eligible = (bool)_af.ValidateClan.Invoke(owner, args);
            int oldRelation = 0, newRelation = (int)args[4];
            bool formerFamily = candidate?.StringId == formerClanId;
            if (formerFamily && !allowFormerClan) { eligible = false; args[3] = "旧王被扣押，其家族不参与本次内战。"; }
            if (eligible)
            {
                oldRelation = candidate.Leader == formerKing ? 100 : candidate.Leader.GetRelation(formerKing);
                eligible = CoupLoyalistPolicy.Opposes(formerFamily, oldRelation, newRelation);
                if (!eligible) args[3] = "对旧王的支持未超过对新王的支持。";
            }
            Logger.Log("Coup", "loyalist_candidate clan=" + candidate?.StringId + " oldFamily=" + formerFamily
                + " oldRelation=" + oldRelation + " newRelation=" + newRelation + " eligible=" + eligible + " note=" + args[3]);
            if (eligible) candidates.Add(new CoupLoyalistCandidate { ClanId = candidate.StringId, FormerRulingClan = formerFamily,
                RelationToOldKing = oldRelation, RelationToNewKing = newRelation, Fortifications = (int)args[5] + (int)args[6], ClanTier = candidate.Tier });
        }
        return CoupLoyalistPolicy.Rank(candidates);
    }

    private static bool StillSupportsFormerKing(Request request, Clan clan, Kingdom kingdom)
    {
        if (clan?.Leader == null || kingdom?.Leader == null) return false;
        if (clan.StringId == request.FormerRulingClanId) return true;
        Hero formerKing = MBObjectManager.Instance.GetObject<Hero>(request.FormerKingId);
        return formerKing != null && CoupLoyalistPolicy.Opposes(false,
            clan.Leader.GetRelation(formerKing), clan.Leader.GetRelation(kingdom.Leader));
    }

    private bool Ready(MyBehavior owner)
    {
        if (!IsAvailable || !_saveValid || !TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
            || owner == null || _campaign == null || !ReferenceEquals(Campaign.Current, _campaign)) return false;
        // Resolve the active owner once per load, not by scanning campaign behaviors every frame.
        if (_owner == null) _owner = _campaign.GetCampaignBehavior<MyBehavior>();
        return ReferenceEquals(owner, _owner);
    }

    private bool Validate(Request request, MyBehavior owner, out Kingdom kingdom, out Clan clan, out List<Clan> followers, out string message)
    {
        kingdom = MBObjectManager.Instance.GetObject<Kingdom>(request.KingdomId);
        clan = MBObjectManager.Instance.GetObject<Clan>(request.ClanId);
        followers = new List<Clan>();
        message = "";
        if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled()) { message = "AF 王国稳定度与叛乱已关闭，本次结束。"; return false; }
        if (kingdom != null && PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)) { message = "玩家王国叛乱免疫已开启，本次结束。"; return false; }
        if (kingdom == null || kingdom.IsEliminated || Campaign.Current.KingdomManager == null
            || kingdom.RulingClan?.StringId != request.RulingClanId || kingdom.Leader?.StringId != request.RulerId
            || clan?.Leader?.StringId != request.ClanLeaderId)
        { message = "王国、统治者或原候选族长已变化，本次结束，不重选家族。"; return false; }
        object[] args = { clan, kingdom, request.LoyalistSelection, null, 0, 0, 0 };
        if (!(bool)_af.ValidateClan.Invoke(owner, args)) { message = "原候选家族不再符合 AF 规则：" + args[3] + " 本次结束。"; return false; }
        if (request.LoyalistSelection && !StillSupportsFormerKing(request, clan, kingdom))
        { message = "原候选家族已改变对新旧国王的支持，本次不再起兵。"; return false; }
        request.Relation = (int)args[4]; request.Towns = (int)args[5]; request.Castles = (int)args[6];
        if (request.TrackCivilWar && !(bool)_af.CanTrackCoupWar.Invoke(owner, new object[] { kingdom }))
        { message = "内战系统已关闭，本次不创建无结算归属的叛军。"; return false; }
        if (request.TrackCivilWar && request.RestoreDynasty && !CanJoinRestoration(request, kingdom, out _))
        { message = "旧王家族已无法参与复位内战，本次不再起兵。"; return false; }
        foreach (string id in request.Followers)
        {
            Clan follower = MBObjectManager.Instance.GetObject<Clan>(id);
            if (request.TrackCivilWar && !request.RestoreDynasty && id == request.FormerRulingClanId) continue;
            object[] followerArgs = { follower, kingdom, clan, false, null, 0, 0, 0, 0 };
            if ((bool)_af.ValidateFollower.Invoke(owner, followerArgs)
                && (request.LoyalistSelection ? StillSupportsFormerKing(request, follower, kingdom)
                    : (bool)_af.FollowerEligible.Invoke(null, new object[] { followerArgs[5], followerArgs[6], 0f }))) followers.Add(follower);
        }
        // The released royal family can join a supporter-led restoration even after
        // losing its last fief. It need not qualify to found the rebel kingdom itself.
        if (request.TrackCivilWar && request.RestoreDynasty && CanJoinRestoration(request, kingdom, out Clan dynasty)
            && dynasty != clan && !followers.Contains(dynasty)) followers.Add(dynasty);
        return true;
    }

    private static bool CanJoinRestoration(Request request, Kingdom kingdom, out Clan dynasty)
    {
        dynasty = MBObjectManager.Instance.GetObject<Clan>(request.FormerRulingClanId);
        Hero formerKing = MBObjectManager.Instance.GetObject<Hero>(request.FormerKingId);
        return formerKing != null && (!formerKing.IsAlive || !formerKing.IsPrisoner)
            && dynasty != null && !dynasty.IsEliminated && dynasty != Clan.PlayerClan && dynasty.Kingdom == kingdom
            && dynasty.Leader?.IsAlive == true && !dynasty.Leader.IsChild && !dynasty.Leader.IsPrisoner;
    }

    // A real rebel kingdom already exists. Failure here may retry registration, never creation.
    private void RegisterCreatedWar(Request request, MyBehavior owner)
    {
        try
        {
            Kingdom home = MBObjectManager.Instance.GetObject<Kingdom>(request.KingdomId);
            Kingdom rebel = MBObjectManager.Instance.GetObject<Kingdom>(request.RebelKingdomId);
            Clan leader = MBObjectManager.Instance.GetObject<Clan>(request.ClanId);
            Hero formerKing = MBObjectManager.Instance.GetObject<Hero>(request.FormerKingId);
            if (home == null || rebel == null || leader?.Kingdom != rebel || formerKing == null)
                throw new InvalidOperationException("已经建国，但内战参与方记录暂不可用。");
            if (request.RestoreDynasty)
            {
                Clan dynasty = MBObjectManager.Instance.GetObject<Clan>(request.FormerRulingClanId);
                if (dynasty?.Kingdom == home && CanJoinRestoration(request, home, out _))
                    ChangeKingdomAction.ApplyByJoinToKingdomByDefection(dynasty, home, rebel, default(CampaignTime), true);
                if (dynasty?.Kingdom != rebel) throw new InvalidOperationException("旧王家族尚未加入复位叛军。");
            }
            object[] args = { request.Id, home, rebel, leader, formerKing, request.RestoreDynasty, request.OriginalName, request.OriginalShortName, null };
            if (!(bool)_af.RegisterCoupWar.Invoke(owner, args)) throw new InvalidOperationException(args[8] as string);
            Complete(request, args[8] as string);
        }
        catch (Exception ex)
        {
            request.Message = "叛军已建立，内战登记等待重试：" + Error(ex);
            request.RegistrationBlocked = true;
            _hasRegistrationFailure = true;
            _hasWork = _requests.Values.Any(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
            Logger.Log("Coup", "war_registration_pending coup=" + request.Id + " rebel=" + request.RebelKingdomId + " reason=" + request.Message);
            Show(request.Message + " 可在城镇菜单选择“重试政变内战登记”；不会再次创建叛军王国。");
        }
    }

    // Real engine time: campaign TickEvent stops while a town/menu is paused.
    internal void OnEngineTick(float dt)
    {
        if (!_hasWork || !Ready(MyBehavior.Instance)) return;
        _tickDelay -= dt;
        if (_tickDelay > 0f) return;
        _tickDelay = 0.25f;
        MyBehavior owner = MyBehavior.Instance;
        try
        {
            if (_completions.TryDequeue(out NamingCompletion completion))
            {
                if (completion.Generation == _generation && ReferenceEquals(completion.Owner, owner)
                    && ReferenceEquals(completion.Campaign, Campaign.Current) && _requests.TryGetValue(completion.Id, out Request finished)
                    && finished.State == RequestState.Naming)
                {
                    finished.NamingJson = completion.Json;
                    finished.Message = completion.Error;
                    finished.State = string.IsNullOrEmpty(completion.Json) ? RequestState.NamingFailed : RequestState.Ready;
                    _runningId = null;
                }
            }
            if (_runningId != null || _choiceId != null || Mission.Current != null
                || !(Game.Current?.GameStateManager?.ActiveState is MapState) || _af.IsBusy(owner)
                || (InformationManager.IsAnyInquiryActive() && _progressId == null)) return;
            Request request = _requests.Values.FirstOrDefault(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
            if (request == null) { _hasWork = false; return; }
            if (request.State == RequestState.WarCreated) { RegisterCreatedWar(request, owner); return; }
            if (!Validate(request, owner, out Kingdom kingdom, out Clan clan, out List<Clan> followers, out string note))
            { Complete(request, note); return; }
            if (request.State == RequestState.NamingFailed) { ShowNamingFailure(request); return; }
            if (request.State == RequestState.Ready)
            {
                object naming = JsonConvert.DeserializeObject(request.NamingJson, _af.NamingType);
                if (naming == null || !(bool)_af.NamingSucceeded.Invoke(null, new[] { naming }))
                {
                    request.Message = naming == null ? "AF 命名结果为空。" : _af.NamingFailure.GetValue(naming) as string;
                    request.State = RequestState.NamingFailed; ShowNamingFailure(request); return;
                }
                // A partial political mutation cannot be safely replayed on load.
                request.State = RequestState.Completed;
                request.Message = "政变后叛乱开始执行，本次判定已消费。";
                object[] args = { clan, kingdom, request.Week, true, request.Relation, request.Towns, request.Castles, naming, followers, null };
                try
                {
                    bool success = (bool)_af.Execute.Invoke(owner, args);
                    if (request.TrackCivilWar && clan.Kingdom != null && clan.Kingdom != kingdom)
                    {
                        request.RebelKingdomId = clan.Kingdom.StringId; request.State = RequestState.WarCreated;
                        RegisterCreatedWar(request, owner);
                    }
                    else Complete(request, (success ? "" : "叛乱未完成：") + args[9]);
                }
                catch (Exception ex)
                {
                    if (request.TrackCivilWar && clan.Kingdom != null && clan.Kingdom != kingdom)
                    {
                        request.RebelKingdomId = clan.Kingdom.StringId; request.State = RequestState.WarCreated;
                        RegisterCreatedWar(request, owner);
                    }
                    else Complete(request, "叛乱结算异常，已发生变化保留且不会重复建国：" + Error(ex));
                }
                return;
            }
            StartNaming(request, owner, kingdom, clan, followers);
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "bridge_tick_failed: " + ex);
            Request request = _requests.Values.FirstOrDefault(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
            if (request != null && _runningId == null) Complete(request, "政变后叛乱接缝异常，本次结束：" + Error(ex));
        }
    }

    private void StartNaming(Request request, MyBehavior owner, Kingdom kingdom, Clan clan, List<Clan> followers)
    {
        object[] args = { clan, kingdom, request.Week, followers, null, null };
        _af.BuildPrompt.Invoke(owner, args);
        string system = (string)args[4], user = (string)args[5];
        if (request.LoyalistSelection)
            user += "\n本次起兵背景：玩家刚通过武装政变夺位，起兵家族属于旧王家族或更支持旧王的家族；这是反对篡位的起兵，不要求与新王关系为负。请以此为建国命名与简介的背景。";
        if (request.TrackCivilWar)
            user += request.RestoreDynasty ? "\n旧王已获释，本次战争目标为旧王朝复位；仅叛军胜利可复位，和平谈判不复位。"
                : "\n旧王被扣押，旧王及其家族不参与本次内战，不要宣称旧王亲自起兵。";
        string requestId = request.Id;
        string logTarget = "政变后叛乱建国命名 - " + request.ClanId;
        int attempts = _af.NamingAttempts;
        long generation = _generation;
        Campaign campaign = Campaign.Current;
        request.State = RequestState.Naming;
        _runningId = _progressId = request.Id;
        InformationManager.ShowInquiry(new InquiryData("正在生成政变后叛乱命名", "正在使用 AF 原有命名服务生成国名与简介。完成后才会执行家族反出与建国。", false, false, "", "", null, null), true);
        Task.Run(() =>
        {
            var result = new NamingCompletion { Id = requestId, Generation = generation, Owner = owner, Campaign = campaign };
            try
            {
                // Exactly the existing AF naming algorithm/gateway; no new request body or provider.
                object naming = _af.Generate.Invoke(owner, new object[] { system, user, logTarget, attempts });
                result.Json = JsonConvert.SerializeObject(naming);
            }
            catch (Exception ex) { result.Error = Error(ex); }
            _completions.Enqueue(result);
        });
    }

    private void ShowNamingFailure(Request request)
    {
        HideProgress(request.Id);
        _choiceId = request.Id;
        long generation = _generation;
        Campaign campaign = Campaign.Current;
        Func<bool> current = () => ReferenceEquals(Instance, this) && _generation == generation && ReferenceEquals(Campaign.Current, campaign)
            && _requests.TryGetValue(request.Id, out Request live) && ReferenceEquals(live, request) && request.State == RequestState.NamingFailed;
        InformationManager.ShowInquiry(new InquiryData("政变后叛乱命名未成功", "AF 未返回可用的国名与简介，本次尚未迁移家族或建国。可重试原命名服务，或跳过这一次叛乱。"
            + (string.IsNullOrEmpty(request.Message) ? "" : "\n" + request.Message), true, true, "重新生成命名", "跳过本次",
            () => { if (!current()) return; _choiceId = null; request.NamingJson = null; request.State = RequestState.Queued; },
            () => { if (!current()) return; _choiceId = null; Complete(request, "已跳过这一次政变后叛乱，不重新选择家族。"); }), true);
    }

    private void HideProgress(string id)
    {
        if (_progressId != id) return;
        InformationManager.HideInquiry();
        _progressId = null;
    }

    private void Complete(Request request, string message)
    {
        HideProgress(request.Id);
        request.State = RequestState.Completed;
        request.Message = message;
        if (_runningId == request.Id) _runningId = null;
        Logger.Log("Coup", "rebellion_completed coup=" + request.Id + " result=" + message);
        Show(message);
        _hasWork = _requests.Values.Any(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
    }

    internal bool TryRecordCoupOutcome(CoupSession session, Hero formerKing, Settlement settlement, bool success, bool captured, out string message)
    {
        message = "";
        string coupId = session?.Id;
        MyBehavior owner = MyBehavior.Instance;
        if (!Ready(owner) || string.IsNullOrWhiteSpace(coupId) || formerKing == null || settlement == null
            || Hero.MainHero == null || PlayerNotorietyBehavior.Instance == null)
        { message = "AF 政变事实接缝或存档尚未就绪。"; return false; }
        if (!CoupOutcomeReport.CanReport(session, success))
        { message = "政变结果尚未完成实际结算，不能登记最终事实。"; return false; }
        coupId = coupId.Trim();
        if (!_outcomes.TryGetValue(coupId, out Outcome receipt))
        {
            string place = settlement.Name.ToString(), king = formerKing.Name.ToString(), player = Hero.MainHero.Name.ToString();
            receipt = new Outcome
            {
                ActorHeroId = Hero.MainHero.StringId, HeroId = formerKing.StringId, SettlementId = settlement.StringId, KingdomId = (settlement.MapFaction as Kingdom)?.StringId ?? "",
                Success = success, Captured = captured, Day = (int)CampaignTime.Now.ToDays, Date = CampaignTime.Now.ToString(),
                NpcText = success ? player + "在" + place + "发动武装政变并击败了你；这次政变已经成功。" : player + "在" + place + "针对你发动武装政变，但突击失败，随后撤回留守部队。",
                PlayerText = success ? "你在" + place + "针对" + king + "发动武装政变并取得成功。" : "你在" + place + "针对" + king + "发动武装政变失败，撤回留守部队。"
            };
            _outcomes.Add(coupId, receipt);
        }
        if (receipt.HeroId != formerKing.StringId || receipt.SettlementId != settlement.StringId || receipt.Success != success || receipt.Captured != captured)
        { message = "同一政变编号的事实不同，拒绝重复覆盖。"; return false; }
        string key = "coup:" + coupId;
        try
        {
            // Snapshot once so a retry or later rename cannot change the report behind its stable key.
            if (string.IsNullOrEmpty(receipt.BulletinText))
            {
                Kingdom original = Kingdom.All.FirstOrDefault(k => k.StringId == session.KingdomId);
                receipt.OriginalKingdomId = session.KingdomId;
                receipt.ActorKingdomId = (Hero.MainHero.MapFaction as Kingdom)?.StringId ?? "";
                receipt.BulletinText = CoupOutcomeReport.Build(session, Hero.MainHero.Name.ToString(), formerKing.Name.ToString(),
                    settlement.Name.ToString(), original?.Name?.ToString() ?? "原王国", success, captured,
                    original != null && Clan.PlayerClan?.IsAtWarWith(original) == true);
            }
            if (!receipt.HistoryQueued)
            {
                if (!string.IsNullOrEmpty(receipt.RecoveryId))
                {
                    string state = MemoryStatus(receipt);
                    receipt.HistoryQueued = state == "Pending" || state == "Completed";
                }
                if (!receipt.HistoryQueued)
                {
                    var commit = (InteractionMemoryCommit)_af.MemoryConstructor.Invoke(new object[] { key, InteractionChannel.Domain, key, receipt.HeroId, "", "",
                        new[] { new FactRecord("coup_outcome", receipt.HeroId, receipt.NpcText) }, 0L, 0L, key, receipt.Day, 0, receipt.SettlementId, -1, -1, "" });
                    object[] identity = { commit, false, formerKing.Name.ToString(), null, null, null };
                    if (!(bool)_af.PrepareMemory.Invoke(null, identity)) { message = "政变记忆未接受：" + identity[5]; return false; }
                    if (!string.IsNullOrEmpty(receipt.RecoveryId) && (receipt.RecoveryId != (string)identity[3] || receipt.RecoveryHash != (string)identity[4]))
                    { message = "政变记忆恢复身份发生变化，拒绝重复写入。"; return false; }
                    receipt.RecoveryId = (string)identity[3]; receipt.RecoveryHash = (string)identity[4];
                    var result = (MemoryCommitResult)_af.CommitMemory.Invoke(null, new object[] { commit, false, formerKing.Name.ToString() });
                    string state = MemoryStatus(receipt);
                    receipt.HistoryQueued = result.HistoryWritten || state == "Pending";
                    if (!receipt.HistoryQueued)
                    {
                        if (state == "Missing") receipt.RecoveryId = receipt.RecoveryHash = null;
                        message = "政变记忆未完成：" + result.ErrorCode; return false;
                    }
                }
            }
            // Only the causal coup fact is added. Vanilla ruling/capture/land events
            // already create their own facts and must not be manually emitted again.
            if (!receipt.NpcRecorded)
            {
                MyBehavior.RecordNpcActionForExternal(formerKing, receipt.NpcText, key + ":npc", "coup_outcome", true, true, Hero.MainHero, settlement, settlement.Name.ToString(), false, !success);
                receipt.NpcRecorded = true;
            }
            if (!receipt.PlayerRecorded)
            {
                MyBehavior.RecordPlayerActionForExternal(receipt.PlayerText, key + ":player", "coup_outcome", true, formerKing, settlement, settlement.Name.ToString(), success);
                receipt.PlayerRecorded = true;
            }
            if (!receipt.WeeklyRecorded)
            {
                _af.Weekly.Invoke(owner, new object[] { "player_coup", "武装政变 - " + settlement.Name, receipt.PlayerText, key + ":weekly", receipt.KingdomId,
                    receipt.SettlementId, true, true, Hero.MainHero.StringId, (Hero.MainHero.MapFaction as Kingdom)?.StringId ?? "", receipt.Day, receipt.Date });
                receipt.WeeklyRecorded = true;
            }
            if (!receipt.BulletinHandled)
            {
                receipt.BulletinHandled = (bool)_af.Bulletin.Invoke(owner, new object[] {
                    coupId, success, receipt.BulletinText, "发生时间：" + receipt.Date,
                    receipt.OriginalKingdomId, receipt.ActorKingdomId, receipt.ActorHeroId ?? Hero.MainHero.StringId, receipt.HeroId });
                if (!receipt.BulletinHandled) { message = "政变快报事实尚未接受。"; return false; }
            }
            message = "政变事实已按当前设置登记到行动、记忆、周报与快报入口。";
            return true;
        }
        catch (Exception ex) { message = "政变事实登记未完成：" + Error(ex); Logger.Log("Coup", message); return false; }
    }

    private static string MemoryStatus(Outcome receipt) => _af.MemoryStatus.Invoke(null, new object[] { receipt.RecoveryId, receipt.HeroId, receipt.RecoveryHash })?.ToString() ?? "Unavailable";
    private static string Error(Exception ex) => (ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex).Message;
    private static void Show(string message) { if (!string.IsNullOrEmpty(message)) InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】" + message)); }
}
