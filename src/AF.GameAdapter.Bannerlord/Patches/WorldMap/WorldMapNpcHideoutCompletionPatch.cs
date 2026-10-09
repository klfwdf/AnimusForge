using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.MapEvents;

namespace AnimusForge;

/// <summary>Only AF-owned NPC raids bypass the player-only completion broadcast.
/// Native casualties, party removal, hideout deactivation and player raids remain native.</summary>
internal static class WorldMapNpcHideoutCompletionPatch
{
	private static readonly object PatchLock = new object();
	internal static bool IsReady { get; private set; }

	internal static void EnsurePatched(Harmony harmony)
	{
		if (IsReady) return;
		lock (PatchLock)
		{
			if (IsReady) return;
			var completion = AccessTools.DeclaredMethod(typeof(HideoutEventComponent), "OnBeforeFinalize");
			var finalized = AccessTools.DeclaredMethod(typeof(MapEventComponent), "FinalizeComponent");
			if (completion == null || finalized == null)
				throw new MissingMethodException("Native hideout completion/finalization hook unavailable.");
			if (harmony == null) throw new ArgumentNullException(nameof(harmony));
			harmony.Patch(completion, prefix: new HarmonyMethod(typeof(WorldMapNpcHideoutCompletionPatch), nameof(BeforeFinalizePrefix)));
			harmony.Patch(finalized, postfix: new HarmonyMethod(typeof(WorldMapNpcHideoutCompletionPatch), nameof(FinalizePostfix)));
			IsReady = true;
			Logger.Log("WorldMapCommand", "patched AF-owned NPC hideout completion isolation");
		}
	}

	private static bool BeforeFinalizePrefix(HideoutEventComponent __instance)
	{
		return !WorldMapPartyCommandBehavior.ShouldSuppressNpcHideoutPlayerCompletion(__instance?.MapEvent);
	}

	private static void FinalizePostfix(MapEventComponent __instance)
	{
		if (!(__instance is HideoutEventComponent)) return;
		try
		{
			WorldMapPartyCommandBehavior.FinalizeNpcHideoutClearBattle(__instance.MapEvent);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldMapCommand", "NPC hideout result commit failed: " + ex);
		}
	}
}
