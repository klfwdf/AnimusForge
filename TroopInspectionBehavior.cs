using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Helpers;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public static class TroopInspectionBehavior
{
	private sealed class TroopInspectionRuntime
	{
		public TroopRoster InspectionRoster { get; set; }

		public TroopRoster InspectionPrisonerRoster { get; set; }

		public TroopRoster HoldingRoster { get; set; }

		public TroopRoster HoldingPrisonerRoster { get; set; }

		public string InspectionSummary { get; set; }

		public string HoldingSummary { get; set; }

		public int PlayerOriginalHitPoints { get; set; }

		public bool PlayerOriginalWasWounded { get; set; }

		public MobileParty HoldingDummyParty { get; set; }
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

		public int Errors;

		public int TotalMen => RegularMen + Heroes;

		public override string ToString()
		{
			return $"regular={RegularMen},heroes={Heroes},total={TotalMen},wounded={RegularWounded},xp={RegularXp},dead_heroes_skipped={DeadHeroesSkipped},errors={Errors}";
		}
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

	private sealed class TroopInspectionDummyPartyComponent : PartyComponent
	{
		private readonly CampaignVec2 _position;

		private readonly TextObject _name;

		private readonly Hero _owner;

		private readonly Clan _clan;

		public override Hero PartyOwner => _owner;

		public override TextObject Name => _name;

		public override Settlement HomeSettlement => null;

		public override bool AvoidHostileActions => true;

		public TroopInspectionDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			_position = position;
			_name = name;
			_owner = owner;
			_clan = clan;
		}

		public override Banner GetDefaultComponentBanner()
		{
			Clan clan = _clan;
			if (clan == null)
			{
				return null;
			}
			return clan.Banner;
		}

		protected override void OnInitialize()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			((PartyComponent)this).MobileParty.ActualClan = _clan;
			((PartyComponent)this).MobileParty.InitializeMobilePartyAroundPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), _position, 0f, 0f, !_position.IsOnLand);
			((PartyComponent)this).MobileParty.SetMoveModeHold();
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
	private const string LogPrefix = "TroopInspection";

	private const string DummyPartyPrefix = "animusforge_troop_inspection_dummy_";

	private const string HoldingDummyPartyPrefix = "animusforge_troop_inspection_holding_";

	private const string SelectionPoolDummyPartyPrefix = "animusforge_troop_inspection_selection_pool_";

	private static TroopInspectionRuntime _runtime;

	private static PendingSelection _pendingSelection;

	private static MobileParty _dummyParty;

	private static MobileParty _holdingDummyParty;

	private static MobileParty _selectionPoolDummyParty;

	private static MapEvent _mapEvent;

	private static string _dummyPartyStringId;

	private static string _holdingDummyPartyStringId;

	private static string _selectionPoolDummyPartyStringId;

	private static bool _isOpening;

	private static bool _cleanupDone;

	private static Mission _activeInspectionMission;

	private static bool _queuedOpenInspection;

	private static float _queuedOpenInspectionAt;

	private static string _inspectionLogPath;

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony != null)
		{
			TryPatchClass(harmony, typeof(TroopInspectionDeathRatePatch));
			TryPatchClass(harmony, typeof(TroopInspectionMeleeDamagePatch));
			TryPatchClass(harmony, typeof(TroopInspectionOrderOfBattlePatch));
			ReinforcementSystemCompatibility.EnsurePatched(harmony);
		}
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

	public static void OpenInspectionFromTerminal()
	{
		Log("terminal_open");
		if (_isOpening)
		{
			Display("检阅正在准备中。");
			Log("precheck blocked: already opening " + BuildOpenStateSummary(MobileParty.MainParty));
			return;
		}
		TryCleanupStaleInspectionStateBeforeOpen("terminal_open_stale_cleanup");
		_isOpening = true;
		_cleanupDone = false;
		_activeInspectionMission = null;
		_runtime = null;
		try
		{
			if (!CanOpenFromCurrentState(out var mainParty, out var blockedReason))
			{
				Display(blockedReason);
				Log("precheck blocked: " + blockedReason + " " + BuildOpenStateSummary(mainParty));
				ResetPendingSelection("precheck_blocked");
				return;
			}
			int num = CountHealthyNonPlayerTroops(PartyBase.MainParty.MemberRoster);
			Log($"precheck wounded={Hero.MainHero.IsWounded} healthy_non_player={num} {BuildOpenStateSummary(mainParty)}");
			if (Hero.MainHero.IsWounded)
			{
				Display("你受伤了，无法检阅部队。");
				ResetPendingSelection("player_wounded");
			}
			else if (num <= 0)
			{
				Display("没有可检阅的健康士兵。");
				ResetPendingSelection("no_healthy_troops");
			}
			else if (PlayerEncounter.Current != null || MapEvent.PlayerMapEvent != null || mainParty.MapEvent != null)
			{
				Display("当前遭遇状态无法检阅部队。");
				Log("precheck blocked: existing encounter or player map event " + BuildOpenStateSummary(mainParty));
				ResetPendingSelection("existing_encounter");
			}
			else
			{
				ReinforcementSystemCompatibility.EnsurePatched();
				OpenInspectionTeamSelection(mainParty);
			}
		}
		catch (Exception ex)
		{
			Log("open failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			ResetPendingSelection("open_exception");
			Display("打开检阅士兵失败。");
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
			}
			else if (!((float)Environment.TickCount / 1000f < _queuedOpenInspectionAt) && !IsPartyScreenStillActive() && Mission.Current == null)
			{
				_queuedOpenInspection = false;
				_isOpening = true;
				if (!CanOpenFromCurrentState(out var mainParty, out var blockedReason))
				{
					Display(blockedReason);
					CleanupRuntime("queued_open_blocked");
					_isOpening = false;
				}
				else
				{
					OpenInspectionMissionAfterSelection(mainParty);
					_isOpening = false;
					Display("士兵检阅开始。");
				}
			}
		}
		catch (Exception ex)
		{
			Log("queued inspection_open failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			CleanupRuntime("queued_open_failed");
			_isOpening = false;
			_queuedOpenInspection = false;
			Display("打开检阅士兵失败。");
		}
	}

	internal static bool IsCurrentInspectionRuntime(string dummyPartyStringId)
	{
		if (!string.IsNullOrEmpty(dummyPartyStringId))
		{
			return string.Equals(dummyPartyStringId, _dummyPartyStringId, StringComparison.Ordinal);
		}
		return false;
	}

	internal static bool ShouldSuppressReinforcementSystem(Mission mission)
	{
		if (mission == null)
		{
			return false;
		}
		if (_isOpening || mission == _activeInspectionMission)
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

	internal static void CleanupRuntime(string reason)
	{
		bool alreadyDone = _cleanupDone;
		_cleanupDone = true;
		Log("cleanup begin reason=" + reason + " already_done=" + alreadyDone + " " + BuildOpenStateSummary(MobileParty.MainParty));
		MobileParty holdingDummyParty = _holdingDummyParty;
		MobileParty dummyParty = _dummyParty;
		string dummyPartyStringId = _dummyPartyStringId;
		MapEvent mapEvent = ResolveInspectionMapEvent();
		try
		{
			RestoreAndDestroyHoldingDummyParty(holdingDummyParty, "inspection_holding_cleanup");
			CleanupOrphanHoldingDummyParties(holdingDummyParty, "inspection_holding_orphan_cleanup");
		}
		catch (Exception ex)
		{
			Log("cleanup holding_party failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		DestroySelectionPoolDummyParty("cleanup_runtime");
		CleanupOrphanSelectionPoolDummyParties("cleanup_selection_pool_orphan");
		DestroyInspectionDummyParty(dummyParty, dummyPartyStringId, "inspection_dummy_cleanup");
		CleanupOrphanInspectionDummyParties(dummyParty, "inspection_dummy_orphan_cleanup");
		CleanupMapEventAndPlayerEncounter(mapEvent, reason);
		_activeInspectionMission = null;
		_pendingSelection = null;
		_queuedOpenInspection = false;
		_isOpening = false;
		_runtime = null;
		_holdingDummyParty = null;
		_holdingDummyPartyStringId = null;
		_selectionPoolDummyParty = null;
		_selectionPoolDummyPartyStringId = null;
		_dummyParty = null;
		_dummyPartyStringId = null;
		_mapEvent = null;
		Log("cleanup end reason=" + reason + " " + BuildOpenStateSummary(MobileParty.MainParty));
	}


	private static void TryCleanupStaleInspectionStateBeforeOpen(string reason)
	{
		try
		{
			if (Mission.Current != null)
			{
				return;
			}
			MapEvent playerEncounterMapEvent = null;
			if (PlayerEncounter.Current != null)
			{
				playerEncounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
			}
			bool hasStaleState = _runtime != null || _pendingSelection != null || _queuedOpenInspection || _isOpening || _activeInspectionMission != null || _dummyParty != null || _holdingDummyParty != null || _selectionPoolDummyParty != null || _mapEvent != null;
			bool hasInspectionEncounter = IsInspectionMapEvent(playerEncounterMapEvent) || IsInspectionMapEvent(MapEvent.PlayerMapEvent) || IsInspectionMapEvent(MobileParty.MainParty?.MapEvent);
			bool hasInspectionDummy = HasActiveInspectionDummyParty();
			bool hasEmptyStaleEncounter = _cleanupDone && PlayerEncounter.Current != null && playerEncounterMapEvent == null && MapEvent.PlayerMapEvent == null && MobileParty.MainParty?.MapEvent == null;
			if (hasStaleState || hasInspectionEncounter || hasInspectionDummy || hasEmptyStaleEncounter)
			{
				Log("stale cleanup before open reason=" + reason + " stale_state=" + hasStaleState + " inspection_encounter=" + hasInspectionEncounter + " inspection_dummy=" + hasInspectionDummy + " empty_stale_encounter=" + hasEmptyStaleEncounter + " " + BuildOpenStateSummary(MobileParty.MainParty));
				if (_mapEvent == null)
				{
					_mapEvent = ResolveInspectionMapEvent();
				}
				CleanupRuntime(reason);
			}
		}
		catch (Exception ex)
		{
			Log("stale cleanup before open failed: " + ex.GetType().Name + ": " + ex.Message);
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
			foreach (MobileParty party2 in parties)
			{
				RestoreAndDestroyHoldingDummyParty(party2, label);
			}
		}
		catch (Exception ex)
		{
			Log("cleanup orphan holding failed: " + ex.GetType().Name + ": " + ex.Message);
		}
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
			foreach (MobileParty party2 in parties)
			{
				ClearRosterDirect(party2.MemberRoster);
				ClearRosterDirect(party2.PrisonRoster);
				DestroyPartyAction.Apply((PartyBase)null, party2);
				Log("selection_pool_orphan_destroyed label=" + label + " id=" + (((MBObjectBase)party2).StringId ?? "null"));
			}
		}
		catch (Exception ex)
		{
			Log("cleanup orphan selection_pool failed: " + ex.GetType().Name + ": " + ex.Message);
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
			string id = ((MBObjectBase)dummyParty).StringId ?? "";
			if (dummyParty.IsActive && id.StartsWith("animusforge_troop_inspection_dummy_", StringComparison.Ordinal) && (string.IsNullOrEmpty(expectedStringId) || string.Equals(id, expectedStringId, StringComparison.Ordinal) || id.StartsWith("animusforge_troop_inspection_dummy_", StringComparison.Ordinal)))
			{
				DestroyPartyAction.Apply((PartyBase)null, dummyParty);
				Log("cleanup dummy_party_destroyed label=" + label + " id=" + id);
			}
			else
			{
				Log("cleanup dummy_party_destroy_skipped label=" + label + " active=" + dummyParty.IsActive + " id=" + id);
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
			foreach (MobileParty party2 in parties)
			{
				DestroyInspectionDummyParty(party2, null, label);
			}
		}
		catch (Exception ex)
		{
			Log("cleanup orphan dummy failed: " + ex.GetType().Name + ": " + ex.Message);
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
			if (IsInspectionMapEvent(MobileParty.MainParty?.MapEvent))
			{
				return MobileParty.MainParty.MapEvent;
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
				Log("cleanup mapevent reason=" + reason + " " + MapEventRuntimeSummary(mapEvent));
				if (!mapEvent.IsFinalized)
				{
					mapEvent.ResetBattleState();
					mapEvent.FinalizeEvent();
					Log("cleanup map_event_finalized reason=" + reason);
				}
				else
				{
					Log("cleanup map_event_already_finalized reason=" + reason);
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
				else
				{
					Log("cleanup player_encounter_skipped reason=" + reason + " current=" + MapEventRuntimeSummary(currentEncounterMapEvent) + " ours=" + MapEventRuntimeSummary(mapEvent));
				}
			}
		}
		catch (Exception ex2)
		{
			Log("cleanup player_encounter failed: " + ex2.GetType().Name + ": " + ex2.Message);
		}
	}

	private static bool HasActiveInspectionDummyParty()
	{
		try
		{
			foreach (MobileParty party in MobileParty.All)
			{
				if (party != null && party.IsActive && IsAnyInspectionDummyParty(party))
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
				if (IsAnyInspectionDummyParty(mobileParty))
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

	private static bool IsAnyInspectionDummyParty(MobileParty party)
	{
		return IsInspectionDummyParty(party) || IsInspectionHoldingDummyParty(party) || IsInspectionSelectionPoolDummyParty(party);
	}

	private static bool IsInspectionDummyParty(MobileParty party)
	{
		return PartyIdStartsWith(party, "animusforge_troop_inspection_dummy_");
	}

	private static bool IsInspectionHoldingDummyParty(MobileParty party)
	{
		return PartyIdStartsWith(party, "animusforge_troop_inspection_holding_");
	}

	private static bool IsInspectionSelectionPoolDummyParty(MobileParty party)
	{
		return PartyIdStartsWith(party, "animusforge_troop_inspection_selection_pool_");
	}

	private static bool PartyIdStartsWith(MobileParty party, string prefix)
	{
		try
		{
			string id = ((MBObjectBase)party)?.StringId ?? "";
			return id.StartsWith(prefix, StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static string BuildOpenStateSummary(MobileParty mainParty)
	{
		try
		{
			MapEvent encounterMapEvent = null;
			if (PlayerEncounter.Current != null)
			{
				encounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
			}
			return "mission_current=" + (Mission.Current != null)
				+ ",party_screen=" + IsPartyScreenStillActive()
				+ ",player_encounter=" + (PlayerEncounter.Current != null)
				+ ",encounter_map={" + MapEventRuntimeSummary(encounterMapEvent) + "}"
				+ ",player_mapevent={" + MapEventRuntimeSummary(MapEvent.PlayerMapEvent) + "}"
				+ ",main_mapevent={" + MapEventRuntimeSummary(mainParty?.MapEvent) + "}"
				+ ",is_opening=" + _isOpening
				+ ",queued=" + _queuedOpenInspection
				+ ",active_mission=" + (_activeInspectionMission != null)
				+ ",cleanup_done=" + _cleanupDone
				+ ",runtime=" + (_runtime != null)
				+ ",pending_selection=" + (_pendingSelection != null)
				+ ",dummy={" + PartyRuntimeSummary(_dummyParty) + "}"
				+ ",holding={" + PartyRuntimeSummary(_holdingDummyParty) + "}"
				+ ",selection_pool={" + PartyRuntimeSummary(_selectionPoolDummyParty) + "}";
		}
		catch (Exception ex)
		{
			return "state_summary_error=" + ex.GetType().Name + ":" + ex.Message;
		}
	}

	private static string PartyRuntimeSummary(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return "id=" + (((MBObjectBase)party).StringId ?? "null")
				+ ",active=" + party.IsActive
				+ ",visible=" + party.IsVisible
				+ ",has_mapevent=" + (party.MapEvent != null)
				+ ",members=" + ((party.MemberRoster != null) ? party.MemberRoster.TotalManCount : -1)
				+ ",prisoners=" + ((party.PrisonRoster != null) ? party.PrisonRoster.TotalManCount : -1);
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
			return "state=" + mapEvent.State
				+ ",battle_state=" + mapEvent.BattleState
				+ ",finalized=" + mapEvent.IsFinalized
				+ ",has_winner=" + mapEvent.HasWinner
				+ ",is_player_mapevent=" + object.ReferenceEquals(MapEvent.PlayerMapEvent, mapEvent)
				+ ",is_inspection=" + IsInspectionMapEvent(mapEvent);
		}
		catch (Exception ex)
		{
			return "error=" + ex.GetType().Name + ":" + ex.Message;
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
			if (Campaign.Current.ConversationManager != null && Campaign.Current.ConversationManager.IsConversationInProgress)
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
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		if (roster == null)
		{
			return 0;
		}
		foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)roster.GetTroopRoster())
		{
			TroopRosterElement current = item;
			CharacterObject character = current.Character;
			if (character != null && !((BasicCharacterObject)character).IsPlayerCharacter)
			{
				num += Math.Max(0, current.Number - current.WoundedNumber);
			}
		}
		return num;
	}

	private static void OpenInspectionTeamSelection(MobileParty mainParty)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_0130: Expected O, but got Unknown
		//IL_0130: Expected O, but got Unknown
		//IL_0130: Expected O, but got Unknown
		//IL_0130: Expected O, but got Unknown
		TroopRoster val = BuildSelectableRoster(mainParty.MemberRoster);
		MobileParty mainParty2 = MobileParty.MainParty;
		object obj = ((mainParty2 != null) ? mainParty2.PrisonRoster : null);
		if (obj == null)
		{
			PartyBase mainParty3 = PartyBase.MainParty;
			obj = ((mainParty3 != null) ? mainParty3.PrisonRoster : null);
		}
		TroopRoster val2 = BuildSelectablePrisonerRoster((TroopRoster)obj);
		TroopRoster val3 = TroopRoster.CreateDummyTroopRoster();
		TroopRoster rightPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
		AddPlayerToInspectionRoster(val3);
		MobileParty val4 = CreateInspectionSelectionPoolDummyParty(mainParty, val, val2);
		PendingSelection obj2 = new PendingSelection
		{
			PlayerOriginalHitPoints = GetMainHeroHitPoints()
		};
		Hero mainHero = Hero.MainHero;
		obj2.PlayerOriginalWasWounded = mainHero != null && mainHero.IsWounded;
		_pendingSelection = obj2;
		TroopRoster memberRoster = val4.MemberRoster;
		TroopRoster prisonRoster = val4.PrisonRoster;
		TextObject val5 = new TextObject("可选成员 / 未参加检阅");
		TextObject val6 = new TextObject("检阅队（玩家固定属于此队）");
		int leftMemberLimit = Math.Max(val.TotalManCount, 0);
		int leftPrisonerLimit = Math.Max(val2.TotalManCount, 0);
		PartyBase party = mainParty.Party;
		int rightMemberLimit = Math.Max((party != null) ? party.PartySizeLimit : (val.TotalManCount + 1), val.TotalManCount + 1);
		PartyBase mainParty4 = PartyBase.MainParty;
		OpenInspectionSelectionScreen(val4, memberRoster, prisonRoster, val3, rightPrisonerRoster, val5, val6, leftMemberLimit, leftPrisonerLimit, rightMemberLimit, Math.Max((mainParty4 != null) ? mainParty4.PrisonerSizeLimit : val2.TotalManCount, val2.TotalManCount), new PartyPresentationDoneButtonConditionDelegate(InspectionTeamDoneCondition), new PartyScreenClosedDelegate(OnInspectionTeamScreenClosed), new IsTroopTransferableDelegate(TroopInspectionTroopTransferableDelegate));
		object arg = val4.MemberRoster.TotalManCount;
		object arg2 = val4.PrisonRoster.TotalManCount;
		MobileParty mainParty5 = MobileParty.MainParty;
		int? obj3;
		if (mainParty5 == null)
		{
			obj3 = null;
		}
		else
		{
			TroopRoster prisonRoster2 = mainParty5.PrisonRoster;
			obj3 = ((prisonRoster2 != null) ? new int?(prisonRoster2.TotalManCount) : ((int?)null));
		}
		int? num = obj3;
		int num2;
		if (!num.HasValue)
		{
			PartyBase mainParty6 = PartyBase.MainParty;
			int? obj4;
			if (mainParty6 == null)
			{
				obj4 = null;
			}
			else
			{
				TroopRoster prisonRoster3 = mainParty6.PrisonRoster;
				obj4 = ((prisonRoster3 != null) ? new int?(prisonRoster3.TotalManCount) : ((int?)null));
			}
			num2 = obj4 ?? (-1);
		}
		else
		{
			num2 = num.GetValueOrDefault();
		}
		Log($"selection_screen_open left_members={arg} left_prisoners={arg2} source_prisoners={num2}");
	}

	private static void OpenInspectionSelectionScreen(MobileParty leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, TextObject leftPartyName, TextObject rightPartyName, int leftMemberLimit, int leftPrisonerLimit, int rightMemberLimit, int rightPrisonerLimit, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		PartyScreenLogic val = new PartyScreenLogic();
		PartyScreenLogicInitializationData val2 = new PartyScreenLogicInitializationData
		{
			LeftOwnerParty = ((leftOwnerParty != null) ? leftOwnerParty.Party : null)
		};
		MobileParty mainParty = MobileParty.MainParty;
		val2.RightOwnerParty = ((mainParty != null) ? mainParty.Party : null);
		val2.LeftMemberRoster = leftMemberRoster ?? TroopRoster.CreateDummyTroopRoster();
		val2.LeftPrisonerRoster = leftPrisonerRoster ?? TroopRoster.CreateDummyTroopRoster();
		val2.RightMemberRoster = rightMemberRoster ?? TroopRoster.CreateDummyTroopRoster();
		val2.RightPrisonerRoster = rightPrisonerRoster ?? TroopRoster.CreateDummyTroopRoster();
		val2.LeftLeaderHero = ((leftOwnerParty != null) ? leftOwnerParty.LeaderHero : null);
		PartyBase mainParty2 = PartyBase.MainParty;
		val2.RightLeaderHero = ((mainParty2 != null) ? mainParty2.LeaderHero : null);
		val2.LeftPartyMembersSizeLimit = Math.Max(0, leftMemberLimit);
		val2.LeftPartyPrisonersSizeLimit = Math.Max(0, leftPrisonerLimit);
		val2.RightPartyMembersSizeLimit = Math.Max(1, rightMemberLimit);
		val2.RightPartyPrisonersSizeLimit = Math.Max(0, rightPrisonerLimit);
		val2.LeftPartyName = leftPartyName;
		val2.RightPartyName = rightPartyName;
		val2.TroopTransferableDelegate = isTroopTransferable;
		val2.CanTalkToTroopDelegate = null;
		val2.PartyPresentationDoneButtonDelegate = InspectionSelectionDoneHandler;
		val2.PartyPresentationDoneButtonConditionDelegate = doneButtonCondition;
		val2.PartyPresentationCancelButtonActivateDelegate = null;
		val2.PartyPresentationCancelButtonDelegate = null;
		val2.PartyScreenClosedDelegate = onPartyScreenClosed;
		val2.IsDismissMode = false;
		val2.IsTroopUpgradesDisabled = true;
		val2.Header = null;
		val2.TransferHealthiesGetWoundedsFirst = true;
		val2.ShowProgressBar = false;
		val2.MemberTransferState = PartyScreenLogic.TransferState.Transferable;
		val2.PrisonerTransferState = PartyScreenLogic.TransferState.Transferable;
		val2.AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
		val2.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
		PartyScreenLogicInitializationData val4 = val2;
		val.Initialize(val4);
		PartyState val5 = Game.Current.GameStateManager.CreateState<PartyState>();
		val5.PartyScreenLogic = val;
		val5.IsDonating = false;
		val5.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
		Game.Current.GameStateManager.PushState((GameState)(object)val5, 0);
	}

	private static MobileParty CreateInspectionSelectionPoolDummyParty(MobileParty mainParty, TroopRoster members, TroopRoster prisoners)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		DestroySelectionPoolDummyParty("recreate_selection_pool");
		CampaignVec2 position = mainParty.Position;
		Vec2 val = ResolveEncounterDirection(mainParty);
		CampaignVec2 position2 = position + val * 0.2f;
		_selectionPoolDummyPartyStringId = "animusforge_troop_inspection_selection_pool_" + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		_selectionPoolDummyParty = MobileParty.CreateParty(_selectionPoolDummyPartyStringId, new TroopInspectionDummyPartyComponent(position2, new TextObject("可选成员 / 未参加检阅"), Hero.MainHero, Clan.PlayerClan));
		if (_selectionPoolDummyParty == null)
		{
			throw new InvalidOperationException("Failed to create inspection selection pool dummy party.");
		}
		_selectionPoolDummyParty.IsVisible = false;
		_selectionPoolDummyParty.SetMoveModeHold();
		CopyRosterInto(members, _selectionPoolDummyParty.MemberRoster);
		CopyRosterInto(prisoners, _selectionPoolDummyParty.PrisonRoster);
		return _selectionPoolDummyParty;
	}

	private static void CopyRosterInto(TroopRoster sourceRoster, TroopRoster targetRoster)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (sourceRoster == null || targetRoster == null)
		{
			return;
		}
		foreach (TroopRosterElement item in SnapshotRoster(sourceRoster))
		{
			TroopRosterElement current = item;
			if (current.Character != null && current.Number > 0)
			{
				targetRoster.Add(current);
			}
		}
	}

	private static bool InspectionSelectionDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		return true;
	}

	private static Tuple<bool, TextObject> InspectionTeamDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		return new Tuple<bool, TextObject>(item1: true, TextObject.GetEmpty());
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
			TroopRoster val = BuildSelectionRosterFromUi(rightMemberRoster);
			TroopRoster sourceRoster = BuildPrisonerSelectionRosterFromUi(rightPrisonRoster);
			AddPlayerToInspectionRoster(val);
			TroopRoster sourceRoster2 = BuildSelectionRosterFromUi(leftMemberRoster);
			TroopRoster sourceRoster3 = BuildPrisonerSelectionRosterFromUi(leftPrisonRoster);
			DestroySelectionPoolDummyParty("selection_done");
			_runtime = new TroopInspectionRuntime
			{
				InspectionRoster = CloneRoster(val),
				InspectionPrisonerRoster = CloneRoster(sourceRoster),
				HoldingRoster = CloneRoster(sourceRoster2),
				HoldingPrisonerRoster = CloneRoster(sourceRoster3),
				PlayerOriginalHitPoints = _pendingSelection.PlayerOriginalHitPoints,
				PlayerOriginalWasWounded = _pendingSelection.PlayerOriginalWasWounded
			};
			_runtime.InspectionSummary = RosterSummary(_runtime.InspectionRoster) + ", prisoners=" + RosterSummary(_runtime.InspectionPrisonerRoster);
			_runtime.HoldingSummary = RosterSummary(_runtime.HoldingRoster) + ", prisoners=" + RosterSummary(_runtime.HoldingPrisonerRoster);
			Log("selection_done inspection=" + _runtime.InspectionSummary + " holding=" + _runtime.HoldingSummary);
			try
			{
				PrepareHoldingDummyAndSplitRoster(_runtime);
			}
			catch (Exception ex)
			{
				Log("split failed: " + ex.GetType().Name + ": " + ex.Message);
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
		catch (Exception ex2)
		{
			Log("inspection_team_screen failed: " + ex2.GetType().Name + ": " + ex2.Message);
			ResetPendingSelection("inspection_exception");
			Display("选择失败。");
		}
	}

	private static void PrepareHoldingDummyAndSplitRoster(TroopInspectionRuntime runtime)
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
		Hero mainHero = Hero.MainHero;
		runtime.PlayerOriginalWasWounded = mainHero != null && mainHero.IsWounded;
		Dictionary<CharacterObject, RosterTotals> beforeMemberTotals = BuildRosterTotals(mainParty.MemberRoster);
		PartyBase mainParty2 = PartyBase.MainParty;
		Dictionary<CharacterObject, RosterTotals> beforePrisonerTotals = BuildRosterTotals((mainParty2 != null) ? mainParty2.PrisonRoster : null);
		TroopRoster memberRoster = mainParty.MemberRoster;
		int beforeMainMen = ((memberRoster != null) ? memberRoster.TotalManCount : 0);
		PartyBase mainParty3 = PartyBase.MainParty;
		int? obj;
		if (mainParty3 == null)
		{
			obj = null;
		}
		else
		{
			TroopRoster prisonRoster = mainParty3.PrisonRoster;
			obj = ((prisonRoster != null) ? new int?(prisonRoster.TotalManCount) : ((int?)null));
		}
		int? num = obj;
		int valueOrDefault = num.GetValueOrDefault();
		CreateInspectionHoldingDummyParty(mainParty);
		runtime.HoldingDummyParty = _holdingDummyParty;
		MoveRosterFromMainParty(runtime.HoldingRoster, _holdingDummyParty, "inspection_holding");
		MovePrisonerRosterFromMainParty(runtime.HoldingPrisonerRoster, _holdingDummyParty, "inspection_holding_prisoners");
		ValidateSplit(runtime, beforeMemberTotals, beforePrisonerTotals, beforeMainMen, valueOrDefault);
		string text = RosterSummary(mainParty.MemberRoster);
		PartyBase mainParty4 = PartyBase.MainParty;
		runtime.InspectionSummary = text + ", prisoners=" + RosterSummary((mainParty4 != null) ? mainParty4.PrisonRoster : null);
		MobileParty holdingDummyParty = _holdingDummyParty;
		string text2 = RosterSummary((holdingDummyParty != null) ? holdingDummyParty.MemberRoster : null);
		MobileParty holdingDummyParty2 = _holdingDummyParty;
		runtime.HoldingSummary = text2 + ", prisoners=" + RosterSummary((holdingDummyParty2 != null) ? holdingDummyParty2.PrisonRoster : null);
		Log("split_ok inspection=" + runtime.InspectionSummary + " holding=" + runtime.HoldingSummary);
	}

	private static void CreateInspectionHoldingDummyParty(MobileParty mainParty)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		CampaignVec2 position = mainParty.Position;
		Vec2 val = ResolveEncounterDirection(mainParty);
		Vec2 val2 = default(Vec2);
		val2 = new Vec2(0f - val.Y, val.X);
		if (val2.LengthSquared <= 0.0001f)
		{
			val2 = new Vec2(0f, 1f);
		}
		CampaignVec2 position2 = position + val2.Normalized() * 0.4f;
		_holdingDummyPartyStringId = "animusforge_troop_inspection_holding_" + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		_holdingDummyParty = MobileParty.CreateParty(_holdingDummyPartyStringId, new TroopInspectionDummyPartyComponent(position2, new TextObject("AnimusForge Troop Inspection Holding"), Hero.MainHero, Clan.PlayerClan));
		if (_holdingDummyParty == null)
		{
			throw new InvalidOperationException("Failed to create inspection holding dummy party.");
		}
		_holdingDummyParty.IsVisible = false;
		_holdingDummyParty.SetMoveModeHold();
		Log($"holding_dummy_create id={((MBObjectBase)_holdingDummyParty).StringId} pos={FormatCampaignVec2(_holdingDummyParty.Position)} members={_holdingDummyParty.Party.NumberOfHealthyMembers}");
	}

	private static void OpenInspectionMissionAfterSelection(MobileParty mainParty)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		PrepareRuntime(mainParty);
		MissionInitializerRecord val = BuildMissionInitializerRecord(mainParty);
		Log($"open_battle scene={val.SceneName} terrain={val.TerrainType}");
		IMission obj = CampaignMission.OpenBattleMission(val);
		Mission val2 = (Mission)(object)((obj is Mission) ? obj : null);
		if (val2 == null)
		{
			throw new InvalidOperationException("CampaignMission.OpenBattleMission returned non-Mission.");
		}
		_activeInspectionMission = val2;
		PlayerEncounter.StartAttackMission();
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		if (playerMapEvent != null)
		{
			playerMapEvent.BeginWait();
		}
		Log(string.Format("mission_behaviors deployment_handler={0} deployment_controller={1} battle_end_logic={2} mode={3}", HasMissionBehavior(val2, "BattleDeploymentHandler"), val2.GetMissionBehavior<BattleDeploymentMissionController>() != null, val2.GetMissionBehavior<BattleEndLogic>() != null, val2.Mode));
		TroopInspectionMissionLogic troopInspectionMissionLogic = new TroopInspectionMissionLogic(_dummyPartyStringId, (_runtime != null) ? _runtime.InspectionPrisonerRoster : null);
		val2.AddMissionBehavior((MissionBehavior)(object)troopInspectionMissionLogic);
		troopInspectionMissionLogic.TryDisableBattleEndLogic("after_open_manual");
		Log("logic_added success");
	}

	private static void PrepareRuntime(MobileParty mainParty)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		CampaignVec2 position = mainParty.Position;
		Vec2 val = ResolveEncounterDirection(mainParty);
		CampaignVec2 position2 = position - val * 0.4f;
		_dummyPartyStringId = "animusforge_troop_inspection_dummy_" + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		_dummyParty = MobileParty.CreateParty(_dummyPartyStringId, new TroopInspectionDummyPartyComponent(position2, new TextObject("AnimusForge Troop Inspection Dummy"), Hero.MainHero, Clan.PlayerClan));
		if (_dummyParty == null)
		{
			throw new InvalidOperationException("Failed to create dummy party.");
		}
		_dummyParty.IsVisible = false;
		_dummyParty.SetMoveModeHold();
		Log($"dummy_party_create id={((MBObjectBase)_dummyParty).StringId} pos={FormatCampaignVec2(_dummyParty.Position)} members={_dummyParty.Party.NumberOfHealthyMembers}");
		FieldBattleEventComponent obj = FieldBattleEventComponent.CreateFieldBattleEvent(PartyBase.MainParty, _dummyParty.Party);
		_mapEvent = ((obj != null) ? ((MapEventComponent)obj).MapEvent : null);
		if (_mapEvent == null)
		{
			throw new InvalidOperationException("Failed to create field battle MapEvent.");
		}
		_mapEvent.ResetBattleState();
		int num = _mapEvent.AttackerSide.RecalculateMemberCountOfSide();
		int num2 = _mapEvent.DefenderSide.RecalculateMemberCountOfSide();
		Log($"mapevent_create attacker_side_count={num} defender_side_count={num2} player_side={_mapEvent.PlayerSide} is_player_mapevent={_mapEvent.IsPlayerMapEvent}");
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(PartyBase.MainParty, _dummyParty.Party);
		SetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent", _mapEvent);
		Log($"player_encounter_context battle={PlayerEncounter.Battle != null} is_mapevent={PlayerEncounter.Battle == _mapEvent} player_mapevent={MapEvent.PlayerMapEvent == _mapEvent}");
	}

	private static MissionInitializerRecord BuildMissionInitializerRecord(MobileParty mainParty)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected I4, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		CampaignVec2 position = mainParty.Position;
		MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(in position);
		string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, false);
		if (string.IsNullOrWhiteSpace(battleSceneForMapPatch))
		{
			throw new InvalidOperationException("Battle scene is empty.");
		}
		MissionInitializerRecord result = new MissionInitializerRecord(battleSceneForMapPatch);
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mainParty.CurrentNavigationFace);
		result.TerrainType = (int)faceTerrainType;
		result.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.NeedsRandomTerrain = false;
		result.PlayingInCampaignMode = true;
		result.RandomTerrainSeed = MBRandom.RandomInt(10000);
		result.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(mainParty.Position);
		result.SceneHasMapPatch = true;
		result.DecalAtlasGroup = 2;
		result.PatchCoordinates = mapPatchAtPosition.normalizedCoordinates;
		result.PatchEncounterDir = ResolvePatchEncounterDirection();
		return result;
	}

	private static Vec2 ResolvePatchEncounterDirection()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MapEvent mapEvent = _mapEvent;
			object obj;
			if (mapEvent == null)
			{
				obj = null;
			}
			else
			{
				MapEventSide attackerSide = mapEvent.AttackerSide;
				obj = ((attackerSide != null) ? attackerSide.LeaderParty : null);
			}
			if (obj != null)
			{
				MapEventSide defenderSide = _mapEvent.DefenderSide;
				if (((defenderSide != null) ? defenderSide.LeaderParty : null) != null)
				{
					CampaignVec2 position = _mapEvent.AttackerSide.LeaderParty.Position;
					Vec2 val = position.ToVec2();
					position = _mapEvent.DefenderSide.LeaderParty.Position;
					Vec2 val2 = val - position.ToVec2();
					if (val2.LengthSquared > 0.0001f)
					{
						return val2.Normalized();
					}
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
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Vec2 val = ((mainParty != null) ? mainParty.Bearing : Vec2.Zero);
			if (val.LengthSquared > 0.0001f)
			{
				return val.Normalized();
			}
		}
		catch
		{
		}
		return new Vec2(1f, 0f);
	}

	private static void MoveRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (selectedRoster == null || targetParty == null)
		{
			return;
		}
		MoveRosterResult moveRosterResult = new MoveRosterResult();
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			TroopRosterElement current = item;
			CharacterObject character = current.Character;
			if (character != null && current.Number > 0 && !((BasicCharacterObject)character).IsPlayerCharacter)
			{
				if (((BasicCharacterObject)character).IsHero)
				{
					MoveHeroToParty(character.HeroObject, targetParty, label, moveRosterResult);
				}
				else
				{
					MoveRegularTroopToParty(current, targetParty, label, moveRosterResult);
				}
			}
		}
		Log($"move_roster_result label={label} {moveRosterResult}");
	}

	private static void MovePrisonerRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (selectedRoster == null || targetParty == null)
		{
			return;
		}
		MoveRosterResult moveRosterResult = new MoveRosterResult();
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			TroopRosterElement current = item;
			if (current.Character != null && current.Number > 0)
			{
				MovePrisonerToParty(current, targetParty, label, moveRosterResult);
			}
		}
		Log($"move_prisoner_roster_result label={label} {moveRosterResult}");
	}

	private static void MovePrisonerToParty(TroopRosterElement item, MobileParty targetParty, string label, MoveRosterResult result)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		PartyBase mainParty = PartyBase.MainParty;
		TroopRoster obj = ((mainParty != null) ? mainParty.PrisonRoster : null);
		TroopRoster val = ((targetParty != null) ? targetParty.PrisonRoster : null);
		CharacterObject character = item.Character;
		if (obj == null || val == null || character == null)
		{
			throw new InvalidOperationException("Invalid prison roster while moving prisoner.");
		}
		int num = obj.FindIndexOfTroop(character);
		if (num < 0)
		{
			throw new InvalidOperationException("Source prisoner not found in MainParty: " + SafeCharacterId(character));
		}
		TroopRosterElement freshRosterElementCopy = GetFreshRosterElementCopy(obj, num);
		int num2 = Math.Max(0, item.Number);
		int num3 = Math.Min(num2, Math.Min(Math.Max(0, item.WoundedNumber), Math.Max(0, freshRosterElementCopy.WoundedNumber)));
		int num4 = CalculateRosterXpToMove(freshRosterElementCopy, num2);
		if (freshRosterElementCopy.Number < num2)
		{
			throw new InvalidOperationException($"Not enough source prisoners for {SafeCharacterId(character)}. have={freshRosterElementCopy.Number} need={num2}");
		}
		if (freshRosterElementCopy.Xp < num4)
		{
			throw new InvalidOperationException($"Not enough source prisoner XP for {SafeCharacterId(character)}. have={freshRosterElementCopy.Xp} need={num4}");
		}
		obj.AddToCounts(character, -num2, false, -num3, -num4, true, -1);
		val.AddToCounts(character, num2, false, num3, num4, true, -1);
		if (((BasicCharacterObject)character).IsHero)
		{
			result.Heroes += num2;
			return;
		}
		result.RegularMen += num2;
		result.RegularWounded += num3;
		result.RegularXp += num4;
	}

	private static void MoveHeroToParty(Hero hero, MobileParty targetParty, string label, MoveRosterResult result)
	{
		if (hero != null && targetParty != null && !hero.IsHumanPlayerCharacter)
		{
			AddHeroToPartyAction.Apply(hero, targetParty, false);
			result.Heroes++;
		}
	}

	private static void MoveRegularTroopToParty(TroopRosterElement item, MobileParty targetParty, string label, MoveRosterResult result)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		MobileParty mainParty = MobileParty.MainParty;
		TroopRoster obj = ((mainParty != null) ? mainParty.MemberRoster : null);
		TroopRoster val = ((targetParty != null) ? targetParty.MemberRoster : null);
		CharacterObject character = item.Character;
		if (obj == null || val == null || character == null)
		{
			throw new InvalidOperationException("Invalid roster while moving regular troop.");
		}
		int num = obj.FindIndexOfTroop(character);
		if (num < 0)
		{
			throw new InvalidOperationException("Source troop not found in MainParty: " + SafeCharacterId(character));
		}
		TroopRosterElement elementCopyAtIndex = obj.GetElementCopyAtIndex(num);
		int num2 = Math.Max(0, item.Number);
		int num3 = 0;
		int num4 = CalculateRosterXpToMove(elementCopyAtIndex, num2);
		if (elementCopyAtIndex.Number < num2)
		{
			throw new InvalidOperationException($"Not enough source troops for {SafeCharacterId(character)}. have={elementCopyAtIndex.Number} need={num2}");
		}
		if (elementCopyAtIndex.Xp < num4)
		{
			throw new InvalidOperationException($"Not enough source XP for {SafeCharacterId(character)}. have={elementCopyAtIndex.Xp} need={num4}");
		}
		obj.AddToCounts(character, -num2, false, -num3, -num4, true, -1);
		val.AddToCounts(character, num2, false, num3, num4, true, -1);
		result.RegularMen += num2;
		result.RegularWounded += num3;
		result.RegularXp += num4;
	}

	private static int CalculateRosterXpToMove(TroopRosterElement sourceElement, int numberToMove)
	{
		try
		{
			int num = Math.Max(0, sourceElement.Number);
			int num2 = Math.Max(0, sourceElement.Xp);
			numberToMove = Math.Max(0, numberToMove);
			if (num <= 0 || num2 <= 0 || numberToMove <= 0)
			{
				return 0;
			}
			if (numberToMove >= num)
			{
				return num2;
			}
			int num3 = (int)Math.Round((double)num2 * (double)numberToMove / (double)num, MidpointRounding.AwayFromZero);
			if (num3 < 0)
			{
				return 0;
			}
			if (num3 > num2)
			{
				return num2;
			}
			return num3;
		}
		catch
		{
			return 0;
		}
	}

	private static MoveRosterResult MoveAllMembersBackToMainParty(MobileParty sourceParty, string label)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		MobileParty mainParty = MobileParty.MainParty;
		MoveRosterResult moveRosterResult = new MoveRosterResult();
		if (sourceParty == null || mainParty == null || sourceParty.MemberRoster == null)
		{
			Log($"cleanup_return_skipped label={label} source_null={sourceParty == null} main_null={mainParty == null}");
			return moveRosterResult;
		}
		foreach (TroopRosterElement item in SnapshotRoster(sourceParty.MemberRoster))
		{
			TroopRosterElement current = item;
			try
			{
				CharacterObject character = current.Character;
				if (character == null || current.Number <= 0)
				{
					continue;
				}
				if (((BasicCharacterObject)character).IsHero)
				{
					Hero heroObject = character.HeroObject;
					if (heroObject != null && heroObject.IsDead)
					{
						moveRosterResult.DeadHeroesSkipped++;
					}
					else if (!((BasicCharacterObject)character).IsPlayerCharacter)
					{
						AddHeroToPartyAction.Apply(character.HeroObject, mainParty, false);
						moveRosterResult.Heroes++;
					}
				}
				else
				{
					int num = Math.Max(0, current.Number);
					int num2 = Math.Max(0, current.WoundedNumber);
					int num3 = Math.Max(0, current.Xp);
					sourceParty.MemberRoster.AddToCounts(character, -num, false, -num2, -num3, true, -1);
					mainParty.MemberRoster.AddToCounts(character, num, false, num2, num3, true, -1);
					moveRosterResult.RegularMen += num;
					moveRosterResult.RegularWounded += num2;
					moveRosterResult.RegularXp += num3;
				}
			}
			catch (Exception ex)
			{
				moveRosterResult.Errors++;
				Log("cleanup_return_element_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
			}
		}
		Log($"cleanup_return_summary label={label} {moveRosterResult}");
		return moveRosterResult;
	}

	private static MoveRosterResult MoveAllPrisonersBackToMainParty(MobileParty sourceParty, string label)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		PartyBase mainParty = PartyBase.MainParty;
		TroopRoster val = ((mainParty != null) ? mainParty.PrisonRoster : null);
		MoveRosterResult moveRosterResult = new MoveRosterResult();
		if (sourceParty == null || val == null || sourceParty.PrisonRoster == null)
		{
			Log($"cleanup_prisoner_return_skipped label={label} source_null={sourceParty == null} main_prison_null={val == null}");
			return moveRosterResult;
		}
		TroopRoster prisonRoster = sourceParty.PrisonRoster;
		foreach (TroopRosterElement item in SnapshotRoster(prisonRoster))
		{
			TroopRosterElement current = item;
			try
			{
				CharacterObject character = current.Character;
				if (character != null && current.Number > 0)
				{
					int num = Math.Max(0, current.Number);
					int num2 = Math.Max(0, current.WoundedNumber);
					int num3 = Math.Max(0, current.Xp);
					prisonRoster.AddToCounts(character, -num, false, -num2, -num3, true, -1);
					val.AddToCounts(character, num, false, num2, num3, true, -1);
					if (((BasicCharacterObject)character).IsHero)
					{
						moveRosterResult.Heroes += num;
						continue;
					}
					moveRosterResult.RegularMen += num;
					moveRosterResult.RegularWounded += num2;
					moveRosterResult.RegularXp += num3;
				}
			}
			catch (Exception ex)
			{
				moveRosterResult.Errors++;
				Log("cleanup_prisoner_return_element_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
			}
		}
		Log($"cleanup_prisoner_return_summary label={label} {moveRosterResult}");
		return moveRosterResult;
	}

	private static void DestroyHoldingDummyParty(MobileParty party, string label)
	{
		try
		{
			if (party != null)
			{
				string text = ((MBObjectBase)party).StringId ?? "";
				if (party.IsActive && text.StartsWith("animusforge_troop_inspection_holding_", StringComparison.Ordinal))
				{
					DestroyPartyAction.Apply((PartyBase)null, party);
					Log("holding_dummy_destroyed label=" + label + " id=" + text);
				}
			}
		}
		catch (Exception ex)
		{
			Log("holding_dummy_destroy_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void CleanupSplitRuntime(string reason)
	{
		DestroySelectionPoolDummyParty(reason + "_selection_pool");
		MobileParty holdingDummyParty = _holdingDummyParty;
		MoveAllMembersBackToMainParty(holdingDummyParty, reason);
		MoveAllPrisonersBackToMainParty(holdingDummyParty, reason + "_prisoners");
		DestroyHoldingDummyParty(holdingDummyParty, reason);
		_holdingDummyParty = null;
		_holdingDummyPartyStringId = null;
		if (_runtime != null)
		{
			_runtime.HoldingDummyParty = null;
		}
	}

	private static void ValidateSplit(TroopInspectionRuntime runtime, Dictionary<CharacterObject, RosterTotals> beforeMemberTotals, Dictionary<CharacterObject, RosterTotals> beforePrisonerTotals, int beforeMainMen, int beforePrisoners)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			throw new InvalidOperationException("MainParty missing after split.");
		}
		object obj = CharacterObject.PlayerCharacter;
		if (obj == null)
		{
			Hero mainHero = Hero.MainHero;
			obj = ((mainHero != null) ? mainHero.CharacterObject : null);
		}
		CharacterObject val = (CharacterObject)obj;
		if (val != null && !mainParty.MemberRoster.Contains(val))
		{
			throw new InvalidOperationException("Player is not in MainParty after split.");
		}
		TroopRoster memberRoster = mainParty.MemberRoster;
		int num = ((memberRoster != null) ? memberRoster.TotalManCount : 0);
		int? obj2;
		if (runtime == null)
		{
			obj2 = null;
		}
		else
		{
			MobileParty holdingDummyParty = runtime.HoldingDummyParty;
			if (holdingDummyParty == null)
			{
				obj2 = null;
			}
			else
			{
				TroopRoster memberRoster2 = holdingDummyParty.MemberRoster;
				obj2 = ((memberRoster2 != null) ? new int?(memberRoster2.TotalManCount) : ((int?)null));
			}
		}
		int? num2 = obj2;
		int num3 = num + num2.GetValueOrDefault();
		if (num3 != beforeMainMen)
		{
			throw new InvalidOperationException($"Total member count mismatch after split. before={beforeMainMen} after={num3}");
		}
		PartyBase mainParty2 = PartyBase.MainParty;
		int? obj3;
		if (mainParty2 == null)
		{
			obj3 = null;
		}
		else
		{
			TroopRoster prisonRoster = mainParty2.PrisonRoster;
			obj3 = ((prisonRoster != null) ? new int?(prisonRoster.TotalManCount) : ((int?)null));
		}
		num2 = obj3;
		int valueOrDefault = num2.GetValueOrDefault();
		int? obj4;
		if (runtime == null)
		{
			obj4 = null;
		}
		else
		{
			MobileParty holdingDummyParty2 = runtime.HoldingDummyParty;
			if (holdingDummyParty2 == null)
			{
				obj4 = null;
			}
			else
			{
				TroopRoster prisonRoster2 = holdingDummyParty2.PrisonRoster;
				obj4 = ((prisonRoster2 != null) ? new int?(prisonRoster2.TotalManCount) : ((int?)null));
			}
		}
		num2 = obj4;
		int num4 = valueOrDefault + num2.GetValueOrDefault();
		if (num4 != beforePrisoners)
		{
			throw new InvalidOperationException($"Total prisoner count mismatch after split. before={beforePrisoners} after={num4}");
		}
		TroopRoster[] obj5 = new TroopRoster[2]
		{
			mainParty.MemberRoster,
			default(TroopRoster)
		};
		object obj6;
		if (runtime == null)
		{
			obj6 = null;
		}
		else
		{
			MobileParty holdingDummyParty3 = runtime.HoldingDummyParty;
			obj6 = ((holdingDummyParty3 != null) ? holdingDummyParty3.MemberRoster : null);
		}
		obj5[1] = (TroopRoster)obj6;
		ValidateRosterTotals("members", beforeMemberTotals, BuildCombinedRosterTotals((TroopRoster[])(object)obj5));
		TroopRoster[] array = new TroopRoster[2];
		PartyBase mainParty3 = PartyBase.MainParty;
		array[0] = ((mainParty3 != null) ? mainParty3.PrisonRoster : null);
		object obj7;
		if (runtime == null)
		{
			obj7 = null;
		}
		else
		{
			MobileParty holdingDummyParty4 = runtime.HoldingDummyParty;
			obj7 = ((holdingDummyParty4 != null) ? holdingDummyParty4.PrisonRoster : null);
		}
		array[1] = (TroopRoster)obj7;
		ValidateRosterTotals("prisoners", beforePrisonerTotals, BuildCombinedRosterTotals((TroopRoster[])(object)array));
		object[] obj8 = new object[6]
		{
			num3,
			num4,
			RosterSummary(mainParty.MemberRoster),
			null,
			null,
			null
		};
		PartyBase mainParty4 = PartyBase.MainParty;
		obj8[3] = RosterSummary((mainParty4 != null) ? mainParty4.PrisonRoster : null);
		object roster;
		if (runtime == null)
		{
			roster = null;
		}
		else
		{
			MobileParty holdingDummyParty5 = runtime.HoldingDummyParty;
			roster = ((holdingDummyParty5 != null) ? holdingDummyParty5.MemberRoster : null);
		}
		obj8[4] = RosterSummary((TroopRoster)roster);
		object roster2;
		if (runtime == null)
		{
			roster2 = null;
		}
		else
		{
			MobileParty holdingDummyParty6 = runtime.HoldingDummyParty;
			roster2 = ((holdingDummyParty6 != null) ? holdingDummyParty6.PrisonRoster : null);
		}
		obj8[5] = RosterSummary((TroopRoster)roster2);
		Log(string.Format("split_validate_ok members={0} prisoners={1} main={2} main_prisoners={3} holding={4} holding_prisoners={5}", obj8));
	}

	private static void ValidateRosterTotals(string label, Dictionary<CharacterObject, RosterTotals> beforeTotals, Dictionary<CharacterObject, RosterTotals> afterTotals)
	{
		beforeTotals = beforeTotals ?? new Dictionary<CharacterObject, RosterTotals>();
		afterTotals = afterTotals ?? new Dictionary<CharacterObject, RosterTotals>();
		foreach (KeyValuePair<CharacterObject, RosterTotals> beforeTotal in beforeTotals)
		{
			if (!afterTotals.TryGetValue(beforeTotal.Key, out var value))
			{
				throw new InvalidOperationException(label + " character missing after split: " + SafeCharacterId(beforeTotal.Key));
			}
			RosterTotals value2 = beforeTotal.Value;
			CharacterObject key = beforeTotal.Key;
			bool num;
			if (key == null || !((BasicCharacterObject)key).IsHero)
			{
				if (value2.Number == value.Number && value2.Wounded == value.Wounded)
				{
					num = value2.Xp == value.Xp;
					goto IL_00c5;
				}
			}
			else if (value2.Number == value.Number)
			{
				num = value2.Wounded == value.Wounded;
				goto IL_00c5;
			}
			goto IL_00c7;
			IL_00c5:
			if (num)
			{
				continue;
			}
			goto IL_00c7;
			IL_00c7:
			throw new InvalidOperationException($"{label} roster totals mismatch for {SafeCharacterId(beforeTotal.Key)}. before={value2} after={value}");
		}
		foreach (KeyValuePair<CharacterObject, RosterTotals> afterTotal in afterTotals)
		{
			if (!beforeTotals.ContainsKey(afterTotal.Key))
			{
				throw new InvalidOperationException(label + " unexpected character after split: " + SafeCharacterId(afterTotal.Key));
			}
			if (afterTotal.Key != null && ((BasicCharacterObject)afterTotal.Key).IsHero && afterTotal.Value.Number != 1)
			{
				throw new InvalidOperationException(label + " hero duplicated after split: " + SafeCharacterId(afterTotal.Key));
			}
		}
	}

	private static void DestroySelectionPoolDummyParty(string label)
	{
		try
		{
			MobileParty selectionPoolDummyParty = _selectionPoolDummyParty;
			_selectionPoolDummyParty = null;
			_selectionPoolDummyPartyStringId = null;
			if (selectionPoolDummyParty != null)
			{
				string text = ((MBObjectBase)selectionPoolDummyParty).StringId ?? "";
				if (selectionPoolDummyParty.IsActive && text.StartsWith("animusforge_troop_inspection_selection_pool_", StringComparison.Ordinal))
				{
					ClearRosterDirect(selectionPoolDummyParty.MemberRoster);
					ClearRosterDirect(selectionPoolDummyParty.PrisonRoster);
					DestroyPartyAction.Apply((PartyBase)null, selectionPoolDummyParty);
					Log("selection_pool_destroyed label=" + label + " id=" + text);
				}
			}
		}
		catch (Exception ex)
		{
			Log("selection_pool_destroy_failed label=" + label + " error=" + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void ClearRosterDirect(TroopRoster roster)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (roster == null)
		{
			return;
		}
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			TroopRosterElement current = item;
			try
			{
				CharacterObject character = current.Character;
				if (character != null && current.Number > 0)
				{
					roster.AddToCounts(character, -Math.Max(0, current.Number), false, -Math.Max(0, current.WoundedNumber), -Math.Max(0, current.Xp), true, -1);
				}
			}
			catch
			{
			}
		}
	}

	private static Dictionary<CharacterObject, RosterTotals> BuildCombinedRosterTotals(params TroopRoster[] rosters)
	{
		Dictionary<CharacterObject, RosterTotals> dictionary = new Dictionary<CharacterObject, RosterTotals>();
		if (rosters == null)
		{
			return dictionary;
		}
		foreach (TroopRoster roster in rosters)
		{
			MergeRosterTotals(dictionary, roster);
		}
		return dictionary;
	}

	private static Dictionary<CharacterObject, RosterTotals> BuildRosterTotals(TroopRoster roster)
	{
		Dictionary<CharacterObject, RosterTotals> dictionary = new Dictionary<CharacterObject, RosterTotals>();
		MergeRosterTotals(dictionary, roster);
		return dictionary;
	}

	private static void MergeRosterTotals(Dictionary<CharacterObject, RosterTotals> totals, TroopRoster roster)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (totals == null || roster == null)
		{
			return;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement freshRosterElementCopy = GetFreshRosterElementCopy(roster, i);
			CharacterObject character = freshRosterElementCopy.Character;
			if (character != null && freshRosterElementCopy.Number > 0)
			{
				totals.TryGetValue(character, out var value);
				value.Number += Math.Max(0, freshRosterElementCopy.Number);
				value.Wounded += Math.Max(0, freshRosterElementCopy.WoundedNumber);
				value.Xp += Math.Max(0, freshRosterElementCopy.Xp);
				totals[character] = value;
			}
		}
	}

	private static List<TroopRosterElement> SnapshotRoster(TroopRoster roster)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		if (roster == null)
		{
			return list;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement freshRosterElementCopy = GetFreshRosterElementCopy(roster, i);
			if (freshRosterElementCopy.Character != null && freshRosterElementCopy.Number > 0)
			{
				list.Add(freshRosterElementCopy);
			}
		}
		return list;
	}

	private static TroopRosterElement GetFreshRosterElementCopy(TroopRoster roster, int index)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(index);
		try
		{
			elementCopyAtIndex.Xp = roster.GetElementXp(index);
		}
		catch
		{
		}
		return elementCopyAtIndex;
	}

	private static bool TroopInspectionTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		if (character != null && !((BasicCharacterObject)character).IsPlayerCharacter)
		{
			return !character.IsNotTransferableInPartyScreen;
		}
		return false;
	}

	private static TroopRoster BuildSelectableRoster(TroopRoster sourceRoster)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		TroopRoster val = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return val;
		}
		foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)sourceRoster.GetTroopRoster())
		{
			TroopRosterElement current = item;
			CharacterObject character = current.Character;
			if (character != null && !((BasicCharacterObject)character).IsPlayerCharacter && current.Number > 0)
			{
				int num = Math.Max(0, current.Number - current.WoundedNumber);
				if (num > 0)
				{
					int num2 = CalculateRosterXpToMove(current, num);
					val.AddToCounts(character, num, false, 0, num2, true, -1);
				}
			}
		}
		return val;
	}

	private static TroopRoster BuildSelectablePrisonerRoster(TroopRoster sourceRoster)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		TroopRoster val = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return val;
		}
		for (int i = 0; i < sourceRoster.Count; i++)
		{
			TroopRosterElement freshRosterElementCopy = GetFreshRosterElementCopy(sourceRoster, i);
			CharacterObject character = freshRosterElementCopy.Character;
			if (character != null && freshRosterElementCopy.Number > 0)
			{
				val.AddToCounts(character, freshRosterElementCopy.Number, false, Math.Max(0, freshRosterElementCopy.WoundedNumber), Math.Max(0, freshRosterElementCopy.Xp), true, -1);
			}
		}
		return val;
	}

	private static TroopRoster BuildSelectionRosterFromUi(TroopRoster sourceRoster)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		TroopRoster val = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return val;
		}
		foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)sourceRoster.GetTroopRoster())
		{
			TroopRosterElement current = item;
			CharacterObject character = current.Character;
			if (character != null && !((BasicCharacterObject)character).IsPlayerCharacter && current.Number > 0)
			{
				int num = Math.Max(0, current.Number - current.WoundedNumber);
				if (num > 0)
				{
					val.AddToCounts(character, num, false, 0, 0, true, -1);
				}
			}
		}
		return val;
	}

	private static TroopRoster BuildPrisonerSelectionRosterFromUi(TroopRoster sourceRoster)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		TroopRoster val = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return val;
		}
		for (int i = 0; i < sourceRoster.Count; i++)
		{
			TroopRosterElement freshRosterElementCopy = GetFreshRosterElementCopy(sourceRoster, i);
			CharacterObject character = freshRosterElementCopy.Character;
			if (character != null && freshRosterElementCopy.Number > 0)
			{
				int num = Math.Max(0, freshRosterElementCopy.Number);
				int num2 = Math.Min(num, Math.Max(0, freshRosterElementCopy.WoundedNumber));
				val.AddToCounts(character, num, false, num2, 0, true, -1);
			}
		}
		return val;
	}

	private static TroopRoster CloneRoster(TroopRoster sourceRoster)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		TroopRoster val = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return val;
		}
		foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)sourceRoster.GetTroopRoster())
		{
			TroopRosterElement current = item;
			if (current.Character != null && current.Number > 0)
			{
				val.Add(current);
			}
		}
		return val;
	}

	private static void AddPlayerToInspectionRoster(TroopRoster inspectionRoster)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (inspectionRoster == null)
		{
			return;
		}
		object obj = CharacterObject.PlayerCharacter;
		if (obj == null)
		{
			Hero mainHero = Hero.MainHero;
			obj = ((mainHero != null) ? mainHero.CharacterObject : null);
		}
		CharacterObject val = (CharacterObject)obj;
		if (val == null || inspectionRoster.Contains(val))
		{
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		object obj2 = ((mainParty != null) ? mainParty.MemberRoster : null);
		if (obj2 == null)
		{
			PartyBase mainParty2 = PartyBase.MainParty;
			obj2 = ((mainParty2 != null) ? mainParty2.MemberRoster : null);
		}
		TroopRoster val2 = (TroopRoster)obj2;
		if (val2 != null)
		{
			foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)val2.GetTroopRoster())
			{
				TroopRosterElement current = item;
				if (current.Character == val && current.Number > 0)
				{
					inspectionRoster.Add(current);
					return;
				}
			}
		}
		inspectionRoster.AddToCounts(val, 1, false, 0, 0, true, -1);
	}

	private static void ResetPendingSelection(string reason)
	{
		DestroySelectionPoolDummyParty(reason);
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager == null) ? null : ((object)gameStateManager.ActiveState)?.GetType().Name);
			}
			if (obj == null)
			{
				obj = "";
			}
			return ((string)obj).IndexOf("PartyState", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return true;
		}
	}

	private static int GetMainHeroHitPoints()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			return (mainHero != null) ? mainHero.HitPoints : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static string RosterSummary(TroopRoster roster)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (roster == null)
		{
			return "null";
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (TroopRosterElement item in (List<TroopRosterElement>)(object)roster.GetTroopRoster())
		{
			TroopRosterElement current = item;
			if (current.Character != null && current.Number > 0)
			{
				num++;
				if (((BasicCharacterObject)current.Character).IsHero)
				{
					num2 += current.Number;
				}
				num3 += Math.Max(0, current.WoundedNumber);
			}
		}
		return $"men={roster.TotalManCount}, elements={num}, heroes={num2}, wounded={num3}";
	}

	private static string SafeCharacterId(CharacterObject character)
	{
		try
		{
			return ((character != null) ? ((MBObjectBase)character).StringId : null) ?? ((character == null) ? null : ((object)((BasicCharacterObject)character).Name)?.ToString()) ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static bool IsOwnDummyParty(MobileParty party, string expectedStringId)
	{
		if (party == null || string.IsNullOrEmpty(expectedStringId))
		{
			return false;
		}
		if (string.Equals(((MBObjectBase)party).StringId, expectedStringId, StringComparison.Ordinal))
		{
			return ((MBObjectBase)party).StringId.StartsWith("animusforge_troop_inspection_dummy_", StringComparison.Ordinal);
		}
		return false;
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
			FieldInfo fieldInfo = target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (fieldInfo != null)
			{
				return (T)fieldInfo.GetValue(target);
			}
		}
		catch
		{
		}
		return default(T);
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

	internal static bool HasMissionBehavior(Mission mission, string typeName)
	{
		if (mission == null)
		{
			return false;
		}
		try
		{
			Type type = Type.GetType("TaleWorlds.MountAndBlade." + typeName + ", TaleWorlds.MountAndBlade");
			if (type == null)
			{
				return false;
			}
			MethodInfo method = typeof(Mission).GetMethod("GetMissionBehavior", Type.EmptyTypes);
			if (method == null)
			{
				return false;
			}
			return method.MakeGenericMethod(type).Invoke(mission, null) != null;
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
			string path = GetInspectionLogPath();
			File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [TroopInspection] " + message + Environment.NewLine);
		}
		catch
		{
			try
			{
				Logger.Log("TroopInspection", "[TroopInspection] " + message);
			}
			catch
			{
			}
		}
	}

	private static string GetInspectionLogPath()
	{
		if (!string.IsNullOrWhiteSpace(_inspectionLogPath))
		{
			return _inspectionLogPath;
		}
		string basePath;
		try
		{
			basePath = TaleWorlds.Engine.Utilities.GetBasePath();
		}
		catch
		{
			basePath = AppDomain.CurrentDomain.BaseDirectory;
		}
		string logDir = Path.Combine(basePath, "Modules", "AnimusForge", "Logs");
		Directory.CreateDirectory(logDir);
		_inspectionLogPath = Path.Combine(logDir, "TroopInspection.log");
		return _inspectionLogPath;
	}

}

internal static class ReinforcementSystemCompatibility
{
	private const string HarmonyId = "com.AnimusForge.spy.reinforcement_guard";

	private static readonly object PatchLock = new object();

	private static Harmony _harmony;

	private static bool _patched;

	private static bool _missingLogged;

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
				Type type = FindReinforcementMainType();
				if (type == null)
				{
					LogMissingOnce("Reinforcement_System.Main not loaded");
					return;
				}
				MethodInfo methodInfo = AccessTools.Method(type, "OnMissionBehaviorInitialize", new Type[1] { typeof(Mission) });
				MethodInfo methodInfo2 = AccessTools.Method(typeof(ReinforcementSystemCompatibility), "OnMissionBehaviorInitializePrefix");
				if (methodInfo == null || methodInfo2 == null)
				{
					LogMissingOnce("Reinforcement_System.Main.OnMissionBehaviorInitialize not found");
					return;
				}
				Harmony obj = _harmony ?? new Harmony("com.AnimusForge.spy.reinforcement_guard");
				obj.Patch(methodInfo, new HarmonyMethod(methodInfo2));
				_harmony = obj;
				_patched = true;
				Log("reinforcement_system_guard_patched");
			}
			catch (Exception ex)
			{
				Log("reinforcement_system_guard_patch_failed " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	private static bool OnMissionBehaviorInitializePrefix(Mission mission)
	{
		if (!TroopInspectionBehavior.ShouldSuppressReinforcementSystem(mission))
		{
			return true;
		}
		Log("reinforcement_system_skipped_for_inspection mission=" + (mission != null));
		return false;
	}

	private static Type FindReinforcementMainType()
	{
		Type type = AccessTools.TypeByName("Reinforcement_System.Main");
		if (type != null)
		{
			return type;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				type = assembly.GetType("Reinforcement_System.Main", throwOnError: false);
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
		if (!_missingLogged)
		{
			_missingLogged = true;
			Log("reinforcement_system_guard_not_active " + message);
		}
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

	private readonly TroopRoster _inspectionPrisonerRoster;

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

	private float _prisonerSpawnTimer = 1f;

	private int _lordFormationClassForceLogCount;

	private const FormationClass RegularPrisonerFormationClass = (FormationClass)6;

	private const FormationClass LordPrisonerFormationClass = (FormationClass)7;

	private static readonly PropertyInfo FormationRepresentativeClassProperty = typeof(Formation).GetProperty("RepresentativeClass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly FieldInfo FormationLogicalClassField = typeof(Formation).GetField("_logicalClass", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo FormationLogicalClassNeedsUpdateField = typeof(Formation).GetField("_logicalClassNeedsUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly Dictionary<Agent, bool> _prisonerIsLordMap = new Dictionary<Agent, bool>();

	private readonly HashSet<Agent> _civilianPrisonerActionSetApplied = new HashSet<Agent>();

	private readonly Dictionary<Agent, float> _prisonerPoseSuppressedUntil = new Dictionary<Agent, float>();

	private readonly HashSet<Agent> _prisonerPoseApplied = new HashSet<Agent>();

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

	public TroopInspectionMissionLogic(string dummyPartyStringId)
		: this(dummyPartyStringId, null)
	{
	}

	public TroopInspectionMissionLogic(string dummyPartyStringId, TroopRoster inspectionPrisonerRoster)
	{
		_dummyPartyStringId = dummyPartyStringId;
		_inspectionPrisonerRoster = (inspectionPrisonerRoster != null) ? CloneRoster(inspectionPrisonerRoster) : null;
	}

	public override void OnBehaviorInitialize()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		base.OnBehaviorInitialize();
		CacheBattleEndLogic();
		TryDisableBattleEndLogic("OnBehaviorInitialize");
		Mission mission = ((MissionBehavior)this).Mission;
		_lastMissionMode = (MissionMode)((mission != null) ? ((int)mission.Mode) : 0);
		_deploymentWasActive = (int)_lastMissionMode == 6;
		Log($"init deployment_active={_deploymentWasActive} mode={_lastMissionMode} battle_end_logic_cached={_battleEndLogic != null}");
		object arg = TroopInspectionBehavior.HasMissionBehavior(((MissionBehavior)this).Mission, "BattleDeploymentHandler");
		Mission mission2 = ((MissionBehavior)this).Mission;
		Log($"mission_behaviors deployment_handler={arg} deployment_controller={((mission2 != null) ? mission2.GetMissionBehavior<BattleDeploymentMissionController>() : null) != null} battle_end_logic={_battleEndLogic != null}");
	}

	public override void AfterStart()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		base.AfterStart();
		if (_battleEndLogic == null)
		{
			CacheBattleEndLogic();
		}
		TryDisableBattleEndLogic("AfterStart");
		Mission mission = ((MissionBehavior)this).Mission;
		_lastMissionMode = (MissionMode)((mission != null) ? ((int)mission.Mode) : 0);
		_deploymentWasActive = (int)_lastMissionMode == 6;
		Log($"after_start deployment_active={_deploymentWasActive} mode={_lastMissionMode} battle_end_disabled={_battleEndDisabled}");
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		RetryBattleEndDisableIfNeeded();
		DetectDeploymentEnd();
		if (!_prisonersSpawned)
		{
			_prisonerSpawnTimer -= dt;
			if (_prisonerSpawnTimer <= 0f)
			{
				SpawnPrisoners();
			}
		}
		if (_prisonersSpawned && !_deploymentEndDetected)
		{
			ForceLordPrisonerFormationClass("deployment_tick");
		}
		TryLogAgentCounts();
		RefreshPrisonerPoses(dt);
		ContinuousAgentRefresh(dt);
		if (!_inspectionMessageShown && _deploymentEndDetected && ((MissionBehavior)this).Mission != null && ((MissionBehavior)this).Mission.CurrentTime > 2f)
		{
			_inspectionMessageShown = true;
			AnimusForgeQuickInfo.Show("检阅模式：可自由指挥部队进行检阅。按TAB撤退结束检阅。");
			Log("inspection_message_shown");
		}
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

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		string text = ((object)missionResult)?.ToString() ?? "null";
		string text2 = text + "|" + _battleEndDisabled + "|" + _deploymentEndDetected;
		Mission mission = ((MissionBehavior)this).Mission;
		float num = ((mission != null) ? mission.CurrentTime : 0f);
		if (!string.Equals(_lastMissionEndedLogState, text2, StringComparison.Ordinal) || num >= _nextMissionEndedLogTime)
		{
			_lastMissionEndedLogState = text2;
			_nextMissionEndedLogTime = num + 5f;
			Log($"mission_ended_check mission_result={text} battle_end_disabled={_battleEndDisabled} deployment_detected={_deploymentEndDetected}");
		}
		return false;
	}

	internal void TryDisableBattleEndLogic(string source)
	{
		try
		{
			if (((MissionBehavior)this).Mission == null)
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
			Mission mission = ((MissionBehavior)this).Mission;
			_battleEndLogic = ((mission != null) ? mission.GetMissionBehavior<BattleEndLogic>() : null);
		}
		catch (Exception ex)
		{
			Log("cache_battle_end_logic failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RetryBattleEndDisableIfNeeded()
	{
		if (!_battleEndDisabled && ((MissionBehavior)this).Mission != null && !(((MissionBehavior)this).Mission.CurrentTime < _nextBattleEndDisableRetryTime))
		{
			_nextBattleEndDisableRetryTime = ((MissionBehavior)this).Mission.CurrentTime + 1f;
			TryDisableBattleEndLogic("retry_tick");
		}
	}

	private void DetectDeploymentEnd()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		if (_deploymentEndDetected || ((MissionBehavior)this).Mission == null)
		{
			return;
		}
		try
		{
			MissionMode mode = ((MissionBehavior)this).Mission.Mode;
			if (_lastMissionMode != mode)
			{
				Log($"mission_mode_changed {_lastMissionMode} -> {mode}");
			}
			if (_deploymentWasActive && (int)mode != 6)
			{
				_deploymentEndDetected = true;
				ForceLordPrisonerFormationClass("deployment_end");
				FreezePrisoners();
				Log("deployment_ended detection");
				TryDisableBattleEndLogic("deployment_ended");
			}
			_deploymentWasActive = (int)mode == 6;
			_lastMissionMode = mode;
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
			if (((MissionBehavior)this).Mission == null)
			{
				return;
			}
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsActive() && item.Origin is PrisonerAgentOrigin)
				{
					item.Formation = null;
					if (_prisonerIsLordMap.TryGetValue(item, out var value))
					{
						ApplyPrisonerPose(item, value, afterDeployment: true);
					}
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
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (_prisonersSpawned || ((MissionBehavior)this).Mission == null)
		{
			return;
		}
		_prisonersSpawned = true;
		TroopRoster val = _inspectionPrisonerRoster;
		string text = "selection_snapshot";
		if (val == null)
		{
			PartyBase mainParty = PartyBase.MainParty;
			val = ((mainParty != null) ? mainParty.PrisonRoster : null);
			text = "main_party_fallback";
		}
		if (val == null)
		{
			Log("spawn_prisoners skipped: PrisonRoster null");
			AnimusForgeQuickInfo.Show("阅兵：无法访问囚犯名册。");
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		foreach (TroopRosterElement item in SnapshotRoster(val))
		{
			TroopRosterElement current = item;
			CharacterObject character = current.Character;
			if (character == null)
			{
				continue;
			}
			int num6 = Math.Max(0, current.Number);
			int num7 = Math.Min(num6, Math.Max(0, current.WoundedNumber));
			num += num6;
			if (((BasicCharacterObject)character).IsHero)
			{
				num2 += num6;
				continue;
			}
			num3 += num6;
			num5 += num7;
			num4 += num6;
		}
		if (num <= 0)
		{
			Log("spawn_prisoners skipped: no selected prisoners source=" + text);
			return;
		}
		Team playerTeam = ((MissionBehavior)this).Mission.PlayerTeam;
		if (playerTeam == null)
		{
			Log("spawn_prisoners skipped: PlayerTeam null");
			return;
		}
		if (num2 > 0)
		{
			playerTeam.GetFormation(LordPrisonerFormationClass);
		}
		if (num4 > 0)
		{
			playerTeam.GetFormation(RegularPrisonerFormationClass);
		}
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num13 = 0;
		string text2 = "";
		foreach (TroopRosterElement item2 in SnapshotRoster(val))
		{
			TroopRosterElement current2 = item2;
			CharacterObject character2 = current2.Character;
			if (character2 == null || !((BasicCharacterObject)character2).IsHero)
			{
				continue;
			}
			int num12 = Math.Max(0, current2.Number);
			for (int i = 0; i < num12; i++)
			{
				try
				{
					PrisonerAgentOrigin prisonerAgentOrigin = new PrisonerAgentOrigin(character2);
					Agent val2 = ((MissionBehavior)this).Mission.SpawnTroop((IAgentOriginBase)(object)prisonerAgentOrigin, true, true, false, false, num2, num11, false, false, true, (Vec3?)null, (Vec2?)null, (string)null, (ItemObject)null, LordPrisonerFormationClass, false);
					if (val2 != null)
					{
						val2.SetIsAIPaused(true);
						val2.DisableScriptedMovement();
						_prisonerIsLordMap[val2] = true;
						ApplyPrisonerPose(val2, isLord: true, afterDeployment: false);
						Formation formation = val2.Formation;
						Log("spawn_prisoner_hero ok troop=" + (((MBObjectBase)character2).StringId ?? "null") + " team=" + ((val2.Team != null) ? ((object)val2.Team.Side).ToString() : "null") + " formation=" + ((formation != null) ? ((object)formation.FormationIndex).ToString() : "null") + " pos=" + val2.Position);
						num8++;
					}
					else
					{
						Log($"spawn_prisoner_hero returned null troop={((MBObjectBase)character2).StringId} formation={(object)LordPrisonerFormationClass}");
					}
					num11++;
				}
				catch (Exception ex)
				{
					num10++;
					text2 = ex.GetType().Name + ": " + ex.Message;
					Log("spawn_prisoner_hero failed: " + text2);
				}
			}
		}
		foreach (TroopRosterElement item2 in SnapshotRoster(val))
		{
			TroopRosterElement current2 = item2;
			CharacterObject character2 = current2.Character;
			if (character2 == null || ((BasicCharacterObject)character2).IsHero)
			{
				continue;
			}
			int num12 = Math.Max(0, current2.Number);
			for (int j = 0; j < num12; j++)
			{
				try
				{
					PrisonerAgentOrigin prisonerAgentOrigin = new PrisonerAgentOrigin(character2);
					Agent val2 = ((MissionBehavior)this).Mission.SpawnTroop((IAgentOriginBase)(object)prisonerAgentOrigin, true, true, false, false, num4, num13, false, false, true, (Vec3?)null, (Vec2?)null, (string)null, (ItemObject)null, RegularPrisonerFormationClass, false);
					if (val2 != null)
					{
						val2.SetIsAIPaused(true);
						val2.DisableScriptedMovement();
						_prisonerIsLordMap[val2] = false;
						ApplyPrisonerPose(val2, isLord: false, afterDeployment: false);
						num9++;
					}
					else
					{
						Log($"spawn_prisoner_regular returned null troop={((MBObjectBase)character2).StringId} formation={(object)RegularPrisonerFormationClass}");
					}
					num13++;
				}
				catch (Exception ex)
				{
					num10++;
					text2 = ex.GetType().Name + ": " + ex.Message;
					Log("spawn_prisoner_regular failed: " + text2);
				}
			}
		}
		Log("spawn_prisoners result: source=" + text + " during_deployment=" + (!_deploymentEndDetected) + " selected=" + num + " selected_heroes=" + num2 + " spawned_heroes=" + num8 + " selected_regulars=" + num3 + " included_wounded_regulars=" + num5 + " spawnable_regulars=" + num4 + " spawned_regulars=" + num9 + " errors=" + num10);
		if (num8 > 0)
		{
			ForceLordPrisonerFormationClass("after_spawn");
		}
		if (num8 + num9 > 0)
		{
			string text3 = "阅兵：";
			if (num8 > 0)
			{
				text3 = text3 + num8 + " 名领主俘虏、";
			}
			if (num9 > 0)
			{
				text3 = text3 + num9 + " 名士兵俘虏";
			}
			text3 += "参加检阅。";
			AnimusForgeQuickInfo.Show(text3);
		}
		else if (!string.IsNullOrWhiteSpace(text2))
		{
			AnimusForgeQuickInfo.Show("阅兵：囚犯生成失败，错误: " + text2);
		}
	}

	private void ForceLordPrisonerFormationClass(string reason)
	{
		try
		{
			Mission mission = ((MissionBehavior)this).Mission;
			Team playerTeam = (mission != null) ? mission.PlayerTeam : null;
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
			object oldLogicalClass = null;
			object oldLogicalClassNeedsUpdate = null;
			if (FormationLogicalClassField != null)
			{
				oldLogicalClass = FormationLogicalClassField.GetValue(formation);
			}
			if (FormationLogicalClassNeedsUpdateField != null)
			{
				oldLogicalClassNeedsUpdate = FormationLogicalClassNeedsUpdateField.GetValue(formation);
			}
			bool needsCorrection = oldRepresentativeClass != FormationClass.Cavalry || (oldLogicalClass is FormationClass && (FormationClass)oldLogicalClass != FormationClass.Cavalry) || (oldLogicalClassNeedsUpdate is bool && (bool)oldLogicalClassNeedsUpdate);
			if (!needsCorrection)
			{
				return;
			}
			if (FormationRepresentativeClassProperty != null)
			{
				FormationRepresentativeClassProperty.SetValue(formation, FormationClass.Cavalry, null);
			}
			if (FormationLogicalClassField != null)
			{
				FormationLogicalClassField.SetValue(formation, FormationClass.Cavalry);
			}
			if (FormationLogicalClassNeedsUpdateField != null)
			{
				FormationLogicalClassNeedsUpdateField.SetValue(formation, false);
			}
			if (_lordFormationClassForceLogCount < 1)
			{
				_lordFormationClassForceLogCount++;
				Log("force_lord_prisoner_formation_class reason=" + reason + " old_rep=" + oldRepresentativeClass + " new_rep=" + FormationClass.Cavalry + " old_logical=" + (oldLogicalClass ?? "null") + " old_needs_update=" + (oldLogicalClassNeedsUpdate ?? "null"));
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

	private static TroopRoster CloneRoster(TroopRoster sourceRoster)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		TroopRoster val = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return val;
		}
		foreach (TroopRosterElement item in SnapshotRoster(sourceRoster))
		{
			TroopRosterElement current = item;
			if (current.Character != null && current.Number > 0)
			{
				val.Add(current);
			}
		}
		return val;
	}

	private static List<TroopRosterElement> SnapshotRoster(TroopRoster roster)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		if (roster == null)
		{
			return list;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			try
			{
				elementCopyAtIndex.Xp = roster.GetElementXp(i);
			}
			catch
			{
			}
			if (elementCopyAtIndex.Character != null && elementCopyAtIndex.Number > 0)
			{
				list.Add(elementCopyAtIndex);
			}
		}
		return list;
	}

	private void TryLogAgentCounts()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		if (((MissionBehavior)this).Mission == null || (!_deploymentEndDetected && ((MissionBehavior)this).Mission.CurrentTime < 3f))
		{
			return;
		}
		try
		{
			Team playerTeam = ((MissionBehavior)this).Mission.PlayerTeam;
			BattleSideEnum val = ((playerTeam != null) ? playerTeam.Side : PartyBase.MainParty.Side);
			BattleSideEnum oppositeSide = TaleWorlds.Core.Extensions.GetOppositeSide(val);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive())
				{
					Team team = item.Team;
					if (team == null)
					{
						num3++;
					}
					else if (team.Side == val)
					{
						num++;
					}
					else if (team.Side == oppositeSide)
					{
						num2++;
					}
					else
					{
						num3++;
					}
				}
			}
			if (!_agentCountsLogged)
			{
				_agentCountsLogged = true;
				Log($"agent_counts player_side={val} enemy_side={oppositeSide} player_agents={num} enemy_agents={num2} neutral_agents={num3}");
			}
			if (!_enemyAgentWarningLogged && num2 > 0)
			{
				_enemyAgentWarningLogged = true;
				Log($"enemy_agents_detected count={num2}");
			}
		}
		catch (Exception ex)
		{
			Log("agent_count_log failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void ContinuousAgentRefresh(float dt)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		_continuousRefreshTimer += dt;
		if (_continuousRefreshTimer < 0.12f)
		{
			return;
		}
		_continuousRefreshTimer = 0f;
		try
		{
			Campaign current = Campaign.Current;
			int num;
			if (((current != null) ? current.ConversationManager : null) != null && Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				num = ((Campaign.Current.ConversationManager.OneToOneConversationAgent != null) ? 1 : 0);
				if (num != 0 && !_conversationStateLogged)
				{
					_conversationStateLogged = true;
					Log("conversation_state_changed in_conversation=true");
				}
			}
			else
			{
				num = 0;
			}
			if (num == 0)
			{
				_conversationStateLogged = false;
			}
			Mission mission = ((MissionBehavior)this).Mission;
			Agent val = ((mission != null) ? mission.MainAgent : null);
			Mission mission2 = ((MissionBehavior)this).Mission;
			Team val2 = ((mission2 != null) ? mission2.PlayerTeam : null);
			if (val == null || val2 == null)
			{
				return;
			}
			_ = val.Position;
			int num2 = 0;
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive() && item != val && item.Team == val2 && !(item.Origin is PrisonerAgentOrigin))
				{
					RefreshSingleAgent(item);
					num2++;
				}
			}
			if (!_firstRefreshLogged)
			{
				_firstRefreshLogged = true;
				Log($"continuous_refresh_started agents_refreshed={num2} interval={0.12f} radius={30f} refresh_all={true}");
			}
		}
		catch (Exception ex)
		{
			Log("continuous_refresh error: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RefreshSingleAgent(Agent agent)
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
		TrySetAgentController(agent, "AI");
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.DisableScriptedMovement();
				mountAgent.ClearTargetFrame();
				mountAgent.SetIsAIPaused(false);
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
		_prisonerPoseRefreshTimer = 0.35f;
		try
		{
			if (((MissionBehavior)this).Mission == null)
			{
				return;
			}
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				if (item != null && item.IsActive() && item.Origin is PrisonerAgentOrigin && TryGetPrisonerIsLord(item, out var isLord))
				{
					ApplyPrisonerPose(item, isLord, _deploymentEndDetected);
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
		BasicCharacterObject obj = (agent.Origin as PrisonerAgentOrigin)?.Troop;
		CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
		if (val == null)
		{
			return false;
		}
		isLord = ((BasicCharacterObject)val).IsHero;
		_prisonerIsLordMap[agent] = isLord;
		return true;
	}

	private void CachePrisonerActions()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!_prisonerActionsCached)
		{
			_prisonerActionsCached = true;
			_lordPrisonerAction = ActionIndexCache.act_scared_idle_1;
			_soldierPrisonerAction = ActionIndexCache.act_scared_idle_1;
			Log("prisoner_actions_cached lord=act_scared_idle_1 soldier=act_scared_idle_1 static_speed=0 progress=0.35");
		}
	}

	private void ApplyPrisonerPose(Agent agent, bool isLord, bool afterDeployment)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		CachePrisonerActions();
		try
		{
			agent.SetIsAIPaused(true);
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
			agent.SetAgentFlags((agentFlags & ~AgentFlag.CanGetAlarmed));
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
		if (afterDeployment && !IsPrisonerPoseTemporarilySuppressed(agent))
		{
			TrySetCivilianPrisonerActionSet(agent);
			TrySetPrisonerAction(agent, isLord);
		}
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
		for (int i = 0; i < 5; i++)
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
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		ActionIndexCache val = (isLord ? _lordPrisonerAction : _soldierPrisonerAction);
		string text = "act_scared_idle_1";
		int num = 0;
		try
		{
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, in val))
			{
				if (isLord && !_lordPrisonerActionMissingLogged)
				{
					_lordPrisonerActionMissingLogged = true;
					Log("prisoner_action_missing action=" + text);
				}
				if (!isLord && !_soldierPrisonerActionMissingLogged)
				{
					_soldierPrisonerActionMissingLogged = true;
					Log("prisoner_action_missing action=" + text);
				}
			}
			else
			{
				if (agent.GetCurrentAction(num) == val && _prisonerPoseApplied.Contains(agent))
				{
					return;
				}
				AnimFlags val2 = (AnimFlags)143881404416L;
				if (agent.SetActionChannel(num, in val, true, val2, 0f, 0f, -0.2f, 0.4f, 0.35f, false, -0.2f, 0, true))
				{
					try
					{
						agent.SetCurrentActionProgress(num, 0.35f);
					}
					catch
					{
					}
					_prisonerPoseApplied.Add(agent);
				}
				else if (!_prisonerActionSetRejectedLogged)
				{
					_prisonerActionSetRejectedLogged = true;
					Log("set_prisoner_action rejected action=" + text);
				}
			}
		}
		catch (Exception ex)
		{
			Log("set_prisoner_action failed action=" + text + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void TrySetCivilianPrisonerActionSet(Agent agent)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent != null && agent.IsActive() && !_civilianPrisonerActionSetApplied.Contains(agent))
			{
				Monster monster = agent.Monster;
				if (monster != null)
				{
					string text = (agent.IsFemale ? "as_human_female_villager" : "as_human_villager");
					AnimationSystemData val = MonsterExtensions.FillAnimationSystemData(monster, MBActionSet.GetActionSet(text), 1f, false);
					agent.SetActionSet(ref val);
					_civilianPrisonerActionSetApplied.Add(agent);
				}
			}
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
			if (agent == null || ((MissionBehavior)this).Mission == null)
			{
				return false;
			}
			if (!_prisonerPoseSuppressedUntil.TryGetValue(agent, out var value))
			{
				return false;
			}
			return ((MissionBehavior)this).Mission.CurrentTime < value;
		}
		catch
		{
			return false;
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, attackerWeapon, blow, attackCollisionData);
		try
		{
			if (affectedAgent != null && ((MissionBehavior)this).Mission != null && affectedAgent.Origin is PrisonerAgentOrigin)
			{
				_prisonerPoseSuppressedUntil[affectedAgent] = ((MissionBehavior)this).Mission.CurrentTime + 0.9f;
				_prisonerPoseApplied.Remove(affectedAgent);
			}
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
			PropertyInfo propertyInfo = ((object)agent).GetType().GetProperty("Controller") ?? ((object)agent).GetType().GetProperty("ControllerType");
			if (!(propertyInfo == null) && propertyInfo.CanWrite)
			{
				object obj = Enum.Parse(propertyInfo.PropertyType, controllerType, ignoreCase: true);
				if (obj != null)
				{
					propertyInfo.SetValue(agent, obj);
				}
			}
		}
		catch
		{
		}
	}

	private void RequestCleanup(string reason)
	{
		if (!_cleanupRequested)
		{
			_cleanupRequested = true;
			try
			{
				object arg = _battleEndDisabled;
				Mission mission = ((MissionBehavior)this).Mission;
				Log(string.Format("request_cleanup reason={0} battle_end_disabled={1} mission_result={2}", reason, arg, ((mission == null) ? null : ((object)mission.MissionResult)?.ToString()) ?? "null"));
			}
			catch
			{
			}
			if (TroopInspectionBehavior.IsCurrentInspectionRuntime(_dummyPartyStringId))
			{
				TroopInspectionBehavior.CleanupRuntime(reason);
			}
			_prisonerIsLordMap.Clear();
			_civilianPrisonerActionSetApplied.Clear();
			_prisonerPoseSuppressedUntil.Clear();
			_prisonerPoseApplied.Clear();
		}
	}

	private static void Log(string message)
	{
		TroopInspectionBehavior.Log(message);
	}
}

public class PrisonerAgentOrigin : IAgentOriginBase
{
	private static readonly uint PrisonerFactionColor;

	private static readonly uint PrisonerFactionColor2;

	private readonly CharacterObject _troop;

	private Banner _banner;

	private bool _isRemoved;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	public BasicCharacterObject Troop => (BasicCharacterObject)(object)_troop;

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	public bool IsUnderPlayersCommand => true;

	public uint FactionColor => PrisonerFactionColor;

	public uint FactionColor2 => PrisonerFactionColor2;

	public IBattleCombatant BattleCombatant => (IBattleCombatant)(object)PartyBase.MainParty;

	public int UniqueSeed => MBRandom.RandomInt(1000000);

	public int Seed => CharacterHelper.GetDefaultFaceSeed((BasicCharacterObject)(object)_troop, 0);

	public Banner Banner => _banner;

	public PrisonerAgentOrigin(CharacterObject troop)
	{
		_troop = troop;
		Clan playerClan = Clan.PlayerClan;
		_banner = ((playerClan != null) ? playerClan.Banner : null);
		AgentOriginUtilities.GetDefaultTroopTraits((BasicCharacterObject)(object)_troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		if (!_isRemoved)
		{
			_isRemoved = true;
		}
	}

	public void SetKilled()
	{
		if (!_isRemoved)
		{
			_isRemoved = true;
		}
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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return AgentOriginUtilities.GetDefaultTraitsMask((IAgentOriginBase)(object)this);
	}

	static PrisonerAgentOrigin()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Color val = new Color(1f, 0f, 0f, 1f);
		PrisonerFactionColor = val.ToUnsignedInteger();
		val = new Color(0.6f, 0f, 0f, 1f);
		PrisonerFactionColor2 = val.ToUnsignedInteger();
	}
}

[HarmonyPatch(typeof(SandboxAgentDecideKilledOrUnconsciousModel), "GetAgentStateProbability")]
public static class TroopInspectionDeathRatePatch
{
	public static void Postfix(Agent affectorAgent, Agent effectedAgent, DamageTypes damageType, ref float __result)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Mission current = Mission.Current;
		if (((current != null) ? current.GetMissionBehavior<TroopInspectionMissionLogic>() : null) != null && effectedAgent != null && effectedAgent.IsHuman && ((int)damageType == 1 || (int)damageType == 0))
		{
			__result = 1f;
		}
	}
}

[HarmonyPatch(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase")]
public static class TroopInspectionMeleeDamagePatch
{
	public static bool Prefix(Mission __instance, Agent attacker, Agent victim, ref bool __result)
	{
		if (attacker == null || victim == null || !attacker.IsMainAgent || !victim.IsHuman || !attacker.IsFriendOf(victim))
		{
			return true;
		}
		if (((__instance != null) ? __instance.GetMissionBehavior<TroopInspectionMissionLogic>() : null) == null)
		{
			return true;
		}
		__result = false;
		return false;
	}
}

[HarmonyPatch]
public static class TroopInspectionOrderOfBattlePatch
{
	private const int RegularPrisonerFormationIndex = 6;

	private const int LordPrisonerFormationIndex = 7;

	private static readonly FieldInfo AllFormationsField = AccessTools.Field(typeof(OrderOfBattleVM), "_allFormations");

	[HarmonyPatch(typeof(OrderOfBattleFormationItemVM), "RefreshFormation", new Type[]
	{
		typeof(Formation),
		typeof(DeploymentFormationClass),
		typeof(bool)
	})]
	[HarmonyPrefix]
	private static void RefreshFormationPrefix(Formation formation, ref DeploymentFormationClass overriddenClass, ref bool mustExist)
	{
		if (IsTroopInspectionRuntime() && formation != null)
		{
			int index = formation.Index;
			if ((uint)(index - 6) <= 1u)
			{
				overriddenClass = (DeploymentFormationClass)3;
				mustExist = true;
			}
		}
	}

	[HarmonyPatch(typeof(OrderOfBattleVM), "EnsureAllFormationTypesAreSet")]
	[HarmonyPrefix]
	private static bool EnsureAllFormationTypesAreSetPrefix(OrderOfBattleFormationItemVM f)
	{
		if (!IsTroopInspectionRuntime() || ((f != null) ? f.Formation : null) == null)
		{
			return true;
		}
		int index = f.Formation.Index;
		if (index != 7)
		{
			return index != 6;
		}
		return false;
	}

	[HarmonyPatch(typeof(OrderOfBattleVM), "Tick")]
	[HarmonyPostfix]
	private static void TickPostfix(OrderOfBattleVM __instance)
	{
		if (!IsTroopInspectionRuntime() || __instance == null)
		{
			return;
		}
		try
		{
			if (AllFormationsField?.GetValue(__instance) is List<OrderOfBattleFormationItemVM> allFormations)
			{
				RefreshPrisonerFormationItem(allFormations, 6, (DeploymentFormationClass)3);
				RefreshPrisonerFormationItem(allFormations, 7, (DeploymentFormationClass)3);
			}
		}
		catch
		{
		}
	}

	private static void RefreshPrisonerFormationItem(List<OrderOfBattleFormationItemVM> allFormations, int formationIndex, DeploymentFormationClass deploymentClass)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM val = allFormations[i];
			Formation val2 = ((val != null) ? val.Formation : null);
			if (val2 != null && val2.Index == formationIndex)
			{
				int countOfUnits = val2.CountOfUnits;
				if (countOfUnits > 0 && (val.OrderOfBattleFormationClassInt == 0 || val.TroopCount != countOfUnits || !val.IsSelectable))
				{
					val.RefreshFormation(val2, deploymentClass, true);
					val.OnSizeChanged();
				}
				break;
			}
		}
	}

	private static bool IsTroopInspectionRuntime()
	{
		try
		{
			Mission current = Mission.Current;
			return ((current != null) ? current.GetMissionBehavior<TroopInspectionMissionLogic>() : null) != null;
		}
		catch
		{
			return false;
		}
	}
}
