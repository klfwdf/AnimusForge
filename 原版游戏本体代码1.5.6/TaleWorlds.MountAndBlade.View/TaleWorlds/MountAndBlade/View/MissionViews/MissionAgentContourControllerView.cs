using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionAgentContourControllerView : MissionView
{
	private const bool IsEnabled = false;

	private uint _nonFocusedContourColor = new Color(0.85f, 0.85f, 0.85f).ToUnsignedInteger();

	private uint _focusedContourColor = new Color(1f, 0.84f, 0.35f).ToUnsignedInteger();

	private uint _friendlyContourColor = new Color(0.44f, 0.83f, 0.26f).ToUnsignedInteger();

	private List<Agent> _contourAgents;

	private Agent _currentFocusedAgent;

	private bool _isContourAppliedToAllAgents;

	private bool _isContourAppliedToFocusedAgent;

	private bool _isMultiplayer;

	private bool _isAllowedByOption
	{
		get
		{
			if (BannerlordConfig.HideBattleUI)
			{
				return GameNetwork.IsMultiplayer;
			}
			return true;
		}
	}

	public MissionAgentContourControllerView()
	{
		_contourAgents = new List<Agent>();
		_isMultiplayer = GameNetwork.IsSessionActive;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isAllowedByOption)
		{
			_ = NativeConfig.GetUIDebugMode;
		}
	}

	private void PopulateContourListWithAgents()
	{
		_contourAgents.Clear();
		if (base.Mission?.PlayerTeam?.PlayerOrderController == null)
		{
			return;
		}
		foreach (Formation selectedFormation in Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations)
		{
			selectedFormation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				if (!agent.IsMainAgent)
				{
					_contourAgents.Add(agent);
				}
			});
		}
	}

	public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
	{
		base.OnFocusGained(agent, focusableObject, isInteractable);
		_ = _isAllowedByOption;
	}

	public override void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		base.OnFocusLost(agent, focusableObject);
		if (_isAllowedByOption)
		{
			RemoveContourFromFocusedAgent();
			_currentFocusedAgent = null;
		}
	}

	private void AddContourToFocusedAgent()
	{
		if (_currentFocusedAgent != null && !_isContourAppliedToFocusedAgent)
		{
			_currentFocusedAgent.AgentVisuals?.SetContourColor(_focusedContourColor);
			_isContourAppliedToFocusedAgent = true;
		}
	}

	private void RemoveContourFromFocusedAgent()
	{
		if (_currentFocusedAgent != null && _isContourAppliedToFocusedAgent)
		{
			if (_contourAgents.Contains(_currentFocusedAgent))
			{
				_currentFocusedAgent.AgentVisuals?.SetContourColor(_nonFocusedContourColor);
			}
			else
			{
				_currentFocusedAgent.AgentVisuals?.SetContourColor(null);
			}
			_isContourAppliedToFocusedAgent = false;
		}
	}

	private void ApplyContourToAllAgents()
	{
		if (_isContourAppliedToAllAgents)
		{
			return;
		}
		foreach (Agent contourAgent in _contourAgents)
		{
			uint value = ((contourAgent == _currentFocusedAgent) ? _focusedContourColor : (_isMultiplayer ? _friendlyContourColor : _nonFocusedContourColor));
			contourAgent.AgentVisuals?.SetContourColor(value);
		}
		_isContourAppliedToAllAgents = true;
	}

	private void RemoveContourFromAllAgents()
	{
		if (!_isContourAppliedToAllAgents)
		{
			return;
		}
		foreach (Agent contourAgent in _contourAgents)
		{
			if (_currentFocusedAgent == null || contourAgent != _currentFocusedAgent)
			{
				contourAgent.AgentVisuals?.SetContourColor(null);
			}
		}
		_isContourAppliedToAllAgents = false;
	}
}
