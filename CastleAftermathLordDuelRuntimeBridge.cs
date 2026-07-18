using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Thin Bannerlord adapter for the castle captive-lord duel. Consent and result
/// wording live in the standalone GCCZ core; this class owns mission state only.
/// </summary>
internal static class CastleAftermathLordDuelRuntimeBridge
{
	private static CastleAftermathLordDuelMissionBehavior _activeBehavior;

	internal static void AttachMissionBehavior(Mission mission)
	{
		if (mission == null)
		{
			return;
		}
		CastleAftermathLordDuelMissionBehavior behavior = mission.GetMissionBehavior<CastleAftermathLordDuelMissionBehavior>();
		if (behavior == null)
		{
			behavior = new CastleAftermathLordDuelMissionBehavior();
			mission.AddMissionBehavior(behavior);
		}
		_activeBehavior = behavior;
	}

	internal static bool TryBegin(Hero hero, Agent agent, out string reasonCode)
	{
		Mission mission = Mission.Current;
		CastleAftermathLordDuelMissionBehavior behavior = ResolveBehavior(mission);
		if (behavior == null)
		{
			reasonCode = "castle_duel_behavior_missing";
			return false;
		}
		return behavior.TryBegin(hero, agent, out reasonCode);
	}

	internal static bool ControlsAgent(Agent agent)
	{
		try
		{
			return agent != null && ResolveBehavior(agent.Mission)?.ControlsAgent(agent) == true;
		}
		catch
		{
			return false;
		}
	}

