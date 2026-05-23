using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace MonoMod.Cil
{
	// Token: 0x02000829 RID: 2089
	internal static class FastDelegateInvokers
	{
		// Token: 0x06002832 RID: 10290 RVA: 0x0008B430 File Offset: 0x00089630
		[GetFastDelegateInvokersArray(16)]
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		private static ValueTuple<MethodInfo, Type>[] GetInvokers()
		{
			return new ValueTuple<MethodInfo, Type>[]
			{
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal1", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal1<>)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal1", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal1<, >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef1", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef1<>)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef1", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef1<, >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal2", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal2<, >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal2", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal2<, , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef2", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef2<, >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef2", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef2<, , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal3", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal3<, , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal3", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal3<, , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef3", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef3<, , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef3", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef3<, , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal4", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal4<, , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal4", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal4<, , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef4", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef4<, , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef4", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef4<, , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal5", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal5<, , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal5", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal5<, , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef5", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef5<, , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef5", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef5<, , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal6", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal6<, , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal6", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal6<, , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef6", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef6<, , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef6", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef6<, , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal7", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal7<, , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal7", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal7<, , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef7", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef7<, , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef7", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef7<, , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal8", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal8<, , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal8", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal8<, , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef8", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef8<, , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef8", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef8<, , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal9", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal9<, , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal9", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal9<, , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef9", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef9<, , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef9", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef9<, , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal10", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal10<, , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal10", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal10<, , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef10", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef10<, , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef10", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef10<, , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal11", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal11<, , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal11", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal11<, , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef11", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef11<, , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef11", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef11<, , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal12", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal12<, , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal12", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal12<, , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef12", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef12<, , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef12", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef12<, , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal13", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal13<, , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal13", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal13<, , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef13", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef13<, , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef13", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef13<, , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal14", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal14<, , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal14", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal14<, , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef14", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef14<, , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef14", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef14<, , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal15", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal15<, , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal15", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal15<, , , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef15", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef15<, , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef15", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef15<, , , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidVal16", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidVal16<, , , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeVal16", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeVal16<, , , , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeVoidRef16", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.VoidRef16<, , , , , , , , , , , , , , , >)),
				new ValueTuple<MethodInfo, Type>(typeof(FastDelegateInvokers).GetMethod("InvokeTypeRef16", BindingFlags.Static | BindingFlags.NonPublic), typeof(FastDelegateInvokers.TypeRef16<, , , , , , , , , , , , , , , , >))
			};
		}

		// Token: 0x06002833 RID: 10291 RVA: 0x0008BF7C File Offset: 0x0008A17C
		[NullableContext(1)]
		[return: TupleElementNames(new string[] { "Invoker", "Delegate" })]
		[return: Nullable(new byte[] { 0, 1, 1 })]
		private static ValueTuple<MethodInfo, Type>? TryGetInvokerForSig(MethodSignature sig)
		{
			if (sig.ParameterCount == 0)
			{
				return null;
			}
			if (sig.ParameterCount > 16)
			{
				return null;
			}
			if (sig.ReturnType.IsByRef || sig.ReturnType.IsByRefLike())
			{
				return null;
			}
			if (sig.FirstParameter.IsByRefLike())
			{
				return null;
			}
			if (sig.Parameters.Skip(1).Any((Type t) => t.IsByRef || t.IsByRefLike()))
			{
				return null;
			}
			int index = 0;
			index |= (((sig.ReturnType != typeof(void)) > false) ? 1 : 0);
			index |= (sig.FirstParameter.IsByRef ? 2 : 0);
			index |= sig.ParameterCount - 1 << 2;
			ValueTuple<MethodInfo, Type> valueTuple = FastDelegateInvokers.invokers[index];
			MethodInfo invoker = valueTuple.Item1;
			Type del = valueTuple.Item2;
			Type[] typeParams = new Type[sig.ParameterCount + (index & 1)];
			int i = 0;
			if ((index & 1) != 0)
			{
				typeParams[i++] = sig.ReturnType;
			}
			foreach (Type t2 in sig.Parameters)
			{
				if (t2.IsByRef)
				{
					t2 = t2.GetElementType();
				}
				typeParams[i++] = t2;
			}
			Helpers.Assert(i == typeParams.Length, null, "i == typeParams.Length");
			return new ValueTuple<MethodInfo, Type>?(new ValueTuple<MethodInfo, Type>(invoker.MakeGenericMethod(typeParams), del.MakeGenericType(typeParams)));
		}

		// Token: 0x06002834 RID: 10292 RVA: 0x0008C130 File Offset: 0x0008A330
		[NullableContext(1)]
		[return: TupleElementNames(new string[] { "Invoker", "Delegate" })]
		[return: Nullable(new byte[] { 0, 1, 1 })]
		public static ValueTuple<MethodInfo, Type>? GetDelegateInvoker(Type delegateType)
		{
			Helpers.ThrowIfArgumentNull<Type>(delegateType2, "delegateType");
			if (!typeof(Delegate).IsAssignableFrom(delegateType2))
			{
				throw new ArgumentException("Argument not a delegate type", "delegateType");
			}
			Tuple<MethodInfo, Type> tuple = FastDelegateInvokers.invokerCache.GetValue(delegateType2, delegate(Type delegateType)
			{
				MethodInfo delInvoke = delegateType.GetMethod("Invoke");
				MethodSignature sig = MethodSignature.ForMethod(delInvoke, true);
				if (sig.ParameterCount == 0)
				{
					return new Tuple<MethodInfo, Type>(null, delegateType);
				}
				ValueTuple<MethodInfo, Type>? builtinInvoker = FastDelegateInvokers.TryGetInvokerForSig(sig);
				if (builtinInvoker != null)
				{
					ValueTuple<MethodInfo, Type> p = builtinInvoker.GetValueOrDefault();
					return new Tuple<MethodInfo, Type>(p.Item1, p.Item2);
				}
				Type[] argTypes = new Type[sig.ParameterCount + 1];
				int i = 0;
				foreach (Type param in sig.Parameters)
				{
					argTypes[i++] = param;
				}
				argTypes[sig.ParameterCount] = delegateType;
				string str = "MMIL:Invoke<";
				Type declaringType = delInvoke.DeclaringType;
				Tuple<MethodInfo, Type> result;
				using (DynamicMethodDefinition dmdInvoke = new DynamicMethodDefinition(str + ((declaringType != null) ? declaringType.FullName : null) + ">", delInvoke.ReturnType, argTypes))
				{
					ILProcessor il = dmdInvoke.GetILProcessor();
					il.Emit(OpCodes.Ldarg, sig.ParameterCount);
					for (i = 0; i < sig.ParameterCount; i++)
					{
						il.Emit(OpCodes.Ldarg, i);
					}
					il.Emit(OpCodes.Callvirt, delInvoke);
					il.Emit(OpCodes.Ret);
					result = new Tuple<MethodInfo, Type>(dmdInvoke.Generate(), delegateType);
				}
				return result;
			});
			if (tuple.Item1 == null)
			{
				return null;
			}
			return new ValueTuple<MethodInfo, Type>?(new ValueTuple<MethodInfo, Type>(tuple.Item1, tuple.Item2));
		}

		// Token: 0x06002835 RID: 10293 RVA: 0x0008C1BD File Offset: 0x0008A3BD
		private static void InvokeVoidVal1<T0>(T0 _0, FastDelegateInvokers.VoidVal1<T0> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal1<T0>>(del, "del")(_0);
		}

		// Token: 0x06002836 RID: 10294 RVA: 0x0008C1D0 File Offset: 0x0008A3D0
		private static TResult InvokeTypeVal1<TResult, T0>(T0 _0, FastDelegateInvokers.TypeVal1<TResult, T0> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal1<TResult, T0>>(del, "del")(_0);
		}

		// Token: 0x06002837 RID: 10295 RVA: 0x0008C1E3 File Offset: 0x0008A3E3
		private static void InvokeVoidRef1<T0>(ref T0 _0, FastDelegateInvokers.VoidRef1<T0> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef1<T0>>(del, "del")(ref _0);
		}

		// Token: 0x06002838 RID: 10296 RVA: 0x0008C1F6 File Offset: 0x0008A3F6
		private static TResult InvokeTypeRef1<TResult, T0>(ref T0 _0, FastDelegateInvokers.TypeRef1<TResult, T0> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef1<TResult, T0>>(del, "del")(ref _0);
		}

		// Token: 0x06002839 RID: 10297 RVA: 0x0008C209 File Offset: 0x0008A409
		private static void InvokeVoidVal2<T0, T1>(T0 _0, T1 _1, FastDelegateInvokers.VoidVal2<T0, T1> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal2<T0, T1>>(del, "del")(_0, _1);
		}

		// Token: 0x0600283A RID: 10298 RVA: 0x0008C21D File Offset: 0x0008A41D
		private static TResult InvokeTypeVal2<TResult, T0, T1>(T0 _0, T1 _1, FastDelegateInvokers.TypeVal2<TResult, T0, T1> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal2<TResult, T0, T1>>(del, "del")(_0, _1);
		}

		// Token: 0x0600283B RID: 10299 RVA: 0x0008C231 File Offset: 0x0008A431
		private static void InvokeVoidRef2<T0, T1>(ref T0 _0, T1 _1, FastDelegateInvokers.VoidRef2<T0, T1> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef2<T0, T1>>(del, "del")(ref _0, _1);
		}

		// Token: 0x0600283C RID: 10300 RVA: 0x0008C245 File Offset: 0x0008A445
		private static TResult InvokeTypeRef2<TResult, T0, T1>(ref T0 _0, T1 _1, FastDelegateInvokers.TypeRef2<TResult, T0, T1> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef2<TResult, T0, T1>>(del, "del")(ref _0, _1);
		}

		// Token: 0x0600283D RID: 10301 RVA: 0x0008C259 File Offset: 0x0008A459
		private static void InvokeVoidVal3<T0, T1, T2>(T0 _0, T1 _1, T2 _2, FastDelegateInvokers.VoidVal3<T0, T1, T2> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal3<T0, T1, T2>>(del, "del")(_0, _1, _2);
		}

		// Token: 0x0600283E RID: 10302 RVA: 0x0008C26E File Offset: 0x0008A46E
		private static TResult InvokeTypeVal3<TResult, T0, T1, T2>(T0 _0, T1 _1, T2 _2, FastDelegateInvokers.TypeVal3<TResult, T0, T1, T2> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal3<TResult, T0, T1, T2>>(del, "del")(_0, _1, _2);
		}

		// Token: 0x0600283F RID: 10303 RVA: 0x0008C283 File Offset: 0x0008A483
		private static void InvokeVoidRef3<T0, T1, T2>(ref T0 _0, T1 _1, T2 _2, FastDelegateInvokers.VoidRef3<T0, T1, T2> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef3<T0, T1, T2>>(del, "del")(ref _0, _1, _2);
		}

		// Token: 0x06002840 RID: 10304 RVA: 0x0008C298 File Offset: 0x0008A498
		private static TResult InvokeTypeRef3<TResult, T0, T1, T2>(ref T0 _0, T1 _1, T2 _2, FastDelegateInvokers.TypeRef3<TResult, T0, T1, T2> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef3<TResult, T0, T1, T2>>(del, "del")(ref _0, _1, _2);
		}

		// Token: 0x06002841 RID: 10305 RVA: 0x0008C2AD File Offset: 0x0008A4AD
		private static void InvokeVoidVal4<T0, T1, T2, T3>(T0 _0, T1 _1, T2 _2, T3 _3, FastDelegateInvokers.VoidVal4<T0, T1, T2, T3> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal4<T0, T1, T2, T3>>(del, "del")(_0, _1, _2, _3);
		}

		// Token: 0x06002842 RID: 10306 RVA: 0x0008C2C4 File Offset: 0x0008A4C4
		private static TResult InvokeTypeVal4<TResult, T0, T1, T2, T3>(T0 _0, T1 _1, T2 _2, T3 _3, FastDelegateInvokers.TypeVal4<TResult, T0, T1, T2, T3> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal4<TResult, T0, T1, T2, T3>>(del, "del")(_0, _1, _2, _3);
		}

		// Token: 0x06002843 RID: 10307 RVA: 0x0008C2DB File Offset: 0x0008A4DB
		private static void InvokeVoidRef4<T0, T1, T2, T3>(ref T0 _0, T1 _1, T2 _2, T3 _3, FastDelegateInvokers.VoidRef4<T0, T1, T2, T3> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef4<T0, T1, T2, T3>>(del, "del")(ref _0, _1, _2, _3);
		}

		// Token: 0x06002844 RID: 10308 RVA: 0x0008C2F2 File Offset: 0x0008A4F2
		private static TResult InvokeTypeRef4<TResult, T0, T1, T2, T3>(ref T0 _0, T1 _1, T2 _2, T3 _3, FastDelegateInvokers.TypeRef4<TResult, T0, T1, T2, T3> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef4<TResult, T0, T1, T2, T3>>(del, "del")(ref _0, _1, _2, _3);
		}

		// Token: 0x06002845 RID: 10309 RVA: 0x0008C309 File Offset: 0x0008A509
		private static void InvokeVoidVal5<T0, T1, T2, T3, T4>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, FastDelegateInvokers.VoidVal5<T0, T1, T2, T3, T4> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal5<T0, T1, T2, T3, T4>>(del, "del")(_0, _1, _2, _3, _4);
		}

		// Token: 0x06002846 RID: 10310 RVA: 0x0008C322 File Offset: 0x0008A522
		private static TResult InvokeTypeVal5<TResult, T0, T1, T2, T3, T4>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, FastDelegateInvokers.TypeVal5<TResult, T0, T1, T2, T3, T4> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal5<TResult, T0, T1, T2, T3, T4>>(del, "del")(_0, _1, _2, _3, _4);
		}

		// Token: 0x06002847 RID: 10311 RVA: 0x0008C33B File Offset: 0x0008A53B
		private static void InvokeVoidRef5<T0, T1, T2, T3, T4>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, FastDelegateInvokers.VoidRef5<T0, T1, T2, T3, T4> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef5<T0, T1, T2, T3, T4>>(del, "del")(ref _0, _1, _2, _3, _4);
		}

		// Token: 0x06002848 RID: 10312 RVA: 0x0008C354 File Offset: 0x0008A554
		private static TResult InvokeTypeRef5<TResult, T0, T1, T2, T3, T4>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, FastDelegateInvokers.TypeRef5<TResult, T0, T1, T2, T3, T4> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef5<TResult, T0, T1, T2, T3, T4>>(del, "del")(ref _0, _1, _2, _3, _4);
		}

		// Token: 0x06002849 RID: 10313 RVA: 0x0008C36D File Offset: 0x0008A56D
		private static void InvokeVoidVal6<T0, T1, T2, T3, T4, T5>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, FastDelegateInvokers.VoidVal6<T0, T1, T2, T3, T4, T5> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal6<T0, T1, T2, T3, T4, T5>>(del, "del")(_0, _1, _2, _3, _4, _5);
		}

		// Token: 0x0600284A RID: 10314 RVA: 0x0008C388 File Offset: 0x0008A588
		private static TResult InvokeTypeVal6<TResult, T0, T1, T2, T3, T4, T5>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, FastDelegateInvokers.TypeVal6<TResult, T0, T1, T2, T3, T4, T5> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal6<TResult, T0, T1, T2, T3, T4, T5>>(del, "del")(_0, _1, _2, _3, _4, _5);
		}

		// Token: 0x0600284B RID: 10315 RVA: 0x0008C3A3 File Offset: 0x0008A5A3
		private static void InvokeVoidRef6<T0, T1, T2, T3, T4, T5>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, FastDelegateInvokers.VoidRef6<T0, T1, T2, T3, T4, T5> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef6<T0, T1, T2, T3, T4, T5>>(del, "del")(ref _0, _1, _2, _3, _4, _5);
		}

		// Token: 0x0600284C RID: 10316 RVA: 0x0008C3BE File Offset: 0x0008A5BE
		private static TResult InvokeTypeRef6<TResult, T0, T1, T2, T3, T4, T5>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, FastDelegateInvokers.TypeRef6<TResult, T0, T1, T2, T3, T4, T5> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef6<TResult, T0, T1, T2, T3, T4, T5>>(del, "del")(ref _0, _1, _2, _3, _4, _5);
		}

		// Token: 0x0600284D RID: 10317 RVA: 0x0008C3D9 File Offset: 0x0008A5D9
		private static void InvokeVoidVal7<T0, T1, T2, T3, T4, T5, T6>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, FastDelegateInvokers.VoidVal7<T0, T1, T2, T3, T4, T5, T6> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal7<T0, T1, T2, T3, T4, T5, T6>>(del, "del")(_0, _1, _2, _3, _4, _5, _6);
		}

		// Token: 0x0600284E RID: 10318 RVA: 0x0008C3F6 File Offset: 0x0008A5F6
		private static TResult InvokeTypeVal7<TResult, T0, T1, T2, T3, T4, T5, T6>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, FastDelegateInvokers.TypeVal7<TResult, T0, T1, T2, T3, T4, T5, T6> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal7<TResult, T0, T1, T2, T3, T4, T5, T6>>(del, "del")(_0, _1, _2, _3, _4, _5, _6);
		}

		// Token: 0x0600284F RID: 10319 RVA: 0x0008C413 File Offset: 0x0008A613
		private static void InvokeVoidRef7<T0, T1, T2, T3, T4, T5, T6>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, FastDelegateInvokers.VoidRef7<T0, T1, T2, T3, T4, T5, T6> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef7<T0, T1, T2, T3, T4, T5, T6>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6);
		}

		// Token: 0x06002850 RID: 10320 RVA: 0x0008C430 File Offset: 0x0008A630
		private static TResult InvokeTypeRef7<TResult, T0, T1, T2, T3, T4, T5, T6>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, FastDelegateInvokers.TypeRef7<TResult, T0, T1, T2, T3, T4, T5, T6> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef7<TResult, T0, T1, T2, T3, T4, T5, T6>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6);
		}

		// Token: 0x06002851 RID: 10321 RVA: 0x0008C450 File Offset: 0x0008A650
		private static void InvokeVoidVal8<T0, T1, T2, T3, T4, T5, T6, T7>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, FastDelegateInvokers.VoidVal8<T0, T1, T2, T3, T4, T5, T6, T7> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal8<T0, T1, T2, T3, T4, T5, T6, T7>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7);
		}

		// Token: 0x06002852 RID: 10322 RVA: 0x0008C47C File Offset: 0x0008A67C
		private static TResult InvokeTypeVal8<TResult, T0, T1, T2, T3, T4, T5, T6, T7>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, FastDelegateInvokers.TypeVal8<TResult, T0, T1, T2, T3, T4, T5, T6, T7> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal8<TResult, T0, T1, T2, T3, T4, T5, T6, T7>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7);
		}

		// Token: 0x06002853 RID: 10323 RVA: 0x0008C4A8 File Offset: 0x0008A6A8
		private static void InvokeVoidRef8<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, FastDelegateInvokers.VoidRef8<T0, T1, T2, T3, T4, T5, T6, T7> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef8<T0, T1, T2, T3, T4, T5, T6, T7>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7);
		}

		// Token: 0x06002854 RID: 10324 RVA: 0x0008C4D4 File Offset: 0x0008A6D4
		private static TResult InvokeTypeRef8<TResult, T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, FastDelegateInvokers.TypeRef8<TResult, T0, T1, T2, T3, T4, T5, T6, T7> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef8<TResult, T0, T1, T2, T3, T4, T5, T6, T7>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7);
		}

		// Token: 0x06002855 RID: 10325 RVA: 0x0008C500 File Offset: 0x0008A700
		private static void InvokeVoidVal9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, FastDelegateInvokers.VoidVal9<T0, T1, T2, T3, T4, T5, T6, T7, T8> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal9<T0, T1, T2, T3, T4, T5, T6, T7, T8>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8);
		}

		// Token: 0x06002856 RID: 10326 RVA: 0x0008C52C File Offset: 0x0008A72C
		private static TResult InvokeTypeVal9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, FastDelegateInvokers.TypeVal9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8);
		}

		// Token: 0x06002857 RID: 10327 RVA: 0x0008C558 File Offset: 0x0008A758
		private static void InvokeVoidRef9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, FastDelegateInvokers.VoidRef9<T0, T1, T2, T3, T4, T5, T6, T7, T8> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef9<T0, T1, T2, T3, T4, T5, T6, T7, T8>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8);
		}

		// Token: 0x06002858 RID: 10328 RVA: 0x0008C584 File Offset: 0x0008A784
		private static TResult InvokeTypeRef9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, FastDelegateInvokers.TypeRef9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8);
		}

		// Token: 0x06002859 RID: 10329 RVA: 0x0008C5B0 File Offset: 0x0008A7B0
		private static void InvokeVoidVal10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, FastDelegateInvokers.VoidVal10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9);
		}

		// Token: 0x0600285A RID: 10330 RVA: 0x0008C5E0 File Offset: 0x0008A7E0
		private static TResult InvokeTypeVal10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, FastDelegateInvokers.TypeVal10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9);
		}

		// Token: 0x0600285B RID: 10331 RVA: 0x0008C610 File Offset: 0x0008A810
		private static void InvokeVoidRef10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, FastDelegateInvokers.VoidRef10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9);
		}

		// Token: 0x0600285C RID: 10332 RVA: 0x0008C640 File Offset: 0x0008A840
		private static TResult InvokeTypeRef10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, FastDelegateInvokers.TypeRef10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9);
		}

		// Token: 0x0600285D RID: 10333 RVA: 0x0008C670 File Offset: 0x0008A870
		private static void InvokeVoidVal11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, FastDelegateInvokers.VoidVal11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
		}

		// Token: 0x0600285E RID: 10334 RVA: 0x0008C6A0 File Offset: 0x0008A8A0
		private static TResult InvokeTypeVal11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, FastDelegateInvokers.TypeVal11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
		}

		// Token: 0x0600285F RID: 10335 RVA: 0x0008C6D0 File Offset: 0x0008A8D0
		private static void InvokeVoidRef11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, FastDelegateInvokers.VoidRef11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
		}

		// Token: 0x06002860 RID: 10336 RVA: 0x0008C700 File Offset: 0x0008A900
		private static TResult InvokeTypeRef11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, FastDelegateInvokers.TypeRef11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
		}

		// Token: 0x06002861 RID: 10337 RVA: 0x0008C730 File Offset: 0x0008A930
		private static void InvokeVoidVal12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, FastDelegateInvokers.VoidVal12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11);
		}

		// Token: 0x06002862 RID: 10338 RVA: 0x0008C764 File Offset: 0x0008A964
		private static TResult InvokeTypeVal12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, FastDelegateInvokers.TypeVal12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11);
		}

		// Token: 0x06002863 RID: 10339 RVA: 0x0008C798 File Offset: 0x0008A998
		private static void InvokeVoidRef12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, FastDelegateInvokers.VoidRef12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11);
		}

		// Token: 0x06002864 RID: 10340 RVA: 0x0008C7CC File Offset: 0x0008A9CC
		private static TResult InvokeTypeRef12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, FastDelegateInvokers.TypeRef12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11);
		}

		// Token: 0x06002865 RID: 10341 RVA: 0x0008C800 File Offset: 0x0008AA00
		private static void InvokeVoidVal13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, FastDelegateInvokers.VoidVal13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12);
		}

		// Token: 0x06002866 RID: 10342 RVA: 0x0008C834 File Offset: 0x0008AA34
		private static TResult InvokeTypeVal13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, FastDelegateInvokers.TypeVal13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12);
		}

		// Token: 0x06002867 RID: 10343 RVA: 0x0008C868 File Offset: 0x0008AA68
		private static void InvokeVoidRef13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, FastDelegateInvokers.VoidRef13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12);
		}

		// Token: 0x06002868 RID: 10344 RVA: 0x0008C89C File Offset: 0x0008AA9C
		private static TResult InvokeTypeRef13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, FastDelegateInvokers.TypeRef13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12);
		}

		// Token: 0x06002869 RID: 10345 RVA: 0x0008C8D0 File Offset: 0x0008AAD0
		private static void InvokeVoidVal14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, FastDelegateInvokers.VoidVal14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13);
		}

		// Token: 0x0600286A RID: 10346 RVA: 0x0008C908 File Offset: 0x0008AB08
		private static TResult InvokeTypeVal14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, FastDelegateInvokers.TypeVal14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13);
		}

		// Token: 0x0600286B RID: 10347 RVA: 0x0008C940 File Offset: 0x0008AB40
		private static void InvokeVoidRef14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, FastDelegateInvokers.VoidRef14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13);
		}

		// Token: 0x0600286C RID: 10348 RVA: 0x0008C978 File Offset: 0x0008AB78
		private static TResult InvokeTypeRef14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, FastDelegateInvokers.TypeRef14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13);
		}

		// Token: 0x0600286D RID: 10349 RVA: 0x0008C9B0 File Offset: 0x0008ABB0
		private static void InvokeVoidVal15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, FastDelegateInvokers.VoidVal15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14);
		}

		// Token: 0x0600286E RID: 10350 RVA: 0x0008C9E8 File Offset: 0x0008ABE8
		private static TResult InvokeTypeVal15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, FastDelegateInvokers.TypeVal15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14);
		}

		// Token: 0x0600286F RID: 10351 RVA: 0x0008CA20 File Offset: 0x0008AC20
		private static void InvokeVoidRef15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, FastDelegateInvokers.VoidRef15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14);
		}

		// Token: 0x06002870 RID: 10352 RVA: 0x0008CA58 File Offset: 0x0008AC58
		private static TResult InvokeTypeRef15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, FastDelegateInvokers.TypeRef15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14);
		}

		// Token: 0x06002871 RID: 10353 RVA: 0x0008CA90 File Offset: 0x0008AC90
		private static void InvokeVoidVal16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, FastDelegateInvokers.VoidVal16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidVal16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15);
		}

		// Token: 0x06002872 RID: 10354 RVA: 0x0008CACC File Offset: 0x0008ACCC
		private static TResult InvokeTypeVal16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, FastDelegateInvokers.TypeVal16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeVal16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(del, "del")(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15);
		}

		// Token: 0x06002873 RID: 10355 RVA: 0x0008CB08 File Offset: 0x0008AD08
		private static void InvokeVoidRef16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, FastDelegateInvokers.VoidRef16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> del)
		{
			Helpers.ThrowIfNull<FastDelegateInvokers.VoidRef16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15);
		}

		// Token: 0x06002874 RID: 10356 RVA: 0x0008CB44 File Offset: 0x0008AD44
		private static TResult InvokeTypeRef16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, FastDelegateInvokers.TypeRef16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> del)
		{
			return Helpers.ThrowIfNull<FastDelegateInvokers.TypeRef16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(del, "del")(ref _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15);
		}

		// Token: 0x04003A2C RID: 14892
		[Nullable(new byte[] { 1, 0, 1, 1 })]
		private static readonly ValueTuple<MethodInfo, Type>[] invokers = FastDelegateInvokers.GetInvokers();

		// Token: 0x04003A2D RID: 14893
		private const int MaxFastInvokerParams = 16;

		// Token: 0x04003A2E RID: 14894
		[Nullable(new byte[] { 1, 1, 1, 2, 1 })]
		private static readonly ConditionalWeakTable<Type, Tuple<MethodInfo, Type>> invokerCache = new ConditionalWeakTable<Type, Tuple<MethodInfo, Type>>();

		// Token: 0x0200082A RID: 2090
		// (Invoke) Token: 0x06002877 RID: 10359
		private delegate void VoidVal1<T0>(T0 _0);

		// Token: 0x0200082B RID: 2091
		// (Invoke) Token: 0x0600287B RID: 10363
		private delegate TResult TypeVal1<TResult, T0>(T0 _0);

		// Token: 0x0200082C RID: 2092
		// (Invoke) Token: 0x0600287F RID: 10367
		private delegate void VoidRef1<T0>(ref T0 _0);

		// Token: 0x0200082D RID: 2093
		// (Invoke) Token: 0x06002883 RID: 10371
		private delegate TResult TypeRef1<TResult, T0>(ref T0 _0);

		// Token: 0x0200082E RID: 2094
		// (Invoke) Token: 0x06002887 RID: 10375
		private delegate void VoidVal2<T0, T1>(T0 _0, T1 _1);

		// Token: 0x0200082F RID: 2095
		// (Invoke) Token: 0x0600288B RID: 10379
		private delegate TResult TypeVal2<TResult, T0, T1>(T0 _0, T1 _1);

		// Token: 0x02000830 RID: 2096
		// (Invoke) Token: 0x0600288F RID: 10383
		private delegate void VoidRef2<T0, T1>(ref T0 _0, T1 _1);

		// Token: 0x02000831 RID: 2097
		// (Invoke) Token: 0x06002893 RID: 10387
		private delegate TResult TypeRef2<TResult, T0, T1>(ref T0 _0, T1 _1);

		// Token: 0x02000832 RID: 2098
		// (Invoke) Token: 0x06002897 RID: 10391
		private delegate void VoidVal3<T0, T1, T2>(T0 _0, T1 _1, T2 _2);

		// Token: 0x02000833 RID: 2099
		// (Invoke) Token: 0x0600289B RID: 10395
		private delegate TResult TypeVal3<TResult, T0, T1, T2>(T0 _0, T1 _1, T2 _2);

		// Token: 0x02000834 RID: 2100
		// (Invoke) Token: 0x0600289F RID: 10399
		private delegate void VoidRef3<T0, T1, T2>(ref T0 _0, T1 _1, T2 _2);

		// Token: 0x02000835 RID: 2101
		// (Invoke) Token: 0x060028A3 RID: 10403
		private delegate TResult TypeRef3<TResult, T0, T1, T2>(ref T0 _0, T1 _1, T2 _2);

		// Token: 0x02000836 RID: 2102
		// (Invoke) Token: 0x060028A7 RID: 10407
		private delegate void VoidVal4<T0, T1, T2, T3>(T0 _0, T1 _1, T2 _2, T3 _3);

		// Token: 0x02000837 RID: 2103
		// (Invoke) Token: 0x060028AB RID: 10411
		private delegate TResult TypeVal4<TResult, T0, T1, T2, T3>(T0 _0, T1 _1, T2 _2, T3 _3);

		// Token: 0x02000838 RID: 2104
		// (Invoke) Token: 0x060028AF RID: 10415
		private delegate void VoidRef4<T0, T1, T2, T3>(ref T0 _0, T1 _1, T2 _2, T3 _3);

		// Token: 0x02000839 RID: 2105
		// (Invoke) Token: 0x060028B3 RID: 10419
		private delegate TResult TypeRef4<TResult, T0, T1, T2, T3>(ref T0 _0, T1 _1, T2 _2, T3 _3);

		// Token: 0x0200083A RID: 2106
		// (Invoke) Token: 0x060028B7 RID: 10423
		private delegate void VoidVal5<T0, T1, T2, T3, T4>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4);

		// Token: 0x0200083B RID: 2107
		// (Invoke) Token: 0x060028BB RID: 10427
		private delegate TResult TypeVal5<TResult, T0, T1, T2, T3, T4>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4);

		// Token: 0x0200083C RID: 2108
		// (Invoke) Token: 0x060028BF RID: 10431
		private delegate void VoidRef5<T0, T1, T2, T3, T4>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4);

		// Token: 0x0200083D RID: 2109
		// (Invoke) Token: 0x060028C3 RID: 10435
		private delegate TResult TypeRef5<TResult, T0, T1, T2, T3, T4>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4);

		// Token: 0x0200083E RID: 2110
		// (Invoke) Token: 0x060028C7 RID: 10439
		private delegate void VoidVal6<T0, T1, T2, T3, T4, T5>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5);

		// Token: 0x0200083F RID: 2111
		// (Invoke) Token: 0x060028CB RID: 10443
		private delegate TResult TypeVal6<TResult, T0, T1, T2, T3, T4, T5>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5);

		// Token: 0x02000840 RID: 2112
		// (Invoke) Token: 0x060028CF RID: 10447
		private delegate void VoidRef6<T0, T1, T2, T3, T4, T5>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5);

		// Token: 0x02000841 RID: 2113
		// (Invoke) Token: 0x060028D3 RID: 10451
		private delegate TResult TypeRef6<TResult, T0, T1, T2, T3, T4, T5>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5);

		// Token: 0x02000842 RID: 2114
		// (Invoke) Token: 0x060028D7 RID: 10455
		private delegate void VoidVal7<T0, T1, T2, T3, T4, T5, T6>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6);

		// Token: 0x02000843 RID: 2115
		// (Invoke) Token: 0x060028DB RID: 10459
		private delegate TResult TypeVal7<TResult, T0, T1, T2, T3, T4, T5, T6>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6);

		// Token: 0x02000844 RID: 2116
		// (Invoke) Token: 0x060028DF RID: 10463
		private delegate void VoidRef7<T0, T1, T2, T3, T4, T5, T6>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6);

		// Token: 0x02000845 RID: 2117
		// (Invoke) Token: 0x060028E3 RID: 10467
		private delegate TResult TypeRef7<TResult, T0, T1, T2, T3, T4, T5, T6>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6);

		// Token: 0x02000846 RID: 2118
		// (Invoke) Token: 0x060028E7 RID: 10471
		private delegate void VoidVal8<T0, T1, T2, T3, T4, T5, T6, T7>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7);

		// Token: 0x02000847 RID: 2119
		// (Invoke) Token: 0x060028EB RID: 10475
		private delegate TResult TypeVal8<TResult, T0, T1, T2, T3, T4, T5, T6, T7>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7);

		// Token: 0x02000848 RID: 2120
		// (Invoke) Token: 0x060028EF RID: 10479
		private delegate void VoidRef8<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7);

		// Token: 0x02000849 RID: 2121
		// (Invoke) Token: 0x060028F3 RID: 10483
		private delegate TResult TypeRef8<TResult, T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7);

		// Token: 0x0200084A RID: 2122
		// (Invoke) Token: 0x060028F7 RID: 10487
		private delegate void VoidVal9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8);

		// Token: 0x0200084B RID: 2123
		// (Invoke) Token: 0x060028FB RID: 10491
		private delegate TResult TypeVal9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8);

		// Token: 0x0200084C RID: 2124
		// (Invoke) Token: 0x060028FF RID: 10495
		private delegate void VoidRef9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8);

		// Token: 0x0200084D RID: 2125
		// (Invoke) Token: 0x06002903 RID: 10499
		private delegate TResult TypeRef9<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8);

		// Token: 0x0200084E RID: 2126
		// (Invoke) Token: 0x06002907 RID: 10503
		private delegate void VoidVal10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9);

		// Token: 0x0200084F RID: 2127
		// (Invoke) Token: 0x0600290B RID: 10507
		private delegate TResult TypeVal10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9);

		// Token: 0x02000850 RID: 2128
		// (Invoke) Token: 0x0600290F RID: 10511
		private delegate void VoidRef10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9);

		// Token: 0x02000851 RID: 2129
		// (Invoke) Token: 0x06002913 RID: 10515
		private delegate TResult TypeRef10<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9);

		// Token: 0x02000852 RID: 2130
		// (Invoke) Token: 0x06002917 RID: 10519
		private delegate void VoidVal11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10);

		// Token: 0x02000853 RID: 2131
		// (Invoke) Token: 0x0600291B RID: 10523
		private delegate TResult TypeVal11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10);

		// Token: 0x02000854 RID: 2132
		// (Invoke) Token: 0x0600291F RID: 10527
		private delegate void VoidRef11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10);

		// Token: 0x02000855 RID: 2133
		// (Invoke) Token: 0x06002923 RID: 10531
		private delegate TResult TypeRef11<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10);

		// Token: 0x02000856 RID: 2134
		// (Invoke) Token: 0x06002927 RID: 10535
		private delegate void VoidVal12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11);

		// Token: 0x02000857 RID: 2135
		// (Invoke) Token: 0x0600292B RID: 10539
		private delegate TResult TypeVal12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11);

		// Token: 0x02000858 RID: 2136
		// (Invoke) Token: 0x0600292F RID: 10543
		private delegate void VoidRef12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11);

		// Token: 0x02000859 RID: 2137
		// (Invoke) Token: 0x06002933 RID: 10547
		private delegate TResult TypeRef12<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11);

		// Token: 0x0200085A RID: 2138
		// (Invoke) Token: 0x06002937 RID: 10551
		private delegate void VoidVal13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12);

		// Token: 0x0200085B RID: 2139
		// (Invoke) Token: 0x0600293B RID: 10555
		private delegate TResult TypeVal13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12);

		// Token: 0x0200085C RID: 2140
		// (Invoke) Token: 0x0600293F RID: 10559
		private delegate void VoidRef13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12);

		// Token: 0x0200085D RID: 2141
		// (Invoke) Token: 0x06002943 RID: 10563
		private delegate TResult TypeRef13<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12);

		// Token: 0x0200085E RID: 2142
		// (Invoke) Token: 0x06002947 RID: 10567
		private delegate void VoidVal14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13);

		// Token: 0x0200085F RID: 2143
		// (Invoke) Token: 0x0600294B RID: 10571
		private delegate TResult TypeVal14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13);

		// Token: 0x02000860 RID: 2144
		// (Invoke) Token: 0x0600294F RID: 10575
		private delegate void VoidRef14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13);

		// Token: 0x02000861 RID: 2145
		// (Invoke) Token: 0x06002953 RID: 10579
		private delegate TResult TypeRef14<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13);

		// Token: 0x02000862 RID: 2146
		// (Invoke) Token: 0x06002957 RID: 10583
		private delegate void VoidVal15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14);

		// Token: 0x02000863 RID: 2147
		// (Invoke) Token: 0x0600295B RID: 10587
		private delegate TResult TypeVal15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14);

		// Token: 0x02000864 RID: 2148
		// (Invoke) Token: 0x0600295F RID: 10591
		private delegate void VoidRef15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14);

		// Token: 0x02000865 RID: 2149
		// (Invoke) Token: 0x06002963 RID: 10595
		private delegate TResult TypeRef15<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14);

		// Token: 0x02000866 RID: 2150
		// (Invoke) Token: 0x06002967 RID: 10599
		private delegate void VoidVal16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15);

		// Token: 0x02000867 RID: 2151
		// (Invoke) Token: 0x0600296B RID: 10603
		private delegate TResult TypeVal16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15);

		// Token: 0x02000868 RID: 2152
		// (Invoke) Token: 0x0600296F RID: 10607
		private delegate void VoidRef16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15);

		// Token: 0x02000869 RID: 2153
		// (Invoke) Token: 0x06002973 RID: 10611
		private delegate TResult TypeRef16<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15);
	}
}
