// Template compiled only after run.py inserts current production method bodies.
using System;
using System.Linq;
using System.Collections.Generic;

enum AgentState { None, Active, Unconscious, Killed, Routed, Deleted }
class Hero { public string Name; public CharacterObject CharacterObject; }
class CharacterObject { public Hero HeroObject; }
class Agent {
    public enum MortalityState { Mortal, Invulnerable }
    public static Agent Main;
    public CharacterObject Character; public int Index; public bool IsMainAgent, IsRunningAway;
    public AgentState State = AgentState.Active; public float Health = 100, HealthLimit = 100;
    public bool IsActive() => State == AgentState.Active;
    public void SetMortalityState(MortalityState value) { }
}
class Mission {
    public static Mission Current; public List<Agent> Agents = new(); public Agent MainAgent;
    public bool IsMissionEnding; public float CurrentTime; public string SceneName = "town", Mode = "Battle"; public object InputManager = new(); public int Ends;
    public List<MissionBehavior> Behaviors = new();
    public T GetMissionBehavior<T>() where T : MissionBehavior => Behaviors.OfType<T>().FirstOrDefault();
    public void Add(MissionBehavior behavior) { behavior.Mission = this; Behaviors.Add(behavior); }
    public void EndMission() { Ends++; IsMissionEnding = true; }
}
class ConversationManager { public bool IsConversationInProgress; }
class Campaign { public static Campaign Current = new(); public ConversationManager ConversationManager = new(); }
enum MissionBehaviorType { Other }
class KillingBlow { }
interface IAgentStateDecider { }
interface IMissionBehavior { }
class MissionBehavior { public Mission Mission; public virtual MissionBehaviorType BehaviorType => MissionBehaviorType.Other; public virtual void OnAgentRemoved(Agent a, Agent b, AgentState state, KillingBlow blow) { } public virtual void OnAgentFleeing(Agent a) { } public virtual void OnMissionTick(float dt) { } }
class MissionLogic : MissionBehavior { }
class DeploymentMissionController { public bool TeamSetupOver = true; }
static class MBRandom { public static float RandomFloat => 0.5f; }
class DetachedDuelDispatchContext { public string Expected, Rejected; public bool Accepted, Ready = true; }
class DuelOutcomeStartIdentity { }
class TextValue { readonly string value; public TextValue(string v) { value = v; } public TextValue SetTextVariable(string name, string v) => this; public override string ToString() => value; }
static class AnimusForgeQuickInfo { public static void Show(string message, object target = null) { } }
static class Logger { public static void Log(string category, string message) { } }
static class DuelSettings { public static float GetHealthThreshold() => 0.3f; }
struct Color { public static Color FromUint(uint value) => new(); }
class InformationMessage { public InformationMessage(string message, Color color) { } }
static class InformationManager { public static void DisplayMessage(InformationMessage message) { } }

