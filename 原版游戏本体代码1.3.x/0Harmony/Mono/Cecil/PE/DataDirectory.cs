using System;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C1 RID: 705
	internal struct DataDirectory
	{
		// Token: 0x170004CF RID: 1231
		// (get) Token: 0x06001227 RID: 4647 RVA: 0x0003804C File Offset: 0x0003624C
		public bool IsZero
		{
			get
			{
				return this.VirtualAddress == 0U && this.Size == 0U;
			}
		}

		// Token: 0x06001228 RID: 4648 RVA: 0x00038061 File Offset: 0x00036261
		public DataDirectory(uint rva, uint size)
		{
			this.VirtualAddress = rva;
			this.Size = size;
		}

		// Token: 0x040006A3 RID: 1699
		public readonly uint VirtualAddress;

		// Token: 0x040006A4 RID: 1700
		public readonly uint Size;
	}
}
