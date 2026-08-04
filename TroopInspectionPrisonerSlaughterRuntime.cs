using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge.SiegeAftermathIntervention;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Bannerlord-only live adapter for the AF troop-inspection prisoner slaughter.
/// It deliberately does not share castle GCCZ state; only the proven combat
/// pattern is mirrored so the two mission lifecycles cannot activate each other.
/// </summary>
internal sealed class TroopInspectionPrisonerSlaughterRuntime
{
	private sealed class FightBehaviorSnapshot
	{
		internal AgentNavigator Navigator;

		internal AlarmedBehaviorGroup Group;

		internal bool GroupExisted;

		internal bool GroupWasActive;

		internal bool DisableCalmDown;

		internal bool FightBehaviorExisted;

		internal AgentBehavior ScriptedBehavior;

		internal Dictionary<AgentBehavior, bool> BehaviorActivity;
	}

	private const float CombatRefreshSeconds = 0.25f;

	private const float TargetMorale = 100f;

	private readonly Mission _mission;

	private readonly List<Agent> _attackers;

	private readonly List<Agent> _targets;

	private readonly HashSet<int> _attackerIndexes;

	private readonly HashSet<int> _targetIndexes;

	private readonly Dictionary<int, Formation> _attackerOriginalFormations;

	private readonly Dictionary<int, Team> _attackerOriginalTeams;

	private readonly Dictionary<int, AgentFlag> _attackerOriginalFlags;

	private readonly Dictionary<int, AgentControllerType> _attackerOriginalControllers;

	private readonly Dictionary<int, float> _attackerOriginalSpeedLimits;

	private readonly Dictionary<int, Formation> _targetOriginalFormations;

	private readonly Dictionary<int, Team> _targetOriginalTeams;

	private readonly Dictionary<int, AgentFlag> _targetOriginalFlags;

	private readonly HashSet<int> _combatReadyAttackerIndexes = new HashSet<int>();

	private readonly Dictionary<int, FightBehaviorSnapshot> _fightBehaviorSnapshots = new Dictionary<int, FightBehaviorSnapshot>();

	private readonly Action<Agent, bool> _restoreAgent;

	private Team _enemyTeam;

	private Formation _alliedFormation;

	private MovementOrder _alliedOriginalMovementOrder;

	private FiringOrder _alliedOriginalFiringOrder;

	private bool _alliedFormationCaptured;

	private bool _alliedFormationWasAiControlled;

	private MissionMode _originalMissionMode = MissionMode.Battle;

	private bool _missionModeChanged;

	private bool _hostilityCaptured;

	private bool _playerWasEnemyOfTargetTeam;

	private bool _targetTeamWasEnemyOfPlayer;

	private bool _restorePending;

	private float _nextCombatRefreshTime;

	private int _killedCount;

	private bool _finishRequested;

	private bool _cleaned;

	private bool _suppressCompletionMessage;

	internal bool IsActive { get; private set; }

	internal bool IsBusy => IsActive || _restorePending;

	internal bool HasPendingRestore => _restorePending;

	internal TroopInspectionPrisonerSlaughterRuntime(
		Mission mission,
		IEnumerable<Agent> attackers,
		IEnumerable<Agent> targets,
		Action<Agent, bool> restoreAgent)
	{
		_mission = mission;
		_attackers = (attackers ?? Enumerable.Empty<Agent>())
			.Where(IsLiveHuman)
			.Distinct()
			.ToList();
		_targets = (targets ?? Enumerable.Empty<Agent>())
			.Where(IsLiveHuman)
			.Distinct()
			.ToList();
		_attackerIndexes = new HashSet<int>(_attackers.Select(agent => agent.Index));
		_targetIndexes = new HashSet<int>(_targets.Select(agent => agent.Index));
		_attackerOriginalFormations = _attackers.ToDictionary(
			agent => agent.Index,
			agent => agent.Formation);
		_attackerOriginalTeams = _attackers.ToDictionary(
			agent => agent.Index,
			agent => agent.Team);
		_attackerOriginalFlags = _attackers.ToDictionary(
			agent => agent.Index,
			agent => agent.GetAgentFlags());
		_attackerOriginalControllers = _attackers.ToDictionary(
			agent => agent.Index,
			agent => agent.Controller);
		_attackerOriginalSpeedLimits = _attackers.ToDictionary(
			agent => agent.Index,
			agent => agent.GetMaximumSpeedLimit());
		_targetOriginalFormations = _targets.ToDictionary(
			agent => agent.Index,
			agent => agent.Formation);
		_targetOriginalTeams = _targets.ToDictionary(
			agent => agent.Index,
			agent => agent.Team);
		_targetOriginalFlags = _targets.ToDictionary(
			agent => agent.Index,
			agent => agent.GetAgentFlags());
		_restoreAgent = restoreAgent;
	}