class DuelBehavior {
    static DuelBehavior Instance;
    bool _isDuelActive, _meetingPendingStart, _meetingPreFightActive, _currentDuelIsArena;
    Mission _activeInPlaceDuelMission, _meetingPendingMission;
    Hero _targetHero; CharacterObject _targetCharacter; int _targetAgentIndex = -1; string _targetDisplayName = "";
    Agent _inPlaceRestorePlayer, _inPlaceRestoreTarget;
    float _meetingPreFightEndTime;
    string _meetingPendingNonHeroMemoryId = "", _pendingNonHeroDuelMemoryId = "";
    DetachedDuelDispatchContext _meetingPendingDuelDispatchContext;
    DuelOutcomeStartIdentity _activeDuelOutcomeStart;
    static bool _duelResultRecorded, _arenaMissionActive, _arenaMissionLeaveRequested;
    static bool _leaveSourceMissionRequested, _openTownMenuRequested, _wildernessDuelEncounterMenuExitRequested;
    static long _wildernessDuelEncounterMenuGuardUntilUtcTicks;
    static Hero _queuedArenaDuelTarget; static CharacterObject _queuedDuelTargetCharacter;
    static Hero LastKiller; static int Deaths;
    int Starts, Ends, Unknowns, Restores, Locks; bool? Defeated, RestoredHealth; string StartedName, UnknownReason;
    Hero LockedHero;
    static void GlobalDuelStarterTick() { } static void GlobalSourceMissionLeaveTick() { }
    static void GlobalArenaLeaveTick() { } static void GlobalTownMenuTick() { }
    static void GlobalWildernessDuelEncounterMenuGuardTick() { }
    static string DuelString(string key, string fallback) => fallback;
    static TextValue DuelText(string key, string fallback) => new(fallback);
    static string LocalizeDuelEligibilityReason(string reason) => reason;
    static string ResolveDuelTargetDisplayName(Agent agent, Hero hero, CharacterObject character) => hero?.Name ?? "soldier";
    static void ShowDuelRiskWarning() { }
    static void RejectDetachedDuelDispatch(DetachedDuelDispatchContext context, string reason) { if (context != null) context.Rejected = reason; }
    static void AbortDetachedDuelDispatch(DetachedDuelDispatchContext context, string reason) => RejectDetachedDuelDispatch(context, reason);
    static void AcceptDetachedDuelDispatch(DetachedDuelDispatchContext context) { if (context != null) context.Accepted = true; }
    static void ReplaceDetachedDuelDispatch(ref DetachedDuelDispatchContext old, DetachedDuelDispatchContext value, string reason) { if (old != null && old != value) old.Rejected = reason; old = value; }
    static bool IsDetachedDuelDispatchReadyForDelayedHost(DetachedDuelDispatchContext context) => context == null || context.Ready;
    static bool CanTargetAgentStartDuel(Agent agent, out string reason) { reason = ""; return true; }
    void ApplyLegacyPendingMeetingDuelLocks(Agent agent) { LockedHero = agent?.Character.HeroObject; }
    void StartDuelInternal(Hero target, DetachedDuelDispatchContext context) { Starts++; StartedName = target?.Name; }
    void StartDuelInternal(Agent target, DetachedDuelDispatchContext context, string memory) { Starts++; StartedName = target?.Character.HeroObject?.Name ?? "soldier"; }
    void RefreshMeetingDuelParticipantLocks(Agent player, Agent target, bool preFight) { Locks++; }
    void EndDuel(bool playerDefeated) { Ends++; Defeated = playerDefeated; }
    static void ForceKillAgentVisual(Agent player, Agent target) { }
    static void ForceKillMainHero(Hero killer) { Deaths++; LastKiller = killer; }
    static void MarkDuelOutcomeUnknown(DuelOutcomeStartIdentity start, string reason, string source) { if (start != null) Instance.Unknowns++; Instance.UnknownReason = reason; }
    static void MarkDetachedDuelDispatchUnknownAfterStart(DetachedDuelDispatchContext context, string reason) { if (context != null) context.Rejected = reason; }
    static void LogWildernessDuelDiagnostic(string source, int id, Hero target) { }
    void RestoreState(bool restoreHealth) { Restores++; RestoredHealth = restoreHealth; }
    void ClearInPlaceDuelRestoreState() { _inPlaceRestorePlayer = null; _inPlaceRestoreTarget = null; _meetingPreFightActive = false; }
    // PRODUCTION_HOST_METHODS

