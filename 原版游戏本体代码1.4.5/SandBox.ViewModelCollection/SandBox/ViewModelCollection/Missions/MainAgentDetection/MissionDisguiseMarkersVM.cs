using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection;

public class MissionDisguiseMarkersVM : ViewModel
{
	private MissionDisguiseMarkerItemVM _targetAgent;

	private MBBindingList<MissionDisguiseMarkerItemVM> _hostileAgents;

	[DataSourceProperty]
	public MissionDisguiseMarkerItemVM TargetAgent
	{
		get
		{
			return _targetAgent;
		}
		set
		{
			if (value != _targetAgent)
			{
				_targetAgent = value;
				OnPropertyChangedWithValue(value, "TargetAgent");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionDisguiseMarkerItemVM> HostileAgents
	{
		get
		{
			return _hostileAgents;
		}
		set
		{
			if (value != _hostileAgents)
			{
				_hostileAgents = value;
				OnPropertyChangedWithValue(value, "HostileAgents");
			}
		}
	}

	public MissionDisguiseMarkersVM()
	{
		HostileAgents = new MBBindingList<MissionDisguiseMarkerItemVM>();
	}
}
