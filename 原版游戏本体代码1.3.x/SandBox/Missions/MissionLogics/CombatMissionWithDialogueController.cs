using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.AI.AgentComponents;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000066 RID: 102
	public class CombatMissionWithDialogueController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
	{
		// Token: 0x06000406 RID: 1030 RVA: 0x000173B7 File Offset: 0x000155B7
		public CombatMissionWithDialogueController(IMissionTroopSupplier[] suppliers, BasicCharacterObject characterToTalkTo)
		{
			this._troopSuppliers = suppliers;
			this._characterToTalkTo = characterToTalkTo;
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x000173CD File Offset: 0x000155CD
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = true;
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x000173E1 File Offset: 0x000155E1
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._battleAgentLogic = Mission.Current.GetMissionBehavior<BattleAgentLogic>();
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x000173F9 File Offset: 0x000155F9
		public override void AfterStart()
		{
			base.AfterStart();
			base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x00017414 File Offset: 0x00015614
		public override void OnMissionTick(float dt)
		{
			if (!this._isMissionInitialized)
			{
				this.SpawnAgents();
				this._isMissionInitialized = true;
				Mission.Current.OnDeploymentFinished();
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
			if (!this._conversationInitialized && Agent.Main != null && Agent.Main.IsActive())
			{
				foreach (Agent agent2 in base.Mission.Agents)
				{
					ScriptedMovementComponent component = agent2.GetComponent<ScriptedMovementComponent>();
					if (component != null && component.ShouldConversationStartWithAgent())
					{
						this.StartConversation(agent2, true);
						this._conversationInitialized = true;
					}
				}
			}
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x00017520 File Offset: 0x00015720
		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			if (!this._conversationInitialized && affectedAgent.Team != Mission.Current.PlayerTeam && affectorAgent != null && affectorAgent == Agent.Main)
			{
				this._conversationInitialized = true;
				this.StartFight(false);
			}
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x00017558 File Offset: 0x00015758
		public void StartFight(bool hasPlayerChangedSide)
		{
			base.Mission.SetMissionMode(MissionMode.Battle, false);
			if (hasPlayerChangedSide)
			{
				Agent.Main.SetTeam((Agent.Main.Team == base.Mission.AttackerTeam) ? base.Mission.DefenderTeam : base.Mission.AttackerTeam, true);
				Mission.Current.PlayerTeam = Agent.Main.Team;
			}
			foreach (Agent agent in base.Mission.Agents)
			{
				if (Agent.Main != agent)
				{
					if (hasPlayerChangedSide && agent.Team != Mission.Current.PlayerTeam && agent.Origin.BattleCombatant as PartyBase == PartyBase.MainParty)
					{
						agent.SetTeam(Mission.Current.PlayerTeam, true);
					}
					AgentFlag agentFlags = agent.GetAgentFlags();
					agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
					agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
					agent.GetComponent<CampaignAgentComponent>().AgentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
					agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
				}
			}
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x0001768C File Offset: 0x0001588C
		public void StartConversation(Agent agent, bool setActionsInstantly)
		{
			Campaign.Current.ConversationManager.SetupAndStartMissionConversation(agent, base.Mission.MainAgent, setActionsInstantly);
			foreach (IAgent agent2 in Campaign.Current.ConversationManager.ConversationAgents)
			{
				Agent agent3 = (Agent)agent2;
				agent3.ForceAiBehaviorSelection();
				agent3.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(true);
			}
			base.Mission.MainAgentServer.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(true);
			base.Mission.SetMissionMode(MissionMode.Conversation, setActionsInstantly);
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x00017730 File Offset: 0x00015930
		private void SpawnAgents()
		{
			Agent targetAgent = null;
			IMissionTroopSupplier[] troopSuppliers = this._troopSuppliers;
			for (int i = 0; i < troopSuppliers.Length; i++)
			{
				foreach (IAgentOriginBase agentOriginBase in troopSuppliers[i].SupplyTroops(25).ToList<IAgentOriginBase>())
				{
					Agent agent = Mission.Current.SpawnTroop(agentOriginBase, agentOriginBase.BattleCombatant.Side == BattleSideEnum.Attacker, false, false, false, 0, 0, false, true, true, null, null, null, null, FormationClass.NumberOfAllFormations, false);
					this._numSpawnedTroops++;
					if (!agent.IsMainAgent)
					{
						agent.AddComponent(new ScriptedMovementComponent(agent, agent.Character == this._characterToTalkTo, (float)(agentOriginBase.IsUnderPlayersCommand ? 5 : 2)));
						if (agent.Character == this._characterToTalkTo)
						{
							targetAgent = agent;
						}
					}
				}
			}
			foreach (Agent agent2 in base.Mission.Agents)
			{
				ScriptedMovementComponent component = agent2.GetComponent<ScriptedMovementComponent>();
				if (component != null)
				{
					if (agent2.Team.Side == Mission.Current.PlayerTeam.Side)
					{
						component.SetTargetAgent(targetAgent);
					}
					else
					{
						component.SetTargetAgent(Agent.Main);
					}
				}
				agent2.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire);
			}
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x000178C8 File Offset: 0x00015AC8
		public void StartSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000410 RID: 1040 RVA: 0x000178CA File Offset: 0x00015ACA
		public void StopSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x000178CC File Offset: 0x00015ACC
		public bool IsSideSpawnEnabled(BattleSideEnum side)
		{
			return false;
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x000178CF File Offset: 0x00015ACF
		public float GetReinforcementInterval()
		{
			return 0f;
		}

		// Token: 0x06000413 RID: 1043 RVA: 0x000178D8 File Offset: 0x00015AD8
		public bool IsSideDepleted(BattleSideEnum side)
		{
			int num = this._troopSuppliers[(int)side].GetAllTroops().Count<IAgentOriginBase>() - this._troopSuppliers[(int)side].NumTroopsNotSupplied - this._troopSuppliers[(int)side].NumRemovedTroops;
			if (Mission.Current.PlayerTeam == base.Mission.DefenderTeam)
			{
				if (side == BattleSideEnum.Attacker)
				{
					num -= MobileParty.MainParty.Party.NumberOfHealthyMembers;
				}
				else if (Agent.Main != null && Agent.Main.IsActive())
				{
					num += MobileParty.MainParty.Party.NumberOfHealthyMembers;
				}
			}
			return num == 0;
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x0001796C File Offset: 0x00015B6C
		public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x00017973 File Offset: 0x00015B73
		public int GetNumberOfPlayerControllableTroops()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000416 RID: 1046 RVA: 0x0001797A File Offset: 0x00015B7A
		public bool GetSpawnHorses(BattleSideEnum side)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04000212 RID: 530
		private BattleAgentLogic _battleAgentLogic;

		// Token: 0x04000213 RID: 531
		private readonly BasicCharacterObject _characterToTalkTo;

		// Token: 0x04000214 RID: 532
		private bool _isMissionInitialized;

		// Token: 0x04000215 RID: 533
		private bool _troopsInitialized;

		// Token: 0x04000216 RID: 534
		private bool _conversationInitialized;

		// Token: 0x04000217 RID: 535
		private int _numSpawnedTroops;

		// Token: 0x04000218 RID: 536
		private readonly IMissionTroopSupplier[] _troopSuppliers;
	}
}
