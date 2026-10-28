using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

internal static class PatchFunctions
{
	internal static List<MethodInfo> GetSortedPatchMethods(MethodBase original, Patch[] patches, bool debug)
	{
		return (from p in new PatchSorter(patches, debug).Sort()
			select p.GetMethod(original)).ToList();
	}

	private static List<Infix> GetInfixes(Patch[] patches)
	{
		return patches.Select((Patch p) => new Infix(p)).ToList();
	}

	internal static MethodInfo UpdateWrapper(MethodBase original, PatchInfo patchInfo)
	{
		bool debug = patchInfo.Debugging || Harmony.DEBUG;
		List<MethodInfo> sortedPatchMethods = GetSortedPatchMethods(original, patchInfo.prefixes, debug);
		List<MethodInfo> sortedPatchMethods2 = GetSortedPatchMethods(original, patchInfo.postfixes, debug);
		List<MethodInfo> sortedPatchMethods3 = GetSortedPatchMethods(original, patchInfo.transpilers, debug);
		List<MethodInfo> sortedPatchMethods4 = GetSortedPatchMethods(original, patchInfo.finalizers, debug);
		List<Infix> infixes = GetInfixes(patchInfo.innerprefixes);
		List<Infix> infixes2 = GetInfixes(patchInfo.innerpostfixes);
		MethodCreator methodCreator = new MethodCreator(new MethodCreatorConfig(original, null, sortedPatchMethods, sortedPatchMethods2, sortedPatchMethods3, sortedPatchMethods4, infixes, infixes2, debug));
		var (methodInfo, finalInstructions) = methodCreator.CreateReplacement();
		if ((object)methodInfo == null)
		{
			throw new MissingMethodException("Cannot create replacement for " + original.FullDescription());
		}
		try
		{
			PatchTools.DetourMethod(original, methodInfo);
			return methodInfo;
		}
		catch (Exception ex)
		{
			throw HarmonyException.Create(ex, finalInstructions);
		}
	}

	internal static MethodInfo ReversePatch(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler)
	{
		if (standin == null)
		{
			throw new ArgumentNullException("standin");
		}
		if ((object)standin.method == null)
		{
			throw new ArgumentNullException("standin", "standin.method is NULL");
		}
		bool debug = standin.debug == true || Harmony.DEBUG;
		List<MethodInfo> list = new List<MethodInfo>();
		if (standin.reversePatchType == HarmonyReversePatchType.Snapshot)
		{
			Patches patchInfo = Harmony.GetPatchInfo(original);
			list.AddRange(GetSortedPatchMethods(original, patchInfo.Transpilers.ToArray(), debug));
		}
		if ((object)postTranspiler != null)
		{
			list.Add(postTranspiler);
		}
		List<MethodInfo> list2 = new List<MethodInfo>();
		List<Infix> list3 = new List<Infix>();
		MethodCreator methodCreator = new MethodCreator(new MethodCreatorConfig(standin.method, original, list2, list2, list, list2, list3, list3, debug));
		var (methodInfo, finalInstructions) = methodCreator.CreateReplacement();
		if ((object)methodInfo == null)
		{
			throw new MissingMethodException("Cannot create replacement for " + standin.method.FullDescription());
		}
		try
		{
			PatchTools.DetourMethod(standin.method, methodInfo);
			return methodInfo;
		}
		catch (Exception ex)
		{
			throw HarmonyException.Create(ex, finalInstructions);
		}
	}
}
