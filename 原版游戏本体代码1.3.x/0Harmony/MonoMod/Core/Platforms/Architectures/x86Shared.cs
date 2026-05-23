using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms.Architectures
{
	// Token: 0x0200055D RID: 1373
	internal static class x86Shared
	{
		// Token: 0x06001ED4 RID: 7892 RVA: 0x00065034 File Offset: 0x00063234
		public static void FixSizeHint(ref int sizeHint)
		{
			if (sizeHint < 0)
			{
				sizeHint = int.MaxValue;
			}
		}

		// Token: 0x06001ED5 RID: 7893 RVA: 0x00065044 File Offset: 0x00063244
		public unsafe static bool TryRel32Detour([NativeInteger] IntPtr from, [NativeInteger] IntPtr to, int sizeHint, out NativeDetourInfo info)
		{
			IntPtr rel = to - (from + (IntPtr)5);
			if (sizeHint >= x86Shared.Rel32Kind.Instance.Size && (x86Shared.Is32Bit((long)rel) || x86Shared.Is32Bit((long)(-(long)rel))) && *(from + 5) != 95)
			{
				info = new NativeDetourInfo(from, to, x86Shared.Rel32Kind.Instance, null);
				return true;
			}
			info = default(NativeDetourInfo);
			return false;
		}

		// Token: 0x06001ED6 RID: 7894 RVA: 0x0006509D File Offset: 0x0006329D
		public static bool Is32Bit(long to)
		{
			return (to & 2147483647L) == to;
		}

		// Token: 0x0200055E RID: 1374
		[NullableContext(2)]
		[Nullable(0)]
		public sealed class Rel32Kind : DetourKindBase
		{
			// Token: 0x170006B7 RID: 1719
			// (get) Token: 0x06001ED7 RID: 7895 RVA: 0x00041313 File Offset: 0x0003F513
			public override int Size
			{
				get
				{
					return 5;
				}
			}

			// Token: 0x06001ED8 RID: 7896 RVA: 0x000650AA File Offset: 0x000632AA
			public unsafe override int GetBytes(IntPtr from, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocHandle)
			{
				*buffer[0] = 233;
				Unsafe.WriteUnaligned<int>(buffer[1], (int)(to - (from + (IntPtr)5)));
				allocHandle = null;
				return this.Size;
			}

			// Token: 0x06001ED9 RID: 7897 RVA: 0x000650D8 File Offset: 0x000632D8
			public override bool TryGetRetargetInfo(NativeDetourInfo orig, IntPtr to, int maxSize, out NativeDetourInfo retargetInfo)
			{
				IntPtr rel = to - (orig.From + (IntPtr)5);
				if (x86Shared.Is32Bit((long)rel) || x86Shared.Is32Bit((long)(-(long)rel)))
				{
					retargetInfo = new NativeDetourInfo(orig.From, to, x86Shared.Rel32Kind.Instance, null);
					return true;
				}
				retargetInfo = default(NativeDetourInfo);
				return false;
			}

			// Token: 0x06001EDA RID: 7898 RVA: 0x0006484C File Offset: 0x00062A4C
			public override int DoRetarget(NativeDetourInfo origInfo, IntPtr to, [Nullable(0)] Span<byte> buffer, object data, out IDisposable allocationHandle, out bool needsRepatch, out bool disposeOldAlloc)
			{
				needsRepatch = true;
				disposeOldAlloc = true;
				return this.GetBytes(origInfo.From, to, buffer, data, out allocationHandle);
			}

			// Token: 0x040012C8 RID: 4808
			[Nullable(1)]
			public static readonly x86Shared.Rel32Kind Instance = new x86Shared.Rel32Kind();
		}
	}
}
