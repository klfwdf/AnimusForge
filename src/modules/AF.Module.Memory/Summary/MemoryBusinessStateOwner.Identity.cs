using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;
internal sealed partial class MemoryBusinessStateOwner
{
internal static List<DailyMemoryDraft> RetargetDailyMemoryDrafts(IEnumerable<DailyMemoryDraft> drafts, string targetMemoryId)
	{
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		foreach (DailyMemoryDraft draft in drafts ?? Enumerable.Empty<DailyMemoryDraft>())
		{
			if (draft == null)
			{
				continue;
			}
			draft.HeroId = target;
			foreach (WeeklyMemoryMaterialTrigger trigger in draft.WeeklyMaterialTriggers ?? new List<WeeklyMemoryMaterialTrigger>())
			{
				if (trigger != null)
				{
					trigger.MemoryId = target;
				}
			}
		}
		return MemoryRecordRules.SanitizeDailyMemoryDrafts(drafts);
	}

internal static List<DailyMemoryDraft> MergeDailyMemoryDraftLists(IEnumerable<DailyMemoryDraft> targetDrafts, IEnumerable<DailyMemoryDraft> sourceDrafts, string targetMemoryId)
	{
		Dictionary<int, DailyMemoryDraft> byDay = new Dictionary<int, DailyMemoryDraft>();
		void AddDrafts(IEnumerable<DailyMemoryDraft> drafts)
		{
			foreach (DailyMemoryDraft draft in RetargetDailyMemoryDrafts(drafts, targetMemoryId))
			{
				if (!byDay.TryGetValue(draft.GameDayIndex, out var merged))
				{
					byDay[draft.GameDayIndex] = draft;
					continue;
				}
				if (string.IsNullOrWhiteSpace(merged.HeroName))
				{
					merged.HeroName = draft.HeroName;
				}
				if (string.IsNullOrWhiteSpace(merged.GameDate))
				{
					merged.GameDate = draft.GameDate;
				}
				merged.HasLlmDialogue = merged.HasLlmDialogue || draft.HasLlmDialogue;
				merged.QueuedForSummary = merged.QueuedForSummary || draft.QueuedForSummary;
				merged.SummaryRetryCount = Math.Max(merged.SummaryRetryCount, draft.SummaryRetryCount);
				if (string.IsNullOrWhiteSpace(merged.LastSummaryError))
				{
					merged.LastSummaryError = draft.LastSummaryError;
				}
				merged.Lines.AddRange(draft.Lines ?? new List<DailyMemoryLine>());
				merged.WeeklyMaterialTriggers.AddRange(draft.WeeklyMaterialTriggers ?? new List<WeeklyMemoryMaterialTrigger>());
			}
		}
		AddDrafts(targetDrafts);
		AddDrafts(sourceDrafts);
		return MemoryRecordRules.SanitizeDailyMemoryDrafts(byDay.Values);
	}

internal static List<CompressedMemoryBlock> RetargetCompressedMemoryBlocks(IEnumerable<CompressedMemoryBlock> blocks, string targetMemoryId)
	{
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		foreach (CompressedMemoryBlock block in blocks ?? Enumerable.Empty<CompressedMemoryBlock>())
		{
			if (block == null)
			{
				continue;
			}
			block.HeroId = target;
			block.Id = MemoryRecordRules.BuildCompressedMemoryBlockId(target, block.GameDayIndex);
			foreach (WeeklyMemoryMaterialTrigger trigger in block.WeeklyMaterialTriggers ?? new List<WeeklyMemoryMaterialTrigger>())
			{
				if (trigger != null)
				{
					trigger.MemoryId = target;
				}
			}
		}
		return MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks);
	}

