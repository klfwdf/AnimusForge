using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Core.Interop.Attributes;
using MonoMod.Utils;

namespace MonoMod.Core.Interop
{
	// Token: 0x02000566 RID: 1382
	internal static class CoreCLR
	{
		// Token: 0x02000567 RID: 1383
		public enum CorJitResult
		{
			// Token: 0x040012DC RID: 4828
			CORJIT_OK
		}

		// Token: 0x02000568 RID: 1384
		public readonly struct InvokeCompileMethodPtr
		{
			// Token: 0x06001F07 RID: 7943 RVA: 0x00065E24 File Offset: 0x00064024
			public InvokeCompileMethodPtr(method ptr)
			{
				this.methodPtr = (IntPtr)ptr;
			}

			// Token: 0x170006C4 RID: 1732
			// (get) Token: 0x06001F08 RID: 7944 RVA: 0x00065E32 File Offset: 0x00064032
			public unsafe method InvokeCompileMethod
			{
				get
				{
					return (void*)this.methodPtr;
				}
			}

			// Token: 0x040012DD RID: 4829
			private readonly IntPtr methodPtr;
		}

		// Token: 0x02000569 RID: 1385
		public class V21
		{
			// Token: 0x170006C5 RID: 1733
			// (get) Token: 0x06001F09 RID: 7945 RVA: 0x00065E3F File Offset: 0x0006403F
			public static CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
			{
				get
				{
					return new CoreCLR.InvokeCompileMethodPtr(ldftn(InvokeCompileMethod));
				}
			}

			// Token: 0x06001F0A RID: 7946 RVA: 0x00065E4C File Offset: 0x0006404C
			public unsafe static CoreCLR.CorJitResult InvokeCompileMethod(IntPtr functionPtr, IntPtr thisPtr, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** pNativeEntry, uint* pNativeSizeOfCode)
			{
				if (functionPtr == IntPtr.Zero)
				{
					*(IntPtr*)pNativeEntry = (IntPtr)((UIntPtr)0);
					*pNativeSizeOfCode = 0U;
					return CoreCLR.CorJitResult.CORJIT_OK;
				}
				method fnPtr = (void*)functionPtr;
				method monoMod.Core.Interop.CoreCLR/CorJitResult_u0020(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*) = fnPtr;
				return calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*), thisPtr, corJitInfo, methodInfo, flags, pNativeEntry, pNativeSizeOfCode, monoMod.Core.Interop.CoreCLR/CorJitResult_u0020(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*));
			}

			// Token: 0x0200056A RID: 1386
			public struct CORINFO_SIG_INST
			{
				// Token: 0x040012DE RID: 4830
				public uint classInstCount;

				// Token: 0x040012DF RID: 4831
				public unsafe IntPtr* classInst;

				// Token: 0x040012E0 RID: 4832
				public uint methInstCount;

				// Token: 0x040012E1 RID: 4833
				public unsafe IntPtr* methInst;
			}

			// Token: 0x0200056B RID: 1387
			public struct CORINFO_SIG_INFO
			{
				// Token: 0x040012E2 RID: 4834
				public int callConv;

				// Token: 0x040012E3 RID: 4835
				public IntPtr retTypeClass;

				// Token: 0x040012E4 RID: 4836
				public IntPtr retTypeSigClass;

				// Token: 0x040012E5 RID: 4837
				public byte retType;

				// Token: 0x040012E6 RID: 4838
				public byte flags;

				// Token: 0x040012E7 RID: 4839
				public ushort numArgs;

				// Token: 0x040012E8 RID: 4840
				public CoreCLR.V21.CORINFO_SIG_INST sigInst;

				// Token: 0x040012E9 RID: 4841
				public IntPtr args;

				// Token: 0x040012EA RID: 4842
				public IntPtr pSig;

				// Token: 0x040012EB RID: 4843
				public uint sbSig;

				// Token: 0x040012EC RID: 4844
				public IntPtr scope;

				// Token: 0x040012ED RID: 4845
				public uint token;
			}

			// Token: 0x0200056C RID: 1388
			public struct CORINFO_METHOD_INFO
			{
				// Token: 0x040012EE RID: 4846
				public IntPtr ftn;

				// Token: 0x040012EF RID: 4847
				public IntPtr scope;

				// Token: 0x040012F0 RID: 4848
				public unsafe byte* ILCode;

				// Token: 0x040012F1 RID: 4849
				public uint ILCodeSize;

				// Token: 0x040012F2 RID: 4850
				public uint maxStack;

				// Token: 0x040012F3 RID: 4851
				public uint EHcount;

				// Token: 0x040012F4 RID: 4852
				public int options;

				// Token: 0x040012F5 RID: 4853
				public int regionKind;

				// Token: 0x040012F6 RID: 4854
				public CoreCLR.V21.CORINFO_SIG_INFO args;

				// Token: 0x040012F7 RID: 4855
				public CoreCLR.V21.CORINFO_SIG_INFO locals;
			}

