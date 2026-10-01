using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Only describes committed lifecycle events/counters; never turns a command into its outcome.</summary>
public static class SettlementRuleMemoryEventText
{
    public static string Aftermath(string actor, string outcome)
    {
        string action = outcome == "Pillage" ? "劫掠" : outcome == "Devastate" ? "摧毁" : "宽恕";
        return "当事人“" + (actor ?? "未知统帅") + "”在该城的战后处置已结算为“" + action + "”。";
    }

    public static string CompletedIntervention(string actor, string outcome, long lootValue, long gold,
        int civilianDeaths, int notableDeaths, int reliefGold, int reliefFood)
        => Aftermath(actor, outcome) + "已结算物资掠取估值" + Math.Max(0, lootValue)
            + "，掠取第纳尔" + Math.Max(0, gold) + "，平民死亡" + Math.Max(0, civilianDeaths)
            + "，要人死亡" + Math.Max(0, notableDeaths) + "；已落实救济第纳尔" + Math.Max(0, reliefGold)
            + "、食物" + Math.Max(0, reliefFood) + "。上述为实际结算记录，不代表后续承诺已兑现。";

    public static string Policy(string eventKind, string title)
    {
        string status;
        switch (eventKind)
        {
            case "published": status = "已发布"; break;
            case "renewed": status = "已续期"; break;
            case "abolished": status = "已废除"; break;
            case "expired": status = "已到期"; break;
            default: return string.Empty;
        }
        return "地方政策《" + title + "》在该城" + status + "。这只确认政策状态，不代表预期经济或民心效果已经发生。";
    }
}
