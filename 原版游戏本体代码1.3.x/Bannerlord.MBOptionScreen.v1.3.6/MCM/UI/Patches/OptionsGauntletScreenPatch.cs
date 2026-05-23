using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.TwoDimension;

namespace MCM.UI.Patches
{
	// Token: 0x0200001B RID: 27
	[NullableContext(1)]
	[Nullable(0)]
	internal static class OptionsGauntletScreenPatch
	{
		// Token: 0x060000A7 RID: 167 RVA: 0x00003CD8 File Offset: 0x00001ED8
		public static void Patch(Harmony harmony)
		{
			harmony.Patch(AccessTools2.Method(typeof(GauntletOptionsScreen), "OnInitialize", null, null, true), null, new HarmonyMethod(typeof(OptionsGauntletScreenPatch), "OnInitializePostfix", null), null, null);
			harmony.Patch(AccessTools2.Method(typeof(GauntletOptionsScreen), "OnFinalize", null, null, true), null, new HarmonyMethod(typeof(OptionsGauntletScreenPatch), "OnFinalizePostfix", null), null, null);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00003D54 File Offset: 0x00001F54
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void OnInitializePostfix(object __instance)
		{
			SpriteCategory spriteCategoryMCMVal;
			SpriteCategory spriteCategoryMCM = (UIResourceManager.SpriteData.SpriteCategories.TryGetValue("ui_mcm", out spriteCategoryMCMVal) ? spriteCategoryMCMVal : null);
			if (spriteCategoryMCM != null)
			{
				spriteCategoryMCM.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
			}
			OptionsGauntletScreenPatch._spriteCategoriesMCM.Add(__instance, spriteCategoryMCM);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00003DA0 File Offset: 0x00001FA0
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void OnFinalizePostfix(object __instance)
		{
			SpriteCategory value = OptionsGauntletScreenPatch._spriteCategoriesMCM.GetValue(__instance, (object _) => null);
			if (value != null)
			{
				value.Unload();
			}
			OptionsGauntletScreenPatch._spriteCategoriesMCM.Remove(__instance);
		}

		// Token: 0x04000027 RID: 39
		[Nullable(new byte[] { 1, 1, 2 })]
		private static readonly ConditionalWeakTable<object, SpriteCategory> _spriteCategoriesMCM = new ConditionalWeakTable<object, SpriteCategory>();
	}
}
