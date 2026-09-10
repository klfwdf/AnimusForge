using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// 同 DLL 的专用业务接缝；不是子 MOD 公共 API，也不授予额外动作权限。
internal interface IPolicyModulePort
{
    bool IsEligibleTargetForExternal(Hero ruler, out string failureReason);
    List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero ruler);
    bool TryProcessAcceptedAgendaTag(Hero ruler, string chainName, string playerProposalText,
        string npcReplyText, ref string content, out string failureReason);
    string BuildActivePolicyDialogueContextForExternal(Hero targetHero,
        CharacterObject targetCharacter, string kingdomIdOverride = null);
}

internal interface IGatheringModulePort
{
    List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero);
    string BuildPostprocessContextForExternal(Hero conversationHero);
    string NormalizeNobleGatheringPostprocessTagsForExternal(string raw);
    string BuildFeastAttendanceContext(Hero hero);
    bool TryApplyNobleGatheringTagsForExternal(Hero conversationHero, ref string content,
        out List<string> generatedFacts, out List<string> notifications);
}

internal interface ISiegeModulePort
{
    List<PostprocessRuleEntry> BuildPostprocessRules(bool selected, int targetAgentIndex,
        bool replyIsDirectPlayerResponse, string playerText);
    string BuildPostprocessContext(bool selected, int targetAgentIndex,
        bool replyIsDirectPlayerResponse, string playerText = null);
    string NormalizePostprocessTags(bool selected, string raw, List<PostprocessRuleEntry> rules);
    bool TryProcessActionTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex,
        ref string text, out bool actionHandled, bool replyIsDirectPlayerResponse = false,
        string playerText = null, string speakerReplyText = null);
}
