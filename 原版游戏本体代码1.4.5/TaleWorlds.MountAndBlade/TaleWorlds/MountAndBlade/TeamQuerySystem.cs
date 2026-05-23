using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Handlers;

namespace TaleWorlds.MountAndBlade;

public class TeamQuerySystem
{
	public readonly Team Team;

	private readonly Mission _mission;

	private readonly QueryData<int> _memberCount;

	private readonly QueryData<WorldPosition> _medianPosition;

	private readonly QueryData<Vec2> _averagePosition;

	private readonly QueryData<Vec2> _averageEnemyPosition;

	private readonly QueryData<FormationQuerySystem> _medianTargetFormation;

	private readonly QueryData<WorldPosition> _medianTargetFormationPosition;

	private readonly QueryData<WorldPosition> _leftFlankEdgePosition;

	private readonly QueryData<WorldPosition> _rightFlankEdgePosition;

	private readonly QueryData<float> _infantryRatio;

	private readonly QueryData<float> _rangedRatio;

	private readonly QueryData<float> _cavalryRatio;

	private readonly QueryData<float> _rangedCavalryRatio;

	private readonly QueryData<int> _allyMemberCount;

	private readonly QueryData<int> _enemyMemberCount;

	private readonly QueryData<float> _allyInfantryRatio;

	private readonly QueryData<float> _allyRangedRatio;

	private readonly QueryData<float> _allyCavalryRatio;

	private readonly QueryData<float> _allyRangedCavalryRatio;

	private readonly QueryData<float> _enemyInfantryRatio;

	private readonly QueryData<float> _enemyRangedRatio;

	private readonly QueryData<float> _enemyCavalryRatio;

	private readonly QueryData<float> _enemyRangedCavalryRatio;

	private readonly QueryData<float> _remainingPowerRatio;

	private readonly QueryData<float> _teamPower;

	private readonly QueryData<float> _totalPowerRatio;

	private readonly QueryData<float> _insideWallsRatio;

	private IBattlePowerCalculationLogic _battlePowerLogic;

	private CasualtyHandler _casualtyHandler;

	private readonly QueryData<float> _maxUnderRangedAttackRatio;

	public int MemberCount => _memberCount.Value;

	public WorldPosition MedianPosition => _medianPosition.Value;

	public Vec2 AveragePosition => _averagePosition.Value;

	public Vec2 AverageEnemyPosition => _averageEnemyPosition.Value;

	public FormationQuerySystem MedianTargetFormation => _medianTargetFormation.Value;

	public WorldPosition MedianTargetFormationPosition => _medianTargetFormationPosition.Value;

	public WorldPosition LeftFlankEdgePosition => _leftFlankEdgePosition.Value;

	public WorldPosition RightFlankEdgePosition => _rightFlankEdgePosition.Value;

	public float InfantryRatio => _infantryRatio.Value;

	public float RangedRatio => _rangedRatio.Value;

	public float CavalryRatio => _cavalryRatio.Value;

	public float RangedCavalryRatio => _rangedCavalryRatio.Value;

	public int AllyUnitCount => _allyMemberCount.Value;

	public int EnemyUnitCount => _enemyMemberCount.Value;

	public float AllyInfantryRatio => _allyInfantryRatio.Value;

	public float AllyRangedRatio => _allyRangedRatio.Value;

	public float AllyCavalryRatio => _allyCavalryRatio.Value;

	public float AllyRangedCavalryRatio => _allyRangedCavalryRatio.Value;

	public float EnemyInfantryRatio => _enemyInfantryRatio.Value;

	public float EnemyRangedRatio => _enemyRangedRatio.Value;

	public float EnemyCavalryRatio => _enemyCavalryRatio.Value;

	public float EnemyRangedCavalryRatio => _enemyRangedCavalryRatio.Value;

	public float RemainingPowerRatio => _remainingPowerRatio.Value;

	public float TeamPower => _teamPower.Value;

	public float TotalPowerRatio => _totalPowerRatio.Value;

	public float InsideWallsRatio => _insideWallsRatio.Value;

	public IBattlePowerCalculationLogic BattlePowerLogic
	{
		get
		{
			if (_battlePowerLogic == null)
			{
				_battlePowerLogic = _mission.GetMissionBehavior<IBattlePowerCalculationLogic>();
			}
			return _battlePowerLogic;
		}
	}

