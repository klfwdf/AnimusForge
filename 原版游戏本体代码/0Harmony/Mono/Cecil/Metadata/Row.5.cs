using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002DD RID: 733
	internal struct Row<T1, T2, T3, T4, T5, T6>
	{
		// Token: 0x060012D1 RID: 4817 RVA: 0x0003B403 File Offset: 0x00039603
		public Row(T1 col1, T2 col2, T3 col3, T4 col4, T5 col5, T6 col6)
		{
			this.Col1 = col1;
			this.Col2 = col2;
			this.Col3 = col3;
			this.Col4 = col4;
			this.Col5 = col5;
			this.Col6 = col6;
		}

		// Token: 0x0400074C RID: 1868
		internal T1 Col1;

		// Token: 0x0400074D RID: 1869
		internal T2 Col2;

		// Token: 0x0400074E RID: 1870
		internal T3 Col3;

		// Token: 0x0400074F RID: 1871
		internal T4 Col4;

		// Token: 0x04000750 RID: 1872
		internal T5 Col5;

		// Token: 0x04000751 RID: 1873
		internal T6 Col6;
	}
}
