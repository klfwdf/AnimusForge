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
            .Append("；目标最终处置=")
            .Append(facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown ? "未结算" : facts.TerminalActionForTarget.ToString())
            .Append("；玩家本轮授权判定=")
            .Append(authorization.IsAuthorized
                ? SiegeCastlePrisonerDispositionKindProfile.Describe(authorization.Disposition)
                : "未授权（" + authorization.ReasonCode + "）")
            .Append("；玩家本轮安兵判定=")
            .Append(appeasementAuthorization.IsAuthorized
                ? "已明确安抚"
                : "未满足（" + appeasementAuthorization.ReasonCode + "）")
            .Append("。己方士兵或普通战俘主动提出释放、贩卖、收编、屠戮、劳役或教官方案时，只能输出与建议语义完全一致的提议标签；提议只记录待确认状态，绝不能直接结算。只有玩家本轮明确命令，或明确同意本说话者此前同类提议后，才可输出对应结算标签。己方士兵可以代玩家执行群体命令，但不能把劳役、释放、贩卖或教官命令改写成收编。自愿分支只能由普通战俘本人直接回应并达到信任门槛。安兵也必须有玩家本轮明确安抚意图，不能只凭士兵表示服从结算。闲聊、旁听、转述或领主回复不得触发普通战俘处置。一次回复最多输出一个城堡处置标签。");

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
        bool playerRulesKingdom = false)
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

    private static int ClampCount(int value)
    {
        return value < 0 ? 0 : value;
    }
}
