using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D7 RID: 727
	internal abstract class Heap
	{
		// Token: 0x060012CA RID: 4810 RVA: 0x0003B373 File Offset: 0x00039573
		protected Heap(byte[] data)
		{
			this.data = data;
		}

		// Token: 0x04000738 RID: 1848
		public int IndexSize;

		// Token: 0x04000739 RID: 1849
		internal readonly byte[] data;
	}
}