internal static List<CompressedMemoryBlock> MergeCompressedMemoryBlockLists(IEnumerable<CompressedMemoryBlock> targetBlocks, IEnumerable<CompressedMemoryBlock> sourceBlocks, string targetMemoryId)
	{
		Dictionary<int, CompressedMemoryBlock> byDay = new Dictionary<int, CompressedMemoryBlock>();
		void AddBlocks(IEnumerable<CompressedMemoryBlock> blocks)
		{
			foreach (CompressedMemoryBlock block in RetargetCompressedMemoryBlocks(blocks, targetMemoryId))
			{
				if (!byDay.TryGetValue(block.GameDayIndex, out var merged))
				{
					byDay[block.GameDayIndex] = block;
					continue;
				}
				if (string.IsNullOrWhiteSpace(merged.HeroName))
				{
					merged.HeroName = block.HeroName;
				}
				if (string.IsNullOrWhiteSpace(merged.GameDate))
				{
					merged.GameDate = block.GameDate;
				}
				merged.StartHour = Math.Min(merged.StartHour, block.StartHour);
				merged.EndHour = Math.Max(merged.EndHour, block.EndHour);
				merged.Scenes.AddRange(block.Scenes ?? new List<string>());
				if (string.IsNullOrWhiteSpace(merged.RichTitle))
				{
					merged.RichTitle = block.RichTitle;
				}
				merged.Summary = MergeDistinctTextBlocks(merged.Summary, block.Summary);
				merged.AfefLines.AddRange(block.AfefLines ?? new List<string>());
				merged.PlayerPublicity = MergeDistinctTextBlocks(merged.PlayerPublicity, block.PlayerPublicity);
				merged.PlayerHistoryMaterial = MergeDistinctTextBlocks(merged.PlayerHistoryMaterial, block.PlayerHistoryMaterial);
				merged.PlayerPublicityReason = MergeDistinctTextBlocks(merged.PlayerPublicityReason, block.PlayerPublicityReason);
				merged.WeeklyMaterialTriggers.AddRange(block.WeeklyMaterialTriggers ?? new List<WeeklyMemoryMaterialTrigger>());
				if (merged.CreatedUtcTicks <= 0L || (block.CreatedUtcTicks > 0L && block.CreatedUtcTicks < merged.CreatedUtcTicks))
				{
					merged.CreatedUtcTicks = block.CreatedUtcTicks;
				}
			}
		}
		AddBlocks(targetBlocks);
		AddBlocks(sourceBlocks);
		return MemoryRecordRules.SanitizeCompressedMemoryBlocks(byDay.Values);
	}

internal static string MergeDistinctTextBlocks(string first, string second)
	{
		string left = (first ?? "").Trim();
		string right = (second ?? "").Trim();
		if (string.IsNullOrWhiteSpace(left))
		{
			return right;
		}
		if (string.IsNullOrWhiteSpace(right) || string.Equals(left, right, StringComparison.Ordinal))
		{
			return left;
		}
		if (left.IndexOf(right, StringComparison.Ordinal) >= 0)
		{
			return left;
		}
		if (right.IndexOf(left, StringComparison.Ordinal) >= 0)
		{
			return right;
		}
		return left + "\n" + right;
	}

internal void RetargetMemoryQueues(string sourceMemoryId, string targetMemoryId)
	{
		string source = MemoryRecordRules.NormalizeMemoryHeroId(sourceMemoryId);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		bool changedSummaryQueue = false;
		foreach (MemorySummaryJob job in DailyQueue ?? new List<MemorySummaryJob>())
		{
			if (job != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId), source, StringComparison.OrdinalIgnoreCase))
			{
				job.HeroId = target;
				changedSummaryQueue = true;
			}
		}
		if (changedSummaryQueue)
		{
			DailyQueue = MemoryRecordRules.SanitizeMemorySummaryQueue(DailyQueue);
		}
		bool changedTriggers = false;
		foreach (WeeklyMemoryMaterialTrigger trigger in PendingWeeklyTriggers ?? new List<WeeklyMemoryMaterialTrigger>())
		{
			if (trigger != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(trigger.MemoryId), source, StringComparison.OrdinalIgnoreCase))
			{
				trigger.MemoryId = target;
				changedTriggers = true;
			}
		}
		if (changedTriggers)
		{
			PendingWeeklyTriggers = MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(PendingWeeklyTriggers);
		}
		bool changedOverviewQueue = false;
		foreach (MemoryOverviewJob job2 in OverviewQueue ?? new List<MemoryOverviewJob>())
		{
			if (job2 != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(job2.HeroId), source, StringComparison.OrdinalIgnoreCase))
			{
				job2.HeroId = target;
				changedOverviewQueue = true;
			}
		}
		if (changedOverviewQueue)
		{
			OverviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue(OverviewQueue);
		}
		bool changedMajorQueue = false;
		foreach (MajorActionSummaryJob job3 in MajorQueue ?? new List<MajorActionSummaryJob>())
		{
			if (job3 != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(job3.HeroId), source, StringComparison.OrdinalIgnoreCase))
			{
				job3.HeroId = target;
				changedMajorQueue = true;
			}
		}
		if (changedMajorQueue)
		{
			MajorQueue = MemoryRecordRules.SanitizeMajorActionSummaryQueue(MajorQueue);
		}
	}

