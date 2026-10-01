using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class CompanionProactiveChatMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string SessionId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public CompanionProactiveChatMapNotification(string sessionId, string titleText, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "一名队内 Hero 想与你交谈。" : descriptionText))
	{
		SessionId = (sessionId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(titleText) ? "队内 Hero 想与你交谈" : titleText);
	}

	public override bool IsValid()
	{
		return CompanionProactiveChatBehavior.Instance?.IsPendingNotification(SessionId) == true;
	}
}

internal sealed class CompanionProactiveChatMapNotificationItemVM : MapNotificationItemBaseVM
{
	public CompanionProactiveChatMapNotificationItemVM(CompanionProactiveChatMapNotification data)
		: base(data)
	{
		NotificationIdentifier = "education";
		_onInspect = delegate
		{
			if (CompanionProactiveChatBehavior.Instance?.OpenPendingChatFromMap(data.SessionId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

public sealed class CompanionProactiveChatBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_af_companion_proactive_chat_v1";
	private const string StatePending = "Pending";
	private const string StateOpening = "Opening";
	private const string MotiveHeroEvent = "HeroRecentEvent";
	private const string MotivePlayerEvent = "PlayerRecentEvent";
	private const string MotiveCare = "Care";
	private const string MotivePartyMorale = "PartyLowMorale";
	private const string MotivePartyWounded = "PartyWounded";
	private const string MotivePrisoners = "PrisonerPressure";
	private const string MotiveBurden = "BurdenPressure";
	private const string MotiveDuty = "PartyRoleDuty";
	private const string MotiveEmotion = "RelationshipEmotion";
	private const string MotiveRomanticInteraction = "RomanticInteraction";
	private const string MotivePolicyDiscussion = "PolicyDiscussion";
	private const string MotiveFamily = "Family";
	private const string MotiveFollowUp = "ConversationFollowUp";
	private const string MotiveCasual = "CasualChat";
	private const int OpeningRecoverySeconds = 12;
	private const double PendingNoticeProbeSeconds = 2.0;

	private CompanionChatStorage _storage = new CompanionChatStorage();
	private readonly HashSet<string> _publishedSessionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private MapNotificationView _registeredMapNotificationView;
	private long _nextNoticeProbeUtcTicks;
	private Hero _pendingHeroCache;
	private string _pendingHeroCacheId = "";

	public static CompanionProactiveChatBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
		MBInformationManager.OnRemoveMapNotice -= OnMapNoticeRemoved;
		MBInformationManager.OnRemoveMapNotice += OnMapNoticeRemoved;
		Logger.Log("CompanionProactiveChat", "registered v1 behavior.");
	}

	public override void SyncData(IDataStore dataStore)
	{
		string json = null;
		if (dataStore.IsSaving)
		{
			NormalizeStorage();
			json = JsonConvert.SerializeObject(_storage);
			CampaignSaveChunkHelper.LogRawJsonSaveStats(StorageKey, "CompanionProactiveChat", json,
				"pending=" + (_storage.PendingSession != null)
				+ " heroCooldowns=" + _storage.HeroCooldownUntilDays.Count
				+ " interactions=" + _storage.LastInteractionDayByHero.Count);
			CampaignSaveChunkHelper.SaveChunkedString(dataStore, StorageKey, json, "CompanionProactiveChat");
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
			json = CampaignSaveChunkHelper.LoadChunkedString(dataStore, StorageKey, "CompanionProactiveChat");
			_storage = string.IsNullOrWhiteSpace(json)
				? new CompanionChatStorage()
				: JsonConvert.DeserializeObject<CompanionChatStorage>(json) ?? new CompanionChatStorage();
			NormalizeStorage();
			if (_storage.PendingSession != null && string.Equals(_storage.PendingSession.State, StateOpening, StringComparison.OrdinalIgnoreCase))
			{
				_storage.PendingSession.State = StatePending;
				_storage.PendingSession.OpeningAttemptUtcTicks = 0L;
			}
			_publishedSessionIds.Clear();
			_registeredMapNotificationView = null;
			ClearPendingHeroCache();
		}
		catch (Exception ex)
		{
			_storage = new CompanionChatStorage();
			ClearPendingHeroCache();
			Logger.Log("CompanionProactiveChat", "load failed: " + ex.Message);
		}
	}

	public static bool HasPendingNativeOpeningForCurrentConversation()
	{
		try
		{
			Hero hero = ShoutBehavior.GetNativeConversationTargetHeroForExternal();
			return Instance?.PendingOpeningMatches(hero) == true;
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
			return Instance?.TryConsumePendingOpening(hero, out extraFact, out promptText) == true;
		}
		catch
		{
			extraFact = "";
			promptText = "";
			return false;
		}
	}

	public bool IsPendingNotification(string sessionId)
	{
		CompanionChatSession pending = _storage?.PendingSession;
		return pending != null
			&& !string.IsNullOrWhiteSpace(sessionId)
			&& string.Equals(pending.Id, sessionId.Trim(), StringComparison.OrdinalIgnoreCase)
			&& (string.Equals(pending.State, StatePending, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(pending.State, StateOpening, StringComparison.OrdinalIgnoreCase))
			&& pending.ExpiresAtDays > NowDays();
	}

	public bool OpenPendingChatFromMap(string sessionId)
	{
		NormalizeStorage();
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableCompanionProactiveChat)
		{
			ConsumePendingSession("disabled_before_click");
			return false;
		}
		CompanionChatSession pending = _storage.PendingSession;
		if (pending == null
			|| !string.Equals(pending.Id, (sessionId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(pending.State, StatePending, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (pending.ExpiresAtDays <= NowDays())
		{
			ConsumePendingSession("expired_before_click");
			return false;
		}
		Hero hero = ResolvePendingHero(pending);
		if (!IsEligiblePartyHero(hero, MobileParty.MainParty, out string heroReason))
		{
			ConsumePendingSession("invalid_before_click:" + heroReason);
			return false;
		}
		if (TryGetUnsafePlayerState(out string busyReason))
		{
			InformationManager.DisplayMessage(new InformationMessage("当前不便交谈；通知会暂时保留。"));
			LogDebug("click delayed hero=" + HeroId(hero) + " reason=" + busyReason);
			return false;
		}
		pending.State = StateOpening;
		pending.OpeningAttemptUtcTicks = DateTime.UtcNow.Ticks;
		try
		{
			ConversationCharacterData player = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false);
			ConversationCharacterData partner = new ConversationCharacterData(hero.CharacterObject, null, false, false, false, false, false, false);
			if (PartyBase.MainParty?.MobileParty?.IsCurrentlyAtSea == true)
			{
				partner.Party = MobileParty.MainParty.Party;
				CampaignMission.OpenConversationMission(player, partner, "", "", false);
			}
			else
			{
				CampaignMapConversation.OpenConversation(player, partner);
			}
			LogDebug("conversation open requested session=" + pending.Id + " hero=" + HeroId(hero));
			return true;
		}
		catch (Exception ex)
		{
			pending.State = StatePending;
			pending.OpeningAttemptUtcTicks = 0L;
			InformationManager.DisplayMessage(new InformationMessage("打开交谈失败，通知会保留。"));
			Logger.Log("CompanionProactiveChat", "open conversation failed hero=" + HeroId(hero) + " error=" + ex);
			return false;
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		ValidatePendingSession();
		TryPublishPendingNotification();
	}

	private void OnHourlyTick()
	{
		try
		{
			NormalizeStorage();
			PruneRuntimeState();
			ValidatePendingSession(normalizeStorage: false);
			if (_storage.PendingSession == null)
			{
				TryScheduleChat();
			}
			TryPublishPendingNotification();
		}
		catch (Exception ex)
		{
			Logger.Log("CompanionProactiveChat", "hourly tick failed: " + ex);
		}
	}

	private void OnCampaignTick(float dt)
	{
		CompanionChatSession pending = _storage?.PendingSession;
		if (pending == null)
		{
			return;
		}
		bool opening = string.Equals(pending.State, StateOpening, StringComparison.OrdinalIgnoreCase);
		bool unpublishedPending = string.Equals(pending.State, StatePending, StringComparison.OrdinalIgnoreCase)
			&& !_publishedSessionIds.Contains(pending.Id ?? "");
		if (!opening && !unpublishedPending)
		{
			return;
		}
		long nowTicks = DateTime.UtcNow.Ticks;
		if (nowTicks < _nextNoticeProbeUtcTicks)
		{
			return;
		}
		_nextNoticeProbeUtcTicks = DateTime.UtcNow.AddSeconds(PendingNoticeProbeSeconds).Ticks;
		try
		{
			if (opening)
			{
				RecoverStuckOpening();
			}
			if (_storage?.PendingSession != null && string.Equals(_storage.PendingSession.State, StatePending, StringComparison.OrdinalIgnoreCase))
			{
				ValidatePendingSession(normalizeStorage: false);
				TryPublishPendingNotification();
			}
		}
		catch
		{
		}
	}

	private void OnConversationEnded(IEnumerable<CharacterObject> characters)
	{
		try
		{
			HashSet<string> participantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (CharacterObject character in characters ?? Enumerable.Empty<CharacterObject>())
			{
				Hero hero = character?.HeroObject;
				if (hero == null || hero == Hero.MainHero)
				{
					continue;
				}
				string heroId = HeroId(hero);
				if (string.IsNullOrWhiteSpace(heroId))
				{
					continue;
				}
				participantIds.Add(heroId);
				RecordInteraction(hero);
			}
			CompanionChatSession pending = _storage?.PendingSession;
			if (pending != null && participantIds.Contains(pending.HeroId ?? ""))
			{
				ConsumePendingSession("conversation_ended");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CompanionProactiveChat", "conversation end handling failed: " + ex.Message);
		}
	}

	private void OnMapNoticeRemoved(InformationData data)
	{
		if (!(data is CompanionProactiveChatMapNotification notice))
		{
			return;
		}
		CompanionChatSession pending = _storage?.PendingSession;
		if (pending == null || !string.Equals(pending.Id, notice.SessionId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		_publishedSessionIds.Remove(pending.Id ?? "");
		if (string.Equals(pending.State, StatePending, StringComparison.OrdinalIgnoreCase))
		{
			ConsumePendingSession("notice_dismissed");
		}
	}

	private void TryScheduleChat()
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableCompanionProactiveChat)
		{
			return;
		}
		float nowHours = NowHours();
		int scanHours = settings.CompanionProactiveChatTestMode
			? 1
			: ClampInt(settings.CompanionProactiveChatScanIntervalHours, 1, 168);
		if (nowHours - _storage.LastScanHour < scanHours)
		{
			return;
		}
		if (TryGetUnsafePlayerState(out string busyReason))
		{
			LogDebug("scan waiting unsafe=" + busyReason);
			return;
		}
		_storage.LastScanHour = nowHours;
		float nowDays = NowDays();
		if (!settings.CompanionProactiveChatTestMode && _storage.GlobalCooldownUntilDays > nowDays)
		{
			LogDebug("scan skipped global cooldown remaining=" + (_storage.GlobalCooldownUntilDays - nowDays).ToString("0.0"));
			return;
		}
		List<CompanionChatCandidate> candidates = BuildCandidates(settings, nowDays);
		CompanionChatCandidate selected = PickWeightedCandidate(candidates);
		if (selected == null)
		{
			LogDebug("scan no eligible motive");
			return;
		}
		float chance = settings.CompanionProactiveChatTestMode
			? 100f
			: ClampFloat(ClampFloat(selected.Motive.Urgency * 0.25f + Math.Max(0, selected.Affinity) * 0.05f, 5f, 30f)
				* ClampFloat(settings.CompanionProactiveChatChanceMultiplier, 0f, 5f), 0f, 100f);
		float roll = MBRandom.RandomFloat * 100f;
		LogDebug("selected hero=" + HeroId(selected.Hero)
			+ " motive=" + selected.Motive.Type
			+ " urgency=" + selected.Motive.Urgency.ToString("0.0")
			+ " affinity=" + selected.Affinity
			+ " weight=" + selected.Weight.ToString("0.0")
			+ " chance=" + chance.ToString("0.0")
			+ " roll=" + roll.ToString("0.0"));
		if (roll >= chance)
		{
			return;
		}
		StartPendingSession(selected, settings, chance, nowDays);
	}

	private List<CompanionChatCandidate> BuildCandidates(DuelSettings settings, float nowDays)
	{
		List<CompanionChatCandidate> result = new List<CompanionChatCandidate>();
		MobileParty mainParty = MobileParty.MainParty;
		int currentDay = CurrentDay();
		int quietDays = settings.CompanionProactiveChatTestMode
			? 0
			: ClampInt(settings.CompanionProactiveChatInteractionQuietDays, 0, 120);
		foreach (Hero hero in EnumerateMainPartyHeroes(mainParty))
		{
			if (!IsEligiblePartyHero(hero, mainParty, out string reason))
			{
				LogDebug("hero skipped=" + HeroId(hero) + " reason=" + reason);
				continue;
			}
			string heroId = HeroId(hero);
			if (!settings.CompanionProactiveChatTestMode
				&& _storage.HeroCooldownUntilDays.TryGetValue(heroId, out float heroCooldown)
				&& heroCooldown > nowDays)
			{
				continue;
			}
			int lastInteractionDay = GetLastInteractionDay(hero);
			if (quietDays > 0 && lastInteractionDay >= 0 && currentDay - lastInteractionDay < quietDays)
			{
				LogDebug("hero quiet=" + heroId + " remaining=" + (quietDays - (currentDay - lastInteractionDay)));
				continue;
			}
			int privateRelation = ClampInt(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			int personalTrust = ClampInt(RewardSystemBehavior.Instance?.GetNpcTrust(hero) ?? 0, -100, 100);
			int affinity = ClampInt((int)Math.Round((privateRelation + personalTrust) / 2f), -100, 100);
			foreach (CompanionChatMotive motive in BuildMotives(hero, mainParty, settings, currentDay, privateRelation, personalTrust))
			{
				if (motive == null || motive.Urgency <= 0f)
				{
					continue;
				}
				if (motive.IsEvent && IsEventKeyConsumed(motive.StableKey, nowDays))
				{
					continue;
				}
				float fatigueMultiplier = GetMotiveFatigueMultiplier(motive.Type, settings, nowDays);
				float weight = Math.Max(1f, motive.Urgency) * fatigueMultiplier;
				if (weight <= 0f)
				{
					continue;
				}
				result.Add(new CompanionChatCandidate
				{
					Hero = hero,
					Motive = motive,
					Affinity = affinity,
					PrivateRelation = privateRelation,
					PersonalTrust = personalTrust,
					Weight = weight
				});
			}
		}
		return result;
	}

	private List<CompanionChatMotive> BuildMotives(Hero hero, MobileParty mainParty, DuelSettings settings, int currentDay, int privateRelation, int personalTrust)
	{
		List<CompanionChatMotive> motives = new List<CompanionChatMotive>();
		string heroName = HeroName(hero);
		bool minor = IsMinor(hero);
		int recentWindow = ClampInt(settings.CompanionProactiveChatRecentEventWindowDays, 1, 30);

		if (MyBehavior.TryGetLatestNpcRecentActionForExternal(hero, out string heroEventKey, out string heroEventText, out int heroEventDay)
			&& IsWithinRecentWindow(heroEventDay, currentDay, recentWindow)
			&& !IsVanillaGrievanceTopic(heroEventKey + " " + heroEventText))
		{
			int age = Math.Max(0, currentDay - heroEventDay);
			motives.Add(new CompanionChatMotive
			{
				Type = MotiveHeroEvent,
				StableKey = "hero_event:" + HeroId(hero) + ":" + heroEventKey,
				IsEvent = true,
				Urgency = ClampFloat(100f - age * 4f, 60f, 100f),
				FactText = heroName + "近期有一段真实经历：" + LimitText(heroEventText, 320),
				IntentText = "你决定主动谈起这件近期真实经历；不要改写结果或虚构后续。"
			});
		}

		if (PlayerNotorietyBehavior.TryGetLatestPlayerRecentActionForExternal(hero, recentWindow, out string playerEventKey, out string playerEventText, out int playerEventDay)
			&& IsWithinRecentWindow(playerEventDay, currentDay, recentWindow)
			&& !IsVanillaGrievanceTopic(playerEventKey + " " + playerEventText))
		{
			int age = Math.Max(0, currentDay - playerEventDay);
			motives.Add(new CompanionChatMotive
			{
				Type = MotivePlayerEvent,
				StableKey = "player_event:" + playerEventKey,
				IsEvent = true,
				Urgency = ClampFloat(95f - age * 3.5f, 60f, 95f),
				FactText = "玩家近期有一段真实经历：" + LimitText(playerEventText, 320),
				IntentText = "你与玩家同队，决定主动谈谈这件近期真实行动；不要虚构未提供的细节。"
			});
		}

		if (hero.IsWounded)
		{
			motives.Add(new CompanionChatMotive
			{
				Type = MotiveCare,
				StableKey = "wounded:" + HeroId(hero),
				Urgency = 72f,
				FactText = heroName + "当前处于受伤状态。",
				IntentText = minor
					? "你决定以未成年家族成员或队内成员的身份谈谈自己的伤势和感受。"
					: "你决定主动谈谈自己的伤势、恢复或由此产生的担忧。"
			});
		}

		PartySnapshot party = BuildPartySnapshot(mainParty);
		if (!minor)
		{
			if (party.MemberCount > 0 && party.Morale <= 40f)
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotivePartyMorale,
					StableKey = "morale:" + currentDay,
					Urgency = ClampFloat(60f + (40f - party.Morale), 60f, 100f),
					FactText = "队中士气低落，气氛沉闷。",
					IntentText = "你决定主动反馈队伍士气低落，但不要谈挨饿、欠饷、逃跑或劫村抱怨。"
				});
			}
			if (party.MemberCount > 0 && party.WoundedRatio >= 0.25f)
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotivePartyWounded,
					StableKey = "party_wounded:" + currentDay,
					Urgency = ClampFloat(60f + (party.WoundedRatio - 0.25f) * 53.34f, 60f, 100f),
					FactText = "队中伤兵不少，众人都在担心恢复。",
					IntentText = "你决定主动谈谈伤兵、恢复或队伍健康压力。"
				});
			}
			if (party.PrisonerLimit > 0 && party.PrisonerRatio >= 0.75f)
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotivePrisoners,
					StableKey = "prisoners:" + currentDay,
					Urgency = ClampFloat(60f + (party.PrisonerRatio - 0.75f) * 80f, 60f, 100f),
					FactText = "队伍带着太多俘虏，照看起来很费心。",
					IntentText = "你决定主动反馈俘虏管理、赎买或转运压力；不要假定任何交易已经成立。"
				});
			}
			if (party.InventoryCapacity > 0 && party.BurdenRatio >= 0.9f)
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotiveBurden,
					StableKey = "burden:" + currentDay,
					Urgency = ClampFloat(60f + (party.BurdenRatio - 0.9f) * 100f, 60f, 100f),
					FactText = "队伍辎重过多，行军颇受拖累。",
					IntentText = "你决定主动反馈负重、运输或物资整理压力。"
				});
			}

			string role = GetPartyRoleLabel(hero, mainParty);
			if (!string.IsNullOrWhiteSpace(role))
			{
				float roleUrgency = 35f;
				string roleFact = heroName + "当前担任玩家主队的" + role + "。";
				if (string.Equals(role, "医师", StringComparison.Ordinal) && party.WoundedRatio >= 0.15f)
				{
					roleUrgency = ClampFloat(45f + party.WoundedRatio * 35f, 45f, 70f);
					roleFact += " 队中伤兵不少，正需要留心照料。";
				}
				else if (string.Equals(role, "军需官", StringComparison.Ordinal) && party.BurdenRatio >= 0.75f)
				{
					roleUrgency = ClampFloat(45f + party.BurdenRatio * 25f, 45f, 70f);
					roleFact += " 队伍辎重过多，行军颇受拖累。";
				}
				motives.Add(new CompanionChatMotive
				{
					Type = MotiveDuty,
					StableKey = "duty:" + HeroId(hero) + ":" + currentDay,
					Urgency = roleUrgency,
					FactText = roleFact,
					IntentText = "你决定以" + role + "身份做一次职责反馈；未提供具体发现时不得虚构敌情、伤病、工程或物资事实。"
				});
			}
		}

		if (minor)
		{
			if (hero.Clan != null && hero.Clan == Clan.PlayerClan)
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotiveFamily,
					StableKey = "family:" + HeroId(hero) + ":" + currentDay,
					Urgency = ClampFloat(40f + Math.Max(0, personalTrust) * 0.2f, 40f, 60f),
					FactText = heroName + "尚未成年，也是玩家的家人，对玩家十分亲近。",
					IntentText = "你决定以未成年家族成员的身份主动谈谈家庭、关心或自己的近况；禁止恋爱化和成人职责表达。"
				});
			}
			else if (personalTrust >= 30)
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotiveCare,
					StableKey = "minor_care:" + HeroId(hero) + ":" + currentDay,
					Urgency = ClampFloat(40f + personalTrust * 0.25f, 40f, 65f),
					FactText = heroName + "尚未成年，与玩家同行已久，也愿意说些心里话。",
					IntentText = "你决定以未成年队内成员的身份表达信任或关心；禁止恋爱化和成人职责表达。"
				});
			}
		}
		else
		{
			if (ProactiveNpcRequestBehavior.IsRomanticInteractionEligibleForExternal(hero)
				&& !ProactiveNpcRequestBehavior.IsRomanticInteractionUnavailableForExternal())
			{
				motives.Add(new CompanionChatMotive
				{
					Type = MotiveRomanticInteraction,
					StableKey = "romantic_interaction:" + HeroId(hero),
					Urgency = ClampFloat(60f + Math.Max(0, privateRelation - 30) * 0.4f, 60f, 88f),
					FactText = heroName + "与玩家相识已久，心中有些牵挂。",
					IntentText = AIConfigHandler.GetProactiveNpcRequestCompanionIntent(MotiveRomanticInteraction)
				});
			}
			if (privateRelation >= 35 || personalTrust >= 35 || privateRelation <= -25 || personalTrust <= -25)
			{
				bool strained = privateRelation <= -25 || personalTrust <= -25;
				float emotionUrgency = strained
					? ClampFloat(55f + Math.Max(-privateRelation, -personalTrust) * 0.35f, 55f, 90f)
					: ClampFloat(40f + Math.Max(privateRelation, personalTrust) * 0.3f, 40f, 70f);
				motives.Add(new CompanionChatMotive
				{
					Type = MotiveEmotion,
					StableKey = "emotion:" + HeroId(hero) + ":" + currentDay,
					Urgency = emotionUrgency,
					FactText = strained
						? heroName + "与玩家之间仍有未解开的芥蒂。"
						: heroName + "与玩家颇为亲近，也信得过对方。",
					IntentText = strained
						? "你决定主动表达与当前低关系或低信任相符的不满、疑虑或冲突；不要虚构具体过错。"
						: "你决定主动表达关心或感受；克制，不夸大感情，也不虚构承诺。"
				});
			}
		}

		if (ProactiveNpcRequestBehavior.TryBuildPolicyDiscussionCompanionMotiveForExternal(hero, out string policyFact, out string policyIntent, out float policyUrgency))
		{
			motives.Add(new CompanionChatMotive
			{
				Type = MotivePolicyDiscussion,
				StableKey = "policy_discussion:" + HeroId(hero),
				Urgency = policyUrgency,
				FactText = policyFact,
				IntentText = policyIntent
			});
		}

		if (MyBehavior.TryGetLatestCompressedMemoryForExternal(hero, out string memoryKey, out string memoryDate, out string memoryText, out int memoryDay)
			&& memoryDay >= 0
			&& currentDay - memoryDay <= 120)
		{
			int age = Math.Max(0, currentDay - memoryDay);
			string memoryDateSuffix = string.IsNullOrWhiteSpace(memoryDate) ? "" : ("（" + memoryDate + "）");
			motives.Add(new CompanionChatMotive
			{
				Type = MotiveFollowUp,
				StableKey = "followup:" + memoryKey,
				IsEvent = true,
				Urgency = ClampFloat(55f - age * 0.25f, 35f, 55f),
				FactText = "你想起此前与玩家的一段互动" + memoryDateSuffix + "：" + LimitText(memoryText, 320),
				IntentText = "你决定从这段互动引出的感受、想法或关切开口；不要把它当作未结束的对话续写，也不要虚构互动外的事实。"
			});
		}

		motives.Add(new CompanionChatMotive
		{
			Type = MotiveCasual,
			StableKey = "casual:" + HeroId(hero) + ":" + currentDay,
			Urgency = 20f,
			FactText = heroName + "正与玩家同行，只想聊几句日常。",
			IntentText = minor
				? "你决定以未成年队内成员的身份主动闲聊；只谈日常、家庭或可确认的当下，不得恋爱化。"
				: "你决定主动进行一次普通闲聊；只谈日常或可确认的当下，不要虚构事件、困难或请求。"
		});
		return motives;
	}

	private void StartPendingSession(CompanionChatCandidate selected, DuelSettings settings, float chance, float nowDays)
	{
		Hero hero = selected.Hero;
		CompanionChatMotive motive = selected.Motive;
		string heroName = HeroName(hero);
		string fact = "[AFEF NPC行为补充] " + heroName + "正与玩家同行，此次由" + heroName + "主动开口。"
			+ motive.FactText;
		if (IsMinor(hero))
		{
			fact += " " + heroName + "尚未成年；谈话只限家庭、关心、近况和日常。";
		}
		string prompt = motive.IntentText + "先开口，只谈这件事，只输出台词。";
		int noticeDays = ClampInt(settings.CompanionProactiveChatNoticeLifetimeDays, 1, 30);
		_storage.PendingSession = new CompanionChatSession
		{
			Id = Guid.NewGuid().ToString("N"),
			HeroId = HeroId(hero),
			HeroName = heroName,
			MotiveType = motive.Type,
			MotiveStableKey = motive.StableKey ?? "",
			IsEventMotive = motive.IsEvent,
			ExtraFact = fact,
			PromptText = prompt,
			Urgency = motive.Urgency,
			Affinity = selected.Affinity,
			PrivateRelation = selected.PrivateRelation,
			PersonalTrust = selected.PersonalTrust,
			ChancePercent = chance,
			CreatedAtDays = nowDays,
			ExpiresAtDays = nowDays + noticeDays,
			State = StatePending
		};
		CachePendingHero(hero);
		int globalDays = settings.CompanionProactiveChatTestMode ? 0 : ClampInt(settings.CompanionProactiveChatGlobalCooldownDays, 0, 120);
		int heroDays = settings.CompanionProactiveChatTestMode ? 0 : ClampInt(settings.CompanionProactiveChatHeroCooldownDays, 0, 240);
		_storage.GlobalCooldownUntilDays = nowDays + globalDays;
		_storage.HeroCooldownUntilDays[HeroId(hero)] = nowDays + heroDays;
		if (string.Equals(motive.Type, MotiveRomanticInteraction, StringComparison.OrdinalIgnoreCase))
		{
			ProactiveNpcRequestBehavior.RecordRomanticInteractionForExternal("companion_notification_created");
		}
		else if (string.Equals(motive.Type, MotivePolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			ProactiveNpcRequestBehavior.RecordPolicyDiscussionForExternal("companion_notification_created");
		}
		int fatigueDays = settings.CompanionProactiveChatTestMode ? 0 : ClampInt(settings.CompanionProactiveChatMotiveFatigueDays, 0, 120);
		if (fatigueDays > 0)
		{
			_storage.MotiveFatigueUntilDays[motive.Type] = nowDays + fatigueDays;
		}
		else
		{
			_storage.MotiveFatigueUntilDays.Remove(motive.Type);
		}
		Logger.Log("CompanionProactiveChat", "pending created session=" + _storage.PendingSession.Id
			+ " hero=" + HeroId(hero)
			+ " motive=" + motive.Type
			+ " urgency=" + motive.Urgency.ToString("0.0")
			+ " affinity=" + selected.Affinity
			+ " chance=" + chance.ToString("0.0")
			+ " globalCooldownDays=" + globalDays
			+ " heroCooldownDays=" + heroDays);
		TryPublishPendingNotification();
	}

	private bool PendingOpeningMatches(Hero hero)
	{
		CompanionChatSession pending = _storage?.PendingSession;
		return pending != null
			&& string.Equals(pending.State, StateOpening, StringComparison.OrdinalIgnoreCase)
			&& HeroMatchesId(hero, pending.HeroId);
	}

	private bool TryConsumePendingOpening(Hero hero, out string extraFact, out string promptText)
	{
		extraFact = "";
		promptText = "";
		CompanionChatSession pending = _storage?.PendingSession;
		if (pending == null
			|| !string.Equals(pending.State, StateOpening, StringComparison.OrdinalIgnoreCase)
			|| !HeroMatchesId(hero, pending.HeroId))
		{
			return false;
		}
		extraFact = (pending.ExtraFact ?? "").Trim();
		promptText = (pending.PromptText ?? "").Trim();
		RecordInteraction(hero);
		ConsumePendingSession("opening_consumed");
		return !string.IsNullOrWhiteSpace(extraFact) || !string.IsNullOrWhiteSpace(promptText);
	}

	private void ConsumePendingSession(string reason)
	{
		CompanionChatSession pending = _storage?.PendingSession;
		if (pending == null)
		{
			return;
		}
		if (pending.IsEventMotive && !string.IsNullOrWhiteSpace(pending.MotiveStableKey))
		{
			int keepDays = Math.Max(30, ClampInt(DuelSettings.GetSettings()?.CompanionProactiveChatRecentEventWindowDays ?? 10, 1, 30) + 3);
			_storage.ConsumedEventKeyUntilDays[pending.MotiveStableKey.Trim()] = NowDays() + keepDays;
		}
		_publishedSessionIds.Remove(pending.Id ?? "");
		Logger.Log("CompanionProactiveChat", "pending consumed session=" + (pending.Id ?? "")
			+ " hero=" + (pending.HeroId ?? "")
			+ " motive=" + (pending.MotiveType ?? "")
			+ " reason=" + (reason ?? ""));
		_storage.PendingSession = null;
		ClearPendingHeroCache();
	}

	private void ValidatePendingSession(bool normalizeStorage = true)
	{
		if (normalizeStorage)
		{
			NormalizeStorage();
		}
		CompanionChatSession pending = _storage.PendingSession;
		if (pending == null)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableCompanionProactiveChat)
		{
			ConsumePendingSession("disabled");
			return;
		}
		if (pending.ExpiresAtDays <= NowDays())
		{
			ConsumePendingSession("expired");
			return;
		}
		Hero hero = ResolvePendingHero(pending);
		if (!IsEligiblePartyHero(hero, MobileParty.MainParty, out string reason))
		{
			ConsumePendingSession("hero_invalid:" + reason);
		}
	}

	private void RecoverStuckOpening()
	{
		CompanionChatSession pending = _storage?.PendingSession;
		if (pending == null
			|| !string.Equals(pending.State, StateOpening, StringComparison.OrdinalIgnoreCase)
			|| pending.OpeningAttemptUtcTicks <= 0L)
		{
			return;
		}
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true || Mission.Current != null)
		{
			return;
		}
		if (DateTime.UtcNow.Ticks - pending.OpeningAttemptUtcTicks < TimeSpan.FromSeconds(OpeningRecoverySeconds).Ticks)
		{
			return;
		}
		pending.State = StatePending;
		pending.OpeningAttemptUtcTicks = 0L;
		_publishedSessionIds.Remove(pending.Id ?? "");
		Logger.Log("CompanionProactiveChat", "opening timed out; notification will be republished session=" + (pending.Id ?? ""));
	}

	private void TryPublishPendingNotification()
	{
		CompanionChatSession pending = _storage?.PendingSession;
		if (pending == null || !string.Equals(pending.State, StatePending, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (_publishedSessionIds.Contains(pending.Id ?? ""))
		{
			return;
		}
		if (!CanPublishMapNotification() || !TryEnsureMapNotificationRegistered())
		{
			return;
		}
		Hero hero = ResolvePendingHero(pending);
		if (hero == null)
		{
			return;
		}
		string heroName = HeroName(hero);
		try
		{
			_publishedSessionIds.Add(pending.Id);
			MBInformationManager.AddNotice(new CompanionProactiveChatMapNotification(
				pending.Id,
				heroName + "想与你交谈",
				heroName + "就在你的队伍中，想在你方便时与你谈谈。"));
			LogDebug("notification published session=" + pending.Id + " hero=" + pending.HeroId);
		}
		catch (Exception ex)
		{
			_publishedSessionIds.Remove(pending.Id ?? "");
			Logger.Log("CompanionProactiveChat", "notification publish failed session=" + (pending.Id ?? "") + " error=" + ex.Message);
		}
	}

	private bool TryEnsureMapNotificationRegistered()
	{
		try
		{
			MapNotificationView view = MapScreen.Instance?.MapNotificationView;
			if (view == null)
			{
				return false;
			}
			if (!ReferenceEquals(_registeredMapNotificationView, view))
			{
				_publishedSessionIds.Clear();
				view.RegisterMapNotificationType(typeof(CompanionProactiveChatMapNotification), typeof(CompanionProactiveChatMapNotificationItemVM));
				_registeredMapNotificationView = view;
			}
			return true;
		}
		catch (Exception ex)
		{
			LogDebug("notification registration failed=" + ex.Message);
			return false;
		}
	}

	private static bool CanPublishMapNotification()
	{
		try
		{
			return Mission.Current == null
				&& Game.Current?.GameStateManager?.ActiveState is MapState
				&& MapScreen.Instance?.MapNotificationView != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetUnsafePlayerState(out string reason)
	{
		reason = "";
		try
		{
			if (!(Game.Current?.GameStateManager?.ActiveState is MapState))
			{
				reason = "not_map_state";
				return true;
			}
			if (Hero.MainHero == null || Hero.MainHero.IsPrisoner)
			{
				reason = "player_invalid_or_prisoner";
				return true;
			}
			return ProactiveNpcRequestBehavior.TryGetPlayerInteractionBusyReasonForExternal(allowSea: true, allowSettlement: true, out reason);
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return true;
		}
	}

	private static IEnumerable<Hero> EnumerateMainPartyHeroes(MobileParty mainParty)
	{
		if (mainParty?.MemberRoster == null)
		{
			return Enumerable.Empty<Hero>();
		}
		List<Hero> heroes = new List<Hero>();
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (TroopRosterElement element in mainParty.MemberRoster.GetTroopRoster())
		{
			Hero hero = element.Character?.HeroObject;
			string id = HeroId(hero);
			if (hero == null || hero == Hero.MainHero || string.IsNullOrWhiteSpace(id) || !ids.Add(id))
			{
				continue;
			}
			heroes.Add(hero);
		}
		return heroes;
	}

	private static bool IsEligiblePartyHero(Hero hero, MobileParty mainParty, out string reason)
	{
		reason = "";
		if (hero == null || hero == Hero.MainHero || hero.CharacterObject?.IsHero != true)
		{
			reason = "invalid_hero";
			return false;
		}
		if (hero.IsDead || !hero.IsAlive)
		{
			reason = "dead";
			return false;
		}
		if (hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
		{
			reason = "prisoner";
			return false;
		}
		if (mainParty?.MemberRoster == null || mainParty.MemberRoster.GetTroopCount(hero.CharacterObject) <= 0)
		{
			reason = "not_in_main_party";
			return false;
		}
		return true;
	}

	private static PartySnapshot BuildPartySnapshot(MobileParty party)
	{
		PartySnapshot snapshot = new PartySnapshot();
		try
		{
			snapshot.MemberCount = Math.Max(0, party?.Party?.NumberOfAllMembers ?? 0);
			snapshot.Morale = ClampFloat(party?.Morale ?? 100f, 0f, 100f);
			foreach (TroopRosterElement element in party?.MemberRoster?.GetTroopRoster() ?? Enumerable.Empty<TroopRosterElement>())
			{
				snapshot.WoundedCount += Math.Max(0, element.WoundedNumber);
			}
			snapshot.WoundedRatio = snapshot.MemberCount <= 0 ? 0f : ClampFloat(snapshot.WoundedCount / (float)snapshot.MemberCount, 0f, 1f);
			snapshot.PrisonerCount = Math.Max(0, party?.PrisonRoster?.TotalManCount ?? party?.Party?.PrisonRoster?.TotalManCount ?? 0);
			snapshot.PrisonerLimit = Math.Max(0, party?.Party?.PrisonerSizeLimit ?? 0);
			snapshot.PrisonerRatio = snapshot.PrisonerLimit <= 0 ? 0f : Math.Max(0f, snapshot.PrisonerCount / (float)snapshot.PrisonerLimit);
			snapshot.InventoryCapacity = Math.Max(0, party?.InventoryCapacity ?? 0);
			snapshot.TotalWeight = Math.Max(0f, party?.TotalWeightCarried ?? 0f);
			snapshot.BurdenRatio = snapshot.InventoryCapacity <= 0 ? 0f : Math.Max(0f, snapshot.TotalWeight / snapshot.InventoryCapacity);
		}
		catch
		{
		}
		return snapshot;
	}

	private static string GetPartyRoleLabel(Hero hero, MobileParty party)
	{
		try
		{
			if (hero == null || party == null)
			{
				return "";
			}
			if (party.GetRoleHolder(PartyRole.Scout) == hero) return "斥候";
			if (party.GetRoleHolder(PartyRole.Engineer) == hero) return "工程师";
			if (party.GetRoleHolder(PartyRole.Surgeon) == hero) return "医师";
			if (party.GetRoleHolder(PartyRole.Quartermaster) == hero) return "军需官";
		}
		catch
		{
		}
		return "";
	}

	private static bool IsMinor(Hero hero)
	{
		try
		{
			return hero != null && (hero.IsChild || hero.Age < 18f);
		}
		catch
		{
			return true;
		}
	}

	private int GetLastInteractionDay(Hero hero)
	{
		string id = HeroId(hero);
		int stored = _storage.LastInteractionDayByHero.TryGetValue(id, out int day) ? day : -1;
		int historyDay = MyBehavior.GetLastMeaningfulDialogueDayForExternal(hero);
		return Math.Max(stored, historyDay);
	}

	private void RecordInteraction(Hero hero)
	{
		string id = HeroId(hero);
		if (!string.IsNullOrWhiteSpace(id))
		{
			_storage.LastInteractionDayByHero[id] = CurrentDay();
		}
	}

	private float GetMotiveFatigueMultiplier(string motiveType, DuelSettings settings, float nowDays)
	{
		if (settings.CompanionProactiveChatTestMode)
		{
			return 1f;
		}
		return _storage.MotiveFatigueUntilDays.TryGetValue(motiveType ?? "", out float untilDays) && untilDays > nowDays
			? ClampFloat(settings.CompanionProactiveChatMotiveFatigueMultiplier, 0f, 1f)
			: 1f;
	}

	private bool IsEventKeyConsumed(string key, float nowDays)
	{
		return !string.IsNullOrWhiteSpace(key)
			&& _storage.ConsumedEventKeyUntilDays.TryGetValue(key.Trim(), out float untilDays)
			&& untilDays > nowDays;
	}

	private static CompanionChatCandidate PickWeightedCandidate(List<CompanionChatCandidate> candidates)
	{
		List<CompanionChatCandidate> valid = (candidates ?? new List<CompanionChatCandidate>())
			.Where(x => x?.Hero != null && x.Motive != null && x.Weight > 0f)
			.ToList();
		float total = valid.Sum(x => x.Weight);
		if (valid.Count == 0 || total <= 0f)
		{
			return null;
		}
		float roll = MBRandom.RandomFloat * total;
		foreach (CompanionChatCandidate candidate in valid)
		{
			roll -= candidate.Weight;
			if (roll <= 0f)
			{
				return candidate;
			}
		}
		return valid[valid.Count - 1];
	}

	private void PruneRuntimeState()
	{
		float nowDays = NowDays();
		PruneExpired(_storage.HeroCooldownUntilDays, nowDays);
		PruneExpired(_storage.MotiveFatigueUntilDays, nowDays);
		PruneExpired(_storage.ConsumedEventKeyUntilDays, nowDays);
		if (_storage.LastInteractionDayByHero.Count > 512)
		{
			int minDay = CurrentDay() - 720;
			foreach (string key in _storage.LastInteractionDayByHero.Where(x => x.Value < minDay).Select(x => x.Key).ToList())
			{
				_storage.LastInteractionDayByHero.Remove(key);
			}
		}
	}

	private static void PruneExpired(Dictionary<string, float> values, float nowDays)
	{
		if (values == null)
		{
			return;
		}
		foreach (string key in values.Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Value <= nowDays).Select(x => x.Key).ToList())
		{
			values.Remove(key);
		}
	}

	private void NormalizeStorage()
	{
		_storage ??= new CompanionChatStorage();
		_storage.HeroCooldownUntilDays = NormalizeFloatDictionary(_storage.HeroCooldownUntilDays);
		_storage.MotiveFatigueUntilDays = NormalizeFloatDictionary(_storage.MotiveFatigueUntilDays);
		_storage.ConsumedEventKeyUntilDays = NormalizeFloatDictionary(_storage.ConsumedEventKeyUntilDays);
		_storage.LastInteractionDayByHero = NormalizeIntDictionary(_storage.LastInteractionDayByHero);
		if (_storage.PendingSession != null)
		{
			CompanionChatSession pending = _storage.PendingSession;
			pending.Id = (pending.Id ?? "").Trim();
			pending.HeroId = (pending.HeroId ?? "").Trim();
			pending.HeroName = (pending.HeroName ?? "").Trim();
			pending.MotiveType = (pending.MotiveType ?? "").Trim();
			pending.MotiveStableKey = (pending.MotiveStableKey ?? "").Trim();
			pending.ExtraFact = pending.ExtraFact ?? "";
			pending.PromptText = pending.PromptText ?? "";
			pending.State = string.Equals(pending.State, StateOpening, StringComparison.OrdinalIgnoreCase) ? StateOpening : StatePending;
			if (string.IsNullOrWhiteSpace(pending.Id) || string.IsNullOrWhiteSpace(pending.HeroId))
			{
				_storage.PendingSession = null;
			}
		}
	}

	private static Dictionary<string, float> NormalizeFloatDictionary(Dictionary<string, float> source)
	{
		Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, float> pair in source ?? new Dictionary<string, float>())
		{
			string key = (pair.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key)) result[key] = pair.Value;
		}
		return result;
	}

	private static Dictionary<string, int> NormalizeIntDictionary(Dictionary<string, int> source)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, int> pair in source ?? new Dictionary<string, int>())
		{
			string key = (pair.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key)) result[key] = pair.Value;
		}
		return result;
	}

	private static bool IsWithinRecentWindow(int eventDay, int currentDay, int windowDays)
	{
		return eventDay >= 0 && eventDay <= currentDay && currentDay - eventDay < Math.Max(1, windowDays);
	}

	private static bool IsVanillaGrievanceTopic(string text)
	{
		string value = (text ?? "").ToLowerInvariant();
		string[] terms =
		{
			"挨饿", "饥饿", "缺粮", "断粮", "starv", "food shortage",
			"欠饷", "军饷", "未付工资", "unpaid wage", "wage",
			"逃跑", "逃离战场", "撤退", "临阵脱逃", "deserted battle", "retreat",
			"劫村", "烧村", "洗劫村庄", "劫掠村庄", "village raid", "village looted"
		};
		return terms.Any(term => value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static string LimitText(string text, int maxChars)
	{
		string value = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (value.Contains("  ")) value = value.Replace("  ", " ");
		return value.Length <= maxChars ? value : value.Substring(0, Math.Max(1, maxChars)).TrimEnd() + "…";
	}

	private Hero ResolvePendingHero(CompanionChatSession pending)
	{
		string heroId = (pending?.HeroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return null;
		}
		if (_pendingHeroCache != null
			&& string.Equals(_pendingHeroCacheId, heroId, StringComparison.OrdinalIgnoreCase)
			&& HeroMatchesId(_pendingHeroCache, heroId))
		{
			return _pendingHeroCache;
		}
		Hero hero = ResolveHero(heroId);
		CachePendingHero(hero);
		return hero;
	}

	private void CachePendingHero(Hero hero)
	{
		_pendingHeroCache = hero;
		_pendingHeroCacheId = HeroId(hero);
	}

	private void ClearPendingHeroCache()
	{
		_pendingHeroCache = null;
		_pendingHeroCacheId = "";
	}

	private static Hero ResolveHero(string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)) return null;
		try
		{
			return Hero.Find(id) ?? Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static bool HeroMatchesId(Hero hero, string heroId)
	{
		return hero != null && string.Equals(HeroId(hero), (heroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static string HeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static string HeroName(Hero hero)
	{
		string name = (hero?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(name) ? "这名队内成员" : name;
	}

	private void LogDebug(string message)
	{
		if (DuelSettings.GetSettings()?.CompanionProactiveChatDebugLog == true)
		{
			Logger.Log("CompanionProactiveChat", message ?? "");
		}
	}

	private static int ClampInt(int value, int min, int max)
	{
		return value < min ? min : (value > max ? max : value);
	}

	private static float ClampFloat(float value, float min, float max)
	{
		return value < min ? min : (value > max ? max : value);
	}

	private static float NowHours()
	{
		try { return (float)CampaignTime.Now.ToHours; }
		catch { return 0f; }
	}

	private static float NowDays()
	{
		try { return (float)CampaignTime.Now.ToDays; }
		catch { return 0f; }
	}

	private static int CurrentDay()
	{
		return Math.Max(0, (int)Math.Floor(NowDays()));
	}

	private sealed class CompanionChatStorage
	{
		public CompanionChatSession PendingSession { get; set; }
		public Dictionary<string, float> HeroCooldownUntilDays { get; set; } = new Dictionary<string, float>();
		public Dictionary<string, float> MotiveFatigueUntilDays { get; set; } = new Dictionary<string, float>();
		public Dictionary<string, float> ConsumedEventKeyUntilDays { get; set; } = new Dictionary<string, float>();
		public Dictionary<string, int> LastInteractionDayByHero { get; set; } = new Dictionary<string, int>();
		public float GlobalCooldownUntilDays { get; set; }
		public float LastScanHour { get; set; } = -99999f;
	}

	private sealed class CompanionChatSession
	{
		public string Id { get; set; } = "";
		public string HeroId { get; set; } = "";
		public string HeroName { get; set; } = "";
		public string MotiveType { get; set; } = "";
		public string MotiveStableKey { get; set; } = "";
		public bool IsEventMotive { get; set; }
		public string ExtraFact { get; set; } = "";
		public string PromptText { get; set; } = "";
		public float Urgency { get; set; }
		public int Affinity { get; set; }
		public int PrivateRelation { get; set; }
		public int PersonalTrust { get; set; }
		public float ChancePercent { get; set; }
		public float CreatedAtDays { get; set; }
		public float ExpiresAtDays { get; set; }
		public string State { get; set; } = StatePending;
		public long OpeningAttemptUtcTicks { get; set; }
	}

	private sealed class CompanionChatCandidate
	{
		public Hero Hero;
		public CompanionChatMotive Motive;
		public int Affinity;
		public int PrivateRelation;
		public int PersonalTrust;
		public float Weight;
	}

	private sealed class CompanionChatMotive
	{
		public string Type = "";
		public string StableKey = "";
		public bool IsEvent;
		public float Urgency;
		public string FactText = "";
		public string IntentText = "";
	}

	private sealed class PartySnapshot
	{
		public int MemberCount;
		public int WoundedCount;
		public float WoundedRatio;
		public float Morale = 100f;
		public int PrisonerCount;
		public int PrisonerLimit;
		public float PrisonerRatio;
		public int InventoryCapacity;
		public float TotalWeight;
		public float BurdenRatio;
	}
}
