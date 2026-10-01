using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

[HarmonyPatch(typeof(FactionManager), nameof(FactionManager.DeclareWar))]
public static class Patch_Diplomacy_GuardFactionManagerDeclareWar
{
	public static bool Prefix(IFaction faction1, IFaction faction2)
	{
		return !DiplomacyRecentPeaceGuard.ShouldBlockDeclareWar(faction1, faction2, "FactionManager.DeclareWar");
	}
}
