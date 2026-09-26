using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// Stateless bridge: preserve arguments, ref/out, return values and owner exceptions.
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
