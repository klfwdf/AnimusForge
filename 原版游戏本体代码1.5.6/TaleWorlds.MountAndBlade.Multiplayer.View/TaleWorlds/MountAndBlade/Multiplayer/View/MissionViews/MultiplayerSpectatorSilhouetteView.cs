using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

public class MultiplayerSpectatorSilhouetteView : MissionView
{
	private const float ReapplyIntervalSeconds = 0.5f;

	private const float HoverMaxScreenDistanceSq = 0.0016f;

	private static readonly uint HoveredContourColor = new Color(0.957f, 0.882f, 0.769f).ToUnsignedInteger();

	private static readonly uint FollowedContourColor = Color.White.ToUnsignedInteger();

	private readonly HashSet<Agent> _contouredAgents = new HashSet<Agent>();

	private bool _isActive;

	private float _reapplyTimer;

	private Agent _hoveredAgent;

	private Agent _followedAgent;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (MultiplayerSpectatorHelper.IsLocalPeerStreamer())
		{
			_reapplyTimer += dt;
			bool flag = _reapplyTimer >= 0.5f;
			if (flag)
			{
				_reapplyTimer = 0f;
			}
			UpdateAccentedAgents(flag);
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

	private void UpdateAccentedAgents(bool forceReapply)
	{
		Agent hoveredAgent = _hoveredAgent;
		Agent followedAgent = _followedAgent;
		_hoveredAgent = FindHoveredAgent();
		Agent lastFollowedAgent = base.MissionScreen.LastFollowedAgent;
		_followedAgent = ((lastFollowedAgent != null && lastFollowedAgent.IsActive()) ? lastFollowedAgent : null);
		if (!forceReapply)
		{
			RepaintIfAccentLost(hoveredAgent);
			RepaintIfAccentLost(followedAgent);
		}
	}

	private void RepaintIfAccentLost(Agent agent)
	{
		if (agent != null && agent != _hoveredAgent && agent != _followedAgent)
		{
			ApplyContour(agent, GetContourColorFor(agent));
		}
	}

	private Agent FindHoveredAgent()
	{
		if (!base.MissionScreen.MouseVisible || base.MissionScreen.RightButtonDraggingMode)
		{
			return null;
		}
		Vec2 mousePositionRanged = base.MissionScreen.SceneLayer.Input.GetMousePositionRanged();
		Agent result = null;
		float num = 0.0016f;
		foreach (Agent agent in base.Mission.Agents)
		{
			if (!agent.IsActive() || !agent.IsCameraAttachable() || agent.MissionPeer == null)
			{
				continue;
			}
			Vec3 position = agent.VisualPosition + new Vec3(0f, 0f, 1.2f);
			Vec2 vec = base.MissionScreen.SceneLayer.WorldPointToScreenPoint(position);
			if (!(vec.x < 0f) && !(vec.x > 1f) && !(vec.y < 0f) && !(vec.y > 1f))
			{
				float num2 = vec.DistanceSquared(mousePositionRanged);
				if (num2 < num)
				{
					num = num2;
					result = agent;
				}
			}
		}
		return result;
	}

	private uint GetContourColorFor(Agent agent)
	{
		if (agent == _hoveredAgent)
		{
			return HoveredContourColor;
		}
		if (agent == _followedAgent)
		{
			return FollowedContourColor;
		}
		if (agent.Team == null)
		{
			return Color.White.ToUnsignedInteger();
		}
		return agent.Team.Color;
	}

	private void RefreshContours(bool forceReapply)
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsActive() && agent.IsHuman)
			{
				bool flag = agent == _hoveredAgent || agent == _followedAgent;
				if (forceReapply || flag || !_contouredAgents.Contains(agent))
				{
					ApplyContour(agent, GetContourColorFor(agent));
				}
			}
		}
	}

	private void ApplyContour(Agent agent, uint color)
	{
		MBAgentVisuals safeAgentVisuals = GetSafeAgentVisuals(agent);
		if (!(safeAgentVisuals == null))
		{
			safeAgentVisuals.SetContourColor(color);
			_contouredAgents.Add(agent);
		}
	}

	private void ClearContours()
	{
		foreach (Agent contouredAgent in _contouredAgents)
		{
			GetSafeAgentVisuals(contouredAgent)?.SetContourColor(null);
		}
		_contouredAgents.Clear();
		_hoveredAgent = null;
		_followedAgent = null;
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

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (_contouredAgents.Contains(affectedAgent))
		{
			GetSafeAgentVisuals(affectedAgent)?.SetContourColor(null);
		}
		ForgetAgent(affectedAgent);
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		base.OnAgentDeleted(affectedAgent);
		ForgetAgent(affectedAgent);
	}

	private void ForgetAgent(Agent agent)
	{
		_contouredAgents.Remove(agent);
		if (_hoveredAgent == agent)
		{
			_hoveredAgent = null;
		}
		if (_followedAgent == agent)
		{
			_followedAgent = null;
		}
	}

	public override void OnClearScene()
	{
		_contouredAgents.Clear();
		_hoveredAgent = null;
		_followedAgent = null;
		_isActive = false;
		_reapplyTimer = 0f;
	}

	public override void OnMissionScreenFinalize()
	{
		_contouredAgents.Clear();
		_hoveredAgent = null;
		_followedAgent = null;
		_isActive = false;
		_reapplyTimer = 0f;
		base.OnMissionScreenFinalize();
	}
}
