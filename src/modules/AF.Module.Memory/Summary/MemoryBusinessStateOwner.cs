using System.Text;
using TaleWorlds.Library;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

// The host's legacy fields are save/engine projections of these same containers.
// Never clones a store and never invokes a whole host Apply/Mark business callback.
// All entry points execute inside the existing campaign-thread dispatcher.
internal sealed partial class MemoryBusinessStateOwner
{
    private MemorySealingOwner _sealing;
    internal MemorySealingOwner Sealing => _sealing ??= new MemorySealingOwner(this);

    internal Dictionary<string, List<DailyMemoryDraft>> Drafts = new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, List<CompressedMemoryBlock>> Blocks = new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, MemoryOverviewState> Overviews = new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, MajorActionSummaryState> MajorSummaries = new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
    internal List<WeeklyMemoryMaterialTrigger> PendingWeeklyTriggers = new List<WeeklyMemoryMaterialTrigger>();
    internal List<MemorySummaryJob> DailyQueue = new List<MemorySummaryJob>();
    internal List<MemoryOverviewJob> OverviewQueue = new List<MemoryOverviewJob>();
    internal List<MajorActionSummaryJob> MajorQueue = new List<MajorActionSummaryJob>();
    internal readonly HashSet<string> DirtyOverviewIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal readonly Queue<string> OverviewCandidateIds = new Queue<string>();
    internal readonly HashSet<string> OverviewCandidateIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    internal List<DailyMemoryDraft> LoadDrafts(string memoryId)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return new List<DailyMemoryDraft>();
        Drafts ??= new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
        return Drafts.TryGetValue(id, out var value) && value != null ? value : new List<DailyMemoryDraft>();
    }

    internal List<CompressedMemoryBlock> LoadBlocks(string memoryId)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return new List<CompressedMemoryBlock>();
        Blocks ??= new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
        return Blocks.TryGetValue(id, out var value) && value != null ? value : new List<CompressedMemoryBlock>();
    }

    internal void SaveDrafts(string memoryId, List<DailyMemoryDraft> drafts)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return;
        Drafts ??= new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
        var normalized = MemoryRecordRules.SanitizeDailyMemoryDrafts(drafts);
        if (normalized.Count > 0) Drafts[id] = normalized;
        else Drafts.Remove(id);
    }

    internal void SaveBlocks(string memoryId, List<CompressedMemoryBlock> blocks, Action<string> markDirty)
    {
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (string.IsNullOrWhiteSpace(id)) return;
        Blocks ??= new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
        var normalized = MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks);
        if (normalized.Count > 0) Blocks[id] = normalized;
        else Blocks.Remove(id);
        markDirty(id);
    }

    internal bool ApplyDaily(MemorySummaryJob job, CompressedMemoryBlock block, string memoryId,
        MemoryDailyCommitEffects effects)
    {
        if (job == null || block == null) return false;
        var blocks = LoadBlocks(memoryId);
        blocks.RemoveAll(x => x != null && x.GameDayIndex == job.GameDayIndex);
        blocks.Add(block);
        SaveBlocks(memoryId, blocks, effects.MarkOverviewDirty);
        var drafts = LoadDrafts(memoryId);
        drafts.RemoveAll(x => x != null && x.GameDayIndex == job.GameDayIndex);
        SaveDrafts(memoryId, drafts);
        // Preserve partial-write behavior: Native failure occurs before queue removal.
        effects.ClearNativeHistory(job.GameDayIndex);
        DailyQueue?.RemoveAll(x => x != null && Same(x.HeroId, memoryId) && x.GameDayIndex == job.GameDayIndex);
        effects.LogSuccess();
        if (!string.IsNullOrWhiteSpace(block.PlayerHistoryMaterial)) effects.RecordPublicMemory(block);
        effects.RecordPublicWeeklyMaterial(block);
        effects.RecordWeeklyMaterial(block);
        effects.EnqueueOverview(memoryId, job.HeroName, blocks);
        return true;
    }

    internal bool ApplyOverview(MemoryOverviewJob job, MemoryOverviewState state, string memoryId, Action log)
    {
        if (job == null || state == null || string.IsNullOrWhiteSpace(state.Summary) || string.IsNullOrWhiteSpace(memoryId)) return false;
        Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        state.HeroId = memoryId;
        if (string.IsNullOrWhiteSpace(state.HeroName)) state.HeroName = (job.HeroName ?? "").Trim();
        Overviews[memoryId] = MemoryRecordRules.SanitizeMemoryOverviewState(state);
        OverviewQueue?.RemoveAll(x => x != null && Same(x.HeroId, memoryId));
        log();
        return true;
    }

    internal bool ApplyMajor(MajorActionSummaryJob job, MajorActionSummaryState state, string memoryId, Action log)
    {
        if (job == null || state == null || string.IsNullOrWhiteSpace(state.Summary) || string.IsNullOrWhiteSpace(memoryId)) return false;
        MajorSummaries ??= new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
        state.HeroId = memoryId;
        if (string.IsNullOrWhiteSpace(state.HeroName)) state.HeroName = (job.HeroName ?? "").Trim();
        MajorSummaries[memoryId] = MemoryRecordRules.SanitizeMajorActionSummaryState(state);
        MajorQueue?.RemoveAll(x => x != null && Same(x.HeroId, memoryId));
        log();
        return true;
    }

    internal void FailDaily(MemorySummaryJob job, string error)
    {
        if (job == null || DailyQueue == null) return;
        string id = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
        foreach (var item in DailyQueue)
            if (item != null && Same(item.HeroId, id) && item.GameDayIndex == job.GameDayIndex)
            { item.RetryCount = 3; item.LastError = (error ?? "").Trim(); }
        var draft = Drafts != null && Drafts.TryGetValue(id, out var drafts)
            ? drafts?.FirstOrDefault(x => x != null && x.GameDayIndex == job.GameDayIndex && Same(x.HeroId, id)) : null;
        if (draft != null) { draft.SummaryRetryCount = 3; draft.LastSummaryError = (error ?? "").Trim(); }
    }

    internal void FailOverview(MemoryOverviewJob job, string error)
    {
        if (job == null) return;
        string id = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
        foreach (var item in OverviewQueue ?? Enumerable.Empty<MemoryOverviewJob>())
            if (item != null && Same(item.HeroId, id)) { item.RetryCount = 3; item.LastError = (error ?? "").Trim(); }
        if (string.IsNullOrWhiteSpace(id)) return;
        Overviews ??= new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
        var state = Overviews.TryGetValue(id, out var value) ? MemoryRecordRules.SanitizeMemoryOverviewState(value?.CopyForSummary()) : null;
        state ??= new MemoryOverviewState { HeroId = id, HeroName = (job.HeroName ?? "").Trim() };
        state.LastError = (error ?? "").Trim();
        Overviews[id] = MemoryRecordRules.SanitizeMemoryOverviewState(state);
    }

    internal void FailMajor(MajorActionSummaryJob job, string error)
    {
        if (job == null) return;
        string id = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
        foreach (var item in MajorQueue ?? Enumerable.Empty<MajorActionSummaryJob>())
            if (item != null && Same(item.HeroId, id)) { item.RetryCount = 3; item.LastError = (error ?? "").Trim(); }
        if (string.IsNullOrWhiteSpace(id)) return;
        MajorSummaries ??= new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
        var state = MajorSummaries.TryGetValue(id, out var value) ? MemoryRecordRules.SanitizeMajorActionSummaryState(value?.CopyForSummary()) : null;
        state ??= new MajorActionSummaryState { HeroId = id, HeroName = (job.HeroName ?? "").Trim() };
        state.LastError = (error ?? "").Trim();
        MajorSummaries[id] = MemoryRecordRules.SanitizeMajorActionSummaryState(state);
    }

    private static bool Same(string left, string normalizedRight) => string.Equals(
        MemoryRecordRules.NormalizeMemoryHeroId(left), normalizedRight, StringComparison.OrdinalIgnoreCase);

    internal void EnsureHistoryAndDailyPersistenceContainers()
    {
		if (History == null)
		{
			History = new Dictionary<string, List<MyBehavior.DialogueDay>>();
		}
		if (HistoryStorage == null)
		{
			HistoryStorage = new Dictionary<string, string>();
		}
		if (Drafts == null)
		{
			Drafts = new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
		}
		if (DraftStorage == null)
		{
			DraftStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (Blocks == null)
		{
			Blocks = new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
		}
		if (BlockStorage == null)
		{
			BlockStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (DailyQueue == null)
		{
			DailyQueue = new List<MemorySummaryJob>();
		}
    }
    internal void EnsureOverviewPersistenceContainers()
    {
		if (Overviews == null)
		{
			Overviews = new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
		}
		if (OverviewStorage == null)
		{
			OverviewStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (OverviewQueue == null)
		{
			OverviewQueue = new List<MemoryOverviewJob>();
		}
    }
    internal void EnsureMajorSummaryPersistenceContainers()
    {
		if (MajorSummaries == null)
		{
			MajorSummaries = new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
		}
		if (MajorStorage == null)
		{
			MajorStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (MajorQueue == null)
		{
			MajorQueue = new List<MajorActionSummaryJob>();
		}
    }
    internal void ClearHistoryAndDailyForCurrentSave()
    {
		History = new Dictionary<string, List<MyBehavior.DialogueDay>>();
		HistoryStorage = new Dictionary<string, string>();
		Drafts = new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
		DraftStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Blocks = new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
		BlockStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		DailyQueue = new List<MemorySummaryJob>();
    }
    internal void ClearOverviewForCurrentSave()
    {
		Overviews = new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
		OverviewStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		OverviewQueue = new List<MemoryOverviewJob>();
    }
    internal void ClearMajorSummaryForCurrentSave()
    {
		MajorSummaries = new Dictionary<string, MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);
		MajorStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		MajorQueue = new List<MajorActionSummaryJob>();
    }
    internal void ClearOverviewDiscoveryForCurrentSave()
    {
		DirtyOverviewIds.Clear();
		OverviewCandidateIds.Clear();
		OverviewCandidateIdSet.Clear();
    }
	internal static bool TryParseBestSummaryJsonObject(string content, string[] requiredPrimaryKeys, string[] requiredSecondaryKeys, out JObject obj, out string error)
	{
		obj = null;
		error = "";
		string text = JsonResponseTextCodec.StripJsonResponseEnvelope(content);
		if (TryParseTaggedSummaryObject(text, requiredPrimaryKeys, requiredSecondaryKeys, out obj))
		{
			return true;
		}
		List<string> candidates = JsonResponseTextCodec.ExtractJsonObjectPayloads(text);
		if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(text))
		{
			candidates.Add(text);
		}
		Exception lastParseException = null;
		int parseFailureCount = 0;
		int validObjectCount = 0;
		foreach (string candidate in candidates)
		{
			if (string.IsNullOrWhiteSpace(candidate))
			{
				continue;
			}
			try
			{
				JObject parsed = JObject.Parse(candidate);
				validObjectCount++;
				if (HasAnyNonWhiteSpaceJsonProperty(parsed, requiredPrimaryKeys) && HasAnyNonWhiteSpaceJsonProperty(parsed, requiredSecondaryKeys))
				{
					obj = parsed;
					return true;
				}
			}
			catch (Exception ex)
			{
				lastParseException = ex;
				parseFailureCount++;
			}
		}
		if (parseFailureCount > 0 && TryParseLooseSummaryJsonObject(text, requiredPrimaryKeys, requiredSecondaryKeys, out obj))
		{
			return true;
		}
		if (validObjectCount > 0)
		{
			error = "未找到包含必需字段的 JSON 对象：" + BuildRequiredJsonFieldDescription(requiredPrimaryKeys, requiredSecondaryKeys) + "。";
			return false;
		}
		if (parseFailureCount > 0)
		{
			error = lastParseException?.Message ?? "JSON 解析失败。";
			return false;
		}
		error = "找不到 JSON 对象。";
		return false;
	}

	internal static bool TryParseTaggedSummaryObject(string content, string[] requiredPrimaryKeys, string[] requiredSecondaryKeys, out JObject obj)
	{
		obj = null;
		string text = JsonResponseTextCodec.StripJsonResponseEnvelope(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		JObject tagged = new JObject();
		AddTaggedSummaryProperty(text, tagged, "rich_title", new string[2] { "TITLE", "RICH_TITLE" });
		AddTaggedSummaryProperty(text, tagged, "summary_content", new string[3] { "SUMMARY", "SUMMARY_CONTENT", "CONTENT" });
		AddTaggedSummaryProperty(text, tagged, "player_publicity", new string[2] { "PUBLICITY", "PLAYER_PUBLICITY" });
		AddTaggedSummaryProperty(text, tagged, "player_history_material", new string[2] { "PLAYER_HISTORY", "PLAYER_HISTORY_MATERIAL" });
		AddTaggedSummaryProperty(text, tagged, "publicity_reason", new string[2] { "REASON", "PUBLICITY_REASON" });
		if (!HasAnyNonWhiteSpaceJsonProperty(tagged, requiredPrimaryKeys) || !HasAnyNonWhiteSpaceJsonProperty(tagged, requiredSecondaryKeys))
		{
			return false;
		}
		obj = tagged;
		return true;
	}

	internal static void AddTaggedSummaryProperty(string text, JObject obj, string propertyName, string[] tagNames)
	{
		if (obj == null || string.IsNullOrWhiteSpace(propertyName) || tagNames == null)
		{
			return;
		}
		if (obj.Properties().Any((JProperty x) => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		foreach (string tagName in tagNames)
		{
			if (TryExtractTaggedBlock(text, tagName, out var value))
			{
				obj[propertyName] = value.Trim();
				return;
			}
		}
	}

	internal static bool TryExtractTaggedBlock(string text, string tagName, out string value)
	{
		value = "";
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(tagName))
		{
			return false;
		}
		string escaped = Regex.Escape(tagName.Trim());
		RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant;
		Match complete = Regex.Match(text, "\\[" + escaped + "\\]\\s*(?<value>.*?)\\s*\\[/" + escaped + "\\]", options);
		if (complete.Success)
		{
			value = (complete.Groups["value"]?.Value ?? "").Trim();
			return !string.IsNullOrWhiteSpace(value);
		}
		Match open = Regex.Match(text, "\\[" + escaped + "\\]\\s*", options);
		if (!open.Success)
		{
			return false;
		}
		int start = open.Index + open.Length;
		Match next = Regex.Match(text.Substring(start), "\\r?\\n\\s*\\[[A-Z_]+\\]\\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		int end = next.Success ? start + next.Index : text.Length;
		value = text.Substring(start, Math.Max(0, end - start)).Trim();
		return !string.IsNullOrWhiteSpace(value);
	}

	internal static bool TryParseLooseSummaryJsonObject(string content, string[] requiredPrimaryKeys, string[] requiredSecondaryKeys, out JObject obj)
	{
		obj = null;
		string text = JsonResponseTextCodec.StripJsonResponseEnvelope(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		JObject loose = new JObject();
		AddLooseJsonStringProperties(text, loose, requiredPrimaryKeys);
		AddLooseJsonStringProperties(text, loose, requiredSecondaryKeys);
		AddLooseJsonStringProperties(text, loose, new string[9] { "player_publicity", "playerPublicity", "publicity", "player_history_material", "playerHistoryMaterial", "history_material", "historyMaterial", "publicity_reason", "publicityReason" });
		if (!HasAnyNonWhiteSpaceJsonProperty(loose, requiredPrimaryKeys) || !HasAnyNonWhiteSpaceJsonProperty(loose, requiredSecondaryKeys))
		{
			return false;
		}
		obj = loose;
		return true;
	}

	internal static void AddLooseJsonStringProperties(string text, JObject obj, string[] names)
	{
		if (obj == null || names == null)
		{
			return;
		}
		foreach (string name in names)
		{
			if (string.IsNullOrWhiteSpace(name) || obj.Properties().Any((JProperty x) => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			if (JsonResponseTextCodec.TryExtractLooseJsonStringProperty(text, name.Trim(), out var value) && !string.IsNullOrWhiteSpace(value))
			{
				obj[name.Trim()] = value.Trim();
			}
		}
	}

	internal static string BuildRequiredJsonFieldDescription(string[] primaryKeys, string[] secondaryKeys)
	{
		string text = BuildRequiredJsonFieldGroupDescription(primaryKeys);
		string text2 = BuildRequiredJsonFieldGroupDescription(secondaryKeys);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + " + " + text2;
	}

	internal static string BuildRequiredJsonFieldGroupDescription(string[] keys)
	{
		List<string> list = (keys ?? new string[0]).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return list.Count == 0 ? "（无）" : string.Join("/", list);
	}

	internal static bool HasAnyNonWhiteSpaceJsonProperty(JObject obj, string[] names)
	{
		if (names == null || names.Length == 0)
		{
			return true;
		}
		return !string.IsNullOrWhiteSpace(JsonResponseTextCodec.GetJsonStringIgnoreCase(obj, names));
	}

	internal static string BuildSummaryJsonParseFailureMessage(string prefix, string parseError, string content)
	{
		string detail = (parseError ?? "").Trim();
		if (string.IsNullOrWhiteSpace(detail))
		{
			detail = "未知解析错误。";
		}
		return LlmRetryPrompt.BuildFailureDetail(prefix + "：" + detail, content);
	}

	internal static string CleanAIResponse(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}
		input = Regex.Replace(input, "<think>.*?</think>", "", RegexOptions.Singleline);
		input = input.Replace("**", "").Replace("#", "").Replace("`", "");
		return input.Trim();
	}
	internal static bool ContainsAnyIgnoreCase(string text, params string[] tokens)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || tokens == null)
		{
			return false;
		}
		foreach (string text3 in tokens)
		{
			if (!string.IsNullOrWhiteSpace(text3) && text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

internal bool HasCompressedMemoryBlock(string heroId, int dayIndex)
	{
		heroId = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
		if (string.IsNullOrWhiteSpace(heroId) || Blocks == null)
		{
			return false;
		}
		return Blocks.TryGetValue(heroId, out var value) && value != null && value.Any((CompressedMemoryBlock x) => x != null && x.GameDayIndex == dayIndex);
	}

	internal const string NonHeroMemoryIdPrefix = "af_nonhero:";
internal static bool IsNonHeroMemoryId(string memoryId)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		return text.StartsWith(NonHeroMemoryIdPrefix, StringComparison.OrdinalIgnoreCase) && text.Length > NonHeroMemoryIdPrefix.Length;
	}

internal static string NormalizeWeeklyPromptKeyPart(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return Regex.Replace(text, "[\\s:|]+", "_");
	}
internal static bool IsMemoryBlockIncludedInOverview(CompressedMemoryBlock block, HashSet<string> includedIds)
	{
		if (block == null || includedIds == null || includedIds.Count <= 0)
		{
			return false;
		}
		string blockId = (block.Id ?? MemoryRecordRules.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
		return !string.IsNullOrWhiteSpace(blockId) && includedIds.Contains(blockId);
	}

    internal bool MaintenanceCycleActive;
    internal MemoryMaintenanceWorkBudget MaintenanceBudget;
    internal void ResolveMaintenanceBudget(Func<double> frameBudgetMs, int metadataPerSlice, int jobsPerTick,
        out long startTimestamp, out double budgetMs)
    {
        var window = MaintenanceBudget;
        if (window == null && MaintenanceCycleActive)
        {
            window = new MemoryMaintenanceWorkBudget(Stopwatch.GetTimestamp(), frameBudgetMs(), metadataPerSlice, jobsPerTick);
            MaintenanceBudget = window;
        }
        startTimestamp = window == null ? Stopwatch.GetTimestamp() : window.Start;
        budgetMs = window == null ? frameBudgetMs() : window.Milliseconds;
    }
internal static bool IsSceneShoutObserverHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		return text.TrimStart().StartsWith("[场景喊话]", StringComparison.Ordinal);
	}

internal static bool IsLoreInjectionHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("【玩家触发了（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【玩家外貌信息（常驻）】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【触发相关话题/背景】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.IndexOf("参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) >= 0)
		{
			return true;
		}
		if (text.IndexOf("应当知晓以下信息", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

internal static bool IsPlayerTurnStartLine(string line)
	{
		return AnimusForge.Refactor.Adapters.MemoryRecallInputCaptureAdapter.TryStripPlayerSpeechPrefix(line, out var _);
	}

internal static bool IsMeaningfulDirectConversationLine(string line)
	{
		return IsMeaningfulConversationLine(line, includeActiveScene: true, isActiveScene: null);
	}

internal static bool IsMeaningfulConversationLine(string line, bool includeActiveScene, Func<string,bool> isActiveScene)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return (includeActiveScene || !(isActiveScene?.Invoke(text) == true)) && !HistoryArchiveRecallOwner.IsSystemFactLine(text) && !IsLoreInjectionHistoryLine(text);
	}
internal bool IsDailyMemoryLinePublished(string normalizedMemoryId, int gameDayIndex, DailyMemoryDraft draft, DailyMemoryLine line)
	{
		return draft != null && line != null && gameDayIndex >= 0
			&& string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(draft.HeroId), normalizedMemoryId, StringComparison.Ordinal)
			&& draft.GameDayIndex == gameDayIndex && line.GameDayIndex == gameDayIndex
			&& Drafts != null && Drafts.TryGetValue(normalizedMemoryId, out var published)
			&& published != null && published.Contains(draft)
			&& draft.Lines != null && draft.Lines.Contains(line);
	}
internal bool AppendDailyMemoryLineById(string memoryId, string memoryName, string speaker, string text, bool isAfef, bool isLlmDialogue, int sceneSessionId, int targetAgentIndex, string targetName, MemoryDailyAppendCapabilities capture)
	{
		string normalizedMemoryId = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (!capture.IsEligible(normalizedMemoryId))
		{
			return false;
		}
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		int dayIndex = capture.Day();
		string gameDate = capture.Date();
		int hour = capture.Hour();
		string heroId = normalizedMemoryId;
		int dialogueSessionId = sceneSessionId >= 0 ? -1 : capture.NativeSession();
		List<DailyMemoryDraft> list = LoadDrafts(heroId);
		DailyMemoryDraft dailyMemoryDraft = list.FirstOrDefault((DailyMemoryDraft x) => x != null && x.GameDayIndex == dayIndex);
		if (dailyMemoryDraft == null)
		{
			dailyMemoryDraft = new DailyMemoryDraft
			{
				HeroId = heroId,
				HeroName = string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName.Trim(),
				GameDayIndex = dayIndex,
				GameDate = gameDate
			};
			list.Add(dailyMemoryDraft);
		}
		if (dailyMemoryDraft.Lines == null)
		{
			dailyMemoryDraft.Lines = new List<DailyMemoryLine>();
		}
		DailyMemoryLine dailyMemoryLine = new DailyMemoryLine
		{
			GameDayIndex = dayIndex,
			GameDate = gameDate,
			GameHour = hour,
			Scene = capture.Scene(),
			Speaker = (speaker ?? "").Trim(),
			Text = text2,
			SceneSessionId = sceneSessionId,
			DialogueSessionId = dialogueSessionId,
			TargetAgentIndex = Math.Max(-1, targetAgentIndex),
			TargetName = (targetName ?? "").Trim(),
			MemorySessionKey = BuildCurrentMemorySessionKey(sceneSessionId, dialogueSessionId),
			IsAfef = isAfef,
			IsLlmDialogue = isLlmDialogue && !isAfef
		};
		dailyMemoryDraft.Lines.Add(dailyMemoryLine);
		if (dailyMemoryLine.IsLlmDialogue)
		{
			dailyMemoryDraft.HasLlmDialogue = true;
		}
		AttachPendingWeeklyTriggers(dailyMemoryDraft, dailyMemoryLine, capture.CurrentDay());
		SaveDrafts(heroId, list);
		bool dailyPublished = IsDailyMemoryLinePublished(
			heroId,
			dayIndex,
			dailyMemoryDraft,
			dailyMemoryLine);
		if (dailyPublished && dailyMemoryLine.IsLlmDialogue)
		{
			capture.NoteConversation(heroId);
		}
		if (IsNonHeroMemoryId(heroId))
		{
			capture.Trace("stage=daily_append memoryId=" + heroId + " memoryName=" + (dailyMemoryDraft.HeroName ?? "") + " day=" + dayIndex + " drafts=" + list.Count + " lines=" + CountDailyMemoryDraftLines(list) + " speaker=" + (dailyMemoryLine.Speaker ?? "") + " isAfef=" + dailyMemoryLine.IsAfef + " isLlm=" + dailyMemoryLine.IsLlmDialogue + " sceneSession=" + sceneSessionId + " dialogueSession=" + dialogueSessionId);
		}
		return dailyPublished;
	}
internal static int CountDailyMemoryDraftLines(IEnumerable<DailyMemoryDraft> drafts)
	{
		try
		{
			return (drafts ?? Enumerable.Empty<DailyMemoryDraft>()).Sum((DailyMemoryDraft draft) => draft?.Lines?.Count ?? 0);
		}
		catch
		{
			return 0;
		}
	}
internal static bool HasMeaningfulConversationHistoryIncludingActiveScene(List<MyBehavior.DialogueDay> records)
	{
		if (records == null || records.Count == 0)
		{
			return false;
		}
		foreach (MyBehavior.DialogueDay record in records)
		{
			if (record?.Lines == null)
			{
				continue;
			}
			foreach (string line in record.Lines)
			{
				if (MemoryBusinessStateOwner.IsMeaningfulConversationLine(line, includeActiveScene: true, isActiveScene: null))
				{
					return true;
				}
			}
		}
		return false;
	}

internal static int GetMemoryCandidateLimitFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return MBMath.ClampInt(settings.MemoryCandidateLimit, 5, 40);
			}
		}
		catch
		{
		}
		return 20;
	}

internal static int GetMemoryFinalInjectCountFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return Math.Min(MBMath.ClampInt(settings.MemoryFinalInjectCount, 2, 20), GetMemoryCandidateLimitFromSettings());
			}
		}
		catch
		{
		}
		return 4;
	}

internal static int GetMemoryPreprocessModeFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null && settings.MemoryPreprocessMode == 2)
			{
				return 2;
			}
		}
		catch
		{
		}
		return 1;
	}

internal long LastMemoryOverviewCandidateScanUtcTicks;
internal void MarkMemoryOverviewDirty(string memoryId)
	{
		try
		{
			string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
			if (!QueuePort.IsEntityEligible(text))
			{
				return;
			}
			DirtyOverviewIds.Add(text);
		}
		catch
		{
		}
	}

internal void EnqueueMemoryOverviewCandidateScanId(string memoryId)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (!QueuePort.IsEntityEligible(text))
		{
			return;
		}
		if (OverviewCandidateIdSet.Add(text))
		{
			OverviewCandidateIds.Enqueue(text);
		}
	}

internal void QueueDirtyMemoryOverviewCandidatesForDeferredScan()
	{
		if (DirtyOverviewIds.Count <= 0)
		{
			return;
		}
		foreach (string memoryId in DirtyOverviewIds.ToList())
		{
			EnqueueMemoryOverviewCandidateScanId(memoryId);
		}
		DirtyOverviewIds.Clear();
	}

internal void QueueAllMemoryOverviewCandidatesForDeferredScan()
	{
		if (Blocks == null || Blocks.Count <= 0)
		{
			return;
		}
		foreach (string memoryId in Blocks.Keys.ToList())
		{
			EnqueueMemoryOverviewCandidateScanId(memoryId);
		}
	}

