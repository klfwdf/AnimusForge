using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Helpers;
using HarmonyLib;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class DuelSettings
{
	[SettingPropertyFloatingInteger("军事演习死亡率强度", 0f, 1f, "#0%", Order = 0, RequireRestart = false, HintText = "仅用于军事演习。0% = 所有人类单位倒地只伤不死；50% = 使用原版死亡率；100% = 原版允许死亡的目标倒地时必死。")]
	[SettingPropertyGroup("11. 军事演习")]
	public float MilitaryExerciseDeathRate { get; set; } = 0.02f;

	[SettingPropertyBool("玩家可战死", Order = 1, RequireRestart = false, HintText = "仅用于军事演习。开启：玩家本人按军事演习死亡率强度可能战死。关闭：玩家本人倒地后必定只伤不死。")]
	[SettingPropertyGroup("11. 军事演习")]
	public bool MilitaryExerciseAllowPlayerDeath { get; set; } = false;

	[SettingPropertyBool("同伴可战死", Order = 2, RequireRestart = false, HintText = "仅用于军事演习。开启：参演英雄同伴按军事演习死亡率强度可能战死。关闭：参演英雄同伴倒地后必定只伤不死。普通士兵不受此开关保护。")]
	[SettingPropertyGroup("11. 军事演习")]
	public bool MilitaryExerciseAllowCompanionDeath { get; set; } = false;

	public static int MilitaryExerciseDeathRatePercent
	{
		get
		{
			float deathRate = 0f;
			try
			{
				deathRate = GetSettings()?.MilitaryExerciseDeathRate ?? 0f;
			}
			catch
			{
				deathRate = 0f;
			}
			if (float.IsNaN(deathRate) || float.IsInfinity(deathRate))
			{
				deathRate = 0f;
			}
			if (deathRate < 0f)
			{
				deathRate = 0f;
			}
			if (deathRate > 1f)
			{
				deathRate = 1f;
			}
			return (int)Math.Round(deathRate * 100f);
		}
	}

	public static bool MilitaryExerciseAllowPlayerDeathEnabled
	{
		get
		{
			try
			{
				return GetSettings()?.MilitaryExerciseAllowPlayerDeath ?? false;
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool MilitaryExerciseAllowCompanionDeathEnabled
	{
		get
		{
			try
			{
				return GetSettings()?.MilitaryExerciseAllowCompanionDeath ?? false;
			}
			catch
			{
				return false;
			}
		}
	}
}

internal sealed class MilitaryExerciseMissionLogic : MissionLogic
{
	private readonly MilitaryExerciseBehavior.MilitaryExerciseRuntime _runtime;

	private BattleEndLogic _battleEndLogic;

	private bool _deploymentWasActive;

	private bool _deploymentEndDetected;

	private bool _settlementRequested;

	private bool _playerSideEverHadCombatAgent;

	private bool _enemySideEverHadCombatAgent;

	private float _nextBattleEndDisableRetryTime = 1f;

	private MissionMode _lastMissionMode;

	private bool _enemyFormationCaptainsAssigned;

	private bool _missionEndedLogged;

	public MilitaryExerciseMissionLogic(MilitaryExerciseBehavior.MilitaryExerciseRuntime runtime)
	{
		_runtime = runtime;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		CacheBattleEndLogic();
		TryDisableBattleEndLogic("OnBehaviorInitialize");
		_lastMissionMode = base.Mission?.Mode ?? MissionMode.StartUp;
		_deploymentWasActive = _lastMissionMode == MissionMode.Deployment;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		if (_battleEndLogic == null)
		{
			CacheBattleEndLogic();
		}
		TryDisableBattleEndLogic("AfterStart");
		_lastMissionMode = base.Mission?.Mode ?? MissionMode.StartUp;
		_deploymentWasActive = _lastMissionMode == MissionMode.Deployment;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		RetryBattleEndDisableIfNeeded();
		DetectDeploymentEnd();
		TryAssignEnemyFormationCaptains();
		TryRequestSettlementForDefeatedSide();
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		return null;
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (!_missionEndedLogged)
		{
			_missionEndedLogged = true;
		}
		// Do not clean up here. Native battle missions can call MissionEnded while
		// probing end conditions during startup/ticks; cleanup here destroys the
		// MapEvent/dummy parties under a still-running mission and can crash.
		// Real cleanup is handled by OnEndMission/OnRemoveBehavior or by our own
		// side-defeated path.
		return false;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
	}

	protected override void OnEndMission()
	{
		MarkPlayerDownIfNeeded();
		MilitaryExerciseBehavior.TryCommitXpOnlyBeforeMissionMapEventRemoval(_runtime, "OnEndMission");
		base.OnEndMission();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if ((agentState == AgentState.Killed || agentState == AgentState.Unconscious)
			&& affectedAgent != null
			&& base.Mission != null
			&& affectedAgent == base.Mission.MainAgent)
		{
			_runtime.PlayerWasDownInExercise = true;
		}
		if (agentState == AgentState.Killed || agentState == AgentState.Unconscious)
		{
			TryRequestSettlementForDefeatedSide();
		}
	}

	internal void TryDisableBattleEndLogic(string source)
	{
		try
		{
			if (base.Mission == null)
			{
				return;
			}
			if (_battleEndLogic == null)
			{
				CacheBattleEndLogic();
			}
			if (_battleEndLogic == null)
			{
				Log("battle_end_disable failed source=" + source + " BattleEndLogic=null");
				return;
			}
			_battleEndLogic.ChangeCanCheckForEndCondition(true);
		}
		catch (Exception ex)
		{
			Log("battle_end_disable exception source=" + source + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void CacheBattleEndLogic()
	{
		try
		{
			_battleEndLogic = base.Mission?.GetMissionBehavior<BattleEndLogic>();
		}
		catch (Exception ex)
		{
			Log("cache_battle_end_logic failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RetryBattleEndDisableIfNeeded()
	{
		if (_battleEndLogic != null || base.Mission == null || base.Mission.CurrentTime < _nextBattleEndDisableRetryTime)
		{
			return;
		}
		_nextBattleEndDisableRetryTime = base.Mission.CurrentTime + 1f;
		CacheBattleEndLogic();
		if (_battleEndLogic != null)
		{
			TryDisableBattleEndLogic("retry_tick");
		}
	}

	private void DetectDeploymentEnd()
	{
		if (_deploymentEndDetected || base.Mission == null)
		{
			return;
		}
		try
		{
			MissionMode currentMode = base.Mission.Mode;
			if (_lastMissionMode != currentMode)
			{
			}
			if (_deploymentWasActive && currentMode != MissionMode.Deployment)
			{
				_deploymentEndDetected = true;
				TryDisableBattleEndLogic("deployment_ended");
			}
			if (!_deploymentWasActive && currentMode != MissionMode.Deployment && base.Mission.CurrentTime > 2f)
			{
				_deploymentEndDetected = true;
			}
			_deploymentWasActive = currentMode == MissionMode.Deployment;
			_lastMissionMode = currentMode;
		}
		catch (Exception ex)
		{
			Log("detect_deployment_end failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void TryRequestSettlementForDefeatedSide()
	{
		if (_settlementRequested || base.Mission == null || !_deploymentEndDetected || base.Mission.CurrentTime < 2f)
		{
			return;
		}
		try
		{
			BattleSideEnum playerSide = base.Mission.PlayerTeam?.Side ?? PartyBase.MainParty.Side;
			BattleSideEnum enemySide = playerSide.GetOppositeSide();
			int playerActive = CountActiveHumanCombatAgents(playerSide);
			int enemyActive = CountActiveHumanCombatAgents(enemySide);
			if (playerActive > 0)
			{
				_playerSideEverHadCombatAgent = true;
			}
			if (enemyActive > 0)
			{
				_enemySideEverHadCombatAgent = true;
			}
			if (!_playerSideEverHadCombatAgent || !_enemySideEverHadCombatAgent)
			{
				return;
			}
		}
		catch (Exception ex)
		{
			Log("defeated_side_check failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private int CountActiveHumanCombatAgents(BattleSideEnum side)
	{
		int count = 0;
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				if (agent.Team == null || agent.Team.Side != side)
				{
					continue;
				}
				if (agent.State != AgentState.Active || agent.Health <= 0f)
				{
					continue;
				}
				count++;
			}
		}
		catch
		{
		}
		return count;
	}

	private void RequestTemporarySettlement(string reason, bool endMission)
	{
		if (_settlementRequested)
		{
			return;
		}
		_settlementRequested = true;
		MarkPlayerDownIfNeeded();
		if (endMission)
		{
			try
			{
				if (base.Mission != null)
				{
					base.Mission.NextCheckTimeEndMission = 0f;
					base.Mission.EndMission();
				}
			}
			catch (Exception ex)
			{
				Log("end_mission failed: " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		MilitaryExerciseBehavior.CleanupExerciseRuntime(_runtime, reason, skipXpCommit: false);
	}

	private void MarkPlayerDownIfNeeded()
	{
		try
		{
			Agent mainAgent = base.Mission?.MainAgent;
			if (mainAgent != null && (mainAgent.State == AgentState.Killed || mainAgent.State == AgentState.Unconscious || mainAgent.Health <= 0f))
			{
				_runtime.PlayerWasDownInExercise = true;
			}
		}
		catch (Exception ex)
		{
			Log("player_down_detect failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void Log(string message)
	{
		string session = _runtime?.TestSessionId ?? "";
		if (!string.IsNullOrWhiteSpace(session))
		{
			string line = "[MilitaryExercise] session=" + session + " " + message;
			MilitaryExerciseBehavior.WriteDiagnosticLine(line);
		}
		else
		{
			string line = "[MilitaryExercise] " + message;
			MilitaryExerciseBehavior.WriteDiagnosticLine(line);
		}
	}

	private void TryAssignEnemyFormationCaptains()
	{
		if (_enemyFormationCaptainsAssigned || base.Mission == null || _runtime?.OpponentDummyParty?.Party == null)
		{
			return;
		}
		try
		{
			BattleSideEnum playerSide = base.Mission.PlayerTeam?.Side ?? PartyBase.MainParty.Side;
			BattleSideEnum enemySide = playerSide.GetOppositeSide();
			List<Agent> heroAgents = new List<Agent>();
			Dictionary<Formation, int> formationCounts = new Dictionary<Formation, int>();
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Team == null || agent.Team.Side != enemySide || agent.Formation == null)
				{
					continue;
				}
				PartyBase agentParty = agent.Origin?.BattleCombatant as PartyBase;
				if (!ReferenceEquals(agentParty, _runtime.OpponentDummyParty.Party))
				{
					continue;
				}
				formationCounts.TryGetValue(agent.Formation, out int count);
				formationCounts[agent.Formation] = count + 1;
				CharacterObject character = agent.Character as CharacterObject;
				if (character?.IsHero == true)
				{
					heroAgents.Add(agent);
				}
			}
			if (formationCounts.Count <= 0)
			{
				return;
			}
			_enemyFormationCaptainsAssigned = true;
			if (heroAgents.Count <= 0)
			{
				return;
			}
			foreach (KeyValuePair<Formation, int> pair in formationCounts)
			{
				Formation formation = pair.Key;
				if (formation == null)
				{
					continue;
				}
				Agent existingCaptain = formation.Captain;
				if (existingCaptain != null && existingCaptain.IsActive())
				{
					continue;
				}
				Agent captain = ChooseCaptainAgentForFormation(heroAgents, formation, _runtime.OpponentLeaderHero);
				if (captain == null)
				{
					continue;
				}
				formation.Captain = captain;
			}
		}
		catch (Exception ex)
		{
			Log("combat_bonus_captain_assign failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static Agent ChooseCaptainAgentForFormation(List<Agent> heroAgents, Formation formation, Hero preferredLeader)
	{
		if (heroAgents == null || formation == null)
		{
			return null;
		}
		Agent best = null;
		int bestScore = int.MinValue;
		foreach (Agent agent in heroAgents)
		{
			if (agent == null || agent.Formation != formation || !agent.IsActive())
			{
				continue;
			}
			CharacterObject character = agent.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			int score = hero == preferredLeader ? 100000 : 0;
			score += GetCombatLeaderScore(hero);
			if (score > bestScore)
			{
				bestScore = score;
				best = agent;
			}
		}
		if (best != null)
		{
			return best;
		}
		foreach (Agent agent in heroAgents)
		{
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			CharacterObject character = agent.Character as CharacterObject;
			if (character?.HeroObject == preferredLeader)
			{
				return agent;
			}
		}
		return null;
	}

	private static int GetCombatLeaderScore(Hero hero)
	{
		if (hero == null)
		{
			return 0;
		}
		try
		{
			return hero.GetSkillValue(DefaultSkills.Leadership)
				+ hero.GetSkillValue(DefaultSkills.Tactics)
				+ hero.GetSkillValue(DefaultSkills.OneHanded)
				+ hero.GetSkillValue(DefaultSkills.TwoHanded)
				+ hero.GetSkillValue(DefaultSkills.Polearm)
				+ hero.GetSkillValue(DefaultSkills.Bow)
				+ hero.GetSkillValue(DefaultSkills.Crossbow)
				+ hero.GetSkillValue(DefaultSkills.Throwing)
				+ hero.GetSkillValue(DefaultSkills.Riding)
				+ hero.GetSkillValue(DefaultSkills.Athletics);
		}
		catch
		{
			return 0;
		}
	}
}

[HarmonyPatch(typeof(SandBox.GameComponents.SandboxAgentDecideKilledOrUnconsciousModel), "GetAgentStateProbability")]
public static class MilitaryExerciseDeathRatePatch
{
	public static void Postfix(Agent affectorAgent, Agent effectedAgent, DamageTypes damageType, ref float __result)
	{
		try
		{
			if (Mission.Current?.GetMissionBehavior<MilitaryExerciseMissionLogic>() == null)
			{
				return;
			}
			if (effectedAgent == null || !effectedAgent.IsHuman)
			{
				return;
			}
			if (MilitaryExerciseBehavior.ShouldProtectPlayerOrCompanionFromDeath(effectedAgent))
			{
				__result = 0f;
				return;
			}
			float vanilla = __result;
			float slider = DuelSettings.MilitaryExerciseDeathRatePercent / 100f;
			if (float.IsNaN(slider) || float.IsInfinity(slider))
			{
				slider = 0.5f;
			}
			slider = MBMath.ClampFloat(slider, 0f, 1f);
			if (slider <= 0.5f)
			{
				__result = vanilla * (slider / 0.5f);
			}
			else
			{
				__result = vanilla + (1f - vanilla) * ((slider - 0.5f) / 0.5f);
			}
			__result = MBMath.ClampFloat(__result, 0f, 1f);
		}
		catch
		{
		}
	}
}

[HarmonyPatch(typeof(MapEvent), "CalculateAndCommitMapEventResults")]
public static class MilitaryExerciseMapEventXpOnlySettlementPatch
{
	public static bool Prefix(MapEvent __instance)
	{
		try
		{
			if (!MilitaryExerciseBehavior.TryHandleVanillaMapEventResultsXpOnly(__instance, "MapEvent.CalculateAndCommitMapEventResults"))
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("xp_only_map_event_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoApplyMapEventResults")]
public static class MilitaryExercisePlayerEncounterResultsCleanupPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !MilitaryExerciseBehavior.TryHandlePlayerEncounterApplyResultsXpOnly(__instance, "PlayerEncounter.DoApplyMapEventResults");
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("player_encounter_results_cleanup_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoWait")]
public static class MilitaryExercisePlayerEncounterContinueCleanupPatch
{
	public static void Postfix(PlayerEncounter __instance)
	{
		try
		{
			MilitaryExerciseBehavior.TryCleanupAfterVanillaPlayerEncounterContinue(__instance, "PlayerEncounter.DoWait");
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("player_encounter_continue_cleanup_postfix", ex);
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoPlayerVictory")]
public static class MilitaryExercisePlayerEncounterVictoryCleanupPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !MilitaryExerciseBehavior.TryHandlePlayerEncounterTerminalStateCleanup(__instance, "PlayerEncounter.DoPlayerVictory");
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("player_encounter_victory_cleanup_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoPlayerDefeat")]
public static class MilitaryExercisePlayerEncounterDefeatCleanupPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !MilitaryExerciseBehavior.TryHandlePlayerEncounterTerminalStateCleanup(__instance, "PlayerEncounter.DoPlayerDefeat");
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("player_encounter_defeat_cleanup_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoEnd")]
public static class MilitaryExercisePlayerEncounterEndCleanupPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !MilitaryExerciseBehavior.TryHandlePlayerEncounterTerminalStateCleanup(__instance, "PlayerEncounter.DoEnd");
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("player_encounter_end_cleanup_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "GetBattleRewards")]
public static class MilitaryExerciseBattleRewardsZeroPatch
{
	public static bool Prefix(
		out float renownChange,
		out float influenceChange,
		out float moraleChange,
		out float goldChange,
		out float playerEarnedLootPercentage,
		out Figurehead playerEarnedFigurehead,
		ref ExplainedNumber renownExplainedNumber,
		ref ExplainedNumber influenceExplainedNumber,
		ref ExplainedNumber moraleExplainedNumber)
	{
		renownChange = 0f;
		influenceChange = 0f;
		moraleChange = 0f;
		goldChange = 0f;
		playerEarnedLootPercentage = 0f;
		playerEarnedFigurehead = null;
		try
		{
			if (!MilitaryExerciseBehavior.ShouldZeroBattleRewardsForExercise("PlayerEncounter.GetBattleRewards"))
			{
				return true;
			}
			renownExplainedNumber = default;
			influenceExplainedNumber = default;
			moraleExplainedNumber = default;
			return false;
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("battle_rewards_zero_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch(typeof(MapEvent), "ApplyRenownAndInfluenceChanges")]
public static class MilitaryExerciseRenownInfluenceSkipPatch
{
	public static bool Prefix(MapEvent __instance)
	{
		try
		{
			if (!MilitaryExerciseBehavior.ShouldSkipRenownInfluenceForExercise(__instance, "MapEvent.ApplyRenownAndInfluenceChanges"))
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			MilitaryExerciseBehavior.LogPatchException("renown_influence_skip_prefix", ex);
			return true;
		}
	}
}

[HarmonyPatch]
public static class MilitaryExerciseBehavior
{
	private const string OpponentDummyPartyPrefix = "animusforge_military_exercise_opponent_";

	private const string HoldingDummyPartyPrefix = "animusforge_military_exercise_holding_";


	private static readonly FieldInfo MapEventPartyRosterField = typeof(MapEventParty).GetField("_roster", BindingFlags.Instance | BindingFlags.NonPublic);

	private static MilitaryExerciseRuntime _runtime;

	private static PendingSelection _pendingSelection;

	private static bool _isOpening;

	private static bool _queuedOpenSecondTeam;

	private static float _queuedOpenSecondTeamAt;

	private static bool _queuedOpenBattle;

	private static float _queuedOpenBattleAt;

	private static bool _harmonyPatched;

	private static float _nextOrphanDummyCleanupAt;


	public static void OnEngineTick()
	{
		EnsureHarmonyPatched();
		TryCleanupOrphanDummyPartiesOnTick();
		TryCleanupFinishedExerciseRuntimeOnTick();
		if (_queuedOpenSecondTeam)
		{
			if (_pendingSelection == null || _pendingSelection.Stage != MilitaryExerciseSelectionStage.SecondTeam)
			{
				_queuedOpenSecondTeam = false;
			}
			else
			{
				float now = (float)Environment.TickCount / 1000f;
				if (now >= _queuedOpenSecondTeamAt)
				{
					try
					{
						if (!IsPartyScreenStillActive())
						{
							_queuedOpenSecondTeam = false;
							_isOpening = true;
							if (!CanOpenFromCurrentState(out _, out string blockedReason))
							{
								Display(blockedReason);
								ResetPendingSelection("queued_second_blocked");
								return;
							}
							OpenSecondTeamSelection(_pendingSelection.RemainingAfterFirstRoster);
						}
					}
					catch (Exception ex)
					{
						Log("queued second failed: " + ex.GetType().Name + ": " + ex.Message);
						ResetPendingSelection("queued_second_exception");
						DisplayFailure("打开选择界面失败", ex);
					}
				}
			}
		}
		if (!_queuedOpenBattle)
		{
			return;
		}
		try
		{
			if (_runtime == null || _runtime.SettlementDone)
			{
				_queuedOpenBattle = false;
				return;
			}
			float now = (float)Environment.TickCount / 1000f;
			if (now < _queuedOpenBattleAt)
			{
				return;
			}
			if (IsPartyScreenStillActive())
			{
				return;
			}
			if (Mission.Current != null)
			{
				return;
			}
			_queuedOpenBattle = false;
			_isOpening = true;
			OpenMilitaryExerciseBattleMission(_runtime);
			_isOpening = false;
			Display("军事演习开始。");
		}
		catch (Exception ex)
		{
			Log("queued battle_open failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			CleanupExerciseRuntime(_runtime, "battle_open_failed", skipXpCommit: true);
			_runtime = null;
			_isOpening = false;
			_queuedOpenBattle = false;
			DisplayFailure("军事演习开启失败", ex);
		}
	}

	public static void OpenExerciseFromTerminal()
	{
		EnsureHarmonyPatched();
		if (_isOpening)
		{
			Display("军事演习正在准备中。");
			return;
		}
		if (_pendingSelection != null && _pendingSelection.Stage == MilitaryExerciseSelectionStage.SecondTeam)
		{
			_isOpening = true;
			try
			{
				if (!CanOpenFromCurrentState(out _, out string blockedReason))
				{
					Display(blockedReason);
					_isOpening = false;
					return;
				}
				OpenSecondTeamSelection(_pendingSelection.RemainingAfterFirstRoster);
			}
			catch (Exception ex)
			{
				Log("resume second failed: " + ex.GetType().Name + ": " + ex.Message);
				ResetPendingSelection("resume_second_exception");
				DisplayFailure("打开选择界面失败", ex);
			}
			return;
		}
		if (IsCurrentExerciseRuntime())
		{
			Display("军事演习正在处理中。");
			return;
		}
		_isOpening = true;
		try
		{
			if (!CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason))
			{
				Display(blockedReason);
				Log("precheck blocked: " + blockedReason);
				_isOpening = false;
				return;
			}
			int selectableMembers = CountSelectableNonPlayerMembers(mainParty.MemberRoster);
			OpenFirstTeamSelection(mainParty);
		}
		catch (Exception ex)
		{
			Log("open failed: " + ex.GetType().Name + ": " + ex.Message);
			ResetPendingSelection("open_exception");
			DisplayFailure("打开军事演习失败", ex);
		}
	}

	public static bool IsCurrentExerciseRuntime()
	{
		return _runtime != null && !_runtime.SettlementDone;
	}

	internal static MilitaryExerciseRuntime GetCurrentRuntime()
	{
		return _runtime;
	}

	internal static bool ShouldProtectPlayerOrCompanionFromDeath(Agent agent)
	{
		try
		{
			CharacterObject character = agent?.Character as CharacterObject;
			if (character == null)
			{
				return false;
			}
			if (character.IsPlayerCharacter)
			{
				return !DuelSettings.MilitaryExerciseAllowPlayerDeathEnabled;
			}
			if (character.IsHero)
			{
				return !DuelSettings.MilitaryExerciseAllowCompanionDeathEnabled;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryHandleVanillaMapEventResultsXpOnly(MapEvent mapEvent, string source)
	{
		MilitaryExerciseRuntime runtime = GetRuntimeForMapEvent(mapEvent);
		if (runtime == null)
		{
			if (!IsMilitaryExerciseMapEventByDummyParty(mapEvent))
			{
				return false;
			}
			bool orphanXpCommitted = CommitXpGainsForMapEvent(mapEvent);
			try
			{
				SetPrivateField(mapEvent, "_mapEventResultsApplied", true);
			}
			catch
			{
			}
			return true;
		}
		runtime.VanillaResultPatchHit = true;
		if (!runtime.XpCommittedByVanillaPatch)
		{
			runtime.XpCommitSucceeded = CommitXpOnlyForRuntime(runtime, mapEvent, source);
			runtime.XpCommittedByVanillaPatch = true;
		}
		else
		{
		}
		try
		{
			SetPrivateField(mapEvent, "_mapEventResultsApplied", true);
		}
		catch
		{
		}
		return true;
	}

	internal static void TryCleanupAfterVanillaPlayerEncounterResults(PlayerEncounter encounter, string source)
	{
		MilitaryExerciseRuntime runtime = _runtime;
		if (runtime == null || runtime.SettlementDone)
		{
			return;
		}
		MapEvent encounterMapEvent = GetPrivateField<MapEvent>(encounter, "_mapEvent");
		if (!runtime.VanillaResultPatchHit && !ReferenceEquals(runtime.MapEvent, encounterMapEvent))
		{
			return;
		}
		CleanupExerciseRuntime(runtime, source, skipXpCommit: runtime.XpCommittedByVanillaPatch && runtime.XpCommitSucceeded);
	}

	internal static bool TryHandlePlayerEncounterApplyResultsXpOnly(PlayerEncounter encounter, string source)
	{
		MapEvent encounterMapEvent = GetPrivateField<MapEvent>(encounter, "_mapEvent");
		if (encounterMapEvent == null)
		{
			encounterMapEvent = MapEvent.PlayerMapEvent;
		}
		MilitaryExerciseRuntime runtime = GetRuntimeForMapEvent(encounterMapEvent);
		if (runtime == null && !IsMilitaryExerciseMapEventByDummyParty(encounterMapEvent))
		{
			return false;
		}
		if (runtime != null)
		{
			runtime.VanillaResultPatchHit = true;
			if (!runtime.XpCommittedByVanillaPatch)
			{
				runtime.XpCommitSucceeded = CommitXpOnlyForRuntime(runtime, encounterMapEvent, source);
				runtime.XpCommittedByVanillaPatch = true;
			}
			CleanupExerciseRuntime(runtime, source, skipXpCommit: true);
		}
		else
		{
			bool xpCommitted = CommitXpGainsForMapEvent(encounterMapEvent);
			CleanupOrphanDummyPartiesForMapEvent(encounterMapEvent, source);
		}
		SetPlayerEncounterState(encounter, "End");
		SetPrivateField(encounter, "_stateHandled", true);
		return true;
	}

	internal static bool TryHandlePlayerEncounterTerminalStateCleanup(PlayerEncounter encounter, string source)
	{
		MapEvent encounterMapEvent = GetPrivateField<MapEvent>(encounter, "_mapEvent");
		if (encounterMapEvent == null)
		{
			encounterMapEvent = MapEvent.PlayerMapEvent;
		}
		MilitaryExerciseRuntime runtime = GetRuntimeForMapEvent(encounterMapEvent);
		if (runtime == null && !IsMilitaryExerciseMapEventByDummyParty(encounterMapEvent))
		{
			return false;
		}
		if (runtime != null)
		{
			bool skipXp = runtime.XpCommittedByVanillaPatch && runtime.XpCommitSucceeded;
			CleanupExerciseRuntime(runtime, source, skipXpCommit: skipXp);
		}
		else
		{
			bool xpCommitted = CommitXpGainsForMapEvent(encounterMapEvent);
			CleanupOrphanDummyPartiesForMapEvent(encounterMapEvent, source);
		}
		SetPlayerEncounterState(encounter, "End");
		SetPrivateField(encounter, "_stateHandled", true);
		return true;
	}

	internal static void TryCleanupAfterVanillaPlayerEncounterContinue(PlayerEncounter encounter, string source)
	{
		MilitaryExerciseRuntime runtime = _runtime;
		if (runtime == null || runtime.SettlementDone || !runtime.RenownInfluenceSkipped)
		{
			return;
		}
		MapEvent encounterMapEvent = GetPrivateField<MapEvent>(encounter, "_mapEvent");
		if (!ReferenceEquals(runtime.MapEvent, encounterMapEvent))
		{
			return;
		}
		CleanupExerciseRuntime(runtime, source, skipXpCommit: runtime.XpCommittedByVanillaPatch && runtime.XpCommitSucceeded);
	}

	internal static bool ShouldSkipRenownInfluenceForExercise(MapEvent mapEvent, string source)
	{
		MilitaryExerciseRuntime runtime = GetRuntimeForMapEvent(mapEvent);
		if (runtime == null)
		{
			if (!IsMilitaryExerciseMapEventByDummyParty(mapEvent))
			{
				return false;
			}
			return true;
		}
		runtime.RenownInfluenceSkipped = true;
		if (!runtime.VanillaResultPatchHit && !runtime.XpCommittedByVanillaPatch)
		{
			runtime.XpCommittedByVanillaPatch = true;
			runtime.XpCommitSucceeded = true;
		}
		return true;
	}

	internal static bool ShouldZeroBattleRewardsForExercise(string source)
	{
		MapEvent mapEvent = MapEvent.PlayerMapEvent;
		MilitaryExerciseRuntime runtime = GetRuntimeForMapEvent(mapEvent);
		if (runtime == null && !IsMilitaryExerciseMapEventByDummyParty(mapEvent))
		{
			return false;
		}
		return true;
	}

	internal static void TryCommitXpOnlyBeforeMissionMapEventRemoval(MilitaryExerciseRuntime runtime, string source)
	{
		try
		{
			if (runtime == null || runtime.SettlementDone)
			{
				return;
			}
			if (runtime.XpCommittedByVanillaPatch)
			{
				return;
			}
			if (runtime.MapEvent == null)
			{
				return;
			}
			runtime.XpCommitSucceeded = CommitXpOnlyForRuntime(runtime, runtime.MapEvent, source + ".early_xp_only");
			runtime.XpCommittedByVanillaPatch = true;
			if (runtime.XpCommitSucceeded && string.Equals(source, "OnEndMission", StringComparison.Ordinal))
			{
				runtime.EarlyXpCommittedOnMissionEnd = true;
			}
			Log($"xp_early_commit_done source={source} success={runtime.XpCommitSucceeded}");
		}
		catch (Exception ex)
		{
			Log($"xp_early_commit_failed source={source} {ex.GetType().Name}: {ex.Message}");
		}
	}

	internal static void LogPatchException(string source, Exception ex)
	{
		Log(source + " failed " + ex.GetType().Name + ": " + ex.Message);
	}

	private static MilitaryExerciseRuntime GetRuntimeForMapEvent(MapEvent mapEvent)
	{
		MilitaryExerciseRuntime runtime = _runtime;
		if (runtime == null || runtime.SettlementDone || mapEvent == null)
		{
			return null;
		}
		if (ReferenceEquals(runtime.MapEvent, mapEvent))
		{
			return runtime;
		}
		if (IsMilitaryExerciseMapEventByDummyParty(mapEvent))
		{
			return runtime;
		}
		try
		{
			if (ReferenceEquals(MapEvent.PlayerMapEvent, mapEvent))
			{
				return runtime;
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool IsMilitaryExerciseMapEventByDummyParty(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		return MapEventSideHasMilitaryExerciseDummyParty(mapEvent.AttackerSide)
			|| MapEventSideHasMilitaryExerciseDummyParty(mapEvent.DefenderSide);
	}

	private static bool MapEventSideHasMilitaryExerciseDummyParty(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				string id = party?.Party?.MobileParty?.StringId ?? "";
				if (id.StartsWith(OpponentDummyPartyPrefix, StringComparison.Ordinal)
					|| id.StartsWith(HoldingDummyPartyPrefix, StringComparison.Ordinal))
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

	private static bool CommitXpOnlyForRuntime(MilitaryExerciseRuntime runtime, MapEvent mapEvent, string source)
	{
		Dictionary<CharacterObject, RosterTotals> mainBeforeXp = BuildRosterTotals(MobileParty.MainParty?.MemberRoster);
		Dictionary<CharacterObject, RosterTotals> opponentBeforeXp = BuildRosterTotals(runtime.OpponentDummyParty?.MemberRoster);
		Dictionary<CharacterObject, RosterTotals> holdingBeforeXp = BuildRosterTotals(runtime.HoldingDummyParty?.MemberRoster);
		bool success = CommitXpGainsForMapEvent(mapEvent);
		int mainXpDelta = CalculateRosterXpDelta(mainBeforeXp, BuildRosterTotals(MobileParty.MainParty?.MemberRoster));
		int opponentXpDelta = CalculateRosterXpDelta(opponentBeforeXp, BuildRosterTotals(runtime.OpponentDummyParty?.MemberRoster));
		int holdingXpDelta = CalculateRosterXpDelta(holdingBeforeXp, BuildRosterTotals(runtime.HoldingDummyParty?.MemberRoster));
		Log($"xp_delta_summary main={mainXpDelta} opponent={opponentXpDelta} holding={holdingXpDelta}");
		return success;
	}

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (_harmonyPatched)
		{
			return;
		}
		try
		{
			harmony ??= new Harmony("com.AnimusForge.militaryexercise");
			PatchHarmonyClass(harmony, typeof(MilitaryExerciseDeathRatePatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExerciseMapEventXpOnlySettlementPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExercisePlayerEncounterResultsCleanupPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExercisePlayerEncounterContinueCleanupPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExercisePlayerEncounterVictoryCleanupPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExercisePlayerEncounterDefeatCleanupPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExercisePlayerEncounterEndCleanupPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExerciseBattleRewardsZeroPatch));
			PatchHarmonyClass(harmony, typeof(MilitaryExerciseRenownInfluenceSkipPatch));
			_harmonyPatched = true;
		}
		catch (Exception ex)
		{
			Log("harmony_patch_failed " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void EnsureHarmonyPatched()
	{
		RegisterHarmonyPatches(null);
	}

	private static void PatchHarmonyClass(Harmony harmony, Type patchType)
	{
		try
		{
			harmony.CreateClassProcessor(patchType).Patch();
		}
		catch (Exception ex)
		{
			Log("harmony_patch_class_failed type=" + patchType?.Name + " " + ex.GetType().Name + ": " + ex.Message);
			throw;
		}
	}

	private static void TryCleanupFinishedExerciseRuntimeOnTick()
	{
		try
		{
			MilitaryExerciseRuntime runtime = _runtime;
			if (runtime == null || runtime.SettlementDone || runtime.SettlementStarted || runtime.IsOpening || Mission.Current != null)
			{
				return;
			}
			MapEvent mapEvent = runtime.MapEvent;
			if (mapEvent == null)
			{
				return;
			}
			bool hasTerminalBattleState = mapEvent.BattleState == BattleState.AttackerVictory
				|| mapEvent.BattleState == BattleState.DefenderVictory
				|| mapEvent.BattleState == BattleState.DefenderPullBack;
			bool waitingRemoval = false;
			try
			{
				waitingRemoval = mapEvent.State == MapEventState.WaitingRemoval;
			}
			catch
			{
			}
			bool earlyMissionEndCleanup = runtime.EarlyXpCommittedOnMissionEnd && runtime.XpCommitSucceeded;
			if (!hasTerminalBattleState && !waitingRemoval && !earlyMissionEndCleanup)
			{
				return;
			}
			bool skipXp = runtime.XpCommittedByVanillaPatch && runtime.XpCommitSucceeded;
			Log($"tick_finished_runtime_cleanup battle_state={mapEvent.BattleState} map_event_state={mapEvent.State} early_mission_end_cleanup={earlyMissionEndCleanup} skip_xp_commit={skipXp} xp_committed_by_patch={runtime.XpCommittedByVanillaPatch} xp_success={runtime.XpCommitSucceeded}");
			CleanupExerciseRuntime(runtime, "OnEngineTick.finished_runtime", skipXpCommit: skipXp);
		}
		catch (Exception ex)
		{
			Log("tick_finished_runtime_cleanup failed " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void TryCleanupOrphanDummyPartiesOnTick()
	{
		try
		{
			if (Campaign.Current == null || Mission.Current != null)
			{
				return;
			}
			float now = (float)Environment.TickCount / 1000f;
			if (now < _nextOrphanDummyCleanupAt)
			{
				return;
			}
			_nextOrphanDummyCleanupAt = now + 3f;
			List<MobileParty> orphanDummies = new List<MobileParty>();
			foreach (MobileParty party in MobileParty.All)
			{
				if (party == null || !party.IsActive || !IsMilitaryExerciseDummyParty(party))
				{
					continue;
				}
				if (_runtime != null && !_runtime.SettlementDone
					&& (ReferenceEquals(party, _runtime.OpponentDummyParty) || ReferenceEquals(party, _runtime.HoldingDummyParty)))
				{
					continue;
				}
				orphanDummies.Add(party);
			}
			foreach (MobileParty orphan in orphanDummies)
			{
				CleanupOrphanDummyParty(orphan);
			}
		}
		catch (Exception ex)
		{
			Log("orphan_dummy_cleanup_tick failed " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupOrphanDummyParty(MobileParty orphan)
	{
		if (orphan == null)
		{
			return;
		}
		string id = orphan.StringId ?? "";
		try
		{
			MapEvent mapEvent = orphan.MapEvent;
			MoveAllMembersBackToMainParty(orphan, "orphan_dummy");
			CleanupMapEventAndPlayerEncounter(mapEvent, "orphan_dummy_cleanup");
			string prefix = id.StartsWith(HoldingDummyPartyPrefix, StringComparison.Ordinal) ? HoldingDummyPartyPrefix : OpponentDummyPartyPrefix;
			DestroyDummyParty(orphan, prefix, "orphan_dummy");
		}
		catch (Exception ex)
		{
			Log($"orphan_dummy_cleanup failed id={id} {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void CleanupOrphanDummyPartiesForMapEvent(MapEvent mapEvent, string source)
	{
		try
		{
			HashSet<MobileParty> parties = new HashSet<MobileParty>();
			AddDummyPartiesFromSide(mapEvent?.AttackerSide, parties);
			AddDummyPartiesFromSide(mapEvent?.DefenderSide, parties);
			foreach (MobileParty party in parties)
			{
				CleanupOrphanDummyParty(party);
			}
			CleanupMapEventAndPlayerEncounter(mapEvent, source + "_orphan_mapevent");
		}
		catch (Exception ex)
		{
			Log($"orphan_dummy_cleanup_from_mapevent failed source={source} {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void AddDummyPartiesFromSide(MapEventSide side, HashSet<MobileParty> parties)
	{
		if (side == null || parties == null)
		{
			return;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				MobileParty mobileParty = party?.Party?.MobileParty;
				if (IsMilitaryExerciseDummyParty(mobileParty))
				{
					parties.Add(mobileParty);
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsMilitaryExerciseDummyParty(MobileParty party)
	{
		string id = party?.StringId ?? "";
		return id.StartsWith(OpponentDummyPartyPrefix, StringComparison.Ordinal)
			|| id.StartsWith(HoldingDummyPartyPrefix, StringComparison.Ordinal);
	}

	private static bool CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason)
	{
		mainParty = MobileParty.MainParty;
		blockedReason = "";
		try
		{
			if (Campaign.Current == null || mainParty == null || PartyBase.MainParty == null)
			{
				blockedReason = "当前没有可用于军事演习的玩家部队。";
				return false;
			}
			if (Mission.Current != null)
			{
				blockedReason = "当前任务中无法开始军事演习。";
				return false;
			}
			if (PlayerEncounter.Current != null || MapEvent.PlayerMapEvent != null || mainParty.MapEvent != null)
			{
				blockedReason = "当前已有遭遇或战斗事件，无法开始军事演习。";
				return false;
			}
			if (CountSelectableNonPlayerMembers(mainParty.MemberRoster) <= 0)
			{
				blockedReason = "玩家部队中除玩家外没有可用于第二队选择的成员。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("precheck exception: " + ex.GetType().Name + ": " + ex.Message);
			blockedReason = "当前状态无法开始军事演习。";
			return false;
		}
	}

	private static int CountSelectableNonPlayerMembers(TroopRoster roster)
	{
		int count = 0;
		if (roster == null)
		{
			return count;
		}
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter)
			{
				continue;
			}
			count += Math.Max(0, item.Number);
		}
		return count;
	}

	private static void OpenFirstTeamSelection(MobileParty mainParty)
	{
		TroopRoster availableRoster = BuildSelectableRoster(mainParty.MemberRoster);
		TroopRoster firstTeamRoster = TroopRoster.CreateDummyTroopRoster();
		AddPlayerToFirstTeamRoster(firstTeamRoster);
		TroopRoster emptyPrisonRoster = TroopRoster.CreateDummyTroopRoster();
		_pendingSelection = new PendingSelection
		{
			Stage = MilitaryExerciseSelectionStage.FirstTeam,
			PlayerOriginalHitPoints = GetMainHeroHitPoints(),
			PlayerOriginalWasWounded = Hero.MainHero?.IsWounded ?? false
		};
		PartyScreenHelper.OpenScreenWithDummyRoster(
			availableRoster,
			emptyPrisonRoster,
			firstTeamRoster,
			TroopRoster.CreateDummyTroopRoster(),
			new TextObject("可选成员"),
			new TextObject("第一队（玩家固定属于此队）"),
			Math.Max(availableRoster.TotalManCount, 0),
			Math.Max(mainParty.Party?.PartySizeLimit ?? availableRoster.TotalManCount, availableRoster.TotalManCount),
			new PartyPresentationDoneButtonConditionDelegate(FirstTeamDoneCondition),
			new PartyScreenClosedDelegate(OnFirstTeamScreenClosed),
			new IsTroopTransferableDelegate(MilitaryExerciseTroopTransferableDelegate));
	}

	private static Tuple<bool, TextObject> FirstTeamDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static void OnFirstTeamScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		try
		{
			if (fromCancel)
			{
				ResetPendingSelection("first_cancel");
				Display("已取消军事演习。");
				return;
			}
			if (_pendingSelection == null || _pendingSelection.Stage != MilitaryExerciseSelectionStage.FirstTeam)
			{
				ResetPendingSelection("first_stage_mismatch");
				return;
			}
			TroopRoster firstTeamRoster = BuildSelectionRosterFromUi(rightMemberRoster);
			AddPlayerToFirstTeamRoster(firstTeamRoster);
			TroopRoster remainingAfterFirstRoster = BuildSelectionRosterFromUi(leftMemberRoster);
			_pendingSelection.FirstTeamRoster = firstTeamRoster;
			_pendingSelection.RemainingAfterFirstRoster = remainingAfterFirstRoster;
			_pendingSelection.Stage = MilitaryExerciseSelectionStage.SecondTeam;
			_isOpening = false;
			QueueOpenSecondTeamSelection();
		}
		catch (Exception ex)
		{
			Log("first_team_screen failed: " + ex.GetType().Name + ": " + ex.Message);
			ResetPendingSelection("first_exception");
			DisplayFailure("选择失败", ex);
		}
	}

	private static void OpenSecondTeamSelection(TroopRoster remainingAfterFirstRoster)
	{
		TroopRoster remainingRoster = CloneRoster(remainingAfterFirstRoster);
		TroopRoster opponentRoster = TroopRoster.CreateDummyTroopRoster();
		PartyScreenHelper.OpenScreenWithDummyRoster(
			remainingRoster,
			TroopRoster.CreateDummyTroopRoster(),
			opponentRoster,
			TroopRoster.CreateDummyTroopRoster(),
			new TextObject("剩余成员"),
			new TextObject("第二队"),
			Math.Max(remainingRoster.TotalManCount, 0),
			Math.Max(remainingRoster.TotalManCount, 1),
			new PartyPresentationDoneButtonConditionDelegate(SecondTeamDoneCondition),
			new PartyScreenClosedDelegate(OnSecondTeamScreenClosed),
			new IsTroopTransferableDelegate(MilitaryExerciseTroopTransferableDelegate));
	}

	private static Tuple<bool, TextObject> SecondTeamDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		if (rightMemberRoster == null || rightMemberRoster.TotalManCount <= 0)
		{
			return new Tuple<bool, TextObject>(false, new TextObject("第二队必须至少 1 人。"));
		}
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static void OnSecondTeamScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		try
		{
			if (fromCancel)
			{
				ResetPendingSelection("second_cancel");
				Display("已取消军事演习。");
				return;
			}
			if (_pendingSelection == null || _pendingSelection.Stage != MilitaryExerciseSelectionStage.SecondTeam || _pendingSelection.FirstTeamRoster == null)
			{
				ResetPendingSelection("second_stage_mismatch");
				return;
			}
			TroopRoster opponentRoster = BuildSelectionRosterFromUi(rightMemberRoster);
			TroopRoster holdingRoster = BuildSelectionRosterFromUi(leftMemberRoster);
			if (opponentRoster.TotalManCount <= 0)
			{
				Log("second_team_screen rejected: empty opponent");
				ResetPendingSelection("empty_opponent");
				Display("第二队必须至少 1 人。");
				return;
			}
			_runtime = new MilitaryExerciseRuntime
			{
				TestSessionId = NewExerciseSessionId(),
				FirstTeamRoster = CloneRoster(_pendingSelection.FirstTeamRoster),
				OpponentRoster = opponentRoster,
				HoldingRoster = holdingRoster,
				PlayerOriginalHitPoints = _pendingSelection.PlayerOriginalHitPoints,
				PlayerOriginalWasWounded = _pendingSelection.PlayerOriginalWasWounded,
				OpponentDummyParty = null,
				HoldingDummyParty = null,
				MapEvent = null,
				IsOpening = false,
				SettlementStarted = false,
				SettlementDone = false
			};
			_runtime.FirstTeamSummary = RosterSummary(_runtime.FirstTeamRoster);
			_runtime.OpponentSummary = RosterSummary(_runtime.OpponentRoster);
			_runtime.HoldingSummary = RosterSummary(_runtime.HoldingRoster);
			Log($"runtime_created first={_runtime.FirstTeamSummary} opponent={_runtime.OpponentSummary} holding={_runtime.HoldingSummary}");
			try
			{
				PrepareDummyPartiesAndSplitRoster(_runtime);
			}
			catch (Exception splitEx)
			{
				Log("split failed: " + splitEx.GetType().Name + ": " + splitEx.Message);
				CleanupSplitRuntime(_runtime, "split_failed");
				_runtime = null;
				_pendingSelection = null;
				_isOpening = false;
				DisplayFailure("军事演习准备失败", splitEx);
				return;
			}
			_pendingSelection = null;
			_isOpening = false;
			QueueOpenBattleMission();
		}
		catch (Exception ex)
		{
			Log("second_team_screen failed: " + ex.GetType().Name + ": " + ex.Message);
			ResetPendingSelection("second_exception");
			DisplayFailure("选择失败", ex);
		}
	}

	private static void PrepareDummyPartiesAndSplitRoster(MilitaryExerciseRuntime runtime)
	{
		if (runtime == null)
		{
			throw new InvalidOperationException("Runtime is null.");
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || PartyBase.MainParty == null)
		{
			throw new InvalidOperationException("MainParty is null.");
		}
		runtime.PlayerOriginalHitPoints = GetMainHeroHitPoints();
		runtime.PlayerOriginalWasWounded = Hero.MainHero?.IsWounded ?? false;
		EnsurePlayerCanJoinBattle(runtime);
		Dictionary<CharacterObject, RosterTotals> beforeTotals = BuildRosterTotals(mainParty.MemberRoster);
		int beforeMainMen = mainParty.MemberRoster?.TotalManCount ?? 0;
		CreateMilitaryExerciseDummyParties(runtime, mainParty);
		MoveRosterFromMainParty(runtime.OpponentRoster, runtime.OpponentDummyParty, "opponent");
		MoveRosterFromMainParty(runtime.HoldingRoster, runtime.HoldingDummyParty, "holding");
		AssignExercisePartyLeader(runtime.OpponentDummyParty, "opponent", runtime);
		AssignExercisePartyLeader(runtime.HoldingDummyParty, "holding", runtime);
		ValidateSplit(runtime, beforeTotals, beforeMainMen);
		runtime.FirstTeamSummary = RosterSummary(mainParty.MemberRoster);
		runtime.OpponentSummary = RosterSummary(runtime.OpponentDummyParty?.MemberRoster);
		runtime.HoldingSummary = RosterSummary(runtime.HoldingDummyParty?.MemberRoster);
	}

	private static void CreateMilitaryExerciseDummyParties(MilitaryExerciseRuntime runtime, MobileParty mainParty)
	{
		CampaignVec2 mainPosition = mainParty.Position;
		Vec2 direction = ResolveEncounterDirection(mainParty);
		CampaignVec2 opponentPosition = mainPosition - direction * 0.4f;
		Vec2 holdingOffset = new Vec2(-direction.Y, direction.X);
		if (holdingOffset.LengthSquared <= 0.0001f)
		{
			holdingOffset = new Vec2(0f, 1f);
		}
		CampaignVec2 holdingPosition = mainPosition + holdingOffset.Normalized() * 0.4f;
		string opponentId = OpponentDummyPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		string holdingId = HoldingDummyPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		runtime.OpponentDummyParty = MobileParty.CreateParty(opponentId, new OpponentDummyPartyComponent(opponentPosition, new TextObject("AnimusForge 军事演习对抗队"), Hero.MainHero, Clan.PlayerClan));
		runtime.HoldingDummyParty = MobileParty.CreateParty(holdingId, new HoldingDummyPartyComponent(holdingPosition, new TextObject("AnimusForge 军事演习待命队"), Hero.MainHero, Clan.PlayerClan));
		if (runtime.OpponentDummyParty == null)
		{
			throw new InvalidOperationException("Failed to create opponent dummy party.");
		}
		if (runtime.HoldingDummyParty == null)
		{
			throw new InvalidOperationException("Failed to create holding dummy party.");
		}
		InitializeCreatedDummyParty(runtime.OpponentDummyParty);
		InitializeCreatedDummyParty(runtime.HoldingDummyParty);
	}

	private static void InitializeCreatedDummyParty(MobileParty party)
	{
		party.IsVisible = false;
		party.SetMoveModeHold();
	}

	private static void MoveRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		if (selectedRoster == null || targetParty == null)
		{
			return;
		}
		MoveRosterResult result = new MoveRosterResult();
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			if (character.IsPlayerCharacter)
			{
				continue;
			}
			if (character.IsHero)
			{
				MoveHeroToParty(character.HeroObject, targetParty, label, result);
				continue;
			}
			MoveRegularTroopToParty(item, targetParty, label, result);
		}
	}

	private static void AssignExercisePartyLeader(MobileParty party, string label, MilitaryExerciseRuntime runtime)
	{
		try
		{
			if (party == null || party.MemberRoster == null)
			{
				return;
			}
			Hero leader = ChooseExercisePartyLeader(party);
			if (leader == null)
			{
				return;
			}
			party.PartyComponent?.ChangePartyLeader(leader);
			party.ActualClan = leader.Clan ?? party.ActualClan;
			party.Party.SetCustomOwner(leader);
			if (runtime != null && ReferenceEquals(party, runtime.OpponentDummyParty))
			{
				runtime.OpponentLeaderHero = leader;
			}
		}
		catch (Exception ex)
		{
			Log($"combat_bonus_party_leader label={label} failed {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static Hero ChooseExercisePartyLeader(MobileParty party)
	{
		if (party?.MemberRoster == null)
		{
			return null;
		}
		Hero best = null;
		int bestScore = int.MinValue;
		foreach (TroopRosterElement item in SnapshotRoster(party.MemberRoster))
		{
			CharacterObject character = item.Character;
			Hero hero = character?.HeroObject;
			if (hero == null || !character.IsHero || hero.IsHumanPlayerCharacter || hero.IsDead)
			{
				continue;
			}
			int score = GetExerciseLeaderScore(hero);
			if (!hero.IsWounded)
			{
				score += 10000;
			}
			if (score > bestScore)
			{
				bestScore = score;
				best = hero;
			}
		}
		return best;
	}

	private static int GetExerciseLeaderScore(Hero hero)
	{
		return GetCombatLeaderScore(hero) + GetExperienceLeaderScore(hero);
	}

	private static int GetExperienceLeaderScore(Hero hero)
	{
		if (hero == null)
		{
			return 0;
		}
		int score = 0;
		try
		{
			if (HeroHasPerk(hero, DefaultPerks.Leadership.LeaderOfMasses))
			{
				score += 100000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Leadership.TrustedCommander))
			{
				score += 80000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Leadership.LeadByExample))
			{
				score += 60000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Leadership.MakeADifference))
			{
				score += 60000;
			}
			if (HeroHasPerk(hero, DefaultPerks.OneHanded.LeadByExample))
			{
				score += 50000;
			}
			if (HeroHasPerk(hero, DefaultPerks.OneHanded.Trainer))
			{
				score += 30000;
			}
			if (HeroHasPerk(hero, DefaultPerks.TwoHanded.BaptisedInBlood))
			{
				score += 30000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Bow.BullsEye))
			{
				score += 30000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Crossbow.MountedCrossbowman))
			{
				score += 30000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Throwing.Resourceful))
			{
				score += 30000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Roguery.NoRestForTheWicked))
			{
				score += 30000;
			}
			if (HeroHasPerk(hero, DefaultPerks.Leadership.InspiringLeader))
			{
				score += 20000;
			}
		}
		catch
		{
		}
		return score;
	}

	private static int GetCombatLeaderScore(Hero hero)
	{
		if (hero == null)
		{
			return 0;
		}
		try
		{
			return hero.GetSkillValue(DefaultSkills.Leadership)
				+ hero.GetSkillValue(DefaultSkills.Tactics)
				+ hero.GetSkillValue(DefaultSkills.OneHanded)
				+ hero.GetSkillValue(DefaultSkills.TwoHanded)
				+ hero.GetSkillValue(DefaultSkills.Polearm)
				+ hero.GetSkillValue(DefaultSkills.Bow)
				+ hero.GetSkillValue(DefaultSkills.Crossbow)
				+ hero.GetSkillValue(DefaultSkills.Throwing)
				+ hero.GetSkillValue(DefaultSkills.Riding)
				+ hero.GetSkillValue(DefaultSkills.Athletics);
		}
		catch
		{
			return 0;
		}
	}

	private static void MoveHeroToParty(Hero hero, MobileParty targetParty, string label, MoveRosterResult result)
	{
		if (hero == null || targetParty == null)
		{
			return;
		}
		if (hero.IsHumanPlayerCharacter)
		{
			return;
		}
		AddHeroToPartyAction.Apply(hero, targetParty, showNotification: false);
		result.Heroes++;
	}

	private static void MoveRegularTroopToParty(TroopRosterElement item, MobileParty targetParty, string label, MoveRosterResult result)
	{
		TroopRoster mainRoster = MobileParty.MainParty?.MemberRoster;
		TroopRoster targetRoster = targetParty?.MemberRoster;
		CharacterObject character = item.Character;
		if (mainRoster == null || targetRoster == null || character == null)
		{
			throw new InvalidOperationException("Invalid roster while moving regular troop.");
		}
		int sourceIndex = mainRoster.FindIndexOfTroop(character);
		if (sourceIndex < 0)
		{
			throw new InvalidOperationException("Source troop not found in MainParty: " + SafeCharacterId(character));
		}
		TroopRosterElement sourceElement = mainRoster.GetElementCopyAtIndex(sourceIndex);
		int number = Math.Max(0, item.Number);
		int wounded = 0;
		int xp = CalculateRosterXpToMove(sourceElement, number);
		if (sourceElement.Number < number)
		{
			throw new InvalidOperationException($"Not enough source troops for {SafeCharacterId(character)}. have={sourceElement.Number} need={number}");
		}
		if (sourceElement.Xp < xp)
		{
			throw new InvalidOperationException($"Not enough source XP for {SafeCharacterId(character)}. have={sourceElement.Xp} need={xp}");
		}
		mainRoster.AddToCounts(character, -number, insertAtFront: false, woundedCount: -wounded, xpChange: -xp, removeDepleted: true, index: -1);
		targetRoster.AddToCounts(character, number, insertAtFront: false, woundedCount: wounded, xpChange: xp, removeDepleted: true, index: -1);
		result.RegularMen += number;
		result.RegularWounded += wounded;
		result.RegularXp += xp;
	}

	private static int CalculateRosterXpToMove(TroopRosterElement sourceElement, int numberToMove)
	{
		try
		{
			int sourceNumber = Math.Max(0, sourceElement.Number);
			int sourceXp = Math.Max(0, sourceElement.Xp);
			numberToMove = Math.Max(0, numberToMove);
			if (sourceNumber <= 0 || sourceXp <= 0 || numberToMove <= 0)
			{
				return 0;
			}
			if (numberToMove >= sourceNumber)
			{
				return sourceXp;
			}
			double raw = (double)sourceXp * numberToMove / sourceNumber;
			int xp = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
			if (xp < 0)
			{
				return 0;
			}
			if (xp > sourceXp)
			{
				return sourceXp;
			}
			return xp;
		}
		catch
		{
			return 0;
		}
	}

	private static void EnsurePlayerCanJoinBattle(MilitaryExerciseRuntime runtime)
	{
		Hero playerHero = Hero.MainHero;
		if (playerHero == null)
		{
			return;
		}
		int woundedLimit = playerHero.WoundedHealthLimit;
		if (playerHero.HitPoints <= woundedLimit)
		{
			int newHitPoints = Math.Min(playerHero.MaxHitPoints, woundedLimit + 1);
			playerHero.HitPoints = newHitPoints;
		}
		else
		{
		}
	}

	private static void ValidateSplit(MilitaryExerciseRuntime runtime, Dictionary<CharacterObject, RosterTotals> beforeTotals, int beforeMainMen)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			throw new InvalidOperationException("MainParty missing after split.");
		}
		CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
		if (playerCharacter != null && !mainParty.MemberRoster.Contains(playerCharacter))
		{
			throw new InvalidOperationException("Player is not in MainParty after split.");
		}
		if (runtime.OpponentDummyParty == null || runtime.OpponentDummyParty.MemberRoster.TotalManCount <= 0)
		{
			throw new InvalidOperationException("Opponent dummy party has no members after split.");
		}
		int afterTotal = (mainParty.MemberRoster?.TotalManCount ?? 0)
			+ (runtime.OpponentDummyParty?.MemberRoster?.TotalManCount ?? 0)
			+ (runtime.HoldingDummyParty?.MemberRoster?.TotalManCount ?? 0);
		if (afterTotal != beforeMainMen)
		{
			throw new InvalidOperationException($"Total member count mismatch after split. before={beforeMainMen} after={afterTotal}");
		}
		Dictionary<CharacterObject, RosterTotals> afterTotals = BuildCombinedRosterTotals(mainParty.MemberRoster, runtime.OpponentDummyParty.MemberRoster, runtime.HoldingDummyParty.MemberRoster);
		foreach (KeyValuePair<CharacterObject, RosterTotals> pair in beforeTotals)
		{
			if (!afterTotals.TryGetValue(pair.Key, out RosterTotals after))
			{
				throw new InvalidOperationException("Character missing after split: " + SafeCharacterId(pair.Key));
			}
			RosterTotals before = pair.Value;
			bool totalsMatch = pair.Key?.IsHero == true
				? before.Number == after.Number && before.Wounded == after.Wounded
				: before.Number == after.Number && before.Wounded == after.Wounded && before.Xp == after.Xp;
			if (!totalsMatch)
			{
				throw new InvalidOperationException($"Roster totals mismatch for {SafeCharacterId(pair.Key)}. before={before} after={after}");
			}
		}
		foreach (KeyValuePair<CharacterObject, RosterTotals> pair in afterTotals)
		{
			if (!beforeTotals.ContainsKey(pair.Key))
			{
				throw new InvalidOperationException("Unexpected character after split: " + SafeCharacterId(pair.Key));
			}
			if (pair.Key != null && pair.Key.IsHero && pair.Value.Number != 1)
			{
				throw new InvalidOperationException("Hero duplicated after split: " + SafeCharacterId(pair.Key));
			}
		}
		Log($"split_validate_ok total={afterTotal} main={RosterSummary(mainParty.MemberRoster)} opponent={RosterSummary(runtime.OpponentDummyParty.MemberRoster)} holding={RosterSummary(runtime.HoldingDummyParty.MemberRoster)}");
	}

	private static Dictionary<CharacterObject, RosterTotals> BuildCombinedRosterTotals(params TroopRoster[] rosters)
	{
		Dictionary<CharacterObject, RosterTotals> result = new Dictionary<CharacterObject, RosterTotals>();
		if (rosters == null)
		{
			return result;
		}
		foreach (TroopRoster roster in rosters)
		{
			MergeRosterTotals(result, roster);
		}
		return result;
	}

	private static Dictionary<CharacterObject, RosterTotals> BuildRosterTotals(TroopRoster roster)
	{
		Dictionary<CharacterObject, RosterTotals> result = new Dictionary<CharacterObject, RosterTotals>();
		MergeRosterTotals(result, roster);
		return result;
	}

	private static void MergeRosterTotals(Dictionary<CharacterObject, RosterTotals> totals, TroopRoster roster)
	{
		if (totals == null || roster == null)
		{
			return;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement item = GetFreshRosterElementCopy(roster, i);
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			totals.TryGetValue(character, out RosterTotals existing);
			existing.Number += Math.Max(0, item.Number);
			existing.Wounded += Math.Max(0, item.WoundedNumber);
			existing.Xp += Math.Max(0, item.Xp);
			totals[character] = existing;
		}
	}

	private static void OpenMilitaryExerciseBattleMission(MilitaryExerciseRuntime runtime)
	{
		if (runtime == null)
		{
			throw new InvalidOperationException("Runtime is null.");
		}
		MobileParty mainParty = MobileParty.MainParty;
		MobileParty opponentParty = runtime.OpponentDummyParty;
		if (mainParty == null || PartyBase.MainParty == null || opponentParty?.Party == null)
		{
			throw new InvalidOperationException("Main or opponent party is missing.");
		}
		runtime.IsOpening = true;
		FieldBattleEventComponent component = FieldBattleEventComponent.CreateFieldBattleEvent(PartyBase.MainParty, opponentParty.Party);
		runtime.MapEvent = component?.MapEvent;
		if (runtime.MapEvent == null)
		{
			throw new InvalidOperationException("Failed to create field battle MapEvent.");
		}
		runtime.MapEvent.ResetBattleState();
		int attackerCount = runtime.MapEvent.AttackerSide.RecalculateMemberCountOfSide();
		int defenderCount = runtime.MapEvent.DefenderSide.RecalculateMemberCountOfSide();
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(PartyBase.MainParty, opponentParty.Party);
		SetPrivateField(PlayerEncounter.Current, "_mapEvent", runtime.MapEvent);
		MissionInitializerRecord rec = BuildMissionInitializerRecord(mainParty, runtime.MapEvent);
		Log($"open_battle scene={rec.SceneName}");
		IMission openedMission = CampaignMission.OpenBattleMission(rec);
		Mission mission = openedMission as Mission;
		if (mission == null)
		{
			throw new InvalidOperationException("CampaignMission.OpenBattleMission returned non-Mission.");
		}
		PlayerEncounter.StartAttackMission();
		MapEvent.PlayerMapEvent?.BeginWait();
		MilitaryExerciseMissionLogic logic = new MilitaryExerciseMissionLogic(runtime);
		mission.AddMissionBehavior(logic);
		logic.TryDisableBattleEndLogic("after_open_manual");
		runtime.IsOpening = false;
	}

	private static MissionInitializerRecord BuildMissionInitializerRecord(MobileParty mainParty, MapEvent mapEvent)
	{
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		MapPatchData patch = mapSceneWrapper.GetMapPatchAtPosition(mainParty.Position);
		string scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, false);
		if (string.IsNullOrWhiteSpace(scene))
		{
			throw new InvalidOperationException("Battle scene is empty.");
		}
		MissionInitializerRecord rec = new MissionInitializerRecord(scene);
		TerrainType terrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mainParty.CurrentNavigationFace);
		rec.TerrainType = (int)terrainType;
		rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = true;
		rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
		rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(mainParty.Position);
		rec.SceneHasMapPatch = true;
		rec.DecalAtlasGroup = 2;
		rec.PatchCoordinates = patch.normalizedCoordinates;
		rec.PatchEncounterDir = ResolvePatchEncounterDirection(mainParty, mapEvent);
		return rec;
	}

	private static Vec2 ResolvePatchEncounterDirection(MobileParty mainParty, MapEvent mapEvent)
	{
		try
		{
			if (mapEvent?.AttackerSide?.LeaderParty != null && mapEvent.DefenderSide?.LeaderParty != null)
			{
				Vec2 v = mapEvent.AttackerSide.LeaderParty.Position.ToVec2() - mapEvent.DefenderSide.LeaderParty.Position.ToVec2();
				if (v.LengthSquared > 0.0001f)
				{
					return v.Normalized();
				}
			}
		}
		catch
		{
		}
		return ResolveEncounterDirection(mainParty);
	}

	internal static void CleanupExerciseRuntime(MilitaryExerciseRuntime runtime, string reason, bool skipXpCommit)
	{
		if (runtime == null)
		{
			return;
		}
		if (runtime.SettlementDone)
		{
			return;
		}
		if (runtime.SettlementStarted)
		{
			return;
		}
		runtime.SettlementStarted = true;
		ExerciseSettlementSummary summary = new ExerciseSettlementSummary();
		Log($"cleanup_exercise begin reason={reason} skip_xp_commit={skipXpCommit}");
		try
		{
			Log("vanilla_full_settlement_blocked no_finish_battle no_renown_influence no_loot_prisoner_morale");
			Dictionary<CharacterObject, RosterTotals> mainBeforeXp = BuildRosterTotals(MobileParty.MainParty?.MemberRoster);
			Dictionary<CharacterObject, RosterTotals> opponentBeforeXp = BuildRosterTotals(runtime.OpponentDummyParty?.MemberRoster);
			Dictionary<CharacterObject, RosterTotals> holdingBeforeXp = BuildRosterTotals(runtime.HoldingDummyParty?.MemberRoster);
			if (skipXpCommit)
			{
				summary.XpCommitted = runtime.XpCommitSucceeded;
			}
			else if (runtime.XpCommittedByVanillaPatch)
			{
				summary.XpCommitted = runtime.XpCommitSucceeded;
			}
			else
			{
				summary.XpCommitted = CommitXpGainsForMapEvent(runtime.MapEvent);
			}
			int mainXpDelta = CalculateRosterXpDelta(mainBeforeXp, BuildRosterTotals(MobileParty.MainParty?.MemberRoster));
			int opponentXpDelta = CalculateRosterXpDelta(opponentBeforeXp, BuildRosterTotals(runtime.OpponentDummyParty?.MemberRoster));
			int holdingXpDelta = CalculateRosterXpDelta(holdingBeforeXp, BuildRosterTotals(runtime.HoldingDummyParty?.MemberRoster));
			Log($"xp_delta_summary main={mainXpDelta} opponent={opponentXpDelta} holding={holdingXpDelta}");
			summary.OpponentReturned = MoveAllMembersBackToMainParty(runtime.OpponentDummyParty, "exercise_opponent");
			summary.HoldingReturned = MoveAllMembersBackToMainParty(runtime.HoldingDummyParty, "exercise_holding");
			RestorePlayerHitPointsAfterExercise(runtime);
			CleanupMapEventAndPlayerEncounter(runtime.MapEvent, reason);
			DestroyDummyParty(runtime.OpponentDummyParty, OpponentDummyPartyPrefix, "exercise_opponent");
			DestroyDummyParty(runtime.HoldingDummyParty, HoldingDummyPartyPrefix, "exercise_holding");
			Display("军事演习结束。");
			Log($"cleanup_exercise end reason={reason} xp_committed={summary.XpCommitted} opponent_returned={summary.OpponentReturned} holding_returned={summary.HoldingReturned}");
		}
		catch (Exception ex)
		{
			Log("cleanup_exercise failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			DisplayFailure("军事演习结算异常", ex);
		}
		finally
		{
			runtime.OpponentDummyParty = null;
			runtime.HoldingDummyParty = null;
			runtime.MapEvent = null;
			runtime.SettlementDone = true;
			if (ReferenceEquals(_runtime, runtime))
			{
				_runtime = null;
			}
			_isOpening = false;
			_queuedOpenBattle = false;
		}
	}

	private static void RestorePlayerHitPointsAfterExercise(MilitaryExerciseRuntime runtime)
	{
		try
		{
			Hero playerHero = Hero.MainHero;
			if (playerHero == null)
			{
				return;
			}
			bool playerDown = runtime.PlayerWasDownInExercise;
			try
			{
				Agent mainAgent = Mission.Current?.MainAgent;
				playerDown = playerDown || (mainAgent != null && (mainAgent.State == AgentState.Killed || mainAgent.State == AgentState.Unconscious || mainAgent.Health <= 0f));
			}
			catch
			{
			}
			if (playerDown)
			{
				return;
			}
			int restored = Math.Max(0, Math.Min(playerHero.MaxHitPoints, runtime.PlayerOriginalHitPoints));
			playerHero.HitPoints = restored;
		}
		catch (Exception ex)
		{
			Log("player_hp_restore failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupMapEventAndPlayerEncounter(MapEvent mapEvent, string reason)
	{
		try
		{
			if (mapEvent != null)
			{
				if (!mapEvent.IsFinalized)
				{
					mapEvent.ResetBattleState();
					mapEvent.FinalizeEvent();
				}
				else
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log("cleanup_mapevent failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		try
		{
			if (PlayerEncounter.Current != null)
			{
				MapEvent currentEncounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
				if (currentEncounterMapEvent == mapEvent || mapEvent == null)
				{
					SetPrivateField<object>(PlayerEncounter.Current, "_campaignBattleResult", null);
					SetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent", null);
					ClearPlayerEncounterProperty();
				}
				else
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log("cleanup_player_encounter failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupSplitRuntime(MilitaryExerciseRuntime runtime, string reason)
	{
		if (runtime == null)
		{
			return;
		}
		MoveAllMembersBackToMainParty(runtime.OpponentDummyParty, "cleanup_opponent");
		MoveAllMembersBackToMainParty(runtime.HoldingDummyParty, "cleanup_holding");
		DestroyDummyParty(runtime.OpponentDummyParty, OpponentDummyPartyPrefix, "cleanup_opponent");
		DestroyDummyParty(runtime.HoldingDummyParty, HoldingDummyPartyPrefix, "cleanup_holding");
		runtime.OpponentDummyParty = null;
		runtime.HoldingDummyParty = null;
	}

	private static bool CommitXpGainsForMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			Log("xp_commit failed map_event=null");
			return false;
		}
		bool attackerOk = InvokeCommitXpGains(mapEvent.AttackerSide, "attacker");
		bool defenderOk = InvokeCommitXpGains(mapEvent.DefenderSide, "defender");
		bool success = attackerOk && defenderOk;
		Log($"xp_commit_result success={success} attacker={attackerOk} defender={defenderOk}");
		return success;
	}

	private static bool InvokeCommitXpGains(object mapEventSide, string label)
	{
		try
		{
			if (mapEventSide == null)
			{
				Log($"xp_commit_{label}_failed side=null");
				return false;
			}
			MethodInfo method = mapEventSide.GetType().GetMethod("CommitXpGains", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				Log($"xp_commit_{label}_failed method_missing type={mapEventSide.GetType().FullName}");
				return false;
			}
			method.Invoke(mapEventSide, null);
			return true;
		}
		catch (TargetInvocationException ex)
		{
			Exception inner = ex.InnerException ?? ex;
			Log($"xp_commit_{label}_failed {inner.GetType().Name}: {inner.Message}");
			return false;
		}
		catch (Exception ex)
		{
			Log($"xp_commit_{label}_failed {ex.GetType().Name}: {ex.Message}");
			return false;
		}
	}

	private static int CalculateRosterXpDelta(Dictionary<CharacterObject, RosterTotals> before, Dictionary<CharacterObject, RosterTotals> after)
	{
		before = before ?? new Dictionary<CharacterObject, RosterTotals>();
		after = after ?? new Dictionary<CharacterObject, RosterTotals>();
		HashSet<CharacterObject> characters = new HashSet<CharacterObject>();
		foreach (CharacterObject character in before.Keys)
		{
			if (character != null)
			{
				characters.Add(character);
			}
		}
		foreach (CharacterObject character in after.Keys)
		{
			if (character != null)
			{
				characters.Add(character);
			}
		}
		int totalDelta = 0;
		foreach (CharacterObject character in characters)
		{
			before.TryGetValue(character, out RosterTotals beforeTotals);
			after.TryGetValue(character, out RosterTotals afterTotals);
			totalDelta += afterTotals.Xp - beforeTotals.Xp;
		}
		return totalDelta;
	}

	private static MoveRosterResult MoveAllMembersBackToMainParty(MobileParty sourceParty, string label)
	{
		MobileParty mainParty = MobileParty.MainParty;
		MoveRosterResult result = new MoveRosterResult();
		if (sourceParty == null || mainParty == null || sourceParty.MemberRoster == null)
		{
			Log($"cleanup_return_skipped label={label} source_null={sourceParty == null} main_null={mainParty == null}");
			return result;
		}
		foreach (TroopRosterElement item in SnapshotRoster(sourceParty.MemberRoster))
		{
			try
			{
				CharacterObject character = item.Character;
				if (character == null || item.Number <= 0)
				{
					continue;
				}
				if (character.IsHero)
				{
					if (character.HeroObject?.IsDead == true)
					{
						result.DeadHeroesSkipped++;
					}
					else if (!character.IsPlayerCharacter)
					{
						AddHeroToPartyAction.Apply(character.HeroObject, mainParty, showNotification: false);
						result.Heroes++;
					}
					continue;
				}
				int number = Math.Max(0, item.Number);
				int wounded = Math.Max(0, item.WoundedNumber);
				int xp = Math.Max(0, item.Xp);
				sourceParty.MemberRoster.AddToCounts(character, -number, insertAtFront: false, woundedCount: -wounded, xpChange: -xp, removeDepleted: true, index: -1);
				mainParty.MemberRoster.AddToCounts(character, number, insertAtFront: false, woundedCount: wounded, xpChange: xp, removeDepleted: true, index: -1);
				result.RegularMen += number;
				result.RegularWounded += wounded;
				result.RegularXp += xp;
			}
			catch (Exception ex)
			{
				result.Errors++;
				Log($"cleanup_return_element_failed label={label} error={ex.GetType().Name}: {ex.Message}");
			}
		}
		Log($"cleanup_return_summary label={label} {result}");
		return result;
	}

	private static void DestroyDummyParty(MobileParty party, string expectedPrefix, string label)
	{
		try
		{
			if (party == null)
			{
				return;
			}
			string id = party.StringId ?? "";
			if (party.IsActive && id.StartsWith(expectedPrefix, StringComparison.Ordinal))
			{
				DestroyPartyAction.Apply(null, party);
			}
			else
			{
			}
		}
		catch (Exception ex)
		{
			Log($"dummy_destroy_failed label={label} error={ex.GetType().Name}: {ex.Message}");
		}
	}

	private static List<TroopRosterElement> SnapshotRoster(TroopRoster roster)
	{
		List<TroopRosterElement> result = new List<TroopRosterElement>();
		if (roster == null)
		{
			return result;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement item = GetFreshRosterElementCopy(roster, i);
			if (item.Character != null && item.Number > 0)
			{
				result.Add(item);
			}
		}
		return result;
	}

	private static TroopRosterElement GetFreshRosterElementCopy(TroopRoster roster, int index)
	{
		TroopRosterElement item = roster.GetElementCopyAtIndex(index);
		try
		{
			item.Xp = roster.GetElementXp(index);
		}
		catch
		{
		}
		return item;
	}

	private static Vec2 ResolveEncounterDirection(MobileParty mainParty)
	{
		try
		{
			Vec2 bearing = mainParty?.Bearing ?? Vec2.Zero;
			if (bearing.LengthSquared > 0.0001f)
			{
				return bearing.Normalized();
			}
		}
		catch
		{
		}
		return new Vec2(1f, 0f);
	}

	private static string FormatCampaignVec2(CampaignVec2 position)
	{
		return $"{position.X:0.00},{position.Y:0.00}";
	}

	private static string SafeCharacterId(CharacterObject character)
	{
		try
		{
			return character?.StringId ?? character?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeHeroId(Hero hero)
	{
		try
		{
			return hero?.StringId ?? hero?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static bool HasPartyPerk(MobileParty party, PerkObject perk, bool checkSecondaryRole)
	{
		try
		{
			return party != null && perk != null && party.HasPerk(perk, checkSecondaryRole);
		}
		catch
		{
			return false;
		}
	}

	private static bool HeroHasPerk(Hero hero, PerkObject perk)
	{
		try
		{
			return hero != null && perk != null && hero.GetPerkValue(perk);
		}
		catch
		{
			return false;
		}
	}

	private static string FormatXpLeaderPerks(Hero hero)
	{
		if (hero == null)
		{
			return "none";
		}
		List<string> perks = new List<string>();
		AddHeroPerkTag(perks, hero, DefaultPerks.Leadership.LeaderOfMasses, "Leadership.LeaderOfMasses");
		AddHeroPerkTag(perks, hero, DefaultPerks.Leadership.TrustedCommander, "Leadership.TrustedCommander");
		AddHeroPerkTag(perks, hero, DefaultPerks.Leadership.LeadByExample, "Leadership.LeadByExample");
		AddHeroPerkTag(perks, hero, DefaultPerks.Leadership.MakeADifference, "Leadership.MakeADifference");
		AddHeroPerkTag(perks, hero, DefaultPerks.Leadership.InspiringLeader, "Leadership.InspiringLeader");
		AddHeroPerkTag(perks, hero, DefaultPerks.OneHanded.LeadByExample, "OneHanded.LeadByExample");
		AddHeroPerkTag(perks, hero, DefaultPerks.OneHanded.Trainer, "OneHanded.Trainer");
		AddHeroPerkTag(perks, hero, DefaultPerks.TwoHanded.BaptisedInBlood, "TwoHanded.BaptisedInBlood");
		AddHeroPerkTag(perks, hero, DefaultPerks.Bow.BullsEye, "Bow.BullsEye");
		AddHeroPerkTag(perks, hero, DefaultPerks.Crossbow.MountedCrossbowman, "Crossbow.MountedCrossbowman");
		AddHeroPerkTag(perks, hero, DefaultPerks.Throwing.Resourceful, "Throwing.Resourceful");
		AddHeroPerkTag(perks, hero, DefaultPerks.Roguery.NoRestForTheWicked, "Roguery.NoRestForTheWicked");
		return perks.Count > 0 ? string.Join(",", perks) : "none";
	}

	private static void AddHeroPerkTag(List<string> perks, Hero hero, PerkObject perk, string tag)
	{
		if (perks != null && HeroHasPerk(hero, perk))
		{
			perks.Add(tag);
		}
	}

	private static string FormatXpPartyLeaderPerks(MobileParty party)
	{
		List<string> perks = new List<string>();
		AddPartyPerkTag(perks, party, DefaultPerks.Leadership.LeaderOfMasses, true, "Leadership.LeaderOfMasses.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Leadership.TrustedCommander, true, "Leadership.TrustedCommander.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Leadership.LeadByExample, true, "Leadership.LeadByExample.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Leadership.MakeADifference, true, "Leadership.MakeADifference.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.OneHanded.LeadByExample, false, "OneHanded.LeadByExample.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.OneHanded.Trainer, true, "OneHanded.Trainer.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.TwoHanded.BaptisedInBlood, true, "TwoHanded.BaptisedInBlood.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Bow.BullsEye, false, "Bow.BullsEye.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Crossbow.MountedCrossbowman, true, "Crossbow.MountedCrossbowman.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Throwing.Resourceful, true, "Throwing.Resourceful.PL");
		AddPartyPerkTag(perks, party, DefaultPerks.Roguery.NoRestForTheWicked, false, "Roguery.NoRestForTheWicked.PL");
		return perks.Count > 0 ? string.Join(",", perks) : "none";
	}

	private static void AddPartyPerkTag(List<string> perks, MobileParty party, PerkObject perk, bool checkSecondaryRole, string tag)
	{
		if (perks != null && HasPartyPerk(party, perk, checkSecondaryRole))
		{
			perks.Add(tag);
		}
	}

	private static bool HasAnySharedXpPartyPerk(MobileParty party, CharacterObject troop)
	{
		if (party == null || troop == null)
		{
			return false;
		}
		if (HasPartyPerk(party, DefaultPerks.Leadership.LeaderOfMasses, true))
		{
			return true;
		}
		if (troop.IsRegular && troop.IsMounted && !party.IsCurrentlyAtSea && HasPartyPerk(party, DefaultPerks.Leadership.LeadByExample, true))
		{
			return true;
		}
		if (troop.IsRegular && troop.IsRanged && HasPartyPerk(party, DefaultPerks.Leadership.MakeADifference, true))
		{
			return true;
		}
		return false;
	}

	private static bool HasAnyHitXpPartyPerk(MobileParty party, CharacterObject troop)
	{
		if (party == null || troop == null)
		{
			return false;
		}
		if (!troop.IsRanged)
		{
			if (!party.IsCurrentlyAtSea && HasPartyPerk(party, DefaultPerks.OneHanded.Trainer, true))
			{
				return true;
			}
			if (HasPartyPerk(party, DefaultPerks.TwoHanded.BaptisedInBlood, true))
			{
				return true;
			}
		}
		if (troop.HasThrowingWeapon() && HasPartyPerk(party, DefaultPerks.Throwing.Resourceful, true))
		{
			return true;
		}
		if (troop.IsInfantry && HasPartyPerk(party, DefaultPerks.OneHanded.CorpsACorps, false))
		{
			return true;
		}
		if (HasPartyPerk(party, DefaultPerks.OneHanded.LeadByExample, false))
		{
			return true;
		}
		if (troop.IsRanged)
		{
			if (HasPartyPerk(party, DefaultPerks.Crossbow.MountedCrossbowman, true))
			{
				return true;
			}
			if (HasPartyPerk(party, DefaultPerks.Bow.BullsEye, false))
			{
				return true;
			}
		}
		if (troop.Culture?.IsBandit == true && HasPartyPerk(party, DefaultPerks.Roguery.NoRestForTheWicked, false))
		{
			return true;
		}
		return false;
	}

	private static string FormatRatioDelta(int result, int baseline)
	{
		if (baseline <= 0)
		{
			return "n/a";
		}
		double value = (double)(result - baseline) / baseline;
		return value.ToString("0.###");
	}

	private static string PartyRuntimeSummary(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			string id = party.StringId ?? "null";
			bool active = party.IsActive;
			bool visible = party.IsVisible;
			bool hasMapEvent = party.MapEvent != null;
			return $"id={id},active={active},visible={visible},has_map_event={hasMapEvent},roster={RosterSummary(party.MemberRoster)}";
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name + ":" + ex.Message;
		}
	}

	private static string MapEventRuntimeSummary(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return "null";
		}
		try
		{
			bool isPlayerMapEvent = object.ReferenceEquals(MapEvent.PlayerMapEvent, mapEvent);
			return $"state={mapEvent.State},battle_state={mapEvent.BattleState},finalized={mapEvent.IsFinalized},has_winner={mapEvent.HasWinner},is_player_mapevent={isPlayerMapEvent}";
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name + ":" + ex.Message;
		}
	}

	private static string SafePartyId(PartyBase party)
	{
		try
		{
			return party?.MobileParty?.StringId ?? party?.Settlement?.StringId ?? party?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static bool MilitaryExerciseTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character != null
			&& !character.IsPlayerCharacter
			&& !character.IsNotTransferableInPartyScreen
			&& type != PartyScreenLogic.TroopType.Prisoner;
	}

	private static TroopRoster BuildSelectableRoster(TroopRoster sourceRoster)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return result;
		}
		foreach (TroopRosterElement item in sourceRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			int healthyNumber = Math.Max(0, item.Number - item.WoundedNumber);
			if (healthyNumber <= 0)
			{
				continue;
			}
			int healthyXp = CalculateRosterXpToMove(item, healthyNumber);
			result.AddToCounts(character, healthyNumber, insertAtFront: false, woundedCount: 0, xpChange: healthyXp, removeDepleted: true, index: -1);
		}
		return result;
	}

	private static TroopRoster BuildSelectionRosterFromUi(TroopRoster sourceRoster)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return result;
		}
		foreach (TroopRosterElement item in sourceRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			int healthyNumber = Math.Max(0, item.Number - item.WoundedNumber);
			if (healthyNumber <= 0)
			{
				continue;
			}
			result.AddToCounts(character, healthyNumber, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
		}
		return result;
	}

	private static TroopRoster CloneRoster(TroopRoster sourceRoster)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return result;
		}
		foreach (TroopRosterElement item in sourceRoster.GetTroopRoster())
		{
			if (item.Character != null && item.Number > 0)
			{
				result.Add(item);
			}
		}
		return result;
	}

	private static void AddPlayerToFirstTeamRoster(TroopRoster firstTeamRoster)
	{
		if (firstTeamRoster == null)
		{
			return;
		}
		CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
		if (playerCharacter == null || firstTeamRoster.Contains(playerCharacter))
		{
			return;
		}
		TroopRoster mainRoster = MobileParty.MainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
		if (mainRoster != null)
		{
			foreach (TroopRosterElement item in mainRoster.GetTroopRoster())
			{
				if (item.Character == playerCharacter && item.Number > 0)
				{
					firstTeamRoster.Add(item);
					return;
				}
			}
		}
		firstTeamRoster.AddToCounts(playerCharacter, 1, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
	}

	private static string RosterSummary(TroopRoster roster)
	{
		if (roster == null)
		{
			return "null";
		}
		int elements = 0;
		int heroes = 0;
		int wounded = 0;
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			if (item.Character == null || item.Number <= 0)
			{
				continue;
			}
			elements++;
			if (item.Character.IsHero)
			{
				heroes += item.Number;
			}
			wounded += Math.Max(0, item.WoundedNumber);
		}
		return $"men={roster.TotalManCount}, elements={elements}, heroes={heroes}, wounded={wounded}";
	}

	private static int GetMainHeroHitPoints()
	{
		try
		{
			return Hero.MainHero?.HitPoints ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	private static void ResetPendingSelection(string reason)
	{
		_pendingSelection = null;
		_isOpening = false;
		_queuedOpenSecondTeam = false;
		_queuedOpenBattle = false;
	}

	private static void QueueOpenSecondTeamSelection()
	{
		_queuedOpenSecondTeam = true;
		_queuedOpenSecondTeamAt = (float)Environment.TickCount / 1000f + 0.2f;
	}

	private static void QueueOpenBattleMission()
	{
		_queuedOpenBattle = true;
		_queuedOpenBattleAt = (float)Environment.TickCount / 1000f + 0.35f;
	}

	private static bool IsPartyScreenStillActive()
	{
		try
		{
			string activeStateName = Game.Current?.GameStateManager?.ActiveState?.GetType().Name ?? "";
			return activeStateName.IndexOf("PartyState", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return true;
		}
	}

	private static string NewExerciseSessionId()
	{
		try
		{
			return "me-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + MBRandom.RandomInt(1000000).ToString("000000");
		}
		catch
		{
			return "me-" + DateTime.UtcNow.Ticks;
		}
	}

	private static void SetPrivateField<T>(object target, string fieldName, T value)
	{
		try
		{
			target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
		}
		catch
		{
		}
	}

	private static T GetPrivateField<T>(object target, string fieldName)
	{
		try
		{
			FieldInfo field = target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				return (T)field.GetValue(target);
			}
		}
		catch
		{
		}
		return default;
	}

	private static void ClearPlayerEncounterProperty()
	{
		try
		{
			if (Campaign.Current != null)
			{
				typeof(Campaign).GetProperty("PlayerEncounter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(Campaign.Current, null);
			}
		}
		catch
		{
		}
	}

	private static void SetPlayerEncounterState(PlayerEncounter encounter, string stateName)
	{
		if (encounter == null || string.IsNullOrWhiteSpace(stateName))
		{
			return;
		}
		try
		{
			Type stateType = Type.GetType("TaleWorlds.CampaignSystem.Encounters.PlayerEncounterState, TaleWorlds.CampaignSystem");
			if (stateType == null)
			{
				return;
			}
			object value = Enum.Parse(stateType, stateName);
			PropertyInfo property = encounter.GetType().GetProperty("EncounterState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.CanWrite)
			{
				property.SetValue(encounter, value);
				return;
			}
			FieldInfo field = encounter.GetType().GetField("_encounterState", BindingFlags.Instance | BindingFlags.NonPublic)
				?? encounter.GetType().GetField("EncounterState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(encounter, value);
				return;
			}
		}
		catch (Exception ex)
		{
			Log($"set_player_encounter_state failed {ex.GetType().Name}: {ex.Message}");
		}
	}

	internal static bool HasMissionBehavior(Mission mission, string typeName)
	{
		if (mission == null)
		{
			return false;
		}
		try
		{
			Type handlerType = Type.GetType("TaleWorlds.MountAndBlade." + typeName + ", TaleWorlds.MountAndBlade");
			if (handlerType == null)
			{
				return false;
			}
			MethodInfo getMethod = typeof(Mission).GetMethod("GetMissionBehavior", Type.EmptyTypes);
			if (getMethod == null)
			{
				return false;
			}
			MethodInfo generic = getMethod.MakeGenericMethod(handlerType);
			return generic.Invoke(mission, null) != null;
		}
		catch
		{
			return false;
		}
	}

	public sealed class MilitaryExerciseSaveableTypeDefiner : SaveableTypeDefiner
	{
		public MilitaryExerciseSaveableTypeDefiner()
			: base(711050)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(OpponentDummyPartyComponent), 1);
			AddClassDefinition(typeof(HoldingDummyPartyComponent), 2);
		}
	}

	private static void Display(string message)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
		}
		catch
		{
		}
	}

	private static void DisplayFailure(string title, Exception ex)
	{
		Display(title + "：" + GetPlayerVisibleFailureReason(ex));
	}

	private static string GetPlayerVisibleFailureReason(Exception ex)
	{
		string message = ex?.Message ?? "";
		if (string.IsNullOrWhiteSpace(message))
		{
			return "原因未知。";
		}
		if (message.IndexOf("Roster totals mismatch", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "队伍选择数据不一致，请重新选择队伍。";
		}
		if (message.IndexOf("Not enough source wounded troops", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "队伍选择数据不一致，请重新选择队伍。";
		}
		if (message.IndexOf("Opponent dummy party has no members", StringComparison.OrdinalIgnoreCase) >= 0
			|| message.IndexOf("empty opponent", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "第二队没有可参演成员。";
		}
		if (message.IndexOf("MainParty is null", StringComparison.OrdinalIgnoreCase) >= 0
			|| message.IndexOf("MainParty missing", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "玩家队伍状态不可用。请回到大地图后重试。";
		}
		if (message.IndexOf("Player is not in MainParty", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "玩家不在主队中，无法开始军事演习。";
		}
		if (message.IndexOf("Failed to create field battle MapEvent", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "无法进入演习战场，请移动到其他位置后重试。";
		}
		if (message.IndexOf("Battle scene is empty", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "无法进入演习战场，请移动到其他位置后重试。";
		}
		if (message.IndexOf("CampaignMission.OpenBattleMission returned non-Mission", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "无法进入演习战场，请移动到其他位置后重试。";
		}
		if (message.Length > 90)
		{
			message = message.Substring(0, 90) + "...";
		}
		return message;
	}

	private static void Log(string message)
	{
		try
		{
			string session = _runtime?.TestSessionId ?? "";
			if (!string.IsNullOrWhiteSpace(session))
			{
				string line = "[MilitaryExercise] session=" + session + " " + message;
				WriteDiagnosticLine(line);
			}
			else
			{
				string line = "[MilitaryExercise] " + message;
				WriteDiagnosticLine(line);
			}
		}
		catch
			{
			}
	}

	internal static void WriteDiagnosticLine(string line)
	{
		try
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (string.IsNullOrWhiteSpace(documents))
			{
				return;
			}
			string dir = Path.Combine(documents, "Mount and Blade II Bannerlord", "Configs", "ModLogs");
			Directory.CreateDirectory(dir);
			string path = Path.Combine(dir, "animusforge_military_exercise.log");
			File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff ") + line + Environment.NewLine);
		}
		catch
		{
		}
	}

	internal sealed class MilitaryExerciseRuntime
	{
		public string TestSessionId { get; set; }

		public TroopRoster FirstTeamRoster { get; set; }

		public TroopRoster OpponentRoster { get; set; }

		public TroopRoster HoldingRoster { get; set; }

		public string FirstTeamSummary { get; set; }

		public string OpponentSummary { get; set; }

		public string HoldingSummary { get; set; }

		public int PlayerOriginalHitPoints { get; set; }

		public bool PlayerOriginalWasWounded { get; set; }

		public bool PlayerWasDownInExercise { get; set; }

		public MobileParty OpponentDummyParty { get; set; }

		public Hero OpponentLeaderHero { get; set; }

		public MobileParty HoldingDummyParty { get; set; }

		public MapEvent MapEvent { get; set; }

		public bool IsOpening { get; set; }

		public bool SettlementStarted { get; set; }

		public bool SettlementDone { get; set; }

		public bool VanillaResultPatchHit { get; set; }

		public bool XpCommittedByVanillaPatch { get; set; }

		public bool XpCommitSucceeded { get; set; }

		public bool EarlyXpCommittedOnMissionEnd { get; set; }

		public bool RenownInfluenceSkipped { get; set; }
	}

	private sealed class PendingSelection
	{
		public MilitaryExerciseSelectionStage Stage { get; set; }

		public TroopRoster FirstTeamRoster { get; set; }

		public TroopRoster RemainingAfterFirstRoster { get; set; }

		public int PlayerOriginalHitPoints { get; set; }

		public bool PlayerOriginalWasWounded { get; set; }
	}

	private sealed class MoveRosterResult
	{
		public int RegularMen;

		public int RegularWounded;

		public int RegularXp;

		public int Heroes;

		public int DeadHeroesSkipped;

		public int Errors;

		public int TotalMen => RegularMen + Heroes;

		public override string ToString()
		{
			return $"regular={RegularMen},heroes={Heroes},total={TotalMen},wounded={RegularWounded},xp={RegularXp},dead_heroes_skipped={DeadHeroesSkipped},errors={Errors}";
		}
	}

	private sealed class ExerciseSettlementSummary
	{
		public bool XpCommitted;

		public MoveRosterResult OpponentReturned = new MoveRosterResult();

		public MoveRosterResult HoldingReturned = new MoveRosterResult();
	}

	private struct RosterTotals
	{
		public int Number;

		public int Wounded;

		public int Xp;

		public override string ToString()
		{
			return $"number={Number},wounded={Wounded},xp={Xp}";
		}
	}

	private abstract class MilitaryExerciseDummyPartyComponent : PartyComponent
	{
		private readonly CampaignVec2 _position;

		private readonly TextObject _name;

		private Hero _owner;

		private Clan _clan;

		private Hero _leader;

		protected MilitaryExerciseDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
		{
			_position = position;
			_name = name;
			_owner = owner;
			_clan = clan;
		}

		public override Hero PartyOwner => _owner;

		public override Hero Leader => _leader;

		public override TextObject Name => _name;

		public override Settlement HomeSettlement => null;

		public override bool AvoidHostileActions => true;

		public override Banner GetDefaultComponentBanner()
		{
			return _clan?.Banner;
		}

		protected override void OnInitialize()
		{
			MobileParty.ActualClan = _clan;
			MobileParty.InitializeMobilePartyAroundPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), _position, 0f, 0f, !_position.IsOnLand);
			MobileParty.SetMoveModeHold();
		}

		protected override void OnChangePartyLeader(Hero newLeader)
		{
			_leader = newLeader;
			if (newLeader != null)
			{
				_owner = newLeader;
				_clan = newLeader.Clan ?? _clan;
				if (MobileParty != null)
				{
					MobileParty.ActualClan = _clan;
				}
			}
		}
	}

	private sealed class OpponentDummyPartyComponent : MilitaryExerciseDummyPartyComponent
	{
		public OpponentDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
			: base(position, name, owner, clan)
		{
		}
	}

	private sealed class HoldingDummyPartyComponent : MilitaryExerciseDummyPartyComponent
	{
		public HoldingDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
			: base(position, name, owner, clan)
		{
		}
	}

	private enum MilitaryExerciseSelectionStage
	{
		None,
		FirstTeam,
		SecondTeam
	}
}
