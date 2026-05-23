using System;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures
{
	// Token: 0x02000559 RID: 1369
	internal abstract class DetourKindBase : INativeDetourKind
	{
		// Token: 0x170006AF RID: 1711
		// (get) Token: 0x06001EB3 RID: 7859
		public abstract int Size { get; }

		// Token: 0x06001EB4 RID: 7860
		[NullableContext(2)]
		public abstract int GetBytes(IntPtr from, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocHandle);

		// Token: 0x06001EB5 RID: 7861 RVA: 0x00064880 File Offset: 0x00062A80
		public static int GetDetourBytes(NativeDetourInfo info, Span<byte> buffer, [Nullable(2)] out IDisposable allocHandle)
		{
			Helpers.ThrowIfArgumentNull<INativeDetourKind>(info.InternalKind, "info.InternalKind");
			if (buffer.Length < info.Size)
			{
				throw new ArgumentException("Buffer too short", "buffer");
			}
			return ((DetourKindBase)info.InternalKind).GetBytes(info.From, info.To, buffer, info.InternalData, out allocHandle);
		}

		// Token: 0x06001EB6 RID: 7862
		public abstract bool TryGetRetargetInfo(NativeDetourInfo orig, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo);

		// Token: 0x06001EB7 RID: 7863 RVA: 0x000648E6 File Offset: 0x00062AE6
		public static bool TryFindRetargetInfo(NativeDetourInfo info, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo)
		{
			Helpers.ThrowIfArgumentNull<INativeDetourKind>(info.InternalKind, "info.InternalKind");
			return ((DetourKindBase)info.InternalKind).TryGetRetargetInfo(info, to, maxSize, out retargetInfo);
		}

		// Token: 0x06001EB8 RID: 7864
		[NullableContext(2)]
		public abstract int DoRetarget(NativeDetourInfo origInfo, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc);

		// Token: 0x06001EB9 RID: 7865 RVA: 0x00064910 File Offset: 0x00062B10
		public static int DoRetarget(NativeDetourInfo orig, NativeDetourInfo info, Span<byte> buffer, [Nullable(2)] out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
		{
			Helpers.ThrowIfArgumentNull<INativeDetourKind>(info.InternalKind, "info.InternalKind");
			if (buffer.Length < info.Size)
			{
				throw new ArgumentException("Buffer too short", "buffer");
			}
			return ((DetourKindBase)info.InternalKind).DoRetarget(orig, info.To, buffer, info.InternalData, out allocationHandle, out needsRepatch, out disposeOldAlloc);
		}
	}
}
