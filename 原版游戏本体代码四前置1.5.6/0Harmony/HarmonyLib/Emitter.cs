using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using MonoMod.Utils.Cil;

namespace HarmonyLib;

internal class Emitter
{
	private readonly ILGenerator iLGenerator;

	private readonly CecilILGenerator il;

	private readonly Dictionary<int, CodeInstruction> instructions = new Dictionary<int, CodeInstruction>();

	internal Emitter(ILGenerator il)
	{
		iLGenerator = il;
		this.il = il.GetProxiedShim<CecilILGenerator>();
	}

	internal Dictionary<int, CodeInstruction> GetInstructions()
	{
		return instructions;
	}

	internal void AddInstruction(System.Reflection.Emit.OpCode opcode, object operand = null)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, operand));
	}

	internal int CurrentPos()
	{
		return il.ILOffset;
	}

	internal static string CodePos(int offset)
	{
		return $"IL_{offset:X4}: ";
	}

	internal string CodePos()
	{
		return CodePos(CurrentPos());
	}

	internal IEnumerable<VariableDefinition> Variables()
	{
		return il.IL.Body.Variables;
	}

	internal static string FormatOperand(object argument)
	{
		if (argument == null)
		{
			return "NULL";
		}
		Type type = argument.GetType();
		if (argument is MethodBase member)
		{
			return member.FullDescription();
		}
		if (argument is FieldInfo fieldInfo)
		{
			return $"{fieldInfo.FieldType.FullDescription()} {fieldInfo.DeclaringType.FullDescription()}::{fieldInfo.Name}";
		}
		if (type == typeof(Label))
		{
			return $"Label{((Label)argument/*cast due to constrained. prefix*/).GetHashCode()}";
		}
		if (type == typeof(Label[]))
		{
			return "Labels" + string.Join(",", ((Label[])argument).Select((Label l) => l.GetHashCode().ToString()).ToArray());
		}
		if (type == typeof(LocalBuilder))
		{
			return $"{((LocalBuilder)argument).LocalIndex} ({((LocalBuilder)argument).LocalType})";
		}
		if (type == typeof(string))
		{
			return argument.ToString().ToLiteral();
		}
		return argument.ToString().Trim();
	}

	internal LocalBuilder DeclareLocalVariable(Type type, bool isReturnValue = false)
	{
		if (type.IsByRef)
		{
			if (isReturnValue)
			{
				LocalBuilder localBuilder = il.DeclareLocal(type);
				Emit(System.Reflection.Emit.OpCodes.Ldc_I4_1);
				Emit(System.Reflection.Emit.OpCodes.Newarr, type.GetElementType());
				Emit(System.Reflection.Emit.OpCodes.Ldc_I4_0);
				Emit(System.Reflection.Emit.OpCodes.Ldelema, type.GetElementType());
				Emit(System.Reflection.Emit.OpCodes.Stloc, localBuilder);
				return localBuilder;
			}
			type = type.GetElementType();
		}
		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}
		if (AccessTools.IsClass(type))
		{
			LocalBuilder localBuilder2 = il.DeclareLocal(type);
			Emit(System.Reflection.Emit.OpCodes.Ldnull);
			Emit(System.Reflection.Emit.OpCodes.Stloc, localBuilder2);
			return localBuilder2;
		}
		if (AccessTools.IsStruct(type))
		{
			LocalBuilder localBuilder3 = il.DeclareLocal(type);
			Emit(System.Reflection.Emit.OpCodes.Ldloca, localBuilder3);
			Emit(System.Reflection.Emit.OpCodes.Initobj, type);
			return localBuilder3;
		}
		if (AccessTools.IsValue(type))
		{
			LocalBuilder localBuilder4 = il.DeclareLocal(type);
			if (type == typeof(float))
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_R4, 0f);
			}
			else if (type == typeof(double))
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_R8, 0.0);
			}
			else if (type == typeof(long) || type == typeof(ulong))
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_I8, 0L);
			}
			else
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_I4, 0);
			}
			Emit(System.Reflection.Emit.OpCodes.Stloc, localBuilder4);
			return localBuilder4;
		}
		return null;
	}

	internal void InitializeOutParameter(int argIndex, Type type)
	{
		if (type.IsByRef)
		{
			type = type.GetElementType();
		}
		Emit(System.Reflection.Emit.OpCodes.Ldarg, argIndex);
		if (AccessTools.IsStruct(type))
		{
			Emit(System.Reflection.Emit.OpCodes.Initobj, type);
		}
		else if (AccessTools.IsValue(type))
		{
			if (type == typeof(float))
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_R4, 0f);
				Emit(System.Reflection.Emit.OpCodes.Stind_R4);
			}
			else if (type == typeof(double))
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_R8, 0.0);
				Emit(System.Reflection.Emit.OpCodes.Stind_R8);
			}
			else if (type == typeof(long))
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_I8, 0L);
				Emit(System.Reflection.Emit.OpCodes.Stind_I8);
			}
			else
			{
				Emit(System.Reflection.Emit.OpCodes.Ldc_I4, 0);
				Emit(System.Reflection.Emit.OpCodes.Stind_I4);
			}
		}
		else
		{
			Emit(System.Reflection.Emit.OpCodes.Ldnull);
			Emit(System.Reflection.Emit.OpCodes.Stind_Ref);
		}
	}

	internal void PrepareArgumentArray(MethodBase original)
	{
		ParameterInfo[] parameters = original.GetParameters();
		int num = 0;
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameterInfo in array)
		{
			int argIndex = num++ + ((!original.IsStatic) ? 1 : 0);
			if (parameterInfo.IsOut || parameterInfo.IsRetval)
			{
				InitializeOutParameter(argIndex, parameterInfo.ParameterType);
			}
		}
		Emit(System.Reflection.Emit.OpCodes.Ldc_I4, parameters.Length);
		Emit(System.Reflection.Emit.OpCodes.Newarr, typeof(object));
		num = 0;
		int num2 = 0;
		ParameterInfo[] array2 = parameters;
		foreach (ParameterInfo parameterInfo2 in array2)
		{
			int arg = num++ + ((!original.IsStatic) ? 1 : 0);
			Type type = parameterInfo2.ParameterType;
			bool isByRef = type.IsByRef;
			if (isByRef)
			{
				type = type.GetElementType();
			}
			Emit(System.Reflection.Emit.OpCodes.Dup);
			Emit(System.Reflection.Emit.OpCodes.Ldc_I4, num2++);
			Emit(System.Reflection.Emit.OpCodes.Ldarg, arg);
			if (isByRef)
			{
				if (AccessTools.IsStruct(type))
				{
					Emit(System.Reflection.Emit.OpCodes.Ldobj, type);
				}
				else
				{
					Emit(MethodPatcherTools.LoadIndOpCodeFor(type));
				}
			}
			if (type.IsValueType)
			{
				Emit(System.Reflection.Emit.OpCodes.Box, type);
			}
			Emit(System.Reflection.Emit.OpCodes.Stelem_Ref);
		}
	}

	internal void RestoreArgumentArray(MethodBase original, LocalBuilderState localState)
	{
		ParameterInfo[] parameters = original.GetParameters();
		int num = 0;
		int num2 = 0;
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameterInfo in array)
		{
			int arg = num++ + ((!original.IsStatic) ? 1 : 0);
			Type parameterType = parameterInfo.ParameterType;
			if (parameterType.IsByRef)
			{
				parameterType = parameterType.GetElementType();
				Emit(System.Reflection.Emit.OpCodes.Ldarg, arg);
				Emit(System.Reflection.Emit.OpCodes.Ldloc, localState["__args"]);
				Emit(System.Reflection.Emit.OpCodes.Ldc_I4, num2);
				Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
				if (parameterType.IsValueType)
				{
					Emit(System.Reflection.Emit.OpCodes.Unbox_Any, parameterType);
					if (AccessTools.IsStruct(parameterType))
					{
						Emit(System.Reflection.Emit.OpCodes.Stobj, parameterType);
					}
					else
					{
						Emit(MethodPatcherTools.StoreIndOpCodeFor(parameterType));
					}
				}
				else
				{
					Emit(System.Reflection.Emit.OpCodes.Castclass, parameterType);
					Emit(System.Reflection.Emit.OpCodes.Stind_Ref);
				}
			}
			else
			{
				Emit(System.Reflection.Emit.OpCodes.Ldloc, localState["__args"]);
				Emit(System.Reflection.Emit.OpCodes.Ldc_I4, num2);
				Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
				if (parameterType.IsValueType)
				{
					Emit(System.Reflection.Emit.OpCodes.Unbox_Any, parameterType);
				}
				else
				{
					Emit(System.Reflection.Emit.OpCodes.Castclass, parameterType);
				}
				Emit(System.Reflection.Emit.OpCodes.Starg, arg);
			}
			num2++;
		}
	}

	internal void MarkLabel(Label label)
	{
		il.MarkLabel(label);
	}

	internal void MarkBlockBefore(ExceptionBlock block, out Label? label)
	{
		label = null;
		switch (block.blockType)
		{
		case ExceptionBlockType.BeginExceptionBlock:
			label = il.BeginExceptionBlock();
			break;
		case ExceptionBlockType.BeginCatchBlock:
			il.BeginCatchBlock(block.catchType);
			break;
		case ExceptionBlockType.BeginExceptFilterBlock:
			il.BeginExceptFilterBlock();
			break;
		case ExceptionBlockType.BeginFaultBlock:
			il.BeginFaultBlock();
			break;
		case ExceptionBlockType.BeginFinallyBlock:
			il.BeginFinallyBlock();
			break;
		}
	}

	internal void MarkBlockAfter(ExceptionBlock block)
	{
		ExceptionBlockType blockType = block.blockType;
		if (blockType == ExceptionBlockType.EndExceptionBlock)
		{
			il.EndExceptionBlock();
		}
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode));
		il.Emit(opcode);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, LocalBuilder local)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, local));
		il.Emit(opcode, local);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, FieldInfo field)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, field));
		il.Emit(opcode, field);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, Label[] labels)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, labels));
		il.Emit(opcode, labels);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, Label label)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, label));
		il.Emit(opcode, label);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, string str)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, str));
		il.Emit(opcode, str);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, float arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, byte arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, sbyte arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, double arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, int arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, MethodInfo meth)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, meth));
		il.Emit(opcode, meth);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, short arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, SignatureHelper signature)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, signature));
		il.Emit(opcode, signature);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, ConstructorInfo con)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, con));
		il.Emit(opcode, con);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, Type cls)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, cls));
		il.Emit(opcode, cls);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, long arg)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, arg));
		il.Emit(opcode, arg);
	}

	internal void Emit(System.Reflection.Emit.OpCode opcode, ICallSiteGenerator operand)
	{
		il.Emit(opcode, operand);
	}

	internal void EmitCall(System.Reflection.Emit.OpCode opcode, MethodInfo methodInfo)
	{
		instructions.Add(CurrentPos(), new CodeInstruction(opcode, methodInfo));
		il.EmitCall(opcode, methodInfo, null);
	}

	internal void DynEmit(System.Reflection.Emit.OpCode opcode, object operand)
	{
		iLGenerator.DynEmit(opcode, operand);
	}
}
