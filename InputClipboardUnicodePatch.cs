using HarmonyLib;
using TaleWorlds.InputSystem;

namespace AnimusForge;

[HarmonyPatch]
internal static class InputClipboardUnicodePatch
{
	[HarmonyPatch(typeof(Input), nameof(Input.GetClipboardText))]
	[HarmonyPrefix]
	private static bool PrefixGetClipboardText(ref string __result)
	{
		if (!WindowsClipboardHelper.TryGetUnicodeText(out var text))
		{
			return true;
		}
		__result = text ?? string.Empty;
		return false;
	}

	[HarmonyPatch(typeof(Input), nameof(Input.SetClipboardText))]
	[HarmonyPrefix]
	private static bool PrefixSetClipboardText(string text)
	{
		return !WindowsClipboardHelper.TrySetUnicodeText(text);
	}
}
