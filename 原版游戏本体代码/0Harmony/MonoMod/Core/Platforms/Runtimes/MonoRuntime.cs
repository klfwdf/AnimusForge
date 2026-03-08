using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000547 RID: 1351
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MonoRuntime : IRuntime
	{
		// Token: 0x17000699 RID: 1689
		// (get) Token: 0x06001E4D RID: 7757 RVA: 0x0005D90B File Offset: 0x0005BB0B
		public RuntimeKind Target
		{
			get
			{
				return RuntimeKind.Mono;
			}
		}

		// Token: 0x1700069A RID: 1690
		// (get) Token: 0x06001E4E RID: 7758 RVA: 0x00062C71 File Offset: 0x00060E71
		public RuntimeFeature Features
		{
			get
			{
				return RuntimeFeature.PreciseGC | RuntimeFeature.GenericSharing | RuntimeFeature.DisableInlining | RuntimeFeature.RequiresMethodPinning | RuntimeFeature.RequiresMethodIdentification | RuntimeFeature.RequiresCustomMethodCompile;
			}
		}

		// Token: 0x1700069B RID: 1691
		// (get) Token: 0x06001E4F RID: 7759 RVA: 0x00062C78 File Offset: 0x00060E78
		public Abi Abi { get; }

		// Token: 0x06001E50 RID: 7760 RVA: 0x00062C80 File Offset: 0x00060E80
		private static TypeClassification LinuxAmd64Classifier(Type type, bool isReturn)
		{
			if (type.IsEnum)
			{
				type = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First<FieldInfo>().FieldType;
			}
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Empty:
				return TypeClassification.InRegister;
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.String:
				return TypeClassification.InRegister;
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return TypeClassification.InRegister;
			case TypeCode.Single:
			case TypeCode.Double:
				return TypeClassification.InRegister;
			}
			if (type.IsPointer)
			{
				return TypeClassification.InRegister;
			}
			if (type.IsByRef)
			{
				return TypeClassification.InRegister;
			}
			if (type == typeof(IntPtr) || type == typeof(UIntPtr))
			{
				return TypeClassification.InRegister;
			}
			if (type == typeof(void))
			{
				return TypeClassification.InRegister;
			}
			Helpers.Assert(type.IsValueType, null, "type.IsValueType");
			return MonoRuntime.ClassifyValueType(type, true);
		}

		// Token: 0x06001E51 RID: 7761 RVA: 0x00062D74 File Offset: 0x00060F74
		private static TypeClassification ClassifyValueType(Type type, bool isReturn)
		{
			int size = type.GetManagedSize();
			bool passOnStack = (!isReturn || size != 8) && (isReturn || size > 16);
			if (size == 0)
			{
				return TypeClassification.InRegister;
			}
			if (!passOnStack)
			{
				int nquads = ((size > 8) ? 2 : 1);
				int args0 = 1;
				int args = 1;
				if (isReturn && nquads != 1)
				{
					args = (args0 = 2);
				}
				if (args0 == 2 || args == 2)
				{
					args0 = 2;
				}
				TypeClassification result;
				if (args0 != 1)
				{
					if (args0 != 2)
					{
						throw new InvalidOperationException();
					}
					result = TypeClassification.OnStack;
				}
				else
				{
					result = TypeClassification.InRegister;
				}
				return result;
			}
			if (!isReturn)
			{
				return TypeClassification.OnStack;
			}
			return TypeClassification.ByReference;
		}

		// Token: 0x06001E52 RID: 7762 RVA: 0x00062DEE File Offset: 0x00060FEE
		private static IEnumerable<FieldInfo> NestedValutypeFields(Type type)
		{
			MonoRuntime.<NestedValutypeFields>d__10 <NestedValutypeFields>d__ = new MonoRuntime.<NestedValutypeFields>d__10(-2);
			<NestedValutypeFields>d__.<>3__type = type;
			return <NestedValutypeFields>d__;
		}

		// Token: 0x06001E53 RID: 7763 RVA: 0x00062E00 File Offset: 0x00061000
		public MonoRuntime(ISystem system)
		{
			this.system = system;
			Abi? defaultAbi = system.DefaultAbi;
			if (defaultAbi != null)
			{
				Abi abi = defaultAbi.GetValueOrDefault();
				OSKind oskind = PlatformDetection.OS.GetKernel();
				bool flag = oskind == OSKind.OSX || oskind == OSKind.Linux;
				if (flag && PlatformDetection.Architecture == ArchitectureKind.x86_64)
				{
					Abi abi2 = abi;
					Classifier classifier;
					if ((classifier = MonoRuntime.<>O.<0>__LinuxAmd64Classifier) == null)
					{
						classifier = (MonoRuntime.<>O.<0>__LinuxAmd64Classifier = new Classifier(MonoRuntime.LinuxAmd64Classifier));
					}
					abi2.Classifier = classifier;
					abi = abi2;
				}
				oskind = PlatformDetection.OS;
				flag = oskind == OSKind.Windows || oskind == OSKind.Wine;
				bool flag2 = flag;
				if (flag2)
				{
					ArchitectureKind architecture = PlatformDetection.Architecture;
					bool flag3 = architecture - ArchitectureKind.x86 <= 1;
					flag2 = flag3;
				}
				if (flag2)
				{
					Abi abi2 = abi;
					abi2.ArgumentOrder = new SpecialArgumentKind[]
					{
						SpecialArgumentKind.ThisPointer,
						SpecialArgumentKind.ReturnBuffer,
						SpecialArgumentKind.UserArguments
					};
					abi = abi2;
				}
				this.Abi = abi;
				return;
			}
			throw new InvalidOperationException("Cannot use Mono system, because the underlying system doesn't provide a default ABI!");
		}

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x06001E54 RID: 7764 RVA: 0x00062F04 File Offset: 0x00061104
		// (remove) Token: 0x06001E55 RID: 7765 RVA: 0x00062F3C File Offset: 0x0006113C
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		public event OnMethodCompiledCallback OnMethodCompiled;

		// Token: 0x06001E56 RID: 7766 RVA: 0x00062F74 File Offset: 0x00061174
		public unsafe void DisableInlining(MethodBase method)
		{
			ushort* iflags = (long)this.GetMethodHandle(method).Value / 2L + 2L;
			ushort* ptr = iflags;
			*ptr |= 8;
		}

		// Token: 0x06001E57 RID: 7767 RVA: 0x00062FA4 File Offset: 0x000611A4
		public RuntimeMethodHandle GetMethodHandle(MethodBase method)
		{
			if (method is DynamicMethod)
			{
				MethodInfo dynamicMethod_CreateDynMethod = MonoRuntime._DynamicMethod_CreateDynMethod;
				if (dynamicMethod_CreateDynMethod != null)
				{
					dynamicMethod_CreateDynMethod.Invoke(method, ArrayEx.Empty<object>());
				}
				if (MonoRuntime._DynamicMethod_mhandle != null)
				{
					return (RuntimeMethodHandle)MonoRuntime._DynamicMethod_mhandle.GetValue(method);
				}
			}
			return method.MethodHandle;
		}

		// Token: 0x06001E58 RID: 7768 RVA: 0x0001B69F File Offset: 0x0001989F
		public bool RequiresGenericContext(MethodBase method)
		{
			return false;
		}

		// Token: 0x06001E59 RID: 7769 RVA: 0x00062FF4 File Offset: 0x000611F4
		[return: Nullable(2)]
		public IDisposable PinMethodIfNeeded(MethodBase method)
		{
			method = this.GetIdentifiable(method);
			MonoRuntime.PrivateMethodPin orAdd = this.pinnedMethods.GetOrAdd(method, delegate(MethodBase m)
			{
				MonoRuntime.PrivateMethodPin pin = new MonoRuntime.PrivateMethodPin(this);
				pin.Pin.Method = m;
				RuntimeMethodHandle handle = (pin.Pin.Handle = this.GetMethodHandle(m));
				this.pinnedHandles[handle] = pin;
				this.DisableInlining(method);
				Type declaringType = method.DeclaringType;
				if (declaringType != null)
				{
					bool isGenericType = declaringType.IsGenericType;
				}
				return pin;
			});
			Interlocked.Increment(ref orAdd.Pin.Count);
			return new MonoRuntime.PinHandle(orAdd);
		}

		// Token: 0x06001E5A RID: 7770 RVA: 0x0006305C File Offset: 0x0006125C
		private void UnpinOnce(MonoRuntime.PrivateMethodPin pin)
		{
			if (Interlocked.Decrement(ref pin.Pin.Count) <= 0)
			{
				MonoRuntime.PrivateMethodPin privateMethodPin;
				this.pinnedMethods.TryRemove(pin.Pin.Method, out privateMethodPin);
				this.pinnedHandles.TryRemove(pin.Pin.Handle, out privateMethodPin);
			}
		}

		// Token: 0x06001E5B RID: 7771 RVA: 0x000630B0 File Offset: 0x000612B0
		public MethodBase GetIdentifiable(MethodBase method)
		{
			MonoRuntime.PrivateMethodPin pin;
			if (!this.pinnedHandles.TryGetValue(this.GetMethodHandle(method), out pin))
			{
				return method;
			}
			return pin.Pin.Method;
		}

		// Token: 0x06001E5C RID: 7772 RVA: 0x000630E0 File Offset: 0x000612E0
		public IntPtr GetMethodEntryPoint(MethodBase method)
		{
			MonoRuntime.PrivateMethodPin pmp;
			if (this.pinnedMethods.TryGetValue(method, out pmp))
			{
				return pmp.Pin.Handle.GetFunctionPointer();
			}
			return this.GetMethodHandle(method).GetFunctionPointer();
		}

		// Token: 0x06001E5D RID: 7773 RVA: 0x00063120 File Offset: 0x00061320
		public void Compile(MethodBase method)
		{
			this.GetMethodHandle(method).GetFunctionPointer();
		}

		// Token: 0x0400128D RID: 4749
		private readonly ISystem system;

		// Token: 0x0400128F RID: 4751
		private static readonly MethodInfo _DynamicMethod_CreateDynMethod = typeof(DynamicMethod).GetMethod("CreateDynMethod", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04001290 RID: 4752
		private static readonly FieldInfo _DynamicMethod_mhandle = typeof(DynamicMethod).GetField("mhandle", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04001291 RID: 4753
		private readonly ConcurrentDictionary<MethodBase, MonoRuntime.PrivateMethodPin> pinnedMethods = new ConcurrentDictionary<MethodBase, MonoRuntime.PrivateMethodPin>();

		// Token: 0x04001292 RID: 4754
		private readonly ConcurrentDictionary<RuntimeMethodHandle, MonoRuntime.PrivateMethodPin> pinnedHandles = new ConcurrentDictionary<RuntimeMethodHandle, MonoRuntime.PrivateMethodPin>();

		// Token: 0x02000548 RID: 1352
		[Nullable(0)]
		private sealed class PrivateMethodPin
		{
			// Token: 0x06001E5F RID: 7775 RVA: 0x00063175 File Offset: 0x00061375
			public PrivateMethodPin(MonoRuntime runtime)
			{
				this.runtime = runtime;
			}

			// Token: 0x06001E60 RID: 7776 RVA: 0x00063184 File Offset: 0x00061384
			public void UnpinOnce()
			{
				this.runtime.UnpinOnce(this);
			}

			// Token: 0x04001293 RID: 4755
			private readonly MonoRuntime runtime;

			// Token: 0x04001294 RID: 4756
			public MonoRuntime.MethodPinInfo Pin;
		}

		// Token: 0x02000549 RID: 1353
		[NullableContext(0)]
		private sealed class PinHandle : IDisposable
		{
			// Token: 0x06001E61 RID: 7777 RVA: 0x00063192 File Offset: 0x00061392
			[NullableContext(1)]
			public PinHandle(MonoRuntime.PrivateMethodPin pin)
			{
				this.pin = pin;
			}

			// Token: 0x06001E62 RID: 7778 RVA: 0x000631A1 File Offset: 0x000613A1
			private void Dispose(bool disposing)
			{
				if (!this.disposedValue)
				{
					this.pin.UnpinOnce();
					this.disposedValue = true;
				}
			}

			// Token: 0x06001E63 RID: 7779 RVA: 0x000631C0 File Offset: 0x000613C0
			~PinHandle()
			{
				this.Dispose(false);
			}

			// Token: 0x06001E64 RID: 7780 RVA: 0x000631F0 File Offset: 0x000613F0
			public void Dispose()
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}

			// Token: 0x04001295 RID: 4757
			[Nullable(1)]
			private readonly MonoRuntime.PrivateMethodPin pin;

			// Token: 0x04001296 RID: 4758
			private bool disposedValue;
		}

		// Token: 0x0200054A RID: 1354
		[Nullable(0)]
		private struct MethodPinInfo
		{
			// Token: 0x06001E65 RID: 7781 RVA: 0x00063200 File Offset: 0x00061400
			public override string ToString()
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 3);
				defaultInterpolatedStringHandler.AppendLiteral("(MethodPinInfo: ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.Count);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<MethodBase>(this.Method);
				defaultInterpolatedStringHandler.AppendLiteral(", 0x");
				defaultInterpolatedStringHandler.AppendFormatted<long>((long)this.Handle.Value, "X");
				defaultInterpolatedStringHandler.AppendLiteral(")");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}

			// Token: 0x04001297 RID: 4759
			public int Count;

			// Token: 0x04001298 RID: 4760
			public MethodBase Method;

			// Token: 0x04001299 RID: 4761
			public RuntimeMethodHandle Handle;
		}

		// Token: 0x0200054B RID: 1355
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x0400129A RID: 4762
			[Nullable(0)]
			public static Classifier <0>__LinuxAmd64Classifier;
		}
	}
}
