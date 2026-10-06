using System;
using StoryMode.View.Missions;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace StoryMode.View;

public static class StoryModeViewCreator
{
	public static MissionView CreateTrainingFieldObjectiveView(Mission mission)
	{
		return ViewCreatorManager.CreateMissionView<MissionTrainingFieldObjectiveView>(isNetwork: true, mission, Array.Empty<object>());
	}
}
