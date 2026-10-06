using System;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private readonly ExecutionCrowdDispersalSchedule _crowdDispersal = new();

    private void TickCrowdDispersal(float dt)
    {
        // Aftermath is entered before restoration succeeds. Keep spectators in
        // place until the ceremony actually hands the town back to the player.
        if (State != ExecutionSessionState.Aftermath || _cleanupComplete
            || !_playerExecutionStateRestored || !_aftermathConversationsRestored
            || !_aftermathSessionReleased
            || !_crowdDispersal.TryTakeNext(dt, _crowdAgents.Count, out var index))
            return;

        var completed = false;
        try
        {
            completed = TryReleaseCrowdAgent(_crowdAgents[index]);
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not release execution spectator {index}; retries are bounded.", exception);
        }
        finally
        {
            _crowdDispersal.CompleteAttempt(completed);
        }
    }

    private bool TryReleaseCrowdAgent(Agent agent)
    {
        if (agent is null || !agent.IsActive()) return true;
        // A new fight or another interaction can take ownership while the
        // staggered release is still running. Never clear its actions/targets.
        if (agent.Controller != AgentControllerType.None || agent.Team != _executionTeam
            || agent.CurrentlyUsedGameObject != null)
            return true;

        var handler = Mission.GetMissionBehavior<MissionAgentHandler>();
        if (CampaignMission.Current?.Location == null || handler == null
            || (!handler.HasUsablePointWithTag("npc_common")
                && !handler.HasUsablePointWithTag("npc_common_limited")))
        {
            RexLog.Warning($"Spectator {agent.Index} cannot disperse: no native town walking points are available.");
            return false;
        }

        var component = agent.GetComponent<CampaignAgentComponent>();
        if (component == null)
        {
            component = new CampaignAgentComponent(agent);
            agent.AddComponent(component);
        }
        var navigator = component.AgentNavigator ?? component.CreateAgentNavigator();
        // Native WalkingBehavior chooses real usable town objects, not invented
        // coordinates. No ChangeLocationBehavior: these are temporary residents.
        navigator.SpecialTargetTag = "npc_common";
        BehaviorSets.AddWandererBehaviors(agent);
        navigator.ClearTarget();
        agent.DisableScriptedMovement();
        agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
        var none = ActionIndexCache.act_none;
        bool cleared = true;
        for (var channel = 0; channel < 2; channel++)
        {
            if (agent.GetCurrentAction(channel) != none)
                cleared &= agent.SetActionChannel(channel, in none, ignorePriority: true);
        }
        if (!cleared) return false;
        // A ceremony death can leave a frozen spectator alarmed. Having checked
        // that no other owner took it, resume peaceful walking rather than flee.
        agent.SetWatchState(Agent.WatchState.Patrolling);
        agent.Controller = AgentControllerType.AI;
        RexLog.Info($"Session {Request.SessionId} released spectator {agent.Index} to native town walking.");
        return true;
    }
}
