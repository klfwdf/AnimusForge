using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x0200004A RID: 74
	internal static class PatchFunctions
	{
		// Token: 0x06000184 RID: 388 RVA: 0x0000BCA4 File Offset: 0x00009EA4
		internal static List<MethodInfo> GetSortedPatchMethods(MethodBase original, Patch[] patches, bool debug)
		{
			return (from p in new PatchSorter(patches, debug).Sort()
				select p.GetMethod(original)).ToList<MethodInfo>();
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0000BCE0 File Offset: 0x00009EE0
		private static List<Infix> GetInfixes(Patch[] patches)
		{
			return (from p in patches
				select new Infix(p)).ToList<Infix>();
		}

		// Token: 0x06000186 RID: 390 RVA: 0x0000BD0C File Offset: 0x00009F0C
		internal static MethodInfo UpdateWrapper(MethodBase original, PatchInfo patchInfo)
		{
			bool debug = patchInfo.Debugging || Harmony.DEBUG;
			List<MethodInfo> sortedPrefixes = PatchFunctions.GetSortedPatchMethods(original, patchInfo.prefixes, debug);
			List<MethodInfo> sortedPostfixes = PatchFunctions.GetSortedPatchMethods(original, patchInfo.postfixes, debug);
			List<MethodInfo> sortedTranspilers = PatchFunctions.GetSortedPatchMethods(original, patchInfo.transpilers, debug);
			List<MethodInfo> sortedFinalizers = PatchFunctions.GetSortedPatchMethods(original, patchInfo.finalizers, debug);
			List<Infix> sortedInnerPrefixes = PatchFunctions.GetInfixes(patchInfo.innerprefixes);
			List<Infix> sortedInnerPostfixes = PatchFunctions.GetInfixes(patchInfo.innerpostfixes);
			MethodCreator patcher = new MethodCreator(new MethodCreatorConfig(original, null, sortedPrefixes, sortedPostfixes, sortedTranspilers, sortedFinalizers, sortedInnerPrefixes, sortedInnerPostfixes, debug));
			ValueTuple<MethodInfo, Dictionary<int, CodeInstruction>> valueTuple = patcher.CreateReplacement();
			MethodInfo replacement = valueTuple.Item1;
			Dictionary<int, CodeInstruction> finalInstructions = valueTuple.Item2;
			if (replacement == null)
			{
				throw new MissingMethodException("Cannot create replacement for " + original.FullDescription());
			}
			try
			{
				PatchTools.DetourMethod(original, replacement);
			}
			catch (Exception ex)
			{
				throw HarmonyException.Create(ex, finalInstructions);
			}
			return replacement;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x0000BDF0 File Offset: 0x00009FF0
		internal static MethodInfo ReversePatch(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler)
		{
			if (standin == null)
			{
				throw new ArgumentNullException("standin");
			}
			if (standin.method == null)
			{
				throw new ArgumentNullException("standin", "standin.method is NULL");
			}
			bool debug = standin.debug.GetValueOrDefault() || Harmony.DEBUG;
			List<MethodInfo> transpilers = new List<MethodInfo>();
			if (standin.reversePatchType.GetValueOrDefault() == HarmonyReversePatchType.Snapshot)
			{
				Patches info = Harmony.GetPatchInfo(original);
				transpilers.AddRange(PatchFunctions.GetSortedPatchMethods(original, info.Transpilers.ToArray<Patch>(), debug));
			}
			if (postTranspiler != null)
			{
				transpilers.Add(postTranspiler);
			}
			List<MethodInfo> emptyFix = new List<MethodInfo>();
			List<Infix> emptyInner = new List<Infix>();
			MethodCreator patcher = new MethodCreator(new MethodCreatorConfig(standin.method, original, emptyFix, emptyFix, transpilers, emptyFix, emptyInner, emptyInner, debug));
			ValueTuple<MethodInfo, Dictionary<int, CodeInstruction>> valueTuple = patcher.CreateReplacement();
			MethodInfo replacement = valueTuple.Item1;
			Dictionary<int, CodeInstruction> finalInstructions = valueTuple.Item2;
			if (replacement == null)
			{
				throw new MissingMethodException("Cannot create replacement for " + standin.method.FullDescription());
			}
			try
			{
				PatchTools.DetourMethod(standin.method, replacement);
			}
			catch (Exception ex)
			{
				throw HarmonyException.Create(ex, finalInstructions);
			}
			return replacement;
		}
	}
}
