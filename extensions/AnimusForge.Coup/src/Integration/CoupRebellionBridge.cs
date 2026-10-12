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
using TaleWorlds.Localization;
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
        // These four gained one trailing optional argument (existing names / captured clan facts).
        // The bridge always passes null, which is the host's own default and keeps the old behaviour.
        internal readonly MethodInfo BuildPrompt = Method("BuildRebelKingdomNamingRequest", 7);
        internal readonly MethodInfo Generate = Method("GenerateRebelKingdomNamingFromPrompts", 5);
        internal readonly MethodInfo Execute = Method("TryExecuteKingdomRebellionWithNaming", 10);
        internal readonly MethodInfo ValidateClan = Method("TryValidateClanForKingdomRebellion", 8);
        internal readonly MethodInfo ValidateFollower = Method("TryValidateClanForRebelFollower", 10);
        internal readonly MethodInfo FollowerEligible = Method("IsEligibleRebelFollowerByStandardRules", 3);
        internal readonly MethodInfo NamingSucceeded = Method("IsRebelKingdomNamingSuccess", 1);
        internal readonly MethodInfo Weekly = Method("RecordEventSourceMaterial", 12);
        internal readonly MethodInfo Bulletin = Method("TryRecordCoupOutcomeWithParticipantsForBulletin", 8);
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
        internal readonly CoupMemoryAccess Memory = new CoupMemoryAccess();

        internal AfAccess()
        {
            Type byRefString = typeof(string).MakeByRefType(), byRefInt = typeof(int).MakeByRefType();
            Require(CanTrackCoupWar, typeof(bool), false, typeof(Kingdom));
            Require(RegisterCoupWar, typeof(bool), false, typeof(string), typeof(Kingdom), typeof(Kingdom), typeof(Clan), typeof(Hero), typeof(bool), typeof(string), typeof(string), byRefString);
            Require(Resolve, SelectedClan.DeclaringType, false, typeof(Kingdom), typeof(int), typeof(bool), typeof(bool));
            // RebellionClanFacts is internal to AF; resolve it by name so the standalone build compiles too.
            Type capturedFacts = typeof(Dictionary<,>).MakeGenericType(typeof(Clan),
                typeof(MyBehavior).Assembly.GetType("AnimusForge.RebellionClanFacts")
                    ?? throw new MissingMemberException("AF RebellionClanFacts"));
            Require(BuildPrompt, typeof(void), false, typeof(Clan), typeof(Kingdom), typeof(int), typeof(IEnumerable<Clan>), byRefString, byRefString, typeof(IReadOnlyCollection<string>));
            Require(Generate, NamingType, false, typeof(string), typeof(string), typeof(string), typeof(int), typeof(IReadOnlyCollection<string>));
            Require(Execute, typeof(bool), false, typeof(Clan), typeof(Kingdom), typeof(int), typeof(bool), typeof(int), typeof(int), typeof(int), NamingType, typeof(List<Clan>), byRefString);
            Require(ValidateClan, typeof(bool), false, typeof(Clan), typeof(Kingdom), typeof(bool), byRefString, byRefInt, byRefInt, byRefInt, capturedFacts);
            Require(ValidateFollower, typeof(bool), false, typeof(Clan), typeof(Kingdom), typeof(Clan), typeof(bool), byRefString, byRefInt, byRefInt, byRefInt, byRefInt, capturedFacts);
            // Passing null for the new trailing argument is only equivalent to the old call while it stays an optional null default.
            foreach (MethodInfo appended in new[] { BuildPrompt, Generate, ValidateClan, ValidateFollower })
            {
                ParameterInfo last = appended.GetParameters().Last();
                if (!last.IsOptional || last.DefaultValue != null)
                    throw new MissingMethodException("AF optional trailing argument changed: " + appended.Name);
            }
            Require(FollowerEligible, typeof(bool), true, typeof(int), typeof(int), typeof(float));
            Require(NamingSucceeded, typeof(bool), true, NamingType);
            Require(Weekly, typeof(void), false, typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(int), typeof(string));
            Require(Bulletin, typeof(bool), false, typeof(string), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            if (SelectedClan.FieldType != typeof(Clan) || SelectedFollowers.FieldType != typeof(List<Clan>) || ResolutionMessage.FieldType != typeof(string)
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
        if (!_saveValid) { Show(new TextObject("{=AF_COUPX_SaveInvalid}The coup aftermath rebellion save is invalid; automatic replay has been disabled and the original record retained.").ToString()); return; }
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
        if (store.IsLoading)
        {
            _requestsJson = CampaignSaveChunkHelper.LoadChunkedString(store, "_afCoupRebellionBridge_v1", "Coup");
            _outcomesJson = CampaignSaveChunkHelper.LoadChunkedString(store, "_afCoupOutcomeBridge_v1", "Coup");
        }
        else if (store.IsSaving)
        {
            CampaignSaveChunkHelper.SaveChunkedString(store, "_afCoupRebellionBridge_v1", _requestsJson, "Coup");
            CampaignSaveChunkHelper.SaveChunkedString(store, "_afCoupOutcomeBridge_v1", _outcomesJson, "Coup");
        }
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
        { message = CoupSourceTexts.Get("SeamNotReady"); return false; }
        coupId = coupId.Trim();
        if (_requests.TryGetValue(coupId, out Request existing))
        { message = existing.Message; return existing.KingdomId == kingdom.StringId; }
        try
        {
            int week = Math.Max(0, (int)CampaignTime.Now.ToDays / 7);
            bool restore = session.Disposition == CoupKingDisposition.Release;
            List<CoupLoyalistCandidate> candidates;
            if (!(bool)_af.CanTrackCoupWar.Invoke(owner, new object[] { kingdom }))
            { candidates = new List<CoupLoyalistCandidate>(); message = CoupSourceTexts.Get("CivilWarDisabled"); }
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
            if (clan != null) { _hasWork = true; request.Message = CoupSourceTexts.Format("RebellionRegistered", clan.Name, request.Followers.Count); }
            message = request.Message;
            Logger.Log("Coup", "rebellion_registered coup=" + coupId + " state=" + request.State + " leader=" + request.ClanId + " followers=" + request.Followers.Count + " reason=" + request.Message);
            return true;
        }
        catch (Exception ex) { message = CoupSourceTexts.Format("RegistrationFailed", Error(ex)); Logger.Log("Coup", "rebellion_registration_failed coup=" + coupId + " error=" + Error(ex)); return false; }
    }

    private List<CoupLoyalistCandidate> SelectLoyalists(MyBehavior owner, Kingdom kingdom, string formerKingId, string formerClanId, bool allowFormerClan, out string message)
    {
        var candidates = new List<CoupLoyalistCandidate>();
        message = CoupSourceTexts.Get("NoEligibleLoyalist");
        if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
        { message = CoupSourceTexts.Get("StabilityDisabled"); return candidates; }
        if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
        { message = CoupSourceTexts.Get("ImmunityEnabled"); return candidates; }
        Hero formerKing = Hero.Find(formerKingId);
        // Heroes belong to CampaignObjectManager, not the XML object registry.
        // Log IDs once per settlement attempt, never per engine tick or candidate.
        Logger.Log("Coup", "loyalist_identity_resolution registry=CampaignObjectManager kingdom=" + kingdom?.StringId
            + " formerKing=" + formerKingId + " formerFound=" + (formerKing != null)
            + " formerClan=" + formerClanId + " currentKing=" + kingdom?.Leader?.StringId);
        if (formerKing == null || kingdom?.Leader == null)
            throw new InvalidOperationException(CoupSourceTexts.Get("IdentityMissing"));
        foreach (Clan candidate in kingdom.Clans)
        {
            object[] args = { candidate, kingdom, true, null, 0, 0, 0, null };
            bool eligible = (bool)_af.ValidateClan.Invoke(owner, args);
            int oldRelation = 0, newRelation = (int)args[4];
            bool formerFamily = candidate?.StringId == formerClanId;
            if (formerFamily && !allowFormerClan) { eligible = false; args[3] = CoupSourceTexts.Get("FormerClanExcluded"); }
            if (eligible)
            {
                oldRelation = candidate.Leader == formerKing ? 100 : candidate.Leader.GetRelation(formerKing);
                eligible = CoupLoyalistPolicy.Opposes(formerFamily, oldRelation, newRelation);
                if (!eligible) args[3] = CoupSourceTexts.Get("SupportInsufficient");
            }
            Logger.Log("Coup", "loyalist_candidate clan=" + candidate?.StringId + " oldFamily=" + formerFamily
                + " oldRelation=" + oldRelation + " newRelation=" + newRelation + " eligible=" + eligible + " note=" + args[3]);
            if (eligible) candidates.Add(new CoupLoyalistCandidate { ClanId = candidate.StringId, FormerRulingClan = formerFamily,
                RelationToOldKing = oldRelation, RelationToNewKing = newRelation, Fortifications = (int)args[5] + (int)args[6], ClanTier = candidate.Tier });
        }
        return CoupLoyalistPolicy.Rank(candidates);
    }

    private static bool StillSupportsFormerKing(Request request, Clan clan, Kingdom kingdom, Hero formerKing)
    {
        if (clan?.Leader == null || kingdom?.Leader == null) return false;
        if (clan.StringId == request.FormerRulingClanId) return true;
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
        kingdom = Campaign.Current.CampaignObjectManager.Find<Kingdom>(request.KingdomId);
        clan = Campaign.Current.CampaignObjectManager.Find<Clan>(request.ClanId);
        followers = new List<Clan>();
        message = "";
        if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled()) { message = CoupSourceTexts.Get("StabilityEndSession"); return false; }
        if (kingdom != null && PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)) { message = CoupSourceTexts.Get("ImmunityEndSession"); return false; }
        if (kingdom == null || kingdom.IsEliminated || Campaign.Current.KingdomManager == null
            || kingdom.RulingClan?.StringId != request.RulingClanId || kingdom.Leader?.StringId != request.RulerId
            || clan?.Leader?.StringId != request.ClanLeaderId)
        { message = CoupSourceTexts.Get("ContextChanged"); return false; }
        object[] args = { clan, kingdom, request.LoyalistSelection, null, 0, 0, 0, null };
        if (!(bool)_af.ValidateClan.Invoke(owner, args)) { message = CoupSourceTexts.Format("ValidationFailed", args[3]); return false; }
        // Resolve once for this validation; followers reuse the same main-thread snapshot.
        Hero formerKing = request.LoyalistSelection ? Hero.Find(request.FormerKingId) : null;
        if (request.LoyalistSelection && !StillSupportsFormerKing(request, clan, kingdom, formerKing))
        { message = CoupSourceTexts.Get("SupportChanged"); return false; }
        request.Relation = (int)args[4]; request.Towns = (int)args[5]; request.Castles = (int)args[6];
        if (request.TrackCivilWar && !(bool)_af.CanTrackCoupWar.Invoke(owner, new object[] { kingdom }))
        { message = CoupSourceTexts.Get("CivilWarEndSession"); return false; }
        if (request.TrackCivilWar && request.RestoreDynasty && !CanJoinRestoration(request, kingdom, out _))
        { message = CoupSourceTexts.Get("FormerClanIneligible"); return false; }
        foreach (string id in request.Followers)
        {
            Clan follower = Campaign.Current.CampaignObjectManager.Find<Clan>(id);
            if (request.TrackCivilWar && !request.RestoreDynasty && id == request.FormerRulingClanId) continue;
            object[] followerArgs = { follower, kingdom, clan, false, null, 0, 0, 0, 0, null };
            if ((bool)_af.ValidateFollower.Invoke(owner, followerArgs)
                && (request.LoyalistSelection ? StillSupportsFormerKing(request, follower, kingdom, formerKing)
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
        dynasty = Campaign.Current.CampaignObjectManager.Find<Clan>(request.FormerRulingClanId);
        Hero formerKing = Hero.Find(request.FormerKingId);
        return formerKing != null && (!formerKing.IsAlive || !formerKing.IsPrisoner)
            && dynasty != null && !dynasty.IsEliminated && dynasty != Clan.PlayerClan && dynasty.Kingdom == kingdom
            && dynasty.Leader?.IsAlive == true && !dynasty.Leader.IsChild && !dynasty.Leader.IsPrisoner;
    }

    // A real rebel kingdom already exists. Failure here may retry registration, never creation.
    private void RegisterCreatedWar(Request request, MyBehavior owner)
    {
        try
        {
            Kingdom home = Campaign.Current.CampaignObjectManager.Find<Kingdom>(request.KingdomId);
            Kingdom rebel = Campaign.Current.CampaignObjectManager.Find<Kingdom>(request.RebelKingdomId);
            Clan leader = Campaign.Current.CampaignObjectManager.Find<Clan>(request.ClanId);
            Hero formerKing = Hero.Find(request.FormerKingId);
            if (home == null || rebel == null || leader?.Kingdom != rebel || formerKing == null)
                throw new InvalidOperationException(CoupSourceTexts.Get("ParticipantsUnavailable"));
            if (request.RestoreDynasty)
            {
                Clan dynasty = Campaign.Current.CampaignObjectManager.Find<Clan>(request.FormerRulingClanId);
                if (dynasty?.Kingdom == home && CanJoinRestoration(request, home, out _))
                    ChangeKingdomAction.ApplyByJoinToKingdomByDefection(dynasty, home, rebel, default(CampaignTime), true);
                if (dynasty?.Kingdom != rebel) throw new InvalidOperationException(CoupSourceTexts.Get("DynastyNotJoined"));
            }
            object[] args = { request.Id, home, rebel, leader, formerKing, request.RestoreDynasty, request.OriginalName, request.OriginalShortName, null };
            if (!(bool)_af.RegisterCoupWar.Invoke(owner, args)) throw new InvalidOperationException(args[8] as string);
            Complete(request, args[8] as string);
        }
        catch (Exception ex)
        {
            request.Message = CoupSourceTexts.Format("WarRegistrationPending", Error(ex));
            request.RegistrationBlocked = true;
            _hasRegistrationFailure = true;
            _hasWork = _requests.Values.Any(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
            Logger.Log("Coup", "war_registration_pending coup=" + request.Id + " rebel=" + request.RebelKingdomId + " reason=" + request.Message);
            Show(request.Message, CoupUiText.WarRetryHint);
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
                    request.Message = naming == null ? CoupSourceTexts.Get("NamingEmpty") : _af.NamingFailure.GetValue(naming) as string;
                    request.State = RequestState.NamingFailed; ShowNamingFailure(request); return;
                }
                // A partial political mutation cannot be safely replayed on load.
                request.State = RequestState.Completed;
                request.Message = CoupSourceTexts.Get("ExecutionConsumed");
                object[] args = { clan, kingdom, request.Week, true, request.Relation, request.Towns, request.Castles, naming, followers, null };
                try
                {
                    bool success = (bool)_af.Execute.Invoke(owner, args);
                    if (request.TrackCivilWar && clan.Kingdom != null && clan.Kingdom != kingdom)
                    {
                        request.RebelKingdomId = clan.Kingdom.StringId; request.State = RequestState.WarCreated;
                        RegisterCreatedWar(request, owner);
                    }
                    else Complete(request, success ? "" : CoupSourceTexts.Format("RebellionIncomplete", args[9]));
                }
                catch (Exception ex)
                {
                    if (request.TrackCivilWar && clan.Kingdom != null && clan.Kingdom != kingdom)
                    {
                        request.RebelKingdomId = clan.Kingdom.StringId; request.State = RequestState.WarCreated;
                        RegisterCreatedWar(request, owner);
                    }
                    else Complete(request, CoupSourceTexts.Format("SettlementException", Error(ex)));
                }
                return;
            }
            StartNaming(request, owner, kingdom, clan, followers);
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "bridge_tick_failed: " + ex);
            Request request = _requests.Values.FirstOrDefault(r => r.State != RequestState.Completed && !r.RegistrationBlocked);
            if (request != null && _runningId == null) Complete(request, CoupSourceTexts.Format("SeamException", Error(ex)));
        }
    }

    private void StartNaming(Request request, MyBehavior owner, Kingdom kingdom, Clan clan, List<Clan> followers)
    {
        object[] args = { clan, kingdom, request.Week, followers, null, null, null };
        _af.BuildPrompt.Invoke(owner, args);
        string system = (string)args[4], user = (string)args[5];
        if (request.LoyalistSelection)
            user += CoupPromptTexts.Get("NamingContext");
        if (request.TrackCivilWar)
            user += CoupPromptTexts.Get(request.RestoreDynasty ? "RestorationContext" : "CapturedContext");
        string requestId = request.Id;
        string logTarget = "Post-coup rebellion kingdom naming - " + request.ClanId;
        int attempts = _af.NamingAttempts;
        long generation = _generation;
        Campaign campaign = Campaign.Current;
        request.State = RequestState.Naming;
        _runningId = _progressId = request.Id;
        InformationManager.ShowInquiry(new InquiryData(CoupUiText.NamingProgressTitle, CoupUiText.NamingProgressBody, false, false, "", "", null, null), true);
        Task.Run(() =>
        {
            var result = new NamingCompletion { Id = requestId, Generation = generation, Owner = owner, Campaign = campaign };
            try
            {
                // Exactly the existing AF naming algorithm/gateway; no new request body or provider.
                object naming = _af.Generate.Invoke(owner, new object[] { system, user, logTarget, attempts, null });
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
        InformationManager.ShowInquiry(new InquiryData(CoupUiText.NamingFailureTitle, CoupUiText.NamingFailureBody(request.Message), true, true, CoupUiText.NamingRetry, CoupUiText.NamingSkip,
            () => { if (!current()) return; _choiceId = null; request.NamingJson = null; request.State = RequestState.Queued; },
            () => { if (!current()) return; _choiceId = null; Complete(request, CoupUiText.NamingSkipped); }), true);
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
        { message = CoupSourceTexts.Get("OutcomeSeamNotReady"); return false; }
        if (!CoupOutcomeReport.CanReport(session, success))
        { message = CoupSourceTexts.Get("OutcomeIncomplete"); return false; }
        coupId = coupId.Trim();
        if (!_outcomes.TryGetValue(coupId, out Outcome receipt))
        {
            string place = settlement.Name.ToString(), king = formerKing.Name.ToString(), player = Hero.MainHero.Name.ToString();
            receipt = new Outcome
            {
                ActorHeroId = Hero.MainHero.StringId, HeroId = formerKing.StringId, SettlementId = settlement.StringId, KingdomId = (settlement.MapFaction as Kingdom)?.StringId ?? "",
                Success = success, Captured = captured, Day = (int)CampaignTime.Now.ToDays, Date = CampaignTime.Now.ToString(),
                NpcText = success ? CoupSourceTexts.Format("NpcFactSuccess", player, place) : CoupSourceTexts.Format("NpcFactFailure", player, place),
                PlayerText = success ? CoupSourceTexts.Format("PlayerFactSuccess", king, place) : CoupSourceTexts.Format("PlayerFactFailure", king, place)
            };
            _outcomes.Add(coupId, receipt);
        }
        if (receipt.HeroId != formerKing.StringId || receipt.SettlementId != settlement.StringId || receipt.Success != success || receipt.Captured != captured)
        { message = CoupSourceTexts.Get("OutcomeMismatch"); return false; }
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
                    settlement.Name.ToString(), original?.Name?.ToString() ?? CoupSourceTexts.Get("DefaultOriginalKingdom"), success, captured,
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
                    InteractionMemoryCommit commit = _af.Memory.CreateCommit(key, receipt.HeroId,
                        receipt.NpcText, receipt.Day, receipt.SettlementId);
                    string npcName = formerKing.Name.ToString();
                    if (!_af.Memory.Prepare(commit, npcName, out string recoveryId,
                        out string payloadHash, out string errorCode))
                    { message = CoupSourceTexts.Format("MemoryRejected", errorCode); return false; }
                    if (!string.IsNullOrEmpty(receipt.RecoveryId) && (receipt.RecoveryId != recoveryId || receipt.RecoveryHash != payloadHash))
                    { message = CoupSourceTexts.Get("MemoryIdentityChanged"); return false; }
                    receipt.RecoveryId = recoveryId; receipt.RecoveryHash = payloadHash;
                    MemoryCommitResult result = _af.Memory.Commit(commit, npcName);
                    string state = MemoryStatus(receipt);
                    receipt.HistoryQueued = result.HistoryWritten || state == "Pending";
                    if (!receipt.HistoryQueued)
                    {
                        if (state == "Missing") receipt.RecoveryId = receipt.RecoveryHash = null;
                        message = CoupSourceTexts.Format("MemoryIncomplete", result.ErrorCode); return false;
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
                _af.Weekly.Invoke(owner, new object[] { "player_coup", CoupSourceTexts.Format("WeeklyTitle", settlement.Name.ToString()), receipt.PlayerText, key + ":weekly", receipt.KingdomId,
                    receipt.SettlementId, true, true, Hero.MainHero.StringId, (Hero.MainHero.MapFaction as Kingdom)?.StringId ?? "", receipt.Day, receipt.Date });
                receipt.WeeklyRecorded = true;
            }
            if (!receipt.BulletinHandled)
            {
                receipt.BulletinHandled = (bool)_af.Bulletin.Invoke(owner, new object[] {
                    coupId, success, receipt.BulletinText, CoupSourceTexts.Format("BulletinTimeLabel", receipt.Date),
                    receipt.OriginalKingdomId, receipt.ActorKingdomId, receipt.ActorHeroId ?? Hero.MainHero.StringId, receipt.HeroId });
                if (!receipt.BulletinHandled) { message = CoupSourceTexts.Get("BulletinRejected"); return false; }
            }
            message = CoupSourceTexts.Get("FactsRegistered");
            return true;
        }
        catch (Exception ex) { message = CoupSourceTexts.Format("FactRegistrationFailed", Error(ex)); Logger.Log("Coup", "outcome_registration_failed coup=" + coupId + " error=" + Error(ex)); return false; }
    }

    private static string MemoryStatus(Outcome receipt) => _af.Memory.GetStatus(receipt.RecoveryId, receipt.HeroId, receipt.RecoveryHash) ?? "Unavailable";
    private static string Error(Exception ex) => (ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex).Message;
    private static void Show(string message, string suffix = null) { if (!string.IsNullOrEmpty(message)) InformationManager.DisplayMessage(new InformationMessage(CoupUiText.Prefix + CoupUiText.LocalizeReason(message) + (suffix ?? string.Empty))); }
}
