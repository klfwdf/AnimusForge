using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dynamic castle rules injected only while the castle aftermath stage is active.
/// It exposes proposal tags separately from settlement tags and filters them by the
/// current speaker, direct-response state, player intent, and unresolved runtime state.
/// </summary>
public static class SiegeCastlePostprocessRuleCatalog
{
    private static readonly SiegePostprocessRuleDefinition ProposeRecruitRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.ProposeRecruitPrisonersTag,
        "【提议标签，不结算】城堡提议收编：仅当己方士兵明确建议玩家收编普通战俘，或普通战俘士兵明确请求归顺，而玩家本轮尚未明确同意收编时输出。只登记待玩家确认状态，绝不改变名册；被俘领主、旁听转述、玩家拒绝或已经授权其他处置时禁止输出。");

    private static readonly SiegePostprocessRuleDefinition ProposeSlaughterRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.ProposeSlaughterPrisonersTag,
        "【提议标签，不结算】城堡提议屠戮：仅当己方士兵明确建议玩家屠戮普通战俘，而玩家本轮尚未明确同意屠戮时输出。只登记待玩家确认状态，绝不伤害或移除任何人；普通战俘、被俘领主、旁听转述、玩家拒绝或已经授权其他处置时禁止输出。");

    private static readonly SiegePostprocessRuleDefinition RecruitRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.RecruitPrisonersTag,
        "【结算标签】城堡收编战俘：当前规则只有在玩家本轮已明确命令收编，或明确同意本说话者此前的收编提议时才会提供。普通战俘或己方士兵直接回应并确认执行时输出；不得降级成提议，不得用于求饶、讨论、旁听或被俘领主回复。结算效果只处理本次带入的普通战俘，并按主队空余编制从俘虏名册转入主队成员名册；被俘领主不受影响。成功收编且有随行士兵在场时会建立待安兵状态，离场前未安抚则主队士气 -30。");

    private static readonly SiegePostprocessRuleDefinition SlaughterRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.SlaughterPrisonersTag,
        "【结算标签】城堡屠戮战俘：当前规则只有在玩家本轮已明确命令屠戮，或明确同意本说话者此前的屠戮提议时才会提供。普通战俘或己方士兵直接回应并确认执行时输出；不得降级成提议，不得用于恐惧闲聊、旁听、主动请示或领主处决。结算效果只处决本次带入且仍在主队俘虏名册中的普通战俘，并将其从名册移除；被俘领主不受影响，也不会把城堡升级为城镇血洗、搜掠或毁坏，城堡仍走默认宽恕结算。");

    private static readonly SiegePostprocessRuleDefinition AppeaseRule = new SiegePostprocessRuleDefinition(
        SiegeCastleActionTagCatalog.AppeaseSoldiersTag,
        "【结算标签】城堡安兵：仅在收编已引发军心待安抚、且玩家本轮确实给出安抚、补偿、军纪解释或战利安排时提供。玩家带入城堡的己方士兵直接接受并明确继续服从时输出；单纯要求服从、泛泛闲聊、疑问、战俘或领主回复均禁止输出。结算效果只把本轮收编引发的军心不满标记为已安抚，阻止离场时的士气 -30；不改动战俘名册、城堡原版结算或普通 AF 对话状态。");

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(SiegeCastlePostprocessRuleFacts facts)
    {
        facts ??= SiegeCastlePostprocessRuleFacts.Empty;
        var rules = new List<SiegePostprocessRuleDefinition>(2);
        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return rules;
        }

        bool canAnswerDisposition = facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
            || facts.SpeakerRole == SiegeCastleActionSpeakerRole.RegularPrisoner;
        SiegeCastlePlayerAuthorizationDecision dispositionAuthorization = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
            facts.PlayerText,
            facts.PendingProposalForSpeaker);
        if (facts.RemainingRegularPrisoners > 0 && canAnswerDisposition && dispositionAuthorization.IsAuthorized)
        {
            rules.Add(dispositionAuthorization.Disposition == SiegeCastlePrisonerDispositionKind.Recruit
                ? RecruitRule
                : SlaughterRule);
            return rules;
        }

        SiegeCastleSoldierAppeasementAuthorizationDecision appeasementAuthorization =
            SiegeCastleSoldierAppeasementAuthorizationPolicy.Evaluate(facts.PlayerText);
        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
            && facts.SoldierAppeasementRequired
            && !facts.SoldierAppeasementApplied
            && appeasementAuthorization.IsAuthorized)
        {
            rules.Add(AppeaseRule);
            return rules;
        }

        if (facts.RemainingRegularPrisoners <= 0
            || dispositionAuthorization.ReasonCode == "player_rejected_or_cancelled")
        {
            return rules;
        }

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier)
        {
            rules.Add(ProposeRecruitRule);
            rules.Add(ProposeSlaughterRule);
        }
        else if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.RegularPrisoner)
        {
            rules.Add(ProposeRecruitRule);
        }

        return rules;
    }
}
