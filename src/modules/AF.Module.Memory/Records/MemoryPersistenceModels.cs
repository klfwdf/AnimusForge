using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.Library;

namespace AnimusForge;

	internal sealed class DailyMemoryLine
	{
		public int GameDayIndex;

		public string GameDate = "";

		public int GameHour;

		public string Scene = "";

		public string Speaker = "";

		public string Text = "";

		public int SceneSessionId = -1;

		public int DialogueSessionId = -1;

		public int TargetAgentIndex = -1;

		public string TargetName = "";

		public string MemorySessionKey = "";

		public bool IsAfef;

		public bool IsLlmDialogue;

		public string MemoryCommitId = "";

		public string MemoryCommitPart = "";

		public string MemoryCommitHash = "";

		public int MemoryCommitOriginGameDay = -1;

		public string MemoryCommitOriginGameDate = "";

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal DailyMemoryLine CopyForSummary()
		{
			var copy = (DailyMemoryLine)MemberwiseClone();
			return copy;
		}
	}


	internal sealed class DailyMemoryDraft
	{
		public string HeroId = "";

		public string HeroName = "";

		public int GameDayIndex;

		public string GameDate = "";

		public bool HasLlmDialogue;

		public bool QueuedForSummary;

		public int SummaryRetryCount;

		public string LastSummaryError = "";

		public List<DailyMemoryLine> Lines = new List<DailyMemoryLine>();

		public List<WeeklyMemoryMaterialTrigger> WeeklyMaterialTriggers = new List<WeeklyMemoryMaterialTrigger>();

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal DailyMemoryDraft CopyForSummary()
		{
			var copy = (DailyMemoryDraft)MemberwiseClone();
			copy.Lines = Lines?.Select(x => x?.CopyForSummary()).ToList();
			copy.WeeklyMaterialTriggers = WeeklyMaterialTriggers?.Select(x => x?.CopyForSummary()).ToList();
			return copy;
		}
	}


	internal sealed class CompressedMemoryBlock
	{
		public string Id = "";

		public string HeroId = "";

		public string HeroName = "";

		public int GameDayIndex;

		public string GameDate = "";

		public int StartHour;

		public int EndHour;

		public List<string> Scenes = new List<string>();

		public string RichTitle = "";

		public string Summary = "";

		public List<string> AfefLines = new List<string>();

		public string PlayerPublicity = "";

		public string PlayerHistoryMaterial = "";

		public string PlayerPublicityReason = "";

		public long CreatedUtcTicks;

		public List<WeeklyMemoryMaterialTrigger> WeeklyMaterialTriggers = new List<WeeklyMemoryMaterialTrigger>();

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal CompressedMemoryBlock CopyForSummary()
		{
			var copy = (CompressedMemoryBlock)MemberwiseClone();
			copy.Scenes = Scenes?.ToList();
			copy.AfefLines = AfefLines?.ToList();
			copy.WeeklyMaterialTriggers = WeeklyMaterialTriggers?.Select(x => x?.CopyForSummary()).ToList();
			return copy;
		}
	}


	internal sealed class WeeklyMemoryMaterialTrigger
	{
		public string MemoryId = "";

		public string NpcName = "";

		public int GameDayIndex;

		public string GameDate = "";

		public int SceneSessionId = -1;

		public int DialogueSessionId = -1;

		public int TargetAgentIndex = -1;

		public string FootholdKingdomId = "";

		public string FootholdSettlementId = "";

		public string NormalizedTagText = "";

		public List<string> Tags = new List<string>();

		public long EstimatedValueDenars;

		public string TriggerReason = "";

		public string StableKey = "";

		// LOCAL-7-K additive provenance for outcome-confirmed detached material.
		// Legacy triggers leave these empty. They are data-only digests and must
		// never be used to reconstruct or replay an ActionPlan.
		public string OutcomeReceiptId = "";

		public string OutcomeCandidateHash = "";

		public string OutcomePayloadHash = "";

		public string OutcomeActionFingerprint = "";

		public string OutcomeTurnFingerprint = "";

		public long CreatedUtcTicks;

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal WeeklyMemoryMaterialTrigger CopyForSummary()
		{
			var copy = (WeeklyMemoryMaterialTrigger)MemberwiseClone();
			copy.Tags = Tags?.ToList();
			return copy;
		}
	}


	internal sealed class MemorySummaryJob
	{
		public string HeroId = "";

		public string HeroName = "";

		public int GameDayIndex;

		public string GameDate = "";

		public int RetryCount;

		public string LastError = "";

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal MemorySummaryJob CopyForSummary()
		{
			var copy = (MemorySummaryJob)MemberwiseClone();
			return copy;
		}
	}


	internal sealed class MemoryOverviewState
	{
		public string HeroId = "";

		public string HeroName = "";

		public string Summary = "";

		public List<string> IncludedBlockIds = new List<string>();

		public long UpdatedUtcTicks;

		public string LastError = "";

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal MemoryOverviewState CopyForSummary()
		{
			var copy = (MemoryOverviewState)MemberwiseClone();
			copy.IncludedBlockIds = IncludedBlockIds?.ToList();
			return copy;
		}
	}


	internal sealed class MemoryOverviewJob
	{
		public string HeroId = "";

		public string HeroName = "";

		public int TriggerGameDayIndex;

		public string TriggerGameDate = "";

		public int RetryCount;

		public string LastError = "";

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal MemoryOverviewJob CopyForSummary()
		{
			var copy = (MemoryOverviewJob)MemberwiseClone();
			return copy;
		}
	}


	internal sealed class MajorActionSummaryState
	{
		public string HeroId = "";

		public string HeroName = "";

		public string Summary = "";

		public int LastSummarizedDay;

		public int LastSummarizedSequence;

		public long UpdatedUtcTicks;

		public string LastError = "";

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal MajorActionSummaryState CopyForSummary()
		{
			var copy = (MajorActionSummaryState)MemberwiseClone();
			return copy;
		}
	}


	internal sealed class MajorActionSummaryJob
	{
		public string HeroId = "";

		public string HeroName = "";

		public int TriggerGameDayIndex;

		public string TriggerGameDate = "";

		public int RetryCount;

		public string LastError = "";

		// Copy all scalar/immutable fields; detach every mutable collection without JSON.
		internal MajorActionSummaryJob CopyForSummary()
		{
			var copy = (MajorActionSummaryJob)MemberwiseClone();
			return copy;
		}
	}

