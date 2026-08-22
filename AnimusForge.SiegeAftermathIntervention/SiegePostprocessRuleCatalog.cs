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
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.MercyPromptTag, "宽恕：玩家明确不杀不抢、不追究、放过民众或约束军纪；这是单方处置，不需要普通民众同意。已有共享物资且回复谈发放/粮食/钱货/安置时，用[ACTION:2]。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.ReliefPromptTag, "救济：士兵路线=已有AF共享物资且士兵明确接受/传达分发给民众；平民路线=明确接受保护、安顿、军纪约束，或围绕共享物资解决粮食/钱货/供应。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.InspirePromptTag, "宣抚：NPC明确接受或传达安民宣抚、公开演讲、安定城心、争取本地合作；单纯分发物资用[ACTION:2]。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.RallyOathPromptTag, "盟誓：NPC明确接受或传达公开盟誓、归心效忠、民众/要人归附；单纯分发物资用[ACTION:2]。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.SoldierAppeasementPromptTag, "安兵：仅军心待安抚时，己方士兵明确接受玩家对士兵的安抚、补偿、军纪解释或战利安排；只安抚军心，不触发民众结算。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.GatherCiviliansPromptTag, "召集：NPC明确接受、传达或执行召集/通知/带来民众听训、演讲、游说或接受处置；看语义，不靠固定词。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.CivilianRobberyPromptTag, "抢钱：战败平民/商人/工匠/头人/要人直接回应玩家索取第纳尔、货物、粮食或物资时输出；局部抢钱，不触发原版Pillage；士兵禁用，用[ACTION:8]。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.PlunderPromptTag, "搜掠：只有玩家己方入城士兵直接回应玩家明确命令全城搜掠、收缴财物或组织战利品时输出；平民/商人/要人禁用，只能用[ACTION:7]。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.MassacrePromptTag, "Start or resume a massacre only when an allied soldier directly accepts the player's current explicit order. Before all captured targets die, a later eligible response may use [ACTION:11] to stop further killing."),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.CulturalRepopulationPromptTag, "殖民：只有玩家己方入城士兵直接回应玩家本轮明确要求杀尽原住民并迁入玩家方人口/强行改文化时输出；普通民众、士兵互聊或主动请示都禁止输出；可直接触发或血洗后升级。"),
        new SiegePostprocessRuleDefinition(SiegeActionTagCatalog.StopMassacrePromptTag, "Stop an active massacre only when an allied soldier directly accepts the player's current order to cease further killing. Do not use for pleas, suggestions, indirect speech, or an inactive massacre."),
    };

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetFallbackRules()
    {
        return FallbackRules;
    }
}
