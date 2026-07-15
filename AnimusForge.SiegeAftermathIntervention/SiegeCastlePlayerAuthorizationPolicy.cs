using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Conservative hard gate for player authorization of ordinary-prisoner outcomes.
/// A generic approval such as "同意" is valid only when the same speaker has a matching pending proposal.
/// </summary>
public static class SiegeCastlePlayerAuthorizationPolicy
{
    private static readonly string[] GlobalRejectionTerms =
    {
        "不同意", "拒绝", "取消", "算了", "否决", "不准", "驳回",
        "disagree", "reject", "cancel", "denied"
    };

    private static readonly string[] RecruitTerms =
    {
        "收编", "招降", "纳降", "编入", "归顺", "加入我军", "加入我的部队", "纳入队伍", "接受投降", "让他们加入",
        "recruit", "enlist", "join my army", "join us", "take them in"
    };

    private static readonly string[] RecruitNegationTerms =
    {
        "不收编", "不要收编", "别收编", "不同意收编", "拒绝收编", "不招降", "不要招降", "拒绝招降",
        "don't recruit", "do not recruit", "not recruit"
    };

    private static readonly string[] SlaughterTerms =
    {
        "屠戮", "处决", "处死", "杀掉", "杀死", "杀了", "杀光", "全杀了", "都杀了", "宰了", "斩杀", "砍了", "全部杀", "一个不留", "动手杀",
        "slaughter", "execute them", "kill them", "put them to death"
    };

    private static readonly string[] SlaughterNegationTerms =
    {
        "不屠戮", "不要屠戮", "别屠戮", "不同意屠戮", "拒绝屠戮", "不处决", "不要处决", "别处决", "拒绝处决", "不杀", "不要杀", "别杀", "不准杀",
        "留他们一命", "饶了他们", "spare them", "don't kill", "do not kill", "not kill"
    };

    private static readonly string[] GenericApprovalTerms =
    {
        "同意", "批准", "准了", "照办", "就这么办", "按你说的办", "按你的建议办", "依你", "依你所言", "如你所愿", "执行吧", "动手吧", "可以",
        "agreed", "approved", "do it", "proceed", "go ahead", "yes"
    };

    private static readonly string[] ShortApprovalTerms =
    {
        "好", "行", "准", "是", "ok", "okay"
    };

    private static readonly string[] QuestionOrDiscussionTerms =
    {
        "?", "？", "吗", "如何", "怎么办", "该不该", "要不要", "是否", "觉得", "建议", "看法", "意见", "能不能", "可不可以",
        "should we", "what do you think", "do you think", "shall we", "would you", "could we"
    };

    public static SiegeCastlePlayerAuthorizationDecision Evaluate(
        string playerText,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_text_missing");
        }

        bool discussion = ContainsAny(text, QuestionOrDiscussionTerms);
        bool recruit = !discussion && ContainsAny(text, RecruitTerms) && !ContainsAny(text, RecruitNegationTerms);
        bool slaughter = !discussion && ContainsAny(text, SlaughterTerms) && !ContainsAny(text, SlaughterNegationTerms);
        if (recruit && slaughter)
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_authorization_ambiguous");
        }
        if (recruit)
        {
            return SiegeCastlePlayerAuthorizationDecision.Authorized(
                SiegeCastlePrisonerDispositionKind.Recruit,
                usedPendingProposal: false,
                "player_explicit_recruit_authorization");
        }
        if (slaughter)
        {
            return SiegeCastlePlayerAuthorizationDecision.Authorized(
                SiegeCastlePrisonerDispositionKind.Slaughter,
                usedPendingProposal: false,
                "player_explicit_slaughter_authorization");
        }

        if (ContainsAny(text, GlobalRejectionTerms))
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_rejected_or_cancelled");
        }

        if (discussion)
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_discussion_not_authorization");
        }

        if (ContainsAny(text, GenericApprovalTerms) || EqualsAnyShortApproval(text))
        {
            return pendingProposalForSpeaker == SiegeCastlePrisonerDispositionKind.None
                ? SiegeCastlePlayerAuthorizationDecision.Denied("generic_approval_requires_matching_proposal")
                : SiegeCastlePlayerAuthorizationDecision.Authorized(
                    pendingProposalForSpeaker,
                    usedPendingProposal: true,
                    "player_approved_matching_proposal");
        }

        return SiegeCastlePlayerAuthorizationDecision.Denied("player_authorization_not_found");
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

    private static bool EqualsAnyShortApproval(string text)
    {
        string normalized = (text ?? string.Empty).Trim().Trim('。', '！', '!', '？', '?', '，', ',', '.', '；', ';', '…').Trim();
        foreach (string term in ShortApprovalTerms)
        {
            if (string.Equals(normalized, term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class SiegeCastlePlayerAuthorizationDecision
{
    private SiegeCastlePlayerAuthorizationDecision(
        bool isAuthorized,
        SiegeCastlePrisonerDispositionKind disposition,
        bool usedPendingProposal,
        string reasonCode)
    {
        IsAuthorized = isAuthorized;
        Disposition = disposition;
        UsedPendingProposal = usedPendingProposal;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAuthorized { get; }

    public SiegeCastlePrisonerDispositionKind Disposition { get; }

    public bool UsedPendingProposal { get; }

    public string ReasonCode { get; }

    internal static SiegeCastlePlayerAuthorizationDecision Authorized(
        SiegeCastlePrisonerDispositionKind disposition,
        bool usedPendingProposal,
        string reasonCode)
    {
        return new SiegeCastlePlayerAuthorizationDecision(true, disposition, usedPendingProposal, reasonCode);
    }

    internal static SiegeCastlePlayerAuthorizationDecision Denied(string reasonCode)
    {
        return new SiegeCastlePlayerAuthorizationDecision(
            false,
            SiegeCastlePrisonerDispositionKind.None,
            usedPendingProposal: false,
            reasonCode);
    }
}
