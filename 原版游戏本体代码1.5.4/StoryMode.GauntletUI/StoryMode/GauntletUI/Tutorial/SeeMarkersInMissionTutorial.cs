using System.Linq;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("SeeMarkersInMissionTutorial")]
public class SeeMarkersInMissionTutorial : TutorialItemBase
{
	private bool _playerEnabledNameMarkers;

	public SeeMarkersInMissionTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Left;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerEnabledNameMarkers;
	}

	public override void OnMissionNameMarkerToggled(MissionNameMarkerToggleEvent obj)
	{
		_playerEnabledNameMarkers = obj.NewState;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		string[] source = new string[5] { "center", "lordshall", "tavern", "prison", "village_center" };
		if (TutorialHelper.PlayerIsInAnySettlement && TutorialHelper.CurrentContext == TutorialContexts.Mission && TutorialHelper.CurrentMissionLocation != null && source.Contains(TutorialHelper.CurrentMissionLocation.StringId))
		{
			return !TutorialHelper.PlayerIsInAConversation;
		}
		return false;
	}
}
