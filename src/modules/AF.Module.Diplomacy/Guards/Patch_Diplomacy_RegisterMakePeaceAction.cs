using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

[HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
public static class Patch_Diplomacy_RegisterMakePeaceAction
{
	public static void Prefix(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2)
    {
        ExpeditionParadeBehavior.CapturePendingPeaceTerms(faction1, faction2, dailyTributeFrom1To2);
    }

    public static void Postfix(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration, MakePeaceAction.MakePeaceDetail detail)
	{
		ExpeditionParadeBehavior.ClearPendingPeaceTerms(faction1, faction2);
        DiplomacyRecentPeaceBridge.RegisterPeace(faction1, faction2, "MakePeaceAction.ApplyInternal:" + detail);
	}
}
