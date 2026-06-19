using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ProactiveNpcRequestBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_af_proactive_npc_request_state_v1";
	private const string NeedFoodShortage = "FoodShortage";
	private const string NeedMoneyShortage = "MoneyShortage";
	private const string NeedTroopShortage = "TroopShortage";
	private const string NeedPrisonerOverload = "PrisonerOverload";
	private const string NeedKingdomMercenaryInvite = "KingdomMercenaryInvite";
	private const string NeedKingdomVassalInvite = "KingdomVassalInvite";
	private const string NeedDiplomacy = "Diplomacy";
	private const string TriggerSourceNeedDriven = "NeedDriven";
	private const string TriggerSourceNotorietyDriven = "NotorietyDriven";
	private const int MercenaryInviteMinPlayerClanTier = 1;
	private const int VassalInviteMinPlayerClanTier = 2;
	private const float KingdomStrongEnoughToSkipMercenaryRatio = 3f;
	private const float ActiveRequestTtlHours = 18f;
	private const float NeedCooldownDays = 7f;

	private ProactiveNpcRequestSession _activeSession;
	private Dictionary<string, float> _heroCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, float> _needCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private float _globalCooldownUntilHours;
	private float _lastScanHour = -99999f;
	private PendingOpeningFact _pendingNativeOpening;
	private PendingOpeningFact _pendingSceneOpening;

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
				NeedCooldownUntilDays = _needCooldownUntilDays,
				GlobalCooldownUntilHours = _globalCooldownUntilHours,
				LastScanHour = _lastScanHour
			});
			CampaignSaveChunkHelper.LogRawJsonSaveStats(StorageKey, "ProactiveNpcRequest", storageJson, "heroCooldowns=" + (_heroCooldownUntilDays?.Count ?? 0) + " needCooldowns=" + (_needCooldownUntilDays?.Count ?? 0) + " active=" + (_activeSession != null));
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
			_heroCooldownUntilDays = NormalizeCooldownDictionary(storage?.HeroCooldownUntilDays);
			_needCooldownUntilDays = NormalizeCooldownDictionary(storage?.NeedCooldownUntilDays);
			_globalCooldownUntilHours = storage?.GlobalCooldownUntilHours ?? 0f;
			_lastScanHour = storage?.LastScanHour ?? -99999f;
			_pendingNativeOpening = null;
			_pendingSceneOpening = null;
		}
		catch (Exception ex)
		{
			_activeSession = null;
			_heroCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			_needCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
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
		ProactiveCandidate candidate = FindBestRequestCandidate(settings, out CandidateScanStats stats);
		if (candidate == null)
		{
			Logger.Log("ProactiveNpcRequest", "scan no candidate: " + stats?.ToLogString());
			return;
		}
		Logger.Log("ProactiveNpcRequest", "scan selected: triggerSource=" + (candidate.TriggerSource ?? "") + " knownMajorBefore=" + candidate.KnownMajorBeforeRequest + " effectiveNotoriety=" + candidate.EffectiveNotorietyAtRequest + " needChance=" + candidate.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + candidate.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + candidate.SelectedNeedUrgency.ToString("0.##") + " need=" + (candidate.NeedType ?? "") + " needs=" + JoinNeedTypesForLog(candidate.NeedTypes, candidate.NeedType) + " hero=" + (candidate.Hero?.StringId ?? "") + " party=" + (candidate.Party?.StringId ?? "") + " kingdom=" + (candidate.TargetKingdomId ?? "") + " playerClanTier=" + candidate.PlayerClanTier + " isKingdomLeader=" + candidate.TargetHeroIsKingdomLeader + " kingdomVassals=" + candidate.KingdomFormalVassalClanCount + "/" + candidate.KingdomTargetVassalClanCount + " kingdomMercs=" + candidate.KingdomMercenaryClanCount + "/" + candidate.KingdomTargetMercenaryClanCount + " kingdomFiefScore=" + candidate.KingdomFiefScore + " kingdomWars=" + candidate.KingdomWarKingdomCount + " kingdomPowerRatio=" + candidate.KingdomPowerRatioToEnemies.ToString("0.00") + " distance=" + candidate.Distance.ToString("0.0") + " foodDays=" + candidate.FoodDays + " partyGold=" + candidate.PartyGold + " totalWage=" + candidate.TotalWage + " unpaidWages=" + candidate.UnpaidWages.ToString("0.00") + " troops=" + candidate.MemberCount + "/" + candidate.PartySizeLimit + " troopRatio=" + candidate.PartySizeRatio.ToString("0.00") + " prisoners=" + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit + " heroPrisoners=" + candidate.HeroPrisonerCount + " prisonerRatio=" + candidate.PrisonerSizeRatio.ToString("0.00") + " wageBudget=" + candidate.AvailableWageBudget + " testFallback=" + candidate.IsTestFallback + " stats=" + stats?.ToLogString());
		StartRequest(candidate, settings);
	}

	private ProactiveCandidate FindBestRequestCandidate(DuelSettings settings, out CandidateScanStats stats)
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
		List<ProactiveCandidate> candidates = new List<ProactiveCandidate>();
		foreach (MobileParty party in MobileParty.AllLordParties ?? Enumerable.Empty<MobileParty>())
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
			if (TryBuildDiplomacyCandidate(candidate, settings, out ProactiveCandidate diplomacyCandidate))
			{
				needCandidates.Add(diplomacyCandidate);
			}
			ProactiveCandidate combinedCandidate = BuildCombinedNeedCandidate(needCandidates);
			if (combinedCandidate != null)
			{
				stats.NeedCandidates++;
				if (TryEvaluateCandidateTrigger(combinedCandidate, settings, stats))
				{
					candidates.Add(combinedCandidate);
				}
			}
		}
		return candidates
			.OrderByDescending(c => c.NeedUrgency)
			.ThenByDescending(c => c.EffectiveNotorietyAtRequest)
			.ThenBy(c => c.Distance)
			.FirstOrDefault();
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
		float needChance = Clamp(urgency * globalScale * knownMultiplier, 0f, 100f);
		float notorietyChance = knownMajor
			? 0f
			: Clamp(effectiveNotoriety * GetEffectiveNotorietyChanceMultiplier(settings) * (urgency / 100f) * globalScale, 0f, 100f);
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
			if (party.MapEvent != null || party.CurrentSettlement != null || party.Army != null || party.BesiegedSettlement != null || party.IsInRaftState || party.IsCurrentlyAtSea)
			{
				skipReason = "party_busy_or_invalid_location";
				return false;
			}
			if (mainParty.MapEvent != null || mainParty.CurrentSettlement != null || mainParty.IsInRaftState || mainParty.IsCurrentlyAtSea)
			{
				skipReason = "main_party_busy_or_invalid_location";
				return false;
			}
			if (party.MapFaction == null || mainParty.MapFaction == null || party.MapFaction.IsAtWarWith(mainParty.MapFaction))
			{
				skipReason = "war_or_missing_faction";
				return false;
			}
			string heroKey = GetHeroKey(hero);
			float nowDays = NowDays();
			if (IsOnCooldown(_heroCooldownUntilDays, heroKey, nowDays))
			{
				skipReason = "hero_cooldown";
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
			Kingdom targetKingdom = ResolveHeroKingdom(hero);
			KingdomManpowerNeedSnapshot kingdomNeed = BuildKingdomManpowerNeedSnapshot(targetKingdom);
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
				KingdomVassalNeedUrgency = kingdomNeed.VassalNeedUrgency
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
		if (settings?.ProactiveNpcRequestTestMode != true && IsOnCooldown(_needCooldownUntilDays, needType, NowDays()))
		{
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
			NeedType = needType,
			NeedTypes = new List<string> { needType },
			NeedUrgency = urgency,
			IsTestFallback = source.IsTestFallback
		};
	}

	private static ProactiveCandidate BuildCombinedNeedCandidate(List<ProactiveCandidate> needCandidates)
	{
		if (needCandidates == null || needCandidates.Count <= 0)
		{
			return null;
		}
		List<ProactiveCandidate> ordered = needCandidates
			.Where(c => c != null && !string.IsNullOrWhiteSpace(c.NeedType))
			.OrderByDescending(c => c.NeedUrgency)
			.ThenByDescending(c => GetNeedPresentationPriority(c.NeedType))
			.ToList();
		if (ordered.Count <= 0)
		{
			return null;
		}
		List<string> needTypes = NormalizeNeedTypes(ordered.Select(c => c.NeedType), ordered[0].NeedType);
		if (needTypes.Count > 3)
		{
			needTypes = needTypes.Take(3).ToList();
		}
		ProactiveCandidate combined = ordered.FirstOrDefault(c => string.Equals(c.NeedType, needTypes[0], StringComparison.OrdinalIgnoreCase)) ?? ordered[0];
		combined.NeedTypes = needTypes;
		combined.NeedType = needTypes[0];
		combined.NeedUrgency = ordered.Max(c => c.NeedUrgency) + Math.Min(12f, Math.Max(0, needTypes.Count - 1) * 4f);
		return combined;
	}

	private bool TryBuildFoodShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsFoodShortageNeedMet(source.Party, source.FoodDays, settings, out float urgency))
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
		if (source == null || !IsTroopShortageNeedMet(source, settings, out float urgency))
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
#if BANNERLORD_1_4_OR_GREATER
			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			bool hasTrade = tradeBeh != null && tradeBeh.HasTradeAgreement(npcKingdom, playerKingdom, out var _);
			if (!hasTrade) { urgency = 45f; return true; }
