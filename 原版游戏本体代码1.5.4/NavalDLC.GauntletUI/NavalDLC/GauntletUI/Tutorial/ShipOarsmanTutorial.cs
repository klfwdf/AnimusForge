using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipOarsmanTutorial")]
public class ShipOarsmanTutorial : TutorialItemBase
{
	public ShipOarsmanTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "OarsmenToggle";
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		MissionShip missionShip = (Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>())?.MissionShip;
		if (missionShip != null)
		{
			if (missionShip.IsPlayerControlled)
			{
				return missionShip.ShipOrder.OarsmenLevel == 2;
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
		NavalStorylineCaptivityMissionController obj = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		MissionShip missionShip = (Mission.Current?.GetMissionBehavior<NavalShipsLogic>())?.AllShips.FirstOrDefault();
		if (obj != null && missionShip != null)
		{
			return missionShip.IsPlayerControlled;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
