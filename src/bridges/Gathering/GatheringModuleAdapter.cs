using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Modules;

// Stateless bridge: preserve arguments, ref/out, return values and owner exceptions.
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
