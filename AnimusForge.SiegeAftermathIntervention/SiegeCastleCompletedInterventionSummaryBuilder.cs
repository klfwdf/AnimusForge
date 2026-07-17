using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// One concise castle completion ledger; it replaces separate native, GCCZ and morale notices.
/// </summary>
public static class SiegeCastleCompletedInterventionSummaryBuilder
{
    public static string Build(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        if (facts == null)
        {
            return "【城堡处置结算】原版宽恕与城堡处置已经结算完成。";
        }

        string settlement = Normalize(facts.SettlementName, "这座城堡");
        string player = Normalize(facts.PlayerName, "玩家");
        var sb = new StringBuilder();
        sb.Append('【').Append(settlement).Append("·城堡处置结算】").AppendLine();
        sb.Append("领地：忠诚 ").Append(Signed(facts.LoyaltyDelta))
            .Append("｜治安 ").Append(Signed(facts.SecurityDelta))
            .Append("｜繁荣 ").Append(Signed(facts.ProsperityDelta)).AppendLine();
        sb.Append("战俘：").Append(BuildPrisonerLine(facts)).AppendLine();
        sb.Append("军心：").Append(BuildMoraleLine(facts, player)).AppendLine();
        sb.Append("民意：").Append(SiegeCastlePublicOpinionProfile.Build(
            player,
            facts.SettlementPublicTrustDelta,
            facts.BoundVillagePublicTrustDelta,
            facts.NotableRelationDelta,
            facts.NotableTrustDelta));

        if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary) || facts.RetainedLordPrisoners > 0)
        {
            sb.AppendLine();
            sb.Append("领主：");
            if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary))
            {
                sb.Append(facts.LordOutcomeSummary.Trim());
                if (facts.RetainedLordPrisoners > 0)
                {
                    sb.Append("；");
                }
            }
            if (facts.RetainedLordPrisoners > 0)
            {
                sb.Append(facts.RetainedLordPrisoners).Append(" 人仍为俘虏");
            }
            sb.Append('。');
        }
        return sb.ToString();
    }

    private static string BuildPrisonerLine(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        List<string> parts = facts.RegularPrisonerOutcomes
            .Where(entry => entry != null && entry.AffectedCount > 0)
            .Select(DescribeOutcome)
            .ToList();
        if (facts.TreatedRegularPrisoners)
        {
            parts.Add("已给予物资并禁止虐待");
        }
        if (facts.ReceivedRegularArmaments)
        {
            parts.Add("已收缴军械入包");
        }
        if (facts.RemainingRegularPrisoners > 0)
        {
            parts.Add(facts.RemainingRegularPrisoners + " 人仍被关押");
        }
        if (parts.Count == 0)
        {
            parts.Add("未另作处置，带入者仍保持俘虏身份");
        }
        return string.Join("；", parts) + "。";
    }

    private static string DescribeOutcome(SiegeCastleDispositionSummaryEntry entry)
    {
        string count = entry.AffectedCount + " 人";
        return entry.Action switch
        {
            SiegeCastleActionKind.ReleasePrisoners => count + "获释",
            SiegeCastleActionKind.SellPrisoners => count + "被贩卖" + (entry.Gold > 0 ? "（+" + entry.Gold + " 第纳尔）" : string.Empty),
            SiegeCastleActionKind.RecruitPrisonersVoluntary => count + "自愿加入部队",
            SiegeCastleActionKind.RecruitPrisonersForced => count + "被强制收编",
            SiegeCastleActionKind.LaborPrisonersVoluntary => count + "自愿接受劳役安置",
            SiegeCastleActionKind.LaborPrisonersForced => count + "被送去强制劳役",
            SiegeCastleActionKind.InstructorPrisonersVoluntary => count + "自愿训练新兵",
            SiegeCastleActionKind.InstructorPrisonersForced => count + "被强迫训练新兵",
            SiegeCastleActionKind.SlaughterPrisoners => count + "在现场被杀",
            _ => count + "完成处置"
        };
    }

    private static string BuildMoraleLine(SiegeCastleCompletedInterventionSummaryFacts facts, string player)
    {
        string action = SiegeCastleSoldierReactionProfile.DescribeConcernAction(facts.SoldierConcernAction);
        if (facts.SoldierMoralePenaltyApplied > 0)
        {
            return "因 " + player + " 对俘虏执行“" + action
                + "”，随军士兵表达不满且未获安抚，士气 -" + facts.SoldierMoralePenaltyApplied
                + BuildRecruitmentMoraleBreakdown(facts.SoldierConcernAction, facts.SoldierMoralePenaltyApplied)
                + "。";
        }
        if (facts.SoldierAppeasementRequired && facts.SoldierAppeasementApplied)
        {
            return "随军士兵曾对“" + action + "”表达不满，但已接受 " + player + " 的安抚，本次不扣士气。";
        }
        return "随军士兵没有形成需要安抚的公开不满，本次不扣额外士气。";
    }

    private static string BuildRecruitmentMoraleBreakdown(SiegeCastleActionKind action, int penalty)
    {
        if (action == SiegeCastleActionKind.RecruitPrisonersVoluntary
            && penalty == SiegeCastleSoldierReactionProfile.VoluntaryRecruitmentTotalPenalty)
        {
            return "（收编战俘不满 -30 + 自愿收编处置 -30）";
        }
        if (action == SiegeCastleActionKind.RecruitPrisonersForced
            && penalty == SiegeCastleSoldierReactionProfile.ForcedRecruitmentTotalPenalty)
        {
            return "（收编战俘不满 -30 + 强制收编惩罚 -60）";
        }
        return string.Empty;
    }

    private static string Signed(float value)
    {
        float normalized = Math.Abs(value) < 0.005f ? 0f : value;
        return normalized > 0f ? "+" + normalized.ToString("0.##") : normalized.ToString("0.##");
    }

    private static string Normalize(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
