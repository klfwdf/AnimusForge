using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class WorldMapPartyCommandBehavior : CampaignBehaviorBase
{
	private const string LogSource = "WorldMapCommand";
	private const string StorageKey = "_af_worldmap_party_command_queues_v1";
	private const string DetachedPartyStorageKey = "_af_worldmap_player_detachments_v1";
	private const string GovernorExpeditionStorageKey = "_af_worldmap_governor_expeditions_v1";
	private const string ForeignClanGuestStorageKey = "_af_worldmap_foreign_clan_guests_v1";
	private const int GovernorExpeditionMinimumTroops = 10;
	private const int GovernorGarrisonMinimumReserve = 40;
	private const float GovernorGarrisonReserveRatio = 0.5f;
	private const string GovernorExpeditionPhaseActive = "active";
	private const string GovernorExpeditionPhaseReturning = "returning";
	private const string GovernorExpeditionPhaseCleanup = "cleanup";
	private const float SettlementArrivalDistance = 3.0f;
	private const float PatrolArrivalDistance = 8.0f;
	private const float PatrolLeashDistance = 24.0f;
	private const float MergeArrivalDistance = 10.0f;
	private const float FollowArrivalDistance = 10.0f;
	private const float FollowLeashDistance = 24.0f;
	// Vanilla GoAroundParty holds roughly 5.95 map units from its target. Leave
	// enough margin for the hourly command tick to switch from tracking to engage.
	private const float PartyAttackCommitDistance = 8.0f;
	private const float SettlementAttackCommitDistance = 6.0f;
	private const float EngageMaintainDistance = 10.0f;
	private const float FriendlySupportRadius = 12.0f;
	private const float AiAttackStrengthRatio = 0.85f;
	private const int DefaultHeroAttackDays = 1;
	private const int DefaultSiegeAttackDays = 15;
	private const int DefaultRaidAttackDays = 5;
	private const string AttackModeAi = "AI";
	private const string AttackModeForce = "FORCE";
	private const string LegacyAttackModeRebellionForce = "REBELLION_FORCE";
	private const string PendingSafeExitResumeFollow = "resume_follow";
	private const string PendingSafeExitAdvance = "advance";
	private const string PendingSafeExitStop = "stop";
	// Food, peace, and inactivity checks can attempt to dissolve an ordered army
	// every hour.
	// Charge at most once per campaign day for the same command, while still
	// suppressing each repeat attempt until the command itself ends.
	private const double ArmySurvivalRenewalChargeIntervalDays = 1.0;

	private static readonly Regex WorldMapOrderTagRegex = new Regex("\\[ACTION:WORLDMAP_ORDER:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private readonly Dictionary<string, PartyCommandQueueState> _queues = new Dictionary<string, PartyCommandQueueState>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PendingCreateCompanionPartyRequest> _pendingCreatePartyRequests = new Dictionary<string, PendingCreateCompanionPartyRequest>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PendingGovernorExpeditionRequest> _pendingGovernorExpeditionRequests = new Dictionary<string, PendingGovernorExpeditionRequest>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PlayerDetachedPartyRecord> _playerDetachedParties = new Dictionary<string, PlayerDetachedPartyRecord>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, GovernorExpeditionRecord> _governorExpeditions = new Dictionary<string, GovernorExpeditionRecord>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, ForeignClanGuestRecord> _foreignClanGuests = new Dictionary<string, ForeignClanGuestRecord>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _governorExpeditionHeroByPartyKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, MobileParty> _governorExpeditionPartyByHeroId = new Dictionary<string, MobileParty>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Settlement> _governorReturnTargetByHeroId = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
	private readonly object _queueLock = new object();
	private int _hasPendingCreateCompanionPartyRequests;
	private int _hasPendingGovernorExpeditionRequests;
	private int _hasPendingGovernorExpeditionReconcile;
	private int _hasPendingFollowSiegeRefresh;
	private int _hasGovernorExpeditions;
	private int _hasForeignClanGuests;
	private bool _isOpeningCreateCompanionPartyScreen;
	private double _nextDetachedPartyPruneDay;
	private double _nextForeignClanGuestReconcileDay;

	public static WorldMapPartyCommandBehavior Instance { get; private set; }

	internal static bool ShouldProtectGovernorExpeditionLeaderFromNativeReplacement(MobileParty party)
	{
		// Harmony invokes this once per party/day. Keep the common path allocation-free:
		// one volatile flag, cheap actor predicates, then a single O(1) party-ID lookup.
		WorldMapPartyCommandBehavior behavior = Instance;
		if (behavior == null || party == null || Volatile.Read(ref behavior._hasGovernorExpeditions) == 0)
		{
			return false;
		}
		try
		{
			Hero leader = party.LeaderHero;
			Clan clan = party.ActualClan;
			if (!party.IsActive || leader == null || leader.PartyBelongedTo != party
				|| !leader.IsNoncombatant || clan == null || clan == Clan.PlayerClan || clan.Leader == leader)
			{
				return false;
			}
			bool shouldProtect = behavior.TryGetGovernorExpeditionForParty(party, out GovernorExpeditionRecord record)
				&& record != null
				&& string.Equals(record.HeroId, leader.StringId, StringComparison.OrdinalIgnoreCase);
			if (shouldProtect)
			{
				Log("protected governor expedition leader from native noncombatant replacement hero=" + leader.StringId + " party=" + (party.StringId ?? ""));
			}
			return shouldProtect;
		}
		catch (Exception ex)
		{
			Log("check native governor expedition leader guard failed party=" + (party.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	internal static bool TryRenewOrderedArmyBeforeNativeDisband(Army army, Army.ArmyDispersionReason reason)
	{
		WorldMapPartyCommandBehavior behavior = Instance;
		if (behavior == null || !IsRenewableOrderedArmyDispersalReason(reason))
		{
			return false;
		}
		try
		{
			return behavior.TryRenewOrderedArmyBeforeNativeDisbandCore(army, reason);
		}
		catch (Exception ex)
		{
			Log("ordered army survival renewal failed open reason=" + reason + " error=" + ex.Message);
			return false;
		}
	}

	private static bool IsRenewableOrderedArmyDispersalReason(Army.ArmyDispersionReason reason)
	{
		return reason == Army.ArmyDispersionReason.CohesionDepleted
			|| reason == Army.ArmyDispersionReason.FoodProblem
			|| reason == Army.ArmyDispersionReason.NoActiveWar
			|| reason == Army.ArmyDispersionReason.Inactivity;
	}

	private bool TryRenewOrderedArmyBeforeNativeDisbandCore(Army army, Army.ArmyDispersionReason reason)
	{
		MobileParty leaderParty = army?.LeaderParty;
		Hero leader = leaderParty?.LeaderHero;
		if (leaderParty == null || leader == null || leader == Hero.MainHero || leaderParty.Army != army || !IsPartyUsable(leaderParty))
		{
			return false;
		}
		PartyCommandQueueState state = null;
		lock (_queueLock)
		{
			_queues.TryGetValue(leader.StringId ?? "", out state);
		}
		PartyCommandEntry command = GetCurrentCommand(state);
		if (state == null || state.ResultLogged || !PartyMatchesActor(leaderParty, state)
			|| !IsKind(command, CommandKind.AttackHero) || !IsSettlementTarget(command))
		{
			return false;
		}
		Settlement settlement = ResolveSettlementById(command.TargetId);
		Clan clan = leader.Clan;
		if (!IsSupportedAttackSettlement(settlement) || clan == null || IsSettlementAttackComplete(leaderParty, settlement))
		{
			return false;
		}
		string renewalKey = (leaderParty.StringId ?? "") + ":" + state.CurrentIndex + ":" + (command.TargetId ?? "") + ":" + state.CommandStartDay.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
		double now = NowDay();
		bool shouldCharge = !string.Equals(state.ArmySurvivalRenewalKey, renewalKey, StringComparison.Ordinal)
			|| state.ArmySurvivalLastPaidDay < 0.0
			|| now - state.ArmySurvivalLastPaidDay >= ArmySurvivalRenewalChargeIntervalDays;
		int influenceCost = 0;
		if (shouldCharge)
		{
			float currentCohesion = army.Cohesion;
			if (float.IsNaN(currentCohesion) || float.IsInfinity(currentCohesion))
			{
				currentCohesion = 0f;
			}
			currentCohesion = Math.Max(0f, Math.Min(100f, currentCohesion));
			float renewalPercentage = Math.Max(1f, 100f - currentCohesion);
			// Food, peace, and inactivity can request dissolution even at full
			// cohesion. Charge a full renewal in that case so those protections
			// still consume influence.
			if (reason != Army.ArmyDispersionReason.CohesionDepleted && renewalPercentage <= 1f)
			{
				renewalPercentage = 100f;
			}
			influenceCost = Math.Max(0, Campaign.Current.Models.ArmyManagementCalculationModel.CalculateTotalInfluenceCost(army, renewalPercentage));
			// ChangeClanInfluenceAction deliberately does not clamp its result, which
			// is required here: a player-authorized campaign order may take the
			// commander below zero influence.
			ChangeClanInfluenceAction.Apply(clan, -(float)influenceCost);
			state.ArmySurvivalRenewalKey = renewalKey;
			state.ArmySurvivalLastPaidDay = now;
		}
		army.Cohesion = 100f;
		SynchronizeArmyObjectiveForCommand(leaderParty, command);
		if (shouldCharge)
		{
			LogFact(state, leader, GetActorName(state, leader, leaderParty) + "消耗" + influenceCost + "影响力强行维持军团，继续" + (settlement.IsVillage ? "烧掠" : "围攻") + GetSettlementName(settlement) + "（军团长影响力现为" + clan.Influence.ToString("0") + "）。");
			Log("ordered_army_survival_renewed leader=" + (leader.StringId ?? "") + " party=" + (leaderParty.StringId ?? "") + " settlement=" + (settlement.StringId ?? "") + " reason=" + reason + " cost=" + influenceCost + " influence=" + clan.Influence.ToString("0"));
		}
		return true;
	}

	private enum CommandKind
	{
		GoToSettlement,
		PatrolSettlement,
		FollowHero,
		FollowParty,
		AttackHero,
		AttackParty,
		MergeToPlayer
	}

	private enum CommandStage
	{
		New,
		Traveling,
		Active,
		Tracking,
		Engaging
	}

	private enum CommandResultOutcome
	{
		Success,
		Failure,
		Incomplete
	}

	private enum CommandMessageTone
	{
		Success,
		Progress,
		Failure,
		Neutral
	}

	private sealed class PartyCommandQueueState
	{
		public string HeroId;
		public string ActorKey;
		public string ActorName;
		public string PartyStringId;
		public int PartyIndex = -1;
		public string NonHeroMemoryId;
		public string NonHeroMemoryName;
		public List<PartyCommandEntry> Commands = new List<PartyCommandEntry>();
		public int CurrentIndex;
		public string Stage = CommandStage.New.ToString();
		public double CommandStartDay;
		public double ArrivalDay = -1.0;
		public double TimeoutDay = -1.0;
		public bool EngageCommitted;
		public string LastIssuedActionKey;
		public string LastStatusMessageKey;
		public string ResultKind;
		public string ResultTargetType;
		public string ResultTargetId;
		public string ResultTargetName;
		public string ResultActorFactionId;
		public string ResultTargetFactionId;
		public double ResultCommitDay = -1.0;
		public double ResultDeadlineDay = -1.0;
		public bool ResultLogged;
		public string SourceId;
		public string FollowSiegeSettlementId;
		public bool FollowSiegeJoinedByCommand;
		public string PendingSafeExitAction;
		public string PendingSafeExitReason;
		public int MergeTransferFailureCount;
		public double MergeRetryAfterDay = -1.0;
		public string ArmySurvivalRenewalKey;
		public double ArmySurvivalLastPaidDay = -1.0;
	}

	private sealed class PartyCommandEntry
	{
		public string Kind;
		public string TargetType;
		public string TargetId;
		public int Days = 1;
		public double HoldUntilDay = -1.0;
		public string Mode;
		public bool RequiresExistingWar;
	}

	private sealed class PendingCreateCompanionPartyRequest
	{
		public string HeroId;
		public List<PartyCommandEntry> FollowUpCommands = new List<PartyCommandEntry>();
	}

	private sealed class PendingGovernorExpeditionRequest
	{
		public string HeroId;
		public string OriginSettlementId;
		public string OriginClanId;
		public List<PartyCommandEntry> FollowUpCommands = new List<PartyCommandEntry>();
	}

	private sealed class GovernorExpeditionRecord
	{
		public string HeroId;
		public string PartyStringId;
		public int PartyIndex = -1;
		public string OriginSettlementId;
		public string OriginClanId;
		public string Phase = GovernorExpeditionPhaseActive;
		public string ReturnTargetSettlementId;
		public string LastIssuedActionKey;
	}

	private sealed class GovernorTroopTransferPlan
	{
		[JsonIgnore]
		public CharacterObject Character;
		public int Count;
		public int Xp;
	}

	private struct RosterElementState
	{
		public CharacterObject Character;
		public int Number;
		public int WoundedNumber;
		public int Xp;
	}

	private sealed class RosterElementTransferRecord
	{
		public TroopRoster Source;
		public TroopRoster Target;
		public CharacterObject Character;
		public RosterElementState SourceBefore;
		public RosterElementState TargetBefore;
		public bool IsPrisonerRoster;
	}

	private sealed class PlayerDetachedPartyRecord
	{
		public string HeroId;
		public string PartyStringId;
		public int PartyIndex = -1;
	}

	private sealed class ForeignClanGuestRecord
	{
		public string HeroId;
		public string ClanId;
		public double JoinedDay;
	}

	private sealed class MergeHeroIdentitySnapshot
	{
		[JsonIgnore]
		public Hero Hero;
		[JsonIgnore]
		public Clan Clan;
		[JsonIgnore]
		public Clan CompanionOf;
		public Occupation Occupation;
	}

	public sealed class WorldMapOrderApplyResult
	{
		public bool HadTag { get; internal set; }
		public bool Handled { get; internal set; }
		public bool StopApplied { get; internal set; }
		public int AddedCommandCount { get; internal set; }
		public bool CompanionPartyCreationQueued { get; internal set; }
		public bool GovernorExpeditionCreationQueued { get; internal set; }
		public bool NeedsChannelExit => CompanionPartyCreationQueued || GovernorExpeditionCreationQueued;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener(this, OnSiegeCompleted);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, OnVillageStateChanged);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnFollowSiegeEventChanged);
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnFollowSiegeEventChanged);
		CampaignEvents.OnMobilePartyJoinedToSiegeEventEvent.AddNonSerializedListener(this, OnFollowSiegePartyChanged);
		CampaignEvents.OnMobilePartyLeftSiegeEventEvent.AddNonSerializedListener(this, OnFollowSiegePartyChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
		Dictionary<string, string> storage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> detachedStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> governorExpeditionStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> foreignClanGuestStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (dataStore.IsSaving)
		{
			lock (_queueLock)
			{
				foreach (KeyValuePair<string, PartyCommandQueueState> pair in _queues)
				{
					if (pair.Value != null && !string.IsNullOrWhiteSpace(pair.Key))
					{
						storage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
					}
				}
				foreach (KeyValuePair<string, PlayerDetachedPartyRecord> pair in _playerDetachedParties)
				{
					if (pair.Value != null && !string.IsNullOrWhiteSpace(pair.Key))
					{
						detachedStorage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
					}
				}
				foreach (KeyValuePair<string, GovernorExpeditionRecord> pair in _governorExpeditions)
				{
					if (pair.Value != null && !string.IsNullOrWhiteSpace(pair.Key))
					{
						governorExpeditionStorage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
					}
				}
				foreach (KeyValuePair<string, ForeignClanGuestRecord> pair in _foreignClanGuests)
				{
					if (pair.Value != null && !string.IsNullOrWhiteSpace(pair.Key))
					{
						foreignClanGuestStorage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
					}
				}
			}
			storage = CampaignSaveChunkHelper.FlattenStringDictionary(storage, StorageKey, "WorldMapPartyCommand");
			detachedStorage = CampaignSaveChunkHelper.FlattenStringDictionary(detachedStorage, DetachedPartyStorageKey, "WorldMapPlayerDetachment");
			governorExpeditionStorage = CampaignSaveChunkHelper.FlattenStringDictionary(governorExpeditionStorage, GovernorExpeditionStorageKey, "WorldMapGovernorExpedition");
			foreignClanGuestStorage = CampaignSaveChunkHelper.FlattenStringDictionary(foreignClanGuestStorage, ForeignClanGuestStorageKey, "WorldMapForeignClanGuest");
		}
		dataStore.SyncData(StorageKey, ref storage);
		dataStore.SyncData(DetachedPartyStorageKey, ref detachedStorage);
		dataStore.SyncData(GovernorExpeditionStorageKey, ref governorExpeditionStorage);
		dataStore.SyncData(ForeignClanGuestStorageKey, ref foreignClanGuestStorage);
		if (!dataStore.IsLoading)
		{
			return;
		}
		storage = CampaignSaveChunkHelper.RestoreStringDictionary(storage, "WorldMapPartyCommand");
		lock (_queueLock)
		{
			_pendingCreatePartyRequests.Clear();
			_pendingGovernorExpeditionRequests.Clear();
			Volatile.Write(ref _hasPendingCreateCompanionPartyRequests, 0);
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, 0);
			_queues.Clear();
			foreach (KeyValuePair<string, string> pair in storage ?? new Dictionary<string, string>())
			{
				try
				{
					PartyCommandQueueState state = JsonConvert.DeserializeObject<PartyCommandQueueState>(pair.Value ?? "");
					if (state == null || state.Commands == null || state.Commands.Count == 0)
					{
						continue;
					}
					NormalizeState(state);
					if (state.Commands == null || state.Commands.Count == 0 || state.CurrentIndex >= state.Commands.Count)
					{
						continue;
					}
					string queueKey = GetQueueKey(state);
					if (string.IsNullOrWhiteSpace(queueKey))
					{
						continue;
					}
					_queues[queueKey] = state;
				}
				catch (Exception ex)
				{
					Log("load failed key=" + pair.Key + " error=" + ex.Message);
				}
			}
			_playerDetachedParties.Clear();
			foreach (KeyValuePair<string, string> pair in CampaignSaveChunkHelper.RestoreStringDictionary(detachedStorage, "WorldMapPlayerDetachment") ?? new Dictionary<string, string>())
			{
				try
				{
					PlayerDetachedPartyRecord record = JsonConvert.DeserializeObject<PlayerDetachedPartyRecord>(pair.Value ?? "");
					if (TryValidateDetachedPartyRecord(record, out Hero _, out MobileParty _))
					{
						_playerDetachedParties[record.HeroId] = record;
					}
				}
				catch (Exception ex)
				{
					Log("load detached party failed key=" + pair.Key + " error=" + ex.Message);
				}
			}
			_governorExpeditions.Clear();
			_governorExpeditionHeroByPartyKey.Clear();
			_governorExpeditionPartyByHeroId.Clear();
			_governorReturnTargetByHeroId.Clear();
			Dictionary<string, string> restoredGovernorStorage = CampaignSaveChunkHelper.RestoreStringDictionary(governorExpeditionStorage, "WorldMapGovernorExpedition");
			foreach (KeyValuePair<string, string> pair in restoredGovernorStorage ?? new Dictionary<string, string>())
			{
				try
				{
					GovernorExpeditionRecord record = JsonConvert.DeserializeObject<GovernorExpeditionRecord>(pair.Value ?? "");
					if (!NormalizeGovernorExpeditionRecord(record))
					{
						continue;
					}
					_governorExpeditions[record.HeroId] = record;
					IndexGovernorExpeditionRecordUnsafe(record);
				}
				catch (Exception ex)
				{
					Log("load governor expedition failed key=" + pair.Key + " error=" + ex.Message);
				}
			}
			_foreignClanGuests.Clear();
			Dictionary<string, string> restoredGuestStorage = CampaignSaveChunkHelper.RestoreStringDictionary(foreignClanGuestStorage, "WorldMapForeignClanGuest");
			foreach (KeyValuePair<string, string> pair in restoredGuestStorage ?? new Dictionary<string, string>())
			{
				try
				{
					ForeignClanGuestRecord record = JsonConvert.DeserializeObject<ForeignClanGuestRecord>(pair.Value ?? "");
					Hero guest = ResolveHeroByIdAny(record?.HeroId);
					if (record == null || guest == null || string.IsNullOrWhiteSpace(record.HeroId)
						|| string.IsNullOrWhiteSpace(record.ClanId) || guest.Clan == null
						|| !string.Equals(guest.Clan.StringId ?? "", record.ClanId, StringComparison.OrdinalIgnoreCase)
						|| !IsForeignClanGuestPlacementValid(guest))
					{
						continue;
					}
					_foreignClanGuests[record.HeroId] = record;
				}
				catch (Exception ex)
				{
					Log("load foreign clan guest failed key=" + pair.Key + " error=" + ex.Message);
				}
			}
		}
		Volatile.Write(ref _hasPendingFollowSiegeRefresh, 1);
		Volatile.Write(ref _hasPendingGovernorExpeditionReconcile, 1);
		Volatile.Write(ref _hasGovernorExpeditions, _governorExpeditions.Count > 0 ? 1 : 0);
		Volatile.Write(ref _hasForeignClanGuests, _foreignClanGuests.Count > 0 ? 1 : 0);
	}

	public static bool HasWorldMapOrderTag(string text)
	{
		return WorldMapOrderTagRegex.IsMatch(text ?? "");
	}

	public static string StripWorldMapOrderTags(string text)
	{
		return WorldMapOrderTagRegex.Replace(text ?? "", "").Trim();
	}

	private static string NormalizeExternalSourceId(string sourceId)
	{
		string text = (sourceId ?? "").Trim();
		if (text.Length > 96)
		{
			text = text.Substring(0, 96);
		}
		return text;
	}

	private static bool ShouldSuppressCommandMessages(PartyCommandQueueState state)
	{
		return IsNobleGatheringSource(state?.SourceId);
	}

	private static bool IsNobleGatheringSource(string sourceId)
	{
		return NormalizeExternalSourceId(sourceId).StartsWith("noble_gathering:", StringComparison.OrdinalIgnoreCase);
	}

	public static string NormalizeWorldMapOrderTagsForExternal(string raw)
	{
		List<string> tags = new List<string>();
		foreach (Match match in WorldMapOrderTagRegex.Matches(raw ?? ""))
		{
			if (TryParseTag(match.Value, validateTargets: true, out PartyCommandEntry command, out bool stop))
			{
				if (stop)
				{
					tags.Add("[ACTION:WORLDMAP_ORDER:STOP]");
				}
				else
				{
					string normalized = BuildTag(command);
					if (!string.IsNullOrWhiteSpace(normalized))
					{
						tags.Add(normalized);
					}
				}
			}
		}
		return string.Join("\n", tags).Trim();
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero)
	{
		return BuildRuntimePostprocessRulesForExternal(targetHero, null, -1);
	}

	public static string BuildCurrentNpcCommandTasksPromptForExternal(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		const string header = "【当前NPC命令任务】";
		try
		{
			WorldMapPartyCommandBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
			if (behavior == null)
			{
				return header + "无";
			}
			targetHero = targetHero ?? targetCharacter?.HeroObject;
			List<PartyCommandEntry> snapshot = new List<PartyCommandEntry>();
			lock (behavior._queueLock)
			{
				if (targetHero != null && !string.IsNullOrWhiteSpace(targetHero.StringId))
				{
					if (behavior._queues.TryGetValue(targetHero.StringId, out PartyCommandQueueState state) && state?.Commands != null && !IsStopPending(state))
					{
						snapshot = state.Commands.Skip(Math.Max(0, state.CurrentIndex)).Where(IsExecutableCommand).Select(CloneCommand).ToList();
					}
					else if (behavior._pendingCreatePartyRequests.TryGetValue(targetHero.StringId, out PendingCreateCompanionPartyRequest pending))
					{
						snapshot = (pending?.FollowUpCommands ?? new List<PartyCommandEntry>()).Where(IsExecutableCommand).Select(CloneCommand).ToList();
					}
				}
				else if (TryResolveNonHeroPartyActorForExternal(targetCharacter, targetAgentIndex, out MobileParty party))
				{
					string actorKey = BuildPartyActorKey(party, createGuid: false);
					if (!string.IsNullOrWhiteSpace(actorKey) && behavior._queues.TryGetValue(actorKey, out PartyCommandQueueState state) && state?.Commands != null && !IsStopPending(state))
					{
						snapshot = state.Commands.Skip(Math.Max(0, state.CurrentIndex)).Where(IsExecutableNonHeroPartyCommand).Select(CloneCommand).ToList();
					}
				}
			}
			if (snapshot.Count == 0)
			{
				return header + "无";
			}
			int shownCount = Math.Min(5, snapshot.Count);
			List<string> phrases = new List<string>();
			for (int i = 0; i < shownCount; i++)
			{
				phrases.Add((i == 0 ? "进行:" : "随后:") + BuildNaturalCommandSummary(snapshot[i]));
			}
			if (snapshot.Count > shownCount)
			{
				phrases.Add("另" + (snapshot.Count - shownCount) + "项");
			}
			return header + string.Join(" → ", phrases);
		}
		catch (Exception ex)
		{
			Log("build command task prompt failed: " + ex.Message);
			return header + "无";
		}
	}

	private static string BuildNaturalCommandSummary(PartyCommandEntry command)
	{
		int days = Math.Max(1, command?.Days ?? 1);
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return "归队" + days + "天";
		}
		string targetName = ResolveNaturalCommandTargetName(command);
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			return "前往" + targetName + days + "天";
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			return "巡逻" + targetName + days + "天";
		}
		if (IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.FollowParty))
		{
			return "跟随" + targetName + days + "天";
		}
		if (IsKind(command, CommandKind.AttackHero) || IsKind(command, CommandKind.AttackParty))
		{
			return (IsForceAttackMode(command?.Mode) ? "强攻" : "攻击") + targetName + days + "天";
		}
		return "未知任务" + days + "天";
	}

	private static string ResolveNaturalCommandTargetName(PartyCommandEntry command)
	{
		if (command == null)
		{
			return "未知目标";
		}
		if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement) || IsSettlementTarget(command))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			return settlement == null ? "未知地点" : GetSettlementName(settlement);
		}
		if (IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty))
		{
			MobileParty party = ResolveMobilePartyById(command.TargetId);
			return party == null ? "未知部队" : GetPartyName(party);
		}
		Hero hero = ResolveHeroByIdAny(command.TargetId);
		return hero == null ? "未知人物" : GetHeroName(hero);
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		List<PostprocessRuleEntry> rules = AIConfigHandler.GetGuardrailRulePostprocessRules("worldmap_party_command") ?? new List<PostprocessRuleEntry>();
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		bool nonHeroPartyFallback = targetHero == null && CanUseNonHeroPartyFallbackForExternal(targetCharacter, targetAgentIndex);
		if (targetHero == null && !nonHeroPartyFallback)
		{
			return new List<PostprocessRuleEntry>();
		}
		bool useMainPartyHeroMergePrompt = false;
		bool canExposeMerge = targetHero != null && TryResolveMergeToPlayerPostprocessVariant(targetHero, out useMainPartyHeroMergePrompt);
		List<PostprocessRuleEntry> filtered = new List<PostprocessRuleEntry>();
		foreach (PostprocessRuleEntry rule in rules)
		{
			if (rule == null)
			{
				continue;
			}
			if (IsMergeToPlayerPostprocessRule(rule))
			{
				if (nonHeroPartyFallback || !canExposeMerge)
				{
					continue;
				}
				filtered.Add(ClonePostprocessRule(rule, BuildMergeToPlayerPostprocessDescription(useMainPartyHeroMergePrompt)));
				continue;
			}
			filtered.Add(ClonePostprocessRule(rule));
		}
		return filtered;
	}

	private static bool TryResolveMergeToPlayerPostprocessVariant(Hero targetHero, out bool useMainPartyHeroMergePrompt)
	{
		useMainPartyHeroMergePrompt = false;
		if (targetHero == null || targetHero == Hero.MainHero)
		{
			return false;
		}
		if (IsHeroActuallyInPlayerMainPartyRoster(targetHero))
		{
			useMainPartyHeroMergePrompt = true;
			return true;
		}
		WorldMapPartyCommandBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
		MobileParty party = ResolveActorParty(targetHero);
		return TryValidatePlayerWildernessPartyForMerge(targetHero, party, behavior, out _);
	}

	// Keep the MERGE_TO_PLAYER route and executor on the same eligibility rule.
	// This mirrors the proven [A:H_J_P_P_C&L] wilderness-party resolution: the
	// hero's live PartyBelongedTo and roster membership are authoritative. The
	// detached-party registry is only an additional ownership signal, not a
	// prerequisite for ordinary player companions or family members.
	private static bool TryValidatePlayerWildernessPartyForMerge(Hero hero, MobileParty party, WorldMapPartyCommandBehavior behavior, out string reason)
	{
		reason = "";
		if (hero == null || hero == Hero.MainHero || hero.CharacterObject == null)
		{
			reason = "invalid_hero";
			return false;
		}
		if (!IsPartyUsable(party) || party == MobileParty.MainParty || party.Party == PartyBase.MainParty)
		{
			reason = "not_independent_wilderness_party";
			return false;
		}
		if (hero.PartyBelongedTo != party || party.LeaderHero != hero)
		{
			reason = "hero_party_mismatch";
			return false;
		}
		if (party.MemberRoster == null || !party.MemberRoster.Contains(hero.CharacterObject))
		{
			reason = "hero_missing_from_party_roster";
			return false;
		}
		bool isRegisteredDetachment = behavior != null && behavior.IsRegisteredPlayerDetachment(hero, party);
		bool isPlayerCompanionOrFamily = RomanceSystemBehavior.IsPlayerCompanionOrFamily(hero);
		if (IsForeignClanGuestHero(hero))
		{
			if (!TryValidateForeignClanGuestMerge(hero, party, out reason))
			{
				return false;
			}
		}
		else if (!isRegisteredDetachment && !isPlayerCompanionOrFamily)
		{
			reason = "not_player_companion_family_or_registered_detachment";
			return false;
		}
		if (!IsPartyUsable(MobileParty.MainParty))
		{
			reason = "main_party_invalid";
			return false;
		}
		return true;
	}

	private static bool TryValidateForeignClanGuestMerge(Hero hero, MobileParty party, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
		Clan foreignClan = hero?.Clan;
		if (playerClan == null || foreignClan == null || foreignClan == playerClan)
		{
			reason = "foreign_guest_missing_foreign_clan";
			return false;
		}
		if (hero.IsDead || !hero.IsActive || hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
		{
			reason = "foreign_guest_inactive_or_captive";
			return false;
		}
		if (!party.IsLordParty || (party.ActualClan != null && party.ActualClan != foreignClan))
		{
			reason = "foreign_guest_not_own_lord_party";
			return false;
		}
		if (hero.GovernorOf != null)
		{
			reason = "foreign_guest_is_governor";
			return false;
		}
		if (party.Army != null)
		{
			reason = "foreign_guest_in_army";
			return false;
		}
		if (ArePlayerAndClanAtWar(foreignClan))
		{
			reason = "foreign_guest_at_war";
			return false;
		}
		return true;
	}

	private static bool ArePlayerAndClanAtWar(Clan foreignClan)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (playerClan == null || foreignClan == null || foreignClan == playerClan)
			{
				return false;
			}
			IFaction playerFaction = playerClan.Kingdom ?? (IFaction)playerClan;
			IFaction foreignFaction = foreignClan.Kingdom ?? (IFaction)foreignClan;
			return playerFaction != null && foreignFaction != null
				&& playerFaction != foreignFaction
				&& FactionManager.IsAtWarAgainstFaction(playerFaction, foreignFaction);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsMergeToPlayerPostprocessRule(PostprocessRuleEntry rule)
	{
		string tag = (rule?.Tag ?? "").Trim();
		return tag.IndexOf("WORLDMAP_ORDER:MERGE_TO_PLAYER", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static PostprocessRuleEntry ClonePostprocessRule(PostprocessRuleEntry rule, string descriptionOverride = null)
	{
		return new PostprocessRuleEntry
		{
			Tag = (rule?.Tag ?? "").Trim(),
			Description = (descriptionOverride ?? rule?.Description ?? "").Trim()
		};
	}

	private static string BuildMergeToPlayerPostprocessDescription(bool useMainPartyHeroMergePrompt)
	{
		if (useMainPartyHeroMergePrompt)
		{
			return "你在输出了其他ACTION:WORLDMAP时，如果玩家没有主动提到无需回归队伍，那么你必须输出此标签在最下方,严禁不输出该标签";
		}
		return "Independent-party merge. Output only when the NPC clearly agrees to merge the whole field party into the player's main party. A foreign-clan Hero joins as a guest and keeps Clan, occupation, and companion status. Never use while hostile, captive, governing, in an army, or not the actual party leader. {days} is the return timeout; use 1 if unspecified.";
	}

	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyWorldMapOrderTagsForExternal(targetHero, null, -1, ref content, out generatedFacts, out notifications, out _);
	}

	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyWorldMapOrderTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out generatedFacts, out notifications, out _);
	}

	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications, out WorldMapOrderApplyResult result)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		result = new WorldMapOrderApplyResult();
		string original = content ?? "";
		try
		{
			List<PartyCommandEntry> commands = new List<PartyCommandEntry>();
			bool hasAnyWorldMapTag = false;
			bool hasParsedTag = false;
			bool leadingStop = false;
			foreach (Match match in WorldMapOrderTagRegex.Matches(original))
			{
				hasAnyWorldMapTag = true;
				if (!TryParseTag(match.Value, validateTargets: true, out PartyCommandEntry command, out bool isStop))
				{
					continue;
				}
				if (!hasParsedTag)
				{
					hasParsedTag = true;
					leadingStop = isStop;
					if (isStop)
					{
						continue;
					}
				}
				if (isStop)
				{
					notifications.Add("大地图命令顺序错误：STOP 只能位于本轮首个有效世界地图标签，已忽略该 STOP。");
					Log("ignored non-leading STOP tag");
					continue;
				}
				if (command != null)
				{
					commands.Add(command);
				}
			}
			content = StripWorldMapOrderTags(original);
			result.HadTag = hasAnyWorldMapTag;
			if (!hasAnyWorldMapTag)
			{
				return false;
			}
			WorldMapPartyCommandBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
			if (behavior == null)
			{
				notifications.Add("大地图命令系统未初始化。");
				return false;
			}
			targetHero = targetHero ?? targetCharacter?.HeroObject;
			if (targetHero == null)
			{
				if (!TryResolveNonHeroPartyActorForExternal(targetCharacter, targetAgentIndex, out MobileParty nonHeroParty))
				{
					notifications.Add("大地图命令失败：当前非英雄说话对象没有可接管的野外部队。");
					return false;
				}
				string actorName = GetActorName(null, null, nonHeroParty);
				if (leadingStop)
				{
					behavior.StopQueueForParty(nonHeroParty, "tag_stop", out string stopFact);
					result.StopApplied = true;
					if (!string.IsNullOrWhiteSpace(stopFact))
					{
						generatedFacts.Add(stopFact);
					}
					notifications.Add(actorName + "已清空旧的大地图命令清单。");
				}
				List<PartyCommandEntry> nonHeroCommands = commands.Where(IsExecutableNonHeroPartyCommand).Select(CloneCommand).ToList();
				if (nonHeroCommands.Count == 0)
				{
					if (leadingStop)
					{
						result.Handled = true;
						return true;
					}
					notifications.Add("大地图命令失败：当前非英雄部队只能执行移动、巡逻、跟随、攻击等队伍级命令。");
					return false;
				}
				if (!behavior.TryAppendQueueForParty(nonHeroParty, nonHeroCommands, out string nonHeroFact, out string nonHeroMessage))
				{
					if (!string.IsNullOrWhiteSpace(nonHeroMessage))
					{
						notifications.Add(nonHeroMessage);
					}
					return false;
				}
				if (!string.IsNullOrWhiteSpace(nonHeroFact))
				{
					generatedFacts.Add(nonHeroFact);
				}
				notifications.Add(nonHeroMessage);
				result.Handled = true;
				result.AddedCommandCount = nonHeroCommands.Count;
				return true;
			}
			if (leadingStop)
			{
				behavior.StopQueue(targetHero, "tag_stop", out string stopFact);
				result.StopApplied = true;
				if (!string.IsNullOrWhiteSpace(stopFact))
				{
					generatedFacts.Add(stopFact);
				}
				notifications.Add(GetHeroName(targetHero) + "已清空旧的大地图命令清单。");
			}
			if (commands.Count == 0)
			{
				if (leadingStop)
				{
					result.Handled = true;
					return true;
				}
				notifications.Add("大地图命令失败：没有可执行的有效目标 ID。");
				return false;
			}
			if (IsHeroActuallyInPlayerMainPartyRoster(targetHero))
			{
				int firstTaskIndex = commands.FindIndex(IsTaskCommandForImplicitPartyCreation);
				if (firstTaskIndex >= 0)
				{
					List<PartyCommandEntry> createCommands = commands.Skip(firstTaskIndex).Select(CloneCommand).ToList();
					if (firstTaskIndex > 0)
					{
						notifications.Add("大地图命令顺序错误：建队前的归队命令无法执行，已从首个有效任务命令开始处理。");
					}
					bool opened = behavior.TryOpenCreateCompanionParty(targetHero, createCommands, out string createMessage);
					notifications.Add(createMessage);
					generatedFacts.Add("[AFEF NPC行为补充] " + GetHeroName(targetHero) + (opened
						? "接受了新的大地图任务；将先从玩家主队分兵，随后按输出顺序执行命令。"
						: "无法创建同伴部队：" + createMessage));
					result.Handled = opened;
					result.CompanionPartyCreationQueued = opened;
					result.AddedCommandCount = opened ? createCommands.Count : 0;
					return opened;
				}
			}
			if (targetHero.GovernorOf != null)
			{
				int firstTaskIndex = commands.FindIndex(IsTaskCommandForImplicitPartyCreation);
				if (firstTaskIndex < 0)
				{
					notifications.Add("大地图命令失败：驻城总督需要至少一道移动、巡逻、跟随或攻击任务才能组建远征队。");
					return false;
				}
				List<PartyCommandEntry> expeditionCommands = commands.Skip(firstTaskIndex).Select(CloneCommand).ToList();
				if (firstTaskIndex > 0)
				{
					notifications.Add("大地图命令顺序错误：总督建队前的归队命令无法执行，已从首个有效任务命令开始处理。");
				}
				bool accepted = behavior.TryStartGovernorExpeditionRequest(targetHero, expeditionCommands, out string expeditionMessage, out bool queuedForChannelExit);
				notifications.Add(expeditionMessage);
				generatedFacts.Add("[AFEF NPC行为补充] " + GetHeroName(targetHero) + (accepted
					? "接受了新的大地图任务；将从管辖地驻军抽调兵力组建临时远征队，任务结束后返城交还兵员并尝试复职。"
					: "无法组建总督远征队：" + expeditionMessage));
				result.Handled = accepted;
				result.GovernorExpeditionCreationQueued = accepted && queuedForChannelExit;
				result.AddedCommandCount = accepted ? expeditionCommands.Count : 0;
				return accepted;
			}
			if (!behavior.TryAppendQueue(targetHero, commands, out string fact, out string message))
			{
				if (!string.IsNullOrWhiteSpace(message))
				{
					notifications.Add(message);
				}
				return false;
			}
			if (!string.IsNullOrWhiteSpace(fact))
			{
				generatedFacts.Add(fact);
			}
			notifications.Add(message);
			result.Handled = true;
			result.AddedCommandCount = commands.Count;
			return true;
		}
		catch (Exception ex)
		{
			content = StripWorldMapOrderTags(original);
			notifications.Add("大地图命令处理失败：" + ex.Message);
			Log("apply tags failed: " + ex);
			return false;
		}
	}

	public static WorldMapOrderApplyResult ProcessWorldMapOrderTagsDispatch(Hero targetHero, ref string content)
	{
		return ProcessWorldMapOrderTagsDispatch(targetHero, null, -1, ref content);
	}

	public static WorldMapOrderApplyResult ProcessWorldMapOrderTagsDispatch(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content)
	{
		TryApplyWorldMapOrderTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out List<string> facts, out List<string> notifications, out WorldMapOrderApplyResult result);
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		if (result.Handled)
		{
			foreach (string fact in facts ?? new List<string>())
			{
				if (!string.IsNullOrWhiteSpace(fact))
				{
					if (targetHero != null)
					{
						MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
					}
					else if (TryResolveNonHeroPartyActorForExternal(targetCharacter, targetAgentIndex, out MobileParty party))
					{
						string memoryId = BuildPartyMemoryId(party);
						if (!string.IsNullOrWhiteSpace(memoryId))
						{
							MyBehavior.AppendExternalNonHeroDialogueHistory(memoryId, GetPartyName(party), null, null, fact);
						}
					}
				}
			}
		}
		foreach (string notification in notifications ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(notification))
			{
				InformationManager.DisplayMessage(new InformationMessage(notification, notification.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0 ? new Color(1f, 0.45f, 0.25f) : new Color(0.4f, 1f, 0.4f)));
			}
		}
		return result;
	}

	public bool TryIssueGoToSettlementForExternal(Hero hero, Settlement settlement, int holdDays, string sourceId, out string message)
	{
		return TryIssueGoToSettlementUntilDayForExternal(hero, settlement, holdDays, -1.0, sourceId, out message);
	}

	public bool TryIssueGoToSettlementUntilDayForExternal(Hero hero, Settlement settlement, int holdDays, double holdUntilDay, string sourceId, out string message)
	{
		message = "";
		string normalizedSource = NormalizeExternalSourceId(sourceId);
		if (string.IsNullOrWhiteSpace(normalizedSource))
		{
			message = "大地图命令失败：缺少外部命令来源。";
			return false;
		}
		if (hero == null || settlement == null)
		{
			message = "大地图命令失败：找不到有效的英雄或定居点。";
			return false;
		}
		lock (_queueLock)
		{
			if (_queues.TryGetValue(hero.StringId ?? "", out PartyCommandQueueState existing) && existing != null)
			{
				string existingSource = NormalizeExternalSourceId(existing.SourceId);
				if (!string.Equals(existingSource, normalizedSource, StringComparison.OrdinalIgnoreCase))
				{
					message = GetHeroName(hero) + "已有其它大地图命令，暂不能改派参加宴会。";
					return false;
				}
			}
		}
		List<PartyCommandEntry> commands = new List<PartyCommandEntry>
		{
			new PartyCommandEntry
			{
				Kind = CommandKind.GoToSettlement.ToString(),
				TargetType = "settlement",
				TargetId = settlement.StringId,
				Days = Math.Max(1, holdDays),
				HoldUntilDay = holdUntilDay > 0.0 ? holdUntilDay : -1.0,
				Mode = ""
			}
		};
		bool ok = TryReplaceQueue(hero, commands, normalizedSource, out _, out message);
		return ok;
	}

	public bool TryStopExternalCommandForExternal(Hero hero, string sourceId, out string message)
	{
		message = "";
		string normalizedSource = NormalizeExternalSourceId(sourceId);
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId) || string.IsNullOrWhiteSpace(normalizedSource))
		{
			message = "大地图命令停止失败：缺少英雄或命令来源。";
			return false;
		}
		PartyCommandQueueState state = null;
		lock (_queueLock)
		{
			_queues.TryGetValue(hero.StringId, out state);
		}
		if (state == null)
		{
			return true;
		}
		if (!string.Equals(NormalizeExternalSourceId(state.SourceId), normalizedSource, StringComparison.OrdinalIgnoreCase))
		{
			message = GetHeroName(hero) + "当前命令不属于该宴会来源，未停止。";
			return false;
		}
		StopQueue(hero, normalizedSource + ":stop_external", out _);
		return true;
	}

	private bool TryAppendQueue(Hero hero, List<PartyCommandEntry> commands, out string fact, out string message)
	{
		fact = "";
		message = "";
		if (hero == null || hero == Hero.MainHero || string.IsNullOrWhiteSpace(hero.StringId))
		{
			message = "大地图命令失败：不能这样指挥玩家本人的部队。";
			return false;
		}
		MobileParty party = ResolveActorParty(hero);
		if (party == null)
		{
			message = "大地图命令失败：" + GetHeroName(hero) + "当前没有独立可控制部队。";
			return false;
		}
		List<PartyCommandEntry> safeCommands = (commands ?? new List<PartyCommandEntry>()).Where(IsExecutableCommand).Select(CloneCommand).ToList();
		string rejectedMergeReason = "";
		if (safeCommands.Any(command => IsKind(command, CommandKind.MergeToPlayer)) && !CanMergeToPlayer(hero, party, out rejectedMergeReason))
		{
			safeCommands.RemoveAll(command => IsKind(command, CommandKind.MergeToPlayer));
			Log("rejected merge before queue actor=" + (hero.StringId ?? "") + " party=" + (party.StringId ?? "") + " reason=" + rejectedMergeReason);
		}
		if (safeCommands.Count == 0)
		{
			message = string.IsNullOrWhiteSpace(rejectedMergeReason)
				? "大地图命令失败：没有通过 ID 校验的命令。"
				: BuildMergeEligibilityFailureMessage(hero, rejectedMergeReason);
			return false;
		}
		safeCommands = ConvertGoToSettlementCommandsToAttacks(hero, party, safeCommands, out int hostileSettlementConvertedCount, out int besiegedSettlementConvertedCount);
		TryReactivateGovernorExpedition(hero, party);
		PartyCommandQueueState state;
		bool startNew = false;
		lock (_queueLock)
		{
			_queues.TryGetValue(hero.StringId, out state);
			if (IsStopPending(state))
			{
				message = GetHeroName(hero) + "正在等待当前战斗安全结算，暂不能追加新的大地图命令。";
				return false;
			}
			if (state != null && !string.IsNullOrWhiteSpace(NormalizeExternalSourceId(state.SourceId)))
			{
				message = GetHeroName(hero) + "当前执行的是隔离来源命令，聊天命令未改动该清单。";
				return false;
			}
			if (state == null || state.Commands == null || state.Commands.Count == 0 || state.CurrentIndex >= state.Commands.Count)
			{
				state = new PartyCommandQueueState
				{
					HeroId = hero.StringId,
					ActorKey = hero.StringId,
					ActorName = GetHeroName(hero),
					PartyStringId = party.StringId,
					PartyIndex = GetPartyIndexSafe(party),
					Commands = safeCommands,
					CurrentIndex = 0,
					Stage = CommandStage.New.ToString(),
					SourceId = ""
				};
				_queues[hero.StringId] = state;
				startNew = true;
			}
			else
			{
				state.Commands.AddRange(safeCommands);
			}
		}
		if (startNew)
		{
			LeaveArmyIfNeeded(party);
			ReleasePartyAi(party);
			StartCurrentCommand(hero, party, state);
		}
		string convertedText = BuildGoToSettlementConversionSummary(hostileSettlementConvertedCount, besiegedSettlementConvertedCount);
		fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + (startNew ? "建立了" : "向现有清单末尾追加了") + safeCommands.Count + "道大地图命令" + convertedText + "。";
		message = GetHeroName(hero) + (startNew ? "已开始执行" : "已追加") + safeCommands.Count + "道大地图命令。";
		if (!string.IsNullOrWhiteSpace(rejectedMergeReason))
		{
			message += " " + BuildMergeEligibilityFailureMessage(hero, rejectedMergeReason);
		}
		return true;
	}

	private bool TryAppendQueueForParty(MobileParty party, List<PartyCommandEntry> commands, out string fact, out string message)
	{
		fact = "";
		message = "";
		if (!IsValidNonHeroPartyFallbackParty(party))
		{
			message = "大地图命令失败：当前非英雄说话对象没有可接管的野外部队。";
			return false;
		}
		string actorKey = BuildPartyActorKey(party, createGuid: true);
		List<PartyCommandEntry> safeCommands = (commands ?? new List<PartyCommandEntry>()).Where(IsExecutableNonHeroPartyCommand).Select(CloneCommand).ToList();
		if (string.IsNullOrWhiteSpace(actorKey) || safeCommands.Count == 0)
		{
			message = "大地图命令失败：无法建立稳定的非英雄部队命令清单。";
			return false;
		}
		safeCommands = ConvertGoToSettlementCommandsToAttacks(null, party, safeCommands, out int hostileSettlementConvertedCount, out int besiegedSettlementConvertedCount);
		PartyCommandQueueState state;
		bool startNew = false;
		string actorName = GetPartyName(party);
		lock (_queueLock)
		{
			_queues.TryGetValue(actorKey, out state);
			if (IsStopPending(state))
			{
				message = actorName + "正在等待当前战斗安全结算，暂不能追加新的大地图命令。";
				return false;
			}
			if (state != null && !string.IsNullOrWhiteSpace(NormalizeExternalSourceId(state.SourceId)))
			{
				message = actorName + "当前执行的是隔离来源命令，聊天命令未改动该清单。";
				return false;
			}
			if (state == null || state.Commands == null || state.Commands.Count == 0 || state.CurrentIndex >= state.Commands.Count)
			{
				state = new PartyCommandQueueState
				{
					HeroId = "",
					ActorKey = actorKey,
					ActorName = actorName,
					PartyStringId = party.StringId,
					PartyIndex = GetPartyIndexSafe(party),
					NonHeroMemoryId = BuildPartyMemoryId(party),
					NonHeroMemoryName = actorName,
					Commands = safeCommands,
					CurrentIndex = 0,
					Stage = CommandStage.New.ToString(),
					SourceId = ""
				};
				_queues[actorKey] = state;
				startNew = true;
			}
			else
			{
				state.Commands.AddRange(safeCommands);
			}
		}
		if (startNew)
		{
			LeaveArmyIfNeeded(party);
			ReleasePartyAi(party);
			StartCurrentCommand(null, party, state);
		}
		string convertedText = BuildGoToSettlementConversionSummary(hostileSettlementConvertedCount, besiegedSettlementConvertedCount);
		fact = "[AFEF NPC行为补充] " + actorName + (startNew ? "建立了" : "向现有清单末尾追加了") + safeCommands.Count + "道大地图命令" + convertedText + "。";
		message = actorName + (startNew ? "已开始执行" : "已追加") + safeCommands.Count + "道大地图命令。";
		return true;
	}

	private bool TryReplaceQueue(Hero hero, List<PartyCommandEntry> commands, string sourceId, out string fact, out string message)
	{
		fact = "";
		message = "";
		if (hero == null || hero == Hero.MainHero)
		{
			message = "大地图命令失败：不能这样指挥玩家本人的部队。";
			return false;
		}
		string normalizedSource = NormalizeExternalSourceId(sourceId);
		List<PartyCommandEntry> safeCommands = (commands ?? new List<PartyCommandEntry>()).Where(IsExecutableCommand).Select(CloneCommand).ToList();
		if (safeCommands.Count == 0)
		{
			message = "大地图命令失败：没有通过 ID 校验的命令。";
			return false;
		}
		MobileParty party = ResolveActorParty(hero);
		if (party == null)
		{
			message = "大地图命令失败：" + GetHeroName(hero) + "当前没有独立可控制部队。";
			return false;
		}
		PartyCommandQueueState existingState = null;
		lock (_queueLock)
		{
			_queues.TryGetValue(hero.StringId ?? "", out existingState);
		}
		if (IsStopPending(existingState))
		{
			message = GetHeroName(hero) + "正在等待当前战斗安全结算，暂不能替换大地图命令。";
			return false;
		}
		if (HasFollowSiegeState(existingState) && !TryExitFollowSiegeControl(party, existingState, detachPreexistingParticipation: false, "replace_queue"))
		{
			message = GetHeroName(hero) + "正在参与原版战斗，结算前不能替换当前大地图命令。";
			return false;
		}
		TryReactivateGovernorExpedition(hero, party);
		int hostileGoToConvertedCount = 0;
		int besiegedGoToConvertedCount = 0;
		if (ShouldConvertGoToSettlementCommands(normalizedSource))
		{
			safeCommands = ConvertGoToSettlementCommandsToAttacks(hero, party, safeCommands, out hostileGoToConvertedCount, out besiegedGoToConvertedCount);
		}
		LeaveArmyIfNeeded(party);
		ReleasePartyAi(party);
		PartyCommandQueueState state = new PartyCommandQueueState
		{
			HeroId = hero.StringId,
			ActorKey = hero.StringId,
			ActorName = GetHeroName(hero),
			PartyStringId = party?.StringId,
			PartyIndex = GetPartyIndexSafe(party),
			Commands = safeCommands,
			CurrentIndex = 0,
			Stage = CommandStage.New.ToString(),
			SourceId = normalizedSource
		};
		lock (_queueLock)
		{
			_queues[hero.StringId] = state;
		}
		StartCurrentCommand(hero, party, state);
		string conversionFact = BuildGoToSettlementConversionSummary(hostileGoToConvertedCount, besiegedGoToConvertedCount);
		string conversionMessage = BuildGoToSettlementConversionMessage(hostileGoToConvertedCount, besiegedGoToConvertedCount);
		fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "接受了玩家的大地图命令队列，共" + safeCommands.Count + "道命令" + conversionFact + "。";
		message = GetHeroName(hero) + "已接受大地图命令队列（" + safeCommands.Count + "道" + conversionMessage + "）。";
		return true;
	}

	private void StopQueue(Hero hero, string reason, out string fact)
	{
		fact = "";
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return;
		}
		PartyCommandQueueState state;
		bool removedPendingCreate;
		bool removedPendingGovernorExpedition;
		lock (_queueLock)
		{
			_queues.TryGetValue(hero.StringId, out state);
			removedPendingCreate = _pendingCreatePartyRequests.Remove(hero.StringId);
			removedPendingGovernorExpedition = _pendingGovernorExpeditionRequests.Remove(hero.StringId);
			Volatile.Write(ref _hasPendingCreateCompanionPartyRequests, _pendingCreatePartyRequests.Count > 0 ? 1 : 0);
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, _pendingGovernorExpeditionRequests.Count > 0 ? 1 : 0);
		}
		MobileParty party = ResolvePartyForSafeExit(state, hero);
		bool alreadyPendingStop = IsStopPending(state);
		if (state != null && HasFollowSiegeState(state) && !TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: false, "stop:" + reason))
		{
			SetPendingSafeExit(state, PendingSafeExitStop, reason);
			RequestFollowSiegeRefresh();
			fact = alreadyPendingStop ? "" : "[AFEF NPC行为补充] " + GetHeroName(hero) + "停止了当前大地图命令；正在进行的原版战斗结算后将安全退出攻城并回归原版行动状态。";
			Log("stop deferred for active follow siege hero=" + hero.StringId + " reason=" + reason + " removedPendingCreate=" + removedPendingCreate + " removedPendingGovernor=" + removedPendingGovernorExpedition);
			return;
		}
		if (party != null && party != MobileParty.MainParty)
		{
			AbortCurrentCommandIfNeeded(party, state);
		}
		lock (_queueLock)
		{
			_queues.Remove(hero.StringId);
		}
		if (BeginGovernorExpeditionReturn(hero, party, "stop:" + reason))
		{
			if (TryGetGovernorExpeditionForHero(hero.StringId, out GovernorExpeditionRecord returningRecord)
				&& string.Equals(returningRecord.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase))
			{
				fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "停止了当前大地图命令，临时总督远征队已开始返驻地交还兵员。";
			}
			else if (returningRecord != null && string.Equals(returningRecord.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase))
			{
				fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "的临时总督远征队已完成资产安置，正在等待安全回收空队并尝试复职。";
			}
			Log("stop governor expedition hero=" + hero.StringId + " reason=" + reason);
			return;
		}
		if (party != null && party != MobileParty.MainParty)
		{
			ReleasePartyAi(party);
		}
		if (removedPendingGovernorExpedition && state == null)
		{
			fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "取消了尚未建队的总督远征请求，驻军和总督状态均未改变。";
			Log("stop pending governor expedition hero=" + hero.StringId + " reason=" + reason);
			return;
		}
		fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "停止了当前大地图命令，回归原版行动状态。";
		Log("stop hero=" + hero.StringId + " reason=" + reason + " removedPendingCreate=" + removedPendingCreate + " removedPendingGovernor=" + removedPendingGovernorExpedition);
	}

	private void StopQueueForParty(MobileParty party, string reason, out string fact)
	{
		fact = "";
		if (!IsValidNonHeroPartyFallbackParty(party))
		{
			return;
		}
		string actorKey = BuildPartyActorKey(party, createGuid: false);
		if (string.IsNullOrWhiteSpace(actorKey))
		{
			actorKey = BuildPartyActorKey(party, createGuid: true);
		}
		PartyCommandQueueState state = null;
		lock (_queueLock)
		{
			if (!string.IsNullOrWhiteSpace(actorKey))
			{
				_queues.TryGetValue(actorKey, out state);
			}
		}
		if (state != null)
		{
			bool alreadyPendingStop = IsStopPending(state);
			if (HasFollowSiegeState(state) && !TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: false, "stop:" + reason))
			{
				SetPendingSafeExit(state, PendingSafeExitStop, reason);
				RequestFollowSiegeRefresh();
				string pendingActorName = GetActorName(state, null, party);
				fact = alreadyPendingStop ? "" : "[AFEF NPC行为补充] " + pendingActorName + "停止了当前大地图命令；正在进行的原版战斗结算后将安全退出攻城并回归原版行动状态。";
				Log("stop deferred for active follow siege party_actor=" + actorKey + " reason=" + reason);
				return;
			}
			AbortCurrentCommandIfNeeded(party, state);
			ReleasePartyAi(party);
		}
		lock (_queueLock)
		{
			if (!string.IsNullOrWhiteSpace(actorKey))
			{
				_queues.Remove(actorKey);
			}
		}
		string actorName = GetActorName(state, null, party);
		fact = "[AFEF NPC行为补充] " + actorName + "停止了当前大地图命令，回归原版行动状态。";
		Log("stop party_actor=" + actorKey + " reason=" + reason);
	}

	private void OnHourlyTickParty(MobileParty party)
	{
		try
		{
			Hero hero = party?.LeaderHero;
			PartyCommandQueueState state = null;
			lock (_queueLock)
			{
				if (hero != null && !string.IsNullOrWhiteSpace(hero.StringId))
				{
					_queues.TryGetValue(hero.StringId, out state);
				}
				if (state == null)
				{
					string partyActorKey = BuildPartyActorKey(party, createGuid: false);
					if (!string.IsNullOrWhiteSpace(partyActorKey))
					{
						_queues.TryGetValue(partyActorKey, out state);
					}
				}
			}
			if (state != null)
			{
				if (string.IsNullOrWhiteSpace(state.HeroId))
				{
					hero = null;
				}
				ProcessQueueTick(hero, party, state);
				return;
			}
			if (Volatile.Read(ref _hasGovernorExpeditions) == 0
				|| !TryGetGovernorExpeditionForParty(party, out GovernorExpeditionRecord expeditionRecord))
			{
				return;
			}
			hero = ResolveHeroByIdAny(expeditionRecord.HeroId);
			if (string.Equals(expeditionRecord.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(expeditionRecord.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase))
			{
				ProcessGovernorExpeditionReturnTick(hero, party, expeditionRecord);
				return;
			}
			BeginGovernorExpeditionReturn(hero, party, "active_without_queue");
		}
		catch (Exception ex)
		{
			Log("hourly tick failed: " + ex);
		}
	}

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnCampaignTick"))
		{
		try
		{
			ProcessPendingGovernorExpeditionReconcile();
			ProcessPendingFollowSiegeRefresh();
			ProcessPendingCreateCompanionPartyRequests();
			ProcessPendingGovernorExpeditionRequests();
			double nowDay = NowDay();
			if (nowDay >= _nextDetachedPartyPruneDay)
			{
				_nextDetachedPartyPruneDay = nowDay + 1.0 / 24.0;
				PruneInvalidPlayerDetachedParties();
			}
			if (Volatile.Read(ref _hasForeignClanGuests) != 0 && nowDay >= _nextForeignClanGuestReconcileDay)
			{
				_nextForeignClanGuestReconcileDay = nowDay + 1.0 / 24.0;
				ReconcileForeignClanGuests();
			}
		}
		catch (Exception ex)
		{
			Log("world map command campaign tick failed: " + ex);
		}
		}
	}

	private void ReconcileForeignClanGuests()
	{
		List<ForeignClanGuestRecord> snapshot;
		lock (_queueLock)
		{
			snapshot = _foreignClanGuests.Values.Where(record => record != null).ToList();
		}
		foreach (ForeignClanGuestRecord record in snapshot)
		{
			Hero guest = ResolveHeroByIdAny(record.HeroId);
			if (guest == null || guest.IsDead || !guest.IsActive || guest.Clan == null
				|| !IsForeignClanGuestHero(guest) || !IsForeignClanGuestPlacementValid(guest))
			{
				RemoveForeignClanGuestRecord(record.HeroId, "guest_no_longer_in_player_party_or_foreign_clan");
				continue;
			}
			string currentClanId = guest.Clan.StringId ?? "";
			if (!string.Equals(currentClanId, record.ClanId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				lock (_queueLock)
				{
					if (_foreignClanGuests.TryGetValue(record.HeroId, out ForeignClanGuestRecord current) && current != null)
					{
						current.ClanId = currentClanId;
					}
				}
				Log("foreign clan guest clan updated hero=" + record.HeroId + " clan=" + currentClanId);
			}
			if (!ArePlayerAndClanAtWar(guest.Clan))
			{
				continue;
			}
			MobileParty guestParty = guest.PartyBelongedTo;
			bool leftPlayerControl = IsHeroActuallyInPlayerMainPartyRoster(guest)
				? TryEjectForeignClanGuestAfterWar(guest)
				: TryReleaseForeignClanGuestDetachmentAfterWar(guest, guestParty);
			if (leftPlayerControl)
			{
				RemoveForeignClanGuestRecord(record.HeroId, "war_started");
			}
		}
	}

	private static bool TryEjectForeignClanGuestAfterWar(Hero guest)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (guest == null || mainParty == null || guest.PartyBelongedTo != mainParty
				|| HasActiveMapEvent(mainParty) || mainParty.DefaultBehavior == AiBehavior.EngageParty
				|| PlayerEncounter.Current != null || Mission.Current != null)
			{
				return false;
			}
			Settlement destination = ResolveForeignClanGuestReturnSettlement(guest);
			if (destination != null)
			{
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(guest, destination);
			}
			else
			{
				MakeHeroFugitiveAction.Apply(guest, showNotification: false);
			}
			if (guest.PartyBelongedTo == mainParty || IsHeroActuallyInPlayerMainPartyRoster(guest))
			{
				return false;
			}
			string destinationText = destination == null ? "原家族势力范围" : GetSettlementName(destination);
			LogFact(guest, GetHeroName(guest) + "所属势力已与玩家开战，因此结束外族客军随队状态并离开玩家主队，返回" + destinationText + "；此前已并入玩家主队的普通兵员和物资不自动撤回。");
			DisplayCommandMessage(GetHeroName(guest) + "所属势力已与玩家开战，该外族客军 Hero 已离开玩家主队。", CommandMessageTone.Failure);
			return true;
		}
		catch (Exception ex)
		{
			Log("eject foreign clan guest failed hero=" + (guest?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private bool TryReleaseForeignClanGuestDetachmentAfterWar(Hero guest, MobileParty party)
	{
		try
		{
			if (guest == null || !IsRegisteredPlayerDetachment(guest, party)
				|| HasActiveMapEvent(party) || party.DefaultBehavior == AiBehavior.EngageParty || party.BesiegerCamp != null
				|| PlayerEncounter.Current != null || Mission.Current != null)
			{
				return false;
			}
			StopQueue(guest, "foreign_guest_war_started", out _);
			RemovePlayerDetachedParty(guest, party, "foreign_guest_war_started");
			ReleasePartyAi(party);
			if (IsRegisteredPlayerDetachment(guest, party))
			{
				return false;
			}
			LogFact(guest, GetHeroName(guest) + "所属势力已与玩家开战，因此其外族客军分队已脱离玩家指挥并回归原家族的原版AI；该分队现有兵员和物资不自动转回玩家主队。");
			DisplayCommandMessage(GetHeroName(guest) + "所属势力已与玩家开战，其外族客军分队已脱离玩家指挥。", CommandMessageTone.Failure);
			return true;
		}
		catch (Exception ex)
		{
			Log("release foreign clan guest detachment failed hero=" + (guest?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static Settlement ResolveForeignClanGuestReturnSettlement(Hero guest)
	{
		try
		{
			Clan clan = guest?.Clan;
			Settlement clanSettlement = clan?.Settlements?.FirstOrDefault(settlement => settlement != null);
			if (clanSettlement != null)
			{
				return clanSettlement;
			}
			Settlement home = guest?.HomeSettlement;
			if (home != null)
			{
				return home;
			}
			return guest?.BornSettlement;
		}
		catch
		{
			return null;
		}
	}

	private void RemoveForeignClanGuestRecord(string heroId, string reason)
	{
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return;
		}
		bool removed;
		lock (_queueLock)
		{
			removed = _foreignClanGuests.Remove(heroId);
			Volatile.Write(ref _hasForeignClanGuests, _foreignClanGuests.Count > 0 ? 1 : 0);
		}
		if (removed)
		{
			Log("removed foreign clan guest hero=" + heroId + " reason=" + (reason ?? ""));
		}
	}

	private void OnFollowSiegeEventChanged(SiegeEvent siegeEvent)
	{
		RequestFollowSiegeRefresh();
	}

	private void OnFollowSiegePartyChanged(MobileParty party)
	{
		RequestFollowSiegeRefresh();
	}

	private void RequestFollowSiegeRefresh()
	{
		Volatile.Write(ref _hasPendingFollowSiegeRefresh, 1);
	}

	private void ProcessPendingFollowSiegeRefresh()
	{
		if (Interlocked.Exchange(ref _hasPendingFollowSiegeRefresh, 0) == 0)
		{
			return;
		}
		List<PartyCommandQueueState> snapshot;
		lock (_queueLock)
		{
			snapshot = _queues.Values
				.Where(x => x != null && (IsCurrentFollowCommand(x) || HasPendingSafeExit(x)))
				.ToList();
		}
		foreach (PartyCommandQueueState state in snapshot)
		{
			try
			{
				if (!IsStateStillQueued(state))
				{
					continue;
				}
				Hero hero = ResolveHeroByIdAny(state.HeroId);
				MobileParty party = HasPendingSafeExit(state) ? ResolvePartyForSafeExit(state, hero) : ResolveActorParty(state, hero);
				ProcessQueueTick(string.IsNullOrWhiteSpace(state.HeroId) ? null : hero, party, state);
			}
			catch (Exception ex)
			{
				Log("follow siege refresh failed actor=" + GetActorLogId(state, null, null) + " error=" + ex.Message);
			}
		}
	}

	private bool IsStateStillQueued(PartyCommandQueueState state)
	{
		string queueKey = GetQueueKey(state);
		if (string.IsNullOrWhiteSpace(queueKey))
		{
			return false;
		}
		lock (_queueLock)
		{
			return _queues.TryGetValue(queueKey, out PartyCommandQueueState current) && ReferenceEquals(current, state);
		}
	}

	private void ProcessPendingCreateCompanionPartyRequests()
	{
		if (Volatile.Read(ref _hasPendingCreateCompanionPartyRequests) == 0)
		{
			return;
		}
		lock (_queueLock)
		{
			if (_pendingCreatePartyRequests.Count == 0)
			{
				return;
			}
		}
		if (_isOpeningCreateCompanionPartyScreen || !CanOpenCreateCompanionPartyScreenNow(out _))
		{
			return;
		}
		PendingCreateCompanionPartyRequest request = null;
		lock (_queueLock)
		{
			if (_pendingCreatePartyRequests.Count == 0)
			{
				return;
			}
			request = _pendingCreatePartyRequests.Values.FirstOrDefault();
			if (request != null)
			{
				_pendingCreatePartyRequests.Remove(request.HeroId ?? "");
			}
			Volatile.Write(ref _hasPendingCreateCompanionPartyRequests, _pendingCreatePartyRequests.Count > 0 ? 1 : 0);
		}
		if (request == null || string.IsNullOrWhiteSpace(request.HeroId))
		{
			return;
		}
		Hero hero = ResolveHeroByIdAny(request.HeroId);
		if (hero == null)
		{
			Log("pending create skipped missing hero=" + request.HeroId);
			return;
		}
		if (!TryOpenCreateCompanionParty(hero, request.FollowUpCommands, out string message))
		{
			LogFact(hero, GetHeroName(hero) + "无法创建同伴部队：" + message);
		}
	}

	private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnMobilePartyDestroyed"))
		{
		PerfProbe.MarkEvent("WorldMapCommand.MobilePartyDestroyed");
		try
		{
			string heroId = destroyedParty?.LeaderHero?.StringId;
			bool trackedGovernorExpedition = TryGetGovernorExpeditionForParty(destroyedParty, out GovernorExpeditionRecord destroyedExpedition);
			if (trackedGovernorExpedition)
			{
				heroId = string.IsNullOrWhiteSpace(heroId) ? destroyedExpedition.HeroId : heroId;
			}
			string partyId = destroyedParty?.StringId;
			string destroyedActorKey = BuildPartyActorKey(destroyedParty, createGuid: false);
			PartyCommandQueueState actorState = null;
			lock (_queueLock)
			{
				if (!string.IsNullOrWhiteSpace(heroId))
				{
					_queues.TryGetValue(heroId, out actorState);
				}
				if (actorState == null && !string.IsNullOrWhiteSpace(destroyedActorKey))
				{
					_queues.TryGetValue(destroyedActorKey, out actorState);
				}
			}
			List<PartyCommandQueueState> activeAttackStates = GetActiveAttackStatesSnapshot();
			if (trackedGovernorExpedition)
			{
				RemoveGovernorExpeditionRecord(destroyedExpedition.HeroId, "party_destroyed", removeQueue: true);
			}
			RemovePlayerDetachedParty(destroyedParty?.LeaderHero, destroyedParty, "party_destroyed");
			if (actorState == null && activeAttackStates.Count == 0)
			{
				return;
			}
			bool handled = actorState != null;
			string actorStateKey = GetQueueKey(actorState);
			if (actorState != null)
			{
				if (actorState != null && IsCurrentAttackCommand(actorState))
				{
					LogTerminalAttackFailure(actorState, "执行者部队已被消灭。", "actor_party_destroyed");
				}
				else
				{
					lock (_queueLock)
					{
						_queues.Remove(actorStateKey);
					}
				}
			}
			foreach (PartyCommandQueueState state in activeAttackStates)
			{
				if (state == null || string.Equals(GetQueueKey(state), actorStateKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null)
				{
					continue;
				}
				if (IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command) && !string.IsNullOrWhiteSpace(heroId) && string.Equals(command.TargetId, heroId, StringComparison.OrdinalIgnoreCase))
				{
					bool actorDestroyedTarget = PartyBaseMatchesActor(destroyerParty, state);
					TryCompleteCurrentAttackResult(state, actorDestroyedTarget ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, actorDestroyedTarget ? "目标部队已被击溃。" : "目标部队已经被消灭或解散。", actorDestroyedTarget ? "target_party_destroyed_by_actor" : "target_party_destroyed");
					continue;
				}
				if (IsKind(command, CommandKind.AttackParty) && !string.IsNullOrWhiteSpace(partyId) && string.Equals(command.TargetId, partyId, StringComparison.OrdinalIgnoreCase))
				{
					bool actorDestroyedTarget = PartyBaseMatchesActor(destroyerParty, state) || PartyBaseMatchesFaction(destroyerParty, state.ResultActorFactionId);
					TryCompleteCurrentAttackResult(state, actorDestroyedTarget ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, actorDestroyedTarget ? "目标部队已被击溃。" : "目标部队已经被消灭或解散。", actorDestroyedTarget ? "target_mobile_party_destroyed_by_actor" : "target_mobile_party_destroyed");
				}
			}
			if (handled)
			{
				Log("mobile party destroyed hero=" + (heroId ?? "") + " party=" + (partyId ?? ""));
			}
		}
		catch (Exception ex)
		{
			Log("party destroyed handling failed: " + ex.Message);
		}
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnMapEventEnded"))
		{
		PerfProbe.MarkEvent("WorldMapCommand.MapEventEnded");
		RequestFollowSiegeRefresh();
		try
		{
			if (mapEvent == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot())
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || IsSettlementTarget(command) || (!IsKind(command, CommandKind.AttackHero) && !IsKind(command, CommandKind.AttackParty)))
				{
					continue;
				}
				BattleSideEnum actorSide = GetActorSideInMapEvent(mapEvent, state);
				BattleSideEnum targetSide = IsKind(command, CommandKind.AttackParty) ? GetPartySideInMapEvent(mapEvent, command.TargetId) : GetHeroSideInMapEvent(mapEvent, command.TargetId);
				if (actorSide == BattleSideEnum.None || targetSide == BattleSideEnum.None || actorSide == targetSide)
				{
					continue;
				}
				if (!mapEvent.HasWinner)
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "战斗已经结束，但原版事件没有明确胜负。", "map_event_no_winner");
					continue;
				}
				bool won = mapEvent.WinningSide == actorSide;
				string detail = won ? (GetStoredTargetName(state, command) + "的部队被击败。") : (GetStoredActorName(state) + "的部队被击退。");
				detail += BuildMapEventCasualtySummary(mapEvent, actorSide, targetSide);
				TryCompleteCurrentAttackResult(state, won ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, won ? "map_event_attack_success" : "map_event_attack_failure");
			}
		}
		catch (Exception ex)
		{
			Log("map event result handling failed: " + ex.Message);
		}
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		using (PerfProbe.Scope("WorldMapCommand.OnHeroPrisonerTaken"))
		{
		PerfProbe.MarkEvent("WorldMapCommand.HeroPrisonerTaken");
		try
		{
			string prisonerId = prisoner?.StringId;
			if (string.IsNullOrWhiteSpace(prisonerId))
			{
				return;
			}
			List<PartyCommandQueueState> activeAttackStates = GetActiveAttackStatesSnapshot();
			if (TryGetGovernorExpeditionForHero(prisonerId, out GovernorExpeditionRecord expeditionRecord))
			{
				MobileParty expeditionParty = ResolveGovernorExpeditionParty(expeditionRecord);
				RemoveGovernorExpeditionRecord(prisonerId, "hero_prisoner_taken", removeQueue: true);
				ReleasePartyAi(expeditionParty);
				Log("governor expedition cleared after capture hero=" + prisonerId);
			}
			foreach (PartyCommandQueueState state in activeAttackStates)
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsKind(command, CommandKind.AttackHero) || IsSettlementTarget(command))
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(state.HeroId) && string.Equals(prisonerId, state.HeroId, StringComparison.OrdinalIgnoreCase))
				{
					LogTerminalAttackFailure(state, "执行者已经被俘，攻击命令失败。", "actor_prisoner_taken");
					continue;
				}
				if (!string.Equals(prisonerId, command.TargetId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (PartyBaseMatchesActor(capturer, state) || PartyBaseMatchesFaction(capturer, state.ResultActorFactionId))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, GetStoredTargetName(state, command) + "已被俘，目标部队被击败。", "target_prisoner_taken");
				}
			}
		}
		catch (Exception ex)
		{
			Log("prisoner result handling failed: " + ex.Message);
		}
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			string victimId = victim?.StringId;
			if (string.IsNullOrWhiteSpace(victimId)
				|| !TryGetGovernorExpeditionForHero(victimId, out GovernorExpeditionRecord expeditionRecord))
			{
				return;
			}
			PartyCommandQueueState victimState = null;
			lock (_queueLock)
			{
				_queues.TryGetValue(victimId, out victimState);
			}
			MobileParty expeditionParty = ResolveGovernorExpeditionParty(expeditionRecord);
			RemoveGovernorExpeditionRecord(victimId, "hero_killed", removeQueue: true);
			ReleasePartyAi(expeditionParty);
			if (IsCurrentAttackCommand(victimState))
			{
				LogTerminalAttackFailure(victimState, "执行者已经死亡，攻击命令失败。", "actor_killed");
			}
			Log("governor expedition cleared after death hero=" + victimId);
		}
		catch (Exception ex)
		{
			Log("governor expedition death handling failed: " + ex.Message);
		}
	}

	private void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, MapEvent.BattleTypes battleType)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("siege"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				if (!PartyMatchesActor(party, state) && !PartyMatchesFaction(party, state.ResultActorFactionId))
				{
					continue;
				}
				string detail = siegeSuccess ? (GetSettlementName(settlement) + "已经被攻下。") : "攻城方未能攻下目标。";
				TryCompleteCurrentAttackResult(state, siegeSuccess ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, siegeSuccess ? "siege_completed_success" : "siege_completed_failure");
			}
		}
		catch (Exception ex)
		{
			Log("siege result handling failed: " + ex.Message);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("siege"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				bool actorFactionCaptured = string.Equals(SafeFactionId(newOwner?.MapFaction), state.ResultActorFactionId, StringComparison.OrdinalIgnoreCase) || string.Equals(SafeFactionId(settlement.MapFaction), state.ResultActorFactionId, StringComparison.OrdinalIgnoreCase);
				bool actorCaptured = !string.IsNullOrWhiteSpace(state.HeroId) && string.Equals(capturerHero?.StringId, state.HeroId, StringComparison.OrdinalIgnoreCase);
				if (actorFactionCaptured || actorCaptured)
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, GetSettlementName(settlement) + "已经被攻下并易主。", "settlement_owner_changed_success");
				}
			}
		}
		catch (Exception ex)
		{
			Log("settlement owner result handling failed: " + ex.Message);
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		try
		{
			Settlement settlement = raidEvent?.MapEventSettlement;
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("raid"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				if (!MapEventSideHasActor(raidEvent.AttackerSide, state))
				{
					continue;
				}
				bool success = winnerSide == BattleSideEnum.Attacker || IsVillageLooted(settlement);
				if (!success && TryKeepRaidCommandAliveAfterRaidEnded(state, command, settlement, raidEvent, "raid_completed_before_loot"))
				{
					continue;
				}
				string detail = success ? "村庄已被洗劫。" : "守军击退了袭掠，村庄没有被洗劫。";
				TryCompleteCurrentAttackResult(state, success ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, success ? "raid_completed_success" : "raid_completed_failure");
			}
		}
		catch (Exception ex)
		{
			Log("raid result handling failed: " + ex.Message);
		}
	}

	private void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
		try
		{
			if (village?.Settlement == null || newState != Village.VillageStates.Looted)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("raid"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, village.Settlement))
				{
					continue;
				}
				if (!PartyMatchesActor(raiderParty, state) && !PartyMatchesFaction(raiderParty, state.ResultActorFactionId))
				{
					continue;
				}
				TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, "村庄已进入被洗劫状态。", "village_looted_success");
			}
		}
		catch (Exception ex)
		{
			Log("village state result handling failed: " + ex.Message);
		}
	}

	private void ProcessQueueTick(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		NormalizeState(state);
		if (ProcessPendingSafeExit(hero, party, state))
		{
			return;
		}
		if (!ValidateActor(state, hero, party, out string reason))
		{
			if (IsCurrentAttackCommand(state))
			{
				TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, "执行者已经失去可控制部队或被俘，命令失败。", "actor_invalid:" + reason);
				return;
			}
			FinishQueue(hero, party, state, "actor_invalid:" + reason, appendFact: true);
			return;
		}
		if (state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		PartyCommandEntry command = state.Commands[state.CurrentIndex];
		if (command == null || !IsExecutableCommand(command))
		{
			AdvanceCommand(hero, party, state, "invalid_command");
			return;
		}
		CommandStage stage = ParseStage(state.Stage);
		if (stage == CommandStage.New)
		{
			StartCurrentCommand(hero, party, state);
			return;
		}
		double now = NowDay();
		if (IsFollowCommand(command) && HasFollowDurationElapsed(state, command, now))
		{
			LogFact(state, hero, BuildFollowCompletedFact(state, hero, party, command));
			AdvanceCommand(hero, party, state, IsKind(command, CommandKind.FollowParty) ? "follow_party_done" : "follow_done");
			return;
		}
		if (state.TimeoutDay > 0.0 && now > state.TimeoutDay)
		{
			if (TryKeepCommandAliveAfterTimeout(hero, party, state, command, now))
			{
				Log("timeout deferred hero=" + (hero?.StringId ?? "") + " index=" + state.CurrentIndex + " kind=" + (command?.Kind ?? "") + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			}
			else
			{
				if (IsKind(command, CommandKind.AttackHero) || IsKind(command, CommandKind.AttackParty))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, BuildAttackTimeoutDetail(command, state), "timeout");
					return;
				}
				LogFact(state, hero, BuildCommandTimeoutFact(state, hero, command));
				AdvanceCommand(hero, party, state, "timeout");
				return;
			}
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			TickGoToSettlement(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			TickPatrolSettlement(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			TickFollowHero(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			TickFollowParty(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			TickAttackHero(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			TickAttackParty(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			TickMergeToPlayer(hero, party, state, command);
			return;
		}
	}

	private void StartCurrentCommand(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		if (state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		PartyCommandEntry command = state.Commands[state.CurrentIndex];
		if (TryConvertCurrentGoToSettlementCommand(hero, party, state, command, "start"))
		{
			command = state.Commands[state.CurrentIndex];
		}
		ResetResultTracking(state);
		state.ArmySurvivalRenewalKey = "";
		state.ArmySurvivalLastPaidDay = -1.0;
		state.CommandStartDay = NowDay();
		state.ArrivalDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.LastStatusMessageKey = "";
		state.MergeTransferFailureCount = 0;
		state.MergeRetryAfterDay = -1.0;
		state.TimeoutDay = ComputeTimeoutDay(party, command);
		bool isFollowCommand = IsFollowCommand(command);
		if (!isFollowCommand && !PreemptBlockingWorldActivityForCommand(hero, party, command, state, "start"))
		{
			return;
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			if (settlement == null)
			{
				AdvanceCommand(hero, party, state, "settlement_missing");
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "visit:" + settlement.StringId;
			if (!ShouldSuppressCommandMessages(state))
			{
				DisplayCommandMessage(BuildGoToSettlementStartMessage(state, hero, party, settlement, command), CommandMessageTone.Progress);
			}
			Log("start go actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			if (settlement == null)
			{
				AdvanceCommand(hero, party, state, "settlement_missing");
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForPatrollingAroundSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "patrol:" + settlement.StringId;
			DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往" + GetSettlementName(settlement) + "附近，抵达后巡逻" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			Log("start patrol actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			state.Stage = CommandStage.Traveling.ToString();
			TickFollowHero(hero, party, state, command, isStarting: true);
			return;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			state.Stage = CommandStage.Traveling.ToString();
			TickFollowParty(hero, party, state, command, isStarting: true);
			return;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			if (IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (!IsSupportedAttackSettlement(settlement))
				{
					AdvanceCommand(hero, party, state, "attack_settlement_invalid");
					return;
				}
				state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
				string settlementAttackMode = NormalizeAttackMode(command.Mode);
				if (CanStartSettlementAttackWithVanillaAi(party, settlement, settlementAttackMode))
				{
					SynchronizeArmyObjectiveForCommand(party, command);
					CommitSettlementAttack(hero, party, settlement, state, settlementAttackMode);
					Log("start settlement_attack_vanilla actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " mode=" + settlementAttackMode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
					return;
				}
				LockPartyAi(party);
				SynchronizeArmyObjectiveForCommand(party, command);
				MoveTowardSettlementAttackPoint(party, settlement);
				state.Stage = CommandStage.Tracking.ToString();
				state.LastIssuedActionKey = "track_settlement_attack:" + settlement.StringId;
				DisplayCommandMessage(GetActorName(state, hero, party) + "开始向" + GetSettlementName(settlement) + "机动，准备" + (settlement.IsVillage ? "烧掠" : "围攻") + "，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
				Log("start settlement_attack_track actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlement.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
				return;
			}
			MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
			if (targetParty == null)
			{
				Hero targetHero = ResolveHeroById(command.TargetId);
				Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, null);
				if (shelter != null)
				{
					state.Stage = CommandStage.Tracking.ToString();
					state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
					DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往" + GetSettlementName(shelter) + "外侧，等待" + GetHeroName(targetHero) + "离开定居点以执行攻击命令。", CommandMessageTone.Progress);
					MaintainAttackShelterWaiting(hero, party, targetHero, shelter, state, command, "start_target_inside_settlement_without_party");
					Log("start attack_shelter_wait actor=" + GetActorLogId(state, hero, party) + " target=" + command.TargetId + " settlement=" + shelter.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
					return;
				}
				AdvanceCommand(hero, party, state, "attack_target_missing");
				return;
			}
			Settlement targetShelter = ResolveTargetHeroShelterSettlement(ResolveHeroById(command.TargetId), targetParty);
			if (targetShelter != null)
			{
				state.Stage = CommandStage.Tracking.ToString();
				state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
				DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往" + GetSettlementName(targetShelter) + "外侧，等待" + GetHeroName(ResolveHeroById(command.TargetId)) + "离开定居点以执行攻击命令。", CommandMessageTone.Progress);
				MaintainAttackShelterWaiting(hero, party, ResolveHeroById(command.TargetId), targetShelter, state, command, "start_target_inside_settlement");
				Log("start attack_shelter_wait actor=" + GetActorLogId(state, hero, party) + " target=" + command.TargetId + " settlement=" + targetShelter.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
			state.Stage = CommandStage.Tracking.ToString();
			state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
			state.LastIssuedActionKey = "track_attack:" + command.TargetId;
			DisplayCommandMessage(GetActorName(state, hero, party) + "开始追踪" + GetHeroName(ResolveHeroById(command.TargetId)) + "的部队，准备攻击，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
			Log("start attack_track actor=" + GetActorLogId(state, hero, party) + " target=" + command.TargetId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			return;
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
			if (!IsPartyUsable(targetParty) || targetParty == party)
			{
				AdvanceCommand(hero, party, state, "attack_party_target_missing");
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
			state.Stage = CommandStage.Tracking.ToString();
			state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
			state.LastIssuedActionKey = "track_party_attack:" + command.TargetId;
			DisplayCommandMessage(GetActorName(state, hero, party) + "开始追踪" + GetPartyName(targetParty) + "，准备攻击，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
			Log("start party_attack_track actor=" + GetActorLogId(state, hero, party) + " targetParty=" + command.TargetId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			return;
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			if (!CanMergeToPlayer(hero, party, out string mergeReason))
			{
				NotifyCommandStatus(state, "merge_to_player_invalid:" + mergeReason, BuildMergeEligibilityFailureMessage(hero, mergeReason), CommandMessageTone.Failure);
				AdvanceCommand(hero, party, state, "merge_invalid:" + mergeReason, terminalFailure: true);
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			IssueMergeApproachAction(party, MobileParty.MainParty);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "merge_to_player";
			DisplayCommandMessage(IsForeignClanGuestHero(hero)
				? (GetHeroName(hero) + "开始率部前往玩家主队，准备在保留原家族身份的情况下以客军形式整队并入。")
				: (GetHeroName(hero) + "开始返回玩家部队，准备会合并转入兵力。"), CommandMessageTone.Progress);
			Log("start merge actor=" + GetActorLogId(state, hero, party));
			return;
		}
	}

	private void TickGoToSettlement(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		Settlement settlement = ResolveSettlementById(command.TargetId);
		if (settlement == null)
		{
			AdvanceCommand(hero, party, state, "settlement_missing");
			return;
		}
		if (TryConvertCurrentGoToSettlementCommand(hero, party, state, command, "tick_go"))
		{
			StartCurrentCommand(hero, party, state);
			return;
		}
		double holdUntilDay = GetCommandHoldUntilDay(command);
		if (state.ArrivalDay < 0.0 && holdUntilDay > 0.0 && NowDay() >= holdUntilDay)
		{
			AdvanceCommand(hero, party, state, "go_hold_until_expired_before_arrival");
			return;
		}
		if (state.ArrivalDay < 0.0 || !IsPartyAtSettlement(party, settlement, SettlementArrivalDistance))
		{
			string actionKey = "visit:" + settlement.StringId;
			bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
				|| !IsPartyVisitingSettlement(party, settlement)
				|| !IsAiDecisionLockActive(party);
			if (shouldRefresh)
			{
				if (!PreemptBlockingWorldActivityForCommand(hero, party, command, state, "tick_go"))
				{
					return;
				}
				LockPartyAi(party);
				SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				SynchronizeArmyObjectiveForCommand(party, command);
				state.Stage = CommandStage.Traveling.ToString();
				state.LastIssuedActionKey = actionKey;
				NotifyCommandStatus(state, actionKey + ":refresh", GetActorName(state, hero, party) + "正在前往" + GetSettlementName(settlement) + "，若原版AI打断会自动重新下达前往命令。", CommandMessageTone.Progress);
				Log("go_refresh hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
			}
		}
		if (state.ArrivalDay < 0.0 && IsPartyAtSettlement(party, settlement, SettlementArrivalDistance))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			LogFact(state, hero, GetActorName(state, hero, party) + "已经抵达" + GetSettlementName(settlement) + "并开始停留。");
		}
		if (state.ArrivalDay >= 0.0 && holdUntilDay > 0.0 && NowDay() >= holdUntilDay)
		{
			LogFact(state, hero, GetActorName(state, hero, party) + "已经完成在" + GetSettlementName(settlement) + "停留至指定期限的命令。");
			AdvanceCommand(hero, party, state, "go_hold_until_done");
		}
		else if (state.ArrivalDay >= 0.0 && holdUntilDay <= 0.0 && NowDay() - state.ArrivalDay >= Math.Max(1, command.Days))
		{
			LogFact(state, hero, GetActorName(state, hero, party) + "已经完成在" + GetSettlementName(settlement) + "停留" + Math.Max(1, command.Days) + "天的命令。");
			AdvanceCommand(hero, party, state, "go_done");
		}
	}

	private void TickPatrolSettlement(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		Settlement settlement = ResolveSettlementById(command.TargetId);
		if (settlement == null)
		{
			AdvanceCommand(hero, party, state, "settlement_missing");
			return;
		}
		string actionKey = "patrol:" + settlement.StringId;
		bool hasArrived = state.ArrivalDay >= 0.0;
		bool insideLeash = IsPartyNearSettlementForPatrol(party, settlement, PatrolLeashDistance);
		bool isEngaging = IsPartyEngagingAnyTarget(party);
		if (hasArrived && insideLeash && IsAiDecisionLockActive(party))
		{
			ReleasePartyAi(party);
			state.LastIssuedActionKey = "patrol_active:" + settlement.StringId;
			Log("patrol_release_ai hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
		}
		bool shouldRefreshTravel = (!hasArrived || !insideLeash) && !isEngaging
			&& (!string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
			|| !IsPartyPatrollingSettlement(party, settlement)
			|| !IsAiDecisionLockActive(party));
		bool shouldRefreshActive = hasArrived && insideLeash && !isEngaging && !IsPartyPatrollingSettlement(party, settlement);
		if (shouldRefreshTravel || shouldRefreshActive)
		{
			if (!PreemptBlockingWorldActivityForCommand(hero, party, command, state, "tick_patrol"))
			{
				return;
			}
			if (shouldRefreshTravel)
			{
				LockPartyAi(party);
			}
			else
			{
				ReleasePartyAi(party);
			}
			SetPartyAiAction.GetActionForPatrollingAroundSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			if (shouldRefreshActive)
			{
				ReleasePartyAi(party);
				state.LastIssuedActionKey = "patrol_active:" + settlement.StringId;
			}
			else
			{
				state.LastIssuedActionKey = actionKey;
			}
			NotifyCommandStatus(state, actionKey + ":refresh", GetActorName(state, hero, party) + "正在" + GetSettlementName(settlement) + "附近巡逻，若原版AI打断会自动重新下达巡逻命令。", CommandMessageTone.Progress);
			Log("patrol_refresh hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
		}
		if (state.ArrivalDay < 0.0 && IsPartyNearSettlementForPatrol(party, settlement, PatrolArrivalDistance))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			state.LastIssuedActionKey = "patrol_active:" + settlement.StringId;
			ReleasePartyAi(party);
			LogFact(state, hero, GetActorName(state, hero, party) + "已经抵达" + GetSettlementName(settlement) + "附近并开始巡逻。");
		}
		if (state.ArrivalDay >= 0.0 && !isEngaging && NowDay() - state.ArrivalDay >= Math.Max(1, command.Days))
		{
			LogFact(state, hero, GetActorName(state, hero, party) + "已经完成在" + GetSettlementName(settlement) + "附近巡逻" + Math.Max(1, command.Days) + "天的命令。");
			AdvanceCommand(hero, party, state, "patrol_done");
		}
	}

	private void TickFollowHero(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, bool isStarting = false)
	{
		MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
		if (targetParty == null)
		{
			AdvanceCommand(hero, party, state, "follow_target_missing");
			return;
		}
		TickFollowCommand(hero, party, state, command, targetParty, GetHeroName(ResolveHeroById(command.TargetId)), "escort:" + command.TargetId, "tick_follow", isStarting);
	}

	private void TickFollowParty(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, bool isStarting = false)
	{
		MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
		if (!IsPartyUsable(targetParty) || targetParty == party)
		{
			AdvanceCommand(hero, party, state, "follow_party_target_missing");
			return;
		}
		TickFollowCommand(hero, party, state, command, targetParty, GetPartyName(targetParty), "escort_party:" + command.TargetId, "tick_follow_party", isStarting);
	}

	private void TickFollowCommand(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, MobileParty targetParty, string targetName, string escortActionKey, string phase, bool isStarting)
	{
		if (TryResolvePlayerFollowSiege(party, targetParty, out SiegeEvent siegeEvent, out Settlement settlement))
		{
			MaintainFollowSiege(hero, party, state, command, targetParty, targetName, siegeEvent, settlement);
			return;
		}
		if (HasFollowSiegeState(state))
		{
			if (!TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: true, "resume_follow:" + phase))
			{
				SetPendingSafeExit(state, PendingSafeExitResumeFollow, "resume_follow:" + phase);
				NotifyCommandStatus(state, "follow_siege_wait_exit:" + (state.FollowSiegeSettlementId ?? ""), GetActorName(state, hero, party) + "正在完成当前战斗，结算后将退出攻城并恢复跟随。", CommandMessageTone.Progress);
				return;
			}
		}
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, escortActionKey, StringComparison.OrdinalIgnoreCase)
			|| !IsPartyEscortingTarget(party, targetParty)
			|| !IsAiDecisionLockActive(party);
		if (shouldRefresh)
		{
			if (!PreemptBlockingWorldActivityForCommand(hero, party, command, state, phase))
			{
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForEscortingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = state.ArrivalDay >= 0.0 ? CommandStage.Active.ToString() : CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = escortActionKey;
			if (isStarting)
			{
				DisplayCommandMessage(GetActorName(state, hero, party) + "开始前往并跟随" + targetName + "，持续" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			}
			else
			{
				NotifyCommandStatus(state, escortActionKey + ":refresh", GetActorName(state, hero, party) + "正在跟随" + targetName + "，若原版AI打断会自动重新下达跟随命令。", CommandMessageTone.Progress);
			}
			Log("follow_refresh actor=" + GetActorLogId(state, hero, party) + " target=" + (targetParty?.StringId ?? command.TargetId ?? "") + " " + DescribePartyAi(party));
		}
		if (state.ArrivalDay < 0.0 && IsPartyCloseEnoughToStartFollowing(party, targetParty))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			LogFact(state, hero, GetActorName(state, hero, party) + "已经追上并开始跟随" + targetName + "。");
		}
	}

	private void MaintainFollowSiege(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, MobileParty targetParty, string targetName, SiegeEvent siegeEvent, Settlement settlement)
	{
		string settlementId = settlement?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(settlementId) || siegeEvent?.BesiegerCamp == null)
		{
			return;
		}
		if (HasFollowSiegeState(state) && !string.Equals(state.FollowSiegeSettlementId, settlementId, StringComparison.OrdinalIgnoreCase))
		{
			if (!TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: true, "switch_follow_siege"))
			{
				SetPendingSafeExit(state, PendingSafeExitResumeFollow, "switch_follow_siege");
				return;
			}
		}
		bool alreadyInCamp = party?.BesiegerCamp == siegeEvent.BesiegerCamp;
		bool wasTrackingThisSiege = string.Equals(state.FollowSiegeSettlementId, settlementId, StringComparison.OrdinalIgnoreCase);
		if (!alreadyInCamp)
		{
			if (!PreemptBlockingWorldActivityForCommand(hero, party, command, state, "follow_siege:" + settlementId))
			{
				return;
			}
			if (!wasTrackingThisSiege)
			{
				state.FollowSiegeSettlementId = settlementId;
				state.FollowSiegeJoinedByCommand = true;
			}
			string actionKey = "follow_siege_travel:" + settlementId;
			bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
				|| party.DefaultBehavior != AiBehavior.BesiegeSettlement
				|| party.TargetSettlement != settlement
				|| !IsAiDecisionLockActive(party);
			if (shouldRefresh)
			{
				LockPartyAi(party);
				SynchronizeArmyObjectiveForFollowSiege(party, settlement);
				SetPartyAiAction.GetActionForBesiegingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false);
				state.Stage = CommandStage.Traveling.ToString();
				state.LastIssuedActionKey = actionKey;
				NotifyCommandStatus(state, actionKey, GetActorName(state, hero, party) + "正在跟随" + targetName + "加入玩家对" + GetSettlementName(settlement) + "的围攻。", CommandMessageTone.Progress);
				Log("follow_siege_travel actor=" + GetActorLogId(state, hero, party) + " target=" + (targetParty?.StringId ?? "") + " settlement=" + settlementId + " " + DescribePartyAi(party));
			}
			return;
		}
		if (!wasTrackingThisSiege)
		{
			state.FollowSiegeSettlementId = settlementId;
			state.FollowSiegeJoinedByCommand = false;
		}
		string activeActionKey = "follow_siege_active:" + settlementId;
		bool becameActive = !string.Equals(state.LastIssuedActionKey, activeActionKey, StringComparison.OrdinalIgnoreCase);
		LockPartyAi(party);
		if (state.FollowSiegeJoinedByCommand)
		{
			SynchronizeArmyObjectiveForFollowSiege(party, settlement);
		}
		state.Stage = CommandStage.Active.ToString();
		state.LastIssuedActionKey = activeActionKey;
		if (state.ArrivalDay < 0.0)
		{
			state.ArrivalDay = NowDay();
		}
		state.TimeoutDay = -1.0;
		if (becameActive)
		{
			LogFact(state, hero, GetActorName(state, hero, party) + "已经随" + targetName + "加入对" + GetSettlementName(settlement) + "的围攻。");
			NotifyCommandStatus(state, activeActionKey, GetActorName(state, hero, party) + "已经加入对" + GetSettlementName(settlement) + "的围攻。", CommandMessageTone.Success);
			Log("follow_siege_active actor=" + GetActorLogId(state, hero, party) + " settlement=" + settlementId + " owned=" + state.FollowSiegeJoinedByCommand);
		}
	}

	private static bool TryResolvePlayerFollowSiege(MobileParty actorParty, MobileParty targetParty, out SiegeEvent siegeEvent, out Settlement settlement)
	{
		siegeEvent = null;
		settlement = null;
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (!IsPartyUsable(actorParty) || !IsPartyUsable(targetParty) || !IsPartyUsable(mainParty) || actorParty == mainParty || actorParty == targetParty)
			{
				return false;
			}
			BesiegerCamp playerCamp = mainParty.BesiegerCamp;
			siegeEvent = playerCamp?.SiegeEvent;
			settlement = siegeEvent?.BesiegedSettlement;
			if (playerCamp == null || siegeEvent == null || settlement == null || settlement.IsVillage || settlement.SiegeEvent != siegeEvent || siegeEvent.BesiegerCamp != playerCamp)
			{
				return false;
			}
			if (targetParty.BesiegerCamp != playerCamp)
			{
				return false;
			}
			IFaction actorFaction = actorParty.MapFaction;
			IFaction siegeFaction = playerCamp.MapFaction;
			if (actorFaction == null || siegeFaction == null || actorFaction != siegeFaction || actorParty.CurrentSettlement == settlement)
			{
				return false;
			}
			if (HasActiveMapEvent(actorParty) && actorParty.BesiegerCamp != playerCamp)
			{
				return false;
			}
			return siegeEvent.CanPartyJoinSide(actorParty.Party, BattleSideEnum.Attacker);
		}
		catch
		{
			siegeEvent = null;
			settlement = null;
			return false;
		}
	}

	private bool ProcessPendingSafeExit(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		if (!HasPendingSafeExit(state))
		{
			return false;
		}
		string action = state.PendingSafeExitAction;
		string reason = state.PendingSafeExitReason;
		bool detachPreexisting = string.Equals(action, PendingSafeExitResumeFollow, StringComparison.OrdinalIgnoreCase);
		if (!TryExitFollowSiegeControl(party, state, detachPreexisting, "pending:" + reason))
		{
			return true;
		}
		ClearPendingSafeExit(state);
		if (string.Equals(action, PendingSafeExitResumeFollow, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(action, PendingSafeExitAdvance, StringComparison.OrdinalIgnoreCase))
		{
			if (party == null)
			{
				FinishQueue(hero, null, state, "actor_invalid_after_follow_siege", appendFact: true);
				return true;
			}
			AdvanceCommand(hero, party, state, string.IsNullOrWhiteSpace(reason) ? "follow_siege_safe_exit" : reason);
			return true;
		}
		if (string.Equals(action, PendingSafeExitStop, StringComparison.OrdinalIgnoreCase))
		{
			if (party != null && party != MobileParty.MainParty)
			{
				AbortCurrentCommandIfNeeded(party, state);
				ReleasePartyAi(party);
			}
			string queueKey = GetQueueKey(state);
			if (!string.IsNullOrWhiteSpace(queueKey))
			{
				lock (_queueLock)
				{
					_queues.Remove(queueKey);
				}
			}
			BeginGovernorExpeditionReturn(hero, party, "stop_after_follow_siege:" + reason);
			Log("stop_after_follow_siege_safe_exit actor=" + GetActorLogId(state, hero, party) + " reason=" + reason);
			return true;
		}
		return false;
	}

	private static bool TryExitFollowSiegeControl(MobileParty party, PartyCommandQueueState state, bool detachPreexistingParticipation, string reason)
	{
		if (!HasFollowSiegeState(state))
		{
			return true;
		}
		try
		{
			Settlement trackedSettlement = ResolveSettlementById(state.FollowSiegeSettlementId);
			if (HasActiveSiegeMapEvent(party, trackedSettlement))
			{
				return false;
			}
			BesiegerCamp currentCamp = party?.BesiegerCamp;
			Settlement currentSiegeSettlement = currentCamp?.SiegeEvent?.BesiegedSettlement;
			bool controlsParticipation = detachPreexistingParticipation || state.FollowSiegeJoinedByCommand;
			bool isTrackedCamp = currentSiegeSettlement != null && string.Equals(currentSiegeSettlement.StringId, state.FollowSiegeSettlementId, StringComparison.OrdinalIgnoreCase);
			if (controlsParticipation && isTrackedCamp)
			{
				party.BesiegerCamp = null;
			}
			if (controlsParticipation && party != null && trackedSettlement != null && party.DefaultBehavior == AiBehavior.BesiegeSettlement && party.TargetSettlement == trackedSettlement)
			{
				party.SetMoveModeHold();
			}
			if (controlsParticipation)
			{
				RestoreArmyObjectiveAfterFollowSiege(party, trackedSettlement);
			}
			Log("follow_siege_exit party=" + (party?.StringId ?? "") + " settlement=" + (state.FollowSiegeSettlementId ?? "") + " detach=" + controlsParticipation + " reason=" + (reason ?? ""));
			ClearFollowSiegeState(state);
			return true;
		}
		catch (Exception ex)
		{
			Log("follow siege exit failed party=" + (party?.StringId ?? "") + " settlement=" + (state?.FollowSiegeSettlementId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static bool HasActiveMapEvent(MobileParty party)
	{
		try
		{
			return party?.MapEvent != null && !party.MapEvent.IsFinalized;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasActiveSettlementMapEvent(Settlement settlement)
	{
		try
		{
			return settlement?.Party?.MapEvent != null && !settlement.Party.MapEvent.IsFinalized;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasActiveSiegeMapEvent(MobileParty party, Settlement settlement)
	{
		try
		{
			if (HasActiveMapEvent(party))
			{
				return true;
			}
			BesiegerCamp camp = party?.BesiegerCamp;
			MapEvent leaderEvent = camp?.LeaderParty?.MapEvent;
			if (leaderEvent != null && !leaderEvent.IsFinalized)
			{
				return true;
			}
			Settlement siegeSettlement = settlement ?? camp?.SiegeEvent?.BesiegedSettlement;
			MapEvent settlementEvent = siegeSettlement?.Party?.MapEvent;
			return settlementEvent != null && !settlementEvent.IsFinalized;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsFollowCommand(PartyCommandEntry command)
	{
		return command != null && (IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.FollowParty));
	}

	private static bool IsCurrentFollowCommand(PartyCommandQueueState state)
	{
		return IsFollowCommand(GetCurrentCommand(state));
	}

	private static bool HasFollowSiegeState(PartyCommandQueueState state)
	{
		return state != null && !string.IsNullOrWhiteSpace(state.FollowSiegeSettlementId);
	}

	private static bool HasPendingSafeExit(PartyCommandQueueState state)
	{
		return state != null && !string.IsNullOrWhiteSpace(state.PendingSafeExitAction);
	}

	private static bool IsStopPending(PartyCommandQueueState state)
	{
		return state != null && string.Equals(state.PendingSafeExitAction, PendingSafeExitStop, StringComparison.OrdinalIgnoreCase);
	}

	private static void SetPendingSafeExit(PartyCommandQueueState state, string action, string reason)
	{
		if (state == null)
		{
			return;
		}
		state.PendingSafeExitAction = action ?? "";
		state.PendingSafeExitReason = reason ?? "";
	}

	private static void ClearPendingSafeExit(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return;
		}
		state.PendingSafeExitAction = "";
		state.PendingSafeExitReason = "";
	}

	private static void ClearFollowSiegeState(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return;
		}
		state.FollowSiegeSettlementId = "";
		state.FollowSiegeJoinedByCommand = false;
		if (!string.IsNullOrWhiteSpace(state.LastIssuedActionKey) && state.LastIssuedActionKey.StartsWith("follow_siege_", StringComparison.OrdinalIgnoreCase))
		{
			state.LastIssuedActionKey = "";
		}
	}

	private static bool HasFollowDurationElapsed(PartyCommandQueueState state, PartyCommandEntry command, double nowDay)
	{
		return state != null && IsFollowCommand(command) && nowDay - state.CommandStartDay >= Math.Max(1, command.Days);
	}

	private static string BuildFollowCompletedFact(PartyCommandQueueState state, Hero hero, MobileParty party, PartyCommandEntry command)
	{
		string targetName = IsKind(command, CommandKind.FollowParty)
			? GetPartyName(ResolveMobilePartyById(command.TargetId))
			: GetHeroName(ResolveHeroById(command.TargetId));
		return GetActorName(state, hero, party) + "已经完成跟随" + targetName + Math.Max(1, command.Days) + "天的命令。";
	}

	private static void SynchronizeArmyObjectiveForFollowSiege(MobileParty party, Settlement settlement)
	{
		try
		{
			if (party?.Army == null || party.Army.LeaderParty != party || settlement == null)
			{
				return;
			}
			party.Army.ArmyType = Army.ArmyTypes.Besieger;
			party.Army.AiBehaviorObject = settlement;
		}
		catch (Exception ex)
		{
			Log("follow siege army objective sync failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static void RestoreArmyObjectiveAfterFollowSiege(MobileParty party, Settlement trackedSettlement)
	{
		try
		{
			if (party?.Army == null || party.Army.LeaderParty != party)
			{
				return;
			}
			if (trackedSettlement == null || party.Army.AiBehaviorObject == trackedSettlement)
			{
				party.Army.ArmyType = Army.ArmyTypes.Patrolling;
				party.Army.AiBehaviorObject = null;
			}
		}
		catch (Exception ex)
		{
			Log("follow siege army objective restore failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private void TickAttackHero(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (IsSettlementTarget(command))
		{
			TickAttackSettlement(hero, party, state, command);
			return;
		}
		Hero targetHero = ResolveHeroById(command.TargetId);
		if (targetHero == null || targetHero.IsDead || targetHero.IsPrisoner)
		{
			TryCompleteCurrentAttackResult(state, state.EngageCommitted ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, state.EngageCommitted ? "目标已经被击败、死亡或被俘。" : "目标已经死亡、被俘或失效。", "attack_target_defeated_or_invalid");
			return;
		}
		MobileParty targetParty = targetHero.PartyBelongedTo;
		if (targetParty == party)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标和执行者位于同一支部队，无法发起攻击。", "attack_target_same_party");
			return;
		}
		if (targetParty == null || !IsPartyUsable(targetParty))
		{
			Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, targetParty);
			if (shelter != null)
			{
				MaintainAttackShelterWaiting(hero, party, targetHero, shelter, state, command, "target_inside_settlement_without_party");
				return;
			}
			TryCompleteCurrentAttackResult(state, state.EngageCommitted ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, state.EngageCommitted ? "目标部队已经被击溃或解散。" : "目标当前没有可攻击的部队。", "attack_target_party_missing");
			return;
		}
		if (state.EngageCommitted)
		{
			MaintainCommittedAttack(hero, party, targetHero, targetParty, state, command);
			return;
		}
		Settlement targetShelter = ResolveTargetHeroShelterSettlement(targetHero, targetParty);
		if (targetShelter != null)
		{
			MaintainAttackShelterWaiting(hero, party, targetHero, targetShelter, state, command, "target_inside_settlement");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, PartyAttackCommitDistance))
		{
			MaintainAttackTracking(hero, party, targetParty, state, command, "closing_distance");
			return;
		}
		string mode = NormalizeAttackMode(command.Mode);
		bool force = IsForceAttackMode(mode);
		bool requiresRebellion = RequiresPlayerClanRebellionForPartyAttack(party, targetParty);
		if (requiresRebellion && !TryPreparePlayerClanRebellionForHeroAttack(hero, party, targetHero, targetParty, apply: false, out string precheckRebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, precheckRebellionReason, "rebellion_attack_blocked");
			return;
		}
		if (force && !CanForceCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainAttackTracking(hero, party, targetParty, state, command, "force_commit_blocked");
			return;
		}
		if (!force && !CanAiCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainAttackTracking(hero, party, targetParty, state, command, "ai_commit_waiting");
			return;
		}
		if (requiresRebellion && !TryPreparePlayerClanRebellionForHeroAttack(hero, party, targetHero, targetParty, apply: true, out string rebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, rebellionReason, "rebellion_attack_blocked");
			return;
		}
		CommitAttack(hero, party, targetHero, targetParty, state, mode);
	}

	private void TickAttackParty(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
		if (!IsPartyUsable(targetParty))
		{
			TryCompleteCurrentAttackResult(state, state.EngageCommitted ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, state.EngageCommitted ? "目标部队已经被击溃或解散。" : "目标部队已经失效或不在大地图上。", "attack_party_target_missing");
			return;
		}
		if (targetParty == party)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标部队和执行者是同一支部队，无法发起攻击。", "attack_party_same_party");
			return;
		}
		if (command.RequiresExistingWar && !ArePartiesAtWar(party, targetParty))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "围城部队已经不再与执行者敌对，未继续攻击，也未重新宣战。", "siege_defense_no_longer_at_war");
			return;
		}
		if (state.EngageCommitted)
		{
			MaintainCommittedPartyAttack(hero, party, targetParty, state, command);
			return;
		}
		if (targetParty.CurrentSettlement != null)
		{
			MaintainPartyAttackSettlementWaiting(hero, party, targetParty, targetParty.CurrentSettlement, state, command, "party_target_inside_settlement");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, PartyAttackCommitDistance))
		{
			MaintainPartyAttackTracking(hero, party, targetParty, state, command, "closing_distance");
			return;
		}
		string mode = NormalizeAttackMode(command.Mode);
		bool force = IsForceAttackMode(mode);
		bool requiresRebellion = RequiresPlayerClanRebellionForPartyAttack(party, targetParty);
		if (requiresRebellion && !TryPreparePlayerClanRebellionForPartyAttack(hero, party, targetParty, apply: false, out string precheckRebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, precheckRebellionReason, "rebellion_party_attack_blocked");
			return;
		}
		if (force && !CanForceCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainPartyAttackTracking(hero, party, targetParty, state, command, "force_commit_blocked");
			return;
		}
		if (!force && !CanAiCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainPartyAttackTracking(hero, party, targetParty, state, command, "ai_commit_waiting");
			return;
		}
		if (requiresRebellion && !TryPreparePlayerClanRebellionForPartyAttack(hero, party, targetParty, apply: true, out string rebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, rebellionReason, "rebellion_party_attack_blocked");
			return;
		}
		CommitPartyAttack(hero, party, targetParty, state, mode);
	}

	private void TickAttackSettlement(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		Settlement settlement = ResolveSettlementById(command.TargetId);
		if (!IsSupportedAttackSettlement(settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标定居点已经失效或不是可攻击目标。", "attack_settlement_invalid");
			return;
		}
		if (state.EngageCommitted && IsSettlementAttackComplete(party, settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, settlement.IsVillage ? "村庄已被洗劫。" : (GetSettlementName(settlement) + "已经被攻下。"), "attack_settlement_done");
			return;
		}
		if (!state.EngageCommitted && IsSettlementAttackUnavailableBeforeCommit(settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标已经不可攻击。", "attack_settlement_unavailable");
			return;
		}
		string mode = NormalizeAttackMode(command.Mode);
		bool requiresRebellion = !state.EngageCommitted && RequiresPlayerClanRebellionForSettlementAttack(party, settlement);
		if (requiresRebellion && !TryPreparePlayerClanRebellionForSettlementAttack(hero, party, settlement, apply: false, out string sameFactionRebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, sameFactionRebellionReason, "rebellion_settlement_attack_blocked");
			return;
		}
		if (state.EngageCommitted)
		{
			MaintainCommittedSettlementAttack(hero, party, settlement, state, command);
			return;
		}
		if (!requiresRebellion && CanStartSettlementAttackWithVanillaAi(party, settlement, mode))
		{
			SynchronizeArmyObjectiveForCommand(party, command);
			CommitSettlementAttack(hero, party, settlement, state, mode);
			return;
		}
		if (!IsPartyNearPosition(party, GetSettlementAttackPosition(settlement), SettlementAttackCommitDistance))
		{
			MaintainSettlementAttackTracking(hero, party, settlement, state, command, "closing_distance");
			return;
		}
		bool force = IsForceAttackMode(mode);
		if (force && !CanForceCommitSettlementAttackForMode(party, settlement, requiresRebellion))
		{
			MaintainSettlementAttackTracking(hero, party, settlement, state, command, "force_commit_blocked");
			return;
		}
		if (!force && !CanAiCommitSettlementAttackForMode(party, settlement, requiresRebellion))
		{
			MaintainSettlementAttackTracking(hero, party, settlement, state, command, "ai_commit_waiting");
			return;
		}
		if (requiresRebellion && !TryPreparePlayerClanRebellionForSettlementAttack(hero, party, settlement, apply: true, out string rebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, rebellionReason, "rebellion_settlement_attack_blocked");
			return;
		}
		CommitSettlementAttack(hero, party, settlement, state, mode);
	}

	private void MaintainSettlementAttackTracking(Hero actorHero, MobileParty party, Settlement settlement, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || settlement == null || state == null || command == null)
		{
			return;
		}
		SynchronizeArmyObjectiveForCommand(party, command);
		string actionKey = "track_settlement_attack:" + settlement.StringId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase) || party.DefaultBehavior != AiBehavior.GoToPoint || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		if (!PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "settlement_attack_track"))
		{
			return;
		}
		LockPartyAi(party);
		MoveTowardSettlementAttackPoint(party, settlement);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		NotifyCommandStatus(state, actionKey + ":" + reason, BuildAttackTrackingStatusMessage(state, actorHero, party, GetSettlementName(settlement), reason), CommandMessageTone.Progress);
		Log("settlement_attack_track_refresh hero=" + (actorHero?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainCommittedSettlementAttack(Hero actorHero, MobileParty party, Settlement settlement, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party == null || settlement == null || state == null || command == null)
		{
			return;
		}
		if (IsSettlementAttackComplete(party, settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, settlement.IsVillage ? "村庄已被洗劫。" : (GetSettlementName(settlement) + "已经被攻下。"), "attack_settlement_done");
			return;
		}
		if (IsPartyCommittedToSettlementAttack(party, settlement))
		{
			SynchronizeArmyObjectiveForCommand(party, command);
			if (settlement.IsVillage)
			{
				EnsureCommittedRaidBehavior(party, settlement);
			}
			LockPartyAi(party);
			return;
		}
		if (!CanForceCommitSettlementAttack(party, settlement))
		{
			MaintainSettlementAttackTracking(actorHero, party, settlement, state, command, "settlement_attack_conditions_lost");
			return;
		}
		CommitSettlementAttack(actorHero, party, settlement, state, NormalizeAttackMode(command.Mode));
	}

	private void CommitSettlementAttack(Hero actorHero, MobileParty party, Settlement settlement, PartyCommandQueueState state, string mode)
	{
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = settlement.MapFaction;
		if (attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && !FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction))
		{
			DeclareWarAction.ApplyByDefault(attackerFaction, defenderFaction);
			Log("declare_war_on_settlement_attack_commit attacker=" + SafeFactionId(attackerFaction) + " defender=" + SafeFactionId(defenderFaction) + " settlement=" + settlement.StringId + " mode=" + mode);
		}
		LeaveTargetSettlementIfInside(party, settlement);
		BeginResultTracking(state, settlement.IsVillage ? "raid" : "siege", "settlement", settlement.StringId, GetSettlementName(settlement), attackerFaction, defenderFaction);
		if (settlement.IsVillage)
		{
			SetPartyAiActionForRaidingSettlement(party, settlement);
			state.LastIssuedActionKey = "raid:" + settlement.StringId;
			LogFact(state, actorHero, GetActorName(state, actorHero, party) + "已经开始烧掠" + GetSettlementName(settlement) + "，结果尚未分出。");
			Log("settlement_attack_commit_raid actor=" + GetActorLogId(state, actorHero, party) + " settlement=" + settlement.StringId + " mode=" + mode);
		}
		else
		{
			SetPartyAiAction.GetActionForBesiegingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false);
			state.LastIssuedActionKey = "besiege:" + settlement.StringId;
			LogFact(state, actorHero, GetActorName(state, actorHero, party) + "已经开始围攻" + GetSettlementName(settlement) + "，结果尚未分出。");
			Log("settlement_attack_commit_siege actor=" + GetActorLogId(state, actorHero, party) + " settlement=" + settlement.StringId + " mode=" + mode);
		}
		LockPartyAi(party);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
	}

	private static void SetPartyAiActionForRaidingSettlement(MobileParty party, Settlement settlement)
	{
		BannerlordApiCompat.GetActionForRaidingSettlement(party, settlement);
	}

	private bool TryKeepRaidCommandAliveAfterRaidEnded(PartyCommandQueueState state, PartyCommandEntry command, Settlement settlement, RaidEventComponent raidEvent, string reason)
	{
		try
		{
			if (state == null || command == null || !IsTargetSettlement(command, settlement) || !settlement.IsVillage || state.ResultLogged)
			{
				return false;
			}
			if (IsVillageLooted(settlement) || settlement.SettlementHitPoints <= 0.001f)
			{
				return false;
			}
			if (raidEvent?.AttackerSide == null || raidEvent.AttackerSide.TroopCount <= 0 || !MapEventSideHasActor(raidEvent.AttackerSide, state))
			{
				return false;
			}
			Hero hero = ResolveHeroByIdAny(state.HeroId);
			MobileParty party = ResolveActorParty(state, hero);
			if (!IsPartyUsable(party) || !CanForceCommitSettlementAttack(party, settlement))
			{
				return false;
			}
			double now = NowDay();
			if (state.TimeoutDay > 0.0 && now >= state.TimeoutDay)
			{
				return false;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			MoveTowardSettlementAttackPoint(party, settlement);
			state.EngageCommitted = false;
			state.Stage = CommandStage.Tracking.ToString();
			state.LastIssuedActionKey = "raid_retry:" + settlement.StringId;
			NotifyCommandStatus(state, state.LastIssuedActionKey + ":" + reason, GetActorName(state, hero, party) + "的烧村行动被原版事件提前中断，正在重新保持对" + GetSettlementName(settlement) + "的烧掠命令。", CommandMessageTone.Progress);
			Log("raid_retry_after_nonfinal_end actor=" + GetActorLogId(state, hero, party) + " settlement=" + (settlement.StringId ?? "") + " reason=" + (reason ?? "") + " hp=" + settlement.SettlementHitPoints.ToString("0.000") + " troops=" + raidEvent.AttackerSide.TroopCount + " " + DescribePartyAi(party));
			return true;
		}
		catch (Exception ex)
		{
			Log("raid retry check failed actor=" + GetActorLogId(state, null, null) + " settlement=" + (settlement?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private void MaintainAttackTracking(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		SynchronizeArmyObjectiveForCommand(party, command);
		string actionKey = "track_attack:" + command.TargetId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase) || !IsPartyTrackingTarget(party, targetParty) || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		if (!PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "attack_track"))
		{
			return;
		}
		LockPartyAi(party);
		SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		NotifyCommandStatus(state, actionKey + ":" + reason, BuildAttackTrackingStatusMessage(state, actorHero, party, GetPartyName(targetParty), reason), CommandMessageTone.Progress);
		Log("attack_track_refresh hero=" + (actorHero?.StringId ?? "") + " target=" + (command.TargetId ?? "") + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainAttackShelterWaiting(Hero actorHero, MobileParty party, Hero targetHero, Settlement shelter, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetHero == null || shelter == null || state == null || command == null)
		{
			return;
		}
		string actionKey = "wait_hero_shelter:" + targetHero.StringId + ":" + shelter.StringId;
		bool actionChanged = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase);
		bool shouldRefresh = actionChanged || party.DefaultBehavior != AiBehavior.GoToPoint || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		if (!PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "attack_shelter_wait"))
		{
			return;
		}
		LeaveTargetSettlementIfInside(party, shelter);
		LockPartyAi(party);
		MoveTowardSettlementAttackPoint(party, shelter);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		if (actionChanged)
		{
			LogFact(state, actorHero, GetHeroName(targetHero) + "当前躲在" + GetSettlementName(shelter) + "内，" + GetActorName(state, actorHero, party) + "正在城外等待其离开，以继续攻击命令。");
		}
		Log("attack_shelter_wait_refresh actor=" + GetActorLogId(state, actorHero, party) + " target=" + targetHero.StringId + " settlement=" + shelter.StringId + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainPartyAttackTracking(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		SynchronizeArmyObjectiveForCommand(party, command);
		string actionKey = "track_party_attack:" + command.TargetId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase) || !IsPartyTrackingTarget(party, targetParty) || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		if (!PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "party_attack_track"))
		{
			return;
		}
		LockPartyAi(party);
		SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		NotifyCommandStatus(state, actionKey + ":" + reason, BuildAttackTrackingStatusMessage(state, actorHero, party, GetPartyName(targetParty), reason), CommandMessageTone.Progress);
		Log("party_attack_track_refresh hero=" + (actorHero?.StringId ?? "") + " targetParty=" + (command.TargetId ?? "") + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainPartyAttackSettlementWaiting(Hero actorHero, MobileParty party, MobileParty targetParty, Settlement shelter, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetParty == null || shelter == null || state == null || command == null)
		{
			return;
		}
		string actionKey = "wait_party_shelter:" + targetParty.StringId + ":" + shelter.StringId;
		bool actionChanged = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase);
		bool shouldRefresh = actionChanged || party.DefaultBehavior != AiBehavior.GoToPoint || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		if (!PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "party_attack_shelter_wait"))
		{
			return;
		}
		LeaveTargetSettlementIfInside(party, shelter);
		LockPartyAi(party);
		MoveTowardSettlementAttackPoint(party, shelter);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		if (actionChanged)
		{
			LogFact(state, actorHero, GetPartyName(targetParty) + "当前在" + GetSettlementName(shelter) + "内，" + GetActorName(state, actorHero, party) + "正在外侧等待其离开，以继续攻击命令。");
		}
		Log("party_attack_shelter_wait_refresh actor=" + GetActorLogId(state, actorHero, party) + " targetParty=" + (targetParty?.StringId ?? "") + " settlement=" + shelter.StringId + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainCommittedPartyAttack(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		if (targetParty.CurrentSettlement != null && !IsPartyEngagingTarget(party, targetParty))
		{
			MaintainPartyAttackSettlementWaiting(actorHero, party, targetParty, targetParty.CurrentSettlement, state, command, "party_target_sheltered_after_commit");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, EngageMaintainDistance))
		{
			MaintainPartyAttackTracking(actorHero, party, targetParty, state, command, "target_left_engage_range");
			return;
		}
		if (!CanForceCommitAttack(party, targetParty))
		{
			MaintainPartyAttackTracking(actorHero, party, targetParty, state, command, "engage_conditions_lost");
			return;
		}
		if (IsPartyEngagingTarget(party, targetParty) && IsAiDecisionLockActive(party))
		{
			return;
		}
		LockPartyAi(party);
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage_party:" + targetParty.StringId;
		NotifyCommandStatus(state, state.LastIssuedActionKey + ":reengage", GetHeroName(actorHero) + "重新追上" + GetPartyName(targetParty) + "，继续执行攻击命令。", CommandMessageTone.Progress);
		Log("party_attack_reengage hero=" + (actorHero?.StringId ?? "") + " targetParty=" + targetParty.StringId + " " + DescribePartyAi(party));
	}

	private void MaintainCommittedAttack(Hero actorHero, MobileParty party, Hero targetHero, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party == null || targetHero == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, targetParty);
		if (shelter != null && !IsPartyEngagingTarget(party, targetParty))
		{
			MaintainAttackShelterWaiting(actorHero, party, targetHero, shelter, state, command, "target_sheltered_after_commit");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, EngageMaintainDistance))
		{
			MaintainAttackTracking(actorHero, party, targetParty, state, command, "target_left_engage_range");
			return;
		}
		if (!CanForceCommitAttack(party, targetParty))
		{
			MaintainAttackTracking(actorHero, party, targetParty, state, command, "engage_conditions_lost");
			return;
		}
		if (IsPartyEngagingTarget(party, targetParty) && IsAiDecisionLockActive(party))
		{
			return;
		}
		LockPartyAi(party);
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage:" + targetHero.StringId;
		NotifyCommandStatus(state, state.LastIssuedActionKey + ":reengage", GetHeroName(actorHero) + "重新追上" + GetHeroName(targetHero) + "的部队，继续执行攻击命令。", CommandMessageTone.Progress);
		Log("attack_reengage hero=" + (actorHero?.StringId ?? "") + " target=" + targetHero.StringId + " " + DescribePartyAi(party));
	}

	private void TickMergeToPlayer(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (!CanMergeToPlayer(hero, party, out string reason))
		{
			NotifyCommandStatus(state, "merge_to_player_invalid:" + reason, BuildMergeEligibilityFailureMessage(hero, reason), CommandMessageTone.Failure);
			AdvanceCommand(hero, party, state, "merge_invalid:" + reason, terminalFailure: true);
			return;
		}
		if (state.MergeRetryAfterDay > NowDay())
		{
			return;
		}
		if (!IsPartyNearParty(party, MobileParty.MainParty, MergeArrivalDistance))
		{
			bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, "merge_to_player", StringComparison.OrdinalIgnoreCase)
				|| !IsMergeApproachActionCurrent(party, MobileParty.MainParty)
				|| !IsAiDecisionLockActive(party);
			if (shouldRefresh)
			{
				if (!PreemptBlockingWorldActivityForCommand(hero, party, command, state, "merge_to_player"))
				{
					return;
				}
				LockPartyAi(party);
				IssueMergeApproachAction(party, MobileParty.MainParty);
				state.LastIssuedActionKey = "merge_to_player";
				NotifyCommandStatus(state, "merge_to_player_refresh", IsForeignClanGuestHero(hero)
					? (GetHeroName(hero) + "正率部前往玩家主队，若原版AI打断会自动重新下达客军并队命令。")
					: (GetHeroName(hero) + "正在返回玩家部队，若原版AI打断会自动重新下达回队命令。"), CommandMessageTone.Progress);
				Log("merge_refresh hero=" + (hero?.StringId ?? "") + " " + DescribePartyAi(party));
			}
			return;
		}
		LeaveArmyIfNeeded(party);
		if (HasActiveMapEvent(party) || party.BesiegerCamp != null || party.Army != null || party.AttachedTo != null)
		{
			return;
		}
		List<TroopRosterElement> memberSnapshot = party.MemberRoster?.GetTroopRoster().ToList() ?? new List<TroopRosterElement>();
		List<TroopRosterElement> prisonerSnapshot = party.PrisonRoster?.GetTroopRoster().ToList() ?? new List<TroopRosterElement>();
		List<ItemRosterElement> itemSnapshot = party.ItemRoster?.ToList() ?? new List<ItemRosterElement>();
		List<Ship> shipSnapshot = GetPartyShipsSnapshot(party);
		int movedMembers = memberSnapshot.Sum(x => Math.Max(0, x.Number));
		int movedPrisoners = prisonerSnapshot.Sum(x => Math.Max(0, x.Number));
		List<Hero> memberHeroes = GetMemberHeroesSnapshot(party);
		List<MergeHeroIdentitySnapshot> memberHeroIdentities = CaptureMergeHeroIdentities(memberHeroes);
		bool foreignClanGuestMerge = IsForeignClanGuestHero(hero);
		List<RosterElementTransferRecord> memberTransfers = new List<RosterElementTransferRecord>();
		List<RosterElementTransferRecord> prisonerTransfers = new List<RosterElementTransferRecord>();
		string transferStage = "regular_members";
		try
		{
			MoveAllRegularRosterElementsVerified(party.MemberRoster, MobileParty.MainParty.MemberRoster, memberTransfers);
			transferStage = "prisoners";
			MoveAllPrisonersToPartyVerified(party, MobileParty.MainParty, prisonerTransfers);
			transferStage = "items";
			MoveAllItemsVerified(party.ItemRoster, MobileParty.MainParty.ItemRoster);
			transferStage = "member_heroes";
			MoveAllMemberHeroesToPartyVerified(party, MobileParty.MainParty, memberHeroes);
			transferStage = "hero_identity_validation";
			ValidateMergeHeroIdentitiesUnchanged(memberHeroIdentities);
			transferStage = "ships";
			TransferShipsVerified(party.Party, MobileParty.MainParty.Party, shipSnapshot);
			transferStage = "post_transfer_validation";
			if ((party.MemberRoster?.TotalManCount ?? 0) != 0
				|| (party.PrisonRoster?.TotalManCount ?? 0) != 0
				|| (party.ItemRoster?.Count ?? 0) != 0
				|| HasPartyShips(party))
			{
				throw new InvalidOperationException("并入主队后源部队仍有未转移资产。");
			}
			bool hadGovernorExpedition = TryGetGovernorExpeditionForHero(hero?.StringId, out GovernorExpeditionRecord mergeRecord)
				&& GovernorRecordMatchesParty(mergeRecord, party);
			string queueKey = GetQueueKey(state);
			if (hadGovernorExpedition)
			{
				RemoveGovernorExpeditionRecord(hero.StringId, "merge_to_player_pending_destroy", removeQueue: true);
			}
			else if (!string.IsNullOrWhiteSpace(queueKey))
			{
				lock (_queueLock)
				{
					_queues.Remove(queueKey);
				}
			}
			if (!TryDestroyStrictlyEmptyParty(party, "merge_to_player"))
			{
				bool rollbackSucceeded = RollbackMergeToPlayerTransfer(party, memberSnapshot, prisonerSnapshot, itemSnapshot, shipSnapshot, memberHeroes, memberTransfers, prisonerTransfers);
				lock (_queueLock)
				{
					if (hadGovernorExpedition && mergeRecord != null)
					{
						_governorExpeditions[mergeRecord.HeroId] = mergeRecord;
						IndexGovernorExpeditionRecordUnsafe(mergeRecord);
						_governorExpeditionPartyByHeroId[mergeRecord.HeroId] = party;
						Volatile.Write(ref _hasGovernorExpeditions, 1);
					}
					if (!string.IsNullOrWhiteSpace(queueKey) && state != null)
					{
						_queues[queueKey] = state;
					}
				}
				state.MergeTransferFailureCount++;
				Log("merge destroy deferred hero=" + (hero?.StringId ?? "") + " party=" + (party?.StringId ?? "") + " rollback=" + rollbackSucceeded + " attempt=" + state.MergeTransferFailureCount);
				if (!rollbackSucceeded || state.MergeTransferFailureCount >= 3)
				{
					NotifyCommandStatus(state, "merge_to_player_destroy_stopped", GetHeroName(hero) + "的临时部队无法安全销毁，自动合并已停止，以免重复转移资产。", CommandMessageTone.Failure);
					AdvanceCommand(hero, party, state, "merge_destroy_failed", terminalFailure: true);
					return;
				}
				state.MergeRetryAfterDay = NowDay() + 0.25;
				return;
			}
			RemovePlayerDetachedParty(hero, party, "merge_done");
			RemoveGovernorExpeditionRecord(hero?.StringId, "merge_to_player", removeQueue: true);
			if (!string.IsNullOrWhiteSpace(queueKey))
			{
				lock (_queueLock)
				{
					_queues.Remove(queueKey);
				}
			}
			RegisterForeignClanGuests(memberHeroIdentities);
			LogFact(hero, GetHeroName(hero) + "已经与玩家部队会合，并转入" + movedMembers + "名成员、" + movedPrisoners + "名俘虏及全部随队物资；"
				+ (foreignClanGuestMerge ? "原独立部队已解散。" : "临时远征记录已清除。")
				+ (foreignClanGuestMerge ? (" " + GetHeroName(hero) + "保留原家族、职业和同伴身份，以外族客军身份随玩家主队行动。") : ""));
		}
		catch (Exception ex)
		{
			bool rollbackSucceeded = RollbackMergeToPlayerTransfer(party, memberSnapshot, prisonerSnapshot, itemSnapshot, shipSnapshot, memberHeroes, memberTransfers, prisonerTransfers);
			state.MergeTransferFailureCount++;
			Log("merge to player transfer failed hero=" + (hero?.StringId ?? "") + " stage=" + transferStage + " rollback=" + rollbackSucceeded + " attempt=" + state.MergeTransferFailureCount + " error=" + ex);
			if (!rollbackSucceeded || state.MergeTransferFailureCount >= 3)
			{
				NotifyCommandStatus(state, "merge_to_player_transfer_stopped", GetHeroName(hero) + "的兵员转入连续失败，自动合并已停止，以免重复回滚损坏资产。", CommandMessageTone.Failure);
				AdvanceCommand(hero, party, state, "merge_transfer_failed", terminalFailure: true);
				return;
			}
			state.MergeRetryAfterDay = NowDay() + 0.25;
			NotifyCommandStatus(state, "merge_to_player_transfer_failed", GetHeroName(hero) + "已经抵达玩家部队，但兵员转入失败；资产已回滚，将在6游戏小时后重试。", CommandMessageTone.Failure);
		}
	}

	private void CommitAttack(Hero actorHero, MobileParty party, Hero targetHero, MobileParty targetParty, PartyCommandQueueState state, string mode)
	{
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = targetParty.MapFaction;
		if (attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && !FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction))
		{
			DeclareWarAction.ApplyByDefault(attackerFaction, defenderFaction);
			Log("declare_war_on_attack_commit attacker=" + SafeFactionId(attackerFaction) + " defender=" + SafeFactionId(defenderFaction) + " mode=" + mode);
		}
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		LockPartyAi(party);
		BeginResultTracking(state, "hero_attack", "hero", targetHero.StringId, GetHeroName(targetHero), attackerFaction, defenderFaction);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage:" + targetHero.StringId;
		LogFact(state, actorHero, GetActorName(state, actorHero, party) + "已经对" + GetHeroName(targetHero) + "的部队发起攻击，结果尚未分出。");
		Log("attack_commit actor=" + GetActorLogId(state, actorHero, party) + " target=" + targetHero.StringId + " mode=" + mode);
	}

	private void CommitPartyAttack(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, string mode)
	{
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = targetParty.MapFaction;
		if (attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && !FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction))
		{
			DeclareWarAction.ApplyByDefault(attackerFaction, defenderFaction);
			Log("declare_war_on_party_attack_commit attacker=" + SafeFactionId(attackerFaction) + " defender=" + SafeFactionId(defenderFaction) + " mode=" + mode);
		}
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		LockPartyAi(party);
		BeginResultTracking(state, "party_attack", "party", targetParty.StringId, GetPartyName(targetParty), attackerFaction, defenderFaction);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage_party:" + targetParty.StringId;
		LogFact(state, actorHero, GetActorName(state, actorHero, party) + "已经对" + GetPartyName(targetParty) + "发起攻击，结果尚未分出。");
		Log("party_attack_commit actor=" + GetActorLogId(state, actorHero, party) + " targetParty=" + targetParty.StringId + " mode=" + mode);
	}

	private bool CanAiCommitAttack(MobileParty party, MobileParty targetParty)
	{
		if (!CanForceCommitAttack(party, targetParty))
		{
			return false;
		}
		bool alreadyAtWar = ArePartiesAtWar(party, targetParty);
		try
		{
			if (alreadyAtWar && Campaign.Current?.Models?.MobilePartyAIModel?.ShouldConsiderAttacking(party, targetParty) != true)
			{
				return false;
			}
		}
		catch
		{
			if (alreadyAtWar)
			{
				return false;
			}
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, targetParty);
		float defenderStrength = EstimatePartyStrength(targetParty);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private bool CanAiCommitAttackForMode(MobileParty party, MobileParty targetParty, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanAiCommitAttack(party, targetParty);
		}
		if (!CanForceCommitAttackForMode(party, targetParty, allowSameFactionViaRebellion))
		{
			return false;
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, targetParty);
		float defenderStrength = EstimatePartyStrength(targetParty);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private static bool CanForceCommitAttack(MobileParty party, MobileParty targetParty)
	{
		if (!IsPartyUsable(party) || !IsPartyUsable(targetParty) || party == targetParty)
		{
			return false;
		}
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = targetParty.MapFaction;
		return attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction;
	}

	private static bool CanForceCommitAttackForMode(MobileParty party, MobileParty targetParty, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanForceCommitAttack(party, targetParty);
		}
		return IsPartyUsable(party) && IsPartyUsable(targetParty) && party != targetParty;
	}

	private bool CanAiCommitSettlementAttack(MobileParty party, Settlement settlement)
	{
		if (!CanForceCommitSettlementAttack(party, settlement))
		{
			return false;
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, settlement);
		float defenderStrength = EstimateSettlementDefenseStrength(settlement);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private bool CanAiCommitSettlementAttackForMode(MobileParty party, Settlement settlement, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanAiCommitSettlementAttack(party, settlement);
		}
		if (!CanForceCommitSettlementAttackForMode(party, settlement, allowSameFactionViaRebellion))
		{
			return false;
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, settlement);
		float defenderStrength = EstimateSettlementDefenseStrength(settlement);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private bool CanStartSettlementAttackWithVanillaAi(MobileParty party, Settlement settlement, string mode)
	{
		if (!IsPartyAtWarWithSettlement(party, settlement))
		{
			return false;
		}
		return IsForceAttackMode(mode) ? CanForceCommitSettlementAttack(party, settlement) : CanAiCommitSettlementAttack(party, settlement);
	}

	private static bool CanForceCommitSettlementAttack(MobileParty party, Settlement settlement)
	{
		if (!IsPartyUsable(party) || !IsSupportedAttackSettlement(settlement))
		{
			return false;
		}
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = settlement.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction == defenderFaction)
		{
			return false;
		}
		if (settlement.IsVillage)
		{
			if (settlement.IsRaided || settlement.SettlementHitPoints <= 0.001f)
			{
				return false;
			}
			return !settlement.IsUnderRaid || IsPartyCommittedToSettlementAttack(party, settlement);
		}
		if (settlement.IsUnderSiege && !IsPartyCommittedToSettlementAttack(party, settlement) && !IsSameFactionSiege(party, settlement))
		{
			return false;
		}
		return true;
	}

	private static bool CanForceCommitSettlementAttackForMode(MobileParty party, Settlement settlement, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanForceCommitSettlementAttack(party, settlement);
		}
		if (!IsPartyUsable(party) || !IsSupportedAttackSettlement(settlement))
		{
			return false;
		}
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = settlement.MapFaction;
		if (attackerFaction == null || defenderFaction == null)
		{
			return false;
		}
		if (settlement.IsVillage)
		{
			if (settlement.IsRaided || settlement.SettlementHitPoints <= 0.001f)
			{
				return false;
			}
			return !settlement.IsUnderRaid || IsPartyCommittedToSettlementAttack(party, settlement);
		}
		if (settlement.IsUnderSiege && !IsPartyCommittedToSettlementAttack(party, settlement) && !IsSameFactionSiege(party, settlement))
		{
			return false;
		}
		return true;
	}

	private static bool IsPartyAtWarWithSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = settlement?.MapFaction;
			return attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction);
		}
		catch
		{
			return false;
		}
	}

	private static bool RequiresPlayerClanRebellionForPartyAttack(MobileParty party, MobileParty targetParty)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = targetParty?.MapFaction;
			return attackerFaction != null && defenderFaction != null && attackerFaction == defenderFaction;
		}
		catch
		{
			return false;
		}
	}

	private static bool RequiresPlayerClanRebellionForSettlementAttack(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = settlement?.MapFaction;
			return attackerFaction != null && defenderFaction != null && attackerFaction == defenderFaction;
		}
		catch
		{
			return false;
		}
	}

	private bool TryPreparePlayerClanRebellionForHeroAttack(Hero actorHero, MobileParty party, Hero targetHero, MobileParty targetParty, bool apply, out string reason)
	{
		reason = "";
		IFaction attackerFaction = party?.MapFaction;
		IFaction defenderFaction = targetParty?.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction)
		{
			return true;
		}
		if (targetHero?.Clan == Clan.PlayerClan)
		{
			reason = "目标属于玩家家族，不能通过叛乱攻击自己的家族部队。";
			return false;
		}
		Kingdom oldKingdom = Clan.PlayerClan?.Kingdom;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, defenderFaction, out reason))
		{
			return false;
		}
		if (!apply)
		{
			return true;
		}
		return TryApplyPlayerClanRebellionForWorldMapAttack(actorHero, party, oldKingdom, "攻击" + GetHeroName(targetHero) + "的部队", out reason);
	}

	private bool TryPreparePlayerClanRebellionForPartyAttack(Hero actorHero, MobileParty party, MobileParty targetParty, bool apply, out string reason)
	{
		reason = "";
		IFaction attackerFaction = party?.MapFaction;
		IFaction defenderFaction = targetParty?.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction)
		{
			return true;
		}
		Clan targetClan = targetParty?.ActualClan ?? targetParty?.LeaderHero?.Clan;
		if (targetClan == Clan.PlayerClan)
		{
			reason = "目标部队属于玩家家族，不能通过叛乱攻击自己的家族部队。";
			return false;
		}
		Kingdom oldKingdom = Clan.PlayerClan?.Kingdom;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, defenderFaction, out reason))
		{
			return false;
		}
		if (!apply)
		{
			return true;
		}
		return TryApplyPlayerClanRebellionForWorldMapAttack(actorHero, party, oldKingdom, "攻击" + GetPartyName(targetParty), out reason);
	}

	private bool TryPreparePlayerClanRebellionForSettlementAttack(Hero actorHero, MobileParty party, Settlement settlement, bool apply, out string reason)
	{
		reason = "";
		IFaction attackerFaction = party?.MapFaction;
		IFaction defenderFaction = settlement?.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction)
		{
			return true;
		}
		if (settlement?.OwnerClan == Clan.PlayerClan)
		{
			reason = "目标定居点属于玩家家族，不能通过叛乱攻击自己的封地。";
			return false;
		}
		Kingdom oldKingdom = Clan.PlayerClan?.Kingdom;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, defenderFaction, out reason))
		{
			return false;
		}
		if (!apply)
		{
			return true;
		}
		string actionText = settlement?.IsVillage == true ? ("烧掠" + GetSettlementName(settlement)) : ("围攻" + GetSettlementName(settlement));
		return TryApplyPlayerClanRebellionForWorldMapAttack(actorHero, party, oldKingdom, actionText, out reason);
	}

	private static bool CanPlayerClanRebelForWorldMapAttack(Hero actorHero, MobileParty party, Kingdom oldKingdom, IFaction targetFaction, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			reason = "玩家家族不存在，无法执行叛乱攻击。";
			return false;
		}
		if (actorHero == null || actorHero == Hero.MainHero || actorHero.Clan != playerClan)
		{
			reason = "只有玩家家族的同伴独立部队可以代表玩家执行叛乱攻击。";
			return false;
		}
		if (!IsPartyUsable(party) || party == MobileParty.MainParty || party.LeaderHero != actorHero)
		{
			reason = "执行者当前没有可控制的独立同伴部队，无法执行叛乱攻击。";
			return false;
		}
		if (party.ActualClan != null && party.ActualClan != playerClan)
		{
			reason = "执行者部队不属于玩家家族，无法触发玩家家族叛乱。";
			return false;
		}
		if (oldKingdom == null)
		{
			reason = "玩家家族当前不属于任何王国，不需要也无法执行带城叛乱。";
			return false;
		}
		if (targetFaction != null && targetFaction != oldKingdom)
		{
			reason = "目标不属于玩家当前王国，不能作为带城叛乱攻击的触发目标。";
			return false;
		}
		if (oldKingdom.IsEliminated)
		{
			reason = "玩家当前王国已经灭亡，无法执行带城叛乱。";
			return false;
		}
		if (playerClan.IsUnderMercenaryService)
		{
			reason = "玩家家族当前是雇佣兵关系，不能执行带城叛乱。";
			return false;
		}
		if (oldKingdom.RulingClan == playerClan)
		{
			reason = "玩家家族是当前王国统治家族，不能对自己的王国执行带城叛乱。";
			return false;
		}
		if (playerClan.Settlements == null || playerClan.Settlements.Count <= 0)
		{
			reason = "玩家家族没有封地，不能执行带城叛乱。";
			return false;
		}
		return true;
	}

	private bool TryApplyPlayerClanRebellionForWorldMapAttack(Hero actorHero, MobileParty party, Kingdom oldKingdom, string actionText, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, oldKingdom, out reason))
		{
			return false;
		}
		try
		{
			string oldKingdomName = GetFactionDisplayName(oldKingdom);
			ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(playerClan, showNotification: true);
			bool leftOldKingdom = playerClan.Kingdom != oldKingdom;
			bool atWar = FactionManager.IsAtWarAgainstFaction(playerClan, oldKingdom);
			if (!leftOldKingdom || !atWar)
			{
				reason = "玩家家族叛乱动作未能建立与旧王国的战争状态。";
				Log("player_clan_rebellion_attack_failed_verify actor=" + (actorHero?.StringId ?? "") + " oldKingdom=" + SafeFactionId(oldKingdom) + " left=" + leftOldKingdom + " atWar=" + atWar);
				return false;
			}
			LogFact(actorHero, "玩家家族已带领封地脱离" + oldKingdomName + "并发动叛乱，" + GetHeroName(actorHero) + "随后继续执行" + (actionText ?? "攻击目标") + "的命令。");
			Log("player_clan_rebellion_attack_commit actor=" + (actorHero?.StringId ?? "") + " oldKingdom=" + SafeFactionId(oldKingdom) + " mode=" + AttackModeForce + " autoRebellion=True " + DescribePartyAi(party));
			return true;
		}
		catch (Exception ex)
		{
			reason = "玩家家族叛乱动作执行失败：" + ex.Message;
			Log("player_clan_rebellion_attack_exception actor=" + (actorHero?.StringId ?? "") + " oldKingdom=" + SafeFactionId(oldKingdom) + " error=" + ex);
			return false;
		}
	}

	private static bool ArePartiesAtWar(MobileParty party, MobileParty targetParty)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = targetParty?.MapFaction;
			return attackerFaction != null && defenderFaction != null && FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction);
		}
		catch
		{
			return false;
		}
	}

	private static bool ArePartyAndSettlementSameFaction(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction partyFaction = party?.MapFaction;
			IFaction settlementFaction = settlement?.MapFaction;
			return partyFaction != null && settlementFaction != null && partyFaction == settlementFaction;
		}
		catch
		{
			return false;
		}
	}

	private float EstimateAttackStrengthWithNearbyAllies(MobileParty party, MobileParty targetParty)
	{
		float strength = EstimatePartyStrength(party);
		try
		{
			IFaction faction = party.MapFaction;
			if (faction == null || targetParty == null)
			{
				return strength;
			}
			foreach (MobileParty other in MobileParty.All)
			{
				if (other == null || other == party || other == targetParty || !IsPartyUsable(other) || other.MapFaction != faction)
				{
					continue;
				}
				if (IsPartyNearParty(other, targetParty, FriendlySupportRadius))
				{
					strength += EstimatePartyStrength(other);
				}
			}
		}
		catch
		{
		}
		return strength;
	}

	private float EstimateAttackStrengthWithNearbyAllies(MobileParty party, Settlement settlement)
	{
		float strength = EstimatePartyStrength(party);
		try
		{
			IFaction faction = party?.MapFaction;
			if (faction == null || settlement == null)
			{
				return strength;
			}
			CampaignVec2 center = GetSettlementAttackPosition(settlement);
			foreach (MobileParty other in MobileParty.All)
			{
				if (other == null || other == party || !IsPartyUsable(other) || other.MapFaction != faction)
				{
					continue;
				}
				if (IsPartyNearPosition(other, center, FriendlySupportRadius))
				{
					strength += EstimatePartyStrength(other);
				}
			}
		}
		catch
		{
		}
		return strength;
	}

	private static float EstimatePartyStrength(MobileParty party)
	{
		try
		{
			float value = party?.GetTotalLandStrengthWithFollowers(includeNonAttachedArmyMembers: true) ?? 0f;
			if (value > 0f)
			{
				return value;
			}
		}
		catch
		{
		}
		try
		{
			return party?.Party?.EstimatedStrength ?? 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private static float EstimateSettlementDefenseStrength(Settlement settlement)
	{
		try
		{
			float value = settlement?.Party?.EstimatedStrength ?? 0f;
			if (value > 0f)
			{
				return value;
			}
		}
		catch
		{
		}
		try
		{
			return settlement?.Town?.GarrisonParty?.Party?.EstimatedStrength ?? 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private List<PartyCommandQueueState> GetActiveAttackStatesSnapshot(string resultKind = null)
	{
		lock (_queueLock)
		{
			return _queues.Values
				.Where(x => x != null && IsCurrentAttackCommand(x) && !x.ResultLogged)
				.Where(x => string.IsNullOrWhiteSpace(resultKind) || string.Equals((x.ResultKind ?? "").Trim(), resultKind, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}
	}

	private static PartyCommandEntry GetCurrentCommand(PartyCommandQueueState state)
	{
		if (state?.Commands == null || state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			return null;
		}
		return state.Commands[state.CurrentIndex];
	}

	private static bool IsCurrentAttackCommand(PartyCommandQueueState state)
	{
		PartyCommandEntry command = GetCurrentCommand(state);
		return command != null && (IsKind(command, CommandKind.AttackHero) || IsKind(command, CommandKind.AttackParty));
	}

	private static void BeginResultTracking(PartyCommandQueueState state, string resultKind, string targetType, string targetId, string targetName, IFaction actorFaction, IFaction targetFaction)
	{
		if (state == null)
		{
			return;
		}
		state.ResultKind = (resultKind ?? "").Trim();
		state.ResultTargetType = (targetType ?? "").Trim();
		state.ResultTargetId = (targetId ?? "").Trim();
		state.ResultTargetName = (targetName ?? "").Trim();
		state.ResultActorFactionId = SafeFactionId(actorFaction);
		state.ResultTargetFactionId = SafeFactionId(targetFaction);
		state.ResultCommitDay = NowDay();
		state.ResultDeadlineDay = state.TimeoutDay;
		state.ResultLogged = false;
	}

	private static void ResetResultTracking(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return;
		}
		state.ResultKind = "";
		state.ResultTargetType = "";
		state.ResultTargetId = "";
		state.ResultTargetName = "";
		state.ResultActorFactionId = "";
		state.ResultTargetFactionId = "";
		state.ResultCommitDay = -1.0;
		state.ResultDeadlineDay = -1.0;
		state.ResultLogged = false;
	}

	private void LogTerminalAttackFailure(PartyCommandQueueState state, string detail, string reason)
	{
		if (state == null || state.ResultLogged || !IsCurrentAttackCommand(state))
		{
			return;
		}
		PartyCommandEntry command = GetCurrentCommand(state);
		Hero hero = ResolveHeroByIdAny(state.HeroId);
		state.ResultLogged = true;
		LogFact(state, hero, BuildAttackResultFact(hero, state, command, CommandResultOutcome.Failure, detail));
		string queueKey = GetQueueKey(state);
		if (!string.IsNullOrWhiteSpace(queueKey))
		{
			lock (_queueLock)
			{
				_queues.Remove(queueKey);
			}
		}
		LogFact(state, hero, GetActorName(state, hero, null) + "的大地图命令队列已经结束（" + GetQueueEndReasonText(reason) + "）。");
	}

	private bool TryCompleteCurrentAttackResult(PartyCommandQueueState state, CommandResultOutcome outcome, string detail, string reason)
	{
		if (state == null || state.ResultLogged || !IsCurrentAttackCommand(state))
		{
			return false;
		}
		PartyCommandEntry command = GetCurrentCommand(state);
		Hero hero = ResolveHeroByIdAny(state.HeroId);
		state.ResultLogged = true;
		LogFact(state, hero, BuildAttackResultFact(hero, state, command, outcome, detail));
		MobileParty activeParty = ResolveActorParty(state, hero);
		if (activeParty != null)
		{
			AdvanceCommand(hero, activeParty, state, reason);
			return true;
		}
		MobileParty releaseParty = ResolveActorParty(state, hero, allowNonLeaderForRelease: true);
		FinishQueue(hero, releaseParty, state, reason, appendFact: true);
		return true;
	}

	private static string BuildAttackResultFact(Hero hero, PartyCommandQueueState state, PartyCommandEntry command, CommandResultOutcome outcome, string detail)
	{
		string actorName = GetStoredActorName(state, hero);
		string targetName = GetStoredTargetName(state, command);
		string safeDetail = NormalizeResultDetail(detail, outcome);
		if (IsSettlementTarget(command))
		{
			bool isRaid = string.Equals((state?.ResultKind ?? "").Trim(), "raid", StringComparison.OrdinalIgnoreCase) || ResolveSettlementById(command?.TargetId)?.IsVillage == true;
			if (isRaid)
			{
				if (outcome == CommandResultOutcome.Success)
				{
					return actorName + "成功烧掠" + targetName + "：" + safeDetail;
				}
				if (outcome == CommandResultOutcome.Failure)
				{
					return actorName + "烧掠" + targetName + "失败：" + safeDetail;
				}
				return actorName + "对" + targetName + "的烧掠未能完成：" + safeDetail;
			}
			if (outcome == CommandResultOutcome.Success)
			{
				return actorName + "围攻" + targetName + "成功：" + safeDetail;
			}
			if (outcome == CommandResultOutcome.Failure)
			{
				return actorName + "围攻" + targetName + "失败：" + safeDetail;
			}
			return actorName + "对" + targetName + "的围攻未能完成：" + safeDetail;
		}
		if (outcome == CommandResultOutcome.Success)
		{
			return actorName + "对" + targetName + "的攻击成功：" + safeDetail;
		}
		if (outcome == CommandResultOutcome.Failure)
		{
			return actorName + "对" + targetName + "的攻击失败：" + safeDetail;
		}
		return actorName + "对" + targetName + "的攻击未能完成：" + safeDetail;
	}

	private static string NormalizeResultDetail(string detail, CommandResultOutcome outcome)
	{
		string text = (detail ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = outcome == CommandResultOutcome.Success ? "目标已被达成。" : (outcome == CommandResultOutcome.Failure ? "原版事件判定为失败。" : "没有取得明确结果。");
		}
		if (!text.EndsWith("。", StringComparison.Ordinal) && !text.EndsWith("！", StringComparison.Ordinal) && !text.EndsWith("？", StringComparison.Ordinal))
		{
			text += "。";
		}
		return text;
	}

	private static string BuildAttackTimeoutDetail(PartyCommandEntry command, PartyCommandQueueState state)
	{
		try
		{
			if (command != null && IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command) && !string.IsNullOrWhiteSpace(state?.LastIssuedActionKey) && state.LastIssuedActionKey.StartsWith("wait_hero_shelter:", StringComparison.OrdinalIgnoreCase))
			{
				Hero targetHero = ResolveHeroByIdAny(command.TargetId);
				Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, targetHero?.PartyBelongedTo);
				if (shelter != null)
				{
					return GetHeroName(targetHero) + "仍在" + GetSettlementName(shelter) + "内，命令时限已到，未能接战。";
				}
			}
			if (command != null && IsKind(command, CommandKind.AttackParty) && !string.IsNullOrWhiteSpace(state?.LastIssuedActionKey) && state.LastIssuedActionKey.StartsWith("wait_party_shelter:", StringComparison.OrdinalIgnoreCase))
			{
				MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
				Settlement shelter = targetParty?.CurrentSettlement;
				if (shelter != null)
				{
					return GetPartyName(targetParty) + "仍在" + GetSettlementName(shelter) + "内，命令时限已到，未能接战。";
				}
			}
		}
		catch
		{
		}
		return "命令时限已到，未能取得明确结果。";
	}

	private static string BuildCommandTimeoutFact(PartyCommandQueueState state, Hero hero, PartyCommandEntry command)
	{
		string actorName = GetActorName(state, hero, null);
		if (command == null)
		{
			return actorName + "的大地图命令时限已到，已跳过当前命令。";
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			return actorName + "未能在时限内抵达" + GetSettlementName(ResolveSettlementById(command.TargetId)) + "，已跳过当前前往命令。";
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			return actorName + "未能在时限内抵达" + GetSettlementName(ResolveSettlementById(command.TargetId)) + "附近，已跳过当前巡逻命令。";
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return actorName + "未能在时限内追上" + GetHeroName(ResolveHeroById(command.TargetId)) + "，已跳过当前跟随命令。";
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return actorName + "未能在时限内追上" + GetPartyName(ResolveMobilePartyById(command.TargetId)) + "，已跳过当前跟随命令。";
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return actorName + "未能在时限内与玩家部队会合，已跳过当前回队命令。";
		}
		return actorName + "的大地图命令时限已到，已跳过当前命令。";
	}

	private static string BuildGoToSettlementStartMessage(PartyCommandQueueState state, Hero hero, MobileParty party, Settlement settlement, PartyCommandEntry command)
	{
		double holdUntilDay = GetCommandHoldUntilDay(command);
		if (holdUntilDay > 0.0)
		{
			return GetActorName(state, hero, party) + "开始前往" + GetSettlementName(settlement) + "，抵达后停留至指定期限。";
		}
		return GetActorName(state, hero, party) + "开始前往" + GetSettlementName(settlement) + "，抵达后停留" + Math.Max(1, command?.Days ?? 1) + "天。";
	}

	private static double GetCommandHoldUntilDay(PartyCommandEntry command)
	{
		return command != null && command.HoldUntilDay > 0.0 ? command.HoldUntilDay : -1.0;
	}

	private static bool TryKeepCommandAliveAfterTimeout(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, double now)
	{
		if (state == null || command == null)
		{
			return false;
		}
		try
		{
			if (state.ArrivalDay >= 0.0 && (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement) || IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.FollowParty)))
			{
				state.TimeoutDay = -1.0;
				Log("travel timeout ignored after arrival hero=" + (hero?.StringId ?? "") + " kind=" + (command.Kind ?? "") + " arrivalDay=" + state.ArrivalDay.ToString("0.00"));
				return true;
			}
			if (IsKind(command, CommandKind.GoToSettlement))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && IsPartyAtSettlement(party, settlement, SettlementArrivalDistance))
				{
					state.TimeoutDay = now + 1.0;
					Log("go timeout deferred because party is already at target hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId);
					return true;
				}
			}
			if (IsKind(command, CommandKind.PatrolSettlement))
			{
				if (IsPartyEngagingAnyTarget(party))
				{
					state.TimeoutDay = now + 1.0;
					Log("patrol timeout deferred while engaging hero=" + (hero?.StringId ?? "") + " " + DescribePartyAi(party));
					return true;
				}
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && IsPartyNearSettlementForPatrol(party, settlement, PatrolLeashDistance))
				{
					state.TimeoutDay = now + 1.0;
					Log("patrol timeout deferred near area hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " distance=" + GetDistanceToSettlementForPatrol(party, settlement).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.FollowHero))
			{
				MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
				if (targetParty != null && IsPartyCloseEnoughToStartFollowing(party, targetParty))
				{
					state.TimeoutDay = now + 1.0;
					Log("follow timeout deferred because party is already near target hero=" + (hero?.StringId ?? "") + " target=" + command.TargetId + " distance=" + GetPartyDistance(party, targetParty).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.FollowParty))
			{
				MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
				if (targetParty != null && IsPartyCloseEnoughToStartFollowing(party, targetParty))
				{
					state.TimeoutDay = now + 1.0;
					Log("follow party timeout deferred because party is already near target hero=" + (hero?.StringId ?? "") + " targetParty=" + command.TargetId + " distance=" + GetPartyDistance(party, targetParty).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.MergeToPlayer) && MobileParty.MainParty != null)
			{
				if (IsPartyNearParty(party, MobileParty.MainParty, MergeArrivalDistance))
				{
					state.TimeoutDay = now + 0.25;
					Log("merge timeout deferred because party is already near player hero=" + (hero?.StringId ?? "") + " distance=" + GetPartyDistance(party, MobileParty.MainParty).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.AttackHero) && IsSettlementTarget(command) && state.EngageCommitted)
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && !IsSettlementAttackComplete(party, settlement) && IsPartyCommittedToSettlementAttack(party, settlement))
				{
					state.TimeoutDay = now + 1.0;
					if (settlement.IsVillage)
					{
						EnsureCommittedRaidBehavior(party, settlement);
					}
					LockPartyAi(party);
					Log("settlement attack timeout deferred while committed hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Log("timeout recovery failed hero=" + (hero?.StringId ?? "") + " kind=" + (command?.Kind ?? "") + " error=" + ex.Message);
		}
		return false;
	}

	private static string GetStoredActorName(PartyCommandQueueState state, Hero hero = null)
	{
		return GetActorName(state, hero ?? ResolveHeroByIdAny(state?.HeroId), null);
	}

	private static string GetStoredTargetName(PartyCommandQueueState state, PartyCommandEntry command)
	{
		string stored = (state?.ResultTargetName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(stored))
		{
			return stored;
		}
		if (IsSettlementTarget(command))
		{
			return GetSettlementName(ResolveSettlementById(command?.TargetId));
		}
		if (IsKind(command, CommandKind.AttackParty) || IsKind(command, CommandKind.FollowParty))
		{
			return GetPartyName(ResolveMobilePartyById(command?.TargetId));
		}
		return GetHeroName(ResolveHeroByIdAny(command?.TargetId));
	}

	private static string BuildMapEventCasualtySummary(MapEvent mapEvent, BattleSideEnum actorSide, BattleSideEnum targetSide)
	{
		try
		{
			MapEventSide actorEventSide = mapEvent?.GetMapEventSide(actorSide);
			MapEventSide targetEventSide = mapEvent?.GetMapEventSide(targetSide);
			if (actorEventSide == null || targetEventSide == null)
			{
				return "";
			}
			int actorLosses = Math.Max(0, actorEventSide.TroopCasualties);
			int targetLosses = Math.Max(0, targetEventSide.TroopCasualties);
			return " 战斗损失：己方" + actorLosses + "人，敌方" + targetLosses + "人。";
		}
		catch
		{
			return "";
		}
	}

	private static BattleSideEnum GetHeroSideInMapEvent(MapEvent mapEvent, string heroId)
	{
		if (mapEvent == null || string.IsNullOrWhiteSpace(heroId))
		{
			return BattleSideEnum.None;
		}
		if (MapEventSideHasHero(mapEvent.AttackerSide, heroId))
		{
			return BattleSideEnum.Attacker;
		}
		if (MapEventSideHasHero(mapEvent.DefenderSide, heroId))
		{
			return BattleSideEnum.Defender;
		}
		return BattleSideEnum.None;
	}

	private static BattleSideEnum GetActorSideInMapEvent(MapEvent mapEvent, PartyCommandQueueState state)
	{
		if (mapEvent == null || state == null)
		{
			return BattleSideEnum.None;
		}
		if (!string.IsNullOrWhiteSpace(state.HeroId))
		{
			BattleSideEnum heroSide = GetHeroSideInMapEvent(mapEvent, state.HeroId);
			if (heroSide != BattleSideEnum.None)
			{
				return heroSide;
			}
		}
		if (MapEventSideHasActor(mapEvent.AttackerSide, state))
		{
			return BattleSideEnum.Attacker;
		}
		if (MapEventSideHasActor(mapEvent.DefenderSide, state))
		{
			return BattleSideEnum.Defender;
		}
		return BattleSideEnum.None;
	}

	private static BattleSideEnum GetPartySideInMapEvent(MapEvent mapEvent, string partyId)
	{
		if (mapEvent == null || string.IsNullOrWhiteSpace(partyId))
		{
			return BattleSideEnum.None;
		}
		if (MapEventSideHasMobileParty(mapEvent.AttackerSide, partyId))
		{
			return BattleSideEnum.Attacker;
		}
		if (MapEventSideHasMobileParty(mapEvent.DefenderSide, partyId))
		{
			return BattleSideEnum.Defender;
		}
		return BattleSideEnum.None;
	}

	private static bool MapEventSideHasHero(MapEventSide side, string heroId)
	{
		if (side?.Parties == null || string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				if (PartyBaseMatchesHero(party?.Party, heroId))
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

	private static bool MapEventSideHasMobileParty(MapEventSide side, string partyId)
	{
		if (side?.Parties == null || string.IsNullOrWhiteSpace(partyId))
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				if (PartyBaseMatchesMobileParty(party?.Party, partyId))
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

	private static bool IsTargetSettlement(PartyCommandEntry command, Settlement settlement)
	{
		return command != null && settlement != null && IsSettlementTarget(command) && string.Equals((command.TargetId ?? "").Trim(), (settlement.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsVillageLooted(Settlement settlement)
	{
		try
		{
			return settlement?.IsVillage == true && (settlement.IsRaided || settlement.Village?.VillageState == Village.VillageStates.Looted);
		}
		catch
		{
			return false;
		}
	}

	private static bool PartyMatchesHero(MobileParty party, string heroId)
	{
		return !string.IsNullOrWhiteSpace(heroId) && string.Equals(party?.LeaderHero?.StringId, heroId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyMatchesActor(MobileParty party, PartyCommandQueueState state)
	{
		if (party == null || state == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(state.HeroId) && PartyMatchesHero(party, state.HeroId))
		{
			return true;
		}
		string actorKey = GetQueueKey(state);
		string partyKey = BuildPartyActorKey(party, createGuid: false);
		if (!string.IsNullOrWhiteSpace(actorKey) && !string.IsNullOrWhiteSpace(partyKey) && string.Equals(actorKey, partyKey, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(state.PartyStringId) && string.Equals(party.StringId, state.PartyStringId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return state.PartyIndex >= 0 && GetPartyIndexSafe(party) == state.PartyIndex;
	}

	private static bool PartyBaseMatchesHero(PartyBase party, string heroId)
	{
		return !string.IsNullOrWhiteSpace(heroId) && string.Equals(party?.LeaderHero?.StringId, heroId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyBaseMatchesActor(PartyBase party, PartyCommandQueueState state)
	{
		if (party == null || state == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(state.HeroId) && PartyBaseMatchesHero(party, state.HeroId))
		{
			return true;
		}
		return PartyMatchesActor(party.MobileParty, state);
	}

	private static bool PartyBaseMatchesMobileParty(PartyBase party, string partyId)
	{
		return !string.IsNullOrWhiteSpace(partyId) && string.Equals(party?.MobileParty?.StringId, partyId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyMatchesFaction(MobileParty party, string factionId)
	{
		return !string.IsNullOrWhiteSpace(factionId) && string.Equals(SafeFactionId(party?.MapFaction), factionId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyBaseMatchesFaction(PartyBase party, string factionId)
	{
		return !string.IsNullOrWhiteSpace(factionId) && string.Equals(SafeFactionId(party?.MapFaction), factionId, StringComparison.OrdinalIgnoreCase);
	}

	private static string GetQueueEndReasonText(string reason)
	{
		string text = (reason ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text == "queue_done")
		{
			return "所有命令执行完毕";
		}
		if (text.StartsWith("merge_invalid:", StringComparison.OrdinalIgnoreCase))
		{
			return "回队合并条件已失效";
		}
		if (text.IndexOf("merge_transfer_failed", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "回队资产转入失败";
		}
		if (text.IndexOf("merge_destroy_failed", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "原独立部队无法安全解散";
		}
		if (text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "命令时限已到";
		}
		if (text.IndexOf("actor", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "执行者失效";
		}
		if (text.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("settlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "目标失效或不可执行";
		}
		if (text.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "命令无效";
		}
		return text;
	}

	private void AdvanceCommand(Hero hero, MobileParty party, PartyCommandQueueState state, string reason, bool terminalFailure = false)
	{
		Log("advance actor=" + GetActorLogId(state, hero, party) + " index=" + state.CurrentIndex + " reason=" + reason);
		if (HasFollowSiegeState(state) && !TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: false, "advance:" + reason))
		{
			SetPendingSafeExit(state, PendingSafeExitAdvance, reason);
			RequestFollowSiegeRefresh();
			return;
		}
		ClearPendingSafeExit(state);
		AbortCurrentCommandIfNeeded(party, state);
		ResetResultTracking(state);
		state.CurrentIndex++;
		state.Stage = CommandStage.New.ToString();
		state.ArrivalDay = -1.0;
		state.TimeoutDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.LastStatusMessageKey = "";
		if (state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, terminalFailure ? reason : "queue_done", appendFact: true);
			return;
		}
		StartCurrentCommand(hero, party, state);
	}

	private void FinishQueue(Hero hero, MobileParty party, PartyCommandQueueState state, string reason, bool appendFact)
	{
		if (HasFollowSiegeState(state) && !TryExitFollowSiegeControl(party, state, detachPreexistingParticipation: false, "finish:" + reason))
		{
			SetPendingSafeExit(state, PendingSafeExitStop, reason);
			RequestFollowSiegeRefresh();
			if (appendFact)
			{
				LogFact(state, hero, GetActorName(state, hero, party) + "的大地图命令队列已经结束；当前原版战斗结算后将安全退出攻城并回归原版行动状态。");
			}
			return;
		}
		if (party != null)
		{
			AbortCurrentCommandIfNeeded(party, state);
			ReleasePartyAi(party);
		}
		string queueKey = GetQueueKey(state);
		if (!string.IsNullOrWhiteSpace(queueKey))
		{
			lock (_queueLock)
			{
				_queues.Remove(queueKey);
			}
		}
		bool governorExpeditionHandled = BeginGovernorExpeditionReturn(hero, party, "queue_finished:" + reason);
		bool returningGovernorExpedition = governorExpeditionHandled
			&& TryGetGovernorExpeditionForHero(hero?.StringId, out GovernorExpeditionRecord finishingRecord)
			&& string.Equals(finishingRecord.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase);
		if (appendFact)
		{
			LogFact(state, hero, GetActorName(state, hero, party) + "的大地图命令队列已经结束（" + GetQueueEndReasonText(reason) + "），"
				+ (returningGovernorExpedition ? "临时总督远征队开始返驻地交还兵员。"
					: governorExpeditionHandled ? "临时总督远征已按当前游戏状态结束或交还原版 AI。" : "回归原版行动状态。"));
		}
		Log("finish actor=" + GetActorLogId(state, hero, party) + " reason=" + reason);
	}

	private static bool TryParseTag(string tag, bool validateTargets, out PartyCommandEntry command, out bool stop)
	{
		command = null;
		stop = false;
		string text = (tag ?? "").Trim();
		const string prefix = "[ACTION:WORLDMAP_ORDER:";
		if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !text.EndsWith("]", StringComparison.Ordinal))
		{
			return false;
		}
		string inner = text.Substring(prefix.Length, text.Length - prefix.Length - 1);
		string[] parts = inner.Split(new[] { ':' }, StringSplitOptions.None).Select(x => (x ?? "").Trim()).ToArray();
		if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
		{
			return false;
		}
		string kind = parts[0].ToUpperInvariant();
		if (kind == "STOP")
		{
			stop = true;
			return true;
		}
		if (kind == "MERGE_TO_PLAYER")
		{
			command = new PartyCommandEntry
			{
				Kind = CommandKind.MergeToPlayer.ToString(),
				Days = ParseDays(parts.Length >= 2 ? parts[1] : null)
			};
			return true;
		}
		if (kind == "GO_TO_SETTLEMENT" || kind == "PATROL_SETTLEMENT")
		{
			if (parts.Length < 3 || !string.Equals(parts[1], "settlement", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (validateTargets && ResolveSettlementById(id) == null)
			{
				return false;
			}
			command = new PartyCommandEntry
			{
				Kind = (kind == "GO_TO_SETTLEMENT") ? CommandKind.GoToSettlement.ToString() : CommandKind.PatrolSettlement.ToString(),
				TargetType = "settlement",
				TargetId = id,
				Days = ParseDays(parts.Length >= 4 ? parts[3] : null)
			};
			return true;
		}
		if (kind == "FOLLOW")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.FollowHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
					Mode = ""
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.FollowParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
					Mode = ""
				};
				return true;
			}
			return false;
		}
		if (kind == "FOLLOW_HERO")
		{
			if (parts.Length < 3 || !string.Equals(parts[1], "hero", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (validateTargets && ResolveHeroById(id) == null)
			{
				return false;
			}
			command = new PartyCommandEntry
			{
				Kind = CommandKind.FollowHero.ToString(),
				TargetType = "hero",
				TargetId = id,
				Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
				Mode = ""
			};
			return true;
		}
		if (kind == "ATTACK")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				Settlement settlement = ResolveSettlementById(id);
				if (validateTargets && !IsSupportedAttackSettlement(settlement))
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "settlement",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, GetDefaultAttackDaysForSettlement(settlement)),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			return false;
		}
		if (kind == "ATTACK_HERO")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				Settlement settlement = ResolveSettlementById(id);
				if (validateTargets && !IsSupportedAttackSettlement(settlement))
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "settlement",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, GetDefaultAttackDaysForSettlement(settlement)),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
		}
		return false;
	}

	private static string BuildTag(PartyCommandEntry command)
	{
		if (command == null)
		{
			return "";
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			return "[ACTION:WORLDMAP_ORDER:GO_TO_SETTLEMENT:settlement:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			return "[ACTION:WORLDMAP_ORDER:PATROL_SETTLEMENT:settlement:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return "[ACTION:WORLDMAP_ORDER:FOLLOW:hero:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return "[ACTION:WORLDMAP_ORDER:FOLLOW:party:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			string targetType = IsSettlementTarget(command) ? "settlement" : "hero";
			return "[ACTION:WORLDMAP_ORDER:ATTACK:" + targetType + ":" + command.TargetId + ":" + Math.Max(1, command.Days) + ":" + NormalizeAttackMode(command.Mode) + "]";
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			return "[ACTION:WORLDMAP_ORDER:ATTACK:party:" + command.TargetId + ":" + Math.Max(1, command.Days) + ":" + NormalizeAttackMode(command.Mode) + "]";
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return "[ACTION:WORLDMAP_ORDER:MERGE_TO_PLAYER:" + Math.Max(1, command.Days) + "]";
		}
		return "";
	}

	private static int ParseDays(string token)
	{
		return ParseDays(token, 1);
	}

	private static int ParseDays(string token, int defaultDays)
	{
		if (!int.TryParse((token ?? "").Trim(), out int result) || result <= 0)
		{
			return Math.Max(1, defaultDays);
		}
		return result;
	}

	private static string NormalizeAttackMode(string token)
	{
		string text = (token ?? "").Trim();
		if (string.Equals(text, LegacyAttackModeRebellionForce, StringComparison.OrdinalIgnoreCase))
		{
			return AttackModeForce;
		}
		if (string.Equals(text, AttackModeForce, StringComparison.OrdinalIgnoreCase))
		{
			return AttackModeForce;
		}
		return AttackModeAi;
	}

	private static bool IsForceAttackMode(string mode)
	{
		string normalized = NormalizeAttackMode(mode);
		return string.Equals(normalized, AttackModeForce, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsExecutableCommand(PartyCommandEntry command)
	{
		if (command == null)
		{
			return false;
		}
		command.Days = Math.Max(1, command.Days);
		command.Mode = NormalizeAttackMode(command.Mode);
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return true;
		}
		if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement))
		{
			return string.Equals(command.TargetType, "settlement", StringComparison.OrdinalIgnoreCase) && ResolveSettlementById(command.TargetId) != null;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return string.Equals(command.TargetType, "hero", StringComparison.OrdinalIgnoreCase) && ResolveHeroById(command.TargetId) != null;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return string.Equals(command.TargetType, "party", StringComparison.OrdinalIgnoreCase) && ResolveMobilePartyById(command.TargetId) != null;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			if (string.Equals(command.TargetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				return ResolveHeroById(command.TargetId) != null;
			}
			return string.Equals(command.TargetType, "settlement", StringComparison.OrdinalIgnoreCase) && IsSupportedAttackSettlement(ResolveSettlementById(command.TargetId));
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			return string.Equals(command.TargetType, "party", StringComparison.OrdinalIgnoreCase) && ResolveMobilePartyById(command.TargetId) != null;
		}
		return false;
	}

	private static bool MapEventSideHasActor(MapEventSide side, PartyCommandQueueState state)
	{
		if (side?.Parties == null || state == null)
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				if (PartyBaseMatchesActor(party?.Party, state))
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

	private static bool IsExecutableNonHeroPartyCommand(PartyCommandEntry command)
	{
		return command != null
			&& !IsKind(command, CommandKind.MergeToPlayer)
			&& IsExecutableCommand(command);
	}

	private static PartyCommandEntry CloneCommand(PartyCommandEntry command)
	{
		return new PartyCommandEntry
		{
			Kind = command.Kind,
			TargetType = NormalizeTargetType(command),
			TargetId = command.TargetId,
			Days = Math.Max(1, command.Days),
			HoldUntilDay = command.HoldUntilDay > 0.0 ? command.HoldUntilDay : -1.0,
			Mode = NormalizeAttackMode(command.Mode),
			RequiresExistingWar = command.RequiresExistingWar
		};
	}

	private static bool ShouldConvertGoToSettlementCommands(string sourceId)
	{
		return string.IsNullOrWhiteSpace(NormalizeExternalSourceId(sourceId));
	}

	private static List<PartyCommandEntry> ConvertGoToSettlementCommandsToAttacks(Hero hero, MobileParty party, List<PartyCommandEntry> commands, out int hostileSettlementConvertedCount, out int besiegedSettlementConvertedCount)
	{
		hostileSettlementConvertedCount = 0;
		besiegedSettlementConvertedCount = 0;
		if (commands == null || commands.Count == 0 || !IsPartyUsable(party))
		{
			return commands ?? new List<PartyCommandEntry>();
		}
		List<PartyCommandEntry> result = new List<PartyCommandEntry>(commands.Count);
		foreach (PartyCommandEntry command in commands)
		{
			if (TryBuildGoToSettlementAttackCommand(party, command, out PartyCommandEntry attackCommand, out Settlement settlement, out MobileParty besiegerParty))
			{
				result.Add(attackCommand);
				if (besiegerParty != null)
				{
					besiegedSettlementConvertedCount++;
					Log("go_to_besieged_settlement_converted hero=" + (hero?.StringId ?? "") + " party=" + (party.StringId ?? "") + " settlement=" + (settlement?.StringId ?? command?.TargetId ?? "") + " besieger=" + (besiegerParty.StringId ?? "") + " days=" + Math.Max(1, command?.Days ?? 1));
				}
				else
				{
					hostileSettlementConvertedCount++;
					Log("go_to_hostile_settlement_converted hero=" + (hero?.StringId ?? "") + " party=" + (party.StringId ?? "") + " settlement=" + (settlement?.StringId ?? command?.TargetId ?? "") + " days=" + Math.Max(1, command?.Days ?? 1));
				}
				continue;
			}
			result.Add(command);
		}
		return result;
	}

	private static bool TryConvertCurrentGoToSettlementCommand(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, string phase)
	{
		if (state == null || !ShouldConvertGoToSettlementCommands(state.SourceId))
		{
			return false;
		}
		if (!TryBuildGoToSettlementAttackCommand(party, command, out PartyCommandEntry attackCommand, out Settlement settlement, out MobileParty besiegerParty))
		{
			return false;
		}
		command.Kind = attackCommand.Kind;
		command.TargetType = attackCommand.TargetType;
		command.TargetId = attackCommand.TargetId;
		command.Days = attackCommand.Days;
		command.HoldUntilDay = attackCommand.HoldUntilDay;
		command.Mode = attackCommand.Mode;
		command.RequiresExistingWar = attackCommand.RequiresExistingWar;
		state.ArrivalDay = -1.0;
		state.TimeoutDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.Stage = CommandStage.New.ToString();
		if (besiegerParty != null)
		{
			NotifyCommandStatus(state, "go_to_besieged_settlement_converted:" + (phase ?? "") + ":" + (settlement?.StringId ?? "") + ":" + (besiegerParty.StringId ?? ""), GetActorName(state, hero, party) + "原本要前往的" + GetSettlementName(settlement) + "正在被敌对的" + GetPartyName(besiegerParty) + "围攻，改为攻击围城部队。", CommandMessageTone.Progress);
			Log("current_go_to_besieged_settlement_converted actor=" + GetActorLogId(state, hero, party) + " party=" + (party?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " besieger=" + (besiegerParty.StringId ?? "") + " phase=" + (phase ?? "") + " days=" + Math.Max(1, command.Days));
		}
		else
		{
			NotifyCommandStatus(state, "go_to_hostile_settlement_converted:" + (phase ?? "") + ":" + (settlement?.StringId ?? command.TargetId ?? ""), GetActorName(state, hero, party) + "原本要前往的" + GetSettlementName(settlement) + "已是敌对定居点，改按AI攻击命令执行。", CommandMessageTone.Progress);
			Log("current_go_to_hostile_settlement_converted actor=" + GetActorLogId(state, hero, party) + " party=" + (party?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? command.TargetId ?? "") + " phase=" + (phase ?? "") + " days=" + Math.Max(1, command.Days));
		}
		return true;
	}

	private static bool TryBuildGoToSettlementAttackCommand(MobileParty party, PartyCommandEntry command, out PartyCommandEntry attackCommand, out Settlement settlement, out MobileParty besiegerParty)
	{
		attackCommand = null;
		settlement = null;
		besiegerParty = null;
		if (!IsKind(command, CommandKind.GoToSettlement) || !IsPartyUsable(party))
		{
			return false;
		}
		settlement = ResolveSettlementById(command.TargetId);
		if (TryResolveHostileBesiegerParty(party, settlement, out besiegerParty))
		{
			attackCommand = new PartyCommandEntry
			{
				Kind = CommandKind.AttackParty.ToString(),
				TargetType = "party",
				TargetId = besiegerParty.StringId,
				Days = Math.Max(1, command.Days),
				HoldUntilDay = -1.0,
				Mode = AttackModeForce,
				RequiresExistingWar = true
			};
			return true;
		}
		if (!IsSupportedAttackSettlement(settlement) || !IsPartyAtWarWithSettlement(party, settlement))
		{
			return false;
		}
		attackCommand = new PartyCommandEntry
		{
			Kind = CommandKind.AttackHero.ToString(),
			TargetType = "settlement",
			TargetId = command.TargetId,
			Days = Math.Max(1, command.Days),
			HoldUntilDay = -1.0,
			Mode = AttackModeAi
		};
		return true;
	}

	private static bool TryResolveHostileBesiegerParty(MobileParty party, Settlement settlement, out MobileParty besiegerParty)
	{
		besiegerParty = null;
		try
		{
			SiegeEvent siegeEvent = settlement?.SiegeEvent;
			BesiegerCamp besiegerCamp = siegeEvent?.BesiegerCamp;
			MobileParty leaderParty = besiegerCamp?.LeaderParty;
			if (siegeEvent == null
				|| siegeEvent.BesiegedSettlement != settlement
				|| !settlement.IsUnderSiege
				|| !IsPartyUsable(leaderParty)
				|| leaderParty == party
				|| string.IsNullOrWhiteSpace(leaderParty.StringId)
				|| !ArePartiesAtWar(party, leaderParty))
			{
				return false;
			}
			besiegerParty = leaderParty;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildGoToSettlementConversionSummary(int hostileSettlementConvertedCount, int besiegedSettlementConvertedCount)
	{
		List<string> parts = new List<string>(2);
		if (besiegedSettlementConvertedCount > 0)
		{
			parts.Add(besiegedSettlementConvertedCount + "道被围攻定居点前往命令已改为攻击敌对围城部队");
		}
		if (hostileSettlementConvertedCount > 0)
		{
			parts.Add(hostileSettlementConvertedCount + "道敌对定居点前往命令已按AI攻击处理");
		}
		return parts.Count > 0 ? "，其中" + string.Join("，", parts) : "";
	}

	private static string BuildGoToSettlementConversionMessage(int hostileSettlementConvertedCount, int besiegedSettlementConvertedCount)
	{
		if (hostileSettlementConvertedCount <= 0 && besiegedSettlementConvertedCount <= 0)
		{
			return "";
		}
		if (hostileSettlementConvertedCount > 0 && besiegedSettlementConvertedCount > 0)
		{
			return "，敌对定居点按AI攻击、被围攻定居点改攻敌对围城部队";
		}
		return besiegedSettlementConvertedCount > 0 ? "，被围攻定居点改攻敌对围城部队" : "，敌对定居点按AI攻击";
	}

	private static string NormalizeTargetType(PartyCommandEntry command)
	{
		if (command == null)
		{
			return "";
		}
		if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement) || IsSettlementTarget(command))
		{
			return "settlement";
		}
		if (IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.AttackHero))
		{
			return "hero";
		}
		if (IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty))
		{
			return "party";
		}
		return (command.TargetType ?? "").Trim();
	}

	private static bool IsKind(PartyCommandEntry command, CommandKind kind)
	{
		return command != null && string.Equals((command.Kind ?? "").Trim(), kind.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSettlementTarget(PartyCommandEntry command)
	{
		return command != null && string.Equals((command.TargetType ?? "").Trim(), "settlement", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSupportedAttackSettlement(Settlement settlement)
	{
		try
		{
			return settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
		}
		catch
		{
			return false;
		}
	}

	private static int GetDefaultAttackDaysForSettlement(Settlement settlement)
	{
		if (settlement?.IsVillage == true)
		{
			return DefaultRaidAttackDays;
		}
		if (settlement?.IsTown == true || settlement?.IsCastle == true)
		{
			return DefaultSiegeAttackDays;
		}
		return DefaultHeroAttackDays;
	}

	private static CommandStage ParseStage(string value)
	{
		if (Enum.TryParse((value ?? "").Trim(), ignoreCase: true, out CommandStage stage))
		{
			return stage;
		}
		return CommandStage.New;
	}

	private static void NormalizeState(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return;
		}
		List<PartyCommandEntry> originalCommands = state.Commands ?? new List<PartyCommandEntry>();
		int originalIndex = Math.Max(0, state.CurrentIndex);
		List<PartyCommandEntry> validCommands = new List<PartyCommandEntry>();
		int rebasedIndex = 0;
		bool currentCommandKept = false;
		for (int i = 0; i < originalCommands.Count; i++)
		{
			PartyCommandEntry command = originalCommands[i];
			if (!IsExecutableCommand(command))
			{
				continue;
			}
			if (i < originalIndex)
			{
				rebasedIndex++;
			}
			else if (i == originalIndex)
			{
				currentCommandKept = true;
			}
			validCommands.Add(command);
		}
		state.Commands = validCommands;
		state.CurrentIndex = Math.Max(0, rebasedIndex);
		if (!currentCommandKept && originalIndex < originalCommands.Count)
		{
			state.Stage = CommandStage.New.ToString();
			state.CommandStartDay = 0.0;
			state.ArrivalDay = -1.0;
			state.TimeoutDay = -1.0;
			state.EngageCommitted = false;
			state.LastIssuedActionKey = "";
			state.LastStatusMessageKey = "";
			ClearFollowSiegeState(state);
			ClearPendingSafeExit(state);
			ResetResultTracking(state);
		}
		state.HeroId = (state.HeroId ?? "").Trim();
		state.ActorKey = (state.ActorKey ?? "").Trim();
		state.ActorName = (state.ActorName ?? "").Trim();
		state.PartyStringId = (state.PartyStringId ?? "").Trim();
		state.NonHeroMemoryId = (state.NonHeroMemoryId ?? "").Trim();
		state.NonHeroMemoryName = (state.NonHeroMemoryName ?? "").Trim();
		state.FollowSiegeSettlementId = (state.FollowSiegeSettlementId ?? "").Trim();
		state.PendingSafeExitAction = (state.PendingSafeExitAction ?? "").Trim();
		state.PendingSafeExitReason = (state.PendingSafeExitReason ?? "").Trim();
		state.ArmySurvivalRenewalKey = (state.ArmySurvivalRenewalKey ?? "").Trim();
		if (double.IsNaN(state.ArmySurvivalLastPaidDay) || double.IsInfinity(state.ArmySurvivalLastPaidDay) || state.ArmySurvivalLastPaidDay < -1.0)
		{
			state.ArmySurvivalLastPaidDay = -1.0;
		}
		if (string.IsNullOrWhiteSpace(state.FollowSiegeSettlementId))
		{
			state.FollowSiegeJoinedByCommand = false;
			ClearPendingSafeExit(state);
		}
		if (!string.Equals(state.PendingSafeExitAction, PendingSafeExitResumeFollow, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(state.PendingSafeExitAction, PendingSafeExitAdvance, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(state.PendingSafeExitAction, PendingSafeExitStop, StringComparison.OrdinalIgnoreCase))
		{
			ClearPendingSafeExit(state);
		}
		if (!IsCurrentFollowCommand(state))
		{
			ClearFollowSiegeState(state);
			ClearPendingSafeExit(state);
		}
		if (state.PartyIndex < -1)
		{
			state.PartyIndex = -1;
		}
		if (string.IsNullOrWhiteSpace(state.ActorKey) && !string.IsNullOrWhiteSpace(state.HeroId))
		{
			state.ActorKey = state.HeroId;
		}
		if (string.IsNullOrWhiteSpace(state.ActorName))
		{
			Hero hero = ResolveHeroByIdAny(state.HeroId);
			if (hero != null)
			{
				state.ActorName = GetHeroName(hero);
			}
			else
			{
				MobileParty party = ResolveMobilePartyByActorState(state);
				state.ActorName = GetPartyName(party);
			}
		}
		if (string.IsNullOrWhiteSpace(state.Stage))
		{
			state.Stage = CommandStage.New.ToString();
		}
		state.SourceId = NormalizeExternalSourceId(state.SourceId);
		foreach (PartyCommandEntry command in state.Commands)
		{
			if (command != null)
			{
				command.Days = Math.Max(1, command.Days);
				command.HoldUntilDay = command.HoldUntilDay > 0.0 ? command.HoldUntilDay : -1.0;
				command.Mode = NormalizeAttackMode(command.Mode);
				if (IsKind(command, CommandKind.AttackHero) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "hero";
				}
				if ((IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement)) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "settlement";
				}
				if (IsKind(command, CommandKind.FollowHero) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "hero";
				}
				if ((IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty)) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "party";
				}
			}
		}
	}

	private static Settlement ResolveSettlementById(string id)
	{
		string text = (id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Settlement.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveHeroById(string id)
	{
		string text = (id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Hero.AllAliveHeroes?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveHeroByIdAny(string id)
	{
		string text = (id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			Hero hero = Hero.Find(text);
			if (hero != null)
			{
				return hero;
			}
		}
		catch
		{
		}
		try
		{
			return Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return ResolveHeroById(text);
		}
	}

	private static MobileParty ResolveTargetHeroParty(string heroId)
	{
		Hero hero = ResolveHeroById(heroId);
		if (hero == null || hero.IsDead || hero.IsPrisoner)
		{
			return null;
		}
		MobileParty party = hero.PartyBelongedTo;
		return IsPartyUsable(party) ? party : null;
	}

	private static MobileParty ResolveMobilePartyById(string id)
	{
		string text = (id ?? "").Trim();
		if (text.StartsWith("party:", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("party:".Length).Trim();
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return MobileParty.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveTargetHeroShelterSettlement(Hero hero, MobileParty targetParty)
	{
		try
		{
			if (hero == null || hero.IsDead || hero.IsPrisoner)
			{
				return null;
			}
			if (targetParty?.MapEvent != null && !targetParty.MapEvent.IsFinalized)
			{
				return null;
			}
			Settlement settlement = targetParty?.CurrentSettlement ?? hero.CurrentSettlement;
			return settlement;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsHeroActuallyInPlayerMainPartyRoster(Hero hero)
	{
		try
		{
			if (hero == null || hero == Hero.MainHero || hero.CharacterObject == null || MobileParty.MainParty?.MemberRoster == null)
			{
				return false;
			}
			return MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(element => element.Character == hero.CharacterObject && element.Number > 0);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsTaskCommandForImplicitPartyCreation(PartyCommandEntry command)
	{
		return command != null
			&& (IsKind(command, CommandKind.GoToSettlement)
				|| IsKind(command, CommandKind.PatrolSettlement)
				|| IsKind(command, CommandKind.FollowHero)
				|| IsKind(command, CommandKind.FollowParty)
				|| IsKind(command, CommandKind.AttackHero)
				|| IsKind(command, CommandKind.AttackParty));
	}

	private static bool TryValidateDetachedPartyRecord(PlayerDetachedPartyRecord record, out Hero hero, out MobileParty party)
	{
		hero = ResolveHeroByIdAny(record?.HeroId);
		party = ResolveMobilePartyById(record?.PartyStringId);
		if (party == null && record != null && record.PartyIndex >= 0)
		{
			try
			{
				party = MobileParty.All?.FirstOrDefault(candidate => candidate != null && GetPartyIndexSafe(candidate) == record.PartyIndex);
			}
			catch
			{
				party = null;
			}
		}
		return hero != null
			&& hero != Hero.MainHero
			&& !IsHeroActuallyInPlayerMainPartyRoster(hero)
			&& IsPartyUsable(party)
			&& party != MobileParty.MainParty
			&& party.LeaderHero == hero
			&& hero.PartyBelongedTo == party;
	}

	private void RegisterPlayerDetachedParty(Hero hero, MobileParty party)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId) || !IsPartyUsable(party) || party.LeaderHero != hero)
		{
			return;
		}
		lock (_queueLock)
		{
			_playerDetachedParties[hero.StringId] = new PlayerDetachedPartyRecord
			{
				HeroId = hero.StringId,
				PartyStringId = party.StringId,
				PartyIndex = GetPartyIndexSafe(party)
			};
		}
		Log("registered player detachment hero=" + hero.StringId + " party=" + (party.StringId ?? ""));
	}

	private void RemovePlayerDetachedParty(Hero hero, MobileParty party, string reason)
	{
		string heroId = hero?.StringId ?? party?.LeaderHero?.StringId ?? "";
		List<string> removedIds = new List<string>();
		lock (_queueLock)
		{
			if (!string.IsNullOrWhiteSpace(heroId) && _playerDetachedParties.Remove(heroId))
			{
				removedIds.Add(heroId);
			}
			if (party != null)
			{
				string partyId = party.StringId ?? "";
				int partyIndex = GetPartyIndexSafe(party);
				foreach (KeyValuePair<string, PlayerDetachedPartyRecord> pair in _playerDetachedParties.ToList())
				{
					PlayerDetachedPartyRecord record = pair.Value;
					if ((!string.IsNullOrWhiteSpace(partyId) && string.Equals(record?.PartyStringId ?? "", partyId, StringComparison.OrdinalIgnoreCase))
						|| (partyIndex >= 0 && record?.PartyIndex == partyIndex))
					{
						_playerDetachedParties.Remove(pair.Key);
						removedIds.Add(pair.Key);
					}
				}
			}
		}
		foreach (string removedId in removedIds.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Log("removed player detachment hero=" + removedId + " reason=" + reason);
		}
	}

	private bool IsRegisteredPlayerDetachment(Hero hero, MobileParty party)
	{
		if (hero == null || party == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return false;
		}
		PlayerDetachedPartyRecord record;
		lock (_queueLock)
		{
			_playerDetachedParties.TryGetValue(hero.StringId, out record);
		}
		return TryValidateDetachedPartyRecord(record, out Hero validHero, out MobileParty validParty)
			&& validHero == hero
			&& validParty == party;
	}

	private void PruneInvalidPlayerDetachedParties()
	{
		List<string> invalidHeroIds = new List<string>();
		lock (_queueLock)
		{
			foreach (KeyValuePair<string, PlayerDetachedPartyRecord> pair in _playerDetachedParties)
			{
				if (!TryValidateDetachedPartyRecord(pair.Value, out Hero _, out MobileParty _))
				{
					invalidHeroIds.Add(pair.Key);
				}
			}
			foreach (string heroId in invalidHeroIds)
			{
				_playerDetachedParties.Remove(heroId);
			}
		}
		foreach (string heroId in invalidHeroIds)
		{
			Log("pruned invalid player detachment hero=" + heroId);
		}
	}

	public static bool CanUseNonHeroPartyFallbackForExternal(CharacterObject targetCharacter, int targetAgentIndex)
	{
		return TryResolveNonHeroPartyActorForExternal(targetCharacter, targetAgentIndex, out _);
	}

	public static bool TryResolveNonHeroPartyActorForExternal(CharacterObject targetCharacter, int targetAgentIndex, out MobileParty party)
	{
		party = null;
		try
		{
			if (targetCharacter == null || targetCharacter.HeroObject != null || targetCharacter.IsHero)
			{
				return false;
			}
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		party = ResolveNonHeroPartyFromAgent(targetAgentIndex);
		if (!IsValidNonHeroPartyFallbackParty(party))
		{
			party = ResolveNonHeroPartyFromEncounter();
		}
		if (!IsValidNonHeroPartyFallbackParty(party))
		{
			party = ResolveNonHeroPartyFromConversation();
		}
		return IsValidNonHeroPartyFallbackParty(party);
	}

	private static MobileParty ResolveNonHeroPartyFromAgent(int targetAgentIndex)
	{
		try
		{
			Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive()) : null;
			PartyBase partyBase = agent?.Origin?.BattleCombatant as PartyBase;
			if (partyBase != null && partyBase.IsMobile && partyBase.MobileParty != null)
			{
				return partyBase.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static MobileParty ResolveNonHeroPartyFromEncounter()
	{
		try
		{
			PartyBase encounteredParty = PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
			if (encounteredParty != null && encounteredParty.IsMobile && encounteredParty.MobileParty != null)
			{
				return encounteredParty.MobileParty;
			}
		}
		catch
		{
		}
		try
		{
			return PlayerEncounter.EncounteredMobileParty;
		}
		catch
		{
			return null;
		}
	}

	private static MobileParty ResolveNonHeroPartyFromConversation()
	{
		try
		{
			return MobileParty.ConversationParty;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsValidNonHeroPartyFallbackParty(MobileParty party)
	{
		try
		{
			return party != null
				&& party.IsActive
				&& party.Party != null
				&& party != MobileParty.MainParty
				&& party.Party != PartyBase.MainParty
				&& party.LeaderHero == null
				&& !CourierDeliveryBehavior.IsCourierParty(party);
		}
		catch
		{
			return false;
		}
	}

	private static string NormalizeActorKeyPart(string value)
	{
		string text = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim().ToLowerInvariant();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		return text;
	}

	private static string BuildPartyActorKey(MobileParty party, bool createGuid)
	{
		if (party == null)
		{
			return "";
		}
		string partyStringId = NormalizeActorKeyPart(party.StringId);
		if (!string.IsNullOrWhiteSpace(partyStringId))
		{
			return "party_string_id:" + partyStringId;
		}
		string savedPartyKey = "";
		try
		{
			savedPartyKey = createGuid ? MyBehavior.GetOrCreateWildernessNonHeroPartyMemoryKeyForExternal(party) : MyBehavior.GetExistingWildernessNonHeroPartyMemoryKeyForExternal(party);
		}
		catch
		{
			savedPartyKey = "";
		}
		savedPartyKey = NormalizeActorKeyPart(savedPartyKey);
		if (!string.IsNullOrWhiteSpace(savedPartyKey))
		{
			return savedPartyKey;
		}
		int partyIndex = GetPartyIndexSafe(party);
		if (partyIndex >= 0)
		{
			return "party_index:" + partyIndex;
		}
		return "";
	}

	private static string GetQueueKey(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return "";
		}
		string actorKey = (state.ActorKey ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(actorKey))
		{
			return actorKey;
		}
		return (state.HeroId ?? "").Trim();
	}

	private static int GetPartyIndexSafe(MobileParty party)
	{
		try
		{
			return party?.Party?.Index ?? -1;
		}
		catch
		{
			return -1;
		}
	}

	private static string BuildPartyMemoryId(MobileParty party)
	{
		string actorKey = BuildPartyActorKey(party, createGuid: true);
		if (string.IsNullOrWhiteSpace(actorKey))
		{
			return "";
		}
		return MyBehavior.BuildNonHeroMemoryIdForExternal("worldmap_party_command|party:" + actorKey);
	}

	private static MobileParty ResolveMobilePartyByActorState(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return null;
		}
		MobileParty byStringId = ResolveMobilePartyById(state.PartyStringId);
		if (byStringId != null)
		{
			return byStringId;
		}
		string actorKey = GetQueueKey(state);
		if (string.IsNullOrWhiteSpace(actorKey))
		{
			return null;
		}
		const string stringPrefix = "party_string_id:";
		if (actorKey.StartsWith(stringPrefix, StringComparison.OrdinalIgnoreCase))
		{
			byStringId = ResolveMobilePartyById(actorKey.Substring(stringPrefix.Length));
			if (byStringId != null)
			{
				return byStringId;
			}
		}
		try
		{
			IEnumerable<MobileParty> allParties = MobileParty.All;
			if (allParties == null)
			{
				return null;
			}
			foreach (MobileParty party in allParties)
			{
				if (party == null)
				{
					continue;
				}
				string candidateKey = BuildPartyActorKey(party, createGuid: false);
				if (!string.IsNullOrWhiteSpace(candidateKey) && string.Equals(candidateKey, actorKey, StringComparison.OrdinalIgnoreCase))
				{
					return party;
				}
				if (state.PartyIndex >= 0 && GetPartyIndexSafe(party) == state.PartyIndex)
				{
					return party;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static MobileParty ResolveActorParty(Hero hero, bool allowNonLeaderForRelease = false)
	{
		if (hero == null || hero.IsDead || hero.IsPrisoner)
		{
			return null;
		}
		MobileParty party = (hero == Hero.MainHero) ? MobileParty.MainParty : hero.PartyBelongedTo;
		if (!IsPartyUsable(party))
		{
			return null;
		}
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return null;
		}
		if (!allowNonLeaderForRelease && party.LeaderHero != hero)
		{
			return null;
		}
		return party;
	}

	private static MobileParty ResolveActorParty(PartyCommandQueueState state, Hero hero, bool allowNonLeaderForRelease = false)
	{
		if (hero != null)
		{
			return ResolveActorParty(hero, allowNonLeaderForRelease);
		}
		if (state == null)
		{
			return null;
		}
		MobileParty party = ResolveMobilePartyByActorState(state);
		if (!IsPartyUsable(party))
		{
			return null;
		}
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return null;
		}
		if (!allowNonLeaderForRelease && party.LeaderHero != null)
		{
			return null;
		}
		return party;
	}

	private static MobileParty ResolvePartyForSafeExit(PartyCommandQueueState state, Hero hero)
	{
		MobileParty party = ResolveActorParty(state, hero, allowNonLeaderForRelease: true);
		if (party == null && state != null)
		{
			party = ResolveMobilePartyByActorState(state);
		}
		if (!IsPartyUsable(party) || party == MobileParty.MainParty || CourierDeliveryBehavior.IsCourierParty(party))
		{
			return null;
		}
		return party;
	}

	private static bool ValidateActor(Hero hero, MobileParty party, out string reason)
	{
		reason = "";
		if (hero == null || hero.IsDead || hero.IsPrisoner)
		{
			reason = "hero_invalid";
			return false;
		}
		if (!IsPartyUsable(party))
		{
			reason = "party_invalid";
			return false;
		}
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			reason = "courier_party";
			return false;
		}
		if (party.LeaderHero != hero)
		{
			reason = "not_party_leader";
			return false;
		}
		return true;
	}

	private static bool ValidateActor(PartyCommandQueueState state, Hero hero, MobileParty party, out string reason)
	{
		if (hero != null)
		{
			return ValidateActor(hero, party, out reason);
		}
		reason = "";
		if (state == null || string.IsNullOrWhiteSpace(GetQueueKey(state)))
		{
			reason = "actor_invalid";
			return false;
		}
		if (!IsPartyUsable(party))
		{
			reason = "party_invalid";
			return false;
		}
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			reason = "courier_party";
			return false;
		}
		if (party == MobileParty.MainParty || party.Party == PartyBase.MainParty)
		{
			reason = "main_party";
			return false;
		}
		if (party.LeaderHero != null)
		{
			reason = "party_now_has_hero_leader";
			return false;
		}
		string expectedKey = GetQueueKey(state);
		string actualKey = BuildPartyActorKey(party, createGuid: false);
		if (!string.IsNullOrWhiteSpace(expectedKey) && !string.IsNullOrWhiteSpace(actualKey) && !string.Equals(expectedKey, actualKey, StringComparison.OrdinalIgnoreCase))
		{
			reason = "party_key_changed";
			return false;
		}
		return true;
	}

	private static bool IsPartyUsable(MobileParty party)
	{
		try
		{
			return party != null && party.IsActive && party.Party != null;
		}
		catch
		{
			return false;
		}
	}

	private static void LockPartyAi(MobileParty party)
	{
		try
		{
			party?.Ai?.SetDoNotMakeNewDecisions(true);
		}
		catch
		{
		}
	}

	private static void ReleasePartyAi(MobileParty party)
	{
		try
		{
			party?.Ai?.SetDoNotMakeNewDecisions(false);
		}
		catch
		{
		}
	}

	private static bool IsAiDecisionLockActive(MobileParty party)
	{
		try
		{
			return party?.Ai?.DoNotMakeNewDecisions == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyTrackingTarget(MobileParty party, MobileParty targetParty)
	{
		try
		{
			return party != null && targetParty != null && party.DefaultBehavior == AiBehavior.GoAroundParty && party.TargetParty == targetParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyEngagingTarget(MobileParty party, MobileParty targetParty)
	{
		try
		{
			return party != null && targetParty != null && party.DefaultBehavior == AiBehavior.EngageParty && party.TargetParty == targetParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyEngagingAnyTarget(MobileParty party)
	{
		try
		{
			if (party?.MapEvent != null && !party.MapEvent.IsFinalized)
			{
				return true;
			}
			return party != null
				&& (party.DefaultBehavior == AiBehavior.EngageParty
					|| party.ShortTermBehavior == AiBehavior.EngageParty
					|| party.DefaultBehavior == AiBehavior.GoAroundParty
					|| party.ShortTermBehavior == AiBehavior.GoAroundParty)
				&& (party.TargetParty != null || party.Ai?.AiBehaviorPartyBase?.MobileParty != null);
		}
		catch
		{
			return false;
		}
	}

	private static void IssueMergeApproachAction(MobileParty party, MobileParty mainParty)
	{
		if (!IsPartyUsable(party) || !IsPartyUsable(mainParty))
		{
			return;
		}
		if (mainParty.CurrentSettlement != null)
		{
			SetPartyAiAction.GetActionForVisitingSettlement(party, mainParty.CurrentSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			return;
		}
		// EscortParty caps a faster returning party to the player's speed, so an
		// existing gap can never close. Refreshing a point target once per hourly
		// command tick keeps native pathfinding while allowing normal catch-up speed.
		party.SetMoveGoToPoint(mainParty.Position, MobileParty.NavigationType.Default);
	}

	private static bool IsMergeApproachActionCurrent(MobileParty party, MobileParty mainParty)
	{
		try
		{
			if (!IsPartyUsable(party) || !IsPartyUsable(mainParty))
			{
				return false;
			}
			if (mainParty.CurrentSettlement != null)
			{
				return party.DefaultBehavior == AiBehavior.GoToSettlement && party.TargetSettlement == mainParty.CurrentSettlement;
			}
			return party.DefaultBehavior == AiBehavior.GoToPoint
				&& party.TargetPosition.DistanceSquared(mainParty.Position) <= 1f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyEscortingTarget(MobileParty party, MobileParty targetParty)
	{
		try
		{
			return party != null && targetParty != null && party.DefaultBehavior == AiBehavior.EscortParty && party.TargetParty == targetParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyVisitingSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			return party != null
				&& settlement != null
				&& ((party.DefaultBehavior == AiBehavior.GoToSettlement && party.TargetSettlement == settlement)
					|| (party.ShortTermBehavior == AiBehavior.GoToSettlement && party.ShortTermTargetSettlement == settlement));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyPatrollingSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			return party != null
				&& settlement != null
				&& party.DefaultBehavior == AiBehavior.PatrolAroundPoint
				&& party.TargetSettlement == settlement;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyNearSettlementForPatrol(MobileParty party, Settlement settlement, float distance)
	{
		try
		{
			if (party == null || settlement == null)
			{
				return false;
			}
			if (party.CurrentSettlement == settlement)
			{
				return true;
			}
			if (IsPartyNearPosition(party, settlement.GatePosition, distance))
			{
				return true;
			}
			CampaignVec2 center = GetSettlementMapPosition(settlement);
			return IsPartyNearPosition(party, center, distance);
		}
		catch
		{
			return false;
		}
	}

	private static float GetDistanceToSettlementForPatrol(MobileParty party, Settlement settlement)
	{
		try
		{
			if (party == null || settlement == null)
			{
				return 0f;
			}
			float gateDistance = party.Position.Distance(settlement.GatePosition);
			float centerDistance = party.Position.Distance(GetSettlementMapPosition(settlement));
			return Math.Min(gateDistance, centerDistance);
		}
		catch
		{
			return 0f;
		}
	}

	private static CampaignVec2 GetSettlementMapPosition(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return CampaignVec2.Zero;
			}
			return new CampaignVec2(settlement.GetPosition2D, settlement.GatePosition.IsOnLand);
		}
		catch
		{
			return settlement?.GatePosition ?? CampaignVec2.Zero;
		}
	}

	private static string DescribePartyAi(MobileParty party)
	{
		try
		{
			return "default=" + party.DefaultBehavior + " short=" + party.ShortTermBehavior + " target=" + (party.TargetParty?.StringId ?? "null") + " locked=" + (party.Ai?.DoNotMakeNewDecisions == true ? "true" : "false");
		}
		catch
		{
			return "";
		}
	}

	private static bool PreemptBlockingWorldActivityForCommand(Hero hero, MobileParty party, PartyCommandEntry command, PartyCommandQueueState state, string phase)
	{
		try
		{
			if (!IsPartyUsable(party) || command == null)
			{
				return false;
			}
			bool changed = false;
			List<string> reasons = new List<string>();
			MapEvent mapEvent = null;
			try
			{
				mapEvent = party.MapEvent;
			}
			catch
			{
				mapEvent = null;
			}
			if (mapEvent != null && !mapEvent.IsFinalized && !IsCommandContinuingCurrentSettlementAttack(party, command, mapEvent.MapEventSettlement, mapEvent))
			{
				string waitKey = "preempt_wait_map_event:" + (phase ?? "") + ":" + (command.Kind ?? "") + ":" + (command.TargetId ?? "");
				NotifyCommandStatus(state, waitKey, GetActorName(state, hero, party) + "正在参与" + GetMapEventPreemptReason(mapEvent) + "，将在原版战斗结算后执行新的大地图命令。", CommandMessageTone.Progress);
				Log("preempt_deferred actor=" + GetActorLogId(state, hero, party) + " party=" + (party.StringId ?? "") + " event=" + GetMapEventPreemptReason(mapEvent) + " phase=" + (phase ?? ""));
				return false;
			}
			Settlement siegeSettlement = GetPartySiegeSettlementSafe(party);
			if (party.BesiegerCamp != null && !IsCommandContinuingCurrentSettlementAttack(party, command, siegeSettlement, null))
			{
				if (HasActiveSiegeMapEvent(party, siegeSettlement))
				{
					string waitKey = "preempt_wait_siege_event:" + (phase ?? "") + ":" + (command.Kind ?? "") + ":" + (command.TargetId ?? "");
					NotifyCommandStatus(state, waitKey, GetActorName(state, hero, party) + "正在参与当前攻城事件，结算后再退出围城执行新命令。", CommandMessageTone.Progress);
					return false;
				}
				try
				{
					party.BesiegerCamp = null;
					changed = true;
					reasons.Add("当前围城");
				}
				catch (Exception ex)
				{
					Log("preempt siege participation failed party=" + (party.StringId ?? "") + " error=" + ex.Message);
					return false;
				}
			}
			Settlement behaviorSettlement = GetBlockingSettlementBehaviorTarget(party);
			if (behaviorSettlement != null && !IsCommandContinuingCurrentSettlementAttack(party, command, behaviorSettlement, null))
			{
				try
				{
					party.SetMoveModeHold();
					changed = true;
					reasons.Add("当前战略行动");
				}
				catch (Exception ex)
				{
					Log("preempt blocking behavior failed party=" + (party.StringId ?? "") + " error=" + ex.Message);
				}
			}
			if (!changed)
			{
				return true;
			}
			string reason = string.Join("、", reasons.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
			if (string.IsNullOrWhiteSpace(reason))
			{
				reason = "当前原版行动";
			}
			string key = "preempt:" + phase + ":" + (command.Kind ?? "") + ":" + (command.TargetType ?? "") + ":" + (command.TargetId ?? "");
			NotifyCommandStatus(state, key, GetActorName(state, hero, party) + "放弃" + reason + "，改为执行新的大地图命令。", CommandMessageTone.Progress);
			LogFact(state, hero, GetActorName(state, hero, party) + "放弃" + reason + "，改为执行新的大地图命令。");
			Log("preempt_activity actor=" + GetActorLogId(state, hero, party) + " party=" + (party.StringId ?? "") + " reason=" + reason + " command=" + (command.Kind ?? "") + ":" + (command.TargetType ?? "") + ":" + (command.TargetId ?? "") + " phase=" + (phase ?? "") + " " + DescribePartyAi(party));
			return true;
		}
		catch (Exception ex)
		{
			Log("preempt blocking activity failed hero=" + (hero?.StringId ?? "") + " party=" + (party?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static bool IsCommandContinuingCurrentSettlementAttack(MobileParty party, PartyCommandEntry command, Settlement settlement, MapEvent mapEvent)
	{
		try
		{
			if (party == null || command == null || settlement == null || !IsTargetSettlement(command, settlement))
			{
				return false;
			}
			if (settlement.IsVillage)
			{
				return (mapEvent != null && mapEvent.IsRaid)
					|| party.DefaultBehavior == AiBehavior.RaidSettlement
					|| party.ShortTermBehavior == AiBehavior.RaidSettlement;
			}
			return (mapEvent != null && mapEvent.IsSiegeAssault)
				|| party.SiegeEvent != null
				|| party.BesiegedSettlement == settlement
				|| party.DefaultBehavior == AiBehavior.BesiegeSettlement
				|| party.ShortTermBehavior == AiBehavior.BesiegeSettlement
				|| party.DefaultBehavior == AiBehavior.AssaultSettlement
				|| party.ShortTermBehavior == AiBehavior.AssaultSettlement;
		}
		catch
		{
			return false;
		}
	}

	private static Settlement GetPartySiegeSettlementSafe(MobileParty party)
	{
		try
		{
			return party?.BesiegedSettlement ?? party?.SiegeEvent?.BesiegedSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement GetBlockingSettlementBehaviorTarget(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return null;
			}
			if (party.DefaultBehavior == AiBehavior.RaidSettlement
				|| party.DefaultBehavior == AiBehavior.BesiegeSettlement
				|| party.DefaultBehavior == AiBehavior.AssaultSettlement
				|| party.ShortTermBehavior == AiBehavior.RaidSettlement
				|| party.ShortTermBehavior == AiBehavior.BesiegeSettlement
				|| party.ShortTermBehavior == AiBehavior.AssaultSettlement)
			{
				return party.TargetSettlement ?? party.ShortTermTargetSettlement ?? party.BesiegedSettlement;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string GetMapEventPreemptReason(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null)
			{
				return "当前原版事件";
			}
			if (mapEvent.IsRaid)
			{
				return "当前烧掠";
			}
			if (mapEvent.IsForcingSupplies)
			{
				return "当前强征补给";
			}
			if (mapEvent.IsForcingVolunteers)
			{
				return "当前强征兵员";
			}
			if (mapEvent.IsSiegeAssault)
			{
				return "当前攻城";
			}
		}
		catch
		{
		}
		return "当前原版事件";
	}

	private static void LeaveArmyIfNeeded(MobileParty party)
	{
		try
		{
			if (party?.Army != null && party.Army.LeaderParty != party)
			{
				party.Army = null;
			}
		}
		catch (Exception ex)
		{
			Log("leave army failed: " + ex.Message);
		}
	}

	private static void SynchronizeArmyObjectiveForCommand(MobileParty party, PartyCommandEntry command)
	{
		try
		{
			if (party?.Army == null || party.Army.LeaderParty != party || command == null)
			{
				return;
			}
			Army army = party.Army;
			IMapPoint oldObject = army.AiBehaviorObject;
			Army.ArmyTypes oldType = army.ArmyType;
			if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null)
				{
					army.ArmyType = Army.ArmyTypes.Defender;
					army.AiBehaviorObject = settlement;
				}
			}
			else if (IsKind(command, CommandKind.AttackHero) && IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null)
				{
					army.ArmyType = settlement.IsVillage ? Army.ArmyTypes.Raider : Army.ArmyTypes.Besieger;
					army.AiBehaviorObject = settlement;
				}
			}
			else if (IsFollowCommand(command))
			{
				army.ArmyType = Army.ArmyTypes.Patrolling;
				army.AiBehaviorObject = null;
			}
			else
			{
				army.AiBehaviorObject = null;
			}
			if (oldType != army.ArmyType || oldObject != army.AiBehaviorObject)
			{
				Log("army_object_sync leader=" + (party.StringId ?? "") + " type=" + army.ArmyType + " target=" + DescribeMapPointForLog(army.AiBehaviorObject));
			}
		}
		catch (Exception ex)
		{
			Log("army objective sync failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static string DescribeMapPointForLog(IMapPoint mapPoint)
	{
		try
		{
			return mapPoint?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "null";
		}
	}

	private static double ComputeTimeoutDay(MobileParty party, PartyCommandEntry command)
	{
		double now = NowDay();
		try
		{
			float distance = 0f;
			if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && party != null)
				{
					distance = IsKind(command, CommandKind.PatrolSettlement) ? GetDistanceToSettlementForPatrol(party, settlement) : party.Position.Distance(settlement.GatePosition);
				}
			}
			else if (IsKind(command, CommandKind.FollowHero) || (IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command)))
			{
				MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
				if (targetParty != null && party != null)
				{
					distance = party.Position.Distance(targetParty.Position);
				}
				else if (IsKind(command, CommandKind.AttackHero) && party != null)
				{
					Settlement shelter = ResolveTargetHeroShelterSettlement(ResolveHeroById(command.TargetId), targetParty);
					if (shelter != null)
					{
						distance = party.Position.Distance(GetSettlementAttackPosition(shelter));
					}
				}
			}
			else if (IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty))
			{
				MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
				if (targetParty != null && party != null)
				{
					distance = party.Position.Distance(targetParty.Position);
				}
				else if (targetParty?.CurrentSettlement != null && party != null)
				{
					distance = party.Position.Distance(GetSettlementAttackPosition(targetParty.CurrentSettlement));
				}
			}
			else if (IsKind(command, CommandKind.AttackHero) && IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && party != null)
				{
					distance = party.Position.Distance(GetSettlementAttackPosition(settlement));
				}
			}
			else if (IsKind(command, CommandKind.MergeToPlayer) && party != null && MobileParty.MainParty != null)
			{
				distance = party.Position.Distance(MobileParty.MainParty.Position);
			}
			float speed = Math.Max(2.0f, party?.Speed ?? 4.0f);
			double estimatedDays = distance / Math.Max(1.0f, speed * 24.0f);
			double timeout = now + Math.Max(1.5, estimatedDays * 3.0 + 1.0);
			if (IsKind(command, CommandKind.PatrolSettlement))
			{
				timeout = Math.Max(timeout, now + Math.Max(3.0, Math.Max(1, command?.Days ?? 1) + 1.0));
			}
			return timeout;
		}
		catch
		{
			return now + 3.0;
		}
	}

	private static bool IsPartyAtSettlement(MobileParty party, Settlement settlement, float distance)
	{
		try
		{
			return party != null && settlement != null && (party.CurrentSettlement == settlement || IsPartyNearPosition(party, settlement.GatePosition, distance));
		}
		catch
		{
			return false;
		}
	}

	private static CampaignVec2 GetSettlementAttackPosition(Settlement settlement)
	{
		try
		{
			return settlement?.GatePosition ?? CampaignVec2.Zero;
		}
		catch
		{
			return CampaignVec2.Zero;
		}
	}

	private static void MoveTowardSettlementAttackPoint(MobileParty party, Settlement settlement)
	{
		if (party == null || settlement == null)
		{
			return;
		}
		try
		{
			party.SetMoveGoToPoint(GetSettlementAttackPosition(settlement), MobileParty.NavigationType.Default);
		}
		catch (Exception ex)
		{
			Log("move settlement attack point failed party=" + (party.StringId ?? "") + " settlement=" + (settlement.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static void LeaveTargetSettlementIfInside(MobileParty party, Settlement settlement)
	{
		try
		{
			if (party != null && settlement != null && party.CurrentSettlement == settlement)
			{
				LeaveSettlementAction.ApplyForParty(party);
			}
		}
		catch (Exception ex)
		{
			Log("leave target settlement before attack failed party=" + (party?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsPartyCommittedToSettlementAttack(MobileParty party, Settlement settlement)
	{
		try
		{
			if (!IsPartyUsable(party) || settlement == null)
			{
				return false;
			}
			if (settlement.IsVillage)
			{
				if ((party.DefaultBehavior == AiBehavior.RaidSettlement && party.TargetSettlement == settlement) || (party.ShortTermBehavior == AiBehavior.RaidSettlement && party.ShortTermTargetSettlement == settlement))
				{
					return true;
				}
				return party.MapEvent != null && !party.MapEvent.IsFinalized && party.MapEvent.IsRaid && party.MapEvent.MapEventSettlement == settlement;
			}
			if ((party.DefaultBehavior == AiBehavior.BesiegeSettlement && party.TargetSettlement == settlement) || party.BesiegedSettlement == settlement)
			{
				return true;
			}
			return party.SiegeEvent != null && party.SiegeEvent.BesiegedSettlement == settlement;
		}
		catch
		{
			return false;
		}
	}

	private static void EnsureCommittedRaidBehavior(MobileParty party, Settlement settlement)
	{
		try
		{
			if (!IsPartyUsable(party) || settlement?.IsVillage != true || IsPartyRaidBehaviorTargetingSettlement(party, settlement))
			{
				return;
			}
			SetPartyAiActionForRaidingSettlement(party, settlement);
			Log("raid_behavior_reasserted party=" + (party.StringId ?? "") + " settlement=" + (settlement.StringId ?? "") + " " + DescribePartyAi(party));
		}
		catch (Exception ex)
		{
			Log("raid behavior reassert failed party=" + (party?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsPartyRaidBehaviorTargetingSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			return IsPartyUsable(party)
				&& settlement != null
				&& ((party.DefaultBehavior == AiBehavior.RaidSettlement && party.TargetSettlement == settlement)
					|| (party.ShortTermBehavior == AiBehavior.RaidSettlement && party.ShortTermTargetSettlement == settlement));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSameFactionSiege(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction partyFaction = party?.MapFaction;
			IFaction siegeFaction = settlement?.SiegeEvent?.BesiegerCamp?.MapFaction;
			return partyFaction != null && siegeFaction != null && partyFaction == siegeFaction;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSettlementAttackComplete(MobileParty party, Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return false;
			}
			if (settlement.IsVillage)
			{
				return settlement.IsRaided;
			}
			IFaction partyFaction = party?.MapFaction;
			return partyFaction != null && settlement.MapFaction == partyFaction && !settlement.IsUnderSiege;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSettlementAttackUnavailableBeforeCommit(Settlement settlement)
	{
		try
		{
			if (settlement?.IsVillage == true)
			{
				return settlement.IsRaided || settlement.IsUnderRaid || settlement.SettlementHitPoints <= 0.001f;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void LogSettlementAttackComplete(Hero hero, Settlement settlement)
	{
		if (hero == null || settlement == null)
		{
			return;
		}
		LogFact(hero, GetHeroName(hero) + (settlement.IsVillage ? "已经完成对" : "已经结束对") + GetSettlementName(settlement) + (settlement.IsVillage ? "的烧掠。" : "的攻击。"));
	}

	private static void AbortCurrentCommandIfNeeded(MobileParty party, PartyCommandQueueState state)
	{
		try
		{
			if (party == null || state == null || state.Commands == null || state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
			{
				return;
			}
			PartyCommandEntry command = state.Commands[state.CurrentIndex];
			if (!IsKind(command, CommandKind.AttackHero) && !IsKind(command, CommandKind.AttackParty))
			{
				return;
			}
			bool isShelterWait = !string.IsNullOrWhiteSpace(state.LastIssuedActionKey) && (state.LastIssuedActionKey.StartsWith("wait_hero_shelter:", StringComparison.OrdinalIgnoreCase) || state.LastIssuedActionKey.StartsWith("wait_party_shelter:", StringComparison.OrdinalIgnoreCase));
			if (!IsSettlementTarget(command) && !isShelterWait)
			{
				return;
			}
			if (party.DefaultBehavior == AiBehavior.BesiegeSettlement || party.DefaultBehavior == AiBehavior.RaidSettlement || party.DefaultBehavior == AiBehavior.GoToPoint)
			{
				party.SetMoveModeHold();
			}
		}
		catch (Exception ex)
		{
			Log("abort current worldmap command failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsPartyNearPosition(MobileParty party, CampaignVec2 position, float distance)
	{
		try
		{
			if (party == null)
			{
				return false;
			}
			return party.Position.DistanceSquared(position) <= distance * distance;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyNearParty(MobileParty a, MobileParty b, float distance)
	{
		try
		{
			if (a == null || b == null)
			{
				return false;
			}
			if (a.CurrentSettlement != null && a.CurrentSettlement == b.CurrentSettlement)
			{
				return true;
			}
			return a.Position.DistanceSquared(b.Position) <= distance * distance;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyCloseEnoughToStartFollowing(MobileParty party, MobileParty targetParty)
	{
		try
		{
			if (IsPartyNearParty(party, targetParty, FollowArrivalDistance))
			{
				return true;
			}
			return IsPartyEscortingTarget(party, targetParty) && IsPartyNearParty(party, targetParty, FollowLeashDistance);
		}
		catch
		{
			return false;
		}
	}

	private static float GetPartyDistance(MobileParty a, MobileParty b)
	{
		try
		{
			if (a == null || b == null)
			{
				return -1f;
			}
			if (a.CurrentSettlement != null && a.CurrentSettlement == b.CurrentSettlement)
			{
				return 0f;
			}
			return a.Position.Distance(b.Position);
		}
		catch
		{
			return -1f;
		}
	}

	private static bool NormalizeGovernorExpeditionRecord(GovernorExpeditionRecord record)
	{
		if (record == null)
		{
			return false;
		}
		record.HeroId = (record.HeroId ?? "").Trim();
		record.PartyStringId = (record.PartyStringId ?? "").Trim();
		record.OriginSettlementId = (record.OriginSettlementId ?? "").Trim();
		record.OriginClanId = (record.OriginClanId ?? "").Trim();
		record.ReturnTargetSettlementId = (record.ReturnTargetSettlementId ?? "").Trim();
		record.LastIssuedActionKey = (record.LastIssuedActionKey ?? "").Trim();
		if (!string.Equals(record.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(record.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase))
		{
			record.Phase = GovernorExpeditionPhaseActive;
		}
		return !string.IsNullOrWhiteSpace(record.HeroId)
			&& (!string.IsNullOrWhiteSpace(record.PartyStringId) || record.PartyIndex >= 0)
			&& !string.IsNullOrWhiteSpace(record.OriginSettlementId);
	}

	private static string BuildGovernorPartyStringIdKey(string partyStringId)
	{
		string text = (partyStringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "" : ("id:" + text);
	}

	private static string BuildGovernorPartyIndexKey(int partyIndex)
	{
		return partyIndex < 0 ? "" : ("index:" + partyIndex);
	}

	private void IndexGovernorExpeditionRecordUnsafe(GovernorExpeditionRecord record)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.HeroId))
		{
			return;
		}
		string stringIdKey = BuildGovernorPartyStringIdKey(record.PartyStringId);
		string indexKey = BuildGovernorPartyIndexKey(record.PartyIndex);
		if (!string.IsNullOrWhiteSpace(stringIdKey))
		{
			_governorExpeditionHeroByPartyKey[stringIdKey] = record.HeroId;
		}
		if (!string.IsNullOrWhiteSpace(indexKey))
		{
			_governorExpeditionHeroByPartyKey[indexKey] = record.HeroId;
		}
	}

	private void UnindexGovernorExpeditionRecordUnsafe(GovernorExpeditionRecord record)
	{
		if (record == null)
		{
			return;
		}
		string stringIdKey = BuildGovernorPartyStringIdKey(record.PartyStringId);
		string indexKey = BuildGovernorPartyIndexKey(record.PartyIndex);
		if (!string.IsNullOrWhiteSpace(stringIdKey)
			&& _governorExpeditionHeroByPartyKey.TryGetValue(stringIdKey, out string stringIdHero)
			&& string.Equals(stringIdHero, record.HeroId, StringComparison.OrdinalIgnoreCase))
		{
			_governorExpeditionHeroByPartyKey.Remove(stringIdKey);
		}
		if (!string.IsNullOrWhiteSpace(indexKey)
			&& _governorExpeditionHeroByPartyKey.TryGetValue(indexKey, out string indexHero)
			&& string.Equals(indexHero, record.HeroId, StringComparison.OrdinalIgnoreCase))
		{
			_governorExpeditionHeroByPartyKey.Remove(indexKey);
		}
	}

	private void RegisterGovernorExpedition(Hero hero, MobileParty party, Settlement originSettlement, Clan originClan)
	{
		if (hero == null || party == null || originSettlement == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			throw new InvalidOperationException("总督远征记录缺少必要身份信息。");
		}
		GovernorExpeditionRecord record = new GovernorExpeditionRecord
		{
			HeroId = hero.StringId,
			PartyStringId = party.StringId,
			PartyIndex = GetPartyIndexSafe(party),
			OriginSettlementId = originSettlement.StringId,
			OriginClanId = originClan?.StringId ?? "",
			Phase = GovernorExpeditionPhaseActive,
			ReturnTargetSettlementId = "",
			LastIssuedActionKey = ""
		};
		lock (_queueLock)
		{
			if (_governorExpeditions.TryGetValue(record.HeroId, out GovernorExpeditionRecord existing))
			{
				UnindexGovernorExpeditionRecordUnsafe(existing);
			}
			_governorExpeditions[record.HeroId] = record;
			IndexGovernorExpeditionRecordUnsafe(record);
			_governorExpeditionPartyByHeroId[record.HeroId] = party;
			_governorReturnTargetByHeroId.Remove(record.HeroId);
			Volatile.Write(ref _hasGovernorExpeditions, 1);
		}
		Log("registered governor expedition hero=" + record.HeroId + " party=" + (record.PartyStringId ?? "") + " origin=" + record.OriginSettlementId);
	}

	private GovernorExpeditionRecord RemoveGovernorExpeditionRecord(string heroId, string reason, bool removeQueue)
	{
		string key = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		GovernorExpeditionRecord removed = null;
		lock (_queueLock)
		{
			if (_governorExpeditions.TryGetValue(key, out removed))
			{
				_governorExpeditions.Remove(key);
				UnindexGovernorExpeditionRecordUnsafe(removed);
			}
			_governorExpeditionPartyByHeroId.Remove(key);
			_governorReturnTargetByHeroId.Remove(key);
			if (removeQueue)
			{
				_queues.Remove(key);
			}
			_pendingGovernorExpeditionRequests.Remove(key);
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, _pendingGovernorExpeditionRequests.Count > 0 ? 1 : 0);
			Volatile.Write(ref _hasGovernorExpeditions, _governorExpeditions.Count > 0 ? 1 : 0);
		}
		if (removed != null)
		{
			Log("removed governor expedition hero=" + key + " reason=" + (reason ?? ""));
		}
		return removed;
	}

	private bool TryGetGovernorExpeditionForHero(string heroId, out GovernorExpeditionRecord record)
	{
		record = null;
		string key = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		lock (_queueLock)
		{
			return _governorExpeditions.TryGetValue(key, out record) && record != null;
		}
	}

	private MobileParty ResolveGovernorExpeditionParty(GovernorExpeditionRecord record)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.HeroId))
		{
			return null;
		}
		lock (_queueLock)
		{
			if (_governorExpeditionPartyByHeroId.TryGetValue(record.HeroId, out MobileParty cachedParty)
				&& GovernorRecordMatchesParty(record, cachedParty))
			{
				return cachedParty;
			}
		}
		MobileParty party = ResolveMobilePartyById(record.PartyStringId);
		if (party == null && record.PartyIndex >= 0)
		{
			try
			{
				party = MobileParty.All?.FirstOrDefault(candidate => candidate != null && GetPartyIndexSafe(candidate) == record.PartyIndex);
			}
			catch
			{
				party = null;
			}
		}
		if (party != null && GovernorRecordMatchesParty(record, party))
		{
			lock (_queueLock)
			{
				_governorExpeditionPartyByHeroId[record.HeroId] = party;
			}
			return party;
		}
		return null;
	}

	private bool TryGetGovernorExpeditionForParty(MobileParty party, out GovernorExpeditionRecord record)
	{
		record = null;
		if (party == null)
		{
			return false;
		}
		string heroId = "";
		string stringIdKey = BuildGovernorPartyStringIdKey(party.StringId);
		string indexKey = BuildGovernorPartyIndexKey(GetPartyIndexSafe(party));
		lock (_queueLock)
		{
			if (!string.IsNullOrWhiteSpace(stringIdKey))
			{
				_governorExpeditionHeroByPartyKey.TryGetValue(stringIdKey, out heroId);
			}
			if (string.IsNullOrWhiteSpace(heroId) && !string.IsNullOrWhiteSpace(indexKey))
			{
				_governorExpeditionHeroByPartyKey.TryGetValue(indexKey, out heroId);
			}
			return !string.IsNullOrWhiteSpace(heroId)
				&& _governorExpeditions.TryGetValue(heroId, out record)
				&& record != null
				&& GovernorRecordMatchesParty(record, party);
		}
	}

	private static bool GovernorRecordMatchesParty(GovernorExpeditionRecord record, MobileParty party)
	{
		if (record == null || party == null)
		{
			return false;
		}
		string recordStringId = (record.PartyStringId ?? "").Trim();
		string partyStringId = (party.StringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(recordStringId) && !string.IsNullOrWhiteSpace(partyStringId))
		{
			return string.Equals(recordStringId, partyStringId, StringComparison.OrdinalIgnoreCase);
		}
		return record.PartyIndex >= 0 && record.PartyIndex == GetPartyIndexSafe(party);
	}

	private bool TryStartGovernorExpeditionRequest(Hero hero, List<PartyCommandEntry> followUpCommands, out string message, out bool queuedForChannelExit)
	{
		message = "";
		queuedForChannelExit = false;
		List<PartyCommandEntry> safeCommands = SanitizeFollowUpCommands(followUpCommands);
		if (safeCommands.Count == 0 || !safeCommands.Any(IsTaskCommandForImplicitPartyCreation))
		{
			message = "没有可用于组建远征队的有效任务命令。";
			return false;
		}
		if (!TryValidateGovernorExpeditionCandidate(hero, out Settlement origin, out MobileParty _, out int _, out string validationMessage))
		{
			message = validationMessage;
			return false;
		}
		if (!CanCreateGovernorExpeditionNow(out string blockedReason))
		{
			if (!TryQueuePendingGovernorExpedition(hero, origin, safeCommands, out message))
			{
				return false;
			}
			queuedForChannelExit = true;
			message = "已记录" + GetHeroName(hero) + "的总督远征请求；" + blockedReason + "，退出后将自动抽调驻军并组建远征队。";
			return true;
		}
		return TryCreateGovernorExpeditionNow(hero, origin.StringId, origin.OwnerClan?.StringId, safeCommands, out message);
	}

	private static bool CanCreateGovernorExpeditionNow(out string blockedReason)
	{
		blockedReason = "";
		if (Campaign.Current == null)
		{
			blockedReason = "战役系统尚未就绪";
			return false;
		}
		if (Mission.Current != null)
		{
			blockedReason = "当前仍在场景中";
			return false;
		}
		if (Campaign.Current.ConversationManager?.IsConversationInProgress == true)
		{
			blockedReason = "当前对话尚未退出";
			return false;
		}
		if (IsPartyScreenStillActive())
		{
			blockedReason = "当前已有部队界面打开";
			return false;
		}
		return true;
	}

	private bool TryQueuePendingGovernorExpedition(Hero hero, Settlement origin, List<PartyCommandEntry> commands, out string message)
	{
		message = "";
		if (hero == null || origin == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			message = "总督远征请求缺少必要身份信息。";
			return false;
		}
		lock (_queueLock)
		{
			if (_pendingGovernorExpeditionRequests.TryGetValue(hero.StringId, out PendingGovernorExpeditionRequest existing) && existing != null)
			{
				if (!string.Equals(existing.OriginSettlementId, origin.StringId, StringComparison.OrdinalIgnoreCase)
					|| !string.Equals(existing.OriginClanId, origin.OwnerClan?.StringId ?? "", StringComparison.OrdinalIgnoreCase))
				{
					message = "已有待处理的总督远征请求，但管辖地或家族已经变化。";
					return false;
				}
				existing.FollowUpCommands = existing.FollowUpCommands ?? new List<PartyCommandEntry>();
				existing.FollowUpCommands.AddRange(SanitizeFollowUpCommands(commands));
			}
			else
			{
				_pendingGovernorExpeditionRequests[hero.StringId] = new PendingGovernorExpeditionRequest
				{
					HeroId = hero.StringId,
					OriginSettlementId = origin.StringId,
					OriginClanId = origin.OwnerClan?.StringId ?? "",
					FollowUpCommands = SanitizeFollowUpCommands(commands)
				};
			}
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, 1);
		}
		Log("queued governor expedition hero=" + hero.StringId + " origin=" + origin.StringId + " commands=" + (commands?.Count ?? 0));
		return true;
	}

	private void ProcessPendingGovernorExpeditionRequests()
	{
		if (Volatile.Read(ref _hasPendingGovernorExpeditionRequests) == 0 || !CanCreateGovernorExpeditionNow(out _))
		{
			return;
		}
		PendingGovernorExpeditionRequest request = null;
		lock (_queueLock)
		{
			request = _pendingGovernorExpeditionRequests.Values.FirstOrDefault();
			if (request != null)
			{
				_pendingGovernorExpeditionRequests.Remove(request.HeroId ?? "");
			}
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, _pendingGovernorExpeditionRequests.Count > 0 ? 1 : 0);
		}
		if (request == null || string.IsNullOrWhiteSpace(request.HeroId))
		{
			return;
		}
		Hero hero = ResolveHeroByIdAny(request.HeroId);
		if (hero == null)
		{
			Log("pending governor expedition skipped missing hero=" + request.HeroId);
			return;
		}
		if (!TryCreateGovernorExpeditionNow(hero, request.OriginSettlementId, request.OriginClanId, request.FollowUpCommands, out string message))
		{
			LogFact(hero, GetHeroName(hero) + "无法组建总督远征队：" + message);
		}
	}

	private static bool TryValidateGovernorExpeditionCandidate(Hero hero, out Settlement origin, out MobileParty garrison, out int partyCapacity, out string message)
	{
		origin = null;
		garrison = null;
		partyCapacity = 0;
		message = "";
		try
		{
			if (hero == null || hero == Hero.MainHero || hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner || hero.IsTraveling)
			{
				message = "该总督当前死亡、被俘、失能或不在可出征状态。";
				return false;
			}
			Town town = hero.GovernorOf;
			origin = town?.Settlement;
			if (town == null || origin == null || town.Governor != hero || (!origin.IsTown && !origin.IsCastle))
			{
				message = "该人物已不是城市或城堡的有效现任总督。";
				return false;
			}
			if (hero.CurrentSettlement != origin)
			{
				message = "该总督当前不在其管辖地内。";
				return false;
			}
			if (hero.Clan == null || origin.OwnerClan != hero.Clan)
			{
				message = "管辖地归属与总督家族不一致。";
				return false;
			}
			if (hero.PartyBelongedTo != null || hero.PartyBelongedToAsPrisoner != null)
			{
				message = "该总督已经属于其他独立部队或俘虏队伍。";
				return false;
			}
			if (!hero.CanLeadParty())
			{
				message = "该总督当前因任务或游戏规则不能带领部队。";
				return false;
			}
			if (origin.IsUnderSiege || HasActiveSettlementMapEvent(origin))
			{
				message = GetSettlementName(origin) + "正在被围攻或进行战斗，不能抽调驻军。";
				return false;
			}
			garrison = town.GarrisonParty;
			if (!IsPartyUsable(garrison) || garrison.CurrentSettlement != origin || garrison.MemberRoster == null || HasActiveMapEvent(garrison))
			{
				message = GetSettlementName(origin) + "当前没有可安全抽调的驻军部队。";
				return false;
			}
			partyCapacity = GetGovernorExpeditionPartyCapacity(hero, hero.Clan);
			if (!TryBuildGovernorTroopTransferPlan(garrison, partyCapacity, out List<GovernorTroopTransferPlan> _, out int _, out message))
			{
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			message = "总督远征资格检查失败：" + ex.Message;
			return false;
		}
	}

	private static int GetGovernorExpeditionPartyCapacity(Hero hero, Clan clan)
	{
		try
		{
			if (hero == null || clan == null)
			{
				return 0;
			}
			int assumedSize = Campaign.Current.Models.PartySizeLimitModel.GetAssumedPartySizeForLordParty(hero, clan.MapFaction, clan);
			return Math.Max(0, assumedSize - 1);
		}
		catch
		{
			return 0;
		}
	}

	private static bool TryBuildGovernorTroopTransferPlan(MobileParty garrison, int partyCapacity, out List<GovernorTroopTransferPlan> plan, out int totalToMove, out string message)
	{
		plan = new List<GovernorTroopTransferPlan>();
		totalToMove = 0;
		message = "";
		if (!IsPartyUsable(garrison) || garrison.MemberRoster == null)
		{
			message = "驻军部队不可用。";
			return false;
		}
		List<TroopRosterElement> candidates = new List<TroopRosterElement>();
		int originalRegulars = 0;
		int healthyRegulars = 0;
		foreach (TroopRosterElement element in garrison.MemberRoster.GetTroopRoster())
		{
			if (element.Character == null || element.Character.IsHero || element.Number <= 0)
			{
				continue;
			}
			originalRegulars += element.Number;
			int healthy = Math.Max(0, element.Number - element.WoundedNumber);
			if (healthy > 0)
			{
				healthyRegulars += healthy;
				candidates.Add(element);
			}
		}
		int reserve = Math.Max(GovernorGarrisonMinimumReserve, (int)Math.Ceiling(originalRegulars * GovernorGarrisonReserveRatio));
		int movableByReserve = Math.Max(0, originalRegulars - reserve);
		totalToMove = Math.Min(Math.Max(0, partyCapacity), Math.Min(healthyRegulars, movableByReserve));
		if (totalToMove < GovernorExpeditionMinimumTroops)
		{
			message = "可抽调健康驻军不足" + GovernorExpeditionMinimumTroops + "人；必须保留至少" + reserve + "名普通驻军并遵守总督队伍上限。";
			return false;
		}
		int remaining = totalToMove;
		foreach (TroopRosterElement element in candidates
			.OrderByDescending(x => x.Character.Tier)
			.ThenBy(x => x.Character.StringId ?? "", StringComparer.Ordinal))
		{
			if (remaining <= 0)
			{
				break;
			}
			int healthy = Math.Max(0, element.Number - element.WoundedNumber);
			int count = Math.Min(healthy, remaining);
			if (count <= 0)
			{
				continue;
			}
			int xp = (int)Math.Min(int.MaxValue, ((long)Math.Max(0, element.Xp) * count) / Math.Max(1, element.Number));
			plan.Add(new GovernorTroopTransferPlan
			{
				Character = element.Character,
				Count = count,
				Xp = xp
			});
			remaining -= count;
		}
		if (remaining != 0)
		{
			plan.Clear();
			totalToMove = 0;
			message = "驻军抽调计划未能形成完整兵员清单。";
			return false;
		}
		return true;
	}

	private bool TryCreateGovernorExpeditionNow(Hero hero, string expectedOriginId, string expectedClanId, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		MobileParty createdParty = null;
		Settlement origin = null;
		MobileParty garrison = null;
		List<TroopRosterElement> garrisonSnapshot = null;
		List<GovernorTroopTransferPlan> transferPlan = null;
		List<RosterElementTransferRecord> transferRecords = new List<RosterElementTransferRecord>();
		try
		{
			List<PartyCommandEntry> safeCommands = SanitizeFollowUpCommands(followUpCommands);
			if (safeCommands.Count == 0 || !safeCommands.Any(IsTaskCommandForImplicitPartyCreation))
			{
				message = "没有可用于远征的有效任务命令。";
				return false;
			}
			if (!TryValidateGovernorExpeditionCandidate(hero, out origin, out garrison, out int _, out message))
			{
				return false;
			}
			if (!string.Equals(origin.StringId, expectedOriginId ?? "", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(hero.Clan?.StringId ?? "", expectedClanId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				message = "总督的管辖地或家族在请求等待期间已经变化。";
				return false;
			}
			garrisonSnapshot = garrison.MemberRoster.GetTroopRoster().ToList();
			Town originTown = origin.Town;
			if (hero.GovernorOf != originTown || originTown.Governor != hero || origin.OwnerClan != hero.Clan)
			{
				message = "总督身份或管辖地归属在建队前已经变化。";
				return false;
			}
			ChangeGovernorAction.RemoveGovernorOf(hero);
			if (hero.GovernorOf != null || originTown.Governor == hero)
			{
				throw new InvalidOperationException("未能安全卸任现任总督。");
			}
			createdParty = MobilePartyHelper.CreateNewClanMobileParty(hero, hero.Clan);
			if (!IsPartyUsable(createdParty) || createdParty == MobileParty.MainParty || createdParty.LeaderHero != hero || hero.PartyBelongedTo != createdParty || createdParty.ActualClan != hero.Clan)
			{
				throw new InvalidOperationException("原版领主队未能以该总督和家族正确建立。");
			}
			PurgeGeneratedGovernorPartyContents(createdParty, hero);
			int actualCapacity = Math.Max(0, createdParty.Party.PartySizeLimit - createdParty.MemberRoster.TotalHeroes);
			if (!TryBuildGovernorTroopTransferPlan(garrison, actualCapacity, out transferPlan, out int totalToMove, out string planMessage))
			{
				throw new InvalidOperationException(planMessage);
			}
			TransferGovernorTroopsWithVerification(garrison, createdParty, transferPlan, transferRecords);
			RegisterGovernorExpedition(hero, createdParty, origin, hero.Clan);
			if (!TryAppendQueue(hero, safeCommands, out string fact, out string queueMessage))
			{
				throw new InvalidOperationException("建队后无法接续命令：" + queueMessage);
			}
			if (!string.IsNullOrWhiteSpace(fact))
			{
				MyBehavior.AppendExternalDialogueHistory(hero, null, null, fact);
			}
			LogFact(hero, GetHeroName(hero) + "已卸任" + GetSettlementName(origin) + "总督并从驻军抽调" + totalToMove + "名健康士兵，临时远征队开始执行命令。");
			message = GetHeroName(hero) + "已从" + GetSettlementName(origin) + "驻军抽调" + totalToMove + "人并开始远征。";
			return true;
		}
		catch (Exception ex)
		{
			message = "组建总督远征队失败：" + ex.Message;
			Log("create governor expedition failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			RollbackGovernorExpeditionCreation(hero, origin, garrison, garrisonSnapshot, createdParty, transferRecords, "create_failed");
			return false;
		}
	}

	private static void PurgeGeneratedGovernorPartyContents(MobileParty party, Hero leader)
	{
		if (!IsPartyUsable(party) || party.MemberRoster == null)
		{
			throw new InvalidOperationException("新领主队名册不可用。");
		}
		for (int i = party.MemberRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = party.MemberRoster.GetElementCopyAtIndex(i);
			if (element.Character == null || element.Number <= 0)
			{
				continue;
			}
			if (element.Character.IsHero)
			{
				if (element.Character.HeroObject != leader)
				{
					throw new InvalidOperationException("新领主队出现了意外 Hero，已停止建队。");
				}
				continue;
			}
			party.MemberRoster.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp, true, -1);
		}
		// CreateNewClanMobileParty adds vanilla starter provisions in both supported APIs.
		// Keep them for the expedition; return/merge already transfers the full item roster.
		PurgeGeneratedGovernorPartyShips(party);
		if (party.MemberRoster.TotalRegulars != 0
			|| (party.PrisonRoster?.TotalManCount ?? 0) != 0
			|| HasPartyShips(party))
		{
			throw new InvalidOperationException("原版模板兵、俘虏或模板船只未能完全清除。");
		}
	}

	private static void PurgeGeneratedGovernorPartyShips(MobileParty party)
	{
		if (party?.Ships == null || party.Ships.Count == 0)
		{
			return;
		}
		foreach (var ship in party.Ships.Where(x => x != null).ToList())
		{
			DestroyShipAction.ApplyByDiscard(ship);
		}
		if (HasPartyShips(party))
		{
			throw new InvalidOperationException("原版领主队模板船只清理失败。");
		}
	}

	private static RosterElementState GetRosterElementState(TroopRoster roster, CharacterObject character)
	{
		RosterElementState state = new RosterElementState
		{
			Character = character,
			Number = 0,
			WoundedNumber = 0,
			Xp = 0
		};
		if (roster == null || character == null)
		{
			return state;
		}
		int index = roster.FindIndexOfTroop(character);
		if (index < 0)
		{
			return state;
		}
		TroopRosterElement element = roster.GetElementCopyAtIndex(index);
		if (element.Character == null)
		{
			return state;
		}
		state.Character = element.Character;
		state.Number = element.Number;
		state.WoundedNumber = element.WoundedNumber;
		state.Xp = element.Xp;
		return state;
	}

	private static void TransferGovernorTroopsWithVerification(MobileParty garrison, MobileParty expedition, List<GovernorTroopTransferPlan> plans, List<RosterElementTransferRecord> transferRecords)
	{
		if (!IsPartyUsable(garrison) || !IsPartyUsable(expedition) || garrison.MemberRoster == null || expedition.MemberRoster == null || plans == null)
		{
			throw new InvalidOperationException("驻军、远征队或抽调计划不可用。");
		}
		foreach (GovernorTroopTransferPlan plan in plans)
		{
			if (plan?.Character == null || plan.Character.IsHero || plan.Count <= 0)
			{
				throw new InvalidOperationException("驻军抽调计划包含无效兵种。");
			}
			RosterElementState sourceBefore = GetRosterElementState(garrison.MemberRoster, plan.Character);
			RosterElementState targetBefore = GetRosterElementState(expedition.MemberRoster, plan.Character);
			if (sourceBefore.Number - sourceBefore.WoundedNumber < plan.Count || sourceBefore.Xp < plan.Xp)
			{
				throw new InvalidOperationException("驻军兵种状态在转移前已经变化：" + (plan.Character.StringId ?? "unknown"));
			}
			RosterElementTransferRecord transfer = new RosterElementTransferRecord
			{
				Source = garrison.MemberRoster,
				Target = expedition.MemberRoster,
				Character = plan.Character,
				SourceBefore = sourceBefore,
				TargetBefore = targetBefore,
				IsPrisonerRoster = false
			};
			transferRecords?.Add(transfer);
			string step = "remove_source_counts";
			try
			{
				// Count and XP must be changed separately. Bannerlord clamps member XP
				// against the already-reduced source count inside PartyBase.OnXpChanged.
				// Reading that actual reduction is the only way to conserve saturated XP.
				garrison.MemberRoster.AddToCounts(plan.Character, -plan.Count, false, 0, 0, true, -1);
				RosterElementState sourceAfterCounts = GetRosterElementState(garrison.MemberRoster, plan.Character);
				if (sourceAfterCounts.Number != sourceBefore.Number - plan.Count
					|| sourceAfterCounts.WoundedNumber != sourceBefore.WoundedNumber)
				{
					throw new InvalidOperationException("驻军健康兵员扣除校验失败：" + (plan.Character.StringId ?? "unknown"));
				}
				step = "remove_source_xp";
				RosterElementState sourceAfter = sourceAfterCounts;
				if (sourceAfterCounts.Number > 0 && plan.Xp > 0)
				{
					sourceAfter = ApplyRosterXpDeltaObserved(garrison.MemberRoster, plan.Character, -plan.Xp, allowDownwardClamp: true, context: "governor_source");
				}
				int actualXpRemoved = sourceBefore.Xp - sourceAfter.Xp;
				if (actualXpRemoved < plan.Xp || actualXpRemoved < 0)
				{
					throw new InvalidOperationException("驻军 XP 扣除结果异常：" + (plan.Character.StringId ?? "unknown"));
				}
				if (actualXpRemoved != plan.Xp)
				{
					Log("governor troop xp adjusted by native cap troop=" + (plan.Character.StringId ?? "unknown") + " plannedXp=" + plan.Xp + " actualXp=" + actualXpRemoved);
				}
				step = "add_target_counts";
				expedition.MemberRoster.AddToCounts(plan.Character, plan.Count, false, 0, 0, true, -1);
				RosterElementState targetAfterCounts = GetRosterElementState(expedition.MemberRoster, plan.Character);
				if (targetAfterCounts.Number != targetBefore.Number + plan.Count
					|| targetAfterCounts.WoundedNumber != targetBefore.WoundedNumber
					|| targetAfterCounts.Xp != targetBefore.Xp)
				{
					throw new InvalidOperationException("远征队兵员写入校验失败：" + (plan.Character.StringId ?? "unknown"));
				}
				step = "add_target_xp";
				RosterElementState targetAfter = actualXpRemoved == 0
					? targetAfterCounts
					: ApplyRosterXpDeltaObserved(expedition.MemberRoster, plan.Character, actualXpRemoved, allowDownwardClamp: false, context: "governor_target");
				if (sourceAfter.Number + targetAfter.Number != sourceBefore.Number + targetBefore.Number
					|| sourceAfter.WoundedNumber + targetAfter.WoundedNumber != sourceBefore.WoundedNumber + targetBefore.WoundedNumber
					|| sourceAfter.Xp + targetAfter.Xp != sourceBefore.Xp + targetBefore.Xp)
				{
					throw new InvalidOperationException("驻军与远征队兵员或 XP 守恒校验失败：" + (plan.Character.StringId ?? "unknown"));
				}
			}
			catch (Exception ex)
			{
				try
				{
					RestoreGovernorRosterElementExact(expedition.MemberRoster, plan.Character, targetBefore);
					RestoreGovernorRosterElementExact(garrison.MemberRoster, plan.Character, sourceBefore);
				}
				catch (Exception rollbackEx)
				{
					Log("governor troop immediate rollback failed troop=" + (plan.Character.StringId ?? "unknown") + " step=" + step + " error=" + rollbackEx);
				}
				throw new InvalidOperationException("总督远征队兵员转移失败 troop=" + (plan.Character.StringId ?? "unknown") + " step=" + step, ex);
			}
		}
	}

	private static RosterElementState ApplyRosterXpDeltaObserved(TroopRoster roster, CharacterObject character, int xpDelta, bool allowDownwardClamp, string context)
	{
		RosterElementState before = GetRosterElementState(roster, character);
		long expectedLong = (long)before.Xp + xpDelta;
		if (roster == null || character == null || before.Number <= 0 || expectedLong < 0 || expectedLong > int.MaxValue)
		{
			throw new InvalidOperationException("兵种 XP 调整参数无效：" + (character?.StringId ?? "unknown"));
		}
		int expected = (int)expectedLong;
		Exception callbackError = null;
		try
		{
			roster.AddXpToTroop(character, xpDelta);
		}
		catch (Exception ex)
		{
			// SetElementXp writes first and then invokes the owner callback. A custom
			// troop may throw from that callback even though the requested XP stuck.
			callbackError = ex;
		}
		RosterElementState after = GetRosterElementState(roster, character);
		bool accepted = after.Xp == expected
			|| (allowDownwardClamp && after.Xp >= 0 && after.Xp < expected);
		if (!accepted)
		{
			throw new InvalidOperationException("兵种 XP 写入校验失败：" + (character.StringId ?? "unknown") + " context=" + (context ?? ""), callbackError);
		}
		if (callbackError != null)
		{
			Log("roster xp callback threw after accepted write troop=" + (character.StringId ?? "unknown") + " context=" + (context ?? "") + " error=" + callbackError.Message);
		}
		return after;
	}

	private static void RestoreGovernorRosterElementExact(TroopRoster roster, CharacterObject character, RosterElementState snapshot)
	{
		if (roster == null || character == null)
		{
			throw new InvalidOperationException("总督远征名册回滚缺少兵种或名册。");
		}
		RosterElementState current = GetRosterElementState(roster, character);
		int countDelta = snapshot.Number - current.Number;
		int woundedDelta = snapshot.WoundedNumber - current.WoundedNumber;
		if (countDelta != 0 || woundedDelta != 0)
		{
			roster.AddToCounts(character, countDelta, false, woundedDelta, 0, true, -1);
		}
		RosterElementState afterCounts = GetRosterElementState(roster, character);
		int xpDelta = snapshot.Xp - afterCounts.Xp;
		RosterElementState restored = xpDelta == 0
			? afterCounts
			: ApplyRosterXpDeltaObserved(roster, character, xpDelta, allowDownwardClamp: false, context: "governor_rollback");
		if (restored.Number != snapshot.Number || restored.WoundedNumber != snapshot.WoundedNumber || restored.Xp != snapshot.Xp)
		{
			throw new InvalidOperationException("总督远征名册精确回滚失败：" + (character.StringId ?? "unknown"));
		}
	}

	private void RollbackGovernorExpeditionCreation(Hero hero, Settlement origin, MobileParty garrison, List<TroopRosterElement> garrisonSnapshot, MobileParty createdParty, List<RosterElementTransferRecord> transferRecords, string reason)
	{
		RemoveGovernorExpeditionRecord(hero?.StringId, reason, removeQueue: true);
		createdParty = IsPartyUsable(createdParty) ? createdParty : hero?.PartyBelongedTo;
		bool garrisonRestored = TryRestoreGovernorGarrisonFromTransferRecords(garrison, garrisonSnapshot, transferRecords, out string restoreError);
		if (!garrisonRestored)
		{
			Log("restore garrison transfer failed hero=" + (hero?.StringId ?? "") + " error=" + restoreError);
			if (hero != null && origin != null && IsPartyUsable(createdParty) && createdParty.LeaderHero == hero && hero.PartyBelongedTo == createdParty)
			{
				try
				{
					RegisterGovernorExpedition(hero, createdParty, origin, hero.Clan);
					BeginGovernorExpeditionReturn(hero, createdParty, "creation_rollback_recovery");
					return;
				}
				catch (Exception ex)
				{
					Log("register governor rollback recovery failed hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
				}
			}
			ReleasePartyAi(createdParty);
			return;
		}
		try
		{
			if (IsPartyUsable(createdParty) && createdParty != MobileParty.MainParty)
			{
				AbortCurrentCommandIfNeeded(createdParty, null);
				ReleasePartyAi(createdParty);
				createdParty.ItemRoster?.Clear();
				createdParty.PrisonRoster?.Clear();
				PurgeGeneratedGovernorPartyShips(createdParty);
				for (int i = createdParty.MemberRoster.Count - 1; i >= 0; i--)
				{
					TroopRosterElement element = createdParty.MemberRoster.GetElementCopyAtIndex(i);
					if (element.Character != null && !element.Character.IsHero && element.Number > 0)
					{
						createdParty.MemberRoster.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp, true, -1);
					}
				}
				foreach (Hero partyHero in GetMemberHeroesSnapshot(createdParty))
				{
					if (CanSafelyPlaceHeroInSettlement(partyHero, createdParty, origin))
					{
						TeleportHeroAction.ApplyImmediateTeleportToSettlement(partyHero, origin);
					}
				}
				if (!TryDestroyStrictlyEmptyParty(createdParty, "governor_create_rollback") && IsPartyUsable(createdParty))
				{
					RegisterGovernorExpedition(hero, createdParty, origin, hero?.Clan);
					if (TryGetGovernorExpeditionForHero(hero?.StringId, out GovernorExpeditionRecord cleanupRecord))
					{
						RestoreGovernorExpeditionRecordForCleanupRetry(cleanupRecord, origin, createdParty);
					}
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Log("rollback governor party failed hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			return;
		}
		TryRestoreGovernorAfterRollback(hero, origin);
	}

	private static bool TryRestoreGovernorGarrisonFromTransferRecords(MobileParty garrison, List<TroopRosterElement> originalSnapshot, List<RosterElementTransferRecord> transferRecords, out string error)
	{
		error = "";
		try
		{
			if (!IsPartyUsable(garrison) || garrison.MemberRoster == null || originalSnapshot == null)
			{
				error = "驻军或原始名册快照不可用。";
				return false;
			}
			for (int i = (transferRecords?.Count ?? 0) - 1; i >= 0; i--)
			{
				RosterElementTransferRecord transfer = transferRecords[i];
				if (transfer?.Character == null || transfer.Source != garrison.MemberRoster || transfer.Target == null)
				{
					throw new InvalidOperationException("驻军回滚缺少有效事务快照。");
				}
				RestoreGovernorRosterElementExact(transfer.Target, transfer.Character, transfer.TargetBefore);
				RestoreGovernorRosterElementExact(transfer.Source, transfer.Character, transfer.SourceBefore);
				RosterElementState sourceAfter = GetRosterElementState(transfer.Source, transfer.Character);
				RosterElementState targetAfter = GetRosterElementState(transfer.Target, transfer.Character);
				if (sourceAfter.Number != transfer.SourceBefore.Number
					|| sourceAfter.WoundedNumber != transfer.SourceBefore.WoundedNumber
					|| sourceAfter.Xp != transfer.SourceBefore.Xp
					|| targetAfter.Number != transfer.TargetBefore.Number
					|| targetAfter.WoundedNumber != transfer.TargetBefore.WoundedNumber
					|| targetAfter.Xp != transfer.TargetBefore.Xp)
				{
					throw new InvalidOperationException("驻军事务快照回滚校验失败：" + (transfer.Character.StringId ?? "unknown"));
				}
			}
			if (!RosterMatchesSnapshot(garrison.MemberRoster, originalSnapshot))
			{
				throw new InvalidOperationException("驻军完整名册与建队前快照不一致。");
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static bool RosterMatchesSnapshot(TroopRoster roster, List<TroopRosterElement> snapshot)
	{
		if (roster == null || snapshot == null)
		{
			return false;
		}
		List<TroopRosterElement> expected = snapshot.Where(x => x.Character != null && x.Number > 0).ToList();
		List<TroopRosterElement> actual = roster.GetTroopRoster().Where(x => x.Character != null && x.Number > 0).ToList();
		if (expected.Count != actual.Count)
		{
			return false;
		}
		return expected.All(element =>
		{
			RosterElementState current = GetRosterElementState(roster, element.Character);
			return current.Number == element.Number && current.WoundedNumber == element.WoundedNumber && current.Xp == element.Xp;
		});
	}

	private static void TryRestoreGovernorAfterRollback(Hero hero, Settlement origin)
	{
		try
		{
			if (hero == null || origin?.Town == null || hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner)
			{
				return;
			}
			if (origin.OwnerClan != hero.Clan || origin.Town.Governor != null || hero.GovernorOf != null || hero.PartyBelongedTo != null || hero.CurrentSettlement != origin)
			{
				return;
			}
			ChangeGovernorAction.Apply(origin.Town, hero);
		}
		catch (Exception ex)
		{
			Log("restore governor after rollback failed hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private bool TryReactivateGovernorExpedition(Hero hero, MobileParty party)
	{
		if (hero == null || party == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return false;
		}
		lock (_queueLock)
		{
			if (!_governorExpeditions.TryGetValue(hero.StringId, out GovernorExpeditionRecord record)
				|| record == null
				|| !GovernorRecordMatchesParty(record, party))
			{
				return false;
			}
			record.Phase = GovernorExpeditionPhaseActive;
			record.ReturnTargetSettlementId = "";
			record.LastIssuedActionKey = "";
			_governorReturnTargetByHeroId.Remove(hero.StringId);
			return true;
		}
	}

	private bool BeginGovernorExpeditionReturn(Hero hero, MobileParty party, string reason)
	{
		string heroId = hero?.StringId ?? party?.LeaderHero?.StringId ?? "";
		if (!TryGetGovernorExpeditionForHero(heroId, out GovernorExpeditionRecord record))
		{
			return false;
		}
		if (hero == null)
		{
			hero = ResolveHeroByIdAny(record.HeroId);
		}
		if (party == null && hero != null)
		{
			party = hero.PartyBelongedTo;
		}
		if (string.Equals(record.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase))
		{
			party = party ?? ResolveGovernorExpeditionParty(record);
			ProcessGovernorExpeditionReturnTick(hero, party, record);
			return true;
		}
		if (hero == null || hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner
			|| !IsPartyUsable(party) || !GovernorRecordMatchesParty(record, party) || party.LeaderHero != hero || hero.PartyBelongedTo != party)
		{
			RemoveGovernorExpeditionRecord(record.HeroId, "return_actor_invalid:" + (reason ?? ""), removeQueue: true);
			ReleasePartyAi(party);
			return true;
		}
		Settlement target = SelectGovernorReturnTarget(hero, party, record);
		if (target == null)
		{
			AbandonGovernorExpeditionToNativeAi(hero, party, record, "no_safe_family_settlement");
			return true;
		}
		lock (_queueLock)
		{
			record.Phase = GovernorExpeditionPhaseReturning;
			record.ReturnTargetSettlementId = target.StringId;
			record.LastIssuedActionKey = "";
			_governorReturnTargetByHeroId[record.HeroId] = target;
		}
		LeaveArmyIfNeeded(party);
		TryIssueGovernorReturnOrder(party, target, record);
		Log("governor expedition returning hero=" + record.HeroId + " target=" + target.StringId + " reason=" + (reason ?? ""));
		if (party.CurrentSettlement == target)
		{
			ProcessGovernorExpeditionReturnTick(hero, party, record);
		}
		return true;
	}

	private Settlement SelectGovernorReturnTarget(Hero hero, MobileParty party, GovernorExpeditionRecord record)
	{
		Clan clan = hero?.Clan;
		if (clan == null || record == null)
		{
			return null;
		}
		Settlement origin = null;
		try
		{
			origin = clan.Settlements?.FirstOrDefault(x => x != null
				&& (x.IsTown || x.IsCastle)
				&& x.OwnerClan == clan
				&& string.Equals(x.StringId, record.OriginSettlementId, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			origin = null;
		}
		if (origin != null)
		{
			return origin;
		}
		Settlement best = null;
		float bestDistance = float.MaxValue;
		try
		{
			if (clan.Settlements == null)
			{
				return null;
			}
			foreach (Settlement settlement in clan.Settlements)
			{
				if (settlement == null || (!settlement.IsTown && !settlement.IsCastle) || settlement.OwnerClan != clan
					|| settlement.IsUnderSiege || HasActiveSettlementMapEvent(settlement))
				{
					continue;
				}
				float distance = party == null ? 0f : party.Position.Distance(settlement.GatePosition);
				if (best == null || distance < bestDistance)
				{
					best = settlement;
					bestDistance = distance;
				}
			}
		}
		catch
		{
			return best;
		}
		return best;
	}

	private static bool TryIssueGovernorReturnOrder(MobileParty party, Settlement target, GovernorExpeditionRecord record)
	{
		if (!IsPartyUsable(party) || target == null || record == null || HasActiveMapEvent(party) || party.BesiegerCamp != null)
		{
			return false;
		}
		if (party.CurrentSettlement == target)
		{
			record.LastIssuedActionKey = "arrived:" + target.StringId;
			return true;
		}
		try
		{
			LockPartyAi(party);
			SetPartyAiAction.GetActionForVisitingSettlement(party, target, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			record.LastIssuedActionKey = "return:" + target.StringId;
			return true;
		}
		catch (Exception ex)
		{
			Log("issue governor return failed party=" + (party.StringId ?? "") + " target=" + (target.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private void ProcessGovernorExpeditionReturnTick(Hero hero, MobileParty party, GovernorExpeditionRecord record)
	{
		if (record == null
			|| (!string.Equals(record.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(record.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		if (!IsPartyUsable(party) || !GovernorRecordMatchesParty(record, party))
		{
			RemoveGovernorExpeditionRecord(record.HeroId, "return_party_missing", removeQueue: true);
			return;
		}
		if (hero == null)
		{
			hero = ResolveHeroByIdAny(record.HeroId);
		}
		bool cleanupPhase = string.Equals(record.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase);
		Settlement target = GetCachedGovernorReturnTarget(record.HeroId);
		if (cleanupPhase)
		{
			if (target == null || !string.Equals(target.StringId, record.ReturnTargetSettlementId, StringComparison.OrdinalIgnoreCase))
			{
				target = ResolveSettlementById(record.ReturnTargetSettlementId);
			}
			if (target != null && (target.IsTown || target.IsCastle))
			{
				lock (_queueLock)
				{
					_governorReturnTargetByHeroId[record.HeroId] = target;
				}
			}
			else
			{
				target = null;
			}
		}
		else if (target == null
			|| !string.Equals(target.StringId, record.ReturnTargetSettlementId, StringComparison.OrdinalIgnoreCase)
			|| target.OwnerClan != hero?.Clan
			|| (!target.IsTown && !target.IsCastle))
		{
			target = SelectGovernorReturnTarget(hero, party, record);
			if (target == null)
			{
				AbandonGovernorExpeditionToNativeAi(hero, party, record, "return_target_lost");
				return;
			}
			lock (_queueLock)
			{
				record.ReturnTargetSettlementId = target.StringId;
				record.LastIssuedActionKey = "";
				_governorReturnTargetByHeroId[record.HeroId] = target;
			}
		}
		bool emptyCleanupRetry = cleanupPhase
			&& (party.MemberRoster?.TotalManCount ?? 0) == 0
			&& (party.PrisonRoster?.TotalManCount ?? 0) == 0
			&& (party.ItemRoster?.Count ?? 0) == 0;
		if (!emptyCleanupRetry
			&& (hero == null || hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner || party.LeaderHero != hero || hero.PartyBelongedTo != party))
		{
			RemoveGovernorExpeditionRecord(record.HeroId, "return_hero_invalid", removeQueue: true);
			ReleasePartyAi(party);
			return;
		}
		if (!emptyCleanupRetry && party.CurrentSettlement != target)
		{
			bool shouldRefresh = !string.Equals(record.LastIssuedActionKey, "return:" + target.StringId, StringComparison.OrdinalIgnoreCase)
				|| !IsPartyVisitingSettlement(party, target)
				|| !IsAiDecisionLockActive(party);
			if (shouldRefresh)
			{
				LeaveArmyIfNeeded(party);
				TryIssueGovernorReturnOrder(party, target, record);
			}
			return;
		}
		if (HasActiveMapEvent(party) || party.BesiegerCamp != null
			|| (target != null && (target.IsUnderSiege || HasActiveSettlementMapEvent(target))))
		{
			return;
		}
		LeaveArmyIfNeeded(party);
		if (party.Army != null || party.AttachedTo != null)
		{
			return;
		}
		TryFinalizeGovernorExpeditionReturn(hero, party, target, record, emptyCleanupRetry);
	}

	private Settlement GetCachedGovernorReturnTarget(string heroId)
	{
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return null;
		}
		lock (_queueLock)
		{
			_governorReturnTargetByHeroId.TryGetValue(heroId, out Settlement target);
			return target;
		}
	}

	private void AbandonGovernorExpeditionToNativeAi(Hero hero, MobileParty party, GovernorExpeditionRecord record, string reason)
	{
		if (record == null)
		{
			return;
		}
		RemoveGovernorExpeditionRecord(record.HeroId, "abandon:" + reason, removeQueue: true);
		ReleasePartyAi(party);
		if (hero != null && !hero.IsDead)
		{
			string detail = string.Equals(reason, "no_safe_family_settlement", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(reason, "return_target_lost", StringComparison.OrdinalIgnoreCase)
				? "没有可安全返还的本家族城市或城堡"
				: "当前无法安全完成返城资产安置";
			LogFact(hero, GetHeroName(hero) + "的总督远征队" + detail + "，已保留当前部队并交还原版 AI 管理。");
		}
	}

	private void ProcessPendingGovernorExpeditionReconcile()
	{
		if (Interlocked.Exchange(ref _hasPendingGovernorExpeditionReconcile, 0) == 0)
		{
			return;
		}
		List<GovernorExpeditionRecord> records;
		lock (_queueLock)
		{
			records = _governorExpeditions.Values.Where(x => x != null).ToList();
			_governorExpeditionHeroByPartyKey.Clear();
			_governorExpeditionPartyByHeroId.Clear();
			foreach (GovernorExpeditionRecord record in records)
			{
				IndexGovernorExpeditionRecordUnsafe(record);
			}
		}
		Dictionary<string, MobileParty> partiesById = new Dictionary<string, MobileParty>(StringComparer.OrdinalIgnoreCase);
		Dictionary<int, MobileParty> partiesByIndex = new Dictionary<int, MobileParty>();
		try
		{
			if (MobileParty.All == null)
			{
				return;
			}
			foreach (MobileParty candidate in MobileParty.All)
			{
				if (candidate == null)
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(candidate.StringId))
				{
					partiesById[candidate.StringId] = candidate;
				}
				int index = GetPartyIndexSafe(candidate);
				if (index >= 0)
				{
					partiesByIndex[index] = candidate;
				}
			}
		}
		catch (Exception ex)
		{
			Log("build governor expedition load party index failed: " + ex.Message);
		}
		foreach (GovernorExpeditionRecord record in records)
		{
			Hero hero = ResolveHeroByIdAny(record.HeroId);
			MobileParty party = null;
			if (!string.IsNullOrWhiteSpace(record.PartyStringId))
			{
				partiesById.TryGetValue(record.PartyStringId, out party);
			}
			if (party == null && record.PartyIndex >= 0)
			{
				partiesByIndex.TryGetValue(record.PartyIndex, out party);
			}
			bool cleanupPhase = string.Equals(record.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase);
			Settlement cleanupTarget = cleanupPhase
				? ResolveSettlementById(record.ReturnTargetSettlementId)
				: null;
			bool validCleanupRetry = cleanupPhase
				&& (party?.MemberRoster?.TotalManCount ?? 0) == 0
				&& (party?.PrisonRoster?.TotalManCount ?? 0) == 0
				&& (party?.ItemRoster?.Count ?? 0) == 0
				&& !HasPartyShips(party);
			bool validActiveActor = party?.LeaderHero == hero && hero?.PartyBelongedTo == party;
			if (!IsPartyUsable(party) || !GovernorRecordMatchesParty(record, party)
				|| (!validCleanupRetry && (hero == null || hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner || !validActiveActor)))
			{
				RemoveGovernorExpeditionRecord(record.HeroId, "load_record_invalid", removeQueue: true);
				continue;
			}
			lock (_queueLock)
			{
				UnindexGovernorExpeditionRecordUnsafe(record);
				record.PartyStringId = party.StringId;
				record.PartyIndex = GetPartyIndexSafe(party);
				IndexGovernorExpeditionRecordUnsafe(record);
				_governorExpeditionPartyByHeroId[record.HeroId] = party;
				if (validCleanupRetry && cleanupTarget != null && (cleanupTarget.IsTown || cleanupTarget.IsCastle))
				{
					_governorReturnTargetByHeroId[record.HeroId] = cleanupTarget;
				}
			}
			if (string.Equals(record.Phase, GovernorExpeditionPhaseReturning, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.Phase, GovernorExpeditionPhaseCleanup, StringComparison.OrdinalIgnoreCase))
			{
				lock (_queueLock)
				{
					_queues.Remove(record.HeroId);
				}
				ProcessGovernorExpeditionReturnTick(hero, party, record);
				continue;
			}
			bool hasQueue;
			lock (_queueLock)
			{
				hasQueue = _queues.ContainsKey(record.HeroId);
			}
			if (!hasQueue)
			{
				BeginGovernorExpeditionReturn(hero, party, "load_active_without_queue");
			}
		}
	}

	private void TryFinalizeGovernorExpeditionReturn(Hero hero, MobileParty party, Settlement target, GovernorExpeditionRecord record, bool emptyCleanupRetry)
	{
		try
		{
			int returnedShipCount = 0;
			if (!emptyCleanupRetry)
			{
				List<Hero> memberHeroes = GetMemberHeroesSnapshot(party);
				if (!memberHeroes.Contains(hero))
				{
					throw new InvalidOperationException("远征队领队 Hero 已不在成员名册中。");
				}
				if (memberHeroes.Any(memberHero => !CanSafelyPlaceHeroInSettlement(memberHero, party, target)))
				{
					return;
				}
				if (target.Town.GarrisonParty == null)
				{
					target.AddGarrisonParty();
				}
				MobileParty garrison = target.Town.GarrisonParty;
				if (!IsPartyUsable(garrison) || garrison.CurrentSettlement != target)
				{
					AbandonGovernorExpeditionToNativeAi(hero, party, record, "return_garrison_unavailable");
					return;
				}
				List<Ship> shipSnapshot = GetPartyShipsSnapshot(party);
				TransferShipsVerified(party.Party, target.Party, shipSnapshot);
				returnedShipCount = shipSnapshot.Count;
				MoveAllRegularRosterElementsVerified(party.MemberRoster, garrison.MemberRoster);
				MoveAllPrisonersToSettlementVerified(party, target);
				MoveAllItemsVerified(party.ItemRoster, garrison.ItemRoster);
				foreach (Hero memberHero in memberHeroes.Where(x => x != hero))
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(memberHero, target);
					if (memberHero.PartyBelongedTo == party || memberHero.CurrentSettlement != target)
					{
						return;
					}
				}
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, target);
				if (hero.PartyBelongedTo == party || hero.CurrentSettlement != target)
				{
					return;
				}
			}
			if ((party.MemberRoster?.TotalManCount ?? 0) != 0
				|| (party.PrisonRoster?.TotalManCount ?? 0) != 0
				|| (party.ItemRoster?.Count ?? 0) != 0
				|| HasPartyShips(party))
			{
				return;
			}
			record.Phase = GovernorExpeditionPhaseCleanup;
			if (target != null)
			{
				record.ReturnTargetSettlementId = target.StringId;
			}
			RemoveGovernorExpeditionRecord(record.HeroId, "return_cleanup", removeQueue: true);
			if (!TryDestroyStrictlyEmptyParty(party, "governor_return") && IsPartyUsable(party))
			{
				RestoreGovernorExpeditionRecordForCleanupRetry(record, target, party);
				return;
			}
			bool restored = TryRestoreGovernorAfterReturn(hero, target, record);
			LogFact(hero, GetHeroName(hero) + "的远征队已返抵" + GetSettlementName(target) + "，普通兵、俘虏与物资已经完成安置，临时部队已回收。"
				+ (returnedShipCount > 0 ? (returnedShipCount + "艘随队船只已转交该定居点。") : "")
				+ (restored ? "该人物已恢复原总督职务。" : "未驱逐现任总督。"));
		}
		catch (Exception ex)
		{
			Log("finalize governor return failed hero=" + (record?.HeroId ?? "") + " error=" + ex);
		}
	}

	private void RestoreGovernorExpeditionRecordForCleanupRetry(GovernorExpeditionRecord record, Settlement target, MobileParty party)
	{
		if (record == null || !NormalizeGovernorExpeditionRecord(record))
		{
			return;
		}
		record.Phase = GovernorExpeditionPhaseCleanup;
		record.ReturnTargetSettlementId = target?.StringId ?? record.ReturnTargetSettlementId;
		lock (_queueLock)
		{
			_governorExpeditions[record.HeroId] = record;
			IndexGovernorExpeditionRecordUnsafe(record);
			if (party != null)
			{
				_governorExpeditionPartyByHeroId[record.HeroId] = party;
			}
			Volatile.Write(ref _hasGovernorExpeditions, 1);
			if (target != null)
			{
				_governorReturnTargetByHeroId[record.HeroId] = target;
			}
		}
	}

	private static bool TryRestoreGovernorAfterReturn(Hero hero, Settlement target, GovernorExpeditionRecord record)
	{
		try
		{
			if (hero == null || target?.Town == null || record == null || hero.IsDead || !hero.IsActive || hero.IsDisabled || hero.IsPrisoner)
			{
				return false;
			}
			if (!string.Equals(target.StringId, record.OriginSettlementId, StringComparison.OrdinalIgnoreCase)
				|| target.OwnerClan != hero.Clan
				|| target.Town.Governor != null
				|| hero.GovernorOf != null
				|| hero.PartyBelongedTo != null
				|| hero.CurrentSettlement != target)
			{
				return false;
			}
			ChangeGovernorAction.Apply(target.Town, hero);
			return target.Town.Governor == hero && hero.GovernorOf == target.Town && hero.PartyBelongedTo == null && hero.CurrentSettlement == target;
		}
		catch (Exception ex)
		{
			Log("restore governor after return failed hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static List<Hero> GetMemberHeroesSnapshot(MobileParty party)
	{
		if (party?.MemberRoster == null)
		{
			return new List<Hero>();
		}
		return party.MemberRoster.GetTroopRoster()
			.Where(x => x.Character?.IsHero == true && x.Character.HeroObject != null && x.Number > 0)
			.Select(x => x.Character.HeroObject)
			.Distinct()
			.ToList();
	}

	private static List<MergeHeroIdentitySnapshot> CaptureMergeHeroIdentities(IEnumerable<Hero> heroes)
	{
		return (heroes ?? Enumerable.Empty<Hero>())
			.Where(hero => hero != null)
			.Distinct()
			.Select(hero => new MergeHeroIdentitySnapshot
			{
				Hero = hero,
				Clan = hero.Clan,
				CompanionOf = hero.CompanionOf,
				Occupation = hero.Occupation
			})
			.ToList();
	}

	private static void ValidateMergeHeroIdentitiesUnchanged(IEnumerable<MergeHeroIdentitySnapshot> identities)
	{
		foreach (MergeHeroIdentitySnapshot identity in identities ?? Enumerable.Empty<MergeHeroIdentitySnapshot>())
		{
			Hero memberHero = identity?.Hero;
			if (memberHero == null || memberHero.Clan != identity.Clan
				|| memberHero.CompanionOf != identity.CompanionOf || memberHero.Occupation != identity.Occupation)
			{
				throw new InvalidOperationException("并队改变了随队 Hero 的家族、职业或同伴身份：" + (memberHero?.StringId ?? "unknown"));
			}
		}
	}

	private static bool IsForeignClanGuestHero(Hero hero)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			return hero != null && hero != Hero.MainHero && hero.Clan != null && playerClan != null && hero.Clan != playerClan;
		}
		catch
		{
			return false;
		}
	}

	private bool IsForeignClanGuestPlacementValid(Hero guest)
	{
		if (IsHeroActuallyInPlayerMainPartyRoster(guest))
		{
			return true;
		}
		MobileParty party = guest?.PartyBelongedTo;
		return IsRegisteredPlayerDetachment(guest, party);
	}

	private void RegisterForeignClanGuests(IEnumerable<MergeHeroIdentitySnapshot> identities)
	{
		try
		{
			int added = 0;
			double joinedDay = NowDay();
			lock (_queueLock)
			{
				foreach (MergeHeroIdentitySnapshot identity in identities ?? Enumerable.Empty<MergeHeroIdentitySnapshot>())
				{
					Hero guest = identity?.Hero;
					Clan clan = identity?.Clan;
					if (!IsForeignClanGuestHero(guest) || clan == null || string.IsNullOrWhiteSpace(guest.StringId)
						|| string.IsNullOrWhiteSpace(clan.StringId) || guest.Clan != clan
						|| !IsHeroActuallyInPlayerMainPartyRoster(guest))
					{
						continue;
					}
					_foreignClanGuests[guest.StringId] = new ForeignClanGuestRecord
					{
						HeroId = guest.StringId,
						ClanId = clan.StringId,
						JoinedDay = joinedDay
					};
					added++;
				}
				Volatile.Write(ref _hasForeignClanGuests, _foreignClanGuests.Count > 0 ? 1 : 0);
			}
			if (added > 0)
			{
				Log("registered foreign clan guests count=" + added);
			}
		}
		catch (Exception ex)
		{
			// Asset transfer and source-party destruction have already committed at
			// this point. Guest bookkeeping must never trigger a rollback attempt
			// against the destroyed source party.
			Log("register foreign clan guests failed: " + ex);
		}
	}

	private static bool CanSafelyPlaceHeroInSettlement(Hero hero, MobileParty sourceParty, Settlement target)
	{
		return hero != null
			&& target != null
			&& !hero.IsDead
			&& hero.IsActive
			&& !hero.IsDisabled
			&& !hero.IsPrisoner
			&& hero.PartyBelongedTo == sourceParty
			&& IsPartyUsable(sourceParty)
			&& !HasActiveMapEvent(sourceParty)
			&& sourceParty.BesiegerCamp == null;
	}

	private static void MoveAllRegularRosterElementsVerified(TroopRoster source, TroopRoster target, List<RosterElementTransferRecord> transferRecords = null)
	{
		List<TroopRosterElement> snapshot = source?.GetTroopRoster()
			.Where(x => x.Character != null && !x.Character.IsHero && x.Number > 0)
			.ToList() ?? new List<TroopRosterElement>();
		foreach (TroopRosterElement element in snapshot)
		{
			MoveRosterElementVerified(source, target, element, transferRecords, isPrisonerRoster: false);
		}
	}

	private static void MoveRosterElementVerified(TroopRoster source, TroopRoster target, TroopRosterElement element, List<RosterElementTransferRecord> transferRecords, bool isPrisonerRoster)
	{
		if (source == null || target == null || element.Character == null || element.Number <= 0)
		{
			return;
		}
		RosterElementState sourceBefore = GetRosterElementState(source, element.Character);
		RosterElementState targetBefore = GetRosterElementState(target, element.Character);
		RosterElementTransferRecord transfer = new RosterElementTransferRecord
		{
			Source = source,
			Target = target,
			Character = element.Character,
			SourceBefore = sourceBefore,
			TargetBefore = targetBefore,
			IsPrisonerRoster = isPrisonerRoster
		};
		transferRecords?.Add(transfer);
		string step = "remove_source_counts";
		try
		{
			// Do not pass XP through AddToCounts. Bannerlord dereferences
			// CharacterObject.UpgradeTargets while applying member XP; some custom
			// troops legitimately expose a null upgrade array and crash that path.
			source.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, 0, true, -1);
			step = "add_target_counts";
			target.AddToCounts(element.Character, element.Number, false, element.WoundedNumber, 0, true, -1);
			step = "apply_target_xp";
			AdjustRosterXpBestEffort(target, element.Character, element.Xp, isPrisonerRoster, "transfer");
			step = "verify";
			RosterElementState sourceAfter = GetRosterElementState(source, element.Character);
			RosterElementState targetAfter = GetRosterElementState(target, element.Character);
			int transferredXp = targetAfter.Xp - targetBefore.Xp;
			if (sourceAfter.Number != sourceBefore.Number - element.Number
				|| targetAfter.Number != targetBefore.Number + element.Number
				|| sourceAfter.WoundedNumber != sourceBefore.WoundedNumber - element.WoundedNumber
				|| targetAfter.WoundedNumber != targetBefore.WoundedNumber + element.WoundedNumber)
			{
				throw new InvalidOperationException("名册人数或伤员守恒校验失败：" + (element.Character.StringId ?? "unknown"));
			}
			if (transferredXp < 0 || transferredXp > element.Xp)
			{
				throw new InvalidOperationException("名册经验值边界校验失败：" + (element.Character.StringId ?? "unknown"));
			}
			if (transferredXp != element.Xp)
			{
				Log("roster xp clamped during transfer troop=" + (element.Character.StringId ?? "unknown") + " requestedXp=" + element.Xp + " transferredXp=" + transferredXp);
			}
		}
		catch (Exception ex)
		{
			try
			{
				RestoreRosterElementExact(target, element.Character, targetBefore, isPrisonerRoster);
				RestoreRosterElementExact(source, element.Character, sourceBefore, isPrisonerRoster);
			}
			catch (Exception rollbackEx)
			{
				Log("roster element immediate rollback failed troop=" + (element.Character.StringId ?? "unknown") + " step=" + step + " error=" + rollbackEx);
			}
			throw new InvalidOperationException("名册转移失败 troop=" + (element.Character.StringId ?? "unknown") + " step=" + step, ex);
		}
	}

	private static void RestoreRosterElementExact(TroopRoster roster, CharacterObject character, RosterElementState snapshot, bool isPrisonerRoster)
	{
		if (roster == null || character == null)
		{
			return;
		}
		RosterElementState current = GetRosterElementState(roster, character);
		int countDelta = snapshot.Number - current.Number;
		int woundedDelta = snapshot.WoundedNumber - current.WoundedNumber;
		if (countDelta != 0 || woundedDelta != 0)
		{
			roster.AddToCounts(character, countDelta, false, woundedDelta, 0, true, -1);
		}
		RosterElementState afterCounts = GetRosterElementState(roster, character);
		int xpDelta = snapshot.Xp - afterCounts.Xp;
		if (xpDelta != 0)
		{
			AdjustRosterXpBestEffort(roster, character, xpDelta, isPrisonerRoster, "rollback");
		}
	}

	private static void AdjustRosterXpBestEffort(TroopRoster roster, CharacterObject character, int xpDelta, bool isPrisonerRoster, string context)
	{
		if (roster == null || character == null || xpDelta == 0)
		{
			return;
		}
		if (!isPrisonerRoster && !CanSafelyApplyMemberRosterXp(character))
		{
			Log("roster xp skipped for non-upgradable troop troop=" + (character.StringId ?? "unknown") + " delta=" + xpDelta + " context=" + (context ?? ""));
			return;
		}
		try
		{
			roster.AddXpToTroop(character, xpDelta);
		}
		catch (Exception ex)
		{
			// SetElementXp writes the value before invoking the game's clamp callback.
			// Keep the count transfer valid and report the callback failure instead of
			// rolling the entire party transaction forever.
			Log("roster xp callback failed troop=" + (character.StringId ?? "unknown") + " delta=" + xpDelta + " context=" + (context ?? "") + " error=" + ex);
		}
	}

	private static bool CanSafelyApplyMemberRosterXp(CharacterObject character)
	{
		try
		{
			if (character == null || character.IsHero || character.UpgradeTargets == null || character.UpgradeTargets.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				if (character.UpgradeTargets[i] == null)
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void MoveAllPrisonersToSettlementVerified(MobileParty sourceParty, Settlement target)
	{
		MoveAllPrisonersVerified(sourceParty, target?.Party);
	}

	private static void MoveAllPrisonersToPartyVerified(MobileParty sourceParty, MobileParty targetParty, List<RosterElementTransferRecord> transferRecords = null)
	{
		MoveAllPrisonersVerified(sourceParty, targetParty?.Party, transferRecords);
	}

	private static void MoveAllPrisonersVerified(MobileParty sourceParty, PartyBase targetParty, List<RosterElementTransferRecord> transferRecords = null)
	{
		TroopRoster source = sourceParty?.PrisonRoster;
		TroopRoster destination = targetParty?.PrisonRoster;
		if (source == null || destination == null)
		{
			return;
		}
		List<TroopRosterElement> snapshot = source.GetTroopRoster().Where(x => x.Character != null && x.Number > 0).ToList();
		foreach (TroopRosterElement element in snapshot)
		{
			if (!element.Character.IsHero)
			{
				MoveRosterElementVerified(source, destination, element, transferRecords, isPrisonerRoster: true);
				continue;
			}
			for (int i = 0; i < element.Number; i++)
			{
				TransferPrisonerAction.Apply(element.Character, sourceParty.Party, targetParty);
			}
		}
	}

	private static void MoveAllMemberHeroesToPartyVerified(MobileParty sourceParty, MobileParty targetParty, List<Hero> memberHeroes)
	{
		if (!IsPartyUsable(sourceParty) || !IsPartyUsable(targetParty))
		{
			throw new InvalidOperationException("并队时源部队或目标部队不可用。");
		}
		Hero leader = sourceParty.LeaderHero;
		List<Hero> orderedHeroes = (memberHeroes ?? new List<Hero>())
			.Where(x => x != null && x != Hero.MainHero)
			.Distinct()
			.OrderBy(x => x == leader ? 1 : 0)
			.ThenBy(x => x.StringId ?? "", StringComparer.Ordinal)
			.ToList();
		List<Hero> movedHeroes = new List<Hero>();
		try
		{
			foreach (Hero memberHero in orderedHeroes)
			{
				if (memberHero.PartyBelongedTo != sourceParty)
				{
					throw new InvalidOperationException("随队 Hero 的所属部队已变化：" + (memberHero.StringId ?? "unknown"));
				}
				AddHeroToPartyAction.Apply(memberHero, targetParty, showNotification: false);
				if (memberHero.PartyBelongedTo != targetParty)
				{
					throw new InvalidOperationException("随队 Hero 未能并入玩家主队：" + (memberHero.StringId ?? "unknown"));
				}
				movedHeroes.Add(memberHero);
			}
		}
		catch
		{
			RestoreMemberHeroesToPartyAfterFailedDestroy(sourceParty, movedHeroes);
			throw;
		}
	}

	private static void RestoreMemberHeroesToPartyAfterFailedDestroy(MobileParty sourceParty, IEnumerable<Hero> memberHeroes)
	{
		if (!IsPartyUsable(sourceParty))
		{
			return;
		}
		foreach (Hero memberHero in (memberHeroes ?? Enumerable.Empty<Hero>()).Where(x => x != null).Distinct())
		{
			try
			{
				if (memberHero.PartyBelongedTo != sourceParty && !memberHero.IsDead && memberHero.IsActive && !memberHero.IsPrisoner)
				{
					AddHeroToPartyAction.Apply(memberHero, sourceParty, showNotification: false);
				}
			}
			catch (Exception ex)
			{
				Log("restore member hero after failed merge destroy failed hero=" + (memberHero.StringId ?? "") + " error=" + ex.Message);
			}
		}
	}

	private static bool RollbackMergeToPlayerTransfer(MobileParty sourceParty, List<TroopRosterElement> memberSnapshot, List<TroopRosterElement> prisonerSnapshot, List<ItemRosterElement> itemSnapshot, List<Ship> shipSnapshot, List<Hero> memberHeroes, List<RosterElementTransferRecord> memberTransfers, List<RosterElementTransferRecord> prisonerTransfers)
	{
		try
		{
			if (!IsPartyUsable(sourceParty) || !IsPartyUsable(MobileParty.MainParty))
			{
				return false;
			}
			RestoreMemberHeroesToPartyAfterFailedDestroy(sourceParty, memberHeroes);
			RestoreRosterTransfersExact(memberTransfers);
			RestoreHeroPrisonersFromMainParty(sourceParty, prisonerSnapshot);
			RestoreRosterTransfersExact(prisonerTransfers);
			RestoreItemsFromCounterpartySnapshot(sourceParty.ItemRoster, MobileParty.MainParty.ItemRoster, itemSnapshot);
			if (!TryRestoreShipOwnership(shipSnapshot, MobileParty.MainParty.Party, sourceParty.Party))
			{
				throw new InvalidOperationException("并队回滚未能恢复源队船只所有权。");
			}
			if (!RosterMatchesMergeSnapshot(sourceParty.MemberRoster, memberSnapshot, isPrisonerRoster: false)
				|| !RosterMatchesMergeSnapshot(sourceParty.PrisonRoster, prisonerSnapshot, isPrisonerRoster: true)
				|| !ItemRosterMatchesSnapshot(sourceParty.ItemRoster, itemSnapshot)
				|| !PartyShipsMatchSnapshot(sourceParty, shipSnapshot))
			{
				throw new InvalidOperationException("并队回滚后的源队资产与事务快照不一致。");
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("rollback merge transfer failed party=" + (sourceParty?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static void RestoreRosterTransfersExact(List<RosterElementTransferRecord> transfers)
	{
		if (transfers == null)
		{
			return;
		}
		for (int i = transfers.Count - 1; i >= 0; i--)
		{
			RosterElementTransferRecord transfer = transfers[i];
			if (transfer?.Character == null || transfer.Source == null || transfer.Target == null)
			{
				throw new InvalidOperationException("并队名册回滚缺少事务记录。");
			}
			RestoreRosterElementExact(transfer.Target, transfer.Character, transfer.TargetBefore, transfer.IsPrisonerRoster);
			RestoreRosterElementExact(transfer.Source, transfer.Character, transfer.SourceBefore, transfer.IsPrisonerRoster);
			RosterElementState sourceAfter = GetRosterElementState(transfer.Source, transfer.Character);
			RosterElementState targetAfter = GetRosterElementState(transfer.Target, transfer.Character);
			if (!RosterElementMatches(sourceAfter, transfer.SourceBefore, transfer.IsPrisonerRoster)
				|| !RosterElementMatches(targetAfter, transfer.TargetBefore, transfer.IsPrisonerRoster))
			{
				throw new InvalidOperationException("并队名册事务快照回滚失败：" + (transfer.Character.StringId ?? "unknown"));
			}
		}
	}

	private static bool RosterElementMatches(RosterElementState actual, RosterElementState expected, bool isPrisonerRoster)
	{
		return actual.Number == expected.Number
			&& actual.WoundedNumber == expected.WoundedNumber
			&& (actual.Xp == expected.Xp || (!isPrisonerRoster && !CanSafelyApplyMemberRosterXp(expected.Character)));
	}

	private static bool RosterElementMatches(RosterElementState actual, TroopRosterElement expected, bool isPrisonerRoster)
	{
		return actual.Number == expected.Number
			&& actual.WoundedNumber == expected.WoundedNumber
			&& (actual.Xp == expected.Xp || (!isPrisonerRoster && !CanSafelyApplyMemberRosterXp(expected.Character)));
	}

	private static bool RosterMatchesMergeSnapshot(TroopRoster roster, List<TroopRosterElement> snapshot, bool isPrisonerRoster)
	{
		if (roster == null || snapshot == null)
		{
			return false;
		}
		List<TroopRosterElement> expected = snapshot.Where(x => x.Character != null && x.Number > 0).ToList();
		List<TroopRosterElement> actual = roster.GetTroopRoster().Where(x => x.Character != null && x.Number > 0).ToList();
		if (expected.Count != actual.Count)
		{
			return false;
		}
		return expected.All(element => RosterElementMatches(GetRosterElementState(roster, element.Character), element, isPrisonerRoster));
	}

	private static void RestoreHeroPrisonersFromMainParty(MobileParty sourceParty, List<TroopRosterElement> prisonerSnapshot)
	{
		foreach (TroopRosterElement desired in (prisonerSnapshot ?? new List<TroopRosterElement>()).Where(x => x.Character?.IsHero == true && x.Number > 0))
		{
			int current = GetRosterElementState(sourceParty.PrisonRoster, desired.Character).Number;
			for (int i = current; i < desired.Number; i++)
			{
				TransferPrisonerAction.Apply(desired.Character, MobileParty.MainParty.Party, sourceParty.Party);
			}
			if (GetRosterElementState(sourceParty.PrisonRoster, desired.Character).Number != desired.Number)
			{
				throw new InvalidOperationException("Hero 俘虏并队回滚失败：" + (desired.Character.StringId ?? "unknown"));
			}
		}
	}

	private static void RestoreItemsFromCounterpartySnapshot(ItemRoster source, ItemRoster counterparty, List<ItemRosterElement> sourceSnapshot)
	{
		if (source == null || counterparty == null || sourceSnapshot == null)
		{
			throw new InvalidOperationException("并队物资回滚缺少源、目标或快照。");
		}
		foreach (ItemRosterElement desired in sourceSnapshot.Where(x => x.Amount > 0))
		{
			int sourceBefore = GetExactItemCount(source, desired.EquipmentElement);
			int counterpartyBefore = GetExactItemCount(counterparty, desired.EquipmentElement);
			int missing = desired.Amount - sourceBefore;
			if (missing < 0 || counterpartyBefore < missing)
			{
				throw new InvalidOperationException("并队物资回滚差额异常。");
			}
			try
			{
				if (missing > 0)
				{
					source.AddToCounts(desired.EquipmentElement, missing);
					counterparty.AddToCounts(desired.EquipmentElement, -missing);
				}
				if (GetExactItemCount(source, desired.EquipmentElement) != desired.Amount
					|| GetExactItemCount(source, desired.EquipmentElement) + GetExactItemCount(counterparty, desired.EquipmentElement) != sourceBefore + counterpartyBefore)
				{
					throw new InvalidOperationException("并队物资回滚校验失败。");
				}
			}
			catch
			{
				int sourceDelta = sourceBefore - GetExactItemCount(source, desired.EquipmentElement);
				int counterpartyDelta = counterpartyBefore - GetExactItemCount(counterparty, desired.EquipmentElement);
				if (sourceDelta != 0)
				{
					source.AddToCounts(desired.EquipmentElement, sourceDelta);
				}
				if (counterpartyDelta != 0)
				{
					counterparty.AddToCounts(desired.EquipmentElement, counterpartyDelta);
				}
				throw;
			}
		}
	}

	private static bool ItemRosterMatchesSnapshot(ItemRoster roster, List<ItemRosterElement> snapshot)
	{
		if (roster == null || snapshot == null)
		{
			return false;
		}
		List<ItemRosterElement> expected = snapshot.Where(x => x.Amount > 0).ToList();
		List<ItemRosterElement> actual = roster.Where(x => x.Amount > 0).ToList();
		return expected.Count == actual.Count
			&& expected.All(element => GetExactItemCount(roster, element.EquipmentElement) == element.Amount);
	}

	private static int GetExactItemCount(ItemRoster roster, EquipmentElement equipmentElement)
	{
		if (roster == null)
		{
			return 0;
		}
		int index = roster.FindIndexOfElement(equipmentElement);
		return index >= 0 ? roster.GetElementCopyAtIndex(index).Amount : 0;
	}

	private static void MoveAllItemsVerified(ItemRoster source, ItemRoster target)
	{
		if (source == null || target == null)
		{
			return;
		}
		List<ItemRosterElement> snapshot = source.Where(x => x.Amount > 0).ToList();
		foreach (ItemRosterElement element in snapshot)
		{
			int sourceBefore = GetExactItemCount(source, element.EquipmentElement);
			int targetBefore = GetExactItemCount(target, element.EquipmentElement);
			try
			{
				target.Add(element);
				source.Remove(element);
				if (GetExactItemCount(source, element.EquipmentElement) + GetExactItemCount(target, element.EquipmentElement) != sourceBefore + targetBefore)
				{
					throw new InvalidOperationException("物资守恒校验失败。");
				}
			}
			catch
			{
				int sourceDelta = sourceBefore - GetExactItemCount(source, element.EquipmentElement);
				int targetDelta = targetBefore - GetExactItemCount(target, element.EquipmentElement);
				if (sourceDelta != 0)
				{
					source.AddToCounts(element.EquipmentElement, sourceDelta);
				}
				if (targetDelta != 0)
				{
					target.AddToCounts(element.EquipmentElement, targetDelta);
				}
				throw;
			}
		}
	}

	private static List<Ship> GetPartyShipsSnapshot(MobileParty party)
	{
		return party?.Ships == null
			? new List<Ship>()
			: party.Ships.Where(x => x != null).Distinct().ToList();
	}

	private static void TransferShipsVerified(PartyBase sourceOwner, PartyBase targetOwner, IEnumerable<Ship> ships)
	{
		List<Ship> snapshot = (ships ?? Enumerable.Empty<Ship>()).Where(x => x != null).Distinct().ToList();
		if (snapshot.Count == 0)
		{
			return;
		}
		if (sourceOwner == null || targetOwner == null || sourceOwner == targetOwner)
		{
			throw new InvalidOperationException("船只转移的来源或目标无效。");
		}
		if (snapshot.Any(ship => ship.Owner != sourceOwner))
		{
			throw new InvalidOperationException("船只所有权在转移前已经变化。");
		}
		try
		{
			foreach (Ship ship in snapshot)
			{
				ChangeShipOwnerAction.ApplyByTransferring(targetOwner, ship);
				if (ship.Owner != targetOwner)
				{
					throw new InvalidOperationException("船只所有权转移校验失败。");
				}
			}
		}
		catch (Exception ex)
		{
			if (!TryRestoreShipOwnership(snapshot, targetOwner, sourceOwner))
			{
				throw new InvalidOperationException("船只转移失败且未能完整回滚。", ex);
			}
			throw new InvalidOperationException("船只转移失败，所有权已回滚。", ex);
		}
	}

	private static bool TryRestoreShipOwnership(IEnumerable<Ship> ships, PartyBase expectedCurrentOwner, PartyBase targetOwner)
	{
		if (targetOwner == null)
		{
			return false;
		}
		bool restored = true;
		foreach (Ship ship in (ships ?? Enumerable.Empty<Ship>()).Where(x => x != null).Distinct().Reverse())
		{
			if (ship.Owner == targetOwner)
			{
				continue;
			}
			if (ship.Owner != expectedCurrentOwner)
			{
				restored = false;
				continue;
			}
			try
			{
				ChangeShipOwnerAction.ApplyByTransferring(targetOwner, ship);
				if (ship.Owner != targetOwner)
				{
					restored = false;
				}
			}
			catch
			{
				restored = false;
			}
		}
		return restored;
	}

	private static bool PartyShipsMatchSnapshot(MobileParty party, IEnumerable<Ship> snapshot)
	{
		if (party?.Party == null)
		{
			return false;
		}
		List<Ship> expected = (snapshot ?? Enumerable.Empty<Ship>()).Where(x => x != null).Distinct().ToList();
		List<Ship> actual = GetPartyShipsSnapshot(party);
		return expected.Count == actual.Count
			&& expected.All(ship => ship.Owner == party.Party && actual.Contains(ship));
	}

	private static bool HasPartyShips(MobileParty party)
	{
		try
		{
			return party?.Ships != null && party.Ships.Count > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryDestroyStrictlyEmptyParty(MobileParty party, string reason)
	{
		try
		{
			if (party == null || party == MobileParty.MainParty || !party.IsActive)
			{
				return party == null || party != MobileParty.MainParty;
			}
			if ((party.MemberRoster?.TotalManCount ?? 0) != 0
				|| (party.PrisonRoster?.TotalManCount ?? 0) != 0
				|| (party.ItemRoster?.Count ?? 0) != 0
				|| HasPartyShips(party)
				|| HasActiveMapEvent(party)
				|| party.BesiegerCamp != null
				|| party.Army != null
				|| party.AttachedTo != null)
			{
				return false;
			}
			DestroyPartyAction.Apply((PartyBase)null, party);
			return !party.IsActive;
		}
		catch (Exception ex)
		{
			Log("destroy strictly empty party failed party=" + (party?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private bool TryOpenCreateCompanionParty(Hero hero, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		try
		{
			List<PartyCommandEntry> safeFollowUpCommands = SanitizeFollowUpCommands(followUpCommands);
			if (!IsHeroActuallyInPlayerMainPartyRoster(hero))
			{
				message = "只有玩家主队成员名册中确实存在的非玩家 Hero 才能隐式创建任务队伍。";
				return false;
			}
			if (!CanOpenCreateCompanionPartyScreenNow(out string blockedReason))
			{
				QueuePendingCreateCompanionParty(hero, safeFollowUpCommands);
				message = "已记录" + GetHeroName(hero) + "的创建同伴部队请求；" + blockedReason + "，返回大地图后会自动打开分兵界面。";
				return true;
			}
			return OpenCreateCompanionPartyScreen(hero, safeFollowUpCommands, out message);
		}
		catch (Exception ex)
		{
			message = "打开原版创建同伴部队界面失败：" + ex.Message;
			Log("create companion party failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static List<PartyCommandEntry> SanitizeFollowUpCommands(List<PartyCommandEntry> followUpCommands)
	{
		return (followUpCommands ?? new List<PartyCommandEntry>())
			.Where(command => command != null && IsExecutableCommand(command))
			.Select(CloneCommand)
			.ToList();
	}

	private void QueuePendingCreateCompanionParty(Hero hero, List<PartyCommandEntry> followUpCommands)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return;
		}
		lock (_queueLock)
		{
			List<PartyCommandEntry> commands = SanitizeFollowUpCommands(followUpCommands);
			if (_pendingCreatePartyRequests.TryGetValue(hero.StringId, out PendingCreateCompanionPartyRequest existing) && existing != null)
			{
				existing.FollowUpCommands = existing.FollowUpCommands ?? new List<PartyCommandEntry>();
				existing.FollowUpCommands.AddRange(commands);
			}
			else
			{
				_pendingCreatePartyRequests[hero.StringId] = new PendingCreateCompanionPartyRequest
				{
					HeroId = hero.StringId,
					FollowUpCommands = commands
				};
			}
			Volatile.Write(ref _hasPendingCreateCompanionPartyRequests, _pendingCreatePartyRequests.Count > 0 ? 1 : 0);
		}
		Log("queued create companion party hero=" + hero.StringId + " followUp=" + (followUpCommands?.Count ?? 0));
	}

	private static bool CanOpenCreateCompanionPartyScreenNow(out string blockedReason)
	{
		blockedReason = "";
		if (Mission.Current != null)
		{
			blockedReason = "当前仍在场景或阅兵中";
			return false;
		}
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			blockedReason = "当前对话尚未退出";
			return false;
		}
		if (IsPartyScreenStillActive())
		{
			blockedReason = "当前已有部队界面打开";
			return false;
		}
		if (Game.Current?.GameStateManager == null)
		{
			blockedReason = "当前游戏界面状态尚未就绪";
			return false;
		}
		if (!IsPartyUsable(MobileParty.MainParty))
		{
			blockedReason = "玩家主队当前不可用";
			return false;
		}
		return true;
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
			return false;
		}
	}

	private bool OpenCreateCompanionPartyScreen(Hero hero, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		try
		{
			List<PartyCommandEntry> safeFollowUpCommands = SanitizeFollowUpCommands(followUpCommands);
			_isOpeningCreateCompanionPartyScreen = true;
			PartyScreenClosedDelegate onClosed = (leftOwnerParty, leftMemberRoster, leftPrisonRoster, rightOwnerParty, rightMemberRoster, rightPrisonRoster, fromCancel) =>
			{
				_isOpeningCreateCompanionPartyScreen = false;
				OnCreateCompanionPartyScreenClosed(hero.StringId, safeFollowUpCommands, leftMemberRoster, leftPrisonRoster, rightOwnerParty, fromCancel);
			};
			if (hero.Clan != null)
			{
				PartyScreenHelper.OpenScreenAsCreateClanPartyForHero(hero, onClosed);
			}
			else
			{
				OpenClanlessHeroCreatePartyScreen(hero, onClosed);
			}
			message = "已打开" + GetHeroName(hero) + "的分兵界面。";
			return true;
		}
		catch (Exception ex)
		{
			_isOpeningCreateCompanionPartyScreen = false;
			message = "打开原版创建同伴部队界面失败：" + ex.Message;
			Log("open create companion party screen failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static void OpenClanlessHeroCreatePartyScreen(Hero hero, PartyScreenClosedDelegate onClosed)
	{
		TroopRoster leftMembers = TroopRoster.CreateDummyTroopRoster();
		TroopRoster leftPrisoners = TroopRoster.CreateDummyTroopRoster();
		TroopRoster rightMembers = MobileParty.MainParty.MemberRoster.CloneRosterData();
		TroopRoster rightPrisoners = MobileParty.MainParty.PrisonRoster.CloneRosterData();
		leftMembers.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
		if (rightMembers.Contains(hero.CharacterObject))
		{
			rightMembers.AddToCounts(hero.CharacterObject, -1, false, 0, 0, true, -1);
		}
		TextObject partyName = new TextObject("{HERO}的队伍");
		partyName.SetTextVariable("HERO", hero.Name);
		int partyLimit = Math.Max(1, MobileParty.MainParty?.Party?.PartySizeLimit ?? 1);
		try
		{
			Clan capacityClan = Clan.PlayerClan;
			if (capacityClan != null)
			{
				partyLimit = Math.Max(1, Campaign.Current.Models.PartySizeLimitModel.GetAssumedPartySizeForLordParty(hero, capacityClan.MapFaction, capacityClan));
			}
		}
		catch
		{
		}
		PartyScreenHelper.OpenScreenWithDummyRoster(
			leftMembers,
			leftPrisoners,
			rightMembers,
			rightPrisoners,
			partyName,
			MobileParty.MainParty.Name,
			partyLimit,
			MobileParty.MainParty.Party.PartySizeLimit,
			null,
			onClosed,
			new IsTroopTransferableDelegate(CreatePartyTroopTransferable));
	}

	private static bool CreatePartyTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character?.IsHero != true;
	}

	private void OnCreateCompanionPartyScreenClosed(string heroId, List<PartyCommandEntry> followUpCommands, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, bool fromCancel)
	{
		Hero hero = ResolveHeroByIdAny(heroId);
		MobileParty createdParty = null;
		try
		{
			if (hero == null)
			{
				Log("create companion party closed missing hero=" + (heroId ?? ""));
				return;
			}
			if (fromCancel)
			{
				LogFact(hero, GetHeroName(hero) + "的同伴部队创建已取消，后续大地图命令未执行。");
				return;
			}
			Hero partyHero = FindHeroInRoster(leftMemberRoster) ?? hero;
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			if (partyHero.Gold < partyGoldLowerThreshold)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, partyHero, partyGoldLowerThreshold - partyHero.Gold, false);
			}
			createdParty = MobilePartyHelper.CreateNewClanMobileParty(partyHero, partyHero.Clan);
			if (!IsPartyUsable(createdParty) || createdParty.LeaderHero != partyHero)
			{
				throw new InvalidOperationException("新任务队伍未能正确建立。");
			}
			RegisterPlayerDetachedParty(partyHero, createdParty);
			int movedMembers = MoveSelectedTroopsToCreatedParty(createdParty, partyHero, leftMemberRoster, rightOwnerParty);
			int movedPrisoners = MoveSelectedPrisonersToCreatedParty(createdParty, leftPrisonRoster, rightOwnerParty);
			LogFact(partyHero, GetHeroName(partyHero) + "已经创建同伴部队，并接收了" + movedMembers + "名士兵" + (movedPrisoners > 0 ? ("、" + movedPrisoners + "名俘虏") : "") + "。");
			if (followUpCommands != null && followUpCommands.Count > 0)
			{
				if (TryAppendQueue(partyHero, followUpCommands, out string fact, out string queueMessage))
				{
					if (!string.IsNullOrWhiteSpace(fact))
					{
						MyBehavior.AppendExternalDialogueHistory(partyHero, null, null, fact);
					}
					DisplayCommandMessage(queueMessage, isFailure: false);
				}
				else
				{
					LogFact(partyHero, GetHeroName(partyHero) + "创建同伴部队后无法接续后续大地图命令：" + queueMessage);
				}
			}
		}
		catch (Exception ex)
		{
			Log("create companion party close failed hero=" + (heroId ?? "") + " error=" + ex);
			if (hero != null)
			{
				EnsureHeroRemainsAvailableAfterCreateFailure(hero, createdParty);
				LogFact(hero, GetHeroName(hero) + "创建同伴部队失败：" + ex.Message);
			}
		}
	}

	private static Hero FindHeroInRoster(TroopRoster roster)
	{
		if (roster == null)
		{
			return null;
		}
		foreach (TroopRosterElement element in roster.GetTroopRoster())
		{
			if (element.Character?.IsHero == true)
			{
				return element.Character.HeroObject;
			}
		}
		return null;
	}

	private void EnsureHeroRemainsAvailableAfterCreateFailure(Hero hero, MobileParty createdParty)
	{
		if (hero == null || hero == Hero.MainHero || IsPartyUsable(createdParty))
		{
			return;
		}
		RemovePlayerDetachedParty(hero, createdParty, "create_failed");
		if (IsHeroActuallyInPlayerMainPartyRoster(hero))
		{
			return;
		}
		try
		{
			AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, showNotification: false);
		}
		catch (Exception ex)
		{
			Log("restore hero after create failure failed hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
			try
			{
				MobileParty.MainParty?.MemberRoster?.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
			}
			catch
			{
			}
		}
	}

	private static int MoveSelectedTroopsToCreatedParty(MobileParty createdParty, Hero partyHero, TroopRoster leftMemberRoster, PartyBase rightOwnerParty)
	{
		if (!IsPartyUsable(createdParty) || leftMemberRoster == null)
		{
			return 0;
		}
		int moved = 0;
		foreach (TroopRosterElement element in leftMemberRoster.GetTroopRoster())
		{
			if (element.Character == null || element.Character == partyHero?.CharacterObject || element.Number <= 0)
			{
				continue;
			}
			createdParty.MemberRoster.Add(element);
			rightOwnerParty?.MemberRoster?.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp, true, -1);
			moved += element.Number;
		}
		return moved;
	}

	private static int MoveSelectedPrisonersToCreatedParty(MobileParty createdParty, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty)
	{
		if (!IsPartyUsable(createdParty) || leftPrisonRoster == null)
		{
			return 0;
		}
		int moved = 0;
		foreach (TroopRosterElement element in leftPrisonRoster.GetTroopRoster())
		{
			if (element.Character == null || element.Number <= 0)
			{
				continue;
			}
			createdParty.PrisonRoster.Add(element);
			rightOwnerParty?.PrisonRoster?.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp, true, -1);
			moved += element.Number;
		}
		return moved;
	}

	private bool CanMergeToPlayer(Hero hero, MobileParty party, out string reason)
	{
		return TryValidatePlayerWildernessPartyForMerge(hero, party, this, out reason);
	}

	private static string BuildMergeEligibilityFailureMessage(Hero hero, string reason)
	{
		string actorName = GetHeroName(hero);
		switch ((reason ?? "").Trim())
		{
			case "not_player_companion_family_or_registered_detachment":
				return "大地图命令失败：" + actorName + "不是玩家的家族成员、同伴或本模组登记的临时分队，不能执行回队合并。";
			case "foreign_guest_missing_foreign_clan":
				return "大地图命令失败：" + actorName + "既不是玩家成员，也没有可保留的外族家族身份。";
			case "foreign_guest_inactive_or_captive":
				return "大地图命令失败：" + actorName + "当前死亡、失效或被俘，不能作为外族客军并入玩家队伍。";
			case "foreign_guest_not_own_lord_party":
				return "大地图命令失败：" + actorName + "当前率领的不是自己家族的领主野外部队，不能整队并入。";
			case "foreign_guest_is_governor":
				return "大地图命令失败：" + actorName + "仍担任总督；为避免原版自动解除职位，不能作为外族客军并入。";
			case "foreign_guest_in_army":
				return "大地图命令失败：" + actorName + "当前正在军团中，必须先退出军团才能整队并入玩家。";
			case "foreign_guest_at_war":
				return "大地图命令失败：" + actorName + "所属势力正与玩家交战，不能以保留原家族身份的客军形式并入。";
			case "not_independent_wilderness_party":
				return "大地图命令失败：" + actorName + "当前没有可归并的独立野外队伍。";
			case "hero_party_mismatch":
				return "大地图命令失败：" + actorName + "已经不是该野外队伍的实际领队，已停止回队合并。";
			case "hero_missing_from_party_roster":
				return "大地图命令失败：" + actorName + "不在该野外队伍名册中，已停止回队合并。";
			case "main_party_invalid":
				return "大地图命令失败：玩家主队当前不可用，无法执行回队合并。";
			default:
				return "大地图命令失败：" + actorName + "当前不满足回队合并条件（" + ((reason ?? "").Trim()) + "）。";
		}
	}

	private static int MoveAllMembersToMainParty(MobileParty sourceParty)
	{
		int moved = 0;
		MobileParty targetParty = MobileParty.MainParty;
		if (sourceParty?.MemberRoster == null || targetParty?.MemberRoster == null)
		{
			return 0;
		}
		List<Hero> heroes = new List<Hero>();
		for (int i = sourceParty.MemberRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = sourceParty.MemberRoster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			int count = Math.Max(0, element.Number);
			if (character == null || count <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				if (character.HeroObject != null && character.HeroObject != Hero.MainHero)
				{
					heroes.Add(character.HeroObject);
				}
				continue;
			}
			int wounded = Math.Max(0, element.WoundedNumber);
			int xp = Math.Max(0, element.Xp);
			sourceParty.MemberRoster.AddToCounts(character, -count, insertAtFront: false, -wounded, 0, false, -1);
			if (xp > 0)
			{
				sourceParty.MemberRoster.AddXpToTroop(character, -xp);
			}
			targetParty.MemberRoster.AddToCounts(character, count, insertAtFront: false, wounded, 0, false, -1);
			if (xp > 0)
			{
				targetParty.MemberRoster.AddXpToTroop(character, xp);
			}
			moved += count;
		}
		foreach (Hero hero in heroes.Distinct())
		{
			try
			{
				AddHeroToPartyAction.Apply(hero, targetParty, showNotification: false);
				moved += 1;
			}
			catch (Exception ex)
			{
				Log("move member hero failed hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			}
		}
		return moved;
	}

	private static int MoveAllPrisonersToMainParty(MobileParty sourceParty)
	{
		int moved = 0;
		MobileParty targetParty = MobileParty.MainParty;
		if (sourceParty?.Party?.PrisonRoster == null || targetParty?.Party == null)
		{
			return 0;
		}
		for (int i = sourceParty.Party.PrisonRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = sourceParty.Party.PrisonRoster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			int count = Math.Max(0, element.Number);
			if (character == null || count <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					TransferPrisonerAction.Apply(character, sourceParty.Party, targetParty.Party);
					moved += 1;
				}
				catch (Exception ex)
				{
					Log("move prisoner hero failed hero=" + (character.HeroObject?.StringId ?? "") + " error=" + ex.Message);
				}
				continue;
			}
			int xp = Math.Max(0, element.Xp);
			sourceParty.Party.PrisonRoster.AddToCounts(character, -count, insertAtFront: false, 0, 0, false, -1);
			if (xp > 0)
			{
				sourceParty.Party.PrisonRoster.AddXpToTroop(character, -xp);
			}
			targetParty.Party.AddPrisoner(character, count);
			if (xp > 0)
			{
				targetParty.Party.PrisonRoster?.AddXpToTroop(character, xp);
			}
			moved += count;
		}
		return moved;
	}

	private static void TryDestroyEmptyParty(MobileParty party)
	{
		try
		{
			if (party == null || party == MobileParty.MainParty || !party.IsActive)
			{
				return;
			}
			int members = party.MemberRoster?.TotalManCount ?? 0;
			int prisoners = party.Party?.PrisonRoster?.TotalManCount ?? 0;
			if (members <= 0 && prisoners <= 0)
			{
				DestroyPartyAction.Apply((PartyBase)null, party);
			}
		}
		catch (Exception ex)
		{
			Log("destroy empty party failed: " + ex.Message);
		}
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
		return (hero?.Name?.ToString() ?? hero?.StringId ?? "NPC").Trim();
	}

	private static string GetActorName(PartyCommandQueueState state, Hero hero, MobileParty party)
	{
		if (hero != null)
		{
			return GetHeroName(hero);
		}
		string stored = (state?.ActorName ?? state?.NonHeroMemoryName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(stored))
		{
			return stored;
		}
		if (party == null && state != null)
		{
			party = ResolveMobilePartyByActorState(state);
		}
		return GetPartyName(party);
	}

	private static string GetActorLogId(PartyCommandQueueState state, Hero hero, MobileParty party)
	{
		if (!string.IsNullOrWhiteSpace(hero?.StringId))
		{
			return hero.StringId;
		}
		string key = GetQueueKey(state);
		if (!string.IsNullOrWhiteSpace(key))
		{
			return key;
		}
		string partyKey = BuildPartyActorKey(party, createGuid: false);
		if (!string.IsNullOrWhiteSpace(partyKey))
		{
			return partyKey;
		}
		return party?.StringId ?? "";
	}

	private static string GetSettlementName(Settlement settlement)
	{
		return (settlement?.Name?.ToString() ?? settlement?.StringId ?? "目标定居点").Trim();
	}

	private static string GetPartyName(MobileParty party)
	{
		return (party?.Name?.ToString() ?? party?.StringId ?? "目标部队").Trim();
	}

	private static string SafeFactionId(IFaction faction)
	{
		try
		{
			return (faction?.StringId ?? faction?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetFactionDisplayName(IFaction faction)
	{
		try
		{
			return (faction?.Name?.ToString() ?? faction?.StringId ?? "目标王国").Trim();
		}
		catch
		{
			return "目标王国";
		}
	}

	private static void LogFact(Hero hero, string factText)
	{
		if (hero == null || string.IsNullOrWhiteSpace(factText))
		{
			return;
		}
		string cleanFact = factText.Trim();
		string fact = "[AFEF NPC行为补充] " + cleanFact;
		MyBehavior.AppendExternalDialogueHistory(hero, null, null, fact);
		DisplayCommandMessage(cleanFact, InferCommandMessageTone(cleanFact));
	}

	private static void LogFact(PartyCommandQueueState state, Hero hero, string factText)
	{
		if (ShouldSuppressCommandMessages(state))
		{
			return;
		}
		if (hero != null)
		{
			LogFact(hero, factText);
			return;
		}
		if (string.IsNullOrWhiteSpace(factText))
		{
			return;
		}
		string cleanFact = factText.Trim();
		string fact = "[AFEF NPC行为补充] " + cleanFact;
		string memoryId = (state?.NonHeroMemoryId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(memoryId))
		{
			MyBehavior.AppendExternalNonHeroDialogueHistory(memoryId, GetActorName(state, null, null), null, null, fact);
		}
		DisplayCommandMessage(cleanFact, InferCommandMessageTone(cleanFact));
	}

	private static void NotifyCommandStatus(PartyCommandQueueState state, string statusKey, string message, CommandMessageTone tone = CommandMessageTone.Progress)
	{
		if (state == null || string.IsNullOrWhiteSpace(statusKey) || string.IsNullOrWhiteSpace(message))
		{
			return;
		}
			if (string.Equals(state.LastStatusMessageKey, statusKey, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			state.LastStatusMessageKey = statusKey;
			if (ShouldSuppressCommandMessages(state))
			{
				return;
			}
			DisplayCommandMessage(message, tone);
		}

	private static string BuildAttackTrackingStatusMessage(PartyCommandQueueState state, Hero actorHero, MobileParty party, string targetName, string reason)
	{
		string actorName = GetActorName(state, actorHero, party);
		string safeTargetName = string.IsNullOrWhiteSpace(targetName) ? "目标" : targetName.Trim();
		string safeReason = (reason ?? "").Trim();
		if (safeReason.IndexOf("ai_commit_waiting", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return actorName + "已经接近" + safeTargetName + "，但战力评估认为当前风险过高，正在等待更好的进攻窗口。";
		}
		if (safeReason.IndexOf("force_commit_blocked", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return actorName + "已经接近" + safeTargetName + "，但攻击硬条件暂不满足，正在继续跟踪目标。";
		}
		if (safeReason.IndexOf("conditions_lost", StringComparison.OrdinalIgnoreCase) >= 0 || safeReason.IndexOf("target_left", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return actorName + "与" + safeTargetName + "的接战条件暂时丢失，正在重新追击。";
		}
		return actorName + "正在追踪" + safeTargetName + "，准备执行攻击命令。";
	}

	private static void DisplayCommandMessage(string message, bool isFailure)
	{
		DisplayCommandMessage(message, isFailure ? CommandMessageTone.Failure : CommandMessageTone.Success);
	}

	private static void DisplayCommandMessage(string message, CommandMessageTone tone)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message.Trim(), GetCommandMessageColor(tone)));
		}
		catch
		{
		}
	}

	private static Color GetCommandMessageColor(CommandMessageTone tone)
	{
		if (tone == CommandMessageTone.Failure)
		{
			return new Color(1f, 0.45f, 0.25f);
		}
		if (tone == CommandMessageTone.Progress)
		{
			return new Color(1f, 0.9f, 0.25f);
		}
		if (tone == CommandMessageTone.Neutral)
		{
			return new Color(0.7f, 0.85f, 1f);
		}
		return new Color(0.4f, 1f, 0.4f);
	}

	private static CommandMessageTone InferCommandMessageTone(string message)
	{
		string text = (message ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return CommandMessageTone.Neutral;
		}
		if (ContainsAny(text, "失败", "无法", "未能", "失效", "取消", "时限已到", "被击退", "没有", "不能", "不可"))
		{
			return CommandMessageTone.Failure;
		}
		if (ContainsAny(text, "正在", "开始", "发起", "等待", "继续执行", "结果尚未分出", "已记录", "已打开", "准备"))
		{
			return CommandMessageTone.Progress;
		}
		return CommandMessageTone.Success;
	}

	private static bool ContainsAny(string text, params string[] needles)
	{
		if (string.IsNullOrWhiteSpace(text) || needles == null)
		{
			return false;
		}
		foreach (string needle in needles)
		{
			if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log(LogSource, message ?? "");
		}
		catch
		{
		}
	}
}
