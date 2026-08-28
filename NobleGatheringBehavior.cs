﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Newtonsoft.Json;
using SandBox;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

internal sealed class NobleGatheringInviteeRecord
{
	public string HeroId { get; set; } = "";

	public string ClanId { get; set; } = "";

	public string Status { get; set; } = "";

	public string Reason { get; set; } = "";

	public double ArrivalDay { get; set; } = -1.0;

	public bool CommandIssued { get; set; }

	public bool RelationRewardApplied { get; set; }

	public string OriginSettlementId { get; set; } = "";

	public string SettlementReturnState { get; set; } = "";

	public string TemporaryPartyId { get; set; } = "";

	public string TemporaryPartyPhase { get; set; } = "";
}

internal sealed class NobleGatheringRecord
{
	public string Id { get; set; } = "";

	public string HostHeroId { get; set; } = "";

	public string HostClanId { get; set; } = "";

	public string KingdomId { get; set; } = "";

	public string SettlementId { get; set; } = "";

	public string State { get; set; } = "";

	public double CreatedDay { get; set; }

	public double StartDay { get; set; }

	public double EndDay { get; set; }

	public bool IsPlayerHosted { get; set; }

	public bool PlayerInvitationNoticeShown { get; set; }

	public string PlayerInvitationStatus { get; set; } = "";

	public bool PlayerInvitationCourierSent { get; set; }

	public double PlayerInvitationCourierNextRetryDay { get; set; } = -1.0;

	public bool PlayerAttendanceRewardApplied { get; set; }

	public double PlayerArrivalDay { get; set; } = -1.0;

	public bool HostCommandIssued { get; set; }

	public string HostOriginSettlementId { get; set; } = "";

	public string HostSettlementReturnState { get; set; } = "";

	public string HostTemporaryPartyId { get; set; } = "";

	public string HostTemporaryPartyPhase { get; set; } = "";

	public int CrisisDecisionLevel { get; set; }

	public string EndReason { get; set; } = "";

	public bool WeeklyStartMaterialRecorded { get; set; }

	public int InvitedClanRelationReward { get; set; } = -1;

	public List<string> RelationRewardedClanIds { get; set; } = new List<string>();

	public List<NobleGatheringInviteeRecord> Invitees { get; set; } = new List<NobleGatheringInviteeRecord>();
}

internal sealed class NobleGatheringInvitationSelector
{
	public string SettlementId { get; set; } = "";

	public List<string> SpecificHeroIds { get; set; } = new List<string>();

	public List<string> ClanIds { get; set; } = new List<string>();

	public List<string> KingdomIds { get; set; } = new List<string>();

	public List<string> CultureIds { get; set; } = new List<string>();

	public int MinAge { get; set; } = -1;

	public int MaxAge { get; set; } = -1;

	public string Gender { get; set; } = "";
}

internal sealed class NobleGatheringBehavior : CampaignBehaviorBase
{
	private const string LogSource = "NobleGathering";
	private const string SaveKeyGatherings = "_afNobleGatherings_v1";
	private const string SaveKeyPlayerHostCooldowns = "_afNobleGatheringPlayerHostCooldowns_v1";
	private const string SaveKeyNpcKingdomHostDays = "_afNobleGatheringNpcKingdomHostDays_v1";
	private const int PlayerHostCooldownDays = 10;
	private const float ArrivalDistance = 3.0f;
	private const string StateActive = "Active";
	private const string StateFinished = "Finished";
	private const string StateCancelled = "Cancelled";
	private const int CrisisDecisionNone = 0;
	private const int CrisisDecisionWar = 1;
	private const int CrisisDecisionSiege = 2;
	private const string InvitePending = "Pending";
	private const string InviteAccepted = "Accepted";
	private const string InviteDeclined = "Declined";
	private const string InviteArrived = "Arrived";
	private const string InviteFailed = "Failed";
	private const string TemporaryPartyPhaseToGathering = "ToGathering";
	private const string TemporaryPartyPhaseAtGathering = "AtGathering";
	private const string TemporaryPartyPhaseReturning = "Returning";
	private const string TemporaryPartyPhaseCleaned = "Cleaned";
	private const string TemporaryPartyPrefix = "af_noble_gathering_temp_";
	private const int TemporaryPartyTargetFood = 80;
	private const float TemporaryPartySpawnRadius = 8f;
	private const float TemporaryPartySpawnMinRadius = 0.5f;
	private const string SettlementReturnPending = "Pending";
	private const string SettlementReturnIssued = "Issued";
	private const string SettlementReturnSkipped = "Skipped";
	private const string PlayerInvitationInvited = "Invited";
	private const string PlayerInvitationArrived = "Arrived";
	private const string LegacyPlayerInvitationPending = "Pending";
	private const string LegacyPlayerInvitationAccepted = "Accepted";
	private const string LegacyPlayerInvitationDeclined = "Declined";
	private const double PlayerInvitationCourierRetryIntervalDays = 0.25;
	private const string PlayerHostCooldownKey = "player";
	private const string LordHallLocationId = "lordshall";
	private const int FeastHallVisibleNobleLimit = 16;
	private const string TavernWenchSpawnTag = "sp_tavern_wench";
	private const string MusicianSpawnTag = "musician";
	private const string FeastWenchDisplayName = "侍女";
	private const int MaxFeastWenches = 2;
	private const int MaxFeastMusicians = 2;
	private const int FeastMusicGapSeconds = 8;
	private const int FeastMusicianPerformanceRefreshMs = 900;
	private const int FeastMusicianLayoutRefreshMs = 1500;
	private const float FeastMusicianMoveToleranceSquared = 1.0f;
	private const float FeastMusicianTeleportDistanceSquared = 25f;
	private const int PendingTemporaryPartyDestroyDelayMs = 750;
	private const int MaxPendingTemporaryPartyDestroyAttempts = 5;
	private static readonly FieldInfo AgentNameField = typeof(Agent).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly Regex NobleGatheringActionTagRegex = new Regex("\\[ACTION:NOBLE_GATHERING:(?:START|CANCEL):[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly object PendingTemporaryPartyDestroyLock = new object();
	private static readonly Dictionary<string, PendingTemporaryPartyDestroyRecord> PendingTemporaryPartyDestroys = new Dictionary<string, PendingTemporaryPartyDestroyRecord>(StringComparer.OrdinalIgnoreCase);
	private static int _hasPendingTemporaryPartyDestroys;
	private static readonly string[] FeastMainSeatTags = new string[]
	{
		"sp_throne",
		"sp_lord",
		"sp_king",
		"sp_ruler",
		"lord_throne",
		"king_throne",
		"ruler_throne",
		"throne",
		"lord_chair",
		"ruler_chair",
		"chair_lord"
	};

	private readonly Dictionary<string, NobleGatheringRecord> _gatherings = new Dictionary<string, NobleGatheringRecord>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, double> _playerHostCooldowns = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, double> _npcKingdomNextHostDays = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _heroActiveFeastId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _feastAttendeeClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly List<LocationCharacter> _addedAtmosphereCharacters = new List<LocationCharacter>();
	private readonly List<LocationCharacter> _hiddenFeastHallNobleCharacters = new List<LocationCharacter>();
	private bool _pendingOpenPlayerGatheringFlow;
	private Hero _pendingGovernorHero;
	private Settlement _pendingSuggestedSettlement;
	private Location _currentAtmosphereLocation;
	private Location _hiddenFeastHallNobleLocation;
	private string _pendingFeastHallVisitHeroId = "";
	private string _pendingFeastHallVisitSettlementId = "";
	private SoundEvent _feastMusicEvent;
	private List<SettlementMusicData> _feastMusicPlayList = new List<SettlementMusicData>();
	private readonly Dictionary<int, FeastMusicianInstrumentChoice> _feastMusicianPerformances = new Dictionary<int, FeastMusicianInstrumentChoice>();
	private readonly HashSet<int> _feastAtmosphereMusicianAgentIndexes = new HashSet<int>();
	private int _feastMusicTrackIndex = -1;
	private long _nextFeastMusicStartUtcTicks;
	private long _nextFeastMusicianPerformanceUtcTicks;
	private long _nextFeastMusicianLayoutUtcTicks;

	private sealed class FeastMusicianInstrumentChoice
	{
		public InstrumentData Instrument { get; }

		public ActionIndexCache Action { get; }

		public float ActionSpeed { get; }

		public FeastMusicianInstrumentChoice(InstrumentData instrument, float actionSpeed)
		{
			Instrument = instrument;
			Action = ActionIndexCache.Create(instrument?.StandingAction);
			ActionSpeed = actionSpeed;
		}
	}

	private readonly struct NobleGatheringOptions
	{
		public readonly bool Enabled;

		public readonly bool EnableNpcAutomaticGatherings;

		public readonly bool AllowNpcPlayerInvitations;

		public readonly bool ShowGuestArrivalMessages;

		public readonly bool AllowNpcGovernorInvitations;

		public readonly int NpcIntervalDays;

		public readonly int DurationDays;

		public readonly int Cost;

		public readonly int InvitedClanRelationReward;

		private NobleGatheringOptions(DuelSettings settings)
		{
			Enabled = settings?.EnableNobleGathering ?? true;
			EnableNpcAutomaticGatherings = settings?.EnableNpcAutomaticNobleGatherings ?? true;
			AllowNpcPlayerInvitations = settings?.AllowNpcNobleGatheringPlayerInvitations ?? true;
			ShowGuestArrivalMessages = settings?.ShowNobleGatheringGuestArrivalMessages ?? true;
			AllowNpcGovernorInvitations = settings?.AllowNpcNobleGatheringGovernorInvitations ?? false;
			NpcIntervalDays = Clamp(
				settings?.NpcNobleGatheringIntervalDays ?? DuelSettings.DefaultNobleGatheringNpcIntervalDays,
				DuelSettings.NobleGatheringNpcIntervalMinDays,
				DuelSettings.NobleGatheringNpcIntervalMaxDays);
			DurationDays = Clamp(
				settings?.NobleGatheringDurationDays ?? DuelSettings.DefaultNobleGatheringDurationDays,
				DuelSettings.NobleGatheringDurationMinDays,
				DuelSettings.NobleGatheringDurationMaxDays);
			Cost = Clamp(
				settings?.NobleGatheringCost ?? DuelSettings.DefaultNobleGatheringCost,
				DuelSettings.NobleGatheringCostMinimum,
				DuelSettings.NobleGatheringCostMaximum);
			InvitedClanRelationReward = Clamp(
				settings?.NobleGatheringInvitedClanRelationReward ?? DuelSettings.DefaultNobleGatheringInvitedClanRelationReward,
				DuelSettings.NobleGatheringInvitedClanRelationRewardMinimum,
				DuelSettings.NobleGatheringInvitedClanRelationRewardMaximum);
		}

		public static NobleGatheringOptions Capture()
		{
			try
			{
				return new NobleGatheringOptions(DuelSettings.GetSettings());
			}
			catch
			{
				return new NobleGatheringOptions(null);
			}
		}

		private static int Clamp(int value, int minimum, int maximum)
		{
			return Math.Max(minimum, Math.Min(maximum, value));
		}
	}

	private sealed class PendingTemporaryPartyDestroyRecord
	{
		public string PartyId;

		public string Reason;

		public long MarkedUtcTicks;

		public int Attempts;
	}

	public static NobleGatheringBehavior Instance { get; private set; }

	public NobleGatheringBehavior()
	{
		Instance = this;
	}

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		harmony.CreateClassProcessor(typeof(FeastHallVisitTargetPatch)).Patch();
	}

	internal static void NoteFeastHallVisitTargetForExternal(Location nextLocation, CharacterObject talkToChar)
	{
		try
		{
			Instance?.RememberFeastHallVisitTarget(nextLocation, talkToChar);
		}
		catch (Exception ex)
		{
			Log("record visit target failed: " + ex.Message);
		}
	}

