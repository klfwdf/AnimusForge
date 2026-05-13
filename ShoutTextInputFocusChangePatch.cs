using HarmonyLib;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

[HarmonyPatch(typeof(ScreenManager), "OnGameWindowFocusChange")]
public static class ShoutTextInputFocusChangePatch
{
	public static void Prefix(bool focusGained)
	{
		if (!focusGained)
		{
			ShoutTextInputPopup.CancelActiveForSystemMenu();
		}
	}
}
