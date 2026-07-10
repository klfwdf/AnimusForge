using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
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
	private const string TriggerSourceNeedDriven = "NeedDriven";
	private const string TriggerSourceNotorietyDriven = "NotorietyDriven";
	private const int MercenaryInviteMinPlayerClanTier = 1;
	private const int VassalInviteMinPlayerClanTier = 2;
	private const float KingdomStrongEnoughToSkipMercenaryRatio = 3f;
	private const float ActiveRequestTtlHours = 18f;

	private ProactiveNpcRequestSession _activeSession;
	private Dictionary<string, float> _heroCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, float> _needTypeFatigueUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
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
				NeedTypeFatigueUntilDays = _needTypeFatigueUntilDays,
				GlobalCooldownUntilHours = _globalCooldownUntilHours,
				LastScanHour = _lastScanHour
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
		}
		catch (Exception ex)
		{
			_activeSession = null;
			_heroCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			_needTypeFatigueUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
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
		ProactiveCandidate candidate = FindBestRequestCandidate(settings, out CandidateScanStats stats);
		if (candidate == null)
		{
			Logger.Log("ProactiveNpcRequest", "scan no candidate: " + stats?.ToLogString());
			return;
		}
		Logger.Log("ProactiveNpcRequest", "scan selected: triggerSource=" + (candidate.TriggerSource ?? "") + " knownMajorBefore=" + candidate.KnownMajorBeforeRequest + " effectiveNotoriety=" + candidate.EffectiveNotorietyAtRequest + " needChance=" + candidate.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + candidate.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + candidate.SelectedNeedUrgency.ToString("0.##") + " typeFatigueMultiplier=" + candidate.NeedTypeFatigueMultiplier.ToString("0.##") + " typeFatigueRemainingDays=" + candidate.NeedTypeFatigueRemainingDays.ToString("0.##") + " need=" + (candidate.NeedType ?? "") + " needs=" + JoinNeedTypesForLog(candidate.NeedTypes, candidate.NeedType) + " hero=" + (candidate.Hero?.StringId ?? "") + " party=" + (candidate.Party?.StringId ?? "") + " kingdom=" + (candidate.TargetKingdomId ?? "") + " playerClanTier=" + candidate.PlayerClanTier + " isKingdomLeader=" + candidate.TargetHeroIsKingdomLeader + " kingdomVassals=" + candidate.KingdomFormalVassalClanCount + "/" + candidate.KingdomTargetVassalClanCount + " kingdomMercs=" + candidate.KingdomMercenaryClanCount + "/" + candidate.KingdomTargetMercenaryClanCount + " kingdomFiefScore=" + candidate.KingdomFiefScore + " kingdomWars=" + candidate.KingdomWarKingdomCount + " kingdomPowerRatio=" + candidate.KingdomPowerRatioToEnemies.ToString("0.00") + " distance=" + candidate.Distance.ToString("0.0") + " foodDays=" + candidate.FoodDays + " partyGold=" + candidate.PartyGold + " totalWage=" + candidate.TotalWage + " unpaidWages=" + candidate.UnpaidWages.ToString("0.00") + " troops=" + candidate.MemberCount + "/" + candidate.PartySizeLimit + " troopRatio=" + candidate.PartySizeRatio.ToString("0.00") + " prisoners=" + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit + " heroPrisoners=" + candidate.HeroPrisonerCount + " prisonerRatio=" + candidate.PrisonerSizeRatio.ToString("0.00") + " wageBudget=" + candidate.AvailableWageBudget + " testFallback=" + candidate.IsTestFallback + " stats=" + stats?.ToLogString());
		Logger.Log("ProactiveNpcRequest", "scan selected extra needs=" + JoinNeedTypesForLog(candidate.NeedTypes, candidate.NeedType) + " morale=" + candidate.Morale.ToString("0.0") + " mounts=" + candidate.MountCount + " packAnimals=" + candidate.PackAnimalCount + " mountRatio=" + candidate.MountRatio.ToString("0.00") + " carry=" + candidate.TotalWeightCarried.ToString("0.0") + "/" + candidate.InventoryCapacity + " carryRatio=" + candidate.CarryRatio.ToString("0.00") + " clanGold=" + candidate.ClanGold + " clanDebt=" + candidate.ClanDebtToKingdom + " captiveClanHeroes=" + candidate.CaptiveClanHeroCount + " captiveHero=" + (candidate.CaptiveClanHeroName ?? "") + " captiveHolder=" + (candidate.CaptiveClanHeroHolderName ?? "") + " captiveLeader=" + candidate.CaptiveClanLeaderHeld + " marriageAdults=" + candidate.MarriageAdultClanHeroCount + " unmarriedAdults=" + candidate.MarriageUnmarriedAdultCount + " firstUnmarried=" + (candidate.MarriageFirstUnmarriedName ?? "") + " revengeScore=" + candidate.RevengePressureScore.ToString("0.0") + " revengeTarget=" + (candidate.RevengeTargetName ?? "") + " fiefProblems=" + candidate.FiefProblemCount + " fief=" + (candidate.FiefProblemName ?? "") + " fiefIssue=" + (candidate.FiefIssueText ?? "") + " allyInfluence=" + candidate.ClanInfluence.ToString("0.0") + " friendlyClans=" + candidate.FriendlyClanCount + " hostileClans=" + candidate.HostileClanCount);
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
					candidates.Add(combinedCandidate);
				}
			}
		}
		return candidates
			.OrderByDescending(c => c.NeedUrgency * c.NeedTypeFatigueMultiplier)
			.ThenByDescending(c => c.NeedUrgency)
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
		float typeFatigueMultiplier = Clamp(candidate.NeedTypeFatigueMultiplier, 0f, 1f);
		if (typeFatigueMultiplier < 0.999f && stats != null)
		{
			stats.TypeFatiguedCandidates++;
		}
		float needChance = Clamp(urgency * globalScale * knownMultiplier * typeFatigueMultiplier, 0f, 100f);
		float notorietyChance = knownMajor
			? 0f
			: Clamp(effectiveNotoriety * GetEffectiveNotorietyChanceMultiplier(settings) * (urgency / 100f) * globalScale * typeFatigueMultiplier, 0f, 100f);
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
		AddLetterNeedCandidate(candidates, TryBuildMarriageAlliancePressureCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildRevengePressureCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildFiefGovernanceAnxietyCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildAllySupportCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildKingdomMercenaryInviteCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildKingdomVassalInviteCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPoliticalAgendaCandidate, source, settings);
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

	private static ProactiveCandidate BuildLetterNeedBaseCandidate(Hero hero, DuelSettings settings)
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
			ClanCaptiveSnapshot captive = BuildClanCaptiveSnapshot(hero);
			Kingdom kingdom = ResolveHeroKingdom(hero);
			MarriageAllianceSnapshot marriage = BuildMarriageAllianceSnapshot(hero);
			FiefGovernanceSnapshot fiefs = BuildFiefGovernanceSnapshot(clan, settings);
			AllySupportSnapshot allies = BuildAllySupportSnapshot(clan, kingdom, settings);
			RevengePressureSnapshot revenge = BuildRevengePressureSnapshot(hero, kingdom, captive, fiefs);
			KingdomManpowerNeedSnapshot manpower = BuildKingdomManpowerNeedSnapshot(kingdom);
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
		string evidence = BuildLetterNeedEvidence(candidate, needType);
		return "[AFEF NPC行为补充] " + npcName + "当前确实存在“" + GetNeedDisplayName(needType) + "”需求（紧急度 " + Clamp(candidate?.NeedUrgency ?? 0f, 0f, 100f).ToString("0") + "/100）" + evidence + "。" + npcName + "决定主动写信给" + playerName + "，本信只围绕这一项需求提出请求；不要假定玩家已经同意，也不要把未发生的交易、承诺或机制结果写成事实。";
	}

	private static string BuildLetterNeedIntent(ProactiveCandidate candidate)
	{
		string needType = NormalizeNeedType(candidate?.NeedType);
		return "围绕“" + GetNeedDisplayName(needType) + "”写一封简洁来信，只提出一个主要请求，并严格使用已提供的游戏事实。";
	}

	private static string BuildLetterNeedEvidence(ProactiveCandidate candidate, string needType)
	{
		if (candidate == null)
		{
			return "";
		}
		if (string.Equals(needType, NeedFoodShortage, StringComparison.OrdinalIgnoreCase)) return "，队伍食物预计还能维持 " + candidate.FoodDays + " 天";
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase)) return "，队伍现金 " + candidate.PartyGold + "，每日军饷约 " + candidate.TotalWage;
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase)) return "，队伍兵力 " + candidate.MemberCount + "/" + candidate.PartySizeLimit;
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase)) return "，俘虏 " + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit;
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase)) return "，被俘家族成员 " + candidate.CaptiveClanHeroCount + " 人，首名为 " + (candidate.CaptiveClanHeroName ?? "未知");
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase)) return "，队伍士气约 " + candidate.Morale.ToString("0");
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase)) return "，坐骑 " + candidate.MountCount + " 匹，队伍成员 " + candidate.MemberCount + " 人";
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase)) return "，负重约为容量的 " + (candidate.CarryRatio * 100f).ToString("0") + "%";
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase)) return "，家族金库 " + candidate.ClanGold + "，王国债务 " + candidate.ClanDebtToKingdom;
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase)) return "，成年成员 " + candidate.MarriageAdultClanHeroCount + " 人，未婚成年成员 " + candidate.MarriageUnmarriedAdultCount + " 人";
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase)) return "，相关对象为 " + (candidate.RevengeTargetName ?? "未知") + "，缘由为 " + (candidate.RevengeReasonText ?? "当前家族压力");
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase)) return "，问题封地 " + (candidate.FiefProblemName ?? "未知") + "，问题数 " + candidate.FiefProblemCount + "，情况为 " + (candidate.FiefIssueText ?? "治理压力");
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase)) return "，家族影响力 " + candidate.ClanInfluence.ToString("0") + "，友好家族 " + candidate.FriendlyClanCount + "，敌对家族 " + candidate.HostileClanCount;
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)) return "，王国当前雇佣兵家族 " + candidate.KingdomMercenaryClanCount + "/目标 " + candidate.KingdomTargetMercenaryClanCount;
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)) return "，王国当前封臣家族 " + candidate.KingdomFormalVassalClanCount + "/目标 " + candidate.KingdomTargetVassalClanCount;
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase)) return "，当前王国内确实存在待处理议程";
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase)) return "，双方王国当前存在可谈判的外交事项";
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
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase)) return "联姻压力";
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase)) return "复仇或营救压力";
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase)) return "封地治理压力";
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase)) return "缺少盟友支持";
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)) return "王国缺少雇佣兵";
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)) return "王国缺少封臣";
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase)) return "王国政治议程";
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
			ClanCaptiveSnapshot captiveSnapshot = BuildClanCaptiveSnapshot(hero);
			Kingdom targetKingdom = ResolveHeroKingdom(hero);
			MarriageAllianceSnapshot marriageSnapshot = BuildMarriageAllianceSnapshot(hero);
			FiefGovernanceSnapshot fiefGovernanceSnapshot = BuildFiefGovernanceSnapshot(hero.Clan, settings);
			AllySupportSnapshot allySupportSnapshot = BuildAllySupportSnapshot(hero.Clan, targetKingdom, settings);
			RevengePressureSnapshot revengeSnapshot = BuildRevengePressureSnapshot(hero, targetKingdom, captiveSnapshot, fiefGovernanceSnapshot);
			if (atWarWithPlayer && !CanBuildWartimeDiplomacyCandidate(hero, targetKingdom))
			{
				skipReason = "war_non_diplomacy";
				return false;
			}
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
		}
		List<ProactiveCandidate> ordered = needCandidates
			.Where(c => c != null && !string.IsNullOrWhiteSpace(c.NeedType) && IsPlayerEligibleForProactiveNeed(c, c.NeedType, out _))
			.OrderByDescending(c => c.NeedUrgency * c.NeedTypeFatigueMultiplier)
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
			if (string.Equals(normalized, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForDiplomacyRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForMarriageAllianceRequest(candidate, out reason);
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
		if (source == null || !IsMountShortageNeedMet(source, settings, out float urgency))
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
		Logger.Log("ProactiveNpcRequest", "started request triggerSource=" + (_activeSession.TriggerSource ?? "") + " knownMajorBefore=" + _activeSession.KnownMajorBeforeRequest + " effectiveNotoriety=" + _activeSession.EffectiveNotorietyAtRequest + " needChance=" + _activeSession.NeedDrivenChance.ToString("0.##") + " notorietyChance=" + _activeSession.NotorietyDrivenChance.ToString("0.##") + " selectedUrgency=" + _activeSession.SelectedNeedUrgency.ToString("0.##") + " typeFatigueMultiplier=" + _activeSession.NeedTypeFatigueMultiplierAtSelection.ToString("0.##") + " typeFatigueRemainingDays=" + _activeSession.NeedTypeFatigueRemainingDaysAtSelection.ToString("0.##") + " need=" + _activeSession.NeedType + " needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " hero=" + _activeSession.HeroId + " party=" + _activeSession.PartyId + " kingdom=" + (_activeSession.TargetKingdomId ?? "") + " playerClanTier=" + _activeSession.PlayerClanTier + " isKingdomLeader=" + _activeSession.TargetHeroIsKingdomLeader + " kingdomVassals=" + _activeSession.KingdomFormalVassalClanCount + "/" + _activeSession.KingdomTargetVassalClanCount + " kingdomMercs=" + _activeSession.KingdomMercenaryClanCount + "/" + _activeSession.KingdomTargetMercenaryClanCount + " kingdomFiefScore=" + _activeSession.KingdomFiefScore + " kingdomWars=" + _activeSession.KingdomWarKingdomCount + " kingdomPowerRatio=" + _activeSession.KingdomPowerRatioToEnemies.ToString("0.00") + " foodDays=" + candidate.FoodDays + " partyGold=" + candidate.PartyGold + " totalWage=" + candidate.TotalWage + " unpaidWages=" + candidate.UnpaidWages.ToString("0.00") + " troops=" + candidate.MemberCount + "/" + candidate.PartySizeLimit + " troopRatio=" + candidate.PartySizeRatio.ToString("0.00") + " prisoners=" + candidate.PrisonerCount + "/" + candidate.PrisonerSizeLimit + " heroPrisoners=" + candidate.HeroPrisonerCount + " prisonerRatio=" + candidate.PrisonerSizeRatio.ToString("0.00") + " morale=" + candidate.Morale.ToString("0.0") + " mounts=" + candidate.MountCount + " packAnimals=" + candidate.PackAnimalCount + " mountRatio=" + candidate.MountRatio.ToString("0.00") + " carry=" + candidate.TotalWeightCarried.ToString("0.0") + "/" + candidate.InventoryCapacity + " carryRatio=" + candidate.CarryRatio.ToString("0.00") + " clanGold=" + candidate.ClanGold + " clanDebt=" + candidate.ClanDebtToKingdom + " captiveClanHeroes=" + candidate.CaptiveClanHeroCount + " captiveLeader=" + candidate.CaptiveClanLeaderHeld + " wageBudget=" + candidate.AvailableWageBudget + " distance=" + candidate.Distance.ToString("0.0") + " testFallback=" + candidate.IsTestFallback);
		Logger.Log("ProactiveNpcRequest", "started request extra needs=" + JoinNeedTypesForLog(_activeSession.NeedTypes, _activeSession.NeedType) + " marriageAdults=" + _activeSession.LastKnownMarriageAdultClanHeroCount + " unmarriedAdults=" + _activeSession.LastKnownMarriageUnmarriedAdultCount + " firstUnmarried=" + (_activeSession.LastKnownMarriageFirstUnmarriedName ?? "") + " revengeScore=" + _activeSession.LastKnownRevengePressureScore.ToString("0.0") + " revengeTarget=" + (_activeSession.LastKnownRevengeTargetName ?? "") + " revengeReason=" + (_activeSession.LastKnownRevengeReasonText ?? "") + " fiefProblems=" + _activeSession.LastKnownFiefProblemCount + " fief=" + (_activeSession.LastKnownFiefProblemName ?? "") + " fiefIssue=" + (_activeSession.LastKnownFiefIssueText ?? "") + " fiefLoyalty=" + _activeSession.LastKnownFiefLoyalty.ToString("0.0") + " fiefSecurity=" + _activeSession.LastKnownFiefSecurity.ToString("0.0") + " fiefGarrison=" + _activeSession.LastKnownFiefGarrisonCount + " allyInfluence=" + _activeSession.LastKnownClanInfluence.ToString("0.0") + " friendlyClans=" + _activeSession.LastKnownFriendlyClanCount + " hostileClans=" + _activeSession.LastKnownHostileClanCount);
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
		int fatigueDays = GetEffectiveNeedTypeFatigueDays(settings);
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
		if (string.Equals(text, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return NeedDiplomacy;
		}
		return "";
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
		string needType = activeNeedTypes.Count > 0 ? activeNeedTypes[0] : (string.IsNullOrWhiteSpace(_activeSession?.NeedType) ? NeedFoodShortage : _activeSession.NeedType);
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return BuildDiplomacyOpeningFact(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPoliticalAgendaOpeningFact(hero, playerName, npcName);
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
			int adults = Math.Max(0, _activeSession?.LastKnownMarriageAdultClanHeroCount ?? 0);
			int unmarried = Math.Max(0, _activeSession?.LastKnownMarriageUnmarriedAdultCount ?? 0);
			string firstName = (_activeSession?.LastKnownMarriageFirstUnmarriedName ?? "").Trim();
			string firstText = string.IsNullOrWhiteSpace(firstName) ? "" : "，其中包括 " + firstName;
			return "你的家族成年核心成员偏少，成年成员约 " + adults + " 人，未婚成年成员约 " + unmarried + " 人" + firstText + "；你想与" + playerName + "谈联姻、介绍合适婚配、家族继承安全或家族间长期互助";
		}
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			string reason = (_activeSession?.LastKnownRevengeReasonText ?? "").Trim();
			string target = (_activeSession?.LastKnownRevengeTargetName ?? "").Trim();
			string targetText = string.IsNullOrWhiteSpace(target) ? "" : "，矛头可能指向 " + target;
			return "你的家族正承受复仇压力" + (string.IsNullOrWhiteSpace(reason) ? "" : "，原因是" + reason) + targetText + "；你想请" + playerName + "协助打听敌情、赎回亲族、报复敌人、护送或参与后续军事行动";
		}
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			string fief = (_activeSession?.LastKnownFiefProblemName ?? "").Trim();
			string issue = (_activeSession?.LastKnownFiefIssueText ?? "").Trim();
			float loyalty = _activeSession?.LastKnownFiefLoyalty ?? -1f;
			float security = _activeSession?.LastKnownFiefSecurity ?? -1f;
			int garrison = _activeSession?.LastKnownFiefGarrisonCount ?? -1;
			string fiefText = string.IsNullOrWhiteSpace(fief) ? "某处封地" : fief;
			return "你的家族封地治理出现焦虑，重点是 " + fiefText + "，问题包括 " + (string.IsNullOrWhiteSpace(issue) ? "忠诚、治安、驻军或战事压力" : issue) + "；当前忠诚约 " + loyalty.ToString("0") + "，治安约 " + security.ToString("0") + "，驻军约 " + garrison + "；你想请" + playerName + "提供粮食、金钱、护送、驻军、治安或政治支持";
		}
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			float influence = _activeSession?.LastKnownClanInfluence ?? 0f;
			int friendly = _activeSession?.LastKnownFriendlyClanCount ?? 0;
			int hostile = _activeSession?.LastKnownHostileClanCount ?? 0;
			return "你的家族在王国内显得孤立，影响力约 " + influence.ToString("0") + "，可靠友好家族约 " + friendly + " 个，敌意家族约 " + hostile + " 个；你想与" + playerName + "谈互相背书、投票支持、政治结盟、护送或短期利益交换";
		}
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return BuildDiplomacyOpeningSummary(hero, playerName, npcName);
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveHeroKingdom(hero);
			string kingdomName = ResolveKnownKingdomName(kingdom);
			return "你和" + playerName + "同属" + kingdomName + "，当前王国内有正在公示或等待处理的议程；你主动来请求" + playerName + "在议程、投票或拉票上支持你的政治目标，可以提出报酬或条件，但不要假定玩家已经答应";
		}
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
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			int captiveCount = Math.Max(0, _activeSession?.LastKnownCaptiveClanHeroCount ?? 0);
			string captiveName = (_activeSession?.LastKnownCaptiveClanHeroName ?? "").Trim();
			string holderName = (_activeSession?.LastKnownCaptiveClanHeroHolderName ?? "").Trim();
			bool leaderHeld = _activeSession?.LastKnownCaptiveClanLeaderHeld == true;
			string captiveText = string.IsNullOrWhiteSpace(captiveName) ? "家族成员" : captiveName;
			string holderText = string.IsNullOrWhiteSpace(holderName) ? "" : "，目前看押方似乎是" + holderName;
			string leaderText = leaderHeld ? "，其中包括家族领袖或关键成员" : "";
			return "你的家族有 " + captiveCount + " 名成员被俘，尤其是" + captiveText + holderText + leaderText + "；你因此想请求" + playerName + "帮忙赎回、营救、斡旋、打听下落或提供赎金渠道";
		}
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			int clanGold = _activeSession?.LastKnownClanGold ?? 0;
			int clanDebt = _activeSession?.LastKnownClanDebtToKingdom ?? 0;
			string debtText = clanDebt > 0 ? "，欠王国债务约 " + clanDebt + " 第纳尔" : "";
			return "你的家族财政紧张，当前家族金库约 " + clanGold + " 第纳尔" + debtText + "；你想向" + playerName + "寻求投资、预付款、贸易周转、雇佣收入或短期资助机会";
		}
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			float totalWeight = SafeTotalWeightCarried(party);
			int capacity = SafeInventoryCapacity(party);
			int packAnimals = SafePackAnimalCount(party);
			if (party == null && _activeSession != null)
			{
				totalWeight = _activeSession.LastKnownTotalWeightCarried;
				capacity = _activeSession.LastKnownInventoryCapacity;
				packAnimals = _activeSession.LastKnownPackAnimalCount;
			}
			float ratio = CalculateCarryRatio(totalWeight, capacity);
			return "你的队伍负重压力很大，当前负重约 " + totalWeight.ToString("0") + "/" + capacity + "，约为容量的 " + (ratio * 100f).ToString("0") + "%，驮畜约 " + packAnimals + " 匹；你想请求" + playerName + "购买、转运、护送或提供驮畜";
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			int memberCount = SafeMemberCount(party);
			int mountCount = SafeMountCount(party);
			int packAnimals = SafePackAnimalCount(party);
			if (party == null && _activeSession != null)
			{
				memberCount = _activeSession.LastKnownMemberCount;
				mountCount = _activeSession.LastKnownMountCount;
				packAnimals = _activeSession.LastKnownPackAnimalCount;
			}
			float mountRatio = CalculateAnimalRatio(mountCount, memberCount);
			return "你的队伍坐骑和机动力不足，当前人数约 " + memberCount + "，坐骑约 " + mountCount + "，约为人数的 " + (mountRatio * 100f).ToString("0") + "%，驮畜约 " + packAnimals + "；你想向" + playerName + "购买马匹、驮畜，或请求帮助摆脱机动劣势";
		}
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			float morale = SafeMorale(party);
			if (party == null && _activeSession != null)
			{
				morale = _activeSession.LastKnownMorale;
			}
			return "你的队伍士气低落，当前士气约 " + morale.ToString("0") + "/100；你想请求" + playerName + "提供补给、金钱、酒食、胜利机会、护送或短期合作来稳定军心";
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

	private string BuildDiplomacyOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom npcKingdom = hero?.Clan?.Kingdom;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		string npcKingdomName = ResolveKnownKingdomName(npcKingdom);
		string playerKingdomName = ResolveKnownKingdomName(playerKingdom);
		string topic = ResolveDiplomacyRequestLabel(npcKingdom, playerKingdom);
		return "[AFEF NPC行为补充] " + npcName + "，你是" + npcKingdomName + "的国王，" + playerName + "是" + playerKingdomName + "的国王。你主动追上" + playerName + "，不是为了宣战，而是想当面发起国王间外交谈判，当前最合适的话题是" + topic + "。你可以提出条件、贡金方向或期限，也可以请求对方考虑共同敌人、贸易利益或战争压力；只有双方明确同意后，系统机制才会在后处理阶段生效，不要假定玩家已经答应。";
	}

	private string BuildPoliticalAgendaOpeningFact(Hero hero, string playerName, string npcName)
	{
		Kingdom kingdom = hero?.Clan?.Kingdom;
		string kingdomName = ResolveKnownKingdomName(kingdom);
		string agendaContext = VoteDealBehavior.BuildPendingDecisionsContext(hero);
		string text = "[AFEF NPC行为补充] " + npcName + "，你和" + playerName + "同属" + kingdomName + "。你主动追上" + playerName + "，是想把当前王国议程、投票或拉票话题摆到台面上，请求" + playerName + "支持你的政治目标。你可以说明想让对方支持哪项议程、哪一方候选或哪种投票立场，也可以提出金钱、利益或未来承诺作为交换；但不要假定玩家已经答应，系统也不会因为这段开场自动记录玩家支持。";
		if (!string.IsNullOrWhiteSpace(agendaContext))
		{
			text += "\n" + agendaContext;
		}
		return text;
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

	private string BuildClanCaptiveOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedClanCaptive, null, ResolveActiveParty(), playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定赎买、营救、放人、付款或承诺已经成立。";
	}

	private string BuildLowMoraleOpeningFact(MobileParty party, string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedLowMorale, null, party, playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何补给、付款、护送或合作已经成立。";
	}

	private string BuildMountShortageOpeningFact(MobileParty party, string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedMountShortage, null, party, playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何马匹买卖、转让、护送或合作已经成立。";
	}

	private string BuildOverburdenedOpeningFact(MobileParty party, string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedOverburdened, null, party, playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何购买、转运、护送、付款或物资转移已经成立。";
	}

	private string BuildClanFinanceStrainOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedClanFinanceStrain, null, ResolveActiveParty(), playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何借款、欠款、还款承诺、投资、交易或记账已经成立。";
	}

	private string BuildMarriageAlliancePressureOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedMarriageAlliancePressure, null, ResolveActiveParty(), playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何婚约、联姻、家族承诺或关系变更已经成立。";
	}

	private string BuildRevengePressureOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedRevengePressure, null, ResolveActiveParty(), playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何攻击、营救、赎买、雇佣或军事承诺已经成立。";
	}

	private string BuildFiefGovernanceAnxietyOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedFiefGovernanceAnxiety, null, ResolveActiveParty(), playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何驻军、金钱、粮食、治理或政治支持已经成立。";
	}

	private string BuildAllySupportOpeningFact(string playerName, string npcName)
	{
		string summary = BuildOpeningNeedSummary(NeedAllySupport, null, ResolveActiveParty(), playerName, npcName);
		return "[AFEF NPC行为补充] " + npcName + "，你主动追上" + playerName + "，并非来开战，而是因为" + summary + "。你应该先开口说明来意，不要把这当作" + playerName + "主动提出的话。不要假定任何结盟、投票、背书、护送或利益交换已经成立。";
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
		List<string> normalized = NormalizeSingleNeedType(needTypes, NeedFoodShortage);
		return BuildOpeningPrompt(normalized.Count > 0 ? normalized[0] : NeedFoodShortage);
	}

	private static string BuildOpeningPrompt(string needType)
	{
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕家族继承与联姻压力，请求介绍婚配、家族互助或长期合作。只输出你作为NPC说出的话。不要假定婚约或承诺已成立。";
		}
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕家族受辱、亲族被俘、封地受袭或战争压力，请求打听、营救、报复或军事协助。只输出你作为NPC说出的话。不要假定行动已成立。";
		}
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕封地忠诚、治安、驻军、劫掠或围困压力，请求粮食、金钱、护送、驻军或政治支持。只输出你作为NPC说出的话。不要假定支持已成立。";
		}
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕家族在王国内孤立、影响力不足或缺少盟友，请求背书、投票支持、政治互助或短期合作。只输出你作为NPC说出的话。不要假定结盟已成立。";
		}
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕家族成员被俘，请求赎回、营救、斡旋、打听下落或赎金渠道。只输出你作为NPC说出的话。不要假定赎买、营救、放人或付款已经成立。";
		}
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕队伍士气低落，请求补给、金钱、酒食、护送、胜利机会或短期合作。只输出你作为NPC说出的话。不要假定任何帮助已经成立。";
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕队伍缺少坐骑或机动不足，请求购买马匹、驮畜、护送或摆脱机动劣势。只输出你作为NPC说出的话。不要假定交易或护送已经成立。";
		}
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕队伍负重压力，请求购买、转运、护送或提供驮畜。只输出你作为NPC说出的话。不要假定购买、付款或物资转移已经成立。";
		}
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕家族财政紧张，请求投资、预付款、贸易周转、雇佣收入或短期资助。只输出你作为NPC说出的话。不要假定借款、欠款、还款承诺、交易或记账已经成立。";
		}
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕国王间外交谈判提出议和、结盟或通商请求。不要主动宣战；只有双方明确同意后才可以让机制生效。只输出你作为NPC说出的话。";
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			return "请你先开口说明自己主动追上玩家的来意，围绕同王国的当前议程、投票或拉票请求玩家支持。可以提出报酬或交换条件，但不要假定玩家已经答应，也不要输出任何系统标签。只输出你作为NPC说出的话。";
		}
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
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
		{
			return "家族成年核心成员偏少或未婚压力，请求联姻介绍、家族互助或长期合作";
		}
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase))
		{
			return "家族亲族被俘、封地受袭或战争压力，请求打听、营救、报复或军事协助";
		}
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase))
		{
			return "封地忠诚、治安、驻军、劫掠或围困压力，请求粮食、金钱、驻军、护送或政治支持";
		}
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
		{
			return "家族在王国内孤立或影响力不足，请求背书、投票支持、政治互助或短期合作";
		}
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase))
		{
			return "家族成员被俘，请求赎回、营救、斡旋或打听下落";
		}
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase))
		{
			return "队伍士气低落，请求补给、金钱、酒食、护送或短期合作";
		}
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase))
		{
			return "队伍缺少坐骑或机动不足，请求马匹、驮畜或护送";
		}
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase))
		{
			return "队伍负重压力过高，请求购买、转运、护送或驮畜";
		}
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase))
		{
			return "家族财政紧张，请求投资、预付款、贸易周转或短期资助";
		}
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
		{
			return "国王间外交请求：议和、结盟或通商，不主动宣战";
		}
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
		{
			return "同王国议程、投票或拉票请求玩家支持，可谈报酬但不自动记录承诺";
		}
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

	private static int GetEffectiveNeedTypeFatigueDays(DuelSettings settings)
	{
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

	private sealed class ClanCaptiveSnapshot
	{
		public int Count { get; set; }
		public string FirstHeroName { get; set; }
		public string FirstHolderName { get; set; }
		public bool LeaderHeld { get; set; }
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

	private sealed class AllySupportSnapshot
	{
		public float ClanInfluence { get; set; }
		public int FriendlyClanCount { get; set; }
		public int HostileClanCount { get; set; }
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
		public int MarriageAlliancePressure { get; set; }
		public int RevengePressure { get; set; }
		public int FiefGovernanceAnxiety { get; set; }
		public int AllySupport { get; set; }
		public int KingdomMercenaryInvite { get; set; }
		public int KingdomVassalInvite { get; set; }
		public int PoliticalAgenda { get; set; }
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
				+ " marriageAlliance=" + MarriageAlliancePressure
				+ " revengePressure=" + RevengePressure
				+ " fiefGovernance=" + FiefGovernanceAnxiety
				+ " allySupport=" + AllySupport
				+ " mercenaryInvite=" + KingdomMercenaryInvite
				+ " vassalInvite=" + KingdomVassalInvite
				+ " politicalAgenda=" + PoliticalAgenda
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
