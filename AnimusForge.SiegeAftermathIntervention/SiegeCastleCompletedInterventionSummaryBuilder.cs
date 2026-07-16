using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only completion wording. Town civilian, market and cultural outcomes must not enter this summary.
/// </summary>
public static class SiegeCastleCompletedInterventionSummaryBuilder
{
    public static string Build(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        if (facts == null)
        {
            return "攻城后的城堡处置已经完成，正在结束本次攻城遭遇。";
        }

        string settlementName = string.IsNullOrWhiteSpace(facts.SettlementName)
            ? "这座城堡"
            : facts.SettlementName.Trim();
        var sb = new StringBuilder();
        sb.AppendLine(settlementName + " 的攻城后城堡处置已经完成。");
        sb.AppendLine();
        sb.AppendLine("普通战俘：已收编 " + facts.RecruitedRegularPrisoners
            + " 人，已处决 " + facts.SlaughteredRegularPrisoners
            + " 人，带入者中仍有 " + facts.RemainingRegularPrisoners + " 人保持俘虏身份。");

        if (facts.RecruitedRegularPrisoners > 0 || facts.SlaughteredRegularPrisoners > 0)
        {
            sb.AppendLine("名册：收编者已转入主队成员名册，处决者已从主队俘虏名册移除；未处置者继续作为俘虏。");
        }

        sb.AppendLine(facts.RetainedLordPrisoners > 0
            ? "被俘领主：" + facts.RetainedLordPrisoners + " 人保持俘虏身份；普通战俘的收编与屠戮标签不会结算领主。"
            : "被俘领主：本次没有带入；领主处置接口仍保持独立且未启用处决结算。");
        sb.AppendLine(BuildMoraleLine(facts));
        sb.AppendLine();
        sb.AppendLine("城堡本身已按默认宽恕完成原版围城结算；该结算不覆盖上述战俘名册与军心后果。");
        sb.AppendLine("正在结束本次攻城遭遇，并进入后续结算。");
        return sb.ToString();
    }

    private static string BuildMoraleLine(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        if (facts.SoldierMoralePenaltyApplied)
        {
            return "军心：收编战俘后未在场景内完成安抚，主队士气已降低 "
                + SiegeCastleSoldierReactionProfile.UnappeasedMoralePenalty + "。";
        }

        if (facts.SoldierAppeasementRequired && facts.SoldierAppeasementApplied)
        {
            return "军心：随行士兵已经在场景内接受安抚，本次收编未扣除士气。";
        }

        if (facts.SoldierAppeasementRequired)
        {
            return "军心：收编引发的不满尚未完成离场结算。";
        }

        return facts.RecruitedRegularPrisoners > 0
            ? "军心：本次收编时没有可安抚的随行士兵在场，不触发士气扣除。"
            : "军心：本次未收编普通战俘，不触发安兵与士气扣除。";
    }
}
