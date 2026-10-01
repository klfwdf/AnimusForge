using System;using System.Collections.Generic;using System.Linq;
namespace AnimusForge;
internal sealed partial class MemoryBusinessStateOwner {
 internal MemoryQueuePort QueuePort;
 internal Dictionary<string,List<NpcActionEntry>> MajorActions = new(StringComparer.OrdinalIgnoreCase);
 internal Dictionary<string,string> OverviewStorage = new(StringComparer.OrdinalIgnoreCase);
 internal Dictionary<string,string> MajorStorage = new(StringComparer.OrdinalIgnoreCase);
 internal bool HasBlock(string id,int day) => Blocks != null && Blocks.TryGetValue(id,out var blocks) && blocks != null && blocks.Any(x=>x!=null && x.GameDayIndex==day);
internal DailyMemoryDraft FindMemoryDraft(MemorySummaryJob job)
	{
		if (job == null)
		{
			return null;
		}
		string text = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
		if (string.IsNullOrWhiteSpace(text) || Drafts == null || !Drafts.TryGetValue(text, out var value) || value == null)
		{
			return null;
		}
		// A persisted queue key must agree with the draft owner; never summarize or retarget an ambiguous old-save draft.
		return value.FirstOrDefault((DailyMemoryDraft x) => x != null && x.GameDayIndex == job.GameDayIndex && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase));
	}
internal bool HasMemorySummaryJobStillPending(MemorySummaryJob job)
	{
		try
		{
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job?.HeroId);
			if (job == null || job.RetryCount >= 3 || string.IsNullOrWhiteSpace(heroId) || !QueuePort.IsEntityEligible(heroId) || HasBlock(heroId, job.GameDayIndex))
			{
				return false;
			}
			DailyMemoryDraft draft = FindMemoryDraft(job);
			// Retry exhaustion is stored on both queue and draft, so old saves cannot silently recreate a terminal job.
			return draft != null && draft.SummaryRetryCount < 3 && draft.HasLlmDialogue && MemorySummaryPlanningRules.CountDailySourceChars(draft) > 0;
		}
		catch
		{
			return false;
		}
	}
internal MemoryOverviewState GetMemoryOverviewState(string heroId)
	{
		heroId = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
		if (string.IsNullOrWhiteSpace(heroId) || Overviews == null)
		{
			return null;
		}
		if (Overviews.TryGetValue(heroId, out var value))
		{
			return MemoryRecordRules.SanitizeMemoryOverviewState(MemoryRecordRules.Clone(value));
		}
		return null;
	}
internal bool HasMemoryOverviewPendingBlocks(string heroId, List<CompressedMemoryBlock> blocks)
	{
		// This is a read-only eligibility query, not a publication/sanitization owner.
		// Project exactly the fields used by Count/IsMemoryBlockIncludedInOverview
		// after ONE SanitizeCompressedMemoryBlocks pass. Do not clone/sort unrelated
		// scenes, AFEF text or weekly trigger graphs just to discard them here.
		List<string> blockIds = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CompressedMemoryBlock block in blocks ?? Enumerable.Empty<CompressedMemoryBlock>())
		{
			if (block == null) continue;
			string ownerId = MemoryRecordRules.NormalizeMemoryHeroId(block.HeroId);
			if (string.IsNullOrWhiteSpace(ownerId) || block.GameDayIndex < 0) continue;
			string blockId = string.IsNullOrWhiteSpace(block.Id) ? MemoryRecordRules.BuildCompressedMemoryBlockId(ownerId, block.GameDayIndex) : block.Id;
			// The original sanitizer reserves an untrimmed ID even when its first
			// block later fails content validation. Keep that ordering and identity.
			if (!seen.Add(blockId)) continue;
			string title = MemoryRecordRules.StripMemoryTitleDateTime((block.RichTitle ?? "").Trim());
			bool hasContent = !string.IsNullOrWhiteSpace(block.Summary)
				|| (block.AfefLines?.Any(line => !string.IsNullOrWhiteSpace(line)) ?? false)
				|| !string.IsNullOrWhiteSpace(title);
			if (hasContent) blockIds.Add(blockId.Trim());
		}
		// Do not deduplicate the trimmed list: raw IDs " x " and "x" count as two
		// blocks, but both match the same IncludedBlockId in the original query.
		if (blockIds.Count < QueuePort.OverviewStartCount()) return false;
		MemoryOverviewState state = GetMemoryOverviewState(heroId);
		if (state != null && !string.IsNullOrWhiteSpace(state.LastError)) return false;
		if (state == null || string.IsNullOrWhiteSpace(state.Summary)) return true;
		HashSet<string> included = new HashSet<string>(state.IncludedBlockIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		return blockIds.Any(id => !included.Contains(id));
	}
internal bool HasMemoryOverviewJobStillPending(MemoryOverviewJob job)
	{
		try
		{
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job?.HeroId);
			if (job == null || job.RetryCount >= 3 || string.IsNullOrWhiteSpace(heroId) || !QueuePort.IsEntityEligible(heroId) || Blocks == null || !Blocks.TryGetValue(heroId, out var blocks) || blocks == null)
			{
				return false;
			}
			return HasMemoryOverviewPendingBlocks(heroId, blocks);
		}
		catch
		{
			return false;
		}
	}
