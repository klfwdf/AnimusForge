using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

/// <summary>
/// Keeps an NPC army intact only while its leader is executing an active AF
/// settlement-attack order. The behavior owns all eligibility and payment
/// rules; this patch is deliberately just the narrow native interception.
/// </summary>
internal static class WorldMapOrderedArmySurvivalPatch
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
				typeof(DisbandArmyAction),
				"ApplyInternal",
				new[] { typeof(Army), typeof(Army.ArmyDispersionReason) });
			if (target == null)
			{
				throw new MissingMethodException(typeof(DisbandArmyAction).FullName, "ApplyInternal(Army, ArmyDispersionReason)");
			}
			patcher.Patch(
				target,
				prefix: new HarmonyMethod(typeof(WorldMapOrderedArmySurvivalPatch), nameof(ApplyInternalPrefix)));
			_isPatched = true;
			Logger.Log("WorldMapCommand", "patched native ordered-army dissolution survival guard");
		}
	}

	private static bool ApplyInternalPrefix(Army army, Army.ArmyDispersionReason reason)
	{
		try
		{
			return !WorldMapPartyCommandBehavior.TryRenewOrderedArmyBeforeNativeDisband(army, reason);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldMapCommand", "ordered-army dissolution guard failed open: " + ex.Message);
			return true;
		}
	}
}

