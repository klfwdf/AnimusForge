using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SandBox;
using SandBox.Objects;
using SandBox.Objects.Usables;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

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

	public bool PlayerAttendanceRewardApplied { get; set; }

	public double PlayerArrivalDay { get; set; } = -1.0;

	public bool HostCommandIssued { get; set; }

	public List<NobleGatheringInviteeRecord> Invitees { get; set; } = new List<NobleGatheringInviteeRecord>();
}

internal sealed class NobleGatheringInvitationMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string GatheringId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public NobleGatheringInvitationMapNotification(string gatheringId, string titleText, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "有贵族邀请你赴宴。" : descriptionText))
	{
		GatheringId = (gatheringId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(titleText) ? "宴会邀请" : titleText);
	}

	public override bool IsValid()
	{
		return NobleGatheringBehavior.Instance?.HasPendingPlayerInvitation(GatheringId) == true;
	}
}

internal sealed class NobleGatheringInvitationMapNotificationItemVM : MapNotificationItemBaseVM
{
	public NobleGatheringInvitationMapNotificationItemVM(NobleGatheringInvitationMapNotification data)
		: base(data)
	{
		NotificationIdentifier = "af_noble_gathering";
		_onInspect = delegate
		{
			if (NobleGatheringBehavior.Instance?.OpenPlayerInvitationFromMap(data.GatheringId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal sealed class NobleGatheringBehavior : CampaignBehaviorBase
{
	private const string LogSource = "NobleGathering";
	private const string SaveKeyGatherings = "_afNobleGatherings_v1";
	private const string SaveKeyPlayerHostCooldowns = "_afNobleGatheringPlayerHostCooldowns_v1";
	private const int GatheringCost = 50000;
	private const int GatheringDurationDays = 5;
	private const int PlayerHostCooldownDays = 10;
	private const float ArrivalDistance = 3.0f;
	private const string StateActive = "Active";
	private const string StateFinished = "Finished";
	private const string InvitePending = "Pending";
	private const string InviteAccepted = "Accepted";
	private const string InviteDeclined = "Declined";
	private const string InviteArrived = "Arrived";
	private const string InviteFailed = "Failed";
	private const string PlayerInvitationPending = "Pending";
	private const string PlayerInvitationAccepted = "Accepted";
	private const string PlayerInvitationDeclined = "Declined";
	private const string PlayerInvitationArrived = "Arrived";
	private const string PlayerHostCooldownKey = "player";
	private const string LordHallLocationId = "lordshall";
	private const string TavernWenchSpawnTag = "sp_tavern_wench";
	private const string MusicianSpawnTag = "musician";
	private const string FeastWenchDisplayName = "侍女";
	private const int MaxFeastWenches = 2;
	private const int MaxFeastMusicians = 3;
	private const int FeastMusicGapSeconds = 8;
	private const int FeastMusicianPerformanceRefreshMs = 900;
	private static readonly FieldInfo AgentNameField = typeof(Agent).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly Dictionary<string, NobleGatheringRecord> _gatherings = new Dictionary<string, NobleGatheringRecord>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, double> _playerHostCooldowns = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _playerInvitationNoticesShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly List<LocationCharacter> _addedAtmosphereCharacters = new List<LocationCharacter>();
	private MapNotificationView _registeredMapNotificationView;
	private long _nextNoticePublishRetryUtcTicks;
	private bool _pendingOpenPlayerGatheringFlow;
	private Hero _pendingGovernorHero;
	private Location _currentAtmosphereLocation;
	private SoundEvent _feastMusicEvent;
	private List<SettlementMusicData> _feastMusicPlayList = new List<SettlementMusicData>();
	private readonly Dictionary<int, FeastMusicianInstrumentChoice> _feastMusicianPerformances = new Dictionary<int, FeastMusicianInstrumentChoice>();
	private int _feastMusicTrackIndex = -1;
	private long _nextFeastMusicStartUtcTicks;
	private long _nextFeastMusicianPerformanceUtcTicks;

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

	public static NobleGatheringBehavior Instance { get; private set; }

	public NobleGatheringBehavior()
	{
		Instance = this;
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
		MBInformationManager.OnRemoveMapNotice -= OnMapNoticeRemoved;
		MBInformationManager.OnRemoveMapNotice += OnMapNoticeRemoved;
	}

	public override void SyncData(IDataStore dataStore)
	{
		Dictionary<string, string> gatheringStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> cooldownStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
			gatheringStorage = CampaignSaveChunkHelper.FlattenStringDictionary(gatheringStorage, SaveKeyGatherings, LogSource);
			cooldownStorage = CampaignSaveChunkHelper.FlattenStringDictionary(cooldownStorage, SaveKeyPlayerHostCooldowns, LogSource);
		}
		dataStore.SyncData(SaveKeyGatherings, ref gatheringStorage);
		dataStore.SyncData(SaveKeyPlayerHostCooldowns, ref cooldownStorage);
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
	}

	public void OnEngineTick()
	{
		UpdateFeastHallMusic();
		UpdateFeastMusicianPerformances();
		if (_pendingOpenPlayerGatheringFlow)
		{
			Hero governor = _pendingGovernorHero;
			_pendingOpenPlayerGatheringFlow = false;
			_pendingGovernorHero = null;
			if (governor != null)
			{
				OpenPlayerGatheringFlow(governor);
			}
		}
		if (!HasPendingPlayerInvitationNotice())
		{
			return;
		}
		long ticks = DateTime.UtcNow.Ticks;
		if (ticks < _nextNoticePublishRetryUtcTicks)
		{
			return;
		}
		_nextNoticePublishRetryUtcTicks = ticks + TimeSpan.FromSeconds(1.0).Ticks;
		TryPublishPlayerInvitationNotices();
	}

	public bool HasActiveGatheringAtSettlement(Settlement settlement)
	{
		string settlementId = (settlement?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return false;
		}
		double now = NowDay();
		return _gatherings.Values.Any(record =>
			record != null
			&& string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase)
			&& now < record.EndDay
			&& string.Equals(record.SettlementId, settlementId, StringComparison.OrdinalIgnoreCase));
	}

	private void OnFeastAtmosphereMissionStarted(IMission mission)
	{
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
		StopFeastHallMusic();
		ClearFeastMusicianPerformances();
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
			AddFeastWenches(location, settlement, unusedUsablePointCount);
			AddFeastMusicians(location, settlement, unusedUsablePointCount);
			ConfigureFeastMusicianGroups(Mission.Current);
			UpdateFeastHallMusic();
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
				ApplyFeastMusicianPerformance(agent, choice);
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
		_feastMusicianPerformances[missionAgent.Index] = choice;
		ApplyFeastMusicianPerformance(missionAgent, choice);
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
		_nextFeastMusicianPerformanceUtcTicks = 0L;
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
		int available = GetAvailableCount(unusedUsablePointCount, spawnTag);
		int desiredCount = Math.Min(MaxFeastMusicians, Math.Max(0, available));
		int existingCount = CountLocationCharacters(location, musician);
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

	private static bool IsLordHallLocation(Location location)
	{
		return string.Equals(location?.StringId, LordHallLocationId, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(location?.StringId, "lords_hall", StringComparison.OrdinalIgnoreCase);
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
			AddGovernorDialogue(starter);
		}
		catch (Exception ex)
		{
			Log("dialog add failed: " + ex.Message);
		}
	}

	private void AddGovernorDialogue(CampaignGameStarter starter)
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
		starter.AddDialogLine(
			"af_noble_gathering_governor_response",
			"af_noble_gathering_governor_response",
			"lord_pretalk",
			"我会为您准备名单与请柬。",
			null,
			null);
	}

	private bool IsGovernorGatheringDialogueAvailable()
	{
		Hero governor = ResolveConversationHero();
		return TryResolveGovernorOwnedSettlement(governor, out _, out _);
	}

	private void OpenGovernorGatheringFlowConsequence()
	{
		_pendingGovernorHero = ResolveConversationHero();
		_pendingOpenPlayerGatheringFlow = true;
		try
		{
			Campaign.Current?.ConversationManager?.EndConversation();
		}
		catch
		{
		}
	}

	private void OpenPlayerGatheringFlow(Hero governor)
	{
		if (!TryResolveGovernorOwnedSettlement(governor, out Settlement settlement, out string reject))
		{
			ShowMessage(reject);
			return;
		}
		ShowPlayerGatheringSettlementSelection(settlement);
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
				bool enabled = IsHeroEligibleForGatheringTravel(hero, out string reason);
				bool alreadySelected = currentHeroIdSet.Contains(hero?.StringId ?? "");
				if (enabled)
				{
					enabledCount++;
				}
				string label = (alreadySelected ? "[已选] " : "") + "[" + GetClanName(hero?.Clan) + "] " + GetHeroName(hero);
				string hint = GetHeroName(hero) + " / " + GetClanName(hero?.Clan) + (alreadySelected ? "\n已经在当前宴会名单中。" : enabled ? "\n可发出赴宴邀请。" : "\n不可邀请：" + reason);
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
			"家族：" + GetClanName(clan) + "\n只有拥有独立大地图部队的人可以接受邀请。\n已选宾客：" + currentHeroIds.Count + " 人。",
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
		List<string> currentHeroIds = NormalizeHeroIds(heroIds);
		List<Hero> heroes = currentHeroIds.Select(ResolveHeroById).Where(x => x != null).ToList();
		if (heroes.Count == 0)
		{
			ShowMessage("宴会未召开：没有可邀请的宾客。");
			ShowPlayerGatheringClanSelection(settlement, new List<string>());
			return;
		}
		string body = "举办地：" + GetSettlementName(settlement)
			+ "\n费用：" + GatheringCost + " 第纳尔"
			+ "\n持续：" + GatheringDurationDays + " 天"
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
		if (!CanPlayerHostAtSettlement(host, settlement, out status))
		{
			return false;
		}
		List<Hero> safeInvitees = (invitedHeroes ?? new List<Hero>())
			.Where(hero => hero != null && IsHeroEligibleForGatheringTravel(hero, out _))
			.GroupBy(hero => hero.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		if (safeInvitees.Count == 0)
		{
			status = "宴会未召开：没有可真实移动赴宴的宾客。";
			return false;
		}
		host.ChangeHeroGold(-GatheringCost);
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
			EndDay = now + GatheringDurationDays,
			IsPlayerHosted = true,
			PlayerInvitationStatus = ""
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
		IssueTravelCommands(record);
		status = "宴会已发出邀请：" + GetSettlementName(settlement) + "，宾客 " + safeInvitees.Count + " 人，持续 " + GatheringDurationDays + " 天";
		Log("player gathering created id=" + record.Id + " settlement=" + settlement.StringId + " invitees=" + safeInvitees.Count);
		return true;
	}

	private void OnDailyTick()
	{
		try
		{
			TryCreateNpcHostedGathering();
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
			ProcessActiveGatherings();
		}
		catch (Exception ex)
		{
			Log("hourly failed: " + ex.Message);
		}
	}

	private void ProcessActiveGatherings()
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
			IssueHostTravelCommand(record);
			IssueTravelCommands(record);
			UpdateArrivalsAndRewards(record, settlement, host);
			UpdatePlayerAttendanceReward(record, settlement, host);
		}
	}

	private void IssueHostTravelCommand(NobleGatheringRecord record)
	{
		if (record == null || record.IsPlayerHosted || record.HostCommandIssued)
		{
			return;
		}
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		Hero host = ResolveHeroById(record.HostHeroId);
		string reason = "";
		if (settlement == null || !IsHeroEligibleForGatheringTravel(host, out reason))
		{
			Log("npc host travel skipped id=" + (record?.Id ?? "") + " reason=" + reason);
			return;
		}
		WorldMapPartyCommandBehavior world = WorldMapPartyCommandBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		if (world == null)
		{
			return;
		}
		int holdDays = Math.Max(1, (int)Math.Ceiling(record.EndDay - NowDay()));
		if (world.TryIssueGoToSettlementUntilDayForExternal(host, settlement, holdDays, record.EndDay, BuildCommandSourceId(record), out string message))
		{
			record.HostCommandIssued = true;
			Log("npc host travel issued id=" + record.Id + " host=" + host.StringId + " settlement=" + settlement.StringId);
		}
		else
		{
			Log("npc host travel failed id=" + record.Id + " host=" + (host?.StringId ?? "") + " message=" + message);
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
		if (world == null)
		{
			return;
		}
		int holdDays = Math.Max(1, (int)Math.Ceiling(record.EndDay - NowDay()));
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			if (invitee == null || invitee.CommandIssued || !IsInviteAcceptedStatus(invitee.Status))
			{
				continue;
			}
			Hero hero = ResolveHeroById(invitee.HeroId);
			if (!IsHeroEligibleForGatheringTravel(hero, out string reason))
			{
				invitee.Status = InviteFailed;
				invitee.Reason = reason;
				continue;
			}
			if (world.TryIssueGoToSettlementUntilDayForExternal(hero, settlement, holdDays, record.EndDay, BuildCommandSourceId(record), out string message))
			{
				invitee.CommandIssued = true;
				invitee.Reason = "command_issued";
			}
			else
			{
				invitee.Status = InviteFailed;
				invitee.Reason = message;
			}
		}
	}

	private void UpdateArrivalsAndRewards(NobleGatheringRecord record, Settlement settlement, Hero host)
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
			if (!invitee.RelationRewardApplied)
			{
				ApplyArrivalRelationReward(record, host, hero);
				invitee.RelationRewardApplied = true;
			}
			DisplayGatheringMessage(GetHeroName(hero) + "已抵达" + GetSettlementName(settlement) + "参加宴会。", new Color(0.4f, 1f, 0.4f));
		}
	}

