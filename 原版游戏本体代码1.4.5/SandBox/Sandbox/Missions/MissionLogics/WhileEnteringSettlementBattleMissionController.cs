using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class WhileEnteringSettlementBattleMissionController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	private const int GuardSpawnPointAndPlayerSpawnPointPositionDelta = 20;

	private BattleAgentLogic _battleAgentLogic;

	private bool _isMissionInitialized;

	private bool _troopsInitialized;

	private int _numberOfMaxTroopForPlayer;

	private int _numberOfMaxTroopForEnemy;

	private int _playerSideSpawnedTroopCount;

	private int _otherSideSpawnedTroopCount;

	private readonly IMissionTroopSupplier[] _troopSuppliers;

	public BattleSideEnum PlayerSide => BattleSideEnum.None;

	public WhileEnteringSettlementBattleMissionController(IMissionTroopSupplier[] suppliers, int numberOfMaxTroopForPlayer, int numberOfMaxTroopForEnemy)
	{
		_troopSuppliers = suppliers;
		_numberOfMaxTroopForPlayer = numberOfMaxTroopForPlayer;
		_numberOfMaxTroopForEnemy = numberOfMaxTroopForEnemy;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_battleAgentLogic = Mission.Current.GetMissionBehavior<BattleAgentLogic>();
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			SpawnAgents();
			_isMissionInitialized = true;
		}
		else
		{
			if (_troopsInitialized)
			{
				return;
			}
			_troopsInitialized = true;
			foreach (Agent agent in base.Mission.Agents)
			{
				_battleAgentLogic.OnAgentBuild(agent, null);
			}
		}
	}

	private void SpawnAgents()
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_outside_near_town_main_gate");
		IMissionTroopSupplier[] troopSuppliers = _troopSuppliers;
		for (int i = 0; i < troopSuppliers.Length; i++)
		{
			foreach (IAgentOriginBase item in troopSuppliers[i].SupplyTroops(_numberOfMaxTroopForPlayer + _numberOfMaxTroopForEnemy).ToList())
			{
				bool flag = item.IsUnderPlayersCommand || item.Troop.IsPlayerCharacter;
				if ((!flag || _numberOfMaxTroopForPlayer >= _playerSideSpawnedTroopCount) && (flag || _numberOfMaxTroopForEnemy >= _otherSideSpawnedTroopCount))
				{
					WorldFrame worldFrame = new WorldFrame(gameEntity.GetGlobalFrame().rotation, new WorldPosition(base.Mission.Scene, gameEntity.GetGlobalFrame().origin));
					if (!flag)
					{
						worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + worldFrame.Rotation.f.AsVec2 * 20f);
						worldFrame.Rotation.f = (gameEntity.GetGlobalFrame().origin.AsVec2 - worldFrame.Origin.AsVec2).ToVec3();
						worldFrame.Origin.SetVec2(base.Mission.GetRandomPositionAroundPoint(worldFrame.Origin.GetNavMeshVec3(), 0f, 2.5f).AsVec2);
					}
					worldFrame.Rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					bool spawnWithHorse = (item.Troop.IsPlayerCharacter ? true : false);
					base.Mission.SpawnTroop(item, flag, hasFormation: false, spawnWithHorse, isReinforcement: false, 0, 0, isAlarmed: true, wieldInitialWeapons: false, worldFrame.Origin.GetGroundVec3(), worldFrame.Rotation.f.AsVec2).Defensiveness = 1f;
					if (flag)
					{
						_playerSideSpawnedTroopCount++;
					}
					else
					{
						_otherSideSpawnedTroopCount++;
					}
				}
			}
		}
	}

	public void StartSpawner(BattleSideEnum side)
	{
	}

	public void StopSpawner(BattleSideEnum side)
	{
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return false;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return 0f;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		if (side == base.Mission.PlayerTeam.Side)
		{
			return _troopSuppliers[(int)side].NumRemovedTroops == _playerSideSpawnedTroopCount;
		}
		return _troopSuppliers[(int)side].NumRemovedTroops == _otherSideSpawnedTroopCount;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		throw new NotImplementedException();
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		throw new NotImplementedException();
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return false;
	}
}