    class WildernessDuelBattleRuntime {
        public Hero TargetHero; public CharacterObject TargetCharacter;
        public DuelOutcomeStartIdentity DuelOutcomeStart = new(); public bool AbortRequested, SettlementDone;
        public int Results, DiagnosticId; public bool? Defeated; public DetachedDuelDispatchContext DuelDispatchContext;
    }
    static void SettleWildernessDuelRuntime(WildernessDuelBattleRuntime runtime, bool playerDefeated, string source) {
        if (runtime.SettlementDone) return;
        runtime.SettlementDone = true; runtime.Results++; runtime.Defeated = playerDefeated;
    }
    class WildernessDuelBattleMissionLogic : MissionLogic {
        public WildernessDuelBattleRuntime _runtime;
        public bool _deploymentCompleted = true, _abortRequested; float _leaveTime;
        bool _startedLogged; float _participantDeadline = -1, _nextDeploymentCheckTime, _nextPruneTime;
        DeploymentMissionController _deploymentController;
        bool TryCompleteNativeDeployment() => true;
        void PruneNonDuelAgents() { } void TryDisableBattleEndLogic(string source) { }
        internal Hero CombatTargetHero => _runtime?.TargetHero;
        // PRODUCTION_NATIVE_METHODS
        public void Resolve() => TryResolveResultFromAgents("replay");
        public void Remove(Agent agent, AgentState state) => OnAgentRemoved(agent, null, state, new());
        public void Flee(Agent agent) => OnAgentFleeing(agent);
        public static bool Defeat(Agent agent) => IsAgentDefeatedOrFleeing(agent);
        public void SetExit(float time) { _leaveTime = time; }
        public void SetDeadline(float time) { _participantDeadline = time; }
    }
    class ArenaDuelMissionBehavior : MissionBehavior {
        public Agent _playerAgent, _opponentAgent; public Hero _targetHero;
        public bool _setupDone = true, _abortRequested, _localPreFightActive, _localDuelResultRecorded;
        public bool IsWildernessDuel;
        public DuelOutcomeStartIdentity _duelOutcomeStart = new();
        internal Hero CombatTargetHero => _targetHero;
        // PRODUCTION_ARENA_GATE
    }

    static int checks;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    static Hero MakeHero(string name) { var h = new Hero { Name = name }; h.CharacterObject = new CharacterObject { HeroObject = h }; return h; }
    static Agent Add(Hero hero, int index) { var a = new Agent { Character = hero.CharacterObject, Index = index }; Mission.Current.Agents.Add(a); return a; }
    static DuelBehavior Fresh(out Agent player, out Agent target) {
        Instance = new(); _duelResultRecorded = false; _arenaMissionActive = false; _arenaMissionLeaveRequested = false;
        Deaths = 0; LastKiller = null; Mission.Current = new(); Campaign.Current = new();
        player = Add(MakeHero("player"), 0); player.IsMainAgent = true; Agent.Main = player; Mission.Current.MainAgent = player;
        target = Add(MakeHero("B"), 20);
        return Instance;
    }
    void Bind(Agent player, Agent target) {
        TrySetCurrentDuelTargetFromAgent(target); _isDuelActive = true;
        _activeDuelOutcomeStart = new(); _activeInPlaceDuelMission = Mission.Current;
        _inPlaceRestorePlayer = player; _inPlaceRestoreTarget = target;
    }
    static WildernessDuelBattleMissionLogic Native(Agent target) {
        var logic = new WildernessDuelBattleMissionLogic { _runtime = new() { TargetHero = target.Character.HeroObject, TargetCharacter = target.Character } };
        Mission.Current.Add(logic); return logic;
    }
    static ArenaDuelMissionBehavior Arena(Agent player, Agent target, bool wilderness = false) {
        var logic = new ArenaDuelMissionBehavior { _playerAgent = player, _opponentAgent = target, _targetHero = target.Character.HeroObject, IsWildernessDuel = wilderness };
        Mission.Current.Add(logic); return logic;
    }
    static AgentState Decide(Mission mission, Agent agent, float probability = 1f) => new DuelPlayerDeathAgentStateDeciderLogic { Mission = mission }.GetAgentState(agent, probability, out _);
    static void Removed(Mission mission, Agent agent, AgentState state) => new DuelMainHeroDeathMissionBehavior { Mission = mission }.OnAgentRemoved(agent, null, state, new());

