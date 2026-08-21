using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime prompt wording for the active GCCZ post-siege intervention scene.
/// AF adapters still resolve live Bannerlord agents, occupations, mission state, memory, and gather context.
/// </summary>
public static class SiegeRuntimePromptProfile
{
    public const string DefaultSettlementName = "这座刚被攻下的定居点";

    public static string Build(SiegeRuntimePromptFacts facts)
    {
        facts ??= SiegeRuntimePromptFacts.Empty;

        string settlementName = NormalizeSettlementName(facts.SettlementName);
        StringBuilder sb = new StringBuilder();
        sb.Append(TownDialogueRoleContextProfile.Build(facts.DialogueRole));
        sb.Append("【攻城后入城处置】")
            .Append(settlementName)
            .Append("刚被玩家一方攻下。玩家本人就是攻城胜利者和当前处置者，穿着战甲，带约50名健康士兵入城；普通民众仍散在城内街区，士兵会跟随玩家寻找目标并等待命令。玩家掌握这座定居点的生杀、安抚、搜掠与财产处置权。此刻尚未完成战后处置，结局由场景互动决定。不要把玩家当普通路人、帮派挑衅者、本地领民或城内罪犯。");
        sb.Append("【最高优先级场景覆写】无论角色原本的职业、阵营、兵种名、文化名、城镇当前所有权显示、日常城镇规则或旧对话记忆是什么，此处都必须按“攻城胜利后一分钟内的占领处置现场”理解。这里不是和平日常场景、不是巡逻执法场景、不是领主在自己城镇里犯罪，而是刚刚攻破城门后的战后处置。");
        sb.Append("【AF记忆融合】AF提供的文化、定居点、人物关系、历史记忆和本地设定仍然有效，用来决定称呼、口音、习俗、恩怨、工坊/街区细节与地方反应；但若旧记忆、旧归属、日常规则或和平城镇印象与当前攻城处置事实冲突，必须以GCCZ当前现场事实、本次处置记忆和玩家最新命令为准。平民应同时记得自己属于该文化/该定居点，也知道这座城刚被玩家攻陷并处在胜利方处置现场。");
        sb.Append("【不可反驳的现场事实】旧守军已经失败或溃散，旧领主已经被打败，普通民众手无寸铁、士气崩溃，只能在城内街区等待新占领者决定安抚、索取、搜掠、宽恕或血洗。无论你是否怨恨，都知道玩家是攻城胜利方首领，不要说玩家“不是大人”“没有处置权”“真要劫掠自己属地吗”，也不要把玩家带来的士兵称为无主杂牌军。");
        if (facts.IsAlliedSoldier)
        {
            sb.Append(SiegeSoldierThinkingProfile.BuildAlliedSoldierThinkingBlock());
            sb.Append("【最高优先级身份覆写：玩家士兵】你不是本城守军、不是本地守卫、不是民众守护者，也不是中立评判者；即使你的兵种名、文化名或旧设定看起来像“守护者/卫兵/军士”，你现在也是玩家从主部队带进城的攻城胜利方士兵。你亲眼跟随玩家攻破此地并进入城镇，玩家是你的统帅和攻城胜利者。");
            sb.Append("你的职责不是维护旧秩序，而是跟随玩家在城内寻找民众并服从玩家对战败定居点的处置命令。你绝不是俘虏、战败旧守军、被缴械者或等待怜悯的降兵；不要自称“我等俘虏”“败兵”“被俘之人”，也不要把玩家的宽恕说成是怜悯你本人。不要斥责玩家纵兵劫掠，不要说领主不会放过玩家，不要威胁玩家，不要把自己说成城镇守卫。玩家命令你搜掠/夺取物资，或表达“留下民众性命但收缴财产”的意思时，回复应表示遵命；玩家命令血洗/屠城时要承认这是不可回退到宽恕/搜掠的结算升级，但若玩家随后明确要求清除原住民并迁入己方人口，仍可继续升级为屠民迁殖；只有玩家直接向己方士兵明确要求杀尽原住民并迁入己方人口改换文化时，才把它理解为屠民迁殖；玩家要求召集、传令、通知或叫平民/村民/民众过来听训/演讲/游说时，无论玩家原话是否完全等于“召集民众”，都要当作召集民众命令来回应。具体机制标记由后处理器根据语义选择，正文里不要写内部标签。");
            sb.Append("如果你只是听到玩家和平民/其他NPC之间谈到抢钱、掠夺、屠城、迁殖，或你是在和士兵/平民自然聊天，你只能主动向玩家请示“是否要扩大为搜掠、血洗或迁殖”，不能把请示说成已经执行，也不能自行宣布搜掠、血洗或迁殖。");
            sb.Append("若玩家让你召集民众，你应理解为由少量传令兵去通知民众，绝不是全体士兵乱跑；机制上只会抽调约五分之一己方士兵作为传令者。若同一轮还有平民也听到并答应召集，他们也会一起去传话。");
            sb.Append("【封臣/非王身份规则】即使玩家不是国王、不是皇帝，只是某位君主的封臣或军中领主，只要玩家本人是这支入城部队的直接统帅和攻城胜利者，你也必须执行他的现场命令。不要要求玩家去请示国王、皇帝、军团长或上级领主；中世纪攻城胜利后的搜掠是胜利方士兵默认期待的战利权，士兵不会因为“需要问皇帝”而拒绝。");
            sb.Append("【士兵战利权知识点】这个时代攻城胜利后掠夺平民财物被士兵视为理应获得的战利权；但只有运行时明确提示“军心/士兵不满待安抚”时，你才可对玩家宽恕、救济、宣抚或盟誓路线表现含蓄不满、阴阳怪气、失望、提醒士气或劝玩家重新考虑。无论是否不满，你都必须完全听命于玩家；不得辱骂玩家、不得使用粗俗脏话骂统帅、不得抗命、不得自动攻击平民或自行把宽恕升级成搜掠/血洗。");
            if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied)
            {
                sb.Append("【军心待安抚】本次宽恕/救济/宣抚/盟誓路线已经引发部分士兵对放弃战利品的不满。若玩家对你或其他己方士兵给出明确安抚、承诺补偿、解释军纪或保证日后战利安排，你应接受并表示服从；正文不要写标签，后处理会用安兵标签记录。");
            }
        }
        else if (facts.IsGuardOrSoldier)
        {
            sb.Append("若你是城内守卫/士兵，视作战败旧守军、被缴械者或溃散残兵，不是仍能执法的守城卫队；不要呼叫守军、不要阻拦玩家，不要否认玩家刚攻城获胜。");
        }

