using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Platforms.Architectures.AltEntryFactories;
using MonoMod.Core.Utils;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures
{
	// Token: 0x0200055B RID: 1371
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class x86Arch : IArchitecture
	{
		// Token: 0x170006B0 RID: 1712
		// (get) Token: 0x06001EBF RID: 7871 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		public ArchitectureKind Target
		{
			get
			{
				return ArchitectureKind.x86;
			}
		}

		// Token: 0x170006B1 RID: 1713
		// (get) Token: 0x06001EC0 RID: 7872 RVA: 0x000411A5 File Offset: 0x0003F3A5
		public ArchitectureFeature Features
		{
			get
			{
				return ArchitectureFeature.CreateAltEntryPoint;
			}
		}

		// Token: 0x170006B2 RID: 1714
		// (get) Token: 0x06001EC1 RID: 7873 RVA: 0x00064D1F File Offset: 0x00062F1F
		public BytePatternCollection KnownMethodThunks
		{
			get
			{
				return Helpers.GetOrInit<BytePatternCollection>(ref this.lazyKnownMethodThunks, x86Arch.createKnownMethodThunksFunc);
			}
		}

		// Token: 0x170006B3 RID: 1715
		// (get) Token: 0x06001EC2 RID: 7874 RVA: 0x00064D31 File Offset: 0x00062F31
		public IAltEntryFactory AltEntryFactory { get; }

		// Token: 0x06001EC3 RID: 7875 RVA: 0x00064D3C File Offset: 0x00062F3C
		private static BytePatternCollection CreateKnownMethodThunks()
		{
			RuntimeKind runtime = PlatformDetection.Runtime;
			bool flag = runtime - RuntimeKind.Framework <= 1;
			if (flag)
			{
				BytePattern[] array = new BytePattern[7];
				array[0] = new BytePattern(new AddressMeaning(AddressKind.Rel32, 16), new ushort[]
				{
					184, 65280, 65280, 65280, 65280, 144, 232, 65280, 65280, 65280,
					65280, 233, 65282, 65282, 65282, 65282
				});
				array[1] = new BytePattern(new AddressMeaning(AddressKind.Rel32, 5), true, new ushort[] { 233, 65282, 65282, 65282, 65282, 95 });
				array[2] = new BytePattern(new AddressMeaning(AddressKind.PrecodeFixupThunkRel32, 5), true, new ushort[] { 232, 65282, 65282, 65282, 65282, 94 });
				array[3] = new BytePattern(new AddressMeaning(AddressKind.PrecodeFixupThunkRel32, 5), true, new ushort[] { 232, 65282, 65282, 65282, 65282, 204 });
				array[4] = new BytePattern(new AddressMeaning(AddressKind.Abs32 | AddressKind.Indirect), true, new ushort[]
				{
					255, 37, 65282, 65282, 65282, 65282, 161, 65280, 65280, 65280,
					65280, 255, 37, 65280, 65280, 65280, 65280
				});
				array[5] = new BytePattern(new AddressMeaning(AddressKind.Abs32 | AddressKind.PrecodeFixupThunkRel32 | AddressKind.Indirect), true, new ushort[]
				{
					161, 65280, 65280, 65280, 65280, 255, 37, 65282, 65282, 65282,
					65282
				});
				return new BytePatternCollection(array);
			}
			return new BytePatternCollection(new BytePattern[0]);
		}

		// Token: 0x06001EC4 RID: 7876 RVA: 0x00064E44 File Offset: 0x00063044
		public NativeDetourInfo ComputeDetourInfo(IntPtr from, IntPtr to, int maxSizeHint = -1)
		{
			x86Shared.FixSizeHint(ref maxSizeHint);
			NativeDetourInfo rel32Info;
			if (x86Shared.TryRel32Detour(from, to, maxSizeHint, out rel32Info))
			{
				return rel32Info;
			}
			if (maxSizeHint < x86Arch.Abs32Kind.Instance.Size)
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(79, 1, ref flag);
				if (flag)
				{
					debugLogWarningStringHandler.AppendLiteral("Size too small for all known detour kinds; defaulting to Abs32. provided size: ");
					debugLogWarningStringHandler.AppendFormatted<int>(maxSizeHint);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			}
			return new NativeDetourInfo(from, to, x86Arch.Abs32Kind.Instance, null);
		}

		// Token: 0x06001EC5 RID: 7877 RVA: 0x000642E3 File Offset: 0x000624E3
		[NullableContext(0)]
		public int GetDetourBytes(NativeDetourInfo info, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle)
		{
			return DetourKindBase.GetDetourBytes(info, buffer, out allocationHandle);
		}

		// Token: 0x06001EC6 RID: 7878 RVA: 0x00064EAC File Offset: 0x000630AC
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

		// Token: 0x06001EC7 RID: 7879 RVA: 0x00064321 File Offset: 0x00062521
		[NullableContext(0)]
		public int GetRetargetBytes(NativeDetourInfo original, NativeDetourInfo retarget, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
		{
			return DetourKindBase.DoRetarget(original, retarget, buffer, out allocationHandle, out needsRepatch, out disposeOldAlloc);
		}

		// Token: 0x06001EC8 RID: 7880 RVA: 0x00064EDD File Offset: 0x000630DD
		public x86Arch(ISystem system)
		{
			this.system = system;
			this.AltEntryFactory = new IcedAltEntryFactory(system, 32);
		}

		// Token: 0x170006B4 RID: 1716
		// (get) Token: 0x06001EC9 RID: 7881 RVA: 0x00064EFA File Offset: 0x000630FA
		[Nullable(0)]
		private unsafe static ReadOnlySpan<byte> WinThisVtableProxyThunk
		{
			[NullableContext(0)]
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.CC04E60244F11264BA0C35EBD477099E8E811C6267800C53D2A16574265D530A), 12);
			}
		}

		// Token: 0x06001ECA RID: 7882 RVA: 0x00064F08 File Offset: 0x00063108
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<IAllocatedMemory> CreateNativeVtableProxyStubs(IntPtr vtableBase, int vtableSize)
		{
			OSKind kernel = PlatformDetection.OS.GetKernel();
			bool premulOffset = true;
			if (kernel.Is(OSKind.Windows))
			{
				ReadOnlySpan<byte> stubData = x86Arch.WinThisVtableProxyThunk;
				int indexOffs = 7;
				return Shared.CreateVtableStubs(this.system, vtableBase, vtableSize, stubData, indexOffs, premulOffset);
			}
			throw new PlatformNotSupportedException();
		}

		// Token: 0x170006B5 RID: 1717
		// (get) Token: 0x06001ECB RID: 7883 RVA: 0x00064F49 File Offset: 0x00063149
		[Nullable(0)]
		private unsafe static ReadOnlySpan<byte> SpecEntryStub
		{
			[NullableContext(0)]
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.73C5DDF3D1B50EF2973C84F1EFCA106392E86FA50DDE4274E0BB239D13E1B2C4), 12);
			}
		}

		// Token: 0x06001ECC RID: 7884 RVA: 0x00064F58 File Offset: 0x00063158
		public unsafe IAllocatedMemory CreateSpecialEntryStub(IntPtr target, IntPtr argument)
		{
			int length = x86Arch.SpecEntryStub.Length;
			Span<byte> stub = new Span<byte>(stackalloc byte[(UIntPtr)length], length);
			x86Arch.SpecEntryStub.CopyTo(stub);
			Unsafe.WriteUnaligned<IntPtr>(stub[6], target);
			Unsafe.WriteUnaligned<IntPtr>(stub[1], argument);
			return Shared.CreateSingleExecutableStub(this.system, stub);
		}

		// Token: 0x040012C0 RID: 4800
		[Nullable(2)]
		private BytePatternCollection lazyKnownMethodThunks;

		// Token: 0x040012C2 RID: 4802
		private static readonly Func<BytePatternCollection> createKnownMethodThunksFunc = new Func<BytePatternCollection>(x86Arch.CreateKnownMethodThunks);

		// Token: 0x040012C3 RID: 4803
		private readonly ISystem system;

		// Token: 0x040012C4 RID: 4804
		private const int WinThisVtableThunkIndexOffs = 7;

		// Token: 0x040012C5 RID: 4805
		private const int SpecEntryStubArgOffs = 1;

		// Token: 0x040012C6 RID: 4806
		private const int SpecEntryStubTargetOffs = 6;

		// Token: 0x0200055C RID: 1372
		[NullableContext(2)]
		[Nullable(0)]
		private sealed class Abs32Kind : DetourKindBase
		{
			// Token: 0x170006B6 RID: 1718
			// (get) Token: 0x06001ECE RID: 7886 RVA: 0x000413EB File Offset: 0x0003F5EB
			public override int Size
			{
				get
				{
					return 6;
				}
			}

			// Token: 0x06001ECF RID: 7887 RVA: 0x00064FCC File Offset: 0x000631CC
			public unsafe override int GetBytes(IntPtr from, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocHandle)
			{
				*buffer[0] = 104;
				Unsafe.WriteUnaligned<int>(buffer[1], *Unsafe.As<IntPtr, int>(ref to));
				*buffer[5] = 195;
				allocHandle = null;
				return this.Size;
			}

			// Token: 0x06001ED0 RID: 7888 RVA: 0x00065008 File Offset: 0x00063208
			public override bool TryGetRetargetInfo(NativeDetourInfo orig, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo)
			{
				NativeDetourInfo nativeDetourInfo = orig;
				nativeDetourInfo.To = to;
				retargetInfo = nativeDetourInfo;
				return true;
			}

			// Token: 0x06001ED1 RID: 7889 RVA: 0x0006484C File Offset: 0x00062A4C
			public override int DoRetarget(NativeDetourInfo origInfo, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
			{
				needsRepatch = true;
				disposeOldAlloc = true;
				return this.GetBytes(origInfo.From, to, buffer, data, out allocationHandle);
			}

			// Token: 0x040012C7 RID: 4807
			[Nullable(1)]
			public static readonly x86Arch.Abs32Kind Instance = new x86Arch.Abs32Kind();
		}
	}
}
