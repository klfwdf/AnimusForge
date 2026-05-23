using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Token: 0x020001D5 RID: 469
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	// Token: 0x06000865 RID: 2149 RVA: 0x0001B72C File Offset: 0x0001992C
	public <>z__ReadOnlySingleElementList(T item)
	{
		this._item = item;
	}

	// Token: 0x06000866 RID: 2150 RVA: 0x0001B73B File Offset: 0x0001993B
	IEnumerator IEnumerable.GetEnumerator()
	{
		return new <>z__ReadOnlySingleElementList<T>.Enumerator(this._item);
	}

	// Token: 0x17000208 RID: 520
	// (get) Token: 0x06000867 RID: 2151 RVA: 0x0001B6C7 File Offset: 0x000198C7
	int ICollection.Count
	{
		get
		{
			return 1;
		}
	}

	// Token: 0x17000209 RID: 521
	// (get) Token: 0x06000868 RID: 2152 RVA: 0x0001B69F File Offset: 0x0001989F
	bool ICollection.IsSynchronized
	{
		get
		{
			return false;
		}
	}

	// Token: 0x1700020A RID: 522
	// (get) Token: 0x06000869 RID: 2153 RVA: 0x0001B6A2 File Offset: 0x000198A2
	object ICollection.SyncRoot
	{
		get
		{
			return this;
		}
	}

	// Token: 0x0600086A RID: 2154 RVA: 0x0001B748 File Offset: 0x00019948
	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(this._item, index);
	}

	// Token: 0x1700020B RID: 523
	object IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return this._item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x1700020C RID: 524
	// (get) Token: 0x0600086D RID: 2157 RVA: 0x0001B6C7 File Offset: 0x000198C7
	bool IList.IsFixedSize
	{
		get
		{
			return true;
		}
	}

	// Token: 0x1700020D RID: 525
	// (get) Token: 0x0600086E RID: 2158 RVA: 0x0001B6C7 File Offset: 0x000198C7
	bool IList.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x0600086F RID: 2159 RVA: 0x00003BBE File Offset: 0x00001DBE
	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000870 RID: 2160 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000871 RID: 2161 RVA: 0x0001B772 File Offset: 0x00019972
	bool IList.Contains(object value)
	{
		return EqualityComparer<T>.Default.Equals(this._item, (T)((object)value));
	}

	// Token: 0x06000872 RID: 2162 RVA: 0x0001B78A File Offset: 0x0001998A
	int IList.IndexOf(object value)
	{
		if (!EqualityComparer<T>.Default.Equals(this._item, (T)((object)value)))
		{
			return -1;
		}
		return 0;
	}

	// Token: 0x06000873 RID: 2163 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000874 RID: 2164 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000875 RID: 2165 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000876 RID: 2166 RVA: 0x0001B73B File Offset: 0x0001993B
	IEnumerator<T> IEnumerable<!0>.GetEnumerator()
	{
		return new <>z__ReadOnlySingleElementList<T>.Enumerator(this._item);
	}

	// Token: 0x1700020E RID: 526
	// (get) Token: 0x06000877 RID: 2167 RVA: 0x0001B6C7 File Offset: 0x000198C7
	int IReadOnlyCollection<!0>.Count
	{
		get
		{
			return 1;
		}
	}

	// Token: 0x1700020F RID: 527
	T IReadOnlyList<!0>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return this._item;
		}
	}

	// Token: 0x17000210 RID: 528
	// (get) Token: 0x06000879 RID: 2169 RVA: 0x0001B6C7 File Offset: 0x000198C7
	int ICollection<!0>.Count
	{
		get
		{
			return 1;
		}
	}

	// Token: 0x17000211 RID: 529
	// (get) Token: 0x0600087A RID: 2170 RVA: 0x0001B6C7 File Offset: 0x000198C7
	bool ICollection<!0>.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x0600087B RID: 2171 RVA: 0x00003BBE File Offset: 0x00001DBE
	void ICollection<!0>.Add(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x0600087C RID: 2172 RVA: 0x00003BBE File Offset: 0x00001DBE
	void ICollection<!0>.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x0600087D RID: 2173 RVA: 0x0001B7B8 File Offset: 0x000199B8
	bool ICollection<!0>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(this._item, item);
	}

	// Token: 0x0600087E RID: 2174 RVA: 0x0001B7CB File Offset: 0x000199CB
	void ICollection<!0>.CopyTo(T[] array, int arrayIndex)
	{
		array[arrayIndex] = this._item;
	}

	// Token: 0x0600087F RID: 2175 RVA: 0x00003BBE File Offset: 0x00001DBE
	bool ICollection<!0>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x17000212 RID: 530
	T IList<!0>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return this._item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x06000882 RID: 2178 RVA: 0x0001B7DA File Offset: 0x000199DA
	int IList<!0>.IndexOf(T item)
	{
		if (!EqualityComparer<T>.Default.Equals(this._item, item))
		{
			return -1;
		}
		return 0;
	}

	// Token: 0x06000883 RID: 2179 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList<!0>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000884 RID: 2180 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList<!0>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x040002DA RID: 730
	[CompilerGenerated]
	private readonly T _item;

	// Token: 0x020001D6 RID: 470
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		// Token: 0x06000885 RID: 2181 RVA: 0x0001B7F2 File Offset: 0x000199F2
		public Enumerator(T item)
		{
			this.System.Collections.Generic.IEnumerator<T>.Current = item;
		}

		// Token: 0x17000213 RID: 531
		// (get) Token: 0x06000886 RID: 2182 RVA: 0x0001B801 File Offset: 0x00019A01
		object IEnumerator.Current
		{
			get
			{
				return this._item;
			}
		}

		// Token: 0x17000214 RID: 532
		// (get) Token: 0x06000887 RID: 2183 RVA: 0x0001B80E File Offset: 0x00019A0E
		T IEnumerator<!0>.Current
		{
			get
			{
				return this._item;
			}
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x0001B818 File Offset: 0x00019A18
		bool IEnumerator.MoveNext()
		{
			return !this._moveNextCalled && (this._moveNextCalled = true);
		}

		// Token: 0x06000889 RID: 2185 RVA: 0x0001B839 File Offset: 0x00019A39
		void IEnumerator.Reset()
		{
			this._moveNextCalled = false;
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x0001B842 File Offset: 0x00019A42
		void IDisposable.Dispose()
		{
		}

		// Token: 0x040002DB RID: 731
		[CompilerGenerated]
		private readonly T _item;

		// Token: 0x040002DC RID: 732
		[CompilerGenerated]
		private bool _moveNextCalled;
	}
}
