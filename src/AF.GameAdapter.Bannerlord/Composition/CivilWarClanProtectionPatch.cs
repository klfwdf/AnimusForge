using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// Vanilla destroys a landless independent clan 28 days after it left its kingdom (FactionDiscontinuationCampaignBehavior).
// Clans that just fought in a civil war get a grace period from the civil-war owner; while it lasts the destruction is skipped,
// so they stay alive, at peace, and can still be taken into a kingdom (joining removes them from vanilla's independent list).
// The prefix runs only when vanilla asks (one dictionary lookup per daily clan tick of an already-expired independent clan).
// DiscontinueClan has the same name and signature in the 1.3 and 1.4 game lines.
internal static class CivilWarClanProtectionPatch
{
	private static bool _patched;

	internal static void Install()
	{
		if (_patched) return;
		try
		{
			var target = AccessTools.Method(typeof(FactionDiscontinuationCampaignBehavior), "DiscontinueClan");
			if (target == null)
			{
				Logger.Log("KingdomCivilWar", "[WARN] clan protection patch target not found; civil-war clans are not sheltered");
				return;
			}
			new Harmony("AnimusForge.civilwar.clanprotection").Patch(target, prefix: new HarmonyMethod(typeof(CivilWarClanProtectionPatch), nameof(Prefix)));
			_patched = true;
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[WARN] clan protection patch failed: " + ex.Message);
		}
	}

	// Returning false skips vanilla's DestroyClanAction for a protected clan. Fails open: any error lets vanilla proceed.
	private static bool Prefix(Clan clan)
	{
		try { return !TeamModuleServices.CivilWar.IsClanProtected(clan); }
		catch { return true; }
	}
}
