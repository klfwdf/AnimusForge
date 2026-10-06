using System;
using System.Collections.Generic;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private readonly ExecutionCrowdDispersalSchedule _crowdDispersal = new();
    private readonly Dictionary<Agent, ExecutionCrowdRoute> _crowdRoutes = new();
    private bool _crowdDispersalInterrupted;
    private float _crowdExclusionRadius;

    public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
    {
        base.OnAgentAlarmedStateChanged(agent, flag);
        if (State == ExecutionSessionState.Aftermath && flag != Agent.AIStateFlag.None
            && agent != null && agent.Controller == AgentControllerType.None && _crowdAgents.Contains(agent))
            _crowdDispersalInterrupted = true;
    }

    public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent,
        in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
    {
        base.OnAgentHit(affectedAgent, affectorAgent, in attackerWeapon, in blow, in attackCollisionData);
        if (State == ExecutionSessionState.Aftermath) _crowdDispersalInterrupted = true;
    }

    private void TickCrowdDispersal(float dt)
    {
        // Aftermath is entered before restoration succeeds. Keep spectators in
        // place until the ceremony actually hands the town back to the player.
        if (State != ExecutionSessionState.Aftermath || _cleanupComplete
            || _crowdDispersalInterrupted
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

        if (Mission.GetMissionBehavior<MissionFightHandler>()?.IsThereActiveFight() == true)
        {
            _crowdDispersalInterrupted = true;
            return true;
        }

        var handler = Mission.GetMissionBehavior<MissionAgentHandler>();
        if (CampaignMission.Current?.Location == null || handler == null || _placement == null)
        {
            RexLog.Warning($"Spectator {agent.Index} cannot disperse: no native town walking points are available.");
            return false;
        }

        if (!_crowdRoutes.TryGetValue(agent, out var route))
        {
            if (_crowdExclusionRadius <= 0f)
            {
                _crowdExclusionRadius = 4f + (_victimPosition.AsVec2 - _placement.Origin.AsVec2).Length;
                // Use captured ceremony positions, not native entities which
                // may already have been removed during aftermath cleanup.
                foreach (var position in _occupiedSpawnPositions)
                    _crowdExclusionRadius = Math.Max(_crowdExclusionRadius,
                        2f + (position.AsVec2 - _placement.Origin.AsVec2).Length);
            }
            route = new ExecutionCrowdRoute(agent, handler, _placement.Origin.AsVec2, _crowdExclusionRadius);
            _crowdRoutes.Add(agent, route);
        }
        var target = route.FindNext();
        if (target == null)
        {
            RexLog.Warning($"Spectator {agent.Index} has no available outward target/path outside the execution site; bounded retry.");
            return false;
        }

        var component = agent.GetComponent<CampaignAgentComponent>();
        if (component == null)
        {
            component = new CampaignAgentComponent(agent);
            agent.AddComponent(component);
        }
        var navigator = component.AgentNavigator ?? component.CreateAgentNavigator();
        var daily = navigator.AddBehaviorGroup<DailyBehaviorGroup>();
        navigator.AddBehaviorGroup<InterruptingBehaviorGroup>();
        var alarmed = navigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
        if (alarmed.GetBehavior<FleeBehavior>() == null) alarmed.AddBehavior<FleeBehavior>();
        if (alarmed.GetBehavior<FightBehavior>() == null) alarmed.AddBehavior<FightBehavior>();
        daily.RemoveBehavior<WalkingBehavior>();
        var walking = daily.GetBehavior<ExecutionCrowdWalkAwayBehavior>() ?? daily.AddBehavior<ExecutionCrowdWalkAwayBehavior>();
        walking.Configure(route, target);
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
        navigator.SetTarget(target);
        agent.Controller = AgentControllerType.AI;
        RexLog.Info($"Session {Request.SessionId} released spectator {agent.Index} to a verified outward town target.");
        return true;
    }
}
