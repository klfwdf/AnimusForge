using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;

namespace Voxforge;

[HarmonyPatch(typeof(ChangeRelationAction), "ApplyInternal")]
public static class Patch_Meeting_SuppressChangeRelationAction
{
	public static bool Prefix()
	{
		if (!MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects)
		{
			return true;
		}
		Logger.Log("MeetingBattle", "Blocked ChangeRelationAction.ApplyInternal during meeting-safe phase.");
		return false;
	}
}
