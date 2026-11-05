using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class FieldCampMissionLogic
{
	// Every clip below is defined directly in as_human_hideout_bandit (Native action_sets.xml, checked 2026-11-04);
	// the female set inherits it through base_set.
	// Ground-compatible resting clip only; never use begging gestures for healthy soldiers.
    private static readonly string[] SitActions = { "act_dungeon_prisoner_sitting_ground_cycle" };
    // Tavern chair/eating clips require a seat-height StandingPoint and must not run on bare terrain.
	private static readonly string[] SentryActions = { "act_guard_cautious_look_around_1", "act_guard_normal_look_around_1", "act_stand_1" };
	// Props use non-usable benches; no seat-height binding exists for these agents.

	private static readonly string[] CookActions = { "act_bartender_meat", "act_bartender_bake" };
	private static readonly string[] SmithActions = { "act_repair_upside", "act_repair" };
	private static readonly string[] RepairActions = { "act_repair", "act_repair_upside" };

	private static readonly string[] LeanActions = { "act_lean_fence_loop", "act_lean_fence_loop2" };
	private static readonly string[] GossipActions = { "act_talk_to_1", "act_talk_to_2", "act_talk_to_3", "act_talk_to_4", "act_talk_to_5" };
	private static readonly string[] LieActions = { "act_dungeon_prisoner_sleep_cycle" };
	private static readonly string[] GroomActions = { "act_npc_horse_grooming" };
	private static readonly string[] ChopActions = { "act_npc_villager_lumberjack" };
	private static readonly string[] ShovelActions = { "act_npc_villager_shoveling" };
	private static readonly string[] WashActions = { "act_npc_villager_cloth_washing" };
	private static readonly string[] WriteActions = { "act_writer" };
	private static readonly string[] PickupActions = { "act_pick_up_from_ground" };
	private static readonly string[] TableActions = { "act_table_top_work", "act_npc_villager_working_while_stand" };
	private static readonly string[] StandActions = { "act_stand_1", "act_stand_2" };
	private static readonly string[] LookoutActions = { "act_guard_cautious_look_around_1", "act_guard_normal_look_around_1", "act_guard_idle_look_around" };
	// Native paired animation points: one side talks, the other listens (hideout set, checked 2026-11-04).
	private static readonly string[] TalkActions = { "act_talk_to_1", "act_talk_to_2", "act_talk_to_3", "act_talk_to_4", "act_talk_to_5" };
	private static readonly string[] ListenActions = { "act_listen_1", "act_listen_2", "act_listen_3", "act_listen_4" };
	// Idle wanderers and workers stopped away from their station loiter instead of standing in act_none.
	private static readonly string[] LoiterActions = { "act_stand_1", "act_stand_2", "act_guard_normal_look_around_1", "act_guard_normal_look_around_2", "act_guard_normal_look_around_3" };

	// Right/left hand prefabs that the native animation points attach for these clips.
	private static readonly Dictionary<string, string[]> HandItems = new Dictionary<string, string[]>(StringComparer.Ordinal)
	{
		["act_npc_horse_grooming"] = new[] { "horse_brush", "" },
		["act_npc_villager_lumberjack"] = new[] { "lumberjack_axe", "" },
		["act_npc_villager_shoveling"] = new[] { "pitchfork", "" },
		["act_repair"] = new[] { "hammer_wooden", "" },
		["act_repair_upside"] = new[] { "hammer_wooden", "" },
		["act_bartender_meat"] = new[] { "tavernkeeper_spatula", "" },
		["act_bartender_bake"] = new[] { "tavernkeeper_knife", "" },
		["act_writer"] = new[] { "pen", "notebook" }
	};


    private enum CampRestPose { None, Sit, Lie }
    private enum CampPoseTransition { None, Enter, Exit }
    private struct CampPoseClip
    {
        internal bool Available, Cyclic;
        internal float Duration;
    }

    // Native clip metadata is resolved once per action-set/action pair for this mission, never via
    // reflection, disk IO or a per-frame asset scan. Female sets can carry different metadata.
    private readonly Dictionary<long, CampPoseClip> _campPoseClips = new Dictionary<long, CampPoseClip>();

    private CampPoseClip GetCampPoseClip(Agent agent, ActionIndexCache action)
    {
        long key = ((long)agent.ActionSet.GetHashCode() << 32) | (uint)action.GetHashCode();
        if (_campPoseClips.TryGetValue(key, out CampPoseClip clip)) return clip;
        try
        {
            clip.Available = MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, in action);
            if (clip.Available)
            {
                clip.Cyclic = (MBActionSet.GetActionAnimationFlags(agent.ActionSet, in action) & AnimFlags.anf_cyclic) != 0;
                clip.Duration = MBActionSet.GetActionAnimationDuration(agent.ActionSet, in action);
            }
        }
        catch (Exception ex)
        {
            clip = default(CampPoseClip);
            Logger.Log(LogSource, "pose metadata unavailable action=" + action + " error=" + ex.Message);
        }
        _campPoseClips.Add(key, clip);
        return clip;
    }

    private static bool IsCampActionPlaying(Agent agent, ActionIndexCache requested)
    {
        ActionIndexCache current = agent.GetCurrentAction(0);
        return current == requested || (current != ActionIndexCache.act_none
            && agent.ActionSet.AreActionsAlternatives(in current, in requested));
    }

    private static bool IsCampPosePlaying(CampAgent entry) => IsCampActionPlaying(entry.Agent, entry.PoseAction);

    private bool TryPlayCampPose(CampAgent entry, string[] pool)
    {
        if (entry.PoseTransition != CampPoseTransition.None) return true;
        CampRestPose rest = pool == SitActions ? CampRestPose.Sit : pool == LieActions ? CampRestPose.Lie : CampRestPose.None;
        bool ambient = rest != CampRestPose.None || pool == StandActions || pool == LoiterActions || pool == TalkActions
            || pool == ListenActions || pool == GossipActions || pool == SentryActions || pool == LookoutActions || pool == LeanActions;
        for (int i = 0; i < pool.Length; i++)
        {
            string name = pool[(entry.Agent.Index + entry.PoseVariant + i) % pool.Length];
            ActionIndexCache action = ResolveAction(name);
            if (action == ActionIndexCache.act_none) continue;
            CampPoseClip clip = GetCampPoseClip(entry.Agent, action);
            // Ambient one-shots are not blindly restarted as "loops". Only the two explicit native
            // ground cycle clips below may be made cyclic; never loop a sit/stand transition.
            if (!clip.Available || (ambient && rest == CampRestPose.None && !clip.Cyclic)) continue;
            if (IsCampActionPlaying(entry.Agent, action))
            {
                entry.PoseAction = action;
                entry.RestPose = rest;
                return true;
            }
            CampRestPose previousRest = entry.RestPose;
            ActionIndexCache previousAction = entry.PoseAction;
            entry.RestPose = rest;
            entry.PoseAction = action;
            if (rest != CampRestPose.None && TryBeginCampPoseTransition(entry,
                rest == CampRestPose.Sit ? "act_dungeon_prisoner_sitting_ground" : "act_dungeon_prisoner_sleep", CampPoseTransition.Enter))
            {
                HideHandItems(entry);
                return true;
            }
            if (StartCampPoseHold(entry, action, ((entry.Agent.Index * 17 + entry.PoseVariant * 7) % 53) / 100f))
            {
                ShowHandItems(entry, name);
                return true;
            }
            entry.RestPose = previousRest;
            entry.PoseAction = previousAction;
        }
        // A game/MOD may redefine a talk gesture as one-shot. Prefer native calm idles then,
        // rather than forcing a cut back into the same gesture every upkeep.
        if (ambient && rest == CampRestPose.None && pool != StandActions) return TryPlayCampPose(entry, StandActions);
        return false;
    }

    private static bool StartCampPoseHold(CampAgent entry, ActionIndexCache action, float progress)
    {
        AnimFlags flags = AnimFlags.anf_lock_movement;
        if (entry.RestPose != CampRestPose.None) flags |= AnimFlags.anf_cyclic;
        return entry.Agent.SetActionChannel(0, in action, false, flags,
            actionSpeed: 0.9f + (entry.Agent.Index % 6) * 0.02f,
            blendInPeriod: 0.3f, blendOutPeriodToNoAnim: 0.4f, startProgress: progress);
    }

    private bool TryBeginCampPoseTransition(CampAgent entry, string name, CampPoseTransition phase)
    {
        ActionIndexCache action = ResolveAction(name);
        if (action == ActionIndexCache.act_none) return false;
        CampPoseClip clip = GetCampPoseClip(entry.Agent, action);
        if (!clip.Available || !entry.Agent.SetActionChannel(0, in action, false, AnimFlags.anf_lock_movement,
            blendInPeriod: 0.3f, blendOutPeriodToNoAnim: 0.6f)) return false;
        entry.PoseTransition = phase;
        entry.TransitionAction = action;
        entry.PoseTransitionStartedAt = _elapsed;
        entry.PoseTransitionDeadline = _elapsed + (clip.Duration > 0f && clip.Duration < 20f ? clip.Duration : 3f) + 0.5f;
        return true;
    }

    private bool TryBeginCampPoseExit(CampAgent entry)
    {
        if (entry.RestPose == CampRestPose.None) return false;
        return TryBeginCampPoseTransition(entry,
            entry.RestPose == CampRestPose.Sit ? "act_dungeon_prisoner_sitting_ground_standup" : "act_dungeon_prisoner_sleep_wakeup",
            CampPoseTransition.Exit);
    }

    // Called by AdvanceCampWalks, within its existing 20-agent/frame budget and owner exclusions.
    private bool AdvanceCampPoseTransition(CampAgent entry)
    {
        CampPoseTransition phase = entry.PoseTransition;
        if (phase == CampPoseTransition.None) return false;
        if (_elapsed - entry.PoseTransitionStartedAt < 0.15f) return true;
        if (_elapsed < entry.PoseTransitionDeadline && IsCampActionPlaying(entry.Agent, entry.TransitionAction)
            && entry.Agent.GetCurrentActionProgress(0) < 0.96f) return true;
        entry.PoseTransition = CampPoseTransition.None;
        entry.TransitionAction = ActionIndexCache.act_none;
        if (phase == CampPoseTransition.Enter) StartCampPoseHold(entry, entry.PoseAction, 0f);
        else
        {
            // The route was prepared before standing up. Release the completed transition, tools,
            // training and upper-body channels only now, not at the start of a failed move request.
            ReleasePose(entry);
            IssueCampWalk(entry);
        }
        return true;
    }

    private static bool HoldCampRestForConversation(CampAgent entry)
    {
        if (!entry.Seated || entry.RestPose == CampRestPose.None || entry.PoseTransition == CampPoseTransition.Exit) return false;
        if (entry.PoseTransition == CampPoseTransition.Enter)
        {
            // Conversation owns this agent now, so the daily transition scheduler will skip him.
            // Settle into the same seated cycle, not act_none or a standing conversation action.
            if (!StartCampPoseHold(entry, entry.PoseAction, 0f)) return false;
            entry.PoseTransition = CampPoseTransition.None;
            entry.TransitionAction = ActionIndexCache.act_none;
        }
        return true;
    }

    private bool RefreshCampGroundRest(CampAgent entry)
    {
        if (entry.RestPose == CampRestPose.None || entry.Returning) return false;
        entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(55f, 95f);
        // Most residents remain seated. A real stroll uses the natural stand-up transition;
        // no standing eating/talking clip is inserted just to vary a ground-sitting pose.
        if (entry.Activity != Activity.Wounded && MBRandom.RandomFloat < 0.2f && TryNearbyStroll(entry, out Vec3 stroll))
        {
            entry.Returning = true;
            MoveTo(entry, stroll, entry.HomeFacing, run: false);
        }
        return true;
    }
}
