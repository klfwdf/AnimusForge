using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
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

	private PlayerNotorietyState _state = new PlayerNotorietyState();
	private readonly Dictionary<string, ActiveConversationState> _activeConversationStates = new Dictionary<string, ActiveConversationState>(StringComparer.OrdinalIgnoreCase);
	private bool _summaryProcessing;

	public static PlayerNotorietyBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnNativeConversationEnded);
		Logger.Log("PlayerNotoriety", "registered v1 behavior.");
	}

	public override void SyncData(IDataStore dataStore)
	{
		string storageJson = null;
		if (dataStore.IsSaving)
		{
			storageJson = JsonConvert.SerializeObject(NormalizeState(_state));
		}
		dataStore.SyncData(StorageKey, ref storageJson);
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
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
		return BuildPlayerMajorRuntimeInstruction(GetHeroId(observer), observer?.Culture?.StringId);
	}

	private string BuildPlayerMajorRuntimeInstruction(string observerKey, string cultureId)
	{
		if (!DoesObserverKnowPlayer(observerKey, cultureId))
		{
			return "";
		}
		string major = BuildMajorHistoryForPrompt();
		if (string.IsNullOrWhiteSpace(major))
		{
			return "";
		}
		return "【你已知的玩家重大履历】\n" + major + "\n使用边界：这些是你已经听说或确认的玩家公开履历。可以自然提及，但不要说成系统提示。";
	}

	private string BuildPlayerRecentRuntimeInstruction(Hero observer, bool courier)
	{
		if (!IsValidObserver(observer))
		{
			return "";
		}
		return BuildPlayerRecentRuntimeInstruction(GetHeroId(observer), observer?.Culture?.StringId, courier);
	}

	private string BuildPlayerRecentRuntimeInstruction(string observerKey, string cultureId, bool courier)
	{
		if (!CanObserverKnowRecentActions(observerKey, cultureId, courier))
		{
			return "";
		}
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
		sb.AppendLine("【你已知的玩家近期行动】");
		foreach (PlayerActionEntry entry in recent)
		{
			sb.AppendLine("- " + (string.IsNullOrWhiteSpace(entry.GameDate) ? ("第" + entry.Day + "日") : entry.GameDate.Trim()) + "：" + entry.Text.Trim());
		}
		sb.Append("使用边界：这些是你对玩家最近十天行动的公开认知。");
		return sb.ToString().Trim();
	}

	private string BuildMajorHistoryForPrompt()
	{
		_state = NormalizeState(_state);
		int summaryChars = GetMajorPromptChars();
		string summary = (_state.MajorSummary ?? "").Trim();
		if (summaryChars > 0 && summary.Length > summaryChars)
		{
			summary = summary.Substring(Math.Max(0, summary.Length - summaryChars), summaryChars);
		}
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
				sb.AppendLine("- " + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "：" + material.Text.Trim());
			}
		}
		return sb.ToString().Trim();
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
		string history = BuildMajorHistoryForPrompt();
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
				sb.AppendLine("- " + (string.IsNullOrWhiteSpace(entry.GameDate) ? ("第" + entry.Day + "日") : entry.GameDate.Trim()) + "：" + entry.Text);
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
					sb.AppendLine("- " + (string.IsNullOrWhiteSpace(material.GameDate) ? ("第" + material.Day + "日") : material.GameDate.Trim()) + "：" + material.Text);
				}
			}
		}
		return sb.ToString().Trim();
	}

	private void OpenPlayerNotorietyView()
	{
		string text = BuildPlayerNotorietyDisplayText(includeRawMaterials: true);
		InformationManager.ShowInquiry(new InquiryData("玩家知名度与履历", text, true, false, "关闭", "", null, null));
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
