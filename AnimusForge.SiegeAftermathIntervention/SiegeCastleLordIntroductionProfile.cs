using System;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for non-clan-leader captive lords introducing the player to their clan leader.
/// The runtime bridge owns live Hero lookup, delayed delivery, and memory recording.
/// </summary>
public static class SiegeCastleLordIntroductionProfile
{
    public const string DiagnosticCategory = "CastleLordIntroduction";

    public const string MemoryTitle = "城堡领主引荐信";

    public const int MinDeliveryDays = 1;

    public const int MaxDeliveryDays = 2;

    public const int MaxLetterChars = 420;

    public const string RecruitLordStrictRuntimeRule = "[ACTION:收编领主] 必须是当前被俘领主明确接受进入收编/引荐流程；非族长默认只写信引见族长，1-2天后送达，不生成AF信使部队；除非正文明确背叛家族成为玩家同伴，否则不得写成直接收编成功。";

    public const string PlayerIntroductionMemoryKind = "gccz_castle_lord_introduction";

    public static string BuildNonLeaderPathText(string introducerName, string leaderName)
    {
        string introducer = string.IsNullOrWhiteSpace(introducerName) ? "当前战败领主" : introducerName.Trim();
        string leader = string.IsNullOrWhiteSpace(leaderName) ? "家族族长" : leaderName.Trim();
        return introducer + "不是家族族长：本次只记录其战败被俘后的引荐意向，并由其写信向" + leader + "引见玩家；信件将在1-2天后到达，不生成AF信使部队。";
    }

    public static string BuildFallbackLetterBody(string introducerName, string leaderName, string castleName)
    {
        string introducer = string.IsNullOrWhiteSpace(introducerName) ? "我" : introducerName.Trim();
        string leader = string.IsNullOrWhiteSpace(leaderName) ? "族长" : leaderName.Trim();
        string castle = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return leader + "，" + introducer + "在" + castle + "战败被俘后与胜利方首领谈及家族前途。此人希望与我族接触，条件与诚意仍需您亲自判断；我只作引见，不代替家族作最后承诺。";
    }

    public static string SanitizeAiLetterBody(string rawText, string fallbackText)
    {
        string fallback = string.IsNullOrWhiteSpace(fallbackText) ? "战败领主已同意写信为玩家引见家族族长，但信中未留下更多内容。" : fallbackText.Trim();
        string text = rawText ?? string.Empty;
        text = Regex.Replace(text, @"\[ACTION:[^\]]+\]", "", RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return TrimToMaxChars(fallback, MaxLetterChars);
        }

        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }

        return TrimToMaxChars(text, MaxLetterChars);
    }

    public static string BuildQueuedMemoryText(string castleName, string introducerName, string leaderName, int dueDays, string letterBody)
    {
        string castle = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        string introducer = string.IsNullOrWhiteSpace(introducerName) ? "当前战败领主" : introducerName.Trim();
        string leader = string.IsNullOrWhiteSpace(leaderName) ? "家族族长" : leaderName.Trim();
        int days = ClampDeliveryDays(dueDays);
        return castle + "：" + introducer + "已答应为玩家写信引见" + leader + "；预计" + days + "天后到达。信件摘录：" + SanitizeAiLetterBody(letterBody, BuildFallbackLetterBody(introducer, leader, castle));
    }

    public static string BuildDeliveredMemoryText(string castleName, string introducerName, string leaderName, string letterBody)
    {
        string castle = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        string introducer = string.IsNullOrWhiteSpace(introducerName) ? "当前战败领主" : introducerName.Trim();
        string leader = string.IsNullOrWhiteSpace(leaderName) ? "家族族长" : leaderName.Trim();
        return castle + "：" + introducer + "此前以战败被俘身份为玩家写下的引荐信已送达" + leader + "。信件内容：" + SanitizeAiLetterBody(letterBody, BuildFallbackLetterBody(introducer, leader, castle));
    }

    public static string BuildDiagnosticText(string introducerId, string leaderId, int dueDay, int currentDay)
    {
        return "introducer=" + (introducerId ?? "N/A")
            + " leader=" + (leaderId ?? "N/A")
            + " dueDay=" + dueDay
            + " currentDay=" + currentDay;
    }

    public static int ClampDeliveryDays(int days)
    {
        if (days < MinDeliveryDays)
        {
            return MinDeliveryDays;
        }

        return days > MaxDeliveryDays ? MaxDeliveryDays : days;
    }

    private static string TrimToMaxChars(string value, int maxChars)
    {
        string text = value ?? string.Empty;
        int max = Math.Max(1, maxChars);
        if (text.Length <= max)
        {
            return text;
        }

        return text.Substring(0, max - 1).TrimEnd() + "…";
    }
}
