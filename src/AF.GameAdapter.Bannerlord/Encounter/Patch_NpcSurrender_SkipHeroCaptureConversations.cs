using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;

namespace AnimusForge;

[HarmonyPatch(typeof(PlayerEncounter), "DoCaptureHeroes")]
internal static class Patch_NpcSurrender_SkipCapturedLordConversation
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !LordEncounterBehavior.TrySkipNpcSurrenderCapturedLordConversation(__instance);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Patch skip captured lord conversation failed: " + ex.Message);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoFreeOrCapturePrisonerHeroes")]
internal static class Patch_NpcSurrender_SkipFreeOrCapturePrisonerHeroConversation
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !LordEncounterBehavior.TrySkipNpcSurrenderFreeOrCapturePrisonerHeroConversation(__instance);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Patch skip free-or-capture prisoner hero conversation failed: " + ex.Message);
			return true;
		}
	}
}
