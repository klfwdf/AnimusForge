using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnimusForge;

internal static class Program
{
    private static int _checks;
    internal static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("FAIL " + message);
        _checks++;
    }

    private static (Kingdom realm, KingdomDecision decision, VoteDealBehavior owner) Scenario(bool ruler, bool foreign = false)
    {
        CampaignTime.Day = 100;
        Kingdom.All.Clear();
        VoteDealBehavior.KingdomAgendaVM._snapshots.Clear();
        Logger.Messages.Clear();
        var player = new Clan { Leader = new Hero { IsHumanPlayerCharacter = true } };
        Clan.PlayerClan = player;
        var own = new Kingdom { StringId = "player_realm", RulingClan = ruler ? player : new Clan { Leader = new Hero() } };
        player.Kingdom = own;
        var realm = foreign ? new Kingdom { StringId = "npc_realm", RulingClan = new Clan { Leader = new Hero() } } : own;
        var decision = new KingdomDecision
        {
            Kingdom = realm, Chooser = realm.RulingClan, IsPlayerParticipant = !foreign,
            Supporters = !foreign && !ruler ? new List<Supporter> { new() { IsPlayer = true } } : new()
        };
        var owner = new VoteDealBehavior();
        CampaignEventDispatcher.Instance = new CampaignEventDispatcher { Handler = owner.DeliverAdded };
        return (realm, decision, owner);
    }

    public static int Main()
    {
        try
        {
            PlayerVassal();
            PlayerRuler();
            ForeignKingdom();
            FailedListener();
            FailureBoundaries();
            NoticeEligibility();
            ManualPlayerVote();
            Console.WriteLine($"PASS bilateral player agenda replay: {_checks} assertions");
            return 0;
        }
        catch (Exception error) { Console.WriteLine(error.Message); return 1; }
    }

    private static void PlayerVassal()
    {
        var (realm, decision, owner) = Scenario(false);
        var record = new BilateralDiplomacyRecord { RecordId = "vassal_peace" };
        bool listenerSawPending = false, listenerSawDeadline = false, listenerSawSnapshot = false;
        CampaignEventDispatcher.Instance.Observer = (d, involved) =>
        {
            listenerSawPending = realm.UnresolvedDecisions.Contains(d);
            listenerSawDeadline = d.TriggerTime.Value == 121;
            listenerSawSnapshot = VoteDealBehavior.KingdomAgendaVM._snapshots.Any(x => x.Decision == d && x.DelayDays == 21);
        };
        Check(VoteDealBehavior.Queue(realm, decision, record, out var reason), "vassal counterpart queues successfully");
        Check(decision.TriggerTime.Value == 121, "vassal counterpart keeps the player kingdom 21-day window");
        Check(reason == "" && record.CounterpartCreated, "successful queue records creation without failure");
        Check(listenerSawPending && listenerSawDeadline && listenerSawSnapshot, "listeners observe the real queue, final deadline and snapshot");
        Check(CampaignEventDispatcher.Instance.LastInvolved, "player-vassal participation reaches event dispatcher");
        Check(owner.PendingCount == 1, "player-vassal initial notification is queued immediately");
        Check(VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(decision), "queued vassal agenda is a valid map notice target");
        var view = new VoteDealBehavior.KingdomAgendaVM { TargetKingdom = realm };
        view.RefreshAgendaItems();
        Check(view.HasItems && view.AgendaItems.Single().Decision == decision, "actual player agenda list contains the pending peace decision");
        view.SelectItem(view.AgendaItems.Single());
        view.RefreshAgendaItems();
        Check(view.SelectedItem?.Decision == decision, "selected peace remains selected across UI refresh");
        CampaignTime.Day = 103.1;
        Check(!VoteDealBehavior.CanAutoVote(decision), "old three-day deadline cannot auto-resolve player peace");
        CampaignTime.Day = 120.99;
        Check(!VoteDealBehavior.CanAutoVote(decision), "no auto-vote immediately before the player deadline");
        CampaignTime.Day = 121.01;
        Check(VoteDealBehavior.CanAutoVote(decision), "existing vassal deadline auto-vote remains available after 21 days");
        realm.UnresolvedDecisions.Clear();
        view.RefreshAgendaItems();
        Check(!view.HasItems && view.SelectedItem == null, "concluded decision leaves the actual agenda list");
        Check(!VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(decision), "concluded decision invalidates its notice");
    }

    private static void PlayerRuler()
    {
        var (realm, decision, owner) = Scenario(true);
        Check(VoteDealBehavior.Queue(realm, decision, new(), out _), "ruler counterpart queues");
        Check(decision.TriggerTime.Value == 121 && owner.PendingCount == 1, "ruler gets the same 21-day window and immediate notice");
        Check(CampaignEventDispatcher.Instance.LastInvolved, "human chooser establishes involvement without a player supporter");
        CampaignTime.Day = 122;
        Check(VoteDealBehavior.RequiresPlayerRulerResolution(decision), "ruler still owns final resolution after the deadline");
        Check(!VoteDealBehavior.CanAutoVote(decision), "ruler counterpart cannot silently auto-vote");
    }

    private static void ForeignKingdom()
    {
        var (realm, decision, owner) = Scenario(false, true);
        Check(VoteDealBehavior.Queue(realm, decision, new(), out _), "NPC counterpart queues");
        Check(decision.TriggerTime.Value == 103, "NPC kingdom retains three-day schedule");
        Check(!CampaignEventDispatcher.Instance.LastInvolved && owner.PendingCount == 0, "NPC-only agenda does not notify player as a participant");
        Check(!VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(decision), "NPC-only agenda is not a player map notice");
        CampaignTime.Day = 102.99;
        Check(!VoteDealBehavior.CanAutoVote(decision), "NPC cannot resolve early");
        CampaignTime.Day = 103.01;
        Check(VoteDealBehavior.CanAutoVote(decision), "NPC can resolve after its existing deadline");
    }

    private static void FailedListener()
    {
        var (realm, decision, owner) = Scenario(false);
        CampaignEventDispatcher.Instance.Observer = (_, _) => throw new InvalidOperationException("synthetic listener failure");
        var record = new BilateralDiplomacyRecord();
        Check(VoteDealBehavior.Queue(realm, decision, record, out var reason), "notification listener failure does not lose a queued agenda");
        Check(record.CounterpartCreated && reason == "" && realm.UnresolvedDecisions.Contains(decision), "listener exception leaves the authoritative pending state intact");
        Check(decision.TriggerTime.Value == 121 && VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(decision), "listener exception preserves timing and eligibility for notification reconciliation");
        var view = new VoteDealBehavior.KingdomAgendaVM { TargetKingdom = realm };
        view.RefreshAgendaItems();
        Check(view.HasItems, "list remains readable even when event delivery failed");
        CampaignEventDispatcher.Instance.Observer = null;
        Check(VoteDealBehavior.Queue(realm, decision, record, out _), "retry of same counterpart succeeds");
        Check(realm.UnresolvedDecisions.Count == 1 && VoteDealBehavior.KingdomAgendaVM._snapshots.Count == 1, "retry cannot duplicate pending item or timing snapshot");
        Check(owner.PendingCount == 1, "retry reaches normal player notification path");
    }

    private static void FailureBoundaries()
    {
        var (realm, decision, owner) = Scenario(false);
        Check(!VoteDealBehavior.Queue(null, decision, new(), out var reason) && reason == "queue_kingdom_null", "missing kingdom is rejected");
        Check(!VoteDealBehavior.Queue(realm, null, new(), out reason) && reason == "queue_decision_null", "missing decision is rejected");
        Check(!VoteDealBehavior.Queue(realm, decision, null, out reason) && reason == "queue_record_null", "missing bilateral record is rejected");
        realm.DropInternalQueue();
        Check(!VoteDealBehavior.Queue(realm, decision, new(), out reason) && reason.StartsWith("unresolved_field_null:"), "missing real queue is rejected");
        Check(CampaignEventDispatcher.Instance.Calls == 0 && owner.PendingCount == 0, "failed admission never announces an absent agenda");
        (realm, decision, owner) = Scenario(false);
        realm.HideQueue = true;
        Check(!VoteDealBehavior.Queue(realm, decision, new(), out reason) && reason.StartsWith("queue_contains_failed:"), "queue exposure failure is detected before publication");
        Check(CampaignEventDispatcher.Instance.Calls == 0 && owner.PendingCount == 0, "unconfirmed queue has no player notification");
    }

    private static void NoticeEligibility()
    {
        var (realm, decision, owner) = Scenario(false);
        decision.ThrowParticipantLookup = true;
        Check(VoteDealBehavior.Queue(realm, decision, new(), out _), "participant lookup exception does not prevent queueing");
        Check(CampaignEventDispatcher.Instance.LastInvolved && owner.PendingCount == 1, "native IsPlayerParticipant fallback still notifies a vassal");
        decision.Cancelled = true;
        Check(!VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(decision), "cancelled agenda invalidates notice");
        var view = new VoteDealBehavior.KingdomAgendaVM { TargetKingdom = realm };
        view.RefreshAgendaItems();
        Check(!view.HasItems, "cancelled agenda is excluded by actual UI method");
    }

    private static void ManualPlayerVote()
    {
        var (realm, decision, owner) = Scenario(false);
        Check(VoteDealBehavior.Queue(realm, decision, new(), out _), "manual-vote case queues");
        var agenda = new VoteDealBehavior.KingdomAgendaVM { TargetKingdom = realm };
        agenda.RefreshAgendaItems();
        var mixin = new VoteDealBehavior.KingdomAgendaVMMixin { Agenda = agenda };
        mixin.OpenVote(decision);
        Check(mixin.ViewModel.Decision.IsActive && mixin.ViewModel.Decision.Selected == decision, "actual StartVoteMeeting lets a player-vassal open the pending peace ballot");
        Check(decision.TriggerTime.Value == CampaignTime.Day, "manual player participation opens the ballot before deadline");
        Check(realm.UnresolvedDecisions.Contains(decision), "opening the ballot does not conclude or remove the peace agenda");
        (realm, decision, owner) = Scenario(false, true);
        Check(VoteDealBehavior.Queue(realm, decision, new(), out _), "foreign manual-vote case queues");
        mixin = new VoteDealBehavior.KingdomAgendaVMMixin();
        mixin.OpenVote(decision);
        Check(!mixin.ViewModel.Decision.IsActive && decision.TriggerTime.Value == 103, "player cannot open or accelerate an NPC-only ballot");
    }
}

