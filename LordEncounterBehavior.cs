using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
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

	private static bool _pendingMeetingBattleVictorySettlement;

	private static float _pendingMeetingBattleVictorySettlementAtTime;

	private static PartyBase _pendingMeetingBattleVictorySettlementEncounterParty;

	private static Hero _pendingMeetingBattleVictorySettlementEncounterLeader;

	private static List<string> _meetingTauntWarnedHeroIds = new List<string>();

	private static readonly Regex MeetingTauntWarnTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_WARN\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MeetingTauntBattleTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_BATTLE\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
		if (_meetingTauntWarnedHeroIds == null)
		{
			_meetingTauntWarnedHeroIds = new List<string>();
		}
		dataStore.SyncData("_meetingTauntWarnedHeroIds_v1", ref _meetingTauntWarnedHeroIds);
		if (_meetingTauntWarnedHeroIds == null)
		{
			_meetingTauntWarnedHeroIds = new List<string>();
			return;
		}
		_meetingTauntWarnedHeroIds = _meetingTauntWarnedHeroIds.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
		AddConversationOptions(starter);
	}

	private void OnMissionEnded(IMission mission)
	{
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
			flag14 = HasPendingMeetingBattleVictorySettlement();
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
		bool flag18 = flag12 || flag13 || flag17 || flag || flag14 || flag15 || flag16;
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
		bool flag9 = flag2 || flag || flag5 || flag6 || flag7 || flag8;
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
		bool flag10 = flag2 && !flag && !flag3;
		bool flag11 = flag2 && flag && !flag3 && !flag4 && !flag6;
		if (flag2 && flag3)
		{
			ClearPendingMeetingBattleVictorySettlement("mission_result_defeat");
			MarkPendingForceNativeDefeatCaptivityMenu("meeting_battle_mission_result_defeat");
			TryResolvePendingDefeatCaptivityImmediately("mission_ended_player_defeated");
		}
		else if (flag2 && flag4)
		{
			MarkPendingMeetingBattleVictorySettlement("meeting_battle_mission_result_victory");
			TryResolvePendingMeetingBattleVictorySettlementImmediately("mission_ended_player_victory");
		}
		else if (flag11)
		{
			MarkPendingForceNativeEncounterBattleMenu("meeting_battle_mission_exit_incomplete");
		}
		if (flag2 && !flag4)
		{
			DisableCustomEncounterMenuForCurrentEncounter("meeting_battle_mission_ended");
		}
		Logger.Log("MeetingBattle", $"OnMissionEnded: combatEscalated={flag}, missionWasBattle={flag2}, missionResultPlayerDefeated={flag3}, missionResultPlayerVictory={flag4}, hasBattleResult={flag5}, hasResolvedBattleResult={flag6}, hasEncounterBattleContext={flag7}, hasEncounterResolvingState={flag8}, nativeResultFlow={flag9}, peacefulCleanup={flag10}, forceNativeEncounterMenu={flag11}");
		MeetingBattleRuntime.EndMeeting();
		_pendingPostMissionCleanup = true;
		_pendingPostMissionCleanupDelay = 0f;
		_pendingPeacefulMeetingBattleCleanup = flag10;
		_encounterMeetingMissionActive = false;
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

	internal static bool IsCustomEncounterMenuDisabledForCurrentEncounter()
	{
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
					flag3 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
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

	private static void MarkPendingMeetingBattleVictorySettlement(string reason)
	{
		_pendingMeetingBattleVictorySettlement = true;
		try
		{
			_pendingMeetingBattleVictorySettlementAtTime = Time.ApplicationTime;
		}
		catch
		{
			_pendingMeetingBattleVictorySettlementAtTime = 0f;
		}
		try
		{
			_pendingMeetingBattleVictorySettlementEncounterParty = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			_pendingMeetingBattleVictorySettlementEncounterParty = null;
		}
		try
		{
			_pendingMeetingBattleVictorySettlementEncounterLeader = _pendingMeetingBattleVictorySettlementEncounterParty?.LeaderHero ?? _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		catch
		{
			_pendingMeetingBattleVictorySettlementEncounterLeader = _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		Logger.Log("LordEncounter", string.Format("Marked pending meeting battle victory settlement. Reason={0}, Target={1}", reason ?? "N/A", _pendingMeetingBattleVictorySettlementEncounterLeader?.Name));
	}

	internal static bool HasPendingMeetingBattleVictorySettlement()
	{
		if (!_pendingMeetingBattleVictorySettlement)
		{
			return false;
		}
		float num = 0f;
		try
		{
			if (_pendingMeetingBattleVictorySettlementAtTime > 0f)
			{
				num = Time.ApplicationTime - _pendingMeetingBattleVictorySettlementAtTime;
			}
		}
		catch
		{
		}
		PartyBase partyBase = null;
		Hero hero = null;
		try
		{
			partyBase = PlayerEncounter.EncounteredParty;
			hero = partyBase?.LeaderHero;
		}
		catch
		{
			partyBase = null;
			hero = null;
		}
		if (_pendingMeetingBattleVictorySettlementEncounterParty != null && partyBase != null && partyBase != _pendingMeetingBattleVictorySettlementEncounterParty)
		{
			ClearPendingMeetingBattleVictorySettlement("encounter_party_changed");
			return false;
		}
		if (_pendingMeetingBattleVictorySettlementEncounterLeader != null && hero != null && hero != _pendingMeetingBattleVictorySettlementEncounterLeader)
		{
			ClearPendingMeetingBattleVictorySettlement("encounter_leader_changed");
			return false;
		}
		if (num > 25f)
		{
			ClearPendingMeetingBattleVictorySettlement("expired");
			return false;
		}
		return true;
	}

	private static void ClearPendingMeetingBattleVictorySettlement(string reason)
	{
		_pendingMeetingBattleVictorySettlement = false;
		_pendingMeetingBattleVictorySettlementAtTime = 0f;
		_pendingMeetingBattleVictorySettlementEncounterParty = null;
		_pendingMeetingBattleVictorySettlementEncounterLeader = null;
		Logger.Log("LordEncounter", "Cleared pending meeting battle victory settlement. Reason=" + (reason ?? "N/A"));
	}

	private static void TryForcePendingMeetingBattleVictorySettlementIfReady()
	{
		if (!HasPendingMeetingBattleVictorySettlement())
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
		if (text == "AnimusForge_lord_encounter")
		{
			return;
		}
		bool flag = false;
		try
		{
			object obj3 = Game.Current?.GameStateManager?.ActiveState;
			flag = obj3 != null && obj3.GetType().Name == "MapState";
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
			Hero hero = null;
			try
			{
				hero = PlayerEncounter.EncounteredParty?.LeaderHero;
			}
			catch
			{
				hero = null;
			}
			if (hero == null)
			{
				hero = _pendingMeetingBattleVictorySettlementEncounterLeader;
			}
			if (hero != null && hero != Hero.MainHero && hero.IsLord)
			{
				SetTarget(hero);
			}
			if (TryResolvePendingMeetingBattleVictorySettlementImmediately("campaign_tick_pending_meeting_victory"))
			{
				return;
			}
			GameMenu.ActivateGameMenu("AnimusForge_lord_encounter");
			string text2 = null;
			try
			{
				text2 = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
				text2 = null;
			}
			if (text2 == "AnimusForge_lord_encounter")
			{
				Logger.Log("LordEncounter", "Forced custom post-battle settlement menu open from pending meeting victory marker.");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Force pending meeting victory settlement failed: " + ex.Message);
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
			GameMenu.ActivateGameMenu("encounter");
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

	private static bool TryResolvePendingMeetingBattleVictorySettlementImmediately(string reason)
	{
		if (!HasPendingMeetingBattleVictorySettlement())
		{
			return false;
		}
		Hero hero = null;
		try
		{
			hero = PlayerEncounter.EncounteredParty?.LeaderHero;
		}
		catch
		{
			hero = null;
		}
		if (hero == null)
		{
			hero = _pendingMeetingBattleVictorySettlementEncounterLeader;
		}
		return TryEnterNativePostBattleSettlement(hero, reason, showFailureMessage: false);
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
			PlayerEncounter.RestartPlayerEncounter(partyBase2, partyBase3, forcePlayerOutFromSettlement: false);
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
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "OnMissionStarted failed: " + ex.Message);
		}
	}

	private void TryRunPostMissionCleanupIfReady()
	{
		if (!_pendingPostMissionCleanup || _pendingPostMissionCleanupDelay > 0f || Game.Current?.GameStateManager?.ActiveState is MissionState)
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

	public static void OpenEncounterMenu(Hero target)
	{
		if (target == null)
		{
			return;
		}
		if (IsCustomEncounterMenuDisabledForCurrentEncounter())
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because custom encounter menu is disabled. Target={target.Name}");
			return;
		}
		if (IsEncounterRedirectSuspended())
		{
			Logger.Log("LordEncounter", $"OpenEncounterMenu ignored because redirect is suspended. Target={target.Name}");
			return;
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
			GameMenu.ActivateGameMenu("AnimusForge_lord_encounter");
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "Failed to activate menu: " + ex.Message);
		}
	}

	public static void SetTarget(Hero target)
	{
		_targetHero = target;
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
				flag4 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
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
				if (num2 <= 0.2f)
				{
					return true;
				}
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
				faction2 = PlayerEncounter.EncounteredParty?.MapFaction;
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

	private static Hero TryResolveEncounterLeaderHero()
	{
		try
		{
			Hero hero = PlayerEncounter.EncounteredParty?.LeaderHero;
			if (hero != null && hero != Hero.MainHero && hero.IsLord)
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
		Hero hero = TryResolveEncounterLeaderHero();
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
			bool flag = false;
			try
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				flag = encounteredParty == null || encounteredParty.LeaderHero != _targetHero;
			}
			catch
			{
				flag = true;
			}
			if (flag)
			{
				Logger.Log("LordEncounter", "Clearing stale encounter target. Reason=" + (reason ?? "N/A"));
				_targetHero = null;
			}
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
		if (args?.MenuContext?.GameMenu?.StringId == "AnimusForge_lord_encounter")
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				TryForcePendingDefeatCaptivityMenuIfReady();
				return;
			}
			EnsureEncounterTargetHero("menu_opened");
			TryRunPostMissionCleanupIfReady();
			_cameraLockWasActive = true;
			FocusMapCameraOnMainParty();
		}
	}

	private void OnCampaignTick(float dt)
	{
		TryClearEncounterRedirectSuspensionWhenBackOnMap();
		TryForcePendingDefeatCaptivityMenuIfReady();
		TryForcePendingMeetingBattleVictorySettlementIfReady();
		TryForcePendingEncounterBattleMenuIfReady();
		try
		{
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
		if (!(text == "AnimusForge_lord_encounter"))
		{
			if (_cameraLockWasActive)
			{
				_cameraLockWasActive = false;
			}
			return;
		}
		if (_targetHero == null)
		{
			EnsureEncounterTargetHero("menu_tick_recover");
		}
		_cameraLockWasActive = true;
		FocusMapCameraOnMainParty();
	}

	private static void TryClearEncounterRedirectSuspensionWhenBackOnMap()
	{
		if (!_suspendEncounterRedirectDuringResultResolution)
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

	private static bool TryBuildMeetingPostBattleSettlementText(Hero target, out TextObject bodyText)
	{
		bodyText = new TextObject("");
		CampaignBattleResult campaignBattleResult = null;
		try
		{
			campaignBattleResult = PlayerEncounter.CampaignBattleResult;
		}
		catch
		{
			campaignBattleResult = null;
		}
		bool flag = false;
		if (campaignBattleResult != null)
		{
			try
			{
				flag = campaignBattleResult.PlayerVictory;
			}
			catch
			{
				flag = false;
			}
		}
		else
		{
			try
			{
				flag = HasPendingMeetingBattleVictorySettlement();
			}
			catch
			{
				flag = false;
			}
		}
		if (!flag)
		{
			return false;
		}
		TextObject content = target?.Name ?? new TextObject("对方领主");
		GameTexts.SetVariable("TARGET_NAME", content);
		bodyText = new TextObject("你们在会面中产生了冲突，并且你将{TARGET_NAME}击败了，现在可以进入战后结算了。");
		return true;
	}

	private static void EnterPostBattleSettlementFromMeetingMenu(Hero target)
	{
		TryEnterNativePostBattleSettlement(target, "manual_enter_post_battle_settlement", showFailureMessage: true);
	}

	private static bool TryEnterNativePostBattleSettlement(Hero target, string reason, bool showFailureMessage)
	{
		try
		{
			SuspendEncounterRedirectDuringResultResolution(reason ?? "enter_post_battle_settlement");
		}
		catch
		{
		}
		try
		{
			LordEncounterRedirectGuard.SuppressForSeconds(6f);
		}
		catch
		{
		}
		if (!TryEnsureEncounterContextForPostBattleSettlement(target))
		{
			Logger.Log("LordEncounter", "EnterPostBattleSettlement aborted: failed to ensure encounter context.");
			if (showFailureMessage)
			{
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("战后结算上下文未就绪，请稍后重试。", Colors.Yellow));
				}
				catch
				{
				}
			}
			return false;
		}
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
		Logger.Log("LordEncounter", $"Entering native post-battle settlement flow. Reason={reason ?? "N/A"}, Target={target?.Name}");
		try
		{
			GameMenu.ActivateGameMenu("encounter");
			string text = null;
			try
			{
				text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
				text = null;
			}
			bool flag = text == "encounter";
			if (flag)
			{
				ClearPendingMeetingBattleVictorySettlement("enter_post_battle_settlement_" + (reason ?? "unknown"));
				return true;
			}
			Logger.Log("LordEncounter", "EnterPostBattleSettlement did not open native encounter menu. CurrentMenu=" + (text ?? "null"));
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "EnterPostBattleSettlement activate menu failed: " + ex.Message);
		}
		if (showFailureMessage)
		{
			try
			{
				InformationManager.DisplayMessage(new InformationMessage("进入战后结算失败，请稍后重试。", Colors.Yellow));
			}
			catch
			{
			}
		}
		return false;
	}

	private static bool TryEnsureEncounterContextForPostBattleSettlement(Hero target)
	{
		try
		{
			if (PlayerEncounter.Current != null)
			{
				return true;
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
		if (partyBase == null)
		{
			try
			{
				partyBase = _pendingMeetingBattleVictorySettlementEncounterParty;
			}
			catch
			{
				partyBase = null;
			}
		}
		if (partyBase == null)
		{
			Hero hero = null;
			try
			{
				hero = target ?? _pendingMeetingBattleVictorySettlementEncounterLeader;
			}
			catch
			{
				hero = target;
			}
			try
			{
				partyBase = hero?.PartyBelongedTo?.Party;
			}
			catch
			{
				partyBase = null;
			}
		}
		if (partyBase == null)
		{
			Hero hero2 = null;
			try
			{
				hero2 = target ?? _pendingMeetingBattleVictorySettlementEncounterLeader;
			}
			catch
			{
				hero2 = target;
			}
			if (hero2 != null)
			{
				try
				{
					foreach (MobileParty item in MobileParty.All)
					{
						if (item != null && item.Party != null)
						{
							Hero hero3 = null;
							try
							{
								hero3 = item.LeaderHero;
							}
							catch
							{
								hero3 = null;
							}
							if (hero3 == hero2)
							{
								partyBase = item.Party;
								break;
							}
						}
					}
				}
				catch
				{
					partyBase = null;
				}
			}
		}
		if (partyBase == null || PartyBase.MainParty == null)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement failed: defender/main party is null.");
			return false;
		}
		try
		{
			PlayerEncounter.RestartPlayerEncounter(partyBase, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
		}
		catch (Exception ex)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement: RestartPlayerEncounter failed: " + ex.Message);
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounter.Start();
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.SetupFields(PartyBase.MainParty, partyBase);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement: Start+SetupFields fallback failed: " + ex2.Message);
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement failed: PlayerEncounter.Current is still null.");
				return false;
			}
		}
		catch
		{
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
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement: StartBattle failed: " + ex3.Message);
		}
		try
		{
			BattleState winnerSide = ((!PlayerEncounter.PlayerIsAttacker) ? BattleState.DefenderVictory : BattleState.AttackerVictory);
			PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(winnerSide);
		}
		catch (Exception ex4)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement: set CampaignBattleResult failed: " + ex4.Message);
		}
		try
		{
			PlayerEncounter.SetPlayerVictorious();
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
		return true;
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("AnimusForge_lord_encounter", "{MENU_BODY_TEXT}", delegate(MenuCallbackArgs args)
		{
			Hero hero = EnsureEncounterTargetHero("menu_init");
			bool flag = HasPendingForceNativeDefeatCaptivityMenu();
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
			TextObject bodyText;
			if (flag)
			{
				args.MenuTitle = new TextObject("遭遇结果");
				bodyText = new TextObject("正在进入原版被俘结算。");
			}
			else if (TryBuildMeetingPostBattleSettlementText(hero, out bodyText))
			{
				args.MenuTitle = new TextObject("战后结算");
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
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "meet_lord", "与{TARGET_NAME}会面", delegate(MenuCallbackArgs args)
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
			{
				return false;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = EnsureEncounterTargetHero("menu_meet_condition");
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
			if (hero == null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。");
			}
			return true;
		}, delegate(MenuCallbackArgs args)
		{
			Hero hero = EnsureEncounterTargetHero("menu_meet_click");
			if (hero == null)
			{
				Logger.Log("LordEncounter", "Meet option clicked but target hero is null after refresh.");
				InformationManager.DisplayMessage(new InformationMessage("当前未识别到遭遇领主，请先离开并重新接触。", Colors.Yellow));
				return;
			}
			IsOpeningConversation = true;
			try
			{
				StartMeeting(hero, args);
			}
			finally
			{
				IsOpeningConversation = false;
			}
		});
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "native_dialogue_lord", "进入原版对话", delegate(MenuCallbackArgs args)
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
			{
				return false;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = EnsureEncounterTargetHero("menu_native_dialogue_condition");
			GameTexts.SetVariable("TARGET_NAME", (hero != null) ? hero.Name : new TextObject("领主"));
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
				InformationManager.DisplayMessage(new InformationMessage("当前未识别到遭遇领主，请先离开并重新接触。", Colors.Yellow));
				return;
			}
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
			TextObject bodyText;
			bool flag = TryBuildMeetingPostBattleSettlementText(hero, out bodyText);
			GameTexts.SetVariable("PRIMARY_ACTION_LABEL", flag ? new TextObject("进入战后结算") : new TextObject("攻击{TARGET_NAME}"));
			return true;
		}, delegate
		{
			Hero target = EnsureEncounterTargetHero("menu_attack_click");
			if (TryBuildMeetingPostBattleSettlementText(target, out var _))
			{
				EnterPostBattleSettlementFromMeetingMenu(target);
			}
			else
			{
				TryApplyImmediateAttackConsequencesForEncounter(target, "menu_attack_option");
				GameMenu.SwitchToMenu("encounter");
			}
		});
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "leave_lord", "离开", delegate
		{
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
			{
				return false;
			}
			return !IsHostileEncounterInitiatedByOpponent();
		}, delegate
		{
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

	internal static bool ApplyHostileEscalationDiplomaticConsequences(PartyBase defenderParty, Hero targetHero, string reason, string logChannel = "MeetingBattle")
	{
		bool flag = false;
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
		if (faction != null && faction2 != null && faction == faction2)
		{
			try
			{
				Clan clan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
				if (clan != null && clan.Kingdom != null)
				{
					if (clan.IsUnderMercenaryService)
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(clan);
						Logger.Log(logChannel, "Immediate escalation: player clan left kingdom as mercenary.");
					}
					else
					{
						ChangeKingdomAction.ApplyByLeaveKingdom(clan);
						Logger.Log(logChannel, "Immediate escalation: player clan left kingdom.");
					}
					flag = true;
				}
			}
			catch (Exception ex)
			{
				Logger.Log(logChannel, "Immediate escalation: leave kingdom failed: " + ex.Message);
			}
		}
		try
		{
			Hero hero = targetHero;
			if (hero == null)
			{
				hero = faction2?.Leader;
			}
			if (hero != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, -10);
				flag = true;
				Logger.Log(logChannel, $"Immediate escalation: relation penalty applied to {hero.Name}.");
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
			if (faction3 == faction2)
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
		try
		{
			MeetingBattleRuntime.RequestCombatEscalation(reason);
			MeetingBattleRuntime.UnlockDiplomaticSideEffects(reason);
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
		TryApplyImmediateEscalationConsequences(partyBase, target, reason ?? "menu_attack_option");
	}

	private static string GetMeetingTauntHeroKey(Hero hero)
	{
		return hero?.StringId?.Trim() ?? "";
	}

	private static bool HasMeetingTauntWarning(Hero hero)
	{
		string meetingTauntHeroKey = GetMeetingTauntHeroKey(hero);
		return !string.IsNullOrWhiteSpace(meetingTauntHeroKey) && _meetingTauntWarnedHeroIds != null && _meetingTauntWarnedHeroIds.Contains(meetingTauntHeroKey, StringComparer.OrdinalIgnoreCase);
	}

	private static void RememberMeetingTauntWarning(Hero hero)
	{
		string meetingTauntHeroKey = GetMeetingTauntHeroKey(hero);
		if (string.IsNullOrWhiteSpace(meetingTauntHeroKey))
		{
			return;
		}
		if (_meetingTauntWarnedHeroIds == null)
		{
			_meetingTauntWarnedHeroIds = new List<string>();
		}
		if (_meetingTauntWarnedHeroIds.Contains(meetingTauntHeroKey, StringComparer.OrdinalIgnoreCase))
		{
			return;
		}
		_meetingTauntWarnedHeroIds.Add(meetingTauntHeroKey);
		Logger.Log("MeetingTaunt", $"Recorded taunt warning state. Target={hero?.Name}, HeroId={meetingTauntHeroKey}");
	}

	private static bool IsMeetingTauntApplicable(Hero hero)
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

	private static bool TryEscalateMeetingTauntToBattle(Hero target, string reason)
	{
		try
		{
			Hero hero = target ?? EnsureEncounterTargetHero("meeting_taunt_battle");
			if (!IsMeetingTauntApplicable(hero))
			{
				Logger.Log("MeetingTaunt", "Battle tag ignored because current context is not a valid hero meeting.");
				return false;
			}
			TryApplyImmediateAttackConsequencesForEncounter(hero, reason ?? "meeting_taunt_battle");
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
			Logger.Log("MeetingTaunt", $"Battle escalation applied from taunt tag. Target={hero?.Name}, Reason={reason ?? "N/A"}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("MeetingTaunt", "Battle escalation from taunt tag failed: " + ex.Message);
			return false;
		}
	}

	internal static string BuildMeetingTauntRuntimeInstructionForExternal(Hero target)
	{
		try
		{
			bool flag = false;
			if (IsMeetingTauntApplicable(target))
			{
				flag = HasMeetingTauntWarning(target);
			}
			return BuildMeetingTauntFallbackInstruction(target, flag);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildMeetingTauntFallbackInstruction(Hero target, bool warned)
	{
		if (!IsMeetingTauntApplicable(target))
		{
			return "";
		}
		string text = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		if (warned)
		{
			return "你已警告过" + text + "。若还忍不了，就在句末输出[ACTION:MEETING_TAUNT_BATTLE]；这会把当前会面立刻升级为战斗，并按玩家攻击了你方军队来处理后果。";
		}
		return "若" + text + "挑衅你，可在句末输出[ACTION:MEETING_TAUNT_WARN]警告他；若忍无可忍，可直接输出[ACTION:MEETING_TAUNT_BATTLE]。这会把当前会面立刻升级为战斗，并按玩家攻击了你方军队来处理后果。";
	}

	internal static bool TryProcessMeetingTauntAction(Hero target, ref string content, out bool escalatedToBattle)
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
			Hero hero = target ?? EnsureEncounterTargetHero("meeting_taunt_action");
			if (flag && IsMeetingTauntApplicable(hero))
			{
				RememberMeetingTauntWarning(hero);
			}
			if (flag2)
			{
				escalatedToBattle = TryEscalateMeetingTauntToBattle(hero, "meeting_taunt_battle_tag");
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
			SetTarget(target);
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
				return;
			}
			DisableMeetingSpawnOverride();
			Logger.Log("LordEncounter", "Meeting requested: redirecting to native encounter attack consequence.");
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
		bool flag = PlayerEncounter.IsNavalEncounter();
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position);
		string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, flag);
		MissionInitializerRecord rec = new MissionInitializerRecord(battleSceneForMapPatch);
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
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
		bool flag2 = MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty p) => p.Party.IsMobile && (p.Party.MobileParty.IsCaravan || (p.Party.Owner != null && p.Party.Owner.IsMerchant)));
		bool flag3 = MapEvent.PlayerMapEvent.MapEventSettlement == null && MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty p) => p.Party.IsMobile && p.Party.MobileParty.IsVillager);
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
				string text3 = terrainTypeAtPosition switch
				{
					TerrainType.Plain => "平原", 
					TerrainType.Forest => "森林", 
					TerrainType.Mountain => "山地", 
					TerrainType.Snow => "雪原", 
					TerrainType.Desert => "沙漠", 
					TerrainType.Steppe => "草原", 
					TerrainType.Swamp => "沼泽", 
					TerrainType.Canyon => "峡谷", 
					TerrainType.Dune => "沙丘", 
					TerrainType.RuralArea => "乡野", 
					TerrainType.Beach => "海滩", 
					_ => terrainTypeAtPosition.ToString(), 
				};
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
