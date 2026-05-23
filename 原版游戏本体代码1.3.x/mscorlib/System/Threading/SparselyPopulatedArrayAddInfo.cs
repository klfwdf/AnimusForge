using System;

namespace System.Threading
{
	// Token: 0x02000549 RID: 1353
	internal struct SparselyPopulatedArrayAddInfo<T> where T : class
	{
		// Token: 0x06003F70 RID: 16240 RVA: 0x000EC3AE File Offset: 0x000EA5AE
		internal SparselyPopulatedArrayAddInfo(SparselyPopulatedArrayFragment<T> source, int index)
		{
			this.m_source = source;
			this.m_index = index;
		}

		// Token: 0x17000962 RID: 2402
		// (get) Token: 0x06003F71 RID: 16241 RVA: 0x000EC3BE File Offset: 0x000EA5BE
		internal SparselyPopulatedArrayFragment<T> Source
		{
			get
			{
				return this.m_source;
			}
		}

		// Token: 0x17000963 RID: 2403
		// (get) Token: 0x06003F72 RID: 16242 RVA: 0x000EC3C6 File Offset: 0x000EA5C6
		internal int Index
		{
			get
			{
				return this.m_index;
			}
		}

		// Token: 0x04001AB7 RID: 6839
		private SparselyPopulatedArrayFragment<T> m_source;

		// Token: 0x04001AB8 RID: 6840
		private int m_index;
	}
}
