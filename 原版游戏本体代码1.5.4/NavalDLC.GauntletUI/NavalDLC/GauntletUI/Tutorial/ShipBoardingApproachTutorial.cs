using System.Linq;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipBoardingApproachTutorial")]
public class ShipBoardingApproachTutorial : TutorialItemBase
{
	public ShipBoardingApproachTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (Mission.Current?.GetMissionBehavior<PirateBattleMissionController>() != null)
		{
			NavalShipsLogic obj = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
			MissionShip missionShip = Agent.Main?.GetComponent<AgentNavalComponent>().FormationShip;
			MissionShip missionShip2 = obj?.AllShips.FirstOrDefault((MissionShip x) => !x.IsPlayerShip);
			if (missionShip2 != null && missionShip != null && missionShip.GameEntity.GlobalPosition.DistanceSquared(missionShip2.GameEntity.GlobalPosition) <= 2500f)
			{
				return true;
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
		PirateBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<PirateBattleMissionController>();
		if (missionBehavior != null)
		{
			return !missionBehavior.IsFirstShipCleared;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
