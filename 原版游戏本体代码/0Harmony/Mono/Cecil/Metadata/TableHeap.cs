using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002E3 RID: 739
	internal sealed class TableHeap : Heap
	{
		// Token: 0x170004D9 RID: 1241
		public TableInformation this[Table table]
		{
			get
			{
				return this.Tables[(int)table];
			}
		}

		// Token: 0x060012DF RID: 4831 RVA: 0x0003B61E File Offset: 0x0003981E
		public TableHeap(byte[] data)
			: base(data)
		{
		}

		// Token: 0x060012E0 RID: 4832 RVA: 0x0003B634 File Offset: 0x00039834
		public bool HasTable(Table table)
		{
			return (this.Valid & (1L << (int)table)) != 0L;
		}

		// Token: 0x04000795 RID: 1941
		public long Valid;

		// Token: 0x04000796 RID: 1942
		public long Sorted;

		// Token: 0x04000797 RID: 1943
		public readonly TableInformation[] Tables = new TableInformation[58];
	}
}
