using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipControlTutorial")]
public class ShipControlTutorial : TutorialItemBase
{
	public ShipControlTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Left;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		NavalStorylineCaptivityMissionController navalStorylineCaptivityMissionController = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		if (navalStorylineCaptivityMissionController != null)
		{
			MissionShip missionShip = navalStorylineCaptivityMissionController.MissionShip;
			if (missionShip != null)
			{
				return missionShip.IsPlayerControlled;
			}
		}
		return false;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Mission.Current == null || !Mission.Current.IsNavalBattle)
		{
			return false;
		}
		NavalStorylineCaptivityMissionController missionBehavior = Mission.Current.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		if (missionBehavior != null && missionBehavior.HasTalkedToGunnar)
		{
			return Mission.Current.Mode != MissionMode.Conversation;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
