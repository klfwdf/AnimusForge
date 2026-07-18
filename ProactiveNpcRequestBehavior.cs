using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ProactiveNpcRequestBehavior : CampaignBehaviorBase
{
	public sealed class LetterNeedSnapshot
	{
		public string NeedType { get; set; }
		public string DisplayName { get; set; }
		public float Urgency { get; set; }
		public float TypeFatigueMultiplier { get; set; } = 1f;
		public float TypeWeightMultiplier { get; set; } = 1f;
		public string FactText { get; set; }
		public string IntentText { get; set; }
	}

	private const string StorageKey = "_af_proactive_npc_request_state_v1";
	private const string NeedFoodShortage = "FoodShortage";
	private const string NeedMoneyShortage = "MoneyShortage";
	private const string NeedTroopShortage = "TroopShortage";
	private const string NeedPrisonerOverload = "PrisonerOverload";
	private const string NeedKingdomMercenaryInvite = "KingdomMercenaryInvite";
	private const string NeedKingdomVassalInvite = "KingdomVassalInvite";
	private const string NeedPoliticalAgenda = "PoliticalAgenda";
	private const string NeedPolicySupport = "PolicySupport";
	private const string NeedPolicyDiscussion = "PolicyDiscussion";
	private const string NeedDiplomacy = "Diplomacy";
	private const string NeedClanCaptive = "ClanCaptive";
	private const string NeedLowMorale = "LowMorale";
	private const string NeedMountShortage = "MountShortage";
	private const string NeedOverburdened = "Overburdened";
	private const string NeedClanFinanceStrain = "ClanFinanceStrain";
	private const string NeedMarriageAlliancePressure = "MarriageAlliancePressure";
	private const string NeedRevengePressure = "RevengePressure";
	private const string NeedFiefGovernanceAnxiety = "FiefGovernanceAnxiety";
	private const string NeedAllySupport = "AllySupport";
	private const string NeedClanService = "ClanService";
	private const string NeedRomanticInteraction = "RomanticInteraction";
	private const string NeedTerritorialInterrogation = "TerritorialInterrogation";
	private const string NeedGreeting = "Greeting";
	private const string NeedFriendship = "Friendship";
	private const string NeedCourtship = "Courtship";
	private const string NeedArmyJoinRequest = "ArmyJoinRequest";
	private const string NeedBanditSuppression = "BanditSuppression";
	private const string NeedPoliticalRivalSuppression = "PoliticalRivalSuppression";
	private const string NeedSettlementPurchase = "SettlementPurchase";
	private const string NeedSettlementSale = "SettlementSale";
	private const string TriggerSourceNeedDriven = "NeedDriven";
	private const string TriggerSourceNotorietyDriven = "NotorietyDriven";
	private const int MercenaryInviteMinPlayerClanTier = 1;
	private const int VassalInviteMinPlayerClanTier = 2;
	private const int KingdomServiceInviteMinNpcTrust = 10;
	private const int PlayerFoodDaysRequiredForFoodRequest = 50;
	private const float PlayerPartyFillRatioRequiredForTroopRequest = 0.80f;
	private const float KingdomStrongEnoughToSkipMercenaryRatio = 3f;
	private const float ActiveRequestTtlHours = 18f;
	private const double ActiveEncounterProbeSeconds = 0.35;
	private const int CandidateScanTargetFrames = 45;
	private const int CandidateScanMaxPartiesPerTick = 16;
	private const double CandidateScanFrameBudgetMilliseconds = 1.5;

	private ProactiveNpcRequestSession _activeSession;
	private Dictionary<string, float> _heroCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, float> _needTypeFatigueUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private float _globalCooldownUntilHours;
	private float _lastScanHour = -99999f;
	private PendingOpeningFact _pendingNativeOpening;
	private PendingOpeningFact _pendingSceneOpening;
	private MobileParty _activePartyCache;
	private string _activePartyCacheId = "";
	private long _nextActiveEncounterProbeUtcTicks;
	private ProactiveCandidateScanState _candidateScan;
	private readonly Dictionary<string, BanditSuppressionSnapshotCacheEntry> _banditSuppressionSnapshotsByClan = new Dictionary<string, BanditSuppressionSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, SettlementSaleSnapshotCacheEntry> _settlementSaleSnapshotsByClan = new Dictionary<string, SettlementSaleSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PolicySupportSnapshotCacheEntry> _policySupportSnapshotsByClan = new Dictionary<string, PolicySupportSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private PolicyDiscussionSnapshotCacheEntry _policyDiscussionSnapshotCache;
	private readonly Dictionary<string, ClanCaptiveSnapshotCacheEntry> _clanCaptiveSnapshotsByClan = new Dictionary<string, ClanCaptiveSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, FiefGovernanceSnapshotCacheEntry> _fiefGovernanceSnapshotsByClan = new Dictionary<string, FiefGovernanceSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, AllySupportSnapshotCacheEntry> _allySupportSnapshotsByClan = new Dictionary<string, AllySupportSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, KingdomManpowerNeedSnapshotCacheEntry> _kingdomManpowerNeedSnapshotsByKingdom = new Dictionary<string, KingdomManpowerNeedSnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);

	public static ProactiveNpcRequestBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		Logger.Log("ProactiveNpcRequest", "registered v1 behavior.");
	}

	public override void SyncData(IDataStore dataStore)
	{
		string storageJson = null;
		if (dataStore.IsSaving)
		{
			storageJson = JsonConvert.SerializeObject(new ProactiveNpcRequestStorage
			{
				ActiveSession = _activeSession,
				HeroCooldownUntilDays = _heroCooldownUntilDays,
				NeedTypeFatigueUntilDays = _needTypeFatigueUntilDays,
				GlobalCooldownUntilHours = _globalCooldownUntilHours,
				// Incremental scans are runtime-only. A save during the one-second scan window retries after load.
				LastScanHour = _candidateScan == null ? _lastScanHour : -99999f
			});
			CampaignSaveChunkHelper.LogRawJsonSaveStats(StorageKey, "ProactiveNpcRequest", storageJson, "heroCooldowns=" + (_heroCooldownUntilDays?.Count ?? 0) + " typeFatigues=" + (_needTypeFatigueUntilDays?.Count ?? 0) + " active=" + (_activeSession != null));
			CampaignSaveChunkHelper.SaveChunkedString(dataStore, StorageKey, storageJson, "ProactiveNpcRequest");
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
			storageJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, StorageKey, "ProactiveNpcRequest");
			ProactiveNpcRequestStorage storage = string.IsNullOrWhiteSpace(storageJson) ? null : JsonConvert.DeserializeObject<ProactiveNpcRequestStorage>(storageJson);
			_activeSession = storage?.ActiveSession;
			NormalizeActiveSessionSingleNeed();
			_heroCooldownUntilDays = NormalizeCooldownDictionary(storage?.HeroCooldownUntilDays);
			_needTypeFatigueUntilDays = NormalizeCooldownDictionary(storage?.NeedTypeFatigueUntilDays ?? storage?.NeedCooldownUntilDays);
			_globalCooldownUntilHours = storage?.GlobalCooldownUntilHours ?? 0f;
			_lastScanHour = storage?.LastScanHour ?? -99999f;
			_pendingNativeOpening = null;
			_pendingSceneOpening = null;
			ClearActivePartyCache();
			_candidateScan = null;
			_policyDiscussionSnapshotCache = null;
		}
		catch (Exception ex)
		{
			_activeSession = null;
			_heroCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			_needTypeFatigueUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			ClearActivePartyCache();
			_candidateScan = null;
			_policyDiscussionSnapshotCache = null;
			Logger.Log("ProactiveNpcRequest", "load failed: " + ex.Message);
		}
	}

	public static bool IsProactiveRequestParty(MobileParty party)
	{
		try
		{
			return Instance?.IsActiveParty(party) == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryBuildMenuText(Hero hero, out string title, out string body)
	{
		title = "";
		body = "";
		try
		{
			if (Instance?.IsActiveHero(hero) != true)
			{
				return false;
			}
			string name = hero?.Name?.ToString() ?? "这位领主";
			title = "NPC主动接触";
			body = name + "的队伍主动追上了你。他似乎有事想找你谈谈。";
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsActiveRequestHero(Hero hero)
	{
		return Instance?.IsActiveHero(hero) == true;
	}

	public static List<LetterNeedSnapshot> GetLetterNeedSnapshotsForExternal(Hero hero)
	{
		try
		{
			return Instance?.BuildLetterNeedSnapshots(hero) ?? new List<LetterNeedSnapshot>();
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "letter need evaluation failed hero=" + GetHeroKey(hero) + " error=" + ex.Message);
			return new List<LetterNeedSnapshot>();
		}
	}

	public static bool IsNeedTypeActiveForExternal(string needType)
	{
		try
		{
			string normalized = NormalizeNeedType(needType);
			return Instance?._activeSession != null
				&& Instance.GetActiveNeedTypes().Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	public static void RecordLetterNeedDeliveredForExternal(string needType)
	{
		try
		{
			Instance?.RecordNeedTypeFatigue(needType, "letter_delivered");
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "record delivered letter need failed need=" + (needType ?? "") + " error=" + ex.Message);
		}
	}

	public static void MarkEncounterOpened(Hero hero)
	{
		try
		{
			Instance?.MarkEncounterOpenedInternal(hero);
		}
		catch
		{
		}
	}

	public static void MarkNativeConversationOpening(Hero hero)
	{
		try
		{
			Instance?.MarkConversationOpeningInternal(hero, nativeConversation: true);
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "mark native opening failed: " + ex.Message);
		}
	}

	public static void MarkSceneConversationOpening(Hero hero)
	{
		try
		{
			Instance?.MarkConversationOpeningInternal(hero, nativeConversation: false);
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "mark scene opening failed: " + ex.Message);
		}
	}

	public static void CompleteActiveForHero(Hero hero, string reason)
	{
		try
		{
			Instance?.CompleteActiveForHeroInternal(hero, reason);
		}
		catch
		{
		}
	}

	public static bool HasPendingNativeOpeningForCurrentConversation()
	{
		try
		{
			Hero hero = ShoutBehavior.GetNativeConversationTargetHeroForExternal();
			return Instance?.PendingMatches(Instance._pendingNativeOpening, hero) == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryConsumePendingNativeOpening(Hero hero, out string extraFact, out string promptText)
	{
		extraFact = "";
		promptText = "";
		try
		{
			return Instance?.TryConsumePendingOpening(hero, nativeConversation: true, out extraFact, out promptText) == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryPeekPendingSceneOpening(out Hero hero, out string extraFact, out string promptText)
	{
		hero = null;
		extraFact = "";
		promptText = "";
		try
		{
			return Instance?.TryPeekPendingOpening(nativeConversation: false, out hero, out extraFact, out promptText) == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryConsumePendingSceneOpeningForHero(Hero hero, out string extraFact, out string promptText)
	{
		extraFact = "";
		promptText = "";
		try
		{
			return Instance?.TryConsumePendingOpening(hero, nativeConversation: false, out extraFact, out promptText) == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryConsumePendingSceneOpeningForAgents(IEnumerable<Agent> agents, out string extraFact)
	{
		extraFact = "";
		try
		{
			if (Instance == null || agents == null)
			{
				return false;
			}
			foreach (Agent agent in agents)
			{
				Hero hero = TryResolveHeroFromAgent(agent);
				if (hero != null && Instance.TryConsumePendingOpening(hero, nativeConversation: false, out extraFact, out var _))
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

	private void OnHourlyTick()
	{
		try
		{
			CleanupActiveSessionIfNeeded("hourly_tick");
			TryStartNewRequest();
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "hourly tick failed: " + ex.Message);
		}
	}

	private void OnCampaignTick(float dt)
	{
		try
		{
			ProcessCandidateScan();
			if (_activeSession == null || !string.Equals(_activeSession.Stage, "Chasing", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			long nowTicks = DateTime.UtcNow.Ticks;
			if (nowTicks < _nextActiveEncounterProbeUtcTicks)
			{
				return;
			}
			_nextActiveEncounterProbeUtcTicks = DateTime.UtcNow.AddSeconds(ActiveEncounterProbeSeconds).Ticks;
			TryOpenActiveEncounterWhenClose();
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "campaign tick failed: " + ex.Message);
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		try
		{
			if (IsActiveParty(mobileParty))
			{
				CancelActiveSession("party_destroyed", releaseParty: false);
			}
		}
		catch
		{
		}
	}

	private void TryStartNewRequest()
	{
		if (_candidateScan != null)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: MCM settings unavailable.");
			return;
		}
		if (!settings.EnableProactiveNpcRequests)
		{
			Logger.LogVerbose("ProactiveNpcRequest", "scan_disabled", () => "scan skipped: disabled by MCM.", 30.0);
			return;
		}
		float nowHours = NowHours();
		int scanIntervalHours = GetEffectiveScanIntervalHours(settings);
		if (nowHours - _lastScanHour < scanIntervalHours)
		{
			return;
		}
		_lastScanHour = nowHours;
		PruneExpiredNeedTypeFatigue(NowDays());
		if (_activeSession != null)
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: active session hero=" + (_activeSession.HeroId ?? "") + " stage=" + (_activeSession.Stage ?? ""));
			return;
		}
		if (nowHours < _globalCooldownUntilHours)
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: global cooldown remainingHours=" + Math.Max(0f, _globalCooldownUntilHours - nowHours).ToString("0.0"));
			return;
		}
		if (TryGetPlayerBusyReason(out var busyReason))
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: player busy reason=" + busyReason);
			return;
		}
		if (!settings.ProactiveNpcRequestTestMode)
		{
			int minTier = Clamp(settings.ProactiveNpcRequestMinClanTier, 0, 6);
			int playerTier = Clan.PlayerClan?.Tier ?? 0;
			if (playerTier < minTier)
			{
				Logger.Log("ProactiveNpcRequest", "scan skipped: clan tier " + playerTier + " < min " + minTier);
				return;
			}
		}
		StartCandidateScan(settings, nowHours);
	}

	private void StartCandidateScan(DuelSettings settings, float nowHours)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: main party unavailable when creating incremental scan.");
			return;
		}
		List<MobileParty> parties = (MobileParty.AllLordParties ?? Enumerable.Empty<MobileParty>()).Where(party => party != null).ToList();
		int batchSize = Math.Max(1, (int)Math.Ceiling(parties.Count / (double)CandidateScanTargetFrames));
		batchSize = Math.Min(CandidateScanMaxPartiesPerTick, batchSize);
		_candidateScan = new ProactiveCandidateScanState
		{
			Settings = settings,
			Parties = parties,
			BatchSize = batchSize,
			Stats = new CandidateScanStats(),
			StartedAtUtcTicks = DateTime.UtcNow.Ticks
		};
		Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_start", () => "incremental scan started parties=" + parties.Count + " batchSize=" + batchSize + " targetFrames=" + CandidateScanTargetFrames + " nowHours=" + nowHours.ToString("0.0"), 5.0);
	}

	private void ProcessCandidateScan()
	{
		ProactiveCandidateScanState scan = _candidateScan;
		if (scan == null)
		{
			return;
		}
		DuelSettings currentSettings = DuelSettings.GetSettings();
		if (currentSettings == null || !currentSettings.EnableProactiveNpcRequests || _activeSession != null)
		{
			Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_cancel", () => "incremental scan cancelled enabled=" + (currentSettings?.EnableProactiveNpcRequests ?? false) + " active=" + (_activeSession != null), 5.0);
			_candidateScan = null;
			return;
		}
		if (scan.NextIndex >= scan.Parties.Count)
		{
			CompleteCandidateScan(scan);
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		int processed = 0;
		while (scan.NextIndex < scan.Parties.Count && processed < scan.BatchSize)
		{
			scan.WorkingBatch.Clear();
			scan.WorkingBatch.Add(scan.Parties[scan.NextIndex++]);
			processed++;
			ProactiveCandidate batchCandidate = FindBestRequestCandidate(scan.Settings, out CandidateScanStats batchStats, scan.WorkingBatch, scan.TerritorialSettlementSnapshots);
			scan.Stats.MergeFrom(batchStats);
			if (IsCandidateBetter(batchCandidate, scan.BestCandidate))
			{
				scan.BestCandidate = batchCandidate;
			}
			if (stopwatch.Elapsed.TotalMilliseconds >= CandidateScanFrameBudgetMilliseconds)
			{
				break;
			}
		}
		if (scan.NextIndex >= scan.Parties.Count)
		{
			CompleteCandidateScan(scan);
		}
	}

	private void CompleteCandidateScan(ProactiveCandidateScanState scan)
	{
		if (!ReferenceEquals(_candidateScan, scan))
		{
			return;
		}
		_candidateScan = null;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableProactiveNpcRequests || _activeSession != null)
		{
			return;
		}
		if (NowHours() < _globalCooldownUntilHours)
		{
			Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_discard", () => "incremental scan discarded after global cooldown began remaining=" + (_globalCooldownUntilHours - NowHours()).ToString("0.0"), 5.0);
			return;
		}
		if (TryGetPlayerBusyReason(out string busyReason))
		{
			Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_discard", () => "incremental scan discarded after player became busy=" + busyReason, 5.0);
			return;
		}
		ProactiveCandidate candidate = scan.BestCandidate;
		if (candidate == null || candidate.Party == null || !candidate.Party.IsActive)
		{
			Logger.LogVerbose("ProactiveNpcRequest", "scan_no_candidate", () => "scan no candidate: " + scan.Stats.ToLogString(), 10.0);
			return;
		}
		Logger.Log("ProactiveNpcRequest", "scan selected: triggerSource=" + (candidate.TriggerSource ?? "") + " knownMajorBefore=" + candidate.KnownMajorBeforeRequest + " effectiveNotoriety=" + candidate.EffectiveNotorietyAtRequest + " needChance=" + candidate.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + candidate.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + candidate.SelectedNeedUrgency.ToString("0.##") + " typeWeight=" + candidate.NeedTypeWeightMultiplier.ToString("0.##") + " need=" + (candidate.NeedType ?? "") + " hero=" + (candidate.Hero?.StringId ?? "") + " party=" + (candidate.Party?.StringId ?? "") + " distance=" + candidate.Distance.ToString("0.0") + " scanMs=" + TimeSpan.FromTicks(DateTime.UtcNow.Ticks - scan.StartedAtUtcTicks).TotalMilliseconds.ToString("0") + " stats=" + scan.Stats.ToLogString());
		StartRequest(candidate, settings);
	}

	private static bool IsCandidateBetter(ProactiveCandidate candidate, ProactiveCandidate currentBest)
	{
		if (candidate == null)
		{
			return false;
		}
		if (currentBest == null)
		{
			return true;
		}
		float candidateWeightedUrgency = GetCandidateWeightedUrgency(candidate);
		float bestWeightedUrgency = GetCandidateWeightedUrgency(currentBest);
		if (Math.Abs(candidateWeightedUrgency - bestWeightedUrgency) > 0.001f) return candidateWeightedUrgency > bestWeightedUrgency;
		if (Math.Abs(candidate.NeedUrgency - currentBest.NeedUrgency) > 0.001f) return candidate.NeedUrgency > currentBest.NeedUrgency;
		if (candidate.EffectiveNotorietyAtRequest != currentBest.EffectiveNotorietyAtRequest) return candidate.EffectiveNotorietyAtRequest > currentBest.EffectiveNotorietyAtRequest;
		return candidate.Distance < currentBest.Distance;
	}

	private ProactiveCandidate FindBestRequestCandidate(DuelSettings settings, out CandidateScanStats stats, IEnumerable<MobileParty> sourceParties = null, Dictionary<string, TerritorialSettlementSnapshot> territorialSettlementSnapshots = null)
	{
		stats = new CandidateScanStats();
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			stats.MainPartyMissing = true;
			return null;
		}
		float distanceMultiplier = Clamp(settings.ProactiveNpcRequestDistanceMultiplier, 0.5f, 5f);
		float maxDistance = Math.Max(1f, mainParty.SeeingRange * distanceMultiplier);
		ProactiveCandidate bestCandidate = null;
		foreach (MobileParty party in sourceParties ?? MobileParty.AllLordParties ?? Enumerable.Empty<MobileParty>())
		{
			stats.TotalLordParties++;
			if (!TryBuildBaseCandidate(party, mainParty, settings, out ProactiveCandidate candidate, out string skipReason))
			{
				stats.AddSkip(skipReason);
				continue;
			}
			stats.BaseEligible++;
			if (candidate.Distance < 0f)
			{
				stats.AddSkip("distance_invalid");
				continue;
			}
			if (candidate.Distance > maxDistance)
			{
				stats.OutOfRange++;
				continue;
			}
			stats.InRange++;
			List<ProactiveCandidate> needCandidates = new List<ProactiveCandidate>();
			if (!candidate.AtWarWithPlayer)
			{
				if (TryBuildFoodShortageCandidate(candidate, settings, out ProactiveCandidate foodCandidate))
				{
					stats.FoodShortage++;
					needCandidates.Add(foodCandidate);
				}
				if (TryBuildMoneyShortageCandidate(candidate, settings, out ProactiveCandidate moneyCandidate))
				{
					stats.MoneyShortage++;
					needCandidates.Add(moneyCandidate);
				}
				if (TryBuildTroopShortageCandidate(candidate, settings, out ProactiveCandidate troopCandidate))
				{
					stats.TroopShortage++;
					needCandidates.Add(troopCandidate);
				}
				if (TryBuildPrisonerOverloadCandidate(candidate, settings, out ProactiveCandidate prisonerCandidate))
				{
					stats.PrisonerOverload++;
					needCandidates.Add(prisonerCandidate);
				}
				if (TryBuildClanCaptiveCandidate(candidate, settings, out ProactiveCandidate clanCaptiveCandidate))
				{
					stats.ClanCaptive++;
					needCandidates.Add(clanCaptiveCandidate);
				}
				if (TryBuildLowMoraleCandidate(candidate, settings, out ProactiveCandidate lowMoraleCandidate))
				{
					stats.LowMorale++;
					needCandidates.Add(lowMoraleCandidate);
				}
				if (TryBuildMountShortageCandidate(candidate, settings, out ProactiveCandidate mountShortageCandidate))
				{
					stats.MountShortage++;
					needCandidates.Add(mountShortageCandidate);
				}
				if (TryBuildOverburdenedCandidate(candidate, settings, out ProactiveCandidate overburdenedCandidate))
				{
					stats.Overburdened++;
					needCandidates.Add(overburdenedCandidate);
				}
				if (TryBuildClanFinanceStrainCandidate(candidate, settings, out ProactiveCandidate clanFinanceCandidate))
				{
					stats.ClanFinanceStrain++;
					needCandidates.Add(clanFinanceCandidate);
				}
				if (TryBuildClanServiceCandidate(candidate, settings, out ProactiveCandidate clanServiceCandidate))
				{
					stats.ClanService++;
					needCandidates.Add(clanServiceCandidate);
				}
				if (TryBuildRomanticInteractionCandidate(candidate, settings, out ProactiveCandidate romanticInteractionCandidate))
				{
					stats.RomanticInteraction++;
					needCandidates.Add(romanticInteractionCandidate);
				}
				if (TryBuildGreetingCandidate(candidate, settings, out ProactiveCandidate greetingCandidate))
				{
					stats.Greeting++;
					needCandidates.Add(greetingCandidate);
				}
				if (TryBuildPolicyDiscussionCandidate(candidate, settings, out ProactiveCandidate policyDiscussionCandidate))
				{
					stats.PolicyDiscussion++;
					needCandidates.Add(policyDiscussionCandidate);
				}
				if (TryBuildFriendshipCandidate(candidate, settings, out ProactiveCandidate friendshipCandidate))
				{
					stats.Friendship++;
					needCandidates.Add(friendshipCandidate);
				}
				if (TryBuildCourtshipCandidate(candidate, settings, out ProactiveCandidate courtshipCandidate))
				{
					stats.Courtship++;
					needCandidates.Add(courtshipCandidate);
				}
				if (TryBuildBanditSuppressionCandidate(candidate, settings, out ProactiveCandidate banditSuppressionCandidate))
				{
					stats.BanditSuppression++;
					needCandidates.Add(banditSuppressionCandidate);
				}
				if (TryBuildTerritorialInterrogationCandidate(candidate, settings, territorialSettlementSnapshots, out ProactiveCandidate territorialInterrogationCandidate))
				{
					stats.TerritorialInterrogation++;
					needCandidates.Add(territorialInterrogationCandidate);
				}
				if (TryBuildMarriageAlliancePressureCandidate(candidate, settings, out ProactiveCandidate marriageCandidate))
				{
					stats.MarriageAlliancePressure++;
					needCandidates.Add(marriageCandidate);
				}
				if (TryBuildRevengePressureCandidate(candidate, settings, out ProactiveCandidate revengeCandidate))
				{
					stats.RevengePressure++;
					needCandidates.Add(revengeCandidate);
				}
				if (TryBuildFiefGovernanceAnxietyCandidate(candidate, settings, out ProactiveCandidate fiefGovernanceCandidate))
				{
					stats.FiefGovernanceAnxiety++;
					needCandidates.Add(fiefGovernanceCandidate);
				}
				if (TryBuildAllySupportCandidate(candidate, settings, out ProactiveCandidate allySupportCandidate))
				{
					stats.AllySupport++;
					needCandidates.Add(allySupportCandidate);
				}
				if (TryBuildKingdomMercenaryInviteCandidate(candidate, settings, out ProactiveCandidate mercenaryInviteCandidate))
				{
					stats.KingdomMercenaryInvite++;
					needCandidates.Add(mercenaryInviteCandidate);
				}
				if (TryBuildKingdomVassalInviteCandidate(candidate, settings, out ProactiveCandidate vassalInviteCandidate))
				{
					stats.KingdomVassalInvite++;
					needCandidates.Add(vassalInviteCandidate);
				}
				if (TryBuildPoliticalAgendaCandidate(candidate, settings, out ProactiveCandidate politicalAgendaCandidate))
				{
					stats.PoliticalAgenda++;
					needCandidates.Add(politicalAgendaCandidate);
				}
				if (TryBuildPolicySupportCandidate(candidate, settings, out ProactiveCandidate policySupportCandidate))
				{
					stats.PolicySupport++;
					needCandidates.Add(policySupportCandidate);
				}
				if (TryBuildPoliticalRivalSuppressionCandidate(candidate, settings, out ProactiveCandidate politicalRivalSuppressionCandidate))
				{
					stats.PoliticalRivalSuppression++;
					needCandidates.Add(politicalRivalSuppressionCandidate);
				}
				if (TryBuildSettlementPurchaseCandidate(candidate, settings, out ProactiveCandidate settlementPurchaseCandidate))
				{
					stats.SettlementPurchase++;
					needCandidates.Add(settlementPurchaseCandidate);
				}
				if (TryBuildSettlementSaleCandidate(candidate, settings, out ProactiveCandidate settlementSaleCandidate))
				{
					stats.SettlementSale++;
					needCandidates.Add(settlementSaleCandidate);
				}
			}
			if (TryBuildDiplomacyCandidate(candidate, settings, out ProactiveCandidate diplomacyCandidate))
			{
				stats.Diplomacy++;
				needCandidates.Add(diplomacyCandidate);
			}
			ProactiveCandidate combinedCandidate = BuildCombinedNeedCandidate(needCandidates, settings);
			if (combinedCandidate != null)
			{
				stats.NeedCandidates++;
				if (TryEvaluateCandidateTrigger(combinedCandidate, settings, stats))
				{
					if (IsCandidateBetter(combinedCandidate, bestCandidate))
					{
						bestCandidate = combinedCandidate;
					}
				}
			}
		}
		return bestCandidate;
	}

	private bool TryEvaluateCandidateTrigger(ProactiveCandidate candidate, DuelSettings settings, CandidateScanStats stats)
	{
		if (candidate == null)
		{
			stats?.AddSkip("candidate_null");
			return false;
		}
		float urgency = Clamp(candidate.NeedUrgency, 0f, 100f);
		float minUrgency = GetEffectiveMinNeedUrgency(settings);
		if (urgency < minUrgency)
		{
			if (stats != null)
			{
				stats.BelowMinUrgency++;
			}
			return false;
		}
		int baseChance = GetEffectiveChancePercent(settings);
		if (baseChance <= 0)
		{
			if (stats != null)
			{
				stats.TriggerRollFailed++;
			}
			return false;
		}
		float globalScale = Clamp(baseChance / 100f, 0f, 1f);
		bool knownMajor = PlayerNotorietyBehavior.HasObserverUnlockedPlayerMajorForExternal(candidate.Hero);
		int effectiveNotoriety = PlayerNotorietyBehavior.GetEffectiveNotorietyForExternal(candidate.Hero);
		float knownMultiplier = knownMajor ? GetEffectiveKnownMajorMultiplier(settings) : 1f;
		float typeFatigueMultiplier = Clamp(candidate.NeedTypeFatigueMultiplier, 0f, 1f);
		float typeWeightMultiplier = Clamp(candidate.NeedTypeWeightMultiplier, 0f, 1f);
		if (typeFatigueMultiplier < 0.999f && stats != null)
		{
			stats.TypeFatiguedCandidates++;
		}
		float needChance = Clamp(urgency * globalScale * knownMultiplier * typeFatigueMultiplier * typeWeightMultiplier, 0f, 100f);
		float notorietyChance = knownMajor
			? 0f
			: Clamp(effectiveNotoriety * GetEffectiveNotorietyChanceMultiplier(settings) * (urgency / 100f) * globalScale * typeFatigueMultiplier * typeWeightMultiplier, 0f, 100f);
		candidate.KnownMajorBeforeRequest = knownMajor;
		candidate.EffectiveNotorietyAtRequest = effectiveNotoriety;
		candidate.NeedDrivenChance = needChance;
		candidate.NotorietyDrivenChance = notorietyChance;
		candidate.SelectedNeedUrgency = urgency;
		if (RollPercent(needChance))
		{
			candidate.TriggerSource = TriggerSourceNeedDriven;
			if (stats != null)
			{
				stats.NeedDrivenTriggered++;
			}
			return true;
		}
		if (RollPercent(notorietyChance))
		{
			candidate.TriggerSource = TriggerSourceNotorietyDriven;
			if (stats != null)
			{
				stats.NotorietyDrivenTriggered++;
			}
			return true;
		}
		if (stats != null)
		{
			stats.TriggerRollFailed++;
		}
		return false;
	}

	private List<LetterNeedSnapshot> BuildLetterNeedSnapshots(Hero hero)
	{
		List<LetterNeedSnapshot> result = new List<LetterNeedSnapshot>();
		DuelSettings settings = DuelSettings.GetSettings();
		if (hero == null || hero == Hero.MainHero || hero.IsDead || hero.IsPrisoner || hero.IsFugitive || hero.PartyBelongedToAsPrisoner != null)
		{
			return result;
		}
		ProactiveCandidate source = BuildLetterNeedBaseCandidate(hero, settings);
		if (source == null)
		{
			return result;
		}
		List<ProactiveCandidate> candidates = new List<ProactiveCandidate>();
		if (source.Party != null)
		{
			AddLetterNeedCandidate(candidates, TryBuildFoodShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildMoneyShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildTroopShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildPrisonerOverloadCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildLowMoraleCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildMountShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildOverburdenedCandidate, source, settings);
		}
		AddLetterNeedCandidate(candidates, TryBuildClanCaptiveCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildClanFinanceStrainCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildClanServiceCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildRomanticInteractionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPolicyDiscussionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildFriendshipCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildCourtshipCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildArmyJoinRequestCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildBanditSuppressionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPoliticalRivalSuppressionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildSettlementPurchaseCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildSettlementSaleCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildMarriageAlliancePressureCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildRevengePressureCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildFiefGovernanceAnxietyCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildAllySupportCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildKingdomMercenaryInviteCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildKingdomVassalInviteCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPoliticalAgendaCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPolicySupportCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildDiplomacyCandidate, source, settings);

		float nowDays = NowDays();
		foreach (ProactiveCandidate candidate in candidates
			.Where(x => x != null && x.NeedUrgency > 0f)
			.OrderByDescending(x => x.NeedUrgency))
		{
			string needType = NormalizeNeedType(candidate.NeedType);
			if (string.IsNullOrWhiteSpace(needType) || IsNeedTypeActiveForExternal(needType))
			{
				continue;
			}
			float remaining = GetNeedTypeFatigueRemainingDays(needType, nowDays);
			float fatigueMultiplier = remaining > 0f ? GetEffectiveNeedTypeFatigueMultiplier(settings) : 1f;
			result.Add(new LetterNeedSnapshot
			{
				NeedType = needType,
				DisplayName = GetNeedDisplayName(needType),
				Urgency = Clamp(candidate.NeedUrgency, 0f, 100f),
				TypeFatigueMultiplier = fatigueMultiplier,
				TypeWeightMultiplier = Clamp(GetEffectiveNeedTypeWeightMultiplier(needType, settings, allowTestModeOverride: false)
					* Clamp(candidate.IntrinsicNeedTypeWeightMultiplier, 0f, 1f), 0f, 1f),
				FactText = BuildLetterNeedFact(candidate),
				IntentText = BuildLetterNeedIntent(candidate)
			});
		}
		return result;
	}

	private delegate bool TryBuildLetterNeedCandidateDelegate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate);

	private static void AddLetterNeedCandidate(List<ProactiveCandidate> result, TryBuildLetterNeedCandidateDelegate builder, ProactiveCandidate source, DuelSettings settings)
	{
		if (result == null || builder == null)
		{
			return;
		}
		if (builder(source, settings, out ProactiveCandidate candidate) && candidate != null)
		{
			result.Add(candidate);
		}
	}

	private ProactiveCandidate BuildLetterNeedBaseCandidate(Hero hero, DuelSettings settings)
	{
		try
		{
			Clan clan = hero?.Clan;
			MobileParty party = hero?.PartyBelongedTo;
			if (party == null || !party.IsActive || party.LeaderHero != hero)
			{
				party = null;
			}
			int foodDays = party == null ? int.MaxValue : SafeFoodDays(party);
			int partyGold = party == null ? 0 : SafePartyTradeGold(party);
			int totalWage = party == null ? 0 : SafeTotalWage(party);
			float unpaidWages = party == null ? 0f : SafeUnpaidWages(party);
			int memberCount = party == null ? 0 : SafeMemberCount(party);
			int partySizeLimit = party == null ? 0 : SafePartySizeLimit(party);
			int prisonerCount = party == null ? 0 : SafePrisonerCount(party);
			int prisonerSizeLimit = party == null ? 0 : SafePrisonerSizeLimit(party);
			int inventoryCapacity = party == null ? 0 : SafeInventoryCapacity(party);
			float totalWeightCarried = party == null ? 0f : SafeTotalWeightCarried(party);
			int mountCount = party == null ? 0 : SafeMountCount(party);
			int packAnimalCount = party == null ? 0 : SafePackAnimalCount(party);
			ClanCaptiveSnapshot captive = GetCachedClanCaptiveSnapshot(hero);
			Kingdom kingdom = ResolveHeroKingdom(hero);
			MarriageAllianceSnapshot marriage = BuildMarriageAllianceSnapshot(hero);
			FiefGovernanceSnapshot fiefs = GetCachedFiefGovernanceSnapshot(clan, settings);
			AllySupportSnapshot allies = GetCachedAllySupportSnapshot(clan, kingdom, settings);
			RevengePressureSnapshot revenge = BuildRevengePressureSnapshot(hero, kingdom, captive, fiefs);
			KingdomManpowerNeedSnapshot manpower = GetCachedKingdomManpowerNeedSnapshot(kingdom);
			bool atWar = false;
			try
			{
				atWar = hero?.MapFaction != null && Hero.MainHero?.MapFaction != null && hero.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
			}
			catch
			{
			}
			return new ProactiveCandidate
			{
				Party = party,
				Hero = hero,
				Distance = -1f,
				FoodDays = foodDays,
				PartyGold = partyGold,
				TotalWage = totalWage,
				UnpaidWages = unpaidWages,
				WageDays = CalculateWageDays(partyGold, totalWage),
				MemberCount = memberCount,
				PartySizeLimit = partySizeLimit,
				PartySizeRatio = CalculatePartySizeRatio(memberCount, partySizeLimit),
				AvailableWageBudget = party == null ? 0 : SafeAvailableWageBudget(party),
				PrisonerCount = prisonerCount,
				PrisonerSizeLimit = prisonerSizeLimit,
				HeroPrisonerCount = party == null ? 0 : SafeHeroPrisonerCount(party),
				PrisonerSizeRatio = CalculatePrisonerSizeRatio(prisonerCount, prisonerSizeLimit),
				Morale = party == null ? 100f : SafeMorale(party),
				InventoryCapacity = inventoryCapacity,
				TotalWeightCarried = totalWeightCarried,
				CarryRatio = CalculateCarryRatio(totalWeightCarried, inventoryCapacity),
				MountCount = mountCount,
				PackAnimalCount = packAnimalCount,
				MountRatio = CalculateAnimalRatio(mountCount, memberCount),
				PackAnimalRatio = CalculateAnimalRatio(packAnimalCount, memberCount),
				ClanGold = SafeClanGold(clan),
				ClanDebtToKingdom = SafeClanDebtToKingdom(clan),
				CaptiveClanHeroCount = captive.Count,
				CaptiveClanHeroName = captive.FirstHeroName,
				CaptiveClanHeroHolderName = captive.FirstHolderName,
				CaptiveClanLeaderHeld = captive.LeaderHeld,
				MarriageAdultClanHeroCount = marriage.AdultClanHeroCount,
				MarriageUnmarriedAdultCount = marriage.UnmarriedAdultCount,
				MarriageFirstUnmarriedName = marriage.FirstUnmarriedName,
				MarriageRequesterUnmarried = marriage.RequesterUnmarried,
				RevengePressureScore = revenge.PressureScore,
				RevengeTargetName = revenge.TargetName,
				RevengeReasonText = revenge.ReasonText,
				FiefProblemCount = fiefs.ProblemCount,
				FiefProblemName = fiefs.FirstProblemName,
				FiefLoyalty = fiefs.LowestLoyalty,
				FiefSecurity = fiefs.LowestSecurity,
				FiefGarrisonCount = fiefs.LowestGarrisonCount,
				FiefIssueText = fiefs.IssueText,
				FiefUnderAttack = fiefs.UnderAttack,
				ClanInfluence = allies.ClanInfluence,
				FriendlyClanCount = allies.FriendlyClanCount,
				HostileClanCount = allies.HostileClanCount,
				TargetKingdom = kingdom,
				TargetKingdomId = GetKingdomKey(kingdom),
				TargetKingdomName = GetKingdomName(kingdom),
				PlayerClanTier = SafePlayerClanTier(),
				TargetHeroIsKingdomLeader = IsKingdomLeader(hero, kingdom),
				TargetClanCanOfferKingdomService = CanClanRepresentKingdom(clan, kingdom),
				KingdomFormalVassalClanCount = manpower.FormalVassalClanCount,
				KingdomMercenaryClanCount = manpower.MercenaryClanCount,
				KingdomFiefScore = manpower.FiefScore,
				KingdomWarKingdomCount = manpower.WarKingdomCount,
				KingdomPowerRatioToEnemies = manpower.PowerRatioToEnemies,
				KingdomTargetMercenaryClanCount = manpower.TargetMercenaryClanCount,
				KingdomTargetVassalClanCount = manpower.TargetVassalClanCount,
				KingdomNeedsMercenaries = manpower.NeedsMercenaries,
				KingdomNeedsVassals = manpower.NeedsVassals,
				KingdomMercenaryNeedUrgency = manpower.MercenaryNeedUrgency,
				KingdomVassalNeedUrgency = manpower.VassalNeedUrgency,
				AtWarWithPlayer = atWar
			};
		}
		catch
		{
			return null;
		}
	}

	private static string BuildLetterNeedFact(ProactiveCandidate candidate)
	{
		string npcName = candidate?.Hero?.Name?.ToString() ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(candidate?.Hero) ?? Hero.MainHero?.Name?.ToString() ?? "玩家";
		string needType = NormalizeNeedType(candidate?.NeedType);
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			return "[AFEF NPC行为补充] " + npcName + "决定主动写信给" + playerName + "。"
				+ BuildPolicyDiscussionSituation(new PolicyDiscussionSnapshot
				{
					PolicyId = candidate?.PolicyDiscussionPolicyId,
					PolicyName = candidate?.PolicyDiscussionPolicyName,
					PolicyContent = candidate?.PolicyDiscussionPolicyContent,
					KingdomName = candidate?.PolicyDiscussionKingdomName,
					PublishedDay = candidate?.PolicyDiscussionPublishedDay ?? 0
				}, npcName, playerName)
				+ "这是你自己的来意；" + playerName + "尚未答应任何事。";
		}
		string evidence = BuildLetterNeedEvidence(candidate, needType);
		return "[AFEF NPC行为补充] " + npcName + "决定主动写信给" + playerName + "。" + evidence + "这封信只谈这一件眼前事；" + playerName + "尚未答应任何安排。";
	}

	private static string BuildLetterNeedIntent(ProactiveCandidate candidate)
	{
		string needType = NormalizeNeedType(candidate?.NeedType);
		return AIConfigHandler.GetProactiveNpcRequestLetterIntent(needType);
	}

	public static bool IsRomanticInteractionEligibleForExternal(Hero hero)
	{
		return IsRomanticInteractionEligible(hero, out _);
	}

	public static bool IsRomanticInteractionOnCooldownForExternal()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedRomanticInteraction, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsRomanticInteractionUnavailableForExternal()
	{
		return IsRomanticInteractionOnCooldownForExternal()
			|| IsNeedTypeActiveForExternal(NeedRomanticInteraction)
			|| CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(NeedRomanticInteraction);
	}

	public static void RecordRomanticInteractionForExternal(string source)
	{
		try
		{
			Instance?.RecordNeedTypeFatigue(NeedRomanticInteraction, source ?? "romantic_interaction");
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "record romantic interaction cooldown failed source=" + (source ?? "") + " error=" + ex.Message);
		}
	}

	public static bool TryBuildPolicyDiscussionCompanionMotiveForExternal(Hero hero, out string factText, out string intentText, out float urgency)
	{
		factText = "";
		intentText = "";
		urgency = 0f;
		try
		{
			return Instance?.TryBuildPolicyDiscussionCompanionMotive(hero, out factText, out intentText, out urgency) == true;
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "policy discussion companion motive failed hero=" + GetHeroKey(hero) + " error=" + ex.Message);
			return false;
		}
	}

	public static bool IsPolicyDiscussionUnavailableForExternal()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null
				&& (instance.GetNeedTypeFatigueRemainingDays(NeedPolicyDiscussion, NowDays()) > 0f
					|| IsNeedTypeActiveForExternal(NeedPolicyDiscussion)
					|| CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(NeedPolicyDiscussion));
		}
		catch
		{
			return false;
		}
	}

	public static void RecordPolicyDiscussionForExternal(string source)
	{
		try
		{
			Instance?.RecordNeedTypeFatigue(NeedPolicyDiscussion, source ?? "policy_discussion");
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "record policy discussion cooldown failed source=" + (source ?? "") + " error=" + ex.Message);
		}
	}

	public static bool IsGreetingUnavailableForExternal()
	{
		return IsGreetingOnCooldown()
			|| IsNeedTypeActiveForExternal(NeedGreeting)
			|| CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(NeedGreeting);
	}

	private static string BuildLetterNeedEvidence(ProactiveCandidate candidate, string needType)
	{
		if (candidate == null)
		{
			return "";
		}
		if (string.Equals(needType, NeedFoodShortage, StringComparison.OrdinalIgnoreCase)) return "队伍的粮食已快见底。";
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase)) return "军饷和行军开销让队伍难以周转。";
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase)) return "队伍人手单薄，难以独自应付眼前局面。";
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase)) return "随军俘虏过多，已经难以妥善看守。";
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase)) return string.IsNullOrWhiteSpace(candidate.CaptiveClanHeroName) ? "家族中有人被俘，音讯令人焦灼。" : candidate.CaptiveClanHeroName + "被俘，家族正设法营救。";
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase)) return "队中人心浮动，需要尽快稳住军心。";
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase)) return "队伍缺少坐骑，行军明显受拖累。";
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase)) return "队伍携带的辎重过多，行军十分吃力。";
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase)) return "家族账目吃紧，维持开销十分艰难。";
		if (string.Equals(needType, NeedClanService, StringComparison.OrdinalIgnoreCase)) return "家族没有封地，正在为今后的归处寻一条路。";
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase)) return "他想向玩家坦露自己的牵挂。";
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase)) return "他想向一位熟人问候近况。";
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase)) return "他听闻玩家在本地颇有名声，想结识这位尚未熟悉的人。";
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase)) return "他听闻玩家的名声，想向这位尚未熟悉的人表达好感。";
		if (string.Equals(needType, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase)) return "战局不利，军团急需更多人手。";
		if (string.Equals(needType, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase)) return (candidate.BanditSuppressionSettlementName ?? "一处家族封地") + "附近强盗横行。";
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase)) return "他与" + (candidate.PoliticalRivalSuppressionRivalClanName ?? "同阵营的一家势力") + "积怨甚深，正需要盟友撑腰。";
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase)) return "他看中玩家手中的封地，想商谈购入其中一处。玩家现有封地包括：" + (candidate.SettlementPurchasePlayerFiefsText ?? "未详") + "。";
		if (string.Equals(needType, NeedSettlementSale, StringComparison.OrdinalIgnoreCase)) return (candidate.SettlementSaleTargetSettlementName ?? "一处边境封地") + "地处边境、收益不佳，又邻近" + (candidate.SettlementSaleForeignFactionName ?? "其他势力") + "的" + (candidate.SettlementSaleForeignSettlementName ?? "封地") + "；他想商谈将其转手。";
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase)) return "他在" + (candidate.TerritorialInterrogationSettlementName ?? "本国领地") + "附近遇见一位来历不明的异乡人。";
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase)) return "家族正为传承和婚配的事忧心。";
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase)) return "家族因" + (candidate.RevengeReasonText ?? "近来的风波") + "承受压力" + (string.IsNullOrWhiteSpace(candidate.RevengeTargetName) ? "。" : "，矛头指向" + candidate.RevengeTargetName + "。 ");
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase)) return (candidate.FiefProblemName ?? "一处封地") + "正受" + (candidate.FiefIssueText ?? "治理困境") + "困扰。";
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase)) return "家族在王国内显得孤立，正需要可信的盟友。";
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)) return "王国正缺能立刻上阵的可靠人手。";
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)) return "王国需要愿意长期分担责任的家族。";
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase)) return "王国内有一件议事正需要有人表态。";
		if (string.Equals(needType, NeedPolicySupport, StringComparison.OrdinalIgnoreCase)) return "他一直主张《" + (candidate.PolicySupportPolicyName ?? "某项政策") + "》。";
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase)) return "两国之间有一件事值得尽早谈一谈。";
		return "";
	}

	private static string GetNeedDisplayName(string needType)
	{
		if (string.Equals(needType, NeedFoodShortage, StringComparison.OrdinalIgnoreCase)) return "缺粮";
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase)) return "缺钱";
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase)) return "缺兵";
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase)) return "俘虏过载或赎买";
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase)) return "家族成员被俘";
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase)) return "队伍士气低落";
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase)) return "缺少坐骑";
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase)) return "负重压力";
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase)) return "家族财政紧张";
		if (string.Equals(needType, NeedClanService, StringComparison.OrdinalIgnoreCase)) return "家族请求效力";
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase)) return "亲密互动";
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase)) return "主动问候";
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase)) return "讨论近来政策";
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase)) return "主动交友";
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase)) return "主动求爱";
		if (string.Equals(needType, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase)) return "请求加入军团";
		if (string.Equals(needType, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase)) return "请求剿匪";
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase)) return "压制政敌";
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase)) return "购买封地";
		if (string.Equals(needType, NeedSettlementSale, StringComparison.OrdinalIgnoreCase)) return "出售边境封地";
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase)) return "领地盘问";
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase)) return "联姻压力";
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase)) return "复仇或营救压力";
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase)) return "封地治理压力";
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase)) return "缺少盟友支持";
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)) return "王国缺少雇佣兵";
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)) return "王国缺少封臣";
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase)) return "王国政治议程";
		if (string.Equals(needType, NeedPolicySupport, StringComparison.OrdinalIgnoreCase)) return "支持政策";
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase)) return "外交谈判";
		return string.IsNullOrWhiteSpace(needType) ? "具体请求" : needType;
	}

	private bool TryBuildBaseCandidate(MobileParty party, MobileParty mainParty, DuelSettings settings, out ProactiveCandidate candidate, out string skipReason)
	{
		candidate = null;
		skipReason = "";
		try
		{
			if (party == null || mainParty == null || party == mainParty || !party.IsActive || !party.IsVisible || !party.IsLordParty)
			{
				skipReason = "not_active_visible_lord_party";
				return false;
			}
			Hero hero = party.LeaderHero;
			if (hero == null || hero == Hero.MainHero || !hero.IsLord || hero.IsPrisoner || hero.IsDead || hero.Clan == null)
			{
				skipReason = "leader_invalid";
				return false;
			}
			if (party.MapEvent != null || party.CurrentSettlement != null || party.Army != null || party.BesiegedSettlement != null || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				skipReason = "party_busy_or_invalid_location";
				return false;
			}
			if (TryGetPlayerNativeActivityBusyReason(mainParty, out string mainBusyReason))
			{
				skipReason = "main_party_" + mainBusyReason;
				return false;
			}
			if (mainParty.MapEvent != null || mainParty.CurrentSettlement != null || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
			{
				skipReason = "main_party_busy_or_invalid_location";
				return false;
			}
			if (party.MapFaction == null || mainParty.MapFaction == null)
			{
				skipReason = "missing_faction";
				return false;
			}
			bool atWarWithPlayer = party.MapFaction.IsAtWarWith(mainParty.MapFaction);
			string heroKey = GetHeroKey(hero);
			float nowDays = NowDays();
			if (IsOnCooldown(_heroCooldownUntilDays, heroKey, nowDays))
			{
				skipReason = "hero_cooldown";
				return false;
			}
			Kingdom targetKingdom = ResolveHeroKingdom(hero);
			if (atWarWithPlayer && !CanBuildWartimeDiplomacyCandidate(hero, targetKingdom))
			{
				skipReason = "war_non_diplomacy";
				return false;
			}
			int foodDays = SafeFoodDays(party);
			int partyGold = SafePartyTradeGold(party);
			int totalWage = SafeTotalWage(party);
			float unpaidWages = SafeUnpaidWages(party);
			int memberCount = SafeMemberCount(party);
			int partySizeLimit = SafePartySizeLimit(party);
			int availableWageBudget = SafeAvailableWageBudget(party);
			int prisonerCount = SafePrisonerCount(party);
			int prisonerSizeLimit = SafePrisonerSizeLimit(party);
			int heroPrisonerCount = SafeHeroPrisonerCount(party);
			float morale = SafeMorale(party);
			int inventoryCapacity = SafeInventoryCapacity(party);
			float totalWeightCarried = SafeTotalWeightCarried(party);
			int mountCount = SafeMountCount(party);
			int packAnimalCount = SafePackAnimalCount(party);
			int clanGold = SafeClanGold(hero.Clan);
			int clanDebtToKingdom = SafeClanDebtToKingdom(hero.Clan);
			ClanCaptiveSnapshot captiveSnapshot = GetCachedClanCaptiveSnapshot(hero);
			MarriageAllianceSnapshot marriageSnapshot = BuildMarriageAllianceSnapshot(hero);
			FiefGovernanceSnapshot fiefGovernanceSnapshot = GetCachedFiefGovernanceSnapshot(hero.Clan, settings);
			AllySupportSnapshot allySupportSnapshot = GetCachedAllySupportSnapshot(hero.Clan, targetKingdom, settings);
			RevengePressureSnapshot revengeSnapshot = BuildRevengePressureSnapshot(hero, targetKingdom, captiveSnapshot, fiefGovernanceSnapshot);
			KingdomManpowerNeedSnapshot kingdomNeed = GetCachedKingdomManpowerNeedSnapshot(targetKingdom);
			int playerClanTier = SafePlayerClanTier();
			bool targetHeroIsKingdomLeader = IsKingdomLeader(hero, targetKingdom);
			bool targetClanCanOfferKingdomService = CanClanRepresentKingdom(hero.Clan, targetKingdom);
			float distance = GetDistanceToMainParty(party, mainParty);
			candidate = new ProactiveCandidate
			{
				Party = party,
				Hero = hero,
				Distance = distance,
				FoodDays = foodDays,
				PartyGold = partyGold,
				TotalWage = totalWage,
				UnpaidWages = unpaidWages,
				WageDays = CalculateWageDays(partyGold, totalWage),
				MemberCount = memberCount,
				PartySizeLimit = partySizeLimit,
				PartySizeRatio = CalculatePartySizeRatio(memberCount, partySizeLimit),
				AvailableWageBudget = availableWageBudget,
				PrisonerCount = prisonerCount,
				PrisonerSizeLimit = prisonerSizeLimit,
				HeroPrisonerCount = heroPrisonerCount,
				PrisonerSizeRatio = CalculatePrisonerSizeRatio(prisonerCount, prisonerSizeLimit),
				Morale = morale,
				InventoryCapacity = inventoryCapacity,
				TotalWeightCarried = totalWeightCarried,
				CarryRatio = CalculateCarryRatio(totalWeightCarried, inventoryCapacity),
				MountCount = mountCount,
				PackAnimalCount = packAnimalCount,
				MountRatio = CalculateAnimalRatio(mountCount, memberCount),
				PackAnimalRatio = CalculateAnimalRatio(packAnimalCount, memberCount),
				ClanGold = clanGold,
				ClanDebtToKingdom = clanDebtToKingdom,
				CaptiveClanHeroCount = captiveSnapshot.Count,
				CaptiveClanHeroName = captiveSnapshot.FirstHeroName,
				CaptiveClanHeroHolderName = captiveSnapshot.FirstHolderName,
				CaptiveClanLeaderHeld = captiveSnapshot.LeaderHeld,
				MarriageAdultClanHeroCount = marriageSnapshot.AdultClanHeroCount,
				MarriageUnmarriedAdultCount = marriageSnapshot.UnmarriedAdultCount,
				MarriageFirstUnmarriedName = marriageSnapshot.FirstUnmarriedName,
				MarriageRequesterUnmarried = marriageSnapshot.RequesterUnmarried,
				RevengePressureScore = revengeSnapshot.PressureScore,
				RevengeTargetName = revengeSnapshot.TargetName,
				RevengeReasonText = revengeSnapshot.ReasonText,
				FiefProblemCount = fiefGovernanceSnapshot.ProblemCount,
				FiefProblemName = fiefGovernanceSnapshot.FirstProblemName,
				FiefLoyalty = fiefGovernanceSnapshot.LowestLoyalty,
				FiefSecurity = fiefGovernanceSnapshot.LowestSecurity,
				FiefGarrisonCount = fiefGovernanceSnapshot.LowestGarrisonCount,
				FiefIssueText = fiefGovernanceSnapshot.IssueText,
				FiefUnderAttack = fiefGovernanceSnapshot.UnderAttack,
				ClanInfluence = allySupportSnapshot.ClanInfluence,
				FriendlyClanCount = allySupportSnapshot.FriendlyClanCount,
				HostileClanCount = allySupportSnapshot.HostileClanCount,
				TargetKingdom = targetKingdom,
				TargetKingdomId = GetKingdomKey(targetKingdom),
				TargetKingdomName = GetKingdomName(targetKingdom),
				PlayerClanTier = playerClanTier,
				TargetHeroIsKingdomLeader = targetHeroIsKingdomLeader,
				TargetClanCanOfferKingdomService = targetClanCanOfferKingdomService,
				KingdomFormalVassalClanCount = kingdomNeed.FormalVassalClanCount,
				KingdomMercenaryClanCount = kingdomNeed.MercenaryClanCount,
				KingdomFiefScore = kingdomNeed.FiefScore,
				KingdomWarKingdomCount = kingdomNeed.WarKingdomCount,
				KingdomPowerRatioToEnemies = kingdomNeed.PowerRatioToEnemies,
				KingdomTargetMercenaryClanCount = kingdomNeed.TargetMercenaryClanCount,
				KingdomTargetVassalClanCount = kingdomNeed.TargetVassalClanCount,
				KingdomNeedsMercenaries = kingdomNeed.NeedsMercenaries,
				KingdomNeedsVassals = kingdomNeed.NeedsVassals,
				KingdomMercenaryNeedUrgency = kingdomNeed.MercenaryNeedUrgency,
				KingdomVassalNeedUrgency = kingdomNeed.VassalNeedUrgency,
				AtWarWithPlayer = atWarWithPlayer
			};
			return true;
		}
		catch
		{
			skipReason = "exception";
			return false;
		}
	}

	private ProactiveCandidate TryBuildNeedCandidate(ProactiveCandidate source, DuelSettings settings, string needType, float urgency)
	{
		if (source == null || string.IsNullOrWhiteSpace(needType))
		{
			return null;
		}
		if (CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(needType))
		{
			return null;
		}
		if (!IsPlayerEligibleForProactiveNeed(source, needType, out string ineligibleReason))
		{
			Logger.LogVerbose("ProactiveNpcRequest", "need_player_ineligible", () => "need skipped because player is ineligible. need=" + needType + " hero=" + (source.Hero?.StringId ?? "") + " reason=" + ineligibleReason, 30.0);
			return null;
		}
		return new ProactiveCandidate
		{
			Party = source.Party,
			Hero = source.Hero,
			Distance = source.Distance,
			FoodDays = source.FoodDays,
			PartyGold = source.PartyGold,
			TotalWage = source.TotalWage,
			UnpaidWages = source.UnpaidWages,
			WageDays = source.WageDays,
			MemberCount = source.MemberCount,
			PartySizeLimit = source.PartySizeLimit,
			PartySizeRatio = source.PartySizeRatio,
			AvailableWageBudget = source.AvailableWageBudget,
			PrisonerCount = source.PrisonerCount,
			PrisonerSizeLimit = source.PrisonerSizeLimit,
			HeroPrisonerCount = source.HeroPrisonerCount,
			PrisonerSizeRatio = source.PrisonerSizeRatio,
			Morale = source.Morale,
			InventoryCapacity = source.InventoryCapacity,
			TotalWeightCarried = source.TotalWeightCarried,
			CarryRatio = source.CarryRatio,
			MountCount = source.MountCount,
			PackAnimalCount = source.PackAnimalCount,
			MountRatio = source.MountRatio,
			PackAnimalRatio = source.PackAnimalRatio,
			ClanGold = source.ClanGold,
			ClanDebtToKingdom = source.ClanDebtToKingdom,
			ClanServiceTargetClanName = source.ClanServiceTargetClanName,
			ClanServiceCurrentKingName = source.ClanServiceCurrentKingName,
			ClanServicePlayerRelation = source.ClanServicePlayerRelation,
			ClanServiceCurrentKingRelation = source.ClanServiceCurrentKingRelation,
			ClanServiceRelationGap = source.ClanServiceRelationGap,
			RomanticInteractionPrivateRelation = source.RomanticInteractionPrivateRelation,
			GreetingPrivateRelation = source.GreetingPrivateRelation,
			ArmyJoinRequestArmyName = source.ArmyJoinRequestArmyName,
			ArmyJoinRequestOwnStrength = source.ArmyJoinRequestOwnStrength,
			ArmyJoinRequestEnemyStrength = source.ArmyJoinRequestEnemyStrength,
			ArmyJoinRequestEnemyKingdomCount = source.ArmyJoinRequestEnemyKingdomCount,
			ArmyJoinRequestOwnToEnemyRatio = source.ArmyJoinRequestOwnToEnemyRatio,
			BanditSuppressionSettlementName = source.BanditSuppressionSettlementName,
			BanditSuppressionBanditCount = source.BanditSuppressionBanditCount,
			BanditSuppressionRadius = source.BanditSuppressionRadius,
			BanditSuppressionTrust = source.BanditSuppressionTrust,
			BanditSuppressionPrivateRelation = source.BanditSuppressionPrivateRelation,
			PoliticalRivalSuppressionKingdomName = source.PoliticalRivalSuppressionKingdomName,
			PoliticalRivalSuppressionRequesterClanName = source.PoliticalRivalSuppressionRequesterClanName,
			PoliticalRivalSuppressionPlayerClanRelation = source.PoliticalRivalSuppressionPlayerClanRelation,
			PoliticalRivalSuppressionRivalClanName = source.PoliticalRivalSuppressionRivalClanName,
			PoliticalRivalSuppressionRivalClanRelation = source.PoliticalRivalSuppressionRivalClanRelation,
			PolicySupportKingdomName = source.PolicySupportKingdomName,
			PolicySupportPlayerClanRelation = source.PolicySupportPlayerClanRelation,
			PolicySupportPolicyName = source.PolicySupportPolicyName,
			PolicySupportDescription = source.PolicySupportDescription,
			PolicySupportEffects = source.PolicySupportEffects,
			PolicySupportScore = source.PolicySupportScore,
			PolicySupportHasPendingDecision = source.PolicySupportHasPendingDecision,
			PolicyDiscussionPolicyId = source.PolicyDiscussionPolicyId,
			PolicyDiscussionPolicyName = source.PolicyDiscussionPolicyName,
			PolicyDiscussionPolicyContent = source.PolicyDiscussionPolicyContent,
			PolicyDiscussionKingdomName = source.PolicyDiscussionKingdomName,
			PolicyDiscussionPublishedDay = source.PolicyDiscussionPublishedDay,
			SettlementPurchaseKingdomName = source.SettlementPurchaseKingdomName,
			SettlementPurchasePlayerTownCount = source.SettlementPurchasePlayerTownCount,
			SettlementPurchasePlayerCastleCount = source.SettlementPurchasePlayerCastleCount,
			SettlementPurchasePlayerFiefsText = source.SettlementPurchasePlayerFiefsText,
			SettlementPurchaseNpcFiefCount = source.SettlementPurchaseNpcFiefCount,
			SettlementPurchaseNpcTownCount = source.SettlementPurchaseNpcTownCount,
			SettlementPurchaseNpcCastleCount = source.SettlementPurchaseNpcCastleCount,
			SettlementSaleKingdomName = source.SettlementSaleKingdomName,
			SettlementSalePlayerClanRelation = source.SettlementSalePlayerClanRelation,
			SettlementSaleNpcFiefCount = source.SettlementSaleNpcFiefCount,
			SettlementSaleTargetSettlementName = source.SettlementSaleTargetSettlementName,
			SettlementSaleTargetSettlementType = source.SettlementSaleTargetSettlementType,
			SettlementSaleTargetDailyIncome = source.SettlementSaleTargetDailyIncome,
			SettlementSaleHighestFamilyDailyIncome = source.SettlementSaleHighestFamilyDailyIncome,
			SettlementSaleForeignSettlementName = source.SettlementSaleForeignSettlementName,
			SettlementSaleForeignFactionName = source.SettlementSaleForeignFactionName,
			SettlementSaleBorderDistance = source.SettlementSaleBorderDistance,
			SettlementSaleBorderRadius = source.SettlementSaleBorderRadius,
			TerritorialInterrogationEligible = source.TerritorialInterrogationEligible,
			TerritorialInterrogationKingdomName = source.TerritorialInterrogationKingdomName,
			TerritorialInterrogationSettlementName = source.TerritorialInterrogationSettlementName,
			TerritorialInterrogationSettlementDistance = source.TerritorialInterrogationSettlementDistance,
			TerritorialInterrogationNpcCultureName = source.TerritorialInterrogationNpcCultureName,
			TerritorialInterrogationCultureNotoriety = source.TerritorialInterrogationCultureNotoriety,
			CaptiveClanHeroCount = source.CaptiveClanHeroCount,
			CaptiveClanHeroName = source.CaptiveClanHeroName,
			CaptiveClanHeroHolderName = source.CaptiveClanHeroHolderName,
			CaptiveClanLeaderHeld = source.CaptiveClanLeaderHeld,
			MarriageAdultClanHeroCount = source.MarriageAdultClanHeroCount,
			MarriageUnmarriedAdultCount = source.MarriageUnmarriedAdultCount,
			MarriageFirstUnmarriedName = source.MarriageFirstUnmarriedName,
			MarriageRequesterUnmarried = source.MarriageRequesterUnmarried,
			RevengePressureScore = source.RevengePressureScore,
			RevengeTargetName = source.RevengeTargetName,
			RevengeReasonText = source.RevengeReasonText,
			FiefProblemCount = source.FiefProblemCount,
			FiefProblemName = source.FiefProblemName,
			FiefLoyalty = source.FiefLoyalty,
			FiefSecurity = source.FiefSecurity,
			FiefGarrisonCount = source.FiefGarrisonCount,
			FiefIssueText = source.FiefIssueText,
			FiefUnderAttack = source.FiefUnderAttack,
			ClanInfluence = source.ClanInfluence,
			FriendlyClanCount = source.FriendlyClanCount,
			HostileClanCount = source.HostileClanCount,
			TargetKingdom = source.TargetKingdom,
			TargetKingdomId = source.TargetKingdomId,
			TargetKingdomName = source.TargetKingdomName,
			PlayerClanTier = source.PlayerClanTier,
			TargetHeroIsKingdomLeader = source.TargetHeroIsKingdomLeader,
			TargetClanCanOfferKingdomService = source.TargetClanCanOfferKingdomService,
			KingdomFormalVassalClanCount = source.KingdomFormalVassalClanCount,
			KingdomMercenaryClanCount = source.KingdomMercenaryClanCount,
			KingdomFiefScore = source.KingdomFiefScore,
			KingdomWarKingdomCount = source.KingdomWarKingdomCount,
			KingdomPowerRatioToEnemies = source.KingdomPowerRatioToEnemies,
			KingdomTargetMercenaryClanCount = source.KingdomTargetMercenaryClanCount,
			KingdomTargetVassalClanCount = source.KingdomTargetVassalClanCount,
			KingdomNeedsMercenaries = source.KingdomNeedsMercenaries,
			KingdomNeedsVassals = source.KingdomNeedsVassals,
			KingdomMercenaryNeedUrgency = source.KingdomMercenaryNeedUrgency,
			KingdomVassalNeedUrgency = source.KingdomVassalNeedUrgency,
			AtWarWithPlayer = source.AtWarWithPlayer,
			NeedType = needType,
			NeedTypes = new List<string> { needType },
			NeedUrgency = urgency,
			IsTestFallback = source.IsTestFallback
		};
	}

	private static bool CanBuildWartimeDiplomacyCandidate(Hero hero, Kingdom targetKingdom)
	{
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			return hero != null
				&& targetKingdom != null
				&& playerKingdom != null
				&& !playerKingdom.IsEliminated
				&& hero == targetKingdom.RulingClan?.Leader
				&& Hero.MainHero == playerKingdom.RulingClan?.Leader
				&& targetKingdom != playerKingdom;
		}
		catch
		{
			return false;
		}
	}

	private ProactiveCandidate BuildCombinedNeedCandidate(List<ProactiveCandidate> needCandidates, DuelSettings settings)
	{
		if (needCandidates == null || needCandidates.Count <= 0)
		{
			return null;
		}
		float nowDays = NowDays();
		foreach (ProactiveCandidate candidate in needCandidates.Where(c => c != null))
		{
			candidate.NeedTypeFatigueRemainingDays = GetNeedTypeFatigueRemainingDays(candidate.NeedType, nowDays);
			candidate.NeedTypeFatigueMultiplier = candidate.NeedTypeFatigueRemainingDays > 0f
				? GetEffectiveNeedTypeFatigueMultiplier(settings)
				: 1f;
			candidate.NeedTypeWeightMultiplier = Clamp(GetEffectiveNeedTypeWeightMultiplier(candidate.NeedType, settings)
				* Clamp(candidate.IntrinsicNeedTypeWeightMultiplier, 0f, 1f), 0f, 1f);
		}
		List<ProactiveCandidate> ordered = needCandidates
			.Where(c => c != null && !string.IsNullOrWhiteSpace(c.NeedType) && IsPlayerEligibleForProactiveNeed(c, c.NeedType, out _))
			.OrderByDescending(GetCandidateWeightedUrgency)
			.ThenByDescending(c => c.NeedUrgency)
			.ThenByDescending(c => GetNeedPresentationPriority(c.NeedType))
			.ToList();
		if (ordered.Count <= 0)
		{
			return null;
		}
		ProactiveCandidate selected = ordered[0];
		string selectedNeedType = NormalizeNeedType(selected.NeedType);
		if (string.IsNullOrWhiteSpace(selectedNeedType))
		{
			return null;
		}
		selected.NeedTypes = new List<string> { selectedNeedType };
		selected.NeedType = selectedNeedType;
		selected.NeedUrgency = Clamp(selected.NeedUrgency, 0f, 100f);
		return selected;
	}

	private static List<string> FilterPlayerEligibleNeedTypes(ProactiveCandidate candidate, IEnumerable<string> needTypes, string fallbackNeedType)
	{
		List<string> raw = new List<string>();
		try
		{
			if (needTypes != null)
			{
				foreach (string needType in needTypes)
				{
					string normalized = NormalizeNeedType(needType);
					if (!string.IsNullOrWhiteSpace(normalized) && !raw.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
					{
						raw.Add(normalized);
					}
				}
			}
			string fallback = NormalizeNeedType(fallbackNeedType);
			if (raw.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
			{
				raw.Add(fallback);
			}
			List<string> eligible = raw
				.Where(needType => IsPlayerEligibleForProactiveNeed(candidate, needType, out _))
				.ToList();
			return eligible.Count <= 0 ? new List<string>() : NormalizeSingleNeedType(eligible, eligible[0]);
		}
		catch
		{
			return new List<string>();
		}
	}

	private static bool IsPlayerEligibleForProactiveNeed(ProactiveCandidate candidate, string needType, out string reason)
	{
		reason = "";
		string normalized = NormalizeNeedType(needType);
		if (candidate == null || string.IsNullOrWhiteSpace(normalized))
		{
			reason = "candidate_or_need_invalid";
			return false;
		}
		try
		{
			if (string.Equals(normalized, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForMercenaryInvite(candidate, out reason);
			}
			if (string.Equals(normalized, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForVassalInvite(candidate, out reason);
			}
			if (string.Equals(normalized, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForPoliticalAgendaRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedPolicySupport, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForPolicySupport(candidate, out reason);
			}
			if (string.Equals(normalized, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForDiplomacyRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForMarriageAllianceRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedClanService, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForClanServiceRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForRomanticInteraction(candidate, out reason);
			}
			if (string.Equals(normalized, NeedGreeting, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForGreeting(candidate, out reason);
			}
			if (string.Equals(normalized, NeedFriendship, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForFriendship(candidate, out reason);
			}
			if (string.Equals(normalized, NeedCourtship, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForCourtship(candidate, out reason);
			}
			if (string.Equals(normalized, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForArmyJoinRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForBanditSuppression(candidate, out reason);
			}
			if (string.Equals(normalized, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForPoliticalRivalSuppression(candidate, out reason);
			}
			if (string.Equals(normalized, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForSettlementPurchase(candidate, out reason);
			}
			if (string.Equals(normalized, NeedSettlementSale, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForSettlementSale(candidate, out reason);
			}
			if (string.Equals(normalized, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForTerritorialInterrogation(candidate, out reason);
			}
			if (string.Equals(normalized, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForAllySupportRequest(candidate, out reason);
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return false;
		}
	}

	private static bool IsPlayerEligibleForMercenaryInvite(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			reason = "player_clan_missing";
			return false;
		}
		if (candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.KingdomNeedsMercenaries)
		{
			reason = "target_cannot_offer_mercenary";
			return false;
		}
		if (candidate.PlayerClanTier < MercenaryInviteMinPlayerClanTier)
		{
			reason = "player_tier_below_mercenary";
			return false;
		}
		if (!HasMinimumTrustForKingdomServiceInvite(candidate.Hero))
		{
			reason = "npc_trust_below_kingdom_service_threshold";
			return false;
		}
		if (playerClan.Kingdom != null || playerClan.IsUnderMercenaryService)
		{
			reason = "player_already_serving";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForVassalInvite(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			reason = "player_clan_missing";
			return false;
		}
		if (candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.TargetHeroIsKingdomLeader || !candidate.KingdomNeedsVassals)
		{
			reason = "target_cannot_offer_vassalage";
			return false;
		}
		if (candidate.PlayerClanTier < VassalInviteMinPlayerClanTier)
		{
			reason = "player_tier_below_vassal";
			return false;
		}
		if (!HasMinimumTrustForKingdomServiceInvite(candidate.Hero))
		{
			reason = "npc_trust_below_kingdom_service_threshold";
			return false;
		}
		if (playerClan.Kingdom == null)
		{
			return true;
		}
		if (playerClan.IsUnderMercenaryService && playerClan.Kingdom == candidate.TargetKingdom)
		{
			return true;
		}
		reason = "player_not_independent_or_target_mercenary";
		return false;
	}

	private static bool HasMinimumTrustForKingdomServiceInvite(Hero hero)
	{
		try
		{
			int trust = Clamp(RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0, -100, 100);
			return trust >= KingdomServiceInviteMinNpcTrust;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerEligibleForPoliticalAgendaRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null || candidate.TargetKingdom == null)
		{
			reason = "player_or_kingdom_missing";
			return false;
		}
		if (playerClan.Kingdom != candidate.TargetKingdom || playerClan.IsUnderMercenaryService)
		{
			reason = "player_not_formal_member_of_target_kingdom";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForDiplomacyRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		if (playerKingdom == null || playerKingdom.IsEliminated)
		{
			reason = "player_kingdom_missing";
			return false;
		}
		if (Hero.MainHero != playerKingdom.RulingClan?.Leader)
		{
			reason = "player_not_kingdom_leader";
			return false;
		}
		if (candidate.TargetKingdom == null || candidate.TargetKingdom == playerKingdom || candidate.Hero != candidate.TargetKingdom.RulingClan?.Leader)
		{
			reason = "target_not_foreign_king";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForAllySupportRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null || candidate.TargetKingdom == null)
		{
			reason = "player_or_kingdom_missing";
			return false;
		}
		if (playerClan.Kingdom != candidate.TargetKingdom || playerClan.IsUnderMercenaryService)
		{
			reason = "player_cannot_vote_or_back_in_target_kingdom";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForMarriageAllianceRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan npcClan = candidate?.Hero?.Clan;
		Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
		if (npcClan == null || playerClan == null || npcClan == playerClan)
		{
			reason = "clan_invalid";
			return false;
		}
		List<Hero> npcCandidates = GetMarriageableClanHeroes(npcClan, includeMainHero: false);
		List<Hero> playerCandidates = GetMarriageableClanHeroes(playerClan, includeMainHero: true);
		if (npcCandidates.Count <= 0)
		{
			reason = "npc_clan_no_marriage_candidate";
			return false;
		}
		if (playerCandidates.Count <= 0)
		{
			reason = "player_clan_no_marriage_candidate";
			return false;
		}
		foreach (Hero npcHero in npcCandidates)
		{
			foreach (Hero playerHero in playerCandidates)
			{
				if (AreHeroesSuitableForMarriageRequest(npcHero, playerHero))
				{
					return true;
				}
			}
		}
		reason = "no_suitable_marriage_pair";
		return false;
	}

	private static bool IsPlayerEligibleForClanServiceRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildClanServiceNeedSnapshot(candidate, out _))
		{
			reason = "clan_service_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForPolicySupport(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		ProactiveNpcRequestBehavior instance = Instance;
		if (instance == null || !instance.TryBuildPolicySupportSnapshot(candidate, out _))
		{
			reason = "policy_support_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForRomanticInteraction(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsRomanticInteractionUnavailableForExternal())
		{
			reason = "romantic_interaction_global_cooldown";
			return false;
		}
		if (!IsRomanticInteractionEligible(candidate?.Hero, out _))
		{
			reason = "romantic_interaction_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForTerritorialInterrogation(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsTerritorialInterrogationOnCooldown())
		{
			reason = "territorial_interrogation_global_cooldown";
			return false;
		}
		if (candidate?.TerritorialInterrogationEligible == true)
		{
			return true;
		}
		if (!TryBuildTerritorialInterrogationSnapshot(candidate, DuelSettings.GetSettings(), null, out _))
		{
			reason = "territorial_interrogation_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForGreeting(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsGreetingOnCooldown())
		{
			reason = "greeting_global_cooldown";
			return false;
		}
		if (!IsGreetingEligible(candidate?.Hero, out _))
		{
			reason = "greeting_private_relation_too_low";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForFriendship(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsFriendshipOnCooldown())
		{
			reason = "friendship_global_cooldown";
			return false;
		}
		if (!TryBuildFriendshipNeedSnapshot(candidate, out _))
		{
			reason = "friendship_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForCourtship(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsCourtshipOnCooldown())
		{
			reason = "courtship_global_cooldown";
			return false;
		}
		if (!TryBuildCourtshipNeedSnapshot(candidate, out _))
		{
			reason = "courtship_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForArmyJoinRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildArmyJoinRequestSnapshot(candidate, out _))
		{
			reason = "army_join_request_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForBanditSuppression(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		ProactiveNpcRequestBehavior instance = Instance;
		if (instance == null || !instance.TryBuildBanditSuppressionSnapshot(candidate, out _))
		{
			reason = "bandit_suppression_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForPoliticalRivalSuppression(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildPoliticalRivalSuppressionSnapshot(candidate, out _))
		{
			reason = "political_rival_suppression_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForSettlementPurchase(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildSettlementPurchaseSnapshot(candidate, out _))
		{
			reason = "settlement_purchase_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForSettlementSale(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		ProactiveNpcRequestBehavior instance = Instance;
		if (instance == null || !instance.TryBuildSettlementSaleSnapshot(candidate, out _))
		{
			reason = "settlement_sale_conditions_not_met";
			return false;
		}
		return true;
	}

	private static List<Hero> GetMarriageableClanHeroes(Clan clan, bool includeMainHero)
	{
		List<Hero> result = new List<Hero>();
		try
		{
			if (clan?.Heroes != null)
			{
				foreach (Hero hero in clan.Heroes)
				{
					AddMarriageableHero(result, hero, clan);
				}
			}
			if (includeMainHero)
			{
				AddMarriageableHero(result, Hero.MainHero, clan);
			}
		}
		catch
		{
		}
		return result;
	}

	private static void AddMarriageableHero(List<Hero> result, Hero hero, Clan clan)
	{
		try
		{
			if (result == null || hero == null || clan == null || hero.Clan != clan || hero.IsDead || hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
			{
				return;
			}
			string heroKey = GetHeroKey(hero);
			if (result.Any(h => string.Equals(GetHeroKey(h), heroKey, StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}
			if (!hero.CanMarry())
			{
				return;
			}
			result.Add(hero);
		}
		catch
		{
		}
	}

	private static bool AreHeroesSuitableForMarriageRequest(Hero left, Hero right)
	{
		try
		{
			return left != null
				&& right != null
				&& left != right
				&& left.Clan != null
				&& right.Clan != null
				&& left.Clan != right.Clan
				&& Campaign.Current?.Models?.MarriageModel?.IsCoupleSuitableForMarriage(left, right) == true;
		}
		catch
		{
			return false;
		}
	}

	private bool TryBuildFoodShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| !IsFoodShortageNeedMet(source.Party, source.FoodDays, settings, out float urgency)
			|| !DoesPlayerHaveFoodForFoodRequest())
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedFoodShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildMoneyShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsMoneyShortageNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedMoneyShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildTroopShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| !IsTroopShortageNeedMet(source, settings, out float urgency)
			|| !DoesPlayerHaveTroopsForTroopRequest())
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedTroopShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildPrisonerOverloadCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsPrisonerOverloadNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedPrisonerOverload, urgency);
		return candidate != null;
	}

	private bool TryBuildClanCaptiveCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsClanCaptiveNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedClanCaptive, urgency);
		return candidate != null;
	}

	private bool TryBuildLowMoraleCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsLowMoraleNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedLowMorale, urgency);
		return candidate != null;
	}

	private bool TryBuildMountShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| !IsMountShortageNeedMet(source, settings, out float urgency)
			|| !HasPlayerSurplusMountsCausingHerdPenalty())
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedMountShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildOverburdenedCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsOverburdenedNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedOverburdened, urgency);
		return candidate != null;
	}

	private bool TryBuildClanFinanceStrainCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsClanFinanceStrainNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedClanFinanceStrain, urgency);
		return candidate != null;
	}

	private bool TryBuildClanServiceCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildClanServiceNeedSnapshot(source, out ClanServiceNeedSnapshot snapshot))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Min(25f, Math.Max(0f, snapshot.RelationGap - 40) * 0.6f), 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedClanService, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.ClanServiceTargetClanName = snapshot.TargetClanName;
		candidate.ClanServiceCurrentKingName = snapshot.CurrentKingName;
		candidate.ClanServicePlayerRelation = snapshot.PlayerRelation;
		candidate.ClanServiceCurrentKingRelation = snapshot.CurrentKingRelation;
		candidate.ClanServiceRelationGap = snapshot.RelationGap;
		return true;
	}

	private bool TryBuildRomanticInteractionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsRomanticInteractionUnavailableForExternal() || !IsRomanticInteractionEligible(source.Hero, out int privateRelation))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Max(0, privateRelation - 30) * 0.4f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedRomanticInteraction, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.RomanticInteractionPrivateRelation = privateRelation;
		return true;
	}

	private bool TryBuildTerritorialInterrogationCandidate(ProactiveCandidate source, DuelSettings settings, Dictionary<string, TerritorialSettlementSnapshot> territorialSettlementSnapshots, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsTerritorialInterrogationOnCooldown()
			|| !TryBuildTerritorialInterrogationSnapshot(source, settings, territorialSettlementSnapshots, out TerritorialInterrogationSnapshot snapshot))
		{
			return false;
		}
		source.TerritorialInterrogationEligible = true;
		source.TerritorialInterrogationKingdomName = snapshot.KingdomName;
		source.TerritorialInterrogationSettlementName = snapshot.SettlementName;
		source.TerritorialInterrogationSettlementDistance = snapshot.SettlementDistance;
		source.TerritorialInterrogationNpcCultureName = snapshot.NpcCultureName;
		source.TerritorialInterrogationCultureNotoriety = snapshot.CultureNotoriety;
		float range = Math.Max(1f, MobileParty.MainParty?.SeeingRange ?? 1f)
			* Clamp(settings?.ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier ?? 3f, 0.5f, 10f);
		float proximityUrgency = Clamp(1f - snapshot.SettlementDistance / Math.Max(1f, range), 0f, 1f) * 20f;
		float urgency = Clamp(60f + proximityUrgency, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedTerritorialInterrogation, urgency);
		return candidate != null;
	}

	private bool TryBuildGreetingCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsGreetingUnavailableForExternal() || !IsGreetingEligible(source.Hero, out int privateRelation))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Max(0, privateRelation - 20) * 0.25f, 0f, 82.5f);
		candidate = TryBuildNeedCandidate(source, settings, NeedGreeting, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.GreetingPrivateRelation = privateRelation;
		return true;
	}

	private bool TryBuildPolicyDiscussionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsPolicyDiscussionUnavailableForExternal()
			|| !IsGreetingEligible(source.Hero, out int privateRelation)
			|| !TryGetRecentPolicyDiscussionSnapshot(out PolicyDiscussionSnapshot snapshot))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Max(0, privateRelation - 20) * 0.25f, 0f, 82.5f);
		candidate = TryBuildNeedCandidate(source, settings, NeedPolicyDiscussion, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.GreetingPrivateRelation = privateRelation;
		candidate.PolicyDiscussionPolicyId = snapshot.PolicyId;
		candidate.PolicyDiscussionPolicyName = snapshot.PolicyName;
		candidate.PolicyDiscussionPolicyContent = snapshot.PolicyContent;
		candidate.PolicyDiscussionKingdomName = snapshot.KingdomName;
		candidate.PolicyDiscussionPublishedDay = snapshot.PublishedDay;
		return true;
	}

	private bool TryBuildPolicyDiscussionCompanionMotive(Hero hero, out string factText, out string intentText, out float urgency)
	{
		factText = "";
		intentText = "";
		urgency = 0f;
		if (IsPolicyDiscussionUnavailableForExternal()
			|| !IsGreetingEligible(hero, out int privateRelation)
			|| !TryGetRecentPolicyDiscussionSnapshot(out PolicyDiscussionSnapshot snapshot))
		{
			return false;
		}
		urgency = Clamp(60f + Math.Max(0, privateRelation - 20) * 0.25f, 0f, 82.5f);
		factText = BuildPolicyDiscussionSituation(snapshot, GetHeroDisplayName(hero), MyBehavior.BuildPlayerPublicDisplayNameForExternal(hero));
		intentText = AIConfigHandler.GetProactiveNpcRequestCompanionIntent(NeedPolicyDiscussion);
		return !string.IsNullOrWhiteSpace(factText) && !string.IsNullOrWhiteSpace(intentText);
	}

	private bool TryGetRecentPolicyDiscussionSnapshot(out PolicyDiscussionSnapshot snapshot)
	{
		snapshot = null;
		float cacheHour = (float)Math.Floor(NowHours());
		if (_policyDiscussionSnapshotCache != null
			&& Math.Abs(_policyDiscussionSnapshotCache.SampledAtHour - cacheHour) < 0.01f)
		{
			snapshot = _policyDiscussionSnapshotCache.Snapshot;
			return snapshot != null;
		}
		try
		{
			NpcRulerPolicyRecord record = NpcRulerPolicyBehavior.GetRecentPolicyRecordsForExternal(maxCount: 1).FirstOrDefault();
			string policyName = (record?.PolicyName ?? "").Trim();
			string policyContent = (record?.PolicyContent ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(policyName) && !string.IsNullOrWhiteSpace(policyContent))
			{
				snapshot = new PolicyDiscussionSnapshot
				{
					PolicyId = (record.PolicyId ?? "").Trim(),
					PolicyName = policyName,
					PolicyContent = policyContent,
					KingdomName = (record.KingdomName ?? "").Trim(),
					PublishedDay = Math.Max(0, record.Day)
				};
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "recent custom policy snapshot failed: " + ex.Message);
		}
		_policyDiscussionSnapshotCache = new PolicyDiscussionSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		return snapshot != null;
	}

	private static string BuildPolicyDiscussionSituation(PolicyDiscussionSnapshot snapshot, string npcName, string playerName)
	{
		if (snapshot == null)
		{
			return "";
		}
		string policyName = (snapshot.PolicyName ?? "").Trim();
		string policyContent = (snapshot.PolicyContent ?? "").Trim();
		string targetName = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
		if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(policyContent))
		{
			return "";
		}
		return "近来公布了一项政策《" + policyName + "》。政策全文如下：\n" + policyContent
			+ "\n你想与" + targetName + "谈谈自己对这项政策的看法。";
	}

	private bool TryBuildFriendshipCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsFriendshipOnCooldown() || !TryBuildFriendshipNeedSnapshot(source, out FriendshipNeedSnapshot snapshot))
		{
			return false;
		}
		float notorietyStrength = Clamp((snapshot.CultureNotoriety - 10) / 90f, 0f, 1f);
		float clanTierStrength = Clamp((snapshot.PlayerClanTier - 1) / 5f, 0f, 1f);
		float urgency = Clamp(50f + notorietyStrength * 30f + clanTierStrength * 20f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedFriendship, urgency);
		return candidate != null;
	}

	private bool TryBuildCourtshipCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsCourtshipOnCooldown() || !TryBuildCourtshipNeedSnapshot(source, out CourtshipNeedSnapshot snapshot))
		{
			return false;
		}
		float notorietyStrength = Clamp((snapshot.CultureNotoriety - 10) / 90f, 0f, 1f);
		float playerTierStrength = Clamp((snapshot.PlayerClanTier - 1) / 5f, 0f, 1f);
		float relativeClanTierAdjustment = Clamp((snapshot.PlayerClanTier - snapshot.NpcClanTier) * 5f, -25f, 30f);
		float urgency = Clamp(60f + notorietyStrength * 15f + playerTierStrength * 10f + relativeClanTierAdjustment, 50f, 90f);
		candidate = TryBuildNeedCandidate(source, settings, NeedCourtship, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.IntrinsicNeedTypeWeightMultiplier = snapshot.TriggerWeightMultiplier;
		return true;
	}

	private bool TryBuildArmyJoinRequestCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildArmyJoinRequestSnapshot(source, out ArmyJoinRequestSnapshot snapshot))
		{
			return false;
		}
		float shortage = Clamp((0.66f - snapshot.OwnToEnemyRatio) / 0.66f, 0f, 1f);
		float urgency = Clamp(65f + shortage * 25f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedArmyJoinRequest, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.ArmyJoinRequestArmyName = snapshot.ArmyName;
		candidate.ArmyJoinRequestOwnStrength = snapshot.OwnStrength;
		candidate.ArmyJoinRequestEnemyStrength = snapshot.EnemyStrength;
		candidate.ArmyJoinRequestEnemyKingdomCount = snapshot.EnemyKingdomCount;
		candidate.ArmyJoinRequestOwnToEnemyRatio = snapshot.OwnToEnemyRatio;
		return true;
	}

	private bool TryBuildBanditSuppressionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildBanditSuppressionSnapshot(source, out BanditSuppressionSnapshot snapshot))
		{
			return false;
		}
		float urgency = Clamp(65f + Math.Min(25f, Math.Max(0, snapshot.BanditCount - 9) * 3f), 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedBanditSuppression, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.BanditSuppressionSettlementName = snapshot.SettlementName;
		candidate.BanditSuppressionBanditCount = snapshot.BanditCount;
		candidate.BanditSuppressionRadius = snapshot.Radius;
		candidate.BanditSuppressionTrust = snapshot.Trust;
		candidate.BanditSuppressionPrivateRelation = snapshot.PrivateRelation;
		return true;
	}

	private bool TryBuildPoliticalRivalSuppressionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsPoliticalRivalSuppressionOnCooldown()
			|| !TryBuildPoliticalRivalSuppressionSnapshot(source, out PoliticalRivalSuppressionSnapshot snapshot))
		{
			return false;
		}
		float playerSupport = Clamp((snapshot.PlayerClanRelation - 20) / 80f, 0f, 1f);
		float rivalry = Clamp((-10 - snapshot.RivalClanRelation) / 90f, 0f, 1f);
		float urgency = Clamp(62f + playerSupport * 13f + rivalry * 15f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedPoliticalRivalSuppression, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.PoliticalRivalSuppressionKingdomName = snapshot.KingdomName;
		candidate.PoliticalRivalSuppressionRequesterClanName = snapshot.RequesterClanName;
		candidate.PoliticalRivalSuppressionPlayerClanRelation = snapshot.PlayerClanRelation;
		candidate.PoliticalRivalSuppressionRivalClanName = snapshot.RivalClanName;
		candidate.PoliticalRivalSuppressionRivalClanRelation = snapshot.RivalClanRelation;
		return true;
	}

	private bool TryBuildSettlementPurchaseCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsSettlementPurchaseOnCooldown()
			|| !TryBuildSettlementPurchaseSnapshot(source, out SettlementPurchaseSnapshot snapshot))
		{
			return false;
		}
		float playerFiefPressure = Clamp((snapshot.PlayerFiefCount - 3) / 7f, 0f, 1f);
		float npcFiefNeed = snapshot.NpcFiefCount <= 0 ? 1f : 0.55f;
		float urgency = Clamp(64f + playerFiefPressure * 16f + npcFiefNeed * 10f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedSettlementPurchase, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.SettlementPurchaseKingdomName = snapshot.KingdomName;
		candidate.SettlementPurchasePlayerTownCount = snapshot.PlayerTownCount;
		candidate.SettlementPurchasePlayerCastleCount = snapshot.PlayerCastleCount;
		candidate.SettlementPurchasePlayerFiefsText = snapshot.PlayerFiefsText;
		candidate.SettlementPurchaseNpcFiefCount = snapshot.NpcFiefCount;
		candidate.SettlementPurchaseNpcTownCount = snapshot.NpcTownCount;
		candidate.SettlementPurchaseNpcCastleCount = snapshot.NpcCastleCount;
		return true;
	}

	private bool TryBuildSettlementSaleCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildSettlementSaleSnapshot(source, out SettlementSaleSnapshot snapshot))
		{
			return false;
		}
		float relationStrength = Clamp((snapshot.PlayerClanRelation - 30) / 70f, 0f, 1f);
		float incomeGap = snapshot.HighestFamilyDailyIncome <= 0
			? 0f
			: Clamp((snapshot.HighestFamilyDailyIncome - snapshot.TargetDailyIncome) / (float)snapshot.HighestFamilyDailyIncome, 0f, 1f);
		float borderPressure = snapshot.BorderRadius <= 0f
			? 0f
			: Clamp(1f - snapshot.BorderDistance / snapshot.BorderRadius, 0f, 1f);
		float urgency = Clamp(58f + relationStrength * 12f + incomeGap * 17f + borderPressure * 13f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedSettlementSale, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.SettlementSaleKingdomName = snapshot.KingdomName;
		candidate.SettlementSalePlayerClanRelation = snapshot.PlayerClanRelation;
		candidate.SettlementSaleNpcFiefCount = snapshot.NpcFiefCount;
		candidate.SettlementSaleTargetSettlementName = snapshot.TargetSettlementName;
		candidate.SettlementSaleTargetSettlementType = snapshot.TargetSettlementType;
		candidate.SettlementSaleTargetDailyIncome = snapshot.TargetDailyIncome;
		candidate.SettlementSaleHighestFamilyDailyIncome = snapshot.HighestFamilyDailyIncome;
		candidate.SettlementSaleForeignSettlementName = snapshot.ForeignSettlementName;
		candidate.SettlementSaleForeignFactionName = snapshot.ForeignFactionName;
		candidate.SettlementSaleBorderDistance = snapshot.BorderDistance;
		candidate.SettlementSaleBorderRadius = snapshot.BorderRadius;
		return true;
	}

	private bool TryBuildMarriageAlliancePressureCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsMarriageAlliancePressureNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedMarriageAlliancePressure, urgency);
		return candidate != null;
	}

	private bool TryBuildRevengePressureCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsRevengePressureNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedRevengePressure, urgency);
		return candidate != null;
	}

	private bool TryBuildFiefGovernanceAnxietyCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsFiefGovernanceAnxietyNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedFiefGovernanceAnxiety, urgency);
		return candidate != null;
	}

	private bool TryBuildAllySupportCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsAllySupportNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedAllySupport, urgency);
		return candidate != null;
	}

	private bool TryBuildKingdomMercenaryInviteCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsKingdomMercenaryInviteNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedKingdomMercenaryInvite, urgency);
		return candidate != null;
	}

	private bool TryBuildKingdomVassalInviteCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsKingdomVassalInviteNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedKingdomVassalInvite, urgency);
		return candidate != null;
	}

	private bool TryBuildPoliticalAgendaCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsPoliticalAgendaNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedPoliticalAgenda, urgency);
		return candidate != null;
	}

	private bool TryBuildPolicySupportCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildPolicySupportSnapshot(source, out PolicySupportSnapshot snapshot))
		{
			return false;
		}
		float relationStrength = Clamp((snapshot.PlayerClanRelation - 30) / 70f, 0f, 1f);
		float supportStrength = Clamp((snapshot.SupportScore - 100f) / 150f, 0f, 1f);
		float pendingUrgency = snapshot.HasPendingDecision ? 12f : 0f;
		float urgency = Clamp(62f + relationStrength * 12f + supportStrength * 14f + pendingUrgency, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedPolicySupport, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.PolicySupportKingdomName = snapshot.KingdomName;
		candidate.PolicySupportPlayerClanRelation = snapshot.PlayerClanRelation;
		candidate.PolicySupportPolicyName = snapshot.PolicyName;
		candidate.PolicySupportDescription = snapshot.Description;
		candidate.PolicySupportEffects = snapshot.Effects;
		candidate.PolicySupportScore = snapshot.SupportScore;
		candidate.PolicySupportHasPendingDecision = snapshot.HasPendingDecision;
		return true;
	}

	private bool TryBuildDiplomacyCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsDiplomacyNeedMet(source, out float urgency))
			return false;
		candidate = TryBuildNeedCandidate(source, settings, NeedDiplomacy, urgency);
		return candidate != null;
	}

	private static bool IsDiplomacyNeedMet(ProactiveCandidate source, out float urgency)
	{
		urgency = 0f;
		try
		{
			Hero hero = source?.Hero;
			if (hero == null) return false;
			Kingdom npcKingdom = hero.Clan?.Kingdom;
			if (npcKingdom == null || hero != npcKingdom.RulingClan?.Leader) return false;
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated) return false;
			if (Hero.MainHero != playerKingdom.RulingClan?.Leader) return false;
			if (npcKingdom == playerKingdom) return false;
			bool atWar = FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom);
			if (atWar) { urgency = 55f; return true; }
			bool hasCommonEnemy = false;
			foreach (Kingdom k in Kingdom.All)
			{
				if (!k.IsEliminated && k != npcKingdom && k != playerKingdom
					&& FactionManager.IsAtWarAgainstFaction(npcKingdom, k)
					&& FactionManager.IsAtWarAgainstFaction(playerKingdom, k))
				{ hasCommonEnemy = true; break; }
			}
			if (hasCommonEnemy) { urgency = 65f; return true; }
			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			bool hasTrade = BannerlordApiCompat.HasTradeAgreement(tradeBeh, npcKingdom, playerKingdom);
			if (!hasTrade) { urgency = 45f; return true; }
			return false;
		}
		catch { urgency = 0f; return false; }
	}

	private static bool IsFoodShortageNeedMet(MobileParty party, int foodDays, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		int threshold = Clamp(settings?.ProactiveNpcRequestFoodDaysThreshold ?? 3, 0, 15);
		try
		{
			if (party?.Party?.IsStarving == true)
			{
				urgency = 100f;
				return true;
			}
		}
		catch
		{
		}
		if (foodDays <= threshold)
		{
			urgency = 80f + Math.Max(0, threshold - foodDays);
			return true;
		}
		return false;
	}

	private static bool IsPoliticalAgendaNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		try
		{
			Hero hero = candidate?.Hero;
			Clan npcClan = hero?.Clan;
			Kingdom kingdom = candidate?.TargetKingdom ?? npcClan?.Kingdom;
			Clan playerClan = Clan.PlayerClan;
			if (hero == null || npcClan == null || kingdom == null || playerClan == null)
			{
				return false;
			}
			if (npcClan.IsUnderMercenaryService || playerClan.IsUnderMercenaryService)
			{
				return false;
			}
			if (playerClan.Kingdom != kingdom)
			{
				return false;
			}
			if (hero != npcClan.Leader && hero != kingdom.RulingClan?.Leader)
			{
				return false;
			}
			int activeAgendaCount = CountActiveKingdomAgendas(kingdom);
			if (activeAgendaCount <= 0)
			{
				return false;
			}
			urgency = 52f + Math.Min(18f, activeAgendaCount * 4f);
			if (hero == kingdom.RulingClan?.Leader)
			{
				urgency += 6f;
			}
			return true;
		}
		catch
		{
			urgency = 0f;
			return false;
		}
	}

	private static int CountActiveKingdomAgendas(Kingdom kingdom)
	{
		try
		{
			if (kingdom?.UnresolvedDecisions == null)
			{
				return 0;
			}
			int count = 0;
			foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
			{
				if (decision == null)
				{
					continue;
				}
				try
				{
					if (decision.ShouldBeCancelled())
					{
						continue;
					}
				}
				catch
				{
				}
				count++;
			}
			return count;
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsMoneyShortageNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null)
		{
			return false;
		}
		if (candidate.UnpaidWages > 0f)
		{
			urgency = 95f + Clamp(candidate.UnpaidWages, 0f, 1f) * 5f;
			return true;
		}
		int goldThreshold = GetEffectiveMoneyGoldThreshold(settings);
		if (candidate.PartyGold < goldThreshold)
		{
			float deficitRatio = goldThreshold <= 0 ? 0f : Clamp((goldThreshold - candidate.PartyGold) / (float)goldThreshold, 0f, 1f);
			urgency = 70f + deficitRatio * 10f;
			return true;
		}
		int wageDaysThreshold = Clamp(settings?.ProactiveNpcRequestMoneyWageDaysThreshold ?? 3, 0, 30);
		if (wageDaysThreshold > 0 && candidate.TotalWage > 0 && candidate.WageDays <= wageDaysThreshold)
		{
			urgency = 60f + Clamp(wageDaysThreshold - candidate.WageDays, 0f, 30f);
			return true;
		}
		return false;
	}

	private static bool IsTroopShortageNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.PartySizeLimit <= 0 || candidate.MemberCount <= 0)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestTroopRatioThresholdPercent ?? 50, 1, 100);
		float thresholdRatio = thresholdPercent / 100f;
		if (candidate.PartySizeRatio > thresholdRatio)
		{
			return false;
		}
		float shortageRatio = Clamp(thresholdRatio - candidate.PartySizeRatio, 0f, 1f);
		urgency = 50f + shortageRatio * 25f;
		return true;
	}

	private static bool IsPrisonerOverloadNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.PrisonerCount <= 0 || candidate.PrisonerSizeLimit <= 0)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestPrisonerRatioThresholdPercent ?? 80, 1, 150);
		float thresholdRatio = thresholdPercent / 100f;
		float prisonerRatio = CalculatePrisonerSizeRatio(candidate.PrisonerCount, candidate.PrisonerSizeLimit);
		float heroBonus = Math.Min(9f, Math.Max(0, candidate.HeroPrisonerCount) * 3f);
		if (candidate.PrisonerCount > candidate.PrisonerSizeLimit)
		{
			float overflowRatio = Clamp((candidate.PrisonerCount - candidate.PrisonerSizeLimit) / (float)candidate.PrisonerSizeLimit, 0f, 1f);
			urgency = 90f + overflowRatio * 10f + heroBonus;
			return true;
		}
		if (prisonerRatio >= thresholdRatio)
		{
			float span = Math.Max(0.01f, 1f - Math.Min(thresholdRatio, 0.99f));
			float pressure = Clamp((prisonerRatio - thresholdRatio) / span, 0f, 1f);
			urgency = 62f + pressure * 18f + heroBonus;
			return true;
		}
		if (thresholdRatio <= 1f && candidate.HeroPrisonerCount > 0 && prisonerRatio >= Math.Min(0.5f, thresholdRatio))
		{
			urgency = 58f + heroBonus;
			return true;
		}
		return false;
	}

	private static bool IsClanCaptiveNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.CaptiveClanHeroCount <= 0)
		{
			return false;
		}
		urgency = 64f + Math.Min(18f, candidate.CaptiveClanHeroCount * 6f);
		if (candidate.CaptiveClanLeaderHeld)
		{
			urgency += 12f;
		}
		return true;
	}

	private static bool IsLowMoraleNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.MemberCount <= 0)
		{
			return false;
		}
		int threshold = Clamp(settings?.ProactiveNpcRequestLowMoraleThreshold ?? 35, 0, 100);
		if (threshold <= 0 || candidate.Morale > threshold)
		{
			return false;
		}
		float deficitRatio = threshold <= 0 ? 0f : Clamp((threshold - candidate.Morale) / Math.Max(1f, threshold), 0f, 1f);
		urgency = 54f + deficitRatio * 30f;
		return true;
	}

	private static bool IsMountShortageNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.MemberCount <= 0)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestMountRatioThresholdPercent ?? 25, 0, 100);
		if (thresholdPercent <= 0)
		{
			return false;
		}
		float thresholdRatio = thresholdPercent / 100f;
		if (candidate.MountRatio >= thresholdRatio)
		{
			return false;
		}
		float deficitRatio = Clamp((thresholdRatio - candidate.MountRatio) / Math.Max(0.01f, thresholdRatio), 0f, 1f);
		urgency = 50f + deficitRatio * 26f;
		if (candidate.PackAnimalRatio < Math.Min(0.15f, thresholdRatio))
		{
			urgency += 4f;
		}
		return true;
	}

	private static bool IsOverburdenedNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.InventoryCapacity <= 0 || candidate.TotalWeightCarried <= 0f)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestOverburdenRatioThresholdPercent ?? 92, 50, 150);
		float thresholdRatio = thresholdPercent / 100f;
		if (candidate.CarryRatio < thresholdRatio)
		{
			return false;
		}
		if (candidate.CarryRatio >= 1f)
		{
			float overRatio = Clamp(candidate.CarryRatio - 1f, 0f, 1f);
			urgency = 76f + overRatio * 20f;
			return true;
		}
		float pressure = Clamp((candidate.CarryRatio - thresholdRatio) / Math.Max(0.01f, 1f - Math.Min(thresholdRatio, 0.99f)), 0f, 1f);
		urgency = 58f + pressure * 18f;
		return true;
	}

	private static bool IsClanFinanceStrainNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null)
		{
			return false;
		}
		int goldThreshold = Clamp(settings?.ProactiveNpcRequestClanGoldThreshold ?? 15000, 0, 200000);
		int debtThreshold = Clamp(settings?.ProactiveNpcRequestClanDebtThreshold ?? 5000, 0, 200000);
		bool goldLow = goldThreshold > 0 && candidate.ClanGold < goldThreshold;
		bool debtHigh = debtThreshold > 0 && candidate.ClanDebtToKingdom > debtThreshold;
		if (!goldLow && !debtHigh)
		{
			return false;
		}
		float goldUrgency = 0f;
		if (goldLow)
		{
			float deficitRatio = Clamp((goldThreshold - candidate.ClanGold) / (float)Math.Max(1, goldThreshold), 0f, 1f);
			goldUrgency = 56f + deficitRatio * 22f;
		}
		float debtUrgency = 0f;
		if (debtHigh)
		{
			float debtRatio = Clamp((candidate.ClanDebtToKingdom - debtThreshold) / (float)Math.Max(1, debtThreshold), 0f, 2f);
			debtUrgency = 62f + Math.Min(24f, debtRatio * 12f);
		}
		urgency = Math.Max(goldUrgency, debtUrgency);
		return urgency > 0f;
	}

	private bool TryBuildPolicySupportSnapshot(ProactiveCandidate candidate, out PolicySupportSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan npcClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = npcClan?.Kingdom;
			if (hero == null
				|| npcClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| npcClan == playerClan
				|| npcClan.IsEliminated
				|| hero != npcClan.Leader
				|| npcClan.IsUnderMercenaryService
				|| playerClan.IsUnderMercenaryService
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}
			int playerClanRelation = Clamp(npcClan.GetRelationWithClan(playerClan), -100, 100);
			if (playerClanRelation <= 30)
			{
				return false;
			}
			PolicySupportSnapshot cachedSnapshot = GetClanPolicySupportSnapshot(npcClan);
			if (cachedSnapshot == null || cachedSnapshot.SupportScore < 100f || string.IsNullOrWhiteSpace(cachedSnapshot.PolicyName))
			{
				return false;
			}
			snapshot = new PolicySupportSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? "该王国").Trim(),
				PlayerClanRelation = playerClanRelation,
				PolicyName = cachedSnapshot.PolicyName,
				Description = cachedSnapshot.Description,
				Effects = cachedSnapshot.Effects,
				SupportScore = cachedSnapshot.SupportScore,
				HasPendingDecision = cachedSnapshot.HasPendingDecision
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private PolicySupportSnapshot GetClanPolicySupportSnapshot(Clan clan)
	{
		string clanKey = (clan?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return null;
		}
		float cacheHour = (float)Math.Floor(NowHours());
		if (_policySupportSnapshotsByClan.TryGetValue(clanKey, out PolicySupportSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		PolicySupportSnapshot snapshot = ScanClanPolicySupportSnapshot(clan);
		_policySupportSnapshotsByClan[clanKey] = new PolicySupportSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		if (_policySupportSnapshotsByClan.Count > 128)
		{
			foreach (string key in _policySupportSnapshotsByClan
				.Where(pair => pair.Value == null || pair.Value.SampledAtHour < cacheHour - 1f)
				.Select(pair => pair.Key)
				.ToList())
			{
				_policySupportSnapshotsByClan.Remove(key);
			}
		}
		return snapshot;
	}

	private static PolicySupportSnapshot ScanClanPolicySupportSnapshot(Clan clan)
	{
		try
		{
			Kingdom kingdom = clan?.Kingdom;
			if (kingdom == null || clan.Leader == null || clan.IsUnderMercenaryService)
			{
				return null;
			}
			PolicyObject selectedPolicy = null;
			float selectedScore = float.MinValue;
			foreach (PolicyObject policy in PolicyObject.All ?? Enumerable.Empty<PolicyObject>())
			{
				if (policy == null || !policy.IsReady || kingdom.ActivePolicies.Contains(policy))
				{
					continue;
				}
				if (!Campaign.Current.Models.KingdomDecisionPermissionModel.IsPolicyDecisionAllowed(policy))
				{
					continue;
				}
				float supportScore;
				try
				{
					supportScore = new KingdomPolicyDecision(clan, policy, false).CalculateSupport(clan);
				}
				catch
				{
					continue;
				}
				if (supportScore < 100f || (selectedPolicy != null && supportScore <= selectedScore))
				{
					continue;
				}
				selectedPolicy = policy;
				selectedScore = supportScore;
			}
			if (selectedPolicy == null)
			{
				return null;
			}
			return new PolicySupportSnapshot
			{
				PolicyName = LimitPolicySupportPromptText(selectedPolicy.Name?.ToString(), 80),
				Description = LimitPolicySupportPromptText(selectedPolicy.Description?.ToString() ?? selectedPolicy.LogEntryDescription?.ToString(), 180),
				Effects = LimitPolicySupportPromptText(selectedPolicy.SecondaryEffects?.ToString(), 180),
				SupportScore = selectedScore,
				HasPendingDecision = HasPendingPolicyDecision(kingdom, selectedPolicy)
			};
		}
		catch
		{
			return null;
		}
	}

	private static bool HasPendingPolicyDecision(Kingdom kingdom, PolicyObject policy)
	{
		try
		{
			return kingdom?.UnresolvedDecisions != null
				&& kingdom.UnresolvedDecisions.Any(decision => decision is KingdomPolicyDecision policyDecision
					&& policyDecision.Policy == policy
					&& !policyDecision.ShouldBeCancelled());
		}
		catch
		{
			return false;
		}
	}

	private static string LimitPolicySupportPromptText(string text, int maxChars)
	{
		text = (text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxChars - 1)).TrimEnd() + "…";
	}

	private static bool TryBuildClanServiceNeedSnapshot(ProactiveCandidate candidate, out ClanServiceNeedSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero clanLeader = candidate?.Hero;
			Clan targetClan = clanLeader?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom playerKingdom = playerClan?.Kingdom;
			Kingdom targetKingdom = targetClan?.Kingdom;
			Clan currentRulingClan = targetKingdom?.RulingClan;
			if (clanLeader == null
				|| targetClan == null
				|| playerClan == null
				|| playerKingdom == null
				|| playerKingdom.IsEliminated
				|| Hero.MainHero != playerKingdom.RulingClan?.Leader
				|| targetClan == playerClan
				|| targetClan.IsEliminated
				|| targetClan.IsUnderMercenaryService
				|| targetClan.IsClanTypeMercenary
				|| clanLeader != targetClan.Leader
				|| targetKingdom == null
				|| targetKingdom.IsEliminated
				|| targetKingdom == playerKingdom
				|| currentRulingClan == null
				|| currentRulingClan.IsEliminated
				|| currentRulingClan == targetClan
				|| (targetClan.Fiefs != null && targetClan.Fiefs.Any(fief => fief != null)))
			{
				return false;
			}

			int playerRelation = playerClan.GetRelationWithClan(targetClan);
			int currentKingRelation = targetClan.GetRelationWithClan(currentRulingClan);
			int relationGap = playerRelation - currentKingRelation;
			if (relationGap <= 40)
			{
				return false;
			}
			snapshot = new ClanServiceNeedSnapshot
			{
				TargetClanName = targetClan.Name?.ToString() ?? "该家族",
				CurrentKingName = currentRulingClan.Leader?.Name?.ToString() ?? "现任国王",
				PlayerRelation = playerRelation,
				CurrentKingRelation = currentKingRelation,
				RelationGap = relationGap
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildPoliticalRivalSuppressionSnapshot(ProactiveCandidate candidate, out PoliticalRivalSuppressionSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan requesterClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = requesterClan?.Kingdom;
			if (hero == null
				|| requesterClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| requesterClan.IsEliminated
				|| requesterClan == playerClan
				|| hero != requesterClan.Leader
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}

			int playerClanRelation = requesterClan.GetRelationWithClan(playerClan);
			if (playerClanRelation <= 20)
			{
				return false;
			}

			Clan rivalClan = null;
			int rivalClanRelation = int.MaxValue;
			foreach (Clan otherClan in kingdom.Clans ?? Enumerable.Empty<Clan>())
			{
				if (otherClan == null
					|| otherClan == requesterClan
					|| otherClan == playerClan
					|| otherClan.IsEliminated
					|| otherClan.Kingdom != kingdom)
				{
					continue;
				}
				int relation = requesterClan.GetRelationWithClan(otherClan);
				if (relation < -10 && relation < rivalClanRelation)
				{
					rivalClan = otherClan;
					rivalClanRelation = relation;
				}
			}
			if (rivalClan == null)
			{
				return false;
			}

			snapshot = new PoliticalRivalSuppressionSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? "该王国").Trim(),
				RequesterClanName = (requesterClan.Name?.ToString() ?? "该家族").Trim(),
				PlayerClanRelation = playerClanRelation,
				RivalClanName = (rivalClan.Name?.ToString() ?? "同阵营家族").Trim(),
				RivalClanRelation = rivalClanRelation
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPoliticalRivalSuppressionOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedPoliticalRivalSuppression, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildSettlementPurchaseSnapshot(ProactiveCandidate candidate, out SettlementPurchaseSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan requesterClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = requesterClan?.Kingdom;
			if (hero == null
				|| requesterClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| requesterClan.IsEliminated
				|| requesterClan == playerClan
				|| hero != requesterClan.Leader
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}

			List<Settlement> playerFiefs = GetClanTownAndCastleSettlements(playerClan);
			List<Settlement> requesterFiefs = GetClanTownAndCastleSettlements(requesterClan);
			if (playerFiefs.Count < 3 || requesterFiefs.Count > 1)
			{
				return false;
			}

			List<string> playerTowns = playerFiefs
				.Where(x => x.IsTown)
				.Select(GetSettlementDisplayName)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.OrderBy(x => x, StringComparer.Ordinal)
				.ToList();
			List<string> playerCastles = playerFiefs
				.Where(x => x.IsCastle)
				.Select(GetSettlementDisplayName)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.OrderBy(x => x, StringComparer.Ordinal)
				.ToList();
			int requesterTownCount = requesterFiefs.Count(x => x.IsTown);
			int requesterCastleCount = requesterFiefs.Count(x => x.IsCastle);
			snapshot = new SettlementPurchaseSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? "该王国").Trim(),
				PlayerFiefCount = playerFiefs.Count,
				PlayerTownCount = playerTowns.Count,
				PlayerCastleCount = playerCastles.Count,
				PlayerFiefsText = "城镇：" + (playerTowns.Count > 0 ? string.Join("、", playerTowns) : "无") + "；城堡：" + (playerCastles.Count > 0 ? string.Join("、", playerCastles) : "无"),
				NpcFiefCount = requesterFiefs.Count,
				NpcTownCount = requesterTownCount,
				NpcCastleCount = requesterCastleCount
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static List<Settlement> GetClanTownAndCastleSettlements(Clan clan)
	{
		Dictionary<string, Settlement> settlements = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (Town fief in clan?.Fiefs ?? Enumerable.Empty<Town>())
			{
				Settlement settlement = fief?.Settlement;
				if (settlement == null || settlement.OwnerClan != clan || (!settlement.IsTown && !settlement.IsCastle))
				{
					continue;
				}
				string key = (settlement.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(key))
				{
					settlements[key] = settlement;
				}
			}
		}
		catch
		{
		}
		return settlements.Values.ToList();
	}

	private static bool IsSettlementPurchaseOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedSettlementPurchase, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private bool TryBuildSettlementSaleSnapshot(ProactiveCandidate candidate, out SettlementSaleSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan requesterClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = requesterClan?.Kingdom;
			if (hero == null
				|| requesterClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| requesterClan.IsEliminated
				|| requesterClan == playerClan
				|| hero != requesterClan.Leader
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}
			int playerClanRelation = Clamp(requesterClan.GetRelationWithClan(playerClan), -100, 100);
			if (playerClanRelation <= 30)
			{
				return false;
			}
			SettlementSaleSnapshot clanSnapshot = GetClanSettlementSaleSnapshot(requesterClan);
			if (clanSnapshot == null || clanSnapshot.NpcFiefCount < 4 || string.IsNullOrWhiteSpace(clanSnapshot.TargetSettlementName))
			{
				return false;
			}
			snapshot = new SettlementSaleSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? "该王国").Trim(),
				PlayerClanRelation = playerClanRelation,
				NpcFiefCount = clanSnapshot.NpcFiefCount,
				TargetSettlementName = clanSnapshot.TargetSettlementName,
				TargetSettlementType = clanSnapshot.TargetSettlementType,
				TargetDailyIncome = clanSnapshot.TargetDailyIncome,
				HighestFamilyDailyIncome = clanSnapshot.HighestFamilyDailyIncome,
				ForeignSettlementName = clanSnapshot.ForeignSettlementName,
				ForeignFactionName = clanSnapshot.ForeignFactionName,
				BorderDistance = clanSnapshot.BorderDistance,
				BorderRadius = clanSnapshot.BorderRadius
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private SettlementSaleSnapshot GetClanSettlementSaleSnapshot(Clan clan)
	{
		string clanKey = (clan?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return null;
		}
		float cacheHour = (float)Math.Floor(NowHours());
		if (_settlementSaleSnapshotsByClan.TryGetValue(clanKey, out SettlementSaleSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		SettlementSaleSnapshot snapshot = ScanClanSettlementSaleSnapshot(clan);
		_settlementSaleSnapshotsByClan[clanKey] = new SettlementSaleSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		if (_settlementSaleSnapshotsByClan.Count > 128)
		{
			foreach (string key in _settlementSaleSnapshotsByClan
				.Where(pair => pair.Value == null || pair.Value.SampledAtHour < cacheHour - 1f)
				.Select(pair => pair.Key)
				.ToList())
			{
				_settlementSaleSnapshotsByClan.Remove(key);
			}
		}
		return snapshot;
	}

	private static SettlementSaleSnapshot ScanClanSettlementSaleSnapshot(Clan clan)
	{
		try
		{
			Kingdom kingdom = clan?.Kingdom;
			List<Settlement> clanFiefs = GetClanTownAndCastleSettlements(clan);
			if (kingdom == null || clanFiefs.Count < 4)
			{
				return null;
			}
			float borderRadius = GetSettlementSaleBorderRadius();
			if (borderRadius <= 0f)
			{
				return null;
			}
			List<Settlement> foreignFiefs = (Settlement.All ?? Enumerable.Empty<Settlement>())
				.Where(settlement => IsForeignFactionFortification(settlement, kingdom))
				.ToList();
			if (foreignFiefs.Count == 0)
			{
				return null;
			}
			List<SettlementSaleFiefIncome> fiefIncomes = clanFiefs
				.Select(settlement => new SettlementSaleFiefIncome
				{
					Settlement = settlement,
					DailyIncome = CalculateSettlementDailyIncomeDenars(settlement, clan)
				})
				.Where(item => item.Settlement != null)
				.ToList();
			if (fiefIncomes.Count < 4)
			{
				return null;
			}
			int lowestIncome = fiefIncomes.Min(item => item.DailyIncome);
			int highestIncome = fiefIncomes.Max(item => item.DailyIncome);
			SettlementSaleFiefIncome target = null;
			foreach (SettlementSaleFiefIncome item in fiefIncomes
				.Where(item => item.DailyIncome == lowestIncome)
				.OrderBy(item => item.Settlement.StringId, StringComparer.Ordinal))
			{
				Settlement nearestForeign = null;
				float nearestDistance = float.MaxValue;
				foreach (Settlement foreignFief in foreignFiefs)
				{
					float distance = GetSettlementTravelDistance(item.Settlement, foreignFief);
					if (distance >= 0f && distance < nearestDistance)
					{
						nearestDistance = distance;
						nearestForeign = foreignFief;
					}
				}
				if (nearestForeign == null || nearestDistance > borderRadius)
				{
					continue;
				}
				item.NearestForeignSettlement = nearestForeign;
				item.NearestForeignDistance = nearestDistance;
				target = item;
				break;
			}
			if (target == null || target.NearestForeignSettlement == null)
			{
				return null;
			}
			Settlement foreignSettlement = target.NearestForeignSettlement;
			return new SettlementSaleSnapshot
			{
				NpcFiefCount = fiefIncomes.Count,
				TargetSettlementName = GetSettlementDisplayName(target.Settlement),
				TargetSettlementType = target.Settlement.IsTown ? "城镇" : "城堡",
				TargetDailyIncome = target.DailyIncome,
				HighestFamilyDailyIncome = highestIncome,
				ForeignSettlementName = GetSettlementDisplayName(foreignSettlement),
				ForeignFactionName = GetSettlementFactionDisplayName(foreignSettlement),
				BorderDistance = target.NearestForeignDistance,
				BorderRadius = borderRadius
			};
		}
		catch
		{
			return null;
		}
	}

	private static bool IsForeignFactionFortification(Settlement settlement, Kingdom ownKingdom)
	{
		return settlement != null
			&& (settlement.IsTown || settlement.IsCastle)
			&& settlement.OwnerClan != null
			&& settlement.MapFaction != ownKingdom
			&& settlement.OwnerClan.Kingdom != ownKingdom;
	}

	private static float GetSettlementSaleBorderRadius()
	{
		try
		{
			return Math.Max(0f, Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 0.66f);
		}
		catch
		{
			return 0f;
		}
	}

	private static float GetSettlementTravelDistance(Settlement fromSettlement, Settlement toSettlement)
	{
		try
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, false, false, MobileParty.NavigationType.Default);
			return float.IsNaN(distance) || float.IsInfinity(distance) ? -1f : distance;
		}
		catch
		{
			return -1f;
		}
	}

	private static int CalculateSettlementDailyIncomeDenars(Settlement settlement, Clan ownerClan)
	{
		try
		{
			Town town = settlement?.Town;
			if (town == null || ownerClan == null || Campaign.Current?.Models == null)
			{
				return 0;
			}
			int income = 0;
			income += (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town, includeDescriptions: false).ResultNumber;
			income += (int)Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromTariffs(ownerClan, town, applyWithdrawals: false).ResultNumber;
			income += Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromProjects(town);
			foreach (Village village in town.Villages)
			{
				if (village != null)
				{
					income += Campaign.Current.Models.ClanFinanceModel.CalculateVillageIncome(ownerClan, village, applyWithdrawals: false);
				}
			}
			return Math.Max(0, income);
		}
		catch
		{
			return 0;
		}
	}

	private static string GetSettlementFactionDisplayName(Settlement settlement)
	{
		try
		{
			return (settlement?.MapFaction?.Name?.ToString()
				?? settlement?.OwnerClan?.Kingdom?.Name?.ToString()
				?? settlement?.OwnerClan?.Name?.ToString()
				?? "其他势力").Trim();
		}
		catch
		{
			return "其他势力";
		}
	}

	private static bool IsRomanticInteractionEligible(Hero hero, out int privateRelation)
	{
		privateRelation = 0;
		try
		{
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero == player
				|| hero.IsDead
				|| !hero.IsAlive
				|| hero.IsPrisoner
				|| hero.IsFugitive
				|| hero.PartyBelongedToAsPrisoner != null
				|| hero.IsChild
				|| player.IsChild
				|| hero.Age < 18f
				|| player.Age < 18f
				|| hero.IsFemale == player.IsFemale)
			{
				return false;
			}
			privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			return privateRelation > 30;
		}
		catch
		{
			privateRelation = 0;
			return false;
		}
	}

	private static bool IsTerritorialInterrogationOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedTerritorialInterrogation, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsGreetingOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedGreeting, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsGreetingEligible(Hero hero, out int privateRelation)
	{
		privateRelation = 0;
		try
		{
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero == player
				|| hero.IsDead
				|| !hero.IsAlive
				|| hero.IsPrisoner
				|| hero.IsFugitive
				|| hero.PartyBelongedToAsPrisoner != null)
			{
				return false;
			}
			privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			return privateRelation > 20;
		}
		catch
		{
			privateRelation = 0;
			return false;
		}
	}

	private static bool IsFriendshipOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedFriendship, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCourtshipOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedCourtship, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildFriendshipNeedSnapshot(ProactiveCandidate candidate, out FriendshipNeedSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero == player
				|| candidate.AtWarWithPlayer
				|| hero.IsDead
				|| !hero.IsAlive
				|| hero.IsPrisoner
				|| hero.IsFugitive
				|| hero.PartyBelongedToAsPrisoner != null)
			{
				return false;
			}

			int playerClanTier = Math.Max(candidate.PlayerClanTier, SafePlayerClanTier());
			if (playerClanTier < 1)
			{
				return false;
			}
			string cultureId = (hero.Culture?.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(cultureId))
			{
				return false;
			}
			int cultureNotoriety = PlayerNotorietyBehavior.GetCultureNotorietyForExternal(cultureId);
			if (cultureNotoriety < 10)
			{
				return false;
			}
			int privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			if (privateRelation >= 5)
			{
				return false;
			}

			snapshot = new FriendshipNeedSnapshot
			{
				CultureNotoriety = cultureNotoriety,
				PlayerClanTier = playerClanTier,
				PrivateRelation = privateRelation
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildCourtshipNeedSnapshot(ProactiveCandidate candidate, out CourtshipNeedSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			if (!TryBuildFriendshipNeedSnapshot(candidate, out FriendshipNeedSnapshot friendship))
			{
				return false;
			}
			Hero hero = candidate.Hero;
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero.IsFemale == player.IsFemale)
			{
				return false;
			}
			int honor = hero.GetTraitLevel(DefaultTraits.Honor);
			int calculating = hero.GetTraitLevel(DefaultTraits.Calculating);
			bool married = IsMarriedToLivingSpouse(hero);
			float triggerWeightMultiplier = 1f;
			if (married)
			{
				// An honourable spouse does not initiate an affair. Other married heroes need a matching disposition and remain rare.
				if (honor > 0 || (honor >= 0 && calculating <= 0))
				{
					return false;
				}
				triggerWeightMultiplier = honor < 0 ? 0.20f : 0.15f;
				if (calculating > 0)
				{
					triggerWeightMultiplier += Math.Min(calculating, 2) * 0.10f;
				}
			}
			snapshot = new CourtshipNeedSnapshot
			{
				CultureNotoriety = friendship.CultureNotoriety,
				PlayerClanTier = friendship.PlayerClanTier,
				NpcClanTier = Clamp(hero.Clan?.Tier ?? 0, 0, 6),
				TriggerWeightMultiplier = Clamp(triggerWeightMultiplier, 0f, 1f)
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMarriedToLivingSpouse(Hero hero)
	{
		try
		{
			Hero spouse = hero?.Spouse;
			return spouse != null && spouse.IsAlive && !spouse.IsDead;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildArmyJoinRequestSnapshot(ProactiveCandidate candidate, out ArmyJoinRequestSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			MobileParty npcParty = candidate?.Party;
			MobileParty playerParty = MobileParty.MainParty;
			Hero hero = candidate?.Hero;
			Kingdom kingdom = candidate?.TargetKingdom;
			Army army = npcParty?.Army;
			if (npcParty == null
				|| playerParty == null
				|| hero == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| playerParty.Army != null
				|| playerParty.MapFaction != kingdom
				|| army == null
				|| army.Kingdom != kingdom
				|| army.LeaderParty != npcParty
				|| npcParty.LeaderHero != hero)
			{
				return false;
			}
			float ownStrength = Math.Max(0f, kingdom.CurrentTotalStrength);
			float enemyStrength = 0f;
			int enemyKingdomCount = 0;
			foreach (IFaction enemy in kingdom.FactionsAtWarWith ?? Enumerable.Empty<IFaction>())
			{
				if (enemy == null || !enemy.IsKingdomFaction || enemy.IsEliminated)
				{
					continue;
				}
				enemyStrength += Math.Max(0f, enemy.CurrentTotalStrength);
				enemyKingdomCount++;
			}
			if (ownStrength <= 0f || enemyStrength <= 0f)
			{
				return false;
			}
			float ownToEnemyRatio = ownStrength / enemyStrength;
			if (ownToEnemyRatio > 0.66f)
			{
				return false;
			}
			snapshot = new ArmyJoinRequestSnapshot
			{
				ArmyName = (army.Name?.ToString() ?? (hero.Name?.ToString() ?? "该领主") + "的军团").Trim(),
				OwnStrength = ownStrength,
				EnemyStrength = enemyStrength,
				EnemyKingdomCount = enemyKingdomCount,
				OwnToEnemyRatio = ownToEnemyRatio
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool TryBuildBanditSuppressionSnapshot(ProactiveCandidate candidate, out BanditSuppressionSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			if (hero == null || candidate.AtWarWithPlayer || hero.Clan == null)
			{
				return false;
			}
			int trust = Clamp(RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0, -100, 100);
			int privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			if (trust <= 10 || privateRelation <= 10)
			{
				return false;
			}
			BanditSuppressionSnapshot fiefSnapshot = GetClanBanditSuppressionSnapshot(hero.Clan);
			if (fiefSnapshot == null || fiefSnapshot.BanditCount <= 8)
			{
				return false;
			}
			snapshot = new BanditSuppressionSnapshot
			{
				SettlementName = fiefSnapshot.SettlementName,
				BanditCount = fiefSnapshot.BanditCount,
				Radius = fiefSnapshot.Radius,
				Trust = trust,
				PrivateRelation = privateRelation
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private BanditSuppressionSnapshot GetClanBanditSuppressionSnapshot(Clan clan)
	{
		string clanKey = (clan?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return null;
		}
		float nowHours = NowHours();
		float cacheHour = (float)Math.Floor(nowHours);
		if (_banditSuppressionSnapshotsByClan.TryGetValue(clanKey, out BanditSuppressionSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		BanditSuppressionSnapshot snapshot = ScanClanBanditSuppressionSnapshot(clan);
		_banditSuppressionSnapshotsByClan[clanKey] = new BanditSuppressionSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		if (_banditSuppressionSnapshotsByClan.Count > 128)
		{
			foreach (string key in _banditSuppressionSnapshotsByClan
				.Where(pair => pair.Value == null || pair.Value.SampledAtHour < cacheHour - 1f)
				.Select(pair => pair.Key)
				.ToList())
			{
				_banditSuppressionSnapshotsByClan.Remove(key);
			}
		}
		return snapshot;
	}

	private static BanditSuppressionSnapshot ScanClanBanditSuppressionSnapshot(Clan clan)
	{
		try
		{
			if (clan?.Fiefs == null || clan.Fiefs.Count <= 0)
			{
				return null;
			}
			float radius = GetBanditSuppressionRadius();
			float radiusSquared = radius * radius;
			BanditSuppressionSnapshot result = null;
			foreach (Town fief in clan.Fiefs)
			{
				Settlement settlement = fief?.Settlement;
				if (settlement == null)
				{
					continue;
				}
				int banditCount = 0;
				foreach (MobileParty party in MobileParty.All ?? Enumerable.Empty<MobileParty>())
				{
					if (party == null
						|| !party.IsActive
						|| party.MapEvent != null
						|| party.CurrentSettlement != null
						|| MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party)
						|| !CourierDeliveryBehavior.IsBanditOrOutlawParty(party)
						|| party.Position.DistanceSquared(settlement.GatePosition) > radiusSquared)
					{
						continue;
					}
					banditCount++;
				}
				if (result == null || banditCount > result.BanditCount)
				{
					result = new BanditSuppressionSnapshot
					{
						SettlementName = (settlement.Name?.ToString() ?? settlement.StringId ?? "某处封地").Trim(),
						BanditCount = banditCount,
						Radius = radius
					};
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static float GetBanditSuppressionRadius()
	{
		try
		{
			return Math.Max(1f, Campaign.Current.EstimatedAverageBanditPartySpeed * CampaignTime.HoursInDay * 0.5f);
		}
		catch
		{
			return 1f;
		}
	}

	private static bool TryBuildTerritorialInterrogationSnapshot(ProactiveCandidate candidate, DuelSettings settings, Dictionary<string, TerritorialSettlementSnapshot> territorialSettlementSnapshots, out TerritorialInterrogationSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero player = Hero.MainHero;
			Hero hero = candidate?.Hero;
			Kingdom kingdom = candidate?.TargetKingdom;
			MobileParty mainParty = MobileParty.MainParty;
			string playerCultureId = (player?.Culture?.StringId ?? "").Trim();
			string npcCultureId = (hero?.Culture?.StringId ?? "").Trim();
			if (player == null
				|| hero == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| mainParty == null
				|| string.IsNullOrWhiteSpace(playerCultureId)
				|| string.IsNullOrWhiteSpace(npcCultureId)
				|| string.Equals(playerCultureId, npcCultureId, StringComparison.OrdinalIgnoreCase)
				|| PlayerNotorietyBehavior.HasObserverUnlockedPlayerMajorForExternal(hero))
			{
				return false;
			}
			int cultureNotoriety = PlayerNotorietyBehavior.GetCultureNotorietyForExternal(npcCultureId);
			if (cultureNotoriety >= 5)
			{
				return false;
			}
			TerritorialSettlementSnapshot nearest = GetNearestKingdomSettlementSnapshot(kingdom, mainParty, territorialSettlementSnapshots);
			if (nearest == null || nearest.Distance < 0f)
			{
				return false;
			}
			float rangeMultiplier = Clamp(settings?.ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier ?? 3f, 0.5f, 10f);
			float maxDistance = Math.Max(1f, mainParty.SeeingRange * rangeMultiplier);
			if (nearest.Distance > maxDistance)
			{
				return false;
			}
			snapshot = new TerritorialInterrogationSnapshot
			{
				KingdomName = GetKingdomName(kingdom),
				SettlementName = nearest.SettlementName,
				SettlementDistance = nearest.Distance,
				NpcCultureName = (hero.Culture?.Name?.ToString() ?? npcCultureId).Trim(),
				CultureNotoriety = cultureNotoriety
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static TerritorialSettlementSnapshot GetNearestKingdomSettlementSnapshot(Kingdom kingdom, MobileParty mainParty, Dictionary<string, TerritorialSettlementSnapshot> cache)
	{
		if (kingdom == null || mainParty == null)
		{
			return null;
		}
		string kingdomKey = GetKingdomKey(kingdom);
		if (cache != null && !string.IsNullOrWhiteSpace(kingdomKey) && cache.TryGetValue(kingdomKey, out TerritorialSettlementSnapshot cached))
		{
			return cached;
		}
		TerritorialSettlementSnapshot result = new TerritorialSettlementSnapshot { Distance = -1f };
		foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
		{
			if (settlement == null
				|| settlement.IsHideout
				|| (settlement.MapFaction != kingdom && settlement.OwnerClan?.Kingdom != kingdom))
			{
				continue;
			}
			float distance = settlement.GatePosition.Distance(mainParty.Position);
			if (distance < 0f || (result.Distance >= 0f && distance >= result.Distance))
			{
				continue;
			}
			result = new TerritorialSettlementSnapshot
			{
				SettlementName = (settlement.Name?.ToString() ?? settlement.StringId ?? "该王国定居点").Trim(),
				Distance = distance
			};
		}
		if (cache != null && !string.IsNullOrWhiteSpace(kingdomKey))
		{
			cache[kingdomKey] = result;
		}
		return result;
	}

	private static bool IsMarriageAlliancePressureNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.MarriageUnmarriedAdultCount <= 0)
		{
			return false;
		}
		int adultThreshold = Clamp(settings?.ProactiveNpcRequestMarriageAdultClanThreshold ?? 3, 1, 12);
		if (candidate.MarriageAdultClanHeroCount > adultThreshold && !candidate.MarriageRequesterUnmarried)
		{
			return false;
		}
		float adultDeficit = Math.Max(0, adultThreshold - candidate.MarriageAdultClanHeroCount);
		urgency = 52f + adultDeficit * 8f + Math.Min(10f, candidate.MarriageUnmarriedAdultCount * 2f);
		if (candidate.MarriageRequesterUnmarried)
		{
			urgency += 8f;
		}
		if (candidate.MarriageAdultClanHeroCount <= 1)
		{
			urgency += 8f;
		}
		return urgency >= 50f;
	}

	private static bool IsRevengePressureNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.RevengePressureScore <= 0f)
		{
			return false;
		}
		urgency = Clamp(candidate.RevengePressureScore, 0f, 100f);
		return urgency >= 55f;
	}

	private static bool IsFiefGovernanceAnxietyNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.FiefProblemCount <= 0)
		{
			return false;
		}
		urgency = 56f + Math.Min(18f, candidate.FiefProblemCount * 4f);
		if (candidate.FiefUnderAttack)
		{
			urgency += 18f;
		}
		if (candidate.FiefLoyalty >= 0f && candidate.FiefLoyalty <= 25f)
		{
			urgency += 8f;
		}
		if (candidate.FiefSecurity >= 0f && candidate.FiefSecurity <= 25f)
		{
			urgency += 6f;
		}
		if (candidate.FiefGarrisonCount > 0 && candidate.FiefGarrisonCount <= 40)
		{
			urgency += 4f;
		}
		urgency = Clamp(urgency, 0f, 100f);
		return urgency >= 50f;
	}

	private static bool IsAllySupportNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.TargetKingdom == null || candidate.Hero?.Clan == null)
		{
			return false;
		}
		int influenceThreshold = Clamp(settings?.ProactiveNpcRequestIsolationInfluenceThreshold ?? 40, 0, 500);
		int maxFriendly = Clamp(settings?.ProactiveNpcRequestIsolationMaxFriendlyClans ?? 1, 0, 10);
		bool lowInfluence = influenceThreshold > 0 && candidate.ClanInfluence <= influenceThreshold;
		bool fewFriends = candidate.FriendlyClanCount <= maxFriendly;
		bool manyEnemies = candidate.HostileClanCount >= 3;
		if (!fewFriends || (!lowInfluence && !manyEnemies))
		{
			return false;
		}
		float influencePressure = lowInfluence
			? Clamp((influenceThreshold - candidate.ClanInfluence) / Math.Max(1f, influenceThreshold), 0f, 1f) * 18f
			: 0f;
		urgency = 52f + influencePressure + Math.Min(16f, candidate.HostileClanCount * 4f);
		if (candidate.FriendlyClanCount <= 0)
		{
			urgency += 8f;
		}
		return urgency >= 50f;
	}

	private static bool IsKingdomMercenaryInviteNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		Clan playerClan = Clan.PlayerClan;
		if (candidate == null || playerClan == null || candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.KingdomNeedsMercenaries)
		{
			return false;
		}
		if (candidate.PlayerClanTier < MercenaryInviteMinPlayerClanTier)
		{
			return false;
		}
		if (playerClan.Kingdom != null || playerClan.IsUnderMercenaryService)
		{
			return false;
		}
		urgency = Math.Max(candidate.TargetHeroIsKingdomLeader ? 58f : 54f, candidate.KingdomMercenaryNeedUrgency);
		return true;
	}

	private static bool IsKingdomVassalInviteNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		Clan playerClan = Clan.PlayerClan;
		if (candidate == null || playerClan == null || candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.TargetHeroIsKingdomLeader || !candidate.KingdomNeedsVassals)
		{
			return false;
		}
		if (candidate.PlayerClanTier < VassalInviteMinPlayerClanTier)
		{
			return false;
		}
		if (playerClan.Kingdom == null)
		{
			urgency = Math.Max(64f, candidate.KingdomVassalNeedUrgency);
			return true;
		}
		if (playerClan.IsUnderMercenaryService && playerClan.Kingdom == candidate.TargetKingdom)
		{
			urgency = Math.Max(66f, candidate.KingdomVassalNeedUrgency + 2f);
			return true;
		}
		return false;
	}

	private void StartRequest(ProactiveCandidate candidate, DuelSettings settings)
	{
		MobileParty party = candidate?.Party;
		Hero hero = candidate?.Hero;
		if (party == null || hero == null)
		{
			return;
		}
		List<string> eligibleNeedTypes = FilterPlayerEligibleNeedTypes(candidate, candidate.NeedTypes, candidate.NeedType);
		if (eligibleNeedTypes.Count <= 0)
		{
			Logger.Log("ProactiveNpcRequest", "start aborted: all needs became player-ineligible. hero=" + GetHeroKey(hero) + " needs=" + JoinNeedTypesForLog(candidate.NeedTypes, candidate.NeedType));
			return;
		}
		candidate.NeedTypes = eligibleNeedTypes;
		candidate.NeedType = eligibleNeedTypes[0];
		if (string.Equals(candidate.TriggerSource, TriggerSourceNotorietyDriven, StringComparison.OrdinalIgnoreCase))
		{
			PlayerNotorietyBehavior.MarkObserverKnowsPlayerForExternal(hero, "proactive_notoriety_request");
		}
		_activeSession = new ProactiveNpcRequestSession
		{
			Id = Guid.NewGuid().ToString("N"),
			HeroId = GetHeroKey(hero),
			PartyId = (party.StringId ?? "").Trim(),
			NeedType = string.IsNullOrWhiteSpace(candidate.NeedType) ? NeedFoodShortage : candidate.NeedType,
			NeedTypes = NormalizeSingleNeedType(candidate.NeedTypes, candidate.NeedType),
			Stage = "Chasing",
			CreatedAtHours = NowHours(),
			ExpiresAtHours = NowHours() + ActiveRequestTtlHours,
			TriggerSource = string.IsNullOrWhiteSpace(candidate.TriggerSource) ? TriggerSourceNeedDriven : candidate.TriggerSource,
			KnownMajorBeforeRequest = candidate.KnownMajorBeforeRequest,
			EffectiveNotorietyAtRequest = candidate.EffectiveNotorietyAtRequest,
			NeedDrivenChance = candidate.NeedDrivenChance,
			NotorietyDrivenChance = candidate.NotorietyDrivenChance,
			SelectedNeedUrgency = candidate.SelectedNeedUrgency,
			NeedTypeFatigueMultiplierAtSelection = candidate.NeedTypeFatigueMultiplier,
			NeedTypeWeightMultiplierAtSelection = candidate.NeedTypeWeightMultiplier,
			NeedTypeFatigueRemainingDaysAtSelection = candidate.NeedTypeFatigueRemainingDays,
			LastKnownFoodDays = candidate.FoodDays,
			LastKnownPartyGold = candidate.PartyGold,
			LastKnownTotalWage = candidate.TotalWage,
			LastKnownUnpaidWages = candidate.UnpaidWages,
			LastKnownMemberCount = candidate.MemberCount,
			LastKnownPartySizeLimit = candidate.PartySizeLimit,
			LastKnownAvailableWageBudget = candidate.AvailableWageBudget,
			LastKnownPrisonerCount = candidate.PrisonerCount,
			LastKnownPrisonerSizeLimit = candidate.PrisonerSizeLimit,
			LastKnownHeroPrisonerCount = candidate.HeroPrisonerCount,
			LastKnownMorale = candidate.Morale,
			LastKnownInventoryCapacity = candidate.InventoryCapacity,
			LastKnownTotalWeightCarried = candidate.TotalWeightCarried,
			LastKnownCarryRatio = candidate.CarryRatio,
			LastKnownMountCount = candidate.MountCount,
			LastKnownPackAnimalCount = candidate.PackAnimalCount,
			LastKnownMountRatio = candidate.MountRatio,
			LastKnownPackAnimalRatio = candidate.PackAnimalRatio,
			LastKnownClanGold = candidate.ClanGold,
			LastKnownClanDebtToKingdom = candidate.ClanDebtToKingdom,
			LastKnownClanServiceTargetClanName = candidate.ClanServiceTargetClanName,
			LastKnownClanServiceCurrentKingName = candidate.ClanServiceCurrentKingName,
			LastKnownClanServicePlayerRelation = candidate.ClanServicePlayerRelation,
			LastKnownClanServiceCurrentKingRelation = candidate.ClanServiceCurrentKingRelation,
			LastKnownClanServiceRelationGap = candidate.ClanServiceRelationGap,
			LastKnownRomanticInteractionPrivateRelation = candidate.RomanticInteractionPrivateRelation,
			LastKnownGreetingPrivateRelation = candidate.GreetingPrivateRelation,
			LastKnownBanditSuppressionSettlementName = candidate.BanditSuppressionSettlementName,
			LastKnownBanditSuppressionBanditCount = candidate.BanditSuppressionBanditCount,
			LastKnownBanditSuppressionRadius = candidate.BanditSuppressionRadius,
			LastKnownBanditSuppressionTrust = candidate.BanditSuppressionTrust,
			LastKnownBanditSuppressionPrivateRelation = candidate.BanditSuppressionPrivateRelation,
			LastKnownPoliticalRivalSuppressionKingdomName = candidate.PoliticalRivalSuppressionKingdomName,
			LastKnownPoliticalRivalSuppressionRequesterClanName = candidate.PoliticalRivalSuppressionRequesterClanName,
			LastKnownPoliticalRivalSuppressionPlayerClanRelation = candidate.PoliticalRivalSuppressionPlayerClanRelation,
			LastKnownPoliticalRivalSuppressionRivalClanName = candidate.PoliticalRivalSuppressionRivalClanName,
			LastKnownPoliticalRivalSuppressionRivalClanRelation = candidate.PoliticalRivalSuppressionRivalClanRelation,
			LastKnownPolicySupportKingdomName = candidate.PolicySupportKingdomName,
			LastKnownPolicySupportPlayerClanRelation = candidate.PolicySupportPlayerClanRelation,
			LastKnownPolicySupportPolicyName = candidate.PolicySupportPolicyName,
			LastKnownPolicySupportDescription = candidate.PolicySupportDescription,
			LastKnownPolicySupportEffects = candidate.PolicySupportEffects,
			LastKnownPolicySupportScore = candidate.PolicySupportScore,
			LastKnownPolicySupportHasPendingDecision = candidate.PolicySupportHasPendingDecision,
			LastKnownPolicyDiscussionPolicyId = candidate.PolicyDiscussionPolicyId,
			LastKnownPolicyDiscussionPolicyName = candidate.PolicyDiscussionPolicyName,
			LastKnownPolicyDiscussionPolicyContent = candidate.PolicyDiscussionPolicyContent,
			LastKnownPolicyDiscussionKingdomName = candidate.PolicyDiscussionKingdomName,
			LastKnownPolicyDiscussionPublishedDay = candidate.PolicyDiscussionPublishedDay,
			LastKnownSettlementPurchaseKingdomName = candidate.SettlementPurchaseKingdomName,
			LastKnownSettlementPurchasePlayerTownCount = candidate.SettlementPurchasePlayerTownCount,
			LastKnownSettlementPurchasePlayerCastleCount = candidate.SettlementPurchasePlayerCastleCount,
			LastKnownSettlementPurchasePlayerFiefsText = candidate.SettlementPurchasePlayerFiefsText,
			LastKnownSettlementPurchaseNpcFiefCount = candidate.SettlementPurchaseNpcFiefCount,
			LastKnownSettlementPurchaseNpcTownCount = candidate.SettlementPurchaseNpcTownCount,
			LastKnownSettlementPurchaseNpcCastleCount = candidate.SettlementPurchaseNpcCastleCount,
			LastKnownSettlementSaleKingdomName = candidate.SettlementSaleKingdomName,
			LastKnownSettlementSalePlayerClanRelation = candidate.SettlementSalePlayerClanRelation,
			LastKnownSettlementSaleNpcFiefCount = candidate.SettlementSaleNpcFiefCount,
			LastKnownSettlementSaleTargetSettlementName = candidate.SettlementSaleTargetSettlementName,
			LastKnownSettlementSaleTargetSettlementType = candidate.SettlementSaleTargetSettlementType,
			LastKnownSettlementSaleTargetDailyIncome = candidate.SettlementSaleTargetDailyIncome,
			LastKnownSettlementSaleHighestFamilyDailyIncome = candidate.SettlementSaleHighestFamilyDailyIncome,
			LastKnownSettlementSaleForeignSettlementName = candidate.SettlementSaleForeignSettlementName,
			LastKnownSettlementSaleForeignFactionName = candidate.SettlementSaleForeignFactionName,
			LastKnownSettlementSaleBorderDistance = candidate.SettlementSaleBorderDistance,
			LastKnownSettlementSaleBorderRadius = candidate.SettlementSaleBorderRadius,
			LastKnownTerritorialInterrogationKingdomName = candidate.TerritorialInterrogationKingdomName,
			LastKnownTerritorialInterrogationSettlementName = candidate.TerritorialInterrogationSettlementName,
			LastKnownTerritorialInterrogationSettlementDistance = candidate.TerritorialInterrogationSettlementDistance,
			LastKnownTerritorialInterrogationNpcCultureName = candidate.TerritorialInterrogationNpcCultureName,
			LastKnownTerritorialInterrogationCultureNotoriety = candidate.TerritorialInterrogationCultureNotoriety,
			LastKnownCaptiveClanHeroCount = candidate.CaptiveClanHeroCount,
			LastKnownCaptiveClanHeroName = candidate.CaptiveClanHeroName,
			LastKnownCaptiveClanHeroHolderName = candidate.CaptiveClanHeroHolderName,
			LastKnownCaptiveClanLeaderHeld = candidate.CaptiveClanLeaderHeld,
			LastKnownMarriageAdultClanHeroCount = candidate.MarriageAdultClanHeroCount,
			LastKnownMarriageUnmarriedAdultCount = candidate.MarriageUnmarriedAdultCount,
			LastKnownMarriageFirstUnmarriedName = candidate.MarriageFirstUnmarriedName,
			LastKnownMarriageRequesterUnmarried = candidate.MarriageRequesterUnmarried,
			LastKnownRevengePressureScore = candidate.RevengePressureScore,
			LastKnownRevengeTargetName = candidate.RevengeTargetName,
			LastKnownRevengeReasonText = candidate.RevengeReasonText,
			LastKnownFiefProblemCount = candidate.FiefProblemCount,
			LastKnownFiefProblemName = candidate.FiefProblemName,
			LastKnownFiefLoyalty = candidate.FiefLoyalty,
			LastKnownFiefSecurity = candidate.FiefSecurity,
			LastKnownFiefGarrisonCount = candidate.FiefGarrisonCount,
			LastKnownFiefIssueText = candidate.FiefIssueText,
			LastKnownFiefUnderAttack = candidate.FiefUnderAttack,
			LastKnownClanInfluence = candidate.ClanInfluence,
			LastKnownFriendlyClanCount = candidate.FriendlyClanCount,
			LastKnownHostileClanCount = candidate.HostileClanCount,
			TargetKingdomId = candidate.TargetKingdomId,
			TargetKingdomName = candidate.TargetKingdomName,
			PlayerClanTier = candidate.PlayerClanTier,
			TargetHeroIsKingdomLeader = candidate.TargetHeroIsKingdomLeader,
			KingdomFormalVassalClanCount = candidate.KingdomFormalVassalClanCount,
			KingdomMercenaryClanCount = candidate.KingdomMercenaryClanCount,
			KingdomFiefScore = candidate.KingdomFiefScore,
			KingdomWarKingdomCount = candidate.KingdomWarKingdomCount,
			KingdomPowerRatioToEnemies = candidate.KingdomPowerRatioToEnemies,
			KingdomTargetMercenaryClanCount = candidate.KingdomTargetMercenaryClanCount,
			KingdomTargetVassalClanCount = candidate.KingdomTargetVassalClanCount,
			IsTestFallback = candidate.IsTestFallback
		};
		CacheActiveParty(party);
		_nextActiveEncounterProbeUtcTicks = 0L;
		try
		{
			party.Ai?.SetDoNotAttackMainParty(Math.Max(2, (int)ActiveRequestTtlHours));
		}
		catch
		{
		}
		SetPartyAiAction.GetActionForEngagingParty(party, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false);
		int globalCooldown = GetEffectiveGlobalCooldownHours(settings);
		_globalCooldownUntilHours = NowHours() + globalCooldown;
		Logger.Log("ProactiveNpcRequest", "started request triggerSource=" + (_activeSession.TriggerSource ?? "") + " knownMajorBefore=" + _activeSession.KnownMajorBeforeRequest + " effectiveNotoriety=" + _activeSession.EffectiveNotorietyAtRequest + " needChance=" + _activeSession.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + _activeSession.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + _activeSession.SelectedNeedUrgency.ToString("0.##") + " typeWeight=" + _activeSession.NeedTypeWeightMultiplierAtSelection.ToString("0.##") + " typeFatigueMultiplier=" + _activeSession.NeedTypeFatigueMultiplierAtSelection.ToString("0.##") + " typeFatigueRemainingDays=" + _activeSession.NeedTypeFatigueRemainingDaysAtSelection.ToString("0.##") + " need=" + _activeSession.NeedType + " needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " hero=" + _activeSession.HeroId + " party=" + _activeSession.PartyId + " kingdom=" + (_activeSession.TargetKingdomId ?? "") + " playerClanTier=" + _activeSession.PlayerClanTier + " isKingdomLeader=" + _activeSession.TargetHeroIsKingdomLeader + " kingdomVassals=" + _activeSession.KingdomFormalVassalClanCount + "/" + _activeSession.KingdomTargetVassalClanCount + " kingdomMercs=" + _activeSession.KingdomMercenaryClanCount + "/" + _activeSession.KingdomTargetMercenaryClanCount + " kingdomFiefScore=" + _activeSession.KingdomFiefScore + " kingdomWars=" + _activeSession.KingdomWarKingdomCount + " kingdomPowerRatio=" + _activeSession.KingdomPowerRatioToEnemies.ToString("0.00") + " foodDays=" + candidate.FoodDays + " partyGold=" + candidate.PartyGold + " totalWage=" + candidate.TotalWage + " unpaidWages=" + candidate.UnpaidWages.ToString("0.00") + " troops=" + candidate.MemberCount + "/" + candidate.PartySizeLimit + " troopRatio=" + candidate.PartySizeRatio.ToString("0.00") + " prisoners=" + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit + " heroPrisoners=" + candidate.HeroPrisonerCount + " prisonerRatio=" + candidate.PrisonerSizeRatio.ToString("0.00") + " morale=" + candidate.Morale.ToString("0.0") + " mounts=" + candidate.MountCount + " packAnimals=" + candidate.PackAnimalCount + " mountRatio=" + candidate.MountRatio.ToString("0.00") + " carry=" + candidate.TotalWeightCarried.ToString("0.0") + "/" + candidate.InventoryCapacity + " carryRatio=" + candidate.CarryRatio.ToString("0.00") + " clanGold=" + candidate.ClanGold + " clanDebt=" + candidate.ClanDebtToKingdom + " captiveClanHeroes=" + candidate.CaptiveClanHeroCount + " captiveLeader=" + candidate.CaptiveClanLeaderHeld + " wageBudget=" + candidate.AvailableWageBudget + " distance=" + candidate.Distance.ToString("0.0") + " testFallback=" + candidate.IsTestFallback);
		Logger.Log("ProactiveNpcRequest", "started request extra needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " marriageAdults=" + _activeSession.LastKnownMarriageAdultClanHeroCount + " unmarriedAdults=" + _activeSession.LastKnownMarriageUnmarriedAdultCount + " firstUnmarried=" + (_activeSession.LastKnownMarriageFirstUnmarriedName ?? "") + " clanServiceClan=" + (_activeSession.LastKnownClanServiceTargetClanName ?? "") + " clanServiceKing=" + (_activeSession.LastKnownClanServiceCurrentKingName ?? "") + " clanServicePlayerRelation=" + _activeSession.LastKnownClanServicePlayerRelation + " clanServiceKingRelation=" + _activeSession.LastKnownClanServiceCurrentKingRelation + " clanServiceGap=" + _activeSession.LastKnownClanServiceRelationGap + " romanticPrivateRelation=" + _activeSession.LastKnownRomanticInteractionPrivateRelation + " greetingPrivateRelation=" + _activeSession.LastKnownGreetingPrivateRelation + " banditSettlement=" + (_activeSession.LastKnownBanditSuppressionSettlementName ?? "") + " banditCount=" + _activeSession.LastKnownBanditSuppressionBanditCount + " banditRadius=" + _activeSession.LastKnownBanditSuppressionRadius.ToString("0.0") + " banditTrust=" + _activeSession.LastKnownBanditSuppressionTrust + " banditPrivateRelation=" + _activeSession.LastKnownBanditSuppressionPrivateRelation + " territorialKingdom=" + (_activeSession.LastKnownTerritorialInterrogationKingdomName ?? "") + " territorialSettlement=" + (_activeSession.LastKnownTerritorialInterrogationSettlementName ?? "") + " territorialDistance=" + _activeSession.LastKnownTerritorialInterrogationSettlementDistance.ToString("0.0") + " territorialCulture=" + (_activeSession.LastKnownTerritorialInterrogationNpcCultureName ?? "") + " territorialCultureNotoriety=" + _activeSession.LastKnownTerritorialInterrogationCultureNotoriety + " revengeScore=" + _activeSession.LastKnownRevengePressureScore.ToString("0.0") + " revengeTarget=" + (_activeSession.LastKnownRevengeTargetName ?? "") + " revengeReason=" + (_activeSession.LastKnownRevengeReasonText ?? "") + " fiefProblems=" + _activeSession.LastKnownFiefProblemCount + " fief=" + (_activeSession.LastKnownFiefProblemName ?? "") + " fiefIssue=" + (_activeSession.LastKnownFiefIssueText ?? "") + " fiefLoyalty=" + _activeSession.LastKnownFiefLoyalty.ToString("0.0") + " fiefSecurity=" + _activeSession.LastKnownFiefSecurity.ToString("0.0") + " fiefGarrison=" + _activeSession.LastKnownFiefGarrisonCount + " allyInfluence=" + _activeSession.LastKnownClanInfluence.ToString("0.0") + " friendlyClans=" + _activeSession.LastKnownFriendlyClanCount + " hostileClans=" + _activeSession.LastKnownHostileClanCount);
	}

	private void TryOpenActiveEncounterWhenClose()
	{
		if (_activeSession == null || !string.Equals(_activeSession.Stage, "Chasing", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		MobileParty party = ResolveActiveParty();
		MobileParty mainParty = MobileParty.MainParty;
		if (party == null || mainParty == null || !party.IsActive || party.Party == null || PartyBase.MainParty == null)
		{
			return;
		}
		Hero hero = party.LeaderHero ?? ResolveHero(_activeSession.HeroId);
		if (!IsActiveHero(hero))
		{
			return;
		}
		if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party) || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
		{
			CancelActiveSession("chase_at_sea", releaseParty: true);
			return;
		}
		if (TryGetPlayerBusyReason(out string busyReason))
		{
			if (ShouldCancelActiveSessionForPlayerBusyReason(busyReason))
			{
				CancelActiveSession("player_busy:" + busyReason, releaseParty: true);
			}
			return;
		}
		if (party.MapEvent != null || party.CurrentSettlement != null)
		{
			return;
		}
		if (PlayerEncounter.Current != null || Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			return;
		}
		float distance = GetDirectDistanceToMainParty(party, mainParty);
		float triggerDistance = GetProactiveEncounterTriggerDistance(party);
		if (distance < 0f || distance > triggerDistance)
		{
			return;
		}
		OpenActiveEncounterMenu(party, hero, distance, triggerDistance);
	}

	private void OpenActiveEncounterMenu(MobileParty party, Hero hero, float distance, float triggerDistance)
	{
		if (party == null || hero == null || party.Party == null || PartyBase.MainParty == null)
		{
			return;
		}
		LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "enter", null, hero, party.Party);
		if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party) || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(MobileParty.MainParty))
		{
			LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "cancel_at_sea", null, hero, party.Party);
			CancelActiveSession("open_menu_at_sea", releaseParty: true);
			return;
		}
		if (!LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(hero, party.Party))
		{
			LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "cancel_ineligible_target", null, hero, party.Party);
			Logger.Log("ProactiveNpcRequest", "active encounter menu blocked because target is not an eligible kingdom noble. hero=" + GetHeroKey(hero) + " party=" + (party.StringId ?? ""));
			CancelActiveSession("ineligible_custom_lord_encounter_target", releaseParty: true);
			return;
		}
		if (LordEncounterBehavior.IsVillageRaidEncounterContext(hero) || LordEncounterBehavior.IsNativeEncounterActivityContext(hero))
		{
			LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "cancel_native_activity_context", null, hero, party.Party);
			Logger.Log("ProactiveNpcRequest", "active encounter menu blocked because target is in native raid/siege activity. hero=" + GetHeroKey(hero) + " party=" + (party.StringId ?? ""));
			CancelActiveSession("target_native_activity_context", releaseParty: true);
			return;
		}
		_activeSession.Stage = "OpeningMenu";
		_activeSession.EncounterOpenedAtHours = NowHours();
		try
		{
			if (party.DefaultBehavior == AiBehavior.EngageParty && party.TargetParty == MobileParty.MainParty)
			{
				party.SetMoveModeHold();
			}
			if (party.Ai != null)
			{
				party.Ai.RethinkAtNextHourlyTick = true;
				party.Ai.SetDoNotAttackMainParty(2);
			}
		}
		catch
		{
		}
		try
		{
			LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "restart_player_encounter_before", null, hero, party.Party);
			PlayerEncounterCompat.RestartPlayerEncounter(party.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
			LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "restart_player_encounter_after", null, hero, party.Party);
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "RestartPlayerEncounter failed: " + ex.Message);
			Logger.LogImmediate("Logic", "[EncounterDiag] stage=ProactiveNpcRequest.OpenActiveEncounterMenu | reason=restart_player_encounter_exception | error=" + ex);
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounter.Start();
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.SetupFields(PartyBase.MainParty, party.Party);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("ProactiveNpcRequest", "Start+SetupFields fallback failed: " + ex2.Message);
		}
		if (PlayerEncounter.Current == null)
		{
			_activeSession.Stage = "Chasing";
			LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "current_null_after_restart", null, hero, party.Party);
			Logger.Log("ProactiveNpcRequest", "close contact reached but PlayerEncounter.Current is null; distance=" + distance.ToString("0.00") + " trigger=" + triggerDistance.ToString("0.00"));
			return;
		}
		try
		{
			PlayerEncounter.LeaveEncounter = false;
			PlayerEncounter.Current.IsPlayerWaiting = false;
		}
		catch
		{
		}
		MarkEncounterOpenedInternal(hero);
		RecordActiveNeedTypeFatigue();
		LordEncounterBehavior.SetTarget(hero);
		LordEncounterBehavior.LogEncounterDiagnostic("ProactiveNpcRequest.OpenActiveEncounterMenu", "open_custom_menu", null, hero, party.Party);
		Logger.Log("ProactiveNpcRequest", "opening custom encounter menu hero=" + GetHeroKey(hero) + " party=" + (party.StringId ?? "") + " distance=" + distance.ToString("0.00") + " trigger=" + triggerDistance.ToString("0.00"));
		LordEncounterBehavior.OpenEncounterMenu(hero);
	}

	private void CleanupActiveSessionIfNeeded(string reason)
	{
		if (_activeSession == null)
		{
			return;
		}
		MobileParty party = ResolveActiveParty();
		if (party == null || !party.IsActive)
		{
			CancelActiveSession(reason + ":missing_party", releaseParty: false);
			return;
		}
		if (NowHours() > _activeSession.ExpiresAtHours)
		{
			CancelActiveSession(reason + ":expired", releaseParty: true);
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			CancelActiveSession(reason + ":missing_main_party", releaseParty: true);
			return;
		}
		if (string.Equals(_activeSession.Stage, "Chasing", StringComparison.OrdinalIgnoreCase)
			&& TryGetPlayerBusyReason(out string busyReason)
			&& ShouldCancelActiveSessionForPlayerBusyReason(busyReason))
		{
			CancelActiveSession(reason + ":player_busy:" + busyReason, releaseParty: true);
			return;
		}
		if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party) || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
		{
			CancelActiveSession(reason + ":sea", releaseParty: true);
			return;
		}
		float maxDistance = Math.Max(1f, mainParty.SeeingRange * 3f);
		float distance = GetDistanceToMainParty(party, mainParty);
		if (distance > maxDistance)
		{
			CancelActiveSession(reason + ":too_far", releaseParty: true);
		}
	}

	private void MarkEncounterOpenedInternal(Hero hero)
	{
		if (!IsActiveHero(hero))
		{
			return;
		}
		_activeSession.Stage = "Menu";
		_activeSession.EncounterOpenedAtHours = NowHours();
	}

	private void MarkConversationOpeningInternal(Hero hero, bool nativeConversation)
	{
		if (!IsActiveHero(hero))
		{
			return;
		}
		string fact = BuildTriggerSourceOpeningFact(hero);
		string needFact = BuildOpeningFact(hero);
		if (!string.IsNullOrWhiteSpace(needFact))
		{
			fact = string.IsNullOrWhiteSpace(fact) ? needFact : fact + "\n" + needFact;
		}
		string prompt = BuildOpeningPrompt(GetActiveNeedTypes());
		PendingOpeningFact pending = new PendingOpeningFact
		{
			HeroId = GetHeroKey(hero),
			ExtraFact = fact,
			PromptText = prompt,
			CreatedAtHours = NowHours()
		};
		if (nativeConversation)
		{
			_pendingNativeOpening = pending;
			_activeSession.Stage = "NativeConversationPending";
		}
		else
		{
			_pendingSceneOpening = pending;
			_activeSession.Stage = "SceneConversationPending";
		}
	}

	private string BuildTriggerSourceOpeningFact(Hero hero)
	{
		if (_activeSession == null)
		{
			return "";
		}
		string playerName = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家").Trim();
		string npcName = hero?.Name?.ToString() ?? "你";
		if (string.Equals(_activeSession.TriggerSource, TriggerSourceNotorietyDriven, StringComparison.OrdinalIgnoreCase))
		{
			return "[AFEF NPC行为补充] " + npcName + "曾听过" + playerName + "的事迹，因此在眼前有难处时想到了对方。";
		}
		return "[AFEF NPC行为补充] " + npcName + "是带着自己眼前的一桩事来找" + playerName + "的。";
	}

	private static string BuildNpcInitiatedRequestFact(string npcName, string playerName, string situation)
	{
		string normalizedSituation = situation?.Trim() ?? "";
		return "[AFEF NPC行为补充] " + npcName + "主动拦下" + playerName + "。" + normalizedSituation + "这是你自己的来意；" + playerName + "尚未答应任何事。";
	}

	private bool TryConsumePendingOpening(Hero hero, bool nativeConversation, out string extraFact, out string promptText)
	{
		extraFact = "";
		promptText = "";
		PendingOpeningFact pending = nativeConversation ? _pendingNativeOpening : _pendingSceneOpening;
		if (!PendingMatches(pending, hero))
		{
			return false;
		}
		extraFact = pending.ExtraFact ?? "";
		promptText = pending.PromptText ?? "";
		if (nativeConversation)
		{
			_pendingNativeOpening = null;
		}
		else
		{
			_pendingSceneOpening = null;
		}
		CompleteActiveForHeroInternal(hero, nativeConversation ? "native_opening_consumed" : "scene_opening_consumed");
		return !string.IsNullOrWhiteSpace(extraFact);
	}

	private bool TryPeekPendingOpening(bool nativeConversation, out Hero hero, out string extraFact, out string promptText)
	{
		hero = null;
		extraFact = "";
		promptText = "";
		PendingOpeningFact pending = nativeConversation ? _pendingNativeOpening : _pendingSceneOpening;
		if (pending == null || string.IsNullOrWhiteSpace(pending.HeroId))
		{
			return false;
		}
		hero = ResolveHero(pending.HeroId);
		if (!PendingMatches(pending, hero))
		{
			return false;
		}
		extraFact = pending.ExtraFact ?? "";
		promptText = pending.PromptText ?? "";
		return !string.IsNullOrWhiteSpace(extraFact);
	}

	private void CompleteActiveForHeroInternal(Hero hero, string reason)
	{
		if (!IsActiveHero(hero))
		{
			return;
		}
		ApplyCooldowns(hero);
		CancelActiveSession("complete:" + (reason ?? "unknown"), releaseParty: true);
	}

	private void ApplyCooldowns(Hero hero)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		float nowDays = NowDays();
		int heroCooldownDays = GetEffectiveHeroCooldownDays(settings);
		string heroKey = GetHeroKey(hero);
		if (!string.IsNullOrWhiteSpace(heroKey))
		{
			_heroCooldownUntilDays[heroKey] = nowDays + heroCooldownDays;
		}
	}

	private void RecordActiveNeedTypeFatigue()
	{
		if (_activeSession == null || _activeSession.NeedTypeFatigueRecorded)
		{
			return;
		}
		foreach (string needType in GetActiveNeedTypes())
		{
			RecordNeedTypeFatigue(needType, "map_encounter_opened");
		}
		_activeSession.NeedTypeFatigueRecorded = true;
	}

	private void RecordNeedTypeFatigue(string needType, string source)
	{
		string normalized = NormalizeNeedType(needType);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		int fatigueDays = GetEffectiveNeedTypeFatigueDays(normalized, settings);
		if (fatigueDays <= 0)
		{
			_needTypeFatigueUntilDays.Remove(normalized);
			return;
		}
		_needTypeFatigueUntilDays[normalized] = NowDays() + fatigueDays;
		Logger.Log("ProactiveNpcRequest", "type fatigue recorded source=" + (source ?? "") + " need=" + normalized + " durationDays=" + fatigueDays + " multiplier=" + GetEffectiveNeedTypeFatigueMultiplier(settings).ToString("0.##"));
	}

	private List<string> GetActiveNeedTypes()
	{
		if (_activeSession == null)
		{
			return new List<string> { NeedFoodShortage };
		}
		return NormalizeSingleNeedType(_activeSession.NeedTypes, string.IsNullOrWhiteSpace(_activeSession.NeedType) ? NeedFoodShortage : _activeSession.NeedType);
	}

	private void NormalizeActiveSessionSingleNeed()
	{
		if (_activeSession == null)
		{
			return;
		}
		List<string> normalized = NormalizeSingleNeedType(_activeSession.NeedTypes, string.IsNullOrWhiteSpace(_activeSession.NeedType) ? NeedFoodShortage : _activeSession.NeedType);
		_activeSession.NeedTypes = normalized;
		_activeSession.NeedType = normalized.Count > 0 ? normalized[0] : NeedFoodShortage;
	}

	private static List<string> NormalizeSingleNeedType(IEnumerable<string> needTypes, string fallbackNeedType)
	{
		List<string> normalized = NormalizeNeedTypes(needTypes, fallbackNeedType);
		string first = normalized.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(first))
		{
			first = NeedFoodShortage;
		}
		return new List<string> { first };
	}

	private static List<string> NormalizeNeedTypes(IEnumerable<string> needTypes, string fallbackNeedType)
	{
		List<string> result = new List<string>();
		try
		{
			if (needTypes != null)
			{
				foreach (string needType in needTypes)
				{
					string normalized = NormalizeNeedType(needType);
					if (!string.IsNullOrWhiteSpace(normalized) && !result.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
					{
						result.Add(normalized);
					}
				}
			}
			string fallback = NormalizeNeedType(fallbackNeedType);
			if (result.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
			{
				result.Add(fallback);
			}
			if (result.Any(x => string.Equals(x, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)))
			{
				result = result.Where(x => !string.Equals(x, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)).ToList();
			}
			return result.Count == 0 ? new List<string> { NeedFoodShortage } : result;
		}
		catch
		{
			return new List<string> { NeedFoodShortage };
		}
	}

	private static string NormalizeNeedType(string needType)
	{
		string text = (needType ?? "").Trim();
		if (string.Equals(text, NeedFoodShortage, StringComparison.OrdinalIgnoreCase))
		{
			return NeedFoodShortage;
		}
		if (string.Equals(text, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			return NeedMoneyShortage;
		}
		if (string.Equals(text, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			return NeedTroopShortage;
		}
		if (string.Equals(text, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			return NeedPrisonerOverload;
		}
		if (string.Equals(text, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			return NeedClanCaptive;
		}
		if (string.Equals(text, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			return NeedLowMorale;
		}
		if (string.Equals(text, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return NeedMountShortage;
		}
		if (string.Equals(text, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			return NeedOverburdened;
		}
		if (string.Equals(text, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			return NeedClanFinanceStrain;
		}
		if (string.Equals(text, NeedClanService, StringComparison.OrdinalIgnoreCase))
		{
			return NeedClanService;
		}
		if (string.Equals(text, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase))
		{
			return NeedRomanticInteraction;
		}
		if (string.Equals(text, NeedGreeting, StringComparison.OrdinalIgnoreCase))
		{
			return NeedGreeting;
		}
		if (string.Equals(text, NeedFriendship, StringComparison.OrdinalIgnoreCase))
		{
			return NeedFriendship;
		}
		if (string.Equals(text, NeedCourtship, StringComparison.OrdinalIgnoreCase))
		{
			return NeedCourtship;
		}
		if (string.Equals(text, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase))
		{
			return NeedArmyJoinRequest;
		}
		if (string.Equals(text, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return NeedBanditSuppression;
		}
		if (string.Equals(text, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return NeedPoliticalRivalSuppression;
		}
		if (string.Equals(text, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase))
		{
			return NeedSettlementPurchase;
		}
		if (string.Equals(text, NeedSettlementSale, StringComparison.OrdinalIgnoreCase))
		{
			return NeedSettlementSale;
		}
		if (string.Equals(text, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase))
		{
			return NeedTerritorialInterrogation;
		}
		if (string.Equals(text, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
		{
			return NeedMarriageAlliancePressure;
		}
		if (string.Equals(text, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			return NeedRevengePressure;
		}
		if (string.Equals(text, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			return NeedFiefGovernanceAnxiety;
		}
		if (string.Equals(text, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			return NeedAllySupport;
		}
		if (string.Equals(text, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			return NeedKingdomMercenaryInvite;
		}
		if (string.Equals(text, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			return NeedKingdomVassalInvite;
		}
		if (string.Equals(text, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			return NeedPoliticalAgenda;
		}
		if (string.Equals(text, NeedPolicySupport, StringComparison.OrdinalIgnoreCase))
		{
			return NeedPolicySupport;
		}
		if (string.Equals(text, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			return NeedPolicyDiscussion;
		}
		if (string.Equals(text, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return NeedDiplomacy;
		}
		return "";
	}

	private static float GetCandidateWeightedUrgency(ProactiveCandidate candidate)
	{
		if (candidate == null)
		{
			return 0f;
		}
		return Clamp(candidate.NeedUrgency, 0f, 100f)
			* Clamp(candidate.NeedTypeFatigueMultiplier, 0f, 1f)
			* Clamp(candidate.NeedTypeWeightMultiplier, 0f, 1f);
	}

	private static float GetEffectiveNeedTypeWeightMultiplier(string needType, DuelSettings settings, bool allowTestModeOverride = true)
	{
		if (allowTestModeOverride && settings?.ProactiveNpcRequestTestMode == true)
		{
			return 1f;
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return Clamp(settings?.ProactiveNpcRequestMountShortageWeight ?? 0.35f, 0f, 1f);
		}
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase))
		{
			return 0.35f;
		}
		return 1f;
	}

	private static int GetNeedPresentationPriority(string needType)
	{
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return 120;
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			return 110;
		}
		if (string.Equals(needType, NeedPolicySupport, StringComparison.OrdinalIgnoreCase))
		{
			return 89;
		}
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			return 70;
		}
		if (string.Equals(needType, NeedFoodShortage, StringComparison.OrdinalIgnoreCase))
		{
			return 100;
		}
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			return 90;
		}
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			return 85;
		}
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			return 84;
		}
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			return 82;
		}
		if (string.Equals(needType, NeedClanService, StringComparison.OrdinalIgnoreCase))
		{
			return 83;
		}
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase))
		{
			return 71;
		}
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase))
		{
			return 69;
		}
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase))
		{
			return 68;
		}
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase))
		{
			return 67;
		}
		if (string.Equals(needType, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase))
		{
			return 86;
		}
		if (string.Equals(needType, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return 87;
		}
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return 80;
		}
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase))
		{
			return 77;
		}
		if (string.Equals(needType, NeedSettlementSale, StringComparison.OrdinalIgnoreCase))
		{
			return 76;
		}
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase))
		{
			return 75;
		}
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			return 81;
		}
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			return 79;
		}
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			return 78;
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return 76;
		}
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			return 74;
		}
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
		{
			return 73;
		}
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			return 72;
		}
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			return 80;
		}
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			return 70;
		}
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			return 60;
		}
		return 0;
	}

	private static string JoinNeedTypesForLog(IEnumerable<string> needTypes, string fallbackNeedType)
	{
		return string.Join("|", NormalizeNeedTypes(needTypes, fallbackNeedType));
	}

	private void CancelActiveSession(string reason, bool releaseParty)
	{
		ProactiveNpcRequestSession session = _activeSession;
		MobileParty party = ResolveActiveParty();
		if (releaseParty)
		{
			ReleasePartyIfStillChasing(party);
		}
		Logger.Log("ProactiveNpcRequest", "cleared active request reason=" + (reason ?? "unknown") + " hero=" + (session?.HeroId ?? "") + " needs=" + JoinNeedTypesForLog(session?.NeedTypes, session?.NeedType));
		_activeSession = null;
		ClearActivePartyCache();
		_nextActiveEncounterProbeUtcTicks = 0L;
	}

	private void ReleasePartyIfStillChasing(MobileParty party)
	{
		try
		{
			if (party == null || MobileParty.MainParty == null)
			{
				return;
			}
			if (party.DefaultBehavior == AiBehavior.EngageParty && party.TargetParty == MobileParty.MainParty)
			{
				party.SetMoveModeHold();
				party.Ai.RethinkAtNextHourlyTick = true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "release party failed: " + ex.Message);
		}
	}

	private bool IsActiveParty(MobileParty party)
	{
		if (party == null || _activeSession == null)
		{
			return false;
		}
		string partyId = (party.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(partyId) && string.Equals(partyId, _activeSession.PartyId, StringComparison.OrdinalIgnoreCase);
	}

	private bool IsActiveHero(Hero hero)
	{
		if (hero == null || _activeSession == null)
		{
			return false;
		}
		return string.Equals(GetHeroKey(hero), _activeSession.HeroId, StringComparison.OrdinalIgnoreCase);
	}

	private bool PendingMatches(PendingOpeningFact pending, Hero hero)
	{
		return pending != null && hero != null && string.Equals(pending.HeroId, GetHeroKey(hero), StringComparison.OrdinalIgnoreCase);
	}

	private void CacheActiveParty(MobileParty party)
	{
		_activePartyCache = party;
		_activePartyCacheId = (party?.StringId ?? "").Trim();
	}

	private void ClearActivePartyCache()
	{
		_activePartyCache = null;
		_activePartyCacheId = "";
	}

	private MobileParty ResolveActiveParty()
	{
		if (_activeSession == null)
		{
			return null;
		}
		string partyId = (_activeSession.PartyId ?? "").Trim();
		if (_activePartyCache != null
			&& _activePartyCache.IsActive
			&& string.Equals(_activePartyCacheId, partyId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals((_activePartyCache.StringId ?? "").Trim(), partyId, StringComparison.OrdinalIgnoreCase))
		{
			return _activePartyCache;
		}
		ClearActivePartyCache();
		if (!string.IsNullOrWhiteSpace(partyId))
		{
			MobileParty party = MobileParty.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), partyId, StringComparison.OrdinalIgnoreCase));
			if (party != null)
			{
				CacheActiveParty(party);
				return party;
			}
		}
		Hero hero = ResolveHero(_activeSession.HeroId);
		MobileParty heroParty = hero?.PartyBelongedTo;
		if (heroParty != null)
		{
			CacheActiveParty(heroParty);
		}
		return heroParty;
	}

	private string BuildOpeningFact(Hero hero)
	{
		MobileParty party = hero?.PartyBelongedTo ?? ResolveActiveParty();
		string playerName = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家").Trim();
		string npcName = hero?.Name?.ToString() ?? "你";
		List<string> activeNeedTypes = GetActiveNeedTypes();
		string needType = activeNeedTypes.Count > 0 ? activeNeedTypes[0] : (string.IsNullOrWhiteSpace(_activeSession?.NeedType) ? NeedFoodShortage : _activeSession.NeedType);
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return BuildDiplomacyOpeningFact(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPoliticalAgendaOpeningFact(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedPolicySupport, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPolicySupportOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPolicyDiscussionOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			return BuildKingdomVassalInviteOpeningFact(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			return BuildKingdomMercenaryInviteOpeningFact(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPrisonerOverloadOpeningFact(party, playerName, npcName);
		}
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			return BuildClanCaptiveOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			return BuildClanFinanceStrainOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedClanService, StringComparison.OrdinalIgnoreCase))
		{
			return BuildClanServiceOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase))
		{
			return BuildRomanticInteractionOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase))
		{
			return BuildGreetingOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase))
		{
			return BuildFriendshipOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase))
		{
			return BuildCourtshipOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return BuildBanditSuppressionOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPoliticalRivalSuppressionOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase))
		{
			return BuildSettlementPurchaseOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedSettlementSale, StringComparison.OrdinalIgnoreCase))
		{
			return BuildSettlementSaleOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase))
		{
			return BuildTerritorialInterrogationOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
		{
			return BuildMarriageAlliancePressureOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			return BuildRevengePressureOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			return BuildFiefGovernanceAnxietyOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			return BuildAllySupportOpeningFact(playerName, npcName);
		}
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			return BuildOverburdenedOpeningFact(party, playerName, npcName);
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return BuildMountShortageOpeningFact(party, playerName, npcName);
		}
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			return BuildLowMoraleOpeningFact(party, playerName, npcName);
		}
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			return BuildTroopShortageOpeningFact(party, playerName, npcName);
		}
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			return BuildMoneyShortageOpeningFact(party, playerName, npcName);
		}
		return BuildFoodShortageOpeningFact(party, playerName, npcName);
	}

	private string BuildOpeningNeedSummary(string needType, Hero hero, MobileParty party, string playerName, string npcName)
	{
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
		{
			string firstName = (_activeSession?.LastKnownMarriageFirstUnmarriedName ?? "").Trim();
			return "你的家族正为传承和婚配的事忧心" + (string.IsNullOrWhiteSpace(firstName) ? "" : "，尤其牵挂" + firstName) + "；你想与" + playerName + "谈谈联姻或家族间的长期互助";
		}
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			string reason = (_activeSession?.LastKnownRevengeReasonText ?? "").Trim();
			string target = (_activeSession?.LastKnownRevengeTargetName ?? "").Trim();
			string targetText = string.IsNullOrWhiteSpace(target) ? "" : "，矛头可能指向 " + target;
			return "你的家族正因" + (string.IsNullOrWhiteSpace(reason) ? "近来的风波" : reason) + "承受压力" + targetText + "；你想请" + playerName + "帮忙想个办法";
		}
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			string fief = (_activeSession?.LastKnownFiefProblemName ?? "").Trim();
			string issue = (_activeSession?.LastKnownFiefIssueText ?? "").Trim();
			string fiefText = string.IsNullOrWhiteSpace(fief) ? "某处封地" : fief;
			return fiefText + "正受" + (string.IsNullOrWhiteSpace(issue) ? "内外事务的困扰" : issue) + "困扰；你想请" + playerName + "帮忙稳住局面";
		}
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			return "你的家族在王国内显得孤立，正需要可信的盟友；你想与" + playerName + "谈谈彼此照应";
		}
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return BuildDiplomacyOpeningSummary(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveHeroKingdom(hero);
			string kingdomName = ResolveKnownKingdomName(kingdom);
			return "你和" + playerName + "同属" + kingdomName + "，王国内有一件议事正需要有人表态；你想请" + playerName + "支持你的立场";
		}
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveHeroKingdom(hero);
			string kingdomName = ResolveKnownKingdomName(kingdom);
			return "你是" + kingdomName + "的国王。王国正需要愿意长期分担责任的家族；你认为" + playerName + "值得一谈，想邀请对方为王国效力";
		}
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveHeroKingdom(hero);
			string kingdomName = ResolveKnownKingdomName(kingdom);
			string authorityText = IsKingdomLeader(hero, kingdom) ? "你是国王" : "你是正式领主";
			return authorityText + "，可以代表" + kingdomName + "邀请雇佣兵。王国眼下需要能立刻上阵的人手；你想邀请" + playerName + "前来效力";
		}
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "你的部队人手单薄，难以独自应付眼前局面；你想向" + playerName + "问问有没有可借的助力";
		}
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			int heroPrisonerCount = SafeHeroPrisonerCount(party);
			if (party == null && _activeSession != null)
			{
				heroPrisonerCount = _activeSession.LastKnownHeroPrisonerCount;
			}
			return "你的队伍带着太多俘虏，已难以妥善看守" + (heroPrisonerCount > 0 ? "，其中还有身份要紧的人" : "") + "；你想请" + playerName + "帮忙想办法";
		}
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			string captiveName = (_activeSession?.LastKnownCaptiveClanHeroName ?? "").Trim();
			string holderName = (_activeSession?.LastKnownCaptiveClanHeroHolderName ?? "").Trim();
			bool leaderHeld = _activeSession?.LastKnownCaptiveClanLeaderHeld == true;
			string captiveText = string.IsNullOrWhiteSpace(captiveName) ? "一名家族成员" : captiveName;
			string holderText = string.IsNullOrWhiteSpace(holderName) ? "" : "，目前看押方似乎是" + holderName;
			string leaderText = leaderHeld ? "，其中包括家族领袖或关键成员" : "";
			return captiveText + "被俘" + holderText + leaderText + "；你想请" + playerName + "帮忙打听或设法营救";
		}
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			return "你的家族账目吃紧，维持开销十分艰难；你想向" + playerName + "寻求周转的办法";
		}
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			return "你的队伍辎重过多，行军十分吃力；你想请" + playerName + "帮忙分担或转运一部分货物";
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "你的队伍缺少坐骑，行军明显受拖累；你想向" + playerName + "求购马匹，或请对方帮忙解围";
		}
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			return "队中人心浮动，需要尽快稳住军心；你想请" + playerName + "帮忙渡过这段低潮";
		}
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "军饷和行军开销让你难以周转；你想与" + playerName + "商量一条出路";
		}
		return "你的部队粮食将尽；你想向" + playerName + "求购粮食或请求援助";
	}

	private string BuildDiplomacyOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom npcKingdom = hero?.Clan?.Kingdom;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		string npcKingdomName = ResolveKnownKingdomName(npcKingdom);
		string playerKingdomName = ResolveKnownKingdomName(playerKingdom);
		string topic = ResolveDiplomacyRequestLabel(npcKingdom, playerKingdom);
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你是" + npcKingdomName + "的国王，" + playerName + "是" + playerKingdomName + "的国王。两国之间有必要谈谈" + topic + "，你想当面探一探对方的口风。 ");
	}

	private string BuildPoliticalAgendaOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom kingdom = hero?.Clan?.Kingdom;
		string kingdomName = ResolveKnownKingdomName(kingdom);
		string agendaContext = VoteDealBehavior.BuildPendingDecisionsContext(hero);
		string text = BuildNpcInitiatedRequestFact(npcName, playerName, "你和" + playerName + "同属" + kingdomName + "。王国内有一件议事正需要有人表态，你想请对方支持你的立场。 ");
		if (!string.IsNullOrWhiteSpace(agendaContext))
		{
			text += "\n" + agendaContext;
		}
		return text;
	}

	private string BuildPolicySupportOpeningFact(string playerName, string npcName)
	{
		string kingdomName = (_activeSession?.LastKnownPolicySupportKingdomName ?? "该王国").Trim();
		string policyName = (_activeSession?.LastKnownPolicySupportPolicyName ?? "某项政策").Trim();
		string description = (_activeSession?.LastKnownPolicySupportDescription ?? "无").Trim();
		string effects = (_activeSession?.LastKnownPolicySupportEffects ?? "无").Trim();
		bool hasPendingDecision = _activeSession?.LastKnownPolicySupportHasPendingDecision == true;
		string decisionState = hasPendingDecision ? "王国内正有人议论此事。" : "你想先为此事争取支持。";
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你与" + playerName + "同属" + kingdomName + "，两家向来交好。你一直主张《" + policyName + "》。此事关乎：" + description + "。若能施行，可能会带来：" + effects + "。" + decisionState);
	}

	private string BuildPolicyDiscussionOpeningFact(string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, BuildPolicyDiscussionSituation(new PolicyDiscussionSnapshot
		{
			PolicyId = _activeSession?.LastKnownPolicyDiscussionPolicyId,
			PolicyName = _activeSession?.LastKnownPolicyDiscussionPolicyName,
			PolicyContent = _activeSession?.LastKnownPolicyDiscussionPolicyContent,
			KingdomName = _activeSession?.LastKnownPolicyDiscussionKingdomName,
			PublishedDay = _activeSession?.LastKnownPolicyDiscussionPublishedDay ?? 0
		}, npcName, playerName));
	}

	private static string BuildDiplomacyOpeningSummary(Hero hero, string playerName, string npcName)
	{
		Kingdom npcKingdom = hero?.Clan?.Kingdom;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		string topic = ResolveDiplomacyRequestLabel(npcKingdom, playerKingdom);
		return "你和" + playerName + "分别是两个王国的国王；你主动来谈" + topic + "，可以提出条件，但不要主动宣战，也不要假定玩家已经同意";
	}

	private static string ResolveDiplomacyRequestLabel(Kingdom npcKingdom, Kingdom playerKingdom)
	{
		try
		{
			if (npcKingdom != null && playerKingdom != null && FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom))
			{
				return "议和";
			}
			if (HasCommonEnemy(npcKingdom, playerKingdom))
			{
				return "结盟";
			}
			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (npcKingdom != null && playerKingdom != null && !BannerlordApiCompat.HasTradeAgreement(tradeBeh, npcKingdom, playerKingdom))
			{
				return "通商";
			}
		}
		catch
		{
		}
		return "国王间外交";
	}

	private static bool HasCommonEnemy(Kingdom first, Kingdom second)
	{
		try
		{
			if (first == null || second == null)
			{
				return false;
			}
			foreach (Kingdom kingdom in Kingdom.All)
			{
				if (kingdom == null || kingdom.IsEliminated || kingdom == first || kingdom == second)
				{
					continue;
				}
				if (FactionManager.IsAtWarAgainstFaction(first, kingdom) && FactionManager.IsAtWarAgainstFaction(second, kingdom))
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

	private string BuildKingdomMercenaryInviteOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom kingdom = ResolveHeroKingdom(hero);
		string kingdomName = ResolveKnownKingdomName(kingdom);
		string authorityText = IsKingdomLeader(hero, kingdom)
			? "你是" + kingdomName + "的国王"
			: "你可以代表" + kingdomName + "发出邀请";
		return BuildNpcInitiatedRequestFact(npcName, playerName, authorityText + "。" + kingdomName + "眼下需要能立刻上阵的人手；你想邀" + playerName + "以雇佣兵身份前来效力。 ");
	}

	private string BuildKingdomVassalInviteOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom kingdom = ResolveHeroKingdom(hero);
		string kingdomName = ResolveKnownKingdomName(kingdom);
		string playerState = IsPlayerMercenaryOfKingdom(kingdom)
			? playerName + "当前已经以雇佣兵身份为" + kingdomName + "效力"
			: playerName + "目前尚未向别的王国效力";
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你是" + kingdomName + "的国王。王国需要愿意长期分担责任的家族。" + playerState + "；你想邀" + playerName + "为" + kingdomName + "效力。 ");
	}

	private string BuildClanCaptiveOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedClanCaptive, null, ResolveActiveParty(), playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildLowMoraleOpeningFact(MobileParty party, string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedLowMorale, null, party, playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildMountShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedMountShortage, null, party, playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildOverburdenedOpeningFact(MobileParty party, string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedOverburdened, null, party, playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildClanFinanceStrainOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedClanFinanceStrain, null, ResolveActiveParty(), playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildClanServiceOpeningFact(string playerName, string npcName)
	{
		string clanName = (_activeSession?.LastKnownClanServiceTargetClanName ?? npcName).Trim();
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你是" + clanName + "的族长。家族没有封地，也更愿意投向" + playerName + "的麾下；你想替家族求一条效力的路。 ");
	}

	private string BuildRomanticInteractionOpeningFact(string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你与" + playerName + "相识已久，心中有些牵挂想亲口说出来。 ");
	}

	private string BuildGreetingOpeningFact(string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你与" + playerName + "颇为熟稔，只是想问候近况。 ");
	}

	private string BuildFriendshipOpeningFact(string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你早已听闻" + playerName + "在这片土地上的名声，虽还不算熟悉，却觉得值得主动结识。 ");
	}

	private string BuildCourtshipOpeningFact(string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你早已听闻" + playerName + "的名声，虽还不算熟悉，却想亲口表达自己的好感。 ");
	}

	private string BuildBanditSuppressionOpeningFact(string playerName, string npcName)
	{
		string settlementName = (_activeSession?.LastKnownBanditSuppressionSettlementName ?? "某处封地").Trim();
		return BuildNpcInitiatedRequestFact(npcName, playerName, settlementName + "附近强盗横行，百姓与商旅都不安生；你信得过" + playerName + "，想请对方相助清剿。 ");
	}

	private string BuildPoliticalRivalSuppressionOpeningFact(string playerName, string npcName)
	{
		string kingdomName = (_activeSession?.LastKnownPoliticalRivalSuppressionKingdomName ?? "该王国").Trim();
		string requesterClanName = (_activeSession?.LastKnownPoliticalRivalSuppressionRequesterClanName ?? npcName).Trim();
		string rivalClanName = (_activeSession?.LastKnownPoliticalRivalSuppressionRivalClanName ?? "同阵营家族").Trim();
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你是" + requesterClanName + "的族长，与你同属" + kingdomName + "。两家向来交好，而" + rivalClanName + "却与你积怨甚深；你想请" + playerName + "在王国事务上为你撑一撑。 ");
	}

	private string BuildSettlementPurchaseOpeningFact(string playerName, string npcName)
	{
		string kingdomName = (_activeSession?.LastKnownSettlementPurchaseKingdomName ?? "该王国").Trim();
		string playerFiefsText = (_activeSession?.LastKnownSettlementPurchasePlayerFiefsText ?? "城镇：无；城堡：无").Trim();
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你与" + playerName + "同属" + kingdomName + "，你想为家族添一处封地。" + playerName + "手中的封地包括：" + playerFiefsText + "。你想商谈购入其中一处。 ");
	}

	private string BuildSettlementSaleOpeningFact(string playerName, string npcName)
	{
		string kingdomName = (_activeSession?.LastKnownSettlementSaleKingdomName ?? "该王国").Trim();
		string settlementName = (_activeSession?.LastKnownSettlementSaleTargetSettlementName ?? "某处封地").Trim();
		string settlementType = (_activeSession?.LastKnownSettlementSaleTargetSettlementType ?? "封地").Trim();
		string foreignFactionName = (_activeSession?.LastKnownSettlementSaleForeignFactionName ?? "其他势力").Trim();
		string foreignSettlementName = (_activeSession?.LastKnownSettlementSaleForeignSettlementName ?? "边境封地").Trim();
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你与" + playerName + "同属" + kingdomName + "。" + settlementName + "是一处边境" + settlementType + "，收益不佳，又邻近" + foreignFactionName + "的" + foreignSettlementName + "；你想商谈将它转手。 ");
	}

	private string BuildTerritorialInterrogationOpeningFact(string playerName, string npcName)
	{
		string kingdomName = (_activeSession?.LastKnownTerritorialInterrogationKingdomName ?? "该王国").Trim();
		string settlementName = (_activeSession?.LastKnownTerritorialInterrogationSettlementName ?? "该王国定居点").Trim();
		string npcCultureName = (_activeSession?.LastKnownTerritorialInterrogationNpcCultureName ?? "该文化").Trim();
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你属于" + kingdomName + "的" + npcCultureName + "人。一个来历不明的异乡人出现在" + settlementName + "附近；你想问清" + playerName + "从何而来、来此意欲何为。保持警惕，但不可把怀疑当成罪证。 ");
	}

	private string BuildMarriageAlliancePressureOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedMarriageAlliancePressure, null, ResolveActiveParty(), playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildRevengePressureOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedRevengePressure, null, ResolveActiveParty(), playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildFiefGovernanceAnxietyOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedFiefGovernanceAnxiety, null, ResolveActiveParty(), playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildAllySupportOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedAllySupport, null, ResolveActiveParty(), playerName, npcName);
		return BuildNpcInitiatedRequestFact(npcName, playerName, summary + "。 ");
	}

	private string BuildFoodShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你的部队粮食将尽；你想向" + playerName + "求购粮食或请求援助。 ");
	}

	private string BuildMoneyShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "军饷和行军开销让你难以周转；你想与" + playerName + "商量一条出路。 ");
	}

	private string BuildTroopShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你的部队人手单薄，难以独自应付眼前局面；你想问问" + playerName + "有没有可借的助力。 ");
	}

	private string BuildPrisonerOverloadOpeningFact(MobileParty party, string playerName, string npcName)
	{
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你带着太多俘虏，已难以妥善看守；你想请" + playerName + "帮忙安排他们的去处。 ");
	}

	private static string BuildOpeningPrompt(IEnumerable<string> needTypes)
	{
		List<string> normalized = NormalizeSingleNeedType(needTypes, NeedFoodShortage);
		return BuildOpeningPrompt(normalized.Count > 0 ? normalized[0] : NeedFoodShortage);
	}

	private static string BuildOpeningPrompt(string needType)
	{
		return AIConfigHandler.GetProactiveNpcRequestOpeningPrompt(NormalizeNeedType(needType));
	}

	public static bool TryGetPlayerInteractionBusyReasonForExternal(bool allowSea, bool allowSettlement, out string reason)
	{
		reason = "";
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				reason = "missing_main_party";
				return true;
			}
			if (Mission.Current != null)
			{
				reason = "mission_active";
				return true;
			}
			if (PlayerEncounterCompat.IsInPostBattleResultFlow())
			{
				reason = "post_battle_result";
				return true;
			}
			if (TryGetPlayerNativeActivityBusyReason(mainParty, out reason))
			{
				return true;
			}
			if (mainParty.MapEvent != null)
			{
				reason = "main_party_map_event";
				return true;
			}
			if (!allowSettlement && mainParty.CurrentSettlement != null)
			{
				reason = "main_party_in_settlement";
				return true;
			}
			if (mainParty.IsInRaftState)
			{
				reason = "main_party_raft";
				return true;
			}
			if (!allowSea && MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
			{
				reason = "main_party_at_sea";
				return true;
			}
			if (PlayerEncounter.Current != null)
			{
				reason = "player_encounter_active";
				return true;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				reason = "conversation_in_progress";
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return true;
		}
	}

	private static bool TryGetPlayerBusyReason(out string reason)
	{
		reason = "";
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				reason = "missing_main_party";
				return true;
			}
			if (TryGetPlayerNativeActivityBusyReason(mainParty, out reason))
			{
				return true;
			}
			if (mainParty.MapEvent != null)
			{
				reason = "main_party_map_event";
				return true;
			}
			if (mainParty.CurrentSettlement != null)
			{
				reason = "main_party_in_settlement";
				return true;
			}
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
			{
				reason = mainParty.IsInRaftState ? "main_party_raft" : "main_party_at_sea";
				return true;
			}
			if (PlayerEncounter.Current != null)
			{
				reason = "player_encounter_active";
				return true;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				reason = "conversation_in_progress";
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return true;
		}
	}

	private static bool TryGetPlayerNativeActivityBusyReason(MobileParty mainParty, out string reason)
	{
		reason = "";
		try
		{
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				reason = "player_siege_event";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mainParty?.MapEvent != null && IsNativeActivityMapEvent(mainParty.MapEvent))
			{
				reason = "main_party_native_activity_map_event";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mainParty?.Party?.MapEvent != null && IsNativeActivityMapEvent(mainParty.Party.MapEvent))
			{
				reason = "main_party_native_activity_party_map_event";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mainParty != null && (mainParty.SiegeEvent != null || mainParty.BesiegedSettlement != null || mainParty.BesiegerCamp != null))
			{
				reason = "main_party_siege";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mainParty?.Party?.SiegeEvent != null)
			{
				reason = "main_party_party_siege";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mainParty != null && IsSettlementCombatBehavior(mainParty.DefaultBehavior))
			{
				reason = "main_party_default_" + mainParty.DefaultBehavior;
				return true;
			}
			if (mainParty != null && IsSettlementCombatBehavior(mainParty.ShortTermBehavior))
			{
				reason = "main_party_short_" + mainParty.ShortTermBehavior;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement targetSettlement = mainParty?.TargetSettlement
				?? mainParty?.ShortTermTargetSettlement
				?? mainParty?.BesiegedSettlement
				?? mainParty?.CurrentSettlement;
			if (IsActivePlayerSiegeSettlement(targetSettlement))
			{
				reason = "target_settlement_siege";
				return true;
			}
			if (IsActivePlayerRaidSettlement(targetSettlement, mainParty))
			{
				reason = "target_village_raid";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (LordEncounterBehavior.IsVillageRaidEncounterContext())
			{
				reason = "village_raid_context";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (LordEncounterBehavior.IsNativeEncounterActivityContext())
			{
				reason = "native_activity_context";
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool ShouldCancelActiveSessionForPlayerBusyReason(string reason)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			return false;
		}
		return reason.IndexOf("siege", StringComparison.OrdinalIgnoreCase) >= 0
			|| reason.IndexOf("besieg", StringComparison.OrdinalIgnoreCase) >= 0
			|| reason.IndexOf("raid", StringComparison.OrdinalIgnoreCase) >= 0
			|| reason.IndexOf("native_activity", StringComparison.OrdinalIgnoreCase) >= 0
			|| reason.IndexOf("map_event", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsSettlementCombatBehavior(AiBehavior behavior)
	{
		return behavior == AiBehavior.BesiegeSettlement
			|| behavior == AiBehavior.AssaultSettlement
			|| behavior == AiBehavior.RaidSettlement;
	}

	private static bool IsNativeActivityMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			if (mapEvent.IsRaid)
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

	private static bool IsActivePlayerSiegeSettlement(Settlement settlement)
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

	private static bool IsActivePlayerRaidSettlement(Settlement settlement, MobileParty mainParty)
	{
		try
		{
			if (settlement == null || !settlement.IsVillage || !settlement.IsUnderRaid)
			{
				return false;
			}
			return mainParty == null || settlement.LastAttackerParty == null || settlement.LastAttackerParty == mainParty;
		}
		catch
		{
			return false;
		}
	}

	private static int SafeFoodDays(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return 999;
			}
			return party.GetNumDaysForFoodToLast();
		}
		catch
		{
			return 999;
		}
	}

	private static bool DoesPlayerHaveFoodForFoodRequest()
	{
		MobileParty mainParty = MobileParty.MainParty;
		return mainParty != null && SafeFoodDays(mainParty) >= PlayerFoodDaysRequiredForFoodRequest;
	}

	private static bool DoesPlayerHaveTroopsForTroopRequest()
	{
		MobileParty mainParty = MobileParty.MainParty;
		int partySizeLimit = SafePartySizeLimit(mainParty);
		return partySizeLimit > 0
			&& SafeMemberCount(mainParty) / (float)partySizeLimit >= PlayerPartyFillRatioRequiredForTroopRequest;
	}

	private static int SafeMemberCount(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.Party?.NumberOfAllMembers ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafePartySizeLimit(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.Party?.PartySizeLimit ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafePrisonerCount(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.PrisonRoster?.TotalManCount ?? party?.Party?.PrisonRoster?.TotalManCount ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafePrisonerSizeLimit(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.Party?.PrisonerSizeLimit ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafeHeroPrisonerCount(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.PrisonRoster?.TotalHeroes ?? party?.Party?.PrisonRoster?.TotalHeroes ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafeAvailableWageBudget(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.GetAvailableWageBudget() ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafePartyTradeGold(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.PartyTradeGold ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafeTotalWage(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.TotalWage ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static float SafeUnpaidWages(MobileParty party)
	{
		try
		{
			return Clamp(party?.HasUnpaidWages ?? 0f, 0f, 1f);
		}
		catch
		{
			return 0f;
		}
	}

	private static float SafeMorale(MobileParty party)
	{
		try
		{
			return Clamp(party?.Morale ?? 100f, 0f, 100f);
		}
		catch
		{
			return 100f;
		}
	}

	private static int SafeInventoryCapacity(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.InventoryCapacity ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static float SafeTotalWeightCarried(MobileParty party)
	{
		try
		{
			return Math.Max(0f, party?.TotalWeightCarried ?? 0f);
		}
		catch
		{
			return 0f;
		}
	}

	private static int SafeMountCount(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.Party?.NumberOfMounts ?? party?.ItemRoster?.NumberOfMounts ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafePackAnimalCount(MobileParty party)
	{
		try
		{
			return Math.Max(0, party?.Party?.NumberOfPackAnimals ?? party?.ItemRoster?.NumberOfPackAnimals ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	// Match the native herding calculation so this request only targets mounts that already slow the player.
	private static bool HasPlayerSurplusMountsCausingHerdPenalty()
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return false;
			}

			int totalMen = 0;
			int footmen = 0;
			int mounts = 0;
			int packAnimals = 0;
			int livestockAnimals = 0;
			AccumulateHerdingInputs(mainParty, ref totalMen, ref footmen, ref mounts, ref packAnimals, ref livestockAnimals);
			if (mainParty.AttachedParties != null)
			{
				foreach (MobileParty attachedParty in mainParty.AttachedParties)
				{
					AccumulateHerdingInputs(attachedParty, ref totalMen, ref footmen, ref mounts, ref packAnimals, ref livestockAnimals);
				}
			}

			int surplusMounts = Math.Max(0, mounts - Math.Min(footmen, mounts));
			int herdSize = packAnimals + livestockAnimals + surplusMounts;
			return surplusMounts > 0 && herdSize > totalMen;
		}
		catch
		{
			return false;
		}
	}

	private static void AccumulateHerdingInputs(
		MobileParty party,
		ref int totalMen,
		ref int footmen,
		ref int mounts,
		ref int packAnimals,
		ref int livestockAnimals)
	{
		if (party == null)
		{
			return;
		}

		totalMen += Math.Max(0, party.MemberRoster?.TotalManCount ?? 0);
		footmen += Math.Max(0, party.Party?.NumberOfMenWithoutHorse ?? 0);
		mounts += Math.Max(0, party.ItemRoster?.NumberOfMounts ?? 0);
		packAnimals += Math.Max(0, party.ItemRoster?.NumberOfPackAnimals ?? 0);
		livestockAnimals += Math.Max(0, party.ItemRoster?.NumberOfLivestockAnimals ?? 0);
	}

	private static int SafeClanGold(Clan clan)
	{
		try
		{
			return Math.Max(0, clan?.Gold ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int SafeClanDebtToKingdom(Clan clan)
	{
		try
		{
			return Math.Max(0, clan?.DebtToKingdom ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static float CalculateWageDays(int partyGold, int totalWage)
	{
		return totalWage <= 0 ? 999f : Math.Max(0f, partyGold / (float)totalWage);
	}

	private static float CalculatePartySizeRatio(int memberCount, int partySizeLimit)
	{
		return partySizeLimit <= 0 ? 1f : Clamp(memberCount / (float)partySizeLimit, 0f, 1f);
	}

	private static float CalculatePrisonerSizeRatio(int prisonerCount, int prisonerSizeLimit)
	{
		return prisonerSizeLimit <= 0 ? 0f : Clamp(prisonerCount / (float)prisonerSizeLimit, 0f, 2f);
	}

	private static float CalculateCarryRatio(float totalWeightCarried, int inventoryCapacity)
	{
		return inventoryCapacity <= 0 ? 0f : Clamp(totalWeightCarried / inventoryCapacity, 0f, 3f);
	}

	private static float CalculateAnimalRatio(int animalCount, int memberCount)
	{
		return memberCount <= 0 ? 0f : Clamp(animalCount / (float)memberCount, 0f, 3f);
	}

	private ClanCaptiveSnapshot GetCachedClanCaptiveSnapshot(Hero requester)
	{
		Clan clan = requester?.Clan;
		string clanKey = GetClanSnapshotCacheKey(clan);
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return BuildClanCaptiveSnapshot(requester);
		}
		float cacheHour = (float)Math.Floor(NowHours());
		if (_clanCaptiveSnapshotsByClan.TryGetValue(clanKey, out ClanCaptiveSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		ClanCaptiveSnapshot snapshot = BuildClanCaptiveSnapshot(requester);
		_clanCaptiveSnapshotsByClan[clanKey] = new ClanCaptiveSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		TrimHourlySnapshotCache(_clanCaptiveSnapshotsByClan, cacheHour, entry => entry?.SampledAtHour ?? float.MinValue);
		return snapshot;
	}

	private FiefGovernanceSnapshot GetCachedFiefGovernanceSnapshot(Clan clan, DuelSettings settings)
	{
		string clanKey = GetClanSnapshotCacheKey(clan);
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return BuildFiefGovernanceSnapshot(clan, settings);
		}
		float cacheHour = (float)Math.Floor(NowHours());
		int settingsFingerprint = BuildFiefGovernanceSettingsFingerprint(settings);
		if (_fiefGovernanceSnapshotsByClan.TryGetValue(clanKey, out FiefGovernanceSnapshotCacheEntry cached)
			&& cached != null
			&& cached.SettingsFingerprint == settingsFingerprint
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		FiefGovernanceSnapshot snapshot = BuildFiefGovernanceSnapshot(clan, settings);
		_fiefGovernanceSnapshotsByClan[clanKey] = new FiefGovernanceSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			SettingsFingerprint = settingsFingerprint,
			Snapshot = snapshot
		};
		TrimHourlySnapshotCache(_fiefGovernanceSnapshotsByClan, cacheHour, entry => entry?.SampledAtHour ?? float.MinValue);
		return snapshot;
	}

	private AllySupportSnapshot GetCachedAllySupportSnapshot(Clan clan, Kingdom kingdom, DuelSettings settings)
	{
		string clanKey = GetClanSnapshotCacheKey(clan);
		string kingdomKey = GetKingdomKey(kingdom);
		if (string.IsNullOrWhiteSpace(clanKey) || string.IsNullOrWhiteSpace(kingdomKey))
		{
			return BuildAllySupportSnapshot(clan, kingdom, settings);
		}
		string cacheKey = clanKey + "|" + kingdomKey;
		float cacheHour = (float)Math.Floor(NowHours());
		if (_allySupportSnapshotsByClan.TryGetValue(cacheKey, out AllySupportSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		AllySupportSnapshot snapshot = BuildAllySupportSnapshot(clan, kingdom, settings);
		_allySupportSnapshotsByClan[cacheKey] = new AllySupportSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		TrimHourlySnapshotCache(_allySupportSnapshotsByClan, cacheHour, entry => entry?.SampledAtHour ?? float.MinValue);
		return snapshot;
	}

	private KingdomManpowerNeedSnapshot GetCachedKingdomManpowerNeedSnapshot(Kingdom kingdom)
	{
		string kingdomKey = GetKingdomKey(kingdom);
		if (string.IsNullOrWhiteSpace(kingdomKey))
		{
			return BuildKingdomManpowerNeedSnapshot(kingdom);
		}
		float cacheHour = (float)Math.Floor(NowHours());
		if (_kingdomManpowerNeedSnapshotsByKingdom.TryGetValue(kingdomKey, out KingdomManpowerNeedSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		KingdomManpowerNeedSnapshot snapshot = BuildKingdomManpowerNeedSnapshot(kingdom);
		_kingdomManpowerNeedSnapshotsByKingdom[kingdomKey] = new KingdomManpowerNeedSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		TrimHourlySnapshotCache(_kingdomManpowerNeedSnapshotsByKingdom, cacheHour, entry => entry?.SampledAtHour ?? float.MinValue);
		return snapshot;
	}

	private static string GetClanSnapshotCacheKey(Clan clan)
	{
		return (clan?.StringId ?? "").Trim();
	}

	private static int BuildFiefGovernanceSettingsFingerprint(DuelSettings settings)
	{
		int loyaltyThreshold = Clamp(settings?.ProactiveNpcRequestFiefLoyaltyThreshold ?? 35, 0, 100);
		int securityThreshold = Clamp(settings?.ProactiveNpcRequestFiefSecurityThreshold ?? 35, 0, 100);
		int garrisonThreshold = Clamp(settings?.ProactiveNpcRequestFiefGarrisonThreshold ?? 80, 0, 1000);
		return loyaltyThreshold + securityThreshold * 101 + garrisonThreshold * 10201;
	}

	private static void TrimHourlySnapshotCache<TEntry>(Dictionary<string, TEntry> cache, float cacheHour, Func<TEntry, float> sampledAtHour)
	{
		if (cache == null || cache.Count <= 128 || sampledAtHour == null)
		{
			return;
		}
		foreach (string key in cache
			.Where(pair => pair.Value == null || sampledAtHour(pair.Value) < cacheHour - 1f)
			.Select(pair => pair.Key)
			.ToList())
		{
			cache.Remove(key);
		}
		if (cache.Count <= 128)
		{
			return;
		}
		foreach (string key in cache
			.OrderBy(pair => pair.Value == null ? float.MinValue : sampledAtHour(pair.Value))
			.Take(cache.Count - 128)
			.Select(pair => pair.Key)
			.ToList())
		{
			cache.Remove(key);
		}
	}

	private static ClanCaptiveSnapshot BuildClanCaptiveSnapshot(Hero requester)
	{
		ClanCaptiveSnapshot snapshot = new ClanCaptiveSnapshot();
		try
		{
			Clan clan = requester?.Clan;
			if (clan?.Heroes == null)
			{
				return snapshot;
			}
			List<Hero> captives = clan.Heroes
				.Where(h => IsRelevantCaptiveClanHero(h, requester))
				.OrderByDescending(h => h == clan.Leader)
				.ThenByDescending(h => h?.IsLord == true)
				.ToList();
			snapshot.Count = captives.Count;
			Hero first = captives.FirstOrDefault();
			if (first != null)
			{
				snapshot.FirstHeroName = GetHeroDisplayName(first);
				snapshot.FirstHolderName = ResolvePrisonerHolderName(first);
				snapshot.LeaderHeld = first == clan.Leader || captives.Any(h => h == clan.Leader);
			}
			else
			{
				snapshot.LeaderHeld = false;
			}
		}
		catch
		{
		}
		return snapshot;
	}

	private static bool IsRelevantCaptiveClanHero(Hero hero, Hero requester)
	{
		try
		{
			if (hero == null || requester == null || hero == requester || hero == Hero.MainHero || hero.IsDead || hero.IsChild)
			{
				return false;
			}
			if (hero.Clan == null || hero.Clan != requester.Clan)
			{
				return false;
			}
			return hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null;
		}
		catch
		{
			return false;
		}
	}

	private static string ResolvePrisonerHolderName(Hero captive)
	{
		try
		{
			PartyBase holder = captive?.PartyBelongedToAsPrisoner;
			if (holder == null)
			{
				return "";
			}
			if (holder == PartyBase.MainParty)
			{
				return MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家";
			}
			string leaderName = GetHeroDisplayName(holder.LeaderHero);
			if (!string.IsNullOrWhiteSpace(leaderName))
			{
				return leaderName;
			}
			return (holder.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static MarriageAllianceSnapshot BuildMarriageAllianceSnapshot(Hero requester)
	{
		MarriageAllianceSnapshot snapshot = new MarriageAllianceSnapshot();
		try
		{
			Clan clan = requester?.Clan;
			if (clan?.Heroes == null)
			{
				return snapshot;
			}
			List<Hero> adults = clan.Heroes
				.Where(h => IsRelevantAdultClanHeroForAlliance(h, clan))
				.OrderByDescending(h => h == requester)
				.ThenByDescending(h => h == clan.Leader)
				.ToList();
			List<Hero> unmarried = adults.Where(h => h?.Spouse == null).ToList();
			snapshot.AdultClanHeroCount = adults.Count;
			snapshot.UnmarriedAdultCount = unmarried.Count;
			snapshot.RequesterUnmarried = requester != null && unmarried.Contains(requester);
			snapshot.FirstUnmarriedName = GetHeroDisplayName(unmarried.FirstOrDefault());
		}
		catch
		{
		}
		return snapshot;
	}

	private static bool IsRelevantAdultClanHeroForAlliance(Hero hero, Clan clan)
	{
		try
		{
			if (hero == null || clan == null || hero.Clan != clan || hero == Hero.MainHero || hero.IsDead || hero.IsPrisoner || hero.IsChild || hero.PartyBelongedToAsPrisoner != null)
			{
				return false;
			}
			float adultAge = Campaign.Current?.Models?.AgeModel?.HeroComesOfAge ?? 18f;
			if (hero.Age > 0f && hero.Age < adultAge)
			{
				return false;
			}
			return hero.IsLord;
		}
		catch
		{
			return false;
		}
	}

	private static FiefGovernanceSnapshot BuildFiefGovernanceSnapshot(Clan clan, DuelSettings settings)
	{
		FiefGovernanceSnapshot snapshot = new FiefGovernanceSnapshot
		{
			LowestLoyalty = -1f,
			LowestSecurity = -1f,
			LowestGarrisonCount = -1,
			FirstProblemPriority = -1
		};
		try
		{
			if (clan?.Fiefs == null)
			{
				return snapshot;
			}
			int loyaltyThreshold = Clamp(settings?.ProactiveNpcRequestFiefLoyaltyThreshold ?? 35, 0, 100);
			int securityThreshold = Clamp(settings?.ProactiveNpcRequestFiefSecurityThreshold ?? 35, 0, 100);
			int garrisonThreshold = Clamp(settings?.ProactiveNpcRequestFiefGarrisonThreshold ?? 80, 0, 1000);
			foreach (Town town in clan.Fiefs)
			{
				if (town?.Settlement == null)
				{
					continue;
				}
				Settlement settlement = town.Settlement;
				float loyalty = SafeTownLoyalty(town);
				float security = SafeTownSecurity(town);
				int garrison = SafeTownGarrisonCount(town);
				bool underAttack = IsSettlementUnderAttack(settlement);
				List<string> issues = new List<string>();
				if (underAttack)
				{
					issues.Add("封地正在被围困或劫掠");
				}
				if (loyaltyThreshold > 0 && loyalty >= 0f && loyalty <= loyaltyThreshold)
				{
					issues.Add("忠诚偏低");
				}
				if (securityThreshold > 0 && security >= 0f && security <= securityThreshold)
				{
					issues.Add("治安偏低");
				}
				if (garrisonThreshold > 0 && garrison >= 0 && garrison <= garrisonThreshold)
				{
					issues.Add("驻军薄弱");
				}
				if (issues.Count <= 0)
				{
					continue;
				}
				snapshot.ProblemCount++;
				snapshot.UnderAttack = snapshot.UnderAttack || underAttack;
				int priority = (underAttack ? 1000 : 0)
					+ Math.Max(0, loyaltyThreshold - (int)Math.Max(0f, loyalty))
					+ Math.Max(0, securityThreshold - (int)Math.Max(0f, security))
					+ Math.Max(0, garrisonThreshold - Math.Max(0, garrison));
				if (priority > snapshot.FirstProblemPriority)
				{
					snapshot.FirstProblemPriority = priority;
					snapshot.FirstProblemName = GetSettlementDisplayName(settlement);
					snapshot.LowestLoyalty = loyalty;
					snapshot.LowestSecurity = security;
					snapshot.LowestGarrisonCount = garrison;
					snapshot.IssueText = string.Join("、", issues);
				}
			}
		}
		catch
		{
		}
		return snapshot;
	}

	private static AllySupportSnapshot BuildAllySupportSnapshot(Clan clan, Kingdom kingdom, DuelSettings settings)
	{
		AllySupportSnapshot snapshot = new AllySupportSnapshot();
		try
		{
			if (clan == null || kingdom == null || kingdom.Clans == null)
			{
				return snapshot;
			}
			snapshot.ClanInfluence = Clamp(clan.Influence, 0f, 9999f);
			foreach (Clan other in kingdom.Clans)
			{
				if (other == null || other == clan || other.IsEliminated || other.Kingdom != kingdom || other.IsUnderMercenaryService || other.IsClanTypeMercenary)
				{
					continue;
				}
				int relation = clan.GetRelationWithClan(other);
				if (relation >= 20)
				{
					snapshot.FriendlyClanCount++;
				}
				if (relation <= -20)
				{
					snapshot.HostileClanCount++;
				}
			}
		}
		catch
		{
		}
		return snapshot;
	}

	private static RevengePressureSnapshot BuildRevengePressureSnapshot(Hero requester, Kingdom targetKingdom, ClanCaptiveSnapshot captiveSnapshot, FiefGovernanceSnapshot fiefSnapshot)
	{
		RevengePressureSnapshot snapshot = new RevengePressureSnapshot();
		try
		{
			if (requester?.Clan == null)
			{
				return snapshot;
			}
			if (captiveSnapshot?.Count > 0)
			{
				snapshot.PressureScore = 72f + Math.Min(16f, captiveSnapshot.Count * 4f);
				snapshot.TargetName = captiveSnapshot.FirstHolderName;
				snapshot.ReasonText = "家族成员被俘";
				return snapshot;
			}
			if (fiefSnapshot?.UnderAttack == true)
			{
				snapshot.PressureScore = 70f + Math.Min(16f, fiefSnapshot.ProblemCount * 4f);
				snapshot.TargetName = ResolveFirstAttackerFactionName(requester.Clan);
				snapshot.ReasonText = "家族封地遭到围困或劫掠";
				return snapshot;
			}
			if (targetKingdom == null || targetKingdom.FactionsAtWarWith == null)
			{
				return snapshot;
			}
			int warCount = CountKingdomWars(targetKingdom);
			if (warCount <= 0)
			{
				return snapshot;
			}
			bool hasPressure = (fiefSnapshot?.ProblemCount ?? 0) > 0;
			if (!hasPressure)
			{
				MobileParty party = requester.PartyBelongedTo;
				int memberCount = SafeMemberCount(party);
				int sizeLimit = SafePartySizeLimit(party);
				hasPressure = memberCount > 0 && sizeLimit > 0 && CalculatePartySizeRatio(memberCount, sizeLimit) <= 0.45f;
			}
			if (!hasPressure)
			{
				return snapshot;
			}
			IFaction enemy = targetKingdom.FactionsAtWarWith.FirstOrDefault(f => f?.IsKingdomFaction == true);
			snapshot.PressureScore = 56f + Math.Min(18f, warCount * 5f);
			snapshot.TargetName = GetFactionDisplayName(enemy);
			snapshot.ReasonText = "战争压力已经影响家族安全";
		}
		catch
		{
		}
		return snapshot;
	}

	private static float SafeTownLoyalty(Town town)
	{
		try
		{
			return town?.Loyalty ?? -1f;
		}
		catch
		{
			return -1f;
		}
	}

	private static float SafeTownSecurity(Town town)
	{
		try
		{
			return town?.Security ?? -1f;
		}
		catch
		{
			return -1f;
		}
	}

	private static int SafeTownGarrisonCount(Town town)
	{
		try
		{
			return Math.Max(0, town?.GarrisonParty?.MemberRoster?.TotalManCount ?? town?.Settlement?.Town?.GarrisonParty?.MemberRoster?.TotalManCount ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsSettlementUnderAttack(Settlement settlement)
	{
		try
		{
			return settlement != null && (settlement.IsUnderRaid || settlement.IsUnderSiege || settlement.SiegeEvent != null);
		}
		catch
		{
			return false;
		}
	}

	private static string ResolveFirstAttackerFactionName(Clan clan)
	{
		try
		{
			foreach (Town town in clan?.Fiefs ?? Enumerable.Empty<Town>())
			{
				Settlement settlement = town?.Settlement;
				if (settlement == null || !IsSettlementUnderAttack(settlement))
				{
					continue;
				}
				string name = GetFactionDisplayName(settlement.LastAttackerParty?.MapFaction);
				if (!string.IsNullOrWhiteSpace(name))
				{
					return name;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static string GetSettlementDisplayName(Settlement settlement)
	{
		try
		{
			return (settlement?.Name?.ToString() ?? "").Trim();
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
			return (faction?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static int GetEffectiveMoneyGoldThreshold(DuelSettings settings)
	{
		int configured = Clamp(settings?.ProactiveNpcRequestMoneyGoldThreshold ?? 5000, 1, 50000);
		try
		{
			int vanilla = Campaign.Current?.Models?.ClanFinanceModel?.PartyGoldLowerThreshold ?? 5000;
			if (vanilla > 0)
			{
				return Math.Max(configured, vanilla);
			}
		}
		catch
		{
		}
		return configured;
	}

	private static int SafePlayerClanTier()
	{
		try
		{
			return Math.Max(0, (Clan.PlayerClan ?? Hero.MainHero?.Clan)?.Tier ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private int ResolveKnownPlayerClanTier()
	{
		return Math.Max(SafePlayerClanTier(), _activeSession?.PlayerClanTier ?? 0);
	}

	private static Kingdom ResolveHeroKingdom(Hero hero)
	{
		try
		{
			Kingdom kingdom = hero?.Clan?.Kingdom;
			if (kingdom != null)
			{
				return kingdom;
			}
		}
		catch
		{
		}
		try
		{
			return hero?.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsKingdomLeader(Hero hero, Kingdom kingdom)
	{
		try
		{
			return hero != null && kingdom != null && kingdom.Leader == hero;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanClanRepresentKingdom(Clan clan, Kingdom kingdom)
	{
		try
		{
			return clan != null
				&& kingdom != null
				&& !kingdom.IsEliminated
				&& clan.Kingdom == kingdom
				&& !clan.IsEliminated
				&& !clan.IsUnderMercenaryService
				&& !clan.IsClanTypeMercenary;
		}
		catch
		{
			return false;
		}
	}

	private static string GetKingdomKey(Kingdom kingdom)
	{
		try
		{
			return (kingdom?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
		try
		{
			return (kingdom?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private string ResolveKnownKingdomName(Kingdom kingdom)
	{
		string name = GetKingdomName(kingdom);
		if (!string.IsNullOrWhiteSpace(name))
		{
			return name;
		}
		name = (_activeSession?.TargetKingdomName ?? "").Trim();
		return string.IsNullOrWhiteSpace(name) ? "该王国" : name;
	}

	private string BuildKingdomManpowerNeedText(Kingdom kingdom)
	{
		KingdomManpowerNeedSnapshot snapshot = GetCachedKingdomManpowerNeedSnapshot(kingdom);
		bool useSession = _activeSession != null
			&& (kingdom == null
				|| string.IsNullOrWhiteSpace(_activeSession.TargetKingdomId)
				|| string.Equals(_activeSession.TargetKingdomId, GetKingdomKey(kingdom), StringComparison.OrdinalIgnoreCase));
		int formalVassals = useSession ? _activeSession.KingdomFormalVassalClanCount : snapshot.FormalVassalClanCount;
		int mercenaries = useSession ? _activeSession.KingdomMercenaryClanCount : snapshot.MercenaryClanCount;
		int fiefScore = useSession ? _activeSession.KingdomFiefScore : snapshot.FiefScore;
		int warCount = useSession ? _activeSession.KingdomWarKingdomCount : snapshot.WarKingdomCount;
		float powerRatio = useSession ? _activeSession.KingdomPowerRatioToEnemies : snapshot.PowerRatioToEnemies;
		int targetMercenaries = useSession ? _activeSession.KingdomTargetMercenaryClanCount : snapshot.TargetMercenaryClanCount;
		int targetVassals = useSession ? _activeSession.KingdomTargetVassalClanCount : snapshot.TargetVassalClanCount;
		string warText = warCount > 0
			? "，正与约 " + warCount + " 个王国交战，敌我力量比约 " + powerRatio.ToString("0.00")
			: "，当前没有主要王国战争";
		return "王国当前正式封臣家族（不含执政家族）约 " + formalVassals + "/" + targetVassals + "，雇佣兵家族约 " + mercenaries + "/" + targetMercenaries + "，封地负担约 " + fiefScore + warText + "。";
	}

	private static bool IsPlayerMercenaryOfKingdom(Kingdom kingdom)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			return playerClan != null && kingdom != null && playerClan.IsUnderMercenaryService && playerClan.Kingdom == kingdom;
		}
		catch
		{
			return false;
		}
	}

	private static KingdomManpowerNeedSnapshot BuildKingdomManpowerNeedSnapshot(Kingdom kingdom)
	{
		KingdomManpowerNeedSnapshot snapshot = new KingdomManpowerNeedSnapshot
		{
			PowerRatioToEnemies = 999f
		};
		try
		{
			if (kingdom == null || kingdom.IsEliminated)
			{
				return snapshot;
			}
			snapshot.WarKingdomCount = CountKingdomWars(kingdom);
			snapshot.PowerRatioToEnemies = SafePowerRatioToEnemies(kingdom, snapshot.WarKingdomCount);
			snapshot.FiefScore = CalculateKingdomFiefScore(kingdom);
			CountKingdomServiceClans(kingdom, snapshot);
			snapshot.TargetMercenaryClanCount = CalculateTargetMercenaryClanCount(snapshot);
			snapshot.TargetVassalClanCount = CalculateTargetVassalClanCount(snapshot);
			snapshot.NeedsMercenaries = snapshot.TargetMercenaryClanCount > 0 && snapshot.MercenaryClanCount < snapshot.TargetMercenaryClanCount;
			snapshot.NeedsVassals = snapshot.TargetVassalClanCount > 0 && snapshot.FormalVassalClanCount < snapshot.TargetVassalClanCount;
			snapshot.MercenaryNeedUrgency = CalculateKingdomMercenaryNeedUrgency(snapshot);
			snapshot.VassalNeedUrgency = CalculateKingdomVassalNeedUrgency(snapshot);
		}
		catch
		{
		}
		return snapshot;
	}

	private static int CountKingdomWars(Kingdom kingdom)
	{
		try
		{
			int count = 0;
			foreach (IFaction faction in kingdom?.FactionsAtWarWith ?? Enumerable.Empty<IFaction>())
			{
				if (faction?.IsKingdomFaction == true)
				{
					count++;
				}
			}
			return count;
		}
		catch
		{
			return 0;
		}
	}

	private static float SafePowerRatioToEnemies(Kingdom kingdom, int warKingdomCount)
	{
		try
		{
			if (kingdom == null || warKingdomCount <= 0)
			{
				return 999f;
			}
			float ratio = FactionHelper.GetPowerRatioToEnemies(kingdom);
			if (float.IsNaN(ratio) || float.IsInfinity(ratio))
			{
				return 999f;
			}
			return Clamp(ratio, 0f, 999f);
		}
		catch
		{
			return 999f;
		}
	}

	private static int CalculateKingdomFiefScore(Kingdom kingdom)
	{
		try
		{
			if (kingdom?.Fiefs == null)
			{
				return 0;
			}
			int score = 0;
			foreach (var fief in kingdom.Fiefs)
			{
				if (fief == null)
				{
					continue;
				}
				try
				{
					score += fief.IsTown ? 2 : 1;
				}
				catch
				{
					score++;
				}
			}
			return score;
		}
		catch
		{
			return 0;
		}
	}

	private static void CountKingdomServiceClans(Kingdom kingdom, KingdomManpowerNeedSnapshot snapshot)
	{
		try
		{
			if (kingdom?.Clans == null || snapshot == null)
			{
				return;
			}
			Clan rulingClan = kingdom.RulingClan;
			foreach (Clan clan in kingdom.Clans)
			{
				if (clan == null || clan.IsEliminated || clan.Kingdom != kingdom)
				{
					continue;
				}
				if (clan.IsUnderMercenaryService)
				{
					snapshot.MercenaryClanCount++;
					continue;
				}
				if (clan.IsClanTypeMercenary)
				{
					continue;
				}
				snapshot.FormalKingdomClanCount++;
				if (clan != rulingClan)
				{
					snapshot.FormalVassalClanCount++;
				}
			}
		}
		catch
		{
		}
	}

	private static int CalculateTargetMercenaryClanCount(KingdomManpowerNeedSnapshot snapshot)
	{
		if (snapshot == null || snapshot.WarKingdomCount <= 0 || snapshot.FiefScore <= 0 || snapshot.PowerRatioToEnemies > KingdomStrongEnoughToSkipMercenaryRatio)
		{
			return 0;
		}
		int target = 1;
		if (snapshot.WarKingdomCount >= 2 || snapshot.PowerRatioToEnemies < 1.1f || snapshot.FiefScore >= 10)
		{
			target = 2;
		}
		if (snapshot.WarKingdomCount >= 3 || snapshot.PowerRatioToEnemies < 0.75f || snapshot.FiefScore >= 18)
		{
			target = 3;
		}
		return Clamp(target, 0, 4);
	}

	private static int CalculateTargetVassalClanCount(KingdomManpowerNeedSnapshot snapshot)
	{
		if (snapshot == null || snapshot.FiefScore <= 0)
		{
			return 0;
		}
		int target = 1;
		if (snapshot.FiefScore >= 5)
		{
			target = 2;
		}
		if (snapshot.FiefScore >= 10)
		{
			target = 3;
		}
		if (snapshot.FiefScore >= 16)
		{
			target = 4;
		}
		if (snapshot.WarKingdomCount >= 2 || snapshot.PowerRatioToEnemies < 0.9f)
		{
			target++;
		}
		return Clamp(target, 0, 6);
	}

	private static float CalculateKingdomMercenaryNeedUrgency(KingdomManpowerNeedSnapshot snapshot)
	{
		if (snapshot == null || !snapshot.NeedsMercenaries)
		{
			return 0f;
		}
		float shortage = Math.Max(0, snapshot.TargetMercenaryClanCount - snapshot.MercenaryClanCount);
		float warPressure = Math.Min(3, snapshot.WarKingdomCount);
		float powerPressure = snapshot.PowerRatioToEnemies < 1.5f ? (1.5f - snapshot.PowerRatioToEnemies) * 4f : 0f;
		return 54f + shortage * 5f + warPressure + powerPressure;
	}

	private static float CalculateKingdomVassalNeedUrgency(KingdomManpowerNeedSnapshot snapshot)
	{
		if (snapshot == null || !snapshot.NeedsVassals)
		{
			return 0f;
		}
		float shortage = Math.Max(0, snapshot.TargetVassalClanCount - snapshot.FormalVassalClanCount);
		float fiefPressure = Math.Min(4f, snapshot.FiefScore / 4f);
		float powerPressure = snapshot.PowerRatioToEnemies < 1f ? (1f - snapshot.PowerRatioToEnemies) * 5f : 0f;
		return 64f + shortage * 4f + fiefPressure + powerPressure;
	}

	private static float GetDistanceToMainParty(MobileParty party, MobileParty mainParty)
	{
		try
		{
			float landRatio;
			return DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(party, mainParty, MobileParty.NavigationType.Default, out landRatio);
		}
		catch
		{
			try
			{
				return party.Position.Distance(mainParty.Position);
			}
			catch
			{
				return -1f;
			}
		}
	}

	private static float GetDirectDistanceToMainParty(MobileParty party, MobileParty mainParty)
	{
		try
		{
			if (party == null || mainParty == null)
			{
				return -1f;
			}
			return party.Position.Distance(mainParty.Position);
		}
		catch
		{
			return -1f;
		}
	}

	private static float GetProactiveEncounterTriggerDistance(MobileParty party)
	{
		try
		{
			float baseDistance = BannerlordApiCompat.GetNeededMaximumDistanceForEncounteringMobileParty(party);
			return Math.Max(0.75f, baseDistance * 1.35f);
		}
		catch
		{
			return 0.75f;
		}
	}

	private static Hero ResolveHero(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return Hero.Find(text) ?? Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

	private static Hero TryResolveHeroFromAgent(Agent agent)
	{
		try
		{
			return (agent?.Character as CharacterObject)?.HeroObject;
		}
		catch
		{
			return null;
		}
	}

	private static string GetHeroKey(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static string GetHeroDisplayName(Hero hero)
	{
		try
		{
			return (hero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static bool IsOnCooldown(Dictionary<string, float> dict, string key, float nowDays)
	{
		return !string.IsNullOrWhiteSpace(key) && dict != null && dict.TryGetValue(key, out float untilDays) && untilDays > nowDays;
	}

	private static Dictionary<string, float> NormalizeCooldownDictionary(Dictionary<string, float> source)
	{
		Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		if (source == null)
		{
			return result;
		}
		foreach (KeyValuePair<string, float> pair in source)
		{
			string key = (pair.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key))
			{
				result[key] = pair.Value;
			}
		}
		return result;
	}

	private float GetNeedTypeFatigueRemainingDays(string needType, float nowDays)
	{
		string normalized = NormalizeNeedType(needType);
		if (string.IsNullOrWhiteSpace(normalized)
			|| _needTypeFatigueUntilDays == null
			|| !_needTypeFatigueUntilDays.TryGetValue(normalized, out float untilDays))
		{
			return 0f;
		}
		return Math.Max(0f, untilDays - nowDays);
	}

	private void PruneExpiredNeedTypeFatigue(float nowDays)
	{
		if (_needTypeFatigueUntilDays == null || _needTypeFatigueUntilDays.Count <= 0)
		{
			return;
		}
		foreach (string key in _needTypeFatigueUntilDays.Where(pair => pair.Value <= nowDays).Select(pair => pair.Key).ToList())
		{
			_needTypeFatigueUntilDays.Remove(key);
		}
	}

	private static int GetEffectiveScanIntervalHours(DuelSettings settings)
	{
		int value = Clamp(settings?.ProactiveNpcRequestScanIntervalHours ?? 1, 1, 24);
		return settings?.ProactiveNpcRequestTestMode == true ? Math.Min(value, 1) : value;
	}

	private static int GetEffectiveChancePercent(DuelSettings settings)
	{
		int value = Clamp(settings?.ProactiveNpcRequestChancePercent ?? 80, 0, 100);
		return settings?.ProactiveNpcRequestTestMode == true ? Math.Max(value, 80) : value;
	}

	private static float GetEffectiveKnownMajorMultiplier(DuelSettings settings)
	{
		return Clamp(settings?.ProactiveNpcKnownMajorMultiplier ?? 2f, 1f, 5f);
	}

	private static float GetEffectiveNotorietyChanceMultiplier(DuelSettings settings)
	{
		return Clamp(settings?.ProactiveNpcNotorietyChanceMultiplier ?? 0.5f, 0f, 3f);
	}

	private static int GetEffectiveMinNeedUrgency(DuelSettings settings)
	{
		int value = Clamp(settings?.ProactiveNpcMinNeedUrgency ?? 50, 0, 100);
		return settings?.ProactiveNpcRequestTestMode == true ? 0 : value;
	}

	private static int GetEffectiveNeedTypeFatigueDays(string needType, DuelSettings settings)
	{
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase))
		{
			return Clamp(settings?.ProactiveNpcRequestRomanticInteractionGlobalCooldownDays ?? 21, 1, 120);
		}
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase))
		{
			return Clamp(settings?.ProactiveNpcRequestGreetingGlobalCooldownDays ?? 42, 1, 120);
		}
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			return 42;
		}
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase))
		{
			return 42;
		}
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase))
		{
			return 42;
		}
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase))
		{
			return Clamp(settings?.ProactiveNpcRequestTerritorialInterrogationGlobalCooldownDays ?? 42, 1, 180);
		}
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase))
		{
			return 48;
		}
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase))
		{
			return 42;
		}
		return Clamp(settings?.ProactiveNpcRequestTypeFatigueDays ?? 10, 0, 60);
	}

	private static float GetEffectiveNeedTypeFatigueMultiplier(DuelSettings settings)
	{
		return Clamp(settings?.ProactiveNpcRequestTypeFatigueMultiplier ?? 0.2f, 0f, 1f);
	}

	private static int GetEffectiveGlobalCooldownHours(DuelSettings settings)
	{
		int value = Clamp(settings?.ProactiveNpcRequestGlobalCooldownHours ?? 6, 0, 240);
		return settings?.ProactiveNpcRequestTestMode == true ? Math.Min(value, 6) : value;
	}

	private static int GetEffectiveHeroCooldownDays(DuelSettings settings)
	{
		int value = Clamp(settings?.ProactiveNpcRequestHeroCooldownDays ?? 3, 0, 60);
		return settings?.ProactiveNpcRequestTestMode == true ? Math.Min(value, 3) : value;
	}

	private static int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		return value > max ? max : value;
	}

	private static float Clamp(float value, float min, float max)
	{
		if (value < min)
		{
			return min;
		}
		return value > max ? max : value;
	}

	private static bool RollPercent(float chance)
	{
		chance = Clamp(chance, 0f, 100f);
		if (chance <= 0f)
		{
			return false;
		}
		if (chance >= 100f)
		{
			return true;
		}
		return MBRandom.RandomFloat < chance / 100f;
	}

	private static float NowHours()
	{
		try
		{
			return (float)CampaignTime.Now.ToHours;
		}
		catch
		{
			return 0f;
		}
	}

	private static float NowDays()
	{
		try
		{
			return (float)CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0f;
		}
	}

	private sealed class ProactiveNpcRequestStorage
	{
		public ProactiveNpcRequestSession ActiveSession { get; set; }
		public Dictionary<string, float> HeroCooldownUntilDays { get; set; }
		public Dictionary<string, float> NeedTypeFatigueUntilDays { get; set; }
		// Legacy v1 field: old saves stored a hard type cooldown here.
		public Dictionary<string, float> NeedCooldownUntilDays { get; set; }
		public float GlobalCooldownUntilHours { get; set; }
		public float LastScanHour { get; set; }
	}

	private sealed class ProactiveNpcRequestSession
	{
		public string Id { get; set; }
		public string HeroId { get; set; }
		public string PartyId { get; set; }
		public string NeedType { get; set; }
		public List<string> NeedTypes { get; set; }
		public string Stage { get; set; }
		public float CreatedAtHours { get; set; }
		public float ExpiresAtHours { get; set; }
		public float EncounterOpenedAtHours { get; set; }
		public string TriggerSource { get; set; }
		public bool KnownMajorBeforeRequest { get; set; }
		public int EffectiveNotorietyAtRequest { get; set; }
		public float NeedDrivenChance { get; set; }
		public float NotorietyDrivenChance { get; set; }
		public float SelectedNeedUrgency { get; set; }
		public float NeedTypeFatigueMultiplierAtSelection { get; set; } = 1f;
		public float NeedTypeWeightMultiplierAtSelection { get; set; } = 1f;
		public float NeedTypeFatigueRemainingDaysAtSelection { get; set; }
		public int LastKnownFoodDays { get; set; }
		public int LastKnownPartyGold { get; set; }
		public int LastKnownTotalWage { get; set; }
		public float LastKnownUnpaidWages { get; set; }
		public int LastKnownMemberCount { get; set; }
		public int LastKnownPartySizeLimit { get; set; }
		public int LastKnownAvailableWageBudget { get; set; }
		public int LastKnownPrisonerCount { get; set; }
		public int LastKnownPrisonerSizeLimit { get; set; }
		public int LastKnownHeroPrisonerCount { get; set; }
		public float LastKnownMorale { get; set; }
		public int LastKnownInventoryCapacity { get; set; }
		public float LastKnownTotalWeightCarried { get; set; }
		public float LastKnownCarryRatio { get; set; }
		public int LastKnownMountCount { get; set; }
		public int LastKnownPackAnimalCount { get; set; }
		public float LastKnownMountRatio { get; set; }
		public float LastKnownPackAnimalRatio { get; set; }
		public int LastKnownClanGold { get; set; }
		public int LastKnownClanDebtToKingdom { get; set; }
		public string LastKnownClanServiceTargetClanName { get; set; }
		public string LastKnownClanServiceCurrentKingName { get; set; }
		public int LastKnownClanServicePlayerRelation { get; set; }
		public int LastKnownClanServiceCurrentKingRelation { get; set; }
		public int LastKnownClanServiceRelationGap { get; set; }
		public int LastKnownRomanticInteractionPrivateRelation { get; set; }
		public int LastKnownGreetingPrivateRelation { get; set; }
		public string LastKnownBanditSuppressionSettlementName { get; set; }
		public int LastKnownBanditSuppressionBanditCount { get; set; }
		public float LastKnownBanditSuppressionRadius { get; set; }
		public int LastKnownBanditSuppressionTrust { get; set; }
		public int LastKnownBanditSuppressionPrivateRelation { get; set; }
		public string LastKnownPoliticalRivalSuppressionKingdomName { get; set; }
		public string LastKnownPoliticalRivalSuppressionRequesterClanName { get; set; }
		public int LastKnownPoliticalRivalSuppressionPlayerClanRelation { get; set; }
		public string LastKnownPoliticalRivalSuppressionRivalClanName { get; set; }
		public int LastKnownPoliticalRivalSuppressionRivalClanRelation { get; set; }
		public string LastKnownPolicySupportKingdomName { get; set; }
		public int LastKnownPolicySupportPlayerClanRelation { get; set; }
		public string LastKnownPolicySupportPolicyName { get; set; }
		public string LastKnownPolicySupportDescription { get; set; }
		public string LastKnownPolicySupportEffects { get; set; }
		public float LastKnownPolicySupportScore { get; set; }
		public bool LastKnownPolicySupportHasPendingDecision { get; set; }
		public string LastKnownPolicyDiscussionPolicyId { get; set; }
		public string LastKnownPolicyDiscussionPolicyName { get; set; }
		public string LastKnownPolicyDiscussionPolicyContent { get; set; }
		public string LastKnownPolicyDiscussionKingdomName { get; set; }
		public int LastKnownPolicyDiscussionPublishedDay { get; set; }
		public string LastKnownSettlementPurchaseKingdomName { get; set; }
		public int LastKnownSettlementPurchasePlayerTownCount { get; set; }
		public int LastKnownSettlementPurchasePlayerCastleCount { get; set; }
		public string LastKnownSettlementPurchasePlayerFiefsText { get; set; }
		public int LastKnownSettlementPurchaseNpcFiefCount { get; set; }
		public int LastKnownSettlementPurchaseNpcTownCount { get; set; }
		public int LastKnownSettlementPurchaseNpcCastleCount { get; set; }
		public string LastKnownSettlementSaleKingdomName { get; set; }
		public int LastKnownSettlementSalePlayerClanRelation { get; set; }
		public int LastKnownSettlementSaleNpcFiefCount { get; set; }
		public string LastKnownSettlementSaleTargetSettlementName { get; set; }
		public string LastKnownSettlementSaleTargetSettlementType { get; set; }
		public int LastKnownSettlementSaleTargetDailyIncome { get; set; }
		public int LastKnownSettlementSaleHighestFamilyDailyIncome { get; set; }
		public string LastKnownSettlementSaleForeignSettlementName { get; set; }
		public string LastKnownSettlementSaleForeignFactionName { get; set; }
		public float LastKnownSettlementSaleBorderDistance { get; set; }
		public float LastKnownSettlementSaleBorderRadius { get; set; }
		public string LastKnownTerritorialInterrogationKingdomName { get; set; }
		public string LastKnownTerritorialInterrogationSettlementName { get; set; }
		public float LastKnownTerritorialInterrogationSettlementDistance { get; set; }
		public string LastKnownTerritorialInterrogationNpcCultureName { get; set; }
		public int LastKnownTerritorialInterrogationCultureNotoriety { get; set; }
		public int LastKnownCaptiveClanHeroCount { get; set; }
		public string LastKnownCaptiveClanHeroName { get; set; }
		public string LastKnownCaptiveClanHeroHolderName { get; set; }
		public bool LastKnownCaptiveClanLeaderHeld { get; set; }
		public int LastKnownMarriageAdultClanHeroCount { get; set; }
		public int LastKnownMarriageUnmarriedAdultCount { get; set; }
		public string LastKnownMarriageFirstUnmarriedName { get; set; }
		public bool LastKnownMarriageRequesterUnmarried { get; set; }
		public float LastKnownRevengePressureScore { get; set; }
		public string LastKnownRevengeTargetName { get; set; }
		public string LastKnownRevengeReasonText { get; set; }
		public int LastKnownFiefProblemCount { get; set; }
		public string LastKnownFiefProblemName { get; set; }
		public float LastKnownFiefLoyalty { get; set; }
		public float LastKnownFiefSecurity { get; set; }
		public int LastKnownFiefGarrisonCount { get; set; }
		public string LastKnownFiefIssueText { get; set; }
		public bool LastKnownFiefUnderAttack { get; set; }
		public float LastKnownClanInfluence { get; set; }
		public int LastKnownFriendlyClanCount { get; set; }
		public int LastKnownHostileClanCount { get; set; }
		public string TargetKingdomId { get; set; }
		public string TargetKingdomName { get; set; }
		public int PlayerClanTier { get; set; }
		public bool TargetHeroIsKingdomLeader { get; set; }
		public int KingdomFormalVassalClanCount { get; set; }
		public int KingdomMercenaryClanCount { get; set; }
		public int KingdomFiefScore { get; set; }
		public int KingdomWarKingdomCount { get; set; }
		public float KingdomPowerRatioToEnemies { get; set; }
		public int KingdomTargetMercenaryClanCount { get; set; }
		public int KingdomTargetVassalClanCount { get; set; }
		public bool NeedTypeFatigueRecorded { get; set; }
		public bool IsTestFallback { get; set; }
	}

	private sealed class PendingOpeningFact
	{
		public string HeroId { get; set; }
		public string ExtraFact { get; set; }
		public string PromptText { get; set; }
		public float CreatedAtHours { get; set; }
	}

	private sealed class ProactiveCandidate
	{
		public MobileParty Party { get; set; }
		public Hero Hero { get; set; }
		public float Distance { get; set; }
		public int FoodDays { get; set; }
		public int PartyGold { get; set; }
		public int TotalWage { get; set; }
		public float UnpaidWages { get; set; }
		public float WageDays { get; set; }
		public int MemberCount { get; set; }
		public int PartySizeLimit { get; set; }
		public float PartySizeRatio { get; set; }
		public int AvailableWageBudget { get; set; }
		public int PrisonerCount { get; set; }
		public int PrisonerSizeLimit { get; set; }
		public int HeroPrisonerCount { get; set; }
		public float PrisonerSizeRatio { get; set; }
		public float Morale { get; set; }
		public int InventoryCapacity { get; set; }
		public float TotalWeightCarried { get; set; }
		public float CarryRatio { get; set; }
		public int MountCount { get; set; }
		public int PackAnimalCount { get; set; }
		public float MountRatio { get; set; }
		public float PackAnimalRatio { get; set; }
		public int ClanGold { get; set; }
		public int ClanDebtToKingdom { get; set; }
		public string ClanServiceTargetClanName { get; set; }
		public string ClanServiceCurrentKingName { get; set; }
		public int ClanServicePlayerRelation { get; set; }
		public int ClanServiceCurrentKingRelation { get; set; }
		public int ClanServiceRelationGap { get; set; }
		public int RomanticInteractionPrivateRelation { get; set; }
		public int GreetingPrivateRelation { get; set; }
		public string ArmyJoinRequestArmyName { get; set; }
		public float ArmyJoinRequestOwnStrength { get; set; }
		public float ArmyJoinRequestEnemyStrength { get; set; }
		public int ArmyJoinRequestEnemyKingdomCount { get; set; }
		public float ArmyJoinRequestOwnToEnemyRatio { get; set; }
		public string BanditSuppressionSettlementName { get; set; }
		public int BanditSuppressionBanditCount { get; set; }
		public float BanditSuppressionRadius { get; set; }
		public int BanditSuppressionTrust { get; set; }
		public int BanditSuppressionPrivateRelation { get; set; }
		public string PoliticalRivalSuppressionKingdomName { get; set; }
		public string PoliticalRivalSuppressionRequesterClanName { get; set; }
		public int PoliticalRivalSuppressionPlayerClanRelation { get; set; }
		public string PoliticalRivalSuppressionRivalClanName { get; set; }
		public int PoliticalRivalSuppressionRivalClanRelation { get; set; }
		public string PolicySupportKingdomName { get; set; }
		public int PolicySupportPlayerClanRelation { get; set; }
		public string PolicySupportPolicyName { get; set; }
		public string PolicySupportDescription { get; set; }
		public string PolicySupportEffects { get; set; }
		public float PolicySupportScore { get; set; }
		public bool PolicySupportHasPendingDecision { get; set; }
		public string PolicyDiscussionPolicyId { get; set; }
		public string PolicyDiscussionPolicyName { get; set; }
		public string PolicyDiscussionPolicyContent { get; set; }
		public string PolicyDiscussionKingdomName { get; set; }
		public int PolicyDiscussionPublishedDay { get; set; }
		public string SettlementPurchaseKingdomName { get; set; }
		public int SettlementPurchasePlayerTownCount { get; set; }
		public int SettlementPurchasePlayerCastleCount { get; set; }
		public string SettlementPurchasePlayerFiefsText { get; set; }
		public int SettlementPurchaseNpcFiefCount { get; set; }
		public int SettlementPurchaseNpcTownCount { get; set; }
		public int SettlementPurchaseNpcCastleCount { get; set; }
		public string SettlementSaleKingdomName { get; set; }
		public int SettlementSalePlayerClanRelation { get; set; }
		public int SettlementSaleNpcFiefCount { get; set; }
		public string SettlementSaleTargetSettlementName { get; set; }
		public string SettlementSaleTargetSettlementType { get; set; }
		public int SettlementSaleTargetDailyIncome { get; set; }
		public int SettlementSaleHighestFamilyDailyIncome { get; set; }
		public string SettlementSaleForeignSettlementName { get; set; }
		public string SettlementSaleForeignFactionName { get; set; }
		public float SettlementSaleBorderDistance { get; set; }
		public float SettlementSaleBorderRadius { get; set; }
		public bool TerritorialInterrogationEligible { get; set; }
		public string TerritorialInterrogationKingdomName { get; set; }
		public string TerritorialInterrogationSettlementName { get; set; }
		public float TerritorialInterrogationSettlementDistance { get; set; }
		public string TerritorialInterrogationNpcCultureName { get; set; }
		public int TerritorialInterrogationCultureNotoriety { get; set; }
		public int CaptiveClanHeroCount { get; set; }
		public string CaptiveClanHeroName { get; set; }
		public string CaptiveClanHeroHolderName { get; set; }
		public bool CaptiveClanLeaderHeld { get; set; }
		public int MarriageAdultClanHeroCount { get; set; }
		public int MarriageUnmarriedAdultCount { get; set; }
		public string MarriageFirstUnmarriedName { get; set; }
		public bool MarriageRequesterUnmarried { get; set; }
		public float RevengePressureScore { get; set; }
		public string RevengeTargetName { get; set; }
		public string RevengeReasonText { get; set; }
		public int FiefProblemCount { get; set; }
		public string FiefProblemName { get; set; }
		public float FiefLoyalty { get; set; }
		public float FiefSecurity { get; set; }
		public int FiefGarrisonCount { get; set; }
		public string FiefIssueText { get; set; }
		public bool FiefUnderAttack { get; set; }
		public float ClanInfluence { get; set; }
		public int FriendlyClanCount { get; set; }
		public int HostileClanCount { get; set; }
		public Kingdom TargetKingdom { get; set; }
		public string TargetKingdomId { get; set; }
		public string TargetKingdomName { get; set; }
		public int PlayerClanTier { get; set; }
		public bool TargetHeroIsKingdomLeader { get; set; }
		public bool TargetClanCanOfferKingdomService { get; set; }
		public int KingdomFormalVassalClanCount { get; set; }
		public int KingdomMercenaryClanCount { get; set; }
		public int KingdomFiefScore { get; set; }
		public int KingdomWarKingdomCount { get; set; }
		public float KingdomPowerRatioToEnemies { get; set; }
		public int KingdomTargetMercenaryClanCount { get; set; }
		public int KingdomTargetVassalClanCount { get; set; }
		public bool KingdomNeedsMercenaries { get; set; }
		public bool KingdomNeedsVassals { get; set; }
		public float KingdomMercenaryNeedUrgency { get; set; }
		public float KingdomVassalNeedUrgency { get; set; }
		public bool AtWarWithPlayer { get; set; }
		public string NeedType { get; set; }
		public List<string> NeedTypes { get; set; }
		public float NeedUrgency { get; set; }
		public string TriggerSource { get; set; }
		public bool KnownMajorBeforeRequest { get; set; }
		public int EffectiveNotorietyAtRequest { get; set; }
		public float NeedDrivenChance { get; set; }
		public float NotorietyDrivenChance { get; set; }
		public float SelectedNeedUrgency { get; set; }
		public float NeedTypeFatigueMultiplier { get; set; } = 1f;
		public float NeedTypeWeightMultiplier { get; set; } = 1f;
		public float IntrinsicNeedTypeWeightMultiplier { get; set; } = 1f;
		public float NeedTypeFatigueRemainingDays { get; set; }
		public bool IsTestFallback { get; set; }
	}

	private sealed class KingdomManpowerNeedSnapshot
	{
		public int FormalKingdomClanCount { get; set; }
		public int FormalVassalClanCount { get; set; }
		public int MercenaryClanCount { get; set; }
		public int FiefScore { get; set; }
		public int WarKingdomCount { get; set; }
		public float PowerRatioToEnemies { get; set; }
		public int TargetMercenaryClanCount { get; set; }
		public int TargetVassalClanCount { get; set; }
		public bool NeedsMercenaries { get; set; }
		public bool NeedsVassals { get; set; }
		public float MercenaryNeedUrgency { get; set; }
		public float VassalNeedUrgency { get; set; }
	}

	private sealed class KingdomManpowerNeedSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public KingdomManpowerNeedSnapshot Snapshot { get; set; }
	}

	private sealed class ClanServiceNeedSnapshot
	{
		public string TargetClanName { get; set; }
		public string CurrentKingName { get; set; }
		public int PlayerRelation { get; set; }
		public int CurrentKingRelation { get; set; }
		public int RelationGap { get; set; }
	}

	private sealed class ClanCaptiveSnapshot
	{
		public int Count { get; set; }
		public string FirstHeroName { get; set; }
		public string FirstHolderName { get; set; }
		public bool LeaderHeld { get; set; }
	}

	private sealed class ClanCaptiveSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public ClanCaptiveSnapshot Snapshot { get; set; }
	}

	private sealed class MarriageAllianceSnapshot
	{
		public int AdultClanHeroCount { get; set; }
		public int UnmarriedAdultCount { get; set; }
		public string FirstUnmarriedName { get; set; }
		public bool RequesterUnmarried { get; set; }
	}

	private sealed class RevengePressureSnapshot
	{
		public float PressureScore { get; set; }
		public string TargetName { get; set; }
		public string ReasonText { get; set; }
	}

	private sealed class FiefGovernanceSnapshot
	{
		public int ProblemCount { get; set; }
		public string FirstProblemName { get; set; }
		public float LowestLoyalty { get; set; }
		public float LowestSecurity { get; set; }
		public int LowestGarrisonCount { get; set; }
		public string IssueText { get; set; }
		public bool UnderAttack { get; set; }
		public int FirstProblemPriority { get; set; }
	}

	private sealed class FiefGovernanceSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public int SettingsFingerprint { get; set; }
		public FiefGovernanceSnapshot Snapshot { get; set; }
	}

	private sealed class AllySupportSnapshot
	{
		public float ClanInfluence { get; set; }
		public int FriendlyClanCount { get; set; }
		public int HostileClanCount { get; set; }
	}

	private sealed class AllySupportSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public AllySupportSnapshot Snapshot { get; set; }
	}

	private sealed class TerritorialInterrogationSnapshot
	{
		public string KingdomName { get; set; }
		public string SettlementName { get; set; }
		public float SettlementDistance { get; set; }
		public string NpcCultureName { get; set; }
		public int CultureNotoriety { get; set; }
	}

	private sealed class FriendshipNeedSnapshot
	{
		public int CultureNotoriety { get; set; }
		public int PlayerClanTier { get; set; }
		public int PrivateRelation { get; set; }
	}

	private sealed class CourtshipNeedSnapshot
	{
		public int CultureNotoriety { get; set; }
		public int PlayerClanTier { get; set; }
		public int NpcClanTier { get; set; }

		public float TriggerWeightMultiplier { get; set; } = 1f;
	}

	private sealed class ArmyJoinRequestSnapshot
	{
		public string ArmyName { get; set; }
		public float OwnStrength { get; set; }
		public float EnemyStrength { get; set; }
		public int EnemyKingdomCount { get; set; }
		public float OwnToEnemyRatio { get; set; }
	}

	private sealed class BanditSuppressionSnapshot
	{
		public string SettlementName { get; set; }
		public int BanditCount { get; set; }
		public float Radius { get; set; }
		public int Trust { get; set; }
		public int PrivateRelation { get; set; }
	}

	private sealed class BanditSuppressionSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public BanditSuppressionSnapshot Snapshot { get; set; }
	}

	private sealed class PoliticalRivalSuppressionSnapshot
	{
		public string KingdomName { get; set; }
		public string RequesterClanName { get; set; }
		public int PlayerClanRelation { get; set; }
		public string RivalClanName { get; set; }
		public int RivalClanRelation { get; set; }
	}

	private sealed class PolicySupportSnapshot
	{
		public string KingdomName { get; set; }
		public int PlayerClanRelation { get; set; }
		public string PolicyName { get; set; }
		public string Description { get; set; }
		public string Effects { get; set; }
		public float SupportScore { get; set; }
		public bool HasPendingDecision { get; set; }
	}

	private sealed class PolicySupportSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public PolicySupportSnapshot Snapshot { get; set; }
	}

	private sealed class PolicyDiscussionSnapshot
	{
		public string PolicyId { get; set; }
		public string PolicyName { get; set; }
		public string PolicyContent { get; set; }
		public string KingdomName { get; set; }
		public int PublishedDay { get; set; }
	}

	private sealed class PolicyDiscussionSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public PolicyDiscussionSnapshot Snapshot { get; set; }
	}

	private sealed class SettlementPurchaseSnapshot
	{
		public string KingdomName { get; set; }
		public int PlayerFiefCount { get; set; }
		public int PlayerTownCount { get; set; }
		public int PlayerCastleCount { get; set; }
		public string PlayerFiefsText { get; set; }
		public int NpcFiefCount { get; set; }
		public int NpcTownCount { get; set; }
		public int NpcCastleCount { get; set; }
	}

	private sealed class SettlementSaleSnapshot
	{
		public string KingdomName { get; set; }
		public int PlayerClanRelation { get; set; }
		public int NpcFiefCount { get; set; }
		public string TargetSettlementName { get; set; }
		public string TargetSettlementType { get; set; }
		public int TargetDailyIncome { get; set; }
		public int HighestFamilyDailyIncome { get; set; }
		public string ForeignSettlementName { get; set; }
		public string ForeignFactionName { get; set; }
		public float BorderDistance { get; set; }
		public float BorderRadius { get; set; }
	}

	private sealed class SettlementSaleSnapshotCacheEntry
	{
		public float SampledAtHour { get; set; }
		public SettlementSaleSnapshot Snapshot { get; set; }
	}

	private sealed class SettlementSaleFiefIncome
	{
		public Settlement Settlement { get; set; }
		public int DailyIncome { get; set; }
		public Settlement NearestForeignSettlement { get; set; }
		public float NearestForeignDistance { get; set; }
	}

	private sealed class TerritorialSettlementSnapshot
	{
		public string SettlementName { get; set; }
		public float Distance { get; set; }
	}

	private sealed class ProactiveCandidateScanState
	{
		public DuelSettings Settings { get; set; }
		public List<MobileParty> Parties { get; set; } = new List<MobileParty>();
		public List<MobileParty> WorkingBatch { get; } = new List<MobileParty>(1);
		public Dictionary<string, TerritorialSettlementSnapshot> TerritorialSettlementSnapshots { get; } = new Dictionary<string, TerritorialSettlementSnapshot>(StringComparer.OrdinalIgnoreCase);
		public CandidateScanStats Stats { get; set; } = new CandidateScanStats();
		public ProactiveCandidate BestCandidate { get; set; }
		public int NextIndex { get; set; }
		public int BatchSize { get; set; }
		public long StartedAtUtcTicks { get; set; }
	}

	private sealed class CandidateScanStats
	{
		public bool MainPartyMissing { get; set; }
		public int TotalLordParties { get; set; }
		public int BaseEligible { get; set; }
		public int InRange { get; set; }
		public int OutOfRange { get; set; }
		public int FoodShortage { get; set; }
		public int MoneyShortage { get; set; }
		public int TroopShortage { get; set; }
		public int PrisonerOverload { get; set; }
		public int ClanCaptive { get; set; }
		public int LowMorale { get; set; }
		public int MountShortage { get; set; }
		public int Overburdened { get; set; }
		public int ClanFinanceStrain { get; set; }
		public int ClanService { get; set; }
		public int RomanticInteraction { get; set; }
		public int Greeting { get; set; }
		public int PolicyDiscussion { get; set; }
		public int Friendship { get; set; }
		public int Courtship { get; set; }
		public int BanditSuppression { get; set; }
		public int PoliticalRivalSuppression { get; set; }
		public int SettlementPurchase { get; set; }
		public int SettlementSale { get; set; }
		public int TerritorialInterrogation { get; set; }
		public int MarriageAlliancePressure { get; set; }
		public int RevengePressure { get; set; }
		public int FiefGovernanceAnxiety { get; set; }
		public int AllySupport { get; set; }
		public int KingdomMercenaryInvite { get; set; }
		public int KingdomVassalInvite { get; set; }
		public int PoliticalAgenda { get; set; }
		public int PolicySupport { get; set; }
		public int Diplomacy { get; set; }
		public int NeedCandidates { get; set; }
		public int TypeFatiguedCandidates { get; set; }
		public int BelowMinUrgency { get; set; }
		public int NeedDrivenTriggered { get; set; }
		public int NotorietyDrivenTriggered { get; set; }
		public int TriggerRollFailed { get; set; }
		public int TestFallbackEligible { get; set; }
		public bool SelectedByTestFallback { get; set; }
		private readonly Dictionary<string, int> _skipReasons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		public void AddSkip(string reason)
		{
			string key = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
			_skipReasons.TryGetValue(key, out int count);
			_skipReasons[key] = count + 1;
		}

		public void MergeFrom(CandidateScanStats other)
		{
			if (other == null)
			{
				return;
			}
			MainPartyMissing |= other.MainPartyMissing;
			TotalLordParties += other.TotalLordParties;
			BaseEligible += other.BaseEligible;
			InRange += other.InRange;
			OutOfRange += other.OutOfRange;
			FoodShortage += other.FoodShortage;
			MoneyShortage += other.MoneyShortage;
			TroopShortage += other.TroopShortage;
			PrisonerOverload += other.PrisonerOverload;
			ClanCaptive += other.ClanCaptive;
			LowMorale += other.LowMorale;
			MountShortage += other.MountShortage;
			Overburdened += other.Overburdened;
			ClanFinanceStrain += other.ClanFinanceStrain;
			ClanService += other.ClanService;
			RomanticInteraction += other.RomanticInteraction;
			Greeting += other.Greeting;
			PolicyDiscussion += other.PolicyDiscussion;
			Friendship += other.Friendship;
			Courtship += other.Courtship;
			BanditSuppression += other.BanditSuppression;
			PoliticalRivalSuppression += other.PoliticalRivalSuppression;
			SettlementPurchase += other.SettlementPurchase;
			SettlementSale += other.SettlementSale;
			TerritorialInterrogation += other.TerritorialInterrogation;
			MarriageAlliancePressure += other.MarriageAlliancePressure;
			RevengePressure += other.RevengePressure;
			FiefGovernanceAnxiety += other.FiefGovernanceAnxiety;
			AllySupport += other.AllySupport;
			KingdomMercenaryInvite += other.KingdomMercenaryInvite;
			KingdomVassalInvite += other.KingdomVassalInvite;
			PoliticalAgenda += other.PoliticalAgenda;
			PolicySupport += other.PolicySupport;
			Diplomacy += other.Diplomacy;
			NeedCandidates += other.NeedCandidates;
			TypeFatiguedCandidates += other.TypeFatiguedCandidates;
			BelowMinUrgency += other.BelowMinUrgency;
			NeedDrivenTriggered += other.NeedDrivenTriggered;
			NotorietyDrivenTriggered += other.NotorietyDrivenTriggered;
			TriggerRollFailed += other.TriggerRollFailed;
			TestFallbackEligible += other.TestFallbackEligible;
			SelectedByTestFallback |= other.SelectedByTestFallback;
			foreach (KeyValuePair<string, int> pair in other._skipReasons)
			{
				_skipReasons.TryGetValue(pair.Key, out int count);
				_skipReasons[pair.Key] = count + pair.Value;
			}
		}

		public string ToLogString()
		{
			string reasons = "";
			try
			{
				reasons = string.Join(",", _skipReasons.OrderByDescending(pair => pair.Value).Take(5).Select(pair => pair.Key + "=" + pair.Value));
			}
			catch
			{
				reasons = "";
			}
			return "mainMissing=" + MainPartyMissing
				+ " total=" + TotalLordParties
				+ " baseEligible=" + BaseEligible
				+ " inRange=" + InRange
				+ " outOfRange=" + OutOfRange
				+ " foodShortage=" + FoodShortage
				+ " moneyShortage=" + MoneyShortage
				+ " troopShortage=" + TroopShortage
				+ " prisonerOverload=" + PrisonerOverload
				+ " clanCaptive=" + ClanCaptive
				+ " lowMorale=" + LowMorale
				+ " mountShortage=" + MountShortage
				+ " overburdened=" + Overburdened
				+ " clanFinanceStrain=" + ClanFinanceStrain
				+ " clanService=" + ClanService
				+ " romanticInteraction=" + RomanticInteraction
				+ " greeting=" + Greeting
				+ " policyDiscussion=" + PolicyDiscussion
				+ " friendship=" + Friendship
				+ " courtship=" + Courtship
				+ " banditSuppression=" + BanditSuppression
				+ " politicalRivalSuppression=" + PoliticalRivalSuppression
				+ " settlementPurchase=" + SettlementPurchase
				+ " settlementSale=" + SettlementSale
				+ " territorialInterrogation=" + TerritorialInterrogation
				+ " marriageAlliance=" + MarriageAlliancePressure
				+ " revengePressure=" + RevengePressure
				+ " fiefGovernance=" + FiefGovernanceAnxiety
				+ " allySupport=" + AllySupport
				+ " mercenaryInvite=" + KingdomMercenaryInvite
				+ " vassalInvite=" + KingdomVassalInvite
				+ " politicalAgenda=" + PoliticalAgenda
				+ " policySupport=" + PolicySupport
				+ " diplomacy=" + Diplomacy
				+ " needCandidates=" + NeedCandidates
				+ " typeFatiguedCandidates=" + TypeFatiguedCandidates
				+ " belowMinUrgency=" + BelowMinUrgency
				+ " needDrivenTriggered=" + NeedDrivenTriggered
				+ " notorietyDrivenTriggered=" + NotorietyDrivenTriggered
				+ " triggerRollFailed=" + TriggerRollFailed
				+ " testFallbackEligible=" + TestFallbackEligible
				+ " selectedByTestFallback=" + SelectedByTestFallback
				+ " skips=" + reasons;
		}
	}
}
