using System;
using System.Collections.Generic;

// Only the generated methods are production code. These objects model the health
// surface needed by those methods; they do not load or simulate a native Mission.
class Agent
{
    public int Index;
    public float Health;
    public float HealthLimit = 100;
    public object Team = new object();
    public bool Active = true;
    public bool IsActive() => Active;
}
class FormalDuelSpectatorSnapshot
{
    public int MountAgentIndex = -1;
    public float LastSafeHealth;
    public float LastSafeMountHealth;
}
struct MissionWeapon { }
struct AttackCollisionData { }
struct Blow { public int InflictedDamage; }
class WeaponComponentData { }
class FakeMissionBehavior
{
    public virtual void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData) { }
    public virtual void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty) { }
}

partial class Replay : FakeMissionBehavior
{
    private bool _formalDuelIsolationStarted = true;
    private readonly Dictionary<int, FormalDuelSpectatorSnapshot> _formalDuelSpectatorSnapshots = new();
    private readonly Dictionary<int, int> _freezeCounts = new();
    private int HitContinuationCount;
    private int ScoreContinuationCount;
    private FormalDuelSpectatorSnapshot FindFormalDuelSpectatorSnapshotForAgent(Agent agent)
        => agent != null && _formalDuelSpectatorSnapshots.TryGetValue(agent.Index, out var snapshot) ? snapshot : null;
    private void FreezeFormalDuelSpectatorAgentOrMount(Agent agent, FormalDuelSpectatorSnapshot snapshot, string reason)
    {
        if (agent == null || snapshot == null) return;
        _freezeCounts.TryGetValue(agent.Index, out int count);
        _freezeCounts[agent.Index] = count + 1;
        if (agent.Index == snapshot.MountAgentIndex)
            snapshot.LastSafeMountHealth = Math.Max(snapshot.LastSafeMountHealth, agent.Health);
        else
            snapshot.LastSafeHealth = Math.Max(snapshot.LastSafeHealth, agent.Health);
    }
    private void LogDuelIsolation(string message) { }
    private string FormatAgent(Agent agent) => agent?.Index.ToString();
    private string FormatTeam(object team) => "team";
    private string SafeHealthText(Agent agent) => agent.Health.ToString();
    private string FormatMissionWeapon(in MissionWeapon weapon) => "weapon";
    private string FormatWeaponComponent(WeaponComponentData weapon) => "weapon";
    private void TryCapturePreEscalationFatalHitContext(Agent victim, Agent attacker, in MissionWeapon weapon, in Blow blow) => HitContinuationCount++;

