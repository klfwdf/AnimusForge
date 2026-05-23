using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x020008BB RID: 2235
	[NullableContext(1)]
	[Nullable(0)]
	internal static class FastReflectionHelper
	{
		// Token: 0x06002E5F RID: 11871 RVA: 0x0009F004 File Offset: 0x0009D204
		[NullableContext(2)]
		private static object FastInvokerForStructInvokerVT<[Nullable(0)] T>([Nullable(1)] FastReflectionHelper.FastStructInvoker invoker, object target, params object[] args) where T : struct
		{
			object result = default(T);
			invoker(target, result, args);
			return result;
		}

		// Token: 0x06002E60 RID: 11872 RVA: 0x0009F02C File Offset: 0x0009D22C
		[NullableContext(2)]
		private static object FastInvokerForStructInvokerNullable<[Nullable(0)] T>([Nullable(1)] FastReflectionHelper.FastStructInvoker invoker, object target, params object[] args) where T : struct
		{
			StrongBox<T?> strongBox;
			if ((strongBox = FastReflectionHelper.TypedCache<T>.NullableStrongBox) == null)
			{
				strongBox = (FastReflectionHelper.TypedCache<T>.NullableStrongBox = new StrongBox<T?>(null));
			}
			StrongBox<T?> result = strongBox;
			invoker(target, result, args);
			return result.Value;
		}

		// Token: 0x06002E61 RID: 11873 RVA: 0x0009F06C File Offset: 0x0009D26C
		[NullableContext(2)]
		private static object FastInvokerForStructInvokerClass([Nullable(1)] FastReflectionHelper.FastStructInvoker invoker, object target, params object[] args)
		{
			WeakBox weakBox;
			if ((weakBox = FastReflectionHelper.CachedWeakBox) == null)
			{
				weakBox = (FastReflectionHelper.CachedWeakBox = new WeakBox());
			}
			WeakBox result = weakBox;
			invoker(target, result, args);
			return result.Value;
		}

		// Token: 0x06002E62 RID: 11874 RVA: 0x0009F09D File Offset: 0x0009D29D
		[NullableContext(2)]
		private static object FastInvokerForStructInvokerVoid([Nullable(1)] FastReflectionHelper.FastStructInvoker invoker, object target, params object[] args)
		{
			invoker(target, null, args);
			return null;
		}

		// Token: 0x06002E63 RID: 11875 RVA: 0x0009F0AC File Offset: 0x0009D2AC
		private static FastReflectionHelper.FastInvoker CreateFastInvoker(FastReflectionHelper.FastStructInvoker fsi, FastReflectionHelper.ReturnTypeClass retTypeClass, Type returnType)
		{
			switch (retTypeClass)
			{
			case FastReflectionHelper.ReturnTypeClass.Void:
				return FastReflectionHelper.S2FVoid.CreateDelegate(fsi);
			case FastReflectionHelper.ReturnTypeClass.ValueType:
				return FastReflectionHelper.S2FValueType.MakeGenericMethod(new Type[] { returnType }).CreateDelegate(fsi);
			case FastReflectionHelper.ReturnTypeClass.Nullable:
				return FastReflectionHelper.S2FNullable.MakeGenericMethod(new Type[] { Nullable.GetUnderlyingType(returnType) }).CreateDelegate(fsi);
			case FastReflectionHelper.ReturnTypeClass.ReferenceType:
				return FastReflectionHelper.S2FClass.CreateDelegate(fsi);
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid ReturnTypeClass ");
				defaultInterpolatedStringHandler.AppendFormatted<FastReflectionHelper.ReturnTypeClass>(retTypeClass);
				throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
		}

		// Token: 0x06002E64 RID: 11876 RVA: 0x0009F150 File Offset: 0x0009D350
		private static FastReflectionHelper.FSITuple GetFSITuple(MethodBase method)
		{
			return FastReflectionHelper.fastStructInvokers.GetValue(method, delegate(MemberInfo _)
			{
				FastReflectionHelper.ReturnTypeClass rtc;
				Type rt;
				return new FastReflectionHelper.FSITuple(FastReflectionHelper.CreateMethodInvoker(method, out rtc, out rt), rtc, rt);
			});
		}

		// Token: 0x06002E65 RID: 11877 RVA: 0x0009F188 File Offset: 0x0009D388
		private static FastReflectionHelper.FSITuple GetFSITuple(FieldInfo field)
		{
			return FastReflectionHelper.fastStructInvokers.GetValue(field, delegate(MemberInfo _)
			{
				FastReflectionHelper.ReturnTypeClass rtc;
				Type rt;
				return new FastReflectionHelper.FSITuple(FastReflectionHelper.CreateFieldInvoker(field, out rtc, out rt), rtc, rt);
			});
		}

		// Token: 0x06002E66 RID: 11878 RVA: 0x0009F1C0 File Offset: 0x0009D3C0
		private static FastReflectionHelper.FSITuple GetFSITuple(MemberInfo member)
		{
			MethodBase mb = member as MethodBase;
			FastReflectionHelper.FSITuple fsituple;
			if (mb == null)
			{
				FieldInfo fi = member as FieldInfo;
				if (fi == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Member type ");
					defaultInterpolatedStringHandler.AppendFormatted<Type>(member.GetType());
					defaultInterpolatedStringHandler.AppendLiteral(" is not supported");
					throw new NotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				fsituple = FastReflectionHelper.GetFSITuple(fi);
			}
			else
			{
				fsituple = FastReflectionHelper.GetFSITuple(mb);
			}
			return fsituple;
		}

		// Token: 0x06002E67 RID: 11879 RVA: 0x0009F232 File Offset: 0x0009D432
		private static FastReflectionHelper.FastInvoker GetFastInvoker(FastReflectionHelper.FSITuple tuple)
		{
			return FastReflectionHelper.fastInvokers.GetValue(tuple, (FastReflectionHelper.FSITuple t) => FastReflectionHelper.CreateFastInvoker(t.FSI, t.RTC, t.ReturnType));
		}

		// Token: 0x06002E68 RID: 11880 RVA: 0x0009F25E File Offset: 0x0009D45E
		public static FastReflectionHelper.FastStructInvoker GetFastStructInvoker(MethodBase method)
		{
			return FastReflectionHelper.GetFSITuple(method).FSI;
		}

		// Token: 0x06002E69 RID: 11881 RVA: 0x0009F26B File Offset: 0x0009D46B
		public static FastReflectionHelper.FastStructInvoker GetFastStructInvoker(FieldInfo field)
		{
			return FastReflectionHelper.GetFSITuple(field).FSI;
		}

		// Token: 0x06002E6A RID: 11882 RVA: 0x0009F278 File Offset: 0x0009D478
		public static FastReflectionHelper.FastStructInvoker GetFastStructInvoker(MemberInfo member)
		{
			return FastReflectionHelper.GetFSITuple(member).FSI;
		}

		// Token: 0x06002E6B RID: 11883 RVA: 0x0009F285 File Offset: 0x0009D485
		public static FastReflectionHelper.FastInvoker GetFastInvoker(this MethodBase method)
		{
			return FastReflectionHelper.GetFastInvoker(FastReflectionHelper.GetFSITuple(method));
		}

		// Token: 0x06002E6C RID: 11884 RVA: 0x0009F292 File Offset: 0x0009D492
		public static FastReflectionHelper.FastInvoker GetFastInvoker(this FieldInfo field)
		{
			return FastReflectionHelper.GetFastInvoker(FastReflectionHelper.GetFSITuple(field));
		}

		// Token: 0x06002E6D RID: 11885 RVA: 0x0009F29F File Offset: 0x0009D49F
		public static FastReflectionHelper.FastInvoker GetFastInvoker(this MemberInfo member)
		{
			return FastReflectionHelper.GetFastInvoker(FastReflectionHelper.GetFSITuple(member));
		}

		// Token: 0x06002E6E RID: 11886 RVA: 0x0009F2AC File Offset: 0x0009D4AC
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CheckArgs(bool isStatic, object target, int retTypeClass, object result, int expectLen, object[] args)
		{
			if (!isStatic)
			{
				Helpers.ThrowIfArgumentNull<object>(target, "target");
			}
			if (retTypeClass != 0 && retTypeClass - 1 <= 2)
			{
				Helpers.ThrowIfArgumentNull<object>(result, "result");
			}
			if (expectLen == 0)
			{
				return;
			}
			Helpers.ThrowIfArgumentNull<object[]>(args, "args");
			if (args.Length < expectLen)
			{
				FastReflectionHelper.<CheckArgs>g__ThrowArgumentOutOfRange|28_0();
			}
		}

		// Token: 0x06002E6F RID: 11887 RVA: 0x0009F2FC File Offset: 0x0009D4FC
		[NullableContext(2)]
		[return: Nullable(1)]
		private static Exception BadArgException(int arg, RuntimeTypeHandle expectType, object target, object result, [Nullable(new byte[] { 1, 2 })] object[] args)
		{
			Type expectedType = Type.GetTypeFromHandle(expectType);
			Type type;
			if (arg != -2)
			{
				if (arg == -1)
				{
					type = ((target != null) ? target.GetType() : null);
				}
				else
				{
					object obj = args[arg];
					type = ((obj != null) ? obj.GetType() : null);
				}
			}
			else
			{
				type = ((result != null) ? result.GetType() : null);
			}
			Type realType = type;
			string text;
			if (arg != -2)
			{
				if (arg == -1)
				{
					text = "target";
				}
				else
				{
					FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(6, 1);
					formatInterpolatedStringHandler.AppendLiteral("args[");
					formatInterpolatedStringHandler.AppendFormatted<int>(arg);
					formatInterpolatedStringHandler.AppendLiteral("]");
					text = DebugFormatter.Format(ref formatInterpolatedStringHandler);
				}
			}
			else
			{
				text = "result";
			}
			string argName = text;
			if (realType == null)
			{
				return new ArgumentNullException(argName);
			}
			if (arg != -2)
			{
				if (arg == -1)
				{
					FormatInterpolatedStringHandler formatInterpolatedStringHandler2 = new FormatInterpolatedStringHandler(48, 2);
					formatInterpolatedStringHandler2.AppendLiteral("Target object is the wrong type; expected ");
					formatInterpolatedStringHandler2.AppendFormatted<Type>(expectedType);
					formatInterpolatedStringHandler2.AppendLiteral(", got ");
					formatInterpolatedStringHandler2.AppendFormatted<Type>(realType);
					text = DebugFormatter.Format(ref formatInterpolatedStringHandler2);
				}
				else
				{
					FormatInterpolatedStringHandler formatInterpolatedStringHandler3 = new FormatInterpolatedStringHandler(44, 3);
					formatInterpolatedStringHandler3.AppendLiteral("Argument ");
					formatInterpolatedStringHandler3.AppendFormatted<int>(arg);
					formatInterpolatedStringHandler3.AppendLiteral(" is the wrong type; expected ");
					formatInterpolatedStringHandler3.AppendFormatted<Type>(expectedType);
					formatInterpolatedStringHandler3.AppendLiteral(", got ");
					formatInterpolatedStringHandler3.AppendFormatted<Type>(realType);
					text = DebugFormatter.Format(ref formatInterpolatedStringHandler3);
				}
			}
			else
			{
				FormatInterpolatedStringHandler formatInterpolatedStringHandler4 = new FormatInterpolatedStringHandler(48, 2);
				formatInterpolatedStringHandler4.AppendLiteral("Result object is the wrong type; expected ");
				formatInterpolatedStringHandler4.AppendFormatted<Type>(expectedType);
				formatInterpolatedStringHandler4.AppendLiteral(", got ");
				formatInterpolatedStringHandler4.AppendFormatted<Type>(realType);
				text = DebugFormatter.Format(ref formatInterpolatedStringHandler4);
			}
			return new ArgumentException(text, argName);
		}

		// Token: 0x06002E70 RID: 11888 RVA: 0x0009F481 File Offset: 0x0009D681
		private static FastReflectionHelper.ReturnTypeClass ClassifyType(Type returnType)
		{
			if (returnType == typeof(void))
			{
				return FastReflectionHelper.ReturnTypeClass.Void;
			}
			if (!returnType.IsValueType)
			{
				return FastReflectionHelper.ReturnTypeClass.ReferenceType;
			}
			if (Nullable.GetUnderlyingType(returnType) != null)
			{
				return FastReflectionHelper.ReturnTypeClass.Nullable;
			}
			return FastReflectionHelper.ReturnTypeClass.ValueType;
		}

		// Token: 0x06002E71 RID: 11889 RVA: 0x0009F4AC File Offset: 0x0009D6AC
		private static void EmitCheckArgs(ILCursor il, bool isStatic, FastReflectionHelper.ReturnTypeClass rtc, int expectParams)
		{
			il.Emit(OpCodes.Ldc_I4, (isStatic > false) ? 1 : 0);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4, (int)rtc);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldc_I4, expectParams);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, FastReflectionHelper.CheckArgsMethod);
		}

		// Token: 0x06002E72 RID: 11890 RVA: 0x0009F518 File Offset: 0x0009D718
		private static void EmitCheckType(ILCursor il, int argId, Type expectType, ILLabel badArgLbl)
		{
			ILLabel nextLbl = il.DefineLabel();
			bool isByRef = expectType.IsByRef;
			VariableDefinition valueVar = null;
			if (isByRef)
			{
				expectType = expectType.GetElementType() ?? expectType;
				FastReflectionHelper.ReturnTypeClass cls = FastReflectionHelper.ClassifyType(expectType);
				if (!expectType.IsValueType)
				{
					valueVar = new VariableDefinition(il.Module.TypeSystem.Object);
					il.Context.Body.Variables.Add(valueVar);
					il.Emit(OpCodes.Stloc, valueVar);
					il.Emit(OpCodes.Ldloc, valueVar);
				}
				FastReflectionHelper.EmitCheckByref(il, cls, expectType, badArgLbl, argId);
				if (expectType.IsValueType)
				{
					return;
				}
				if (valueVar != null)
				{
					il.Emit(OpCodes.Ldloc, valueVar);
				}
				FastReflectionHelper.EmitLoadByref(il, cls, expectType);
				il.Emit(OpCodes.Ldind_Ref);
			}
			if (expectType != typeof(object))
			{
				il.Emit(OpCodes.Isinst, expectType);
			}
			il.Emit(OpCodes.Brtrue, nextLbl);
			il.Emit(OpCodes.Ldc_I4, argId);
			il.Emit(OpCodes.Ldtoken, expectType);
			il.Emit(OpCodes.Br, badArgLbl);
			il.MarkLabel(nextLbl);
		}

		// Token: 0x06002E73 RID: 11891 RVA: 0x0009F630 File Offset: 0x0009D830
		private static void EmitCheckAllowNull(ILCursor il, int argId, Type expectType, ILLabel badArgLbl)
		{
			ILLabel nextLbl = il.DefineLabel();
			bool isByref = expectType.IsByRef;
			VariableDefinition valueVar = null;
			if (isByref)
			{
				expectType = expectType.GetElementType() ?? expectType;
				FastReflectionHelper.ReturnTypeClass cls = FastReflectionHelper.ClassifyType(expectType);
				if (!expectType.IsValueType)
				{
					valueVar = new VariableDefinition(il.Module.TypeSystem.Object);
					il.Context.Body.Variables.Add(valueVar);
					il.Emit(OpCodes.Stloc, valueVar);
					il.Emit(OpCodes.Ldloc, valueVar);
				}
				FastReflectionHelper.EmitCheckByref(il, cls, expectType, badArgLbl, argId);
				if (expectType.IsValueType)
				{
					return;
				}
				if (valueVar != null)
				{
					il.Emit(OpCodes.Ldloc, valueVar);
				}
				FastReflectionHelper.EmitLoadByref(il, cls, expectType);
				il.Emit(OpCodes.Ldind_Ref);
			}
			if (expectType == typeof(object))
			{
				il.Emit(OpCodes.Pop);
				return;
			}
			if (!expectType.IsValueType || Nullable.GetUnderlyingType(expectType) != null)
			{
				ILLabel doCheck = il.DefineLabel();
				VariableDefinition val = new VariableDefinition(il.Module.TypeSystem.Object);
				il.Context.Body.Variables.Add(val);
				il.Emit(OpCodes.Stloc, val);
				il.Emit(OpCodes.Ldloc, val);
				il.Emit(OpCodes.Brtrue, doCheck);
				il.Emit(OpCodes.Br, nextLbl);
				il.MarkLabel(doCheck);
				il.Emit(OpCodes.Ldloc, val);
			}
			if (!expectType.IsValueType || (!isByref && expectType.IsValueType))
			{
				FastReflectionHelper.EmitCheckType(il, argId, expectType, badArgLbl);
			}
			il.MarkLabel(nextLbl);
		}

		// Token: 0x06002E74 RID: 11892 RVA: 0x0009F7C8 File Offset: 0x0009D9C8
		private static void EmitBadArgCall(ILCursor il, ILLabel badArgLbl)
		{
			il.MarkLabel(badArgLbl);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, FastReflectionHelper.BadArgExceptionMethod);
			il.Emit(OpCodes.Throw);
		}

		// Token: 0x06002E75 RID: 11893 RVA: 0x0009F820 File Offset: 0x0009DA20
		private static void EmitCheckByref(ILCursor il, FastReflectionHelper.ReturnTypeClass rtc, Type returnType, ILLabel badArgLbl, int argId = -2)
		{
			Type expectType;
			switch (rtc)
			{
			case FastReflectionHelper.ReturnTypeClass.Void:
				return;
			case FastReflectionHelper.ReturnTypeClass.ValueType:
				expectType = returnType;
				break;
			case FastReflectionHelper.ReturnTypeClass.Nullable:
				expectType = typeof(StrongBox<>).MakeGenericType(new Type[] { returnType });
				break;
			case FastReflectionHelper.ReturnTypeClass.ReferenceType:
				expectType = typeof(WeakBox);
				break;
			default:
				return;
			}
			FastReflectionHelper.EmitCheckType(il, argId, expectType, badArgLbl);
		}

		// Token: 0x06002E76 RID: 11894 RVA: 0x0009F87C File Offset: 0x0009DA7C
		private static void EmitLoadByref(ILCursor il, FastReflectionHelper.ReturnTypeClass rtc, Type returnType)
		{
			switch (rtc)
			{
			case FastReflectionHelper.ReturnTypeClass.Void:
				break;
			case FastReflectionHelper.ReturnTypeClass.ValueType:
				il.Emit(OpCodes.Unbox, returnType);
				return;
			case FastReflectionHelper.ReturnTypeClass.Nullable:
			{
				FieldInfo strongBoxField = typeof(StrongBox<>).MakeGenericType(new Type[] { returnType }).GetField("Value");
				il.Emit(OpCodes.Ldflda, strongBoxField);
				return;
			}
			case FastReflectionHelper.ReturnTypeClass.ReferenceType:
				il.Emit(OpCodes.Ldflda, FastReflectionHelper.WeakBoxValueField);
				break;
			default:
				return;
			}
		}

		// Token: 0x06002E77 RID: 11895 RVA: 0x0009F8F1 File Offset: 0x0009DAF1
		private static void EmitLoadArgO(ILCursor il, int arg)
		{
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldc_I4, arg);
			il.Emit(OpCodes.Ldelem_Ref);
		}

		// Token: 0x06002E78 RID: 11896 RVA: 0x0009F918 File Offset: 0x0009DB18
		private static void EmitStoreByref(ILCursor il, FastReflectionHelper.ReturnTypeClass rtc, Type returnType)
		{
			if (rtc != FastReflectionHelper.ReturnTypeClass.Void)
			{
				if (returnType.IsValueType)
				{
					il.Emit(OpCodes.Stobj, returnType);
					return;
				}
				il.Emit(OpCodes.Stind_Ref);
			}
		}

		// Token: 0x06002E79 RID: 11897 RVA: 0x0009F940 File Offset: 0x0009DB40
		private static FastReflectionHelper.FastStructInvoker CreateMethodInvoker(MethodBase method, out FastReflectionHelper.ReturnTypeClass retTypeClass, out Type retType)
		{
			FastReflectionHelper.<>c__DisplayClass44_0 CS$<>8__locals1 = new FastReflectionHelper.<>c__DisplayClass44_0();
			CS$<>8__locals1.method = method;
			if (!CS$<>8__locals1.method.IsStatic)
			{
				Type declaringType = CS$<>8__locals1.method.DeclaringType;
				if (declaringType != null && declaringType.IsByRefLike())
				{
					throw new ArgumentException("Cannot create reflection invoker for instance method on byref-like type", "method");
				}
			}
			FastReflectionHelper.<>c__DisplayClass44_0 CS$<>8__locals2 = CS$<>8__locals1;
			MethodInfo mi = CS$<>8__locals1.method as MethodInfo;
			CS$<>8__locals2.returnType = ((mi != null) ? mi.ReturnType : CS$<>8__locals1.method.DeclaringType);
			retType = CS$<>8__locals1.returnType;
			if (CS$<>8__locals1.returnType.IsByRef || CS$<>8__locals1.returnType.IsByRefLike())
			{
				throw new ArgumentException("Cannot create reflection invoker for method with byref or byref-like return type", "method");
			}
			retTypeClass = FastReflectionHelper.ClassifyType(CS$<>8__locals1.returnType);
			CS$<>8__locals1.typeClass = retTypeClass;
			CS$<>8__locals1.methParams = CS$<>8__locals1.method.GetParameters();
			FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(22, 1);
			formatInterpolatedStringHandler.AppendLiteral("MM:FastStructInvoker<");
			formatInterpolatedStringHandler.AppendFormatted<MethodBase>(CS$<>8__locals1.method);
			formatInterpolatedStringHandler.AppendLiteral(">");
			FastReflectionHelper.FastStructInvoker result;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition(DebugFormatter.Format(ref formatInterpolatedStringHandler), null, FastReflectionHelper.FastStructInvokerArgs))
			{
				using (ILContext ilc2 = new ILContext(dmd.Definition))
				{
					ilc2.Invoke(delegate(ILContext ilc)
					{
						ILCursor il = new ILCursor(ilc);
						FastReflectionHelper.EmitCheckArgs(il, CS$<>8__locals1.method.IsStatic || CS$<>8__locals1.method is ConstructorInfo, CS$<>8__locals1.typeClass, CS$<>8__locals1.methParams.Length);
						ILLabel badArgLbl = il.DefineLabel();
						if (!CS$<>8__locals1.method.IsStatic && !(CS$<>8__locals1.method is ConstructorInfo))
						{
							Type expectType = CS$<>8__locals1.method.DeclaringType;
							Helpers.Assert(expectType != null, null, "expectType is not null");
							il.Emit(OpCodes.Ldarg_0);
							FastReflectionHelper.EmitCheckType(il, -1, expectType, badArgLbl);
						}
						if (CS$<>8__locals1.typeClass != FastReflectionHelper.ReturnTypeClass.Void)
						{
							il.Emit(OpCodes.Ldarg_1);
							FastReflectionHelper.EmitCheckByref(il, CS$<>8__locals1.typeClass, CS$<>8__locals1.returnType, badArgLbl, -2);
						}
						for (int arg = 0; arg < CS$<>8__locals1.methParams.Length; arg++)
						{
							Type ptype = CS$<>8__locals1.methParams[arg].ParameterType;
							if (ptype.IsByRefLike())
							{
								throw new ArgumentException("Cannot create reflection invoker for method with byref-like argument types", "method");
							}
							FastReflectionHelper.EmitLoadArgO(il, arg);
							FastReflectionHelper.EmitCheckAllowNull(il, arg, ptype, badArgLbl);
						}
						if (CS$<>8__locals1.typeClass != FastReflectionHelper.ReturnTypeClass.Void)
						{
							il.Emit(OpCodes.Ldarg_1);
							FastReflectionHelper.EmitLoadByref(il, CS$<>8__locals1.typeClass, CS$<>8__locals1.returnType);
						}
						if (!CS$<>8__locals1.method.IsStatic && !(CS$<>8__locals1.method is ConstructorInfo))
						{
							Type declType = CS$<>8__locals1.method.DeclaringType;
							Helpers.Assert(declType != null, null, "declType is not null");
							il.Emit(OpCodes.Ldarg_0);
							if (declType.IsValueType)
							{
								il.Emit(OpCodes.Unbox, declType);
							}
						}
						for (int arg2 = 0; arg2 < CS$<>8__locals1.methParams.Length; arg2++)
						{
							il.DefineLabel();
							Type ptype2 = CS$<>8__locals1.methParams[arg2].ParameterType;
							Type realType = (ptype2.IsByRef ? (ptype2.GetElementType() ?? ptype2) : ptype2);
							FastReflectionHelper.EmitLoadArgO(il, arg2);
							if (ptype2.IsByRef)
							{
								FastReflectionHelper.EmitLoadByref(il, FastReflectionHelper.ClassifyType(realType), realType);
							}
							else if (ptype2.IsValueType)
							{
								il.Emit(OpCodes.Unbox_Any, realType);
							}
						}
						ConstructorInfo ci = CS$<>8__locals1.method as ConstructorInfo;
						if (ci != null)
						{
							il.Emit(OpCodes.Newobj, ci);
						}
						else if (CS$<>8__locals1.method.IsVirtual)
						{
							il.Emit(OpCodes.Callvirt, CS$<>8__locals1.method);
						}
						else
						{
							il.Emit(OpCodes.Call, CS$<>8__locals1.method);
						}
						FastReflectionHelper.EmitStoreByref(il, CS$<>8__locals1.typeClass, CS$<>8__locals1.returnType);
						il.Emit(OpCodes.Ret);
						FastReflectionHelper.EmitBadArgCall(il, badArgLbl);
					});
					result = dmd.Generate().CreateDelegate<FastReflectionHelper.FastStructInvoker>();
				}
			}
			return result;
		}

		// Token: 0x06002E7A RID: 11898 RVA: 0x0009FAB4 File Offset: 0x0009DCB4
		private static FastReflectionHelper.FastStructInvoker CreateFieldInvoker(FieldInfo field, out FastReflectionHelper.ReturnTypeClass retTypeClass, out Type retType)
		{
			if (!field.IsStatic)
			{
				Type declaringType = field.DeclaringType;
				if (declaringType != null && declaringType.IsByRefLike())
				{
					throw new ArgumentException("Cannot create reflection invoker for instance field on byref-like type", "field");
				}
			}
			Type returnType = field.FieldType;
			retType = returnType;
			retTypeClass = FastReflectionHelper.ClassifyType(returnType);
			FastReflectionHelper.ReturnTypeClass typeClass = retTypeClass;
			FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(22, 1);
			formatInterpolatedStringHandler.AppendLiteral("MM:FastStructInvoker<");
			formatInterpolatedStringHandler.AppendFormatted<FieldInfo>(field);
			formatInterpolatedStringHandler.AppendLiteral(">");
			FastReflectionHelper.FastStructInvoker result;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition(DebugFormatter.Format(ref formatInterpolatedStringHandler), null, FastReflectionHelper.FastStructInvokerArgs))
			{
				using (ILContext ilc2 = new ILContext(dmd.Definition))
				{
					ilc2.Invoke(delegate(ILContext ilc)
					{
						ILCursor il = new ILCursor(ilc);
						FastReflectionHelper.EmitCheckArgs(il, field.IsStatic, typeClass, 0);
						ILLabel badArgLbl = il.DefineLabel();
						if (!field.IsStatic)
						{
							Type expect = field.DeclaringType;
							il.Emit(OpCodes.Ldarg_0);
							FastReflectionHelper.EmitCheckType(il, -1, expect, badArgLbl);
						}
						il.Emit(OpCodes.Ldarg_1);
						FastReflectionHelper.EmitCheckByref(il, typeClass, returnType, badArgLbl, -2);
						ILLabel getLbl = il.DefineLabel();
						il.Emit(OpCodes.Ldarg_2);
						il.Emit(OpCodes.Brfalse, getLbl);
						il.Emit(OpCodes.Ldarg_2);
						il.Emit(OpCodes.Ldlen);
						il.Emit(OpCodes.Ldc_I4_1);
						il.Emit(OpCodes.Blt, getLbl);
						FastReflectionHelper.EmitLoadArgO(il, 0);
						FastReflectionHelper.EmitCheckAllowNull(il, 0, field.FieldType, badArgLbl);
						il.Emit(OpCodes.Ldarg_1);
						FastReflectionHelper.EmitLoadByref(il, typeClass, returnType);
						if (!field.IsStatic)
						{
							Type declType = field.DeclaringType;
							Helpers.Assert(declType != null, null, "declType is not null");
							il.Emit(OpCodes.Ldarg_0);
							if (declType.IsValueType)
							{
								il.Emit(OpCodes.Unbox, declType);
							}
						}
						FastReflectionHelper.EmitLoadArgO(il, 0);
						il.Emit(OpCodes.Unbox_Any, field.FieldType);
						if (field.IsStatic)
						{
							il.Emit(OpCodes.Stsfld, field);
						}
						else
						{
							il.Emit(OpCodes.Stfld, field);
						}
						FastReflectionHelper.EmitLoadArgO(il, 0);
						il.Emit(OpCodes.Unbox_Any, field.FieldType);
						FastReflectionHelper.EmitStoreByref(il, typeClass, returnType);
						il.Emit(OpCodes.Ret);
						il.MarkLabel(getLbl);
						il.Emit(OpCodes.Ldarg_1);
						FastReflectionHelper.EmitLoadByref(il, typeClass, returnType);
						if (!field.IsStatic)
						{
							Type declType2 = field.DeclaringType;
							Helpers.Assert(declType2 != null, null, "declType is not null");
							il.Emit(OpCodes.Ldarg_0);
							if (declType2.IsValueType)
							{
								il.Emit(OpCodes.Unbox, declType2);
							}
						}
						if (field.IsStatic)
						{
							il.Emit(OpCodes.Ldsfld, field);
						}
						else
						{
							il.Emit(OpCodes.Ldfld, field);
						}
						FastReflectionHelper.EmitStoreByref(il, typeClass, returnType);
						il.Emit(OpCodes.Ret);
						FastReflectionHelper.EmitBadArgCall(il, badArgLbl);
					});
					result = dmd.Generate().CreateDelegate<FastReflectionHelper.FastStructInvoker>();
				}
			}
			return result;
		}

		// Token: 0x06002E7C RID: 11900 RVA: 0x0009FCE2 File Offset: 0x0009DEE2
		[CompilerGenerated]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void <CheckArgs>g__ThrowArgumentOutOfRange|28_0()
		{
			throw new ArgumentOutOfRangeException("args", "Argument array has too few arguments!");
		}

		// Token: 0x04003B1B RID: 15131
		private static readonly Type[] FastStructInvokerArgs = new Type[]
		{
			typeof(object),
			typeof(object),
			typeof(object[])
		};

		// Token: 0x04003B1C RID: 15132
		private static readonly MethodInfo S2FValueType = typeof(FastReflectionHelper).GetMethod("FastInvokerForStructInvokerVT", BindingFlags.Static | BindingFlags.NonPublic);

		// Token: 0x04003B1D RID: 15133
		private static readonly MethodInfo S2FNullable = typeof(FastReflectionHelper).GetMethod("FastInvokerForStructInvokerNullable", BindingFlags.Static | BindingFlags.NonPublic);

		// Token: 0x04003B1E RID: 15134
		[Nullable(2)]
		[ThreadStatic]
		private static WeakBox CachedWeakBox;

		// Token: 0x04003B1F RID: 15135
		private static readonly MethodInfo S2FClass = typeof(FastReflectionHelper).GetMethod("FastInvokerForStructInvokerClass", BindingFlags.Static | BindingFlags.NonPublic);

		// Token: 0x04003B20 RID: 15136
		private static readonly MethodInfo S2FVoid = typeof(FastReflectionHelper).GetMethod("FastInvokerForStructInvokerVoid", BindingFlags.Static | BindingFlags.NonPublic);

		// Token: 0x04003B21 RID: 15137
		private static ConditionalWeakTable<MemberInfo, FastReflectionHelper.FSITuple> fastStructInvokers = new ConditionalWeakTable<MemberInfo, FastReflectionHelper.FSITuple>();

		// Token: 0x04003B22 RID: 15138
		private static ConditionalWeakTable<FastReflectionHelper.FSITuple, FastReflectionHelper.FastInvoker> fastInvokers = new ConditionalWeakTable<FastReflectionHelper.FSITuple, FastReflectionHelper.FastInvoker>();

		// Token: 0x04003B23 RID: 15139
		private static readonly MethodInfo CheckArgsMethod = typeof(FastReflectionHelper).GetMethod("CheckArgs", BindingFlags.Static | BindingFlags.NonPublic);

		// Token: 0x04003B24 RID: 15140
		private const int TargetArgId = -1;

		// Token: 0x04003B25 RID: 15141
		private const int ResultArgId = -2;

		// Token: 0x04003B26 RID: 15142
		private static readonly MethodInfo BadArgExceptionMethod = typeof(FastReflectionHelper).GetMethod("BadArgException", BindingFlags.Static | BindingFlags.NonPublic);

		// Token: 0x04003B27 RID: 15143
		private static readonly FieldInfo WeakBoxValueField = typeof(WeakBox).GetField("Value");

		// Token: 0x020008BC RID: 2236
		// (Invoke) Token: 0x06002E7E RID: 11902
		[NullableContext(0)]
		public delegate object FastInvoker(object target, params object[] args);

		// Token: 0x020008BD RID: 2237
		// (Invoke) Token: 0x06002E82 RID: 11906
		[NullableContext(0)]
		public delegate void FastStructInvoker(object target, object result, params object[] args);

		// Token: 0x020008BE RID: 2238
		[NullableContext(0)]
		private static class TypedCache<T> where T : struct
		{
			// Token: 0x04003B28 RID: 15144
			[Nullable(new byte[] { 2, 0 })]
			[ThreadStatic]
			public static StrongBox<T?> NullableStrongBox;
		}

		// Token: 0x020008BF RID: 2239
		[NullableContext(0)]
		private enum ReturnTypeClass
		{
			// Token: 0x04003B2A RID: 15146
			Void,
			// Token: 0x04003B2B RID: 15147
			ValueType,
			// Token: 0x04003B2C RID: 15148
			Nullable,
			// Token: 0x04003B2D RID: 15149
			ReferenceType
		}

		// Token: 0x020008C0 RID: 2240
		[Nullable(0)]
		private sealed class FSITuple
		{
			// Token: 0x06002E85 RID: 11909 RVA: 0x0009FCF3 File Offset: 0x0009DEF3
			public FSITuple(FastReflectionHelper.FastStructInvoker fsi, FastReflectionHelper.ReturnTypeClass rtc, Type rt)
			{
				this.FSI = fsi;
				this.RTC = rtc;
				this.ReturnType = rt;
			}

			// Token: 0x04003B2E RID: 15150
			public readonly FastReflectionHelper.FastStructInvoker FSI;

			// Token: 0x04003B2F RID: 15151
			public readonly FastReflectionHelper.ReturnTypeClass RTC;

			// Token: 0x04003B30 RID: 15152
			public readonly Type ReturnType;
		}
	}
}
