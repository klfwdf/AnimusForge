using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;
internal static partial class MemoryDeveloperEditOwner
{
    internal static void DeleteDailyDraft(MemoryBusinessStateOwner state, MemoryDailyDeveloperEditContext context,
        MemoryDailyDeveloperEditEffects effects, int day)
    {
        var drafts = state.LoadDrafts(context.HeroId);
        var previous = CloneDevDailyMemoryLines(drafts.FirstOrDefault(x => x != null && x.GameDayIndex == day)?.Lines);
        drafts.RemoveAll(x => x != null && x.GameDayIndex == day);
        SaveDevDailyMemoryDraftsAfterEdit(state, context, effects, drafts, day, "delete_draft", previous);
    }

    internal static void ClearMemoryWithHistorySync(MemoryBusinessStateOwner state,
        MemoryDailyDeveloperEditContext context, MemoryImportExportState exportState,
        Func<int, MemoryDailyDeveloperEditEffects> effectsForDay)
    {
        var oldDrafts = MemoryRecordRules.SanitizeDailyMemoryDrafts(state.LoadDrafts(context.HeroId));
        foreach (var draft in oldDrafts)
            SyncDialogueHistoryForDailyMemoryDraftEdit(context, effectsForDay(draft.GameDayIndex), draft.GameDayIndex,
                CloneDevDailyMemoryLines(draft.Lines), new List<DailyMemoryLine>(), "clear_compressed_memory");
        Clear(context.HeroId, exportState);
    }

    internal static void ClearDialogueHistory(MemoryBusinessStateOwner state, string rawHeroId, Action clearNativeHistory)
    {
        state.History ??= new Dictionary<string, List<MyBehavior.DialogueDay>>();
        if (!string.IsNullOrEmpty(rawHeroId)) state.History.Remove(rawHeroId);
        // Explicit user-requested full clear also removes commit marker-only days.
        // Preserve deletion before the native effect, including partial failure.
        clearNativeHistory();
    }

    internal static bool MutateDailyLine(MemoryBusinessStateOwner state, MemoryDailyDeveloperEditContext context,
        MemoryDailyDeveloperEditEffects effects, int day, int lineIndex, Action<DailyMemoryDraft, DailyMemoryLine> mutate)
    {
        var drafts = state.LoadDrafts(context.HeroId);
        var draft = drafts.FirstOrDefault(x => x != null && x.GameDayIndex == day);
        if (draft?.Lines == null || lineIndex < 0 || lineIndex >= draft.Lines.Count) return false;
        var previous = CloneDevDailyMemoryLines(draft.Lines);
        mutate?.Invoke(draft, draft.Lines[lineIndex]);
        SaveDevDailyMemoryDraftsAfterEdit(state, context, effects, drafts, day, "edit_line", previous);
        return true;
    }

