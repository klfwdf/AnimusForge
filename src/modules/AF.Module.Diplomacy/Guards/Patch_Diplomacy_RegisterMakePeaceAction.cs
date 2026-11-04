using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

[HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
public static class Patch_Diplomacy_RegisterMakePeaceAction
{
	public static void Postfix(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration, MakePeaceAction.MakePeaceDetail detail)
	{
		// Native peace only opens the short re-declaration window while AF diplomacy owns war/peace.
		bool enabled;
		try { enabled = DuelSettings.GetSettings()?.EnableWorldDiplomacy == true; }
		catch { enabled = false; }
		if (enabled) DiplomacyRecentPeaceBridge.RegisterPeace(faction1, faction2, "MakePeaceAction.ApplyInternal:" + detail);
	}
}
