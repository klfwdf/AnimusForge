using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Token: 0x020001D4 RID: 468
[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<!0>, IReadOnlyCollection<!0>, IReadOnlyList<!0>, ICollection<!0>, IList<!0>
{
	// Token: 0x06000845 RID: 2117 RVA: 0x0001B679 File Offset: 0x00019879
	public <>z__ReadOnlyArray(T[] items)
	{
		this._items = items;
	}

	// Token: 0x06000846 RID: 2118 RVA: 0x0001B688 File Offset: 0x00019888
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this._items.GetEnumerator();
	}

	// Token: 0x170001FD RID: 509
	// (get) Token: 0x06000847 RID: 2119 RVA: 0x0001B695 File Offset: 0x00019895
	int ICollection.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	// Token: 0x170001FE RID: 510
	// (get) Token: 0x06000848 RID: 2120 RVA: 0x0001B69F File Offset: 0x0001989F
	bool ICollection.IsSynchronized
	{
		get
		{
			return false;
		}
	}

	// Token: 0x170001FF RID: 511
	// (get) Token: 0x06000849 RID: 2121 RVA: 0x0001B6A2 File Offset: 0x000198A2
	object ICollection.SyncRoot
	{
		get
		{
			return this;
		}
	}

	// Token: 0x0600084A RID: 2122 RVA: 0x0001B6A5 File Offset: 0x000198A5
	void ICollection.CopyTo(Array array, int index)
	{
		this._items.CopyTo(array, index);
	}

	// Token: 0x17000200 RID: 512
	object IList.this[int index]
	{
		get
		{
			return this._items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x17000201 RID: 513
	// (get) Token: 0x0600084D RID: 2125 RVA: 0x0001B6C7 File Offset: 0x000198C7
	bool IList.IsFixedSize
	{
		get
		{
			return true;
		}
	}

	// Token: 0x17000202 RID: 514
	// (get) Token: 0x0600084E RID: 2126 RVA: 0x0001B6C7 File Offset: 0x000198C7
	bool IList.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x0600084F RID: 2127 RVA: 0x00003BBE File Offset: 0x00001DBE
	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000850 RID: 2128 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000851 RID: 2129 RVA: 0x0001B6CA File Offset: 0x000198CA
	bool IList.Contains(object value)
	{
		return this._items.Contains(value);
	}

	// Token: 0x06000852 RID: 2130 RVA: 0x0001B6D8 File Offset: 0x000198D8
	int IList.IndexOf(object value)
	{
		return this._items.IndexOf(value);
	}

	// Token: 0x06000853 RID: 2131 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000854 RID: 2132 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000855 RID: 2133 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000856 RID: 2134 RVA: 0x0001B6E6 File Offset: 0x000198E6
	IEnumerator<T> IEnumerable<!0>.GetEnumerator()
	{
		return this._items.GetEnumerator();
	}

	// Token: 0x17000203 RID: 515
	// (get) Token: 0x06000857 RID: 2135 RVA: 0x0001B695 File Offset: 0x00019895
	int IReadOnlyCollection<!0>.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	// Token: 0x17000204 RID: 516
	T IReadOnlyList<!0>.this[int index]
	{
		get
		{
			return this._items[index];
		}
	}

	// Token: 0x17000205 RID: 517
	// (get) Token: 0x06000859 RID: 2137 RVA: 0x0001B695 File Offset: 0x00019895
	int ICollection<!0>.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	// Token: 0x17000206 RID: 518
	// (get) Token: 0x0600085A RID: 2138 RVA: 0x0001B6C7 File Offset: 0x000198C7
	bool ICollection<!0>.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x0600085B RID: 2139 RVA: 0x00003BBE File Offset: 0x00001DBE
	void ICollection<!0>.Add(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x0600085C RID: 2140 RVA: 0x00003BBE File Offset: 0x00001DBE
	void ICollection<!0>.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x0600085D RID: 2141 RVA: 0x0001B701 File Offset: 0x00019901
	bool ICollection<!0>.Contains(T item)
	{
		return this._items.Contains(item);
	}

	// Token: 0x0600085E RID: 2142 RVA: 0x0001B70F File Offset: 0x0001990F
	void ICollection<!0>.CopyTo(T[] array, int arrayIndex)
	{
		this._items.CopyTo(array, arrayIndex);
	}

	// Token: 0x0600085F RID: 2143 RVA: 0x00003BBE File Offset: 0x00001DBE
	bool ICollection<!0>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x17000207 RID: 519
	T IList<!0>.this[int index]
	{
		get
		{
			return this._items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x06000862 RID: 2146 RVA: 0x0001B71E File Offset: 0x0001991E
	int IList<!0>.IndexOf(T item)
	{
		return this._items.IndexOf(item);
	}

	// Token: 0x06000863 RID: 2147 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList<!0>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000864 RID: 2148 RVA: 0x00003BBE File Offset: 0x00001DBE
	void IList<!0>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x040002D9 RID: 729
	[CompilerGenerated]
	private readonly T[] _items;
}
