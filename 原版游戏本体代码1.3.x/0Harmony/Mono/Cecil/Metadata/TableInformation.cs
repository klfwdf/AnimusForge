using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002E2 RID: 738
	internal struct TableInformation
	{
		// Token: 0x170004D8 RID: 1240
		// (get) Token: 0x060012DD RID: 4829 RVA: 0x0003B601 File Offset: 0x00039801
		public bool IsLarge
		{
			get
			{
				return this.Length > 65535U;
			}
		}

		// Token: 0x04000792 RID: 1938
		public uint Offset;

		// Token: 0x04000793 RID: 1939
		public uint Length;

		// Token: 0x04000794 RID: 1940
		public uint RowSize;
	}
}
