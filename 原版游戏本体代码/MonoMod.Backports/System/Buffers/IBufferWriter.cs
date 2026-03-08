using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200002B RID: 43
	[NullableContext(2)]
	public interface IBufferWriter<T>
	{
		// Token: 0x060001B6 RID: 438
		void Advance(int count);

		// Token: 0x060001B7 RID: 439
		[return: Nullable(new byte[] { 0, 1 })]
		Memory<T> GetMemory(int sizeHint = 0);

		// Token: 0x060001B8 RID: 440
		[return: Nullable(new byte[] { 0, 1 })]
		Span<T> GetSpan(int sizeHint = 0);
	}
}
