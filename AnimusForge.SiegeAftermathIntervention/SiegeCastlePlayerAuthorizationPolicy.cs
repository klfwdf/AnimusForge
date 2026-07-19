using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Conservative hard gate for player authorization of ordinary-prisoner outcomes.
/// A generic approval such as "同意" is valid only when the same speaker has a matching pending proposal.
/// </summary>
public static class SiegeCastlePlayerAuthorizationPolicy
{
    private static readonly string[] GlobalRejectionTerms =
    {
        "不同意", "拒绝", "取消", "算了", "否决", "不准", "驳回", "不可以", "不能这么做", "不行",
        "disagree", "reject", "cancel", "denied", "not approved"
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

    private static readonly string[] ReleaseTerms =
    {
        "释放", "释放战俘", "放了他们", "放你们走", "放走", "恢复自由", "准许离开", "饶你们一命",
        "release", "set free", "let them go", "let you go"
    };

    private static readonly string[] ReleaseNegationTerms =
    {
        "不释放", "不要释放", "别释放", "不能释放", "不放他们", "不要放走", "别放走", "不准离开",
        "don't release", "do not release", "not release"
    };

    private static readonly string[] SellTerms =
    {
        "贩卖", "卖掉", "卖给", "发卖", "发卖为奴", "送去赎卖", "换取赎金", "押去奴隶市场",
        "sell the prisoners", "ransom them", "slave market"
    };

    private static readonly string[] SellNegationTerms =
    {
        "不贩卖", "不要贩卖", "别贩卖", "不卖", "不要卖", "别卖", "不发卖",
        "don't sell", "do not sell", "not sell"
    };

    private static readonly string[] GenericLaborTerms =
    {
        "劳役", "服刑", "劳动赎罪", "做苦工",
        "forced labor", "work sentence", "labor service"
    };

    private static readonly string[] ExternalLaborTerms =
    {
        "农奴", "发配为农奴", "发配农奴", "发放农奴", "送去当农奴", "充作农奴", "编为农奴",
        "派往村庄", "送往村庄", "送去村庄", "修缮道路", "修路", "修复道路", "耕种", "耕田", "农田", "农庄", "庄园",
        "repair the roads", "send to the villages", "serf"
    };

    private static readonly string[] RepairCastleTerms =
    {
        "修缮城堡", "修复城堡", "修缮城池", "修复城池", "修缮城墙", "修复城墙", "重建城墙",
        "修缮城防", "修复城防", "重建城防", "修缮要塞", "修复要塞", "修城", "修筑城防",
        "repair the castle", "repair the walls", "rebuild the fortifications"
    };

    private static readonly string[] LaborNegationTerms =
    {
        "不劳役", "不要劳役", "免除劳役", "不服刑", "不当农奴", "不要当农奴", "不做苦工",
        "no forced labor", "do not use forced labor"
    };

    private static readonly string[] InstructorTerms =
    {
        "充当教官", "担任教官", "做教官", "当教官", "训练新兵", "操练新兵", "教授军务", "教导新兵", "训练志愿兵",
        "serve as instructors", "train recruits", "drill recruits"
    };

    private static readonly string[] InstructorNegationTerms =
    {
        "不当教官", "不要当教官", "别当教官", "不做教官", "不训练新兵", "不要训练新兵",
        "do not serve as instructors", "don't train recruits"
    };

    private static readonly string[] GenericApprovalTerms =
    {
        "同意", "批准", "准了", "照办", "就这么办", "按你说的办", "按你的建议办", "依你所言", "如你所愿", "执行吧", "动手吧",
        "agreed", "approved", "do it", "proceed", "go ahead"
    };

    private static readonly string[] ShortApprovalTerms =
    {
        "好", "行", "准", "是", "可以", "依你", "ok", "okay", "yes"
    };

    private static readonly string[] ShortRejectionTerms =
    {
        "不", "否", "不行", "不可以", "no", "nope"
    };

    private static readonly string[] QuestionOrDiscussionTerms =
    {
        "?", "？", "吗", "如何", "怎么办", "该不该", "要不要", "是否", "觉得", "建议", "看法", "意见", "能不能", "可不可以",
        "should we", "what do you think", "do you think", "shall we", "would you", "could we"
    };

    private static readonly string[] GeneralDispositionAdviceTerms =
    {
        "怎么处置", "如何处置", "怎么处理这些俘虏", "如何处理这些俘虏", "该怎么处理", "有什么建议", "你的建议", "你的意见", "有什么请求", "你们有什么请求",
        "what should we do with", "how should we handle", "what do you suggest"
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

        int intentCount = CountDispositionIntents(text);
        if (intentCount > 1)
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_authorization_ambiguous");
        }

        SiegeCastlePrisonerDispositionKind intent = DetectIntent(text);
        if (pendingProposalForSpeaker == SiegeCastlePrisonerDispositionKind.Labor
            && intent == SiegeCastlePrisonerDispositionKind.RepairCastle
            && IsBareLaborReference(text)
            && (ContainsAny(text, GenericApprovalTerms) || EqualsAny(text, ShortApprovalTerms)))
        {
            return SiegeCastlePlayerAuthorizationDecision.Authorized(
                pendingProposalForSpeaker,
                usedPendingProposal: true,
                "player_approved_matching_proposal");
        }
        if (intent != SiegeCastlePrisonerDispositionKind.None && !IsDiscussionText(text))
        {
            return SiegeCastlePlayerAuthorizationDecision.Authorized(
                intent,
                usedPendingProposal: false,
                GetExplicitReasonCode(intent));
        }

        if (ContainsAny(text, GlobalRejectionTerms) || EqualsAny(text, ShortRejectionTerms))
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_rejected_or_cancelled");
        }

