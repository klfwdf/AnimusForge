using System;
using System.Collections.Generic;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    /// <summary>
    /// Normalizes the JSON-backed debt ledger in place. The caller captures
    /// campaign day and supplies the existing ID generator on the game thread.
    /// </summary>
    private static class EconomyDebtNormalizationPolicy
    {
        internal static bool HasContent(DebtRecord record)
        {
            if (record == null)
            {
                return false;
            }
            if (record.OwedGold > 0)
            {
                return true;
            }
            if (record.OwedItems == null)
            {
                return false;
            }
            foreach (KeyValuePair<string, int> owedItem in record.OwedItems)
            {
                if (owedItem.Value > 0)
                {
                    return true;
                }
            }
            return false;
        }

        internal static string NormalizeNote(string note)
        {
            string normalized = (note ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }
            return normalized.Length > 120 ? normalized.Substring(0, 120) : normalized;
        }

        internal static void Normalize(
            DebtRecord record,
            float nowCampaignDay,
            Func<string> createDebtId,
            int overduePenaltyMaxWeeks,
            int manualPenaltyMax)
        {
            if (record == null)
            {
                return;
            }
            if (createDebtId == null)
            {
                throw new ArgumentNullException(nameof(createDebtId));
            }
            if (record.DebtLines == null)
            {
                record.DebtLines = new List<DebtRecord.DebtLine>();
            }
            if (record.OwedItems == null)
            {
                record.OwedItems = new Dictionary<string, int>();
            }
            if (record.DebtLines.Count == 0)
            {
                if (record.OwedGold > 0)
                {
                    float createdDay = record.CreatedDay > 0f ? record.CreatedDay : nowCampaignDay;
                    float dueDay = record.DueDay > 0f ? record.DueDay : createdDay + 1f;
                    record.DebtLines.Add(CreateLegacyLine(
                        createDebtId(),
                        isGold: true,
                        itemId: null,
                        record.OwedGold,
                        createdDay,
                        dueDay));
                }
                foreach (KeyValuePair<string, int> owedItem in record.OwedItems)
                {
                    if (string.IsNullOrWhiteSpace(owedItem.Key) || owedItem.Value <= 0)
                    {
                        continue;
                    }
                    float createdDay = record.CreatedDay > 0f ? record.CreatedDay : nowCampaignDay;
                    float dueDay = record.DueDay > 0f ? record.DueDay : createdDay + 1f;
                    record.DebtLines.Add(CreateLegacyLine(
                        createDebtId(),
                        isGold: false,
                        owedItem.Key,
                        owedItem.Value,
                        createdDay,
                        dueDay));
                }
            }

            List<DebtRecord.DebtLine> normalizedLines = new List<DebtRecord.DebtLine>();
            for (int index = 0; index < record.DebtLines.Count; index++)
            {
                DebtRecord.DebtLine line = record.DebtLines[index];
                if (line == null)
                {
                    continue;
                }
                line.RemainingAmount = Math.Max(0, line.RemainingAmount);
                if (line.RemainingAmount <= 0 || (!line.IsGold && string.IsNullOrWhiteSpace(line.ItemId)))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line.DebtId))
                {
                    line.DebtId = createDebtId();
                }
                if (line.InitialAmount <= 0 || line.InitialAmount < line.RemainingAmount)
                {
                    line.InitialAmount = line.RemainingAmount;
                }
                if (line.CreatedDay <= 0f)
                {
                    line.CreatedDay = nowCampaignDay;
                }
                if (line.IsGold)
                {
                    line.IsItemUnavailableDeclared = false;
                }
                if (line.IsDueUnlimited)
                {
                    line.DueDay = 0f;
                }
                else if (line.DueDay <= 0f)
                {
                    line.DueDay = line.CreatedDay + 1f;
                }
                line.BestPreDueCoverage = Clamp01(line.BestPreDueCoverage);
                line.OnTimePenaltyTierApplied = Math.Max(0, Math.Min(5, line.OnTimePenaltyTierApplied));
                line.OverduePenaltyDaysApplied = Math.Max(
                    0,
                    Math.Min(Math.Max(0, overduePenaltyMaxWeeks), line.OverduePenaltyDaysApplied));
                if (line.LastOverduePenaltyDay < -1)
                {
                    line.LastOverduePenaltyDay = -1;
                }
                line.OverdueTrustPenaltyPerDay = NormalizePenalty(line.OverdueTrustPenaltyPerDay, manualPenaltyMax);
                line.OverdueRelationPenaltyPerDay = NormalizePenalty(line.OverdueRelationPenaltyPerDay, manualPenaltyMax);
                line.CompensationUnitPrice = Math.Max(0, line.CompensationUnitPrice);
                line.CompensationGoldCredit = Math.Max(0, line.CompensationGoldCredit);
                line.UnlimitedTrustPenaltyNumeratorCarry = Math.Max(
                    0L,
                    Math.Min(UnlimitedDebtPenaltyReferenceValue - 1L, line.UnlimitedTrustPenaltyNumeratorCarry));
                line.DebtNote = NormalizeNote(line.DebtNote);
                normalizedLines.Add(line);
            }

            record.DebtLines = normalizedLines;
            record.OwedGold = 0;
            record.OwedItems = new Dictionary<string, int>();
            float earliestCreatedDay = 0f;
            float earliestDueDay = 0f;
            for (int index = 0; index < record.DebtLines.Count; index++)
            {
                DebtRecord.DebtLine line = record.DebtLines[index];
                if (line == null || line.RemainingAmount <= 0)
                {
                    continue;
                }
                if (line.IsGold)
                {
                    record.OwedGold += line.RemainingAmount;
                }
                else
                {
                    string itemId = line.ItemId ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        continue;
                    }
                    record.OwedItems[itemId] = record.OwedItems.TryGetValue(itemId, out int count)
                        ? count + line.RemainingAmount
                        : line.RemainingAmount;
                }
                if (earliestCreatedDay <= 0f || line.CreatedDay < earliestCreatedDay)
                {
                    earliestCreatedDay = line.CreatedDay;
                }
                if (!line.IsDueUnlimited && line.DueDay > 0f
                    && (earliestDueDay <= 0f || line.DueDay < earliestDueDay))
                {
                    earliestDueDay = line.DueDay;
                }
            }
            record.CreatedDay = earliestCreatedDay;
            record.DueDay = earliestDueDay;
            if (!HasContent(record))
            {
                record.CreatedDay = 0f;
                record.DueDay = 0f;
            }
        }

        private static DebtRecord.DebtLine CreateLegacyLine(
            string debtId,
            bool isGold,
            string itemId,
            int amount,
            float createdDay,
            float dueDay)
        {
            return new DebtRecord.DebtLine
            {
                DebtId = debtId,
                IsGold = isGold,
                ItemId = itemId,
                IsDueUnlimited = false,
                IsItemUnavailableDeclared = false,
                InitialAmount = amount,
                RemainingAmount = amount,
                CreatedDay = createdDay,
                DueDay = dueDay,
                BestPreDueCoverage = 0f,
                OnTimePenaltyTierApplied = 0,
                OverduePenaltyDaysApplied = 0,
                LastOverduePenaltyDay = -1,
                OverdueTrustPenaltyPerDay = 0,
                OverdueRelationPenaltyPerDay = 0,
                CompensationUnitPrice = 0,
                CompensationGoldCredit = 0
            };
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static int NormalizePenalty(int value, int max)
        {
            return Math.Max(0, Math.Min(Math.Max(0, max), value));
        }
    }
}
