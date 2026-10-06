using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using SandBox;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
    private static readonly SiegeStuckRecovery StuckRecovery = new SiegeStuckRecovery();
    private static float _nextStuckRecoverySampleTime;

    private static void TickInterventionStuckRecovery(Mission mission)
    {
        if (NativeMovementOrders.Count == 0 || mission?.Agents == null || mission.CurrentTime < _nextStuckRecoverySampleTime) return;
        _nextStuckRecoverySampleTime = mission.CurrentTime + SiegeStuckRecovery.SampleSeconds;
        foreach (Agent agent in mission.Agents)
        {
            if (agent == null || !NativeMovementOrders.TryGetOrder(agent.Index, out SiegeNativeMovementOrders.Order order)) continue;
            try
            {
                AgentNavigator navigator = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
                Vec3 target = new Vec3(order.X, order.Y, order.Z);
                bool eligible = IsEligibleNativeMovementActor(agent, mission) && agent.MountAgent == null
                    && !agent.IsUsingGameObject && !agent.IsInBeingStruckAction && agent.GetMaximumSpeedLimit() != 0f
                    && IsRecoveryLocomotionAction(agent.GetCurrentActionType(0)) && IsRecoveryLocomotionAction(agent.GetCurrentActionType(1))
                    && (agent.GetScriptedFlags() & (Agent.AIScriptedFrameFlags.GoToPosition | Agent.AIScriptedFrameFlags.InConversation
                        | Agent.AIScriptedFrameFlags.Crouch)) == Agent.AIScriptedFrameFlags.GoToPosition
                    && navigator != null && navigator.TargetPosition.IsValid
                    && navigator.TargetPosition.AsVec2.DistanceSquared(target.AsVec2)
                        <= SiegeNativeMovementOrders.TargetChangeDistance * SiegeNativeMovementOrders.TargetChangeDistance;
                Vec3 current = agent.Position;
                SiegeStuckRecoveryDecision decision = StuckRecovery.Observe(agent.Index, eligible, mission.CurrentTime,
                    current.x, current.y, current.z, order);
                if (decision == SiegeStuckRecoveryDecision.RetryNativePath)
                {
                    bool assigned = TryAssignNativeMovementFrame(agent, mission, navigator, target,
                        (Agent.AIScriptedFrameFlags)order.Flags, "stuck_native_retry");
                    StuckRecovery.RecordNativeRetry(agent.Index, assigned);
                }
                else if (decision == SiegeStuckRecoveryDecision.TryShortHop)
                {
                    string reason = "no_safe_landing";
                    if (TryFindShortRecoveryLanding(agent, mission, target, out Vec3 landing, out reason))
                    {
                        navigator.ClearTarget();
                        agent.TeleportToPosition(landing);
                        StuckRecovery.Suspend(agent.Index);
                        bool assigned = TryAssignNativeMovementFrame(agent, mission, navigator, target,
                            (Agent.AIScriptedFrameFlags)order.Flags, "stuck_hop_resume");
                        Logger.Log("GcczMovement", "Applied guarded short recovery. Agent=" + agent.Index
                            + ", MinimumStallSeconds=" + SiegeStuckRecovery.StuckSeconds + ", NativeOrderResumed=" + assigned);
                    }
                    else
                    {
                        Logger.Log("GcczMovement", "Skipped guarded short recovery. Agent=" + agent.Index + ", Reason=" + reason);
                    }
                }
            }
            catch (Exception ex)
            {
                StuckRecovery.Suspend(agent.Index);
                Logger.Log("GcczMovement", "Stuck recovery failed closed. Agent=" + agent.Index + ", Error=" + ex.GetType().Name);
            }
        }
    }

    private static bool IsRecoveryLocomotionAction(Agent.ActionCodeType action)
    {
        return action == Agent.ActionCodeType.Other || action == Agent.ActionCodeType.Idle || action == Agent.ActionCodeType.Guard;
    }

    private static bool TryFindShortRecoveryLanding(Agent agent, Mission mission, Vec3 target, out Vec3 landing, out string reason)
    {
        landing = agent.Position;
        reason = "no_nearby_scene_obstruction";
        Vec3 origin = agent.Position;
        Vec3 forward = target - origin;
        forward.z = 0f;
        if (forward.LengthSquared < 0.01f) return false;
        forward.Normalize();
        Vec3 chest = origin + Vec3.Up * 0.8f;
        Vec3 forwardProbe = chest + forward * 1.2f;
        if (!mission.Scene.RayCastForClosestEntityOrTerrain(chest, forwardProbe, out float _, 0.2f, BodyFlags.None)
            && !mission.Scene.RayCastForClosestEntityOrTerrain(forwardProbe, chest, out float _, 0.2f, BodyFlags.None)) return false;
        CapsuleData body = agent.CollisionCapsule;
        float radius = body.Radius + 0.02f;
        if (!body.P1.IsValid || !body.P2.IsValid || float.IsNaN(radius) || float.IsInfinity(radius)
            || radius <= 0.05f || radius > 1f) { reason = "invalid_actor_capsule"; return false; }
        WorldPosition goal = new WorldPosition(mission.Scene, target);
        if (goal.GetNavMesh() == UIntPtr.Zero) { reason = "no_goal_navmesh"; return false; }
        var occupied = new List<Vec3>();
        foreach (Agent other in mission.Agents)
            if (other != null && other != agent && other.IsActive()) occupied.Add(other.Position);
        reason = "no_safe_landing";
        for (int i = 0; i < SiegeStuckRecovery.LandingSamples; i++)
        {
            Vec3 sample = mission.GetRandomPositionAroundPoint(origin, SiegeStuckRecovery.MinimumHopDistance,
                SiegeStuckRecovery.MaximumHopDistance, true);
            WorldPosition candidate = new WorldPosition(mission.Scene, sample);
            if (!candidate.IsValid || candidate.GetNavMesh() == UIntPtr.Zero || !mission.IsPositionInsideBoundaries(candidate.AsVec2)) continue;
            Vec3 point = candidate.GetNavMeshVec3();
            if (!point.IsValid || !SiegeStuckRecovery.IsShortHopGeometryValid(point.AsVec2.DistanceSquared(origin.AsVec2), point.z - origin.z)) continue;
            bool overlaps = false;
            foreach (Vec3 other in occupied)
                if (Math.Abs(other.z - point.z) < 2f && other.AsVec2.DistanceSquared(point.AsVec2) < (radius + 0.8f) * (radius + 0.8f))
                { overlaps = true; break; }
            if (overlaps) continue;
            Vec3 offset = point - origin + Vec3.Up * 0.08f;
            if (mission.Scene.RayCastForClosestEntityOrTerrain(body.P1 + offset, body.P2 + offset, out float _, radius, BodyFlags.None)) continue;
            if (!mission.Scene.RayCastForClosestEntityOrTerrain(point + Vec3.Up * 0.4f, point - Vec3.Up * 0.5f,
                out float _, out Vec3 floor, 0.01f, BodyFlags.None) || Math.Abs(floor.z - point.z) > 0.2f) continue;
            if (!mission.Scene.GetPathDistanceBetweenPositions(ref candidate, ref goal, radius, out float pathDistance)
                || float.IsNaN(pathDistance) || float.IsInfinity(pathDistance) || pathDistance < 0) continue;
            landing = point;
            reason = "safe_short_hop";
            return true;
        }
        return false;
    }
}
