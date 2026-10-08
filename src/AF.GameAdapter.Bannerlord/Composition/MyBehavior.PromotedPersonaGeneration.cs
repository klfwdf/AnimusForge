using System;
using System.Text;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    // Main-thread adapter for recruitment's existing two-stage generation.
    // Reuses Persona's sole reservation owner and the existing budgeted dispatcher.
	public static async Task GeneratePromotedNonHeroCompanionProfileForExternalAsync(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string dialogueHistory, string equipmentSummary)
    {
        MyBehavior owner = Instance;
        if (owner == null || hero == null) return;
        await owner.GeneratePromotedNonHeroCompanionProfileAsync(hero, personalName, originalFullName, originalTroopName,
            originalTroopId, cultureName, sceneLabel, joinEventFact, dialogueHistory, equipmentSummary).ConfigureAwait(false);
    }

	private Task GeneratePromotedNonHeroCompanionProfileAsync(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string dialogueHistory, string equipmentSummary)
    {
        return PromotedPersonaGenerationApplication.GeneratePromotedNonHeroCompanionProfileAsync(hero, personalName, originalFullName, originalTroopName, originalTroopId, cultureName, sceneLabel, joinEventFact, dialogueHistory, equipmentSummary);
    }

    private static Task<ApiCallResult> AwaitPromotedCompanionResponseAsync(Task<ApiCallResult> request)
    {
        return AnimusForge.Refactor.Adapters.PromotedPersonaGenerationApplicationAdapter.AwaitPromotedCompanionResponseAsync(request);
    }

	private Task GeneratePromotedNonHeroCompanionSkillsAsync(Hero hero, string personalName, string originalTroopName, string cultureName, string equipmentSummary, NpcPersonaGenerationOwner.Lease lease, long runtimeGeneration)
    {
        return PromotedPersonaGenerationApplication.GeneratePromotedNonHeroCompanionSkillsAsync(hero, personalName, originalTroopName, cultureName, equipmentSummary, lease, runtimeGeneration);
    }
}
