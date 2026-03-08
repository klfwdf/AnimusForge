using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Tournaments.AgentControllers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics
{
	// Token: 0x0200002F RID: 47
	public class TournamentJoustingMissionController : MissionLogic, ITournamentGameBehavior
	{
		// Token: 0x14000003 RID: 3
		// (add) Token: 0x060001A3 RID: 419 RVA: 0x0000A8F8 File Offset: 0x00008AF8
		// (remove) Token: 0x060001A4 RID: 420 RVA: 0x0000A930 File Offset: 0x00008B30
		public event TournamentJoustingMissionController.JoustingEventDelegate VictoryAchieved;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x060001A5 RID: 421 RVA: 0x0000A968 File Offset: 0x00008B68
		// (remove) Token: 0x060001A6 RID: 422 RVA: 0x0000A9A0 File Offset: 0x00008BA0
		public event TournamentJoustingMissionController.JoustingEventDelegate PointGanied;

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x060001A7 RID: 423 RVA: 0x0000A9D8 File Offset: 0x00008BD8
		// (remove) Token: 0x060001A8 RID: 424 RVA: 0x0000AA10 File Offset: 0x00008C10
		public event TournamentJoustingMissionController.JoustingEventDelegate Disqualified;

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x060001A9 RID: 425 RVA: 0x0000AA48 File Offset: 0x00008C48
		// (remove) Token: 0x060001AA RID: 426 RVA: 0x0000AA80 File Offset: 0x00008C80
		public event TournamentJoustingMissionController.JoustingEventDelegate Unconscious;

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x060001AB RID: 427 RVA: 0x0000AAB8 File Offset: 0x00008CB8
		// (remove) Token: 0x060001AC RID: 428 RVA: 0x0000AAF0 File Offset: 0x00008CF0
		public event TournamentJoustingMissionController.JoustingAgentStateChangedEventDelegate AgentStateChanged;

		// Token: 0x060001AD RID: 429 RVA: 0x0000AB28 File Offset: 0x00008D28
		public TournamentJoustingMissionController(CultureObject culture)
		{
			this._culture = culture;
			this._match = null;
			this.RegionBoxList = new List<GameEntity>(2);
			this.RegionExitBoxList = new List<GameEntity>(2);
			this.CornerBackStartList = new List<MatrixFrame>();
			this.CornerStartList = new List<GameEntity>(2);
			this.CornerMiddleList = new List<MatrixFrame>();
			this.CornerFinishList = new List<MatrixFrame>();
			this.IsSwordDuelStarted = false;
			this._joustingEquipment = new Equipment();
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("charger"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("horse_harness_e"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("vlandia_lance_2_t4"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("leather_round_shield"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("desert_lamellar"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("nasal_helmet_with_mail"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("reinforced_mail_mitten"), null, null, false));
			this._joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("leather_cavalier_boots"), null, null, false));
		}

		// Token: 0x060001AE RID: 430 RVA: 0x0000ACE4 File Offset: 0x00008EE4
		public override void AfterStart()
		{
			TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_jousting"));
			for (int i = 0; i < 2; i++)
			{
				GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_jousting_back_" + i);
				GameEntity item = base.Mission.Scene.FindEntityWithTag("sp_jousting_start_" + i);
				GameEntity gameEntity2 = base.Mission.Scene.FindEntityWithTag("sp_jousting_middle_" + i);
				GameEntity gameEntity3 = base.Mission.Scene.FindEntityWithTag("sp_jousting_finish_" + i);
				this.CornerBackStartList.Add(gameEntity.GetGlobalFrame());
				this.CornerStartList.Add(item);
				this.CornerMiddleList.Add(gameEntity2.GetGlobalFrame());
				this.CornerFinishList.Add(gameEntity3.GetGlobalFrame());
			}
			GameEntity item2 = base.Mission.Scene.FindEntityWithName("region_box_0");
			this.RegionBoxList.Add(item2);
			GameEntity item3 = base.Mission.Scene.FindEntityWithName("region_box_1");
			this.RegionBoxList.Add(item3);
			GameEntity item4 = base.Mission.Scene.FindEntityWithName("region_end_box_0");
			this.RegionExitBoxList.Add(item4);
			GameEntity item5 = base.Mission.Scene.FindEntityWithName("region_end_box_1");
			this.RegionExitBoxList.Add(item5);
			base.Mission.SetMissionMode(MissionMode.Battle, true);
		}

		// Token: 0x060001AF RID: 431 RVA: 0x0000AE88 File Offset: 0x00009088
		public void StartMatch(TournamentMatch match, bool isLastRound)
		{
			this._match = match;
			int num = 0;
			foreach (TournamentTeam tournamentTeam in this._match.Teams)
			{
				Team team = base.Mission.Teams.Add(BattleSideEnum.None, uint.MaxValue, uint.MaxValue, null, true, false, true);
				foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
				{
					tournamentParticipant.MatchEquipment = this._joustingEquipment.Clone(false);
					this.SetItemsAndSpawnCharacter(tournamentParticipant, team, num);
				}
				num++;
			}
			List<Team> list = base.Mission.Teams.ToList<Team>();
			for (int i = 0; i < list.Count; i++)
			{
				for (int j = i + 1; j < list.Count; j++)
				{
					list[i].SetIsEnemyOf(list[j], true);
				}
			}
			base.Mission.Scene.SetAbilityOfFacesWithId(1, false);
			base.Mission.Scene.SetAbilityOfFacesWithId(2, false);
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x0000AFC8 File Offset: 0x000091C8
		public void SkipMatch(TournamentMatch match)
		{
			this._match = match;
			this.Simulate();
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x0000AFD8 File Offset: 0x000091D8
		public bool IsMatchEnded()
		{
			if (this._isSimulated || this._match == null)
			{
				return true;
			}
			if (this._endTimer != null && this._endTimer.ElapsedTime > 6f)
			{
				this._endTimer = null;
				return true;
			}
			if (this._endTimer == null && this._winnerTeam != null)
			{
				this._endTimer = new BasicMissionTimer();
			}
			return false;
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x0000B038 File Offset: 0x00009238
		public void OnMatchEnded()
		{
			SandBoxHelpers.MissionHelper.FadeOutAgents(base.Mission.Agents, true, false);
			base.Mission.ClearCorpses(false);
			base.Mission.Teams.Clear();
			base.Mission.RemoveSpawnedItemsAndMissiles();
			this._match = null;
			this._endTimer = null;
			this._isSimulated = false;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x0000B094 File Offset: 0x00009294
		private void Simulate()
		{
			this._isSimulated = false;
			List<TournamentParticipant> participants = this._match.Participants.ToList<TournamentParticipant>();
			while (participants.Count > 1 && participants.Any((TournamentParticipant x) => x.Team != participants[0].Team))
			{
				if (participants.Any((TournamentParticipant x) => x.Score >= 3))
				{
					break;
				}
				TournamentParticipant tournamentParticipant = participants[MBRandom.RandomInt(participants.Count)];
				TournamentParticipant tournamentParticipant2 = participants[MBRandom.RandomInt(participants.Count)];
				while (tournamentParticipant == tournamentParticipant2 || tournamentParticipant.Team == tournamentParticipant2.Team)
				{
					tournamentParticipant2 = participants[MBRandom.RandomInt(participants.Count)];
				}
				tournamentParticipant.AddScore(1);
			}
			this._isSimulated = true;
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x0000B194 File Offset: 0x00009394
		private void SetItemsAndSpawnCharacter(TournamentParticipant participant, Team team, int cornerIndex)
		{
			AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(participant.Character, -1, null, participant.Descriptor)).Team(team).InitialFrameFromSpawnPointEntity(this.CornerStartList[cornerIndex]).Equipment(participant.MatchEquipment)
				.Controller(participant.Character.IsPlayerCharacter ? AgentControllerType.Player : AgentControllerType.AI);
			Agent agent = base.Mission.SpawnAgent(agentBuildData, false);
			agent.Health = agent.HealthLimit;
			this.AddJoustingAgentController(agent);
			agent.GetController<JoustingAgentController>().CurrentCornerIndex = cornerIndex;
			if (participant.Character.IsPlayerCharacter)
			{
				agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
				base.Mission.PlayerTeam = team;
				return;
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x0000B248 File Offset: 0x00009448
		private void AddJoustingAgentController(Agent agent)
		{
			agent.AddController(typeof(JoustingAgentController));
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x0000B25C File Offset: 0x0000945C
		public bool IsAgentInTheTrack(Agent agent, bool inCurrentTrack = true)
		{
			bool result = false;
			if (agent != null)
			{
				JoustingAgentController controller = agent.GetController<JoustingAgentController>();
				int index = (inCurrentTrack ? controller.CurrentCornerIndex : (1 - controller.CurrentCornerIndex));
				result = this.RegionBoxList[index].CheckPointWithOrientedBoundingBox(agent.Position);
			}
			return result;
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x0000B2A4 File Offset: 0x000094A4
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (!base.Mission.IsMissionEnding)
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					JoustingAgentController controller = agent.GetController<JoustingAgentController>();
					if (controller != null)
					{
						controller.UpdateState();
					}
				}
				this.CheckStartOfSwordDuel();
			}
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x0000B320 File Offset: 0x00009520
		private void CheckStartOfSwordDuel()
		{
			if (!base.Mission.IsMissionEnding)
			{
				if (!this.IsSwordDuelStarted)
				{
					if (base.Mission.Agents.Count <= 0)
					{
						return;
					}
					if (base.Mission.Agents.Count((Agent a) => a.IsMount) >= 2)
					{
						return;
					}
					this.IsSwordDuelStarted = true;
					this.RemoveBarriers();
					base.Mission.Scene.SetAbilityOfFacesWithId(2, true);
					using (List<Agent>.Enumerator enumerator = base.Mission.Agents.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							Agent agent = enumerator.Current;
							if (agent.IsHuman)
							{
								JoustingAgentController controller = agent.GetController<JoustingAgentController>();
								controller.State = JoustingAgentController.JoustingAgentState.SwordDuel;
								controller.PrepareAgentToSwordDuel();
							}
						}
						return;
					}
				}
				foreach (Agent agent2 in base.Mission.Agents)
				{
					if (agent2.IsHuman)
					{
						JoustingAgentController controller2 = agent2.GetController<JoustingAgentController>();
						controller2.State = JoustingAgentController.JoustingAgentState.SwordDuel;
						if (controller2.PrepareEquipmentsAfterDismount && agent2.MountAgent == null)
						{
							CharacterObject characterObject = (CharacterObject)agent2.Character;
							controller2.PrepareEquipmentsForSwordDuel();
							agent2.DisableScriptedMovement();
							if (characterObject == CharacterObject.PlayerCharacter)
							{
								agent2.Controller = AgentControllerType.Player;
							}
						}
					}
				}
			}
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x0000B4A0 File Offset: 0x000096A0
		private void RemoveBarriers()
		{
			foreach (GameEntity gameEntity in base.Mission.Scene.FindEntitiesWithTag("jousting_barrier").ToList<GameEntity>())
			{
				gameEntity.Remove(95);
			}
		}

		// Token: 0x060001BA RID: 442 RVA: 0x0000B508 File Offset: 0x00009708
		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			if (!base.Mission.IsMissionEnding && !this.IsSwordDuelStarted && affectedAgent.IsHuman && affectorAgent != null && affectorAgent.IsHuman && affectedAgent != affectorAgent)
			{
				JoustingAgentController controller = affectorAgent.GetController<JoustingAgentController>();
				JoustingAgentController controller2 = affectedAgent.GetController<JoustingAgentController>();
				if (this.IsAgentInTheTrack(affectorAgent, true) && controller2.IsRiding() && controller.IsRiding())
				{
					this._match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(1);
					controller.Score++;
					if (controller.Score >= 3)
					{
						this._winnerTeam = affectorAgent.Team;
						TournamentJoustingMissionController.JoustingEventDelegate victoryAchieved = this.VictoryAchieved;
						if (victoryAchieved == null)
						{
							return;
						}
						victoryAchieved(affectorAgent, affectedAgent);
						return;
					}
					else
					{
						TournamentJoustingMissionController.JoustingEventDelegate pointGanied = this.PointGanied;
						if (pointGanied == null)
						{
							return;
						}
						pointGanied(affectorAgent, affectedAgent);
						return;
					}
				}
				else
				{
					this._match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(-100);
					this._winnerTeam = affectedAgent.Team;
					MBTextManager.SetTextVariable("OPPONENT_GENDER", affectorAgent.Character.IsFemale ? "0" : "1", false);
					TournamentJoustingMissionController.JoustingEventDelegate disqualified = this.Disqualified;
					if (disqualified == null)
					{
						return;
					}
					disqualified(affectorAgent, affectedAgent);
				}
			}
		}

		// Token: 0x060001BB RID: 443 RVA: 0x0000B644 File Offset: 0x00009844
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (!base.Mission.IsMissionEnding && affectedAgent.IsHuman && affectorAgent != null && affectorAgent.IsHuman && affectedAgent != affectorAgent)
			{
				if (this.IsAgentInTheTrack(affectorAgent, true) || this.IsSwordDuelStarted)
				{
					this._match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(100);
					this._winnerTeam = affectorAgent.Team;
					if (this.Unconscious != null)
					{
						this.Unconscious(affectorAgent, affectedAgent);
						return;
					}
				}
				else
				{
					this._match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(-100);
					this._winnerTeam = affectedAgent.Team;
					MBTextManager.SetTextVariable("OPPONENT_GENDER", affectorAgent.Character.IsFemale ? "0" : "1", false);
					if (this.Disqualified != null)
					{
						this.Disqualified(affectorAgent, affectedAgent);
					}
				}
			}
		}

		// Token: 0x060001BC RID: 444 RVA: 0x0000B739 File Offset: 0x00009939
		public void OnJoustingAgentStateChanged(Agent agent, JoustingAgentController.JoustingAgentState state)
		{
			if (this.AgentStateChanged != null)
			{
				this.AgentStateChanged(agent, state);
			}
		}

		// Token: 0x04000091 RID: 145
		private Team _winnerTeam;

		// Token: 0x04000092 RID: 146
		public List<GameEntity> RegionBoxList;

		// Token: 0x04000093 RID: 147
		public List<GameEntity> RegionExitBoxList;

		// Token: 0x04000094 RID: 148
		public List<MatrixFrame> CornerBackStartList;

		// Token: 0x04000095 RID: 149
		public List<GameEntity> CornerStartList;

		// Token: 0x04000096 RID: 150
		public List<MatrixFrame> CornerMiddleList;

		// Token: 0x04000097 RID: 151
		public List<MatrixFrame> CornerFinishList;

		// Token: 0x04000098 RID: 152
		public bool IsSwordDuelStarted;

		// Token: 0x04000099 RID: 153
		private TournamentMatch _match;

		// Token: 0x0400009A RID: 154
		private BasicMissionTimer _endTimer;

		// Token: 0x0400009B RID: 155
		private bool _isSimulated;

		// Token: 0x0400009C RID: 156
		private CultureObject _culture;

		// Token: 0x0400009D RID: 157
		private readonly Equipment _joustingEquipment;

		// Token: 0x0200013B RID: 315
		// (Invoke) Token: 0x06000DD2 RID: 3538
		public delegate void JoustingEventDelegate(Agent affectedAgent, Agent affectorAgent);

		// Token: 0x0200013C RID: 316
		// (Invoke) Token: 0x06000DD6 RID: 3542
		public delegate void JoustingAgentStateChangedEventDelegate(Agent agent, JoustingAgentController.JoustingAgentState state);
	}
}
