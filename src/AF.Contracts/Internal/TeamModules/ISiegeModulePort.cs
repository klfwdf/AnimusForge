using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// Same-DLL team-module contract; this is not the public sub-MOD API.
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
