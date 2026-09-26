namespace AnimusForge;

// Explicit process-lifetime composition, matching the existing team services pattern.
// Adapters resolve current campaign identities on demand; no cached live campaign objects.
internal static class DiplomacyModuleServices
{
    internal static IDiplomacyConversationPort Conversation { get; } = new DiplomacyConversationModuleAdapter();
    internal static IWorldDiplomacyModulePort World { get; } = new WorldDiplomacyModuleAdapter();
    internal static IDiplomacyPolicyObservationPort Policy { get; } = new DiplomacyPolicyObservationBridge();
}
