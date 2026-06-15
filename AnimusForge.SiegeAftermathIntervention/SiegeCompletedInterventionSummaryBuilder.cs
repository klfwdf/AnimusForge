using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCompletedInterventionSummaryBuilder
{
    public static string Build(SiegeCompletedInterventionSummaryFacts facts)
    {
        if (facts == null)
        {
            return "攻城后的入城处置已经完成，正在结束本次攻城遭遇。";
        }

        string settlementName = string.IsNullOrWhiteSpace(facts.SettlementName) ? "这座定居点" : facts.SettlementName.Trim();
        string action = DescribeAction(facts.AftermathKind, facts.CulturalRepopulationApplied);
        var sb = new StringBuilder();
        sb.AppendLine(settlementName + " 的攻城后" + action + "已经完成。");
        sb.AppendLine();
        if (facts.CulturalRepopulationApplied)
        {
            string targetCultureText = string.IsNullOrWhiteSpace(facts.TargetCultureText) ? "目标文化" : facts.TargetCultureText.Trim();
            sb.AppendLine("你选择了最高级不可逆处置：屠民迁殖。城镇将按毁坏结算，并被强行改为 " + targetCultureText + "。");
        }
        else if (facts.MassacreStarted)
        {
            sb.AppendLine("你的士兵已完成战后处置，城内残余抵抗均已肃清。");
        }
        else if (facts.PlunderStarted)
        {
            sb.AppendLine("你的士兵已经按胜利方战利权搜掠财物；离场结算已记录市场库存、市场金库和平民第纳尔。");
        }
        else
        {
            sb.AppendLine("你选择在场景中安抚民众，已按宽恕或安抚结果结算。");
        }

        sb.AppendLine();
        sb.AppendLine("市场物资：" + facts.MarketItemTotal + " 件 / " + facts.MarketStackKinds + " 类，估值 " + facts.MarketItemValue + "。");
        sb.AppendLine("市场金库：" + facts.MarketGold + " 第纳尔。");
        sb.AppendLine("民众第纳尔：" + facts.CivilianGold + "，目标数 " + facts.CivilianTargetsLooted + "。");
        sb.AppendLine();
        sb.AppendLine("正在结束本次攻城遭遇，并进入后续结算。");
        return sb.ToString();
    }

    public static string DescribeAction(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        switch (aftermathKind)
        {
            case SiegeAftermathResolutionKind.Devastate:
                return culturalRepopulationApplied ? "屠民迁殖" : "血洗与毁坏";
            case SiegeAftermathResolutionKind.Pillage:
                return "搜掠";
            case SiegeAftermathResolutionKind.ShowMercy:
                return "安抚";
            default:
                return "处置";
        }
    }
}
