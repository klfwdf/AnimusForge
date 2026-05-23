using System;

namespace Mono.Cecil
{
	// Token: 0x02000268 RID: 616
	internal struct Range
	{
		// Token: 0x06000DFD RID: 3581 RVA: 0x0002E150 File Offset: 0x0002C350
		public Range(uint index, uint length)
		{
			this.Start = index;
			this.Length = length;
		}

		// Token: 0x0400041D RID: 1053
		public uint Start;

		// Token: 0x0400041E RID: 1054
		public uint Length;
	}
}
