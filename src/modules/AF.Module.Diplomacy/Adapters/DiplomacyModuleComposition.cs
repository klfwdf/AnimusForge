using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Lifecycle mapping only. Registration order and Harmony entry point remain unchanged.
internal static class DiplomacyModuleComposition
{
    internal static void Register(CampaignGameStarter starter)
    {
        starter.AddBehavior(new WorldDiplomacyBehavior());
        starter.AddBehavior(new DiplomacyBehavior());
    }
    internal static void RegisterPatches(Harmony harmony) => WorldDiplomacyBehavior.RegisterHarmonyPatches(harmony);
}