internal int ProcessMemoryOverviewCandidateScanBudget(long startTimestamp, double budgetMs, Func<long, double, bool> isBudgetExceeded)
	{
		int processed = 0;
		while (OverviewCandidateIds.Count > 0 && !isBudgetExceeded(startTimestamp, budgetMs))
		{
			string memoryId = OverviewCandidateIds.Dequeue();
			OverviewCandidateIdSet.Remove(memoryId);
			string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
			if (string.IsNullOrWhiteSpace(text) || Blocks == null || !Blocks.TryGetValue(text, out var blocks) || blocks == null || blocks.Count <= 0)
			{
				continue;
			}
			string heroName = blocks.Select((CompressedMemoryBlock x) => x?.HeroName).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "NPC";
			TryEnqueueMemoryOverviewForMemoryId(text, heroName, blocks);
			processed++;
		}
		return processed;
	}

internal bool ShouldScanMemoryOverviewCandidates(bool force, double throttleSeconds)
	{
		if (Blocks == null || Blocks.Count <= 0)
		{
			return false;
		}
		if (force)
		{
			LastMemoryOverviewCandidateScanUtcTicks = DateTime.UtcNow.Ticks;
			return true;
		}
		long now = DateTime.UtcNow.Ticks;
		if (now - LastMemoryOverviewCandidateScanUtcTicks < TimeSpan.FromSeconds(throttleSeconds).Ticks)
		{
			return false;
		}
		LastMemoryOverviewCandidateScanUtcTicks = now;
		return true;
	}

