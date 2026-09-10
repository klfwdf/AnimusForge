using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// 只委托原 owner：保留参数、ref/out、返回值和异常，不新增后处理或事实写入。
internal sealed class PolicyModuleAdapter : IPolicyModulePort
{
    public bool IsEligibleTargetForExternal(Hero ruler, out string failureReason)
        => KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal(ruler, out failureReason);

    public List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero ruler)
        => KingdomAgendaCustomPolicyBehavior.BuildRuntimePostprocessRulesForExternal(ruler);

    public bool TryProcessAcceptedAgendaTag(Hero ruler, string chainName, string playerProposalText,
        string npcReplyText, ref string content, out string failureReason)
        => KingdomAgendaCustomPolicyBehavior.TryProcessAcceptedAgendaTag(ruler, chainName,
            playerProposalText, npcReplyText, ref content, out failureReason);

    // 原 public 兼容入口目前返回空上下文；此桥不擅自恢复或重写政策业务。
    public string BuildActivePolicyDialogueContextForExternal(Hero targetHero,
        CharacterObject targetCharacter, string kingdomIdOverride = null)
        => NpcRulerPolicyBehavior.BuildActivePolicyDialogueContextForExternal(
            targetHero, targetCharacter, kingdomIdOverride);
}

internal sealed class GatheringModuleAdapter : IGatheringModulePort
{
    public List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero)
        => NobleGatheringBehavior.BuildRuntimePostprocessRulesForExternal(targetHero);

    public string BuildPostprocessContextForExternal(Hero conversationHero)
        => NobleGatheringBehavior.BuildPostprocessContextForExternal(conversationHero);

    public string NormalizeNobleGatheringPostprocessTagsForExternal(string raw)
        => NobleGatheringBehavior.NormalizeNobleGatheringPostprocessTagsForExternal(raw);

    public string BuildFeastAttendanceContext(Hero hero)
        => NobleGatheringBehavior.BuildFeastAttendanceContext(hero);

    public bool TryApplyNobleGatheringTagsForExternal(Hero conversationHero, ref string content,
        out List<string> generatedFacts, out List<string> notifications)
        => NobleGatheringBehavior.TryApplyNobleGatheringTagsForExternal(conversationHero,
            ref content, out generatedFacts, out notifications);
}

internal sealed class SiegeModuleAdapter : ISiegeModulePort
{
    // 原 AfGcczShoutBridge 继续负责模块开关、活跃场景和具体业务分流。
    public List<PostprocessRuleEntry> BuildPostprocessRules(bool selected, int targetAgentIndex,
        bool replyIsDirectPlayerResponse, string playerText)
        => AfGcczShoutBridge.BuildPostprocessRules(selected, targetAgentIndex,
            replyIsDirectPlayerResponse, playerText);

    public string BuildPostprocessContext(bool selected, int targetAgentIndex,
        bool replyIsDirectPlayerResponse, string playerText = null)
        => AfGcczShoutBridge.BuildPostprocessContext(selected, targetAgentIndex,
            replyIsDirectPlayerResponse, playerText);

    public string NormalizePostprocessTags(bool selected, string raw, List<PostprocessRuleEntry> rules)
        => AfGcczShoutBridge.NormalizePostprocessTags(selected, raw, rules);

    public bool TryProcessActionTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex,
        ref string text, out bool actionHandled, bool replyIsDirectPlayerResponse = false,
        string playerText = null, string speakerReplyText = null)
        => AfGcczShoutBridge.TryProcessActionTags(targetHero, targetCharacter, targetAgentIndex,
            ref text, out actionHandled, replyIsDirectPlayerResponse, playerText, speakerReplyText);
}
