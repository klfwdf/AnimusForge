using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

[HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
public static class Patch_Diplomacy_RegisterMakePeaceAction
{
	public static void Postfix(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration, MakePeaceAction.MakePeaceDetail detail)
	{
		DiplomacyRecentPeaceGuard.RegisterPeace(faction1, faction2, "MakePeaceAction.ApplyInternal:" + detail);
	}
}
