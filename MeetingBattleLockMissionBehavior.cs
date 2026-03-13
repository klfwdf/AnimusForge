using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace AnimusForge;

public class MeetingBattleLockMissionBehavior : MissionBehavior
{
	private const float StartupLoadingBlackTimeSeconds = 4f;

	private const float StartupLoadingFadeOutSeconds = 0.08f;

	private const float StartupLoadingFadeInSeconds = 0.22f;

	private const float StartupLoadingFadeRetryTimeoutSeconds = 6f;

	private readonly Hero _targetHero;

	private Agent _mainAgent;

	private Agent _targetAgent;

	private bool _leadersPlaced;

	private bool _combatResumed;

	private float _findAgentsTimer;

	private float _pauseTickTimer;

	private float _keepLeaderPoseTimer;

	private bool _escortsPlaced;

	private float _escortPlacementTimer;

	private float _escortDebugLogCooldown;

	private float _leaderSheathTimer;

	private Team _targetOriginalTeam;

	private float _targetNeutralRefreshTimer;

	private bool _meetingCombatUnlockApplied;

	private bool _targetControllerSuppressed;

	private bool _targetMountControllerSuppressed;

	private bool _encounterHostilityApplied;

	private IFaction _playerMapFactionAtEncounterStart;

	private IFaction _targetMapFactionAtEncounterStart;

	private bool _sameMapFactionAtEncounterStart;

	private bool _sameFactionAttackWarningShown;

	private Vec3 _targetLockedForward;

	private bool _hasTargetLockedForward;

	private Vec3 _targetLockedPosition;

	private bool _hasTargetLockedPosition;

	private bool _formalDuelCombatReleaseApplied;

	private Formation _targetOriginalFormation;

	private bool _hasCapturedTargetOriginalFormation;

	private Formation _formalDuelTargetFormation;

	private float _formalDuelOrderRefreshTimer;

	private bool _wasFormalDuelActiveLastTick;

	private bool _deploymentSkipApplied;

	private float _deploymentSkipEarliestTime;

	private bool _allowTargetFreeMovementAfterFormalDuel;

	private bool _startupLoadingFadeApplied;

	private bool _startupLoadingFadeAborted;

	private float _startupLoadingFadeElapsed;

	private Agent _meetingTargetEscortAgent;

	private Agent _meetingPlayerEscortAgent;

	private readonly HashSet<int> _meetingFormationManagedAgentIndices = new HashSet<int>();

	private readonly Dictionary<int, Vec3> _meetingLockPositions = new Dictionary<int, Vec3>();

	private readonly Dictionary<int, Vec2> _meetingLockDirections = new Dictionary<int, Vec2>();

	private readonly Dictionary<int, Formation> _meetingDetachedFormations = new Dictionary<int, Formation>();

	private readonly HashSet<int> _meetingMountedHardLockRiderIndices = new HashSet<int>();

	private readonly HashSet<int> _meetingMountedHardLockMountIndices = new HashSet<int>();

	private readonly Dictionary<int, Vec3> _meetingMountedHardLockPositions = new Dictionary<int, Vec3>();

	private readonly Dictionary<int, Vec3> _meetingMountedHardLockForwards = new Dictionary<int, Vec3>();

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public MeetingBattleLockMissionBehavior(Hero targetHero)
	{
		_targetHero = targetHero;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		LordEncounterBehavior.SetEncounterMeetingMissionActive(active: true);
		_findAgentsTimer = 0f;
		_pauseTickTimer = 0f;
		_keepLeaderPoseTimer = 0f;
		_leadersPlaced = false;
		_combatResumed = false;
		_escortsPlaced = false;
		_escortPlacementTimer = 0.3f;
		_escortDebugLogCooldown = 0f;
		_leaderSheathTimer = 0f;
		_targetOriginalTeam = null;
		_targetNeutralRefreshTimer = 0f;
		_meetingCombatUnlockApplied = false;
		_targetControllerSuppressed = false;
		_targetMountControllerSuppressed = false;
		_encounterHostilityApplied = false;
		_playerMapFactionAtEncounterStart = null;
		_targetMapFactionAtEncounterStart = null;
		_sameMapFactionAtEncounterStart = false;
		_sameFactionAttackWarningShown = false;
		_targetLockedForward = new Vec3(1f);
		_hasTargetLockedForward = false;
		_targetLockedPosition = Vec3.Zero;
		_hasTargetLockedPosition = false;
		_formalDuelCombatReleaseApplied = false;
		_targetOriginalFormation = null;
		_hasCapturedTargetOriginalFormation = false;
		_formalDuelTargetFormation = null;
		_formalDuelOrderRefreshTimer = 0f;
		_wasFormalDuelActiveLastTick = false;
		_deploymentSkipApplied = false;
		_deploymentSkipEarliestTime = -1f;
		_allowTargetFreeMovementAfterFormalDuel = false;
		_startupLoadingFadeApplied = false;
		_startupLoadingFadeAborted = false;
		_startupLoadingFadeElapsed = 0f;
		_meetingTargetEscortAgent = null;
		_meetingPlayerEscortAgent = null;
		_meetingFormationManagedAgentIndices.Clear();
		ClearMeetingLockAnchors();
		ClearMeetingDetachedFormations();
		ClearMeetingMountedHardLocks();
		try
		{
			_playerMapFactionAtEncounterStart = Hero.MainHero?.MapFaction;
		}
		catch
		{
			_playerMapFactionAtEncounterStart = null;
		}
		try
		{
			_targetMapFactionAtEncounterStart = _targetHero?.MapFaction;
		}
		catch
		{
			_targetMapFactionAtEncounterStart = null;
		}
		_sameMapFactionAtEncounterStart = _playerMapFactionAtEncounterStart != null && _targetMapFactionAtEncounterStart != null && _playerMapFactionAtEncounterStart == _targetMapFactionAtEncounterStart;
		TryApplyStartupLoadingFade(0f);
	}

	public override void OnRemoveBehavior()
	{
		bool flag = false;
		try
		{
			flag = base.Mission == null || base.Mission.MissionEnded;
		}
		catch
		{
			flag = true;
		}
		if (!flag)
		{
			try
			{
				EnsureMainAgentFreeMovement();
			}
			catch
			{
			}
			try
			{
				RestoreTargetLordControllerForCombat();
			}
			catch
			{
			}
			try
			{
				RestoreTargetFormationAfterFormalDuel();
			}
			catch
			{
			}
			try
			{
				RestoreAllDetachedFormations();
			}
			catch
			{
			}
		}
		LordEncounterBehavior.SetEncounterMeetingMissionActive(active: false);
		ClearMeetingLockAnchors();
		ClearMeetingDetachedFormations();
		ClearMeetingMountedHardLocks();
		base.OnRemoveBehavior();
	}

