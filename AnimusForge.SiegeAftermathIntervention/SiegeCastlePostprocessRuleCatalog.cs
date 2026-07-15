using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dynamic castle rules injected only while the castle aftermath stage is active.
/// They are intentionally absent from the passive town ModuleData rule.
/// </summary>
public static class SiegeCastlePostprocessRuleCatalog
{
    private static readonly SiegePostprocessRuleDefinition ProposeRecruitRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.ProposeRecruitPrisonersTag,
        "城堡提议收编：仅当己方士兵明确建议玩家收编普通战俘，或普通战俘士兵明确请求归顺，而玩家本轮尚未明确同意收编时输出。此标签只登记待玩家确认的提议，不得改变名册；被俘领主、旁听转述或已经得到玩家授权时禁止输出。");

    private static readonly SiegePostprocessRuleDefinition ProposeSlaughterRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.ProposeSlaughterPrisonersTag,
        "城堡提议屠戮：仅当己方士兵明确建议玩家屠戮普通战俘，而玩家本轮尚未明确同意屠戮时输出。此标签只登记待玩家确认的提议，不得伤害或移除任何人；普通战俘、被俘领主、旁听转述或已经得到玩家授权时禁止输出。");

    private static readonly SiegePostprocessRuleDefinition RecruitRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.RecruitPrisonersTag,
        "城堡收编战俘：仅当普通战俘或己方士兵直接回应玩家本轮对收编的明确命令/同意，并确认将执行时输出。若本轮只是士兵主动提议、玩家尚未授权、泛泛讨论、旁听转述或求饶，则禁止输出；被俘领主禁止输出。");

    private static readonly SiegePostprocessRuleDefinition SlaughterRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.SlaughterPrisonersTag,
        "城堡屠戮战俘：仅当普通战俘或己方士兵直接回应玩家本轮对屠戮普通战俘的明确命令/同意，并确认将执行时输出。若本轮只是士兵主动提议、玩家尚未授权、恐惧闲聊、旁听传闻或主动请示，则禁止输出；此标签不得用于领主处决。");

    private static readonly SiegePostprocessRuleDefinition AppeaseRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.AppeaseSoldiersTag,
        "城堡安兵：仅在玩家已经收编至少一名普通战俘且军心待安抚时，由玩家带入城堡的己方士兵直接回应玩家本轮安抚、军纪解释、补偿或服从要求，并明确接受继续服从时输出。战俘、领主、旁听闲聊或完全抗命的回复禁止输出。");

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(
        int remainingRegularPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied)
    {
        var rules = new List<SiegePostprocessRuleDefinition>(5);
        if (remainingRegularPrisoners > 0)
        {
            rules.Add(ProposeRecruitRule);
            rules.Add(ProposeSlaughterRule);
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
