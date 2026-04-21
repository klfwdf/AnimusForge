using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

[HarmonyPatch(typeof(ButtonWidget), "HandleClick")]
public static class Patch_GlobalUI_Click
{
	public static void Prefix(ButtonWidget __instance)
	{
		if (!TraceHelper.IsEnabled)
		{
			return;
		}
		try
		{
			Logger.LogTrace("GodMode", "\ud83d\udc49 [UI点击] ID: " + ((Widget)__instance).Id);
		}
		catch
		{
		}
	}
}
