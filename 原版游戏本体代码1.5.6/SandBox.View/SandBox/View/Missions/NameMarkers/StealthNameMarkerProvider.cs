using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Missions.NameMarkers;

public class StealthNameMarkerProvider : MissionNameMarkerProvider
{
	private StealthAreaMissionLogic _stealthAreaMissionLogic;

	protected override void OnInitialize(Mission mission)
	{
		base.OnInitialize(mission);
		_stealthAreaMissionLogic = mission.GetMissionBehavior<StealthAreaMissionLogic>();
	}

	protected override void OnDestroy(Mission mission)
	{
		base.OnDestroy(mission);
		_stealthAreaMissionLogic = null;
	}

	public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
	{
		CreateStealthAreaMarkers(markers);
	}

	private void CreateStealthAreaMarkers(List<MissionNameMarkerTargetBaseVM> markers)
	{
		if (_stealthAreaMissionLogic == null || Mission.Current == null || Agent.Main == null)
		{
			return;
		}
		foreach (StealthAreaUsePoint item2 in Mission.Current.ActiveMissionObjects.FindAllWithType<StealthAreaUsePoint>())
		{
			if (item2.IsUsableByAgent(Agent.Main))
			{
				MissionStealthAreaUsePointNameMarkerTargetVM item = new MissionStealthAreaUsePointNameMarkerTargetVM(item2);
				markers.Add(item);
			}
		}
	}
}
