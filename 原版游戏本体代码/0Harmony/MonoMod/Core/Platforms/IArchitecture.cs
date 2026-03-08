using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Utils;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004F8 RID: 1272
	[NullableContext(1)]
	internal interface IArchitecture
	{
		// Token: 0x1700061C RID: 1564
		// (get) Token: 0x06001C5E RID: 7262
		ArchitectureKind Target { get; }

		// Token: 0x1700061D RID: 1565
		// (get) Token: 0x06001C5F RID: 7263
		ArchitectureFeature Features { get; }

		// Token: 0x1700061E RID: 1566
		// (get) Token: 0x06001C60 RID: 7264
		BytePatternCollection KnownMethodThunks { get; }

		// Token: 0x1700061F RID: 1567
		// (get) Token: 0x06001C61 RID: 7265
		IAltEntryFactory AltEntryFactory { get; }

		// Token: 0x06001C62 RID: 7266
		NativeDetourInfo ComputeDetourInfo(IntPtr from, IntPtr target, int maxSizeHint = -1);

		// Token: 0x06001C63 RID: 7267
		[NullableContext(0)]
		int GetDetourBytes(NativeDetourInfo info, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle);

		// Token: 0x06001C64 RID: 7268
		NativeDetourInfo ComputeRetargetInfo(NativeDetourInfo detour, IntPtr target, int maxSizeHint = -1);

		// Token: 0x06001C65 RID: 7269
		[NullableContext(0)]
		int GetRetargetBytes(NativeDetourInfo original, NativeDetourInfo retarget, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc);

		// Token: 0x06001C66 RID: 7270
		[return: Nullable(new byte[] { 0, 1 })]
		ReadOnlyMemory<IAllocatedMemory> CreateNativeVtableProxyStubs(IntPtr vtableBase, int vtableSize);

		// Token: 0x06001C67 RID: 7271
		IAllocatedMemory CreateSpecialEntryStub(IntPtr target, IntPtr argument);
	}
}
