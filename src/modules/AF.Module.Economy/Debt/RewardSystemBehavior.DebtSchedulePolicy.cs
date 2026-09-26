using System;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    /// <summary>
    /// Owns debt due-window, reminder, and overdue penalty arithmetic. The
    /// daily Campaign adapter supplies current time/value and applies effects.
    /// </summary>
    private static class EconomyDebtSchedulePolicy
    {
        internal static int NormalizeDueDays(int days)
        {
            return Math.Max(1, Math.Min(120, days));
        }

        internal static int ComputeWeeklyOverdueTrustPenaltyByDebtValue(int debtValue)
        {
            int normalizedValue = Math.Max(0, debtValue);
            if (normalizedValue <= 0)
            {
                return 0;
            }
            return Math.Max(1, normalizedValue / OverdueTrustPenaltyPerWeekValueStep);
        }

        internal static int ConsumeUnlimitedDebtTrustPenaltyUnits(
            DebtRecord.DebtLine line,
            int debtValue,
            int campaignDayIndex)
        {
            if (line == null || !line.IsDueUnlimited || line.RemainingAmount <= 0)
            {
                return 0;
            }
            if (line.LastOverduePenaltyDay <= 0)
            {
                line.LastOverduePenaltyDay = campaignDayIndex;
                return 0;
            }
            int elapsedDays = campaignDayIndex - line.LastOverduePenaltyDay;
            if (elapsedDays <= 0)
            {
                return 0;
            }
            line.LastOverduePenaltyDay = campaignDayIndex;
            int normalizedValue = Math.Max(0, debtValue);
            if (normalizedValue <= 0)
            {
                return 0;
            }
            decimal numerator = Math.Max(0L, line.UnlimitedTrustPenaltyNumeratorCarry)
                + (decimal)normalizedValue * UnlimitedDebtPenaltyTrustUnitsPerReferencePerDay * elapsedDays;
            decimal penaltyUnits = decimal.Floor(numerator / UnlimitedDebtPenaltyReferenceValue);
            line.UnlimitedTrustPenaltyNumeratorCarry = (long)(numerator % UnlimitedDebtPenaltyReferenceValue);
            if (penaltyUnits <= 0m)
            {
                return 0;
            }
            return penaltyUnits >= int.MaxValue ? int.MaxValue : (int)penaltyUnits;
        }

        internal static bool ShouldIncludeDebtLineInScheduledReminder(
            DebtRecord.DebtLine line,
            int campaignDayIndex)
        {
            if (line == null || line.RemainingAmount <= 0)
            {
                return false;
            }
            if (!line.IsDueUnlimited)
            {
                return true;
            }
            int createdDay = Math.Max(0, (int)Math.Floor(line.CreatedDay));
            int elapsedDays = campaignDayIndex - createdDay;
            return elapsedDays >= UnlimitedDebtReminderIntervalDays
                && elapsedDays % UnlimitedDebtReminderIntervalDays == 0;
        }

        internal static int ComputeWeeklyOverdueRelationPenaltyTotal(
            int weeksApplied,
            int trustPenaltyPerWeek)
        {
            if (weeksApplied <= 0 || trustPenaltyPerWeek <= 0)
            {
                return 0;
            }
            long totalTrustPenalty = (long)weeksApplied * trustPenaltyPerWeek;
            long relationPenalty = totalTrustPenalty / OverdueRelationPenaltyPerWeekTrustStep;
            if (relationPenalty > int.MaxValue)
            {
                return int.MaxValue;
            }
            return Math.Max(0, (int)relationPenalty);
        }

        internal static int ComputeWeeklyOverdueRelationPenaltyDelta(
            int previousWeeksApplied,
            int currentWeeksApplied,
            int trustPenaltyPerWeek)
        {
            int previous = ComputeWeeklyOverdueRelationPenaltyTotal(
                Math.Max(0, previousWeeksApplied),
                trustPenaltyPerWeek);
            int current = ComputeWeeklyOverdueRelationPenaltyTotal(
                Math.Max(0, currentWeeksApplied),
                trustPenaltyPerWeek);
            return Math.Max(0, current - previous);
        }

        internal static int ComputeOverdueElapsedWeeks(float nowCampaignDay, float dueDay)
        {
            if (dueDay <= 0f || nowCampaignDay <= dueDay + 0.01f)
            {
                return 0;
            }
            int elapsedDays = Math.Max(0, (int)Math.Floor(nowCampaignDay - dueDay));
            int elapsedWeeks = elapsedDays / OverduePenaltyIntervalDays;
            return Math.Max(0, Math.Min(OverduePenaltyMaxWeeks, elapsedWeeks));
        }
    }
}
