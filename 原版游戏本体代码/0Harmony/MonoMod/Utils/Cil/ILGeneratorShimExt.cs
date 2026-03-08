using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils.Cil
{
	// Token: 0x020008FF RID: 2303
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ILGeneratorShimExt
	{
		// Token: 0x0600307E RID: 12414 RVA: 0x000A7308 File Offset: 0x000A5508
		static ILGeneratorShimExt()
		{
			foreach (MethodInfo method in typeof(ILGenerator).GetMethods())
			{
				if (!(method.Name != "Emit"))
				{
					ParameterInfo[] args = method.GetParameters();
					if (args.Length == 2 && !(args[0].ParameterType != typeof(OpCode)))
					{
						ILGeneratorShimExt._Emitters[args[1].ParameterType] = method;
					}
				}
			}
			foreach (MethodInfo method2 in typeof(ILGeneratorShim).GetMethods())
			{
				if (!(method2.Name != "Emit"))
				{
					ParameterInfo[] args2 = method2.GetParameters();
					if (args2.Length == 2 && !(args2[0].ParameterType != typeof(OpCode)))
					{
						ILGeneratorShimExt._EmittersShim[args2[1].ParameterType] = method2;
					}
				}
			}
		}

		// Token: 0x0600307F RID: 12415 RVA: 0x000A740B File Offset: 0x000A560B
		public static ILGeneratorShim GetProxiedShim(this ILGenerator il)
		{
			FieldInfo field = Helpers.ThrowIfNull<ILGenerator>(il, "il").GetType().GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return (ILGeneratorShim)((field != null) ? field.GetValue(il) : null);
		}

		// Token: 0x06003080 RID: 12416 RVA: 0x000A743B File Offset: 0x000A563B
		public static T GetProxiedShim<[Nullable(0)] T>(this ILGenerator il) where T : ILGeneratorShim
		{
			return (T)((object)il.GetProxiedShim());
		}

		// Token: 0x06003081 RID: 12417 RVA: 0x000A7448 File Offset: 0x000A5648
		[return: Nullable(2)]
		public static object DynEmit(this ILGenerator il, OpCode opcode, object operand)
		{
			return il.DynEmit(new object[] { opcode, operand });
		}

		// Token: 0x06003082 RID: 12418 RVA: 0x000A7464 File Offset: 0x000A5664
		[return: Nullable(2)]
		public static object DynEmit(this ILGenerator il, object[] emitArgs)
		{
			Helpers.ThrowIfArgumentNull<object[]>(emitArgs, "emitArgs");
			Type operandType = emitArgs[1].GetType();
			object target = il.GetProxiedShim() ?? il;
			Dictionary<Type, MethodInfo> emitters = ((target is ILGeneratorShim) ? ILGeneratorShimExt._EmittersShim : ILGeneratorShimExt._Emitters);
			MethodInfo emit;
			if (!emitters.TryGetValue(operandType, out emit))
			{
				emit = emitters.FirstOrDefault((KeyValuePair<Type, MethodInfo> kvp) => kvp.Key.IsAssignableFrom(operandType)).Value;
			}
			if (emit == null)
			{
				throw new InvalidOperationException("Unexpected unemittable operand type " + operandType.FullName);
			}
			return emit.Invoke(target, emitArgs);
		}

		// Token: 0x04003C0C RID: 15372
		private static readonly Dictionary<Type, MethodInfo> _Emitters = new Dictionary<Type, MethodInfo>();

		// Token: 0x04003C0D RID: 15373
		private static readonly Dictionary<Type, MethodInfo> _EmittersShim = new Dictionary<Type, MethodInfo>();
	}
}
