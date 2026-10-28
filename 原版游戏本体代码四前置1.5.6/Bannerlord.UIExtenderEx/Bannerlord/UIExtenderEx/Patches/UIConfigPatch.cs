using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

namespace Bannerlord.UIExtenderEx.Patches;

internal static class UIConfigPatch
{
	public static void Patch(Harmony harmony)
	{
		harmony.TryPatch(AccessTools2.DeclaredPropertySetter("TaleWorlds.Engine.GauntletUI.UIConfig:DoNotUseGeneratedPrefabs"), AccessTools2.DeclaredMethod("Bannerlord.UIExtenderEx.Patches.UIConfigPatch:Prefix"));
	}

	private static bool Prefix()
	{
		return false;
	}
}
