using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x0200003C RID: 60
	internal static class MethodCreatorTools
	{
		// Token: 0x0600013B RID: 315 RVA: 0x000093CC File Offset: 0x000075CC
		internal static List<CodeInstruction> GenerateVariableInit(this MethodCreator _, LocalBuilder variable, bool isReturnValue = false)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>();
			Type type = variable.LocalType;
			if (type.IsByRef)
			{
				if (isReturnValue)
				{
					codes.Add(Code.Ldc_I4_1);
					codes.Add(Code.Newarr[type.GetElementType(), null]);
					codes.Add(Code.Ldc_I4_0);
					codes.Add(Code.Ldelema[type.GetElementType(), null]);
					codes.Add(Code.Stloc[variable, null]);
					return codes;
				}
				type = type.GetElementType();
			}
			if (type.IsEnum)
			{
				type = Enum.GetUnderlyingType(type);
			}
			if (AccessTools.IsClass(type))
			{
				codes.Add(Code.Ldnull);
				codes.Add(Code.Stloc[variable, null]);
				return codes;
			}
			if (AccessTools.IsStruct(type))
			{
				codes.Add(Code.Ldloca[variable, null]);
				codes.Add(Code.Initobj[type, null]);
				return codes;
			}
			if (AccessTools.IsValue(type))
			{
				if (type == typeof(float))
				{
					codes.Add(Code.Ldc_R4[0f, null]);
				}
				else if (type == typeof(double))
				{
					codes.Add(Code.Ldc_R8[0.0, null]);
				}
				else if (type == typeof(long) || type == typeof(ulong))
				{
					codes.Add(Code.Ldc_I8[0L, null]);
				}
				else
				{
					codes.Add(Code.Ldc_I4[0, null]);
				}
				codes.Add(Code.Stloc[variable, null]);
				return codes;
			}
			return codes;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00009590 File Offset: 0x00007790
		internal static List<CodeInstruction> PrepareArgumentArray(this MethodCreator creator)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>();
			MethodBase original = creator.config.original;
			bool originalIsStatic = original.IsStatic;
			ParameterInfo[] parameters = original.GetParameters();
			int i = 0;
			foreach (ParameterInfo pInfo in parameters)
			{
				int argIndex = i++ + ((!originalIsStatic) ? 1 : 0);
				if (pInfo.IsOut || pInfo.IsRetval)
				{
					codes.AddRange(MethodCreatorTools.InitializeOutParameter(argIndex, pInfo.ParameterType));
				}
			}
			codes.Add(Code.Ldc_I4[parameters.Length, null]);
			codes.Add(Code.Newarr[typeof(object), null]);
			i = 0;
			int arrayIdx = 0;
			foreach (ParameterInfo pInfo2 in parameters)
			{
				int argIndex2 = i++ + ((!originalIsStatic) ? 1 : 0);
				Type pType = pInfo2.ParameterType;
				bool paramByRef = pType.IsByRef;
				if (paramByRef)
				{
					pType = pType.GetElementType();
				}
				codes.Add(Code.Dup);
				codes.Add(Code.Ldc_I4[arrayIdx++, null]);
				codes.Add(Code.Ldarg[argIndex2, null]);
				if (paramByRef)
				{
					if (AccessTools.IsStruct(pType))
					{
						codes.Add(Code.Ldobj[pType, null]);
					}
					else
					{
						codes.Add(MethodCreatorTools.LoadIndOpCodeFor(pType));
					}
				}
				if (pType.IsValueType)
				{
					codes.Add(Code.Box[pType, null]);
				}
				codes.Add(Code.Stelem_Ref);
			}
			return codes;
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00009734 File Offset: 0x00007934
		internal static bool AffectsOriginal(this MethodCreator creator, MethodInfo fix)
		{
			if (fix.ReturnType == typeof(bool))
			{
				return true;
			}
			List<InjectedParameter> injectedParameters;
			if (!creator.config.injections.TryGetValue(fix, out injectedParameters))
			{
				return false;
			}
			return injectedParameters.Any(delegate(InjectedParameter parameter)
			{
				if (parameter.injectionType == InjectionType.Instance)
				{
					return false;
				}
				if (parameter.injectionType == InjectionType.OriginalMethod)
				{
					return false;
				}
				if (parameter.injectionType == InjectionType.State)
				{
					return false;
				}
				ParameterInfo p = parameter.parameterInfo;
				if (p.IsOut || p.IsRetval)
				{
					return true;
				}
				Type type = p.ParameterType;
				return type.IsByRef || (!AccessTools.IsValue(type) && !AccessTools.IsStruct(type));
			});
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00009796 File Offset: 0x00007996
		internal static CodeInstruction MarkBlock(this MethodCreator _, ExceptionBlockType blockType)
		{
			return Code.Nop.WithBlocks(new ExceptionBlock[]
			{
				new ExceptionBlock(blockType, null)
			});
		}

		// Token: 0x0600013F RID: 319 RVA: 0x000097B4 File Offset: 0x000079B4
		internal static List<CodeInstruction> EmitCallParameter(this MethodCreator creator, MethodInfo patch, bool allowFirsParamPassthrough, out LocalBuilder tmpInstanceBoxingVar, out LocalBuilder tmpObjectVar, out bool refResultUsed, List<KeyValuePair<LocalBuilder, Type>> tmpBoxVars)
		{
			tmpInstanceBoxingVar = null;
			tmpObjectVar = null;
			refResultUsed = false;
			List<CodeInstruction> codes = new List<CodeInstruction>();
			MethodCreatorConfig config = creator.config;
			MethodBase original = config.original;
			bool originalIsStatic = original.IsStatic;
			Type returnType = config.returnType;
			List<InjectedParameter> injections = config.injections[patch].ToList<InjectedParameter>();
			bool isInstance = !originalIsStatic;
			ParameterInfo[] originalParameters = original.GetParameters();
			string[] originalParameterNames = (from p in originalParameters
				select p.Name).ToArray<string>();
			Type originalType = original.DeclaringType;
			List<ParameterInfo> parameters = patch.GetParameters().ToList<ParameterInfo>();
			if (allowFirsParamPassthrough && patch.ReturnType != typeof(void) && parameters.Count > 0 && parameters[0].ParameterType == patch.ReturnType)
			{
				injections.RemoveAt(0);
				parameters.RemoveAt(0);
			}
			foreach (InjectedParameter injection in injections)
			{
				InjectionType injectionType = injection.injectionType;
				string paramRealName = injection.realName;
				Type paramType = injection.parameterInfo.ParameterType;
				LocalBuilder localBuilder;
				if (injectionType == InjectionType.OriginalMethod)
				{
					if (!MethodCreatorTools.EmitOriginalBaseMethod(original, codes))
					{
						codes.Add(Code.Ldnull);
					}
				}
				else if (injectionType == InjectionType.Exception)
				{
					if (config.exceptionVariable != null)
					{
						codes.Add(Code.Ldloc[config.exceptionVariable, null]);
					}
					else
					{
						codes.Add(Code.Ldnull);
					}
				}
				else if (injectionType == InjectionType.RunOriginal)
				{
					if (config.runOriginalVariable != null)
					{
						codes.Add(Code.Ldloc[config.runOriginalVariable, null]);
					}
					else
					{
						codes.Add(Code.Ldc_I4_0);
					}
				}
				else if (injectionType == InjectionType.Instance)
				{
					if (originalIsStatic)
					{
						codes.Add(Code.Ldnull);
					}
					else
					{
						bool parameterIsRef = paramType.IsByRef;
						bool parameterIsObject = paramType == typeof(object) || paramType == typeof(object).MakeByRefType();
						if (AccessTools.IsStruct(originalType))
						{
							if (parameterIsObject)
							{
								if (parameterIsRef)
								{
									codes.Add(Code.Ldarg_0);
									codes.Add(Code.Ldobj[originalType, null]);
									codes.Add(Code.Box[originalType, null]);
									tmpInstanceBoxingVar = config.DeclareLocal(typeof(object), false);
									codes.Add(Code.Stloc[tmpInstanceBoxingVar, null]);
									codes.Add(Code.Ldloca[tmpInstanceBoxingVar, null]);
								}
								else
								{
									codes.Add(Code.Ldarg_0);
									codes.Add(Code.Ldobj[originalType, null]);
									codes.Add(Code.Box[originalType, null]);
								}
							}
							else if (parameterIsRef)
							{
								codes.Add(Code.Ldarg_0);
							}
							else
							{
								codes.Add(Code.Ldarg_0);
								codes.Add(Code.Ldobj[originalType, null]);
							}
						}
						else if (parameterIsRef)
						{
							codes.Add(Code.Ldarga[0, null]);
						}
						else
						{
							codes.Add(Code.Ldarg_0);
						}
					}
				}
				else if (injectionType == InjectionType.ArgsArray)
				{
					LocalBuilder argsArrayVar;
					if (config.localVariables.TryGetValue(InjectionType.ArgsArray, out argsArrayVar))
					{
						codes.Add(Code.Ldloc[argsArrayVar, null]);
					}
					else
					{
						codes.Add(Code.Ldnull);
					}
				}
				else if (paramRealName.StartsWith("___", StringComparison.Ordinal))
				{
					string fieldName = paramRealName.Substring("___".Length);
					IEnumerable<char> source = fieldName;
					Func<char, bool> predicate;
					if ((predicate = MethodCreatorTools.<>O.<0>__IsDigit) == null)
					{
						predicate = (MethodCreatorTools.<>O.<0>__IsDigit = new Func<char, bool>(char.IsDigit));
					}
					FieldInfo fieldInfo;
					if (source.All(predicate))
					{
						fieldInfo = AccessTools.DeclaredField(originalType, int.Parse(fieldName));
						if (fieldInfo == null)
						{
							throw new ArgumentException("No field found at given index in class " + (((originalType != null) ? originalType.AssemblyQualifiedName : null) ?? "null"), fieldName);
						}
					}
					else
					{
						fieldInfo = AccessTools.Field(originalType, fieldName);
						if (fieldInfo == null)
						{
							throw new ArgumentException("No such field defined in class " + (((originalType != null) ? originalType.AssemblyQualifiedName : null) ?? "null"), fieldName);
						}
					}
					if (fieldInfo.IsStatic)
					{
						codes.Add(paramType.IsByRef ? Code.Ldsflda[fieldInfo, null] : Code.Ldsfld[fieldInfo, null]);
					}
					else
					{
						codes.Add(Code.Ldarg_0);
						codes.Add(paramType.IsByRef ? Code.Ldflda[fieldInfo, null] : Code.Ldfld[fieldInfo, null]);
					}
				}
				else if (injectionType == InjectionType.State)
				{
					System.Reflection.Emit.OpCode ldlocCode = (paramType.IsByRef ? System.Reflection.Emit.OpCodes.Ldloca : System.Reflection.Emit.OpCodes.Ldloc);
					VariableState localVariables = config.localVariables;
					Type declaringType = patch.DeclaringType;
					LocalBuilder stateVar;
					if (localVariables.TryGetValue(((declaringType != null) ? declaringType.AssemblyQualifiedName : null) ?? "null", out stateVar))
					{
						codes.Add(new CodeInstruction(ldlocCode, stateVar));
					}
					else
					{
						codes.Add(Code.Ldnull);
					}
				}
				else if (injectionType == InjectionType.Result)
				{
					if (returnType == typeof(void))
					{
						throw new Exception("Cannot get result from void method " + original.FullDescription());
					}
					Type resultType = paramType;
					if (resultType.IsByRef && !returnType.IsByRef)
					{
						resultType = resultType.GetElementType();
					}
					if (!resultType.IsAssignableFrom(returnType))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 4);
						defaultInterpolatedStringHandler.AppendLiteral("Cannot assign method return type ");
						defaultInterpolatedStringHandler.AppendFormatted(returnType.FullName);
						defaultInterpolatedStringHandler.AppendLiteral(" to ");
						defaultInterpolatedStringHandler.AppendFormatted("__result");
						defaultInterpolatedStringHandler.AppendLiteral(" type ");
						defaultInterpolatedStringHandler.AppendFormatted(resultType.FullName);
						defaultInterpolatedStringHandler.AppendLiteral(" for method ");
						defaultInterpolatedStringHandler.AppendFormatted(original.FullDescription());
						throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					System.Reflection.Emit.OpCode ldlocCode2 = ((paramType.IsByRef && !returnType.IsByRef) ? System.Reflection.Emit.OpCodes.Ldloca : System.Reflection.Emit.OpCodes.Ldloc);
					if (returnType.IsValueType && paramType == typeof(object).MakeByRefType())
					{
						ldlocCode2 = System.Reflection.Emit.OpCodes.Ldloc;
					}
					codes.Add(new CodeInstruction(ldlocCode2, config.GetLocal(InjectionType.Result)));
					if (returnType.IsValueType)
					{
						if (paramType == typeof(object))
						{
							codes.Add(Code.Box[returnType, null]);
						}
						else if (paramType == typeof(object).MakeByRefType())
						{
							codes.Add(Code.Box[returnType, null]);
							tmpObjectVar = config.DeclareLocal(typeof(object), false);
							codes.Add(Code.Stloc[tmpObjectVar, null]);
							codes.Add(Code.Ldloca[tmpObjectVar, null]);
						}
					}
				}
				else if (injectionType == InjectionType.ResultRef)
				{
					if (!returnType.IsByRef)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(48, 3);
						defaultInterpolatedStringHandler2.AppendLiteral("Cannot use ");
						defaultInterpolatedStringHandler2.AppendFormatted<InjectionType>(InjectionType.ResultRef);
						defaultInterpolatedStringHandler2.AppendLiteral(" with non-ref return type ");
						defaultInterpolatedStringHandler2.AppendFormatted(returnType.FullName);
						defaultInterpolatedStringHandler2.AppendLiteral(" of method ");
						defaultInterpolatedStringHandler2.AppendFormatted(original.FullDescription());
						throw new Exception(defaultInterpolatedStringHandler2.ToStringAndClear());
					}
					Type resultType2 = paramType;
					Type expectedTypeRef = typeof(RefResult<>).MakeGenericType(new Type[] { returnType.GetElementType() }).MakeByRefType();
					if (resultType2 != expectedTypeRef)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(43, 4);
						defaultInterpolatedStringHandler3.AppendLiteral("Wrong type of ");
						defaultInterpolatedStringHandler3.AppendFormatted("__resultRef");
						defaultInterpolatedStringHandler3.AppendLiteral(" for method ");
						defaultInterpolatedStringHandler3.AppendFormatted(original.FullDescription());
						defaultInterpolatedStringHandler3.AppendLiteral(". Expected ");
						defaultInterpolatedStringHandler3.AppendFormatted(expectedTypeRef.FullName);
						defaultInterpolatedStringHandler3.AppendLiteral(", got ");
						defaultInterpolatedStringHandler3.AppendFormatted(resultType2.FullName);
						throw new Exception(defaultInterpolatedStringHandler3.ToStringAndClear());
					}
					codes.Add(Code.Ldloca[config.GetLocal(InjectionType.ResultRef), null]);
					refResultUsed = true;
				}
				else if (config.localVariables.TryGetValue(paramRealName, out localBuilder))
				{
					System.Reflection.Emit.OpCode ldlocCode3 = (paramType.IsByRef ? System.Reflection.Emit.OpCodes.Ldloca : System.Reflection.Emit.OpCodes.Ldloc);
					codes.Add(new CodeInstruction(ldlocCode3, localBuilder));
				}
				else
				{
					int argumentIdx;
					if (paramRealName.StartsWith("__", StringComparison.Ordinal))
					{
						string val = paramRealName.Substring("__".Length);
						if (!int.TryParse(val, out argumentIdx))
						{
							throw new Exception("Parameter " + paramRealName + " does not contain a valid index");
						}
						if (argumentIdx < 0 || argumentIdx >= originalParameters.Length)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler4 = new DefaultInterpolatedStringHandler(28, 1);
							defaultInterpolatedStringHandler4.AppendLiteral("No parameter found at index ");
							defaultInterpolatedStringHandler4.AppendFormatted<int>(argumentIdx);
							throw new Exception(defaultInterpolatedStringHandler4.ToStringAndClear());
						}
					}
					else
					{
						argumentIdx = patch.GetArgumentIndex(originalParameterNames, injection.parameterInfo);
						if (argumentIdx == -1)
						{
							HarmonyMethod harmonyMethod = HarmonyMethodExtensions.GetMergedFromType(paramType);
							HarmonyMethod harmonyMethod2 = harmonyMethod;
							MethodType value = harmonyMethod2.methodType.GetValueOrDefault();
							if (harmonyMethod2.methodType == null)
							{
								value = MethodType.Normal;
								harmonyMethod2.methodType = new MethodType?(value);
							}
							MethodBase delegateOriginal = harmonyMethod.GetOriginalMethod();
							MethodInfo methodInfo = delegateOriginal as MethodInfo;
							if (methodInfo != null)
							{
								ConstructorInfo delegateConstructor = paramType.GetConstructor(new Type[]
								{
									typeof(object),
									typeof(IntPtr)
								});
								if (delegateConstructor != null)
								{
									if (methodInfo.IsStatic)
									{
										codes.Add(Code.Ldnull);
									}
									else
									{
										codes.Add(Code.Ldarg_0);
										if (originalType != null && originalType.IsValueType)
										{
											codes.Add(Code.Ldobj[originalType, null]);
											codes.Add(Code.Box[originalType, null]);
										}
									}
									if (!methodInfo.IsStatic && !harmonyMethod.nonVirtualDelegate)
									{
										codes.Add(Code.Dup);
										codes.Add(Code.Ldvirtftn[methodInfo, null]);
									}
									else
									{
										codes.Add(Code.Ldftn[methodInfo, null]);
									}
									codes.Add(Code.Newobj[delegateConstructor, null]);
									continue;
								}
							}
							throw new Exception("Parameter \"" + paramRealName + "\" not found in method " + original.FullDescription());
						}
					}
					Type originalParamType = originalParameters[argumentIdx].ParameterType;
					Type originalParamElementType = (originalParamType.IsByRef ? originalParamType.GetElementType() : originalParamType);
					Type patchParamType = paramType;
					Type patchParamElementType = (patchParamType.IsByRef ? patchParamType.GetElementType() : patchParamType);
					bool originalIsNormal = !originalParameters[argumentIdx].IsOut && !originalParamType.IsByRef;
					bool patchIsNormal = !injection.parameterInfo.IsOut && !patchParamType.IsByRef;
					bool needsBoxing = originalParamElementType.IsValueType && !patchParamElementType.IsValueType;
					int patchArgIndex = argumentIdx + ((isInstance > false) ? 1 : 0);
					if (originalIsNormal == patchIsNormal)
					{
						codes.Add(Code.Ldarg[patchArgIndex, null]);
						if (needsBoxing)
						{
							if (patchIsNormal)
							{
								codes.Add(Code.Box[originalParamElementType, null]);
							}
							else
							{
								codes.Add(Code.Ldobj[originalParamElementType, null]);
								codes.Add(Code.Box[originalParamElementType, null]);
								LocalBuilder tmpBoxVar = config.DeclareLocal(patchParamElementType, false);
								codes.Add(Code.Stloc[tmpBoxVar, null]);
								codes.Add(Code.Ldloca_S[tmpBoxVar, null]);
								tmpBoxVars.Add(new KeyValuePair<LocalBuilder, Type>(tmpBoxVar, originalParamElementType));
							}
						}
					}
					else if (originalIsNormal && !patchIsNormal)
					{
						if (needsBoxing)
						{
							codes.Add(Code.Ldarg[patchArgIndex, null]);
							codes.Add(Code.Box[originalParamElementType, null]);
							LocalBuilder tmpBoxVar2 = config.DeclareLocal(patchParamElementType, false);
							codes.Add(Code.Stloc[tmpBoxVar2, null]);
							codes.Add(Code.Ldloca_S[tmpBoxVar2, null]);
						}
						else
						{
							codes.Add(Code.Ldarga[patchArgIndex, null]);
						}
					}
					else
					{
						codes.Add(Code.Ldarg[patchArgIndex, null]);
						if (needsBoxing)
						{
							codes.Add(Code.Ldobj[originalParamElementType, null]);
							codes.Add(Code.Box[originalParamElementType, null]);
						}
						else if (originalParamElementType.IsValueType)
						{
							codes.Add(Code.Ldobj[originalParamElementType, null]);
						}
						else
						{
							codes.Add(new CodeInstruction(MethodCreatorTools.LoadIndOpCodeFor(originalParameters[argumentIdx].ParameterType)));
						}
					}
				}
			}
			return codes;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000A484 File Offset: 0x00008684
		internal static LocalBuilder[] DeclareOriginalLocalVariables(this MethodCreator creator, MethodBase member)
		{
			System.Reflection.MethodBody methodBody = member.GetMethodBody();
			IList<LocalVariableInfo> vars = ((methodBody != null) ? methodBody.LocalVariables : null);
			if (vars == null)
			{
				return Array.Empty<LocalBuilder>();
			}
			return (from lvi in vars
				select creator.config.il.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray<LocalBuilder>();
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000A4D4 File Offset: 0x000086D4
		internal static List<CodeInstruction> RestoreArgumentArray(this MethodCreator creator)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>();
			MethodBase original = creator.config.original;
			bool originalIsStatic = original.IsStatic;
			ParameterInfo[] parameters = original.GetParameters();
			int i = 0;
			int arrayIdx = 0;
			foreach (ParameterInfo pInfo in parameters)
			{
				int argIndex = i++ + ((!originalIsStatic) ? 1 : 0);
				Type pType = pInfo.ParameterType;
				if (pType.IsByRef)
				{
					pType = pType.GetElementType();
					codes.Add(Code.Ldarg[argIndex, null]);
					codes.Add(Code.Ldloc[creator.config.GetLocal(InjectionType.ArgsArray), null]);
					codes.Add(Code.Ldc_I4[arrayIdx, null]);
					codes.Add(Code.Ldelem_Ref);
					if (pType.IsValueType)
					{
						codes.Add(Code.Unbox_Any[pType, null]);
						if (AccessTools.IsStruct(pType))
						{
							codes.Add(Code.Stobj[pType, null]);
						}
						else
						{
							codes.Add(MethodCreatorTools.StoreIndOpCodeFor(pType));
						}
					}
					else
					{
						codes.Add(Code.Castclass[pType, null]);
						codes.Add(Code.Stind_Ref);
					}
				}
				else
				{
					codes.Add(Code.Ldloc[creator.config.GetLocal(InjectionType.ArgsArray), null]);
					codes.Add(Code.Ldc_I4[arrayIdx, null]);
					codes.Add(Code.Ldelem_Ref);
					if (pType.IsValueType)
					{
						codes.Add(Code.Unbox_Any[pType, null]);
					}
					else
					{
						codes.Add(Code.Castclass[pType, null]);
					}
					codes.Add(Code.Starg[argIndex, null]);
				}
				arrayIdx++;
			}
			return codes;
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000A6B0 File Offset: 0x000088B0
		internal static IEnumerable<CodeInstruction> CleanupCodes(this MethodCreator creator, IEnumerable<CodeInstruction> instructions, List<Label> endLabels)
		{
			MethodCreatorTools.<CleanupCodes>d__10 <CleanupCodes>d__ = new MethodCreatorTools.<CleanupCodes>d__10(-2);
			<CleanupCodes>d__.<>3__creator = creator;
			<CleanupCodes>d__.<>3__instructions = instructions;
			<CleanupCodes>d__.<>3__endLabels = endLabels;
			return <CleanupCodes>d__;
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000A6D0 File Offset: 0x000088D0
		internal static void LogCodes(this MethodCreator _, Emitter emitter, List<CodeInstruction> codeInstructions)
		{
			int codePos = emitter.CurrentPos();
			IEnumerable<VariableDefinition> sequence = emitter.Variables();
			Action<VariableDefinition> action;
			if ((action = MethodCreatorTools.<>O.<1>__LogIL) == null)
			{
				action = (MethodCreatorTools.<>O.<1>__LogIL = new Action<VariableDefinition>(FileLog.LogIL));
			}
			sequence.Do(action);
			Action<Label> <>9__1;
			Action<ExceptionBlock> <>9__2;
			Action<ExceptionBlock> <>9__3;
			codeInstructions.Do(delegate(CodeInstruction codeInstruction)
			{
				IEnumerable<Label> labels = codeInstruction.labels;
				Action<Label> action2;
				if ((action2 = <>9__1) == null)
				{
					action2 = (<>9__1 = delegate(Label label)
					{
						FileLog.LogIL(codePos, label);
					});
				}
				labels.Do(action2);
				IEnumerable<ExceptionBlock> blocks = codeInstruction.blocks;
				Action<ExceptionBlock> action3;
				if ((action3 = <>9__2) == null)
				{
					action3 = (<>9__2 = delegate(ExceptionBlock block)
					{
						FileLog.LogILBlockBegin(codePos, block);
					});
				}
				blocks.Do(action3);
				System.Reflection.Emit.OpCode code = codeInstruction.opcode;
				object operand = codeInstruction.operand;
				bool realCode = true;
				System.Reflection.Emit.OperandType operandType = code.OperandType;
				if (operandType != System.Reflection.Emit.OperandType.InlineNone)
				{
					if (operandType != System.Reflection.Emit.OperandType.InlineSig)
					{
						FileLog.LogIL(codePos, code, operand);
					}
					else
					{
						FileLog.LogIL(codePos, code, (ICallSiteGenerator)operand);
					}
				}
				else
				{
					string comment = codeInstruction.IsAnnotation();
					if (comment != null)
					{
						FileLog.LogILComment(codePos, comment);
						realCode = false;
					}
					else
					{
						FileLog.LogIL(codePos, code);
					}
				}
				IEnumerable<ExceptionBlock> blocks2 = codeInstruction.blocks;
				Action<ExceptionBlock> action4;
				if ((action4 = <>9__3) == null)
				{
					action4 = (<>9__3 = delegate(ExceptionBlock block)
					{
						FileLog.LogILBlockEnd(codePos, block);
					});
				}
				blocks2.Do(action4);
				if (realCode)
				{
					codePos += codeInstruction.GetSize();
				}
			});
			FileLog.FlushBuffer();
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000A72C File Offset: 0x0000892C
		internal static void EmitCodes(this MethodCreator _, Emitter emitter, List<CodeInstruction> codeInstructions)
		{
			Action<Label> <>9__1;
			Action<ExceptionBlock> <>9__2;
			Action<ExceptionBlock> <>9__3;
			codeInstructions.Do(delegate(CodeInstruction codeInstruction)
			{
				IEnumerable<Label> labels = codeInstruction.labels;
				Action<Label> action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate(Label label)
					{
						emitter.MarkLabel(label);
					});
				}
				labels.Do(action);
				IEnumerable<ExceptionBlock> blocks = codeInstruction.blocks;
				Action<ExceptionBlock> action2;
				if ((action2 = <>9__2) == null)
				{
					action2 = (<>9__2 = delegate(ExceptionBlock block)
					{
						Label? label;
						emitter.MarkBlockBefore(block, out label);
					});
				}
				blocks.Do(action2);
				System.Reflection.Emit.OpCode code = codeInstruction.opcode;
				object operand = codeInstruction.operand;
				System.Reflection.Emit.OperandType operandType = code.OperandType;
				if (operandType != System.Reflection.Emit.OperandType.InlineNone)
				{
					if (operandType != System.Reflection.Emit.OperandType.InlineSig)
					{
						if (operand == null)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Wrong null argument: ");
							defaultInterpolatedStringHandler.AppendFormatted<CodeInstruction>(codeInstruction);
							throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
						}
						emitter.DynEmit(code, operand);
					}
					else
					{
						if (operand == null)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(21, 1);
							defaultInterpolatedStringHandler2.AppendLiteral("Wrong null argument: ");
							defaultInterpolatedStringHandler2.AppendFormatted<CodeInstruction>(codeInstruction);
							throw new Exception(defaultInterpolatedStringHandler2.ToStringAndClear());
						}
						if (!(operand is ICallSiteGenerator))
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(29, 2);
							defaultInterpolatedStringHandler3.AppendLiteral("Wrong Emit argument type ");
							defaultInterpolatedStringHandler3.AppendFormatted<Type>(operand.GetType());
							defaultInterpolatedStringHandler3.AppendLiteral(" in ");
							defaultInterpolatedStringHandler3.AppendFormatted<CodeInstruction>(codeInstruction);
							throw new Exception(defaultInterpolatedStringHandler3.ToStringAndClear());
						}
						emitter.Emit(code, (ICallSiteGenerator)operand);
					}
				}
				else if (codeInstruction.IsAnnotation() == null)
				{
					emitter.Emit(code);
				}
				IEnumerable<ExceptionBlock> blocks2 = codeInstruction.blocks;
				Action<ExceptionBlock> action3;
				if ((action3 = <>9__3) == null)
				{
					action3 = (<>9__3 = delegate(ExceptionBlock block)
					{
						emitter.MarkBlockAfter(block);
					});
				}
				blocks2.Do(action3);
			});
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000A758 File Offset: 0x00008958
		private static List<CodeInstruction> InitializeOutParameter(int argIndex, Type type)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>();
			if (type.IsByRef)
			{
				type = type.GetElementType();
			}
			codes.Add(Code.Ldarg[argIndex, null]);
			if (AccessTools.IsStruct(type))
			{
				codes.Add(Code.Initobj[type, null]);
				return codes;
			}
			if (!AccessTools.IsValue(type))
			{
				codes.Add(Code.Ldnull);
				codes.Add(Code.Stind_Ref);
				return codes;
			}
			if (type == typeof(float))
			{
				codes.Add(Code.Ldc_R4[0f, null]);
				codes.Add(Code.Stind_R4);
				return codes;
			}
			if (type == typeof(double))
			{
				codes.Add(Code.Ldc_R8[0.0, null]);
				codes.Add(Code.Stind_R8);
				return codes;
			}
			if (type == typeof(long))
			{
				codes.Add(Code.Ldc_I8[0L, null]);
				codes.Add(Code.Stind_I8);
				return codes;
			}
			codes.Add(Code.Ldc_I4[0, null]);
			codes.Add(Code.Stind_I4);
			return codes;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000A8A4 File Offset: 0x00008AA4
		private static CodeInstruction LoadIndOpCodeFor(Type type)
		{
			if (MethodCreatorTools.PrimitivesWithObjectTypeCode.Contains(type))
			{
				return Code.Ldind_I;
			}
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Empty:
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.String:
				return Code.Ldind_Ref;
			case TypeCode.Boolean:
			case TypeCode.SByte:
			case TypeCode.Byte:
				return Code.Ldind_I1;
			case TypeCode.Char:
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return Code.Ldind_I2;
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return Code.Ldind_I4;
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return Code.Ldind_I8;
			case TypeCode.Single:
				return Code.Ldind_R4;
			case TypeCode.Double:
				return Code.Ldind_R8;
			case TypeCode.Decimal:
			case TypeCode.DateTime:
				throw new NotSupportedException();
			}
			return Code.Ldind_Ref;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000A964 File Offset: 0x00008B64
		private static bool EmitOriginalBaseMethod(MethodBase original, List<CodeInstruction> codes)
		{
			MethodInfo method = original as MethodInfo;
			if (method != null)
			{
				codes.Add(Code.Ldtoken[method, null]);
			}
			else
			{
				ConstructorInfo constructor = original as ConstructorInfo;
				if (constructor == null)
				{
					return false;
				}
				codes.Add(Code.Ldtoken[constructor, null]);
			}
			Type type = original.ReflectedType;
			if (type.IsGenericType)
			{
				codes.Add(Code.Ldtoken[type, null]);
			}
			codes.Add(Code.Call[type.IsGenericType ? MethodCreatorTools.m_GetMethodFromHandle2 : MethodCreatorTools.m_GetMethodFromHandle1, null]);
			return true;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000A9F8 File Offset: 0x00008BF8
		private static CodeInstruction StoreIndOpCodeFor(Type type)
		{
			if (MethodCreatorTools.PrimitivesWithObjectTypeCode.Contains(type))
			{
				return Code.Stind_I;
			}
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Empty:
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.String:
				return Code.Stind_Ref;
			case TypeCode.Boolean:
			case TypeCode.SByte:
			case TypeCode.Byte:
				return Code.Stind_I1;
			case TypeCode.Char:
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return Code.Stind_I2;
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return Code.Stind_I4;
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return Code.Stind_I8;
			case TypeCode.Single:
				return Code.Stind_R4;
			case TypeCode.Double:
				return Code.Stind_R8;
			case TypeCode.Decimal:
			case TypeCode.DateTime:
				throw new NotSupportedException();
			}
			return Code.Stind_Ref;
		}

		// Token: 0x040000D4 RID: 212
		internal const string PARAM_INDEX_PREFIX = "__";

		// Token: 0x040000D5 RID: 213
		private const string INSTANCE_FIELD_PREFIX = "___";

		// Token: 0x040000D6 RID: 214
		private static readonly Dictionary<System.Reflection.Emit.OpCode, System.Reflection.Emit.OpCode> shortJumps = new Dictionary<System.Reflection.Emit.OpCode, System.Reflection.Emit.OpCode>
		{
			{
				System.Reflection.Emit.OpCodes.Leave_S,
				System.Reflection.Emit.OpCodes.Leave
			},
			{
				System.Reflection.Emit.OpCodes.Brfalse_S,
				System.Reflection.Emit.OpCodes.Brfalse
			},
			{
				System.Reflection.Emit.OpCodes.Brtrue_S,
				System.Reflection.Emit.OpCodes.Brtrue
			},
			{
				System.Reflection.Emit.OpCodes.Beq_S,
				System.Reflection.Emit.OpCodes.Beq
			},
			{
				System.Reflection.Emit.OpCodes.Bge_S,
				System.Reflection.Emit.OpCodes.Bge
			},
			{
				System.Reflection.Emit.OpCodes.Bgt_S,
				System.Reflection.Emit.OpCodes.Bgt
			},
			{
				System.Reflection.Emit.OpCodes.Ble_S,
				System.Reflection.Emit.OpCodes.Ble
			},
			{
				System.Reflection.Emit.OpCodes.Blt_S,
				System.Reflection.Emit.OpCodes.Blt
			},
			{
				System.Reflection.Emit.OpCodes.Bne_Un_S,
				System.Reflection.Emit.OpCodes.Bne_Un
			},
			{
				System.Reflection.Emit.OpCodes.Bge_Un_S,
				System.Reflection.Emit.OpCodes.Bge_Un
			},
			{
				System.Reflection.Emit.OpCodes.Bgt_Un_S,
				System.Reflection.Emit.OpCodes.Bgt_Un
			},
			{
				System.Reflection.Emit.OpCodes.Ble_Un_S,
				System.Reflection.Emit.OpCodes.Ble_Un
			},
			{
				System.Reflection.Emit.OpCodes.Br_S,
				System.Reflection.Emit.OpCodes.Br
			},
			{
				System.Reflection.Emit.OpCodes.Blt_Un_S,
				System.Reflection.Emit.OpCodes.Blt_Un
			}
		};

		// Token: 0x040000D7 RID: 215
		private static readonly MethodInfo m_GetMethodFromHandle1 = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle) });

		// Token: 0x040000D8 RID: 216
		private static readonly MethodInfo m_GetMethodFromHandle2 = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[]
		{
			typeof(RuntimeMethodHandle),
			typeof(RuntimeTypeHandle)
		});

		// Token: 0x040000D9 RID: 217
		private static readonly HashSet<Type> PrimitivesWithObjectTypeCode = new HashSet<Type>
		{
			typeof(IntPtr),
			typeof(UIntPtr),
			typeof(IntPtr),
			typeof(UIntPtr)
		};

		// Token: 0x0200003D RID: 61
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x040000DA RID: 218
			public static Func<char, bool> <0>__IsDigit;

			// Token: 0x040000DB RID: 219
			public static Action<VariableDefinition> <1>__LogIL;
		}
	}
}
