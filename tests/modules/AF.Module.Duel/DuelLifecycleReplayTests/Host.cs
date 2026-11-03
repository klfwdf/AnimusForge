using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

enum AgentState { None, Active, Routed, Unconscious, Killed, Deleted }
enum BattleSideEnum { Attacker, Defender }
enum EquipmentIndex { ArmorItemEndSlot, HorseHarness }
enum AgentController { None, Player, AI }
enum MissionMode { Battle }
enum MissionBehaviorType { Other }
enum InputKey { Tab }
enum DuelSessionKind { Arena, Wilderness }
enum DuelOutcomeEffectState { NotApplicable, Applied }
class KillingBlow { }
class DetachedDuelDispatchContext { }
class DuelOutcomeStartIdentity { public string DuelId = "duel"; }
class DuelOutcomeResultIdentity { }
class DuelOutcomeEffects { }
class DuelOutcomeReceipt { public bool IsTerminal; }
class Banner { }
class Faction { public uint Color, Color2; }
class Clan { public Banner Banner = new(); }
class EquipmentElement { public static EquipmentElement Invalid = new(); }
class Equipment
{
    public EquipmentElement this[EquipmentIndex index] { get => new(); set { } }
    public Equipment Clone() => new();
}
class CharacterObject { public Hero HeroObject; public Equipment FirstBattleEquipment = new(); }
class Hero
{
    public static Hero MainHero;
    public CharacterObject CharacterObject = new();
    public Faction MapFaction = new(); public Clan Clan = new(); public Equipment BattleEquipment = new();
    public string Name = "hero", StringId = "target";
    public float InitialHealth = 100;
    public Hero() { CharacterObject.HeroObject = this; }
}
struct Vec3
{
    public float x, y, z;
    public Vec3(float x, float y = 0, float z = 0) { this.x = x; this.y = y; this.z = z; }
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
}
struct Vec2 { public static readonly Vec2 Zero = new(); public Vec2(float x, float y) { } }
struct Rotation { public Vec3 f; }
struct MatrixFrame { public Vec3 origin; public Rotation rotation; }
struct Color { public static Color FromUint(uint value) => new(); }
class Team { public int EnemyLinks; public void SetIsEnemyOf(Team team, bool isEnemyOf) { if (isEnemyOf) EnemyLinks++; } }
class Teams : List<Team>
{
    public Team Add(BattleSideEnum side, uint color, uint color2, Banner banner, bool isPlayerGeneral = false, bool isPlayerSergeant = false)
    { var team = new Team(); base.Add(team); return team; }
}
class AgentBuildData
{
    public CharacterObject Character; public Team TargetTeam;
    public AgentBuildData(CharacterObject character) { Character = character; }
    public AgentBuildData Team(Team team) { TargetTeam = team; return this; }
    public AgentBuildData Equipment(Equipment equipment) => this;
    public AgentBuildData InitialPosition(in Vec3 position) => this;
    public AgentBuildData InitialDirection(in Vec2 direction) => this;
}
class Agent
{
    public enum MortalityState { Mortal, Invulnerable }
    public enum WatchState { Alarmed }
    public static Agent Main;
    public AgentState State = AgentState.Active;
    public CharacterObject Character;
    public Team Team;
    public Mission Mission;
    public int Index;
    public float Health = 100, HealthLimit = 100;
    public MortalityState Mortality;
    public bool ThrowSetTeam, ThrowMortal;
    public string FailController;
    AgentController _controller = AgentController.AI;
    public AgentController Controller
    {
        get => _controller;
        set { if (value.ToString() == FailController) throw new Exception("injected controller failure"); _controller = value; }
    }
    public bool IsMainAgent => ReferenceEquals(Mission.MainAgent, this);
    public bool IsActive() => State == AgentState.Active;
    public void SetTeam(Team team, bool sync) { if (ThrowSetTeam) throw new Exception("injected SetTeam failure"); Team = team; }
    public void SetMortalityState(MortalityState state)
    { if (ThrowMortal && state == MortalityState.Mortal) throw new Exception("injected mortality failure"); Mortality = state; }
    public void SetWatchState(WatchState state) { }
    public void SetMovementDirection(in Vec2 direction) { }
    public void ClearTargetFrame() { }
}
class Mission
{
    public static Mission Current;
    public readonly List<Agent> Agents = new();
    public readonly Teams Teams = new();
    readonly Dictionary<int, Agent> _indexed = new();
    public Team PlayerTeam;
    public object Scene;
    public float CurrentTime = 1;
    public bool IsMissionEnding;
    public int EndCalls, PlayerSpawnCalls, TargetSpawnCalls, FailPlayerSpawns, FailTargetSpawns;
    public string ThrowBeforeSpawn, ThrowAfterSpawn, PlayerControllerFailure, TargetControllerFailure;
    public bool TargetSetTeamFailure;
    public CharacterObject Target;
    public MissionBehavior Behavior;
    Agent _main;
    public Agent MainAgent { get => _main; set { _main = value; Agent.Main = value; } }
    public Agent FindAgentWithIndex(int index) => _indexed.TryGetValue(index, out var agent) ? agent : null;
    public void RemoveLookup(Agent agent) { _indexed.Remove(agent.Index); Agents.Remove(agent); }
    public void ReplaceLookup(Agent agent) { _indexed[agent.Index] = agent; }
    public Agent Add(CharacterObject character)
    {
        var agent = new Agent { Mission = this, Index = Agents.Count + 1, Character = character, Health = character.HeroObject?.InitialHealth ?? 100 };
        Agents.Add(agent); _indexed[agent.Index] = agent; return agent;
    }
    public Agent SpawnAgent(AgentBuildData data)
    {
        bool target = ReferenceEquals(data.Character, Target);
        string role = target ? "target" : "player";
        if (target) TargetSpawnCalls++; else PlayerSpawnCalls++;
        if (ThrowBeforeSpawn == role) throw new Exception("injected spawn before native add");
        if (target && FailTargetSpawns-- > 0 || !target && FailPlayerSpawns-- > 0) return null;
        var agent = Add(data.Character); agent.Team = data.TargetTeam;
        agent.ThrowSetTeam = target && TargetSetTeamFailure;
        agent.FailController = target ? TargetControllerFailure : PlayerControllerFailure;
        if (ThrowAfterSpawn == role) throw new Exception("injected native OnAgentBuild after add");
        return agent;
    }
    public void SetMissionMode(MissionMode mode, bool atStart) { }
    public void EndMission()
    {
        EndCalls++; IsMissionEnding = true; Behavior.EndForTest();
        foreach (var agent in Agents.ToArray())
        { Behavior.OnAgentRemoved(agent, null, AgentState.Deleted, new()); Behavior.OnAgentDeleted(agent); }
    }
}
class MissionBehavior
{
    public Mission Mission;
    public virtual MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    public virtual void OnAfterMissionCreated() { }
    public virtual void OnBehaviorInitialize() { }
    public virtual void OnCreated() { }
    public virtual void EarlyStart() { }
    public virtual void AfterStart() { }
    public virtual void OnMissionTick(float dt) { }
    public virtual void OnAgentRemoved(Agent a, Agent b, AgentState state, KillingBlow blow) { }
    public virtual void OnAgentDeleted(Agent agent) { }
    protected virtual void OnEndMission() { }
    public void EndForTest() => OnEndMission();
}
static class Input { public static bool Tab; public static bool IsKeyPressed(InputKey key) { bool value = Tab; Tab = false; return value; } }
static class Logger { public static void Log(string category, string message) { } }
static class AnimusForgeQuickInfo { public static void Show(string text, CharacterObject character = null) { } }
static class DuelSettings { public static float GetHealthThreshold() => .35f; }
static class MyBehavior { public static int MemoryWrites; public static void RecordDuelResultForExternal(Hero hero, bool won, string source) { MemoryWrites++; } }