	[HarmonyPatch]
	private static class FeastHallVisitTargetPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			MethodInfo town = AccessTools.Method(typeof(TownEncounter), "CreateAndOpenMissionController");
			if (town != null)
			{
				yield return town;
			}
			MethodInfo castle = AccessTools.Method(typeof(CastleEncounter), "CreateAndOpenMissionController");
			if (castle != null)
			{
				yield return castle;
			}
		}

		private static void Prefix(Location nextLocation, CharacterObject talkToChar)
		{
			NoteFeastHallVisitTargetForExternal(nextLocation, talkToChar);
		}
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnFeastAtmosphereMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnFeastAtmosphereMissionEnded);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, OnFeastAtmosphereLocationCharactersAreReadyToSpawn);
	}

	public override void SyncData(IDataStore dataStore)
	{
		Dictionary<string, string> gatheringStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> cooldownStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> npcKingdomHostDayStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (dataStore.IsSaving)
		{
			foreach (KeyValuePair<string, NobleGatheringRecord> pair in _gatherings)
			{
				if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
				{
					gatheringStorage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
				}
			}
			foreach (KeyValuePair<string, double> pair in _playerHostCooldowns)
			{
				if (!string.IsNullOrWhiteSpace(pair.Key))
				{
					cooldownStorage[pair.Key] = pair.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
				}
			}
			foreach (KeyValuePair<string, double> pair in _npcKingdomNextHostDays)
			{
				if (!string.IsNullOrWhiteSpace(pair.Key))
				{
					npcKingdomHostDayStorage[pair.Key] = pair.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
				}
			}
			gatheringStorage = CampaignSaveChunkHelper.FlattenStringDictionary(gatheringStorage, SaveKeyGatherings, LogSource);
			cooldownStorage = CampaignSaveChunkHelper.FlattenStringDictionary(cooldownStorage, SaveKeyPlayerHostCooldowns, LogSource);
			npcKingdomHostDayStorage = CampaignSaveChunkHelper.FlattenStringDictionary(npcKingdomHostDayStorage, SaveKeyNpcKingdomHostDays, LogSource);
		}
		dataStore.SyncData(SaveKeyGatherings, ref gatheringStorage);
		dataStore.SyncData(SaveKeyPlayerHostCooldowns, ref cooldownStorage);
		dataStore.SyncData(SaveKeyNpcKingdomHostDays, ref npcKingdomHostDayStorage);
		if (!dataStore.IsLoading)
		{
			return;
		}
		_gatherings.Clear();
		gatheringStorage = CampaignSaveChunkHelper.RestoreStringDictionary(gatheringStorage, LogSource);
		foreach (KeyValuePair<string, string> pair in gatheringStorage ?? new Dictionary<string, string>())
		{
			try
			{
				NobleGatheringRecord record = JsonConvert.DeserializeObject<NobleGatheringRecord>(pair.Value ?? "");
				if (record != null && !string.IsNullOrWhiteSpace(record.Id))
				{
					NormalizeRecord(record);
					_gatherings[record.Id] = record;
				}
			}
			catch (Exception ex)
			{
				Log("load gathering failed key=" + pair.Key + " error=" + ex.Message);
			}
		}
		_playerHostCooldowns.Clear();
		cooldownStorage = CampaignSaveChunkHelper.RestoreStringDictionary(cooldownStorage, LogSource);
		foreach (KeyValuePair<string, string> pair in cooldownStorage ?? new Dictionary<string, string>())
		{
			if (double.TryParse(pair.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double day))
			{
				_playerHostCooldowns[pair.Key] = day;
			}
		}
		_npcKingdomNextHostDays.Clear();
		npcKingdomHostDayStorage = CampaignSaveChunkHelper.RestoreStringDictionary(npcKingdomHostDayStorage, LogSource);
		foreach (KeyValuePair<string, string> pair in npcKingdomHostDayStorage ?? new Dictionary<string, string>())
		{
			if (double.TryParse(pair.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double day))
			{
				_npcKingdomNextHostDays[pair.Key] = day;
			}
		}
		RebuildFeastAttendeeIndex();
	}

	public void OnEngineTick()
	{
		ProcessPendingTemporaryPartyDestroysOnEngineTick();
		if (Mission.Current == null)
		{
			if (_feastMusicEvent != null || _feastMusicPlayList.Count > 0 || _feastMusicianPerformances.Count > 0)
			{
				StopFeastHallMusic();
			}
		}
		else
		{
			UpdateFeastHallMusic();
			UpdateFeastMusicianPerformances();
			UpdateFeastMusicianLayout();
		}
		if (_pendingOpenPlayerGatheringFlow)
		{
			Hero governor = _pendingGovernorHero;
			Settlement suggestedSettlement = _pendingSuggestedSettlement;
			_pendingOpenPlayerGatheringFlow = false;
			_pendingGovernorHero = null;
			_pendingSuggestedSettlement = null;
			OpenPlayerGatheringFlow(governor, suggestedSettlement);
		}
	}

	public bool HasActiveGatheringAtSettlement(Settlement settlement)
	{
		return GetActiveGatheringAtSettlement(settlement) != null;
	}

	private NobleGatheringRecord GetActiveGatheringAtSettlement(Settlement settlement)
	{
		string settlementId = (settlement?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return null;
		}
		double now = NowDay();
		return _gatherings.Values
			.Where(record =>
				record != null
				&& string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase)
				&& now < record.EndDay
				&& string.Equals(record.SettlementId, settlementId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(record => record.IsPlayerHosted)
			.ThenBy(record => record.StartDay)
			.FirstOrDefault();
	}

	private void OnFeastAtmosphereMissionStarted(IMission mission)
	{
		RestoreHiddenFeastHallNobles();
		CleanupAddedAtmosphereCharacters();
		StopFeastHallMusic();
		try
		{
			ConfigureFeastMusicianGroups(mission);
			UpdateFeastHallMusic();
		}
		catch (Exception ex)
		{
			Log("music setup failed: " + ex.Message);
		}
	}

	private void OnFeastAtmosphereMissionEnded(IMission mission)
	{
		CleanupAddedAtmosphereCharacters();
		RestoreHiddenFeastHallNobles();
		StopFeastHallMusic();
		ClearFeastMusicianPerformances();
		_nextFeastMusicianLayoutUtcTicks = 0L;
		_pendingFeastHallVisitHeroId = "";
		_pendingFeastHallVisitSettlementId = "";
	}

	private void OnFeastAtmosphereLocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		try
		{
			if (!TryGetCurrentFeastLordHall(out Settlement settlement, out Location location))
			{
				return;
			}
			_currentAtmosphereLocation = location;
			ApplyFeastHallNobleDisplayLimit(settlement, location);
			AddFeastWenches(location, settlement, unusedUsablePointCount);
			AddFeastMusicians(location, settlement, unusedUsablePointCount);
			ConfigureFeastMusicianGroups(Mission.Current);
			UpdateFeastHallMusic();
			UpdateFeastMusicianLayout(force: true);
		}
		catch (Exception ex)
		{
			Log("spawn setup failed: " + ex.Message);
		}
	}

	private void ConfigureFeastMusicianGroups(IMission mission)
	{
		if (!(mission is Mission missionInstance) || !TryGetCurrentFeastLordHall(out Settlement settlement, out _))
		{
			return;
		}
		List<SettlementMusicData> playList = CreateFeastPlayList(settlement);
		if (playList.Count == 0)
		{
			return;
		}
		foreach (MusicianGroup musicianGroup in missionInstance.MissionObjects.FindAllWithType<MusicianGroup>())
		{
			musicianGroup.SetPlayList(playList);
		}
	}

	private void UpdateFeastHallMusic()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || !TryGetCurrentFeastLordHall(out Settlement settlement, out _))
			{
				StopFeastHallMusic();
				return;
			}
			if (_feastMusicEvent != null)
			{
				if (_feastMusicEvent.IsPlaying())
				{
					_feastMusicEvent.SetPosition(GetFeastMusicPosition());
					return;
				}
				ReleaseFeastMusicEvent();
				_nextFeastMusicStartUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(FeastMusicGapSeconds).Ticks;
			}
			if (DateTime.UtcNow.Ticks < _nextFeastMusicStartUtcTicks)
			{
				return;
			}
			StartNextFeastMusicTrack(mission, settlement);
		}
		catch (Exception ex)
		{
			Log("music tick failed: " + ex.Message);
			StopFeastHallMusic();
		}
	}

	private void StartNextFeastMusicTrack(Mission mission, Settlement settlement)
	{
		if (mission?.Scene == null)
		{
			return;
		}
		if (_feastMusicPlayList == null || _feastMusicPlayList.Count == 0)
		{
			_feastMusicPlayList = CreateFeastPlayList(settlement);
			_feastMusicTrackIndex = -1;
		}
		if (_feastMusicPlayList.Count == 0)
		{
			return;
		}
		_feastMusicTrackIndex++;
		if (_feastMusicTrackIndex >= _feastMusicPlayList.Count)
		{
			_feastMusicTrackIndex = 0;
		}
		SettlementMusicData track = _feastMusicPlayList[_feastMusicTrackIndex];
		if (track == null || string.IsNullOrWhiteSpace(track.MusicPath))
		{
			return;
		}
		int eventId = SoundEvent.GetEventIdFromString(track.MusicPath);
		_feastMusicEvent = SoundEvent.CreateEvent(eventId, mission.Scene);
		if (_feastMusicEvent == null)
		{
			return;
		}
		_feastMusicEvent.SetPosition(GetFeastMusicPosition());
		_feastMusicEvent.Play();
	}

	private void StopFeastHallMusic()
	{
		ReleaseFeastMusicEvent();
		_feastMusicPlayList.Clear();
		_feastMusicTrackIndex = -1;
		_nextFeastMusicStartUtcTicks = 0L;
		_nextFeastMusicianLayoutUtcTicks = 0L;
		ClearFeastMusicianPerformances();
	}

	private void ReleaseFeastMusicEvent()
	{
		if (_feastMusicEvent == null)
		{
			return;
		}
		try
		{
			if (_feastMusicEvent.IsPlaying())
			{
				_feastMusicEvent.Stop();
			}
			_feastMusicEvent.Release();
		}
		catch
		{
		}
		_feastMusicEvent = null;
	}

	private static Vec3 GetFeastMusicPosition()
	{
		Agent mainAgent = Agent.Main;
		return mainAgent != null ? mainAgent.Position : Vec3.Zero;
	}

	private void UpdateFeastMusicianLayout(bool force = false)
	{
		try
		{
			long ticks = DateTime.UtcNow.Ticks;
			if (!force && ticks < _nextFeastMusicianLayoutUtcTicks)
			{
				return;
			}
			_nextFeastMusicianLayoutUtcTicks = ticks + TimeSpan.FromMilliseconds(FeastMusicianLayoutRefreshMs).Ticks;
			Mission mission = Mission.Current;
			if (mission?.Agents == null || mission.Scene == null || !TryGetCurrentFeastLordHall(out Settlement settlement, out _))
			{
				return;
			}
			List<Agent> musicians = GetTrackedFeastMusicianAgents(mission, settlement)
				.OrderBy(agent => agent.Index)
				.Take(MaxFeastMusicians)
				.ToList();
			if (musicians.Count == 0)
			{
				return;
			}
			MatrixFrame mainFrame = ResolveFeastMusicianMainFrame(mission, musicians);
			Vec3 anchor = mainFrame.origin;
			Vec3 forward = NormalizePlanar(mainFrame.rotation.f, new Vec3(1f, 0f, 0f));
			Vec3 right = NormalizePlanar(new Vec3(forward.y, -forward.x, 0f), new Vec3(0f, 1f, 0f));
			for (int i = 0; i < musicians.Count; i++)
			{
				Vec3 position = BuildFeastMusicianLayoutPosition(i, anchor, forward, right, null);
				Vec3 lookTarget = position + forward * 4f;
				ApplyFeastMusicianLayoutFrame(mission, musicians[i], position, lookTarget, force);
			}
			PruneFeastMusicianLayoutAgents(mission);
		}
		catch (Exception ex)
		{
			Log("musician layout tick failed: " + ex.Message);
		}
	}

	private List<Agent> GetTrackedFeastMusicianAgents(Mission mission, Settlement settlement)
	{
		CharacterObject musician = settlement?.Culture?.Musician;
		if (mission?.Agents == null || musician == null)
		{
			return new List<Agent>();
		}
		if (_feastAtmosphereMusicianAgentIndexes.Count > 0)
		{
			return mission.Agents
				.Where(agent => agent != null
					&& agent.IsHuman
					&& agent.IsActive()
					&& agent.Character == musician
					&& _feastAtmosphereMusicianAgentIndexes.Contains(agent.Index))
				.ToList();
		}
		HashSet<LocationCharacter> tracked = new HashSet<LocationCharacter>(_addedAtmosphereCharacters.Where(character => character?.Character == musician));
		if (tracked.Count == 0)
		{
			return new List<Agent>();
		}
		return mission.Agents
			.Where(agent => agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& agent.Character == musician
				&& IsTrackedFeastLocationCharacter(agent, tracked))
			.ToList();
	}

	private static bool IsTrackedFeastLocationCharacter(Agent agent, HashSet<LocationCharacter> tracked)
	{
		if (agent == null || tracked == null || tracked.Count == 0)
		{
			return false;
		}
		try
		{
			LocationCharacter locationCharacter = LocationComplex.Current?.FindCharacter(agent);
			return locationCharacter != null && tracked.Contains(locationCharacter);
		}
		catch
		{
			return false;
		}
	}

	private static MatrixFrame ResolveFeastMusicianMainFrame(Mission mission, List<Agent> musicians)
	{
		if (TryGetFeastTaggedFrame(mission?.Scene, FeastMainSeatTags, out MatrixFrame frame))
		{
			return frame;
		}
		Vec3 origin = musicians != null && musicians.Count > 0 ? AverageAgentPosition(musicians) : Vec3.Zero;
		return BuildFeastFacingFrame(origin, new Vec3(1f, 0f, 0f));
	}

	private static List<Vec3> FindFeastDoorGuardPositions(Mission mission, Settlement settlement, Vec3 anchor)
	{
		CharacterObject musician = settlement?.Culture?.Musician;
		CharacterObject tavernWench = settlement?.Culture?.TavernWench;
		if (mission?.Agents == null)
		{
			return new List<Vec3>();
		}
		return mission.Agents
			.Where(agent => IsFeastDoorGuardCandidate(agent, musician, tavernWench))
			.OrderByDescending(agent => GetFeastGuardScore(agent, anchor))
			.Take(2)
			.Select(agent => agent.Position)
			.ToList();
	}

	private static bool IsFeastDoorGuardCandidate(Agent agent, CharacterObject musician, CharacterObject tavernWench)
	{
		if (agent == null || agent == Agent.Main || !agent.IsHuman || !agent.IsActive())
		{
			return false;
		}
		if (agent.Character == musician || agent.Character == tavernWench)
		{
			return false;
		}
		CharacterObject character = agent.Character as CharacterObject;
		if (character == null || character.HeroObject != null)
		{
			return false;
		}
		return true;
	}

	private static float GetFeastGuardScore(Agent agent, Vec3 anchor)
	{
		float score = (agent.Position - anchor).LengthSquared;
		try
		{
			string id = ((agent.Character as CharacterObject)?.StringId ?? "").ToLowerInvariant();
			string name = (agent.Character?.Name?.ToString() ?? "").ToLowerInvariant();
			if (id.Contains("guard") || id.Contains("sentinel") || id.Contains("watchman") || name.Contains("guard") || name.Contains("sentinel"))
			{
				score += 1000f;
			}
		}
		catch
		{
		}
		return score;
	}

	private static Vec3 BuildFeastMusicianLayoutPosition(int index, Vec3 anchor, Vec3 forward, Vec3 right, List<Vec3> guardPositions)
	{
		if (index == 0)
		{
			return anchor + forward * 1.15f + right * 2.25f;
		}
		if (index == 1)
		{
			return anchor + forward * 1.15f - right * 2.25f;
		}
		int doorIndex = index - 2;
		if (guardPositions != null && guardPositions.Count > 0)
		{
			Vec3 basePosition = guardPositions[Math.Min(doorIndex, guardPositions.Count - 1)];
			Vec3 outward = NormalizePlanar(basePosition - anchor, forward);
			Vec3 side = NormalizePlanar(new Vec3(outward.y, -outward.x, 0f), right);
			return basePosition + outward * 0.55f + side * (doorIndex % 2 == 0 ? 1.15f : -1.15f);
		}
		return anchor + forward * 6.4f + right * (doorIndex % 2 == 0 ? 2.15f : -2.15f);
	}

	private static Vec3 ResolveFeastMusicianDoorLookTarget(int doorIndex, List<Vec3> guardPositions, Vec3 anchor, Vec3 forward)
	{
		if (guardPositions != null && guardPositions.Count > 0)
		{
			return guardPositions[Math.Min(Math.Max(0, doorIndex), guardPositions.Count - 1)];
		}
		return anchor + forward * 3.5f;
	}

	private void ApplyFeastMusicianLayoutFrame(Mission mission, Agent agent, Vec3 position, Vec3 lookTarget, bool force)
	{
		if (mission?.Scene == null || agent == null || agent == Agent.Main || !agent.IsHuman || !agent.IsActive())
		{
			return;
		}
		try
		{
			if (Campaign.Current?.ConversationManager?.OneToOneConversationAgent == agent)
			{
				return;
			}
		}
		catch
		{
		}
		ResolveFeastMusicianStandingHeight(mission.Scene, agent, ref position);
		position.z += 0.03f;
		float distanceSquared = (agent.Position - position).LengthSquared;
		if (!force && distanceSquared < FeastMusicianMoveToleranceSquared)
		{
			return;
		}
		Vec3 facing = NormalizePlanar(lookTarget - position, new Vec3(1f, 0f, 0f));
		try
		{
			if (agent.CurrentlyUsedGameObject != null)
			{
				agent.StopUsingGameObject(false, Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
			}
			agent.ClearTargetFrame();
			Vec2 zero = Vec2.Zero;
			agent.SetMovementDirection(in zero);
			agent.MovementInputVector = Vec2.Zero;
			agent.MovementFlags = Agent.MovementControlFlag.None;
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			agent.SetTargetPosition(position.AsVec2);
			if (distanceSquared > FeastMusicianTeleportDistanceSquared)
			{
				agent.TeleportToPosition(position);
			}
			agent.LookDirection = facing;
			WorldPosition scriptedPosition = new WorldPosition(mission.Scene, UIntPtr.Zero, position, true);
			agent.SetScriptedPositionAndDirection(ref scriptedPosition, facing.AsVec2.RotationInRadians, false, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
			if (_feastMusicianPerformances.TryGetValue(agent.Index, out FeastMusicianInstrumentChoice choice))
			{
				EnsureFeastMusicianPerformance(agent, choice);
			}
		}
		catch (Exception ex)
		{
			Log("apply musician layout failed agent=" + agent.Index + " error=" + ex.Message);
		}
	}

	private static void ResolveFeastMusicianStandingHeight(Scene scene, Agent agent, ref Vec3 position)
	{
		if (scene == null)
		{
			return;
		}
		float referenceZ = agent != null ? agent.Position.z : position.z;
		float bestZ = referenceZ;
		float bestDelta = float.MaxValue;
		try
		{
			float groundHeight = scene.GetGroundHeightAtPosition(position, BodyFlags.CommonCollisionExcludeFlags);
			float groundDelta = Math.Abs(groundHeight - referenceZ);
			if (groundDelta <= 1.25f)
			{
				bestZ = groundHeight;
				bestDelta = groundDelta;
			}
		}
		catch
		{
		}
		try
		{
			float heightAtPoint = position.z;
			if (scene.GetHeightAtPoint(position.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref heightAtPoint))
			{
				float delta = Math.Abs(heightAtPoint - referenceZ);
				if (delta <= 1.25f && delta < bestDelta)
				{
					bestZ = heightAtPoint;
				}
			}
		}
		catch
		{
		}
		position.z = bestZ;
	}

	private static bool TryGetFeastTaggedFrame(Scene scene, string[] tags, out MatrixFrame frame)
	{
		frame = MatrixFrame.Identity;
		if (scene == null || tags == null)
		{
			return false;
		}
		foreach (string tag in tags)
		{
			if (string.IsNullOrWhiteSpace(tag))
			{
				continue;
			}
			try
			{
				GameEntity entity = scene.FindEntityWithTag(tag);
				if (entity != null)
				{
					frame = entity.GetGlobalFrame();
					frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private static MatrixFrame BuildFeastFacingFrame(Vec3 origin, Vec3 forward)
	{
		forward = NormalizePlanar(forward, new Vec3(1f, 0f, 0f));
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = origin;
		frame.rotation.f = forward;
		frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return frame;
	}

	private static Vec3 NormalizePlanar(Vec3 vector, Vec3 fallback)
	{
		vector.z = 0f;
		if (vector.LengthSquared < 0.0001f)
		{
			vector = fallback;
			vector.z = 0f;
		}
		if (vector.LengthSquared < 0.0001f)
		{
			vector = new Vec3(1f, 0f, 0f);
		}
		vector.Normalize();
		return vector;
	}

	private static Vec3 AverageAgentPosition(List<Agent> agents)
	{
		if (agents == null || agents.Count == 0)
		{
			return Vec3.Zero;
		}
		Vec3 sum = Vec3.Zero;
		int count = 0;
		foreach (Agent agent in agents)
		{
			if (agent == null)
			{
				continue;
			}
			sum += agent.Position;
			count++;
		}
		return count > 0 ? sum * (1f / count) : Vec3.Zero;
	}

	private void PruneFeastMusicianLayoutAgents(Mission mission)
	{
		if (mission?.Agents == null || _feastAtmosphereMusicianAgentIndexes.Count == 0)
		{
			return;
		}
		HashSet<int> liveAgentIndexes = new HashSet<int>(mission.Agents.Where(agent => agent != null && agent.IsActive()).Select(agent => agent.Index));
		foreach (int index in _feastAtmosphereMusicianAgentIndexes.ToList())
		{
			if (!liveAgentIndexes.Contains(index))
			{
				_feastAtmosphereMusicianAgentIndexes.Remove(index);
			}
		}
	}

	private void UpdateFeastMusicianPerformances()
	{
		try
		{
			long ticks = DateTime.UtcNow.Ticks;
			if (ticks < _nextFeastMusicianPerformanceUtcTicks)
			{
				return;
			}
			_nextFeastMusicianPerformanceUtcTicks = ticks + TimeSpan.FromMilliseconds(FeastMusicianPerformanceRefreshMs).Ticks;
			Mission mission = Mission.Current;
			if (mission?.Agents == null || !TryGetCurrentFeastLordHall(out Settlement settlement, out _))
			{
				ClearFeastMusicianPerformances();
				return;
			}
			CharacterObject musician = settlement?.Culture?.Musician;
			if (musician == null)
			{
				return;
			}
			List<Agent> musicianAgents = mission.Agents
				.Where(agent => agent != null && agent.IsHuman && agent.IsActive() && agent.Character == musician)
				.ToList();
			if (musicianAgents.Count == 0)
			{
				_feastMusicianPerformances.Clear();
				return;
			}
			List<FeastMusicianInstrumentChoice> choices = null;
			int fallbackSlot = 0;
			HashSet<int> liveAgentIndexes = new HashSet<int>();
			foreach (Agent agent in musicianAgents)
			{
				liveAgentIndexes.Add(agent.Index);
				if (!_feastMusicianPerformances.TryGetValue(agent.Index, out FeastMusicianInstrumentChoice choice) || choice == null)
				{
					choices ??= CreateFeastInstrumentChoices(settlement);
					choice = SelectFeastInstrumentChoice(choices, fallbackSlot++);
					if (choice != null)
					{
						_feastMusicianPerformances[agent.Index] = choice;
					}
				}
				EnsureFeastMusicianPerformance(agent, choice);
			}
			foreach (int index in _feastMusicianPerformances.Keys.ToList())
			{
				if (!liveAgentIndexes.Contains(index))
				{
					_feastMusicianPerformances.Remove(index);
				}
			}
		}
		catch (Exception ex)
		{
			Log("musician performance tick failed: " + ex.Message);
		}
	}

	private void RegisterFeastMusicianAgent(IAgent agent, FeastMusicianInstrumentChoice choice)
	{
		if (!(agent is Agent missionAgent) || choice == null)
		{
			return;
		}
		_feastAtmosphereMusicianAgentIndexes.Add(missionAgent.Index);
		_feastMusicianPerformances[missionAgent.Index] = choice;
		EnsureFeastMusicianPerformance(missionAgent, choice);
		UpdateFeastMusicianLayout(force: true);
	}

	private void ClearFeastMusicianPerformances()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Agents != null)
			{
				foreach (int index in _feastMusicianPerformances.Keys.ToList())
				{
					Agent agent = mission.Agents.FirstOrDefault(candidate => candidate != null && candidate.Index == index);
					if (agent != null && agent.IsActive())
					{
						ClearFeastMusicianAction(agent);
					}
				}
			}
		}
		catch
		{
		}
		_feastMusicianPerformances.Clear();
		_feastAtmosphereMusicianAgentIndexes.Clear();
		_nextFeastMusicianPerformanceUtcTicks = 0L;
		_nextFeastMusicianLayoutUtcTicks = 0L;
	}

	private static void ApplyFeastMusicianPerformance(Agent agent, FeastMusicianInstrumentChoice choice)
	{
		if (agent == null || choice?.Instrument == null || string.IsNullOrWhiteSpace(choice.Instrument.StandingAction))
		{
			return;
		}
		if (!agent.IsHuman || !agent.IsActive())
		{
			return;
		}
		ActionIndexCache action = choice.Action;
		if (!HasActionClip(agent, action))
		{
			return;
		}
		if (agent.CurrentlyUsedGameObject != null)
		{
			try
			{
				agent.StopUsingGameObject(false, Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
			}
			catch
			{
			}
		}
		SetFeastMusicianAction(agent, action, choice.ActionSpeed);
	}

	private static void EnsureFeastMusicianPerformance(Agent agent, FeastMusicianInstrumentChoice choice)
	{
		if (choice == null || IsFeastMusicianPerformanceActive(agent, choice))
		{
			return;
		}
		ApplyFeastMusicianPerformance(agent, choice);
	}

	private static bool IsFeastMusicianPerformanceActive(Agent agent, FeastMusicianInstrumentChoice choice)
	{
		if (agent == null || choice == null)
		{
			return false;
		}
		try
		{
			return agent.GetCurrentAction(0) == choice.Action;
		}
		catch
		{
			return false;
		}
	}

	private static void ClearFeastMusicianAction(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
#if BANNERLORD_1_4_OR_GREATER
		agent.SetActionChannel(0, in ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
#else
		agent.SetActionChannel(0, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
#endif
	}

	private static bool HasActionClip(Agent agent, ActionIndexCache action)
	{
		if (agent == null)
		{
			return false;
		}
#if BANNERLORD_1_4_OR_GREATER
		return MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, in action);
#else
		return MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, action);
#endif
	}

	private static bool SetFeastMusicianAction(Agent agent, ActionIndexCache action, float actionSpeed)
	{
#if BANNERLORD_1_4_OR_GREATER
		return agent.SetActionChannel(0, in action, true, (AnimFlags)0UL, 0f, actionSpeed, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
#else
		return agent.SetActionChannel(0, action, true, (AnimFlags)0UL, 0f, actionSpeed, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
#endif
	}

	private void AddFeastWenches(Location location, Settlement settlement, Dictionary<string, int> unusedUsablePointCount)
	{
		CharacterObject tavernWench = settlement?.Culture?.TavernWench;
		if (location == null || tavernWench == null)
		{
			return;
		}
		int available = GetAvailableCount(unusedUsablePointCount, TavernWenchSpawnTag);
		if (available <= 0)
		{
			available = Math.Max(GetAvailableCount(unusedUsablePointCount, "npc_common_limited"), GetAvailableCount(unusedUsablePointCount, "npc_common"));
		}
		int desiredCount = Math.Min(MaxFeastWenches, Math.Max(0, available));
		int existingCount = CountLocationCharacters(location, tavernWench, TavernWenchSpawnTag);
		for (int i = existingCount; i < desiredCount; i++)
		{
			LocationCharacter character = CreateFeastTavernWench(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
			AddAtmosphereCharacter(location, character);
		}
	}

	private void AddFeastMusicians(Location location, Settlement settlement, Dictionary<string, int> unusedUsablePointCount)
	{
		CharacterObject musician = settlement?.Culture?.Musician;
		if (location == null || musician == null)
		{
			return;
		}
		string spawnTag = GetBestAvailableSpawnTag(unusedUsablePointCount, MusicianSpawnTag, "npc_common_limited", "npc_common");
		int desiredCount = MaxFeastMusicians;
		int existingCount = _addedAtmosphereCharacters.Count(locationCharacter => locationCharacter?.Character == musician);
		List<FeastMusicianInstrumentChoice> instrumentChoices = CreateFeastInstrumentChoices(settlement);
		for (int i = existingCount; i < desiredCount; i++)
		{
			FeastMusicianInstrumentChoice instrumentChoice = SelectFeastInstrumentChoice(instrumentChoices, i);
			LocationCharacter character = CreateFeastMusician(settlement.Culture, LocationCharacter.CharacterRelations.Neutral, spawnTag, instrumentChoice);
			AddAtmosphereCharacter(location, character);
		}
	}

	private void AddAtmosphereCharacter(Location location, LocationCharacter character)
	{
		if (location == null || character == null)
		{
			return;
		}
		location.AddCharacter(character);
		_addedAtmosphereCharacters.Add(character);
	}

	private void CleanupAddedAtmosphereCharacters()
	{
		if (_currentAtmosphereLocation != null)
		{
			foreach (LocationCharacter character in _addedAtmosphereCharacters.ToList())
			{
				try
				{
					_currentAtmosphereLocation.RemoveLocationCharacter(character);
				}
				catch
				{
				}
			}
		}
		_addedAtmosphereCharacters.Clear();
		_currentAtmosphereLocation = null;
	}

	private bool TryGetCurrentFeastLordHall(out Settlement settlement, out Location location)
	{
		settlement = null;
		location = CampaignMission.Current?.Location;
		if (!IsLordHallLocation(location))
		{
			return false;
		}
		settlement = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
		return settlement != null && HasActiveGatheringAtSettlement(settlement);
	}

	private void RememberFeastHallVisitTarget(Location nextLocation, CharacterObject talkToChar)
	{
		if (!IsLordHallLocation(nextLocation) || talkToChar?.HeroObject == null)
		{
			return;
		}
		Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
		if (settlement == null || !HasActiveGatheringAtSettlement(settlement))
		{
			return;
		}
		_pendingFeastHallVisitHeroId = talkToChar.HeroObject.StringId ?? "";
		_pendingFeastHallVisitSettlementId = settlement.StringId ?? "";
	}

	private static bool IsLordHallLocation(Location location)
	{
		return string.Equals(location?.StringId, LordHallLocationId, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(location?.StringId, "lords_hall", StringComparison.OrdinalIgnoreCase);
	}

	private void ApplyFeastHallNobleDisplayLimit(Settlement settlement, Location location)
	{
		NobleGatheringRecord record = GetActiveGatheringAtSettlement(settlement);
		if (record == null || location == null)
		{
			return;
		}
		RestoreHiddenFeastHallNobles();
		List<LocationCharacter> nobles = location.GetCharacterList()
			.Where(IsFeastHallNobleLocationCharacter)
			.ToList();
		if (nobles.Count <= FeastHallVisibleNobleLimit)
		{
			return;
		}
		Hero forcedVisitHero = ResolvePendingFeastHallVisitHero(settlement);
		HashSet<string> inviteeHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (record.Invitees != null)
		{
			foreach (NobleGatheringInviteeRecord invitee in record.Invitees)
			{
				if (!string.IsNullOrWhiteSpace(invitee?.HeroId))
				{
					inviteeHeroIds.Add(invitee.HeroId);
				}
			}
		}
		HashSet<LocationCharacter> keep = new HashSet<LocationCharacter>();
		LocationCharacter forcedVisitCharacter = nobles.FirstOrDefault(character => character?.Character?.HeroObject == forcedVisitHero);
		if (forcedVisitCharacter != null)
		{
			keep.Add(forcedVisitCharacter);
		}
		LocationCharacter hostCharacter = nobles.FirstOrDefault(character => string.Equals(
			character?.Character?.HeroObject?.StringId ?? "",
			record.HostHeroId ?? "",
			StringComparison.OrdinalIgnoreCase));
		if (hostCharacter != null)
		{
			keep.Add(hostCharacter);
		}
		int remainingSlots = Math.Max(0, FeastHallVisibleNobleLimit - keep.Count);
		foreach (LocationCharacter character in nobles
			.Where(character => !keep.Contains(character))
			.OrderBy(character => GetFeastHallNobleDisplayPriority(character?.Character?.HeroObject, record, settlement, forcedVisitHero, inviteeHeroIds))
			.ThenByDescending(character => GetFeastHallNobleImportance(character?.Character?.HeroObject))
			.ThenBy(character => character?.Character?.HeroObject?.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.Take(remainingSlots))
		{
			keep.Add(character);
		}
		int hiddenCount = 0;
		foreach (LocationCharacter character in nobles)
		{
			if (keep.Contains(character))
			{
				continue;
			}
			try
			{
				location.RemoveLocationCharacter(character);
				_hiddenFeastHallNobleCharacters.Add(character);
				_hiddenFeastHallNobleLocation = location;
				hiddenCount++;
			}
			catch (Exception ex)
			{
				Log("hide feast hall noble failed hero=" + (character?.Character?.HeroObject?.StringId ?? "") + " error=" + ex.Message);
			}
		}
		Log("feast hall noble limit settlement=" + (settlement.StringId ?? "")
			+ " total=" + nobles.Count
			+ " visible=" + keep.Count
			+ " hidden=" + hiddenCount
			+ " limit=" + FeastHallVisibleNobleLimit);
	}

	private void RestoreHiddenFeastHallNobles()
	{
		if (_hiddenFeastHallNobleLocation == null || _hiddenFeastHallNobleCharacters.Count == 0)
		{
			_hiddenFeastHallNobleCharacters.Clear();
			_hiddenFeastHallNobleLocation = null;
			return;
		}
		Location location = _hiddenFeastHallNobleLocation;
		foreach (LocationCharacter character in _hiddenFeastHallNobleCharacters.ToList())
		{
			try
			{
				if (character != null && !location.ContainsCharacter(character))
				{
					location.AddCharacter(character);
				}
			}
			catch (Exception ex)
			{
				Log("restore feast hall noble failed hero=" + (character?.Character?.HeroObject?.StringId ?? "") + " error=" + ex.Message);
			}
		}
		_hiddenFeastHallNobleCharacters.Clear();
		_hiddenFeastHallNobleLocation = null;
	}

	private Hero ResolvePendingFeastHallVisitHero(Settlement settlement)
	{
		if (settlement == null
			|| string.IsNullOrWhiteSpace(_pendingFeastHallVisitHeroId)
			|| !string.Equals(_pendingFeastHallVisitSettlementId ?? "", settlement.StringId ?? "", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		Hero hero = ResolveHeroById(_pendingFeastHallVisitHeroId);
		return hero != null && !hero.IsDead && !hero.IsPrisoner ? hero : null;
	}

	private static bool IsFeastHallNobleLocationCharacter(LocationCharacter locationCharacter)
	{
		Hero hero = locationCharacter?.Character?.HeroObject;
		return hero != null
			&& hero != Hero.MainHero
			&& !hero.IsDead
			&& (hero.IsLord || hero.Occupation == Occupation.Lord);
	}

	private static int GetFeastHallNobleDisplayPriority(
		Hero hero,
		NobleGatheringRecord record,
		Settlement settlement,
		Hero forcedVisitHero,
		HashSet<string> inviteeHeroIds)
	{
		if (hero == null)
		{
			return 1000;
		}
		if (forcedVisitHero != null && hero == forcedVisitHero)
		{
			return 0;
		}
		if (record != null && string.Equals(hero.StringId ?? "", record.HostHeroId ?? "", StringComparison.OrdinalIgnoreCase))
		{
			return 10;
		}
		if (inviteeHeroIds?.Contains(hero.StringId ?? "") == true)
		{
			return 20;
		}
		if (IsKingdomLeaderForFeast(hero, record, settlement))
		{
			return 30;
		}
		if (hero.Clan?.Leader == hero || hero.IsClanLeader)
		{
			return 40;
		}
		if (hero.GovernorOf != null)
		{
			return 50;
		}
		return 60;
	}

	private static bool IsKingdomLeaderForFeast(Hero hero, NobleGatheringRecord record, Settlement settlement)
	{
		if (hero == null)
		{
			return false;
		}
		Kingdom kingdom = ResolveKingdomToken(record?.KingdomId) ?? (settlement?.MapFaction as Kingdom) ?? hero.Clan?.Kingdom;
		return kingdom?.Leader == hero;
	}

	private static int GetFeastHallNobleImportance(Hero hero)
	{
		if (hero == null)
		{
			return 0;
		}
		int score = 0;
		try
		{
			score += Math.Max(0, hero.Clan?.Tier ?? 0) * 1000;
		}
		catch
		{
		}
		try
		{
			score += Math.Max(0, hero.CharacterObject?.Level ?? 0) * 10;
		}
		catch
		{
		}
		if (hero.IsFemale)
		{
			score += 1;
		}
		return score;
	}

	private static int GetAvailableCount(Dictionary<string, int> unusedUsablePointCount, string tag)
	{
		if (unusedUsablePointCount == null || string.IsNullOrWhiteSpace(tag))
		{
			return 0;
		}
		return unusedUsablePointCount.TryGetValue(tag, out int count) ? Math.Max(0, count) : 0;
	}

	private static string GetBestAvailableSpawnTag(Dictionary<string, int> unusedUsablePointCount, params string[] tags)
	{
		foreach (string tag in tags ?? Array.Empty<string>())
		{
			if (GetAvailableCount(unusedUsablePointCount, tag) > 0)
			{
				return tag;
			}
		}
		return (tags != null && tags.Length > 0) ? tags[0] : "";
	}

	private static int CountLocationCharacters(Location location, CharacterObject character)
	{
		if (location == null || character == null)
		{
			return 0;
		}
		return location.GetCharacterList().Count(locationCharacter =>
			locationCharacter != null
			&& locationCharacter.Character == character);
	}

	private static int CountLocationCharacters(Location location, CharacterObject character, string spawnTag)
	{
		if (location == null || character == null)
		{
			return 0;
		}
		return location.GetCharacterList().Count(locationCharacter =>
			locationCharacter != null
			&& locationCharacter.Character == character
			&& string.Equals(locationCharacter.SpecialTargetTag ?? "", spawnTag ?? "", StringComparison.OrdinalIgnoreCase));
	}

	private static LocationCharacter CreateFeastTavernWench(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject tavernWench = culture.TavernWench;
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tavernWench, out int minAge, out int maxAge, "");
		Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(tavernWench.Race, "_settlement");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(tavernWench, -1, null, default(UniqueTroopDescriptor)))
			.Monster(monster)
			.Age(MBRandom.RandomInt(minAge, maxAge));
		return new LocationCharacter(
			agentData,
			SandBoxManager.Instance.AgentBehaviorManager.AddFixedGuardBehaviors,
			TavernWenchSpawnTag,
			true,
			relation,
			ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_barmaid"),
			true,
			false,
			null,
			false,
			false,
			true,
			ApplyFeastWenchDisplayName,
			false)
		{
			PrefabNamesForBones =
			{
				{
					agentData.AgentMonster.OffHandItemBoneIndex,
					"kitchen_pitcher_b_tavern"
				}
			}
		};
	}

	private static void ApplyFeastWenchDisplayName(IAgent agent)
	{
		if (!(agent is Agent missionAgent) || AgentNameField == null)
		{
			return;
		}
		try
		{
			AgentNameField.SetValue(missionAgent, new TextObject(FeastWenchDisplayName));
		}
		catch (Exception ex)
		{
			Log("rename feast wench failed: " + ex.Message);
		}
	}

	private LocationCharacter CreateFeastMusician(CultureObject culture, LocationCharacter.CharacterRelations relation, string spawnTag, FeastMusicianInstrumentChoice instrumentChoice)
	{
		CharacterObject musician = culture.Musician;
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(musician, out int minAge, out int maxAge, "");
		Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(musician.Race, "_settlement");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(musician, -1, null, default(UniqueTroopDescriptor)))
			.Monster(monster)
			.Age(MBRandom.RandomInt(minAge, maxAge));
		LocationCharacter character = new LocationCharacter(
			agentData,
			SandBoxManager.Instance.AgentBehaviorManager.AddFixedGuardBehaviors,
			string.IsNullOrWhiteSpace(spawnTag) ? MusicianSpawnTag : spawnTag,
			true,
			relation,
			ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_musician"),
			true,
			false,
			null,
			false,
			false,
			true,
			agent => RegisterFeastMusicianAgent(agent, instrumentChoice),
			false);
		AddInstrumentPrefabs(character, instrumentChoice?.Instrument);
		return character;
	}

	private static void AddInstrumentPrefabs(LocationCharacter character, InstrumentData instrument)
	{
		if (character?.PrefabNamesForBones == null || instrument?.InstrumentEntities == null)
		{
			return;
		}
		foreach (var entity in instrument.InstrumentEntities)
		{
			HumanBone bone = entity.Item1;
			string prefabName = entity.Item2;
			if (bone == HumanBone.Invalid || string.IsNullOrWhiteSpace(prefabName))
			{
				continue;
			}
			character.PrefabNamesForBones[(sbyte)bone] = prefabName;
		}
	}

	private static List<FeastMusicianInstrumentChoice> CreateFeastInstrumentChoices(Settlement settlement)
	{
		List<FeastMusicianInstrumentChoice> visibleChoices = new List<FeastMusicianInstrumentChoice>();
		List<FeastMusicianInstrumentChoice> fallbackChoices = new List<FeastMusicianInstrumentChoice>();
		HashSet<string> visibleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> fallbackKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (SettlementMusicData track in CreateFeastPlayList(settlement))
		{
			if (track?.Instruments == null)
			{
				continue;
			}
			float actionSpeed = Math.Max(0.25f, Math.Min(2.0f, track.Tempo / 120f));
			foreach (InstrumentData instrument in track.Instruments)
			{
				if (instrument == null || string.IsNullOrWhiteSpace(instrument.StandingAction))
				{
					continue;
				}
				if (HasVisibleInstrument(instrument))
				{
					AddUniqueInstrumentChoice(visibleChoices, visibleKeys, instrument, actionSpeed);
				}
				AddUniqueInstrumentChoice(fallbackChoices, fallbackKeys, instrument, actionSpeed);
			}
		}
		return visibleChoices.Count > 0 ? visibleChoices : fallbackChoices;
	}

	private static void AddUniqueInstrumentChoice(List<FeastMusicianInstrumentChoice> choices, HashSet<string> keys, InstrumentData instrument, float actionSpeed)
	{
		string key = string.IsNullOrWhiteSpace(instrument.StringId) ? instrument.GetHashCode().ToString() : instrument.StringId;
		if (keys.Add(key))
		{
			choices.Add(new FeastMusicianInstrumentChoice(instrument, actionSpeed));
		}
	}

	private static bool HasVisibleInstrument(InstrumentData instrument)
	{
		if (instrument?.InstrumentEntities == null)
		{
			return false;
		}
		foreach (var entity in instrument.InstrumentEntities)
		{
			if (entity.Item1 != HumanBone.Invalid && !string.IsNullOrWhiteSpace(entity.Item2))
			{
				return true;
			}
		}
		return false;
	}

	private static FeastMusicianInstrumentChoice SelectFeastInstrumentChoice(List<FeastMusicianInstrumentChoice> choices, int slot)
	{
		if (choices == null || choices.Count == 0)
		{
			return null;
		}
		return choices[Math.Abs(slot) % choices.Count];
	}

	private static List<SettlementMusicData> CreateFeastPlayList(Settlement settlement)
	{
		List<SettlementMusicData> allTracks = MBObjectManager.Instance.GetObjectTypeList<SettlementMusicData>()
			.Where(track => track != null && IsFeastMusicLocation(track.LocationId))
			.ToList();
		if (allTracks.Count == 0)
		{
			return allTracks;
		}
		CultureObject settlementCulture = settlement?.Culture;
		CultureObject factionCulture = settlement?.MapFaction?.Culture;
		List<SettlementMusicData> preferredTracks = allTracks
			.Where(track => track.Culture == settlementCulture || track.Culture == factionCulture)
			.OrderBy(_ => MBRandom.RandomFloat)
			.ToList();
		List<SettlementMusicData> otherTracks = allTracks
			.Where(track => !preferredTracks.Contains(track))
			.OrderBy(_ => MBRandom.RandomFloat)
			.ToList();
		preferredTracks.AddRange(otherTracks);
		return preferredTracks;
	}

	private static bool IsFeastMusicLocation(string locationId)
	{
		return string.Equals(locationId, "tavern", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(locationId, LordHallLocationId, StringComparison.OrdinalIgnoreCase);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		try
		{
			AddPlayerGatheringDialogues(starter);
		}
		catch (Exception ex)
		{
			Log("dialog add failed: " + ex.Message);
		}
		try
		{
			RepairTrackedTemporaryGatheringParties("session_launched");
		}
		catch (Exception ex)
		{
			Log("session temporary party repair failed: " + ex.Message);
		}
	}

	private void AddPlayerGatheringDialogues(CampaignGameStarter starter)
	{
		if (starter == null)
		{
			return;
		}
		starter.AddPlayerLine(
			"af_noble_gathering_governor_start_main",
			"hero_main_options",
			"af_noble_gathering_governor_response",
			"召开宴会",
			IsGovernorGatheringDialogueAvailable,
			OpenGovernorGatheringFlowConsequence);
		starter.AddPlayerLine(
			"af_noble_gathering_governor_start_ask",
			"lord_talk_ask_something_2",
			"af_noble_gathering_governor_response",
			"召开宴会",
			IsGovernorGatheringDialogueAvailable,
			OpenGovernorGatheringFlowConsequence);
		starter.AddPlayerLine(
			"af_noble_gathering_companion_start_main",
			"hero_main_options",
			"af_noble_gathering_companion_response",
			"我想举办一场宴会，你帮我安排。",
			IsCompanionGatheringDialogueAvailable,
			OpenCompanionGatheringFlowConsequence);
		starter.AddPlayerLine(
			"af_noble_gathering_companion_start_ask",
			"lord_talk_ask_something_2",
			"af_noble_gathering_companion_response",
			"我想举办一场宴会，你帮我安排。",
			IsCompanionGatheringDialogueAvailable,
			OpenCompanionGatheringFlowConsequence);
		starter.AddDialogLine(
			"af_noble_gathering_governor_response",
			"af_noble_gathering_governor_response",
			"lord_pretalk",
			"我会为您准备名单与请柬。",
			null,
			null);
		starter.AddDialogLine(
			"af_noble_gathering_companion_response",
			"af_noble_gathering_companion_response",
			"lord_pretalk",
			"明白，我会按您的意思准备举办地和宾客名单。",
			null,
			null);
	}

	private bool IsGovernorGatheringDialogueAvailable()
	{
		if (!NobleGatheringOptions.Capture().Enabled)
		{
			return false;
		}
		Hero governor = ResolveConversationHero();
		return TryResolveGovernorOwnedSettlement(governor, out _, out _);
	}

	private bool IsCompanionGatheringDialogueAvailable()
	{
		if (!NobleGatheringOptions.Capture().Enabled)
		{
			return false;
		}
		Hero companion = ResolveConversationHero();
		return companion != null
			&& companion != Hero.MainHero
			&& companion.IsPlayerCompanion
			&& GetPlayerHostSettlements(null).Any();
	}

	private void OpenGovernorGatheringFlowConsequence()
	{
		_pendingGovernorHero = ResolveConversationHero();
		TryResolveGovernorOwnedSettlement(_pendingGovernorHero, out _pendingSuggestedSettlement, out _);
		_pendingOpenPlayerGatheringFlow = true;
		try
		{
			Campaign.Current?.ConversationManager?.EndConversation();
		}
		catch
		{
		}
	}

	private void OpenCompanionGatheringFlowConsequence()
	{
		_pendingGovernorHero = ResolveConversationHero();
		_pendingSuggestedSettlement = ResolveBestPlayerHostSettlement(null);
		_pendingOpenPlayerGatheringFlow = true;
		try
		{
			Campaign.Current?.ConversationManager?.EndConversation();
		}
		catch
		{
		}
	}

	private void OpenPlayerGatheringFlow(Hero requester, Settlement suggestedSettlement)
	{
		if (!NobleGatheringOptions.Capture().Enabled)
		{
			ShowMessage("宴会功能已在 MCM 中关闭。");
			return;
		}
		if (requester != null && requester.GovernorOf != null && !TryResolveGovernorOwnedSettlement(requester, out suggestedSettlement, out string reject))
		{
			ShowMessage(reject);
			return;
		}
		ShowPlayerGatheringSettlementSelection(suggestedSettlement);
	}

	private void ShowPlayerGatheringSettlementSelection(Settlement suggestedSettlement)
	{
		List<Settlement> settlements = GetPlayerHostSettlements(suggestedSettlement).ToList();
		if (settlements.Count == 0)
		{
			ShowMessage("宴会无法召开：你的家族没有可作为举办地的城镇或城堡。");
			return;
		}
		int enabledCount = 0;
		string firstReject = "";
		List<InquiryElement> options = settlements
			.Select(settlement =>
			{
				bool enabled = CanPlayerHostAtSettlement(Hero.MainHero, settlement, out string reject);
				if (enabled)
				{
					enabledCount++;
				}
				else if (string.IsNullOrWhiteSpace(firstReject))
				{
					firstReject = reject;
				}
				string label = GetSettlementName(settlement);
				string hint = enabled ? "可作为宴会举办地。" : reject;
				return new InquiryElement(settlement.StringId, label, null, enabled, hint);
			})
			.ToList();
		if (enabledCount == 0)
		{
			ShowMessage(string.IsNullOrWhiteSpace(firstReject) ? "宴会无法召开：没有可用举办地。" : firstReject);
			return;
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			"召开宴会：选择举办地",
			"举办地必须是你的家族拥有的城镇或城堡。",
			options,
			isExitShown: true,
			1,
			1,
			"下一步",
			"取消",
			selected =>
			{
				string settlementId = (selected ?? new List<InquiryElement>()).Select(x => x.Identifier as string).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
				Settlement settlement = ResolveSettlementById(settlementId);
				if (!CanPlayerHostAtSettlement(Hero.MainHero, settlement, out string reject))
				{
					ShowMessage(reject);
					ShowPlayerGatheringSettlementSelection(suggestedSettlement);
					return;
				}
				ShowPlayerGatheringClanSelection(settlement);
			},
			null,
			"",
			isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void ShowPlayerGatheringClanSelection(Settlement settlement)
	{
		ShowPlayerGatheringClanSelection(settlement, new List<string>());
	}

	private void ShowPlayerGatheringClanSelection(Settlement settlement, List<string> selectedHeroIds)
	{
		List<Clan> clans = GetPlayerGatheringCandidateClans(settlement).ToList();
		if (clans.Count == 0)
		{
			ShowMessage("没有可邀请的贵族家族。");
			return;
		}
		List<string> currentHeroIds = NormalizeHeroIds(selectedHeroIds);
		List<InquiryElement> options = new List<InquiryElement>();
		if (currentHeroIds.Count > 0)
		{
			options.Add(new InquiryElement("__confirm__", "确认当前名单（" + currentHeroIds.Count + "人）", null, isEnabled: true, BuildSelectedGuestHint(currentHeroIds)));
			options.Add(new InquiryElement("__clear__", "清空已选名单", null, isEnabled: true, "重新选择宴会宾客。"));
		}
		options.AddRange(clans.Select(clan =>
		{
			int selectedInClan = CountSelectedGuestsForClan(currentHeroIds, clan);
			string label = selectedInClan > 0 ? GetClanName(clan) + "（已选 " + selectedInClan + "）" : GetClanName(clan);
			string hint = BuildClanHint(clan);
			if (selectedInClan > 0)
			{
				hint += "\n该家族已选宾客 " + selectedInClan + " 人。";
			}
			return new InquiryElement(clan.StringId, label, null, isEnabled: true, hint);
		}));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			"召开宴会：选择家族",
			"举办地：" + GetSettlementName(settlement) + "\n已选宾客：" + currentHeroIds.Count + " 人。\n请选择一个家族打开成员名单。",
			options,
			isExitShown: true,
			1,
			1,
			"下一步",
			"取消",
			selected =>
			{
				string selectedId = (selected ?? new List<InquiryElement>()).Select(x => x.Identifier as string).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
				if (string.IsNullOrWhiteSpace(selectedId))
				{
					ShowPlayerGatheringClanSelection(settlement, currentHeroIds);
					return;
				}
				if (string.Equals(selectedId, "__confirm__", StringComparison.OrdinalIgnoreCase))
				{
					ShowPlayerGatheringConfirm(settlement, currentHeroIds);
					return;
				}
				if (string.Equals(selectedId, "__clear__", StringComparison.OrdinalIgnoreCase))
				{
					ShowPlayerGatheringClanSelection(settlement, new List<string>());
					return;
				}
				ShowPlayerGatheringHeroSelection(settlement, selectedId, currentHeroIds);
			},
			null,
			"",
			isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void ShowPlayerGatheringHeroSelection(Settlement settlement, string selectedClanId, List<string> selectedHeroIds)
	{
		List<string> currentHeroIds = NormalizeHeroIds(selectedHeroIds);
		HashSet<string> currentHeroIdSet = new HashSet<string>(currentHeroIds, StringComparer.OrdinalIgnoreCase);
		Clan clan = ResolveClanById(selectedClanId);
		List<Hero> heroes = GetPlayerGatheringCandidateHeroes(new List<string> { selectedClanId }).ToList();
		int enabledCount = 0;
		List<InquiryElement> options = heroes
			.Select(hero =>
			{
				bool enabled = IsHeroEligibleForGatheringInvitation(hero, out string reason);
				bool alreadySelected = currentHeroIdSet.Contains(hero?.StringId ?? "");
				if (enabled)
				{
					enabledCount++;
				}
				string identityLabel = BuildGatheringHeroIdentityLabel(hero);
				string label = (alreadySelected ? "[已选] " : "") + "[" + GetClanName(hero?.Clan) + "] " + GetHeroName(hero) + "（" + identityLabel + "）";
				string hint = GetHeroName(hero) + " / " + GetClanName(hero?.Clan)
					+ "\n" + BuildGatheringHeroIdentityHint(hero)
					+ (alreadySelected ? "\n已经在当前宴会名单中。" : enabled ? "\n可发出赴宴邀请；没有独立部队者将按原版旅行机制前往主办地。" : "\n不可邀请：" + reason);
				return new InquiryElement(hero.StringId, label, null, enabled, hint);
			})
			.ToList();
		if (options.Count == 0)
		{
			ShowMessage("所选家族没有可显示的贵族成员。");
			ShowPlayerGatheringClanSelection(settlement, currentHeroIds);
			return;
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			"召开宴会：选择宾客",
			"家族：" + GetClanName(clan) + "\n这是玩家举办的宴会，总督仍可邀请。有独立部队的人会直接被调度；留在城里的贵族、总督、无部队英雄会按原版旅行机制前往主办地，宴会结束后返回原驻留地。\n已选宾客：" + currentHeroIds.Count + " 人。",
			options,
			isExitShown: true,
			0,
			Math.Max(1, enabledCount),
			"加入名单",
			"返回",
			selected =>
			{
				List<string> addedHeroIds = (selected ?? new List<InquiryElement>()).Select(x => x.Identifier as string).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
				List<string> mergedHeroIds = NormalizeHeroIds(currentHeroIds.Concat(addedHeroIds));
				ShowPlayerGatheringClanSelection(settlement, mergedHeroIds);
			},
			_ => ShowPlayerGatheringClanSelection(settlement, currentHeroIds),
			"",
			isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void ShowPlayerGatheringConfirm(Settlement settlement, List<string> heroIds)
	{
		NobleGatheringOptions options = NobleGatheringOptions.Capture();
		List<string> currentHeroIds = NormalizeHeroIds(heroIds);
		List<Hero> heroes = currentHeroIds.Select(ResolveHeroById).Where(x => x != null).ToList();
		if (heroes.Count == 0)
		{
			ShowMessage("宴会未召开：没有可邀请的宾客。");
			ShowPlayerGatheringClanSelection(settlement, new List<string>());
			return;
		}
		string body = "举办地：" + GetSettlementName(settlement)
			+ "\n费用：" + options.Cost + " 第纳尔"
			+ "\n持续：" + options.DurationDays + " 天"
			+ "\n宾客：" + heroes.Count + " 人"
			+ "\n\n确认后将扣款并向宾客下达前往举办地的宴会邀请。";
		InformationManager.ShowInquiry(new InquiryData(
			"确认召开宴会",
			body,
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"支付并发出邀请",
			"返回",
			() =>
			{
				if (TryCreatePlayerHostedGathering(settlement, heroes, out string status))
				{
					ShowMessage(status);
				}
				else
				{
					ShowMessage(status);
					ShowPlayerGatheringClanSelection(settlement, currentHeroIds);
				}
			},
			() => ShowPlayerGatheringClanSelection(settlement, currentHeroIds)),
			pauseGameActiveState: true,
			prioritize: false);
	}

	private bool TryCreatePlayerHostedGathering(Settlement settlement, List<Hero> invitedHeroes, out string status)
	{
		status = "";
		Hero host = Hero.MainHero;
		NobleGatheringOptions options = NobleGatheringOptions.Capture();
		if (!CanPlayerHostAtSettlement(host, settlement, options, out status))
		{
			return false;
		}
		List<Hero> safeInvitees = (invitedHeroes ?? new List<Hero>())
			.Where(hero => hero != null && IsHeroEligibleForGatheringInvitation(hero, out _))
			.GroupBy(hero => hero.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		if (safeInvitees.Count == 0)
		{
			status = "宴会未召开：没有可赴宴的宾客。";
			return false;
		}
		host.ChangeHeroGold(-options.Cost);
		double now = NowDay();
		_playerHostCooldowns[PlayerHostCooldownKey] = now + PlayerHostCooldownDays;
		NobleGatheringRecord record = new NobleGatheringRecord
		{
			Id = GenerateGatheringId(),
			HostHeroId = host.StringId,
			HostClanId = host.Clan?.StringId ?? "",
			KingdomId = (host.Clan?.Kingdom ?? (host.MapFaction as Kingdom))?.StringId ?? "",
			SettlementId = settlement.StringId,
			State = StateActive,
			CreatedDay = now,
			StartDay = now,
			EndDay = now + options.DurationDays,
			IsPlayerHosted = true,
			PlayerInvitationStatus = "",
			InvitedClanRelationReward = options.InvitedClanRelationReward
		};
		foreach (Hero hero in safeInvitees)
		{
			NobleGatheringInviteeRecord invitee = new NobleGatheringInviteeRecord
			{
				HeroId = hero.StringId,
				ClanId = hero.Clan?.StringId ?? "",
				Status = InviteAccepted,
				Reason = "player_invited",
				ArrivalDay = -1.0
			};
			record.Invitees.Add(invitee);
		}
		_gatherings[record.Id] = record;
		RegisterFeastAttendee(host, record);
		ApplyInvitedClanRelationRewards(record, host);
		IssueTravelCommands(record);
		RecordGatheringStartedWeeklyMaterial(record);
		status = "宴会已发出邀请：" + GetSettlementName(settlement) + "，宾客 " + safeInvitees.Count + " 人，持续 " + options.DurationDays + " 天";
		Log("player gathering created id=" + record.Id + " settlement=" + settlement.StringId + " invitees=" + safeInvitees.Count);
		return true;
	}

	private void OnDailyTick()
	{
		try
		{
			TryCreateNpcHostedGathering(force: false, out _);
		}
		catch (Exception ex)
		{
			Log("npc daily failed: " + ex.Message);
		}
	}

	private void OnHourlyTick()
	{
		try
		{
			NobleGatheringOptions options = NobleGatheringOptions.Capture();
			RepairTrackedTemporaryGatheringParties("hourly");
			if (options.Enabled)
			{
				ProcessActiveGatherings(options);
			}
			else
			{
				CancelActiveGatheringsBecauseDisabled();
			}
			ProcessTemporaryPartyReturnsAndOrphans();
			ProcessSettlementTravelReturnsAndOrphans();
		}
		catch (Exception ex)
		{
			Log("hourly failed: " + ex.Message);
		}
	}

	private void ProcessActiveGatherings(NobleGatheringOptions options)
	{
		double now = NowDay();
		foreach (NobleGatheringRecord record in _gatherings.Values.ToList())
		{
			NormalizeRecord(record);
			if (!string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			Settlement settlement = ResolveSettlementById(record.SettlementId);
			Hero host = ResolveHeroById(record.HostHeroId);
			if (settlement == null || host == null || host.IsDead || now >= record.EndDay)
			{
				FinishGathering(record, now >= record.EndDay ? "宴会已经结束。" : "宴会因主办方或举办地失效而结束。");
				continue;
			}
			RegisterFeastAttendee(host, record);
			RecordGatheringStartedWeeklyMaterial(record);
			if (TryCancelNpcGatheringForCrisis(record, host))
			{
				continue;
			}
			TrySendPlayerInvitationCourier(record);
			IssueHostTravelCommand(record);
			IssueTravelCommands(record);
			ProcessActiveTemporaryParties(record, settlement);
			ApplyInvitedClanRelationRewards(record, host);
			UpdateArrivalsAndRewards(record, settlement, host, options.ShowGuestArrivalMessages);
			UpdatePlayerAttendanceReward(record, settlement, host);
		}
	}

	private void CancelActiveGatheringsBecauseDisabled()
	{
		List<NobleGatheringRecord> activeRecords = _gatherings.Values
			.Where(record => record != null && string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase))
			.ToList();
		foreach (NobleGatheringRecord record in activeRecords)
		{
			CompleteGathering(record, StateCancelled, "宴会功能已在 MCM 中关闭。", showMessage: false);
		}
		if (activeRecords.Count > 0)
		{
			DisplayGatheringMessage("宴会功能已关闭，正在进行的宴会已经取消，主人和宾客将陆续返程。", new Color(0.8f, 0.95f, 1f));
			Log("cancelled active gatherings because disabled count=" + activeRecords.Count);
		}
	}

	private void IssueHostTravelCommand(NobleGatheringRecord record)
	{
		if (record == null || record.IsPlayerHosted)
		{
			return;
		}
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		Hero host = ResolveHeroById(record.HostHeroId);
		string reason = "";
		if (settlement == null || host == null || host.IsDead || host.IsPrisoner)
		{
			Log("npc host travel skipped id=" + (record?.Id ?? "") + " reason=host_invalid");
			return;
		}
		RegisterFeastAttendee(host, record);
		WorldMapPartyCommandBehavior world = WorldMapPartyCommandBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		int holdDays = Math.Max(1, (int)Math.Ceiling(record.EndDay - NowDay()));
		if (CanUseExistingPartyForGatheringTravel(host, out reason))
		{
			MobileParty party = host.PartyBelongedTo;
			bool atSettlement = IsHeroAtSettlement(host, settlement);
			bool alreadyTargetingSettlement = IsPartyTargetingGatheringSettlement(party, settlement);
			if (record.HostCommandIssued && (atSettlement || alreadyTargetingSettlement))
			{
				return;
			}
			string worldMessage = "大地图命令系统未初始化。";
			if (world != null && world.TryIssueGoToSettlementUntilDayForExternal(host, settlement, holdDays, record.EndDay, BuildCommandSourceId(record), out worldMessage))
			{
				record.HostCommandIssued = true;
				Log("npc host travel issued id=" + record.Id + " host=" + host.StringId + " settlement=" + settlement.StringId);
			}
			else
			{
				Log("npc host travel failed id=" + record.Id + " host=" + (host.StringId ?? "") + " message=" + (worldMessage ?? reason));
			}
			return;
		}
		if (host.PartyBelongedTo == null && IsHeroAtSettlement(host, settlement))
		{
			record.HostCommandIssued = true;
			if (string.IsNullOrWhiteSpace(record.HostOriginSettlementId))
			{
				record.HostOriginSettlementId = (ResolveHeroOriginSettlement(host) ?? settlement)?.StringId ?? "";
			}
			return;
		}
		if (host.PartyBelongedTo == null
			&& record.HostCommandIssued
			&& !host.IsPrisoner
			&& !IsHeroAtSettlement(host, settlement))
		{
			if (IsHeroTeleportingToSettlement(host, settlement))
			{
				return;
			}
			SafelyPlaceHeroBackToSettlement(host, settlement, "maintain_active_host");
			if (IsHeroAtSettlement(host, settlement))
			{
				return;
			}
		}
		if (TryIssueDelayedSettlementTravel(record, null, host, settlement, out string message))
		{
			record.HostCommandIssued = true;
			Log("npc host settlement travel issued id=" + record.Id + " host=" + host.StringId + " settlement=" + settlement.StringId + " message=" + message);
		}
		else
		{
			Log("npc host travel failed id=" + record.Id + " host=" + (host?.StringId ?? "") + " message=" + (message ?? reason));
		}
	}

	private void IssueTravelCommands(NobleGatheringRecord record)
	{
		Settlement settlement = ResolveSettlementById(record?.SettlementId);
		if (record == null || settlement == null)
		{
			return;
		}
		WorldMapPartyCommandBehavior world = WorldMapPartyCommandBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		int holdDays = Math.Max(1, (int)Math.Ceiling(record.EndDay - NowDay()));
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			if (invitee == null || invitee.CommandIssued || !IsInviteAcceptedStatus(invitee.Status))
			{
				continue;
			}
			Hero hero = ResolveHeroById(invitee.HeroId);
			if (!IsHeroEligibleForGatheringInvitation(hero, out string reason))
			{
				invitee.Status = InviteFailed;
				invitee.Reason = reason;
				continue;
			}
			if (CanUseExistingPartyForGatheringTravel(hero, out reason) && world != null && world.TryIssueGoToSettlementUntilDayForExternal(hero, settlement, holdDays, record.EndDay, BuildCommandSourceId(record), out string message))
			{
				invitee.CommandIssued = true;
				invitee.Reason = "command_issued";
			}
			else if (TryIssueDelayedSettlementTravel(record, invitee, hero, settlement, out message))
			{
				invitee.CommandIssued = true;
				invitee.Reason = "settlement_travel_issued";
			}
			else
			{
				invitee.Status = InviteFailed;
				invitee.Reason = string.IsNullOrWhiteSpace(message) ? reason : message;
			}
		}
	}

	private void UpdateArrivalsAndRewards(NobleGatheringRecord record, Settlement settlement, Hero host, bool showGuestArrivalMessages)
	{
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			if (invitee == null || !IsInviteAcceptedStatus(invitee.Status))
			{
				continue;
			}
			Hero hero = ResolveHeroById(invitee.HeroId);
			if (hero == null || hero.IsDead || hero.IsPrisoner)
			{
				invitee.Status = InviteFailed;
				invitee.Reason = "hero_invalid";
				continue;
			}
			if (!IsHeroAtSettlement(hero, settlement))
			{
				continue;
			}
			invitee.Status = InviteArrived;
			invitee.ArrivalDay = NowDay();
			RegisterFeastAttendee(hero, record);
			RecordNotableGatheringAttendanceWeeklyMaterial(record, hero);
			if (record.IsPlayerHosted && showGuestArrivalMessages)
			{
				DisplayGatheringMessage(GetHeroName(hero) + "已抵达" + GetSettlementName(settlement) + "参加宴会。", new Color(0.4f, 1f, 0.4f));
			}
		}
	}

	private int ApplyInvitedClanRelationRewards(NobleGatheringRecord record, Hero host)
	{
		if (record == null)
		{
			return 0;
		}
		int appliedCount = 0;
		record.RelationRewardedClanIds ??= new List<string>();
		HashSet<string> rewardedClanIds = new HashSet<string>(
			record.RelationRewardedClanIds
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id.Trim()),
			StringComparer.OrdinalIgnoreCase);
		foreach (string clanId in (record.Invitees ?? new List<NobleGatheringInviteeRecord>())
			.Select(invitee => (invitee?.ClanId ?? "").Trim())
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			if (rewardedClanIds.Contains(clanId))
			{
				MarkInvitedClanRelationRewardApplied(record, clanId);
				continue;
			}
			if (ApplyInvitedClanRelationReward(record, host, clanId, out bool relationChanged))
			{
				rewardedClanIds.Add(clanId);
				record.RelationRewardedClanIds.Add(clanId);
				MarkInvitedClanRelationRewardApplied(record, clanId);
				if (relationChanged)
				{
					appliedCount++;
				}
			}
		}
		return appliedCount;
	}

	private bool ApplyInvitedClanRelationReward(NobleGatheringRecord record, Hero host, string clanId, out bool relationChanged)
	{
		relationChanged = false;
		Clan clan = ResolveClanById(clanId);
		Hero leader = clan?.Leader;
		if (record == null || leader == null || leader.IsDead || leader == Hero.MainHero || leader == host)
		{
			return true;
		}
		if (!record.IsPlayerHosted && (host == null || host == Hero.MainHero))
		{
			return false;
		}
		int relationReward = GetRecordInvitedClanRelationReward(record);
		if (relationReward <= 0)
		{
			return true;
		}
		try
		{
			if (record.IsPlayerHosted)
			{
				ChangeRelationAction.ApplyPlayerRelation(leader, relationReward, affectRelatives: false, showQuickNotification: true);
				relationChanged = true;
			}
			else if (host != null && host != Hero.MainHero)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(host, leader, relationReward, showQuickNotification: false);
				relationChanged = true;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("invited clan relation reward failed clan=" + (clanId ?? "") + " leader=" + (leader?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static void MarkInvitedClanRelationRewardApplied(NobleGatheringRecord record, string clanId)
	{
		string id = (clanId ?? "").Trim();
		if (record == null || string.IsNullOrWhiteSpace(id))
		{
			return;
		}
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			if (invitee != null && string.Equals((invitee.ClanId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase))
			{
				invitee.RelationRewardApplied = true;
			}
		}
	}

	private void UpdatePlayerAttendanceReward(NobleGatheringRecord record, Settlement settlement, Hero host)
	{
		if (record == null
			|| record.IsPlayerHosted
			|| record.PlayerAttendanceRewardApplied
			|| !string.Equals(record.PlayerInvitationStatus, PlayerInvitationInvited, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (!IsPlayerAtSettlement(settlement))
		{
			return;
		}
		record.PlayerAttendanceRewardApplied = true;
		record.PlayerInvitationStatus = PlayerInvitationArrived;
		record.PlayerArrivalDay = NowDay();
		RegisterFeastAttendee(Hero.MainHero, record);
		RecordNotableGatheringAttendanceWeeklyMaterial(record, Hero.MainHero);
		if (host != null && host != Hero.MainHero)
		{
			try
			{
				ChangeRelationAction.ApplyPlayerRelation(host, 10, affectRelatives: false, showQuickNotification: true);
			}
			catch (Exception ex)
			{
				Log("player attendance relation failed: " + ex.Message);
			}
		}
		ShowMessage("你参加了" + GetHeroName(host) + "的宴会，与主办方的好感提升了。");
	}

	private void FinishGathering(NobleGatheringRecord record, string reason)
	{
		CompleteGathering(record, StateFinished, reason);
	}

	private void CancelGathering(NobleGatheringRecord record, string reason)
	{
		CompleteGathering(record, StateCancelled, reason);
	}

	private void CompleteGathering(NobleGatheringRecord record, string finalState, string reason, bool showMessage = true)
	{
		if (record == null || !string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		record.State = string.Equals(finalState, StateCancelled, StringComparison.OrdinalIgnoreCase) ? StateCancelled : StateFinished;
		record.EndReason = (reason ?? "").Trim();
		UnregisterFeastAttendees(record);
		WorldMapPartyCommandBehavior world = WorldMapPartyCommandBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		Hero host = ResolveHeroById(record.HostHeroId);
		if (!record.IsPlayerHosted && world != null)
		{
			if (host != null)
			{
				world.TryStopExternalCommandForExternal(host, BuildCommandSourceId(record), out _);
			}
		}
		string completionReason = string.Equals(record.State, StateCancelled, StringComparison.OrdinalIgnoreCase) ? "cancel" : "finish";
		if (!record.IsPlayerHosted)
		{
			if (!string.IsNullOrWhiteSpace(record.HostTemporaryPartyId))
			{
				StartTemporaryHostReturn(record, completionReason);
			}
			else
			{
				QueueSettlementHostReturn(record, completionReason);
			}
		}
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			Hero hero = ResolveHeroById(invitee?.HeroId);
			if (hero != null && world != null && string.IsNullOrWhiteSpace(invitee?.TemporaryPartyId))
			{
				world.TryStopExternalCommandForExternal(hero, BuildCommandSourceId(record), out _);
			}
			if (!string.IsNullOrWhiteSpace(invitee?.TemporaryPartyId))
			{
				StartTemporaryInviteeReturn(invitee, completionReason);
			}
			else
			{
				QueueSettlementInviteeReturn(invitee, completionReason);
			}
		}
		if (string.Equals(record.State, StateCancelled, StringComparison.OrdinalIgnoreCase))
		{
			RecordGatheringCancelledWeeklyMaterial(record, reason);
		}
		if (showMessage)
		{
			DisplayGatheringMessage(BuildGatheringEndMessage(record, reason), new Color(0.8f, 0.95f, 1f));
		}
		Log("complete gathering id=" + record.Id + " state=" + record.State + " reason=" + reason);
	}

	private bool TryCancelNpcGatheringForCrisis(NobleGatheringRecord record, Hero host)
	{
		if (record == null || record.IsPlayerHosted || host == null)
		{
			return false;
		}
		Kingdom kingdom = ResolveKingdomToken(record.KingdomId) ?? host.Clan?.Kingdom;
		int crisisLevel = ResolveGatheringCrisisLevel(kingdom, out Settlement besiegedSettlement);
		if (crisisLevel <= record.CrisisDecisionLevel)
		{
			return false;
		}
		record.CrisisDecisionLevel = crisisLevel;
		float cancellationChance = CalculateCrisisCancellationChance(host, kingdom, besiegedSettlement, crisisLevel);
		float roll = MBRandom.RandomFloat;
		Log("npc crisis decision id=" + record.Id
			+ " host=" + (host.StringId ?? "")
			+ " level=" + crisisLevel
			+ " chance=" + cancellationChance.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
			+ " roll=" + roll.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
		if (roll >= cancellationChance)
		{
			return false;
		}
		string reason = crisisLevel >= CrisisDecisionSiege
			? "王国内的" + GetSettlementName(besiegedSettlement) + "正遭围攻，主办人决定取消宴会。"
			: "王国已经卷入战争，主办人决定取消宴会。";
		CancelGathering(record, reason);
		return true;
	}

	private static int ResolveGatheringCrisisLevel(Kingdom kingdom, out Settlement besiegedSettlement)
	{
		besiegedSettlement = null;
		if (kingdom == null || kingdom.IsEliminated)
		{
			return CrisisDecisionNone;
		}
		try
		{
			besiegedSettlement = Settlement.All?
				.Where(candidate => candidate?.Town != null
					&& candidate.IsUnderSiege
					&& (candidate.OwnerClan?.Kingdom == kingdom || candidate.MapFaction == kingdom))
				.OrderByDescending(candidate => candidate.OwnerClan == kingdom.RulingClan)
				.FirstOrDefault();
			if (besiegedSettlement != null)
			{
				return CrisisDecisionSiege;
			}
			return IsKingdomMostlyPeaceful(kingdom) ? CrisisDecisionNone : CrisisDecisionWar;
		}
		catch
		{
			return CrisisDecisionNone;
		}
	}

	private static float CalculateCrisisCancellationChance(Hero host, Kingdom kingdom, Settlement besiegedSettlement, int crisisLevel)
	{
		float chance = crisisLevel >= CrisisDecisionSiege ? 0.85f : 0.55f;
		try
		{
			chance += host.GetTraitLevel(DefaultTraits.Calculating) * 0.08f;
			chance -= host.GetTraitLevel(DefaultTraits.Valor) * 0.08f;
			if (kingdom?.Leader == host)
			{
				chance += 0.08f;
			}
			if (besiegedSettlement?.OwnerClan == host.Clan)
			{
				chance += 0.08f;
			}
		}
		catch
		{
		}
		return MBMath.ClampFloat(chance, 0.15f, 0.95f);
	}

	private bool TryCreateNpcHostedGathering(bool force, out string status)
	{
		status = "";
		NobleGatheringOptions options = NobleGatheringOptions.Capture();
		if (!options.Enabled)
		{
			status = "noble_gathering_disabled";
			return false;
		}
		if (!force && !options.EnableNpcAutomaticGatherings)
		{
			status = "npc_automatic_gathering_disabled";
			return false;
		}
		double now = NowDay();
		IEnumerable<Kingdom> kingdomSource = Kingdom.All == null ? Enumerable.Empty<Kingdom>() : Kingdom.All;
		List<Kingdom> kingdoms = kingdomSource
			.Where(kingdom => kingdom != null && !kingdom.IsEliminated)
			.OrderBy(_ => MBRandom.RandomFloat)
			.ToList();
		bool createdAny = false;
		List<string> results = new List<string>();
		foreach (Kingdom kingdom in kingdoms)
		{
			string kingdomId = (kingdom.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(kingdomId) || HasActiveGatheringForKingdom(kingdom))
			{
				continue;
			}
			if (!force)
			{
				if (!_npcKingdomNextHostDays.TryGetValue(kingdomId, out double nextEligibleDay))
				{
					_npcKingdomNextHostDays[kingdomId] = now + options.NpcIntervalDays;
					continue;
				}
				if (now < nextEligibleDay)
				{
					continue;
				}
			}
			if (!TryCreateNpcHostedGatheringForKingdom(kingdom, options, out string kingdomStatus))
			{
				if (!string.IsNullOrWhiteSpace(kingdomStatus))
				{
					results.Add(kingdomId + ":" + kingdomStatus);
				}
				continue;
			}
			createdAny = true;
			_npcKingdomNextHostDays[kingdomId] = now + options.NpcIntervalDays;
			results.Add(kingdomStatus);
			if (force)
			{
				break;
			}
		}
		status = results.Count > 0 ? string.Join(" | ", results) : "no_eligible_kingdom";
		return createdAny;
	}

	private bool TryCreateNpcHostedGatheringForKingdom(Kingdom kingdom, NobleGatheringOptions options, out string status)
	{
		status = "";
		if (kingdom == null || kingdom.IsEliminated || !IsKingdomMostlyPeaceful(kingdom))
		{
			status = "kingdom_not_peaceful";
			return false;
		}
		List<Hero> possibleHosts = Hero.AllAliveHeroes
			.Where(hero => hero != null && hero != Hero.MainHero && hero.IsClanLeader && hero.Clan != null && !hero.IsPrisoner && hero.Gold >= options.Cost)
			.Where(hero => hero.Clan.Kingdom == kingdom && !hero.Clan.IsEliminated && !hero.Clan.IsMinorFaction && !hero.Clan.IsBanditFaction)
			.Where(hero => IsHeroEligibleForGatheringInvitation(hero, out _))
			.Where(hero => !HasActiveGatheringForHost(hero))
			.OrderBy(_ => MBRandom.RandomFloat)
			.Take(50)
			.ToList();
		foreach (Hero host in possibleHosts)
		{
			if (!TryPickNpcHostSettlement(host, out Settlement settlement))
			{
				continue;
			}
			List<Hero> invitees = PickNpcInvitees(host, settlement, options);
			if (invitees.Count < 2)
			{
				continue;
			}
			double now = NowDay();
			NobleGatheringRecord record = new NobleGatheringRecord
			{
				Id = GenerateGatheringId(),
				HostHeroId = host.StringId,
				HostClanId = host.Clan?.StringId ?? "",
				KingdomId = host.Clan?.Kingdom?.StringId ?? "",
				SettlementId = settlement.StringId,
				State = StateActive,
				CreatedDay = now,
				StartDay = now,
				EndDay = now + options.DurationDays,
				IsPlayerHosted = false,
				PlayerInvitationStatus = invitees.Contains(Hero.MainHero) ? PlayerInvitationInvited : "",
				InvitedClanRelationReward = options.InvitedClanRelationReward
			};
			foreach (Hero hero in invitees)
			{
				if (hero == Hero.MainHero)
				{
					continue;
				}
				bool accepted = ShouldNpcAcceptInvitation(host, hero);
				record.Invitees.Add(new NobleGatheringInviteeRecord
				{
					HeroId = hero.StringId,
					ClanId = hero.Clan?.StringId ?? "",
					Status = accepted ? InviteAccepted : InviteDeclined,
					Reason = accepted ? "npc_accept" : "npc_decline",
					ArrivalDay = -1.0
				});
			}
			IssueHostTravelCommand(record);
			if (!record.HostCommandIssued && !IsHeroAtSettlement(host, settlement))
			{
				Log("npc gathering skipped because host travel could not be issued id=" + record.Id + " host=" + host.StringId);
				continue;
			}
			host.ChangeHeroGold(-options.Cost);
			_gatherings[record.Id] = record;
			RegisterFeastAttendee(host, record);
			ApplyInvitedClanRelationRewards(record, host);
			IssueTravelCommands(record);
			TrySendPlayerInvitationCourier(record);
			RecordGatheringStartedWeeklyMaterial(record);
			DisplayGatheringMessage(GetHeroName(host) + "将在" + GetSettlementName(settlement) + "举办宴会。", new Color(0.8f, 0.95f, 1f));
			Log("npc gathering created id=" + record.Id + " host=" + host.StringId + " settlement=" + settlement.StringId);
			status = "created:" + record.Id;
			return true;
		}
		status = "no_valid_npc_host";
		return false;
	}

	private void TrySendPlayerInvitationCourier(NobleGatheringRecord record)
	{
		if (record == null
			|| record.IsPlayerHosted
			|| record.PlayerInvitationCourierSent
			|| !string.Equals(record.PlayerInvitationStatus, PlayerInvitationInvited, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		double now = NowDay();
		if (record.PlayerInvitationCourierNextRetryDay >= 0.0 && now < record.PlayerInvitationCourierNextRetryDay)
		{
			return;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		if (host == null || settlement == null)
		{
			record.PlayerInvitationCourierNextRetryDay = now + PlayerInvitationCourierRetryIntervalDays;
			return;
		}
		string endDate = CampaignTime.Days((float)record.EndDay).ToString();
		string letter = GetHeroName(host) + "致" + (Hero.MainHero?.Name?.ToString() ?? "你") + "：\n\n我将在" + GetSettlementName(settlement) + "举办一场宴会，诚邀你前来赴宴。宴会预计持续至 " + endDate + "；若你愿意，到达举办地即可。";
		if (CourierDeliveryBehavior.TrySendNpcLetterToPlayerForExternal(host, letter, "noble_gathering:" + record.Id, out string status))
		{
			record.PlayerInvitationCourierSent = true;
			record.PlayerInvitationCourierNextRetryDay = -1.0;
			Log("player invitation courier sent id=" + record.Id + " status=" + status);
		}
		else
		{
			record.PlayerInvitationCourierNextRetryDay = now + PlayerInvitationCourierRetryIntervalDays;
			Log("player invitation courier failed id=" + record.Id + " status=" + status);
		}
	}

	private void ProcessActiveTemporaryParties(NobleGatheringRecord record, Settlement settlement)
	{
		if (record == null || settlement == null)
		{
			return;
		}
		ProcessTemporaryHostToGathering(record, settlement);
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			ProcessTemporaryInviteeToGathering(invitee, settlement);
		}
	}

	private void ProcessTemporaryPartyReturnsAndOrphans()
	{
		foreach (NobleGatheringRecord record in _gatherings.Values.ToList())
		{
			if (record == null)
			{
				continue;
			}
			if (!string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase))
			{
				StartTemporaryHostReturn(record, "record_not_active");
				foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
				{
					StartTemporaryInviteeReturn(invitee, "record_not_active");
				}
			}
			ProcessTemporaryHostReturn(record);
			foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
			{
				ProcessTemporaryInviteeReturn(invitee);
			}
		}
		CleanupUntrackedTemporaryParties();
	}

	private void ProcessTemporaryHostToGathering(NobleGatheringRecord record, Settlement settlement)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.HostTemporaryPartyId))
		{
			return;
		}
		if (!string.Equals(record.HostTemporaryPartyPhase, TemporaryPartyPhaseToGathering, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		MobileParty party = ResolveMobilePartyById(record.HostTemporaryPartyId);
		if (host == null || host.IsDead || host.IsPrisoner || party == null || !party.IsActive)
		{
			StartTemporaryHostReturn(record, "host_invalid_or_party_missing");
			return;
		}
		EnsureTemporaryGatheringPartySupplies(party);
		if (EnsureTemporaryPartyAtSettlement(party, settlement, "host_arrived"))
		{
			record.HostTemporaryPartyPhase = TemporaryPartyPhaseAtGathering;
			record.HostCommandIssued = true;
		}
		else
		{
			RefreshTemporaryPartyRoute(party, settlement);
		}
	}

	private void ProcessTemporaryInviteeToGathering(NobleGatheringInviteeRecord invitee, Settlement settlement)
	{
		if (invitee == null || string.IsNullOrWhiteSpace(invitee.TemporaryPartyId))
		{
			return;
		}
		if (!string.Equals(invitee.TemporaryPartyPhase, TemporaryPartyPhaseToGathering, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Hero hero = ResolveHeroById(invitee.HeroId);
		MobileParty party = ResolveMobilePartyById(invitee.TemporaryPartyId);
		if (hero == null || hero.IsDead || hero.IsPrisoner || party == null || !party.IsActive)
		{
			invitee.Status = InviteFailed;
			invitee.Reason = "temporary_party_missing_or_hero_invalid";
			StartTemporaryInviteeReturn(invitee, "invalid_to_gathering");
			return;
		}
		EnsureTemporaryGatheringPartySupplies(party);
		if (EnsureTemporaryPartyAtSettlement(party, settlement, "invitee_arrived"))
		{
			invitee.TemporaryPartyPhase = TemporaryPartyPhaseAtGathering;
		}
		else
		{
			RefreshTemporaryPartyRoute(party, settlement);
		}
	}

	private void ProcessTemporaryHostReturn(NobleGatheringRecord record)
	{
		if (record == null || !string.Equals(record.HostTemporaryPartyPhase, TemporaryPartyPhaseReturning, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Settlement origin = ResolveSettlementById(record.HostOriginSettlementId);
		MobileParty party = ResolveMobilePartyById(record.HostTemporaryPartyId);
		Hero host = ResolveHeroById(record.HostHeroId);
		if (party == null || !party.IsActive)
		{
			SafelyPlaceHeroBackToSettlement(host, origin, "host_party_missing");
			record.HostTemporaryPartyPhase = TemporaryPartyPhaseCleaned;
			record.HostTemporaryPartyId = "";
			return;
		}
		if (origin == null)
		{
			origin = ResolveBestHeroHomeSettlement(host);
		}
		if (origin == null)
		{
			DestroyTemporaryPartyAfterRemovingHero(party, host, null, "host_return_no_origin");
			record.HostTemporaryPartyPhase = TemporaryPartyPhaseCleaned;
			record.HostTemporaryPartyId = "";
			return;
		}
		EnsureTemporaryGatheringPartySupplies(party);
		if (EnsureTemporaryPartyAtSettlement(party, origin, "host_return_arrived"))
		{
			DestroyTemporaryPartyAfterRemovingHero(party, host, origin, "host_return_complete");
			record.HostTemporaryPartyPhase = TemporaryPartyPhaseCleaned;
			record.HostTemporaryPartyId = "";
		}
		else
		{
			RefreshTemporaryPartyRoute(party, origin);
		}
	}

	private void ProcessTemporaryInviteeReturn(NobleGatheringInviteeRecord invitee)
	{
		if (invitee == null || !string.Equals(invitee.TemporaryPartyPhase, TemporaryPartyPhaseReturning, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Settlement origin = ResolveSettlementById(invitee.OriginSettlementId);
		MobileParty party = ResolveMobilePartyById(invitee.TemporaryPartyId);
		Hero hero = ResolveHeroById(invitee.HeroId);
		if (party == null || !party.IsActive)
		{
			SafelyPlaceHeroBackToSettlement(hero, origin, "invitee_party_missing");
			invitee.TemporaryPartyPhase = TemporaryPartyPhaseCleaned;
			invitee.TemporaryPartyId = "";
			return;
		}
		if (origin == null)
		{
			origin = ResolveBestHeroHomeSettlement(hero);
		}
		if (origin == null)
		{
			DestroyTemporaryPartyAfterRemovingHero(party, hero, null, "invitee_return_no_origin");
			invitee.TemporaryPartyPhase = TemporaryPartyPhaseCleaned;
			invitee.TemporaryPartyId = "";
			return;
		}
		EnsureTemporaryGatheringPartySupplies(party);
		if (EnsureTemporaryPartyAtSettlement(party, origin, "invitee_return_arrived"))
		{
			DestroyTemporaryPartyAfterRemovingHero(party, hero, origin, "invitee_return_complete");
			invitee.TemporaryPartyPhase = TemporaryPartyPhaseCleaned;
			invitee.TemporaryPartyId = "";
		}
		else
		{
			RefreshTemporaryPartyRoute(party, origin);
		}
	}

	private void StartTemporaryHostReturn(NobleGatheringRecord record, string reason)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.HostTemporaryPartyId))
		{
			return;
		}
		if (string.Equals(record.HostTemporaryPartyPhase, TemporaryPartyPhaseReturning, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(record.HostTemporaryPartyPhase, TemporaryPartyPhaseCleaned, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		record.HostTemporaryPartyPhase = TemporaryPartyPhaseReturning;
		Settlement origin = ResolveSettlementById(record.HostOriginSettlementId) ?? ResolveBestHeroHomeSettlement(ResolveHeroById(record.HostHeroId));
		MobileParty party = ResolveMobilePartyById(record.HostTemporaryPartyId);
		if (party != null && party.IsActive && origin != null)
		{
			EnsureTemporaryGatheringPartySupplies(party);
			LeaveSettlementIfNeeded(party);
			RefreshTemporaryPartyRoute(party, origin);
		}
		Log("temporary host return id=" + (record.Id ?? "") + " reason=" + (reason ?? ""));
	}

	private void StartTemporaryInviteeReturn(NobleGatheringInviteeRecord invitee, string reason)
	{
		if (invitee == null || string.IsNullOrWhiteSpace(invitee.TemporaryPartyId))
		{
			return;
		}
		if (string.Equals(invitee.TemporaryPartyPhase, TemporaryPartyPhaseReturning, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(invitee.TemporaryPartyPhase, TemporaryPartyPhaseCleaned, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		invitee.TemporaryPartyPhase = TemporaryPartyPhaseReturning;
		Settlement origin = ResolveSettlementById(invitee.OriginSettlementId) ?? ResolveBestHeroHomeSettlement(ResolveHeroById(invitee.HeroId));
		MobileParty party = ResolveMobilePartyById(invitee.TemporaryPartyId);
		if (party != null && party.IsActive && origin != null)
		{
			EnsureTemporaryGatheringPartySupplies(party);
			LeaveSettlementIfNeeded(party);
			RefreshTemporaryPartyRoute(party, origin);
		}
		Log("temporary invitee return hero=" + (invitee.HeroId ?? "") + " reason=" + (reason ?? ""));
	}

	private bool TryIssueDelayedSettlementTravel(NobleGatheringRecord record, NobleGatheringInviteeRecord invitee, Hero hero, Settlement targetSettlement, out string message)
	{
		message = "";
		if (record == null || hero == null || targetSettlement == null)
		{
			message = "settlement_travel_invalid";
			return false;
		}
		bool alreadyAtTarget = IsHeroAtSettlement(hero, targetSettlement);
		bool alreadyTravelingToTarget = IsHeroTeleportingToSettlement(hero, targetSettlement);
		if (!alreadyAtTarget && !alreadyTravelingToTarget && !CanUseDelayedSettlementTravelForGathering(hero, out string reason))
		{
			message = reason;
			return false;
		}
		Settlement origin = ResolveSettlementById(invitee == null ? record.HostOriginSettlementId : invitee.OriginSettlementId) ?? ResolveHeroOriginSettlement(hero);
		if (origin == null)
		{
			message = invitee == null ? "找不到主办人的原始所在城。" : "找不到宾客的原始所在城。";
			return false;
		}
		if (invitee == null)
		{
			record.HostOriginSettlementId = origin.StringId ?? "";
			record.HostSettlementReturnState = "";
		}
		else
		{
			invitee.OriginSettlementId = origin.StringId ?? "";
			invitee.SettlementReturnState = "";
		}
		if (alreadyAtTarget)
		{
			message = "settlement_travel_already_at_target";
			return true;
		}
		if (alreadyTravelingToTarget)
		{
			message = "settlement_travel_already_pending";
			return true;
		}
		if (TryGetHeroTeleportTarget(hero, out Settlement currentTarget))
		{
			message = "正在旅行至" + GetSettlementName(currentTarget);
			return false;
		}
		if (hero.IsTraveling)
		{
			message = "正在旅行";
			return false;
		}
		if (hero.PartyBelongedTo != null)
		{
			message = "已有部队";
			return false;
		}
		if (!CanHeroMoveToSettlement(hero, out string moveReason))
		{
			message = moveReason;
			return false;
		}
		try
		{
			TeleportHeroAction.ApplyDelayedTeleportToSettlement(hero, targetSettlement);
			message = "settlement_travel_issued";
			Log("settlement travel issued hero=" + (hero.StringId ?? "") + " origin=" + (origin.StringId ?? "") + " target=" + (targetSettlement.StringId ?? "") + " gathering=" + (record.Id ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			message = "原版旅行下达失败：" + ex.Message;
			Log("settlement travel failed hero=" + (hero?.StringId ?? "") + " target=" + (targetSettlement?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private void QueueSettlementHostReturn(NobleGatheringRecord record, string reason)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.HostOriginSettlementId))
		{
			return;
		}
		if (string.Equals(record.HostSettlementReturnState, SettlementReturnIssued, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(record.HostSettlementReturnState, SettlementReturnSkipped, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		record.HostSettlementReturnState = SettlementReturnPending;
		Log("settlement host return queued id=" + (record.Id ?? "") + " host=" + (record.HostHeroId ?? "") + " origin=" + (record.HostOriginSettlementId ?? "") + " reason=" + (reason ?? ""));
		ProcessSettlementHostReturn(record, reason);
	}

	private void QueueSettlementInviteeReturn(NobleGatheringInviteeRecord invitee, string reason)
	{
		if (invitee == null || string.IsNullOrWhiteSpace(invitee.OriginSettlementId))
		{
			return;
		}
		if (string.Equals(invitee.SettlementReturnState, SettlementReturnIssued, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(invitee.SettlementReturnState, SettlementReturnSkipped, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		invitee.SettlementReturnState = SettlementReturnPending;
		Log("settlement invitee return queued hero=" + (invitee.HeroId ?? "") + " origin=" + (invitee.OriginSettlementId ?? "") + " reason=" + (reason ?? ""));
		ProcessSettlementInviteeReturn(invitee, reason);
	}

	private void ProcessSettlementTravelReturnsAndOrphans()
	{
		foreach (NobleGatheringRecord record in _gatherings.Values.ToList())
		{
			if (record == null)
			{
				continue;
			}
			ProcessSettlementHostReturn(record, "hourly");
			foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
			{
				ProcessSettlementInviteeReturn(invitee, "hourly");
			}
		}
	}

	private void ProcessSettlementHostReturn(NobleGatheringRecord record, string reason)
	{
		if (record == null || !string.Equals(record.HostSettlementReturnState, SettlementReturnPending, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Settlement origin = ResolveSettlementById(record.HostOriginSettlementId);
		Hero host = ResolveHeroById(record.HostHeroId);
		string state = TryIssueSettlementReturnTravel(host, origin, out string message);
		if (!string.Equals(state, SettlementReturnPending, StringComparison.OrdinalIgnoreCase))
		{
			record.HostSettlementReturnState = state;
			Log("settlement host return " + state + " id=" + (record.Id ?? "") + " host=" + (record.HostHeroId ?? "") + " origin=" + (record.HostOriginSettlementId ?? "") + " reason=" + (reason ?? "") + " message=" + (message ?? ""));
		}
	}

	private void ProcessSettlementInviteeReturn(NobleGatheringInviteeRecord invitee, string reason)
	{
		if (invitee == null || !string.Equals(invitee.SettlementReturnState, SettlementReturnPending, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Settlement origin = ResolveSettlementById(invitee.OriginSettlementId);
		Hero hero = ResolveHeroById(invitee.HeroId);
		string state = TryIssueSettlementReturnTravel(hero, origin, out string message);
		if (!string.Equals(state, SettlementReturnPending, StringComparison.OrdinalIgnoreCase))
		{
			invitee.SettlementReturnState = state;
			Log("settlement invitee return " + state + " hero=" + (invitee.HeroId ?? "") + " origin=" + (invitee.OriginSettlementId ?? "") + " reason=" + (reason ?? "") + " message=" + (message ?? ""));
		}
	}

	private static string TryIssueSettlementReturnTravel(Hero hero, Settlement origin, out string message)
	{
		message = "";
		if (origin == null)
		{
			message = "return_no_origin";
			return SettlementReturnSkipped;
		}
		if (hero == null || hero.IsDead)
		{
			message = "return_hero_invalid";
			return SettlementReturnSkipped;
		}
		if (hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
		{
			message = "return_hero_prisoner";
			return SettlementReturnSkipped;
		}
		if (hero.PartyBelongedTo != null)
		{
			message = "return_skipped_has_party";
			return SettlementReturnSkipped;
		}
		if (IsHeroAtSettlement(hero, origin))
		{
			message = "return_already_home";
			return SettlementReturnIssued;
		}
		if (TryGetHeroTeleportTarget(hero, out Settlement currentTarget))
		{
			if (currentTarget == origin)
			{
				message = "return_already_pending";
				return SettlementReturnIssued;
			}
			message = "return_waiting_current_travel:" + (currentTarget?.StringId ?? "");
			return SettlementReturnPending;
		}
		if (hero.IsTraveling)
		{
			message = "return_waiting_current_travel";
			return SettlementReturnPending;
		}
		if (!CanHeroMoveToSettlement(hero, out string moveReason))
		{
			message = moveReason;
			return SettlementReturnPending;
		}
		try
		{
			TeleportHeroAction.ApplyDelayedTeleportToSettlement(hero, origin);
			message = "return_issued";
			return SettlementReturnIssued;
		}
		catch (Exception ex)
		{
			message = "return_exception:" + ex.Message;
			Log("settlement return failed hero=" + (hero?.StringId ?? "") + " origin=" + (origin?.StringId ?? "") + " error=" + ex);
			return SettlementReturnPending;
		}
	}

	private static bool TryGetHeroTeleportTarget(Hero hero, out Settlement targetSettlement)
	{
		targetSettlement = null;
		try
		{
			ITeleportationCampaignBehavior teleportation = Campaign.Current?.GetCampaignBehavior<ITeleportationCampaignBehavior>();
			if (teleportation == null || !teleportation.GetTargetOfTeleportingHero(hero, out _, out _, out IMapPoint target))
			{
				return false;
			}
			targetSettlement = target as Settlement;
			return targetSettlement != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHeroTeleportingToSettlement(Hero hero, Settlement settlement)
	{
		return settlement != null && TryGetHeroTeleportTarget(hero, out Settlement targetSettlement) && targetSettlement == settlement;
	}

	private static bool CanHeroMoveToSettlement(Hero hero, out string reason)
	{
		reason = "";
		try
		{
			if (hero != null && hero.CanMoveToSettlement())
			{
				return true;
			}
			reason = "当前无法移动至定居点";
			return false;
		}
		catch (Exception ex)
		{
			reason = "移动检查失败：" + ex.Message;
			return false;
		}
	}

	private static void RefreshTemporaryPartyRoute(MobileParty party, Settlement targetSettlement)
	{
		if (party == null || targetSettlement == null || !party.IsActive)
		{
			return;
		}
		try
		{
			PrepareTemporaryGatheringPartyForLandTravel(party);
			LockTemporaryPartyNativeAi(party, "route");
			if (!RepairTemporaryGatheringPartyPosition(party, targetSettlement, "route"))
			{
				party.SetMoveModeHold();
				return;
			}
			if (party.CurrentSettlement == targetSettlement)
			{
				party.SetMoveModeHold();
				return;
			}
			string previousTarget = party.TargetSettlement?.StringId ?? "";
			LeaveSettlementIfNeeded(party);
			party.SetMoveGoToSettlement(targetSettlement, MobileParty.NavigationType.Default, false);
			if (!string.Equals(previousTarget, targetSettlement.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				Log("temporary party route set party=" + (party.StringId ?? "") + " target=" + (targetSettlement.StringId ?? "") + " previousTarget=" + previousTarget + " leader=" + (party.LeaderHero?.StringId ?? "null"));
			}
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "temporary party route failed party=" + (party?.StringId ?? "") + " target=" + (targetSettlement?.StringId ?? "") + " error=" + ex);
		}
	}

	private static bool EnsureTemporaryPartyAtSettlement(MobileParty party, Settlement settlement, string reason)
	{
		if (party == null || settlement == null || !party.IsActive)
		{
			return false;
		}
		try
		{
			LockTemporaryPartyNativeAi(party, "arrival_" + (reason ?? ""));
			if (!RepairTemporaryGatheringPartyPosition(party, settlement, "arrival_" + (reason ?? "")))
			{
				return false;
			}
			if (party.CurrentSettlement == settlement)
			{
				party.SetMoveModeHold();
				return true;
			}
			CampaignVec2 arrivalPosition = settlement.GatePosition;
			if (TryResolveTemporaryGatheringLandPosition(settlement, out CampaignVec2 safeArrivalPosition, out _))
			{
				arrivalPosition = safeArrivalPosition;
			}
			if (party.Position.Distance(arrivalPosition) > ArrivalDistance)
			{
				return false;
			}
			LeaveSettlementIfNeeded(party);
			EnterSettlementAction.ApplyForParty(party, settlement);
			party.SetMoveModeHold();
			Log("temporary party arrived party=" + (party.StringId ?? "") + " settlement=" + (settlement.StringId ?? "") + " reason=" + (reason ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			Log("temporary party enter settlement failed party=" + (party?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private void RepairTrackedTemporaryGatheringParties(string reason)
	{
		try
		{
			foreach (NobleGatheringRecord record in _gatherings.Values.ToList())
			{
				if (record == null)
				{
					continue;
				}
				RepairTrackedTemporaryHostParty(record, reason);
				foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
				{
					RepairTrackedTemporaryInviteeParty(record, invitee, reason);
				}
			}
			CleanupUntrackedTemporaryParties();
		}
		catch (Exception ex)
		{
			Log("temporary party repair sweep failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void RepairTrackedTemporaryHostParty(NobleGatheringRecord record, string reason)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.HostTemporaryPartyId))
		{
			return;
		}
		MobileParty party = ResolveMobilePartyById(record.HostTemporaryPartyId);
		if (party == null || !party.IsActive)
		{
			return;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement preferred = ResolveTemporaryPartyPreferredSettlement(record.HostTemporaryPartyPhase, record.SettlementId, record.HostOriginSettlementId, host, party);
		if (RepairTemporaryGatheringPartyPosition(party, preferred, (reason ?? "") + "_host"))
		{
			return;
		}
		DestroyTemporaryPartyAfterRemovingHero(party, host, preferred, (reason ?? "") + "_host_bad_position");
		record.HostTemporaryPartyPhase = TemporaryPartyPhaseCleaned;
		record.HostTemporaryPartyId = "";
	}

	private void RepairTrackedTemporaryInviteeParty(NobleGatheringRecord record, NobleGatheringInviteeRecord invitee, string reason)
	{
		if (record == null || invitee == null || string.IsNullOrWhiteSpace(invitee.TemporaryPartyId))
		{
			return;
		}
		MobileParty party = ResolveMobilePartyById(invitee.TemporaryPartyId);
		if (party == null || !party.IsActive)
		{
			return;
		}
		Hero hero = ResolveHeroById(invitee.HeroId);
		Settlement preferred = ResolveTemporaryPartyPreferredSettlement(invitee.TemporaryPartyPhase, record.SettlementId, invitee.OriginSettlementId, hero, party);
		if (RepairTemporaryGatheringPartyPosition(party, preferred, (reason ?? "") + "_invitee"))
		{
			return;
		}
		DestroyTemporaryPartyAfterRemovingHero(party, hero, preferred, (reason ?? "") + "_invitee_bad_position");
		invitee.Status = InviteFailed;
		invitee.Reason = "temporary_party_bad_position";
		invitee.TemporaryPartyPhase = TemporaryPartyPhaseCleaned;
		invitee.TemporaryPartyId = "";
	}

	private static Settlement ResolveTemporaryPartyPreferredSettlement(string phase, string gatheringSettlementId, string originSettlementId, Hero hero, MobileParty party)
	{
		Settlement gathering = ResolveSettlementById(gatheringSettlementId);
		Settlement origin = ResolveSettlementById(originSettlementId);
		Settlement home = ResolveBestHeroHomeSettlement(hero) ?? party?.HomeSettlement;
		if (string.Equals(phase, TemporaryPartyPhaseReturning, StringComparison.OrdinalIgnoreCase))
		{
			return origin ?? home ?? party?.CurrentSettlement ?? party?.TargetSettlement ?? gathering;
		}
		return gathering ?? party?.TargetSettlement ?? party?.CurrentSettlement ?? origin ?? home;
	}

	private static void PrepareTemporaryGatheringPartyForLandTravel(MobileParty party)
	{
		if (party == null || !party.IsActive)
		{
			return;
		}
		try
		{
			party.SetLandNavigationAccess(true);
			LockTemporaryPartyNativeAi(party, "prepare_land_travel");
			if (IsValidTemporaryGatheringLandPosition(party.Position))
			{
				party.IsCurrentlyAtSea = false;
			}
		}
		catch
		{
		}
	}

	private static bool RepairTemporaryGatheringPartyPosition(MobileParty party, Settlement preferredSettlement, string reason)
	{
		if (!IsTemporaryGatheringParty(party) || !party.IsActive)
		{
			return false;
		}
		try
		{
			PrepareTemporaryGatheringPartyForLandTravel(party);
			LockTemporaryPartyNativeAi(party, "repair_" + (reason ?? ""));
			if (!party.IsCurrentlyAtSea && IsValidTemporaryGatheringLandPosition(party.Position))
			{
				return true;
			}
			Settlement fallback = preferredSettlement ?? party.TargetSettlement ?? party.CurrentSettlement ?? party.HomeSettlement ?? ResolveBestHeroHomeSettlement(party.LeaderHero);
			string positionReason = "";
			if (fallback == null || !TryResolveTemporaryGatheringLandPosition(fallback, out CampaignVec2 position, out positionReason))
			{
				Log("temporary party repair no safe position party=" + (party.StringId ?? "") + " settlement=" + (fallback?.StringId ?? "") + " reason=" + (reason ?? "") + " positionReason=" + (positionReason ?? ""));
				return false;
			}
			LeaveSettlementIfNeeded(party);
			party.SetLandNavigationAccess(true);
			party.IsCurrentlyAtSea = false;
			party.Position = position;
			party.MoveTargetPoint = position;
			party.SetMoveModeHold();
			Log("temporary party position repaired party=" + (party.StringId ?? "") + " settlement=" + (fallback.StringId ?? "") + " reason=" + (reason ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			Log("temporary party repair failed party=" + (party?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static bool TryResolveTemporaryGatheringLandPosition(Settlement settlement, out CampaignVec2 position, out string reason)
	{
		position = CampaignVec2.Invalid;
		reason = "";
		if (settlement == null)
		{
			reason = "settlement_missing";
			return false;
		}
		CampaignVec2[] centers = new CampaignVec2[]
		{
			settlement.GatePosition,
			settlement.Position
		};
		foreach (CampaignVec2 center in centers)
		{
			if (TryResolveReachableTemporaryGatheringLandPosition(center, out position))
			{
				return true;
			}
			if (TryGetClosestTemporaryGatheringLandCenter(center, out CampaignVec2 closest)
				&& TryResolveReachableTemporaryGatheringLandPosition(closest, out position))
			{
				return true;
			}
		}
		reason = "no_valid_land_position";
		return false;
	}

	private static bool TryResolveReachableTemporaryGatheringLandPosition(CampaignVec2 center, out CampaignVec2 position)
	{
		position = CampaignVec2.Invalid;
		try
		{
			if (!center.IsValid())
			{
				return false;
			}
			CampaignVec2 candidate = Helpers.NavigationHelper.FindReachablePointAroundPosition(center, MobileParty.NavigationType.Default, TemporaryPartySpawnRadius, TemporaryPartySpawnMinRadius, true);
			if (IsValidTemporaryGatheringLandPosition(candidate))
			{
				position = candidate;
				return true;
			}
			if (IsValidTemporaryGatheringLandPosition(center))
			{
				position = center;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryGetClosestTemporaryGatheringLandCenter(CampaignVec2 center, out CampaignVec2 position)
	{
		position = CampaignVec2.Invalid;
		try
		{
			if (!center.IsValid())
			{
				return false;
			}
			int[] invalidTerrainTypes = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default);
			position = Helpers.NavigationHelper.GetClosestNavMeshFaceCenterPositionForPosition(center, invalidTerrainTypes);
			return position.IsValid();
		}
		catch
		{
			return false;
		}
	}

	private static bool IsValidTemporaryGatheringLandPosition(CampaignVec2 position)
	{
		try
		{
			return position.IsValid()
				&& position.IsOnLand
				&& Helpers.NavigationHelper.IsPositionValidForNavigationType(position, MobileParty.NavigationType.Default)
				&& IsTemporaryGatheringPositionInsideWeatherBounds(position)
				&& IsTemporaryGatheringPositionWeatherSafe(position);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsTemporaryGatheringPositionInsideWeatherBounds(CampaignVec2 position)
	{
		try
		{
			if (Campaign.Current?.MapSceneWrapper == null)
			{
				return true;
			}
			Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
			Vec2 vec = position.ToVec2();
			if (float.IsNaN(vec.x) || float.IsNaN(vec.y))
			{
				return false;
			}
			if (terrainSize.X <= 0f || terrainSize.Y <= 0f)
			{
				return true;
			}
			return vec.x >= 0f && vec.y >= 0f && vec.x < terrainSize.X && vec.y < terrainSize.Y;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsTemporaryGatheringPositionWeatherSafe(CampaignVec2 position)
	{
		try
		{
			if (Campaign.Current?.Models?.MapWeatherModel == null)
			{
				return true;
			}
			Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(position.ToVec2());
			return true;
		}
		catch (IndexOutOfRangeException)
		{
			return false;
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static void EnsureTemporaryGatheringPartySupplies(MobileParty party)
	{
		if (!IsTemporaryGatheringParty(party) || !party.IsActive || party.ItemRoster == null)
		{
			return;
		}
		try
		{
			int missingFood = TemporaryPartyTargetFood - party.ItemRoster.TotalFood;
			if (missingFood <= 0)
			{
				return;
			}
			ItemObject food = ResolveTemporaryGatheringFoodItem();
			if (food == null)
			{
				return;
			}
			party.ItemRoster.AddToCounts(food, missingFood);
		}
		catch (Exception ex)
		{
			Log("temporary party supply failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static ItemObject ResolveTemporaryGatheringFoodItem()
	{
		ItemObject food = DefaultItems.Grain;
		if (food != null && food.IsFood)
		{
			return food;
		}
		return MBObjectManager.Instance.GetObjectTypeList<ItemObject>().FirstOrDefault(item => item != null && item.IsFood);
	}

	private static void DestroyTemporaryPartyAfterRemovingHero(MobileParty party, Hero hero, Settlement finalSettlement, string reason)
	{
		if (party == null)
		{
			SafelyPlaceHeroBackToSettlement(hero, finalSettlement, reason + "_missing_party");
			return;
		}
		try
		{
			LockTemporaryPartyNativeAi(party, "destroy_" + (reason ?? ""));
			if (hero?.CharacterObject != null && party.MemberRoster != null && party.MemberRoster.Contains(hero.CharacterObject))
			{
				party.MemberRoster.AddToCounts(hero.CharacterObject, -1, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
			}
			SafelyPlaceHeroBackToSettlement(hero, finalSettlement, reason);
			if (party.IsActive)
			{
				MarkTemporaryPartyForDelayedDestroy(party, reason);
			}
		}
		catch (Exception ex)
		{
			Log("destroy temporary party failed party=" + (party?.StringId ?? "") + " hero=" + (hero?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void MarkTemporaryPartyForDelayedDestroy(MobileParty party, string reason)
	{
		if (!IsTemporaryGatheringParty(party) || !party.IsActive)
		{
			return;
		}
		try
		{
			LockTemporaryPartyNativeAi(party, "delayed_destroy_mark");
			party.SetMoveModeHold();
			string partyId = party.StringId ?? "";
			if (string.IsNullOrWhiteSpace(partyId))
			{
				return;
			}
			lock (PendingTemporaryPartyDestroyLock)
			{
				if (!PendingTemporaryPartyDestroys.TryGetValue(partyId, out PendingTemporaryPartyDestroyRecord record))
				{
					record = new PendingTemporaryPartyDestroyRecord
					{
						PartyId = partyId,
						Attempts = 0
					};
					PendingTemporaryPartyDestroys[partyId] = record;
				}
				record.Reason = (reason ?? "").Trim();
				record.MarkedUtcTicks = DateTime.UtcNow.Ticks;
				System.Threading.Volatile.Write(ref _hasPendingTemporaryPartyDestroys, 1);
			}
			Logger.LogImmediate(LogSource, "temporary party delayed destroy marked party=" + partyId + " reason=" + (reason ?? "") + " leader=" + (party.LeaderHero?.StringId ?? "null") + " settlement=" + (party.CurrentSettlement?.StringId ?? "null") + " target=" + (party.TargetSettlement?.StringId ?? "null"));
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "temporary party delayed destroy mark failed party=" + (party?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex);
		}
	}

	private static void ProcessPendingTemporaryPartyDestroysOnEngineTick()
	{
		if (System.Threading.Volatile.Read(ref _hasPendingTemporaryPartyDestroys) == 0)
		{
			return;
		}
		try
		{
			List<PendingTemporaryPartyDestroyRecord> due;
			lock (PendingTemporaryPartyDestroyLock)
			{
				if (PendingTemporaryPartyDestroys.Count == 0)
				{
					System.Threading.Volatile.Write(ref _hasPendingTemporaryPartyDestroys, 0);
					return;
				}
				long nowTicks = DateTime.UtcNow.Ticks;
				long delayTicks = TimeSpan.FromMilliseconds(PendingTemporaryPartyDestroyDelayMs).Ticks;
				due = new List<PendingTemporaryPartyDestroyRecord>();
				foreach (PendingTemporaryPartyDestroyRecord record in PendingTemporaryPartyDestroys.Values)
				{
					if (record == null || string.IsNullOrWhiteSpace(record.PartyId))
					{
						continue;
					}
					if (nowTicks - record.MarkedUtcTicks < delayTicks)
					{
						continue;
					}
					due.Add(record);
				}
				foreach (PendingTemporaryPartyDestroyRecord record in due)
				{
					PendingTemporaryPartyDestroys.Remove(record.PartyId);
				}
				if (PendingTemporaryPartyDestroys.Count == 0)
				{
					System.Threading.Volatile.Write(ref _hasPendingTemporaryPartyDestroys, 0);
				}
			}
			foreach (PendingTemporaryPartyDestroyRecord record in due)
			{
				ApplyPendingTemporaryPartyDestroy(record);
			}
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "pending temporary party destroy sweep failed: " + ex);
		}
	}

	private static void ApplyPendingTemporaryPartyDestroy(PendingTemporaryPartyDestroyRecord record)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.PartyId))
		{
			return;
		}
		MobileParty party = ResolveMobilePartyById(record.PartyId);
		if (party == null || !party.IsActive)
		{
			Logger.LogImmediate(LogSource, "pending temporary party destroy skipped inactive party=" + (record.PartyId ?? "") + " reason=" + (record.Reason ?? ""));
			return;
		}
		if (!IsTemporaryGatheringParty(party))
		{
			Logger.LogImmediate(LogSource, "pending temporary party destroy skipped non-temp party=" + (record.PartyId ?? "") + " reason=" + (record.Reason ?? ""));
			return;
		}
		try
		{
			LockTemporaryPartyNativeAi(party, "delayed_destroy_apply");
			party.SetMoveModeHold();
			if (party.CurrentSettlement != null)
			{
				LeaveSettlementIfNeeded(party);
			}
			Logger.LogImmediate(LogSource, "pending temporary party destroy apply party=" + (party.StringId ?? "") + " reason=" + (record.Reason ?? "") + " attempts=" + record.Attempts + " leader=" + (party.LeaderHero?.StringId ?? "null") + " settlement=" + (party.CurrentSettlement?.StringId ?? "null") + " target=" + (party.TargetSettlement?.StringId ?? "null"));
			DestroyPartyAction.Apply(null, party);
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "pending temporary party destroy failed party=" + (record.PartyId ?? "") + " reason=" + (record.Reason ?? "") + " attempts=" + record.Attempts + " error=" + ex);
			if (record.Attempts >= MaxPendingTemporaryPartyDestroyAttempts)
			{
				return;
			}
			record.Attempts++;
			record.MarkedUtcTicks = DateTime.UtcNow.Ticks;
			lock (PendingTemporaryPartyDestroyLock)
			{
				PendingTemporaryPartyDestroys[record.PartyId] = record;
				System.Threading.Volatile.Write(ref _hasPendingTemporaryPartyDestroys, 1);
			}
		}
	}

	private static void LockTemporaryPartyNativeAi(MobileParty party, string reason)
	{
		if (!IsTemporaryGatheringParty(party) || !party.IsActive)
		{
			return;
		}
		try
		{
			if (party.Ai != null && !party.Ai.DoNotMakeNewDecisions)
			{
				party.Ai.SetDoNotMakeNewDecisions(true);
				Log("temporary party native ai locked party=" + (party.StringId ?? "") + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Logger.LogImmediate(LogSource, "temporary party native ai lock failed party=" + (party?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex);
		}
	}

	private static void SafelyPlaceHeroBackToSettlement(Hero hero, Settlement settlement, string reason)
	{
		if (hero == null || hero.IsDead || hero.IsPrisoner || settlement == null)
		{
			return;
		}
		try
		{
			if (hero.PartyBelongedTo != null)
			{
				return;
			}
			if (hero.CurrentSettlement != settlement)
			{
				if (hero.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForCharacterOnly(hero);
				}
				EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
			}
		}
		catch (Exception ex)
		{
			Log("place hero back failed hero=" + (hero?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void LeaveSettlementIfNeeded(MobileParty party)
	{
		try
		{
			if (party?.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForParty(party);
			}
		}
		catch
		{
		}
	}

	private static void CleanupUntrackedTemporaryParties()
	{
		try
		{
			HashSet<string> tracked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			NobleGatheringBehavior behavior = Instance;
			if (behavior != null)
			{
				foreach (NobleGatheringRecord record in behavior._gatherings.Values)
				{
					if (!string.IsNullOrWhiteSpace(record?.HostTemporaryPartyId))
					{
						tracked.Add(record.HostTemporaryPartyId);
					}
					foreach (NobleGatheringInviteeRecord invitee in record?.Invitees ?? new List<NobleGatheringInviteeRecord>())
					{
						if (!string.IsNullOrWhiteSpace(invitee?.TemporaryPartyId))
						{
							tracked.Add(invitee.TemporaryPartyId);
						}
					}
				}
			}
			foreach (MobileParty party in MobileParty.All?.ToList() ?? new List<MobileParty>())
			{
				if (!IsTemporaryGatheringParty(party) || tracked.Contains(party.StringId ?? ""))
				{
					continue;
				}
				Hero leader = party.LeaderHero;
				Settlement home = ResolveBestHeroHomeSettlement(leader) ?? party.HomeSettlement;
				DestroyTemporaryPartyAfterRemovingHero(party, leader, home, "orphan_cleanup");
			}
		}
		catch (Exception ex)
		{
			Log("orphan cleanup failed: " + ex.Message);
		}
	}

	public static bool IsTemporaryGatheringParty(MobileParty party)
	{
		return party != null && IsTemporaryGatheringPartyId(party.StringId);
	}

	public static bool IsTemporaryGatheringPartyId(string partyId)
	{
		return !string.IsNullOrWhiteSpace(partyId) && partyId.StartsWith(TemporaryPartyPrefix, StringComparison.OrdinalIgnoreCase);
	}

	private static MobileParty ResolveMobilePartyById(string partyId)
	{
		string id = (partyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return MobileParty.All?.FirstOrDefault(party => string.Equals(party?.StringId, id, StringComparison.OrdinalIgnoreCase));
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero)
	{
		if (!NobleGatheringOptions.Capture().Enabled)
		{
			return new List<PostprocessRuleEntry>();
		}
		return (AIConfigHandler.GetGuardrailRulePostprocessRules("noble_gathering") ?? new List<PostprocessRuleEntry>())
			.Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.Tag))
			.Select(rule => new PostprocessRuleEntry
			{
				Tag = (rule.Tag ?? "").Trim(),
				Description = (rule.Description ?? "").Trim()
			})
			.ToList();
	}

	public static string BuildPostprocessContextForExternal(Hero conversationHero)
	{
		try
		{
			if (!NobleGatheringOptions.Capture().Enabled)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("【贵族宴会可用ID】");
			sb.AppendLine("输出格式：");
			sb.AppendLine("[ACTION:NOBLE_GATHERING:START:settlement=settlementId:heroes=heroId1,heroId2:clans=clanId1:kingdoms=kingdomId1:cultures=cultureId1:age=18-30:gender=female]");
			sb.AppendLine("[ACTION:NOBLE_GATHERING:CANCEL:gathering=gatheringId]");
			sb.AppendLine("若玩家只说邀请某家族/王国/文化/年龄/性别群体，可以省略 heroes，由系统按筛选展开。所有 id 必须优先使用下方候选，不要凭空猜。");
			NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			List<NobleGatheringRecord> cancellableGatherings = behavior?.GetCancellableGatheringsForConversationHero(conversationHero) ?? new List<NobleGatheringRecord>();
			sb.AppendLine();
			sb.AppendLine("当前对话可取消的宴会：");
			if (cancellableGatherings.Count == 0)
			{
				sb.AppendLine("- 无。禁止输出取消宴会标签。");
			}
			foreach (NobleGatheringRecord record in cancellableGatherings)
			{
				Hero host = ResolveHeroById(record.HostHeroId);
				Settlement venue = ResolveSettlementById(record.SettlementId);
				string mode = record.IsPlayerHosted ? "player_order（玩家命令，当前同伴/总督必须执行）" : "host_persuasion（当前NPC是主人，只有明确接受劝说才执行）";
				sb.AppendLine("- gathering=" + (record.Id ?? "")
					+ " | host=" + GetHeroName(host)
					+ " | settlement=" + GetSettlementName(venue)
					+ " | mode=" + mode);
			}
			sb.AppendLine();
			sb.AppendLine("举办地候选：");
			foreach (Settlement settlement in GetPlayerHostSettlements(ResolveConversationSuggestedSettlement(conversationHero)).Take(24))
			{
				sb.AppendLine("- settlement=" + (settlement.StringId ?? "") + " | " + GetSettlementName(settlement) + " | " + (settlement.IsTown ? "town" : settlement.IsCastle ? "castle" : "settlement"));
			}
			sb.AppendLine();
			sb.AppendLine("家族候选：");
			foreach (Clan clan in GetPlayerGatheringCandidateClans(null).Take(60))
			{
				sb.AppendLine("- clan=" + (clan.StringId ?? "") + " | " + GetClanName(clan) + " | kingdom=" + (clan.Kingdom?.StringId ?? "") + " | culture=" + (clan.Culture?.StringId ?? ""));
			}
			sb.AppendLine();
			sb.AppendLine("人物候选：");
			foreach (Hero hero in Hero.AllAliveHeroes.Where(IsGatheringGroupCandidateHero).OrderBy(hero => GetClanName(hero.Clan)).ThenBy(GetHeroName).Take(120))
			{
				sb.AppendLine("- hero=" + (hero.StringId ?? "") + " | " + GetHeroName(hero) + " | clan=" + (hero.Clan?.StringId ?? "") + " | kingdom=" + (hero.Clan?.Kingdom?.StringId ?? "") + " | culture=" + (hero.Culture?.StringId ?? "") + " | age=" + Math.Floor(hero.Age) + " | gender=" + (hero.IsFemale ? "female" : "male"));
			}
			sb.AppendLine();
			sb.AppendLine("王国候选：");
			foreach (Kingdom kingdom in Kingdom.All?.Where(k => k != null && !k.IsEliminated).OrderBy(k => k.Name?.ToString() ?? k.StringId ?? "").Take(40) ?? Enumerable.Empty<Kingdom>())
			{
				sb.AppendLine("- kingdom=" + (kingdom.StringId ?? "") + " | " + (kingdom.Name?.ToString() ?? kingdom.StringId ?? "") + " | culture=" + (kingdom.Culture?.StringId ?? ""));
			}
			sb.AppendLine();
			sb.AppendLine("文化候选：");
			foreach (CultureObject culture in MBObjectManager.Instance.GetObjectTypeList<CultureObject>()?.Where(c => c != null && !string.IsNullOrWhiteSpace(c.StringId)).OrderBy(c => c.Name?.ToString() ?? c.StringId ?? "").Take(40) ?? Enumerable.Empty<CultureObject>())
			{
				sb.AppendLine("- culture=" + (culture.StringId ?? "") + " | " + (culture.Name?.ToString() ?? culture.StringId ?? ""));
			}
			return sb.ToString().Trim();
		}
		catch (Exception ex)
		{
			Log("build postprocess context failed: " + ex.Message);
			return "";
		}
	}

	public static bool IsHeroAtFeast(Hero hero)
	{
		try
		{
			if (hero == null) return false;
			NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			return behavior != null && behavior._heroActiveFeastId.ContainsKey(hero.StringId ?? "");
		}
		catch { return false; }
	}

	public static bool IsClanAtFeast(Clan clan)
	{
		try
		{
			if (clan == null) return false;
			NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			return behavior != null && behavior._feastAttendeeClanIds.Contains(clan.StringId ?? "");
		}
		catch { return false; }
	}

	public static string BuildFeastAttendanceContext(Hero hero)
	{
		try
		{
			if (hero == null) return "";
			NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			if (behavior == null) return "";
			string heroId = hero.StringId ?? "";
			NobleGatheringRecord gathering = behavior.FindActiveGatheringForHero(hero);
			if (gathering == null) return "";
			Hero host = ResolveHeroById(gathering.HostHeroId);
			Settlement settlement = ResolveSettlementById(gathering.SettlementId);
			if (host == null || settlement == null) return "";
			double now = NowDay();
			int dayNumber = Math.Max(1, (int)Math.Ceiling(now - gathering.StartDay) + 1);
			int totalDays = GetRecordDurationDays(gathering);
			string role;
			if (string.Equals(heroId, gathering.HostHeroId, StringComparison.OrdinalIgnoreCase))
				role = "主办方";
			else if (hero == Hero.MainHero)
				role = "玩家（赴宴嘉宾）";
			else
				role = "受邀宾客";
			bool isHost = string.Equals(heroId, gathering.HostHeroId, StringComparison.OrdinalIgnoreCase);
			bool isAtVenue = IsHeroAtSettlement(hero, settlement);
			StringBuilder sb = new StringBuilder();
			if (isHost && !isAtVenue)
			{
				sb.AppendLine("【当前宴会事实】你举办的贵族宴会已经开始执行，你正赶往" + GetSettlementName(settlement) + "主持宴会。不得把宴会说成尚未安排、只是传闻或与你无关。");
			}
			else
			{
				sb.AppendLine("【当前宴会事实】你此刻正在" + GetSettlementName(settlement) + "参加一场正在进行的贵族宴会。不得把宴会说成尚未开始、只是计划或与你无关。");
			}
			sb.AppendLine("- 主办方：" + GetHeroName(host));
			sb.AppendLine("- 举办地：" + GetSettlementName(settlement));
			sb.AppendLine("- 宴会已进行到第 " + dayNumber + " 天，共计 " + totalDays + " 天");
			sb.AppendLine("- 你的身份：" + role);
			if (isHost)
			{
				sb.AppendLine(isAtVenue
					? "- 你就是这场宴会的主人和主办者。你清楚宾客是应你的邀请而来，应留在举办地并以主人身份接待、回应和谈论宴会。"
					: "- 你就是这场宴会的主人和主办者。你必须尽快抵达举办地，并在宴会结束前留在那里接待宾客。");
			}
			else
			{
				sb.AppendLine("- 你已经抵达并作为宾客赴宴。你清楚" + GetHeroName(host) + "是宴会主人，可以自然谈论宴席、宾客和当前局势。");
			}
			return sb.ToString().Trim();
		}
		catch (Exception ex)
		{
			Log("build feast attendance context failed: " + ex.Message);
			return "";
		}
	}

	public static string BuildRecentDiplomacyMaterialForExternal(IEnumerable<string> relevantKingdomIds, int maxCount = 3)
	{
		try
		{
			NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			HashSet<string> relevant = new HashSet<string>((relevantKingdomIds ?? Enumerable.Empty<string>())
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id.Trim()), StringComparer.OrdinalIgnoreCase);
			if (behavior == null || relevant.Count == 0)
			{
				return "";
			}

			double now = NowDay();
			List<string> lines = new List<string>();
			foreach (NobleGatheringRecord record in behavior._gatherings.Values
				.Where(item => item != null
					&& string.Equals(item.State, StateActive, StringComparison.OrdinalIgnoreCase)
					&& item.StartDay <= now
					&& now < item.EndDay
					&& now - item.StartDay <= 14d)
				.OrderByDescending(item => item.StartDay)
				.ThenBy(item => item.Id ?? "", StringComparer.OrdinalIgnoreCase))
			{
				Hero host = ResolveHeroById(record.HostHeroId);
				Settlement settlement = ResolveSettlementById(record.SettlementId);
				string hostKingdomId = !string.IsNullOrWhiteSpace(record.KingdomId)
					? record.KingdomId.Trim()
					: (host?.Clan?.Kingdom?.StringId ?? "").Trim();
				List<Hero> relevantGuests = (record.Invitees ?? new List<NobleGatheringInviteeRecord>())
					.Where(invitee => invitee != null
						&& (string.Equals(invitee.Status, InviteAccepted, StringComparison.OrdinalIgnoreCase)
							|| string.Equals(invitee.Status, InviteArrived, StringComparison.OrdinalIgnoreCase)))
					.Select(invitee => ResolveHeroById(invitee.HeroId))
					.Where(hero => hero != null && relevant.Contains(hero.Clan?.Kingdom?.StringId ?? ""))
					.Take(4)
					.ToList();
				if (!relevant.Contains(hostKingdomId) && relevantGuests.Count == 0)
				{
					continue;
				}

				StringBuilder line = new StringBuilder();
				line.Append(GetHeroName(host)).Append("已经在").Append(GetSettlementName(settlement)).Append("举办贵族宴会");
				if (relevantGuests.Count > 0)
				{
					line.Append("；与相关各国有关的已受邀或已抵达宾客包括：")
						.Append(string.Join("、", relevantGuests.Select(GetHeroName)));
				}
				line.Append("。这是已经开始的公开事件，不代表任何外交协议已经成立。");
				lines.Add(line.ToString());
				if (lines.Count >= Math.Max(1, Math.Min(3, maxCount)))
				{
					break;
				}
			}
			return lines.Count == 0 ? "" : "- " + string.Join("\n- ", lines);
		}
		catch (Exception ex)
		{
			Log("build diplomacy feast material failed: " + ex.Message);
			return "";
		}
	}

	private void RegisterFeastAttendee(Hero hero, NobleGatheringRecord record)
	{
		try
		{
			if (hero == null || record == null || string.IsNullOrWhiteSpace(hero.StringId)) return;
			_heroActiveFeastId[hero.StringId] = record.Id;
			if (!string.IsNullOrWhiteSpace(hero.Clan?.StringId))
				_feastAttendeeClanIds.Add(hero.Clan.StringId);
		}
		catch (Exception ex) { Log("register feast attendee failed: " + ex.Message); }
	}

	private void UnregisterFeastAttendees(NobleGatheringRecord record)
	{
		try
		{
			if (record == null) return;
			string hostId = record.HostHeroId ?? "";
			if (!string.IsNullOrWhiteSpace(hostId)) _heroActiveFeastId.Remove(hostId);
			foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
			{
				string heroId = invitee?.HeroId ?? "";
				if (!string.IsNullOrWhiteSpace(heroId)) _heroActiveFeastId.Remove(heroId);
			}
			RebuildClanIndex();
		}
		catch (Exception ex) { Log("unregister feast attendees failed: " + ex.Message); }
	}

	private void RebuildClanIndex()
	{
		try
		{
			_feastAttendeeClanIds.Clear();
			double now = NowDay();
			foreach (NobleGatheringRecord r in _gatherings.Values)
			{
				if (r == null || !string.Equals(r.State, StateActive, StringComparison.OrdinalIgnoreCase) || now >= r.EndDay) continue;
				foreach (string heroId in _heroActiveFeastId.Keys.ToList())
				{
					if (!string.Equals(_heroActiveFeastId[heroId], r.Id, StringComparison.OrdinalIgnoreCase)) continue;
					Hero h = ResolveHeroById(heroId);
					if (h?.Clan?.StringId != null) _feastAttendeeClanIds.Add(h.Clan.StringId);
				}
			}
		}
		catch (Exception ex) { Log("rebuild clan index failed: " + ex.Message); }
	}

	private void RebuildFeastAttendeeIndex()
	{
		try
		{
			_heroActiveFeastId.Clear();
			_feastAttendeeClanIds.Clear();
			double now = NowDay();
			foreach (NobleGatheringRecord r in _gatherings.Values)
			{
				if (r == null || !string.Equals(r.State, StateActive, StringComparison.OrdinalIgnoreCase) || now >= r.EndDay) continue;
				Hero host = ResolveHeroById(r.HostHeroId);
				if (host != null) RegisterFeastAttendee(host, r);
				foreach (NobleGatheringInviteeRecord invitee in r.Invitees ?? new List<NobleGatheringInviteeRecord>())
				{
					if (invitee == null || !string.Equals(invitee.Status, InviteArrived, StringComparison.OrdinalIgnoreCase)) continue;
					Hero h = ResolveHeroById(invitee.HeroId);
					if (h != null) RegisterFeastAttendee(h, r);
				}
			}
		}
		catch (Exception ex) { Log("rebuild feast attendee index failed: " + ex.Message); }
	}

public static string NormalizeNobleGatheringPostprocessTagsForExternal(string raw)
	{
		List<string> tags = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in NobleGatheringActionTagRegex.Matches(raw ?? ""))
		{
			string tag = (match.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(tag) && seen.Add(tag))
			{
				tags.Add(tag);
			}
		}
		return string.Join("\n", tags).Trim();
	}

	public static bool TryApplyNobleGatheringTagsForExternal(Hero conversationHero, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		string original = content ?? "";
		MatchCollection matches = NobleGatheringActionTagRegex.Matches(original);
		if (matches == null || matches.Count == 0)
		{
			return false;
		}
		content = NobleGatheringActionTagRegex.Replace(original, "").Trim();
		NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
		if (behavior == null)
		{
			notifications.Add("宴会系统未初始化。");
			return true;
		}
		foreach (Match match in matches)
		{
			if (TryParseCancellationTag(match.Value, out string gatheringId))
			{
				if (behavior.TryCancelGatheringFromConversation(gatheringId, conversationHero, out string cancelStatus, out string cancelFact))
				{
					notifications.Add(cancelStatus);
					if (!string.IsNullOrWhiteSpace(cancelFact))
					{
						generatedFacts.Add(cancelFact);
					}
					return true;
				}
				notifications.Add(cancelStatus);
				continue;
			}
			NobleGatheringInvitationSelector selector = ParseInvitationSelectorTag(match.Value);
			if (selector == null)
			{
				continue;
			}
			if (behavior.TryCreatePlayerHostedGatheringFromSelector(selector, conversationHero, out string status, out string fact))
			{
				notifications.Add(status);
				if (!string.IsNullOrWhiteSpace(fact))
				{
					generatedFacts.Add(fact);
				}
				return true;
			}
			notifications.Add(status);
		}
		if (notifications.Count == 0)
		{
			notifications.Add("宴会指令未执行：标签参数无法解析。");
		}
		return true;
	}

	private bool TryCancelGatheringFromConversation(string gatheringId, Hero conversationHero, out string status, out string generatedFact)
	{
		status = "";
		generatedFact = "";
		string id = (gatheringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)
			|| !_gatherings.TryGetValue(id, out NobleGatheringRecord record)
			|| record == null
			|| !string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase))
		{
			status = "取消宴会失败：目标宴会不存在或已经结束。";
			return false;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		if (record.IsPlayerHosted)
		{
			if (!IsPlayerGatheringCancellationDelegate(conversationHero))
			{
				status = "取消宴会失败：当前对话对象不是你的总督或同伴，不能代你执行散宴命令。";
				return false;
			}
			string delegateName = GetHeroName(conversationHero);
			CancelGathering(record, "玩家命令" + delegateName + "立即散宴。");
			status = delegateName + "已经遵照你的命令取消了在" + GetSettlementName(settlement) + "举办的宴会。";
			generatedFact = "[AFEF玩家行为补充] 玩家命令" + delegateName + "取消了自己在" + GetSettlementName(settlement) + "举办的宴会，宾客已经散去。";
			return true;
		}
		if (host == null
			|| conversationHero == null
			|| !string.Equals(host.StringId ?? "", conversationHero.StringId ?? "", StringComparison.OrdinalIgnoreCase))
		{
			status = "取消宴会失败：只有宴会主人本人才能接受劝说并取消这场宴会。";
			return false;
		}
		CancelGathering(record, GetHeroName(host) + "接受玩家劝说后决定散宴。");
		status = GetHeroName(host) + "已经接受你的劝说，取消了在" + GetSettlementName(settlement) + "举办的宴会。";
		generatedFact = "[AFEF NPC行为补充] " + GetHeroName(host) + "接受玩家劝说，取消了自己在" + GetSettlementName(settlement) + "举办的宴会，宾客已经散去。";
		return true;
	}

	private static bool TryParseCancellationTag(string tag, out string gatheringId)
	{
		gatheringId = "";
		string text = (tag ?? "").Trim();
		const string prefix = "[ACTION:NOBLE_GATHERING:CANCEL:";
		if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !text.EndsWith("]", StringComparison.Ordinal))
		{
			return false;
		}
		string payload = text.Substring(prefix.Length, text.Length - prefix.Length - 1).Trim();
		int split = payload.IndexOf('=');
		if (split <= 0 || !string.Equals(payload.Substring(0, split).Trim(), "gathering", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		gatheringId = payload.Substring(split + 1).Trim();
		return !string.IsNullOrWhiteSpace(gatheringId)
			&& gatheringId.IndexOfAny(new[] { '[', ']', '\r', '\n', ';' }) < 0;
	}

	private static bool IsPlayerGatheringCancellationDelegate(Hero hero)
	{
		if (hero == null || hero == Hero.MainHero || hero.IsDead)
		{
			return false;
		}
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			bool isCompanion = hero.IsPlayerCompanion || (playerClan != null && hero.CompanionOf == playerClan);
			bool isPlayerGovernor = hero.GovernorOf?.Settlement?.OwnerClan == playerClan;
			return isCompanion || isPlayerGovernor;
		}
		catch
		{
			return false;
		}
	}

	private bool TryCreatePlayerHostedGatheringFromSelector(NobleGatheringInvitationSelector selector, Hero conversationHero, out string status, out string generatedFact)
	{
		status = "";
		generatedFact = "";
		Settlement settlement = ResolveSelectorSettlement(selector, conversationHero);
		if (!CanPlayerHostAtSettlement(Hero.MainHero, settlement, out status))
		{
			return false;
		}
		List<Hero> invitees = ResolveSelectorInvitees(selector, conversationHero)
			.Where(hero => IsHeroEligibleForGatheringInvitation(hero, out _))
			.GroupBy(hero => hero.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		if (invitees.Count == 0)
		{
			status = "宴会未召开：没有解析到可赴宴宾客。";
			return false;
		}
		if (!TryCreatePlayerHostedGathering(settlement, invitees, out status))
		{
			return false;
		}
		generatedFact = "[AFEF玩家行为补充] 玩家通过对话安排在" + GetSettlementName(settlement) + "举办宴会，并邀请了" + string.Join("、", invitees.Take(12).Select(GetHeroName)) + (invitees.Count > 12 ? "等" + invitees.Count + "人。" : "。");
		return true;
	}

	private static NobleGatheringInvitationSelector ParseInvitationSelectorTag(string tag)
	{
		string text = (tag ?? "").Trim();
		const string prefix = "[ACTION:NOBLE_GATHERING:START:";
		if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !text.EndsWith("]", StringComparison.Ordinal))
		{
			return null;
		}
		text = text.Substring(prefix.Length, text.Length - prefix.Length - 1);
		NobleGatheringInvitationSelector selector = new NobleGatheringInvitationSelector();
		foreach (string rawPart in text.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string part = (rawPart ?? "").Trim();
			if (string.IsNullOrWhiteSpace(part))
			{
				continue;
			}
			int split = part.IndexOf('=');
			if (split <= 0)
			{
				continue;
			}
			string key = part.Substring(0, split).Trim().ToLowerInvariant();
			string value = part.Substring(split + 1).Trim();
			if (string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			switch (key)
			{
				case "settlement":
				case "place":
				case "地点":
					selector.SettlementId = value;
					break;
				case "hero":
				case "heroes":
				case "persons":
				case "人物":
					selector.SpecificHeroIds.AddRange(SplitSelectorValues(value));
					break;
				case "clan":
				case "clans":
				case "family":
				case "families":
				case "家族":
					selector.ClanIds.AddRange(SplitSelectorValues(value));
					break;
				case "kingdom":
				case "kingdoms":
				case "faction":
				case "factions":
				case "王国":
				case "阵营":
					selector.KingdomIds.AddRange(SplitSelectorValues(value));
					break;
				case "culture":
				case "cultures":
				case "文化":
					selector.CultureIds.AddRange(SplitSelectorValues(value));
					break;
				case "age":
				case "年龄":
					ParseAgeRange(value, selector);
					break;
				case "gender":
				case "sex":
				case "性别":
					selector.Gender = NormalizeGenderToken(value);
					break;
			}
		}
		selector.SpecificHeroIds = NormalizeStringList(selector.SpecificHeroIds);
		selector.ClanIds = NormalizeStringList(selector.ClanIds);
		selector.KingdomIds = NormalizeStringList(selector.KingdomIds);
		selector.CultureIds = NormalizeStringList(selector.CultureIds);
		return selector;
	}

	private static List<string> SplitSelectorValues(string value)
	{
		return (value ?? "")
			.Split(new[] { ',', '，', '|', '/', '、' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(x => x.Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "none", StringComparison.OrdinalIgnoreCase) && !string.Equals(x, "无", StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	private static List<string> NormalizeStringList(IEnumerable<string> values)
	{
		return (values ?? Enumerable.Empty<string>())
			.Select(x => (x ?? "").Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static void ParseAgeRange(string value, NobleGatheringInvitationSelector selector)
	{
		if (selector == null)
		{
			return;
		}
		Match match = Regex.Match(value ?? "", "(\\d+)\\s*[-~至到]\\s*(\\d+)");
		if (match.Success && int.TryParse(match.Groups[1].Value, out int min) && int.TryParse(match.Groups[2].Value, out int max))
		{
			selector.MinAge = Math.Min(min, max);
			selector.MaxAge = Math.Max(min, max);
			return;
		}
		if (int.TryParse(Regex.Match(value ?? "", "\\d+").Value, out int age))
		{
			selector.MinAge = age;
			selector.MaxAge = age;
		}
	}

	private static string NormalizeGenderToken(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (text == "female" || text == "woman" || text == "women" || text == "f" || text.Contains("女"))
		{
			return "female";
		}
		if (text == "male" || text == "man" || text == "men" || text == "m" || text.Contains("男"))
		{
			return "male";
		}
		return "";
	}

	private static Settlement ResolveSelectorSettlement(NobleGatheringInvitationSelector selector, Hero conversationHero)
	{
		Settlement settlement = ResolveSettlementToken(selector?.SettlementId);
		if (settlement != null)
		{
			return settlement;
		}
		Settlement suggested = ResolveConversationSuggestedSettlement(conversationHero);
		return ResolveBestPlayerHostSettlement(suggested);
	}

	private static Settlement ResolveConversationSuggestedSettlement(Hero conversationHero)
	{
		try
		{
			return conversationHero?.GovernorOf?.Settlement
				?? conversationHero?.CurrentSettlement
				?? Settlement.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveSettlementToken(string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Settlement exact = Settlement.All?.FirstOrDefault(settlement => string.Equals(settlement?.StringId, text, StringComparison.OrdinalIgnoreCase));
		if (exact != null)
		{
			return exact;
		}
		List<Settlement> byName = Settlement.All?.Where(settlement => settlement != null && (string.Equals(settlement.Name?.ToString(), text, StringComparison.OrdinalIgnoreCase) || (settlement.Name?.ToString() ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList() ?? new List<Settlement>();
		return byName.Count == 1 ? byName[0] : null;
	}

	private IEnumerable<Hero> ResolveSelectorInvitees(NobleGatheringInvitationSelector selector, Hero conversationHero)
	{
		HashSet<Hero> result = new HashSet<Hero>();
		foreach (string token in selector?.SpecificHeroIds ?? new List<string>())
		{
			Hero hero = ResolveHeroToken(token);
			if (hero != null)
			{
				result.Add(hero);
			}
		}
		List<Clan> clans = (selector?.ClanIds ?? new List<string>()).Select(ResolveClanToken).Where(x => x != null).ToList();
		List<Kingdom> kingdoms = (selector?.KingdomIds ?? new List<string>()).Select(ResolveKingdomToken).Where(x => x != null).ToList();
		List<CultureObject> cultures = (selector?.CultureIds ?? new List<string>()).Select(ResolveCultureToken).Where(x => x != null).ToList();
		bool hasGroupFilter = clans.Count > 0 || kingdoms.Count > 0 || cultures.Count > 0 || (selector?.MinAge ?? -1) >= 0 || (selector?.MaxAge ?? -1) >= 0 || !string.IsNullOrWhiteSpace(selector?.Gender);
		if (hasGroupFilter)
		{
			foreach (Hero hero in Hero.AllAliveHeroes.Where(IsGatheringGroupCandidateHero))
			{
				if (clans.Count > 0 && !clans.Contains(hero.Clan))
				{
					continue;
				}
				if (kingdoms.Count > 0 && !kingdoms.Contains(hero.Clan?.Kingdom))
				{
					continue;
				}
				if (cultures.Count > 0 && !cultures.Contains(hero.Culture))
				{
					continue;
				}
				if ((selector?.MinAge ?? -1) >= 0 && hero.Age < selector.MinAge)
				{
					continue;
				}
				if ((selector?.MaxAge ?? -1) >= 0 && hero.Age > selector.MaxAge)
				{
					continue;
				}
				if (string.Equals(selector?.Gender, "female", StringComparison.OrdinalIgnoreCase) && !hero.IsFemale)
				{
					continue;
				}
				if (string.Equals(selector?.Gender, "male", StringComparison.OrdinalIgnoreCase) && hero.IsFemale)
				{
					continue;
				}
				result.Add(hero);
			}
		}
		if (result.Count == 0 && conversationHero != null && conversationHero != Hero.MainHero)
		{
			result.Add(conversationHero);
		}
		return result.Where(hero => hero != null && hero != Hero.MainHero && IsHeroAdultAndSpawnedForGathering(hero, out _));
	}

	private static bool IsHeroAdultAndSpawnedForGathering(Hero hero, out string reason)
	{
		reason = "";
		if (hero == null || hero.CharacterObject == null || hero.IsTemplate)
		{
			reason = "无效人物";
			return false;
		}
		try
		{
			if (hero.IsNotSpawned)
			{
				reason = "尚未出现在世界";
				return false;
			}
			if (hero.IsChild || hero.Age < 18f)
			{
				reason = "未满十八岁";
				return false;
			}
		}
		catch
		{
			reason = "未成年或未出世";
			return false;
		}
		return true;
	}

	private static bool IsGatheringGroupCandidateHero(Hero hero)
	{
		if (hero == null || hero == Hero.MainHero || hero.IsDead)
		{
			return false;
		}
		if (!IsHeroAdultAndSpawnedForGathering(hero, out _))
		{
			return false;
		}
		return hero.Occupation == Occupation.Lord || hero.IsPlayerCompanion || hero.IsWanderer || hero.Clan != null;
	}

	private static Hero ResolveHeroToken(string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Hero exact = Hero.AllAliveHeroes.FirstOrDefault(hero => string.Equals(hero?.StringId, text, StringComparison.OrdinalIgnoreCase));
		if (exact != null)
		{
			return exact;
		}
		List<Hero> byName = Hero.AllAliveHeroes.Where(hero => hero != null && (string.Equals(GetHeroName(hero), text, StringComparison.OrdinalIgnoreCase) || GetHeroName(hero).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
		return byName.Count == 1 ? byName[0] : null;
	}

	private static Clan ResolveClanToken(string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Clan exact = Clan.All.FirstOrDefault(clan => string.Equals(clan?.StringId, text, StringComparison.OrdinalIgnoreCase));
		if (exact != null)
		{
			return exact;
		}
		List<Clan> byName = Clan.All.Where(clan => clan != null && (string.Equals(GetClanName(clan), text, StringComparison.OrdinalIgnoreCase) || GetClanName(clan).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
		return byName.Count == 1 ? byName[0] : null;
	}

	private static Kingdom ResolveKingdomToken(string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Kingdom exact = Kingdom.All?.FirstOrDefault(kingdom => string.Equals(kingdom?.StringId, text, StringComparison.OrdinalIgnoreCase));
		if (exact != null)
		{
			return exact;
		}
		List<Kingdom> byName = Kingdom.All?.Where(kingdom => kingdom != null && (string.Equals(kingdom.Name?.ToString(), text, StringComparison.OrdinalIgnoreCase) || (kingdom.Name?.ToString() ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList() ?? new List<Kingdom>();
		return byName.Count == 1 ? byName[0] : null;
	}

	private static CultureObject ResolveCultureToken(string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		IEnumerable<CultureObject> cultures = MBObjectManager.Instance.GetObjectTypeList<CultureObject>() ?? Enumerable.Empty<CultureObject>();
		CultureObject exact = cultures.FirstOrDefault(culture => string.Equals(culture?.StringId, text, StringComparison.OrdinalIgnoreCase));
		if (exact != null)
		{
			return exact;
		}
		List<CultureObject> byName = cultures.Where(culture => culture != null && (string.Equals(culture.Name?.ToString(), text, StringComparison.OrdinalIgnoreCase) || (culture.Name?.ToString() ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
		return byName.Count == 1 ? byName[0] : null;
	}

	private bool CanPlayerHostAtSettlement(Hero host, Settlement settlement, out string reason)
	{
		return CanPlayerHostAtSettlement(host, settlement, NobleGatheringOptions.Capture(), out reason);
	}

	private bool CanPlayerHostAtSettlement(Hero host, Settlement settlement, NobleGatheringOptions options, out string reason)
	{
		reason = "";
		if (!options.Enabled)
		{
			reason = "宴会无法召开：宴会功能已在 MCM 中关闭。";
			return false;
		}
		if (host == null || settlement == null || settlement.Town == null)
		{
			reason = "宴会无法召开：必须选择有效城镇或城堡。";
			return false;
		}
		if (host.Clan == null || (host.Clan.Leader != host && !host.IsClanLeader))
		{
			reason = "宴会无法召开：只有家族族长能够作为宴会主人。";
			return false;
		}
		if (settlement.OwnerClan != Clan.PlayerClan)
		{
			reason = "宴会无法召开：主办地必须是你自己家族拥有的定居点。";
			return false;
		}
		if (settlement.IsUnderSiege)
		{
			reason = "宴会无法召开：该定居点正在被围攻。";
			return false;
		}
		if (host.Gold < options.Cost)
		{
			reason = "宴会无法召开：你需要 " + options.Cost + " 第纳尔。";
			return false;
		}
		double now = NowDay();
		if (_playerHostCooldowns.TryGetValue(PlayerHostCooldownKey, out double until) && now < until)
		{
			reason = "宴会筹备尚在冷却中，还需要约 " + Math.Ceiling(until - now) + " 天。";
			return false;
		}
		if (_gatherings.Values.Any(record => record != null && record.IsPlayerHosted && string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase)))
		{
			reason = "宴会无法召开：你已经有一场正在进行的宴会。";
			return false;
		}
		if (_gatherings.Values.Any(record => record != null && string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase) && string.Equals(record.SettlementId, settlement.StringId, StringComparison.OrdinalIgnoreCase)))
		{
			reason = "宴会无法召开：该定居点已有正在进行的宴会。";
			return false;
		}
		return true;
	}

	private static IEnumerable<Settlement> GetPlayerHostSettlements(Settlement suggestedSettlement)
	{
		return Settlement.All
			.Where(settlement => settlement != null && settlement.Town != null && settlement.OwnerClan == Clan.PlayerClan)
			.OrderBy(settlement => settlement == suggestedSettlement ? 0 : 1)
			.ThenBy(settlement => settlement.Name?.ToString() ?? settlement.StringId ?? "");
	}

	private static IEnumerable<Clan> GetPlayerGatheringCandidateClans(Settlement settlement)
	{
		Kingdom kingdom = Clan.PlayerClan?.Kingdom;
		return Clan.All
			.Where(clan => clan != null && !clan.IsEliminated && !clan.IsBanditFaction && !clan.IsMinorFaction)
			.Where(clan => kingdom == null || clan.Kingdom == kingdom)
			.OrderBy(clan => clan.Name?.ToString() ?? "");
	}

	private static IEnumerable<Hero> GetPlayerGatheringCandidateHeroes(List<string> selectedClanIds)
	{
		HashSet<string> ids = new HashSet<string>(selectedClanIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		return Hero.AllAliveHeroes
			.Where(hero => hero != null && hero != Hero.MainHero && hero.Clan != null && ids.Contains(hero.Clan.StringId ?? ""))
			.Where(hero => IsHeroAdultAndSpawnedForGathering(hero, out _))
			.Where(hero => hero.Occupation == Occupation.Lord)
			.OrderBy(hero => GetClanName(hero.Clan))
			.ThenBy(hero => GetHeroName(hero));
	}

	private bool IsHeroEligibleForGatheringInvitation(Hero hero, out string reason)
	{
		reason = "";
		if (hero == null || hero == Hero.MainHero || hero.IsDead)
		{
			reason = "无效人物";
			return false;
		}
		if (hero.IsPrisoner)
		{
			reason = "被俘";
			return false;
		}
		if (hero.IsWounded)
		{
			reason = "重伤";
			return false;
		}
		if (!IsHeroAdultAndSpawnedForGathering(hero, out reason))
		{
			return false;
		}
		if (CanUseExistingPartyForGatheringTravel(hero, out _))
		{
			return true;
		}
		if (CanUseDelayedSettlementTravelForGathering(hero, out reason))
		{
			return true;
		}
		return false;
	}

	private bool CanUseExistingPartyForGatheringTravel(Hero hero, out string reason)
	{
		reason = "";
		if (hero == null || hero == Hero.MainHero || hero.IsDead)
		{
			reason = "无效人物";
			return false;
		}
		if (!IsHeroAdultAndSpawnedForGathering(hero, out reason))
		{
			return false;
		}
		if (hero.IsPrisoner)
		{
			reason = "被俘";
			return false;
		}
		if (hero.IsWounded)
		{
			reason = "重伤";
			return false;
		}
		MobileParty party = hero.PartyBelongedTo;
		if (party == null || party.LeaderHero != hero || !party.IsActive)
		{
			reason = "没有独立部队";
			return false;
		}
		if (party.Army != null && party.Army.LeaderParty != party)
		{
			reason = "正在军团中";
			return false;
		}
		if (party.MapEvent != null || party.SiegeEvent != null)
		{
			reason = "正在战斗或围城";
			return false;
		}
		return true;
	}

	private bool CanUseDelayedSettlementTravelForGathering(Hero hero, out string reason)
	{
		reason = "";
		if (hero == null || hero == Hero.MainHero || hero.IsDead)
		{
			reason = "无效人物";
			return false;
		}
		if (hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
		{
			reason = "被俘";
			return false;
		}
		if (hero.IsWounded)
		{
			reason = "重伤";
			return false;
		}
		if (!IsHeroAdultAndSpawnedForGathering(hero, out reason))
		{
			return false;
		}
		if (hero.PartyBelongedTo != null)
		{
			reason = "已有非独立部队或正处于部队中";
			return false;
		}
		if (TryGetHeroTeleportTarget(hero, out Settlement target))
		{
			reason = "正在旅行至" + GetSettlementName(target);
			return false;
		}
		if (hero.IsTraveling)
		{
			reason = "正在旅行";
			return false;
		}
		if (ResolveHeroOriginSettlement(hero) == null)
		{
			reason = "找不到原始所在城";
			return false;
		}
		if (!CanHeroMoveToSettlement(hero, out reason))
		{
			return false;
		}
		return true;
	}

	private static Settlement ResolveHeroOriginSettlement(Hero hero)
	{
		if (hero == null)
		{
			return null;
		}
		return hero.CurrentSettlement
			?? hero.GovernorOf?.Settlement
			?? ResolveBestHeroHomeSettlement(hero);
	}

	private static Settlement ResolveBestHeroHomeSettlement(Hero hero)
	{
		try
		{
			return hero?.HomeSettlement
				?? hero?.Clan?.HomeSettlement
				?? hero?.Clan?.Fiefs?.FirstOrDefault()?.Settlement
				?? hero?.Clan?.Settlements?.FirstOrDefault(settlement => settlement?.Town != null)
				?? Settlement.All?.Where(settlement => settlement?.Town != null && settlement.OwnerClan == hero?.Clan).OrderBy(settlement => settlement.Name?.ToString() ?? settlement.StringId ?? "").FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveBestPlayerHostSettlement(Settlement suggestedSettlement)
	{
		return GetPlayerHostSettlements(suggestedSettlement).FirstOrDefault();
	}

	private static bool IsHeroAtSettlement(Hero hero, Settlement settlement)
	{
		try
		{
			if (hero?.CurrentSettlement == settlement)
			{
				return true;
			}
			MobileParty party = hero?.PartyBelongedTo;
			if (party == null || settlement == null)
			{
				return false;
			}
			if (party.CurrentSettlement == settlement)
			{
				return true;
			}
			return party.Position.Distance(settlement.GatePosition) <= ArrivalDistance;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerAtSettlement(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return false;
			}
			if (Settlement.CurrentSettlement == settlement || Hero.MainHero?.CurrentSettlement == settlement)
			{
				return true;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return false;
			}
			if (mainParty.CurrentSettlement == settlement)
			{
				return true;
			}
			return mainParty.Position.Distance(settlement.GatePosition) <= ArrivalDistance;
		}
		catch
		{
			return false;
		}
	}

	private bool TryResolveGovernorOwnedSettlement(Hero governor, out Settlement settlement, out string reason)
	{
		settlement = null;
		reason = "";
		if (governor == null)
		{
			reason = "宴会无法召开：当前对话对象不是有效总督。";
			return false;
		}
		Town town = governor.GovernorOf;
		if (town?.Settlement == null)
		{
			reason = "宴会无法召开：当前对话对象不是总督。";
			return false;
		}
		settlement = town.Settlement;
		if (settlement.OwnerClan != Clan.PlayerClan)
		{
			reason = "宴会无法召开：该总督管理的定居点不属于你的家族。";
			return false;
		}
		return true;
	}

	private bool TryPickNpcHostSettlement(Hero host, out Settlement settlement)
	{
		settlement = null;
		if (host?.Clan == null)
		{
			return false;
		}
		List<Settlement> options = Settlement.All
			.Where(s => s != null && s.Town != null && s.OwnerClan == host.Clan && !s.IsUnderSiege)
			.OrderBy(_ => MBRandom.RandomFloat)
			.ToList();
		settlement = options.FirstOrDefault();
		return settlement != null;
	}

	private List<Hero> PickNpcInvitees(Hero host, Settlement settlement, NobleGatheringOptions options)
	{
		Kingdom kingdom = host?.Clan?.Kingdom;
		// This governor switch only affects NPC-generated guest lists. Player-hosted
		// gatherings use a separate candidate path and always keep governors eligible.
		List<Hero> result = Hero.AllAliveHeroes
			.Where(hero => hero != null && hero != host && hero.Occupation == Occupation.Lord && hero.Clan?.Kingdom == kingdom)
			.Where(hero => hero != Hero.MainHero || options.AllowNpcPlayerInvitations)
			.Where(hero => hero == Hero.MainHero || options.AllowNpcGovernorInvitations || hero.GovernorOf == null)
			.Where(hero => hero == Hero.MainHero || IsHeroEligibleForGatheringInvitation(hero, out _))
			.Where(hero => hero == Hero.MainHero || ShouldNpcConsiderInviting(host, hero))
			.OrderByDescending(hero => hero == Hero.MainHero ? host.GetRelation(hero) : host.GetRelation(hero))
			.ThenBy(_ => MBRandom.RandomFloat)
			.Take(16)
			.ToList();
		if (options.AllowNpcPlayerInvitations
			&& kingdom == Clan.PlayerClan?.Kingdom
			&& Hero.MainHero != null
			&& Hero.MainHero != host
			&& !result.Contains(Hero.MainHero)
			&& host.GetRelation(Hero.MainHero) >= 10)
		{
			result.Insert(0, Hero.MainHero);
		}
		return result;
	}

	private static bool ShouldNpcConsiderInviting(Hero host, Hero guest)
	{
		try
		{
			int relation = host.GetRelation(guest);
			return relation >= 10 || MBRandom.RandomFloat < Math.Max(0.05f, (relation + 20) / 100f);
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldNpcAcceptInvitation(Hero host, Hero guest)
	{
		try
		{
			int relation = host.GetRelation(guest);
			float chance = 0.35f + relation / 100f;
			return MBRandom.RandomFloat < MBMath.ClampFloat(chance, 0.05f, 0.95f);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsKingdomMostlyPeaceful(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && !kingdom.IsEliminated && !Kingdom.All.Any(other => other != null && other != kingdom && !other.IsEliminated && kingdom.IsAtWarWith(other));
		}
		catch
		{
			return false;
		}
	}

	private bool HasActiveGatheringForHost(Hero host)
	{
		string id = host?.StringId ?? "";
		return !string.IsNullOrWhiteSpace(id) && _gatherings.Values.Any(record => record != null && string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase) && string.Equals(record.HostHeroId, id, StringComparison.OrdinalIgnoreCase));
	}

	private bool HasActiveGatheringForKingdom(Kingdom kingdom)
	{
		string id = (kingdom?.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(id)
			&& _gatherings.Values.Any(record => record != null
				&& string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase)
				&& string.Equals((record.KingdomId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsPartyTargetingGatheringSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			return party != null
				&& settlement != null
				&& (party.CurrentSettlement == settlement
					|| party.TargetSettlement == settlement
					|| party.ShortTermTargetSettlement == settlement);
		}
		catch
		{
			return false;
		}
	}

	private List<NobleGatheringRecord> GetCancellableGatheringsForConversationHero(Hero conversationHero)
	{
		if (conversationHero == null || conversationHero == Hero.MainHero)
		{
			return new List<NobleGatheringRecord>();
		}
		bool canExecutePlayerOrder = IsPlayerGatheringCancellationDelegate(conversationHero);
		string heroId = (conversationHero.StringId ?? "").Trim();
		return _gatherings.Values
			.Where(record => record != null && string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase))
			.Where(record => (record.IsPlayerHosted && canExecutePlayerOrder)
				|| (!record.IsPlayerHosted && string.Equals(record.HostHeroId, heroId, StringComparison.OrdinalIgnoreCase)))
			.OrderBy(record => record.EndDay)
			.ToList();
	}

	private NobleGatheringRecord FindActiveGatheringForHero(Hero hero)
	{
		string heroId = (hero?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return null;
		}
		double now = NowDay();
		if (_heroActiveFeastId.TryGetValue(heroId, out string gatheringId)
			&& _gatherings.TryGetValue(gatheringId, out NobleGatheringRecord indexed)
			&& indexed != null
			&& string.Equals(indexed.State, StateActive, StringComparison.OrdinalIgnoreCase)
			&& now < indexed.EndDay)
		{
			return indexed;
		}
		NobleGatheringRecord found = _gatherings.Values.FirstOrDefault(record =>
			record != null
			&& string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase)
			&& now < record.EndDay
			&& (string.Equals(record.HostHeroId, heroId, StringComparison.OrdinalIgnoreCase)
				|| (record.Invitees ?? new List<NobleGatheringInviteeRecord>()).Any(invitee =>
					invitee != null
					&& string.Equals(invitee.HeroId, heroId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(invitee.Status, InviteArrived, StringComparison.OrdinalIgnoreCase))
				|| (hero == Hero.MainHero
					&& string.Equals(record.PlayerInvitationStatus, PlayerInvitationArrived, StringComparison.OrdinalIgnoreCase))));
		if (found != null)
		{
			RegisterFeastAttendee(hero, found);
		}
		return found;
	}

	private static Hero ResolveConversationHero()
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
			CharacterObject character = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter ?? CharacterObject.OneToOneConversationCharacter;
			return character?.HeroObject;
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveHeroById(string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return Hero.AllAliveHeroes.FirstOrDefault(hero => string.Equals(hero?.StringId, id, StringComparison.OrdinalIgnoreCase));
	}

	private static Clan ResolveClanById(string clanId)
	{
		string id = (clanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return Clan.All.FirstOrDefault(clan => string.Equals(clan?.StringId, id, StringComparison.OrdinalIgnoreCase));
	}

	private static Settlement ResolveSettlementById(string settlementId)
	{
		string id = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return Settlement.All.FirstOrDefault(settlement => string.Equals(settlement?.StringId, id, StringComparison.OrdinalIgnoreCase));
	}

	private static string GenerateGatheringId()
	{
		return "NG" + Guid.NewGuid().ToString("N").Substring(0, 12);
	}

	private static string BuildCommandSourceId(NobleGatheringRecord record)
	{
		return "noble_gathering:" + (record?.Id ?? "");
	}

	private static bool IsInviteAcceptedStatus(string status)
	{
		return string.Equals(status, InviteAccepted, StringComparison.OrdinalIgnoreCase);
	}

	private static List<string> NormalizeHeroIds(IEnumerable<string> heroIds)
	{
		return (heroIds ?? Enumerable.Empty<string>())
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static int CountSelectedGuestsForClan(List<string> heroIds, Clan clan)
	{
		if (clan == null)
		{
			return 0;
		}
		return NormalizeHeroIds(heroIds)
			.Select(ResolveHeroById)
			.Count(hero => hero?.Clan == clan);
	}

	private static string BuildSelectedGuestHint(List<string> heroIds)
	{
		List<Hero> heroes = NormalizeHeroIds(heroIds)
			.Select(ResolveHeroById)
			.Where(hero => hero != null)
			.ToList();
		if (heroes.Count == 0)
		{
			return "尚未选择宾客。";
		}
		string names = string.Join("、", heroes.Take(12).Select(hero => GetHeroName(hero) + "（" + BuildGatheringHeroIdentityLabel(hero) + "）"));
		return heroes.Count > 12 ? "已选：" + names + " 等 " + heroes.Count + " 人。" : "已选：" + names;
	}

	private void RecordGatheringStartedWeeklyMaterial(NobleGatheringRecord record)
	{
		if (record == null || record.WeeklyStartMaterialRecorded)
		{
			return;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		Kingdom kingdom = ResolveKingdomToken(record.KingdomId) ?? host?.Clan?.Kingdom;
		if (host == null || settlement == null || kingdom == null)
		{
			return;
		}
		List<Hero> acceptedGuests = (record.Invitees ?? new List<NobleGatheringInviteeRecord>())
			.Where(invitee => invitee != null
				&& (string.Equals(invitee.Status, InviteAccepted, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(invitee.Status, InviteArrived, StringComparison.OrdinalIgnoreCase)))
			.Select(invitee => ResolveHeroById(invitee.HeroId))
			.Where(hero => hero != null)
			.ToList();
		if (!record.IsPlayerHosted
			&& (string.Equals(record.PlayerInvitationStatus, PlayerInvitationInvited, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.PlayerInvitationStatus, PlayerInvitationArrived, StringComparison.OrdinalIgnoreCase))
			&& Hero.MainHero != null)
		{
			acceptedGuests.Add(Hero.MainHero);
		}
		List<string> notableGuests = acceptedGuests
			.Where(IsWeeklyNotableNoble)
			.Select(hero => GetHeroName(hero) + "（" + GetWeeklyNobleRole(hero) + "）")
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(12)
			.ToList();
		StringBuilder snapshot = new StringBuilder();
		snapshot.Append(GetHeroName(host))
			.Append("（").Append(GetWeeklyNobleRole(host)).Append("）在")
			.Append(GetSettlementName(settlement))
			.Append("举办了一场贵族宴会，宴会计划持续")
			.Append(GetRecordDurationDays(record))
			.Append("天。");
		if (notableGuests.Count > 0)
		{
			snapshot.Append(" 已接受邀请的重要宾客包括：").Append(string.Join("、", notableGuests)).Append("。");
		}
		snapshot.Append(" 这是已经实际发出邀请并开始执行的宴会事件，不是未经证实的计划；尚未抵达的宾客只能写作接受邀请或准备赴宴。");
		MyBehavior.RecordNobleGatheringWeeklyMaterialForExternal(
			"noble_gathering:" + record.Id + ":started",
			"贵族宴会举办 - " + GetHeroName(host),
			snapshot.ToString(),
			kingdom.StringId ?? "",
			settlement.StringId ?? "",
			host.StringId ?? "",
			IsKingdomLeaderForWeeklyMaterial(host, kingdom));
		record.WeeklyStartMaterialRecorded = true;
	}

	private void RecordNotableGatheringAttendanceWeeklyMaterial(NobleGatheringRecord record, Hero attendee)
	{
		if (record == null || attendee == null || !IsWeeklyNotableNoble(attendee))
		{
			return;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		Kingdom kingdom = ResolveKingdomToken(record.KingdomId) ?? host?.Clan?.Kingdom;
		if (host == null || settlement == null || kingdom == null)
		{
			return;
		}
		string snapshot = GetHeroName(attendee) + "（" + GetWeeklyNobleRole(attendee) + "）已经实际抵达"
			+ GetSettlementName(settlement) + "，参加了" + GetHeroName(host) + "举办的贵族宴会。"
			+ "这是一项已经发生的出席事实，可以写入本周王国纪事。";
		bool includeInWorld = IsKingdomLeaderForWeeklyMaterial(host, kingdom)
			|| IsKingdomLeaderForWeeklyMaterial(attendee, attendee.Clan?.Kingdom);
		MyBehavior.RecordNobleGatheringWeeklyMaterialForExternal(
			"noble_gathering:" + record.Id + ":attendance:" + (attendee.StringId ?? ""),
			"重要贵族赴宴 - " + GetHeroName(attendee),
			snapshot,
			kingdom.StringId ?? "",
			settlement.StringId ?? "",
			attendee.StringId ?? "",
			includeInWorld);
	}

	private void RecordGatheringCancelledWeeklyMaterial(NobleGatheringRecord record, string reason)
	{
		Hero host = ResolveHeroById(record?.HostHeroId);
		Settlement settlement = ResolveSettlementById(record?.SettlementId);
		Kingdom kingdom = ResolveKingdomToken(record?.KingdomId) ?? host?.Clan?.Kingdom;
		if (record == null || host == null || settlement == null || kingdom == null)
		{
			return;
		}
		string snapshot = GetHeroName(host) + "取消了自己在" + GetSettlementName(settlement) + "举办的贵族宴会，宾客已经散去。"
			+ BuildEndReasonSuffix(reason);
		MyBehavior.RecordNobleGatheringWeeklyMaterialForExternal(
			"noble_gathering:" + record.Id + ":cancelled",
			"贵族宴会取消 - " + GetHeroName(host),
			snapshot,
			kingdom.StringId ?? "",
			settlement.StringId ?? "",
			host.StringId ?? "",
			IsKingdomLeaderForWeeklyMaterial(host, kingdom));
	}

	private static bool IsWeeklyNotableNoble(Hero hero)
	{
		return hero != null && (hero.IsClanLeader || hero.Clan?.Leader == hero || hero.Clan?.Kingdom?.Leader == hero);
	}

	private static bool IsKingdomLeaderForWeeklyMaterial(Hero hero, Kingdom kingdom)
	{
		return hero != null && kingdom != null && kingdom.Leader == hero;
	}

	private static string GetWeeklyNobleRole(Hero hero)
	{
		if (hero?.Clan?.Kingdom?.Leader == hero)
		{
			return "国王";
		}
		if (hero?.Clan?.Leader == hero || hero?.IsClanLeader == true)
		{
			return "家族族长";
		}
		return "贵族";
	}

	private static double NowDay()
	{
		try
		{
			return CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0.0;
		}
	}

	private static string GetHeroName(Hero hero)
	{
		return hero?.Name?.ToString() ?? "未知贵族";
	}

	private static string BuildGatheringHeroIdentityLabel(Hero hero)
	{
		if (hero == null)
		{
			return "身份未知";
		}
		List<string> identities = new List<string>();
		if (hero.GovernorOf != null)
		{
			identities.Add("总督");
		}
		if (hero.Clan?.Kingdom?.Leader == hero)
		{
			identities.Add("君主");
		}
		if (hero.Clan?.Leader == hero || hero.IsClanLeader)
		{
			identities.Add("族长");
		}
		if (hero.IsFemale)
		{
			identities.Add("女士");
		}
		if (identities.Count == 0)
		{
			identities.Add("领主");
		}
		return string.Join("、", identities);
	}

	private static string BuildGatheringHeroIdentityHint(Hero hero)
	{
		string hint = "身份：" + BuildGatheringHeroIdentityLabel(hero);
		Settlement governedSettlement = hero?.GovernorOf?.Settlement;
		if (governedSettlement != null)
		{
			hint += "\n管辖：" + GetSettlementName(governedSettlement);
		}
		return hint;
	}

	private static string GetClanName(Clan clan)
	{
		return clan?.Name?.ToString() ?? "未知家族";
	}

	private static string GetSettlementName(Settlement settlement)
	{
		return settlement?.Name?.ToString() ?? "未知定居点";
	}

	private static string BuildClanHint(Clan clan)
	{
		int members = Hero.AllAliveHeroes.Count(hero => hero?.Clan == clan && hero.Occupation == Occupation.Lord);
		int movable = Hero.AllAliveHeroes.Count(hero => hero?.Clan == clan && hero.Occupation == Occupation.Lord && hero != Hero.MainHero && hero.PartyBelongedTo?.LeaderHero == hero);
		int settlementTravel = 0;
		try
		{
			NobleGatheringBehavior behavior = Instance;
			if (behavior != null)
			{
				settlementTravel = Hero.AllAliveHeroes.Count(hero => hero?.Clan == clan && hero.Occupation == Occupation.Lord && hero != Hero.MainHero && hero.PartyBelongedTo == null && behavior.CanUseDelayedSettlementTravelForGathering(hero, out _));
			}
		}
		catch
		{
			settlementTravel = 0;
		}
		return "成员 " + members + " 人；有独立部队 " + movable + " 人；可原版旅行赴宴 " + settlementTravel + " 人。";
	}

	private static string BuildGatheringEndMessage(NobleGatheringRecord record, string reason)
	{
		Settlement settlement = ResolveSettlementById(record?.SettlementId);
		Hero host = ResolveHeroById(record?.HostHeroId);
		string place = GetSettlementName(settlement);
		bool cancelled = string.Equals(record?.State, StateCancelled, StringComparison.OrdinalIgnoreCase);
		if (record?.IsPlayerHosted == true)
		{
			return cancelled
				? "宴会已取消：你在" + place + "举办的宴会已经散场。" + BuildEndReasonSuffix(reason)
				: "宴会已结束：你在" + place + "举办的宴会已经散场。";
		}
		return cancelled
			? "宴会已取消：" + GetHeroName(host) + "在" + place + "举办的宴会已经散场。" + BuildEndReasonSuffix(reason)
			: "宴会已结束：" + GetHeroName(host) + "在" + place + "举办的宴会已经散场。";
	}

	private static string BuildEndReasonSuffix(string reason)
	{
		string text = (reason ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "" : " 原因：" + text;
	}

	private static int GetRecordDurationDays(NobleGatheringRecord record)
	{
		double duration = (record?.EndDay ?? 0.0) - (record?.StartDay ?? 0.0);
		if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0.0)
		{
			return DuelSettings.DefaultNobleGatheringDurationDays;
		}
		return Math.Max(1, (int)Math.Ceiling(duration));
	}

	private static int GetRecordInvitedClanRelationReward(NobleGatheringRecord record)
	{
		int reward = record?.InvitedClanRelationReward ?? -1;
		if (reward < 0)
		{
			reward = DuelSettings.DefaultNobleGatheringInvitedClanRelationReward;
		}
		return Math.Max(
			DuelSettings.NobleGatheringInvitedClanRelationRewardMinimum,
			Math.Min(DuelSettings.NobleGatheringInvitedClanRelationRewardMaximum, reward));
	}

	private static void NormalizeRecord(NobleGatheringRecord record)
	{
		if (record == null)
		{
			return;
		}
		record.Id = (record.Id ?? "").Trim();
		record.HostHeroId = (record.HostHeroId ?? "").Trim();
		record.HostClanId = (record.HostClanId ?? "").Trim();
		record.KingdomId = (record.KingdomId ?? "").Trim();
		record.SettlementId = (record.SettlementId ?? "").Trim();
		record.State = string.IsNullOrWhiteSpace(record.State) ? StateActive : record.State.Trim();
		record.PlayerInvitationStatus = (record.PlayerInvitationStatus ?? "").Trim();
		bool legacyPending = string.Equals(record.PlayerInvitationStatus, LegacyPlayerInvitationPending, StringComparison.OrdinalIgnoreCase);
		bool legacyAccepted = string.Equals(record.PlayerInvitationStatus, LegacyPlayerInvitationAccepted, StringComparison.OrdinalIgnoreCase);
		bool legacyDeclined = string.Equals(record.PlayerInvitationStatus, LegacyPlayerInvitationDeclined, StringComparison.OrdinalIgnoreCase);
		if (!record.IsPlayerHosted && (legacyPending || legacyAccepted || legacyDeclined))
		{
			record.PlayerInvitationStatus = PlayerInvitationInvited;
			if (legacyPending || legacyDeclined)
			{
				record.PlayerInvitationCourierSent = false;
				record.PlayerInvitationCourierNextRetryDay = 0.0;
			}
			else if (!record.PlayerInvitationCourierSent && record.PlayerInvitationNoticeShown)
			{
				// Legacy "Accepted + notice shown" normally means the courier was already created.
				record.PlayerInvitationCourierSent = true;
			}
		}
		if (record.PlayerInvitationCourierNextRetryDay < -1.0)
		{
			record.PlayerInvitationCourierNextRetryDay = -1.0;
		}
		record.HostOriginSettlementId = (record.HostOriginSettlementId ?? "").Trim();
		record.HostSettlementReturnState = (record.HostSettlementReturnState ?? "").Trim();
		record.HostTemporaryPartyId = (record.HostTemporaryPartyId ?? "").Trim();
		record.HostTemporaryPartyPhase = (record.HostTemporaryPartyPhase ?? "").Trim();
		record.CrisisDecisionLevel = Math.Max(CrisisDecisionNone, Math.Min(CrisisDecisionSiege, record.CrisisDecisionLevel));
		record.EndReason = (record.EndReason ?? "").Trim();
		record.InvitedClanRelationReward = GetRecordInvitedClanRelationReward(record);
		record.RelationRewardedClanIds = (record.RelationRewardedClanIds ?? new List<string>())
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		HashSet<string> rewardedClanIds = new HashSet<string>(record.RelationRewardedClanIds, StringComparer.OrdinalIgnoreCase);
		record.Invitees ??= new List<NobleGatheringInviteeRecord>();
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees)
		{
			if (invitee == null)
			{
				continue;
			}
			invitee.HeroId = (invitee.HeroId ?? "").Trim();
			invitee.ClanId = (invitee.ClanId ?? "").Trim();
			invitee.Status = string.IsNullOrWhiteSpace(invitee.Status) ? InvitePending : invitee.Status.Trim();
			invitee.Reason = (invitee.Reason ?? "").Trim();
			invitee.OriginSettlementId = (invitee.OriginSettlementId ?? "").Trim();
			invitee.SettlementReturnState = (invitee.SettlementReturnState ?? "").Trim();
			invitee.TemporaryPartyId = (invitee.TemporaryPartyId ?? "").Trim();
			invitee.TemporaryPartyPhase = (invitee.TemporaryPartyPhase ?? "").Trim();
			if (invitee.RelationRewardApplied && !string.IsNullOrWhiteSpace(invitee.ClanId) && rewardedClanIds.Add(invitee.ClanId))
			{
				record.RelationRewardedClanIds.Add(invitee.ClanId);
			}
			if (!invitee.RelationRewardApplied && !string.IsNullOrWhiteSpace(invitee.ClanId) && rewardedClanIds.Contains(invitee.ClanId))
			{
				invitee.RelationRewardApplied = true;
			}
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("debug_npc_feast", "AnimusForge")]
	public static string CommandDebugNpcFeast(List<string> args)
	{
		try
		{
			NobleGatheringBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			if (behavior == null)
			{
				return "NobleGatheringBehavior not initialized.";
			}
			bool ok = behavior.TryCreateNpcHostedGathering(force: true, out string status);
			return ok ? ("NPC noble gathering created: " + status) : ("NPC noble gathering not created: " + status);
		}
		catch (Exception ex)
		{
			return "NPC noble gathering debug failed: " + ex.Message;
		}
	}

	public sealed class NobleGatheringSaveableTypeDefiner : SaveableTypeDefiner
	{
		public NobleGatheringSaveableTypeDefiner()
			: base(711090)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(NobleGatheringTemporaryPartyComponent), 1);
		}
	}

	private sealed class NobleGatheringTemporaryPartyComponent : PartyComponent
	{
		private readonly CampaignVec2 _position;

		private readonly TextObject _name;

		private Hero _owner;

		private Clan _clan;

		private Hero _leader;

		public NobleGatheringTemporaryPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
		{
			_position = position;
			_name = name;
			_owner = owner;
			_leader = owner;
			_clan = clan;
		}

		public override Hero PartyOwner => _owner;

		public override Hero Leader => _leader;

		public override TextObject Name => _name;

		public override Settlement HomeSettlement => ResolveBestHeroHomeSettlement(_owner);

		public override bool AvoidHostileActions => true;

		public override Banner GetDefaultComponentBanner()
		{
			return _clan?.Banner;
		}

		protected override void OnInitialize()
		{
			MobileParty.ActualClan = _clan;
			MobileParty.InitializeMobilePartyAroundPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), _position, 0f, 0f, false);
			MobileParty.SetLandNavigationAccess(true);
			MobileParty.IsCurrentlyAtSea = false;
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

	private static void DisplayGatheringMessage(string text, Color color)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(text, color));
		}
		catch
		{
		}
	}

	private static void ShowMessage(string text)
	{
		DisplayGatheringMessage(text, new Color(0.8f, 0.95f, 1f));
	}

	private static void Log(string text)
	{
		Logger.Log(LogSource, text ?? "");
	}
}