	internal bool TryStart(out string reason)
	{
		reason = string.Empty;
		if (_mission == null || _mission.IsMissionEnding || _attackers.Count == 0)
		{
			reason = "not_normal_inspection";
			return false;
		}
		if (_targets.Count == 0)
		{
			reason = "no_regular_prisoners";
			return false;
		}

		try
		{
			Team playerTeam = _mission.PlayerTeam ?? Agent.Main?.Team;
			if (playerTeam == null)
			{
				reason = "combat_setup_unavailable";
				return false;
			}
			if (!TryEnterCombatMode())
			{
				reason = "combat_setup_unavailable";
				_restorePending = !RestoreMissionMode("start_failed_mode");
				return false;
			}
			_enemyTeam = EnsureEnemyTeam(_mission, playerTeam);
			if (_enemyTeam == null || _enemyTeam == playerTeam)
			{
				reason = "combat_setup_unavailable";
				_restorePending = !RestoreMissionMode("start_failed_team");
				if (!_restorePending)
				{
					_enemyTeam = null;
				}
				return false;
			}
			IsActive = true;
			if (!SetTeamHostility(playerTeam, hostile: true))
			{
				reason = "combat_setup_unavailable";
				IsActive = false;
				RestoreSurvivors("start_failed_hostility");
				return false;
			}

			ReleaseAfConversationControl();
			_nextCombatRefreshTime = 0f;
			RefreshCombat(forceWeaponSelection: true);
			Logger.Log(
				"TroopInspection",
				"[InspectionSlaughter] started attackers=" + _attackers.Count
				+ " targets=" + _targets.Count
				+ " mode=" + _mission.Mode
				+ " mutualHostility=" + playerTeam.IsEnemyOf(_enemyTeam));
			return true;
		}
		catch (Exception ex)
		{
			reason = "combat_setup_unavailable";
			Logger.Log("TroopInspection", "[InspectionSlaughter] start failed: " + ex);
			IsActive = false;
			if (_enemyTeam != null || _missionModeChanged || _hostilityCaptured)
			{
				RestoreSurvivors("start_failed");
			}
			return false;
		}
	}

	internal void Tick(float dt)
	{
		if (_restorePending)
		{
			RetryPendingMissionStateRestore("tick");
			return;
		}
		if (!IsActive || _cleaned || _mission == null || _mission.IsMissionEnding)
		{
			return;
		}
		try
		{
			float now = _mission.CurrentTime;
			if (now >= _nextCombatRefreshTime)
			{
				_nextCombatRefreshTime = now + CombatRefreshSeconds;
				RefreshCombat(forceWeaponSelection: false);
			}

			if (!_finishRequested && CountLiveTargets() == 0)
			{
				_finishRequested = true;
			}
			if (_finishRequested)
			{
				FinalizeFight("targets_exhausted");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] tick failed: " + ex.Message);
		}
	}

