using System.Collections.Generic;
using System.Collections.ObjectModel;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionFightHandler : MissionLogic
{
	private enum State
	{
		NoFight,
		Fighting,
		FightEnded
	}

	public delegate void OnFightEndDelegate(bool isPlayerSideWon);

	private static OnFightEndDelegate _onFightEnd;

	private List<Agent> _playerSideAgents;

	private List<Agent> _opponentSideAgents;

	private Dictionary<Agent, Team> _playerSideAgentsOldTeamData;

	private Dictionary<Agent, Team> _opponentSideAgentsOldTeamData;

	private State _state;

	private BasicMissionTimer _finishTimer;

	private bool _isPlayerSideWon;

	private MissionMode _oldMissionMode;

	private MissionEquipment _playerEquipment;

	private MissionEquipment _opponentEquipment;

	private static MissionFightHandler _current => Mission.Current.GetMissionBehavior<MissionFightHandler>();

	public float MinMissionEndTime { get; private set; }

	public ReadOnlyCollection<Agent> PlayerSideAgents => _playerSideAgents.AsReadOnly();

	public ReadOnlyCollection<Agent> OpponentSideAgents => _opponentSideAgents.AsReadOnly();

	public bool IsPlayerSideWon => _isPlayerSideWon;

	public override void OnBehaviorInitialize()
	{
		base.Mission.IsAgentInteractionAllowed_AdditionalCondition += IsAgentInteractionAllowed_AdditionalCondition;
	}

	public override void EarlyStart()
	{
		_playerSideAgents = new List<Agent>();
		_opponentSideAgents = new List<Agent>();
	}

	public override void AfterStart()
	{
	}

	public override void OnMissionTick(float dt)
	{
		if (base.Mission.CurrentTime > MinMissionEndTime && _finishTimer != null && _finishTimer.ElapsedTime > 5f)
		{
			_finishTimer = null;
			EndFight();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (_state != State.Fighting)
		{
			return;
		}
		if (affectedAgent == Agent.Main)
		{
			Mission.Current.NextCheckTimeEndMission += 8f;
		}
		if (affectorAgent != null && _playerSideAgents.Contains(affectedAgent))
		{
			_playerSideAgents.Remove(affectedAgent);
			if (_playerSideAgents.Count == 0)
			{
				_isPlayerSideWon = false;
				_finishTimer = new BasicMissionTimer();
			}
		}
		else if (affectorAgent != null && _opponentSideAgents.Contains(affectedAgent))
		{
			_opponentSideAgents.Remove(affectedAgent);
			if (_opponentSideAgents.Count == 0)
			{
				_isPlayerSideWon = true;
				_finishTimer = new BasicMissionTimer();
			}
		}
	}

	public void StartCustomFight(List<Agent> playerSideAgents, List<Agent> opponentSideAgents, bool dropWeapons, bool isItemUseDisabled, OnFightEndDelegate onFightEndDelegate, float minimumEndTime = float.Epsilon)
	{
		StartFightInternal(playerSideAgents, opponentSideAgents, dropWeapons, isItemUseDisabled, onFightEndDelegate, minimumEndTime);
		SetTeamsForFightAndDuel();
		_oldMissionMode = Mission.Current.Mode;
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
	}

	public void StartFistFight(Agent opponent, OnFightEndDelegate onFightEndDelegate, float minimumEndTime = float.Epsilon)
	{
		StartFightInternal(new List<Agent> { Agent.Main }, new List<Agent> { opponent }, dropWeapons: false, isItemUseDisabled: false, delegate(bool playerWon)
		{
			AttachCachedEquipment(Agent.Main, opponent);
			onFightEndDelegate?.Invoke(playerWon);
		}, minimumEndTime);
		SetTeamsForFightAndDuel();
		_playerEquipment = new MissionEquipment();
		_opponentEquipment = new MissionEquipment();
		RemoveWeaponsFromAgents(Agent.Main, opponent);
		_oldMissionMode = Mission.Current.Mode;
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
	}

	private void RemoveWeaponsFromAgents(Agent main, Agent opponent)
	{
		_playerEquipment.FillFrom(main.Equipment);
		_opponentEquipment.FillFrom(opponent.Equipment);
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

	private void AttachCachedEquipment(Agent main, Agent opponent)
	{
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			MissionWeapon weapon = _playerEquipment[equipmentIndex];
			main.EquipWeaponWithNewEntity(equipmentIndex, ref weapon);
			MissionWeapon weapon2 = _opponentEquipment[equipmentIndex];
			opponent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon2);
		}
		_playerEquipment = null;
		_opponentEquipment = null;
	}

	private void StartFightInternal(List<Agent> playerSideAgents, List<Agent> opponentSideAgents, bool dropWeapons, bool isItemUseDisabled, OnFightEndDelegate onFightEndDelegate, float minimumEndTime = float.Epsilon)
	{
		_state = State.Fighting;
		_opponentSideAgents = opponentSideAgents;
		_playerSideAgents = playerSideAgents;
		_playerSideAgentsOldTeamData = new Dictionary<Agent, Team>();
		_opponentSideAgentsOldTeamData = new Dictionary<Agent, Team>();
		_onFightEnd = onFightEndDelegate;
		_isPlayerSideWon = false;
		Mission.Current.MainAgent.IsItemUseDisabled = isItemUseDisabled;
		foreach (Agent opponentSideAgent in _opponentSideAgents)
		{
			if (dropWeapons)
			{
				DropAllWeapons(opponentSideAgent);
			}
			_opponentSideAgentsOldTeamData.Add(opponentSideAgent, opponentSideAgent.Team);
			ForceAgentForFight(opponentSideAgent);
		}
		foreach (Agent playerSideAgent in _playerSideAgents)
		{
			if (dropWeapons)
			{
				DropAllWeapons(playerSideAgent);
			}
			_playerSideAgentsOldTeamData.Add(playerSideAgent, playerSideAgent.Team);
			ForceAgentForFight(playerSideAgent);
		}
		if (minimumEndTime > 0f && !minimumEndTime.ApproximatelyEqualsTo(float.Epsilon))
		{
			MinMissionEndTime = base.Mission.CurrentTime + minimumEndTime;
		}
		else
		{
			MinMissionEndTime = 0f;
		}
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (_state == State.Fighting && (_opponentSideAgents.Count > 0 || _playerSideAgents.Count > 0))
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=Fpk3BUBs}Your fight has not ended yet!"));
			canPlayerLeave = false;
		}
		return null;
	}

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

	protected override void OnEndMission()
	{
		base.Mission.IsAgentInteractionAllowed_AdditionalCondition -= IsAgentInteractionAllowed_AdditionalCondition;
	}

	private void SetTeamsForFightAndDuel()
	{
		Mission.Current.PlayerEnemyTeam.SetIsEnemyOf(Mission.Current.PlayerTeam, isEnemyOf: true);
		foreach (Agent playerSideAgent in _playerSideAgents)
		{
			if (playerSideAgent.IsHuman)
			{
				if (playerSideAgent.IsAIControlled)
				{
					playerSideAgent.SetWatchState(Agent.WatchState.Alarmed);
				}
				playerSideAgent.SetTeam(Mission.Current.PlayerTeam, sync: true);
			}
		}
		foreach (Agent opponentSideAgent in _opponentSideAgents)
		{
			if (opponentSideAgent.IsHuman)
			{
				if (opponentSideAgent.IsAIControlled)
				{
					opponentSideAgent.SetWatchState(Agent.WatchState.Alarmed);
				}
				opponentSideAgent.SetTeam(Mission.Current.PlayerEnemyTeam, sync: true);
			}
		}
	}

	private void ResetTeamsForFightAndDuel()
	{
		foreach (Agent playerSideAgent in _playerSideAgents)
		{
			if (playerSideAgent.IsAIControlled)
			{
				playerSideAgent.ResetEnemyCaches();
				playerSideAgent.InvalidateTargetAgent();
				playerSideAgent.InvalidateAIWeaponSelections();
				playerSideAgent.SetWatchState(Agent.WatchState.Patrolling);
			}
			playerSideAgent.SetTeam(new Team(_playerSideAgentsOldTeamData[playerSideAgent].MBTeam, BattleSideEnum.None, base.Mission), sync: true);
		}
		foreach (Agent opponentSideAgent in _opponentSideAgents)
		{
			if (opponentSideAgent.IsAIControlled)
			{
				opponentSideAgent.ResetEnemyCaches();
				opponentSideAgent.InvalidateTargetAgent();
				opponentSideAgent.InvalidateAIWeaponSelections();
				opponentSideAgent.SetWatchState(Agent.WatchState.Patrolling);
			}
			opponentSideAgent.SetTeam(new Team(_opponentSideAgentsOldTeamData[opponentSideAgent].MBTeam, BattleSideEnum.None, base.Mission), sync: true);
		}
	}

	private bool IsAgentInteractionAllowed_AdditionalCondition()
	{
		return _state != State.Fighting;
	}

	public static Agent GetAgentToSpectate()
	{
		MissionFightHandler current = _current;
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

	private void DropAllWeapons(Agent agent)
	{
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			if (!agent.Equipment[equipmentIndex].IsEmpty)
			{
				agent.DropItem(equipmentIndex);
			}
		}
	}

	private void ResetScriptedBehaviors()
	{
		foreach (Agent playerSideAgent in _playerSideAgents)
		{
			if (playerSideAgent.IsActive() && playerSideAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				playerSideAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().DisableScriptedBehavior();
			}
		}
		foreach (Agent opponentSideAgent in _opponentSideAgents)
		{
			if (opponentSideAgent.IsActive() && opponentSideAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				opponentSideAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().DisableScriptedBehavior();
			}
		}
	}

	public void BeginEndFight()
	{
		_finishTimer = new BasicMissionTimer();
	}

	public void EndFight(bool overrideDuelWonByPlayer = false)
	{
		ResetScriptedBehaviors();
		ResetTeamsForFightAndDuel();
		_state = State.FightEnded;
		foreach (Agent playerSideAgent in _playerSideAgents)
		{
			if (playerSideAgent.IsActive())
			{
				playerSideAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
				playerSideAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
			}
		}
		foreach (Agent opponentSideAgent in _opponentSideAgents)
		{
			if (opponentSideAgent.IsActive())
			{
				opponentSideAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
				opponentSideAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
			}
		}
		_playerSideAgents.Clear();
		_opponentSideAgents.Clear();
		if (Mission.Current.MainAgent != null)
		{
			Mission.Current.MainAgent.IsItemUseDisabled = false;
		}
		if (_oldMissionMode == MissionMode.Conversation && !Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			_oldMissionMode = MissionMode.StartUp;
		}
		Mission.Current.SetMissionMode(_oldMissionMode, atStart: false);
		if (_onFightEnd != null)
		{
			_onFightEnd(_isPlayerSideWon || overrideDuelWonByPlayer);
			_isPlayerSideWon = false;
			_onFightEnd = null;
		}
	}

	public bool IsThereActiveFight()
	{
		return _state == State.Fighting;
	}

	public void AddAgentToSide(Agent agent, bool isPlayerSide)
	{
		if (IsThereActiveFight() && !_playerSideAgents.Contains(agent) && !_opponentSideAgents.Contains(agent))
		{
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			if (isPlayerSide)
			{
				agent.SetTeam(Mission.Current.PlayerTeam, sync: true);
				_playerSideAgents.Add(agent);
				_playerSideAgentsOldTeamData.Add(agent, agent.Team);
			}
			else
			{
				agent.SetTeam(Mission.Current.PlayerEnemyTeam, sync: true);
				_opponentSideAgents.Add(agent);
				_opponentSideAgentsOldTeamData.Add(agent, agent.Team);
			}
			if (_playerSideAgents.Count == 0 || _opponentSideAgents.Count == 0)
			{
				_finishTimer = new BasicMissionTimer();
			}
			else
			{
				_finishTimer = null;
			}
			ForceAgentForFight(agent);
		}
	}

	public IEnumerable<Agent> GetDangerSources(Agent ownerAgent)
	{
		if (!(ownerAgent.Character is CharacterObject))
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\MissionFightHandler.cs", "GetDangerSources", 469);
			return new List<Agent>();
		}
		if (IsThereActiveFight() && !IsAgentAggressive(ownerAgent) && Agent.Main != null)
		{
			return new List<Agent> { Agent.Main };
		}
		return new List<Agent>();
	}

	public static bool IsAgentAggressive(Agent agent)
	{
		CharacterObject characterObject = agent.Character as CharacterObject;
		if (!agent.HasWeapon())
		{
			if (characterObject != null)
			{
				if (characterObject.Occupation != Occupation.Mercenary && !IsAgentVillian(characterObject))
				{
					return IsAgentJusticeWarrior(characterObject);
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public static bool IsAgentJusticeWarrior(CharacterObject character)
	{
		if (character.Occupation != Occupation.Soldier && character.Occupation != Occupation.Guard)
		{
			return character.Occupation == Occupation.PrisonGuard;
		}
		return true;
	}

	public static bool IsAgentVillian(CharacterObject character)
	{
		if (character.Occupation != Occupation.Gangster && character.Occupation != Occupation.GangLeader)
		{
			return character.Occupation == Occupation.Bandit;
		}
		return true;
	}
}