    static void QueueTests() {
        foreach (bool exact in new[] { false, true }) {
            var h = Fresh(out var p, out var b); var a = Add(MakeHero("A"), 10); h.TrySetCurrentDuelTargetFromAgent(a);
            Campaign.Current.ConversationManager.IsConversationInProgress = true;
            var context = exact ? new DetachedDuelDispatchContext { Expected = "B" } : null;
            h.StartDuelViaAI(b.Character.HeroObject, context);
            Check(h.GetTargetAgent() == b && h._targetCharacter == b.Character && h._targetAgentIndex == b.Index && h._targetDisplayName == "B", "Hero B replaces full stale A tuple " + exact);
            Campaign.Current.ConversationManager.IsConversationInProgress = false; h.OnEngineTick();
            Check(h.Starts == 1 && h.StartedName == "B" && !h._meetingPendingStart && h._meetingPendingMission == null, "actual pending Tick dispatches B once " + exact);
            h.OnEngineTick(); Check(h.Starts == 1, "pending consumed once " + exact);
        }
        var host = Fresh(out var player, out var target); var old = Add(MakeHero("A"), 10); host.TrySetCurrentDuelTargetFromAgent(old);
        Mission.Current.Agents.Remove(target); Campaign.Current.ConversationManager.IsConversationInProgress = true;
        host.StartDuelViaAI(target.Character.HeroObject, null);
        Check(host._targetAgentIndex == -1 && host.GetTargetAgent() == null, "missing B cannot reuse cached A index");
        Mission.Current.Agents.Add(target); Campaign.Current.ConversationManager.IsConversationInProgress = false; host.OnEngineTick();
        Check(host.StartedName == "B", "B resolving after conversation still starts B");
        foreach (bool exact in new[] { false, true }) {
            host = Fresh(out player, out target); Campaign.Current.ConversationManager.IsConversationInProgress = true;
            var context = exact ? new DetachedDuelDispatchContext() : null; host.StartDuelViaAI(target.Character.HeroObject, context);
            Mission.Current = new(); Mission.Current.Agents.Add(target); Campaign.Current.ConversationManager.IsConversationInProgress = false; host.OnEngineTick();
            Check(host.Starts == 0 && !host._meetingPendingStart && (context == null || context.Rejected == "meeting_mission_changed"), "pending cannot cross Mission " + exact);
        }
        host = Fresh(out player, out target); Campaign.Current.ConversationManager.IsConversationInProgress = true;
        target.Character.HeroObject = null; host.StartDuelViaAI(target, null);
        Check(host._meetingPendingMission == Mission.Current && host.GetTargetAgent() == target, "non-Hero pending captures origin mission");
        Mission.Current = null; host.OnEngineTick(); Check(!host._meetingPendingStart && host.Starts == 0, "legacy non-Hero pending cleared on mission loss");
        host = Fresh(out player, out target); host.StartDuelViaAI(target.Character.HeroObject, null);
        Check(host.Starts == 1 && host.StartedName == "B" && !host._meetingPendingStart, "direct Hero path preserved");
    }

