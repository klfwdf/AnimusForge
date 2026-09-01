using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class PlayerNotorietyBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_af_player_notoriety_state_v1";
	private const int RecentActionWindowDays = 10;
	private const int MaxRecentActions = 96;
	private const int MaxMajorMaterials = 180;
	private const int SummaryBatchSize = 24;
	private const int MaxSummarizedMaterialKeys = 512;
	private const int MaxSummaryRetries = 3;
	private const int PersonalKnownBonusPerLine = 3;
	private const int CourierReplyKnownBonus = 1;
	private const int TrustPrivateLeakThreshold = -20;
	private const string PlayerHeroId = "__player__";

	private static readonly string[] NotorietyLevelTexts = new string[11]
	{
		"默默无闻",
		"鲜为人知",
		"略有耳闻",
		"渐为人知",
		"口耳相传",
		"广为人知",
		"远近皆知",
		"街知巷闻",
		"妇孺皆知",
		"家喻户晓",
		"人尽皆知"
	};

	private static readonly string[] CultureDisplayOrder = new string[6]
	{
		"empire",
		"vlandia",
		"sturgia",
		"aserai",
		"khuzait",
		"battania"
	};

	private PlayerNotorietyState _state = new PlayerNotorietyState();
	private readonly Dictionary<string, ActiveConversationState> _activeConversationStates = new Dictionary<string, ActiveConversationState>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _soldPrisonerDonationSkipKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private string _currentSettlementStayId = "";
	private string _currentSettlementStayName = "";
	private double _currentSettlementStayStartDays = -1.0;
	private int _currentSettlementStayStartDay = -1;
	private bool _summaryProcessing;

	public static PlayerNotorietyBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnNativeConversationEnded);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.OnPrisonerReleasedEvent.AddNonSerializedListener(this, OnPlayerPrisonersReleased);
		CampaignEvents.OnPrisonerSoldEvent.AddNonSerializedListener(this, OnPlayerPrisonersSold);
		CampaignEvents.OnMainPartyPrisonerRecruitedEvent.AddNonSerializedListener(this, OnMainPartyPrisonerRecruited);
		CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPlayerPrisonersDonatedToSettlement);
		CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnPlayerSettlementEnteredForRecentAction);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnPlayerSettlementLeftForRecentAction);
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnPlayerTroopRecruitedForRecentAction);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinishedForPlayerRecentAction);
		CampaignEvents.OnPlayerBoardGameOverEvent.AddNonSerializedListener(this, OnPlayerBoardGameOverForRecentAction);
		CampaignEvents.OnRansomOfferCancelledEvent.AddNonSerializedListener(this, OnRansomOfferCancelledForPlayerRecentAction);
		CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, OnMarriageOfferCanceledForPlayerRecentAction);
		CampaignEvents.OnVassalOrMercenaryServiceOfferCanceledEvent.AddNonSerializedListener(this, OnVassalOrMercenaryOfferCanceledForPlayerRecentAction);
		CampaignEvents.OnPartyLeaderChangeOfferCanceledEvent.AddNonSerializedListener(this, OnPartyLeaderChangeOfferCanceledForPlayerRecentAction);
		CampaignEvents.OnPeaceOfferResolvedEvent.AddNonSerializedListener(this, OnPeaceOfferResolvedForPlayerRecentAction);
		CampaignEvents.AlleyClearedByPlayer.AddNonSerializedListener(this, OnAlleyClearedByPlayerForRecentAction);
		Logger.Log("PlayerNotoriety", "registered v1 behavior.");
	}

	public override void SyncData(IDataStore dataStore)
	{
		string storageJson = null;
		if (dataStore.IsSaving)
		{
			_state = NormalizeState(_state);
			PrepareNotorietyConversationOutcomeStorageForSave();
			storageJson = JsonConvert.SerializeObject(_state);
			CampaignSaveChunkHelper.LogRawJsonSaveStats(StorageKey, "PlayerNotoriety", storageJson, BuildStorageDiagnostics());
			CampaignSaveChunkHelper.SaveChunkedString(dataStore, StorageKey, storageJson, "PlayerNotoriety");
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
			storageJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, StorageKey, "PlayerNotoriety");
			_state = string.IsNullOrWhiteSpace(storageJson) ? new PlayerNotorietyState() : JsonConvert.DeserializeObject<PlayerNotorietyState>(storageJson) ?? new PlayerNotorietyState();
			_state = NormalizeState(_state);
			_activeConversationStates.Clear();
			ActivateNotorietyConversationOutcomeStorageAfterLoad();
		}
		catch (Exception ex)
		{
			_state = new PlayerNotorietyState();
			_activeConversationStates.Clear();
			ResetNotorietyConversationOutcomeStorageAfterFailedLoad();
			Logger.Log("PlayerNotoriety", "load failed: " + ex.Message);
		}
	}

	private string BuildStorageDiagnostics()
	{
		try
		{
			PlayerNotorietyState state = NormalizeState(_state);
			int recent = state.RecentActions?.Count ?? 0;
			int materials = state.MajorMaterials?.Count ?? 0;
			int pending = state.MajorMaterials?.Count(x => x != null && !x.Summarized) ?? 0;
			int summaryBytes = CampaignSaveChunkHelper.GetUtf8ByteCountForDiagnostics(state.MajorSummary ?? "");
			int maxMaterialBytes = state.MajorMaterials?.Select(x => CampaignSaveChunkHelper.GetUtf8ByteCountForDiagnostics(x?.Text ?? "")).DefaultIfEmpty(0).Max() ?? 0;
			return "recentActions=" + recent
				+ " majorMaterials=" + materials
				+ " pendingMaterials=" + pending
				+ " summarizedMaterialKeys=" + (state.SummarizedMaterialKeys?.Count ?? 0)
				+ " npcKnowledge=" + (state.NpcKnowledge?.Count ?? 0)
				+ " conversationOutcomeReceipts=" + (state.ConversationOutcomeReceipts?.Count ?? 0)
				+ " cultures=" + (state.CultureNotoriety?.Count ?? 0)
				+ " summaryBytes=" + summaryBytes
				+ " maxMaterialBytes=" + maxMaterialBytes;
		}
		catch
		{
			return "";
		}
	}

	public static void RecordPlayerActionForExternal(string text, string stableKey, string actionKind, bool isMajor, int day, string gameDate, int sequence, string settlementId, string settlementName, string locationText, string actorCultureId, string targetCultureId, string settlementCultureId, bool? won)
	{
		try
		{
			Instance?.RecordPlayerAction(text, stableKey, actionKind, isMajor, day, gameDate, sequence, settlementId, settlementName, locationText, actorCultureId, targetCultureId, settlementCultureId, won);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "record action failed: " + ex.Message);
		}
	}

	public static void RecordPlayerHistoryMaterialForExternal(string text, string stableKey, string sourceKind, int day, string gameDate, string actorCultureId, string targetCultureId, string settlementCultureId)
	{
		try
		{
			Instance?.RecordPlayerHistoryMaterial(text, stableKey, sourceKind, day, gameDate, actorCultureId, targetCultureId, settlementCultureId);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "record history material failed: " + ex.Message);
		}
	}

	public static string BuildPlayerHistoryNameForExternal()
	{
		return BuildPlayerHistoryDisplayName();
	}

	public static bool IsLowProfileModeEnabledForExternal()
	{
		try
		{
			return Instance?._state?.LowProfileModeEnabled == true;
		}
		catch
		{
			return false;
		}
	}

	public static string RenderPlayerHistoryMaterialForExternal(string text)
	{
		return RenderPlayerHistoryTextForPrompt(text, BuildPlayerHistoryDisplayName());
	}

	public static string RenderPlayerNamedReferenceForExternal(string text)
	{
		return RenderPlayerNamedReference(text, BuildPlayerHistoryDisplayName());
	}

	public static void RecordPlayerPrisonBreakRescueForExternal(Hero rescuedHero)
	{
		try
		{
			Instance?.RecordPlayerPrisonBreakRescue(rescuedHero);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "record prison break rescue failed: " + ex.Message);
		}
	}

	public static void RecordPublicMemoryForExternal(Hero npc, Settlement settlement, string material, string publicity, string reason, int gameDayIndex, string gameDate)
	{
		try
		{
			Instance?.RecordPublicMemory(npc, settlement, material, publicity, reason, gameDayIndex, gameDate);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "record memory failed: " + ex.Message);
		}
	}

	public static void NoteConversationLineForExternal(string heroId)
	{
		try
		{
			Instance?.NoteConversationLine(heroId);
		}
		catch
		{
		}
	}

	public static void NoteCourierSentForExternal(Hero recipient)
	{
		try
		{
			Instance?.NoteCourierSent(recipient);
		}
		catch
		{
		}
	}

	public static void NoteCourierReplyForExternal(Hero recipient)
	{
		try
		{
			Instance?.AdjustPersonalKnownBonus(recipient, CourierReplyKnownBonus, "courier_reply");
		}
		catch
		{
		}
	}

	public static void FinalizeConversationForExternal(Hero hero)
	{
		try
		{
			Instance?.FinalizeConversation(hero);
		}
		catch
		{
		}
	}

	public static void FinalizeConversationForExternal(IEnumerable<CharacterObject> characters)
	{
		try
		{
			Instance?.FinalizeConversation(characters);
		}
		catch
		{
		}
	}

	public static bool DoesObserverKnowPlayerForExternal(Hero observer)
	{
		try
		{
			return Instance?.DoesObserverKnowPlayer(observer) ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static bool DoesObserverKnowPlayerForExternal(string observerKey, string cultureId)
	{
		try
		{
			return Instance?.DoesObserverKnowPlayer(observerKey, cultureId) ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static bool HasObserverUnlockedPlayerMajorForExternal(Hero observer)
	{
		try
		{
			return Instance?.HasObserverUnlockedPlayerMajor(observer) ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static bool HasObserverUnlockedPlayerMajorForExternal(string observerKey)
	{
		try
		{
			return Instance?.HasObserverUnlockedPlayerMajor(observerKey) ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static void MarkObserverKnowsPlayerForExternal(Hero observer, string reason)
	{
		try
		{
			Instance?.MarkObserverKnowsPlayer(observer, reason);
		}
		catch
		{
		}
	}

	public static void MarkObserverKnowsPlayerForExternal(string observerKey, string reason)
	{
		try
		{
			Instance?.MarkObserverKnowsPlayer(observerKey, reason);
		}
		catch
		{
		}
	}

	public static string BuildPlayerMajorRuntimeInstructionForExternal(Hero observer)
	{
		try
		{
			return Instance?.BuildPlayerMajorRuntimeInstruction(observer) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildPlayerMajorRuntimeInstructionForExternal(string observerKey, string cultureId)
	{
		try
		{
			return Instance?.BuildPlayerMajorRuntimeInstruction(observerKey, cultureId) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildPlayerRecentRuntimeInstructionForExternal(Hero observer, bool courier = false)
	{
		try
		{
			return Instance?.BuildPlayerRecentRuntimeInstruction(observer, courier) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildPlayerRecentRuntimeInstructionForExternal(string observerKey, string cultureId, bool courier = false)
	{
		try
		{
			return Instance?.BuildPlayerRecentRuntimeInstruction(observerKey, cultureId, courier) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildPlayerNotorietyEncyclopediaTextForExternal()
	{
		try
		{
			return Instance?.BuildPlayerNotorietyDisplayText(includeRawMaterials: false) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static void OpenPlayerNotorietyViewForExternal()
	{
		try
		{
			Instance?.OpenPlayerNotorietyView();
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开玩家知名度失败：" + ex.Message));
		}
	}

	public static int GetEffectiveNotorietyForExternal(Hero observer)
	{
		try
		{
			return Instance?.GetEffectiveNotoriety(observer) ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	public static int GetEffectiveNotorietyForExternal(string observerKey, string cultureId)
	{
		try
		{
			return Instance?.GetEffectiveNotoriety(observerKey, cultureId) ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	public static int GetCultureNotorietyForExternal(string cultureId)
	{
		try
		{
			return Instance?.GetCultureNotoriety(cultureId) ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	private void RecordPlayerAction(string text, string stableKey, string actionKind, bool isMajor, int day, string gameDate, int sequence, string settlementId, string settlementName, string locationText, string actorCultureId, string targetCultureId, string settlementCultureId, bool? won)
	{
		_state = NormalizeState(_state);
		string normalizedText = NormalizeLine(text);
		if (string.IsNullOrWhiteSpace(normalizedText))
		{
			return;
		}
		int currentDay = day >= 0 ? day : GetCurrentGameDayIndex();
		string key = NormalizeStableKey(stableKey, normalizedText, currentDay);
		PlayerActionEntry entry = new PlayerActionEntry
		{
			Day = currentDay,
			Order = GetNextOrderForDay(_state.RecentActions, currentDay),
			Sequence = sequence > 0 ? sequence : GetNextSequence(),
			GameDate = string.IsNullOrWhiteSpace(gameDate) ? GetCurrentGameDateText() : gameDate.Trim(),
			Text = normalizedText,
			StableKey = key,
			ActionKind = (actionKind ?? "").Trim(),
			SettlementId = (settlementId ?? "").Trim(),
			SettlementName = (settlementName ?? "").Trim(),
			LocationText = (locationText ?? "").Trim(),
			ActorCultureId = NormalizeCultureId(actorCultureId),
			TargetCultureId = NormalizeCultureId(targetCultureId),
			SettlementCultureId = NormalizeCultureId(settlementCultureId),
			Won = won,
			IsMajor = isMajor
		};
		AddActionEntry(_state.RecentActions, entry, keepRecentWindow: true, MaxRecentActions);
		if (isMajor)
		{
			AddHistoryMaterialFromAction(entry);
		}
		LogDebug("record action major=" + isMajor + " kind=" + entry.ActionKind + " day=" + entry.Day + " text=" + entry.Text);
	}

	private void RecordPlayerHistoryMaterial(string text, string stableKey, string sourceKind, int day, string gameDate, string actorCultureId, string targetCultureId, string settlementCultureId)
	{
		_state = NormalizeState(_state);
		string normalizedText = NormalizeLine(text);
		if (string.IsNullOrWhiteSpace(normalizedText))
		{
			return;
		}
		int currentDay = day >= 0 ? day : GetCurrentGameDayIndex();
		PlayerHistoryMaterial material = new PlayerHistoryMaterial
		{
			Day = currentDay,
			GameDate = string.IsNullOrWhiteSpace(gameDate) ? GetCurrentGameDateText() : gameDate.Trim(),
			Text = normalizedText,
			SourceKind = string.IsNullOrWhiteSpace(sourceKind) ? "player_history_material" : sourceKind.Trim(),
			StableKey = NormalizeStableKey(stableKey, normalizedText, currentDay),
			CultureIds = BuildCultureIds(actorCultureId, targetCultureId, settlementCultureId),
			Summarized = false,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		AddHistoryMaterial(material);
		LogDebug("record history material kind=" + material.SourceKind + " day=" + material.Day + " text=" + material.Text);
	}

	private void RecordPlayerPrisonBreakRescue(Hero rescuedHero)
	{
		try
		{
			Hero player = Hero.MainHero;
			if (player == null || rescuedHero == null || rescuedHero == player)
			{
				return;
			}
			int day = GetCurrentGameDayIndex();
			int hour = GetCurrentGameHour();
			Settlement settlement = ResolvePlayerCurrentSettlement() ?? rescuedHero.CurrentSettlement ?? rescuedHero.StayingInSettlement;
			bool hasSettlement = settlement != null;
			string settlementName = hasSettlement ? GetSettlementDisplayName(settlement) : "";
			string locationText = hasSettlement ? settlementName : "越狱现场";
			string locationPhrase = hasSettlement ? ("在" + settlementName + "的地牢中") : "从囚禁中";
			string rescuedName = GetHeroDisplayName(rescuedHero);
			string text = "你" + locationPhrase + "越狱营救了" + rescuedName + "，并帮助其成功脱离囚禁。";
			string stableKey = "player_prison_break_rescue:" + GetHeroId(player) + ":" + GetHeroId(rescuedHero) + ":" + (settlement?.StringId ?? "") + ":" + day + ":" + hour;
			RecordPlayerAction(
				text,
				stableKey,
				"prison_break_rescue",
				isMajor: true,
				day,
				GetCurrentGameDateText(),
				0,
				settlement?.StringId ?? "",
				settlementName,
				locationText,
				player.Culture?.StringId ?? "",
				rescuedHero.Culture?.StringId ?? "",
				settlement?.Culture?.StringId ?? "",
				true);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "RecordPlayerPrisonBreakRescue failed: " + ex.Message);
		}
	}

	private void RecordPublicMemory(Hero npc, Settlement settlement, string material, string publicity, string reason, int gameDayIndex, string gameDate)
	{
		_state = NormalizeState(_state);
		string text = NormalizeLine(material);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string normalizedPublicity = (publicity ?? "").Trim().ToLowerInvariant();
		bool isPublic = normalizedPublicity == "public" || normalizedPublicity == "leaked_public";
		if (!isPublic)
		{
			LogDebug("skip private memory npc=" + (npc?.StringId ?? "") + " publicity=" + normalizedPublicity);
			return;
		}
		int day = gameDayIndex >= 0 ? gameDayIndex : GetCurrentGameDayIndex();
		PlayerHistoryMaterial historyMaterial = new PlayerHistoryMaterial
		{
			Day = day,
			GameDate = string.IsNullOrWhiteSpace(gameDate) ? GetCurrentGameDateText() : gameDate.Trim(),
			Text = text,
			SourceKind = "public_memory",
			StableKey = "memory:" + (npc?.StringId ?? "unknown") + ":" + day + ":" + Math.Abs(text.GetHashCode()),
			CultureIds = BuildCultureIds(npc?.Culture?.StringId, settlement?.Culture?.StringId, null),
			Summarized = false,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		AddHistoryMaterial(historyMaterial);
		foreach (string cultureId in historyMaterial.CultureIds)
		{
			AddCultureNotoriety(cultureId, 1.0, "public_memory");
		}
		LogDebug("record public memory npc=" + (npc?.StringId ?? "") + " cultures=" + string.Join(",", historyMaterial.CultureIds));
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		try
		{
			if (prisoner == null)
			{
				return;
			}
			if (prisoner == Hero.MainHero)
			{
				string playerText = BuildMainHeroReleasedText(party, capturerFaction, detail);
				if (!string.IsNullOrWhiteSpace(playerText))
				{
					RecordPlayerRecentActionFromEvent(playerText, "player_captivity_released", GetHeroId(prisoner) + ":" + detail, Hero.MainHero?.Culture?.StringId ?? "", ResolvePlayerCurrentSettlement(), "");
				}
				return;
			}
			if (!IsPlayerPartyBase(party) || !ShouldRecordPlayerHeroPrisonerRelease(detail))
			{
				return;
			}
			string verb = BuildPlayerHeroPrisonerReleaseVerb(detail);
			if (string.IsNullOrWhiteSpace(verb))
			{
				return;
			}
			string prisonerName = GetHeroDisplayName(prisoner);
			string text = "你" + verb + prisonerName + "。";
			RecordPlayerRecentActionFromEvent(text, "hero_prisoner_released", GetHeroId(prisoner) + ":" + detail, prisoner?.Culture?.StringId ?? "", ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "hero prisoner release recent action failed: " + ex.Message);
		}
	}

	private void OnPlayerPrisonersReleased(FlattenedTroopRoster roster)
	{
		try
		{
			PrisonerRosterSummary summary = BuildFlattenedPrisonerRosterSummary(roster, includeHeroes: false);
			if (summary.TotalCount <= 0)
			{
				return;
			}
			string text = "你释放了 " + summary.TotalCount + " 名普通俘虏" + BuildRosterDetailSuffix(summary) + "。";
			RecordPlayerRecentActionFromEvent(text, "prisoners_released", summary.Signature, summary.PrimaryCultureId, ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "prisoner release recent action failed: " + ex.Message);
		}
	}

	private void OnPlayerPrisonersSold(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
	{
		try
		{
			if (!IsPlayerPartyBase(sellerParty))
			{
				return;
			}
			PrisonerRosterSummary summary = BuildTroopRosterSummary(prisoners, includeHeroes: true);
			if (summary.TotalCount <= 0)
			{
				return;
			}
			Settlement settlement = buyerParty?.Settlement ?? sellerParty?.Settlement ?? ResolvePlayerCurrentSettlement();
			string buyerName = BuildPartyDisplayName(buyerParty);
			string targetText = string.IsNullOrWhiteSpace(buyerName) ? "" : ("给" + buyerName);
			string text = "你" + targetText + "出售了 " + summary.TotalCount + " 名俘虏" + BuildRosterDetailSuffix(summary) + "。";
			RecordPlayerRecentActionFromEvent(text, "prisoners_sold", BuildPartyScope(buyerParty) + ":" + summary.Signature, summary.PrimaryCultureId, settlement, "");
			if (buyerParty?.Settlement != null)
			{
				_soldPrisonerDonationSkipKeys.Add(BuildPrisonerDonationSkipKey(buyerParty.Settlement, summary.Signature));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "prisoner sold recent action failed: " + ex.Message);
		}
	}

	private void OnMainPartyPrisonerRecruited(FlattenedTroopRoster roster)
	{
		try
		{
			PrisonerRosterSummary summary = BuildFlattenedPrisonerRosterSummary(roster, includeHeroes: true);
			if (summary.TotalCount <= 0)
			{
				return;
			}
			string text = "你招募了 " + summary.TotalCount + " 名曾为俘虏的士兵加入队伍" + BuildRosterDetailSuffix(summary) + "。";
			RecordPlayerRecentActionFromEvent(text, "prisoners_recruited", summary.Signature, summary.PrimaryCultureId, ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "prisoner recruited recent action failed: " + ex.Message);
		}
	}

	private void OnPlayerPrisonersDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
	{
		try
		{
			if (!IsPlayerMobileParty(donatingParty))
			{
				return;
			}
			PrisonerRosterSummary summary = BuildFlattenedPrisonerRosterSummary(donatedPrisoners, includeHeroes: true);
			if (summary.TotalCount <= 0)
			{
				return;
			}
			string skipKey = BuildPrisonerDonationSkipKey(donatedSettlement, summary.Signature);
			if (_soldPrisonerDonationSkipKeys.Remove(skipKey))
			{
				return;
			}
			string settlementName = GetSettlementDisplayName(donatedSettlement);
			string text = "你向" + settlementName + "移交了 " + summary.TotalCount + " 名俘虏" + BuildRosterDetailSuffix(summary) + "。";
			RecordPlayerRecentActionFromEvent(text, "prisoners_donated", (donatedSettlement?.StringId ?? "") + ":" + summary.Signature, summary.PrimaryCultureId, donatedSettlement ?? ResolvePlayerCurrentSettlement(), settlementName);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "prisoner donated recent action failed: " + ex.Message);
		}
	}

	private void OnPlayerSettlementEnteredForRecentAction(MobileParty party, Settlement settlement, Hero hero)
	{
		try
		{
			if (settlement == null || settlement.IsHideout || (!IsPlayerMobileParty(party) && hero != Hero.MainHero))
			{
				return;
			}
			_currentSettlementStayId = (settlement.StringId ?? "").Trim();
			_currentSettlementStayName = GetSettlementDisplayName(settlement);
			_currentSettlementStayStartDays = GetCurrentGameTimeDays();
			_currentSettlementStayStartDay = GetCurrentGameDayIndex();
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "settlement stay start tracking failed: " + ex.Message);
		}
	}

	private void OnPlayerSettlementLeftForRecentAction(MobileParty party, Settlement settlement)
	{
		try
		{
			if (!IsPlayerMobileParty(party) || settlement == null || settlement.IsHideout)
			{
				return;
			}
			string settlementId = (settlement.StringId ?? "").Trim();
			if (_currentSettlementStayStartDays < 0.0 || !string.Equals(_currentSettlementStayId, settlementId, StringComparison.OrdinalIgnoreCase))
			{
				ClearCurrentSettlementStayTracking();
				return;
			}
			double stayHours = Math.Max(0.0, (GetCurrentGameTimeDays() - _currentSettlementStayStartDays) * 24.0);
			int currentDay = GetCurrentGameDayIndex();
			bool crossedDay = _currentSettlementStayStartDay >= 0 && _currentSettlementStayStartDay != currentDay;
			if (stayHours >= 6.0 || (crossedDay && stayHours >= 3.0))
			{
				string settlementName = string.IsNullOrWhiteSpace(_currentSettlementStayName) ? GetSettlementDisplayName(settlement) : _currentSettlementStayName.Trim();
				string text = stayHours >= 20.0
					? ("你最近在" + settlementName + "停留了约 " + FormatStayDuration(stayHours) + "，进行休整和补给。")
					: ("你最近在" + settlementName + "休整了约 " + FormatStayDuration(stayHours) + "。");
				RecordPlayerRecentActionFromEvent(text, "settlement_rest_stay", settlementId + ":" + _currentSettlementStayStartDay + ":" + currentDay, settlement?.Culture?.StringId ?? "", settlement, settlementName);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "settlement stay recent action failed: " + ex.Message);
		}
		finally
		{
			ClearCurrentSettlementStayTracking();
		}
	}

	private void OnPlayerTroopRecruitedForRecentAction(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruiter, CharacterObject troop, int amount)
	{
		try
		{
			if (recruiterHero != Hero.MainHero || troop == null || amount <= 0)
			{
				return;
			}
			string settlementName = GetSettlementDisplayName(recruitmentSettlement);
			string troopName = GetCharacterDisplayName(troop);
			string sourceText = recruiter == null || recruiter == Hero.MainHero ? "" : ("，来源：" + GetHeroDisplayName(recruiter));
			string text = "你在" + settlementName + "招募了 " + amount + " 名 " + troopName + sourceText + "。";
			string scope = (recruitmentSettlement?.StringId ?? "") + ":" + (troop.StringId ?? troopName) + ":" + GetHeroId(recruiter) + ":" + amount;
			RecordPlayerRecentActionFromEvent(text, "troops_recruited", scope, troop.Culture?.StringId ?? "", recruitmentSettlement, settlementName);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "troop recruitment recent action failed: " + ex.Message);
		}
	}

	private void OnTournamentFinishedForPlayerRecentAction(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		try
		{
			CharacterObject playerCharacter = Hero.MainHero?.CharacterObject;
			if (playerCharacter == null || participants == null || !participants.Any(x => x == playerCharacter || x?.HeroObject == Hero.MainHero))
			{
				return;
			}
			if (winner == playerCharacter || winner?.HeroObject == Hero.MainHero)
			{
				return;
			}
			Settlement settlement = town?.Settlement;
			string settlementName = GetSettlementDisplayName(settlement);
			string winnerName = GetCharacterDisplayName(winner);
			string prizeText = string.IsNullOrWhiteSpace(prize?.Name?.ToString()) ? "" : (" 奖品是" + prize.Name.ToString().Trim() + "。");
			string text = "你参加了" + settlementName + "的竞技大会，但没有夺冠。本次冠军是" + winnerName + "。" + prizeText;
			string scope = (settlement?.StringId ?? "") + ":" + GetCurrentGameDayIndex() + ":" + (winner?.StringId ?? winnerName);
			RecordPlayerRecentActionFromEvent(text, "tournament_participated_nonwinner", scope, winner?.Culture?.StringId ?? "", settlement, settlementName);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "tournament participation recent action failed: " + ex.Message);
		}
	}

	private void OnPlayerBoardGameOverForRecentAction(Hero opposingHero, BoardGameHelper.BoardGameState state)
	{
		try
		{
			if (state == BoardGameHelper.BoardGameState.None)
			{
				return;
			}
			string opponentName = GetHeroDisplayName(opposingHero);
			string resultText = GetBoardGameResultText(state);
			string text = "你与" + opponentName + "下了一局棋，结果：" + resultText + "。";
			string scope = GetHeroId(opposingHero) + ":" + state;
			RecordPlayerRecentActionFromEvent(text, "board_game_result", scope, opposingHero?.Culture?.StringId ?? "", ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "board game recent action failed: " + ex.Message);
		}
	}

	private void OnRansomOfferCancelledForPlayerRecentAction(Hero captiveHero)
	{
		try
		{
			if (captiveHero == null)
			{
				return;
			}
			string captiveName = GetHeroDisplayName(captiveHero);
			string text = "有关" + captiveName + "的赎金提议没有达成。";
			RecordPlayerRecentActionFromEvent(text, "ransom_offer_cancelled", GetHeroId(captiveHero), captiveHero.Culture?.StringId ?? "", ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "ransom offer cancellation recent action failed: " + ex.Message);
		}
	}

	private void OnMarriageOfferCanceledForPlayerRecentAction(Hero suitor, Hero maiden)
	{
		try
		{
			if (!IsPlayerClanHero(suitor) && !IsPlayerClanHero(maiden))
			{
				return;
			}
			string suitorName = GetHeroDisplayName(suitor);
			string maidenName = GetHeroDisplayName(maiden);
			string text = "有关" + suitorName + "与" + maidenName + "的婚约提议没有达成。";
			Hero otherHero = IsPlayerClanHero(suitor) ? maiden : suitor;
			RecordPlayerRecentActionFromEvent(text, "marriage_offer_cancelled", GetHeroId(suitor) + ":" + GetHeroId(maiden), otherHero?.Culture?.StringId ?? "", ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "marriage offer cancellation recent action failed: " + ex.Message);
		}
	}

	private void OnVassalOrMercenaryOfferCanceledForPlayerRecentAction(Kingdom offeredKingdom)
	{
		try
		{
			if (offeredKingdom == null)
			{
				return;
			}
			string kingdomName = GetFactionDisplayName(offeredKingdom, "某个王国");
			string text = "来自" + kingdomName + "的雇佣或效忠邀请没有达成。";
			RecordPlayerRecentActionFromEvent(text, "vassal_mercenary_offer_cancelled", GetKingdomId(offeredKingdom), "", null, kingdomName);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "vassal or mercenary offer cancellation recent action failed: " + ex.Message);
		}
	}

	private void OnPartyLeaderChangeOfferCanceledForPlayerRecentAction(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return;
			}
			string partyName = string.IsNullOrWhiteSpace(party.Name?.ToString()) ? "一支部队" : party.Name.ToString().Trim();
			string text = "你收到的接管" + partyName + "领导权提议没有达成。";
			RecordPlayerRecentActionFromEvent(text, "party_leader_change_offer_cancelled", party.StringId ?? partyName, party.LeaderHero?.Culture?.StringId ?? "", party.CurrentSettlement ?? ResolvePlayerCurrentSettlement(), "");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "party leader change offer cancellation recent action failed: " + ex.Message);
		}
	}

	private void OnPeaceOfferResolvedForPlayerRecentAction(IFaction opponentFaction)
	{
		try
		{
			if (opponentFaction == null)
			{
				return;
			}
			string factionName = GetFactionDisplayName(opponentFaction, "对方势力");
			string text = "你所在势力近期处理了来自" + factionName + "的和平提议。";
			RecordPlayerRecentActionFromEvent(text, "peace_offer_resolved", GetFactionId(opponentFaction), "", null, factionName);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "peace offer recent action failed: " + ex.Message);
		}
	}

	private void OnAlleyClearedByPlayerForRecentAction(Alley alley)
	{
		try
		{
			Settlement settlement = alley?.Settlement ?? Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			string settlementName = GetSettlementDisplayName(settlement);
			string alleyName = GetAlleyDisplayName(alley);
			string text = "你清理了" + settlementName + "的" + alleyName + "，并选择不占领该街巷。";
			string scope = (settlement?.StringId ?? "") + ":" + (alley?.Tag ?? alleyName);
			RecordPlayerRecentActionFromEvent(text, "alley_cleared_not_occupied", scope, settlement?.Culture?.StringId ?? "", settlement, settlementName);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "alley cleared recent action failed: " + ex.Message);
		}
	}

	private void RecordPlayerRecentActionFromEvent(string text, string actionKind, string scope, string targetCultureId, Settlement settlement, string locationText)
	{
		string normalizedText = NormalizeLine(text);
		if (string.IsNullOrWhiteSpace(normalizedText))
		{
			return;
		}
		int day = GetCurrentGameDayIndex();
		string stableKey = BuildPlayerRecentEventStableKey(actionKind, scope, day);
		RecordPlayerAction(normalizedText, stableKey, actionKind, isMajor: false, day, GetCurrentGameDateText(), 0, settlement?.StringId ?? "", GetSettlementDisplayName(settlement), locationText ?? "", Hero.MainHero?.Culture?.StringId ?? "", targetCultureId ?? "", settlement?.Culture?.StringId ?? "", null);
	}

	private void AddHistoryMaterialFromAction(PlayerActionEntry entry)
	{
		if (entry == null || string.IsNullOrWhiteSpace(entry.Text))
		{
			return;
		}
		List<string> cultureIds = BuildCultureIds(entry.ActorCultureId, entry.TargetCultureId, entry.SettlementCultureId);
		PlayerHistoryMaterial material = new PlayerHistoryMaterial
		{
			Day = entry.Day,
			GameDate = entry.GameDate ?? "",
			Text = entry.Text,
			SourceKind = string.IsNullOrWhiteSpace(entry.ActionKind) ? "player_action" : entry.ActionKind,
			StableKey = entry.StableKey ?? "",
			CultureIds = cultureIds,
			Summarized = false,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		AddHistoryMaterial(material);
	}

	private void AddHistoryMaterial(PlayerHistoryMaterial material)
	{
		if (material == null || string.IsNullOrWhiteSpace(material.Text))
		{
			return;
		}
		_state = NormalizeState(_state);
		string key = NormalizeStableKey(material.StableKey, material.Text, material.Day);
		if (IsKnownMajorMaterialKey(key))
		{
			return;
		}
		material.StableKey = key;
		material.Text = RenderPlayerHistoryTextForPrompt(material.Text, BuildPlayerHistoryDisplayName());
		if (string.IsNullOrWhiteSpace(material.Text))
		{
			return;
		}
		material.CultureIds = NormalizeCultureList(material.CultureIds);
		_state.MajorMaterials.Add(material);
		if (_state.MajorMaterials.Count > MaxMajorMaterials)
		{
			_state.MajorMaterials = _state.MajorMaterials
				.OrderByDescending(x => x?.Day ?? int.MinValue)
				.ThenByDescending(x => x?.CreatedUtcTicks ?? 0L)
				.Take(MaxMajorMaterials)
				.OrderBy(x => x?.Day ?? 0)
				.ThenBy(x => x?.CreatedUtcTicks ?? 0L)
				.ToList();
		}
		TryStartSummaryProcessing();
	}

	private bool IsKnownMajorMaterialKey(string key)
	{
		string normalizedKey = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalizedKey))
		{
			return false;
		}
		if (_state?.MajorMaterials?.Any(x => x != null && string.Equals(x.StableKey ?? "", normalizedKey, StringComparison.OrdinalIgnoreCase)) == true)
		{
			return true;
		}
		return _state?.SummarizedMaterialKeys?.Any(x => string.Equals(x ?? "", normalizedKey, StringComparison.OrdinalIgnoreCase)) == true;
	}

	private void OnDailyTick()
	{
		_state = NormalizeState(_state);
		PruneRecentActions();
		_soldPrisonerDonationSkipKeys.Clear();
		FinalizeStaleActiveConversations();
		TryStartSummaryProcessing();
	}

	private void OnNativeConversationEnded(IEnumerable<CharacterObject> characters)
	{
		FinalizeConversation(characters);
	}

	private void TryStartSummaryProcessing(bool force = false)
	{
		if (_summaryProcessing)
		{
			return;
		}
		if (!force && !HasSummaryWorkDue())
		{
			return;
		}
		if (force && !HasPendingMajorMaterials() && !IsMajorSummaryOverPromptLimit())
		{
			return;
		}
		_summaryProcessing = true;
		_ = ProcessSummaryAsync();
	}

	private bool HasSummaryWorkDue()
	{
		_state = NormalizeState(_state);
		if (IsMajorSummaryOverPromptLimit())
		{
			return true;
		}
		if (!HasPendingMajorMaterials())
		{
			return false;
		}
		int interval = GetSummaryIntervalDays();
		if (_state.LastSummaryDay < 0)
		{
			return true;
		}
		return GetCurrentGameDayIndex() - _state.LastSummaryDay >= interval;
	}

	private bool HasPendingMajorMaterials()
	{
		return _state?.MajorMaterials != null && _state.MajorMaterials.Any(x => x != null && !x.Summarized);
	}

	private async Task ProcessSummaryAsync()
	{
		bool continueAfterBatch = false;
		try
		{
			_state = NormalizeState(_state);
			List<PlayerHistoryMaterial> sourceMaterials = _state.MajorMaterials
				.Where(x => x != null && !x.Summarized)
				.OrderBy(x => x.Day)
				.ThenBy(x => x.CreatedUtcTicks)
				.Take(SummaryBatchSize)
				.ToList();
			bool compactOnly = sourceMaterials.Count == 0 && IsMajorSummaryOverPromptLimit();
			if (sourceMaterials.Count == 0 && !compactOnly)
			{
				return;
			}
			string playerDisplayName = BuildSummaryPlayerDisplayName();
			string sys = BuildSummarySystemPrompt(playerDisplayName);
			string user = BuildSummaryUserPrompt(sourceMaterials, playerDisplayName);
			string response = await MyBehavior.CallAuxiliaryApiTextForExternal(sys, user, "PlayerNotorietySummary");
			if (TryParseSummaryResponse(response, out string summary, out double delta, out string error))
			{
				summary = RenderPlayerHistoryTextForPrompt(summary, playerDisplayName);
				ApplySummarySuccess(sourceMaterials, summary, delta);
				continueAfterBatch = HasPendingMajorMaterials();
				return;
			}
			_state.SummaryRetryCount++;
			_state.LastSummaryError = error;
			Logger.Log("PlayerNotoriety", "summary parse failed: " + error);
			if (_state.SummaryRetryCount >= MaxSummaryRetries)
			{
				if (compactOnly)
				{
					_state.MajorSummary = TrimMajorSummaryFallback(_state.MajorSummary, GetMajorPromptChars());
				}
				else
				{
					_state.MajorSummary = BuildFallbackMajorSummary(_state.MajorSummary, sourceMaterials, playerDisplayName);
					foreach (PlayerHistoryMaterial material in sourceMaterials)
					{
						if (material != null)
						{
							material.Summarized = true;
							RememberSummarizedMaterialKey(material);
						}
					}
					PruneSummarizedMajorMaterials();
				}
				_state.SummaryRetryCount = 0;
				continueAfterBatch = HasPendingMajorMaterials();
			}
		}
		catch (Exception ex)
		{
			_state.LastSummaryError = ex.Message;
			Logger.Log("PlayerNotoriety", "summary failed: " + ex);
		}
		finally
		{
			_summaryProcessing = false;
			if (continueAfterBatch)
			{
				TryStartSummaryProcessing(force: true);
			}
		}
	}

	public static bool TryGetLatestPlayerRecentActionForExternal(Hero observer, int maxAgeDays, out string stableKey, out string actionText, out int day)
	{
		stableKey = "";
		actionText = "";
		day = -1;
		try
		{
			return Instance?.TryGetLatestPlayerRecentAction(observer, maxAgeDays, out stableKey, out actionText, out day) == true;
		}
		catch
		{
			stableKey = "";
			actionText = "";
			day = -1;
			return false;
		}
	}

	private static string BuildSummarySystemPrompt(string playerDisplayName)
	{
		int targetChars = GetMajorPromptChars();
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		return "你是 AnimusForge 的" + playerName + "履历与知名度总结器。只输出严格 JSON：{\"summary_content\":\"新的" + playerName + "重大履历时间线摘要\",\"notoriety_delta\":0到10之间的小数}。"
			+ "任务是重写，不是增量续写：已有摘要只作为待压缩素材，必须与新增素材打散、去重、合并，输出一份完整的新摘要来整体替换旧摘要。"
			+ "summary_content 必须压缩到" + targetChars + "个中文字符以内；这是硬上限，不是建议。若事实放不下，必须主动删掉次要事件、重复信息、数字细节和长名单，不得为了保留全部内容而超长。"
			+ "只保留最重要的关键人物、地点、胜败、承诺、身份变化和公开影响；同类战斗、任务、处决、招募必须归并概括，禁止逐条罗列。"
			+ "提及履历主体时必须一律使用“" + playerName + "”，不得写“你”、“玩家”或文化加年龄段描述。"
			+ "没有新增素材时只压缩已有摘要，notoriety_delta 输出0。不要编造素材没有的事实。"
			+ "notoriety_delta 只评估本次新增公开素材，严禁根据已有摘要重复增加；范围0-10，小事应为0到1之间的小数，重大胜利、夺城、处决、王国事件才可接近10。";
	}

	private string BuildSummaryUserPrompt(List<PlayerHistoryMaterial> materials, string playerDisplayName)
	{
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("已有" + playerName + "履历摘要：");
		sb.AppendLine(string.IsNullOrWhiteSpace(_state.MajorSummary) ? "（无）" : RenderPlayerHistoryTextForPrompt(_state.MajorSummary.Trim(), playerName));
		sb.AppendLine();
		sb.AppendLine("新增公开素材（" + playerName + "）：");
		List<PlayerHistoryMaterial> sourceMaterials = materials ?? new List<PlayerHistoryMaterial>();
		if (sourceMaterials.Count == 0)
		{
			sb.AppendLine("（无新增素材；只压缩已有摘要，丢弃不重要信息）");
		}
		foreach (PlayerHistoryMaterial material in sourceMaterials)
		{
			if (material == null || string.IsNullOrWhiteSpace(material.Text))
			{
				continue;
			}
			sb.AppendLine("- [" + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "][" + (material.SourceKind ?? "material") + "][culture:" + string.Join(",", material.CultureIds ?? new List<string>()) + "] " + RenderPlayerHistoryTextForPrompt(material.Text.Trim(), playerName));
		}
		sb.AppendLine();
		sb.AppendLine("现在整体重写。只输出 JSON；summary_content 最多 " + GetMajorPromptChars() + " 个中文字符。若超长，继续删除低价值细节后再输出。禁止在旧摘要后追加。");
		return sb.ToString().Trim();
	}

	private bool TryParseSummaryResponse(string response, out string summary, out double delta, out string error)
	{
		summary = "";
		delta = 0.0;
		error = "";
		try
		{
			if (string.IsNullOrWhiteSpace(response))
			{
				error = "empty response";
				return false;
			}
			JObject obj = TryParseJsonObject(response);
			if (obj == null)
			{
				error = "not json";
				return false;
			}
			summary = GetJsonString(obj, "summary_content", "summaryContent", "summary", "content").Trim();
			if (string.IsNullOrWhiteSpace(summary))
			{
				error = "summary_content empty";
				return false;
			}
			JToken deltaToken = GetJsonToken(obj, "notoriety_delta", "notorietyDelta", "delta");
			if (deltaToken != null && double.TryParse(deltaToken.ToString(), out double parsed))
			{
				delta = ClampDouble(parsed, 0.0, 10.0);
			}
			else
			{
				delta = 0.0;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private void ApplySummarySuccess(List<PlayerHistoryMaterial> materials, string summary, double delta)
	{
		_state = NormalizeState(_state);
		_state.MajorSummary = NormalizeMajorSummaryForStorage(summary);
		_state.LastSummaryDay = GetCurrentGameDayIndex();
		_state.SummaryRetryCount = 0;
		_state.LastSummaryError = "";
		_state.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
		HashSet<string> cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int materialCount = 0;
		foreach (PlayerHistoryMaterial material in materials ?? new List<PlayerHistoryMaterial>())
		{
			if (material == null)
			{
				continue;
			}
			materialCount++;
			material.Summarized = true;
			RememberSummarizedMaterialKey(material);
			foreach (string cultureId in NormalizeCultureList(material.CultureIds))
			{
				cultures.Add(cultureId);
			}
		}
		PruneSummarizedMajorMaterials();
		if (materialCount <= 0)
		{
			delta = 0.0;
		}
		if (materialCount > 0 && cultures.Count == 0)
		{
			AddWorldNotoriety(delta / 3.0, "summary_world_only");
		}
		else if (materialCount > 0)
		{
			foreach (string cultureId in cultures)
			{
				AddCultureNotoriety(cultureId, delta, "summary");
			}
		}
		Logger.Log("PlayerNotoriety", "summary_success materials=" + (materials?.Count ?? 0) + " delta=" + delta.ToString("0.##") + " cultures=" + string.Join(",", cultures));
	}

	private string BuildPlayerMajorRuntimeInstruction(Hero observer)
	{
		if (!IsValidObserver(observer))
		{
			return "";
		}
		return BuildPlayerMajorRuntimeInstruction(GetHeroId(observer), observer?.Culture?.StringId, BuildPlayerHistoryDisplayName());
	}

	private string BuildPlayerMajorRuntimeInstruction(string observerKey, string cultureId)
	{
		return BuildPlayerMajorRuntimeInstruction(observerKey, cultureId, BuildPlayerHistoryDisplayName());
	}

	private string BuildPlayerMajorRuntimeInstruction(string observerKey, string cultureId, string playerDisplayName)
	{
		if (!DoesObserverKnowPlayer(observerKey, cultureId))
		{
			return "";
		}
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		string major = BuildMajorHistoryForPrompt(playerName);
		if (string.IsNullOrWhiteSpace(major))
		{
			return "";
		}
		return "【已知的" + playerName + "重大履历】\n" + major + "\n边界：以上是" + playerName + "公开履历，可自然提及，勿说成系统提示。";
	}

	private string BuildPlayerRecentRuntimeInstruction(Hero observer, bool courier)
	{
		if (!IsValidObserver(observer))
		{
			return "";
		}
		return BuildPlayerRecentRuntimeInstruction(GetHeroId(observer), observer?.Culture?.StringId, courier, BuildPlayerDisplayNameForPrompt(observer));
	}

	private string BuildPlayerRecentRuntimeInstruction(string observerKey, string cultureId, bool courier)
	{
		return BuildPlayerRecentRuntimeInstruction(observerKey, cultureId, courier, BuildPlayerDisplayNameForPrompt(observerKey, cultureId));
	}

	private string BuildPlayerRecentRuntimeInstruction(string observerKey, string cultureId, bool courier, string playerDisplayName)
	{
		if (!CanObserverKnowRecentActions(observerKey, cultureId, courier))
		{
			return "";
		}
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		PruneRecentActions();
		List<PlayerActionEntry> recent = (_state.RecentActions ?? new List<PlayerActionEntry>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.Sequence)
			.Take(16)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.Sequence)
			.ToList();
		if (recent.Count == 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【已知的" + playerName + "近期行动】");
		foreach (PlayerActionEntry entry in recent)
		{
			sb.AppendLine("- " + (string.IsNullOrWhiteSpace(entry.GameDate) ? ("第" + entry.Day + "日") : entry.GameDate.Trim()) + "：" + RenderPlayerActionTextForPrompt(entry.Text, playerName));
		}
		sb.Append("边界：以上是" + playerName + "最近十天公开行动，可自然提及，勿说成系统提示。");
		return sb.ToString().Trim();
	}

	private bool TryGetLatestPlayerRecentAction(Hero observer, int maxAgeDays, out string stableKey, out string actionText, out int day)
	{
		stableKey = "";
		actionText = "";
		day = -1;
		if (!IsValidObserver(observer))
		{
			return false;
		}
		bool isCurrentPartyMember = false;
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			isCurrentPartyMember = mainParty != null
				&& (observer.PartyBelongedTo == mainParty || (observer.CharacterObject != null && mainParty.MemberRoster.GetTroopCount(observer.CharacterObject) > 0));
		}
		catch
		{
			isCurrentPartyMember = false;
		}
		if (!isCurrentPartyMember && !CanObserverKnowRecentActions(observer, courier: false))
		{
			return false;
		}
		PruneRecentActions();
		int windowDays = Math.Max(1, Math.Min(RecentActionWindowDays, maxAgeDays));
		int minDay = GetCurrentGameDayIndex() - windowDays + 1;
		PlayerActionEntry latest = (_state.RecentActions ?? new List<PlayerActionEntry>())
			.Where(x => x != null && x.Day >= minDay && !string.IsNullOrWhiteSpace(x.Text))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.Sequence)
			.ThenByDescending(x => x.Order)
			.FirstOrDefault();
		if (latest == null)
		{
			return false;
		}
		stableKey = string.IsNullOrWhiteSpace(latest.StableKey)
			? ((latest.ActionKind ?? "player_event") + ":" + latest.Day + ":" + latest.Order + ":" + latest.Sequence)
			: latest.StableKey.Trim();
		actionText = latest.Text.Trim();
		day = latest.Day;
		return !string.IsNullOrWhiteSpace(stableKey) && !string.IsNullOrWhiteSpace(actionText);
	}

	private string BuildMajorHistoryForPrompt(string playerDisplayName)
	{
		_state = NormalizeState(_state);
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		string summary = NormalizeMajorSummaryForStorage(_state.MajorSummary);
		summary = RenderPlayerHistoryTextForPrompt(summary, playerName);
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(summary))
		{
			sb.AppendLine(summary);
		}
		return sb.ToString().Trim();
	}

	private static string BuildPlayerDisplayNameForPrompt(Hero observer)
	{
		try
		{
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal(observer);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return NormalizePlayerDisplayName(text);
			}
		}
		catch
		{
		}
		return "玩家";
	}

	private static string BuildPlayerDisplayNameForPrompt(string observerKey, string cultureId)
	{
		Hero observer = FindHeroById(observerKey);
		if (observer != null)
		{
			return BuildPlayerDisplayNameForPrompt(observer);
		}
		try
		{
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal(observerKey, cultureId);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return NormalizePlayerDisplayName(text);
			}
		}
		catch
		{
		}
		return "玩家";
	}

	private static string NormalizePlayerDisplayName(string playerDisplayName)
	{
		string text = NormalizeLine(playerDisplayName);
		return string.IsNullOrWhiteSpace(text) ? "玩家" : text;
	}

	private static string BuildSummaryPlayerDisplayName()
	{
		return BuildPlayerHistoryDisplayName();
	}

	private static string BuildPlayerHistoryDisplayName()
	{
		try
		{
			string text = NormalizeLine(Hero.MainHero?.Name?.ToString()).Replace("玩家", "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		return "主角";
	}

	private static IEnumerable<string> BuildPlayerHistoryAnonymousAliases(string playerName)
	{
		HashSet<string> aliases = new HashSet<string>(StringComparer.Ordinal);
		string actualName = NormalizePlayerDisplayName(playerName);
		try
		{
			string publicDisplayName = NormalizeLine(MyBehavior.BuildPlayerPublicDisplayNameForExternal());
			if (!string.IsNullOrWhiteSpace(publicDisplayName) && !string.Equals(publicDisplayName, actualName, StringComparison.Ordinal))
			{
				aliases.Add(publicDisplayName);
			}
		}
		catch
		{
		}
		string[] ageLabels = new string[6] { "少年", "青年", "壮年", "中年", "老年", "未知" };
		foreach (CultureObject culture in GetCulturesForNotorietyPopup())
		{
			string cultureName = NormalizeLine(culture?.Name?.ToString());
			if (string.IsNullOrWhiteSpace(cultureName))
			{
				continue;
			}
			foreach (string ageLabel in ageLabels)
			{
				string alias = cultureName + ageLabel;
				if (!string.Equals(alias, actualName, StringComparison.Ordinal) && actualName.IndexOf(alias, StringComparison.Ordinal) < 0)
				{
					aliases.Add(alias);
				}
			}
		}
		return aliases.OrderByDescending(x => x.Length);
	}

	private static string StripPlayerInternalMarkers(string text)
	{
		return (text ?? "").Replace("\uFF08player\uFF09", "").Replace("(player)", "");
	}

	private static string RenderPlayerActionTextForPrompt(string rawText, string playerDisplayName)
	{
		string text = StripPlayerInternalMarkers(NormalizeLine(rawText));
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string name = NormalizePlayerDisplayName(playerDisplayName);
		return text
			.Replace("玩家的", name + "的")
			.Replace("玩家", name)
			.Replace("你们的", name + "一方的")
			.Replace("你们", name + "一方")
			.Replace("你方的", name + "一方的")
			.Replace("你方", name + "一方")
			.Replace("你的", name + "的")
			.Replace("你部队", name + "的部队")
			.Replace("你", name);
	}

	private static string RenderPlayerHistoryTextForPrompt(string rawText, string playerDisplayName)
	{
		string playerName = NormalizeLine(playerDisplayName).Replace("玩家", "").Trim();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = BuildPlayerHistoryDisplayName();
		}
		string text = RenderPlayerActionTextForPrompt(rawText, playerName);
		foreach (string alias in BuildPlayerHistoryAnonymousAliases(playerName))
		{
			text = text.Replace(alias, playerName);
		}
		return RenderPlayerNamedReference(text, playerName);
	}

	private static string RenderPlayerNamedReference(string rawText, string playerDisplayName)
	{
		string playerName = NormalizeLine(playerDisplayName).Replace("玩家", "").Trim();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = BuildPlayerHistoryDisplayName();
		}
		string text = StripPlayerInternalMarkers(rawText ?? "");
		try
		{
			string publicDisplayName = NormalizeLine(MyBehavior.BuildPlayerPublicDisplayNameForExternal());
			if (!string.IsNullOrWhiteSpace(publicDisplayName) && !string.Equals(publicDisplayName, playerName, StringComparison.Ordinal))
			{
				text = text.Replace(publicDisplayName, playerName);
			}
		}
		catch
		{
		}
		return text.Replace("玩家", playerName);
	}

	private bool IsLowProfileModeEnabled()
	{
		_state = NormalizeState(_state);
		return _state.LowProfileModeEnabled;
	}

	private void SetLowProfileModeEnabled(bool enabled)
	{
		_state = NormalizeState(_state);
		if (_state.LowProfileModeEnabled == enabled)
		{
			return;
		}
		_state.LowProfileModeEnabled = enabled;
		AbandonOpenNotorietyConversationOutcomes("low_profile_changed");
		_activeConversationStates.Clear();
		LogDebug("low profile mode=" + enabled);
	}

	private static bool IsObserverAllowedDuringLowProfile(Hero observer)
	{
		return IsValidObserver(observer) && IsHeroInPlayerMainParty(observer) && IsPlayerCompanionOrFamilyObserver(observer);
	}

	private static bool IsObserverKeyAllowedDuringLowProfile(string observerKey)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return false;
		}
		Hero observer = FindHeroById(key);
		if (IsValidObserver(observer))
		{
			return IsObserverAllowedDuringLowProfile(observer);
		}
		if (!TryResolveAgentIndexFromObserverKey(key, out int agentIndex))
		{
			return false;
		}
		return IsAgentFromPlayerMainParty(FindMissionAgentByIndex(agentIndex));
	}

	private static bool TryResolveAgentIndexFromObserverKey(string observerKey, out int agentIndex)
	{
		agentIndex = -1;
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		const string marker = "agent:";
		int markerIndex = key.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (markerIndex < 0)
		{
			return false;
		}
		int start = markerIndex + marker.Length;
		int end = start;
		while (end < key.Length && char.IsDigit(key[end]))
		{
			end++;
		}
		if (end <= start)
		{
			return false;
		}
		return int.TryParse(key.Substring(start, end - start), out agentIndex) && agentIndex >= 0;
	}

	private static Agent FindMissionAgentByIndex(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return null;
		}
		try
		{
			return Mission.Current?.Agents?.FirstOrDefault(agent => agent != null && agent.Index == agentIndex);
		}
		catch
		{
			return null;
		}
	}

	private static bool IsAgentFromPlayerMainParty(Agent agent)
	{
		try
		{
			PartyBase party = agent?.Origin?.BattleCombatant as PartyBase;
			return IsStrictPlayerMainPartyBase(party);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsStrictPlayerMainPartyBase(PartyBase party)
	{
		try
		{
			if (party == null)
			{
				return false;
			}
			if (party == PartyBase.MainParty)
			{
				return true;
			}
			MobileParty mobileParty = party.MobileParty;
			return mobileParty != null && (mobileParty == MobileParty.MainParty || mobileParty.IsMainParty);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHeroInPlayerMainParty(Hero hero)
	{
		try
		{
			if (!IsValidObserver(hero))
			{
				return false;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (hero.PartyBelongedTo != null && (hero.PartyBelongedTo == mainParty || hero.PartyBelongedTo.IsMainParty || hero.PartyBelongedTo.Party == PartyBase.MainParty))
			{
				return true;
			}
			return mainParty?.MemberRoster != null && hero.CharacterObject != null && mainParty.MemberRoster.Contains(hero.CharacterObject);
		}
		catch
		{
			return false;
		}
	}

	private bool DoesObserverKnowPlayer(Hero observer)
	{
		if (!IsValidObserver(observer))
		{
			return false;
		}
		if (IsLowProfileModeEnabled() && !IsObserverAllowedDuringLowProfile(observer))
		{
			return false;
		}
		if (IsPlayerCompanionOrFamilyObserver(observer))
		{
			MarkObserverKnowsPlayer(observer, "player_companion_or_family");
			return true;
		}
		if (IsObserverInPlayerOwnedSettlement(observer))
		{
			MarkObserverKnowsPlayer(observer, "player_owned_settlement");
			return true;
		}
		return DoesObserverKnowPlayer(GetHeroId(observer), observer?.Culture?.StringId);
	}

	private bool DoesObserverKnowPlayer(string observerKey, string cultureId)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return false;
		}
		Hero observer = FindHeroById(key);
		if (IsLowProfileModeEnabled())
		{
			if (IsValidObserver(observer))
			{
				if (!IsObserverAllowedDuringLowProfile(observer))
				{
					return false;
				}
			}
			else
			{
				return IsObserverKeyAllowedDuringLowProfile(key);
			}
		}
		if (IsObserverInPlayerOwnedSettlement(observer))
		{
			MarkObserverKnowsPlayer(observer, "player_owned_settlement");
			return true;
		}
		if (IsNonHeroObserverKey(key) && IsObserverKeyInPlayerOwnedSettlement(key))
		{
			return true;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(key, create: true);
		if (state.KnowsMajorHistory)
		{
			return true;
		}
		ActiveConversationState active = GetOrCreateActiveConversation(key, cultureId);
		return active.KnowsMajorThisSession;
	}

	private bool HasObserverUnlockedPlayerMajor(Hero observer)
	{
		if (!IsValidObserver(observer))
		{
			return false;
		}
		if (IsLowProfileModeEnabled() && !IsObserverAllowedDuringLowProfile(observer))
		{
			return false;
		}
		if (IsPlayerCompanionOrFamilyObserver(observer))
		{
			return true;
		}
		if (IsObserverInPlayerOwnedSettlement(observer))
		{
			return true;
		}
		return HasObserverUnlockedPlayerMajor(GetHeroId(observer));
	}

	private bool HasObserverUnlockedPlayerMajor(string observerKey)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return false;
		}
		Hero observer = FindHeroById(key);
		if (IsLowProfileModeEnabled())
		{
			if (IsValidObserver(observer))
			{
				if (!IsObserverAllowedDuringLowProfile(observer))
				{
					return false;
				}
			}
			else
			{
				return IsObserverKeyAllowedDuringLowProfile(key);
			}
		}
		if (IsObserverInPlayerOwnedSettlement(observer))
		{
			return true;
		}
		if (IsNonHeroObserverKey(key) && IsObserverKeyInPlayerOwnedSettlement(key))
		{
			return true;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(key, create: false);
		return state?.KnowsMajorHistory == true
			|| _activeConversationStates.TryGetValue(key, out ActiveConversationState active)
				&& active?.KnowsMajorThisSession == true;
	}

	private void MarkObserverKnowsPlayer(Hero observer, string reason)
	{
		if (!IsValidObserver(observer))
		{
			return;
		}
		if (IsLowProfileModeEnabled() && !IsObserverAllowedDuringLowProfile(observer))
		{
			return;
		}
		MarkObserverKnowsPlayer(GetHeroId(observer), reason);
	}

	private void MarkObserverKnowsPlayer(string observerKey, string reason)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return;
		}
		if (IsLowProfileModeEnabled() && !IsObserverKeyAllowedDuringLowProfile(key))
		{
			return;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(key, create: true);
		if (state == null)
		{
			return;
		}
		bool wasKnown = state.KnowsMajorHistory;
		state.KnowsMajorHistory = true;
		if (state.KnownAtDay < 0)
		{
			state.KnownAtDay = GetCurrentGameDayIndex();
		}
		LogDebug("mark known observer=" + key + " reason=" + (reason ?? "") + " wasKnown=" + wasKnown);
	}

	private ActiveConversationState GetOrCreateActiveConversation(Hero observer)
	{
		return GetOrCreateActiveConversation(GetHeroId(observer), observer?.Culture?.StringId);
	}

	private ActiveConversationState GetOrCreateActiveConversation(string observerKey, string cultureId)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		if (!_activeConversationStates.TryGetValue(key, out ActiveConversationState active) || active == null)
		{
			PlayerNpcKnowledgeState state = GetNpcKnowledgeState(key, create: true);
			int chance = GetEffectiveNotoriety(key, cultureId);
			bool knows = state?.KnowsMajorHistory == true || RollPercent(chance);
			active = new ActiveConversationState
			{
				HeroId = key,
				StartDay = GetCurrentGameDayIndex(),
				StartHour = GetCurrentGameHour(),
				KnownRollChance = chance,
				KnowsMajorThisSession = knows,
				LineCount = 0
			};
			_activeConversationStates[key] = active;
			LogDebug("start known roll observer=" + key + " chance=" + chance + " knows=" + knows);
		}
		return active;
	}

	private bool CanObserverKnowRecentActions(Hero observer, bool courier)
	{
		if (!IsValidObserver(observer))
		{
			return false;
		}
		if (IsLowProfileModeEnabled() && !IsObserverAllowedDuringLowProfile(observer))
		{
			return false;
		}
		return CanObserverKnowRecentActions(GetHeroId(observer), observer?.Culture?.StringId, courier);
	}

	private bool CanObserverKnowRecentActions(string observerKey, string cultureId, bool courier)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return false;
		}
		if (IsLowProfileModeEnabled())
		{
			Hero observer = FindHeroById(key);
			if (IsValidObserver(observer))
			{
				if (!IsObserverAllowedDuringLowProfile(observer))
				{
					return false;
				}
			}
			else if (IsObserverKeyAllowedDuringLowProfile(key))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(key, create: true);
		if (state.KnowsMajorHistory || DoesObserverKnowPlayer(key, cultureId))
		{
			return true;
		}
		if (courier)
		{
			return state.LastCourierSentDistance >= 0f && state.LastCourierSentDistance <= GetCourierRecentDistanceThreshold();
		}
		return state.CompletedConversationSessions >= 1;
	}

	private int GetEffectiveNotoriety(Hero observer)
	{
		if (!IsValidObserver(observer))
		{
			return 0;
		}
		if (IsLowProfileModeEnabled() && !IsObserverAllowedDuringLowProfile(observer))
		{
			return 0;
		}
		return GetEffectiveNotoriety(GetHeroId(observer), observer?.Culture?.StringId);
	}

	private int GetEffectiveNotoriety(string observerKey, string cultureId)
	{
		_state = NormalizeState(_state);
		if (IsLowProfileModeEnabled() && !IsObserverKeyAllowedDuringLowProfile(observerKey))
		{
			return 0;
		}
		string normalizedCultureId = NormalizeCultureId(cultureId);
		double culture = 0.0;
		if (!string.IsNullOrWhiteSpace(normalizedCultureId) && _state.CultureNotoriety.TryGetValue(normalizedCultureId, out double value))
		{
			culture = value;
		}
		PlayerNpcKnowledgeState npcState = GetNpcKnowledgeState(NormalizeObserverKey(observerKey), create: true);
		double total = culture + _state.WorldNotoriety + GetPlayerClanTierBonus() + (npcState?.PersonalKnownBonus ?? 0);
		return ClampPercent(total);
	}

	private int GetCultureNotoriety(string cultureId)
	{
		_state = NormalizeState(_state);
		string normalizedCultureId = NormalizeCultureId(cultureId);
		if (string.IsNullOrWhiteSpace(normalizedCultureId)
			|| !_state.CultureNotoriety.TryGetValue(normalizedCultureId, out double value))
		{
			return 0;
		}
		return ClampPercent(value);
	}

	private PlayerNpcKnowledgeState GetNpcKnowledgeState(Hero observer, bool create)
	{
		return GetNpcKnowledgeState(GetHeroId(observer), create);
	}

	private PlayerNpcKnowledgeState GetNpcKnowledgeState(string observerKey, bool create)
	{
		_state = NormalizeState(_state);
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return null;
		}
		if (!_state.NpcKnowledge.TryGetValue(key, out PlayerNpcKnowledgeState state) || state == null)
		{
			if (!create)
			{
				return null;
			}
			state = new PlayerNpcKnowledgeState
			{
				HeroId = key,
				PersonalKnownBonus = 0,
				LastCourierSentDistance = -1f
			};
			_state.NpcKnowledge[key] = state;
		}
		state.HeroId = key;
		if (state.LastCourierSentDistance < -0.01f)
		{
			state.LastCourierSentDistance = -1f;
		}
		return state;
	}

	private void NoteConversationLine(string heroId)
	{
		string normalizedHeroId = NormalizeObserverKey(heroId);
		if (string.IsNullOrWhiteSpace(normalizedHeroId) || normalizedHeroId == PlayerHeroId)
		{
			return;
		}
		Hero observer = FindHeroById(normalizedHeroId);
		string cultureId = IsValidObserver(observer) ? observer.Culture?.StringId : "";
		ActiveConversationState active = IsValidObserver(observer)
			? GetOrCreateActiveConversation(observer)
			: GetOrCreateActiveConversation(normalizedHeroId, cultureId);
		if (active == null)
		{
			return;
		}
		DowngradeExactNotorietyOutcomeToLegacy(active);
		PublishKnownRollForLegacyLine(active);
		active.HasLegacyLines = true;
		active.LineCount++;
		active.LastDay = GetCurrentGameDayIndex();
		active.LastHour = GetCurrentGameHour();
	}

	private void FinalizeConversation(IEnumerable<CharacterObject> characters)
	{
		if (characters == null)
		{
			FinalizeAllActiveConversations();
			return;
		}
		HashSet<string> heroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CharacterObject character in characters)
		{
			Hero hero = null;
			try
			{
				hero = character?.HeroObject;
			}
			catch
			{
				hero = null;
			}
			string heroId = GetHeroId(hero);
			if (!string.IsNullOrWhiteSpace(heroId))
			{
				heroIds.Add(heroId);
				continue;
			}
			string nonHeroKey = NormalizeObserverKey(character?.StringId);
			if (!string.IsNullOrWhiteSpace(nonHeroKey))
			{
				heroIds.Add("troop:" + nonHeroKey);
			}
		}
		if (heroIds.Count == 0)
		{
			FinalizeAllActiveConversations();
			return;
		}
		foreach (string heroId in heroIds)
		{
			FinalizeConversationByHeroId(heroId);
		}
	}

	private void FinalizeConversation(Hero hero)
	{
		FinalizeConversationByHeroId(GetHeroId(hero));
	}

	private void FinalizeAllActiveConversations()
	{
		foreach (string heroId in _activeConversationStates.Keys.ToList())
		{
			FinalizeConversationByHeroId(heroId);
		}
	}

	private void FinalizeStaleActiveConversations()
	{
		int currentDay = GetCurrentGameDayIndex();
		foreach (ActiveConversationState state in _activeConversationStates.Values.ToList())
		{
			if (state == null || currentDay > state.StartDay)
			{
				FinalizeStaleNotorietyConversationByHeroId(state?.HeroId);
			}
		}
	}

	private void FinalizeConversationByHeroId(string heroId)
	{
		string normalizedHeroId = NormalizeObserverKey(heroId);
		if (string.IsNullOrWhiteSpace(normalizedHeroId))
		{
			return;
		}
		if (!_activeConversationStates.TryGetValue(normalizedHeroId, out ActiveConversationState active) || active == null)
		{
			return;
		}
		if (active.LineCount <= 0)
		{
			// Read-only prompt checks may freeze a roll, but a conversation
			// without a published LLM line is not a completed session.
			_activeConversationStates.Remove(normalizedHeroId);
			return;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(normalizedHeroId, create: true);
		if (state == null)
		{
			return;
		}
		if (TryFinalizeExactNotorietyConversation(
			normalizedHeroId,
			active,
			state,
			out bool exactHandled))
		{
			_activeConversationStates.Remove(normalizedHeroId);
			return;
		}
		if (exactHandled)
		{
			return;
		}
		state.CompletedConversationSessions++;
		if (!state.KnowsMajorHistory && active.LineCount > 0)
		{
			state.PersonalKnownBonus = ClampPercentDouble(state.PersonalKnownBonus + active.LineCount * PersonalKnownBonusPerLine);
		}
		state.LastConversationDay = GetCurrentGameDayIndex();
		_activeConversationStates.Remove(normalizedHeroId);
		LogDebug("finalize conversation observer=" + normalizedHeroId + " lines=" + active.LineCount + " bonus=" + state.PersonalKnownBonus.ToString("0.##"));
	}

	private void NoteCourierSent(Hero recipient)
	{
		if (!IsValidObserver(recipient))
		{
			return;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(recipient, create: true);
		if (state == null)
		{
			return;
		}
		state.LastCourierSentDistance = GetDistanceToHeroParty(recipient);
		state.LastCourierSentDay = GetCurrentGameDayIndex();
		LogDebug("courier sent hero=" + GetHeroId(recipient) + " distance=" + state.LastCourierSentDistance.ToString("0.##"));
	}

	private void AdjustPersonalKnownBonus(Hero hero, int delta, string reason)
	{
		if (!IsValidObserver(hero) || delta == 0)
		{
			return;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(hero, create: true);
		if (state == null || state.KnowsMajorHistory)
		{
			return;
		}
		state.PersonalKnownBonus = ClampPercentDouble(state.PersonalKnownBonus + delta);
		LogDebug("personal bonus hero=" + GetHeroId(hero) + " delta=" + delta + " reason=" + reason + " now=" + state.PersonalKnownBonus.ToString("0.##"));
	}

	private void AddCultureNotoriety(string cultureId, double delta, string reason)
	{
		cultureId = NormalizeCultureId(cultureId);
		delta = ClampDouble(delta, 0.0, 10.0);
		if (string.IsNullOrWhiteSpace(cultureId) || delta <= 0.0)
		{
			return;
		}
		_state = NormalizeState(_state);
		_state.CultureNotoriety.TryGetValue(cultureId, out double current);
		_state.CultureNotoriety[cultureId] = ClampPercentDouble(current + delta);
		AddWorldNotoriety(delta / 3.0, reason + "_world_share");
	}

	private void AddWorldNotoriety(double delta, string reason)
	{
		if (delta <= 0.0)
		{
			return;
		}
		_state.WorldNotoriety = ClampPercentDouble(_state.WorldNotoriety + delta);
		LogDebug("world notoriety +" + delta.ToString("0.##") + " reason=" + reason + " now=" + _state.WorldNotoriety.ToString("0.##"));
	}

	private void PruneRecentActions()
	{
		_state = NormalizeState(_state);
		int minDay = GetCurrentGameDayIndex() - RecentActionWindowDays + 1;
		_state.RecentActions.RemoveAll(x => x == null || x.Day < minDay || string.IsNullOrWhiteSpace(x.Text));
	}

	private void AddActionEntry(List<PlayerActionEntry> list, PlayerActionEntry entry, bool keepRecentWindow, int maxEntries)
	{
		if (list == null || entry == null || string.IsNullOrWhiteSpace(entry.Text))
		{
			return;
		}
		if (list.Any(x => x != null && x.Day == entry.Day && (string.Equals(x.StableKey ?? "", entry.StableKey ?? "", StringComparison.OrdinalIgnoreCase) || string.Equals((x.Text ?? "").Trim(), entry.Text.Trim(), StringComparison.Ordinal))))
		{
			return;
		}
		list.Add(entry);
		if (keepRecentWindow)
		{
			int minDay = GetCurrentGameDayIndex() - RecentActionWindowDays + 1;
			list.RemoveAll(x => x == null || x.Day < minDay || string.IsNullOrWhiteSpace(x.Text));
		}
		if (maxEntries > 0 && list.Count > maxEntries)
		{
			list.Sort((a, b) => CompareActionEntry(a, b));
			list.RemoveRange(0, list.Count - maxEntries);
		}
		list.Sort((a, b) => CompareActionEntry(a, b));
	}

	private static int CompareActionEntry(PlayerActionEntry a, PlayerActionEntry b)
	{
		int day = (a?.Day ?? 0).CompareTo(b?.Day ?? 0);
		if (day != 0)
		{
			return day;
		}
		int seq = (a?.Sequence ?? 0).CompareTo(b?.Sequence ?? 0);
		if (seq != 0)
		{
			return seq;
		}
		return (a?.Order ?? 0).CompareTo(b?.Order ?? 0);
	}

	private PlayerNotorietyPopupData BuildPlayerNotorietyPopupData(bool canEdit)
	{
		_state = NormalizeState(_state);
		double world = ClampPercentDouble(_state.WorldNotoriety);
		float effectiveWorld = (float)ClampPercentDouble(world + GetPlayerClanTierBonus());
		return new PlayerNotorietyPopupData
		{
			HistoryText = BuildPlayerNotorietyHistoryText(includeRawMaterials: canEdit),
			CenturyText = "",
			WorldFillPercent = effectiveWorld,
			ShowEditButton = canEdit,
			EditText = "编辑履历",
			IsLowProfileModeEnabled = IsLowProfileModeEnabled(),
			LowProfileToggleText = IsLowProfileModeEnabled() ? "关闭低调模式" : "开启低调模式",
			CultureRows = BuildPlayerNotorietyCultureRows()
		};
	}

	private string BuildPlayerNotorietyHistoryText(bool includeRawMaterials)
	{
		_state = NormalizeState(_state);
		StringBuilder sb = new StringBuilder();
		if (IsLowProfileModeEnabled())
		{
			sb.AppendLine("【低调模式】已开启。除当前主队伍内的士兵和同伴外，其他人暂时不会认出玩家；低调期间的新行为仍会进入重大履历、近期行动和周报素材。");
			sb.AppendLine();
		}
		string playerName = "玩家";
		string summary = RenderPlayerHistoryTextForPrompt((_state.MajorSummary ?? "").Trim(), BuildPlayerHistoryDisplayName());
		if (!string.IsNullOrWhiteSpace(summary))
		{
			sb.AppendLine(summary);
		}
		PruneRecentActions();
		List<PlayerActionEntry> recentActions = (_state.RecentActions ?? new List<PlayerActionEntry>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.Sequence)
			.ThenByDescending(x => x.Order)
			.ToList();
		if (recentActions.Count > 0)
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine("【近期行动】");
			foreach (PlayerActionEntry entry in recentActions)
			{
				sb.AppendLine("- " + (string.IsNullOrWhiteSpace(entry.GameDate) ? ("第" + entry.Day + "日") : entry.GameDate.Trim()) + "：" + RenderPlayerActionTextForPrompt(entry.Text, playerName));
			}
		}
		string text = sb.ToString().Trim();
		return string.IsNullOrWhiteSpace(text) ? "尚无可展示的公开履历。" : text;
	}

	private PlayerNotorietyCultureRowData[] BuildPlayerNotorietyCultureRows()
	{
		_state = NormalizeState(_state);
		List<PlayerNotorietyCultureRowData> rows = new List<PlayerNotorietyCultureRowData>();
		HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CultureObject culture in GetCulturesForNotorietyPopup())
		{
			string id = NormalizeCultureId(culture?.StringId);
			if (string.IsNullOrWhiteSpace(id) || !added.Add(id))
			{
				continue;
			}
			_state.CultureNotoriety.TryGetValue(id, out double value);
			rows.Add(new PlayerNotorietyCultureRowData
			{
				CultureId = id,
				CultureName = ResolveCultureDisplayName(culture, id),
				FillPercent = (float)ClampPercentDouble(value),
				FillColor = ResolveCultureFillColor(culture)
			});
		}
		foreach (KeyValuePair<string, double> pair in _state.CultureNotoriety.OrderBy(x => ResolveCultureDisplayName(x.Key), StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
		{
			string id = NormalizeCultureId(pair.Key);
			if (string.IsNullOrWhiteSpace(id) || !added.Add(id))
			{
				continue;
			}
			rows.Add(new PlayerNotorietyCultureRowData
			{
				CultureId = id,
				CultureName = ResolveCultureDisplayName(id),
				FillPercent = (float)ClampPercentDouble(pair.Value),
				FillColor = Color.FromUint(0xFF8F6E3Bu)
			});
		}
		return rows.ToArray();
	}

	private static List<CultureObject> GetCulturesForNotorietyPopup()
	{
		try
		{
			return TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObjectTypeList<CultureObject>()
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.StringId))
				.GroupBy(x => NormalizeCultureId(x.StringId), StringComparer.OrdinalIgnoreCase)
				.Select(x => x.First())
				.OrderBy(x => GetCultureDisplayOrder(NormalizeCultureId(x.StringId)))
				.ThenBy(x => ResolveCultureDisplayName(x, NormalizeCultureId(x.StringId)), StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => NormalizeCultureId(x.StringId), StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return new List<CultureObject>();
		}
	}

	private static int GetCultureDisplayOrder(string cultureId)
	{
		string id = NormalizeCultureId(cultureId);
		for (int i = 0; i < CultureDisplayOrder.Length; i++)
		{
			if (string.Equals(id, CultureDisplayOrder[i], StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return int.MaxValue;
	}

	private static string ResolveCultureDisplayName(CultureObject culture, string fallbackId)
	{
		string name = culture?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(name))
		{
			return name.Trim();
		}
		string id = NormalizeCultureId(fallbackId);
		return string.IsNullOrWhiteSpace(id) ? "未知文化" : id;
	}

	private static Color ResolveCultureFillColor(CultureObject culture)
	{
		try
		{
			return Color.FromUint(NormalizeUiColor(culture?.Color ?? 0u));
		}
		catch
		{
			return Color.FromUint(0xFF8F6E3Bu);
		}
	}

	private static uint NormalizeUiColor(uint color)
	{
		if ((color & 0x00FFFFFFu) == 0u)
		{
			return 0xFF8F6E3Bu;
		}
		if ((color & 0xFF000000u) == 0u)
		{
			color |= 0xFF000000u;
		}
		return color;
	}

	private static string ResolveCampaignCenturyText()
	{
		try
		{
			int year = Math.Max(1, CampaignTime.Now.GetYear);
			return ((year + 99) / 100).ToString() + "世纪";
		}
		catch
		{
			return "";
		}
	}

	private string BuildPlayerNotorietyDisplayText(bool includeRawMaterials)
	{
		_state = NormalizeState(_state);
		StringBuilder sb = new StringBuilder();
		double world = ClampPercentDouble(_state.WorldNotoriety);
		int clanTierBonus = GetPlayerClanTierBonus();
		double effectiveWorld = ClampPercentDouble(world + clanTierBonus);
		sb.AppendLine("【玩家知名度】");
		sb.AppendLine("世界知名度：" + FormatScore(effectiveWorld) + "/100（" + GetLevelText(effectiveWorld) + "；基础 " + FormatScore(world) + " + 家族修正 " + clanTierBonus + "）");
		sb.AppendLine("家族等级修正：+" + clanTierBonus);
		sb.AppendLine();
		sb.AppendLine("【文化知名度】");
		if (_state.CultureNotoriety.Count == 0)
		{
			sb.AppendLine("（暂无文化知名度）");
		}
		else
		{
			foreach (KeyValuePair<string, double> pair in _state.CultureNotoriety.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
			{
				double effective = ClampPercentDouble(pair.Value + world + clanTierBonus);
				sb.AppendLine("- " + ResolveCultureDisplayName(pair.Key) + "：" + FormatScore(pair.Value) + "/100；有效 " + FormatScore(effective) + "/100（" + GetLevelText(effective) + "）");
			}
		}
		string history = BuildMajorHistoryForPrompt(BuildPlayerHistoryDisplayName());
		if (!string.IsNullOrWhiteSpace(history))
		{
			sb.AppendLine();
			sb.AppendLine("【玩家履历】");
			sb.AppendLine(history);
		}
		PruneRecentActions();
		if (_state.RecentActions.Count > 0)
		{
			sb.AppendLine();
			sb.AppendLine("【玩家近期行动】");
			foreach (PlayerActionEntry entry in _state.RecentActions.OrderByDescending(x => x.Day).ThenByDescending(x => x.Sequence).Take(20))
			{
				sb.AppendLine("- " + (string.IsNullOrWhiteSpace(entry.GameDate) ? ("第" + entry.Day + "日") : entry.GameDate.Trim()) + "：" + RenderPlayerActionTextForPrompt(entry.Text, "玩家"));
			}
		}
		if (includeRawMaterials)
		{
			List<PlayerHistoryMaterial> pending = _state.MajorMaterials.Where(x => x != null && !x.Summarized).OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
			sb.AppendLine();
			sb.AppendLine("【未总结素材】");
			if (pending.Count == 0)
			{
				sb.AppendLine("（无）");
			}
			else
			{
				foreach (PlayerHistoryMaterial material in pending.Take(30))
				{
					sb.AppendLine("- " + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "：" + RenderPlayerHistoryTextForPrompt(material.Text, BuildPlayerHistoryDisplayName()));
				}
			}
		}
		return sb.ToString().Trim();
	}

	private void OpenPlayerNotorietyView()
	{
		bool canEdit = MyBehavior.IsDevDataManagementEnabledForExternal();
		if (PlayerNotorietyPopup.Show(BuildPlayerNotorietyPopupData(canEdit), canEdit ? OpenPlayerMajorHistoryEditor : null, ToggleLowProfileModeFromPopup))
		{
			return;
		}
		string text = BuildPlayerNotorietyHistoryText(includeRawMaterials: canEdit);
		if (canEdit)
		{
			InformationManager.ShowInquiry(new InquiryData("玩家知名度与履历", text, true, true, "编辑履历", "关闭", OpenPlayerMajorHistoryEditor, null));
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("玩家知名度与履历", text, true, false, "关闭", "", null, null));
	}

	private PlayerNotorietyPopupData ToggleLowProfileModeFromPopup()
	{
		bool enabled = !IsLowProfileModeEnabled();
		SetLowProfileModeEnabled(enabled);
		InformationManager.DisplayMessage(new InformationMessage(enabled ? "已开启低调模式。" : "已关闭低调模式。"));
		return BuildPlayerNotorietyPopupData(MyBehavior.IsDevDataManagementEnabledForExternal());
	}

	private void OpenPlayerMajorHistoryEditor()
	{
		try
		{
			if (!MyBehavior.IsDevDataManagementEnabledForExternal())
			{
				InformationManager.DisplayMessage(new InformationMessage("开发者数据管理未开启（请在 MCM 中启用）。"));
				OpenPlayerNotorietyView();
				return;
			}
			_state = NormalizeState(_state);
			string initialText = (_state.MajorSummary ?? "").Trim();
			string subtitle = "这里编辑的是已总结玩家重大履历摘要；未总结素材、近期行动和知名度数值不会被修改。";
			string hint = "请输入新的玩家履历摘要；留空=清空已总结履历。未总结素材仍会保留，并可在后续总结中重新融合。";
			DevTextEditorHelper.ShowLongTextEditor("编辑玩家履历", subtitle, hint, initialText, delegate(string input)
			{
				ApplyPlayerMajorHistoryEditorInput(input);
				OpenPlayerNotorietyView();
			}, delegate
			{
				OpenPlayerNotorietyView();
			}, "保存", "返回");
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "open player major history editor failed: " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("打开玩家履历编辑器失败：" + ex.Message));
		}
	}

	private void ApplyPlayerMajorHistoryEditorInput(string input)
	{
		try
		{
			_state = NormalizeState(_state);
			_state.MajorSummary = NormalizeEditableMajorHistoryText(input);
			_state.LastSummaryDay = GetCurrentGameDayIndex();
			_state.SummaryRetryCount = 0;
			_state.LastSummaryError = "";
			_state.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(_state.MajorSummary) ? "已清空玩家履历。" : "玩家履历已更新。"));
			LogDebug("manual major summary edit chars=" + (_state.MajorSummary?.Length ?? 0));
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "apply player major history editor input failed: " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("保存玩家履历失败：" + ex.Message));
		}
	}

	private static string NormalizeEditableMajorHistoryText(string input)
	{
		return RenderPlayerHistoryTextForPrompt((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), BuildPlayerHistoryDisplayName());
	}

	private static string NormalizeMajorSummaryForStorage(string input)
	{
		return TrimMajorSummaryFallback(NormalizeLine(input), GetMajorPromptChars());
	}

	private static string BuildFallbackMajorSummary(string existingSummary, List<PlayerHistoryMaterial> materials, string playerDisplayName)
	{
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		StringBuilder sb = new StringBuilder();
		string existing = RenderPlayerHistoryTextForPrompt(existingSummary, playerName);
		if (!string.IsNullOrWhiteSpace(existing))
		{
			sb.Append(existing);
		}
		foreach (PlayerHistoryMaterial material in materials ?? new List<PlayerHistoryMaterial>())
		{
			string text = RenderPlayerHistoryTextForPrompt(material?.Text, playerName);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (sb.Length > 0)
			{
				sb.Append(" ");
			}
			sb.Append(string.IsNullOrWhiteSpace(material.GameDate) ? "" : (material.GameDate.Trim() + "："));
			sb.Append(text);
		}
		return NormalizeMajorSummaryForStorage(sb.ToString());
	}

	private bool IsMajorSummaryOverPromptLimit()
	{
		int maxChars = GetMajorPromptChars();
		string summary = (_state?.MajorSummary ?? "").Trim();
		return maxChars > 0 && summary.Length > maxChars;
	}

	private static string TrimMajorSummaryFallback(string input, int maxChars)
	{
		string text = NormalizeLine(input);
		if (maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		if (maxChars <= 3)
		{
			return text.Substring(0, maxChars);
		}
		return text.Substring(0, maxChars - 3).TrimEnd() + "...";
	}

	private void RememberSummarizedMaterialKey(PlayerHistoryMaterial material)
	{
		if (material == null)
		{
			return;
		}
		RememberSummarizedMaterialKey(NormalizeStableKey(material.StableKey, material.Text, material.Day));
	}

	private void RememberSummarizedMaterialKey(string key)
	{
		_state = NormalizeState(_state);
		RememberSummarizedMaterialKey(_state, key);
	}

	private static void RememberSummarizedMaterialKey(PlayerNotorietyState state, string key)
	{
		if (state == null)
		{
			return;
		}
		string normalizedKey = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalizedKey))
		{
			return;
		}
		state.SummarizedMaterialKeys = NormalizeSummarizedMaterialKeys(state.SummarizedMaterialKeys);
		state.SummarizedMaterialKeys.RemoveAll(x => string.Equals(x ?? "", normalizedKey, StringComparison.OrdinalIgnoreCase));
		state.SummarizedMaterialKeys.Add(normalizedKey);
		if (state.SummarizedMaterialKeys.Count > MaxSummarizedMaterialKeys)
		{
			state.SummarizedMaterialKeys = state.SummarizedMaterialKeys
				.Skip(state.SummarizedMaterialKeys.Count - MaxSummarizedMaterialKeys)
				.ToList();
		}
	}

	private void PruneSummarizedMajorMaterials()
	{
		_state = PruneSummarizedMajorMaterials(NormalizeState(_state));
	}

	private static PlayerNotorietyState PruneSummarizedMajorMaterials(PlayerNotorietyState state)
	{
		if (state?.MajorMaterials == null)
		{
			return state ?? new PlayerNotorietyState();
		}
		foreach (PlayerHistoryMaterial material in state.MajorMaterials)
		{
			if (material?.Summarized == true)
			{
				RememberSummarizedMaterialKey(state, NormalizeStableKey(material.StableKey, material.Text, material.Day));
			}
		}
		state.MajorMaterials = state.MajorMaterials
			.Where(x => x != null && !x.Summarized && !string.IsNullOrWhiteSpace(x.Text))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.Take(MaxMajorMaterials)
			.ToList();
		state.SummarizedMaterialKeys = NormalizeSummarizedMaterialKeys(state.SummarizedMaterialKeys);
		return state;
	}

	private static List<string> NormalizeSummarizedMaterialKeys(IEnumerable<string> keys)
	{
		List<string> normalized = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string rawKey in keys ?? Enumerable.Empty<string>())
		{
			string key = (rawKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
			{
				continue;
			}
			normalized.Add(key);
		}
		if (normalized.Count > MaxSummarizedMaterialKeys)
		{
			normalized = normalized
				.Skip(normalized.Count - MaxSummarizedMaterialKeys)
				.ToList();
		}
		return normalized;
	}

	private static PlayerNotorietyState NormalizeState(PlayerNotorietyState state)
	{
		state ??= new PlayerNotorietyState();
		bool repairLegacyVillageRaidDefense = !state.LegacyVillageRaidDefenseRepairApplied;
		state.CultureNotoriety ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
		state.NpcKnowledge ??= new Dictionary<string, PlayerNpcKnowledgeState>(StringComparer.OrdinalIgnoreCase);
		state.ConversationOutcomeReceipts ??= new Dictionary<string, string>(StringComparer.Ordinal);
		state.RecentActions ??= new List<PlayerActionEntry>();
		state.MajorMaterials ??= new List<PlayerHistoryMaterial>();
		state.SummarizedMaterialKeys = NormalizeSummarizedMaterialKeys(state.SummarizedMaterialKeys);
		state.MajorSummary = (state.MajorSummary ?? "").Trim();
		if (state.LastSummaryDay == 0 && state.UpdatedUtcTicks == 0 && string.IsNullOrWhiteSpace(state.MajorSummary))
		{
			state.LastSummaryDay = -1;
		}
		state.WorldNotoriety = ClampPercentDouble(state.WorldNotoriety);
		state.CultureNotoriety = state.CultureNotoriety
			.Where(x => !string.IsNullOrWhiteSpace(x.Key))
			.ToDictionary(x => NormalizeCultureId(x.Key), x => ClampPercentDouble(x.Value), StringComparer.OrdinalIgnoreCase);
		state.NpcKnowledge = state.NpcKnowledge
			.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value != null)
			.ToDictionary(x => NormalizeHeroId(x.Key), x =>
			{
				x.Value.HeroId = NormalizeHeroId(x.Value.HeroId);
				x.Value.PersonalKnownBonus = ClampPercentDouble(x.Value.PersonalKnownBonus);
				if (x.Value.LastCourierSentDistance < -0.01f)
				{
					x.Value.LastCourierSentDistance = -1f;
				}
				return x.Value;
			}, StringComparer.OrdinalIgnoreCase);
		state.RecentActions = state.RecentActions
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
			.Select(x => NormalizeActionEntry(x, repairLegacyVillageRaidDefense))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.Sequence)
			.Take(MaxRecentActions)
			.ToList();
		state.MajorMaterials = state.MajorMaterials
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
			.Select(x => NormalizeHistoryMaterial(x, repairLegacyVillageRaidDefense))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ToList();
		state.LegacyVillageRaidDefenseRepairApplied = true;
		return PruneSummarizedMajorMaterials(state);
	}

	private static PlayerActionEntry NormalizeActionEntry(PlayerActionEntry entry, bool repairLegacyVillageRaidDefense)
	{
		entry.Text = NormalizeLine(entry.Text);
		entry.StableKey = NormalizeStableKey(entry.StableKey, entry.Text, entry.Day);
		entry.ActionKind = (entry.ActionKind ?? "").Trim();
		entry.GameDate = (entry.GameDate ?? "").Trim();
		entry.SettlementId = (entry.SettlementId ?? "").Trim();
		entry.SettlementName = (entry.SettlementName ?? "").Trim();
		entry.LocationText = (entry.LocationText ?? "").Trim();
		entry.ActorCultureId = NormalizeCultureId(entry.ActorCultureId);
		entry.TargetCultureId = NormalizeCultureId(entry.TargetCultureId);
		entry.SettlementCultureId = NormalizeCultureId(entry.SettlementCultureId);
		if (repairLegacyVillageRaidDefense)
		{
			entry.Text = RepairLegacyVillageRaidDefensePlayerActionText(entry);
		}
		return entry;
	}

	private static PlayerHistoryMaterial NormalizeHistoryMaterial(PlayerHistoryMaterial material, bool repairLegacyVillageRaidDefense)
	{
		material.Text = NormalizeLine(material.Text);
		material.StableKey = NormalizeStableKey(material.StableKey, material.Text, material.Day);
		material.SourceKind = (material.SourceKind ?? "").Trim();
		material.GameDate = (material.GameDate ?? "").Trim();
		material.CultureIds = NormalizeCultureList(material.CultureIds);
		if (repairLegacyVillageRaidDefense)
		{
			material.Text = RepairLegacyVillageRaidDefenseHistoryMaterialText(material);
		}
		return material;
	}

	private static string RepairLegacyVillageRaidDefensePlayerActionText(PlayerActionEntry entry)
	{
		string text = NormalizeLine(entry?.Text);
		if (!IsLegacyVillageRaidDefenseRecord(entry?.ActionKind, entry?.StableKey, text, out bool isAftermath))
		{
			return text;
		}
		string place = ResolveLegacyVillageRaidDefenseLocation(entry?.LocationText, entry?.SettlementName);
		return BuildLegacyVillageRaidDefenseText(text, place, entry?.Won, isAftermath);
	}

	private static string RepairLegacyVillageRaidDefenseHistoryMaterialText(PlayerHistoryMaterial material)
	{
		string text = NormalizeLine(material?.Text);
		if (!IsLegacyVillageRaidDefenseRecord(material?.SourceKind, material?.StableKey, text, out bool isAftermath))
		{
			return text;
		}
		return BuildLegacyVillageRaidDefenseText(text, "当地村庄", InferLegacyVillageRaidDefenseOutcome(text), isAftermath);
	}

	private static bool IsLegacyVillageRaidDefenseRecord(string sourceKind, string stableKey, string text, out bool isAftermath)
	{
		isAftermath = false;
		string key = (stableKey ?? "").Trim();
		if (key.IndexOf(":side:defender:hero:", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}
		string kind = (sourceKind ?? "").Trim();
		isAftermath = string.Equals(kind, "map_event_aftermath", StringComparison.OrdinalIgnoreCase)
			|| key.StartsWith("mapevent_aftermath:", StringComparison.OrdinalIgnoreCase);
		bool isMapEvent = string.Equals(kind, "map_event", StringComparison.OrdinalIgnoreCase)
			|| key.StartsWith("mapevent:", StringComparison.OrdinalIgnoreCase);
		if (!isMapEvent && !isAftermath)
		{
			return false;
		}
		if (isAftermath)
		{
			return text.IndexOf("的袭掠已经结束", StringComparison.OrdinalIgnoreCase) >= 0
				&& (text.IndexOf("清点缴获", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("收拢部队并处理残局", StringComparison.OrdinalIgnoreCase) >= 0);
		}
		return text.IndexOf("发动的袭掠中", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string ResolveLegacyVillageRaidDefenseLocation(string locationText, string settlementName)
	{
		string place = NormalizeLine(locationText);
		if (string.IsNullOrWhiteSpace(place))
		{
			place = NormalizeLine(settlementName);
		}
		return string.IsNullOrWhiteSpace(place) ? "当地村庄" : place;
	}

	private static bool? InferLegacyVillageRaidDefenseOutcome(string text)
	{
		if ((text ?? "").IndexOf("袭掠中得手", StringComparison.OrdinalIgnoreCase) >= 0
			|| (text ?? "").IndexOf("清点缴获", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if ((text ?? "").IndexOf("袭掠中失利", StringComparison.OrdinalIgnoreCase) >= 0
			|| (text ?? "").IndexOf("收拢部队并处理残局", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		return null;
	}

	private static string BuildLegacyVillageRaidDefenseText(string originalText, string place, bool? won, bool isAftermath)
	{
		string corrected = isAftermath
			? (won == true
				? (place + "的袭掠已经结束，你协助守军击退了袭掠者，正在整顿部队。")
				: (won == false
					? (place + "的袭掠已经结束，袭掠者已经得手；你正在收拢部队并处理残局。")
					: (place + "的袭掠已经结束，你正在协助守军整顿部队。")))
			: (won == true
				? ("你在" + place + "参与村庄保卫战，击退了袭掠者。")
				: (won == false
					? ("你在" + place + "参与村庄保卫战时失利，未能阻止袭掠者。")
					: ("你在" + place + "参与了村庄保卫战。")));
		int sentenceEnd = (originalText ?? "").IndexOf('。');
		if (sentenceEnd < 0 || sentenceEnd >= originalText.Length - 1)
		{
			return corrected;
		}
		string detail = originalText.Substring(sentenceEnd + 1).Trim();
		return string.IsNullOrWhiteSpace(detail) ? corrected : (corrected + " " + detail);
	}

	private static bool ShouldRecordPlayerHeroPrisonerRelease(EndCaptivityDetail detail)
	{
		return detail == EndCaptivityDetail.ReleasedByChoice || detail == EndCaptivityDetail.Ransom || detail == EndCaptivityDetail.ReleasedByCompensation;
	}

	private static string BuildPlayerHeroPrisonerReleaseVerb(EndCaptivityDetail detail)
	{
		switch (detail)
		{
		case EndCaptivityDetail.Ransom:
			return "接受赎金释放了英雄俘虏";
		case EndCaptivityDetail.ReleasedByCompensation:
			return "通过补偿协议释放了英雄俘虏";
		case EndCaptivityDetail.ReleasedByChoice:
			return "主动释放了英雄俘虏";
		default:
			return "";
		}
	}

	private static string BuildMainHeroReleasedText(PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail)
	{
		string source = BuildPartyDisplayName(party);
		if (string.IsNullOrWhiteSpace(source))
		{
			source = capturerFaction?.Name?.ToString();
		}
		string sourceSuffix = string.IsNullOrWhiteSpace(source) ? "" : ("，脱离了" + source.Trim() + "的囚禁");
		switch (detail)
		{
		case EndCaptivityDetail.Ransom:
			return "你被赎金赎回并结束了俘虏状态" + sourceSuffix + "。";
		case EndCaptivityDetail.ReleasedAfterPeace:
			return "你因和平协议获释" + sourceSuffix + "。";
		case EndCaptivityDetail.ReleasedAfterBattle:
			return "你在战后获释" + sourceSuffix + "。";
		case EndCaptivityDetail.ReleasedAfterEscape:
			return "你成功逃脱囚禁" + sourceSuffix + "。";
		case EndCaptivityDetail.ReleasedByCompensation:
			return "你因补偿协议获释" + sourceSuffix + "。";
		case EndCaptivityDetail.ReleasedByChoice:
			return "你被释放并结束了俘虏状态" + sourceSuffix + "。";
		default:
			return "";
		}
	}

	private static PrisonerRosterSummary BuildFlattenedPrisonerRosterSummary(FlattenedTroopRoster roster, bool includeHeroes)
	{
		List<PrisonerRosterCountEntry> entries = new List<PrisonerRosterCountEntry>();
		if (roster != null)
		{
			foreach (FlattenedTroopRosterElement element in roster)
			{
				AddPrisonerRosterCount(entries, element.Troop, 1, includeHeroes);
			}
		}
		return BuildPrisonerRosterSummary(entries);
	}

	private static PrisonerRosterSummary BuildTroopRosterSummary(TroopRoster roster, bool includeHeroes)
	{
		List<PrisonerRosterCountEntry> entries = new List<PrisonerRosterCountEntry>();
		if (roster != null)
		{
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				AddPrisonerRosterCount(entries, element.Character, element.Number, includeHeroes);
			}
		}
		return BuildPrisonerRosterSummary(entries);
	}

	private static void AddPrisonerRosterCount(List<PrisonerRosterCountEntry> entries, CharacterObject character, int count, bool includeHeroes)
	{
		if (entries == null || character == null || count <= 0 || (!includeHeroes && character.IsHero))
		{
			return;
		}
		string key = (character.StringId ?? character.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			key = character.Name?.ToString() ?? "unknown";
		}
		PrisonerRosterCountEntry entry = entries.FirstOrDefault(x => x != null && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
		if (entry == null)
		{
			entry = new PrisonerRosterCountEntry
			{
				Key = key,
				Character = character
			};
			entries.Add(entry);
		}
		entry.Count += count;
	}

	private static PrisonerRosterSummary BuildPrisonerRosterSummary(List<PrisonerRosterCountEntry> entries)
	{
		List<PrisonerRosterCountEntry> ordered = (entries ?? new List<PrisonerRosterCountEntry>())
			.Where(x => x != null && x.Count > 0 && x.Character != null)
			.OrderByDescending(x => x.Count)
			.ThenBy(x => GetCharacterDisplayName(x.Character), StringComparer.OrdinalIgnoreCase)
			.ToList();
		PrisonerRosterSummary summary = new PrisonerRosterSummary();
		summary.TotalCount = ordered.Sum(x => Math.Max(0, x.Count));
		summary.HeroCount = ordered.Where(x => x.Character.IsHero).Sum(x => Math.Max(0, x.Count));
		summary.RegularCount = Math.Max(0, summary.TotalCount - summary.HeroCount);
		summary.PrimaryCultureId = ordered.Select(x => x.Character?.Culture?.StringId ?? "").FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
		summary.Signature = string.Join("|", ordered.Select(x => (x.Character?.StringId ?? GetCharacterDisplayName(x.Character)) + ":" + x.Count));
		List<string> parts = ordered.Take(3).Select(x => x.Count + " 名 " + GetCharacterDisplayName(x.Character)).ToList();
		if (ordered.Count > 3)
		{
			parts.Add("等");
		}
		summary.DetailText = string.Join("、", parts);
		return summary;
	}

	private static string BuildRosterDetailSuffix(PrisonerRosterSummary summary)
	{
		if (summary == null || string.IsNullOrWhiteSpace(summary.DetailText))
		{
			return "";
		}
		return "（" + summary.DetailText.Trim() + "）";
	}

	private static bool IsPlayerPartyBase(PartyBase party)
	{
		try
		{
			if (party == null)
			{
				return false;
			}
			if (party == PartyBase.MainParty)
			{
				return true;
			}
			if (IsPlayerMobileParty(party.MobileParty))
			{
				return true;
			}
			return party.LeaderHero == Hero.MainHero || party.Owner == Hero.MainHero;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerMobileParty(MobileParty party)
	{
		try
		{
			return party != null && (party == MobileParty.MainParty || party.IsMainParty || party.LeaderHero == Hero.MainHero);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerClanHero(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return false;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			return hero == Hero.MainHero || hero.IsPlayerCompanion || (playerClan != null && (hero.Clan == playerClan || hero.CompanionOf == playerClan));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerCompanionOrFamilyObserver(Hero hero)
	{
		try
		{
			if (!IsValidObserver(hero))
			{
				return false;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			return hero.IsPlayerCompanion || (playerClan != null && (hero.Clan == playerClan || hero.CompanionOf == playerClan));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsObserverInPlayerOwnedSettlement(Hero hero)
	{
		try
		{
			if (!IsValidObserver(hero))
			{
				return false;
			}
			if (IsPlayerOwnedSettlement(ResolveObserverSettlement(hero)))
			{
				return true;
			}
			return hero == Hero.OneToOneConversationHero && IsPlayerOwnedSettlement(ResolveCurrentInteractionSettlement());
		}
		catch
		{
			return false;
		}
	}

	private static Settlement ResolveObserverSettlement(Hero hero)
	{
		try
		{
			return hero?.CurrentSettlement
				?? hero?.StayingInSettlement
				?? hero?.PartyBelongedTo?.CurrentSettlement
				?? hero?.PartyBelongedToAsPrisoner?.MobileParty?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsObserverKeyInPlayerOwnedSettlement(string observerKey)
	{
		try
		{
			return IsPlayerOwnedSettlement(ResolveObserverKeySettlement(observerKey) ?? ResolveCurrentInteractionSettlement());
		}
		catch
		{
			return false;
		}
	}

	private static Settlement ResolveObserverKeySettlement(string observerKey)
	{
		string key = NormalizeObserverKey(observerKey);
		int at = key.LastIndexOf('@');
		if (at < 0 || at >= key.Length - 1)
		{
			return null;
		}
		string settlementId = key.Substring(at + 1).Trim();
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return null;
		}
		try
		{
			return Settlement.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), settlementId, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveCurrentInteractionSettlement()
	{
		try
		{
			return Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement ?? Hero.MainHero?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsPlayerOwnedSettlement(Settlement settlement)
	{
		try
		{
			if (settlement == null || settlement.IsHideout)
			{
				return false;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (playerClan == null)
			{
				return false;
			}
			Clan ownerClan = settlement.OwnerClan;
			return ownerClan == playerClan || ownerClan?.Leader == Hero.MainHero;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNonHeroObserverKey(string observerKey)
	{
		string key = NormalizeObserverKey(observerKey);
		return key.StartsWith("agent:", StringComparison.OrdinalIgnoreCase) || key.StartsWith("troop:", StringComparison.OrdinalIgnoreCase);
	}

	private static Settlement ResolvePlayerCurrentSettlement()
	{
		try
		{
			return Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement ?? Hero.MainHero?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static string BuildPartyDisplayName(PartyBase party)
	{
		try
		{
			string text = party?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
			text = party?.LeaderHero?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
			text = party?.Settlement?.Name?.ToString();
			return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetAlleyDisplayName(Alley alley)
	{
		string text = alley?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? "一处街巷" : text.Trim();
	}

	private static string BuildPartyScope(PartyBase party)
	{
		try
		{
			if (party == null)
			{
				return "";
			}
			return (party.MobileParty?.StringId ?? party.Settlement?.StringId ?? party.LeaderHero?.StringId ?? BuildPartyDisplayName(party) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetHeroDisplayName(Hero hero)
	{
		string text = hero?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? "未知英雄" : text.Trim();
	}

	private static string GetCharacterDisplayName(CharacterObject character)
	{
		string text = character?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? ((character?.StringId ?? "未知兵种").Trim()) : text.Trim();
	}

	private static string GetSettlementDisplayName(Settlement settlement)
	{
		string text = settlement?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? "当前地点" : text.Trim();
	}

	private static string GetKingdomId(Kingdom kingdom)
	{
		return (kingdom?.StringId ?? "").Trim();
	}

	private static string GetFactionId(IFaction faction)
	{
		return (faction?.StringId ?? faction?.Name?.ToString() ?? "").Trim();
	}

	private static string GetFactionDisplayName(IFaction faction, string fallback)
	{
		string text = "";
		try
		{
			text = faction?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = faction?.InformalName?.ToString();
			}
		}
		catch
		{
			text = "";
		}
		return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
	}

	private static string BuildPrisonerDonationSkipKey(Settlement settlement, string signature)
	{
		return GetCurrentGameDayIndex() + ":" + (settlement?.StringId ?? "") + ":" + ((signature ?? "").Trim());
	}

	private static double GetCurrentGameTimeDays()
	{
		try
		{
			return Math.Max(0.0, CampaignTime.Now.ToDays);
		}
		catch
		{
			return 0.0;
		}
	}

	private void ClearCurrentSettlementStayTracking()
	{
		_currentSettlementStayId = "";
		_currentSettlementStayName = "";
		_currentSettlementStayStartDays = -1.0;
		_currentSettlementStayStartDay = -1;
	}

	private static string FormatStayDuration(double stayHours)
	{
		if (stayHours >= 48.0)
		{
			return Math.Max(1, (int)Math.Round(stayHours / 24.0)) + " 天";
		}
		if (stayHours >= 20.0)
		{
			return "1 天";
		}
		return Math.Max(1, (int)Math.Round(stayHours)) + " 小时";
	}

	private static string GetBoardGameResultText(BoardGameHelper.BoardGameState state)
	{
		switch (state)
		{
		case BoardGameHelper.BoardGameState.Win:
			return "获胜";
		case BoardGameHelper.BoardGameState.Loss:
			return "落败";
		case BoardGameHelper.BoardGameState.Draw:
			return "平局";
		default:
			return "结束";
		}
	}

	private static string BuildPlayerRecentEventStableKey(string actionKind, string scope, int day)
	{
		string raw = (actionKind ?? "") + ":" + (scope ?? "");
		return "player_recent:" + (actionKind ?? "event").Trim() + ":" + day + ":" + GetCurrentGameHour() + ":" + (raw.GetHashCode() & int.MaxValue);
	}

	private static List<string> BuildCultureIds(params string[] cultureIds)
	{
		return NormalizeCultureList(cultureIds?.ToList());
	}

	private static List<string> NormalizeCultureList(IEnumerable<string> cultureIds)
	{
		HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string cultureId in cultureIds ?? Enumerable.Empty<string>())
		{
			string normalized = NormalizeCultureId(cultureId);
			if (!string.IsNullOrWhiteSpace(normalized))
			{
				set.Add(normalized);
			}
		}
		return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string NormalizeCultureId(string cultureId)
	{
		return (cultureId ?? "").Trim().ToLowerInvariant();
	}

	private static string NormalizeHeroId(string heroId)
	{
		return (heroId ?? "").Trim().ToLowerInvariant();
	}

	private static string NormalizeHeroLookupId(string heroId)
	{
		string id = NormalizeHeroId(heroId);
		return id.StartsWith("hero:", StringComparison.OrdinalIgnoreCase) ? id.Substring("hero:".Length).Trim() : id;
	}

	private static string NormalizeObserverKey(string observerKey)
	{
		return NormalizeHeroId(observerKey);
	}

	private static string GetHeroId(Hero hero)
	{
		return NormalizeHeroId(hero?.StringId);
	}

	private static bool IsValidObserver(Hero observer)
	{
		return observer != null && observer != Hero.MainHero && !string.IsNullOrWhiteSpace(observer.StringId);
	}

	private static string NormalizeLine(string text)
	{
		return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static string NormalizeStableKey(string stableKey, string text, int day)
	{
		string key = (stableKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			key = "auto:" + day + ":" + Math.Abs((text ?? "").GetHashCode());
		}
		return key;
	}

	private static int GetCurrentGameDayIndex()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static int GetCurrentGameHour()
	{
		try
		{
			return Math.Max(0, Math.Min(23, (int)Math.Floor((CampaignTime.Now.ToDays - Math.Floor(CampaignTime.Now.ToDays)) * 24.0)));
		}
		catch
		{
			return 0;
		}
	}

	private static string GetCurrentGameDateText()
	{
		try
		{
			string text = CampaignTime.Now.ToString();
			return string.IsNullOrWhiteSpace(text) ? ("第 " + GetCurrentGameDayIndex() + " 日") : text.Trim();
		}
		catch
		{
			return "第 " + GetCurrentGameDayIndex() + " 日";
		}
	}

	private int GetNextOrderForDay(List<PlayerActionEntry> entries, int day)
	{
		return (entries ?? new List<PlayerActionEntry>()).Where(x => x != null && x.Day == day).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
	}

	private int GetNextSequence()
	{
		_state.LastSequence++;
		if (_state.LastSequence <= 0)
		{
			_state.LastSequence = 1;
		}
		return _state.LastSequence;
	}

	private static int GetPlayerClanTierBonus()
	{
		try
		{
			return Math.Max(0, Math.Min(6, Clan.PlayerClan?.Tier ?? Hero.MainHero?.Clan?.Tier ?? 0)) * 10;
		}
		catch
		{
			return 0;
		}
	}

	private static bool RollPercent(int chance)
	{
		chance = Math.Max(0, Math.Min(100, chance));
		if (chance <= 0)
		{
			return false;
		}
		if (chance >= 100)
		{
			return true;
		}
		return MBRandom.RandomInt(0, 100) < chance;
	}

	private static int ClampPercent(double value)
	{
		return (int)Math.Round(ClampPercentDouble(value));
	}

	private static double ClampPercentDouble(double value)
	{
		return ClampDouble(value, 0.0, 100.0);
	}

	private static double ClampDouble(double value, double min, double max)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return min;
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static int GetSummaryIntervalDays()
	{
		try
		{
			return Math.Max(1, Math.Min(30, DuelSettings.GetSettings()?.PlayerNotorietySummaryIntervalDays ?? 3));
		}
		catch
		{
			return 3;
		}
	}

	private static int GetMajorPromptChars()
	{
		try
		{
			return Math.Max(80, Math.Min(1000, DuelSettings.GetSettings()?.PlayerNotorietyMajorPromptChars ?? 300));
		}
		catch
		{
			return 300;
		}
	}

	private static float GetCourierDistanceMultiplier()
	{
		try
		{
			return Math.Max(0.5f, Math.Min(10f, DuelSettings.GetSettings()?.PlayerNotorietyCourierRecentDistanceMultiplier ?? 3f));
		}
		catch
		{
			return 3f;
		}
	}

	private static float GetCourierRecentDistanceThreshold()
	{
		try
		{
			float seeingRange = Math.Max(1f, MobileParty.MainParty?.SeeingRange ?? 1f);
			return seeingRange * GetCourierDistanceMultiplier();
		}
		catch
		{
			return 3f;
		}
	}

	private static float GetDistanceToHeroParty(Hero hero)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			MobileParty targetParty = hero?.PartyBelongedTo;
			if (mainParty == null || targetParty == null)
			{
				return -1f;
			}
			return mainParty.Position.Distance(targetParty.Position);
		}
		catch
		{
			return -1f;
		}
	}

	private static string GetLevelText(double score)
	{
		int index = (int)Math.Floor(ClampPercentDouble(score) / 10.0);
		if (index > 10)
		{
			index = 10;
		}
		return NotorietyLevelTexts[index];
	}

	private static string FormatScore(double value)
	{
		value = ClampPercentDouble(value);
		return Math.Abs(value - Math.Round(value)) < 0.005 ? ((int)Math.Round(value)).ToString() : value.ToString("0.##");
	}

	private static string ResolveCultureDisplayName(string cultureId)
	{
		string id = NormalizeCultureId(cultureId);
		if (string.IsNullOrWhiteSpace(id))
		{
			return "未知文化";
		}
		try
		{
			foreach (CultureObject culture in TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
			{
				if (culture != null && string.Equals((culture.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase))
				{
					string name = culture.Name?.ToString();
					return string.IsNullOrWhiteSpace(name) ? id : name.Trim();
				}
			}
		}
		catch
		{
		}
		return id;
	}

	private static Hero FindHeroById(string heroId)
	{
		string id = NormalizeHeroLookupId(heroId);
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Hero.AllAliveHeroes.FirstOrDefault(x => x != null && string.Equals(NormalizeHeroId(x.StringId), id, StringComparison.OrdinalIgnoreCase))
				?? Hero.FindFirst(x => x != null && string.Equals(NormalizeHeroId(x.StringId), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static JObject TryParseJsonObject(string response)
	{
		string text = (response ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return JObject.Parse(text);
		}
		catch
		{
			int start = text.IndexOf('{');
			int end = text.LastIndexOf('}');
			if (start >= 0 && end > start)
			{
				try
				{
					return JObject.Parse(text.Substring(start, end - start + 1));
				}
				catch
				{
				}
			}
		}
		return null;
	}

	private static JToken GetJsonToken(JObject obj, params string[] names)
	{
		if (obj == null)
		{
			return null;
		}
		foreach (string name in names ?? Array.Empty<string>())
		{
			JProperty property = obj.Properties().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
			if (property != null)
			{
				return property.Value;
			}
		}
		return null;
	}

	private static string GetJsonString(JObject obj, params string[] names)
	{
		JToken token = GetJsonToken(obj, names);
		return token?.Type == JTokenType.Null ? "" : (token?.ToString() ?? "");
	}

	private static void LogDebug(string message)
	{
		try
		{
			if (DuelSettings.GetSettings()?.PlayerNotorietyDebugLogs == true)
			{
				Logger.Log("PlayerNotoriety", message);
			}
		}
		catch
		{
		}
	}

	public static string NormalizeMemoryPublicity(string raw, int effectiveTrust)
	{
		string text = (raw ?? "").Trim().ToLowerInvariant();
		if (text == "public")
		{
			return "public";
		}
		if (text == "private" && effectiveTrust <= TrustPrivateLeakThreshold)
		{
			return "leaked_public";
		}
		return "private";
	}

	private sealed class PlayerNotorietyState
	{
		public bool LowProfileModeEnabled;
		public double WorldNotoriety;
		public Dictionary<string, double> CultureNotoriety = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, PlayerNpcKnowledgeState> NpcKnowledge = new Dictionary<string, PlayerNpcKnowledgeState>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> ConversationOutcomeReceipts = new Dictionary<string, string>(StringComparer.Ordinal);
		public List<PlayerActionEntry> RecentActions = new List<PlayerActionEntry>();
		public List<PlayerHistoryMaterial> MajorMaterials = new List<PlayerHistoryMaterial>();
		public List<string> SummarizedMaterialKeys = new List<string>();
		public string MajorSummary = "";
		public int LastSummaryDay = -1;
		public int LastSequence;
		public int SummaryRetryCount;
		public string LastSummaryError = "";
		public long UpdatedUtcTicks;
		public bool LegacyVillageRaidDefenseRepairApplied;
	}

	private sealed class PlayerNpcKnowledgeState
	{
		public string HeroId = "";
		public bool KnowsMajorHistory;
		public int KnownAtDay = -1;
		public double PersonalKnownBonus;
		public int CompletedConversationSessions;
		public int LastConversationDay = -1;
		public float LastCourierSentDistance = -1f;
		public int LastCourierSentDay = -1;
	}

	private sealed class ActiveConversationState
	{
		public string HeroId = "";
		public int StartDay;
		public int StartHour;
		public int LastDay;
		public int LastHour;
		public int KnownRollChance;
		public bool KnowsMajorThisSession;
		public int LineCount;
		public bool HasLegacyLines;
		public string ExactOutcomeReceiptId = "";
		public string ExactOutcomeCandidateHash = "";
		public string ExactMemorySessionKey = "";
	}

	private sealed class PrisonerRosterCountEntry
	{
		public string Key = "";
		public CharacterObject Character;
		public int Count;
	}

	private sealed class PrisonerRosterSummary
	{
		public int TotalCount;
		public int HeroCount;
		public int RegularCount;
		public string DetailText = "";
		public string Signature = "";
		public string PrimaryCultureId = "";
	}

	private sealed class PlayerActionEntry
	{
		public int Day;
		public int Order;
		public int Sequence;
		public string GameDate = "";
		public string Text = "";
		public string StableKey = "";
		public string ActionKind = "";
		public string SettlementId = "";
		public string SettlementName = "";
		public string LocationText = "";
		public string ActorCultureId = "";
		public string TargetCultureId = "";
		public string SettlementCultureId = "";
		public bool? Won;
		public bool IsMajor;
	}

	private sealed class PlayerHistoryMaterial
	{
		public int Day;
		public string GameDate = "";
		public string Text = "";
		public string SourceKind = "";
		public string StableKey = "";
		public List<string> CultureIds = new List<string>();
		public bool Summarized;
		public long CreatedUtcTicks;
	}
}