    internal static bool MutateDailyDraft(MemoryBusinessStateOwner state, MemoryDailyDeveloperEditContext context,
        MemoryDailyDeveloperEditEffects effects, int day, Action<DailyMemoryDraft> mutate, string reason)
    {
        var drafts = state.LoadDrafts(context.HeroId);
        var draft = drafts.FirstOrDefault(x => x != null && x.GameDayIndex == day);
        if (draft == null) return false;
        var previous = CloneDevDailyMemoryLines(draft.Lines);
        mutate?.Invoke(draft);
        SaveDevDailyMemoryDraftsAfterEdit(state, context, effects, drafts, day, reason, previous);
        return true;
    }
internal static void SaveDevDailyMemoryDraftsAfterEdit(MemoryBusinessStateOwner state, MemoryDailyDeveloperEditContext context, MemoryDailyDeveloperEditEffects effects, List<DailyMemoryDraft> drafts, int affectedDayIndex, string reason, List<DailyMemoryLine> previousLines = null)
	{
		string heroId = context.HeroId;
		foreach (DailyMemoryDraft draft in drafts ?? new List<DailyMemoryDraft>())
		{
			NormalizeDevDailyMemoryDraftForSave(context.HeroId, context.HeroName, draft);
		}
		state.SaveDrafts(context.HeroId, drafts);
		if (previousLines != null)
		{
			DailyMemoryDraft currentDraft = state.LoadDrafts(context.HeroId).FirstOrDefault(x => x != null && x.GameDayIndex == affectedDayIndex);
			SyncDialogueHistoryForDailyMemoryDraftEdit(context, effects, affectedDayIndex, previousLines, CloneDevDailyMemoryLines(currentDraft?.Lines), reason);
		}
		if (state.LoadDrafts(context.HeroId).FirstOrDefault(x => x != null && x.GameDayIndex == affectedDayIndex) == null && state.DailyQueue != null)
		{
			state.DailyQueue.RemoveAll((MemorySummaryJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase) && x.GameDayIndex == affectedDayIndex);
		}
		effects.Log( "manual_raw_edit hero=" + heroId + " day=" + affectedDayIndex + " reason=" + (reason ?? ""));
	}

internal static void SyncDialogueHistoryForDailyMemoryDraftEdit(MemoryDailyDeveloperEditContext context, MemoryDailyDeveloperEditEffects effects, int affectedDayIndex, IEnumerable<DailyMemoryLine> previousLines, IEnumerable<DailyMemoryLine> currentLines, string reason)
	{
		if (!context.HasHero || affectedDayIndex < 0 || previousLines == null)
		{
			return;
		}
		try
		{
			List<DailyMemoryLine> oldLines = CloneDevDailyMemoryLines(previousLines);
			List<DailyMemoryLine> newLines = CloneDevDailyMemoryLines(currentLines);
			Dictionary<string, int> oldCounts = BuildDailyMemorySyncLineCounts(oldLines);
			Dictionary<string, int> newCounts = BuildDailyMemorySyncLineCounts(newLines);
			Dictionary<string, int> removeCounts = BuildDailyMemorySyncCountDelta(oldCounts, newCounts);
			Dictionary<string, int> addCounts = BuildDailyMemorySyncCountDelta(newCounts, oldCounts);
			effects.SyncNativeHistory(oldLines, newLines);
			if (removeCounts.Count == 0 && addCounts.Count == 0)
			{
				return;
			}
			List<MyBehavior.DialogueDay> records = effects.LoadHistory();
			MyBehavior.DialogueDay day = records.FirstOrDefault((MyBehavior.DialogueDay x) => x != null && x.GameDayIndex == affectedDayIndex);
			int removedCount = 0;
			if (day != null)
			{
				removedCount = RemoveDialogueHistoryLinesByCounts(day, removeCounts);
				if ((day.Lines == null || day.Lines.Count == 0)
					&& (day.MemoryCommitMarkers == null || day.MemoryCommitMarkers.Count == 0))
				{
					records.Remove(day);
					day = null;
				}
			}
			List<string> addedLines = BuildDailyMemorySyncAddedDialogueLines(newLines, addCounts);
			if (addedLines.Count > 0)
			{
				if (day == null)
				{
					day = new MyBehavior.DialogueDay
					{
						GameDayIndex = affectedDayIndex,
						GameDate = ResolveDailyMemorySyncGameDate(affectedDayIndex, newLines, oldLines, effects.ResolveGameDate)
					};
					records.Add(day);
				}
				if (day.Lines == null)
				{
					day.Lines = new List<string>();
				}
				day.Lines.AddRange(addedLines);
			}
			if (removedCount > 0 || addedLines.Count > 0)
			{
				records = records.Where((MyBehavior.DialogueDay x) => x != null
					&& ((x.Lines != null && x.Lines.Count > 0)
						|| (x.MemoryCommitMarkers != null && x.MemoryCommitMarkers.Count > 0)))
					.OrderBy((MyBehavior.DialogueDay x) => x.GameDayIndex).ToList();
				effects.SaveHistory(records);
				effects.Log( "sync_raw_edit_dialogue_history hero=" + context.HeroId + " day=" + affectedDayIndex + " removed=" + removedCount + " added=" + addedLines.Count + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			effects.Log( "[WARN] sync_raw_edit_dialogue_history failed: " + ex.Message);
		}
	}

internal static List<DailyMemoryLine> CloneDevDailyMemoryLines(IEnumerable<DailyMemoryLine> lines)
	{
		List<DailyMemoryLine> result = new List<DailyMemoryLine>();
		foreach (DailyMemoryLine line in lines ?? Enumerable.Empty<DailyMemoryLine>())
		{
			if (line == null)
			{
				continue;
			}
			result.Add(new DailyMemoryLine
			{
				GameDayIndex = line.GameDayIndex,
				GameDate = line.GameDate ?? "",
				GameHour = line.GameHour,
				Scene = line.Scene ?? "",
				Speaker = line.Speaker ?? "",
				Text = line.Text ?? "",
				SceneSessionId = line.SceneSessionId,
				DialogueSessionId = line.DialogueSessionId,
				TargetAgentIndex = line.TargetAgentIndex,
				TargetName = line.TargetName ?? "",
				MemorySessionKey = line.MemorySessionKey ?? "",
				IsAfef = line.IsAfef,
				IsLlmDialogue = line.IsLlmDialogue
			});
		}
		return result;
	}

internal static Dictionary<string, int> BuildDailyMemorySyncLineCounts(IEnumerable<DailyMemoryLine> lines)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (DailyMemoryLine line in lines ?? Enumerable.Empty<DailyMemoryLine>())
		{
			string key = NormalizeDialogueHistoryLineForDailyMemorySync(line?.Text);
			if (string.IsNullOrWhiteSpace(key))
			{
				continue;
			}
			if (!result.ContainsKey(key))
			{
				result[key] = 0;
			}
			result[key]++;
		}
		return result;
	}

internal static Dictionary<string, int> BuildDailyMemorySyncCountDelta(Dictionary<string, int> source, Dictionary<string, int> target)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, int> item in source ?? new Dictionary<string, int>(StringComparer.Ordinal))
		{
			int targetCount = 0;
			target?.TryGetValue(item.Key, out targetCount);
			int count = item.Value - targetCount;
			if (count > 0)
			{
				result[item.Key] = count;
			}
		}
		return result;
	}

