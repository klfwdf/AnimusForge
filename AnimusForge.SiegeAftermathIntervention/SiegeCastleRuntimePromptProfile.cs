using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime wording and action isolation for an active castle aftermath stage.
/// The AF adapter supplies live agent roles, prisoner state, and lord battle provenance.
/// </summary>
public static class SiegeCastleRuntimePromptProfile
{
    public const string DefaultCastleName = "这座刚被攻下的城堡";

    public const string DefaultPlayerName = "玩家";

    public static string Build(SiegeCastleRuntimePromptFacts facts)
    {
        facts ??= SiegeCastleRuntimePromptFacts.Empty;

        string castleName = Normalize(facts.CastleName, DefaultCastleName);
        string playerName = Normalize(facts.PlayerName, DefaultPlayerName);
        StringBuilder sb = new StringBuilder();
        sb.Append("【城堡攻占后亲自处置·最高优先级】")
            .Append(castleName)
            .Append("刚被")
            .Append(playerName)
            .Append("一方攻下。当前只是调用原版围城战争场景作为处置现场，不是在继续进行围城战；城墙破坏状态来自刚结束的围城并应保持可见。")
            .Append("场景内没有重新刷新的原版守卫或城镇民众，只有玩家、玩家挑选带入的己方士兵、战败守军俘虏和被俘领主。玩家可用原版指挥系统调整编队站位，但可被指挥只代表现场押解与站位，不会自动改变俘虏阵营、身份或忠诚。")
            .Append("所有角色都应理解攻城已经结束、守军已经失败、玩家掌握现场控制权；不要把这里说成和平城镇、藏匿点、阅兵、仍在交战的围城任务或原版守卫执法现场。");

        AppendIfNotEmpty(sb, facts.RoleSituationContext);
        AppendIfNotEmpty(sb, facts.MemoryContext);

        if (facts.IsAlliedSoldier)
        {
            sb.Append("【己方士兵】你服从玩家的现场军令，可以表达疑虑、不满或担忧，但不能抗命、完全反驳玩家或自行处置俘虏。你可以建议收编或屠戮普通战俘，但建议本身绝不代表执行，必须等待玩家明确同意；在玩家授权前不得声称名册已经改变或战俘已经被杀。己方士兵只有一个正式结算标签：安抚随军士兵。是否产生不满由玩家的具体处置、双方文化、强制程度、善待或虐待情况综合判断；同文化屠戮、强制收编等可能触发，累计离场士气惩罚最多仍为-30，不叠加多个标签。");
            if (facts.PendingProposalForSpeaker != SiegeCastlePrisonerDispositionKind.None)
            {
                sb.Append(SiegeCastleSoldierProposalProfile.BuildPendingContext(facts.PendingProposalForSpeaker));
            }
            if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied)
            {
                sb.Append("当前玩家对普通战俘的处置已经引发军心不满，确实处于待安抚状态；其中已收编普通战俘=")
                    .Append(facts.RecruitedRegularPrisoners)
                    .Append("，当前普通战俘最终处置=")
                    .Append(facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown ? "尚未结算" : facts.TerminalActionForTarget.ToString())
                    .Append("。你可以表达不满、疑虑并要求解释，但最终仍须服从。只有玩家本轮实际给出安抚、补偿、军纪解释或战利安排，并由你直接回应且明确接受时，后处理才可结算城堡安兵；单纯要求继续站岗或服从不算安抚。");
            }
            else if (facts.SoldierAppeasementApplied)
            {
                sb.Append("本次战俘处置引发的军心不满已经被玩家在现场安抚，不要再次声称仍待安抚或重复结算。");
            }
            else
            {
                sb.Append("当前没有待处理的城堡战俘处置军心事件，不得凭空触发安兵结算。");
            }
            if (facts.SpeakerCultureMatchesCastle)
            {
                sb.Append("你与这座城堡文化相同，对同文化战败者可以有更复杂的同情、顾虑或身份矛盾，但不能抗命，也不能绕过直接回应门槛。");
            }
        }
        else if (facts.IsPrisoner)
        {
            sb.Append(facts.IsLord
                ? "【被俘领主】你可以愤怒、不甘、傲慢、求饶或谈判，但必须承认自己已被控制。善待与接收军械只针对你本人；收编与处决也只针对你本人，不能代表普通战俘。非族长面对收编谈判时要区分写信引见族长与背叛家族成为同伴；不要擅自宣布已经加入玩家或已经被处决。"
                : "【战俘士兵】你按守城战败、缴械并等待处置的普通守军理解。你可以恐惧、求生、屈服、请求被收编或谈条件，但请求与提议本身绝不代表玩家同意；不能把可指挥编队误认为已经收编。普通士兵标签按本次带入群体结算：善待和接收军械是可重复对话但单次结算的流程，释放、贩卖、屠戮、自愿/强制收编、30天劳役服刑、自愿/强制担任教官属于互斥最终处置。自愿结算必须由你明确心甘情愿并满足信任门槛；否则只能是强制分支。求饶闲聊、旁听和未获玩家同意的主动提议不能结算。");
            if (facts.IsLord)
            {
                sb.Append("【当前领主政治事实】个人信任=").Append(facts.SpeakerTrust)
                    .Append("；是否族长=").Append(facts.SpeakerIsClanLeader ? "是" : "否")
                    .Append("；玩家是否已有王国=").Append(facts.PlayerHasKingdom ? "是" : "否")
                    .Append("；玩家是否为该王国统治者=").Append(facts.PlayerRulesKingdom ? "是" : "否").Append("。");
                if (facts.SpeakerIsClanLeader)
                {
                    sb.Append(facts.PlayerHasKingdom
                        ? (facts.PlayerRulesKingdom
                            ? "若玩家明确招揽，你可以代表家族归附玩家统治的王国；不得把全族归附说成加入玩家家族当同伴。"
                            : "若玩家明确招揽，你只能请求玩家带你面见其统治者；玩家本人无权在这里直接让全族完成归附。")
                        : "玩家尚未加入任何王国；若玩家明确招揽，你可以表达支持或拥立玩家为王的政治意向，但不得凭空创建王国或立即转移家族归属。");
                }
                else
                {
                    sb.Append("你不是族长，不能代表全族归附。玩家必须明确选择：一是由你写信并在1至2天后向族长引见玩家；二是你背叛原家族、成为玩家同伴。未明确二选一时不得输出领主收编标签。");
                }
            }
            else
            {
                sb.Append("【当前俘虏信任】数值=").Append(facts.SpeakerTrust)
                    .Append("；自愿收编门槛=").Append(SiegeCastlePrisonerTrustProfile.VoluntaryRecruitThreshold)
                    .Append("，自愿劳役门槛=").Append(SiegeCastlePrisonerTrustProfile.VoluntaryLaborThreshold)
                    .Append("，自愿教官门槛=").Append(SiegeCastlePrisonerTrustProfile.VoluntaryInstructorThreshold)
                    .Append("。善待会提高信任，接收军械会降低信任；不要把信任不足写成心甘情愿。");
            }
            if (facts.TreatedForTarget)
            {
                sb.Append("该目标本场已经结算善待，不得重复触发。");
            }
            if (facts.ArmamentsReceivedForTarget)
            {
                sb.Append("该目标本场已经结算接收军械，不得重复触发。");
            }
            if (facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
            {
                sb.Append("该目标已经进入最终处置：").Append(facts.TerminalActionForTarget)
                    .Append("；不得再触发其他流程或互斥最终标签。");
            }
            if (facts.PendingProposalForSpeaker != SiegeCastlePrisonerDispositionKind.None)
            {
                sb.Append(SiegeCastleSoldierProposalProfile.BuildPendingContext(facts.PendingProposalForSpeaker));
            }
        }

        sb.Append("【城堡与城镇规则隔离】城镇民众、搜掠、抢钱、救济、宣抚、盟誓、召集民众、血洗城镇和迁殖规则不适用于本城堡阶段。不要输出或暗示任何城镇 GCCZ 处置标签。城堡专用的战俘收编、屠戮、士兵安抚和领主处置由独立接口处理，不能借用城镇标签代替。")
            .Append("【结算门槛】只有对应角色直接回应玩家本轮明确命令或明确同意时，城堡专用接口才可进入结算候选；运行时只向后处理器提供与当前角色、玩家本轮意图和未结状态匹配的标签。NPC主动提出收编/屠戮只能登记待确认提议，NPC闲聊、旁听、互相请示或环境短句只能表达态度，不能直接结算高风险处置。一次回复最多结算一个城堡动作。正文自然说话，不要直接打印动作标签、解释内部机制或伪造已经发生的副作用；动作标签只由独立后处理器生成。");

        return sb.ToString();
    }