    static void TickTests() {
        foreach (string owner in new[] { "arena", "native" }) {
            var h = Fresh(out var p, out var t); h._isDuelActive = true; h.TrySetCurrentDuelTargetFromAgent(t);
            if (owner == "arena") Arena(p, t); else Native(t); p.State = AgentState.Unconscious;
            h.OnEngineTick(); Check(h.Ends == 0 && h.Unknowns == 0 && Deaths == 0, "global Tick cannot resolve independent " + owner);
        }
        foreach (bool main in new[] { false, true }) {
            var h = Fresh(out var p, out var t); h.Bind(p, t); (main ? p : t).State = AgentState.Deleted; (main ? p : t).Health = 0;
            h.OnEngineTick(); Check(h.Ends == 0 && h.Unknowns == 1 && h.RestoredHealth == false && !h._isDuelActive && Deaths == 0, "Deleted is unknown without award/heal " + main);
        }
        var host = Fresh(out var player, out var target); host.Bind(player, target); player.Health = player.HealthLimit = 122; target.Health = target.HealthLimit = 122;
        host.OnEngineTick(); Check(host.Ends == 0 && host.Unknowns == 0, "healthy 122/122 cannot settle");
        target.Health = 30; target.HealthLimit = 100; host.OnEngineTick(); Check(host.Defeated == false && Deaths == 0, "target threshold wins without fatal player handling");
        host = Fresh(out player, out target); host.Bind(player, target); player.Health = 30; host.OnEngineTick();
        Check(host.Defeated == true && Deaths == 0, "player threshold loss is not death");
        host = Fresh(out player, out target); host.Bind(player, target); player.State = AgentState.Unconscious; host.OnEngineTick();
        Check(host.Defeated == true && Deaths == 1 && LastKiller == target.Character.HeroObject, "actual unconscious player retains fatal loss");
        host = Fresh(out player, out target); host.Bind(player, target); host._meetingPreFightActive = true; host._meetingPreFightEndTime = 10; player.Health = 1;
        host.OnEngineTick(); Check(host.Ends == 0 && host.Locks == 1 && Deaths == 0, "preparation does not settle low health");
        Mission.Current.IsMissionEnding = true; host.OnEngineTick(); Check(host.Ends == 0 && host.Unknowns == 1 && Deaths == 0, "ending mission aborts without retroactive result");
        host = Fresh(out player, out target); host.Bind(player, target); Mission.Current = new(); host.OnEngineTick();
        Check(host.Ends == 0 && host.Unknowns == 1 && host._activeInPlaceDuelMission == null, "in-place owner cannot cross Mission");
        host = Fresh(out player, out target); host.Bind(player, target); Mission.Current = null; host.OnEngineTick();
        Check(host.Ends == 0 && host.Unknowns == 1 && !host._isDuelActive, "missing mission closes actual owner unknown");
    }

