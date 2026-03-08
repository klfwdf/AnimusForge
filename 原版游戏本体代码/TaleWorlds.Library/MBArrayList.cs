using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.Library
{
	// Token: 0x02000067 RID: 103
	public class MBArrayList<T> : IMBCollection, ICollection, IEnumerable, IEnumerable<T>
	{
		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600032F RID: 815 RVA: 0x0000BC18 File Offset: 0x00009E18
		// (set) Token: 0x06000330 RID: 816 RVA: 0x0000BC20 File Offset: 0x00009E20
		public int Count { get; private set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000331 RID: 817 RVA: 0x0000BC29 File Offset: 0x00009E29
		public int Capacity
		{
			get
			{
				return this._data.Length;
			}
		}

		// Token: 0x06000332 RID: 818 RVA: 0x0000BC33 File Offset: 0x00009E33
		public MBArrayList()
		{
			this._data = new T[1];
			this.Count = 0;
		}

		// Token: 0x06000333 RID: 819 RVA: 0x0000BC4E File Offset: 0x00009E4E
		public MBArrayList(List<T> list)
		{
			this._data = list.ToArray();
			this.Count = this._data.Length;
		}

		// Token: 0x06000334 RID: 820 RVA: 0x0000BC70 File Offset: 0x00009E70
		public MBArrayList(IEnumerable<T> list)
		{
			this._data = list.ToArray<T>();
			this.Count = this._data.Length;
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000335 RID: 821 RVA: 0x0000BC92 File Offset: 0x00009E92
		public T[] RawArray
		{
			get
			{
				return this._data;
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000336 RID: 822 RVA: 0x0000BC9A File Offset: 0x00009E9A
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000337 RID: 823 RVA: 0x0000BC9D File Offset: 0x00009E9D
		public object SyncRoot
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000055 RID: 85
		public T this[int index]
		{
			get
			{
				return this._data[index];
			}
			set
			{
				this._data[index] = value;
			}
		}

		// Token: 0x0600033A RID: 826 RVA: 0x0000BCC0 File Offset: 0x00009EC0
		public int IndexOf(T item)
		{
			int result = -1;
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			for (int i = 0; i < this.Count; i++)
			{
				if (@default.Equals(this._data[i], item))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x0600033B RID: 827 RVA: 0x0000BD00 File Offset: 0x00009F00
		public bool Contains(T item)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			for (int i = 0; i < this.Count; i++)
			{
				if (@default.Equals(this._data[i], item))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600033C RID: 828 RVA: 0x0000BD3C File Offset: 0x00009F3C
		public IEnumerator<T> GetEnumerator()
		{
			int num;
			for (int i = 0; i < this.Count; i = num + 1)
			{
				yield return this._data[i];
				num = i;
			}
			yield break;
		}

		// Token: 0x0600033D RID: 829 RVA: 0x0000BD4B File Offset: 0x00009F4B
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0600033E RID: 830 RVA: 0x0000BD54 File Offset: 0x00009F54
		public void Clear()
		{
			for (int i = 0; i < this.Count; i++)
			{
				this._data[i] = default(T);
			}
			this.Count = 0;
		}

		// Token: 0x0600033F RID: 831 RVA: 0x0000BD90 File Offset: 0x00009F90
		private void EnsureCapacity(int newMinimumCapacity)
		{
			if (newMinimumCapacity > this.Capacity)
			{
				T[] array = new T[MathF.Max(this.Capacity * 2, newMinimumCapacity)];
				this.CopyTo(array, 0);
				this._data = array;
			}
		}

		// Token: 0x06000340 RID: 832 RVA: 0x0000BDCC File Offset: 0x00009FCC
		public void Add(T item)
		{
			this.EnsureCapacity(this.Count + 1);
			this._data[this.Count] = item;
			int count = this.Count;
			this.Count = count + 1;
		}

		// Token: 0x06000341 RID: 833 RVA: 0x0000BE0C File Offset: 0x0000A00C
		public void AddRange(IEnumerable<T> list)
		{
			foreach (T t in list)
			{
				this.EnsureCapacity(this.Count + 1);
				this._data[this.Count] = t;
				int count = this.Count;
				this.Count = count + 1;
			}
		}

		// Token: 0x06000342 RID: 834 RVA: 0x0000BE80 File Offset: 0x0000A080
		public bool Remove(T item)
		{
			int num = this.IndexOf(item);
			if (num >= 0)
			{
				for (int i = num; i < this.Count - 1; i++)
				{
					this._data[num] = this._data[num + 1];
				}
				int count = this.Count;
				this.Count = count - 1;
				this._data[this.Count] = default(T);
				return true;
			}
			return false;
		}

		// Token: 0x06000343 RID: 835 RVA: 0x0000BEF4 File Offset: 0x0000A0F4
		public void CopyTo(Array array, int index)
		{
			T[] array2;
			if ((array2 = array as T[]) != null)
			{
				for (int i = 0; i < this.Count; i++)
				{
					array2[i + index] = this._data[i];
				}
				return;
			}
			array.GetType().GetElementType();
			object[] array3 = array as object[];
			try
			{
				for (int j = 0; j < this.Count; j++)
				{
					array3[index++] = this._data[j];
				}
			}
			catch (ArrayTypeMismatchException)
			{
				Debug.FailedAssert("Invalid array type", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\MBArrayList.cs", "CopyTo", 210);
			}
		}

		// Token: 0x0400012D RID: 301
		private T[] _data;
	}
}