	protected override void OnEndMission()
	{
		try
		{
			LordEncounterBehavior.SetEncounterMeetingMissionActive(active: false);
		}
		catch
		{
		}
		ClearMeetingDetachedFormations();
		ClearMeetingMountedHardLocks();
		base.OnEndMission();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (base.Mission == null)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = base.Mission.MissionEnded;
		}
		catch
		{
			flag = false;
		}
		if (flag)
		{
			try
			{
				LordEncounterBehavior.SetEncounterMeetingMissionActive(active: false);
				return;
			}
			catch
			{
				return;
			}
		}
		TryApplyStartupLoadingFade(dt);
		TrySkipDeploymentPhaseForMeeting();
		bool flag2 = false;
		try
		{
			flag2 = DuelBehavior.IsFormalDuelActive;
		}
		catch
		{
			flag2 = false;
		}
		if (_wasFormalDuelActiveLastTick && !flag2)
		{
			try
			{
				ResetLeaderLockAnchorAfterFormalDuel();
			}
			catch
			{
			}
			_allowTargetFreeMovementAfterFormalDuel = true;
			EnsureTargetLordReleasedAfterFormalDuel();
			Logger.Log("MeetingBattle", "Formal duel ended: target duelist movement released from meeting lock.");
		}
		_wasFormalDuelActiveLastTick = flag2;
		if (flag2)
		{
			_allowTargetFreeMovementAfterFormalDuel = false;
			if (!_combatResumed)
			{
				RestoreTargetLordControllerForCombat();
				_combatResumed = true;
				Logger.Log("MeetingBattle", "Formal duel active: released target controller only; keep non-duel agents locked.");
			}
			KeepFormalDuelIsolation();
			return;
		}
		if (MeetingBattleRuntime.IsCombatEscalated)
		{
			RestoreTargetFormationAfterFormalDuel();
			if (!_meetingCombatUnlockApplied)
			{
				EnsureMissionBattleModeForCombat();
				EnsureMissionCombatTeamRelationships();
				RestoreTargetLordControllerForCombat();
				ReleaseMeetingLocksForCombat();
				ForceAgentsIntoCombatReadiness();
				_meetingCombatUnlockApplied = true;
			}
			LordEncounterBehavior.SetEncounterMeetingMissionActive(active: false);
			EnsureMainAgentFreeMovement();
			TryApplyEncounterHostilityForEscalatedCombat();
			if (!_combatResumed)
			{
				ResumeAllAIAgents();
				_combatResumed = true;
			}
			return;
		}
		RestoreTargetFormationAfterFormalDuel();
		_combatResumed = false;
		_findAgentsTimer -= dt;
		_pauseTickTimer -= dt;
		_keepLeaderPoseTimer -= dt;
		_escortPlacementTimer -= dt;
		_escortDebugLogCooldown -= dt;
		_leaderSheathTimer -= dt;
		_targetNeutralRefreshTimer -= dt;
		if (_findAgentsTimer <= 0f)
		{
			FindMainAndTargetAgents();
			_findAgentsTimer = 0.2f;
		}
		if (_targetNeutralRefreshTimer <= 0f)
		{
			if (_allowTargetFreeMovementAfterFormalDuel)
			{
				EnsureTargetLordReleasedAfterFormalDuel();
			}
			else
			{
				EnsureTargetLordNeutralized();
			}
			_targetNeutralRefreshTimer = 0.03f;
		}
		if (!_leadersPlaced && _mainAgent != null && _targetAgent != null)
		{
			PlaceLeadersForMeeting();
			_leadersPlaced = true;
			_keepLeaderPoseTimer = 2f;
		}
		if (_leadersPlaced && !_allowTargetFreeMovementAfterFormalDuel)
		{
			KeepLeadersFacingEachOther();
		}
		if (_leadersPlaced && !_escortsPlaced && _escortPlacementTimer <= 0f)
		{
			if (TryPlaceEscortGuards())
			{
				_escortsPlaced = true;
			}
			else
			{
				_escortPlacementTimer = 0.5f;
			}
		}
		if (_leaderSheathTimer <= 0f)
		{
			EnsureTargetLordSheathed();
			_leaderSheathTimer = 0.06f;
		}
		EnsureMainAgentFreeMovement();
		if (_pauseTickTimer <= 0f)
		{
			PauseAllAIAgentsAndSheathWeapons(sheathWeapons: false);
			_pauseTickTimer = 0.15f;
		}
	}

	private void EnsureMissionBattleModeForCombat()
	{
		if (base.Mission == null)
		{
			return;
		}
		try
		{
			MissionMode mode = base.Mission.Mode;
			if (mode == MissionMode.Battle)
			{
				return;
			}
			base.Mission.SetMissionMode(MissionMode.Battle, atStart: false);
			Logger.Log("MeetingBattle", $"Forced mission mode to Battle for combat escalation. PreviousMode={mode}");
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingBattle", "Failed to force mission mode to Battle during combat escalation: " + ex.Message);
		}
	}

	private void EnsureMissionCombatTeamRelationships()
	{
		if (base.Mission == null)
		{
			return;
		}
		Team team = null;
		Team team2 = null;
		Team team3 = null;
		try
		{
			team = _mainAgent?.Team ?? base.Mission.PlayerTeam;
		}
		catch
		{
			team = null;
		}
		try
		{
			team2 = _targetOriginalTeam ?? _targetAgent?.Team;
		}
		catch
		{
			team2 = null;
		}
		try
		{
			team3 = base.Mission.PlayerEnemyTeam;
		}
		catch
		{
			team3 = null;
		}
		if ((team2 == null || team2 == team) && team3 != null && team3 != team)
		{
			team2 = team3;
			try
			{
				if (_targetAgent != null && _targetAgent.IsActive() && _targetAgent.Team != team2)
				{
					_targetAgent.SetTeam(team2, sync: true);
				}
			}
			catch
			{
			}
			try
			{
				Agent mountAgent = _targetAgent?.MountAgent;
				if (mountAgent != null && mountAgent.IsActive() && mountAgent.Team != team2)
				{
					mountAgent.SetTeam(team2, sync: true);
				}
			}
			catch
			{
			}
		}
		if (team == null || team2 == null || team == team2)
		{
			Logger.Log("MeetingBattle", "Combat team relationship fix skipped: unable to resolve distinct player/target teams.");
			return;
		}
		string text = GetTeamSideKey(team);
		string text2 = GetTeamSideKey(team2);
		List<Team> list = new List<Team>();
		List<Team> list2 = new List<Team>();
		AddUniqueTeam(list, team);
		AddUniqueTeam(list2, team2);
		try
		{
			foreach (Team item in base.Mission.Teams)
			{
				if (item == null)
				{
					continue;
				}
				string teamSideKey = GetTeamSideKey(item);
				if (!string.IsNullOrEmpty(text) && string.Equals(teamSideKey, text, StringComparison.OrdinalIgnoreCase))
				{
					AddUniqueTeam(list, item);
				}
				else if (!string.IsNullOrEmpty(text2) && string.Equals(teamSideKey, text2, StringComparison.OrdinalIgnoreCase))
				{
					AddUniqueTeam(list2, item);
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			AddUniqueTeam(list, team);
		}
		if (list2.Count == 0)
		{
			AddUniqueTeam(list2, team2);
		}
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				TrySetEnemyRelation(list[i], list[j], isEnemy: false);
			}
		}
		for (int k = 0; k < list2.Count; k++)
		{
			for (int l = k + 1; l < list2.Count; l++)
			{
				TrySetEnemyRelation(list2[k], list2[l], isEnemy: false);
			}
		}
		foreach (Team item2 in list)
		{
			foreach (Team item3 in list2)
			{
				TrySetEnemyRelation(item2, item3, isEnemy: true);
			}
		}
		bool flag = false;
		try
		{
			flag = team.IsEnemyOf(team2) || team2.IsEnemyOf(team);
		}
		catch
		{
			flag = false;
		}
		Logger.Log("MeetingBattle", $"Combat team relationship fix applied. PlayerSideKey={text ?? "unknown"}, TargetSideKey={text2 ?? "unknown"}, PlayerSideTeams={list.Count}, TargetSideTeams={list2.Count}, PlayerAgents={CountActiveAgentsOnTeams(list)}, TargetAgents={CountActiveAgentsOnTeams(list2)}, DirectEnemy={flag}");
	}

	private static void AddUniqueTeam(List<Team> teams, Team team)
	{
		if (teams == null || team == null || teams.Contains(team))
		{
			return;
		}
		teams.Add(team);
	}

	private void TrySetEnemyRelation(Team a, Team b, bool isEnemy)
	{
		if (a == null || b == null || a == b)
		{
			return;
		}
		try
		{
			a.SetIsEnemyOf(b, isEnemyOf: isEnemy);
		}
		catch
		{
		}
		try
		{
			b.SetIsEnemyOf(a, isEnemyOf: isEnemy);
		}
		catch
		{
		}
	}

	private string GetTeamSideKey(Team team)
	{
		if (team == null)
		{
			return null;
		}
		try
		{
			PropertyInfo propertyInfo = team.GetType().GetProperty("Side") ?? team.GetType().GetProperty("BattleSide") ?? team.GetType().GetProperty("MissionSide");
			if (propertyInfo != null)
			{
				object value = propertyInfo.GetValue(team, null);
				if (value != null)
				{
					return value.ToString();
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private int CountActiveAgentsOnTeams(List<Team> teams)
	{
		if (teams == null || teams.Count == 0 || base.Mission == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive())
				{
					continue;
				}
				Team team = null;
				try
				{
					team = agent.Team;
				}
				catch
				{
					team = null;
				}
				if (team != null && teams.Contains(team))
				{
					num++;
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private void ForceAgentsIntoCombatReadiness()
	{
		if (base.Mission == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		Team team = null;
		try
		{
			team = _mainAgent?.Team ?? base.Mission.PlayerTeam;
		}
		catch
		{
			team = null;
		}
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive())
				{
					continue;
				}
				try
				{
					if (!agent.IsMainAgent)
					{
						AgentFlag agentFlags = agent.GetAgentFlags();
						agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
					}
				}
				catch
				{
				}
				try
				{
					agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
					num++;
				}
				catch
				{
				}
				try
				{
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
				catch
				{
				}
				try
				{
					agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
					num2++;
				}
				catch
				{
				}
				try
				{
					agent.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Team item in base.Mission.Teams)
			{
				if (item == null)
				{
					continue;
				}
				try
				{
					foreach (Formation item2 in item.FormationsIncludingEmpty)
					{
						if (item2 != null)
						{
							try
							{
								item2.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
							}
							catch
							{
							}
						}
					}
				}
				catch
				{
				}
				if (team != null && item != team)
				{
					try
					{
						item.MasterOrderController?.SelectAllFormations();
					}
					catch
					{
					}
					try
					{
						item.MasterOrderController?.SetOrder(OrderType.Charge);
						num3++;
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
		Logger.Log("MeetingBattle", $"Combat readiness refresh applied. AlarmedAgents={num}, WieldRefreshed={num2}, EnemyChargeTeams={num3}");
	}

	private void TryApplyStartupLoadingFade(float dt)
	{
		if (_startupLoadingFadeApplied || _startupLoadingFadeAborted || base.Mission == null)
		{
			return;
		}
		_startupLoadingFadeElapsed += dt;
		MissionCameraFadeView missionCameraFadeView = null;
		try
		{
			missionCameraFadeView = base.Mission.GetMissionBehavior<MissionCameraFadeView>();
		}
		catch
		{
			missionCameraFadeView = null;
		}
		if (missionCameraFadeView == null)
		{
			if (_startupLoadingFadeElapsed >= 6f)
			{
				_startupLoadingFadeAborted = true;
				Logger.Log("MeetingBattle", "Startup loading delay skipped: MissionCameraFadeView not available.");
			}
			return;
		}
		try
		{
			if (missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
			{
				missionCameraFadeView.BeginFadeOutAndIn(0.08f, 4f, 0.22f);
				_startupLoadingFadeApplied = true;
				Logger.Log("MeetingBattle", $"Applied startup loading delay. BlackTime={4f:0.0}s");
				return;
			}
		}
		catch (Exception ex)
		{
			_startupLoadingFadeAborted = true;
			Logger.Log("MeetingBattle", "Startup loading delay failed: " + ex.Message);
			return;
		}
		if (_startupLoadingFadeElapsed >= 6f)
		{
			_startupLoadingFadeAborted = true;
			Logger.Log("MeetingBattle", "Startup loading delay skipped: camera fade state never reached White.");
		}
	}

	private void TrySkipDeploymentPhaseForMeeting()
	{
		if (_deploymentSkipApplied || base.Mission == null)
		{
			return;
		}
		float num = 0f;
		try
		{
			num = base.Mission.CurrentTime;
		}
		catch
		{
			num = 0f;
		}
		if (_deploymentSkipEarliestTime < 0f)
		{
			_deploymentSkipEarliestTime = num + 0.6f;
		}
		if (num < _deploymentSkipEarliestTime)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = base.Mission.Mode == MissionMode.Deployment;
			if (!flag)
			{
				string text = base.Mission.Mode.ToString();
				flag = !string.IsNullOrEmpty(text) && text.IndexOf("Deploy", StringComparison.OrdinalIgnoreCase) >= 0;
			}
		}
		catch
		{
			flag = false;
		}
		if (!flag)
		{
			return;
		}
		FindMainAndTargetAgents();
		if (_mainAgent == null || !_mainAgent.IsActive() || _targetAgent == null || !_targetAgent.IsActive())
		{
			return;
		}
		try
		{
			DeploymentHandler missionBehavior = base.Mission.GetMissionBehavior<DeploymentHandler>();
			if (missionBehavior != null)
			{
				missionBehavior.FinishDeployment();
				_deploymentSkipApplied = true;
				Logger.Log("MeetingBattle", $"Meeting mission deployment auto-ready triggered via DeploymentHandler.FinishDeployment(). t={num:0.00}s");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingBattle", "Meeting mission deployment auto-ready failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void KeepFormalDuelIsolation()
	{
		if (base.Mission == null)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = DuelBehavior.IsFormalDuelPreFightActive;
		}
		catch
		{
			flag = false;
		}
		Agent agent = _mainAgent;
		if (agent == null || !agent.IsActive())
		{
			try
			{
				agent = base.Mission.MainAgent;
			}
			catch
			{
				agent = null;
			}
			if (agent == null || !agent.IsActive())
			{
				try
				{
					agent = Agent.Main;
				}
				catch
				{
					agent = null;
				}
			}
		}
		if (agent != null && agent.IsActive())
		{
			_mainAgent = agent;
		}
		Agent targetAgent = _targetAgent;
		if (targetAgent == null || !targetAgent.IsActive())
		{
			FindMainAndTargetAgents();
			targetAgent = _targetAgent;
		}
		Agent agent2 = null;
		Agent agent3 = null;
		try
		{
			agent2 = agent?.MountAgent;
		}
		catch
		{
			agent2 = null;
		}
		try
		{
			agent3 = targetAgent?.MountAgent;
		}
		catch
		{
			agent3 = null;
		}
		try
		{
			if (agent != null && agent.IsActive())
			{
				if (agent.IsAIControlled)
				{
					agent.SetIsAIPaused(isPaused: false);
				}
				if (flag)
				{
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
				}
			}
		}
		catch
		{
		}
		try
		{
			if (agent2 != null && agent2.IsActive())
			{
				agent2.SetIsAIPaused(isPaused: false);
				if (flag)
				{
					agent2.DisableScriptedMovement();
					agent2.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					agent2.DisableScriptedMovement();
					agent2.ClearTargetFrame();
				}
			}
		}
		catch
		{
		}
		try
		{
			if (targetAgent != null && targetAgent.IsActive())
			{
				TrySetAgentController(targetAgent, flag ? "None" : "AI");
				targetAgent.SetIsAIPaused(flag);
				if (flag)
				{
					targetAgent.DisableScriptedMovement();
					targetAgent.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					targetAgent.DisableScriptedMovement();
					targetAgent.ClearTargetFrame();
					targetAgent.SetWatchState(Agent.WatchState.Alarmed);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (agent3 != null && agent3.IsActive())
			{
				TrySetAgentController(agent3, flag ? "None" : "AI");
				agent3.SetIsAIPaused(flag);
				if (flag)
				{
					agent3.DisableScriptedMovement();
					agent3.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					agent3.DisableScriptedMovement();
					agent3.ClearTargetFrame();
				}
			}
		}
		catch
		{
		}
		if (!flag)
		{
			if (agent != null && agent.IsActive() && targetAgent != null && targetAgent.IsActive())
			{
				_formalDuelCombatReleaseApplied = true;
			}
			try
			{
				KeepFormalDuelOpponentsEngaged(agent, targetAgent);
			}
			catch
			{
			}
		}
		else
		{
			_formalDuelCombatReleaseApplied = false;
		}
		try
		{
			foreach (Agent agent4 in base.Mission.Agents)
			{
				if (agent4 == null || !agent4.IsActive())
				{
					continue;
				}
				bool flag2 = false;
				try
				{
					flag2 = agent4.IsMainAgent;
				}
				catch
				{
					flag2 = false;
				}
				bool flag3 = agent4 == agent || agent4 == agent2 || agent4 == targetAgent || agent4 == agent3 || flag2;
				if (!flag3)
				{
					try
					{
						Agent riderAgent = agent4.RiderAgent;
						if (riderAgent != null && (riderAgent == agent || riderAgent == targetAgent || riderAgent.IsMainAgent))
						{
							flag3 = true;
						}
					}
					catch
					{
					}
				}
				if (flag3)
				{
					continue;
				}
				try
				{
					if (agent4.IsAIControlled)
					{
						agent4.SetIsAIPaused(isPaused: true);
						agent4.DisableScriptedMovement();
						agent4.ClearTargetFrame();
					}
				}
				catch
				{
				}
				try
				{
					Agent mountAgent = agent4.MountAgent;
					if (mountAgent == null || !mountAgent.IsActive())
					{
						continue;
					}
					bool flag4 = mountAgent == agent || mountAgent == agent2 || mountAgent == targetAgent || mountAgent == agent3;
					if (!flag4)
					{
						try
						{
							Agent riderAgent2 = mountAgent.RiderAgent;
							if (riderAgent2 != null && (riderAgent2 == agent || riderAgent2 == targetAgent || riderAgent2.IsMainAgent))
							{
								flag4 = true;
							}
						}
						catch
						{
						}
					}
					if (!flag4)
					{
						mountAgent.SetIsAIPaused(isPaused: true);
						mountAgent.DisableScriptedMovement();
						mountAgent.ClearTargetFrame();
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private void KeepFormalDuelOpponentsEngaged(Agent main, Agent target)
	{
		if (base.Mission == null || main == null || target == null || !main.IsActive() || !target.IsActive())
		{
			return;
		}
		if (!_hasCapturedTargetOriginalFormation)
		{
			try
			{
				_targetOriginalFormation = target.Formation;
			}
			catch
			{
				_targetOriginalFormation = null;
			}
			_hasCapturedTargetOriginalFormation = true;
		}
		try
		{
			target.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch
		{
		}
		if (_formalDuelTargetFormation == null)
		{
			Team team = null;
			try
			{
				team = target.Team;
			}
			catch
			{
				team = null;
			}
			if (team != null)
			{
				try
				{
					foreach (Formation item in team.FormationsIncludingEmpty)
					{
						if (item != null)
						{
							if (target.Formation == item && item.CountOfUnits <= 1)
							{
								_formalDuelTargetFormation = item;
								break;
							}
							if (item.CountOfUnits == 0)
							{
								_formalDuelTargetFormation = item;
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
		}
		if (_formalDuelTargetFormation != null)
		{
			try
			{
				if (target.Formation != _formalDuelTargetFormation)
				{
					target.Formation = _formalDuelTargetFormation;
				}
			}
			catch
			{
			}
		}
		float num = 0f;
		try
		{
			num = base.Mission.CurrentTime;
		}
		catch
		{
		}
		if (!(num >= _formalDuelOrderRefreshTimer))
		{
			return;
		}
		_formalDuelOrderRefreshTimer = num + 0.5f;
		try
		{
			(_formalDuelTargetFormation ?? target.Formation)?.SetMovementOrder(MovementOrder.MovementOrderCharge);
		}
		catch
		{
		}
		try
		{
			Team team2 = target.Team;
			if (team2 != null && team2.MasterOrderController != null)
			{
				team2.MasterOrderController.SelectAllFormations();
				team2.MasterOrderController.SetOrder(OrderType.Charge);
			}
		}
		catch
		{
		}
	}

	private void RestoreTargetFormationAfterFormalDuel()
	{
		if (!_hasCapturedTargetOriginalFormation)
		{
			return;
		}
		try
		{
			if (_targetAgent != null && _targetAgent.IsActive())
			{
				try
				{
					_targetAgent.Formation = _targetOriginalFormation;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		_targetOriginalFormation = null;
		_hasCapturedTargetOriginalFormation = false;
		_formalDuelTargetFormation = null;
		_formalDuelOrderRefreshTimer = 0f;
	}

	private void ResetLeaderLockAnchorAfterFormalDuel()
	{
		FindMainAndTargetAgents();
		_hasTargetLockedPosition = false;
		_hasTargetLockedForward = false;
		try
		{
			if (_targetAgent != null && _targetAgent.IsActive())
			{
				_targetLockedPosition = _targetAgent.Position;
				_hasTargetLockedPosition = true;
				_targetLockedForward = _targetAgent.LookDirection;
				_hasTargetLockedForward = true;
			}
		}
		catch
		{
		}
	}

	private void FindMainAndTargetAgents()
	{
		try
		{
			_mainAgent = base.Mission?.MainAgent ?? _mainAgent;
		}
		catch
		{
		}
		if (_targetAgent != null && _targetAgent.IsActive())
		{
			return;
		}
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || ((agent.Character is CharacterObject characterObject) ? characterObject.HeroObject : null) != _targetHero)
				{
					continue;
				}
				_targetAgent = agent;
				break;
			}
		}
		catch
		{
		}
	}

	private void PlaceLeadersForMeeting()
	{
		try
		{
			if (!TryBuildMeetingFramesFromBattleLines(out var targetFrame, out var playerFrame))
			{
				targetFrame = LordEncounterBehavior.BuildTargetHeroSpawnFrame();
				playerFrame = LordEncounterBehavior.BuildPlayerSpawnFrame();
			}
			ApplyFrame(_targetAgent, targetFrame);
			ApplyFrame(_mainAgent, playerFrame);
			_targetLockedForward = targetFrame.rotation.f;
			_targetLockedForward.z = 0f;
			if (_targetLockedForward.LengthSquared > 0.0001f)
			{
				_targetLockedForward.Normalize();
				_hasTargetLockedForward = true;
			}
			else
			{
				_hasTargetLockedForward = false;
			}
			_targetLockedPosition = targetFrame.origin;
			_hasTargetLockedPosition = true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingBattle", "PlaceLeadersForMeeting failed: " + ex.Message);
		}
	}

	private bool TryBuildMeetingFramesFromBattleLines(out MatrixFrame targetFrame, out MatrixFrame playerFrame)
	{
		targetFrame = MatrixFrame.Identity;
		playerFrame = MatrixFrame.Identity;
		if (base.Mission == null || _mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return false;
		}
		Team team = null;
		Team team2 = null;
		try
		{
			team = _mainAgent.Team;
		}
		catch
		{
		}
		try
		{
			team2 = _targetAgent.Team;
		}
		catch
		{
		}
		if (team == null || team2 == null || team == team2)
		{
			return false;
		}
		if (!TryGetTeamHumanCenter(team, out var center) || !TryGetTeamHumanCenter(team2, out var center2))
		{
			return false;
		}
		Vec3 vec = center - center2;
		vec.z = 0f;
		if (vec.LengthSquared < 0.0001f)
		{
			return false;
		}
		vec.Normalize();
		Vec3 vec2 = (center + center2) * 0.5f;
		float num = 6.2f;
		Vec3 candidate = vec2 - vec * num;
		Vec3 candidate2 = vec2 + vec * num;
		LordEncounterBehavior.ClampPointInsideMissionBoundary(ref candidate, vec2);
		LordEncounterBehavior.ClampPointInsideMissionBoundary(ref candidate2, vec2);
		LordEncounterBehavior.ResolveSceneGroundHeight(base.Mission.Scene, ref candidate);
		LordEncounterBehavior.ResolveSceneGroundHeight(base.Mission.Scene, ref candidate2);
		targetFrame.origin = candidate;
		targetFrame.rotation.f = vec;
		targetFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		playerFrame.origin = candidate2;
		playerFrame.rotation.f = -vec;
		playerFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return true;
	}

	private bool TryGetTeamHumanCenter(Team team, out Vec3 center)
	{
		center = Vec3.Zero;
		if (team == null || base.Mission == null)
		{
			return false;
		}
		Vec3 zero = Vec3.Zero;
		int num = 0;
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent != null && agent.IsActive() && agent.IsHuman)
				{
					Team team2 = null;
					try
					{
						team2 = agent.Team;
					}
					catch
					{
					}
					if (team2 != null && team2 == team)
					{
						zero += agent.Position;
						num++;
					}
				}
			}
		}
		catch
		{
		}
		if (num <= 0)
		{
			return false;
		}
		center = zero * (1f / (float)num);
		center.z = 0f;
		return true;
	}

	private void ConfigureMeetingHoldFormations(Agent playerEscort, Agent targetEscort)
	{
		if (base.Mission == null)
		{
			return;
		}
		if (playerEscort != null && playerEscort.IsActive())
		{
			_meetingPlayerEscortAgent = playerEscort;
		}
		if (targetEscort != null && targetEscort.IsActive())
		{
			_meetingTargetEscortAgent = targetEscort;
		}
		RefreshMeetingFormationManagedAgents();
	}

	private void RefreshMeetingFormationManagedAgents()
	{
		_meetingFormationManagedAgentIndices.Clear();
		RegisterMeetingFormationManagedAgent(_meetingTargetEscortAgent);
		RegisterMeetingFormationManagedAgent(_meetingPlayerEscortAgent);
	}

	private void RegisterMeetingFormationManagedAgent(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			_meetingFormationManagedAgentIndices.Add(agent.Index);
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				_meetingFormationManagedAgentIndices.Add(mountAgent.Index);
			}
		}
		catch
		{
		}
	}

	private bool IsMeetingFormationManagedAgent(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			return _meetingFormationManagedAgentIndices.Contains(agent.Index);
		}
		catch
		{
			return false;
		}
	}

	private void ClearMeetingMountedHardLocks()
	{
		_meetingMountedHardLockRiderIndices.Clear();
		_meetingMountedHardLockMountIndices.Clear();
		_meetingMountedHardLockPositions.Clear();
		_meetingMountedHardLockForwards.Clear();
	}

	private void ForgetMountedHardLock(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		int index = -1;
		try
		{
			index = agent.Index;
		}
		catch
		{
			return;
		}
		try
		{
			_meetingMountedHardLockRiderIndices.Remove(index);
			_meetingMountedHardLockMountIndices.Remove(index);
			_meetingMountedHardLockPositions.Remove(index);
			_meetingMountedHardLockForwards.Remove(index);
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null)
			{
				_meetingMountedHardLockMountIndices.Remove(mountAgent.Index);
			}
		}
		catch
		{
		}
		try
		{
			Agent riderAgent = agent.RiderAgent;
			if (riderAgent != null)
			{
				_meetingMountedHardLockRiderIndices.Remove(riderAgent.Index);
				_meetingMountedHardLockPositions.Remove(riderAgent.Index);
				_meetingMountedHardLockForwards.Remove(riderAgent.Index);
			}
		}
		catch
		{
		}
	}

	private void EnsureMountedHardLocks()
	{
		if (_meetingMountedHardLockRiderIndices.Count > 0 || base.Mission == null || _mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return;
		}
		Team team = null;
		Team team2 = null;
		try
		{
			team = _mainAgent.Team;
		}
		catch
		{
			team = null;
		}
		try
		{
			team2 = _targetAgent.Team;
		}
		catch
		{
			team2 = null;
		}
		if (team == null || team2 == null || team == team2)
		{
			return;
		}
		RegisterMountedHardLocksForTeam(team);
		RegisterMountedHardLocksForTeam(team2);
	}

	private void RegisterMountedHardLocksForTeam(Team team)
	{
		if (team == null || base.Mission == null)
		{
			return;
		}
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (!IsMountedHardLockCandidate(agent, team))
				{
					continue;
				}
				RegisterMountedHardLock(agent);
			}
		}
		catch
		{
		}
	}

	private bool IsMountedHardLockCandidate(Agent agent, Team team)
	{
		if (agent == null || !agent.IsActive() || !agent.IsHuman || !agent.IsAIControlled || team == null)
		{
			return false;
		}
		try
		{
			if (agent.Team != team)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		if (IsMeetingFormationManagedAgent(agent) || agent == _mainAgent || agent == _targetAgent)
		{
			return false;
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			return mountAgent != null && mountAgent.IsActive();
		}
		catch
		{
			return false;
		}
	}

	private void RegisterMountedHardLock(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		Vec3 vec = new Vec3(1f);
		try
		{
			Vec3 lookDirection = agent.LookDirection;
			lookDirection.z = 0f;
			if (lookDirection.LengthSquared > 0.0001f)
			{
				lookDirection.Normalize();
				vec = lookDirection;
			}
		}
		catch
		{
		}
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec3(1f);
		}
		vec.z = 0f;
		vec.Normalize();
		try
		{
			_meetingMountedHardLockRiderIndices.Add(agent.Index);
			_meetingMountedHardLockPositions[agent.Index] = agent.Position;
			_meetingMountedHardLockForwards[agent.Index] = vec;
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				_meetingMountedHardLockMountIndices.Add(mountAgent.Index);
			}
		}
		catch
		{
		}
	}

	private bool IsMountedHardLockMount(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			return _meetingMountedHardLockMountIndices.Contains(agent.Index);
		}
		catch
		{
			return false;
		}
	}

	private bool TryGetMountedHardLock(Agent agent, out Vec3 forward, out Vec3 anchor)
	{
		forward = Vec3.Zero;
		anchor = Vec3.Zero;
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		int index;
		try
		{
			index = agent.Index;
		}
		catch
		{
			return false;
		}
		if (!_meetingMountedHardLockRiderIndices.Contains(index))
		{
			return false;
		}
		if (!_meetingMountedHardLockPositions.TryGetValue(index, out anchor))
		{
			return false;
		}
		if (!_meetingMountedHardLockForwards.TryGetValue(index, out forward))
		{
			return false;
		}
		return true;
	}

	private void ApplyMountedHardLock(Agent agent, Vec3 forward, Vec3 anchor, bool sheathWeapons)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		DetachAgentFromFormationForMeetingLock(agent);
		try
		{
			TrySetAgentController(agent, "None");
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				TrySetAgentController(mountAgent, "None");
			}
		}
		catch
		{
		}
		if (sheathWeapons && agent.IsHuman)
		{
			TrySheathWeapons(agent);
		}
		LockAgentAndMountInPlace(agent, forward, anchor);
	}

	private void KeepLeadersFacingEachOther()
	{
		if (_mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return;
		}
		try
		{
			Vec3 vec = _mainAgent.Position - _targetAgent.Position;
			vec.z = 0f;
			if (!(vec.LengthSquared > 0.0001f))
			{
				return;
			}
			Vec3 vec2 = vec;
			if (_hasTargetLockedForward && _targetLockedForward.LengthSquared > 0.0001f)
			{
				vec2 = _targetLockedForward;
			}
			else
			{
				vec2.Normalize();
			}
			_targetAgent.LookDirection = vec2;
			try
			{
				Agent mountAgent = _targetAgent.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					mountAgent.LookDirection = vec2;
				}
			}
			catch
			{
			}
			if (!_hasTargetLockedPosition)
			{
				_targetLockedPosition = _targetAgent.Position;
				_hasTargetLockedPosition = true;
			}
			LockAgentAndMountInPlace(_targetAgent, vec2, _hasTargetLockedPosition ? new Vec3?(_targetLockedPosition) : ((Vec3?)null));
			TrySheathWeapons(_targetAgent);
		}
		catch
		{
		}
	}

	private void LockAgentAndMountInPlace(Agent agent, Vec3 forward, Vec3? anchor)
	{
		if (base.Mission == null || agent == null || !agent.IsActive())
		{
			return;
		}
		Vec3 lookDirection = forward;
		lookDirection.z = 0f;
		if (lookDirection.LengthSquared < 0.0001f)
		{
			lookDirection = new Vec3(1f);
		}
		lookDirection.Normalize();
		Vec2 vec = lookDirection.AsVec2;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(1f, 0f);
		}
		vec = vec.Normalized();
		Vec3 vec2 = anchor ?? agent.Position;
		try
		{
			if (base.Mission.Scene != null)
			{
				float height = vec2.z;
				if (base.Mission.Scene.GetHeightAtPoint(vec2.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
				{
					vec2.z = height;
				}
				else
				{
					vec2.z = base.Mission.Scene.GetGroundHeightAtPosition(vec2);
				}
			}
		}
		catch
		{
		}
		try
		{
			agent.LookDirection = lookDirection;
		}
		catch
		{
		}
		try
		{
			bool flag = false;
			try
			{
				flag = agent.IsMainAgent;
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				agent.SetIsAIPaused(isPaused: true);
				agent.ClearTargetFrame();
				try
				{
					if ((agent.Position - vec2).LengthSquared > 0.04f)
					{
						agent.TeleportToPosition(vec2);
					}
				}
				catch
				{
				}
				WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, vec2);
				agent.SetScriptedPositionAndDirection(ref scriptedPosition, vec.RotationInRadians, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
			}
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent == null || !mountAgent.IsActive())
			{
				return;
			}
			mountAgent.LookDirection = lookDirection;
			mountAgent.SetIsAIPaused(isPaused: true);
			mountAgent.ClearTargetFrame();
			mountAgent.SetMovementDirection(in vec);
			try
			{
				if ((mountAgent.Position - vec2).LengthSquared > 0.04f)
				{
					mountAgent.TeleportToPosition(vec2);
				}
			}
			catch
			{
			}
			WorldPosition scriptedPosition2 = new WorldPosition(base.Mission.Scene, vec2);
			mountAgent.SetScriptedPositionAndDirection(ref scriptedPosition2, vec.RotationInRadians, addHumanLikeDelay: false);
		}
		catch
		{
		}
	}

	private void EnsureTargetLordSheathed()
	{
		if (_targetAgent == null || !_targetAgent.IsActive() || !_targetAgent.IsHuman)
		{
			return;
		}
		try
		{
			TrySheathWeapons(_targetAgent);
		}
		catch
		{
		}
	}

	private void EnsureTargetLordNeutralized()
	{
		if (MeetingBattleRuntime.IsCombatEscalated || _targetAgent == null || !_targetAgent.IsActive())
		{
			return;
		}
		Team team = null;
		try
		{
			team = _targetAgent.Team;
		}
		catch
		{
			team = null;
		}
		if (team != null)
		{
			if (_targetOriginalTeam == null)
			{
				_targetOriginalTeam = team;
				Logger.Log("MeetingBattle", "Captured target lord original team snapshot.");
			}
			EnsureTargetLordControllerSuppressed();
			ForceLockTargetLordInPlace();
			TrySheathWeapons(_targetAgent);
		}
	}

	private void EnsureTargetLordReleasedAfterFormalDuel()
	{
		if (_targetAgent == null || !_targetAgent.IsActive())
		{
			return;
		}
		RestoreTargetLordControllerForCombat();
		TrySetAgentController(_targetAgent, "AI");
		EnsureAgentFreeMovement(_targetAgent);
		try
		{
			Agent mountAgent = _targetAgent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				TrySetAgentController(mountAgent, "AI");
				EnsureAgentFreeMovement(mountAgent);
			}
		}
		catch
		{
		}
	}

	private void ForceLockTargetLordInPlace()
	{
		if (_targetAgent == null || !_targetAgent.IsActive())
		{
			return;
		}
		Vec3 forward = new Vec3(1f);
		try
		{
			forward = _targetAgent.LookDirection;
		}
		catch
		{
		}
		try
		{
			if (_hasTargetLockedForward && _targetLockedForward.LengthSquared > 0.0001f)
			{
				forward = _targetLockedForward;
			}
		}
		catch
		{
		}
		Vec3? anchor = (_hasTargetLockedPosition ? new Vec3?(_targetLockedPosition) : ((Vec3?)null));
		if (!anchor.HasValue)
		{
			try
			{
				anchor = _targetAgent.Position;
				_targetLockedPosition = anchor.Value;
				_hasTargetLockedPosition = true;
			}
			catch
			{
			}
		}
		LockAgentAndMountInPlace(_targetAgent, forward, anchor);
	}

	private void EnsureTargetLordControllerSuppressed()
	{
		if (_targetAgent == null || !_targetAgent.IsActive())
		{
			return;
		}
		TrySetAgentController(_targetAgent, "None");
		_targetControllerSuppressed = true;
		try
		{
			Agent mountAgent = _targetAgent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				TrySetAgentController(mountAgent, "None");
				_targetMountControllerSuppressed = true;
			}
		}
		catch
		{
		}
	}

	private void RestoreTargetLordControllerForCombat()
	{
		if (!_targetControllerSuppressed)
		{
			return;
		}
		try
		{
			if (_targetAgent != null && _targetAgent.IsActive())
			{
				TrySetAgentController(_targetAgent, "AI");
			}
			if (_targetMountControllerSuppressed)
			{
				Agent agent = _targetAgent?.MountAgent;
				if (agent != null && agent.IsActive())
				{
					TrySetAgentController(agent, "AI");
				}
			}
		}
		catch
		{
		}
		_targetControllerSuppressed = false;
		_targetMountControllerSuppressed = false;
	}

	private void TrySetAgentController(Agent agent, string controllerType)
	{
		try
		{
			if (agent == null || string.IsNullOrWhiteSpace(controllerType))
			{
				return;
			}
			PropertyInfo propertyInfo = agent.GetType().GetProperty("Controller") ?? agent.GetType().GetProperty("ControllerType");
			if (propertyInfo == null || !propertyInfo.CanWrite)
			{
				return;
			}
			Type propertyType = propertyInfo.PropertyType;
			object obj = null;
			try
			{
				obj = Enum.Parse(propertyType, controllerType, ignoreCase: true);
			}
			catch
			{
			}
			if (obj == null)
			{
				string[] names = Enum.GetNames(propertyType);
				foreach (string text in names)
				{
					if (text.Equals(controllerType, StringComparison.OrdinalIgnoreCase))
					{
						obj = Enum.Parse(propertyType, text, ignoreCase: true);
						break;
					}
					if (controllerType.Equals("AI", StringComparison.OrdinalIgnoreCase) && text.IndexOf("AI", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						obj = Enum.Parse(propertyType, text, ignoreCase: true);
						break;
					}
					if (controllerType.Equals("None", StringComparison.OrdinalIgnoreCase) && text.IndexOf("None", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						obj = Enum.Parse(propertyType, text, ignoreCase: true);
						break;
					}
				}
			}
			if (obj != null)
			{
				propertyInfo.SetValue(agent, obj);
			}
		}
		catch
		{
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
		if (MeetingBattleRuntime.IsCombatEscalated || damagedHp <= 0f || affectorAgent == null || affectedAgent == null)
		{
			return;
		}
		Agent agent = _mainAgent;
		if (agent == null || !agent.IsActive())
		{
			try
			{
				agent = base.Mission?.MainAgent;
			}
			catch
			{
				agent = null;
			}
		}
		bool flag = agent != null && agent.IsActive() && affectorAgent == agent;
		if (!flag)
		{
			try
			{
				flag = agent != null && agent.IsActive() && agent.MountAgent != null && affectorAgent == agent.MountAgent;
			}
			catch
			{
				flag = false;
			}
		}
		bool flag2 = false;
		if (!flag)
		{
			try
			{
				Team team = agent?.Team;
				Team team2 = affectorAgent?.Team;
				Team team3 = affectedAgent?.Team;
				if (team != null && team2 != null && team3 != null && team2 != team3)
				{
					bool flag3 = team2.IsEnemyOf(team3) || team3.IsEnemyOf(team2);
					bool flag4 = team2 == team || team3 == team;
					flag2 = flag3 && flag4;
				}
			}
			catch
			{
				flag2 = false;
			}
		}
		if (!flag && !flag2)
		{
			return;
		}
		bool flag5 = _targetAgent != null && affectedAgent == _targetAgent;
		bool flag6 = false;
		try
		{
			flag6 = _targetAgent != null && _targetAgent.MountAgent != null && affectedAgent == _targetAgent.MountAgent;
		}
		catch
		{
			flag6 = false;
		}
		bool flag7 = false;
		try
		{
			Team team4 = agent.Team;
			Team team5 = affectedAgent.Team;
			if (team4 != null && team5 != null && team4 != team5)
			{
				flag7 = team5.IsEnemyOf(team4) || team4.IsEnemyOf(team5);
			}
		}
		catch
		{
			flag7 = false;
		}
		if (!flag7)
		{
			try
			{
				if (_targetOriginalTeam != null)
				{
					flag7 = affectedAgent.Team == _targetOriginalTeam;
				}
			}
			catch
			{
				flag7 = false;
			}
		}
		bool flag8 = flag5 || flag6 || flag7;
		if (!flag8 && flag2)
		{
			flag8 = true;
		}
		if (!flag8)
		{
			try
			{
				flag8 = affectedAgent != agent && (agent?.MountAgent == null || affectedAgent != agent.MountAgent);
			}
			catch
			{
				flag8 = false;
			}
		}
		if (flag8 && !DuelBehavior.IsFormalDuelActive)
		{
			if (flag)
			{
				TryNotifySameFactionAttackWarning(affectedAgent);
			}
			string reason = (flag ? "player_dealt_damage" : "combat_damage_detected");
			MeetingBattleRuntime.RequestCombatEscalation(reason);
			MeetingBattleRuntime.UnlockDiplomaticSideEffects(reason);
		}
	}

	private void TryNotifySameFactionAttackWarning(Agent affectedAgent)
	{
		if (_sameFactionAttackWarningShown || !_sameMapFactionAtEncounterStart || affectedAgent == null || !affectedAgent.IsActive())
		{
			return;
		}
		try
		{
			Agent agent = _mainAgent ?? base.Mission?.MainAgent;
			if (agent != null && (affectedAgent == agent || (agent.MountAgent != null && affectedAgent == agent.MountAgent)))
			{
				return;
			}
		}
		catch
		{
		}
		TextObject message = new TextObject("背叛是不可饶恕的");
		try
		{
			MBInformationManager.AddQuickInformation(message);
		}
		catch
		{
		}
		_sameFactionAttackWarningShown = true;
	}

	private void TryApplyEncounterHostilityForEscalatedCombat()
	{
		if (_encounterHostilityApplied || !MeetingBattleRuntime.IsCombatEscalated)
		{
			return;
		}
		PartyBase partyBase = null;
		try
		{
			partyBase = (PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle ?? MapEvent.PlayerMapEvent)?.GetLeaderParty(PartyBase.MainParty.OpponentSide);
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase == null)
		{
			try
			{
				partyBase = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				partyBase = null;
			}
		}
		if (partyBase == null)
		{
			try
			{
				partyBase = _targetHero?.PartyBelongedTo?.Party;
			}
			catch
			{
				partyBase = null;
			}
		}
		if (partyBase == null)
		{
			return;
		}
		try
		{
			LordEncounterBehavior.TryApplyImmediateEscalationConsequences(partyBase, _targetHero, "meeting_combat_escalated_runtime");
		}
		finally
		{
			_encounterHostilityApplied = true;
		}
	}

	private void PauseAllAIAgentsAndSheathWeapons(bool sheathWeapons)
	{
		try
		{
			Agent agent = null;
			Agent agent2 = null;
			Agent agent3 = null;
			Agent agent4 = null;
			try
			{
				agent = _mainAgent;
				if (agent == null || !agent.IsActive())
				{
					agent = base.Mission?.MainAgent;
				}
				if (agent != null && agent.IsActive())
				{
					agent2 = agent.MountAgent;
				}
				agent3 = _targetAgent;
				if (agent3 != null && agent3.IsActive())
				{
					agent4 = agent3.MountAgent;
				}
			}
			catch
			{
			}
			EnsureMountedHardLocks();
			foreach (Agent agent5 in base.Mission.Agents)
			{
				if (agent5 == null || !agent5.IsActive())
				{
					continue;
				}
				bool flag = agent5 == agent || agent5 == agent2;
				if (!flag)
				{
					try
					{
						flag = agent5.IsMainAgent;
					}
					catch
					{
						flag = false;
					}
				}
				if (!flag)
				{
					try
					{
						Agent riderAgent = agent5.RiderAgent;
						flag = riderAgent != null && (riderAgent == agent || riderAgent.IsMainAgent);
					}
					catch
					{
					}
				}
				if (flag)
				{
					EnsureAgentFreeMovement(agent5);
					continue;
				}
				bool flag2 = false;
				if (_allowTargetFreeMovementAfterFormalDuel)
				{
					flag2 = agent5 == agent3 || agent5 == agent4;
					if (!flag2)
					{
						try
						{
							Agent riderAgent2 = agent5.RiderAgent;
							flag2 = riderAgent2 != null && riderAgent2 == agent3;
						}
						catch
						{
						}
					}
				}
				if (flag2)
				{
					EnsureAgentFreeMovement(agent5);
					continue;
				}
				if (IsMountedHardLockMount(agent5))
				{
					continue;
				}
				if (TryGetMountedHardLock(agent5, out var forward, out var anchor))
				{
					ApplyMountedHardLock(agent5, forward, anchor, sheathWeapons);
					continue;
				}
				DetachAgentFromFormationForMeetingLock(agent5);
				try
				{
					if (agent5.IsAIControlled)
					{
						agent5.SetIsAIPaused(isPaused: true);
						agent5.ClearTargetFrame();
						Agent mountAgent = null;
						try
						{
							mountAgent = agent5.MountAgent;
						}
						catch
						{
							mountAgent = null;
						}
						if (mountAgent != null && mountAgent.IsActive())
						{
							try
							{
								agent5.DisableScriptedMovement();
							}
							catch
							{
							}
							ForgetMeetingLockAnchor(agent5);
						}
						else
						{
							TryLockAgentToCurrentPosition(agent5);
						}
					}
				}
				catch
				{
				}
				if (sheathWeapons && agent5.IsHuman)
				{
					TrySheathWeapons(agent5);
				}
				try
				{
					Agent mountAgent = agent5.MountAgent;
					if (mountAgent == null || !mountAgent.IsActive())
					{
						continue;
					}
					bool flag4 = mountAgent == agent2;
					if (!flag4)
					{
						try
						{
							Agent riderAgent3 = mountAgent.RiderAgent;
							flag4 = riderAgent3 != null && (riderAgent3 == agent || riderAgent3.IsMainAgent);
						}
						catch
						{
						}
					}
					if (flag4)
					{
						EnsureAgentFreeMovement(mountAgent);
						continue;
					}
					if (_allowTargetFreeMovementAfterFormalDuel && (mountAgent == agent4 || mountAgent == agent3))
					{
						EnsureAgentFreeMovement(mountAgent);
						continue;
					}
					mountAgent.SetIsAIPaused(isPaused: true);
					mountAgent.ClearTargetFrame();
					try
					{
						Vec3 position = mountAgent.Position;
						mountAgent.SetTargetPosition(position.AsVec2);
					}
					catch
					{
					}
					TryLockAgentToCurrentPosition(mountAgent);
				}
				catch
				{
				}
			}
			FreezeAllFormationsForMeeting();
		}
		catch
		{
		}
	}

	private void EnsureMainAgentFreeMovement()
	{
		Agent agent = null;
		try
		{
			agent = _mainAgent;
		}
		catch
		{
			agent = null;
		}
		if (agent == null || !agent.IsActive())
		{
			try
			{
				agent = base.Mission?.MainAgent;
			}
			catch
			{
				agent = null;
			}
		}
		if (agent == null || !agent.IsActive())
		{
			try
			{
				agent = Agent.Main;
			}
			catch
			{
				agent = null;
			}
		}
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_mainAgent = agent;
		TryEnsureMainAgentPlayerController(agent);
		EnsureAgentFreeMovement(agent);
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				EnsureAgentFreeMovement(mountAgent);
			}
		}
		catch
		{
		}
	}

	private void EnsureAgentFreeMovement(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		RestoreDetachedFormation(agent);
		ForgetMountedHardLock(agent);
		try
		{
			if (agent.IsAIControlled)
			{
				TrySetAgentController(agent, "AI");
			}
		}
		catch
		{
		}
		ForgetMeetingLockAnchor(agent);
	}

	private void TryEnsureMainAgentPlayerController(Agent main)
	{
		if (main == null || !main.IsActive())
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = main.IsMainAgent;
		}
		catch
		{
			flag = false;
		}
		if (!flag)
		{
			return;
		}
		try
		{
			TrySetAgentController(main, "Player");
		}
		catch
		{
		}
		try
		{
			main.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
	}

	private void TryLockAgentToCurrentPosition(Agent agent, bool recaptureMeetingAnchor = false, bool preserveFacing = false)
	{
		if (base.Mission == null || agent == null || !agent.IsActive())
		{
			return;
		}
		if (!TryGetMeetingLockAnchor(agent, recaptureMeetingAnchor, out var position, out var vec))
		{
			return;
		}
		float rotationInRadians = 0f;
		if (preserveFacing)
		{
			Vec3 lookDirection = new Vec3(vec.x, vec.y);
			try
			{
				agent.LookDirection = lookDirection;
			}
			catch
			{
			}
			try
			{
				agent.SetMovementDirection(in vec);
			}
			catch
			{
			}
			rotationInRadians = vec.RotationInRadians;
		}
		else
		{
			Vec2 vec2 = Vec2.Zero;
			try
			{
				vec2 = agent.LookDirection.AsVec2;
			}
			catch
			{
				vec2 = Vec2.Zero;
			}
			if (vec2.LengthSquared < 0.0001f)
			{
				vec2 = vec;
			}
			rotationInRadians = vec2.Normalized().RotationInRadians;
		}
		try
		{
			if (base.Mission.Scene != null)
			{
				float height = position.z;
				if (base.Mission.Scene.GetHeightAtPoint(position.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
				{
					position.z = height;
				}
				else
				{
					position.z = base.Mission.Scene.GetGroundHeightAtPosition(position);
				}
			}
		}
		catch
		{
		}
		try
		{
			WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, position);
			agent.SetScriptedPositionAndDirection(ref scriptedPosition, rotationInRadians, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
		}
		catch
		{
		}
	}

	private void FreezeAllFormationsForMeeting()
	{
		try
		{
			foreach (Team team in base.Mission.Teams)
			{
				if (team == null)
				{
					continue;
				}
				foreach (Formation item in team.FormationsIncludingEmpty)
				{
					if (item != null)
					{
						try
						{
							item.SetMovementOrder(MovementOrder.MovementOrderStop);
						}
						catch
						{
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	private void ClearMeetingLockAnchors()
	{
		_meetingLockPositions.Clear();
		_meetingLockDirections.Clear();
	}

	private void ClearMeetingDetachedFormations()
	{
		_meetingDetachedFormations.Clear();
	}

	private void DetachAgentFromFormationForMeetingLock(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		Formation formation = null;
		int index;
		try
		{
			if (!agent.IsHuman)
			{
				return;
			}
			index = agent.Index;
			formation = agent.Formation;
		}
		catch
		{
			return;
		}
		if (formation == null)
		{
			return;
		}
		if (!_meetingDetachedFormations.ContainsKey(index))
		{
			_meetingDetachedFormations[index] = formation;
		}
		try
		{
			agent.Formation = null;
		}
		catch
		{
		}
	}

	private void RestoreDetachedFormation(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		int index;
		try
		{
			index = agent.Index;
		}
		catch
		{
			return;
		}
		if (!_meetingDetachedFormations.TryGetValue(index, out var formation))
		{
			return;
		}
		try
		{
			if (formation != null && agent.IsActive())
			{
				agent.Formation = formation;
			}
		}
		catch
		{
		}
		finally
		{
			_meetingDetachedFormations.Remove(index);
		}
	}

	private void RestoreAllDetachedFormations()
	{
		if (_meetingDetachedFormations.Count == 0)
		{
			return;
		}
		try
		{
			if (base.Mission != null)
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					RestoreDetachedFormation(agent);
				}
			}
		}
		catch
		{
		}
		finally
		{
			_meetingDetachedFormations.Clear();
		}
	}

	private void ForgetMeetingLockAnchor(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		try
		{
			_meetingLockPositions.Remove(agent.Index);
			_meetingLockDirections.Remove(agent.Index);
		}
		catch
		{
		}
	}

	private bool TryGetMeetingLockAnchor(Agent agent, bool recaptureMeetingAnchor, out Vec3 position, out Vec2 direction)
	{
		position = Vec3.Zero;
		direction = Vec2.Zero;
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		int index;
		try
		{
			index = agent.Index;
		}
		catch
		{
			return false;
		}
		if (recaptureMeetingAnchor || !_meetingLockPositions.TryGetValue(index, out position))
		{
			Vec3 lookDirection;
			try
			{
				position = agent.Position;
				lookDirection = agent.LookDirection;
			}
			catch
			{
				return false;
			}
			lookDirection.z = 0f;
			if (lookDirection.LengthSquared < 0.0001f)
			{
				lookDirection = new Vec3(1f);
			}
			lookDirection.Normalize();
			direction = lookDirection.AsVec2;
			if (direction.LengthSquared < 0.0001f)
			{
				direction = new Vec2(1f, 0f);
			}
			direction = direction.Normalized();
			_meetingLockPositions[index] = position;
			_meetingLockDirections[index] = direction;
			return true;
		}
		if (!_meetingLockDirections.TryGetValue(index, out direction) || direction.LengthSquared < 0.0001f)
		{
			direction = new Vec2(1f, 0f);
			_meetingLockDirections[index] = direction;
		}
		return true;
	}

	private void ResumeAllAIAgents()
	{
		try
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive())
				{
					continue;
				}
				try
				{
					if (agent.IsAIControlled)
					{
						agent.DisableScriptedMovement();
						agent.ClearTargetFrame();
						agent.SetIsAIPaused(isPaused: false);
						RestoreDetachedFormation(agent);
					}
				}
				catch
				{
				}
				try
				{
					Agent mountAgent = agent.MountAgent;
					if (mountAgent != null && mountAgent.IsActive())
					{
						mountAgent.DisableScriptedMovement();
						mountAgent.ClearTargetFrame();
						mountAgent.SetIsAIPaused(isPaused: false);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		RestoreAllDetachedFormations();
		ClearMeetingLockAnchors();
		ClearMeetingMountedHardLocks();
	}

	private void ReleaseMeetingLocksForCombat()
	{
		if (base.Mission == null)
		{
			return;
		}
		Agent agent = null;
		Agent agent2 = null;
		try
		{
			agent = _mainAgent;
			if (agent == null || !agent.IsActive())
			{
				agent = base.Mission.MainAgent;
			}
			if (agent != null && agent.IsActive())
			{
				agent2 = agent.MountAgent;
			}
		}
		catch
		{
		}
		if (agent != null && agent.IsActive())
		{
			TryEnsureMainAgentPlayerController(agent);
			EnsureAgentFreeMovement(agent);
		}
		if (agent2 != null && agent2.IsActive())
		{
			EnsureAgentFreeMovement(agent2);
		}
		int num = 0;
		try
		{
			foreach (Agent agent4 in base.Mission.Agents)
			{
				if (agent4 != null && agent4.IsActive() && agent4 != agent && agent4 != agent2)
				{
					if (ReleaseSingleAgentFromMeetingLock(agent4))
					{
						num++;
					}
					Agent agent3 = null;
					try
					{
						agent3 = agent4.MountAgent;
					}
					catch
					{
						agent3 = null;
					}
					if (agent3 != null && agent3.IsActive() && agent3 != agent && agent3 != agent2 && ReleaseSingleAgentFromMeetingLock(agent3))
					{
						num++;
					}
				}
			}
		}
		catch
		{
		}
		ClearMeetingLockAnchors();
		Logger.Log("MeetingBattle", $"Meeting combat unlock applied. ReleasedAgents={num}");
	}

	private bool ReleaseSingleAgentFromMeetingLock(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		bool result = false;
		try
		{
			TrySetAgentController(agent, "AI");
			result = true;
		}
		catch
		{
		}
		try
		{
			agent.DisableScriptedMovement();
			result = true;
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
			result = true;
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
			result = true;
		}
		catch
		{
		}
		try
		{
			if (agent.IsAIControlled)
			{
				try
				{
					agent.SetMovementDirection(in Vec2.Zero);
				}
				catch
				{
				}
				result = true;
			}
		}
		catch
		{
		}
		ForgetMountedHardLock(agent);
		RestoreDetachedFormation(agent);
		ForgetMeetingLockAnchor(agent);
		return result;
	}

	private void ApplyFrame(Agent agent, MatrixFrame frame)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		Vec3 origin = frame.origin;
		try
		{
			if (base.Mission?.Scene != null)
			{
				float height = origin.z;
				if (base.Mission.Scene.GetHeightAtPoint(origin.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
				{
					origin.z = height;
				}
				else
				{
					origin.z = base.Mission.Scene.GetGroundHeightAtPosition(origin);
				}
			}
		}
		catch
		{
		}
		Vec3 lookDirection = frame.rotation.f;
		lookDirection.z = 0f;
		if (lookDirection.LengthSquared < 0.0001f)
		{
			lookDirection = new Vec3(1f);
		}
		lookDirection.Normalize();
		try
		{
			agent.TeleportToPosition(origin);
		}
		catch
		{
		}
		try
		{
			agent.LookDirection = lookDirection;
		}
		catch
		{
		}
		try
		{
			if (agent.IsAIControlled)
			{
				agent.SetIsAIPaused(isPaused: true);
				agent.ClearTargetFrame();
				agent.SetTargetPosition(origin.AsVec2);
			}
		}
		catch
		{
		}
		try
		{
			if (agent.MountAgent == null || !agent.MountAgent.IsActive())
			{
				return;
			}
			agent.MountAgent.LookDirection = lookDirection;
			bool flag = false;
			try
			{
				flag = agent.IsMainAgent;
			}
			catch
			{
				flag = false;
			}
			if (flag)
			{
				try
				{
					agent.MountAgent.SetIsAIPaused(isPaused: false);
				}
				catch
				{
				}
				try
				{
					agent.MountAgent.DisableScriptedMovement();
				}
				catch
				{
				}
				try
				{
					agent.MountAgent.ClearTargetFrame();
					return;
				}
				catch
				{
					return;
				}
			}
			try
			{
				agent.MountAgent.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent.MountAgent.ClearTargetFrame();
			}
			catch
			{
			}
			try
			{
				agent.MountAgent.SetTargetPosition(agent.MountAgent.Position.AsVec2);
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void TrySheathWeapons(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		try
		{
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
		try
		{
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
	}

	private bool TryPlaceEscortGuards()
	{
		if (_mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return false;
		}
		Team team = null;
		Team team2 = null;
		try
		{
			team = _mainAgent.Team;
		}
		catch
		{
		}
		try
		{
			team2 = _targetAgent.Team;
		}
		catch
		{
		}
		if (team == null || team2 == null)
		{
			return false;
		}
		if (team == team2)
		{
			if (_escortDebugLogCooldown <= 0f)
			{
				Logger.Log("MeetingBattle", "Escort placement postponed: main/target leaders are still on the same team.");
				_escortDebugLogCooldown = 2f;
			}
			return false;
		}
		List<(float, float, bool)> list = BuildEscortSlots();
		if (list.Count == 0)
		{
			return false;
		}
		List<Agent> list2 = CollectTopTierTeamAgents(team, list.Count);
		List<Agent> list3 = CollectTopTierTeamAgents(team2, list.Count);
		Vec3 vec = _targetAgent.Position - _mainAgent.Position;
		vec.z = 0f;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec3(1f);
		}
		vec.Normalize();
		Vec3 vec2 = _mainAgent.Position - _targetAgent.Position;
		vec2.z = 0f;
		if (vec2.LengthSquared < 0.0001f)
		{
			vec2 = -vec;
		}
		vec2.Normalize();
		PartyBase party = null;
		PartyBase party2 = null;
		try
		{
			party = PartyBase.MainParty;
		}
		catch
		{
		}
		try
		{
			party2 = _targetHero?.PartyBelongedTo?.Party;
		}
		catch
		{
		}
		bool flag = ShouldRequireEscortForSide(team, party);
		bool flag2 = ShouldRequireEscortForSide(team2, party2);
		bool flag3 = flag && list2.Count < list.Count;
		bool flag4 = flag2 && list3.Count < list.Count;
		if (flag3 || flag4)
		{
			TrySpawnFallbackEscortsForBothSides(list.Count, team, team2, _mainAgent.Position, vec, _targetAgent.Position, vec2, list2.Count, list3.Count, flag3, flag4);
			list2 = CollectTopTierTeamAgents(team, list.Count);
			list3 = CollectTopTierTeamAgents(team2, list.Count);
		}
		if (list2.Count > 0 || list3.Count > 0)
		{
			ConfigureMeetingHoldFormations(list2.FirstOrDefault(), list3.FirstOrDefault());
		}
		if (list2.Count > 0)
		{
			PositionEscortAgents(_mainAgent.Position, vec, list2, list);
		}
		if (list3.Count > 0)
		{
			PositionEscortAgents(_targetAgent.Position, vec2, list3, list);
		}
		bool flag5 = !flag || list2.Count > 0;
		bool flag6 = !flag2 || list3.Count > 0;
		if (!flag5 || !flag6)
		{
			if (_escortDebugLogCooldown <= 0f)
			{
				Logger.Log("MeetingBattle", $"Escort pending: playerCandidates={list2.Count}, targetCandidates={list3.Count}, playerEscortRequired={flag}, targetEscortRequired={flag2}");
				_escortDebugLogCooldown = 5f;
			}
			return false;
		}
		if (list2.Count > 0 || list3.Count > 0)
		{
			Logger.Log("MeetingBattle", $"Escort guards placed. PlayerSide={list2.Count}, TargetSide={list3.Count}");
		}
		return true;
	}

	private bool ShouldRequireEscortForSide(Team team, PartyBase primaryParty)
	{
		if (CountHealthyNonHeroTroops(primaryParty) > 0)
		{
			return true;
		}
		if (CountCurrentTeamEscortCandidates(team) > 0)
		{
			return true;
		}
		if (TryGetEncounterBattleTroopCountForTeam(team, out var troopCount))
		{
			return troopCount > 1;
		}
		return false;
	}

	private int CountCurrentTeamEscortCandidates(Team team)
	{
		if (team == null || base.Mission == null)
		{
			return 0;
		}
		int num = 0;
		Agent agent = null;
		Agent agent2 = null;
		try
		{
			agent = _mainAgent?.MountAgent;
		}
		catch
		{
		}
		try
		{
			agent2 = _targetAgent?.MountAgent;
		}
		catch
		{
		}
		try
		{
			foreach (Agent agent3 in base.Mission.Agents)
			{
				if (agent3 == null || !agent3.IsActive() || !agent3.IsHuman || agent3 == _mainAgent || agent3 == _targetAgent || agent3 == agent || agent3 == agent2)
				{
					continue;
				}
				Team team2 = null;
				try
				{
					team2 = agent3.Team;
				}
				catch
				{
					team2 = null;
				}
				if (team2 == null || team2 != team)
				{
					continue;
				}
				CharacterObject characterObject = null;
				try
				{
					characterObject = agent3.Character as CharacterObject;
				}
				catch
				{
					characterObject = null;
				}
				if (characterObject != null && !characterObject.IsHero)
				{
					num++;
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private bool TryGetEncounterBattleTroopCountForTeam(Team team, out int troopCount)
	{
		troopCount = 0;
		if (team == null)
		{
			return false;
		}
		if (!TryGetBattleSideForTeam(team, out var side))
		{
			return false;
		}
		MapEvent currentEncounterBattleSafe = GetCurrentEncounterBattleSafe();
		if (currentEncounterBattleSafe == null)
		{
			return false;
		}
		try
		{
			troopCount = ((side == BattleSideEnum.Attacker) ? currentEncounterBattleSafe.AttackerSide.TroopCount : currentEncounterBattleSafe.DefenderSide.TroopCount);
			return troopCount > 0;
		}
		catch
		{
			troopCount = 0;
			return false;
		}
	}

	private bool TryGetBattleSideForTeam(Team team, out BattleSideEnum side)
	{
		side = BattleSideEnum.None;
		if (team == null)
		{
			return false;
		}
		try
		{
			PropertyInfo propertyInfo = team.GetType().GetProperty("Side") ?? team.GetType().GetProperty("BattleSide") ?? team.GetType().GetProperty("MissionSide");
			object value = propertyInfo?.GetValue(team, null);
			if (value == null)
			{
				return false;
			}
			if (value is BattleSideEnum battleSideEnum)
			{
				side = battleSideEnum;
				return side != BattleSideEnum.None;
			}
			if (Enum.TryParse(value.ToString(), ignoreCase: true, out BattleSideEnum result))
			{
				side = result;
				return side != BattleSideEnum.None;
			}
		}
		catch
		{
		}
		return false;
	}

	private MapEvent GetCurrentEncounterBattleSafe()
	{
		try
		{
			return PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle ?? MapEvent.PlayerMapEvent;
		}
		catch
		{
			return null;
		}
	}

	private void TrySpawnFallbackEscortsForBothSides(int desiredCount, Team playerTeam, Team targetTeam, Vec3 playerAnchor, Vec3 playerForward, Vec3 targetAnchor, Vec3 targetForward, int existingPlayerEscorts, int existingTargetEscorts, bool allowPlayerSpawn, bool allowTargetSpawn)
	{
		try
		{
			PartyBase mainParty = PartyBase.MainParty;
			PartyBase partyBase = _targetHero?.PartyBelongedTo?.Party;
			if (mainParty == null || partyBase == null || playerTeam == null || targetTeam == null)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			int num3 = Math.Max(0, desiredCount - Math.Max(0, existingPlayerEscorts));
			int num4 = CountHealthyNonHeroTroops(mainParty);
			if (allowPlayerSpawn && num3 > 0 && num4 > 0)
			{
				int num5 = Math.Min(num3, num4);
				if (num5 > 0)
				{
					List<CharacterObject> list = CollectTopTroopsFromParty(mainParty, num5);
					if (list.Count > 0)
					{
						num = SpawnEscortAgentsFromTroops(mainParty, playerTeam, playerAnchor, playerForward, list);
					}
				}
			}
			int num6 = Math.Max(0, desiredCount - Math.Max(0, existingTargetEscorts));
			int num7 = CountHealthyNonHeroTroops(partyBase);
			if (allowTargetSpawn && num6 > 0 && num7 > 0)
			{
				int num8 = Math.Min(num6, num7);
				if (num8 > 0)
				{
					List<CharacterObject> list2 = CollectTopTroopsFromParty(partyBase, num8);
					if (list2.Count > 0)
					{
						num2 = SpawnEscortAgentsFromTroops(partyBase, targetTeam, targetAnchor, targetForward, list2);
					}
				}
			}
			Logger.Log("MeetingBattle", $"Fallback escort spawn: player={num}, target={num2}");
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingBattle", "Fallback escort spawn failed: " + ex.Message);
		}
	}

	private int CountHealthyNonHeroTroops(PartyBase party)
	{
		int num = 0;
		if (party == null)
		{
			return 0;
		}
		try
		{
			foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
			{
				CharacterObject character = item.Character;
				if (character != null && !character.IsHero)
				{
					int num2 = item.Number - item.WoundedNumber;
					if (num2 > 0)
					{
						num += num2;
					}
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private List<CharacterObject> CollectTopTroopsFromParty(PartyBase party, int maxCount)
	{
		List<CharacterObject> list = new List<CharacterObject>();
		if (party == null || maxCount <= 0)
		{
			return list;
		}
		List<(CharacterObject, int, int, int)> list2 = new List<(CharacterObject, int, int, int)>();
		try
		{
			foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
			{
				CharacterObject character = item.Character;
				if (character != null && !character.IsHero)
				{
					int num = item.Number - item.WoundedNumber;
					if (num > 0)
					{
						list2.Add((character, character.Tier, character.Level, num));
					}
				}
			}
		}
		catch
		{
		}
		foreach (var item2 in from x in list2
			orderby GetCharacterEscortPriority(x.Item1) descending, x.Item2 descending, x.Item3 descending
			select x)
		{
			for (int num2 = 0; num2 < item2.Item4; num2++)
			{
				if (list.Count >= maxCount)
				{
					break;
				}
				list.Add(item2.Item1);
			}
			if (list.Count >= maxCount)
			{
				break;
			}
		}
		return list;
	}

	private int SpawnEscortAgentsFromTroops(PartyBase party, Team team, Vec3 anchor, Vec3 forward, List<CharacterObject> troops)
	{
		if (base.Mission == null || party == null || team == null || troops == null || troops.Count == 0)
		{
			return 0;
		}
		int num = 0;
		Vec3 vec = forward;
		vec.z = 0f;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec3(1f);
		}
		vec.Normalize();
		Vec3 vec2 = new Vec3(0f - vec.y, vec.x);
		if (vec2.LengthSquared < 0.0001f)
		{
			vec2 = Vec3.Side;
		}
		vec2.Normalize();
		for (int i = 0; i < troops.Count; i++)
		{
			CharacterObject characterObject = troops[i];
			if (characterObject == null)
			{
				continue;
			}
			Vec3 position = anchor + vec2 * (((i % 2 == 0) ? 1f : (-1f)) * (3f + (float)(i / 2) * 0.8f)) - vec * (3f + (float)(i / 2) * 0.5f);
			try
			{
				if (base.Mission.Scene != null)
				{
					float height = position.z;
					if (base.Mission.Scene.GetHeightAtPoint(position.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
					{
						position.z = height;
					}
					else
					{
						position.z = base.Mission.Scene.GetGroundHeightAtPosition(position);
					}
				}
			}
			catch
			{
			}
			bool noHorses = !CharacterPrefersMountedEscort(characterObject);
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new PartyAgentOrigin(party, characterObject)).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement")).Team(team)
				.InitialPosition(in position)
				.InitialDirection(vec.AsVec2.Normalized())
				.Controller(AgentControllerType.AI)
				.CivilianEquipment(civilianEquipment: false)
				.NoHorses(noHorses);
			Agent agent = null;
			try
			{
				agent = base.Mission.SpawnAgent(agentBuildData);
			}
			catch
			{
				agent = null;
			}
			if (agent != null)
			{
				try
				{
					agent.SetIsAIPaused(isPaused: true);
				}
				catch
				{
				}
				try
				{
					agent.ClearTargetFrame();
				}
				catch
				{
				}
				try
				{
					agent.SetTargetPosition(agent.Position.AsVec2);
				}
				catch
				{
				}
				try
				{
					agent.LookDirection = vec;
				}
				catch
				{
				}
				try
				{
					TrySheathWeapons(agent);
				}
				catch
				{
				}
				num++;
			}
		}
		return num;
	}

	private List<Agent> CollectTopTierTeamAgents(Team team, int maxCount)
	{
		List<Agent> list = new List<Agent>();
		if (team == null || maxCount <= 0 || base.Mission == null)
		{
			return list;
		}
		Agent agent = null;
		Agent agent2 = null;
		try
		{
			agent = _mainAgent?.MountAgent;
		}
		catch
		{
		}
		try
		{
			agent2 = _targetAgent?.MountAgent;
		}
		catch
		{
		}
		try
		{
			foreach (Agent agent3 in base.Mission.Agents)
			{
				if (agent3 == null || !agent3.IsActive() || !agent3.IsHuman || agent3 == _mainAgent || agent3 == _targetAgent || agent3 == agent || agent3 == agent2)
				{
					continue;
				}
				Team team2 = null;
				try
				{
					team2 = agent3.Team;
				}
				catch
				{
				}
				if (team2 != null && team2 == team)
				{
					CharacterObject characterObject = null;
					try
					{
						characterObject = agent3.Character as CharacterObject;
					}
					catch
					{
					}
					if (characterObject != null && !characterObject.IsHero)
					{
						list.Add(agent3);
					}
				}
			}
		}
		catch
		{
		}
		return list.OrderByDescending(GetAgentEscortPriority).ThenByDescending(GetAgentTier).ThenByDescending(GetAgentLevel).Take(maxCount)
			.ToList();
	}

	private int GetAgentEscortPriority(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return 0;
		}
		try
		{
			if (agent.MountAgent != null && agent.MountAgent.IsActive())
			{
				return 3;
			}
		}
		catch
		{
		}
		try
		{
			if (agent.Character is CharacterObject characterObject)
			{
				return GetCharacterEscortPriority(characterObject);
			}
		}
		catch
		{
		}
		return 0;
	}

	private int GetCharacterEscortPriority(CharacterObject character)
	{
		if (character == null)
		{
			return 0;
		}
		try
		{
			if (CharacterPrefersMountedEscort(character))
			{
				return IsHorseArcherCharacter(character) ? 2 : 3;
			}
		}
		catch
		{
		}
		return 1;
	}

	private bool CharacterPrefersMountedEscort(CharacterObject character)
	{
		if (character == null)
		{
			return false;
		}
		try
		{
			if (character.IsMounted)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			FormationClass defaultFormationClass = character.DefaultFormationClass;
			return defaultFormationClass == FormationClass.Cavalry || defaultFormationClass == FormationClass.LightCavalry || defaultFormationClass == FormationClass.HeavyCavalry || defaultFormationClass == FormationClass.HorseArcher;
		}
		catch
		{
			return false;
		}
	}

	private void TryEquipMeetingEscortWeapons(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		EquipmentIndex? shieldSlot = null;
		EquipmentIndex? preferredShieldPolearmSlot = null;
		EquipmentIndex? preferredPolearmSlot = null;
		EquipmentIndex? preferredShieldWeaponSlot = null;
		EquipmentIndex? preferredWeaponSlot = null;
		int num = int.MinValue;
		int num2 = int.MinValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		EquipmentIndex[] array = new EquipmentIndex[5]
		{
			EquipmentIndex.ExtraWeaponSlot,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3
		};
		EquipmentIndex[] array2 = array;
		foreach (EquipmentIndex equipmentIndex in array2)
		{
			ItemObject itemObject = null;
			WeaponComponentData weaponComponentData = null;
			try
			{
				itemObject = agent.Equipment[equipmentIndex].Item;
			}
			catch
			{
				itemObject = null;
			}
			if (itemObject == null)
			{
				continue;
			}
			try
			{
				weaponComponentData = itemObject.PrimaryWeapon;
			}
			catch
			{
				weaponComponentData = null;
			}
			if (weaponComponentData == null)
			{
				continue;
			}
			bool flag = false;
			try
			{
				flag = weaponComponentData.IsShield || itemObject.Type == ItemObject.ItemTypeEnum.Shield;
			}
			catch
			{
				flag = false;
			}
			if (flag)
			{
				if (!shieldSlot.HasValue)
				{
					shieldSlot = equipmentIndex;
				}
				continue;
			}
			bool flag2 = false;
			try
			{
				flag2 = itemObject.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand);
			}
			catch
			{
				flag2 = false;
			}
			if (flag2)
			{
				continue;
			}
			int num5 = this.GetMeetingEscortWeaponScore(itemObject);
			bool flag3 = this.IsMeetingEscortPolearmWeapon(itemObject);
			bool flag4 = this.CanUseMeetingEscortWeaponWithShield(itemObject);
			if (num5 > num4)
			{
				preferredWeaponSlot = equipmentIndex;
				num4 = num5;
			}
			if (flag4 && num5 > num3)
			{
				preferredShieldWeaponSlot = equipmentIndex;
				num3 = num5;
			}
			if (flag3 && num5 > num2)
			{
				preferredPolearmSlot = equipmentIndex;
				num2 = num5;
			}
			if (!shieldSlot.HasValue || !flag3 || !flag4 || num5 <= num)
			{
				continue;
			}
			preferredShieldPolearmSlot = equipmentIndex;
			num = num5;
		}
		EquipmentIndex? equipmentIndex2 = preferredShieldPolearmSlot ?? preferredPolearmSlot ?? preferredShieldWeaponSlot ?? preferredWeaponSlot;
		if (!equipmentIndex2.HasValue)
		{
			return;
		}
		try
		{
			agent.TryToWieldWeaponInSlot(equipmentIndex2.Value, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn: false);
		}
		catch
		{
		}
		if (shieldSlot.HasValue)
		{
			try
			{
				ItemObject item = agent.Equipment[equipmentIndex2.Value].Item;
				if (this.CanUseMeetingEscortWeaponWithShield(item))
				{
					agent.TryToWieldWeaponInSlot(shieldSlot.Value, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn: false);
				}
			}
			catch
			{
			}
		}
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.SetIsAIPaused(isPaused: true);
			}
		}
		catch
		{
		}
	}

	private int GetMeetingEscortWeaponScore(ItemObject itemObject)
	{
		if (itemObject == null)
		{
			return int.MinValue;
		}
		WeaponComponentData primaryWeapon = null;
		try
		{
			primaryWeapon = itemObject.PrimaryWeapon;
		}
		catch
		{
			primaryWeapon = null;
		}
		if (primaryWeapon == null)
		{
			return int.MinValue;
		}
		int num = 0;
		bool flag = false;
		try
		{
			flag = primaryWeapon.IsMeleeWeapon;
		}
		catch
		{
			flag = false;
		}
		if (flag)
		{
			num += 500;
		}
		bool flag2 = this.IsMeetingEscortPolearmWeapon(itemObject);
		if (flag2)
		{
			num += 2000;
		}
		if (this.IsMeetingEscortPreferredPolearm(itemObject))
		{
			num += 1000;
		}
		if (this.CanUseMeetingEscortWeaponWithShield(itemObject))
		{
			num += 300;
		}
		bool flag3 = false;
		try
		{
			flag3 = primaryWeapon.IsRangedWeapon;
		}
		catch
		{
			flag3 = false;
		}
		if (flag3)
		{
			num -= 800;
		}
		return num;
	}

	private bool IsMeetingEscortPolearmWeapon(ItemObject itemObject)
	{
		if (itemObject == null)
		{
			return false;
		}
		WeaponComponentData primaryWeapon = null;
		try
		{
			primaryWeapon = itemObject.PrimaryWeapon;
		}
		catch
		{
			primaryWeapon = null;
		}
		if (primaryWeapon == null)
		{
			return false;
		}
		try
		{
			if (itemObject.Type == ItemObject.ItemTypeEnum.Polearm)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			WeaponClass weaponClass = primaryWeapon.WeaponClass;
			if (weaponClass == WeaponClass.OneHandedPolearm || weaponClass == WeaponClass.TwoHandedPolearm || weaponClass == WeaponClass.LowGripPolearm || weaponClass == WeaponClass.Javelin)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return primaryWeapon.IsPolearm;
		}
		catch
		{
		}
		return this.IsMeetingEscortPreferredPolearm(itemObject);
	}

	private bool IsMeetingEscortPreferredPolearm(ItemObject itemObject)
	{
		if (itemObject == null)
		{
			return false;
		}
		string text = "";
		string text2 = "";
		try
		{
			text = (itemObject.StringId ?? "").ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		try
		{
			text2 = (itemObject.Name?.ToString() ?? "").ToLowerInvariant();
		}
		catch
		{
			text2 = "";
		}
		return text.Contains("lance") || text.Contains("spear") || text.Contains("pike") || text2.Contains("骑枪") || text2.Contains("长矛") || text2.Contains("长枪") || text2.Contains("矛") || text2.Contains("枪");
	}

	private bool CanUseMeetingEscortWeaponWithShield(ItemObject itemObject)
	{
		if (itemObject == null)
		{
			return false;
		}
		WeaponComponentData primaryWeapon = null;
		try
		{
			primaryWeapon = itemObject.PrimaryWeapon;
		}
		catch
		{
			primaryWeapon = null;
		}
		if (primaryWeapon == null)
		{
			return false;
		}
		try
		{
			if (primaryWeapon.IsShield || itemObject.Type == ItemObject.ItemTypeEnum.Shield)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (itemObject.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (!primaryWeapon.IsOneHanded)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (primaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			ItemObject.ItemUsageSetFlags itemUsageSetFlags = MBItem.GetItemUsageSetFlags(primaryWeapon.ItemUsage);
			if (itemUsageSetFlags.HasAnyFlag(ItemObject.ItemUsageSetFlags.RequiresNoShield))
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private bool IsHorseArcherCharacter(CharacterObject character)
	{
		if (character == null)
		{
			return false;
		}
		try
		{
			return character.DefaultFormationClass == FormationClass.HorseArcher;
		}
		catch
		{
			return false;
		}
	}

	private int GetAgentTier(Agent agent)
	{
		try
		{
			return (agent?.Character is CharacterObject characterObject) ? characterObject.Tier : 0;
		}
		catch
		{
			return 0;
		}
	}

	private int GetAgentLevel(Agent agent)
	{
		try
		{
			return (agent?.Character is CharacterObject characterObject) ? characterObject.Level : 0;
		}
		catch
		{
			return 0;
		}
	}

	private List<(float fwdDist, float sideDist, bool faceBack)> BuildEscortSlots()
	{
		return new List<(float, float, bool)>
		{
			(-1f, -2.4f, false),
			(-1f, 2.4f, false)
		};
	}

	private void PositionEscortAgents(Vec3 anchor, Vec3 forward, List<Agent> escorts, List<(float fwdDist, float sideDist, bool faceBack)> slots)
	{
		if (escorts == null || slots == null || escorts.Count == 0 || slots.Count == 0)
		{
			return;
		}
		Vec3 vec = forward;
		vec.z = 0f;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec3(1f);
		}
		vec.Normalize();
		Vec3 vec2 = new Vec3(0f - vec.y, vec.x);
		if (vec2.LengthSquared < 0.0001f)
		{
			vec2 = Vec3.Side;
		}
		vec2.Normalize();
		int num = Math.Min(escorts.Count, slots.Count);
		for (int i = 0; i < num; i++)
		{
			Agent agent = escorts[i];
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			(float, float, bool) tuple = slots[i];
			Vec3 position = anchor + vec * tuple.Item1 + vec2 * tuple.Item2;
			Vec3 lookDirection = (tuple.Item3 ? (-vec) : vec);
			if (lookDirection.LengthSquared < 0.0001f)
			{
				lookDirection = vec;
			}
			lookDirection.Normalize();
			try
			{
				if (base.Mission?.Scene != null)
				{
					float height = position.z;
					if (base.Mission.Scene.GetHeightAtPoint(position.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
					{
						position.z = height;
					}
					else
					{
						position.z = base.Mission.Scene.GetGroundHeightAtPosition(position);
					}
				}
			}
			catch
			{
			}
			try
			{
				agent.TeleportToPosition(position);
			}
			catch
			{
			}
			try
			{
				agent.LookDirection = lookDirection;
			}
			catch
			{
			}
			try
			{
				agent.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent.ClearTargetFrame();
			}
			catch
			{
			}
			try
			{
				agent.SetTargetPosition(position.AsVec2);
			}
			catch
			{
			}
			try
			{
				TrySheathWeapons(agent);
			}
			catch
			{
			}
			bool flag = IsMeetingFormationManagedAgent(agent);
			if (flag)
			{
				try
				{
					TryEquipMeetingEscortWeapons(agent);
				}
				catch
				{
				}
			}
			bool flag2 = false;
			try
			{
				Agent mountAgent = agent.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					flag2 = true;
					mountAgent.TeleportToPosition(position);
					mountAgent.LookDirection = lookDirection;
					mountAgent.SetIsAIPaused(isPaused: true);
					mountAgent.ClearTargetFrame();
					mountAgent.SetTargetPosition(position.AsVec2);
					if (flag)
					{
						TrySetAgentController(mountAgent, "None");
					}
					else
					{
						TryLockAgentToCurrentPosition(mountAgent, recaptureMeetingAnchor: true, preserveFacing: true);
					}
				}
			}
			catch
			{
			}
			if (flag && flag2)
			{
				try
				{
					TrySetAgentController(agent, "None");
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					TryLockAgentToCurrentPosition(agent, recaptureMeetingAnchor: true, preserveFacing: true);
				}
				catch
				{
				}
			}
		}
	}
}