	public CasualtyHandler CasualtyHandler
	{
		get
		{
			if (_casualtyHandler == null)
			{
				_casualtyHandler = _mission.GetMissionBehavior<CasualtyHandler>();
			}
			return _casualtyHandler;
		}
	}

	public float MaxUnderRangedAttackRatio => _maxUnderRangedAttackRatio.Value;

	public int DeathCount { get; private set; }

	public int DeathByRangedCount { get; private set; }

	public int AllyRangedUnitCount => (int)(AllyRangedRatio * (float)AllyUnitCount);

	public int AllCavalryUnitCount => (int)(AllyCavalryRatio * (float)AllyUnitCount);

	public int EnemyRangedUnitCount => (int)(EnemyRangedRatio * (float)EnemyUnitCount);

	public void Expire()
	{
		_memberCount.Expire();
		_medianPosition.Expire();
		_averagePosition.Expire();
		_averageEnemyPosition.Expire();
		_medianTargetFormationPosition.Expire();
		_leftFlankEdgePosition.Expire();
		_rightFlankEdgePosition.Expire();
		_infantryRatio.Expire();
		_rangedRatio.Expire();
		_cavalryRatio.Expire();
		_rangedCavalryRatio.Expire();
		_allyMemberCount.Expire();
		_enemyMemberCount.Expire();
		_allyInfantryRatio.Expire();
		_allyRangedRatio.Expire();
		_allyCavalryRatio.Expire();
		_allyRangedCavalryRatio.Expire();
		_enemyInfantryRatio.Expire();
		_enemyRangedRatio.Expire();
		_enemyCavalryRatio.Expire();
		_enemyRangedCavalryRatio.Expire();
		_remainingPowerRatio.Expire();
		_teamPower.Expire();
		_totalPowerRatio.Expire();
		_insideWallsRatio.Expire();
		_maxUnderRangedAttackRatio.Expire();
		foreach (Formation item in Team.FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.QuerySystem.Expire();
			}
		}
	}

	public void ExpireAfterUnitAddRemove()
	{
		_memberCount.Expire();
		_medianPosition.Expire();
		_averagePosition.Expire();
		_leftFlankEdgePosition.Expire();
		_rightFlankEdgePosition.Expire();
		_infantryRatio.Expire();
		_rangedRatio.Expire();
		_cavalryRatio.Expire();
		_rangedCavalryRatio.Expire();
		_allyMemberCount.Expire();
		_allyInfantryRatio.Expire();
		_allyRangedRatio.Expire();
		_allyCavalryRatio.Expire();
		_allyRangedCavalryRatio.Expire();
		_remainingPowerRatio.Expire();
		_teamPower.Expire();
		_totalPowerRatio.Expire();
		_insideWallsRatio.Expire();
		_maxUnderRangedAttackRatio.Expire();
	}

	private void InitializeTelemetryScopeNames()
	{
	}

	public TeamQuerySystem(Team team)
	{
		TeamQuerySystem teamQuerySystem = this;
		Team = team;
		_mission = Mission.Current;
		_memberCount = new QueryData<int>(delegate
		{
			int num = 0;
			foreach (Formation item in teamQuerySystem.Team.FormationsIncludingSpecialAndEmpty)
			{
				num += item.CountOfUnits;
			}
			return num;
		}, 2f);
		_allyMemberCount = new QueryData<int>(delegate
		{
			int num = 0;
			foreach (Team team2 in teamQuerySystem._mission.Teams)
			{
				if (team2.IsFriendOf(teamQuerySystem.Team))
				{
					num += team2.QuerySystem.MemberCount;
				}
			}
			return num;
		}, 2f);
		_enemyMemberCount = new QueryData<int>(delegate
		{
			int num = 0;
			foreach (Team team3 in teamQuerySystem._mission.Teams)
			{
				if (team3.IsEnemyOf(teamQuerySystem.Team))
				{
					num += team3.QuerySystem.MemberCount;
				}
			}
			return num;
		}, 2f);
		_averagePosition = new QueryData<Vec2>(team.GetAveragePosition, 5f);
		_medianPosition = new QueryData<WorldPosition>(() => team.GetMedianPosition(teamQuerySystem.AveragePosition), 5f);
		_averageEnemyPosition = new QueryData<Vec2>(delegate
		{
			Vec2 averagePositionOfEnemies = team.GetAveragePositionOfEnemies();
			if (averagePositionOfEnemies.IsValid)
			{
				return averagePositionOfEnemies;
			}
			if (team.Side == BattleSideEnum.Attacker)
			{
				SiegeDeploymentHandler missionBehavior = teamQuerySystem._mission.GetMissionBehavior<SiegeDeploymentHandler>();
				if (missionBehavior != null)
				{
					return missionBehavior.GetEstimatedAverageDefenderPosition();
				}
			}
			return (!teamQuerySystem.AveragePosition.IsValid) ? team.GetAveragePosition() : teamQuerySystem.AveragePosition;
		}, 5f);
		_medianTargetFormation = new QueryData<FormationQuerySystem>(delegate
		{
			float num = float.MaxValue;
			Formation formation = null;
			foreach (Team team4 in teamQuerySystem._mission.Teams)
			{
				if (team4.IsEnemyOf(teamQuerySystem.Team))
				{
					foreach (Formation item2 in team4.FormationsIncludingSpecialAndEmpty)
					{
						if (item2.CountOfUnits > 0)
						{
							float num2 = item2.CachedMedianPosition.AsVec2.DistanceSquared(teamQuerySystem.AverageEnemyPosition);
							if (num > num2)
							{
								num = num2;
								formation = item2;
							}
						}
					}
				}
			}
			return formation?.QuerySystem;
		}, 1f);
		_medianTargetFormationPosition = new QueryData<WorldPosition>(() => (teamQuerySystem.MedianTargetFormation != null) ? teamQuerySystem.MedianTargetFormation.Formation.CachedMedianPosition : teamQuerySystem.MedianPosition, 1f);
		QueryData<WorldPosition>.SetupSyncGroup(_averageEnemyPosition, _medianTargetFormationPosition);
		_leftFlankEdgePosition = new QueryData<WorldPosition>(delegate
		{
			Vec2 vec = (teamQuerySystem.MedianTargetFormationPosition.AsVec2 - teamQuerySystem.AveragePosition).RightVec();
			vec.Normalize();
			WorldPosition medianTargetFormationPosition = teamQuerySystem.MedianTargetFormationPosition;
			medianTargetFormationPosition.SetVec2(medianTargetFormationPosition.AsVec2 - vec * 50f);
			return medianTargetFormationPosition;
		}, 5f);
		_rightFlankEdgePosition = new QueryData<WorldPosition>(delegate
		{
			Vec2 vec = (teamQuerySystem.MedianTargetFormationPosition.AsVec2 - teamQuerySystem.AveragePosition).RightVec();
			vec.Normalize();
			WorldPosition medianTargetFormationPosition = teamQuerySystem.MedianTargetFormationPosition;
			medianTargetFormationPosition.SetVec2(medianTargetFormationPosition.AsVec2 + vec * 50f);
			return medianTargetFormationPosition;
		}, 5f);
		_infantryRatio = new QueryData<float>(() => (teamQuerySystem.MemberCount != 0) ? ((teamQuerySystem.Team.FormationsIncludingSpecialAndEmpty.Sum((Formation f) => (f.CountOfUnits <= 0) ? 0f : (f.QuerySystem.InfantryUnitRatio * (float)f.CountOfUnits)) + (float)team.Heroes.Count((Agent h) => QueryLibrary.IsInfantry(h))) / (float)teamQuerySystem.MemberCount) : 0f, 15f);
		_rangedRatio = new QueryData<float>(() => (teamQuerySystem.MemberCount != 0) ? ((teamQuerySystem.Team.FormationsIncludingSpecialAndEmpty.Sum((Formation f) => (f.CountOfUnits <= 0) ? 0f : (f.QuerySystem.RangedUnitRatio * (float)f.CountOfUnits)) + (float)team.Heroes.Count((Agent h) => QueryLibrary.IsRanged(h))) / (float)teamQuerySystem.MemberCount) : 0f, 15f);
		_cavalryRatio = new QueryData<float>(() => (teamQuerySystem.MemberCount != 0) ? ((teamQuerySystem.Team.FormationsIncludingSpecialAndEmpty.Sum((Formation f) => (f.CountOfUnits <= 0) ? 0f : (f.QuerySystem.CavalryUnitRatio * (float)f.CountOfUnits)) + (float)team.Heroes.Count((Agent h) => QueryLibrary.IsCavalry(h))) / (float)teamQuerySystem.MemberCount) : 0f, 15f);
		_rangedCavalryRatio = new QueryData<float>(() => (teamQuerySystem.MemberCount != 0) ? ((teamQuerySystem.Team.FormationsIncludingSpecialAndEmpty.Sum((Formation f) => (f.CountOfUnits <= 0) ? 0f : (f.QuerySystem.RangedCavalryUnitRatio * (float)f.CountOfUnits)) + (float)team.Heroes.Count((Agent h) => QueryLibrary.IsRangedCavalry(h))) / (float)teamQuerySystem.MemberCount) : 0f, 15f);
		QueryData<float>.SetupSyncGroup(_infantryRatio, _rangedRatio, _cavalryRatio, _rangedCavalryRatio);
		_allyInfantryRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team5 in teamQuerySystem._mission.Teams)
			{
				if (team5.IsFriendOf(teamQuerySystem.Team))
				{
					int memberCount = team5.QuerySystem.MemberCount;
					num += team5.QuerySystem.InfantryRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_allyRangedRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team6 in teamQuerySystem._mission.Teams)
			{
				if (team6.IsFriendOf(teamQuerySystem.Team))
				{
					int memberCount = team6.QuerySystem.MemberCount;
					num += team6.QuerySystem.RangedRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_allyCavalryRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team7 in teamQuerySystem._mission.Teams)
			{
				if (team7.IsFriendOf(teamQuerySystem.Team))
				{
					int memberCount = team7.QuerySystem.MemberCount;
					num += team7.QuerySystem.CavalryRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_allyRangedCavalryRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team8 in teamQuerySystem._mission.Teams)
			{
				if (team8.IsFriendOf(teamQuerySystem.Team))
				{
					int memberCount = team8.QuerySystem.MemberCount;
					num += team8.QuerySystem.RangedCavalryRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		QueryData<float>.SetupSyncGroup(_allyInfantryRatio, _allyRangedRatio, _allyCavalryRatio, _allyRangedCavalryRatio);
		_enemyInfantryRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team9 in teamQuerySystem._mission.Teams)
			{
				if (team9.IsEnemyOf(teamQuerySystem.Team))
				{
					int memberCount = team9.QuerySystem.MemberCount;
					num += team9.QuerySystem.InfantryRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_enemyRangedRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team10 in teamQuerySystem._mission.Teams)
			{
				if (team10.IsEnemyOf(teamQuerySystem.Team))
				{
					int memberCount = team10.QuerySystem.MemberCount;
					num += team10.QuerySystem.RangedRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_enemyCavalryRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team11 in teamQuerySystem._mission.Teams)
			{
				if (team11.IsEnemyOf(teamQuerySystem.Team))
				{
					int memberCount = team11.QuerySystem.MemberCount;
					num += team11.QuerySystem.CavalryRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_enemyRangedCavalryRatio = new QueryData<float>(delegate
		{
			float num = 0f;
			int num2 = 0;
			foreach (Team team12 in teamQuerySystem._mission.Teams)
			{
				if (team12.IsEnemyOf(teamQuerySystem.Team))
				{
					int memberCount = team12.QuerySystem.MemberCount;
					num += team12.QuerySystem.RangedCavalryRatio * (float)memberCount;
					num2 += memberCount;
				}
			}
			return (num2 != 0) ? (num / (float)num2) : 0f;
		}, 15f);
		_teamPower = new QueryData<float>(() => team.FormationsIncludingSpecialAndEmpty.Sum((Formation f) => (f.CountOfUnits <= 0) ? 0f : f.GetFormationPower()), 5f);
		_remainingPowerRatio = new QueryData<float>(delegate
		{
			IBattlePowerCalculationLogic battlePowerLogic = teamQuerySystem.BattlePowerLogic;
			CasualtyHandler casualtyHandler = teamQuerySystem.CasualtyHandler;
			float num = 0f;
			float num2 = 0f;
			foreach (Team team13 in teamQuerySystem.Team.Mission.Teams)
			{
				if (team13.IsEnemyOf(teamQuerySystem.Team))
				{
					num2 += battlePowerLogic.GetTotalTeamPower(team13);
					foreach (Formation item3 in team13.FormationsIncludingSpecialAndEmpty)
					{
						num2 -= casualtyHandler.GetCasualtyPowerLossOfFormation(item3);
					}
				}
				else
				{
					num += battlePowerLogic.GetTotalTeamPower(team13);
					foreach (Formation item4 in team13.FormationsIncludingSpecialAndEmpty)
					{
						num -= casualtyHandler.GetCasualtyPowerLossOfFormation(item4);
					}
				}
			}
			num = TaleWorlds.Library.MathF.Max(0f, num);
			num2 = TaleWorlds.Library.MathF.Max(0f, num2);
			return (num + 1f) / (num2 + 1f);
		}, 5f);
		_totalPowerRatio = new QueryData<float>(delegate
		{
			IBattlePowerCalculationLogic battlePowerLogic = teamQuerySystem.BattlePowerLogic;
			float num = 0f;
			float num2 = 0f;
			foreach (Team team14 in teamQuerySystem.Team.Mission.Teams)
			{
				if (team14.IsEnemyOf(teamQuerySystem.Team))
				{
					num2 += battlePowerLogic.GetTotalTeamPower(team14);
				}
				else
				{
					num += battlePowerLogic.GetTotalTeamPower(team14);
				}
			}
			return (num + 1f) / (num2 + 1f);
		}, 10f);
		_insideWallsRatio = new QueryData<float>(delegate
		{
			if (!(team.TeamAI is TeamAISiegeComponent))
			{
				return 1f;
			}
			if (teamQuerySystem.AllyUnitCount == 0)
			{
				return 0f;
			}
			int num = 0;
			foreach (Team team15 in Mission.Current.Teams)
			{
				if (team15.IsFriendOf(team))
				{
					foreach (Formation item5 in team15.FormationsIncludingSpecialAndEmpty)
					{
						if (item5.CountOfUnits > 0)
						{
							num += item5.CountUnitsOnNavMeshIDMod10(1, includeOnlyPositionedUnits: false);
						}
					}
				}
			}
			return (float)num / (float)teamQuerySystem.AllyUnitCount;
		}, 10f);
		_maxUnderRangedAttackRatio = new QueryData<float>(delegate
		{
			float num;
			if (teamQuerySystem.AllyUnitCount == 0)
			{
				num = 0f;
			}
			else
			{
				float currentTime = MBCommon.GetTotalMissionTime();
				int num2 = 0;
				foreach (Team team16 in teamQuerySystem._mission.Teams)
				{
					if (team16.IsFriendOf(teamQuerySystem.Team))
					{
						for (int i = 0; i < Math.Min(team16.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
						{
							Formation formation = team16.FormationsIncludingSpecialAndEmpty[i];
							if (formation.CountOfUnits > 0)
							{
								num2 += formation.GetCountOfUnitsWithCondition((Agent agent) => currentTime - agent.LastRangedHitTime < 10f && !agent.Equipment.HasShield());
							}
						}
					}
				}
				num = (float)num2 / (float)teamQuerySystem.AllyUnitCount;
			}
			return (!(num > teamQuerySystem._maxUnderRangedAttackRatio.GetCachedValue())) ? teamQuerySystem._maxUnderRangedAttackRatio.GetCachedValue() : num;
		}, 3f);
		DeathCount = 0;
		DeathByRangedCount = 0;
		InitializeTelemetryScopeNames();
	}

	public void RegisterDeath()
	{
		DeathCount++;
	}

	public void RegisterDeathByRanged()
	{
		DeathByRangedCount++;
	}

	public float GetLocalAllyPower(Vec2 target)
	{
		return Team.FormationsIncludingSpecialAndEmpty.Sum((Formation f) => (f.CountOfUnits <= 0) ? 0f : (f.QuerySystem.FormationPower / f.CachedAveragePosition.Distance(target)));
	}

	public float GetLocalEnemyPower(Vec2 target)
	{
		float num = 0f;
		foreach (Team team in Mission.Current.Teams)
		{
			if (!Team.IsEnemyOf(team))
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					num += item.QuerySystem.FormationPower / item.CachedAveragePosition.Distance(target);
				}
			}
		}
		return num;
	}
}
