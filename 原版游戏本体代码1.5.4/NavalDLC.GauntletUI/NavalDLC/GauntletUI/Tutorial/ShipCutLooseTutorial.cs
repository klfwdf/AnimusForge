using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipCutLooseTutorial")]
public class ShipCutLooseTutorial : TutorialItemBase
{
	private int _lastControllerHashCode;

	private bool _hasCutLoose;

	public ShipCutLooseTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		PirateBattleMissionController pirateBattleMissionController = Mission.Current?.GetMissionBehavior<PirateBattleMissionController>();
		NavalShipsLogic navalShipsLogic = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
		if (pirateBattleMissionController != null)
		{
			if (_lastControllerHashCode != pirateBattleMissionController.GetHashCode())
			{
				_hasCutLoose = false;
				_lastControllerHashCode = pirateBattleMissionController.GetHashCode();
			}
			if (navalShipsLogic != null)
			{
				MBList<MissionShip> mBList = new MBList<MissionShip>();
				navalShipsLogic.FillTeamShips(TeamSideEnum.PlayerTeam, mBList);
				if (pirateBattleMissionController.HasSelectedShip && mBList.Count == 2)
				{
					MissionShip missionShip = mBList[0];
					MissionShip missionShip2 = mBList[1];
					if (missionShip.IsDisconnectionBlocked())
					{
						missionShip.ResetDisconnectionBlock();
					}
					if (missionShip2.IsDisconnectionBlocked())
					{
						missionShip2.ResetDisconnectionBlock();
					}
					if (Agent.Main?.GetComponent<AgentNavalComponent>().FormationShip != null && !missionShip.GetIsThereActiveBridgeTo(missionShip2) && pirateBattleMissionController.HasSelectedShip)
					{
						_hasCutLoose = true;
					}
				}
			}
		}
		return _hasCutLoose;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Mission.Current == null || !Mission.Current.IsNavalBattle)
		{
			return false;
		}
		PirateBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<PirateBattleMissionController>();
		if (missionBehavior != null && missionBehavior.IsFirstShipCleared && missionBehavior.HasSelectedShip)
		{
			return !_hasCutLoose;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