    public static string BuildImmediateReactionIdentityOverride(
        string castleName,
        string playerName,
        bool isAlliedSoldier,
        bool isPrisoner,
        bool isLord)
    {
        string role = isAlliedSoldier
            ? "你是玩家挑选带入城堡的己方士兵，玩家是你的直接统帅。"
            : (isPrisoner
                ? (isLord
                    ? "你是被带入刚陷落城堡等待处置的敌方贵族俘虏。"
                    : "你是守城战败、缴械并等待处置的普通守军俘虏。")
                : "你在刚陷落城堡的处置现场，必须承认玩家控制现场。");

        return "【城堡处置即时身份覆写】" + Normalize(castleName, DefaultCastleName) + "已经被"
            + Normalize(playerName, DefaultPlayerName) + "一方攻下；这里是战后处置现场，不是和平城镇、阅兵或仍在进行的围城战。"
            + role + "原版指挥编队只用于站位和押解，不改变俘虏身份。";
    }

    public static bool ShouldExposeTownAftermathRules(bool isCastleStage)
    {
        return !isCastleStage;
    }

    private static void AppendIfNotEmpty(StringBuilder sb, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            sb.Append(text.Trim());
        }
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

public sealed class SiegeCastleRuntimePromptFacts
{
    public SiegeCastleRuntimePromptFacts(
        string castleName,
        string playerName,
        bool isAlliedSoldier,
        bool isPrisoner,
        bool isLord,
        string roleSituationContext,
        string memoryContext,
        int remainingRegularPrisoners = 0,
        int recruitedRegularPrisoners = 0,
        int slaughteredRegularPrisoners = 0,
        bool soldierAppeasementRequired = false,
        bool soldierAppeasementApplied = false,
        bool speakerCultureMatchesCastle = false,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker = SiegeCastlePrisonerDispositionKind.None,
        int speakerTrust = SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust,
        bool treatedForTarget = false,
        bool armamentsReceivedForTarget = false,
        SiegeCastleActionKind terminalActionForTarget = SiegeCastleActionKind.Unknown,
        bool speakerIsClanLeader = false,
        bool playerHasKingdom = false,
        bool playerRulesKingdom = false)
    {
        CastleName = castleName ?? string.Empty;
        PlayerName = playerName ?? string.Empty;
        IsAlliedSoldier = isAlliedSoldier;
        IsPrisoner = isPrisoner;
        IsLord = isLord;
        RoleSituationContext = roleSituationContext ?? string.Empty;
        MemoryContext = memoryContext ?? string.Empty;
        RemainingRegularPrisoners = remainingRegularPrisoners < 0 ? 0 : remainingRegularPrisoners;
        RecruitedRegularPrisoners = recruitedRegularPrisoners < 0 ? 0 : recruitedRegularPrisoners;
        SlaughteredRegularPrisoners = slaughteredRegularPrisoners < 0 ? 0 : slaughteredRegularPrisoners;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        SpeakerCultureMatchesCastle = speakerCultureMatchesCastle;
        PendingProposalForSpeaker = pendingProposalForSpeaker;
        SpeakerTrust = SiegeCastlePrisonerTrustProfile.Clamp(speakerTrust);
        TreatedForTarget = treatedForTarget;
        ArmamentsReceivedForTarget = armamentsReceivedForTarget;
        TerminalActionForTarget = terminalActionForTarget;
        SpeakerIsClanLeader = speakerIsClanLeader;
        PlayerHasKingdom = playerHasKingdom;
        PlayerRulesKingdom = playerRulesKingdom;
    }

