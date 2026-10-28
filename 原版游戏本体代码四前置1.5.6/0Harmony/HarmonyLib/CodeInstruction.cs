using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;

namespace HarmonyLib;

public class CodeInstruction
{
	internal static class State
	{
		internal static readonly List<Delegate> closureCache = new List<Delegate>();
	}

	public OpCode opcode;

	public object operand;

	public List<Label> labels = new List<Label>();

	public List<ExceptionBlock> blocks = new List<ExceptionBlock>();

	internal CodeInstruction()
	{
	}

	internal static CodeInstruction Annotation(string annotation)
	{
		return new CodeInstruction(OpCodes.Nop, annotation);
	}

	internal string IsAnnotation()
	{
		if (!(opcode == OpCodes.Nop))
		{
			return null;
		}
		return operand as string;
	}

	public CodeInstruction(OpCode opcode, object operand = null)
	{
		this.opcode = opcode;
		this.operand = operand;
	}

	public CodeInstruction(CodeInstruction instruction)
	{
		opcode = instruction.opcode;
		operand = instruction.operand;
		labels = instruction.labels.ToList();
		blocks = instruction.blocks.ToList();
	}

	public CodeInstruction Clone()
	{
		return new CodeInstruction(this)
		{
			labels = new List<Label>(),
			blocks = new List<ExceptionBlock>()
		};
	}

	public CodeInstruction Clone(OpCode opcode)
	{
		CodeInstruction codeInstruction = Clone();
		codeInstruction.opcode = opcode;
		return codeInstruction;
	}

	public CodeInstruction Clone(object operand)
	{
		CodeInstruction codeInstruction = Clone();
		codeInstruction.operand = operand;
		return codeInstruction;
	}

	public static CodeInstruction Call(Type type, string name, Type[] parameters = null, Type[] generics = null)
	{
		MethodInfo methodInfo = AccessTools.Method(type, name, parameters, generics);
		if ((object)methodInfo == null)
		{
			throw new ArgumentException($"No method found for type={type}, name={name}, parameters={parameters.Description()}, generics={generics.Description()}");
		}
		return new CodeInstruction(OpCodes.Call, methodInfo);
	}

	public static CodeInstruction Call(string typeColonMethodname, Type[] parameters = null, Type[] generics = null)
	{
		MethodInfo methodInfo = AccessTools.Method(typeColonMethodname, parameters, generics);
		if ((object)methodInfo == null)
		{
			throw new ArgumentException($"No method found for {typeColonMethodname}, parameters={parameters.Description()}, generics={generics.Description()}");
		}
		return new CodeInstruction(OpCodes.Call, methodInfo);
	}

