using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipSailTutorial")]
public class ShipSailTutorial : TutorialItemBase
{
	public ShipSailTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "SailToggle";
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		MissionShip missionShip = (Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>())?.MissionShip;
		if (missionShip != null)
		{
			if (missionShip.IsPlayerControlled)
			{
				return missionShip.SailTargetSetting > 0.5f;
			}
			return false;
		}
		return false;
	}

	public override bool IsConditionsMetForActivation()
	{
		return (Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>())?.IsFirstHighlightCleared() ?? false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
