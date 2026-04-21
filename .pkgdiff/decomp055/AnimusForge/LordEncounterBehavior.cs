using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class LordEncounterBehavior : CampaignBehaviorBase
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<string, bool> _003C_003E9__58_0;

		public static Func<string, string> _003C_003E9__58_1;

		public static OnInitDelegate _003C_003E9__106_0;

		public static OnConditionDelegate _003C_003E9__106_1;

		public static OnConsequenceDelegate _003C_003E9__106_2;

		public static OnConditionDelegate _003C_003E9__106_3;

		public static OnConsequenceDelegate _003C_003E9__106_4;

		public static OnConditionDelegate _003C_003E9__106_5;

		public static OnConsequenceDelegate _003C_003E9__106_6;

		public static OnConditionDelegate _003C_003E9__106_7;

		public static OnConsequenceDelegate _003C_003E9__106_8;

		public static Func<MapEventParty, bool> _003C_003E9__122_0;

		public static Func<MapEventParty, bool> _003C_003E9__122_1;

		public static Func<Settlement, bool> _003C_003E9__141_1;

		internal bool _003CSyncData_003Eb__58_0(string x)
		{
			return !string.IsNullOrWhiteSpace(x);
		}

		internal string _003CSyncData_003Eb__58_1(string x)
		{
			return x.Trim();
		}

		internal void _003CAddGameMenus_003Eb__106_0(MenuCallbackArgs args)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Expected O, but got Unknown
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Expected O, but got Unknown
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Expected O, but got Unknown
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Expected O, but got Unknown
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Expected O, but got Unknown
			Hero val = EnsureEncounterTargetHero("menu_init");
			bool flag = HasPendingForceNativeDefeatCaptivityMenu();
			GameTexts.SetVariable("TARGET_NAME", (TextObject)((val != null) ? ((object)val.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
			TextObject bodyText;
			if (flag)
			{
				args.MenuTitle = new TextObject("遭遇结果", (Dictionary<string, object>)null);
				bodyText = new TextObject("正在进入原版被俘结算。", (Dictionary<string, object>)null);
			}
			else if (TryBuildMeetingPostBattleSettlementText(val, out bodyText))
			{
				args.MenuTitle = new TextObject("战后结算", (Dictionary<string, object>)null);
			}
			else
			{
				args.MenuTitle = new TextObject("遭遇领主", (Dictionary<string, object>)null);
				TextObject val2 = (IsHostileEncounterInitiatedByOpponent() ? new TextObject("对方试图向你发动进攻。", (Dictionary<string, object>)null) : new TextObject("", (Dictionary<string, object>)null));
				GameTexts.SetVariable("ENCOUNTER_INTENT", val2);
				bodyText = new TextObject("你在荒野中遇到了{TARGET_NAME}。{ENCOUNTER_INTENT}", (Dictionary<string, object>)null);
			}
			GameTexts.SetVariable("MENU_BODY_TEXT", bodyText);
			ApplyLordEncounterMenuBackground(args, val);
			FocusMapCameraOnMainParty();
		}

		internal bool _003CAddGameMenus_003Eb__106_1(MenuCallbackArgs args)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Expected O, but got Unknown
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
			{
				return false;
			}
			args.optionLeaveType = (LeaveType)22;
			Hero val = EnsureEncounterTargetHero("menu_meet_condition");
			GameTexts.SetVariable("TARGET_NAME", (TextObject)((val != null) ? ((object)val.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
			if (val == null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。", (Dictionary<string, object>)null);
			}
			return true;
		}

		internal void _003CAddGameMenus_003Eb__106_2(MenuCallbackArgs args)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			Hero val = EnsureEncounterTargetHero("menu_meet_click");
			if (val == null)
			{
				Logger.Log("LordEncounter", "Meet option clicked but target hero is null after refresh.");
				InformationManager.DisplayMessage(new InformationMessage("当前未识别到遭遇领主，请先离开并重新接触。", Colors.Yellow));
				return;
			}
			IsOpeningConversation = true;
			try
			{
				StartMeeting(val, args);
			}
			finally
			{
				IsOpeningConversation = false;
			}
		}

		internal bool _003CAddGameMenus_003Eb__106_3(MenuCallbackArgs args)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Expected O, but got Unknown
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
			{
				return false;
			}
			args.optionLeaveType = (LeaveType)22;
			Hero val = EnsureEncounterTargetHero("menu_native_dialogue_condition");
			GameTexts.SetVariable("TARGET_NAME", (TextObject)((val != null) ? ((object)val.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
			if (val == null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。", (Dictionary<string, object>)null);
			}
			return true;
		}

		internal void _003CAddGameMenus_003Eb__106_4(MenuCallbackArgs _003Cp0_003E)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			Hero val = EnsureEncounterTargetHero("menu_native_dialogue_click");
			if (val == null)
			{
				Logger.Log("LordEncounter", "Native dialogue option clicked but target hero is null after refresh.");
				InformationManager.DisplayMessage(new InformationMessage("当前未识别到遭遇领主，请先离开并重新接触。", Colors.Yellow));
			}
			else
			{
				OpenNativeEncounterConversation(val);
			}
		}

		internal bool _003CAddGameMenus_003Eb__106_5(MenuCallbackArgs _003Cp0_003E)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Expected O, but got Unknown
			if (HasPendingForceNativeDefeatCaptivityMenu())
			{
				return false;
			}
			Hero val = EnsureEncounterTargetHero("menu_attack_condition");
			GameTexts.SetVariable("TARGET_NAME", (TextObject)((val != null) ? ((object)val.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
			TextObject bodyText;
			bool flag = TryBuildMeetingPostBattleSettlementText(val, out bodyText);
			GameTexts.SetVariable("PRIMARY_ACTION_LABEL", flag ? new TextObject("进入战后结算", (Dictionary<string, object>)null) : new TextObject("攻击{TARGET_NAME}", (Dictionary<string, object>)null));
			return true;
		}

		internal void _003CAddGameMenus_003Eb__106_6(MenuCallbackArgs _003Cp0_003E)
		{
			Hero target = EnsureEncounterTargetHero("menu_attack_click");
			if (TryBuildMeetingPostBattleSettlementText(target, out var _))
			{
				EnterPostBattleSettlementFromMeetingMenu(target);
				return;
			}
			TryApplyImmediateAttackConsequencesForEncounter(target, "menu_attack_option");
			GameMenu.SwitchToMenu("encounter");
		}

		internal bool _003CAddGameMenus_003Eb__106_7(MenuCallbackArgs _003Cp0_003E)
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
		}

		internal void _003CAddGameMenus_003Eb__106_8(MenuCallbackArgs _003Cp0_003E)
		{
			PlayerEncounter.Finish(true);
		}

		internal bool _003COpenBattleMissionFallbackFromEncounter_003Eb__122_0(MapEventParty p)
		{
			return p.Party.IsMobile && (p.Party.MobileParty.IsCaravan || (p.Party.Owner != null && p.Party.Owner.IsMerchant));
		}

		internal bool _003COpenBattleMissionFallbackFromEncounter_003Eb__122_1(MapEventParty p)
		{
			return p.Party.IsMobile && p.Party.MobileParty.IsVillager;
		}

		internal bool _003CSaveMainPartyPosition_003Eb__141_1(Settlement s)
		{
			return s != null && !s.IsHideout;
		}
	}

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

	private static Vec3 _targetHeroSpawnPos = new Vec3(415.722f, 732.8734f, 1.918564f, -1f);

	private static Vec3 _targetHeroSpawnForward = new Vec3(0.9261521f, 0.3696325f, 0f, -1f);

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

	private static TextObject _lastMeetingPlayerFactionName = new TextObject("你的势力", (Dictionary<string, object>)null);

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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		pos = _savedMainPartyPosition;
		return _hasSavedMainPartyPosition && ((CampaignVec2)(ref _savedMainPartyPosition)).IsValid();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener((object)this, (Action<IMission>)OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener((object)this, (Action<IMission>)OnMissionEnded);
		CampaignEvents.TickEvent.AddNonSerializedListener((object)this, (Action<float>)OnCampaignTick);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, (Action<MenuCallbackArgs>)OnGameMenuOpened);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_meetingTauntWarnedHeroIds == null)
		{
			_meetingTauntWarnedHeroIds = new List<string>();
		}
		dataStore.SyncData<List<string>>("_meetingTauntWarnedHeroIds_v1", ref _meetingTauntWarnedHeroIds);
		if (_meetingTauntWarnedHeroIds == null)
		{
			_meetingTauntWarnedHeroIds = new List<string>();
			return;
		}
		_meetingTauntWarnedHeroIds = (from x in _meetingTauntWarnedHeroIds
			where !string.IsNullOrWhiteSpace(x)
			select x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
		AddConversationOptions(starter);
	}

	private void OnMissionEnded(IMission mission)
	{
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Invalid comparison between Unknown and I4
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
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
			flag2 = MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
			flag2 = false;
		}
		try
		{
			flag3 = _encounterMeetingMissionActive;
		}
		catch
		{
			flag3 = false;
		}
		try
		{
			flag4 = HasPendingMeetingBattleVictorySettlement();
		}
		catch
		{
			flag4 = false;
		}
		try
		{
			flag5 = HasPendingForceNativeEncounterBattleMenu();
		}
		catch
		{
			flag5 = false;
		}
		try
		{
			flag6 = HasPendingForceNativeDefeatCaptivityMenu();
		}
		catch
		{
			flag6 = false;
		}
		bool flag8 = false;
		bool flag9 = false;
		bool flag10 = false;
		try
		{
			Mission val = (Mission)(object)((mission is Mission) ? mission : null);
			flag8 = val != null && val.GetMissionBehavior<BattleEndLogic>() != null;
			flag7 = val != null && val.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null;
			if (val != null)
			{
				try
				{
					flag9 = val.MissionResult != null && val.MissionResult.PlayerDefeated;
				}
				catch
				{
					flag9 = false;
				}
				try
				{
					flag10 = val.MissionResult != null && val.MissionResult.PlayerVictory;
				}
				catch
				{
					flag10 = false;
				}
			}
		}
		catch
		{
			flag8 = false;
			flag9 = false;
			flag10 = false;
			flag7 = false;
		}
		if (!(flag2 || flag3 || flag7 || flag || flag4 || flag5 || flag6))
		{
			Logger.Log("MeetingBattle", $"OnMissionEnded ignored for non-meeting mission. missionWasBattle={flag8}, missionResultPlayerDefeated={flag9}, missionResultPlayerVictory={flag10}");
			return;
		}
		bool flag11 = false;
		try
		{
			flag11 = PlayerEncounter.CampaignBattleResult != null;
		}
		catch
		{
			flag11 = false;
		}
		bool flag12 = HasResolvedCampaignBattleResult();
		bool flag13 = false;
		bool flag14 = false;
		try
		{
			if (PlayerEncounter.Current != null)
			{
				try
				{
					flag13 = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null;
				}
				catch
				{
					flag13 = false;
				}
				try
				{
					PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
					flag14 = (int)encounterState != 0 && (int)encounterState != 1;
				}
				catch
				{
					flag14 = false;
				}
			}
		}
		catch
		{
			flag13 = false;
			flag14 = false;
		}
		bool flag15 = flag8 || flag || flag11 || flag12 || flag13 || flag14;
		if (flag15)
		{
			try
			{
				SuspendEncounterRedirectDuringResultResolution("mission_ended_after_meeting_battle");
			}
			catch
			{
			}
		}
		if (flag8 && flag)
		{
			try
			{
				PartyBase val2 = null;
				try
				{
					val2 = PlayerEncounter.EncounteredParty;
				}
				catch
				{
					val2 = null;
				}
				object obj16 = val2;
				if (obj16 == null)
				{
					Hero targetHero = _targetHero;
					if (targetHero == null)
					{
						obj16 = null;
					}
					else
					{
						MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
						obj16 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
					}
				}
				val2 = (PartyBase)obj16;
				TryApplyImmediateEscalationConsequences(val2, _targetHero, "meeting_battle_mission_end_fallback");
			}
			catch (Exception ex)
			{
				Logger.Log("MeetingBattle", "OnMissionEnded fallback escalation failed: " + ex.Message);
			}
		}
		bool flag16 = flag8 && !flag && !flag9;
		bool flag17 = flag8 && flag && !flag9 && !flag10 && !flag12;
		if (flag8 && flag9)
		{
			ClearPendingMeetingBattleVictorySettlement("mission_result_defeat");
			MarkPendingForceNativeDefeatCaptivityMenu("meeting_battle_mission_result_defeat");
			TryResolvePendingDefeatCaptivityImmediately("mission_ended_player_defeated");
		}
		else if (flag8 && flag10)
		{
			if (_lastMeetingWasSameMapFactionConflict)
			{
				ClearPendingMeetingBattleVictorySettlement("mission_result_victory_same_faction_meeting");
				Logger.Log("MeetingBattle", "Skipped legacy post-battle settlement flow because the meeting started with same-faction parties.");
			}
			else
			{
				MarkPendingMeetingBattleVictorySettlement("meeting_battle_mission_result_victory");
				TryResolvePendingMeetingBattleVictorySettlementImmediately("mission_ended_player_victory");
			}
		}
		else if (flag17)
		{
			MarkPendingForceNativeEncounterBattleMenu("meeting_battle_mission_exit_incomplete");
		}
		if (flag8 && !flag10)
		{
			DisableCustomEncounterMenuForCurrentEncounter("meeting_battle_mission_ended");
		}
		Logger.Log("MeetingBattle", $"OnMissionEnded: combatEscalated={flag}, missionWasBattle={flag8}, missionResultPlayerDefeated={flag9}, missionResultPlayerVictory={flag10}, hasBattleResult={flag11}, hasResolvedBattleResult={flag12}, hasEncounterBattleContext={flag13}, hasEncounterResolvingState={flag14}, nativeResultFlow={flag15}, peacefulCleanup={flag16}, forceNativeEncounterMenu={flag17}");
		MeetingBattleRuntime.EndMeeting();
		_pendingPostMissionCleanup = true;
		_pendingPostMissionCleanupDelay = 0f;
		_pendingPeacefulMeetingBattleCleanup = flag16;
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
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Invalid comparison between Unknown and I4
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
				PartyBase val = null;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				try
				{
					val = PlayerEncounter.EncounteredParty;
				}
				catch
				{
					val = null;
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
					flag5 = (int)encounterState != 0 && (int)encounterState != 1;
				}
				catch
				{
					flag5 = false;
				}
				if (_disableCustomEncounterMenuEncounterParty != null && val != null && val != _disableCustomEncounterMenuEncounterParty)
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
			Game current = Game.Current;
			object obj9;
			if (current == null)
			{
				obj9 = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj9 = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			flag6 = obj9 is MissionState;
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
			PartyBase pendingForceNativeDefeatCaptivityParty = _pendingForceNativeDefeatCaptivityParty;
			_pendingForceNativeDefeatCaptivityHero = ((pendingForceNativeDefeatCaptivityParty != null) ? pendingForceNativeDefeatCaptivityParty.LeaderHero : null) ?? _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
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
		string arg = reason ?? "N/A";
		Hero pendingForceNativeDefeatCaptivityHero = _pendingForceNativeDefeatCaptivityHero;
		TextObject arg2 = ((pendingForceNativeDefeatCaptivityHero != null) ? pendingForceNativeDefeatCaptivityHero.Name : null);
		PartyBase pendingForceNativeDefeatCaptivityParty2 = _pendingForceNativeDefeatCaptivityParty;
		Logger.Log("LordEncounter", $"Marked pending native defeat captivity menu redirect. Reason={arg}, CaptorHero={arg2}, CaptorParty={((pendingForceNativeDefeatCaptivityParty2 != null) ? pendingForceNativeDefeatCaptivityParty2.Name : null)}");
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
			PartyBase pendingForceNativeEncounterBattleMenuEncounterParty = _pendingForceNativeEncounterBattleMenuEncounterParty;
			_pendingForceNativeEncounterBattleMenuEncounterLeader = ((pendingForceNativeEncounterBattleMenuEncounterParty != null) ? pendingForceNativeEncounterBattleMenuEncounterParty.LeaderHero : null) ?? _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
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
		PartyBase val = null;
		try
		{
			val = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			val = null;
		}
		if (_pendingForceNativeEncounterBattleMenuEncounterParty != null && val != null && val != _pendingForceNativeEncounterBattleMenuEncounterParty)
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
			PartyBase pendingMeetingBattleVictorySettlementEncounterParty = _pendingMeetingBattleVictorySettlementEncounterParty;
			_pendingMeetingBattleVictorySettlementEncounterLeader = ((pendingMeetingBattleVictorySettlementEncounterParty != null) ? pendingMeetingBattleVictorySettlementEncounterParty.LeaderHero : null) ?? _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		catch
		{
			_pendingMeetingBattleVictorySettlementEncounterLeader = _targetHero ?? _encounterRedirectSuspendedEncounterLeader;
		}
		string arg = reason ?? "N/A";
		Hero pendingMeetingBattleVictorySettlementEncounterLeader = _pendingMeetingBattleVictorySettlementEncounterLeader;
		Logger.Log("LordEncounter", $"Marked pending meeting battle victory settlement. Reason={arg}, Target={((pendingMeetingBattleVictorySettlementEncounterLeader != null) ? pendingMeetingBattleVictorySettlementEncounterLeader.Name : null)}");
	}

	internal static bool HasPendingMeetingBattleVictorySettlement()
	{
		if (!_pendingMeetingBattleVictorySettlement)
		{
			return false;
		}
		if (_lastMeetingWasSameMapFactionConflict)
		{
			ClearPendingMeetingBattleVictorySettlement("blocked_same_faction_meeting_victory_flow");
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
		PartyBase val = null;
		Hero val2 = null;
		try
		{
			val = PlayerEncounter.EncounteredParty;
			val2 = ((val != null) ? val.LeaderHero : null);
		}
		catch
		{
			val = null;
			val2 = null;
		}
		if (_pendingMeetingBattleVictorySettlementEncounterParty != null && val != null && val != _pendingMeetingBattleVictorySettlementEncounterParty)
		{
			ClearPendingMeetingBattleVictorySettlement("encounter_party_changed");
			return false;
		}
		if (_pendingMeetingBattleVictorySettlementEncounterLeader != null && val2 != null && val2 != _pendingMeetingBattleVictorySettlementEncounterLeader)
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
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
			Campaign current2 = Campaign.Current;
			object obj3;
			if (current2 == null)
			{
				obj3 = null;
			}
			else
			{
				MenuContext currentMenuContext = current2.CurrentMenuContext;
				if (currentMenuContext == null)
				{
					obj3 = null;
				}
				else
				{
					GameMenu gameMenu = currentMenuContext.GameMenu;
					obj3 = ((gameMenu != null) ? gameMenu.StringId : null);
				}
			}
			text = (string)obj3;
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
			Game current3 = Game.Current;
			object obj5;
			if (current3 == null)
			{
				obj5 = null;
			}
			else
			{
				GameStateManager gameStateManager2 = current3.GameStateManager;
				obj5 = ((gameStateManager2 != null) ? gameStateManager2.ActiveState : null);
			}
			object obj6 = obj5;
			flag = obj6 != null && obj6.GetType().Name == "MapState";
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
			Hero val = null;
			try
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				val = ((encounteredParty != null) ? encounteredParty.LeaderHero : null);
			}
			catch
			{
				val = null;
			}
			if (val == null)
			{
				val = _pendingMeetingBattleVictorySettlementEncounterLeader;
			}
			if (val != null && val != Hero.MainHero && val.IsLord)
			{
				SetTarget(val);
			}
			if (TryResolvePendingMeetingBattleVictorySettlementImmediately("campaign_tick_pending_meeting_victory"))
			{
				return;
			}
			GameMenu.ActivateGameMenu("AnimusForge_lord_encounter");
			string text2 = null;
			try
			{
				Campaign current4 = Campaign.Current;
				object obj9;
				if (current4 == null)
				{
					obj9 = null;
				}
				else
				{
					MenuContext currentMenuContext2 = current4.CurrentMenuContext;
					if (currentMenuContext2 == null)
					{
						obj9 = null;
					}
					else
					{
						GameMenu gameMenu2 = currentMenuContext2.GameMenu;
						obj9 = ((gameMenu2 != null) ? gameMenu2.StringId : null);
					}
				}
				text2 = (string)obj9;
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
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
			Campaign current2 = Campaign.Current;
			object obj3;
			if (current2 == null)
			{
				obj3 = null;
			}
			else
			{
				MenuContext currentMenuContext = current2.CurrentMenuContext;
				if (currentMenuContext == null)
				{
					obj3 = null;
				}
				else
				{
					GameMenu gameMenu = currentMenuContext.GameMenu;
					obj3 = ((gameMenu != null) ? gameMenu.StringId : null);
				}
			}
			text = (string)obj3;
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
				Campaign current3 = Campaign.Current;
				object obj11;
				if (current3 == null)
				{
					obj11 = null;
				}
				else
				{
					MenuContext currentMenuContext2 = current3.CurrentMenuContext;
					if (currentMenuContext2 == null)
					{
						obj11 = null;
					}
					else
					{
						GameMenu gameMenu2 = currentMenuContext2.GameMenu;
						obj11 = ((gameMenu2 != null) ? gameMenu2.StringId : null);
					}
				}
				text2 = (string)obj11;
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
			PlayerEncounter val = null;
			try
			{
				val = PlayerEncounter.Current;
			}
			catch
			{
				val = null;
			}
			if (val == null)
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
			_playerEncounterDoPlayerDefeatMethod.Invoke(val, null);
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
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
			Campaign current2 = Campaign.Current;
			object obj3;
			if (current2 == null)
			{
				obj3 = null;
			}
			else
			{
				MenuContext currentMenuContext = current2.CurrentMenuContext;
				if (currentMenuContext == null)
				{
					obj3 = null;
				}
				else
				{
					GameMenu gameMenu = currentMenuContext.GameMenu;
					obj3 = ((gameMenu != null) ? gameMenu.StringId : null);
				}
			}
			text = (string)obj3;
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
			Game current3 = Game.Current;
			object obj6;
			if (current3 == null)
			{
				obj6 = null;
			}
			else
			{
				GameStateManager gameStateManager2 = current3.GameStateManager;
				obj6 = ((gameStateManager2 != null) ? gameStateManager2.ActiveState : null);
			}
			object obj7 = obj6;
			flag2 = obj7 != null && obj7.GetType().Name == "MapState";
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
					Campaign current4 = Campaign.Current;
					object obj10;
					if (current4 == null)
					{
						obj10 = null;
					}
					else
					{
						MenuContext currentMenuContext2 = current4.CurrentMenuContext;
						if (currentMenuContext2 == null)
						{
							obj10 = null;
						}
						else
						{
							GameMenu gameMenu2 = currentMenuContext2.GameMenu;
							obj10 = ((gameMenu2 != null) ? gameMenu2.StringId : null);
						}
					}
					text2 = (string)obj10;
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
			PartyBase val = ResolvePendingDefeatCaptivityParty();
			if (!flag && val != null)
			{
				try
				{
					TakePrisonerAction.Apply(val, Hero.MainHero);
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
				Campaign current5 = Campaign.Current;
				object obj13;
				if (current5 == null)
				{
					obj13 = null;
				}
				else
				{
					MenuContext currentMenuContext3 = current5.CurrentMenuContext;
					if (currentMenuContext3 == null)
					{
						obj13 = null;
					}
					else
					{
						GameMenu gameMenu3 = currentMenuContext3.GameMenu;
						obj13 = ((gameMenu3 != null) ? gameMenu3.StringId : null);
					}
				}
				text3 = (string)obj13;
			}
			catch
			{
				text3 = null;
			}
			flag3 = (text3 == "taken_prisoner" || text3 == "defeated_and_taken_prisoner") && flag;
			object[] obj15 = new object[4]
			{
				text3 == "taken_prisoner" || text3 == "defeated_and_taken_prisoner",
				flag,
				(val != null) ? val.Name : null,
				null
			};
			object obj16;
			if (val == null)
			{
				obj16 = null;
			}
			else
			{
				Hero leaderHero = val.LeaderHero;
				obj16 = ((leaderHero != null) ? leaderHero.Name : null);
			}
			obj15[3] = obj16;
			Logger.Log("LordEncounter", string.Format("Forced native captivity fallback attempted. Opened={0}, Prisoner={1}, Captor={2}, CaptorHero={3}", obj15));
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
		Hero val = null;
		try
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			val = ((encounteredParty != null) ? encounteredParty.LeaderHero : null);
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			val = _pendingMeetingBattleVictorySettlementEncounterLeader;
		}
		return TryEnterNativePostBattleSettlement(val, reason, showFailureMessage: false);
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
			if (!TryInvokeNativeDoPlayerDefeat())
			{
				return;
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
				Campaign current = Campaign.Current;
				object obj2;
				if (current == null)
				{
					obj2 = null;
				}
				else
				{
					MenuContext currentMenuContext = current.CurrentMenuContext;
					if (currentMenuContext == null)
					{
						obj2 = null;
					}
					else
					{
						GameMenu gameMenu = currentMenuContext.GameMenu;
						obj2 = ((gameMenu != null) ? gameMenu.StringId : null);
					}
				}
				text = (string)obj2;
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
		catch (Exception ex2)
		{
			Logger.Log("LordEncounter", "Immediate defeat captivity DoPlayerDefeat attempt failed: " + ex2.Message);
		}
	}

	private static PartyBase ResolvePendingDefeatCaptivityParty()
	{
		PartyBase val = null;
		try
		{
			val = _pendingForceNativeDefeatCaptivityParty;
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			try
			{
				Hero pendingForceNativeDefeatCaptivityHero = _pendingForceNativeDefeatCaptivityHero;
				object obj2;
				if (pendingForceNativeDefeatCaptivityHero == null)
				{
					obj2 = null;
				}
				else
				{
					MobileParty partyBelongedTo = pendingForceNativeDefeatCaptivityHero.PartyBelongedTo;
					obj2 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				val = (PartyBase)obj2;
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
				object obj5;
				if (targetHero == null)
				{
					obj5 = null;
				}
				else
				{
					MobileParty partyBelongedTo2 = targetHero.PartyBelongedTo;
					obj5 = ((partyBelongedTo2 != null) ? partyBelongedTo2.Party : null);
				}
				val = (PartyBase)obj5;
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
				Hero encounterRedirectSuspendedEncounterLeader = _encounterRedirectSuspendedEncounterLeader;
				object obj7;
				if (encounterRedirectSuspendedEncounterLeader == null)
				{
					obj7 = null;
				}
				else
				{
					MobileParty partyBelongedTo3 = encounterRedirectSuspendedEncounterLeader.PartyBelongedTo;
					obj7 = ((partyBelongedTo3 != null) ? partyBelongedTo3.Party : null);
				}
				val = (PartyBase)obj7;
			}
			catch
			{
				val = null;
			}
		}
		return val;
	}

	private static bool TryAdvancePendingDefeatCaptivityThroughNativeEncounter()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!TryEnsureEncounterContextForDefeatCaptivity(out var partyBase))
			{
				return false;
			}
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
			MapEvent val = TryGetCurrentEncounterBattle();
			if (val == null)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity aborted: battle context is null.");
				return false;
			}
			BattleSideEnum opponentSide = PartyBase.MainParty.OpponentSide;
			BattleState val2 = (BattleState)(((int)opponentSide != 1) ? 1 : 2);
			try
			{
				val.SetOverrideWinner(opponentSide);
			}
			catch (Exception ex)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity: SetOverrideWinner failed: " + ex.Message);
			}
			try
			{
				PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(val2, false);
			}
			catch (Exception ex2)
			{
				Logger.Log("LordEncounter", "Advance pending defeat captivity: set CampaignBattleResult failed: " + ex2.Message);
			}
			if (!TrySetPlayerEncounterState(PlayerEncounter.Current, (PlayerEncounterState)2))
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
				Campaign current = Campaign.Current;
				object obj3;
				if (current == null)
				{
					obj3 = null;
				}
				else
				{
					MenuContext currentMenuContext = current.CurrentMenuContext;
					if (currentMenuContext == null)
					{
						obj3 = null;
					}
					else
					{
						GameMenu gameMenu = currentMenuContext.GameMenu;
						obj3 = ((gameMenu != null) ? gameMenu.StringId : null);
					}
				}
				text = (string)obj3;
			}
			catch
			{
				text = null;
			}
			bool flag2 = text == "taken_prisoner" || text == "defeated_and_taken_prisoner";
			Logger.Log("LordEncounter", string.Format("Advanced pending defeat through native encounter flow. Menu={0}, Prisoner={1}, Captor={2}, PlayerWasAttacker={3}", text ?? "null", flag, (partyBase != null) ? partyBase.Name : null, _pendingForceNativeDefeatCaptivityPlayerWasAttacker));
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
		PartyBase val = (flag ? partyBase : PartyBase.MainParty);
		PartyBase val2 = (flag ? PartyBase.MainParty : partyBase);
		try
		{
			PlayerEncounter.RestartPlayerEncounter(val, val2, false);
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
					PlayerEncounter.Current.SetupFields(val2, val);
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
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
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
			if (!MeetingBattleRuntime.IsMeetingActive)
			{
				return;
			}
			Mission val = (Mission)(object)((mission is Mission) ? mission : null);
			if (val != null)
			{
				bool flag = false;
				try
				{
					flag = val.GetMissionBehavior<BattleEndLogic>() != null;
				}
				catch
				{
				}
				if (flag && val.GetMissionBehavior<MeetingBattleLockMissionBehavior>() == null)
				{
					Logger.Log("LordEncounter", "Attaching MeetingBattleLockMissionBehavior to native battle mission.");
					val.AddMissionBehavior((MissionBehavior)(object)new MeetingBattleLockMissionBehavior(MeetingBattleRuntime.TargetHero));
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
		if (!_pendingPostMissionCleanup || _pendingPostMissionCleanupDelay > 0f)
		{
			return;
		}
		Game current = Game.Current;
		object obj;
		if (current == null)
		{
			obj = null;
		}
		else
		{
			GameStateManager gameStateManager = current.GameStateManager;
			obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
		}
		if (obj is MissionState)
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
		}
		finally
		{
			_pendingPeacefulMeetingBattleCleanup = pendingPeacefulMeetingBattleCleanup;
		}
	}

	private void AddConversationOptions(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("AnimusForge_meet_talk", "lord_talk_ask_something_2", "lord_talk_ask_something_2", "Let's talk.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		starter.AddPlayerLine("AnimusForge_show_item", "lord_talk_ask_something_2", "AnimusForge_show_item_response", "I want to show you something.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		starter.AddDialogLine("AnimusForge_show_item_response", "AnimusForge_show_item_response", "lord_start", "Oh? What is it?", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		starter.AddPlayerLine("AnimusForge_give_item", "lord_talk_ask_something_2", "AnimusForge_give_item_response", "I have something for you.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		starter.AddDialogLine("AnimusForge_give_item_response", "AnimusForge_give_item_response", "lord_start", "Thank you, I will take a look.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
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
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				_encounterRedirectSuspendedEncounterLeader = ((encounteredParty != null) ? encounteredParty.LeaderHero : null) ?? _targetHero;
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
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Invalid comparison between Unknown and I4
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
			Hero val = null;
			PartyBase val2 = null;
			try
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				val = ((encounteredParty != null) ? encounteredParty.LeaderHero : null);
			}
			catch
			{
				val = null;
			}
			try
			{
				val2 = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				val2 = null;
			}
			if (_encounterRedirectSuspendedEncounterParty != null && val2 != null && val2 != _encounterRedirectSuspendedEncounterParty)
			{
				ClearEncounterRedirectSuspension("encounter_party_changed");
				return false;
			}
			if (_encounterRedirectSuspendedEncounterLeader != null && val != null && val != _encounterRedirectSuspendedEncounterLeader)
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
					Game current = Game.Current;
					object obj4;
					if (current == null)
					{
						obj4 = null;
					}
					else
					{
						GameStateManager gameStateManager = current.GameStateManager;
						obj4 = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
					}
					flag = obj4 is MissionState;
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
				Game current2 = Game.Current;
				object obj10;
				if (current2 == null)
				{
					obj10 = null;
				}
				else
				{
					GameStateManager gameStateManager2 = current2.GameStateManager;
					obj10 = ((gameStateManager2 != null) ? gameStateManager2.ActiveState : null);
				}
				flag6 = obj10 is MissionState;
			}
			catch
			{
				flag6 = false;
			}
			try
			{
				PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
				flag7 = (int)encounterState != 0 && (int)encounterState != 1;
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
			IFaction val = null;
			IFaction val2 = null;
			try
			{
				PartyBase mainParty = PartyBase.MainParty;
				val = ((mainParty != null) ? mainParty.MapFaction : null);
			}
			catch
			{
			}
			try
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				val2 = ((encounteredParty != null) ? encounteredParty.MapFaction : null);
			}
			catch
			{
			}
			if (val == null || val2 == null)
			{
				return true;
			}
			bool flag = false;
			try
			{
				flag = val.IsAtWarWith(val2) || val2.IsAtWarWith(val);
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
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			Hero val = ((encounteredParty != null) ? encounteredParty.LeaderHero : null);
			if (val != null && val != Hero.MainHero && val.IsLord)
			{
				return val;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Hero EnsureEncounterTargetHero(string reason)
	{
		Hero val = TryResolveEncounterLeaderHero();
		if (val != null)
		{
			if (_targetHero != val)
			{
				Logger.Log("LordEncounter", string.Format("Refreshed encounter target from active encounter. Reason={0}, Target={1}", reason ?? "N/A", val.Name));
			}
			_targetHero = val;
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
			MobileParty mainParty = MobileParty.MainParty;
			if (((mainParty != null) ? mainParty.Party : null) != null)
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
			object obj;
			if (args == null)
			{
				obj = null;
			}
			else
			{
				MenuContext menuContext = args.MenuContext;
				if (menuContext == null)
				{
					obj = null;
				}
				else
				{
					GameMenu gameMenu = menuContext.GameMenu;
					obj = ((gameMenu != null) ? gameMenu.StringId : null);
				}
			}
			text = (string)obj;
		}
		catch
		{
			text = null;
		}
		object obj3;
		if (args == null)
		{
			obj3 = null;
		}
		else
		{
			MenuContext menuContext2 = args.MenuContext;
			if (menuContext2 == null)
			{
				obj3 = null;
			}
			else
			{
				GameMenu gameMenu2 = menuContext2.GameMenu;
				obj3 = ((gameMenu2 != null) ? gameMenu2.StringId : null);
			}
		}
		if ((string)obj3 == "AnimusForge_lord_encounter")
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
		Campaign current = Campaign.Current;
		object obj2;
		if (current == null)
		{
			obj2 = null;
		}
		else
		{
			MenuContext currentMenuContext = current.CurrentMenuContext;
			if (currentMenuContext == null)
			{
				obj2 = null;
			}
			else
			{
				GameMenu gameMenu = currentMenuContext.GameMenu;
				obj2 = ((gameMenu != null) ? gameMenu.StringId : null);
			}
		}
		string text = (string)obj2;
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
			Game current = Game.Current;
			object obj3;
			if (current == null)
			{
				obj3 = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj3 = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			flag2 = obj3 is MissionState;
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
		if (((args != null) ? args.MenuContext : null) == null)
		{
			return;
		}
		try
		{
			string text = null;
			PartyBase val = null;
			MobileParty val2 = null;
			bool flag = false;
			try
			{
				val = PlayerEncounter.EncounteredParty;
			}
			catch
			{
			}
			try
			{
				val2 = ((val != null) ? val.MobileParty : null);
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
			if (val2 != null)
			{
				if (flag && (val2.IsVillager || val2.IsCaravan || ((val != null) ? val.MapFaction : null) == null))
				{
					text = "encounter_naval";
				}
				else if (val2.IsVillager)
				{
					text = "encounter_peasant";
				}
				else if (val2.IsCaravan)
				{
					text = "encounter_caravan";
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				CultureObject val3 = null;
				try
				{
					object obj4;
					if (val == null)
					{
						obj4 = null;
					}
					else
					{
						IFaction mapFaction = val.MapFaction;
						obj4 = ((mapFaction != null) ? mapFaction.Culture : null);
					}
					val3 = (CultureObject)obj4;
				}
				catch
				{
					val3 = null;
				}
				if (val3 == null)
				{
					try
					{
						object obj6;
						if (target == null)
						{
							obj6 = null;
						}
						else
						{
							IFaction mapFaction2 = target.MapFaction;
							obj6 = ((mapFaction2 != null) ? mapFaction2.Culture : null);
						}
						val3 = (CultureObject)obj6;
					}
					catch
					{
						val3 = null;
					}
				}
				if (val3 == null)
				{
					try
					{
						Hero mainHero = Hero.MainHero;
						object obj8;
						if (mainHero == null)
						{
							obj8 = null;
						}
						else
						{
							IFaction mapFaction3 = mainHero.MapFaction;
							obj8 = ((mapFaction3 != null) ? mapFaction3.Culture : null);
						}
						val3 = (CultureObject)obj8;
					}
					catch
					{
						val3 = null;
					}
				}
				if (val3 != null)
				{
					text = MenuHelper.GetEncounterCultureBackgroundMesh(val3);
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
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		bodyText = new TextObject("", (Dictionary<string, object>)null);
		if (_lastMeetingWasSameMapFactionConflict)
		{
			if (_pendingMeetingBattleVictorySettlement)
			{
				ClearPendingMeetingBattleVictorySettlement("suppress_same_faction_meeting_post_battle_text");
			}
			return false;
		}
		CampaignBattleResult val = null;
		try
		{
			val = PlayerEncounter.CampaignBattleResult;
		}
		catch
		{
			val = null;
		}
		bool flag = false;
		if (val != null)
		{
			try
			{
				flag = val.PlayerVictory;
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
		TextObject val2 = (TextObject)(((object)((target != null) ? target.Name : null)) ?? ((object)new TextObject("对方领主", (Dictionary<string, object>)null)));
		GameTexts.SetVariable("TARGET_NAME", val2);
		bodyText = new TextObject("你们在会面中产生了冲突，并且你将{TARGET_NAME}击败了，现在可以进入战后结算了。", (Dictionary<string, object>)null);
		return true;
	}

	private static void EnterPostBattleSettlementFromMeetingMenu(Hero target)
	{
		TryEnterNativePostBattleSettlement(target, "manual_enter_post_battle_settlement", showFailureMessage: true);
	}

	private static bool TryEnterNativePostBattleSettlement(Hero target, string reason, bool showFailureMessage)
	{
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
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
		Logger.Log("LordEncounter", string.Format("Entering native post-battle settlement flow. Reason={0}, Target={1}", reason ?? "N/A", (target != null) ? target.Name : null));
		try
		{
			GameMenu.ActivateGameMenu("encounter");
			string text = null;
			try
			{
				Campaign current = Campaign.Current;
				object obj5;
				if (current == null)
				{
					obj5 = null;
				}
				else
				{
					MenuContext currentMenuContext = current.CurrentMenuContext;
					if (currentMenuContext == null)
					{
						obj5 = null;
					}
					else
					{
						GameMenu gameMenu = currentMenuContext.GameMenu;
						obj5 = ((gameMenu != null) ? gameMenu.StringId : null);
					}
				}
				text = (string)obj5;
			}
			catch
			{
				text = null;
			}
			if (text == "encounter")
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
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
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
		PartyBase val = null;
		try
		{
			val = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			try
			{
				object obj3;
				if (target == null)
				{
					obj3 = null;
				}
				else
				{
					MobileParty partyBelongedTo = target.PartyBelongedTo;
					obj3 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				val = (PartyBase)obj3;
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
				val = _pendingMeetingBattleVictorySettlementEncounterParty;
			}
			catch
			{
				val = null;
			}
		}
		if (val == null)
		{
			Hero val2 = null;
			try
			{
				val2 = target ?? _pendingMeetingBattleVictorySettlementEncounterLeader;
			}
			catch
			{
				val2 = target;
			}
			try
			{
				object obj7;
				if (val2 == null)
				{
					obj7 = null;
				}
				else
				{
					MobileParty partyBelongedTo2 = val2.PartyBelongedTo;
					obj7 = ((partyBelongedTo2 != null) ? partyBelongedTo2.Party : null);
				}
				val = (PartyBase)obj7;
			}
			catch
			{
				val = null;
			}
		}
		if (val == null)
		{
			Hero val3 = null;
			try
			{
				val3 = target ?? _pendingMeetingBattleVictorySettlementEncounterLeader;
			}
			catch
			{
				val3 = target;
			}
			if (val3 != null)
			{
				try
				{
					foreach (MobileParty item in (List<MobileParty>)(object)MobileParty.All)
					{
						if (item != null && item.Party != null)
						{
							Hero val4 = null;
							try
							{
								val4 = item.LeaderHero;
							}
							catch
							{
								val4 = null;
							}
							if (val4 == val3)
							{
								val = item.Party;
								break;
							}
						}
					}
				}
				catch
				{
					val = null;
				}
			}
		}
		if (val == null || PartyBase.MainParty == null)
		{
			Logger.Log("LordEncounter", "TryEnsureEncounterContextForPostBattleSettlement failed: defender/main party is null.");
			return false;
		}
		try
		{
			PlayerEncounter.RestartPlayerEncounter(val, PartyBase.MainParty, false);
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
					PlayerEncounter.Current.SetupFields(PartyBase.MainParty, val);
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
			BattleState val5 = (BattleState)((!PlayerEncounter.PlayerIsAttacker) ? 1 : 2);
			PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(val5, false);
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
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		object obj = _003C_003Ec._003C_003E9__106_0;
		if (obj == null)
		{
			OnInitDelegate val = delegate(MenuCallbackArgs args)
			{
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				//IL_0040: Unknown result type (might be due to invalid IL or missing references)
				//IL_004a: Expected O, but got Unknown
				//IL_0050: Unknown result type (might be due to invalid IL or missing references)
				//IL_0056: Expected O, but got Unknown
				//IL_0084: Unknown result type (might be due to invalid IL or missing references)
				//IL_008e: Expected O, but got Unknown
				//IL_006f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0079: Expected O, but got Unknown
				//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
				//IL_009b: Unknown result type (might be due to invalid IL or missing references)
				//IL_00af: Expected O, but got Unknown
				//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c8: Expected O, but got Unknown
				Hero val10 = EnsureEncounterTargetHero("menu_init");
				bool flag = HasPendingForceNativeDefeatCaptivityMenu();
				GameTexts.SetVariable("TARGET_NAME", (TextObject)((val10 != null) ? ((object)val10.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
				TextObject bodyText;
				if (flag)
				{
					args.MenuTitle = new TextObject("遭遇结果", (Dictionary<string, object>)null);
					bodyText = new TextObject("正在进入原版被俘结算。", (Dictionary<string, object>)null);
				}
				else if (TryBuildMeetingPostBattleSettlementText(val10, out bodyText))
				{
					args.MenuTitle = new TextObject("战后结算", (Dictionary<string, object>)null);
				}
				else
				{
					args.MenuTitle = new TextObject("遭遇领主", (Dictionary<string, object>)null);
					TextObject val11 = (IsHostileEncounterInitiatedByOpponent() ? new TextObject("对方试图向你发动进攻。", (Dictionary<string, object>)null) : new TextObject("", (Dictionary<string, object>)null));
					GameTexts.SetVariable("ENCOUNTER_INTENT", val11);
					bodyText = new TextObject("你在荒野中遇到了{TARGET_NAME}。{ENCOUNTER_INTENT}", (Dictionary<string, object>)null);
				}
				GameTexts.SetVariable("MENU_BODY_TEXT", bodyText);
				ApplyLordEncounterMenuBackground(args, val10);
				FocusMapCameraOnMainParty();
			};
			_003C_003Ec._003C_003E9__106_0 = val;
			obj = (object)val;
		}
		starter.AddGameMenu("AnimusForge_lord_encounter", "{MENU_BODY_TEXT}", (OnInitDelegate)obj, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		object obj2 = _003C_003Ec._003C_003E9__106_1;
		if (obj2 == null)
		{
			OnConditionDelegate val2 = delegate(MenuCallbackArgs args)
			{
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				//IL_0071: Unknown result type (might be due to invalid IL or missing references)
				//IL_007b: Expected O, but got Unknown
				if (HasPendingForceNativeDefeatCaptivityMenu())
				{
					return false;
				}
				if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
				{
					return false;
				}
				args.optionLeaveType = (LeaveType)22;
				Hero val10 = EnsureEncounterTargetHero("menu_meet_condition");
				GameTexts.SetVariable("TARGET_NAME", (TextObject)((val10 != null) ? ((object)val10.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
				if (val10 == null)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。", (Dictionary<string, object>)null);
				}
				return true;
			};
			_003C_003Ec._003C_003E9__106_1 = val2;
			obj2 = (object)val2;
		}
		object obj3 = _003C_003Ec._003C_003E9__106_2;
		if (obj3 == null)
		{
			OnConsequenceDelegate val3 = delegate(MenuCallbackArgs args)
			{
				//IL_002a: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Expected O, but got Unknown
				Hero val10 = EnsureEncounterTargetHero("menu_meet_click");
				if (val10 == null)
				{
					Logger.Log("LordEncounter", "Meet option clicked but target hero is null after refresh.");
					InformationManager.DisplayMessage(new InformationMessage("当前未识别到遭遇领主，请先离开并重新接触。", Colors.Yellow));
					return;
				}
				IsOpeningConversation = true;
				try
				{
					StartMeeting(val10, args);
				}
				finally
				{
					IsOpeningConversation = false;
				}
			};
			_003C_003Ec._003C_003E9__106_2 = val3;
			obj3 = (object)val3;
		}
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "meet_lord", "与{TARGET_NAME}会面", (OnConditionDelegate)obj2, (OnConsequenceDelegate)obj3, false, -1, false, (object)null);
		object obj4 = _003C_003Ec._003C_003E9__106_3;
		if (obj4 == null)
		{
			OnConditionDelegate val4 = delegate(MenuCallbackArgs args)
			{
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				//IL_0071: Unknown result type (might be due to invalid IL or missing references)
				//IL_007b: Expected O, but got Unknown
				if (HasPendingForceNativeDefeatCaptivityMenu())
				{
					return false;
				}
				if (TryBuildMeetingPostBattleSettlementText(_targetHero, out var _))
				{
					return false;
				}
				args.optionLeaveType = (LeaveType)22;
				Hero val10 = EnsureEncounterTargetHero("menu_native_dialogue_condition");
				GameTexts.SetVariable("TARGET_NAME", (TextObject)((val10 != null) ? ((object)val10.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
				if (val10 == null)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("无法识别当前遭遇领主，请先离开后重新接触。", (Dictionary<string, object>)null);
				}
				return true;
			};
			_003C_003Ec._003C_003E9__106_3 = val4;
			obj4 = (object)val4;
		}
		object obj5 = _003C_003Ec._003C_003E9__106_4;
		if (obj5 == null)
		{
			OnConsequenceDelegate val5 = delegate
			{
				//IL_002a: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Expected O, but got Unknown
				Hero val10 = EnsureEncounterTargetHero("menu_native_dialogue_click");
				if (val10 == null)
				{
					Logger.Log("LordEncounter", "Native dialogue option clicked but target hero is null after refresh.");
					InformationManager.DisplayMessage(new InformationMessage("当前未识别到遭遇领主，请先离开并重新接触。", Colors.Yellow));
				}
				else
				{
					OpenNativeEncounterConversation(val10);
				}
			};
			_003C_003Ec._003C_003E9__106_4 = val5;
			obj5 = (object)val5;
		}
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "native_dialogue_lord", "进入原版对话", (OnConditionDelegate)obj4, (OnConsequenceDelegate)obj5, false, -1, false, (object)null);
		object obj6 = _003C_003Ec._003C_003E9__106_5;
		if (obj6 == null)
		{
			OnConditionDelegate val6 = delegate
			{
				//IL_0029: Unknown result type (might be due to invalid IL or missing references)
				//IL_0060: Unknown result type (might be due to invalid IL or missing references)
				//IL_0053: Unknown result type (might be due to invalid IL or missing references)
				//IL_006a: Expected O, but got Unknown
				if (HasPendingForceNativeDefeatCaptivityMenu())
				{
					return false;
				}
				Hero val10 = EnsureEncounterTargetHero("menu_attack_condition");
				GameTexts.SetVariable("TARGET_NAME", (TextObject)((val10 != null) ? ((object)val10.Name) : ((object)new TextObject("领主", (Dictionary<string, object>)null))));
				TextObject bodyText;
				bool flag = TryBuildMeetingPostBattleSettlementText(val10, out bodyText);
				GameTexts.SetVariable("PRIMARY_ACTION_LABEL", flag ? new TextObject("进入战后结算", (Dictionary<string, object>)null) : new TextObject("攻击{TARGET_NAME}", (Dictionary<string, object>)null));
				return true;
			};
			_003C_003Ec._003C_003E9__106_5 = val6;
			obj6 = (object)val6;
		}
		object obj7 = _003C_003Ec._003C_003E9__106_6;
		if (obj7 == null)
		{
			OnConsequenceDelegate val7 = delegate
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
			};
			_003C_003Ec._003C_003E9__106_6 = val7;
			obj7 = (object)val7;
		}
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "attack_lord", "{PRIMARY_ACTION_LABEL}", (OnConditionDelegate)obj6, (OnConsequenceDelegate)obj7, false, -1, false, (object)null);
		object obj8 = _003C_003Ec._003C_003E9__106_7;
		if (obj8 == null)
		{
			OnConditionDelegate val8 = delegate
			{
				if (HasPendingForceNativeDefeatCaptivityMenu())
				{
					return false;
				}
				TextObject bodyText;
				return !TryBuildMeetingPostBattleSettlementText(_targetHero, out bodyText) && !IsHostileEncounterInitiatedByOpponent();
			};
			_003C_003Ec._003C_003E9__106_7 = val8;
			obj8 = (object)val8;
		}
		object obj9 = _003C_003Ec._003C_003E9__106_8;
		if (obj9 == null)
		{
			OnConsequenceDelegate val9 = delegate
			{
				PlayerEncounter.Finish(true);
			};
			_003C_003Ec._003C_003E9__106_8 = val9;
			obj9 = (object)val9;
		}
		starter.AddGameMenuOption("AnimusForge_lord_encounter", "leave_lord", "离开", (OnConditionDelegate)obj8, (OnConsequenceDelegate)obj9, true, -1, false, (object)null);
	}

	internal static bool TryApplyImmediateEscalationConsequences(PartyBase defenderParty, Hero targetHero, string reason)
	{
		if (!MeetingBattleRuntime.TryMarkCombatEscalationConsequencesApplied())
		{
			Logger.Log("LordEncounter", "Immediate escalation consequences already applied or meeting inactive. Reason=" + (reason ?? "N/A"));
			return false;
		}
		return ApplyHostileEscalationDiplomaticConsequences(defenderParty, targetHero, reason);
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
				object obj2;
				if (targetHero == null)
				{
					obj2 = null;
				}
				else
				{
					MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
					obj2 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				defenderParty = (PartyBase)obj2;
			}
			catch
			{
				defenderParty = null;
			}
		}
		IFaction val = null;
		IFaction val2 = null;
		try
		{
			PartyBase mainParty = PartyBase.MainParty;
			val = ((mainParty != null) ? mainParty.MapFaction : null);
		}
		catch
		{
			val = null;
		}
		try
		{
			val2 = ((defenderParty != null) ? defenderParty.MapFaction : null) ?? ((targetHero != null) ? targetHero.MapFaction : null);
		}
		catch
		{
			val2 = null;
		}
		if (val != null && val2 != null && val == val2)
		{
			try
			{
				object obj6 = Clan.PlayerClan;
				if (obj6 == null)
				{
					Hero mainHero = Hero.MainHero;
					obj6 = ((mainHero != null) ? mainHero.Clan : null);
				}
				Clan val3 = (Clan)obj6;
				if (val3 != null && val3.Kingdom != null)
				{
					if (val3.IsUnderMercenaryService)
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(val3, true);
						Logger.Log(logChannel, "Immediate escalation: player clan left kingdom as mercenary.");
					}
					else
					{
						ChangeKingdomAction.ApplyByLeaveKingdom(val3, true);
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
			Hero val4 = targetHero;
			if (val4 == null)
			{
				val4 = ((val2 != null) ? val2.Leader : null);
			}
			if (val4 != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(val4, -10, true, true);
				flag = true;
				Logger.Log(logChannel, $"Immediate escalation: relation penalty applied to {val4.Name}.");
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
			IFaction val5 = null;
			try
			{
				PartyBase mainParty2 = PartyBase.MainParty;
				val5 = ((mainParty2 != null) ? mainParty2.MapFaction : null);
			}
			catch
			{
				val5 = null;
			}
			if (val5 == val2)
			{
				try
				{
					val5 = (IFaction)(object)Clan.PlayerClan;
				}
				catch
				{
				}
			}
			if (val5 != null && val2 != null && val5 != val2 && !FactionManager.IsAtWarAgainstFaction(val5, val2))
			{
				DeclareWarAction.ApplyByPlayerHostility(val5, val2);
				flag = true;
				Logger.Log(logChannel, $"Immediate escalation: declared war. Attacker={val5.Name}, Defender={val2.Name}");
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
		PartyBase val = null;
		try
		{
			val = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			try
			{
				object obj3;
				if (target == null)
				{
					obj3 = null;
				}
				else
				{
					MobileParty partyBelongedTo = target.PartyBelongedTo;
					obj3 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				val = (PartyBase)obj3;
			}
			catch
			{
				val = null;
			}
		}
		TryApplyImmediateEscalationConsequences(val, target, reason ?? "menu_attack_option");
	}

	private static string GetMeetingTauntHeroKey(Hero hero)
	{
		return ((hero == null) ? null : ((MBObjectBase)hero).StringId?.Trim()) ?? "";
	}

	private static bool HasMeetingTauntWarning(Hero hero)
	{
		string meetingTauntHeroKey = GetMeetingTauntHeroKey(hero);
		return !string.IsNullOrWhiteSpace(meetingTauntHeroKey) && _meetingTauntWarnedHeroIds != null && _meetingTauntWarnedHeroIds.Contains(meetingTauntHeroKey, StringComparer.OrdinalIgnoreCase);
	}

	private static void RememberMeetingTauntWarning(Hero hero)
	{
		string meetingTauntHeroKey = GetMeetingTauntHeroKey(hero);
		if (!string.IsNullOrWhiteSpace(meetingTauntHeroKey))
		{
			if (_meetingTauntWarnedHeroIds == null)
			{
				_meetingTauntWarnedHeroIds = new List<string>();
			}
			if (!_meetingTauntWarnedHeroIds.Contains(meetingTauntHeroKey, StringComparer.OrdinalIgnoreCase))
			{
				_meetingTauntWarnedHeroIds.Add(meetingTauntHeroKey);
				Logger.Log("MeetingTaunt", $"Recorded taunt warning state. Target={((hero != null) ? hero.Name : null)}, HeroId={meetingTauntHeroKey}");
			}
		}
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
			Hero val = MeetingBattleRuntime.TargetHero ?? _targetHero;
			if (val != null && val != hero)
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
			Hero val = target ?? EnsureEncounterTargetHero("meeting_taunt_battle");
			if (!IsMeetingTauntApplicable(val))
			{
				Logger.Log("MeetingTaunt", "Battle tag ignored because current context is not a valid hero meeting.");
				return false;
			}
			TryApplyImmediateAttackConsequencesForEncounter(val, reason ?? "meeting_taunt_battle");
			try
			{
				Campaign current = Campaign.Current;
				if (current != null)
				{
					ConversationManager conversationManager = current.ConversationManager;
					if (conversationManager != null)
					{
						conversationManager.EndConversation();
					}
				}
			}
			catch
			{
			}
			Logger.Log("MeetingTaunt", string.Format("Battle escalation applied from taunt tag. Target={0}, Reason={1}", (val != null) ? val.Name : null, reason ?? "N/A"));
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
			bool warned = false;
			if (IsMeetingTauntApplicable(target))
			{
				warned = HasMeetingTauntWarning(target);
			}
			return BuildMeetingTauntFallbackInstruction(target, warned);
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
			Hero val = target ?? EnsureEncounterTargetHero("meeting_taunt_action");
			if (flag && IsMeetingTauntApplicable(val))
			{
				RememberMeetingTauntWarning(val);
			}
			if (flag2)
			{
				escalatedToBattle = TryEscalateMeetingTauntToBattle(val, "meeting_taunt_battle_tag");
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
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
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
			_lastMeetingPlayerFactionName = new TextObject("你的势力", (Dictionary<string, object>)null);
			try
			{
				PartyBase mainParty = PartyBase.MainParty;
				IFaction val = ((mainParty != null) ? mainParty.MapFaction : null);
				IFaction val2 = ((target != null) ? target.MapFaction : null);
				_lastMeetingWasSameMapFactionConflict = val != null && val2 != null && val == val2;
				if (((val != null) ? val.Name : null) != (TextObject)null)
				{
					_lastMeetingPlayerFactionName = val.Name;
				}
			}
			catch
			{
			}
			MeetingBattleRuntime.BeginMeeting(target);
			Campaign.Current.CurrentConversationContext = (ConversationContext)3;
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
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
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
			Campaign.Current.CurrentConversationContext = (ConversationContext)3;
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
			PartyBase val = null;
			try
			{
				val = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				val = null;
			}
			object obj4 = val;
			if (obj4 == null)
			{
				MobileParty partyBelongedTo = target.PartyBelongedTo;
				obj4 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
			}
			val = (PartyBase)obj4;
			ConversationCharacterData val2 = default(ConversationCharacterData);
			((ConversationCharacterData)(ref val2))._002Ector(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false);
			ConversationCharacterData val3 = default(ConversationCharacterData);
			((ConversationCharacterData)(ref val3))._002Ector(target.CharacterObject, val, false, false, false, false, false, false);
			IsOpeningConversation = true;
			try
			{
				if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
				{
					CampaignMission.OpenConversationMission(val2, val3, "", "", false);
				}
				else
				{
					CampaignMapConversation.OpenConversation(val2, val3);
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
			MapEvent val = TryGetCurrentEncounterBattle();
			if (val != null)
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
				val = PlayerEncounter.StartBattle();
			}
			catch
			{
				val = null;
			}
			if (val == null)
			{
				PartyBase val2 = null;
				try
				{
					val2 = PlayerEncounter.EncounteredParty;
				}
				catch
				{
				}
				if (val2 == null)
				{
					try
					{
						object obj3;
						if (target == null)
						{
							obj3 = null;
						}
						else
						{
							MobileParty partyBelongedTo = target.PartyBelongedTo;
							obj3 = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
						}
						val2 = (PartyBase)obj3;
					}
					catch
					{
					}
				}
				if (val2 != null)
				{
					Logger.Log("LordEncounter", $"Fallback battle prep via StartBattleAction.Apply. Defender={val2.Name}");
					StartBattleAction.Apply(PartyBase.MainParty, val2);
				}
			}
			val = TryGetCurrentEncounterBattle();
			if (val != null)
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
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected I4, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		MapEvent val = TryGetCurrentEncounterBattle();
		if (val == null)
		{
			val = PlayerEncounter.StartBattle();
		}
		if (val == null)
		{
			throw new InvalidOperationException("Cannot fallback-open mission because battle is null.");
		}
		bool flag = PlayerEncounter.IsNavalEncounter();
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		CampaignVec2 position = MobileParty.MainParty.Position;
		MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(ref position);
		string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, flag);
		MissionInitializerRecord val2 = default(MissionInitializerRecord);
		((MissionInitializerRecord)(ref val2))._002Ector(battleSceneForMapPatch);
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		val2.TerrainType = (int)faceTerrainType;
		val2.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		val2.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		val2.NeedsRandomTerrain = false;
		val2.PlayingInCampaignMode = true;
		val2.RandomTerrainSeed = MBRandom.RandomInt(10000);
		val2.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		val2.SceneHasMapPatch = true;
		val2.DecalAtlasGroup = 2;
		val2.PatchCoordinates = mapPatchAtPosition.normalizedCoordinates;
		position = val.AttackerSide.LeaderParty.Position;
		Vec2 val3 = ((CampaignVec2)(ref position)).ToVec2();
		position = val.DefenderSide.LeaderParty.Position;
		Vec2 val4 = val3 - ((CampaignVec2)(ref position)).ToVec2();
		val2.PatchEncounterDir = ((Vec2)(ref val4)).Normalized();
		bool flag2 = ((IEnumerable<MapEventParty>)MapEvent.PlayerMapEvent.PartiesOnSide((BattleSideEnum)0)).Any((MapEventParty p) => p.Party.IsMobile && (p.Party.MobileParty.IsCaravan || (p.Party.Owner != null && p.Party.Owner.IsMerchant)));
		bool flag3 = MapEvent.PlayerMapEvent.MapEventSettlement == null && ((IEnumerable<MapEventParty>)MapEvent.PlayerMapEvent.PartiesOnSide((BattleSideEnum)0)).Any((MapEventParty p) => p.Party.IsMobile && p.Party.MobileParty.IsVillager);
		if (flag)
		{
			CampaignMission.OpenNavalBattleMission(val2);
		}
		else if (flag2 || flag3)
		{
			CampaignMission.OpenCaravanBattleMission(val2, flag2);
		}
		else
		{
			CampaignMission.OpenBattleMission(val2);
		}
		PlayerEncounter.StartAttackMission();
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		if (playerMapEvent != null)
		{
			playerMapEvent.BeginWait();
		}
	}

	private static Vec2 BuildMeetingPatchEncounterDirection(Hero target)
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		Vec2 result = default(Vec2);
		((Vec2)(ref result))._002Ector(1f, 0f);
		CampaignVec2 position;
		try
		{
			MapEvent val = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
			if (val != null)
			{
				MapEventSide attackerSide = val.AttackerSide;
				if (((attackerSide != null) ? attackerSide.LeaderParty : null) != null)
				{
					MapEventSide defenderSide = val.DefenderSide;
					if (((defenderSide != null) ? defenderSide.LeaderParty : null) != null)
					{
						position = val.AttackerSide.LeaderParty.Position;
						Vec2 val2 = ((CampaignVec2)(ref position)).ToVec2();
						position = val.DefenderSide.LeaderParty.Position;
						Vec2 val3 = ((CampaignVec2)(ref position)).ToVec2();
						Vec2 val4 = val2 - val3;
						if (((Vec2)(ref val4)).LengthSquared > 0.0001f)
						{
							return ((Vec2)(ref val4)).Normalized();
						}
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty != null && ((target != null) ? target.PartyBelongedTo : null) != null)
			{
				position = MobileParty.MainParty.Position;
				Vec2 val5 = ((CampaignVec2)(ref position)).ToVec2();
				position = target.PartyBelongedTo.Position;
				Vec2 val6 = val5 - ((CampaignVec2)(ref position)).ToVec2();
				if (((Vec2)(ref val6)).LengthSquared > 0.0001f)
				{
					result = ((Vec2)(ref val6)).Normalized();
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
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		center = Vec3.Zero;
		try
		{
			Mission current = Mission.Current;
			Scene val = ((current != null) ? current.Scene : null);
			if ((NativeObject)(object)val == (NativeObject)null)
			{
				return false;
			}
			if (TryGetBoundaryPolygonCenter(val, out var center2D))
			{
				center = new Vec3(center2D.x, center2D.y, 0f, -1f);
				ResolveSceneGroundHeight(val, ref center);
				return true;
			}
			Vec3 val2 = default(Vec3);
			Vec3 val3 = default(Vec3);
			val.GetBoundingBox(ref val2, ref val3);
			if (val2 == Vec3.Invalid || val3 == Vec3.Invalid)
			{
				val.GetSceneLimits(ref val2, ref val3);
			}
			center = new Vec3((val2.x + val3.x) * 0.5f, (val2.y + val3.y) * 0.5f, (val2.z + val3.z) * 0.5f, -1f);
			ResolveSceneGroundHeight(val, ref center);
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		polygon = new List<Vec2>();
		if ((NativeObject)(object)scene == (NativeObject)null)
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
			Vec2 val = polygon[0];
			Vec2 val2 = polygon[polygon.Count - 1];
			Vec2 val3 = val - val2;
			if (((Vec2)(ref val3)).LengthSquared < 0.0001f)
			{
				polygon.RemoveAt(polygon.Count - 1);
			}
		}
		return polygon.Count >= 3;
	}

	private static bool TryComputePolygonCentroid(List<Vec2> polygon, out Vec2 centroid)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
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
			Vec2 val = polygon[i];
			Vec2 val2 = polygon[(i + 1) % count];
			float num4 = val.x * val2.y - val2.x * val.y;
			num += num4;
			num2 += (val.x + val2.x) * num4;
			num3 += (val.y + val2.y) * num4;
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
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
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
			Vec2 val = polygon[num];
			Vec2 val2 = polygon[index];
			if (val.y > p.y != val2.y > p.y && p.x < (val2.x - val.x) * (p.y - val.y) / (val2.y - val.y + 1E-06f) + val.x)
			{
				flag = !flag;
			}
			index = num++;
		}
		return flag;
	}

	private static bool TryFindNearestInsidePoint(List<Vec2> polygon, Vec2 preferred, out Vec2 insidePoint)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
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
			Vec2 val = polygon[i];
			if (val.x < num)
			{
				num = val.x;
			}
			if (val.y < num2)
			{
				num2 = val.y;
			}
			if (val.x > num3)
			{
				num3 = val.x;
			}
			if (val.y > num4)
			{
				num4 = val.y;
			}
		}
		if (num3 - num < 0.01f || num4 - num2 < 0.01f)
		{
			return false;
		}
		bool flag = false;
		float num5 = float.MaxValue;
		int num6 = 18;
		Vec2 val2 = default(Vec2);
		for (int j = 0; j <= num6; j++)
		{
			float num7 = num + (num3 - num) * ((float)j / (float)num6);
			for (int k = 0; k <= num6; k++)
			{
				float num8 = num2 + (num4 - num2) * ((float)k / (float)num6);
				((Vec2)(ref val2))._002Ector(num7, num8);
				if (IsPointInsidePolygon(val2, polygon))
				{
					Vec2 val3 = val2 - preferred;
					float lengthSquared = ((Vec2)(ref val3)).LengthSquared;
					if (!flag || lengthSquared < num5)
					{
						flag = true;
						num5 = lengthSquared;
						insidePoint = val2;
					}
				}
			}
		}
		return flag;
	}

	internal static void ResolveSceneGroundHeight(Scene scene, ref Vec3 pos)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if ((NativeObject)(object)scene == (NativeObject)null)
		{
			return;
		}
		try
		{
			float z = pos.z;
			if (scene.GetHeightAtPoint(((Vec3)(ref pos)).AsVec2, (BodyFlags)544321929, ref z))
			{
				pos.z = z;
			}
			else
			{
				pos.z = scene.GetGroundHeightAtPosition(pos, (BodyFlags)544321929);
			}
		}
		catch
		{
		}
	}

	internal static void ClampPointInsideMissionBoundary(ref Vec3 candidate, Vec3 anchor)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Mission current = Mission.Current;
			Scene val = ((current != null) ? current.Scene : null);
			if ((NativeObject)(object)val == (NativeObject)null || !TryGetMissionBoundaryPolygon(val, out var polygon) || polygon.Count < 3)
			{
				return;
			}
			Vec2 asVec = ((Vec3)(ref candidate)).AsVec2;
			if (IsPointInsidePolygon(asVec, polygon))
			{
				return;
			}
			Vec2 asVec2 = ((Vec3)(ref anchor)).AsVec2;
			Vec2 val2 = asVec2;
			bool flag = false;
			for (int i = 1; i <= 25; i++)
			{
				float num = (float)i / 25f;
				Vec2 val3 = asVec + (asVec2 - asVec) * num;
				if (IsPointInsidePolygon(val3, polygon))
				{
					val2 = val3;
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
				val2 = insidePoint;
			}
			candidate.x = val2.x;
			candidate.y = val2.y;
			ResolveSceneGroundHeight(val, ref candidate);
		}
		catch
		{
		}
	}

	internal static MatrixFrame BuildPlayerSpawnFrame()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		MatrixFrame val = BuildTargetHeroSpawnFrame();
		Vec3 origin = val.origin;
		Vec3 f = val.rotation.f;
		f.z = 0f;
		if (((Vec3)(ref f)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref f))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref f)).Normalize();
		Vec3 val2 = default(Vec3);
		((Vec3)(ref val2))._002Ector(0f - f.y, f.x, 0f, -1f);
		if (((Vec3)(ref val2)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref val2))._002Ector(0f, 1f, 0f, -1f);
		}
		((Vec3)(ref val2)).Normalize();
		Vec3 candidate = origin + f * 12.4f - val2 * 0.7f;
		ClampPointInsideMissionBoundary(ref candidate, origin);
		try
		{
			Mission current = Mission.Current;
			Scene val3 = ((current != null) ? current.Scene : null);
			if ((NativeObject)(object)val3 != (NativeObject)null)
			{
				float z = candidate.z;
				if (val3.GetHeightAtPoint(((Vec3)(ref candidate)).AsVec2, (BodyFlags)544321929, ref z))
				{
					candidate.z = z;
				}
				else
				{
					candidate.z = val3.GetGroundHeightAtPosition(candidate, (BodyFlags)544321929);
				}
			}
		}
		catch
		{
		}
		Vec3 f2 = -f;
		f2.z = 0f;
		if (((Vec3)(ref f2)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref f2))._002Ector(-1f, 0f, 0f, -1f);
		}
		((Vec3)(ref f2)).Normalize();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = candidate;
		identity.rotation.f = f2;
		((Mat3)(ref identity.rotation)).OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return identity;
	}

	private static bool TryPrepareNextTargetHeroSpawnFrame()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_nextTargetHeroSpawnFrame = BuildTargetHeroSpawnFrame();
		_overrideNextTargetHeroSpawnFrame = true;
		return true;
	}

	internal static MatrixFrame BuildTargetHeroSpawnFrame()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		Vec3 origin = _targetHeroSpawnPos;
		Vec3 f = _targetHeroSpawnForward;
		if (TryGetMeetingSceneCenter(out var center))
		{
			origin = center;
			_targetHeroSpawnPos = center;
		}
		try
		{
			Vec2 val = BuildMeetingPatchEncounterDirection(_targetHero);
			if (((Vec2)(ref val)).LengthSquared > 0.0001f)
			{
				f = (_targetHeroSpawnForward = new Vec3(val.x, val.y, 0f, -1f));
			}
		}
		catch
		{
		}
		f.z = 0f;
		if (((Vec3)(ref f)).LengthSquared < 0.0001f)
		{
			((Vec3)(ref f))._002Ector(1f, 0f, 0f, -1f);
		}
		((Vec3)(ref f)).Normalize();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = origin;
		identity.rotation.f = f;
		((Mat3)(ref identity.rotation)).OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
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

	private unsafe static void SaveMainPartyPosition()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Expected I4, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Expected I4, but got Unknown
		if (MobileParty.MainParty == null)
		{
			return;
		}
		_savedMainPartyPosition = MobileParty.MainParty.Position;
		_hasSavedMainPartyPosition = ((CampaignVec2)(ref _savedMainPartyPosition)).IsValid();
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				string text = FormatSettlementNameWithType(Settlement.CurrentSettlement);
				if (string.IsNullOrEmpty(text))
				{
					text = ((object)Settlement.CurrentSettlement.Name).ToString();
				}
				_encounterMeetingLocationInfoOverride = "你位于 " + text + "。";
			}
			else
			{
				Settlement val = null;
				try
				{
					val = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, (NavigationType)3, (Func<Settlement, bool>)((Settlement s) => s != null && !s.IsHideout));
				}
				catch
				{
				}
				if (val != null)
				{
					string text2 = FormatSettlementNameWithType(val);
					if (string.IsNullOrEmpty(text2))
					{
						text2 = ((object)val.Name).ToString();
					}
					float num = 0f;
					bool flag = false;
					try
					{
						if (!_hasSavedMainPartyPosition || !((CampaignVec2)(ref _savedMainPartyPosition)).IsValid())
						{
							goto IL_0151;
						}
						CampaignVec2 val2 = val.GatePosition;
						if (!((CampaignVec2)(ref val2)).IsValid())
						{
							goto IL_0151;
						}
						val2 = val.GatePosition;
						num = MathF.Sqrt(((CampaignVec2)(ref val2)).DistanceSquared(_savedMainPartyPosition));
						flag = num > 0.001f;
						goto end_IL_00f9;
						IL_0151:
						if (MobileParty.MainParty != null)
						{
							val2 = MobileParty.MainParty.Position;
							if (((CampaignVec2)(ref val2)).IsValid())
							{
								val2 = val.GatePosition;
								if (((CampaignVec2)(ref val2)).IsValid())
								{
									val2 = val.GatePosition;
									num = MathF.Sqrt(((CampaignVec2)(ref val2)).DistanceSquared(MobileParty.MainParty.Position));
									flag = num > 0.001f;
								}
							}
						}
						end_IL_00f9:;
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
				TerrainType terrainTypeAtPosition = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(ref _savedMainPartyPosition);
				if (1 == 0)
				{
				}
				string text3 = (terrainTypeAtPosition - 1) switch
				{
					0 => "平原", 
					3 => "森林", 
					6 => "山地", 
					2 => "雪原", 
					1 => "沙漠", 
					4 => "草原", 
					14 => "沼泽", 
					12 => "峡谷", 
					15 => "沙丘", 
					13 => "乡野", 
					19 => "海滩", 
					_ => ((object)(*(TerrainType*)(&terrainTypeAtPosition))/*cast due to .constrained prefix*/).ToString(), 
				};
				if (1 == 0)
				{
				}
				string text4 = text3;
				string text5 = "";
				try
				{
					GameModels models = Campaign.Current.Models;
					MapWeatherModel val3 = ((models != null) ? models.MapWeatherModel : null);
					if (val3 != null)
					{
						WeatherEvent weatherEventInPosition = val3.GetWeatherEventInPosition(((CampaignVec2)(ref _savedMainPartyPosition)).ToVec2());
						if (1 == 0)
						{
						}
						text3 = (int)weatherEventInPosition switch
						{
							0 => "晴朗", 
							1 => "小雨", 
							2 => "大雨", 
							3 => "降雪", 
							4 => "暴风雪", 
							5 => "风暴", 
							_ => ((object)(*(WeatherEvent*)(&weatherEventInPosition))/*cast due to .constrained prefix*/).ToString(), 
						};
						if (1 == 0)
						{
						}
						text5 = text3;
					}
				}
				catch
				{
					text5 = "";
				}
				List<string> list = new List<string>();
				list.Add("地形：" + text4);
				if (!string.IsNullOrEmpty(text5))
				{
					list.Add("天气：" + text5);
				}
				if (list.Count <= 0)
				{
					return;
				}
				string text6 = string.Join("；", list).Trim();
				if (!string.IsNullOrEmpty(text6))
				{
					_encounterMeetingLocationInfoOverride = (_encounterMeetingLocationInfoOverride ?? "").Trim();
					if (!string.IsNullOrEmpty(_encounterMeetingLocationInfoOverride) && !_encounterMeetingLocationInfoOverride.EndsWith("。", StringComparison.Ordinal))
					{
						_encounterMeetingLocationInfoOverride += "。";
					}
					_encounterMeetingLocationInfoOverride = _encounterMeetingLocationInfoOverride + " " + text6 + "。";
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
			string text7 = (((object)st.Name)?.ToString() ?? "").Trim();
			if (string.IsNullOrEmpty(text7))
			{
				return "";
			}
			string text8 = (st.IsTown ? "城镇" : (st.IsCastle ? "城堡" : (st.IsVillage ? "村庄" : ((!st.IsFortification) ? "定居点" : "要塞"))));
			return text7 + "（" + text8 + "）";
		}
	}

	private static void RestoreMainPartyPosition()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
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
