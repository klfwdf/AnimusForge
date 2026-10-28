using System.Runtime.CompilerServices;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace MCM.UI.Patches;

internal static class OptionsVMPatch
{
	public static bool BlockSwitch { get; set; }

	public static void Patch(Harmony harmony)
	{
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(OptionsVM), "SetSelectedCategory"), new HarmonyMethod(typeof(OptionsVMPatch), "SetSelectedCategoryPatch"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool SetSelectedCategoryPatch()
	{
		return !BlockSwitch;
	}
}