	public static CodeInstruction Call(Expression<Action> expression)
	{
		return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));
	}

	public static CodeInstruction Call<T>(Expression<Action<T>> expression)
	{
		return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));
	}

	public static CodeInstruction Call<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));
	}

	public static CodeInstruction Call(LambdaExpression expression)
	{
		return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));
	}

	public static CodeInstruction CallClosure<T>(T closure) where T : Delegate
	{
		if (closure.Method.IsStatic && closure.Target == null)
		{
			return new CodeInstruction(OpCodes.Call, closure.Method);
		}
		Type[] array = (from x in closure.Method.GetParameters()
			select x.ParameterType).ToArray();
		DynamicMethodDefinition dynamicMethodDefinition = new DynamicMethodDefinition(closure.Method.Name, closure.Method.ReturnType, array);
		ILGenerator iLGenerator = dynamicMethodDefinition.GetILGenerator();
		Type type = closure.Target.GetType();
		if (closure.Target != null && type.GetFields().Any((FieldInfo x) => !x.IsStatic))
		{
			State.closureCache.Add(closure);
			iLGenerator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(State), "closureCache"));
			iLGenerator.Emit(OpCodes.Ldc_I4, State.closureCache.Count - 1);
			iLGenerator.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<Delegate>), "Item"));
		}
		else
		{
			if (closure.Target == null)
			{
				iLGenerator.Emit(OpCodes.Ldnull);
			}
			else
			{
				iLGenerator.Emit(OpCodes.Newobj, AccessTools.FirstConstructor(type, (ConstructorInfo x) => !x.IsStatic && x.GetParameters().Length == 0));
			}
			iLGenerator.Emit(OpCodes.Ldftn, closure.Method);
			iLGenerator.Emit(OpCodes.Newobj, AccessTools.Constructor(typeof(T), new Type[2]
			{
				typeof(object),
				typeof(IntPtr)
			}));
		}
		for (int num = 0; num < array.Length; num++)
		{
			iLGenerator.Emit(OpCodes.Ldarg, num);
		}
		iLGenerator.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(T), "Invoke"));
		iLGenerator.Emit(OpCodes.Ret);
		return new CodeInstruction(OpCodes.Call, dynamicMethodDefinition.Generate());
	}

	public static CodeInstruction LoadField(Type type, string name, bool useAddress = false)
	{
		FieldInfo fieldInfo = AccessTools.Field(type, name);
		if ((object)fieldInfo == null)
		{
			throw new ArgumentException($"No field found for {type} and {name}");
		}
		return new CodeInstruction((!useAddress) ? (fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld) : (fieldInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda), fieldInfo);
	}

	public static CodeInstruction StoreField(Type type, string name)
	{
		FieldInfo fieldInfo = AccessTools.Field(type, name);
		if ((object)fieldInfo == null)
		{
			throw new ArgumentException($"No field found for {type} and {name}");
		}
		return new CodeInstruction(fieldInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
	}

	public static CodeInstruction LoadLocal(int index, bool useAddress = false)
	{
		if (useAddress)
		{
			if (index < 256)
			{
				return new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(index));
			}
			return new CodeInstruction(OpCodes.Ldloca, index);
		}
		if (index == 0)
		{
			return new CodeInstruction(OpCodes.Ldloc_0);
		}
		if (index == 1)
		{
			return new CodeInstruction(OpCodes.Ldloc_1);
		}
		if (index == 2)
		{
			return new CodeInstruction(OpCodes.Ldloc_2);
		}
		if (index == 3)
		{
			return new CodeInstruction(OpCodes.Ldloc_3);
		}
		if (index < 256)
		{
			return new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(index));
		}
		return new CodeInstruction(OpCodes.Ldloc, index);
	}

	public static CodeInstruction StoreLocal(int index)
	{
		if (index == 0)
		{
			return new CodeInstruction(OpCodes.Stloc_0);
		}
		if (index == 1)
		{
			return new CodeInstruction(OpCodes.Stloc_1);
		}
		if (index == 2)
		{
			return new CodeInstruction(OpCodes.Stloc_2);
		}
		if (index == 3)
		{
			return new CodeInstruction(OpCodes.Stloc_3);
		}
		if (index < 256)
		{
			return new CodeInstruction(OpCodes.Stloc_S, Convert.ToByte(index));
		}
		return new CodeInstruction(OpCodes.Stloc, index);
	}

	public static CodeInstruction LoadArgument(int index, bool useAddress = false)
	{
		if (useAddress)
		{
			if (index < 256)
			{
				return new CodeInstruction(OpCodes.Ldarga_S, Convert.ToByte(index));
			}
			return new CodeInstruction(OpCodes.Ldarga, index);
		}
		if (index == 0)
		{
			return new CodeInstruction(OpCodes.Ldarg_0);
		}
		if (index == 1)
		{
			return new CodeInstruction(OpCodes.Ldarg_1);
		}
		if (index == 2)
		{
			return new CodeInstruction(OpCodes.Ldarg_2);
		}
		if (index == 3)
		{
			return new CodeInstruction(OpCodes.Ldarg_3);
		}
		if (index < 256)
		{
			return new CodeInstruction(OpCodes.Ldarg_S, Convert.ToByte(index));
		}
		return new CodeInstruction(OpCodes.Ldarg, index);
	}

	public static CodeInstruction StoreArgument(int index)
	{
		if (index < 256)
		{
			return new CodeInstruction(OpCodes.Starg_S, Convert.ToByte(index));
		}
		return new CodeInstruction(OpCodes.Starg, index);
	}

	public bool HasBlock(ExceptionBlockType type)
	{
		return blocks?.Any((ExceptionBlock block) => block.blockType == type) ?? false;
	}

	public override string ToString()
	{
		List<string> list = new List<string>();
		foreach (Label label in labels)
		{
			list.Add($"Label{label.GetHashCode()}");
		}
		foreach (ExceptionBlock block in blocks)
		{
			list.Add("EX_" + block.blockType.ToString().Replace("Block", ""));
		}
		string text = ((list.Count > 0) ? (" [" + string.Join(", ", list.ToArray()) + "]") : "");
		string text2 = Emitter.FormatOperand(operand);
		if (text2.Length > 0)
		{
			text2 = " " + text2;
		}
		OpCode opCode = opcode;
		return opCode.ToString() + text2 + text;
	}
}
