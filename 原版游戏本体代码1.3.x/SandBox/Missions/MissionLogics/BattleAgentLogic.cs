using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000062 RID: 98
	public class BattleAgentLogic : MissionLogic
	{
		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060003BC RID: 956 RVA: 0x00015DF3 File Offset: 0x00013FF3
		private TroopUpgradeTracker _troopUpgradeTracker
		{
			get
			{
				return MapEvent.PlayerMapEvent.TroopUpgradeTracker;
			}
		}

		// Token: 0x060003BD RID: 957 RVA: 0x00015DFF File Offset: 0x00013FFF
		public override void AfterStart()
		{
			this._battleObserverMissionLogic = Mission.Current.GetMissionBehavior<BattleObserverMissionLogic>();
			this.CheckPerkEffectsOnTeams();
		}

		// Token: 0x060003BE RID: 958 RVA: 0x00015E18 File Offset: 0x00014018
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (this._battleObserverMissionLogic != null && agent.Character != null && agent.Origin != null)
			{
				PartyBase partyBase = (PartyBase)agent.Origin.BattleCombatant;
				CharacterObject character = (CharacterObject)agent.Character;
				if (partyBase != null)
				{
					TroopUpgradeTracker troopUpgradeTracker = this._troopUpgradeTracker;
					if (troopUpgradeTracker == null)
					{
						return;
					}
					troopUpgradeTracker.AddTrackedTroop(partyBase, character);
				}
			}
		}

		// Token: 0x060003BF RID: 959 RVA: 0x00015E70 File Offset: 0x00014070
		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			if (affectedAgent.Character != null && affectorAgent != null && affectorAgent.Character != null && affectedAgent.State == AgentState.Active && affectorAgent != null)
			{
				bool flag = affectedAgent.Health - (float)blow.InflictedDamage < 1f;
				bool flag2 = false;
				if (affectedAgent.Team != null && affectorAgent.Team != null)
				{
					flag2 = affectedAgent.Team.Side == affectorAgent.Team.Side;
				}
				IAgentOriginBase origin = affectorAgent.Origin;
				BasicCharacterObject character = affectedAgent.Character;
				Formation formation = affectorAgent.Formation;
				BasicCharacterObject formationCaptain;
				if (formation == null)
				{
					formationCaptain = null;
				}
				else
				{
					Agent captain = formation.Captain;
					formationCaptain = ((captain != null) ? captain.Character : null);
				}
				int inflictedDamage = blow.InflictedDamage;
				bool isFatal = flag;
				bool isTeamKill = flag2;
				MissionWeapon missionWeapon = attackerWeapon;
				origin.OnScoreHit(character, formationCaptain, inflictedDamage, isFatal, isTeamKill, missionWeapon.CurrentUsageItem);
			}
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x00015F34 File Offset: 0x00014134
		public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
		{
			if (prevTeam != null && prevTeam != Team.Invalid && newTeam != null && prevTeam != newTeam)
			{
				BattleObserverMissionLogic battleObserverMissionLogic = this._battleObserverMissionLogic;
				if (battleObserverMissionLogic == null)
				{
					return;
				}
				IBattleObserver battleObserver = battleObserverMissionLogic.BattleObserver;
				if (battleObserver == null)
				{
					return;
				}
				battleObserver.TroopSideChanged((prevTeam != null) ? prevTeam.Side : BattleSideEnum.None, (newTeam != null) ? newTeam.Side : BattleSideEnum.None, (PartyBase)agent.Origin.BattleCombatant, agent.Character);
			}
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x00015F9C File Offset: 0x0001419C
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
			this.EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, isSiegeEngineHit, attackerWeapon, blow.AttackType, 0.5f * num2, num, collisionData.IsSneakAttack);
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x00016024 File Offset: 0x00014224
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (affectorAgent == null && affectedAgent.IsMount && agentState == AgentState.Routed)
			{
				return;
			}
			CharacterObject characterObject = (CharacterObject)affectedAgent.Character;
			CharacterObject characterObject2 = (CharacterObject)((affectorAgent != null) ? affectorAgent.Character : null);
			if (affectedAgent.Origin != null)
			{
				PartyBase partyBase = (PartyBase)affectedAgent.Origin.BattleCombatant;
				if (agentState == AgentState.Unconscious)
				{
					affectedAgent.Origin.SetWounded();
					return;
				}
				if (agentState == AgentState.Killed)
				{
					affectedAgent.Origin.SetKilled();
					Hero hero = (affectedAgent.IsHuman ? characterObject.HeroObject : null);
					Hero hero2 = ((affectorAgent == null) ? null : (affectorAgent.IsHuman ? characterObject2.HeroObject : null));
					if (hero != null && hero2 != null)
					{
						CampaignEventDispatcher.Instance.OnCharacterDefeated(hero2, hero);
					}
					if (partyBase != null)
					{
						this.CheckUpgrade(affectedAgent.Team.Side, partyBase, characterObject);
						return;
					}
				}
				else
				{
					bool flag = affectedAgent.GetMorale() < 0.01f;
					affectedAgent.Origin.SetRouted(!flag);
				}
			}
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x0001610F File Offset: 0x0001430F
		public override void OnAgentFleeing(Agent affectedAgent)
		{
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x00016111 File Offset: 0x00014311
		public override void OnMissionTick(float dt)
		{
			this.UpdateMorale();
			if (this._nextMoraleCheckTime.IsPast)
			{
				this._nextMoraleCheckTime = MissionTime.SecondsFromNow(10f);
			}
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x00016136 File Offset: 0x00014336
		private void CheckPerkEffectsOnTeams()
		{
		}

		// Token: 0x060003C6 RID: 966 RVA: 0x00016138 File Offset: 0x00014338
		private void UpdateMorale()
		{
		}

		// Token: 0x060003C7 RID: 967 RVA: 0x0001613C File Offset: 0x0001433C
		private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, bool isSiegeEngineHit, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
		{
			CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
			CharacterObject characterObject = (CharacterObject)affectorAgent.Character;
			if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null && affectorAgent.Team != null && affectorAgent.Team.IsValid && affectedAgent.Team != null && affectedAgent.Team.IsValid)
			{
				PartyBase partyBase = (PartyBase)affectorAgent.Origin.BattleCombatant;
				Hero captain = BattleAgentLogic.GetCaptain(affectorAgent);
				Hero hero = ((affectorAgent.Team.Leader != null && affectorAgent.Team.Leader.Character.IsHero) ? ((CharacterObject)affectorAgent.Team.Leader.Character).HeroObject : null);
				bool isTeamKill = affectorAgent.Team.Side == affectedAgent.Team.Side;
				bool flag = affectorAgent.MountAgent != null;
				bool isHorseCharge = flag && attackType == AgentAttackType.Collision;
				SkillLevelingManager.OnCombatHit(characterObject, affectedCharacter, (captain != null) ? captain.CharacterObject : null, hero, lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, flag, isTeamKill, hero != null && affectorAgent.Character != hero.CharacterObject && (hero != Hero.MainHero || affectorAgent.Formation == null || !affectorAgent.Formation.IsAIControlled), damageAmount, affectedAgent.Health < 1f, isSiegeEngineHit, isHorseCharge, isSneakAttack);
				BattleObserverMissionLogic battleObserverMissionLogic = this._battleObserverMissionLogic;
				if (((battleObserverMissionLogic != null) ? battleObserverMissionLogic.BattleObserver : null) != null && affectorAgent.Character != null)
				{
					if (affectorAgent.Character.IsHero)
					{
						Hero heroObject = characterObject.HeroObject;
						using (IEnumerator<SkillObject> enumerator = this._troopUpgradeTracker.CheckSkillUpgrades(heroObject).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SkillObject skill = enumerator.Current;
								this._battleObserverMissionLogic.BattleObserver.HeroSkillIncreased(affectorAgent.Team.Side, partyBase, characterObject, skill);
							}
							return;
						}
					}
					this.CheckUpgrade(affectorAgent.Team.Side, partyBase, characterObject);
				}
			}
		}

		// Token: 0x060003C8 RID: 968 RVA: 0x00016360 File Offset: 0x00014560
		private static Hero GetCaptain(Agent affectorAgent)
		{
			Hero result = null;
			if (affectorAgent.Formation != null)
			{
				Agent captain = affectorAgent.Formation.Captain;
				if (captain != null)
				{
					float captainRadius = Campaign.Current.Models.CombatXpModel.CaptainRadius;
					if (captain.Position.Distance(affectorAgent.Position) < captainRadius)
					{
						result = ((CharacterObject)captain.Character).HeroObject;
					}
				}
			}
			return result;
		}

		// Token: 0x060003C9 RID: 969 RVA: 0x000163C4 File Offset: 0x000145C4
		private void CheckUpgrade(BattleSideEnum side, PartyBase party, CharacterObject character)
		{
			BattleObserverMissionLogic battleObserverMissionLogic = this._battleObserverMissionLogic;
			if (((battleObserverMissionLogic != null) ? battleObserverMissionLogic.BattleObserver : null) != null)
			{
				int num = this._troopUpgradeTracker.CheckUpgradedCount(party, character);
				if (num != 0)
				{
					this._battleObserverMissionLogic.BattleObserver.TroopNumberChanged(side, party, character, 0, 0, 0, 0, 0, num);
				}
			}
		}

		// Token: 0x04000201 RID: 513
		private BattleObserverMissionLogic _battleObserverMissionLogic;

		// Token: 0x04000202 RID: 514
		private const float XpShareForKill = 0.5f;

		// Token: 0x04000203 RID: 515
		private const float XpShareForDamage = 0.5f;

		// Token: 0x04000204 RID: 516
		private MissionTime _nextMoraleCheckTime;
	}
}
