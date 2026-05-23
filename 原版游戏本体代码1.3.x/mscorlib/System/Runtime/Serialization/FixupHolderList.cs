using System;

namespace System.Runtime.Serialization
{
	// Token: 0x0200074E RID: 1870
	[Serializable]
	internal class FixupHolderList
	{
		// Token: 0x060052B3 RID: 21171 RVA: 0x00122BF3 File Offset: 0x00120DF3
		internal FixupHolderList()
			: this(2)
		{
		}

		// Token: 0x060052B4 RID: 21172 RVA: 0x00122BFC File Offset: 0x00120DFC
		internal FixupHolderList(int startingSize)
		{
			this.m_count = 0;
			this.m_values = new FixupHolder[startingSize];
		}

		// Token: 0x060052B5 RID: 21173 RVA: 0x00122C18 File Offset: 0x00120E18
		internal virtual void Add(long id, object fixupInfo)
		{
			if (this.m_count == this.m_values.Length)
			{
				this.EnlargeArray();
			}
			this.m_values[this.m_count].m_id = id;
			FixupHolder[] values = this.m_values;
			int count = this.m_count;
			this.m_count = count + 1;
			values[count].m_fixupInfo = fixupInfo;
		}

		// Token: 0x060052B6 RID: 21174 RVA: 0x00122C6C File Offset: 0x00120E6C
		internal virtual void Add(FixupHolder fixup)
		{
			if (this.m_count == this.m_values.Length)
			{
				this.EnlargeArray();
			}
			FixupHolder[] values = this.m_values;
			int count = this.m_count;
			this.m_count = count + 1;
			values[count] = fixup;
		}

		// Token: 0x060052B7 RID: 21175 RVA: 0x00122CA8 File Offset: 0x00120EA8
		private void EnlargeArray()
		{
			int num = this.m_values.Length * 2;
			if (num < 0)
			{
				if (num == 2147483647)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_TooManyElements"));
				}
				num = int.MaxValue;
			}
			FixupHolder[] array = new FixupHolder[num];
			Array.Copy(this.m_values, array, this.m_count);
			this.m_values = array;
		}

		// Token: 0x040024A6 RID: 9382
		internal const int InitialSize = 2;

		// Token: 0x040024A7 RID: 9383
		internal FixupHolder[] m_values;

		// Token: 0x040024A8 RID: 9384
		internal int m_count;
	}
}
