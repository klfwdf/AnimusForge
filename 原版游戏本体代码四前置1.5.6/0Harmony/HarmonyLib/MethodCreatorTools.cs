using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;

namespace HarmonyLib;

internal static class MethodCreatorTools
{
	internal const string PARAM_INDEX_PREFIX = "__";

	private const string INSTANCE_FIELD_PREFIX = "___";

	private static readonly Dictionary<OpCode, OpCode> shortJumps = new Dictionary<OpCode, OpCode>
	{
		{
			OpCodes.Leave_S,
			OpCodes.Leave
		},
		{
			OpCodes.Brfalse_S,
			OpCodes.Brfalse
		},
		{
			OpCodes.Brtrue_S,
			OpCodes.Brtrue
		},
		{
			OpCodes.Beq_S,
			OpCodes.Beq
		},
		{
			OpCodes.Bge_S,
			OpCodes.Bge
		},
		{
			OpCodes.Bgt_S,
			OpCodes.Bgt
		},
		{
			OpCodes.Ble_S,
			OpCodes.Ble
		},
		{
			OpCodes.Blt_S,
			OpCodes.Blt
		},
		{
			OpCodes.Bne_Un_S,
			OpCodes.Bne_Un
		},
		{
			OpCodes.Bge_Un_S,
			OpCodes.Bge_Un
		},
		{
			OpCodes.Bgt_Un_S,
			OpCodes.Bgt_Un
		},
		{
			OpCodes.Ble_Un_S,
			OpCodes.Ble_Un
		},
		{
			OpCodes.Br_S,
			OpCodes.Br
		},
		{
			OpCodes.Blt_Un_S,
			OpCodes.Blt_Un
		}
	};

