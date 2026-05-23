using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Hideout
{
	// Token: 0x02000094 RID: 148
	public class HideoutMissionController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
	{
		// Token: 0x06000612 RID: 1554 RVA: 0x00029810 File Offset: 0x00027A10
		public HideoutMissionController(IMissionTroopSupplier[] suppliers, BattleSideEnum playerSide, int firstPhaseEnemyTroopCount, int firstPhasePlayerSideTroopCount)
		{
			this._areaMarkers = new List<CommonAreaMarker>();
			this._patrolAreas = new List<PatrolArea>();
			this._defenderAgentObjects = new Dictionary<Agent, HideoutMissionController.UsedObject>();
			this._firstPhaseEnemyTroopCount = firstPhaseEnemyTroopCount;
			this._firstPhasePlayerSideTroopCount = firstPhasePlayerSideTroopCount;
			this._overriddenHideoutBossCharacterObject = null;
			this._missionSides = new HideoutMissionController.MissionSide[2];
			for (int i = 0; i < 2; i++)
			{
				IMissionTroopSupplier troopSupplier = suppliers[i];
				bool isPlayerSide = i == (int)playerSide;
				this._missionSides[i] = new HideoutMissionController.MissionSide((BattleSideEnum)i, troopSupplier, isPlayerSide);
			}
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x0002988B File Offset: 0x00027A8B
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = false;
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x000298A0 File Offset: 0x00027AA0
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._battleAgentLogic = base.Mission.GetMissionBehavior<BattleAgentLogic>();
			this._battleEndLogic = base.Mission.GetMissionBehavior<BattleEndLogic>();
			this._battleEndLogic.ChangeCanCheckForEndCondition(false);
			this._agentVictoryLogic = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
			this._cinematicController = base.Mission.GetMissionBehavior<HideoutCinematicController>();
			base.Mission.IsMainAgentObjectInteractionEnabled = false;
			this._cinematicController = base.Mission.GetMissionBehavior<HideoutCinematicController>();
		}

		// Token: 0x06000615 RID: 1557 RVA: 0x00029920 File Offset: 0x00027B20
		public override void OnObjectStoppedBeingUsed(Agent userAgent, UsableMissionObject usedObject)
		{
			if (usedObject != null && usedObject is AnimationPoint && userAgent.IsActive() && userAgent.IsAIControlled && userAgent.CurrentWatchState == Agent.WatchState.Patrolling)
			{
				PatrolArea firstScriptOfType = usedObject.GameEntity.Parent.GetFirstScriptOfType<PatrolArea>();
				if (firstScriptOfType == null)
				{
					return;
				}
				((IDetachment)firstScriptOfType).AddAgent(userAgent, -1, Agent.AIScriptedFrameFlags.None);
			}
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x00029974 File Offset: 0x00027B74
		public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
		{
			if (this._hideoutMissionState < HideoutMissionController.HideoutMissionState.ConversationBetweenLeaders && agent.Team == base.Mission.DefenderTeam)
			{
				bool flag2 = (flag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Alarmed;
				if (flag2 || (flag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Cautious)
				{
					if (agent.IsUsingGameObject)
					{
						agent.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
					else
					{
						agent.DisableScriptedMovement();
						if (agent.IsAIControlled && agent.AIMoveToGameObjectIsEnabled())
						{
							agent.AIMoveToGameObjectDisable();
							Formation formation = agent.Formation;
							if (formation != null)
							{
								formation.Team.DetachmentManager.RemoveScoresOfAgentFromDetachments(agent);
							}
						}
					}
					this._defenderAgentObjects[agent].IsMachineAITicked = false;
				}
				else if ((flag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.None)
				{
					this._defenderAgentObjects[agent].IsMachineAITicked = true;
					agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
					((IDetachment)this._defenderAgentObjects[agent].Machine).AddAgent(agent, -1, Agent.AIScriptedFrameFlags.None);
				}
				if (flag2)
				{
					agent.SetWantsToYell();
				}
			}
		}

		// Token: 0x06000617 RID: 1559 RVA: 0x00029A50 File Offset: 0x00027C50
		public override void OnMissionTick(float dt)
		{
			if (!this._isMissionInitialized)
			{
				this.InitializeMission();
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
			this.UsedObjectTick(dt);
			if (!this._battleResolved)
			{
				this.CheckBattleResolved();
			}
		}

		// Token: 0x06000618 RID: 1560 RVA: 0x00029AE8 File Offset: 0x00027CE8
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithDuel)
			{
				using (List<Agent>.Enumerator enumerator = base.Mission.Agents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Agent agent = enumerator.Current;
						if (agent != affectedAgent && agent != affectorAgent && agent.IsActive() && agent.GetLookAgent() == affectedAgent)
						{
							agent.SetLookAgent(null);
						}
					}
					return;
				}
			}
			if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.InitialFightBeforeBossFight && affectedAgent.IsMainAgent)
			{
				base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations(false);
				affectedAgent.Formation = null;
				base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Retreat);
			}
		}

		// Token: 0x06000619 RID: 1561 RVA: 0x00029BA8 File Offset: 0x00027DA8
		public void SetOverriddenHideoutBossCharacterObject(CharacterObject characterObject)
		{
			this._overriddenHideoutBossCharacterObject = characterObject;
		}

		// Token: 0x0600061A RID: 1562 RVA: 0x00029BB4 File Offset: 0x00027DB4
		private void InitializeMission()
		{
			base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(true);
			base.Mission.SetMissionMode(MissionMode.Stealth, true);
			this._areaMarkers.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
				orderby area.AreaIndex
				select area);
			this._patrolAreas.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<PatrolArea>()
				orderby area.AreaIndex
				select area);
			this.DecideMissionState();
			base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
			for (int i = 0; i < 2; i++)
			{
				int spawnCount;
				if (this._missionSides[i].IsPlayerSide)
				{
					spawnCount = this._firstPhasePlayerSideTroopCount;
				}
				else
				{
					if (this._missionSides[i].NumberOfTroopsNotSupplied <= this._firstPhaseEnemyTroopCount)
					{
						Debug.FailedAssert("_missionSides[i].NumberOfTroopsNotSupplied <= _firstPhaseEnemyTroopCount", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutMissionController.cs", "InitializeMission", 542);
						this._firstPhaseEnemyTroopCount = (int)((float)this._missionSides[i].NumberOfTroopsNotSupplied * 0.7f);
					}
					spawnCount = ((this._hideoutMissionState == HideoutMissionController.HideoutMissionState.InitialFightBeforeBossFight) ? this._firstPhaseEnemyTroopCount : this._missionSides[i].NumberOfTroopsNotSupplied);
				}
				this._missionSides[i].SpawnTroops(this._areaMarkers, this._patrolAreas, this._defenderAgentObjects, spawnCount);
			}
			Mission.Current.OnDeploymentFinished();
		}

		// Token: 0x0600061B RID: 1563 RVA: 0x00029D30 File Offset: 0x00027F30
		private void UsedObjectTick(float dt)
		{
			foreach (KeyValuePair<Agent, HideoutMissionController.UsedObject> keyValuePair in this._defenderAgentObjects)
			{
				if (keyValuePair.Value.IsMachineAITicked)
				{
					keyValuePair.Value.MachineAI.Tick(keyValuePair.Key, null, null, dt);
				}
			}
		}

		// Token: 0x0600061C RID: 1564 RVA: 0x00029DA8 File Offset: 0x00027FA8
		protected override void OnEndMission()
		{
			int num = 0;
			if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithDuel)
			{
				if (Agent.Main == null || !Agent.Main.IsActive())
				{
					List<Agent> duelPhaseAllyAgents = this._duelPhaseAllyAgents;
					num = ((duelPhaseAllyAgents != null) ? duelPhaseAllyAgents.Count : 0);
				}
				else if (this._bossAgent == null || !this._bossAgent.IsActive())
				{
					PlayerEncounter.EnemySurrender = true;
				}
			}
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount <= num && MapEvent.PlayerMapEvent.BattleState == BattleState.None)
			{
				MapEvent.PlayerMapEvent.SetOverrideWinner(BattleSideEnum.Defender);
			}
		}

		// Token: 0x0600061D RID: 1565 RVA: 0x00029E2C File Offset: 0x0002802C
		private void CheckBattleResolved()
		{
			if (this._hideoutMissionState != HideoutMissionController.HideoutMissionState.CutSceneBeforeBossFight && this._hideoutMissionState != HideoutMissionController.HideoutMissionState.ConversationBetweenLeaders)
			{
				if (this.IsSideDepleted(BattleSideEnum.Attacker))
				{
					if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithDuel)
					{
						this.OnDuelOver(BattleSideEnum.Defender);
					}
					this._battleEndLogic.ChangeCanCheckForEndCondition(true);
					this._battleResolved = true;
					return;
				}
				if (this.IsSideDepleted(BattleSideEnum.Defender))
				{
					if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.InitialFightBeforeBossFight)
					{
						if (this._firstPhaseEndTimer == null)
						{
							this._firstPhaseEndTimer = new Timer(base.Mission.CurrentTime, 4f, true);
							this._oldMissionMode = Mission.Current.Mode;
							Mission.Current.SetMissionMode(MissionMode.CutScene, false);
							return;
						}
						if (this._firstPhaseEndTimer.Check(base.Mission.CurrentTime))
						{
							this._cinematicController.StartCinematic(new HideoutCinematicController.OnInitialFadeOutFinished(this.OnInitialFadeOutOver), new Action(this.OnCutSceneOver), 0.4f, 0.2f, 8f, false);
							return;
						}
					}
					else
					{
						if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithDuel)
						{
							this.OnDuelOver(BattleSideEnum.Attacker);
						}
						this._battleEndLogic.ChangeCanCheckForEndCondition(true);
						MapEvent.PlayerMapEvent.SetOverrideWinner(BattleSideEnum.Attacker);
						this._battleResolved = true;
					}
				}
			}
		}

		// Token: 0x0600061E RID: 1566 RVA: 0x00029F52 File Offset: 0x00028152
		public void StartSpawner(BattleSideEnum side)
		{
			this._missionSides[(int)side].SetSpawnTroops(true);
		}

		// Token: 0x0600061F RID: 1567 RVA: 0x00029F62 File Offset: 0x00028162
		public void StopSpawner(BattleSideEnum side)
		{
			this._missionSides[(int)side].SetSpawnTroops(false);
		}

		// Token: 0x06000620 RID: 1568 RVA: 0x00029F72 File Offset: 0x00028172
		public bool IsSideSpawnEnabled(BattleSideEnum side)
		{
			return this._missionSides[(int)side].TroopSpawningActive;
		}

		// Token: 0x06000621 RID: 1569 RVA: 0x00029F81 File Offset: 0x00028181
		public float GetReinforcementInterval()
		{
			return 0f;
		}

		// Token: 0x06000622 RID: 1570 RVA: 0x00029F88 File Offset: 0x00028188
		public unsafe bool IsSideDepleted(BattleSideEnum side)
		{
			bool flag = this._missionSides[(int)side].NumberOfActiveTroops == 0;
			if (!flag)
			{
				if ((Agent.Main == null || !Agent.Main.IsActive()) && side == BattleSideEnum.Attacker)
				{
					if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithDuel || this._hideoutMissionState == HideoutMissionController.HideoutMissionState.InitialFightBeforeBossFight)
					{
						flag = true;
					}
					else if (this._hideoutMissionState == HideoutMissionController.HideoutMissionState.WithoutBossFight || this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithAll)
					{
						bool flag2 = base.Mission.Teams.Attacker.FormationsIncludingEmpty.Any(delegate(Formation f)
						{
							if (f.CountOfUnits > 0)
							{
								MovementOrder movementOrder = *f.GetReadonlyMovementOrderReference();
								return movementOrder.OrderType == OrderType.Charge;
							}
							return false;
						});
						bool flag3 = base.Mission.Teams.Defender.ActiveAgents.Any((Agent t) => t.CurrentWatchState == Agent.WatchState.Alarmed);
						flag = !flag2 && !flag3;
					}
				}
				else if (side == BattleSideEnum.Defender && this._hideoutMissionState == HideoutMissionController.HideoutMissionState.BossFightWithDuel && (this._bossAgent == null || !this._bossAgent.IsActive()))
				{
					flag = true;
				}
			}
			else if (side == BattleSideEnum.Defender && this._hideoutMissionState == HideoutMissionController.HideoutMissionState.InitialFightBeforeBossFight && (Agent.Main == null || !Agent.Main.IsActive()))
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x06000623 RID: 1571 RVA: 0x0002A0C0 File Offset: 0x000282C0
		private void DecideMissionState()
		{
			HideoutMissionController.MissionSide missionSide = this._missionSides[0];
			this._hideoutMissionState = ((!missionSide.IsPlayerSide) ? HideoutMissionController.HideoutMissionState.InitialFightBeforeBossFight : HideoutMissionController.HideoutMissionState.WithoutBossFight);
		}

		// Token: 0x06000624 RID: 1572 RVA: 0x0002A0E8 File Offset: 0x000282E8
		private void SetWatchStateOfAIAgents(Agent.WatchState state)
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.IsAIControlled)
				{
					agent.SetWatchState(state);
				}
			}
		}

		// Token: 0x06000625 RID: 1573 RVA: 0x0002A148 File Offset: 0x00028348
		private void SpawnBossAndBodyguards()
		{
			HideoutMissionController.MissionSide missionSide = this._missionSides[0];
			MatrixFrame banditsInitialFrame = this._cinematicController.GetBanditsInitialFrame();
			missionSide.SpawnRemainingTroopsForBossFight(new List<MatrixFrame> { banditsInitialFrame }, missionSide.NumberOfTroopsNotSupplied, this._overriddenHideoutBossCharacterObject);
			this._bossAgent = this.SelectBossAgent();
			this._bossAgent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.MeleeForMainHand);
			foreach (Agent agent in this._enemyTeam.ActiveAgents)
			{
				if (agent != this._bossAgent)
				{
					agent.WieldInitialWeapons(Agent.WeaponWieldActionType.WithAnimationUninterruptible, Equipment.InitialWeaponEquipPreference.Any);
				}
			}
		}

		// Token: 0x06000626 RID: 1574 RVA: 0x0002A1F8 File Offset: 0x000283F8
		private Agent SelectBossAgent()
		{
			Agent agent = null;
			Agent agent2 = null;
			foreach (Agent agent3 in base.Mission.Agents)
			{
				if (agent3.Team == this._enemyTeam && agent3.IsHuman)
				{
					if (agent3.IsHero)
					{
						agent = agent3;
						agent2 = agent3;
						break;
					}
					if (agent3.Character.Culture.IsBandit)
					{
						CultureObject cultureObject = agent3.Character.Culture as CultureObject;
						if (((cultureObject != null) ? cultureObject.BanditBoss : null) != null && ((CultureObject)agent3.Character.Culture).BanditBoss == agent3.Character)
						{
							agent = agent3;
						}
					}
					if (agent2 == null || agent3.Character.Level > agent2.Character.Level)
					{
						agent2 = agent3;
					}
				}
			}
			agent = agent ?? agent2;
			return agent;
		}

		// Token: 0x06000627 RID: 1575 RVA: 0x0002A2F4 File Offset: 0x000284F4
		private void OnInitialFadeOutOver(ref Agent playerAgent, ref List<Agent> playerCompanions, ref Agent bossAgent, ref List<Agent> bossCompanions, ref float placementPerturbation, ref float placementAngle)
		{
			this._hideoutMissionState = HideoutMissionController.HideoutMissionState.CutSceneBeforeBossFight;
			this._enemyTeam = base.Mission.PlayerEnemyTeam;
			this.SpawnBossAndBodyguards();
			base.Mission.PlayerTeam.SetIsEnemyOf(this._enemyTeam, false);
			this.SetWatchStateOfAIAgents(Agent.WatchState.Patrolling);
			if (Agent.Main.IsUsingGameObject)
			{
				Agent.Main.StopUsingGameObject(false, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
			}
			playerAgent = Agent.Main;
			playerCompanions = (from x in base.Mission.Agents
				where x.IsActive() && x.Team == base.Mission.PlayerTeam && x.IsHuman && x.IsAIControlled
				select x).ToList<Agent>();
			bossAgent = this._bossAgent;
			bossCompanions = (from x in base.Mission.Agents
				where x.IsActive() && x.Team == this._enemyTeam && x.IsHuman && x.IsAIControlled && x != this._bossAgent
				select x).ToList<Agent>();
		}

		// Token: 0x06000628 RID: 1576 RVA: 0x0002A3AB File Offset: 0x000285AB
		private void OnCutSceneOver()
		{
			Mission.Current.SetMissionMode(this._oldMissionMode, false);
			this._hideoutMissionState = HideoutMissionController.HideoutMissionState.ConversationBetweenLeaders;
			MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
			missionBehavior.DisableStartConversation(false);
			missionBehavior.StartConversation(this._bossAgent, false, false);
		}

		// Token: 0x06000629 RID: 1577 RVA: 0x0002A3E4 File Offset: 0x000285E4
		private void OnDuelOver(BattleSideEnum winnerSide)
		{
			AgentVictoryLogic missionBehavior = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
			if (missionBehavior != null)
			{
				missionBehavior.SetCheerActionGroup(AgentVictoryLogic.CheerActionGroupEnum.HighCheerActions);
			}
			if (missionBehavior != null)
			{
				missionBehavior.SetCheerReactionTimerSettings(0.25f, 3f);
			}
			if (winnerSide == BattleSideEnum.Attacker && this._duelPhaseAllyAgents != null)
			{
				using (List<Agent>.Enumerator enumerator = this._duelPhaseAllyAgents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Agent agent = enumerator.Current;
						if (agent.State == AgentState.Active)
						{
							agent.SetTeam(base.Mission.PlayerTeam, true);
							agent.SetWatchState(Agent.WatchState.Alarmed);
						}
					}
					return;
				}
			}
			if (winnerSide == BattleSideEnum.Defender && this._duelPhaseBanditAgents != null)
			{
				foreach (Agent agent2 in this._duelPhaseBanditAgents)
				{
					if (agent2.State == AgentState.Active)
					{
						agent2.SetTeam(this._enemyTeam, true);
						agent2.SetWatchState(Agent.WatchState.Alarmed);
					}
				}
			}
		}

		// Token: 0x0600062A RID: 1578 RVA: 0x0002A4F0 File Offset: 0x000286F0
		public static void StartBossFightDuelMode()
		{
			Mission mission = Mission.Current;
			HideoutMissionController hideoutMissionController = ((mission != null) ? mission.GetMissionBehavior<HideoutMissionController>() : null);
			if (hideoutMissionController == null)
			{
				return;
			}
			hideoutMissionController.StartBossFightDuelModeInternal();
		}

		// Token: 0x0600062B RID: 1579 RVA: 0x0002A510 File Offset: 0x00028710
		private void StartBossFightDuelModeInternal()
		{
			base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(true);
			base.Mission.PlayerTeam.SetIsEnemyOf(this._enemyTeam, true);
			this._duelPhaseAllyAgents = (from x in base.Mission.Agents
				where x.IsActive() && x.Team == base.Mission.PlayerTeam && x.IsHuman && x.IsAIControlled && x != Agent.Main
				select x).ToList<Agent>();
			this._duelPhaseBanditAgents = (from x in base.Mission.Agents
				where x.IsActive() && x.Team == this._enemyTeam && x.IsHuman && x.IsAIControlled && x != this._bossAgent
				select x).ToList<Agent>();
			foreach (Agent agent in this._duelPhaseAllyAgents)
			{
				agent.SetTeam(Team.Invalid, true);
				WorldPosition worldPosition = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref worldPosition, false, Agent.AIScriptedFrameFlags.None);
				agent.SetLookAgent(Agent.Main);
			}
			foreach (Agent agent2 in this._duelPhaseBanditAgents)
			{
				agent2.SetTeam(Team.Invalid, true);
				WorldPosition worldPosition2 = agent2.GetWorldPosition();
				agent2.SetScriptedPosition(ref worldPosition2, false, Agent.AIScriptedFrameFlags.None);
				agent2.SetLookAgent(this._bossAgent);
			}
			this._bossAgent.SetWatchState(Agent.WatchState.Alarmed);
			this._hideoutMissionState = HideoutMissionController.HideoutMissionState.BossFightWithDuel;
		}

		// Token: 0x0600062C RID: 1580 RVA: 0x0002A670 File Offset: 0x00028870
		public static void StartBossFightBattleMode()
		{
			Mission mission = Mission.Current;
			HideoutMissionController hideoutMissionController = ((mission != null) ? mission.GetMissionBehavior<HideoutMissionController>() : null);
			if (hideoutMissionController == null)
			{
				return;
			}
			hideoutMissionController.StartBossFightBattleModeInternal();
		}

		// Token: 0x0600062D RID: 1581 RVA: 0x0002A690 File Offset: 0x00028890
		private void StartBossFightBattleModeInternal()
		{
			base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(true);
			base.Mission.PlayerTeam.SetIsEnemyOf(this._enemyTeam, true);
			this.SetWatchStateOfAIAgents(Agent.WatchState.Alarmed);
			this._hideoutMissionState = HideoutMissionController.HideoutMissionState.BossFightWithAll;
			foreach (Formation formation in base.Mission.PlayerTeam.FormationsIncludingEmpty)
			{
				if (formation.CountOfUnits > 0)
				{
					formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
					formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
				}
			}
		}

		// Token: 0x0600062E RID: 1582 RVA: 0x0002A73C File Offset: 0x0002893C
		public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
		{
			return this._missionSides[(int)side].GetAllTroops();
		}

		// Token: 0x0600062F RID: 1583 RVA: 0x0002A758 File Offset: 0x00028958
		public int GetNumberOfPlayerControllableTroops()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x0002A75F File Offset: 0x0002895F
		public bool GetSpawnHorses(BattleSideEnum side)
		{
			return false;
		}

		// Token: 0x0400033F RID: 831
		private const int FirstPhaseEndInSeconds = 4;

		// Token: 0x04000340 RID: 832
		private readonly List<CommonAreaMarker> _areaMarkers;

		// Token: 0x04000341 RID: 833
		private readonly List<PatrolArea> _patrolAreas;

		// Token: 0x04000342 RID: 834
		private readonly Dictionary<Agent, HideoutMissionController.UsedObject> _defenderAgentObjects;

		// Token: 0x04000343 RID: 835
		private readonly HideoutMissionController.MissionSide[] _missionSides;

		// Token: 0x04000344 RID: 836
		private List<Agent> _duelPhaseAllyAgents;

		// Token: 0x04000345 RID: 837
		private List<Agent> _duelPhaseBanditAgents;

		// Token: 0x04000346 RID: 838
		private BattleAgentLogic _battleAgentLogic;

		// Token: 0x04000347 RID: 839
		private BattleEndLogic _battleEndLogic;

		// Token: 0x04000348 RID: 840
		private AgentVictoryLogic _agentVictoryLogic;

		// Token: 0x04000349 RID: 841
		private HideoutMissionController.HideoutMissionState _hideoutMissionState;

		// Token: 0x0400034A RID: 842
		private Agent _bossAgent;

		// Token: 0x0400034B RID: 843
		private Team _enemyTeam;

		// Token: 0x0400034C RID: 844
		private Timer _firstPhaseEndTimer;

		// Token: 0x0400034D RID: 845
		private CharacterObject _overriddenHideoutBossCharacterObject;

		// Token: 0x0400034E RID: 846
		private bool _troopsInitialized;

		// Token: 0x0400034F RID: 847
		private bool _isMissionInitialized;

		// Token: 0x04000350 RID: 848
		private bool _battleResolved;

		// Token: 0x04000351 RID: 849
		private int _firstPhaseEnemyTroopCount;

		// Token: 0x04000352 RID: 850
		private int _firstPhasePlayerSideTroopCount;

		// Token: 0x04000353 RID: 851
		private MissionMode _oldMissionMode;

		// Token: 0x04000354 RID: 852
		private HideoutCinematicController _cinematicController;

		// Token: 0x0200019C RID: 412
		private class MissionSide
		{
			// Token: 0x17000127 RID: 295
			// (get) Token: 0x06000ED0 RID: 3792 RVA: 0x00065E95 File Offset: 0x00064095
			// (set) Token: 0x06000ED1 RID: 3793 RVA: 0x00065E9D File Offset: 0x0006409D
			public bool TroopSpawningActive { get; private set; }

			// Token: 0x17000128 RID: 296
			// (get) Token: 0x06000ED2 RID: 3794 RVA: 0x00065EA6 File Offset: 0x000640A6
			public int NumberOfActiveTroops
			{
				get
				{
					return this._numberOfSpawnedTroops - this._troopSupplier.NumRemovedTroops;
				}
			}

			// Token: 0x17000129 RID: 297
			// (get) Token: 0x06000ED3 RID: 3795 RVA: 0x00065EBA File Offset: 0x000640BA
			public int NumberOfTroopsNotSupplied
			{
				get
				{
					return this._troopSupplier.NumTroopsNotSupplied;
				}
			}

			// Token: 0x06000ED4 RID: 3796 RVA: 0x00065EC7 File Offset: 0x000640C7
			public MissionSide(BattleSideEnum side, IMissionTroopSupplier troopSupplier, bool isPlayerSide)
			{
				this._side = side;
				this.IsPlayerSide = isPlayerSide;
				this._troopSupplier = troopSupplier;
			}

			// Token: 0x06000ED5 RID: 3797 RVA: 0x00065EE4 File Offset: 0x000640E4
			public void SpawnTroops(List<CommonAreaMarker> areaMarkers, List<PatrolArea> patrolAreas, Dictionary<Agent, HideoutMissionController.UsedObject> defenderAgentObjects, int spawnCount)
			{
				int num = 0;
				bool flag = false;
				List<StandingPoint> list = new List<StandingPoint>();
				foreach (CommonAreaMarker commonAreaMarker in areaMarkers)
				{
					foreach (UsableMachine usableMachine in commonAreaMarker.GetUsableMachinesInRange(null))
					{
						list.AddRange(usableMachine.StandingPoints);
					}
				}
				List<IAgentOriginBase> list2 = this._troopSupplier.SupplyTroops(spawnCount).ToList<IAgentOriginBase>();
				for (int i = 0; i < list2.Count; i++)
				{
					if (BattleSideEnum.Attacker == this._side)
					{
						Mission.Current.SpawnTroop(list2[i], true, true, false, false, 0, 0, true, true, true, null, null, null, null, FormationClass.NumberOfAllFormations, false);
						this._numberOfSpawnedTroops++;
					}
					else if (areaMarkers.Count > num)
					{
						StandingPoint standingPoint = null;
						int num2 = list2.Count - i;
						if (num2 < list.Count / 2 && num2 < 4)
						{
							flag = true;
						}
						if (!flag)
						{
							list.Shuffle<StandingPoint>();
							standingPoint = list.FirstOrDefault((StandingPoint point) => !point.IsDeactivated && !point.IsDisabled && !point.HasUser);
						}
						else
						{
							IEnumerable<PatrolArea> source = from area in patrolAreas
								where area.StandingPoints.All((StandingPoint point) => !point.HasUser && !point.HasAIMovingTo)
								select area;
							if (!source.IsEmpty<PatrolArea>())
							{
								foreach (StandingPoint standingPoint2 in source.First<PatrolArea>().StandingPoints)
								{
									if (!standingPoint2.IsDisabled)
									{
										standingPoint = standingPoint2;
										break;
									}
								}
							}
						}
						if (standingPoint != null && !standingPoint.IsDisabled)
						{
							MatrixFrame globalFrame = standingPoint.GameEntity.GetGlobalFrame();
							globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
							Agent agent = Mission.Current.SpawnTroop(list2[i], false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
							this.InitializeBanditAgent(agent, standingPoint, flag, defenderAgentObjects);
							this._numberOfSpawnedTroops++;
							int groupId = ((AnimationPoint)standingPoint).GroupId;
							if (flag)
							{
								goto IL_2D0;
							}
							using (List<StandingPoint>.Enumerator enumerator3 = standingPoint.GameEntity.Parent.GetFirstScriptOfType<UsableMachine>().StandingPoints.GetEnumerator())
							{
								while (enumerator3.MoveNext())
								{
									StandingPoint standingPoint3 = enumerator3.Current;
									int groupId2 = ((AnimationPoint)standingPoint3).GroupId;
									if (groupId == groupId2 && standingPoint3 != standingPoint)
									{
										standingPoint3.SetDisabledAndMakeInvisible(false, false);
									}
								}
								goto IL_2D0;
							}
						}
						num++;
					}
					IL_2D0:;
				}
				foreach (Formation formation in Mission.Current.AttackerTeam.FormationsIncludingEmpty)
				{
					if (formation.CountOfUnits > 0)
					{
						formation.SetMovementOrder(MovementOrder.MovementOrderMove(formation.CachedMedianPosition));
					}
					formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
					if (Mission.Current.AttackerTeam == Mission.Current.PlayerTeam)
					{
						formation.PlayerOwner = Mission.Current.MainAgent;
					}
				}
			}

			// Token: 0x06000ED6 RID: 3798 RVA: 0x000662A0 File Offset: 0x000644A0
			public void SpawnRemainingTroopsForBossFight(List<MatrixFrame> spawnFrames, int spawnCount, CharacterObject overriddenHideoutBossCharacterObject)
			{
				List<IAgentOriginBase> list = this._troopSupplier.SupplyTroops(spawnCount).ToList<IAgentOriginBase>();
				if (overriddenHideoutBossCharacterObject != null)
				{
					IAgentOriginBase agentOriginBase = list.Find((IAgentOriginBase t) => t.Troop == overriddenHideoutBossCharacterObject);
					MatrixFrame matrixFrame = spawnFrames.FirstOrDefault<MatrixFrame>();
					matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					Agent agent = Mission.Current.SpawnTroop(agentOriginBase, false, false, false, false, 0, 0, false, false, false, new Vec3?(matrixFrame.origin), new Vec2?(matrixFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
					AgentFlag agentFlags = agent.GetAgentFlags();
					if (agentFlags.HasAnyFlag(AgentFlag.CanRetreat))
					{
						agent.SetAgentFlags(agentFlags & ~AgentFlag.CanRetreat);
					}
					list.Remove(agentOriginBase);
				}
				for (int i = 0; i < list.Count; i++)
				{
					MatrixFrame matrixFrame2 = spawnFrames.FirstOrDefault<MatrixFrame>();
					matrixFrame2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					Agent agent2 = Mission.Current.SpawnTroop(list[i], false, false, false, false, 0, 0, false, false, false, new Vec3?(matrixFrame2.origin), new Vec2?(matrixFrame2.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
					AgentFlag agentFlags2 = agent2.GetAgentFlags();
					if (agentFlags2.HasAnyFlag(AgentFlag.CanRetreat))
					{
						agent2.SetAgentFlags(agentFlags2 & ~AgentFlag.CanRetreat);
					}
					this._numberOfSpawnedTroops++;
				}
				foreach (Formation formation in Mission.Current.AttackerTeam.FormationsIncludingEmpty)
				{
					if (formation.CountOfUnits > 0)
					{
						formation.SetMovementOrder(MovementOrder.MovementOrderMove(formation.CachedMedianPosition));
					}
					formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
					if (Mission.Current.AttackerTeam == Mission.Current.PlayerTeam)
					{
						formation.PlayerOwner = Mission.Current.MainAgent;
					}
				}
			}

			// Token: 0x06000ED7 RID: 3799 RVA: 0x000664C4 File Offset: 0x000646C4
			private void InitializeBanditAgent(Agent agent, StandingPoint spawnPoint, bool isPatrolling, Dictionary<Agent, HideoutMissionController.UsedObject> defenderAgentObjects)
			{
				UsableMachine usableMachine = (isPatrolling ? spawnPoint.GameEntity.Parent.GetScriptComponents<PatrolArea>().FirstOrDefault<PatrolArea>() : spawnPoint.GameEntity.Parent.GetScriptComponents<UsableMachine>().FirstOrDefault<UsableMachine>());
				if (isPatrolling)
				{
					((IDetachment)usableMachine).AddAgent(agent, -1, Agent.AIScriptedFrameFlags.None);
					agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
				}
				else
				{
					agent.UseGameObject(spawnPoint, -1);
				}
				defenderAgentObjects.Add(agent, new HideoutMissionController.UsedObject(usableMachine, isPatrolling));
				AgentFlag agentFlags = agent.GetAgentFlags();
				agent.SetAgentFlags((agentFlags | AgentFlag.CanGetAlarmed) & ~AgentFlag.CanRetreat);
				agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator().AddBehaviorGroup<AlarmedBehaviorGroup>()
					.AddBehavior<CautiousBehavior>();
				this.SimulateTick(agent);
			}

			// Token: 0x06000ED8 RID: 3800 RVA: 0x00066574 File Offset: 0x00064774
			private void SimulateTick(Agent agent)
			{
				int num = MBRandom.RandomInt(1, 20);
				for (int i = 0; i < num; i++)
				{
					if (agent.IsUsingGameObject)
					{
						agent.CurrentlyUsedGameObject.SimulateTick(0.1f);
					}
				}
			}

			// Token: 0x06000ED9 RID: 3801 RVA: 0x000665AE File Offset: 0x000647AE
			public void SetSpawnTroops(bool spawnTroops)
			{
				this.TroopSpawningActive = spawnTroops;
			}

			// Token: 0x06000EDA RID: 3802 RVA: 0x000665B7 File Offset: 0x000647B7
			public IEnumerable<IAgentOriginBase> GetAllTroops()
			{
				return this._troopSupplier.GetAllTroops();
			}

			// Token: 0x040007A7 RID: 1959
			private readonly BattleSideEnum _side;

			// Token: 0x040007A8 RID: 1960
			private readonly IMissionTroopSupplier _troopSupplier;

			// Token: 0x040007A9 RID: 1961
			public readonly bool IsPlayerSide;

			// Token: 0x040007AB RID: 1963
			private int _numberOfSpawnedTroops;
		}

		// Token: 0x0200019D RID: 413
		private class UsedObject
		{
			// Token: 0x06000EDB RID: 3803 RVA: 0x000665C4 File Offset: 0x000647C4
			public UsedObject(UsableMachine machine, bool isMachineAITicked)
			{
				this.Machine = machine;
				this.MachineAI = machine.CreateAIBehaviorObject();
				this.IsMachineAITicked = isMachineAITicked;
			}

			// Token: 0x040007AC RID: 1964
			public readonly UsableMachine Machine;

			// Token: 0x040007AD RID: 1965
			public readonly UsableMachineAIBase MachineAI;

			// Token: 0x040007AE RID: 1966
			public bool IsMachineAITicked;
		}

		// Token: 0x0200019E RID: 414
		private enum HideoutMissionState
		{
			// Token: 0x040007B0 RID: 1968
			NotDecided,
			// Token: 0x040007B1 RID: 1969
			WithoutBossFight,
			// Token: 0x040007B2 RID: 1970
			InitialFightBeforeBossFight,
			// Token: 0x040007B3 RID: 1971
			CutSceneBeforeBossFight,
			// Token: 0x040007B4 RID: 1972
			ConversationBetweenLeaders,
			// Token: 0x040007B5 RID: 1973
			BossFightWithDuel,
			// Token: 0x040007B6 RID: 1974
			BossFightWithAll
		}
	}
}