internal static int RemoveDialogueHistoryLinesByCounts(MyBehavior.DialogueDay day, Dictionary<string, int> removeCounts)
	{
		if (day?.Lines == null || removeCounts == null || removeCounts.Count == 0)
		{
			return 0;
		}
		int removed = 0;
		for (int i = 0; i < day.Lines.Count; i++)
		{
			string key = NormalizeDialogueHistoryLineForDailyMemorySync(day.Lines[i]);
			if (string.IsNullOrWhiteSpace(key) || !removeCounts.TryGetValue(key, out var remaining) || remaining <= 0)
			{
				continue;
			}
			day.Lines.RemoveAt(i);
			i--;
			removed++;
			removeCounts[key] = remaining - 1;
		}
		return removed;
	}

internal static List<string> BuildDailyMemorySyncAddedDialogueLines(IEnumerable<DailyMemoryLine> currentLines, Dictionary<string, int> addCounts)
	{
		List<string> result = new List<string>();
		if (addCounts == null || addCounts.Count == 0)
		{
			return result;
		}
		foreach (DailyMemoryLine line in currentLines ?? Enumerable.Empty<DailyMemoryLine>())
		{
			string key = NormalizeDialogueHistoryLineForDailyMemorySync(line?.Text);
			if (string.IsNullOrWhiteSpace(key) || !addCounts.TryGetValue(key, out var remaining) || remaining <= 0)
			{
				continue;
			}
			result.Add(BuildDialogueHistoryLineForDailyMemorySync(line));
			addCounts[key] = remaining - 1;
		}
		return result;
	}

