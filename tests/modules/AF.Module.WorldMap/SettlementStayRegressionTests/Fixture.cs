// Only campaign state, movement side effects and unrelated infrastructure are faked.
// The runner injects production query/conversion/tick/model methods without rewriting them.
using System;
using System.Collections.Generic;

class Hero { public string StringId = "hero"; }
class Settlement
{
    public string StringId = "town";
    public bool IsFortification = true, IsUnderSiege, Hostile;
    public SiegeEvent SiegeEvent;
}
class SiegeEvent { public Settlement BesiegedSettlement; public BesiegerCamp BesiegerCamp; }
class BesiegerCamp { public MobileParty LeaderParty; }
class MobileParty
{
    public enum NavigationType { Default }
    public static MobileParty MainParty = new MobileParty();
    public string StringId = "party";
    public Hero LeaderHero;
    public Settlement CurrentSettlement, TargetSettlement;
    public bool IsActive = true, Locked = true, NearGate, Hostile;
    public object Party = new object();
}
class SetPartyAiAction
{
    public static int Visits;
    public static void GetActionForVisitingSettlement(MobileParty p, Settlement s, MobileParty.NavigationType n, bool isFromPort, bool isTargetingPort)
    { Visits++; p.TargetSettlement = s; }
}
class NativeModel
{
    public bool Enabled = true;
    public virtual bool ShouldPartyCheckInitiativeBehavior(MobileParty p) => Enabled;
}
class CourierMobilePartyAIModel : NativeModel
{
    public NativeModel _inner = new NativeModel();
    // MODEL_METHOD
}
class WorldMapPartyCommandBehavior
{
    enum CommandKind { GoToSettlement, PatrolSettlement, AttackParty, AttackHero }
    enum CommandStage { New, Traveling, Active }
    enum CommandMessageTone { Progress }
    class PartyCommandEntry
    {
        public string Kind = "GoToSettlement", TargetId = "town", TargetType = "settlement", Mode;
        public int Days = 3;
        public double HoldUntilDay = -1;
        public bool RequiresExistingWar;
    }
    class PartyCommandQueueState
    {
        public List<PartyCommandEntry> Commands = new List<PartyCommandEntry>();
        public int CurrentIndex;
        public double ArrivalDay = -1, TimeoutDay = -1;
        public string SourceId, PendingSafeExitAction, LastIssuedActionKey, Stage;
        public bool EngageCommitted;
    }
    const string PendingSafeExitStop = "stop", AttackModeForce = "FORCE", AttackModeAi = "AI";
    const float SettlementArrivalDistance = 3;
    static WorldMapPartyCommandBehavior Instance;
    readonly object _queueLock = new object();
    readonly Dictionary<string, PartyCommandQueueState> _queues = new Dictionary<string, PartyCommandQueueState>(StringComparer.OrdinalIgnoreCase);
    static Settlement Target;
    static double Day;
    static int Started, Completed, Checks;
    static bool BlockedByBattle;
    static string BuildPartyActorKey(MobileParty p, bool createGuid) => "party:" + p.StringId;
    static Settlement ResolveSettlementById(string id) => Target?.StringId == id ? Target : null;
    static bool IsPartyAtWarWithSettlement(MobileParty p, Settlement s) => s?.Hostile == true;
    static bool IsSupportedAttackSettlement(Settlement s) => s != null;
    static bool ArePartiesAtWar(MobileParty a, MobileParty b) => b.Hostile;
    static string NormalizeExternalSourceId(string source) => source;
    static double NowDay() => Day;
    static bool IsPartyAtSettlement(MobileParty p, Settlement s, float d) => p.CurrentSettlement == s || p.NearGate;
    static bool IsPartyVisitingSettlement(MobileParty p, Settlement s) => p.TargetSettlement == s;
    static bool IsAiDecisionLockActive(MobileParty p) => p.Locked;
    static void LockPartyAi(MobileParty p) => p.Locked = true;
    static bool PreemptBlockingWorldActivityForCommand(Hero h, MobileParty p, PartyCommandEntry c, PartyCommandQueueState s, string phase) => !BlockedByBattle;
    static void SynchronizeArmyObjectiveForCommand(MobileParty p, PartyCommandEntry c) { }
    void StartCurrentCommand(Hero h, MobileParty p, PartyCommandQueueState s) { Started++; }
    void AdvanceCommand(Hero h, MobileParty p, PartyCommandQueueState s, string why) { Completed++; s.CurrentIndex++; p.Locked = false; }
    static void LogFact(PartyCommandQueueState s, Hero h, string text) { }
    static void Log(string text) { }
    static void NotifyCommandStatus(PartyCommandQueueState s, string key, string text, CommandMessageTone tone) { }
    static string GetActorName(PartyCommandQueueState s, Hero h, MobileParty p) => "actor";
    static string GetActorLogId(PartyCommandQueueState s, Hero h, MobileParty p) => "actor";
    static string GetSettlementName(Settlement s) => s?.StringId;
    static string GetPartyName(MobileParty p) => p?.StringId;
    static string DescribePartyAi(MobileParty p) => "fixture";
    // OWNER_METHODS

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        Checks++;
    }
    public static void Main()
    {
        Instance = new WorldMapPartyCommandBehavior();
        Target = new Settlement();
        var hero = new Hero();
        var party = new MobileParty { LeaderHero = hero, CurrentSettlement = Target, TargetSettlement = Target };
        var command = new PartyCommandEntry();
        var state = new PartyCommandQueueState { Commands = new List<PartyCommandEntry> { command }, ArrivalDay = 10 };
        Instance._queues[hero.StringId] = state;
        var model = new CourierMobilePartyAIModel();
        Check(!model.ShouldPartyCheckInitiativeBehavior(party), "settled defender suppresses native initiative before siege");
        var besieger = new MobileParty { StringId = "enemy", Hostile = true };
        Target.IsUnderSiege = true;
        Target.SiegeEvent = new SiegeEvent { BesiegedSettlement = Target, BesiegerCamp = new BesiegerCamp { LeaderParty = besieger } };
        Check(!TryBuildGoToSettlementAttackCommand(party, command, out _, out _, out _), "settled defender must not convert stay into force attack");
        Check(!model.ShouldPartyCheckInitiativeBehavior(party), "native siege exception cannot override stay");
        Day = 11;
        Instance.TickGoToSettlement(hero, party, state, command);
        Check(command.Kind == "GoToSettlement" && Started == 0 && Completed == 0 && state.ArrivalDay == 10, "hourly siege retains stay and its original timer");
        party.Locked = false;
        Instance.TickGoToSettlement(hero, party, state, command);
        Check(party.Locked && SetPartyAiAction.Visits == 1, "lost AI lock restored while still inside town");
        party.TargetSettlement = null;
        Instance.TickGoToSettlement(hero, party, state, command);
        Check(party.TargetSettlement == Target && SetPartyAiAction.Visits == 2, "changed native objective restored while inside town");
        Instance.TickGoToSettlement(hero, party, state, command);
        Check(SetPartyAiAction.Visits == 2, "stable stay does not repeatedly reissue movement");
        party.Locked = false; BlockedByBattle = true;
        Instance.TickGoToSettlement(hero, party, state, command);
        Check(!party.Locked && SetPartyAiAction.Visits == 2, "active battle not interrupted to refresh AI");
        party.Locked = true; BlockedByBattle = false;
        Day = 13;
        Instance.TickGoToSettlement(hero, party, state, command);
        Check(Completed == 1 && model.ShouldPartyCheckInitiativeBehavior(party), "expiry advances queue and removes protection");
        state.CurrentIndex = 0; state.ArrivalDay = -1;
        Check(!model.ShouldPartyCheckInitiativeBehavior(party), "entry before hourly arrival bookkeeping already protected");
        state.PendingSafeExitAction = "stop";
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "pending stop removes stay protection");
        state.PendingSafeExitAction = null;
        Instance._queues.Clear();
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "cleared queue restores native initiative");
        Instance._queues[hero.StringId] = state;
        command.Kind = "PatrolSettlement";
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "patrol may engage enemies");
        command.Kind = "AttackParty";
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "explicit attack may leave settlement");
        command.Kind = "GoToSettlement";
        party.CurrentSettlement = null; party.NearGate = true; state.ArrivalDay = 10;
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "gate proximity is not being inside walls");
        Check(TryBuildGoToSettlementAttackCommand(party, command, out var attack, out _, out _) && attack.Kind == "AttackParty" && attack.Mode == "FORCE" && attack.RequiresExistingWar, "outside relief remains forced attack on hostile besieger");
        party.CurrentSettlement = new Settlement { StringId = "other" };
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "staying in wrong settlement not protected");
        party.CurrentSettlement = Target;
        Target.Hostile = true; Target.IsUnderSiege = false; Target.SiegeEvent = null;
        Check(TryBuildGoToSettlementAttackCommand(party, command, out attack, out _, out _) && attack.TargetType == "settlement" && attack.Mode == "AI", "hostile destination retains siege conversion");
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "hostile settlement not treated as friendly stay");
        Target.Hostile = false; Target.IsFortification = false;
        Check(model.ShouldPartyCheckInitiativeBehavior(party), "village excluded from inside-walls protection");
        Target.IsFortification = true;
        party.LeaderHero = null; Instance._queues.Clear(); Instance._queues[BuildPartyActorKey(party, false)] = state;
        Check(!model.ShouldPartyCheckInitiativeBehavior(party), "nonhero ordered party uses its own queue");
        Check(model.ShouldPartyCheckInitiativeBehavior(null), "null actor passes through");
        MobileParty.MainParty.CurrentSettlement = Target;
        Check(model.ShouldPartyCheckInitiativeBehavior(MobileParty.MainParty), "player main party excluded");
        Instance._queues.Clear(); model._inner.Enabled = false;
        Check(!model.ShouldPartyCheckInitiativeBehavior(party), "inner AI model decision preserved");
        Console.WriteLine("PASS: " + Checks + " settlement stay behavioral checks (fake campaign; production methods)");
    }
}
