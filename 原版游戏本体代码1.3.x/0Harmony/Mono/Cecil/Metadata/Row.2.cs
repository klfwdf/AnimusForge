using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002DA RID: 730
	internal struct Row<T1, T2, T3>
	{
		// Token: 0x060012CE RID: 4814 RVA: 0x0003B3A6 File Offset: 0x000395A6
		public Row(T1 col1, T2 col2, T3 col3)
		{
			this.Col1 = col1;
			this.Col2 = col2;
			this.Col3 = col3;
		}

		// Token: 0x04000740 RID: 1856
		internal T1 Col1;

		// Token: 0x04000741 RID: 1857
		internal T2 Col2;

		// Token: 0x04000742 RID: 1858
		internal T3 Col3;
	}
}
