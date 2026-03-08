using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D8 RID: 728
	internal sealed class PdbHeap : Heap
	{
		// Token: 0x060012CB RID: 4811 RVA: 0x0003AA03 File Offset: 0x00038C03
		public PdbHeap(byte[] data)
			: base(data)
		{
		}

		// Token: 0x060012CC RID: 4812 RVA: 0x0003B382 File Offset: 0x00039582
		public bool HasTable(Table table)
		{
			return (this.TypeSystemTables & (1L << (int)table)) != 0L;
		}

		// Token: 0x0400073A RID: 1850
		public byte[] Id;

		// Token: 0x0400073B RID: 1851
		public uint EntryPoint;

		// Token: 0x0400073C RID: 1852
		public long TypeSystemTables;

		// Token: 0x0400073D RID: 1853
		public uint[] TypeSystemTableRows;
	}
}