    public static SiegeCastleRuntimePromptFacts Empty => new SiegeCastleRuntimePromptFacts(
        castleName: string.Empty,
        playerName: string.Empty,
        isAlliedSoldier: false,
        isPrisoner: false,
        isLord: false,
        roleSituationContext: string.Empty,
        memoryContext: string.Empty,
        remainingRegularPrisoners: 0,
        recruitedRegularPrisoners: 0,
        slaughteredRegularPrisoners: 0,
        soldierAppeasementRequired: false,
        soldierAppeasementApplied: false,
        speakerCultureMatchesCastle: false,
        pendingProposalForSpeaker: SiegeCastlePrisonerDispositionKind.None);

    public string CastleName { get; }

    public string PlayerName { get; }

    public bool IsAlliedSoldier { get; }

    public bool IsPrisoner { get; }

    public bool IsLord { get; }

    public string RoleSituationContext { get; }

    public string MemoryContext { get; }

    public int RemainingRegularPrisoners { get; }

    public int RecruitedRegularPrisoners { get; }

    public int SlaughteredRegularPrisoners { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public bool SpeakerCultureMatchesCastle { get; }

    public SiegeCastlePrisonerDispositionKind PendingProposalForSpeaker { get; }

    public int SpeakerTrust { get; }

    public bool TreatedForTarget { get; }

    public bool ArmamentsReceivedForTarget { get; }

    public SiegeCastleActionKind TerminalActionForTarget { get; }

    public bool SpeakerIsClanLeader { get; }

    public bool PlayerHasKingdom { get; }

    public bool PlayerRulesKingdom { get; }
}