internal static class MemoryRecordRules
{
	internal static T Clone<T>(T value) where T : class
	{
		if (value == null) return null;
		object copy = value switch
		{
			DailyMemoryLine item => item.CopyForSummary(),
			DailyMemoryDraft item => item.CopyForSummary(),
			CompressedMemoryBlock item => item.CopyForSummary(),
			WeeklyMemoryMaterialTrigger item => item.CopyForSummary(),
			MemorySummaryJob item => item.CopyForSummary(),
			MemoryOverviewJob item => item.CopyForSummary(),
			MajorActionSummaryJob item => item.CopyForSummary(),
			MemoryOverviewState item => item.CopyForSummary(),
			MajorActionSummaryState item => item.CopyForSummary(),
			NpcActionEntry item => item.CopyForSummary(),
			List<NpcActionEntry> items => items.Select(x => x?.CopyForSummary()).ToList(),
			List<CompressedMemoryBlock> items => items.Select(x => x?.CopyForSummary()).ToList(),
			_ => throw new ArgumentException("Unsupported memory record model: " + typeof(T).Name)
		};
		return (T)copy;
	}

	internal static List<WeeklyMemoryMaterialTrigger> SanitizeWeeklyMemoryMaterialTriggers(IEnumerable<WeeklyMemoryMaterialTrigger> triggers)
	{
		List<WeeklyMemoryMaterialTrigger> list = new List<WeeklyMemoryMaterialTrigger>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (WeeklyMemoryMaterialTrigger sourceEntry in triggers ?? Enumerable.Empty<WeeklyMemoryMaterialTrigger>())
		{
			WeeklyMemoryMaterialTrigger trigger = TWParallel.IsMainThread() ? sourceEntry : sourceEntry?.CopyForSummary();
			if (trigger == null)
			{
				continue;
			}
			string memoryId = NormalizeMemoryHeroId(trigger.MemoryId);
			string kingdomId = (trigger.FootholdKingdomId ?? "").Trim();
			string tagText = NormalizeWeeklyMemoryMaterialTagText(trigger.NormalizedTagText);
			List<string> tags = NormalizeWeeklyMemoryMaterialTags(trigger.Tags);
			if (tags.Count == 0)
			{
				tags = ExtractWeeklyMemoryMaterialTags(tagText);
			}
			if (string.IsNullOrWhiteSpace(tagText))
			{
				tagText = string.Join("\n", tags);
			}
			if (string.IsNullOrWhiteSpace(memoryId) || string.IsNullOrWhiteSpace(kingdomId) || tags.Count == 0)
			{
				continue;
			}
			string stableKey = (trigger.StableKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(stableKey))
			{
				stableKey = BuildWeeklyMemoryMaterialTriggerStableKey(memoryId, trigger.GameDayIndex, trigger.SceneSessionId, trigger.DialogueSessionId, kingdomId, tagText);
			}
			if (!seen.Add(stableKey))
			{
				continue;
			}
			trigger.MemoryId = memoryId;
			trigger.NpcName = (trigger.NpcName ?? "").Trim();
			trigger.GameDayIndex = Math.Max(0, trigger.GameDayIndex);
			trigger.GameDate = (trigger.GameDate ?? "").Trim();
			if (trigger.SceneSessionId < -1)
			{
				trigger.SceneSessionId = -1;
			}
			if (trigger.DialogueSessionId < -1)
			{
				trigger.DialogueSessionId = -1;
			}
			if (trigger.TargetAgentIndex < -1)
			{
				trigger.TargetAgentIndex = -1;
			}
			trigger.FootholdKingdomId = kingdomId;
			trigger.FootholdSettlementId = (trigger.FootholdSettlementId ?? "").Trim();
			trigger.NormalizedTagText = tagText;
			trigger.Tags = tags;
			trigger.EstimatedValueDenars = Math.Max(0L, trigger.EstimatedValueDenars);
			trigger.TriggerReason = (trigger.TriggerReason ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			trigger.StableKey = stableKey;
			trigger.OutcomeReceiptId = (trigger.OutcomeReceiptId ?? "").Trim();
			trigger.OutcomeCandidateHash = (trigger.OutcomeCandidateHash ?? "").Trim();
			trigger.OutcomePayloadHash = (trigger.OutcomePayloadHash ?? "").Trim();
			trigger.OutcomeActionFingerprint = (trigger.OutcomeActionFingerprint ?? "").Trim();
			trigger.OutcomeTurnFingerprint = (trigger.OutcomeTurnFingerprint ?? "").Trim();
			bool hasOutcomeProvenance = !string.IsNullOrWhiteSpace(trigger.OutcomeReceiptId)
				|| !string.IsNullOrWhiteSpace(trigger.OutcomeCandidateHash)
				|| !string.IsNullOrWhiteSpace(trigger.OutcomePayloadHash)
				|| !string.IsNullOrWhiteSpace(trigger.OutcomeActionFingerprint)
				|| !string.IsNullOrWhiteSpace(trigger.OutcomeTurnFingerprint);
			bool hasOutcomeSemanticTag = trigger.Tags.Any(tag =>
				(tag ?? "").StartsWith("[WEEKLY:ECONOMY_", StringComparison.OrdinalIgnoreCase));
			bool hasOutcomeStableKey = stableKey.StartsWith(
				"weekly_outcome:", StringComparison.Ordinal);
			bool validOutcomeProvenance = hasOutcomeProvenance
				&& MyBehavior.IsMemoryRecoveryHexDigest(trigger.OutcomeReceiptId)
				&& MyBehavior.IsMemoryRecoveryHexDigest(trigger.OutcomeCandidateHash)
				&& MyBehavior.IsMemoryRecoveryHexDigest(trigger.OutcomePayloadHash)
				&& MyBehavior.IsMemoryRecoveryHexDigest(trigger.OutcomeActionFingerprint)
				&& MyBehavior.IsMemoryRecoveryHexDigest(trigger.OutcomeTurnFingerprint)
				&& string.Equals(
					stableKey,
					"weekly_outcome:" + trigger.OutcomeReceiptId + ":" + trigger.OutcomePayloadHash,
					StringComparison.Ordinal);
			if ((hasOutcomeSemanticTag || hasOutcomeStableKey)
				&& (!hasOutcomeSemanticTag || !validOutcomeProvenance))
			{
				// Outcome-only semantic material has no legitimate legacy form.
				// Never downgrade a malformed exact receipt into a broad trigger.
				continue;
			}
			if (hasOutcomeProvenance && !validOutcomeProvenance)
			{
				// Legacy action-tag material may survive, but malformed additive
				// provenance cannot authorize exact readback.
				trigger.OutcomeReceiptId = "";
				trigger.OutcomeCandidateHash = "";
				trigger.OutcomePayloadHash = "";
				trigger.OutcomeActionFingerprint = "";
				trigger.OutcomeTurnFingerprint = "";
			}
			if (trigger.CreatedUtcTicks <= 0L)
			{
				trigger.CreatedUtcTicks = DateTime.UtcNow.Ticks;
			}
			list.Add(trigger);
		}
		return list.OrderBy((WeeklyMemoryMaterialTrigger x) => x.GameDayIndex).ThenBy((WeeklyMemoryMaterialTrigger x) => x.CreatedUtcTicks).ToList();
	}


	internal static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(IEnumerable<DailyMemoryDraft> drafts)
	{
		List<DailyMemoryDraft> list = new List<DailyMemoryDraft>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (DailyMemoryDraft sourceEntry in drafts ?? Enumerable.Empty<DailyMemoryDraft>())
		{
			DailyMemoryDraft draft = SanitizeDailyMemoryDraftEntry(sourceEntry, seen);
			if (draft != null) list.Add(draft);
		}
		return list.OrderBy((DailyMemoryDraft x) => x.GameDayIndex).ToList();
	}


	internal static void BindDailyMemoryDraftWeeklyTrigger(WeeklyMemoryMaterialTrigger trigger, string memoryId, int gameDayIndex, string gameDate)
	{
		if (trigger == null)
		{
			return;
		}
		trigger.MemoryId = memoryId;
		trigger.GameDayIndex = gameDayIndex;
		trigger.GameDate = string.IsNullOrWhiteSpace(trigger.GameDate) ? gameDate : trigger.GameDate;
	}


	internal static DailyMemoryLine SanitizeDailyMemoryDraftLine(DailyMemoryLine x, DailyMemoryDraft draft)
	{
		if (x == null || string.IsNullOrWhiteSpace((x.Text ?? "").Trim()))
		{
			return null;
		}
		x.GameDayIndex = draft.GameDayIndex;
		x.GameDate = string.IsNullOrWhiteSpace(x.GameDate) ? draft.GameDate : x.GameDate.Trim();
		x.GameHour = MBMath.ClampInt(x.GameHour, 0, 23);
		x.Scene = (x.Scene ?? "").Trim();
		x.Speaker = (x.Speaker ?? "").Trim();
		x.Text = (x.Text ?? "").Trim();
		x.TargetAgentIndex = Math.Max(-1, x.TargetAgentIndex);
		x.TargetName = (x.TargetName ?? "").Trim();
		x.MemorySessionKey = (x.MemorySessionKey ?? "").Trim();
		x.MemoryCommitId = (x.MemoryCommitId ?? "").Trim();
		x.MemoryCommitPart = (x.MemoryCommitPart ?? "").Trim();
		x.MemoryCommitHash = (x.MemoryCommitHash ?? "").Trim();
		x.MemoryCommitOriginGameDay = Math.Max(-1, x.MemoryCommitOriginGameDay);
		x.MemoryCommitOriginGameDate = (x.MemoryCommitOriginGameDate ?? "").Trim();
		if (!MyBehavior.IsValidMemoryCommitMarker(x.MemoryCommitId, x.MemoryCommitPart, x.MemoryCommitHash))
		{
			x.MemoryCommitId = "";
			x.MemoryCommitPart = "";
			x.MemoryCommitHash = "";
			x.MemoryCommitOriginGameDay = -1;
			x.MemoryCommitOriginGameDate = "";
		}
		if (x.SceneSessionId < -1)
		{
			x.SceneSessionId = -1;
		}
		if (x.DialogueSessionId < -1)
		{
			x.DialogueSessionId = -1;
		}
		return x;
	}


	internal static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(DailyMemoryDraft sourceEntry, HashSet<string> seen)
	{
		DailyMemoryDraft draft = TWParallel.IsMainThread() ? sourceEntry : sourceEntry?.CopyForSummary();
		if (draft == null)
		{
			return null;
		}
		string text = NormalizeMemoryHeroId(draft.HeroId);
		if (string.IsNullOrWhiteSpace(text) || draft.GameDayIndex < 0)
		{
			return null;
		}
		string key = text + "|" + draft.GameDayIndex;
		if (!seen.Add(key))
		{
			return null;
		}
		draft.HeroId = text;
		draft.HeroName = (draft.HeroName ?? "").Trim();
		draft.GameDate = (draft.GameDate ?? "").Trim();
		draft.LastSummaryError = (draft.LastSummaryError ?? "").Trim();
		if (draft.WeeklyMaterialTriggers != null)
		{
			foreach (WeeklyMemoryMaterialTrigger trigger in draft.WeeklyMaterialTriggers)
			{
				BindDailyMemoryDraftWeeklyTrigger(trigger, text, draft.GameDayIndex, draft.GameDate);
			}
		}
		draft.WeeklyMaterialTriggers = SanitizeWeeklyMemoryMaterialTriggers(draft.WeeklyMaterialTriggers);
		List<DailyMemoryLine> lines = new List<DailyMemoryLine>();
		bool hasLlmDialogue = draft.HasLlmDialogue;
		foreach (DailyMemoryLine sourceLine in draft.Lines ?? new List<DailyMemoryLine>())
		{
			DailyMemoryLine line = SanitizeDailyMemoryDraftLine(sourceLine, draft);
			if (line == null)
			{
				continue;
			}
			lines.Add(line);
			hasLlmDialogue = hasLlmDialogue || (line.IsLlmDialogue && !line.IsAfef);
		}
		draft.Lines = lines;
		draft.HasLlmDialogue = hasLlmDialogue;
		return draft.Lines.Count > 0 ? draft : null;
	}


	internal static List<CompressedMemoryBlock> SanitizeCompressedMemoryBlocks(IEnumerable<CompressedMemoryBlock> blocks)
	{
		List<CompressedMemoryBlock> list = new List<CompressedMemoryBlock>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CompressedMemoryBlock sourceEntry in blocks ?? Enumerable.Empty<CompressedMemoryBlock>())
		{
			CompressedMemoryBlock block = TWParallel.IsMainThread() ? sourceEntry : sourceEntry?.CopyForSummary();
			if (block == null)
			{
				continue;
			}
			block.HeroId = NormalizeMemoryHeroId(block.HeroId);
			if (string.IsNullOrWhiteSpace(block.HeroId) || block.GameDayIndex < 0)
			{
				continue;
			}
			if (string.IsNullOrWhiteSpace(block.Id))
			{
				block.Id = BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex);
			}
			if (!seen.Add(block.Id))
			{
				continue;
			}
			block.HeroName = (block.HeroName ?? "").Trim();
			block.GameDate = (block.GameDate ?? "").Trim();
			block.StartHour = MBMath.ClampInt(block.StartHour, 0, 23);
			block.EndHour = MBMath.ClampInt(block.EndHour, 0, 23);
			block.Scenes = (block.Scenes ?? new List<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToList();
			block.RichTitle = StripMemoryTitleDateTime((block.RichTitle ?? "").Trim());
			block.Summary = (block.Summary ?? "").Trim();
			block.AfefLines = (block.AfefLines ?? new List<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Take(80).ToList();
			if (block.WeeklyMaterialTriggers != null)
			{
				foreach (WeeklyMemoryMaterialTrigger trigger in block.WeeklyMaterialTriggers)
				{
					if (trigger != null)
					{
						trigger.MemoryId = block.HeroId;
						trigger.GameDayIndex = block.GameDayIndex;
						trigger.GameDate = string.IsNullOrWhiteSpace(trigger.GameDate) ? block.GameDate : trigger.GameDate;
					}
				}
			}
			block.WeeklyMaterialTriggers = SanitizeWeeklyMemoryMaterialTriggers(block.WeeklyMaterialTriggers);
			if (!string.IsNullOrWhiteSpace(block.RichTitle) || !string.IsNullOrWhiteSpace(block.Summary) || block.AfefLines.Count > 0)
			{
				list.Add(block);
			}
		}
		return list.OrderBy((CompressedMemoryBlock x) => x.GameDayIndex).ThenBy((CompressedMemoryBlock x) => x.StartHour).ToList();
	}


	internal static List<MemorySummaryJob> SanitizeMemorySummaryQueue(IEnumerable<MemorySummaryJob> jobs)
	{
		return NormalizeMemorySummaryQueue(jobs).OrderBy((MemorySummaryJob x) => x.GameDayIndex).ThenBy((MemorySummaryJob x) => x.HeroName).ToList();
	}


	internal static List<MemorySummaryJob> NormalizeMemorySummaryQueue(IEnumerable<MemorySummaryJob> jobs)
	{
		List<MemorySummaryJob> list = new List<MemorySummaryJob>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MemorySummaryJob job in jobs ?? Enumerable.Empty<MemorySummaryJob>())
		{
			if (job == null)
			{
				continue;
			}
			job.HeroId = NormalizeMemoryHeroId(job.HeroId);
			if (string.IsNullOrWhiteSpace(job.HeroId) || job.GameDayIndex < 0)
			{
				continue;
			}
			string key = job.HeroId + "|" + job.GameDayIndex;
			if (!seen.Add(key))
			{
				continue;
			}
			job.HeroName = (job.HeroName ?? "").Trim();
			job.GameDate = (job.GameDate ?? "").Trim();
			job.LastError = (job.LastError ?? "").Trim();
			job.RetryCount = MBMath.ClampInt(job.RetryCount, 0, 3);
			list.Add(job);
		}
		return list;
	}


