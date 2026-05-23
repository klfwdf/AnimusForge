using System;
using System.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020001F8 RID: 504
	internal abstract class SortedTable<TRow> : MetadataTable<TRow>, IComparer<TRow> where TRow : struct
	{
		// Token: 0x06000AB3 RID: 2739 RVA: 0x00024AD4 File Offset: 0x00022CD4
		public sealed override void Sort()
		{
			MergeSort<TRow>.Sort(this.rows, 0, this.length, this);
		}

		// Token: 0x06000AB4 RID: 2740 RVA: 0x00024AE9 File Offset: 0x00022CE9
		protected static int Compare(uint x, uint y)
		{
			if (x == y)
			{
				return 0;
			}
			if (x <= y)
			{
				return -1;
			}
			return 1;
		}

		// Token: 0x06000AB5 RID: 2741
		public abstract int Compare(TRow x, TRow y);
	}
}
