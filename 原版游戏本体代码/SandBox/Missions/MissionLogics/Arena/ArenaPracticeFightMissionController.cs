using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena
{
	// Token: 0x02000098 RID: 152
	public class ArenaPracticeFightMissionController : MissionLogic
	{
		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000641 RID: 1601 RVA: 0x0002AC31 File Offset: 0x00028E31
		private int AISpawnIndex
		{
			get
			{
				return this._spawnedOpponentAgentCount;
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x06000642 RID: 1602 RVA: 0x0002AC39 File Offset: 0x00028E39
		// (set) Token: 0x06000643 RID: 1603 RVA: 0x0002AC41 File Offset: 0x00028E41
		public int RemainingOpponentCountFromLastPractice { get; private set; }

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000644 RID: 1604 RVA: 0x0002AC4A File Offset: 0x00028E4A
		// (set) Token: 0x06000645 RID: 1605 RVA: 0x0002AC52 File Offset: 0x00028E52
		public bool IsPlayerPracticing { get; private set; }

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000646 RID: 1606 RVA: 0x0002AC5B File Offset: 0x00028E5B
		// (set) Token: 0x06000647 RID: 1607 RVA: 0x0002AC63 File Offset: 0x00028E63
		public int OpponentCountBeatenByPlayer { get; private set; }

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000648 RID: 1608 RVA: 0x0002AC6C File Offset: 0x00028E6C
		public int RemainingOpponentCount
		{
			get
			{
				return 30 - this._spawnedOpponentAgentCount + this._aliveOpponentCount;
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000649 RID: 1609 RVA: 0x0002AC7E File Offset: 0x00028E7E
		// (set) Token: 0x0600064A RID: 1610 RVA: 0x0002AC86 File Offset: 0x00028E86
		public bool IsPlayerSurvived { get; private set; }

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x0600064B RID: 1611 RVA: 0x0002AC8F File Offset: 0x00028E8F
		// (set) Token: 0x0600064C RID: 1612 RVA: 0x0002AC97 File Offset: 0x00028E97
		public bool AfterPractice { get; set; }

		// Token: 0x0600064D RID: 1613 RVA: 0x0002ACA0 File Offset: 0x00028EA0
		public override void AfterStart()
		{
			this._settlement = PlayerEncounter.LocationEncounter.Settlement;
			this.InitializeTeams();
			GameEntity item = base.Mission.Scene.FindEntityWithTag("tournament_practice") ?? base.Mission.Scene.FindEntityWithTag("tournament_fight");
			List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("arena_set").ToList<GameEntity>();
			list.Remove(item);
			foreach (GameEntity gameEntity in list)
			{
				gameEntity.Remove(88);
			}
			this._initialSpawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena")
				select e.GetGlobalFrame()).ToList<MatrixFrame>();
			this._spawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena_respawn")
				select e.GetGlobalFrame()).ToList<MatrixFrame>();
			for (int i = 0; i < this._initialSpawnFrames.Count; i++)
			{
				MatrixFrame value = this._initialSpawnFrames[i];
				value.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				this._initialSpawnFrames[i] = value;
			}
			for (int j = 0; j < this._spawnFrames.Count; j++)
			{
				MatrixFrame value2 = this._spawnFrames[j];
				value2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				this._spawnFrames[j] = value2;
			}
			this.IsPlayerPracticing = false;
			this._participantAgents = new List<Agent>();
			this.StartPractice();
			MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			SandBoxHelpers.MissionHelper.SpawnPlayer(true, true, false, false, "");
			missionBehavior.SpawnLocationCharacters(null);
		}

		// Token: 0x0600064E RID: 1614 RVA: 0x0002AE94 File Offset: 0x00029094
		private void SpawnPlayerNearTournamentMaster()
		{
			GameEntity entity = base.Mission.Scene.FindEntityWithTag("sp_player_near_arena_master");
			base.Mission.SpawnAgent(new AgentBuildData(CharacterObject.PlayerCharacter).Team(base.Mission.PlayerTeam).InitialFrameFromSpawnPointEntity(entity).NoHorses(true)
				.CivilianEquipment(true)
				.TroopOrigin(new SimpleAgentOrigin(CharacterObject.PlayerCharacter, -1, null, default(UniqueTroopDescriptor)))
				.Controller(AgentControllerType.Player), false);
			Mission.Current.SetMissionMode(MissionMode.StartUp, false);
		}

		// Token: 0x0600064F RID: 1615 RVA: 0x0002AF1C File Offset: 0x0002911C
		private Agent SpawnArenaAgent(Team team, MatrixFrame frame)
		{
			CharacterObject characterObject;
			int spawnIndex;
			if (team == base.Mission.PlayerTeam)
			{
				characterObject = CharacterObject.PlayerCharacter;
				spawnIndex = 0;
			}
			else
			{
				characterObject = this._participantCharacters[this.AISpawnIndex];
				spawnIndex = this.AISpawnIndex;
			}
			Equipment equipment = new Equipment();
			this.AddRandomWeapons(equipment, spawnIndex);
			this.AddRandomClothes(characterObject, equipment);
			Mission mission = base.Mission;
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).InitialPosition(frame.origin);
			Vec2 vec = frame.rotation.f.AsVec2;
			vec = vec.Normalized();
			Agent agent = mission.SpawnAgent(agentBuildData.InitialDirection(vec).NoHorses(true).Equipment(equipment)
				.TroopOrigin(new SimpleAgentOrigin(characterObject, -1, null, default(UniqueTroopDescriptor)))
				.Controller((characterObject == CharacterObject.PlayerCharacter) ? AgentControllerType.Player : AgentControllerType.AI), false);
			agent.FadeIn();
			if (characterObject != CharacterObject.PlayerCharacter)
			{
				this._aliveOpponentCount++;
				this._spawnedOpponentAgentCount++;
			}
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			return agent;
		}

		// Token: 0x06000650 RID: 1616 RVA: 0x0002B028 File Offset: 0x00029228
		public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
		{
			if (affectorAgent == null)
			{
				return;
			}
			if (affectorAgent.IsMount && affectorAgent.RiderAgent != null)
			{
				affectorAgent = affectorAgent.RiderAgent;
			}
			if (affectorAgent.Character == null || affectedAgent.Character == null)
			{
				return;
			}
			float num = (float)blow.InflictedDamage;
			if (num > affectedAgent.HealthLimit)
			{
				num = affectedAgent.HealthLimit;
			}
			float num2 = num / affectedAgent.HealthLimit;
			this.EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, blow.AttackType, 0.5f * num2, num, collisionData.IsSneakAttack);
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x0002B0AC File Offset: 0x000292AC
		private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData attackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
		{
			CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
			CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;
			if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
			{
				bool flag = affectorAgent.MountAgent != null;
				bool isHorseCharge = flag && attackType == AgentAttackType.Collision;
				SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus, lastShotDifficulty, attackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.PracticeFight, flag, affectorAgent.Team == affectedAgent.Team, false, damageAmount, affectedAgent.Health < 1f, false, isHorseCharge, isSneakAttack);
			}
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x0002B130 File Offset: 0x00029330
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (this._aliveOpponentCount < 6 && this._spawnedOpponentAgentCount < 30 && (this._aliveOpponentCount == 2 || this._nextSpawnTime < base.Mission.CurrentTime))
			{
				Team team = this.SelectRandomAiTeam();
				Agent item = this.SpawnArenaAgent(team, this.GetSpawnFrame(true, false));
				this._participantAgents.Add(item);
				this._nextSpawnTime = base.Mission.CurrentTime + 14f - (float)this._spawnedOpponentAgentCount / 3f;
				if (this._spawnedOpponentAgentCount == 30 && !this.IsPlayerPracticing)
				{
					this._spawnedOpponentAgentCount = 0;
				}
			}
			if (this._teleportTimer == null && this.IsPlayerPracticing && this.CheckPracticeEndedForPlayer())
			{
				this._teleportTimer = new BasicMissionTimer();
				this.IsPlayerSurvived = base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive();
				if (this.IsPlayerSurvived)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=seyti8xR}Victory!", null), 0, null, null, "event:/ui/mission/arena_victory");
				}
				this.AfterPractice = true;
			}
			if (this._teleportTimer != null && this._teleportTimer.ElapsedTime > (float)this.TeleportTime)
			{
				this._teleportTimer = null;
				this.RemainingOpponentCountFromLastPractice = this.RemainingOpponentCount;
				this.IsPlayerPracticing = false;
				this.StartPractice();
				this.SpawnPlayerNearTournamentMaster();
				Agent agent = base.Mission.Agents.FirstOrDefault((Agent x) => x.Character != null && ((CharacterObject)x.Character).Occupation == Occupation.ArenaMaster);
				MissionConversationLogic.Current.StartConversation(agent, true, false);
			}
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x0002B2C8 File Offset: 0x000294C8
		private Team SelectRandomAiTeam()
		{
			Team team = null;
			foreach (Team team2 in this._AIParticipantTeams)
			{
				if (!team2.HasBots)
				{
					team = team2;
					break;
				}
			}
			if (team == null)
			{
				team = this._AIParticipantTeams[MBRandom.RandomInt(this._AIParticipantTeams.Count - 1) + 1];
			}
			return team;
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x0002B348 File Offset: 0x00029548
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (affectedAgent != null && affectedAgent.IsHuman)
			{
				if (affectedAgent != Agent.Main)
				{
					this._aliveOpponentCount--;
				}
				if (affectorAgent != null && affectorAgent.IsHuman && affectorAgent == Agent.Main && affectedAgent != Agent.Main)
				{
					int opponentCountBeatenByPlayer = this.OpponentCountBeatenByPlayer;
					this.OpponentCountBeatenByPlayer = opponentCountBeatenByPlayer + 1;
				}
			}
			if (this._participantAgents.Contains(affectedAgent))
			{
				this._participantAgents.Remove(affectedAgent);
			}
		}

		// Token: 0x06000655 RID: 1621 RVA: 0x0002B3BC File Offset: 0x000295BC
		public override bool MissionEnded(ref MissionResult missionResult)
		{
			return false;
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x0002B3C0 File Offset: 0x000295C0
		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			if (!this.IsPlayerPracticing)
			{
				return null;
			}
			return new InquiryData(new TextObject("{=zv49qE35}Practice Fight", null).ToString(), GameTexts.FindText("str_give_up_fight", null).ToString(), true, true, GameTexts.FindText("str_ok", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action(base.Mission.OnEndMissionResult), null, "", 0f, null, null, null);
		}

		// Token: 0x06000657 RID: 1623 RVA: 0x0002B440 File Offset: 0x00029640
		public void StartPlayerPractice()
		{
			this.IsPlayerPracticing = true;
			this.AfterPractice = false;
			this.StartPractice();
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x0002B458 File Offset: 0x00029658
		private void StartPractice()
		{
			this.InitializeParticipantCharacters();
			SandBoxHelpers.MissionHelper.FadeOutAgents(from agent in base.Mission.Agents
				where this._participantAgents.Contains(agent) || agent.IsMount || agent.IsPlayerControlled
				select agent, true, false);
			this._spawnedOpponentAgentCount = 0;
			this._aliveOpponentCount = 0;
			this._participantAgents.Clear();
			Mission.Current.ClearCorpses(false);
			base.Mission.RemoveSpawnedItemsAndMissiles();
			this.ArrangePlayerTeamEnmity();
			if (this.IsPlayerPracticing)
			{
				Agent agent2 = this.SpawnArenaAgent(base.Mission.PlayerTeam, this.GetSpawnFrame(false, true));
				agent2.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
				this.OpponentCountBeatenByPlayer = 0;
				this._participantAgents.Add(agent2);
			}
			int count = this._AIParticipantTeams.Count;
			int num = 0;
			while (this._spawnedOpponentAgentCount < 6)
			{
				this._participantAgents.Add(this.SpawnArenaAgent(this._AIParticipantTeams[num % count], this.GetSpawnFrame(false, true)));
				num++;
			}
			this._nextSpawnTime = base.Mission.CurrentTime + 14f;
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x0002B55B File Offset: 0x0002975B
		private bool CheckPracticeEndedForPlayer()
		{
			return base.Mission.MainAgent == null || !base.Mission.MainAgent.IsActive() || this.RemainingOpponentCount == 0;
		}

		// Token: 0x0600065A RID: 1626 RVA: 0x0002B588 File Offset: 0x00029788
		private void AddRandomWeapons(Equipment equipment, int spawnIndex)
		{
			int num = 1 + spawnIndex * 3 / 30;
			List<Equipment> list = (Game.Current.ObjectManager.GetObject<CharacterObject>(string.Concat(new object[]
			{
				"weapon_practice_stage_",
				num,
				"_",
				this._settlement.MapFaction.Culture.StringId
			})) ?? Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + num + "_empire")).BattleEquipments.ToList<Equipment>();
			int index = MBRandom.RandomInt(list.Count);
			for (int i = 0; i <= 3; i++)
			{
				EquipmentElement equipmentFromSlot = list[index].GetEquipmentFromSlot((EquipmentIndex)i);
				if (equipmentFromSlot.Item != null)
				{
					equipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
				}
			}
		}

		// Token: 0x0600065B RID: 1627 RVA: 0x0002B658 File Offset: 0x00029858
		private void AddRandomClothes(CharacterObject troop, Equipment equipment)
		{
			Equipment participantArmor = Campaign.Current.Models.TournamentModel.GetParticipantArmor(troop);
			for (int i = 0; i < 12; i++)
			{
				if (i > 4 && i != 10 && i != 11)
				{
					EquipmentElement equipmentFromSlot = participantArmor.GetEquipmentFromSlot((EquipmentIndex)i);
					if (equipmentFromSlot.Item != null)
					{
						equipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
					}
				}
			}
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x0002B6B0 File Offset: 0x000298B0
		private void InitializeTeams()
		{
			this._AIParticipantTeams = new List<Team>();
			base.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			base.Mission.PlayerTeam = base.Mission.DefenderTeam;
			this._tournamentMasterTeam = base.Mission.Teams.Add(BattleSideEnum.None, this._settlement.MapFaction.Color, this._settlement.MapFaction.Color2, null, true, false, true);
			while (this._AIParticipantTeams.Count < 6)
			{
				this._AIParticipantTeams.Add(base.Mission.Teams.Add(BattleSideEnum.Attacker, uint.MaxValue, uint.MaxValue, null, true, false, true));
			}
			for (int i = 0; i < this._AIParticipantTeams.Count; i++)
			{
				this._AIParticipantTeams[i].SetIsEnemyOf(this._tournamentMasterTeam, false);
				for (int j = i + 1; j < this._AIParticipantTeams.Count; j++)
				{
					this._AIParticipantTeams[i].SetIsEnemyOf(this._AIParticipantTeams[j], true);
				}
			}
		}

		// Token: 0x0600065D RID: 1629 RVA: 0x0002B7E4 File Offset: 0x000299E4
		private void InitializeParticipantCharacters()
		{
			List<CharacterObject> participantCharacters = ArenaPracticeFightMissionController.GetParticipantCharacters(this._settlement);
			this._participantCharacters = (from x in participantCharacters
				orderby x.Level
				select x).ToList<CharacterObject>();
		}

		// Token: 0x0600065E RID: 1630 RVA: 0x0002B830 File Offset: 0x00029A30
		public static List<CharacterObject> GetParticipantCharacters(Settlement settlement)
		{
			int num = 30;
			List<CharacterObject> list = new List<CharacterObject>();
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			if (list.Count < num && settlement.Town.GarrisonParty != null)
			{
				foreach (TroopRosterElement troopRosterElement in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
				{
					int num5 = num - list.Count;
					if (!list.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 3 && (float)num5 * 0.4f > (float)num2)
					{
						list.Add(troopRosterElement.Character);
						num2++;
					}
					else if (!list.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 4 && (float)num5 * 0.4f > (float)num3)
					{
						list.Add(troopRosterElement.Character);
						num3++;
					}
					else if (!list.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 5 && (float)num5 * 0.2f > (float)num4)
					{
						list.Add(troopRosterElement.Character);
						num4++;
					}
					if (list.Count >= num)
					{
						break;
					}
				}
			}
			if (list.Count < num)
			{
				List<CharacterObject> list2 = new List<CharacterObject>();
				ArenaPracticeFightMissionController.GetUpgradeTargets(((settlement != null) ? settlement.Culture : Game.Current.ObjectManager.GetObject<CultureObject>("empire")).BasicTroop, ref list2);
				int num6 = num - list.Count;
				using (List<CharacterObject>.Enumerator enumerator2 = list2.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						CharacterObject characterObject = enumerator2.Current;
						if (!list.Contains(characterObject) && characterObject.Tier == 3 && (float)num6 * 0.4f > (float)num2)
						{
							list.Add(characterObject);
							num2++;
						}
						else if (!list.Contains(characterObject) && characterObject.Tier == 4 && (float)num6 * 0.4f > (float)num3)
						{
							list.Add(characterObject);
							num3++;
						}
						else if (!list.Contains(characterObject) && characterObject.Tier == 5 && (float)num6 * 0.2f > (float)num4)
						{
							list.Add(characterObject);
							num4++;
						}
						if (list.Count >= num)
						{
							break;
						}
					}
					goto IL_284;
				}
				IL_256:
				int num7 = 0;
				while (num7 < list2.Count && list.Count < num)
				{
					list.Add(list2[num7]);
					num7++;
				}
				IL_284:
				if (list.Count < num)
				{
					goto IL_256;
				}
			}
			return list;
		}

		// Token: 0x0600065F RID: 1631 RVA: 0x0002BAE8 File Offset: 0x00029CE8
		private static void GetUpgradeTargets(CharacterObject troop, ref List<CharacterObject> list)
		{
			if (!list.Contains(troop) && troop.Tier >= 3)
			{
				list.Add(troop);
			}
			CharacterObject[] upgradeTargets = troop.UpgradeTargets;
			for (int i = 0; i < upgradeTargets.Length; i++)
			{
				ArenaPracticeFightMissionController.GetUpgradeTargets(upgradeTargets[i], ref list);
			}
		}

		// Token: 0x06000660 RID: 1632 RVA: 0x0002BB30 File Offset: 0x00029D30
		private void ArrangePlayerTeamEnmity()
		{
			foreach (Team team in this._AIParticipantTeams)
			{
				team.SetIsEnemyOf(base.Mission.PlayerTeam, this.IsPlayerPracticing);
			}
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x0002BB94 File Offset: 0x00029D94
		private Team GetStrongestTeamExceptPlayerTeam()
		{
			Team result = null;
			int num = -1;
			foreach (Team team in this._AIParticipantTeams)
			{
				int num2 = this.CalculateTeamPower(team);
				if (num2 > num)
				{
					result = team;
					num = num2;
				}
			}
			return result;
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x0002BBF8 File Offset: 0x00029DF8
		private int CalculateTeamPower(Team team)
		{
			int num = 0;
			foreach (Agent agent in team.ActiveAgents)
			{
				num += agent.Character.Level * agent.KillCount + (int)MathF.Sqrt(agent.Health);
			}
			return num;
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x0002BC6C File Offset: 0x00029E6C
		private MatrixFrame GetSpawnFrame(bool considerPlayerDistance, bool isInitialSpawn)
		{
			List<MatrixFrame> list = ((isInitialSpawn || this._spawnFrames.IsEmpty<MatrixFrame>()) ? this._initialSpawnFrames : this._spawnFrames);
			if (list.Count == 1)
			{
				Debug.FailedAssert("Spawn point count is wrong! Arena practice spawn point set should be used in arena scenes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Arena\\ArenaPracticeFightMissionController.cs", "GetSpawnFrame", 616);
				return list[0];
			}
			MatrixFrame result;
			if (considerPlayerDistance && Agent.Main != null && Agent.Main.IsActive())
			{
				int num = MBRandom.RandomInt(list.Count);
				result = list[num];
				float num2 = float.MinValue;
				for (int i = num + 1; i < num + list.Count; i++)
				{
					MatrixFrame matrixFrame = list[i % list.Count];
					float num3 = this.CalculateLocationScore(matrixFrame);
					if (num3 >= 100f)
					{
						result = matrixFrame;
						break;
					}
					if (num3 > num2)
					{
						result = matrixFrame;
						num2 = num3;
					}
				}
			}
			else
			{
				int num4 = this._spawnedOpponentAgentCount;
				if (this.IsPlayerPracticing && Agent.Main != null)
				{
					num4++;
				}
				result = list[num4 % list.Count];
			}
			return result;
		}

		// Token: 0x06000664 RID: 1636 RVA: 0x0002BD70 File Offset: 0x00029F70
		private float CalculateLocationScore(MatrixFrame matrixFrame)
		{
			float num = 100f;
			float num2 = 0.25f;
			float num3 = 0.75f;
			if (matrixFrame.origin.DistanceSquared(Agent.Main.Position) < 144f)
			{
				num *= num2;
			}
			for (int i = 0; i < this._participantAgents.Count; i++)
			{
				if (this._participantAgents[i].Position.DistanceSquared(matrixFrame.origin) < 144f)
				{
					num *= num3;
				}
			}
			return num;
		}

		// Token: 0x0400035E RID: 862
		private const int AIParticipantCount = 30;

		// Token: 0x0400035F RID: 863
		private const int MaxAliveAgentCount = 6;

		// Token: 0x04000360 RID: 864
		private const int MaxSpawnInterval = 14;

		// Token: 0x04000361 RID: 865
		private const int MinSpawnDistanceSquared = 144;

		// Token: 0x04000362 RID: 866
		private const int TotalStageCount = 3;

		// Token: 0x04000363 RID: 867
		private const int PracticeFightTroopTierLimit = 3;

		// Token: 0x04000364 RID: 868
		public int TeleportTime = 5;

		// Token: 0x04000365 RID: 869
		private Settlement _settlement;

		// Token: 0x04000366 RID: 870
		private int _spawnedOpponentAgentCount;

		// Token: 0x04000367 RID: 871
		private int _aliveOpponentCount;

		// Token: 0x04000368 RID: 872
		private float _nextSpawnTime;

		// Token: 0x04000369 RID: 873
		private List<MatrixFrame> _initialSpawnFrames;

		// Token: 0x0400036A RID: 874
		private List<MatrixFrame> _spawnFrames;

		// Token: 0x0400036B RID: 875
		private List<Team> _AIParticipantTeams;

		// Token: 0x0400036C RID: 876
		private List<Agent> _participantAgents;

		// Token: 0x0400036D RID: 877
		private Team _tournamentMasterTeam;

		// Token: 0x0400036E RID: 878
		private BasicMissionTimer _teleportTimer;

		// Token: 0x0400036F RID: 879
		private List<CharacterObject> _participantCharacters;

		// Token: 0x04000375 RID: 885
		private const float XpShareForKill = 0.5f;

		// Token: 0x04000376 RID: 886
		private const float XpShareForDamage = 0.5f;
	}
}
