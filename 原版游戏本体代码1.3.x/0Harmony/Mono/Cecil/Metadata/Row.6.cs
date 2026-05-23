using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002DE RID: 734
	internal struct Row<T1, T2, T3, T4, T5, T6, T7, T8, T9>
	{
		// Token: 0x060012D2 RID: 4818 RVA: 0x0003B434 File Offset: 0x00039634
		public Row(T1 col1, T2 col2, T3 col3, T4 col4, T5 col5, T6 col6, T7 col7, T8 col8, T9 col9)
		{
			this.Col1 = col1;
			this.Col2 = col2;
			this.Col3 = col3;
			this.Col4 = col4;
			this.Col5 = col5;
			this.Col6 = col6;
			this.Col7 = col7;
			this.Col8 = col8;
			this.Col9 = col9;
		}

		// Token: 0x04000752 RID: 1874
		internal T1 Col1;

		// Token: 0x04000753 RID: 1875
		internal T2 Col2;

		// Token: 0x04000754 RID: 1876
		internal T3 Col3;

		// Token: 0x04000755 RID: 1877
		internal T4 Col4;

		// Token: 0x04000756 RID: 1878
		internal T5 Col5;

		// Token: 0x04000757 RID: 1879
		internal T6 Col6;

		// Token: 0x04000758 RID: 1880
		internal T7 Col7;

		// Token: 0x04000759 RID: 1881
		internal T8 Col8;

		// Token: 0x0400075A RID: 1882
		internal T9 Col9;
	}
}
