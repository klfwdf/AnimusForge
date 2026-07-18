using System;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Safety veto for an AI-confirmed captive-lord duel tag. The AI postprocessor is
/// authoritative for positive consent; this policy only rejects missing replies,
/// explicit refusals, and replies that still impose an unmet condition.
/// </summary>
public static class SiegeCastleLordDuelConsentPolicy
{
    private static readonly Regex CastleActionTagRegex = new Regex(
        SiegeCastleActionTagCatalog.AnyActionTagPattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] RefusalTerms =
    {
        "我拒绝决斗", "我拒绝你的挑战", "拒绝你的挑战", "我不接受", "我不答应", "我不会应战",
        "我不会和你决斗", "我不会与你决斗", "我不和你决斗", "我不与你决斗", "休想", "免谈", "不可能接受",
        "i refuse the duel", "i refuse your challenge", "i do not accept", "i don't accept",
        "i will not fight", "i won't fight", "i refuse to fight"
    };

    private static readonly string[] ConditionalTerms =
    {
        "先下马", "你先下马", "请下马", "先从马上下来", "下马后再", "下马再", "下马收弓", "等你下马", "除非你下马",
        "先放下弓", "先丢下弓", "先收起弓", "先收弓", "请收弓", "放下弓后", "收弓后", "不用弓再", "等你放下弓", "等你收弓", "除非你不用弓",
        "不得使用弓", "不准用弓", "不能用弓", "不得骑马", "不准骑马", "不能骑马",
        "只有你先", "除非你先", "dismount first", "first dismount", "only if you dismount", "unless you dismount",
        "put down your bow first", "only if you put down", "unless you put down", "no bow", "without your bow",
        "on foot first"
    };

    public static SiegeCastleLordDuelConsentDecision Evaluate(string lordReplyText)
    {
        string text = CastleActionTagRegex.Replace(lordReplyText ?? string.Empty, string.Empty).Trim();
        if (text.Length == 0)
        {
            return SiegeCastleLordDuelConsentDecision.Denied("castle_duel_lord_reply_missing");
        }
        if (ContainsAny(text, RefusalTerms))
        {
            return SiegeCastleLordDuelConsentDecision.Denied("castle_duel_lord_refused");
        }
        if (ContainsAny(text, ConditionalTerms))
        {
            return SiegeCastleLordDuelConsentDecision.Denied("castle_duel_lord_condition_not_fulfilled");
        }
        return SiegeCastleLordDuelConsentDecision.Accepted("castle_duel_lord_ai_tag_confirmed");
    }

    private static bool ContainsAny(string text, string[] terms)
    {
        foreach (string term in terms)
        {
            if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class SiegeCastleLordDuelConsentDecision
{
    private SiegeCastleLordDuelConsentDecision(bool isAccepted, string reasonCode)
    {
        IsAccepted = isAccepted;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAccepted { get; }

    public string ReasonCode { get; }

    internal static SiegeCastleLordDuelConsentDecision Accepted(string reasonCode)
        => new SiegeCastleLordDuelConsentDecision(true, reasonCode);

    internal static SiegeCastleLordDuelConsentDecision Denied(string reasonCode)
        => new SiegeCastleLordDuelConsentDecision(false, reasonCode);
}
