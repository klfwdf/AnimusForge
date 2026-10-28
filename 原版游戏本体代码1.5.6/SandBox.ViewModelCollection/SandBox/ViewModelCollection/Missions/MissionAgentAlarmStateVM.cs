using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.ViewModelCollection.Missions;

public class MissionAgentAlarmStateVM : ViewModel
{
	private bool _isInitialized;

	private Mission _mission;

	private Camera _camera;

	private DisguiseMissionLogic _disguiseMissionLogic;

	private bool _areStealthBoxesDirty;

	private List<StealthBox> _stealthBoxes;

	private bool _isMainAgentInSafeArea;

	private MBBindingList<MissionAgentAlarmTargetVM> _targets;

	[DataSourceProperty]
	public MBBindingList<MissionAgentAlarmTargetVM> Targets
	{
		get
		{
			return _targets;
		}
		set
		{
			if (value != _targets)
			{
				_targets = value;
				OnPropertyChangedWithValue(value, "Targets");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainAgentInSafeArea
	{
		get
		{
			return _isMainAgentInSafeArea;
		}
		set
		{
			if (value != _isMainAgentInSafeArea)
			{
				_isMainAgentInSafeArea = value;
				OnPropertyChangedWithValue(value, "IsMainAgentInSafeArea");
			}
		}
	}

	public MissionAgentAlarmStateVM()
	{
		Targets = new MBBindingList<MissionAgentAlarmTargetVM>();
		_stealthBoxes = new List<StealthBox>();
	}

	public void Initialize(Mission mission, Camera camera)
	{
		_mission = mission;
		_camera = camera;
		_isInitialized = true;
		_areStealthBoxesDirty = true;
		RefreshTargets();
		StealthBox.OnBoxInitialized += OnStealthBoxInitialized;
		StealthBox.OnBoxRemoved += OnStealthBoxRemoved;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		StealthBox.OnBoxInitialized -= OnStealthBoxInitialized;
		StealthBox.OnBoxRemoved -= OnStealthBoxRemoved;
	}

	private void OnStealthBoxInitialized(StealthBox stealthBox)
	{
		_areStealthBoxesDirty = true;
	}

	private void OnStealthBoxRemoved(StealthBox stealthBox)
	{
		_areStealthBoxesDirty = true;
	}

	private void RefreshStealthBoxEntities()
	{
		_stealthBoxes.Clear();
		if (Mission.Current?.Scene == null)
		{
			return;
		}
		List<GameEntity> entities = new List<GameEntity>();
		Mission.Current.Scene.GetAllEntitiesWithScriptComponent<StealthBox>(ref entities);
		for (int i = 0; i < entities.Count; i++)
		{
			StealthBox firstScriptOfTypeRecursive = entities[i].GetFirstScriptOfTypeRecursive<StealthBox>();
			if (firstScriptOfTypeRecursive != null)
			{
				_stealthBoxes.Add(firstScriptOfTypeRecursive);
			}
		}
	}

	public void Update()
	{
		if (!_isInitialized)
		{
			return;
		}
		if (_disguiseMissionLogic == null)
		{
			_disguiseMissionLogic = _mission?.GetMissionBehavior<DisguiseMissionLogic>();
		}
		bool isStealthModeEnabled = _disguiseMissionLogic?.IsInStealthMode ?? false;
		IsMainAgentInSafeArea = IsMainAgentInStealthArea();
		for (int i = 0; i < Targets.Count; i++)
		{
			MissionAgentAlarmTargetVM missionAgentAlarmTargetVM = Targets[i];
			if (_disguiseMissionLogic == null)
			{
				missionAgentAlarmTargetVM.IsStealthModeEnabled = true;
				missionAgentAlarmTargetVM.IsMainAgentInVisibilityRange = SandBoxUIHelper.IsAgentInVisibilityRangeApproximate(missionAgentAlarmTargetVM.TargetAgent, Agent.Main);
				missionAgentAlarmTargetVM.IsInVision = true;
				missionAgentAlarmTargetVM.IsSuspected = missionAgentAlarmTargetVM.AlarmProgress > 0;
				missionAgentAlarmTargetVM.UpdateScreenPosition(_camera);
				missionAgentAlarmTargetVM.UpdateValues();
				continue;
			}
			missionAgentAlarmTargetVM.IsStealthModeEnabled = isStealthModeEnabled;
			DisguiseMissionLogic.ShadowingAgentOffenseInfo agentOffenseInfo = _disguiseMissionLogic.GetAgentOffenseInfo(missionAgentAlarmTargetVM.TargetAgent);
			if (agentOffenseInfo != null)
			{
				missionAgentAlarmTargetVM.IsMainAgentInVisibilityRange = SandBoxUIHelper.IsAgentInVisibilityRangeApproximate(missionAgentAlarmTargetVM.TargetAgent, Agent.Main);
				missionAgentAlarmTargetVM.IsInVision = agentOffenseInfo.CanPlayerCameraSeeTheAgent;
				missionAgentAlarmTargetVM.IsSuspected = missionAgentAlarmTargetVM.AlarmProgress > 0;
			}
			missionAgentAlarmTargetVM.UpdateScreenPosition(_camera);
			missionAgentAlarmTargetVM.UpdateValues();
		}
	}

	private bool IsMainAgentInStealthArea()
	{
		Agent main = Agent.Main;
		if (main == null)
		{
			return false;
		}
		if (Mission.Current?.Scene == null)
		{
			return false;
		}
		if (_areStealthBoxesDirty)
		{
			RefreshStealthBoxEntities();
			_areStealthBoxesDirty = false;
		}
		for (int i = 0; i < _stealthBoxes.Count; i++)
		{
			if (_stealthBoxes[i].IsAgentInside(main))
			{
				return true;
			}
		}
		return false;
	}

	public void OnAgentRemoved(Agent agent)
	{
		MissionAgentAlarmTargetVM agentTargetFromAgent = GetAgentTargetFromAgent(agent);
		if (agentTargetFromAgent != null)
		{
			Targets.Remove(agentTargetFromAgent);
		}
	}

	private void RefreshTargets()
	{
		Targets.Clear();
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != null && SandBoxUIHelper.CanAgentBeAlarmed(agent))
			{
				Targets.Add(new MissionAgentAlarmTargetVM(agent, OnRemoveTarget));
			}
		}
	}

	public void OnAgentBuild(Agent agent, Banner banner)
	{
		RefreshTargets();
	}

	public void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		if (agent != null && agent == Agent.Main)
		{
			RefreshTargets();
			return;
		}
		MissionAgentAlarmTargetVM agentTargetFromAgent = GetAgentTargetFromAgent(agent);
		if (agentTargetFromAgent == null && SandBoxUIHelper.CanAgentBeAlarmed(agent))
		{
			Targets.Add(new MissionAgentAlarmTargetVM(agent, OnRemoveTarget));
		}
		else if (agentTargetFromAgent != null && (newTeam == Team.Invalid || newTeam == null || newTeam.IsPlayerAlly))
		{
			Targets.Remove(agentTargetFromAgent);
		}
	}

	private void OnRemoveTarget(MissionAgentAlarmTargetVM targetToRemove)
	{
		Targets.Remove(targetToRemove);
	}

	private MissionAgentAlarmTargetVM GetAgentTargetFromAgent(Agent agent)
	{
		for (int i = 0; i < Targets.Count; i++)
		{
			MissionAgentAlarmTargetVM missionAgentAlarmTargetVM = Targets[i];
			if (missionAgentAlarmTargetVM.TargetAgent == agent)
			{
				return missionAgentAlarmTargetVM;
			}
		}
		return null;
	}
}
