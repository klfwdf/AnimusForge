using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

internal static class WorldMapGovernorExpeditionNativeLifecyclePatch
{
	private static readonly object PatchLock = new object();
	private static bool _isPatched;

	internal static void EnsurePatched(Harmony harmony)
	{
		if (_isPatched)
		{
			return;
		}
		lock (PatchLock)
		{
			if (_isPatched)
			{
				return;
			}
			Harmony patcher = harmony ?? throw new ArgumentNullException(nameof(harmony));
			MethodInfo target = AccessTools.DeclaredMethod(
				typeof(TeleportationCampaignBehavior),
				"DailyTickParty",
				new[] { typeof(MobileParty) });
			if (target == null)
			{
				throw new MissingMethodException(typeof(TeleportationCampaignBehavior).FullName, "DailyTickParty(MobileParty)");
			}
			patcher.Patch(
				target,
				prefix: new HarmonyMethod(typeof(WorldMapGovernorExpeditionNativeLifecyclePatch), nameof(DailyTickPartyPrefix)));
			_isPatched = true;
			Logger.Log("WorldMapCommand", "patched native noncombatant leader replacement guard for governor expeditions");
		}
	}

	private static bool DailyTickPartyPrefix(MobileParty mobileParty)
	{
		try
		{
			return !WorldMapPartyCommandBehavior.ShouldProtectGovernorExpeditionLeaderFromNativeReplacement(mobileParty);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldMapCommand", "native governor expedition leader guard failed open: " + ex.Message);
			return true;
		}
	}
}
