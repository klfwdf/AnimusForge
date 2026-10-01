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

public sealed partial class ProactiveNpcRequestBehavior : CampaignBehaviorBase
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

	private readonly ProactiveRequestSessionOwner _sessionOwner = new ProactiveRequestSessionOwner();
	private ProactiveNpcRequestSession _activeSession => _sessionOwner.Current;
	private readonly ProactiveRequestCooldownOwner _cooldownOwner = new ProactiveRequestCooldownOwner();
	private readonly ProactiveOpeningOwner _openingOwner = new ProactiveOpeningOwner();
	private MobileParty _activePartyCache;
	private string _activePartyCacheId = "";
	private readonly ProactiveCandidateScanOwner _candidateScanOwner = new ProactiveCandidateScanOwner();
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
				HeroCooldownUntilDays = _cooldownOwner.HeroCooldownUntilDays,
				NeedTypeFatigueUntilDays = _cooldownOwner.NeedTypeFatigueUntilDays,
				DiplomacyDiscussionKeysUntilDays = _cooldownOwner.DiplomacyDiscussionKeysUntilDays,
				GlobalCooldownUntilHours = _cooldownOwner.GlobalCooldownUntilHours,
				// Incremental scans are runtime-only. A save during the one-second scan window retries after load.
				LastScanHour = !_candidateScanOwner.IsRunning ? _cooldownOwner.LastScanHour : -99999f
			});
			CampaignSaveChunkHelper.LogRawJsonSaveStats(StorageKey, "ProactiveNpcRequest", storageJson, "heroCooldowns=" + _cooldownOwner.HeroCooldownUntilDays.Count + " typeFatigues=" + _cooldownOwner.NeedTypeFatigueUntilDays.Count + " active=" + (_activeSession != null));
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
			_sessionOwner.Import(storage?.ActiveSession);
			_cooldownOwner.Import(storage?.HeroCooldownUntilDays,
				storage?.NeedTypeFatigueUntilDays ?? storage?.NeedCooldownUntilDays,
				storage?.DiplomacyDiscussionKeysUntilDays,
				storage?.GlobalCooldownUntilHours ?? 0f, storage?.LastScanHour ?? -99999f);
			_openingOwner.Clear();
			ClearActivePartyCache();
			_candidateScanOwner.Clear();
			_policyDiscussionSnapshotCache = null;
		}
		catch (Exception ex)
		{
			_sessionOwner.Clear();
			_cooldownOwner.ResetDictionaries();
			_openingOwner.Clear();
			ClearActivePartyCache();
			_candidateScanOwner.Clear();
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
			return hero != null && Instance?._openingOwner.Matches(true, Instance._activeSession?.Id, GetHeroKey(hero)) == true;
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
			if (!_sessionOwner.TryReserveEncounterProbe(DateTime.UtcNow.Ticks))
			{
				return;
			}
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
		if (_candidateScanOwner.IsRunning)
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
		if (!_cooldownOwner.TryBeginScan(nowHours, scanIntervalHours))
		{
			return;
		}
		_cooldownOwner.PruneExpiredNeedTypeFatigue(NowDays());
		_cooldownOwner.PruneExpiredDiplomacyDiscussionKeys(NowDays());
		if (_activeSession != null)
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: active session hero=" + (_activeSession.HeroId ?? "") + " stage=" + (_activeSession.Stage ?? ""));
			return;
		}
		if (_cooldownOwner.IsGlobalCooldownActive(nowHours))
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: global cooldown remainingHours=" + Math.Max(0f, _cooldownOwner.GlobalCooldownUntilHours - nowHours).ToString("0.0"));
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
		ProactiveCandidateScanState scan = _candidateScanOwner.Start(settings, parties, DateTime.UtcNow.Ticks);
		if (scan != null)
		{
			Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_start", () => "incremental scan started parties=" + parties.Count + " batchSize=" + scan.BatchSize + " targetFrames=" + CandidateScanTargetFrames + " nowHours=" + nowHours.ToString("0.0"), 5.0);
		}
	}

	private void ProcessCandidateScan()
	{
		ProactiveCandidateScanState scan = _candidateScanOwner.Current;
		if (scan == null)
		{
			return;
		}
		DuelSettings currentSettings = DuelSettings.GetSettings();
		if (currentSettings == null || !currentSettings.EnableProactiveNpcRequests || _activeSession != null)
		{
			Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_cancel", () => "incremental scan cancelled enabled=" + (currentSettings?.EnableProactiveNpcRequests ?? false) + " active=" + (_activeSession != null), 5.0);
			_candidateScanOwner.Clear();
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
			_candidateScanOwner.Consider(scan, batchCandidate, batchStats);
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
		if (!_candidateScanOwner.TryComplete(scan))
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableProactiveNpcRequests || _activeSession != null)
		{
			return;
		}
		if (_cooldownOwner.IsGlobalCooldownActive(NowHours()))
		{
			Logger.LogVerbose("ProactiveNpcRequest", "incremental_scan_discard", () => "incremental scan discarded after global cooldown began remaining=" + (_cooldownOwner.GlobalCooldownUntilHours - NowHours()).ToString("0.0"), 5.0);
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

	private void StartRequest(ProactiveCandidate candidate, DuelSettings settings)
	{
		MobileParty party = candidate?.Party;
		Hero hero = candidate?.Hero;
		if (party == null || hero == null || _activeSession != null)
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
		ProactiveNpcRequestSession session = new ProactiveNpcRequestSession
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
			DiplomacyDiscussionKey = candidate.DiplomacyDiscussionKey,
			DiplomacyDiscussionFact = candidate.DiplomacyDiscussionFact,
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
		if (!_sessionOwner.TryStart(session))
		{
			return;
		}
		if (string.Equals(candidate.TriggerSource, TriggerSourceNotorietyDriven, StringComparison.OrdinalIgnoreCase))
		{
			PlayerNotorietyBehavior.MarkObserverKnowsPlayerForExternal(hero, "proactive_notoriety_request");
		}
		CacheActiveParty(party);
		try
		{
			party.Ai?.SetDoNotAttackMainParty(Math.Max(2, (int)ActiveRequestTtlHours));
		}
		catch
		{
		}
		SetPartyAiAction.GetActionForEngagingParty(party, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false);
		int globalCooldown = GetEffectiveGlobalCooldownHours(settings);
		_cooldownOwner.StartGlobalCooldown(NowHours(), globalCooldown);
		Logger.Log("ProactiveNpcRequest", "started request triggerSource=" + (_activeSession.TriggerSource ?? "") + " knownMajorBefore=" + _activeSession.KnownMajorBeforeRequest + " effectiveNotoriety=" + _activeSession.EffectiveNotorietyAtRequest + " needChance=" + _activeSession.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + _activeSession.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + _activeSession.SelectedNeedUrgency.ToString("0.##") + " typeWeight=" + _activeSession.NeedTypeWeightMultiplierAtSelection.ToString("0.##") + " typeFatigueMultiplier=" + _activeSession.NeedTypeFatigueMultiplierAtSelection.ToString("0.##") + " typeFatigueRemainingDays=" + _activeSession.NeedTypeFatigueRemainingDaysAtSelection.ToString("0.##") + " need=" + _activeSession.NeedType + " needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " hero=" + _activeSession.HeroId + " party=" + _activeSession.PartyId + " kingdom=" + (_activeSession.TargetKingdomId ?? "") + " playerClanTier=" + _activeSession.PlayerClanTier + " isKingdomLeader=" + _activeSession.TargetHeroIsKingdomLeader + " kingdomVassals=" + _activeSession.KingdomFormalVassalClanCount + "/" + _activeSession.KingdomTargetVassalClanCount + " kingdomMercs=" + _activeSession.KingdomMercenaryClanCount + "/" + _activeSession.KingdomTargetMercenaryClanCount + " kingdomFiefScore=" + _activeSession.KingdomFiefScore + " kingdomWars=" + _activeSession.KingdomWarKingdomCount + " kingdomPowerRatio=" + _activeSession.KingdomPowerRatioToEnemies.ToString("0.00") + " foodDays=" + candidate.FoodDays + " partyGold=" + candidate.PartyGold + " totalWage=" + candidate.TotalWage + " unpaidWages=" + candidate.UnpaidWages.ToString("0.00") + " troops=" + candidate.MemberCount + "/" + candidate.PartySizeLimit + " troopRatio=" + candidate.PartySizeRatio.ToString("0.00") + " prisoners=" + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit + " heroPrisoners=" + candidate.HeroPrisonerCount + " prisonerRatio=" + candidate.PrisonerSizeRatio.ToString("0.00") + " morale=" + candidate.Morale.ToString("0.0") + " mounts=" + candidate.MountCount + " packAnimals=" + candidate.PackAnimalCount + " mountRatio=" + candidate.MountRatio.ToString("0.00") + " carry=" + candidate.TotalWeightCarried.ToString("0.0") + "/" + candidate.InventoryCapacity + " carryRatio=" + candidate.CarryRatio.ToString("0.00") + " clanGold=" + candidate.ClanGold + " clanDebt=" + candidate.ClanDebtToKingdom + " captiveClanHeroes=" + candidate.CaptiveClanHeroCount + " captiveLeader=" + candidate.CaptiveClanLeaderHeld + " wageBudget=" + candidate.AvailableWageBudget + " distance=" + candidate.Distance.ToString("0.0") + " testFallback=" + candidate.IsTestFallback);
		Logger.Log("ProactiveNpcRequest", "started request extra needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " marriageAdults=" + _activeSession.LastKnownMarriageAdultClanHeroCount + " unmarriedAdults=" + _activeSession.LastKnownMarriageUnmarriedAdultCount + " firstUnmarried=" + (_activeSession.LastKnownMarriageFirstUnmarriedName ?? "") + " clanServiceClan=" + (_activeSession.LastKnownClanServiceTargetClanName ?? "") + " clanServiceKing=" + (_activeSession.LastKnownClanServiceCurrentKingName ?? "") + " clanServicePlayerRelation=" + _activeSession.LastKnownClanServicePlayerRelation + " clanServiceKingRelation=" + _activeSession.LastKnownClanServiceCurrentKingRelation + " clanServiceGap=" + _activeSession.LastKnownClanServiceRelationGap + " romanticPrivateRelation=" + _activeSession.LastKnownRomanticInteractionPrivateRelation + " greetingPrivateRelation=" + _activeSession.LastKnownGreetingPrivateRelation + " banditSettlement=" + (_activeSession.LastKnownBanditSuppressionSettlementName ?? "") + " banditCount=" + _activeSession.LastKnownBanditSuppressionBanditCount + " banditRadius=" + _activeSession.LastKnownBanditSuppressionRadius.ToString("0.0") + " banditTrust=" + _activeSession.LastKnownBanditSuppressionTrust + " banditPrivateRelation=" + _activeSession.LastKnownBanditSuppressionPrivateRelation + " territorialKingdom=" + (_activeSession.LastKnownTerritorialInterrogationKingdomName ?? "") + " territorialSettlement=" + (_activeSession.LastKnownTerritorialInterrogationSettlementName ?? "") + " territorialDistance=" + _activeSession.LastKnownTerritorialInterrogationSettlementDistance.ToString("0.0") + " territorialCulture=" + (_activeSession.LastKnownTerritorialInterrogationNpcCultureName ?? "") + " territorialCultureNotoriety=" + _activeSession.LastKnownTerritorialInterrogationCultureNotoriety + " revengeScore=" + _activeSession.LastKnownRevengePressureScore.ToString("0.0") + " revengeTarget=" + (_activeSession.LastKnownRevengeTargetName ?? "") + " revengeReason=" + (_activeSession.LastKnownRevengeReasonText ?? "") + " fiefProblems=" + _activeSession.LastKnownFiefProblemCount + " fief=" + (_activeSession.LastKnownFiefProblemName ?? "") + " fiefIssue=" + (_activeSession.LastKnownFiefIssueText ?? "") + " fiefLoyalty=" + _activeSession.LastKnownFiefLoyalty.ToString("0.0") + " fiefSecurity=" + _activeSession.LastKnownFiefSecurity.ToString("0.0") + " fiefGarrison=" + _activeSession.LastKnownFiefGarrisonCount + " allyInfluence=" + _activeSession.LastKnownClanInfluence.ToString("0.0") + " friendlyClans=" + _activeSession.LastKnownFriendlyClanCount + " hostileClans=" + _activeSession.LastKnownHostileClanCount);
	}

	private void TryOpenActiveEncounterWhenClose()
	{
		if (!_sessionOwner.IsChasing)
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
			if (ProactiveRequestSessionOwner.ShouldCancelForBusyReason(busyReason))
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
		_sessionOwner.MarkOpeningMenu(NowHours());
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
			_sessionOwner.ReturnToChasing();
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
		if (_sessionOwner.IsExpired(NowHours()))
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
			&& ProactiveRequestSessionOwner.ShouldCancelForBusyReason(busyReason))
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
		_sessionOwner.MarkEncounterOpened(NowHours());
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
		_openingOwner.Open(nativeConversation, _activeSession.Id, GetHeroKey(hero), fact, prompt, NowHours());
		_sessionOwner.MarkConversationOpening(nativeConversation);
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
		if (hero == null || !_openingOwner.TryConsume(nativeConversation, _activeSession?.Id, GetHeroKey(hero), out extraFact, out promptText))
		{
			return false;
		}
		CompleteActiveForHeroInternal(hero, nativeConversation ? "native_opening_consumed" : "scene_opening_consumed");
		return !string.IsNullOrWhiteSpace(extraFact);
	}

	private bool TryPeekPendingOpening(bool nativeConversation, out Hero hero, out string extraFact, out string promptText)
	{
		hero = null;
		extraFact = "";
		promptText = "";
		if (!_openingOwner.TryPeek(nativeConversation, _activeSession?.Id, out string heroId, out extraFact, out promptText))
		{
			return false;
		}
		hero = ResolveHero(heroId);
		if (hero == null || !_openingOwner.Matches(nativeConversation, _activeSession?.Id, GetHeroKey(hero)))
		{
			extraFact = "";
			promptText = "";
			return false;
		}
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
		_cooldownOwner.RecordHeroCooldown(heroKey, nowDays, heroCooldownDays);
	}

	private void RecordActiveNeedTypeFatigue()
	{
		if (!_sessionOwner.ShouldRecordFatigue)
		{
			return;
		}
		foreach (string needType in GetActiveNeedTypes())
		{
			RecordNeedTypeFatigue(needType, "map_encounter_opened");
		}
		if (!string.IsNullOrWhiteSpace(_activeSession.DiplomacyDiscussionKey))
		{
			int retentionDays = Math.Max(7, GetEffectiveNeedTypeFatigueDays(NeedDiplomacy, DuelSettings.GetSettings()));
			_cooldownOwner.RecordDiscussion(_activeSession.DiplomacyDiscussionKey, NowDays(), retentionDays);
		}
		_sessionOwner.MarkFatigueRecorded();
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
			_cooldownOwner.RecordNeedFatigue(normalized, 0f, fatigueDays);
			return;
		}
		_cooldownOwner.RecordNeedFatigue(normalized, NowDays(), fatigueDays);
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
			return 88;
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
		_sessionOwner.Clear();
		_openingOwner.Clear();
		ClearActivePartyCache();
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
		return _sessionOwner.MatchesPartyId(partyId);
	}

	private bool IsActiveHero(Hero hero)
	{
		if (hero == null || _activeSession == null)
		{
			return false;
		}
		return _sessionOwner.MatchesHeroId(GetHeroKey(hero));
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
		if (!string.IsNullOrWhiteSpace(_activeSession?.DiplomacyDiscussionFact))
		{
			return BuildNpcInitiatedRequestFact(npcName, playerName, _activeSession.DiplomacyDiscussionFact);
		}
		string kingdomName = ResolveKnownKingdomName(hero?.Clan?.Kingdom);
		return BuildNpcInitiatedRequestFact(npcName, playerName, "你与" + playerName + "同属" + kingdomName + "。本国近日收到了一些外交消息，你想与对方交换判断；不得替国王承诺或执行外交行动。");
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
		return npcName + "与" + playerName + "同属一个王国；他根据本国已经获知的近期外交宣言，主动来讨论局势、影响和可考虑的立场。不得替国王作出承诺，不得把建议说成已经执行的外交行动。";
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

	private float GetNeedTypeFatigueRemainingDays(string needType, float nowDays)
	{
		string normalized = NormalizeNeedType(needType);
		return _cooldownOwner.GetNeedRemainingDays(normalized, nowDays);
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
		public Dictionary<string, float> DiplomacyDiscussionKeysUntilDays { get; set; }
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
		public string DiplomacyDiscussionKey { get; set; }
		public string DiplomacyDiscussionFact { get; set; }
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
		public string SessionId { get; set; }
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
		public string DiplomacyDiscussionKey { get; set; }
		public string DiplomacyDiscussionFact { get; set; }
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
