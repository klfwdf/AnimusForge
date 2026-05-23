using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002DB RID: 731
	internal struct Row<T1, T2, T3, T4>
	{
		// Token: 0x060012CF RID: 4815 RVA: 0x0003B3BD File Offset: 0x000395BD
		public Row(T1 col1, T2 col2, T3 col3, T4 col4)
		{
			this.Col1 = col1;
			this.Col2 = col2;
			this.Col3 = col3;
			this.Col4 = col4;
		}

		// Token: 0x04000743 RID: 1859
		internal T1 Col1;

		// Token: 0x04000744 RID: 1860
		internal T2 Col2;

		// Token: 0x04000745 RID: 1861
		internal T3 Col3;

		// Token: 0x04000746 RID: 1862
		internal T4 Col4;
	}
}