	internal static void ProtectCapturedLord(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !CastleAftermathRuntimeBridge.IsLordPrisonerAgent(agent))
		{
			return;
		}
		CastleAftermathLordDuelMissionBehavior behavior = ResolveBehavior(agent.Mission);
		if (behavior?.IsFightingAgent(agent) == true)
		{
			return;
		}
		try
		{
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
		}
		catch
		{
		}
	}

	internal static bool IsPlayerMounted()
	{
		try
		{
			return Agent.Main?.MountAgent?.IsActive() == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool PlayerCarriesRangedWeapon()
	{
		return HasRangedWeapon(Agent.Main, wieldedOnly: false);
	}

	internal static bool PlayerWieldsRangedWeapon()
	{
		return HasRangedWeapon(Agent.Main, wieldedOnly: true);
	}

	internal static void CancelForMission(Mission mission, string source)
	{
		CastleAftermathLordDuelMissionBehavior behavior = ResolveBehavior(mission);
		behavior?.Cancel(source ?? "castle_duel_cancelled", showMessage: false);
	}

	internal static void Clear(Mission mission)
	{
		if (_activeBehavior != null && (mission == null || ReferenceEquals(_activeBehavior.Mission, mission)))
		{
			_activeBehavior = null;
		}
	}

	private static CastleAftermathLordDuelMissionBehavior ResolveBehavior(Mission mission)
	{
		if (mission == null)
		{
			return null;
		}
		if (_activeBehavior != null && ReferenceEquals(_activeBehavior.Mission, mission))
		{
			return _activeBehavior;
		}
		_activeBehavior = mission.GetMissionBehavior<CastleAftermathLordDuelMissionBehavior>();
		return _activeBehavior;
	}

	private static bool HasRangedWeapon(Agent agent, bool wieldedOnly)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			if (wieldedOnly)
			{
				return IsRanged(agent, agent.GetPrimaryWieldedItemIndex())
					|| IsRanged(agent, agent.GetOffhandWieldedItemIndex());
			}
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
			{
				if (IsRanged(agent, slot))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsRanged(Agent agent, EquipmentIndex slot)
	{
		if (agent == null || slot == EquipmentIndex.None)
		{
			return false;
		}
		MissionWeapon weapon = agent.Equipment[slot];
		return !weapon.IsEmpty && weapon.CurrentUsageItem?.IsRangedWeapon == true;
	}
}

internal sealed class CastleAftermathLordDuelMissionBehavior : MissionLogic
{
	private enum RuntimeStage
	{
		Idle,
		ApproachingWeapon,
		Fighting,
		Finishing
	}

	private enum DuelLoadoutKind
	{
		None,
		OneHandedAndShield,
		TwoHanded
	}

	private sealed class AgentSnapshot
	{
		internal Agent Agent;
		internal Team Team;
		internal Formation Formation;
		internal AgentControllerType Controller;
		internal Agent.MortalityState Mortality;
		internal float Health;
	}

	private readonly Dictionary<int, AgentSnapshot> _audienceSnapshots = new Dictionary<int, AgentSnapshot>();
	private readonly HashSet<int> _controlledAgentIndexes = new HashSet<int>();
	private RuntimeStage _stage;
	private Hero _lordHero;
	private Agent _lordAgent;
	private AgentSnapshot _playerSnapshot;
	private AgentSnapshot _playerMountSnapshot;
	private AgentSnapshot _lordSnapshot;
	private Team _originalMissionPlayerTeam;
	private Team _playerDuelTeam;
	private Team _lordDuelTeam;
	private Team _playerMountTeam;
	private MissionMode _originalMissionMode = MissionMode.Battle;
	private bool _missionModeChanged;
	private bool _duelTeamRelationsCaptured;
	private bool _playerTeamWasEnemyOfLordTeam;
	private bool _lordTeamWasEnemyOfPlayerTeam;
	private Vec3 _weaponPosition;
	private Vec3 _shieldPosition;
	private Vec3 _arenaCenter;
	private SpawnedItemEntity _weaponVisual;
	private SpawnedItemEntity _shieldVisual;
	private ItemObject _duelWeaponItem;
	private ItemObject _duelShieldItem;
	private DuelLoadoutKind _duelLoadoutKind;
	private float _approachStartedAt;
	private float _nextCombatRefreshAt;
	private float _playerVirtualHealth;
	private float _lordVirtualHealth;
	private bool _playerWasMountedWhenAccepted;
	private bool _playerCarriedRangedWeaponWhenAccepted;
	private bool _playerMountedDuringDuel;
	private bool _playerUsedRangedWeapon;
	private bool _pendingPlayerWon;
	private string _cancelSource;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	internal bool ControlsAgent(Agent agent)
	{
		return _stage != RuntimeStage.Idle
			&& agent != null
			&& _controlledAgentIndexes.Contains(agent.Index);
	}

	internal bool IsFightingAgent(Agent agent)
	{
		return _stage == RuntimeStage.Fighting && ReferenceEquals(agent, _lordAgent);
	}

	internal bool TryBegin(Hero hero, Agent agent, out string reasonCode)
	{
		reasonCode = string.Empty;
		Mission mission = base.Mission;
		Agent player = Agent.Main;
		if (_stage != RuntimeStage.Idle)
		{
			reasonCode = "castle_duel_already_active";
			return false;
		}
		if (mission == null || mission.IsMissionEnding || !CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
		{
			reasonCode = "castle_duel_mission_unavailable";
			return false;
		}
		if (CastleAftermathRuntimeBridge.IsRegularPrisonerSlaughterActive(mission))
		{
			reasonCode = "castle_slaughter_active";
			return false;
		}
		if (player == null || !player.IsActive() || player.State == AgentState.Killed || player.State == AgentState.Unconscious)
		{
			reasonCode = "castle_duel_player_unavailable";
			return false;
		}
		if (hero == null || !hero.IsAlive || !hero.IsLord || hero == Hero.MainHero
			|| agent == null || !agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious
			|| !CastleAftermathRuntimeBridge.IsLordPrisonerAgent(agent)
			|| !CastleAftermathRuntimeBridge.ContainsSelectedLord(hero))
		{
			reasonCode = "castle_duel_invalid_captive_lord";
			return false;
		}
		Hero agentHero = (agent.Character as CharacterObject)?.HeroObject;
		if (agentHero != hero)
		{
			reasonCode = "castle_duel_hero_agent_mismatch";
			return false;
		}

		if (!TryResolveRandomDuelLoadout(
			out int oneHandedCandidateCount,
			out int twoHandedCandidateCount,
			out int shieldCandidateCount))
		{
			reasonCode = "castle_duel_loadout_missing";
			return false;
		}
		int selectedWeaponTier = GetDisplayTier(_duelWeaponItem);
		string selectedWeaponName = _duelWeaponItem.Name?.ToString() ?? _duelWeaponItem.StringId ?? "N/A";
		int selectedShieldTier = GetDisplayTier(_duelShieldItem);
		string selectedShieldName = _duelShieldItem?.Name?.ToString() ?? _duelShieldItem?.StringId ?? string.Empty;
		string loadoutDescription = _duelLoadoutKind == DuelLoadoutKind.OneHandedAndShield
			? selectedWeaponName + "（" + selectedWeaponTier + "级）和" + selectedShieldName + "（" + selectedShieldTier + "级）"
			: selectedWeaponName + "（" + selectedWeaponTier + "级）";

		_lordHero = hero;
		_lordAgent = agent;
		_playerSnapshot = Capture(player);
		_playerMountSnapshot = player.MountAgent?.IsActive() == true ? Capture(player.MountAgent) : null;
		_lordSnapshot = Capture(agent);
		_originalMissionPlayerTeam = mission.PlayerTeam;
		_playerMountTeam = player.MountAgent?.Team;
		_originalMissionMode = mission.Mode;
		_missionModeChanged = false;
		_playerWasMountedWhenAccepted = CastleAftermathLordDuelRuntimeBridge.IsPlayerMounted();
		_playerCarriedRangedWeaponWhenAccepted = CastleAftermathLordDuelRuntimeBridge.PlayerCarriesRangedWeapon();
		_playerMountedDuringDuel = _playerWasMountedWhenAccepted;
		_playerUsedRangedWeapon = false;
		_playerVirtualHealth = Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, player.Health);
		_lordVirtualHealth = Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, agent.Health);
		ResolveArenaPoints(mission, player);

		if (!TrySpawnLoadoutVisuals(mission))
		{
			ResetFields();
			reasonCode = "castle_duel_loadout_spawn_failed";
			return false;
		}

		_stage = RuntimeStage.ApproachingWeapon;
		_controlledAgentIndexes.Add(agent.Index);
		PrepareAudience(mission, player, agent);
		if (!TryInitializeDuelTeams(mission, player, agent))
		{
			Cancel("castle_duel_team_initialization_failed", showMessage: false);
			reasonCode = "castle_duel_team_initialization_failed";
			return false;
		}
		if (!TryEnterDuelMissionMode(mission))
		{
			Cancel("castle_duel_mission_mode_failed", showMessage: false);
			reasonCode = "castle_duel_mission_mode_failed";
			return false;
		}
		// Do not close AF's active conversation until every reversible duel
		// prerequisite has succeeded. A failed setup must leave the player in the
		// same interaction state instead of stranding the processing overlay.
		EndCurrentConversation();

		PrepareLordForWeaponApproach(mission, agent);
		_approachStartedAt = mission.CurrentTime;
		Logger.Log("CastleAftermath", "Started captive-lord duel approach. Hero=" + (hero.StringId ?? "N/A")
			+ ", Agent=" + agent.Index + ", WeaponDistance=" + SiegeCastleLordDuelProfile.WeaponForwardDistance
			+ ", Weapon=" + (_duelWeaponItem.StringId ?? "N/A") + ", WeaponTier=" + selectedWeaponTier
			+ ", Shield=" + (_duelShieldItem?.StringId ?? "N/A") + ", ShieldTier=" + selectedShieldTier
			+ ", Loadout=" + _duelLoadoutKind + ", OneHandedPool=" + oneHandedCandidateCount
			+ ", TwoHandedPool=" + twoHandedCandidateCount + ", ShieldPool=" + shieldCandidateCount
			+ ", Audience=" + _audienceSnapshots.Count + ", Mounted=" + _playerWasMountedWhenAccepted
			+ ", CarriesRanged=" + _playerCarriedRangedWeaponWhenAccepted);
		GcczDiagnosticLog.Log("CastleLordDuel", "started hero=" + (hero.StringId ?? "N/A")
			+ " agent=" + agent.Index + " audience=" + _audienceSnapshots.Count
			+ " weapon=" + (_duelWeaponItem.StringId ?? "N/A") + " tier=" + selectedWeaponTier
			+ " shield=" + (_duelShieldItem?.StringId ?? "N/A") + " shieldTier=" + selectedShieldTier
			+ " loadout=" + _duelLoadoutKind + " oneHandedPool=" + oneHandedCandidateCount
			+ " twoHandedPool=" + twoHandedCandidateCount + " shieldPool=" + shieldCandidateCount
			+ " mounted=" + _playerWasMountedWhenAccepted + " ranged=" + _playerCarriedRangedWeaponWhenAccepted);
		AnimusForgeQuickInfo.Show("【城堡决斗】众人正在散开，俘虏领主正走向前方的"
			+ loadoutDescription + "。双方在其拿起装备前均不会受伤。", agent.Character as CharacterObject);
		return true;
	}

	private bool TryResolveRandomDuelLoadout(
		out int oneHandedCandidateCount,
		out int twoHandedCandidateCount,
		out int shieldCandidateCount)
	{
		oneHandedCandidateCount = 0;
		twoHandedCandidateCount = 0;
		shieldCandidateCount = 0;
		_duelWeaponItem = null;
		_duelShieldItem = null;
		_duelLoadoutKind = DuelLoadoutKind.None;
		try
		{
			List<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>()?.ToList()
				?? new List<ItemObject>();
			List<ItemObject> oneHandedWeapons = items
				.Where(item => IsEligibleDuelWeapon(item, ItemObject.ItemTypeEnum.OneHandedWeapon))
				.OrderBy(item => item.StringId ?? string.Empty, StringComparer.Ordinal)
				.ToList();
			List<ItemObject> twoHandedWeapons = items
				.Where(item => IsEligibleDuelWeapon(item, ItemObject.ItemTypeEnum.TwoHandedWeapon))
				.OrderBy(item => item.StringId ?? string.Empty, StringComparer.Ordinal)
				.ToList();
			List<ItemObject> shields = items
				.Where(IsEligibleDuelShield)
				.OrderBy(item => item.StringId ?? string.Empty, StringComparer.Ordinal)
				.ToList();
			oneHandedCandidateCount = oneHandedWeapons.Count;
			twoHandedCandidateCount = twoHandedWeapons.Count;
			shieldCandidateCount = shields.Count;

			bool oneHandedLoadoutAvailable = oneHandedCandidateCount > 0 && shieldCandidateCount > 0;
			bool twoHandedLoadoutAvailable = twoHandedCandidateCount > 0;
			if (!oneHandedLoadoutAvailable && !twoHandedLoadoutAvailable)
			{
				Logger.Log("CastleAftermath", "No eligible tier 4-6 captive-lord duel loadout was available."
					+ " OneHandedPool=" + oneHandedCandidateCount
					+ ", TwoHandedPool=" + twoHandedCandidateCount
					+ ", ShieldPool=" + shieldCandidateCount);
				return false;
			}

			bool useOneHandedLoadout = oneHandedLoadoutAvailable
				&& (!twoHandedLoadoutAvailable || MBRandom.RandomInt(2) == 0);
			if (useOneHandedLoadout)
			{
				_duelWeaponItem = oneHandedWeapons[MBRandom.RandomInt(oneHandedCandidateCount)];
				_duelShieldItem = shields[MBRandom.RandomInt(shieldCandidateCount)];
				_duelLoadoutKind = DuelLoadoutKind.OneHandedAndShield;
			}
			else
			{
				_duelWeaponItem = twoHandedWeapons[MBRandom.RandomInt(twoHandedCandidateCount)];
				_duelLoadoutKind = DuelLoadoutKind.TwoHanded;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Build captive-lord duel loadout pools failed: " + ex.Message);
			return false;
		}
	}

	private static bool IsEligibleDuelWeapon(ItemObject item, ItemObject.ItemTypeEnum requiredType)
	{
		if (item == null || item.PrimaryWeapon == null || item.ItemType != requiredType)
		{
			return false;
		}
		return SiegeCastleLordDuelProfile.IsEligibleWeapon(
			GetDisplayTier(item),
			requiredType == ItemObject.ItemTypeEnum.OneHandedWeapon,
			requiredType == ItemObject.ItemTypeEnum.TwoHandedWeapon,
			item.PrimaryWeapon.IsMeleeWeapon,
			isMerchandise: !item.NotMerchandise);
	}

	private static bool IsEligibleDuelShield(ItemObject item)
	{
		return item != null
			&& item.PrimaryWeapon != null
			&& SiegeCastleLordDuelProfile.IsEligibleShield(
				GetDisplayTier(item),
				item.ItemType == ItemObject.ItemTypeEnum.Shield && item.PrimaryWeapon.IsShield,
				isMerchandise: !item.NotMerchandise);
	}

	private static int GetDisplayTier(ItemObject item)
	{
		return item == null ? 0 : (int)item.Tier + 1;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		Mission mission = base.Mission;
		ProtectCapturedLords(mission);
		if (_stage == RuntimeStage.Idle)
		{
			return;
		}
		if (mission == null || mission.IsMissionEnding || !CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
		{
			Cancel("castle_duel_context_ended", showMessage: false);
			return;
		}
		if (_stage == RuntimeStage.Finishing)
		{
			if (!string.IsNullOrEmpty(_cancelSource))
			{
				Cancel(_cancelSource, showMessage: true);
			}
			else
			{
				CompleteDuel(_pendingPlayerWon);
			}
			return;
		}
		Agent player = Agent.Main;
		if (player == null || !player.IsActive() || _lordAgent == null || !_lordAgent.IsActive())
		{
			Cancel("castle_duel_participant_unavailable", showMessage: true);
			return;
		}

		_playerMountedDuringDuel |= CastleAftermathLordDuelRuntimeBridge.IsPlayerMounted();
		if (_stage == RuntimeStage.ApproachingWeapon)
		{
			TickWeaponApproach(mission, player);
			return;
		}
		if (_stage == RuntimeStage.Fighting)
		{
			player.SetMortalityState(Agent.MortalityState.Immortal);
			ProtectAgent(_playerMountSnapshot?.Agent, Agent.MortalityState.Immortal);
			_lordAgent.SetMortalityState(Agent.MortalityState.Immortal);
			if (mission.CurrentTime >= _nextCombatRefreshAt)
			{
				_nextCombatRefreshAt = mission.CurrentTime + 0.35f;
				RefreshLordCombatAi(player, _lordAgent);
			}
		}
	}

	public override void OnAgentHit(
		Agent affectedAgent,
		Agent affectorAgent,
		in MissionWeapon attackerWeapon,
		in Blow blow,
		in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in attackerWeapon, in blow, in attackCollisionData);
		if (_stage != RuntimeStage.Fighting || affectedAgent == null || affectorAgent == null)
		{
			return;
		}
		Agent player = Agent.Main;
		bool playerHitLord = ReferenceEquals(affectedAgent, _lordAgent) && IsAgentOrMountOf(affectorAgent, player);
		bool lordHitPlayer = ReferenceEquals(affectedAgent, player) && IsAgentOrMountOf(affectorAgent, _lordAgent);
		if (!playerHitLord && !lordHitPlayer)
		{
			return;
		}

		int damage = Math.Max(0, blow.InflictedDamage);
		if (damage <= 0)
		{
			return;
		}
		if (playerHitLord)
		{
			_playerUsedRangedWeapon |= !attackerWeapon.IsEmpty && attackerWeapon.CurrentUsageItem?.IsRangedWeapon == true;
			_lordVirtualHealth = Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, _lordVirtualHealth - damage);
			_lordAgent.Health = _lordVirtualHealth;
			if (_lordVirtualHealth <= SiegeCastleLordDuelProfile.DuelHealthFloor)
			{
				QueueResult(playerWon: true);
			}
		}
		else
		{
			_playerVirtualHealth = Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, _playerVirtualHealth - damage);
			player.Health = _playerVirtualHealth;
			if (_playerVirtualHealth <= SiegeCastleLordDuelProfile.DuelHealthFloor)
			{
				QueueResult(playerWon: false);
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (_stage != RuntimeStage.Idle
			&& (ReferenceEquals(affectedAgent, _lordAgent) || ReferenceEquals(affectedAgent, Agent.Main)))
		{
			_cancelSource = "castle_duel_participant_removed";
			_stage = RuntimeStage.Finishing;
		}
	}

	public override void OnRemoveBehavior()
	{
		Cancel("castle_duel_behavior_removed", showMessage: false);
		CastleAftermathLordDuelRuntimeBridge.Clear(base.Mission);
		base.OnRemoveBehavior();
	}

	protected override void OnEndMission()
	{
		Cancel("castle_duel_mission_ended", showMessage: false);
		CastleAftermathLordDuelRuntimeBridge.Clear(base.Mission);
		base.OnEndMission();
	}

	internal void Cancel(string source, bool showMessage)
	{
		if (_stage == RuntimeStage.Idle)
		{
			return;
		}
		string heroId = _lordHero?.StringId ?? "N/A";
		RestoreRuntime(playerWon: false, cancelled: true);
		Logger.Log("CastleAftermath", "Cancelled captive-lord duel. Hero=" + heroId + ", Source=" + (source ?? "N/A"));
		GcczDiagnosticLog.Log("CastleLordDuel", "cancelled hero=" + heroId + " source=" + (source ?? "N/A"));
		if (showMessage)
		{
			AnimusForgeQuickInfo.Show("【城堡决斗】决斗未能继续，双方已恢复为战后处置状态。");
		}
	}

	private void TickWeaponApproach(Mission mission, Agent player)
	{
		_lordAgent.SetMortalityState(Agent.MortalityState.Invulnerable);
		player.SetMortalityState(Agent.MortalityState.Invulnerable);
		ProtectAgent(_playerMountSnapshot?.Agent, Agent.MortalityState.Invulnerable);
		float distanceSquared = (_lordAgent.Position.AsVec2 - _weaponPosition.AsVec2).LengthSquared;
		bool arrived = distanceSquared <= SiegeCastleLordDuelProfile.WeaponArrivalDistance * SiegeCastleLordDuelProfile.WeaponArrivalDistance;
		bool timedOut = mission.CurrentTime - _approachStartedAt >= SiegeCastleLordDuelProfile.WeaponWalkTimeoutSeconds;
		if (!arrived && !timedOut)
		{
			return;
		}
		if (timedOut && !arrived)
		{
			try
			{
				_lordAgent.TeleportToPosition(_weaponPosition);
			}
			catch
			{
			}
		}
		if (!TryEquipDuelLoadout(_lordAgent))
		{
			Cancel("castle_duel_loadout_equip_failed", showMessage: true);
			return;
		}
		_stage = RuntimeStage.Fighting;
		_nextCombatRefreshAt = 0f;
		player.SetMortalityState(Agent.MortalityState.Immortal);
		_lordAgent.SetMortalityState(Agent.MortalityState.Immortal);
		RefreshLordCombatAi(player, _lordAgent);
		AnimusForgeQuickInfo.Show("【城堡决斗】俘虏领主已经拿起武器，决斗开始。双方生命降至最后一点即判负，不会死亡。", _lordAgent.Character as CharacterObject);
		GcczDiagnosticLog.Log("CastleLordDuel", "fighting hero=" + (_lordHero?.StringId ?? "N/A")
			+ " approachTimedOut=" + timedOut);
	}

	private void QueueResult(bool playerWon)
	{
		if (_stage != RuntimeStage.Fighting)
		{
			return;
		}
		_pendingPlayerWon = playerWon;
		_cancelSource = null;
		_stage = RuntimeStage.Finishing;
	}

	private void CompleteDuel(bool playerWon)
	{
		Hero hero = _lordHero;
		Agent lord = _lordAgent;
		int targetIndex = lord?.Index ?? -1;
		bool playerWasMountedWhenAccepted = _playerWasMountedWhenAccepted;
		bool playerCarriedRangedWeaponWhenAccepted = _playerCarriedRangedWeaponWhenAccepted;
		bool playerMountedDuringDuel = _playerMountedDuringDuel;
		bool playerUsedRangedWeapon = _playerUsedRangedWeapon;
		string resultFact = SiegeCastleLordDuelProfile.BuildResultFact(
			Settlement.CurrentSettlement?.Name?.ToString(),
			Hero.MainHero?.Name?.ToString(),
			hero?.Name?.ToString(),
			playerWon,
			playerWasMountedWhenAccepted,
			playerCarriedRangedWeaponWhenAccepted,
			playerMountedDuringDuel,
			playerUsedRangedWeapon);
		RestoreRuntime(playerWon, cancelled: false);
		string outcome = "hero=" + (hero?.StringId ?? "N/A") + ", playerWon=" + playerWon
			+ ", mountedAtAcceptance=" + playerWasMountedWhenAccepted
			+ ", carriedRangedAtAcceptance=" + playerCarriedRangedWeaponWhenAccepted
			+ ", mountedDuring=" + playerMountedDuringDuel + ", usedRanged=" + playerUsedRangedWeapon;
		Logger.Log("CastleAftermath", "Completed captive-lord duel. " + outcome);
		GcczDiagnosticLog.Log("CastleLordDuel", "completed " + outcome);
		AnimusForgeQuickInfo.Show(playerWon
			? "【城堡决斗】你击败了俘虏领主；对方已认输并恢复俘虏姿态。"
			: "【城堡决斗】俘虏领主击败了你并放下武器；双方仍留在处置现场。");
		if (targetIndex >= 0)
		{
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(
				resultFact,
				targetIndex,
				persistHeroPrivateHistory: true,
				suppressStare: true,
				postSpeechLeaveSeconds: -1f,
				runSiegeReactionPostprocess: false);
		}
	}

	private void RestoreRuntime(bool playerWon, bool cancelled)
	{
		Agent player = Agent.Main;
		Agent lord = _lordAgent;
		TryDeleteLoadoutVisuals();
		TryDropAndStripLordLoadout(lord);
		RestoreDuelTeamRelations();
		RestoreTeam(player, _playerSnapshot?.Team);
		RestoreTeam(_playerMountSnapshot?.Agent, _playerMountTeam);
		RestoreTeam(lord, _lordSnapshot?.Team);
		try
		{
			if (base.Mission != null && _originalMissionPlayerTeam != null)
			{
				base.Mission.PlayerTeam = _originalMissionPlayerTeam;
			}
		}
		catch
		{
		}

		RestorePlayer(player);
		RestoreSnapshotState(_playerMountSnapshot);
		if (lord != null && lord.IsActive())
		{
			try
			{
				lord.Health = !cancelled && playerWon
					? SiegeCastleLordDuelProfile.DuelHealthFloor
					: Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, _lordSnapshot?.Health ?? lord.HealthLimit);
				lord.SetMortalityState(Agent.MortalityState.Invulnerable);
				lord.Controller = _lordSnapshot?.Controller ?? AgentControllerType.AI;
				lord.Formation = _lordSnapshot?.Formation;
				lord.DisableScriptedMovement();
				lord.InvalidateTargetAgent();
				lord.ResetEnemyCaches();
				lord.UpdateFormationOrders();
				lord.SetWatchState(Agent.WatchState.Patrolling);
			}
			catch
			{
			}
		}

		// Release the ownership gate before asking the inspection/command bridges to
		// restore prisoner poses; otherwise their safety checks correctly refuse.
		_controlledAgentIndexes.Clear();
		foreach (AgentSnapshot snapshot in _audienceSnapshots.Values.ToList())
		{
			RestoreAudienceAgent(snapshot);
		}
		if (lord != null && lord.IsActive())
		{
			CastleAftermathRuntimeBridge.RestorePrisonerAfterExternalControl(lord);
		}
		RestoreMissionMode();
		ResetFields();
	}

	private void RestorePlayer(Agent player)
	{
		if (player == null || !player.IsActive() || _playerSnapshot == null)
		{
			return;
		}
		try
		{
			player.Health = Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, _playerSnapshot.Health);
			player.SetMortalityState(_playerSnapshot.Mortality);
			player.Controller = _playerSnapshot.Controller == AgentControllerType.None
				? AgentControllerType.Player
				: _playerSnapshot.Controller;
			player.Formation = _playerSnapshot.Formation;
			player.SetIsAIPaused(isPaused: false);
			player.DisableScriptedMovement();
			player.InvalidateTargetAgent();
			player.ResetEnemyCaches();
			player.UpdateFormationOrders();
		}
		catch
		{
		}
	}

	private static void RestoreSnapshotState(AgentSnapshot snapshot)
	{
		Agent agent = snapshot?.Agent;
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.Health = Math.Max(SiegeCastleLordDuelProfile.DuelHealthFloor, snapshot.Health);
			agent.SetMortalityState(snapshot.Mortality);
			agent.Controller = snapshot.Controller;
			agent.Formation = snapshot.Formation;
			agent.SetIsAIPaused(isPaused: false);
			agent.DisableScriptedMovement();
			agent.InvalidateTargetAgent();
			agent.ResetEnemyCaches();
			agent.UpdateFormationOrders();
		}
		catch
		{
		}
	}

	private static void ProtectAgent(Agent agent, Agent.MortalityState mortality)
	{
		try
		{
			if (agent != null && agent.IsActive())
			{
				agent.SetMortalityState(mortality);
			}
		}
		catch
		{
		}
	}

	private void PrepareAudience(Mission mission, Agent player, Agent lord)
	{
		IEnumerable<Agent> candidates = mission.Agents
			.Where(agent => agent != null && agent.IsHuman && agent.IsActive() && agent != player && agent != lord)
			.Where(agent => CastleAftermathRuntimeBridge.IsPrisonerAgent(agent)
				|| SiegeAiInterventionBehavior.IsInterventionAlliedSoldierForExternal(agent, requireActive: true))
			.OrderBy(agent => (agent.Position.AsVec2 - _arenaCenter.AsVec2).LengthSquared);
		int index = 0;
		foreach (Agent audience in candidates)
		{
			AgentSnapshot snapshot = Capture(audience);
			_audienceSnapshots[audience.Index] = snapshot;
			_controlledAgentIndexes.Add(audience.Index);
			PlaceAudienceAgent(audience, index++);
		}
	}

	private void PlaceAudienceAgent(Agent agent, int globalIndex)
	{
		int remaining = globalIndex;
		int ring = 0;
		float radius;
		int capacity;
		while (true)
		{
			radius = SiegeCastleLordDuelProfile.AudienceBaseRadius + ring * SiegeCastleLordDuelProfile.AudienceRingSpacing;
			capacity = Math.Max(8, (int)Math.Floor(2.0 * Math.PI * radius / SiegeCastleLordDuelProfile.AudienceSpacing));
			if (remaining < capacity)
			{
				break;
			}
			remaining -= capacity;
			ring++;
		}
		double angle = 2.0 * Math.PI * remaining / capacity;
		Vec3 target = new Vec3(
			_arenaCenter.x + (float)Math.Cos(angle) * radius,
			_arenaCenter.y + (float)Math.Sin(angle) * radius,
			_arenaCenter.z,
			-1f);
		try
		{
			target = ProjectToNearestNavMesh(base.Mission, target);
			Vec2 facing = _arenaCenter.AsVec2 - target.AsVec2;
			facing = facing.LengthSquared < 0.01f ? Vec2.Forward : facing.Normalized();
			WorldPosition worldPosition = new WorldPosition(base.Mission.Scene, target);
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
			agent.Controller = AgentControllerType.AI;
			agent.SetIsAIPaused(isPaused: false);
			agent.SetMaximumSpeedLimit(1.35f, false);
			agent.SetScriptedPositionAndDirection(
				ref worldPosition,
				facing.RotationInRadians,
				addHumanLikeDelay: false,
				Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
		}
		catch
		{
		}
	}

	private void RestoreAudienceAgent(AgentSnapshot snapshot)
	{
		Agent agent = snapshot?.Agent;
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
			agent.SetMortalityState(snapshot.Mortality);
			agent.Controller = snapshot.Controller;
			agent.Formation = snapshot.Formation;
			agent.SetMaximumSpeedLimit(-1f, false);
			agent.SetIsAIPaused(isPaused: false);
			agent.InvalidateTargetAgent();
			agent.ResetEnemyCaches();
			agent.UpdateFormationOrders();
		}
		catch
		{
		}
		if (CastleAftermathRuntimeBridge.IsPrisonerAgent(agent))
		{
			CastleAftermathRuntimeBridge.RestorePrisonerAfterExternalControl(agent);
		}
		else
		{
			SiegeAiInterventionBehavior.EnsureAgentPlayerCommandableForExternal(
				agent,
				"castle_lord_duel_finished");
		}
	}

	private void ResolveArenaPoints(Mission mission, Agent player)
	{
		Vec2 forward = player.LookDirection.AsVec2;
		if (forward.LengthSquared < 0.01f)
		{
			forward = Vec2.Forward;
		}
		else
		{
			forward = forward.Normalized();
		}
		_weaponPosition = player.Position + new Vec3(
			forward.x * SiegeCastleLordDuelProfile.WeaponForwardDistance,
			forward.y * SiegeCastleLordDuelProfile.WeaponForwardDistance,
			0f,
			-1f);
		_weaponPosition = ProjectToNearestNavMesh(mission, _weaponPosition);
		_weaponPosition.z += 0.05f;
		Vec2 right = new Vec2(forward.y, -forward.x);
		_shieldPosition = _weaponPosition + new Vec3(
			right.x * SiegeCastleLordDuelProfile.LoadoutItemSpacing,
			right.y * SiegeCastleLordDuelProfile.LoadoutItemSpacing,
			0f,
			-1f);
		_shieldPosition = ProjectToNearestNavMesh(mission, _shieldPosition);
		_shieldPosition.z += 0.05f;
		_arenaCenter = player.Position + new Vec3(
			forward.x * SiegeCastleLordDuelProfile.WeaponForwardDistance * 0.5f,
			forward.y * SiegeCastleLordDuelProfile.WeaponForwardDistance * 0.5f,
			0f,
			-1f);
		_arenaCenter = ProjectToNearestNavMesh(mission, _arenaCenter);
	}

	private static Vec3 ProjectToNearestNavMesh(Mission mission, Vec3 position)
	{
		try
		{
			if (mission?.Scene == null)
			{
				return position;
			}
			position.z = mission.Scene.GetGroundHeightAtPosition(position);
			WorldPosition worldPosition = new WorldPosition(mission.Scene, position);
			if (worldPosition.GetNearestNavMesh() != UIntPtr.Zero)
			{
				return worldPosition.GetNavMeshVec3();
			}
		}
		catch
		{
		}
		return position;
	}

	private bool TrySpawnLoadoutVisuals(Mission mission)
	{
		try
		{
			TryDeleteLoadoutVisuals();
			_weaponVisual = SpawnLoadoutItemVisual(mission, _duelWeaponItem, _weaponPosition);
			if (_weaponVisual == null)
			{
				return false;
			}
			if (_duelShieldItem != null)
			{
				_shieldVisual = SpawnLoadoutItemVisual(mission, _duelShieldItem, _shieldPosition);
				if (_shieldVisual == null)
				{
					TryDeleteLoadoutVisuals();
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			TryDeleteLoadoutVisuals();
			Logger.Log("CastleAftermath", "Spawn captive-lord duel loadout failed: " + ex.Message);
			return false;
		}
	}

	private static SpawnedItemEntity SpawnLoadoutItemVisual(Mission mission, ItemObject item, Vec3 position)
	{
		if (mission == null || item == null)
		{
			return null;
		}
		MissionWeapon missionWeapon = new MissionWeapon(item, null, null, 1);
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = position;
		GameEntity entity = mission.SpawnWeaponWithNewEntity(
			ref missionWeapon,
			Mission.WeaponSpawnFlags.WithStaticPhysics | Mission.WeaponSpawnFlags.CannotBePickedUp,
			frame);
		return entity?.GetFirstScriptOfType<SpawnedItemEntity>();
	}

	private void PrepareLordForWeaponApproach(Mission mission, Agent lord)
	{
		TryRestoreCombatActionSet(lord);
		StripWeapons(lord);
		try
		{
			lord.SetMortalityState(Agent.MortalityState.Invulnerable);
			lord.Controller = AgentControllerType.AI;
			lord.SetIsAIPaused(isPaused: false);
			lord.SetMaximumSpeedLimit(1.35f, false);
			lord.SetAgentFlags(lord.GetAgentFlags() | AgentFlag.CanGetAlarmed | AgentFlag.CanWieldWeapon);
			lord.SetWatchState(Agent.WatchState.Cautious);
			WorldPosition weaponWorldPosition = new WorldPosition(mission.Scene, _weaponPosition);
			lord.SetScriptedPosition(
				ref weaponWorldPosition,
				addHumanLikeDelay: false,
				Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun | Agent.AIScriptedFrameFlags.GoWithoutMount);
		}
		catch
		{
		}
	}

	private bool TryEquipDuelLoadout(Agent lord)
	{
		try
		{
			TryDeleteLoadoutVisuals();
			MissionWeapon weapon = new MissionWeapon(_duelWeaponItem, null, null, 1);
			lord.DisableScriptedMovement();
			lord.SetMaximumSpeedLimit(-1f, false);
			lord.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref weapon);
			if (_duelShieldItem != null)
			{
				MissionWeapon shield = new MissionWeapon(_duelShieldItem, null, null, 1);
				lord.EquipWeaponWithNewEntity(EquipmentIndex.Weapon1, ref shield);
			}
			lord.WieldInitialWeapons(
				Agent.WeaponWieldActionType.InstantAfterPickUp,
				Equipment.InitialWeaponEquipPreference.MeleeForMainHand);
			return !lord.Equipment[EquipmentIndex.Weapon0].IsEmpty
				&& (_duelShieldItem == null || !lord.Equipment[EquipmentIndex.Weapon1].IsEmpty);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Equip captive-lord duel loadout failed: " + ex.Message);
			return false;
		}
	}

	private bool TryInitializeDuelTeams(Mission mission, Agent player, Agent lord)
	{
		if (mission == null || player == null || lord == null)
		{
			return false;
		}

		_playerDuelTeam = player.Team ?? mission.PlayerTeam;
		_lordDuelTeam = ResolveExistingOpponentTeam(mission, _playerDuelTeam, lord.Team);
		if (_playerDuelTeam == null || _lordDuelTeam == null || _playerDuelTeam == _lordDuelTeam)
		{
			Logger.Log("CastleAftermath", "Initialize captive-lord duel teams failed: no distinct existing opponent team."
				+ " PlayerTeam=" + (_playerDuelTeam?.TeamIndex.ToString() ?? "null")
				+ ", LordOriginalTeam=" + (lord.Team?.TeamIndex.ToString() ?? "null")
				+ ", MissionTeams=" + (mission.Teams?.Count ?? 0));
			return false;
		}

		CaptureDuelTeamRelations();
		if (!TryAssignAgentTeam(player, _playerDuelTeam, "player")
			|| !TryAssignAgentTeam(player.MountAgent, _playerDuelTeam, "player_mount", allowMissing: true)
			|| !TryAssignAgentTeam(lord, _lordDuelTeam, "lord")
			|| !TryAssignAgentTeam(lord.MountAgent, _lordDuelTeam, "lord_mount", allowMissing: true))
		{
			return false;
		}
		if (!TrySetMutualHostility(_playerDuelTeam, _lordDuelTeam, isEnemy: true))
		{
			return false;
		}

		Logger.Log("CastleAftermath", "Initialized captive-lord duel with existing mission teams. PlayerTeam="
			+ _playerDuelTeam.TeamIndex + ", LordTeam=" + _lordDuelTeam.TeamIndex
			+ ", LordAgent=" + lord.Index);
		return true;
	}

	private static Team ResolveExistingOpponentTeam(Mission mission, Team playerTeam, Team lordOriginalTeam)
	{
		if (lordOriginalTeam != null && lordOriginalTeam != playerTeam)
		{
			return lordOriginalTeam;
		}
		try
		{
			Team nativeEnemy = mission?.PlayerEnemyTeam;
			if (nativeEnemy != null && nativeEnemy != playerTeam)
			{
				return nativeEnemy;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Resolve native castle duel enemy team failed: " + ex);
		}
		try
		{
			return mission?.Teams?
				.FirstOrDefault(team => team != null && team != playerTeam && team.Side != playerTeam?.Side)
				?? mission?.Teams?.FirstOrDefault(team => team != null && team != playerTeam);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Resolve fallback castle duel enemy team failed: " + ex);
			return null;
		}
	}

	private static bool TryAssignAgentTeam(Agent agent, Team team, string role, bool allowMissing = false)
	{
		if (agent == null || !agent.IsActive())
		{
			return allowMissing;
		}
		if (team == null)
		{
			return false;
		}
		try
		{
			if (agent.Team != team)
			{
				agent.SetTeam(team, sync: true);
			}
			if (agent.Team == team)
			{
				return true;
			}
			Logger.Log("CastleAftermath", "Assign captive-lord duel team did not stick. Role="
				+ role + ", Agent=" + agent.Index + ", Team=" + team.TeamIndex);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Assign captive-lord duel team failed. Role="
				+ role + ", Agent=" + agent.Index + ", Error=" + ex);
		}
		return false;
	}

	private void CaptureDuelTeamRelations()
	{
		_duelTeamRelationsCaptured = false;
		try
		{
			_playerTeamWasEnemyOfLordTeam = _playerDuelTeam.IsEnemyOf(_lordDuelTeam);
			_lordTeamWasEnemyOfPlayerTeam = _lordDuelTeam.IsEnemyOf(_playerDuelTeam);
			_duelTeamRelationsCaptured = true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Capture castle duel team relations failed; neutral restore will be used. Error=" + ex);
			_playerTeamWasEnemyOfLordTeam = false;
			_lordTeamWasEnemyOfPlayerTeam = false;
			_duelTeamRelationsCaptured = true;
		}
	}

	private static bool TrySetMutualHostility(Team playerTeam, Team lordTeam, bool isEnemy)
	{
		if (playerTeam == null || lordTeam == null || playerTeam == lordTeam)
		{
			return false;
		}
		try
		{
			playerTeam.SetIsEnemyOf(lordTeam, isEnemy);
			lordTeam.SetIsEnemyOf(playerTeam, isEnemy);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Set castle duel mutual hostility failed: " + ex);
			return false;
		}
	}

	private bool TryEnterDuelMissionMode(Mission mission)
	{
		try
		{
			mission.SetMissionMode(MissionMode.Battle, atStart: true);
			_missionModeChanged = true;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Enter captive-lord duel mission mode failed: " + ex);
			return false;
		}
	}

	private static void RefreshLordCombatAi(Agent player, Agent lord)
	{
		if (player == null || !player.IsActive() || lord == null || !lord.IsActive())
		{
			return;
		}
		try
		{
			lord.Controller = AgentControllerType.AI;
			lord.SetIsAIPaused(isPaused: false);
			lord.DisableScriptedMovement();
			lord.ResetEnemyCaches();
			lord.InvalidateTargetAgent();
			lord.InvalidateAIWeaponSelections();
			lord.ClearTargetFrame();
			lord.SetTargetPosition(player.Position.AsVec2);
			lord.WieldInitialWeapons(
				Agent.WeaponWieldActionType.InstantAfterPickUp,
				Equipment.InitialWeaponEquipPreference.MeleeForMainHand);
			lord.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch
		{
		}
	}

	private void ProtectCapturedLords(Mission mission)
	{
		if (mission == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents)
		{
			if (agent == null || !agent.IsActive() || !CastleAftermathRuntimeBridge.IsLordPrisonerAgent(agent))
			{
				continue;
			}
			try
			{
				agent.SetMortalityState(
					_stage == RuntimeStage.Fighting && ReferenceEquals(agent, _lordAgent)
						? Agent.MortalityState.Immortal
						: Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
		}
	}

	private static AgentSnapshot Capture(Agent agent)
	{
		return new AgentSnapshot
		{
			Agent = agent,
			Team = agent?.Team,
			Formation = agent?.Formation,
			Controller = agent?.Controller ?? AgentControllerType.None,
			Mortality = agent?.CurrentMortalityState ?? Agent.MortalityState.Mortal,
			Health = agent?.Health ?? 1f
		};
	}

	private static void RestoreTeam(Agent agent, Team team)
	{
		try
		{
			if (agent != null && agent.IsActive() && team != null && agent.Team != team)
			{
				agent.SetTeam(team, sync: true);
			}
		}
		catch
		{
		}
	}

	private void RestoreDuelTeamRelations()
	{
		if (!_duelTeamRelationsCaptured || _playerDuelTeam == null || _lordDuelTeam == null
			|| _playerDuelTeam == _lordDuelTeam)
		{
			return;
		}
		try
		{
			_playerDuelTeam.SetIsEnemyOf(_lordDuelTeam, _playerTeamWasEnemyOfLordTeam);
			_lordDuelTeam.SetIsEnemyOf(_playerDuelTeam, _lordTeamWasEnemyOfPlayerTeam);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Restore castle duel team relations failed: " + ex);
		}
	}

	private void RestoreMissionMode()
	{
		if (!_missionModeChanged || base.Mission == null || base.Mission.IsMissionEnding)
		{
			return;
		}
		try
		{
			base.Mission.SetMissionMode(_originalMissionMode, atStart: true);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Restore captive-lord duel mission mode failed: " + ex);
		}
	}

	private static bool IsAgentOrMountOf(Agent candidate, Agent principal)
	{
		return candidate != null && principal != null
			&& (ReferenceEquals(candidate, principal) || ReferenceEquals(candidate.RiderAgent, principal));
	}

	private static void TryRestoreCombatActionSet(Agent agent)
	{
		try
		{
			if (agent?.Monster == null)
			{
				return;
			}
			AnimationSystemData data = agent.Monster.FillAnimationSystemData(
				MBActionSet.GetActionSet(agent.Monster.ActionSetCode),
				1f,
				false);
			agent.SetActionSet(ref data);
		}
		catch
		{
		}
	}

	private static void StripWeapons(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
		{
			try
			{
				agent.RemoveEquippedWeapon(slot);
			}
			catch
			{
			}
		}
		try
		{
			agent.InvalidateAIWeaponSelections();
			agent.UpdateWeapons();
		}
		catch
		{
		}
	}

	private static void TryDropAndStripLordLoadout(Agent lord)
	{
		if (lord == null || !lord.IsActive())
		{
			return;
		}
		try
		{
			EquipmentIndex primarySlot = lord.GetPrimaryWieldedItemIndex();
			EquipmentIndex offhandSlot = lord.GetOffhandWieldedItemIndex();
			TryDropEquippedItem(lord, primarySlot == EquipmentIndex.None ? EquipmentIndex.Weapon0 : primarySlot);
			if (offhandSlot != primarySlot)
			{
				TryDropEquippedItem(lord, offhandSlot == EquipmentIndex.None ? EquipmentIndex.Weapon1 : offhandSlot);
			}
		}
		catch
		{
		}
		StripWeapons(lord);
	}

	private static void TryDropEquippedItem(Agent agent, EquipmentIndex slot)
	{
		if (slot != EquipmentIndex.None && !agent.Equipment[slot].IsEmpty)
		{
			agent.DropItem(slot);
		}
	}

	private void TryDeleteLoadoutVisuals()
	{
		try
		{
			_weaponVisual?.RequestDeletionOnNextTick();
			_shieldVisual?.RequestDeletionOnNextTick();
		}
		catch
		{
		}
		_weaponVisual = null;
		_shieldVisual = null;
	}

	private static void EndCurrentConversation()
	{
		try
		{
			Campaign.Current?.ConversationManager?.EndConversation();
		}
		catch
		{
		}
	}

	private void ResetFields()
	{
		_stage = RuntimeStage.Idle;
		_lordHero = null;
		_lordAgent = null;
		_playerSnapshot = null;
		_playerMountSnapshot = null;
		_lordSnapshot = null;
		_originalMissionPlayerTeam = null;
		_playerDuelTeam = null;
		_lordDuelTeam = null;
		_playerMountTeam = null;
		_originalMissionMode = MissionMode.Battle;
		_missionModeChanged = false;
		_duelTeamRelationsCaptured = false;
		_playerTeamWasEnemyOfLordTeam = false;
		_lordTeamWasEnemyOfPlayerTeam = false;
		_duelWeaponItem = null;
		_duelShieldItem = null;
		_duelLoadoutKind = DuelLoadoutKind.None;
		_weaponVisual = null;
		_shieldVisual = null;
		_audienceSnapshots.Clear();
		_controlledAgentIndexes.Clear();
		_approachStartedAt = 0f;
		_nextCombatRefreshAt = 0f;
		_playerVirtualHealth = 0f;
		_lordVirtualHealth = 0f;
		_playerWasMountedWhenAccepted = false;
		_playerCarriedRangedWeaponWhenAccepted = false;
		_playerMountedDuringDuel = false;
		_playerUsedRangedWeapon = false;
		_pendingPlayerWon = false;
		_cancelSource = null;
	}
}
