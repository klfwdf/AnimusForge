using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class EmitterExtensions
{
	public static void UnboxOrCast(this IEmitter emitter, Type type)
	{
		if (!(emitter.StackType == null) && (!(type == typeof(bool)) || !(emitter.StackType == typeof(int))) && (!(type == typeof(byte)) || !(emitter.StackType == typeof(int))) && (!(type == typeof(sbyte)) || !(emitter.StackType == typeof(int))) && (!(type == typeof(short)) || !(emitter.StackType == typeof(int))) && (!(type == typeof(ushort)) || !(emitter.StackType == typeof(int))) && (!(type == typeof(uint)) || !(emitter.StackType == typeof(int))) && (!(type == typeof(ulong)) || !(emitter.StackType == typeof(long))) && !type.GetTypeInfo().IsEnum && !type.GetTypeInfo().IsAssignableFrom(emitter.StackType.GetTypeInfo()))
		{
			emitter.Emit(type.GetTypeInfo().IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
		}
	}

	public static void PushConstant(this IEmitter emitter, int index, Type type)
	{
		emitter.PushConstant(index);
		emitter.UnboxOrCast(type);
	}

	public static void PushConstant(this IEmitter emitter, int index)
	{
		emitter.PushArgument(0);
		emitter.Push(index);
		emitter.PushArrayElement();
	}

	public static void PushArrayElement(this IEmitter emitter)
	{
		emitter.Emit(OpCodes.Ldelem_Ref);
	}

	public static void PushArguments(this IEmitter emitter, ParameterInfo[] parameters)
	{
		LocalBuilder localBuilder = emitter.DeclareLocal(typeof(object[]));
		emitter.Emit(OpCodes.Ldarg_0);
		emitter.Emit(OpCodes.Ldarg_0);
		emitter.Emit(OpCodes.Ldlen);
		emitter.Emit(OpCodes.Conv_I4);
		emitter.Emit(OpCodes.Ldc_I4_1);
		emitter.Emit(OpCodes.Sub);
		emitter.Emit(OpCodes.Ldelem_Ref);
		emitter.Emit(OpCodes.Castclass, typeof(object[]));
		emitter.Emit(OpCodes.Stloc, localBuilder);
		for (int i = 0; i < parameters.Length; i++)
		{
			emitter.Emit(OpCodes.Ldloc, localBuilder);
			emitter.Emit(OpCodes.Ldc_I4, i);
			emitter.Emit(OpCodes.Ldelem_Ref);
			emitter.Emit(parameters[i].ParameterType.GetTypeInfo().IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameters[i].ParameterType);
		}
	}

	public static void Call(this IEmitter emitter, MethodInfo methodInfo)
	{
		emitter.Emit(OpCodes.Callvirt, methodInfo);
	}

	public static void New(this IEmitter emitter, ConstructorInfo constructorInfo)
	{
		emitter.Emit(OpCodes.Newobj, constructorInfo);
	}

	public static void Push(this IEmitter emitter, LocalBuilder localBuilder)
	{
		int localIndex = localBuilder.LocalIndex;
		switch (localIndex)
		{
		case 0:
			emitter.Emit(OpCodes.Ldloc_0);
			return;
		case 1:
			emitter.Emit(OpCodes.Ldloc_1);
			return;
		case 2:
			emitter.Emit(OpCodes.Ldloc_2);
			return;
		case 3:
			emitter.Emit(OpCodes.Ldloc_3);
			return;
		}
		if (localIndex <= 255)
		{
			emitter.Emit(OpCodes.Ldloc_S, (byte)localIndex);
		}
		else
		{
			emitter.Emit(OpCodes.Ldloc, localIndex);
		}
	}

	public static void PushArgument(this IEmitter emitter, int index)
	{
		switch (index)
		{
		case 0:
			emitter.Emit(OpCodes.Ldarg_0);
			return;
		case 1:
			emitter.Emit(OpCodes.Ldarg_1);
			return;
		case 2:
			emitter.Emit(OpCodes.Ldarg_2);
			return;
		case 3:
			emitter.Emit(OpCodes.Ldarg_3);
			return;
		}
		if (index <= 255)
		{
			emitter.Emit(OpCodes.Ldarg_S, (byte)index);
		}
		else
		{
			emitter.Emit(OpCodes.Ldarg, index);
		}
	}

	public static void Store(this IEmitter emitter, LocalBuilder localBuilder)
	{
		int localIndex = localBuilder.LocalIndex;
		switch (localIndex)
		{
		case 0:
			emitter.Emit(OpCodes.Stloc_0);
			return;
		case 1:
			emitter.Emit(OpCodes.Stloc_1);
			return;
		case 2:
			emitter.Emit(OpCodes.Stloc_2);
			return;
		case 3:
			emitter.Emit(OpCodes.Stloc_3);
			return;
		}
		if (localIndex <= 255)
		{
			emitter.Emit(OpCodes.Stloc_S, (byte)localIndex);
		}
		else
		{
			emitter.Emit(OpCodes.Stloc, localIndex);
		}
	}

	public static void PushNewArray(this IEmitter emitter, Type elementType)
	{
		emitter.Emit(OpCodes.Newarr, elementType);
	}

	public static void Push(this IEmitter emitter, int value)
	{
		switch (value)
		{
		case 0:
			emitter.Emit(OpCodes.Ldc_I4_0);
			return;
		case 1:
			emitter.Emit(OpCodes.Ldc_I4_1);
			return;
		case 2:
			emitter.Emit(OpCodes.Ldc_I4_2);
			return;
		case 3:
			emitter.Emit(OpCodes.Ldc_I4_3);
			return;
		case 4:
			emitter.Emit(OpCodes.Ldc_I4_4);
			return;
		case 5:
			emitter.Emit(OpCodes.Ldc_I4_5);
			return;
		case 6:
			emitter.Emit(OpCodes.Ldc_I4_6);
			return;
		case 7:
			emitter.Emit(OpCodes.Ldc_I4_7);
			return;
		case 8:
			emitter.Emit(OpCodes.Ldc_I4_8);
			return;
		}
		if (value > -129 && value < 128)
		{
			emitter.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
		}
		else
		{
			emitter.Emit(OpCodes.Ldc_I4, value);
		}
	}

	public static void Cast(this IEmitter emitter, Type type)
	{
		emitter.Emit(OpCodes.Castclass, type);
	}

	public static void Return(this IEmitter emitter)
	{
		emitter.Emit(OpCodes.Ret);
	}
}
