using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.TwoDimension;

namespace MCM.UI.Patches
{
	// Token: 0x0200001A RID: 26
	[NullableContext(1)]
	[Nullable(0)]
	internal static class MissionGauntletOptionsUIHandlerPatch
	{
		// Token: 0x060000A3 RID: 163 RVA: 0x00003BBC File Offset: 0x00001DBC
		public static void Patch(Harmony harmony)
		{
			harmony.Patch(AccessTools2.Constructor(typeof(MissionGauntletOptionsUIHandler), null, false, true), null, new HarmonyMethod(typeof(MissionGauntletOptionsUIHandlerPatch), "OnInitializePostfix", null), null, null);
			harmony.Patch(AccessTools2.Method(typeof(MissionGauntletOptionsUIHandler), "OnMissionScreenFinalize", null, null, true), null, new HarmonyMethod(typeof(MissionGauntletOptionsUIHandlerPatch), "OnFinalizePostfix", null), null, null);
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00003C30 File Offset: 0x00001E30
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void OnInitializePostfix(object __instance)
		{
			SpriteCategory spriteCategoryMCMVal;
			SpriteCategory spriteCategoryMCM = (UIResourceManager.SpriteData.SpriteCategories.TryGetValue("ui_mcm", out spriteCategoryMCMVal) ? spriteCategoryMCMVal : null);
			if (spriteCategoryMCM != null)
			{
				spriteCategoryMCM.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
			}
			MissionGauntletOptionsUIHandlerPatch._spriteCategoriesMCM.Add(__instance, spriteCategoryMCM);
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00003C7C File Offset: 0x00001E7C
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void OnFinalizePostfix(object __instance)
		{
			SpriteCategory value = MissionGauntletOptionsUIHandlerPatch._spriteCategoriesMCM.GetValue(__instance, (object _) => null);
			if (value != null)
			{
				value.Unload();
			}
			MissionGauntletOptionsUIHandlerPatch._spriteCategoriesMCM.Remove(__instance);
		}

		// Token: 0x04000026 RID: 38
		[Nullable(new byte[] { 1, 1, 2 })]
		private static readonly ConditionalWeakTable<object, SpriteCategory> _spriteCategoriesMCM = new ConditionalWeakTable<object, SpriteCategory>();
	}
}
