using System.Runtime.CompilerServices;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.TwoDimension;

namespace MCM.UI.Patches;

internal static class OptionsGauntletScreenPatch
{
	private static readonly ConditionalWeakTable<object, SpriteCategory?> _spriteCategoriesMCM = new ConditionalWeakTable<object, SpriteCategory>();

	public static void Patch(Harmony harmony)
	{
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(GauntletOptionsScreen), "OnInitialize"), null, new HarmonyMethod(typeof(OptionsGauntletScreenPatch), "OnInitializePostfix"));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(GauntletOptionsScreen), "OnFinalize"), null, new HarmonyMethod(typeof(OptionsGauntletScreenPatch), "OnFinalizePostfix"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnInitializePostfix(object __instance)
	{
		SpriteCategory spriteCategory = (UIResourceManager.SpriteData.SpriteCategories.TryGetValue("ui_mcm", out var value) ? value : null);
		spriteCategory?.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
		_spriteCategoriesMCM.Add(__instance, spriteCategory);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnFinalizePostfix(object __instance)
	{
		_spriteCategoriesMCM.GetValue(__instance, (object _) => (SpriteCategory?)null)?.Unload();
		_spriteCategoriesMCM.Remove(__instance);
	}
}
