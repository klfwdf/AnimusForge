using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionAgentLockVisualizerVM : ViewModel
{
	private readonly Dictionary<Agent, MissionAgentLockItemVM> _allTrackedAgentsSet;

	private MBBindingList<MissionAgentLockItemVM> _allTrackedAgents;

	private bool _isEnabled;

	[DataSourceProperty]
	public MBBindingList<MissionAgentLockItemVM> AllTrackedAgents
	{
		get
		{
			return _allTrackedAgents;
		}
		set
		{
			if (value != _allTrackedAgents)
			{
				_allTrackedAgents = value;
				OnPropertyChangedWithValue(value, "AllTrackedAgents");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				if (!value)
				{
					AllTrackedAgents.Clear();
					_allTrackedAgentsSet.Clear();
				}
			}
		}
	}

	public MissionAgentLockVisualizerVM()
	{
		_allTrackedAgentsSet = new Dictionary<Agent, MissionAgentLockItemVM>();
		AllTrackedAgents = new MBBindingList<MissionAgentLockItemVM>();
		IsEnabled = true;
	}

	public void OnActiveLockAgentChange(Agent oldAgent, Agent newAgent)
	{
		if (oldAgent != null && _allTrackedAgentsSet.ContainsKey(oldAgent))
		{
			AllTrackedAgents.Remove(_allTrackedAgentsSet[oldAgent]);
			_allTrackedAgentsSet.Remove(oldAgent);
		}
		if (newAgent != null)
		{
			if (_allTrackedAgentsSet.ContainsKey(newAgent))
			{
				_allTrackedAgentsSet[newAgent].SetLockState(MissionAgentLockItemVM.LockStates.Active);
				return;
			}
			MissionAgentLockItemVM missionAgentLockItemVM = new MissionAgentLockItemVM(newAgent, MissionAgentLockItemVM.LockStates.Active);
			_allTrackedAgentsSet.Add(newAgent, missionAgentLockItemVM);
			AllTrackedAgents.Add(missionAgentLockItemVM);
		}
	}

	public void OnPossibleLockAgentChange(Agent oldPossibleAgent, Agent newPossibleAgent)
	{
		if (oldPossibleAgent != null && _allTrackedAgentsSet.ContainsKey(oldPossibleAgent))
		{
			AllTrackedAgents.Remove(_allTrackedAgentsSet[oldPossibleAgent]);
			_allTrackedAgentsSet.Remove(oldPossibleAgent);
		}
		if (newPossibleAgent != null)
		{
			if (_allTrackedAgentsSet.ContainsKey(newPossibleAgent))
			{
				_allTrackedAgentsSet[newPossibleAgent].SetLockState(MissionAgentLockItemVM.LockStates.Possible);
				return;
			}
			MissionAgentLockItemVM missionAgentLockItemVM = new MissionAgentLockItemVM(newPossibleAgent, MissionAgentLockItemVM.LockStates.Possible);
			_allTrackedAgentsSet.Add(newPossibleAgent, missionAgentLockItemVM);
			AllTrackedAgents.Add(missionAgentLockItemVM);
		}
	}
}
