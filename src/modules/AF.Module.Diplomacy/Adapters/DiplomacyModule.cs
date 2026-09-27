using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// One process-lifetime entry for diplomacy capabilities and lifecycle routing.
// The ports retain their existing identity and are resolved only once.
internal sealed class DiplomacyModule
{
    internal DiplomacyModule(IDiplomacyPolicyObservationPort policy)
    {
        Policy = policy;
    }

    internal IDiplomacyConversationPort Conversation { get; } = new DiplomacyConversationModuleAdapter();
    internal IWorldDiplomacyModulePort World { get; } = new WorldDiplomacyModuleAdapter();
    internal IDiplomacyPolicyObservationPort Policy { get; }

    internal void Register(CampaignGameStarter starter) => DiplomacyModuleComposition.Register(starter);
    internal void RegisterPatches(Harmony harmony) => DiplomacyModuleComposition.RegisterPatches(harmony);
}
