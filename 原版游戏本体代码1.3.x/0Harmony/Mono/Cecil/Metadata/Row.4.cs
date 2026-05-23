using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002DC RID: 732
	internal struct Row<T1, T2, T3, T4, T5>
	{
		// Token: 0x060012D0 RID: 4816 RVA: 0x0003B3DC File Offset: 0x000395DC
		public Row(T1 col1, T2 col2, T3 col3, T4 col4, T5 col5)
		{
			this.Col1 = col1;
			this.Col2 = col2;
			this.Col3 = col3;
			this.Col4 = col4;
			this.Col5 = col5;
		}

		// Token: 0x04000747 RID: 1863
		internal T1 Col1;

		// Token: 0x04000748 RID: 1864
		internal T2 Col2;

		// Token: 0x04000749 RID: 1865
		internal T3 Col3;

		// Token: 0x0400074A RID: 1866
		internal T4 Col4;

		// Token: 0x0400074B RID: 1867
		internal T5 Col5;
	}
}
