using System;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free count policy for staged regular-prisoner allocations.
/// Runtime code owns concrete Bannerlord troop rosters and supplies the random roll.
/// </summary>
public static class SiegeCastlePrisonerAllocationProfile
{
    private static readonly Regex ArabicQualifiedCountRegex = new Regex(
        @"(?<!\d)(?<count>\d{1,4})(?!\d)\s*(?:名|人|个|位)",
        RegexOptions.Compiled);

    private static readonly Regex ArabicBareCountRegex = new Regex(
        @"(?<!\d)(?<count>\d{1,4})(?!\d)\s*(?:战俘|俘虏|士兵|降兵|降卒|人质)",
        RegexOptions.Compiled);

    private static readonly Regex ChineseCountRegex = new Regex(
        @"(?<count>[零〇一二两三四五六七八九十百]{1,6})\s*(?:名|人|个|位)",
        RegexOptions.Compiled);

    private static readonly string[] AllKeywords =
    {
        "全部", "全都", "所有", "全员", "剩余", "剩下", "其余", "余下", "杀光", "全杀"
    };

    private static readonly string[] ResetKeywords =
    {
        "反悔", "改判", "重新安排", "重新处置", "全部重来", "推翻之前", "取消之前", "不算之前"
    };

    public static SiegeCastlePrisonerQuantityDecision Resolve(
        string playerText,
        int availableCount,
        double randomRoll)
    {
        int available = Math.Max(0, availableCount);
        if (available == 0)
        {
            return new SiegeCastlePrisonerQuantityDecision(0, false, false, IsPlanResetRequested(playerText), "none_available");
        }

        bool reset = IsPlanResetRequested(playerText);
        if (ContainsAny(playerText, AllKeywords))
        {
            return new SiegeCastlePrisonerQuantityDecision(available, true, true, reset, "all_available");
        }

        if (TryParseExplicitCount(playerText, out int explicitCount))
        {
            return new SiegeCastlePrisonerQuantityDecision(
                Math.Min(available, Math.Max(1, explicitCount)),
                true,
                false,
                reset,
                explicitCount > available ? "explicit_clamped" : "explicit_count");
        }

        return new SiegeCastlePrisonerQuantityDecision(available, false, true, reset, "default_all_unspecified_count");
    }

    public static bool IsPlanResetRequested(string playerText)
        => ContainsAny(playerText, ResetKeywords);

    public static bool RequestsAllAvailable(string playerText)
        => ContainsAny(playerText, AllKeywords);

    public static bool RequestsEliteTroops(string playerText)
        => ContainsAny(playerText, new[] { "精锐", "高阶", "高级", "强壮", "最强", "老兵" });

    public static bool RequestsLowTierTroops(string playerText)
        => ContainsAny(playerText, new[] { "低阶", "低级", "弱小", "最弱", "新兵", "农奴", "民兵" });

    public static bool TryParseExplicitCount(string playerText, out int count)
    {
        string text = playerText ?? string.Empty;
        Match arabic = ArabicQualifiedCountRegex.Match(text);
        if (!arabic.Success)
        {
            arabic = ArabicBareCountRegex.Match(text);
        }
        if (arabic.Success && int.TryParse(arabic.Groups["count"].Value, out count) && count > 0)
        {
            return true;
        }

        Match chinese = ChineseCountRegex.Match(text);
        if (chinese.Success)
        {
            count = ParseChineseNumber(chinese.Groups["count"].Value);
            return count > 0;
        }

        count = 0;
        return false;
    }

    private static int ParseChineseNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        int total = 0;
        int current = 0;
        foreach (char ch in value)
        {
            int digit = ChineseDigit(ch);
            if (digit >= 0)
            {
                current = digit;
                continue;
            }
            int unit = ch == '十' ? 10 : (ch == '百' ? 100 : 0);
            if (unit > 0)
            {
                total += Math.Max(1, current) * unit;
                current = 0;
            }
        }
        return total + current;
    }

    private static int ChineseDigit(char value)
    {
        return value switch
        {
            '零' or '〇' => 0,
            '一' => 1,
            '二' or '两' => 2,
            '三' => 3,
            '四' => 4,
            '五' => 5,
            '六' => 6,
            '七' => 7,
            '八' => 8,
            '九' => 9,
            _ => -1
        };
    }

    private static bool ContainsAny(string text, string[] keywords)
    {
        string source = text ?? string.Empty;
        foreach (string keyword in keywords)
        {
            if (source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class SiegeCastlePrisonerQuantityDecision
{
    public SiegeCastlePrisonerQuantityDecision(
        int requestedCount,
        bool countWasExplicit,
        bool usedAllAvailable,
        bool resetPreviousPlan,
        string reasonCode)
    {
        RequestedCount = Math.Max(0, requestedCount);
        CountWasExplicit = countWasExplicit;
        UsedAllAvailable = usedAllAvailable;
        ResetPreviousPlan = resetPreviousPlan;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public int RequestedCount { get; }

    public bool CountWasExplicit { get; }

    public bool UsedAllAvailable { get; }

    public bool ResetPreviousPlan { get; }

    public string ReasonCode { get; }
}
