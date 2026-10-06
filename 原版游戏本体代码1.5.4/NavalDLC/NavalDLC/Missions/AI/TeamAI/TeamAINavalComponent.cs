using NavalDLC.Missions.AI.Behaviors;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.TeamAI;

public class TeamAINavalComponent : TeamAIComponent
{
	private readonly bool _isRiverBattle;

	private NavalShipsLogic _navalShipsLogic;

	private SpawnPathData _spawnPathData;

	public NavalQuerySystem TeamNavalQuerySystem { get; protected set; }

	public bool UseSpawnPathApproachPosition
	{
		get
		{
			if (_isRiverBattle && _spawnPathData != null)
			{
				return _spawnPathData.IsValid;
			}
			return false;
		}
	}

	public TeamAINavalComponent(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		TeamNavalQuerySystem = new NavalQuerySystem(currentTeam);
		NavalOrderController customMasterOrderController = new NavalOrderController(Mission, Team, null);
		NavalOrderController customPlayerOrderController = new NavalOrderController(Mission, Team, (Team.IsPlayerTeam && Team.IsPlayerGeneral) ? Mission.Current.MainAgent : null);
		Team.SetCustomOrderController(customMasterOrderController, customPlayerOrderController);
		Team.DisableDetachmentTicking();
		_isRiverBattle = Mission.Current.Scene.GetNavmeshFaceCountBetweenTwoIds(1, 1) > 0;
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
		if (formation.AI.GetBehavior<BehaviorNavalRemoveConnection>() == null)
		{
			formation.ForceCalculateCaches();
			formation.AI.AddAiBehavior(new BehaviorNavalRemoveConnection(formation));
			formation.AI.AddAiBehavior(new BehaviorNavalEngageCorrespondingEnemy(formation));
			formation.AI.AddAiBehavior(new BehaviorNavalDefendInLine(formation));
			formation.AI.AddAiBehavior(new BehaviorNavalSkirmish(formation));
			formation.AI.AddAiBehavior(new BehaviorNavalRamming(formation));
			formation.AI.AddAiBehavior(new BehaviorNavalApproachInLine(formation));
		}
	}

	public override void OnDeploymentFinished()
	{
		foreach (Formation item in Team.FormationsIncludingEmpty)
		{
			item.OnDeploymentFinished();
		}
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (Mission.Current.IsBattleSpawnPathSelectorInitialized)
		{
			_spawnPathData = Mission.Current.GetInitialSpawnPathData(Team.Side);
		}
	}

	public Formation GetConnectedAllyFormation(ulong shipUniqueBitwiseID)
	{
		return _navalShipsLogic.GetConnectedTeamShip(Team.TeamSide, shipUniqueBitwiseID)?.Formation;
	}

	public Formation GetNearestAllyShipFormation(Agent agent)
	{
		Vec3 position = agent.Frame.origin;
		return _navalShipsLogic.GetNearestTeamShip(Team.TeamSide, in position, float.MaxValue, (MissionShip ship) => ship.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating && !ship.BeingAbandoned)?.Formation;
	}

	public void GetRiverApproachPosition(out Vec2 position, out Vec2 direction)
	{
		_spawnPathData.GetSpawnPathFrameFacingTarget(0f, 1f, useTangentDirection: false, out position, out direction);
	}
}