	private void ApplyArrivalRelationReward(NobleGatheringRecord record, Hero host, Hero guest)
	{
		if (guest == null || guest == Hero.MainHero)
		{
			return;
		}
		try
		{
			if (record.IsPlayerHosted)
			{
				ChangeRelationAction.ApplyPlayerRelation(guest, 5, affectRelatives: false, showQuickNotification: true);
			}
			else if (host != null && host != Hero.MainHero)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(host, guest, 5, showQuickNotification: false);
			}
		}
		catch (Exception ex)
		{
			Log("relation reward failed guest=" + (guest?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private void UpdatePlayerAttendanceReward(NobleGatheringRecord record, Settlement settlement, Hero host)
	{
		if (record == null
			|| record.IsPlayerHosted
			|| record.PlayerAttendanceRewardApplied
			|| !string.Equals(record.PlayerInvitationStatus, PlayerInvitationAccepted, StringComparison.OrdinalIgnoreCase))
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
		if (record == null)
		{
			return;
		}
		record.State = StateFinished;
		WorldMapPartyCommandBehavior world = WorldMapPartyCommandBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		if (!record.IsPlayerHosted && world != null)
		{
			Hero host = ResolveHeroById(record.HostHeroId);
			if (host != null)
			{
				world.TryStopExternalCommandForExternal(host, BuildCommandSourceId(record), out _);
			}
		}
		foreach (NobleGatheringInviteeRecord invitee in record.Invitees ?? new List<NobleGatheringInviteeRecord>())
		{
			Hero hero = ResolveHeroById(invitee?.HeroId);
			if (hero != null && world != null)
			{
				world.TryStopExternalCommandForExternal(hero, BuildCommandSourceId(record), out _);
			}
		}
		DisplayGatheringMessage(BuildGatheringEndMessage(record, reason), new Color(0.8f, 0.95f, 1f));
		Log("finish gathering id=" + record.Id + " reason=" + reason);
	}

	private void TryCreateNpcHostedGathering()
	{
		if (MBRandom.RandomFloat > 0.06f)
		{
			return;
		}
		List<Hero> possibleHosts = Hero.AllAliveHeroes
			.Where(hero => hero != null && hero != Hero.MainHero && hero.IsClanLeader && hero.Clan != null && !hero.IsPrisoner && hero.Gold >= GatheringCost)
			.Where(hero => hero.Clan.Kingdom != null && !hero.Clan.IsEliminated && !hero.Clan.IsMinorFaction && !hero.Clan.IsBanditFaction)
			.Where(hero => IsHeroEligibleForGatheringTravel(hero, out _))
			.Where(hero => !HasActiveGatheringForHost(hero))
			.OrderBy(_ => MBRandom.RandomFloat)
			.Take(10)
			.ToList();
		foreach (Hero host in possibleHosts)
		{
			if (!TryPickNpcHostSettlement(host, out Settlement settlement))
			{
				continue;
			}
			if (!IsKingdomMostlyPeaceful(host.Clan?.Kingdom))
			{
				continue;
			}
			List<Hero> invitees = PickNpcInvitees(host, settlement);
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
				EndDay = now + GatheringDurationDays,
				IsPlayerHosted = false,
				PlayerInvitationStatus = invitees.Contains(Hero.MainHero) ? PlayerInvitationPending : ""
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
			host.ChangeHeroGold(-GatheringCost);
			_gatherings[record.Id] = record;
			IssueTravelCommands(record);
			DisplayGatheringMessage(GetHeroName(host) + "将在" + GetSettlementName(settlement) + "举办宴会。", new Color(0.8f, 0.95f, 1f));
			Log("npc gathering created id=" + record.Id + " host=" + host.StringId + " settlement=" + settlement.StringId);
			return;
		}
	}

	private bool HasPendingPlayerInvitationNotice()
	{
		return _gatherings.Values.Any(record => HasPendingPlayerInvitation(record?.Id));
	}

	public bool HasPendingPlayerInvitation(string gatheringId)
	{
		if (string.IsNullOrWhiteSpace(gatheringId) || !_gatherings.TryGetValue(gatheringId, out NobleGatheringRecord record))
		{
			return false;
		}
		return string.Equals(record.State, StateActive, StringComparison.OrdinalIgnoreCase)
			&& !record.IsPlayerHosted
			&& string.Equals(record.PlayerInvitationStatus, PlayerInvitationPending, StringComparison.OrdinalIgnoreCase);
	}

	public bool OpenPlayerInvitationFromMap(string gatheringId)
	{
		if (!HasPendingPlayerInvitation(gatheringId) || !_gatherings.TryGetValue(gatheringId, out NobleGatheringRecord record))
		{
			return false;
		}
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		string body = GetHeroName(host) + "邀请你前往" + GetSettlementName(settlement) + "参加宴会。\n拒绝不会降低好感。";
		InformationManager.ShowInquiry(new InquiryData(
			"宴会邀请",
			body,
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"接受邀请",
			"拒绝",
			() => AcceptPlayerInvitation(record),
			() => DeclinePlayerInvitation(record)),
			pauseGameActiveState: true,
			prioritize: false);
		return true;
	}

	private void AcceptPlayerInvitation(NobleGatheringRecord record)
	{
		if (record == null)
		{
			return;
		}
		record.PlayerInvitationStatus = PlayerInvitationAccepted;
		Hero host = ResolveHeroById(record.HostHeroId);
		Settlement settlement = ResolveSettlementById(record.SettlementId);
		ShowMessage("你接受了" + GetHeroName(host) + "的宴会邀请。抵达" + GetSettlementName(settlement) + "后会与主办方提升好感。");
	}

	private void DeclinePlayerInvitation(NobleGatheringRecord record)
	{
		if (record == null)
		{
			return;
		}
		record.PlayerInvitationStatus = PlayerInvitationDeclined;
		ShowMessage("你婉拒了宴会邀请。");
	}

	private void TryPublishPlayerInvitationNotices()
	{
		if (!CanPublishMapNotification() || !TryEnsureMapNotificationRegistered())
		{
			return;
		}
		foreach (NobleGatheringRecord record in _gatherings.Values.ToList())
		{
			if (!HasPendingPlayerInvitation(record.Id) || _playerInvitationNoticesShownThisSession.Contains(record.Id))
			{
				continue;
			}
			Hero host = ResolveHeroById(record.HostHeroId);
			Settlement settlement = ResolveSettlementById(record.SettlementId);
			_playerInvitationNoticesShownThisSession.Add(record.Id);
			record.PlayerInvitationNoticeShown = true;
			MBInformationManager.AddNotice(new NobleGatheringInvitationMapNotification(record.Id, "宴会邀请", GetHeroName(host) + "邀请你前往" + GetSettlementName(settlement) + "赴宴。"));
		}
	}

	private bool TryEnsureMapNotificationRegistered()
	{
		try
		{
			MapNotificationView mapNotificationView = MapScreen.Instance?.MapNotificationView;
			if (mapNotificationView == null)
			{
				return false;
			}
			if (!ReferenceEquals(_registeredMapNotificationView, mapNotificationView))
			{
				_playerInvitationNoticesShownThisSession.Clear();
				mapNotificationView.RegisterMapNotificationType(typeof(NobleGatheringInvitationMapNotification), typeof(NobleGatheringInvitationMapNotificationItemVM));
				_registeredMapNotificationView = mapNotificationView;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("register notification failed: " + ex.Message);
			return false;
		}
	}

	private static bool CanPublishMapNotification()
	{
		try
		{
			return Mission.Current == null && Game.Current?.GameStateManager?.ActiveState is MapState && MapScreen.Instance?.MapNotificationView != null;
		}
		catch
		{
			return false;
		}
	}

	private void OnMapNoticeRemoved(InformationData data)
	{
		if (data is NobleGatheringInvitationMapNotification notice && _gatherings.TryGetValue(notice.GatheringId, out NobleGatheringRecord record))
		{
			if (string.Equals(record.PlayerInvitationStatus, PlayerInvitationPending, StringComparison.OrdinalIgnoreCase))
			{
				record.PlayerInvitationStatus = PlayerInvitationDeclined;
			}
		}
	}

	private bool CanPlayerHostAtSettlement(Hero host, Settlement settlement, out string reason)
	{
		reason = "";
		if (host == null || settlement == null || settlement.Town == null)
		{
			reason = "宴会无法召开：必须选择有效城镇或城堡。";
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
		if (host.Gold < GatheringCost)
		{
			reason = "宴会无法召开：你需要 " + GatheringCost + " 第纳尔。";
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
			.Where(hero => hero.Occupation == Occupation.Lord)
			.OrderBy(hero => GetClanName(hero.Clan))
			.ThenBy(hero => GetHeroName(hero));
	}

	private bool IsHeroEligibleForGatheringTravel(Hero hero, out string reason)
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

	private List<Hero> PickNpcInvitees(Hero host, Settlement settlement)
	{
		Kingdom kingdom = host?.Clan?.Kingdom;
		List<Hero> result = Hero.AllAliveHeroes
			.Where(hero => hero != null && hero != host && hero.Occupation == Occupation.Lord && hero.Clan?.Kingdom == kingdom)
			.Where(hero => hero == Hero.MainHero || IsHeroEligibleForGatheringTravel(hero, out _))
			.Where(hero => hero == Hero.MainHero || ShouldNpcConsiderInviting(host, hero))
			.OrderByDescending(hero => hero == Hero.MainHero ? host.GetRelation(hero) : host.GetRelation(hero))
			.ThenBy(_ => MBRandom.RandomFloat)
			.Take(16)
			.ToList();
		if (kingdom == Clan.PlayerClan?.Kingdom && Hero.MainHero != host && !result.Contains(Hero.MainHero) && host.GetRelation(Hero.MainHero) >= 10)
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
		string names = string.Join("、", heroes.Take(12).Select(GetHeroName));
		return heroes.Count > 12 ? "已选：" + names + " 等 " + heroes.Count + " 人。" : "已选：" + names;
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
		return "成员 " + members + " 人；有独立部队 " + movable + " 人。";
	}

	private static string BuildGatheringEndMessage(NobleGatheringRecord record, string reason)
	{
		Settlement settlement = ResolveSettlementById(record?.SettlementId);
		Hero host = ResolveHeroById(record?.HostHeroId);
		string place = GetSettlementName(settlement);
		if (record?.IsPlayerHosted == true)
		{
			return "宴会已结束：你在" + place + "举办的宴会已经散场。";
		}
		return "宴会已结束：" + GetHeroName(host) + "在" + place + "举办的宴会已经散场。";
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