partial class Replay
{
    static Replay Instance;
    bool _isDuelActive;
    readonly Dictionary<string, int> _lastDuelResults = new();
    static bool _arenaMissionLeaveRequested, _arenaMissionStartedOnce, _arenaMissionActive;
    static float _arenaMissionLeaveReadyTime;
    static long _arenaMissionOpeningGraceUntilUtcTicks;
    static DetachedDuelDispatchContext _openingDuelDispatchContext;
    static bool IsArenaMissionActive => _arenaMissionActive;
    static int _unknown, _records, _renown, _stakes, _finalized, _heroDeaths, _visualDeaths, _frames;
    static bool _recordFails;
    static string _outcome;
    static DuelOutcomeReceipt _receipt;
    static DuelOutcomeStartIdentity _started;
    void FinishDuel() { _isDuelActive = false; }
    static string ResolveDuelTargetDisplayName(object agent, Hero hero, CharacterObject character) => "target";
    static void LogWildernessDuelDiagnostic(string text, int diagnosticId, Hero target) { }
    static void LogDuelLoadingCheckpoint(string text, int diagnosticId, Hero target, object unused, bool immediate) { }
    static void ResolveIndependentDuelSpawnFrames(object scene, out MatrixFrame player, out MatrixFrame enemy) { _frames++; player = new(); enemy = new(); }
    static string DuelString(string key, string fallback) => fallback;
    static string ResolveDuelOutcomeSubjectId(Hero hero, CharacterObject character, string memoryId) => "target";
    static bool TryBeginDuelOutcome(string subject, DuelSessionKind kind, string source, out DuelOutcomeStartIdentity start, DetachedDuelDispatchContext context)
    { start = _started ??= new(); return true; }
    static void MarkDetachedDuelDispatchUnknownAfterStart(DetachedDuelDispatchContext context, string reason)
    { if (context != null && !_receipt.IsTerminal) { _unknown++; _receipt.IsTerminal = true; } }
    static void MarkDuelOutcomeUnknown(DuelOutcomeStartIdentity start, string reason, string source)
    { if (start != null && !_receipt.IsTerminal) { _unknown++; _receipt.IsTerminal = true; } }
    static bool TryReadDuelOutcome(string id, out DuelOutcomeReceipt receipt) { receipt = _receipt; return true; }
    static bool TryRecordDuelOutcome(DuelOutcomeStartIdentity start, bool win, string source, out DuelOutcomeResultIdentity result)
    {
        result = null;
        if (_recordFails || _receipt.IsTerminal || !ReferenceEquals(start, _started) || _records > 0) return false;
        _records++; _outcome = win ? "win" : "loss"; result = new(); return true;
    }
    static void SetDuelDebtTagGateState(Hero hero, int result) { }
    static string ApplyDuelRenownPenaltyAndBuildResultText(Hero hero, bool won, out DuelOutcomeEffectState state)
    { _renown++; state = DuelOutcomeEffectState.Applied; return ""; }
    static string ApplyDuelStakeSettlementAndBuildResultText(Hero hero, bool won, string id, out DuelOutcomeEffectState state)
    { _stakes++; state = DuelOutcomeEffectState.Applied; return ""; }
    static void TryPostDuelAiShout(Hero hero, Agent agent, bool won, string id) { }
    static bool TryCreateDuelOutcomeEffects(DuelSessionKind kind, bool hero, bool restored, bool lost, DuelOutcomeEffectState renown, DuelOutcomeEffectState stake, out DuelOutcomeEffects effects)
    { effects = new(); return true; }
    static bool TryFinalizeDuelOutcome(DuelOutcomeResultIdentity result, string source, DuelOutcomeEffects effects, out object ignored)
    { ignored = null; _finalized++; _receipt.IsTerminal = true; return true; }
    static void ForceKillMainHero(Hero hero) { _heroDeaths++; }
    static void ForceKillAgentVisual(Agent agent, Agent killer)
    { _visualDeaths++; agent.State = AgentState.Killed; agent.Mission.Behavior.OnAgentRemoved(agent, killer, AgentState.Killed, new()); }

