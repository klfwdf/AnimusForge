using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.TeamAI;

public class TeamAINavalRaidAttackerComponent : TeamAIComponent
{
	private readonly bool _isRiverBattle;

	private NavalShipsLogic _navalShipsLogic;

	private SpawnPathData _spawnPathData;

	public NavalQuerySystem TeamNavalQuerySystem { get; protected set; }

	public bool UseSpawnPathApproachPosition
	{
		get
		{
			if (_isRiverBattle)
			{
				return _spawnPathData.IsValid;
			}
			return false;
		}
	}

	public TeamAINavalRaidAttackerComponent(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		TeamNavalQuerySystem = new NavalQuerySystem(currentTeam);
		Team.DisableDetachmentTicking();
		_isRiverBattle = Mission.Current.Scene.GetNavmeshFaceCountBetweenTwoIds(1, 1) > 0;
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
		if (GameNetwork.IsServer)
		{
			formation.ForceCalculateCaches();
			if (formation.AI.GetBehavior<BehaviorCharge>() == null)
			{
				if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
				{
					formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
				}
				else if (formation.FormationIndex == FormationClass.Bodyguard)
				{
					formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
				}
				formation.AI.AddAiBehavior(new BehaviorCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
				formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
				formation.AI.AddAiBehavior(new BehaviorReserve(formation));
				formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
				formation.AI.AddAiBehavior(new BehaviorStop(formation));
				formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPInfantry(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPLastFlagLastStand(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPMounted(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPMountedRanged(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPRanged(formation));
			}
		}
		else
		{
			if (GameNetwork.IsClientOrReplay)
			{
				return;
			}
			formation.ForceCalculateCaches();
			if (formation.AI.GetBehavior<BehaviorCharge>() == null)
			{
				if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
				{
					formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
				}
				else if (formation.FormationIndex == FormationClass.Bodyguard)
				{
					formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
				}
				formation.AI.AddAiBehavior(new BehaviorCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
				formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
				formation.AI.AddAiBehavior(new BehaviorReserve(formation));
				formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
				formation.AI.AddAiBehavior(new BehaviorStop(formation));
				formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorAdvance(formation));
				formation.AI.AddAiBehavior(new BehaviorCautiousAdvance(formation));
				formation.AI.AddAiBehavior(new BehaviorCavalryScreen(formation));
				formation.AI.AddAiBehavior(new BehaviorDefend(formation));
				formation.AI.AddAiBehavior(new BehaviorDefensiveRing(formation));
				formation.AI.AddAiBehavior(new BehaviorFireFromInfantryCover(formation));
				formation.AI.AddAiBehavior(new BehaviorFlank(formation));
				formation.AI.AddAiBehavior(new BehaviorHoldHighGround(formation));
				formation.AI.AddAiBehavior(new BehaviorHorseArcherSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorMountedSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorProtectFlank(formation));
				formation.AI.AddAiBehavior(new BehaviorScreenedSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmishBehindFormation(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmishLine(formation));
				formation.AI.AddAiBehavior(new BehaviorVanguard(formation));
				formation.AI.AddAiBehavior(new BehaviorShootFromCliff(formation));
			}
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
}
