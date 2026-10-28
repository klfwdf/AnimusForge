using System.Collections.Generic;
using System.Linq;
using SandBox.Objects;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets;
using Storymode.Missions;
using TaleWorlds.MountAndBlade;

namespace StoryMode.View.MarkerProviders;

public class StealthTutorialMarkerProvider : MissionNameMarkerProvider
{
	private SneakIntoTheVillaMissionController _controller;

	private SneakIntoTheVillaMissionController Controller
	{
		get
		{
			if (_controller == null)
			{
				_controller = Mission.Current?.GetMissionBehavior<SneakIntoTheVillaMissionController>();
			}
			return _controller;
		}
	}

	public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
	{
		foreach (PassageUsePoint item in Mission.Current.ActiveMissionObjects.FindAllWithType<PassageUsePoint>().ToList())
		{
			if (item.IsMissionExit && !item.IsDeactivated)
			{
				markers.Add(new MissionPassageUsePointNameMarkerTargetVM(item));
			}
		}
		if (Controller != null)
		{
			markers.Add(new MissionAgentMarkerTargetVM(Controller.HeadmanAgent));
		}
	}

	protected override void OnTick(float dt)
	{
		if (Controller != null && Controller.AreVisualsDirty)
		{
			SetMarkersDirty();
			Controller.AreVisualsDirty = false;
		}
	}
}
