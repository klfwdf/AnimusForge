using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

[HarmonyPatch(typeof(BeHostileAction), "ApplyEncounterHostileAction")]
public static class Patch_Meeting_SuppressEncounterHostileAction
{
	public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
	{
		if (DiplomacyRecentPeaceGuard.ShouldBlockEncounterHostility(attackerParty, defenderParty, "BeHostileAction.ApplyEncounterHostileAction"))
		{
			return false;
		}
		if (!MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects)
		{
			return true;
		}
		Logger.Log("MeetingBattle", "Blocked BeHostileAction.ApplyEncounterHostileAction during meeting-safe phase.");
		return false;
	}
}
