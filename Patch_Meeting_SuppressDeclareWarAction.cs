using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

[HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
public static class Patch_Meeting_SuppressDeclareWarAction
{
	public static bool Prefix()
	{
		if (!MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects)
		{
			return true;
		}
		Logger.Log("MeetingBattle", "Blocked DeclareWarAction.ApplyInternal during meeting-safe phase.");
		return false;
	}
}
