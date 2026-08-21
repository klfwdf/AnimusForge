using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free formatter for active GCCZ postprocess runtime facts.
/// AF adapters gather live objects; this core owns the prompt text structure.
/// </summary>
public static class SiegePostprocessContextBuilder
{
    public const string DefaultSettlementName = "刚被攻下的定居点";

    public const string DefaultSpeakerName = "NPC";

    public const string AlliedSoldierSpeakerIdentity = "玩家己方入城士兵";

    public const string CivilianSpeakerIdentity = "战败定居点普通民众/商人/工匠";

    public const string DefaultSpeakerIdentity = "其他场景NPC";

    public static string SelectSpeakerIdentity(bool isAlliedSoldier, bool isCivilian)
    {
        if (isAlliedSoldier)
        {
            return AlliedSoldierSpeakerIdentity;
        }

        return isCivilian ? CivilianSpeakerIdentity : DefaultSpeakerIdentity;
    }

    public static string Build(SiegePostprocessContextFacts facts)
    {
        var sb = new StringBuilder();
        sb.AppendLine(TownDialogueRoleContextProfile.Build(facts.DialogueRole));
        sb.AppendLine("【攻城处置后处理运行时事实】");
        sb.AppendLine("- 当前场景：" + (string.IsNullOrWhiteSpace(facts.SettlementName) ? DefaultSettlementName : facts.SettlementName));
        sb.AppendLine("- 当前处置状态：" + (string.IsNullOrWhiteSpace(facts.CurrentOutcome) ? "尚未选择最终处置" : facts.CurrentOutcome));
        sb.AppendLine("- 破坏性处置门控：不因同文化自动禁止；搜掠/血洗/殖民只看当前说话者是否为己方入城士兵，且是否直接回应玩家本轮明确命令。外部禁用标志=" + (facts.DestructiveAllowed ? "否" : "是/兼容字段，仅作诊断，不覆盖同文化可清算规则"));
        sb.AppendLine("- 当前说话者：" + (string.IsNullOrWhiteSpace(facts.SpeakerName) ? DefaultSpeakerName : facts.SpeakerName) + "；身份=" + (string.IsNullOrWhiteSpace(facts.SpeakerIdentity) ? DefaultSpeakerIdentity : facts.SpeakerIdentity) + "；AgentIndex=" + facts.TargetAgentIndex);
        sb.AppendLine("- 当前回复是否直接回应玩家本轮发言：" + (facts.ReplyIsDirectPlayerResponse ? "是" : "否；这是NPC之间自然接话/即时反应/传闻回声"));
        sb.AppendLine("- AF给予共享物资：" + facts.SharedReliefPoolDescription);
        sb.AppendLine("- GCCZ共享物资规则：本阶段玩家通过AF给予功能交给任一己方士兵、平民、商人、工匠、头人或要人的第纳尔、粮食或物资，全部视为全城平民共享安抚物资，不是收件NPC私人独占。");

        if (!string.IsNullOrWhiteSpace(facts.CivilianGatherContext))
        {
            sb.AppendLine("- " + facts.CivilianGatherContext);
        }

        if (!string.IsNullOrWhiteSpace(facts.InterventionMemoryContext))
        {
            sb.AppendLine("- " + facts.InterventionMemoryContext);
        }

        sb.AppendLine("- 每个NPC回复都要由后处理独立判断是否输出攻城处置标签；不要用固定词机械触发，要看玩家本轮语义、NPC回复是否明确成交/服从/传达。");
        sb.AppendLine("- 宽恕/安抚/宣抚可覆盖尚未升级为血洗的搜掠；血洗不能回退为搜掠、宽恕或救济，但血洗后仍可由玩家继续升级为屠民迁殖。屠民迁殖也可由己方士兵在开局直接触发并启动血洗式屠戮，普通民众不能触发。");
        sb.AppendLine("- 士兵知识点：胜利方士兵默认期待搜掠战利品；但只有运行时事实显示军心待安抚时，己方士兵才可含蓄不满或劝玩家重想。无论如何必须服从，不能辱骂、抗命、自动攻击或自行升级处置。");
        sb.AppendLine("- 安兵标签只用于军心待安抚时玩家对己方士兵的安抚/补偿承诺/军纪解释；它不触发民众结算，也不能代替宽恕、救济、宣抚或盟誓。");
        sb.AppendLine("- 如果只是让己方士兵分发已交付的共享粮食、物资或第纳尔，最高只输出救济，不输出宣抚或盟誓。");
        return sb.ToString().Trim();
    }
}
