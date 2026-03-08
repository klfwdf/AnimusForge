using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Hideout
{
	// Token: 0x02000092 RID: 146
	public class HideoutAmbushMissionController : MissionLogic
	{
		// Token: 0x1700007D RID: 125
		// (get) Token: 0x060005CB RID: 1483 RVA: 0x00026DAC File Offset: 0x00024FAC
		public bool IsReadyForCallTroopsCinematic
		{
			get
			{
				return this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.CallTroopsCutSceneState;
			}
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x00026DB8 File Offset: 0x00024FB8
		public HideoutAmbushMissionController(BattleSideEnum playerSide, FlattenedTroopRoster priorAllyTroops)
		{
			this._playerSide = playerSide;
			this._allEnemyTroops = new List<IAgentOriginBase>();
			this._priorAllyTroops = new List<IAgentOriginBase>();
			this._stealthAreaData = new List<StealthAreaMissionLogic.StealthAreaData>();
			this._waitTimerToChangeStealthModeIntoBattle = null;
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.NotDecided;
			this._overriddenHideoutBossCharacterObject = null;
			this.InitializeTroops(priorAllyTroops);
			this._initialHideoutPopulation = this._allEnemyTroops.Count;
			Campaign.Current.CampaignBehaviorManager.GetBehavior<IHideoutCampaignBehavior>();
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x00026E30 File Offset: 0x00025030
		private void InitializeTroops(FlattenedTroopRoster priorAllyTroops)
		{
			foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in priorAllyTroops)
			{
				if (flattenedTroopRosterElement.Troop != CharacterObject.PlayerCharacter)
				{
					this._priorAllyTroops.Add(new PartyAgentOrigin(PartyBase.MainParty, flattenedTroopRosterElement.Troop, -1, flattenedTroopRosterElement.Descriptor, false, false));
				}
			}
			foreach (PartyBase partyBase in MapEvent.PlayerMapEvent.InvolvedParties)
			{
				if (partyBase.Side != this._playerSide)
				{
					foreach (TroopRosterElement troopRosterElement in partyBase.MemberRoster.GetTroopRoster())
					{
						int num = troopRosterElement.Number - troopRosterElement.WoundedNumber;
						for (int i = 0; i < num; i++)
						{
							this._allEnemyTroops.Add(new PartyAgentOrigin(partyBase, troopRosterElement.Character, -1, default(UniqueTroopDescriptor), false, false));
						}
					}
				}
			}
			this._allEnemyTroops.Shuffle<IAgentOriginBase>();
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x00026F88 File Offset: 0x00025188
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = false;
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00026FB4 File Offset: 0x000251B4
		public override void AfterStart()
		{
			base.AfterStart();
			SandBoxHelpers.MissionHelper.SpawnPlayer(false, true, false, false, "");
			Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters(null);
			Agent.Main.SetClothingColor1(4281281067U);
			Agent.Main.SetClothingColor2(4281281067U);
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.StealthEquipment);
			foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
			{
				foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
				{
					this._sentryCount += keyValuePair.Value.Count;
					this._remainingSentryCount += keyValuePair.Value.Count;
				}
			}
			Mission.Current.GetMissionBehavior<StealthFailCounterMissionLogic>().FailCounterSeconds = 15f;
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x000270D8 File Offset: 0x000252D8
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._battleAgentLogic = base.Mission.GetMissionBehavior<BattleAgentLogic>();
			this._battleEndLogic = base.Mission.GetMissionBehavior<BattleEndLogic>();
			this._battleEndLogic.ChangeCanCheckForEndCondition(false);
			this._stealthAreaMissionLogic = base.Mission.GetMissionBehavior<StealthAreaMissionLogic>();
			this._stealthAreaMissionLogic.GetReinforcementAllyTroops = new StealthAreaMissionLogic.GetReinforcementAllyTroopsDelegate(this.GetReinforcementAllyTroops);
			this._hideoutAmbushBossFightCinematicController = base.Mission.GetMissionBehavior<HideoutAmbushBossFightCinematicController>();
			foreach (StealthAreaUsePoint stealthAreaUsePoint in base.Mission.ActiveMissionObjects.FindAllWithType<StealthAreaUsePoint>())
			{
				this._stealthAreaData.Add(new StealthAreaMissionLogic.StealthAreaData(stealthAreaUsePoint));
			}
			Game.Current.EventManager.RegisterEvent<OnStealthMissionCounterFailedEvent>(new Action<OnStealthMissionCounterFailedEvent>(this.OnStealthMissionCounterFailed));
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x000271C4 File Offset: 0x000253C4
		private List<IAgentOriginBase> GetReinforcementAllyTroops(StealthAreaMissionLogic.StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker)
		{
			int count = triggeredStealthAreaData.StealthAreaMarkers.Count;
			StealthAreaMarker[] array = triggeredStealthAreaData.StealthAreaMarkers.Keys.ToArray<StealthAreaMarker>();
			List<IAgentOriginBase> list = new List<IAgentOriginBase>();
			for (int i = 0; i < this._priorAllyTroops.Count; i++)
			{
				if (array[i % count] == stealthAreaMarker)
				{
					list.Add(this._priorAllyTroops[i]);
				}
			}
			return list;
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x00027228 File Offset: 0x00025428
		public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
		{
			if (usedObject is StealthAreaUsePoint)
			{
				StealthAreaMissionLogic.StealthAreaData stealthAreaData = null;
				foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData2 in this._stealthAreaData)
				{
					if (stealthAreaData2.StealthAreaUsePoint == usedObject)
					{
						stealthAreaData = stealthAreaData2;
						break;
					}
				}
				if (stealthAreaData != null)
				{
					this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.CallTroopsCutSceneState;
					this._waitTimerToChangeStealthModeIntoBattle = new Timer(base.Mission.CurrentTime, 10f, true);
				}
			}
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x000272B4 File Offset: 0x000254B4
		public void SetOverriddenHideoutBossCharacterObject(CharacterObject characterObject)
		{
			this._overriddenHideoutBossCharacterObject = characterObject;
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x000272C0 File Offset: 0x000254C0
		private IAgentOriginBase GetOneEnemyTroopToSpawnInFirstPhase()
		{
			IAgentOriginBase agentOriginBase = null;
			foreach (IAgentOriginBase agentOriginBase2 in this._allEnemyTroops)
			{
				CharacterObject characterObject = (CharacterObject)agentOriginBase2.Troop;
				if (!characterObject.IsHero && characterObject.Culture.BanditBoss != agentOriginBase2.Troop && characterObject != this._overriddenHideoutBossCharacterObject)
				{
					agentOriginBase = agentOriginBase2;
					break;
				}
			}
			CharacterObject characterObject2 = (CharacterObject)agentOriginBase.Troop;
			PartyBase party = ((PartyAgentOrigin)agentOriginBase).Party;
			party.AddMember(characterObject2, 1, 0);
			return new PartyAgentOrigin(party, characterObject2, -1, default(UniqueTroopDescriptor), false, false);
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x0002737C File Offset: 0x0002557C
		public void SpawnRemainingTroopsForBossFight(List<MatrixFrame> spawnFrames)
		{
			int num = (int)MathF.Clamp((float)(this._initialHideoutPopulation / 2), 4f, 20f);
			this._allEnemyTroops = (from x in this._allEnemyTroops
				orderby x.Troop.IsHero descending
				select x).ToList<IAgentOriginBase>();
			if (this._overriddenHideoutBossCharacterObject != null)
			{
				IAgentOriginBase agentOriginBase = this._allEnemyTroops.Find((IAgentOriginBase t) => t.Troop == this._overriddenHideoutBossCharacterObject);
				MatrixFrame matrixFrame = spawnFrames.FirstOrDefault<MatrixFrame>();
				matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent = Mission.Current.SpawnTroop(agentOriginBase, false, false, false, false, 0, 0, false, false, false, new Vec3?(matrixFrame.origin), new Vec2?(matrixFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
				AgentFlag agentFlags = agent.GetAgentFlags();
				if (agentFlags.HasAnyFlag(AgentFlag.CanRetreat))
				{
					agent.SetAgentFlags(agentFlags & ~AgentFlag.CanRetreat);
				}
				this._allEnemyTroops.Remove(agentOriginBase);
			}
			foreach (IAgentOriginBase troopOrigin in this._allEnemyTroops)
			{
				MatrixFrame matrixFrame2 = spawnFrames.FirstOrDefault<MatrixFrame>();
				matrixFrame2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Agent agent2 = Mission.Current.SpawnTroop(troopOrigin, false, false, false, false, 0, 0, false, false, false, new Vec3?(matrixFrame2.origin), new Vec2?(matrixFrame2.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
				AgentFlag agentFlags2 = agent2.GetAgentFlags();
				if (agentFlags2.HasAnyFlag(AgentFlag.CanRetreat))
				{
					agent2.SetAgentFlags(agentFlags2 & ~AgentFlag.CanRetreat);
				}
				num--;
				if (num <= 0)
				{
					break;
				}
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

		// Token: 0x060005D6 RID: 1494 RVA: 0x000275F4 File Offset: 0x000257F4
		private void SpawnAllyAgent(IAgentOriginBase troopOrigin, GameEntity spawnPoint, Vec3 position)
		{
			MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
			Agent agent = Mission.Current.SpawnTroop(troopOrigin, true, false, false, false, 0, 0, true, true, true, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), null, null, FormationClass.NumberOfAllFormations, false);
			WorldPosition worldPosition = new WorldPosition(spawnPoint.Scene, position);
			agent.SetScriptedPosition(ref worldPosition, true, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.Crouch);
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x00027668 File Offset: 0x00025868
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("hideout_center");
			int num = 0;
			if (unusedUsablePointCount.TryGetValue("stealth_agent_forced", out num))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateForcedSentry), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Enemy, num);
			}
			if (unusedUsablePointCount.TryGetValue("stealth_agent", out num))
			{
				int num2 = this._initialHideoutPopulation / 8;
				if (num2 >= 1)
				{
					locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateSentry), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Enemy, Math.Min(num2, num));
				}
			}
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x000276FC File Offset: 0x000258FC
		private LocationCharacter CreateForcedSentry(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			IAgentOriginBase oneEnemyTroopToSpawnInFirstPhase = this.GetOneEnemyTroopToSpawnInFirstPhase();
			CharacterObject characterObject = (CharacterObject)oneEnemyTroopToSpawnInFirstPhase.Troop;
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(characterObject, out minValue, out maxValue, "");
			return new LocationCharacter(new AgentData(oneEnemyTroopToSpawnInFirstPhase).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors), "stealth_agent_forced", true, relation, null, false, false, null, false, false, true, null, true);
		}

		// Token: 0x060005D9 RID: 1497 RVA: 0x0002778C File Offset: 0x0002598C
		private LocationCharacter CreateSentry(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			IAgentOriginBase oneEnemyTroopToSpawnInFirstPhase = this.GetOneEnemyTroopToSpawnInFirstPhase();
			CharacterObject characterObject = (CharacterObject)oneEnemyTroopToSpawnInFirstPhase.Troop;
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(characterObject, out minValue, out maxValue, "");
			return new LocationCharacter(new AgentData(oneEnemyTroopToSpawnInFirstPhase).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors), "stealth_agent", true, relation, null, false, false, null, false, false, true, null, false);
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x0002781C File Offset: 0x00025A1C
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (this._waitTimerToChangeStealthModeIntoBattle != null && this._waitTimerToChangeStealthModeIntoBattle.Check(base.Mission.CurrentTime))
			{
				this.ChangeHideoutMissionModeToBattle();
				this._waitTimerToChangeStealthModeIntoBattle = null;
			}
			if (!this._isMissionInitialized)
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive())
				{
					this.InitializeMission();
					this._isMissionInitialized = true;
					return;
				}
			}
			if (this._isMissionInitialized)
			{
				if (!this._troopsInitialized)
				{
					this._troopsInitialized = true;
					foreach (Agent agent in base.Mission.Agents)
					{
						this._battleAgentLogic.OnAgentBuild(agent, null);
					}
				}
				if (!this._battleResolved)
				{
					this.CheckBattleResolved();
					return;
				}
				if (!base.Mission.ForceNoFriendlyFire)
				{
					base.Mission.ForceNoFriendlyFire = true;
				}
			}
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x00027918 File Offset: 0x00025B18
		private void InitializeMission()
		{
			base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(true);
			base.Mission.SetMissionMode(MissionMode.Stealth, true);
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.StealthState;
			base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
		}

		// Token: 0x060005DC RID: 1500 RVA: 0x00027950 File Offset: 0x00025B50
		private void ChangeHideoutMissionModeToBattle()
		{
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.BattleBeforeBossFight;
			Mission.Current.SetMissionMode(MissionMode.Battle, false);
			foreach (Agent agent in Mission.Current.PlayerTeam.ActiveAgents)
			{
				if (!agent.IsMainAgent)
				{
					agent.ClearTargetFrame();
					agent.DisableScriptedMovement();
				}
			}
			base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations(false);
			base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
			base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations(false);
			base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
			foreach (Agent agent2 in base.Mission.PlayerEnemyTeam.ActiveAgents)
			{
				agent2.SetAlarmState(Agent.AIStateFlag.Alarmed);
			}
			string eventFullName = "event:/ui/mission/horns/attack";
			Vec3 position = Agent.Main.Position;
			SoundManager.StartOneShotEvent(eventFullName, position);
		}

		// Token: 0x060005DD RID: 1501 RVA: 0x00027A88 File Offset: 0x00025C88
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (this._currentHideoutMissionState < HideoutAmbushMissionController.HideoutMissionState.CutSceneBeforeBossFight && agent.IsHuman && agent.Team == Mission.Current.PlayerEnemyTeam)
			{
				foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
				{
					foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
					{
						if (keyValuePair.Key.IsPositionInRange(agent.Position))
						{
							stealthAreaData.AddAgentToStealthAreaMarker(keyValuePair.Key, agent);
							break;
						}
					}
				}
			}
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x00027B60 File Offset: 0x00025D60
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (affectorAgent != null && affectorAgent.IsMainAgent)
			{
				this._remainingSentryCount = 0;
				foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
				{
					foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
					{
						if (keyValuePair.Value.Contains(affectedAgent) || keyValuePair.Value.IsEmpty<Agent>())
						{
							stealthAreaData.RemoveAgentFromStealthAreaMarker(keyValuePair.Key, affectedAgent);
						}
						this._remainingSentryCount += keyValuePair.Value.Count;
					}
				}
			}
			if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel)
			{
				using (List<Agent>.Enumerator enumerator3 = base.Mission.Agents.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						Agent agent = enumerator3.Current;
						if (agent != affectedAgent && agent != affectorAgent && agent.IsActive() && agent.GetLookAgent() == affectedAgent)
						{
							agent.SetLookAgent(null);
						}
					}
					return;
				}
			}
			if ((this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.StealthState || this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BattleBeforeBossFight) && affectedAgent.IsMainAgent)
			{
				base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations(false);
				affectedAgent.Formation = null;
				base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Retreat);
			}
		}

		// Token: 0x060005DF RID: 1503 RVA: 0x00027D04 File Offset: 0x00025F04
		public void OnStealthMissionCounterFailed(OnStealthMissionCounterFailedEvent obj)
		{
			Campaign.Current.GameMenuManager.SetNextMenu("hideout_after_found_by_sentries");
		}

		// Token: 0x060005E0 RID: 1504 RVA: 0x00027D1C File Offset: 0x00025F1C
		private void CheckBattleResolved()
		{
			if (this._currentHideoutMissionState != HideoutAmbushMissionController.HideoutMissionState.NotDecided && this._currentHideoutMissionState != HideoutAmbushMissionController.HideoutMissionState.CutSceneBeforeBossFight && this._currentHideoutMissionState != HideoutAmbushMissionController.HideoutMissionState.ConversationBetweenLeaders)
			{
				if (this.IsSideDepleted(base.Mission.PlayerTeam.Side))
				{
					if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel)
					{
						this.OnDuelOver(base.Mission.PlayerEnemyTeam.Side);
					}
					Campaign.Current.SkillLevelingManager.OnHideoutMissionEnd(false);
					this._battleEndLogic.ChangeCanCheckForEndCondition(true);
					this._battleResolved = true;
					return;
				}
				if (this.IsSideDepleted(base.Mission.PlayerEnemyTeam.Side))
				{
					if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BattleBeforeBossFight || this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.StealthState)
					{
						Agent main = Agent.Main;
						if (main != null && main.IsActive())
						{
							if (this._firstPhaseEndTimer == null)
							{
								this._firstPhaseEndTimer = new Timer(base.Mission.CurrentTime, 4f, true);
								Mission.Current.SetMissionMode(MissionMode.CutScene, false);
								return;
							}
							if (this._firstPhaseEndTimer.Check(base.Mission.CurrentTime))
							{
								this._hideoutAmbushBossFightCinematicController.StartCinematic(new HideoutAmbushBossFightCinematicController.OnInitialFadeOutFinished(this.OnInitialFadeOutOver), new Action(this.OnCutSceneOver), 0.4f, 0.2f, 8f, false);
								return;
							}
						}
					}
					else
					{
						if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel)
						{
							this.OnDuelOver(base.Mission.PlayerTeam.Side);
						}
						Campaign.Current.SkillLevelingManager.OnHideoutMissionEnd(true);
						this._battleEndLogic.ChangeCanCheckForEndCondition(true);
						MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerTeam.Side);
						this._battleResolved = true;
					}
				}
			}
		}

		// Token: 0x060005E1 RID: 1505 RVA: 0x00027ECC File Offset: 0x000260CC
		public bool IsSideDepleted(BattleSideEnum side)
		{
			bool flag = ((side == BattleSideEnum.Attacker) ? Mission.Current.Teams.Attacker : Mission.Current.Teams.Defender).ActiveAgents.Count == 0;
			if (!flag)
			{
				if (this._playerSide == side)
				{
					if (Agent.Main == null || !Agent.Main.IsActive())
					{
						if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel || this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BattleBeforeBossFight)
						{
							flag = true;
						}
						else if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithAll)
						{
							flag = base.Mission.PlayerTeam.ActiveAgents.IsEmpty<Agent>() && !base.Mission.PlayerEnemyTeam.ActiveAgents.IsEmpty<Agent>();
						}
					}
				}
				else if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel && (this._bossAgent == null || !this._bossAgent.IsActive()))
				{
					flag = true;
				}
			}
			return flag;
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x00027FA4 File Offset: 0x000261A4
		public void OnAgentsShouldBeEnabled()
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent.IsActive() && agent.IsAIControlled)
				{
					agent.SetIsAIPaused(false);
				}
			}
		}

		// Token: 0x060005E3 RID: 1507 RVA: 0x0002800C File Offset: 0x0002620C
		protected override void OnEndMission()
		{
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			int num = 0;
			if (this._currentHideoutMissionState == HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel)
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
			if (!PlayerEncounter.EnemySurrender && num <= 0 && MobileParty.MainParty.MemberRoster.TotalHealthyCount <= 0 && MapEvent.PlayerMapEvent.BattleState == BattleState.None)
			{
				MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerEnemyTeam.Side);
			}
			Game.Current.EventManager.UnregisterEvent<OnStealthMissionCounterFailedEvent>(new Action<OnStealthMissionCounterFailedEvent>(this.OnStealthMissionCounterFailed));
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x000280D0 File Offset: 0x000262D0
		private void SpawnBossAndBodyguards()
		{
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = Agent.Main.Position + Agent.Main.LookDirection * -3f;
			this.SpawnRemainingTroopsForBossFight(new List<MatrixFrame> { identity });
			this._bossAgent = this.SelectBossAgent();
			this._bossAgent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			foreach (Agent agent in base.Mission.PlayerEnemyTeam.ActiveAgents)
			{
				if (agent != this._bossAgent)
				{
					agent.WieldInitialWeapons(Agent.WeaponWieldActionType.WithAnimationUninterruptible, Equipment.InitialWeaponEquipPreference.Any);
				}
			}
		}

		// Token: 0x060005E5 RID: 1509 RVA: 0x00028194 File Offset: 0x00026394
		private Agent SelectBossAgent()
		{
			Agent agent = null;
			Agent agent2 = null;
			foreach (Agent agent3 in base.Mission.Agents)
			{
				if (agent3.IsHuman && !agent3.Team.IsPlayerAlly)
				{
					if (this._overriddenHideoutBossCharacterObject == null)
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
					}
					else if (agent3.Character == this._overriddenHideoutBossCharacterObject)
					{
						agent = agent3;
						agent2 = agent3;
						break;
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

		// Token: 0x060005E6 RID: 1510 RVA: 0x000282B0 File Offset: 0x000264B0
		private void OnInitialFadeOutOver(ref Agent playerAgent, ref List<Agent> playerCompanions, ref Agent bossAgent, ref List<Agent> bossCompanions, ref float placementPerturbation, ref float placementAngle)
		{
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.CutSceneBeforeBossFight;
			this._enemyTeam = base.Mission.PlayerEnemyTeam;
			this.SpawnBossAndBodyguards();
			base.Mission.PlayerTeam.SetIsEnemyOf(this._enemyTeam, false);
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

		// Token: 0x060005E7 RID: 1511 RVA: 0x00028360 File Offset: 0x00026560
		private void OnCutSceneOver()
		{
			Mission.Current.SetMissionMode(MissionMode.Battle, false);
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.ConversationBetweenLeaders;
			MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
			missionBehavior.DisableStartConversation(false);
			missionBehavior.StartConversation(this._bossAgent, false, false);
		}

		// Token: 0x060005E8 RID: 1512 RVA: 0x00028394 File Offset: 0x00026594
		private void OnDuelOver(BattleSideEnum winnerSide)
		{
			if (winnerSide == base.Mission.PlayerTeam.Side && this._duelPhaseAllyAgents != null)
			{
				using (List<Agent>.Enumerator enumerator = this._duelPhaseAllyAgents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Agent agent = enumerator.Current;
						if (agent.State == AgentState.Active)
						{
							agent.SetTeam(base.Mission.PlayerTeam, true);
						}
					}
					return;
				}
			}
			if (winnerSide == base.Mission.PlayerEnemyTeam.Side && this._duelPhaseBanditAgents != null)
			{
				foreach (Agent agent2 in this._duelPhaseBanditAgents)
				{
					if (agent2.State == AgentState.Active)
					{
						agent2.SetTeam(this._enemyTeam, true);
						agent2.DisableScriptedMovement();
						agent2.ClearTargetFrame();
					}
				}
				foreach (Agent agent3 in this._duelPhaseAllyAgents)
				{
					if (agent3.State == AgentState.Active)
					{
						agent3.SetTeam(base.Mission.PlayerTeam, true);
						agent3.DisableScriptedMovement();
						agent3.ClearTargetFrame();
					}
				}
				foreach (Agent agent4 in base.Mission.PlayerEnemyTeam.ActiveAgents)
				{
					agent4.SetAlarmState(Agent.AIStateFlag.Alarmed);
				}
			}
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x00028544 File Offset: 0x00026744
		public static void StartBossFightDuelMode()
		{
			Mission mission = Mission.Current;
			HideoutAmbushMissionController hideoutAmbushMissionController = ((mission != null) ? mission.GetMissionBehavior<HideoutAmbushMissionController>() : null);
			if (hideoutAmbushMissionController == null)
			{
				return;
			}
			hideoutAmbushMissionController.StartBossFightDuelModeInternal();
		}

		// Token: 0x060005EA RID: 1514 RVA: 0x00028564 File Offset: 0x00026764
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
			this._bossAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.BossFightWithDuel;
		}

		// Token: 0x060005EB RID: 1515 RVA: 0x000286C4 File Offset: 0x000268C4
		public static void StartBossFightBattleMode()
		{
			Mission mission = Mission.Current;
			HideoutAmbushMissionController hideoutAmbushMissionController = ((mission != null) ? mission.GetMissionBehavior<HideoutAmbushMissionController>() : null);
			if (hideoutAmbushMissionController == null)
			{
				return;
			}
			hideoutAmbushMissionController.StartBossFightBattleModeInternal();
		}

		// Token: 0x060005EC RID: 1516 RVA: 0x000286E4 File Offset: 0x000268E4
		private void StartBossFightBattleModeInternal()
		{
			base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(true);
			base.Mission.PlayerTeam.SetIsEnemyOf(this._enemyTeam, true);
			this._currentHideoutMissionState = HideoutAmbushMissionController.HideoutMissionState.BossFightWithAll;
			foreach (Agent agent in base.Mission.PlayerEnemyTeam.ActiveAgents)
			{
				agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			}
			base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations(false);
			base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
			base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations(false);
			base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x000287C8 File Offset: 0x000269C8
		private void KillAllSentries()
		{
			List<Agent> list = new List<Agent>();
			foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
			{
				foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
				{
					list.AddRange(keyValuePair.Value);
				}
			}
			foreach (Agent agent in list)
			{
				base.Mission.KillAgentCheat(agent);
			}
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x000288A8 File Offset: 0x00026AA8
		[CommandLineFunctionality.CommandLineArgumentFunction("kill_all_sentries", "mission")]
		public static string KillAllSentries(List<string> strings)
		{
			string empty = string.Empty;
			if (!CampaignCheats.CheckCheatUsage(ref empty))
			{
				return empty;
			}
			Mission mission = Mission.Current;
			HideoutAmbushMissionController hideoutAmbushMissionController = ((mission != null) ? mission.GetMissionBehavior<HideoutAmbushMissionController>() : null);
			if (hideoutAmbushMissionController != null)
			{
				hideoutAmbushMissionController.KillAllSentries();
				return "Done";
			}
			return "This cheat only works in hideout ambush mission!";
		}

		// Token: 0x0400030C RID: 780
		private const int FirstPhaseEndInSeconds = 4;

		// Token: 0x0400030D RID: 781
		private int _initialHideoutPopulation;

		// Token: 0x0400030E RID: 782
		private bool _troopsInitialized;

		// Token: 0x0400030F RID: 783
		private bool _isMissionInitialized;

		// Token: 0x04000310 RID: 784
		private bool _battleResolved;

		// Token: 0x04000311 RID: 785
		private readonly BattleSideEnum _playerSide;

		// Token: 0x04000312 RID: 786
		private HideoutAmbushMissionController.HideoutMissionState _currentHideoutMissionState;

		// Token: 0x04000313 RID: 787
		private List<Agent> _duelPhaseAllyAgents;

		// Token: 0x04000314 RID: 788
		private List<Agent> _duelPhaseBanditAgents;

		// Token: 0x04000315 RID: 789
		private List<IAgentOriginBase> _allEnemyTroops;

		// Token: 0x04000316 RID: 790
		private List<IAgentOriginBase> _priorAllyTroops;

		// Token: 0x04000317 RID: 791
		private List<StealthAreaMissionLogic.StealthAreaData> _stealthAreaData;

		// Token: 0x04000318 RID: 792
		private Timer _waitTimerToChangeStealthModeIntoBattle;

		// Token: 0x04000319 RID: 793
		private Timer _firstPhaseEndTimer;

		// Token: 0x0400031A RID: 794
		private int _sentryCount;

		// Token: 0x0400031B RID: 795
		private int _remainingSentryCount;

		// Token: 0x0400031C RID: 796
		private BattleAgentLogic _battleAgentLogic;

		// Token: 0x0400031D RID: 797
		private BattleEndLogic _battleEndLogic;

		// Token: 0x0400031E RID: 798
		private HideoutAmbushBossFightCinematicController _hideoutAmbushBossFightCinematicController;

		// Token: 0x0400031F RID: 799
		private StealthAreaMissionLogic _stealthAreaMissionLogic;

		// Token: 0x04000320 RID: 800
		private Agent _bossAgent;

		// Token: 0x04000321 RID: 801
		private Team _enemyTeam;

		// Token: 0x04000322 RID: 802
		private CharacterObject _overriddenHideoutBossCharacterObject;

		// Token: 0x02000192 RID: 402
		public class TroopData
		{
			// Token: 0x06000EC2 RID: 3778 RVA: 0x00065E01 File Offset: 0x00064001
			public TroopData(CharacterObject troop, int number)
			{
				this.Troop = troop;
				this.Number = number;
			}

			// Token: 0x0400077D RID: 1917
			public CharacterObject Troop;

			// Token: 0x0400077E RID: 1918
			public int Number;

			// Token: 0x0400077F RID: 1919
			public int Level;
		}

		// Token: 0x02000193 RID: 403
		private enum HideoutMissionState
		{
			// Token: 0x04000781 RID: 1921
			NotDecided,
			// Token: 0x04000782 RID: 1922
			StealthState,
			// Token: 0x04000783 RID: 1923
			CallTroopsCutSceneState,
			// Token: 0x04000784 RID: 1924
			BattleBeforeBossFight,
			// Token: 0x04000785 RID: 1925
			CutSceneBeforeBossFight,
			// Token: 0x04000786 RID: 1926
			ConversationBetweenLeaders,
			// Token: 0x04000787 RID: 1927
			BossFightWithDuel,
			// Token: 0x04000788 RID: 1928
			BossFightWithAll
		}
	}
}