	internal void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState state)
	{
		if (!IsActive || affectedAgent == null || !_targetIndexes.Contains(affectedAgent.Index))
		{
			return;
		}
		if (state == AgentState.Killed)
		{
			_killedCount++;
		}
		Logger.Log(
			"TroopInspection",
			"[InspectionSlaughter] target removed agent=" + affectedAgent.Index
			+ " troop=" + ((affectedAgent.Character as CharacterObject)?.StringId ?? "unknown")
			+ " state=" + state
			+ " affector=" + (affectorAgent?.Index.ToString() ?? "null")
			+ " killed=" + _killedCount
			+ " remaining=" + CountLiveTargets());
		if (CountLiveTargets() == 0)
		{
			_finishRequested = true;
		}
	}

	internal bool ControlsAgent(Agent agent)
	{
		return IsActive
			&& agent != null
			&& (_attackerIndexes.Contains(agent.Index) || _targetIndexes.Contains(agent.Index));
	}

	internal void Cleanup(string reason)
	{
		if (_cleaned)
		{
			return;
		}
		_cleaned = true;
		_suppressCompletionMessage = true;
		if (IsActive || _missionModeChanged || _hostilityCaptured || _restorePending)
		{
			IsActive = false;
			RestoreSurvivors(reason ?? "cleanup");
		}
		Logger.Log(
			"TroopInspection",
			"[InspectionSlaughter] cleaned reason=" + (reason ?? "N/A")
			+ " killed=" + _killedCount);
	}

	private void RefreshCombat(bool forceWeaponSelection)
	{
		Team playerTeam = _mission?.PlayerTeam ?? Agent.Main?.Team;
		if (!IsActive || playerTeam == null || _enemyTeam == null)
		{
			return;
		}
		Formation enemyFormation = _enemyTeam.GetFormation(FormationClass.Infantry);
		Formation alliedFormation = playerTeam.GetFormation(FormationClass.Infantry);
		List<Agent> liveTargets = _targets.Where(IsLiveHuman).ToList();
		foreach (Agent target in liveTargets)
		{
			PrepareTarget(target, enemyFormation);
		}
		try
		{
			enemyFormation?.SetMovementOrder(MovementOrder.MovementOrderStop);
			if (CaptureAlliedFormationState(alliedFormation))
			{
				alliedFormation.SetControlledByAI(true, false);
				alliedFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
				alliedFormation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] formation order failed: " + ex.Message);
		}
		foreach (Agent attacker in _attackers.Where(IsLiveHuman))
		{
			PrepareAttacker(attacker, playerTeam, alliedFormation, forceWeaponSelection);
			Agent target = FindNearestTarget(attacker, liveTargets);
			if (target == null)
			{
				continue;
			}
			try
			{
				Agent currentTarget = BannerlordApiCompat.GetAgentCombatTarget(attacker);
				if (currentTarget == null
					|| !currentTarget.IsActive()
					|| !_targetIndexes.Contains(currentTarget.Index))
				{
					attacker.SetLookAgent(null);
					attacker.ResetEnemyCaches();
					attacker.InvalidateTargetAgent();
					attacker.InvalidateAIWeaponSelections();
					BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(attacker, enabled: false);
					BannerlordApiCompat.TrySetAgentCombatTarget(attacker, target);
					attacker.SetLookAgent(target);
					attacker.ForceAiBehaviorSelection();
					if (!ReferenceEquals(
						BannerlordApiCompat.GetAgentCombatTarget(attacker),
						target))
					{
						BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(
							attacker,
							enabled: true);
						attacker.ResetEnemyCaches();
						attacker.ForceAiBehaviorSelection();
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(
					"TroopInspection",
					"[InspectionSlaughter] target lock failed attacker=" + attacker.Index
					+ " error=" + ex.Message);
			}
		}
	}

	private void PrepareTarget(Agent target, Formation enemyFormation)
	{
		if (!IsLiveHuman(target))
		{
			return;
		}
		try
		{
			target.SetActionChannel(
				0,
				ActionIndexCache.act_none,
				true,
				(AnimFlags)0UL,
				0f,
				1f,
				-0.2f,
				0.4f,
				0f,
				false,
				-0.2f,
				0,
				true);
			target.SetMortalityState(Agent.MortalityState.Mortal);
			target.SetMaximumSpeedLimit(0f, false);
			target.SetIsAIPaused(isPaused: true);
			target.DisableScriptedMovement();
			target.Controller = AgentControllerType.AI;
			target.SetAgentFlags(
				target.GetAgentFlags()
				| AgentFlag.CanAttack
				| AgentFlag.CanDefend
				| AgentFlag.IsHumanoid
				| AgentFlag.CanGetAlarmed);
			if (target.Team != _enemyTeam)
			{
				target.SetTeam(_enemyTeam, sync: true);
			}
			if (enemyFormation != null && target.Formation != enemyFormation)
			{
				target.Formation = enemyFormation;
			}
			target.TryAttachToFormation();
			target.SetMorale(TargetMorale);
			target.StopRetreatingMoraleComponent();
			target.SetWatchState(Agent.WatchState.Alarmed);
			target.SetShouldCatchUpWithFormation(false);
			target.UpdateFormationOrders();
		}
		catch (Exception ex)
		{
			Logger.Log(
				"TroopInspection",
				"[InspectionSlaughter] prepare target failed agent=" + target.Index
				+ " error=" + ex.Message);
		}
	}

	private bool CaptureAlliedFormationState(Formation alliedFormation)
	{
		if (_alliedFormationCaptured)
		{
			return ReferenceEquals(_alliedFormation, alliedFormation);
		}
		if (alliedFormation == null)
		{
			return false;
		}
		try
		{
			MovementOrder movementOrder = alliedFormation.GetReadonlyMovementOrderReference();
			_alliedFormation = alliedFormation;
			_alliedOriginalMovementOrder = movementOrder;
			_alliedOriginalFiringOrder = alliedFormation.FiringOrder;
			_alliedFormationWasAiControlled = alliedFormation.IsAIControlled;
			_alliedFormationCaptured = true;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] capture formation state failed: " + ex.Message);
			return false;
		}
	}

	private void RestoreAlliedFormationState(string source)
	{
		if (!_alliedFormationCaptured || _alliedFormation == null)
		{
			return;
		}
		try
		{
			_alliedFormation.SetControlledByAI(_alliedFormationWasAiControlled, false);
			_alliedFormation.SetMovementOrder(_alliedOriginalMovementOrder);
			_alliedFormation.SetFiringOrder(_alliedOriginalFiringOrder);
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] restore formation state failed source="
				+ (source ?? "N/A") + " error=" + ex.Message);
		}
		finally
		{
			_alliedFormation = null;
			_alliedFormationCaptured = false;
		}
	}

	private void PrepareAttacker(
		Agent attacker,
		Team playerTeam,
		Formation alliedFormation,
		bool forceWeaponSelection)
	{
		if (!IsLiveHuman(attacker))
		{
			return;
		}
		try
		{
			if (attacker.Team != playerTeam)
			{
				attacker.SetTeam(playerTeam, sync: true);
			}
			if (alliedFormation != null && attacker.Formation != alliedFormation)
			{
				attacker.Formation = alliedFormation;
			}
			attacker.Controller = AgentControllerType.AI;
			attacker.SetIsAIPaused(isPaused: false);
			attacker.DisableScriptedMovement();
			attacker.SetMaximumSpeedLimit(-1f, false);
			attacker.SetAgentFlags(
				attacker.GetAgentFlags()
				| AgentFlag.CanAttack
				| AgentFlag.CanDefend
				| AgentFlag.IsHumanoid
				| AgentFlag.CanGetAlarmed
				| AgentFlag.CanWieldWeapon);
			attacker.SetWatchState(Agent.WatchState.Alarmed);
			ActivateFightBehavior(attacker);
			attacker.SetShouldCatchUpWithFormation(true);
			attacker.TryAttachToFormation();
			attacker.UpdateFormationOrders();
			if (_combatReadyAttackerIndexes.Add(attacker.Index))
			{
				ClearPresentationActions(attacker);
				attacker.ResetEnemyCaches();
				attacker.InvalidateTargetAgent();
				attacker.InvalidateAIWeaponSelections();
				forceWeaponSelection = true;
			}
			EquipPreferredWeapon(attacker, forceWeaponSelection);
		}
		catch (Exception ex)
		{
			Logger.Log(
				"TroopInspection",
				"[InspectionSlaughter] prepare attacker failed agent=" + attacker.Index
				+ " error=" + ex.Message);
		}
	}

	private void ReleaseAfConversationControl()
	{
		List<int> controlledAgents = _attackerIndexes
			.Concat(_targetIndexes)
			.Distinct()
			.ToList();
		ShoutBehavior.ReleaseSceneConversationAgentsForCombatExternal(
			controlledAgents,
			TroopInspectionPrisonerSlaughterProfile.StartSource);
		foreach (int agentIndex in controlledAgents)
		{
			ShoutBehavior.TryForceStopSceneFollowForExternal(
				agentIndex,
				TroopInspectionPrisonerSlaughterProfile.StartSource);
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(
				agentIndex,
				TroopInspectionPrisonerSlaughterProfile.StartSource);
		}
	}

	private static void ClearPresentationActions(Agent agent)
	{
		if (!IsLiveHuman(agent))
		{
			return;
		}
		try
		{
			agent.SetActionChannel(
				0,
				ActionIndexCache.act_none,
				true,
				(AnimFlags)0UL,
				0f,
				1f,
				-0.2f,
				0.4f,
				0f,
				false,
				-0.2f,
				0,
				true);
			agent.SetActionChannel(
				1,
				ActionIndexCache.act_none,
				true,
				(AnimFlags)0UL,
				0f,
				1f,
				-0.2f,
				0.4f,
				0f,
				false,
				-0.2f,
				0,
				true);
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] clear presentation failed agent="
				+ agent.Index + " error=" + ex.Message);
		}
	}

	private void ActivateFightBehavior(Agent agent)
	{
		try
		{
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			AgentNavigator navigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			AlarmedBehaviorGroup alarmedGroup = navigator?.GetBehaviorGroup<AlarmedBehaviorGroup>();
			bool groupExisted = alarmedGroup != null;
			alarmedGroup ??= navigator?.AddBehaviorGroup<AlarmedBehaviorGroup>();
			if (alarmedGroup == null)
			{
				return;
			}
			if (!_fightBehaviorSnapshots.ContainsKey(agent.Index))
			{
				FightBehavior existingFightBehavior = alarmedGroup.GetBehavior<FightBehavior>();
				_fightBehaviorSnapshots[agent.Index] = new FightBehaviorSnapshot
				{
					Navigator = navigator,
					Group = alarmedGroup,
					GroupExisted = groupExisted,
					GroupWasActive = alarmedGroup.IsActive,
					DisableCalmDown = alarmedGroup.DisableCalmDown,
					FightBehaviorExisted = existingFightBehavior != null,
					ScriptedBehavior = alarmedGroup.ScriptedBehavior,
					BehaviorActivity = alarmedGroup.Behaviors?
						.Where(behavior => behavior != null)
						.ToDictionary(behavior => behavior, behavior => behavior.IsActive)
						?? new Dictionary<AgentBehavior, bool>()
				};
			}
			alarmedGroup.DisableCalmDown = true;
			FightBehavior fightBehavior = alarmedGroup.GetBehavior<FightBehavior>()
				?? alarmedGroup.AddBehavior<FightBehavior>();
			if (fightBehavior != null)
			{
				alarmedGroup.SetScriptedBehavior<FightBehavior>();
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] activate fight behavior failed agent="
				+ (agent?.Index.ToString() ?? "null") + " error=" + ex.Message);
		}
	}

	private void RestoreFightBehavior(Agent agent)
	{
		try
		{
			if (agent == null || !_fightBehaviorSnapshots.TryGetValue(agent.Index, out FightBehaviorSnapshot snapshot))
			{
				return;
			}
			AlarmedBehaviorGroup alarmedGroup = snapshot.Group;
			if (alarmedGroup == null)
			{
				return;
			}
			alarmedGroup.DisableScriptedBehavior();
			if (!snapshot.FightBehaviorExisted)
			{
				alarmedGroup.RemoveBehavior<FightBehavior>();
			}
			if (!snapshot.GroupExisted)
			{
				snapshot.Navigator?.RemoveBehaviorGroup<AlarmedBehaviorGroup>();
				_fightBehaviorSnapshots.Remove(agent.Index);
				return;
			}
			alarmedGroup.IsActive = snapshot.GroupWasActive;
			RestoreScriptedBehavior(alarmedGroup, snapshot.ScriptedBehavior);
			foreach (KeyValuePair<AgentBehavior, bool> pair in snapshot.BehaviorActivity)
			{
				if (pair.Key != null && alarmedGroup.Behaviors.Contains(pair.Key))
				{
					pair.Key.IsActive = pair.Value;
				}
			}
			alarmedGroup.DisableCalmDown = snapshot.DisableCalmDown;
			_fightBehaviorSnapshots.Remove(agent.Index);
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] restore fight behavior failed agent="
				+ (agent?.Index.ToString() ?? "null") + " error=" + ex.Message);
		}
	}

	private static void RestoreScriptedBehavior(
		AlarmedBehaviorGroup alarmedGroup,
		AgentBehavior scriptedBehavior)
	{
		if (alarmedGroup == null
			|| scriptedBehavior == null
			|| alarmedGroup.Behaviors == null
			|| !alarmedGroup.Behaviors.Contains(scriptedBehavior))
		{
			return;
		}
		try
		{
			MethodInfo method = typeof(AgentBehaviorGroup)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.FirstOrDefault(candidate =>
					candidate.Name == nameof(AgentBehaviorGroup.SetScriptedBehavior)
					&& candidate.IsGenericMethodDefinition
					&& candidate.GetParameters().Length == 0);
			method?.MakeGenericMethod(scriptedBehavior.GetType()).Invoke(alarmedGroup, null);
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] restore scripted behavior failed: " + ex.Message);
		}
	}

	private static Team EnsureEnemyTeam(Mission mission, Team playerTeam)
	{
		try
		{
			Team enemy = mission?.PlayerEnemyTeam;
			if (enemy != null && enemy != playerTeam)
			{
				return enemy;
			}
			if (mission == null || playerTeam == null)
			{
				return null;
			}
			BattleSideEnum side = playerTeam.Side == BattleSideEnum.Defender
				? BattleSideEnum.Attacker
				: BattleSideEnum.Defender;
			return mission.Teams.Add(
				side,
				0xFF7A2020u,
				0xFF2A0808u,
				null,
				isPlayerGeneral: false,
				isPlayerSergeant: false);
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] create enemy team failed: " + ex.Message);
			return null;
		}
	}

	private bool SetTeamHostility(Team playerTeam, bool hostile)
	{
		if (!hostile && !_hostilityCaptured)
		{
			return true;
		}
		if (!hostile && (_mission == null || _mission.IsMissionEnding))
		{
			_hostilityCaptured = false;
			return true;
		}
		if (playerTeam == null || _enemyTeam == null || playerTeam == _enemyTeam)
		{
			return !hostile && !_hostilityCaptured;
		}
		if (hostile && !_hostilityCaptured)
		{
			try
			{
				_playerWasEnemyOfTargetTeam = playerTeam.IsEnemyOf(_enemyTeam);
				_targetTeamWasEnemyOfPlayer = _enemyTeam.IsEnemyOf(playerTeam);
				_hostilityCaptured = true;
			}
			catch (Exception ex)
			{
				Logger.Log("TroopInspection", "[InspectionSlaughter] capture team hostility failed: " + ex.Message);
				return false;
			}
		}

		bool enemyDirectionSet = TrySetEnemyOf(
			_enemyTeam,
			playerTeam,
			hostile || _targetTeamWasEnemyOfPlayer,
			"enemy_to_player");
		bool playerDirectionSet = TrySetEnemyOf(
			playerTeam,
			_enemyTeam,
			hostile || _playerWasEnemyOfTargetTeam,
			"player_to_enemy");
		if (enemyDirectionSet && playerDirectionSet)
		{
			if (!hostile)
			{
				_hostilityCaptured = false;
			}
			return true;
		}

		if (hostile && _hostilityCaptured)
		{
			bool enemyRolledBack = TrySetEnemyOf(
				_enemyTeam,
				playerTeam,
				_targetTeamWasEnemyOfPlayer,
				"rollback_enemy_to_player");
			bool playerRolledBack = TrySetEnemyOf(
				playerTeam,
				_enemyTeam,
				_playerWasEnemyOfTargetTeam,
				"rollback_player_to_enemy");
			if (enemyRolledBack && playerRolledBack)
			{
				_hostilityCaptured = false;
			}
		}
		return false;
	}

	private static bool TrySetEnemyOf(
		Team source,
		Team target,
		bool value,
		string direction)
	{
		try
		{
			source.SetIsEnemyOf(target, value);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] set team hostility failed direction="
				+ (direction ?? "N/A") + " value=" + value + " error=" + ex.Message);
			return false;
		}
	}

	private bool TryEnterCombatMode()
	{
		try
		{
			_originalMissionMode = _mission.Mode;
			_missionModeChanged = _mission.Mode != MissionMode.Battle;
			if (_missionModeChanged)
			{
				_mission.SetMissionMode(MissionMode.Battle, atStart: false);
			}
			return _mission.Mode == MissionMode.Battle;
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] enter combat mode failed: " + ex);
			return false;
		}
	}

	private bool RestoreMissionMode(string source)
	{
		if (!_missionModeChanged)
		{
			return true;
		}
		try
		{
			if (_mission != null && !_mission.IsMissionEnding)
			{
				_mission.SetMissionMode(_originalMissionMode, atStart: false);
			}
			_originalMissionMode = MissionMode.Battle;
			_missionModeChanged = false;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] restore mission mode failed source="
				+ (source ?? "N/A") + " error=" + ex.Message);
			return false;
		}
	}

	private void RetryPendingMissionStateRestore(string source)
	{
		Team playerTeam = _mission?.PlayerTeam ?? Agent.Main?.Team;
		bool hostilityRestored = SetTeamHostility(playerTeam, hostile: false);
		bool missionModeRestored = RestoreMissionMode(source);
		_restorePending = !hostilityRestored || !missionModeRestored;
		if (!_restorePending)
		{
			_enemyTeam = null;
			Logger.Log("TroopInspection", "[InspectionSlaughter] pending mission state restored source="
				+ (source ?? "N/A"));
		}
	}

	private void FinalizeFight(string source)
	{
		if (!IsActive)
		{
			return;
		}
		IsActive = false;
		RestoreSurvivors(source);
		if (!_suppressCompletionMessage)
		{
			InformationManager.DisplayMessage(new InformationMessage(
				TroopInspectionPrisonerSlaughterProfile.BuildCompletedMessage(_killedCount),
				Color.FromUint(TroopInspectionPrisonerSlaughterProfile.CompletionMessageColor)));
		}
		Logger.Log(
			"TroopInspection",
			"[InspectionSlaughter] completed killed=" + _killedCount
			+ " source=" + (source ?? "N/A"));
	}

	private void RestoreSurvivors(string source)
	{
		Team playerTeam = _mission?.PlayerTeam ?? Agent.Main?.Team;
		foreach (Agent attacker in _attackers.Where(IsLiveHuman))
		{
			try
			{
				RestoreFightBehavior(attacker);
				BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(attacker, enabled: true);
				BannerlordApiCompat.TrySetAgentCombatTarget(attacker, null);
				attacker.SetLookAgent(null);
				attacker.InvalidateTargetAgent();
				attacker.ResetEnemyCaches();
				attacker.SetWatchState(Agent.WatchState.Patrolling);
				Team originalTeam = _attackerOriginalTeams.TryGetValue(
					attacker.Index,
					out Team savedTeam)
					? savedTeam
					: playerTeam;
				if (originalTeam != null && attacker.Team != originalTeam)
				{
					attacker.SetTeam(originalTeam, sync: true);
				}
				if (_attackerOriginalFormations.TryGetValue(
					attacker.Index,
					out Formation formation))
				{
					attacker.Formation = formation;
					if (formation != null)
					{
						attacker.TryAttachToFormation();
					}
				}
				_restoreAgent?.Invoke(attacker, false);
				if (_attackerOriginalFlags.TryGetValue(attacker.Index, out AgentFlag originalFlags))
				{
					attacker.SetAgentFlags(originalFlags);
				}
				if (_attackerOriginalSpeedLimits.TryGetValue(attacker.Index, out float originalSpeedLimit))
				{
					attacker.SetMaximumSpeedLimit(originalSpeedLimit, false);
				}
				if (_attackerOriginalControllers.TryGetValue(
					attacker.Index,
					out AgentControllerType originalController))
				{
					attacker.Controller = originalController;
				}
			}
			catch (Exception ex)
			{
				Logger.Log(
					"TroopInspection",
					"[InspectionSlaughter] restore attacker failed agent=" + attacker.Index
					+ " source=" + (source ?? "N/A")
					+ " error=" + ex.Message);
			}
		}
		RestoreAlliedFormationState(source);

		foreach (Agent target in _targets.Where(IsLiveHuman))
		{
			try
			{
				BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(target, enabled: true);
				BannerlordApiCompat.TrySetAgentCombatTarget(target, null);
				target.SetLookAgent(null);
				target.InvalidateTargetAgent();
				target.ResetEnemyCaches();
				Team originalTeam = _targetOriginalTeams.TryGetValue(
					target.Index,
					out Team savedTeam)
					? savedTeam
					: playerTeam;
				if (originalTeam != null && target.Team != originalTeam)
				{
					target.SetTeam(originalTeam, sync: true);
				}
				if (_targetOriginalFormations.TryGetValue(
					target.Index,
					out Formation originalFormation))
				{
					target.Formation = originalFormation;
					if (originalFormation != null)
					{
						target.TryAttachToFormation();
					}
				}
				if (_targetOriginalFlags.TryGetValue(target.Index, out AgentFlag originalFlags))
				{
					target.SetAgentFlags(originalFlags);
				}
				target.SetMortalityState(Agent.MortalityState.Immortal);
				_restoreAgent?.Invoke(target, true);
			}
			catch (Exception ex)
			{
				Logger.Log(
					"TroopInspection",
					"[InspectionSlaughter] restore prisoner failed agent=" + target.Index
					+ " source=" + (source ?? "N/A")
					+ " error=" + ex.Message);
			}
		}
		bool hostilityRestored = SetTeamHostility(playerTeam, hostile: false);
		bool missionModeRestored = RestoreMissionMode(source);
		_restorePending = !hostilityRestored || !missionModeRestored;
		if (!_restorePending)
		{
			_enemyTeam = null;
		}
		else
		{
			Logger.Log("TroopInspection", "[InspectionSlaughter] mission state restore pending source="
				+ (source ?? "N/A")
				+ " hostility=" + hostilityRestored
				+ " mode=" + missionModeRestored);
		}
		_combatReadyAttackerIndexes.Clear();
		_fightBehaviorSnapshots.Clear();
	}

	private int CountLiveTargets()
	{
		return _targets.Count(IsLiveHuman);
	}

	private static Agent FindNearestTarget(Agent source, IReadOnlyList<Agent> targets)
	{
		Agent nearest = null;
		float nearestDistance = float.MaxValue;
		if (source == null || targets == null)
		{
			return null;
		}
		foreach (Agent target in targets)
		{
			if (!IsLiveHuman(target))
			{
				continue;
			}
			float distance = source.Position.DistanceSquared(target.Position);
			if (distance < nearestDistance)
			{
				nearest = target;
				nearestDistance = distance;
			}
		}
		return nearest;
	}

	private static bool EquipPreferredWeapon(Agent agent, bool forceSelection)
	{
		if (!IsLiveHuman(agent))
		{
			return false;
		}
		try
		{
			if (!forceSelection && IsCombatWeaponWielded(agent))
			{
				return true;
			}
			EquipmentIndex rangedSlot = FindWeaponSlot(agent, preferRanged: true);
			EquipmentIndex meleeSlot = FindWeaponSlot(agent, preferRanged: false);
			bool useRanged = rangedSlot != EquipmentIndex.None;
			EquipmentIndex preferredSlot = useRanged ? rangedSlot : meleeSlot;
			if (preferredSlot == EquipmentIndex.None)
			{
				return false;
			}
			agent.InvalidateAIWeaponSelections();
			agent.WieldInitialWeapons(
				Agent.WeaponWieldActionType.InstantAfterPickUp,
				useRanged
					? Equipment.InitialWeaponEquipPreference.RangedForMainHand
					: Equipment.InitialWeaponEquipPreference.MeleeForMainHand);
			if (!IsCombatWeaponWielded(agent))
			{
				agent.TryToWieldWeaponInSlot(
					preferredSlot,
					Agent.WeaponWieldActionType.InstantAfterPickUp,
					isWieldedOnSpawn: false);
			}
			return IsCombatWeaponWielded(agent);
		}
		catch
		{
			return false;
		}
	}

	private static EquipmentIndex FindWeaponSlot(Agent agent, bool preferRanged)
	{
		for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot;
			slot < EquipmentIndex.NumAllWeaponSlots;
			slot++)
		{
			MissionWeapon weapon = agent.Equipment[slot];
			if (weapon.IsEmpty || weapon.Item?.Weapons == null)
			{
				continue;
			}
			bool matches = weapon.Item.Weapons.Any(usage =>
				usage != null
				&& !usage.IsAmmo
				&& !usage.IsShield
				&& (preferRanged ? usage.IsRangedWeapon : usage.IsMeleeWeapon));
			if (matches)
			{
				return slot;
			}
		}
		return EquipmentIndex.None;
	}

	private static bool IsCombatWeaponWielded(Agent agent)
	{
		return IsCombatWeaponWielded(agent, agent.GetPrimaryWieldedItemIndex())
			|| IsCombatWeaponWielded(agent, agent.GetOffhandWieldedItemIndex());
	}

	private static bool IsCombatWeaponWielded(Agent agent, EquipmentIndex slot)
	{
		if (slot == EquipmentIndex.None
			|| slot < EquipmentIndex.WeaponItemBeginSlot
			|| slot >= EquipmentIndex.NumAllWeaponSlots)
		{
			return false;
		}
		MissionWeapon weapon = agent.Equipment[slot];
		WeaponComponentData usage = weapon.CurrentUsageItem;
		return !weapon.IsEmpty
			&& usage != null
			&& !usage.IsAmmo
			&& !usage.IsShield
			&& (usage.IsMeleeWeapon || usage.IsRangedWeapon);
	}

	private static bool IsLiveHuman(Agent agent)
	{
		return agent != null && agent.IsHuman && agent.IsActive();
	}
}
