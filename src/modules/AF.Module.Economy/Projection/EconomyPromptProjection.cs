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
        Deadline = string.IsNullOrWhiteSpace(deadline) ? EconomyPromptTexts.Get("S009") : deadline;
        Note = string.IsNullOrWhiteSpace(note) ? EconomyPromptTexts.Get("S013") : note;
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
        return string.Format(EconomyPromptTexts.Get("S633"), effectiveTrust, levelText ?? string.Empty, levelIndex);
    }

    internal static string BuildTrustPrompt(string behaviorText, string actionGuideText)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(EconomyPromptTexts.Get("S634") + (behaviorText ?? string.Empty));
        builder.AppendLine(EconomyPromptTexts.Get("S635") + (actionGuideText ?? string.Empty));
        builder.AppendLine(EconomyPromptTexts.Get("S636"));
        return builder.ToString().TrimEnd();
    }

    internal static string BuildHeroDebtHint(
        bool hasDebtContent,
        IEnumerable<EconomyDebtPromptLine> lines)
    {
        return hasDebtContent
            ? BuildDebtHint(EconomyPromptTexts.Get("S637"), null, lines)
            : string.Empty;
    }

    internal static string BuildSettlementMerchantDebtHint(
        string merchantLabel,
        bool hasDebtContent,
        IEnumerable<EconomyDebtPromptLine> lines)
    {
        return hasDebtContent
            ? BuildDebtHint(
                EconomyPromptTexts.Get("S638") + (merchantLabel ?? string.Empty) + EconomyPromptTexts.Get("S639"),
                EconomyPromptTexts.Get("S640"),
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
            details.Append(EconomyPromptTexts.Get("S641")).Append(line.DebtId).Append(EconomyPromptTexts.Get("S642"))
                .Append(line.RemainingValue)
                .Append(EconomyPromptTexts.Get("S015"))
                .Append(line.Deadline)
                .Append(EconomyPromptTexts.Get("S016"))
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
