using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.MountAndBlade.Options;

namespace Bannerlord.ButterLib.Implementation.HotKeys.Patches;

internal sealed class OptionsProviderPatches
{
	internal static bool Enable(Harmony harmony)
	{
		return harmony.TryPatch(AccessTools2.Method(typeof(OptionsProvider), "GetGameKeyCategoriesList"), null, AccessTools2.Method(typeof(OptionsProviderPatches), "GetGameKeyCategoriesListPostfix"));
	}

	internal static bool Disable(Harmony harmony)
	{
		harmony.Unpatch(AccessTools2.Method(typeof(OptionsProvider), "GetGameKeyCategoriesList"), AccessTools2.Method(typeof(OptionsProviderPatches), "GetGameKeyCategoriesListPostfix"));
		return true;
	}

	private static IEnumerable<string> GetGameKeyCategoriesListPostfix(IEnumerable<string> values)
	{
		return values.Concat<string>(HotKeyManagerImplementation.GlobalContainerStorage.Select((HotKeyCategoryContainer x) => x.GameKeyCategoryId)).Distinct();
	}
}