internal MajorActionSummaryState GetMajorActionSummaryState(string heroId)
	{
		heroId = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
		if (string.IsNullOrWhiteSpace(heroId) || MajorSummaries == null)
		{
			return null;
		}
		if (MajorSummaries.TryGetValue(heroId, out var value))
		{
			return MemoryRecordRules.SanitizeMajorActionSummaryState(MemoryRecordRules.Clone(value));
		}
		return null;
	}
internal static void GetMajorActionMaxCursor(IEnumerable<NpcActionEntry> actions, out int day, out int sequence)
	{
		day = 0;
		sequence = 0;
		foreach (NpcActionEntry action in actions ?? Enumerable.Empty<NpcActionEntry>())
		{
			if (action == null)
			{
				continue;
			}
			int actionDay = Math.Max(0, action.Day);
			int actionSequence = Math.Max(0, action.Sequence);
			if (actionDay > day || (actionDay == day && actionSequence > sequence))
			{
				day = actionDay;
				sequence = actionSequence;
			}
		}
	}
internal static bool IsNpcActionAfterSummaryCursor(NpcActionEntry action, MajorActionSummaryState state)
	{
		if (action == null)
		{
			return false;
		}
		if (state == null || string.IsNullOrWhiteSpace(state.Summary))
		{
			return true;
		}
		int day = Math.Max(0, action.Day);
		int sequence = Math.Max(0, action.Sequence);
		return day > state.LastSummarizedDay || (day == state.LastSummarizedDay && sequence > state.LastSummarizedSequence);
	}
internal bool HasMajorActionsNeedingSummary(string heroId, List<NpcActionEntry> actions)
	{
		if (actions == null || actions.Count <= 0)
		{
			return false;
		}
		MajorActionSummaryState state = GetMajorActionSummaryState(heroId);
		if (state != null && !string.IsNullOrWhiteSpace(state.LastError))
		{
			// A completed three-attempt failure remains inspectable, but cannot recreate an automatic queue on every day tick.
			return false;
		}
		if (state == null || string.IsNullOrWhiteSpace(state.Summary))
		{
			return true;
		}
		GetMajorActionMaxCursor(actions, out var day, out var sequence);
		return day > state.LastSummarizedDay || (day == state.LastSummarizedDay && sequence > state.LastSummarizedSequence);
	}
internal bool HasMajorActionSummaryJobStillPending(MajorActionSummaryJob job)
	{
		try
		{
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job?.HeroId);
			if (job == null || job.RetryCount >= 3 || string.IsNullOrWhiteSpace(heroId) || MemorySummaryPlanningRules.IsNonHero(heroId) || !QueuePort.IsEntityEligible(heroId) || MajorActions == null || !MajorActions.TryGetValue(heroId, out var actions) || actions == null)
			{
				// Major-action summaries are Hero-only; malformed nonhero queue data must not stay runnable.
				return false;
			}
			return HasMajorActionsNeedingSummary(heroId, NpcActionLedger.SanitizeNpcActionEntries(actions, false, QueuePort.CurrentDay()));
		}
		catch
		{
			return false;
		}
	}
internal void TryEnqueueMajorActionSummaryForDraft(DailyMemoryDraft draft, HashSet<string> queuedMajorHeroIds, bool ownerAlreadyEligible = false)
	{
		try
		{
			if (draft == null || !draft.HasLlmDialogue)
			{
				return;
			}
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(draft.HeroId);
			if (string.IsNullOrWhiteSpace(heroId) || MemorySummaryPlanningRules.IsNonHero(heroId) || (!ownerAlreadyEligible && !QueuePort.IsEntityEligible(heroId)) || MajorActions == null || !MajorActions.TryGetValue(heroId, out var actions) || actions == null)
			{
				// Major-action summaries belong to Heroes only; callers may reuse an owner-level eligibility check for a slice.
				return;
			}
			List<NpcActionEntry> sanitizedActions = NpcActionLedger.SanitizeNpcActionEntries(actions, false, QueuePort.CurrentDay());
			if (sanitizedActions.Count <= 0 || !HasMajorActionsNeedingSummary(heroId, sanitizedActions))
			{
				return;
			}
			if (MajorQueue == null)
			{
				MajorQueue = new List<MajorActionSummaryJob>();
			}
			MajorActionSummaryJob existing = MajorQueue.FirstOrDefault((MajorActionSummaryJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase));
			if (existing != null)
			{
				existing.TriggerGameDayIndex = Math.Max(existing.TriggerGameDayIndex, draft.GameDayIndex);
				if (string.IsNullOrWhiteSpace(existing.TriggerGameDate))
				{
					existing.TriggerGameDate = (draft.GameDate ?? "").Trim();
				}
				queuedMajorHeroIds?.Add(heroId);
				return;
			}
			if (queuedMajorHeroIds != null && !queuedMajorHeroIds.Add(heroId))
			{
				return;
			}
			MajorQueue.Add(new MajorActionSummaryJob
			{
				HeroId = heroId,
				HeroName = (draft.HeroName ?? "").Trim(),
				TriggerGameDayIndex = draft.GameDayIndex,
				TriggerGameDate = (draft.GameDate ?? "").Trim()
			});
		}
		catch (Exception ex)
		{
			QueuePort.Log("NpcMajorSummary", "[ERROR] TryEnqueueMajorActionSummaryForDraft failed: " + ex.Message);
		}
	}
