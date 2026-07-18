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
        "善待", "优待", "给俘虏", "给予物资", "发放物资", "分发物资", "给他们食物", "给你们食物", "分发口粮", "发放口粮", "给水", "发药", "包扎", "医治", "照料", "不许虐待", "不要虐待", "别虐待",
        "treat well", "give supplies", "give food", "care for"
    };

    private static readonly string[] ArmamentTerms =
    {
        "接收军械", "收缴军械", "收缴装备", "收缴武器", "收缴盔甲", "收缴甲胄", "收缴他们的武器", "收缴他们的盔甲", "收缴他们的甲胄", "交出装备", "交出武器", "交出盔甲", "交出甲胄", "卸下武器", "卸甲", "剥去甲胄", "收走盔甲", "收走他们的甲胄", "缴械", "搜身", "搜缴", "搜刮装备", "搜掠装备", "钱财",
        "confiscate", "hand over your weapons", "seize equipment", "disarm"
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

    private static readonly string[] LordDuelTerms =
    {
        "和你决斗", "与你决斗", "跟你决斗", "同你决斗", "向你挑战", "和我决斗", "与我决斗", "跟我决斗",
        "单挑", "决斗", "赢了我就放", "打赢我就放", "胜过我就放",
        "我已下马", "我已经下马", "我下马了", "我已收弓", "我已经收弓", "我已收起弓", "我已经收起弓", "我收起弓了",
        "我不用弓", "不使用弓", "按你的条件", "答应你的条件", "来吧，我答应你", "来吧我答应你", "公平对决", "公平决斗", "可以开始决斗",
        "fight me", "duel", "single combat", "i have dismounted", "i'm on foot", "i put away my bow",
        "i will not use a bow", "i accept your terms", "your terms are met", "begin the duel"
    };

    private static readonly string[] LordSellTerms =
    {
        "把你卖给赎金经纪人", "将你卖给赎金经纪人", "把你交给赎金经纪人", "将你交给赎金经纪人",
        "把你卖到酒馆", "将你卖到酒馆", "把你卖了", "将你卖了", "卖掉你", "卖了你", "赎卖你", "拿你换赎金", "卖你换赎金",
        "sell you to the ransom broker", "ransom you through the tavern", "sell you at the tavern"
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

        if (SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action))
        {
            SiegeCastlePlayerAuthorizationDecision disposition = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
                text,
                pendingProposalForSpeaker);
            SiegeCastlePrisonerDispositionKind required = SiegeCastlePrisonerDispositionKindProfile.FromAction(action);
            if (disposition.IsAuthorized && disposition.Disposition == required)
            {
                return SiegeCastleDirectActionAuthorizationDecision.Authorized(disposition.ReasonCode);
            }

            // A direct offer/question may resolve only through a willing prisoner's reply.
            if (SiegeCastleActionKindProfile.IsVoluntary(action)
                && SiegeCastlePlayerAuthorizationPolicy.HasPositiveIntent(text, required))
            {
                string reason = required switch
                {
                    SiegeCastlePrisonerDispositionKind.Recruit => "player_voluntary_recruit_negotiation",
                    SiegeCastlePrisonerDispositionKind.Labor => "player_voluntary_labor_negotiation",
                    SiegeCastlePrisonerDispositionKind.Instructor => "player_voluntary_instructor_negotiation",
                    _ => "player_voluntary_castle_negotiation"
                };
                return SiegeCastleDirectActionAuthorizationDecision.Authorized(reason);
            }
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_disposition_authorization_required");
        }

        if (ContainsAny(text, GlobalRefusalTerms))
        {
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_rejected_or_cancelled");
        }

        if (action == SiegeCastleActionKind.SellLord
            && SiegeCastlePlayerAuthorizationPolicy.IsDiscussionText(text))
        {
            return SiegeCastleDirectActionAuthorizationDecision.Denied("player_discussion_not_authorization");
        }

        bool matched = action switch
        {
            SiegeCastleActionKind.TreatPrisoners => ContainsAny(text, TreatTerms),
            SiegeCastleActionKind.ReceiveArmaments => ContainsAny(text, ArmamentTerms),
            SiegeCastleActionKind.RecruitLord => ContainsAny(text, LordRecruitTerms),
            SiegeCastleActionKind.SellLord => ContainsAny(text, LordSellTerms),
            SiegeCastleActionKind.DuelLord => ContainsAny(text, LordDuelTerms),
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
