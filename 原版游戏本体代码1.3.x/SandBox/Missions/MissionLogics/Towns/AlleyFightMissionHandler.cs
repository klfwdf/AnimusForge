using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns
{
	// Token: 0x0200008D RID: 141
	public class AlleyFightMissionHandler : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
	{
		// Token: 0x06000577 RID: 1399 RVA: 0x000241CF File Offset: 0x000223CF
		public AlleyFightMissionHandler(TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
		{
			this._playerSideTroops = playerSideTroops;
			this._rivalSideTroops = rivalSideTroops;
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x000241FC File Offset: 0x000223FC
		public override void EarlyStart()
		{
			base.EarlyStart();
			base.Mission.Teams.Add(BattleSideEnum.Defender, Clan.PlayerClan.Color, Clan.PlayerClan.Color2, Clan.PlayerClan.Banner, true, false, true);
			base.Mission.Teams.Add(BattleSideEnum.Attacker, Clan.BanditFactions.First<Clan>().Color, Clan.BanditFactions.First<Clan>().Color2, Clan.BanditFactions.First<Clan>().Banner, true, false, true);
			base.Mission.PlayerTeam = base.Mission.DefenderTeam;
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x0002429C File Offset: 0x0002249C
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
			if (this._playerSideAliveAgents.Contains(affectedAgent))
			{
				this._playerSideAliveAgents.Remove(affectedAgent);
				this._playerSideTroops.RemoveTroop(affectedAgent.Character as CharacterObject, 1, default(UniqueTroopDescriptor), 0);
			}
			else if (this._rivalSideAliveAgents.Contains(affectedAgent))
			{
				this._rivalSideAliveAgents.Remove(affectedAgent);
				this._rivalSideTroops.RemoveTroop(affectedAgent.Character as CharacterObject, 1, default(UniqueTroopDescriptor), 0);
			}
			if (affectedAgent == Agent.Main)
			{
				Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().OnPlayerDiedInMission();
			}
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x00024348 File Offset: 0x00022548
		public override void AfterStart()
		{
			DefaultMissionDeploymentPlan defaultMissionDeploymentPlan;
			base.Mission.GetDeploymentPlan<DefaultMissionDeploymentPlan>(out defaultMissionDeploymentPlan);
			defaultMissionDeploymentPlan.AddTroops(base.Mission.DefenderTeam, FormationClass.Infantry, this._playerSideTroops.TotalManCount, 0, false);
			defaultMissionDeploymentPlan.AddTroops(base.Mission.AttackerTeam, FormationClass.Infantry, this._rivalSideTroops.TotalManCount, 0, false);
			base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
		}

		// Token: 0x0600057B RID: 1403 RVA: 0x000243B4 File Offset: 0x000225B4
		public override InquiryData OnEndMissionRequest(out bool canLeave)
		{
			canLeave = true;
			return new InquiryData("", GameTexts.FindText("str_give_up_fight", null).ToString(), true, true, GameTexts.FindText("str_ok", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action(base.Mission.OnEndMissionResult), null, "", 0f, null, null, null);
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x0002441F File Offset: 0x0002261F
		public override void OnRetreatMission()
		{
			Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().OnPlayerRetreatedFromMission();
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x00024430 File Offset: 0x00022630
		public override void OnRenderingStarted()
		{
			Mission.Current.SetMissionMode(MissionMode.Battle, true);
			this.SpawnAgentsForBothSides();
			base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations(false);
			base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
			base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations(false);
			base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x000244A8 File Offset: 0x000226A8
		private void SpawnAgentsForBothSides()
		{
			Mission.Current.PlayerEnemyTeam.SetIsEnemyOf(Mission.Current.PlayerTeam, true);
			foreach (TroopRosterElement troopRosterElement in this._playerSideTroops.GetTroopRoster())
			{
				for (int i = 0; i < troopRosterElement.Number; i++)
				{
					this.SpawnATroop(troopRosterElement.Character, true);
				}
			}
			foreach (TroopRosterElement troopRosterElement2 in this._rivalSideTroops.GetTroopRoster())
			{
				for (int j = 0; j < troopRosterElement2.Number; j++)
				{
					this.SpawnATroop(troopRosterElement2.Character, false);
				}
			}
		}

		// Token: 0x0600057F RID: 1407 RVA: 0x00024598 File Offset: 0x00022798
		private void SpawnATroop(CharacterObject character, bool isPlayerSide)
		{
			SimpleAgentOrigin troopOrigin = new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor));
			Agent agent = Mission.Current.SpawnTroop(troopOrigin, isPlayerSide, true, false, false, 0, 0, true, true, true, null, null, null, null, FormationClass.NumberOfAllFormations, false);
			if (isPlayerSide)
			{
				this._playerSideAliveAgents.Add(agent);
			}
			else
			{
				this._rivalSideAliveAgents.Add(agent);
			}
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags((agentFlags | AgentFlag.CanGetAlarmed) & ~AgentFlag.CanRetreat);
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			if (isPlayerSide)
			{
				agent.SetTeam(Mission.Current.PlayerTeam, true);
				return;
			}
			agent.SetTeam(Mission.Current.PlayerEnemyTeam, true);
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x00024651 File Offset: 0x00022851
		public void StartSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x00024653 File Offset: 0x00022853
		public void StopSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x00024655 File Offset: 0x00022855
		public bool IsSideSpawnEnabled(BattleSideEnum side)
		{
			return true;
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x00024658 File Offset: 0x00022858
		public bool IsSideDepleted(BattleSideEnum side)
		{
			if (side != BattleSideEnum.Attacker)
			{
				return this._playerSideAliveAgents.Count == 0;
			}
			return this._rivalSideAliveAgents.Count == 0;
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x0002467B File Offset: 0x0002287B
		public float GetReinforcementInterval()
		{
			return float.MaxValue;
		}

		// Token: 0x06000585 RID: 1413 RVA: 0x00024682 File Offset: 0x00022882
		public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000586 RID: 1414 RVA: 0x00024689 File Offset: 0x00022889
		public int GetNumberOfPlayerControllableTroops()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000587 RID: 1415 RVA: 0x00024690 File Offset: 0x00022890
		public bool GetSpawnHorses(BattleSideEnum side)
		{
			return false;
		}

		// Token: 0x040002DA RID: 730
		private TroopRoster _playerSideTroops;

		// Token: 0x040002DB RID: 731
		private TroopRoster _rivalSideTroops;

		// Token: 0x040002DC RID: 732
		private List<Agent> _playerSideAliveAgents = new List<Agent>();

		// Token: 0x040002DD RID: 733
		private List<Agent> _rivalSideAliveAgents = new List<Agent>();
	}
}