// Fake engine boundary. Production queue, deadline, involvement, notification
// admission, snapshot and UI-list methods are extracted unchanged by run.ps1.
public class MBList<T> : List<T> { }
public sealed class Hero { public bool IsHumanPlayerCharacter; }
public sealed class Clan
{
    public static Clan PlayerClan;
    public Hero Leader;
    public Kingdom Kingdom;
    public string StringId = "clan";
}
public sealed class Kingdom
{
    public static readonly List<Kingdom> All = new();
    private MBList<KingdomDecision> _unresolvedDecisions = new();
    public bool HideQueue;
    public MBList<KingdomDecision> UnresolvedDecisions => HideQueue ? null : _unresolvedDecisions;
    public string StringId;
    public Clan RulingClan;
    public Kingdom() => All.Add(this);
    public void DropInternalQueue() => _unresolvedDecisions = null;
}
public sealed class Supporter { public bool IsPlayer; }
public sealed class KingdomDecision
{
    public Kingdom Kingdom;
    public Clan Chooser;
    public Clan ProposerClan => Chooser;
    public List<Supporter> Supporters = new();
    public bool IsPlayerParticipant, IsEnforced, Cancelled, ThrowParticipantLookup;
    public CampaignTime TriggerTime { get; private set; }
    public Clan DetermineChooser() => ThrowParticipantLookup ? throw new InvalidOperationException("synthetic participant failure") : Chooser;
    public IEnumerable<Supporter> DetermineSupporters() => Supporters;
    public bool ShouldBeCancelled() => Cancelled;
    public string GetGeneralTitle() => "Peace";
}
public readonly struct CampaignTime
{
    public static double Day;
    public double Value { get; }
    private CampaignTime(double value) => Value = value;
    public bool IsFuture => Value > Day;
    public bool IsPast => Value <= Day;
    public static CampaignTime Now => new(Day);
    public static CampaignTime DaysFromNow(float days) => new(Day + days);
}
public sealed class BilateralDiplomacyRecord { public string RecordId = "test_peace"; public bool CounterpartCreated; }
public sealed class CampaignEventDispatcher
{
    public static CampaignEventDispatcher Instance;
    public Action<KingdomDecision, bool> Handler, Observer;
    public int Calls;
    public bool LastInvolved;
    public void OnKingdomDecisionAdded(KingdomDecision d, bool involved)
    {
        Calls++; LastInvolved = involved;
        Observer?.Invoke(d, involved);
        Handler?.Invoke(d, involved);
    }
}
public static class Logger
{
    public static readonly List<string> Messages = new();
    public static void Log(string category, string message) => Messages.Add(category + ": " + message);
}
public readonly struct Color { public static Color FromUint(uint value) => new(); }
public sealed class InformationMessage { public InformationMessage(string message, Color color) { } }
public static class InformationManager { public static void DisplayMessage(InformationMessage message) { } }
public static class DiplomacyUiLocalization
{
    public static string Text(string key, string fallback) => fallback;
    public static string Format(string key, string fallback, params (string, object)[] values) => fallback;
}
public static class KingdomAgendaTabState { public static void RequestReturnAfterRefresh(ManagementVm vm) { } }
public sealed class ManagementVm
{
    public readonly BallotVm Decision = new();
    public void OnPropertyChanged(string property) { }
}
public sealed class BallotVm
{
    public KingdomDecision Selected;
    public bool IsActive;
    public void RefreshWith(KingdomDecision decision) => Selected = decision;
}
public sealed class Traverse
{
    private readonly object _instance;
    private string _field, _property;
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private Traverse(object instance) => _instance = instance;
    public static Traverse Create(object instance) => new(instance);
    public Traverse Field(string field) { _field = field; return this; }
    public Traverse Property(string property) { _property = property; return this; }
    public T GetValue<T>() => (T)_instance.GetType().GetField(_field, Flags).GetValue(_instance);
    public void SetValue(object value) => _instance.GetType().GetProperty(_property, Flags).SetValue(_instance, value);
}
public partial class VoteDealBehavior
{
    private static readonly bool AgendaMapNotificationsEnabled = true;
    private readonly HashSet<KingdomDecision> _pendingAgendaMapNotices = new();
    private int _hasPendingAgendaMapNoticeWork;
    private long _nextAgendaMapNoticeRetryUtcTicks;
    public int PendingCount => _pendingAgendaMapNotices.Count;
    public void DeliverAdded(KingdomDecision d, bool involved) => OnAgendaKingdomDecisionAdded(d, involved);
    public static bool Queue(Kingdom k, KingdomDecision d, BilateralDiplomacyRecord r, out string error) => QueueBilateralCounterpartDecision(k, d, r, out error);
    public static bool CanAutoVote(KingdomDecision d) => ShouldAutoStartAgendaVote(d);
    private static string GetSafeDecisionTitle(KingdomDecision d) => "Peace";
    internal static string GetDecisionDisplayTitleForAgenda(KingdomDecision d) => "Peace";
    internal static string GetDecisionKingdomIdForDiagnostics(KingdomDecision d) => d?.Kingdom?.StringId;
    internal static Kingdom ResolveKingdom(string id) => Kingdom.All.FirstOrDefault(x => x.StringId == id);
    internal static bool TryGetRedundantSingleClanFiefAgenda(Kingdom k, KingdomDecision d, out object a, out object b) { a = b = null; return false; }
    public partial class KingdomAgendaVM
    {
        public sealed class Snapshot
        {
            public KingdomDecision Decision;
            public string KingdomId;
            public CampaignTime CreatedAt;
            public float DelayDays;
            public bool IsPlayerKingdom;
        }
        public static readonly List<Snapshot> _snapshots = new();
        public Kingdom TargetKingdom;
        public readonly List<KingdomAgendaItemVM> AgendaItems = new();
        public KingdomAgendaItemVM SelectedItem;
        public bool HasItems;
        private static void CleanupSnapshots() { }
        private void OnPropertyChanged(string property) { }
        public void SelectItem(KingdomAgendaItemVM item) => SelectedItem = item;
        public void ClearSelection() => SelectedItem = null;
    }
    public partial class KingdomAgendaVMMixin
    {
        public KingdomAgendaVM Agenda;
        public readonly ManagementVm ViewModel = new();
        public void OpenVote(KingdomDecision d) => StartVoteMeeting(d);
    }
    public sealed class KingdomAgendaItemVM
    {
        public KingdomDecision Decision;
        public KingdomAgendaVM Parent;
        public bool IsSelected;
        public KingdomAgendaItemVM(KingdomDecision d) => Decision = d;
    }
}
