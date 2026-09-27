namespace AnimusForge;

// Explicit process-lifetime composition, matching the existing team services pattern.
// Adapters resolve current campaign identities on demand; no cached live campaign objects.
internal static class DiplomacyModuleServices
{
    internal static DiplomacyModule Module { get; } = new DiplomacyModule(new DiplomacyPolicyObservationBridge());
    internal static IDiplomacyConversationPort Conversation => Module.Conversation;
    internal static IWorldDiplomacyModulePort World => Module.World;
    internal static IDiplomacyPolicyObservationPort Policy => Module.Policy;
    internal static void Register(TaleWorlds.CampaignSystem.CampaignGameStarter starter) => Module.Register(starter);
    internal static void RegisterPatches(HarmonyLib.Harmony harmony) => Module.RegisterPatches(harmony);
}
