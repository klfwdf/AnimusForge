using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000545 RID: 1349
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class FxCoreBaseRuntime : IRuntime
	{
		// Token: 0x17000692 RID: 1682
		// (get) Token: 0x06001E2F RID: 7727
		public abstract RuntimeKind Target { get; }

		// Token: 0x17000693 RID: 1683
		// (get) Token: 0x06001E30 RID: 7728 RVA: 0x00061EEE File Offset: 0x000600EE
		public virtual RuntimeFeature Features
		{
			get
			{
				return RuntimeFeature.PreciseGC | RuntimeFeature.GenericSharing | RuntimeFeature.DisableInlining | RuntimeFeature.RequiresMethodIdentification | RuntimeFeature.RequiresBodyThunkWalking | RuntimeFeature.HasKnownABI | RuntimeFeature.RequiresCustomMethodCompile;
			}
		}

		// Token: 0x17000694 RID: 1684
		// (get) Token: 0x06001E31 RID: 7729 RVA: 0x00061EF8 File Offset: 0x000600F8
		public Abi Abi
		{
			get
			{
				Abi? abiCore = this.AbiCore;
				if (abiCore == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 1);
					defaultInterpolatedStringHandler.AppendLiteral("The runtime's Abi field is not set, and is unusable (");
					defaultInterpolatedStringHandler.AppendFormatted<Type>(base.GetType());
					defaultInterpolatedStringHandler.AppendLiteral(")");
					throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return abiCore.GetValueOrDefault();
			}
		}

		// Token: 0x06001E32 RID: 7730 RVA: 0x00061F58 File Offset: 0x00060158
		private static TypeClassification ClassifyRyuJitX86(Type type, bool isReturn)
		{
			while (!type.IsPrimitive || type.IsEnum)
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fields == null || fields.Length != 1)
				{
					break;
				}
				type = fields[0].FieldType;
			}
			TypeCode typeCode = Type.GetTypeCode(type);
			bool flag = typeCode == TypeCode.Boolean || typeCode - TypeCode.SByte <= 5;
			if (flag || type == typeof(IntPtr) || type == typeof(UIntPtr))
			{
				return TypeClassification.InRegister;
			}
			flag = isReturn;
			if (flag)
			{
				bool flag2 = typeCode - TypeCode.Int64 <= 1;
				flag = flag2;
			}
			if (flag)
			{
				return TypeClassification.InRegister;
			}
			flag = isReturn;
			if (flag)
			{
				bool flag2 = typeCode - TypeCode.Single <= 1;
				flag = flag2;
			}
			if (flag)
			{
				return TypeClassification.InRegister;
			}
			if (!isReturn)
			{
				return TypeClassification.OnStack;
			}
			int typeSize = type.GetManagedSize();
			flag = typeSize - 1 <= 1 || typeSize == 4;
			if (flag)
			{
				return TypeClassification.InRegister;
			}
			return TypeClassification.ByReference;
		}

		// Token: 0x06001E33 RID: 7731 RVA: 0x0006202C File Offset: 0x0006022C
		protected FxCoreBaseRuntime()
		{
			if (PlatformDetection.Architecture == ArchitectureKind.x86)
			{
				ReadOnlyMemory<SpecialArgumentKind> argumentOrder = new SpecialArgumentKind[]
				{
					SpecialArgumentKind.ThisPointer,
					SpecialArgumentKind.ReturnBuffer,
					SpecialArgumentKind.UserArguments,
					SpecialArgumentKind.GenericContext
				};
				Classifier classifier;
				if ((classifier = FxCoreBaseRuntime.<>O.<0>__ClassifyRyuJitX86) == null)
				{
					classifier = (FxCoreBaseRuntime.<>O.<0>__ClassifyRyuJitX86 = new Classifier(FxCoreBaseRuntime.ClassifyRyuJitX86));
				}
				this.AbiCore = new Abi?(new Abi(argumentOrder, classifier, true));
			}
		}

		// Token: 0x06001E34 RID: 7732 RVA: 0x0006208C File Offset: 0x0006028C
		protected static Abi AbiForCoreFx45X64(Abi baseAbi)
		{
			Abi result = baseAbi;
			result.ArgumentOrder = new SpecialArgumentKind[]
			{
				SpecialArgumentKind.ThisPointer,
				SpecialArgumentKind.ReturnBuffer,
				SpecialArgumentKind.GenericContext,
				SpecialArgumentKind.UserArguments
			};
			return result;
		}

		// Token: 0x06001E35 RID: 7733 RVA: 0x000620BC File Offset: 0x000602BC
		protected static Abi AbiForCoreFx45ARM64(Abi baseAbi)
		{
			Abi result = baseAbi;
			result.ArgumentOrder = new SpecialArgumentKind[]
			{
				SpecialArgumentKind.ThisPointer,
				SpecialArgumentKind.GenericContext,
				SpecialArgumentKind.UserArguments
			};
			return result;
		}

		// Token: 0x06001E36 RID: 7734 RVA: 0x000620E6 File Offset: 0x000602E6
		public virtual MethodBase GetIdentifiable(MethodBase method)
		{
			if (FxCoreBaseRuntime.RTDynamicMethod_m_owner != null && method.GetType() == FxCoreBaseRuntime.RTDynamicMethod)
			{
				return (MethodBase)FxCoreBaseRuntime.RTDynamicMethod_m_owner.GetValue(method);
			}
			return method;
		}

		// Token: 0x06001E37 RID: 7735 RVA: 0x0006211C File Offset: 0x0006031C
		public virtual RuntimeMethodHandle GetMethodHandle(MethodBase method)
		{
			DynamicMethod dm = method as DynamicMethod;
			if (dm != null)
			{
				RuntimeMethodHandle handle;
				if (this.TryGetDMHandle(dm, out handle) && this.TryInvokeBclCompileMethod(handle))
				{
					return handle;
				}
				try
				{
					dm.CreateDelegate(typeof(MulticastDelegate));
				}
				catch
				{
				}
				if (this.TryGetDMHandle(dm, out handle))
				{
					return handle;
				}
				if (FxCoreBaseRuntime._DynamicMethod_m_method != null)
				{
					return (RuntimeMethodHandle)FxCoreBaseRuntime._DynamicMethod_m_method.GetValue(method);
				}
			}
			return method.MethodHandle;
		}

		// Token: 0x06001E38 RID: 7736 RVA: 0x000621A4 File Offset: 0x000603A4
		public bool RequiresGenericContext(MethodBase method)
		{
			Type declaringType = method.DeclaringType ?? typeof(object);
			if (!method.IsGenericMethod && !declaringType.IsGenericType)
			{
				return false;
			}
			IEnumerable<Type> source = (method.IsGenericMethod ? method.GetGenericArguments() : Type.EmptyTypes);
			Func<Type, bool> predicate;
			if ((predicate = FxCoreBaseRuntime.<>O.<1>__IsGenericSharedType) == null)
			{
				predicate = (FxCoreBaseRuntime.<>O.<1>__IsGenericSharedType = new Func<Type, bool>(FxCoreBaseRuntime.IsGenericSharedType));
			}
			if (source.Any(predicate))
			{
				return true;
			}
			if (!method.IsGenericMethod && !method.IsStatic && !declaringType.IsValueType && (!declaringType.IsInterface || method.IsAbstract))
			{
				return false;
			}
			IEnumerable<Type> source2 = (declaringType.IsGenericType ? declaringType.GetGenericArguments() : Type.EmptyTypes);
			Func<Type, bool> predicate2;
			if ((predicate2 = FxCoreBaseRuntime.<>O.<1>__IsGenericSharedType) == null)
			{
				predicate2 = (FxCoreBaseRuntime.<>O.<1>__IsGenericSharedType = new Func<Type, bool>(FxCoreBaseRuntime.IsGenericSharedType));
			}
			return source2.Any(predicate2);
		}

		// Token: 0x06001E39 RID: 7737 RVA: 0x0006227C File Offset: 0x0006047C
		private static bool IsGenericSharedType(Type type)
		{
			if (type.IsPrimitive)
			{
				return false;
			}
			if (!type.IsValueType)
			{
				return true;
			}
			if (type.IsGenericType)
			{
				IEnumerable<Type> genericArguments = type.GetGenericArguments();
				Func<Type, bool> predicate;
				if ((predicate = FxCoreBaseRuntime.<>O.<1>__IsGenericSharedType) == null)
				{
					predicate = (FxCoreBaseRuntime.<>O.<1>__IsGenericSharedType = new Func<Type, bool>(FxCoreBaseRuntime.IsGenericSharedType));
				}
				return genericArguments.Any(predicate);
			}
			return false;
		}

		// Token: 0x17000695 RID: 1685
		// (get) Token: 0x06001E3A RID: 7738 RVA: 0x000622D0 File Offset: 0x000604D0
		private Func<DynamicMethod, RuntimeMethodHandle> GetDMHandleHelper
		{
			get
			{
				Func<DynamicMethod, RuntimeMethodHandle> result;
				if ((result = this.lazyGetDmHandleHelper) == null)
				{
					result = (this.lazyGetDmHandleHelper = FxCoreBaseRuntime.CreateGetDMHandleHelper());
				}
				return result;
			}
		}

		// Token: 0x17000696 RID: 1686
		// (get) Token: 0x06001E3B RID: 7739 RVA: 0x000622F5 File Offset: 0x000604F5
		private static bool CanCreateGetDMHandleHelper
		{
			get
			{
				return FxCoreBaseRuntime._DynamicMethod_GetMethodDescriptor != null;
			}
		}

		// Token: 0x06001E3C RID: 7740 RVA: 0x00062304 File Offset: 0x00060504
		private static Func<DynamicMethod, RuntimeMethodHandle> CreateGetDMHandleHelper()
		{
			Helpers.Assert(FxCoreBaseRuntime.CanCreateGetDMHandleHelper, null, "CanCreateGetDMHandleHelper");
			Func<DynamicMethod, RuntimeMethodHandle> result;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition("get DynamicMethod RuntimeMethodHandle", typeof(RuntimeMethodHandle), new Type[] { typeof(DynamicMethod) }))
			{
				ModuleDefinition module = dmd.Module;
				ILProcessor ilprocessor = dmd.GetILProcessor();
				Helpers.Assert(FxCoreBaseRuntime._DynamicMethod_GetMethodDescriptor != null, null, "_DynamicMethod_GetMethodDescriptor is not null");
				ilprocessor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
				ilprocessor.Emit(Mono.Cecil.Cil.OpCodes.Call, FxCoreBaseRuntime._DynamicMethod_GetMethodDescriptor);
				ilprocessor.Emit(Mono.Cecil.Cil.OpCodes.Ret);
				result = dmd.Generate().CreateDelegate<Func<DynamicMethod, RuntimeMethodHandle>>();
			}
			return result;
		}

		// Token: 0x17000697 RID: 1687
		// (get) Token: 0x06001E3D RID: 7741 RVA: 0x000623BC File Offset: 0x000605BC
		private Action<RuntimeMethodHandle> BclCompileMethodHelper
		{
			get
			{
				Action<RuntimeMethodHandle> result;
				if ((result = this.lazyBclCompileMethod) == null)
				{
					result = (this.lazyBclCompileMethod = FxCoreBaseRuntime.CreateBclCompileMethodHelper());
				}
				return result;
			}
		}

		// Token: 0x17000698 RID: 1688
		// (get) Token: 0x06001E3E RID: 7742 RVA: 0x000623E1 File Offset: 0x000605E1
		private static bool CanCreateBclCompileMethodHelper
		{
			get
			{
				return FxCoreBaseRuntime._RuntimeHelpers__CompileMethod != null && (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIntPtr || (FxCoreBaseRuntime._RuntimeMethodHandle_m_value != null && (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIRuntimeMethodInfo || (FxCoreBaseRuntime._IRuntimeMethodInfo_get_Value != null && FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesRuntimeMethodHandleInternal))));
			}
		}

		// Token: 0x06001E3F RID: 7743 RVA: 0x00062418 File Offset: 0x00060618
		private static Action<RuntimeMethodHandle> CreateBclCompileMethodHelper()
		{
			Helpers.Assert(FxCoreBaseRuntime.CanCreateBclCompileMethodHelper, null, "CanCreateBclCompileMethodHelper");
			Action<RuntimeMethodHandle> result;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition("invoke RuntimeHelpers.CompileMethod", null, new Type[] { typeof(RuntimeMethodHandle) }))
			{
				ModuleDefinition module = dmd.Module;
				ILProcessor il = dmd.GetILProcessor();
				il.Emit(Mono.Cecil.Cil.OpCodes.Ldarga_S, 0);
				if (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIntPtr)
				{
					il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(FxCoreBaseRuntime._RuntimeMethodHandle_get_Value));
					il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(FxCoreBaseRuntime._RuntimeHelpers__CompileMethod));
					il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
					result = dmd.Generate().CreateDelegate<Action<RuntimeMethodHandle>>();
				}
				else
				{
					Helpers.Assert(FxCoreBaseRuntime._RuntimeMethodHandle_m_value != null, null, "_RuntimeMethodHandle_m_value is not null");
					il.Emit(Mono.Cecil.Cil.OpCodes.Ldfld, module.ImportReference(FxCoreBaseRuntime._RuntimeMethodHandle_m_value));
					if (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIRuntimeMethodInfo)
					{
						il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(FxCoreBaseRuntime._RuntimeHelpers__CompileMethod));
						il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
						result = dmd.Generate().CreateDelegate<Action<RuntimeMethodHandle>>();
					}
					else
					{
						Helpers.Assert(FxCoreBaseRuntime._IRuntimeMethodInfo_get_Value != null, null, "_IRuntimeMethodInfo_get_Value is not null");
						il.Emit(Mono.Cecil.Cil.OpCodes.Callvirt, module.ImportReference(FxCoreBaseRuntime._IRuntimeMethodInfo_get_Value));
						if (!FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesRuntimeMethodHandleInternal)
						{
							Helpers.Assert(false, "Tried to generate BCL CompileMethod helper when it's not possible? (This should never happen if CanCreateBclCompileMethodHelper is correct)", "false");
							throw new InvalidOperationException("UNREACHABLE");
						}
						il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(FxCoreBaseRuntime._RuntimeHelpers__CompileMethod));
						il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
						result = dmd.Generate().CreateDelegate<Action<RuntimeMethodHandle>>();
					}
				}
			}
			return result;
		}

		// Token: 0x06001E40 RID: 7744 RVA: 0x000625C8 File Offset: 0x000607C8
		private bool TryGetDMHandle(DynamicMethod dm, out RuntimeMethodHandle handle)
		{
			if (FxCoreBaseRuntime.CanCreateGetDMHandleHelper)
			{
				handle = this.GetDMHandleHelper(dm);
				return true;
			}
			return FxCoreBaseRuntime.TryGetDMHandleRefl(dm, out handle);
		}

		// Token: 0x06001E41 RID: 7745 RVA: 0x000625EC File Offset: 0x000607EC
		protected bool TryInvokeBclCompileMethod(RuntimeMethodHandle handle)
		{
			if (FxCoreBaseRuntime.CanCreateBclCompileMethodHelper)
			{
				this.BclCompileMethodHelper(handle);
				return true;
			}
			return FxCoreBaseRuntime.TryInvokeBclCompileMethodRefl(handle);
		}

		// Token: 0x06001E42 RID: 7746 RVA: 0x00062609 File Offset: 0x00060809
		private static bool TryGetDMHandleRefl(DynamicMethod dm, out RuntimeMethodHandle handle)
		{
			handle = default(RuntimeMethodHandle);
			if (FxCoreBaseRuntime._DynamicMethod_GetMethodDescriptor == null)
			{
				return false;
			}
			handle = (RuntimeMethodHandle)FxCoreBaseRuntime._DynamicMethod_GetMethodDescriptor.Invoke(dm, null);
			return true;
		}

		// Token: 0x06001E43 RID: 7747 RVA: 0x00062634 File Offset: 0x00060834
		private static bool TryInvokeBclCompileMethodRefl(RuntimeMethodHandle handle)
		{
			if (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod == null)
			{
				return false;
			}
			if (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIntPtr)
			{
				FxCoreBaseRuntime._RuntimeHelpers__CompileMethod.Invoke(null, new object[] { handle.Value });
				return true;
			}
			if (FxCoreBaseRuntime._RuntimeMethodHandle_m_value == null)
			{
				return false;
			}
			object rtMethodInfo = FxCoreBaseRuntime._RuntimeMethodHandle_m_value.GetValue(handle);
			if (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIRuntimeMethodInfo)
			{
				FxCoreBaseRuntime._RuntimeHelpers__CompileMethod.Invoke(null, new object[] { rtMethodInfo });
				return true;
			}
			if (FxCoreBaseRuntime._IRuntimeMethodInfo_get_Value == null)
			{
				return false;
			}
			object rtMethodHandleInternal = FxCoreBaseRuntime._IRuntimeMethodInfo_get_Value.Invoke(rtMethodInfo, null);
			if (FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesRuntimeMethodHandleInternal)
			{
				FxCoreBaseRuntime._RuntimeHelpers__CompileMethod.Invoke(null, new object[] { rtMethodHandleInternal });
				return true;
			}
			bool flag;
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(81, 1, ref flag);
			if (flag)
			{
				debugLogErrorStringHandler.AppendLiteral("Could not compile DynamicMethod using BCL reflection (_CompileMethod first arg: ");
				debugLogErrorStringHandler.AppendFormatted<Type>(FxCoreBaseRuntime.RtH_CM_FirstArg);
				debugLogErrorStringHandler.AppendLiteral(")");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler);
			return false;
		}

		// Token: 0x06001E44 RID: 7748 RVA: 0x0006271C File Offset: 0x0006091C
		public virtual void Compile(MethodBase method)
		{
			RuntimeMethodHandle handle = this.GetMethodHandle(method);
			RuntimeHelpers.PrepareMethod(handle);
			Helpers.Assert(this.TryInvokeBclCompileMethod(handle), null, "TryInvokeBclCompileMethod(handle)");
			if (method.IsVirtual)
			{
				Type declaringType = method.DeclaringType;
				if (declaringType != null && declaringType.IsValueType)
				{
					if (this.TryGetCanonicalMethodHandle(ref handle))
					{
						Helpers.Assert(this.TryInvokeBclCompileMethod(handle), null, "TryInvokeBclCompileMethod(handle)");
						return;
					}
					try
					{
						method.CreateDelegate<Action>();
					}
					catch (Exception e)
					{
						bool flag;
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler debugLogSpamStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler(91, 1, ref flag);
						if (flag)
						{
							debugLogSpamStringHandler.AppendLiteral("Caught exception while attempting to compile real entry point of virtual method on struct: ");
							debugLogSpamStringHandler.AppendFormatted<Exception>(e);
						}
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Spam(ref debugLogSpamStringHandler);
					}
				}
			}
		}

		// Token: 0x06001E45 RID: 7749 RVA: 0x0001B69F File Offset: 0x0001989F
		protected virtual bool TryGetCanonicalMethodHandle(ref RuntimeMethodHandle handle)
		{
			return false;
		}

		// Token: 0x06001E46 RID: 7750 RVA: 0x0002B871 File Offset: 0x00029A71
		[return: Nullable(2)]
		public virtual IDisposable PinMethodIfNeeded(MethodBase method)
		{
			return null;
		}

		// Token: 0x06001E47 RID: 7751 RVA: 0x000627C8 File Offset: 0x000609C8
		public unsafe virtual void DisableInlining(MethodBase method)
		{
			RuntimeMethodHandle handle = this.GetMethodHandle(method);
			int offset = (FxCoreBaseRuntime.IsDebugClr ? (IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size) : 0) + 2 + 1 + 1 + 2;
			ushort* m_wFlags = (ushort*)((byte*)(void*)handle.Value + offset);
			ushort* ptr = m_wFlags;
			*ptr |= 8192;
		}

		// Token: 0x06001E48 RID: 7752 RVA: 0x00062828 File Offset: 0x00060A28
		public virtual IntPtr GetMethodEntryPoint(MethodBase method)
		{
			method = this.GetIdentifiable(method);
			if (method.IsVirtual)
			{
				Type declaringType = method.DeclaringType;
				if (declaringType != null && declaringType.IsValueType)
				{
					return method.GetLdftnPointer();
				}
			}
			return this.GetMethodHandle(method).GetFunctionPointer();
		}

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06001E49 RID: 7753 RVA: 0x00062870 File Offset: 0x00060A70
		// (remove) Token: 0x06001E4A RID: 7754 RVA: 0x000628A8 File Offset: 0x00060AA8
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		public event OnMethodCompiledCallback OnMethodCompiled;

		// Token: 0x06001E4B RID: 7755 RVA: 0x000628E0 File Offset: 0x00060AE0
		[NullableContext(0)]
		protected unsafe virtual void OnMethodCompiledCore(RuntimeTypeHandle declaringType, RuntimeMethodHandle methodHandle, ReadOnlyMemory<RuntimeTypeHandle>? genericTypeArguments, ReadOnlyMemory<RuntimeTypeHandle>? genericMethodArguments, IntPtr methodBodyStart, IntPtr methodBodyRw, ulong methodBodySize)
		{
			try
			{
				Type declType = Type.GetTypeFromHandle(declaringType);
				if (genericTypeArguments != null)
				{
					ReadOnlyMemory<RuntimeTypeHandle> gte = genericTypeArguments.GetValueOrDefault();
					if (declType.IsGenericTypeDefinition)
					{
						Type[] typeArr = new Type[gte.Length];
						for (int i = 0; i < gte.Length; i++)
						{
							typeArr[i] = Type.GetTypeFromHandle(*gte.Span[i]);
						}
						declType = declType.MakeGenericType(typeArr);
					}
				}
				MethodBase method = MethodBase.GetMethodFromHandle(methodHandle, declType.TypeHandle);
				if (method == null)
				{
					foreach (MethodInfo meth in declType.GetMethods((BindingFlags)(-1)))
					{
						if (meth.MethodHandle.Value == methodHandle.Value)
						{
							method = meth;
							break;
						}
					}
				}
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler debugLogSpamStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler(28, 3, ref flag);
				if (flag)
				{
					debugLogSpamStringHandler.AppendLiteral("JIT compiled ");
					debugLogSpamStringHandler.AppendFormatted<MethodBase>(method);
					debugLogSpamStringHandler.AppendLiteral(" to 0x");
					debugLogSpamStringHandler.AppendFormatted<IntPtr>(methodBodyStart, "x16");
					debugLogSpamStringHandler.AppendLiteral(" (rw: 0x");
					debugLogSpamStringHandler.AppendFormatted<IntPtr>(methodBodyRw, "x16");
					debugLogSpamStringHandler.AppendLiteral(")");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Spam(ref debugLogSpamStringHandler);
				try
				{
					OnMethodCompiledCallback onMethodCompiled = this.OnMethodCompiled;
					if (onMethodCompiled != null)
					{
						onMethodCompiled(methodHandle, method, methodBodyStart, methodBodyRw, methodBodySize);
					}
				}
				catch (Exception e)
				{
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(40, 1, ref flag);
					if (flag)
					{
						debugLogErrorStringHandler.AppendLiteral("Error executing OnMethodCompiled event: ");
						debugLogErrorStringHandler.AppendFormatted<Exception>(e);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler);
				}
			}
			catch (Exception e2)
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler2 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(31, 1, ref flag);
				if (flag)
				{
					debugLogErrorStringHandler2.AppendLiteral("Error in OnMethodCompiledCore: ");
					debugLogErrorStringHandler2.AppendFormatted<Exception>(e2);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler2);
			}
		}

		// Token: 0x06001E4C RID: 7756 RVA: 0x00062ACC File Offset: 0x00060CCC
		// Note: this type is marked as 'beforefieldinit'.
		static FxCoreBaseRuntime()
		{
			Type rtdynamicMethod = FxCoreBaseRuntime.RTDynamicMethod;
			FxCoreBaseRuntime.RTDynamicMethod_m_owner = ((rtdynamicMethod != null) ? rtdynamicMethod.GetField("m_owner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
			FxCoreBaseRuntime._DynamicMethod_m_method = typeof(DynamicMethod).GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
			FxCoreBaseRuntime._DynamicMethod_GetMethodDescriptor = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
			FxCoreBaseRuntime._RuntimeMethodHandle_get_Value = typeof(RuntimeMethodHandle).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
			FxCoreBaseRuntime._RuntimeMethodHandle_m_value = typeof(RuntimeMethodHandle).GetField("m_value", BindingFlags.Instance | BindingFlags.NonPublic);
			Type type = typeof(RuntimeMethodHandle).Assembly.GetType("System.IRuntimeMethodInfo");
			FxCoreBaseRuntime._IRuntimeMethodInfo_get_Value = ((type != null) ? type.GetMethod("get_Value") : null);
			FxCoreBaseRuntime._RuntimeHelpers__CompileMethod = typeof(RuntimeHelpers).GetMethod("_CompileMethod", BindingFlags.Static | BindingFlags.NonPublic) ?? typeof(RuntimeHelpers).GetMethod("CompileMethod", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo runtimeHelpers__CompileMethod = FxCoreBaseRuntime._RuntimeHelpers__CompileMethod;
			FxCoreBaseRuntime.RtH_CM_FirstArg = ((runtimeHelpers__CompileMethod != null) ? runtimeHelpers__CompileMethod.GetParameters()[0].ParameterType : null);
			Type rtH_CM_FirstArg = FxCoreBaseRuntime.RtH_CM_FirstArg;
			FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIntPtr = ((rtH_CM_FirstArg != null) ? rtH_CM_FirstArg.FullName : null) == "System.IntPtr";
			Type rtH_CM_FirstArg2 = FxCoreBaseRuntime.RtH_CM_FirstArg;
			FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesIRuntimeMethodInfo = ((rtH_CM_FirstArg2 != null) ? rtH_CM_FirstArg2.FullName : null) == "System.IRuntimeMethodInfo";
			Type rtH_CM_FirstArg3 = FxCoreBaseRuntime.RtH_CM_FirstArg;
			FxCoreBaseRuntime._RuntimeHelpers__CompileMethod_TakesRuntimeMethodHandleInternal = ((rtH_CM_FirstArg3 != null) ? rtH_CM_FirstArg3.FullName : null) == "System.RuntimeMethodHandleInternal";
			bool isEnabled;
			FxCoreBaseRuntime.IsDebugClr = Switches.TryGetSwitchEnabled("DebugClr", out isEnabled) && isEnabled;
		}

		// Token: 0x04001279 RID: 4729
		protected Abi? AbiCore;

		// Token: 0x0400127A RID: 4730
		[Nullable(2)]
		private static readonly Type RTDynamicMethod = typeof(DynamicMethod).GetNestedType("RTDynamicMethod", BindingFlags.NonPublic);

		// Token: 0x0400127B RID: 4731
		[Nullable(2)]
		private static readonly FieldInfo RTDynamicMethod_m_owner;

		// Token: 0x0400127C RID: 4732
		[Nullable(2)]
		private static readonly FieldInfo _DynamicMethod_m_method;

		// Token: 0x0400127D RID: 4733
		[Nullable(2)]
		private static readonly MethodInfo _DynamicMethod_GetMethodDescriptor;

		// Token: 0x0400127E RID: 4734
		[Nullable(2)]
		private static readonly MethodInfo _RuntimeMethodHandle_get_Value;

		// Token: 0x0400127F RID: 4735
		[Nullable(2)]
		private static readonly FieldInfo _RuntimeMethodHandle_m_value;

		// Token: 0x04001280 RID: 4736
		[Nullable(2)]
		private static readonly MethodInfo _IRuntimeMethodInfo_get_Value;

		// Token: 0x04001281 RID: 4737
		[Nullable(2)]
		private static readonly MethodInfo _RuntimeHelpers__CompileMethod;

		// Token: 0x04001282 RID: 4738
		[Nullable(2)]
		private static readonly Type RtH_CM_FirstArg;

		// Token: 0x04001283 RID: 4739
		private static readonly bool _RuntimeHelpers__CompileMethod_TakesIntPtr;

		// Token: 0x04001284 RID: 4740
		private static readonly bool _RuntimeHelpers__CompileMethod_TakesIRuntimeMethodInfo;

		// Token: 0x04001285 RID: 4741
		private static readonly bool _RuntimeHelpers__CompileMethod_TakesRuntimeMethodHandleInternal;

		// Token: 0x04001286 RID: 4742
		[Nullable(new byte[] { 2, 1 })]
		private Func<DynamicMethod, RuntimeMethodHandle> lazyGetDmHandleHelper;

		// Token: 0x04001287 RID: 4743
		[Nullable(2)]
		private Action<RuntimeMethodHandle> lazyBclCompileMethod;

		// Token: 0x04001288 RID: 4744
		protected static readonly bool IsDebugClr;

		// Token: 0x02000546 RID: 1350
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x0400128A RID: 4746
			[Nullable(0)]
			public static Classifier <0>__ClassifyRyuJitX86;

			// Token: 0x0400128B RID: 4747
			[Nullable(0)]
			public static Func<Type, bool> <1>__IsGenericSharedType;
		}
	}
}
