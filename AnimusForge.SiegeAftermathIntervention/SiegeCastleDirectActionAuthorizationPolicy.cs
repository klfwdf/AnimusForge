using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Conservative lexical gate used after the role/direct-reply checks. It does not decide
/// willingness; the response tag plus the trust threshold decide voluntary vs forced service.
/// </summary>
public static class SiegeCastleDirectActionAuthorizationPolicy
{
    private static readonly string[] GlobalRefusalTerms =
    {
        "取消这个命令", "算了", "拒绝执行", "不同意这样处置", "停止处置",
        "cancel", "do not", "don't", "refuse", "stop"
    };

    private static readonly string[] TreatTerms =
    {
        "善待", "优待", "给俘虏", "给予物资", "发放物资", "给他们食物", "给你们食物", "给水", "医治", "照料", "不许虐待", "不要虐待", "别虐待",
        "treat well", "give supplies", "give food", "care for"
    };

    private static readonly string[] ArmamentTerms =
    {
        "接收军械", "收缴军械", "收缴装备", "交出装备", "交出武器", "交出盔甲", "卸下武器", "缴械", "搜身", "搜缴", "钱财",
        "confiscate", "hand over your weapons", "seize equipment", "disarm"
    };

    private static readonly string[] ReleaseTerms =
    {
        "释放", "放了他们", "放你们走", "放走", "恢复自由", "准许离开", "饶你们一命",
        "release", "set free", "let them go", "let you go"
    };

    private static readonly string[] SellTerms =
    {
        "贩卖", "卖掉", "卖给", "送去赎卖", "换取赎金", "押去奴隶市场",
        "sell the prisoners", "ransom them", "slave market"
    };

    private static readonly string[] RecruitTerms =
    {
        "收编", "招降", "纳降", "编入", "归顺", "加入我军", "加入我的部队", "加入我们", "为我效力",
        "recruit", "enlist", "join my army", "join us"
    };

    private static readonly string[] LaborTerms =
    {
        "劳役", "服刑", "农奴", "修缮道路", "修路", "修复道路", "修缮城堡", "劳动赎罪", "做苦工",
        "forced labor", "work sentence", "repair the roads", "labor service"
    };

    private static readonly string[] InstructorTerms =
    {
        "充当教官", "担任教官", "训练新兵", "操练新兵", "教授军务", "教导新兵", "训练志愿兵",
        "serve as instructors", "train recruits", "drill recruits"
    };

    private static readonly string[] LordRecruitTerms =
    {
        "收编你", "招揽你", "投靠我", "向我效忠", "加入我的国家", "加入我的王国", "加入我的家族", "成为我的同伴", "引见族长", "写信给族长", "写信给你的族长", "拥立我",
        "join my kingdom", "join my clan", "serve me", "introduce your clan leader"
    };

    private static readonly string[] LordExecuteTerms =
    {
        "处决你", "将你处决", "砍你的头", "斩首", "对你行刑", "送上断头台",
        "execute you", "behead you"
    };

    public static SiegeCastleDirectActionAuthorizationDecision Evaluate(
        SiegeCastleActionKind action,
        string playerText,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker = SiegeCastlePrisonerDispositionKind.None)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_text_missing");
        }

        if (action == SiegeCastleActionKind.SlaughterPrisoners
            || SiegeCastleActionKindProfile.IsRecruitment(action))
        {
            SiegeCastlePlayerAuthorizationDecision disposition = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
                text,
                pendingProposalForSpeaker);
            SiegeCastlePrisonerDispositionKind required = action == SiegeCastleActionKind.SlaughterPrisoners
                ? SiegeCastlePrisonerDispositionKind.Slaughter
                : SiegeCastlePrisonerDispositionKind.Recruit;
            if (disposition.IsAuthorized && disposition.Disposition == required)
            {
                return SiegeCastleDirectActionAuthorizationDecision.Authorized(disposition.ReasonCode);
            }

            // A direct offer/question to the prisoners may resolve as voluntary recruitment.
            if (action == SiegeCastleActionKind.RecruitPrisonersVoluntary
                && ContainsAny(text, RecruitTerms)
                && !ContainsAny(text, new[] { "不收编", "别加入", "不要加入", "don't join", "do not join" }))
            {
                return SiegeCastleDirectActionAuthorizationDecision.Authorized("player_voluntary_recruit_negotiation");
            }
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_disposition_authorization_required");
        }

        if (ContainsAny(text, GlobalRefusalTerms))
        {
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_rejected_or_cancelled");
        }

        bool matched = action switch
        {
            SiegeCastleActionKind.TreatPrisoners => ContainsAny(text, TreatTerms),
            SiegeCastleActionKind.ReceiveArmaments => ContainsAny(text, ArmamentTerms),
            SiegeCastleActionKind.ReleasePrisoners => ContainsAny(text, ReleaseTerms),
            SiegeCastleActionKind.SellPrisoners => ContainsAny(text, SellTerms),
            SiegeCastleActionKind.LaborPrisonersVoluntary => ContainsAny(text, LaborTerms),
            SiegeCastleActionKind.LaborPrisonersForced => ContainsAny(text, LaborTerms),
            SiegeCastleActionKind.InstructorPrisonersVoluntary => ContainsAny(text, InstructorTerms),
            SiegeCastleActionKind.InstructorPrisonersForced => ContainsAny(text, InstructorTerms),
            SiegeCastleActionKind.RecruitLord => ContainsAny(text, LordRecruitTerms),
            SiegeCastleActionKind.ExecuteLord => ContainsAny(text, LordExecuteTerms),
            _ => false
        };
        return matched
            ? SiegeCastleDirectActionAuthorizationDecision.Authorized("player_explicit_castle_action")
            : SiegeCastleDirectActionAuthorizationDecision.Denied("player_castle_action_not_found");
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

public sealed class SiegeCastleDirectActionAuthorizationDecision
{
    private SiegeCastleDirectActionAuthorizationDecision(bool isAuthorized, string reasonCode)
    {
        IsAuthorized = isAuthorized;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAuthorized { get; }

    public string ReasonCode { get; }

    internal static SiegeCastleDirectActionAuthorizationDecision Authorized(string reasonCode)
        => new SiegeCastleDirectActionAuthorizationDecision(true, reasonCode);

    internal static SiegeCastleDirectActionAuthorizationDecision Denied(string reasonCode)
        => new SiegeCastleDirectActionAuthorizationDecision(false, reasonCode);
}
