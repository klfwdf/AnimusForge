using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Cecil;
using MonoMod.Core.Interop;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x0200052A RID: 1322
	[NullableContext(1)]
	[Nullable(0)]
	internal class Core21Runtime : CoreBaseRuntime
	{
		// Token: 0x17000671 RID: 1649
		// (get) Token: 0x06001DAF RID: 7599 RVA: 0x0006024C File Offset: 0x0005E44C
		public override RuntimeFeature Features
		{
			get
			{
				return base.Features | RuntimeFeature.CompileMethodHook;
			}
		}

		// Token: 0x06001DB0 RID: 7600 RVA: 0x00060256 File Offset: 0x0005E456
		public Core21Runtime(ISystem system)
			: base(system)
		{
		}

		// Token: 0x06001DB1 RID: 7601 RVA: 0x0006026A File Offset: 0x0005E46A
		private static Core21Runtime.JitHookHelpersHolder CreateJitHookHelpers(Core21Runtime self)
		{
			return new Core21Runtime.JitHookHelpersHolder(self);
		}

		// Token: 0x17000672 RID: 1650
		// (get) Token: 0x06001DB2 RID: 7602 RVA: 0x00060272 File Offset: 0x0005E472
		protected Core21Runtime.JitHookHelpersHolder JitHookHelpers
		{
			get
			{
				return Helpers.GetOrInitWithLock<Core21Runtime, Core21Runtime.JitHookHelpersHolder>(ref this.lazyJitHookHelpers, this.sync, Core21Runtime.createJitHookHelpersFunc, this);
			}
		}

		// Token: 0x17000673 RID: 1651
		// (get) Token: 0x06001DB3 RID: 7603 RVA: 0x0006028B File Offset: 0x0005E48B
		protected virtual Guid ExpectedJitVersion
		{
			get
			{
				return Core21Runtime.JitVersionGuid;
			}
		}

		// Token: 0x17000674 RID: 1652
		// (get) Token: 0x06001DB4 RID: 7604 RVA: 0x000411A5 File Offset: 0x0003F3A5
		protected virtual int VtableIndexICorJitCompilerGetVersionGuid
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x17000675 RID: 1653
		// (get) Token: 0x06001DB5 RID: 7605 RVA: 0x0001B69F File Offset: 0x0001989F
		protected virtual int VtableIndexICorJitCompilerCompileMethod
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x17000676 RID: 1654
		// (get) Token: 0x06001DB6 RID: 7606 RVA: 0x00060292 File Offset: 0x0005E492
		protected virtual CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
		{
			get
			{
				return CoreCLR.V21.InvokeCompileMethodPtr;
			}
		}

		// Token: 0x06001DB7 RID: 7607 RVA: 0x00060299 File Offset: 0x0005E499
		protected virtual Delegate CastCompileHookToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V21.CompileMethodDelegate>();
		}

		// Token: 0x06001DB8 RID: 7608 RVA: 0x000602A1 File Offset: 0x0005E4A1
		[NullableContext(0)]
		protected unsafe static IntPtr* GetVTableEntry(IntPtr @object, int index)
		{
			return *(IntPtr*)(void*)@object / (IntPtr)sizeof(IntPtr) + index * sizeof(IntPtr);
		}

		// Token: 0x06001DB9 RID: 7609 RVA: 0x000602B4 File Offset: 0x0005E4B4
		protected unsafe static IntPtr ReadObjectVTable(IntPtr @object, int index)
		{
			return *Core21Runtime.GetVTableEntry(@object, index);
		}

		// Token: 0x06001DBA RID: 7610 RVA: 0x000602C0 File Offset: 0x0005E4C0
		protected unsafe void CheckVersionGuid(IntPtr jit)
		{
			method getVersionIdentPtr = (void*)Core21Runtime.ReadObjectVTable(jit, this.VtableIndexICorJitCompilerGetVersionGuid);
			method system.Void_u0020(System.IntPtr,System.Guid*) = getVersionIdentPtr;
			Guid guid;
			calli(System.Void(System.IntPtr,System.Guid*), jit, &guid, system.Void_u0020(System.IntPtr,System.Guid*));
			bool flag = guid == this.ExpectedJitVersion;
			bool value = flag;
			bool flag2;
			AssertionInterpolatedStringHandler assertionInterpolatedStringHandler = new AssertionInterpolatedStringHandler(66, 2, flag, ref flag2);
			if (flag2)
			{
				assertionInterpolatedStringHandler.AppendLiteral("JIT version does not match expected JIT version! ");
				assertionInterpolatedStringHandler.AppendLiteral("expected: ");
				assertionInterpolatedStringHandler.AppendFormatted<Guid>(this.ExpectedJitVersion);
				assertionInterpolatedStringHandler.AppendLiteral(", got: ");
				assertionInterpolatedStringHandler.AppendFormatted<Guid>(guid);
			}
			Helpers.Assert(value, ref assertionInterpolatedStringHandler, "guid == ExpectedJitVersion");
		}

		// Token: 0x06001DBB RID: 7611 RVA: 0x00060350 File Offset: 0x0005E550
		protected unsafe override void InstallManagedJitHook(IntPtr jit)
		{
			this.CheckVersionGuid(jit);
			IntPtr* compileMethodSlot = Core21Runtime.GetVTableEntry(jit, this.VtableIndexICorJitCompilerCompileMethod);
			IntPtr compileMethod = base.EHManagedToNative(*compileMethodSlot, out this.m2nHookHelper);
			Delegate ourCompileMethodDelegate = this.CastCompileHookToRealType(this.CreateCompileMethodDelegate(compileMethod));
			this.ourCompileMethod = ourCompileMethodDelegate;
			IntPtr ourCompileMethodPtr = base.EHNativeToManaged(Marshal.GetFunctionPointerForDelegate(ourCompileMethodDelegate), out this.n2mHookHelper);
			this.InvokeCompileMethodToPrepare(ourCompileMethodPtr);
			int num = sizeof(IntPtr);
			Span<byte> ptrData = new Span<byte>(stackalloc byte[(UIntPtr)num], num);
			MemoryMarshal.Write<IntPtr>(ptrData, ref ourCompileMethodPtr);
			base.System.PatchData(PatchTargetKind.ReadOnly, (IntPtr)((void*)compileMethodSlot), ptrData, default(Span<byte>));
		}

		// Token: 0x06001DBC RID: 7612 RVA: 0x000603F4 File Offset: 0x0005E5F4
		protected unsafe virtual void InvokeCompileMethodToPrepare(IntPtr method)
		{
			method invokeCompileMethod = this.InvokeCompileMethodPtr.InvokeCompileMethod;
			CoreCLR.V21.CORINFO_METHOD_INFO methodInfo;
			byte* nativeStart;
			uint nativeSize;
			object obj = calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*), method, IntPtr.Zero, IntPtr.Zero, &methodInfo, 0, &nativeStart, &nativeSize, invokeCompileMethod);
		}

		// Token: 0x06001DBD RID: 7613 RVA: 0x0006042E File Offset: 0x0005E62E
		protected virtual Delegate CreateCompileMethodDelegate(IntPtr compileMethod)
		{
			return new <>f__AnonymousDelegate0(new Core21Runtime.JitHookDelegateHolder(this, this.InvokeCompileMethodPtr, compileMethod).CompileMethodHook);
		}

		// Token: 0x06001DBE RID: 7614 RVA: 0x00060448 File Offset: 0x0005E648
		protected virtual MethodInfo MakeCreateRuntimeMethodInfoStub(Type methodHandleInternal)
		{
			Type[] runtimeMethodInfoStubCtorArgs = new Type[]
			{
				typeof(IntPtr),
				typeof(object)
			};
			Type runtimeMethodInfoStub = typeof(RuntimeMethodHandle).Assembly.GetType("System.RuntimeMethodInfoStub");
			ConstructorInfo runtimeMethodInfoStubCtor = runtimeMethodInfoStub.GetConstructor(runtimeMethodInfoStubCtorArgs);
			MethodInfo runtimeMethodInfoStubCtorWrapper;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition("new RuntimeMethodInfoStub", runtimeMethodInfoStub, runtimeMethodInfoStubCtorArgs))
			{
				ILGenerator ilgenerator = dmd.GetILGenerator();
				ilgenerator.Emit(OpCodes.Ldarg_0);
				ilgenerator.Emit(OpCodes.Ldarg_1);
				ilgenerator.Emit(OpCodes.Newobj, runtimeMethodInfoStubCtor);
				ilgenerator.Emit(OpCodes.Ret);
				runtimeMethodInfoStubCtorWrapper = dmd.Generate();
			}
			return runtimeMethodInfoStubCtorWrapper;
		}

		// Token: 0x06001DBF RID: 7615 RVA: 0x00060500 File Offset: 0x0005E700
		protected virtual MethodInfo GetOrCreateGetTypeFromHandleUnsafe()
		{
			MethodInfo method = typeof(Type).GetMethod("GetTypeFromHandleUnsafe", (BindingFlags)(-1));
			if (method != null)
			{
				return method;
			}
			Assembly assembly;
			using (ModuleDefinition module = ModuleDefinition.CreateModule("MonoMod.Core.Platforms.Runtimes.Core21Runtime+Helpers", new ModuleParameters
			{
				Kind = ModuleKind.Dll
			}))
			{
				TypeDefinition sysType = new TypeDefinition("System", "Type", Mono.Cecil.TypeAttributes.Abstract)
				{
					BaseType = module.TypeSystem.Object
				};
				module.Types.Add(sysType);
				MethodDefinition targetMethod = new MethodDefinition("GetTypeFromHandleUnsafe", Mono.Cecil.MethodAttributes.FamANDAssem | Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.Static, module.ImportReference(typeof(Type)))
				{
					IsInternalCall = true
				};
				targetMethod.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(IntPtr))));
				sysType.Methods.Add(targetMethod);
				assembly = ReflectionHelper.Load(module);
			}
			this.MakeAssemblySystemAssembly(assembly);
			return assembly.GetType("System.Type").GetMethod("GetTypeFromHandleUnsafe", (BindingFlags)(-1));
		}

		// Token: 0x06001DC0 RID: 7616 RVA: 0x00060608 File Offset: 0x0005E808
		protected unsafe virtual void MakeAssemblySystemAssembly(Assembly assembly)
		{
			IntPtr value = (IntPtr)Core21Runtime.RuntimeAssemblyPtrField.GetValue(assembly);
			int domOffset = IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + 4 + IntPtr.Size + IntPtr.Size + 4 + 4 + IntPtr.Size + IntPtr.Size + 4 + 4 + IntPtr.Size;
			if (IntPtr.Size == 8)
			{
				domOffset += 4;
			}
			IntPtr value2 = *(IntPtr*)((byte*)(void*)value + domOffset);
			int pAssemOffset = IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size;
			IntPtr value3 = *(IntPtr*)((byte*)(void*)value2 + pAssemOffset);
			int peAssemOffset = IntPtr.Size + (FxCoreBaseRuntime.IsDebugClr ? (IntPtr.Size + 4 + 4 + 4 + IntPtr.Size + 4) : 0) + IntPtr.Size + IntPtr.Size + 4 + 4 + IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + 4;
			if (FxCoreBaseRuntime.IsDebugClr && IntPtr.Size == 8)
			{
				peAssemOffset += 8;
			}
			int* flags = (int*)((byte*)(void*)value3 + peAssemOffset);
			*flags |= 1;
		}

		// Token: 0x04001233 RID: 4659
		private static readonly Func<Core21Runtime, Core21Runtime.JitHookHelpersHolder> createJitHookHelpersFunc = new Func<Core21Runtime, Core21Runtime.JitHookHelpersHolder>(Core21Runtime.CreateJitHookHelpers);

		// Token: 0x04001234 RID: 4660
		private readonly object sync = new object();

		// Token: 0x04001235 RID: 4661
		[Nullable(2)]
		private Core21Runtime.JitHookHelpersHolder lazyJitHookHelpers;

		// Token: 0x04001236 RID: 4662
		private static readonly Guid JitVersionGuid = new Guid(195102408U, 33184, 16511, 153, 161, 146, 132, 72, 193, 235, 98);

		// Token: 0x04001237 RID: 4663
		[Nullable(2)]
		private Delegate ourCompileMethod;

		// Token: 0x04001238 RID: 4664
		[Nullable(2)]
		private IDisposable n2mHookHelper;

		// Token: 0x04001239 RID: 4665
		[Nullable(2)]
		private IDisposable m2nHookHelper;

		// Token: 0x0400123A RID: 4666
		private protected static readonly FieldInfo RuntimeAssemblyPtrField = Type.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0200052B RID: 1323
		[Nullable(0)]
		private sealed class JitHookDelegateHolder
		{
			// Token: 0x06001DC2 RID: 7618 RVA: 0x00060784 File Offset: 0x0005E984
			public unsafe JitHookDelegateHolder(Core21Runtime runtime, CoreCLR.InvokeCompileMethodPtr icmp, IntPtr compileMethod)
			{
				this.Runtime = runtime;
				this.NativeExceptionHelper = runtime.NativeExceptionHelper;
				this.JitHookHelpers = runtime.JitHookHelpers;
				this.InvokeCompileMethodPtr = icmp;
				this.CompileMethodPtr = compileMethod;
				method invokeCompileMethod = icmp.InvokeCompileMethod;
				CoreCLR.V21.CORINFO_METHOD_INFO methodInfo;
				byte* nativeStart;
				uint nativeSize;
				object obj = calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, &methodInfo, 0, &nativeStart, &nativeSize, invokeCompileMethod);
				MarshalEx.SetLastPInvokeError(MarshalEx.GetLastPInvokeError());
				INativeExceptionHelper neh = this.NativeExceptionHelper;
				if (neh != null)
				{
					this.GetNativeExceptionSlot = neh.GetExceptionSlot;
					this.GetNativeExceptionSlot();
				}
				int num = Core21Runtime.JitHookDelegateHolder.hookEntrancy;
				Core21Runtime.JitHookDelegateHolder.hookEntrancy = 0;
			}

			// Token: 0x06001DC3 RID: 7619 RVA: 0x00060828 File Offset: 0x0005EA28
			[NullableContext(0)]
			public unsafe CoreCLR.CorJitResult CompileMethodHook(IntPtr jit, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** pNativeEntry, uint* pNativeSizeOfCode)
			{
				if (jit == IntPtr.Zero)
				{
					return CoreCLR.CorJitResult.CORJIT_OK;
				}
				*(IntPtr*)pNativeEntry = (IntPtr)((UIntPtr)0);
				*pNativeSizeOfCode = 0U;
				int lastError = MarshalEx.GetLastPInvokeError();
				IntPtr nativeException = (IntPtr)0;
				GetExceptionSlot getNex = this.GetNativeExceptionSlot;
				IntPtr* pNEx = ((getNex != null) ? getNex() : null);
				Core21Runtime.JitHookDelegateHolder.hookEntrancy++;
				CoreCLR.CorJitResult result2;
				try
				{
					method invokeCompileMethod = this.InvokeCompileMethodPtr.InvokeCompileMethod;
					CoreCLR.CorJitResult result = calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*), this.CompileMethodPtr, jit, corJitInfo, methodInfo, flags, pNativeEntry, pNativeSizeOfCode, invokeCompileMethod);
					if (pNEx != null && (nativeException = *pNEx) != 0)
					{
						bool flag;
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(59, 1, ref flag);
						if (flag)
						{
							debugLogWarningStringHandler.AppendLiteral("Native exception caught in JIT by exception helper (ex: 0x");
							debugLogWarningStringHandler.AppendFormatted<IntPtr>(nativeException, "x16");
							debugLogWarningStringHandler.AppendLiteral(")");
						}
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
						result2 = result;
					}
					else
					{
						if (Core21Runtime.JitHookDelegateHolder.hookEntrancy == 1)
						{
							try
							{
								RuntimeTypeHandle[] genericClassArgs = null;
								RuntimeTypeHandle[] genericMethodArgs = null;
								if (methodInfo->args.sigInst.classInst != null)
								{
									genericClassArgs = new RuntimeTypeHandle[methodInfo->args.sigInst.classInstCount];
									for (int i = 0; i < genericClassArgs.Length; i++)
									{
										genericClassArgs[i] = this.JitHookHelpers.GetTypeFromNativeHandle(methodInfo->args.sigInst.classInst[(IntPtr)i * (IntPtr)sizeof(IntPtr) / (IntPtr)sizeof(IntPtr)]).TypeHandle;
									}
								}
								if (methodInfo->args.sigInst.methInst != null)
								{
									genericMethodArgs = new RuntimeTypeHandle[methodInfo->args.sigInst.methInstCount];
									for (int j = 0; j < genericMethodArgs.Length; j++)
									{
										genericMethodArgs[j] = this.JitHookHelpers.GetTypeFromNativeHandle(methodInfo->args.sigInst.methInst[(IntPtr)j * (IntPtr)sizeof(IntPtr) / (IntPtr)sizeof(IntPtr)]).TypeHandle;
									}
								}
								RuntimeTypeHandle declaringType = this.JitHookHelpers.GetDeclaringTypeOfMethodHandle(methodInfo->ftn).TypeHandle;
								RuntimeMethodHandle method = this.JitHookHelpers.CreateHandleForHandlePointer(methodInfo->ftn);
								this.Runtime.OnMethodCompiledCore(declaringType, method, new ReadOnlyMemory<RuntimeTypeHandle>?(genericClassArgs), new ReadOnlyMemory<RuntimeTypeHandle>?(genericMethodArgs), (IntPtr)(*(IntPtr*)pNativeEntry), (IntPtr)(*(IntPtr*)pNativeEntry), (ulong)(*pNativeSizeOfCode));
							}
							catch
							{
							}
						}
						result2 = result;
					}
				}
				finally
				{
					Core21Runtime.JitHookDelegateHolder.hookEntrancy--;
					if (pNEx != null)
					{
						*pNEx = nativeException;
					}
					MarshalEx.SetLastPInvokeError(lastError);
				}
				return result2;
			}

			// Token: 0x0400123B RID: 4667
			public readonly Core21Runtime Runtime;

			// Token: 0x0400123C RID: 4668
			[Nullable(2)]
			public readonly INativeExceptionHelper NativeExceptionHelper;

			// Token: 0x0400123D RID: 4669
			[Nullable(2)]
			public readonly GetExceptionSlot GetNativeExceptionSlot;

			// Token: 0x0400123E RID: 4670
			public readonly Core21Runtime.JitHookHelpersHolder JitHookHelpers;

			// Token: 0x0400123F RID: 4671
			public readonly CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr;

			// Token: 0x04001240 RID: 4672
			public readonly IntPtr CompileMethodPtr;

			// Token: 0x04001241 RID: 4673
			[ThreadStatic]
			private static int hookEntrancy;
		}

		// Token: 0x0200052C RID: 1324
		[NullableContext(0)]
		protected sealed class JitHookHelpersHolder
		{
			// Token: 0x06001DC4 RID: 7620 RVA: 0x00060AB4 File Offset: 0x0005ECB4
			public RuntimeMethodHandle CreateHandleForHandlePointer(IntPtr handle)
			{
				return this.CreateRuntimeMethodHandle(this.CreateRuntimeMethodInfoStub(handle, this.MethodHandle_GetLoaderAllocator(handle)));
			}

			// Token: 0x06001DC5 RID: 7621 RVA: 0x00060ADC File Offset: 0x0005ECDC
			[NullableContext(1)]
			public JitHookHelpersHolder(Core21Runtime runtime)
			{
				MethodInfo getLoaderAllocator = typeof(RuntimeMethodHandle).GetMethod("GetLoaderAllocator", BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo invokeWrapper;
				using (DynamicMethodDefinition dmd = new DynamicMethodDefinition("MethodHandle_GetLoaderAllocator", typeof(object), new Type[] { typeof(IntPtr) }))
				{
					ILGenerator ilgenerator = dmd.GetILGenerator();
					Type paramType = getLoaderAllocator.GetParameters().First<ParameterInfo>().ParameterType;
					ilgenerator.Emit(OpCodes.Ldarga_S, 0);
					ilgenerator.Emit(OpCodes.Ldobj, paramType);
					ilgenerator.Emit(OpCodes.Call, getLoaderAllocator);
					ilgenerator.Emit(OpCodes.Ret);
					invokeWrapper = dmd.Generate();
				}
				this.MethodHandle_GetLoaderAllocator = invokeWrapper.CreateDelegate<Core21Runtime.JitHookHelpersHolder.MethodHandle_GetLoaderAllocatorD>();
				MethodInfo getTypeFromHandleUnsafe = runtime.GetOrCreateGetTypeFromHandleUnsafe();
				this.GetTypeFromNativeHandle = getTypeFromHandleUnsafe.CreateDelegate<Core21Runtime.JitHookHelpersHolder.GetTypeFromNativeHandleD>();
				Type methodHandleInternal = typeof(RuntimeMethodHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
				MethodInfo getDeclaringType = typeof(RuntimeMethodHandle).GetMethod("GetDeclaringType", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { methodHandleInternal }, null);
				MethodInfo invokeWrapper2;
				using (DynamicMethodDefinition dmd2 = new DynamicMethodDefinition("GetDeclaringTypeOfMethodHandle", typeof(Type), new Type[] { typeof(IntPtr) }))
				{
					ILGenerator ilgenerator2 = dmd2.GetILGenerator();
					ilgenerator2.Emit(OpCodes.Ldarga_S, 0);
					ilgenerator2.Emit(OpCodes.Ldobj, methodHandleInternal);
					ilgenerator2.Emit(OpCodes.Call, getDeclaringType);
					ilgenerator2.Emit(OpCodes.Ret);
					invokeWrapper2 = dmd2.Generate();
				}
				this.GetDeclaringTypeOfMethodHandle = invokeWrapper2.CreateDelegate<Core21Runtime.JitHookHelpersHolder.GetDeclaringTypeOfMethodHandleD>();
				this.CreateRuntimeMethodInfoStub = runtime.MakeCreateRuntimeMethodInfoStub(methodHandleInternal).CreateDelegate<Core21Runtime.JitHookHelpersHolder.CreateRuntimeMethodInfoStubD>();
				ConstructorInfo ctor = typeof(RuntimeMethodHandle).GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First<ConstructorInfo>();
				MethodInfo ctorWrapper;
				using (DynamicMethodDefinition dmd3 = new DynamicMethodDefinition("new RuntimeMethodHandle", typeof(RuntimeMethodHandle), new Type[] { typeof(object) }))
				{
					ILGenerator ilgenerator3 = dmd3.GetILGenerator();
					ilgenerator3.Emit(OpCodes.Ldarg_0);
					ilgenerator3.Emit(OpCodes.Newobj, ctor);
					ilgenerator3.Emit(OpCodes.Ret);
					ctorWrapper = dmd3.Generate();
				}
				this.CreateRuntimeMethodHandle = ctorWrapper.CreateDelegate<Core21Runtime.JitHookHelpersHolder.CreateRuntimeMethodHandleD>();
			}

			// Token: 0x04001242 RID: 4674
			[Nullable(1)]
			public readonly Core21Runtime.JitHookHelpersHolder.MethodHandle_GetLoaderAllocatorD MethodHandle_GetLoaderAllocator;

			// Token: 0x04001243 RID: 4675
			[Nullable(1)]
			public readonly Core21Runtime.JitHookHelpersHolder.CreateRuntimeMethodInfoStubD CreateRuntimeMethodInfoStub;

			// Token: 0x04001244 RID: 4676
			[Nullable(1)]
			public readonly Core21Runtime.JitHookHelpersHolder.CreateRuntimeMethodHandleD CreateRuntimeMethodHandle;

			// Token: 0x04001245 RID: 4677
			[Nullable(1)]
			public readonly Core21Runtime.JitHookHelpersHolder.GetDeclaringTypeOfMethodHandleD GetDeclaringTypeOfMethodHandle;

			// Token: 0x04001246 RID: 4678
			[Nullable(1)]
			public readonly Core21Runtime.JitHookHelpersHolder.GetTypeFromNativeHandleD GetTypeFromNativeHandle;

			// Token: 0x0200052D RID: 1325
			// (Invoke) Token: 0x06001DC7 RID: 7623
			public delegate object MethodHandle_GetLoaderAllocatorD(IntPtr methodHandle);

			// Token: 0x0200052E RID: 1326
			// (Invoke) Token: 0x06001DCB RID: 7627
			public delegate object CreateRuntimeMethodInfoStubD(IntPtr methodHandle, object loaderAllocator);

			// Token: 0x0200052F RID: 1327
			// (Invoke) Token: 0x06001DCF RID: 7631
			public delegate RuntimeMethodHandle CreateRuntimeMethodHandleD(object runtimeMethodInfo);

			// Token: 0x02000530 RID: 1328
			// (Invoke) Token: 0x06001DD3 RID: 7635
			public delegate Type GetDeclaringTypeOfMethodHandleD(IntPtr methodHandle);

			// Token: 0x02000531 RID: 1329
			// (Invoke) Token: 0x06001DD7 RID: 7639
			public delegate Type GetTypeFromNativeHandleD(IntPtr handle);
		}
	}
}
