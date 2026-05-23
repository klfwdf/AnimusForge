using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000066 RID: 102
	public class MBList2D<T> : IMBCollection
	{
		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000322 RID: 802 RVA: 0x0000BA62 File Offset: 0x00009C62
		// (set) Token: 0x06000323 RID: 803 RVA: 0x0000BA6A File Offset: 0x00009C6A
		public int Count1 { get; private set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000324 RID: 804 RVA: 0x0000BA73 File Offset: 0x00009C73
		// (set) Token: 0x06000325 RID: 805 RVA: 0x0000BA7B File Offset: 0x00009C7B
		public int Count2 { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000326 RID: 806 RVA: 0x0000BA84 File Offset: 0x00009C84
		private int Capacity
		{
			get
			{
				return this._data.Length;
			}
		}

		// Token: 0x06000327 RID: 807 RVA: 0x0000BA8E File Offset: 0x00009C8E
		public MBList2D(int count1, int count2)
		{
			this._data = new T[count1 * count2];
			this.Count1 = count1;
			this.Count2 = count2;
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000328 RID: 808 RVA: 0x0000BAB2 File Offset: 0x00009CB2
		public T[] RawArray
		{
			get
			{
				return this._data;
			}
		}

		// Token: 0x1700004F RID: 79
		public T this[int index1, int index2]
		{
			get
			{
				return this._data[index1 * this.Count2 + index2];
			}
			set
			{
				this._data[index1 * this.Count2 + index2] = value;
			}
		}

		// Token: 0x0600032B RID: 811 RVA: 0x0000BAEC File Offset: 0x00009CEC
		public bool Contains(T item)
		{
			for (int i = 0; i < this.Count1; i++)
			{
				for (int j = 0; j < this.Count2; j++)
				{
					if (this._data[i * this.Count2 + j].Equals(item))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600032C RID: 812 RVA: 0x0000BB48 File Offset: 0x00009D48
		public void Clear()
		{
			for (int i = 0; i < this.Count1 * this.Count2; i++)
			{
				this._data[i] = default(T);
			}
		}

		// Token: 0x0600032D RID: 813 RVA: 0x0000BB84 File Offset: 0x00009D84
		public void ResetWithNewCount(int newCount1, int newCount2)
		{
			if (this.Count1 == newCount1 && this.Count2 == newCount2)
			{
				this.Clear();
				return;
			}
			this.Count1 = newCount1;
			this.Count2 = newCount2;
			if (this.Capacity < newCount1 * newCount2)
			{
				this._data = new T[MathF.Max(this.Capacity * 2, newCount1 * newCount2)];
				return;
			}
			this.Clear();
		}

		// Token: 0x0600032E RID: 814 RVA: 0x0000BBE8 File Offset: 0x00009DE8
		public void CopyRowTo(int sourceIndex1, int sourceIndex2, MBList2D<T> destination, int destinationIndex1, int destinationIndex2, int copyCount)
		{
			for (int i = 0; i < copyCount; i++)
			{
				destination[destinationIndex1, destinationIndex2 + i] = this[sourceIndex1, sourceIndex2 + i];
			}
		}

		// Token: 0x0400012A RID: 298
		private T[] _data;
	}
}
