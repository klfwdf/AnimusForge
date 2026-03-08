using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200008C RID: 140
	public class WhileEnteringSettlementBattleMissionController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
	{
		// Token: 0x0600056B RID: 1387 RVA: 0x00023E6E File Offset: 0x0002206E
		public WhileEnteringSettlementBattleMissionController(IMissionTroopSupplier[] suppliers, int numberOfMaxTroopForPlayer, int numberOfMaxTroopForEnemy)
		{
			this._troopSuppliers = suppliers;
			this._numberOfMaxTroopForPlayer = numberOfMaxTroopForPlayer;
			this._numberOfMaxTroopForEnemy = numberOfMaxTroopForEnemy;
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x00023E8B File Offset: 0x0002208B
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._battleAgentLogic = Mission.Current.GetMissionBehavior<BattleAgentLogic>();
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x00023EA4 File Offset: 0x000220A4
		public override void OnMissionTick(float dt)
		{
			if (!this._isMissionInitialized)
			{
				this.SpawnAgents();
				this._isMissionInitialized = true;
				return;
			}
			if (!this._troopsInitialized)
			{
				this._troopsInitialized = true;
				foreach (Agent agent in base.Mission.Agents)
				{
					this._battleAgentLogic.OnAgentBuild(agent, null);
				}
			}
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x00023F28 File Offset: 0x00022128
		private void SpawnAgents()
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_outside_near_town_main_gate");
			IMissionTroopSupplier[] troopSuppliers = this._troopSuppliers;
			for (int i = 0; i < troopSuppliers.Length; i++)
			{
				foreach (IAgentOriginBase agentOriginBase in troopSuppliers[i].SupplyTroops(this._numberOfMaxTroopForPlayer + this._numberOfMaxTroopForEnemy).ToList<IAgentOriginBase>())
				{
					bool flag = agentOriginBase.IsUnderPlayersCommand || agentOriginBase.Troop.IsPlayerCharacter;
					if ((!flag || this._numberOfMaxTroopForPlayer >= this._playerSideSpawnedTroopCount) && (flag || this._numberOfMaxTroopForEnemy >= this._otherSideSpawnedTroopCount))
					{
						WorldFrame worldFrame = new WorldFrame(gameEntity.GetGlobalFrame().rotation, new WorldPosition(base.Mission.Scene, gameEntity.GetGlobalFrame().origin));
						if (!flag)
						{
							worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + worldFrame.Rotation.f.AsVec2 * 20f);
							worldFrame.Rotation.f = (gameEntity.GetGlobalFrame().origin.AsVec2 - worldFrame.Origin.AsVec2).ToVec3(0f);
							worldFrame.Origin.SetVec2(base.Mission.GetRandomPositionAroundPoint(worldFrame.Origin.GetNavMeshVec3(), 0f, 2.5f, false).AsVec2);
						}
						worldFrame.Rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
						base.Mission.SpawnTroop(agentOriginBase, flag, false, false, false, 0, 0, true, false, false, new Vec3?(worldFrame.Origin.GetGroundVec3()), new Vec2?(worldFrame.Rotation.f.AsVec2), null, null, FormationClass.NumberOfAllFormations, false).Defensiveness = 1f;
						if (flag)
						{
							this._playerSideSpawnedTroopCount++;
						}
						else
						{
							this._otherSideSpawnedTroopCount++;
						}
					}
				}
			}
		}

		// Token: 0x0600056F RID: 1391 RVA: 0x00024170 File Offset: 0x00022370
		public void StartSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000570 RID: 1392 RVA: 0x00024172 File Offset: 0x00022372
		public void StopSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000571 RID: 1393 RVA: 0x00024174 File Offset: 0x00022374
		public bool IsSideSpawnEnabled(BattleSideEnum side)
		{
			return false;
		}

		// Token: 0x06000572 RID: 1394 RVA: 0x00024177 File Offset: 0x00022377
		public float GetReinforcementInterval()
		{
			return 0f;
		}

		// Token: 0x06000573 RID: 1395 RVA: 0x0002417E File Offset: 0x0002237E
		public bool IsSideDepleted(BattleSideEnum side)
		{
			if (side == base.Mission.PlayerTeam.Side)
			{
				return this._troopSuppliers[(int)side].NumRemovedTroops == this._playerSideSpawnedTroopCount;
			}
			return this._troopSuppliers[(int)side].NumRemovedTroops == this._otherSideSpawnedTroopCount;
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x000241BE File Offset: 0x000223BE
		public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x000241C5 File Offset: 0x000223C5
		public int GetNumberOfPlayerControllableTroops()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x000241CC File Offset: 0x000223CC
		public bool GetSpawnHorses(BattleSideEnum side)
		{
			return false;
		}

		// Token: 0x040002D1 RID: 721
		private const int GuardSpawnPointAndPlayerSpawnPointPositionDelta = 20;

		// Token: 0x040002D2 RID: 722
		private BattleAgentLogic _battleAgentLogic;

		// Token: 0x040002D3 RID: 723
		private bool _isMissionInitialized;

		// Token: 0x040002D4 RID: 724
		private bool _troopsInitialized;

		// Token: 0x040002D5 RID: 725
		private int _numberOfMaxTroopForPlayer;

		// Token: 0x040002D6 RID: 726
		private int _numberOfMaxTroopForEnemy;

		// Token: 0x040002D7 RID: 727
		private int _playerSideSpawnedTroopCount;

		// Token: 0x040002D8 RID: 728
		private int _otherSideSpawnedTroopCount;

		// Token: 0x040002D9 RID: 729
		private readonly IMissionTroopSupplier[] _troopSuppliers;
	}
}