    static ArenaDuelMissionBehavior New(bool wilderness = false, bool detached = false, bool nonHero = false)
    {
        Instance = new() { _isDuelActive = true };
        _arenaMissionLeaveRequested = _arenaMissionStartedOnce = _arenaMissionActive = false;
        _arenaMissionLeaveReadyTime = 0; _openingDuelDispatchContext = null;
        _unknown = _records = _renown = _stakes = _finalized = _heroDeaths = _visualDeaths = _frames = 0;
        _recordFails = false; _outcome = "none"; _receipt = new(); _started = null;
        MyBehavior.MemoryWrites = 0; Input.Tab = false; Hero.MainHero = new();
        var target = new Hero();
        var character = nonHero ? new CharacterObject() : target.CharacterObject;
        var behavior = nonHero
            ? new ArenaDuelMissionBehavior(character, wilderness, duelDispatchContext: detached ? new() : null, nonHeroMemoryId: "npc-1")
            : new ArenaDuelMissionBehavior(target, wilderness, duelDispatchContext: detached ? new() : null);
        behavior.Mission = Mission.Current = new() { Target = character, Behavior = behavior };
        return behavior;
    }
    static T Field<T>(object item, string name) => (T)item.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item);
    static void Set(object item, string name, object value) => item.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, value);
    static void Check(ArenaDuelMissionBehavior behavior) => typeof(ArenaDuelMissionBehavior).GetMethod("CheckDuelResult", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(behavior, null);
    static Agent Player(ArenaDuelMissionBehavior b) => Field<Agent>(b, "_playerAgent");
    static Agent Target(ArenaDuelMissionBehavior b) => Field<Agent>(b, "_opponentAgent");
    static void Tick(ArenaDuelMissionBehavior b, float time) { b.Mission.CurrentTime = time; b.OnMissionTick(.016f); }
    static void Start(ArenaDuelMissionBehavior b) { b.AfterStart(); Tick(b, 6); }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void NoAward() { Require(_records == 0 && _renown == 0 && _stakes == 0 && _finalized == 0 && MyBehavior.MemoryWrites == 0, "unexpected result or business effect"); }
    static void Unknown(ArenaDuelMissionBehavior b) { NoAward(); Require(_unknown == 1 && Field<bool>(b, "_abortRequested"), "abort must terminalize Unknown exactly once"); }
    static void Outcome(string expected)
    { Require(_outcome == expected && _records == 1 && _renown == 1 && _stakes == 1 && _finalized == 1 && MyBehavior.MemoryWrites == 1, "typed result/effects not exactly once: " + _outcome); }

    public static int Main()
    {
        int passed = 0, failed = 0;
        void Case(string name, Action run)
        { try { run(); passed++; Console.WriteLine("PASS " + name); } catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
        Case("normal_arena_countdown_exact_participants", () => {
            var b = New(); b.AfterStart(); Require(b.Mission.Agents.Count == 2 && b.Mission.Teams.Count == 2, "spawn count");
            Require(!b.IsCombatParticipant(Player(b)) && Player(b).Mortality == Agent.MortalityState.Invulnerable, "prep protection");
            Tick(b, 5.99f); NoAward(); Require(!b.IsCombatParticipant(Target(b)), "countdown shortened");
            Tick(b, 6); Require(b.IsCombatParticipant(Target(b)) && Target(b).Controller == AgentController.AI, "combat not ready"); NoAward();
        });
        Case("independent_wilderness_uses_same_lifecycle", () => { var b = New(true); Start(b); Require(_frames == 1 && b.IsCombatParticipant(Player(b)), "wilderness setup"); NoAward(); });
        Case("nonhero_identity_starts_without_hero_lookup", () => { var b = New(nonHero: true); Start(b); Require(b.CombatTargetHero == null && b.IsCombatParticipant(Target(b)) && Field<string>(b, "_nonHeroMemoryId") == "npc-1", "nonhero identity changed"); NoAward(); });
        Case("repeated_after_start_never_duplicates_participants", () => { var b = New(); b.AfterStart(); b.AfterStart(); Require(b.Mission.Agents.Count == 2 && b.Mission.Teams.Count == 2 && b.Mission.PlayerSpawnCalls == 1 && b.Mission.TargetSpawnCalls == 1, "AfterStart not idempotent"); });
        Case("initial_hp_is_not_healed", () => { var b = New(); Hero.MainHero.InitialHealth = 62; b.Mission.Target.HeroObject.InitialHealth = 73; Start(b); Require(Player(b).Health == 62 && Target(b).Health == 73, "initial HP changed"); NoAward(); });
        Case("transient_target_failure_reuses_player_and_teams", () => {
            var b = New(); b.Mission.FailTargetSpawns = 1; b.AfterStart(); Require(Player(b).Mortality == Agent.MortalityState.Invulnerable, "partial player exposed");
            Tick(b, 1.016f); Require(b.Mission.TargetSpawnCalls == 1, "unthrottled retry"); Tick(b, 1.25f);
            Require(b.Mission.PlayerSpawnCalls == 1 && b.Mission.TargetSpawnCalls == 2 && b.Mission.Agents.Count == 2 && b.Mission.Teams.Count == 2 && Field<bool>(b, "_setupDone"), "partial spawn duplicated or never ready");
        });
        Case("transient_player_failure_reuses_teams", () => { var b = New(); b.Mission.FailPlayerSpawns = 1; b.AfterStart(); Tick(b, 1.25f); Require(b.Mission.Agents.Count == 2 && b.Mission.Teams.Count == 2 && Field<bool>(b, "_setupDone"), "player retry failed"); });
        Case("lost_partial_player_aborts_instead_of_respawn", () => { var b = New(); b.Mission.FailTargetSpawns = 1; b.AfterStart(); b.Mission.RemoveLookup(Player(b)); Tick(b, 1.25f); Unknown(b); Require(b.Mission.PlayerSpawnCalls == 1 && b.Mission.TargetSpawnCalls == 1, "lost partial player respawned"); });
        Case("three_attempts_do_not_abort_before_deadline", () => {
            var b = New(); b.Mission.FailTargetSpawns = 999; b.AfterStart(); Tick(b, 1.016f); Tick(b, 1.032f);
            Require(Field<int>(b, "_setupAttempts") == 1, "early frame retries"); Tick(b, 1.25f); Tick(b, 1.5f);
            Require(Field<int>(b, "_setupAttempts") == 3 && !Field<bool>(b, "_abortRequested") && b.Mission.EndCalls == 0, "three attempts still instant-abort"); NoAward();
        });
        Case("deadline_unknown_then_teardown_without_award", () => {
            var b = New(detached: true); b.Mission.FailTargetSpawns = 999; b.AfterStart(); Tick(b, 30.99f); Require(!Field<bool>(b, "_abortRequested"), "deadline early");
            Tick(b, 31); Unknown(b); int calls = b.Mission.TargetSpawnCalls; Tick(b, 31.016f); Require(b.Mission.EndCalls == 1 && b.Mission.TargetSpawnCalls == calls, "abort spawned again"); Unknown(b);
        });
        foreach (string role in new[] { "player", "target" }) foreach (bool after in new[] { false, true })
        {
            string selectedRole = role; bool selectedAfter = after;
            Case("spawn_exception_" + role + (after ? "_after_native_add" : "_before_native_add"), () => {
                var b = New(); if (selectedAfter) b.Mission.ThrowAfterSpawn = selectedRole; else b.Mission.ThrowBeforeSpawn = selectedRole;
                b.AfterStart(); Unknown(b); int count = b.Mission.Agents.Count; Tick(b, 2); Require(b.Mission.Agents.Count == count && !Field<bool>(b, "_setupDone"), "exception retried/adopted partial agent"); Unknown(b);
            });
        }
        Case("set_team_exception_never_sets_ready", () => { var b = New(); b.Mission.TargetSetTeamFailure = true; b.AfterStart(); Unknown(b); Require(!Field<bool>(b, "_setupDone"), "early ready flag"); });
        Case("player_controller_failure_aborts", () => { var b = New(); b.Mission.PlayerControllerFailure = "Player"; b.AfterStart(); Unknown(b); });
        Case("target_controller_failure_aborts", () => { var b = New(); b.Mission.TargetControllerFailure = "None"; b.AfterStart(); Unknown(b); });
        Case("same_character_spectator_not_adopted_or_awarded", () => {
            var b = New(); var spectator = b.Mission.Add(b.Mission.Target); Start(b);
            Require(!ReferenceEquals(Target(b), spectator) && !b.IsCombatParticipant(spectator), "character-only participant binding");
            b.OnAgentRemoved(spectator, Player(b), AgentState.Killed, new()); b.OnAgentDeleted(spectator); Check(b); NoAward();
        });
        Case("preparation_target_removal_is_unknown", () => { var b = New(); b.AfterStart(); b.OnAgentRemoved(Target(b), Player(b), AgentState.Killed, new()); Unknown(b); });
        Case("preparation_player_removal_does_not_kill_campaign_hero", () => { var b = New(); b.AfterStart(); b.OnAgentRemoved(Player(b), Target(b), AgentState.Unconscious, new()); Unknown(b); Require(_heroDeaths == 0, "prep killed campaign hero"); });
        Case("not_ready_callback_cannot_award", () => { var b = New(); b.AfterStart(); Set(b, "_setupDone", false); b.OnAgentRemoved(Target(b), Player(b), AgentState.Killed, new()); Unknown(b); });
        Case("preparation_missing_target_aborts_before_countdown", () => { var b = New(); b.AfterStart(); b.Mission.RemoveLookup(Target(b)); Tick(b, 2); Unknown(b); });
        Case("combat_controller_failure_is_unknown", () => { var b = New(); b.AfterStart(); Target(b).FailController = "AI"; Tick(b, 6); Unknown(b); });
        Case("combat_mortality_failure_is_unknown", () => { var b = New(); b.AfterStart(); Target(b).ThrowMortal = true; Tick(b, 6); Unknown(b); });
        Case("valid_threshold_win_and_ten_second_exit", () => {
            var b = New(); Start(b); Target(b).Health = 35; Check(b); Check(b); Outcome("win");
            Tick(b, 15.99f); Require(b.Mission.EndCalls == 0, "exit timer shortened"); Tick(b, 16); Require(b.Mission.EndCalls == 1, "exit timer not honored"); Outcome("win");
        });
        Case("both_low_health_player_loses_first", () => { var b = New(); Start(b); Player(b).Health = Target(b).Health = 20; Check(b); Outcome("loss"); });
        foreach (var state in new[] { AgentState.Killed, AgentState.Unconscious, AgentState.Routed })
        {
            var selected = state;
            Case("target_valid_state_" + state, () => { var b = New(); Start(b); Target(b).State = selected; Check(b); Outcome("win"); });
            Case("target_valid_callback_" + state, () => { var b = New(); Start(b); b.OnAgentRemoved(Target(b), Player(b), selected, new()); Outcome("win"); });
        }
        foreach (var state in new[] { AgentState.Deleted, AgentState.None })
        {
            var selected = state;
            Case("target_invalid_state_" + state, () => { var b = New(); Start(b); Target(b).State = selected; Check(b); Unknown(b); });
            Case("target_invalid_callback_" + state, () => { var b = New(); Start(b); b.OnAgentRemoved(Target(b), Player(b), selected, new()); Unknown(b); });
        }
        Case("target_deleted_callback_is_unknown", () => { var b = New(); Start(b); b.OnAgentDeleted(Target(b)); Unknown(b); });
        Case("target_missing_without_removal_proof_is_unknown", () => { var b = New(); Start(b); b.Mission.RemoveLookup(Target(b)); Check(b); Unknown(b); });
        Case("target_index_reused_does_not_adopt_replacement", () => { var b = New(); Start(b); var replacement = new Agent { Index = Target(b).Index, Mission = b.Mission, Character = b.Mission.Target }; b.Mission.ReplaceLookup(replacement); Check(b); Unknown(b); });
        Case("main_agent_replaced_is_unknown", () => { var b = New(); Start(b); b.Mission.MainAgent = b.Mission.Add(Hero.MainHero.CharacterObject); Check(b); Unknown(b); });
        Case("player_missing_is_unknown", () => { var b = New(); Start(b); b.Mission.RemoveLookup(Player(b)); Check(b); Unknown(b); });
        Case("mission_ending_removals_are_not_victory", () => { var b = New(); Start(b); b.Mission.IsMissionEnding = true; b.OnAgentRemoved(Target(b), Player(b), AgentState.Killed, new()); Unknown(b); });
        Case("tab_exit_blocks_later_result", () => { var b = New(); Start(b); Input.Tab = true; Tick(b, 7); b.OnAgentRemoved(Target(b), Player(b), AgentState.Killed, new()); Unknown(b); });
        Case("mission_end_during_preparation_is_unknown", () => { var b = New(); b.AfterStart(); b.Mission.EndMission(); Unknown(b); });
        foreach (var state in new[] { AgentState.Unconscious, AgentState.Killed })
        {
            var selected = state;
            Case("arena_player_callback_does_not_add_campaign_death_" + state, () => {
                var b = New(); Start(b); b.OnAgentRemoved(Player(b), Target(b), selected, new()); Outcome("loss");
                Require(_heroDeaths == 0 && _visualDeaths == 0, "ordinary Arena callback gained forced death");
            });
            Case("wilderness_player_callback_preserves_campaign_death_only_" + state, () => {
                var b = New(wilderness: true); Start(b); b.OnAgentRemoved(Player(b), Target(b), selected, new()); Outcome("loss");
                Require(_heroDeaths == 1 && _visualDeaths == 0, "wilderness legacy generic death behavior changed");
            });
        }
        Case("player_unconscious_poll_preserves_legacy_exception_and_reentry_guard", () => { var b = New(); Start(b); Player(b).State = AgentState.Unconscious; Check(b); Outcome("loss"); Require(_heroDeaths == 1 && _visualDeaths == 1, "legacy Unconscious poll exception changed"); });
        Case("player_killed_poll_does_not_add_campaign_death", () => { var b = New(); Start(b); Player(b).State = AgentState.Killed; Check(b); Outcome("loss"); Require(_heroDeaths == 0 && _visualDeaths == 0, "Killed poll gained forced death"); });
        Case("record_failure_never_runs_business_effects", () => { var b = New(); Start(b); _recordFails = true; Target(b).Health = 20; Check(b); NoAward(); Require(Field<bool>(b, "_abortRequested") && _arenaMissionLeaveRequested, "failed typed result stayed active"); });
        Case("post_result_delete_and_end_do_not_rewrite_receipt", () => { var b = New(); Start(b); Target(b).Health = 20; Check(b); b.OnAgentDeleted(Target(b)); b.Mission.EndMission(); Outcome("win"); Require(_unknown == 0, "terminal result rewritten"); });
        Console.WriteLine($"RESULT passed={passed} failed={failed}");
        return failed == 0 ? 0 : 1;
    }
}