#endif
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
			NeedTypes = NormalizeNeedTypes(candidate.NeedTypes, candidate.NeedType),
			Stage = "Chasing",
			CreatedAtHours = NowHours(),
			ExpiresAtHours = NowHours() + ActiveRequestTtlHours,
			TriggerSource = string.IsNullOrWhiteSpace(candidate.TriggerSource) ? TriggerSourceNeedDriven : candidate.TriggerSource,
			KnownMajorBeforeRequest = candidate.KnownMajorBeforeRequest,
			EffectiveNotorietyAtRequest = candidate.EffectiveNotorietyAtRequest,
			NeedDrivenChance = candidate.NeedDrivenChance,
			NotorietyDrivenChance = candidate.NotorietyDrivenChance,
			SelectedNeedUrgency = candidate.SelectedNeedUrgency,
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
		Logger.Log("ProactiveNpcRequest", "started request triggerSource=" + (_activeSession.TriggerSource ?? "") + " knownMajorBefore=" + _activeSession.KnownMajorBeforeRequest + " effectiveNotoriety=" + _activeSession.EffectiveNotorietyAtRequest + " needChance=" + _activeSession.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + _activeSession.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + _activeSession.SelectedNeedUrgency.ToString("0.##") + " need=" + _activeSession.NeedType + " needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " hero=" + _activeSession.HeroId + " party=" + _activeSession.PartyId + " kingdom=" + (_activeSession.TargetKingdomId ?? "") + " playerClanTier=" + _activeSession.PlayerClanTier + " isKingdomLeader=" + _activeSession.TargetHeroIsKingdomLeader + " kingdomVassals=" + _activeSession.KingdomFormalVassalClanCount + "/" + _activeSession.KingdomTargetVassalClanCount + " kingdomMercs=" + _activeSession.KingdomMercenaryClanCount + "/" + _activeSession.KingdomTargetMercenaryClanCount + " kingdomFiefScore=" + _activeSession.KingdomFiefScore + " kingdomWars=" + _activeSession.KingdomWarKingdomCount + " kingdomPowerRatio=" + _activeSession.KingdomPowerRatioToEnemies.ToString("0.00") + " foodDays=" + candidate.FoodDays + " partyGold=" + candidate.PartyGold + " totalWage=" + candidate.TotalWage + " unpaidWages=" + candidate.UnpaidWages.ToString("0.00") + " troops=" + candidate.MemberCount + "/" + candidate.PartySizeLimit + " troopRatio=" + candidate.PartySizeRatio.ToString("0.00") + " prisoners=" + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit + " heroPrisoners=" + candidate.HeroPrisonerCount + " prisonerRatio=" + candidate.PrisonerSizeRatio.ToString("0.00") + " wageBudget=" + candidate.AvailableWageBudget + " distance=" + candidate.Distance.ToString("0.0") + " testFallback=" + candidate.IsTestFallback);
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
		if (party.MapEvent != null || mainParty.MapEvent != null || party.CurrentSettlement != null || mainParty.CurrentSettlement != null || party.IsInRaftState || mainParty.IsInRaftState || party.IsCurrentlyAtSea || mainParty.IsCurrentlyAtSea)
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
			PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "RestartPlayerEncounter failed: " + ex.Message);
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
		LordEncounterBehavior.SetTarget(hero);
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
			return "[AFEF NPC行为补充] " + npcName + "，你是因为听说过" + playerName + "的公开履历，才在当前困难中想到主动寻找对方。你现在已经知道这些公开履历，但仍必须围绕自己的具体需求开口，不要把这当作闲聊。";
		}
		return "[AFEF NPC行为补充] " + npcName + "，你是因为当前困难或利益需求主动寻找" + playerName + "，不是因为无事闲聊。你应当围绕自己的具体需求开口，不要把这当作" + playerName + "主动提出的话。";
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
		if (_activeSession?.IsTestFallback == true || settings?.ProactiveNpcRequestTestMode == true)
		{
			foreach (string needType in GetActiveNeedTypes())
			{
				if (!string.IsNullOrWhiteSpace(needType))
				{
					_needCooldownUntilDays.Remove(needType);
				}
			}
		}
		else
		{
			foreach (string needType in GetActiveNeedTypes())
			{
				if (!string.IsNullOrWhiteSpace(needType))
				{
					_needCooldownUntilDays[needType] = nowDays + NeedCooldownDays;
				}
			}
		}
	}

	private List<string> GetActiveNeedTypes()
	{
		if (_activeSession == null)
		{
			return new List<string> { NeedFoodShortage };
		}
		return NormalizeNeedTypes(_activeSession.NeedTypes, string.IsNullOrWhiteSpace(_activeSession.NeedType) ? NeedFoodShortage : _activeSession.NeedType);
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
		if (string.Equals(text, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			return NeedKingdomMercenaryInvite;
		}
		if (string.Equals(text, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			return NeedKingdomVassalInvite;
		}
		return "";
	}

	private static int GetNeedPresentationPriority(string needType)
	{
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

	private MobileParty ResolveActiveParty()
	{
		if (_activeSession == null)
		{
			return null;
		}
		string partyId = (_activeSession.PartyId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(partyId))
		{
			MobileParty party = MobileParty.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), partyId, StringComparison.OrdinalIgnoreCase));
			if (party != null)
			{
				return party;
			}
		}
		Hero hero = ResolveHero(_activeSession.HeroId);
		return hero?.PartyBelongedTo;
	}

	private string BuildOpeningFact(Hero hero)
	{
		MobileParty party = hero?.PartyBelongedTo ?? ResolveActiveParty();
		string playerName = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家").Trim();
		string npcName = hero?.Name?.ToString() ?? "你";
		List<string> activeNeedTypes = GetActiveNeedTypes();
		if (activeNeedTypes.Count > 1)
		{
			return BuildCombinedOpeningFact(hero, party, playerName, npcName, activeNeedTypes);
		}
		string needType = activeNeedTypes.Count > 0 ? activeNeedTypes[0] : (string.IsNullOrWhiteSpace(_activeSession?.NeedType) ? NeedFoodShortage : _activeSession.NeedType);
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

	private string BuildCombinedOpeningFact(Hero hero, MobileParty party, string playerName, string npcName, List<string> needTypes)
	{
		List<string> normalized = NormalizeNeedTypes(needTypes, NeedFoodShortage);
		if (normalized.Count <= 1)
		{
			return BuildOpeningFact(hero);
		}
		List<string> items = new List<string>();
		for (int i = 0; i < normalized.Count && i < 3; i++)
		{
			items.Add((i + 1).ToString() + ". " + BuildOpeningNeedSummary(normalized[i], hero, party, playerName, npcName));
		}
		string intro = _activeSession?.IsTestFallback == true
			? npcName + "，测试模式下你被选为 NPC 主动接触测试对象。"
			: npcName + "，你主动追上" + playerName + "。";
		return "[AFEF NPC行为补充] " + intro + "你并非来开战，而是有数件事想谈：" + string.Join("；", items) + "。你应该先开口说明这些来意，可以按紧急程度合并表达，不要把这些当作" + playerName + "主动提出的话。不要假定任何交易、赎买、转移俘虏、借款、欠款、还款承诺、记账、入队或效力关系已经由系统成立。";
	}

	private string BuildOpeningNeedSummary(string needType, Hero hero, MobileParty party, string playerName, string npcName)
	{
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveHeroKingdom(hero);
			string kingdomName = ResolveKnownKingdomName(kingdom);
			int playerClanTier = ResolveKnownPlayerClanTier();
			return "你是" + kingdomName + "的国王，" + BuildKingdomManpowerNeedText(kingdom) + "你判断王国缺少长期封臣；" + playerName + "的玩家家族等级为 " + playerClanTier + "，已达到封臣门槛，因此你想邀请其成为正式封臣，也可以把雇佣兵契约作为较低承诺的选择";
		}
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveHeroKingdom(hero);
			string kingdomName = ResolveKnownKingdomName(kingdom);
			int playerClanTier = ResolveKnownPlayerClanTier();
			string authorityText = IsKingdomLeader(hero, kingdom) ? "你是国王" : "你是正式领主";
			return authorityText + "，可以代表" + kingdomName + "邀请雇佣兵。" + BuildKingdomManpowerNeedText(kingdom) + playerName + "的玩家家族等级为 " + playerClanTier + "，已达到雇佣兵门槛，因此你想邀请其以雇佣兵身份效力";
		}
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			int memberCount = SafeMemberCount(party);
			int partySizeLimit = SafePartySizeLimit(party);
			int availableWageBudget = SafeAvailableWageBudget(party);
			if (party == null && _activeSession != null)
			{
				memberCount = _activeSession.LastKnownMemberCount;
				partySizeLimit = _activeSession.LastKnownPartySizeLimit;
				availableWageBudget = _activeSession.LastKnownAvailableWageBudget;
			}
			int missing = Math.Max(0, partySizeLimit - memberCount);
			string ratioText = partySizeLimit > 0 ? "，约为上限的 " + (CalculatePartySizeRatio(memberCount, partySizeLimit) * 100f).ToString("0") + "%" : "";
			string wageText = availableWageBudget > 0 ? "，当前可用军饷预算约为 " + availableWageBudget + " 第纳尔" : "";
			return "你的部队兵力不足，当前人数约为 " + memberCount + "/" + partySizeLimit + ratioText + "，缺口约 " + missing + " 人" + wageText + "，因此你想询问是否有士兵、俘虏、雇佣兵、护卫或短期军事合作";
		}
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			int prisonerCount = SafePrisonerCount(party);
			int prisonerSizeLimit = SafePrisonerSizeLimit(party);
			int heroPrisonerCount = SafeHeroPrisonerCount(party);
			if (party == null && _activeSession != null)
			{
				prisonerCount = _activeSession.LastKnownPrisonerCount;
				prisonerSizeLimit = _activeSession.LastKnownPrisonerSizeLimit;
				heroPrisonerCount = _activeSession.LastKnownHeroPrisonerCount;
			}
			string ratioText = prisonerSizeLimit > 0 ? "，约为俘虏容量的 " + (CalculatePrisonerSizeRatio(prisonerCount, prisonerSizeLimit) * 100f).ToString("0") + "%" : "";
			string heroText = heroPrisonerCount > 0 ? "，其中包括 " + heroPrisonerCount + " 名英雄俘虏" : "";
			return "你的队伍俘虏负担过重，当前俘虏约为 " + prisonerCount + "/" + prisonerSizeLimit + ratioText + heroText + "，因此你想询问是否愿意赎买、接收、转运俘虏，或帮助联系赎金渠道";
		}
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			int partyGold = SafePartyTradeGold(party);
			int totalWage = SafeTotalWage(party);
			float unpaidWages = SafeUnpaidWages(party);
			if (party == null && _activeSession != null)
			{
				partyGold = _activeSession.LastKnownPartyGold;
				totalWage = _activeSession.LastKnownTotalWage;
				unpaidWages = _activeSession.LastKnownUnpaidWages;
			}
			string wageText = totalWage > 0 ? "，每日军饷约为 " + totalWage + " 第纳尔，可支撑军饷约 " + CalculateWageDays(partyGold, totalWage).ToString("0.0") + " 天" : "";
			string unpaidText = unpaidWages > 0f ? "，并且已有约 " + (unpaidWages * 100f).ToString("0") + "% 的军饷未能支付" : "";
			return "你资金短缺，当前可用现金约为 " + partyGold + " 第纳尔" + wageText + unpaidText + "，因此你想寻求资助、预付款、雇佣或交易周转机会";
		}
		int foodDays = SafeFoodDays(party);
		int totalFood = 0;
		try
		{
			totalFood = party?.ItemRoster?.TotalFood ?? 0;
		}
		catch
		{
		}
		return "你的部队缺少食物，剩余食物约可维持 " + foodDays + " 天，库存食物数量约为 " + totalFood + "，因此你想请求援助或购买食物";
	}

	private string BuildKingdomMercenaryInviteOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom kingdom = ResolveHeroKingdom(hero);
		string kingdomName = ResolveKnownKingdomName(kingdom);
		int playerClanTier = ResolveKnownPlayerClanTier();
		string manpowerText = BuildKingdomManpowerNeedText(kingdom);
		string authorityText = IsKingdomLeader(hero, kingdom)
			? "你是" + kingdomName + "的国王，有权邀请对方签订雇佣兵契约"
			: "你是" + kingdomName + "的正式领主，可以代表王国邀请对方成为雇佣兵";
		return "[AFEF NPC行为补充] " + npcName + "，" + authorityText + "。" + manpowerText + "你判断" + kingdomName + "当前缺少可快速补充战力的雇佣兵。你注意到" + playerName + "的玩家家族等级为 " + playerClanTier + "，已经达到雇佣兵门槛 " + MercenaryInviteMinPlayerClanTier + "，且当前没有以正式封臣身份效力于其他王国。你主动追上" + playerName + "，决定邀请" + playerName + "以雇佣兵身份为" + kingdomName + "效力。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要在这条主动请求里把普通领主说成可以授予正式封臣身份。";
	}

	private string BuildKingdomVassalInviteOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom kingdom = ResolveHeroKingdom(hero);
		string kingdomName = ResolveKnownKingdomName(kingdom);
		int playerClanTier = ResolveKnownPlayerClanTier();
		string manpowerText = BuildKingdomManpowerNeedText(kingdom);
		string playerState = IsPlayerMercenaryOfKingdom(kingdom)
			? playerName + "当前已经以雇佣兵身份为" + kingdomName + "效力"
			: playerName + "当前没有以正式封臣身份效力于其他王国";
		return "[AFEF NPC行为补充] " + npcName + "，你是" + kingdomName + "的国王，有权同时邀请" + playerName + "成为正式封臣或签订雇佣兵契约。" + manpowerText + "你判断" + kingdomName + "当前缺少能长期治理土地、承担军役和参与王国政治的正式封臣。" + playerState + "。" + playerName + "的玩家家族等级为 " + playerClanTier + "，已经达到正式封臣门槛 " + VassalInviteMinPlayerClanTier + "，也达到雇佣兵门槛 " + MercenaryInviteMinPlayerClanTier + "。你主动追上" + playerName + "，决定试探并邀请" + playerName + "为" + kingdomName + "效力，可以重点提出正式封臣身份，也可以把雇佣兵契约作为较低承诺的选择。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。";
	}

	private string BuildFoodShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		int foodDays = SafeFoodDays(party);
		int totalFood = 0;
		bool isTestFallback = _activeSession?.IsTestFallback == true;
		try
		{
			totalFood = party?.ItemRoster?.TotalFood ?? 0;
		}
		catch
		{
		}
		if (isTestFallback)
		{
			return "[AFEF NPC行为补充] " + npcName + "，测试模式下你被选为 NPC 主动接触测试对象。你当前队伍需要补充或储备食物，剩余食物约可维持 " + foodDays + " 天，库存食物数量约为 " + totalFood + "。你主动追上" + playerName + "，决定向" + playerName + "询问能否购买或获得一些食物。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。";
		}
		return "[AFEF NPC行为补充] " + npcName + "，你现在部队缺少食物，剩余食物约可维持 " + foodDays + " 天，库存食物数量约为 " + totalFood + "。你主动追上" + playerName + "，决定向" + playerName + "请求援助或购买食物。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。";
	}

	private string BuildMoneyShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		int partyGold = SafePartyTradeGold(party);
		int totalWage = SafeTotalWage(party);
		float unpaidWages = SafeUnpaidWages(party);
		if (party == null && _activeSession != null)
		{
			partyGold = _activeSession.LastKnownPartyGold;
			totalWage = _activeSession.LastKnownTotalWage;
			unpaidWages = _activeSession.LastKnownUnpaidWages;
		}
		string wageText = totalWage > 0 ? ("，每日军饷约为 " + totalWage + " 第纳尔，可支撑军饷约 " + CalculateWageDays(partyGold, totalWage).ToString("0.0") + " 天") : "";
		string unpaidText = unpaidWages > 0f ? ("，并且已有约 " + (unpaidWages * 100f).ToString("0") + "% 的军饷未能支付") : "";
		if (_activeSession?.IsTestFallback == true)
		{
			return "[AFEF NPC行为补充] " + npcName + "，测试模式下你被选为 NPC 主动接触测试对象。你当前可用现金约为 " + partyGold + " 第纳尔" + wageText + unpaidText + "。你主动追上" + playerName + "，决定向" + playerName + "询问是否有资助、预付款、雇佣或交易周转的机会。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何借款、欠款、还款承诺或记账已经由系统成立。";
		}
		return "[AFEF NPC行为补充] " + npcName + "，你现在资金短缺，当前可用现金约为 " + partyGold + " 第纳尔" + wageText + unpaidText + "。你主动追上" + playerName + "，决定向" + playerName + "寻求资助、预付款、雇佣或交易周转的机会。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何借款、欠款、还款承诺或记账已经由系统成立。";
	}

	private string BuildTroopShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		int memberCount = SafeMemberCount(party);
		int partySizeLimit = SafePartySizeLimit(party);
		int availableWageBudget = SafeAvailableWageBudget(party);
		if (party == null && _activeSession != null)
		{
			memberCount = _activeSession.LastKnownMemberCount;
			partySizeLimit = _activeSession.LastKnownPartySizeLimit;
			availableWageBudget = _activeSession.LastKnownAvailableWageBudget;
		}
		int missing = Math.Max(0, partySizeLimit - memberCount);
		string ratioText = partySizeLimit > 0 ? ("，约为上限的 " + (CalculatePartySizeRatio(memberCount, partySizeLimit) * 100f).ToString("0") + "%") : "";
		string wageText = availableWageBudget > 0 ? ("，当前可用军饷预算约为 " + availableWageBudget + " 第纳尔") : "";
		if (_activeSession?.IsTestFallback == true)
		{
			return "[AFEF NPC行为补充] " + npcName + "，测试模式下你被选为 NPC 主动接触测试对象。你当前部队人数约为 " + memberCount + "/" + partySizeLimit + ratioText + "，缺口约 " + missing + " 人" + wageText + "。你主动追上" + playerName + "，决定向" + playerName + "询问是否有士兵、俘虏、雇佣兵、护卫或短期军事合作的机会。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。";
		}
		return "[AFEF NPC行为补充] " + npcName + "，你现在部队兵力不足，当前部队人数约为 " + memberCount + "/" + partySizeLimit + ratioText + "，缺口约 " + missing + " 人" + wageText + "。你主动追上" + playerName + "，决定向" + playerName + "寻求士兵、俘虏、雇佣兵、护卫或短期军事合作的机会。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。";
	}

	private string BuildPrisonerOverloadOpeningFact(MobileParty party, string playerName, string npcName)
	{
		int prisonerCount = SafePrisonerCount(party);
		int prisonerSizeLimit = SafePrisonerSizeLimit(party);
		int heroPrisonerCount = SafeHeroPrisonerCount(party);
		if (party == null && _activeSession != null)
		{
			prisonerCount = _activeSession.LastKnownPrisonerCount;
			prisonerSizeLimit = _activeSession.LastKnownPrisonerSizeLimit;
			heroPrisonerCount = _activeSession.LastKnownHeroPrisonerCount;
		}
		string ratioText = prisonerSizeLimit > 0 ? ("，约为俘虏容量的 " + (CalculatePrisonerSizeRatio(prisonerCount, prisonerSizeLimit) * 100f).ToString("0") + "%") : "";
		string heroText = heroPrisonerCount > 0 ? ("，其中包括 " + heroPrisonerCount + " 名英雄俘虏") : "";
		if (_activeSession?.IsTestFallback == true)
		{
			return "[AFEF NPC行为补充] " + npcName + "，测试模式下你被选为 NPC 主动接触测试对象。你当前队伍俘虏约为 " + prisonerCount + "/" + prisonerSizeLimit + ratioText + heroText + "。你主动追上" + playerName + "，决定向" + playerName + "询问是否愿意赎买、接收、转运俘虏，或帮助联系赎金渠道。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何赎买、付款、转移俘虏或记账已经由系统成立。";
		}
		return "[AFEF NPC行为补充] " + npcName + "，你现在队伍俘虏负担过重，当前俘虏约为 " + prisonerCount + "/" + prisonerSizeLimit + ratioText + heroText + "。你主动追上" + playerName + "，决定向" + playerName + "询问是否愿意赎买、接收、转运俘虏，或帮助联系赎金渠道。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何赎买、付款、转移俘虏或记账已经由系统成立。";
	}

	private static string BuildOpeningPrompt(IEnumerable<string> needTypes)
	{
		List<string> normalized = NormalizeNeedTypes(needTypes, NeedFoodShortage);
		if (normalized.Count <= 1)
		{
			return BuildOpeningPrompt(normalized.Count > 0 ? normalized[0] : NeedFoodShortage);
		}
		List<string> labels = normalized.Take(3).Select(GetNeedPromptLabel).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		return "请你先开口说明自己主动追上玩家的来意。你同时有多个请求，可以按紧急程度合并表达，依次围绕：" + string.Join("；", labels) + "。只输出你作为NPC说出的话。不要假定任何交易、赎买、转移俘虏、借款、欠款、还款承诺、记账、入队或效力关系已经成立。";
	}

	private static string BuildOpeningPrompt(string needType)
	{
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕你的王国缺少长期封臣，作为国王邀请玩家为你的王国效力。玩家家族等级已达到封臣门槛，你可以同时提出正式封臣身份或雇佣兵契约两个方向。只输出你作为NPC说出的话。";
		}
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕你的王国缺少雇佣兵，并且玩家家族等级已达到雇佣兵门槛，邀请玩家以雇佣兵身份为你的王国效力。只输出你作为NPC说出的话。";
		}
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕当前兵力不足请求士兵、俘虏、雇佣兵、护卫或短期军事合作。只输出你作为NPC说出的话。";
		}
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕当前俘虏负担过重，请求赎买、接收、转运俘虏，或帮助联系赎金渠道。只输出你作为NPC说出的话。不要假定任何赎买、付款、转移俘虏或记账已经成立。";
		}
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕当前缺钱、军饷或资金周转压力请求资助、预付款、雇佣或交易机会。只输出你作为NPC说出的话。不要假定任何借款、欠款、还款承诺或记账已经成立。";
		}
		return "请你先开口说明自己主动追上玩家的来意，围绕当前缺粮处境请求援助或购买食物。只输出你作为NPC说出的话。";
	}

	private static string GetNeedPromptLabel(string needType)
	{
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
		{
			return "王国缺少长期封臣，作为国王邀请玩家成为正式封臣，也可提出雇佣兵契约";
		}
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
		{
			return "王国缺少雇佣兵，邀请玩家以雇佣兵身份效力";
		}
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "当前兵力不足，请求士兵、俘虏、雇佣兵、护卫或短期军事合作";
		}
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase))
		{
			return "当前俘虏负担过重，请求赎买、接收、转运俘虏，或帮助联系赎金渠道";
		}
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "当前缺钱、军饷或资金周转压力，请求资助、预付款、雇佣或交易机会";
		}
		return "当前缺粮，请求援助或购买食物";
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
			if (mainParty.IsInRaftState)
			{
				reason = "main_party_raft";
				return true;
			}
			if (mainParty.IsCurrentlyAtSea)
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
		KingdomManpowerNeedSnapshot snapshot = BuildKingdomManpowerNeedSnapshot(kingdom);
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
#if BANNERLORD_1_4_OR_GREATER
			float baseDistance = party?.IsCurrentlyAtSea == true
				? Campaign.Current.Models.EncounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty
				: Campaign.Current.Models.EncounterModel.NeededMaximumLandDistanceForEncounteringMobileParty;
#else
			float baseDistance = Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringMobileParty;
#endif
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
		public string NeedType { get; set; }
		public List<string> NeedTypes { get; set; }
		public float NeedUrgency { get; set; }
		public string TriggerSource { get; set; }
		public bool KnownMajorBeforeRequest { get; set; }
		public int EffectiveNotorietyAtRequest { get; set; }
		public float NeedDrivenChance { get; set; }
		public float NotorietyDrivenChance { get; set; }
		public float SelectedNeedUrgency { get; set; }
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
		public int KingdomMercenaryInvite { get; set; }
		public int KingdomVassalInvite { get; set; }
		public int NeedCandidates { get; set; }
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
				+ " mercenaryInvite=" + KingdomMercenaryInvite
				+ " vassalInvite=" + KingdomVassalInvite
				+ " needCandidates=" + NeedCandidates
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