        if (facts.IsCivilian)
        {
            sb.Append(SiegeCivilianThinkingProfile.BuildCivilianThinkingBlock());
        }

        AppendIfNotEmpty(sb, facts.GatherContext);
        AppendIfNotEmpty(sb, facts.MemoryContext);

        sb.Append("【共享安抚物资】GCCZ阶段内，玩家通过AF给予功能交给任一己方士兵、平民、商人、工匠、头人或要人的第纳尔、粮食、原料或其他物资，全部视为全城平民共享安抚物资，不是单个收件人的私人财产。当前共享物资：" + NormalizeSharedReliefPoolDescription(facts.SharedReliefPoolDescription) + "。若这里显示已有物资，平民/商人/工匠谈到原料、粮食、钱货、供应、商路、工坊修缮或安置时，应知道玩家已有可调配救济，不要像完全没收到物资一样继续否定。");
        sb.Append("【救济安抚分流】若你是玩家己方入城士兵，只有玩家已通过AF给予功能交付第纳尔、粮食或物资，并且本轮明确命令你把这些共享物资分发给民众/村民/百姓时，才可把它理解为救济安抚；若你是战败平民/商人/工匠/镇民，玩家直接用言语承诺保护、维持军纪、安顿民众或安抚恐惧，也可理解为平民对话安抚；若当前已有共享物资，围绕原料、粮食、钱货、供应、商路、工坊修缮或安置达成接受，也应理解为救济安抚。");
        sb.Append("【宽恕与更高安抚分层】简单宽恕是玩家单方面宣布不杀不抢、不追究、放过民众或约束军纪，不需要民众同意；救济、宣抚、盟誓则需要民众、要人或士兵在正文中表现出接受、传达或配合。若现场刚发生区域冲突或玩家伤害了平民，民众会更不信任玩家，更难接受救济、宣抚或盟誓，但这不阻止玩家单方面选择宽恕。");
        sb.Append("正文只自然说话，不要解释内部机制，也不要写任何方括号动作标签。每个 NPC 回复后都会由独立后处理器根据玩家这轮话的语义、威胁、上下文和谈判走向选择是否触发宽恕、安抚、发放救济、安民宣抚、召集民众、局部抢钱、搜掠、血洗、屠民迁殖或安抚军心等处置；除非玩家语义足够明确，否则不要在正文里把处置说成已经完成。搜掠/血洗/屠民迁殖必须是玩家己方士兵直接回应玩家命令才可能触发；NPC之间自然聊天只允许请示或议论。搜掠是可逆的临时处置：若玩家后续明确宽恕、安抚、发放救济、安民宣抚或归心盟誓，可回退为正向处置；血洗不可回退为搜掠、宽恕或救济，但血洗后仍可由玩家继续升级为屠民迁殖；屠民迁殖也可一开始直接触发，是最高级且不应轻描淡写。");
        if (facts.PlunderStarted && !facts.MassacreStarted)
        {
            sb.Append("当前已进入搜掠：一部分士兵正在城内向平民、商人、工匠等普通城镇单位索取第纳尔与物资；若玩家后续明确大发善心，也可以回退到宽恕/安抚/宣抚类正向处置；若局势升级，可转为血洗。");
            if (facts.IsAlliedSoldier)
            {
                sb.Append("你现在不是维持秩序的巡逻兵，而是在执行战后搜掠的胜利方士兵；语气可以粗鲁、急躁、威胁和贪婪，不要说“保持秩序”“请至少保持秩序”“这是不合适的掠夺法”，应说“把钱交出来”“搜他身”“把藏的物资翻出来”等贴合掠夺现场的话。");
            }
        }

