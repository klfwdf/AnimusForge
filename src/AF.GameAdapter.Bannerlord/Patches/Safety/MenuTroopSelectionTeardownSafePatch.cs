using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class MenuTroopSelectionTeardownSafePatch
{
	private const string TargetTypeName = "SandBox.GauntletUI.Menu.GauntletMenuTroopSelectionView";

	private static bool _patched;

	internal static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName(TargetTypeName);
			MethodInfo methodInfo = type == null ? null : AccessTools.Method(type, "OnFinalize");
			MethodInfo methodInfo2 = AccessTools.Method(typeof(MenuTroopSelectionTeardownSafePatch), nameof(Finalizer));
			if (methodInfo == null || methodInfo2 == null)
			{
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.menu.troopselection.teardown.safety");
			harmony.Patch(methodInfo, finalizer: new HarmonyMethod(methodInfo2));
			_patched = true;
			Logger.LogTrace("System", "MenuTroopSelectionTeardownSafePatch enabled.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "MenuTroopSelectionTeardownSafePatch failed: " + ex.Message);
		}
	}

	public static Exception Finalizer(Exception __exception)
	{
		if (!(__exception is NullReferenceException))
		{
			return __exception;
		}
		try
		{
			// A live campaign means this is a normal troop-selection close. Preserve
			// the exception so SETS/GCCZ and vanilla selection errors stay visible.
			if (Campaign.Current != null)
			{
				return __exception;
			}
			Logger.Log("UiTeardown", "Suppressed stale menu troop-selection NullReferenceException after campaign teardown.");
			return null;
		}
		catch
		{
			return __exception;
		}
	}
}