        if (IsDiscussionText(text))
        {
            return SiegeCastlePlayerAuthorizationDecision.Denied("player_discussion_not_authorization");
        }

        if (ContainsAny(text, GenericApprovalTerms) || EqualsAny(text, ShortApprovalTerms))
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

    public static SiegeCastlePrisonerDispositionKind DetectIntent(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (CountDispositionIntents(text) != 1)
        {
            return SiegeCastlePrisonerDispositionKind.None;
        }
        foreach (SiegeCastlePrisonerDispositionKind disposition in DetectPositiveIntents(text))
        {
            return disposition;
        }
        return SiegeCastlePrisonerDispositionKind.None;
    }

    public static IReadOnlyList<SiegeCastlePrisonerDispositionKind> DetectPositiveIntents(string playerText)
    {
        string text = (playerText ?? string.Empty).Trim();
        var result = new List<SiegeCastlePrisonerDispositionKind>();
        foreach (SiegeCastlePrisonerDispositionKind disposition in new[]
        {
            SiegeCastlePrisonerDispositionKind.Recruit,
            SiegeCastlePrisonerDispositionKind.Slaughter,
            SiegeCastlePrisonerDispositionKind.Release,
            SiegeCastlePrisonerDispositionKind.Sell,
            SiegeCastlePrisonerDispositionKind.RepairCastle,
            SiegeCastlePrisonerDispositionKind.Labor,
            SiegeCastlePrisonerDispositionKind.Instructor
        })
        {
            if (HasPositiveIntent(text, disposition))
            {
                result.Add(disposition);
            }
        }
        return result;
    }

    public static bool HasPositiveIntent(string playerText, SiegeCastlePrisonerDispositionKind disposition)
    {
        string text = playerText ?? string.Empty;
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => ContainsAny(text, RecruitTerms) && !ContainsAny(text, RecruitNegationTerms),
            SiegeCastlePrisonerDispositionKind.Slaughter => ContainsAny(text, SlaughterTerms) && !ContainsAny(text, SlaughterNegationTerms),
            SiegeCastlePrisonerDispositionKind.Release => ContainsAny(text, ReleaseTerms) && !ContainsAny(text, ReleaseNegationTerms),
            SiegeCastlePrisonerDispositionKind.Sell => ContainsAny(text, SellTerms) && !ContainsAny(text, SellNegationTerms),
            SiegeCastlePrisonerDispositionKind.Labor => HasExternalLaborIntent(text),
            SiegeCastlePrisonerDispositionKind.RepairCastle => HasRepairCastleIntent(text),
            SiegeCastlePrisonerDispositionKind.Instructor => ContainsAny(text, InstructorTerms) && !ContainsAny(text, InstructorNegationTerms),
            _ => false
        };
    }

    public static bool IsDiscussionText(string playerText)
        => ContainsAny(playerText ?? string.Empty, QuestionOrDiscussionTerms);

    public static bool IsGeneralDispositionAdviceRequest(string playerText)
        => ContainsAny(playerText ?? string.Empty, GeneralDispositionAdviceTerms);

    private static int CountDispositionIntents(string text)
    {
        return DetectPositiveIntents(text).Count;
    }

    private static string GetExplicitReasonCode(SiegeCastlePrisonerDispositionKind disposition)
    {
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => "player_explicit_recruit_authorization",
            SiegeCastlePrisonerDispositionKind.Slaughter => "player_explicit_slaughter_authorization",
            SiegeCastlePrisonerDispositionKind.Release => "player_explicit_release_authorization",
            SiegeCastlePrisonerDispositionKind.Sell => "player_explicit_sell_authorization",
            SiegeCastlePrisonerDispositionKind.Labor => "player_explicit_labor_authorization",
            SiegeCastlePrisonerDispositionKind.RepairCastle => "player_explicit_repair_castle_authorization",
            SiegeCastlePrisonerDispositionKind.Instructor => "player_explicit_instructor_authorization",
            _ => "player_explicit_castle_disposition_authorization"
        };
    }

    private static bool HasExternalLaborIntent(string text)
    {
        return !ContainsAny(text, LaborNegationTerms)
            && !ContainsAny(text, RepairCastleTerms)
            && ContainsAny(text, ExternalLaborTerms);
    }

    private static bool HasRepairCastleIntent(string text)
    {
        if (ContainsAny(text, LaborNegationTerms))
        {
            return false;
        }
        return ContainsAny(text, RepairCastleTerms)
            || (ContainsAny(text, GenericLaborTerms) && !ContainsAny(text, ExternalLaborTerms));
    }

    private static bool IsBareLaborReference(string text)
    {
        return ContainsAny(text, GenericLaborTerms)
            && !ContainsAny(text, ExternalLaborTerms)
            && !ContainsAny(text, RepairCastleTerms);
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

    private static bool EqualsAny(string text, string[] terms)
    {
        string normalized = (text ?? string.Empty).Trim().Trim('。', '！', '!', '？', '?', '，', ',', '.', '；', ';', '…').Trim();
        foreach (string term in terms)
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
