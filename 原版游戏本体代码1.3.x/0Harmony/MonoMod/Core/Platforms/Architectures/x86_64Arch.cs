using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Platforms.Architectures.AltEntryFactories;
using MonoMod.Core.Utils;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures
{
	// Token: 0x0200055F RID: 1375
	internal sealed class x86_64Arch : IArchitecture
	{
		// Token: 0x170006B8 RID: 1720
		// (get) Token: 0x06001EDD RID: 7901 RVA: 0x0005D90B File Offset: 0x0005BB0B
		public ArchitectureKind Target
		{
			get
			{
				return ArchitectureKind.x86_64;
			}
		}

		// Token: 0x170006B9 RID: 1721
		// (get) Token: 0x06001EDE RID: 7902 RVA: 0x000413EB File Offset: 0x0003F5EB
		public ArchitectureFeature Features
		{
			get
			{
				return ArchitectureFeature.Immediate64 | ArchitectureFeature.CreateAltEntryPoint;
			}
		}

		// Token: 0x170006BA RID: 1722
		// (get) Token: 0x06001EDF RID: 7903 RVA: 0x00065136 File Offset: 0x00063336
		[Nullable(1)]
		public BytePatternCollection KnownMethodThunks
		{
			[NullableContext(1)]
			get
			{
				return Helpers.GetOrInit<BytePatternCollection>(ref this.lazyKnownMethodThunks, x86_64Arch.createKnownMethodThunksFunc);
			}
		}

		// Token: 0x170006BB RID: 1723
		// (get) Token: 0x06001EE0 RID: 7904 RVA: 0x00065148 File Offset: 0x00063348
		[Nullable(1)]
		public IAltEntryFactory AltEntryFactory
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x06001EE1 RID: 7905 RVA: 0x00065150 File Offset: 0x00063350
		[NullableContext(1)]
		private static BytePatternCollection CreateKnownMethodThunks()
		{
			RuntimeKind runtime = PlatformDetection.Runtime;
			bool flag = runtime - RuntimeKind.Framework <= 1;
			if (flag)
			{
				BytePattern[] array = new BytePattern[14];
				array[0] = new BytePattern(new AddressMeaning(AddressKind.Abs64), true, new ushort[]
				{
					72, 133, 201, 116, 65280, 72, 139, 1, 73, 65280,
					65280, 65280, 65280, 65280, 65280, 65280, 65280, 65280, 73, 59,
					194, 116, 65280, 72, 184, 65282, 65282, 65282, 65282, 65282,
					65282, 65282, 65282
				});
				array[1] = new BytePattern(new AddressMeaning(AddressKind.Rel32, 5), true, new ushort[] { 233, 65282, 65282, 65282, 65282, 95 });
				array[2] = new BytePattern(new AddressMeaning(AddressKind.Abs64), false, new ushort[]
				{
					72, 184, 65282, 65282, 65282, 65282, 65282, 65282, 65282, 65282,
					255, 224
				});
				array[3] = new BytePattern(new AddressMeaning(AddressKind.Rel32, 19), false, new byte[]
				{
					240, byte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0,
					byte.MaxValue, byte.MaxValue, 240, byte.MaxValue, byte.MaxValue, 0, 0, 0, 0
				}, new byte[]
				{
					64, 184, 0, 0, 0, 0, 0, 0, 0, 0,
					102, byte.MaxValue, 0, 15, 133, 2, 2, 2, 2
				});
				array[4] = new BytePattern(new AddressMeaning(AddressKind.Abs64), false, new byte[]
				{
					240, byte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0,
					byte.MaxValue, byte.MaxValue, 240, byte.MaxValue, 0, byte.MaxValue, byte.MaxValue, 0, 0, 0,
					0, 0, 0, 0, 0, byte.MaxValue, byte.MaxValue
				}, new byte[]
				{
					64, 184, 0, 0, 0, 0, 0, 0, 0, 0,
					102, byte.MaxValue, 0, 116, 0, 72, 184, 2, 2, 2,
					2, 2, 2, 2, 2, byte.MaxValue, 224
				});
				array[5] = new BytePattern(new AddressMeaning(AddressKind.PrecodeFixupThunkRel32, 5), true, new ushort[] { 232, 65282, 65282, 65282, 65282, 94 });
				array[6] = new BytePattern(new AddressMeaning(AddressKind.PrecodeFixupThunkRel32, 5), true, new ushort[] { 232, 65282, 65282, 65282, 65282, 204 });
				array[7] = new BytePattern(new AddressMeaning(AddressKind.Indirect, 6), true, new byte[]
				{
					byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue
				}, new byte[]
				{
					byte.MaxValue, 37, 2, 2, 2, 2, 76, 139, 21, 251,
					15, 0, 0, byte.MaxValue, 37, 253, 15, 0, 0
				});
				array[8] = new BytePattern(new AddressMeaning(AddressKind.PrecodeFixupThunkRel32 | AddressKind.Indirect, 13), true, new byte[]
				{
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0,
					0, 0, 0
				}, new byte[]
				{
					76, 139, 21, 251, 15, 0, 0, byte.MaxValue, 37, 2,
					2, 2, 2
				});
				array[9] = new BytePattern(new AddressMeaning(AddressKind.Indirect, 18), true, new byte[]
				{
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue
				}, new byte[]
				{
					72, 139, 5, 249, 15, 0, 0, 102, byte.MaxValue, 8,
					116, 6, byte.MaxValue, 37, 2, 2, 2, 2, byte.MaxValue, 37,
					248, 15, 0, 0
				});
				array[10] = new BytePattern(new AddressMeaning(AddressKind.Indirect, 6), true, new byte[]
				{
					byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue
				}, new byte[]
				{
					byte.MaxValue, 37, 2, 2, 2, 2, 76, 139, 21, 251,
					63, 0, 0, byte.MaxValue, 37, 253, 63, 0, 0
				});
				array[11] = new BytePattern(new AddressMeaning(AddressKind.PrecodeFixupThunkRel32 | AddressKind.Indirect, 13), true, new byte[]
				{
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0,
					0, 0, 0
				}, new byte[]
				{
					76, 139, 21, 251, 63, 0, 0, byte.MaxValue, 37, 2,
					2, 2, 2
				});
				array[12] = new BytePattern(new AddressMeaning(AddressKind.Indirect, 18), true, new byte[]
				{
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue
				}, new byte[]
				{
					72, 139, 5, 249, 63, 0, 0, 102, byte.MaxValue, 8,
					116, 6, byte.MaxValue, 37, 2, 2, 2, 2, byte.MaxValue, 37,
					248, 63, 0, 0
				});
				return new BytePatternCollection(array);
			}
			return new BytePatternCollection(new BytePattern[0]);
		}

		// Token: 0x06001EE2 RID: 7906 RVA: 0x0006542C File Offset: 0x0006362C
		[NullableContext(1)]
		public x86_64Arch(ISystem system)
		{
			this.system = system;
			this.AltEntryFactory = new IcedAltEntryFactory(system, 64);
		}

		// Token: 0x06001EE3 RID: 7907 RVA: 0x0006544C File Offset: 0x0006364C
		public NativeDetourInfo ComputeDetourInfo([NativeInteger] IntPtr from, [NativeInteger] IntPtr to, int sizeHint)
		{
			x86Shared.FixSizeHint(ref sizeHint);
			NativeDetourInfo rel32Info;
			if (x86Shared.TryRel32Detour(from, to, sizeHint, out rel32Info))
			{
				return rel32Info;
			}
			IntPtr target = from + (IntPtr)6;
			IntPtr lowBound = target + (IntPtr)int.MinValue;
			if (lowBound > target)
			{
				lowBound = (IntPtr)0;
			}
			IntPtr highBound = target + (IntPtr)int.MaxValue;
			if (highBound < target)
			{
				highBound = (IntPtr)(-1);
			}
			PositionedAllocationRequest memRequest = new PositionedAllocationRequest(target, lowBound, highBound, new AllocationRequest(IntPtr.Size));
			IAllocatedMemory allocated;
			if (sizeHint >= x86_64Arch.Rel32Ind64Kind.Instance.Size && this.system.MemoryAllocator.TryAllocateInRange(memRequest, out allocated))
			{
				return new NativeDetourInfo(from, to, x86_64Arch.Rel32Ind64Kind.Instance, allocated);
			}
			if (sizeHint < x86_64Arch.Abs64Kind.Instance.Size)
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(79, 1, ref flag);
				if (flag)
				{
					debugLogWarningStringHandler.AppendLiteral("Size too small for all known detour kinds; defaulting to Abs64. provided size: ");
					debugLogWarningStringHandler.AppendFormatted<int>(sizeHint);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			}
			return new NativeDetourInfo(from, to, x86_64Arch.Abs64Kind.Instance, null);
		}

		// Token: 0x06001EE4 RID: 7908 RVA: 0x000642E3 File Offset: 0x000624E3
		public int GetDetourBytes(NativeDetourInfo info, Span<byte> buffer, [Nullable(2)] out IDisposable allocHandle)
		{
			return DetourKindBase.GetDetourBytes(info, buffer, out allocHandle);
		}

		// Token: 0x06001EE5 RID: 7909 RVA: 0x00065520 File Offset: 0x00063720
		public NativeDetourInfo ComputeRetargetInfo(NativeDetourInfo detour, IntPtr to, int maxSizeHint = -1)
		{
			x86Shared.FixSizeHint(ref maxSizeHint);
			NativeDetourInfo retarget;
			if (DetourKindBase.TryFindRetargetInfo(detour, to, maxSizeHint, out retarget))
			{
				return retarget;
			}
			return this.ComputeDetourInfo(detour.From, to, maxSizeHint);
		}

		// Token: 0x06001EE6 RID: 7910 RVA: 0x00064321 File Offset: 0x00062521
		public int GetRetargetBytes(NativeDetourInfo original, NativeDetourInfo retarget, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
		{
			return DetourKindBase.DoRetarget(original, retarget, buffer, out allocationHandle, out needsRepatch, out disposeOldAlloc);
		}

		// Token: 0x170006BC RID: 1724
		// (get) Token: 0x06001EE7 RID: 7911 RVA: 0x00065551 File Offset: 0x00063751
		private unsafe static ReadOnlySpan<byte> VtblProxyStubWin
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.74A985B69363DA038C215EBDC95C2DEC71CCAA7EEE4FE7A349F688FAF77E0BAE), 16);
			}
		}

		// Token: 0x170006BD RID: 1725
		// (get) Token: 0x06001EE8 RID: 7912 RVA: 0x0006555F File Offset: 0x0006375F
		private unsafe static ReadOnlySpan<byte> VtblProxyStubSysV
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.D9C90CF616457576F11A613E9AD2971E209CEC74951E276569E1097ADFEB26D5), 16);
			}
		}

		// Token: 0x06001EE9 RID: 7913 RVA: 0x00065570 File Offset: 0x00063770
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<IAllocatedMemory> CreateNativeVtableProxyStubs(IntPtr vtableBase, int vtableSize)
		{
			ReadOnlySpan<byte> stubData;
			int indexOffs;
			bool premulOffset;
			if (PlatformDetection.OS.Is(OSKind.Windows))
			{
				stubData = x86_64Arch.VtblProxyStubWin;
				indexOffs = 9;
				premulOffset = true;
			}
			else
			{
				stubData = x86_64Arch.VtblProxyStubSysV;
				indexOffs = 9;
				premulOffset = true;
			}
			return Shared.CreateVtableStubs(this.system, vtableBase, vtableSize, stubData, indexOffs, premulOffset);
		}

		// Token: 0x170006BE RID: 1726
		// (get) Token: 0x06001EEA RID: 7914 RVA: 0x000655B2 File Offset: 0x000637B2
		private unsafe static ReadOnlySpan<byte> SpecEntryStub
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.EF9D8CA5CBE169340066F08BC0A169D0779A59AAABC495DBE3FF991D03534944), 23);
			}
		}

		// Token: 0x06001EEB RID: 7915 RVA: 0x000655C0 File Offset: 0x000637C0
		[NullableContext(1)]
		public unsafe IAllocatedMemory CreateSpecialEntryStub(IntPtr target, IntPtr argument)
		{
			int length = x86_64Arch.SpecEntryStub.Length;
			Span<byte> stub = new Span<byte>(stackalloc byte[(UIntPtr)length], length);
			x86_64Arch.SpecEntryStub.CopyTo(stub);
			Unsafe.WriteUnaligned<IntPtr>(stub[12], target);
			Unsafe.WriteUnaligned<IntPtr>(stub[2], argument);
			return Shared.CreateSingleExecutableStub(this.system, stub);
		}

		// Token: 0x040012C9 RID: 4809
		[Nullable(2)]
		private BytePatternCollection lazyKnownMethodThunks;

		// Token: 0x040012CB RID: 4811
		[Nullable(1)]
		private static readonly Func<BytePatternCollection> createKnownMethodThunksFunc = new Func<BytePatternCollection>(x86_64Arch.CreateKnownMethodThunks);

		// Token: 0x040012CC RID: 4812
		[Nullable(1)]
		private readonly ISystem system;

		// Token: 0x040012CD RID: 4813
		private const int VtblProxyStubIdxOffs = 9;

		// Token: 0x040012CE RID: 4814
		private const bool VtblProxyStubIdxPremul = true;

		// Token: 0x040012CF RID: 4815
		private const int SpecEntryStubArgOffs = 2;

		// Token: 0x040012D0 RID: 4816
		private const int SpecEntryStubTargetOffs = 12;

		// Token: 0x02000560 RID: 1376
		[NullableContext(2)]
		[Nullable(0)]
		private sealed class Abs64Kind : DetourKindBase
		{
			// Token: 0x170006BF RID: 1727
			// (get) Token: 0x06001EED RID: 7917 RVA: 0x00065635 File Offset: 0x00063835
			public override int Size
			{
				get
				{
					return 14;
				}
			}

			// Token: 0x06001EEE RID: 7918 RVA: 0x0006563C File Offset: 0x0006383C
			public unsafe override int GetBytes(IntPtr from, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocHandle)
			{
				*buffer[0] = byte.MaxValue;
				*buffer[1] = 37;
				Unsafe.WriteUnaligned<int>(buffer[2], 0);
				Unsafe.WriteUnaligned<long>(buffer[6], (long)to);
				allocHandle = null;
				return this.Size;
			}

			// Token: 0x06001EEF RID: 7919 RVA: 0x00065690 File Offset: 0x00063890
			public override bool TryGetRetargetInfo(NativeDetourInfo orig, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo)
			{
				NativeDetourInfo nativeDetourInfo = orig;
				nativeDetourInfo.To = to;
				retargetInfo = nativeDetourInfo;
				return true;
			}

			// Token: 0x06001EF0 RID: 7920 RVA: 0x0006484C File Offset: 0x00062A4C
			public override int DoRetarget(NativeDetourInfo origInfo, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
			{
				needsRepatch = true;
				disposeOldAlloc = true;
				return this.GetBytes(origInfo.From, to, buffer, data, out allocationHandle);
			}

			// Token: 0x040012D1 RID: 4817
			[Nullable(1)]
			public static readonly x86_64Arch.Abs64Kind Instance = new x86_64Arch.Abs64Kind();
		}

		// Token: 0x02000561 RID: 1377
		[NullableContext(2)]
		[Nullable(0)]
		private sealed class Rel32Ind64Kind : DetourKindBase
		{
			// Token: 0x170006C0 RID: 1728
			// (get) Token: 0x06001EF3 RID: 7923 RVA: 0x000413EB File Offset: 0x0003F5EB
			public override int Size
			{
				get
				{
					return 6;
				}
			}

			// Token: 0x06001EF4 RID: 7924 RVA: 0x000656BC File Offset: 0x000638BC
			public unsafe override int GetBytes(IntPtr from, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocHandle)
			{
				Helpers.ThrowIfArgumentNull<object>(data, "data");
				IAllocatedMemory alloc = (IAllocatedMemory)data;
				*buffer[0] = byte.MaxValue;
				*buffer[1] = 37;
				Unsafe.WriteUnaligned<int>(buffer[2], (int)(alloc.BaseAddress - (from + (IntPtr)6)));
				Unsafe.WriteUnaligned<IntPtr>(alloc.Memory[0], to);
				allocHandle = alloc;
				return this.Size;
			}

			// Token: 0x06001EF5 RID: 7925 RVA: 0x00065730 File Offset: 0x00063930
			public override bool TryGetRetargetInfo(NativeDetourInfo orig, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo)
			{
				NativeDetourInfo nativeDetourInfo = orig;
				nativeDetourInfo.To = to;
				retargetInfo = nativeDetourInfo;
				return true;
			}

			// Token: 0x06001EF6 RID: 7926 RVA: 0x00065750 File Offset: 0x00063950
			public override int DoRetarget(NativeDetourInfo origInfo, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
			{
				if (origInfo.InternalKind == this)
				{
					needsRepatch = false;
					disposeOldAlloc = false;
					Helpers.ThrowIfArgumentNull<object>(data, "data");
					IAllocatedMemory alloc = (IAllocatedMemory)data;
					Unsafe.WriteUnaligned<IntPtr>(alloc.Memory[0], to);
					allocationHandle = alloc;
					return 0;
				}
				needsRepatch = true;
				disposeOldAlloc = true;
				return this.GetBytes(origInfo.From, to, buffer, data, out allocationHandle);
			}

			// Token: 0x040012D2 RID: 4818
			[Nullable(1)]
			public static readonly x86_64Arch.Rel32Ind64Kind Instance = new x86_64Arch.Rel32Ind64Kind();
		}
	}
}
