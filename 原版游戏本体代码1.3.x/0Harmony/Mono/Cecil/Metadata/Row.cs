using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D9 RID: 729
	internal struct Row<T1, T2>
	{
		// Token: 0x060012CD RID: 4813 RVA: 0x0003B396 File Offset: 0x00039596
		public Row(T1 col1, T2 col2)
		{
			this.Col1 = col1;
			this.Col2 = col2;
		}

		// Token: 0x0400073E RID: 1854
		internal T1 Col1;

		// Token: 0x0400073F RID: 1855
		internal T2 Col2;
	}
}
