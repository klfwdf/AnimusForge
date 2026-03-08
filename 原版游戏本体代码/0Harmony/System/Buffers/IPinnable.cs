using System;

namespace System.Buffers
{
	// Token: 0x02000493 RID: 1171
	internal interface IPinnable
	{
		// Token: 0x06001A17 RID: 6679
		MemoryHandle Pin(int elementIndex);

		// Token: 0x06001A18 RID: 6680
		void Unpin();
	}
}
