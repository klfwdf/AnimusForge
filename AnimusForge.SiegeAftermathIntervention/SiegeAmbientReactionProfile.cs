namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free prompt facts and throttling constants for short ambient speeches
/// from NPC units that are not directly talking to the player. AF adapters still
/// choose live agents and call the scene speech bridge.
/// </summary>
public static class SiegeAmbientReactionProfile
{
    public const float WindowSeconds = 30.0f;

    public const int MaxSpeakersPerAudience = 3;

    public const float RequestSpacingSeconds = WindowSeconds / MaxSpeakersPerAudience;

    public const int RangeShoutAutoFollowupSpeakers = 3;

    public const float RangeShoutAutoReplySpacingSeconds = 9.0f;

    public const int LightweightRecentHistoryLineLimit = 2;

    public const int LightweightOutputMaxTokens = 96;

    public const string DefaultSettlementName = "这座刚被攻下的定居点";

    public const string DefaultFocusName = "附近的人";

    public static bool IsAmbientReactionFact(string factText)
    {
        return !string.IsNullOrWhiteSpace(factText)
            && factText.Contains("【攻城处置环境发言】");
    }

    public static string BuildLightweightSystemInstruction()
    {
        return "你正在攻城后处置现场发出一句很短的即时反应。"
            + "必须承认玩家是刚攻下此地的现场统帅和处置者。"
            + "只输出你嘴里说出的一句话；不要写旁白、动作、内心、规则解释或方括号标签。";
    }

    public static string BuildLightweightReactionPrompt(string factText, string recentHistory, string compactRuntimeContext)
    {
        string fact = string.IsNullOrWhiteSpace(factText) ? "【攻城处置环境发言】处置局势正在变化。" : factText.Trim();
        string history = string.IsNullOrWhiteSpace(recentHistory) ? "最近现场对话：无。" : "最近现场对话：\n" + recentHistory.Trim();
        string runtime = string.IsNullOrWhiteSpace(compactRuntimeContext) ? "" : "\n\n你的可见处境：\n" + compactRuntimeContext.Trim();

        return fact
            + "\n\n" + history
            + runtime
            + "\n\n请根据你的身份、文化、性格和当前处置动作，回一句12到36字的现场话。不要输出任何标签。";
    }

    public static string BuildFact(
        SiegeInterventionActionKind action,
        bool alliedSoldier,
        bool speakerCultureMatchesSettlement,
        string settlementName,
        string focusName)
    {
        string audience = alliedSoldier ? "玩家己方入城士兵" : "战败平民/商人/工匠/头人或要人";
        string sceneName = string.IsNullOrWhiteSpace(settlementName) ? DefaultSettlementName : settlementName.Trim();
        string focus = string.IsNullOrWhiteSpace(focusName) ? DefaultFocusName : focusName.Trim();
        string sameCultureLine = speakerCultureMatchesSettlement
            ? "你和本地民众/此地文化相近，说话要更压抑、短促、羞耻或不忍；但这不改变现场服从、恐惧或求生逻辑。"
            : "按你的身份、文化、兵种和处境自然说话。";

        return "【攻城处置环境发言】当前地点是" + sceneName + "。你不是当前与玩家直接对话的人；你是" + audience + "，刚看到/听到玩家的处置命令已经开始执行：" + DescribeAction(action, alliedSoldier) + "。"
            + "请立刻说一句很短的现场话，只输出你嘴里说出的话，不要写旁白、动作描写或方括号标签。"
            + "可回应或喊给" + focus + "，语气要像战后街巷里的即时反应。"
            + sameCultureLine;
    }

    private static string DescribeAction(SiegeInterventionActionKind action, bool alliedSoldier)
    {
        switch (action)
        {
            case SiegeInterventionActionKind.GatherCivilians:
                return alliedSoldier
                    ? "召集民众；你可以催促、传令或让人都到大人身边听训。"
                    : "召集民众；你可以答应马上过去、喊邻近的人跟上，或慌张地转告大家去听命。";
            case SiegeInterventionActionKind.CivilianRobbery:
                return alliedSoldier
                    ? "局部索取财物；你只能维持胜利方压力或向玩家请示是否扩大，不能宣布全城搜掠。"
                    : "玩家正在向民众索取钱物；你可以求饶、交钱、讨价还价、护住家人或提醒别人把财物拿出来换安全。";
            case SiegeInterventionActionKind.Plunder:
                return alliedSoldier
                    ? "全城搜掠；你可以粗鲁催交财物、叫同伴搜查，但不得自行升级成血洗。"
                    : "全城搜掠；你可以惊慌求饶、交出钱物、喊家人躲好或请求别伤人。";
            case SiegeInterventionActionKind.Massacre:
                return alliedSoldier
                    ? "血洗已经开始；你可以喊杀、威吓或执行命令；若同文化，避免轻浮辱骂，用压低的冷硬服从表达。"
                    : "血洗已经开始；你可以求饶、呼喊逃命、护住老人孩子，少数有胆者也可绝望反抗。";
            case SiegeInterventionActionKind.CulturalRepopulation:
                return alliedSoldier
                    ? "屠民迁殖已经被要求；你知道这是最高级处置，要清除原住民并迁入己方人口，说话不能轻飘。"
                    : "屠民迁殖已经被要求；你意识到这不只是抢掠而是要清除原住民，可以绝望求饶、逃散或喊家人快走。";
            case SiegeInterventionActionKind.Mercy:
                return alliedSoldier
                    ? "玩家选择宽恕民众；你必须服从，可低声提醒收兵、看住街口或压下战利落空的不满。"
                    : "玩家选择宽恕民众；你可以惊疑、感谢、试探地请求保证老人孩子安全。";
            case SiegeInterventionActionKind.Relief:
                return alliedSoldier
                    ? "玩家选择救济或分发物资；你可以传达军纪、维持队列或让民众别乱抢。"
                    : "玩家选择救济/保护/安顿；你可以感谢、松一口气、请求粮食或让旁人别乱跑。";
            case SiegeInterventionActionKind.Inspire:
                return alliedSoldier
                    ? "玩家正在宣抚安民；你可以让民众安静听令，表现出执行军纪。"
                    : "玩家正在宣抚安民；你可以试探回应、请求兑现承诺，或让旁人先听大人说完。";
            case SiegeInterventionActionKind.RallyOath:
                return alliedSoldier
                    ? "玩家正在组织归心盟誓；你可以催促民众列队听誓，维持胜利方秩序。"
                    : "玩家正在要求归心盟誓；你可以惶恐应承、观望或劝身边人别顶撞。";
            case SiegeInterventionActionKind.AppeaseSoldiers:
                return alliedSoldier
                    ? "玩家正在安抚士兵军心；你可以接受补偿/军纪解释，仍服从统帅。"
                    : "玩家正在安抚士兵军心；你可以害怕地旁听，盼望士兵别再扩大伤害。";
            default:
                return "处置局势正在变化；按你的身份做出短促现场反应。";
        }
    }
}
