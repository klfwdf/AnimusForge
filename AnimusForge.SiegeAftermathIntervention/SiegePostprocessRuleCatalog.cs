using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Fallback postprocess rules for the active GCCZ intervention scene.
/// These mirror the passive ModuleData rule and keep rule wording out of the AF adapter.
/// </summary>
public static class SiegePostprocessRuleCatalog
{
    public const string RuleId = "siege_intervention_aftermath";

    public const string InjectedRuleBlockMarker = "【附加规则:siege_intervention_aftermath】";

    private static readonly SiegePostprocessRuleDefinition[] FallbackRules =
    {
        new SiegePostprocessRuleDefinition("[ACTION:宽恕]", "宽恕是玩家对战败民众的单方处置，不需要普通民众同意；只要玩家本轮明确宣布宽恕、不追究、不杀不抢、放过民众、约束军纪或禁止杀戮掠夺，就输出这个，即使NPC害怕、拒绝、咒骂、沉默或没有能力代表全城同意；若运行时显示已有AF共享物资，且回复正在谈粮食、原料、钱货、供应、商路、工坊、修缮、安置、发放或救济，优先输出救济而不是只输出宽恕；若玩家没有明确给出处置，或语义已经升级为救济/宣抚/盟誓/搜掠/血洗/殖民，就不要只输出宽恕"),
        new SiegePostprocessRuleDefinition("[ACTION:救济]", "分两种情况输出：A）当前说话者是玩家己方入城士兵时，必须同时有AF共享第纳尔/粮食/物资，并且<latest_reply>明确接受或传达把这些已交付物资分发给民众；B）当前说话者是战败平民/商人/工匠/镇民/要人时，只要<latest_reply>明确接受玩家的言语安抚、保护承诺、军纪约束、安顿民众安排，或在已有AF共享物资的运行时事实下围绕粮食、原料、钱货、供应、商路、工坊、修缮、安置、发放等民生救助达成接受，也输出本标签。士兵路线没有物资时不要输出救济，应改用宽恕或其他更准确标签"),
        new SiegePostprocessRuleDefinition("[ACTION:宣抚]", "如果NPC在<latest_reply>里明确接受或传达玩家进行安民宣抚、公开演讲安定城心、争取本地人合作，就输出这个；若只是命令己方士兵分发物资或第纳尔，不要输出此标签，改用救济"),
        new SiegePostprocessRuleDefinition("[ACTION:盟誓]", "如果NPC在<latest_reply>里明确接受或传达玩家组织公开盟誓、归心效忠、强力争取民众和要人归附，就输出这个；若只是命令己方士兵分发物资或第纳尔，不要输出此标签，改用救济"),
        new SiegePostprocessRuleDefinition("[ACTION:安兵]", "只有运行时事实显示已经触发士兵不满待安抚，并且当前说话者是玩家己方入城士兵，且<latest_reply>明确接受玩家对士兵的安抚、补偿承诺、军纪解释或日后战利安排时输出；此标签只安抚军心，不触发民众结算"),
        new SiegePostprocessRuleDefinition("[ACTION:召集]", "如果NPC在<latest_reply>里明确接受、传达或执行召集/通知/带来民众听训、演讲、游说、接受处置，就输出这个；只要语义是召集民众即可，不依赖固定措辞"),
        new SiegePostprocessRuleDefinition("[ACTION:抢钱]", "只有当前说话者是战败平民、商人、工匠、头人或要人，且当前回复是直接回应玩家本轮向其索取第纳尔、货物、粮食、物资或以财物换安全时输出；NPC闲聊、转述、士兵请示或非直接回应玩家的回声不要输出这个标签。此标签只做局部抢钱/抢物资，不触发原版Pillage；当前说话者是己方士兵时禁止输出，改用搜掠"),
        new SiegePostprocessRuleDefinition("[ACTION:搜掠]", "只有当前说话者是玩家己方入城士兵，并且<latest_reply>是直接回应玩家本轮明确命令士兵按胜利方战利权搜掠全城、收缴财物或组织战利品搜取时，才输出这个；战败平民、商人、工匠、头人或要人禁止输出搜掠，只能在财物被索取时输出抢钱；士兵与士兵、士兵与平民的自然聊天或请示不要输出"),
        new SiegePostprocessRuleDefinition("[ACTION:血洗]", "只有当前说话者是玩家己方入城士兵，并且<latest_reply>是直接回应玩家本轮明确下令血洗/屠城/清洗时，才输出这个；平民恐惧、士兵听闻、NPC互相讨论或主动请示“是否血洗”都不要输出；血洗触发后不能回退为搜掠/宽恕/救济，但之后仍可继续升级为殖民；语义不够明确时不要输出"),
        new SiegePostprocessRuleDefinition("[ACTION:殖民]", "只有当前说话者是玩家己方入城士兵，并且<latest_reply>是直接回应玩家本轮明确要求杀尽原住民并迁入玩家方人口、强行改换文化时，才输出这个；普通民众、平民对话、士兵互聊或主动请示都禁止输出；殖民可开局直接触发，也可在血洗后继续升级触发，触发后会直接启动或维持血洗式屠戮并按毁坏/迁殖结算"),
    };

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetFallbackRules()
    {
        return FallbackRules;
    }
}
