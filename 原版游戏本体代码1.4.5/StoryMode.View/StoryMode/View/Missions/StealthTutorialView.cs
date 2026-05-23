using SandBox.ViewModelCollection.Missions.NameMarker;
using StoryMode.View.MarkerProviders;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace StoryMode.View.Missions;

public class StealthTutorialView : MissionView
{
	public override void AfterStart()
	{
		MissionNameMarkerFactory.PushContext("StealthTutorialContext", addDefaultProviders: false).AddProvider<StealthTutorialMarkerProvider>();
	}

	protected override void OnEndMission()
	{
		MissionNameMarkerFactory.PopContext("StealthTutorialContext");
	}
}
