using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class NoblePrisonerEscortMissionBehavior : MissionLogic
{
	private const float ResolveTimeoutSeconds = 8f;
	private const float NativePopulationSettleSeconds = 1.25f;
	private const float MaintainIntervalSeconds = 0.5f;

	private readonly Dictionary<int, EscortedRuntimeAgent> _agents = new Dictionary<int, EscortedRuntimeAgent>();
	private NoblePrisonerEscortMode _mode;
	private bool _spawnAttempted;
	private bool _cleaned;
	private float _resolveElapsed;
	private float _maintainTimer;

	public override void AfterStart()
	{
		base.AfterStart();
		_resolveElapsed = 0f;
		_maintainTimer = 0f;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		Mission mission = base.Mission;
		if (_cleaned || mission == null || mission.IsMissionEnding)
		{
			return;
		}

		NoblePrisonerExecutionRuntime.Tick(mission, dt);
		if (!_spawnAttempted)
		{
			_resolveElapsed += Math.Max(0f, dt);
			_mode = NoblePrisonerEscortBehavior.ResolveModeForMission(mission);
			if (_mode != NoblePrisonerEscortMode.None
				&& _resolveElapsed >= NativePopulationSettleSeconds
				&& mission.MainAgent != null
				&& mission.PlayerTeam != null)
			{
				_spawnAttempted = true;
				SpawnConfiguredPrisoners();
			}
			else if (_resolveElapsed >= ResolveTimeoutSeconds)
			{
				_spawnAttempted = true;
				NoblePrisonerEscortLog.LogVerbose("Mission context resolve timed out; no escort spawned. scene=" + (mission.SceneName ?? "N/A"));
			}
		}

		if (_agents.Count == 0)
		{
			return;
		}
		if (_mode == NoblePrisonerEscortMode.WorldMapEncounterMeeting && MeetingBattleRuntime.IsCombatEscalated)
		{
			DespawnMeetingEscortsForCombat();
		}
		_maintainTimer -= Math.Max(0f, dt);
		if (_maintainTimer <= 0f)
		{
			_maintainTimer = MaintainIntervalSeconds;
			MaintainAgents();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		NoblePrisonerExecutionRuntime.OnAgentRemoved(affectedAgent, affectorAgent, agentState);
		if (affectedAgent != null && _agents.Remove(affectedAgent.Index))
		{
			NoblePrisonerEscortBehavior.UnregisterEscortedAgent(affectedAgent, "agent_removed_" + agentState);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in attackerWeapon, in blow, in attackCollisionData);
		NoblePrisonerExecutionRuntime.OnAgentHit(affectedAgent, affectorAgent);
	}

	protected override void OnEndMission()
	{
		Cleanup("mission_end");
		base.OnEndMission();
	}

	internal void DespawnMeetingEscortsForCombat()
	{
		if (_mode != NoblePrisonerEscortMode.WorldMapEncounterMeeting)
		{
			return;
		}
		foreach (EscortedRuntimeAgent runtime in _agents.Values.ToList())
		{
			if (runtime?.Agent == null || runtime.CombatDespawnStarted)
			{
				continue;
			}
			runtime.CombatDespawnStarted = true;
			NoblePrisonerEscortBehavior.MarkMeetingCombatDespawnStarted(runtime.Agent);
			DespawnMeetingEscortForCombat(runtime, "combat_escalated");
		}
	}

	private void SpawnConfiguredPrisoners()
	{
		TroopRoster roster = NoblePrisonerEscortBehavior.ResolveLiveProfile(_mode, out int configured, out int unavailable);
		List<CharacterObject> characters = SnapshotCharacters(roster);
		if (characters.Count == 0)
		{
			NoblePrisonerEscortLog.Log("No live configured noble prisoners for mission. mode=" + _mode + ", configured=" + configured + ", unavailable=" + unavailable);
			return;
		}

		Agent main = base.Mission.MainAgent;
		if (_mode == NoblePrisonerEscortMode.LordsHall)
		{
			NoblePrisonerEscortBehavior.ResolvePlayerCommandTeamForExternal(base.Mission, "lords_hall_spawn_prepare");
		}
		Vec3 forward = main.LookDirection;
		forward.z = 0f;
		if (forward.LengthSquared < 0.001f)
		{
			forward = new Vec3(1f, 0f, 0f, -1f);
		}
		forward.Normalize();
		Vec3 side = Vec3.CrossProduct(forward, Vec3.Up);
		if (side.LengthSquared < 0.001f)
		{
			side = new Vec3(0f, 1f, 0f, -1f);
		}
		side.Normalize();

		int spawned = 0;
		for (int i = 0; i < characters.Count; i++)
		{
			CharacterObject character = characters[i];
			Hero hero = character?.HeroObject;
			if (!IsStillMainPartyPrisoner(hero))
			{
				continue;
			}
			float lateral = (i - (characters.Count - 1) * 0.5f) * 1.35f;
			Vec3 spawnPosition = main.Position - forward * 2.6f + side * lateral;
			spawnPosition = ResolveReachableSpawnPosition(spawnPosition, main.Position);
			Agent agent = FindExistingHeroAgent(hero);
			bool adopted = agent != null;
			if (!adopted)
			{
				EscortedNoblePrisonerAgentOrigin origin = new EscortedNoblePrisonerAgentOrigin(character);
				agent = BannerlordApiCompat.SpawnInspectionTroop(
					base.Mission,
					origin,
					characters.Count,
					i,
					(FormationClass)NoblePrisonerEscortBehavior.LordPrisonerFormationIndex,
					wieldInitialWeapons: false,
					spawnPosition,
					forward.AsVec2);
			}
			if (agent == null)
			{
				NoblePrisonerEscortLog.Log("Spawn returned null. mode=" + _mode + ", hero=" + (hero?.StringId ?? "null"));
				continue;
			}
			EnsureEscortFormation(agent);
			ConfigureNonCombatEscort(agent);
			if (_mode == NoblePrisonerEscortMode.LordsHall)
			{
				PlaceLordsHallEscort(agent, spawnPosition, forward);
			}
			EscortedRuntimeAgent runtime = new EscortedRuntimeAgent { Hero = hero, Agent = agent };
			_agents[agent.Index] = runtime;
			NoblePrisonerEscortBehavior.RegisterEscortedAgent(base.Mission, _mode, hero, agent);
			if (adopted)
			{
				NoblePrisonerEscortLog.Log("Adopted existing hero agent instead of spawning duplicate. mode=" + _mode + ", hero=" + (hero.StringId ?? "N/A") + ", agent=" + agent.Index);
			}
			spawned++;
		}

		IssueFormationFollowOrder();
		if (_mode == NoblePrisonerEscortMode.LordsHall)
		{
			NoblePrisonerEscortBehavior.EnsureCommandUiReadyForExternal(base.Mission, "lords_hall_spawn");
		}
		NoblePrisonerEscortLog.Log("Spawned configured noble prisoners. mode=" + _mode + ", selected=" + characters.Count + ", spawned=" + spawned + ", unavailable=" + unavailable);
		if (spawned > 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(
				"【贵族俘虏随行】已带入 " + spawned + " 名英雄俘虏，归入第 8 编队。",
				Color.FromUint(0xFFDFC16Bu)));
		}
	}

	private void MaintainAgents()
	{
		foreach (EscortedRuntimeAgent runtime in _agents.Values.ToList())
		{
			Agent agent = runtime?.Agent;
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			if (runtime.CombatDespawnStarted)
			{
				continue;
			}
			if (NoblePrisonerExecutionRuntime.ControlsAgent(agent)
				|| DuelBehavior.ControlsAgentForExternal(agent))
			{
				continue;
			}
			EnsureEscortFormation(agent);
			ConfigureNonCombatEscort(agent);
			PruneDuplicateHeroAgents(runtime);
		}
		if (_mode == NoblePrisonerEscortMode.LordsHall)
		{
			NoblePrisonerEscortBehavior.EnsureCommandUiReadyForExternal(base.Mission, "lords_hall_maintain");
		}
	}

	private void PruneDuplicateHeroAgents(EscortedRuntimeAgent runtime)
	{
		Agent retained = runtime?.Agent;
		Hero hero = runtime?.Hero;
		if (retained == null || hero == null || base.Mission?.Agents == null)
		{
			return;
		}
		foreach (Agent duplicate in base.Mission.Agents.ToList())
		{
			if (duplicate == null || duplicate == retained || duplicate == Agent.Main || duplicate.IsMainAgent
				|| !duplicate.IsActive() || duplicate.IsFadingOut()
				|| (duplicate.Character as CharacterObject)?.HeroObject != hero)
			{
				continue;
			}
			try
			{
				duplicate.SetMortalityState(Agent.MortalityState.Invulnerable);
				duplicate.FadeOut(hideInstantly: false, hideMount: true);
				NoblePrisonerEscortLog.Log("Removed late duplicate hero agent. mode=" + _mode + ", hero=" + (hero.StringId ?? "N/A") + ", retained=" + retained.Index + ", duplicate=" + duplicate.Index);
			}
			catch (Exception ex)
			{
				NoblePrisonerEscortLog.LogVerbose("Remove late duplicate hero agent failed. hero=" + (hero.StringId ?? "N/A") + ", duplicate=" + duplicate.Index + ", error=" + ex.Message);
			}
		}
	}

	private void EnsureEscortFormation(Agent agent)
	{
		try
		{
			Team playerTeam = base.Mission?.PlayerTeam;
			Formation formation = playerTeam?.GetFormation((FormationClass)NoblePrisonerEscortBehavior.LordPrisonerFormationIndex);
			if (agent == null || !agent.IsActive() || playerTeam == null || formation == null)
			{
				return;
			}
			if (agent.Team != playerTeam)
			{
				agent.SetTeam(playerTeam, sync: true);
			}
			bool needsAttach = agent.Formation != formation || agent.IsDetachedFromFormation;
			if (agent.Formation != formation)
			{
				agent.Formation = formation;
			}
			if (needsAttach)
			{
				agent.TryAttachToFormation();
				agent.SetShouldCatchUpWithFormation(true);
				agent.UpdateFormationOrders();
			}
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.LogVerbose("Maintain escort formation failed. agent=" + (agent?.Index ?? -1) + ", error=" + ex.Message);
		}
	}

	private void ConfigureNonCombatEscort(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
			agent.SetIsAIPaused(isPaused: false);
			agent.DisableScriptedMovement();
			agent.ClearTargetFrame();
			agent.InvalidateTargetAgent();
			BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(agent, enabled: false);
			agent.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			if (_mode == NoblePrisonerEscortMode.LordsHall)
			{
				DisableSettlementDailyBehavior(agent);
			}
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.LogVerbose("Maintain non-combat escort failed. agent=" + agent.Index + ", error=" + ex.Message);
		}
	}

	private void PlaceLordsHallEscort(Agent agent, Vec3 position, Vec3 facing)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			agent.TeleportToPosition(position);
			agent.LookDirection = facing;
			agent.SetMovementDirection(Vec2.Zero);
			NoblePrisonerEscortLog.Log("Placed lords-hall escorted prisoner near player. hero="
				+ ((agent.Character as CharacterObject)?.HeroObject?.StringId ?? "null")
				+ ", agent=" + agent.Index + ", position=" + position);
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Place lords-hall escorted prisoner failed. agent="
				+ (agent?.Index ?? -1) + ", error=" + ex.Message);
		}
	}

	private static void DisableSettlementDailyBehavior(Agent agent)
	{
		try
		{
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			AgentNavigator navigator = component?.AgentNavigator;
			if (navigator == null)
			{
				return;
			}
			try
			{
				navigator.ClearTarget();
			}
			catch
			{
			}
			DailyBehaviorGroup dailyGroup = navigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (dailyGroup == null)
			{
				return;
			}
			try
			{
				dailyGroup.DisableScriptedBehavior();
			}
			catch
			{
			}
			try
			{
				dailyGroup.DisableAllBehaviors();
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void IssueFormationFollowOrder()
	{
		try
		{
			Agent main = base.Mission.MainAgent;
			Formation formation = base.Mission.PlayerTeam?.GetFormation((FormationClass)NoblePrisonerEscortBehavior.LordPrisonerFormationIndex);
			if (main != null && formation != null)
			{
				formation.SetMovementOrder(MovementOrder.MovementOrderFollow(main));
				formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
			}
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Set formation 8 follow order failed. error=" + ex.Message);
		}
	}

	private void DespawnMeetingEscortForCombat(EscortedRuntimeAgent runtime, string source)
	{
		Agent agent = runtime?.Agent;
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			string formation = agent.Formation?.FormationIndex.ToString() ?? "null";
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
			agent.SetIsAIPaused(isPaused: true);
			agent.DisableScriptedMovement();
			agent.ClearTargetFrame();
			agent.InvalidateTargetAgent();
			BannerlordApiCompat.TrySetAgentAutomaticTargetSelection(agent, enabled: false);
			// Full removal keeps the formation arrangement and Agent state in sync; DetachUnit alone does not.
			agent.Formation = null;
			agent.FadeOut(hideInstantly: true, hideMount: true);
			NoblePrisonerEscortLog.Log("Despawned meeting escort for combat. hero=" + (runtime.Hero?.StringId ?? "null") + ", agent=" + agent.Index + ", source=" + source + ", previousFormation=" + formation + ", fading=" + agent.IsFadingOut());
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Meeting escort combat despawn failed. hero=" + (runtime.Hero?.StringId ?? "null") + ", agent=" + agent.Index + ", source=" + source + ", error=" + ex.Message);
		}
	}

	private Agent FindExistingHeroAgent(Hero hero)
	{
		if (hero == null)
		{
			return null;
		}
		return base.Mission.Agents.FirstOrDefault(agent =>
			agent != null
			&& agent.IsActive()
			&& (agent.Character as CharacterObject)?.HeroObject == hero);
	}

	private Vec3 ResolveReachableSpawnPosition(Vec3 candidate, Vec3 anchor)
	{
		try
		{
			Scene scene = base.Mission?.Scene;
			if (scene != null)
			{
				candidate.z = scene.GetGroundHeightAtPosition(candidate);
				WorldPosition candidateWorld = new WorldPosition(scene, candidate);
				if (candidateWorld.GetNearestNavMesh() != UIntPtr.Zero)
				{
					return candidateWorld.GetNavMeshVec3();
				}
				Vec3 fallback = base.Mission.GetRandomPositionAroundPoint(anchor, 1.5f, 7f, true);
				WorldPosition fallbackWorld = new WorldPosition(scene, fallback);
				if (fallbackWorld.GetNearestNavMesh() != UIntPtr.Zero)
				{
					return fallbackWorld.GetNavMeshVec3();
				}
			}
		}
		catch
		{
		}
		return anchor;
	}

	private static bool IsStillMainPartyPrisoner(Hero hero)
	{
		return hero != null
			&& hero.IsAlive
			&& hero.IsPrisoner
			&& hero.PartyBelongedToAsPrisoner == PartyBase.MainParty
			&& hero.CharacterObject != null;
	}

	private static List<CharacterObject> SnapshotCharacters(TroopRoster roster)
	{
		List<CharacterObject> result = new List<CharacterObject>();
		if (roster == null)
		{
			return result;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(i);
			if (element.Character?.IsHero == true && element.Number > 0)
			{
				result.Add(element.Character);
			}
		}
		return result;
	}

	private void Cleanup(string source)
	{
		if (_cleaned)
		{
			return;
		}
		_cleaned = true;
		NoblePrisonerExecutionRuntime.CancelForMission(base.Mission, source);
		foreach (EscortedRuntimeAgent runtime in _agents.Values)
		{
			NoblePrisonerEscortBehavior.UnregisterEscortedAgent(runtime?.Agent, source);
		}
		_agents.Clear();
	}

	private sealed class EscortedRuntimeAgent
	{
		internal Hero Hero;
		internal Agent Agent;
		internal bool CombatDespawnStarted;
	}
}

internal sealed class EscortedNoblePrisonerAgentOrigin : IAgentOriginBase
{
	private static readonly uint PrisonerColor = new Color(0.55f, 0.12f, 0.12f).ToUnsignedInteger();
	private static readonly uint PrisonerColor2 = new Color(0.25f, 0.04f, 0.04f).ToUnsignedInteger();
	private readonly CharacterObject _troop;
	private Banner _banner;
	private readonly bool _hasThrownWeapon;
	private readonly bool _hasSpear;
	private readonly bool _hasShield;
	private readonly bool _hasHeavyArmor;

	internal CharacterObject Character => _troop;
	public BasicCharacterObject Troop => _troop;
	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;
	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;
	bool IAgentOriginBase.HasShield => _hasShield;
	bool IAgentOriginBase.HasSpear => _hasSpear;
	public bool IsUnderPlayersCommand => true;
#if BANNERLORD_1_4_OR_GREATER
	public bool IsInSameArmyAsPlayer => true;
#endif
	public uint FactionColor => PrisonerColor;
	public uint FactionColor2 => PrisonerColor2;
	public IBattleCombatant BattleCombatant => PartyBase.MainParty;
	public int UniqueSeed => MBRandom.RandomInt(1000000);
	public int Seed => CharacterHelper.GetDefaultFaceSeed(_troop, 0);
	public Banner Banner => _banner;

	internal EscortedNoblePrisonerAgentOrigin(CharacterObject troop)
	{
		_troop = troop;
		_banner = Clan.PlayerClan?.Banner;
		AgentOriginUtilities.GetDefaultTroopTraits(_troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		NoblePrisonerEscortLog.Log("Suppressed campaign wound writeback for escorted prisoner. hero=" + (_troop?.HeroObject?.StringId ?? "null"));
	}

	public void SetKilled()
	{
		NoblePrisonerEscortLog.Log("Suppressed campaign death writeback for escorted prisoner. hero=" + (_troop?.HeroObject?.StringId ?? "null"));
	}

	public void SetRouted(bool isOrderRetreat)
	{
	}

	public void OnAgentRemoved(float agentHealth)
	{
	}

	void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
	}

	public void SetBanner(Banner banner)
	{
		_banner = banner;
	}

	TroopTraitsMask IAgentOriginBase.GetTraitsMask()
	{
		return AgentOriginUtilities.GetDefaultTraitsMask(this);
	}
}
