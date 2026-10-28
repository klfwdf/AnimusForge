using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class Emitter : IEmitter
{
	private readonly ILGenerator generator;

	private readonly Type[] parameterTypes;

	private readonly Stack<Type> stack = new Stack<Type>();

	private readonly List<LocalBuilder> variables = new List<LocalBuilder>();

	private readonly List<Instruction> instructions = new List<Instruction>();

	public Type StackType => (stack.Count == 0) ? null : stack.Peek();

	public List<Instruction> Instructions => instructions;

	public Emitter(ILGenerator generator, Type[] parameterTypes)
	{
		this.generator = generator;
		this.parameterTypes = parameterTypes;
	}

	public void Emit(OpCode code)
	{
		if (code == OpCodes.Ldarg_0)
		{
			stack.Push(parameterTypes[0]);
		}
		else if (code == OpCodes.Ldarg_1)
		{
			stack.Push(parameterTypes[1]);
		}
		else if (code == OpCodes.Ldarg_2)
		{
			stack.Push(parameterTypes[2]);
		}
		else if (code == OpCodes.Ldarg_3)
		{
			stack.Push(parameterTypes[3]);
		}
		else if (code == OpCodes.Ldloc_0)
		{
			stack.Push(variables[0].LocalType);
		}
		else if (code == OpCodes.Ldloc_1)
		{
			stack.Push(variables[1].LocalType);
		}
		else if (code == OpCodes.Ldloc_2)
		{
			stack.Push(variables[2].LocalType);
		}
		else if (code == OpCodes.Ldloc_3)
		{
			stack.Push(variables[3].LocalType);
		}
		else if (code == OpCodes.Stloc_0)
		{
			stack.Pop();
		}
		else if (code == OpCodes.Stloc_1)
		{
			stack.Pop();
		}
		else if (code == OpCodes.Stloc_2)
		{
			stack.Pop();
		}
		else if (code == OpCodes.Stloc_3)
		{
			stack.Pop();
		}
		else if (code == OpCodes.Ldelem_Ref)
		{
			stack.Pop();
			Type type = stack.Pop();
			stack.Push(type.GetElementType());
		}
		else if (code == OpCodes.Ldlen)
		{
			stack.Pop();
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Conv_I4)
		{
			stack.Pop();
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_0)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_1)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_2)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_3)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_4)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_5)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_6)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_7)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldc_I4_8)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Sub)
		{
			stack.Pop();
			stack.Pop();
			stack.Push(typeof(int));
		}
		else if (!(code == OpCodes.Ret))
		{
			if (!(code == OpCodes.Ldnull))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Push(null);
		}
		instructions.Add(new Instruction(code, delegate(ILGenerator il)
		{
			il.Emit(code);
		}));
		if (!(code == OpCodes.Ret))
		{
			return;
		}
		foreach (Instruction instruction in instructions)
		{
			instruction.Emit(generator);
		}
	}

	public void Emit(OpCode code, int arg)
	{
		if (code == OpCodes.Ldc_I4)
		{
			stack.Push(typeof(int));
		}
		else if (code == OpCodes.Ldarg)
		{
			stack.Push(parameterTypes[arg]);
		}
		else if (code == OpCodes.Ldloc)
		{
			stack.Push(variables[arg].LocalType);
		}
		else if (code == OpCodes.Ldloca)
		{
			stack.Push(variables[arg].LocalType.MakePointerType());
		}
		else
		{
			if (!(code == OpCodes.Stloc))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Pop();
		}
		instructions.Add(new Instruction<int>(code, arg, delegate(ILGenerator il)
		{
			il.Emit(code, arg);
		}));
	}

	public void Emit(OpCode code, long arg)
	{
		if (code == OpCodes.Ldc_I8)
		{
			stack.Push(typeof(long));
			instructions.Add(new Instruction<long>(code, arg, delegate(ILGenerator il)
			{
				il.Emit(code, arg);
			}));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, sbyte arg)
	{
		if (code == OpCodes.Ldc_I4_S)
		{
			stack.Push(typeof(int));
			instructions.Add(new Instruction<int>(code, arg, delegate(ILGenerator il)
			{
				il.Emit(code, arg);
			}));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, byte arg)
	{
		if (code == OpCodes.Ldloc_S)
		{
			stack.Push(variables[arg].LocalType);
		}
		else if (code == OpCodes.Ldarg_S)
		{
			stack.Push(parameterTypes[arg]);
		}
		else
		{
			if (!(code == OpCodes.Stloc_S))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Pop();
		}
		instructions.Add(new Instruction<byte>(code, arg, delegate(ILGenerator il)
		{
			il.Emit(code, arg);
		}));
	}

	public void Emit(OpCode code, Type type)
	{
		if (code == OpCodes.Newarr)
		{
			stack.Pop();
			stack.Push(type.MakeArrayType());
		}
		else if (code == OpCodes.Stelem)
		{
			stack.Pop();
			stack.Pop();
			stack.Pop();
		}
		else if (code == OpCodes.Castclass)
		{
			stack.Pop();
			stack.Push(type);
		}
		else if (code == OpCodes.Box)
		{
			stack.Pop();
			stack.Push(typeof(object));
		}
		else if (code == OpCodes.Unbox_Any)
		{
			stack.Pop();
			stack.Push(type);
		}
		else
		{
			if (!(code == OpCodes.Initobj))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Pop();
		}
		instructions.Add(new Instruction<Type>(code, type, delegate(ILGenerator il)
		{
			il.Emit(code, type);
		}));
	}

	public void Emit(OpCode code, ConstructorInfo constructor)
	{
		if (code == OpCodes.Newobj)
		{
			int num = constructor.GetParameters().Length;
			for (int i = 0; i < num; i++)
			{
				stack.Pop();
			}
			stack.Push(constructor.DeclaringType);
			instructions.Add(new Instruction<ConstructorInfo>(code, constructor, delegate(ILGenerator il)
			{
				il.Emit(code, constructor);
			}));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, LocalBuilder localBuilder)
	{
		if (code == OpCodes.Stloc)
		{
			stack.Pop();
		}
		else
		{
			if (!(code == OpCodes.Ldloc))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Push(localBuilder.LocalType);
		}
		instructions.Add(new Instruction<LocalBuilder>(code, localBuilder, delegate(ILGenerator il)
		{
			il.Emit(code, localBuilder);
		}));
	}

	public void Emit(OpCode code, MethodInfo methodInfo)
	{
		if (code == OpCodes.Callvirt || code == OpCodes.Call)
		{
			int num = methodInfo.GetParameters().Length;
			for (int i = 0; i < num; i++)
			{
				stack.Pop();
			}
			if (!methodInfo.IsStatic)
			{
				stack.Pop();
			}
			if (methodInfo.ReturnType != typeof(void))
			{
				stack.Push(methodInfo.ReturnType);
			}
			instructions.Add(new Instruction<MethodInfo>(code, methodInfo, delegate(ILGenerator il)
			{
				il.Emit(code, methodInfo);
			}));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public LocalBuilder DeclareLocal(Type type)
	{
		LocalBuilder localBuilder = generator.DeclareLocal(type);
		variables.Add(localBuilder);
		return localBuilder;
	}

	public void Emit(OpCode code, string arg)
	{
		if (code == OpCodes.Ldstr)
		{
			stack.Push(typeof(string));
			instructions.Add(new Instruction<string>(code, arg, delegate(ILGenerator il)
			{
				il.Emit(code, arg);
			}));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void PushConstantValue(object arg, Type type)
	{
		stack.Push(type);
		instructions.Add(new Instruction<object>(OpCodes.Nop, arg, delegate(ILGenerator il)
		{
			il.PushConstantValue(arg, type);
		}));
	}
}
