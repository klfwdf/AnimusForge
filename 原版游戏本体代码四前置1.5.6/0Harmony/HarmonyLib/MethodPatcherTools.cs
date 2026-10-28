using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Cecil;
using MonoMod.Utils;

namespace HarmonyLib;

internal class MethodPatcherTools
{
	internal const string INSTANCE_PARAM = "__instance";

	internal const string ORIGINAL_METHOD_PARAM = "__originalMethod";

	internal const string ARGS_ARRAY_VAR = "__args";

	internal const string RESULT_VAR = "__result";

	internal const string RESULT_REF_VAR = "__resultRef";

	internal const string STATE_VAR = "__state";

	internal const string EXCEPTION_VAR = "__exception";

	internal const string RUN_ORIGINAL_VAR = "__runOriginal";

	internal const string PARAM_INDEX_PREFIX = "__";

	internal const string INSTANCE_FIELD_PREFIX = "___";

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

	internal static DynamicMethodDefinition CreateDynamicMethod(MethodBase original, string suffix, bool debug)
	{
		if ((object)original == null)
		{
			throw new ArgumentNullException("original");
		}
		string text = (original.DeclaringType?.FullName ?? "GLOBALTYPE") + "." + original.Name + suffix;
		text = text.Replace("<>", "");
		ParameterInfo[] parameters = original.GetParameters();
		List<Type> list = new List<Type>();
		list.AddRange(parameters.Types());
		if (!original.IsStatic)
		{
			if (AccessTools.IsStruct(original.DeclaringType))
			{
				list.Insert(0, original.DeclaringType.MakeByRefType());
			}
			else
			{
				list.Insert(0, original.DeclaringType);
			}
		}
		Type returnedType = AccessTools.GetReturnedType(original);
		DynamicMethodDefinition dynamicMethodDefinition = new DynamicMethodDefinition(text, returnedType, list.ToArray());
		int num = ((!original.IsStatic) ? 1 : 0);
		if (!original.IsStatic)
		{
			dynamicMethodDefinition.Definition.Parameters[0].Name = "this";
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			ParameterDefinition parameterDefinition = dynamicMethodDefinition.Definition.Parameters[i + num];
			parameterDefinition.Attributes = (Mono.Cecil.ParameterAttributes)parameters[i].Attributes;
			parameterDefinition.Name = parameters[i].Name;
		}
		if (debug)
		{
			List<string> list2 = list.Select((Type p) => p.FullDescription()).ToList();
			if (list.Count == dynamicMethodDefinition.Definition.Parameters.Count)
			{
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					List<string> list3 = list2;
					int index = num2;
					list3[index] = list3[index] + " " + dynamicMethodDefinition.Definition.Parameters[num2].Name;
				}
			}
			FileLog.Log($"### Replacement: static {returnedType.FullDescription()} {original.DeclaringType?.FullName ?? "GLOBALTYPE"}::{text}({list2.Join()})");
		}
		return dynamicMethodDefinition;
	}

	internal static IEnumerable<(ParameterInfo info, string realName)> OriginalParameters(MethodInfo method)
	{
		IEnumerable<HarmonyArgument> baseArgs = method.GetArgumentAttributes();
		if ((object)method.DeclaringType != null)
		{
			baseArgs = baseArgs.Union(method.DeclaringType.GetArgumentAttributes()).OfType<HarmonyArgument>();
		}
		return method.GetParameters().Select(delegate(ParameterInfo p)
		{
			HarmonyArgument argumentAttribute = p.GetArgumentAttribute();
			return (argumentAttribute != null) ? (p: p, argumentAttribute.OriginalName ?? p.Name) : (p: p, baseArgs.GetRealName(p.Name, null) ?? p.Name);
		});
	}

	internal static Dictionary<string, string> RealNames(MethodInfo method)
	{
		return OriginalParameters(method).ToDictionary(((ParameterInfo info, string realName) pair) => pair.info.Name, ((ParameterInfo info, string realName) pair) => pair.realName);
	}

	internal static LocalBuilder[] DeclareOriginalLocalVariables(ILGenerator il, MethodBase member)
	{
		IList<LocalVariableInfo> list = member.GetMethodBody()?.LocalVariables;
		if (list == null)
		{
			return Array.Empty<LocalBuilder>();
		}
		return list.Select((LocalVariableInfo lvi) => il.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray();
	}

	internal static bool PrefixAffectsOriginal(MethodInfo fix)
	{
		if (fix.ReturnType == typeof(bool))
		{
			return true;
		}
		return OriginalParameters(fix).Any(delegate((ParameterInfo info, string realName) pair)
		{
			ParameterInfo item = pair.info;
			string item2 = pair.realName;
			Type parameterType = item.ParameterType;
			switch (item2)
			{
			case "__instance":
				return false;
			case "__originalMethod":
				return false;
			case "__state":
				return false;
			default:
				if (item.IsOut || item.IsRetval)
				{
					return true;
				}
				if (parameterType.IsByRef)
				{
					return true;
				}
				if (!AccessTools.IsValue(parameterType) && !AccessTools.IsStruct(parameterType))
				{
					return true;
				}
				return false;
			}
		});
	}

	internal static bool EmitOriginalBaseMethod(MethodBase original, Emitter emitter)
	{
		if (original is MethodInfo meth)
		{
			emitter.Emit(OpCodes.Ldtoken, meth);
		}
		else
		{
			if (!(original is ConstructorInfo con))
			{
				return false;
			}
			emitter.Emit(OpCodes.Ldtoken, con);
		}
		Type reflectedType = original.ReflectedType;
		if (reflectedType.IsGenericType)
		{
			emitter.Emit(OpCodes.Ldtoken, reflectedType);
		}
		emitter.Emit(OpCodes.Call, reflectedType.IsGenericType ? m_GetMethodFromHandle2 : m_GetMethodFromHandle1);
		return true;
	}

	internal static OpCode LoadIndOpCodeFor(Type type)
	{
		if (PrimitivesWithObjectTypeCode.Contains(type))
		{
			return OpCodes.Ldind_I;
		}
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Boolean:
		case TypeCode.SByte:
		case TypeCode.Byte:
			return OpCodes.Ldind_I1;
		case TypeCode.Char:
		case TypeCode.Int16:
		case TypeCode.UInt16:
			return OpCodes.Ldind_I2;
		case TypeCode.Int32:
		case TypeCode.UInt32:
			return OpCodes.Ldind_I4;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			return OpCodes.Ldind_I8;
		case TypeCode.Single:
			return OpCodes.Ldind_R4;
		case TypeCode.Double:
			return OpCodes.Ldind_R8;
		case TypeCode.Decimal:
		case TypeCode.DateTime:
			throw new NotSupportedException();
		case TypeCode.Empty:
		case TypeCode.Object:
		case TypeCode.DBNull:
		case TypeCode.String:
			return OpCodes.Ldind_Ref;
		default:
			return OpCodes.Ldind_Ref;
		}
	}

	internal static OpCode StoreIndOpCodeFor(Type type)
	{
		if (PrimitivesWithObjectTypeCode.Contains(type))
		{
			return OpCodes.Stind_I;
		}
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Boolean:
		case TypeCode.SByte:
		case TypeCode.Byte:
			return OpCodes.Stind_I1;
		case TypeCode.Char:
		case TypeCode.Int16:
		case TypeCode.UInt16:
			return OpCodes.Stind_I2;
		case TypeCode.Int32:
		case TypeCode.UInt32:
			return OpCodes.Stind_I4;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			return OpCodes.Stind_I8;
		case TypeCode.Single:
			return OpCodes.Stind_R4;
		case TypeCode.Double:
			return OpCodes.Stind_R8;
		case TypeCode.Decimal:
		case TypeCode.DateTime:
			throw new NotSupportedException();
		case TypeCode.Empty:
		case TypeCode.Object:
		case TypeCode.DBNull:
		case TypeCode.String:
			return OpCodes.Stind_Ref;
		default:
			return OpCodes.Stind_Ref;
		}
	}
}
