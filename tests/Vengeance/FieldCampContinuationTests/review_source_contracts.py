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
        cls.poses = read(CAMP / "FieldCampMissionLogic.Poses.cs")
        cls.execution = read(CAMP / "FieldCampMissionLogic.Execution.cs")
        cls.people = read(CAMP / "FieldCampMissionLogic.People.cs")
        cls.duel = read(ROOT / "src/modules/AF.Module.Duel/Host/DuelBehavior.cs")

    def test_camp_follower_exclusively_owns_movement(self):
        follow = read(CAMP / "FieldCampMissionLogic.Follow.cs")
        self.assertIn("entry.FollowingPlayer = true;", block(follow, "internal bool BeginFollowPlayer("))
        upkeep = block(self.life, "private void UpkeepBatchOfAgents(")
        self.assertIn("if (entry.FollowingPlayer || entry.PoseTransition != CampPoseTransition.None) continue;", upkeep)
        walks = block(self.life, "private void AdvanceCampWalks(")
        self.assertIn("if (entry.FollowingPlayer ||", walks)
        self.assertLess(walks.index("if (entry.FollowingPlayer ||"), walks.index("AdvanceCampPoseTransition(entry)"))
        self.assertIn("AdvanceCampPoseTransition(entry) || entry.Seated || entry.MoveBlocked", walks)
        release = block(self.life, "internal void ReleaseSceneConversation(")
        self.assertLess(release.index("if (IsFollowingPlayer(agent))"), release.index("StopCampNavigation(agent);"))
        self.assertIn("SceneMovementController.TryEnableVanillaSceneFollowBehavior", release)
        self.assertIn("entry.FollowingPlayer || IsCampAgentBusyForConversation", block(self.life, "internal bool RestoreCampAttentionOwner("))

    def test_camp_only_admits_follow_rules_not_all_scene_movement(self):
        config = read(ROOT / "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs")
        self.assertIn("mission.GetMissionBehavior<FieldCampMissionLogic>() != null", block(config, "private static bool ShouldExcludeSceneMoveRuleForMission("))
        capture = read(ROOT / "src/AF.GameAdapter.Bannerlord/Prompt/SceneMechanismPromptCaptureAdapter.cs")
        camp_branch = block(capture, "if (camp != null && includeGenericRules)")
        self.assertIn("SCENE_FOLLOW_PLAYER", camp_branch)
        self.assertIn("SCENE_STOP_FOLLOW", camp_branch)
        self.assertNotIn("SCENE_GUIDE", camp_branch)
        self.assertNotIn("SCENE_SUMMON", camp_branch)
        self.assertIn("return list;", camp_branch)
        post = read(ROOT / "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs")
        self.assertIn("!campFollowRuleAvailable", post)
        self.assertIn("campFollowRuleAvailable &&", post)
        manual = read(ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.PlayerDirectActions.cs")
        self.assertIn("if (FieldCampMissionLogic.Current != null) return PlayerDirectActionContext.Scene;", manual)
        self.assertIn("FieldCampMissionLogic.Current.CanFollowPlayer(agent)", manual)

    def test_walking_clears_both_pose_channels_and_combat_owner(self):
        release = block(self.life, "private static void ReleasePose(")
        self.assertIn("DisableScriptedCombatMovement", release)
        self.assertIn("SetMaximumSpeedLimit(-1f", release)
        self.assertIn("SetActionChannel(0, in ActionIndexCache.act_none", release)
        self.assertIn("SetActionChannel(1, in ActionIndexCache.act_none", release)
        self.assertIn("blendOutPeriodToNoAnim: 0f", release)
        self.assertIn("ReleasePose(entry);", block(self.life, "private void MoveTo("))
        self.assertNotIn('"act_sit_beggar_idle"', self.life + self.poses)
        self.assertIn("TryPlayCampPose(entry, pool)", block(self.life, "private void ApplyPose("))
        self.assertIn("private void PlayLoop(CampAgent entry, string[] pool) => TryPlayCampPose(entry, pool);", self.life)
        hold = block(self.poses, "private static bool StartCampPoseHold(")
        self.assertIn("AnimFlags.anf_lock_movement", hold)
        self.assertIn("startProgress:", hold)
        self.assertIn("if (entry.RestPose != CampRestPose.None) flags |= AnimFlags.anf_cyclic", hold)

    def test_short_routes_fail_closed_and_keep_budget(self):
        route = block(self.life, "private bool TryCampRoute(")
        self.assertIn("stayInsideCamp: true", route)
        self.assertNotIn("HasCeremonyWalkPath", route)
        stroll = block(self.life, "private bool TryNearbyStroll(")
        self.assertLess(stroll.index("for (int i = 0; i < 6; i++)"), stroll.index("int count = _layout.WanderSlots.Count;"))
        self.assertIn("_campRouteBudget--", stroll)

    def test_training_uses_placed_entity_not_idle_archer_clip(self):
        training = read(CAMP / "FieldCampMissionLogic.Training.cs")
        props = read(CAMP / "FieldCampProps.cs")
        self.assertIn("ArcheryTargets.Add(aim)", props)
        self.assertIn("ArcheryTargets.Clear();", props)
        self.assertIn("SetScriptedTargetEntity(target.WeakEntity", training)
        self.assertIn("UpkeepCampArchery(entry)", block(self.life, "private void UpkeepBatchOfAgents("))
        main = read(CAMP / "FieldCampMissionLogic.cs")
        self.assertIn("walkToAssignedSlot: s.Work == FieldCampLayout.WorkKind.Archer", main)
        zones = read(ROOT / "src/modules/AF.Module.FieldCamp/FieldCampLayout.Zones.cs")
        self.assertIn("WorkSlots.Insert(i,", block(zones, "private void PlaceTraining("))
        self.assertNotIn("private static readonly string[] ArcherActions", self.life)

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
