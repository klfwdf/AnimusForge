using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000491 RID: 1169
	[NullableContext(2)]
	internal interface IBufferWriter<T>
	{
		// Token: 0x06001A13 RID: 6675
		void Advance(int count);

		// Token: 0x06001A14 RID: 6676
		[return: Nullable(new byte[] { 0, 1 })]
		Memory<T> GetMemory(int sizeHint = 0);

		// Token: 0x06001A15 RID: 6677
		[return: Nullable(new byte[] { 0, 1 })]
		Span<T> GetSpan(int sizeHint = 0);
	}
}
