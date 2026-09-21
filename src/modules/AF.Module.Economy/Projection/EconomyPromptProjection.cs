using System.Collections.Generic;
using System.Text;

namespace AnimusForge.Modules.Economy;

/// <summary>
/// Detached debt line captured by the Reward owner on the game thread.
/// It intentionally contains no Hero, Settlement, item, quest, or save object.
/// </summary>
internal sealed class EconomyDebtPromptLine
{
    internal EconomyDebtPromptLine(string debtId, int remainingValue, string deadline, string note)
    {
        DebtId = debtId ?? string.Empty;
        RemainingValue = remainingValue;
        Deadline = string.IsNullOrWhiteSpace(deadline) ? "未设定" : deadline;
        Note = string.IsNullOrWhiteSpace(note) ? "无" : note;
    }

    internal string DebtId { get; }
    internal int RemainingValue { get; }
    internal string Deadline { get; }
    internal string Note { get; }
}

/// <summary>
/// Pure text projection for economy prompt facts. The live Reward owner still
/// normalizes debt records and resolves current prices before calling here.
/// </summary>
internal static class EconomyPromptProjection
{
    internal static string BuildTrustStatus(int effectiveTrust, string levelText, int levelIndex)
    {
        return $"综合信任 {effectiveTrust}（{levelText ?? string.Empty}，{levelIndex}/10）";
    }

    internal static string BuildTrustPrompt(string behaviorText, string actionGuideText)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("本级语义：" + (behaviorText ?? string.Empty));
        builder.AppendLine("本级信用规则：" + (actionGuideText ?? string.Empty));
        builder.AppendLine("价值口径：总价值=第纳尔金额+物品估值（guidePrice * 数量）。");
        return builder.ToString().TrimEnd();
    }

    internal static string BuildHeroDebtHint(
        bool hasDebtContent,
        IEnumerable<EconomyDebtPromptLine> lines)
    {
        return hasDebtContent
            ? BuildDebtHint("【系统账目提示】玩家对你有以下承诺或欠款（分笔记录）：", null, lines)
            : string.Empty;
    }

    internal static string BuildSettlementMerchantDebtHint(
        string merchantLabel,
        bool hasDebtContent,
        IEnumerable<EconomyDebtPromptLine> lines)
    {
        return hasDebtContent
            ? BuildDebtHint(
                "【系统账目提示】玩家对你代表的" + (merchantLabel ?? string.Empty) + "有以下承诺或欠款（分笔记录）：",
                "【债务解除确认】若玩家本轮行为已被系统事实明确记录为偿还、豁免或免除",
                lines)
            : string.Empty;
    }

    private static string BuildDebtHint(
        string header,
        string secondLine,
        IEnumerable<EconomyDebtPromptLine> lines)
    {
        StringBuilder details = new StringBuilder();
        foreach (EconomyDebtPromptLine line in lines ?? new EconomyDebtPromptLine[0])
        {
            if (line == null)
            {
                continue;
            }
            details.Append("- [债务ID:").Append(line.DebtId).Append("] 玩家的承诺或欠款价值 ")
                .Append(line.RemainingValue)
                .Append(" 第纳尔，达成期限为：")
                .Append(line.Deadline)
                .Append("，备注：")
                .Append(line.Note)
                .AppendLine();
        }

        StringBuilder result = new StringBuilder();
        result.AppendLine(header ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(secondLine))
        {
            result.AppendLine(secondLine);
        }
        result.Append(details);
        return result.ToString().Trim();
    }
}