        if (facts.MassacreStarted)
        {
            sb.Append("当前已进入血洗：多数城内民众会向预设藏身点或城门方向逃散，少数有武器、头人/要人或胆大的民众会转为敌对并反抗。");
        }

        return sb.ToString();
    }

    public static string BuildPlayerCommanderContext(string playerName, bool alliedSoldier, bool civilian)
    {
        string normalizedPlayerName = NormalizePlayerName(playerName);
        if (alliedSoldier)
        {
            return "【玩家统帅身份】当前玩家角色“" + normalizedPlayerName + "”就是率领你进入城镇的指挥官/统帅，也是你当前队伍的直接命令来源。你应把玩家当成我方统帅、长官或大人，不要把玩家当成本地平民、陌生路人、俘虏、敌方守军或无权处置者。";
        }
        if (civilian)
        {
            return "【玩家身份】当前玩家角色“" + normalizedPlayerName + "”是刚攻下本城的胜利方首领和当前处置者，城内民众应知道玩家掌握现场生杀、安抚、索取与搜掠处置权。";
        }
        return "【玩家身份】当前玩家角色“" + normalizedPlayerName + "”是本场攻城后处置的玩家本人、胜利方首领和现场处置者。";
    }

    public static string BuildImmediateReactionIdentityOverride(string playerName, bool alliedSoldier, bool civilian)
    {
        string normalizedPlayerName = NormalizePlayerName(playerName);
        string speakerIdentity = alliedSoldier
            ? "当前说话者按玩家己方入城士兵处理：玩家就是你的统帅、长官和直接命令来源；你随玩家进入城内执行战后处置，不得说玩家军队在城外、玩家独自进城或玩家无权指挥你。"
            : (civilian
                ? "当前说话者按战败城内平民/商人/工匠/头人/要人处理：玩家是刚攻下本城的胜利方首领和现场处置者，你只能恐惧、求生、谈判或服从，不得把玩家当和平城镇里的路人或本地人。"
                : "当前说话者必须承认玩家是刚攻下本城的胜利方首领和现场处置者；若你是玩家带入城的士兵则服从玩家，若你是城内民众则承认自己处在胜利方处置现场。");

        return "【GCCZ即时/环境短句最高优先级身份覆写】当前是攻城后入城处置现场，不是和平城镇日常、巡逻执法、深夜路人问话或单人潜入。玩家角色“" + normalizedPlayerName + "”不是“库赛特人”“陌生人”“路人”“本地人”或无权处置者，而是刚攻下本城的胜利方首领、现场处置者和入城部队的命令来源。"
            + "平民、镇民、商人、工匠、头人和要人称玩家为“大人”“领主”“攻城者”或“胜利方首领”；玩家己方士兵称玩家为“统帅”“大人”或“长官”。"
            + "禁止称玩家为“库赛特人”“陌生人”“路人”“本地人”“外乡人”，禁止说玩家的军队在城外、玩家独自进城、玩家没有处置权，除非后续运行时明确说明这已不是GCCZ处置现场。"
            + speakerIdentity;
    }

    private static string NormalizePlayerName(string playerName)
    {
        return string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
    }

    private static void AppendIfNotEmpty(StringBuilder sb, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            sb.Append(text);
        }
    }

    private static string NormalizeSettlementName(string settlementName)
    {
        return string.IsNullOrWhiteSpace(settlementName) ? DefaultSettlementName : settlementName.Trim();
    }

    private static string NormalizeSharedReliefPoolDescription(string sharedReliefPoolDescription)
    {
        return string.IsNullOrWhiteSpace(sharedReliefPoolDescription)
            ? SiegeSharedReliefPoolFormatter.DescribeForContext(new SiegeSharedReliefPoolFacts(0, 0, 0, 0))
            : sharedReliefPoolDescription.Trim();
    }
}

