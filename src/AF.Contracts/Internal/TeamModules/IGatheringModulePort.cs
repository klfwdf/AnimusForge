using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// Same-DLL team-module contract; this is not the public sub-MOD API.
internal interface IGatheringModulePort
{
    List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero);
    string BuildPostprocessContextForExternal(Hero conversationHero);
    string NormalizeNobleGatheringPostprocessTagsForExternal(string raw);
    string BuildFeastAttendanceContext(Hero hero);
    bool TryApplyNobleGatheringTagsForExternal(Hero conversationHero, ref string content,
        out List<string> generatedFacts, out List<string> notifications);
}
