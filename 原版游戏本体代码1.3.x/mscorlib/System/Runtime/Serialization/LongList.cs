using System;

namespace System.Runtime.Serialization
{
	// Token: 0x0200074F RID: 1871
	[Serializable]
	internal class LongList
	{
		// Token: 0x060052B8 RID: 21176 RVA: 0x00122D02 File Offset: 0x00120F02
		internal LongList()
			: this(2)
		{
		}

		// Token: 0x060052B9 RID: 21177 RVA: 0x00122D0B File Offset: 0x00120F0B
		internal LongList(int startingSize)
		{
			this.m_count = 0;
			this.m_totalItems = 0;
			this.m_values = new long[startingSize];
		}

		// Token: 0x060052BA RID: 21178 RVA: 0x00122D30 File Offset: 0x00120F30
		internal void Add(long value)
		{
			if (this.m_totalItems == this.m_values.Length)
			{
				this.EnlargeArray();
			}
			long[] values = this.m_values;
			int totalItems = this.m_totalItems;
			this.m_totalItems = totalItems + 1;
			values[totalItems] = value;
			this.m_count++;
		}

		// Token: 0x17000DAB RID: 3499
		// (get) Token: 0x060052BB RID: 21179 RVA: 0x00122D7A File Offset: 0x00120F7A
		internal int Count
		{
			get
			{
				return this.m_count;
			}
		}

		// Token: 0x060052BC RID: 21180 RVA: 0x00122D82 File Offset: 0x00120F82
		internal void StartEnumeration()
		{
			this.m_currentItem = -1;
		}

		// Token: 0x060052BD RID: 21181 RVA: 0x00122D8C File Offset: 0x00120F8C
		internal bool MoveNext()
		{
			int num;
			do
			{
				num = this.m_currentItem + 1;
				this.m_currentItem = num;
			}
			while (num < this.m_totalItems && this.m_values[this.m_currentItem] == -1L);
			return this.m_currentItem != this.m_totalItems;
		}

		// Token: 0x17000DAC RID: 3500
		// (get) Token: 0x060052BE RID: 21182 RVA: 0x00122DD4 File Offset: 0x00120FD4
		internal long Current
		{
			get
			{
				return this.m_values[this.m_currentItem];
			}
		}

		// Token: 0x060052BF RID: 21183 RVA: 0x00122DE4 File Offset: 0x00120FE4
		internal bool RemoveElement(long value)
		{
			int num = 0;
			while (num < this.m_totalItems && this.m_values[num] != value)
			{
				num++;
			}
			if (num == this.m_totalItems)
			{
				return false;
			}
			this.m_values[num] = -1L;
			return true;
		}

		// Token: 0x060052C0 RID: 21184 RVA: 0x00122E24 File Offset: 0x00121024
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
			long[] array = new long[num];
			Array.Copy(this.m_values, array, this.m_count);
			this.m_values = array;
		}

		// Token: 0x040024A9 RID: 9385
		private const int InitialSize = 2;

		// Token: 0x040024AA RID: 9386
		private long[] m_values;

		// Token: 0x040024AB RID: 9387
		private int m_count;

		// Token: 0x040024AC RID: 9388
		private int m_totalItems;

		// Token: 0x040024AD RID: 9389
		private int m_currentItem;
	}
}
