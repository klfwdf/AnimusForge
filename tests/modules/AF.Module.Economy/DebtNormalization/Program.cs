using System;
using System.Collections.Generic;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    internal static int RunDebtNormalizationTests()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            checks++;
            if (!condition)
            {
                throw new InvalidOperationException("FAIL " + message);
            }
        }

        Check(!EconomyDebtNormalizationPolicy.HasContent(null), "null content");
        Check(!EconomyDebtNormalizationPolicy.HasContent(new DebtRecord()), "empty content");
        Check(EconomyDebtNormalizationPolicy.HasContent(new DebtRecord { OwedGold = 1 }), "gold content");
        Check(EconomyDebtNormalizationPolicy.HasContent(new DebtRecord
        {
            OwedItems = new Dictionary<string, int> { ["grain"] = 1 }
        }), "item content");
        Check(EconomyDebtNormalizationPolicy.NormalizeNote(null) == string.Empty, "null note");
        Check(EconomyDebtNormalizationPolicy.NormalizeNote("  note  ") == "note", "note trim");
        Check(EconomyDebtNormalizationPolicy.NormalizeNote(new string('x', 130)).Length == 120, "note truncate");

        int idCounter = 0;
        string CreateId() => "debt-" + (++idCounter);
        EconomyDebtNormalizationPolicy.Normalize(null, 42f, CreateId, 12, 10);
        Check(idCounter == 0, "null normalization does not allocate identity");

        DebtRecord legacy = new DebtRecord
        {
            OwedGold = 100,
            OwedItems = new Dictionary<string, int>
            {
                [string.Empty] = 8,
                ["grain"] = 3,
                ["fish"] = 0
            },
            CreatedDay = 0f,
            DueDay = 0f,
            DebtLines = new List<DebtRecord.DebtLine>()
        };
        EconomyDebtNormalizationPolicy.Normalize(legacy, 42f, CreateId, 12, 10);
        Check(legacy.DebtLines.Count == 2, "legacy aggregate conversion count");
        DebtRecord.DebtLine legacyGold = legacy.DebtLines.Find(line => line.IsGold);
        DebtRecord.DebtLine legacyItem = legacy.DebtLines.Find(line => !line.IsGold);
        Check(legacyGold != null && legacyGold.DebtId == "debt-1", "legacy gold identity");
        Check(legacyItem != null && legacyItem.DebtId == "debt-2" && legacyItem.ItemId == "grain", "legacy item identity");
        Check(legacyGold.CreatedDay == 42f && legacyGold.DueDay == 43f, "legacy gold dates");
        Check(legacyItem.CreatedDay == 42f && legacyItem.DueDay == 43f, "legacy item dates");
        Check(legacy.OwedGold == 100 && legacy.OwedItems.Count == 1 && legacy.OwedItems["grain"] == 3,
            "legacy aggregates rebuilt");
        Check(legacy.CreatedDay == 42f && legacy.DueDay == 43f, "legacy aggregate dates");

        string firstGoldId = legacyGold.DebtId;
        string firstItemId = legacyItem.DebtId;
        EconomyDebtNormalizationPolicy.Normalize(legacy, 99f, CreateId, 12, 10);
        Check(idCounter == 2, "idempotent identity allocation");
        Check(legacy.DebtLines.Count == 2
              && legacy.DebtLines[0].DebtId == firstGoldId
              && legacy.DebtLines[1].DebtId == firstItemId,
            "idempotent line identity");
        Check(legacy.CreatedDay == 42f && legacy.DueDay == 43f, "idempotent aggregate dates");

        DebtRecord.DebtLine clampedGold = new DebtRecord.DebtLine
        {
            DebtId = " ",
            IsGold = true,
            ItemId = "must-clear",
            IsItemUnavailableDeclared = true,
            InitialAmount = -5,
            RemainingAmount = 7,
            CreatedDay = 0f,
            DueDay = 0f,
            BestPreDueCoverage = 2f,
            OnTimePenaltyTierApplied = 9,
            OverduePenaltyDaysApplied = 20,
            LastOverduePenaltyDay = -5,
            OverdueTrustPenaltyPerDay = -2,
            OverdueRelationPenaltyPerDay = 99,
            CompensationUnitPrice = -4,
            CompensationGoldCredit = -8,
            UnlimitedTrustPenaltyNumeratorCarry = 200000,
            DebtNote = "  " + new string('n', 130) + "  "
        };
        DebtRecord.DebtLine unlimitedItem = new DebtRecord.DebtLine
        {
            DebtId = "item-id",
            IsGold = false,
            ItemId = "grain",
            IsDueUnlimited = true,
            IsItemUnavailableDeclared = true,
            InitialAmount = 1,
            RemainingAmount = 4,
            CreatedDay = 5f,
            DueDay = 55f,
            BestPreDueCoverage = -0.5f,
            OnTimePenaltyTierApplied = -1,
            OverduePenaltyDaysApplied = -4,
            LastOverduePenaltyDay = -10,
            OverdueTrustPenaltyPerDay = 11,
            OverdueRelationPenaltyPerDay = -1,
            CompensationUnitPrice = -3,
            CompensationGoldCredit = -2,
            UnlimitedTrustPenaltyNumeratorCarry = -7,
            DebtNote = null
        };
        DebtRecord dirty = new DebtRecord
        {
            OwedGold = 999,
            OwedItems = null,
            CreatedDay = 77f,
            DueDay = 88f,
            DebtLines = new List<DebtRecord.DebtLine>
            {
                null,
                new DebtRecord.DebtLine { IsGold = true, RemainingAmount = 0 },
                new DebtRecord.DebtLine { IsGold = false, ItemId = " ", RemainingAmount = 5 },
                clampedGold,
                unlimitedItem
            }
        };
        EconomyDebtNormalizationPolicy.Normalize(dirty, 50f, CreateId, 12, 10);
        Check(dirty.DebtLines.Count == 2, "invalid lines removed");
        Check(clampedGold.DebtId == "debt-3", "missing identity assigned");
        Check(clampedGold.InitialAmount == 7 && clampedGold.CreatedDay == 50f && clampedGold.DueDay == 51f,
            "finite line amount and dates");
        Check(!clampedGold.IsItemUnavailableDeclared, "gold item-unavailable cleared");
        Check(clampedGold.BestPreDueCoverage == 1f
              && clampedGold.OnTimePenaltyTierApplied == 5
              && clampedGold.OverduePenaltyDaysApplied == 12,
            "upper clamps");
        Check(clampedGold.LastOverduePenaltyDay == -1
              && clampedGold.OverdueTrustPenaltyPerDay == 0
              && clampedGold.OverdueRelationPenaltyPerDay == 10,
            "penalty clamps");
        Check(clampedGold.CompensationUnitPrice == 0
              && clampedGold.CompensationGoldCredit == 0
              && clampedGold.UnlimitedTrustPenaltyNumeratorCarry == 99999,
            "compensation and carry clamps");
        Check(clampedGold.DebtNote.Length == 120 && clampedGold.DebtNote[0] == 'n', "normalized long note");
        Check(unlimitedItem.InitialAmount == 4 && unlimitedItem.DueDay == 0f, "unlimited due and amount");
        Check(unlimitedItem.BestPreDueCoverage == 0f
              && unlimitedItem.OnTimePenaltyTierApplied == 0
              && unlimitedItem.OverduePenaltyDaysApplied == 0,
            "lower clamps");
        Check(unlimitedItem.LastOverduePenaltyDay == -1
              && unlimitedItem.OverdueTrustPenaltyPerDay == 10
              && unlimitedItem.OverdueRelationPenaltyPerDay == 0,
            "unlimited penalty clamps");
        Check(unlimitedItem.CompensationUnitPrice == 0
              && unlimitedItem.CompensationGoldCredit == 0
              && unlimitedItem.UnlimitedTrustPenaltyNumeratorCarry == 0
              && unlimitedItem.DebtNote == string.Empty,
            "unlimited auxiliary clamps");
        Check(dirty.OwedGold == 7 && dirty.OwedItems.Count == 1 && dirty.OwedItems["grain"] == 4,
            "dirty aggregates rebuilt");
        Check(dirty.CreatedDay == 5f && dirty.DueDay == 51f, "unlimited excluded from earliest due");

        DebtRecord emptied = new DebtRecord
        {
            OwedGold = 10,
            OwedItems = new Dictionary<string, int> { ["grain"] = 2 },
            CreatedDay = 9f,
            DueDay = 10f,
            DebtLines = new List<DebtRecord.DebtLine>
            {
                new DebtRecord.DebtLine { IsGold = true, RemainingAmount = -1 }
            }
        };
        EconomyDebtNormalizationPolicy.Normalize(emptied, 50f, CreateId, 12, 10);
        Check(emptied.DebtLines.Count == 0 && emptied.OwedGold == 0 && emptied.OwedItems.Count == 0,
            "empty normalization clears aggregates");
        Check(emptied.CreatedDay == 0f && emptied.DueDay == 0f, "empty normalization clears dates");

        Check(EconomyDebtSchedulePolicy.NormalizeDueDays(-1) == 1
              && EconomyDebtSchedulePolicy.NormalizeDueDays(1) == 1
              && EconomyDebtSchedulePolicy.NormalizeDueDays(120) == 120
              && EconomyDebtSchedulePolicy.NormalizeDueDays(121) == 120,
            "due day bounds");
        Check(EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(0) == 0
              && EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(1) == 1
              && EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(9999) == 1
              && EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(20000) == 2,
            "weekly trust penalty value scale");

        DebtRecord.DebtLine unlimited = new DebtRecord.DebtLine
        {
            IsDueUnlimited = true,
            RemainingAmount = 1,
            LastOverduePenaltyDay = 0,
            UnlimitedTrustPenaltyNumeratorCarry = 0
        };
        Check(EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(unlimited, 100000, 10) == 0
              && unlimited.LastOverduePenaltyDay == 10,
            "unlimited penalty initializes clock");
        Check(EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(unlimited, 100000, 10) == 0,
            "unlimited penalty ignores same day");
        Check(EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(unlimited, 100000, 11) == 16
              && unlimited.LastOverduePenaltyDay == 11
              && unlimited.UnlimitedTrustPenaltyNumeratorCarry == 0,
            "unlimited penalty daily value scale");
        unlimited.UnlimitedTrustPenaltyNumeratorCarry = 99999;
        Check(EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(unlimited, 1, 12) == 1
              && unlimited.UnlimitedTrustPenaltyNumeratorCarry == 15,
            "unlimited penalty carry");
        Check(EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(unlimited, -1, 13) == 0
              && unlimited.LastOverduePenaltyDay == 13
              && unlimited.UnlimitedTrustPenaltyNumeratorCarry == 15,
            "unlimited zero-value clock advance");
        DebtRecord.DebtLine saturated = new DebtRecord.DebtLine
        {
            IsDueUnlimited = true,
            RemainingAmount = 1,
            LastOverduePenaltyDay = 1
        };
        Check(EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(
                  saturated, int.MaxValue, int.MaxValue) == int.MaxValue,
            "unlimited penalty saturates");

        DebtRecord.DebtLine finiteReminder = new DebtRecord.DebtLine
        {
            RemainingAmount = 1,
            IsDueUnlimited = false
        };
        DebtRecord.DebtLine unlimitedReminder = new DebtRecord.DebtLine
        {
            RemainingAmount = 1,
            IsDueUnlimited = true,
            CreatedDay = 10f
        };
        Check(!EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(null, 17)
              && !EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(
                  new DebtRecord.DebtLine { RemainingAmount = 0 }, 17)
              && EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(finiteReminder, 1),
            "scheduled reminder eligibility");
        Check(!EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(unlimitedReminder, 16)
              && EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(unlimitedReminder, 17)
              && !EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(unlimitedReminder, 18)
              && EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(unlimitedReminder, 24),
            "scheduled reminder cadence");

        Check(EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyTotal(1, 4) == 0
              && EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyTotal(1, 5) == 1
              && EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyTotal(-1, 5) == 0,
            "weekly relation penalty total");
        Check(EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyDelta(1, 2, 5) == 1
              && EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyDelta(2, 1, 5) == 0,
            "weekly relation penalty delta");
        Check(EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyTotal(
                  int.MaxValue, int.MaxValue) == int.MaxValue,
            "weekly relation penalty saturates");
        Check(EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(10f, 0f) == 0
              && EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(10f, 10f) == 0
              && EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(16.99f, 10f) == 0
              && EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(17f, 10f) == 1
              && EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(1000f, 10f) == 12,
            "overdue elapsed week bounds");

        return checks;
    }
}

internal static class Program
{
    private static int Main()
    {
        int checks = RewardSystemBehavior.RunDebtNormalizationTests();
        Console.WriteLine("PASS economyDebtNormalization checks=" + checks);
        return 0;
    }
}