internal static string BuildDialogueHistoryLineForDailyMemorySync(DailyMemoryLine line)
	{
		string text = NormalizeDialogueHistoryLineForDailyMemorySync(line?.Text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int sceneSessionId = line?.SceneSessionId ?? -1;
		return sceneSessionId >= 0 ? DialogueHistoryLedger.TagSceneSession(text, sceneSessionId) : text;
	}

internal static string NormalizeDialogueHistoryLineForDailyMemorySync(string line)
	{
		string text = (line ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		return (text ?? "").Replace("\r", "").Trim();
	}

internal static string ResolveDailyMemorySyncGameDate(int dayIndex, IEnumerable<DailyMemoryLine> currentLines, IEnumerable<DailyMemoryLine> previousLines, Func<int, string> fallbackDate)
	{
		foreach (DailyMemoryLine line in currentLines ?? Enumerable.Empty<DailyMemoryLine>())
		{
			if (line != null && line.GameDayIndex == dayIndex && !string.IsNullOrWhiteSpace(line.GameDate))
			{
				return line.GameDate.Trim();
			}
		}
		foreach (DailyMemoryLine line in previousLines ?? Enumerable.Empty<DailyMemoryLine>())
		{
			if (line != null && line.GameDayIndex == dayIndex && !string.IsNullOrWhiteSpace(line.GameDate))
			{
				return line.GameDate.Trim();
			}
		}
        return fallbackDate?.Invoke(dayIndex) ?? "";
    }

internal static void NormalizeDevDailyMemoryDraftForSave(string heroId, string heroName, DailyMemoryDraft draft)
	{
		if (draft == null)
		{
			return;
		}
		draft.HeroId = heroId;
		draft.HeroName = heroName ?? draft.HeroName ?? "";
		draft.GameDate = (draft.GameDate ?? "").Trim();
		draft.LastSummaryError = (draft.LastSummaryError ?? "").Trim();
		if (draft.WeeklyMaterialTriggers != null)
		{
			foreach (WeeklyMemoryMaterialTrigger trigger in draft.WeeklyMaterialTriggers)
			{
				if (trigger != null)
				{
					trigger.MemoryId = draft.HeroId;
					trigger.GameDayIndex = draft.GameDayIndex;
					trigger.GameDate = string.IsNullOrWhiteSpace(trigger.GameDate) ? draft.GameDate : trigger.GameDate;
				}
			}
		}
		draft.WeeklyMaterialTriggers = MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(draft.WeeklyMaterialTriggers);
		if (draft.Lines == null)
		{
			draft.Lines = new List<DailyMemoryLine>();
		}
		foreach (DailyMemoryLine line in draft.Lines)
		{
			if (line == null)
			{
				continue;
			}
			line.GameDayIndex = draft.GameDayIndex;
			line.GameDate = string.IsNullOrWhiteSpace(line.GameDate) ? draft.GameDate : line.GameDate.Trim();
			line.GameHour = Math.Min(23, Math.Max(0, line.GameHour));
			line.Scene = (line.Scene ?? "").Trim();
			line.Speaker = (line.Speaker ?? "").Trim();
			line.Text = NormalizeMultiline(line.Text);
			line.MemorySessionKey = (line.MemorySessionKey ?? "").Trim();
			if (line.IsAfef)
			{
				line.IsLlmDialogue = false;
			}
			if (string.IsNullOrWhiteSpace(line.Speaker))
			{
				line.Speaker = line.IsAfef ? "AFEF" : "手动";
			}
		}
		draft.HasLlmDialogue = draft.Lines.Any((DailyMemoryLine x) => x != null && x.IsLlmDialogue && !x.IsAfef && !string.IsNullOrWhiteSpace(x.Text));
	}
}

internal sealed class MemoryDailyDeveloperEditContext
{
    internal string HeroId, HeroName;
    internal bool HasHero;
}
internal sealed class MemoryDailyDeveloperEditEffects
{
    internal Func<List<MyBehavior.DialogueDay>> LoadHistory;
    internal Action<List<MyBehavior.DialogueDay>> SaveHistory;
    internal Action<List<DailyMemoryLine>, List<DailyMemoryLine>> SyncNativeHistory;
    internal Func<int, string> ResolveGameDate;
    internal Action<string> Log;
}
