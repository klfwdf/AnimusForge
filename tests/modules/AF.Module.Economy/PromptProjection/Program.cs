using System;
using System.Collections.Generic;
using AnimusForge.Modules.Economy;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("FAIL " + name);
        }
        _checks++;
    }

    private static void Main()
    {
        Check(EconomyPromptProjection.BuildTrustStatus(0, "中性观望", 6)
            == "综合信任 0（中性观望，6/10）", "trust status");
        Check(EconomyPromptProjection.BuildTrustPrompt("谨慎", "要求抵押")
            == "本级语义：谨慎" + Environment.NewLine
                + "本级信用规则：要求抵押" + Environment.NewLine
                + "价值口径：总价值=第纳尔金额+物品估值（guidePrice * 数量）。",
            "trust prompt");
        Check(EconomyPromptProjection.BuildHeroDebtHint(false, Array.Empty<EconomyDebtPromptLine>()) == string.Empty,
            "empty hero debt");
        Check(EconomyPromptProjection.BuildHeroDebtHint(true, Array.Empty<EconomyDebtPromptLine>())
            == "【系统账目提示】玩家对你有以下承诺或欠款（分笔记录）：", "aggregate-only hero debt");

        List<EconomyDebtPromptLine> lines = new List<EconomyDebtPromptLine>
        {
            new EconomyDebtPromptLine("gold-1", 120, "约 2 天内", "军饷"),
            null,
            new EconomyDebtPromptLine("item-2", 75, "", "")
        };
        string hero = EconomyPromptProjection.BuildHeroDebtHint(true, lines);
        Check(hero.StartsWith("【系统账目提示】玩家对你有以下承诺或欠款（分笔记录）：" + Environment.NewLine, StringComparison.Ordinal),
            "hero header");
        Check(hero.Contains("[债务ID:gold-1] 玩家的承诺或欠款价值 120 第纳尔，达成期限为：约 2 天内，备注：军饷"),
            "hero first line");
        Check(hero.Contains("[债务ID:item-2] 玩家的承诺或欠款价值 75 第纳尔，达成期限为：未设定，备注：无"),
            "hero defaults");

        string merchant = EconomyPromptProjection.BuildSettlementMerchantDebtHint("铁匠", true, lines);
        Check(merchant.StartsWith("【系统账目提示】玩家对你代表的铁匠有以下承诺或欠款（分笔记录）：" + Environment.NewLine, StringComparison.Ordinal),
            "merchant header");
        Check(merchant.Contains("【债务解除确认】若玩家本轮行为已被系统事实明确记录为偿还、豁免或免除"),
            "merchant confirmation");
        Check(!merchant.EndsWith("\n", StringComparison.Ordinal), "trimmed output");

        Console.WriteLine($"PASS economyPromptProjection checks={_checks}");
    }
}
