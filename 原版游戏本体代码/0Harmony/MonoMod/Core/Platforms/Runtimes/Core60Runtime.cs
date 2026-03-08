using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MonoMod.Core.Interop;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000535 RID: 1333
	[NullableContext(1)]
	[Nullable(0)]
	internal class Core60Runtime : Core50Runtime
	{
		// Token: 0x06001DE8 RID: 7656 RVA: 0x00060DDC File Offset: 0x0005EFDC
		public Core60Runtime(ISystem system, IArchitecture arch)
			: base(system)
		{
			this.arch = arch;
		}

		// Token: 0x1700067D RID: 1661
		// (get) Token: 0x06001DE9 RID: 7657 RVA: 0x00060DEC File Offset: 0x0005EFEC
		protected override Guid ExpectedJitVersion
		{
			get
			{
				return Core60Runtime.JitVersionGuid;
			}
		}

		// Token: 0x06001DEA RID: 7658 RVA: 0x00060DF3 File Offset: 0x0005EFF3
		protected override void InstallJitHook(IntPtr jit)
		{
			if ((base.System.Features & SystemFeature.MayUseNativeJitHooks) != SystemFeature.None && this.InstallNativeJitHook(jit))
			{
				return;
			}
			this.InstallManagedJitHook(jit);
		}

		// Token: 0x06001DEB RID: 7659 RVA: 0x00060E18 File Offset: 0x0005F018
		protected unsafe virtual bool InstallNativeJitHook(IntPtr jit)
		{
			Core60Runtime.NativeJitHookConfig* hookConfig = this.GetNativeJitHookConfig();
			if (hookConfig == null)
			{
				return false;
			}
			base.CheckVersionGuid(jit);
			IntPtr* compileMethodSlot = Core21Runtime.GetVTableEntry(jit, this.VtableIndexICorJitCompilerCompileMethod);
			Delegate compileHookPost = this.CastCompileMethodHookPostToRealType(this.CreateCompileMethodHookPostDelegate());
			this.ourCompileMethodHookPost = compileHookPost;
			IntPtr ourCompileMethodHookPostPtr = Marshal.GetFunctionPointerForDelegate(compileHookPost);
			method invokeCompileMethodHookPost = CoreCLR.V60.InvokeCompileMethodHookPostPtr.InvokeCompileMethodHookPost;
			CoreCLR.V21.CORINFO_METHOD_INFO methodInfo;
			byte* nativeStart;
			uint nativeSize;
			CoreCLR.V60.AllocMemArgs args;
			object obj = calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*,MonoMod.Core.Interop.CoreCLR/CorJitResult,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*), ourCompileMethodHookPostPtr, IntPtr.Zero, IntPtr.Zero, &methodInfo, 0, &nativeStart, &nativeSize, 0, &args, invokeCompileMethodHookPost);
			hookConfig->compileMethod = *compileMethodSlot;
			hookConfig->compileMethodHookPost = ourCompileMethodHookPostPtr;
			IntPtr ourCompileMethodHookPtr = hookConfig->compileMethodHook;
			int num = sizeof(IntPtr);
			Span<byte> ptrData = new Span<byte>(stackalloc byte[(UIntPtr)num], num);
			MemoryMarshal.Write<IntPtr>(ptrData, ref ourCompileMethodHookPtr);
			base.System.PatchData(PatchTargetKind.ReadOnly, (IntPtr)((void*)compileMethodSlot), ptrData, default(Span<byte>));
			Core60Runtime.CompileMethodPatchPrimer();
			return true;
		}

		// Token: 0x06001DEC RID: 7660 RVA: 0x0001B842 File Offset: 0x00019A42
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void CompileMethodPatchPrimer()
		{
		}

		// Token: 0x06001DED RID: 7661 RVA: 0x00060EF2 File Offset: 0x0005F0F2
		protected override Delegate CreateCompileMethodDelegate(IntPtr compileMethod)
		{
			return new <>f__AnonymousDelegate0(new Core60Runtime.JitHookDelegateHolder(this, this.InvokeCompileMethodPtr, compileMethod).CompileMethodHook);
		}

		// Token: 0x06001DEE RID: 7662 RVA: 0x00060F0C File Offset: 0x0005F10C
		[NullableContext(0)]
		private unsafe void CompileMethodHookPostCommon(CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, byte** nativeEntry, uint* nativeSizeOfCode, IntPtr rwEntry)
		{
			RuntimeTypeHandle[] genericClassArgs = null;
			RuntimeTypeHandle[] genericMethodArgs = null;
			if (methodInfo->args.sigInst.classInst != null)
			{
				genericClassArgs = new RuntimeTypeHandle[methodInfo->args.sigInst.classInstCount];
				for (int i = 0; i < genericClassArgs.Length; i++)
				{
					genericClassArgs[i] = base.JitHookHelpers.GetTypeFromNativeHandle(methodInfo->args.sigInst.classInst[(IntPtr)i * (IntPtr)sizeof(IntPtr) / (IntPtr)sizeof(IntPtr)]).TypeHandle;
				}
			}
			if (methodInfo->args.sigInst.methInst != null)
			{
				genericMethodArgs = new RuntimeTypeHandle[methodInfo->args.sigInst.methInstCount];
				for (int j = 0; j < genericMethodArgs.Length; j++)
				{
					genericMethodArgs[j] = base.JitHookHelpers.GetTypeFromNativeHandle(methodInfo->args.sigInst.methInst[(IntPtr)j * (IntPtr)sizeof(IntPtr) / (IntPtr)sizeof(IntPtr)]).TypeHandle;
				}
			}
			RuntimeTypeHandle declaringType = base.JitHookHelpers.GetDeclaringTypeOfMethodHandle(methodInfo->ftn).TypeHandle;
			RuntimeMethodHandle method = base.JitHookHelpers.CreateHandleForHandlePointer(methodInfo->ftn);
			this.OnMethodCompiledCore(declaringType, method, new ReadOnlyMemory<RuntimeTypeHandle>?(genericClassArgs), new ReadOnlyMemory<RuntimeTypeHandle>?(genericMethodArgs), (IntPtr)(*(IntPtr*)nativeEntry), rwEntry, (ulong)(*nativeSizeOfCode));
		}

		// Token: 0x06001DEF RID: 7663 RVA: 0x0006105F File Offset: 0x0005F25F
		[NullableContext(0)]
		protected unsafe virtual Core60Runtime.NativeJitHookConfig* GetNativeJitHookConfig()
		{
			return (Core60Runtime.NativeJitHookConfig*)(void*)base.System.GetNativeJitHookConfig(60);
		}

		// Token: 0x06001DF0 RID: 7664 RVA: 0x00061073 File Offset: 0x0005F273
		protected virtual Delegate CreateCompileMethodHookPostDelegate()
		{
			return new <>f__AnonymousDelegate1(new Core60Runtime.JitHookPostDelegateHolder(this).CompileMethodHookPost);
		}

		// Token: 0x06001DF1 RID: 7665 RVA: 0x00061086 File Offset: 0x0005F286
		protected virtual Delegate CastCompileMethodHookPostToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V60.CompileMethodHookPostDelegate>();
		}

		// Token: 0x06001DF2 RID: 7666 RVA: 0x00061090 File Offset: 0x0005F290
		[NullableContext(0)]
		protected unsafe virtual void PatchWrapperVtable(IntPtr* vtbl)
		{
			this.allocMemDelegate = this.CastAllocMemToRealType(this.CreateAllocMemDelegate());
			IntPtr allocMemFnPtr = base.EHNativeToManaged(Marshal.GetFunctionPointerForDelegate(this.allocMemDelegate), out this.n2mAllocMemHelper);
			method invokeAllocMem = this.InvokeAllocMemPtr.InvokeAllocMem;
			calli(System.Void(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*), allocMemFnPtr, IntPtr.Zero, (UIntPtr)0, invokeAllocMem);
			vtbl[(IntPtr)this.VtableIndexICorJitInfoAllocMem * (IntPtr)sizeof(IntPtr) / (IntPtr)sizeof(IntPtr)] = allocMemFnPtr;
		}

		// Token: 0x1700067E RID: 1662
		// (get) Token: 0x06001DF3 RID: 7667 RVA: 0x000610F6 File Offset: 0x0005F2F6
		protected virtual int VtableIndexICorJitInfoAllocMem
		{
			get
			{
				return 156;
			}
		}

		// Token: 0x1700067F RID: 1663
		// (get) Token: 0x06001DF4 RID: 7668 RVA: 0x000610FD File Offset: 0x0005F2FD
		protected virtual int ICorJitInfoFullVtableCount
		{
			get
			{
				return 173;
			}
		}

		// Token: 0x17000680 RID: 1664
		// (get) Token: 0x06001DF5 RID: 7669 RVA: 0x00061104 File Offset: 0x0005F304
		protected virtual CoreCLR.InvokeAllocMemPtr InvokeAllocMemPtr
		{
			get
			{
				return CoreCLR.V60.InvokeAllocMemPtr;
			}
		}

		// Token: 0x17000681 RID: 1665
		// (get) Token: 0x06001DF6 RID: 7670 RVA: 0x0006110B File Offset: 0x0005F30B
		protected override CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
		{
			get
			{
				return CoreCLR.V60.InvokeCompileMethodPtr;
			}
		}

		// Token: 0x06001DF7 RID: 7671 RVA: 0x00061112 File Offset: 0x0005F312
		protected override Delegate CastCompileHookToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V60.CompileMethodDelegate>();
		}

		// Token: 0x06001DF8 RID: 7672 RVA: 0x0006111A File Offset: 0x0005F31A
		protected virtual Delegate CastAllocMemToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V60.AllocMemDelegate>();
		}

		// Token: 0x06001DF9 RID: 7673 RVA: 0x00061122 File Offset: 0x0005F322
		protected virtual Delegate CreateAllocMemDelegate()
		{
			return new <>f__AnonymousDelegate2(new Core60Runtime.AllocMemDelegateHolder(this, this.InvokeAllocMemPtr).AllocMemHook);
		}

		// Token: 0x04001249 RID: 4681
		private readonly IArchitecture arch;

		// Token: 0x0400124A RID: 4682
		private static readonly Guid JitVersionGuid = new Guid(1590910040U, 34171, 18653, 168, 24, 124, 1, 54, 220, 159, 115);

		// Token: 0x0400124B RID: 4683
		[Nullable(2)]
		private Delegate ourCompileMethodHookPost;

		// Token: 0x0400124C RID: 4684
		[Nullable(2)]
		private Delegate allocMemDelegate;

		// Token: 0x0400124D RID: 4685
		[Nullable(2)]
		private IDisposable n2mAllocMemHelper;

		// Token: 0x02000536 RID: 1334
		[NullableContext(0)]
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct ICorJitInfoWrapper
		{
			// Token: 0x17000682 RID: 1666
			public IntPtr this[int index]
			{
				get
				{
					return Unsafe.Add<IntPtr>(Unsafe.As<ulong, IntPtr>(ref this.data.FixedElementField), index);
				}
			}

			// Token: 0x0400124E RID: 4686
			public IntPtr Vtbl;

			// Token: 0x0400124F RID: 4687
			public unsafe IntPtr** Wrapped;

			// Token: 0x04001250 RID: 4688
			public const int HotCodeRW = 0;

			// Token: 0x04001251 RID: 4689
			public const int ColdCodeRW = 1;

			// Token: 0x04001252 RID: 4690
			private const int DataQWords = 4;

			// Token: 0x04001253 RID: 4691
			[FixedBuffer(typeof(ulong), 4)]
			private Core60Runtime.ICorJitInfoWrapper.<data>e__FixedBuffer data;

			// Token: 0x02000537 RID: 1335
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 32)]
			public struct <data>e__FixedBuffer
			{
				// Token: 0x04001254 RID: 4692
				public ulong FixedElementField;
			}
		}

		// Token: 0x02000538 RID: 1336
		[Nullable(0)]
		private sealed class JitHookDelegateHolder
		{
			// Token: 0x06001DFC RID: 7676 RVA: 0x00061194 File Offset: 0x0005F394
			public unsafe JitHookDelegateHolder(Core60Runtime runtime, CoreCLR.InvokeCompileMethodPtr icmp, IntPtr compileMethod)
			{
				this.Runtime = runtime;
				this.NativeExceptionHelper = runtime.NativeExceptionHelper;
				this.JitHookHelpers = runtime.JitHookHelpers;
				this.InvokeCompileMethodPtr = icmp;
				this.CompileMethodPtr = compileMethod;
				this.iCorJitInfoWrapperVtbl = Marshal.AllocHGlobal(IntPtr.Size * runtime.ICorJitInfoFullVtableCount);
				this.iCorJitInfoWrapperAllocs = this.Runtime.arch.CreateNativeVtableProxyStubs(this.iCorJitInfoWrapperVtbl, runtime.ICorJitInfoFullVtableCount);
				this.Runtime.PatchWrapperVtable((IntPtr*)(void*)this.iCorJitInfoWrapperVtbl);
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(42, 1, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("Allocated ICorJitInfo wrapper vtable at 0x");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.iCorJitInfoWrapperVtbl, "x16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
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
				int num = Core60Runtime.JitHookDelegateHolder.hookEntrancy;
				Core60Runtime.JitHookDelegateHolder.hookEntrancy = 0;
			}

			// Token: 0x06001DFD RID: 7677 RVA: 0x000612C4 File Offset: 0x0005F4C4
			[NullableContext(0)]
			public unsafe CoreCLR.CorJitResult CompileMethodHook(IntPtr jit, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode)
			{
				if (jit == IntPtr.Zero)
				{
					return CoreCLR.CorJitResult.CORJIT_OK;
				}
				*(IntPtr*)nativeEntry = (IntPtr)((UIntPtr)0);
				*nativeSizeOfCode = 0U;
				int lastError = MarshalEx.GetLastPInvokeError();
				IntPtr nativeException = (IntPtr)0;
				GetExceptionSlot getNex = this.GetNativeExceptionSlot;
				IntPtr* pNEx = ((getNex != null) ? getNex() : null);
				Core60Runtime.JitHookDelegateHolder.hookEntrancy++;
				CoreCLR.CorJitResult result2;
				try
				{
					bool flag;
					if (Core60Runtime.JitHookDelegateHolder.hookEntrancy == 1)
					{
						try
						{
							IAllocatedMemory corJitWrapper = this.iCorJitInfoWrapper.Value;
							if (corJitWrapper == null)
							{
								AllocationRequest allocReq = new AllocationRequest(sizeof(Core60Runtime.ICorJitInfoWrapper))
								{
									Alignment = IntPtr.Size,
									Executable = false
								};
								IAllocatedMemory alloc;
								if (this.Runtime.System.MemoryAllocator.TryAllocate(allocReq, out alloc))
								{
									corJitWrapper = (this.iCorJitInfoWrapper.Value = alloc);
								}
							}
							if (corJitWrapper != null)
							{
								Core60Runtime.ICorJitInfoWrapper* wrapper = (Core60Runtime.ICorJitInfoWrapper*)(void*)corJitWrapper.BaseAddress;
								wrapper->Vtbl = this.iCorJitInfoWrapperVtbl;
								wrapper->Wrapped = (IntPtr**)(void*)corJitInfo;
								*(*wrapper)[0] = IntPtr.Zero;
								*(*wrapper)[1] = IntPtr.Zero;
								corJitInfo = (IntPtr)((void*)wrapper);
							}
						}
						catch (Exception e)
						{
							try
							{
								<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(48, 1, ref flag);
								if (flag)
								{
									debugLogErrorStringHandler.AppendLiteral("Error while setting up the ICorJitInfo wrapper: ");
									debugLogErrorStringHandler.AppendFormatted<Exception>(e);
								}
								<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler);
							}
							catch
							{
							}
						}
					}
					method invokeCompileMethod = this.InvokeCompileMethodPtr.InvokeCompileMethod;
					CoreCLR.CorJitResult result = calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*), this.CompileMethodPtr, jit, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode, invokeCompileMethod);
					if (pNEx != null && (nativeException = *pNEx) != 0)
					{
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
						if (Core60Runtime.JitHookDelegateHolder.hookEntrancy == 1)
						{
							try
							{
								IAllocatedMemory corJitWrapper2 = this.iCorJitInfoWrapper.Value;
								if (corJitWrapper2 == null)
								{
									return result;
								}
								ref Core60Runtime.ICorJitInfoWrapper wrapper2 = ref *(Core60Runtime.ICorJitInfoWrapper*)(void*)corJitWrapper2.BaseAddress;
								IntPtr rwEntry = *wrapper2[0];
								this.Runtime.CompileMethodHookPostCommon(methodInfo, nativeEntry, nativeSizeOfCode, rwEntry);
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
					Core60Runtime.JitHookDelegateHolder.hookEntrancy--;
					if (pNEx != null)
					{
						*pNEx = nativeException;
					}
					MarshalEx.SetLastPInvokeError(lastError);
				}
				return result2;
			}

			// Token: 0x04001255 RID: 4693
			public readonly Core60Runtime Runtime;

			// Token: 0x04001256 RID: 4694
			[Nullable(2)]
			public readonly INativeExceptionHelper NativeExceptionHelper;

			// Token: 0x04001257 RID: 4695
			[Nullable(2)]
			public readonly GetExceptionSlot GetNativeExceptionSlot;

			// Token: 0x04001258 RID: 4696
			public readonly Core21Runtime.JitHookHelpersHolder JitHookHelpers;

			// Token: 0x04001259 RID: 4697
			public readonly CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr;

			// Token: 0x0400125A RID: 4698
			public readonly IntPtr CompileMethodPtr;

			// Token: 0x0400125B RID: 4699
			public readonly ThreadLocal<IAllocatedMemory> iCorJitInfoWrapper = new ThreadLocal<IAllocatedMemory>();

			// Token: 0x0400125C RID: 4700
			[Nullable(new byte[] { 0, 1 })]
			public readonly ReadOnlyMemory<IAllocatedMemory> iCorJitInfoWrapperAllocs;

			// Token: 0x0400125D RID: 4701
			public readonly IntPtr iCorJitInfoWrapperVtbl;

			// Token: 0x0400125E RID: 4702
			[ThreadStatic]
			private static int hookEntrancy;
		}

		// Token: 0x02000539 RID: 1337
		[NullableContext(0)]
		protected struct NativeJitHookConfig
		{
			// Token: 0x0400125F RID: 4703
			public IntPtr compileMethod;

			// Token: 0x04001260 RID: 4704
			public IntPtr compileMethodHook;

			// Token: 0x04001261 RID: 4705
			public IntPtr compileMethodHookPost;

			// Token: 0x04001262 RID: 4706
			public IntPtr allocMem;

			// Token: 0x04001263 RID: 4707
			public IntPtr allocMemHook;
		}

		// Token: 0x0200053A RID: 1338
		[Nullable(0)]
		private sealed class JitHookPostDelegateHolder
		{
			// Token: 0x06001DFE RID: 7678 RVA: 0x00061550 File Offset: 0x0005F750
			public JitHookPostDelegateHolder(Core60Runtime runtime)
			{
				this.Runtime = runtime;
				this.JitHookHelpers = runtime.JitHookHelpers;
			}

			// Token: 0x06001DFF RID: 7679 RVA: 0x0006156C File Offset: 0x0005F76C
			[NullableContext(0)]
			public unsafe CoreCLR.CorJitResult CompileMethodHookPost(IntPtr jit, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode, CoreCLR.CorJitResult res, CoreCLR.V60.AllocMemArgs* pArgs)
			{
				if (jit == IntPtr.Zero)
				{
					return res;
				}
				try
				{
					if (!Core60Runtime.JitHookPostDelegateHolder.patchedICorJitInfo)
					{
						object obj = Core60Runtime.JitHookPostDelegateHolder.patchedICorJitInfoSyncRoot;
						lock (obj)
						{
							if (!Core60Runtime.JitHookPostDelegateHolder.patchedICorJitInfo)
							{
								IntPtr* allocMemSlot = Core21Runtime.GetVTableEntry(corJitInfo, this.Runtime.VtableIndexICorJitInfoAllocMem);
								Core60Runtime.NativeJitHookConfig* nativeJitHookConfig = this.Runtime.GetNativeJitHookConfig();
								nativeJitHookConfig->allocMem = *allocMemSlot;
								IntPtr ourAllocMemPtr = nativeJitHookConfig->allocMemHook;
								int num = sizeof(IntPtr);
								Span<byte> ptrData = new Span<byte>(stackalloc byte[(UIntPtr)num], num);
								MemoryMarshal.Write<IntPtr>(ptrData, ref ourAllocMemPtr);
								this.Runtime.System.PatchData(PatchTargetKind.ReadOnly, (IntPtr)((void*)allocMemSlot), ptrData, default(Span<byte>));
								Core60Runtime.JitHookPostDelegateHolder.patchedICorJitInfo = true;
							}
						}
					}
					this.Runtime.CompileMethodHookPostCommon(methodInfo, nativeEntry, nativeSizeOfCode, pArgs->hotCodeBlockRW);
				}
				catch
				{
				}
				return res;
			}

			// Token: 0x04001264 RID: 4708
			public readonly Core60Runtime Runtime;

			// Token: 0x04001265 RID: 4709
			public readonly Core21Runtime.JitHookHelpersHolder JitHookHelpers;

			// Token: 0x04001266 RID: 4710
			public static volatile bool patchedICorJitInfo;

			// Token: 0x04001267 RID: 4711
			public static readonly object patchedICorJitInfoSyncRoot = new object();
		}

		// Token: 0x0200053B RID: 1339
		[NullableContext(0)]
		private sealed class AllocMemDelegateHolder
		{
			// Token: 0x06001E01 RID: 7681 RVA: 0x0006167C File Offset: 0x0005F87C
			[NullableContext(1)]
			public AllocMemDelegateHolder(Core60Runtime runtime, CoreCLR.InvokeAllocMemPtr iamp)
			{
				this.Runtime = runtime;
				this.NativeExceptionHelper = runtime.NativeExceptionHelper;
				INativeExceptionHelper nativeExceptionHelper = this.NativeExceptionHelper;
				this.GetNativeExceptionSlot = ((nativeExceptionHelper != null) ? nativeExceptionHelper.GetExceptionSlot : null);
				this.InvokeAllocMemPtr = iamp;
				this.ICorJitInfoAllocMemIdx = this.Runtime.VtableIndexICorJitInfoAllocMem;
				method invokeAllocMem = iamp.InvokeAllocMem;
				calli(System.Void(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*), IntPtr.Zero, IntPtr.Zero, (UIntPtr)0, invokeAllocMem);
			}

			// Token: 0x06001E02 RID: 7682 RVA: 0x000616F7 File Offset: 0x0005F8F7
			private IntPtr GetRealInvokePtr(IntPtr ptr)
			{
				if (this.NativeExceptionHelper == null)
				{
					return ptr;
				}
				return this.AllocMemExceptionHelperCache.GetOrAdd(ptr, delegate(IntPtr p)
				{
					IDisposable h;
					return new ValueTuple<IntPtr, IDisposable>(this.Runtime.EHManagedToNative(p, out h), h);
				}).Item1;
			}

			// Token: 0x06001E03 RID: 7683 RVA: 0x00061720 File Offset: 0x0005F920
			public unsafe void AllocMemHook(IntPtr thisPtr, CoreCLR.V60.AllocMemArgs* args)
			{
				if (thisPtr == IntPtr.Zero)
				{
					return;
				}
				Core60Runtime.ICorJitInfoWrapper* wrap = (Core60Runtime.ICorJitInfoWrapper*)(void*)thisPtr;
				IntPtr** wrapped = wrap->Wrapped;
				method invokeAllocMem = this.InvokeAllocMemPtr.InvokeAllocMem;
				calli(System.Void(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*), this.GetRealInvokePtr(*(*(IntPtr*)wrapped + (IntPtr)this.ICorJitInfoAllocMemIdx * (IntPtr)sizeof(IntPtr))), (IntPtr)((void*)wrapped), args, invokeAllocMem);
				GetExceptionSlot neh = this.GetNativeExceptionSlot;
				if (neh != null && *neh() != 0)
				{
					return;
				}
				*(*wrap)[0] = args->hotCodeBlockRW;
				*(*wrap)[1] = args->coldCodeBlockRW;
			}

			// Token: 0x04001268 RID: 4712
			[Nullable(1)]
			public readonly Core60Runtime Runtime;

			// Token: 0x04001269 RID: 4713
			[Nullable(2)]
			public readonly INativeExceptionHelper NativeExceptionHelper;

			// Token: 0x0400126A RID: 4714
			[Nullable(2)]
			public readonly GetExceptionSlot GetNativeExceptionSlot;

			// Token: 0x0400126B RID: 4715
			public readonly CoreCLR.InvokeAllocMemPtr InvokeAllocMemPtr;

			// Token: 0x0400126C RID: 4716
			public readonly int ICorJitInfoAllocMemIdx;

			// Token: 0x0400126D RID: 4717
			[TupleElementNames(new string[] { "M2N", null })]
			[Nullable(new byte[] { 1, 0, 2 })]
			public readonly ConcurrentDictionary<IntPtr, ValueTuple<IntPtr, IDisposable>> AllocMemExceptionHelperCache = new ConcurrentDictionary<IntPtr, ValueTuple<IntPtr, IDisposable>>();
		}
	}
}