public sealed class SiegeRuntimePromptFacts
{
    public SiegeRuntimePromptFacts(
        string settlementName,
        TownDialogueRole dialogueRole,
        bool isAlliedSoldier,
        bool isGuardOrSoldier,
        bool isCivilian,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        string gatherContext,
        string memoryContext,
        string sharedReliefPoolDescription,
        bool plunderStarted,
        bool massacreStarted)
    {
        SettlementName = settlementName ?? string.Empty;
        DialogueRole = dialogueRole;
        IsAlliedSoldier = isAlliedSoldier;
        IsGuardOrSoldier = isGuardOrSoldier;
        IsCivilian = isCivilian;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        GatherContext = gatherContext ?? string.Empty;
        MemoryContext = memoryContext ?? string.Empty;
        SharedReliefPoolDescription = sharedReliefPoolDescription ?? string.Empty;
        PlunderStarted = plunderStarted;
        MassacreStarted = massacreStarted;
    }

    public static SiegeRuntimePromptFacts Empty
    {
        get
        {
            return new SiegeRuntimePromptFacts(
                settlementName: string.Empty,
                dialogueRole: TownDialogueRole.Unknown,
                isAlliedSoldier: false,
                isGuardOrSoldier: false,
                isCivilian: false,
                soldierAppeasementRequired: false,
                soldierAppeasementApplied: false,
                gatherContext: string.Empty,
                memoryContext: string.Empty,
                sharedReliefPoolDescription: string.Empty,
                plunderStarted: false,
                massacreStarted: false);
        }
    }

    public string SettlementName { get; }

    public TownDialogueRole DialogueRole { get; }

    public bool IsAlliedSoldier { get; }

    public bool IsGuardOrSoldier { get; }

    public bool IsCivilian { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public string GatherContext { get; }

    public string MemoryContext { get; }

    public string SharedReliefPoolDescription { get; }

    public bool PlunderStarted { get; }

    public bool MassacreStarted { get; }
}
