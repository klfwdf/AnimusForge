using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    // Called only at prompt capture/postprocess boundaries, never from a background worker.
    // Being inside a village mission does not make a courier or stale target a scene speaker.
    private static bool HasCurrentDispositionPromptSpeaker(Hero hero, CharacterObject character, int agentIndex)
    {
        if (!IsActive() || agentIndex < 0) return false;
        if (ResolveHostileSpeakerRole(agentIndex, out Agent agent) == VillageDispositionSpeakerRole.Unknown) return false;
        var actual = agent?.Character as CharacterObject;
        return actual != null && (character == null || ReferenceEquals(character, actual))
            && (hero == null || ReferenceEquals(hero, actual.HeroObject));
    }

    internal static string BuildRuntimePromptForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
    {
        if (!HasCurrentDispositionPromptSpeaker(targetHero, targetCharacter, targetAgentIndex)) return string.Empty;
        var village = ResolveActiveVillage();
        return _hostileDispositionActive
            ? BuildHostileDialoguePrompt(village, targetHero, targetCharacter, targetAgentIndex)
            : BuildFriendlyPrompt(village, targetHero, targetCharacter, targetAgentIndex);
    }
}
