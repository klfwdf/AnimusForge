using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ProactiveNpcRequestBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_af_proactive_npc_request_state_v1";
	private const string NeedFoodShortage = "FoodShortage";
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
		}
		dataStore.SyncData(StorageKey, ref storageJson);
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
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
			body = name + " 的队伍追上了你。对方不是来开战，而是因为粮食短缺主动找你商议。";
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
		int chance = GetEffectiveChancePercent(settings);
		if (chance <= 0 || (chance < 100 && MBRandom.RandomFloat > chance / 100f))
		{
			Logger.Log("ProactiveNpcRequest", "scan skipped: chance roll failed chance=" + chance);
			return;
		}
		ProactiveCandidate candidate = FindBestFoodShortageCandidate(settings, out CandidateScanStats stats);
		if (candidate == null)
		{
			Logger.Log("ProactiveNpcRequest", "scan no candidate: " + stats?.ToLogString());
			return;
		}
		Logger.Log("ProactiveNpcRequest", "scan selected: hero=" + (candidate.Hero?.StringId ?? "") + " party=" + (candidate.Party?.StringId ?? "") + " distance=" + candidate.Distance.ToString("0.0") + " foodDays=" + candidate.FoodDays + " testFallback=" + candidate.IsTestFallback + " stats=" + stats?.ToLogString());
		StartRequest(candidate, settings);
	}

	private ProactiveCandidate FindBestFoodShortageCandidate(DuelSettings settings, out CandidateScanStats stats)
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
		List<ProactiveCandidate> testFallbackCandidates = new List<ProactiveCandidate>();
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
			if (IsFoodShortageNeedMet(party, candidate.FoodDays, settings))
			{
				stats.FoodShortage++;
				candidates.Add(candidate);
			}
			else if (settings.ProactiveNpcRequestTestMode)
			{
				stats.TestFallbackEligible++;
				candidate.IsTestFallback = true;
				testFallbackCandidates.Add(candidate);
			}
		}
		ProactiveCandidate selected = candidates.OrderBy(c => c.Distance).FirstOrDefault();
		if (selected == null && settings.ProactiveNpcRequestTestMode)
		{
			selected = testFallbackCandidates.OrderBy(c => c.Distance).ThenBy(c => c.FoodDays).FirstOrDefault();
			if (selected != null)
			{
				stats.SelectedByTestFallback = true;
			}
		}
		return selected;
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
			if (!settings.ProactiveNpcRequestTestMode && IsOnCooldown(_needCooldownUntilDays, NeedFoodShortage, nowDays))
			{
				skipReason = "need_cooldown";
				return false;
			}
			int foodDays = SafeFoodDays(party);
			float distance = GetDistanceToMainParty(party, mainParty);
			candidate = new ProactiveCandidate
			{
				Party = party,
				Hero = hero,
				Distance = distance,
				FoodDays = foodDays
			};
			return true;
		}
		catch
		{
			skipReason = "exception";
			return false;
		}
	}

	private static bool IsFoodShortageNeedMet(MobileParty party, int foodDays, DuelSettings settings)
	{
		int threshold = Clamp(settings?.ProactiveNpcRequestFoodDaysThreshold ?? 3, 0, 15);
		try
		{
			if (party?.Party?.IsStarving == true)
			{
				return true;
			}
		}
		catch
		{
		}
		return foodDays <= threshold;
	}

	private void StartRequest(ProactiveCandidate candidate, DuelSettings settings)
	{
		MobileParty party = candidate?.Party;
		Hero hero = candidate?.Hero;
		if (party == null || hero == null)
		{
			return;
		}
		_activeSession = new ProactiveNpcRequestSession
		{
			Id = Guid.NewGuid().ToString("N"),
			HeroId = GetHeroKey(hero),
			PartyId = (party.StringId ?? "").Trim(),
			NeedType = NeedFoodShortage,
			Stage = "Chasing",
			CreatedAtHours = NowHours(),
			ExpiresAtHours = NowHours() + ActiveRequestTtlHours,
			LastKnownFoodDays = candidate.FoodDays,
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
		Logger.Log("ProactiveNpcRequest", "started food request hero=" + _activeSession.HeroId + " party=" + _activeSession.PartyId + " foodDays=" + candidate.FoodDays + " distance=" + candidate.Distance.ToString("0.0") + " testFallback=" + candidate.IsTestFallback);
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
		string fact = BuildOpeningFact(hero);
		string prompt = "请你先开口说明自己主动追上玩家的来意，围绕当前缺粮处境请求援助或购买食物。只输出你作为NPC说出的话。";
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
			_needCooldownUntilDays.Remove(NeedFoodShortage);
		}
		else
		{
			_needCooldownUntilDays[NeedFoodShortage] = nowDays + NeedCooldownDays;
		}
	}

	private void CancelActiveSession(string reason, bool releaseParty)
	{
		ProactiveNpcRequestSession session = _activeSession;
		MobileParty party = ResolveActiveParty();
		if (releaseParty)
		{
			ReleasePartyIfStillChasing(party);
		}
		Logger.Log("ProactiveNpcRequest", "cleared active request reason=" + (reason ?? "unknown") + " hero=" + (session?.HeroId ?? ""));
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
		public string Stage { get; set; }
		public float CreatedAtHours { get; set; }
		public float ExpiresAtHours { get; set; }
		public float EncounterOpenedAtHours { get; set; }
		public int LastKnownFoodDays { get; set; }
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
		public bool IsTestFallback { get; set; }
	}

	private sealed class CandidateScanStats
	{
		public bool MainPartyMissing { get; set; }
		public int TotalLordParties { get; set; }
		public int BaseEligible { get; set; }
		public int InRange { get; set; }
		public int OutOfRange { get; set; }
		public int FoodShortage { get; set; }
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
				+ " shortage=" + FoodShortage
				+ " testFallbackEligible=" + TestFallbackEligible
				+ " selectedByTestFallback=" + SelectedByTestFallback
				+ " skips=" + reasons;
		}
	}
}
