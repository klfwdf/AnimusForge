using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x02000043 RID: 67
	internal class MethodPatcherTools
	{
		// Token: 0x06000163 RID: 355 RVA: 0x0000B29C File Offset: 0x0000949C
		internal static DynamicMethodDefinition CreateDynamicMethod(MethodBase original, string suffix, bool debug)
		{
			if (original == null)
			{
				throw new ArgumentNullException("original");
			}
			Type declaringType = original.DeclaringType;
			string patchName = (((declaringType != null) ? declaringType.FullName : null) ?? "GLOBALTYPE") + "." + original.Name + suffix;
			patchName = patchName.Replace("<>", "");
			ParameterInfo[] parameters = original.GetParameters();
			List<Type> parameterTypes = new List<Type>();
			parameterTypes.AddRange(parameters.Types());
			if (!original.IsStatic)
			{
				if (AccessTools.IsStruct(original.DeclaringType))
				{
					parameterTypes.Insert(0, original.DeclaringType.MakeByRefType());
				}
				else
				{
					parameterTypes.Insert(0, original.DeclaringType);
				}
			}
			Type returnType = AccessTools.GetReturnedType(original);
			DynamicMethodDefinition method = new DynamicMethodDefinition(patchName, returnType, parameterTypes.ToArray());
			int offset = ((!original.IsStatic) ? 1 : 0);
			if (!original.IsStatic)
			{
				method.Definition.Parameters[0].Name = "this";
			}
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterDefinition param = method.Definition.Parameters[i + offset];
				param.Attributes = (Mono.Cecil.ParameterAttributes)parameters[i].Attributes;
				param.Name = parameters[i].Name;
			}
			if (debug)
			{
				List<string> parameterStrings = (from p in parameterTypes
					select p.FullDescription()).ToList<string>();
				if (parameterTypes.Count == method.Definition.Parameters.Count)
				{
					for (int j = 0; j < parameterTypes.Count; j++)
					{
						List<string> list = parameterStrings;
						int index = j;
						list[index] = list[index] + " " + method.Definition.Parameters[j].Name;
					}
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 4);
				defaultInterpolatedStringHandler.AppendLiteral("### Replacement: static ");
				defaultInterpolatedStringHandler.AppendFormatted(returnType.FullDescription());
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				Type declaringType2 = original.DeclaringType;
				defaultInterpolatedStringHandler.AppendFormatted(((declaringType2 != null) ? declaringType2.FullName : null) ?? "GLOBALTYPE");
				defaultInterpolatedStringHandler.AppendLiteral("::");
				defaultInterpolatedStringHandler.AppendFormatted(patchName);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(parameterStrings.Join(null, ", "));
				defaultInterpolatedStringHandler.AppendLiteral(")");
				FileLog.Log(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return method;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000B510 File Offset: 0x00009710
		[return: TupleElementNames(new string[] { "info", "realName" })]
		internal static IEnumerable<ValueTuple<ParameterInfo, string>> OriginalParameters(MethodInfo method)
		{
			IEnumerable<HarmonyArgument> baseArgs = method.GetArgumentAttributes();
			if (method.DeclaringType != null)
			{
				baseArgs = baseArgs.Union(method.DeclaringType.GetArgumentAttributes()).OfType<HarmonyArgument>();
			}
			return method.GetParameters().Select(delegate(ParameterInfo p)
			{
				HarmonyArgument arg = p.GetArgumentAttribute();
				if (arg != null)
				{
					return new ValueTuple<ParameterInfo, string>(p, arg.OriginalName ?? p.Name);
				}
				return new ValueTuple<ParameterInfo, string>(p, baseArgs.GetRealName(p.Name, null) ?? p.Name);
			});
		}

		// Token: 0x06000165 RID: 357 RVA: 0x0000B570 File Offset: 0x00009770
		internal static Dictionary<string, string> RealNames(MethodInfo method)
		{
			return MethodPatcherTools.OriginalParameters(method).ToDictionary(([TupleElementNames(new string[] { "info", "realName" })] ValueTuple<ParameterInfo, string> pair) => pair.Item1.Name, ([TupleElementNames(new string[] { "info", "realName" })] ValueTuple<ParameterInfo, string> pair) => pair.Item2);
		}

		// Token: 0x06000166 RID: 358 RVA: 0x0000B5C8 File Offset: 0x000097C8
		internal static LocalBuilder[] DeclareOriginalLocalVariables(ILGenerator il, MethodBase member)
		{
			MethodBody methodBody = member.GetMethodBody();
			IList<LocalVariableInfo> vars = ((methodBody != null) ? methodBody.LocalVariables : null);
			if (vars == null)
			{
				return Array.Empty<LocalBuilder>();
			}
			return (from lvi in vars
				select il.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray<LocalBuilder>();
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000B618 File Offset: 0x00009818
		internal static bool PrefixAffectsOriginal(MethodInfo fix)
		{
			if (fix.ReturnType == typeof(bool))
			{
				return true;
			}
			return MethodPatcherTools.OriginalParameters(fix).Any(delegate([TupleElementNames(new string[] { "info", "realName" })] ValueTuple<ParameterInfo, string> pair)
			{
				ParameterInfo p = pair.Item1;
				string name = pair.Item2;
				Type type = p.ParameterType;
				return !(name == "__instance") && !(name == "__originalMethod") && !(name == "__state") && (p.IsOut || p.IsRetval || type.IsByRef || (!AccessTools.IsValue(type) && !AccessTools.IsStruct(type)));
			});
		}

		// Token: 0x06000168 RID: 360 RVA: 0x0000B668 File Offset: 0x00009868
		internal static bool EmitOriginalBaseMethod(MethodBase original, Emitter emitter)
		{
			MethodInfo method = original as MethodInfo;
			if (method != null)
			{
				emitter.Emit(OpCodes.Ldtoken, method);
			}
			else
			{
				ConstructorInfo constructor = original as ConstructorInfo;
				if (constructor == null)
				{
					return false;
				}
				emitter.Emit(OpCodes.Ldtoken, constructor);
			}
			Type type = original.ReflectedType;
			if (type.IsGenericType)
			{
				emitter.Emit(OpCodes.Ldtoken, type);
			}
			emitter.Emit(OpCodes.Call, type.IsGenericType ? MethodPatcherTools.m_GetMethodFromHandle2 : MethodPatcherTools.m_GetMethodFromHandle1);
			return true;
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000B6E4 File Offset: 0x000098E4
		internal static OpCode LoadIndOpCodeFor(Type type)
		{
			if (MethodPatcherTools.PrimitivesWithObjectTypeCode.Contains(type))
			{
				return OpCodes.Ldind_I;
			}
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Empty:
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.String:
				return OpCodes.Ldind_Ref;
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
			}
			return OpCodes.Ldind_Ref;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x0000B7A4 File Offset: 0x000099A4
		internal static OpCode StoreIndOpCodeFor(Type type)
		{
			if (MethodPatcherTools.PrimitivesWithObjectTypeCode.Contains(type))
			{
				return OpCodes.Stind_I;
			}
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Empty:
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.String:
				return OpCodes.Stind_Ref;
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
			}
			return OpCodes.Stind_Ref;
		}

		// Token: 0x040000F3 RID: 243
		internal const string INSTANCE_PARAM = "__instance";

		// Token: 0x040000F4 RID: 244
		internal const string ORIGINAL_METHOD_PARAM = "__originalMethod";

		// Token: 0x040000F5 RID: 245
		internal const string ARGS_ARRAY_VAR = "__args";

		// Token: 0x040000F6 RID: 246
		internal const string RESULT_VAR = "__result";

		// Token: 0x040000F7 RID: 247
		internal const string RESULT_REF_VAR = "__resultRef";

		// Token: 0x040000F8 RID: 248
		internal const string STATE_VAR = "__state";

		// Token: 0x040000F9 RID: 249
		internal const string EXCEPTION_VAR = "__exception";

		// Token: 0x040000FA RID: 250
		internal const string RUN_ORIGINAL_VAR = "__runOriginal";

		// Token: 0x040000FB RID: 251
		internal const string PARAM_INDEX_PREFIX = "__";

		// Token: 0x040000FC RID: 252
		internal const string INSTANCE_FIELD_PREFIX = "___";

		// Token: 0x040000FD RID: 253
		private static readonly MethodInfo m_GetMethodFromHandle1 = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle) });

		// Token: 0x040000FE RID: 254
		private static readonly MethodInfo m_GetMethodFromHandle2 = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[]
		{
			typeof(RuntimeMethodHandle),
			typeof(RuntimeTypeHandle)
		});

		// Token: 0x040000FF RID: 255
		private static readonly HashSet<Type> PrimitivesWithObjectTypeCode = new HashSet<Type>
		{
			typeof(IntPtr),
			typeof(UIntPtr),
			typeof(IntPtr),
			typeof(UIntPtr)
		};
	}
}
