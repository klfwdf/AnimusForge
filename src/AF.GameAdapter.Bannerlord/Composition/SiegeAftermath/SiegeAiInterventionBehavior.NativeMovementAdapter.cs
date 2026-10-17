using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using SandBox;
using SandBox.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
    private static readonly SiegeNativeMovementOrders NativeMovementOrders = new SiegeNativeMovementOrders();

    private static bool TrySetInterventionAgentTargetPosition(Agent agent, Vec3 target, string source,
        Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.NeverSlowDown)
    {
        try
        {
            Mission mission = Mission.Current;
            bool eligible = IsEligibleNativeMovementActor(agent, mission);
            if (!eligible)
            {
                NativeMovementOrders.Forget(agent?.Index ?? -1);
                return false;
            }
            CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
            AgentNavigator navigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
            float tolerance = SiegeNativeMovementOrders.TargetChangeDistance;
            bool matchingTarget = navigator != null && navigator.TargetPosition.IsValid
                && navigator.TargetPosition.AsVec2.DistanceSquared(target.AsVec2) <= tolerance * tolerance;
            SiegeNativeMovementOrderDecision decision = NativeMovementOrders.Request(agent.Index, true, source,
                mission.CurrentTime, target.x, target.y, target.z, (int)flags, matchingTarget);
            if (decision == SiegeNativeMovementOrderDecision.Reject) return false;
            if (decision == SiegeNativeMovementOrderDecision.KeepNativeOrder) return true;
            return TryAssignNativeMovementFrame(agent, mission, navigator, target, flags, source);
        }
        catch (Exception ex)
        {
            Logger.Log("GcczMovement", "Native movement order failed. Agent=" + (agent?.Index ?? -1)
                + ", Source=" + source + ", Error=" + ex.GetType().Name);
            return false;
        }
    }

    private static bool IsEligibleNativeMovementActor(Agent agent, Mission mission)
    {
        return IsActiveInCurrentMission() && mission?.Scene != null && !mission.IsMissionEnding
            && agent != null && agent.Mission == mission && !agent.IsMainAgent && agent.IsHuman && agent.IsActive()
            && agent.Controller == AgentControllerType.AI && !agent.IsPaused
            && !ConversationMission.ConversationAgents.Contains(agent)
            && !CastleAftermathLordDuelRuntimeBridge.ControlsAgent(agent)
            && (AlliedAgentIndexes.Contains(agent.Index) || IsEligibleCivilianAgent(agent, includeHeroes: true));
    }

    private static bool TryAssignNativeMovementFrame(Agent agent, Mission mission, AgentNavigator navigator,
        Vec3 target, Agent.AIScriptedFrameFlags flags, string source)
    {
        if (navigator == null)
        {
            Logger.Log("GcczMovement", "Native navigator unavailable. Agent=" + agent.Index + ", Source=" + source);
            return false;
        }
        bool localFlee = string.Equals(source, SiegeLocalCivilianReactionProfile.WitnessFleeSource + ":long_flee", StringComparison.Ordinal);
        WorldPosition position = new WorldPosition(mission.Scene, target);
        bool hasNavmesh = localFlee
            ? TryResolveLocalFleeWorldPosition(mission, agent.Position, target, out position)
            : position.GetNavMesh() != UIntPtr.Zero;
        if (!hasNavmesh)
        {
            // A failed local-flee probe must not cancel an already-running native escape.
            if (!localFlee) navigator.ClearTarget();
            Logger.Log("GcczMovement", "No native navmesh target; movement deferred. Agent=" + agent.Index + ", Source=" + source);
            return false;
        }
        Vec3 resolvedTarget = position.GetNavMeshVec3();
        Vec2 direction = resolvedTarget.AsVec2 - agent.Position.AsVec2;
        float rotation = direction.LengthSquared > 0.04f ? direction.RotationInRadians : agent.LookDirection.AsVec2.RotationInRadians;
        navigator.SetTargetFrame(position, rotation, SiegeAgentWallRescueProfile.NativeTargetFrameArrivalRadius,
            SiegeAgentWallRescueProfile.NativeTargetFrameStopDistance, flags, false);
        SetAgentLookTowardPoint(agent, resolvedTarget);
        return true;
    }

    // Restore the pre-ee796fec nearest-navmesh/nearby-candidate behavior only for
    // local civilian escape; guard, follow, gather and duel movement keep their contract.
    private static bool TryResolveLocalFleeWorldPosition(Mission mission, Vec3 origin, Vec3 target, out WorldPosition position)
    {
        Vec3 grounded = ProjectCivilianRoutPointToGround(mission, target);
        position = new WorldPosition(mission.Scene, grounded);
        if (position.GetNearestNavMesh() != UIntPtr.Zero) return true;

        WorldPosition best = WorldPosition.Invalid;
        float bestScore = float.MinValue;
        Vec2 desired = grounded.AsVec2 - origin.AsVec2;
        if (desired.LengthSquared > 0.04f) desired.Normalize();
        for (int i = 0; i < SiegeAgentWallRescueProfile.NativeTargetFrameSampleCount; i++)
        {
            Vec3 candidate = mission.GetRandomPositionAroundPoint(grounded,
                SiegeAgentWallRescueProfile.NativeTargetFrameSampleMinRadius,
                SiegeAgentWallRescueProfile.NativeTargetFrameSampleMaxRadius, i % 2 == 0);
            WorldPosition world = new WorldPosition(mission.Scene, candidate);
            if (world.GetNearestNavMesh() == UIntPtr.Zero) continue;
            Vec2 direction = world.AsVec2 - origin.AsVec2;
            if (direction.LengthSquared < 0.25f) continue;
            direction.Normalize();
            float alignment = Vec2.DotProduct(direction, desired);
            if (alignment < 0f) continue;
            float score = -world.AsVec2.DistanceSquared(grounded.AsVec2) + alignment;
            if (score > bestScore) { best = world; bestScore = score; }
        }
        if (bestScore == float.MinValue) return false;
        position = best;
        return true;
    }
}
