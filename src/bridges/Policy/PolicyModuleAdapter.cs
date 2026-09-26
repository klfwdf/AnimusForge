using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// Stateless bridge: preserve arguments, ref/out, return values and owner exceptions.
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
