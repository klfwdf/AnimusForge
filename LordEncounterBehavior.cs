using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class LordEncounterBehavior : CampaignBehaviorBase
{
	private static Hero _targetHero;

	public static bool IsOpeningConversation = false;

	private const float NativeEncounterAttackDialogDelaySeconds = 5f;

	private const float NativeConversationReleaseDialogDelaySeconds = 10f;

	// Matches SafePassageBarterable.Apply(): a release must give the player enough
	// separation time that the same party cannot immediately re-open the encounter.
	private const int MeetingReleaseSafePassageHours = 32;

	private static bool _encounterMeetingMissionActive;

	private static CampaignVec2 _savedMainPartyPosition;

	private static bool _hasSavedMainPartyPosition;

	private static string _encounterMeetingLocationInfoOverride;

	private static bool _overrideNextPlayerSpawnFrame;

	private static MatrixFrame _nextPlayerSpawnFrame;

	private static bool _preferPreparedPlayerSpawnFrame;

	private static bool _overrideNextTargetHeroSpawnFrame;

	private static MatrixFrame _nextTargetHeroSpawnFrame;

	private static bool _meetingSpawnOverrideActive;

	private static Vec3 _targetHeroSpawnPos = new Vec3(415.722f, 732.8734f, 1.918564f);

	private static Vec3 _targetHeroSpawnForward = new Vec3(0.9261521f, 0.3696325f);

	private static bool _pendingPostMissionCleanup;

	private static float _pendingPostMissionCleanupDelay;

	private static bool _pendingPeacefulMeetingBattleCleanup;

	private static bool _cameraLockWasActive;

	private static bool _meetingPlayerReleaseAuthorized;

	// The custom meeting mission performs an additional encounter cleanup after
	// OnMissionEnded. Keep the release target until that cleanup has finished so
	// its final Finish call cannot restore the party's prior pursuit order.
	private static bool _pendingMeetingReleaseSafePassageFinalReapply;

	private static Hero _pendingMeetingReleaseSafePassageHero;

	private static PartyBase _pendingMeetingReleaseSafePassageParty;

	private static string _pendingMeetingReleaseSafePassageReason;

	private static bool _meetingStartedForProactiveRequest;

	private static Hero _meetingStartedForProactiveRequestHero;

	private static bool _meetingStartedFromCustomEncounterMenu;

	private static Hero _meetingStartedFromCustomEncounterMenuHero;

	private const float PlayerMeetingMinimumHealthRatio = 0.21f;

	private static string _lastLowHealthMeetingBlockedHeroId;

	private static bool _pendingReturnToEncounterMenuAfterUnauthorizedMeetingExit;

	private static bool _suspendEncounterRedirectDuringResultResolution;

	private static float _encounterRedirectSuspendSinceTime = -1f;

	private static float _encounterRedirectSuspendUntilTime = -1f;

	private static Hero _encounterRedirectSuspendedEncounterLeader;

	private static PartyBase _encounterRedirectSuspendedEncounterParty;

	private static bool _lastMeetingWasSameMapFactionConflict;

	private static TextObject _lastMeetingPlayerFactionName = new TextObject("你的势力");

	private static bool _disableCustomEncounterMenuForCurrentEncounter;

	private static float _disableCustomEncounterMenuSinceTime = -1f;

	private static PartyBase _disableCustomEncounterMenuEncounterParty;

	private static bool _suppressCustomEncounterMenuUntilBackOnMap;

	private static float _suppressCustomEncounterMenuStartedAtTime = -1f;

	private static float _suppressCustomEncounterMenuBackOnMapSinceTime = -1f;

	private static string _suppressCustomEncounterMenuReason;

	private static bool _pendingForceNativeDefeatCaptivityMenu;

	private static float _pendingForceNativeDefeatCaptivityMenuAtTime;

	private static float _pendingForceNativeDefeatCaptivityLastAttemptTime = -1f;

	private static Hero _pendingForceNativeDefeatCaptivityHero;

	private static PartyBase _pendingForceNativeDefeatCaptivityParty;

	private static bool _pendingForceNativeDefeatCaptivityPlayerWasAttacker = true;

	private static bool _pendingForceNativeEncounterBattleMenu;

	private static float _pendingForceNativeEncounterBattleMenuAtTime;

	private static float _pendingForceNativeEncounterBattleMenuLastAttemptTime = -1f;

	private static PartyBase _pendingForceNativeEncounterBattleMenuEncounterParty;

	private static Hero _pendingForceNativeEncounterBattleMenuEncounterLeader;

	private static bool _pendingForceNativeEncounterAttack;

	private static float _pendingForceNativeEncounterAttackAtTime;

	private static float _pendingForceNativeEncounterAttackLastAttemptTime = -1f;

	private static bool _pendingForceNativeEncounterAttackDiplomacyApplied;

	private static bool _pendingForceNativeEncounterAttackEndHookRegistered;

	private static bool _pendingForceNativeEncounterAttackConversationEnded;

	private static PartyBase _pendingForceNativeEncounterAttackParty;

	private static Hero _pendingForceNativeEncounterAttackHero;

	private static string _pendingForceNativeEncounterAttackReason;

	private static bool _pendingMeetingBattleNativeResult;

	private static float _pendingMeetingBattleNativeResultAtTime;

	private static float _pendingMeetingBattleNativeResultLastAttemptTime = -1f;

	private static PartyBase _pendingMeetingBattleNativeResultParty;

	private static Hero _pendingMeetingBattleNativeResultHero;

	private static string _pendingMeetingBattleNativeResultReason;

	private static bool _pendingMeetingBattleNativeResultPlayerVictory;

	private static bool _pendingMeetingBattleNativeResultPlayerDefeat;

	private static bool _pendingNativeConversationNpcSurrender;

	private static float _pendingNativeConversationNpcSurrenderAtTime;

	private static float _pendingNativeConversationNpcSurrenderLastAttemptTime = -1f;

	private static Hero _pendingNativeConversationNpcSurrenderHero;

	private static CharacterObject _pendingNativeConversationNpcSurrenderCharacter;

	private static PartyBase _pendingNativeConversationNpcSurrenderParty;

	private static int _pendingNativeConversationNpcSurrenderAgentIndex = -1;

	private static string _pendingNativeConversationNpcSurrenderReason;

	private static bool _pendingNativeConversationMeetingRelease;

	private static float _pendingNativeConversationMeetingReleaseAtTime;

	private static float _pendingNativeConversationMeetingReleaseLastAttemptTime = -1f;

	private static Hero _pendingNativeConversationMeetingReleaseHero;

	private static PartyBase _pendingNativeConversationMeetingReleaseParty;

	private static string _pendingNativeConversationMeetingReleaseReason;

	private static bool _npcSurrenderSkipHeroCaptureConversations;

	private static PartyBase _npcSurrenderSkipEncounterParty;

	private static string _npcSurrenderSkipReason;

	private static bool _nativeSettlementRequestMeetingContextActive;

	private static float _nativeSettlementRequestMeetingContextUntilTime = -1f;

	private static Settlement _nativeSettlementRequestMeetingSettlement;

	private static string _nativeSettlementRequestMeetingMenuId;

	private static readonly Regex MeetingTauntWarnTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_WARN\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MeetingTauntBattleTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_BATTLE\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MeetingReleasePlayerTagRegex = new Regex("\\[ACTION:LET_PLAYER_GO\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static MethodInfo _playerEncounterDoPlayerDefeatMethod;

	private static PropertyInfo _playerEncounterStateProperty;

	private static Type _mapCameraViewType;

	private static PropertyInfo _mapCameraViewInstanceProperty;

	private static MethodInfo _mapCameraViewTeleportToMainPartyMethod;

	internal static bool IsEncounterMeetingMissionActive => _encounterMeetingMissionActive;

	internal static string EncounterMeetingLocationInfoOverride => _encounterMeetingLocationInfoOverride;

	internal static void SetEncounterMeetingMissionActive(bool active)
	{
		_encounterMeetingMissionActive = active;
	}

	private static void AuthorizeMeetingPlayerRelease(string reason)
	{
		_meetingPlayerReleaseAuthorized = true;
		Logger.Log("MeetingRelease", "Meeting player release authorized. Reason=" + (reason ?? "N/A"));
	}

	private static bool ConsumeMeetingPlayerReleaseAuthorization(string reason)
	{
		bool meetingPlayerReleaseAuthorized = _meetingPlayerReleaseAuthorized;
		_meetingPlayerReleaseAuthorized = false;
		Logger.Log("MeetingRelease", $"Meeting player release authorization consumed={meetingPlayerReleaseAuthorized}. Reason={reason ?? "N/A"}");
		return meetingPlayerReleaseAuthorized;
	}

	private static void ClearMeetingPlayerReleaseAuthorization(string reason)
	{
		_meetingPlayerReleaseAuthorized = false;
		Logger.Log("MeetingRelease", "Meeting player release authorization cleared. Reason=" + (reason ?? "N/A"));
	}

	private static void ScheduleMeetingReleaseSafePassageFinalReapply(Hero releasedByHero, string reason)
	{
		PartyBase partyBase = TryGetMeetingReleaseEncounterParty();
		_pendingMeetingReleaseSafePassageFinalReapply = true;
		_pendingMeetingReleaseSafePassageHero = IsNonHeroMeetingReleaseParty(partyBase) ? null : releasedByHero;
		_pendingMeetingReleaseSafePassageParty = partyBase ?? _pendingMeetingReleaseSafePassageHero?.PartyBelongedTo?.Party;
		_pendingMeetingReleaseSafePassageReason = reason;
		Logger.Log("MeetingRelease", "Scheduled safe-passage reapply after final meeting cleanup. Target=" + (_pendingMeetingReleaseSafePassageHero?.StringId ?? "null") + ", Party=" + GetPartyLogName(_pendingMeetingReleaseSafePassageParty) + ", Reason=" + (reason ?? "N/A"));
	}

	private static void ClearMeetingReleaseSafePassageFinalReapply(string reason)
	{
		_pendingMeetingReleaseSafePassageFinalReapply = false;
		_pendingMeetingReleaseSafePassageHero = null;
		_pendingMeetingReleaseSafePassageParty = null;
		_pendingMeetingReleaseSafePassageReason = null;
		Logger.Log("MeetingRelease", "Cleared pending safe-passage reapply. Reason=" + (reason ?? "N/A"));
	}

	private static void MarkPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit(string reason)
	{
		_pendingReturnToEncounterMenuAfterUnauthorizedMeetingExit = true;
		Logger.Log("MeetingRelease", "Pending return to encounter menu after unauthorized meeting exit. Reason=" + (reason ?? "N/A"));
	}

	private static void ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit(string reason)
	{
		_pendingReturnToEncounterMenuAfterUnauthorizedMeetingExit = false;
		Logger.Log("MeetingRelease", "Cleared pending unauthorized meeting exit return. Reason=" + (reason ?? "N/A"));
	}

	internal static bool TryGetSavedMainPartyPosition(out CampaignVec2 pos)
	{
		pos = _savedMainPartyPosition;
		return _hasSavedMainPartyPosition && _savedMainPartyPosition.IsValid();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		ClearNativeSettlementRequestMeetingContext("session_launched");
		AddGameMenus(starter);
		AddConversationOptions(starter);
	}

	private void OnMissionEnded(IMission mission)
	{
		ClearNativeSettlementRequestMeetingContext("mission_ended");
		bool flag = false;
		bool flag12 = false;
		bool flag13 = false;
		bool flag14 = false;
		bool flag15 = false;
		bool flag16 = false;
		bool flag17 = false;
		try
		{
			flag = MeetingBattleRuntime.IsCombatEscalated;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag12 = MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
			flag12 = false;
		}
		try
		{
			flag13 = _encounterMeetingMissionActive;
		}
		catch
		{
			flag13 = false;
		}
		try
		{
			flag14 = HasPendingMeetingBattleNativeResult();
		}
		catch
		{
			flag14 = false;
		}
		try
		{
			flag15 = HasPendingForceNativeEncounterBattleMenu();
		}
		catch
		{
			flag15 = false;
		}
		try
		{
			flag16 = HasPendingForceNativeDefeatCaptivityMenu();
		}
		catch
		{
			flag16 = false;
		}
		bool flag21 = false;
		bool flag22 = false;
		Hero hero4 = null;
		try
		{
			flag21 = _meetingStartedForProactiveRequest;
			flag22 = _meetingStartedFromCustomEncounterMenu;
			if (flag21 || flag22)
			{
				hero4 = _meetingStartedFromCustomEncounterMenuHero ?? _meetingStartedForProactiveRequestHero ?? MeetingBattleRuntime.TargetHero ?? _targetHero;
			}
		}
		catch
		{
			flag21 = false;
			flag22 = false;
			hero4 = null;
		}
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		try
		{
			Mission mission2 = mission as Mission;
			flag2 = mission2 != null && mission2.GetMissionBehavior<BattleEndLogic>() != null;
			flag17 = mission2 != null && mission2.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null;
			if (mission2 != null)
			{
				try
				{
					flag3 = mission2.MissionResult != null && mission2.MissionResult.PlayerDefeated;
				}
				catch
				{
					flag3 = false;
				}
				try
				{
					flag4 = mission2.MissionResult != null && mission2.MissionResult.PlayerVictory;
				}
				catch
				{
					flag4 = false;
				}
			}
		}
		catch
		{
			flag2 = false;
			flag3 = false;
			flag4 = false;
			flag17 = false;
		}
		if (flag2 && (flag || flag14))
		{
			try
			{
				Hero hero = _pendingMeetingBattleNativeResultHero ?? MeetingBattleRuntime.TargetHero ?? _targetHero;
				PartyBase defenderParty = ResolveMeetingBattleNativeResultParty(hero, null);
				MarkPendingMeetingBattleNativeResult(hero, defenderParty, "mission_ended_meeting_battle", flag4, flag3);
				flag14 = HasPendingMeetingBattleNativeResult();
			}
			catch (Exception ex)
			{
				Logger.Log("MeetingBattle", "Mark pending meeting battle native result on mission end failed: " + ex.Message);
			}
		}
		bool flag18 = flag12 || flag13 || flag17 || flag || flag14 || flag15 || flag16 || flag21 || flag22;
		if (!flag18)
		{
			Logger.Log("MeetingBattle", $"OnMissionEnded ignored for non-meeting mission. missionWasBattle={flag2}, missionResultPlayerDefeated={flag3}, missionResultPlayerVictory={flag4}");
			return;
		}
		bool flag5 = false;
		try
		{
			flag5 = PlayerEncounter.CampaignBattleResult != null;
		}
		catch
		{
			flag5 = false;
		}
		bool flag6 = HasResolvedCampaignBattleResult();
		bool flag7 = false;
		bool flag8 = false;
		try
		{
			if (PlayerEncounter.Current != null)
			{
				try
				{
					flag7 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
				}
				catch
				{
					flag7 = false;
				}
				try
				{
					PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
					flag8 = encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait;
				}
				catch
				{
					flag8 = false;
				}
			}
		}
		catch
		{
			flag7 = false;
			flag8 = false;
		}
		bool flag9 = flag2 || flag || flag14 || flag5 || flag6 || flag7 || flag8;
		if (flag9)
		{
			try
			{
				SuspendEncounterRedirectDuringResultResolution("mission_ended_after_meeting_battle");
			}
			catch
			{
			}
		}
		if (flag2 && flag)
		{
			try
			{
				PartyBase partyBase = null;
				try
				{
					partyBase = PlayerEncounter.EncounteredParty;
				}
				catch
				{
					partyBase = null;
				}
				partyBase = partyBase ?? _targetHero?.PartyBelongedTo?.Party;
				TryApplyImmediateEscalationConsequences(partyBase, _targetHero, "meeting_battle_mission_end_fallback");
			}
			catch (Exception ex)
			{
				Logger.Log("MeetingBattle", "OnMissionEnded fallback escalation failed: " + ex.Message);
			}
		}
		bool flag10 = flag2 && !flag && !flag3 && !flag14;
		bool flag19 = ConsumeMeetingPlayerReleaseAuthorization("mission_ended");
		bool flag20 = flag10 && !flag19 && !flag22 && !flag21 && IsHostileEncounterInitiatedByOpponent();
		bool flag11 = flag2 && flag && !flag3 && !flag4 && !flag6;
		if (flag19)
		{
			flag10 = flag2;
			flag11 = false;
			Hero hero5 = ResolveMeetingReleaseHeroForCurrentEncounter(hero4 ?? _targetHero);
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("meeting_release_authorized_exit");
			if (flag21)
			{
				try
				{
					ProactiveNpcRequestBehavior.CompleteActiveForHero(hero5, "meeting_release_authorized_exit");
				}
				catch
				{
				}
			}
			try
			{
				ApplyMeetingPlayerReleaseWorldMapCooldown(hero5, "meeting_release_authorized_exit");
			}
			catch
			{
			}
			if (flag10)
			{
				ScheduleMeetingReleaseSafePassageFinalReapply(hero5, "meeting_release_authorized_exit");
			}
		}
		else if (flag10 && (flag22 || flag21))
		{
			flag11 = false;
			string text = flag21 ? "proactive_request_meeting_exit" : "custom_encounter_meeting_exit";
			Hero hero6 = ResolveMeetingReleaseHeroForCurrentEncounter(hero4 ?? _targetHero);
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit(text);
			try
			{
				ApplyMeetingPlayerReleaseWorldMapCooldown(hero6, text);
			}
			catch
			{
			}
			ScheduleMeetingReleaseSafePassageFinalReapply(hero6, text);
			if (flag21)
			{
				try
				{
					ProactiveNpcRequestBehavior.CompleteActiveForHero(hero6, text);
				}
				catch
				{
				}
			}
			Logger.Log("MeetingBattle", "Custom encounter meeting exited peacefully; skipping hostile unauthorized encounter return. ProactiveRequest=" + flag21);
		}
		else if (flag2 && flag3)
		{
			if (flag14)
			{
				Hero hero2 = _pendingMeetingBattleNativeResultHero ?? MeetingBattleRuntime.TargetHero ?? _targetHero;
				MarkPendingMeetingBattleNativeResult(hero2, ResolveMeetingBattleNativeResultParty(hero2, null), "mission_result_defeat", playerDefeat: true);
			}
			MarkPendingForceNativeDefeatCaptivityMenu("meeting_battle_mission_result_defeat");
			ClearPendingMeetingBattleNativeResult("delegated_to_native_defeat_mission_result");
			TryResolvePendingDefeatCaptivityImmediately("mission_ended_player_defeated");
		}
		else if (flag2 && flag4)
		{
			Hero hero3 = _pendingMeetingBattleNativeResultHero ?? MeetingBattleRuntime.TargetHero ?? _targetHero;
			if (flag || flag14)
			{
				MarkPendingMeetingBattleNativeResult(hero3, ResolveMeetingBattleNativeResultParty(hero3, null), "mission_result_victory", playerVictory: true);
			}
			DisableCustomEncounterMenuForCurrentEncounter("meeting_battle_mission_result_victory");
		}
		else if (flag11)
		{
			MarkPendingForceNativeEncounterBattleMenu("meeting_battle_mission_exit_incomplete");
		}
		if (flag20)
		{
			flag10 = false;
			MarkPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("meeting_mission_exit_without_release");
			try
			{
				PlayerEncounter.LeaveEncounter = false;
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
			catch
			{
			}
		}
		if (flag2 && !flag4 && !flag20)
		{
			DisableCustomEncounterMenuForCurrentEncounter("meeting_battle_mission_ended");
		}
		Logger.Log("MeetingBattle", $"OnMissionEnded: combatEscalated={flag}, missionWasBattle={flag2}, missionResultPlayerDefeated={flag3}, missionResultPlayerVictory={flag4}, pendingMeetingNativeResult={flag14}, hasBattleResult={flag5}, hasResolvedBattleResult={flag6}, hasEncounterBattleContext={flag7}, hasEncounterResolvingState={flag8}, nativeResultFlow={flag9}, peacefulCleanup={flag10}, forceNativeEncounterMenu={flag11}, releaseAuthorized={flag19}, proactiveRequestMeeting={flag21}, customEncounterMeeting={flag22}, unauthorizedMeetingExit={flag20}");
		MeetingBattleRuntime.EndMeeting();
		_pendingPostMissionCleanup = true;
		_pendingPostMissionCleanupDelay = 0f;
		_pendingPeacefulMeetingBattleCleanup = flag10;
		_encounterMeetingMissionActive = false;
		_meetingStartedForProactiveRequest = false;
		_meetingStartedForProactiveRequestHero = null;
		_meetingStartedFromCustomEncounterMenu = false;
		_meetingStartedFromCustomEncounterMenuHero = null;
	}

	internal static void DisableCustomEncounterMenuForCurrentEncounter(string reason)
	{
		_disableCustomEncounterMenuForCurrentEncounter = true;
		try
		{
			_disableCustomEncounterMenuSinceTime = Time.ApplicationTime;
		}
		catch
		{
			_disableCustomEncounterMenuSinceTime = 0f;
		}
		try
		{
			_disableCustomEncounterMenuEncounterParty = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			_disableCustomEncounterMenuEncounterParty = null;
		}
		Logger.Log("LordEncounter", "Custom encounter menu disabled for current encounter. Reason=" + (reason ?? "N/A"));
	}

	private static void ClearCustomEncounterMenuDisable(string reason)
	{
		_disableCustomEncounterMenuForCurrentEncounter = false;
		_disableCustomEncounterMenuSinceTime = -1f;
		_disableCustomEncounterMenuEncounterParty = null;
		Logger.Log("LordEncounter", "Custom encounter menu disable cleared. Reason=" + (reason ?? "N/A"));
	}

	private static PartyBase GetCurrentEncounterPartySafe()
	{
		try
		{
			return PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
		}
		catch
		{
			try
			{
				return PlayerEncounter.EncounteredParty;
			}
			catch
			{
				return null;
			}
		}
	}

	private static Hero GetCurrentEncounterLeaderSafe()
	{
		try
		{
			return GetCurrentEncounterPartySafe()?.LeaderHero;
		}
		catch
		{
			return null;
		}
	}

	internal static bool ShouldLogEncounterDiagnosticForMenu(string menuId)
	{
		if (string.IsNullOrWhiteSpace(menuId))
		{
			return false;
		}
		string text = menuId.Trim();
		return text == "encounter"
			|| text == "join_encounter"
			|| text == "AnimusForge_lord_encounter"
			|| text.StartsWith("encounter_interrupted", StringComparison.OrdinalIgnoreCase)
			|| text.IndexOf("raid", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("village", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("siege", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static void LogEncounterDiagnostic(string stage, string reason = null, string menuId = null, Hero target = null, PartyBase encounterParty = null)
	{
		try
		{
			Logger.LogImmediate("Logic", BuildEncounterDiagnostic(stage, reason, menuId, target, encounterParty));
		}
		catch
		{
		}
	}

	private static string BuildEncounterDiagnostic(string stage, string reason, string menuId, Hero target, PartyBase encounterParty)
	{
		List<string> parts = new List<string>
		{
			"[EncounterDiag]",
			"stage=" + EncounterDiagText(stage),
			"reason=" + EncounterDiagText(reason),
			"menu=" + EncounterDiagText(menuId)
		};
		PartyBase resolvedParty = encounterParty ?? GetCurrentEncounterPartySafe();
		Hero resolvedTarget = target;
		if (resolvedTarget == null)
		{
			try
			{
				resolvedTarget = resolvedParty?.LeaderHero ?? _targetHero;
			}
			catch
			{
				resolvedTarget = _targetHero;
			}
		}
		AddEncounterDiagPart(parts, "currentMenu", () => Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId);
		AddEncounterDiagPart(parts, "conversationContext", () => (Campaign.Current?.CurrentConversationContext ?? ConversationContext.Default).ToString());
		AddEncounterDiagPart(parts, "flags", () => string.Join(",",
			"native=" + EncounterDiagBool(() => IsNativeEncounterActivityContext(resolvedTarget)),
			"villageRaid=" + EncounterDiagBool(() => IsVillageRaidEncounterContext(resolvedTarget)),
			"eligible=" + EncounterDiagBool(() => IsEligibleCustomLordEncounterTarget(resolvedTarget, resolvedParty)),
			"sea=" + EncounterDiagBool(() => MapSeaContextGuard.IsCurrentPlayerEncounterAtSea(resolvedTarget)),
			"pendingAttack=" + EncounterDiagBool(() => HasPendingForceNativeEncounterAttack()),
			"pendingMeetingResult=" + EncounterDiagBool(() => HasPendingMeetingBattleNativeResult()),
			"redirectSuspended=" + EncounterDiagBool(() => IsEncounterRedirectSuspended())));
		AddEncounterDiagPart(parts, "playerEncounter", DescribePlayerEncounterForEncounterDiag);
		AddEncounterDiagPart(parts, "target", () => DescribeHeroForEncounterDiag(resolvedTarget));
		AddEncounterDiagPart(parts, "encounterParty", () => DescribePartyBaseForEncounterDiag(resolvedParty));
		AddEncounterDiagPart(parts, "encounteredPartyStatic", () => DescribePartyBaseForEncounterDiag(PlayerEncounter.EncounteredParty));
		AddEncounterDiagPart(parts, "encounteredMobileStatic", () => DescribeMobilePartyForEncounterDiag(PlayerEncounter.EncounteredMobileParty));
		AddEncounterDiagPart(parts, "targetMobile", () => DescribeMobilePartyForEncounterDiag(resolvedTarget?.PartyBelongedTo));
		AddEncounterDiagPart(parts, "mainMobile", () => DescribeMobilePartyForEncounterDiag(MobileParty.MainParty));
		AddEncounterDiagPart(parts, "encounterSettlement", () => DescribeSettlementForEncounterDiag(PlayerEncounter.EncounterSettlement));
		AddEncounterDiagPart(parts, "settlementCurrent", () => DescribeSettlementForEncounterDiag(Settlement.CurrentSettlement));
		AddEncounterDiagPart(parts, "mainCurrentSettlement", () => DescribeSettlementForEncounterDiag(MobileParty.MainParty?.CurrentSettlement));
		AddEncounterDiagPart(parts, "mapCurrent", () => DescribeMapEventForEncounterDiag(PlayerEncounterCompat.GetCurrentMapEventSafe()));
		AddEncounterDiagPart(parts, "mapBattle", () => DescribeMapEventForEncounterDiag(PlayerEncounterCompat.GetBattleSafe()));
		AddEncounterDiagPart(parts, "mapEncountered", () => DescribeMapEventForEncounterDiag(PlayerEncounterCompat.GetEncounteredBattleSafe()));
		AddEncounterDiagPart(parts, "mapPlayer", () => DescribeMapEventForEncounterDiag(MapEvent.PlayerMapEvent));
		AddEncounterDiagPart(parts, "mapMainParty", () => DescribeMapEventForEncounterDiag(PartyBase.MainParty?.MapEvent));
		return string.Join(" | ", parts);
	}

	private static void AddEncounterDiagPart(List<string> parts, string name, Func<string> valueFactory)
	{
		try
		{
			parts.Add(name + "=" + EncounterDiagText(valueFactory?.Invoke()));
		}
		catch (Exception ex)
		{
			parts.Add(name + "=ERR:" + ex.GetType().Name);
		}
	}

	private static string EncounterDiagBool(Func<bool> valueFactory)
	{
		try
		{
			return valueFactory != null && valueFactory() ? "1" : "0";
		}
		catch (Exception ex)
		{
			return "ERR:" + ex.GetType().Name;
		}
	}

	private static string EncounterDiagText(object value)
	{
		string text = value?.ToString() ?? "null";
		return text.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
	}

	private static string DescribePlayerEncounterForEncounterDiag()
	{
		PlayerEncounter current = null;
		try
		{
			current = PlayerEncounter.Current;
		}
		catch
		{
			current = null;
		}
		if (current == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		AddEncounterDiagPart(parts, "state", () => current.EncounterState.ToString());
		AddEncounterDiagPart(parts, "leave", () => PlayerEncounter.LeaveEncounter ? "1" : "0");
		AddEncounterDiagPart(parts, "surrender", () => PlayerEncounter.PlayerSurrender ? "1" : "0");
		AddEncounterDiagPart(parts, "playerAttacker", () => PlayerEncounter.PlayerIsAttacker ? "1" : "0");
		AddEncounterDiagPart(parts, "forceRaid", () => current.ForceRaid ? "1" : "0");
		AddEncounterDiagPart(parts, "forceSallyOut", () => current.ForceSallyOut ? "1" : "0");
		AddEncounterDiagPart(parts, "forceSupplies", () => current.ForceSupplies ? "1" : "0");
		AddEncounterDiagPart(parts, "forceVolunteers", () => current.ForceVolunteers ? "1" : "0");
		AddEncounterDiagPart(parts, "restartedForRaid", () => BannerlordApiCompat.IsPlayerEncounterRestartedForRaid(current) ? "1" : "0");
		AddEncounterDiagPart(parts, "waiting", () => current.IsPlayerWaiting ? "1" : "0");
		return string.Join(",", parts);
	}

	private static string DescribeHeroForEncounterDiag(Hero hero)
	{
		if (hero == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		AddEncounterDiagPart(parts, "id", () => hero.StringId);
		AddEncounterDiagPart(parts, "name", () => hero.Name?.ToString());
		AddEncounterDiagPart(parts, "isLord", () => hero.IsLord ? "1" : "0");
		AddEncounterDiagPart(parts, "clan", () => hero.Clan?.StringId);
		AddEncounterDiagPart(parts, "kingdom", () => hero.Clan?.Kingdom?.StringId);
		return string.Join(",", parts);
	}

	private static string DescribePartyBaseForEncounterDiag(PartyBase party)
	{
		if (party == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		AddEncounterDiagPart(parts, "key", () => DescribePartyBaseKeyForEncounterDiag(party));
		AddEncounterDiagPart(parts, "name", () => party.Name?.ToString());
		AddEncounterDiagPart(parts, "leader", () => DescribeHeroForEncounterDiag(party.LeaderHero));
		AddEncounterDiagPart(parts, "mobile", () => party.IsMobile ? "1" : "0");
		AddEncounterDiagPart(parts, "settlement", () => party.IsSettlement ? "1" : "0");
		AddEncounterDiagPart(parts, "all", () => party.NumberOfAllMembers.ToString());
		AddEncounterDiagPart(parts, "healthy", () => party.NumberOfHealthyMembers.ToString());
		AddEncounterDiagPart(parts, "side", () => party.Side.ToString());
		AddEncounterDiagPart(parts, "mapEvent", () => DescribeMapEventKeyForEncounterDiag(party.MapEvent));
		AddEncounterDiagPart(parts, "mobileParty", () => DescribeMobilePartyForEncounterDiag(party.MobileParty));
		AddEncounterDiagPart(parts, "settlementObj", () => DescribeSettlementForEncounterDiag(party.Settlement));
		return string.Join(",", parts);
	}

	private static string DescribeMobilePartyForEncounterDiag(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		AddEncounterDiagPart(parts, "id", () => party.StringId);
		AddEncounterDiagPart(parts, "name", () => party.Name?.ToString());
		AddEncounterDiagPart(parts, "active", () => party.IsActive ? "1" : "0");
		AddEncounterDiagPart(parts, "main", () => party.IsMainParty ? "1" : "0");
		AddEncounterDiagPart(parts, "leader", () => DescribeHeroForEncounterDiag(party.LeaderHero));
		AddEncounterDiagPart(parts, "default", () => party.DefaultBehavior.ToString());
		AddEncounterDiagPart(parts, "short", () => party.ShortTermBehavior.ToString());
		AddEncounterDiagPart(parts, "current", () => DescribeSettlementKeyForEncounterDiag(party.CurrentSettlement));
		AddEncounterDiagPart(parts, "target", () => DescribeSettlementKeyForEncounterDiag(party.TargetSettlement));
		AddEncounterDiagPart(parts, "shortTarget", () => DescribeSettlementKeyForEncounterDiag(party.ShortTermTargetSettlement));
		AddEncounterDiagPart(parts, "mapEvent", () => DescribeMapEventKeyForEncounterDiag(party.MapEvent));
		AddEncounterDiagPart(parts, "siege", () => party.SiegeEvent != null ? "1" : "0");
		AddEncounterDiagPart(parts, "besieged", () => DescribeSettlementKeyForEncounterDiag(party.BesiegedSettlement));
		return string.Join(",", parts);
	}

	private static string DescribeSettlementForEncounterDiag(Settlement settlement)
	{
		if (settlement == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		AddEncounterDiagPart(parts, "id", () => settlement.StringId);
		AddEncounterDiagPart(parts, "name", () => settlement.Name?.ToString());
		AddEncounterDiagPart(parts, "village", () => settlement.IsVillage ? "1" : "0");
		AddEncounterDiagPart(parts, "fort", () => settlement.IsFortification ? "1" : "0");
		AddEncounterDiagPart(parts, "raid", () => settlement.IsUnderRaid ? "1" : "0");
		AddEncounterDiagPart(parts, "siege", () => settlement.IsUnderSiege ? "1" : "0");
		AddEncounterDiagPart(parts, "lastAttacker", () => DescribeMobilePartyKeyForEncounterDiag(settlement.LastAttackerParty));
		AddEncounterDiagPart(parts, "partyMapEvent", () => DescribeMapEventKeyForEncounterDiag(settlement.Party?.MapEvent));
		return string.Join(",", parts);
	}

	private static string DescribeMapEventForEncounterDiag(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		AddEncounterDiagPart(parts, "type", () => mapEvent.EventType.ToString());
		AddEncounterDiagPart(parts, "settlement", () => DescribeSettlementKeyForEncounterDiag(mapEvent.MapEventSettlement));
		AddEncounterDiagPart(parts, "player", () => mapEvent.IsPlayerMapEvent ? "1" : "0");
		AddEncounterDiagPart(parts, "raid", () => mapEvent.IsRaid ? "1" : "0");
		AddEncounterDiagPart(parts, "forceSupplies", () => mapEvent.IsForcingSupplies ? "1" : "0");
		AddEncounterDiagPart(parts, "forceVolunteers", () => mapEvent.IsForcingVolunteers ? "1" : "0");
		AddEncounterDiagPart(parts, "siegeAssault", () => mapEvent.IsSiegeAssault ? "1" : "0");
		AddEncounterDiagPart(parts, "sally", () => mapEvent.IsSallyOut ? "1" : "0");
		AddEncounterDiagPart(parts, "siegeOutside", () => mapEvent.IsSiegeOutside ? "1" : "0");
		AddEncounterDiagPart(parts, "blockade", () => mapEvent.IsBlockade ? "1" : "0");
		AddEncounterDiagPart(parts, "blockadeSally", () => mapEvent.IsBlockadeSallyOut ? "1" : "0");
		AddEncounterDiagPart(parts, "siegeAmbush", () => mapEvent.IsSiegeAmbush ? "1" : "0");
		AddEncounterDiagPart(parts, "finalized", () => mapEvent.IsFinalized ? "1" : "0");
		AddEncounterDiagPart(parts, "hasWinner", () => mapEvent.HasWinner ? "1" : "0");
		AddEncounterDiagPart(parts, "battleState", () => mapEvent.BattleState.ToString());
		AddEncounterDiagPart(parts, "playerSide", () => mapEvent.PlayerSide.ToString());
		AddEncounterDiagPart(parts, "winningSide", () => mapEvent.WinningSide.ToString());
		AddEncounterDiagPart(parts, "attLeader", () => DescribePartyBaseKeyForEncounterDiag(mapEvent.AttackerSide?.LeaderParty));
		AddEncounterDiagPart(parts, "defLeader", () => DescribePartyBaseKeyForEncounterDiag(mapEvent.DefenderSide?.LeaderParty));
		AddEncounterDiagPart(parts, "attTroops", () => mapEvent.AttackerSide?.TroopCount.ToString());
		AddEncounterDiagPart(parts, "defTroops", () => mapEvent.DefenderSide?.TroopCount.ToString());
		AddEncounterDiagPart(parts, "wasLooting", () => MapEventWasEverInLootingPhase(mapEvent) ? "1" : "0");
		return string.Join(",", parts);
	}

	private static string DescribeMapEventKeyForEncounterDiag(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return "null";
		}
		return "type=" + EncounterDiagText(mapEvent.EventType) + ",settlement=" + DescribeSettlementKeyForEncounterDiag(mapEvent.MapEventSettlement);
	}

	private static string DescribePartyBaseKeyForEncounterDiag(PartyBase party)
	{
		if (party == null)
		{
			return "null";
		}
		string id = null;
		try
		{
			if (party.IsMobile)
			{
				id = party.MobileParty?.StringId;
			}
			else if (party.IsSettlement)
			{
				id = party.Settlement?.StringId;
			}
		}
		catch
		{
			id = null;
		}
		if (string.IsNullOrWhiteSpace(id))
		{
			id = "party";
		}
		return EncounterDiagText(id) + "/" + EncounterDiagText(party.Name);
	}

	private static string DescribeMobilePartyKeyForEncounterDiag(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		return EncounterDiagText(party.StringId) + "/" + EncounterDiagText(party.Name);
	}

	private static string DescribeSettlementKeyForEncounterDiag(Settlement settlement)
	{
		if (settlement == null)
		{
			return "null";
		}
		return EncounterDiagText(settlement.StringId) + "/" + EncounterDiagText(settlement.Name);
	}

	internal static void SuppressCustomEncounterMenuUntilBackOnMapForExternal(string reason)
	{
		SuppressCustomEncounterMenuUntilBackOnMap(reason);
	}

	internal static bool IsCustomEncounterMenuHardSuppressedForExternal()
	{
		return IsCustomEncounterMenuHardSuppressedUntilBackOnMap();
	}

	private static void SuppressCustomEncounterMenuUntilBackOnMap(string reason)
	{
		if (_suppressCustomEncounterMenuUntilBackOnMap)
		{
			_disableCustomEncounterMenuForCurrentEncounter = true;
			if (string.IsNullOrEmpty(_suppressCustomEncounterMenuReason))
			{
				_suppressCustomEncounterMenuReason = reason ?? "meeting_battle";
			}
			return;
		}
		_suppressCustomEncounterMenuUntilBackOnMap = true;
		try
		{
			_suppressCustomEncounterMenuStartedAtTime = Time.ApplicationTime;
		}
		catch
		{
			_suppressCustomEncounterMenuStartedAtTime = 0f;
		}
		_suppressCustomEncounterMenuBackOnMapSinceTime = -1f;
		_suppressCustomEncounterMenuReason = reason ?? "meeting_battle";
		DisableCustomEncounterMenuForCurrentEncounter("hard_suppress_until_back_on_map_" + (reason ?? "unknown"));
		try
		{
			LordEncounterRedirectGuard.SuppressForSeconds(120f);
		}
		catch
		{
		}
		Logger.Log("LordEncounter", "Hard-suppressed custom encounter menu until the world map is stable for 2 seconds. Reason=" + (reason ?? "N/A"));
	}

	private static bool IsCustomEncounterMenuHardSuppressedUntilBackOnMap()
	{
		if (!_suppressCustomEncounterMenuUntilBackOnMap)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		try
		{
			flag = Game.Current?.GameStateManager?.ActiveState is MissionState;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag2 = Game.Current?.GameStateManager?.ActiveState is MapState;
		}
		catch
		{
			flag2 = false;
		}
		try
		{
			flag3 = PlayerEncounter.Current != null;
		}
		catch
		{
			flag3 = false;
		}
		try
		{
			flag4 = PlayerEncounterCompat.HasEncounterBattleContext() || PlayerEncounterCompat.HasCampaignBattleResult();
		}
		catch
		{
			flag4 = false;
		}
		try
		{
			flag5 = HasPendingForceNativeEncounterAttack() || HasPendingMeetingBattleNativeResult();
		}
		catch
		{
			flag5 = false;
		}
		if (flag || !flag2 || flag3 || flag4 || flag5 || IsNativeBattleResultConversationActive() || HasPendingForceNativeDefeatCaptivityMenu())
		{
			_suppressCustomEncounterMenuBackOnMapSinceTime = -1f;
			return true;
		}
		float num = 0f;
		try
		{
			num = Time.ApplicationTime;
		}
		catch
		{
			num = 0f;
		}
		if (_suppressCustomEncounterMenuBackOnMapSinceTime <= 0f)
		{
			_suppressCustomEncounterMenuBackOnMapSinceTime = num;
			return true;
		}
		if (num - _suppressCustomEncounterMenuBackOnMapSinceTime < 2f)
		{
			return true;
		}
		ClearCustomEncounterMenuHardSuppression("back_on_map_stable_2s");
		return false;
	}

	private static void ClearCustomEncounterMenuHardSuppression(string reason)
	{
		if (!_suppressCustomEncounterMenuUntilBackOnMap)
		{
			return;
		}
		_suppressCustomEncounterMenuUntilBackOnMap = false;
		_suppressCustomEncounterMenuStartedAtTime = -1f;
		_suppressCustomEncounterMenuBackOnMapSinceTime = -1f;
		_suppressCustomEncounterMenuReason = null;
		ClearCustomEncounterMenuDisable("hard_suppression_cleared_" + (reason ?? "unknown"));
		Logger.Log("LordEncounter", "Custom encounter menu hard suppression cleared. Reason=" + (reason ?? "N/A"));
	}

	private static bool CanSafelyActivateNativeEncounterMenu()
	{
		try
		{
			if (!(Game.Current?.GameStateManager?.ActiveState is MapState))
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			return PlayerEncounter.EncounteredParty != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryActivateNativeEncounterMenuSafely(string reason)
	{
		if (!CanSafelyActivateNativeEncounterMenu())
		{
			Logger.Log("LordEncounter", "Skipped native encounter menu activation because encounter context is incomplete. Reason=" + (reason ?? "N/A"));
			return false;
		}
		try
		{
			GameMenu.ActivateGameMenu("encounter");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Safe native encounter menu activation failed. Reason=" + (reason ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	internal static bool IsCustomEncounterMenuDisabledForCurrentEncounter()
	{
		if (IsNativeSettlementRequestMeetingContext())
		{
			return true;
		}
		if (IsCustomEncounterMenuHardSuppressedUntilBackOnMap())
		{
			return true;
		}
		if (HasPendingForceNativeEncounterAttack())
		{
			return true;
		}
		if (HasPendingMeetingBattleNativeResult())
		{
			return true;
		}
		if (HasPendingForceNativeDefeatCaptivityMenu())
		{
			return true;
		}
		if (IsNativeEncounterActivityContext())
		{
			return true;
		}
		if (!_disableCustomEncounterMenuForCurrentEncounter)
		{
			return false;
		}
		float num = 0f;
		float num2 = 999f;
		try
		{
			num = Time.ApplicationTime;
			if (_disableCustomEncounterMenuSinceTime > 0f)
			{
				num2 = num - _disableCustomEncounterMenuSinceTime;
			}
		}
		catch
		{
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag2 = HasPendingForceNativeDefeatCaptivityMenu();
		}
		catch
		{
			flag2 = false;
		}
		try
		{
			if (PlayerEncounter.Current != null)
			{
				PartyBase partyBase = null;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				try
				{
					partyBase = PlayerEncounter.EncounteredParty;
				}
				catch
				{
					partyBase = null;
				}
				try
				{
					flag3 = PlayerEncounterCompat.HasResolvedEncounterBattleContext();
				}
				catch
				{
					flag3 = false;
				}
				try
				{
					flag4 = PlayerEncounter.CampaignBattleResult != null;
				}
				catch
				{
					flag4 = false;
				}
				try
				{
					PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
					flag5 = encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait;
				}
				catch
				{
					flag5 = false;
				}
				if (_disableCustomEncounterMenuEncounterParty != null && partyBase != null && partyBase != _disableCustomEncounterMenuEncounterParty)
				{
					ClearCustomEncounterMenuDisable("encounter_party_changed");
					return false;
				}
				if (!(flag3 || flag4 || flag5 || flag || flag2))
				{
					ClearCustomEncounterMenuDisable("active_encounter_no_result_context");
					return false;
				}
				return true;
			}
		}
		catch
		{
		}
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		try
		{
			flag6 = Game.Current?.GameStateManager?.ActiveState is MissionState;
		}
		catch
		{
			flag6 = false;
		}
		try
		{
			flag7 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
		}
		catch
		{
			flag7 = false;
		}
		try
		{
			flag8 = PlayerEncounter.CampaignBattleResult != null;
		}
		catch
		{
			flag8 = false;
		}
		if (!flag6 && !flag7 && !flag8 && !flag && !flag2 && num2 >= 0.8f)
		{
			ClearCustomEncounterMenuDisable("back_on_map_no_result_context");
			return false;
		}
		if (flag2)
		{
			return true;
		}
		if (num2 > 12f)
		{
			ClearCustomEncounterMenuDisable("stale_timeout");
			return false;
		}
		return true;
	}

	internal static bool IsNativeSettlementRequestMeetingContext(Hero target = null)
	{
		try
		{
			string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			if (IsNativeSettlementRequestMeetingMenu(menuId))
			{
				if (TryGetNativeSettlementRequestMeetingSettlement(target, out Settlement settlement) && IsHostileSettlementForMainHero(settlement, target))
				{
					MarkNativeSettlementRequestMeetingContext(settlement, menuId, "current_menu");
					return true;
				}
				ClearNativeSettlementRequestMeetingContext("current_menu_not_hostile");
				return false;
			}
		}
		catch
		{
		}
		if (TryGetNativeSettlementRequestMeetingSettlement(target, out Settlement encounterSettlement)
			&& IsHostileSettlementForMainHero(encounterSettlement, target)
			&& IsCurrentPlayerEncounterBoundToSettlement(encounterSettlement, target))
		{
			MarkNativeSettlementRequestMeetingContext(encounterSettlement, "encounter", "player_encounter_settlement");
			return true;
		}
		if (!_nativeSettlementRequestMeetingContextActive)
		{
			return false;
		}
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (_nativeSettlementRequestMeetingContextUntilTime > 0f && applicationTime > _nativeSettlementRequestMeetingContextUntilTime)
			{
				ClearNativeSettlementRequestMeetingContext("expired");
				return false;
			}
		}
		catch
		{
		}
		if (_nativeSettlementRequestMeetingSettlement != null)
		{
			return IsHostileSettlementForMainHero(_nativeSettlementRequestMeetingSettlement, target);
		}
		return true;
	}

	private static bool IsNativeSettlementRequestMeetingMenu(string menuId)
	{
		if (string.IsNullOrWhiteSpace(menuId))
		{
			return false;
		}
		string text = menuId.Trim();
		return text == "request_meeting"
			|| text == "encounter_meeting"
			|| text == "request_meeting_with_besiegers"
			|| text == "request_meeting_parley";
	}

	private static bool TryGetNativeSettlementRequestMeetingSettlement(Hero target, out Settlement settlement)
	{
		settlement = null;
		try
		{
			settlement = Settlement.CurrentSettlement;
		}
		catch
		{
			settlement = null;
		}
		try
		{
			settlement ??= PlayerEncounter.EncounterSettlement;
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounterCompat.GetEncounteredPartySafe();
			if (settlement == null && encounteredParty != null)
			{
				if (encounteredParty.IsSettlement)
				{
					settlement = encounteredParty.Settlement;
				}
				else if (encounteredParty.IsMobile)
				{
					settlement = encounteredParty.MobileParty?.CurrentSettlement;
				}
			}
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (settlement == null && encounteredParty != null)
			{
				if (encounteredParty.IsSettlement)
				{
					settlement = encounteredParty.Settlement;
				}
				else if (encounteredParty.IsMobile)
				{
					settlement = encounteredParty.MobileParty?.CurrentSettlement;
				}
			}
		}
		catch
		{
		}
		try
		{
			settlement ??= PlayerEncounter.EncounteredMobileParty?.CurrentSettlement;
		}
		catch
		{
		}
		try
		{
			settlement ??= PlayerEncounterCompat.GetCurrentMapEventSafe()?.MapEventSettlement;
		}
		catch
		{
		}
		try
		{
			settlement ??= MobileParty.MainParty?.CurrentSettlement;
		}
		catch
		{
		}
		try
		{
			settlement ??= target?.CurrentSettlement;
		}
		catch
		{
		}
		try
		{
			settlement ??= target?.PartyBelongedTo?.CurrentSettlement;
		}
		catch
		{
		}
		return settlement != null && settlement.IsFortification;
	}

	private static bool IsCurrentPlayerEncounterBoundToSettlement(Settlement settlement, Hero target)
	{
		if (settlement == null)
		{
			return false;
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (PlayerEncounter.EncounterSettlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
			if (encounteredParty != null)
			{
				if (encounteredParty.IsSettlement && encounteredParty.Settlement == settlement)
				{
					return true;
				}
				if (encounteredParty.IsMobile && encounteredParty.MobileParty?.CurrentSettlement == settlement)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.EncounteredMobileParty?.CurrentSettlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounterCompat.GetCurrentMapEventSafe()?.MapEventSettlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (target != null && (target.CurrentSettlement == settlement || target.PartyBelongedTo?.CurrentSettlement == settlement))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsHostileSettlementForMainHero(Settlement settlement, Hero target)
	{
		try
		{
			IFaction playerFaction = Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.MapFaction;
			IFaction settlementFaction = settlement?.MapFaction ?? settlement?.OwnerClan?.MapFaction;
			if (playerFaction != null && settlementFaction != null && FactionManager.IsAtWarAgainstFaction(settlementFaction, playerFaction))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			IFaction playerFaction = Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.MapFaction;
			IFaction targetFaction = target?.MapFaction ?? target?.Clan?.MapFaction;
			if (playerFaction != null && targetFaction != null && FactionManager.IsAtWarAgainstFaction(targetFaction, playerFaction))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return target?.Clan != null && Clan.PlayerClan != null && target.Clan.IsAtWarWith(Clan.PlayerClan);
		}
		catch
		{
			return false;
		}
	}

	private static void MarkNativeSettlementRequestMeetingContext(Settlement settlement, string menuId, string reason)
	{
		bool flag = !_nativeSettlementRequestMeetingContextActive || _nativeSettlementRequestMeetingSettlement != settlement || _nativeSettlementRequestMeetingMenuId != menuId;
		_nativeSettlementRequestMeetingContextActive = true;
		_nativeSettlementRequestMeetingSettlement = settlement;
		_nativeSettlementRequestMeetingMenuId = menuId;
		try
		{
			_nativeSettlementRequestMeetingContextUntilTime = Time.ApplicationTime + 20f;
		}
		catch
		{
			_nativeSettlementRequestMeetingContextUntilTime = -1f;
		}
		if (flag)
		{
			Logger.Log("LordEncounter", "Native hostile settlement request meeting detected; custom encounter menu disabled for this vanilla meeting. Menu=" + (menuId ?? "N/A") + ", Settlement=" + (settlement?.Name?.ToString() ?? "N/A") + ", Reason=" + (reason ?? "N/A"));
		}
	}

	private static void ClearNativeSettlementRequestMeetingContext(string reason)
	{
		if (!_nativeSettlementRequestMeetingContextActive)
		{
			return;
		}
		_nativeSettlementRequestMeetingContextActive = false;
		_nativeSettlementRequestMeetingContextUntilTime = -1f;
		_nativeSettlementRequestMeetingSettlement = null;
		_nativeSettlementRequestMeetingMenuId = null;
		Logger.Log("LordEncounter", "Native hostile settlement request meeting guard cleared. Reason=" + (reason ?? "N/A"));
	}

	internal static bool IsNativeEncounterActivityContext(Hero target = null)
	{
		try
		{
			if (IsNativeActivityMenu(Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			PlayerEncounter current = PlayerEncounter.Current;
			if (current != null)
			{
				if (current.ForceRaid || current.ForceSallyOut || current.ForceSupplies || current.ForceVolunteers)
				{
					return true;
				}
				if (BannerlordApiCompat.IsPlayerEncounterRestartedForRaid(current))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivityMapEvent(PlayerEncounterCompat.GetCurrentMapEventSafe()))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivityParty(PlayerEncounterCompat.GetEncounteredPartySafe()))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivityParty(PlayerEncounter.EncounteredParty))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivityMobileParty(PlayerEncounter.EncounteredMobileParty))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivitySettlement(PlayerEncounter.EncounterSettlement))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivitySettlement(Settlement.CurrentSettlement))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivitySettlement(MobileParty.MainParty?.CurrentSettlement))
			{
				return true;
			}
		}
		catch
		{
		}
		if (IsNativeActivityHeroParty(target))
		{
			return true;
		}
		try
		{
			if (Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter && IsNativeActivityHeroParty(Hero.OneToOneConversationHero))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject characterObject = CharacterObject.OneToOneConversationCharacter;
			if (Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter && IsNativeActivityHeroParty(characterObject?.HeroObject))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool IsVillageRaidEncounterContext(Hero target = null)
	{
		try
		{
			if (IsVillageRaidMenu(Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsVillageRaidMapEvent(PlayerEncounterCompat.GetCurrentMapEventSafe()))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsVillageRaidParty(PlayerEncounterCompat.GetEncounteredPartySafe()))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsVillageRaidParty(PlayerEncounter.EncounteredParty))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsVillageRaidMobileParty(PlayerEncounter.EncounteredMobileParty))
			{
				return true;
			}
		}
		catch
		{
		}
		if (IsVillageRaidHeroParty(target))
		{
			return true;
		}
		try
		{
			if (Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter && IsVillageRaidHeroParty(Hero.OneToOneConversationHero))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject characterObject = CharacterObject.OneToOneConversationCharacter;
			if (Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter && IsVillageRaidHeroParty(characterObject?.HeroObject))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsNativeActivityMenu(string menuId)
	{
		if (string.IsNullOrWhiteSpace(menuId))
		{
			return false;
		}
		string text = menuId.Trim();
		return text == "join_siege_event"
			|| text == "join_sally_out"
			|| text == "naval_town_outside"
			|| text == "raiding_village"
			|| text == "raid_occupied"
			|| text == "village_hostile_action"
			|| text == "raid_village_no_resist_warn_player"
			|| text == "force_supplies_village_resist_warn_player"
			|| text == "force_troops_village_resist_warn_player"
			|| text == "continue_siege_after_attack"
			|| text == "menu_siege_strategies"
			|| text.StartsWith("encounter_interrupted_raid", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsNativeActivityMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			Settlement settlement = mapEvent.MapEventSettlement;
			if (settlement != null && settlement.IsVillage && IsNativeVillageHostileMapEvent(mapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return mapEvent.IsSiegeAssault
				|| mapEvent.IsSallyOut
				|| mapEvent.IsSiegeOutside
				|| mapEvent.IsBlockade
				|| mapEvent.IsBlockadeSallyOut
				|| mapEvent.IsSiegeAmbush;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNativeActivityParty(PartyBase party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			if (IsNativeActivityMapEvent(party.MapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.SiegeEvent != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.IsSettlement && IsNativeActivitySettlement(party.Settlement))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.IsMobile && IsNativeActivityMobileParty(party.MobileParty))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsNativeActivityMobileParty(MobileParty party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			if (IsNativeActivityMapEvent(party.MapEvent) || IsNativeActivityMapEvent(party.Party?.MapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.SiegeEvent != null || party.BesiegedSettlement != null || party.BesiegerCamp != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsVillageRaidMobileParty(party))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsActiveSiegeSettlement(party.CurrentSettlement))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsNativeActivityHeroParty(Hero hero)
	{
		try
		{
			return hero != null && IsNativeActivityMobileParty(hero.PartyBelongedTo);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNativeActivitySettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return false;
		}
		try
		{
			if (IsActiveVillageRaidSettlement(settlement) || IsActiveSiegeSettlement(settlement))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsNativeActivityMapEvent(settlement.Party?.MapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsVillageRaidMenu(string menuId)
	{
		return menuId == "raiding_village" || menuId == "raid_occupied" || menuId == "encounter_interrupted_raid_started";
	}

	private static bool IsVillageRaidMapEvent(MapEvent mapEvent)
	{
		try
		{
			return mapEvent != null && mapEvent.MapEventSettlement != null && mapEvent.MapEventSettlement.IsVillage && IsNativeVillageHostileMapEvent(mapEvent);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNativeVillageHostileMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			if (mapEvent.IsRaid || mapEvent.IsForcingSupplies || mapEvent.IsForcingVolunteers)
			{
				return true;
			}
		}
		catch
		{
		}
		return MapEventWasEverInLootingPhase(mapEvent);
	}

	private static bool MapEventWasEverInLootingPhase(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			PropertyInfo property = mapEvent.GetType().GetProperty("WasEverInLootingPhase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return property != null && Convert.ToBoolean(property.GetValue(mapEvent, null));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsVillageRaidParty(PartyBase party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			if (IsVillageRaidMapEvent(party.MapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.IsMobile && IsVillageRaidMobileParty(party.MobileParty))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.IsSettlement && IsActiveVillageRaidSettlement(party.Settlement))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsVillageRaidMobileParty(MobileParty party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			if (IsVillageRaidMapEvent(party.MapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsVillageRaidMapEvent(party.Party?.MapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement targetSettlement = party.TargetSettlement;
			if (IsActiveVillageRaidSettlement(targetSettlement) && targetSettlement.LastAttackerParty == party)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement currentSettlement = party.CurrentSettlement;
			if (IsActiveVillageRaidSettlement(currentSettlement) && currentSettlement.LastAttackerParty == party)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.DefaultBehavior == AiBehavior.RaidSettlement && IsVillageSettlement(party.TargetSettlement))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.ShortTermBehavior == AiBehavior.RaidSettlement && IsVillageSettlement(party.ShortTermTargetSettlement))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsVillageSettlement(Settlement settlement)
	{
		try
		{
			return settlement != null && settlement.IsVillage;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsVillageRaidHeroParty(Hero hero)
	{
		try
		{
			return hero != null && IsVillageRaidMobileParty(hero.PartyBelongedTo);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsActiveVillageRaidSettlement(Settlement settlement)
	{
		try
		{
			return settlement != null && settlement.IsVillage && settlement.IsUnderRaid;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsActiveSiegeSettlement(Settlement settlement)
	{
		try
		{
			return settlement != null && settlement.IsFortification && (settlement.IsUnderSiege || settlement.SiegeEvent != null);
		}
		catch
		{
			return false;
		}
	}

	private static void MarkPendingForceNativeDefeatCaptivityMenu(string reason)
	{
		_pendingForceNativeDefeatCaptivityMenu = true;
		try
		{
			_pendingForceNativeDefeatCaptivityMenuAtTime = Time.ApplicationTime;
		}
		catch
		{
			_pendingForceNativeDefeatCaptivityMenuAtTime = 0f;
		}
		_pendingForceNativeDefeatCaptivityLastAttemptTime = -1f;
		try
		{
			_pendingForceNativeDefeatCaptivityParty = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			_pendingForceNativeDefeatCaptivityParty = null;
		}
		try
		{
			_pendingForceNativeDefeatCaptivityHero = _pendingForceNativeDefeatCaptivityParty?.LeaderHero ?? _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		catch
		{
			_pendingForceNativeDefeatCaptivityHero = _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		try
		{
			_pendingForceNativeDefeatCaptivityPlayerWasAttacker = PlayerEncounter.Current == null || PlayerEncounter.PlayerIsAttacker;
		}
		catch
		{
			_pendingForceNativeDefeatCaptivityPlayerWasAttacker = true;
		}
		try
		{
			SuspendEncounterRedirectDuringResultResolution(reason);
		}
		catch
		{
		}
		Logger.Log("LordEncounter", string.Format("Marked pending native defeat captivity menu redirect. Reason={0}, CaptorHero={1}, CaptorParty={2}", reason ?? "N/A", _pendingForceNativeDefeatCaptivityHero?.Name, _pendingForceNativeDefeatCaptivityParty?.Name));
	}

	internal static bool HasPendingForceNativeDefeatCaptivityMenu()
	{
		if (!_pendingForceNativeDefeatCaptivityMenu)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		try
		{
			num = Time.ApplicationTime;
			if (_pendingForceNativeDefeatCaptivityMenuAtTime > 0f)
			{
				num2 = num - _pendingForceNativeDefeatCaptivityMenuAtTime;
			}
		}
		catch
		{
		}
		if (num2 > 30f)
		{
			ClearPendingForceNativeDefeatCaptivityMenu("expired");
			return false;
		}
		return true;
	}

	private static void ClearPendingForceNativeDefeatCaptivityMenu(string reason)
	{
		_pendingForceNativeDefeatCaptivityMenu = false;
		_pendingForceNativeDefeatCaptivityMenuAtTime = 0f;
		_pendingForceNativeDefeatCaptivityLastAttemptTime = -1f;
		_pendingForceNativeDefeatCaptivityHero = null;
		_pendingForceNativeDefeatCaptivityParty = null;
		_pendingForceNativeDefeatCaptivityPlayerWasAttacker = true;
		Logger.Log("LordEncounter", "Cleared pending native defeat captivity marker. Reason=" + (reason ?? "N/A"));
	}

	private static void MarkPendingForceNativeEncounterBattleMenu(string reason)
	{
		_pendingForceNativeEncounterBattleMenu = true;
		try
		{
			_pendingForceNativeEncounterBattleMenuAtTime = Time.ApplicationTime;
		}
		catch
		{
			_pendingForceNativeEncounterBattleMenuAtTime = 0f;
		}
		_pendingForceNativeEncounterBattleMenuLastAttemptTime = -1f;
		try
		{
			_pendingForceNativeEncounterBattleMenuEncounterParty = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			_pendingForceNativeEncounterBattleMenuEncounterParty = null;
		}
		try
		{
			_pendingForceNativeEncounterBattleMenuEncounterLeader = _pendingForceNativeEncounterBattleMenuEncounterParty?.LeaderHero ?? _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		catch
		{
			_pendingForceNativeEncounterBattleMenuEncounterLeader = _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		Logger.Log("LordEncounter", "Marked pending native encounter battle menu redirect. Reason=" + (reason ?? "N/A"));
	}

	internal static bool HasPendingForceNativeEncounterBattleMenu()
	{
		if (!_pendingForceNativeEncounterBattleMenu)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		try
		{
			num = Time.ApplicationTime;
			if (_pendingForceNativeEncounterBattleMenuAtTime > 0f)
			{
				num2 = num - _pendingForceNativeEncounterBattleMenuAtTime;
			}
		}
		catch
		{
		}
		PartyBase partyBase = null;
		try
		{
			partyBase = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			partyBase = null;
		}
		if (_pendingForceNativeEncounterBattleMenuEncounterParty != null && partyBase != null && partyBase != _pendingForceNativeEncounterBattleMenuEncounterParty)
		{
			ClearPendingForceNativeEncounterBattleMenu("encounter_party_changed");
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = PlayerEncounter.Current != null;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag2 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
		}
		catch
		{
			flag2 = false;
		}
		if (num2 > 2.5f && !flag && !flag2)
		{
			ClearPendingForceNativeEncounterBattleMenu("no_encounter_context");
			return false;
		}
		if (num2 > 20f)
		{
			ClearPendingForceNativeEncounterBattleMenu("expired");
			return false;
		}
		return true;
	}

	private static void ClearPendingForceNativeEncounterBattleMenu(string reason)
	{
		_pendingForceNativeEncounterBattleMenu = false;
		_pendingForceNativeEncounterBattleMenuAtTime = 0f;
		_pendingForceNativeEncounterBattleMenuLastAttemptTime = -1f;
		_pendingForceNativeEncounterBattleMenuEncounterParty = null;
		_pendingForceNativeEncounterBattleMenuEncounterLeader = null;
		Logger.Log("LordEncounter", "Cleared pending native encounter battle menu marker. Reason=" + (reason ?? "N/A"));
	}

	internal static void MarkPendingMeetingBattleNativeResultForExternal(Hero target, string reason)
	{
		MarkPendingMeetingBattleNativeResult(target, null, reason);
	}

	internal static bool HasPendingMeetingBattleNativeResultForExternal()
	{
		return HasPendingMeetingBattleNativeResult();
	}

	private static void MarkPendingMeetingBattleNativeResult(Hero target, PartyBase defenderParty, string reason, bool playerVictory = false, bool playerDefeat = false)
	{
		bool flag = _pendingMeetingBattleNativeResult;
		_pendingMeetingBattleNativeResult = true;
		if (!flag || _pendingMeetingBattleNativeResultAtTime <= 0f)
		{
			try
			{
				_pendingMeetingBattleNativeResultAtTime = Time.ApplicationTime;
			}
			catch
			{
				_pendingMeetingBattleNativeResultAtTime = 0f;
			}
		}
		if (!flag)
		{
			_pendingMeetingBattleNativeResultLastAttemptTime = -1f;
		}
		_pendingMeetingBattleNativeResultHero = target ?? _pendingMeetingBattleNativeResultHero ?? MeetingBattleRuntime.TargetHero ?? _targetHero;
		_pendingMeetingBattleNativeResultParty = ResolveMeetingBattleNativeResultParty(_pendingMeetingBattleNativeResultHero, defenderParty);
		_pendingMeetingBattleNativeResultReason = reason ?? _pendingMeetingBattleNativeResultReason ?? "meeting_battle_native_result";
		_pendingMeetingBattleNativeResultPlayerVictory = _pendingMeetingBattleNativeResultPlayerVictory || playerVictory;
		_pendingMeetingBattleNativeResultPlayerDefeat = _pendingMeetingBattleNativeResultPlayerDefeat || playerDefeat;
		SuppressCustomEncounterMenuUntilBackOnMap("meeting_battle_native_result_" + (reason ?? "unknown"));
		try
		{
			if (_pendingMeetingBattleNativeResultHero != null)
			{
				SetTarget(_pendingMeetingBattleNativeResultHero);
			}
		}
		catch
		{
		}
		DisableCustomEncounterMenuForCurrentEncounter("meeting_battle_native_result_" + (reason ?? "unknown"));
		SuspendEncounterRedirectDuringResultResolution("meeting_battle_native_result_" + (reason ?? "unknown"));
		try
		{
			LordEncounterRedirectGuard.SuppressForSeconds(90f);
		}
		catch
		{
		}
		Logger.Log("MeetingBattle", $"Marked pending meeting battle native result. Target={_pendingMeetingBattleNativeResultHero?.Name}, Party={_pendingMeetingBattleNativeResultParty?.Name}, Victory={_pendingMeetingBattleNativeResultPlayerVictory}, Defeat={_pendingMeetingBattleNativeResultPlayerDefeat}, Reason={_pendingMeetingBattleNativeResultReason}");
	}

	private static bool HasPendingMeetingBattleNativeResult()
	{
		if (!_pendingMeetingBattleNativeResult)
		{
			return false;
		}
		float num = GetPendingMeetingBattleNativeResultElapsedSeconds();
		if (num > 180f)
		{
			ClearPendingMeetingBattleNativeResult("expired");
			return false;
		}
		PartyBase partyBase = GetCurrentEncounterPartySafe();
		if (_pendingMeetingBattleNativeResultParty != null && partyBase != null && partyBase != _pendingMeetingBattleNativeResultParty)
		{
			ClearPendingMeetingBattleNativeResult("encounter_party_changed");
			return false;
		}
		Hero currentEncounterLeaderSafe = GetCurrentEncounterLeaderSafe();
		if (_pendingMeetingBattleNativeResultHero != null && currentEncounterLeaderSafe != null && currentEncounterLeaderSafe != _pendingMeetingBattleNativeResultHero)
		{
			ClearPendingMeetingBattleNativeResult("encounter_target_changed");
			return false;
		}
		return true;
	}

	private static float GetPendingMeetingBattleNativeResultElapsedSeconds()
	{
		try
		{
			if (_pendingMeetingBattleNativeResultAtTime > 0f)
			{
				return Time.ApplicationTime - _pendingMeetingBattleNativeResultAtTime;
			}
		}
		catch
		{
		}
		return 0f;
	}

	private static void ClearPendingMeetingBattleNativeResult(string reason)
	{
		_pendingMeetingBattleNativeResult = false;
		_pendingMeetingBattleNativeResultAtTime = 0f;
		_pendingMeetingBattleNativeResultLastAttemptTime = -1f;
		_pendingMeetingBattleNativeResultParty = null;
		_pendingMeetingBattleNativeResultHero = null;
		_pendingMeetingBattleNativeResultReason = null;
		_pendingMeetingBattleNativeResultPlayerVictory = false;
		_pendingMeetingBattleNativeResultPlayerDefeat = false;
		Logger.Log("MeetingBattle", "Cleared pending meeting battle native result. Reason=" + (reason ?? "N/A"));
	}

	private static PartyBase ResolveMeetingBattleNativeResultParty(Hero target, PartyBase fallbackParty)
	{
		PartyBase partyBase = null;
		try
		{
			partyBase = fallbackParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = _pendingMeetingBattleNativeResultParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = PlayerEncounterCompat.GetEncounteredPartySafe();
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = target?.PartyBelongedTo?.Party;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = MeetingBattleRuntime.TargetHero?.PartyBelongedTo?.Party;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = _targetHero?.PartyBelongedTo?.Party;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = _encounterRedirectSuspendedEncounterParty;
		}
		catch
		{
			partyBase = null;
		}
		return partyBase;
	}

	private static void TryGetPendingMeetingBattleNativeOutcome(out bool playerVictory, out bool playerDefeat)
	{
		playerVictory = _pendingMeetingBattleNativeResultPlayerVictory;
		playerDefeat = _pendingMeetingBattleNativeResultPlayerDefeat;
		try
		{
			CampaignBattleResult campaignBattleResult = PlayerEncounterCompat.GetCampaignBattleResultSafe();
			if (campaignBattleResult == null)
			{
				try
				{
					campaignBattleResult = PlayerEncounter.CampaignBattleResult;
				}
				catch
				{
					campaignBattleResult = null;
				}
			}
			if (campaignBattleResult != null)
			{
				try
				{
					playerVictory = playerVictory || campaignBattleResult.PlayerVictory;
				}
				catch
				{
				}
				try
				{
					playerDefeat = playerDefeat || campaignBattleResult.PlayerDefeat;
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
			MapEvent mapEvent = TryGetCurrentEncounterBattle() ?? PlayerEncounterCompat.GetCurrentMapEventSafe();
			if (mapEvent != null && mapEvent.HasWinner)
			{
				BattleSideEnum playerSide = mapEvent.PlayerSide;
				playerVictory = playerVictory || mapEvent.WinningSide == playerSide;
				playerDefeat = playerDefeat || mapEvent.DefeatedSide == playerSide;
			}
		}
		catch
		{
		}
	}

	private static void TryForcePendingMeetingBattleNativeResultIfReady(string reason)
	{
		if (!HasPendingMeetingBattleNativeResult())
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		if (IsNativeBattleResultConversationActive())
		{
			return;
		}
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (_pendingMeetingBattleNativeResultLastAttemptTime > 0f && applicationTime - _pendingMeetingBattleNativeResultLastAttemptTime < 0.25f)
			{
				return;
			}
			_pendingMeetingBattleNativeResultLastAttemptTime = applicationTime;
		}
		catch
		{
			_pendingMeetingBattleNativeResultLastAttemptTime = 0f;
		}
		TryGetPendingMeetingBattleNativeOutcome(out var playerVictory, out var playerDefeat);
		if (playerDefeat)
		{
			MarkPendingForceNativeDefeatCaptivityMenu("meeting_battle_native_result_defeat_" + (reason ?? "unknown"));
			ClearPendingMeetingBattleNativeResult("delegated_to_native_defeat_" + (reason ?? "unknown"));
			TryResolvePendingDefeatCaptivityImmediately(reason ?? "meeting_battle_native_result_defeat");
			TryForcePendingDefeatCaptivityMenuIfReady();
			return;
		}
		if (playerVictory)
		{
			Hero hero = _pendingMeetingBattleNativeResultHero ?? MeetingBattleRuntime.TargetHero ?? _targetHero;
			PartyBase partyBase = ResolveMeetingBattleNativeResultParty(hero, _pendingMeetingBattleNativeResultParty);
			MarkPendingMeetingBattleNativeResult(hero, partyBase, "native_victory_wait_" + (reason ?? "unknown"), playerVictory: true);
			TryClearPendingMeetingBattleNativeResultIfComplete(reason ?? "meeting_battle_native_result_victory");
			return;
		}
		try
		{
			if (string.Equals(Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId, "AnimusForge_lord_encounter", StringComparison.Ordinal))
			{
				DisableCustomEncounterMenuForCurrentEncounter("pending_meeting_native_result_no_outcome");
				TryActivateNativeEncounterMenuSafely("pending_meeting_native_result_no_outcome");
				return;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingBattle", "Pending native result fallback to native encounter menu failed: " + ex.Message);
		}
		TryClearPendingMeetingBattleNativeResultIfComplete(reason ?? "meeting_battle_native_result_waiting");
	}

	private static void TryClearPendingMeetingBattleNativeResultIfComplete(string reason)
	{
		if (!_pendingMeetingBattleNativeResult)
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		try
		{
			flag = PlayerEncounter.Current != null;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag2 = PlayerEncounterCompat.HasEncounterBattleContext();
		}
		catch
		{
			flag2 = false;
		}
		try
		{
			flag3 = PlayerEncounterCompat.HasCampaignBattleResult();
		}
		catch
		{
			flag3 = false;
		}
		if (!flag && !flag2 && !flag3)
		{
			ClearPendingMeetingBattleNativeResult("native_result_complete_" + (reason ?? "unknown"));
		}
	}

	private static bool IsNativeBattleResultConversationActive()
	{
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			ConversationContext currentConversationContext = Campaign.Current.CurrentConversationContext;
			return currentConversationContext == ConversationContext.CapturedLord || currentConversationContext == ConversationContext.FreeOrCapturePrisonerHero;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasPendingForceNativeEncounterAttack()
	{
		if (!_pendingForceNativeEncounterAttack)
		{
			return false;
		}
		float num = 0f;
		try
		{
			if (_pendingForceNativeEncounterAttackAtTime > 0f)
			{
				num = Time.ApplicationTime - _pendingForceNativeEncounterAttackAtTime;
			}
		}
		catch
		{
		}
		if (num > 120f)
		{
			ClearPendingForceNativeEncounterAttack("expired");
			return false;
		}
		PartyBase currentEncounterPartySafe = GetCurrentEncounterPartySafe();
		if (_pendingForceNativeEncounterAttackParty != null && currentEncounterPartySafe != null && currentEncounterPartySafe != _pendingForceNativeEncounterAttackParty)
		{
			ClearPendingForceNativeEncounterAttack("encounter_party_changed");
			return false;
		}
		Hero currentEncounterLeaderSafe = GetCurrentEncounterLeaderSafe();
		if (_pendingForceNativeEncounterAttackHero != null && currentEncounterLeaderSafe != null && currentEncounterLeaderSafe != _pendingForceNativeEncounterAttackHero)
		{
			ClearPendingForceNativeEncounterAttack("encounter_target_changed");
			return false;
		}
		return true;
	}

	internal static bool HasPendingNativeEncounterAttackForExternal()
	{
		return HasPendingForceNativeEncounterAttack();
	}

	private static void MarkPendingForceNativeEncounterAttack(Hero target, PartyBase defenderParty, string reason)
	{
		_pendingForceNativeEncounterAttack = true;
		try
		{
			_pendingForceNativeEncounterAttackAtTime = Time.ApplicationTime;
		}
		catch
		{
			_pendingForceNativeEncounterAttackAtTime = 0f;
		}
		_pendingForceNativeEncounterAttackLastAttemptTime = -1f;
		_pendingForceNativeEncounterAttackDiplomacyApplied = false;
		_pendingForceNativeEncounterAttackConversationEnded = false;
		_pendingForceNativeEncounterAttackHero = target ?? _targetHero;
		_pendingForceNativeEncounterAttackParty = defenderParty;
		_pendingForceNativeEncounterAttackReason = reason ?? "native_conversation_taunt_battle";
		SuppressCustomEncounterMenuUntilBackOnMap("pending_native_encounter_attack_" + (_pendingForceNativeEncounterAttackReason ?? "unknown"));
		RegisterPendingNativeEncounterAttackConversationEndHook();
		try
		{
			InformationManager.DisplayMessage(new InformationMessage("挑衅已升级为敌对行动：5秒后将自动结束对话并攻入敌阵。", Colors.Yellow));
		}
		catch
		{
		}
		Logger.Log("MeetingTaunt", $"Marked pending native encounter attack. Target={_pendingForceNativeEncounterAttackHero?.Name}, Defender={_pendingForceNativeEncounterAttackParty?.Name}, Reason={_pendingForceNativeEncounterAttackReason}");
	}

	private static void ClearPendingForceNativeEncounterAttack(string reason)
	{
		UnregisterPendingNativeEncounterAttackConversationEndHook();
		_pendingForceNativeEncounterAttack = false;
		_pendingForceNativeEncounterAttackAtTime = 0f;
		_pendingForceNativeEncounterAttackLastAttemptTime = -1f;
		_pendingForceNativeEncounterAttackDiplomacyApplied = false;
		_pendingForceNativeEncounterAttackConversationEnded = false;
		_pendingForceNativeEncounterAttackParty = null;
		_pendingForceNativeEncounterAttackHero = null;
		_pendingForceNativeEncounterAttackReason = null;
		Logger.Log("MeetingTaunt", "Cleared pending native encounter attack. Reason=" + (reason ?? "N/A"));
	}

	private static void RegisterPendingNativeEncounterAttackConversationEndHook()
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager == null)
			{
				return;
			}
			conversationManager.ConversationEndOneShot -= OnPendingNativeEncounterAttackConversationEnded;
			conversationManager.ConversationEndOneShot += OnPendingNativeEncounterAttackConversationEnded;
			_pendingForceNativeEncounterAttackEndHookRegistered = true;
			Logger.Log("MeetingTaunt", "Registered pending native encounter attack conversation-end hook.");
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Registering native encounter attack conversation-end hook failed: " + ex.Message);
		}
	}

	private static void UnregisterPendingNativeEncounterAttackConversationEndHook()
	{
		if (!_pendingForceNativeEncounterAttackEndHookRegistered)
		{
			return;
		}
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager != null)
			{
				conversationManager.ConversationEndOneShot -= OnPendingNativeEncounterAttackConversationEnded;
			}
		}
		catch
		{
		}
		_pendingForceNativeEncounterAttackEndHookRegistered = false;
	}

	private static void OnPendingNativeEncounterAttackConversationEnded()
	{
		_pendingForceNativeEncounterAttackEndHookRegistered = false;
		if (!HasPendingForceNativeEncounterAttack())
		{
			return;
		}
		_pendingForceNativeEncounterAttackConversationEnded = true;
		_pendingForceNativeEncounterAttackLastAttemptTime = -1f;
		Logger.Log("MeetingTaunt", "Native encounter attack conversation ended; attack will execute on next engine/campaign tick.");
	}

	private static float GetPendingNativeEncounterAttackElapsedSeconds()
	{
		try
		{
			if (_pendingForceNativeEncounterAttackAtTime > 0f)
			{
				return Time.ApplicationTime - _pendingForceNativeEncounterAttackAtTime;
			}
		}
		catch
		{
		}
		return NativeEncounterAttackDialogDelaySeconds;
	}

	private static void TryApplyPendingNativeEncounterAttackDiplomacy(string reason)
	{
		if (_pendingForceNativeEncounterAttackDiplomacyApplied)
		{
			return;
		}
		try
		{
			PartyBase defenderParty = ResolveNativeEncounterAttackDefenderParty(_pendingForceNativeEncounterAttackHero, _pendingForceNativeEncounterAttackParty);
			Hero hero = _pendingForceNativeEncounterAttackHero ?? defenderParty?.LeaderHero ?? _targetHero;
			ApplyHostileEscalationDiplomaticConsequences(defenderParty, hero, reason ?? _pendingForceNativeEncounterAttackReason ?? "native_conversation_taunt_attack_delay", "MeetingTaunt");
			_pendingForceNativeEncounterAttackDiplomacyApplied = true;
			Logger.Log("MeetingTaunt", $"Native encounter attack diplomacy applied during dialog delay. Target={hero?.Name}, Defender={defenderParty?.Name}, Reason={reason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Native encounter attack diplomacy failed during dialog delay: " + ex.Message);
		}
	}

	private static PartyBase ResolveNativeEncounterAttackDefenderParty(Hero target)
	{
		return ResolveNativeEncounterAttackDefenderParty(target, null);
	}

	private static PartyBase ResolveNativeEncounterAttackDefenderParty(Hero target, PartyBase fallbackParty)
	{
		PartyBase partyBase = null;
		try
		{
			partyBase = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = target?.PartyBelongedTo?.Party;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = fallbackParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = _pendingForceNativeEncounterAttackParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			Hero hero = target ?? _pendingForceNativeEncounterAttackHero ?? _targetHero;
			if (hero != null)
			{
				foreach (MobileParty item in MobileParty.All)
				{
					if (item?.Party != null && item.LeaderHero == hero)
					{
						return item.Party;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool TryEnsureEncounterContextForNativeAttack(Hero target, out PartyBase defenderParty)
	{
		defenderParty = ResolveNativeEncounterAttackDefenderParty(target);
		if (defenderParty == null || PartyBase.MainParty == null)
		{
			Logger.Log("MeetingTaunt", "Native encounter attack aborted: defender/main party is null.");
			return false;
		}
		DisableCustomEncounterMenuForCurrentEncounter("native_conversation_taunt_attack_prepare");
		SuspendEncounterRedirectDuringResultResolution("native_conversation_taunt_attack_prepare");
		try
		{
			LordEncounterRedirectGuard.SuppressForSeconds(12f);
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounterCompat.RestartPlayerEncounter(defenderParty, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "RestartPlayerEncounter for native attack failed: " + ex.Message);
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounter.Start();
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.SetupFields(PartyBase.MainParty, defenderParty);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("MeetingTaunt", "Start+SetupFields fallback for native attack failed: " + ex2.Message);
		}
		if (PlayerEncounter.Current == null)
		{
			Logger.Log("MeetingTaunt", "Native encounter attack aborted: PlayerEncounter.Current is null.");
			return false;
		}
		try
		{
			PartyBase encounteredPartySafe = PlayerEncounterCompat.GetEncounteredPartySafe();
			if (encounteredPartySafe == null)
			{
				try
				{
					encounteredPartySafe = PlayerEncounter.EncounteredParty;
				}
				catch
				{
					encounteredPartySafe = null;
				}
			}
			if (encounteredPartySafe == null || encounteredPartySafe != defenderParty)
			{
				PlayerEncounter.Current.SetupFields(PartyBase.MainParty, defenderParty);
				Logger.Log("MeetingTaunt", "Repaired native encounter fields before direct attack. Defender=" + (defenderParty?.Name?.ToString() ?? "unknown"));
			}
		}
		catch (Exception ex5)
		{
			Logger.Log("MeetingTaunt", "Repairing encounter fields before native attack failed: " + ex5.Message);
		}
		try
		{
			if (TryGetCurrentEncounterBattle() == null)
			{
				PlayerEncounter.StartBattle();
			}
		}
		catch (Exception ex3)
		{
			Logger.Log("MeetingTaunt", "StartBattle for native attack failed: " + ex3.Message);
		}
		if (TryGetCurrentEncounterBattle() == null)
		{
			try
			{
				StartBattleAction.Apply(PartyBase.MainParty, defenderParty);
			}
			catch (Exception ex4)
			{
				Logger.Log("MeetingTaunt", "StartBattleAction fallback for native attack failed: " + ex4.Message);
			}
		}
		if (TryGetCurrentEncounterBattle() == null)
		{
			return false;
		}
		try
		{
			return PlayerEncounterCompat.GetEncounteredPartySafe() != null || PlayerEncounter.EncounteredParty != null;
		}
		catch
		{
			return PlayerEncounterCompat.GetEncounteredPartySafe() != null;
		}
	}

	private static MenuCallbackArgs BuildNativeEncounterAttackMenuArgs()
	{
		MenuContext menuContext = null;
		try
		{
			menuContext = Campaign.Current?.CurrentMenuContext;
		}
		catch
		{
			menuContext = null;
		}
		if (menuContext != null)
		{
			return new MenuCallbackArgs(menuContext, new TextObject(""));
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MapState mapState)
			{
				return new MenuCallbackArgs(mapState, new TextObject(""));
			}
		}
		catch
		{
		}
		return new MenuCallbackArgs((MenuContext)null, new TextObject(""));
	}

	private static bool TryExecuteNativeEncounterAttackNow(Hero target, string reason)
	{
		PartyBase defenderParty;
		if (!TryEnsureEncounterContextForNativeAttack(target, out defenderParty))
		{
			return false;
		}
		try
		{
			SetTarget(target ?? _pendingForceNativeEncounterAttackHero ?? _targetHero);
		}
		catch
		{
		}
		MarkPendingMeetingBattleNativeResult(target ?? _pendingForceNativeEncounterAttackHero ?? _targetHero, defenderParty, reason ?? "native_conversation_taunt_attack");
		try
		{
			Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
		}
		catch
		{
		}
		try
		{
			PlayerEncounter.LeaveEncounter = false;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
		}
		catch
		{
		}
		try
		{
			MeetingBattleRuntime.EndMeeting();
		}
		catch
		{
		}
		ClearMeetingPlayerReleaseAuthorization("native_encounter_attack");
		ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("native_encounter_attack");
		ClearEncounterRedirectSuspension("native_conversation_taunt_attack_refresh");
		DisableCustomEncounterMenuForCurrentEncounter("native_conversation_taunt_attack");
		SuspendEncounterRedirectDuringResultResolution("native_conversation_taunt_attack");
		try
		{
			LordEncounterRedirectGuard.SuppressForSeconds(30f);
		}
		catch
		{
		}
		TryApplyPendingNativeEncounterAttackDiplomacy(reason ?? "native_conversation_taunt_attack");
		try
		{
			MenuCallbackArgs args = BuildNativeEncounterAttackMenuArgs();
			MenuHelper.EncounterAttackConsequence(args);
			Logger.Log("MeetingTaunt", $"Native encounter attack consequence executed. Target={(target ?? _pendingForceNativeEncounterAttackHero ?? _targetHero)?.Name}, Defender={defenderParty?.Name}, Reason={reason ?? "N/A"}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Native encounter attack consequence failed; falling back to direct battle mission open. " + ex.Message);
			try
			{
				OpenBattleMissionFallbackFromEncounter();
				Logger.Log("MeetingTaunt", $"Native encounter fallback attack mission opened. Target={(target ?? _pendingForceNativeEncounterAttackHero ?? _targetHero)?.Name}, Defender={defenderParty?.Name}, Reason={reason ?? "N/A"}");
				return true;
			}
			catch (Exception ex2)
			{
				Logger.Log("MeetingTaunt", "Native direct attack mission open failed: " + ex2.Message);
				return false;
			}
		}
	}

	private static void TryForcePendingNativeEncounterAttackIfReady()
	{
		if (!HasPendingForceNativeEncounterAttack())
		{
			return;
		}
		string reason = _pendingForceNativeEncounterAttackReason ?? "native_conversation_taunt_attack";
		TryApplyPendingNativeEncounterAttackDiplomacy(reason);
		float pendingNativeEncounterAttackElapsedSeconds = GetPendingNativeEncounterAttackElapsedSeconds();
		bool nativeConversationStillActive = IsNativeConversationStillActive();
		if (nativeConversationStillActive && !_pendingForceNativeEncounterAttackConversationEnded && pendingNativeEncounterAttackElapsedSeconds < NativeEncounterAttackDialogDelaySeconds)
		{
			return;
		}
		try
		{
			if (nativeConversationStillActive)
			{
				try
				{
					Campaign.Current.ConversationManager.EndConversation();
				}
				catch
				{
				}
				if (IsNativeConversationStillActive())
				{
					return;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (_pendingForceNativeEncounterAttackLastAttemptTime > 0f && applicationTime - _pendingForceNativeEncounterAttackLastAttemptTime < 0.25f)
			{
				return;
			}
			_pendingForceNativeEncounterAttackLastAttemptTime = applicationTime;
		}
		catch
		{
			_pendingForceNativeEncounterAttackLastAttemptTime = 0f;
		}
		try
		{
			Hero hero = _pendingForceNativeEncounterAttackHero ?? _targetHero;
			if (TryExecuteNativeEncounterAttackNow(hero, reason))
			{
				ClearPendingForceNativeEncounterAttack("executed");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Force pending native encounter attack failed: " + ex.Message);
		}
	}

	internal static void OnEngineTick()
	{
		if (_pendingForceNativeEncounterAttack)
		{
			try
			{
				TryForcePendingNativeEncounterAttackIfReady();
			}
			catch (Exception ex)
			{
				Logger.Log("MeetingTaunt", "Engine tick pending native encounter attack failed: " + ex.Message);
			}
		}
		if (_pendingNativeConversationMeetingRelease)
		{
			try
			{
				TryForcePendingNativeConversationMeetingReleaseIfReady();
			}
			catch (Exception ex2)
			{
				Logger.Log("MeetingRelease", "Engine tick pending native conversation release failed: " + ex2.Message);
			}
		}
	}

	private static void TryForcePendingEncounterBattleMenuIfReady()
	{
		if (!HasPendingForceNativeEncounterBattleMenu())
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		string text = null;
		try
		{
			text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
		}
		catch
		{
			text = null;
		}
		if (text == "encounter")
		{
			ClearPendingForceNativeEncounterBattleMenu("already_in_encounter_menu");
			return;
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = PlayerEncounter.Current != null;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag2 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
		}
		catch
		{
			flag2 = false;
		}
		if (!flag || !flag2)
		{
			float num = 0f;
			try
			{
				if (_pendingForceNativeEncounterBattleMenuAtTime > 0f)
				{
					num = Time.ApplicationTime - _pendingForceNativeEncounterBattleMenuAtTime;
				}
			}
			catch
			{
			}
			if (num > 2.5f)
			{
				ClearPendingForceNativeEncounterBattleMenu("missing_encounter_or_battle_context");
			}
			return;
		}
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (_pendingForceNativeEncounterBattleMenuLastAttemptTime > 0f && applicationTime - _pendingForceNativeEncounterBattleMenuLastAttemptTime < 0.25f)
			{
				return;
			}
			_pendingForceNativeEncounterBattleMenuLastAttemptTime = applicationTime;
		}
		catch
		{
			_pendingForceNativeEncounterBattleMenuLastAttemptTime = 0f;
		}
		try
		{
			DisableCustomEncounterMenuForCurrentEncounter("pending_native_encounter_battle_menu");
			try
			{
				PlayerEncounter.LeaveEncounter = false;
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
			catch
			{
			}
			if (!TryActivateNativeEncounterMenuSafely("pending_native_encounter_battle_menu"))
			{
				return;
			}
			string text2 = null;
			try
			{
				text2 = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
				text2 = null;
			}
			if (text2 == "encounter")
			{
				ClearPendingForceNativeEncounterBattleMenu("encounter_menu_opened");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Force pending encounter battle menu failed: " + ex.Message);
		}
	}

	private static void TryForcePendingReturnToEncounterMenuAfterUnauthorizedMeetingExitIfReady()
	{
		if (!_pendingReturnToEncounterMenuAfterUnauthorizedMeetingExit)
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		bool flag = false;
		try
		{
			object obj = Game.Current?.GameStateManager?.ActiveState;
			flag = obj != null && obj.GetType().Name == "MapState";
		}
		catch
		{
			flag = false;
		}
		if (!flag)
		{
			return;
		}
		bool flag2 = false;
		try
		{
			flag2 = PlayerEncounter.Current != null;
		}
		catch
		{
			flag2 = false;
		}
		if (!flag2)
		{
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("missing_player_encounter");
			return;
		}
		if (IsCustomEncounterMenuHardSuppressedUntilBackOnMap())
		{
			Logger.Log("MeetingRelease", "Skipped forced custom encounter menu return while custom menu hard suppression is active.");
			return;
		}
		try
		{
			ClearCustomEncounterMenuDisable("unauthorized_meeting_exit_return");
		}
		catch
		{
		}
		try
		{
			PlayerEncounter.LeaveEncounter = false;
		}
		catch
		{
		}
		try
		{
			PlayerEncounter.Current.IsPlayerWaiting = false;
		}
		catch
		{
		}
		try
		{
			EnsureEncounterTargetHero("unauthorized_meeting_exit_return");
		}
		catch
		{
		}
		try
		{
			GameMenu.ActivateGameMenu("AnimusForge_lord_encounter");
			if (string.Equals(Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId, "AnimusForge_lord_encounter", StringComparison.Ordinal))
			{
				ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("custom_encounter_menu_opened");
				try
				{
					AnimusForgeQuickInfo.Show("对方并没有同意放你离开。", _targetHero?.CharacterObject);
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingRelease", "Force return to encounter menu after unauthorized meeting exit failed: " + ex.Message);
		}
	}

	private static bool TryInvokeNativeDoPlayerDefeat()
	{
		try
		{
			PlayerEncounter playerEncounter = null;
			try
			{
				playerEncounter = PlayerEncounter.Current;
			}
			catch
			{
				playerEncounter = null;
			}
			if (playerEncounter == null)
			{
				return false;
			}
			if (_playerEncounterDoPlayerDefeatMethod == null)
			{
				_playerEncounterDoPlayerDefeatMethod = typeof(PlayerEncounter).GetMethod("DoPlayerDefeat", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			if (_playerEncounterDoPlayerDefeatMethod == null)
			{
				Logger.Log("LordEncounter", "Native DoPlayerDefeat method not found via reflection.");
				return false;
			}
			_playerEncounterDoPlayerDefeatMethod.Invoke(playerEncounter, null);
			Logger.Log("LordEncounter", "Invoked native PlayerEncounter.DoPlayerDefeat via reflection.");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Invoke native DoPlayerDefeat failed: " + ex.Message);
			return false;
		}
	}

	private static void TryForcePendingDefeatCaptivityMenuIfReady()
	{
		if (!HasPendingForceNativeDefeatCaptivityMenu())
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		string text = null;
		try
		{
			text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
		}
		catch
		{
			text = null;
		}
		bool flag = false;
		try
		{
			flag = Hero.MainHero != null && Hero.MainHero.IsPrisoner;
		}
		catch
		{
			flag = false;
		}
		if ((text == "defeated_and_taken_prisoner" || text == "taken_prisoner") && flag)
		{
			ClearPendingForceNativeDefeatCaptivityMenu("already_in_native_captivity_menu");
			return;
		}
		bool flag2 = false;
		try
		{
			object obj3 = Game.Current?.GameStateManager?.ActiveState;
			flag2 = obj3 != null && obj3.GetType().Name == "MapState";
		}
		catch
		{
			flag2 = false;
		}
		if (!flag2)
		{
			return;
		}
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (_pendingForceNativeDefeatCaptivityLastAttemptTime > 0f && applicationTime - _pendingForceNativeDefeatCaptivityLastAttemptTime < 0.25f)
			{
				return;
			}
			_pendingForceNativeDefeatCaptivityLastAttemptTime = applicationTime;
		}
		catch
		{
			_pendingForceNativeDefeatCaptivityLastAttemptTime = 0f;
		}
		if (TryAdvancePendingDefeatCaptivityThroughNativeEncounter())
		{
			ClearPendingForceNativeDefeatCaptivityMenu("advanced_native_defeat_encounter_flow");
			return;
		}
		bool flag3 = false;
		if (TryInvokeNativeDoPlayerDefeat())
		{
			try
			{
				string text2 = null;
				try
				{
					text2 = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
				}
				catch
				{
					text2 = null;
				}
				bool flag4 = false;
				try
				{
					flag4 = Hero.MainHero != null && Hero.MainHero.IsPrisoner;
				}
				catch
				{
					flag4 = false;
				}
				if ((text2 == "defeated_and_taken_prisoner" || text2 == "taken_prisoner") && flag4)
				{
					ClearPendingForceNativeDefeatCaptivityMenu("native_do_player_defeat_opened_menu");
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("LordEncounter", "Check native DoPlayerDefeat menu result failed: " + ex.Message);
			}
		}
		try
		{
			PartyBase partyBase = ResolvePendingDefeatCaptivityParty();
			if (!flag && partyBase != null)
			{
				try
				{
					TakePrisonerAction.Apply(partyBase, Hero.MainHero);
					flag = true;
				}
				catch (Exception ex2)
				{
					Logger.Log("LordEncounter", "Force pending captivity: TakePrisonerAction failed: " + ex2.Message);
				}
			}
			GameMenu.ActivateGameMenu("taken_prisoner");
			string text3 = null;
			try
			{
				text3 = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
				text3 = null;
			}
			flag3 = (text3 == "taken_prisoner" || text3 == "defeated_and_taken_prisoner") && flag;
			Logger.Log("LordEncounter", $"Forced native captivity fallback attempted. Opened={text3 == "taken_prisoner" || text3 == "defeated_and_taken_prisoner"}, Prisoner={flag}, Captor={partyBase?.Name}, CaptorHero={partyBase?.LeaderHero?.Name}");
			if (flag3)
			{
				ClearPendingForceNativeDefeatCaptivityMenu("fallback_captivity_menu_opened");
			}
			else
			{
				Logger.Log("LordEncounter", "Native captivity menu not ready yet; will retry while pending marker is active.");
			}
		}
		catch (Exception ex3)
		{
			Logger.Log("LordEncounter", "Force pending defeat captivity menu failed: " + ex3.Message);
		}
	}

	private static bool TryAdvanceMeetingVictoryThroughNativeEncounter(Hero target, string reason)
	{
		Logger.Log("LordEncounter", "Skipped unsafe recovered native victory flow. Reason=" + (reason ?? "N/A"));
		return false;
	}

	private static bool TryEnsureEncounterContextForMeetingVictory(Hero target, out PartyBase defenderParty)
	{
		defenderParty = ResolveMeetingVictoryEncounterParty(target);
		if (defenderParty == null || PartyBase.MainParty == null)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForMeetingVictory failed: defender/main party is null.");
			return false;
		}
		if (PlayerEncounter.Current == null)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForMeetingVictory failed: PlayerEncounter.Current is null.");
			return false;
		}
		MapEvent mapEvent = TryGetCurrentEncounterBattle() ?? PlayerEncounterCompat.GetCurrentMapEventSafe();
		if (!IsMapEventSafeForNativeBattleResult(mapEvent, "meeting_victory_context"))
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForMeetingVictory failed: existing battle context is incomplete.");
			return false;
		}
		return true;
	}

	private static bool IsMapEventSafeForNativeBattleResult(MapEvent mapEvent, string reason)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
			BattleSideEnum playerSide = mapEvent.PlayerSide;
			if (playerSide != BattleSideEnum.Attacker && playerSide != BattleSideEnum.Defender)
			{
				return false;
			}
			if (mapEvent.AttackerSide?.LeaderParty == null || mapEvent.DefenderSide?.LeaderParty == null)
			{
				return false;
			}
			if (mapEvent.GetMapEventSide(BattleSideEnum.Attacker)?.LeaderParty == null || mapEvent.GetMapEventSide(BattleSideEnum.Defender)?.LeaderParty == null)
			{
				return false;
			}
			if (mapEvent.GetMapEventSide(playerSide)?.Parties == null || mapEvent.GetMapEventSide(playerSide).Parties.Count <= 0)
			{
				return false;
			}
			BattleSideEnum oppositeSide = playerSide == BattleSideEnum.Attacker ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
			if (mapEvent.GetMapEventSide(oppositeSide)?.Parties == null || mapEvent.GetMapEventSide(oppositeSide).Parties.Count <= 0)
			{
				return false;
			}
			PartyBase leaderParty = mapEvent.GetMapEventSide(oppositeSide).LeaderParty;
			if (leaderParty == null || leaderParty.MapFaction == null || leaderParty.Name == null)
			{
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "MapEvent safety check failed. Reason=" + (reason ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static PartyBase ResolveMeetingVictoryEncounterParty(Hero target)
	{
		PartyBase partyBase = null;
		try
		{
			partyBase = PlayerEncounterCompat.GetEncounteredPartySafe();
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = target?.PartyBelongedTo?.Party;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = _encounterRedirectSuspendedEncounterParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase != null)
		{
			return partyBase;
		}
		try
		{
			partyBase = _encounterRedirectSuspendedEncounterLeader?.PartyBelongedTo?.Party;
		}
		catch
		{
			partyBase = null;
		}
		return partyBase;
	}

	private static void TryResolvePendingDefeatCaptivityImmediately(string reason)
	{
		if (!HasPendingForceNativeDefeatCaptivityMenu())
		{
			return;
		}
		try
		{
			if (TryAdvancePendingDefeatCaptivityThroughNativeEncounter())
			{
				ClearPendingForceNativeDefeatCaptivityMenu("immediate_native_defeat_encounter_flow_" + (reason ?? "unknown"));
				return;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Immediate defeat captivity native encounter attempt failed: " + ex.Message);
		}
		try
		{
			if (TryInvokeNativeDoPlayerDefeat())
			{
				bool flag = false;
				try
				{
					flag = Hero.MainHero != null && Hero.MainHero.IsPrisoner;
				}
				catch
				{
					flag = false;
				}
				string text = null;
				try
				{
					text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
				}
				catch
				{
					text = null;
				}
				if ((text == "taken_prisoner" || text == "defeated_and_taken_prisoner") && flag)
				{
					ClearPendingForceNativeDefeatCaptivityMenu("immediate_native_do_player_defeat_" + (reason ?? "unknown"));
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("LordEncounter", "Immediate defeat captivity DoPlayerDefeat attempt failed: " + ex2.Message);
		}
	}

	private static PartyBase ResolvePendingDefeatCaptivityParty()
	{
		PartyBase partyBase = null;
		try
		{
			partyBase = _pendingForceNativeDefeatCaptivityParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase == null)
		{
			try
			{
				partyBase = _pendingForceNativeDefeatCaptivityHero?.PartyBelongedTo?.Party;
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
			try
			{
				partyBase = _encounterRedirectSuspendedEncounterLeader?.PartyBelongedTo?.Party;
			}
			catch
			{
				partyBase = null;
			}
		}
		return partyBase;
	}

	private static bool TryAdvancePendingDefeatCaptivityThroughNativeEncounter()
	{
		try
		{
			PartyBase partyBase;
			if (!TryEnsureEncounterContextForDefeatCaptivity(out partyBase))
			{
				return false;
			}
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
			MapEvent mapEvent = TryGetCurrentEncounterBattle();
			if (mapEvent == null)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity aborted: battle context is null.");
				return false;
			}
			BattleSideEnum battleSideEnum = PartyBase.MainParty.OpponentSide;
			BattleState winnerSide = ((battleSideEnum != BattleSideEnum.Attacker) ? BattleState.DefenderVictory : BattleState.AttackerVictory);
			try
			{
				mapEvent.SetOverrideWinner(battleSideEnum);
			}
			catch (Exception ex)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity: SetOverrideWinner failed: " + ex.Message);
			}
			try
			{
				PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(winnerSide);
			}
			catch (Exception ex2)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity: set CampaignBattleResult failed: " + ex2.Message);
			}
			if (!TrySetPlayerEncounterState(PlayerEncounter.Current, PlayerEncounterState.PrepareResults))
			{
				return false;
			}
			try
			{
				PlayerEncounter.LeaveEncounter = false;
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.Update();
			}
			catch (Exception ex3)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity: PlayerEncounter.Update failed: " + ex3.Message);
			}
			bool flag = false;
			try
			{
				flag = Hero.MainHero != null && Hero.MainHero.IsPrisoner;
			}
			catch
			{
				flag = false;
			}
			string text = null;
			try
			{
				text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
				text = null;
			}
			bool flag2 = text == "taken_prisoner" || text == "defeated_and_taken_prisoner";
			Logger.Log("LordEncounter", $"Advanced pending defeat through native encounter flow. Menu={text ?? "null"}, Prisoner={flag}, Captor={partyBase?.Name}, PlayerWasAttacker={_pendingForceNativeDefeatCaptivityPlayerWasAttacker}");
			return flag && flag2;
		}
		catch (Exception ex4)
		{
			Logger.Log("LordEncounter", "Advance pending defeat captivity via native encounter failed: " + ex4.Message);
			return false;
		}
	}

	private static bool TryEnsureEncounterContextForDefeatCaptivity(out PartyBase partyBase)
	{
		partyBase = ResolvePendingDefeatCaptivityParty();
		if (partyBase == null || PartyBase.MainParty == null)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForDefeatCaptivity failed: captor/main party is null.");
			return false;
		}
		bool flag = _pendingForceNativeDefeatCaptivityPlayerWasAttacker;
		try
		{
			if (PlayerEncounter.Current != null)
			{
				flag = PlayerEncounter.PlayerIsAttacker;
			}
		}
		catch
		{
		}
		PartyBase partyBase2 = flag ? partyBase : PartyBase.MainParty;
		PartyBase partyBase3 = flag ? PartyBase.MainParty : partyBase;
		try
		{
			PlayerEncounterCompat.RestartPlayerEncounter(partyBase2, partyBase3, forcePlayerOutFromSettlement: false);
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForDefeatCaptivity: RestartPlayerEncounter failed: " + ex.Message);
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounter.Start();
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.SetupFields(partyBase3, partyBase2);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForDefeatCaptivity: Start+SetupFields fallback failed: " + ex2.Message);
		}
		if (PlayerEncounter.Current == null)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForDefeatCaptivity failed: PlayerEncounter.Current is null.");
			return false;
		}
		try
		{
			if (PlayerEncounter.Battle == null && PlayerEncounter.EncounteredBattle == null && MapEvent.PlayerMapEvent == null)
			{
				PlayerEncounter.StartBattle();
			}
		}
		catch (Exception ex3)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForDefeatCaptivity: StartBattle failed: " + ex3.Message);
		}
		return TryGetCurrentEncounterBattle() != null;
	}

	private static bool TrySetPlayerEncounterState(PlayerEncounter playerEncounter, PlayerEncounterState encounterState)
	{
		try
		{
			if (playerEncounter == null)
			{
				return false;
			}
			if (_playerEncounterStateProperty == null)
			{
				_playerEncounterStateProperty = typeof(PlayerEncounter).GetProperty("EncounterState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (_playerEncounterStateProperty == null)
			{
				Logger.Log("LordEncounter", "PlayerEncounter.EncounterState property not found via reflection.");
				return false;
			}
			_playerEncounterStateProperty.SetValue(playerEncounter, encounterState, null);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Set PlayerEncounter.EncounterState via reflection failed: " + ex.Message);
			return false;
		}
	}

	private static bool HasResolvedCampaignBattleResult()
	{
		try
		{
			CampaignBattleResult campaignBattleResult = PlayerEncounter.CampaignBattleResult;
			if (campaignBattleResult == null)
			{
				return false;
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			try
			{
				flag = campaignBattleResult.BattleResolved;
			}
			catch
			{
				flag = false;
			}
			try
			{
				flag2 = campaignBattleResult.EnemyPulledBack;
			}
			catch
			{
				flag2 = false;
			}
			try
			{
				flag3 = campaignBattleResult.EnemyRetreated;
			}
			catch
			{
				flag3 = false;
			}
			return flag || flag2 || flag3;
		}
		catch
		{
			return false;
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		try
		{
			if (MeetingBattleRuntime.IsMeetingActive && mission is Mission mission2)
			{
				bool flag = false;
				try
				{
					flag = mission2.GetMissionBehavior<BattleEndLogic>() != null;
				}
				catch
				{
				}
				if (flag && mission2.GetMissionBehavior<MeetingBattleLockMissionBehavior>() == null)
				{
					Logger.Log("LordEncounter", "Attaching MeetingBattleLockMissionBehavior to native battle mission.");
					mission2.AddMissionBehavior(new MeetingBattleLockMissionBehavior(MeetingBattleRuntime.TargetHero));
				}
				if (flag)
				{
					SuppressCustomEncounterMenuUntilBackOnMap("meeting_battle_mission_started");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "OnMissionStarted failed: " + ex.Message);
		}
	}

	private void TryRunPostMissionCleanupIfReady()
	{
		bool nativeEncounterMenuActive = false;
		try
		{
			nativeEncounterMenuActive = string.Equals(Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId, "encounter", StringComparison.Ordinal);
		}
		catch
		{
			nativeEncounterMenuActive = false;
		}
		if (!_pendingPostMissionCleanup || _pendingPostMissionCleanupDelay > 0f || (Game.Current?.GameStateManager?.ActiveState is MissionState && !(_pendingPeacefulMeetingBattleCleanup && nativeEncounterMenuActive)))
		{
			return;
		}
		if (!_pendingPeacefulMeetingBattleCleanup)
		{
			try
			{
				if (PlayerEncounter.Current != null)
				{
					return;
				}
			}
			catch
			{
			}
		}
		try
		{
			RestoreMainPartyPosition();
		}
		catch
		{
		}
		try
		{
			RunPendingPeacefulMeetingBattleCleanupIfNeeded();
		}
		catch
		{
		}
		try
		{
			DisableMeetingSpawnOverride();
		}
		catch
		{
		}
		try
		{
			FocusMapCameraOnMainParty();
		}
		catch
		{
		}
		_pendingPostMissionCleanup = false;
		_pendingPostMissionCleanupDelay = 0f;
	}

	internal static bool TryResolvePendingPeacefulMeetingCleanupForExternal(string reason)
	{
		if (!_pendingPeacefulMeetingBattleCleanup && !_pendingPostMissionCleanup)
		{
			return false;
		}
		if (!_pendingPeacefulMeetingBattleCleanup)
		{
			return false;
		}
		Logger.Log("MeetingBattle", "Resolving pending peaceful meeting cleanup immediately. Reason=" + (reason ?? "N/A"));
		try
		{
			RestoreMainPartyPosition();
		}
		catch
		{
		}
		try
		{
			RunPendingPeacefulMeetingBattleCleanupIfNeeded();
		}
		catch
		{
		}
		try
		{
			DisableMeetingSpawnOverride();
		}
		catch
		{
		}
		try
		{
			ClearEncounterRedirectSuspension("peaceful_meeting_cleanup_" + (reason ?? "unknown"));
		}
		catch
		{
		}
		try
		{
			ClearCustomEncounterMenuHardSuppression("peaceful_meeting_cleanup_" + (reason ?? "unknown"));
		}
		catch
		{
		}
		try
		{
			FocusMapCameraOnMainParty();
		}
		catch
		{
		}
		_pendingPostMissionCleanup = _pendingPeacefulMeetingBattleCleanup;
		_pendingPostMissionCleanupDelay = 0f;
		return true;
	}

	private static void RunPendingPeacefulMeetingBattleCleanupIfNeeded()
	{
		if (!_pendingPeacefulMeetingBattleCleanup)
		{
			return;
		}
		bool pendingPeacefulMeetingBattleCleanup = false;
		try
		{
			if (PlayerEncounter.Current != null)
			{
				Logger.Log("MeetingBattle", "Peaceful meeting exit detected. Clearing temporary encounter-battle state.");
				try
				{
					PlayerEncounter.CampaignBattleResult = null;
				}
				catch
				{
				}
				try
				{
					PlayerEncounter.Current.FinalizeBattle();
				}
				catch
				{
				}
				try
				{
					PlayerEncounter.LeaveEncounter = true;
				}
				catch
				{
				}
				try
				{
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
				catch
				{
				}
				try
				{
					PlayerEncounter.Update();
				}
				catch
				{
				}
				try
				{
					PlayerEncounter.Finish();
				}
				catch
				{
				}
				try
				{
					PlayerEncounter.Finish(true);
				}
				catch
				{
				}
				bool flag = false;
				try
				{
					flag = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
				}
				catch
				{
					flag = false;
				}
				if (flag)
				{
					pendingPeacefulMeetingBattleCleanup = true;
					Logger.Log("MeetingBattle", "Peaceful cleanup incomplete; will retry on next campaign tick.");
				}
			}
			if (_pendingMeetingReleaseSafePassageFinalReapply)
			{
				try
				{
					// FinalizeBattle/Finish can restore the party's old movement state.
					// Apply the native safe-passage state only after this custom cleanup.
					ApplyMeetingPlayerReleaseWorldMapCooldown(_pendingMeetingReleaseSafePassageHero, (_pendingMeetingReleaseSafePassageReason ?? "meeting_release") + "_after_final_cleanup", _pendingMeetingReleaseSafePassageParty);
				}
				catch
				{
				}
				if (!pendingPeacefulMeetingBattleCleanup)
				{
					ClearMeetingReleaseSafePassageFinalReapply("final_meeting_cleanup_finished");
				}
			}
		}
		finally
		{
			_pendingPeacefulMeetingBattleCleanup = pendingPeacefulMeetingBattleCleanup;
		}
	}

	private void AddConversationOptions(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("AnimusForge_meet_talk", "lord_talk_ask_something_2", "lord_talk_ask_something_2", "Let's talk.", null, null);
		starter.AddPlayerLine("AnimusForge_show_item", "lord_talk_ask_something_2", "AnimusForge_show_item_response", "I want to show you something.", null, null);
		starter.AddDialogLine("AnimusForge_show_item_response", "AnimusForge_show_item_response", "lord_start", "Oh? What is it?", null, null);
		starter.AddPlayerLine("AnimusForge_give_item", "lord_talk_ask_something_2", "AnimusForge_give_item_response", "I have something for you.", null, null);
		starter.AddDialogLine("AnimusForge_give_item_response", "AnimusForge_give_item_response", "lord_start", "Thank you, I will take a look.", null, null);
	}

	public static bool OpenEncounterMenu(Hero target)
	{
		if (target == null)
		{
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "target_null");
			return false;
		}
		LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "enter", null, target);
		if (IsNativeSettlementRequestMeetingContext(target))
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because this is a native hostile settlement request meeting. Target={target.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_native_settlement_request_meeting", null, target);
			return false;
		}
		PartyBase encounterParty = GetCurrentEncounterPartySafe();
		if (!IsEligibleCustomLordEncounterTarget(target, encounterParty))
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because target is not an eligible kingdom noble encounter. Target={target.Name}, Party={encounterParty?.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_ineligible_target", null, target, encounterParty);
			return false;
		}
		if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea(target))
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because current encounter is at sea. Target={target.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_sea_context", null, target, encounterParty);
			return false;
		}
		if (HasPendingForceNativeEncounterAttack())
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because native encounter attack is pending. Target={target.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_pending_attack", null, target, encounterParty);
			return false;
		}
		if (IsNativeEncounterActivityContext(target))
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because current encounter is a native siege or village activity context. Target={target.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_native_activity_context", null, target, encounterParty);
			return false;
		}
		if (IsCustomEncounterMenuDisabledForCurrentEncounter())
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because custom encounter menu is disabled. Target={target.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_custom_disabled", null, target, encounterParty);
			return false;
		}
		if (IsEncounterRedirectSuspended())
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because redirect is suspended. Target={target.Name}");
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_redirect_suspended", null, target, encounterParty);
			return false;
		}
		try
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter)
			{
				Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because native encounter leave is pending. Target={target.Name}");
				LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_leave_encounter", null, target, encounterParty);
				return false;
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.PlayerSurrender)
			{
				Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because native player surrender is pending. Target={target.Name}");
				LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "ignore_player_surrender", null, target, encounterParty);
				return false;
			}
		}
		catch
		{
		}
		SetTarget(target);
		try
		{
			try
			{
				LordEncounterRedirectGuard.Clear();
			}
			catch
			{
			}
			LogEncounterDiagnostic("LordEncounter.OpenEncounterMenu", "activate_custom_menu", "AnimusForge_lord_encounter", target, encounterParty);
			GameMenu.ActivateGameMenu("AnimusForge_lord_encounter");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Failed to activate menu: " + ex.Message);
			Logger.LogImmediate("Logic", "[EncounterDiag] stage=LordEncounter.OpenEncounterMenu | reason=activate_exception | error=" + ex);
			return false;
		}
	}

	public static void SetTarget(Hero target)
	{
		_targetHero = target;
	}

	internal static bool IsEligibleCustomLordEncounterTarget(Hero hero, PartyBase encounterParty = null)
	{
		if (hero == null || hero == Hero.MainHero || !hero.IsLord || hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
		{
			return false;
		}
		Clan clan = null;
		try
		{
			clan = hero.Clan;
		}
		catch
		{
			clan = null;
		}
		if (IsExcludedCustomLordClan(clan))
		{
			return false;
		}
		try
		{
			if (encounterParty != null)
			{
				Hero partyLeader = encounterParty.LeaderHero;
				if (partyLeader != null && partyLeader != hero && !IsEncounterArmyMemberTarget(hero, encounterParty))
				{
					return false;
				}
				IFaction partyFaction = encounterParty.MapFaction;
				if (!IsEligibleCustomLordEncounterFaction(partyFaction, clan))
				{
					return false;
				}
				if (clan.Kingdom != null && partyFaction != null && partyFaction != clan.Kingdom && !(IsEligibleMercenaryClanForCustomLordEncounter(clan) && partyFaction == clan))
				{
					return false;
				}
			}
		}
		catch
		{
			return false;
		}
		return true;
	}

	private static bool IsEncounterArmyMemberTarget(Hero hero, PartyBase encounterParty)
	{
		if (hero == null || encounterParty == null || !encounterParty.IsMobile)
		{
			return false;
		}
		try
		{
			MobileParty targetParty = hero.PartyBelongedTo;
			MobileParty encounterMobileParty = encounterParty.MobileParty;
			if (targetParty == null || encounterMobileParty == null || targetParty.LeaderHero != hero)
			{
				return false;
			}
			Army army = encounterMobileParty.Army;
			if (army == null || targetParty.Army != army)
			{
				return false;
			}
			MobileParty leaderParty = army.LeaderParty;
			if (leaderParty == null)
			{
				return false;
			}
			return targetParty == leaderParty || leaderParty.AttachedParties.Contains(targetParty);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsExcludedCustomLordClan(Clan clan)
	{
		if (clan == null)
		{
			return true;
		}
		try
		{
			bool eligibleMercenary = IsEligibleMercenaryClanForCustomLordEncounter(clan);
			// A mercenary clan keeps its static IsClanTypeMercenary identity while it is
			// between contracts, so its Kingdom can legitimately be null here.
			if (eligibleMercenary)
			{
				return false;
			}
			if (!clan.IsNoble || clan.Kingdom == null || clan.IsBanditFaction || clan.IsMinorFaction || clan.IsOutlaw)
			{
				return true;
			}
			return IsExcludedCustomLordMapFaction(clan.Kingdom);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsEligibleCustomLordEncounterFaction(IFaction faction, Clan clan)
	{
		if (faction == null)
		{
			return false;
		}
		if (!IsExcludedCustomLordMapFaction(faction))
		{
			return true;
		}
		return IsEligibleMercenaryClanForCustomLordEncounter(clan) && faction == clan;
	}

	private static bool IsEligibleMercenaryClanForCustomLordEncounter(Clan clan)
	{
		if (clan == null)
		{
			return false;
		}
		try
		{
			return (clan.IsClanTypeMercenary || clan.IsUnderMercenaryService)
				&& !clan.IsEliminated
				&& !clan.IsBanditFaction
				&& !clan.IsOutlaw;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsExcludedCustomLordMapFaction(IFaction faction)
	{
		if (faction == null)
		{
			return true;
		}
		try
		{
			return faction.IsBanditFaction || faction.IsMinorFaction || faction.IsOutlaw || !faction.IsKingdomFaction;
		}
		catch
		{
			return true;
		}
	}

	internal static void SuspendEncounterRedirectDuringResultResolution(string reason)
	{
		if (!_suspendEncounterRedirectDuringResultResolution)
		{
			_suspendEncounterRedirectDuringResultResolution = true;
			try
			{
				_encounterRedirectSuspendUntilTime = (_encounterRedirectSuspendSinceTime = Time.ApplicationTime) + 12f;
			}
			catch
			{
				_encounterRedirectSuspendSinceTime = -1f;
				_encounterRedirectSuspendUntilTime = -1f;
			}
			try
			{
				_encounterRedirectSuspendedEncounterParty = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				_encounterRedirectSuspendedEncounterParty = null;
			}
			try
			{
				_encounterRedirectSuspendedEncounterLeader = PlayerEncounter.EncounteredParty?.LeaderHero ?? _targetHero;
			}
			catch
			{
				_encounterRedirectSuspendedEncounterLeader = _targetHero;
			}
			Logger.Log("LordEncounter", "Suspending encounter menu redirect until encounter fully resolves. Reason=" + (reason ?? "N/A"));
		}
	}

	internal static void ClearEncounterRedirectSuspension(string reason)
	{
		if (_suspendEncounterRedirectDuringResultResolution)
		{
			_suspendEncounterRedirectDuringResultResolution = false;
			_encounterRedirectSuspendSinceTime = -1f;
			_encounterRedirectSuspendUntilTime = -1f;
			_encounterRedirectSuspendedEncounterLeader = null;
			_encounterRedirectSuspendedEncounterParty = null;
			Logger.Log("LordEncounter", "Encounter redirect suspension cleared. Reason=" + (reason ?? "N/A"));
		}
	}

	internal static bool IsEncounterRedirectSuspended()
	{
		if (!_suspendEncounterRedirectDuringResultResolution)
		{
			return false;
		}
		try
		{
			float num = 0f;
			try
			{
				num = Time.ApplicationTime;
			}
			catch
			{
				num = 0f;
			}
			float num2 = ((_encounterRedirectSuspendSinceTime > 0f) ? (num - _encounterRedirectSuspendSinceTime) : 999f);
			Hero hero = null;
			PartyBase partyBase = null;
			try
			{
				hero = PlayerEncounter.EncounteredParty?.LeaderHero;
			}
			catch
			{
				hero = null;
			}
			try
			{
				partyBase = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				partyBase = null;
			}
			if (_encounterRedirectSuspendedEncounterParty != null && partyBase != null && partyBase != _encounterRedirectSuspendedEncounterParty)
			{
				ClearEncounterRedirectSuspension("encounter_party_changed");
				return false;
			}
			if (_encounterRedirectSuspendedEncounterLeader != null && hero != null && hero != _encounterRedirectSuspendedEncounterLeader)
			{
				ClearEncounterRedirectSuspension("encounter_target_changed");
				return false;
			}
			if (PlayerEncounter.Current == null)
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				try
				{
					flag = Game.Current?.GameStateManager?.ActiveState is MissionState;
				}
				catch
				{
					flag = false;
				}
				try
				{
					flag2 = PlayerEncounter.CampaignBattleResult != null;
				}
				catch
				{
					flag2 = false;
				}
				try
				{
					flag3 = MeetingBattleRuntime.IsMeetingActive;
				}
				catch
				{
					flag3 = false;
				}
				if (!flag && !flag2 && !flag3 && num2 >= 1.5f)
				{
					ClearEncounterRedirectSuspension("no_active_encounter_grace_elapsed");
					return false;
				}
				if (_encounterRedirectSuspendUntilTime > 0f && num <= _encounterRedirectSuspendUntilTime)
				{
					return true;
				}
				if (_encounterRedirectSuspendUntilTime > 0f && num > _encounterRedirectSuspendUntilTime)
				{
					ClearEncounterRedirectSuspension("suspension_window_elapsed");
					return false;
				}
				ClearEncounterRedirectSuspension("no_active_player_encounter");
				return false;
			}
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			try
			{
				flag4 = PlayerEncounterCompat.HasResolvedEncounterBattleContext();
			}
			catch
			{
				flag4 = false;
			}
			try
			{
				flag5 = PlayerEncounter.CampaignBattleResult != null;
			}
			catch
			{
				flag5 = false;
			}
			try
			{
				flag6 = Game.Current?.GameStateManager?.ActiveState is MissionState;
			}
			catch
			{
				flag6 = false;
			}
			try
			{
				PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
				flag7 = encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait;
			}
			catch
			{
				flag7 = false;
			}
			if (!(flag4 || flag5 || flag6 || flag7) && !MeetingBattleRuntime.IsMeetingActive)
			{
				ClearEncounterRedirectSuspension("active_encounter_no_result_context");
				return false;
			}
		}
		catch
		{
			_suspendEncounterRedirectDuringResultResolution = false;
			_encounterRedirectSuspendSinceTime = -1f;
			_encounterRedirectSuspendUntilTime = -1f;
			_encounterRedirectSuspendedEncounterLeader = null;
			_encounterRedirectSuspendedEncounterParty = null;
			return false;
		}
		return true;
	}

	private static bool IsHostileEncounterInitiatedByOpponent()
	{
		try
		{
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
			if (!PlayerEncounter.PlayerIsDefender)
			{
				return false;
			}
			PartyBase partyBase = GetCurrentEncounterPartySafe();
			if (IsBanditOrOutlawEncounterParty(partyBase))
			{
				// Bandit factions are not guaranteed to report a formal faction war,
				// but PlayerIsDefender means this party initiated the encounter.
				return true;
			}
			IFaction faction = null;
			IFaction faction2 = null;
			try
			{
				faction = PartyBase.MainParty?.MapFaction;
			}
			catch
			{
			}
			try
			{
				faction2 = partyBase?.MapFaction ?? PlayerEncounter.EncounteredParty?.MapFaction;
			}
			catch
			{
			}
			if (faction == null || faction2 == null)
			{
				return true;
			}
			bool flag = false;
			try
			{
				flag = faction.IsAtWarWith(faction2) || faction2.IsAtWarWith(faction);
			}
			catch
			{
				flag = false;
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsBanditOrOutlawEncounterParty(PartyBase party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			MobileParty mobileParty = party.MobileParty;
			return party.MapFaction?.IsBanditFaction == true
				|| mobileParty?.IsBandit == true
				|| mobileParty?.MapFaction?.IsBanditFaction == true
				|| mobileParty?.ActualClan?.IsBanditFaction == true;
		}
		catch
		{
			return false;
		}
	}

	private static Hero TryResolveEncounterLeaderHero(PartyBase encounteredParty = null)
	{
		try
		{
			encounteredParty ??= PlayerEncounter.EncounteredParty;
			Hero hero = encounteredParty?.LeaderHero;
			if (IsEligibleCustomLordEncounterTarget(hero, encounteredParty))
			{
				return hero;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Hero EnsureEncounterTargetHero(string reason)
	{
		PartyBase encounteredParty = GetCurrentEncounterPartySafe();
		if (_targetHero != null && IsEligibleCustomLordEncounterTarget(_targetHero, encounteredParty))
		{
			return _targetHero;
		}
		Hero hero = TryResolveEncounterLeaderHero(encounteredParty);
		if (hero != null)
		{
			if (_targetHero != hero)
			{
				Logger.Log("LordEncounter", string.Format("Refreshed encounter target from active encounter. Reason={0}, Target={1}", reason ?? "N/A", hero.Name));
			}
			_targetHero = hero;
			return _targetHero;
		}
		if (_targetHero != null)
		{
			Logger.Log("LordEncounter", "Clearing stale encounter target. Reason=" + (reason ?? "N/A"));
			_targetHero = null;
		}
		return _targetHero;
	}

	private static void EnsureMapCameraReflectionInitialized()
	{
		if (_mapCameraViewType != null)
		{
			return;
		}
		try
		{
			_mapCameraViewType = Type.GetType("SandBox.View.Map.MapCameraView, SandBox.View");
			_mapCameraViewInstanceProperty = _mapCameraViewType?.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			_mapCameraViewTeleportToMainPartyMethod = _mapCameraViewType?.GetMethod("TeleportCameraToMainParty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch
		{
			_mapCameraViewType = null;
			_mapCameraViewInstanceProperty = null;
			_mapCameraViewTeleportToMainPartyMethod = null;
		}
	}

	private static void FocusMapCameraOnMainParty()
	{
		try
		{
			if (MobileParty.MainParty?.Party != null)
			{
				Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
			}
		}
		catch
		{
		}
		try
		{
			EnsureMapCameraReflectionInitialized();
			object obj2 = _mapCameraViewInstanceProperty?.GetValue(null, null);
			if (obj2 != null)
			{
				_mapCameraViewTeleportToMainPartyMethod?.Invoke(obj2, null);
			}
		}
		catch
		{
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		string text = null;
		try
		{
			text = args?.MenuContext?.GameMenu?.StringId;
		}
		catch
		{
			text = null;
		}
		if (IsNativeSettlementRequestMeetingMenu(text))
		{
			IsNativeSettlementRequestMeetingContext();
		}
		else if (text != "AnimusForge_lord_encounter")
		{
			ClearNativeSettlementRequestMeetingContext("game_menu_opened_" + (text ?? "null"));
		}
		if (text == "encounter" && TryResolvePendingPeacefulMeetingCleanupForExternal("game_menu_opened_encounter"))
		{
			return;
		}
		if (args?.MenuContext?.GameMenu?.StringId == "AnimusForge_lord_encounter")
		{
			if (IsCustomEncounterMenuHardSuppressedUntilBackOnMap())
			{
				TryActivateNativeEncounterMenuSafely("hard_suppression_menu_opened");
				return;
			}
			if (HasPendingMeetingBattleNativeResult())
			{
				TryForcePendingMeetingBattleNativeResultIfReady("custom_menu_opened");
				return;
			}
			if (TryResolveNativePlayerSurrenderFromCustomMenu("menu_opened"))
			{
				return;
			}
			if (TryFinishNativeLeaveEncounterFromCustomMenu("menu_opened"))
			{
				return;
			}
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				TryForcePendingDefeatCaptivityMenuIfReady();
				return;
			}
			Hero hero = EnsureEncounterTargetHero("menu_opened");
			if (IsNativeSettlementRequestMeetingContext(hero))
			{
				Logger.Log("LordEncounter", "Custom encounter menu opened during a native hostile settlement request meeting; leaving vanilla meeting flow intact.");
				return;
			}
			if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea(hero))
			{
				TryActivateNativeEncounterMenuSafely("sea_custom_menu_opened");
				return;
			}
			TryRunPostMissionCleanupIfReady();
			_cameraLockWasActive = true;
			FocusMapCameraOnMainParty();
		}
	}

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("LordEncounter.OnCampaignTick"))
		{
		if (_suspendEncounterRedirectDuringResultResolution)
		{
			TryClearEncounterRedirectSuspensionWhenBackOnMap();
		}
		if (_pendingMeetingBattleNativeResult)
		{
			TryForcePendingMeetingBattleNativeResultIfReady("campaign_tick");
		}
		if (_pendingForceNativeDefeatCaptivityMenu)
		{
			TryForcePendingDefeatCaptivityMenuIfReady();
		}
		if (_pendingForceNativeEncounterAttack)
		{
			TryForcePendingNativeEncounterAttackIfReady();
		}
		if (_pendingNativeConversationMeetingRelease)
		{
			TryForcePendingNativeConversationMeetingReleaseIfReady();
		}
		if (_pendingNativeConversationNpcSurrender)
		{
			TryForcePendingNativeConversationNpcSurrenderIfReady();
		}
		if (_pendingForceNativeEncounterBattleMenu)
		{
			TryForcePendingEncounterBattleMenuIfReady();
		}
		if (_pendingReturnToEncounterMenuAfterUnauthorizedMeetingExit)
		{
			TryForcePendingReturnToEncounterMenuAfterUnauthorizedMeetingExitIfReady();
		}
		try
		{
			// This also clears a completed encounter suppression after returning to the map.
			IsCustomEncounterMenuDisabledForCurrentEncounter();
		}
		catch
		{
		}
		if (_pendingPostMissionCleanup)
		{
			_pendingPostMissionCleanupDelay -= dt;
			if (_pendingPostMissionCleanupDelay < 0f)
			{
				_pendingPostMissionCleanupDelay = 0f;
			}
			TryRunPostMissionCleanupIfReady();
		}
		string text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
		if (IsNativeSettlementRequestMeetingMenu(text))
		{
			IsNativeSettlementRequestMeetingContext();
		}
		if (!(text == "AnimusForge_lord_encounter"))
		{
			if (_cameraLockWasActive)
			{
				_cameraLockWasActive = false;
			}
			return;
		}
		if (IsCustomEncounterMenuHardSuppressedUntilBackOnMap())
		{
			TryActivateNativeEncounterMenuSafely("hard_suppression_campaign_tick");
			return;
		}
		if (HasPendingMeetingBattleNativeResult())
		{
			TryForcePendingMeetingBattleNativeResultIfReady("custom_menu_tick");
			return;
		}
		if (TryResolveNativePlayerSurrenderFromCustomMenu("campaign_tick"))
		{
			return;
		}
		if (TryFinishNativeLeaveEncounterFromCustomMenu("campaign_tick"))
		{
			return;
		}
		if (IsNativeSettlementRequestMeetingContext(EnsureEncounterTargetHero("native_settlement_request_meeting_custom_menu_tick")))
		{
			Logger.Log("LordEncounter", "Custom encounter menu tick ignored during a native hostile settlement request meeting.");
			return;
		}
		if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea(EnsureEncounterTargetHero("sea_custom_menu_tick")))
		{
			TryActivateNativeEncounterMenuSafely("sea_custom_menu_tick");
			return;
		}
		if (_targetHero == null)
		{
			EnsureEncounterTargetHero("menu_tick_recover");
		}
		_cameraLockWasActive = true;
		FocusMapCameraOnMainParty();
		}
	}

	private static bool TryResolveNativePlayerSurrenderFromCustomMenu(string reason)
	{
		try
		{
			if (PlayerEncounter.Current == null || !PlayerEncounter.PlayerSurrender)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				TryForcePendingDefeatCaptivityMenuIfReady();
				return IsNativeCaptivityMenuActive();
			}
		}
		catch
		{
		}
		Logger.Log("LordEncounter", "Resolving native player surrender from custom menu. Reason=" + (reason ?? "N/A"));
		try
		{
			PlayerEncounter.LeaveEncounter = false;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
		}
		catch
		{
		}
		try
		{
			PlayerEncounter.Update();
			if (IsNativeCaptivityMenuActive())
			{
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Native player surrender update failed: " + ex.Message);
		}
		try
		{
			if (TryInvokeNativeDoPlayerDefeat() && IsNativeCaptivityMenuActive())
			{
				return true;
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("LordEncounter", "Native player surrender DoPlayerDefeat fallback failed: " + ex2.Message);
		}
		MarkPendingForceNativeDefeatCaptivityMenu("native_player_surrender_" + (reason ?? "unknown"));
		TryResolvePendingDefeatCaptivityImmediately("native_player_surrender_" + (reason ?? "unknown"));
		TryForcePendingDefeatCaptivityMenuIfReady();
		return IsNativeCaptivityMenuActive();
	}

	private static bool IsNativeCaptivityMenuActive()
	{
		try
		{
			string text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			return text == "taken_prisoner" || text == "defeated_and_taken_prisoner";
		}
		catch
		{
			return false;
		}
	}

	private static bool TryFinishNativeLeaveEncounterFromCustomMenu(string reason)
	{
		try
		{
			if (PlayerEncounter.Current == null || !PlayerEncounter.LeaveEncounter)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			Logger.Log("LordEncounter", "Finishing native leave encounter from custom menu. Reason=" + (reason ?? "N/A"));
			PlayerEncounter.Finish(true);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Failed to finish native leave encounter from custom menu. Reason=" + (reason ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static void TryClearEncounterRedirectSuspensionWhenBackOnMap()
	{
		if (!_suspendEncounterRedirectDuringResultResolution)
		{
			return;
		}
		if (HasPendingMeetingBattleNativeResult())
		{
			return;
		}
		try
		{
			if (MeetingBattleRuntime.IsMeetingActive)
			{
				return;
			}
		}
		catch
		{
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		try
		{
			flag = PlayerEncounter.Current != null;
		}
		catch
		{
			flag = false;
		}
		try
		{
			flag2 = Game.Current?.GameStateManager?.ActiveState is MissionState;
		}
		catch
		{
			flag2 = false;
		}
		try
		{
			flag3 = PlayerEncounter.CampaignBattleResult != null;
		}
		catch
		{
			flag3 = false;
		}
		if (!(flag || flag2 || flag3))
		{
			float num = 0f;
			try
			{
				num = Time.ApplicationTime;
			}
			catch
			{
				num = 0f;
			}
			float num2 = ((_encounterRedirectSuspendSinceTime > 0f) ? (num - _encounterRedirectSuspendSinceTime) : 999f);
			if (!(num2 < 0.8f))
			{
				ClearEncounterRedirectSuspension("campaign_tick_back_on_map");
			}
		}
	}

	private static void ApplyLordEncounterMenuBackground(MenuCallbackArgs args, Hero target)
	{
		if (args?.MenuContext == null)
		{
			return;
		}
		try
		{
			string text = null;
			PartyBase partyBase = null;
			MobileParty mobileParty = null;
			bool flag = false;
			try
			{
				partyBase = PlayerEncounter.EncounteredParty;
			}
			catch
			{
			}
			try
			{
				mobileParty = partyBase?.MobileParty;
			}
			catch
			{
			}
			try
			{
				flag = PlayerEncounter.IsNavalEncounter();
			}
			catch
			{
			}
			if (mobileParty != null)
			{
				if (flag && (mobileParty.IsVillager || mobileParty.IsCaravan || partyBase?.MapFaction == null))
				{
					text = "encounter_naval";
				}
				else if (mobileParty.IsVillager)
				{
					text = "encounter_peasant";
				}
				else if (mobileParty.IsCaravan)
				{
					text = "encounter_caravan";
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				CultureObject cultureObject = null;
				try
				{
					cultureObject = partyBase?.MapFaction?.Culture;
				}
				catch
				{
					cultureObject = null;
				}
				if (cultureObject == null)
				{
					try
					{
						cultureObject = target?.MapFaction?.Culture;
					}
					catch
					{
						cultureObject = null;
					}
				}
				if (cultureObject == null)
				{
					try
					{
						cultureObject = Hero.MainHero?.MapFaction?.Culture;
					}
					catch
					{
						cultureObject = null;
					}
				}
				if (cultureObject != null)
				{
					text = MenuHelper.GetEncounterCultureBackgroundMesh(cultureObject);
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				text = "encounter_caravan";
			}
			args.MenuContext.SetBackgroundMeshName(text);
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "ApplyLordEncounterMenuBackground failed: " + ex.Message);
		}
	}

	private static bool IsMainHeroHealthTooLowForMeeting()
	{
		Hero mainHero = Hero.MainHero;
		return mainHero != null && mainHero.MaxHitPoints > 0 && (float)mainHero.HitPoints / mainHero.MaxHitPoints < PlayerMeetingMinimumHealthRatio;
	}

	private static bool IsTargetHeroHealthTooLowForMeeting(Hero target)
	{
		return target != null && target != Hero.MainHero && target.IsLord && target.MaxHitPoints > 0 && (float)target.HitPoints / target.MaxHitPoints < PlayerMeetingMinimumHealthRatio;
	}

	private static string GetLowHealthMeetingBlockedMessage(Hero target)
	{
		string text = target?.Name?.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "该NPC";
		}
		return "你的健康状况不允许你与" + text + "会面";
	}

	private static string GetTargetLowHealthMeetingBlockedMessage(Hero target)
	{
		string text = target?.Name?.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "该领主";
		}
		return text + " 的血量低于 21%，暂时无法与你会面。";
	}

	private static void DisplayLowHealthMeetingBlockedMessageOnce(Hero target, string message)
	{
		string text = target?.StringId;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = target?.Name?.ToString() ?? "__unknown__";
		}
		if (string.Equals(_lastLowHealthMeetingBlockedHeroId, text, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		_lastLowHealthMeetingBlockedHeroId = text;
		InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
	}

	private static void ClearLowHealthMeetingBlockedMessageState()
	{
		_lastLowHealthMeetingBlockedHeroId = null;
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("AnimusForge_lord_encounter", "{MENU_BODY_TEXT}", delegate(MenuCallbackArgs args)
		{
			if (TryResolveNativePlayerSurrenderFromCustomMenu("menu_init"))
			{
				return;
			}
			if (TryFinishNativeLeaveEncounterFromCustomMenu("menu_init"))
			{
				return;
			}
			Hero hero = EnsureEncounterTargetHero("menu_init");
			bool flag = HasPendingForceNativeDefeatCaptivityMenu();
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
			TextObject bodyText;
			if (flag)
			{
				args.MenuTitle = new TextObject("遭遇结果");
				bodyText = new TextObject("正在进入原版被俘结算。");
			}
			else if (ProactiveNpcRequestBehavior.TryBuildMenuText(hero, out var proactiveTitle, out var proactiveBody))
			{
				args.MenuTitle = new TextObject(proactiveTitle);
				bodyText = new TextObject(proactiveBody);
			}
			else
			{
				args.MenuTitle = new TextObject("遭遇领主");
				TextObject content = (IsHostileEncounterInitiatedByOpponent() ? new TextObject("对方试图向你发动进攻。") : new TextObject(""));
				GameTexts.SetVariable("ENCOUNTER_INTENT", content);
				bodyText = new TextObject("你在荒野中遇到了{TARGET_NAME}。{ENCOUNTER_INTENT}");
			}
			GameTexts.SetVariable("MENU_BODY_TEXT", bodyText);
			ApplyLordEncounterMenuBackground(args, hero);
			FocusMapCameraOnMainParty();
		});
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "meet_lord", "{MEET_LORD_LABEL}", delegate(MenuCallbackArgs args)
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = EnsureEncounterTargetHero("menu_meet_condition");
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
			GameTexts.SetVariable("MEET_LORD_LABEL", new TextObject("与{TARGET_NAME}会面"));
			if (hero == null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。");
			}
			else if (IsMainHeroHealthTooLowForMeeting())
			{
				args.IsEnabled = false;
				string text = GetLowHealthMeetingBlockedMessage(hero);
				args.Tooltip = new TextObject(text);
				DisplayLowHealthMeetingBlockedMessageOnce(hero, text);
			}
			else if (IsTargetHeroHealthTooLowForMeeting(hero))
			{
				args.IsEnabled = false;
				string text2 = GetTargetLowHealthMeetingBlockedMessage(hero);
				args.Tooltip = new TextObject(text2);
				DisplayLowHealthMeetingBlockedMessageOnce(hero, text2);
			}
			else
			{
				ClearLowHealthMeetingBlockedMessageState();
			}
			return true;
		}, delegate(MenuCallbackArgs args)
		{
			Hero hero = EnsureEncounterTargetHero("menu_meet_click");
			if (hero == null)
			{
				Logger.Log("LordEncounter", "Meet option clicked but target hero is null after refresh.");
				AnimusForgeQuickInfo.Show("当前未识别到遭遇领主，请先离开并重新接触。");
				return;
			}
			if (IsMainHeroHealthTooLowForMeeting())
			{
				AnimusForgeQuickInfo.Show(GetLowHealthMeetingBlockedMessage(hero), hero.CharacterObject);
				return;
			}
			if (IsTargetHeroHealthTooLowForMeeting(hero))
			{
				AnimusForgeQuickInfo.Show(GetTargetLowHealthMeetingBlockedMessage(hero), hero.CharacterObject);
				return;
			}
			IsOpeningConversation = true;
			try
			{
				ProactiveNpcRequestBehavior.MarkSceneConversationOpening(hero);
				StartMeeting(hero, args);
			}
			finally
			{
				IsOpeningConversation = false;
			}
		});
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "native_dialogue_lord", "{NATIVE_DIALOGUE_LABEL}", delegate(MenuCallbackArgs args)
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = EnsureEncounterTargetHero("menu_native_dialogue_condition");
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
			GameTexts.SetVariable("NATIVE_DIALOGUE_LABEL", ProactiveNpcRequestBehavior.IsActiveRequestHero(hero) ? new TextObject("进入对话") : new TextObject("进入原版对话"));
			if (hero == null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。");
			}
			return true;
		}, delegate
		{
			Hero hero = EnsureEncounterTargetHero("menu_native_dialogue_click");
			if (hero == null)
			{
				Logger.Log("LordEncounter", "Native dialogue option clicked but target hero is null after refresh.");
				AnimusForgeQuickInfo.Show("当前未识别到遭遇领主，请先离开并重新接触。");
				return;
			}
			ProactiveNpcRequestBehavior.MarkNativeConversationOpening(hero);
			OpenNativeEncounterConversation(hero);
		});
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "attack_lord", "{PRIMARY_ACTION_LABEL}", delegate
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			Hero hero = EnsureEncounterTargetHero("menu_attack_condition");
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
			GameTexts.SetVariable("PRIMARY_ACTION_LABEL", new TextObject("攻击{TARGET_NAME}"));
			return true;
		}, delegate
		{
			Hero target = EnsureEncounterTargetHero("menu_attack_click");
			ProactiveNpcRequestBehavior.CompleteActiveForHero(target, "attack_option");
			TryApplyImmediateAttackConsequencesForEncounter(target, "menu_attack_option");
			GameMenu.SwitchToMenu("encounter");
		});
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "leave_lord", "离开", delegate
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			return !IsHostileEncounterInitiatedByOpponent();
		}, delegate
		{
			ProactiveNpcRequestBehavior.CompleteActiveForHero(EnsureEncounterTargetHero("menu_leave_click"), "leave_option");
			PlayerEncounter.Finish();
		}, isLeave: true);
	}

	internal static bool TryApplyImmediateEscalationConsequences(PartyBase defenderParty, Hero targetHero, string reason)
	{
		if (!MeetingBattleRuntime.TryMarkCombatEscalationConsequencesApplied())
		{
			Logger.Log("LordEncounter", "Immediate escalation consequences already applied or meeting inactive. Reason=" + (reason ?? "N/A"));
			return false;
		}
		return ApplyHostileEscalationDiplomaticConsequences(defenderParty, targetHero, reason, "MeetingBattle");
	}

	private static Clan ResolveEncounterDefenderClanForHostileEscalation(PartyBase defenderParty, Hero targetHero)
	{
		Clan clan = null;
		try
		{
			clan = targetHero?.Clan ?? targetHero?.PartyBelongedTo?.ActualClan;
		}
		catch
		{
			clan = null;
		}
		if (clan != null)
		{
			return clan;
		}
		try
		{
			clan = defenderParty?.MobileParty?.ActualClan ?? defenderParty?.Owner?.Clan;
		}
		catch
		{
			clan = null;
		}
		if (clan != null)
		{
			return clan;
		}
		try
		{
			return defenderParty?.Settlement?.OwnerClan;
		}
		catch
		{
			return null;
		}
	}

	private static bool AreEncounterFactionsAlreadyAtWar(IFaction playerFaction, IFaction defenderFaction)
	{
		if (playerFaction == null || defenderFaction == null || playerFaction == defenderFaction)
		{
			return false;
		}
		try
		{
			if (FactionManager.IsAtWarAgainstFaction(playerFaction, defenderFaction)
				|| FactionManager.IsAtWarAgainstFaction(defenderFaction, playerFaction))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return playerFaction.IsAtWarWith(defenderFaction) || defenderFaction.IsAtWarWith(playerFaction);
		}
		catch
		{
			return false;
		}
	}

	internal static bool ApplyHostileEscalationDiplomaticConsequences(PartyBase defenderParty, Hero targetHero, string reason, string logChannel = "MeetingBattle")
	{
		bool flag = false;
		bool playerWasRulerInSharedKingdom = false;
		bool defenderClanLeftSharedKingdom = false;
		Clan sharedKingdomDefenderClan = null;
		try
		{
			if (defenderParty == null)
			{
				defenderParty = PlayerEncounter.EncounteredParty;
			}
		}
		catch
		{
			defenderParty = null;
		}
		if (defenderParty == null)
		{
			try
			{
				defenderParty = targetHero?.PartyBelongedTo?.Party;
			}
			catch
			{
				defenderParty = null;
			}
		}
		IFaction faction = null;
		IFaction faction2 = null;
		try
		{
			faction = PartyBase.MainParty?.MapFaction;
		}
		catch
		{
			faction = null;
		}
		try
		{
			faction2 = defenderParty?.MapFaction ?? targetHero?.MapFaction;
		}
		catch
		{
			faction2 = null;
		}
		bool encounterFactionsAlreadyAtWar = AreEncounterFactionsAlreadyAtWar(faction, faction2);
		if (faction != null && faction2 != null && faction == faction2)
		{
			try
			{
				Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
				Kingdom sharedKingdom = playerClan?.Kingdom;
				playerWasRulerInSharedKingdom = sharedKingdom != null && sharedKingdom == faction && (sharedKingdom.RulingClan == playerClan || sharedKingdom.Leader == Hero.MainHero);
				sharedKingdomDefenderClan = ResolveEncounterDefenderClanForHostileEscalation(defenderParty, targetHero);
				Clan clanToLeave = playerClan;
				if (playerWasRulerInSharedKingdom)
				{
					if (sharedKingdomDefenderClan != null && !sharedKingdomDefenderClan.IsEliminated && sharedKingdomDefenderClan != playerClan && sharedKingdomDefenderClan.Kingdom == sharedKingdom)
					{
						clanToLeave = sharedKingdomDefenderClan;
					}
					else
					{
						clanToLeave = null;
						Logger.Log(logChannel, "Immediate escalation: player ruling clan preserved; no eligible distinct defender clan could be separated. DefenderClan=" + (sharedKingdomDefenderClan?.StringId ?? "null"));
					}
				}
				if (clanToLeave != null && clanToLeave.Kingdom != null)
				{
					if (clanToLeave.IsUnderMercenaryService)
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(clanToLeave);
						Logger.Log(logChannel, "Immediate escalation: " + (clanToLeave == playerClan ? "player" : "defender") + " clan left kingdom as mercenary. Clan=" + (clanToLeave.StringId ?? ""));
					}
					else
					{
						ChangeKingdomAction.ApplyByLeaveKingdom(clanToLeave);
						Logger.Log(logChannel, "Immediate escalation: " + (clanToLeave == playerClan ? "player" : "defender") + " clan left kingdom. Clan=" + (clanToLeave.StringId ?? ""));
					}
					defenderClanLeftSharedKingdom = playerWasRulerInSharedKingdom && clanToLeave == sharedKingdomDefenderClan && clanToLeave.Kingdom != sharedKingdom;
					if (playerWasRulerInSharedKingdom)
					{
						Logger.Log(logChannel, "Immediate escalation: ruler friendly attack separation verified. PlayerClanStayed=" + (playerClan.Kingdom == sharedKingdom) + ", DefenderClanLeft=" + defenderClanLeftSharedKingdom + ", DefenderClan=" + (sharedKingdomDefenderClan?.StringId ?? "null"));
					}
					flag = true;
				}
			}
			catch (Exception ex)
			{
				Logger.Log(logChannel, "Immediate escalation: leave kingdom failed: " + ex.Message);
			}
		}
		if (defenderClanLeftSharedKingdom)
		{
			try
			{
				PartyBase selectedDefenderParty = targetHero?.PartyBelongedTo?.Party;
				if (selectedDefenderParty != null)
				{
					defenderParty = selectedDefenderParty;
				}
				faction2 = targetHero?.MapFaction ?? defenderParty?.MapFaction ?? sharedKingdomDefenderClan;
			}
			catch
			{
				faction2 = sharedKingdomDefenderClan;
			}
		}
		try
		{
			Hero hero = targetHero;
			if (hero == null)
			{
				hero = faction2?.Leader;
			}
			if (hero != null && !encounterFactionsAlreadyAtWar)
			{
				if (RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(hero, out var _))
				{
					RomanceSystemBehavior.Instance?.AdjustPrivateLove(hero, -10, "meeting_hostile_escalation_relation_delta");
				}
				else
				{
					ChangeRelationAction.ApplyPlayerRelation(hero, -10);
				}
				flag = true;
				Logger.Log(logChannel, $"Immediate escalation: relation penalty applied to {hero.Name}.");
			}
			else if (hero != null)
			{
				Logger.Log(logChannel, $"Immediate escalation: relation penalty skipped because factions are already at war. Target={hero.Name}.");
			}
		}
		catch (Exception ex2)
		{
			Logger.Log(logChannel, "Immediate escalation: relation penalty failed: " + ex2.Message);
		}
		try
		{
			if (defenderParty != null)
			{
				BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, defenderParty);
				flag = true;
				Logger.Log(logChannel, $"Immediate escalation: encounter hostility applied. Defender={defenderParty.Name}");
			}
		}
		catch (Exception ex3)
		{
			Logger.Log(logChannel, "Immediate escalation: ApplyEncounterHostileAction failed: " + ex3.Message);
		}
		try
		{
			IFaction faction3 = null;
			try
			{
				faction3 = PartyBase.MainParty?.MapFaction;
			}
			catch
			{
				faction3 = null;
			}
			if (faction3 == faction2 && !playerWasRulerInSharedKingdom)
			{
				try
				{
					faction3 = Clan.PlayerClan;
				}
				catch
				{
				}
			}
			if (faction3 != null && faction2 != null && faction3 != faction2 && !FactionManager.IsAtWarAgainstFaction(faction3, faction2))
			{
				DeclareWarAction.ApplyByPlayerHostility(faction3, faction2);
				flag = true;
				Logger.Log(logChannel, $"Immediate escalation: declared war. Attacker={faction3.Name}, Defender={faction2.Name}");
			}
		}
		catch (Exception ex4)
		{
			Logger.Log(logChannel, "Immediate escalation: declare war failed: " + ex4.Message);
		}
		Logger.Log(logChannel, string.Format("Immediate escalation consequences completed. Reason={0}, AppliedAny={1}", reason ?? "N/A", flag));
		return flag;
	}

	private static void TryApplyImmediateAttackConsequencesForEncounter(Hero target, string reason)
	{
		SuppressCustomEncounterMenuUntilBackOnMap("immediate_attack_" + (reason ?? "unknown"));
		bool flag = false;
		try
		{
			flag = MeetingBattleRuntime.IsMeetingActive;
			if (flag)
			{
				MeetingBattleRuntime.RequestCombatEscalation(reason);
				MeetingBattleRuntime.UnlockDiplomaticSideEffects(reason);
			}
		}
		catch
		{
			flag = false;
		}
		PartyBase partyBase = null;
		try
		{
			partyBase = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			partyBase = null;
		}
		if (partyBase == null)
		{
			try
			{
				partyBase = target?.PartyBelongedTo?.Party;
			}
			catch
			{
				partyBase = null;
			}
		}
		string text = reason ?? "menu_attack_option";
		if (flag)
		{
			TryApplyImmediateEscalationConsequences(partyBase, target, text);
		}
		else
		{
			ApplyHostileEscalationDiplomaticConsequences(partyBase, target, text, "LordEncounter");
		}
	}

	private static bool IsMeetingPseudoBattleTauntApplicable(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		bool flag = false;
		try
		{
			flag = MeetingBattleRuntime.IsMeetingActive || _encounterMeetingMissionActive;
		}
		catch
		{
			flag = false;
		}
		if (!flag)
		{
			return false;
		}
		try
		{
			Hero hero2 = MeetingBattleRuntime.TargetHero ?? _targetHero;
			if (hero2 != null && hero2 != hero)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static Hero ResolveNativeEncounterConversationHero()
	{
		try
		{
			if (Hero.OneToOneConversationHero != null)
			{
				return Hero.OneToOneConversationHero;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject characterObject = CharacterObject.OneToOneConversationCharacter;
			if (characterObject?.HeroObject != null)
			{
				return characterObject.HeroObject;
			}
		}
		catch
		{
		}
		try
		{
			if (_targetHero != null)
			{
				return _targetHero;
			}
		}
		catch
		{
		}
		try
		{
			return PlayerEncounter.EncounteredParty?.LeaderHero;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsMissionConversationStateActive()
	{
		try
		{
			if (Mission.Current != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return Game.Current?.GameStateManager?.ActiveState is MissionState;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNativeEncounterDialogConversationActive()
	{
		if (IsMissionConversationStateActive())
		{
			return false;
		}
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			return Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNativeEncounterConversationTauntApplicable(Hero hero)
	{
		return IsNativeEncounterConversationTauntApplicable(hero, null);
	}

	private static bool IsNativeEncounterConversationTauntApplicable(Hero hero, PartyBase defenderParty)
	{
		if (hero == null && defenderParty == null)
		{
			return false;
		}
		if (!IsNativeEncounterDialogConversationActive())
		{
			return false;
		}
		Hero hero2 = ResolveNativeEncounterConversationHero();
		if (hero != null && hero2 != null && hero2 != hero)
		{
			return false;
		}
		PartyBase partyBase = ResolveNativeEncounterAttackDefenderParty(hero, defenderParty);
		if (partyBase == null || PartyBase.MainParty == null || partyBase == PartyBase.MainParty)
		{
			return false;
		}
		bool flag = false;
		try
		{
			if (PlayerEncounter.Current != null || PlayerEncounter.EncounteredParty != null)
			{
				flag = true;
			}
		}
		catch
		{
		}
		if (flag)
		{
			return true;
		}
		try
		{
			if (hero != null)
			{
				return hero.PartyBelongedTo?.Party == partyBase;
			}
			return defenderParty != null && defenderParty == partyBase;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMeetingTauntApplicable(Hero hero)
	{
		return IsMeetingTauntApplicable(hero, null);
	}

	private static bool IsMeetingTauntApplicable(Hero hero, PartyBase defenderParty)
	{
		return (hero != null && IsMeetingPseudoBattleTauntApplicable(hero)) || IsNativeEncounterConversationTauntApplicable(hero, defenderParty);
	}

	private static bool TryRequestNativeEncounterAttackFromConversation(Hero target, string reason)
	{
		return TryRequestNativeEncounterAttackFromConversation(target, null, reason);
	}

	private static bool TryRequestNativeEncounterAttackFromConversation(Hero target, PartyBase defenderParty, string reason)
	{
		try
		{
			if (!IsNativeEncounterDialogConversationActive())
			{
				Logger.Log("MeetingTaunt", "Native attack tag ignored because it was not emitted from an active encounter dialog conversation.");
				return false;
			}
			Hero hero = target;
			if (hero == null)
			{
				try
				{
					hero = defenderParty?.LeaderHero;
				}
				catch
				{
					hero = null;
				}
			}
			if (hero == null && defenderParty == null)
			{
				hero = ResolveNativeEncounterConversationHero() ?? EnsureEncounterTargetHero("native_conversation_taunt_attack");
			}
			PartyBase partyBase = ResolveNativeEncounterAttackDefenderParty(hero, defenderParty);
			if (!IsNativeEncounterConversationTauntApplicable(hero, partyBase))
			{
				Logger.Log("MeetingTaunt", "Native attack tag ignored because current context is not a valid encounter conversation.");
				return false;
			}
			if (partyBase == null)
			{
				Logger.Log("MeetingTaunt", "Native attack tag ignored because defender party could not be resolved.");
				return false;
			}
			if (hero != null)
			{
				SetTarget(hero);
			}
			try
			{
				ProactiveNpcRequestBehavior.CompleteActiveForHero(hero, "native_conversation_attack_" + (reason ?? "unknown"));
			}
			catch
			{
			}
			DisableCustomEncounterMenuForCurrentEncounter(reason ?? "native_conversation_taunt_battle");
			SuspendEncounterRedirectDuringResultResolution(reason ?? "native_conversation_taunt_battle");
			try
			{
				LordEncounterRedirectGuard.SuppressForSeconds(12f);
			}
			catch
			{
			}
			MarkPendingForceNativeEncounterAttack(hero, partyBase, reason ?? "native_conversation_taunt_battle");
			TryApplyPendingNativeEncounterAttackDiplomacy(reason ?? "native_conversation_taunt_battle");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Request native encounter attack failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryEscalateMeetingTauntToBattle(Hero target, string reason)
	{
		return TryEscalateMeetingTauntToBattle(target, null, reason);
	}

	private static bool TryEscalateMeetingTauntToBattle(Hero target, PartyBase defenderParty, string reason)
	{
		try
		{
			Hero hero = target;
			if (hero == null)
			{
				try
				{
					hero = defenderParty?.LeaderHero;
				}
				catch
				{
					hero = null;
				}
			}
			if (hero == null && defenderParty == null)
			{
				hero = ResolveNativeEncounterConversationHero() ?? EnsureEncounterTargetHero("meeting_taunt_battle");
			}
			if (hero != null && IsMeetingPseudoBattleTauntApplicable(hero))
			{
				TryApplyImmediateAttackConsequencesForEncounter(hero, reason ?? "meeting_taunt_battle");
				try
				{
					Campaign.Current?.ConversationManager?.EndConversation();
				}
				catch
				{
				}
				Logger.Log("MeetingTaunt", $"Meeting pseudo-battle escalation applied from taunt tag. Target={hero?.Name}, Reason={reason ?? "N/A"}");
				return true;
			}
			PartyBase partyBase = ResolveNativeEncounterAttackDefenderParty(hero, defenderParty);
			if (IsNativeEncounterConversationTauntApplicable(hero, partyBase))
			{
				bool flag = TryRequestNativeEncounterAttackFromConversation(hero, partyBase, reason ?? "native_conversation_taunt_battle");
				if (flag)
				{
					Logger.Log("MeetingTaunt", $"Native encounter attack requested from taunt tag. Target={hero?.Name}, Defender={partyBase?.Name}, Reason={reason ?? "N/A"}");
				}
				return flag;
			}
			if (!IsMeetingTauntApplicable(hero, partyBase))
			{
				Logger.Log("MeetingTaunt", "Battle tag ignored because current context is not a valid hero meeting or encounter conversation.");
				return false;
			}
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Battle escalation from taunt tag failed: " + ex.Message);
			return false;
		}
	}

	internal static string BuildMeetingTauntRuntimeInstructionForExternal(Hero target)
	{
		return BuildMeetingTauntRuntimeInstructionForExternal(target, null, null);
	}

	internal static string BuildMeetingTauntRuntimeInstructionForExternal(Hero target, CharacterObject targetCharacter)
	{
		return BuildMeetingTauntRuntimeInstructionForExternal(target, targetCharacter, null);
	}

	internal static string BuildMeetingTauntRuntimeInstructionForExternal(Hero target, CharacterObject targetCharacter, PartyBase defenderParty)
	{
		try
		{
			Hero hero = target ?? targetCharacter?.HeroObject;
			PartyBase partyBase = ResolveNativeEncounterAttackDefenderParty(hero, defenderParty);
			if (!IsMeetingTauntApplicable(hero, partyBase))
			{
				return "";
			}
			return BuildMeetingTauntFallbackInstruction(hero, targetCharacter, partyBase);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildMeetingTauntFallbackInstruction(Hero target, CharacterObject targetCharacter, PartyBase defenderParty)
	{
		if (!IsMeetingTauntApplicable(target, defenderParty))
		{
			return "";
		}
		return BuildMeetingTauntInstructionBody(target, targetCharacter);
	}

	internal static string BuildForcedMeetingTauntRuntimeInstructionForExternal(Hero target, CharacterObject targetCharacter)
	{
		try
		{
			return BuildMeetingTauntInstructionBody(target, targetCharacter);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildMeetingTauntInstructionBody(Hero target, CharacterObject targetCharacter)
	{
		string text = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return "若" + text + "辱骂、贬低或挑衅你，你可以在句末输出[ACTION:MEETING_TAUNT_BATTLE]；该标签会触发你和" + text + "的战斗，你的友军会上来帮助你，这与单挑和决斗有本质的不同。";
	}

	private static int GetRelationToPlayerSafe(Hero hero)
	{
		try
		{
			if (hero == null || Hero.MainHero == null)
			{
				return 0;
			}
			if (RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(hero, out var relation))
			{
				return relation;
			}
			return hero.GetRelation(Hero.MainHero);
		}
		catch
		{
			return 0;
		}
	}

	private static int GetEncounterReleaseClanRelationWithPlayer(Hero target)
	{
		Hero hero = target?.Clan?.Leader ?? target;
		return GetRelationToPlayerSafe(hero);
	}

	private static int GetEncounterReleasePrivateRelationWithPlayer(Hero target)
	{
		try
		{
			return RomanceSystemBehavior.Instance?.GetPrivateLove(target) ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	private static Hero GetEncounterReleaseKingHero(Hero target)
	{
		try
		{
			Kingdom kingdom = target?.Clan?.Kingdom;
			return kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		}
		catch
		{
			return null;
		}
	}

	private static int TryGetEncounterReleaseVanillaSafePassageQuote(Hero target)
	{
		try
		{
			Hero hero = target ?? EnsureEncounterTargetHero("meeting_release_quote");
			PartyBase party = hero?.PartyBelongedTo?.Party;
			if (hero == null || party == null || PartyBase.MainParty == null)
			{
				return 0;
			}
			IFaction faction = (IFaction)hero.Clan ?? hero.MapFaction ?? party.MapFaction;
			if (faction == null)
			{
				return 0;
			}
			SafePassageBarterable safePassageBarterable = new SafePassageBarterable(hero, Hero.MainHero, party, PartyBase.MainParty);
			NoAttackBarterable noAttackBarterable = new NoAttackBarterable(Hero.MainHero, hero, PartyBase.MainParty, party, CampaignTime.Days(5f));
			int valueForFaction = safePassageBarterable.GetValueForFaction(faction);
			int valueForFaction2 = noAttackBarterable.GetValueForFaction(faction);
			return Math.Max(0, -(valueForFaction + valueForFaction2));
		}
		catch
		{
			return 0;
		}
	}

	private static PartyBase TryGetMeetingReleaseEncounterParty()
	{
		try
		{
			PartyBase partyBase = PlayerEncounterCompat.GetEncounteredPartySafe();
			if (partyBase != null)
			{
				return partyBase;
			}
		}
		catch
		{
		}
		try
		{
			return PlayerEncounter.EncounteredParty;
		}
		catch
		{
			return null;
		}
	}

	internal static PartyBase CaptureMeetingReleaseEncounterPartyForExternal()
	{
		return TryGetMeetingReleaseEncounterParty();
	}

	private static bool IsNonHeroMeetingReleaseParty(PartyBase party)
	{
		try
		{
			return party != null && party.LeaderHero == null;
		}
		catch
		{
			return false;
		}
	}

	private static Hero ResolveMeetingReleaseHeroForCurrentEncounter(Hero fallback)
	{
		PartyBase partyBase = TryGetMeetingReleaseEncounterParty();
		if (IsNonHeroMeetingReleaseParty(partyBase))
		{
			return null;
		}
		try
		{
			return fallback ?? partyBase?.LeaderHero ?? _targetHero;
		}
		catch
		{
			return fallback ?? _targetHero;
		}
	}

	private static bool TryGetMeetingReleaseContext(Hero target, out Hero resolvedTarget, out int clanRelation, out int privateRelation, out int averageRelation, out int kingRelation, out string kingName, out bool negotiable)
	{
		resolvedTarget = null;
		clanRelation = 0;
		privateRelation = 0;
		averageRelation = 0;
		kingRelation = 0;
		kingName = "该势力的国王";
		negotiable = false;
		try
		{
			PartyBase partyBase = TryGetMeetingReleaseEncounterParty();
			bool isNonHeroEncounterParty = IsNonHeroMeetingReleaseParty(partyBase);
			// A bandit/looter party has no Hero. Do not let a stale lord target turn
			// that party into a hero negotiation or prevent its release tag.
			resolvedTarget = isNonHeroEncounterParty ? null : (target ?? EnsureEncounterTargetHero("meeting_release_context"));
			if (!IsMeetingReleaseRuntimeSceneActive() || !IsHostileEncounterInitiatedByOpponent())
			{
				return false;
			}
			if (resolvedTarget == null)
			{
				if (partyBase == null || partyBase == PartyBase.MainParty)
				{
					return false;
				}
				negotiable = true;
				kingName = partyBase.MapFaction?.Name?.ToString() ?? "当前遭遇方";
				return true;
			}
			clanRelation = GetEncounterReleaseClanRelationWithPlayer(resolvedTarget);
			privateRelation = GetEncounterReleasePrivateRelationWithPlayer(resolvedTarget);
			averageRelation = (int)Math.Round((clanRelation + privateRelation) / 2.0, MidpointRounding.AwayFromZero);
			Hero encounterReleaseKingHero = GetEncounterReleaseKingHero(resolvedTarget);
			kingRelation = GetRelationToPlayerSafe(encounterReleaseKingHero);
			kingName = encounterReleaseKingHero?.Name?.ToString() ?? "该势力的国王";
			negotiable = averageRelation > kingRelation;
			return true;
		}
		catch
		{
			resolvedTarget = null;
			return false;
		}
	}

	private static bool IsMeetingReleaseRuntimeSceneActive()
	{
		bool meetingActive = false;
		try
		{
			meetingActive = MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
			meetingActive = false;
		}
		if (meetingActive || _encounterMeetingMissionActive || _pendingNativeConversationMeetingRelease)
		{
			return true;
		}
		try
		{
			return Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildMeetingPlayerReleaseNonEligibleRuntimeInstruction(Hero target)
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		if (IsMeetingReleaseRuntimeSceneActive())
		{
			return $"【遭遇放走规则】当前不是敌对方拦截玩家的放走场景；普通通行、告别或结束谈话不算放走{text}。";
		}
		return $"【遭遇放走规则】本轮无敌对遭遇放走机制；普通通行、告别或结束谈话不算放走{text}。";
	}

	internal static string BuildMeetingPlayerReleaseRuntimeInstructionForExternal(Hero target, bool includeNonEligibleFallback = false)
	{
		if (!TryGetMeetingReleaseContext(target, out var resolvedTarget, out var clanRelation, out var privateRelation, out var averageRelation, out var kingRelation, out var kingName, out var negotiable))
		{
			return includeNonEligibleFallback ? BuildMeetingPlayerReleaseNonEligibleRuntimeInstruction(target) : "";
		}
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		if (negotiable)
		{
			int encounterReleaseVanillaSafePassageQuote = TryGetEncounterReleaseVanillaSafePassageQuote(resolvedTarget);
			string text2 = (encounterReleaseVanillaSafePassageQuote > 0) ? $"按原版谈判口径，这次“安全放行并约定五天内不主动攻击”的参考估值约为 {encounterReleaseVanillaSafePassageQuote} 第纳尔，你可以把它当作软参考来谈价、索赔或换物，但不必机械照搬这个数字。" : "";
			return $"【遭遇放走规则】你和你的的部队试图向{text}发起攻击，但本轮可以谈是否放{text}离开；可要求赔偿、立誓、交人、交钱或别的交换。{text2}只有你最终明确同意让{text}立刻离开，才算真的放走；还在谈条件就不算。";
		}
		return $"【遭遇放走规则】你和你的的部队试图向{text}发起攻击，并且你绝不可以放{text}走,他是你的势力的敌人，你必须在回复末尾输出标签[ACTION:MEETING_TAUNT_BATTLE]让你的部队攻击他";
	}

	internal static List<PostprocessRuleEntry> BuildMeetingPlayerReleasePostprocessRulesForExternal(Hero target)
	{
		if (!TryGetMeetingReleaseContext(target, out var _, out var _, out var _, out var _, out var _, out var _, out var negotiable) || !negotiable)
		{
			return new List<PostprocessRuleEntry>();
		}
		return AIConfigHandler.GetGuardrailRulePostprocessRules("encounter_release_player") ?? new List<PostprocessRuleEntry>();
	}

	internal static void StripMeetingPlayerReleaseTag(ref string content)
	{
		if (!string.IsNullOrWhiteSpace(content))
		{
			content = MeetingReleasePlayerTagRegex.Replace(content, "").Trim();
		}
	}

	internal static bool TryConsumeMeetingPlayerReleaseTag(Hero target, ref string content, out bool shouldRelease)
	{
		shouldRelease = false;
		try
		{
			if (string.IsNullOrWhiteSpace(content) || !MeetingReleasePlayerTagRegex.IsMatch(content))
			{
				return false;
			}
			content = MeetingReleasePlayerTagRegex.Replace(content, "").Trim();
			bool flag = TryGetMeetingReleaseContext(target, out var resolvedTarget, out var _, out var _, out var _, out var _, out var _, out var negotiable);
			shouldRelease = flag && negotiable;
			if (!shouldRelease)
			{
				Logger.Log("MeetingRelease", "Release tag ignored because current encounter is not in negotiable release state.");
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingRelease", "Consume release tag failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryEndCurrentMeetingMissionByReflection(string reason)
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null)
			{
				return false;
			}
			MethodInfo method = current.GetType().GetMethod("EndMission", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			if (method == null)
			{
				Logger.Log("MeetingRelease", "EndMission method not found on current mission. Reason=" + (reason ?? "N/A"));
				return false;
			}
			method.Invoke(current, null);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingRelease", "End mission by reflection failed: " + ex.Message);
			return false;
		}
	}

	private static void ApplyMeetingPlayerReleaseWorldMapCooldown(Hero releasedByHero, string reason, PartyBase encounterPartyOverride = null)
	{
		try
		{
			// Keep this aligned with the native SafePassageBarterable path. The
			// encounter can contain army members that are not represented by the
			// currently speaking hero, so protecting only EncounteredMobileParty lets
			// one of those members immediately attack the player again.
			List<MobileParty> partiesJoiningPlayerSide = new List<MobileParty>();
			List<MobileParty> protectedParties = new List<MobileParty>();
			try
			{
				PlayerEncounter.Current?.FindAllNpcPartiesWhoWillJoinEvent(partiesJoiningPlayerSide, protectedParties);
			}
			catch (Exception ex)
			{
				Logger.Log("MeetingRelease", "Could not resolve native safe-passage parties: " + ex.Message);
			}
			void AddProtectedParty(MobileParty party)
			{
				if (party != null && party != MobileParty.MainParty && !protectedParties.Contains(party))
				{
					protectedParties.Add(party);
				}
			}
			void AddProtectedArmy(Army army)
			{
				if (army == null)
				{
					return;
				}
				AddProtectedParty(army.LeaderParty);
				try
				{
					foreach (MobileParty party in army.Parties)
					{
						AddProtectedParty(party);
					}
				}
				catch (Exception ex2)
				{
					Logger.Log("MeetingRelease", "Could not resolve release army members: " + ex2.Message);
				}
			}
			PartyBase meetingReleaseEncounterParty = encounterPartyOverride ?? TryGetMeetingReleaseEncounterParty();
			AddProtectedParty(meetingReleaseEncounterParty?.MobileParty);
			AddProtectedParty(releasedByHero?.PartyBelongedTo);
			AddProtectedArmy(meetingReleaseEncounterParty?.MobileParty?.Army);
			AddProtectedArmy(releasedByHero?.PartyBelongedTo?.Army);
			int num = 0;
			foreach (MobileParty item in protectedParties)
			{
				try
				{
					item.Ai?.SetDoNotAttackMainParty(MeetingReleaseSafePassageHours);
					item.SetMoveModeHold();
					item.IgnoreForHours(MeetingReleaseSafePassageHours);
					item.Ai?.SetInitiative(0f, 0.8f, 8f);
					num++;
				}
				catch (Exception ex2)
				{
					Logger.Log("MeetingRelease", "Could not apply native safe-passage state to party=" + GetPartyLogName(item?.Party) + ": " + ex2.Message);
				}
			}
			try
			{
				LordEncounterRedirectGuard.SuppressForSeconds(12f);
			}
			catch
			{
			}
			Logger.Log("MeetingRelease", $"Applied native-equivalent release safe passage. ProtectedParties={num}, Hours={MeetingReleaseSafePassageHours}, Reason={reason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingRelease", "Apply release world-map cooldown failed: " + ex.Message);
		}
	}

	private static bool IsMissionStateActiveForMeetingRelease()
	{
		try
		{
			return Mission.Current != null || Game.Current?.GameStateManager?.ActiveState is MissionState;
		}
		catch
		{
			return Mission.Current != null;
		}
	}

	private static bool TryFinishMeetingPlayerReleaseEncounterDirectly(Hero releasedByHero, string reason, PartyBase encounterPartyOverride = null)
	{
		try
		{
			if (PlayerEncounter.Current == null)
			{
				Logger.Log("MeetingRelease", "Direct release finish skipped because PlayerEncounter.Current is null. Reason=" + (reason ?? "N/A"));
				return false;
			}
			PartyBase partyBase = encounterPartyOverride ?? TryGetMeetingReleaseEncounterParty();
			try
			{
				PlayerEncounter.CampaignBattleResult = null;
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.LeaveEncounter = true;
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.Update();
			}
			catch
			{
			}
			try
			{
				ApplyMeetingPlayerReleaseWorldMapCooldown(releasedByHero, reason ?? "meeting_release_direct_finish", partyBase);
			}
			catch
			{
			}
			PlayerEncounter.Finish(true);
			try
			{
				// Finish may reset the party AI after the first application above.
				ApplyMeetingPlayerReleaseWorldMapCooldown(releasedByHero, (reason ?? "meeting_release_direct_finish") + "_after_final_finish", partyBase);
			}
			catch
			{
			}
			Logger.Log("MeetingRelease", "Directly finished player encounter after release. Reason=" + (reason ?? "N/A"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingRelease", "Direct release encounter finish failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryExecuteMeetingPlayerRelease(Hero target, string reason)
	{
		return TryExecuteMeetingPlayerRelease(target, null, reason);
	}

	internal static bool TryExecuteMeetingPlayerRelease(Hero target, PartyBase expectedEncounterParty, string reason)
	{
		try
		{
			PartyBase partyBase = TryGetMeetingReleaseEncounterParty();
			if (expectedEncounterParty != null && partyBase != expectedEncounterParty)
			{
				Logger.Log("MeetingRelease", "Execute release ignored because the scheduled encounter party changed. Expected=" + GetPartyLogName(expectedEncounterParty) + ", Current=" + GetPartyLogName(partyBase));
				return false;
			}
			if (!TryGetMeetingReleaseContext(target, out var resolvedTarget, out var _, out var _, out var _, out var _, out var _, out var negotiable) || !negotiable)
			{
				Logger.Log("MeetingRelease", "Execute release ignored because current encounter is not eligible.");
				return false;
			}
			string targetName = resolvedTarget?.Name?.ToString();
			PartyBase releaseParty = expectedEncounterParty ?? partyBase;
			if (string.IsNullOrWhiteSpace(targetName))
			{
				targetName = releaseParty?.Name?.ToString() ?? "encounter_party";
			}
			Logger.Log("MeetingRelease", $"Player release triggered. Target={targetName}, Reason={reason ?? "N/A"}");
			bool flag = IsMissionStateActiveForMeetingRelease();
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
			AuthorizeMeetingPlayerRelease(reason ?? "meeting_release_player");
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("meeting_release_player");
			if (flag)
			{
				if (!TryEndCurrentMeetingMissionByReflection(reason ?? "meeting_release_player"))
				{
					Logger.Log("MeetingRelease", "Release authorized but mission could not be ended automatically; waiting for mission exit. Reason=" + (reason ?? "N/A"));
				}
			}
			else if (TryFinishMeetingPlayerReleaseEncounterDirectly(resolvedTarget, reason ?? "meeting_release_player", releaseParty))
			{
				ClearMeetingPlayerReleaseAuthorization("direct_release_finished");
			}
			try
			{
				AnimusForgeQuickInfo.Show(flag ? "对方同意放你离开，正在退出当前会面。" : "对方同意放你离开。", _targetHero?.CharacterObject);
			}
			catch
			{
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingRelease", "Execute release failed: " + ex.Message);
			return false;
		}
	}

	internal static bool ScheduleNativeConversationMeetingPlayerRelease(Hero target, string reason)
	{
		try
		{
			if (!TryGetMeetingReleaseContext(target, out var resolvedTarget, out var _, out var _, out var _, out var _, out var _, out var negotiable) || !negotiable)
			{
				Logger.Log("MeetingRelease", "Native conversation release schedule ignored because current encounter is not eligible.");
				return false;
			}
			_pendingNativeConversationMeetingRelease = true;
			try
			{
				_pendingNativeConversationMeetingReleaseAtTime = Time.ApplicationTime;
			}
			catch
			{
				_pendingNativeConversationMeetingReleaseAtTime = 0f;
			}
			_pendingNativeConversationMeetingReleaseLastAttemptTime = -1f;
			_pendingNativeConversationMeetingReleaseParty = TryGetMeetingReleaseEncounterParty();
			bool isNonHeroEncounterParty = IsNonHeroMeetingReleaseParty(_pendingNativeConversationMeetingReleaseParty);
			_pendingNativeConversationMeetingReleaseHero = isNonHeroEncounterParty ? null : (resolvedTarget ?? target);
			_pendingNativeConversationMeetingReleaseReason = reason ?? "native_conversation_release_tag";
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("native_conversation_release_scheduled");
			DisableCustomEncounterMenuForCurrentEncounter("native_conversation_release_scheduled");
			try
			{
				string message = "对方同意放你离开，10秒后将自动结束当前对话并回到大地图。";
				InformationManager.DisplayMessage(new InformationMessage(message, new Color(0.4f, 1f, 0.4f)));
				AnimusForgeQuickInfo.Show(message, _pendingNativeConversationMeetingReleaseHero?.CharacterObject);
			}
			catch (Exception ex)
			{
				Logger.Log("MeetingRelease", "Show pending native conversation release prompt failed: " + ex.Message);
			}
			Logger.Log("MeetingRelease", "Scheduled native conversation release after delay. Target=" + (_pendingNativeConversationMeetingReleaseHero?.StringId ?? "null") + ", Party=" + GetPartyLogName(_pendingNativeConversationMeetingReleaseParty) + ", Delay=" + NativeConversationReleaseDialogDelaySeconds.ToString("F1") + ", Reason=" + (reason ?? "N/A"));
			return true;
		}
		catch (Exception ex2)
		{
			Logger.Log("MeetingRelease", "Schedule native conversation release failed: " + ex2.Message);
			return false;
		}
	}

	private static bool HasPendingNativeConversationMeetingRelease()
	{
		if (!_pendingNativeConversationMeetingRelease)
		{
			return false;
		}
		float elapsed = 0f;
		try
		{
			if (_pendingNativeConversationMeetingReleaseAtTime > 0f)
			{
				elapsed = Time.ApplicationTime - _pendingNativeConversationMeetingReleaseAtTime;
			}
		}
		catch
		{
		}
		if (elapsed > 120f)
		{
			ClearPendingNativeConversationMeetingRelease("expired");
			return false;
		}
		return true;
	}

	private static void ClearPendingNativeConversationMeetingRelease(string reason)
	{
		_pendingNativeConversationMeetingRelease = false;
		_pendingNativeConversationMeetingReleaseAtTime = 0f;
		_pendingNativeConversationMeetingReleaseLastAttemptTime = -1f;
		_pendingNativeConversationMeetingReleaseHero = null;
		_pendingNativeConversationMeetingReleaseParty = null;
		_pendingNativeConversationMeetingReleaseReason = null;
		Logger.Log("MeetingRelease", "Cleared pending native conversation release. Reason=" + (reason ?? "N/A"));
	}

	private static void TryForcePendingNativeConversationMeetingReleaseIfReady()
	{
		if (!HasPendingNativeConversationMeetingRelease())
		{
			return;
		}
		float applicationTime = 0f;
		try
		{
			applicationTime = Time.ApplicationTime;
			if (_pendingNativeConversationMeetingReleaseAtTime > 0f && applicationTime - _pendingNativeConversationMeetingReleaseAtTime < NativeConversationReleaseDialogDelaySeconds)
			{
				return;
			}
			if (_pendingNativeConversationMeetingReleaseLastAttemptTime > 0f && applicationTime - _pendingNativeConversationMeetingReleaseLastAttemptTime < 0.25f)
			{
				return;
			}
			_pendingNativeConversationMeetingReleaseLastAttemptTime = applicationTime;
		}
		catch
		{
			_pendingNativeConversationMeetingReleaseLastAttemptTime = 0f;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		PartyBase currentParty = TryGetMeetingReleaseEncounterParty();
		if (_pendingNativeConversationMeetingReleaseParty != null && currentParty != _pendingNativeConversationMeetingReleaseParty)
		{
			Logger.Log("MeetingRelease", "Pending native conversation release cancelled because encounter party changed. Pending=" + GetPartyLogName(_pendingNativeConversationMeetingReleaseParty) + ", Current=" + GetPartyLogName(currentParty));
			ClearPendingNativeConversationMeetingRelease("encounter_party_changed");
			return;
		}
		if (PlayerEncounter.Current == null)
		{
			ClearPendingNativeConversationMeetingRelease("player_encounter_missing");
			return;
		}
		string reason = _pendingNativeConversationMeetingReleaseReason ?? "native_conversation_release_tag";
		Hero target = _pendingNativeConversationMeetingReleaseHero;
		if (target == null)
		{
			try
			{
				target = _pendingNativeConversationMeetingReleaseParty?.LeaderHero;
			}
			catch
			{
			}
			if (target == null && _pendingNativeConversationMeetingReleaseParty == null)
			{
				target = _targetHero;
			}
		}
		try
		{
			SuppressCustomEncounterMenuUntilBackOnMap("native_conversation_release");
		}
		catch
		{
		}
		try
		{
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("native_conversation_release");
		}
		catch
		{
		}
		if (TryExecuteMeetingPlayerRelease(target, reason))
		{
			ClearPendingNativeConversationMeetingRelease("executed");
		}
	}

	internal static bool TryExecuteNpcSurrenderFromNativeConversation(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
		return MarkPendingNativeConversationNpcSurrender(targetHero, targetCharacter, targetAgentIndex, reason ?? "native_conversation_npc_surrender_tag");
	}

	internal static bool TryExecuteNpcSurrenderFromDirectDialog(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
		return TryExecuteNpcSurrenderFromFreeConversation(targetHero, targetCharacter, targetAgentIndex, reason);
	}

	internal static bool TryExecuteNpcSurrenderFromFreeConversation(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
		return TryExecuteNpcSurrenderFromFreeConversation(targetHero, targetCharacter, targetAgentIndex, reason, closeConversation: true);
	}

	private static bool TryExecuteNpcSurrenderFromFreeConversation(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason, bool closeConversation)
	{
		string targetId = targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown";
		try
		{
			if (!TryGetNpcSurrenderEncounterParty(targetHero, targetCharacter, out var encounterParty, out var blockedReason))
			{
				Logger.Log("NpcSurrender", "Ignored conversation/dialog NPC surrender tag. Target=" + targetId + " agentIndex=" + targetAgentIndex + " reason=" + (blockedReason ?? "unknown") + " source=" + (reason ?? "N/A"));
				return false;
			}
			if (PlayerEncounter.EnemySurrender)
			{
				Logger.Log("NpcSurrender", "Conversation/dialog NPC surrender already applied. Target=" + targetId + " party=" + GetPartyLogName(encounterParty) + " source=" + (reason ?? "N/A"));
				return true;
			}
			if (closeConversation)
			{
				try
				{
					Campaign.Current?.ConversationManager?.EndConversation();
				}
				catch
				{
				}
			}
			try
			{
				SuspendEncounterRedirectDuringResultResolution(reason ?? "native_conversation_npc_surrender");
			}
			catch
			{
			}
			try
			{
				LordEncounterRedirectGuard.SuppressForSeconds(12f);
			}
			catch
			{
			}
			try
			{
				DisableCustomEncounterMenuForCurrentEncounter(reason ?? "native_conversation_npc_surrender");
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.LeaveEncounter = false;
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
			catch
			{
			}
			MapEvent mapEvent = EnsureNpcSurrenderEncounterBattle(encounterParty, reason ?? "native_conversation_npc_surrender");
			if (mapEvent == null || PlayerEncounterCompat.GetBattleSafe() == null)
			{
				Logger.Log("NpcSurrender", "Conversation/dialog NPC surrender failed because encounter battle is unavailable. Target=" + targetId + " party=" + GetPartyLogName(encounterParty) + " source=" + (reason ?? "N/A"));
				return false;
			}
			List<PartyBase> surrenderParties = BuildNpcSurrenderParties(encounterParty, targetHero, targetCharacter, mapEvent);
			BattleSideEnum surrenderSide = ResolveNpcSurrenderOpponentSide(mapEvent);
			int addedParties = EnsureNpcSurrenderPartiesOnBattleSide(mapEvent, surrenderSide, surrenderParties, reason ?? "native_conversation_npc_surrender");
			try
			{
				mapEvent.SetOverrideWinner(mapEvent.PlayerSide);
				Logger.Log("NpcSurrender", "Forced NPC surrender battle winner to player side. Target=" + targetId + " party=" + GetPartyLogName(encounterParty) + " surrenderParties=" + (surrenderParties?.Count ?? 0) + " addedParties=" + addedParties + " battleState=" + mapEvent.BattleState + " source=" + (reason ?? "N/A"));
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "SetOverrideWinner for NPC surrender failed: " + ex.Message);
			}
			BeginNpcSurrenderHeroConversationSkip(encounterParty, reason ?? "native_conversation_npc_surrender");
			PlayerEncounter.EnemySurrender = true;
			try
			{
				PlayerEncounter.Update();
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "PlayerEncounter.Update after NPC surrender failed: " + ex.Message);
			}
			int capturedFallbackHeroes = CaptureRemainingNpcSurrenderPartyHeroes(surrenderParties, reason ?? "native_conversation_npc_surrender");
			try
			{
				AnimusForgeQuickInfo.Show("对方已投降，正在进入俘虏与战利品结算。", targetHero?.CharacterObject ?? targetCharacter);
			}
			catch
			{
			}
			string currentMenu = null;
			try
			{
				currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
			}
			Logger.Log("NpcSurrender", "Executed conversation/dialog NPC surrender. Target=" + targetId + " party=" + GetPartyLogName(encounterParty) + " surrenderParties=" + (surrenderParties?.Count ?? 0) + " addedParties=" + addedParties + " fallbackCapturedHeroes=" + capturedFallbackHeroes + " currentMenu=" + (currentMenu ?? "null") + " battleState=" + mapEvent.BattleState + " reason=" + (reason ?? "N/A"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Execute conversation/dialog NPC surrender failed. Target=" + targetId + " source=" + (reason ?? "N/A") + " error=" + ex.Message);
			return false;
		}
	}

	private static List<PartyBase> BuildNpcSurrenderParties(PartyBase encounterParty, Hero targetHero, CharacterObject targetCharacter, MapEvent mapEvent)
	{
		List<PartyBase> result = new List<PartyBase>();
		void AddParty(PartyBase party)
		{
			if (!IsEligibleNpcSurrenderParty(party, mapEvent) || result.Contains(party))
			{
				return;
			}
			result.Add(party);
		}
		void AddMobileParty(MobileParty mobileParty)
		{
			AddParty(mobileParty?.Party);
		}
		void AddArmy(Army army)
		{
			if (army == null)
			{
				return;
			}
			try
			{
				if (MobileParty.MainParty?.Army != null && army == MobileParty.MainParty.Army)
				{
					return;
				}
			}
			catch
			{
			}
			try
			{
				AddMobileParty(army.LeaderParty);
			}
			catch
			{
			}
			try
			{
				if (army.Parties != null)
				{
					foreach (MobileParty party in army.Parties)
					{
						AddMobileParty(party);
					}
				}
			}
			catch
			{
			}
			try
			{
				if (army.LeaderParty?.AttachedParties != null)
				{
					foreach (MobileParty party in army.LeaderParty.AttachedParties)
					{
						AddMobileParty(party);
					}
				}
			}
			catch
			{
			}
		}
		void AddPartyAndArmy(PartyBase party)
		{
			AddParty(party);
			MobileParty mobileParty = party?.MobileParty;
			if (mobileParty == null)
			{
				return;
			}
			try
			{
				AddArmy(mobileParty.Army);
			}
			catch
			{
			}
			try
			{
				AddMobileParty(mobileParty.AttachedTo);
				AddArmy(mobileParty.AttachedTo?.Army);
			}
			catch
			{
			}
			try
			{
				if (mobileParty.AttachedParties != null)
				{
					foreach (MobileParty attachedParty in mobileParty.AttachedParties)
					{
						AddMobileParty(attachedParty);
						AddArmy(attachedParty?.Army);
					}
				}
			}
			catch
			{
			}
		}
		try
		{
			AddPartyAndArmy(encounterParty);
			AddPartyAndArmy(targetHero?.PartyBelongedTo?.Party);
			AddPartyAndArmy(targetCharacter?.HeroObject?.PartyBelongedTo?.Party);
			AddPartyAndArmy(MobileParty.ConversationParty?.Party);
			AddPartyAndArmy(PlayerEncounterCompat.GetEncounteredPartySafe());
			AddNpcSurrenderMapEventSideParties(result, mapEvent);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Build NPC surrender party list failed: " + ex.Message);
		}
		return result;
	}

	private static void AddNpcSurrenderMapEventSideParties(List<PartyBase> result, MapEvent mapEvent)
	{
		if (result == null || mapEvent == null)
		{
			return;
		}
		try
		{
			BattleSideEnum surrenderSide = ResolveNpcSurrenderOpponentSide(mapEvent);
			if (surrenderSide != BattleSideEnum.Attacker && surrenderSide != BattleSideEnum.Defender)
			{
				return;
			}
			MapEventSide side = mapEvent.GetMapEventSide(surrenderSide);
			if (side?.Parties == null)
			{
				return;
			}
			foreach (MapEventParty mapEventParty in side.Parties)
			{
				PartyBase party = mapEventParty?.Party;
				if (IsEligibleNpcSurrenderParty(party, mapEvent) && !result.Contains(party))
				{
					result.Add(party);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Add existing map-event side parties for NPC surrender failed: " + ex.Message);
		}
	}

	private static bool IsEligibleNpcSurrenderParty(PartyBase party, MapEvent mapEvent)
	{
		if (party == null || PartyBase.MainParty == null)
		{
			return false;
		}
		if (party == PartyBase.MainParty || party.MobileParty == MobileParty.MainParty)
		{
			return false;
		}
		if (party.MobileParty == null)
		{
			return false;
		}
		try
		{
			MobileParty mobileParty = party.MobileParty;
			if (MobileParty.MainParty?.Army != null && mobileParty?.Army == MobileParty.MainParty.Army)
			{
				return false;
			}
			if (mobileParty?.AttachedTo == MobileParty.MainParty)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			MapEvent existingMapEvent = party.MapEvent;
			if (existingMapEvent != null && mapEvent != null && existingMapEvent != mapEvent)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (!party.IsActive && party.MapEvent != mapEvent)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static BattleSideEnum ResolveNpcSurrenderOpponentSide(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent != null && (mapEvent.PlayerSide == BattleSideEnum.Attacker || mapEvent.PlayerSide == BattleSideEnum.Defender))
			{
				return mapEvent.GetOtherSide(mapEvent.PlayerSide);
			}
		}
		catch
		{
		}
		try
		{
			BattleSideEnum side = PartyBase.MainParty?.OpponentSide ?? BattleSideEnum.None;
			if (side == BattleSideEnum.Attacker || side == BattleSideEnum.Defender)
			{
				return side;
			}
		}
		catch
		{
		}
		return BattleSideEnum.None;
	}

	private static int EnsureNpcSurrenderPartiesOnBattleSide(MapEvent mapEvent, BattleSideEnum surrenderSide, List<PartyBase> surrenderParties, string reason)
	{
		if (mapEvent == null || surrenderParties == null || surrenderParties.Count == 0 || (surrenderSide != BattleSideEnum.Attacker && surrenderSide != BattleSideEnum.Defender))
		{
			return 0;
		}
		MapEventSide side = null;
		try
		{
			side = mapEvent.GetMapEventSide(surrenderSide);
		}
		catch
		{
			side = null;
		}
		if (side == null)
		{
			return 0;
		}
		int added = 0;
		int skippedDifferentSide = 0;
		foreach (PartyBase party in surrenderParties)
		{
			try
			{
				if (!IsEligibleNpcSurrenderParty(party, mapEvent))
				{
					continue;
				}
				if (party.MapEventSide == side)
				{
					continue;
				}
				if (party.MapEventSide != null && party.MapEventSide != side)
				{
					skippedDifferentSide++;
					continue;
				}
				party.MapEventSide = side;
				added++;
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "Failed to add surrender party to battle side. Party=" + GetPartyLogName(party) + " side=" + surrenderSide + " reason=" + (reason ?? "N/A") + " error=" + ex.Message);
			}
		}
		try
		{
			mapEvent.RecalculateStrengthOfSides();
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Recalculate strength after NPC surrender army join failed: " + ex.Message);
		}
		Logger.Log("NpcSurrender", "Prepared NPC surrender battle side. Side=" + surrenderSide + " parties=" + surrenderParties.Count + " added=" + added + " skippedDifferentSide=" + skippedDifferentSide + " reason=" + (reason ?? "N/A"));
		return added;
	}

	private static int CaptureRemainingNpcSurrenderPartyHeroes(List<PartyBase> surrenderParties, string source)
	{
		if (surrenderParties == null || surrenderParties.Count == 0)
		{
			return 0;
		}
		HashSet<Hero> heroes = new HashSet<Hero>();
		foreach (PartyBase party in surrenderParties)
		{
			try
			{
				if (!IsEligibleNpcSurrenderParty(party, null))
				{
					continue;
				}
				Hero leader = party.LeaderHero;
				if (leader != null && leader != Hero.MainHero)
				{
					heroes.Add(leader);
				}
				TroopRoster roster = party.MemberRoster;
				if (roster == null)
				{
					continue;
				}
				for (int i = 0; i < roster.Count; i++)
				{
					CharacterObject character = roster.GetElementCopyAtIndex(i).Character;
					Hero hero = character?.IsHero == true ? character.HeroObject : null;
					if (hero != null && hero != Hero.MainHero)
					{
						heroes.Add(hero);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "Collect remaining surrender party heroes failed. Party=" + GetPartyLogName(party) + " source=" + (source ?? "unknown") + " error=" + ex.Message);
			}
		}
		int captured = 0;
		foreach (Hero hero in heroes)
		{
			try
			{
				if (hero == null || hero == Hero.MainHero)
				{
					continue;
				}
				if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner == PartyBase.MainParty)
				{
					continue;
				}
				if (TryMoveHeroToMainPartyPrisoners(hero.CharacterObject, source ?? "npc_surrender_army_fallback"))
				{
					captured++;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "Capture remaining surrender hero failed. Hero=" + (hero?.StringId ?? "") + " source=" + (source ?? "unknown") + " error=" + ex.Message);
			}
		}
		if (captured > 0)
		{
			Logger.Log("NpcSurrender", "Captured remaining surrender party heroes. Count=" + captured + " source=" + (source ?? "unknown"));
		}
		return captured;
	}

	private static void BeginNpcSurrenderHeroConversationSkip(PartyBase encounterParty, string reason)
	{
		_npcSurrenderSkipHeroCaptureConversations = true;
		_npcSurrenderSkipEncounterParty = encounterParty;
		_npcSurrenderSkipReason = reason ?? "npc_surrender";
		Logger.Log("NpcSurrender", "Enabled hero capture conversation skip for NPC surrender. Party=" + GetPartyLogName(encounterParty) + " reason=" + (_npcSurrenderSkipReason ?? "N/A"));
	}

	private static void ClearNpcSurrenderHeroConversationSkip(string reason)
	{
		if (!_npcSurrenderSkipHeroCaptureConversations)
		{
			return;
		}
		Logger.Log("NpcSurrender", "Cleared hero capture conversation skip for NPC surrender. Reason=" + (reason ?? "N/A"));
		_npcSurrenderSkipHeroCaptureConversations = false;
		_npcSurrenderSkipEncounterParty = null;
		_npcSurrenderSkipReason = null;
	}

	internal static bool TrySkipNpcSurrenderCapturedLordConversation(PlayerEncounter encounter)
	{
		try
		{
			if (!ShouldSkipNpcSurrenderHeroCaptureConversations(encounter))
			{
				return false;
			}
			int moved = MoveHeroLootRosterToMainPartyPrisoners(encounter?.RosterToReceiveLootPrisoners, alreadyPrisonerOnly: false, "captured_lords");
			if (!TrySetPlayerEncounterState(encounter, PlayerEncounterState.FreeHeroes))
			{
				return false;
			}
			Logger.Log("NpcSurrender", "Skipped captured lord conversation for NPC surrender. CapturedHeroesMoved=" + moved + " reason=" + (_npcSurrenderSkipReason ?? "N/A"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Skip captured lord conversation failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TrySkipNpcSurrenderFreeOrCapturePrisonerHeroConversation(PlayerEncounter encounter)
	{
		try
		{
			if (!ShouldSkipNpcSurrenderHeroCaptureConversations(encounter))
			{
				return false;
			}
			int moved = MoveHeroLootRosterToMainPartyPrisoners(encounter?.RosterToReceiveLootMembers, alreadyPrisonerOnly: true, "already_prisoner_lords");
			if (!TrySetPlayerEncounterState(encounter, PlayerEncounterState.LootParty))
			{
				return false;
			}
			Logger.Log("NpcSurrender", "Skipped free-or-capture prisoner lord conversation for NPC surrender. PrisonerHeroesMoved=" + moved + " reason=" + (_npcSurrenderSkipReason ?? "N/A"));
			ClearNpcSurrenderHeroConversationSkip("advanced_to_loot_party");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Skip free-or-capture prisoner lord conversation failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldSkipNpcSurrenderHeroCaptureConversations(PlayerEncounter encounter)
	{
		if (!_npcSurrenderSkipHeroCaptureConversations || encounter == null || PlayerEncounter.Current != encounter)
		{
			return false;
		}
		try
		{
			if (!PlayerEncounter.EnemySurrender)
			{
				ClearNpcSurrenderHeroConversationSkip("enemy_surrender_not_active");
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (_npcSurrenderSkipEncounterParty != null)
			{
				PartyBase encounteredParty = PlayerEncounterCompat.GetEncounteredPartySafe();
				if (encounteredParty != null && encounteredParty != _npcSurrenderSkipEncounterParty)
				{
					ClearNpcSurrenderHeroConversationSkip("encounter_party_changed");
					return false;
				}
			}
		}
		catch
		{
		}
		return true;
	}

	private static int MoveHeroLootRosterToMainPartyPrisoners(TroopRoster roster, bool alreadyPrisonerOnly, string source)
	{
		if (roster == null || PartyBase.MainParty == null)
		{
			return 0;
		}
		List<TroopRosterElement> heroes = null;
		try
		{
			heroes = roster.RemoveIf((TroopRosterElement element) =>
			{
				Hero hero = element.Character?.HeroObject;
				if (hero == null || hero == Hero.MainHero)
				{
					return false;
				}
				return !alreadyPrisonerOnly || hero.PartyBelongedToAsPrisoner != PartyBase.MainParty;
			}).ToList();
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Remove hero loot roster entries failed. Source=" + (source ?? "unknown") + " error=" + ex.Message);
			return 0;
		}
		int moved = 0;
		foreach (TroopRosterElement element in heroes ?? new List<TroopRosterElement>())
		{
			if (TryMoveHeroToMainPartyPrisoners(element.Character, source))
			{
				moved++;
			}
		}
		return moved;
	}

	private static bool TryMoveHeroToMainPartyPrisoners(CharacterObject character, string source)
	{
		Hero hero = character?.HeroObject;
		if (hero == null || hero == Hero.MainHero || PartyBase.MainParty == null)
		{
			return false;
		}
		try
		{
			if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner == PartyBase.MainParty)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase prisonerOwner = hero.PartyBelongedToAsPrisoner;
			if (prisonerOwner != null && prisonerOwner != PartyBase.MainParty && RosterContainsCharacter(prisonerOwner.PrisonRoster, character))
			{
				TransferPrisonerAction.Apply(character, prisonerOwner, PartyBase.MainParty);
				Logger.Log("NpcSurrender", "Transferred already-prisoner hero to player after NPC surrender. Hero=" + (hero.StringId ?? "") + " source=" + (source ?? "unknown") + " from=" + GetPartyLogName(prisonerOwner));
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Transfer already-prisoner hero after NPC surrender failed. Hero=" + (hero.StringId ?? "") + " source=" + (source ?? "unknown") + " error=" + ex.Message);
		}
		try
		{
			TakePrisonerAction.Apply(PartyBase.MainParty, hero);
			Logger.Log("NpcSurrender", "Captured lord directly into player prisoners after NPC surrender. Hero=" + (hero.StringId ?? "") + " source=" + (source ?? "unknown"));
			return true;
		}
		catch (Exception ex2)
		{
			Logger.Log("NpcSurrender", "Capture lord directly after NPC surrender failed. Hero=" + (hero.StringId ?? "") + " source=" + (source ?? "unknown") + " error=" + ex2.Message);
			return false;
		}
	}

	private static bool RosterContainsCharacter(TroopRoster roster, CharacterObject character)
	{
		try
		{
			return roster != null && character != null && roster.FindIndexOfTroop(character) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool MarkPendingNativeConversationNpcSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
		string targetId = targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown";
		try
		{
			if (!TryGetNpcSurrenderEncounterParty(targetHero, targetCharacter, out var encounterParty, out var blockedReason))
			{
				Logger.Log("NpcSurrender", "Ignored native conversation NPC surrender tag. Target=" + targetId + " agentIndex=" + targetAgentIndex + " reason=" + (blockedReason ?? "unknown") + " source=" + (reason ?? "N/A"));
				return false;
			}
			_pendingNativeConversationNpcSurrender = true;
			try
			{
				_pendingNativeConversationNpcSurrenderAtTime = Time.ApplicationTime;
			}
			catch
			{
				_pendingNativeConversationNpcSurrenderAtTime = 0f;
			}
			_pendingNativeConversationNpcSurrenderLastAttemptTime = -1f;
			_pendingNativeConversationNpcSurrenderHero = targetHero;
			_pendingNativeConversationNpcSurrenderCharacter = targetCharacter;
			_pendingNativeConversationNpcSurrenderParty = encounterParty;
			_pendingNativeConversationNpcSurrenderAgentIndex = targetAgentIndex;
			_pendingNativeConversationNpcSurrenderReason = reason ?? "native_conversation_npc_surrender_tag";
			try
			{
				string message = "对方已同意投降。请手动离开当前对话，离开后将进入俘虏与战利品结算。";
				InformationManager.DisplayMessage(new InformationMessage(message, new Color(0.4f, 1f, 0.4f)));
				AnimusForgeQuickInfo.Show(message, targetHero?.CharacterObject ?? targetCharacter);
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "Show pending native NPC surrender prompt failed: " + ex.Message);
			}
			Logger.Log("NpcSurrender", "Marked pending native conversation NPC surrender; waiting for player to leave conversation manually. Target=" + targetId + " party=" + GetPartyLogName(encounterParty) + " agentIndex=" + targetAgentIndex + " reason=" + (reason ?? "N/A"));
			return true;
		}
		catch (Exception ex2)
		{
			Logger.Log("NpcSurrender", "Mark pending native conversation NPC surrender failed. Target=" + targetId + " source=" + (reason ?? "N/A") + " error=" + ex2.Message);
			return false;
		}
	}

	private static bool HasPendingNativeConversationNpcSurrender()
	{
		if (!_pendingNativeConversationNpcSurrender)
		{
			return false;
		}
		if (IsNativeConversationStillActive())
		{
			return true;
		}
		float elapsed = 0f;
		try
		{
			if (_pendingNativeConversationNpcSurrenderAtTime > 0f)
			{
				elapsed = Time.ApplicationTime - _pendingNativeConversationNpcSurrenderAtTime;
			}
		}
		catch
		{
		}
		if (elapsed > 600f)
		{
			ClearPendingNativeConversationNpcSurrender("expired");
			return false;
		}
		return true;
	}

	private static void ClearPendingNativeConversationNpcSurrender(string reason)
	{
		_pendingNativeConversationNpcSurrender = false;
		_pendingNativeConversationNpcSurrenderAtTime = 0f;
		_pendingNativeConversationNpcSurrenderLastAttemptTime = -1f;
		_pendingNativeConversationNpcSurrenderHero = null;
		_pendingNativeConversationNpcSurrenderCharacter = null;
		_pendingNativeConversationNpcSurrenderParty = null;
		_pendingNativeConversationNpcSurrenderAgentIndex = -1;
		_pendingNativeConversationNpcSurrenderReason = null;
		Logger.Log("NpcSurrender", "Cleared pending native conversation NPC surrender. Reason=" + (reason ?? "N/A"));
	}

	private static bool IsNativeConversationStillActive()
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			return conversationManager != null && (conversationManager.IsConversationInProgress || conversationManager.IsConversationFlowActive);
		}
		catch
		{
			return false;
		}
	}

	private static void TryForcePendingNativeConversationNpcSurrenderIfReady()
	{
		if (!HasPendingNativeConversationNpcSurrender())
		{
			return;
		}
		try
		{
			if (IsNativeConversationStillActive())
			{
				return;
			}
		}
		catch
		{
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		try
		{
			float applicationTime = Time.ApplicationTime;
			if (_pendingNativeConversationNpcSurrenderAtTime > 0f && applicationTime - _pendingNativeConversationNpcSurrenderAtTime < 0.25f)
			{
				return;
			}
			if (_pendingNativeConversationNpcSurrenderLastAttemptTime > 0f && applicationTime - _pendingNativeConversationNpcSurrenderLastAttemptTime < 0.25f)
			{
				return;
			}
			_pendingNativeConversationNpcSurrenderLastAttemptTime = applicationTime;
		}
		catch
		{
			_pendingNativeConversationNpcSurrenderLastAttemptTime = 0f;
		}
		try
		{
			if (_pendingNativeConversationNpcSurrenderParty != null)
			{
				_pendingNativeConversationNpcSurrenderCharacter ??= _pendingNativeConversationNpcSurrenderParty.LeaderHero?.CharacterObject;
				_pendingNativeConversationNpcSurrenderHero ??= _pendingNativeConversationNpcSurrenderParty.LeaderHero;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.Current == null && _pendingNativeConversationNpcSurrenderParty != null && PartyBase.MainParty != null)
			{
				try
				{
					PlayerEncounterCompat.RestartPlayerEncounter(_pendingNativeConversationNpcSurrenderParty, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
				}
				catch (Exception ex)
				{
					Logger.Log("NpcSurrender", "RestartPlayerEncounter for pending native NPC surrender failed: " + ex.Message);
				}
				if (PlayerEncounter.Current == null)
				{
					try
					{
						PlayerEncounter.Start();
						if (PlayerEncounter.Current != null)
						{
							PlayerEncounter.Current.SetupFields(PartyBase.MainParty, _pendingNativeConversationNpcSurrenderParty);
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("NpcSurrender", "Start+SetupFields fallback for pending native NPC surrender failed: " + ex2.Message);
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			if (TryExecuteNpcSurrenderFromFreeConversation(_pendingNativeConversationNpcSurrenderHero, _pendingNativeConversationNpcSurrenderCharacter, _pendingNativeConversationNpcSurrenderAgentIndex, _pendingNativeConversationNpcSurrenderReason ?? "native_conversation_npc_surrender_tag", closeConversation: false))
			{
				ClearPendingNativeConversationNpcSurrender("executed");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Force pending native conversation NPC surrender failed: " + ex.Message);
		}
	}

	private static bool TryGetNpcSurrenderEncounterParty(Hero targetHero, CharacterObject targetCharacter, out PartyBase encounterParty, out string blockedReason)
	{
		encounterParty = null;
		blockedReason = null;
		try
		{
			if (PlayerEncounter.Current == null)
			{
				blockedReason = "no_player_encounter";
				return false;
			}
			try
			{
				if (PlayerEncounter.PlayerSurrender)
				{
					blockedReason = "player_surrender_pending";
					return false;
				}
			}
			catch
			{
			}
			encounterParty = PlayerEncounterCompat.GetEncounteredPartySafe();
			if (encounterParty == null)
			{
				try
				{
					encounterParty = PlayerEncounter.EncounteredParty;
				}
				catch
				{
				}
			}
			if (encounterParty == null)
			{
				try
				{
					encounterParty = MobileParty.ConversationParty?.Party;
				}
				catch
				{
				}
			}
			if (encounterParty == null)
			{
				try
				{
					encounterParty = targetHero?.PartyBelongedTo?.Party ?? targetCharacter?.HeroObject?.PartyBelongedTo?.Party;
				}
				catch
				{
				}
			}
			if (encounterParty == null)
			{
				blockedReason = "no_encounter_party";
				return false;
			}
			if (PartyBase.MainParty == null || encounterParty == PartyBase.MainParty || encounterParty.MobileParty == MobileParty.MainParty)
			{
				blockedReason = "main_party_or_missing_main_party";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			blockedReason = "exception:" + ex.Message;
			return false;
		}
	}

	private static MapEvent EnsureNpcSurrenderEncounterBattle(PartyBase encounterParty, string reason)
	{
		try
		{
			MapEvent mapEvent = PlayerEncounterCompat.GetBattleSafe() ?? TryGetCurrentEncounterBattle();
			if (PlayerEncounterCompat.GetBattleSafe() != null)
			{
				return mapEvent;
			}
			if (PlayerEncounter.Current == null)
			{
				Logger.Log("NpcSurrender", "Cannot prepare NPC surrender battle because PlayerEncounter.Current is null. Reason=" + (reason ?? "N/A"));
				return null;
			}
			Logger.Log("NpcSurrender", "Preparing encounter battle for NPC surrender via PlayerEncounter.StartBattle(). Party=" + GetPartyLogName(encounterParty) + " reason=" + (reason ?? "N/A"));
			try
			{
				mapEvent = PlayerEncounter.StartBattle();
			}
			catch (Exception ex)
			{
				Logger.Log("NpcSurrender", "PlayerEncounter.StartBattle for NPC surrender failed: " + ex.Message);
				mapEvent = null;
			}
			MapEvent playerEncounterBattle = PlayerEncounterCompat.GetBattleSafe();
			if (playerEncounterBattle != null)
			{
				return playerEncounterBattle;
			}
			if (mapEvent == null && encounterParty != null && PartyBase.MainParty != null)
			{
				try
				{
					Logger.Log("NpcSurrender", "Fallback battle prep via StartBattleAction.Apply for NPC surrender. Party=" + GetPartyLogName(encounterParty));
					StartBattleAction.Apply(PartyBase.MainParty, encounterParty);
				}
				catch (Exception ex)
				{
					Logger.Log("NpcSurrender", "StartBattleAction fallback for NPC surrender failed: " + ex.Message);
				}
			}
			return PlayerEncounterCompat.GetBattleSafe();
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Ensure NPC surrender encounter battle failed: " + ex.Message);
			return null;
		}
	}

	private static string GetPartyLogName(PartyBase party)
	{
		try
		{
			return party?.Name?.ToString() ?? "unknown";
		}
		catch
		{
			return "unknown";
		}
	}

	internal static bool TryProcessMeetingTauntAction(Hero target, ref string content, out bool escalatedToBattle)
	{
		return TryProcessMeetingTauntAction(target, null, ref content, out escalatedToBattle);
	}

	internal static bool TryProcessMeetingTauntAction(Hero target, PartyBase defenderParty, ref string content, out bool escalatedToBattle)
	{
		escalatedToBattle = false;
		try
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}
			bool flag = MeetingTauntWarnTagRegex.IsMatch(content);
			bool flag2 = MeetingTauntBattleTagRegex.IsMatch(content);
			if (!flag && !flag2)
			{
				return false;
			}
			content = MeetingTauntWarnTagRegex.Replace(content, "").Trim();
			content = MeetingTauntBattleTagRegex.Replace(content, "").Trim();
			Hero hero = target;
			if (hero == null)
			{
				try
				{
					hero = defenderParty?.LeaderHero;
				}
				catch
				{
					hero = null;
				}
			}
			if (hero == null && defenderParty == null)
			{
				hero = EnsureEncounterTargetHero("meeting_taunt_action");
			}
			if (flag)
			{
				Logger.Log("MeetingTaunt", "Legacy warning tag stripped without changing meeting taunt state.");
			}
			if (flag2)
			{
				escalatedToBattle = TryEscalateMeetingTauntToBattle(hero, defenderParty, "meeting_taunt_battle_tag");
			}
			return flag || flag2;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Processing taunt tag failed: " + ex.Message);
			return false;
		}
	}

	public static void StartMeeting(Hero target, MenuCallbackArgs args = null)
	{
		try
		{
			if (target == null)
			{
				target = EnsureEncounterTargetHero("start_meeting_null_target");
				if (target == null)
				{
					Logger.Log("LordEncounter", "StartMeeting aborted because target hero is null.");
					return;
				}
			}
			if (IsMainHeroHealthTooLowForMeeting())
			{
				AnimusForgeQuickInfo.Show(GetLowHealthMeetingBlockedMessage(target), target.CharacterObject);
				return;
			}
			if (IsTargetHeroHealthTooLowForMeeting(target))
			{
				AnimusForgeQuickInfo.Show(GetTargetLowHealthMeetingBlockedMessage(target), target.CharacterObject);
				return;
			}
			SetTarget(target);
			ClearMeetingPlayerReleaseAuthorization("start_meeting");
			ClearPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit("start_meeting");
			_meetingStartedForProactiveRequest = false;
			_meetingStartedForProactiveRequestHero = null;
			_meetingStartedFromCustomEncounterMenu = true;
			_meetingStartedFromCustomEncounterMenuHero = target;
			try
			{
				if (ProactiveNpcRequestBehavior.IsActiveRequestHero(target))
				{
					_meetingStartedForProactiveRequest = true;
					_meetingStartedForProactiveRequestHero = target;
					Logger.Log("MeetingBattle", "Meeting started from proactive NPC request.");
				}
			}
			catch
			{
				_meetingStartedForProactiveRequest = false;
				_meetingStartedForProactiveRequestHero = null;
			}
			_lastMeetingWasSameMapFactionConflict = false;
			_lastMeetingPlayerFactionName = new TextObject("你的势力");
			try
			{
				IFaction faction = PartyBase.MainParty?.MapFaction;
				IFaction faction2 = target?.MapFaction;
				_lastMeetingWasSameMapFactionConflict = faction != null && faction2 != null && faction == faction2;
				if (faction?.Name != null)
				{
					_lastMeetingPlayerFactionName = faction.Name;
				}
			}
			catch
			{
			}
			MeetingBattleRuntime.BeginMeeting(target);
			Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
			SaveMainPartyPosition();
			if (args == null)
			{
				Logger.Log("LordEncounter", "StartMeeting aborted because menu args are null.");
				_meetingStartedForProactiveRequest = false;
				_meetingStartedForProactiveRequestHero = null;
				_meetingStartedFromCustomEncounterMenu = false;
				_meetingStartedFromCustomEncounterMenuHero = null;
				return;
			}
			DisableMeetingSpawnOverride();
			Logger.Log("LordEncounter", "Meeting requested: redirecting to native encounter attack consequence.");
			SuppressCustomEncounterMenuUntilBackOnMap("start_meeting_battle");
			EnsureEncounterBattlePrepared(target);
			LordEncounterRedirectGuard.SuppressForSeconds(8f);
			try
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = false;
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
			}
			catch
			{
			}
			try
			{
				MenuHelper.EncounterAttackConsequence(args);
			}
			catch (NullReferenceException ex)
			{
				Logger.Log("LordEncounter", "EncounterAttackConsequence null-ref; falling back to direct battle mission open. " + ex.Message);
				OpenBattleMissionFallbackFromEncounter();
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("LordEncounter", "StartMeeting failed: " + ex2);
			MeetingBattleRuntime.EndMeeting();
			DisableMeetingSpawnOverride();
			_meetingStartedForProactiveRequest = false;
			_meetingStartedForProactiveRequestHero = null;
			_meetingStartedFromCustomEncounterMenu = false;
			_meetingStartedFromCustomEncounterMenuHero = null;
		}
	}

	private static void OpenNativeEncounterConversation(Hero target)
	{
		try
		{
			if (target == null)
			{
				target = EnsureEncounterTargetHero("open_native_conversation_null_target");
				if (target == null)
				{
					Logger.Log("LordEncounter", "OpenNativeEncounterConversation aborted because target hero is null.");
					return;
				}
			}
			SetTarget(target);
			SuppressCustomEncounterMenuUntilBackOnMap("native_dialogue_handoff");
			Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
			try
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = false;
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
			}
			catch
			{
			}
			try
			{
				PlayerEncounter.SetMeetingDone();
			}
			catch
			{
			}
			PartyBase partyBase = null;
			try
			{
				partyBase = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				partyBase = null;
			}
			partyBase = partyBase ?? target.PartyBelongedTo?.Party;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(target.CharacterObject, partyBase, false, false, false, false, false, false);
			IsOpeningConversation = true;
			try
			{
				if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
				{
					CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
				}
				else
				{
					CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
				}
			}
			finally
			{
				IsOpeningConversation = false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "OpenNativeEncounterConversation failed: " + ex);
		}
	}

	private static void EnsureEncounterBattlePrepared(Hero target)
	{
		try
		{
			MapEvent mapEvent = TryGetCurrentEncounterBattle();
			if (mapEvent != null)
			{
				return;
			}
			if (PlayerEncounter.Current == null)
			{
				throw new InvalidOperationException("PlayerEncounter.Current is null when preparing meeting battle.");
			}
			Logger.Log("LordEncounter", "Preparing encounter battle via PlayerEncounter.StartBattle().");
			try
			{
				mapEvent = PlayerEncounter.StartBattle();
			}
			catch
			{
				mapEvent = null;
			}
			if (mapEvent == null)
			{
				PartyBase partyBase = null;
				try
				{
					partyBase = PlayerEncounter.EncounteredParty;
				}
				catch
				{
				}
				if (partyBase == null)
				{
					try
					{
						partyBase = target?.PartyBelongedTo?.Party;
					}
					catch
					{
					}
				}
				if (partyBase != null)
				{
					Logger.Log("LordEncounter", $"Fallback battle prep via StartBattleAction.Apply. Defender={partyBase.Name}");
					StartBattleAction.Apply(PartyBase.MainParty, partyBase);
				}
			}
			mapEvent = TryGetCurrentEncounterBattle();
			if (mapEvent != null)
			{
				return;
			}
			throw new InvalidOperationException("Battle is still null after encounter battle preparation.");
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "EnsureEncounterBattlePrepared failed: " + ex);
			throw;
		}
	}

	private static MapEvent TryGetCurrentEncounterBattle()
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

	private static void OpenBattleMissionFallbackFromEncounter()
	{
		MapEvent mapEvent = TryGetCurrentEncounterBattle();
		if (mapEvent == null)
		{
			mapEvent = PlayerEncounter.StartBattle();
		}
		if (mapEvent == null)
		{
			throw new InvalidOperationException("Cannot fallback-open mission because battle is null.");
		}
		if (mapEvent.MapEventSettlement != null && mapEvent.MapEventSettlement.IsVillage)
		{
			PlayerEncounter.StartVillageBattleMission();
			PlayerEncounter.StartAttackMission();
			MapEvent.PlayerMapEvent?.BeginWait();
			return;
		}
		bool flag = PlayerEncounter.IsNavalEncounter();
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position);
		string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, flag);
		MissionInitializerRecord rec = new MissionInitializerRecord(battleSceneForMapPatch);
		TerrainType faceTerrainType = BannerlordApiCompat.ResolveTerrainTypeForParty(MobileParty.MainParty, TerrainType.Plain, allowNavigationFaceFallback: false);
		rec.TerrainType = (int)faceTerrainType;
		rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = true;
		rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
		rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		rec.SceneHasMapPatch = true;
		rec.DecalAtlasGroup = 2;
		rec.PatchCoordinates = mapPatchAtPosition.normalizedCoordinates;
		Vec2 vec = mapEvent.AttackerSide.LeaderParty.Position.ToVec2();
		rec.PatchEncounterDir = (vec - mapEvent.DefenderSide.LeaderParty.Position.ToVec2()).Normalized();
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent ?? mapEvent;
		bool flag2 = playerMapEvent != null && playerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty p) => p.Party.IsMobile && (p.Party.MobileParty.IsCaravan || (p.Party.Owner != null && p.Party.Owner.IsMerchant)));
		bool flag3 = playerMapEvent != null && playerMapEvent.MapEventSettlement == null && playerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty p) => p.Party.IsMobile && p.Party.MobileParty.IsVillager);
		if (flag)
		{
			CampaignMission.OpenNavalBattleMission(rec);
		}
		else if (flag2 || flag3)
		{
			CampaignMission.OpenCaravanBattleMission(rec, flag2);
		}
		else
		{
			CampaignMission.OpenBattleMission(rec);
		}
		PlayerEncounter.StartAttackMission();
		MapEvent.PlayerMapEvent?.BeginWait();
	}

	private static Vec2 BuildMeetingPatchEncounterDirection(Hero target)
	{
		Vec2 result = new Vec2(1f, 0f);
		try
		{
			MapEvent mapEvent = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
			if (mapEvent != null && mapEvent.AttackerSide?.LeaderParty != null && mapEvent.DefenderSide?.LeaderParty != null)
			{
				Vec2 vec = mapEvent.AttackerSide.LeaderParty.Position.ToVec2();
				Vec2 vec2 = mapEvent.DefenderSide.LeaderParty.Position.ToVec2();
				Vec2 vec3 = vec - vec2;
				if (vec3.LengthSquared > 0.0001f)
				{
					return vec3.Normalized();
				}
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty != null && target?.PartyBelongedTo != null)
			{
				Vec2 vec4 = MobileParty.MainParty.Position.ToVec2() - target.PartyBelongedTo.Position.ToVec2();
				if (vec4.LengthSquared > 0.0001f)
				{
					result = vec4.Normalized();
				}
			}
		}
		catch
		{
		}
		return result;
	}

	internal static bool TryOverrideNextPlayerSpawnFrame(ref MatrixFrame spawnFrame, bool consume)
	{
		if (!_meetingSpawnOverrideActive)
		{
			return false;
		}
		if (!_overrideNextPlayerSpawnFrame)
		{
			return false;
		}
		if (!_preferPreparedPlayerSpawnFrame)
		{
			_nextPlayerSpawnFrame = BuildPlayerSpawnFrame();
		}
		spawnFrame = _nextPlayerSpawnFrame;
		if (consume)
		{
			_overrideNextPlayerSpawnFrame = false;
			_preferPreparedPlayerSpawnFrame = false;
		}
		return true;
	}

	internal static void SetPreparedPlayerSpawnFrame(MatrixFrame frame)
	{
		_nextPlayerSpawnFrame = frame;
		_overrideNextPlayerSpawnFrame = true;
		_preferPreparedPlayerSpawnFrame = true;
	}

	internal static void ClearPreparedPlayerSpawnFrame()
	{
		_preferPreparedPlayerSpawnFrame = false;
	}

	internal static bool TryConsumeNextTargetHeroSpawnFrame(out MatrixFrame spawnFrame)
	{
		if (!_meetingSpawnOverrideActive)
		{
			spawnFrame = default(MatrixFrame);
			return false;
		}
		if (!_overrideNextTargetHeroSpawnFrame)
		{
			spawnFrame = default(MatrixFrame);
			return false;
		}
		_nextTargetHeroSpawnFrame = BuildTargetHeroSpawnFrame();
		spawnFrame = _nextTargetHeroSpawnFrame;
		_overrideNextTargetHeroSpawnFrame = false;
		return true;
	}

	private static bool TryGetMeetingSceneCenter(out Vec3 center)
	{
		center = Vec3.Zero;
		try
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return false;
			}
			if (TryGetBoundaryPolygonCenter(scene, out var center2D))
			{
				center = new Vec3(center2D.x, center2D.y);
				ResolveSceneGroundHeight(scene, ref center);
				return true;
			}
			scene.GetBoundingBox(out var min, out var max);
			if (min == Vec3.Invalid || max == Vec3.Invalid)
			{
				scene.GetSceneLimits(out min, out max);
			}
			center = new Vec3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f);
			ResolveSceneGroundHeight(scene, ref center);
			return true;
		}
		catch
		{
			center = Vec3.Zero;
			return false;
		}
	}

	private static bool TryGetBoundaryPolygonCenter(Scene scene, out Vec2 center2D)
	{
		center2D = Vec2.Zero;
		if (!TryGetMissionBoundaryPolygon(scene, out var polygon) || polygon.Count < 3)
		{
			return false;
		}
		if (!TryComputePolygonCentroid(polygon, out var centroid))
		{
			return false;
		}
		if (IsPointInsidePolygon(centroid, polygon))
		{
			center2D = centroid;
			return true;
		}
		if (TryFindNearestInsidePoint(polygon, centroid, out var insidePoint))
		{
			center2D = insidePoint;
			return true;
		}
		return false;
	}

	private static bool TryGetMissionBoundaryPolygon(Scene scene, out List<Vec2> polygon)
	{
		polygon = new List<Vec2>();
		if (scene == null)
		{
			return false;
		}
		try
		{
			int num = 0;
			try
			{
				num = scene.GetHardBoundaryVertexCount();
			}
			catch
			{
				num = 0;
			}
			if (num > 2)
			{
				for (int i = 0; i < num; i++)
				{
					try
					{
						polygon.Add(scene.GetHardBoundaryVertex(i));
					}
					catch
					{
					}
				}
			}
			if (polygon.Count < 3)
			{
				polygon.Clear();
				try
				{
					num = scene.GetSoftBoundaryVertexCount();
				}
				catch
				{
					num = 0;
				}
				if (num > 2)
				{
					for (int j = 0; j < num; j++)
					{
						try
						{
							polygon.Add(scene.GetSoftBoundaryVertex(j));
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
			polygon.Clear();
		}
		if (polygon.Count >= 2)
		{
			Vec2 vec = polygon[0];
			Vec2 vec2 = polygon[polygon.Count - 1];
			if ((vec - vec2).LengthSquared < 0.0001f)
			{
				polygon.RemoveAt(polygon.Count - 1);
			}
		}
		return polygon.Count >= 3;
	}

	private static bool TryComputePolygonCentroid(List<Vec2> polygon, out Vec2 centroid)
	{
		centroid = Vec2.Zero;
		if (polygon == null || polygon.Count < 3)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int count = polygon.Count;
		for (int i = 0; i < count; i++)
		{
			Vec2 vec = polygon[i];
			Vec2 vec2 = polygon[(i + 1) % count];
			float num4 = vec.x * vec2.y - vec2.x * vec.y;
			num += num4;
			num2 += (vec.x + vec2.x) * num4;
			num3 += (vec.y + vec2.y) * num4;
		}
		if (MathF.Abs(num) < 0.0001f)
		{
			float num5 = 0f;
			float num6 = 0f;
			for (int j = 0; j < count; j++)
			{
				num5 += polygon[j].x;
				num6 += polygon[j].y;
			}
			centroid = new Vec2(num5 / (float)count, num6 / (float)count);
			return true;
		}
		float num7 = 1f / (3f * num);
		centroid = new Vec2(num2 * num7, num3 * num7);
		return true;
	}

	private static bool IsPointInsidePolygon(Vec2 p, List<Vec2> polygon)
	{
		if (polygon == null || polygon.Count < 3)
		{
			return false;
		}
		bool flag = false;
		int count = polygon.Count;
		int num = 0;
		int index = count - 1;
		while (num < count)
		{
			Vec2 vec = polygon[num];
			Vec2 vec2 = polygon[index];
			if (vec.y > p.y != vec2.y > p.y && p.x < (vec2.x - vec.x) * (p.y - vec.y) / (vec2.y - vec.y + 1E-06f) + vec.x)
			{
				flag = !flag;
			}
			index = num++;
		}
		return flag;
	}

	private static bool TryFindNearestInsidePoint(List<Vec2> polygon, Vec2 preferred, out Vec2 insidePoint)
	{
		insidePoint = Vec2.Zero;
		if (polygon == null || polygon.Count < 3)
		{
			return false;
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		for (int i = 0; i < polygon.Count; i++)
		{
			Vec2 vec = polygon[i];
			if (vec.x < num)
			{
				num = vec.x;
			}
			if (vec.y < num2)
			{
				num2 = vec.y;
			}
			if (vec.x > num3)
			{
				num3 = vec.x;
			}
			if (vec.y > num4)
			{
				num4 = vec.y;
			}
		}
		if (num3 - num < 0.01f || num4 - num2 < 0.01f)
		{
			return false;
		}
		bool flag = false;
		float num5 = float.MaxValue;
		int num6 = 18;
		for (int j = 0; j <= num6; j++)
		{
			float a = num + (num3 - num) * ((float)j / (float)num6);
			for (int k = 0; k <= num6; k++)
			{
				float b = num2 + (num4 - num2) * ((float)k / (float)num6);
				Vec2 vec2 = new Vec2(a, b);
				if (IsPointInsidePolygon(vec2, polygon))
				{
					float lengthSquared = (vec2 - preferred).LengthSquared;
					if (!flag || lengthSquared < num5)
					{
						flag = true;
						num5 = lengthSquared;
						insidePoint = vec2;
					}
				}
			}
		}
		return flag;
	}

	internal static void ResolveSceneGroundHeight(Scene scene, ref Vec3 pos)
	{
		if (scene == null)
		{
			return;
		}
		try
		{
			float height = pos.z;
			if (scene.GetHeightAtPoint(pos.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
			{
				pos.z = height;
			}
			else
			{
				pos.z = scene.GetGroundHeightAtPosition(pos);
			}
		}
		catch
		{
		}
	}

	internal static void ClampPointInsideMissionBoundary(ref Vec3 candidate, Vec3 anchor)
	{
		try
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null || !TryGetMissionBoundaryPolygon(scene, out var polygon) || polygon.Count < 3)
			{
				return;
			}
			Vec2 asVec = candidate.AsVec2;
			if (IsPointInsidePolygon(asVec, polygon))
			{
				return;
			}
			Vec2 asVec2 = anchor.AsVec2;
			Vec2 vec = asVec2;
			bool flag = false;
			for (int i = 1; i <= 25; i++)
			{
				float num = (float)i / 25f;
				Vec2 vec2 = asVec + (asVec2 - asVec) * num;
				if (IsPointInsidePolygon(vec2, polygon))
				{
					vec = vec2;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (!TryFindNearestInsidePoint(polygon, asVec2, out var insidePoint))
				{
					return;
				}
				vec = insidePoint;
			}
			candidate.x = vec.x;
			candidate.y = vec.y;
			ResolveSceneGroundHeight(scene, ref candidate);
		}
		catch
		{
		}
	}

	internal static MatrixFrame BuildPlayerSpawnFrame()
	{
		MatrixFrame matrixFrame = BuildTargetHeroSpawnFrame();
		Vec3 origin = matrixFrame.origin;
		Vec3 vec = matrixFrame.rotation.f;
		vec.z = 0f;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec3(1f);
		}
		vec.Normalize();
		Vec3 vec2 = new Vec3(0f - vec.y, vec.x);
		if (vec2.LengthSquared < 0.0001f)
		{
			vec2 = new Vec3(0f, 1f);
		}
		vec2.Normalize();
		Vec3 candidate = origin + vec * 12.4f - vec2 * 0.7f;
		ClampPointInsideMissionBoundary(ref candidate, origin);
		try
		{
			Scene scene = Mission.Current?.Scene;
			if (scene != null)
			{
				float height = candidate.z;
				if (scene.GetHeightAtPoint(candidate.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
				{
					candidate.z = height;
				}
				else
				{
					candidate.z = scene.GetGroundHeightAtPosition(candidate);
				}
			}
		}
		catch
		{
		}
		Vec3 f = -vec;
		f.z = 0f;
		if (f.LengthSquared < 0.0001f)
		{
			f = new Vec3(-1f);
		}
		f.Normalize();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = candidate;
		identity.rotation.f = f;
		identity.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return identity;
	}

	private static bool TryPrepareNextTargetHeroSpawnFrame()
	{
		_nextTargetHeroSpawnFrame = BuildTargetHeroSpawnFrame();
		_overrideNextTargetHeroSpawnFrame = true;
		return true;
	}

	internal static MatrixFrame BuildTargetHeroSpawnFrame()
	{
		Vec3 origin = _targetHeroSpawnPos;
		Vec3 f = _targetHeroSpawnForward;
		if (TryGetMeetingSceneCenter(out var center))
		{
			origin = center;
			_targetHeroSpawnPos = center;
		}
		try
		{
			Vec2 vec = BuildMeetingPatchEncounterDirection(_targetHero);
			if (vec.LengthSquared > 0.0001f)
			{
				f = (_targetHeroSpawnForward = new Vec3(vec.x, vec.y));
			}
		}
		catch
		{
		}
		f.z = 0f;
		if (f.LengthSquared < 0.0001f)
		{
			f = new Vec3(1f);
		}
		f.Normalize();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = origin;
		identity.rotation.f = f;
		identity.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return identity;
	}

	private static void EnableMeetingSpawnOverride()
	{
		_meetingSpawnOverrideActive = true;
	}

	private static void DisableMeetingSpawnOverride()
	{
		_meetingSpawnOverrideActive = false;
		_overrideNextPlayerSpawnFrame = false;
		_preferPreparedPlayerSpawnFrame = false;
		_overrideNextTargetHeroSpawnFrame = false;
	}

	private static void SaveMainPartyPosition()
	{
		if (MobileParty.MainParty == null)
		{
			return;
		}
		_savedMainPartyPosition = MobileParty.MainParty.Position;
		_hasSavedMainPartyPosition = _savedMainPartyPosition.IsValid();
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				string text = FormatSettlementNameWithType(Settlement.CurrentSettlement);
				if (string.IsNullOrEmpty(text))
				{
					text = Settlement.CurrentSettlement.Name.ToString();
				}
				_encounterMeetingLocationInfoOverride = "你位于 " + text + "。";
			}
			else
			{
				Settlement settlement = null;
				try
				{
					settlement = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement s) => s != null && !s.IsHideout);
				}
				catch
				{
				}
				if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(MobileParty.MainParty))
				{
					if (settlement != null)
					{
						string seaNearestName = FormatSettlementNameWithType(settlement);
						if (string.IsNullOrEmpty(seaNearestName))
						{
							seaNearestName = settlement.Name.ToString();
						}
						_encounterMeetingLocationInfoOverride = "你正位于" + seaNearestName + "附近的海上。";
					}
					else
					{
						_encounterMeetingLocationInfoOverride = "你正位于海上。";
					}
					return;
				}
				if (settlement != null)
				{
					string text2 = FormatSettlementNameWithType(settlement);
					if (string.IsNullOrEmpty(text2))
					{
						text2 = settlement.Name.ToString();
					}
					float num = 0f;
					bool flag = false;
					try
					{
						if (_hasSavedMainPartyPosition && _savedMainPartyPosition.IsValid() && settlement.GatePosition.IsValid())
						{
							num = MathF.Sqrt(settlement.GatePosition.DistanceSquared(_savedMainPartyPosition));
							flag = num > 0.001f;
						}
						else if (MobileParty.MainParty != null && MobileParty.MainParty.Position.IsValid() && settlement.GatePosition.IsValid())
						{
							num = MathF.Sqrt(settlement.GatePosition.DistanceSquared(MobileParty.MainParty.Position));
							flag = num > 0.001f;
						}
					}
					catch
					{
						flag = false;
					}
					_encounterMeetingLocationInfoOverride = (flag ? $"你身处野外，靠近 {text2}。距离：{num:0.0} 公里。" : ("你身处野外，靠近 " + text2 + "。"));
				}
				else
				{
					_encounterMeetingLocationInfoOverride = "你身处野外。";
				}
			}
			try
			{
				if (!_hasSavedMainPartyPosition || Campaign.Current == null || Campaign.Current.MapSceneWrapper == null)
				{
					return;
				}
				TerrainType terrainTypeAtPosition = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(in _savedMainPartyPosition);
				string text3 = MapSeaContextGuard.BuildTerrainPromptLabel(terrainTypeAtPosition);
				string text4 = "";
				try
				{
					MapWeatherModel mapWeatherModel = Campaign.Current.Models?.MapWeatherModel;
					if (mapWeatherModel != null)
					{
						MapWeatherModel.WeatherEvent weatherEventInPosition = mapWeatherModel.GetWeatherEventInPosition(_savedMainPartyPosition.ToVec2());
						text4 = weatherEventInPosition switch
						{
							MapWeatherModel.WeatherEvent.Clear => "晴朗",
							MapWeatherModel.WeatherEvent.LightRain => "小雨",
							MapWeatherModel.WeatherEvent.HeavyRain => "大雨",
							MapWeatherModel.WeatherEvent.Snowy => "降雪",
							MapWeatherModel.WeatherEvent.Blizzard => "暴风雪",
							MapWeatherModel.WeatherEvent.Storm => "风暴",
							_ => weatherEventInPosition.ToString(),
						};
					}
				}
				catch
				{
					text4 = "";
				}
				List<string> list = new List<string>();
				list.Add("地形：" + text3);
				if (!string.IsNullOrEmpty(text4))
				{
					list.Add("天气：" + text4);
				}
				if (list.Count <= 0)
				{
					return;
				}
				string text5 = string.Join("；", list).Trim();
				if (!string.IsNullOrEmpty(text5))
				{
					_encounterMeetingLocationInfoOverride = (_encounterMeetingLocationInfoOverride ?? "").Trim();
					if (!string.IsNullOrEmpty(_encounterMeetingLocationInfoOverride) && !_encounterMeetingLocationInfoOverride.EndsWith("。", StringComparison.Ordinal))
					{
						_encounterMeetingLocationInfoOverride += "。";
					}
					_encounterMeetingLocationInfoOverride = _encounterMeetingLocationInfoOverride + " " + text5 + "。";
				}
			}
			catch
			{
			}
		}
		catch
		{
			_encounterMeetingLocationInfoOverride = null;
		}
		static string FormatSettlementNameWithType(Settlement st)
		{
			if (st == null)
			{
				return "";
			}
			string text6 = (st.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrEmpty(text6))
			{
				return "";
			}
			string text7 = (st.IsTown ? "城镇" : (st.IsCastle ? "城堡" : (st.IsVillage ? "村庄" : ((!st.IsFortification) ? "定居点" : "要塞"))));
			return text6 + "（" + text7 + "）";
		}
	}

	private static void RestoreMainPartyPosition()
	{
		try
		{
			if (_hasSavedMainPartyPosition && MobileParty.MainParty != null)
			{
				MobileParty.MainParty.SetPositionAfterMapChange(_savedMainPartyPosition);
			}
		}
		catch
		{
		}
		finally
		{
			_hasSavedMainPartyPosition = false;
			_encounterMeetingLocationInfoOverride = null;
		}
	}
}
