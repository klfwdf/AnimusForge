using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

[HarmonyPatch(typeof(BeHostileAction), "ApplyEncounterHostileAction")]
public static class Patch_Meeting_SuppressEncounterHostileAction
{
	public static bool Prefix()
	{
		if (!MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects)
		{
			return true;
		}
		if (Mission.Current == null)
		{
			return true;
		}
		Logger.Log("MeetingBattle", "Blocked BeHostileAction.ApplyEncounterHostileAction during meeting-safe phase.");
		return false;
	}
}
