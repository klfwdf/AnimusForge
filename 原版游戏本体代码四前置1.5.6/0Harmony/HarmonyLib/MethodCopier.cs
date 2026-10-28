using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib;

internal class MethodCopier
{
	private readonly MethodBodyReader reader;

	private readonly List<MethodInfo> transpilers = new List<MethodInfo>();

	internal MethodCopier(MethodBase fromMethod, ILGenerator toILGenerator, LocalBuilder[] existingVariables = null)
	{
		if ((object)fromMethod == null)
		{
			throw new ArgumentNullException("fromMethod");
		}
		reader = new MethodBodyReader(fromMethod, toILGenerator);
		reader.DeclareVariables(existingVariables);
		reader.GenerateInstructions();
	}

	internal MethodCopier(MethodCreatorConfig config)
	{
		if ((object)config.MethodBase == null)
		{
			throw new ArgumentNullException("config.methodbase");
		}
		reader = new MethodBodyReader(config.MethodBase, config.il);
		reader.DeclareVariables(config.originalVariables);
		reader.GenerateInstructions();
		reader.SetDebugging(config.debug);
	}

	internal void AddTranspiler(MethodInfo transpiler)
	{
		transpilers.Add(transpiler);
	}

	internal List<CodeInstruction> Finalize(bool stripLastReturn, out bool hasReturnCode, out bool methodEndsInDeadCode, List<Label> endLabels)
	{
		return reader.FinalizeILCodes(transpilers, stripLastReturn, out hasReturnCode, out methodEndsInDeadCode, endLabels);
	}

	internal static List<CodeInstruction> GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers)
	{
		if (generator == null)
		{
			throw new ArgumentNullException("generator");
		}
		if ((object)method == null)
		{
			throw new ArgumentNullException("method");
		}
		LocalBuilder[] existingVariables = MethodPatcherTools.DeclareOriginalLocalVariables(generator, method);
		MethodCopier methodCopier = new MethodCopier(method, generator, existingVariables);
		Patches patchInfo = Harmony.GetPatchInfo(method);
		if (patchInfo != null)
		{
			List<MethodInfo> sortedPatchMethods = PatchFunctions.GetSortedPatchMethods(method, patchInfo.Transpilers.ToArray(), debug: false);
			for (int i = 0; i < maxTranspilers && i < sortedPatchMethods.Count; i++)
			{
				methodCopier.AddTranspiler(sortedPatchMethods[i]);
			}
		}
		bool hasReturnCode;
		bool methodEndsInDeadCode;
		return methodCopier.Finalize(stripLastReturn: false, out hasReturnCode, out methodEndsInDeadCode, null);
	}
}
