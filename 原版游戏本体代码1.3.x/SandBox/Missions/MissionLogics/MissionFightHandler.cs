using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000079 RID: 121
	public class MissionFightHandler : MissionLogic
	{
		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060004D0 RID: 1232 RVA: 0x0001EC7B File Offset: 0x0001CE7B
		private static MissionFightHandler _current
		{
			get
			{
				return Mission.Current.GetMissionBehavior<MissionFightHandler>();
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x060004D1 RID: 1233 RVA: 0x0001EC87 File Offset: 0x0001CE87
		// (set) Token: 0x060004D2 RID: 1234 RVA: 0x0001EC8F File Offset: 0x0001CE8F
		public float MinMissionEndTime { get; private set; }

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x060004D3 RID: 1235 RVA: 0x0001EC98 File Offset: 0x0001CE98
		public ReadOnlyCollection<Agent> PlayerSideAgents
		{
			get
			{
				return this._playerSideAgents.AsReadOnly();
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x060004D4 RID: 1236 RVA: 0x0001ECA5 File Offset: 0x0001CEA5
		public ReadOnlyCollection<Agent> OpponentSideAgents
		{
			get
			{
				return this._opponentSideAgents.AsReadOnly();
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060004D5 RID: 1237 RVA: 0x0001ECB2 File Offset: 0x0001CEB2
		public bool IsPlayerSideWon
		{
			get
			{
				return this._isPlayerSideWon;
			}
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x0001ECBA File Offset: 0x0001CEBA
		public override void OnBehaviorInitialize()
		{
			base.Mission.IsAgentInteractionAllowed_AdditionalCondition += this.IsAgentInteractionAllowed_AdditionalCondition;
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x0001ECD3 File Offset: 0x0001CED3
		public override void EarlyStart()
		{
			this._playerSideAgents = new List<Agent>();
			this._opponentSideAgents = new List<Agent>();
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x0001ECEB File Offset: 0x0001CEEB
		public override void AfterStart()
		{
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x0001ECED File Offset: 0x0001CEED
		public override void OnMissionTick(float dt)
		{
			if (base.Mission.CurrentTime > this.MinMissionEndTime && this._finishTimer != null && this._finishTimer.ElapsedTime > 5f)
			{
				this._finishTimer = null;
				this.EndFight(false);
			}
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x0001ED2C File Offset: 0x0001CF2C
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (this._state != MissionFightHandler.State.Fighting)
			{
				return;
			}
			if (affectedAgent == Agent.Main)
			{
				Mission.Current.NextCheckTimeEndMission += 8f;
			}
			if (affectorAgent != null && this._playerSideAgents.Contains(affectedAgent))
			{
				this._playerSideAgents.Remove(affectedAgent);
				if (this._playerSideAgents.Count == 0)
				{
					this._isPlayerSideWon = false;
					this._finishTimer = new BasicMissionTimer();
					return;
				}
			}
			else if (affectorAgent != null && this._opponentSideAgents.Contains(affectedAgent))
			{
				this._opponentSideAgents.Remove(affectedAgent);
				if (this._opponentSideAgents.Count == 0)
				{
					this._isPlayerSideWon = true;
					this._finishTimer = new BasicMissionTimer();
				}
			}
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x0001EDDC File Offset: 0x0001CFDC
		public void StartCustomFight(List<Agent> playerSideAgents, List<Agent> opponentSideAgents, bool dropWeapons, bool isItemUseDisabled, MissionFightHandler.OnFightEndDelegate onFightEndDelegate, float minimumEndTime = 1E-45f)
		{
			this.StartFightInternal(playerSideAgents, opponentSideAgents, dropWeapons, isItemUseDisabled, onFightEndDelegate, minimumEndTime);
			this.SetTeamsForFightAndDuel();
			this._oldMissionMode = Mission.Current.Mode;
			Mission.Current.SetMissionMode(MissionMode.Battle, false);
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x0001EE10 File Offset: 0x0001D010
		public void StartFistFight(Agent opponent, MissionFightHandler.OnFightEndDelegate onFightEndDelegate, float minimumEndTime = 1E-45f)
		{
			this.StartFightInternal(new List<Agent> { Agent.Main }, new List<Agent> { opponent }, false, false, delegate(bool playerWon)
			{
				this.AttachCachedEquipment(Agent.Main, opponent);
				MissionFightHandler.OnFightEndDelegate onFightEndDelegate2 = onFightEndDelegate;
				if (onFightEndDelegate2 == null)
				{
					return;
				}
				onFightEndDelegate2(playerWon);
			}, minimumEndTime);
			this.SetTeamsForFightAndDuel();
			this._playerEquipment = new MissionEquipment();
			this._opponentEquipment = new MissionEquipment();
			this.RemoveWeaponsFromAgents(Agent.Main, opponent);
			this._oldMissionMode = Mission.Current.Mode;
			Mission.Current.SetMissionMode(MissionMode.Battle, false);
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x0001EEB8 File Offset: 0x0001D0B8
		private void RemoveWeaponsFromAgents(Agent main, Agent opponent)
		{
			this._playerEquipment.FillFrom(main.Equipment);
			this._opponentEquipment.FillFrom(opponent.Equipment);
			main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			opponent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			opponent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				main.RemoveEquippedWeapon(equipmentIndex);
				opponent.RemoveEquippedWeapon(equipmentIndex);
			}
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x0001EF24 File Offset: 0x0001D124
		private void AttachCachedEquipment(Agent main, Agent opponent)
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				MissionWeapon missionWeapon = this._playerEquipment[equipmentIndex];
				main.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon);
				MissionWeapon missionWeapon2 = this._opponentEquipment[equipmentIndex];
				opponent.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon2);
			}
			this._playerEquipment = null;
			this._opponentEquipment = null;
		}

		// Token: 0x060004DF RID: 1247 RVA: 0x0001EF78 File Offset: 0x0001D178
		private void StartFightInternal(List<Agent> playerSideAgents, List<Agent> opponentSideAgents, bool dropWeapons, bool isItemUseDisabled, MissionFightHandler.OnFightEndDelegate onFightEndDelegate, float minimumEndTime = 1E-45f)
		{
			this._state = MissionFightHandler.State.Fighting;
			this._opponentSideAgents = opponentSideAgents;
			this._playerSideAgents = playerSideAgents;
			this._playerSideAgentsOldTeamData = new Dictionary<Agent, Team>();
			this._opponentSideAgentsOldTeamData = new Dictionary<Agent, Team>();
			MissionFightHandler._onFightEnd = onFightEndDelegate;
			this._isPlayerSideWon = false;
			Mission.Current.MainAgent.IsItemUseDisabled = isItemUseDisabled;
			foreach (Agent agent in this._opponentSideAgents)
			{
				if (dropWeapons)
				{
					this.DropAllWeapons(agent);
				}
				this._opponentSideAgentsOldTeamData.Add(agent, agent.Team);
				this.ForceAgentForFight(agent);
			}
			foreach (Agent agent2 in this._playerSideAgents)
			{
				if (dropWeapons)
				{
					this.DropAllWeapons(agent2);
				}
				this._playerSideAgentsOldTeamData.Add(agent2, agent2.Team);
				this.ForceAgentForFight(agent2);
			}
			if (minimumEndTime > 0f && !minimumEndTime.ApproximatelyEqualsTo(1E-45f, 1E-05f))
			{
				this.MinMissionEndTime = base.Mission.CurrentTime + minimumEndTime;
				return;
			}
			this.MinMissionEndTime = 0f;
		}

		// Token: 0x060004E0 RID: 1248 RVA: 0x0001F0CC File Offset: 0x0001D2CC
		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			if (this._state == MissionFightHandler.State.Fighting && (this._opponentSideAgents.Count > 0 || this._playerSideAgents.Count > 0))
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=Fpk3BUBs}Your fight has not ended yet!", null), 0, null, null, "");
				canPlayerLeave = false;
			}
			return null;
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x0001F11D File Offset: 0x0001D31D
		private void ForceAgentForFight(Agent agent)
		{
			if (agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				AlarmedBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
				behaviorGroup.DisableCalmDown = true;
				behaviorGroup.AddBehavior<FightBehavior>();
				behaviorGroup.SetScriptedBehavior<FightBehavior>();
			}
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x0001F14F File Offset: 0x0001D34F
		protected override void OnEndMission()
		{
			base.Mission.IsAgentInteractionAllowed_AdditionalCondition -= this.IsAgentInteractionAllowed_AdditionalCondition;
		}

		// Token: 0x060004E3 RID: 1251 RVA: 0x0001F168 File Offset: 0x0001D368
		private void SetTeamsForFightAndDuel()
		{
			Mission.Current.PlayerEnemyTeam.SetIsEnemyOf(Mission.Current.PlayerTeam, true);
			foreach (Agent agent in this._playerSideAgents)
			{
				if (agent.IsHuman)
				{
					if (agent.IsAIControlled)
					{
						agent.SetWatchState(Agent.WatchState.Alarmed);
					}
					agent.SetTeam(Mission.Current.PlayerTeam, true);
				}
			}
			foreach (Agent agent2 in this._opponentSideAgents)
			{
				if (agent2.IsHuman)
				{
					if (agent2.IsAIControlled)
					{
						agent2.SetWatchState(Agent.WatchState.Alarmed);
					}
					agent2.SetTeam(Mission.Current.PlayerEnemyTeam, true);
				}
			}
		}

		// Token: 0x060004E4 RID: 1252 RVA: 0x0001F25C File Offset: 0x0001D45C
		private void ResetTeamsForFightAndDuel()
		{
			foreach (Agent agent in this._playerSideAgents)
			{
				if (agent.IsAIControlled)
				{
					agent.ResetEnemyCaches();
					agent.InvalidateTargetAgent();
					agent.InvalidateAIWeaponSelections();
					agent.SetWatchState(Agent.WatchState.Patrolling);
				}
				agent.SetTeam(new Team(this._playerSideAgentsOldTeamData[agent].MBTeam, BattleSideEnum.None, base.Mission, uint.MaxValue, uint.MaxValue, null), true);
			}
			foreach (Agent agent2 in this._opponentSideAgents)
			{
				if (agent2.IsAIControlled)
				{
					agent2.ResetEnemyCaches();
					agent2.InvalidateTargetAgent();
					agent2.InvalidateAIWeaponSelections();
					agent2.SetWatchState(Agent.WatchState.Patrolling);
				}
				agent2.SetTeam(new Team(this._opponentSideAgentsOldTeamData[agent2].MBTeam, BattleSideEnum.None, base.Mission, uint.MaxValue, uint.MaxValue, null), true);
			}
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x0001F374 File Offset: 0x0001D574
		private bool IsAgentInteractionAllowed_AdditionalCondition()
		{
			return this._state != MissionFightHandler.State.Fighting;
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x0001F384 File Offset: 0x0001D584
		public static Agent GetAgentToSpectate()
		{
			MissionFightHandler current = MissionFightHandler._current;
			if (current._playerSideAgents.Count > 0)
			{
				return current._playerSideAgents[0];
			}
			if (current._opponentSideAgents.Count > 0)
			{
				return current._opponentSideAgents[0];
			}
			return null;
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x0001F3D0 File Offset: 0x0001D5D0
		private void DropAllWeapons(Agent agent)
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!agent.Equipment[equipmentIndex].IsEmpty)
				{
					agent.DropItem(equipmentIndex, WeaponClass.Undefined);
				}
			}
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x0001F408 File Offset: 0x0001D608
		private void ResetScriptedBehaviors()
		{
			foreach (Agent agent in this._playerSideAgents)
			{
				if (agent.IsActive() && agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
				{
					agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().DisableScriptedBehavior();
				}
			}
			foreach (Agent agent2 in this._opponentSideAgents)
			{
				if (agent2.IsActive() && agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
				{
					agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().DisableScriptedBehavior();
				}
			}
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x0001F4E4 File Offset: 0x0001D6E4
		public void BeginEndFight()
		{
			this._finishTimer = new BasicMissionTimer();
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x0001F4F4 File Offset: 0x0001D6F4
		public void EndFight(bool overrideDuelWonByPlayer = false)
		{
			this.ResetScriptedBehaviors();
			this.ResetTeamsForFightAndDuel();
			this._state = MissionFightHandler.State.FightEnded;
			foreach (Agent agent in this._playerSideAgents)
			{
				if (agent.IsActive())
				{
					agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
					agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
				}
			}
			foreach (Agent agent2 in this._opponentSideAgents)
			{
				if (agent2.IsActive())
				{
					agent2.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
					agent2.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
				}
			}
			this._playerSideAgents.Clear();
			this._opponentSideAgents.Clear();
			if (Mission.Current.MainAgent != null)
			{
				Mission.Current.MainAgent.IsItemUseDisabled = false;
			}
			if (this._oldMissionMode == MissionMode.Conversation && !Campaign.Current.ConversationManager.IsConversationFlowActive)
			{
				this._oldMissionMode = MissionMode.StartUp;
			}
			Mission.Current.SetMissionMode(this._oldMissionMode, false);
			if (MissionFightHandler._onFightEnd != null)
			{
				MissionFightHandler._onFightEnd(this._isPlayerSideWon || overrideDuelWonByPlayer);
				this._isPlayerSideWon = false;
				MissionFightHandler._onFightEnd = null;
			}
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001F648 File Offset: 0x0001D848
		public bool IsThereActiveFight()
		{
			return this._state == MissionFightHandler.State.Fighting;
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0001F654 File Offset: 0x0001D854
		public void AddAgentToSide(Agent agent, bool isPlayerSide)
		{
			if (!this.IsThereActiveFight() || this._playerSideAgents.Contains(agent) || this._opponentSideAgents.Contains(agent))
			{
				return;
			}
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			if (isPlayerSide)
			{
				agent.SetTeam(Mission.Current.PlayerTeam, true);
				this._playerSideAgents.Add(agent);
				this._playerSideAgentsOldTeamData.Add(agent, agent.Team);
			}
			else
			{
				agent.SetTeam(Mission.Current.PlayerEnemyTeam, true);
				this._opponentSideAgents.Add(agent);
				this._opponentSideAgentsOldTeamData.Add(agent, agent.Team);
			}
			if (this._playerSideAgents.Count == 0 || this._opponentSideAgents.Count == 0)
			{
				this._finishTimer = new BasicMissionTimer();
			}
			else
			{
				this._finishTimer = null;
			}
			this.ForceAgentForFight(agent);
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x0001F730 File Offset: 0x0001D930
		public IEnumerable<Agent> GetDangerSources(Agent ownerAgent)
		{
			if (!(ownerAgent.Character is CharacterObject))
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\MissionFightHandler.cs", "GetDangerSources", 469);
				return new List<Agent>();
			}
			if (this.IsThereActiveFight() && !MissionFightHandler.IsAgentAggressive(ownerAgent) && Agent.Main != null)
			{
				return new List<Agent> { Agent.Main };
			}
			return new List<Agent>();
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x0001F798 File Offset: 0x0001D998
		public static bool IsAgentAggressive(Agent agent)
		{
			CharacterObject characterObject = agent.Character as CharacterObject;
			return agent.HasWeapon() || (characterObject != null && (characterObject.Occupation == Occupation.Mercenary || MissionFightHandler.IsAgentVillian(characterObject) || MissionFightHandler.IsAgentJusticeWarrior(characterObject)));
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x0001F7D9 File Offset: 0x0001D9D9
		public static bool IsAgentJusticeWarrior(CharacterObject character)
		{
			return character.Occupation == Occupation.Soldier || character.Occupation == Occupation.Guard || character.Occupation == Occupation.PrisonGuard;
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x0001F7FA File Offset: 0x0001D9FA
		public static bool IsAgentVillian(CharacterObject character)
		{
			return character.Occupation == Occupation.Gangster || character.Occupation == Occupation.GangLeader || character.Occupation == Occupation.Bandit;
		}

		// Token: 0x04000284 RID: 644
		private static MissionFightHandler.OnFightEndDelegate _onFightEnd;

		// Token: 0x04000286 RID: 646
		private List<Agent> _playerSideAgents;

		// Token: 0x04000287 RID: 647
		private List<Agent> _opponentSideAgents;

		// Token: 0x04000288 RID: 648
		private Dictionary<Agent, Team> _playerSideAgentsOldTeamData;

		// Token: 0x04000289 RID: 649
		private Dictionary<Agent, Team> _opponentSideAgentsOldTeamData;

		// Token: 0x0400028A RID: 650
		private MissionFightHandler.State _state;

		// Token: 0x0400028B RID: 651
		private BasicMissionTimer _finishTimer;

		// Token: 0x0400028C RID: 652
		private bool _isPlayerSideWon;

		// Token: 0x0400028D RID: 653
		private MissionMode _oldMissionMode;

		// Token: 0x0400028E RID: 654
		private MissionEquipment _playerEquipment;

		// Token: 0x0400028F RID: 655
		private MissionEquipment _opponentEquipment;

		// Token: 0x0200016D RID: 365
		private enum State
		{
			// Token: 0x04000703 RID: 1795
			NoFight,
			// Token: 0x04000704 RID: 1796
			Fighting,
			// Token: 0x04000705 RID: 1797
			FightEnded
		}

		// Token: 0x0200016E RID: 366
		// (Invoke) Token: 0x06000E51 RID: 3665
		public delegate void OnFightEndDelegate(bool isPlayerSideWon);
	}
}
