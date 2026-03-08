using System;
using System.Runtime.CompilerServices;

namespace System.IO
{
	// Token: 0x02000487 RID: 1159
	[NullableContext(1)]
	[Nullable(0)]
	internal static class StreamExtensions
	{
		// Token: 0x060019E7 RID: 6631 RVA: 0x00054B37 File Offset: 0x00052D37
		public static void CopyTo(this Stream src, Stream destination)
		{
			ThrowHelper.ThrowIfArgumentNull(src, "src", null);
			src.CopyTo(destination);
		}

		// Token: 0x060019E8 RID: 6632 RVA: 0x00054B4C File Offset: 0x00052D4C
		public static void CopyTo(this Stream src, Stream destination, int bufferSize)
		{
			ThrowHelper.ThrowIfArgumentNull(src, "src", null);
			src.CopyTo(destination, bufferSize);
		}
	}
}
