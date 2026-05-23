using System;

namespace System.Runtime.Serialization
{
	// Token: 0x02000750 RID: 1872
	internal class ObjectHolderList
	{
		// Token: 0x060052C1 RID: 21185 RVA: 0x00122E7E File Offset: 0x0012107E
		internal ObjectHolderList()
			: this(8)
		{
		}

		// Token: 0x060052C2 RID: 21186 RVA: 0x00122E87 File Offset: 0x00121087
		internal ObjectHolderList(int startingSize)
		{
			this.m_count = 0;
			this.m_values = new ObjectHolder[startingSize];
		}

		// Token: 0x060052C3 RID: 21187 RVA: 0x00122EA4 File Offset: 0x001210A4
		internal virtual void Add(ObjectHolder value)
		{
			if (this.m_count == this.m_values.Length)
			{
				this.EnlargeArray();
			}
			ObjectHolder[] values = this.m_values;
			int count = this.m_count;
			this.m_count = count + 1;
			values[count] = value;
		}

		// Token: 0x060052C4 RID: 21188 RVA: 0x00122EE0 File Offset: 0x001210E0
		internal ObjectHolderListEnumerator GetFixupEnumerator()
		{
			return new ObjectHolderListEnumerator(this, true);
		}

		// Token: 0x060052C5 RID: 21189 RVA: 0x00122EEC File Offset: 0x001210EC
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
			ObjectHolder[] array = new ObjectHolder[num];
			Array.Copy(this.m_values, array, this.m_count);
			this.m_values = array;
		}

		// Token: 0x17000DAD RID: 3501
		// (get) Token: 0x060052C6 RID: 21190 RVA: 0x00122F46 File Offset: 0x00121146
		internal int Version
		{
			get
			{
				return this.m_count;
			}
		}

		// Token: 0x17000DAE RID: 3502
		// (get) Token: 0x060052C7 RID: 21191 RVA: 0x00122F4E File Offset: 0x0012114E
		internal int Count
		{
			get
			{
				return this.m_count;
			}
		}

		// Token: 0x040024AE RID: 9390
		internal const int DefaultInitialSize = 8;

		// Token: 0x040024AF RID: 9391
		internal ObjectHolder[] m_values;

		// Token: 0x040024B0 RID: 9392
		internal int m_count;
	}
}
