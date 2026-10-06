using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

public class MultiplayerSpectatorSilhouetteView : MissionView
{
	private const float ReapplyIntervalSeconds = 0.5f;

	private readonly HashSet<Agent> _contouredAgents = new HashSet<Agent>();

	private bool _isActive;

	private float _reapplyTimer;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (MultiplayerSpectatorHelper.IsStreamerModeActive())
		{
			_reapplyTimer += dt;
			bool flag = _reapplyTimer >= 0.5f;
			if (flag)
			{
				_reapplyTimer = 0f;
			}
			RefreshContours(flag);
			_isActive = true;
		}
		else if (_isActive)
		{
			ClearContours();
			_isActive = false;
			_reapplyTimer = 0f;
		}
	}

	private void RefreshContours(bool forceReapply)
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsActive() && agent.IsHuman && (forceReapply || !_contouredAgents.Contains(agent)))
			{
				MBAgentVisuals safeAgentVisuals = GetSafeAgentVisuals(agent);
				if (!(safeAgentVisuals == null))
				{
					uint value = ((agent.Team != null) ? agent.Team.Color : Color.White.ToUnsignedInteger());
					safeAgentVisuals.SetContourColor(value);
					_contouredAgents.Add(agent);
				}
			}
		}
	}

	private void ClearContours()
	{
		foreach (Agent contouredAgent in _contouredAgents)
		{
			GetSafeAgentVisuals(contouredAgent)?.SetContourColor(null);
		}
		_contouredAgents.Clear();
	}

	private static MBAgentVisuals GetSafeAgentVisuals(Agent agent)
	{
		if (agent == null || agent.State == AgentState.Deleted)
		{
			return null;
		}
		MBAgentVisuals agentVisuals = agent.AgentVisuals;
		if (agentVisuals == null || !agentVisuals.IsValid())
		{
			return null;
		}
		return agentVisuals;
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		base.OnAgentDeleted(affectedAgent);
		_contouredAgents.Remove(affectedAgent);
	}

	public override void OnClearScene()
	{
		_contouredAgents.Clear();
		_isActive = false;
		_reapplyTimer = 0f;
	}

	public override void OnMissionScreenFinalize()
	{
		_contouredAgents.Clear();
		_isActive = false;
		_reapplyTimer = 0f;
		base.OnMissionScreenFinalize();
	}
}
