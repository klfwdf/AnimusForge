using System;using System.Collections.Generic;using System.Linq;
namespace AnimusForge;
internal sealed partial class MemoryBusinessStateOwner { internal MemoryIdentityPort IdentityPort;
internal Dictionary<string, List<MyBehavior.DialogueDay>> History = new(StringComparer.OrdinalIgnoreCase);
internal Dictionary<string, string> HistoryStorage = new(StringComparer.OrdinalIgnoreCase);
internal Dictionary<string, string> DraftStorage = new(StringComparer.OrdinalIgnoreCase);
internal Dictionary<string, string> BlockStorage = new(StringComparer.OrdinalIgnoreCase);
internal Dictionary<string, string> MajorActionStorage = new(StringComparer.OrdinalIgnoreCase);
internal Dictionary<string, string> RecentActionStorage = new(StringComparer.OrdinalIgnoreCase);
internal Dictionary<string, List<NpcActionEntry>> RecentActions = new(StringComparer.OrdinalIgnoreCase);
internal void MergeMemoryEntityDataById(string sourceMemoryId, string targetMemoryId)
	{
		string source = MemoryRecordRules.NormalizeMemoryHeroId(sourceMemoryId);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (History != null && History.TryGetValue(source, out var sourceHistory) && sourceHistory != null && sourceHistory.Count > 0)
		{
			IdentityPort.SaveHistory(target, MergeDialogueDayLists(IdentityPort.LoadHistory(target), sourceHistory));
		}
		if (Drafts != null && Drafts.TryGetValue(source, out var sourceDrafts) && sourceDrafts != null && sourceDrafts.Count > 0)
		{
			SaveDrafts(target, MergeDailyMemoryDraftLists(LoadDrafts(target), sourceDrafts, target));
		}
		if (Blocks != null && Blocks.TryGetValue(source, out var sourceBlocks) && sourceBlocks != null && sourceBlocks.Count > 0)
		{
			SaveBlocks(target, MergeCompressedMemoryBlockLists(LoadBlocks(target), sourceBlocks, target), IdentityPort.MarkOverviewDirty);
		}
		RetargetMemoryQueues(source, target);
		MergeMemoryOverviewStateById(source, target);
		MergeMajorActionSummaryStateById(source, target);
		MergeNpcActionStorageById(MajorActions, source, target, keepOnlyRecentWindow: false);
		MergeNpcActionStorageById(RecentActions, source, target, keepOnlyRecentWindow: true);
		RetargetMemoryOverviewCandidateScanIds(source, target);
		IdentityPort.Recovery().RetargetInteractionMemoryRecoveryProjection(source, target);
		RemoveMemoryEntityDataById(source);
	}
internal static List<MyBehavior.DialogueDay> MergeDialogueDayLists(IEnumerable<MyBehavior.DialogueDay> targetDays, IEnumerable<MyBehavior.DialogueDay> sourceDays)
	{
		Dictionary<int, MyBehavior.DialogueDay> byDay = new Dictionary<int, MyBehavior.DialogueDay>();
		void AddDays(IEnumerable<MyBehavior.DialogueDay> days)
		{
			foreach (MyBehavior.DialogueDay day in days ?? Enumerable.Empty<MyBehavior.DialogueDay>())
			{
				if (day == null)
				{
					continue;
				}
				int dayIndex = Math.Max(0, day.GameDayIndex);
				if (!byDay.TryGetValue(dayIndex, out var merged))
				{
					merged = new MyBehavior.DialogueDay
					{
						GameDayIndex = dayIndex,
						GameDate = (day.GameDate ?? "").Trim(),
						Lines = new List<string>(),
						MemoryCommitMarkers = new Dictionary<string, string>(StringComparer.Ordinal)
					};
					byDay[dayIndex] = merged;
				}
				if (string.IsNullOrWhiteSpace(merged.GameDate))
				{
					merged.GameDate = (day.GameDate ?? "").Trim();
				}
				foreach (string line in day.Lines ?? new List<string>())
				{
					string text = (line ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !merged.Lines.Contains(text, StringComparer.Ordinal))
					{
						merged.Lines.Add(text);
					}
				}
				foreach (KeyValuePair<string, string> marker in MemoryRecoveryStateOwner.SanitizeMemoryCommitMarkers(day.MemoryCommitMarkers))
				{
					merged.MemoryCommitMarkers[marker.Key] = marker.Value;
				}
			}
		}
		AddDays(targetDays);
		AddDays(sourceDays);
		return byDay.Values.Where((MyBehavior.DialogueDay x) => (x.Lines != null && x.Lines.Count > 0)
			|| (x.MemoryCommitMarkers != null && x.MemoryCommitMarkers.Count > 0)).OrderBy((MyBehavior.DialogueDay x) => x.GameDayIndex).ToList();
	}
internal void MergeNpcActionStorageById(Dictionary<string, List<NpcActionEntry>> storage, string sourceMemoryId, string targetMemoryId, bool keepOnlyRecentWindow)
	{
		string source = MemoryRecordRules.NormalizeMemoryHeroId(sourceMemoryId);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetMemoryId);
		if (storage == null || !storage.TryGetValue(source, out var sourceActions) || sourceActions == null || sourceActions.Count == 0)
		{
			return;
		}
		storage.TryGetValue(target, out var targetActions);
		List<NpcActionEntry> merged = NpcActionLedger.SanitizeNpcActionEntries((targetActions ?? new List<NpcActionEntry>()).Concat(sourceActions).ToList(), keepOnlyRecentWindow, QueuePort.CurrentDay());
		if (merged.Count > 0)
		{
			storage[target] = merged;
			IdentityPort.MarkWeeklySourcesDirty();
		}
		if (keepOnlyRecentWindow)
		{
			IdentityPort.RefreshRecentIndex(target, merged);
		}
	}
