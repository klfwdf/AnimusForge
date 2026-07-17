using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCastlePostprocessContextProfile
{
    public static string Build(SiegeCastlePostprocessContextFacts facts)
    {
        facts ??= SiegeCastlePostprocessContextFacts.Empty;
        SiegeCastlePlayerAuthorizationDecision authorization = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
            facts.PlayerText,
            facts.PendingProposalForSpeaker);
        SiegeCastleSoldierAppeasementAuthorizationDecision appeasementAuthorization =
            SiegeCastleSoldierAppeasementAuthorizationPolicy.Evaluate(facts.PlayerText);
        StringBuilder sb = new StringBuilder();
        if (facts.IsWitnessReaction)
        {
            return sb.Append("【城堡处置即时见证后处理】本轮是己方士兵对刚发生的“")
                .Append(SiegeCastleSoldierReactionProfile.DescribeConcernAction(facts.ReactionToAction))
                .Append("”作出的自由发言，不是直接回应玩家的新命令。只有发言正文明确表达反感、忧虑、同情、军纪疑虑、文化冲突或对统帅做法的不满时，才可输出城堡随军士兵不满标签；赞同、中立复述、普通询问或建议不得输出。该标签只登记待安抚军心，不得结算或提议任何新的战俘去向，也不得触发下一轮见证反应。")
                .ToString();
        }
        sb.Append("【城堡处置后处理事实】定居点=")
            .Append(string.IsNullOrWhiteSpace(facts.CastleName) ? SiegeCastleRuntimePromptProfile.DefaultCastleName : facts.CastleName.Trim())
            .Append("；本轮说话者身份=")
            .Append(SiegeCastleActionSpeakerRoleProfile.Describe(facts.SpeakerRole))
            .Append("；是否直接回应玩家本轮输入=")
            .Append(facts.ReplyIsDirectPlayerResponse ? "是" : "否")
            .Append("；仍待处置普通战俘=")
            .Append(facts.RemainingRegularPrisoners)
            .Append("；已收编普通战俘=")
            .Append(facts.RecruitedRegularPrisoners)
            .Append("；已屠戮普通战俘=")
            .Append(facts.SlaughteredRegularPrisoners)
            .Append("；己方军心待安抚=")
            .Append(facts.SoldierAppeasementRequired ? "是" : "否")
            .Append("；安抚已完成=")
            .Append(facts.SoldierAppeasementApplied ? "是" : "否")
            .Append("；本说话者待确认提议=")
            .Append(SiegeCastlePrisonerDispositionKindProfile.Describe(facts.PendingProposalForSpeaker))
            .Append("；本说话者俘虏信任=")
            .Append(facts.SpeakerTrust)
            .Append(facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord
                ? "；目标最终处置="
                : "；普通战俘当前暂定处置=")
            .Append(facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown ? "未指定" : facts.TerminalActionForTarget.ToString())
            .Append("；玩家本轮授权判定=")
            .Append(authorization.IsAuthorized
                ? SiegeCastlePrisonerDispositionKindProfile.Describe(authorization.Disposition)
                : "未授权（" + authorization.ReasonCode + "）")
            .Append("；玩家本轮安兵判定=")
            .Append(appeasementAuthorization.IsAuthorized
                ? "已明确安抚"
                : "未满足（" + appeasementAuthorization.ReasonCode + "）")
            .Append("。己方士兵或普通战俘主动提出释放、贩卖、收编、屠戮、劳役或教官方案时，只能输出与建议语义完全一致的提议标签；提议只记录待确认状态，绝不能直接结算。只有玩家本轮明确命令，或明确同意本说话者此前同类提议后，才可输出对应处置标签。每个普通战俘处置标签只登记玩家本轮指定的数量与兵种，多个分组可以累计；玩家未说明数量或兵种时由运行时随机选择。只有玩家明确说反悔、改判或全部重来时才清空幸存者旧计划。除现场屠戮的真实死亡外，不得声称战俘已经消失、转队、获释、售出或完成地方效果。己方士兵可以代玩家执行群体命令，但不能把劳役、释放、贩卖或教官命令改写成收编。自愿分支只能由普通战俘本人直接回应并达到信任门槛。安兵也必须有玩家本轮明确安抚意图，不能只凭士兵表示服从结算。闲聊、旁听、转述或领主回复不得触发普通战俘处置。一次回复最多输出一个城堡处置标签。");

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord)
        {
            SiegeCastleLordRecruitmentBranch branch = SiegeCastleLordRecruitmentBranchProfile.Resolve(
                facts.SpeakerIsClanLeader,
                facts.PlayerHasKingdom,
                facts.PlayerRulesKingdom,
                facts.PlayerText);
            sb.Append("【领主收编分支】是否族长=").Append(facts.SpeakerIsClanLeader ? "是" : "否")
                .Append("；玩家已有王国=").Append(facts.PlayerHasKingdom ? "是" : "否")
                .Append("；玩家为统治者=").Append(facts.PlayerRulesKingdom ? "是" : "否")
                .Append("；本轮可执行分支=").Append(SiegeCastleLordRecruitmentBranchProfile.Describe(branch)).Append("。");
            if (branch == SiegeCastleLordRecruitmentBranch.Unknown)
            {
                sb.Append("非族长的引见族长与成为玩家同伴尚未明确二选一，禁止输出领主收编标签。");
            }
            sb.Append("领主贩卖是当前被俘领主的独立单体最终处置：只有玩家本轮明确命令将该领主交给赎金经纪人时才可输出，价格与副作用必须走原版酒馆赎卖链，不能借用普通战俘群体贩卖标签。");
        }

        if (facts.SpeakerCultureMatchesCastle)
        {
            sb.Append("该己方士兵与城堡文化相同，可以表现出对同文化战败者更复杂的疑虑或同情，但仍须服从玩家；这不会绕过直接回应和军心待安抚门槛。");
        }

        return sb.ToString();
    }
}