	internal static MemoryOverviewState SanitizeMemoryOverviewState(MemoryOverviewState state)
	{
		if (state == null)
		{
			return null;
		}
		state.HeroId = NormalizeMemoryHeroId(state.HeroId);
		state.HeroName = (state.HeroName ?? "").Trim();
		state.Summary = (state.Summary ?? "").Replace("\r", "").Trim();
		state.LastError = (state.LastError ?? "").Trim();
		state.IncludedBlockIds = (state.IncludedBlockIds ?? new List<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return state;
	}


	internal static List<MemoryOverviewJob> SanitizeMemoryOverviewQueue(IEnumerable<MemoryOverviewJob> jobs)
	{
		List<MemoryOverviewJob> list = new List<MemoryOverviewJob>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MemoryOverviewJob job in jobs ?? Enumerable.Empty<MemoryOverviewJob>())
		{
			if (job == null)
			{
				continue;
			}
			job.HeroId = NormalizeMemoryHeroId(job.HeroId);
			if (string.IsNullOrWhiteSpace(job.HeroId))
			{
				continue;
			}
			if (!seen.Add(job.HeroId))
			{
				continue;
			}
			job.HeroName = (job.HeroName ?? "").Trim();
			job.TriggerGameDate = (job.TriggerGameDate ?? "").Trim();
			job.TriggerGameDayIndex = Math.Max(0, job.TriggerGameDayIndex);
			job.LastError = (job.LastError ?? "").Trim();
			job.RetryCount = MBMath.ClampInt(job.RetryCount, 0, 3);
			list.Add(job);
		}
		return list.OrderBy((MemoryOverviewJob x) => x.TriggerGameDayIndex).ThenBy((MemoryOverviewJob x) => x.HeroName).ToList();
	}


