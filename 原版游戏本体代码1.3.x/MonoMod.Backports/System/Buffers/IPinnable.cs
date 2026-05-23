using System;

namespace System.Buffers
{
	// Token: 0x0200002D RID: 45
	public interface IPinnable
	{
		// Token: 0x060001BA RID: 442
		MemoryHandle Pin(int elementIndex);

		// Token: 0x060001BB RID: 443
		void Unpin();
	}
}