    static void DeathGateTests() {
        var host = Fresh(out var player, out var target); host.Bind(player, target); var outsider = Add(MakeHero("outsider"), 99);
        Check(Decide(Mission.Current, player) == AgentState.Unconscious && Decide(Mission.Current, target) == AgentState.Unconscious && Decide(Mission.Current, outsider) == AgentState.Killed, "in-place decider only exact combat participants");
        host._targetHero = outsider.Character.HeroObject; Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 1 && LastKiller == target.Character.HeroObject, "death attribution uses bound session target not stale global Hero");
        Deaths = 0; host._meetingPreFightActive = true; Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 0 && Decide(Mission.Current, player) == AgentState.Killed, "preparation has no duel mortality override");
        host._meetingPreFightActive = false; _duelResultRecorded = true; Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 0 && Decide(Mission.Current, player) == AgentState.Killed, "completed in-place session has no death override");
        _duelResultRecorded = false; var origin = Mission.Current; Mission.Current = new(); Removed(origin, player, AgentState.Unconscious);
        Check(Deaths == 0 && Decide(origin, player) == AgentState.Killed, "old Mission callback cannot kill current main Hero");
        host = Fresh(out player, out target); host._isDuelActive = true; host.TrySetCurrentDuelTargetFromAgent(target); Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 0 && Decide(Mission.Current, target) == AgentState.Killed, "active flag alone is not actual session");
        host = Fresh(out player, out target); var arena = Arena(player, target);
        Check(Decide(Mission.Current, player, 0f) == AgentState.Unconscious && Decide(Mission.Current, player, 1f) == AgentState.Killed
            && Decide(Mission.Current, target, 0f) == AgentState.Unconscious && Decide(Mission.Current, target, 1f) == AgentState.Killed,
            "ordinary Arena preserves probability 0/1 for player and opponent");
        Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 0, "ordinary Arena generic death helper does not add campaign kill");
        arena.IsWildernessDuel = true;
        Check(Decide(Mission.Current, player, 0f) == AgentState.Unconscious && Decide(Mission.Current, player, 1f) == AgentState.Unconscious
            && Decide(Mission.Current, target, 0f) == AgentState.Unconscious && Decide(Mission.Current, target, 1f) == AgentState.Unconscious,
            "standalone wilderness preserves forced unconscious for both actors at probability 0/1");
        arena._localPreFightActive = true; Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 0 && Decide(Mission.Current, player) == AgentState.Killed, "Arena preparation gate preserved");
        arena._localPreFightActive = false; arena._localDuelResultRecorded = true;
        Check(Decide(Mission.Current, target) == AgentState.Killed, "finished Arena does not intercept death");
        host = Fresh(out player, out target); var native = Native(target);
        Check(Decide(Mission.Current, target) == AgentState.Unconscious, "actual native owner works without global active flag");
        native._deploymentCompleted = false; Removed(Mission.Current, player, AgentState.Unconscious);
        Check(Deaths == 0 && Decide(Mission.Current, target) == AgentState.Killed, "native deployment is not active combat mortality");
    }

    static void NativeTests() {
        foreach (AgentState state in new[] { AgentState.None, AgentState.Deleted }) {
            Fresh(out var player, out var target); var native = Native(target); target.State = state; target.Health = 0; native.Resolve();
            Check(!WildernessDuelBattleMissionLogic.Defeat(target) && native._runtime.Results == 0 && Deaths == 0, "native nonactive zeroHP is not defeat " + state);
        }
        foreach (AgentState state in new[] { AgentState.Killed, AgentState.Unconscious, AgentState.Routed }) {
            Fresh(out var player, out var target); var native = Native(target); native.Remove(player, state);
            Check(native._runtime.Results == 1 && native._runtime.Defeated == true && Deaths == (state == AgentState.Routed ? 0 : 1), "native explicit removal preserves fatal-only policy " + state);
            native.Remove(player, state); Removed(Mission.Current, player, state);
            Check(native._runtime.Results == 1 && Deaths == (state == AgentState.Routed ? 0 : 1), "native repeated callback and post-result death helper consumed once " + state);
        }
        Fresh(out var p, out var t); var logic = Native(t); p.Health = 20; logic.Resolve();
        Check(logic._runtime.Defeated == true && Deaths == 0, "native threshold loss not fatal");
        Fresh(out p, out t); logic = Native(t); p.State = AgentState.Unconscious; logic.Resolve();
        Check(logic._runtime.Defeated == true && Deaths == 1, "native Tick unconscious remains fatal");
        Fresh(out p, out t); logic = Native(t); logic.Flee(t);
        Check(logic._runtime.Defeated == false && Deaths == 0, "native explicit opponent fleeing remains victory");
        Fresh(out p, out t); logic = Native(t); logic._deploymentCompleted = false; t.Health = 0; logic.Resolve(); logic.Remove(t, AgentState.Killed);
        Check(logic._runtime.Results == 0, "native preparation cannot settle via Tick or removal");
        logic._deploymentCompleted = true; Mission.Current.IsMissionEnding = true; logic.Resolve(); logic.Remove(t, AgentState.Killed);
        Check(logic._runtime.Results == 0, "native ending does not harvest teardown result");
        foreach (bool removePlayer in new[] { false, true }) {
            Fresh(out p, out t); logic = Native(t); logic.Remove(removePlayer ? p : t, AgentState.Routed);
            Mission.Current.Agents.Remove(removePlayer ? p : t); if (removePlayer) { Mission.Current.MainAgent = null; Agent.Main = null; }
            Mission.Current.CurrentTime = 3; logic.OnMissionTick(0.1f);
            Check(Mission.Current.Ends == 1 && logic._runtime.Results == 1 && !logic._runtime.AbortRequested && Instance.Unknowns == 0, "settled native missing participant still exits after two seconds " + removePlayer);
        }
        Fresh(out p, out t); logic = Native(t); Mission.Current.Agents.Remove(t); logic.SetDeadline(30); Mission.Current.CurrentTime = 31; logic.OnMissionTick(0.1f);
        Check(Mission.Current.Ends == 1 && logic._runtime.Results == 0 && logic._runtime.AbortRequested && Instance.Unknowns == 1, "unsettled missing participant keeps startup-timeout unknown policy");
    }

    public static void Main() { QueueTests(); TickTests(); DeathGateTests(); NativeTests(); Console.WriteLine($"{checks} production-body behavior assertions passed; synthetic engine, no live-game claim."); }
}
