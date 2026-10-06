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
        WorldPosition position = new WorldPosition(mission.Scene, target);
        if (position.GetNavMesh() == UIntPtr.Zero)
        {
            navigator.ClearTarget();
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
}
