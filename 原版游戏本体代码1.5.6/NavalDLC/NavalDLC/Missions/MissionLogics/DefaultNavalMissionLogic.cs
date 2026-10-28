using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class DefaultNavalMissionLogic : MissionLogic, IAgentStateDecider, IMissionBehavior
{
	private const float InterTeamDeploymentGap = 32f;

	private NavalShipsLogic _shipsLogic;

	private NavalMissionDeploymentPlanningLogic _deploymentPlan;

	private readonly MBList<IShipOrigin> _playerTeamShips;

	private readonly MBList<IShipOrigin> _playerAllyTeamShips;

	private readonly MBList<IShipOrigin> _enemyTeamShips;

	private readonly NavalShipDeploymentLimit _playerTeamShipDeploymentLimit;

	private readonly NavalShipDeploymentLimit _playerAllyTeamShipDeploymentLimit;

	private readonly NavalShipDeploymentLimit _enemyTeamShipDeploymentLimit;

	public MBReadOnlyList<IShipOrigin> PlayerShips => _playerTeamShips;

	public MBReadOnlyList<IShipOrigin> PlayerAllyShips => _playerAllyTeamShips;

	public MBReadOnlyList<IShipOrigin> PlayerEnemyShips => _enemyTeamShips;

	public override void OnMissionStateFinalized()
	{
		SailWindProfile.FinalizeProfile();
	}

	public override void OnDeploymentFinished()
	{
		foreach (MissionShip allShip in _shipsLogic.AllShips)
		{
			allShip.SetAnchor(isAnchored: false);
			if (!allShip.IsPlayerShip)
			{
				allShip.SetController(ShipControllerType.AI);
			}
		}
		_shipsLogic.SetDeploymentMode(value: false);
	}

	internal void DeployBattleSide(BattleSideEnum battleSide)
	{
		MakeDeploymentPlansForSide(battleSide);
		foreach (Team item in Mission.GetTeamsOfSide(battleSide))
		{
			foreach (Formation item2 in item.FormationsIncludingEmpty)
			{
				FormationClass formationIndex = item2.FormationIndex;
				IFormationDeploymentPlan formationPlan = _deploymentPlan.GetFormationPlan(item, formationIndex);
				if (formationPlan.HasFrame())
				{
					MatrixFrame spawnFrame = formationPlan.GetFrame();
					_shipsLogic.SpawnShip(item2, in spawnFrame, spawnAnchored: true, checkForFreeArea: false).SetController(ShipControllerType.None);
				}
			}
		}
	}

	public DefaultNavalMissionLogic(MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips, NavalShipDeploymentLimit playerTeamShipDeploymentLimit, NavalShipDeploymentLimit playerAllyTeamShipDeploymentLimit, NavalShipDeploymentLimit enemyTeamShipDeploymentLimit)
	{
		_playerTeamShips = playerShips;
		_playerAllyTeamShips = playerAllyShips;
		_enemyTeamShips = enemyShips;
		_playerTeamShipDeploymentLimit = playerTeamShipDeploymentLimit;
		_playerAllyTeamShipDeploymentLimit = playerAllyTeamShipDeploymentLimit;
		_enemyTeamShipDeploymentLimit = enemyTeamShipDeploymentLimit;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_deploymentPlan = base.Mission.GetMissionBehavior<NavalMissionDeploymentPlanningLogic>();
		UpdateSceneWindDirection();
		if (base.Mission.TerrainType != TerrainType.River)
		{
			UpdateSceneWaterStrength();
		}
		InitializeShipAssignments();
	}

	public override void OnBehaviorInitialize()
	{
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		_shipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_shipsLogic.SetDeploymentMode(value: true);
		_shipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerTeam, _playerTeamShipDeploymentLimit);
		_shipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerAllyTeam, _playerAllyTeamShipDeploymentLimit);
		_shipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.EnemyTeam, _enemyTeamShipDeploymentLimit);
		MissionGameModels.Current.BattleInitializationModel.InitializeModel();
	}

	public AgentState GetAgentState(Agent affectedAgent, float deathProbability, out bool usedSurgery)
	{
		return GetNavalAgentState(affectedAgent, deathProbability, out usedSurgery);
	}

	private void InitializeShipAssignments()
	{
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_shipsLogic.ClearShipAssignments();
		if (_playerTeamShips.Count > 0)
		{
			int b = TaleWorlds.Library.MathF.Min(_playerTeamShipDeploymentLimit.NetDeploymentLimit, _playerTeamShips.Count);
			b = TaleWorlds.Library.MathF.Min(missionBehavior.GetTeamTroopOrigins(TeamSideEnum.PlayerTeam).Count(), b);
			foreach (var item in AssignShipsToFormations(_playerTeamShips, b))
			{
				_shipsLogic.SetShipAssignment(TeamSideEnum.PlayerTeam, item.formationIndex, item.ship);
			}
		}
		if (_playerAllyTeamShips != null && _playerAllyTeamShips.Count > 0)
		{
			int b2 = TaleWorlds.Library.MathF.Min(_playerAllyTeamShipDeploymentLimit.NetDeploymentLimit, _playerAllyTeamShips.Count);
			b2 = TaleWorlds.Library.MathF.Min(missionBehavior.GetTeamTroopOrigins(TeamSideEnum.PlayerAllyTeam).Count(), b2);
			foreach (var item2 in AssignShipsToFormations(_playerAllyTeamShips, b2))
			{
				_shipsLogic.SetShipAssignment(TeamSideEnum.PlayerAllyTeam, item2.formationIndex, item2.ship);
			}
		}
		if (_enemyTeamShips.Count <= 0)
		{
			return;
		}
		int b3 = TaleWorlds.Library.MathF.Min(_enemyTeamShipDeploymentLimit.NetDeploymentLimit, _enemyTeamShips.Count);
		b3 = TaleWorlds.Library.MathF.Min(missionBehavior.GetTeamTroopOrigins(TeamSideEnum.EnemyTeam).Count(), b3);
		foreach (var item3 in AssignShipsToFormations(_enemyTeamShips, b3))
		{
			_shipsLogic.SetShipAssignment(TeamSideEnum.EnemyTeam, item3.formationIndex, item3.ship);
		}
	}

	private float GetTeamSpawnPathOffsetRange(Path initialSpawnPath, Team team)
	{
		float num = 0f;
		_ = team.TeamSide;
		for (int i = 0; i < 11; i++)
		{
			ShipAssignment shipAssignment = _shipsLogic.GetShipAssignment(team.TeamSide, (FormationClass)i);
			if (shipAssignment.IsSet)
			{
				num = Math.Max(shipAssignment.MissionShipObject.DeploymentArea.y, num);
			}
		}
		return 1.1f * num;
	}

	private List<(FormationClass formationIndex, IShipOrigin ship)> AssignShipsToFormations(MBReadOnlyList<IShipOrigin> ships, int shipCount)
	{
		List<(FormationClass, IShipOrigin)> list = new List<(FormationClass, IShipOrigin)>();
		int num = 8;
		int num2 = 0;
		foreach (IShipOrigin ship in ships)
		{
			if (num2 < num && num2 < shipCount)
			{
				list.Add(((FormationClass)num2, ship));
				num2++;
				continue;
			}
			break;
		}
		return list;
	}

	private void MakeDeploymentPlansForSide(BattleSideEnum battleSide)
	{
		MBReadOnlyList<(Team, int)> mBReadOnlyList = CollectSortedBattleSideTeamsData(battleSide);
		SpawnPathData initialSpawnPathData = Mission.Current.GetInitialSpawnPathData(battleSide);
		Path path = initialSpawnPathData.Path;
		float[] array = new float[mBReadOnlyList.Count];
		for (int i = 0; i < mBReadOnlyList.Count; i++)
		{
			Team item = mBReadOnlyList[i].Item1;
			AddTeamShipsToDeploymentPlan(item);
			array[i] = GetTeamSpawnPathOffsetRange(path, item);
		}
		float baseDeploymentOffset = _shipsLogic.ComputeSpawnPathDeploymentOffset(path);
		DefaultBattleMissionAgentSpawnLogic.ComputeDeploymentBaseOffsets(initialSpawnPathData, baseDeploymentOffset, out var deployingSideBaseOffset, out var opposingSideBaseOffset);
		DefaultBattleMissionAgentSpawnLogic.ComputeTeamDeploymentOffsets(initialSpawnPathData, deployingSideBaseOffset, 32f, array, out var teamDeployOffsets);
		for (int j = 0; j < mBReadOnlyList.Count; j++)
		{
			_deploymentPlan.MakeDeploymentPlan(mBReadOnlyList[j].Item1, teamDeployOffsets[j], opposingSideBaseOffset);
		}
	}

	private void AddTeamShipsToDeploymentPlan(Team team)
	{
		for (int i = 0; i < 11; i++)
		{
			ShipAssignment shipAssignment = _shipsLogic.GetShipAssignment(team.TeamSide, (FormationClass)i);
			if (shipAssignment.IsSet)
			{
				_deploymentPlan.AddShip(team, shipAssignment.FormationIndex, shipAssignment.ShipOrigin);
			}
		}
	}

	internal static AgentState GetNavalAgentState(Agent affectedAgent, float deathProbability, out bool usedSurgery)
	{
		if (affectedAgent.IsInWater())
		{
			usedSurgery = true;
			if (affectedAgent.Character != null && affectedAgent.Character.IsHero)
			{
				return AgentState.Unconscious;
			}
			return AgentState.Killed;
		}
		usedSurgery = false;
		return AgentState.None;
	}

	internal static void UpdateSceneWindDirection()
	{
		Vec2 windVector = Mission.Current.Scene.GetGlobalWindVelocity();
		if (windVector.IsNonZero())
		{
			float northRotation = Mission.Current.Scene.GetNorthRotation();
			windVector.RotateCCW(northRotation);
			Mission.Current.Scene.SetGlobalWindVelocity(in windVector);
		}
	}

	internal static void UpdateSceneWaterStrength()
	{
		float length = Mission.Current.Scene.GetGlobalWindVelocity().Length;
		float num = 30f;
		float num2 = 10f;
		Mission.Current.Scene.SetWaterStrength(length * num2 / num);
	}

	private MBReadOnlyList<(Team team, int shipCount)> CollectSortedBattleSideTeamsData(BattleSideEnum battleSide)
	{
		MBList<(Team, int)> mBList = new MBList<(Team, int)>();
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side == battleSide)
			{
				int countOfSetShipAssignments = _shipsLogic.GetCountOfSetShipAssignments(team.TeamSide);
				if (countOfSetShipAssignments > 0)
				{
					mBList.Add((team, countOfSetShipAssignments));
				}
			}
		}
		mBList.Sort(delegate((Team team, int shipCount) t1, (Team team, int shipCount) t2)
		{
			bool flag = t1.team == base.Mission.PlayerTeam || t1.team == base.Mission.PlayerEnemyTeam;
			bool flag2 = t2.team == base.Mission.PlayerTeam || t2.team == base.Mission.PlayerEnemyTeam;
			if (!flag && !flag2)
			{
				if (t1.shipCount > t2.shipCount)
				{
					return -1;
				}
				if (t1.shipCount < t2.shipCount)
				{
					return 1;
				}
				return 0;
			}
			return flag ? 1 : (-1);
		});
		return mBList;
	}
}
