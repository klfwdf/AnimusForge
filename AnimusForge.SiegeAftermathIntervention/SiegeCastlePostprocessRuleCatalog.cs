using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dynamic castle rules injected only while the castle aftermath stage is active.
/// They are intentionally absent from the passive town ModuleData rule.
/// </summary>
public static class SiegeCastlePostprocessRuleCatalog
{
    private static readonly SiegePostprocessRuleDefinition RecruitRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.RecruitPrisonersTag,
        "城堡收编战俘：仅当普通战俘士兵直接回应玩家本轮明确提出的收编、归顺或编入部队命令，并明确接受服从时输出。被俘领主、己方士兵、旁听闲聊、主动提议、求饶但未接受收编均禁止输出。");

    private static readonly SiegePostprocessRuleDefinition SlaughterRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.SlaughterPrisonersTag,
        "城堡屠戮战俘：仅当普通战俘士兵直接回应玩家本轮明确下达的杀死、处决或屠戮普通战俘命令，并明确理解该命令将执行时输出。被俘领主、己方士兵、恐惧闲聊、旁听传闻、主动请示均禁止输出；此标签不得用于领主处决。");

    private static readonly SiegePostprocessRuleDefinition AppeaseRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.AppeaseSoldiersTag,
        "城堡安兵：仅在玩家已经收编至少一名普通战俘且军心待安抚时，由玩家带入城堡的己方士兵直接回应玩家本轮安抚、军纪解释、补偿或服从要求，并明确接受继续服从时输出。战俘、领主、旁听闲聊或完全抗命的回复禁止输出。");

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(
        int remainingRegularPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied)
    {
        var rules = new List<SiegePostprocessRuleDefinition>(3);
        if (remainingRegularPrisoners > 0)
        {
            rules.Add(RecruitRule);
            rules.Add(SlaughterRule);
        }

        if (soldierAppeasementRequired && !soldierAppeasementApplied)
        {
            rules.Add(AppeaseRule);
        }

        return rules;
    }
}
