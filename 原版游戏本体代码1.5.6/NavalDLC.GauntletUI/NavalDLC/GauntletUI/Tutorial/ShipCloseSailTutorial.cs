using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipCloseSailTutorial")]
public class ShipCloseSailTutorial : TutorialItemBase
{
	public ShipCloseSailTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "SailToggle";
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		NavalStorylineCaptivityMissionController navalStorylineCaptivityMissionController = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		MissionShip missionShip = navalStorylineCaptivityMissionController?.MissionShip;
		if (missionShip != null)
		{
			if (navalStorylineCaptivityMissionController.IsReadyToCloseSails() && missionShip.IsPlayerControlled && missionShip.SailTargetSetting < 0.5f)
			{
				return missionShip.Physics.LinearVelocity.Length <= navalStorylineCaptivityMissionController.GetStoppedShipSpeedThreshold();
			}
			return false;
		}
		return false;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Mission.Current == null || !Mission.Current.IsNavalBattle)
		{
			return false;
		}
		return Mission.Current.GetMissionBehavior<NavalStorylineCaptivityMissionController>()?.IsReadyToCloseSails() ?? false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
