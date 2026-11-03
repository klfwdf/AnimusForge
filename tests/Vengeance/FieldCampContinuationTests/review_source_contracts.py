"""Offline source guards for the three camp review regressions.

These checks inspect production control flow and reject reverted fix hunks.
They do not compile/run C# or validate native team, navigation or UI behavior.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
CAMP = ROOT / "src/modules/AF.Module.FieldCamp/Host"


def read(path):
    return path.read_text(encoding="utf-8-sig")


def block(source, marker):
    start = source.index("{", source.index(marker))
    depth = 1
    end = start + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start + 1:end - 1]


def require(value, description):
    if not value:
        raise AssertionError(description)


def duel_handoff(grounds, duel, people):
    place = block(grounds, "internal bool PlaceDuelists(")
    scoped = "if (player == Mission.MainAgent && _prisonerPlayerTeam != null && player.Team == _prisonerPlayerTeam)"
    require(scoped in place, "only the real captive-isolated player rejoins the camp team")
    handoff = place.index("player.SetTeam(_team, true);")
    require(handoff < place.index("HoldDuelist(opponent);"), "normalize before handing off the duel participants")
    call = duel.index("!fieldCamp.PlaceDuelists(Agent.Main, agent)")
    require(call < duel.index("_preDuelPlayerTeam = Agent.Main?.Team ?? current.PlayerTeam;", call),
            "existing duel snapshot follows the camp handoff")
    require("camp.GetDuelTeams(out _duelPlayerTeam, out _duelEnemyTeam)" in duel and
            "TryInitializeFormalDuelTeams(current, agent)" in duel,
            "camp isolates both participants from the resident team")
    isolation = block(people, "private void MaintainCampPrisonerCombatIsolation(")
    require(isolation.index("_duelists.Count != 0") < isolation.index("player.SetTeam(_prisonerPlayerTeam, true);"),
            "camp upkeep cannot re-isolate the player during a duel")


def blocked_retry(life):
    upkeep = block(life, "private void UpkeepBatchOfAgents(")
    require("if (entry.MoveBlocked) continue;" not in upkeep, "blocked state cannot skip upkeep forever")
    retry = block(upkeep, "if (entry.MoveBlocked)")
    require("if (_elapsed < entry.NextWanderTime) continue;" in retry,
            "failed walkers wait for the existing cooldown")
    require(retry.index("entry.NextWanderTime") < retry.index("MoveTo(entry, entry.Target, entry.TargetFacing, run: entry.Run);"),
            "queued/assembled walkers retry the actual destination and speed only after cooldown")
    require("entry.RouteQueued || _assembled" in retry and "entry.CanLeavePost" in retry and
            "MoveTo(entry, entry.Home, entry.HomeFacing, run: false);" in retry,
            "ordinary companions retry home rather than remaining blocked at a stroll")
    require("continue;" in retry and upkeep.index("if (entry.MoveBlocked)") < upkeep.index("if (!entry.Seated && _elapsed >= entry.MoveDeadline)"),
            "blocked recovery precedes stale movement deadlines")
    require("RecoverBlockedCampWalk(entry);" in upkeep,
            "expired/stuck walkers enter the bounded recovery owner")
    recovery = block(life, "private void RecoverBlockedCampWalk(")
    require("TeleportToPosition" not in recovery and "SettleCampAgent(entry, blocked: true)" in recovery,
            "collision recovery replans without teleporting")
    move = block(life, "private void MoveTo(")
    require("entry.MoveBlocked = entry.RouteQueued = false;" in move and "entry.MoveDeadline = _elapsed + Math.Max(30f," in move,
            "actual command owner resets blockage and retains a finite deadline")
    queued = block(move, "if (!prepared && _campRouteBudget <= 0)")
    require("entry.Returning = returnAfterVisit;" in queued and "entry.RouteQueued = true;" in queued and
            "entry.NextWanderTime = _elapsed + 0.5f;" in queued,
            "budget deferral preserves return intent and retries on next upkeep")


def formation_owner(life, execution):
    require("_ceremonyActive = false;" in block(execution, "private void OnCeremonyReleased("),
            "exercise the post-release interval, not just the active ceremony")
    for name in ("Assemble", "Dismiss"):
        method = block(life, "internal bool " + name + "(")
        guard = method[:method.index("return false;")]
        require("_ceremonyWalkers.Count > 0" in guard,
                name + " must wait for returning ceremony walkers")
    returning = block(execution, "private void TickCeremonyReturn(")
    require("if (remove) RemoveCeremonyMove(move);" in returning and
            "_ceremonyWalkers.Remove(move);" in block(execution, "private void RemoveCeremonyMove("),
            "the return owner retires walkers through the shared cleanup owner")
    require("if (!FinishCeremonyReturn(move))" in returning,
            "failed safe returns retain their owner instead of reporting completion")


class CampReviewSourceContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.grounds = read(CAMP / "FieldCampMissionLogic.Grounds.cs")
        cls.life = read(CAMP / "FieldCampMissionLogic.Life.cs")
        cls.execution = read(CAMP / "FieldCampMissionLogic.Execution.cs")
        cls.people = read(CAMP / "FieldCampMissionLogic.People.cs")
        cls.duel = read(ROOT / "src/modules/AF.Module.Duel/Host/DuelBehavior.cs")

    def test_duel_captive_team_handoff(self):
        duel_handoff(self.grounds, self.duel, self.people)

    def test_blocked_walk_cooldown_recovers(self):
        blocked_retry(self.life)

    def test_formation_waits_for_ceremony_return(self):
        formation_owner(self.life, self.execution)

    def test_duel_guard_rejects_reverted_handoff(self):
        mutated = self.grounds.replace("player.SetTeam(_team, true);", "// missing handoff")
        with self.assertRaises((AssertionError, ValueError)):
            duel_handoff(mutated, self.duel, self.people)

    def test_retry_guard_rejects_permanent_skip(self):
        mutated = self.life.replace("if (entry.MoveBlocked)", "if (entry.MoveBlocked) continue; if (false)")
        with self.assertRaises((AssertionError, ValueError)):
            blocked_retry(mutated)

    def test_formation_guard_rejects_either_unprotected_command(self):
        for name in ("Assemble", "Dismiss"):
            method = block(self.life, "internal bool " + name + "(")
            mutated = self.life.replace(method, method.replace(" || _ceremonyWalkers.Count > 0", ""), 1)
            with self.subTest(command=name), self.assertRaises((AssertionError, ValueError)):
                formation_owner(mutated, self.execution)


if __name__ == "__main__":
    unittest.main(verbosity=2)
