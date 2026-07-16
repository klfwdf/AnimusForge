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
        if (facts.TreatedRegularPrisoners || facts.ReceivedRegularArmaments)
        {
            sb.Append("普通战俘流程：");
            if (facts.TreatedRegularPrisoners)
            {
                sb.Append("已善待并约束随军士兵不得虐待");
            }
            if (facts.TreatedRegularPrisoners && facts.ReceivedRegularArmaments)
            {
                sb.Append("；");
            }
            if (facts.ReceivedRegularArmaments)
            {
                sb.Append("已接收军械且物品直接入包");
            }
            sb.AppendLine("。流程标签保留为信任记忆，但不会替代最终处置。");
        }

        sb.AppendLine(BuildRegularTerminalLine(facts));
        if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary))
        {
            sb.AppendLine("被俘领主：" + facts.LordOutcomeSummary.Trim());
        }
        else
        {
            sb.AppendLine(facts.RetainedLordPrisoners > 0
                ? "被俘领主：" + facts.RetainedLordPrisoners + " 人保持俘虏身份；普通战俘群体标签不会结算领主。"
                : "被俘领主：本次没有仍待处置的带入领主；领主处决接口仍按设计延后。 ");
        }
        sb.AppendLine(BuildMoraleLine(facts));
        sb.AppendLine();
        sb.AppendLine("城堡本身已按默认宽恕完成原版围城结算；该结算不覆盖上述战俘名册与军心后果。");
        sb.AppendLine("正在结束本次攻城遭遇，并进入后续结算。");
        return sb.ToString();
    }

    private static string BuildRegularTerminalLine(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        int affected = facts.TerminalAffectedRegularPrisoners;
        string outcome = facts.RegularTerminalAction switch
        {
            SiegeCastleActionKind.ReleasePrisoners => "已释放 " + affected + " 人",
            SiegeCastleActionKind.SellPrisoners => "已贩卖 " + affected + " 人并获得 " + facts.TerminalGold + " 金币",
            SiegeCastleActionKind.RecruitPrisonersVoluntary => "已自愿收编 " + facts.RecruitedRegularPrisoners + " 人",
            SiegeCastleActionKind.RecruitPrisonersForced => "已强制收编 " + facts.RecruitedRegularPrisoners + " 人",
            SiegeCastleActionKind.LaborPrisonersVoluntary => "已按自愿劳役处置 " + affected + " 人",
            SiegeCastleActionKind.LaborPrisonersForced => "已按强制劳役处置 " + affected + " 人",
            SiegeCastleActionKind.InstructorPrisonersVoluntary => "已按自愿教官方案处置 " + affected + " 人",
            SiegeCastleActionKind.InstructorPrisonersForced => "已按强制教官方案处置 " + affected + " 人",
            SiegeCastleActionKind.SlaughterPrisoners => "场景内实际杀死 " + facts.SlaughteredRegularPrisoners + " 人；未实际死亡者不计入屠戮",
            _ => facts.RecruitedRegularPrisoners > 0
                ? "已收编 " + facts.RecruitedRegularPrisoners + " 人"
                : (facts.SlaughteredRegularPrisoners > 0
                    ? "场景内实际杀死 " + facts.SlaughteredRegularPrisoners + " 人"
                    : "没有完成互斥最终处置")
        };
        return "普通战俘最终处置：" + outcome + "；带入者中仍有 "
            + facts.RemainingRegularPrisoners + " 人保持俘虏身份。";
    }

    private static string BuildMoraleLine(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        if (facts.SoldierMoralePenaltyApplied)
        {
            return "军心：战俘处置引发不满且未在场景内完成安抚，主队士气已降低 "
                + SiegeCastleSoldierReactionProfile.UnappeasedMoralePenalty + "。";
        }

        if (facts.SoldierAppeasementRequired && facts.SoldierAppeasementApplied)
        {
            return "军心：随行士兵已在场景内接受安抚，本次处置不扣除额外士气。";
        }

        if (facts.SoldierAppeasementRequired)
        {
            return "军心：战俘处置引发的不满尚未完成离场结算。";
        }

        return "军心：本次处置未形成待安抚事件，不触发额外士气扣除。";
    }
}
