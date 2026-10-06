"""Source wiring audit, complementary to the compiled production-projection tests.

This does not claim to run Bannerlord callbacks or native damage settlement.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[4]
taunt = (root / "src/modules/AF.Module.Taunt/Host/SceneTauntBehavior.cs").read_text(encoding="utf-8-sig")
sets = (root / "src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs").read_text(encoding="utf-8-sig")
host = (root / "src/bridges/Vengeance/Host/VengeanceRuntimeBridge.cs").read_text(encoding="utf-8-sig")
checks = 0


def body(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if depth == 0:
            return source[start + 1:end]
    raise AssertionError("unterminated method " + signature)


def check(condition, label):
    global checks
    assert condition, label
    checks += 1
    print("PASS wiring: " + label)


for source, signatures in [
    (taunt, ["private static bool CanInitializePeaceSceneConflict(",
             "public override void OnAgentHit(",
             "internal bool ShouldUseFullCombatDamage(",
             "private static bool IsPlayerProtectedSceneAttackAgent(",
             "internal static void ApplyArmedConflictStartCrimeForExternal(",
             "private void TryApplyArmedNpcKnockdownConsequencesCore(",
             "private static void TryForceAgentMortal(",
             "private void AddAgentToFightSide(",
             "private static void AddUniqueAgent(",
             "private static bool ShouldJoinArmedBystanderToConflict(",
             "private static void TryForceUnarmedBystanderToFlee(",
             "private void TryForceArmedBystanderToWatchPlayer(",
             "private static void TryAlarmAgent("]),
    (sets, ["internal bool ShouldHandlePhysicalAttack(Agent target)",
            "private void StartConflict(",
            "internal bool ShouldAllowDefenderConflictDamage(",
            "internal bool TryStartOwnedSettlementMassacre(",
            "private void StartOwnedSettlementIncident(",
            "private bool IsOwnedSettlementIncidentTarget(",
            "private bool IsSceneConflictTriggerAgent(",
            "private static bool IsVictoryObjectiveSceneAgent(",
            "private void MarkEnemyAgent(Agent agent, bool victoryObjective)",
            "private void ForceOwnedSettlementCivilianFlee("]),
]:
    for signature in signatures:
        check(body(source, signature).strip().startswith("if (ExecutionSceneConflictBridge."), signature)

tick = body(taunt, "public override void OnMissionTick(float dt)")
check(tick.index("ExecutionSceneConflictBridge.IsSceneControlled") < tick.index("TryActivateSettlementArmedCarryover"),
      "Taunt tick gates before carryover and attack scanning")
check(tick.index("UpdateMainAgentAttackReleaseTracking") < tick.index("TryActivateSettlementArmedCarryover")
      and "executionSceneControlled || _executionSceneWasControlled" in tick,
      "isolation and release frame consume stale attack stage")
for source in [taunt, sets]:
    score = body(source, "public override void OnScoreHit(")
    first = score.index("ExecutionSceneConflictBridge.BlocksConflict")
    check("return;" in score[first:score.index("\n", first)], "score hit exits before local side effects")
    removed = body(source, "public override void OnAgentRemoved(")
    check(removed.index("ExecutionSceneConflictBridge.BlocksConflict") < removed.index(
        "TryHandleOwnedSettlementPassiveAttackKnockdown" if source is taunt else "_townRiotKilledNotable = true"),
        "agent removal gates penalties after necessary local cleanup")
tick = body(sets, "public override void OnMissionTick(float dt)")
check(tick.index("if (ExecutionSceneConflictBridge.IsSceneControlled(base.Mission)) return;")
      < tick.index('StartConflict("armed_coup_start"'), "SETS autonomous conflict gated")
check("GetMissionBehavior<RichExecutions.Scene.TownExecutionMissionBehavior>() != null" not in tick,
      "SETS escort hold releases when ceremony owner returns control")
admission = body(taunt, "internal bool CanStartConflict(")
check("CanInitializePeaceSceneConflict" in admission and "BlocksConflict(base.Mission, agent)" in admission,
      "verbal and physical admission both exclude execution and victim")
inject = body(host, "internal static void TryInjectMission(")
check(inject.index("finally") < inject.index("ExecutionSceneConflictBridge.Register"),
      "registration also covers partial injection failures")
print(f"{checks}/{checks} source wiring checks passed; native callback execution NOT_RUN")
