using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Core.Utils;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures
{
	// Token: 0x02000556 RID: 1366
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class Arm64Arch : IArchitecture
	{
		// Token: 0x170006AA RID: 1706
		// (get) Token: 0x06001EA0 RID: 7840 RVA: 0x00041313 File Offset: 0x0003F513
		public ArchitectureKind Target
		{
			get
			{
				return ArchitectureKind.Arm64;
			}
		}

		// Token: 0x170006AB RID: 1707
		// (get) Token: 0x06001EA1 RID: 7841 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		public ArchitectureFeature Features
		{
			get
			{
				return ArchitectureFeature.Immediate64;
			}
		}

		// Token: 0x170006AC RID: 1708
		// (get) Token: 0x06001EA2 RID: 7842 RVA: 0x00064254 File Offset: 0x00062454
		public BytePatternCollection KnownMethodThunks
		{
			get
			{
				Func<BytePatternCollection> init;
				if ((init = Arm64Arch.<>O.<0>__CreateKnownMethodThunks) == null)
				{
					init = (Arm64Arch.<>O.<0>__CreateKnownMethodThunks = new Func<BytePatternCollection>(Arm64Arch.CreateKnownMethodThunks));
				}
				return Helpers.GetOrInit<BytePatternCollection>(ref this.lazyKnownMethodThunks, init);
			}
		}

		// Token: 0x170006AD RID: 1709
		// (get) Token: 0x06001EA3 RID: 7843 RVA: 0x0002B871 File Offset: 0x00029A71
		public IAltEntryFactory AltEntryFactory
		{
			get
			{
				return null;
			}
		}

		// Token: 0x06001EA4 RID: 7844 RVA: 0x0006427C File Offset: 0x0006247C
		public Arm64Arch(ISystem system)
		{
			this.System = system;
		}

		// Token: 0x06001EA5 RID: 7845 RVA: 0x0006428C File Offset: 0x0006248C
		public NativeDetourInfo ComputeDetourInfo(IntPtr from, IntPtr target, int maxSizeHint)
		{
			x86Shared.FixSizeHint(ref maxSizeHint);
			if (maxSizeHint < Arm64Arch.BranchRegisterKind.Instance.Size)
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(88, 1, ref flag);
				if (flag)
				{
					debugLogWarningStringHandler.AppendLiteral("Size too small for all known detour kinds! Defaulting to BranchRegister. provided size: ");
					debugLogWarningStringHandler.AppendFormatted<int>(maxSizeHint);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			}
			return new NativeDetourInfo(from, target, Arm64Arch.BranchRegisterKind.Instance, null);
		}

		// Token: 0x06001EA6 RID: 7846 RVA: 0x000642E3 File Offset: 0x000624E3
		[NullableContext(0)]
		public int GetDetourBytes(NativeDetourInfo info, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle)
		{
			return DetourKindBase.GetDetourBytes(info, buffer, out allocationHandle);
		}

		// Token: 0x06001EA7 RID: 7847 RVA: 0x000642F0 File Offset: 0x000624F0
		public NativeDetourInfo ComputeRetargetInfo(NativeDetourInfo detour, IntPtr target, int maxSizeHint = -1)
		{
			x86Shared.FixSizeHint(ref maxSizeHint);
			NativeDetourInfo retarget;
			if (DetourKindBase.TryFindRetargetInfo(detour, target, maxSizeHint, out retarget))
			{
				return retarget;
			}
			return this.ComputeDetourInfo(detour.From, target, maxSizeHint);
		}

		// Token: 0x06001EA8 RID: 7848 RVA: 0x00064321 File Offset: 0x00062521
		[NullableContext(0)]
		public int GetRetargetBytes(NativeDetourInfo original, NativeDetourInfo retarget, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
		{
			return DetourKindBase.DoRetarget(original, retarget, buffer, out allocationHandle, out needsRepatch, out disposeOldAlloc);
		}

		// Token: 0x06001EA9 RID: 7849 RVA: 0x00064334 File Offset: 0x00062534
		[return: Nullable(new byte[] { 0, 1 })]
		public unsafe ReadOnlyMemory<IAllocatedMemory> CreateNativeVtableProxyStubs(IntPtr vtableBase, int vtableSize)
		{
			ReadOnlySpan<byte> stubData = new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.F3802BB374CB7730856DFA9B2994568F5F20D78A6D085EE83B6B63FE286F83C7), 28);
			return Shared.CreateVtableStubs(this.System, vtableBase, vtableSize, stubData, 24, true);
		}

		// Token: 0x06001EAA RID: 7850 RVA: 0x00064360 File Offset: 0x00062560
		public unsafe IAllocatedMemory CreateSpecialEntryStub(IntPtr target, IntPtr argument)
		{
			ReadOnlySpan<byte> stubData = new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.6C0B441759C39B924045B27B9BC8490EBEAD0D2E3FB37A7E54D7585CE448D12E), 32);
			int length = stubData.Length;
			Span<byte> stub = new Span<byte>(stackalloc byte[(UIntPtr)length], length);
			stubData.CopyTo(stub);
			Unsafe.WriteUnaligned<IntPtr>(stub[16], argument);
			Unsafe.WriteUnaligned<IntPtr>(stub[24], target);
			return Shared.CreateSingleExecutableStub(this.System, stub);
		}

		// Token: 0x06001EAB RID: 7851 RVA: 0x000643C8 File Offset: 0x000625C8
		private unsafe static BytePatternCollection CreateKnownMethodThunks()
		{
			RuntimeKind runtime = PlatformDetection.Runtime;
			bool flag = runtime - RuntimeKind.Framework <= 1;
			if (flag)
			{
				List<BytePattern> patterns = new List<BytePattern>
				{
					new BytePattern(new AddressMeaning(AddressKind.Abs64), true, new byte[]
					{
						byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
						byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0,
						0, 0, 0, 0
					}, new byte[]
					{
						137, 0, 0, 16, 42, 49, 64, 169, 64, 1,
						31, 214, 0, 0, 0, 0, 2, 2, 2, 2,
						2, 2, 2, 2
					}),
					new BytePattern(new AddressMeaning(AddressKind.Abs64), true, new byte[]
					{
						byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
						byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0,
						0, 0, 0, 0
					}, new byte[]
					{
						139, 0, 0, 16, 106, 49, 64, 169, 64, 1,
						31, 214, 0, 0, 0, 0, 2, 2, 2, 2,
						2, 2, 2, 2
					}),
					new BytePattern(new AddressMeaning(AddressKind.Abs64), true, new byte[]
					{
						byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
						byte.MaxValue, byte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0,
						0, 0, 0, 0
					}, new byte[]
					{
						12, 0, 0, 16, 107, 0, 0, 88, 96, 1,
						31, 214, 0, 0, 0, 0, 2, 2, 2, 2,
						2, 2, 2, 2
					}),
					new BytePattern(new AddressMeaning(AddressKind.Abs64), true, new byte[]
					{
						byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
						byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
						0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
						0, 0
					}, new byte[]
					{
						16, 0, 0, 145, 32, 0, 0, 145, 1, 2,
						0, 145, 112, 0, 0, 88, 0, 2, 31, 214,
						0, 0, 0, 0, 2, 2, 2, 2, 2, 2,
						2, 2
					})
				};
				int[] array;
				if ((array = <24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.85AD7009527B6DE3AC5F5E12927AD6128ABCC8515CCE30BCB0255EBFDEEECE0B_A6) == null)
				{
					array = (<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.85AD7009527B6DE3AC5F5E12927AD6128ABCC8515CCE30BCB0255EBFDEEECE0B_A6 = new int[] { 4096, 8192, 16384, 32768, 65536 });
				}
				ReadOnlySpan<int> pageSizes = new ReadOnlySpan<int>(array);
				ReadOnlySpan<byte> fixupPrecodeCode = new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.6429D8F0DD24CBCF7B3A2285E61F8070D49FF70D9E25C10C0FC23F7B01DA5111), 20);
				ReadOnlySpan<byte> fixupPrecodeCode2 = new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.FC7B60795B80EF58D2D4162DEE34AF892D9A11536D4150FA5C0B6BA59698AEDD), 24);
				ReadOnlySpan<byte> callCountingStubCode = new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.3D23FF3F668395EBA947A1A486FB6291E544D53C9F48661A3B9C2B51F481CEF7), 36);
				ReadOnlyMemory<byte> bigMask = new byte[]
				{
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
					byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue
				};
				ReadOnlySpan<int> readOnlySpan = pageSizes;
				for (int i = 0; i < readOnlySpan.Length; i++)
				{
					int pageSize = *readOnlySpan[i];
					byte[] fixupPrecode = fixupPrecodeCode.ToArray();
					Arm64Arch.EncodeLdr64LiteralTo(fixupPrecode.AsSpan(0), pageSize, 11);
					Arm64Arch.EncodeLdr64LiteralTo(fixupPrecode.AsSpan(8), -8 + pageSize + 8, 12);
					Arm64Arch.EncodeLdr64LiteralTo(fixupPrecode.AsSpan(12), -12 + pageSize + 16, 11);
					patterns.Add(new BytePattern(new AddressMeaning(AddressKind.Rel64 | AddressKind.Indirect | AddressKind.ConstantAddr, 0, (ulong)pageSize), true, bigMask.Slice(0, fixupPrecode.Length), fixupPrecode));
					patterns.Add(new BytePattern(new AddressMeaning(AddressKind.Rel64 | AddressKind.PrecodeFixupThunkRel32 | AddressKind.Indirect | AddressKind.ConstantAddr, 0, (ulong)(pageSize + 16 - 8)), true, bigMask.Slice(0, fixupPrecode.Length - 8), fixupPrecode.AsMemory(8)));
					byte[] fixupPrecode2 = fixupPrecodeCode2.ToArray();
					Arm64Arch.EncodeLdr64LiteralTo(fixupPrecode2.AsSpan(0), pageSize, 11);
					Arm64Arch.EncodeLdr64LiteralTo(fixupPrecode2.AsSpan(12), -12 + pageSize + 8, 12);
					Arm64Arch.EncodeLdr64LiteralTo(fixupPrecode2.AsSpan(16), -16 + pageSize + 16, 11);
					patterns.Add(new BytePattern(new AddressMeaning(AddressKind.Rel64 | AddressKind.Indirect | AddressKind.ConstantAddr, 0, (ulong)pageSize), true, bigMask.Slice(0, fixupPrecode2.Length), fixupPrecode2));
					patterns.Add(new BytePattern(new AddressMeaning(AddressKind.Rel64 | AddressKind.PrecodeFixupThunkRel32 | AddressKind.Indirect | AddressKind.ConstantAddr, 0, (ulong)(pageSize + 16 - 8)), true, bigMask.Slice(0, fixupPrecode2.Length - 8), fixupPrecode2.AsMemory(8)));
					byte[] callCountingStub = callCountingStubCode.ToArray();
					Arm64Arch.EncodeLdr64LiteralTo(callCountingStub.AsSpan(0), pageSize, 9);
					Arm64Arch.EncodeLdr64LiteralTo(callCountingStub.AsSpan(20), -20 + pageSize + 8, 9);
					Arm64Arch.EncodeLdr64LiteralTo(callCountingStub.AsSpan(28), -28 + pageSize + 16, 9);
					patterns.Add(new BytePattern(new AddressMeaning(AddressKind.Rel64 | AddressKind.Indirect | AddressKind.ConstantAddr, 0, (ulong)(pageSize + 8)), true, bigMask.Slice(0, callCountingStub.Length), callCountingStub));
				}
				return new BytePatternCollection(patterns.ToArray());
			}
			return new BytePatternCollection(new BytePattern[0]);
		}

		// Token: 0x06001EAC RID: 7852 RVA: 0x00064770 File Offset: 0x00062970
		[NullableContext(0)]
		private static void EncodeLdr64LiteralTo(Span<byte> dest, int offset, byte reg)
		{
			uint imm19 = (uint)offset >> 2;
			imm19 &= 524287U;
			uint opcode = 1476395008U;
			opcode |= imm19 << 5;
			opcode |= (uint)reg;
			MemoryMarshal.Write<uint>(dest, ref opcode);
		}

		// Token: 0x040012BC RID: 4796
		[Nullable(2)]
		private BytePatternCollection lazyKnownMethodThunks;

		// Token: 0x040012BD RID: 4797
		private readonly ISystem System;

		// Token: 0x02000557 RID: 1367
		[NullableContext(2)]
		[Nullable(0)]
		private sealed class BranchRegisterKind : DetourKindBase
		{
			// Token: 0x170006AE RID: 1710
			// (get) Token: 0x06001EAD RID: 7853 RVA: 0x0005FE37 File Offset: 0x0005E037
			public override int Size
			{
				get
				{
					return 16;
				}
			}

			// Token: 0x06001EAE RID: 7854 RVA: 0x000647A4 File Offset: 0x000629A4
			public unsafe override int GetBytes(IntPtr from, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocHandle)
			{
				ReadOnlySpan<byte> stubData = new ReadOnlySpan<byte>((void*)(&<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.464FCBB1223EB3625C4B5CE8ED1C096B7322AFC2C52C73CE667DAB0F609B3B17), 16);
				stubData.CopyTo(buffer);
				Unsafe.WriteUnaligned<ulong>(buffer[8], (ulong)(long)to);
				allocHandle = null;
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(29, 2, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("Detouring arm64 from 0x");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(from, "X16");
					debugLogTraceStringHandler.AppendLiteral(" to 0x");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(to, "X16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
				return this.Size;
			}

			// Token: 0x06001EAF RID: 7855 RVA: 0x0006482C File Offset: 0x00062A2C
			public override bool TryGetRetargetInfo(NativeDetourInfo orig, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo)
			{
				NativeDetourInfo nativeDetourInfo = orig;
				nativeDetourInfo.To = to;
				retargetInfo = nativeDetourInfo;
				return true;
			}

			// Token: 0x06001EB0 RID: 7856 RVA: 0x0006484C File Offset: 0x00062A4C
			public override int DoRetarget(NativeDetourInfo origInfo, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
			{
				needsRepatch = true;
				disposeOldAlloc = true;
				return this.GetBytes(origInfo.From, to, buffer, data, out allocationHandle);
			}

			// Token: 0x040012BE RID: 4798
			[Nullable(1)]
			public static readonly Arm64Arch.BranchRegisterKind Instance = new Arm64Arch.BranchRegisterKind();
		}

		// Token: 0x02000558 RID: 1368
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x040012BF RID: 4799
			[Nullable(new byte[] { 0, 1 })]
			public static Func<BytePatternCollection> <0>__CreateKnownMethodThunks;
		}
	}
}
