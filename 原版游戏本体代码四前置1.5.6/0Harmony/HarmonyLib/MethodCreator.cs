using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib;

internal class MethodCreator
{
	internal MethodCreatorConfig config;

	internal MethodCreator(MethodCreatorConfig config)
	{
		if ((object)config.original == null)
		{
			throw new ArgumentNullException("config.original");
		}
		this.config = config;
		if (config.debug)
		{
			FileLog.LogBuffered("### Patch: " + config.original.FullDescription());
			FileLog.FlushBuffer();
		}
		if (!config.Prepare())
		{
			throw new Exception("Could not create replacement method");
		}
	}

	internal (MethodInfo, Dictionary<int, CodeInstruction>) CreateReplacement()
	{
		config.originalVariables = this.DeclareOriginalLocalVariables(config.MethodBase);
		config.localVariables = new VariableState();
		if (config.Fixes.Any() && config.returnType != typeof(void))
		{
			config.resultVariable = config.DeclareLocal(config.returnType);
			config.AddLocal(InjectionType.Result, config.resultVariable);
			config.AddCodes(this.GenerateVariableInit(config.resultVariable, isReturnValue: true));
		}
		if (config.AnyFixHas(InjectionType.ResultRef) && config.returnType.IsByRef)
		{
			Type type = typeof(RefResult<>).MakeGenericType(config.returnType.GetElementType());
			LocalBuilder localBuilder = config.DeclareLocal(type);
			config.AddLocal(InjectionType.ResultRef, localBuilder);
			config.AddCodes(new _003C_003Ez__ReadOnlyArray<CodeInstruction>(new CodeInstruction[2]
			{
				Code.Ldnull,
				Code.Stloc[localBuilder, null]
			}));
		}
		if (config.AnyFixHas(InjectionType.ArgsArray))
		{
			LocalBuilder localBuilder2 = config.DeclareLocal(typeof(object[]));
			config.AddLocal(InjectionType.ArgsArray, localBuilder2);
			config.AddCodes(this.PrepareArgumentArray());
			config.AddCode(Code.Stloc[localBuilder2, null]);
		}
		config.skipOriginalLabel = null;
		bool flag = config.prefixes.Any(this.AffectsOriginal);
		bool flag2 = config.AnyFixHas(InjectionType.RunOriginal);
		if (flag | flag2)
		{
			config.runOriginalVariable = config.DeclareLocal(typeof(bool));
			config.AddCodes(new _003C_003Ez__ReadOnlyArray<CodeInstruction>(new CodeInstruction[2]
			{
				Code.Ldc_I4_1,
				Code.Stloc[config.runOriginalVariable, null]
			}));
			if (flag)
			{
				config.skipOriginalLabel = config.DefineLabel();
			}
		}
		config.WithFixes(delegate(MethodInfo fix)
		{
			Type declaringType = fix.DeclaringType;
			if ((object)declaringType == null)
			{
				return;
			}
			string assemblyQualifiedName = declaringType.AssemblyQualifiedName;
			config.localVariables.TryGetValue(assemblyQualifiedName, out var local2);
			foreach (InjectedParameter item2 in config.InjectionsFor(fix, InjectionType.State))
			{
				Type parameterType = item2.parameterInfo.ParameterType;
				Type type2 = (parameterType.IsByRef ? parameterType.GetElementType() : parameterType);
				if (local2 != null)
				{
					if (!type2.IsAssignableFrom(local2.LocalType))
					{
						string message = $"__state type mismatch in patch \"{fix.DeclaringType.FullName}.{fix.Name}\": previous __state was declared as \"{local2.LocalType.FullName}\" but this patch expects \"{type2.FullName}\"";
						throw new HarmonyException(message);
					}
				}
				else
				{
					LocalBuilder localBuilder3 = config.DeclareLocal(type2);
					config.AddLocal(assemblyQualifiedName, localBuilder3);
					config.AddCodes(this.GenerateVariableInit(localBuilder3));
				}
			}
		});
		config.finalizedVariable = null;
		if (config.finalizers.Count > 0)
		{
			config.finalizedVariable = config.DeclareLocal(typeof(bool));
			config.AddCodes(this.GenerateVariableInit(config.finalizedVariable));
			config.exceptionVariable = config.DeclareLocal(typeof(Exception));
			config.AddLocal(InjectionType.Exception, config.exceptionVariable);
			config.AddCodes(this.GenerateVariableInit(config.exceptionVariable));
			config.AddCode(this.MarkBlock(ExceptionBlockType.BeginExceptionBlock));
		}
		AddPrefixes();
		if (config.skipOriginalLabel.HasValue)
		{
			config.AddCodes(new _003C_003Ez__ReadOnlyArray<CodeInstruction>(new CodeInstruction[2]
			{
				Code.Ldloc[config.runOriginalVariable, null],
				Code.Brfalse[config.skipOriginalLabel.Value, null]
			}));
		}
		MethodCopier methodCopier = new MethodCopier(config);
		foreach (MethodInfo transpiler in config.transpilers)
		{
			methodCopier.AddTranspiler(transpiler);
		}
		methodCopier.AddTranspiler(PatchTools.m_GetExecutingAssemblyReplacementTranspiler);
		List<Label> list = new List<Label>();
		List<CodeInstruction> instructions = methodCopier.Finalize(stripLastReturn: true, out var hasReturnCode, out var methodEndsInDeadCode, list);
		instructions = AddInfixes(instructions).ToList();
		config.AddCode(Code.Nop["start original", null]);
		config.AddCodes(this.CleanupCodes(instructions, list));
		config.AddCode(Code.Nop["end original", null]);
		if (list.Count > 0)
		{
			config.AddCode(Code.Nop.WithLabels(list));
		}
		if ((config.resultVariable != null) & hasReturnCode)
		{
			config.AddCode(Code.Stloc[config.resultVariable, null]);
		}
		if (config.skipOriginalLabel.HasValue)
		{
			config.AddCode(Code.Nop.WithLabels(config.skipOriginalLabel.Value));
		}
		AddPostfixes(passthroughPatches: false);
		if (config.resultVariable != null && (hasReturnCode || (methodEndsInDeadCode && config.skipOriginalLabel.HasValue)))
		{
			config.AddCode(Code.Ldloc[config.resultVariable, null]);
		}
		bool flag3 = AddPostfixes(passthroughPatches: true);
		if (config.finalizers.Count > 0)
		{
			LocalBuilder local = config.GetLocal(InjectionType.Exception);
			if (flag3)
			{
				config.AddCode(Code.Stloc[config.resultVariable, null]);
				config.AddCode(Code.Ldloc[config.resultVariable, null]);
			}
			AddFinalizers(catchExceptions: false);
			config.AddCode(Code.Ldc_I4_1);
			config.AddCode(Code.Stloc[config.finalizedVariable, null]);
			Label label = config.DefineLabel();
			config.AddCode(Code.Ldloc[local, null]);
			config.AddCode(Code.Brfalse[label, null]);
			config.AddCode(Code.Ldloc[local, null]);
			config.AddCode(Code.Throw);
			config.AddCode(Code.Nop.WithLabels(label));
			config.AddCode(this.MarkBlock(ExceptionBlockType.BeginCatchBlock));
			config.AddCode(Code.Stloc[local, null]);
			config.AddCode(Code.Ldloc[config.finalizedVariable, null]);
			Label label2 = config.DefineLabel();
			config.AddCode(Code.Brtrue[label2, null]);
			bool flag4 = AddFinalizers(catchExceptions: true);
			config.AddCode(Code.Nop.WithLabels(label2));
			Label label3 = config.DefineLabel();
			config.AddCode(Code.Ldloc[local, null]);
			config.AddCode(Code.Brfalse[label3, null]);
			if (flag4)
			{
				config.AddCode(Code.Rethrow);
			}
			else
			{
				config.AddCode(Code.Ldloc[local, null]);
				config.AddCode(Code.Throw);
			}
			config.AddCode(Code.Nop.WithLabels(label3));
			config.AddCode(this.MarkBlock(ExceptionBlockType.EndExceptionBlock));
			if (config.resultVariable != null)
			{
				config.AddCode(Code.Ldloc[config.resultVariable, null]);
			}
		}
		if (methodEndsInDeadCode)
		{
			Label? skipOriginalLabel = config.skipOriginalLabel;
			if (!skipOriginalLabel.HasValue && config.finalizers.Count <= 0 && config.postfixes.Count <= 0)
			{
				goto IL_0860;
			}
		}
		config.AddCode(Code.Ret);
		goto IL_0860;
		IL_0860:
		config.instructions = FaultBlockRewriter.Rewrite(config.instructions, config.il);
		if (config.debug)
		{
			Emitter emitter = new Emitter(config.il);
			this.LogCodes(emitter, config.instructions);
		}
		Emitter emitter2 = new Emitter(config.il);
		this.EmitCodes(emitter2, config.instructions);
		MethodInfo item = config.patch.Generate();
		if (config.debug)
		{
			FileLog.LogBuffered("DONE");
			FileLog.LogBuffered("");
			FileLog.FlushBuffer();
		}
		return (item, emitter2.GetInstructions());
	}

