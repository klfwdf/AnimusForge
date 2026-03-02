using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Voxforge;

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
			Logger.LogTrace("GodMode", "\ud83d\udc49 [UI点击] ID: " + __instance.Id);
		}
		catch
		{
		}
	}
}