internal void RemoveMemoryEntityDataById(string memoryId)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		IdentityPort.Recovery().QuarantineInteractionMemoryRecoveryProjection(text, "memory_recovery_subject_removed");
		History?.Remove(text);
		HistoryStorage?.Remove(text);
		Drafts?.Remove(text);
		DraftStorage?.Remove(text);
		Blocks?.Remove(text);
		BlockStorage?.Remove(text);
		DailyQueue?.RemoveAll((MemorySummaryJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase));
		PendingWeeklyTriggers?.RemoveAll((WeeklyMemoryMaterialTrigger x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.MemoryId), text, StringComparison.OrdinalIgnoreCase));
		Overviews?.Remove(text);
		OverviewStorage?.Remove(text);
		OverviewQueue?.RemoveAll((MemoryOverviewJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase));
		MajorSummaries?.Remove(text);
		MajorStorage?.Remove(text);
		MajorQueue?.RemoveAll((MajorActionSummaryJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase));
		bool removedMajorActions = MajorActions?.Remove(text) == true;
		MajorActionStorage?.Remove(text);
		bool removedRecentActions = RecentActions?.Remove(text) == true;
		RecentActionStorage?.Remove(text);
		if (removedMajorActions || removedRecentActions)
		{
			IdentityPort.MarkWeeklySourcesDirty();
		}
		DirtyOverviewIds?.Remove(text);
		OverviewCandidateIdSet?.Remove(text);
		if (OverviewCandidateIds != null && OverviewCandidateIds.Count > 0)
		{
			List<string> kept = OverviewCandidateIds.Where((string x) => !string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x), text, StringComparison.OrdinalIgnoreCase)).ToList();
			OverviewCandidateIds.Clear();
			foreach (string item in kept)
			{
				OverviewCandidateIds.Enqueue(item);
			}
		}
	}
}
internal sealed class MemoryIdentityPort {internal Func<string,List<MyBehavior.DialogueDay>> LoadHistory;internal Action<string,List<MyBehavior.DialogueDay>> SaveHistory;internal Func<MemoryRecoveryStateOwner> Recovery;internal Action<string,List<NpcActionEntry>> RefreshRecentIndex;internal Action MarkWeeklySourcesDirty;internal Action<string> MarkOverviewDirty;}
