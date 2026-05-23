using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D3 RID: 723
	internal sealed class PdbHeapBuffer : HeapBuffer
	{
		// Token: 0x170004D7 RID: 1239
		// (get) Token: 0x060012C6 RID: 4806 RVA: 0x0001B69F File Offset: 0x0001989F
		public override bool IsEmpty
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060012C7 RID: 4807 RVA: 0x0003B31B File Offset: 0x0003951B
		public PdbHeapBuffer()
			: base(0)
		{
		}
	}
}
