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
            sb.Append("【己方士兵】你服从玩家的现场军令，可以表达疑虑、不满或担忧，但不能抗命、完全反驳玩家或自行处置俘虏。城堡阶段属于你的专用反应只应围绕玩家收编战俘后的军心不满与玩家安抚；没有明确发生收编时，不要凭空声称士气已经受损。");
        }
        else if (facts.IsPrisoner)
        {
            sb.Append(facts.IsLord
                ? "【被俘领主】你可以愤怒、不甘、傲慢、求饶或谈判，但必须承认自己已被控制。处决目前只保留接口；不要擅自宣布自己已获释、加入玩家或已经被处决。"
                : "【战俘士兵】你按守城战败、缴械并等待处置的普通守军理解。你可以恐惧、求生、屈服或谈条件，但不能把可指挥编队误认为已经收编，也不能自行宣布屠戮或收编已经执行。");
        }

        sb.Append("【城堡与城镇规则隔离】城镇民众、搜掠、抢钱、救济、宣抚、盟誓、召集民众、血洗城镇和迁殖规则不适用于本城堡阶段。不要输出或暗示任何城镇 GCCZ 处置标签。城堡专用的战俘收编、屠戮、士兵安抚和领主处置由独立接口处理，不能借用城镇标签代替。")
            .Append("【结算门槛】只有对应角色直接回应玩家本轮明确命令或谈判时，城堡专用接口才可进入结算候选；NPC闲聊、旁听、互相请示、主动提议或环境短句只能表达态度，不能直接结算高风险处置。正文自然说话，不要解释内部机制，也不要伪造已经发生的副作用。");

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
        string memoryContext)
    {
        CastleName = castleName ?? string.Empty;
        PlayerName = playerName ?? string.Empty;
        IsAlliedSoldier = isAlliedSoldier;
        IsPrisoner = isPrisoner;
        IsLord = isLord;
        RoleSituationContext = roleSituationContext ?? string.Empty;
        MemoryContext = memoryContext ?? string.Empty;
    }

    public static SiegeCastleRuntimePromptFacts Empty => new SiegeCastleRuntimePromptFacts(
        castleName: string.Empty,
        playerName: string.Empty,
        isAlliedSoldier: false,
        isPrisoner: false,
        isLord: false,
        roleSituationContext: string.Empty,
        memoryContext: string.Empty);

    public string CastleName { get; }

    public string PlayerName { get; }

    public bool IsAlliedSoldier { get; }

    public bool IsPrisoner { get; }

    public bool IsLord { get; }

    public string RoleSituationContext { get; }

    public string MemoryContext { get; }
}
