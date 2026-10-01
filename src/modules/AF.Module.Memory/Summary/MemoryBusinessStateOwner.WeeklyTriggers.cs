using System;
using AnimusForge.Refactor.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// The authoritative draft and pending stores remain on this owner. Called only at
// material events and daily-line append; no tick scans or additional state table.
internal sealed partial class MemoryBusinessStateOwner
{
	internal void AddWeeklyTrigger(DailyMemoryDraft draft, WeeklyMemoryMaterialTrigger trigger)
	{
		if (draft == null || trigger == null)
		{
			return;
		}
		trigger.MemoryId = MemoryRecordRules.NormalizeMemoryHeroId(draft.HeroId);
		trigger.GameDayIndex = draft.GameDayIndex;
		trigger.GameDate = string.IsNullOrWhiteSpace(trigger.GameDate) ? draft.GameDate : trigger.GameDate;
		if (draft.WeeklyMaterialTriggers == null)
		{
			draft.WeeklyMaterialTriggers = new List<WeeklyMemoryMaterialTrigger>();
		}
		string stableKey = (trigger.StableKey ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(stableKey) && draft.WeeklyMaterialTriggers.Any((WeeklyMemoryMaterialTrigger x) => x != null && string.Equals((x.StableKey ?? "").Trim(), stableKey, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		draft.WeeklyMaterialTriggers.Add(trigger);
		draft.WeeklyMaterialTriggers = MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(draft.WeeklyMaterialTriggers);
	}

	internal void AttachPendingWeeklyTriggers(DailyMemoryDraft draft, DailyMemoryLine line, int currentDay)
	{
		if (draft == null || line == null || PendingWeeklyTriggers == null || PendingWeeklyTriggers.Count == 0)
		{
			return;
		}
		string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(draft.HeroId);
		int day = draft.GameDayIndex;
		List<WeeklyMemoryMaterialTrigger> matched = new List<WeeklyMemoryMaterialTrigger>();
		foreach (WeeklyMemoryMaterialTrigger trigger in PendingWeeklyTriggers)
		{
			if (trigger == null || !string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(trigger.MemoryId), memoryId, StringComparison.OrdinalIgnoreCase) || trigger.GameDayIndex != day)
			{
				continue;
			}
			bool sceneMatch = trigger.SceneSessionId >= 0 && trigger.SceneSessionId == line.SceneSessionId;
			bool dialogueMatch = trigger.DialogueSessionId >= 0 && trigger.DialogueSessionId == line.DialogueSessionId;
			bool looseMatch = trigger.SceneSessionId < 0 && trigger.DialogueSessionId < 0;
			if (sceneMatch || dialogueMatch || looseMatch)
			{
				matched.Add(trigger);
			}
		}
		if (matched.Count == 0)
		{
			PrunePendingWeeklyTriggers(currentDay);
			return;
		}
		foreach (WeeklyMemoryMaterialTrigger trigger2 in matched)
		{
			AddWeeklyTrigger(draft, trigger2);
		}
		HashSet<string> matchedKeys = new HashSet<string>(matched.Select((WeeklyMemoryMaterialTrigger x) => (x?.StableKey ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		PendingWeeklyTriggers.RemoveAll((WeeklyMemoryMaterialTrigger x) => x == null || matchedKeys.Contains((x.StableKey ?? "").Trim()));
		PrunePendingWeeklyTriggers(currentDay);
	}

	internal void PrunePendingWeeklyTriggers(int currentDay)
	{
		if (PendingWeeklyTriggers == null || PendingWeeklyTriggers.Count == 0)
		{
			return;
		}
		int minDay = Math.Max(0, currentDay - 2);
		PendingWeeklyTriggers = MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(PendingWeeklyTriggers).Where((WeeklyMemoryMaterialTrigger x) => x != null && x.GameDayIndex >= minDay).ToList();
	}

    internal bool StageOrAttachWeeklyTrigger(WeeklyMemoryMaterialTrigger trigger, int currentDay)
    {
        if (trigger == null) return false;
        var drafts = LoadDrafts(trigger.MemoryId);
        var draft = drafts.FirstOrDefault(x => x != null && x.GameDayIndex == trigger.GameDayIndex);
        if (draft == null)
        {
            StageWeeklyTrigger(trigger, currentDay);
            return false;
        }
        AddWeeklyTrigger(draft, trigger);
        SaveDrafts(trigger.MemoryId, drafts);
        return true;
    }

    internal void StageWeeklyTrigger(WeeklyMemoryMaterialTrigger trigger, int currentDay)
    {
        if (trigger == null) return;
        PendingWeeklyTriggers ??= new List<WeeklyMemoryMaterialTrigger>();
        PendingWeeklyTriggers.RemoveAll(x => x == null || string.Equals(
            (x.StableKey ?? "").Trim(), trigger.StableKey, StringComparison.OrdinalIgnoreCase));
        PendingWeeklyTriggers.Add(trigger);
        PrunePendingWeeklyTriggers(currentDay);
    }
    internal bool AttachConfirmedWeeklyOutcome(WeeklyMemoryMaterialOutcomeReceipt receipt,
        int currentDay, string currentDate, long utcTicks, out int storageDay)
    {
        var frozen = receipt.Payload;
        string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(frozen.MemoryId);
        int selectedDay = receipt.OriginGameDay < currentDay
                && LoadBlocks(memoryId).Any(item => item != null && item.GameDayIndex == receipt.OriginGameDay)
            ? currentDay
            : receipt.OriginGameDay;
        storageDay = selectedDay;
        List<DailyMemoryDraft> drafts = LoadDrafts(memoryId);
        DailyMemoryDraft draft = drafts.FirstOrDefault(item => item != null
            && item.GameDayIndex == selectedDay);
        if (draft == null || draft.Lines == null || draft.Lines.Count == 0)
        {
            return false;
        }

        string stableKey = "weekly_outcome:" + receipt.ReceiptId + ":" + receipt.PayloadHash;
        if (!HasExactWeeklyActionOutcomeTrigger(draft, receipt, stableKey))
        {
            List<string> labels = frozen.Atoms
                .Select(atom => atom?.Label)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            AddWeeklyTrigger(draft, new WeeklyMemoryMaterialTrigger
            {
                MemoryId = memoryId,
                NpcName = frozen.NpcName,
                GameDayIndex = storageDay,
                GameDate = storageDay == receipt.OriginGameDay
                    ? frozen.OriginGameDate
                    : currentDate,
                SceneSessionId = receipt.SceneSessionId,
                DialogueSessionId = receipt.DialogueSessionId,
                TargetAgentIndex = receipt.TargetAgentIndex,
                FootholdKingdomId = frozen.FootholdKingdomId,
                FootholdSettlementId = frozen.FootholdSettlementId,
                NormalizedTagText = string.Join("\n", labels),
                Tags = labels,
                EstimatedValueDenars = frozen.EstimatedValueDenars,
                TriggerReason = frozen.Reason,
                StableKey = stableKey,
                OutcomeReceiptId = receipt.ReceiptId,
                OutcomeCandidateHash = receipt.CandidateHash,
                OutcomePayloadHash = receipt.PayloadHash,
                OutcomeActionFingerprint = receipt.ActionFingerprint,
                OutcomeTurnFingerprint = receipt.TurnFingerprint,
                CreatedUtcTicks = receipt.ConfirmedUtcTicks > 0L
                    ? receipt.ConfirmedUtcTicks
                    : utcTicks
            });
            SaveDrafts(memoryId, drafts);
            drafts = LoadDrafts(memoryId);
            draft = drafts.FirstOrDefault(item => item != null && item.GameDayIndex == selectedDay);
        }
        if (!HasExactWeeklyActionOutcomeTrigger(draft, receipt, stableKey))
        {
            return false;
        }

        return true;
    }

    private static bool HasExactWeeklyActionOutcomeTrigger(
        DailyMemoryDraft draft,
        WeeklyMemoryMaterialOutcomeReceipt receipt,
        string stableKey)
    {
        if (draft?.WeeklyMaterialTriggers == null || receipt?.Payload == null)
        {
            return false;
        }
        WeeklyMemoryMaterialFrozenPayload frozen = receipt.Payload;
        List<string> labels = frozen.Atoms
            .Select(atom => atom?.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        string normalizedTagText = string.Join("\n", labels);
        return draft.WeeklyMaterialTriggers.Any(trigger => trigger != null
                && string.Equals(trigger.StableKey, stableKey, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeReceiptId, receipt.ReceiptId, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeCandidateHash, receipt.CandidateHash, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomePayloadHash, receipt.PayloadHash, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeActionFingerprint, receipt.ActionFingerprint, StringComparison.Ordinal)
                && string.Equals(trigger.OutcomeTurnFingerprint, receipt.TurnFingerprint, StringComparison.Ordinal)
                && string.Equals(MemoryRecordRules.NormalizeMemoryHeroId(trigger.MemoryId), frozen.MemoryId,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(trigger.NpcName, frozen.NpcName, StringComparison.Ordinal)
                && trigger.GameDayIndex == draft.GameDayIndex
                && trigger.SceneSessionId == receipt.SceneSessionId
                && trigger.DialogueSessionId == receipt.DialogueSessionId
                && trigger.TargetAgentIndex == receipt.TargetAgentIndex
                && string.Equals(trigger.FootholdKingdomId, frozen.FootholdKingdomId,
                    StringComparison.Ordinal)
                && string.Equals(trigger.FootholdSettlementId, frozen.FootholdSettlementId,
                    StringComparison.Ordinal)
                && string.Equals(trigger.NormalizedTagText, normalizedTagText,
                    StringComparison.Ordinal)
                && (trigger.Tags ?? new List<string>()).SequenceEqual(
                    labels,
                    StringComparer.OrdinalIgnoreCase)
                && trigger.EstimatedValueDenars == frozen.EstimatedValueDenars
                && string.Equals(trigger.TriggerReason, frozen.Reason, StringComparison.Ordinal));
    }

}
