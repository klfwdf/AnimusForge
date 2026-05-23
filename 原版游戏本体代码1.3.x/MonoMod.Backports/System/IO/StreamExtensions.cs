using System;
using System.Runtime.CompilerServices;

namespace System.IO
{
	// Token: 0x02000025 RID: 37
	[NullableContext(1)]
	[Nullable(0)]
	public static class StreamExtensions
	{
		// Token: 0x06000196 RID: 406 RVA: 0x00009AC3 File Offset: 0x00007CC3
		public static void CopyTo(this Stream src, Stream destination)
		{
			ThrowHelper.ThrowIfArgumentNull(src, "src", null);
			src.CopyTo(destination);
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00009AD8 File Offset: 0x00007CD8
		public static void CopyTo(this Stream src, Stream destination, int bufferSize)
		{
			ThrowHelper.ThrowIfArgumentNull(src, "src", null);
			src.CopyTo(destination, bufferSize);
		}
	}
}
