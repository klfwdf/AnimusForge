using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for GCCZ postprocess throttling.
/// AF adapters own MCM reads and runtime counters.
/// </summary>
public static class SiegePostprocessFrequencyProfile
{
    public const int MinFrequencyLimit = 1;

    public const int MaxFrequencyLimit = 10;

    public const int DefaultFrequencyLimit = MaxFrequencyLimit;

    public const int FrequencyWindowSize = 10;

    public const string ThrottleSource = "gccz_postprocess_frequency_throttle";

    public const string AiReviewCandidateBypassSource = "gccz_postprocess_ai_review_candidate_bypass";

    public const string MissionStartResetSource = "gccz_mission_start";

    public const string MissionEndResetSource = "gccz_mission_end";

    public static int ClampFrequencyLimit(int value)
    {
        return Math.Max(MinFrequencyLimit, Math.Min(MaxFrequencyLimit, value));
    }

    /// <summary>
    /// Returns true only to let AF spend a postprocess call for AI review.
    /// This never emits ACTION tags or applies settlement outcomes by itself.
    /// </summary>
    public static bool ShouldBypassThrottleForPlayerIntentReview(string playerText, bool replyIsDirectPlayerResponse)
    {
        return replyIsDirectPlayerResponse && LooksLikeHighValuePlayerIntentText(playerText);
    }

    public static bool LooksLikeHighValuePlayerIntentText(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return ContainsAny(text,
            "宽恕", "不杀", "放过", "饶了", "保护", "军纪",
            "救济", "发粮", "给粮", "分粮", "分发", "物资", "钱粮",
            "宣抚", "安民", "演讲", "训示", "安定",
            "盟誓", "归心", "效忠", "归附",
            "安兵", "安抚士兵", "补偿士兵", "战利安排",
            "召集", "集合", "叫民众", "叫平民", "传令", "通知民众", "聚集",
            "抢钱", "索取", "交钱", "交出", "收缴",
            "搜掠", "劫掠", "掠夺", "洗劫", "战利品",
            "血洗", "屠城", "屠杀", "杀光", "杀尽", "清洗",
            "殖民", "迁殖", "迁入", "清除原住民", "改换文化",
            "mercy", "relief", "inspire", "oath", "gather",
            "rob", "robbery", "plunder", "loot", "massacre", "colonize", "repopulation");
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(text) || needles == null)
        {
            return false;
        }

        foreach (string needle in needles)
        {
            if (!string.IsNullOrWhiteSpace(needle)
                && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