internal void MergeMemoryOverviewStateById(string sourceMemoryId, string targetMemoryId)
	{
		string source = MemoryRecordRules.NormalizeMemoryHeroId(sourceMemoryId);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		if (Overviews == null || !Overviews.TryGetValue(source, out var sourceState) || sourceState == null)
		{
			return;
		}
		Overviews.TryGetValue(target, out var targetState);
		if (targetState == null)
		{
			sourceState.HeroId = target;
			Overviews[target] = MemoryRecordRules.SanitizeMemoryOverviewState(sourceState);
			return;
		}
		targetState.HeroId = target;
		if (string.IsNullOrWhiteSpace(targetState.HeroName))
		{
			targetState.HeroName = sourceState.HeroName;
		}
		targetState.Summary = MergeDistinctTextBlocks(targetState.Summary, sourceState.Summary);
		targetState.IncludedBlockIds = (targetState.IncludedBlockIds ?? new List<string>()).Concat(sourceState.IncludedBlockIds ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		targetState.UpdatedUtcTicks = Math.Max(targetState.UpdatedUtcTicks, sourceState.UpdatedUtcTicks);
		if (string.IsNullOrWhiteSpace(targetState.LastError))
		{
			targetState.LastError = sourceState.LastError;
		}
		Overviews[target] = MemoryRecordRules.SanitizeMemoryOverviewState(targetState);
	}

internal void MergeMajorActionSummaryStateById(string sourceMemoryId, string targetMemoryId)
	{
		string source = MemoryRecordRules.NormalizeMemoryHeroId(sourceMemoryId);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		if (MajorSummaries == null || !MajorSummaries.TryGetValue(source, out var sourceState) || sourceState == null)
		{
			return;
		}
		MajorSummaries.TryGetValue(target, out var targetState);
		if (targetState == null)
		{
			sourceState.HeroId = target;
			MajorSummaries[target] = MemoryRecordRules.SanitizeMajorActionSummaryState(sourceState);
			return;
		}
		targetState.HeroId = target;
		if (string.IsNullOrWhiteSpace(targetState.HeroName))
		{
			targetState.HeroName = sourceState.HeroName;
		}
		targetState.Summary = MergeDistinctTextBlocks(targetState.Summary, sourceState.Summary);
		targetState.LastSummarizedDay = Math.Max(targetState.LastSummarizedDay, sourceState.LastSummarizedDay);
		targetState.LastSummarizedSequence = Math.Max(targetState.LastSummarizedSequence, sourceState.LastSummarizedSequence);
		targetState.UpdatedUtcTicks = Math.Max(targetState.UpdatedUtcTicks, sourceState.UpdatedUtcTicks);
		if (string.IsNullOrWhiteSpace(targetState.LastError))
		{
			targetState.LastError = sourceState.LastError;
		}
		MajorSummaries[target] = MemoryRecordRules.SanitizeMajorActionSummaryState(targetState);
	}

internal void RetargetMemoryOverviewCandidateScanIds(string sourceMemoryId, string targetMemoryId)
	{
		string source = MemoryRecordRules.NormalizeMemoryHeroId(sourceMemoryId);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		if (DirtyOverviewIds != null && DirtyOverviewIds.Remove(source))
		{
			DirtyOverviewIds.Add(target);
		}
		if (OverviewCandidateIdSet != null && OverviewCandidateIdSet.Remove(source))
		{
			OverviewCandidateIdSet.Add(target);
		}
		if (OverviewCandidateIds == null || OverviewCandidateIds.Count <= 0)
		{
			return;
		}
		List<string> ids = OverviewCandidateIds.Select((string x) => string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x), source, StringComparison.OrdinalIgnoreCase) ? target : MemoryRecordRules.NormalizeMemoryHeroId(x)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		OverviewCandidateIds.Clear();
		foreach (string id in ids)
		{
			OverviewCandidateIds.Enqueue(id);
		}
	}
}
