using System;

namespace System.Runtime.Serialization
{
	// Token: 0x02000751 RID: 1873
	internal class ObjectHolderListEnumerator
	{
		// Token: 0x060052C8 RID: 21192 RVA: 0x00122F56 File Offset: 0x00121156
		internal ObjectHolderListEnumerator(ObjectHolderList list, bool isFixupEnumerator)
		{
			this.m_list = list;
			this.m_startingVersion = this.m_list.Version;
			this.m_currPos = -1;
			this.m_isFixupEnumerator = isFixupEnumerator;
		}

		// Token: 0x060052C9 RID: 21193 RVA: 0x00122F84 File Offset: 0x00121184
		internal bool MoveNext()
		{
			if (this.m_isFixupEnumerator)
			{
				int num;
				do
				{
					num = this.m_currPos + 1;
					this.m_currPos = num;
				}
				while (num < this.m_list.Count && this.m_list.m_values[this.m_currPos].CompletelyFixed);
				return this.m_currPos != this.m_list.Count;
			}
			this.m_currPos++;
			return this.m_currPos != this.m_list.Count;
		}

		// Token: 0x17000DAF RID: 3503
		// (get) Token: 0x060052CA RID: 21194 RVA: 0x0012300B File Offset: 0x0012120B
		internal ObjectHolder Current
		{
			get
			{
				return this.m_list.m_values[this.m_currPos];
			}
		}

		// Token: 0x040024B1 RID: 9393
		private bool m_isFixupEnumerator;

		// Token: 0x040024B2 RID: 9394
		private ObjectHolderList m_list;

		// Token: 0x040024B3 RID: 9395
		private int m_startingVersion;

		// Token: 0x040024B4 RID: 9396
		private int m_currPos;
	}
}
