using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace MCM.UI.Patches
{
	// Token: 0x0200001C RID: 28
	internal static class OptionsVMPatch
	{
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000AB RID: 171 RVA: 0x00003DFA File Offset: 0x00001FFA
		// (set) Token: 0x060000AC RID: 172 RVA: 0x00003E01 File Offset: 0x00002001
		public static bool BlockSwitch { get; set; }

		// Token: 0x060000AD RID: 173 RVA: 0x00003E09 File Offset: 0x00002009
		[NullableContext(1)]
		public static void Patch(Harmony harmony)
		{
			harmony.Patch(AccessTools2.Method(typeof(OptionsVM), "SetSelectedCategory", null, null, true), new HarmonyMethod(typeof(OptionsVMPatch), "SetSelectedCategoryPatch", null), null, null, null);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00003E41 File Offset: 0x00002041
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static bool SetSelectedCategoryPatch()
		{
			return !OptionsVMPatch.BlockSwitch;
		}
	}
}
