using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class BattleAgentLogic : MissionLogic
{
	private BattleObserverMissionLogic _battleObserverMissionLogic;

	private const float XpShareForKill = 0.5f;

	private const float XpShareForDamage = 0.5f;

	private MissionTime _nextMoraleCheckTime;

	private TroopUpgradeTracker _troopUpgradeTracker => MapEvent.PlayerMapEvent.TroopUpgradeTracker;

	public override void AfterStart()
	{
		_battleObserverMissionLogic = Mission.Current.GetMissionBehavior<BattleObserverMissionLogic>();
		CheckPerkEffectsOnTeams();
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (_battleObserverMissionLogic != null && agent.Character != null && agent.Origin != null)
		{
			PartyBase partyBase = (PartyBase)agent.Origin.BattleCombatant;
			CharacterObject character = (CharacterObject)agent.Character;
			if (partyBase != null)
			{
				_troopUpgradeTracker?.AddTrackedTroop(partyBase, character);
			}
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		if (affectedAgent.Character != null && affectorAgent != null && affectorAgent.Character != null && affectedAgent.State == AgentState.Active && affectorAgent != null)
		{
			bool isFatal = affectedAgent.Health - (float)blow.InflictedDamage < 1f;
			bool isTeamKill = false;
			if (affectedAgent.Team != null && affectorAgent.Team != null)
			{
				isTeamKill = affectedAgent.Team.Side == affectorAgent.Team.Side;
			}
			affectorAgent.Origin.OnScoreHit(affectedAgent.Character, affectorAgent.Formation?.Captain?.Character, blow.InflictedDamage, isFatal, isTeamKill, attackerWeapon.CurrentUsageItem);
		}
	}

	public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		if (prevTeam != null && prevTeam != Team.Invalid && newTeam != null && prevTeam != newTeam)
		{
			_battleObserverMissionLogic?.BattleObserver?.TroopSideChanged(prevTeam?.Side ?? BattleSideEnum.None, newTeam?.Side ?? BattleSideEnum.None, (PartyBase)agent.Origin.BattleCombatant, agent.Character);
		}
	}

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
		if (affectorAgent.Character != null && affectedAgent.Character != null)
		{
			float num = blow.InflictedDamage;
			if (num > affectedAgent.HealthLimit)
			{
				num = affectedAgent.HealthLimit;
			}
			float num2 = num / affectedAgent.HealthLimit;
			EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, isSiegeEngineHit, attackerWeapon, blow.AttackType, 0.5f * num2, num, collisionData.IsSneakAttack);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectorAgent == null && affectedAgent.IsMount && agentState == AgentState.Routed)
		{
			return;
		}
		CharacterObject characterObject = (CharacterObject)affectedAgent.Character;
		CharacterObject characterObject2 = (CharacterObject)(affectorAgent?.Character);
		if (affectedAgent.Origin == null)
		{
			return;
		}
		PartyBase partyBase = (PartyBase)affectedAgent.Origin.BattleCombatant;
		switch (agentState)
		{
		case AgentState.Unconscious:
			affectedAgent.Origin.SetWounded();
			break;
		case AgentState.Killed:
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
				CheckUpgrade(affectedAgent.Team.Side, partyBase, characterObject);
			}
			break;
		}
		default:
		{
			bool flag = affectedAgent.GetMorale() < 0.01f;
			affectedAgent.Origin.SetRouted(!flag);
			break;
		}
		}
	}

	public override void OnAgentFleeing(Agent affectedAgent)
	{
	}

	public override void OnMissionTick(float dt)
	{
		UpdateMorale();
		if (_nextMoraleCheckTime.IsPast)
		{
			_nextMoraleCheckTime = MissionTime.SecondsFromNow(10f);
		}
	}

	private void CheckPerkEffectsOnTeams()
	{
	}

	private void UpdateMorale()
	{
	}

	private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, bool isSiegeEngineHit, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
	{
		CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
		CharacterObject characterObject = (CharacterObject)affectorAgent.Character;
		if (affectedAgent.Origin == null || affectorAgent == null || affectorAgent.Origin == null || affectorAgent.Team == null || !affectorAgent.Team.IsValid || affectedAgent.Team == null || !affectedAgent.Team.IsValid)
		{
			return;
		}
		PartyBase partyBase = (PartyBase)affectorAgent.Origin.BattleCombatant;
		Hero captain = GetCaptain(affectorAgent);
		Hero hero = ((affectorAgent.Team.Leader != null && affectorAgent.Team.Leader.Character.IsHero) ? ((CharacterObject)affectorAgent.Team.Leader.Character).HeroObject : null);
		bool isTeamKill = affectorAgent.Team.Side == affectedAgent.Team.Side;
		bool flag = affectorAgent.MountAgent != null;
		bool isHorseCharge = flag && attackType == AgentAttackType.Collision;
		SkillLevelingManager.OnCombatHit(characterObject, affectedCharacter, captain?.CharacterObject, hero, lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, flag, isTeamKill, hero != null && affectorAgent.Character != hero.CharacterObject && (hero != Hero.MainHero || affectorAgent.Formation == null || !affectorAgent.Formation.IsAIControlled), damageAmount, affectedAgent.Health < 1f, isSiegeEngineHit, isHorseCharge, isSneakAttack);
		if (_battleObserverMissionLogic?.BattleObserver == null || affectorAgent.Character == null)
		{
			return;
		}
		if (affectorAgent.Character.IsHero)
		{
			Hero heroObject = characterObject.HeroObject;
			{
				foreach (SkillObject item in _troopUpgradeTracker.CheckSkillUpgrades(heroObject))
				{
					_battleObserverMissionLogic.BattleObserver.HeroSkillIncreased(affectorAgent.Team.Side, partyBase, characterObject, item);
				}
				return;
			}
		}
		CheckUpgrade(affectorAgent.Team.Side, partyBase, characterObject);
	}

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

	private void CheckUpgrade(BattleSideEnum side, PartyBase party, CharacterObject character)
	{
		if (_battleObserverMissionLogic?.BattleObserver != null)
		{
			int num = _troopUpgradeTracker.CheckUpgradedCount(party, character);
			if (num != 0)
			{
				_battleObserverMissionLogic.BattleObserver.TroopNumberChanged(side, party, character, 0, 0, 0, 0, 0, num);
			}
		}
	}
}