	internal void AddPrefixes()
	{
		foreach (MethodInfo prefix in config.prefixes)
		{
			Label? label = (this.AffectsOriginal(prefix) ? new Label?(config.DefineLabel()) : ((Label?)null));
			if (label.HasValue)
			{
				config.AddCodes(new _003C_003Ez__ReadOnlyArray<CodeInstruction>(new CodeInstruction[2]
				{
					Code.Ldloc[config.runOriginalVariable, null],
					Code.Brfalse[label.Value, null]
				}));
			}
			List<KeyValuePair<LocalBuilder, Type>> list = new List<KeyValuePair<LocalBuilder, Type>>();
			config.AddCodes(this.EmitCallParameter(prefix, allowFirsParamPassthrough: false, out var tmpInstanceBoxingVar, out var tmpObjectVar, out var refResultUsed, list));
			config.AddCode(Code.Call[prefix, null]);
			if (MethodPatcherTools.OriginalParameters(prefix).Any(((ParameterInfo info, string realName) pair) => pair.realName == "__args"))
			{
				config.AddCodes(this.RestoreArgumentArray());
			}
			if (tmpInstanceBoxingVar != null)
			{
				config.AddCode(Code.Ldarg_0);
				config.AddCode(Code.Ldloc[tmpInstanceBoxingVar, null]);
				config.AddCode(Code.Unbox_Any[config.original.DeclaringType, null]);
				config.AddCode(Code.Stobj[config.original.DeclaringType, null]);
			}
			if (refResultUsed)
			{
				Label label2 = config.DefineLabel();
				config.AddCode(Code.Ldloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Brfalse_S[label2, null]);
				config.AddCode(Code.Ldloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Callvirt[AccessTools.Method(config.GetLocal(InjectionType.ResultRef).LocalType, "Invoke"), null]);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Result), null]);
				config.AddCode(Code.Ldnull);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Nop.WithLabels(label2));
			}
			else if (tmpObjectVar != null)
			{
				config.AddCode(Code.Ldloc[tmpObjectVar, null]);
				config.AddCode(Code.Unbox_Any[AccessTools.GetReturnedType(config.original), null]);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Result), null]);
			}
			list.Do(delegate(KeyValuePair<LocalBuilder, Type> tmpBoxVar)
			{
				config.AddCode(new CodeInstruction(config.OriginalIsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));
				config.AddCode(Code.Ldloc[tmpBoxVar.Key, null]);
				config.AddCode(Code.Unbox_Any[tmpBoxVar.Value, null]);
				config.AddCode(Code.Stobj[tmpBoxVar.Value, null]);
			});
			Type returnType = prefix.ReturnType;
			if (returnType != typeof(void))
			{
				if (returnType != typeof(bool))
				{
					throw new Exception($"Prefix patch {prefix} has not \"bool\" or \"void\" return type: {prefix.ReturnType}");
				}
				config.AddCode(Code.Stloc[config.runOriginalVariable, null]);
			}
			if (label.HasValue)
			{
				config.AddCode(Code.Nop.WithLabels(label.Value));
			}
		}
	}

	internal bool AddPostfixes(bool passthroughPatches)
	{
		bool result = false;
		MethodBase original = config.original;
		bool originalIsStatic = original.IsStatic;
		foreach (MethodInfo item in config.postfixes.Where((MethodInfo fix) => passthroughPatches == (fix.ReturnType != typeof(void))))
		{
			List<KeyValuePair<LocalBuilder, Type>> list = new List<KeyValuePair<LocalBuilder, Type>>();
			config.AddCodes(this.EmitCallParameter(item, allowFirsParamPassthrough: true, out var tmpInstanceBoxingVar, out var tmpObjectVar, out var refResultUsed, list));
			config.AddCode(Code.Call[item, null]);
			if (MethodPatcherTools.OriginalParameters(item).Any(((ParameterInfo info, string realName) pair) => pair.realName == "__args"))
			{
				config.AddCodes(this.RestoreArgumentArray());
			}
			if (tmpInstanceBoxingVar != null)
			{
				config.AddCode(Code.Ldarg_0);
				config.AddCode(Code.Ldloc[tmpInstanceBoxingVar, null]);
				config.AddCode(Code.Unbox_Any[original.DeclaringType, null]);
				config.AddCode(Code.Stobj[original.DeclaringType, null]);
			}
			if (refResultUsed)
			{
				Label label = config.DefineLabel();
				config.AddCode(Code.Ldloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Brfalse_S[label, null]);
				config.AddCode(Code.Ldloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Callvirt[AccessTools.Method(config.GetLocal(InjectionType.ResultRef).LocalType, "Invoke"), null]);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Result), null]);
				config.AddCode(Code.Ldnull);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Nop.WithLabels(label));
			}
			else if (tmpObjectVar != null)
			{
				config.AddCode(Code.Ldloc[tmpObjectVar, null]);
				config.AddCode(Code.Unbox_Any[AccessTools.GetReturnedType(original), null]);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Result), null]);
			}
			list.Do(delegate(KeyValuePair<LocalBuilder, Type> tmpBoxVar)
			{
				config.AddCode(new CodeInstruction(originalIsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));
				config.AddCode(Code.Ldloc[tmpBoxVar.Key, null]);
				config.AddCode(Code.Unbox_Any[tmpBoxVar.Value, null]);
				config.AddCode(Code.Stobj[tmpBoxVar.Value, null]);
			});
			if (!(item.ReturnType != typeof(void)))
			{
				continue;
			}
			ParameterInfo parameterInfo = item.GetParameters().FirstOrDefault();
			if (parameterInfo != null && item.ReturnType == parameterInfo.ParameterType)
			{
				result = true;
				continue;
			}
			if (parameterInfo != null)
			{
				throw new Exception($"Return type of pass through postfix {item} does not match type of its first parameter");
			}
			throw new Exception($"Postfix patch {item} must have a \"void\" return type");
		}
		return result;
	}

	internal bool AddFinalizers(bool catchExceptions)
	{
		bool rethrowPossible = true;
		MethodBase original = config.original;
		bool originalIsStatic = original.IsStatic;
		config.finalizers.Do(delegate(MethodInfo fix)
		{
			if (catchExceptions)
			{
				config.AddCode(this.MarkBlock(ExceptionBlockType.BeginExceptionBlock));
			}
			List<KeyValuePair<LocalBuilder, Type>> list = new List<KeyValuePair<LocalBuilder, Type>>();
			config.AddCodes(this.EmitCallParameter(fix, allowFirsParamPassthrough: false, out var tmpInstanceBoxingVar, out var tmpObjectVar, out var refResultUsed, list));
			config.AddCode(Code.Call[fix, null]);
			if (MethodPatcherTools.OriginalParameters(fix).Any(((ParameterInfo info, string realName) pair) => pair.realName == "__args"))
			{
				config.AddCodes(this.RestoreArgumentArray());
			}
			if (tmpInstanceBoxingVar != null)
			{
				config.AddCode(Code.Ldarg_0);
				config.AddCode(Code.Ldloc[tmpInstanceBoxingVar, null]);
				config.AddCode(Code.Unbox_Any[original.DeclaringType, null]);
				config.AddCode(Code.Stobj[original.DeclaringType, null]);
			}
			if (refResultUsed)
			{
				Label label = config.DefineLabel();
				config.AddCode(Code.Ldloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Brfalse_S[label, null]);
				config.AddCode(Code.Ldloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Callvirt[AccessTools.Method(config.GetLocal(InjectionType.ResultRef).LocalType, "Invoke"), null]);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Result), null]);
				config.AddCode(Code.Ldnull);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.ResultRef), null]);
				config.AddCode(Code.Nop.WithLabels(label));
			}
			else if (tmpObjectVar != null)
			{
				config.AddCode(Code.Ldloc[tmpObjectVar, null]);
				config.AddCode(Code.Unbox_Any[AccessTools.GetReturnedType(original), null]);
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Result), null]);
			}
			list.Do(delegate(KeyValuePair<LocalBuilder, Type> tmpBoxVar)
			{
				config.AddCode(new CodeInstruction(originalIsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));
				config.AddCode(Code.Ldloc[tmpBoxVar.Key, null]);
				config.AddCode(Code.Unbox_Any[tmpBoxVar.Value, null]);
				config.AddCode(Code.Stobj[tmpBoxVar.Value, null]);
			});
			if (fix.ReturnType != typeof(void))
			{
				config.AddCode(Code.Stloc[config.GetLocal(InjectionType.Exception), null]);
				rethrowPossible = false;
			}
			if (catchExceptions)
			{
				config.AddCode(this.MarkBlock(ExceptionBlockType.BeginCatchBlock));
				config.AddCode(Code.Pop);
				config.AddCode(this.MarkBlock(ExceptionBlockType.EndExceptionBlock));
			}
		});
		return rethrowPossible;
	}

	private IEnumerable<CodeInstruction> AddInfixes(IEnumerable<CodeInstruction> instructions)
	{
		IEnumerable<IGrouping<MethodInfo, CodeInstruction>> source = from ins in instructions
			where ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt
			where ins.operand is MethodInfo
			group ins by (MethodInfo)ins.operand;
		Dictionary<CodeInstruction, CodeInstruction[]> replacements = new Dictionary<CodeInstruction, CodeInstruction[]>();
		foreach (var item3 in source.Select((IGrouping<MethodInfo, CodeInstruction> g) => (Key: g.Key, Calls: g.ToList())))
		{
			MethodInfo item = item3.Key;
			List<CodeInstruction> item2 = item3.Calls;
			int count = item2.Count;
			for (int num = 0; num < count; num++)
			{
				CodeInstruction codeInstruction = item2[num];
				IEnumerable<CodeInstruction> collection = config.innerprefixes.FilterAndSort(item, num + 1, count, config.debug).SelectMany((Infix fix) => fix.Apply(config, isPrefix: true));
				IEnumerable<CodeInstruction> collection2 = config.innerpostfixes.FilterAndSort(item, num + 1, count, config.debug).SelectMany((Infix fix) => fix.Apply(config, isPrefix: false));
				Dictionary<CodeInstruction, CodeInstruction[]> dictionary = replacements;
				List<CodeInstruction> list = new List<CodeInstruction>();
				list.AddRange(collection);
				list.Add(codeInstruction);
				list.AddRange(collection2);
				dictionary[codeInstruction] = list.ToArray();
			}
		}
		return instructions.SelectMany((CodeInstruction instruction) => (!replacements.TryGetValue(instruction, out var value)) ? new CodeInstruction[1] { instruction } : value);
	}
}
