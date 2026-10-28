using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using NavalDLC.View.MissionViews;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipCameraTutorial")]
public class ShipCameraTutorial : TutorialItemBase
{
	public ShipCameraTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "CameraToggle";
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		MissionShipControlView missionShipControlView = Mission.Current?.GetMissionBehavior<MissionShipControlView>();
		NavalStorylineCaptivityMissionController navalStorylineCaptivityMissionController = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		if (navalStorylineCaptivityMissionController != null && navalStorylineCaptivityMissionController.IsPlayerInShipControls())
		{
			if (missionShipControlView == null)
			{
				return false;
			}
			return missionShipControlView.ActiveCameraMode == MissionShipControlView.CameraModes.Back;
		}
		return false;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Mission.Current == null || !Mission.Current.IsNavalBattle)
		{
			return false;
		}
		NavalStorylineCaptivityMissionController navalStorylineCaptivityMissionController = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		MissionShip missionShip = navalStorylineCaptivityMissionController?.MissionShip;
		if (missionShip != null)
		{
			if (navalStorylineCaptivityMissionController.HasTalkedToGunnar)
			{
				return missionShip.ShipOrder.OarsmenLevel == 2;
			}
			return false;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
