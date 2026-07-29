using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using SandBox.Missions.MissionLogics;
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
	private const float CombatRefreshSeconds = 0.25f;

	private const float TargetMorale = 100f;

	private readonly Mission _mission;

	private readonly List<Agent> _attackers;

	private readonly List<Agent> _targets;

	private readonly HashSet<int> _attackerIndexes;

	private readonly HashSet<int> _targetIndexes;

	private readonly Dictionary<int, Formation> _attackerOriginalFormations;

	private readonly Action<Agent, bool> _restoreAgent;

	private MissionFightHandler _fightHandler;

	private float _nextCombatRefreshTime;

	private int _killedCount;

	private bool _finishRequested;

	private bool _cleaned;

	private bool _suppressCompletionMessage;

	internal bool IsActive { get; private set; }

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
			_fightHandler = _mission.GetMissionBehavior<MissionFightHandler>();
			if (_fightHandler == null || _fightHandler.IsThereActiveFight())
			{
				reason = "native_fight_unavailable";
				return false;
			}

			ReleaseAfConversationControl();
			foreach (Agent target in _targets)
			{
				PrepareTarget(target);
			}
			foreach (Agent attacker in _attackers)
			{
				PrepareAttacker(attacker, forceWeaponSelection: true);
			}

			IsActive = true;
			_nextCombatRefreshTime = 0f;
			_fightHandler.StartCustomFight(
				_attackers,
				_targets,
				dropWeapons: false,
				isItemUseDisabled: false,
				OnFightEnded,
				float.Epsilon);
			RefreshCombat(forceWeaponSelection: true);
			Logger.Log(
				"TroopInspection",
				"[InspectionSlaughter] started attackers=" + _attackers.Count
				+ " targets=" + _targets.Count
				+ " mode=" + _mission.Mode);
			return true;
		}
		catch (Exception ex)
		{
			reason = "native_fight_unavailable";
			Logger.Log("TroopInspection", "[InspectionSlaughter] start failed: " + ex);
			try
			{
				if (_fightHandler?.IsThereActiveFight() == true)
				{
					_fightHandler.EndFight(false);
				}
			}
			catch
			{
			}
			IsActive = false;
			RestoreSurvivors("start_failed");
			return false;
		}
	}

	internal void Tick(float dt)
	{
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
				if (_fightHandler?.IsThereActiveFight() == true)
				{
					_fightHandler.EndFight(false);
				}
				else
				{
					FinalizeFight(playerSideWon: true, "targets_exhausted_without_callback");
				}
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
		try
		{
			if (_fightHandler?.IsThereActiveFight() == true)
			{
				_fightHandler.EndFight(false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log(
				"TroopInspection",
				"[InspectionSlaughter] end native fight during cleanup failed reason="
				+ (reason ?? "N/A")
				+ " error=" + ex.Message);
		}
		if (IsActive)
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
		if (!IsActive)
		{
			return;
		}
		List<Agent> liveTargets = _targets.Where(IsLiveHuman).ToList();
		foreach (Agent target in liveTargets)
		{
			PrepareTarget(target);
		}
		foreach (Agent attacker in _attackers.Where(IsLiveHuman))
		{
			PrepareAttacker(attacker, forceWeaponSelection);
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

	private void PrepareTarget(Agent target)
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
			target.SetMorale(TargetMorale);
			target.StopRetreatingMoraleComponent();
			target.SetWatchState(Agent.WatchState.Alarmed);
			target.SetShouldCatchUpWithFormation(false);
		}
		catch (Exception ex)
		{
			Logger.Log(
				"TroopInspection",
				"[InspectionSlaughter] prepare target failed agent=" + target.Index
				+ " error=" + ex.Message);
		}
	}

	private void PrepareAttacker(Agent attacker, bool forceWeaponSelection)
	{
		if (!IsLiveHuman(attacker))
		{
			return;
		}
		try
		{
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
			attacker.SetShouldCatchUpWithFormation(true);
			attacker.UpdateFormationOrders();
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

	private void OnFightEnded(bool playerSideWon)
	{
		FinalizeFight(playerSideWon, "native_callback");
	}

	private void FinalizeFight(bool playerSideWon, string source)
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
			"[InspectionSlaughter] completed player_side_won=" + playerSideWon
			+ " killed=" + _killedCount
			+ " source=" + (source ?? "N/A"));
	}

	private void RestoreSurvivors(string source)
	{
		Team playerTeam = _mission?.PlayerTeam ?? Agent.Main?.Team;
		foreach (Agent attacker in _attackers.Where(IsLiveHuman))
		{
			try
			{
				BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(attacker, enabled: true);
				BannerlordApiCompat.TrySetAgentCombatTarget(attacker, null);
				attacker.SetLookAgent(null);
				attacker.InvalidateTargetAgent();
				attacker.ResetEnemyCaches();
				attacker.SetWatchState(Agent.WatchState.Patrolling);
				if (playerTeam != null && attacker.Team != playerTeam)
				{
					attacker.SetTeam(playerTeam, sync: true);
				}
				if (_attackerOriginalFormations.TryGetValue(
					attacker.Index,
					out Formation formation)
					&& formation != null)
				{
					attacker.Formation = formation;
					attacker.TryAttachToFormation();
				}
				_restoreAgent?.Invoke(attacker, false);
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

		foreach (Agent target in _targets.Where(IsLiveHuman))
		{
			try
			{
				BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(target, enabled: true);
				BannerlordApiCompat.TrySetAgentCombatTarget(target, null);
				target.SetLookAgent(null);
				target.InvalidateTargetAgent();
				target.ResetEnemyCaches();
				if (playerTeam != null && target.Team != playerTeam)
				{
					target.SetTeam(playerTeam, sync: true);
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