public sealed class SiegeCastlePostprocessContextFacts
{
    public SiegeCastlePostprocessContextFacts(
        string castleName,
        SiegeCastleActionSpeakerRole speakerRole,
        bool replyIsDirectPlayerResponse,
        int remainingRegularPrisoners,
        int recruitedRegularPrisoners,
        int slaughteredRegularPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        bool speakerCultureMatchesCastle,
        string playerText = null,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker = SiegeCastlePrisonerDispositionKind.None,
        int speakerTrust = SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust,
        SiegeCastleActionKind terminalActionForTarget = SiegeCastleActionKind.Unknown,
        bool speakerIsClanLeader = false,
        bool playerHasKingdom = false,
        bool playerRulesKingdom = false,
        bool isWitnessReaction = false,
        SiegeCastleActionKind reactionToAction = SiegeCastleActionKind.Unknown)
    {
        CastleName = castleName ?? string.Empty;
        SpeakerRole = speakerRole;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        RemainingRegularPrisoners = ClampCount(remainingRegularPrisoners);
        RecruitedRegularPrisoners = ClampCount(recruitedRegularPrisoners);
        SlaughteredRegularPrisoners = ClampCount(slaughteredRegularPrisoners);
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        SpeakerCultureMatchesCastle = speakerCultureMatchesCastle;
        PlayerText = playerText ?? string.Empty;
        PendingProposalForSpeaker = pendingProposalForSpeaker;
        SpeakerTrust = SiegeCastlePrisonerTrustProfile.Clamp(speakerTrust);
        TerminalActionForTarget = terminalActionForTarget;
        SpeakerIsClanLeader = speakerIsClanLeader;
        PlayerHasKingdom = playerHasKingdom;
        PlayerRulesKingdom = playerRulesKingdom;
        IsWitnessReaction = isWitnessReaction;
        ReactionToAction = reactionToAction;
    }

    public static SiegeCastlePostprocessContextFacts Empty => new SiegeCastlePostprocessContextFacts(
        string.Empty,
        SiegeCastleActionSpeakerRole.Unknown,
        replyIsDirectPlayerResponse: false,
        remainingRegularPrisoners: 0,
        recruitedRegularPrisoners: 0,
        slaughteredRegularPrisoners: 0,
        soldierAppeasementRequired: false,
        soldierAppeasementApplied: false,
        speakerCultureMatchesCastle: false,
        playerText: string.Empty,
        pendingProposalForSpeaker: SiegeCastlePrisonerDispositionKind.None);

    public string CastleName { get; }

    public SiegeCastleActionSpeakerRole SpeakerRole { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public int RemainingRegularPrisoners { get; }

    public int RecruitedRegularPrisoners { get; }

    public int SlaughteredRegularPrisoners { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public bool SpeakerCultureMatchesCastle { get; }

    public string PlayerText { get; }

    public SiegeCastlePrisonerDispositionKind PendingProposalForSpeaker { get; }

    public int SpeakerTrust { get; }

    public SiegeCastleActionKind TerminalActionForTarget { get; }

    public bool SpeakerIsClanLeader { get; }

    public bool PlayerHasKingdom { get; }

    public bool PlayerRulesKingdom { get; }

    public bool IsWitnessReaction { get; }

    public SiegeCastleActionKind ReactionToAction { get; }

    private static int ClampCount(int value)
    {
        return value < 0 ? 0 : value;
    }
}