internal void TryEnqueueMemoryOverviewForMemoryId(string memoryId, string memoryName, List<CompressedMemoryBlock> blocks = null)
	{
		try
		{
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
			if (!QueuePort.IsEntityEligible(heroId))
			{
				return;
			}
			List<CompressedMemoryBlock> sanitizedBlocks = MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks ?? LoadBlocks(heroId));
			if (!HasMemoryOverviewPendingBlocks(heroId, sanitizedBlocks))
			{
				return;
			}
			if (OverviewQueue == null)
			{
				OverviewQueue = new List<MemoryOverviewJob>();
			}
			MemoryOverviewJob existing = OverviewQueue.FirstOrDefault((MemoryOverviewJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase));
			CompressedMemoryBlock latestBlock = sanitizedBlocks.OrderByDescending((CompressedMemoryBlock x) => x.GameDayIndex).ThenByDescending((CompressedMemoryBlock x) => x.EndHour).FirstOrDefault();
			string heroName = (memoryName ?? latestBlock?.HeroName ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(heroName))
			{
				heroName = "NPC";
			}
			if (existing != null)
			{
				existing.HeroName = string.IsNullOrWhiteSpace(existing.HeroName) ? heroName : existing.HeroName;
				existing.TriggerGameDayIndex = Math.Max(existing.TriggerGameDayIndex, latestBlock?.GameDayIndex ?? 0);
				if (string.IsNullOrWhiteSpace(existing.TriggerGameDate))
				{
					existing.TriggerGameDate = (latestBlock?.GameDate ?? "").Trim();
				}
				return;
			}
			OverviewQueue.Add(new MemoryOverviewJob
			{
				HeroId = heroId,
				HeroName = heroName,
				TriggerGameDayIndex = latestBlock?.GameDayIndex ?? 0,
				TriggerGameDate = (latestBlock?.GameDate ?? "").Trim()
			});
			OverviewQueue = MemoryRecordRules.SanitizeMemoryOverviewQueue(OverviewQueue);
		}
		catch (Exception ex)
		{
			QueuePort.Log("MemoryOverview", "[ERROR] TryEnqueueMemoryOverviewForMemoryId failed: " + ex.Message);
		}
	}
internal bool CancelUnavailableHeroCompressionWorkById(string memoryId, string reason)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(text) || MemorySummaryPlanningRules.IsNonHero(text))
		{
			return false;
		}
		// Cancel only LLM work and its derived summaries. Source drafts/blocks/pending triggers are retained because
		// they can still carry weekly-report facts even after the Hero is no longer a valid conversation target.
		bool removed = false;
		if (DailyQueue != null)
		{
			removed |= DailyQueue.RemoveAll((MemorySummaryJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase)) > 0;
		}
		if (Overviews != null)
		{
			removed |= Overviews.Remove(text);
		}
		if (OverviewStorage != null)
		{
			removed |= OverviewStorage.Remove(text);
		}
		if (OverviewQueue != null)
		{
			removed |= OverviewQueue.RemoveAll((MemoryOverviewJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase)) > 0;
		}
		if (MajorSummaries != null)
		{
			removed |= MajorSummaries.Remove(text);
		}
		if (MajorStorage != null)
		{
			removed |= MajorStorage.Remove(text);
		}
		if (MajorQueue != null)
		{
			removed |= MajorQueue.RemoveAll((MajorActionSummaryJob x) => x != null && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase)) > 0;
		}
		if (DirtyOverviewIds.Remove(text))
		{
			removed = true;
		}
		if (OverviewCandidateIdSet.Remove(text))
		{
			removed = true;
		}
		if (OverviewCandidateIds.Count > 0)
		{
			List<string> kept = OverviewCandidateIds.Where((string x) => !string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(x), text, StringComparison.OrdinalIgnoreCase)).ToList();
			if (kept.Count != OverviewCandidateIds.Count)
			{
				removed = true;
				OverviewCandidateIds.Clear();
				foreach (string item in kept)
				{
					OverviewCandidateIds.Enqueue(item);
				}
			}
		}
		if (Sealing.Queued != null)
		{
			Sealing.Queued.RemoveWhere((string x) => (x ?? "").StartsWith(text + "|", StringComparison.OrdinalIgnoreCase));
		}
		Sealing.QueuedMajor?.Remove(text);
		if (removed)
		{
			QueuePort.Log("CompressedMemory", "cancelled unavailable hero compression work hero=" + text + " reason=" + (reason ?? ""));
		}
		return removed;
	}
}
internal sealed class MemoryQueuePort { internal Func<string,bool> IsEntityEligible;internal Func<int> OverviewStartCount;internal Func<int> CurrentDay;internal Action<string,string> Log; }