	private static readonly MethodInfo m_GetMethodFromHandle1 = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[1] { typeof(RuntimeMethodHandle) });

	private static readonly MethodInfo m_GetMethodFromHandle2 = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[2]
	{
		typeof(RuntimeMethodHandle),
		typeof(RuntimeTypeHandle)
	});

	private static readonly HashSet<Type> PrimitivesWithObjectTypeCode = new HashSet<Type>
	{
		typeof(IntPtr),
		typeof(UIntPtr),
		typeof(IntPtr),
		typeof(UIntPtr)
	};

	internal static List<CodeInstruction> GenerateVariableInit(this MethodCreator _, LocalBuilder variable, bool isReturnValue = false)
	{
		List<CodeInstruction> list = new List<CodeInstruction>();
		Type type = variable.LocalType;
		if (type.IsByRef)
		{
			if (isReturnValue)
			{
				list.Add(Code.Ldc_I4_1);
				list.Add(Code.Newarr[type.GetElementType(), null]);
				list.Add(Code.Ldc_I4_0);
				list.Add(Code.Ldelema[type.GetElementType(), null]);
				list.Add(Code.Stloc[variable, null]);
				return list;
			}
			type = type.GetElementType();
		}
		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}
		if (AccessTools.IsClass(type))
		{
			list.Add(Code.Ldnull);
			list.Add(Code.Stloc[variable, null]);
			return list;
		}
		if (AccessTools.IsStruct(type))
		{
			list.Add(Code.Ldloca[variable, null]);
			list.Add(Code.Initobj[type, null]);
			return list;
		}
		if (AccessTools.IsValue(type))
		{
			if (type == typeof(float))
			{
				list.Add(Code.Ldc_R4[0f, null]);
			}
			else if (type == typeof(double))
			{
				list.Add(Code.Ldc_R8[0.0, null]);
			}
			else if (type == typeof(long) || type == typeof(ulong))
			{
				list.Add(Code.Ldc_I8[0L, null]);
			}
			else
			{
				list.Add(Code.Ldc_I4[0, null]);
			}
			list.Add(Code.Stloc[variable, null]);
			return list;
		}
		return list;
	}

	internal static List<CodeInstruction> PrepareArgumentArray(this MethodCreator creator)
	{
		List<CodeInstruction> list = new List<CodeInstruction>();
		MethodBase original = creator.config.original;
		bool isStatic = original.IsStatic;
		ParameterInfo[] parameters = original.GetParameters();
		int num = 0;
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameterInfo in array)
		{
			int argIndex = num++ + ((!isStatic) ? 1 : 0);
			if (parameterInfo.IsOut || parameterInfo.IsRetval)
			{
				list.AddRange(InitializeOutParameter(argIndex, parameterInfo.ParameterType));
			}
		}
		list.Add(Code.Ldc_I4[parameters.Length, null]);
		list.Add(Code.Newarr[typeof(object), null]);
		num = 0;
		int num2 = 0;
		ParameterInfo[] array2 = parameters;
		foreach (ParameterInfo parameterInfo2 in array2)
		{
			int num3 = num++ + ((!isStatic) ? 1 : 0);
			Type type = parameterInfo2.ParameterType;
			bool isByRef = type.IsByRef;
			if (isByRef)
			{
				type = type.GetElementType();
			}
			list.Add(Code.Dup);
			list.Add(Code.Ldc_I4[num2++, null]);
			list.Add(Code.Ldarg[num3, null]);
			if (isByRef)
			{
				if (AccessTools.IsStruct(type))
				{
					list.Add(Code.Ldobj[type, null]);
				}
				else
				{
					list.Add(LoadIndOpCodeFor(type));
				}
			}
			if (type.IsValueType)
			{
				list.Add(Code.Box[type, null]);
			}
			list.Add(Code.Stelem_Ref);
		}
		return list;
	}

	internal static bool AffectsOriginal(this MethodCreator creator, MethodInfo fix)
	{
		if (fix.ReturnType == typeof(bool))
		{
			return true;
		}
		if (!creator.config.injections.TryGetValue(fix, out var value))
		{
			return false;
		}
		return value.Any(delegate(InjectedParameter parameter)
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
			ParameterInfo parameterInfo = parameter.parameterInfo;
			if (parameterInfo.IsOut || parameterInfo.IsRetval)
			{
				return true;
			}
			Type parameterType = parameterInfo.ParameterType;
			if (parameterType.IsByRef)
			{
				return true;
			}
			return (!AccessTools.IsValue(parameterType) && !AccessTools.IsStruct(parameterType)) ? true : false;
		});
	}

	internal static CodeInstruction MarkBlock(this MethodCreator _, ExceptionBlockType blockType)
	{
		return Code.Nop.WithBlocks(new ExceptionBlock(blockType));
	}

	internal static List<CodeInstruction> EmitCallParameter(this MethodCreator creator, MethodInfo patch, bool allowFirsParamPassthrough, out LocalBuilder tmpInstanceBoxingVar, out LocalBuilder tmpObjectVar, out bool refResultUsed, List<KeyValuePair<LocalBuilder, Type>> tmpBoxVars)
	{
		tmpInstanceBoxingVar = null;
		tmpObjectVar = null;
		refResultUsed = false;
		List<CodeInstruction> list = new List<CodeInstruction>();
		MethodCreatorConfig config = creator.config;
		MethodBase original = config.original;
		bool isStatic = original.IsStatic;
		Type returnType = config.returnType;
		List<InjectedParameter> list2 = config.injections[patch].ToList();
		bool flag = !isStatic;
		ParameterInfo[] parameters = original.GetParameters();
		string[] originalParameterNames = parameters.Select((ParameterInfo p) => p.Name).ToArray();
		Type declaringType = original.DeclaringType;
		List<ParameterInfo> list3 = patch.GetParameters().ToList();
		if (allowFirsParamPassthrough && patch.ReturnType != typeof(void) && list3.Count > 0 && list3[0].ParameterType == patch.ReturnType)
		{
			list2.RemoveAt(0);
			list3.RemoveAt(0);
		}
		foreach (InjectedParameter item in list2)
		{
			InjectionType injectionType = item.injectionType;
			string realName = item.realName;
			Type parameterType = item.parameterInfo.ParameterType;
			switch (injectionType)
			{
			case InjectionType.OriginalMethod:
				if (!EmitOriginalBaseMethod(original, list))
				{
					list.Add(Code.Ldnull);
				}
				continue;
			case InjectionType.Exception:
				if (config.exceptionVariable != null)
				{
					list.Add(Code.Ldloc[config.exceptionVariable, null]);
				}
				else
				{
					list.Add(Code.Ldnull);
				}
				continue;
			case InjectionType.RunOriginal:
				if (config.runOriginalVariable != null)
				{
					list.Add(Code.Ldloc[config.runOriginalVariable, null]);
				}
				else
				{
					list.Add(Code.Ldc_I4_0);
				}
				continue;
			case InjectionType.Instance:
			{
				if (isStatic)
				{
					list.Add(Code.Ldnull);
					continue;
				}
				bool isByRef = parameterType.IsByRef;
				bool flag2 = parameterType == typeof(object) || parameterType == typeof(object).MakeByRefType();
				if (AccessTools.IsStruct(declaringType))
				{
					if (flag2)
					{
						if (isByRef)
						{
							list.Add(Code.Ldarg_0);
							list.Add(Code.Ldobj[declaringType, null]);
							list.Add(Code.Box[declaringType, null]);
							tmpInstanceBoxingVar = config.DeclareLocal(typeof(object));
							list.Add(Code.Stloc[tmpInstanceBoxingVar, null]);
							list.Add(Code.Ldloca[tmpInstanceBoxingVar, null]);
						}
						else
						{
							list.Add(Code.Ldarg_0);
							list.Add(Code.Ldobj[declaringType, null]);
							list.Add(Code.Box[declaringType, null]);
						}
					}
					else if (isByRef)
					{
						list.Add(Code.Ldarg_0);
					}
					else
					{
						list.Add(Code.Ldarg_0);
						list.Add(Code.Ldobj[declaringType, null]);
					}
				}
				else if (isByRef)
				{
					list.Add(Code.Ldarga[0, null]);
				}
				else
				{
					list.Add(Code.Ldarg_0);
				}
				continue;
			}
			case InjectionType.ArgsArray:
			{
				if (config.localVariables.TryGetValue(InjectionType.ArgsArray, out var local))
				{
					list.Add(Code.Ldloc[local, null]);
				}
				else
				{
					list.Add(Code.Ldnull);
				}
				continue;
			}
			}
			if (realName.StartsWith("___", StringComparison.Ordinal))
			{
				string text = realName.Substring("___".Length);
				FieldInfo fieldInfo;
				if (text.All(char.IsDigit))
				{
					fieldInfo = AccessTools.DeclaredField(declaringType, int.Parse(text));
					if ((object)fieldInfo == null)
					{
						throw new ArgumentException("No field found at given index in class " + (declaringType?.AssemblyQualifiedName ?? "null"), text);
					}
				}
				else
				{
					fieldInfo = AccessTools.Field(declaringType, text);
					if ((object)fieldInfo == null)
					{
						throw new ArgumentException("No such field defined in class " + (declaringType?.AssemblyQualifiedName ?? "null"), text);
					}
				}
				if (fieldInfo.IsStatic)
				{
					list.Add(parameterType.IsByRef ? ((CodeMatch)Code.Ldsflda[fieldInfo, null]) : ((CodeMatch)Code.Ldsfld[fieldInfo, null]));
					continue;
				}
				list.Add(Code.Ldarg_0);
				list.Add(parameterType.IsByRef ? ((CodeMatch)Code.Ldflda[fieldInfo, null]) : ((CodeMatch)Code.Ldfld[fieldInfo, null]));
				continue;
			}
			switch (injectionType)
			{
			case InjectionType.State:
			{
				OpCode opcode2 = (parameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc);
				if (config.localVariables.TryGetValue(patch.DeclaringType?.AssemblyQualifiedName ?? "null", out var local2))
				{
					list.Add(new CodeInstruction(opcode2, local2));
				}
				else
				{
					list.Add(Code.Ldnull);
				}
				continue;
			}
			case InjectionType.Result:
			{
				if (returnType == typeof(void))
				{
					throw new Exception("Cannot get result from void method " + original.FullDescription());
				}
				Type type = parameterType;
				if (type.IsByRef && !returnType.IsByRef)
				{
					type = type.GetElementType();
				}
				if (!type.IsAssignableFrom(returnType))
				{
					throw new Exception($"Cannot assign method return type {returnType.FullName} to {"__result"} type {type.FullName} for method {original.FullDescription()}");
				}
				OpCode opcode = ((parameterType.IsByRef && !returnType.IsByRef) ? OpCodes.Ldloca : OpCodes.Ldloc);
				if (returnType.IsValueType && parameterType == typeof(object).MakeByRefType())
				{
					opcode = OpCodes.Ldloc;
				}
				list.Add(new CodeInstruction(opcode, config.GetLocal(InjectionType.Result)));
				if (returnType.IsValueType)
				{
					if (parameterType == typeof(object))
					{
						list.Add(Code.Box[returnType, null]);
					}
					else if (parameterType == typeof(object).MakeByRefType())
					{
						list.Add(Code.Box[returnType, null]);
						tmpObjectVar = config.DeclareLocal(typeof(object));
						list.Add(Code.Stloc[tmpObjectVar, null]);
						list.Add(Code.Ldloca[tmpObjectVar, null]);
					}
				}
				continue;
			}
			case InjectionType.ResultRef:
			{
				if (!returnType.IsByRef)
				{
					throw new Exception($"Cannot use {InjectionType.ResultRef} with non-ref return type {returnType.FullName} of method {original.FullDescription()}");
				}
				Type type2 = parameterType;
				Type type3 = typeof(RefResult<>).MakeGenericType(returnType.GetElementType()).MakeByRefType();
				if (type2 != type3)
				{
					throw new Exception($"Wrong type of {"__resultRef"} for method {original.FullDescription()}. Expected {type3.FullName}, got {type2.FullName}");
				}
				list.Add(Code.Ldloca[config.GetLocal(InjectionType.ResultRef), null]);
				refResultUsed = true;
				continue;
			}
			}
			if (config.localVariables.TryGetValue(realName, out var local3))
			{
				OpCode opcode3 = (parameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc);
				list.Add(new CodeInstruction(opcode3, local3));
				continue;
			}
			int result;
			if (realName.StartsWith("__", StringComparison.Ordinal))
			{
				string s = realName.Substring("__".Length);
				if (!int.TryParse(s, out result))
				{
					throw new Exception("Parameter " + realName + " does not contain a valid index");
				}
				if (result < 0 || result >= parameters.Length)
				{
					throw new Exception($"No parameter found at index {result}");
				}
			}
			else
			{
				result = patch.GetArgumentIndex(originalParameterNames, item.parameterInfo);
				if (result == -1)
				{
					HarmonyMethod mergedFromType = HarmonyMethodExtensions.GetMergedFromType(parameterType);
					HarmonyMethod harmonyMethod = mergedFromType;
					MethodType valueOrDefault = harmonyMethod.methodType.GetValueOrDefault();
					if (!harmonyMethod.methodType.HasValue)
					{
						valueOrDefault = MethodType.Normal;
						harmonyMethod.methodType = valueOrDefault;
					}
					MethodBase originalMethod = mergedFromType.GetOriginalMethod();
					if (originalMethod is MethodInfo methodInfo)
					{
						ConstructorInfo constructor = parameterType.GetConstructor(new Type[2]
						{
							typeof(object),
							typeof(IntPtr)
						});
						if ((object)constructor != null)
						{
							if (methodInfo.IsStatic)
							{
								list.Add(Code.Ldnull);
							}
							else
							{
								list.Add(Code.Ldarg_0);
								if (declaringType != null && declaringType.IsValueType)
								{
									list.Add(Code.Ldobj[declaringType, null]);
									list.Add(Code.Box[declaringType, null]);
								}
							}
							if (!methodInfo.IsStatic && !mergedFromType.nonVirtualDelegate)
							{
								list.Add(Code.Dup);
								list.Add(Code.Ldvirtftn[methodInfo, null]);
							}
							else
							{
								list.Add(Code.Ldftn[methodInfo, null]);
							}
							list.Add(Code.Newobj[constructor, null]);
							continue;
						}
					}
					throw new Exception("Parameter \"" + realName + "\" not found in method " + original.FullDescription());
				}
			}
			Type parameterType2 = parameters[result].ParameterType;
			Type type4 = (parameterType2.IsByRef ? parameterType2.GetElementType() : parameterType2);
			Type type5 = parameterType;
			Type type6 = (type5.IsByRef ? type5.GetElementType() : type5);
			bool flag3 = !parameters[result].IsOut && !parameterType2.IsByRef;
			bool flag4 = !item.parameterInfo.IsOut && !type5.IsByRef;
			bool flag5 = type4.IsValueType && !type6.IsValueType;
			int num = result + (flag ? 1 : 0);
			if (flag3 == flag4)
			{
				list.Add(Code.Ldarg[num, null]);
				if (flag5)
				{
					if (flag4)
					{
						list.Add(Code.Box[type4, null]);
						continue;
					}
					list.Add(Code.Ldobj[type4, null]);
					list.Add(Code.Box[type4, null]);
					LocalBuilder localBuilder = config.DeclareLocal(type6);
					list.Add(Code.Stloc[localBuilder, null]);
					list.Add(Code.Ldloca_S[localBuilder, null]);
					tmpBoxVars.Add(new KeyValuePair<LocalBuilder, Type>(localBuilder, type4));
				}
			}
			else if (flag3 && !flag4)
			{
				if (flag5)
				{
					list.Add(Code.Ldarg[num, null]);
					list.Add(Code.Box[type4, null]);
					LocalBuilder operand = config.DeclareLocal(type6);
					list.Add(Code.Stloc[operand, null]);
					list.Add(Code.Ldloca_S[operand, null]);
				}
				else
				{
					list.Add(Code.Ldarga[num, null]);
				}
			}
			else
			{
				list.Add(Code.Ldarg[num, null]);
				if (flag5)
				{
					list.Add(Code.Ldobj[type4, null]);
					list.Add(Code.Box[type4, null]);
				}
				else if (type4.IsValueType)
				{
					list.Add(Code.Ldobj[type4, null]);
				}
				else
				{
					list.Add(new CodeInstruction(LoadIndOpCodeFor(parameters[result].ParameterType)));
				}
			}
		}
		return list;
	}

	internal static LocalBuilder[] DeclareOriginalLocalVariables(this MethodCreator creator, MethodBase member)
	{
		IList<LocalVariableInfo> list = member.GetMethodBody()?.LocalVariables;
		if (list == null)
		{
			return Array.Empty<LocalBuilder>();
		}
		return list.Select((LocalVariableInfo lvi) => creator.config.il.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray();
	}

	internal static List<CodeInstruction> RestoreArgumentArray(this MethodCreator creator)
	{
		List<CodeInstruction> list = new List<CodeInstruction>();
		MethodBase original = creator.config.original;
		bool isStatic = original.IsStatic;
		ParameterInfo[] parameters = original.GetParameters();
		int num = 0;
		int num2 = 0;
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameterInfo in array)
		{
			int num3 = num++ + ((!isStatic) ? 1 : 0);
			Type parameterType = parameterInfo.ParameterType;
			if (parameterType.IsByRef)
			{
				parameterType = parameterType.GetElementType();
				list.Add(Code.Ldarg[num3, null]);
				list.Add(Code.Ldloc[creator.config.GetLocal(InjectionType.ArgsArray), null]);
				list.Add(Code.Ldc_I4[num2, null]);
				list.Add(Code.Ldelem_Ref);
				if (parameterType.IsValueType)
				{
					list.Add(Code.Unbox_Any[parameterType, null]);
					if (AccessTools.IsStruct(parameterType))
					{
						list.Add(Code.Stobj[parameterType, null]);
					}
					else
					{
						list.Add(StoreIndOpCodeFor(parameterType));
					}
				}
				else
				{
					list.Add(Code.Castclass[parameterType, null]);
					list.Add(Code.Stind_Ref);
				}
			}
			else
			{
				list.Add(Code.Ldloc[creator.config.GetLocal(InjectionType.ArgsArray), null]);
				list.Add(Code.Ldc_I4[num2, null]);
				list.Add(Code.Ldelem_Ref);
				if (parameterType.IsValueType)
				{
					list.Add(Code.Unbox_Any[parameterType, null]);
				}
				else
				{
					list.Add(Code.Castclass[parameterType, null]);
				}
				list.Add(Code.Starg[num3, null]);
			}
			num2++;
		}
		return list;
	}

	internal static IEnumerable<CodeInstruction> CleanupCodes(this MethodCreator creator, IEnumerable<CodeInstruction> instructions, List<Label> endLabels)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			OpCode opcode = instruction.opcode;
			OpCode value;
			if (opcode == OpCodes.Ret)
			{
				Label endLabel = creator.config.DefineLabel();
				yield return Code.Br[endLabel, null].WithLabels(instruction.labels).WithBlocks(instruction.blocks);
				endLabels.Add(endLabel);
			}
			else if (shortJumps.TryGetValue(opcode, out value))
			{
				yield return new CodeInstruction(value, instruction.operand).WithLabels(instruction.labels).WithBlocks(instruction.blocks);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	internal static void LogCodes(this MethodCreator _, Emitter emitter, List<CodeInstruction> codeInstructions)
	{
		int codePos = emitter.CurrentPos();
		emitter.Variables().Do(FileLog.LogIL);
		codeInstructions.Do(delegate(CodeInstruction codeInstruction)
		{
			codeInstruction.labels.Do(delegate(Label label)
			{
				FileLog.LogIL(codePos, label);
			});
			codeInstruction.blocks.Do(delegate(ExceptionBlock block)
			{
				FileLog.LogILBlockBegin(codePos, block);
			});
			OpCode opcode = codeInstruction.opcode;
			object operand = codeInstruction.operand;
			bool flag = true;
			switch (opcode.OperandType)
			{
			case OperandType.InlineNone:
			{
				string text = codeInstruction.IsAnnotation();
				if (text != null)
				{
					FileLog.LogILComment(codePos, text);
					flag = false;
				}
				else
				{
					FileLog.LogIL(codePos, opcode);
				}
				break;
			}
			case OperandType.InlineSig:
				FileLog.LogIL(codePos, opcode, (ICallSiteGenerator)operand);
				break;
			default:
				FileLog.LogIL(codePos, opcode, operand);
				break;
			}
			codeInstruction.blocks.Do(delegate(ExceptionBlock block)
			{
				FileLog.LogILBlockEnd(codePos, block);
			});
			if (flag)
			{
				codePos += codeInstruction.GetSize();
			}
		});
		FileLog.FlushBuffer();
	}

	internal static void EmitCodes(this MethodCreator _, Emitter emitter, List<CodeInstruction> codeInstructions)
	{
		codeInstructions.Do(delegate(CodeInstruction codeInstruction)
		{
			codeInstruction.labels.Do(delegate(Label label)
			{
				emitter.MarkLabel(label);
			});
			codeInstruction.blocks.Do(delegate(ExceptionBlock block)
			{
				emitter.MarkBlockBefore(block, out var _);
			});
			OpCode opcode = codeInstruction.opcode;
			object operand = codeInstruction.operand;
			switch (opcode.OperandType)
			{
			case OperandType.InlineNone:
				if (codeInstruction.IsAnnotation() == null)
				{
					emitter.Emit(opcode);
				}
				break;
			case OperandType.InlineSig:
				if (operand == null)
				{
					throw new Exception($"Wrong null argument: {codeInstruction}");
				}
				if (!(operand is ICallSiteGenerator))
				{
					throw new Exception($"Wrong Emit argument type {operand.GetType()} in {codeInstruction}");
				}
				emitter.Emit(opcode, (ICallSiteGenerator)operand);
				break;
			default:
				if (operand == null)
				{
					throw new Exception($"Wrong null argument: {codeInstruction}");
				}
				emitter.DynEmit(opcode, operand);
				break;
			}
			codeInstruction.blocks.Do(delegate(ExceptionBlock block)
			{
				emitter.MarkBlockAfter(block);
			});
		});
	}

	private static List<CodeInstruction> InitializeOutParameter(int argIndex, Type type)
	{
		List<CodeInstruction> list = new List<CodeInstruction>();
		if (type.IsByRef)
		{
			type = type.GetElementType();
		}
		list.Add(Code.Ldarg[argIndex, null]);
		if (AccessTools.IsStruct(type))
		{
			list.Add(Code.Initobj[type, null]);
			return list;
		}
		if (AccessTools.IsValue(type))
		{
			if (type == typeof(float))
			{
				list.Add(Code.Ldc_R4[0f, null]);
				list.Add(Code.Stind_R4);
				return list;
			}
			if (type == typeof(double))
			{
				list.Add(Code.Ldc_R8[0.0, null]);
				list.Add(Code.Stind_R8);
				return list;
			}
			if (type == typeof(long))
			{
				list.Add(Code.Ldc_I8[0L, null]);
				list.Add(Code.Stind_I8);
				return list;
			}
			list.Add(Code.Ldc_I4[0, null]);
			list.Add(Code.Stind_I4);
			return list;
		}
		list.Add(Code.Ldnull);
		list.Add(Code.Stind_Ref);
		return list;
	}

	private static CodeInstruction LoadIndOpCodeFor(Type type)
	{
		if (PrimitivesWithObjectTypeCode.Contains(type))
		{
			return Code.Ldind_I;
		}
		switch (Type.GetTypeCode(type))
		{
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
		case TypeCode.Empty:
		case TypeCode.Object:
		case TypeCode.DBNull:
		case TypeCode.String:
			return Code.Ldind_Ref;
		default:
			return Code.Ldind_Ref;
		}
	}

	private static bool EmitOriginalBaseMethod(MethodBase original, List<CodeInstruction> codes)
	{
		if (original is MethodInfo operand)
		{
			codes.Add(Code.Ldtoken[operand, null]);
		}
		else
		{
			if (!(original is ConstructorInfo operand2))
			{
				return false;
			}
			codes.Add(Code.Ldtoken[operand2, null]);
		}
		Type reflectedType = original.ReflectedType;
		if (reflectedType.IsGenericType)
		{
			codes.Add(Code.Ldtoken[reflectedType, null]);
		}
		codes.Add(Code.Call[reflectedType.IsGenericType ? m_GetMethodFromHandle2 : m_GetMethodFromHandle1, null]);
		return true;
	}

	private static CodeInstruction StoreIndOpCodeFor(Type type)
	{
		if (PrimitivesWithObjectTypeCode.Contains(type))
		{
			return Code.Stind_I;
		}
		switch (Type.GetTypeCode(type))
		{
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
		case TypeCode.Empty:
		case TypeCode.Object:
		case TypeCode.DBNull:
		case TypeCode.String:
			return Code.Stind_Ref;
		default:
			return Code.Stind_Ref;
		}
	}
}
