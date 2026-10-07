using System;
using System.Collections.Generic;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private readonly Dictionary<Agent, Team> _suspendedExecutionEscorts = new();
    private readonly List<Agent> _releasedExecutionEscorts = new();
    private bool _executionEscortControlActive;
    private bool _spawningExecutionAgent;

    private void SuspendExistingExecutionEscorts()
    {
        // One entry scan, before the prisoner is spawned. Late arrivals are
        // handled by OnAgentBuild; the mission tick never scans all agents.
        _executionEscortControlActive = true;
        foreach (var agent in Mission.Agents)
            SuspendExecutionEscort(agent);
    }

    public override void OnAgentBuild(Agent agent, Banner banner)
    {
        base.OnAgentBuild(agent, banner);
        TrySuspendArrivingExecutionEscort(agent);
    }

    public override void OnAgentTeamChanged(Team previousTeam, Team newTeam, Agent agent)
    {
        base.OnAgentTeamChanged(previousTeam, newTeam, agent);
        if (agent is null) return;
        // Native location arrivals can receive the player team after SpawnAgent
        // returns. A team change also relinquishes any older control snapshot.
        if (previousTeam != newTeam)
            _suspendedExecutionEscorts.Remove(agent);
        TrySuspendArrivingExecutionEscort(agent);
    }

    private void TrySuspendArrivingExecutionEscort(Agent agent)
    {
        if (!_executionEscortControlActive || _spawningExecutionAgent || _cleanupComplete)
            return;

        try
        {
            SuspendExecutionEscort(agent);
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not suspend a newly arrived execution escort.", exception);
            if (!_lethalAttempted)
                CancelSceneAndReturn(ExecutionFailureReason.ScenePlacementFailed,
                    new TextObject("{=REX_Error_Scene_Build}The execution site could not be built safely. Nothing was spent and the prisoner lives."));
        }
    }

    private void SuspendExecutionEscort(Agent agent)
    {
        if (agent is null || !agent.IsActive() || !agent.IsHuman ||
            ReferenceEquals(agent, _playerAgent) || ReferenceEquals(agent, Mission.MainAgent) ||
            ReferenceEquals(agent, _victimAgent) ||
            _executionTeam is null || !_executionTeam.IsValid || agent.Team != _executionTeam ||
            agent.Controller != AgentControllerType.AI || _spawnedAgents.Contains(agent) ||
            _suspendedExecutionEscorts.ContainsKey(agent))
            return;

        // Retain team/formation/navigation and record before the native setter
        // so cancellation can restore even a partially successful engine call.
        _suspendedExecutionEscorts.Add(agent, agent.Team);
        agent.Controller = AgentControllerType.None;
    }

    private bool RestoreExecutionEscortControl(bool force = false)
    {
        if (!force)
        {
            if (State != ExecutionSessionState.Aftermath || !_playerExecutionStateRestored ||
                !_aftermathConversationsRestored || !_aftermathSessionReleased)
                return false;

            // Some methods retain a living frozen visual until mission exit.
            // Keep escorts paused if its transfer out of the hostile side failed.
            if (_victimAgent is not null && _victimAgent.IsActive() &&
                _victimAgent.Team is not null && _victimAgent.Team.IsValid &&
                _executionTeam is not null && _executionTeam.IsValid &&
                (_executionTeam.IsEnemyOf(_victimAgent.Team) ||
                 _victimAgent.Team.IsEnemyOf(_executionTeam)))
                return false;
        }

        _executionEscortControlActive = false;
        _releasedExecutionEscorts.Clear();
        foreach (var entry in _suspendedExecutionEscorts)
        {
            try
            {
                var agent = entry.Key;
                // Another mechanism's controller/team change relinquishes our
                // ownership. Never overwrite a new fight or player takeover.
                if (agent.IsActive() && agent.Team == entry.Value &&
                    agent.Controller == AgentControllerType.None)
                    agent.Controller = AgentControllerType.AI;
                _releasedExecutionEscorts.Add(agent);
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not restore an execution escort; cleanup will retry.", exception);
            }
        }
        foreach (var agent in _releasedExecutionEscorts)
            _suspendedExecutionEscorts.Remove(agent);
        _releasedExecutionEscorts.Clear();
        return _suspendedExecutionEscorts.Count == 0;
    }
}
