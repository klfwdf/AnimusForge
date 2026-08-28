using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCastlePostprocessContextProfile
{
    public static string Build(SiegeCastlePostprocessContextFacts facts)
    {
        facts ??= SiegeCastlePostprocessContextFacts.Empty;
        bool hasCompoundAuthorization = SiegeCastleCompoundDispositionPlanProfile.TryBuild(
            facts.PlayerText,
            out SiegeCastleCompoundDispositionPlan compoundPlan);
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
            .Append("；玩家本轮原话=《")
            .Append((facts.PlayerText ?? string.Empty).Trim())
            .Append("》；明确复合分组=")
            .Append(hasCompoundAuthorization ? DescribeCompoundPlan(compoundPlan) : "未识别")
            .Append("。当前传入的每个城堡标签都只是该角色与实时状态下合法的候选能力，不代表代码已判定触发。AF动作后处理AI是本轮语义判定者：必须综合玩家完整原话、NPC最新直接回复、既有提议和场景记忆决定输出哪个标签或不输出城堡标签；不得依靠固定关键词，不得因为候选标签存在就输出。提问、假设、转述、否定、拒绝、反悔、闲聊、旁听、尚未答应或语义不明确时不得输出结算标签。己方士兵或普通战俘主动提出释放、贩卖、收编、屠戮、外派村庄劳役、修缮本城堡或教官方案时，只能输出与建议语义完全一致的提议标签；提议只记录待确认状态，绝不能直接结算。只有AI从完整对话判断玩家本轮已明确下令，或已明确同意本说话者此前同类提议时，才输出对应处置标签。每个普通战俘处置标签只登记玩家本轮指定的数量与兵种，多个分组可以累计；玩家未说明数量或兵种时由运行时随机选择。只有玩家明确反悔、要求全部重来或推翻之前安排时才清空幸存者旧计划；单独说‘改判为某处置’只登记当前新分组，不得解释为清空其他分组。外派村庄当农奴、修路或耕种使用普通劳役标签；留在当前城堡修城墙、城防或项目使用修缮城堡专用标签，裸称‘劳役’且没有外派目标时按修缮当前城堡理解。除现场屠戮的真实死亡外，不得声称战俘已经消失、转队、获释、售出或完成地方效果。己方士兵可以代玩家执行群体命令，但不能把劳役、释放、贩卖、修缮或教官命令改写成收编。自愿分支只能由普通战俘本人直接回应并达到信任门槛。安兵标签也由AI根据玩家原话与士兵是否明确接受安抚来判断，不能只凭士兵服从结算。一次回复默认最多输出一个城堡处置标签；仅当玩家同一句用明确人数并以‘其余/剩下’划分多个互不重叠的普通战俘去向时，按分组分别输出多个对应标签。");

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord)
        {
            SiegeCastleLordRecruitmentBranch playerTextBranch = SiegeCastleLordRecruitmentBranchProfile.Resolve(
                facts.SpeakerIsClanLeader,
                facts.PlayerHasKingdom,
                facts.PlayerRulesKingdom,
                facts.PlayerText);
            sb.Append("【领主收编分支】是否族长=").Append(facts.SpeakerIsClanLeader ? "是" : "否")
                .Append("；玩家已有王国=").Append(facts.PlayerHasKingdom ? "是" : "否")
                .Append("；玩家为统治者=").Append(facts.PlayerRulesKingdom ? "是" : "否")
                .Append("；玩家原话中已识别分支=").Append(SiegeCastleLordRecruitmentBranchProfile.Describe(playerTextBranch)).Append("。");
            if (!facts.SpeakerIsClanLeader)
            {
                sb.Append("非族长的引见族长与成为玩家同伴必须明确二选一；AI必须把玩家原话与领主最新回复合并理解，只有完整对话已经明确其中一个分支时才输出领主收编标签，尚未明确时不得输出。");
            }
            sb.Append("领主贩卖是当前被俘领主的独立单体最终处置：只有玩家本轮明确命令将该领主交给赎金经纪人时才可输出，价格与副作用必须走原版酒馆赎卖链，不能借用普通战俘群体贩卖标签。");
            sb.Append("领主处决同样由AI按完整语义判断，不要求固定措辞：玩家明确表示要杀死、斩杀或处决当前领主且回复正面承接时输出；问询、假设、反悔、单纯辱骂或泛泛威胁不得输出。标签只打开原版处刑确认，不能绕过玩家最终确认。");
            sb.Append("【领主决斗事实】")
                .Append(SiegeCastleLordDuelProfile.BuildEquipmentFact(
                    facts.PlayerIsMounted,
                    facts.PlayerCarriesRangedWeapon,
                    facts.PlayerWieldsRangedWeapon))
                .Append("领主可因玩家骑马、携弓或认为规则不公而拒绝，或要求玩家先下马、收弓；有条件答复仍不是同意。AI后处理必须按完整回复语义判断而不是依赖固定关键词：该领主本轮以任何自然说法明确、无条件表示现在应战时都要输出领主决斗标签。若这是紧接上一轮决斗谈判的续接，玩家本轮确认守信或履行条件后领主明确应战，也应输出，玩家无需再次说“决斗”。玩家单方面说要决斗、领主拒绝或条件尚未满足绝不能触发。");
        }

        if (facts.SpeakerCultureMatchesCastle)
        {
            sb.Append("该己方士兵与城堡文化相同，可以表现出对同文化战败者更复杂的疑虑或同情，但仍须服从玩家；这不会绕过直接回应和军心待安抚门槛。");
        }

        return sb.ToString();
    }

    private static string DescribeCompoundPlan(SiegeCastleCompoundDispositionPlan plan)
    {
        StringBuilder sb = new StringBuilder("明确分组（");
        for (int i = 0; i < plan.Steps.Count; i++)
        {
            if (i > 0)
            {
                sb.Append("、");
            }
            sb.Append(SiegeCastlePrisonerDispositionKindProfile.Describe(plan.Steps[i].Disposition));
        }
        return sb.Append("）").ToString();
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
        SiegeCastleActionKind reactionToAction = SiegeCastleActionKind.Unknown,
        bool playerIsMounted = false,
        bool playerCarriesRangedWeapon = false,
        bool playerWieldsRangedWeapon = false)
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
        PlayerIsMounted = playerIsMounted;
        PlayerCarriesRangedWeapon = playerCarriesRangedWeapon;
        PlayerWieldsRangedWeapon = playerWieldsRangedWeapon;
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

    public bool PlayerIsMounted { get; }

    public bool PlayerCarriesRangedWeapon { get; }

    public bool PlayerWieldsRangedWeapon { get; }

    private static int ClampCount(int value)
    {
        return value < 0 ? 0 : value;
    }
}
