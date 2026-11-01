// Template: inserted opener/decider bodies come from Git legacy source or current production.
using System;
using System.Linq;
using System.Collections.Generic;
enum AgentState { Active, Unconscious, Killed }
class Hero { public static Hero MainHero; public string StringId; public CharacterObject CharacterObject; public Settlement CurrentSettlement; }
class Settlement { public string StringId; }
class CharacterObject { public Hero HeroObject; public string StringId; }
class Agent { public bool IsMainAgent; public CharacterObject Character; public int Index; }
class Mission {
    public static Mission Current; public string SceneName; public bool IsMissionEnding;
    public List<MissionBehavior> Behaviors = new(); public Agent MainAgent;
    public T GetMissionBehavior<T>() where T : MissionBehavior => Behaviors.OfType<T>().FirstOrDefault();
}
class MissionInitializerRecord { public string SceneName; public MissionInitializerRecord(string name) { SceneName = name; } }
static class MissionState {
    public static Mission OpenNew(string name, MissionInitializerRecord rec, Func<Mission, MissionBehavior[]> factory) {
        var mission = new Mission { SceneName = rec.SceneName }; Mission.Current = mission;
        foreach (var behavior in factory(mission)) { behavior.Mission = mission; mission.Behaviors.Add(behavior); }
        return mission;
    }
}
class MissionBehavior { public Mission Mission; }
class MissionLogic : MissionBehavior { }
interface IAgentStateDecider { }
interface IMissionBehavior { }
class MissionOptionsComponent : MissionBehavior { }
class AgentHumanAILogic : MissionBehavior { }
class MissionHardBorderPlacer : MissionBehavior { }
class MissionBoundaryPlacer : MissionBehavior { }
class DetachedDuelDispatchContext { }
class DuelOutcomeStartIdentity { }
static class Logger { public static void Log(string category, string message) { } }
static class MBRandom { public static float RandomFloat => 0.5f; }
class Observation { public bool Active, Opened; public AgentState Player0, Player1, Target0, Target1; }
// SPLIT
class DuelBehavior {
    static DuelBehavior Instance;
    bool _isDuelActive, _currentDuelIsArena, _meetingPreFightActive;
    Hero _targetHero; CharacterObject _targetCharacter; int _targetAgentIndex = -1; string _targetDisplayName;
    Mission _activeInPlaceDuelMission; Agent _inPlaceRestorePlayer, _inPlaceRestoreTarget;
    DuelOutcomeStartIdentity _activeDuelOutcomeStart;
    static int _wildernessDuelActiveDiagnosticId, _wildernessDuelDiagnosticSerial;
    static long _wildernessDuelOpenStartedUtcTicks, _arenaMissionOpeningGraceUntilUtcTicks;
    static string _wildernessDuelLastOpenScene;
    static bool _arenaMissionActive, _arenaMissionLeaveRequested, _arenaMissionStartedOnce, _returnToMapAfterIndependentDuel;
    static bool _duelResultRecorded, _forcedMainHeroDeath, _pendingMainHeroDeath;
    static float _arenaMissionLeaveReadyTime;
    static Hero _pendingMainHeroDeathKiller; static object _wildernessDuelRuntime;
    static DetachedDuelDispatchContext _openingDuelDispatchContext;
    static string ResolveDuelTargetDisplayName(Agent a, Hero h, CharacterObject c) => h?.StringId ?? c?.StringId;
    static bool IsWildernessDuelContext(CharacterObject c, out string reason) { reason = ""; return true; }
    static bool TryBuildWildernessDuelMissionInitializerRecord(CharacterObject c, out MissionInitializerRecord rec, out string reason) { rec = new("field"); reason = ""; return true; }
    static bool TryFinishPlayerEncounterForWildernessDuelOpening(int id, Hero h, MissionInitializerRecord rec, string source) => true;
    static void EnsureMainHeroHealthForWildernessDuel(string source) { }
    static void CleanupWildernessDuelRuntime(object runtime, string source) { }
    static void ResetWildernessDuelOpeningState() { }
    static void LogWildernessDuelDiagnostic(string source, int id, Hero h, MissionInitializerRecord rec = null) { }
    static void LogDuelLoadingCheckpoint(string source, int id, Hero h, MissionInitializerRecord rec, bool immediate) { }
    static MissionBehavior CreateWildernessDuelBoundaryCrossingHandler() => new();
    static void SetDuelDebtTagGateState(Hero h, int state) { }
    static MissionInitializerRecord CreateArenaDuelInitializerRecord(string scene) => new(scene);
    static void ReplaceDetachedDuelDispatch(ref DetachedDuelDispatchContext current, DetachedDuelDispatchContext next, string reason) { current = next; }
    static void MarkDetachedDuelSideEffectBoundaryCrossed(DetachedDuelDispatchContext context) { }
    static void MarkDetachedDuelDispatchUnknownAfterStart(DetachedDuelDispatchContext context, string reason) { }
    class DuelMainHeroDeathMissionBehavior : MissionBehavior { }
    class WildernessDuelBattleMissionLogic : MissionLogic { internal Hero CombatTargetHero => null; internal bool IsCombatParticipant(Agent a) => false; }
    class ArenaDuelMissionBehavior : MissionBehavior {
        Agent _playerAgent, _opponentAgent; Hero _targetHero; bool _isWildernessDuel;
        bool _setupDone, _abortRequested, _localPreFightActive, _localDuelResultRecorded;
        DuelOutcomeStartIdentity _duelOutcomeStart;
        internal bool IsWildernessDuel => _isWildernessDuel;
        internal Hero CombatTargetHero => _targetHero;
        public ArenaDuelMissionBehavior(CharacterObject target, bool isWildernessDuel = false, int diagnosticId = 0, DetachedDuelDispatchContext duelDispatchContext = null, string nonHeroMemoryId = null) { _targetHero = target.HeroObject; _isWildernessDuel = isWildernessDuel; }
        public void InjectSuccessfulCombatSetup(Agent player, Agent target) { _playerAgent = player; _opponentAgent = target; _setupDone = true; _duelOutcomeStart = new(); }
        // ARENA_GATE
    }
    // PRODUCTION_METHODS
    static Hero NewHero(string name) { var hero = new Hero { StringId = name }; hero.CharacterObject = new() { StringId = name, HeroObject = hero }; return hero; }
    public static Observation Run(bool wilderness) {
        Instance = new(); Mission.Current = null; Hero.MainHero = NewHero("player"); var targetHero = NewHero("target");
        bool opened = wilderness ? TryOpenStandaloneWildernessDuelMission(targetHero.CharacterObject, "replay", "legacy_parity") : Instance.TryTeleportToArenaForDuel(targetHero.CharacterObject);
        if (!opened || Mission.Current == null) throw new Exception("opener failed");
        var player = new Agent { IsMainAgent = true, Character = Hero.MainHero.CharacterObject, Index = 0 };
        var target = new Agent { Character = targetHero.CharacterObject, Index = 1 };
        Mission.Current.MainAgent = player; Mission.Current.GetMissionBehavior<ArenaDuelMissionBehavior>().InjectSuccessfulCombatSetup(player, target);
        var decider = Mission.Current.GetMissionBehavior<DuelPlayerDeathAgentStateDeciderLogic>();
        return new Observation { Opened = opened, Active = Instance._isDuelActive,
            Player0 = decider.GetAgentState(player, 0f, out _), Player1 = decider.GetAgentState(player, 1f, out _),
            Target0 = decider.GetAgentState(target, 0f, out _), Target1 = decider.GetAgentState(target, 1f, out _) };
    }
}
// SPLIT
static class Program {
    static int assertions;
    static void Check(bool result, string name) { if (!result) throw new Exception(name); assertions++; Console.WriteLine("PASS " + name); }
    static void Main() {
        foreach (bool wilderness in new[] { false, true }) {
            var old = Legacy.DuelBehavior.Run(wilderness); var current = Current.DuelBehavior.Run(wilderness);
            string mode = wilderness ? "standalone-wilderness" : "ordinary-arena";
            Check(old.Opened && current.Opened, mode + " actual old/current openers execute");
            Check(old.Active == wilderness && current.Active == old.Active, mode + " actual opener global active parity = " + old.Active);
            Check(current.Player0 == old.Player0, mode + " player probability 0 legacy/current = " + old.Player0);
            Check(current.Player1 == old.Player1, mode + " player probability 1 legacy/current = " + old.Player1);
            Check(current.Target0 == old.Target0, mode + " opponent probability 0 legacy/current = " + old.Target0);
            Check(current.Target1 == old.Target1, mode + " opponent probability 1 legacy/current = " + old.Target1);
        }
        Console.WriteLine($"{assertions} actual legacy/current opener-decider parity assertions passed; native construction/setup are synthetic boundaries.");
    }
}
