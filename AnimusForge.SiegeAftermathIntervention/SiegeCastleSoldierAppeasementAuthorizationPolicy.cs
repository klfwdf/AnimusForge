using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Conservative player-intent gate for the castle soldier-appeasement settlement.
/// Soldier acceptance alone must never waive the morale penalty unless the player actually
/// offered reassurance, compensation, a discipline explanation, or a spoil arrangement.
/// </summary>
public static class SiegeCastleSoldierAppeasementAuthorizationPolicy
{
    private static readonly string[] AppeasementTerms =
    {
        "安抚", "放心", "不要担心", "不必担心", "无需担心", "不会亏待", "不亏待",
        "补偿", "犒赏", "奖赏", "赏赐", "论功行赏", "记功", "战利品", "战利分配", "分配战利",
        "严明军纪", "约束军纪", "整顿军纪", "我会约束", "我会解释", "听我解释", "体谅你们", "理解你们的顾虑",
        "reassure", "compensate", "reward you", "share the spoils", "discipline explanation"
    };

    private static readonly string[] RefusalTerms =
    {
        "不安抚", "拒绝安抚", "不会补偿", "不给补偿", "没有补偿", "休想补偿", "别想补偿",
        "不做解释", "无需解释", "不必解释",
        "no compensation", "will not compensate", "no explanation"
    };

    private static readonly string[] DiscussionTerms =
    {
        "?", "？", "吗", "如何", "怎么办", "是否", "觉得", "看法", "意见", "能不能", "可不可以",
        "what do you think", "do you think", "should i", "should we", "would you"
    };

    public static SiegeCastleSoldierAppeasementAuthorizationDecision Evaluate(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_text_missing");
        }

        if (ContainsAny(text, DiscussionTerms))
        {
            return SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_discussion_not_appeasement");
        }

        if (ContainsAny(text, RefusalTerms))
        {
            return SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_refused_appeasement");
        }

        return ContainsAny(text, AppeasementTerms)
            ? SiegeCastleSoldierAppeasementAuthorizationDecision.Authorized("player_explicit_appeasement")
            : SiegeCastleSoldierAppeasementAuthorizationDecision.Denied("player_appeasement_not_found");
    }

    private static bool ContainsAny(string text, string[] terms)
    {
        foreach (string term in terms)
        {
            if (!string.IsNullOrWhiteSpace(term)
                && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class SiegeCastleSoldierAppeasementAuthorizationDecision
{
    private SiegeCastleSoldierAppeasementAuthorizationDecision(bool isAuthorized, string reasonCode)
    {
        IsAuthorized = isAuthorized;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAuthorized { get; }

    public string ReasonCode { get; }

    internal static SiegeCastleSoldierAppeasementAuthorizationDecision Authorized(string reasonCode)
    {
        return new SiegeCastleSoldierAppeasementAuthorizationDecision(true, reasonCode);
    }

    internal static SiegeCastleSoldierAppeasementAuthorizationDecision Denied(string reasonCode)
    {
        return new SiegeCastleSoldierAppeasementAuthorizationDecision(false, reasonCode);
    }
}