			// Token: 0x0200056D RID: 1389
			// (Invoke) Token: 0x06001F0D RID: 7949
			[UnmanagedFunctionPointer(CallingConvention.StdCall)]
			public unsafe delegate CoreCLR.CorJitResult CompileMethodDelegate(IntPtr thisPtr, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode);
		}

		// Token: 0x0200056E RID: 1390
		public class V30 : CoreCLR.V21
		{
		}

		// Token: 0x0200056F RID: 1391
		public class V31 : CoreCLR.V30
		{
		}

		// Token: 0x02000570 RID: 1392
		public class V50 : CoreCLR.V31
		{
		}

		// Token: 0x02000571 RID: 1393
		public class V100 : CoreCLR.V90
		{
			// Token: 0x02000572 RID: 1394
			public new static class ICorJitInfoVtable
			{
				// Token: 0x040012F8 RID: 4856
				public const int AllocMemIndex = 160;

				// Token: 0x040012F9 RID: 4857
				public const int TotalVtableCount = 176;
			}
		}

		// Token: 0x02000573 RID: 1395
		public readonly struct InvokeCompileMethodHookPostPtr
		{
			// Token: 0x06001F14 RID: 7956 RVA: 0x00065EA9 File Offset: 0x000640A9
			public InvokeCompileMethodHookPostPtr(method ptr)
			{
				this.methodPtr = (IntPtr)ptr;
			}

			// Token: 0x170006C6 RID: 1734
			// (get) Token: 0x06001F15 RID: 7957 RVA: 0x00065EB7 File Offset: 0x000640B7
			public unsafe method InvokeCompileMethodHookPost
			{
				get
				{
					return (void*)this.methodPtr;
				}
			}

			// Token: 0x040012FA RID: 4858
			private readonly IntPtr methodPtr;
		}

		// Token: 0x02000574 RID: 1396
		public readonly struct InvokeAllocMemPtr
		{
			// Token: 0x06001F16 RID: 7958 RVA: 0x00065EC4 File Offset: 0x000640C4
			public InvokeAllocMemPtr(method ptr)
			{
				this.methodPtr = (IntPtr)ptr;
			}

			// Token: 0x170006C7 RID: 1735
			// (get) Token: 0x06001F17 RID: 7959 RVA: 0x00065ED2 File Offset: 0x000640D2
			public unsafe method InvokeAllocMem
			{
				get
				{
					return (void*)this.methodPtr;
				}
			}

			// Token: 0x040012FB RID: 4859
			private readonly IntPtr methodPtr;
		}

		// Token: 0x02000575 RID: 1397
		public class V60 : CoreCLR.V50
		{
			// Token: 0x170006C8 RID: 1736
			// (get) Token: 0x06001F18 RID: 7960 RVA: 0x00065EDF File Offset: 0x000640DF
			public static CoreCLR.InvokeAllocMemPtr InvokeAllocMemPtr
			{
				get
				{
					return new CoreCLR.InvokeAllocMemPtr(ldftn(InvokeAllocMem));
				}
			}

			// Token: 0x06001F19 RID: 7961 RVA: 0x00065EEC File Offset: 0x000640EC
			public unsafe static void InvokeAllocMem(IntPtr functionPtr, IntPtr thisPtr, CoreCLR.V60.AllocMemArgs* args)
			{
				if (functionPtr == IntPtr.Zero)
				{
					return;
				}
				method fnPtr = (void*)functionPtr;
				method system.Void_u0020(System.IntPtr,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*) = fnPtr;
				calli(System.Void(System.IntPtr,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*), thisPtr, args, system.Void_u0020(System.IntPtr,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*));
			}

			// Token: 0x170006C9 RID: 1737
			// (get) Token: 0x06001F1A RID: 7962 RVA: 0x00065F18 File Offset: 0x00064118
			public new static CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
			{
				get
				{
					return new CoreCLR.InvokeCompileMethodPtr(ldftn(InvokeCompileMethod));
				}
			}

			// Token: 0x06001F1B RID: 7963 RVA: 0x00065F28 File Offset: 0x00064128
			public new unsafe static CoreCLR.CorJitResult InvokeCompileMethod(IntPtr functionPtr, IntPtr thisPtr, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode)
			{
				if (functionPtr == IntPtr.Zero)
				{
					*(IntPtr*)nativeEntry = (IntPtr)((UIntPtr)0);
					*nativeSizeOfCode = 0U;
					return CoreCLR.CorJitResult.CORJIT_OK;
				}
				method fnPtr = (void*)functionPtr;
				method monoMod.Core.Interop.CoreCLR/CorJitResult_u0020(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*) = fnPtr;
				return calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*), thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode, monoMod.Core.Interop.CoreCLR/CorJitResult_u0020(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*));
			}

			// Token: 0x170006CA RID: 1738
			// (get) Token: 0x06001F1C RID: 7964 RVA: 0x00065F65 File Offset: 0x00064165
			public static CoreCLR.InvokeCompileMethodHookPostPtr InvokeCompileMethodHookPostPtr
			{
				get
				{
					return new CoreCLR.InvokeCompileMethodHookPostPtr(ldftn(InvokeCompileMethodHookPost));
				}
			}

			// Token: 0x06001F1D RID: 7965 RVA: 0x00065F74 File Offset: 0x00064174
			public unsafe static CoreCLR.CorJitResult InvokeCompileMethodHookPost(IntPtr functionPtr, IntPtr thisPtr, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode, CoreCLR.CorJitResult res, CoreCLR.V60.AllocMemArgs* pArgs)
			{
				if (functionPtr == IntPtr.Zero)
				{
					return res;
				}
				method fnPtr = (void*)functionPtr;
				method monoMod.Core.Interop.CoreCLR/CorJitResult_u0020(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*,MonoMod.Core.Interop.CoreCLR/CorJitResult,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*) = fnPtr;
				return calli(MonoMod.Core.Interop.CoreCLR/CorJitResult(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*,MonoMod.Core.Interop.CoreCLR/CorJitResult,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*), thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode, res, pArgs, monoMod.Core.Interop.CoreCLR/CorJitResult_u0020(System.IntPtr,System.IntPtr,MonoMod.Core.Interop.CoreCLR/V21/CORINFO_METHOD_INFO*,System.UInt32,System.Byte**,System.UInt32*,MonoMod.Core.Interop.CoreCLR/CorJitResult,MonoMod.Core.Interop.CoreCLR/V60/AllocMemArgs*));
			}

			// Token: 0x02000576 RID: 1398
			public static class ICorJitInfoVtable
			{
				// Token: 0x040012FC RID: 4860
				public const int AllocMemIndex = 156;

				// Token: 0x040012FD RID: 4861
				public const int TotalVtableCount = 173;
			}

			// Token: 0x02000577 RID: 1399
			public struct AllocMemArgs
			{
				// Token: 0x040012FE RID: 4862
				public uint hotCodeSize;

				// Token: 0x040012FF RID: 4863
				public uint coldCodeSize;

				// Token: 0x04001300 RID: 4864
				public uint roDataSize;

				// Token: 0x04001301 RID: 4865
				public uint xcptnsCount;

				// Token: 0x04001302 RID: 4866
				public int flag;

				// Token: 0x04001303 RID: 4867
				public IntPtr hotCodeBlock;

				// Token: 0x04001304 RID: 4868
				public IntPtr hotCodeBlockRW;

				// Token: 0x04001305 RID: 4869
				public IntPtr coldCodeBlock;

				// Token: 0x04001306 RID: 4870
				public IntPtr coldCodeBlockRW;

				// Token: 0x04001307 RID: 4871
				public IntPtr roDataBlock;

				// Token: 0x04001308 RID: 4872
				public IntPtr roDataBlockRW;
			}

			// Token: 0x02000578 RID: 1400
			// (Invoke) Token: 0x06001F20 RID: 7968
			[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
			public unsafe delegate void AllocMemDelegate(IntPtr thisPtr, CoreCLR.V60.AllocMemArgs* args);

			// Token: 0x02000579 RID: 1401
			// (Invoke) Token: 0x06001F24 RID: 7972
			[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
			public new unsafe delegate CoreCLR.CorJitResult CompileMethodDelegate(IntPtr thisPtr, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode);

			// Token: 0x0200057A RID: 1402
			// (Invoke) Token: 0x06001F28 RID: 7976
			[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
			public unsafe delegate CoreCLR.CorJitResult CompileMethodHookPostDelegate(IntPtr thisPtr, IntPtr corJitInfo, CoreCLR.V21.CORINFO_METHOD_INFO* methodInfo, uint flags, byte** nativeEntry, uint* nativeSizeOfCode, CoreCLR.CorJitResult res, CoreCLR.V60.AllocMemArgs* allocMemArgs);

			// Token: 0x0200057B RID: 1403
			public enum MethodClassification
			{
				// Token: 0x0400130A RID: 4874
				IL,
				// Token: 0x0400130B RID: 4875
				FCall,
				// Token: 0x0400130C RID: 4876
				NDirect,
				// Token: 0x0400130D RID: 4877
				EEImpl,
				// Token: 0x0400130E RID: 4878
				Array,
				// Token: 0x0400130F RID: 4879
				Instantiated,
				// Token: 0x04001310 RID: 4880
				ComInterop,
				// Token: 0x04001311 RID: 4881
				Dynamic
			}

			// Token: 0x0200057C RID: 1404
			[Flags]
			public enum MethodDescClassification : ushort
			{
				// Token: 0x04001313 RID: 4883
				ClassificationMask = 7,
				// Token: 0x04001314 RID: 4884
				HasNonVtableSlot = 8,
				// Token: 0x04001315 RID: 4885
				MethodImpl = 16,
				// Token: 0x04001316 RID: 4886
				HasNativeCodeSlot = 32,
				// Token: 0x04001317 RID: 4887
				HasComPlusCallInfo = 64,
				// Token: 0x04001318 RID: 4888
				Static = 128,
				// Token: 0x04001319 RID: 4889
				Duplicate = 1024,
				// Token: 0x0400131A RID: 4890
				VerifiedState = 2048,
				// Token: 0x0400131B RID: 4891
				Verifiable = 4096,
				// Token: 0x0400131C RID: 4892
				NotInline = 8192,
				// Token: 0x0400131D RID: 4893
				Synchronized = 16384,
				// Token: 0x0400131E RID: 4894
				RequiresFullSlotNumber = 32768
			}

			// Token: 0x0200057D RID: 1405
			public struct RelativePointer
			{
				// Token: 0x06001F2B RID: 7979 RVA: 0x00065FB5 File Offset: 0x000641B5
				public RelativePointer([NativeInteger] IntPtr delta)
				{
					this.m_delta = delta;
				}

				// Token: 0x170006CB RID: 1739
				// (get) Token: 0x06001F2C RID: 7980 RVA: 0x00065FC0 File Offset: 0x000641C0
				public unsafe void* Value
				{
					get
					{
						IntPtr delta = this.m_delta;
						if (delta != 0)
						{
							return Unsafe.AsPointer<CoreCLR.V60.RelativePointer>(Unsafe.AddByteOffset<CoreCLR.V60.RelativePointer>(ref this, delta));
						}
						return null;
					}
				}

				// Token: 0x0400131F RID: 4895
				[NativeInteger]
				private IntPtr m_delta;
			}

			// Token: 0x0200057E RID: 1406
			public struct RelativeFixupPointer
			{
				// Token: 0x170006CC RID: 1740
				// (get) Token: 0x06001F2D RID: 7981 RVA: 0x00065FE8 File Offset: 0x000641E8
				public unsafe void* Value
				{
					get
					{
						IntPtr delta = this.m_delta;
						if (delta == 0)
						{
							return null;
						}
						IntPtr addr = Unsafe.AsPointer<CoreCLR.V60.RelativeFixupPointer>(Unsafe.AddByteOffset<CoreCLR.V60.RelativeFixupPointer>(ref this, delta));
						if ((addr & (IntPtr)1) != 0)
						{
							addr = *(addr - (IntPtr)1);
						}
						return addr;
					}
				}

				// Token: 0x04001320 RID: 4896
				[NativeInteger]
				private IntPtr m_delta;

				// Token: 0x04001321 RID: 4897
				[NativeInteger]
				public const IntPtr FIXUP_POINTER_INDIRECTION = 1;
			}

			// Token: 0x0200057F RID: 1407
			public struct MethodDesc
			{
				// Token: 0x170006CD RID: 1741
				// (get) Token: 0x06001F2E RID: 7982 RVA: 0x0006601C File Offset: 0x0006421C
				public ushort SlotNumber
				{
					get
					{
						if (!this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.RequiresFullSlotNumber))
						{
							return this.m_wSlotNumber & 1023;
						}
						return this.m_wSlotNumber;
					}
				}

				// Token: 0x170006CE RID: 1742
				// (get) Token: 0x06001F2F RID: 7983 RVA: 0x00066044 File Offset: 0x00064244
				public CoreCLR.V60.MethodClassification Classification
				{
					get
					{
						return (CoreCLR.V60.MethodClassification)(this.m_wFlags & CoreCLR.V60.MethodDescClassification.ClassificationMask);
					}
				}

				// Token: 0x170006CF RID: 1743
				// (get) Token: 0x06001F30 RID: 7984 RVA: 0x0006604E File Offset: 0x0006424E
				public unsafe CoreCLR.V60.MethodDescChunk* MethodDescChunk
				{
					get
					{
						return (CoreCLR.V60.MethodDescChunk*)((byte*)Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this) - (ulong)((IntPtr)sizeof(CoreCLR.V60.MethodDescChunk) + (IntPtr)((UIntPtr)this.m_chunkIndex * CoreCLR.V60.MethodDesc.Alignment)));
					}
				}

				// Token: 0x170006D0 RID: 1744
				// (get) Token: 0x06001F31 RID: 7985 RVA: 0x0006606E File Offset: 0x0006426E
				public unsafe CoreCLR.V60.MethodTable* MethodTable
				{
					get
					{
						return this.MethodDescChunk->m_methodTable;
					}
				}

				// Token: 0x06001F32 RID: 7986 RVA: 0x0006607C File Offset: 0x0006427C
				public unsafe void* GetMethodEntryPoint()
				{
					if (!this.HasNonVtableSlot)
					{
						return this.MethodTable->GetSlot((uint)this.SlotNumber);
					}
					UIntPtr size = this.GetBaseSize();
					byte* pSlot = (byte*)Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this) + (ulong)size;
					if (!this.MethodDescChunk->m_flagsAndTokenRange.Has(CoreCLR.V60.MethodDescChunk.Flags.IsZapped))
					{
						return *(IntPtr*)pSlot;
					}
					return new CoreCLR.V60.RelativePointer(pSlot).Value;
				}

				// Token: 0x06001F33 RID: 7987 RVA: 0x000660DD File Offset: 0x000642DD
				public bool TryAsFCall(out CoreCLR.V60.FCallMethodDescPtr md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.FCall)
					{
						md = new CoreCLR.V60.FCallMethodDescPtr(Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this), CoreCLR.V60.FCallMethodDescPtr.CurrentVtable);
						return true;
					}
					md = default(CoreCLR.V60.FCallMethodDescPtr);
					return false;
				}

				// Token: 0x06001F34 RID: 7988 RVA: 0x00066108 File Offset: 0x00064308
				public bool TryAsNDirect(out CoreCLR.V60.NDirectMethodDescPtr md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.NDirect)
					{
						md = new CoreCLR.V60.NDirectMethodDescPtr(Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this), CoreCLR.V60.NDirectMethodDescPtr.CurrentVtable);
						return true;
					}
					md = default(CoreCLR.V60.NDirectMethodDescPtr);
					return false;
				}

				// Token: 0x06001F35 RID: 7989 RVA: 0x00066133 File Offset: 0x00064333
				public bool TryAsEEImpl(out CoreCLR.V60.EEImplMethodDescPtr md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.EEImpl)
					{
						md = new CoreCLR.V60.EEImplMethodDescPtr(Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this), CoreCLR.V60.EEImplMethodDescPtr.CurrentVtable);
						return true;
					}
					md = default(CoreCLR.V60.EEImplMethodDescPtr);
					return false;
				}

				// Token: 0x06001F36 RID: 7990 RVA: 0x0006615E File Offset: 0x0006435E
				public bool TryAsArray(out CoreCLR.V60.ArrayMethodDescPtr md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.Array)
					{
						md = new CoreCLR.V60.ArrayMethodDescPtr(Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this), CoreCLR.V60.ArrayMethodDescPtr.CurrentVtable);
						return true;
					}
					md = default(CoreCLR.V60.ArrayMethodDescPtr);
					return false;
				}

				// Token: 0x06001F37 RID: 7991 RVA: 0x00066189 File Offset: 0x00064389
				public unsafe bool TryAsInstantiated(out CoreCLR.V60.InstantiatedMethodDesc* md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.Instantiated)
					{
						md = Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this);
						return true;
					}
					md = default(CoreCLR.V60.InstantiatedMethodDesc*);
					return false;
				}

				// Token: 0x06001F38 RID: 7992 RVA: 0x000661A6 File Offset: 0x000643A6
				public unsafe bool TryAsComPlusCall(out CoreCLR.V60.ComPlusCallMethodDesc* md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.ComInterop)
					{
						md = Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this);
						return true;
					}
					md = default(CoreCLR.V60.ComPlusCallMethodDesc*);
					return false;
				}

				// Token: 0x06001F39 RID: 7993 RVA: 0x000661C3 File Offset: 0x000643C3
				public bool TryAsDynamic(out CoreCLR.V60.DynamicMethodDescPtr md)
				{
					if (this.Classification == CoreCLR.V60.MethodClassification.Dynamic)
					{
						md = new CoreCLR.V60.DynamicMethodDescPtr(Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(ref this), CoreCLR.V60.DynamicMethodDescPtr.CurrentVtable);
						return true;
					}
					md = default(CoreCLR.V60.DynamicMethodDescPtr);
					return false;
				}

				// Token: 0x06001F3A RID: 7994 RVA: 0x000661F0 File Offset: 0x000643F0
				[return: NativeInteger]
				public unsafe UIntPtr SizeOf(bool includeNonVtable = true, bool includeMethodImpl = true, bool includeComPlus = true, bool includeNativeCode = true)
				{
					UIntPtr size = this.GetBaseSize() + (UIntPtr)((includeNonVtable && this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.HasNonVtableSlot)) ? ((IntPtr)sizeof(void*)) : ((IntPtr)0)) + (UIntPtr)((includeMethodImpl && this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.MethodImpl)) ? ((IntPtr)sizeof(void*) * (IntPtr)2) : ((IntPtr)0)) + (UIntPtr)((includeComPlus && this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.HasComPlusCallInfo)) ? ((IntPtr)sizeof(void*)) : ((IntPtr)0)) + (UIntPtr)((includeNativeCode && this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.HasNativeCodeSlot)) ? ((IntPtr)sizeof(void*)) : ((IntPtr)0));
					if (includeNativeCode && this.HasNativeCodeSlot)
					{
						size += (UIntPtr)(((this.GetAddrOfNativeCodeSlot() & 1) != null) ? ((IntPtr)sizeof(void*)) : ((IntPtr)0));
					}
					return size;
				}

				// Token: 0x06001F3B RID: 7995 RVA: 0x000662A8 File Offset: 0x000644A8
				public unsafe void* GetNativeCode()
				{
					if (this.HasNativeCodeSlot)
					{
						void* pCode = *(IntPtr*)(this.GetAddrOfNativeCodeSlot() & ~1);
						if (pCode != null)
						{
							return pCode;
						}
					}
					if (!this.HasStableEntryPoint || this.HasPrecode)
					{
						return null;
					}
					return this.GetStableEntryPoint();
				}

				// Token: 0x06001F3C RID: 7996 RVA: 0x000662E9 File Offset: 0x000644E9
				public unsafe void* GetStableEntryPoint()
				{
					return this.GetMethodEntryPoint();
				}

				// Token: 0x170006D1 RID: 1745
				// (get) Token: 0x06001F3D RID: 7997 RVA: 0x000662F1 File Offset: 0x000644F1
				public bool HasNonVtableSlot
				{
					get
					{
						return this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.HasNonVtableSlot);
					}
				}

				// Token: 0x170006D2 RID: 1746
				// (get) Token: 0x06001F3E RID: 7998 RVA: 0x000662FF File Offset: 0x000644FF
				public bool HasStableEntryPoint
				{
					get
					{
						return this.m_bFlags2.Has(CoreCLR.V60.MethodDesc.Flags2.HasStableEntryPoint);
					}
				}

				// Token: 0x170006D3 RID: 1747
				// (get) Token: 0x06001F3F RID: 7999 RVA: 0x0006630D File Offset: 0x0006450D
				public bool HasPrecode
				{
					get
					{
						return this.m_bFlags2.Has(CoreCLR.V60.MethodDesc.Flags2.HasPrecode);
					}
				}

				// Token: 0x170006D4 RID: 1748
				// (get) Token: 0x06001F40 RID: 8000 RVA: 0x0006631B File Offset: 0x0006451B
				public bool HasNativeCodeSlot
				{
					get
					{
						return this.m_wFlags.Has(CoreCLR.V60.MethodDescClassification.HasNativeCodeSlot);
					}
				}

				// Token: 0x170006D5 RID: 1749
				// (get) Token: 0x06001F41 RID: 8001 RVA: 0x0006632A File Offset: 0x0006452A
				public bool IsUnboxingStub
				{
					get
					{
						return this.m_bFlags2.Has(CoreCLR.V60.MethodDesc.Flags2.IsUnboxingStub);
					}
				}

				// Token: 0x170006D6 RID: 1750
				// (get) Token: 0x06001F42 RID: 8002 RVA: 0x00066338 File Offset: 0x00064538
				public unsafe bool HasMethodInstantiation
				{
					get
					{
						CoreCLR.V60.InstantiatedMethodDesc* inst;
						return this.TryAsInstantiated(out inst) && inst->IMD_HasMethodInstantiation;
					}
				}

				// Token: 0x170006D7 RID: 1751
				// (get) Token: 0x06001F43 RID: 8003 RVA: 0x00066358 File Offset: 0x00064558
				public unsafe bool IsGenericMethodDefinition
				{
					get
					{
						CoreCLR.V60.InstantiatedMethodDesc* inst;
						return this.TryAsInstantiated(out inst) && inst->IMD_IsGenericMethodDefinition;
					}
				}

				// Token: 0x170006D8 RID: 1752
				// (get) Token: 0x06001F44 RID: 8004 RVA: 0x00066378 File Offset: 0x00064578
				public unsafe bool IsInstantiatingStub
				{
					get
					{
						CoreCLR.V60.InstantiatedMethodDesc* inst;
						return !this.IsUnboxingStub && this.TryAsInstantiated(out inst) && inst->IMD_IsWrapperStubWithInstantiations;
					}
				}

				// Token: 0x170006D9 RID: 1753
				// (get) Token: 0x06001F45 RID: 8005 RVA: 0x0006639F File Offset: 0x0006459F
				public bool IsWrapperStub
				{
					get
					{
						return this.IsUnboxingStub || this.IsInstantiatingStub;
					}
				}

				// Token: 0x170006DA RID: 1754
				// (get) Token: 0x06001F46 RID: 8006 RVA: 0x000663B1 File Offset: 0x000645B1
				public bool IsTightlyBoundToMethodTable
				{
					get
					{
						if (!this.HasNonVtableSlot)
						{
							return true;
						}
						if (this.HasMethodInstantiation)
						{
							return this.IsGenericMethodDefinition;
						}
						return !this.IsWrapperStub;
					}
				}

				// Token: 0x06001F47 RID: 8007 RVA: 0x000663D8 File Offset: 0x000645D8
				public unsafe static CoreCLR.V60.MethodDesc* FindTightlyBoundWrappedMethodDesc(CoreCLR.V60.MethodDesc* pMD)
				{
					CoreCLR.V60.InstantiatedMethodDesc* inst;
					if (pMD->IsUnboxingStub && pMD->TryAsInstantiated(out inst))
					{
						pMD = inst->IMD_GetWrappedMethodDesc();
					}
					if (!pMD->IsTightlyBoundToMethodTable)
					{
						pMD = pMD->GetCanonicalMethodTable()->GetParallelMethodDesc(pMD);
					}
					if (pMD->IsUnboxingStub)
					{
						pMD = CoreCLR.V60.MethodDesc.GetNextIntroducedMethod(pMD);
					}
					return pMD;
				}

				// Token: 0x06001F48 RID: 8008 RVA: 0x00066428 File Offset: 0x00064628
				public unsafe static CoreCLR.V60.MethodDesc* GetNextIntroducedMethod(CoreCLR.V60.MethodDesc* pMD)
				{
					CoreCLR.V60.MethodDescChunk* pChunk = pMD->MethodDescChunk;
					UIntPtr pNext = pMD + pMD->SizeOf(true, true, true, true) / (UIntPtr)sizeof(CoreCLR.V60.MethodDesc);
					UIntPtr pEnd = pChunk + pChunk->SizeOf / (UIntPtr)sizeof(CoreCLR.V60.MethodDescChunk);
					if (pNext < pEnd)
					{
						return pNext;
					}
					pChunk = pChunk->m_next;
					if (pChunk != null)
					{
						return pChunk->FirstMethodDesc;
					}
					return null;
				}

				// Token: 0x06001F49 RID: 8009 RVA: 0x0006646D File Offset: 0x0006466D
				public unsafe CoreCLR.V60.MethodTable* GetCanonicalMethodTable()
				{
					return this.MethodTable->GetCanonicalMethodTable();
				}

				// Token: 0x06001F4A RID: 8010 RVA: 0x0006647C File Offset: 0x0006467C
				public unsafe void* GetAddrOfNativeCodeSlot()
				{
					UIntPtr size = this.SizeOf(true, true, false, false);
					return Unsafe.AsPointer<CoreCLR.V60.MethodDesc>(Unsafe.AddByteOffset<CoreCLR.V60.MethodDesc>(ref this, size));
				}

				// Token: 0x06001F4B RID: 8011 RVA: 0x000664A0 File Offset: 0x000646A0
				[return: NativeInteger]
				public UIntPtr GetBaseSize()
				{
					return CoreCLR.V60.MethodDesc.GetBaseSize(this.Classification);
				}

				// Token: 0x06001F4C RID: 8012 RVA: 0x000664AD File Offset: 0x000646AD
				[return: NativeInteger]
				public static UIntPtr GetBaseSize(CoreCLR.V60.MethodClassification classification)
				{
					return CoreCLR.V60.MethodDesc.s_ClassificationSizeTable[(int)classification];
				}

				// Token: 0x04001322 RID: 4898
				[NativeInteger]
				public static readonly UIntPtr Alignment = (UIntPtr)((IntPtr.Size == 8) ? ((IntPtr)8) : ((IntPtr)4));

				// Token: 0x04001323 RID: 4899
				public CoreCLR.V60.MethodDesc.Flags3 m_wFlags3AndTokenRemainder;

				// Token: 0x04001324 RID: 4900
				public byte m_chunkIndex;

				// Token: 0x04001325 RID: 4901
				public CoreCLR.V60.MethodDesc.Flags2 m_bFlags2;

				// Token: 0x04001326 RID: 4902
				public const ushort PackedSlot_SlotMask = 1023;

				// Token: 0x04001327 RID: 4903
				public const ushort PackedSlot_NameHashMask = 64512;

				// Token: 0x04001328 RID: 4904
				public ushort m_wSlotNumber;

				// Token: 0x04001329 RID: 4905
				public CoreCLR.V60.MethodDescClassification m_wFlags;

				// Token: 0x0400132A RID: 4906
				[NativeInteger]
				[Nullable(1)]
				private static readonly UIntPtr[] s_ClassificationSizeTable = new UIntPtr[]
				{
					(UIntPtr)((IntPtr)sizeof(CoreCLR.V60.MethodDesc)),
					(UIntPtr)((IntPtr)CoreCLR.V60.FCallMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)CoreCLR.V60.NDirectMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)CoreCLR.V60.EEImplMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)CoreCLR.V60.ArrayMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)sizeof(CoreCLR.V60.InstantiatedMethodDesc)),
					(UIntPtr)((IntPtr)sizeof(CoreCLR.V60.ComPlusCallMethodDesc)),
					(UIntPtr)((IntPtr)CoreCLR.V60.DynamicMethodDescPtr.CurrentSize)
				};

				// Token: 0x02000580 RID: 1408
				[Flags]
				public enum Flags3 : ushort
				{
					// Token: 0x0400132C RID: 4908
					TokenRemainderMask = 16383,
					// Token: 0x0400132D RID: 4909
					HasForwardedValuetypeParameter = 16384,
					// Token: 0x0400132E RID: 4910
					ValueTypeParametersWalked = 16384,
					// Token: 0x0400132F RID: 4911
					DoesNotHaveEquivalentValuetypeParameters = 32768
				}

				// Token: 0x02000581 RID: 1409
				[Flags]
				public enum Flags2 : byte
				{
					// Token: 0x04001331 RID: 4913
					HasStableEntryPoint = 1,
					// Token: 0x04001332 RID: 4914
					HasPrecode = 2,
					// Token: 0x04001333 RID: 4915
					IsUnboxingStub = 4,
					// Token: 0x04001334 RID: 4916
					IsJitIntrinsic = 16,
					// Token: 0x04001335 RID: 4917
					IsEligibleForTieredCompilation = 32,
					// Token: 0x04001336 RID: 4918
					RequiresCovariantReturnTypeChecking = 64
				}
			}

			// Token: 0x02000582 RID: 1410
			public struct MethodDescChunk
			{
				// Token: 0x170006DB RID: 1755
				// (get) Token: 0x06001F4E RID: 8014 RVA: 0x0006652E File Offset: 0x0006472E
				public unsafe CoreCLR.V60.MethodDesc* FirstMethodDesc
				{
					get
					{
						return (CoreCLR.V60.MethodDesc*)((byte*)Unsafe.AsPointer<CoreCLR.V60.MethodDescChunk>(ref this) + sizeof(CoreCLR.V60.MethodDescChunk));
					}
				}

				// Token: 0x170006DC RID: 1756
				// (get) Token: 0x06001F4F RID: 8015 RVA: 0x0006653D File Offset: 0x0006473D
				public uint Size
				{
					get
					{
						return (uint)(this.m_size + 1);
					}
				}

				// Token: 0x170006DD RID: 1757
				// (get) Token: 0x06001F50 RID: 8016 RVA: 0x00066547 File Offset: 0x00064747
				public uint Count
				{
					get
					{
						return (uint)(this.m_count + 1);
					}
				}

				// Token: 0x170006DE RID: 1758
				// (get) Token: 0x06001F51 RID: 8017 RVA: 0x00066551 File Offset: 0x00064751
				[NativeInteger]
				public UIntPtr SizeOf
				{
					[return: NativeInteger]
					get
					{
						return (UIntPtr)((IntPtr)sizeof(CoreCLR.V60.MethodDescChunk) + (IntPtr)((UIntPtr)this.Size * CoreCLR.V60.MethodDesc.Alignment));
					}
				}

				// Token: 0x04001337 RID: 4919
				public unsafe CoreCLR.V60.MethodTable* m_methodTable;

				// Token: 0x04001338 RID: 4920
				public unsafe CoreCLR.V60.MethodDescChunk* m_next;

				// Token: 0x04001339 RID: 4921
				public byte m_size;

				// Token: 0x0400133A RID: 4922
				public byte m_count;

				// Token: 0x0400133B RID: 4923
				public CoreCLR.V60.MethodDescChunk.Flags m_flagsAndTokenRange;

				// Token: 0x02000583 RID: 1411
				[Flags]
				public enum Flags : ushort
				{
					// Token: 0x0400133D RID: 4925
					TokenRangeMask = 1023,
					// Token: 0x0400133E RID: 4926
					HasCompactEntrypoints = 16384,
					// Token: 0x0400133F RID: 4927
					IsZapped = 32768
				}
			}

			// Token: 0x02000584 RID: 1412
			[FatInterface]
			public struct StoredSigMethodDescPtr
			{
				// Token: 0x170006DF RID: 1759
				// (get) Token: 0x06001F52 RID: 8018 RVA: 0x00066568 File Offset: 0x00064768
				[Nullable(1)]
				public static IntPtr[] CurrentVtable
				{
					[NullableContext(1)]
					get;
				} = ((IntPtr.Size == 8) ? CoreCLR.V60.StoredSigMethodDesc_64.FatVtable_ : CoreCLR.V60.StoredSigMethodDesc_32.FatVtable_);

				// Token: 0x170006E0 RID: 1760
				// (get) Token: 0x06001F53 RID: 8019 RVA: 0x0006656F File Offset: 0x0006476F
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(CoreCLR.V60.StoredSigMethodDesc_64) : sizeof(CoreCLR.V60.StoredSigMethodDesc_32));

				// Token: 0x06001F54 RID: 8020 RVA: 0x00066578 File Offset: 0x00064778
				private unsafe void* GetPSig()
				{
					method system.Void*_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(System.Void*(System.Void*), this.ptr_, system.Void*_u0020(System.Void*));
				}

				// Token: 0x170006E1 RID: 1761
				// (get) Token: 0x06001F55 RID: 8021 RVA: 0x0006659F File Offset: 0x0006479F
				public unsafe void* m_pSig
				{
					[FatInterfaceIgnore]
					get
					{
						return this.GetPSig();
					}
				}

				// Token: 0x06001F56 RID: 8022 RVA: 0x000665A8 File Offset: 0x000647A8
				private unsafe uint GetCSig()
				{
					method system.UInt32_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(System.UInt32(System.Void*), this.ptr_, system.UInt32_u0020(System.Void*));
				}

				// Token: 0x170006E2 RID: 1762
				// (get) Token: 0x06001F57 RID: 8023 RVA: 0x000665CF File Offset: 0x000647CF
				public uint m_cSig
				{
					[FatInterfaceIgnore]
					get
					{
						return this.GetCSig();
					}
				}

				// Token: 0x06001F58 RID: 8024 RVA: 0x000665D7 File Offset: 0x000647D7
				public unsafe StoredSigMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x04001342 RID: 4930
				private unsafe readonly void* ptr_;

				// Token: 0x04001343 RID: 4931
				[Nullable(1)]
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x02000585 RID: 1413
			[FatInterfaceImpl(typeof(CoreCLR.V60.StoredSigMethodDescPtr))]
			public struct StoredSigMethodDesc_64
			{
				// Token: 0x06001F5A RID: 8026 RVA: 0x0006661D File Offset: 0x0006481D
				private unsafe void* GetPSig()
				{
					return this.m_pSig;
				}

				// Token: 0x06001F5B RID: 8027 RVA: 0x00066625 File Offset: 0x00064825
				private uint GetCSig()
				{
					return this.m_cSig;
				}

				// Token: 0x170006E3 RID: 1763
				// (get) Token: 0x06001F5C RID: 8028 RVA: 0x0006662D File Offset: 0x0006482D
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.StoredSigMethodDesc_64.fatVtable_) == null)
						{
							result = (CoreCLR.V60.StoredSigMethodDesc_64.fatVtable_ = new IntPtr[]
							{
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetPSig_0|8_0),
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetCSig_1|8_1)
							});
						}
						return result;
					}
				}

				// Token: 0x06001F5D RID: 8029 RVA: 0x00066660 File Offset: 0x00064860
				[CompilerGenerated]
				internal unsafe static void* <get_FatVtable_>g__S_GetPSig_0|8_0(void* ptr__)
				{
					return ((CoreCLR.V60.StoredSigMethodDesc_64*)ptr__)->GetPSig();
				}

				// Token: 0x06001F5E RID: 8030 RVA: 0x00066668 File Offset: 0x00064868
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetCSig_1|8_1(void* ptr__)
				{
					return ((CoreCLR.V60.StoredSigMethodDesc_64*)ptr__)->GetCSig();
				}

				// Token: 0x04001344 RID: 4932
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x04001345 RID: 4933
				public unsafe void* m_pSig;

				// Token: 0x04001346 RID: 4934
				public uint m_cSig;

				// Token: 0x04001347 RID: 4935
				public uint m_dwExtendedFlags;

				// Token: 0x04001348 RID: 4936
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x02000586 RID: 1414
			[FatInterfaceImpl(typeof(CoreCLR.V60.StoredSigMethodDescPtr))]
			public struct StoredSigMethodDesc_32
			{
				// Token: 0x06001F5F RID: 8031 RVA: 0x00066670 File Offset: 0x00064870
				private unsafe void* GetPSig()
				{
					return this.m_pSig;
				}

				// Token: 0x06001F60 RID: 8032 RVA: 0x00066678 File Offset: 0x00064878
				private uint GetCSig()
				{
					return this.m_cSig;
				}

				// Token: 0x170006E4 RID: 1764
				// (get) Token: 0x06001F61 RID: 8033 RVA: 0x00066680 File Offset: 0x00064880
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.StoredSigMethodDesc_32.fatVtable_) == null)
						{
							result = (CoreCLR.V60.StoredSigMethodDesc_32.fatVtable_ = new IntPtr[]
							{
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetPSig_0|7_0),
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetCSig_1|7_1)
							});
						}
						return result;
					}
				}

				// Token: 0x06001F62 RID: 8034 RVA: 0x000666B3 File Offset: 0x000648B3
				[CompilerGenerated]
				internal unsafe static void* <get_FatVtable_>g__S_GetPSig_0|7_0(void* ptr__)
				{
					return ((CoreCLR.V60.StoredSigMethodDesc_32*)ptr__)->GetPSig();
				}

				// Token: 0x06001F63 RID: 8035 RVA: 0x000666BB File Offset: 0x000648BB
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetCSig_1|7_1(void* ptr__)
				{
					return ((CoreCLR.V60.StoredSigMethodDesc_32*)ptr__)->GetCSig();
				}

				// Token: 0x04001349 RID: 4937
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x0400134A RID: 4938
				public unsafe void* m_pSig;

				// Token: 0x0400134B RID: 4939
				public uint m_cSig;

				// Token: 0x0400134C RID: 4940
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x02000587 RID: 1415
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct FCallMethodDescPtr
			{
				// Token: 0x170006E5 RID: 1765
				// (get) Token: 0x06001F64 RID: 8036 RVA: 0x000666C3 File Offset: 0x000648C3
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? CoreCLR.V60.FCallMethodDesc_64.FatVtable_ : CoreCLR.V60.FCallMethodDesc_32.FatVtable_);

				// Token: 0x170006E6 RID: 1766
				// (get) Token: 0x06001F65 RID: 8037 RVA: 0x000666CA File Offset: 0x000648CA
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(CoreCLR.V60.FCallMethodDesc_64) : sizeof(CoreCLR.V60.FCallMethodDesc_32));

				// Token: 0x06001F66 RID: 8038 RVA: 0x000666D4 File Offset: 0x000648D4
				private unsafe uint GetECallID()
				{
					method system.UInt32_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(System.UInt32(System.Void*), this.ptr_, system.UInt32_u0020(System.Void*));
				}

				// Token: 0x170006E7 RID: 1767
				// (get) Token: 0x06001F67 RID: 8039 RVA: 0x000666FB File Offset: 0x000648FB
				public uint m_dwECallID
				{
					[FatInterfaceIgnore]
					get
					{
						return this.GetECallID();
					}
				}

				// Token: 0x06001F68 RID: 8040 RVA: 0x00066703 File Offset: 0x00064903
				[NullableContext(0)]
				public unsafe FCallMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x0400134F RID: 4943
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x04001350 RID: 4944
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x02000588 RID: 1416
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(CoreCLR.V60.FCallMethodDescPtr))]
			public struct FCallMethodDesc_64
			{
				// Token: 0x06001F6A RID: 8042 RVA: 0x00066749 File Offset: 0x00064949
				private uint GetECallID()
				{
					return this.m_dwECallID;
				}

				// Token: 0x170006E8 RID: 1768
				// (get) Token: 0x06001F6B RID: 8043 RVA: 0x00066751 File Offset: 0x00064951
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.FCallMethodDesc_64.fatVtable_) == null)
						{
							result = (CoreCLR.V60.FCallMethodDesc_64.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetECallID_0|6_0) });
						}
						return result;
					}
				}

				// Token: 0x06001F6C RID: 8044 RVA: 0x00066776 File Offset: 0x00064976
				[NullableContext(0)]
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetECallID_0|6_0(void* ptr__)
				{
					return ((CoreCLR.V60.FCallMethodDesc_64*)ptr__)->GetECallID();
				}

				// Token: 0x04001351 RID: 4945
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x04001352 RID: 4946
				public uint m_dwECallID;

				// Token: 0x04001353 RID: 4947
				public uint m_padding;

				// Token: 0x04001354 RID: 4948
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x02000589 RID: 1417
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(CoreCLR.V60.FCallMethodDescPtr))]
			public struct FCallMethodDesc_32
			{
				// Token: 0x06001F6D RID: 8045 RVA: 0x0006677E File Offset: 0x0006497E
				private uint GetECallID()
				{
					return this.m_dwECallID;
				}

				// Token: 0x170006E9 RID: 1769
				// (get) Token: 0x06001F6E RID: 8046 RVA: 0x00066786 File Offset: 0x00064986
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.FCallMethodDesc_32.fatVtable_) == null)
						{
							result = (CoreCLR.V60.FCallMethodDesc_32.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetECallID_0|5_0) });
						}
						return result;
					}
				}

				// Token: 0x06001F6F RID: 8047 RVA: 0x000667AB File Offset: 0x000649AB
				[NullableContext(0)]
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetECallID_0|5_0(void* ptr__)
				{
					return ((CoreCLR.V60.FCallMethodDesc_32*)ptr__)->GetECallID();
				}

				// Token: 0x04001355 RID: 4949
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x04001356 RID: 4950
				public uint m_dwECallID;

				// Token: 0x04001357 RID: 4951
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x0200058A RID: 1418
			public struct DynamicResolver
			{
			}

			// Token: 0x0200058B RID: 1419
			[Flags]
			public enum DynamicMethodDesc_ExtendedFlags
			{
				// Token: 0x04001359 RID: 4953
				Attrs = 65535,
				// Token: 0x0400135A RID: 4954
				ILStubAttrs = 23,
				// Token: 0x0400135B RID: 4955
				MemberAccessMask = 7,
				// Token: 0x0400135C RID: 4956
				ReverseStub = 8,
				// Token: 0x0400135D RID: 4957
				Static = 16,
				// Token: 0x0400135E RID: 4958
				CALLIStub = 32,
				// Token: 0x0400135F RID: 4959
				DelegateStub = 64,
				// Token: 0x04001360 RID: 4960
				StructMarshalStub = 128,
				// Token: 0x04001361 RID: 4961
				Unbreakable = 256,
				// Token: 0x04001362 RID: 4962
				SignatureNeedsResture = 1024,
				// Token: 0x04001363 RID: 4963
				StubNeedsCOMStarted = 2048,
				// Token: 0x04001364 RID: 4964
				MulticastStub = 4096,
				// Token: 0x04001365 RID: 4965
				UnboxingILStub = 8192,
				// Token: 0x04001366 RID: 4966
				WrapperDelegateStub = 16384,
				// Token: 0x04001367 RID: 4967
				UnmanagedCallersOnlyStub = 32768,
				// Token: 0x04001368 RID: 4968
				ILStub = 65536,
				// Token: 0x04001369 RID: 4969
				LCGMethod = 131072,
				// Token: 0x0400136A RID: 4970
				StackArgSize = 268173312
			}

			// Token: 0x0200058C RID: 1420
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct DynamicMethodDescPtr
			{
				// Token: 0x170006EA RID: 1770
				// (get) Token: 0x06001F70 RID: 8048 RVA: 0x000667B3 File Offset: 0x000649B3
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? CoreCLR.V60.DynamicMethodDesc_64.FatVtable_ : CoreCLR.V60.DynamicMethodDesc_32.FatVtable_);

				// Token: 0x170006EB RID: 1771
				// (get) Token: 0x06001F71 RID: 8049 RVA: 0x000667BA File Offset: 0x000649BA
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(CoreCLR.V60.DynamicMethodDesc_64) : sizeof(CoreCLR.V60.DynamicMethodDesc_32));

				// Token: 0x06001F72 RID: 8050 RVA: 0x000667C4 File Offset: 0x000649C4
				private unsafe CoreCLR.V60.DynamicMethodDesc_ExtendedFlags GetFlags()
				{
					method monoMod.Core.Interop.CoreCLR/V60/DynamicMethodDesc_ExtendedFlags_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(MonoMod.Core.Interop.CoreCLR/V60/DynamicMethodDesc_ExtendedFlags(System.Void*), this.ptr_, monoMod.Core.Interop.CoreCLR/V60/DynamicMethodDesc_ExtendedFlags_u0020(System.Void*));
				}

				// Token: 0x170006EC RID: 1772
				// (get) Token: 0x06001F73 RID: 8051 RVA: 0x000667EB File Offset: 0x000649EB
				public CoreCLR.V60.DynamicMethodDesc_ExtendedFlags Flags
				{
					get
					{
						return this.GetFlags();
					}
				}

				// Token: 0x06001F74 RID: 8052 RVA: 0x000667F3 File Offset: 0x000649F3
				[NullableContext(0)]
				public unsafe DynamicMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x0400136D RID: 4973
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x0400136E RID: 4974
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x0200058D RID: 1421
			[FatInterfaceImpl(typeof(CoreCLR.V60.DynamicMethodDescPtr))]
			public struct DynamicMethodDesc_64
			{
				// Token: 0x06001F76 RID: 8054 RVA: 0x00066839 File Offset: 0x00064A39
				private CoreCLR.V60.DynamicMethodDesc_ExtendedFlags GetFlags()
				{
					return (CoreCLR.V60.DynamicMethodDesc_ExtendedFlags)this.@base.m_dwExtendedFlags;
				}

				// Token: 0x170006ED RID: 1773
				// (get) Token: 0x06001F77 RID: 8055 RVA: 0x00066846 File Offset: 0x00064A46
				public CoreCLR.V60.DynamicMethodDesc_ExtendedFlags Flags
				{
					get
					{
						return this.GetFlags();
					}
				}

				// Token: 0x170006EE RID: 1774
				// (get) Token: 0x06001F78 RID: 8056 RVA: 0x0006684E File Offset: 0x00064A4E
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.DynamicMethodDesc_64.fatVtable_) == null)
						{
							result = (CoreCLR.V60.DynamicMethodDesc_64.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetFlags_0|8_0) });
						}
						return result;
					}
				}

				// Token: 0x06001F79 RID: 8057 RVA: 0x00066846 File Offset: 0x00064A46
				[CompilerGenerated]
				internal unsafe static CoreCLR.V60.DynamicMethodDesc_ExtendedFlags <get_FatVtable_>g__S_GetFlags_0|8_0(void* ptr__)
				{
					return ((CoreCLR.V60.DynamicMethodDesc_64*)ptr__)->GetFlags();
				}

				// Token: 0x0400136F RID: 4975
				public CoreCLR.V60.StoredSigMethodDesc_64 @base;

				// Token: 0x04001370 RID: 4976
				public unsafe byte* m_pszMethodName;

				// Token: 0x04001371 RID: 4977
				public unsafe CoreCLR.V60.DynamicResolver* m_pResolver;

				// Token: 0x04001372 RID: 4978
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x0200058E RID: 1422
			[FatInterfaceImpl(typeof(CoreCLR.V60.DynamicMethodDescPtr))]
			public struct DynamicMethodDesc_32
			{
				// Token: 0x06001F7A RID: 8058 RVA: 0x00066873 File Offset: 0x00064A73
				private CoreCLR.V60.DynamicMethodDesc_ExtendedFlags GetFlags()
				{
					return (CoreCLR.V60.DynamicMethodDesc_ExtendedFlags)this.m_dwExtendedFlags;
				}

				// Token: 0x170006EF RID: 1775
				// (get) Token: 0x06001F7B RID: 8059 RVA: 0x0006687B File Offset: 0x00064A7B
				public CoreCLR.V60.DynamicMethodDesc_ExtendedFlags Flags
				{
					get
					{
						return this.GetFlags();
					}
				}

				// Token: 0x170006F0 RID: 1776
				// (get) Token: 0x06001F7C RID: 8060 RVA: 0x00066883 File Offset: 0x00064A83
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.DynamicMethodDesc_32.fatVtable_) == null)
						{
							result = (CoreCLR.V60.DynamicMethodDesc_32.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetFlags_0|9_0) });
						}
						return result;
					}
				}

				// Token: 0x06001F7D RID: 8061 RVA: 0x0006687B File Offset: 0x00064A7B
				[CompilerGenerated]
				internal unsafe static CoreCLR.V60.DynamicMethodDesc_ExtendedFlags <get_FatVtable_>g__S_GetFlags_0|9_0(void* ptr__)
				{
					return ((CoreCLR.V60.DynamicMethodDesc_32*)ptr__)->GetFlags();
				}

				// Token: 0x04001373 RID: 4979
				public CoreCLR.V60.StoredSigMethodDesc_32 @base;

				// Token: 0x04001374 RID: 4980
				public unsafe byte* m_pszMethodName;

				// Token: 0x04001375 RID: 4981
				public unsafe CoreCLR.V60.DynamicResolver* m_pResolver;

				// Token: 0x04001376 RID: 4982
				public uint m_dwExtendedFlags;

				// Token: 0x04001377 RID: 4983
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x0200058F RID: 1423
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct ArrayMethodDescPtr
			{
				// Token: 0x170006F1 RID: 1777
				// (get) Token: 0x06001F7E RID: 8062 RVA: 0x000668A8 File Offset: 0x00064AA8
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? CoreCLR.V60.ArrayMethodDesc_64.FatVtable_ : CoreCLR.V60.ArrayMethodDesc_32.FatVtable_);

				// Token: 0x170006F2 RID: 1778
				// (get) Token: 0x06001F7F RID: 8063 RVA: 0x000668AF File Offset: 0x00064AAF
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(CoreCLR.V60.ArrayMethodDesc_64) : sizeof(CoreCLR.V60.ArrayMethodDesc_32));

				// Token: 0x06001F80 RID: 8064 RVA: 0x000668B6 File Offset: 0x00064AB6
				[NullableContext(0)]
				public unsafe ArrayMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x0400137A RID: 4986
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x0400137B RID: 4987
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x02000590 RID: 1424
			public enum ArrayFunc
			{
				// Token: 0x0400137D RID: 4989
				Get,
				// Token: 0x0400137E RID: 4990
				Set,
				// Token: 0x0400137F RID: 4991
				Address,
				// Token: 0x04001380 RID: 4992
				Ctor
			}

			// Token: 0x02000591 RID: 1425
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(CoreCLR.V60.ArrayMethodDescPtr))]
			public struct ArrayMethodDesc_64
			{
				// Token: 0x170006F3 RID: 1779
				// (get) Token: 0x06001F82 RID: 8066 RVA: 0x000668FC File Offset: 0x00064AFC
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.ArrayMethodDesc_64.fatVtable_) == null)
						{
							result = (CoreCLR.V60.ArrayMethodDesc_64.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x04001381 RID: 4993
				public CoreCLR.V60.StoredSigMethodDesc_64 @base;

				// Token: 0x04001382 RID: 4994
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x02000592 RID: 1426
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(CoreCLR.V60.ArrayMethodDescPtr))]
			public struct ArrayMethodDesc_32
			{
				// Token: 0x170006F4 RID: 1780
				// (get) Token: 0x06001F83 RID: 8067 RVA: 0x00066913 File Offset: 0x00064B13
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.ArrayMethodDesc_32.fatVtable_) == null)
						{
							result = (CoreCLR.V60.ArrayMethodDesc_32.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x04001383 RID: 4995
				public CoreCLR.V60.StoredSigMethodDesc_32 @base;

				// Token: 0x04001384 RID: 4996
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x02000593 RID: 1427
			public struct NDirectWriteableData
			{
			}

			// Token: 0x02000594 RID: 1428
			[Flags]
			public enum NDirectMethodDesc_Flags : ushort
			{
				// Token: 0x04001386 RID: 4998
				EarlyBound = 1,
				// Token: 0x04001387 RID: 4999
				HasSuppressUnmanagedCodeAccess = 2,
				// Token: 0x04001388 RID: 5000
				DefaultDllImportSearchPathIsCached = 4,
				// Token: 0x04001389 RID: 5001
				IsMarshalingRequiredCached = 16,
				// Token: 0x0400138A RID: 5002
				CachedMarshalingRequired = 32,
				// Token: 0x0400138B RID: 5003
				NativeAnsi = 64,
				// Token: 0x0400138C RID: 5004
				LastError = 128,
				// Token: 0x0400138D RID: 5005
				NativeNoMangle = 256,
				// Token: 0x0400138E RID: 5006
				VarArgs = 512,
				// Token: 0x0400138F RID: 5007
				StdCall = 1024,
				// Token: 0x04001390 RID: 5008
				ThisCall = 2048,
				// Token: 0x04001391 RID: 5009
				IsQCall = 4096,
				// Token: 0x04001392 RID: 5010
				DefaultDllImportSearchPathsStatus = 8192,
				// Token: 0x04001393 RID: 5011
				NDirectPopulated = 32768
			}

			// Token: 0x02000595 RID: 1429
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct NDirectMethodDescPtr
			{
				// Token: 0x170006F5 RID: 1781
				// (get) Token: 0x06001F84 RID: 8068 RVA: 0x0006692A File Offset: 0x00064B2A
				public static IntPtr[] CurrentVtable { get; } = ((PlatformDetection.Architecture == ArchitectureKind.x86) ? CoreCLR.V60.NDirectMethodDesc_x86.FatVtable_ : CoreCLR.V60.NDirectMethodDesc_other.FatVtable_);

				// Token: 0x170006F6 RID: 1782
				// (get) Token: 0x06001F85 RID: 8069 RVA: 0x00066931 File Offset: 0x00064B31
				public static int CurrentSize { get; } = ((PlatformDetection.Architecture == ArchitectureKind.x86) ? sizeof(CoreCLR.V60.NDirectMethodDesc_x86) : sizeof(CoreCLR.V60.NDirectMethodDesc_other));

				// Token: 0x06001F86 RID: 8070 RVA: 0x00066938 File Offset: 0x00064B38
				[NullableContext(0)]
				public unsafe NDirectMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x04001396 RID: 5014
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x04001397 RID: 5015
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x02000596 RID: 1430
			[FatInterfaceImpl(typeof(CoreCLR.V60.NDirectMethodDescPtr))]
			public struct NDirectMethodDesc_other
			{
				// Token: 0x170006F7 RID: 1783
				// (get) Token: 0x06001F88 RID: 8072 RVA: 0x0006697E File Offset: 0x00064B7E
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.NDirectMethodDesc_other.fatVtable_) == null)
						{
							result = (CoreCLR.V60.NDirectMethodDesc_other.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x04001398 RID: 5016
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x04001399 RID: 5017
				private CoreCLR.V60.NDirectMethodDesc_other.NDirect ndirect;

				// Token: 0x0400139A RID: 5018
				[Nullable(2)]
				private static IntPtr[] fatVtable_;

				// Token: 0x02000597 RID: 1431
				public struct NDirect
				{
					// Token: 0x0400139B RID: 5019
					public unsafe void* m_pNativeNDirectTarget;

					// Token: 0x0400139C RID: 5020
					public unsafe byte* m_pszEntrypointName;

					// Token: 0x0400139D RID: 5021
					[NativeInteger]
					public UIntPtr union_pszLibName_dwECallID;

					// Token: 0x0400139E RID: 5022
					public unsafe CoreCLR.V60.NDirectWriteableData* m_pWriteableData;

					// Token: 0x0400139F RID: 5023
					public unsafe void* m_pImportThunkGlue;

					// Token: 0x040013A0 RID: 5024
					public uint m_DefaultDllImportSearchPathsAttributeValue;

					// Token: 0x040013A1 RID: 5025
					public CoreCLR.V60.NDirectMethodDesc_Flags m_wFlags;

					// Token: 0x040013A2 RID: 5026
					public unsafe CoreCLR.V60.MethodDesc* m_pStubMD;
				}
			}

			// Token: 0x02000598 RID: 1432
			[FatInterfaceImpl(typeof(CoreCLR.V60.NDirectMethodDescPtr))]
			public struct NDirectMethodDesc_x86
			{
				// Token: 0x170006F8 RID: 1784
				// (get) Token: 0x06001F89 RID: 8073 RVA: 0x00066995 File Offset: 0x00064B95
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.NDirectMethodDesc_x86.fatVtable_) == null)
						{
							result = (CoreCLR.V60.NDirectMethodDesc_x86.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x040013A3 RID: 5027
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x040013A4 RID: 5028
				private CoreCLR.V60.NDirectMethodDesc_x86.NDirect ndirect;

				// Token: 0x040013A5 RID: 5029
				[Nullable(2)]
				private static IntPtr[] fatVtable_;

				// Token: 0x02000599 RID: 1433
				public struct NDirect
				{
					// Token: 0x040013A6 RID: 5030
					public unsafe void* m_pNativeNDirectTarget;

					// Token: 0x040013A7 RID: 5031
					public unsafe byte* m_pszEntrypointName;

					// Token: 0x040013A8 RID: 5032
					[NativeInteger]
					public UIntPtr union_pszLibName_dwECallID;

					// Token: 0x040013A9 RID: 5033
					public unsafe CoreCLR.V60.NDirectWriteableData* m_pWriteableData;

					// Token: 0x040013AA RID: 5034
					public unsafe void* m_pImportThunkGlue;

					// Token: 0x040013AB RID: 5035
					public uint m_DefaultDllImportSearchPathsAttributeValue;

					// Token: 0x040013AC RID: 5036
					public CoreCLR.V60.NDirectMethodDesc_Flags m_wFlags;

					// Token: 0x040013AD RID: 5037
					public ushort m_cbStackArgumentSize;

					// Token: 0x040013AE RID: 5038
					public unsafe CoreCLR.V60.MethodDesc* m_pStubMD;
				}
			}

			// Token: 0x0200059A RID: 1434
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct EEImplMethodDescPtr
			{
				// Token: 0x170006F9 RID: 1785
				// (get) Token: 0x06001F8A RID: 8074 RVA: 0x000669AC File Offset: 0x00064BAC
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? CoreCLR.V60.EEImplMethodDesc_64.FatVtable_ : CoreCLR.V60.EEImplMethodDesc_32.FatVtable_);

				// Token: 0x170006FA RID: 1786
				// (get) Token: 0x06001F8B RID: 8075 RVA: 0x000669B3 File Offset: 0x00064BB3
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(CoreCLR.V60.EEImplMethodDesc_64) : sizeof(CoreCLR.V60.EEImplMethodDesc_32));

				// Token: 0x06001F8C RID: 8076 RVA: 0x000669BA File Offset: 0x00064BBA
				[NullableContext(0)]
				public unsafe EEImplMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x040013B1 RID: 5041
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x040013B2 RID: 5042
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x0200059B RID: 1435
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(CoreCLR.V60.EEImplMethodDescPtr))]
			public struct EEImplMethodDesc_64
			{
				// Token: 0x170006FB RID: 1787
				// (get) Token: 0x06001F8E RID: 8078 RVA: 0x00066A00 File Offset: 0x00064C00
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.EEImplMethodDesc_64.fatVtable_) == null)
						{
							result = (CoreCLR.V60.EEImplMethodDesc_64.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x040013B3 RID: 5043
				public CoreCLR.V60.StoredSigMethodDesc_64 @base;

				// Token: 0x040013B4 RID: 5044
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x0200059C RID: 1436
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(CoreCLR.V60.EEImplMethodDescPtr))]
			public struct EEImplMethodDesc_32
			{
				// Token: 0x170006FC RID: 1788
				// (get) Token: 0x06001F8F RID: 8079 RVA: 0x00066A17 File Offset: 0x00064C17
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = CoreCLR.V60.EEImplMethodDesc_32.fatVtable_) == null)
						{
							result = (CoreCLR.V60.EEImplMethodDesc_32.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x040013B5 RID: 5045
				public CoreCLR.V60.StoredSigMethodDesc_32 @base;

				// Token: 0x040013B6 RID: 5046
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x0200059D RID: 1437
			public struct ComPlusCallMethodDesc
			{
				// Token: 0x040013B7 RID: 5047
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x040013B8 RID: 5048
				public unsafe void* m_pComPlusCallInfo;
			}

			// Token: 0x0200059E RID: 1438
			public struct InstantiatedMethodDesc
			{
				// Token: 0x170006FD RID: 1789
				// (get) Token: 0x06001F90 RID: 8080 RVA: 0x00066A2E File Offset: 0x00064C2E
				public bool IMD_HasMethodInstantiation
				{
					get
					{
						return this.IMD_IsGenericMethodDefinition || this.m_pPerInstInfo != null;
					}
				}

				// Token: 0x170006FE RID: 1790
				// (get) Token: 0x06001F91 RID: 8081 RVA: 0x00066A47 File Offset: 0x00064C47
				public bool IMD_IsGenericMethodDefinition
				{
					get
					{
						return (this.m_wFlags2 & CoreCLR.V60.InstantiatedMethodDesc.Flags.KindMask) == CoreCLR.V60.InstantiatedMethodDesc.Flags.GenericMethodDefinition;
					}
				}

				// Token: 0x170006FF RID: 1791
				// (get) Token: 0x06001F92 RID: 8082 RVA: 0x00066A54 File Offset: 0x00064C54
				public bool IMD_IsWrapperStubWithInstantiations
				{
					get
					{
						return (this.m_wFlags2 & CoreCLR.V60.InstantiatedMethodDesc.Flags.KindMask) == CoreCLR.V60.InstantiatedMethodDesc.Flags.WrapperStubWithInstantiations;
					}
				}

				// Token: 0x06001F93 RID: 8083 RVA: 0x00066A61 File Offset: 0x00064C61
				public unsafe CoreCLR.V60.MethodDesc* IMD_GetWrappedMethodDesc()
				{
					Helpers.Assert(this.IMD_IsWrapperStubWithInstantiations, null, "IMD_IsWrapperStubWithInstantiations");
					return (CoreCLR.V60.MethodDesc*)this.union_pDictLayout_pWrappedMethodDesc;
				}

				// Token: 0x040013B9 RID: 5049
				public CoreCLR.V60.MethodDesc @base;

				// Token: 0x040013BA RID: 5050
				public unsafe void* union_pDictLayout_pWrappedMethodDesc;

				// Token: 0x040013BB RID: 5051
				public unsafe CoreCLR.V60.Dictionary* m_pPerInstInfo;

				// Token: 0x040013BC RID: 5052
				public CoreCLR.V60.InstantiatedMethodDesc.Flags m_wFlags2;

				// Token: 0x040013BD RID: 5053
				public ushort m_wNumGenericArgs;

				// Token: 0x0200059F RID: 1439
				[Flags]
				public enum Flags : ushort
				{
					// Token: 0x040013BF RID: 5055
					KindMask = 7,
					// Token: 0x040013C0 RID: 5056
					GenericMethodDefinition = 0,
					// Token: 0x040013C1 RID: 5057
					UnsharedMethodInstantiation = 1,
					// Token: 0x040013C2 RID: 5058
					SharedMethodInstantiation = 2,
					// Token: 0x040013C3 RID: 5059
					WrapperStubWithInstantiations = 3,
					// Token: 0x040013C4 RID: 5060
					EnCAddedMethod = 7,
					// Token: 0x040013C5 RID: 5061
					Unrestored = 8,
					// Token: 0x040013C6 RID: 5062
					HasComPlusCallInfo = 16
				}
			}

			// Token: 0x020005A0 RID: 1440
			public struct Dictionary
			{
			}

			// Token: 0x020005A1 RID: 1441
			public struct Module
			{
			}

			// Token: 0x020005A2 RID: 1442
			public struct MethodTableWriteableData
			{
			}

			// Token: 0x020005A3 RID: 1443
			public struct VTableIndir2_t
			{
				// Token: 0x17000700 RID: 1792
				// (get) Token: 0x06001F94 RID: 8084 RVA: 0x00066A7A File Offset: 0x00064C7A
				public unsafe void* Value
				{
					get
					{
						return this.pCode;
					}
				}

				// Token: 0x040013C7 RID: 5063
				public unsafe void* pCode;
			}

			// Token: 0x020005A4 RID: 1444
			public struct VTableIndir_t
			{
				// Token: 0x040013C8 RID: 5064
				public unsafe CoreCLR.V60.VTableIndir2_t* Value;
			}

			// Token: 0x020005A5 RID: 1445
			private static class MultipurposeSlotHelpers
			{
				// Token: 0x06001F95 RID: 8085 RVA: 0x00066A84 File Offset: 0x00064C84
				public unsafe static byte OffsetOfMp1()
				{
					CoreCLR.V60.MethodTable t = default(CoreCLR.V60.MethodTable);
					return (byte)((long)((byte*)(&t.union_pPerInstInfo_ElementTypeHnd_pMultipurposeSlot1) - (byte*)(&t)));
				}

				// Token: 0x06001F96 RID: 8086 RVA: 0x00066AAC File Offset: 0x00064CAC
				public unsafe static byte OffsetOfMp2()
				{
					CoreCLR.V60.MethodTable t = default(CoreCLR.V60.MethodTable);
					return (byte)((long)((byte*)(&t.union_p_InterfaceMap_pMultipurposeSlot2) - (byte*)(&t)));
				}

				// Token: 0x06001F97 RID: 8087 RVA: 0x00066AD1 File Offset: 0x00064CD1
				public static byte RegularOffset(int index)
				{
					return (byte)(sizeof(CoreCLR.V60.MethodTable) + index * IntPtr.Size - 2 * IntPtr.Size);
				}
			}

			// Token: 0x020005A6 RID: 1446
			public struct MethodTable
			{
				// Token: 0x06001F98 RID: 8088 RVA: 0x00066AEC File Offset: 0x00064CEC
				public unsafe CoreCLR.V60.MethodTable* GetCanonicalMethodTable()
				{
					UIntPtr addr = this.union_pEEClass_pCanonMT;
					if ((addr & (UIntPtr)((IntPtr)2)) == 0)
					{
						return addr;
					}
					if ((addr & (UIntPtr)((IntPtr)1)) != 0)
					{
						return *(addr - (UIntPtr)((IntPtr)3));
					}
					return addr - 2;
				}

				// Token: 0x06001F99 RID: 8089 RVA: 0x00066B18 File Offset: 0x00064D18
				public unsafe CoreCLR.V60.MethodDesc* GetParallelMethodDesc(CoreCLR.V60.MethodDesc* pDefMD)
				{
					return this.GetMethodDescForSlot((uint)pDefMD->SlotNumber);
				}

				// Token: 0x17000701 RID: 1793
				// (get) Token: 0x06001F9A RID: 8090 RVA: 0x00066B26 File Offset: 0x00064D26
				public bool IsInterface
				{
					get
					{
						return (this.m_dwFlags & 983040U) == 786432U;
					}
				}

				// Token: 0x06001F9B RID: 8091 RVA: 0x00066B3B File Offset: 0x00064D3B
				public unsafe CoreCLR.V60.MethodDesc* GetMethodDescForSlot(uint slotNumber)
				{
					if (this.IsInterface)
					{
						this.GetNumVirtuals();
					}
					throw new NotImplementedException();
				}

				// Token: 0x06001F9C RID: 8092 RVA: 0x00066B54 File Offset: 0x00064D54
				public unsafe void* GetRestoredSlot(uint slotNumber)
				{
					CoreCLR.V60.MethodTable* pMT = (CoreCLR.V60.MethodTable*)Unsafe.AsPointer<CoreCLR.V60.MethodTable>(ref this);
					void* slot;
					for (;;)
					{
						pMT = pMT->GetCanonicalMethodTable();
						slot = pMT->GetSlot(slotNumber);
						if (slot != null)
						{
							break;
						}
						pMT = pMT->GetParentMethodTable();
					}
					return slot;
				}

				// Token: 0x17000702 RID: 1794
				// (get) Token: 0x06001F9D RID: 8093 RVA: 0x00066B86 File Offset: 0x00064D86
				public bool HasIndirectParent
				{
					get
					{
						return (this.m_dwFlags & 8388608U) > 0U;
					}
				}

				// Token: 0x06001F9E RID: 8094 RVA: 0x00066B98 File Offset: 0x00064D98
				public unsafe CoreCLR.V60.MethodTable* GetParentMethodTable()
				{
					void* ptr = this.m_pParentMethodTable;
					if (this.HasIndirectParent)
					{
						return *(IntPtr*)ptr;
					}
					return (CoreCLR.V60.MethodTable*)ptr;
				}

				// Token: 0x06001F9F RID: 8095 RVA: 0x00066BB8 File Offset: 0x00064DB8
				public unsafe void* GetSlot(uint slotNumber)
				{
					IntPtr pSlot = this.GetSlotPtrRaw(slotNumber);
					if (slotNumber < (uint)this.GetNumVirtuals())
					{
						return pSlot.Value;
					}
					if ((this.m_wFlags2 & CoreCLR.V60.MethodTable.Flags2.IsZapped) != ~(CoreCLR.V60.MethodTable.Flags2.MultipurposeSlotsMask | CoreCLR.V60.MethodTable.Flags2.IsZapped | CoreCLR.V60.MethodTable.Flags2.IsPreRestored | CoreCLR.V60.MethodTable.Flags2.HasModuleDependencies | CoreCLR.V60.MethodTable.Flags2.IsIntrinsicType | CoreCLR.V60.MethodTable.Flags2.RequiresDispatchTokenFat | CoreCLR.V60.MethodTable.Flags2.HasCctor | CoreCLR.V60.MethodTable.Flags2.HasVirtualStaticMethods | CoreCLR.V60.MethodTable.Flags2.REquiresAlign8 | CoreCLR.V60.MethodTable.Flags2.HasBoxedRegularStatics | CoreCLR.V60.MethodTable.Flags2.HasSingleNonVirtualSlot | CoreCLR.V60.MethodTable.Flags2.DependsOnEquivalentOrForwardedStructs) && slotNumber >= (uint)this.GetNumVirtuals())
					{
						return pSlot.Value;
					}
					return *pSlot;
				}

				// Token: 0x06001FA0 RID: 8096 RVA: 0x00066BFC File Offset: 0x00064DFC
				[return: NativeInteger]
				public unsafe IntPtr GetSlotPtrRaw(uint slotNum)
				{
					if (slotNum < (uint)this.GetNumVirtuals())
					{
						uint index = CoreCLR.V60.MethodTable.GetIndexOfVtableIndirection(slotNum);
						return CoreCLR.V60.MethodTable.VTableIndir_t__GetValueMaybeNullAtPtr(this.GetVtableIndirections() + (ulong)index * (ulong)((long)sizeof(CoreCLR.V60.VTableIndir_t)) / (ulong)sizeof(CoreCLR.V60.VTableIndir_t)) + (ulong)CoreCLR.V60.MethodTable.GetIndexAfterVtableIndirection(slotNum) * (ulong)((long)sizeof(CoreCLR.V60.VTableIndir2_t)) / (ulong)sizeof(CoreCLR.V60.VTableIndir2_t);
					}
					if (this.HasSingleNonVirtualSlot)
					{
						return this.GetNonVirtualSlotsPtr();
					}
					return this.GetNonVirtualSlotsArray() + (ulong)(slotNum - (uint)this.GetNumVirtuals()) * (ulong)((long)sizeof(void**)) / (ulong)sizeof(void**);
				}

				// Token: 0x06001FA1 RID: 8097 RVA: 0x00066C6B File Offset: 0x00064E6B
				public ushort GetNumVirtuals()
				{
					return this.m_wNumVirtuals;
				}

				// Token: 0x06001FA2 RID: 8098 RVA: 0x00066C73 File Offset: 0x00064E73
				public static uint GetIndexOfVtableIndirection(uint slotNum)
				{
					return slotNum >> 3;
				}

				// Token: 0x06001FA3 RID: 8099 RVA: 0x00066C78 File Offset: 0x00064E78
				public unsafe CoreCLR.V60.VTableIndir_t* GetVtableIndirections()
				{
					return (CoreCLR.V60.VTableIndir_t*)((byte*)Unsafe.AsPointer<CoreCLR.V60.MethodTable>(ref this) + sizeof(CoreCLR.V60.MethodTable));
				}

				// Token: 0x06001FA4 RID: 8100 RVA: 0x0001B6A2 File Offset: 0x000198A2
				public unsafe static CoreCLR.V60.VTableIndir2_t* VTableIndir_t__GetValueMaybeNullAtPtr([NativeInteger] IntPtr @base)
				{
					return @base;
				}

				// Token: 0x06001FA5 RID: 8101 RVA: 0x00066C87 File Offset: 0x00064E87
				public static uint GetIndexAfterVtableIndirection(uint slotNum)
				{
					return slotNum & 7U;
				}

				// Token: 0x17000703 RID: 1795
				// (get) Token: 0x06001FA6 RID: 8102 RVA: 0x00066C8C File Offset: 0x00064E8C
				public bool HasSingleNonVirtualSlot
				{
					get
					{
						return this.m_wFlags2.Has(CoreCLR.V60.MethodTable.Flags2.HasSingleNonVirtualSlot);
					}
				}

				// Token: 0x06001FA7 RID: 8103 RVA: 0x00066CA0 File Offset: 0x00064EA0
				[NullableContext(1)]
				[MultipurposeSlotOffsetTable(3, typeof(CoreCLR.V60.MultipurposeSlotHelpers))]
				private static byte[] GetNonVirtualSlotsOffsets()
				{
					return new byte[]
					{
						CoreCLR.V60.MultipurposeSlotHelpers.OffsetOfMp1(),
						CoreCLR.V60.MultipurposeSlotHelpers.OffsetOfMp2(),
						CoreCLR.V60.MultipurposeSlotHelpers.OffsetOfMp1(),
						CoreCLR.V60.MultipurposeSlotHelpers.RegularOffset(2),
						CoreCLR.V60.MultipurposeSlotHelpers.OffsetOfMp2(),
						CoreCLR.V60.MultipurposeSlotHelpers.RegularOffset(2),
						CoreCLR.V60.MultipurposeSlotHelpers.RegularOffset(2),
						CoreCLR.V60.MultipurposeSlotHelpers.RegularOffset(3)
					};
				}

				// Token: 0x06001FA8 RID: 8104 RVA: 0x00066CF7 File Offset: 0x00064EF7
				[return: NativeInteger]
				public IntPtr GetNonVirtualSlotsPtr()
				{
					return this.GetMultipurposeSlotPtr(CoreCLR.V60.MethodTable.Flags2.HasNonVirtualSlots, CoreCLR.V60.MethodTable.c_NonVirtualSlotsOffsets);
				}

				// Token: 0x06001FA9 RID: 8105 RVA: 0x00066D08 File Offset: 0x00064F08
				[NullableContext(1)]
				[return: NativeInteger]
				public unsafe IntPtr GetMultipurposeSlotPtr(CoreCLR.V60.MethodTable.Flags2 flag, byte[] offsets)
				{
					IntPtr offset = (IntPtr)((UIntPtr)offsets[(int)(this.m_wFlags2 & (flag - 1))]);
					if (offset >= (IntPtr)sizeof(CoreCLR.V60.MethodTable))
					{
						offset += (IntPtr)((UIntPtr)this.GetNumVTableIndirections() * (UIntPtr)((IntPtr)sizeof(CoreCLR.V60.VTableIndir_t)));
					}
					return (byte*)Unsafe.AsPointer<CoreCLR.V60.MethodTable>(ref this) + offset;
				}

				// Token: 0x06001FAA RID: 8106 RVA: 0x00066D47 File Offset: 0x00064F47
				public unsafe void*** GetNonVirtualSlotsArray()
				{
					return this.GetNonVirtualSlotsPtr();
				}

				// Token: 0x06001FAB RID: 8107 RVA: 0x00066D4F File Offset: 0x00064F4F
				public uint GetNumVTableIndirections()
				{
					return CoreCLR.V60.MethodTable.GetNumVtableIndirections((uint)this.GetNumVirtuals());
				}

				// Token: 0x06001FAC RID: 8108 RVA: 0x00066D5C File Offset: 0x00064F5C
				public static uint GetNumVtableIndirections(uint numVirtuals)
				{
					return numVirtuals + 7U >> 3;
				}

				// Token: 0x040013C9 RID: 5065
				public uint m_dwFlags;

				// Token: 0x040013CA RID: 5066
				public uint m_BaseSize;

				// Token: 0x040013CB RID: 5067
				public CoreCLR.V60.MethodTable.Flags2 m_wFlags2;

				// Token: 0x040013CC RID: 5068
				public ushort m_wToken;

				// Token: 0x040013CD RID: 5069
				public ushort m_wNumVirtuals;

				// Token: 0x040013CE RID: 5070
				public ushort m_wNumInterfaces;

				// Token: 0x040013CF RID: 5071
				private unsafe void* m_pParentMethodTable;

				// Token: 0x040013D0 RID: 5072
				public unsafe CoreCLR.V60.Module* m_pLoaderModule;

				// Token: 0x040013D1 RID: 5073
				public unsafe CoreCLR.V60.MethodTableWriteableData* m_pWriteableData;

				// Token: 0x040013D2 RID: 5074
				public unsafe void* union_pEEClass_pCanonMT;

				// Token: 0x040013D3 RID: 5075
				public unsafe void* union_pPerInstInfo_ElementTypeHnd_pMultipurposeSlot1;

				// Token: 0x040013D4 RID: 5076
				public unsafe void* union_p_InterfaceMap_pMultipurposeSlot2;

				// Token: 0x040013D5 RID: 5077
				public const int VTABLE_SLOTS_PER_CHUNK = 8;

				// Token: 0x040013D6 RID: 5078
				public const int VTABLE_SLOTS_PER_CHUNK_LOG2 = 3;

				// Token: 0x040013D7 RID: 5079
				[Nullable(1)]
				private static readonly byte[] c_NonVirtualSlotsOffsets = CoreCLR.V60.MethodTable.GetNonVirtualSlotsOffsets();

				// Token: 0x020005A7 RID: 1447
				[Flags]
				public enum Flags2 : ushort
				{
					// Token: 0x040013D9 RID: 5081
					MultipurposeSlotsMask = 31,
					// Token: 0x040013DA RID: 5082
					HasPerInstInfo = 1,
					// Token: 0x040013DB RID: 5083
					HasInterfaceMap = 2,
					// Token: 0x040013DC RID: 5084
					HasDispatchMapSlot = 4,
					// Token: 0x040013DD RID: 5085
					HasNonVirtualSlots = 8,
					// Token: 0x040013DE RID: 5086
					HasModuleOverride = 16,
					// Token: 0x040013DF RID: 5087
					IsZapped = 32,
					// Token: 0x040013E0 RID: 5088
					IsPreRestored = 64,
					// Token: 0x040013E1 RID: 5089
					HasModuleDependencies = 128,
					// Token: 0x040013E2 RID: 5090
					IsIntrinsicType = 256,
					// Token: 0x040013E3 RID: 5091
					RequiresDispatchTokenFat = 512,
					// Token: 0x040013E4 RID: 5092
					HasCctor = 1024,
					// Token: 0x040013E5 RID: 5093
					HasVirtualStaticMethods = 2048,
					// Token: 0x040013E6 RID: 5094
					REquiresAlign8 = 4096,
					// Token: 0x040013E7 RID: 5095
					HasBoxedRegularStatics = 8192,
					// Token: 0x040013E8 RID: 5096
					HasSingleNonVirtualSlot = 16384,
					// Token: 0x040013E9 RID: 5097
					DependsOnEquivalentOrForwardedStructs = 32768
				}

				// Token: 0x020005A8 RID: 1448
				public enum UnionLowBits
				{
					// Token: 0x040013EB RID: 5099
					EEClass,
					// Token: 0x040013EC RID: 5100
					Invalid,
					// Token: 0x040013ED RID: 5101
					MethodTable,
					// Token: 0x040013EE RID: 5102
					Indirection
				}
			}
		}

		// Token: 0x020005A9 RID: 1449
		public class V70 : CoreCLR.V60
		{
			// Token: 0x020005AA RID: 1450
			public new static class ICorJitInfoVtable
			{
				// Token: 0x040013EF RID: 5103
				public const int AllocMemIndex = 159;

				// Token: 0x040013F0 RID: 5104
				public const int TotalVtableCount = 175;
			}
		}

		// Token: 0x020005AB RID: 1451
		public class V80 : CoreCLR.V70
		{
			// Token: 0x020005AC RID: 1452
			public new static class ICorJitInfoVtable
			{
				// Token: 0x040013F1 RID: 5105
				public const int AllocMemIndex = 154;

				// Token: 0x040013F2 RID: 5106
				public const int TotalVtableCount = 170;
			}
		}

		// Token: 0x020005AD RID: 1453
		public class V90 : CoreCLR.V80
		{
			// Token: 0x020005AE RID: 1454
			public new static class ICorJitInfoVtable
			{
				// Token: 0x040013F3 RID: 5107
				public const int AllocMemIndex = 158;

				// Token: 0x040013F4 RID: 5108
				public const int TotalVtableCount = 174;
			}
		}
	}
}
