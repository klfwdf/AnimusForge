using System;
using System.Collections.Generic;
using System.Linq;

enum AgentState { Active, Unconscious, Killed, Routed, Deleted, None }
class KillingBlow { }
class Hero { }
class CharacterObject { public Hero HeroObject = new(); }
class Agent
{
    public CharacterObject Character;
    public AgentState State = AgentState.Active;
    public Mission Mission;
    public bool IsMainAgent => ReferenceEquals(Mission.MainAgent, this);
}
class Mission
{
    public static Mission Current;
    public Agent MainAgent;
    public bool IsMissionEnding;
}
class MissionBehavior
{
    public Mission Mission;
    public virtual void OnAgentRemoved(Agent agent, Agent affector, AgentState state, KillingBlow blow) { }
}
static class Logger { public static void Log(string category, string message) { } }

partial class DuelBehavior
{
    static DuelBehavior Instance;
    bool _isDuelActive;
    Hero _targetHero;
    static bool _arenaMissionLeaveRequested;
    static CurrentArena _currentArena;
    static readonly List<string> Trace = new();
    static void ForceKillMainHero(Hero hero) => Trace.Add("campaign_death_requested");
    static void ForceKillAgentVisual(Agent agent, Agent affector) => Trace.Add("visual_death_forced");
    static bool TryGetCombatDuelTargetHero(Mission mission, Agent agent, out Hero hero)
    {
        hero = null;
        if (_currentArena == null || !_currentArena.IsWildernessDuel || !_currentArena.IsCombatParticipant(agent)) return false;
        hero = _currentArena.TargetHero; return true;
    }
    partial class LegacyArena : MissionBehavior
    {
        CharacterObject _targetCharacter;
        bool _localDuelResultRecorded;
        public LegacyArena(CharacterObject character) { _targetCharacter = character; }
        void EndDuelLocal(bool playerDefeated)
        {
            if (_localDuelResultRecorded) throw new Exception("duplicate legacy result");
            _localDuelResultRecorded = true;
            Trace.Add(playerDefeated ? "result_loss" : "result_win");
        }
    }
    partial class CurrentArena : MissionBehavior
    {
        readonly Agent _playerAgent, _opponentAgent;
        readonly Hero _targetHero;
        readonly bool _isWildernessDuel;
        bool _abortRequested, _localDuelResultRecorded, _localPreFightActive;
        bool _setupDone = true;
        object _duelOutcomeStart = new();
        public bool IsWildernessDuel => _isWildernessDuel;
        public Hero TargetHero => _targetHero;
        public CurrentArena(Agent player, Agent opponent, bool wilderness)
        { _playerAgent = player; _opponentAgent = opponent; _targetHero = opponent.Character.HeroObject; _isWildernessDuel = wilderness; }
        void EndDuelLocal(bool playerDefeated)
        {
            if (_localDuelResultRecorded || !IsCombatParticipant(_playerAgent)) throw new Exception("invalid current result");
            _localDuelResultRecorded = true;
            Trace.Add(playerDefeated ? "result_loss" : "result_win");
        }
        void AbortArenaDuel(string reason, string source) { _abortRequested = true; Trace.Add("abort:" + reason); }
    }
    partial class LegacyMainDeath : MissionBehavior { }
    partial class CurrentMainDeath : MissionBehavior { }

    static string Run(bool current, bool wilderness, bool playerAffected, AgentState state)
    {
        Trace.Clear(); _currentArena = null; _arenaMissionLeaveRequested = false;
        var mission = Mission.Current = new Mission();
        var targetCharacter = new CharacterObject();
        var player = new Agent { Mission = mission, Character = new() };
        var target = new Agent { Mission = mission, Character = targetCharacter };
        mission.MainAgent = player;
        // Actual ordinary Arena opener leaves this default false; standalone opener sets true.
        Instance = new DuelBehavior { _isDuelActive = wilderness, _targetHero = targetCharacter.HeroObject };
        MissionBehavior local = current ? _currentArena = new CurrentArena(player, target, wilderness) : new LegacyArena(targetCharacter);
        MissionBehavior generic = current ? new CurrentMainDeath() : new LegacyMainDeath();
        local.Mission = generic.Mission = mission;
        var affected = playerAffected ? player : target;
        var affector = playerAffected ? target : player;
        // Native Mission.OnAgentRemoved sets state before callbacks and clears MainAgent after them.
        affected.State = state;
        local.OnAgentRemoved(affected, affector, state, new());
        generic.OnAgentRemoved(affected, affector, state, new());
        if (playerAffected) mission.MainAgent = null;
        return string.Join(",", Trace);
    }
    public static int Main()
    {
        int passed = 0, failed = 0;
        foreach (bool wilderness in new[] { false, true })
        foreach (bool playerAffected in new[] { false, true })
        foreach (var state in new[] { AgentState.Unconscious, AgentState.Killed, AgentState.Routed })
        {
            string name = (wilderness ? "standalone_wilderness" : "ordinary_arena") + "/" + (playerAffected ? "player" : "target") + "/" + state;
            string oldTrace = Run(false, wilderness, playerAffected, state);
            string currentTrace = Run(true, wilderness, playerAffected, state);
            bool valid = oldTrace == currentTrace
                && oldTrace.StartsWith(playerAffected ? "result_loss" : "result_win", StringComparison.Ordinal)
                && !currentTrace.Contains("visual_death_forced", StringComparison.Ordinal);
            if (valid) passed++; else failed++;
            Console.WriteLine((valid ? "PASS " : "FAIL ") + name + " old=[" + oldTrace + "] current=[" + currentTrace + "]");
        }
        Console.WriteLine($"RESULT passed={passed} failed={failed}");
        return failed == 0 ? 0 : 1;
    }
}
