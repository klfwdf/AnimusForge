using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace AnimusForge;

public sealed class PlayerNotorietyBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_af_player_notoriety_state_v1";
	private const int RecentActionWindowDays = 10;
	private const int MaxRecentActions = 96;
	private const int MaxMajorMaterials = 180;
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
		Logger.Log("PlayerNotoriety", "registered v1 behavior.");
	}

	public override void SyncData(IDataStore dataStore)
	{
		string storageJson = null;
		if (dataStore.IsSaving)
		{
			storageJson = JsonConvert.SerializeObject(NormalizeState(_state));
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
		}
		catch (Exception ex)
		{
			_state = new PlayerNotorietyState();
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
				+ " npcKnowledge=" + (state.NpcKnowledge?.Count ?? 0)
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
		if (_state.MajorMaterials.Any(x => x != null && string.Equals(x.StableKey ?? "", key, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		material.StableKey = key;
		material.Text = NormalizeLine(material.Text);
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

	private void TryStartSummaryProcessing()
	{
		if (_summaryProcessing)
		{
			return;
		}
		if (!HasSummaryWorkDue())
		{
			return;
		}
		_summaryProcessing = true;
		_ = ProcessSummaryAsync();
	}

	private bool HasSummaryWorkDue()
	{
		_state = NormalizeState(_state);
		if (!_state.MajorMaterials.Any(x => x != null && !x.Summarized))
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

	private async Task ProcessSummaryAsync()
	{
		try
		{
			_state = NormalizeState(_state);
			List<PlayerHistoryMaterial> sourceMaterials = _state.MajorMaterials
				.Where(x => x != null && !x.Summarized)
				.OrderBy(x => x.Day)
				.ThenBy(x => x.CreatedUtcTicks)
				.Take(24)
				.ToList();
			if (sourceMaterials.Count == 0)
			{
				return;
			}
			string sys = BuildSummarySystemPrompt();
			string user = BuildSummaryUserPrompt(sourceMaterials);
			string response = await MyBehavior.CallAuxiliaryApiTextForExternal(sys, user, "PlayerNotorietySummary");
			if (TryParseSummaryResponse(response, out string summary, out double delta, out string error))
			{
				ApplySummarySuccess(sourceMaterials, summary, delta);
				return;
			}
			_state.SummaryRetryCount++;
			_state.LastSummaryError = error;
			Logger.Log("PlayerNotoriety", "summary parse failed: " + error);
			if (_state.SummaryRetryCount >= MaxSummaryRetries)
			{
				foreach (PlayerHistoryMaterial material in sourceMaterials)
				{
					if (material != null)
					{
						material.Summarized = true;
					}
				}
				_state.SummaryRetryCount = 0;
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
		}
	}

	private static string BuildSummarySystemPrompt()
	{
		return "你是 AnimusForge 的玩家履历与知名度总结器。你必须只输出严格 JSON：{\"summary_content\":\"新的玩家重大履历时间线摘要\",\"notoriety_delta\":0到10之间的小数}。"
			+ "你要把已有摘要与新增重大履历融合成一段新的时间线摘要，保留关键人物、地点、胜败、承诺和公开影响。"
			+ "不要编造素材没有的事实。summary_content 最多约700个中文字符。"
			+ "notoriety_delta 表示这批公开素材带来的文化知名度增量，范围0-10；小事应为0到1之间的小数，重大胜利、夺城、处决、王国事件才可接近10。";
	}

	private string BuildSummaryUserPrompt(List<PlayerHistoryMaterial> materials)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("已有玩家履历摘要：");
		sb.AppendLine(string.IsNullOrWhiteSpace(_state.MajorSummary) ? "（无）" : _state.MajorSummary.Trim());
		sb.AppendLine();
		sb.AppendLine("新增公开素材：");
		foreach (PlayerHistoryMaterial material in materials ?? new List<PlayerHistoryMaterial>())
		{
			if (material == null || string.IsNullOrWhiteSpace(material.Text))
			{
				continue;
			}
			sb.AppendLine("- [" + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "][" + (material.SourceKind ?? "material") + "][culture:" + string.Join(",", material.CultureIds ?? new List<string>()) + "] " + material.Text.Trim());
		}
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
		_state.MajorSummary = NormalizeLine(summary);
		_state.LastSummaryDay = GetCurrentGameDayIndex();
		_state.SummaryRetryCount = 0;
		_state.LastSummaryError = "";
		_state.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
		HashSet<string> cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PlayerHistoryMaterial material in materials ?? new List<PlayerHistoryMaterial>())
		{
			if (material == null)
			{
				continue;
			}
			material.Summarized = true;
			foreach (string cultureId in NormalizeCultureList(material.CultureIds))
			{
				cultures.Add(cultureId);
			}
		}
		if (cultures.Count == 0)
		{
			AddWorldNotoriety(delta / 3.0, "summary_world_only");
		}
		else
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
		return BuildPlayerMajorRuntimeInstruction(GetHeroId(observer), observer?.Culture?.StringId, BuildPlayerDisplayNameForPrompt(observer));
	}

	private string BuildPlayerMajorRuntimeInstruction(string observerKey, string cultureId)
	{
		return BuildPlayerMajorRuntimeInstruction(observerKey, cultureId, BuildPlayerDisplayNameForPrompt(observerKey, cultureId));
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

	private string BuildMajorHistoryForPrompt(string playerDisplayName)
	{
		_state = NormalizeState(_state);
		string playerName = NormalizePlayerDisplayName(playerDisplayName);
		int summaryChars = GetMajorPromptChars();
		string summary = (_state.MajorSummary ?? "").Trim();
		if (summaryChars > 0 && summary.Length > summaryChars)
		{
			summary = summary.Substring(Math.Max(0, summary.Length - summaryChars), summaryChars);
		}
		summary = RenderPlayerActionTextForPrompt(summary, playerName);
		List<PlayerHistoryMaterial> unsummarized = (_state.MajorMaterials ?? new List<PlayerHistoryMaterial>())
			.Where(x => x != null && !x.Summarized && !string.IsNullOrWhiteSpace(x.Text))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.Take(16)
			.ToList();
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(summary))
		{
			sb.AppendLine(summary);
		}
		if (unsummarized.Count > 0)
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine("尚未总结的新增公开履历素材：");
			foreach (PlayerHistoryMaterial material in unsummarized)
			{
				sb.AppendLine("- " + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "：" + RenderPlayerActionTextForPrompt(material.Text, playerName));
			}
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
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
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

	private static string RenderPlayerActionTextForPrompt(string rawText, string playerDisplayName)
	{
		string text = NormalizeLine(rawText);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string name = NormalizePlayerDisplayName(playerDisplayName);
		return text
			.Replace("你们的", name + "一方的")
			.Replace("你们", name + "一方")
			.Replace("你方的", name + "一方的")
			.Replace("你方", name + "一方")
			.Replace("你的", name + "的")
			.Replace("你部队", name + "的部队")
			.Replace("你", name);
	}

	private bool DoesObserverKnowPlayer(Hero observer)
	{
		if (!IsValidObserver(observer))
		{
			return false;
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
		return HasObserverUnlockedPlayerMajor(GetHeroId(observer));
	}

	private bool HasObserverUnlockedPlayerMajor(string observerKey)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return false;
		}
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(key, create: false);
		return state?.KnowsMajorHistory == true;
	}

	private void MarkObserverKnowsPlayer(Hero observer, string reason)
	{
		if (!IsValidObserver(observer))
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
			bool knows = RollPercent(chance);
			if (knows)
			{
				state.KnowsMajorHistory = true;
				state.KnownAtDay = GetCurrentGameDayIndex();
			}
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
		return CanObserverKnowRecentActions(GetHeroId(observer), observer?.Culture?.StringId, courier);
	}

	private bool CanObserverKnowRecentActions(string observerKey, string cultureId, bool courier)
	{
		string key = NormalizeObserverKey(observerKey);
		if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
		{
			return false;
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
		return GetEffectiveNotoriety(GetHeroId(observer), observer?.Culture?.StringId);
	}

	private int GetEffectiveNotoriety(string observerKey, string cultureId)
	{
		_state = NormalizeState(_state);
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
				FinalizeConversationByHeroId(state?.HeroId);
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
		_activeConversationStates.Remove(normalizedHeroId);
		PlayerNpcKnowledgeState state = GetNpcKnowledgeState(normalizedHeroId, create: true);
		if (state == null)
		{
			return;
		}
		state.CompletedConversationSessions++;
		if (!state.KnowsMajorHistory && active.LineCount > 0)
		{
			state.PersonalKnownBonus = ClampPercentDouble(state.PersonalKnownBonus + active.LineCount * PersonalKnownBonusPerLine);
		}
		state.LastConversationDay = GetCurrentGameDayIndex();
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
			CultureRows = BuildPlayerNotorietyCultureRows()
		};
	}

	private string BuildPlayerNotorietyHistoryText(bool includeRawMaterials)
	{
		_state = NormalizeState(_state);
		StringBuilder sb = new StringBuilder();
		string playerName = "玩家";
		string summary = RenderPlayerActionTextForPrompt((_state.MajorSummary ?? "").Trim(), playerName);
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
		string history = BuildMajorHistoryForPrompt("玩家");
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
					sb.AppendLine("- " + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "：" + RenderPlayerActionTextForPrompt(material.Text, "玩家"));
				}
			}
		}
		return sb.ToString().Trim();
	}

	private void OpenPlayerNotorietyView()
	{
		bool canEdit = MyBehavior.IsDevDataManagementEnabledForExternal();
		if (PlayerNotorietyPopup.Show(BuildPlayerNotorietyPopupData(canEdit), canEdit ? OpenPlayerMajorHistoryEditor : null))
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
		return (input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
	}

	private static PlayerNotorietyState NormalizeState(PlayerNotorietyState state)
	{
		state ??= new PlayerNotorietyState();
		state.CultureNotoriety ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
		state.NpcKnowledge ??= new Dictionary<string, PlayerNpcKnowledgeState>(StringComparer.OrdinalIgnoreCase);
		state.RecentActions ??= new List<PlayerActionEntry>();
		state.MajorMaterials ??= new List<PlayerHistoryMaterial>();
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
			.Select(NormalizeActionEntry)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.Sequence)
			.Take(MaxRecentActions)
			.ToList();
		state.MajorMaterials = state.MajorMaterials
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
			.Select(NormalizeHistoryMaterial)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.Take(MaxMajorMaterials)
			.ToList();
		return state;
	}

	private static PlayerActionEntry NormalizeActionEntry(PlayerActionEntry entry)
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
		return entry;
	}

	private static PlayerHistoryMaterial NormalizeHistoryMaterial(PlayerHistoryMaterial material)
	{
		material.Text = NormalizeLine(material.Text);
		material.StableKey = NormalizeStableKey(material.StableKey, material.Text, material.Day);
		material.SourceKind = (material.SourceKind ?? "").Trim();
		material.GameDate = (material.GameDate ?? "").Trim();
		material.CultureIds = NormalizeCultureList(material.CultureIds);
		return material;
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

	private static string BuildPrisonerDonationSkipKey(Settlement settlement, string signature)
	{
		return GetCurrentGameDayIndex() + ":" + (settlement?.StringId ?? "") + ":" + ((signature ?? "").Trim());
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
		string id = NormalizeHeroId(heroId);
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
		public double WorldNotoriety;
		public Dictionary<string, double> CultureNotoriety = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, PlayerNpcKnowledgeState> NpcKnowledge = new Dictionary<string, PlayerNpcKnowledgeState>(StringComparer.OrdinalIgnoreCase);
		public List<PlayerActionEntry> RecentActions = new List<PlayerActionEntry>();
		public List<PlayerHistoryMaterial> MajorMaterials = new List<PlayerHistoryMaterial>();
		public string MajorSummary = "";
		public int LastSummaryDay = -1;
		public int LastSequence;
		public int SummaryRetryCount;
		public string LastSummaryError = "";
		public long UpdatedUtcTicks;
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