	internal static MajorActionSummaryState SanitizeMajorActionSummaryState(MajorActionSummaryState state)
	{
		if (state == null)
		{
			return null;
		}
		state.HeroId = NormalizeMemoryHeroId(state.HeroId);
		state.HeroName = (state.HeroName ?? "").Trim();
		state.Summary = (state.Summary ?? "").Replace("\r", "").Trim();
		state.LastError = (state.LastError ?? "").Trim();
		state.LastSummarizedDay = Math.Max(0, state.LastSummarizedDay);
		state.LastSummarizedSequence = Math.Max(0, state.LastSummarizedSequence);
		return state;
	}


	internal static List<MajorActionSummaryJob> SanitizeMajorActionSummaryQueue(IEnumerable<MajorActionSummaryJob> jobs)
	{
		return NormalizeMajorActionSummaryQueue(jobs).OrderBy((MajorActionSummaryJob x) => x.TriggerGameDayIndex).ThenBy((MajorActionSummaryJob x) => x.HeroName).ToList();
	}


	internal static List<MajorActionSummaryJob> NormalizeMajorActionSummaryQueue(IEnumerable<MajorActionSummaryJob> jobs)
	{
		List<MajorActionSummaryJob> list = new List<MajorActionSummaryJob>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MajorActionSummaryJob job in jobs ?? Enumerable.Empty<MajorActionSummaryJob>())
		{
			if (job == null)
			{
				continue;
			}
			job.HeroId = NormalizeMemoryHeroId(job.HeroId);
			if (string.IsNullOrWhiteSpace(job.HeroId))
			{
				continue;
			}
			if (!seen.Add(job.HeroId))
			{
				continue;
			}
			job.HeroName = (job.HeroName ?? "").Trim();
			job.TriggerGameDate = (job.TriggerGameDate ?? "").Trim();
			job.TriggerGameDayIndex = Math.Max(0, job.TriggerGameDayIndex);
			job.LastError = (job.LastError ?? "").Trim();
			job.RetryCount = MBMath.ClampInt(job.RetryCount, 0, 3);
			list.Add(job);
		}
		return list;
	}


	internal static string NormalizeMemoryHeroId(string heroId)
	{
		return (heroId ?? "").Trim().ToLowerInvariant();
	}


	internal static string NormalizeWeeklyMemoryMaterialTagText(string text)
	{
		List<string> tags = ExtractWeeklyMemoryMaterialTags(text);
		return string.Join("\n", tags);
	}


	internal static List<string> ExtractWeeklyMemoryMaterialTags(string text)
	{
		List<string> list = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in Regex.Matches(text ?? "", "\\[(?:ACTION:[^\\]\\r\\n]*|AD:[^\\]\\r\\n]*|ADP:[^\\]\\r\\n]*|ATT:[^\\]\\r\\n]*|ATP:[^\\]\\r\\n]*)\\]", RegexOptions.IgnoreCase))
		{
			string tag = (match?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(tag) && !tag.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase) && seen.Add(tag))
			{
				list.Add(tag);
			}
		}
		return list;
	}


	internal static List<string> NormalizeWeeklyMemoryMaterialTags(IEnumerable<string> tags)
	{
		List<string> list = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in tags ?? Enumerable.Empty<string>())
		{
			string tag = (item ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(tag) && !tag.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase) && seen.Add(tag))
			{
				list.Add(tag);
			}
		}
		return list;
	}


	internal static string BuildWeeklyMemoryMaterialTriggerStableKey(string memoryId, int day, int sceneSessionId, int dialogueSessionId, string kingdomId, string tagText)
	{
		string source = (memoryId ?? "").Trim() + "|" + day + "|" + sceneSessionId + "|" + dialogueSessionId + "|" + ((kingdomId ?? "").Trim()) + "|" + ((tagText ?? "").Trim());
		return "weekly_memory_trigger:" + ComputeWeeklyMemoryMaterialHash(source);
	}


	internal static string ComputeWeeklyMemoryMaterialHash(string sourceText)
	{
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(sourceText ?? "");
			byte[] array = SHA1.Create().ComputeHash(bytes);
			return string.Concat(array.Select((byte b) => b.ToString("x2")));
		}
		catch
		{
			return Math.Abs((sourceText ?? "").GetHashCode()).ToString("x");
		}
	}


	internal static string BuildCompressedMemoryBlockId(string heroId, int dayIndex)
	{
		return NormalizeMemoryHeroId(heroId) + ":" + dayIndex.ToString();
	}


	internal static string StripMemoryTitleDateTime(string title)
	{
		string text = (title ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = Regex.Replace(text, "^\\s*[\\[【(（]?[\\d\\-/:：年月日\\s时点春夏秋冬第]+[\\]】)）]?\\s*", "", RegexOptions.CultureInvariant).Trim();
		text = Regex.Replace(text, "\\s+", " ").Trim();
		if (text.Length > 36)
		{
			text = text.Substring(0, 36).Trim();
		}
		return text;
	}

}
