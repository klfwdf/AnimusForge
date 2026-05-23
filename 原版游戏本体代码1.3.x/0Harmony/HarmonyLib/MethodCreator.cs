using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	// Token: 0x02000032 RID: 50
	internal class MethodCreator
	{
		// Token: 0x06000102 RID: 258 RVA: 0x0000734C File Offset: 0x0000554C
		internal MethodCreator(MethodCreatorConfig config)
		{
			if (config.original == null)
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

		// Token: 0x06000103 RID: 259 RVA: 0x000073B4 File Offset: 0x000055B4
		internal ValueTuple<MethodInfo, Dictionary<int, CodeInstruction>> CreateReplacement()
		{
			this.config.originalVariables = this.DeclareOriginalLocalVariables(this.config.MethodBase);
			this.config.localVariables = new VariableState();
			if (this.config.Fixes.Any<MethodInfo>() && this.config.returnType != typeof(void))
			{
				this.config.resultVariable = this.config.DeclareLocal(this.config.returnType, false);
				this.config.AddLocal(InjectionType.Result, this.config.resultVariable);
				this.config.AddCodes(this.GenerateVariableInit(this.config.resultVariable, true));
			}
			if (this.config.AnyFixHas(InjectionType.ResultRef) && this.config.returnType.IsByRef)
			{
				Type varType = typeof(RefResult<>).MakeGenericType(new Type[] { this.config.returnType.GetElementType() });
				LocalBuilder resultRefVariable = this.config.DeclareLocal(varType, false);
				this.config.AddLocal(InjectionType.ResultRef, resultRefVariable);
				this.config.AddCodes(new <>z__ReadOnlyArray<CodeInstruction>(new CodeInstruction[]
				{
					Code.Ldnull,
					Code.Stloc[resultRefVariable, null]
				}));
			}
			if (this.config.AnyFixHas(InjectionType.ArgsArray))
			{
				LocalBuilder argsArrayVariable = this.config.DeclareLocal(typeof(object[]), false);
				this.config.AddLocal(InjectionType.ArgsArray, argsArrayVariable);
				this.config.AddCodes(this.PrepareArgumentArray());
				this.config.AddCode(Code.Stloc[argsArrayVariable, null]);
			}
			this.config.skipOriginalLabel = null;
			bool prefixAffectsOriginal = this.config.prefixes.Any(new Func<MethodInfo, bool>(base.AffectsOriginal));
			bool anyFixHasRunOriginal = this.config.AnyFixHas(InjectionType.RunOriginal);
			if (prefixAffectsOriginal || anyFixHasRunOriginal)
			{
				this.config.runOriginalVariable = this.config.DeclareLocal(typeof(bool), false);
				this.config.AddCodes(new <>z__ReadOnlyArray<CodeInstruction>(new CodeInstruction[]
				{
					Code.Ldc_I4_1,
					Code.Stloc[this.config.runOriginalVariable, null]
				}));
				if (prefixAffectsOriginal)
				{
					this.config.skipOriginalLabel = new Label?(this.config.DefineLabel());
				}
			}
			this.config.WithFixes(delegate(MethodInfo fix)
			{
				Type declaringType = fix.DeclaringType;
				if (declaringType == null)
				{
					return;
				}
				string varName = declaringType.AssemblyQualifiedName;
				LocalBuilder maybeLocal;
				this.config.localVariables.TryGetValue(varName, out maybeLocal);
				foreach (InjectedParameter injection in this.config.InjectionsFor(fix, InjectionType.State))
				{
					Type parameterType = injection.parameterInfo.ParameterType;
					Type type = (parameterType.IsByRef ? parameterType.GetElementType() : parameterType);
					if (maybeLocal != null)
					{
						if (!type.IsAssignableFrom(maybeLocal.LocalType))
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(97, 4);
							defaultInterpolatedStringHandler.AppendLiteral("__state type mismatch in patch \"");
							defaultInterpolatedStringHandler.AppendFormatted(fix.DeclaringType.FullName);
							defaultInterpolatedStringHandler.AppendLiteral(".");
							defaultInterpolatedStringHandler.AppendFormatted(fix.Name);
							defaultInterpolatedStringHandler.AppendLiteral("\": ");
							defaultInterpolatedStringHandler.AppendLiteral("previous __state was declared as \"");
							defaultInterpolatedStringHandler.AppendFormatted(maybeLocal.LocalType.FullName);
							defaultInterpolatedStringHandler.AppendLiteral("\" but this patch expects \"");
							defaultInterpolatedStringHandler.AppendFormatted(type.FullName);
							defaultInterpolatedStringHandler.AppendLiteral("\"");
							string message = defaultInterpolatedStringHandler.ToStringAndClear();
							throw new HarmonyException(message);
						}
					}
					else
					{
						LocalBuilder privateStateVariable = this.config.DeclareLocal(type, false);
						this.config.AddLocal(varName, privateStateVariable);
						this.config.AddCodes(this.GenerateVariableInit(privateStateVariable, false));
					}
				}
			});
			this.config.finalizedVariable = null;
			if (this.config.finalizers.Count > 0)
			{
				this.config.finalizedVariable = this.config.DeclareLocal(typeof(bool), false);
				this.config.AddCodes(this.GenerateVariableInit(this.config.finalizedVariable, false));
				this.config.exceptionVariable = this.config.DeclareLocal(typeof(Exception), false);
				this.config.AddLocal(InjectionType.Exception, this.config.exceptionVariable);
				this.config.AddCodes(this.GenerateVariableInit(this.config.exceptionVariable, false));
				this.config.AddCode(this.MarkBlock(ExceptionBlockType.BeginExceptionBlock));
			}
			this.AddPrefixes();
			if (this.config.skipOriginalLabel != null)
			{
				this.config.AddCodes(new <>z__ReadOnlyArray<CodeInstruction>(new CodeInstruction[]
				{
					Code.Ldloc[this.config.runOriginalVariable, null],
					Code.Brfalse[this.config.skipOriginalLabel.Value, null]
				}));
			}
			MethodCopier copier = new MethodCopier(this.config);
			foreach (MethodInfo transpiler in this.config.transpilers)
			{
				copier.AddTranspiler(transpiler);
			}
			copier.AddTranspiler(PatchTools.m_GetExecutingAssemblyReplacementTranspiler);
			List<Label> endLabels = new List<Label>();
			bool hasReturnCode;
			bool methodEndsInDeadCode;
			List<CodeInstruction> replacement = copier.Finalize(true, out hasReturnCode, out methodEndsInDeadCode, endLabels);
			replacement = this.AddInfixes(replacement).ToList<CodeInstruction>();
			this.config.AddCode(Code.Nop["start original", null]);
			this.config.AddCodes(this.CleanupCodes(replacement, endLabels));
			this.config.AddCode(Code.Nop["end original", null]);
			if (endLabels.Count > 0)
			{
				this.config.AddCode(Code.Nop.WithLabels(endLabels));
			}
			if (this.config.resultVariable != null && hasReturnCode)
			{
				this.config.AddCode(Code.Stloc[this.config.resultVariable, null]);
			}
			if (this.config.skipOriginalLabel != null)
			{
				this.config.AddCode(Code.Nop.WithLabels(new Label[] { this.config.skipOriginalLabel.Value }));
			}
			this.AddPostfixes(false);
			if (this.config.resultVariable != null && (hasReturnCode || (methodEndsInDeadCode && this.config.skipOriginalLabel != null)))
			{
				this.config.AddCode(Code.Ldloc[this.config.resultVariable, null]);
			}
			bool needsToStorePassthroughResult = this.AddPostfixes(true);
			if (this.config.finalizers.Count > 0)
			{
				LocalBuilder exceptionVariable = this.config.GetLocal(InjectionType.Exception);
				if (needsToStorePassthroughResult)
				{
					this.config.AddCode(Code.Stloc[this.config.resultVariable, null]);
					this.config.AddCode(Code.Ldloc[this.config.resultVariable, null]);
				}
				this.AddFinalizers(false);
				this.config.AddCode(Code.Ldc_I4_1);
				this.config.AddCode(Code.Stloc[this.config.finalizedVariable, null]);
				Label noExceptionLabel = this.config.DefineLabel();
				this.config.AddCode(Code.Ldloc[exceptionVariable, null]);
				this.config.AddCode(Code.Brfalse[noExceptionLabel, null]);
				this.config.AddCode(Code.Ldloc[exceptionVariable, null]);
				this.config.AddCode(Code.Throw);
				this.config.AddCode(Code.Nop.WithLabels(new Label[] { noExceptionLabel }));
				this.config.AddCode(this.MarkBlock(ExceptionBlockType.BeginCatchBlock));
				this.config.AddCode(Code.Stloc[exceptionVariable, null]);
				this.config.AddCode(Code.Ldloc[this.config.finalizedVariable, null]);
				Label endFinalizerLabel = this.config.DefineLabel();
				this.config.AddCode(Code.Brtrue[endFinalizerLabel, null]);
				bool rethrowPossible = this.AddFinalizers(true);
				this.config.AddCode(Code.Nop.WithLabels(new Label[] { endFinalizerLabel }));
				Label noExceptionLabel2 = this.config.DefineLabel();
				this.config.AddCode(Code.Ldloc[exceptionVariable, null]);
				this.config.AddCode(Code.Brfalse[noExceptionLabel2, null]);
				if (rethrowPossible)
				{
					this.config.AddCode(Code.Rethrow);
				}
				else
				{
					this.config.AddCode(Code.Ldloc[exceptionVariable, null]);
					this.config.AddCode(Code.Throw);
				}
				this.config.AddCode(Code.Nop.WithLabels(new Label[] { noExceptionLabel2 }));
				this.config.AddCode(this.MarkBlock(ExceptionBlockType.EndExceptionBlock));
				if (this.config.resultVariable != null)
				{
					this.config.AddCode(Code.Ldloc[this.config.resultVariable, null]);
				}
			}
			if (methodEndsInDeadCode)
			{
				Label? skipOriginalLabel = this.config.skipOriginalLabel;
				if (skipOriginalLabel == null && this.config.finalizers.Count <= 0 && this.config.postfixes.Count <= 0)
				{
					goto IL_860;
				}
			}
			this.config.AddCode(Code.Ret);
			IL_860:
			this.config.instructions = FaultBlockRewriter.Rewrite(this.config.instructions, this.config.il);
			if (this.config.debug)
			{
				Emitter logEmitter = new Emitter(this.config.il);
				this.LogCodes(logEmitter, this.config.instructions);
			}
			Emitter codeEmitter = new Emitter(this.config.il);
			this.EmitCodes(codeEmitter, this.config.instructions);
			MethodInfo replacementMethod = this.config.patch.Generate();
			if (this.config.debug)
			{
				FileLog.LogBuffered("DONE");
				FileLog.LogBuffered("");
				FileLog.FlushBuffer();
			}
			return new ValueTuple<MethodInfo, Dictionary<int, CodeInstruction>>(replacementMethod, codeEmitter.GetInstructions());
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00007CF4 File Offset: 0x00005EF4
		internal void AddPrefixes()
		{
			foreach (MethodInfo fix in this.config.prefixes)
			{
				Label? skipLabel = (this.AffectsOriginal(fix) ? new Label?(this.config.DefineLabel()) : null);
				if (skipLabel != null)
				{
					this.config.AddCodes(new <>z__ReadOnlyArray<CodeInstruction>(new CodeInstruction[]
					{
						Code.Ldloc[this.config.runOriginalVariable, null],
						Code.Brfalse[skipLabel.Value, null]
					}));
				}
				List<KeyValuePair<LocalBuilder, Type>> tmpBoxVars = new List<KeyValuePair<LocalBuilder, Type>>();
				LocalBuilder tmpInstanceBoxingVar;
				LocalBuilder tmpObjectVar;
				bool refResultUsed;
				this.config.AddCodes(this.EmitCallParameter(fix, false, out tmpInstanceBoxingVar, out tmpObjectVar, out refResultUsed, tmpBoxVars));
				this.config.AddCode(Code.Call[fix, null]);
				if (MethodPatcherTools.OriginalParameters(fix).Any(([TupleElementNames(new string[] { "info", "realName" })] ValueTuple<ParameterInfo, string> pair) => pair.Item2 == "__args"))
				{
					this.config.AddCodes(this.RestoreArgumentArray());
				}
				if (tmpInstanceBoxingVar != null)
				{
					this.config.AddCode(Code.Ldarg_0);
					this.config.AddCode(Code.Ldloc[tmpInstanceBoxingVar, null]);
					this.config.AddCode(Code.Unbox_Any[this.config.original.DeclaringType, null]);
					this.config.AddCode(Code.Stobj[this.config.original.DeclaringType, null]);
				}
				if (refResultUsed)
				{
					Label label = this.config.DefineLabel();
					this.config.AddCode(Code.Ldloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Brfalse_S[label, null]);
					this.config.AddCode(Code.Ldloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Callvirt[AccessTools.Method(this.config.GetLocal(InjectionType.ResultRef).LocalType, "Invoke", null, null), null]);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Result), null]);
					this.config.AddCode(Code.Ldnull);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Nop.WithLabels(new Label[] { label }));
				}
				else if (tmpObjectVar != null)
				{
					this.config.AddCode(Code.Ldloc[tmpObjectVar, null]);
					this.config.AddCode(Code.Unbox_Any[AccessTools.GetReturnedType(this.config.original), null]);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Result), null]);
				}
				tmpBoxVars.Do(delegate(KeyValuePair<LocalBuilder, Type> tmpBoxVar)
				{
					this.config.AddCode(new CodeInstruction(this.config.OriginalIsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1, null));
					this.config.AddCode(Code.Ldloc[tmpBoxVar.Key, null]);
					this.config.AddCode(Code.Unbox_Any[tmpBoxVar.Value, null]);
					this.config.AddCode(Code.Stobj[tmpBoxVar.Value, null]);
				});
				Type returnType = fix.ReturnType;
				if (returnType != typeof(void))
				{
					if (returnType != typeof(bool))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Prefix patch ");
						defaultInterpolatedStringHandler.AppendFormatted<MethodInfo>(fix);
						defaultInterpolatedStringHandler.AppendLiteral(" has not \"bool\" or \"void\" return type: ");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(fix.ReturnType);
						throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					this.config.AddCode(Code.Stloc[this.config.runOriginalVariable, null]);
				}
				if (skipLabel != null)
				{
					this.config.AddCode(Code.Nop.WithLabels(new Label[] { skipLabel.Value }));
				}
			}
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00008118 File Offset: 0x00006318
		internal bool AddPostfixes(bool passthroughPatches)
		{
			bool result = false;
			MethodBase original = this.config.original;
			bool originalIsStatic = original.IsStatic;
			IEnumerable<MethodInfo> postfixes = this.config.postfixes;
			Func<MethodInfo, bool> <>9__0;
			Func<MethodInfo, bool> predicate;
			if ((predicate = <>9__0) == null)
			{
				predicate = (<>9__0 = (MethodInfo fix) => passthroughPatches == (fix.ReturnType != typeof(void)));
			}
			Action<KeyValuePair<LocalBuilder, Type>> <>9__2;
			foreach (MethodInfo fix2 in postfixes.Where(predicate))
			{
				List<KeyValuePair<LocalBuilder, Type>> tmpBoxVars = new List<KeyValuePair<LocalBuilder, Type>>();
				LocalBuilder tmpInstanceBoxingVar;
				LocalBuilder tmpObjectVar;
				bool refResultUsed;
				this.config.AddCodes(this.EmitCallParameter(fix2, true, out tmpInstanceBoxingVar, out tmpObjectVar, out refResultUsed, tmpBoxVars));
				this.config.AddCode(Code.Call[fix2, null]);
				if (MethodPatcherTools.OriginalParameters(fix2).Any(([TupleElementNames(new string[] { "info", "realName" })] ValueTuple<ParameterInfo, string> pair) => pair.Item2 == "__args"))
				{
					this.config.AddCodes(this.RestoreArgumentArray());
				}
				if (tmpInstanceBoxingVar != null)
				{
					this.config.AddCode(Code.Ldarg_0);
					this.config.AddCode(Code.Ldloc[tmpInstanceBoxingVar, null]);
					this.config.AddCode(Code.Unbox_Any[original.DeclaringType, null]);
					this.config.AddCode(Code.Stobj[original.DeclaringType, null]);
				}
				if (refResultUsed)
				{
					Label label = this.config.DefineLabel();
					this.config.AddCode(Code.Ldloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Brfalse_S[label, null]);
					this.config.AddCode(Code.Ldloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Callvirt[AccessTools.Method(this.config.GetLocal(InjectionType.ResultRef).LocalType, "Invoke", null, null), null]);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Result), null]);
					this.config.AddCode(Code.Ldnull);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Nop.WithLabels(new Label[] { label }));
				}
				else if (tmpObjectVar != null)
				{
					this.config.AddCode(Code.Ldloc[tmpObjectVar, null]);
					this.config.AddCode(Code.Unbox_Any[AccessTools.GetReturnedType(original), null]);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Result), null]);
				}
				IEnumerable<KeyValuePair<LocalBuilder, Type>> sequence = tmpBoxVars;
				Action<KeyValuePair<LocalBuilder, Type>> action;
				if ((action = <>9__2) == null)
				{
					action = (<>9__2 = delegate(KeyValuePair<LocalBuilder, Type> tmpBoxVar)
					{
						this.config.AddCode(new CodeInstruction(originalIsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1, null));
						this.config.AddCode(Code.Ldloc[tmpBoxVar.Key, null]);
						this.config.AddCode(Code.Unbox_Any[tmpBoxVar.Value, null]);
						this.config.AddCode(Code.Stobj[tmpBoxVar.Value, null]);
					});
				}
				sequence.Do(action);
				if (fix2.ReturnType != typeof(void))
				{
					ParameterInfo firstFixParam = fix2.GetParameters().FirstOrDefault<ParameterInfo>();
					bool hasPassThroughResultParam = firstFixParam != null && fix2.ReturnType == firstFixParam.ParameterType;
					if (hasPassThroughResultParam)
					{
						result = true;
					}
					else
					{
						if (firstFixParam != null)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(79, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Return type of pass through postfix ");
							defaultInterpolatedStringHandler.AppendFormatted<MethodInfo>(fix2);
							defaultInterpolatedStringHandler.AppendLiteral(" does not match type of its first parameter");
							throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
						}
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(45, 1);
						defaultInterpolatedStringHandler2.AppendLiteral("Postfix patch ");
						defaultInterpolatedStringHandler2.AppendFormatted<MethodInfo>(fix2);
						defaultInterpolatedStringHandler2.AppendLiteral(" must have a \"void\" return type");
						throw new Exception(defaultInterpolatedStringHandler2.ToStringAndClear());
					}
				}
			}
			return result;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00008510 File Offset: 0x00006710
		internal bool AddFinalizers(bool catchExceptions)
		{
			bool rethrowPossible = true;
			MethodBase original = this.config.original;
			bool originalIsStatic = original.IsStatic;
			Action<KeyValuePair<LocalBuilder, Type>> <>9__2;
			this.config.finalizers.Do(delegate(MethodInfo fix)
			{
				if (catchExceptions)
				{
					this.config.AddCode(this.MarkBlock(ExceptionBlockType.BeginExceptionBlock));
				}
				List<KeyValuePair<LocalBuilder, Type>> tmpBoxVars = new List<KeyValuePair<LocalBuilder, Type>>();
				LocalBuilder tmpInstanceBoxingVar;
				LocalBuilder tmpObjectVar;
				bool refResultUsed;
				this.config.AddCodes(this.EmitCallParameter(fix, false, out tmpInstanceBoxingVar, out tmpObjectVar, out refResultUsed, tmpBoxVars));
				this.config.AddCode(Code.Call[fix, null]);
				if (MethodPatcherTools.OriginalParameters(fix).Any(([TupleElementNames(new string[] { "info", "realName" })] ValueTuple<ParameterInfo, string> pair) => pair.Item2 == "__args"))
				{
					this.config.AddCodes(this.RestoreArgumentArray());
				}
				if (tmpInstanceBoxingVar != null)
				{
					this.config.AddCode(Code.Ldarg_0);
					this.config.AddCode(Code.Ldloc[tmpInstanceBoxingVar, null]);
					this.config.AddCode(Code.Unbox_Any[original.DeclaringType, null]);
					this.config.AddCode(Code.Stobj[original.DeclaringType, null]);
				}
				if (refResultUsed)
				{
					Label label = this.config.DefineLabel();
					this.config.AddCode(Code.Ldloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Brfalse_S[label, null]);
					this.config.AddCode(Code.Ldloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Callvirt[AccessTools.Method(this.config.GetLocal(InjectionType.ResultRef).LocalType, "Invoke", null, null), null]);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Result), null]);
					this.config.AddCode(Code.Ldnull);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.ResultRef), null]);
					this.config.AddCode(Code.Nop.WithLabels(new Label[] { label }));
				}
				else if (tmpObjectVar != null)
				{
					this.config.AddCode(Code.Ldloc[tmpObjectVar, null]);
					this.config.AddCode(Code.Unbox_Any[AccessTools.GetReturnedType(original), null]);
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Result), null]);
				}
				IEnumerable<KeyValuePair<LocalBuilder, Type>> sequence = tmpBoxVars;
				Action<KeyValuePair<LocalBuilder, Type>> action;
				if ((action = <>9__2) == null)
				{
					action = (<>9__2 = delegate(KeyValuePair<LocalBuilder, Type> tmpBoxVar)
					{
						this.config.AddCode(new CodeInstruction(originalIsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1, null));
						this.config.AddCode(Code.Ldloc[tmpBoxVar.Key, null]);
						this.config.AddCode(Code.Unbox_Any[tmpBoxVar.Value, null]);
						this.config.AddCode(Code.Stobj[tmpBoxVar.Value, null]);
					});
				}
				sequence.Do(action);
				if (fix.ReturnType != typeof(void))
				{
					this.config.AddCode(Code.Stloc[this.config.GetLocal(InjectionType.Exception), null]);
					rethrowPossible = false;
				}
				if (catchExceptions)
				{
					this.config.AddCode(this.MarkBlock(ExceptionBlockType.BeginCatchBlock));
					this.config.AddCode(Code.Pop);
					this.config.AddCode(this.MarkBlock(ExceptionBlockType.EndExceptionBlock));
				}
			});
			return rethrowPossible;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000857C File Offset: 0x0000677C
		private IEnumerable<CodeInstruction> AddInfixes(IEnumerable<CodeInstruction> instructions)
		{
			IEnumerable<IGrouping<MethodInfo, CodeInstruction>> callGroups = from ins in instructions
				where ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt
				where ins.operand is MethodInfo
				group ins by (MethodInfo)ins.operand;
			Dictionary<CodeInstruction, CodeInstruction[]> replacements = new Dictionary<CodeInstruction, CodeInstruction[]>();
			Func<Infix, IEnumerable<CodeInstruction>> <>9__5;
			Func<Infix, IEnumerable<CodeInstruction>> <>9__6;
			foreach (ValueTuple<MethodInfo, List<CodeInstruction>> valueTuple in from g in callGroups
				select new ValueTuple<MethodInfo, List<CodeInstruction>>(g.Key, g.ToList<CodeInstruction>()))
			{
				MethodInfo innerMethod = valueTuple.Item1;
				List<CodeInstruction> calls = valueTuple.Item2;
				int total = calls.Count;
				for (int i = 0; i < total; i++)
				{
					CodeInstruction callInstruction = calls[i];
					IEnumerable<Infix> source = this.config.innerprefixes.FilterAndSort(innerMethod, i + 1, total, this.config.debug);
					Func<Infix, IEnumerable<CodeInstruction>> selector;
					if ((selector = <>9__5) == null)
					{
						selector = (<>9__5 = (Infix fix) => fix.Apply(this.config, true));
					}
					IEnumerable<CodeInstruction> prefixes = source.SelectMany(selector);
					IEnumerable<Infix> source2 = this.config.innerpostfixes.FilterAndSort(innerMethod, i + 1, total, this.config.debug);
					Func<Infix, IEnumerable<CodeInstruction>> selector2;
					if ((selector2 = <>9__6) == null)
					{
						selector2 = (<>9__6 = (Infix fix) => fix.Apply(this.config, false));
					}
					IEnumerable<CodeInstruction> postfixes = source2.SelectMany(selector2);
					Dictionary<CodeInstruction, CodeInstruction[]> replacements2 = replacements;
					CodeInstruction key = callInstruction;
					List<CodeInstruction> list = new List<CodeInstruction>();
					list.AddRange(prefixes);
					list.Add(callInstruction);
					list.AddRange(postfixes);
					replacements2[key] = list.ToArray();
				}
			}
			return instructions.SelectMany(delegate(CodeInstruction instruction)
			{
				CodeInstruction[] list2;
				if (!replacements.TryGetValue(instruction, out list2))
				{
					return new CodeInstruction[] { instruction };
				}
				return list2;
			});
		}

		// Token: 0x0400009E RID: 158
		internal MethodCreatorConfig config;
	}
}