internal string BuildHistoryContextById(MemoryHistoryContextReadCapabilities capture, string memoryId, string memoryName, int maxLines = 0, string currentInput = null, string secondaryInput = null, bool includeCurrentActiveSceneSession = false, MyBehavior.HistoryPromptSnapshot snapshot = null)
	{
		string normalizedMemoryId = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (snapshot == null && !capture.IsEligible(normalizedMemoryId))
		{
			return "";
		}
		try
		{
			Stopwatch sw = Stopwatch.StartNew();
			int memoryBlockCount = snapshot?.BlockCount ?? LoadBlocks(normalizedMemoryId).Count;
			int dailyDraftCount = snapshot?.DraftCount ?? LoadDrafts(normalizedMemoryId).Count;
			StringBuilder stringBuilder = new StringBuilder(4096);
			string memoryOverviewContext = snapshot == null ? capture.Overview(normalizedMemoryId) : snapshot.Overview;
			if (!string.IsNullOrWhiteSpace(memoryOverviewContext))
			{
				stringBuilder.AppendLine(memoryOverviewContext);
				stringBuilder.AppendLine();
			}
			string compressedMemoryContext = capture.Compressed(normalizedMemoryId, currentInput, secondaryInput, snapshot);
			if (!string.IsNullOrWhiteSpace(compressedMemoryContext))
			{
				stringBuilder.AppendLine(compressedMemoryContext);
				stringBuilder.AppendLine();
			}
			string text4 = stringBuilder.ToString().TrimEnd();
			sw.Stop();
			Logger.Log("DialogueHistory", string.Format("compressed_context hero={0} chars={1} blocks={2} drafts={3}", normalizedMemoryId, text4.Length, memoryBlockCount, dailyDraftCount));
			Logger.Log("Logic", "[MemoryPerf] history_context_done hero=" + normalizedMemoryId + " blocks=" + memoryBlockCount + " drafts=" + dailyDraftCount + " overviewChars=" + ((memoryOverviewContext ?? "").Length) + " compressedChars=" + ((compressedMemoryContext ?? "").Length) + " totalChars=" + text4.Length + " npcRecall=" + (!string.IsNullOrWhiteSpace(secondaryInput)) + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			if (IsNonHeroMemoryId(normalizedMemoryId))
			{
				capture.Trace("stage=history_context_done memoryId=" + normalizedMemoryId + " blocks=" + memoryBlockCount + " drafts=" + dailyDraftCount + " overviewChars=" + ((memoryOverviewContext ?? "").Length) + " compressedChars=" + ((compressedMemoryContext ?? "").Length) + " totalChars=" + text4.Length + " npcRecall=" + (!string.IsNullOrWhiteSpace(secondaryInput)) + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			}
			Logger.Obs("History", "build_context", new Dictionary<string, object>
			{
				["heroId"] = normalizedMemoryId,
				["memoryBlocks"] = memoryBlockCount,
				["dailyDrafts"] = dailyDraftCount,
				["npcRecall"] = !string.IsNullOrWhiteSpace(secondaryInput),
				["chars"] = text4.Length
			});
			Logger.Metric("history.build_context");
			return text4;
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 构建上下文失败: " + ex.Message);
			Logger.Obs("History", "build_context_error", new Dictionary<string, object>
			{
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("history.build_context", ok: false);
			return "";
		}
	}

internal static int GetMemorySummaryRequestsPerMinuteFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return MBMath.ClampInt(settings.MemorySummaryRequestsPerMinute, 1, 20);
			}
		}
		catch
		{
		}
		return 3;
	}

internal static string ConvertWeekNumberToChineseOrdinal(int weekNumber)
	{
		switch (weekNumber)
		{
		case 1:
			return "第一";
		case 2:
			return "第二";
		case 3:
			return "第三";
		case 4:
			return "第四";
		case 5:
			return "第五";
		case 6:
			return "第六";
		case 7:
			return "第七";
		case 8:
			return "第八";
		case 9:
			return "第九";
		case 10:
			return "第十";
		default:
			return "第" + Math.Max(1, weekNumber);
		}
	}

internal void TryEnqueueMemoryOverviewForAllCandidates()
	{
		using (PerfProbe.Scope("MyBehavior.TryEnqueueMemoryOverviewForAllCandidates"))
		{
		try
		{
			if (Blocks == null || Blocks.Count <= 0)
			{
				return;
			}
			foreach (KeyValuePair<string, List<CompressedMemoryBlock>> item in Blocks.ToList())
			{
				string heroId = MemoryRecordRules.NormalizeMemoryHeroId(item.Key);
				if (string.IsNullOrWhiteSpace(heroId))
				{
					continue;
				}
				string heroName = (item.Value ?? new List<CompressedMemoryBlock>()).Select((CompressedMemoryBlock x) => x?.HeroName).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "NPC";
				TryEnqueueMemoryOverviewForMemoryId(heroId, heroName, item.Value);
			}
			OverviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue(OverviewQueue);
		}
		catch (Exception ex)
		{
			Logger.Log("MemoryOverview", "[ERROR] TryEnqueueMemoryOverviewForAllCandidates failed: " + ex.Message);
		}
		}
	}

internal bool TrySealPastDailyMemoryDrafts(Func<MemorySealingPort> capturePort, long startTimestamp = 0L, double budgetMs = double.MaxValue, bool requirePendingProbe = false)
	{
		try
		{
			if (Drafts == null || (Drafts.Count <= 0 && !Sealing.IsActive))
			{
				Sealing.Reset();
				return true;
			}
			DailyQueue = DailyQueue ?? new List<MemorySummaryJob>();
			MajorQueue = MajorQueue ?? new List<MajorActionSummaryJob>();
			return Sealing.ContinueDailyMemorySeal(startTimestamp, budgetMs, requirePendingProbe, capturePort());
		}
		catch (Exception ex)
		{
			Sealing.Reset();
			Logger.Log("CompressedMemory", "[ERROR] TrySealPastDailyMemoryDrafts failed: " + ex.Message);
			return true;
		}
	}

internal static bool IsMapEventNpcAction(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(text, "map_event", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "map_event_aftermath", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string text2 = (entry?.StableKey ?? "").Trim();
		return text2.StartsWith("mapevent:", StringComparison.OrdinalIgnoreCase) || text2.StartsWith("mapevent_aftermath:", StringComparison.OrdinalIgnoreCase);
	}

internal static bool IsPrisonerTakenAction(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		return string.Equals(text, "prisoner_taken_captor", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "prisoner_taken_prisoner", StringComparison.OrdinalIgnoreCase);
	}

internal static bool IsPrisonerReleasedAction(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		return string.Equals(text, "prisoner_released_captor", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase);
	}

internal static string GetCapturedHeroId(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(text, "prisoner_taken_captor", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.TargetHeroId ?? "").Trim();
		}
		if (string.Equals(text, "prisoner_taken_prisoner", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.ActorHeroId ?? "").Trim();
		}
		return "";
	}

internal static string GetReleasedHeroId(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(text, "prisoner_released_captor", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.TargetHeroId ?? "").Trim();
		}
		if (string.Equals(text, "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.ActorHeroId ?? "").Trim();
		}
		return "";
	}

internal static bool IsArmyCommanderDailyBehavior(NpcActionEntry entry)
	{
		string text = (entry?.StableKey ?? "").Trim();
		if (!text.StartsWith("daily_behavior:army:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text2 = (entry?.Text ?? "").Trim();
		return text2.IndexOf("率领", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static bool IsDailyBehaviorRaid(NpcActionEntry entry)
	{
		string text = (entry?.StableKey ?? "").Trim();
		if (text.IndexOf("raidsettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text2 = (entry?.Text ?? "").Trim();
		return text2.IndexOf("袭扰", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static bool IsDailyBehaviorDefend(NpcActionEntry entry)
	{
		string text = (entry?.StableKey ?? "").Trim();
		if (text.IndexOf("defendsettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text2 = (entry?.Text ?? "").Trim();
		return text2.IndexOf("守备", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("保卫", StringComparison.OrdinalIgnoreCase) >= 0;
	}


internal int CountNonHeroDailyDraftLines()
	{
		try
		{
			return (Drafts ?? new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase)).Where((KeyValuePair<string, List<DailyMemoryDraft>> item) => IsNonHeroMemoryId(item.Key)).Sum((KeyValuePair<string, List<DailyMemoryDraft>> item) => CountDailyMemoryDraftLines(item.Value));
		}
		catch
		{
			return 0;
		}
	}

internal static string StripActionTags(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		text = Regex.Replace(text, "\\[ACTION:[^\\]]*\\]", "");
		text = Regex.Replace(text, "\\[AD:[^\\]]*\\]", "", RegexOptions.IgnoreCase);
		text = Regex.Replace(text, "\\[ADP:[^\\]]*\\]", "", RegexOptions.IgnoreCase);
		text = PartyTransferTagCodec.TroopRegex.Replace(text, "");
		text = PartyTransferTagCodec.PrisonerRegex.Replace(text, "");
		return text.Trim();
	}

internal static bool ContainsIgnoreCase(string text, string token)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
		{
			return false;
		}
		return text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static bool IsNpcAskingForConfirmation(string npcText)
	{
		string text = StripActionTags(npcText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!text.Contains("？") && !text.Contains("?"))
		{
			return false;
		}
		string[] array = new string[13]
		{
			"是否", "要不要", "你要", "你是要", "确认", "同意", "愿意", "还是", "雇佣兵", "封臣",
			"成交吗", "继续吗", "要这样吗"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (ContainsIgnoreCase(text, array[i]))
			{
				return true;
			}
		}
		return false;
	}

internal static PromptTopicSemanticEvaluator CreateBuiltInTopicSemanticEvaluator(string tag, string input, string secondaryInput, HashSet<string> excludedRuleIdSet)
	{
		return (string ruleTag, out string matchedKeyword, out float score) =>
		{
			string instruction;
			List<string> keywords;
			switch (ruleTag)
			{
			case "duel": instruction = AIConfigHandler.DuelInstruction; keywords = AIConfigHandler.DuelTriggerKeywords; break;
			case "reward": instruction = AIConfigHandler.RewardInstruction; keywords = AIConfigHandler.RewardTriggerKeywords; break;
			case "loan": instruction = AIConfigHandler.LoanInstruction; keywords = AIConfigHandler.LoanTriggerKeywords; break;
			case "surroundings": instruction = AIConfigHandler.SurroundingsInstruction; keywords = AIConfigHandler.SurroundingsTriggerKeywords; break;
			default: instruction = AIConfigHandler.GetGuardrailRuleInstruction(ruleTag); keywords = AIConfigHandler.GetGuardrailRuleKeywords(ruleTag); break;
			}
			return AIConfigHandler.IsGuardrailSemanticHit(input, secondaryInput, ruleTag, instruction, keywords, out matchedKeyword, out score, excludedRuleIdSet);
		};
	}


internal static int ClampRecentDialogueTurns(int turns)
	{
		if (turns < 1)
		{
			return 1;
		}
		if (turns > 80)
		{
			return 80;
		}
		return turns;
	}

internal static int GetRecentDialogueTurnsFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return ClampRecentDialogueTurns(settings.RecentDialogueTurns);
			}
		}
		catch
		{
		}
		return 20;
	}


internal static string EncounterDiagEscape(string text)
	{
		return (text ?? "null").Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
	}


internal static void AppendPlayerExtraFactLine(StringBuilder sb, string extraFact)
	{
		if (sb == null)
		{
			return;
		}
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
			{
				sb.AppendLine(text);
			}
			else
			{
				sb.AppendLine("[AFEF玩家行为补充] " + text);
			}
		}
	}

internal static List<string> BuildRenderedHistoryLines(List<HistoryLineEntry> entries, string targetDisplayName = null, bool addressToYou = true)
	{
		List<string> list = new List<string>();
		if (entries == null || entries.Count == 0)
		{
			return list;
		}
		int num = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			HistoryLineEntry historyLineEntry = entries[i];
			if (historyLineEntry != null && !string.IsNullOrWhiteSpace(historyLineEntry.Line))
			{
				if (historyLineEntry.Day != 0 && historyLineEntry.Day != num)
				{
					num = historyLineEntry.Day;
					string text = ((!string.IsNullOrWhiteSpace(historyLineEntry.Date)) ? historyLineEntry.Date : $"第 {historyLineEntry.Day} 日");
					list.Add("—— " + text + " ——");
				}
				list.Add(AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.NormalizePlayerHistoryLineForPrompt(historyLineEntry.Line, targetDisplayName, addressToYou));
			}
		}
		return list;
	}

internal static bool IsCurrentInputPlayerLine(string line, string currentInput)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(currentInput))
		{
			return false;
		}
		if (!AnimusForge.Refactor.Adapters.MemoryRecallInputCaptureAdapter.TryStripPlayerSpeechPrefix(line, out var text))
		{
			return false;
		}
		string b = currentInput.Trim();
		return string.Equals(text, b, StringComparison.Ordinal);
	}
}

// Each effect is one engine/cross-domain operation, not a callback to host memory rules.
internal sealed class MemoryDailyCommitEffects
{
    internal Action<string> MarkOverviewDirty;
    internal Action<int> ClearNativeHistory;
    internal Action LogSuccess;
    internal Action<CompressedMemoryBlock> RecordPublicMemory;
    internal Action<CompressedMemoryBlock> RecordPublicWeeklyMaterial;
    internal Action<CompressedMemoryBlock> RecordWeeklyMaterial;
    internal Action<string, string, List<CompressedMemoryBlock>> EnqueueOverview;

}

internal sealed class MemoryDailyAppendCapabilities
{
 internal readonly Func<string,bool> IsEligible;
 internal readonly Func<int> Day,Hour,NativeSession,CurrentDay;
 internal readonly Func<string> Date,Scene;
 internal readonly Action<string> NoteConversation,Trace;
 internal MemoryDailyAppendCapabilities(Func<string,bool> eligible,Func<int> day,Func<string> date,Func<int> hour,Func<int> native,Func<string> scene,Func<int> currentDay,Action<string> note,Action<string> trace)
 {IsEligible=eligible;Day=day;Date=date;Hour=hour;NativeSession=native;Scene=scene;CurrentDay=currentDay;NoteConversation=note;Trace=trace;}
}

internal sealed class MemoryHistoryContextReadCapabilities
{
 internal Func<string,bool> IsEligible;
 internal Func<string,string> Overview;
 internal Func<string,string,string,MyBehavior.HistoryPromptSnapshot,string> Compressed;
 internal Action<string> Trace;
}
