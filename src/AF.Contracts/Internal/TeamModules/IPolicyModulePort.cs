using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// Same-DLL team-module contract; this is not the public sub-MOD API.
internal interface IPolicyModulePort
{
    bool IsEligibleTargetForExternal(Hero ruler, out string failureReason);
    List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero ruler);
    bool TryProcessAcceptedAgendaTag(Hero ruler, string chainName, string playerProposalText,
        string npcReplyText, ref string content, out string failureReason);
    string BuildActivePolicyDialogueContextForExternal(Hero targetHero,
        CharacterObject targetCharacter, string kingdomIdOverride = null);
}
