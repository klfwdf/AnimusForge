using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class MeetingBattleLockMissionBehavior : MissionBehavior, IAgentStateDecider, IMissionBehavior
{
	private sealed class PendingFatalHitContext
	{
		internal DamageTypes DamageType;

		internal bool CanDamageKillEvenIfBlunt;

		internal PartyBase VictimParty;

		internal PartyBase EnemyParty;
	}

	private static MeetingBattleLockMissionBehavior _currentInstance;

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

	private Formation _mainOriginalFormation;

	private bool _hasCapturedMainOriginalFormation;

	private Formation _targetOriginalFormation;

	private bool _hasCapturedTargetOriginalFormation;

	private Formation _formalDuelPlayerFormation;

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

	private readonly Dictionary<int, PendingFatalHitContext> _pendingFatalHitContexts = new Dictionary<int, PendingFatalHitContext>();

	private bool _deferredDetachedFormationRestoreActive;

	private bool _deferredDetachedFormationRestoreApplied;

	private float _deferredDetachedFormationRestoreEarliestTime;

	public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

	public MeetingBattleLockMissionBehavior(Hero targetHero)
	{
		_targetHero = targetHero;
	}

	internal static void ReapplyMeetingLockForAgentIfNeeded(Agent agent, bool recaptureAnchor = false, bool preserveFacing = true)
	{
		try
		{
			_currentInstance?.TryReapplyMeetingLockForAgent(agent, recaptureAnchor, preserveFacing);
		}
		catch
		{
		}
	}

	public override void AfterStart()
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		((MissionBehavior)this).AfterStart();
		_currentInstance = this;
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
		_targetLockedForward = new Vec3(1f, 0f, 0f, -1f);
		_hasTargetLockedForward = false;
		_targetLockedPosition = Vec3.Zero;
		_hasTargetLockedPosition = false;
		_formalDuelCombatReleaseApplied = false;
		_mainOriginalFormation = null;
		_hasCapturedMainOriginalFormation = false;
		_targetOriginalFormation = null;
		_hasCapturedTargetOriginalFormation = false;
		_formalDuelPlayerFormation = null;
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
		_deferredDetachedFormationRestoreActive = false;
		_deferredDetachedFormationRestoreApplied = false;
		_deferredDetachedFormationRestoreEarliestTime = 0f;
		_pendingFatalHitContexts.Clear();
		ClearMeetingLockAnchors();
		ClearMeetingDetachedFormations();
		ClearMeetingMountedHardLocks();
		try
		{
			Hero mainHero = Hero.MainHero;
			_playerMapFactionAtEncounterStart = ((mainHero != null) ? mainHero.MapFaction : null);
		}
		catch
		{
			_playerMapFactionAtEncounterStart = null;
		}
		try
		{
			Hero targetHero = _targetHero;
			_targetMapFactionAtEncounterStart = ((targetHero != null) ? targetHero.MapFaction : null);
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
		if (_currentInstance == this)
		{
			_currentInstance = null;
		}
		bool flag = false;
		try
		{
			flag = ((MissionBehavior)this).Mission == null || ((MissionBehavior)this).Mission.MissionEnded;
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
		_pendingFatalHitContexts.Clear();
		((MissionBehavior)this).OnRemoveBehavior();
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
		_pendingFatalHitContexts.Clear();
		((MissionBehavior)this).OnEndMission();
	}

	public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		usedSurgery = false;
		try
		{
			if (TryUseMeetingNaturalDefeatState(effectedAgent, out var result))
			{
				return result;
			}
		}
		catch
		{
		}
		float num = deathProbability;
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		return (AgentState)((MBRandom.RandomFloat <= num) ? 4 : 3);
	}

	public override void OnMissionTick(float dt)
	{
		((MissionBehavior)this).OnMissionTick(dt);
		if (((MissionBehavior)this).Mission == null)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = ((MissionBehavior)this).Mission.MissionEnded;
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
			_formalDuelCombatReleaseApplied = false;
			_allowTargetFreeMovementAfterFormalDuel = false;
			EnsureTargetLordNeutralized();
			Logger.Log("MeetingBattle", "Formal duel ended: target duelist returned to meeting-neutral lock.");
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
				ArmDeferredDetachedFormationRestoreForCombat();
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
			TryRestoreDeferredDetachedFormationsAfterCombat();
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
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null)
		{
			return;
		}
		try
		{
			MissionMode mode = ((MissionBehavior)this).Mission.Mode;
			if ((int)mode != 2)
			{
				((MissionBehavior)this).Mission.SetMissionMode((MissionMode)2, false);
				Logger.Log("MeetingBattle", $"Forced mission mode to Battle for combat escalation. PreviousMode={mode}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingBattle", "Failed to force mission mode to Battle during combat escalation: " + ex.Message);
		}
	}

	private void EnsureMissionCombatTeamRelationships()
	{
		if (((MissionBehavior)this).Mission == null)
		{
			return;
		}
		Team val = null;
		Team val2 = null;
		Team val3 = null;
		try
		{
			Agent mainAgent = _mainAgent;
			val = ((mainAgent != null) ? mainAgent.Team : null) ?? ((MissionBehavior)this).Mission.PlayerTeam;
		}
		catch
		{
			val = null;
		}
		try
		{
			object obj2 = _targetOriginalTeam;
			if (obj2 == null)
			{
				Agent targetAgent = _targetAgent;
				obj2 = ((targetAgent != null) ? targetAgent.Team : null);
			}
			val2 = (Team)obj2;
		}
		catch
		{
			val2 = null;
		}
		try
		{
			val3 = ((MissionBehavior)this).Mission.PlayerEnemyTeam;
		}
		catch
		{
			val3 = null;
		}
		if ((val2 == null || val2 == val) && val3 != null && val3 != val)
		{
			val2 = val3;
			try
			{
				if (_targetAgent != null && _targetAgent.IsActive() && _targetAgent.Team != val2)
				{
					_targetAgent.SetTeam(val2, true);
				}
			}
			catch
			{
			}
			try
			{
				Agent targetAgent2 = _targetAgent;
				Agent val4 = ((targetAgent2 != null) ? targetAgent2.MountAgent : null);
				if (val4 != null && val4.IsActive() && val4.Team != val2)
				{
					val4.SetTeam(val2, true);
				}
			}
			catch
			{
			}
		}
		if (val == null || val2 == null || val == val2)
		{
			Logger.Log("MeetingBattle", "Combat team relationship fix skipped: unable to resolve distinct player/target teams.");
			return;
		}
		string teamSideKey = GetTeamSideKey(val);
		string teamSideKey2 = GetTeamSideKey(val2);
		List<Team> list = new List<Team>();
		List<Team> list2 = new List<Team>();
		AddUniqueTeam(list, val);
		AddUniqueTeam(list2, val2);
		try
		{
			foreach (Team item in (List<Team>)(object)((MissionBehavior)this).Mission.Teams)
			{
				if (item != null)
				{
					string teamSideKey3 = GetTeamSideKey(item);
					if (!string.IsNullOrEmpty(teamSideKey) && string.Equals(teamSideKey3, teamSideKey, StringComparison.OrdinalIgnoreCase))
					{
						AddUniqueTeam(list, item);
					}
					else if (!string.IsNullOrEmpty(teamSideKey2) && string.Equals(teamSideKey3, teamSideKey2, StringComparison.OrdinalIgnoreCase))
					{
						AddUniqueTeam(list2, item);
					}
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			AddUniqueTeam(list, val);
		}
		if (list2.Count == 0)
		{
			AddUniqueTeam(list2, val2);
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
			flag = AreTeamsHostileSafely(val, val2);
		}
		catch
		{
			flag = false;
		}
		Logger.Log("MeetingBattle", string.Format("Combat team relationship fix applied. PlayerSideKey={0}, TargetSideKey={1}, PlayerSideTeams={2}, TargetSideTeams={3}, PlayerAgents={4}, TargetAgents={5}, DirectEnemy={6}", teamSideKey ?? "unknown", teamSideKey2 ?? "unknown", list.Count, list2.Count, CountActiveAgentsOnTeams(list), CountActiveAgentsOnTeams(list2), flag));
	}

	private static void AddUniqueTeam(List<Team> teams, Team team)
	{
		if (teams != null && team != null && !teams.Contains(team))
		{
			teams.Add(team);
		}
	}

	private static bool IsUsableTeam(Team team)
	{
		try
		{
			return team != null && team != Team.Invalid && team.IsValid;
		}
		catch
		{
			return false;
		}
	}

	private static bool AreTeamsHostileSafely(Team firstTeam, Team secondTeam)
	{
		try
		{
			return IsUsableTeam(firstTeam) && IsUsableTeam(secondTeam) && firstTeam != secondTeam && (firstTeam.IsEnemyOf(secondTeam) || secondTeam.IsEnemyOf(firstTeam));
		}
		catch
		{
			return false;
		}
	}

	private void TrySetEnemyRelation(Team a, Team b, bool isEnemy)
	{
		if (a == null || b == null || a == b)
		{
			return;
		}
		try
		{
			a.SetIsEnemyOf(b, isEnemy);
		}
		catch
		{
		}
		try
		{
			b.SetIsEnemyOf(a, isEnemy);
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
			PropertyInfo propertyInfo = ((object)team).GetType().GetProperty("Side") ?? ((object)team).GetType().GetProperty("BattleSide") ?? ((object)team).GetType().GetProperty("MissionSide");
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
		if (teams == null || teams.Count == 0 || ((MissionBehavior)this).Mission == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsActive())
				{
					Team val = null;
					try
					{
						val = item.Team;
					}
					catch
					{
						val = null;
					}
					if (val != null && teams.Contains(val))
					{
						num++;
					}
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
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		Team val = null;
		try
		{
			Agent mainAgent = _mainAgent;
			val = ((mainAgent != null) ? mainAgent.Team : null) ?? ((MissionBehavior)this).Mission.PlayerTeam;
		}
		catch
		{
			val = null;
		}
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item == null || !item.IsActive())
				{
					continue;
				}
				try
				{
					if (!item.IsMainAgent)
					{
						AgentFlag agentFlags = item.GetAgentFlags();
						item.SetAgentFlags((AgentFlag)(agentFlags | 0x10000));
					}
				}
				catch
				{
				}
				try
				{
					item.SetAlarmState((AIStateFlag)3);
					num++;
				}
				catch
				{
				}
				try
				{
					item.SetWatchState((WatchState)2);
				}
				catch
				{
				}
				try
				{
					item.WieldInitialWeapons((WeaponWieldActionType)2, (InitialWeaponEquipPreference)0);
					num2++;
				}
				catch
				{
				}
				try
				{
					item.SetFiringOrder((RangedWeaponUsageOrderEnum)0);
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
			foreach (Team item2 in (List<Team>)(object)((MissionBehavior)this).Mission.Teams)
			{
				if (item2 == null)
				{
					continue;
				}
				try
				{
					foreach (Formation item3 in (List<Formation>)(object)item2.FormationsIncludingEmpty)
					{
						if (item3 != null)
						{
							try
							{
								item3.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
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
				if (val == null || item2 == val)
				{
					continue;
				}
				try
				{
					OrderController masterOrderController = item2.MasterOrderController;
					if (masterOrderController != null)
					{
						masterOrderController.SelectAllFormations(false);
					}
				}
				catch
				{
				}
				try
				{
					OrderController masterOrderController2 = item2.MasterOrderController;
					if (masterOrderController2 != null)
					{
						masterOrderController2.SetOrder((OrderType)4);
					}
					num3++;
				}
				catch
				{
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
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		if (_startupLoadingFadeApplied || _startupLoadingFadeAborted || ((MissionBehavior)this).Mission == null)
		{
			return;
		}
		_startupLoadingFadeElapsed += dt;
		MissionCameraFadeView val = null;
		try
		{
			val = ((MissionBehavior)this).Mission.GetMissionBehavior<MissionCameraFadeView>();
		}
		catch
		{
			val = null;
		}
		if (val == null)
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
			if ((int)val.FadeState == 0)
			{
				val.BeginFadeOutAndIn(0.08f, 4f, 0.22f);
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
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Invalid comparison between Unknown and I4
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if (_deploymentSkipApplied || ((MissionBehavior)this).Mission == null)
		{
			return;
		}
		float num = 0f;
		try
		{
			num = ((MissionBehavior)this).Mission.CurrentTime;
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
			flag = (int)((MissionBehavior)this).Mission.Mode == 6;
			if (!flag)
			{
				string text = ((object)((MissionBehavior)this).Mission.Mode/*cast due to .constrained prefix*/).ToString();
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
			DeploymentHandler missionBehavior = ((MissionBehavior)this).Mission.GetMissionBehavior<DeploymentHandler>();
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
		if (((MissionBehavior)this).Mission == null)
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
		Agent val = _mainAgent;
		if (val == null || !val.IsActive())
		{
			try
			{
				val = ((MissionBehavior)this).Mission.MainAgent;
			}
			catch
			{
				val = null;
			}
			if (val == null || !val.IsActive())
			{
				try
				{
					val = Agent.Main;
				}
				catch
				{
					val = null;
				}
			}
		}
		if (val != null && val.IsActive())
		{
			_mainAgent = val;
		}
		Agent targetAgent = _targetAgent;
		if (targetAgent == null || !targetAgent.IsActive())
		{
			FindMainAndTargetAgents();
			targetAgent = _targetAgent;
		}
		Agent val2 = null;
		Agent val3 = null;
		try
		{
			val2 = ((val != null) ? val.MountAgent : null);
		}
		catch
		{
			val2 = null;
		}
		try
		{
			val3 = ((targetAgent != null) ? targetAgent.MountAgent : null);
		}
		catch
		{
			val3 = null;
		}
		try
		{
			if (val != null && val.IsActive())
			{
				if (val.IsAIControlled)
				{
					val.SetIsAIPaused(false);
				}
				if (flag)
				{
					val.DisableScriptedMovement();
					val.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					TryEnsureMainAgentPlayerController(val);
					EnsureAgentFreeMovement(val);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (val2 != null && val2.IsActive())
			{
				val2.SetIsAIPaused(false);
				if (flag)
				{
					val2.DisableScriptedMovement();
					val2.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					EnsureAgentFreeMovement(val2);
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
					ReleaseSingleAgentFromMeetingLock(targetAgent);
					targetAgent.SetWatchState((WatchState)2);
					try
					{
						targetAgent.WieldInitialWeapons((WeaponWieldActionType)2, (InitialWeaponEquipPreference)1);
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
		try
		{
			if (val3 != null && val3.IsActive())
			{
				TrySetAgentController(val3, flag ? "None" : "AI");
				val3.SetIsAIPaused(flag);
				if (flag)
				{
					val3.DisableScriptedMovement();
					val3.ClearTargetFrame();
				}
				else if (!_formalDuelCombatReleaseApplied)
				{
					ReleaseSingleAgentFromMeetingLock(val3);
				}
			}
		}
		catch
		{
		}
		if (!flag)
		{
			if (val != null && val.IsActive() && targetAgent != null && targetAgent.IsActive())
			{
				_formalDuelCombatReleaseApplied = true;
			}
			try
			{
				KeepFormalDuelOpponentsEngaged(val, targetAgent);
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
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item == null || !item.IsActive())
				{
					continue;
				}
				bool flag2 = false;
				try
				{
					flag2 = item.IsMainAgent;
				}
				catch
				{
					flag2 = false;
				}
				bool flag3 = item == val || item == val2 || item == targetAgent || item == val3 || flag2;
				if (!flag3)
				{
					try
					{
						Agent riderAgent = item.RiderAgent;
						if (riderAgent != null && (riderAgent == val || riderAgent == targetAgent || riderAgent.IsMainAgent))
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
					if (item.IsAIControlled)
					{
						item.SetIsAIPaused(true);
						item.DisableScriptedMovement();
						item.ClearTargetFrame();
					}
				}
				catch
				{
				}
				try
				{
					Agent mountAgent = item.MountAgent;
					if (mountAgent == null || !mountAgent.IsActive())
					{
						continue;
					}
					bool flag4 = mountAgent == val || mountAgent == val2 || mountAgent == targetAgent || mountAgent == val3;
					if (!flag4)
					{
						try
						{
							Agent riderAgent2 = mountAgent.RiderAgent;
							if (riderAgent2 != null && (riderAgent2 == val || riderAgent2 == targetAgent || riderAgent2.IsMainAgent))
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
						mountAgent.SetIsAIPaused(true);
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
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null || main == null || target == null || !main.IsActive() || !target.IsActive())
		{
			return;
		}
		if (!_hasCapturedMainOriginalFormation)
		{
			try
			{
				_mainOriginalFormation = main.Formation;
			}
			catch
			{
				_mainOriginalFormation = null;
			}
			_hasCapturedMainOriginalFormation = true;
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
			target.SetWatchState((WatchState)2);
		}
		catch
		{
		}
		if (_formalDuelPlayerFormation == null)
		{
			_formalDuelPlayerFormation = CreateFormalDuelDetachedFormation(main, 0, null);
		}
		if (_formalDuelTargetFormation == null)
		{
			int num = 0;
			try
			{
				num = ((target.Team != null && target.Team == main.Team) ? 1 : 0);
			}
			catch
			{
				num = 0;
			}
			_formalDuelTargetFormation = CreateFormalDuelDetachedFormation(target, num, _formalDuelPlayerFormation);
		}
		if (_formalDuelPlayerFormation != null)
		{
			try
			{
				if (main.Formation != _formalDuelPlayerFormation)
				{
					main.Formation = _formalDuelPlayerFormation;
				}
			}
			catch
			{
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
		float num2 = 0f;
		try
		{
			num2 = ((MissionBehavior)this).Mission.CurrentTime;
		}
		catch
		{
		}
		if (!(num2 >= _formalDuelOrderRefreshTimer))
		{
			return;
		}
		_formalDuelOrderRefreshTimer = num2 + 0.5f;
		try
		{
			target.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = target.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.ClearTargetFrame();
			}
		}
		catch
		{
		}
		try
		{
			Formation obj10 = _formalDuelPlayerFormation ?? main.Formation;
			if (obj10 != null)
			{
				obj10.SetMovementOrder(MovementOrder.MovementOrderStop);
			}
		}
		catch
		{
		}
		try
		{
			Formation obj12 = _formalDuelTargetFormation ?? target.Formation;
			if (obj12 != null)
			{
				obj12.SetMovementOrder(MovementOrder.MovementOrderCharge);
			}
		}
		catch
		{
		}
	}

	private void RestoreTargetFormationAfterFormalDuel()
	{
		if (!_hasCapturedMainOriginalFormation && !_hasCapturedTargetOriginalFormation)
		{
			return;
		}
		try
		{
			if (_mainAgent != null && _mainAgent.IsActive())
			{
				try
				{
					_mainAgent.Formation = _mainOriginalFormation;
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
		_mainOriginalFormation = null;
		_hasCapturedMainOriginalFormation = false;
		_targetOriginalFormation = null;
		_hasCapturedTargetOriginalFormation = false;
		_formalDuelPlayerFormation = null;
		_formalDuelTargetFormation = null;
		_formalDuelOrderRefreshTimer = 0f;
	}

	private Formation ResolveFormalDuelSoloFormation(Agent agent, Formation avoidFormation)
	{
		if (agent == null || !agent.IsActive())
		{
			return null;
		}
		Formation val = null;
		try
		{
			val = agent.Formation;
		}
		catch
		{
			val = null;
		}
		try
		{
			if (val != null && val != avoidFormation && val.CountOfUnits <= 1)
			{
				return val;
			}
		}
		catch
		{
		}
		Team val2 = null;
		try
		{
			val2 = agent.Team;
		}
		catch
		{
			val2 = null;
		}
		if (val2 != null)
		{
			try
			{
				foreach (Formation item in (List<Formation>)(object)val2.FormationsIncludingEmpty)
				{
					if (item != null && item != avoidFormation && item.CountOfUnits == 0)
					{
						return item;
					}
				}
			}
			catch
			{
			}
		}
		return val;
	}

	private Formation CreateFormalDuelDetachedFormation(Agent agent, int formationIndex, Formation avoidFormation)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		if (agent == null || !agent.IsActive())
		{
			return null;
		}
		try
		{
			Team team = agent.Team;
			if (team != null)
			{
				Formation val = new Formation(team, formationIndex);
				if (val != null && val != avoidFormation)
				{
					return val;
				}
			}
		}
		catch
		{
		}
		return ResolveFormalDuelSoloFormation(agent, avoidFormation);
	}

	private void ResetLeaderLockAnchorAfterFormalDuel()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
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
			Mission mission = ((MissionBehavior)this).Mission;
			_mainAgent = ((mission != null) ? mission.MainAgent : null) ?? _mainAgent;
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
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive())
				{
					BasicCharacterObject character = item.Character;
					CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
					if (((val != null) ? val.HeroObject : null) == _targetHero)
					{
						_targetAgent = item;
						break;
					}
				}
			}
		}
		catch
		{
		}
	}

	private void PlaceLeadersForMeeting()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
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
			if (((Vec3)(ref _targetLockedForward)).LengthSquared > 0.0001f)
			{
				((Vec3)(ref _targetLockedForward)).Normalize();
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		targetFrame = MatrixFrame.Identity;
		playerFrame = MatrixFrame.Identity;
		if (((MissionBehavior)this).Mission == null || _mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return false;
		}
		Team val = null;
		Team val2 = null;
		try
		{
			val = _mainAgent.Team;
		}
		catch
		{
		}
		try
		{
			val2 = _targetAgent.Team;
		}
		catch
		{
		}
		if (val == null || val2 == null || val == val2)
		{
			return false;
		}
		if (!TryGetTeamHumanCenter(val, out var center) || !TryGetTeamHumanCenter(val2, out var center2))
		{
			return false;
		}
		Vec3 val3 = center - center2;
		val3.z = 0f;
		if (((Vec3)(ref val3)).LengthSquared < 0.0001f)
		{
			return false;
		}
		((Vec3)(ref val3)).Normalize();
		Vec3 val4 = (center + center2) * 0.5f;
		float num = 6.2f;
		Vec3 candidate = val4 - val3 * num;
		Vec3 candidate2 = val4 + val3 * num;
		LordEncounterBehavior.ClampPointInsideMissionBoundary(ref candidate, val4);
		LordEncounterBehavior.ClampPointInsideMissionBoundary(ref candidate2, val4);
		LordEncounterBehavior.ResolveSceneGroundHeight(((MissionBehavior)this).Mission.Scene, ref candidate);
		LordEncounterBehavior.ResolveSceneGroundHeight(((MissionBehavior)this).Mission.Scene, ref candidate2);
		targetFrame.origin = candidate;
		targetFrame.rotation.f = val3;
		((Mat3)(ref targetFrame.rotation)).OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		playerFrame.origin = candidate2;
		playerFrame.rotation.f = -val3;
		((Mat3)(ref playerFrame.rotation)).OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return true;
	}

	private bool TryGetTeamHumanCenter(Team team, out Vec3 center)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		center = Vec3.Zero;
		if (team == null || ((MissionBehavior)this).Mission == null)
		{
			return false;
		}
		Vec3 val = Vec3.Zero;
		int num = 0;
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsActive() && item.IsHuman)
				{
					Team val2 = null;
					try
					{
						val2 = item.Team;
					}
					catch
					{
					}
					if (val2 != null && val2 == team)
					{
						val += item.Position;
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
		center = val * (1f / (float)num);
		center.z = 0f;
		return true;
	}

	private void ConfigureMeetingHoldFormations(Agent playerEscort, Agent targetEscort)
	{
		if (((MissionBehavior)this).Mission != null)
		{
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
		int num = -1;
		try
		{
			num = agent.Index;
		}
		catch
		{
			return;
		}
		try
		{
			_meetingMountedHardLockRiderIndices.Remove(num);
			_meetingMountedHardLockMountIndices.Remove(num);
			_meetingMountedHardLockPositions.Remove(num);
			_meetingMountedHardLockForwards.Remove(num);
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
		if (_meetingMountedHardLockRiderIndices.Count <= 0 && ((MissionBehavior)this).Mission != null && _mainAgent != null && _targetAgent != null && _mainAgent.IsActive() && _targetAgent.IsActive())
		{
			Team val = null;
			Team val2 = null;
			try
			{
				val = _mainAgent.Team;
			}
			catch
			{
				val = null;
			}
			try
			{
				val2 = _targetAgent.Team;
			}
			catch
			{
				val2 = null;
			}
			if (val != null && val2 != null && val != val2)
			{
				RegisterMountedHardLocksForTeam(val);
				RegisterMountedHardLocksForTeam(val2);
			}
		}
	}

	private void RegisterMountedHardLocksForTeam(Team team)
	{
		if (team == null || ((MissionBehavior)this).Mission == null)
		{
			return;
		}
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (IsMountedHardLockCandidate(item, team))
				{
					RegisterMountedHardLock(item);
				}
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
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		Vec3 value = default(Vec3);
		((Vec3)(ref value))._002Ector(1f, 0f, 0f, -1f);
		try
		{
			Vec3 lookDirection = agent.LookDirection;
			lookDirection.z = 0f;
			if (((Vec3)(ref lookDirection)).LengthSquared > 0.0001f)
			{
				((Vec3)(ref lookDirection)).Normalize();
				value = lookDirection;
			}
		}
		catch
		{
		}
		if (((Vec3)(ref value)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref value))._002Ector(1f, 0f, 0f, -1f);
		}
		value.z = 0f;
		((Vec3)(ref value)).Normalize();
		try
		{
			_meetingMountedHardLockRiderIndices.Add(agent.Index);
			_meetingMountedHardLockPositions[agent.Index] = agent.Position;
			_meetingMountedHardLockForwards[agent.Index] = value;
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		if (_mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return;
		}
		try
		{
			Vec3 val = _mainAgent.Position - _targetAgent.Position;
			val.z = 0f;
			if (!(((Vec3)(ref val)).LengthSquared > 0.0001f))
			{
				return;
			}
			Vec3 val2 = val;
			if (_hasTargetLockedForward && ((Vec3)(ref _targetLockedForward)).LengthSquared > 0.0001f)
			{
				val2 = _targetLockedForward;
			}
			else
			{
				((Vec3)(ref val2)).Normalize();
			}
			_targetAgent.LookDirection = val2;
			try
			{
				Agent mountAgent = _targetAgent.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					mountAgent.LookDirection = val2;
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
			LockAgentAndMountInPlace(_targetAgent, val2, _hasTargetLockedPosition ? new Vec3?(_targetLockedPosition) : ((Vec3?)null));
			TrySheathWeapons(_targetAgent);
		}
		catch
		{
		}
	}

	private void LockAgentAndMountInPlace(Agent agent, Vec3 forward, Vec3? anchor)
	{
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null || agent == null || !agent.IsActive())
		{
			return;
		}
		Vec3 lookDirection = forward;
		lookDirection.z = 0f;
		if (((Vec3)(ref lookDirection)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref lookDirection))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref lookDirection)).Normalize();
		Vec2 val = ((Vec3)(ref lookDirection)).AsVec2;
		if (((Vec2)(ref val)).LengthSquared < 0.0001f)
		{
			((Vec2)(ref val))._002Ector(1f, 0f);
		}
		val = ((Vec2)(ref val)).Normalized();
		Vec3 val2 = (Vec3)(((_003F?)anchor) ?? agent.Position);
		try
		{
			if ((NativeObject)(object)((MissionBehavior)this).Mission.Scene != (NativeObject)null)
			{
				float z = val2.z;
				if (((MissionBehavior)this).Mission.Scene.GetHeightAtPoint(((Vec3)(ref val2)).AsVec2, (BodyFlags)544321929, ref z))
				{
					val2.z = z;
				}
				else
				{
					val2.z = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(val2, (BodyFlags)544321929);
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
		Vec3 val3;
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
				agent.SetIsAIPaused(true);
				agent.ClearTargetFrame();
				try
				{
					val3 = agent.Position - val2;
					if (((Vec3)(ref val3)).LengthSquared > 0.04f)
					{
						agent.TeleportToPosition(val2);
					}
				}
				catch
				{
				}
				WorldPosition val4 = default(WorldPosition);
				((WorldPosition)(ref val4))._002Ector(((MissionBehavior)this).Mission.Scene, val2);
				agent.SetScriptedPositionAndDirection(ref val4, ((Vec2)(ref val)).RotationInRadians, false, (AIScriptedFrameFlags)18);
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
			mountAgent.SetIsAIPaused(true);
			mountAgent.ClearTargetFrame();
			mountAgent.SetMovementDirection(ref val);
			try
			{
				val3 = mountAgent.Position - val2;
				if (((Vec3)(ref val3)).LengthSquared > 0.04f)
				{
					mountAgent.TeleportToPosition(val2);
				}
			}
			catch
			{
			}
			WorldPosition val5 = default(WorldPosition);
			((WorldPosition)(ref val5))._002Ector(((MissionBehavior)this).Mission.Scene, val2);
			mountAgent.SetScriptedPositionAndDirection(ref val5, ((Vec2)(ref val)).RotationInRadians, false, (AIScriptedFrameFlags)0);
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
		Team val = null;
		try
		{
			val = _targetAgent.Team;
		}
		catch
		{
			val = null;
		}
		if (val != null)
		{
			if (_targetOriginalTeam == null)
			{
				_targetOriginalTeam = val;
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
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		if (_targetAgent == null || !_targetAgent.IsActive())
		{
			return;
		}
		Vec3 forward = default(Vec3);
		((Vec3)(ref forward))._002Ector(1f, 0f, 0f, -1f);
		try
		{
			forward = _targetAgent.LookDirection;
		}
		catch
		{
		}
		try
		{
			if (_hasTargetLockedForward && ((Vec3)(ref _targetLockedForward)).LengthSquared > 0.0001f)
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
				Agent targetAgent = _targetAgent;
				Agent val = ((targetAgent != null) ? targetAgent.MountAgent : null);
				if (val != null && val.IsActive())
				{
					TrySetAgentController(val, "AI");
				}
			}
		}
		catch
		{
		}
		_targetControllerSuppressed = false;
		_targetMountControllerSuppressed = false;
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		((MissionBehavior)this).OnAgentHit(affectedAgent, affectorAgent, ref attackerWeapon, ref blow, ref attackCollisionData);
		TryCapturePreEscalationFatalHitContext(affectedAgent, affectorAgent, in attackerWeapon, in blow);
	}

	private void TrySetAgentController(Agent agent, string controllerType)
	{
		try
		{
			if (agent == null || string.IsNullOrWhiteSpace(controllerType))
			{
				return;
			}
			PropertyInfo propertyInfo = ((object)agent).GetType().GetProperty("Controller") ?? ((object)agent).GetType().GetProperty("ControllerType");
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
				string[] array = names;
				foreach (string text in array)
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

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		((MissionBehavior)this).OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		try
		{
			if (affectedAgent != null)
			{
				_pendingFatalHitContexts.Remove(affectedAgent.Index);
			}
		}
		catch
		{
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		((MissionBehavior)this).OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, ref blow, ref collisionData, damagedHp, hitDistance, shotDifficulty);
		if (MeetingBattleRuntime.IsCombatEscalated || damagedHp <= 0f || affectorAgent == null || affectedAgent == null)
		{
			return;
		}
		Agent val = _mainAgent;
		if (val == null || !val.IsActive())
		{
			try
			{
				Mission mission = ((MissionBehavior)this).Mission;
				val = ((mission != null) ? mission.MainAgent : null);
			}
			catch
			{
				val = null;
			}
		}
		bool flag = val != null && val.IsActive() && affectorAgent == val;
		if (!flag)
		{
			try
			{
				flag = val != null && val.IsActive() && val.MountAgent != null && affectorAgent == val.MountAgent;
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
				Team val2 = ((val != null) ? val.Team : null);
				Team val3 = ((affectorAgent != null) ? affectorAgent.Team : null);
				Team val4 = ((affectedAgent != null) ? affectedAgent.Team : null);
				if (IsUsableTeam(val2) && IsUsableTeam(val3) && IsUsableTeam(val4) && val3 != val4)
				{
					bool flag3 = AreTeamsHostileSafely(val3, val4);
					bool flag4 = val3 == val2 || val4 == val2;
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
			Team team = val.Team;
			Team team2 = affectedAgent.Team;
			if (IsUsableTeam(team) && IsUsableTeam(team2) && team != team2)
			{
				flag7 = AreTeamsHostileSafely(team2, team);
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
				flag8 = affectedAgent != val && (((val != null) ? val.MountAgent : null) == null || affectedAgent != val.MountAgent);
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

	private void TryCapturePreEscalationFatalHitContext(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow)
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (affectedAgent == null || !affectedAgent.IsHuman)
		{
			return;
		}
		try
		{
			if (DuelBehavior.IsFormalDuelActive)
			{
				return;
			}
		}
		catch
		{
		}
		if (!MeetingBattleRuntime.IsMeetingActive || _meetingCombatUnlockApplied)
		{
			return;
		}
		Agent val = NormalizeDamageAffector(affectorAgent);
		if (val == null || val == affectedAgent)
		{
			return;
		}
		float num = 0f;
		try
		{
			num = affectedAgent.Health - (float)blow.InflictedDamage;
		}
		catch
		{
			return;
		}
		if (num >= 1f)
		{
			_pendingFatalHitContexts.Remove(affectedAgent.Index);
			return;
		}
		WeaponComponentData val2 = null;
		try
		{
			MissionWeapon val3 = attackerWeapon;
			val2 = ((MissionWeapon)(ref val3)).CurrentUsageItem;
		}
		catch
		{
			val2 = null;
		}
		_pendingFatalHitContexts[affectedAgent.Index] = new PendingFatalHitContext
		{
			DamageType = blow.DamageType,
			CanDamageKillEvenIfBlunt = (val2 != null && Extensions.HasAnyFlag<WeaponFlags>(val2.WeaponFlags, (WeaponFlags)17179869184L)),
			VictimParty = ResolveAgentParty(affectedAgent),
			EnemyParty = ResolveAgentParty(val)
		};
	}

	private bool TryUseMeetingNaturalDefeatState(Agent effectedAgent, out AgentState result)
	{
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		result = (AgentState)3;
		if (effectedAgent == null || !effectedAgent.IsHuman)
		{
			return false;
		}
		try
		{
			if (DuelBehavior.IsFormalDuelActive)
			{
				return false;
			}
		}
		catch
		{
		}
		if (!MeetingBattleRuntime.IsMeetingActive || _meetingCombatUnlockApplied)
		{
			return false;
		}
		Agent val = _mainAgent;
		if (val == null || !val.IsActive())
		{
			try
			{
				Mission mission = ((MissionBehavior)this).Mission;
				val = ((mission != null) ? mission.MainAgent : null) ?? Agent.Main;
			}
			catch
			{
				val = null;
			}
		}
		if (val != null)
		{
			try
			{
				if (effectedAgent == val || effectedAgent == val.MountAgent)
				{
					return false;
				}
			}
			catch
			{
			}
		}
		if (!_pendingFatalHitContexts.TryGetValue(effectedAgent.Index, out var value))
		{
			return false;
		}
		_pendingFatalHitContexts.Remove(effectedAgent.Index);
		BasicCharacterObject character = effectedAgent.Character;
		CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		if (val2 == null)
		{
			return false;
		}
		PartyBase val3 = value.VictimParty ?? ResolveAgentParty(effectedAgent);
		PartyBase val4 = value.EnemyParty;
		if (val3 == null && _targetAgent != null && effectedAgent == _targetAgent)
		{
			try
			{
				Hero targetHero = _targetHero;
				object obj4;
				if (targetHero == null)
				{
					obj4 = null;
				}
				else
				{
					MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
					obj4 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				if (obj4 == null)
				{
					obj4 = PlayerEncounter.EncounteredParty;
				}
				val3 = (PartyBase)obj4;
			}
			catch
			{
				Hero targetHero2 = _targetHero;
				object obj6;
				if (targetHero2 == null)
				{
					obj6 = null;
				}
				else
				{
					MobileParty partyBelongedTo2 = targetHero2.PartyBelongedTo;
					obj6 = ((partyBelongedTo2 != null) ? partyBelongedTo2.Party : null);
				}
				val3 = (PartyBase)obj6;
			}
		}
		if (val4 == null && val != null)
		{
			val4 = ResolveAgentParty(val);
		}
		if (val3 == null)
		{
			Logger.Log("MeetingBattle", "Pre-escalation natural defeat fallback skipped because victim party is missing. Victim=" + effectedAgent.Name);
			return false;
		}
		float num = 1f - Campaign.Current.Models.PartyHealingModel.GetSurvivalChance(val3, val2, value.DamageType, value.CanDamageKillEvenIfBlunt, val4);
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		result = (AgentState)((MBRandom.RandomFloat <= num) ? 4 : 3);
		Hero heroObject = val2.HeroObject;
		if (heroObject != null)
		{
			Logger.Log("MeetingBattle", $"Applied pre-escalation natural defeat state. Victim={heroObject.Name}, DeathChance={num:0.###}, Result={result}, VictimParty={val3.Name}");
		}
		return true;
	}

	private Agent NormalizeDamageAffector(Agent affectorAgent)
	{
		if (affectorAgent == null)
		{
			return null;
		}
		try
		{
			if (affectorAgent.IsMount && affectorAgent.RiderAgent != null)
			{
				return affectorAgent.RiderAgent;
			}
		}
		catch
		{
		}
		return affectorAgent;
	}

	private PartyBase ResolveAgentParty(Agent agent)
	{
		if (agent == null)
		{
			return null;
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			if (((component != null) ? component.OwnerParty : null) != null)
			{
				return component.OwnerParty;
			}
		}
		catch
		{
		}
		try
		{
			IAgentOriginBase origin = agent.Origin;
			IBattleCombatant obj2 = ((origin != null) ? origin.BattleCombatant : null);
			PartyBase val = (PartyBase)(object)((obj2 is PartyBase) ? obj2 : null);
			if (val != null)
			{
				return val;
			}
		}
		catch
		{
		}
		try
		{
			BasicCharacterObject character = agent.Character;
			BasicCharacterObject obj4 = ((character is CharacterObject) ? character : null);
			Hero val2 = ((obj4 != null) ? ((CharacterObject)obj4).HeroObject : null);
			object obj5;
			if (val2 == null)
			{
				obj5 = null;
			}
			else
			{
				MobileParty partyBelongedTo = val2.PartyBelongedTo;
				obj5 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
			}
			if (obj5 != null)
			{
				return val2.PartyBelongedTo.Party;
			}
		}
		catch
		{
		}
		try
		{
			if (agent == _mainAgent || agent.IsMainAgent)
			{
				return PartyBase.MainParty;
			}
		}
		catch
		{
		}
		try
		{
			if (_targetAgent != null && agent == _targetAgent)
			{
				Hero targetHero = _targetHero;
				object obj8;
				if (targetHero == null)
				{
					obj8 = null;
				}
				else
				{
					MobileParty partyBelongedTo2 = targetHero.PartyBelongedTo;
					obj8 = ((partyBelongedTo2 != null) ? partyBelongedTo2.Party : null);
				}
				if (obj8 == null)
				{
					obj8 = PlayerEncounter.EncounteredParty;
				}
				return (PartyBase)obj8;
			}
		}
		catch
		{
		}
		return null;
	}

	private void TryNotifySameFactionAttackWarning(Agent affectedAgent)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		if (_sameFactionAttackWarningShown || !_sameMapFactionAtEncounterStart || affectedAgent == null || !affectedAgent.IsActive())
		{
			return;
		}
		try
		{
			object obj = _mainAgent;
			if (obj == null)
			{
				Mission mission = ((MissionBehavior)this).Mission;
				obj = ((mission != null) ? mission.MainAgent : null);
			}
			Agent val = (Agent)obj;
			if (val != null && (affectedAgent == val || (val.MountAgent != null && affectedAgent == val.MountAgent)))
			{
				return;
			}
		}
		catch
		{
		}
		TextObject val2 = new TextObject("背叛是不可饶恕的", (Dictionary<string, object>)null);
		try
		{
			MBInformationManager.AddQuickInformation(val2, 0, (BasicCharacterObject)null, (Equipment)null, "");
		}
		catch
		{
		}
		_sameFactionAttackWarningShown = true;
	}

	private void TryApplyEncounterHostilityForEscalatedCombat()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (_encounterHostilityApplied || !MeetingBattleRuntime.IsCombatEscalated)
		{
			return;
		}
		PartyBase val = null;
		try
		{
			MapEvent obj = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle ?? MapEvent.PlayerMapEvent;
			val = ((obj != null) ? obj.GetLeaderParty(PartyBase.MainParty.OpponentSide) : null);
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			try
			{
				val = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				val = null;
			}
		}
		if (val == null)
		{
			try
			{
				Hero targetHero = _targetHero;
				object obj4;
				if (targetHero == null)
				{
					obj4 = null;
				}
				else
				{
					MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
					obj4 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				val = (PartyBase)obj4;
			}
			catch
			{
				val = null;
			}
		}
		if (val == null)
		{
			return;
		}
		try
		{
			LordEncounterBehavior.TryApplyImmediateEscalationConsequences(val, _targetHero, "meeting_combat_escalated_runtime");
		}
		finally
		{
			_encounterHostilityApplied = true;
		}
	}

	private void PauseAllAIAgentsAndSheathWeapons(bool sheathWeapons)
	{
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Agent val = null;
			Agent val2 = null;
			Agent val3 = null;
			Agent val4 = null;
			try
			{
				val = _mainAgent;
				if (val == null || !val.IsActive())
				{
					Mission mission = ((MissionBehavior)this).Mission;
					val = ((mission != null) ? mission.MainAgent : null);
				}
				if (val != null && val.IsActive())
				{
					val2 = val.MountAgent;
				}
				val3 = _targetAgent;
				if (val3 != null && val3.IsActive())
				{
					val4 = val3.MountAgent;
				}
			}
			catch
			{
			}
			EnsureMountedHardLocks();
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item == null || !item.IsActive())
				{
					continue;
				}
				bool flag = item == val || item == val2;
				if (!flag)
				{
					try
					{
						flag = item.IsMainAgent;
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
						Agent riderAgent = item.RiderAgent;
						flag = riderAgent != null && (riderAgent == val || riderAgent.IsMainAgent);
					}
					catch
					{
					}
				}
				if (flag)
				{
					EnsureAgentFreeMovement(item);
					continue;
				}
				bool flag2 = false;
				if (_allowTargetFreeMovementAfterFormalDuel)
				{
					flag2 = item == val3 || item == val4;
					if (!flag2)
					{
						try
						{
							Agent riderAgent2 = item.RiderAgent;
							flag2 = riderAgent2 != null && riderAgent2 == val3;
						}
						catch
						{
						}
					}
				}
				if (flag2)
				{
					EnsureAgentFreeMovement(item);
				}
				else
				{
					if (IsMountedHardLockMount(item))
					{
						continue;
					}
					if (TryGetMountedHardLock(item, out var forward, out var anchor))
					{
						ApplyMountedHardLock(item, forward, anchor, sheathWeapons);
						continue;
					}
					DetachAgentFromFormationForMeetingLock(item);
					try
					{
						if (item.IsAIControlled)
						{
							item.SetIsAIPaused(true);
							item.ClearTargetFrame();
							Agent val5 = null;
							try
							{
								val5 = item.MountAgent;
							}
							catch
							{
								val5 = null;
							}
							if (val5 != null && val5.IsActive())
							{
								try
								{
									item.DisableScriptedMovement();
								}
								catch
								{
								}
								ForgetMeetingLockAnchor(item);
							}
							else
							{
								TryLockAgentToCurrentPosition(item);
							}
						}
					}
					catch
					{
					}
					if (sheathWeapons && item.IsHuman)
					{
						TrySheathWeapons(item);
					}
					try
					{
						Agent mountAgent = item.MountAgent;
						if (mountAgent == null || !mountAgent.IsActive())
						{
							continue;
						}
						bool flag3 = mountAgent == val2;
						if (!flag3)
						{
							try
							{
								Agent riderAgent3 = mountAgent.RiderAgent;
								flag3 = riderAgent3 != null && (riderAgent3 == val || riderAgent3.IsMainAgent);
							}
							catch
							{
							}
						}
						if (flag3)
						{
							EnsureAgentFreeMovement(mountAgent);
							continue;
						}
						if (_allowTargetFreeMovementAfterFormalDuel && (mountAgent == val4 || mountAgent == val3))
						{
							EnsureAgentFreeMovement(mountAgent);
							continue;
						}
						mountAgent.SetIsAIPaused(true);
						mountAgent.ClearTargetFrame();
						try
						{
							Vec3 position = mountAgent.Position;
							mountAgent.SetTargetPosition(((Vec3)(ref position)).AsVec2);
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
			}
			FreezeAllFormationsForMeeting();
		}
		catch
		{
		}
	}

	private void EnsureMainAgentFreeMovement()
	{
		Agent val = null;
		try
		{
			val = _mainAgent;
		}
		catch
		{
			val = null;
		}
		if (val == null || !val.IsActive())
		{
			try
			{
				Mission mission = ((MissionBehavior)this).Mission;
				val = ((mission != null) ? mission.MainAgent : null);
			}
			catch
			{
				val = null;
			}
		}
		if (val == null || !val.IsActive())
		{
			try
			{
				val = Agent.Main;
			}
			catch
			{
				val = null;
			}
		}
		if (val == null || !val.IsActive())
		{
			return;
		}
		_mainAgent = val;
		TryEnsureMainAgentPlayerController(val);
		EnsureAgentFreeMovement(val);
		try
		{
			Agent mountAgent = val.MountAgent;
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
			agent.SetIsAIPaused(false);
		}
		catch
		{
		}
		TryRestoreDetachedFormationWhenSafe(agent);
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
			main.SetIsAIPaused(false);
		}
		catch
		{
		}
	}

	private void TryLockAgentToCurrentPosition(Agent agent, bool recaptureMeetingAnchor = false, bool preserveFacing = false)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null || agent == null || !agent.IsActive() || !TryGetMeetingLockAnchor(agent, recaptureMeetingAnchor, out var position, out var direction))
		{
			return;
		}
		float num = 0f;
		if (preserveFacing)
		{
			Vec3 lookDirection = default(Vec3);
			((Vec3)(ref lookDirection))._002Ector(direction.x, direction.y, 0f, -1f);
			try
			{
				agent.LookDirection = lookDirection;
			}
			catch
			{
			}
			try
			{
				agent.SetMovementDirection(ref direction);
			}
			catch
			{
			}
			num = ((Vec2)(ref direction)).RotationInRadians;
		}
		else
		{
			Vec2 val = Vec2.Zero;
			try
			{
				Vec3 lookDirection2 = agent.LookDirection;
				val = ((Vec3)(ref lookDirection2)).AsVec2;
			}
			catch
			{
				val = Vec2.Zero;
			}
			if (((Vec2)(ref val)).LengthSquared < 0.0001f)
			{
				val = direction;
			}
			Vec2 val2 = ((Vec2)(ref val)).Normalized();
			num = ((Vec2)(ref val2)).RotationInRadians;
		}
		try
		{
			if ((NativeObject)(object)((MissionBehavior)this).Mission.Scene != (NativeObject)null)
			{
				float z = position.z;
				if (((MissionBehavior)this).Mission.Scene.GetHeightAtPoint(((Vec3)(ref position)).AsVec2, (BodyFlags)544321929, ref z))
				{
					position.z = z;
				}
				else
				{
					position.z = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(position, (BodyFlags)544321929);
				}
			}
		}
		catch
		{
		}
		try
		{
			WorldPosition val3 = default(WorldPosition);
			((WorldPosition)(ref val3))._002Ector(((MissionBehavior)this).Mission.Scene, position);
			agent.SetScriptedPositionAndDirection(ref val3, num, false, (AIScriptedFrameFlags)18);
		}
		catch
		{
		}
	}

	private void TryReapplyMeetingLockForAgent(Agent agent, bool recaptureAnchor, bool preserveFacing)
	{
		if (((MissionBehavior)this).Mission == null || agent == null || !agent.IsActive() || !MeetingBattleRuntime.IsMeetingActive || MeetingBattleRuntime.IsCombatEscalated || agent == _mainAgent)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = DuelBehavior.IsFormalDuelActive;
		}
		catch
		{
			flag = false;
		}
		if (flag && agent == _targetAgent)
		{
			return;
		}
		try
		{
			TrySetAgentController(agent, "None");
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
		try
		{
			TryLockAgentToCurrentPosition(agent, recaptureAnchor, preserveFacing);
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
				TryLockAgentToCurrentPosition(mountAgent, recaptureAnchor, preserveFacing);
			}
		}
		catch
		{
		}
	}

	private void FreezeAllFormationsForMeeting()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			foreach (Team item in (List<Team>)(object)((MissionBehavior)this).Mission.Teams)
			{
				if (item == null)
				{
					continue;
				}
				foreach (Formation item2 in (List<Formation>)(object)item.FormationsIncludingEmpty)
				{
					if (item2 != null)
					{
						try
						{
							item2.SetMovementOrder(MovementOrder.MovementOrderStop);
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
		_deferredDetachedFormationRestoreActive = false;
		_deferredDetachedFormationRestoreApplied = false;
		_deferredDetachedFormationRestoreEarliestTime = 0f;
	}

	private void DetachAgentFromFormationForMeetingLock(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		try
		{
			_meetingDetachedFormations.Remove(agent.Index);
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
		if (!_meetingDetachedFormations.TryGetValue(index, out var value))
		{
			return;
		}
		try
		{
			if (value != null && agent.IsActive())
			{
				agent.Formation = value;
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

	private void TryRestoreDetachedFormationWhenSafe(Agent agent)
	{
		if (agent != null && (!_deferredDetachedFormationRestoreActive || _deferredDetachedFormationRestoreApplied))
		{
			RestoreDetachedFormation(agent);
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
			if (((MissionBehavior)this).Mission == null)
			{
				return;
			}
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				RestoreDetachedFormation(item);
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

	private void ArmDeferredDetachedFormationRestoreForCombat()
	{
		if (((MissionBehavior)this).Mission != null && _meetingDetachedFormations.Count != 0 && !_deferredDetachedFormationRestoreApplied && !_deferredDetachedFormationRestoreActive)
		{
			_deferredDetachedFormationRestoreActive = true;
			_deferredDetachedFormationRestoreEarliestTime = ((MissionBehavior)this).Mission.CurrentTime + 0.3f;
			Logger.Log("MeetingBattle", $"Deferred detached formation restore armed. Count={_meetingDetachedFormations.Count}, EarliestTime={_deferredDetachedFormationRestoreEarliestTime:0.00}");
		}
	}

	private void TryRestoreDeferredDetachedFormationsAfterCombat()
	{
		if (_deferredDetachedFormationRestoreActive && !_deferredDetachedFormationRestoreApplied && ((MissionBehavior)this).Mission != null)
		{
			if (_meetingDetachedFormations.Count == 0)
			{
				_deferredDetachedFormationRestoreActive = false;
				_deferredDetachedFormationRestoreApplied = true;
				_deferredDetachedFormationRestoreEarliestTime = 0f;
			}
			else if (!(((MissionBehavior)this).Mission.CurrentTime < _deferredDetachedFormationRestoreEarliestTime))
			{
				int count = _meetingDetachedFormations.Count;
				RestoreAllDetachedFormations();
				_deferredDetachedFormationRestoreActive = false;
				_deferredDetachedFormationRestoreApplied = true;
				_deferredDetachedFormationRestoreEarliestTime = 0f;
				Logger.Log("MeetingBattle", $"Deferred detached formation restore applied. RestoredAgents={count}");
			}
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
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
			if (((Vec3)(ref lookDirection)).LengthSquared < 0.0001f)
			{
				((Vec3)(ref lookDirection))._002Ector(1f, 0f, 0f, -1f);
			}
			((Vec3)(ref lookDirection)).Normalize();
			direction = ((Vec3)(ref lookDirection)).AsVec2;
			if (((Vec2)(ref direction)).LengthSquared < 0.0001f)
			{
				direction = new Vec2(1f, 0f);
			}
			direction = ((Vec2)(ref direction)).Normalized();
			_meetingLockPositions[index] = position;
			_meetingLockDirections[index] = direction;
			return true;
		}
		if (!_meetingLockDirections.TryGetValue(index, out direction) || ((Vec2)(ref direction)).LengthSquared < 0.0001f)
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
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item == null || !item.IsActive())
				{
					continue;
				}
				try
				{
					if (item.IsAIControlled)
					{
						item.DisableScriptedMovement();
						item.ClearTargetFrame();
						item.SetIsAIPaused(false);
						TryRestoreDetachedFormationWhenSafe(item);
					}
				}
				catch
				{
				}
				try
				{
					Agent mountAgent = item.MountAgent;
					if (mountAgent != null && mountAgent.IsActive())
					{
						mountAgent.DisableScriptedMovement();
						mountAgent.ClearTargetFrame();
						mountAgent.SetIsAIPaused(false);
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
		if (!_deferredDetachedFormationRestoreActive || _deferredDetachedFormationRestoreApplied)
		{
			RestoreAllDetachedFormations();
		}
		ClearMeetingLockAnchors();
		ClearMeetingMountedHardLocks();
	}

	private void ReleaseMeetingLocksForCombat()
	{
		if (((MissionBehavior)this).Mission == null)
		{
			return;
		}
		Agent val = null;
		Agent val2 = null;
		try
		{
			val = _mainAgent;
			if (val == null || !val.IsActive())
			{
				val = ((MissionBehavior)this).Mission.MainAgent;
			}
			if (val != null && val.IsActive())
			{
				val2 = val.MountAgent;
			}
		}
		catch
		{
		}
		if (val != null && val.IsActive())
		{
			TryEnsureMainAgentPlayerController(val);
			EnsureAgentFreeMovement(val);
		}
		if (val2 != null && val2.IsActive())
		{
			EnsureAgentFreeMovement(val2);
		}
		int num = 0;
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsActive() && item != val && item != val2)
				{
					if (ReleaseSingleAgentFromMeetingLock(item))
					{
						num++;
					}
					Agent val3 = null;
					try
					{
						val3 = item.MountAgent;
					}
					catch
					{
						val3 = null;
					}
					if (val3 != null && val3.IsActive() && val3 != val && val3 != val2 && ReleaseSingleAgentFromMeetingLock(val3))
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
			agent.SetIsAIPaused(false);
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
					agent.SetMovementDirection(ref Vec2.Zero);
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
		TryRestoreDetachedFormationWhenSafe(agent);
		ForgetMeetingLockAnchor(agent);
		return result;
	}

	private void ApplyFrame(Agent agent, MatrixFrame frame)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		Vec3 origin = frame.origin;
		try
		{
			Mission mission = ((MissionBehavior)this).Mission;
			if ((NativeObject)(object)((mission != null) ? mission.Scene : null) != (NativeObject)null)
			{
				float z = origin.z;
				if (((MissionBehavior)this).Mission.Scene.GetHeightAtPoint(((Vec3)(ref origin)).AsVec2, (BodyFlags)544321929, ref z))
				{
					origin.z = z;
				}
				else
				{
					origin.z = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(origin, (BodyFlags)544321929);
				}
			}
		}
		catch
		{
		}
		Vec3 f = frame.rotation.f;
		f.z = 0f;
		if (((Vec3)(ref f)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref f))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref f)).Normalize();
		try
		{
			agent.TeleportToPosition(origin);
		}
		catch
		{
		}
		try
		{
			agent.LookDirection = f;
		}
		catch
		{
		}
		try
		{
			if (agent.IsAIControlled)
			{
				agent.SetIsAIPaused(true);
				agent.ClearTargetFrame();
				agent.SetTargetPosition(((Vec3)(ref origin)).AsVec2);
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
			agent.MountAgent.LookDirection = f;
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
					agent.MountAgent.SetIsAIPaused(false);
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
				agent.MountAgent.SetIsAIPaused(true);
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
				Agent mountAgent = agent.MountAgent;
				Vec3 position = agent.MountAgent.Position;
				mountAgent.SetTargetPosition(((Vec3)(ref position)).AsVec2);
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
			agent.TryToSheathWeaponInHand((HandIndex)1, (WeaponWieldActionType)1);
		}
		catch
		{
		}
		try
		{
			agent.TryToSheathWeaponInHand((HandIndex)0, (WeaponWieldActionType)1);
		}
		catch
		{
		}
	}

	private bool TryPlaceEscortGuards()
	{
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		if (_mainAgent == null || _targetAgent == null || !_mainAgent.IsActive() || !_targetAgent.IsActive())
		{
			return false;
		}
		Team val = null;
		Team val2 = null;
		try
		{
			val = _mainAgent.Team;
		}
		catch
		{
		}
		try
		{
			val2 = _targetAgent.Team;
		}
		catch
		{
		}
		if (val == null || val2 == null)
		{
			return false;
		}
		if (val == val2)
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
		List<Agent> list2 = CollectTopTierTeamAgents(val, list.Count);
		List<Agent> list3 = CollectTopTierTeamAgents(val2, list.Count);
		Vec3 val3 = _targetAgent.Position - _mainAgent.Position;
		val3.z = 0f;
		if (((Vec3)(ref val3)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref val3))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref val3)).Normalize();
		Vec3 val4 = _mainAgent.Position - _targetAgent.Position;
		val4.z = 0f;
		if (((Vec3)(ref val4)).LengthSquared < 0.0001f)
		{
			val4 = -val3;
		}
		((Vec3)(ref val4)).Normalize();
		PartyBase primaryParty = null;
		PartyBase primaryParty2 = null;
		try
		{
			primaryParty = PartyBase.MainParty;
		}
		catch
		{
		}
		try
		{
			Hero targetHero = _targetHero;
			object obj4;
			if (targetHero == null)
			{
				obj4 = null;
			}
			else
			{
				MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
				obj4 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
			}
			primaryParty2 = (PartyBase)obj4;
		}
		catch
		{
		}
		bool flag = ShouldRequireEscortForSide(val, primaryParty);
		bool flag2 = ShouldRequireEscortForSide(val2, primaryParty2);
		bool flag3 = flag && list2.Count < list.Count;
		bool flag4 = flag2 && list3.Count < list.Count;
		if (flag3 || flag4)
		{
			TrySpawnFallbackEscortsForBothSides(list.Count, val, val2, _mainAgent.Position, val3, _targetAgent.Position, val4, list2.Count, list3.Count, flag3, flag4);
			list2 = CollectTopTierTeamAgents(val, list.Count);
			list3 = CollectTopTierTeamAgents(val2, list.Count);
		}
		if (list2.Count > 0 || list3.Count > 0)
		{
			ConfigureMeetingHoldFormations(list2.FirstOrDefault(), list3.FirstOrDefault());
		}
		if (list2.Count > 0)
		{
			PositionEscortAgents(_mainAgent.Position, val3, list2, list);
		}
		if (list3.Count > 0)
		{
			PositionEscortAgents(_targetAgent.Position, val4, list3, list);
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
		if (team == null || ((MissionBehavior)this).Mission == null)
		{
			return 0;
		}
		int num = 0;
		Agent val = null;
		Agent val2 = null;
		try
		{
			Agent mainAgent = _mainAgent;
			val = ((mainAgent != null) ? mainAgent.MountAgent : null);
		}
		catch
		{
		}
		try
		{
			Agent targetAgent = _targetAgent;
			val2 = ((targetAgent != null) ? targetAgent.MountAgent : null);
		}
		catch
		{
		}
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item == null || !item.IsActive() || !item.IsHuman || item == _mainAgent || item == _targetAgent || item == val || item == val2)
				{
					continue;
				}
				Team val3 = null;
				try
				{
					val3 = item.Team;
				}
				catch
				{
					val3 = null;
				}
				if (val3 != null && val3 == team)
				{
					CharacterObject val4 = null;
					try
					{
						BasicCharacterObject character = item.Character;
						val4 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
					}
					catch
					{
						val4 = null;
					}
					if (val4 != null && !((BasicCharacterObject)val4).IsHero)
					{
						num++;
					}
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
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
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
			troopCount = (((int)side == 1) ? currentEncounterBattleSafe.AttackerSide.TroopCount : currentEncounterBattleSafe.DefenderSide.TroopCount);
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
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected I4, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected I4, but got Unknown
		side = (BattleSideEnum)(-1);
		if (team == null)
		{
			return false;
		}
		try
		{
			object obj = (((object)team).GetType().GetProperty("Side") ?? ((object)team).GetType().GetProperty("BattleSide") ?? ((object)team).GetType().GetProperty("MissionSide"))?.GetValue(team, null);
			if (obj == null)
			{
				return false;
			}
			if (obj is BattleSideEnum val)
			{
				side = (BattleSideEnum)(int)val;
				return (int)side != -1;
			}
			if (Enum.TryParse<BattleSideEnum>(obj.ToString(), ignoreCase: true, out BattleSideEnum result))
			{
				side = (BattleSideEnum)(int)result;
				return (int)side != -1;
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
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PartyBase mainParty = PartyBase.MainParty;
			Hero targetHero = _targetHero;
			object obj;
			if (targetHero == null)
			{
				obj = null;
			}
			else
			{
				MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
				obj = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
			}
			PartyBase val = (PartyBase)obj;
			if (mainParty == null || val == null || playerTeam == null || targetTeam == null)
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
			int num7 = CountHealthyNonHeroTroops(val);
			if (allowTargetSpawn && num6 > 0 && num7 > 0)
			{
				int num8 = Math.Min(num6, num7);
				if (num8 > 0)
				{
					List<CharacterObject> list2 = CollectTopTroopsFromParty(val, num8);
					if (list2.Count > 0)
					{
						num2 = SpawnEscortAgentsFromTroops(val, targetTeam, targetAnchor, targetForward, list2);
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
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		if (party == null)
		{
			return 0;
		}
		try
		{
			foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)party.MemberRoster.GetTroopRoster())
			{
				TroopRosterElement current = item;
				CharacterObject character = current.Character;
				if (character != null && !((BasicCharacterObject)character).IsHero)
				{
					int num2 = ((TroopRosterElement)(ref current)).Number - ((TroopRosterElement)(ref current)).WoundedNumber;
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
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		List<CharacterObject> list = new List<CharacterObject>();
		if (party == null || maxCount <= 0)
		{
			return list;
		}
		List<(CharacterObject, int, int, int)> list2 = new List<(CharacterObject, int, int, int)>();
		try
		{
			foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)party.MemberRoster.GetTroopRoster())
			{
				TroopRosterElement current = item;
				CharacterObject character = current.Character;
				if (character != null && !((BasicCharacterObject)character).IsHero)
				{
					int num = ((TroopRosterElement)(ref current)).Number - ((TroopRosterElement)(ref current)).WoundedNumber;
					if (num > 0)
					{
						list2.Add((character, character.Tier, ((BasicCharacterObject)character).Level, num));
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
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Expected O, but got Unknown
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null || party == null || team == null || troops == null || troops.Count == 0)
		{
			return 0;
		}
		int num = 0;
		Vec3 val = forward;
		val.z = 0f;
		if (((Vec3)(ref val)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref val))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref val)).Normalize();
		Vec3 side = default(Vec3);
		((Vec3)(ref side))._002Ector(0f - val.y, val.x, 0f, -1f);
		if (((Vec3)(ref side)).LengthSquared < 0.0001f)
		{
			side = Vec3.Side;
		}
		((Vec3)(ref side)).Normalize();
		for (int i = 0; i < troops.Count; i++)
		{
			CharacterObject val2 = troops[i];
			if (val2 == null)
			{
				continue;
			}
			Vec3 val3 = anchor + side * (((i % 2 == 0) ? 1f : (-1f)) * (3f + (float)(i / 2) * 0.8f)) - val * (3f + (float)(i / 2) * 0.5f);
			try
			{
				if ((NativeObject)(object)((MissionBehavior)this).Mission.Scene != (NativeObject)null)
				{
					float z = val3.z;
					if (((MissionBehavior)this).Mission.Scene.GetHeightAtPoint(((Vec3)(ref val3)).AsVec2, (BodyFlags)544321929, ref z))
					{
						val3.z = z;
					}
					else
					{
						val3.z = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(val3, (BodyFlags)544321929);
					}
				}
			}
			catch
			{
			}
			bool flag = !CharacterPrefersMountedEscort(val2);
			AgentBuildData obj2 = new AgentBuildData((BasicCharacterObject)(object)val2).TroopOrigin((IAgentOriginBase)new PartyAgentOrigin(party, val2, -1, default(UniqueTroopDescriptor), false, false)).Monster(FaceGen.GetMonsterWithSuffix(((BasicCharacterObject)val2).Race, "_settlement")).Team(team)
				.InitialPosition(ref val3);
			Vec2 val4 = ((Vec3)(ref val)).AsVec2;
			val4 = ((Vec2)(ref val4)).Normalized();
			AgentBuildData val5 = obj2.InitialDirection(ref val4).Controller((AgentControllerType)1).CivilianEquipment(false)
				.NoHorses(flag);
			Agent val6 = null;
			try
			{
				val6 = ((MissionBehavior)this).Mission.SpawnAgent(val5, false);
			}
			catch
			{
				val6 = null;
			}
			if (val6 != null)
			{
				try
				{
					val6.SetIsAIPaused(true);
				}
				catch
				{
				}
				try
				{
					val6.ClearTargetFrame();
				}
				catch
				{
				}
				try
				{
					Agent obj6 = val6;
					Vec3 position = val6.Position;
					obj6.SetTargetPosition(((Vec3)(ref position)).AsVec2);
				}
				catch
				{
				}
				try
				{
					val6.LookDirection = val;
				}
				catch
				{
				}
				try
				{
					TrySheathWeapons(val6);
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
		if (team == null || maxCount <= 0 || ((MissionBehavior)this).Mission == null)
		{
			return list;
		}
		Agent val = null;
		Agent val2 = null;
		try
		{
			Agent mainAgent = _mainAgent;
			val = ((mainAgent != null) ? mainAgent.MountAgent : null);
		}
		catch
		{
		}
		try
		{
			Agent targetAgent = _targetAgent;
			val2 = ((targetAgent != null) ? targetAgent.MountAgent : null);
		}
		catch
		{
		}
		try
		{
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item == null || !item.IsActive() || !item.IsHuman || item == _mainAgent || item == _targetAgent || item == val || item == val2)
				{
					continue;
				}
				Team val3 = null;
				try
				{
					val3 = item.Team;
				}
				catch
				{
				}
				if (val3 != null && val3 == team)
				{
					CharacterObject val4 = null;
					try
					{
						BasicCharacterObject character = item.Character;
						val4 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
					}
					catch
					{
					}
					if (val4 != null && !((BasicCharacterObject)val4).IsHero)
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		return list.OrderByDescending(GetAgentEscortPriority).ThenByDescending(GetAgentTier).ThenByDescending(GetAgentLevel)
			.Take(maxCount)
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
			BasicCharacterObject character = agent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			if (val != null)
			{
				return GetCharacterEscortPriority(val);
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
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if (character == null)
		{
			return false;
		}
		try
		{
			if (((BasicCharacterObject)character).IsMounted)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			FormationClass defaultFormationClass = ((BasicCharacterObject)character).DefaultFormationClass;
			return (int)defaultFormationClass == 2 || (int)defaultFormationClass == 6 || (int)defaultFormationClass == 7 || (int)defaultFormationClass == 3;
		}
		catch
		{
			return false;
		}
	}

	private void TryEquipMeetingEscortWeapons(Agent agent)
	{
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Invalid comparison between Unknown and I4
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		EquipmentIndex? val = null;
		EquipmentIndex? val2 = null;
		EquipmentIndex? val3 = null;
		EquipmentIndex? val4 = null;
		EquipmentIndex? val5 = null;
		int num = int.MinValue;
		int num2 = int.MinValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		EquipmentIndex[] array = new EquipmentIndex[5];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		EquipmentIndex[] array2 = (EquipmentIndex[])(object)array;
		EquipmentIndex[] array3 = array2;
		EquipmentIndex[] array4 = array3;
		MissionWeapon val9;
		foreach (EquipmentIndex val6 in array4)
		{
			ItemObject val7 = null;
			WeaponComponentData val8 = null;
			try
			{
				val9 = agent.Equipment[val6];
				val7 = ((MissionWeapon)(ref val9)).Item;
			}
			catch
			{
				val7 = null;
			}
			if (val7 == null)
			{
				continue;
			}
			try
			{
				val8 = val7.PrimaryWeapon;
			}
			catch
			{
				val8 = null;
			}
			if (val8 == null)
			{
				continue;
			}
			bool flag = false;
			try
			{
				flag = val8.IsShield || (int)val7.Type == 8;
			}
			catch
			{
				flag = false;
			}
			if (flag)
			{
				if (!val.HasValue)
				{
					val = val6;
				}
				continue;
			}
			bool flag2 = false;
			try
			{
				flag2 = Extensions.HasAnyFlag<ItemFlags>(val7.ItemFlags, (ItemFlags)524288);
			}
			catch
			{
				flag2 = false;
			}
			if (!flag2)
			{
				int meetingEscortWeaponScore = GetMeetingEscortWeaponScore(val7);
				bool flag3 = IsMeetingEscortPolearmWeapon(val7);
				bool flag4 = CanUseMeetingEscortWeaponWithShield(val7);
				if (meetingEscortWeaponScore > num4)
				{
					val5 = val6;
					num4 = meetingEscortWeaponScore;
				}
				if (flag4 && meetingEscortWeaponScore > num3)
				{
					val4 = val6;
					num3 = meetingEscortWeaponScore;
				}
				if (flag3 && meetingEscortWeaponScore > num2)
				{
					val3 = val6;
					num2 = meetingEscortWeaponScore;
				}
				if (val.HasValue && flag3 && flag4 && meetingEscortWeaponScore > num)
				{
					val2 = val6;
					num = meetingEscortWeaponScore;
				}
			}
		}
		EquipmentIndex? val10 = val2 ?? val3 ?? val4 ?? val5;
		if (!val10.HasValue)
		{
			return;
		}
		try
		{
			agent.TryToWieldWeaponInSlot(val10.Value, (WeaponWieldActionType)1, false);
		}
		catch
		{
		}
		if (val.HasValue)
		{
			try
			{
				val9 = agent.Equipment[val10.Value];
				ItemObject item = ((MissionWeapon)(ref val9)).Item;
				if (CanUseMeetingEscortWeaponWithShield(item))
				{
					agent.TryToWieldWeaponInSlot(val.Value, (WeaponWieldActionType)1, false);
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
				mountAgent.SetIsAIPaused(true);
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
		WeaponComponentData val = null;
		try
		{
			val = itemObject.PrimaryWeapon;
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			return int.MinValue;
		}
		int num = 0;
		bool flag = false;
		try
		{
			flag = val.IsMeleeWeapon;
		}
		catch
		{
			flag = false;
		}
		if (flag)
		{
			num += 500;
		}
		if (IsMeetingEscortPolearmWeapon(itemObject))
		{
			num += 2000;
		}
		if (IsMeetingEscortPreferredPolearm(itemObject))
		{
			num += 1000;
		}
		if (CanUseMeetingEscortWeaponWithShield(itemObject))
		{
			num += 300;
		}
		bool flag2 = false;
		try
		{
			flag2 = val.IsRangedWeapon;
		}
		catch
		{
			flag2 = false;
		}
		if (flag2)
		{
			num -= 800;
		}
		return num;
	}

	private bool IsMeetingEscortPolearmWeapon(ItemObject itemObject)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		if (itemObject == null)
		{
			return false;
		}
		WeaponComponentData val = null;
		try
		{
			val = itemObject.PrimaryWeapon;
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			return false;
		}
		try
		{
			if ((int)itemObject.Type == 4)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			WeaponClass weaponClass = val.WeaponClass;
			if ((int)weaponClass == 9 || (int)weaponClass == 10 || (int)weaponClass == 11 || (int)weaponClass == 23)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return val.IsPolearm;
		}
		catch
		{
		}
		return IsMeetingEscortPreferredPolearm(itemObject);
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
			text = (((MBObjectBase)itemObject).StringId ?? "").ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		try
		{
			text2 = (((object)itemObject.Name)?.ToString() ?? "").ToLowerInvariant();
		}
		catch
		{
			text2 = "";
		}
		return text.Contains("lance") || text.Contains("spear") || text.Contains("pike") || text2.Contains("骑枪") || text2.Contains("长矛") || text2.Contains("长枪") || text2.Contains("矛") || text2.Contains("枪");
	}

	private bool CanUseMeetingEscortWeaponWithShield(ItemObject itemObject)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		if (itemObject == null)
		{
			return false;
		}
		WeaponComponentData val = null;
		try
		{
			val = itemObject.PrimaryWeapon;
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			return false;
		}
		try
		{
			if (val.IsShield || (int)itemObject.Type == 8)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Extensions.HasAnyFlag<ItemFlags>(itemObject.ItemFlags, (ItemFlags)524288))
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (!val.IsOneHanded)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Extensions.HasAnyFlag<WeaponFlags>(val.WeaponFlags, (WeaponFlags)16))
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			ItemUsageSetFlags itemUsageSetFlags = MBItem.GetItemUsageSetFlags(val.ItemUsage);
			if (Extensions.HasAnyFlag<ItemUsageSetFlags>(itemUsageSetFlags, (ItemUsageSetFlags)8))
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
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		if (character == null)
		{
			return false;
		}
		try
		{
			return (int)((BasicCharacterObject)character).DefaultFormationClass == 3;
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
			BasicCharacterObject obj = ((agent != null) ? agent.Character : null);
			CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
			return (val != null) ? val.Tier : 0;
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
			BasicCharacterObject obj = ((agent != null) ? agent.Character : null);
			CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
			return (val != null) ? ((BasicCharacterObject)val).Level : 0;
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
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		if (escorts == null || slots == null || escorts.Count == 0 || slots.Count == 0)
		{
			return;
		}
		Vec3 val = forward;
		val.z = 0f;
		if (((Vec3)(ref val)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref val))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref val)).Normalize();
		Vec3 side = default(Vec3);
		((Vec3)(ref side))._002Ector(0f - val.y, val.x, 0f, -1f);
		if (((Vec3)(ref side)).LengthSquared < 0.0001f)
		{
			side = Vec3.Side;
		}
		((Vec3)(ref side)).Normalize();
		int num = Math.Min(escorts.Count, slots.Count);
		for (int i = 0; i < num; i++)
		{
			Agent val2 = escorts[i];
			if (val2 == null || !val2.IsActive())
			{
				continue;
			}
			(float, float, bool) tuple = slots[i];
			Vec3 val3 = anchor + val * tuple.Item1 + side * tuple.Item2;
			Vec3 lookDirection = (tuple.Item3 ? (-val) : val);
			if (((Vec3)(ref lookDirection)).LengthSquared < 0.0001f)
			{
				lookDirection = val;
			}
			((Vec3)(ref lookDirection)).Normalize();
			try
			{
				Mission mission = ((MissionBehavior)this).Mission;
				if ((NativeObject)(object)((mission != null) ? mission.Scene : null) != (NativeObject)null)
				{
					float z = val3.z;
					if (((MissionBehavior)this).Mission.Scene.GetHeightAtPoint(((Vec3)(ref val3)).AsVec2, (BodyFlags)544321929, ref z))
					{
						val3.z = z;
					}
					else
					{
						val3.z = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(val3, (BodyFlags)544321929);
					}
				}
			}
			catch
			{
			}
			try
			{
				val2.TeleportToPosition(val3);
			}
			catch
			{
			}
			try
			{
				val2.LookDirection = lookDirection;
			}
			catch
			{
			}
			try
			{
				val2.SetIsAIPaused(true);
			}
			catch
			{
			}
			try
			{
				val2.ClearTargetFrame();
			}
			catch
			{
			}
			try
			{
				val2.SetTargetPosition(((Vec3)(ref val3)).AsVec2);
			}
			catch
			{
			}
			try
			{
				TrySheathWeapons(val2);
			}
			catch
			{
			}
			bool flag = IsMeetingFormationManagedAgent(val2);
			if (flag)
			{
				try
				{
					TryEquipMeetingEscortWeapons(val2);
				}
				catch
				{
				}
			}
			bool flag2 = false;
			try
			{
				Agent mountAgent = val2.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					flag2 = true;
					mountAgent.TeleportToPosition(val3);
					mountAgent.LookDirection = lookDirection;
					mountAgent.SetIsAIPaused(true);
					mountAgent.ClearTargetFrame();
					mountAgent.SetTargetPosition(((Vec3)(ref val3)).AsVec2);
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
					TrySetAgentController(val2, "None");
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					TryLockAgentToCurrentPosition(val2, recaptureMeetingAnchor: true, preserveFacing: true);
				}
				catch
				{
				}
			}
		}
	}
}