    private static Agent NewAgent(int id, float health) => new Agent { Index = id, Health = health };
    private void Spectator(Agent agent, float safeHealth, bool mount = false)
        => _formalDuelSpectatorSnapshots[agent.Index] = new FormalDuelSpectatorSnapshot
        {
            LastSafeHealth = safeHealth, MountAgentIndex = mount ? agent.Index : -1,
            LastSafeMountHealth = mount ? safeHealth : 0,
        };
    private void Hit(Agent victim, Agent attacker, int inflictedDamage)
    {
        var weapon = new MissionWeapon();
        var blow = new Blow { InflictedDamage = inflictedDamage };
        var collision = new AttackCollisionData();
        OnAgentHit(victim, attacker, in weapon, in blow, in collision);
    }
    private void Score(Agent victim, Agent attacker, int inflictedDamage, float actualDamage)
    {
        var blow = new Blow { InflictedDamage = inflictedDamage };
        var collision = new AttackCollisionData();
        OnScoreHit(victim, attacker, new WeaponComponentData(), false, false, in blow, in collision, actualDamage, 0, 0);
    }
    private void Pair(Agent victim, Agent attacker, int rawDamage, float actualDamage)
    {
        // Native Agent deducts once, then Mission calls OnAgentHit and OnScoreHit.
        victim.Health -= actualDamage;
        Hit(victim, attacker, rawDamage);
        Score(victim, attacker, rawDamage, actualDamage);
    }
    private static void Equal(float expected, float actual, string label)
    {
        if (Math.Abs(expected - actual) > 0.001f) throw new Exception(label + " expected=" + expected + " actual=" + actual);
    }
    private static void Require(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
    private void Intercepted(Agent spectator)
    {
        Equal(0, HitContinuationCount, "OnAgentHit early return retained");
        Equal(0, ScoreContinuationCount, "OnScoreHit early return retained");
        Equal(2, _freezeCounts[spectator.Index], "both callbacks freeze spectator");
    }

    public static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("attacker_single_compensation", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 70); var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r.Pair(player, bystander, 10, 10);
                Equal(70, player.Health, "one native hit must not heal above prior health"); r.Intercepted(bystander);
            }),
            ("on_hit_freezes_without_additive_healing", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 60); var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r.Hit(player, bystander, 10);
                Equal(60, player.Health, "OnAgentHit is not damage compensation owner");
                Equal(0, r.HitContinuationCount, "fatal context is not captured for spectator");
                Equal(1, r._freezeCounts[2], "immediate freeze preserved");
                r.Score(player, bystander, 10, 10); Equal(70, player.Health, "OnScoreHit restores actual damage");
            }),
            ("near_limit_does_not_fill_player", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 95); var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r.Pair(player, bystander, 10, 10); Equal(95, player.Health, "prior HP retained");
            }),
            ("actual_damage_not_raw_blow", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 70); var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r.Pair(player, bystander, 25, 4); Equal(70, player.Health, "compensate actual damagedHp");
            }),
            ("harmless_hit_does_not_heal", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 70); var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r.Pair(player, bystander, 25, 0); Equal(70, player.Health, "zero actual damage"); r.Intercepted(bystander);
            }),
            ("real_opponent_damage_unchanged", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 70); var opponent = NewAgent(3, 90);
                r.Spectator(NewAgent(2, 80), 80); r.Pair(player, opponent, 10, 10);
                Equal(60, player.Health, "real duel damage retained"); Equal(1, r.HitContinuationCount, "normal hit continuation"); Equal(1, r.ScoreContinuationCount, "normal score continuation");
                Equal(0, r._freezeCounts.Count, "participants not frozen");
            }),
            ("spectator_victim_restored_to_snapshot", () =>
            {
                var r = new Replay(); var bystander = NewAgent(2, 70); var opponent = NewAgent(3, 90);
                r.Spectator(bystander, 70); r.Pair(bystander, opponent, 10, 10); Equal(70, bystander.Health, "safe snapshot preserved"); r.Intercepted(bystander);
            }),
            ("two_spectators_do_not_double_compensate", () =>
            {
                var r = new Replay(); var victim = NewAgent(2, 70); var attacker = NewAgent(4, 80);
                r.Spectator(victim, 70); r.Spectator(attacker, 80); r.Pair(victim, attacker, 10, 10);
                Equal(70, victim.Health, "snapshot recovery and additive recovery must not stack"); r.Intercepted(victim); r.Intercepted(attacker);
            }),
            ("spectator_mount_snapshot_restored", () =>
            {
                var r = new Replay(); var mount = NewAgent(4, 45); var opponent = NewAgent(3, 90);
                r.Spectator(mount, 45, mount: true); r.Pair(mount, opponent, 10, 10); Equal(45, mount.Health, "mount safe snapshot"); r.Intercepted(mount);
            }),
            ("disabled_isolation_is_native_passthrough", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 70); var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r._formalDuelIsolationStarted = false; r.Pair(player, bystander, 10, 10);
                Equal(60, player.Health, "outside isolation retains damage"); Equal(0, r._freezeCounts.Count, "no freeze outside isolation"); Equal(1, r.ScoreContinuationCount, "native continuation");
            }),
            ("empty_snapshots_is_native_passthrough", () =>
            {
                var r = new Replay(); var player = NewAgent(1, 70); r.Pair(player, NewAgent(2, 80), 10, 10);
                Equal(60, player.Health, "no owned spectator"); Equal(1, r.HitContinuationCount, "normal continuation");
            }),
            ("fatal_inactive_victim_not_resurrected", () =>
            {
                var r = new Replay(); var victim = NewAgent(1, 0); victim.Active = false; var bystander = NewAgent(2, 80);
                r.Spectator(bystander, 80); r.Hit(victim, bystander, 100); r.Score(victim, bystander, 100, 100);
                Equal(0, victim.Health, "existing inactive gate retained"); Require(!victim.Active, "not resurrected"); r.Intercepted(bystander);
            }),
            ("missing_victim_is_safe", () =>
            {
                var r = new Replay(); var bystander = NewAgent(2, 80); r.Spectator(bystander, 80);
                r.Hit(null, bystander, 10); r.Score(null, bystander, 10, 10); r.Intercepted(bystander);
            }),
        };
        int failures = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception error) { failures++; Console.WriteLine("FAIL " + test.Name + " :: " + error.Message); }
        }
        Console.WriteLine("RESULT cases=" + tests.Length + " passed=" + (tests.Length - failures) + " failed=" + failures);
        return failures == 0 ? 0 : 1;
    }
}
