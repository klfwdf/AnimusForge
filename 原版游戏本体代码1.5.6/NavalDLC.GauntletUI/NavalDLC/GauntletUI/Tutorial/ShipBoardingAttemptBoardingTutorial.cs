using NavalDLC.Missions;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipBoardingAttemptBoardingTutorial")]
public class ShipBoardingAttemptBoardingTutorial : TutorialItemBase
{
	public ShipBoardingAttemptBoardingTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (Mission.Current?.GetMissionBehavior<PirateBattleMissionController>() != null)
		{
			return (Agent.Main?.GetComponent<AgentNavalComponent>().FormationShip)?.GetIsAnyBridgeActive() ?? false;
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
