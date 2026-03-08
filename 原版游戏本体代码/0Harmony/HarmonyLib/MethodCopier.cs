using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib
{
	// Token: 0x0200002E RID: 46
	internal class MethodCopier
	{
		// Token: 0x060000E3 RID: 227 RVA: 0x00005FF0 File Offset: 0x000041F0
		internal MethodCopier(MethodBase fromMethod, ILGenerator toILGenerator, LocalBuilder[] existingVariables = null)
		{
			if (fromMethod == null)
			{
				throw new ArgumentNullException("fromMethod");
			}
			this.reader = new MethodBodyReader(fromMethod, toILGenerator);
			this.reader.DeclareVariables(existingVariables);
			this.reader.GenerateInstructions();
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00006040 File Offset: 0x00004240
		internal MethodCopier(MethodCreatorConfig config)
		{
			if (config.MethodBase == null)
			{
				throw new ArgumentNullException("config.methodbase");
			}
			this.reader = new MethodBodyReader(config.MethodBase, config.il);
			this.reader.DeclareVariables(config.originalVariables);
			this.reader.GenerateInstructions();
			this.reader.SetDebugging(config.debug);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x000060B5 File Offset: 0x000042B5
		internal void AddTranspiler(MethodInfo transpiler)
		{
			this.transpilers.Add(transpiler);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000060C3 File Offset: 0x000042C3
		internal List<CodeInstruction> Finalize(bool stripLastReturn, out bool hasReturnCode, out bool methodEndsInDeadCode, List<Label> endLabels)
		{
			return this.reader.FinalizeILCodes(this.transpilers, stripLastReturn, out hasReturnCode, out methodEndsInDeadCode, endLabels);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x000060DC File Offset: 0x000042DC
		internal static List<CodeInstruction> GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers)
		{
			if (generator == null)
			{
				throw new ArgumentNullException("generator");
			}
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			LocalBuilder[] originalVariables = MethodPatcherTools.DeclareOriginalLocalVariables(generator, method);
			MethodCopier copier = new MethodCopier(method, generator, originalVariables);
			Patches info = Harmony.GetPatchInfo(method);
			if (info != null)
			{
				List<MethodInfo> sortedTranspilers = PatchFunctions.GetSortedPatchMethods(method, info.Transpilers.ToArray<Patch>(), false);
				int i = 0;
				while (i < maxTranspilers && i < sortedTranspilers.Count)
				{
					copier.AddTranspiler(sortedTranspilers[i]);
					i++;
				}
			}
			bool flag;
			bool flag2;
			return copier.Finalize(false, out flag, out flag2, null);
		}

		// Token: 0x04000088 RID: 136
		private readonly MethodBodyReader reader;

		// Token: 0x04000089 RID: 137
		private readonly List<MethodInfo> transpilers = new List<MethodInfo>();
	}
}
