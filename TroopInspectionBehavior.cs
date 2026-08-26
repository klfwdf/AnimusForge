using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public static partial class TroopInspectionBehavior
{
	private sealed class TroopInspectionRuntime
	{
		public TroopRoster InspectionRoster { get; set; }

		public TroopRoster InspectionPrisonerRoster { get; set; }

		public TroopRoster NotSelectedMemberRoster { get; set; }

		public TroopRoster NotSelectedPrisonerRoster { get; set; }

		public MobileParty HoldingDummyParty { get; set; }

		public MainPartyRoleSnapshot RoleSnapshot { get; set; }

		public string InspectionSummary { get; set; }

		public string NotSelectedSummary { get; set; }

		public bool RestoreCampaignEncounterAfterInspection { get; set; }

		public MapEventSide OriginalMainPartyMapEventSide { get; set; }

		public PlayerEncounter OriginalPlayerEncounter { get; set; }
	}

	private sealed class MainPartyRoleSnapshot
	{
		public Hero Scout { get; set; }

		public Hero Engineer { get; set; }

		public Hero Quartermaster { get; set; }

		public Hero Surgeon { get; set; }

		public bool Restored { get; set; }
	}

	private sealed class PendingSelection
	{
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

		public int HeroLikeSkipped;

		public int Errors;

		public int TotalMen => RegularMen + Heroes;

		public override string ToString()
		{
			return $"regular={RegularMen},heroes={Heroes},total={TotalMen},wounded={RegularWounded},xp={RegularXp},dead_heroes_skipped={DeadHeroesSkipped},hero_like_skipped={HeroLikeSkipped},errors={Errors}";
		}
	}


	private const string LogPrefix = "TroopInspection";

	private static readonly bool VerboseInspectionLogs = false;

	private const string DummyPartyPrefix = "animusforge_troop_inspection_dummy_";


	private const string SelectionPoolDummyPartyPrefix = "animusforge_troop_inspection_selection_pool_";

	private static TroopInspectionRuntime _runtime;

	private static PendingSelection _pendingSelection;

	private static MobileParty _dummyParty;

	private static MapEvent _mapEvent;

	private static string _dummyPartyStringId;

	private static bool _isOpening;

	private static bool _queuedOpenInspection;

	private static float _queuedOpenInspectionAt;

	private static bool _cleanupDone;

	private static Mission _activeInspectionMission;

	private static string _inspectionLogPath;

	private static bool _playerStateCaptured;

	private static int _playerOriginalHitPoints;

	private static bool _playerOriginalWasWounded;

	private static readonly FieldInfo TroopRosterTotalRegularsField = typeof(TroopRoster).GetField("_totalRegulars", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo TroopRosterTotalWoundedRegularsField = typeof(TroopRoster).GetField("_totalWoundedRegulars", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo TroopRosterTotalHeroesField = typeof(TroopRoster).GetField("_totalHeroes", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo TroopRosterTotalWoundedHeroesField = typeof(TroopRoster).GetField("_totalWoundedHeroes", BindingFlags.Instance | BindingFlags.NonPublic);


	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		TryPatchClass(harmony, typeof(TroopInspectionDeathRatePatch));
		TryPatchClass(harmony, typeof(TroopInspectionMeleeDamagePatch));
		TryPatchClass(harmony, typeof(TroopInspectionFormationIsolationPatch));
		TryPatchClass(harmony, typeof(TroopInspectionOrderOfBattlePatch));
		TryPatchClass(harmony, typeof(CastleAftermathBattleMusicUpdatePatch));
		ReinforcementSystemCompatibility.EnsurePatched(harmony);
	}

	private static void TryPatchClass(Harmony harmony, Type patchType)
	{
		try
		{
			harmony.CreateClassProcessor(patchType).Patch();
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> " + patchType.Name + " init failed: " + ex.Message);
		}
	}

	public static void OnEngineTick()
	{
		if (!_queuedOpenInspection)
		{
			return;
		}
		try
		{
			if (_runtime == null)
			{
				_queuedOpenInspection = false;
				return;
			}
			if ((float)Environment.TickCount / 1000f < _queuedOpenInspectionAt || IsPartyScreenStillActive() || Mission.Current != null)
			{
				return;
			}
			_queuedOpenInspection = false;
			_isOpening = true;
			EnsureMainHeroReadyForInspection("queued_open");
			if (!CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason))
			{
				Display(blockedReason);
				CleanupRuntime("queued_open_blocked");
				return;
			}
			OpenInspectionMissionAfterSelection(mainParty);
			Display("士兵检阅开始。");
		}
		catch (Exception ex)
		{
			Log("queued inspection_open failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			CleanupRuntime("queued_open_failed");
			Display("打开检阅士兵失败。");
		}
		finally
		{
			_isOpening = false;
		}
	}

	public static bool NeedsEngineTick()
	{
		return _queuedOpenInspection || _runtime != null || _isOpening;
	}

	public static void OpenInspectionFromTerminal()
	{
		Log("terminal_open");
		if (_isOpening)
		{
			Display("检阅正在准备中。");
			Log("precheck blocked: already opening");
			return;
		}
		if (_pendingSelection != null)
		{
			if (IsPartyScreenStillActive())
			{
				Display("检阅队伍选择界面已经打开。");
				Log("precheck blocked: selection screen active");
				return;
			}
			Log("selection abandoned: party screen no longer active");
			ResetPendingSelection("selection_screen_lost");
		}
		TryCleanupStaleInspectionStateBeforeOpen("terminal_open_stale_cleanup");
		EnsureMainHeroReadyForInspection("terminal_open");
		_isOpening = true;
		_cleanupDone = false;
		_activeInspectionMission = null;
		_runtime = null;
		try
		{
			if (!CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason))
			{
				Display(blockedReason);
				Log("precheck blocked: " + blockedReason);
				ResetPendingSelection("precheck_blocked");
				return;
			}
			int healthyInspectableTroops = CountHealthyNonPlayerTroops(PartyBase.MainParty.MemberRoster);
			Log($"precheck {BuildMainHeroInspectionStateSummary()} healthy_non_player={healthyInspectableTroops} mission_current={Mission.Current != null} player_encounter={PlayerEncounter.Current != null} player_mapevent={MapEvent.PlayerMapEvent != null}");
			if (Hero.MainHero?.IsWounded == true)
			{
				Display("你受伤了，无法检阅部队。");
				ResetPendingSelection("player_wounded");
				return;
			}
			if (healthyInspectableTroops <= 0)
			{
				Display("没有可检阅的健康士兵。");
				ResetPendingSelection("no_healthy_troops");
				return;
			}
			if (PlayerEncounter.Current != null || MapEvent.PlayerMapEvent != null || mainParty.MapEvent != null)
			{
				Display("当前遭遇状态无法检阅部队。");
				Log("precheck blocked: existing encounter or player map event");
				ResetPendingSelection("existing_encounter");
				return;
			}
			ReinforcementSystemCompatibility.EnsurePatched();
			OpenInspectionTeamSelection(mainParty);
		}
		catch (Exception ex)
		{
			Log("open failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			ResetPendingSelection("open_failed");
			Display("打开检阅士兵失败。");
		}
		finally
		{
			_isOpening = false;
		}
	}

	internal static bool IsCurrentInspectionRuntime(string dummyPartyStringId)
	{
		return !string.IsNullOrEmpty(dummyPartyStringId) && string.Equals(dummyPartyStringId, _dummyPartyStringId, StringComparison.Ordinal);
	}

	internal static bool IsPreparedExternalInspectionRuntime => _runtime?.RestoreCampaignEncounterAfterInspection == true;

	internal static string CurrentInspectionDummyPartyId => _dummyPartyStringId;

	internal static bool TryPrepareExternalInspectionRuntime(
		TroopRoster selectedMembers,
		TroopRoster selectedPrisoners,
		TroopRoster notSelectedMembers,
		TroopRoster notSelectedPrisoners,
		out string error)
	{
		error = "";
		if (Mission.Current != null || _runtime != null || _queuedOpenInspection || _isOpening)
		{
			error = "inspection_runtime_busy";
			return false;
		}

		try
		{
			_cleanupDone = false;
			_activeInspectionMission = null;
			_pendingSelection = null;
			EnsureMainHeroReadyForInspection("external_prepare");

			TroopRoster inspectionMembers = BuildSelectionRosterFromUi(selectedMembers);
			AddPlayerToInspectionRoster(inspectionMembers);
			TroopInspectionRuntime runtime = new TroopInspectionRuntime
			{
				InspectionRoster = CloneRoster(inspectionMembers),
				InspectionPrisonerRoster = BuildPrisonerSelectionRosterFromUi(selectedPrisoners),
				NotSelectedMemberRoster = BuildSelectionRosterFromUi(notSelectedMembers),
				NotSelectedPrisonerRoster = BuildPrisonerSelectionRosterFromUi(notSelectedPrisoners),
				RestoreCampaignEncounterAfterInspection = true
			};
			runtime.InspectionSummary = RosterSummary(runtime.InspectionRoster) + ", prisoners=" + RosterSummary(runtime.InspectionPrisonerRoster);
			runtime.NotSelectedSummary = RosterSummary(runtime.NotSelectedMemberRoster) + ", prisoners=" + RosterSummary(runtime.NotSelectedPrisonerRoster);
			_runtime = runtime;
			PrepareSelectionRuntimeWithMainPartySplit(runtime, reconcileExternalCastlePrisoners: true);
			Log("external_runtime_prepared inspection=" + runtime.InspectionSummary
				+ " not_selected=" + runtime.NotSelectedSummary);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.GetType().Name + ": " + ex.Message;
			Log("external_runtime_prepare failed: " + error);
			CleanupRuntime("external_prepare_failed");
			return false;
		}
	}

	internal static bool TryOpenPreparedExternalInspectionMission(
		MissionInitializerRecord initializer,
		out Mission mission,
		out string error)
	{
		mission = null;
		error = "";
		if (!IsPreparedExternalInspectionRuntime || MobileParty.MainParty == null)
		{
			error = "external_inspection_runtime_not_prepared";
			return false;
		}

		try
		{
			_isOpening = true;
			EnsureMainHeroReadyForInspection("external_open");
			PrepareRuntime(MobileParty.MainParty);
			IMission openedMission = CampaignMission.OpenBattleMission(initializer);
			mission = openedMission as Mission;
			if (mission == null)
			{
				throw new InvalidOperationException("CampaignMission.OpenBattleMission returned non-Mission.");
			}
			_activeInspectionMission = mission;
			PlayerEncounter.StartAttackMission();
			MapEvent.PlayerMapEvent?.BeginWait();
			LogMissionSourceDiag("after_open_external_battle");
			Log("external_mission_opened scene=" + initializer.SceneName
				+ " levels=" + initializer.SceneLevels
				+ " mode=" + mission.Mode);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.GetType().Name + ": " + ex.Message;
			Log("external_mission_open failed: " + error + "\n" + ex.StackTrace);
			CleanupRuntime("external_open_failed");
			mission = null;
			return false;
		}
		finally
		{
			_isOpening = false;
		}
	}

	internal static void CancelPreparedExternalInspectionRuntime(string reason)
	{
		if (IsPreparedExternalInspectionRuntime && Mission.Current == null)
		{
			CleanupRuntime(reason ?? "external_cancel");
		}
	}

	internal static bool ShouldSuppressReinforcementSystem(Mission mission)
	{
		if (mission == null)
		{
			return false;
		}
		if (_isOpening || object.ReferenceEquals(mission, _activeInspectionMission))
		{
			return true;
		}
		try
		{
			return mission.GetMissionBehavior<TroopInspectionMissionLogic>() != null;
		}
		catch
		{
			return false;
		}
	}

	internal static bool CanOfferPrisonerSlaughterActionForExternal(
		int speakerAgentIndex,
		out int regularPrisonerCount,
		out int attackerCount)
	{
		regularPrisonerCount = 0;
		attackerCount = 0;
		try
		{
			TroopInspectionMissionLogic logic = Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>();
			return logic != null
				&& logic.CanOfferPrisonerSlaughterAction(
					speakerAgentIndex,
					out regularPrisonerCount,
					out attackerCount);
		}
		catch (Exception ex)
		{
			Log("inspection_slaughter_offer failed agent=" + speakerAgentIndex + " error="
				+ ex.GetType().Name + ": " + ex.Message);
			return false;
		}
	}

	internal static string BuildPrisonerSlaughterPromptInstructionForExternal(int speakerAgentIndex)
	{
		return CanOfferPrisonerSlaughterActionForExternal(
			speakerAgentIndex,
			out int regularPrisonerCount,
			out int attackerCount)
			? TroopInspectionPrisonerSlaughterProfile.BuildRuntimeInstruction(
				regularPrisonerCount,
				attackerCount)
			: string.Empty;
	}

	internal static string BuildPrisonerSlaughterPostprocessRuleForExternal(int speakerAgentIndex)
	{
		return CanOfferPrisonerSlaughterActionForExternal(
			speakerAgentIndex,
			out int regularPrisonerCount,
			out int attackerCount)
			? TroopInspectionPrisonerSlaughterProfile.BuildPostprocessRuleDescription(
				regularPrisonerCount,
				attackerCount)
			: string.Empty;
	}

	internal static bool TryProcessPrisonerSlaughterActionTagForExternal(
		int speakerAgentIndex,
		ref string content)
	{
		string actionTag = TroopInspectionPrisonerSlaughterProfile.ActionTag;
		if (string.IsNullOrWhiteSpace(content)
			|| content.IndexOf(actionTag, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}

		content = System.Text.RegularExpressions.Regex.Replace(
			content ?? string.Empty,
			System.Text.RegularExpressions.Regex.Escape(actionTag),
			string.Empty,
			System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
		try
		{
			TroopInspectionMissionLogic logic = Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>();
			int attackerCount = 0;
			int targetCount = 0;
			string reason = "not_normal_inspection";
			if (logic == null
				|| !logic.TryStartPrisonerSlaughter(
					speakerAgentIndex,
					out attackerCount,
					out targetCount,
					out reason))
			{
				string blockedReason = string.IsNullOrWhiteSpace(reason)
					? "not_normal_inspection"
					: reason;
				InformationManager.DisplayMessage(new InformationMessage(
					TroopInspectionPrisonerSlaughterProfile.BuildUnavailableMessage(blockedReason),
					Color.FromUint(TroopInspectionPrisonerSlaughterProfile.WarningMessageColor)));
				Log("inspection_slaughter_tag blocked agent=" + speakerAgentIndex
					+ " reason=" + blockedReason);
				return false;
			}

			InformationManager.DisplayMessage(new InformationMessage(
				TroopInspectionPrisonerSlaughterProfile.BuildStartedMessage(
					attackerCount,
					targetCount),
				Color.FromUint(TroopInspectionPrisonerSlaughterProfile.StartMessageColor)));
			Log("inspection_slaughter_tag started agent=" + speakerAgentIndex
				+ " attackers=" + attackerCount
				+ " targets=" + targetCount);
			return true;
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(
				TroopInspectionPrisonerSlaughterProfile.BuildUnavailableMessage("exception"),
				Color.FromUint(TroopInspectionPrisonerSlaughterProfile.WarningMessageColor)));
			Log("inspection_slaughter_tag failed agent=" + speakerAgentIndex + " error=" + ex);
			return false;
		}
	}

	internal static void CleanupRuntime(string reason)
	{
		bool alreadyDone = _cleanupDone;
		_cleanupDone = true;
		Log("cleanup begin reason=" + reason + " already_done=" + alreadyDone);
		_activeInspectionMission = null;
		TroopInspectionRuntime runtime = _runtime;
		MapEvent mapEvent = ResolveInspectionMapEvent();
		MobileParty dummyParty = _dummyParty;
		string dummyId = _dummyPartyStringId;
		MobileParty holdingParty = runtime?.HoldingDummyParty;
		try
		{
			RestoreAndDestroyHoldingDummyParty(holdingParty, "inspection_holding_cleanup");
			CleanupOrphanHoldingDummyParties(holdingParty, "inspection_holding_orphan_cleanup");
		}
		catch (Exception ex)
		{
			Log("cleanup holding_party failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		RestoreMainPartyRolesFromSnapshot(runtime, reason);
		CleanupOrphanSelectionPoolDummyParties("cleanup_selection_pool_orphan");
		_mapEvent = null;
		_dummyParty = null;
		_dummyPartyStringId = null;
		if (runtime != null)
		{
			runtime.HoldingDummyParty = null;
		}
		_pendingSelection = null;
		_queuedOpenInspection = false;
		_runtime = null;
		CleanupMapEventAndPlayerEncounter(mapEvent, reason);
		DestroyInspectionDummyParty(dummyParty, dummyId, "inspection_dummy_cleanup");
		CleanupOrphanInspectionDummyParties(dummyParty, "inspection_dummy_orphan_cleanup");
		RestoreExternalCampaignEncounter(runtime, reason);
		RestoreMainHeroAfterInspection(reason);
		Log("cleanup end reason=" + reason);
	}

	

	private static void TryCleanupStaleInspectionStateBeforeOpen(string reason)
	{
		try
		{
			if (Mission.Current != null)
			{
				return;
			}
			MapEvent encounterMapEvent = null;
			if (PlayerEncounter.Current != null)
			{
				encounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
			}
			bool hasStaleState = _runtime != null || _pendingSelection != null || _queuedOpenInspection || _isOpening || _activeInspectionMission != null || _dummyParty != null || _mapEvent != null;
			bool hasInspectionEncounter = IsInspectionMapEvent(encounterMapEvent) || IsInspectionMapEvent(MapEvent.PlayerMapEvent) || IsInspectionMapEvent(MobileParty.MainParty?.MapEvent);
			bool hasInspectionDummy = HasActiveInspectionDummyParty();
			bool hasEmptyStaleEncounter = _cleanupDone && PlayerEncounter.Current != null && encounterMapEvent == null && MapEvent.PlayerMapEvent == null && MobileParty.MainParty?.MapEvent == null;
			if (hasStaleState || hasInspectionEncounter || hasInspectionDummy || hasEmptyStaleEncounter)
			{
				Log("stale cleanup before open reason=" + reason + " stale_state=" + hasStaleState + " inspection_encounter=" + hasInspectionEncounter + " inspection_dummy=" + hasInspectionDummy + " empty_stale_encounter=" + hasEmptyStaleEncounter);
				CleanupRuntime(reason);
			}
		}
		catch (Exception ex)
		{
			Log("stale cleanup before open failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static MapEvent ResolveInspectionMapEvent()
	{
		try
		{
			if (_mapEvent != null)
			{
				return _mapEvent;
			}
			if (_dummyParty?.MapEvent != null)
			{
				return _dummyParty.MapEvent;
			}
			if (PlayerEncounter.Current != null)
			{
				MapEvent encounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
				if (IsInspectionMapEvent(encounterMapEvent))
				{
					return encounterMapEvent;
				}
			}
			if (IsInspectionMapEvent(MapEvent.PlayerMapEvent))
			{
				return MapEvent.PlayerMapEvent;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (IsInspectionMapEvent(mainParty?.MapEvent))
			{
				return mainParty.MapEvent;
			}
			foreach (MobileParty party in MobileParty.All)
			{
				if (party != null && party.IsActive && IsInspectionDummyParty(party) && party.MapEvent != null)
				{
					return party.MapEvent;
				}
			}
		}
		catch (Exception ex)
		{
			Log("resolve inspection mapevent failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		return null;
	}

	private static void CleanupMapEventAndPlayerEncounter(MapEvent mapEvent, string reason)
	{
		try
		{
			if (mapEvent != null)
			{
				Log($"cleanup_mapevent reason={reason} state={mapEvent.State} battle_state={mapEvent.BattleState} finalized={mapEvent.IsFinalized} has_winner={mapEvent.HasWinner}");
				if (!mapEvent.IsFinalized)
				{
					mapEvent.ResetBattleState();
					mapEvent.FinalizeEvent();
					Log("cleanup map_event_finalized reason=" + reason);
				}
			}
		}
		catch (Exception ex)
		{
			Log("cleanup map_event failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		try
		{
			if (PlayerEncounter.Current != null)
			{
				MapEvent currentEncounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
				bool clearEmptyStaleEncounter = mapEvent == null && currentEncounterMapEvent == null && MapEvent.PlayerMapEvent == null && MobileParty.MainParty?.MapEvent == null && _cleanupDone;
				if ((mapEvent != null && currentEncounterMapEvent == mapEvent) || IsInspectionMapEvent(currentEncounterMapEvent) || clearEmptyStaleEncounter)
				{
					SetPrivateField<object>(PlayerEncounter.Current, "_campaignBattleResult", null);
					SetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent", null);
					ClearPlayerEncounterProperty();
					Log("cleanup player_encounter_context_cleared reason=" + reason + " empty_stale=" + clearEmptyStaleEncounter);
				}
			}
		}
		catch (Exception ex)
		{
			Log("cleanup player_encounter failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void DestroyInspectionDummyParty(MobileParty dummyParty, string expectedStringId, string label)
	{
		try
		{
			if (dummyParty == null)
			{
				return;
			}
			string id = dummyParty.StringId ?? "";
			if (dummyParty.IsActive && id.StartsWith(DummyPartyPrefix, StringComparison.Ordinal) && (string.IsNullOrEmpty(expectedStringId) || string.Equals(id, expectedStringId, StringComparison.Ordinal)))
			{
				DestroyPartyAction.Apply(null, dummyParty);
				Log("cleanup dummy_party_destroyed label=" + label + " id=" + id);
			}
		}
		catch (Exception ex)
		{
			Log("cleanup dummy_party failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupOrphanInspectionDummyParties(MobileParty exceptParty, string label)
	{
		try
		{
			List<MobileParty> parties = new List<MobileParty>();
			foreach (MobileParty party in MobileParty.All)
			{
				if (party != null && !object.ReferenceEquals(party, exceptParty) && party.IsActive && IsInspectionDummyParty(party))
				{
					parties.Add(party);
				}
			}
			foreach (MobileParty party in parties)
			{
				DestroyInspectionDummyParty(party, null, label);
			}
		}
		catch (Exception ex)
		{
			Log("cleanup orphan dummy failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool HasActiveInspectionDummyParty()
	{
		try
		{
			foreach (MobileParty party in MobileParty.All)
			{
				if (party != null && party.IsActive && (IsInspectionDummyParty(party) || IsInspectionHoldingDummyParty(party) || IsInspectionSelectionPoolDummyParty(party)))
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

	private static bool IsInspectionMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		if (object.ReferenceEquals(mapEvent, _mapEvent))
		{
			return true;
		}
		try
		{
			return MapEventSideHasInspectionDummy(mapEvent.AttackerSide) || MapEventSideHasInspectionDummy(mapEvent.DefenderSide);
		}
		catch
		{
			return false;
		}
	}

	private static bool MapEventSideHasInspectionDummy(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				MobileParty mobileParty = party?.Party?.MobileParty;
				if (IsInspectionDummyParty(mobileParty))
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

	private static bool IsInspectionDummyParty(MobileParty party)
	{
		try
		{
			return ((party != null) ? party.StringId : null)?.StartsWith(DummyPartyPrefix, StringComparison.Ordinal) == true;
		}
		catch
		{
			return false;
		}
	}


	private static bool IsInspectionSelectionPoolDummyParty(MobileParty party)
	{
		try
		{
			return party?.StringId?.StartsWith(SelectionPoolDummyPartyPrefix, StringComparison.Ordinal) == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason)
	{
		mainParty = MobileParty.MainParty;
		blockedReason = "";
		try
		{
			if (Campaign.Current == null || mainParty == null || PartyBase.MainParty == null)
			{
				blockedReason = "当前状态无法检阅部队。";
				return false;
			}
			if (Mission.Current != null)
			{
				blockedReason = "当前任务中无法检阅部队。";
				return false;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				blockedReason = "当前对话中无法检阅部队。";
				return false;
			}
			if (Settlement.CurrentSettlement != null || mainParty.CurrentSettlement != null)
			{
				blockedReason = "当前地点无法检阅部队。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("precheck exception: " + ex.GetType().Name + ": " + ex.Message);
			blockedReason = "当前状态无法检阅部队。";
			return false;
		}
	}

	private static int CountHealthyNonPlayerTroops(TroopRoster roster)
	{
		int count = 0;
		if (roster == null)
		{
			return 0;
		}
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter)
			{
				continue;
			}
			count += Math.Max(0, item.Number - item.WoundedNumber);
		}
		return count;
	}


	private static void OpenInspectionTeamSelection(MobileParty mainParty)
	{
		PrepareMainPartyRosterStateForInspection("before_selection");
		TroopRoster selectableMembers = BuildSelectableRoster(mainParty.MemberRoster);
		TroopRoster selectablePrisoners = BuildSelectablePrisonerRoster(MobileParty.MainParty?.PrisonRoster ?? PartyBase.MainParty?.PrisonRoster);
		TroopRoster inspectionMembers = TroopRoster.CreateDummyTroopRoster();
		AddPlayerToInspectionRoster(inspectionMembers);
		TroopRoster inspectionPrisoners = TroopRoster.CreateDummyTroopRoster();
		_pendingSelection = new PendingSelection
		{
			PlayerOriginalHitPoints = Hero.MainHero?.HitPoints ?? 0,
			PlayerOriginalWasWounded = Hero.MainHero?.IsWounded ?? false
		};
		TextObject leftName = new TextObject("可选成员 / 未参加检阅");
		TextObject rightName = new TextObject("检阅队（玩家固定属于此队）");
		int leftMemberLimit = Math.Max(selectableMembers.TotalManCount, 0);
		int leftPrisonerLimit = Math.Max(selectablePrisoners.TotalManCount, 0);
		int rightMemberLimit = Math.Max(mainParty.Party?.PartySizeLimit ?? (selectableMembers.TotalManCount + 1), selectableMembers.TotalManCount + 1);
		int rightPrisonerLimit = Math.Max(PartyBase.MainParty?.PrisonerSizeLimit ?? selectablePrisoners.TotalManCount, selectablePrisoners.TotalManCount);
		OpenInspectionSelectionScreen(null, selectableMembers, selectablePrisoners, inspectionMembers, inspectionPrisoners, leftName, rightName, leftMemberLimit, leftPrisonerLimit, rightMemberLimit, rightPrisonerLimit, InspectionTeamDoneCondition, OnInspectionTeamScreenClosed, TroopInspectionTroopTransferableDelegate);
		Log($"selection_screen_open helper=dummy_roster left_members={selectableMembers.TotalManCount} left_prisoners={selectablePrisoners.TotalManCount} source_prisoners={(PartyBase.MainParty?.PrisonRoster?.TotalManCount ?? -1)}");
	}

	private static void OpenInspectionSelectionScreen(MobileParty leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, TextObject leftPartyName, TextObject rightPartyName, int leftMemberLimit, int leftPrisonerLimit, int rightMemberLimit, int rightPrisonerLimit, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable)
	{
		PartyScreenLogic logic = new PartyScreenLogic();
		PartyScreenLogicInitializationData data = new PartyScreenLogicInitializationData
		{
			LeftOwnerParty = leftOwnerParty?.Party,
			RightOwnerParty = MobileParty.MainParty?.Party,
			LeftMemberRoster = leftMemberRoster ?? TroopRoster.CreateDummyTroopRoster(),
			LeftPrisonerRoster = leftPrisonerRoster ?? TroopRoster.CreateDummyTroopRoster(),
			RightMemberRoster = rightMemberRoster ?? TroopRoster.CreateDummyTroopRoster(),
			RightPrisonerRoster = rightPrisonerRoster ?? TroopRoster.CreateDummyTroopRoster(),
			LeftLeaderHero = leftOwnerParty?.LeaderHero,
			RightLeaderHero = PartyBase.MainParty?.LeaderHero,
			LeftPartyMembersSizeLimit = Math.Max(0, leftMemberLimit),
			LeftPartyPrisonersSizeLimit = Math.Max(0, leftPrisonerLimit),
			RightPartyMembersSizeLimit = Math.Max(1, rightMemberLimit),
			RightPartyPrisonersSizeLimit = Math.Max(0, rightPrisonerLimit),
			LeftPartyName = leftPartyName,
			RightPartyName = rightPartyName,
			TroopTransferableDelegate = isTroopTransferable,
			CanTalkToTroopDelegate = null,
			PartyPresentationDoneButtonDelegate = InspectionSelectionDoneHandler,
			PartyPresentationDoneButtonConditionDelegate = doneButtonCondition,
			PartyPresentationCancelButtonActivateDelegate = null,
			PartyPresentationCancelButtonDelegate = null,
			PartyScreenClosedDelegate = onPartyScreenClosed,
			IsDismissMode = true,
			IsTroopUpgradesDisabled = true,
			Header = null,
			TransferHealthiesGetWoundedsFirst = true,
			ShowProgressBar = false,
			MemberTransferState = PartyScreenLogic.TransferState.Transferable,
			PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
			AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable,
			PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal
		};
		logic.Initialize(data);
		PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
		state.PartyScreenLogic = logic;
		state.IsDonating = false;
		state.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
		Game.Current.GameStateManager.PushState((GameState)(object)state, 0);
	}

	private static bool InspectionSelectionDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		return true;
	}

	private static Tuple<bool, TextObject> InspectionTeamDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static void OnInspectionTeamScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		try
		{
			if (fromCancel)
			{
				ResetPendingSelection("inspection_cancel");
				Display("已取消士兵检阅。");
				return;
			}
			if (_pendingSelection == null)
			{
				ResetPendingSelection("inspection_pending_missing");
				return;
			}
			TroopRoster inspectionMembers = BuildSelectionRosterFromUi(rightMemberRoster);
			AddPlayerToInspectionRoster(inspectionMembers);
			TroopRoster inspectionPrisoners = BuildPrisonerSelectionRosterFromUi(rightPrisonRoster);
			TroopRoster holdingMembers = BuildSelectionRosterFromUi(leftMemberRoster);
			TroopRoster holdingPrisoners = BuildPrisonerSelectionRosterFromUi(leftPrisonRoster);
			_runtime = new TroopInspectionRuntime
			{
				InspectionRoster = CloneRoster(inspectionMembers),
				InspectionPrisonerRoster = CloneRoster(inspectionPrisoners),
				NotSelectedMemberRoster = CloneRoster(holdingMembers),
				NotSelectedPrisonerRoster = CloneRoster(holdingPrisoners)
			};
			_runtime.InspectionSummary = RosterSummary(_runtime.InspectionRoster) + ", prisoners=" + RosterSummary(_runtime.InspectionPrisonerRoster);
			_runtime.NotSelectedSummary = RosterSummary(_runtime.NotSelectedMemberRoster) + ", prisoners=" + RosterSummary(_runtime.NotSelectedPrisonerRoster);
			Log("selection_done inspection=" + _runtime.InspectionSummary + " not_selected=" + _runtime.NotSelectedSummary + " ui_left_members=" + RosterSummary(leftMemberRoster) + " ui_right_members=" + RosterSummary(rightMemberRoster) + " ui_left_prisoners=" + RosterSummary(leftPrisonRoster) + " ui_right_prisoners=" + RosterSummary(rightPrisonRoster));
			try
			{
				PrepareSelectionRuntimeWithMainPartySplit(_runtime, reconcileExternalCastlePrisoners: false);
			}
			catch (Exception ex)
			{
				Log("runtime_prepare failed: " + ex.GetType().Name + ": " + ex.Message);
				CleanupSplitRuntime("split_failed");
				ResetPendingSelection("split_failed");
				_runtime = null;
				Display("士兵检阅准备失败：队伍选择数据不一致，请重新选择。");
				return;
			}
			_pendingSelection = null;
			_isOpening = false;
			QueueOpenInspectionMission();
		}
		catch (Exception ex)
		{
			Log("inspection_team_screen failed: " + ex.GetType().Name + ": " + ex.Message);
			ResetPendingSelection("inspection_exception");
			Display("选择失败。");
		}
	}


	private static void OpenInspectionMissionAfterSelection(MobileParty mainParty)
	{
		EnsureMainHeroReadyForInspection("open_mission");
		PrepareRuntime(mainParty);
		MissionInitializerRecord rec = BuildMissionInitializerRecord(mainParty);
		Log($"open_battle scene={rec.SceneName} terrain={rec.TerrainType}");
		IMission openedMission = CampaignMission.OpenBattleMission(rec);
		Mission mission = openedMission as Mission;
		if (mission == null)
		{
			throw new InvalidOperationException("CampaignMission.OpenBattleMission returned non-Mission.");
		}
		_activeInspectionMission = mission;
		PlayerEncounter.StartAttackMission();
		MapEvent.PlayerMapEvent?.BeginWait();
		LogMissionSourceDiag("after_open_battle");
		Log($"mission_behaviors deployment_handler={HasMissionBehavior(mission, "BattleDeploymentHandler")} deployment_controller={mission.GetMissionBehavior<BattleDeploymentMissionController>() != null} battle_end_logic={mission.GetMissionBehavior<BattleEndLogic>() != null} mode={mission.Mode}");
		TroopInspectionMissionLogic logic = new TroopInspectionMissionLogic(_dummyPartyStringId, _runtime?.InspectionPrisonerRoster);
		mission.AddMissionBehavior(logic);
		logic.TryDisableBattleEndLogic("after_open_manual");
		Log("logic_added success");
	}




	private static void RebuildTroopRosterCachedTotals(TroopRoster roster, string label, bool throwOnFailure = false)
	{
		if (roster == null)
		{
			return;
		}
		try
		{
			if (TroopRosterTotalRegularsField == null || TroopRosterTotalWoundedRegularsField == null || TroopRosterTotalHeroesField == null || TroopRosterTotalWoundedHeroesField == null)
			{
				if (throwOnFailure)
				{
					throw new InvalidOperationException("TroopRoster cache fields are unavailable.");
				}
				Log("roster_cache_repair_unavailable label=" + label);
				return;
			}
			int totalRegulars = 0;
			int totalWoundedRegulars = 0;
			int totalHeroes = 0;
			int totalWoundedHeroes = 0;
			foreach (TroopRosterElement item in SnapshotRoster(roster))
			{
				CharacterObject character = item.Character;
				if (character == null || item.Number <= 0)
				{
					continue;
				}
				if (character.IsHero)
				{
					totalHeroes++;
					if (Math.Max(0, item.WoundedNumber) > 0 || (character.HeroObject != null && character.HeroObject.IsWounded))
					{
						totalWoundedHeroes++;
					}
					continue;
				}
				totalRegulars += Math.Max(0, item.Number);
				totalWoundedRegulars += Math.Max(0, item.WoundedNumber);
			}
			int beforeRegulars = GetIntFieldValue(TroopRosterTotalRegularsField, roster);
			int beforeWoundedRegulars = GetIntFieldValue(TroopRosterTotalWoundedRegularsField, roster);
			int beforeHeroes = GetIntFieldValue(TroopRosterTotalHeroesField, roster);
			int beforeWoundedHeroes = GetIntFieldValue(TroopRosterTotalWoundedHeroesField, roster);
			if (beforeRegulars == totalRegulars && beforeWoundedRegulars == totalWoundedRegulars && beforeHeroes == totalHeroes && beforeWoundedHeroes == totalWoundedHeroes)
			{
				return;
			}
			TroopRosterTotalRegularsField.SetValue(roster, totalRegulars);
			TroopRosterTotalWoundedRegularsField.SetValue(roster, totalWoundedRegulars);
			TroopRosterTotalHeroesField.SetValue(roster, totalHeroes);
			TroopRosterTotalWoundedHeroesField.SetValue(roster, totalWoundedHeroes);
			try
			{
				roster.UpdateVersion();
			}
			catch
			{
			}
			Log("roster_cache_repaired label=" + label + " before_regular=" + beforeRegulars + " before_heroes=" + beforeHeroes + " before_wounded_regular=" + beforeWoundedRegulars + " before_wounded_heroes=" + beforeWoundedHeroes + " after_regular=" + totalRegulars + " after_heroes=" + totalHeroes + " after_wounded_regular=" + totalWoundedRegulars + " after_wounded_heroes=" + totalWoundedHeroes + " before_total=" + (beforeRegulars + beforeHeroes) + " after_total=" + (totalRegulars + totalHeroes));
		}
		catch (Exception ex)
		{
			Log("roster_cache_repair_failed label=" + label + " " + ex.GetType().Name + ": " + ex.Message);
			if (throwOnFailure)
			{
				throw;
			}
		}
	}

	private static void PrepareMainPartyRosterStateForInspection(string reason)
	{
		EnsureMainHeroReadyForInspection(reason);
		RebuildTroopRosterCachedTotals(MobileParty.MainParty?.MemberRoster, reason + "_main_pre", throwOnFailure: true);
		RebuildTroopRosterCachedTotals(PartyBase.MainParty?.PrisonRoster, reason + "_prisoners_pre", throwOnFailure: true);
		NormalizeMainPartyHeroRosterForInspection(reason);
		RepairMainPartyHeroBelongedToForInspection(reason);
		RebuildTroopRosterCachedTotals(MobileParty.MainParty?.MemberRoster, reason + "_main_post", throwOnFailure: true);
		RebuildTroopRosterCachedTotals(PartyBase.MainParty?.PrisonRoster, reason + "_prisoners_post", throwOnFailure: true);
		ValidateMainPartyHeroRosterReadyForInspection(reason);
	}

	private static int GetIntFieldValue(FieldInfo field, object target)
	{
		try
		{
			object value = field?.GetValue(target);
			return value is int result ? result : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static void NormalizeMainPartyHeroRosterForInspection(string reason)
	{
		try
		{
			TroopRoster roster = MobileParty.MainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
			if (roster == null)
			{
				return;
			}
			foreach (TroopRosterElement item in SnapshotRoster(roster))
			{
				CharacterObject character = item.Character;
				if (character == null || !character.IsHero || item.Number <= 0)
				{
					continue;
				}
				Hero hero = character.HeroObject;
				string partyId = hero?.PartyBelongedTo?.StringId ?? "null";
				if (item.Number > 1)
				{
					int removeNumber = item.Number - 1;
					int removeWounded = Math.Min(removeNumber, Math.Max(0, item.WoundedNumber));
					int removeXp = CalculateRosterXpToMove(item, removeNumber);
					roster.AddToCounts(character, -removeNumber, false, -removeWounded, -removeXp, true, -1);
					Log("hero_roster_normalized reason=" + reason + " troop=" + SafeCharacterId(character) + " before_number=" + item.Number + " after_number=1 before_wounded=" + item.WoundedNumber + " removed_wounded=" + removeWounded + " party=" + partyId);
				}
				if (hero != null && hero.PartyBelongedTo != MobileParty.MainParty)
				{
					Log("hero_party_mismatch_diag reason=" + reason + " troop=" + SafeCharacterId(character) + " party=" + partyId + " main=" + (MobileParty.MainParty?.StringId ?? "null"));
				}
			}
			RebuildTroopRosterCachedTotals(roster, "hero_normalize_" + reason, throwOnFailure: true);
		}
		catch (Exception ex)
		{
			Log("hero_roster_normalize_failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
			throw;
		}
	}

	private static void RepairMainPartyHeroBelongedToForInspection(string reason)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			TroopRoster roster = mainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
			if (mainParty == null || roster == null)
			{
				return;
			}
			foreach (TroopRosterElement item in SnapshotRoster(roster))
			{
				CharacterObject character = item.Character;
				Hero hero = character?.HeroObject;
				if (character == null || !character.IsHero || hero == null || item.Number <= 0)
				{
					continue;
				}
				MobileParty beforeParty = hero.PartyBelongedTo;
				if (beforeParty != null)
				{
					continue;
				}
				SetPrivateField(hero, "_partyBelongedTo", mainParty);
				if (hero.PartyBelongedTo != mainParty)
				{
					throw new InvalidOperationException("Failed to rebind hero to MainParty. troop=" + SafeCharacterId(character) + " party=" + (hero.PartyBelongedTo?.StringId ?? "null"));
				}
				Log("hero_party_repaired reason=" + reason + " troop=" + SafeCharacterId(character) + " from=null to=" + (hero.PartyBelongedTo?.StringId ?? "null"));
			}
		}
		catch (Exception ex)
		{
			Log("hero_party_repair_failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
			throw;
		}
	}

	private static void ValidateMainPartyHeroRosterReadyForInspection(string reason)
	{
		MobileParty mainParty = MobileParty.MainParty;
		TroopRoster roster = mainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
		if (mainParty == null || roster == null)
		{
			throw new InvalidOperationException("MainParty roster is unavailable before inspection. reason=" + reason);
		}
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			CharacterObject character = item.Character;
			Hero hero = character?.HeroObject;
			if (character == null || !character.IsHero || hero == null || item.Number <= 0)
			{
				continue;
			}
			if (item.Number != 1)
			{
				throw new InvalidOperationException("Hero stack is invalid before inspection. reason=" + reason + " troop=" + SafeCharacterId(character) + " number=" + item.Number);
			}
			if (!character.IsPlayerCharacter && !hero.IsDead && hero.PartyBelongedTo != mainParty)
			{
				throw new InvalidOperationException("Hero party mismatch before inspection. reason=" + reason + " troop=" + SafeCharacterId(character) + " party=" + (hero.PartyBelongedTo?.StringId ?? "null") + " main=" + (mainParty.StringId ?? "null"));
			}
		}
	}


	private static int CalculateRosterXpToMove(TroopRosterElement sourceElement, int numberToMove)
	{
		try
		{
			int number = Math.Max(0, sourceElement.Number);
			int xp = Math.Max(0, sourceElement.Xp);
			numberToMove = Math.Max(0, numberToMove);
			if (number <= 0 || xp <= 0 || numberToMove <= 0)
			{
				return 0;
			}
			if (numberToMove >= number)
			{
				return xp;
			}
			int result = (int)Math.Round((double)xp * numberToMove / number, MidpointRounding.AwayFromZero);
			return Math.Max(0, Math.Min(xp, result));
		}
		catch
		{
			return 0;
		}
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
					if (character.HeroObject != null && character.HeroObject.IsDead)
					{
						result.DeadHeroesSkipped++;
					}
					else if (!character.IsPlayerCharacter && character.HeroObject != null)
					{
						Hero hero = character.HeroObject;
						MobileParty beforeParty = hero.PartyBelongedTo;
						if (beforeParty == sourceParty)
						{
							AddHeroToPartyAction.Apply(hero, mainParty, false);
							result.Heroes++;
							Log("cleanup_return_hero label=" + label + " troop=" + SafeCharacterId(character) + " mode=action from=" + (beforeParty?.StringId ?? "null") + " to=" + (hero.PartyBelongedTo?.StringId ?? "null") + " source_number=" + item.Number);
						}
						else
						{
							result.HeroLikeSkipped += Math.Max(1, item.Number);
							Log("cleanup_return_hero_skipped label=" + label + " troop=" + SafeCharacterId(character) + " reason=party_mismatch party=" + (beforeParty?.StringId ?? "null") + " source=" + (sourceParty.StringId ?? "null") + " source_number=" + item.Number);
						}
					}
					continue;
				}
				int number = Math.Max(0, item.Number);
				int wounded = Math.Max(0, item.WoundedNumber);
				int xp = Math.Max(0, item.Xp);
				sourceParty.MemberRoster.AddToCounts(character, -number, false, -wounded, -xp, true, -1);
				mainParty.MemberRoster.AddToCounts(character, number, false, wounded, xp, true, -1);
				result.RegularMen += number;
				result.RegularWounded += wounded;
				result.RegularXp += xp;
			}
			catch (Exception ex)
			{
				result.Errors++;
				Log("cleanup_return_element_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
			}
		}
		RebuildTroopRosterCachedTotals(sourceParty.MemberRoster, "cleanup_return_source_" + label);
		RebuildTroopRosterCachedTotals(mainParty.MemberRoster, "cleanup_return_main_" + label);
		Log($"cleanup_return_summary label={label} {result}");
		return result;
	}

	private static MoveRosterResult MoveAllPrisonersBackToMainParty(MobileParty sourceParty, string label)
	{
		TroopRoster mainRoster = PartyBase.MainParty?.PrisonRoster;
		MoveRosterResult result = new MoveRosterResult();
		if (sourceParty == null || mainRoster == null || sourceParty.PrisonRoster == null)
		{
			Log($"cleanup_prisoner_return_skipped label={label} source_null={sourceParty == null} main_prison_null={mainRoster == null}");
			return result;
		}
		foreach (TroopRosterElement item in SnapshotRoster(sourceParty.PrisonRoster))
		{
			try
			{
				CharacterObject character = item.Character;
				if (character == null || item.Number <= 0)
				{
					continue;
				}
				int number = Math.Max(0, item.Number);
				int wounded = Math.Max(0, item.WoundedNumber);
				int xp = Math.Max(0, item.Xp);
				sourceParty.PrisonRoster.AddToCounts(character, -number, false, -wounded, -xp, true, -1);
				mainRoster.AddToCounts(character, number, false, wounded, xp, true, -1);
				if (character.IsHero)
				{
					result.Heroes += number;
				}
				else
				{
					result.RegularMen += number;
					result.RegularWounded += wounded;
					result.RegularXp += xp;
				}
			}
			catch (Exception ex)
			{
				result.Errors++;
				Log("cleanup_prisoner_return_element_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
			}
		}
		RebuildTroopRosterCachedTotals(sourceParty.PrisonRoster, "cleanup_prisoner_return_source_" + label);
		RebuildTroopRosterCachedTotals(mainRoster, "cleanup_prisoner_return_main_" + label);
		Log($"cleanup_prisoner_return_summary label={label} {result}");
		return result;
	}

	private static MainPartyRoleSnapshot CaptureMainPartyRoleSnapshot(string reason)
	{
		MainPartyRoleSnapshot snapshot = new MainPartyRoleSnapshot();
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				Log("role_snapshot reason=" + reason + " skipped=main_party_null");
				return snapshot;
			}
			snapshot.Scout = mainParty.GetRoleHolder(PartyRole.Scout);
			snapshot.Engineer = mainParty.GetRoleHolder(PartyRole.Engineer);
			snapshot.Quartermaster = mainParty.GetRoleHolder(PartyRole.Quartermaster);
			snapshot.Surgeon = mainParty.GetRoleHolder(PartyRole.Surgeon);
			Log("role_snapshot reason=" + reason
				+ " scout=" + SafeHeroId(snapshot.Scout)
				+ " engineer=" + SafeHeroId(snapshot.Engineer)
				+ " quartermaster=" + SafeHeroId(snapshot.Quartermaster)
				+ " surgeon=" + SafeHeroId(snapshot.Surgeon));
		}
		catch (Exception ex)
		{
			Log("role_snapshot failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
		}
		return snapshot;
	}

	private static void RestoreMainPartyRolesFromSnapshot(TroopInspectionRuntime runtime, string reason)
	{
		MainPartyRoleSnapshot snapshot = runtime?.RoleSnapshot;
		if (snapshot == null || snapshot.Restored)
		{
			return;
		}
		snapshot.Restored = true;
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				Log("role_restore reason=" + reason + " skipped=main_party_null");
				return;
			}
			List<string> changes = new List<string>();
			RestoreMainPartyRole(mainParty, PartyRole.Scout, snapshot.Scout, changes);
			RestoreMainPartyRole(mainParty, PartyRole.Engineer, snapshot.Engineer, changes);
			RestoreMainPartyRole(mainParty, PartyRole.Quartermaster, snapshot.Quartermaster, changes);
			RestoreMainPartyRole(mainParty, PartyRole.Surgeon, snapshot.Surgeon, changes);
			if (changes.Count > 0)
			{
				Log("role_restore reason=" + reason + " " + string.Join(" ", changes));
			}
		}
		catch (Exception ex)
		{
			Log("role_restore failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void RestoreMainPartyRole(MobileParty mainParty, PartyRole role, Hero originalHolder, List<string> changes)
	{
		string roleName = GetPartyRoleLogName(role);
		Hero currentHolder = mainParty.GetRoleHolder(role);
		if (originalHolder == null)
		{
			if (currentHolder != null)
			{
				SetMainPartyRole(mainParty, role, null);
				changes.Add(roleName + "=cleared_from:" + SafeHeroId(currentHolder));
			}
			return;
		}
		if (!CanRestoreMainPartyRoleHero(mainParty, originalHolder, out string skipReason))
		{
			changes.Add(roleName + "=skipped:" + skipReason + ":" + SafeHeroId(originalHolder));
			return;
		}
		if (currentHolder == originalHolder)
		{
			return;
		}
		SetMainPartyRole(mainParty, role, originalHolder);
		changes.Add(roleName + "=restored:" + SafeHeroId(originalHolder));
	}

	private static bool CanRestoreMainPartyRoleHero(MobileParty mainParty, Hero hero, out string reason)
	{
		if (hero == null)
		{
			reason = "null";
			return false;
		}
		if (hero.IsDead)
		{
			reason = "dead";
			return false;
		}
		if (hero.IsPrisoner)
		{
			reason = "prisoner";
			return false;
		}
		if (hero.PartyBelongedTo != mainParty)
		{
			reason = "party_" + (hero.PartyBelongedTo?.StringId ?? "null");
			return false;
		}
		if (mainParty.MemberRoster == null || !mainParty.MemberRoster.Contains(hero.CharacterObject))
		{
			reason = "not_in_main_roster";
			return false;
		}
		reason = null;
		return true;
	}

	private static void SetMainPartyRole(MobileParty mainParty, PartyRole role, Hero hero)
	{
		switch (role)
		{
			case PartyRole.Scout:
				mainParty.SetPartyScout(hero);
				break;
			case PartyRole.Engineer:
				mainParty.SetPartyEngineer(hero);
				break;
			case PartyRole.Quartermaster:
				mainParty.SetPartyQuartermaster(hero);
				break;
			case PartyRole.Surgeon:
				mainParty.SetPartySurgeon(hero);
				break;
		}
	}

	private static string GetPartyRoleLogName(PartyRole role)
	{
		switch (role)
		{
			case PartyRole.Scout:
				return "scout";
			case PartyRole.Engineer:
				return "engineer";
			case PartyRole.Quartermaster:
				return "quartermaster";
			case PartyRole.Surgeon:
				return "surgeon";
			default:
				return role.ToString().ToLowerInvariant();
		}
	}

	private static string SafeHeroId(Hero hero)
	{
		return SafeCharacterId(hero?.CharacterObject);
	}


	private static void CleanupOrphanSelectionPoolDummyParties(string label)
	{
		try
		{
			List<MobileParty> parties = new List<MobileParty>();
			foreach (MobileParty party in MobileParty.All)
			{
				if (party != null && party.IsActive && IsInspectionSelectionPoolDummyParty(party))
				{
					parties.Add(party);
				}
			}
			foreach (MobileParty party in parties)
			{
				ClearRosterDirect(party.MemberRoster);
				ClearRosterDirect(party.PrisonRoster);
				DestroyPartyAction.Apply((PartyBase)null, party);
				Log("selection_pool_orphan_destroyed label=" + label + " id=" + (party.StringId ?? "null"));
			}
		}
		catch (Exception ex)
		{
			Log("cleanup orphan selection_pool failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupSplitRuntime(string reason)
	{
		CleanupOrphanSelectionPoolDummyParties(reason + "_selection_pool_orphan");
		CleanupOrphanHoldingDummyParties(null, reason + "_legacy_holding_orphan");
		RestoreMainPartyRolesFromSnapshot(_runtime, reason);
	}

	private static void ClearRosterDirect(TroopRoster roster)
	{
		if (roster == null)
		{
			return;
		}
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			try
			{
				CharacterObject character = item.Character;
				if (character != null && item.Number > 0)
				{
					roster.AddToCounts(character, -Math.Max(0, item.Number), false, -Math.Max(0, item.WoundedNumber), -Math.Max(0, item.Xp), true, -1);
				}
			}
			catch
			{
			}
		}
	}

	internal static List<TroopRosterElement> SnapshotRoster(TroopRoster roster)
	{
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		if (roster == null)
		{
			return list;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement element = GetFreshRosterElementCopy(roster, i);
			if (element.Character != null && element.Number > 0)
			{
				list.Add(element);
			}
		}
		return list;
	}

	internal static TroopRosterElement GetFreshRosterElementCopy(TroopRoster roster, int index)
	{
		TroopRosterElement element = roster.GetElementCopyAtIndex(index);
		try
		{
			element.Xp = roster.GetElementXp(index);
		}
		catch
		{
		}
		return element;
	}

	private static bool TroopInspectionTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character != null && !character.IsPlayerCharacter && !character.IsNotTransferableInPartyScreen;
	}

	private static TroopRoster BuildSelectableRoster(TroopRoster sourceRoster)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in sourceRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			int healthy = character.IsHero ? ((character.HeroObject != null && !character.HeroObject.IsDead && !character.HeroObject.IsWounded) ? 1 : 0) : Math.Max(0, item.Number - item.WoundedNumber);
			if (healthy > 0)
			{
				roster.AddToCounts(character, healthy, false, 0, CalculateRosterXpToMove(item, healthy), true, -1);
			}
		}
		return roster;
	}

	private static TroopRoster BuildSelectablePrisonerRoster(TroopRoster sourceRoster)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return roster;
		}
		for (int i = 0; i < sourceRoster.Count; i++)
		{
			TroopRosterElement item = GetFreshRosterElementCopy(sourceRoster, i);
			if (item.Character != null && item.Number > 0)
			{
				int number = item.Character.IsHero ? 1 : Math.Max(0, item.Number);
				int wounded = item.Character.IsHero ? 0 : Math.Max(0, item.WoundedNumber);
				roster.AddToCounts(item.Character, number, false, wounded, Math.Max(0, item.Xp), true, -1);
			}
		}
		return roster;
	}

	private static TroopRoster BuildSelectionRosterFromUi(TroopRoster sourceRoster)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in sourceRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			int healthy = character.IsHero ? 1 : Math.Max(0, item.Number - item.WoundedNumber);
			if (healthy > 0)
			{
				roster.AddToCounts(character, healthy, false, 0, 0, true, -1);
			}
		}
		return roster;
	}

	private static TroopRoster BuildPrisonerSelectionRosterFromUi(TroopRoster sourceRoster)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return roster;
		}
		for (int i = 0; i < sourceRoster.Count; i++)
		{
			TroopRosterElement item = GetFreshRosterElementCopy(sourceRoster, i);
			if (item.Character != null && item.Number > 0)
			{
				int number = item.Character.IsHero ? 1 : Math.Max(0, item.Number);
				int wounded = item.Character.IsHero ? 0 : Math.Min(number, Math.Max(0, item.WoundedNumber));
				roster.AddToCounts(item.Character, number, false, wounded, 0, true, -1);
			}
		}
		return roster;
	}

	internal static TroopRoster CloneRoster(TroopRoster sourceRoster)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in SnapshotRoster(sourceRoster))
		{
			if (item.Character != null && item.Number > 0)
			{
				roster.Add(item);
			}
		}
		return roster;
	}

	private static void AddPlayerToInspectionRoster(TroopRoster inspectionRoster)
	{
		if (inspectionRoster == null)
		{
			return;
		}
		CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
		if (playerCharacter == null || inspectionRoster.Contains(playerCharacter))
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
					inspectionRoster.Add(item);
					return;
				}
			}
		}
		inspectionRoster.AddToCounts(playerCharacter, 1, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
	}

	private static void ResetPendingSelection(string reason)
	{
		Log("selection_reset reason=" + (reason ?? "unknown"));
		CleanupOrphanSelectionPoolDummyParties(reason + "_selection_pool_orphan");
		_pendingSelection = null;
		_isOpening = false;
		_queuedOpenInspection = false;
	}

	private static void QueueOpenInspectionMission()
	{
		_queuedOpenInspection = true;
		_queuedOpenInspectionAt = (float)Environment.TickCount / 1000f + 0.35f;
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

	private static string RosterSummary(TroopRoster roster)
	{
		try
		{
			if (roster == null)
			{
				return "null";
			}
			int heroes = 0;
			int regular = 0;
			int wounded = 0;
			List<string> samples = VerboseInspectionLogs ? new List<string>() : null;
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement item = GetFreshRosterElementCopy(roster, i);
				if (item.Character == null || item.Number <= 0)
				{
					continue;
				}
				wounded += Math.Max(0, item.WoundedNumber);
				if (item.Character.IsHero)
				{
					heroes += Math.Max(0, item.Number);
				}
				else
				{
					regular += Math.Max(0, item.Number);
				}
				if (samples != null && samples.Count < 8)
				{
					Hero hero = item.Character.HeroObject;
					samples.Add(SafeCharacterId(item.Character) + ":n=" + item.Number + ",w=" + item.WoundedNumber + ",hero=" + item.Character.IsHero + ",alive=" + (hero?.IsAlive.ToString() ?? "null") + ",dead=" + (hero?.IsDead.ToString() ?? "null") + ",wounded=" + (hero?.IsWounded.ToString() ?? "null"));
				}
			}
			string summary = "entries=" + roster.Count + ",total=" + roster.TotalManCount + ",regular=" + regular + ",heroes=" + heroes + ",wounded=" + wounded;
			if (samples != null)
			{
				summary += ",samples=[" + string.Join(";", samples) + "]";
			}
			return summary;
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name;
		}
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

	private static void PrepareRuntime(MobileParty mainParty)
	{
		CaptureAndDetachExternalCampaignEncounter();
		CampaignVec2 mainPosition = mainParty.Position;
		Vec2 direction = ResolveEncounterDirection(mainParty);
		CampaignVec2 dummyPosition = mainPosition - direction * 0.4f;
		_dummyPartyStringId = DummyPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		_dummyParty = MobileParty.CreateParty(_dummyPartyStringId, new TroopInspectionDummyPartyComponent(dummyPosition, new TextObject("AnimusForge Troop Inspection Dummy"), Hero.MainHero, Clan.PlayerClan));
		if (_dummyParty == null)
		{
			throw new InvalidOperationException("Failed to create dummy party.");
		}
		_dummyParty.IsVisible = false;
		_dummyParty.SetMoveModeHold();
		Log($"dummy_party_create id={_dummyParty.StringId} pos={FormatCampaignVec2(_dummyParty.Position)} members={_dummyParty.Party.NumberOfHealthyMembers}");
		LogMissionSourceDiag("before_mapevent_create");
		FieldBattleEventComponent component = FieldBattleEventComponent.CreateFieldBattleEvent(PartyBase.MainParty, _dummyParty.Party);
		_mapEvent = component?.MapEvent;
		if (_mapEvent == null)
		{
			throw new InvalidOperationException("Failed to create field battle MapEvent.");
		}
		_mapEvent.ResetBattleState();
		int attackerCount = _mapEvent.AttackerSide.RecalculateMemberCountOfSide();
		int defenderCount = _mapEvent.DefenderSide.RecalculateMemberCountOfSide();
		Log($"mapevent_create attacker_side_count={attackerCount} defender_side_count={defenderCount} player_side={_mapEvent.PlayerSide} is_player_mapevent={_mapEvent.IsPlayerMapEvent}");
		LogMissionSourceDiag("after_mapevent_create");
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(PartyBase.MainParty, _dummyParty.Party);
		SetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent", _mapEvent);
		Log($"player_encounter_context battle={PlayerEncounter.Battle != null} is_mapevent={PlayerEncounter.Battle == _mapEvent} player_mapevent={MapEvent.PlayerMapEvent == _mapEvent}");
	}

	private static void CaptureAndDetachExternalCampaignEncounter()
	{
		TroopInspectionRuntime runtime = _runtime;
		if (runtime?.RestoreCampaignEncounterAfterInspection != true)
		{
			return;
		}

		runtime.OriginalPlayerEncounter = PlayerEncounter.Current;
		runtime.OriginalMainPartyMapEventSide = PartyBase.MainParty?.MapEventSide;
		if (PartyBase.MainParty != null && runtime.OriginalMainPartyMapEventSide != null)
		{
			PartyBase.MainParty.MapEventSide = null;
		}
		Log("external_campaign_context_detached original_encounter=" + (runtime.OriginalPlayerEncounter != null)
			+ " original_mapevent=" + (runtime.OriginalMainPartyMapEventSide?.MapEvent != null));
	}

	private static void RestoreExternalCampaignEncounter(TroopInspectionRuntime runtime, string reason)
	{
		if (runtime?.RestoreCampaignEncounterAfterInspection != true)
		{
			return;
		}

		try
		{
			if (PartyBase.MainParty != null)
			{
				PartyBase.MainParty.MapEventSide = runtime.OriginalMainPartyMapEventSide;
			}
			SetPlayerEncounterProperty(runtime.OriginalPlayerEncounter);
			Log("external_campaign_context_restored reason=" + (reason ?? "N/A")
				+ " encounter=" + (runtime.OriginalPlayerEncounter != null)
				+ " mapevent=" + (runtime.OriginalMainPartyMapEventSide?.MapEvent != null));
		}
		catch (Exception ex)
		{
			Log("external_campaign_context_restore failed reason=" + (reason ?? "N/A")
				+ " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static MissionInitializerRecord BuildMissionInitializerRecord(MobileParty mainParty)
	{
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		MapPatchData patch = mapSceneWrapper.GetMapPatchAtPosition(mainParty.Position);
		string scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, false);
		if (string.IsNullOrWhiteSpace(scene))
		{
			throw new InvalidOperationException("Battle scene is empty.");
		}
		MissionInitializerRecord rec = new MissionInitializerRecord(scene);
		TerrainType terrainType = BannerlordApiCompat.ResolveTerrainTypeForParty(mainParty, TerrainType.Plain, allowNavigationFaceFallback: false);
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
		rec.PatchEncounterDir = ResolvePatchEncounterDirection();
		return rec;
	}

	private static Vec2 ResolvePatchEncounterDirection()
	{
		try
		{
			if (_mapEvent?.AttackerSide?.LeaderParty != null && _mapEvent.DefenderSide?.LeaderParty != null)
			{
				Vec2 v = _mapEvent.AttackerSide.LeaderParty.Position.ToVec2() - _mapEvent.DefenderSide.LeaderParty.Position.ToVec2();
				if (v.LengthSquared > 0.0001f)
				{
					return v.Normalized();
				}
			}
		}
		catch
		{
		}
		return ResolveEncounterDirection(MobileParty.MainParty);
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

	private static bool IsOwnDummyParty(MobileParty party, string expectedStringId)
	{
		if (party == null || string.IsNullOrEmpty(expectedStringId))
		{
			return false;
		}
		return string.Equals(party.StringId, expectedStringId, StringComparison.Ordinal) && party.StringId.StartsWith(DummyPartyPrefix, StringComparison.Ordinal);
	}

	

	private static void CaptureMainHeroStateIfNeeded()
	{
		try
		{
			if (_playerStateCaptured)
			{
				return;
			}
			Hero mainHero = Hero.MainHero;
			if (mainHero == null)
			{
				return;
			}
			_playerOriginalHitPoints = mainHero.HitPoints;
			_playerOriginalWasWounded = mainHero.IsWounded;
			_playerStateCaptured = true;
		}
		catch
		{
		}
	}

	private static void EnsureMainHeroReadyForInspection(string reason)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? mainHero?.CharacterObject;
			if (mainHero == null || playerCharacter == null)
			{
				return;
			}
			CaptureMainHeroStateIfNeeded();
			int beforeHp = mainHero.HitPoints;
			bool beforeWounded = mainHero.IsWounded;
			bool beforeDead = mainHero.IsDead;
			int beforeRosterWounded = GetMainPartyPlayerWoundedNumber(playerCharacter);
			bool hpRestored = false;
			if (!beforeDead && beforeWounded && _playerStateCaptured && !_playerOriginalWasWounded)
			{
				mainHero.HitPoints = GetHealthyHitPointsFor(mainHero, _playerOriginalHitPoints);
				hpRestored = mainHero.HitPoints != beforeHp;
			}
			bool rosterFixed = false;
			if (!mainHero.IsDead && !mainHero.IsWounded && beforeRosterWounded > 0)
			{
				rosterFixed = TrySetMainPartyPlayerWoundedNumber(playerCharacter, 0);
			}
			if (hpRestored || rosterFixed || beforeWounded || beforeRosterWounded > 0)
			{
				Log($"player_ready_check reason={reason} before_hp={beforeHp} after_hp={mainHero.HitPoints} before_wounded={beforeWounded} after_wounded={mainHero.IsWounded} dead={beforeDead} before_roster_wounded={beforeRosterWounded} after_roster_wounded={GetMainPartyPlayerWoundedNumber(playerCharacter)} hp_restored={hpRestored} roster_fixed={rosterFixed}");
			}
		}
		catch (Exception ex)
		{
			Log("player_ready_check failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void RestoreMainHeroAfterInspection(string reason)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? mainHero?.CharacterObject;
			if (mainHero == null || playerCharacter == null)
			{
				return;
			}
			int beforeHp = mainHero.HitPoints;
			bool beforeWounded = mainHero.IsWounded;
			bool beforeDead = mainHero.IsDead;
			int beforeRosterWounded = GetMainPartyPlayerWoundedNumber(playerCharacter);
			bool hpRestored = false;
			if (_playerStateCaptured && !_playerOriginalWasWounded && !beforeDead)
			{
				int targetHp = GetHealthyHitPointsFor(mainHero, _playerOriginalHitPoints);
				if (mainHero.HitPoints < targetHp || mainHero.IsWounded)
				{
					mainHero.HitPoints = targetHp;
					hpRestored = true;
				}
			}
			bool rosterFixed = false;
			if (!mainHero.IsDead && !mainHero.IsWounded && beforeRosterWounded > 0)
			{
				rosterFixed = TrySetMainPartyPlayerWoundedNumber(playerCharacter, 0);
			}
			Log($"player_health_restore reason={reason} captured={_playerStateCaptured} original_hp={_playerOriginalHitPoints} original_wounded={_playerOriginalWasWounded} before_hp={beforeHp} after_hp={mainHero.HitPoints} before_wounded={beforeWounded} after_wounded={mainHero.IsWounded} dead={beforeDead} before_roster_wounded={beforeRosterWounded} after_roster_wounded={GetMainPartyPlayerWoundedNumber(playerCharacter)} hp_restored={hpRestored} roster_fixed={rosterFixed}");
		}
		catch (Exception ex)
		{
			Log("player_health_restore failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			_playerStateCaptured = false;
			_playerOriginalHitPoints = 0;
			_playerOriginalWasWounded = false;
		}
	}

	private static int GetHealthyHitPointsFor(Hero hero, int preferredHitPoints)
	{
		try
		{
			int maxHitPoints = Math.Max(1, hero.MaxHitPoints);
			int minHealthyHitPoints = Math.Max(1, hero.WoundedHealthLimit + 1);
			int target = preferredHitPoints > 0 ? preferredHitPoints : minHealthyHitPoints;
			if (target < minHealthyHitPoints)
			{
				target = minHealthyHitPoints;
			}
			if (target > maxHitPoints)
			{
				target = maxHitPoints;
			}
			return Math.Max(1, target);
		}
		catch
		{
			return Math.Max(1, preferredHitPoints);
		}
	}

	private static int GetMainPartyPlayerWoundedNumber(CharacterObject playerCharacter)
	{
		try
		{
			TroopRoster memberRoster = MobileParty.MainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
			if (memberRoster == null || playerCharacter == null)
			{
				return -1;
			}
			int index = memberRoster.FindIndexOfTroop(playerCharacter);
			if (index < 0)
			{
				return -1;
			}
			return memberRoster.GetElementWoundedNumber(index);
		}
		catch
		{
			return -1;
		}
	}

	private static bool TrySetMainPartyPlayerWoundedNumber(CharacterObject playerCharacter, int woundedNumber)
	{
		try
		{
			TroopRoster memberRoster = MobileParty.MainParty?.MemberRoster ?? PartyBase.MainParty?.MemberRoster;
			if (memberRoster == null || playerCharacter == null)
			{
				return false;
			}
			int index = memberRoster.FindIndexOfTroop(playerCharacter);
			if (index < 0)
			{
				return false;
			}
			int currentWounded = memberRoster.GetElementWoundedNumber(index);
			int targetWounded = Math.Max(0, Math.Min(memberRoster.GetElementNumber(index), woundedNumber));
			if (currentWounded == targetWounded)
			{
				return false;
			}
			memberRoster.AddToCounts(playerCharacter, 0, false, targetWounded - currentWounded, 0, true, index);
			return true;
		}
		catch (Exception ex)
		{
			Log("player_roster_wounded_fix failed: " + ex.GetType().Name + ": " + ex.Message);
			return false;
		}
	}

	private static string BuildMainHeroInspectionStateSummary()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? mainHero?.CharacterObject;
			if (mainHero == null)
			{
				return "player_state=null";
			}
			return $"player_state=hp:{mainHero.HitPoints}/{mainHero.MaxHitPoints},wounded:{mainHero.IsWounded},dead:{mainHero.IsDead},wounded_limit:{mainHero.WoundedHealthLimit},roster_wounded:{GetMainPartyPlayerWoundedNumber(playerCharacter)}";
		}
		catch (Exception ex)
		{
			return "player_state_error=" + ex.GetType().Name + ":" + ex.Message;
		}
	}

	private static string FormatCampaignVec2(CampaignVec2 position)
	{
		return $"{position.X:0.00},{position.Y:0.00}";
	}

	private static void Display(string message)
	{
		AnimusForgeQuickInfo.Show(message);
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
			var field = target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
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
		SetPlayerEncounterProperty(null);
	}

	private static void SetPlayerEncounterProperty(PlayerEncounter encounter)
	{
		try
		{
			if (Campaign.Current != null)
			{
				typeof(Campaign).GetProperty("PlayerEncounter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(Campaign.Current, encounter);
			}
		}
		catch
		{
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
			var getMethod = typeof(Mission).GetMethod("GetMissionBehavior", Type.EmptyTypes);
			if (getMethod == null)
			{
				return false;
			}
			var generic = getMethod.MakeGenericMethod(handlerType);
			return generic.Invoke(mission, null) != null;
		}
		catch
		{
			return false;
		}
	}

	internal static void Log(string message)
	{
		try
		{
			if (!ShouldWriteInspectionLog(message))
			{
				return;
			}
			string path = GetInspectionLogPath();
			File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [TroopInspection] " + message + Environment.NewLine);
		}
		catch
		{
			try
			{
				Logger.Log(LogPrefix, "[TroopInspection] " + message);
			}
			catch
			{
			}
		}
	}

	private static bool ShouldWriteInspectionLog(string message)
	{
		if (VerboseInspectionLogs)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(message))
		{
			return false;
		}
		if (message.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
			message.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
			message.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
			message.IndexOf("blocked", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (message.StartsWith("terminal_open", StringComparison.Ordinal) ||
			message.StartsWith("precheck ", StringComparison.Ordinal) ||
			message.StartsWith("selection_screen_open", StringComparison.Ordinal) ||
			message.StartsWith("selection_done", StringComparison.Ordinal) ||
			message.StartsWith("split_validate_ok", StringComparison.Ordinal) ||
			message.StartsWith("split_validate_fail", StringComparison.Ordinal) ||
			message.StartsWith("split_validate_reconcile", StringComparison.Ordinal) ||
			message.StartsWith("runtime_ready", StringComparison.Ordinal) ||
			message.StartsWith("mapevent_create", StringComparison.Ordinal) ||
			message.StartsWith("mission_behaviors", StringComparison.Ordinal) ||
			message.StartsWith("logic_added", StringComparison.Ordinal) ||
			message.StartsWith("spawn_wait_diag", StringComparison.Ordinal) ||
			message.StartsWith("spawn_prisoners_direct", StringComparison.Ordinal) ||
			message.StartsWith("spawn_prisoners_begin", StringComparison.Ordinal) ||
			message.StartsWith("spawn_prisoners result", StringComparison.Ordinal) ||
			message.StartsWith("deployment_bypassed_for_prisoners", StringComparison.Ordinal) ||
			message.StartsWith("deployment_ended", StringComparison.Ordinal) ||
			message.StartsWith("mission_ended_check", StringComparison.Ordinal) ||
			message.StartsWith("cleanup begin", StringComparison.Ordinal) ||
			message.StartsWith("cleanup end", StringComparison.Ordinal) ||
			message.StartsWith("cleanup_return_summary", StringComparison.Ordinal) ||
			message.StartsWith("cleanup_prisoner_return_summary", StringComparison.Ordinal) ||
			message.StartsWith("role_snapshot", StringComparison.Ordinal) ||
			message.StartsWith("role_restore", StringComparison.Ordinal) ||
			message.StartsWith("player_health_restore", StringComparison.Ordinal))
		{
			return true;
		}
		if (message.StartsWith("death_rate_diag", StringComparison.Ordinal))
		{
			return true;
		}
		if (message.StartsWith("prisoner_origin_diag", StringComparison.Ordinal))
		{
			return message.IndexOf("set_killed_end", StringComparison.Ordinal) >= 0 ||
				message.IndexOf("set_wounded_end", StringComparison.Ordinal) >= 0;
		}
		if (message.StartsWith("ready_troops_filter", StringComparison.Ordinal))
		{
			return message.IndexOf("dropped=0", StringComparison.Ordinal) < 0;
		}
		return false;
	}

	private static string GetInspectionLogPath()
	{
		if (!string.IsNullOrWhiteSpace(_inspectionLogPath))
		{
			return _inspectionLogPath;
		}
		string logDir = AnimusForgeModulePaths.GetLogsDirectory();
		Directory.CreateDirectory(logDir);
		_inspectionLogPath = Path.Combine(logDir, "TroopInspection.log");
		return _inspectionLogPath;
	}

	private sealed class TroopInspectionDummyPartyComponent : PartyComponent
	{
		private readonly CampaignVec2 _position;

		private readonly TextObject _name;

		private readonly Hero _owner;

		private readonly Clan _clan;

		public TroopInspectionDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
		{
			_position = position;
			_name = name;
			_owner = owner;
			_clan = clan;
		}

		public override Hero PartyOwner => _owner;

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
	}
	public sealed class TroopInspectionSaveableTypeDefiner : SaveableTypeDefiner
	{
		public TroopInspectionSaveableTypeDefiner()
			: base(711060)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(TroopInspectionDummyPartyComponent), 1);
		}
	}


	// Runtime roster split and cleanup helpers.
private const string HoldingDummyPartyPrefix = "animusforge_troop_inspection_holding_";

	// Cleanup helpers for the temporary holding party used while MainParty is split
	// down to the current inspection selection.
	private static bool IsInspectionHoldingDummyParty(MobileParty party)
	{
		try
		{
			return party?.StringId?.StartsWith(HoldingDummyPartyPrefix, StringComparison.Ordinal) == true;
		}
		catch
		{
			return false;
		}
	}

	private static void RestoreAndDestroyHoldingDummyParty(MobileParty holdingDummyParty, string label)
	{
		if (holdingDummyParty == null)
		{
			return;
		}
		MoveAllMembersBackToMainParty(holdingDummyParty, label);
		MoveAllPrisonersBackToMainParty(holdingDummyParty, label + "_prisoners");
		DestroyHoldingDummyParty(holdingDummyParty, label);
	}

	private static void DestroyHoldingDummyParty(MobileParty party, string label)
	{
		try
		{
			if (party == null)
			{
				return;
			}
			string id = party.StringId ?? "";
			if (party.IsActive && id.StartsWith(HoldingDummyPartyPrefix, StringComparison.Ordinal))
			{
				DestroyPartyAction.Apply((PartyBase)null, party);
				Log("holding_dummy_destroyed label=" + label + " id=" + id);
			}
		}
		catch (Exception ex)
		{
			Log("holding_dummy_destroy_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupOrphanHoldingDummyParties(MobileParty exceptParty, string label)
	{
		try
		{
			List<MobileParty> parties = new List<MobileParty>();
			foreach (MobileParty party in MobileParty.All)
			{
				if (party != null && !object.ReferenceEquals(party, exceptParty) && party.IsActive && IsInspectionHoldingDummyParty(party))
				{
					parties.Add(party);
				}
			}
			foreach (MobileParty party in parties)
			{
				RestoreAndDestroyHoldingDummyParty(party, label);
			}
		}
		catch (Exception ex)
		{
			Log("cleanup orphan holding failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private struct RosterTotals
	{
		public int Number;

		public int Wounded;

		public int Xp;

		public override string ToString()
		{
			return "number=" + Number + ",wounded=" + Wounded + ",xp=" + Xp;
		}
	}

	private static void PrepareSelectionRuntimeWithMainPartySplit(
		TroopInspectionRuntime runtime,
		bool reconcileExternalCastlePrisoners)
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

		runtime.RoleSnapshot = CaptureMainPartyRoleSnapshot("before_split");
		PrepareMainPartyRosterStateForInspection("prepare_runtime_snapshot");

		Dictionary<CharacterObject, RosterTotals> beforeMembers = BuildRosterTotals(mainParty.MemberRoster);
		Dictionary<CharacterObject, RosterTotals> beforePrisoners = BuildRosterTotals(PartyBase.MainParty?.PrisonRoster);
		int beforeMemberCount = mainParty.MemberRoster?.TotalManCount ?? 0;
		int beforePrisonerCount = PartyBase.MainParty?.PrisonRoster?.TotalManCount ?? 0;

		CreateInspectionHoldingDummyParty(runtime, mainParty);
		MoveMemberRosterFromMainParty(runtime.NotSelectedMemberRoster, runtime.HoldingDummyParty, "inspection_holding");
		MovePrisonerRosterFromMainParty(runtime.NotSelectedPrisonerRoster, runtime.HoldingDummyParty, "inspection_holding_prisoners");
		if (reconcileExternalCastlePrisoners)
		{
			ReconcileExternalCastlePrisonerRemainder(runtime, beforePrisoners);
		}

		RebuildTroopRosterCachedTotals(mainParty.MemberRoster, "prepare_runtime_main_after_split", throwOnFailure: true);
		RebuildTroopRosterCachedTotals(runtime.HoldingDummyParty?.MemberRoster, "prepare_runtime_holding_after_split", throwOnFailure: true);
		RebuildTroopRosterCachedTotals(PartyBase.MainParty?.PrisonRoster, "prepare_runtime_prisoners_after_split", throwOnFailure: true);
		RebuildTroopRosterCachedTotals(runtime.HoldingDummyParty?.PrisonRoster, "prepare_runtime_holding_prisoners_after_split", throwOnFailure: true);

		ValidateInspectionSplit(runtime, beforeMembers, beforePrisoners, beforeMemberCount, beforePrisonerCount);
		runtime.InspectionSummary = RosterSummary(runtime.InspectionRoster) + ", prisoners=" + RosterSummary(runtime.InspectionPrisonerRoster);
		runtime.NotSelectedSummary = RosterSummary(runtime.NotSelectedMemberRoster) + ", prisoners=" + RosterSummary(runtime.NotSelectedPrisonerRoster);
		Log("runtime_ready mainparty_split=true inspection=" + runtime.InspectionSummary
			+ " ui_not_selected=" + runtime.NotSelectedSummary
			+ " main_members=" + RosterSummary(mainParty.MemberRoster)
			+ " main_prisoners=" + RosterSummary(PartyBase.MainParty?.PrisonRoster)
			+ " holding_members=" + RosterSummary(runtime.HoldingDummyParty?.MemberRoster)
			+ " holding_prisoners=" + RosterSummary(runtime.HoldingDummyParty?.PrisonRoster));
	}

	private static void CreateInspectionHoldingDummyParty(TroopInspectionRuntime runtime, MobileParty mainParty)
	{
		CleanupOrphanHoldingDummyParties(null, "pre_split_holding_orphan");
		CampaignVec2 mainPosition = mainParty.Position;
		Vec2 direction = ResolveEncounterDirection(mainParty);
		Vec2 holdingOffset = new Vec2(-direction.Y, direction.X);
		if (holdingOffset.LengthSquared <= 0.0001f)
		{
			holdingOffset = new Vec2(0f, 1f);
		}
		CampaignVec2 holdingPosition = mainPosition + holdingOffset.Normalized() * 0.4f;
		string holdingId = HoldingDummyPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		runtime.HoldingDummyParty = MobileParty.CreateParty(holdingId, new TroopInspectionDummyPartyComponent(holdingPosition, new TextObject("AnimusForge Troop Inspection Holding"), Hero.MainHero, Clan.PlayerClan));
		if (runtime.HoldingDummyParty == null)
		{
			throw new InvalidOperationException("Failed to create inspection holding dummy party.");
		}
		runtime.HoldingDummyParty.IsVisible = false;
		runtime.HoldingDummyParty.SetMoveModeHold();
		Log("holding_dummy_create id=" + runtime.HoldingDummyParty.StringId + " pos=" + FormatCampaignVec2(runtime.HoldingDummyParty.Position) + " members=" + runtime.HoldingDummyParty.Party.NumberOfHealthyMembers);
	}

	private static void MoveMemberRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		if (selectedRoster == null || targetParty == null)
		{
			return;
		}
		MoveRosterResult result = new MoveRosterResult();
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			try
			{
				CharacterObject character = item.Character;
				if (character == null || item.Number <= 0 || character.IsPlayerCharacter)
				{
					continue;
				}
				if (character.IsHero)
				{
					MoveHeroMemberToParty(character.HeroObject, targetParty, label, result);
					continue;
				}
				MoveRegularMemberToParty(item, targetParty, result);
			}
			catch (Exception ex)
			{
				result.Errors++;
				Log("move_member_element_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
			}
		}
		Log("move_member_roster_result label=" + label + " " + result);
	}

	private static void MoveHeroMemberToParty(Hero hero, MobileParty targetParty, string label, MoveRosterResult result)
	{
		if (hero == null || targetParty == null || hero.IsHumanPlayerCharacter)
		{
			return;
		}
		if (hero.IsDead)
		{
			result.DeadHeroesSkipped++;
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		MobileParty beforeParty = hero.PartyBelongedTo;
		if (beforeParty != mainParty)
		{
			throw new InvalidOperationException("Hero party mismatch before inspection split. troop=" + SafeCharacterId(hero.CharacterObject) + " party=" + (beforeParty?.StringId ?? "null") + " main=" + (mainParty?.StringId ?? "null"));
		}
		AddHeroToPartyAction.Apply(hero, targetParty, showNotification: false);
		result.Heroes++;
		Log("move_member_hero_result label=" + label + " troop=" + SafeCharacterId(hero.CharacterObject) + " from=" + (beforeParty?.StringId ?? "null") + " to=" + (hero.PartyBelongedTo?.StringId ?? "null") + " target=" + (targetParty.StringId ?? "null"));
	}

	private static void MoveRegularMemberToParty(TroopRosterElement selectedElement, MobileParty targetParty, MoveRosterResult result)
	{
		TroopRoster mainRoster = MobileParty.MainParty?.MemberRoster;
		TroopRoster targetRoster = targetParty?.MemberRoster;
		CharacterObject character = selectedElement.Character;
		if (mainRoster == null || targetRoster == null || character == null)
		{
			throw new InvalidOperationException("Invalid roster while moving regular inspection member.");
		}
		int sourceIndex = mainRoster.FindIndexOfTroop(character);
		if (sourceIndex < 0)
		{
			throw new InvalidOperationException("Source member not found in MainParty: " + SafeCharacterId(character));
		}
		TroopRosterElement sourceElement = GetFreshRosterElementCopy(mainRoster, sourceIndex);
		int number = Math.Max(0, selectedElement.Number);
		int healthyAvailable = Math.Max(0, sourceElement.Number - sourceElement.WoundedNumber);
		if (healthyAvailable < number)
		{
			throw new InvalidOperationException("Not enough healthy source members for " + SafeCharacterId(character) + ". have=" + healthyAvailable + " need=" + number);
		}
		int xp = CalculateRosterXpToMove(sourceElement, number);
		mainRoster.AddToCounts(character, -number, insertAtFront: false, woundedCount: 0, xpChange: -xp, removeDepleted: true, index: -1);
		targetRoster.AddToCounts(character, number, insertAtFront: false, woundedCount: 0, xpChange: xp, removeDepleted: true, index: -1);
		result.RegularMen += number;
		result.RegularXp += xp;
	}

	private static void MovePrisonerRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		if (selectedRoster == null || targetParty == null)
		{
			return;
		}
		TroopRoster mainRoster = PartyBase.MainParty?.PrisonRoster;
		TroopRoster targetRoster = targetParty.PrisonRoster;
		MoveRosterResult result = new MoveRosterResult();
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			try
			{
				CharacterObject character = item.Character;
				if (mainRoster == null || targetRoster == null || character == null || item.Number <= 0)
				{
					continue;
				}
				int sourceIndex = mainRoster.FindIndexOfTroop(character);
				if (sourceIndex < 0)
				{
					throw new InvalidOperationException("Source prisoner not found in MainParty: " + SafeCharacterId(character));
				}
				TroopRosterElement sourceElement = GetFreshRosterElementCopy(mainRoster, sourceIndex);
				int number = Math.Max(0, item.Number);
				int wounded = Math.Min(number, Math.Max(0, item.WoundedNumber));
				int xp = CalculateRosterXpToMove(sourceElement, number);
				if (sourceElement.Number < number)
				{
					throw new InvalidOperationException("Not enough source prisoners for " + SafeCharacterId(character) + ". have=" + sourceElement.Number + " need=" + number);
				}
				mainRoster.AddToCounts(character, -number, insertAtFront: false, woundedCount: -wounded, xpChange: -xp, removeDepleted: true, index: -1);
				targetRoster.AddToCounts(character, number, insertAtFront: false, woundedCount: wounded, xpChange: xp, removeDepleted: true, index: -1);
				if (character.IsHero)
				{
					result.Heroes += number;
				}
				else
				{
					result.RegularMen += number;
					result.RegularWounded += wounded;
					result.RegularXp += xp;
				}
			}
			catch (Exception ex)
			{
				result.Errors++;
				Log("move_prisoner_element_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
			}
		}
		Log("move_prisoner_roster_result label=" + label + " " + result);
	}

	private static void ReconcileExternalCastlePrisonerRemainder(
		TroopInspectionRuntime runtime,
		Dictionary<CharacterObject, RosterTotals> originalTotals)
	{
		TroopRoster mainRoster = PartyBase.MainParty?.PrisonRoster;
		TroopRoster holdingRoster = runtime?.HoldingDummyParty?.PrisonRoster;
		if (mainRoster == null || holdingRoster == null)
		{
			throw new InvalidOperationException("Castle prisoner rosters are unavailable after inspection split.");
		}

		Dictionary<CharacterObject, RosterTotals> mainTotals = BuildRosterTotals(mainRoster);
		Dictionary<CharacterObject, RosterTotals> holdingTotals = BuildRosterTotals(holdingRoster);
		int adjustedStacks = 0;
		foreach (KeyValuePair<CharacterObject, RosterTotals> pair in originalTotals ?? new Dictionary<CharacterObject, RosterTotals>())
		{
			CharacterObject character = pair.Key;
			RosterTotals original = pair.Value;
			mainTotals.TryGetValue(character, out RosterTotals main);
			holdingTotals.TryGetValue(character, out RosterTotals holding);
			if (main.Number + holding.Number != original.Number)
			{
				throw new InvalidOperationException("Castle prisoner partition count mismatch for " + SafeCharacterId(character)
					+ ". original=" + original.Number + " main=" + main.Number + " holding=" + holding.Number);
			}

			int targetMainWounded = SiegeCastleRosterSelectionProfile.ResolveMainStackWounded(
				original.Number,
				original.Wounded,
				main.Number,
				main.Wounded);
			int targetHoldingWounded = Math.Max(0, original.Wounded - targetMainWounded);
			int targetMainXp = character?.IsHero == true
				? main.Xp
				: (main.Number <= 0 ? 0 : SiegeCastleRosterSelectionProfile.ResolveMainStackXp(original.Xp, holding.Xp));
			int targetHoldingXp = character?.IsHero == true
				? holding.Xp
				: Math.Max(0, original.Xp - targetMainXp);

			int mainIndex = mainRoster.FindIndexOfTroop(character);
			int holdingIndex = holdingRoster.FindIndexOfTroop(character);
			int mainWoundedDelta = targetMainWounded - main.Wounded;
			int mainXpDelta = character?.IsHero == true ? 0 : targetMainXp - main.Xp;
			int holdingWoundedDelta = targetHoldingWounded - holding.Wounded;
			int holdingXpDelta = character?.IsHero == true ? 0 : targetHoldingXp - holding.Xp;
			if ((mainWoundedDelta != 0 || mainXpDelta != 0) && mainIndex < 0)
			{
				throw new InvalidOperationException("Castle main prisoner stack missing during reconciliation for " + SafeCharacterId(character));
			}
			if ((holdingWoundedDelta != 0 || holdingXpDelta != 0) && holdingIndex < 0)
			{
				throw new InvalidOperationException("Castle holding prisoner stack missing during reconciliation for " + SafeCharacterId(character));
			}

			if (mainWoundedDelta != 0 || mainXpDelta != 0)
			{
				mainRoster.AddToCountsAtIndex(mainIndex, 0, mainWoundedDelta, mainXpDelta, removeDepleted: true);
				adjustedStacks++;
			}
			if (holdingWoundedDelta != 0 || holdingXpDelta != 0)
			{
				holdingRoster.AddToCountsAtIndex(holdingIndex, 0, holdingWoundedDelta, holdingXpDelta, removeDepleted: true);
				adjustedStacks++;
			}
			if (mainWoundedDelta != 0 || mainXpDelta != 0 || holdingWoundedDelta != 0 || holdingXpDelta != 0)
			{
				Log("split_validate_reconcile troop=" + SafeCharacterId(character)
					+ " main=" + main + "->number=" + main.Number + ",wounded=" + targetMainWounded + ",xp=" + targetMainXp
					+ " holding=" + holding + "->number=" + holding.Number + ",wounded=" + targetHoldingWounded + ",xp=" + targetHoldingXp
					+ " original=" + original
					+ " adjusted_stacks=" + adjustedStacks);
			}
		}
		ValidateRosterTotals(
			"castle_reconciled_prisoners",
			originalTotals,
			BuildCombinedRosterTotals(mainRoster, holdingRoster));
		Log("split_validate_reconcile_complete adjusted_stacks=" + adjustedStacks
			+ " main=" + RosterSummary(mainRoster)
			+ " holding=" + RosterSummary(holdingRoster));
	}

	private static void ValidateInspectionSplit(TroopInspectionRuntime runtime, Dictionary<CharacterObject, RosterTotals> beforeMembers, Dictionary<CharacterObject, RosterTotals> beforePrisoners, int beforeMemberCount, int beforePrisonerCount)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			throw new InvalidOperationException("MainParty missing after inspection split.");
		}
		CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
		if (playerCharacter != null && !mainParty.MemberRoster.Contains(playerCharacter))
		{
			throw new InvalidOperationException("Player is not in MainParty after inspection split.");
		}
		int afterMemberCount = (mainParty.MemberRoster?.TotalManCount ?? 0) + (runtime.HoldingDummyParty?.MemberRoster?.TotalManCount ?? 0);
		int afterPrisonerCount = (PartyBase.MainParty?.PrisonRoster?.TotalManCount ?? 0) + (runtime.HoldingDummyParty?.PrisonRoster?.TotalManCount ?? 0);
		if (afterMemberCount != beforeMemberCount)
		{
			throw new InvalidOperationException("Total member count mismatch after inspection split. before=" + beforeMemberCount + " after=" + afterMemberCount);
		}
		if (afterPrisonerCount != beforePrisonerCount)
		{
			throw new InvalidOperationException("Total prisoner count mismatch after inspection split. before=" + beforePrisonerCount + " after=" + afterPrisonerCount);
		}
		ValidateRosterTotals("members", beforeMembers, BuildCombinedRosterTotals(mainParty.MemberRoster, runtime.HoldingDummyParty?.MemberRoster));
		ValidateRosterTotals("prisoners", beforePrisoners, BuildCombinedRosterTotals(PartyBase.MainParty?.PrisonRoster, runtime.HoldingDummyParty?.PrisonRoster));
		ValidateMainRosterMatchesInspectionSelection(runtime);
		Log("split_validate_ok members=" + afterMemberCount
			+ " prisoners=" + afterPrisonerCount
			+ " main=" + RosterSummary(mainParty.MemberRoster)
			+ " main_prisoners=" + RosterSummary(PartyBase.MainParty?.PrisonRoster)
			+ " holding=" + RosterSummary(runtime.HoldingDummyParty?.MemberRoster)
			+ " holding_prisoners=" + RosterSummary(runtime.HoldingDummyParty?.PrisonRoster));
	}

	private static void ValidateRosterTotals(string label, Dictionary<CharacterObject, RosterTotals> before, Dictionary<CharacterObject, RosterTotals> after)
	{
		before = before ?? new Dictionary<CharacterObject, RosterTotals>();
		after = after ?? new Dictionary<CharacterObject, RosterTotals>();
		foreach (KeyValuePair<CharacterObject, RosterTotals> pair in before)
		{
			if (!after.TryGetValue(pair.Key, out RosterTotals afterTotals))
			{
				throw new InvalidOperationException("Character missing after inspection split " + label + ": " + SafeCharacterId(pair.Key));
			}
			RosterTotals beforeTotals = pair.Value;
			bool totalsMatch = pair.Key?.IsHero == true
				? beforeTotals.Number == afterTotals.Number && beforeTotals.Wounded == afterTotals.Wounded
				: beforeTotals.Number == afterTotals.Number && beforeTotals.Wounded == afterTotals.Wounded && beforeTotals.Xp == afterTotals.Xp;
			if (!totalsMatch)
			{
				throw new InvalidOperationException("Roster totals mismatch after inspection split " + label + " for " + SafeCharacterId(pair.Key) + ". before=" + beforeTotals + " after=" + afterTotals);
			}
		}
		foreach (KeyValuePair<CharacterObject, RosterTotals> pair in after)
		{
			if (!before.ContainsKey(pair.Key))
			{
				throw new InvalidOperationException("Unexpected character after inspection split " + label + ": " + SafeCharacterId(pair.Key));
			}
			if (pair.Key != null && pair.Key.IsHero && pair.Value.Number != 1)
			{
				throw new InvalidOperationException("Hero duplicated after inspection split " + label + ": " + SafeCharacterId(pair.Key));
			}
		}
	}

	private static void ValidateMainRosterMatchesInspectionSelection(TroopInspectionRuntime runtime)
	{
		Dictionary<CharacterObject, int> expectedMembers = BuildRosterCounts(runtime.InspectionRoster, healthyOnly: true);
		Dictionary<CharacterObject, int> actualMembers = BuildRosterCounts(MobileParty.MainParty?.MemberRoster, healthyOnly: true);
		ValidateExpectedCounts("main_members", expectedMembers, actualMembers, allowExtraPlayerOnly: true);
		Dictionary<CharacterObject, int> expectedPrisoners = BuildRosterCounts(runtime.InspectionPrisonerRoster, healthyOnly: false);
		Dictionary<CharacterObject, int> actualPrisoners = BuildRosterCounts(PartyBase.MainParty?.PrisonRoster, healthyOnly: false);
		ValidateExpectedCounts("main_prisoners", expectedPrisoners, actualPrisoners, allowExtraPlayerOnly: false);
	}

	private static void ValidateExpectedCounts(string label, Dictionary<CharacterObject, int> expected, Dictionary<CharacterObject, int> actual, bool allowExtraPlayerOnly)
	{
		expected = expected ?? new Dictionary<CharacterObject, int>();
		actual = actual ?? new Dictionary<CharacterObject, int>();
		foreach (KeyValuePair<CharacterObject, int> pair in expected)
		{
			actual.TryGetValue(pair.Key, out int actualCount);
			if (actualCount != pair.Value)
			{
				throw new InvalidOperationException("Inspection split count mismatch " + label + " troop=" + SafeCharacterId(pair.Key) + " expected=" + pair.Value + " actual=" + actualCount);
			}
		}
		foreach (KeyValuePair<CharacterObject, int> pair in actual)
		{
			if (pair.Value <= 0)
			{
				continue;
			}
			if (allowExtraPlayerOnly && pair.Key != null && pair.Key.IsPlayerCharacter)
			{
				continue;
			}
			if (!expected.ContainsKey(pair.Key))
			{
				throw new InvalidOperationException("Inspection split unexpected " + label + " troop=" + SafeCharacterId(pair.Key) + " count=" + pair.Value);
			}
		}
	}

	private static Dictionary<CharacterObject, int> BuildRosterCounts(TroopRoster roster, bool healthyOnly)
	{
		Dictionary<CharacterObject, int> result = new Dictionary<CharacterObject, int>();
		if (roster == null)
		{
			return result;
		}
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			int count = healthyOnly && !character.IsHero ? Math.Max(0, item.Number - item.WoundedNumber) : Math.Max(0, item.Number);
			if (character.IsHero && healthyOnly && (character.HeroObject?.IsDead == true || character.HeroObject?.IsWounded == true))
			{
				count = 0;
			}
			if (count <= 0)
			{
				continue;
			}
			result.TryGetValue(character, out int existing);
			result[character] = existing + count;
		}
		return result;
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
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
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

	internal static void LogMissionSourceDiag(string source)
	{
		try
		{
			Log("mission_source_diag source=" + source
				+ " runtime_inspection=" + RosterSummary(_runtime?.InspectionRoster)
				+ " runtime_prisoners=" + RosterSummary(_runtime?.InspectionPrisonerRoster)
				+ " main_members=" + RosterSummary(MobileParty.MainParty?.MemberRoster)
				+ " main_prisoners=" + RosterSummary(PartyBase.MainParty?.PrisonRoster)
				+ " holding_members=" + RosterSummary(_runtime?.HoldingDummyParty?.MemberRoster)
				+ " holding_prisoners=" + RosterSummary(_runtime?.HoldingDummyParty?.PrisonRoster)
				+ " dummy_members=" + RosterSummary(_dummyParty?.MemberRoster)
				+ " dummy_prisoners=" + RosterSummary(_dummyParty?.PrisonRoster));
		}
		catch (Exception ex)
		{
			Log("mission_source_diag failed source=" + source + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

}



internal static class ReinforcementSystemCompatibility
{
	private const string HarmonyId = "com.AnimusForge.spy.reinforcement_guard";

	private const string MainTypeName = "Reinforcement_System.Main";

	private const string FieldCoreTypeName = "Reinforcement_System.RS_Core_Field";

	private const string SiegeCoreTypeName = "Reinforcement_System.RS_Core_Siege";

	private static readonly string[] CoreMethodNames =
	{
		"AfterStart",
		"OnMissionModeChange",
		"OnMissionTick",
		"OnTeamDeployed",
		"OnAgentRemoved",
		"OnEarlyAgentRemoved",
		"OnAgentPanicked",
		"OnMissionResultReady",
		"OnEndMission"
	};

	private static readonly object PatchLock = new object();

	private static Harmony _harmony;

	private static bool _patched;

	private static bool _missingLogged;

	private static int _coreSuppressedLogCount;

	internal static void EnsurePatched(Harmony harmony = null)
	{
		if (_patched)
		{
			return;
		}
		lock (PatchLock)
		{
			if (_patched)
			{
				return;
			}
			if (harmony != null)
			{
				_harmony = harmony;
			}
			try
			{
				Type targetType = FindType(MainTypeName);
				if (targetType == null)
				{
					LogMissingOnce(MainTypeName + " not loaded");
					return;
				}
				MethodInfo target = AccessTools.Method(targetType, "OnMissionBehaviorInitialize", new Type[] { typeof(Mission) });
				MethodInfo prefix = AccessTools.Method(typeof(ReinforcementSystemCompatibility), nameof(OnMissionBehaviorInitializePrefix));
				if (target == null || prefix == null)
				{
					LogMissingOnce(MainTypeName + ".OnMissionBehaviorInitialize not found");
					return;
				}
				Harmony activeHarmony = _harmony ?? new Harmony(HarmonyId);
				activeHarmony.Patch(target, prefix: new HarmonyMethod(prefix));
				int corePatchCount = PatchCoreBehaviorType(activeHarmony, FieldCoreTypeName);
				corePatchCount += PatchCoreBehaviorType(activeHarmony, SiegeCoreTypeName);
				_harmony = activeHarmony;
				_patched = true;
				Log("reinforcement_system_guard_patched core_methods=" + corePatchCount);
			}
			catch (Exception ex)
			{
				Log("reinforcement_system_guard_patch_failed " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	private static bool OnMissionBehaviorInitializePrefix(Mission mission)
	{
		if (!TryGetSuppressionReason(mission, out string reason))
		{
			return true;
		}
		Log("reinforcement_system_skipped mission=" + (mission != null) + " reason=" + reason);
		return false;
	}

	private static bool OnCoreBehaviorPrefix(object __instance)
	{
		Mission mission = null;
		try
		{
			mission = (__instance as MissionBehavior)?.Mission ?? Mission.Current;
		}
		catch
		{
			mission = Mission.Current;
		}
		if (!TryGetSuppressionReason(mission, out string reason))
		{
			return true;
		}
		_coreSuppressedLogCount++;
		if (_coreSuppressedLogCount <= 8)
		{
			string typeName = "null";
			try
			{
				typeName = __instance?.GetType().FullName ?? "null";
			}
			catch
			{
			}
			Log("reinforcement_system_core_skipped type=" + typeName + " reason=" + reason + " count=" + _coreSuppressedLogCount);
		}
		return false;
	}

	internal static int RemoveReinforcementMissionBehaviors(Mission mission, string reason)
	{
		if (mission?.MissionBehaviors == null)
		{
			return 0;
		}
		int removed = 0;
		try
		{
			List<MissionBehavior> toRemove = new List<MissionBehavior>();
			foreach (MissionBehavior behavior in mission.MissionBehaviors)
			{
				if (IsReinforcementCoreBehavior(behavior))
				{
					toRemove.Add(behavior);
				}
			}
			foreach (MissionBehavior behavior2 in toRemove)
			{
				try
				{
					mission.RemoveMissionBehavior(behavior2);
					removed++;
				}
				catch (Exception ex)
				{
					Log("reinforcement_system_remove_failed type=" + (behavior2?.GetType().FullName ?? "null") + " reason=" + (reason ?? "") + " error=" + ex.GetType().Name + ": " + ex.Message);
				}
			}
			if (removed > 0)
			{
				Log("reinforcement_system_behaviors_removed count=" + removed + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex2)
		{
			Log("reinforcement_system_remove_scan_failed reason=" + (reason ?? "") + " error=" + ex2.GetType().Name + ": " + ex2.Message);
		}
		return removed;
	}

	private static int PatchCoreBehaviorType(Harmony activeHarmony, string typeName)
	{
		Type type = FindType(typeName);
		if (type == null)
		{
			return 0;
		}
		MethodInfo prefix = AccessTools.Method(typeof(ReinforcementSystemCompatibility), nameof(OnCoreBehaviorPrefix));
		if (prefix == null)
		{
			return 0;
		}
		int count = 0;
		try
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			foreach (MethodInfo method in methods)
			{
				if (method == null || Array.IndexOf(CoreMethodNames, method.Name) < 0)
				{
					continue;
				}
				try
				{
					activeHarmony.Patch(method, prefix: new HarmonyMethod(prefix));
					count++;
				}
				catch (Exception ex)
				{
					Log("reinforcement_system_core_patch_failed type=" + typeName + " method=" + method.Name + " error=" + ex.GetType().Name + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Log("reinforcement_system_core_patch_scan_failed type=" + typeName + " error=" + ex2.GetType().Name + ": " + ex2.Message);
		}
		return count;
	}

	private static bool TryGetSuppressionReason(Mission mission, out string reason)
	{
		reason = "";
		try
		{
			if (TroopInspectionBehavior.ShouldSuppressReinforcementSystem(mission))
			{
				reason = "troop_inspection";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (DuelBehavior.ShouldSuppressReinforcementSystem(mission))
			{
				reason = "wilderness_duel";
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsReinforcementCoreBehavior(MissionBehavior behavior)
	{
		if (behavior == null)
		{
			return false;
		}
		try
		{
			string fullName = behavior.GetType().FullName ?? "";
			return string.Equals(fullName, FieldCoreTypeName, StringComparison.Ordinal) || string.Equals(fullName, SiegeCoreTypeName, StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static Type FindType(string typeName)
	{
		Type type = AccessTools.TypeByName(typeName);
		if (type != null)
		{
			return type;
		}
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				type = assembly.GetType(typeName, throwOnError: false);
				if (type != null)
				{
					return type;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void LogMissingOnce(string message)
	{
		if (_missingLogged)
		{
			return;
		}
		_missingLogged = true;
		Log("reinforcement_system_guard_not_active " + message);
	}

	private static void Log(string message)
	{
		Logger.Log("TroopInspection", "[TroopInspection] " + message);
		Logger.LogEvent("TroopInspection", "reinforcement_guard " + message);
	}
}

internal sealed class TroopInspectionMissionLogic : MissionLogic
{
	private readonly string _dummyPartyStringId;

	private readonly TroopRoster _inspectionMemberRoster;

	private readonly TroopRoster _inspectionPrisonerRoster;

	private readonly Action<Agent, bool> _externalPrisonerSpawned;

	private readonly Action<int, int, int> _externalPrisonerSpawnCompleted;

	private readonly Action<string> _externalCleanup;

	private readonly bool _externalCastleRuntime;

	private BattleEndLogic _battleEndLogic;

	private bool _battleEndDisabled;

	private bool _deploymentWasActive;

	private bool _deploymentEndDetected;

	private bool _inspectionMessageShown;

	private bool _cleanupRequested;

	private bool _agentCountsLogged;

	private bool _enemyAgentWarningLogged;

	private float _continuousRefreshTimer;

	private const float RefreshInterval = 0.12f;

	private const float RefreshRadius = 30f;

	private const bool RefreshAllPlayerAgents = true;

	private bool _conversationStateLogged;

	private bool _firstRefreshLogged;

	private float _nextBattleEndDisableRetryTime = 1f;

	private MissionMode _lastMissionMode;

	private bool _prisonersSpawned;

	private bool _alliesPrepared;

	private float _allyPrepareTimer = 1f;

	private float _prisonerSpawnTimer = 1f;

	private readonly Dictionary<Agent, bool> _prisonerIsLordMap = new Dictionary<Agent, bool>();

	private readonly HashSet<Agent> _civilianPrisonerActionSetApplied = new HashSet<Agent>();

	private readonly Dictionary<Agent, float> _prisonerPoseSuppressedUntil = new Dictionary<Agent, float>();

	private readonly HashSet<Agent> _prisonerPoseApplied = new HashSet<Agent>();

	private TroopInspectionPrisonerSlaughterRuntime _prisonerSlaughterRuntime;

	private ActionIndexCache _lordPrisonerAction;

	private ActionIndexCache _soldierPrisonerAction;

	private bool _prisonerActionsCached;

	private bool _lordPrisonerActionMissingLogged;

	private bool _soldierPrisonerActionMissingLogged;

	private bool _prisonerActionSetRejectedLogged;

	private float _prisonerPoseRefreshTimer;

	private const float PrisonerPoseRefreshInterval = 0.35f;

	private const float PrisonerPoseStartProgress = 0.35f;

	private const float PrisonerPoseActionSpeed = 0f;

	private string _lastMissionEndedLogState = "";

	private float _nextMissionEndedLogTime;

	private const FormationClass RegularPrisonerFormationClass = (FormationClass)6;

	private const FormationClass LordPrisonerFormationClass = (FormationClass)7;

	private const FormationClass LordPrisonerRuntimeClass = FormationClass.Cavalry;

	private static readonly PropertyInfo FormationRepresentativeClassProperty = typeof(Formation).GetProperty("RepresentativeClass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly FieldInfo FormationLogicalClassField = typeof(Formation).GetField("_logicalClass", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo FormationLogicalClassNeedsUpdateField = typeof(Formation).GetField("_logicalClassNeedsUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

	private int _lordFormationClassForceLogCount;

	private int _prisonerDeployPairDiagLogCount;

	private float _nextPrisonerDeployPairDiagTime;

	private int _formationIsolationLogCount;

	private int _prisonerFormationRecalcLogCount;

	private int _prisonerSpawnWaitLogCount;

	private int _agentRemovedDiagLogCount;

	private const int PrisonerDeployPairDiagLogLimit = 6;

	private const int FormationIsolationLogLimit = 3;

	private const int PrisonerFormationRecalcLogLimit = 3;

	private const int PrisonerSpawnWaitDiagLimit = 6;

	private const int AgentRemovedDiagLogLimit = 40;

	public TroopInspectionMissionLogic(string dummyPartyStringId)
		: this(dummyPartyStringId, null)
	{
	}

	public TroopInspectionMissionLogic(string dummyPartyStringId, TroopRoster inspectionPrisonerRoster)
		: this(dummyPartyStringId, null, inspectionPrisonerRoster, null, null, null, false)
	{
	}

	internal TroopInspectionMissionLogic(
		string dummyPartyStringId,
		TroopRoster inspectionMemberRoster,
		TroopRoster inspectionPrisonerRoster,
		Action<Agent, bool> externalPrisonerSpawned,
		Action<int, int, int> externalPrisonerSpawnCompleted,
		Action<string> externalCleanup)
		: this(dummyPartyStringId, inspectionMemberRoster, inspectionPrisonerRoster, externalPrisonerSpawned, externalPrisonerSpawnCompleted, externalCleanup, true)
	{
	}

	private TroopInspectionMissionLogic(
		string dummyPartyStringId,
		TroopRoster inspectionMemberRoster,
		TroopRoster inspectionPrisonerRoster,
		Action<Agent, bool> externalPrisonerSpawned,
		Action<int, int, int> externalPrisonerSpawnCompleted,
		Action<string> externalCleanup,
		bool externalCastleRuntime)
	{
		_dummyPartyStringId = dummyPartyStringId;
		_inspectionMemberRoster = inspectionMemberRoster != null ? TroopInspectionBehavior.CloneRoster(inspectionMemberRoster) : null;
		_inspectionPrisonerRoster = inspectionPrisonerRoster != null ? TroopInspectionBehavior.CloneRoster(inspectionPrisonerRoster) : null;
		_externalPrisonerSpawned = externalPrisonerSpawned;
		_externalPrisonerSpawnCompleted = externalPrisonerSpawnCompleted;
		_externalCleanup = externalCleanup;
		_externalCastleRuntime = externalCastleRuntime;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		CacheBattleEndLogic();
		TryDisableBattleEndLogic("OnBehaviorInitialize");
		_lastMissionMode = base.Mission?.Mode ?? MissionMode.StartUp;
		_deploymentWasActive = _lastMissionMode == MissionMode.Deployment;
		Log($"init deployment_active={_deploymentWasActive} mode={_lastMissionMode} battle_end_logic_cached={_battleEndLogic != null}");
		Log($"mission_behaviors deployment_handler={TroopInspectionBehavior.HasMissionBehavior(base.Mission, "BattleDeploymentHandler")} deployment_controller={base.Mission?.GetMissionBehavior<BattleDeploymentMissionController>() != null} battle_end_logic={_battleEndLogic != null}");
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
		Log($"after_start deployment_active={_deploymentWasActive} mode={_lastMissionMode} battle_end_disabled={_battleEndDisabled}");
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		RetryBattleEndDisableIfNeeded();
		TryPrepareExternalAllies(dt);
		bool hasPrisonersToSpawn = HasPrisonersToSpawn();
		if (!_prisonersSpawned && hasPrisonersToSpawn && !_deploymentWasActive && _prisonerSpawnWaitLogCount < PrisonerSpawnWaitDiagLimit)
		{
			_prisonerSpawnWaitLogCount++;
			Log("spawn_wait_diag sample=" + _prisonerSpawnWaitLogCount + " reason=deployment_not_active mode=" + (base.Mission?.Mode.ToString() ?? "null") + " last_mode=" + _lastMissionMode + " timer=" + _prisonerSpawnTimer + " roster=" + PrisonerRosterDiag(GetPrisonerRosterForSpawn()) + " main_roster=" + PrisonerRosterDiag(PartyBase.MainParty?.PrisonRoster));
		}
		if (!_prisonersSpawned && hasPrisonersToSpawn)
		{
			_prisonerSpawnTimer -= dt;
			if (_prisonerSpawnTimer <= 0f)
			{
				if (_deploymentWasActive || IsDeploymentCurrentlyActive())
				{
					SpawnPrisoners();
				}
				else if (CanSpawnPrisonersNow())
				{
					Log("spawn_prisoners_direct reason=deployment_not_active mode=" + (base.Mission?.Mode.ToString() ?? "null") + " last_mode=" + _lastMissionMode + " current_time=" + (base.Mission?.CurrentTime.ToString("0.00") ?? "null") + " roster=" + PrisonerRosterDiag(GetPrisonerRosterForSpawn()));
					SpawnPrisoners();
					if (_prisonersSpawned)
					{
						MarkDeploymentBypassedAfterDirectPrisonerSpawn();
					}
				}
			}
		}
		if (_prisonersSpawned && !_deploymentEndDetected)
		{
			ForceLordPrisonerFormationClass("deployment_tick");
			EnsurePrisonerFormationsIsolated("deployment_tick");
			TryRecalculateLordPrisonerFormationWidth("deployment_tick", onlyIfAnomalous: true);
			TryLogPrisonerDeployPairDiag();
		}
		DetectDeploymentEnd();
		TryLogAgentCounts();
		_prisonerSlaughterRuntime?.Tick(dt);
		if (!_externalCastleRuntime && _prisonerSlaughterRuntime?.IsBusy != true)
		{
			RefreshPrisonerPoses(dt);
		}
		ContinuousAgentRefresh(dt);
		if (!_inspectionMessageShown && _deploymentEndDetected && base.Mission != null && base.Mission.CurrentTime > 2f)
		{
			_inspectionMessageShown = true;
			AnimusForgeQuickInfo.Show(_externalCastleRuntime
				? "城堡处置：可用原版指挥系统调整士兵与俘虏站位。按TAB结束处置。"
				: "检阅模式：可自由指挥部队进行检阅。按TAB撤退结束检阅。");
			Log("inspection_message_shown");
		}
	}

	private TroopRoster GetPrisonerRosterForSpawn()
	{
		return _inspectionPrisonerRoster ?? PartyBase.MainParty?.PrisonRoster;
	}

	private void TryPrepareExternalAllies(float dt)
	{
		if (_alliesPrepared || !_externalCastleRuntime || _inspectionMemberRoster == null || _inspectionMemberRoster.TotalManCount <= 0)
		{
			return;
		}

		_allyPrepareTimer -= dt;
		Mission mission = base.Mission;
		if (_allyPrepareTimer > 0f || mission?.PlayerTeam == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}

		_alliesPrepared = true;
		PrepareExternalAllies();
	}

	private void PrepareExternalAllies()
	{
		Mission mission = base.Mission;
		Team playerTeam = mission?.PlayerTeam;
		Agent main = Agent.Main ?? mission?.MainAgent;
		PartyBase mainParty = PartyBase.MainParty;
		if (mission == null || playerTeam == null || main == null || mainParty == null)
		{
			Log("prepare_external_allies aborted: mission context unavailable");
			return;
		}

		List<CharacterObject> selected = ExpandExternalAllies();
		Dictionary<CharacterObject, Queue<Agent>> existingByCharacter = mission.Agents
			.Where(agent => agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& agent != main
				&& agent.Team == playerTeam
				&& !(agent.Origin is PrisonerAgentOrigin)
				&& agent.Character is CharacterObject)
			.GroupBy(agent => (CharacterObject)agent.Character)
			.ToDictionary(group => group.Key, group => new Queue<Agent>(group));
		FormationClass alliedFormationClass = ResolveExternalAllyFormationClass();
		List<Agent> preparedAgents = new List<Agent>();
		int reused = 0;
		int spawned = 0;
		int failed = 0;
		for (int selectedIndex = 0; selectedIndex < selected.Count; selectedIndex++)
		{
			CharacterObject character = selected[selectedIndex];

			Agent agent = null;
			if (existingByCharacter.TryGetValue(character, out Queue<Agent> existing) && existing.Count > 0)
			{
				agent = existing.Dequeue();
				reused++;
			}
			else
			{
				PartyBase originParty = CastleAftermathArmyRosterRuntimeBridge.ResolveAgentOriginParty(character, mainParty);
				agent = BannerlordApiCompat.SpawnInspectionTroop(
					mission,
					new PartyAgentOrigin(originParty, character),
					selected.Count,
					selectedIndex,
					alliedFormationClass,
					wieldInitialWeapons: true);
				if (agent != null)
				{
					spawned++;
				}
			}

			if (agent == null)
			{
				failed++;
				continue;
			}
			PrepareExternalAllyAgent(agent, selectedIndex, main, alliedFormationClass);
			preparedAgents.Add(agent);
		}

		HoldPreparedAlliedFormations(preparedAgents);
		int bannerBearers = SiegeAiInterventionBehavior.EnsureInterventionBannerBearersForExternal(
			mission,
			SiegeBannerBearerProfile.SpawnSource);
		bool commandUiReady = SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(
			mission,
			SiegeCastleRosterSelectionProfile.AlliedCommandUiRefreshSource);
		Log("prepare_external_allies result selected=" + selected.Count
			+ " reused=" + reused
			+ " spawned=" + spawned
			+ " active=" + preparedAgents.Count
			+ " failed=" + failed
			+ " allied_formation=" + (int)alliedFormationClass
			+ " banner_bearers=" + bannerBearers
			+ " mission_agents=" + (mission.Agents?.Count ?? 0)
			+ " command_ui=" + commandUiReady);
		Logger.LogEvent("TroopInspection", "castle_allies result selected=" + selected.Count
			+ " reused=" + reused + " spawned=" + spawned + " active=" + preparedAgents.Count + " failed=" + failed);
		AnimusForgeQuickInfo.Show(SiegeCastleRosterSelectionProfile.BuildAlliedSceneReadyMessage(selected.Count, preparedAgents.Count));
	}

	private List<CharacterObject> ExpandExternalAllies()
	{
		List<CharacterObject> result = new List<CharacterObject>();
		foreach (TroopRosterElement element in TroopInspectionBehavior.SnapshotRoster(_inspectionMemberRoster))
		{
			CharacterObject character = element.Character;
			if (character == null || character.IsPlayerCharacter || element.Number <= 0)
			{
				continue;
			}
			for (int index = 0; index < element.Number && result.Count < SiegeCastleRosterSelectionProfile.MaxAlliedTroops; index++)
			{
				result.Add(character);
			}
		}
		return result;
	}

	private static FormationClass ResolveExternalAllyFormationClass()
	{
		FormationClass formationClass = (FormationClass)SiegeCastleRosterSelectionProfile.AlliedFormationClassIndex;
		return formationClass >= FormationClass.Infantry && formationClass < FormationClass.NumberOfRegularFormations
			? formationClass
			: FormationClass.Infantry;
	}

	private void PrepareExternalAllyAgent(Agent agent, int selectedIndex, Agent main, FormationClass formationClass)
	{
		try
		{
			Vec3 position = ResolveExternalAllyAssemblyPosition(main, selectedIndex);
			agent.TeleportToPosition(position);
			agent.LookDirection = main.LookDirection;
			agent.SetMortalityState(Agent.MortalityState.Immortal);
			agent.SetIsAIPaused(isPaused: false);
			agent.DisableScriptedMovement();
			SiegeAiInterventionBehavior.EnsureAgentPlayerCommandableForExternal(
				agent,
				SiegeCastleRosterSelectionProfile.AlliedSpawnCommandSource,
				formationClass);
		}
		catch (Exception ex)
		{
			Log("prepare_external_ally_agent failed troop=" + SafeAgentCharacterId(agent) + " error=" + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private Vec3 ResolveExternalAllyAssemblyPosition(Agent main, int index)
	{
		Mission mission = base.Mission;
		Vec3 anchor = main?.Position ?? Vec3.Zero;
		Vec3 forward = main?.LookDirection ?? Vec3.Forward;
		forward.z = 0f;
		if (forward.LengthSquared < 0.01f)
		{
			forward = Vec3.Forward;
		}
		forward.Normalize();
		Vec3 right = new Vec3(-forward.y, forward.x, 0f);
		int columns = Math.Max(1, SiegeCastleRosterSelectionProfile.AlliedInitialGridColumns);
		int row = Math.Max(0, index) / columns;
		int column = Math.Max(0, index) % columns;
		float lateral = (column - (columns - 1) * 0.5f) * SiegeCastleRosterSelectionProfile.AlliedInitialLateralSpacing;
		float depth = SiegeCastleRosterSelectionProfile.AlliedInitialStartDepth + row * SiegeCastleRosterSelectionProfile.AlliedInitialRowSpacing;
		Vec3 candidate = anchor - forward * depth + right * lateral;
		try
		{
			TaleWorlds.Engine.Scene scene = mission?.Scene;
			if (scene != null)
			{
				candidate.z = scene.GetGroundHeightAtPosition(candidate);
				TaleWorlds.Engine.WorldPosition worldPosition = new TaleWorlds.Engine.WorldPosition(scene, candidate);
				if (worldPosition.GetNearestNavMesh() != UIntPtr.Zero)
				{
					return worldPosition.GetNavMeshVec3();
				}
				Vec3 fallback = mission.GetRandomPositionAroundPoint(anchor, 1.5f, 8f, true);
				TaleWorlds.Engine.WorldPosition fallbackWorld = new TaleWorlds.Engine.WorldPosition(scene, fallback);
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

	private void HoldPreparedAlliedFormations(IEnumerable<Agent> agents)
	{
		foreach (IGrouping<Formation, Agent> group in agents
			.Where(agent => agent?.Formation != null && agent.IsActive())
			.GroupBy(agent => agent.Formation))
		{
			try
			{
				Formation formation = group.Key;
				Vec2 center = Vec2.Zero;
				int count = 0;
				foreach (Agent agent in group)
				{
					center += agent.Position.AsVec2;
					count++;
				}
				if (count > 0)
				{
					center /= count;
					formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
					Vec3 centerPosition = new Vec3(center.x, center.y, group.First().Position.z);
					TaleWorlds.Engine.WorldPosition centerWorld = new TaleWorlds.Engine.WorldPosition(base.Mission.Scene, centerPosition);
					formation.SetMovementOrder(MovementOrder.MovementOrderMove(centerWorld));
				}
			}
			catch
			{
			}
		}
	}

	private bool HasPrisonersToSpawn()
	{
		TroopRoster roster = GetPrisonerRosterForSpawn();
		return roster != null && roster.TotalManCount > 0;
	}

	private bool IsDeploymentCurrentlyActive()
	{
		return base.Mission != null && base.Mission.Mode == MissionMode.Deployment;
	}

	private bool CanSpawnPrisonersNow()
	{
		return base.Mission != null && base.Mission.PlayerTeam != null;
	}

	private void MarkDeploymentBypassedAfterDirectPrisonerSpawn()
	{
		if (_deploymentEndDetected)
		{
			return;
		}
		_deploymentEndDetected = true;
		ForceLordPrisonerFormationClass("direct_spawn_no_deployment");
		EnsurePrisonerFormationsIsolated("direct_spawn_no_deployment");
		TryRecalculateLordPrisonerFormationWidth("direct_spawn_no_deployment", onlyIfAnomalous: true);
		if (!_externalCastleRuntime)
		{
			FreezePrisoners();
		}
		Log("deployment_bypassed_for_prisoners reason=direct_spawn_no_vanilla_deployment mode=" + (base.Mission?.Mode.ToString() ?? "null") + " current_time=" + (base.Mission?.CurrentTime.ToString("0.00") ?? "null"));
		TryDisableBattleEndLogic("direct_spawn_no_deployment");
	}

	public override void OnRemoveBehavior()
	{
		RequestCleanup("OnRemoveBehavior");
		base.OnRemoveBehavior();
	}

	protected override void OnEndMission()
	{
		RequestCleanup("OnEndMission");
		base.OnEndMission();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		_prisonerSlaughterRuntime?.OnAgentRemoved(
			affectedAgent,
			affectorAgent,
			agentState);
		LogAgentRemovedDiag(affectedAgent, affectorAgent, agentState);
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		string missionResultText = missionResult?.ToString() ?? "null";
		string state = missionResultText + "|" + _battleEndDisabled + "|" + _deploymentEndDetected;
		float currentTime = base.Mission?.CurrentTime ?? 0f;
		if (!string.Equals(_lastMissionEndedLogState, state, StringComparison.Ordinal) || currentTime >= _nextMissionEndedLogTime)
		{
			_lastMissionEndedLogState = state;
			_nextMissionEndedLogTime = currentTime + 5f;
			Log($"mission_ended_check mission_result={missionResultText} battle_end_disabled={_battleEndDisabled} deployment_detected={_deploymentEndDetected}");
		}
		return false;
	}

	internal void TryDisableBattleEndLogic(string source)
	{
		try
		{
			if (base.Mission == null)
			{
				Log("battle_end_disable skipped source=" + source + " mission=null");
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
			_battleEndLogic.ChangeCanCheckForEndCondition(false);
			_battleEndDisabled = true;
			Log("battle_end_disable success source=" + source);
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
		if (_battleEndDisabled || base.Mission == null || base.Mission.CurrentTime < _nextBattleEndDisableRetryTime)
		{
			return;
		}
		_nextBattleEndDisableRetryTime = base.Mission.CurrentTime + 1f;
		TryDisableBattleEndLogic("retry_tick");
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
				Log($"mission_mode_changed {_lastMissionMode} -> {currentMode}");
			}
			if (_deploymentWasActive && currentMode != MissionMode.Deployment)
			{
				_deploymentEndDetected = true;
				ForceLordPrisonerFormationClass("deployment_end");
				if (!_externalCastleRuntime)
				{
					FreezePrisoners();
				}
				Log("deployment_ended detection");
				TryDisableBattleEndLogic("deployment_ended");
			}
			_deploymentWasActive = currentMode == MissionMode.Deployment;
			_lastMissionMode = currentMode;
		}
		catch (Exception ex)
		{
			Log("detect_deployment_end failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void FreezePrisoners()
	{
		try
		{
			if (base.Mission == null)
			{
				return;
			}
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive() || !(agent.Origin is PrisonerAgentOrigin)
					|| CastleAftermathLordDuelRuntimeBridge.ControlsAgent(agent))
				{
					continue;
				}
				agent.Formation = null;
				bool isLord;
				if (_prisonerIsLordMap.TryGetValue(agent, out isLord))
				{
					ApplyPrisonerPose(agent, isLord, afterDeployment: true);
				}
			}
			Log("freeze_prisoners done");
		}
		catch (Exception ex)
		{
			Log("freeze_prisoners failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void SpawnPrisoners()
	{
		if (_prisonersSpawned || base.Mission == null)
		{
			return;
		}
		TroopRoster prisonRoster = GetPrisonerRosterForSpawn();
		string prisonerSource = _inspectionPrisonerRoster != null ? "selection_snapshot" : "main_party_fallback";
		Log("spawn_prisoners_begin source=" + prisonerSource + " mode=" + (base.Mission?.Mode.ToString() ?? "null") + " deployment_was=" + _deploymentWasActive + " deployment_end=" + _deploymentEndDetected + " roster=" + PrisonerRosterDiag(prisonRoster) + " main_roster=" + PrisonerRosterDiag(PartyBase.MainParty?.PrisonRoster));
		if (prisonRoster == null)
		{
			Log("spawn_prisoners skipped: PrisonRoster null source=" + prisonerSource + " main_party=" + (PartyBase.MainParty != null));
			Logger.LogEvent("TroopInspection", "spawn_prisoners skipped: PrisonRoster null");
			AnimusForgeQuickInfo.Show("阅兵：无法访问囚犯名册。");
			return;
		}
		int totalCount = 0;
		int heroCount = 0;
		int regularCount = 0;
		foreach (TroopRosterElement item in TroopInspectionBehavior.SnapshotRoster(prisonRoster))
		{
			if (item.Character == null)
			{
				continue;
			}
			totalCount += item.Number;
			if (item.Character.IsHero)
			{
				heroCount += item.Number;
			}
			else
			{
				regularCount += item.Number;
			}
		}
		if (totalCount <= 0)
		{
			Log("spawn_prisoners skipped: no selected prisoners source=" + prisonerSource + " roster=" + PrisonerRosterDiag(prisonRoster) + " main_roster=" + PrisonerRosterDiag(PartyBase.MainParty?.PrisonRoster));
			Logger.LogEvent("TroopInspection", "spawn_prisoners skipped: no prisoners at all");
			AnimusForgeQuickInfo.Show("阅兵：没有囚犯可参加阅兵。");
			return;
		}
		Team playerTeam = base.Mission.PlayerTeam;
		if (playerTeam == null)
		{
			Log("spawn_prisoners skipped: PlayerTeam null source=" + prisonerSource + " roster=" + PrisonerRosterDiag(prisonRoster));
			Logger.LogEvent("TroopInspection", "spawn_prisoners skipped: PlayerTeam null");
			return;
		}
		_prisonersSpawned = true;
		if (heroCount > 0)
		{
			playerTeam.GetFormation(LordPrisonerFormationClass);
		}
		if (regularCount > 0)
		{
			playerTeam.GetFormation(RegularPrisonerFormationClass);
		}
		EnsurePrisonerFormationsIsolated("before_spawn");
		int spawnedHeroes = 0;
		int spawnedRegulars = 0;
		int totalErrors = 0;
		string lastError = "";
		int heroIdx = 0;
		foreach (TroopRosterElement item in TroopInspectionBehavior.SnapshotRoster(prisonRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || !character.IsHero)
			{
				continue;
			}
			for (int i = 0; i < item.Number; i++)
			{
				try
				{
					PrisonerAgentOrigin origin = new PrisonerAgentOrigin(character);
					Agent agent = SpawnPrisonerAgent(origin, heroCount, heroIdx, LordPrisonerFormationClass);
					if (agent != null)
					{
						agent.SetIsAIPaused(isPaused: true);
						agent.DisableScriptedMovement();
						_prisonerIsLordMap[agent] = true;
						ApplyPrisonerPose(agent, isLord: true, afterDeployment: false);
						NotifyExternalPrisonerSpawned(agent, isLord: true);
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_hero ok troop={character.StringId} team={agent.Team?.Side.ToString() ?? "null"} formation={agent.Formation?.FormationIndex.ToString() ?? "null"} pos={agent.Position}");
						spawnedHeroes++;
					}
					else
					{
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_hero returned null troop={character.StringId} formation={LordPrisonerFormationClass}");
					}
					heroIdx++;
				}
				catch (Exception ex)
				{
					totalErrors++;
					lastError = ex.GetType().Name + ": " + ex.Message;
					Logger.LogEvent("TroopInspection", "spawn_prisoner_hero failed: " + lastError);
				}
			}
		}
		int regIdx = 0;
		foreach (TroopRosterElement item in TroopInspectionBehavior.SnapshotRoster(prisonRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsHero)
			{
				continue;
			}
			for (int i = 0; i < item.Number; i++)
			{
				try
				{
					PrisonerAgentOrigin origin = new PrisonerAgentOrigin(character);
					Agent agent = SpawnPrisonerAgent(origin, regularCount, regIdx, RegularPrisonerFormationClass);
					if (agent != null)
					{
						agent.SetIsAIPaused(isPaused: true);
						agent.DisableScriptedMovement();
						_prisonerIsLordMap[agent] = false;
						ApplyPrisonerPose(agent, isLord: false, afterDeployment: false);
						NotifyExternalPrisonerSpawned(agent, isLord: false);
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_regular ok troop={character.StringId} team={agent.Team?.Side.ToString() ?? "null"} formation={agent.Formation?.FormationIndex.ToString() ?? "null"} pos={agent.Position}");
						spawnedRegulars++;
					}
					else
					{
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_regular returned null troop={character.StringId} formation={RegularPrisonerFormationClass}");
					}
					regIdx++;
				}
				catch (Exception ex)
				{
					totalErrors++;
					lastError = ex.GetType().Name + ": " + ex.Message;
					Logger.LogEvent("TroopInspection", "spawn_prisoner_regular failed: " + lastError);
				}
			}
		}
		int spawned = spawnedHeroes + spawnedRegulars;
		Log("spawn_prisoners result: source=" + prisonerSource + " total=" + totalCount + " heroes=" + heroCount + " spawned_heroes=" + spawnedHeroes + " regulars=" + regularCount + " spawned_regulars=" + spawnedRegulars + " errors=" + totalErrors + " roster_after=" + PrisonerRosterDiag(prisonRoster) + " main_roster_after=" + PrisonerRosterDiag(PartyBase.MainParty?.PrisonRoster));
		Logger.LogEvent("TroopInspection", "spawn_prisoners result: total=" + totalCount + " heroes=" + heroCount + " spawned_heroes=" + spawnedHeroes + " regulars=" + regularCount + " spawned_regulars=" + spawnedRegulars + " errors=" + totalErrors);
		if (spawnedHeroes > 0)
		{
			ForceLordPrisonerFormationClass("after_spawn");
			TryRecalculateLordPrisonerFormationWidth("after_spawn", onlyIfAnomalous: false);
		}
		if (spawned > 0)
		{
			EnsurePrisonerFormationsIsolated("after_spawn");
		}
		NotifyExternalPrisonerSpawnCompleted(totalCount, spawnedRegulars, spawnedHeroes);
		if (!_externalCastleRuntime && spawned > 0)
		{
			string msg = "阅兵：";
			if (spawnedHeroes > 0)
			{
				msg += spawnedHeroes + " 名领主俘虏（8号领主编队），";
			}
			if (spawnedRegulars > 0)
			{
				msg += spawnedRegulars + " 名士兵俘虏（7号俘虏编队）";
			}
			AnimusForgeQuickInfo.Show(msg);
		}
		else if (!_externalCastleRuntime && totalErrors > 0)
		{
			AnimusForgeQuickInfo.Show("阅兵：囚犯生成失败(" + totalErrors + "/" + totalCount + ") 错误: " + lastError);
		}
		else if (!_externalCastleRuntime)
		{
			AnimusForgeQuickInfo.Show("阅兵：囚犯生成失败(" + totalCount + "名尝试，0名成功)。");
		}
	}

	private Agent SpawnPrisonerAgent(
		IAgentOriginBase origin,
		int formationTroopCount,
		int formationTroopIndex,
		FormationClass formationClass)
	{
		return BannerlordApiCompat.SpawnInspectionTroop(
			base.Mission,
			origin,
			formationTroopCount,
			formationTroopIndex,
			formationClass,
			wieldInitialWeapons: false);
	}

	private void NotifyExternalPrisonerSpawned(Agent agent, bool isLord)
	{
		try
		{
			_externalPrisonerSpawned?.Invoke(agent, isLord);
		}
		catch (Exception ex)
		{
			Log("external_prisoner_spawned callback failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void NotifyExternalPrisonerSpawnCompleted(int selectedCount, int spawnedRegulars, int spawnedLords)
	{
		try
		{
			_externalPrisonerSpawnCompleted?.Invoke(selectedCount, spawnedRegulars, spawnedLords);
		}
		catch (Exception ex)
		{
			Log("external_prisoner_spawn_completed callback failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}





	private static string PrisonerRosterDiag(TroopRoster roster)
	{
		try
		{
			if (roster == null)
			{
				return "null";
			}
			int total = 0;
			int heroes = 0;
			int regular = 0;
			int wounded = 0;
			List<string> samples = new List<string>();
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				CharacterObject character = element.Character;
				if (character == null || element.Number <= 0)
				{
					continue;
				}
				total += Math.Max(0, element.Number);
				wounded += Math.Max(0, element.WoundedNumber);
				if (character.IsHero)
				{
					heroes += Math.Max(0, element.Number);
				}
				else
				{
					regular += Math.Max(0, element.Number);
				}
				if (samples.Count < 5)
				{
					Hero hero = character.HeroObject;
					samples.Add(character.StringId + ":n=" + element.Number + ",w=" + element.WoundedNumber + ",hero=" + character.IsHero + ",alive=" + ((hero != null) ? hero.IsAlive.ToString() : "null") + ",dead=" + ((hero != null) ? hero.IsDead.ToString() : "null"));
				}
			}
			return "count=" + roster.Count + ",total=" + total + ",heroes=" + heroes + ",regular=" + regular + ",wounded=" + wounded + ",samples=[" + string.Join(";", samples) + "]";
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name + ":" + ex.Message;
		}
	}
	private void ForceLordPrisonerFormationClass(string reason)
	{
		try
		{
			Team playerTeam = base.Mission?.PlayerTeam;
			if (playerTeam == null)
			{
				return;
			}
			Formation formation = playerTeam.GetFormation(LordPrisonerFormationClass);
			if (formation == null)
			{
				return;
			}
			FormationClass oldRepresentativeClass = formation.RepresentativeClass;
			object oldLogicalClass = FormationLogicalClassField?.GetValue(formation);
			object oldLogicalClassNeedsUpdate = FormationLogicalClassNeedsUpdateField?.GetValue(formation);
			bool needsCorrection = oldRepresentativeClass != LordPrisonerRuntimeClass || (oldLogicalClass is FormationClass && (FormationClass)oldLogicalClass != LordPrisonerRuntimeClass) || (oldLogicalClassNeedsUpdate is bool && (bool)oldLogicalClassNeedsUpdate);
			if (!needsCorrection)
			{
				return;
			}
			FormationRepresentativeClassProperty?.SetValue(formation, LordPrisonerRuntimeClass, null);
			FormationLogicalClassField?.SetValue(formation, LordPrisonerRuntimeClass);
			FormationLogicalClassNeedsUpdateField?.SetValue(formation, false);
			if (_lordFormationClassForceLogCount < 1)
			{
				_lordFormationClassForceLogCount++;
				Log("force_lord_prisoner_formation_class reason=" + reason + " old_rep=" + oldRepresentativeClass + " new_rep=" + LordPrisonerRuntimeClass + " old_logical=" + (oldLogicalClass ?? "null") + " old_needs_update=" + (oldLogicalClassNeedsUpdate ?? "null"));
			}
		}
		catch (Exception ex)
		{
			if (_lordFormationClassForceLogCount < 1)
			{
				_lordFormationClassForceLogCount++;
				Log("force_lord_prisoner_formation_class failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	private void EnsurePrisonerFormationsIsolated(string reason)
	{
		try
		{
			Mission mission = base.Mission;
			Team playerTeam = mission?.PlayerTeam;
			if (mission == null || playerTeam == null)
			{
				return;
			}
			Formation regularFormation = playerTeam.GetFormation(RegularPrisonerFormationClass);
			Formation lordFormation = playerTeam.GetFormation(LordPrisonerFormationClass);
			int normalMovedOut = 0;
			int regularPrisonersMoved = 0;
			int lordPrisonersMoved = 0;
			foreach (Agent agent in mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || agent.Team != playerTeam)
				{
					continue;
				}
				Formation formation = agent.Formation;
				bool inReserved = (regularFormation != null && formation == regularFormation) || (lordFormation != null && formation == lordFormation);
				if (TryGetPrisonerIsLord(agent, out bool isLord))
				{
					Formation target = isLord ? lordFormation : regularFormation;
					if (target != null && formation != target && TryMoveAgentToFormation(agent, target))
					{
						if (isLord) lordPrisonersMoved++; else regularPrisonersMoved++;
					}
				}
				else if (inReserved)
				{
					Formation target = ResolveNormalTroopFormation(playerTeam, agent, formation);
					if (target != null && TryMoveAgentToFormation(agent, target))
					{
						normalMovedOut++;
					}
				}
			}
			if ((normalMovedOut > 0 || regularPrisonersMoved > 0 || lordPrisonersMoved > 0) && _formationIsolationLogCount < FormationIsolationLogLimit)
			{
				_formationIsolationLogCount++;
				Log("formation_isolate result reason=" + reason + " normal_moved_out=" + normalMovedOut + " regular_prisoners_moved=" + regularPrisonersMoved + " lord_prisoners_moved=" + lordPrisonersMoved);
			}
		}
		catch (Exception ex)
		{
			if (_formationIsolationLogCount < FormationIsolationLogLimit)
			{
				_formationIsolationLogCount++;
				Log("formation_isolate failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	internal static Formation ResolveNormalTroopFormation(Team playerTeam, Agent agent, Formation currentFormation)
	{
		if (playerTeam == null)
		{
			return null;
		}
		try
		{
			CharacterObject character = agent?.Character as CharacterObject;
			if (character != null && !IsReservedPrisonerFormationClass(character.DefaultFormationClass))
			{
				Formation formation = playerTeam.GetFormation(character.DefaultFormationClass);
				if (formation != null && formation != currentFormation)
				{
					return formation;
				}
			}
		}
		catch
		{
		}
		for (int i = 0; i < 6; i++)
		{
			try
			{
				Formation formation = playerTeam.GetFormation((FormationClass)i);
				if (formation != null && formation != currentFormation)
				{
					return formation;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	internal static bool IsReservedPrisonerFormationClass(FormationClass formationClass)
	{
		return (int)formationClass == 6 || (int)formationClass == 7;
	}

	internal static bool TryMoveAgentToFormation(Agent agent, Formation targetFormation)
	{
		if (agent == null || targetFormation == null || agent.Formation == targetFormation)
		{
			return false;
		}
		agent.Formation = targetFormation;
		agent.TryAttachToFormation();
		return agent.Formation == targetFormation;
	}

	internal static bool TryResolveInspectionPrisoner(Agent agent, out bool isLord)
	{
		isLord = false;
		if (agent == null)
		{
			return false;
		}
		try
		{
			TroopInspectionMissionLogic logic = Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>();
			if (logic != null && logic.TryGetPrisonerIsLord(agent, out isLord))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject character = (agent.Origin as PrisonerAgentOrigin)?.Troop as CharacterObject;
			if (character == null)
			{
				return false;
			}
			isLord = character.IsHero;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void TryRecalculateLordPrisonerFormationWidth(string reason, bool onlyIfAnomalous)
	{
		try
		{
			Formation formation = base.Mission?.PlayerTeam?.GetFormation(LordPrisonerFormationClass);
			if (formation == null || formation.CountOfUnits <= 0)
			{
				return;
			}
			float oldWidth = formation.Width;
			float unitDiameter = formation.UnitDiameter;
			if (unitDiameter <= 0.01f)
			{
				return;
			}
			float targetWidth = unitDiameter * (float)Math.Max(1, formation.CountOfUnits * 2 - 1);
			if (onlyIfAnomalous && oldWidth <= targetWidth * 1.35f + 0.5f)
			{
				return;
			}
			formation.SetFormOrder(FormOrder.FormOrderCustom(targetWidth), updateDesiredFileCount: true);
			formation.ResetArrangementOrderTickTimer();
			formation.SetHasPendingUnitPositions(true);
			if (_prisonerFormationRecalcLogCount < PrisonerFormationRecalcLogLimit)
			{
				_prisonerFormationRecalcLogCount++;
				Log("prisoner_form_recalc reason=" + reason + " formation=8 count=" + formation.CountOfUnits + " old_width=" + oldWidth + " target_width=" + targetWidth + " new_width=" + formation.Width);
			}
		}
		catch (Exception ex)
		{
			if (_prisonerFormationRecalcLogCount < PrisonerFormationRecalcLogLimit)
			{
				_prisonerFormationRecalcLogCount++;
				Log("prisoner_form_recalc failed reason=" + reason + " " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}


	private void TryLogPrisonerDeployPairDiag()
	{
		if (_prisonerDeployPairDiagLogCount >= PrisonerDeployPairDiagLogLimit)
		{
			return;
		}
		Mission mission = base.Mission;
		if (mission == null || mission.PlayerTeam == null || mission.CurrentTime < _nextPrisonerDeployPairDiagTime)
		{
			return;
		}
		try
		{
			Formation regularFormation = mission.PlayerTeam.GetFormation(RegularPrisonerFormationClass);
			Formation lordFormation = mission.PlayerTeam.GetFormation(LordPrisonerFormationClass);
			CountSelectedPrisoners(out int selectedRegularPrisoners, out int selectedLordPrisoners);
			string orderDelta = "null";
			string avgDelta = "null";
			if (regularFormation != null && lordFormation != null)
			{
				orderDelta = FormatVec2(new Vec2(lordFormation.OrderPosition.X - regularFormation.OrderPosition.X, lordFormation.OrderPosition.Y - regularFormation.OrderPosition.Y));
				Vec2 regularAverage = CalculateFormationAveragePosition(regularFormation, out int regularActiveCount);
				Vec2 lordAverage = CalculateFormationAveragePosition(lordFormation, out int lordActiveCount);
				if (regularActiveCount > 0 && lordActiveCount > 0)
				{
					avgDelta = FormatVec2(new Vec2(lordAverage.X - regularAverage.X, lordAverage.Y - regularAverage.Y));
				}
			}
			_prisonerDeployPairDiagLogCount++;
			_nextPrisonerDeployPairDiagTime = mission.CurrentTime + 0.75f;
			Log("prisoner_deploy_pair_diag sample=" + _prisonerDeployPairDiagLogCount
				+ " selected_regular_prisoners=" + selectedRegularPrisoners
				+ " selected_lord_prisoners=" + selectedLordPrisoners
				+ " source=" + (_inspectionPrisonerRoster != null ? "selection_snapshot" : "main_party_fallback")
				+ " " + BuildFormationDeployDiag("f6", regularFormation)
				+ " " + BuildFormationDeployDiag("f7", lordFormation)
				+ " delta_order_7_minus_6=" + orderDelta
				+ " delta_avg_7_minus_6=" + avgDelta);
		}
		catch (Exception ex)
		{
			_prisonerDeployPairDiagLogCount = PrisonerDeployPairDiagLogLimit;
			Log("prisoner_deploy_pair_diag failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private string BuildFormationDeployDiag(string label, Formation formation)
	{
		if (formation == null)
		{
			return label + "=null";
		}
		Vec2 averagePosition = CalculateFormationAveragePosition(formation, out int activeAgentCount);
		object logicalField = null;
		if (FormationLogicalClassField != null)
		{
			logicalField = FormationLogicalClassField.GetValue(formation);
		}
		return label + ": count=" + formation.CountOfUnits
			+ " active=" + activeAgentCount
			+ " rep=" + formation.RepresentativeClass
			+ " logical=" + formation.LogicalClass
			+ " logical_field=" + (logicalField ?? "null")
			+ " physical=" + formation.PhysicalClass
			+ " query_main=" + (formation.QuerySystem != null ? formation.QuerySystem.MainClass.ToString() : "null")
			+ " order=" + FormatVec2(formation.OrderPosition)
			+ " current=" + FormatVec2(formation.CurrentPosition)
			+ " avg=" + (activeAgentCount > 0 ? FormatVec2(averagePosition) : "null")
			+ " cached_avg=" + FormatVec2(formation.CachedAveragePosition)
			+ " width=" + FormatFloat(formation.Width)
			+ " depth=" + FormatFloat(formation.Depth);
	}

	private Vec2 CalculateFormationAveragePosition(Formation formation, out int activeAgentCount)
	{
		activeAgentCount = 0;
		float x = 0f;
		float y = 0f;
		Mission mission = base.Mission;
		if (formation == null || mission == null)
		{
			return Vec2.Zero;
		}
		foreach (Agent agent in mission.Agents)
		{
			if (agent != null && agent.Formation == formation && agent.IsHuman && agent.IsActive())
			{
				Vec3 position = agent.Position;
				x += position.X;
				y += position.Y;
				activeAgentCount++;
			}
		}
		if (activeAgentCount <= 0)
		{
			return Vec2.Zero;
		}
		return new Vec2(x / activeAgentCount, y / activeAgentCount);
	}

	private void CountSelectedPrisoners(out int regularPrisoners, out int lordPrisoners)
	{
		regularPrisoners = 0;
		lordPrisoners = 0;
		TroopRoster roster = _inspectionPrisonerRoster ?? PartyBase.MainParty?.PrisonRoster;
		if (roster == null)
		{
			return;
		}
		foreach (TroopRosterElement item in TroopInspectionBehavior.SnapshotRoster(roster))
		{
			CharacterObject character = item.Character;
			if (character == null)
			{
				continue;
			}
			if (character.IsHero)
			{
				lordPrisoners += Math.Max(0, item.Number);
			}
			else
			{
				regularPrisoners += Math.Max(0, item.Number);
			}
		}
	}

	private static string FormatVec2(Vec2 position)
	{
		return $"{position.X:0.00},{position.Y:0.00}";
	}

	private static string FormatFloat(float value)
	{
		return value.ToString("0.00");
	}

	private void LogAgentRemovedDiag(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			if (_agentRemovedDiagLogCount >= AgentRemovedDiagLogLimit || affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			bool isPrisoner = TryResolveInspectionPrisoner(affectedAgent, out bool isLordPrisoner);
			CharacterObject character = affectedAgent.Character as CharacterObject;
			bool isHero = character?.IsHero ?? false;
			bool isPlayer = affectedAgent.IsMainAgent || (character?.IsPlayerCharacter ?? false);
			Team playerTeam = base.Mission?.PlayerTeam;
			bool isPlayerTeam = playerTeam != null && affectedAgent.Team == playerTeam;
			if (!isPrisoner && !isHero && !isPlayer && !isPlayerTeam)
			{
				return;
			}
			_agentRemovedDiagLogCount++;
			Log("agent_removed_diag sample=" + _agentRemovedDiagLogCount
				+ " victim=" + SafeAgentCharacterId(affectedAgent)
				+ " victim_origin=" + SafeAgentOriginType(affectedAgent)
				+ " victim_team=" + SafeAgentTeamSide(affectedAgent)
				+ " formation=" + SafeAgentFormationIndex(affectedAgent)
				+ " agent_state=" + agentState
				+ " active=" + SafeAgentActive(affectedAgent)
				+ " hp=" + SafeAgentHealth(affectedAgent)
				+ " prisoner=" + isPrisoner
				+ " is_lord_prisoner=" + isLordPrisoner
				+ " hero=" + isHero
				+ " player=" + isPlayer
				+ " player_team=" + isPlayerTeam
				+ " hero_state=" + SafeHeroState(character?.HeroObject)
				+ " affector=" + SafeAgentCharacterId(affectorAgent)
				+ " affector_origin=" + SafeAgentOriginType(affectorAgent)
				+ " affector_main=" + ((affectorAgent != null) ? affectorAgent.IsMainAgent.ToString() : "null"));
		}
		catch (Exception ex)
		{
			if (_agentRemovedDiagLogCount < AgentRemovedDiagLogLimit)
			{
				_agentRemovedDiagLogCount++;
				Log("agent_removed_diag_failed sample=" + _agentRemovedDiagLogCount + " " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	private static string SafeAgentCharacterId(Agent agent)
	{
		try
		{
			CharacterObject character = agent?.Character as CharacterObject;
			return character?.StringId ?? character?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeAgentOriginType(Agent agent)
	{
		try
		{
			return agent?.Origin?.GetType().Name ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeAgentTeamSide(Agent agent)
	{
		try
		{
			return agent?.Team?.Side.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeAgentFormationIndex(Agent agent)
	{
		try
		{
			return agent?.Formation?.FormationIndex.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeAgentActive(Agent agent)
	{
		try
		{
			return agent?.IsActive().ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeAgentHealth(Agent agent)
	{
		try
		{
			return agent != null ? agent.Health.ToString("0.##") : "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeHeroState(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return "null";
			}
			return "alive=" + hero.IsAlive + ",dead=" + hero.IsDead + ",wounded=" + hero.IsWounded + ",hp=" + hero.HitPoints + "/" + hero.MaxHitPoints + ",wounded_limit=" + hero.WoundedHealthLimit;
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name;
		}
	}

	private void TryLogAgentCounts()
	{
		if (base.Mission == null || (!_deploymentEndDetected && base.Mission.CurrentTime < 3f))
		{
			return;
		}
		try
		{
			BattleSideEnum playerSide = base.Mission.PlayerTeam?.Side ?? PartyBase.MainParty.Side;
			BattleSideEnum enemySide = playerSide.GetOppositeSide();
			int playerAgents = 0;
			int enemyAgents = 0;
			int neutralAgents = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				Team team = agent.Team;
				if (team == null)
				{
					neutralAgents++;
				}
				else if (team.Side == playerSide)
				{
					playerAgents++;
				}
				else if (team.Side == enemySide)
				{
					enemyAgents++;
				}
				else
				{
					neutralAgents++;
				}
			}
			if (!_agentCountsLogged)
			{
				_agentCountsLogged = true;
				Log($"agent_counts player_side={playerSide} enemy_side={enemySide} player_agents={playerAgents} enemy_agents={enemyAgents} neutral_agents={neutralAgents}");
			}
			if (!_enemyAgentWarningLogged && enemyAgents > 0)
			{
				_enemyAgentWarningLogged = true;
				Log($"enemy_agents_detected count={enemyAgents}");
			}
		}
		catch (Exception ex)
		{
			Log("agent_count_log failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void ContinuousAgentRefresh(float dt)
	{
		_continuousRefreshTimer += dt;
		if (_continuousRefreshTimer < RefreshInterval)
		{
			return;
		}
		_continuousRefreshTimer = 0f;
		try
		{
			bool isInConversation = Campaign.Current?.ConversationManager != null && Campaign.Current.ConversationManager.IsConversationInProgress && Campaign.Current.ConversationManager.OneToOneConversationAgent != null;
			if (isInConversation && !_conversationStateLogged)
			{
				_conversationStateLogged = true;
				Log("conversation_state_changed in_conversation=true");
			}
			if (!isInConversation)
			{
				_conversationStateLogged = false;
			}
			Agent mainAgent = base.Mission?.MainAgent;
			Team playerTeam = base.Mission?.PlayerTeam;
			if (mainAgent == null || playerTeam == null)
			{
				return;
			}
			Vec3 mainPos = mainAgent.Position;
			int refreshed = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				if (agent == mainAgent)
				{
					continue;
				}
				if (agent.Team != playerTeam)
				{
					continue;
				}
				if (agent.Origin is PrisonerAgentOrigin)
				{
					continue;
				}
				if (!RefreshAllPlayerAgents && agent.Position.Distance(mainPos) > RefreshRadius)
				{
					continue;
				}
				RefreshSingleAgent(agent);
				refreshed++;
			}
			if (!_firstRefreshLogged)
			{
				_firstRefreshLogged = true;
				Log($"continuous_refresh_started agents_refreshed={refreshed} interval={RefreshInterval} radius={RefreshRadius} refresh_all={RefreshAllPlayerAgents}");
			}
		}
		catch (Exception ex)
		{
			Log("continuous_refresh error: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RefreshSingleAgent(Agent agent)
	{
		if (agent == null || !agent.IsActive()
			|| CastleAftermathLordDuelRuntimeBridge.ControlsAgent(agent)
			|| _prisonerSlaughterRuntime?.ControlsAgent(agent) == true)
		{
			return;
		}
		bool castleSlaughterOwnsAgent = _externalCastleRuntime
			&& CastleAftermathRuntimeBridge.IsRegularPrisonerSlaughterActive(base.Mission)
			&& agent.Formation?.FormationIndex == SiegeCastleRosterSelectionProfile.AlliedFormationClassIndex;
		if (castleSlaughterOwnsAgent)
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
		TrySetAgentController(agent, "AI");
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.DisableScriptedMovement();
				mountAgent.ClearTargetFrame();
				mountAgent.SetIsAIPaused(isPaused: false);
				TrySetAgentController(mountAgent, "AI");
			}
		}
		catch
		{
		}
	}

	private void RefreshPrisonerPoses(float dt)
	{
		_prisonerPoseRefreshTimer -= dt;
		if (_prisonerPoseRefreshTimer > 0f)
		{
			return;
		}
		_prisonerPoseRefreshTimer = PrisonerPoseRefreshInterval;
		try
		{
			if (base.Mission == null)
			{
				return;
			}
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive() || !(agent.Origin is PrisonerAgentOrigin)
					|| CastleAftermathLordDuelRuntimeBridge.ControlsAgent(agent))
				{
					continue;
				}
				bool isLord;
				if (TryGetPrisonerIsLord(agent, out isLord))
				{
					ApplyPrisonerPose(agent, isLord, _deploymentEndDetected);
				}
			}
		}
		catch (Exception ex)
		{
			Log("refresh_prisoner_poses failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private bool TryGetPrisonerIsLord(Agent agent, out bool isLord)
	{
		isLord = false;
		if (agent == null)
		{
			return false;
		}
		if (_prisonerIsLordMap.TryGetValue(agent, out isLord))
		{
			return true;
		}
		PrisonerAgentOrigin origin = agent.Origin as PrisonerAgentOrigin;
		CharacterObject character = origin?.Troop as CharacterObject;
		if (character == null)
		{
			return false;
		}
		isLord = character.IsHero;
		_prisonerIsLordMap[agent] = isLord;
		return true;
	}

	private void CachePrisonerActions()
	{
		if (_prisonerActionsCached)
		{
			return;
		}
		_prisonerActionsCached = true;
		_lordPrisonerAction = ActionIndexCache.act_scared_idle_1;
		_soldierPrisonerAction = ActionIndexCache.act_scared_idle_1;
		Log("prisoner_actions_cached lord=act_scared_idle_1 soldier=act_scared_idle_1 static_speed=0 progress=0.35");
	}

	private void ApplyPrisonerPose(Agent agent, bool isLord, bool afterDeployment)
	{
		if (agent == null || !agent.IsActive()
			|| CastleAftermathLordDuelRuntimeBridge.ControlsAgent(agent))
		{
			return;
		}
		CachePrisonerActions();
		try
		{
			agent.SetIsAIPaused(isPaused: true);
		}
		catch
		{
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		if (afterDeployment)
		{
			try
			{
				agent.SetMaximumSpeedLimit(0f, false);
			}
			catch
			{
			}
		}
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags(agentFlags & ~AgentFlag.CanGetAlarmed);
		}
		catch
		{
		}
		StripPrisonerWeapons(agent);
		try
		{
			agent.SetCrouchMode(false);
		}
		catch
		{
		}
		if (!afterDeployment)
		{
			return;
		}
		if (IsPrisonerPoseTemporarilySuppressed(agent))
		{
			return;
		}
		TrySetCivilianPrisonerActionSet(agent);
		TrySetPrisonerAction(agent, isLord);
	}

	internal void RestoreCastlePrisonerAfterExternalControl(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !TryGetPrisonerIsLord(agent, out bool isLord))
		{
			return;
		}
		_prisonerPoseApplied.Remove(agent);
		_civilianPrisonerActionSetApplied.Remove(agent);
		_prisonerPoseSuppressedUntil.Remove(agent);
		ApplyPrisonerPose(agent, isLord, _deploymentEndDetected);
	}

	private static void StripPrisonerWeapons(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
		for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
		{
			try
			{
				agent.RemoveEquippedWeapon((EquipmentIndex)i);
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

	private void TrySetPrisonerAction(Agent agent, bool isLord)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		ActionIndexCache action = isLord ? _lordPrisonerAction : _soldierPrisonerAction;
		string actionName = "act_scared_idle_1";
		int channelNo = 0;
		try
		{
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, action))
			{
				if (isLord && !_lordPrisonerActionMissingLogged)
				{
					_lordPrisonerActionMissingLogged = true;
					Log("prisoner_action_missing action=" + actionName);
				}
				if (!isLord && !_soldierPrisonerActionMissingLogged)
				{
					_soldierPrisonerActionMissingLogged = true;
					Log("prisoner_action_missing action=" + actionName);
				}
				return;
			}
			ActionIndexCache currentAction = agent.GetCurrentAction(channelNo);
			if (currentAction == action && _prisonerPoseApplied.Contains(agent))
			{
				return;
			}
			AnimFlags poseFlags = AnimFlags.anf_disable_alternative_randomization | AnimFlags.anf_disable_auto_increment_progress | AnimFlags.anf_enforce_all;
			bool actionSet = agent.SetActionChannel(channelNo, action, true, poseFlags, 0f, PrisonerPoseActionSpeed, -0.2f, 0.4f, PrisonerPoseStartProgress, false, -0.2f, 0, true);
			if (actionSet)
			{
				try
				{
					agent.SetCurrentActionProgress(channelNo, PrisonerPoseStartProgress);
				}
				catch
				{
				}
				_prisonerPoseApplied.Add(agent);
			}
			else if (!_prisonerActionSetRejectedLogged)
			{
				_prisonerActionSetRejectedLogged = true;
				Log("set_prisoner_action rejected action=" + actionName);
			}
		}
		catch (Exception ex)
		{
			Log("set_prisoner_action failed action=" + actionName + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void TrySetCivilianPrisonerActionSet(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || _civilianPrisonerActionSetApplied.Contains(agent))
			{
				return;
			}
			Monster monster = agent.Monster;
			if (monster == null)
			{
				return;
			}
			string actionSetCode = agent.IsFemale ? "as_human_female_villager" : "as_human_villager";
			AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBActionSet.GetActionSet(actionSetCode), 1f, false);
			agent.SetActionSet(ref animationSystemData);
			_civilianPrisonerActionSetApplied.Add(agent);
			Log("set_civilian_prisoner_action_set action_set=" + actionSetCode);
		}
		catch (Exception ex)
		{
			Log("set_civilian_prisoner_action_set failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private bool IsPrisonerPoseTemporarilySuppressed(Agent agent)
	{
		try
		{
			if (agent == null || base.Mission == null)
			{
				return false;
			}
			float suppressUntil;
			if (!_prisonerPoseSuppressedUntil.TryGetValue(agent, out suppressUntil))
			{
				return false;
			}
			return base.Mission.CurrentTime < suppressUntil;
		}
		catch
		{
			return false;
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in attackerWeapon, in blow, in attackCollisionData);
		try
		{
			if (affectedAgent == null || base.Mission == null || !(affectedAgent.Origin is PrisonerAgentOrigin)
				|| CastleAftermathLordDuelRuntimeBridge.ControlsAgent(affectedAgent))
			{
				return;
			}
			_prisonerPoseSuppressedUntil[affectedAgent] = base.Mission.CurrentTime + 0.9f;
			_prisonerPoseApplied.Remove(affectedAgent);
		}
		catch
		{
		}
	}

	private static void TrySetAgentController(Agent agent, string controllerType)
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
			object value = Enum.Parse(propertyInfo.PropertyType, controllerType, ignoreCase: true);
			if (value != null)
			{
				propertyInfo.SetValue(agent, value);
			}
		}
		catch
		{
		}
	}

	private void RequestCleanup(string reason)
	{
		if (_cleanupRequested)
		{
			return;
		}
		_cleanupRequested = true;
		try
		{
			_prisonerSlaughterRuntime?.Cleanup(reason);
		}
		catch (Exception ex)
		{
			Log("inspection_slaughter_cleanup failed reason=" + (reason ?? "N/A")
				+ " error=" + ex.GetType().Name + ": " + ex.Message);
		}
		try
		{
			Log($"request_cleanup reason={reason} battle_end_disabled={_battleEndDisabled} mission_result={base.Mission?.MissionResult?.ToString() ?? "null"}");
		}
		catch
		{
		}
		if (TroopInspectionBehavior.IsCurrentInspectionRuntime(_dummyPartyStringId))
		{
			TroopInspectionBehavior.CleanupRuntime(reason);
		}
		try
		{
			_externalCleanup?.Invoke(reason);
		}
		catch (Exception ex)
		{
			Log("external_cleanup callback failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		_prisonerIsLordMap.Clear();
		_civilianPrisonerActionSetApplied.Clear();
		_prisonerPoseSuppressedUntil.Clear();
		_prisonerPoseApplied.Clear();
	}

	internal bool CanOfferPrisonerSlaughterAction(
		int speakerAgentIndex,
		out int regularPrisonerCount,
		out int attackerCount)
	{
		regularPrisonerCount = 0;
		attackerCount = 0;
		Mission mission = base.Mission;
		Agent speaker = mission?.Agents?.FirstOrDefault(agent =>
			agent != null && agent.Index == speakerAgentIndex);
		List<Agent> prisoners = GetActiveRegularInspectionPrisoners();
		List<Agent> attackers = GetActiveInspectionSoldiers();
		regularPrisonerCount = prisoners.Count;
		attackerCount = attackers.Count;
		return TroopInspectionPrisonerSlaughterProfile.ShouldOfferRule(
			inspectionActive: mission != null && !mission.IsMissionEnding,
			externalCastleRuntime: _externalCastleRuntime,
			slaughterActive: _prisonerSlaughterRuntime?.IsBusy == true,
			speakerIsInspectedRegularSoldier: IsEligibleInspectionSoldier(speaker),
			regularPrisonerCount,
			attackerCount);
	}

	internal bool TryStartPrisonerSlaughter(
		int speakerAgentIndex,
		out int attackerCount,
		out int targetCount,
		out string reason)
	{
		attackerCount = 0;
		targetCount = 0;
		reason = string.Empty;
		if (_externalCastleRuntime || base.Mission == null || base.Mission.IsMissionEnding)
		{
			reason = "not_normal_inspection";
			return false;
		}
		if (_prisonerSlaughterRuntime?.IsBusy == true)
		{
			reason = "slaughter_already_active";
			return false;
		}

		Agent speaker = base.Mission.Agents?.FirstOrDefault(agent =>
			agent != null && agent.Index == speakerAgentIndex);
		if (!IsEligibleInspectionSoldier(speaker))
		{
			reason = "speaker_not_inspected_soldier";
			return false;
		}

		List<Agent> targets = GetActiveRegularInspectionPrisoners();
		if (targets.Count == 0)
		{
			reason = "no_regular_prisoners";
			return false;
		}
		List<Agent> attackers = GetActiveInspectionSoldiers();
		if (attackers.Count == 0)
		{
			reason = "speaker_not_inspected_soldier";
			return false;
		}

		_prisonerSlaughterRuntime = new TroopInspectionPrisonerSlaughterRuntime(
			base.Mission,
			attackers,
			targets,
			RestoreInspectionAgentAfterSlaughter);
		if (!_prisonerSlaughterRuntime.TryStart(out reason))
		{
			_prisonerSlaughterRuntime.Cleanup("start_failed");
			if (!_prisonerSlaughterRuntime.HasPendingRestore)
			{
				_prisonerSlaughterRuntime = null;
			}
			return false;
		}

		attackerCount = attackers.Count;
		targetCount = targets.Count;
		return true;
	}

	private List<Agent> GetActiveRegularInspectionPrisoners()
	{
		try
		{
			return base.Mission?.Agents?
				.Where(agent =>
					agent != null
					&& agent.IsHuman
					&& agent.IsActive()
					&& agent.Origin is PrisonerAgentOrigin
					&& TryGetPrisonerIsLord(agent, out bool isLord)
					&& !isLord)
				.OrderBy(agent => agent.Index)
				.ToList() ?? new List<Agent>();
		}
		catch
		{
			return new List<Agent>();
		}
	}

	private List<Agent> GetActiveInspectionSoldiers()
	{
		try
		{
			return base.Mission?.Agents?
				.Where(IsEligibleInspectionSoldier)
				.OrderBy(agent => agent.Index)
				.ToList() ?? new List<Agent>();
		}
		catch
		{
			return new List<Agent>();
		}
	}

	private bool IsEligibleInspectionSoldier(Agent agent)
	{
		if (agent == null
			|| !agent.IsHuman
			|| !agent.IsActive()
			|| agent == Agent.Main
			|| agent.Origin is PrisonerAgentOrigin
			|| agent.Team != (base.Mission?.PlayerTeam ?? Agent.Main?.Team))
		{
			return false;
		}
		CharacterObject character = agent.Character as CharacterObject;
		// Inspection selections may contain modded regular troops whose
		// CharacterObject.IsSoldier flag is not populated. In this mission every
		// active non-hero player-side non-prisoner is a selected troop, so the
		// roster/mission identity is the authoritative gate.
		return character != null && !character.IsHero;
	}

	private void RestoreInspectionAgentAfterSlaughter(Agent agent, bool prisoner)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		if (prisoner)
		{
			_prisonerPoseSuppressedUntil.Remove(agent);
			_prisonerPoseApplied.Remove(agent);
			ApplyPrisonerPose(agent, isLord: false, _deploymentEndDetected);
			return;
		}
		RefreshSingleAgent(agent);
	}

	private static void Log(string message)
	{
		Logger.Log("TroopInspection", "[TroopInspection] " + message);
	}
}

public class PrisonerAgentOrigin : IAgentOriginBase
{
	private static readonly uint PrisonerFactionColor = new Color(1f, 0f, 0f).ToUnsignedInteger();

	private static readonly uint PrisonerFactionColor2 = new Color(0.6f, 0f, 0f).ToUnsignedInteger();

	private const int OriginCasualtyDiagLogLimit = 40;

	private static int _originCasualtyDiagLogCount;

	private readonly CharacterObject _troop;

	private Banner _banner;

	private bool _isRemoved;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	public BasicCharacterObject Troop => _troop;

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	public bool IsUnderPlayersCommand => true;

#if BANNERLORD_1_4_OR_GREATER
	public bool IsInSameArmyAsPlayer => true;

#endif
	public uint FactionColor => PrisonerFactionColor;

	public uint FactionColor2 => PrisonerFactionColor2;

	public IBattleCombatant BattleCombatant => PartyBase.MainParty;

	public int UniqueSeed => MBRandom.RandomInt(1000000);

	public int Seed => CharacterHelper.GetDefaultFaceSeed(_troop, 0);

	public Banner Banner => _banner;

	public PrisonerAgentOrigin(CharacterObject troop)
	{
		_troop = troop;
		_banner = Clan.PlayerClan?.Banner;
		AgentOriginUtilities.GetDefaultTroopTraits(_troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		LogOriginCasualty("set_wounded_begin", "removed=" + _isRemoved + " " + BuildOriginTroopState(_troop));
		if (_isRemoved)
		{
			LogOriginCasualty("set_wounded_skip_removed", BuildOriginTroopState(_troop));
			return;
		}
		_isRemoved = true;
		if (_troop.IsHero)
		{
			_troop.HeroObject.MakeWounded();
		}
		else
		{
			PartyBase.MainParty.PrisonRoster.WoundTroop(_troop, 1, default(UniqueTroopDescriptor));
		}
		LogOriginCasualty("set_wounded_end", BuildOriginTroopState(_troop));
	}

	public void SetKilled()
	{
		LogOriginCasualty("set_killed_begin", "removed=" + _isRemoved + " " + BuildOriginTroopState(_troop));
		if (_isRemoved)
		{
			LogOriginCasualty("set_killed_skip_removed", BuildOriginTroopState(_troop));
			return;
		}
		_isRemoved = true;
		if (_troop.IsHero)
		{
			KillCharacterAction.ApplyByBattle(_troop.HeroObject, null, showNotification: true);
		}
		else
		{
			PartyBase.MainParty.PrisonRoster.AddToCounts(_troop, -1, false, 0, 0, true, -1);
		}
		LogOriginCasualty("set_killed_end", BuildOriginTroopState(_troop));
	}

	internal void MarkCampaignCasualtyHandledExternally(string source)
	{
		if (_isRemoved)
		{
			return;
		}
		_isRemoved = true;
		LogOriginCasualty(
			"external_campaign_casualty_handled",
			"source=" + (source ?? "N/A") + " " + BuildOriginTroopState(_troop));
	}

	public void SetRouted(bool isOrderRetreat)
	{
	}

	public void OnAgentRemoved(float agentHealth)
	{
		LogOriginCasualty("origin_on_agent_removed_begin", "agent_health=" + agentHealth + " " + BuildOriginTroopState(_troop));
		if (_troop.IsHero && !_troop.HeroObject.IsDead)
		{
			_troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
		}
		LogOriginCasualty("origin_on_agent_removed_end", "agent_health=" + agentHealth + " " + BuildOriginTroopState(_troop));
	}

	private static void LogOriginCasualty(string tag, string detail)
	{
		try
		{
			if (_originCasualtyDiagLogCount >= OriginCasualtyDiagLogLimit)
			{
				return;
			}
			_originCasualtyDiagLogCount++;
			TroopInspectionBehavior.Log("prisoner_origin_diag sample=" + _originCasualtyDiagLogCount + " tag=" + tag + " " + detail);
		}
		catch
		{
		}
	}

	private static string BuildOriginTroopState(CharacterObject troop)
	{
		try
		{
			if (troop == null)
			{
				return "troop=null";
			}
			Hero hero = troop.HeroObject;
			return "troop=" + troop.StringId + ",hero=" + troop.IsHero + ",hero_state=" + BuildOriginHeroState(hero) + ",roster_state=" + BuildPrisonRosterState(troop);
		}
		catch (Exception ex)
		{
			return "troop_state_error=" + ex.GetType().Name + ":" + ex.Message;
		}
	}

	private static string BuildOriginHeroState(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return "null";
			}
			return "alive=" + hero.IsAlive + ",dead=" + hero.IsDead + ",wounded=" + hero.IsWounded + ",hp=" + hero.HitPoints + "/" + hero.MaxHitPoints + ",wounded_limit=" + hero.WoundedHealthLimit;
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name;
		}
	}

	private static string BuildPrisonRosterState(CharacterObject troop)
	{
		try
		{
			TroopRoster roster = PartyBase.MainParty?.PrisonRoster;
			if (roster == null || troop == null)
			{
				return "null";
			}
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				if (element.Character == troop)
				{
					return "n=" + element.Number + ",w=" + element.WoundedNumber;
				}
			}
			return "missing";
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name;
		}
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

[HarmonyPatch(typeof(SandBox.GameComponents.SandboxAgentDecideKilledOrUnconsciousModel), "GetAgentStateProbability")]
public static class TroopInspectionDeathRatePatch
{
	private const float InspectionDeathRatePercent = 100f;

	private const int DeathRateDiagLogLimit = 24;

	private static int _deathRateDiagLogCount;

	public static void Postfix(Agent affectorAgent, Agent effectedAgent, DamageTypes damageType, ref float __result)
	{
		try
		{
			if (Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>() == null)
			{
				return;
			}
			if (effectedAgent == null || !effectedAgent.IsHuman)
			{
				return;
			}
			float vanilla = __result;
			if (IsMainPlayerAgent(effectedAgent))
			{
				__result = 0f;
				LogDeathRateSample(affectorAgent, effectedAgent, damageType, vanilla, __result, "main_player_protected");
				return;
			}
			float slider = InspectionDeathRatePercent / 100f;
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
			LogDeathRateSample(affectorAgent, effectedAgent, damageType, vanilla, __result, "inspection_death_rate_max");
		}
		catch (Exception ex)
		{
			if (_deathRateDiagLogCount < DeathRateDiagLogLimit)
			{
				_deathRateDiagLogCount++;
				TroopInspectionBehavior.Log("death_rate_diag_failed sample=" + _deathRateDiagLogCount + " " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	private static bool IsMainPlayerAgent(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return false;
			}
			if (agent.IsMainAgent)
			{
				return true;
			}
			CharacterObject character = agent.Character as CharacterObject;
			return character != null && character.IsPlayerCharacter;
		}
		catch
		{
			return false;
		}
	}

	private static void LogDeathRateSample(Agent affectorAgent, Agent effectedAgent, DamageTypes damageType, float vanilla, float adjusted, string reason)
	{
		if (_deathRateDiagLogCount >= DeathRateDiagLogLimit)
		{
			return;
		}
		bool isPrisoner = TroopInspectionMissionLogic.TryResolveInspectionPrisoner(effectedAgent, out bool isLord);
		CharacterObject victimCharacter = effectedAgent?.Character as CharacterObject;
		CharacterObject affectorCharacter = affectorAgent?.Character as CharacterObject;
		_deathRateDiagLogCount++;
		TroopInspectionBehavior.Log("death_rate_diag sample=" + _deathRateDiagLogCount
			+ " victim=" + SafeAgentCharacterId(effectedAgent)
			+ " victim_origin=" + SafeAgentOriginType(effectedAgent)
			+ " victim_hero=" + (victimCharacter?.IsHero.ToString() ?? "null")
			+ " victim_player=" + (((effectedAgent != null && effectedAgent.IsMainAgent) || (victimCharacter?.IsPlayerCharacter ?? false)).ToString())
			+ " victim_hero_state=" + SafeHeroState(victimCharacter?.HeroObject)
			+ " prisoner=" + isPrisoner
			+ " is_lord=" + isLord
			+ " damage_type=" + damageType
			+ " vanilla=" + vanilla
			+ " adjusted=" + adjusted
			+ " reason=" + reason
			+ " affector=" + SafeAgentCharacterId(affectorAgent)
			+ " affector_origin=" + SafeAgentOriginType(affectorAgent)
			+ " affector_hero=" + (affectorCharacter?.IsHero.ToString() ?? "null")
			+ " affector_main=" + ((affectorAgent != null) ? affectorAgent.IsMainAgent.ToString() : "null"));
	}

	private static string SafeAgentOriginType(Agent agent)
	{
		try
		{
			return agent?.Origin?.GetType().Name ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeHeroState(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return "null";
			}
			return "alive=" + hero.IsAlive + ",dead=" + hero.IsDead + ",wounded=" + hero.IsWounded + ",hp=" + hero.HitPoints + "/" + hero.MaxHitPoints + ",wounded_limit=" + hero.WoundedHealthLimit;
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name;
		}
	}

	private static string SafeAgentCharacterId(Agent agent)
	{
		try
		{
			CharacterObject character = agent?.Character as CharacterObject;
			return character?.StringId ?? character?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}
}

[HarmonyPatch(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase")]
public static class TroopInspectionMeleeDamagePatch
{
	private const int DamageCancelDiagLogLimit = 24;

	private static int _damageCancelDiagLogCount;

	public static bool Prefix(Mission __instance, Agent attacker, Agent victim, ref bool __result)
	{
		if (attacker == null || victim == null)
		{
			return true;
		}
		if (__instance?.GetMissionBehavior<TroopInspectionMissionLogic>() == null)
		{
			return true;
		}
		bool willOverride = attacker.IsHuman && victim.IsHuman && attacker.IsFriendOf(victim);
		if (_damageCancelDiagLogCount < DamageCancelDiagLogLimit && TroopInspectionMissionLogic.TryResolveInspectionPrisoner(victim, out bool isLord))
		{
			_damageCancelDiagLogCount++;
			TroopInspectionBehavior.Log("damage_cancel_diag sample=" + _damageCancelDiagLogCount + " victim=" + SafeAgentCharacterId(victim) + " is_lord=" + isLord + " attacker=" + SafeAgentCharacterId(attacker) + " attacker_human=" + attacker.IsHuman + " attacker_main=" + attacker.IsMainAgent + " victim_human=" + victim.IsHuman + " attacker_friend=" + attacker.IsFriendOf(victim) + " override=" + willOverride);
		}
		if (!willOverride)
		{
			return true;
		}
		__result = false;
		return false;
	}

	private static string SafeAgentCharacterId(Agent agent)
	{
		try
		{
			CharacterObject character = agent?.Character as CharacterObject;
			return character?.StringId ?? character?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}
}

[HarmonyPatch]
public static class TroopInspectionFormationIsolationPatch
{
	private static bool IsTroopInspectionRuntime()
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>() != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsReservedFormation(Formation formation)
	{
		if (formation == null)
		{
			return false;
		}
		return formation.Index == 6 || formation.Index == 7;
	}

	private static Formation ResolveAllowedFormation(Agent agent, Formation requestedFormation)
	{
		if (agent == null || requestedFormation == null || !IsReservedFormation(requestedFormation))
		{
			return requestedFormation;
		}
		Team team = requestedFormation.Team;
		if (team == null)
		{
			return null;
		}
		if (TroopInspectionMissionLogic.TryResolveInspectionPrisoner(agent, out bool isLord))
		{
			int expectedIndex = isLord ? 7 : 6;
			if (requestedFormation.Index == expectedIndex)
			{
				return requestedFormation;
			}
			try
			{
				return team.GetFormation((FormationClass)expectedIndex);
			}
			catch
			{
				return requestedFormation;
			}
		}
		return TroopInspectionMissionLogic.ResolveNormalTroopFormation(team, agent, requestedFormation);
	}

	[HarmonyPatch(typeof(Agent), "set_Formation")]
	[HarmonyPrefix]
	private static void AgentSetFormationPrefix(Agent __instance, ref Formation value)
	{
		if (!IsTroopInspectionRuntime() || !IsReservedFormation(value))
		{
			return;
		}
		Formation formation = ResolveAllowedFormation(__instance, value);
		value = formation;
	}

	[HarmonyPatch(typeof(Formation), "AddUnit")]
	[HarmonyPrefix]
	private static bool FormationAddUnitPrefix(Formation __instance, Agent unit)
	{
		if (!IsTroopInspectionRuntime() || !IsReservedFormation(__instance) || unit == null)
		{
			return true;
		}
		Formation formation = ResolveAllowedFormation(unit, __instance);
		if (formation == __instance)
		{
			return true;
		}
		if (formation != null)
		{
			TroopInspectionMissionLogic.TryMoveAgentToFormation(unit, formation);
		}
		else
		{
			try
			{
				unit.Formation = null;
			}
			catch
			{
			}
		}
		return false;
	}
}

[HarmonyPatch]
public static class TroopInspectionOrderOfBattlePatch
{
	private static readonly FieldInfo AllFormationsField = AccessTools.Field(typeof(OrderOfBattleVM), "_allFormations");

	private static readonly FieldInfo ClassBelongedFormationItemField = AccessTools.Field(typeof(OrderOfBattleFormationClassVM), "BelongedFormationItem");

	[HarmonyPatch(typeof(OrderOfBattleFormationItemVM), nameof(OrderOfBattleFormationItemVM.RefreshFormation), new Type[] { typeof(Formation), typeof(DeploymentFormationClass), typeof(bool) })]
	[HarmonyPrefix]
	private static void RefreshFormationPrefix(Formation formation, ref DeploymentFormationClass overriddenClass, ref bool mustExist)
	{
		if (!IsTroopInspectionRuntime() || formation == null)
		{
			return;
		}
		if (formation.Index == 6)
		{
			overriddenClass = DeploymentFormationClass.Infantry;
			mustExist = true;
		}
		else if (formation.Index == 7)
		{
			overriddenClass = DeploymentFormationClass.Cavalry;
			mustExist = true;
		}
	}

	[HarmonyPatch(typeof(OrderOfBattleFormationItemVM), nameof(OrderOfBattleFormationItemVM.RefreshFormation), new Type[] { typeof(Formation), typeof(DeploymentFormationClass), typeof(bool) })]
	[HarmonyPostfix]
	private static void RefreshFormationPostfix(OrderOfBattleFormationItemVM __instance)
	{
		if (IsTroopInspectionRuntime())
		{
			LockPrisonerFormationItem(__instance);
		}
	}

	[HarmonyPatch(typeof(OrderOfBattleFormationClassVM), "UpdateWeightAdjustable")]
	[HarmonyPostfix]
	private static void UpdateWeightAdjustablePostfix(OrderOfBattleFormationClassVM __instance)
	{
		if (IsTroopInspectionRuntime() && IsPrisonerFormationClass(__instance))
		{
			LockPrisonerFormationClass(__instance);
		}
	}

	[HarmonyPatch(typeof(OrderOfBattleFormationClassVM), "OnWeightAdjusted")]
	[HarmonyPrefix]
	private static bool OnWeightAdjustedPrefix(OrderOfBattleFormationClassVM __instance)
	{
		if (!IsTroopInspectionRuntime() || !IsPrisonerFormationClass(__instance))
		{
			return true;
		}
		LockPrisonerFormationClass(__instance);
		return false;
	}

	[HarmonyPatch(typeof(OrderOfBattleVM), "EnsureAllFormationTypesAreSet")]
	[HarmonyPrefix]
	private static bool EnsureAllFormationTypesAreSetPrefix(OrderOfBattleFormationItemVM f)
	{
		if (!IsTroopInspectionRuntime() || f?.Formation == null)
		{
			return true;
		}
		int index = f.Formation.Index;
		return index != 6 && index != 7;
	}

	[HarmonyPatch(typeof(OrderOfBattleVM), nameof(OrderOfBattleVM.Tick))]
	[HarmonyPostfix]
	private static void TickPostfix(OrderOfBattleVM __instance)
	{
		if (!IsTroopInspectionRuntime() || __instance == null)
		{
			return;
		}
		try
		{
			List<OrderOfBattleFormationItemVM> allFormations = AllFormationsField?.GetValue(__instance) as List<OrderOfBattleFormationItemVM>;
			if (allFormations == null)
			{
				return;
			}
			RefreshPrisonerFormationItem(allFormations, 6, DeploymentFormationClass.Infantry);
			RefreshPrisonerFormationItem(allFormations, 7, DeploymentFormationClass.Cavalry);
		}
		catch
		{
		}
	}

	private static void RefreshPrisonerFormationItem(List<OrderOfBattleFormationItemVM> allFormations, int formationIndex, DeploymentFormationClass deploymentClass)
	{
		for (int i = 0; i < allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM item = allFormations[i];
			Formation formation = item?.Formation;
			if (formation == null || formation.Index != formationIndex)
			{
				continue;
			}
			int actualCount = formation.CountOfUnits;
			if (actualCount > 0 && (item.OrderOfBattleFormationClassInt == 0 || item.TroopCount != actualCount || !item.IsSelectable))
			{
				item.RefreshFormation(formation, deploymentClass, mustExist: true);
				item.OnSizeChanged();
			}
			LockPrisonerFormationItem(item);
			return;
		}
	}

	private static bool IsPrisonerFormationItem(OrderOfBattleFormationItemVM item)
	{
		Formation formation = item?.Formation;
		return formation != null && (formation.Index == 6 || formation.Index == 7);
	}

	private static bool IsPrisonerFormationClass(OrderOfBattleFormationClassVM formationClass)
	{
		return IsPrisonerFormationItem(TryGetBelongedFormationItem(formationClass));
	}

	private static OrderOfBattleFormationItemVM TryGetBelongedFormationItem(OrderOfBattleFormationClassVM formationClass)
	{
		try
		{
			return ClassBelongedFormationItemField?.GetValue(formationClass) as OrderOfBattleFormationItemVM;
		}
		catch
		{
			return null;
		}
	}

	private static void LockPrisonerFormationItem(OrderOfBattleFormationItemVM item)
	{
		if (!IsPrisonerFormationItem(item))
		{
			return;
		}
		try
		{
			if (item.Classes != null)
			{
				foreach (OrderOfBattleFormationClassVM formationClass in item.Classes)
				{
					LockPrisonerFormationClass(formationClass);
				}
			}
		}
		catch
		{
		}
	}

	private static void LockPrisonerFormationClass(OrderOfBattleFormationClassVM formationClass)
	{
		if (formationClass == null)
		{
			return;
		}
		try
		{
			formationClass.SetWeightAdjustmentLock(true);
		}
		catch
		{
		}
		try
		{
			formationClass.IsAdjustable = false;
			formationClass.IsLocked = true;
			formationClass.UpdateTroopCountText();
		}
		catch
		{
		}
	}

	private static bool IsTroopInspectionRuntime()
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>() != null;
		}
		catch
		{
			return false;
		}
	}
}
