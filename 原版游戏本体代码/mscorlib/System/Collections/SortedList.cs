using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections
{
	// Token: 0x020004A4 RID: 1188
	[DebuggerTypeProxy(typeof(SortedList.SortedListDebugView))]
	[DebuggerDisplay("Count = {Count}")]
	[ComVisible(true)]
	[Serializable]
	public class SortedList : IDictionary, ICollection, IEnumerable, ICloneable
	{
		// Token: 0x060038D0 RID: 14544 RVA: 0x000D9D4B File Offset: 0x000D7F4B
		public SortedList()
		{
			this.Init();
		}

		// Token: 0x060038D1 RID: 14545 RVA: 0x000D9D59 File Offset: 0x000D7F59
		private void Init()
		{
			this.keys = SortedList.emptyArray;
			this.values = SortedList.emptyArray;
			this._size = 0;
			this.comparer = new Comparer(CultureInfo.CurrentCulture);
		}

		// Token: 0x060038D2 RID: 14546 RVA: 0x000D9D88 File Offset: 0x000D7F88
		public SortedList(int initialCapacity)
		{
			if (initialCapacity < 0)
			{
				throw new ArgumentOutOfRangeException("initialCapacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			this.keys = new object[initialCapacity];
			this.values = new object[initialCapacity];
			this.comparer = new Comparer(CultureInfo.CurrentCulture);
		}

		// Token: 0x060038D3 RID: 14547 RVA: 0x000D9DDC File Offset: 0x000D7FDC
		public SortedList(IComparer comparer)
			: this()
		{
			if (comparer != null)
			{
				this.comparer = comparer;
			}
		}

		// Token: 0x060038D4 RID: 14548 RVA: 0x000D9DEE File Offset: 0x000D7FEE
		public SortedList(IComparer comparer, int capacity)
			: this(comparer)
		{
			this.Capacity = capacity;
		}

		// Token: 0x060038D5 RID: 14549 RVA: 0x000D9DFE File Offset: 0x000D7FFE
		public SortedList(IDictionary d)
			: this(d, null)
		{
		}

		// Token: 0x060038D6 RID: 14550 RVA: 0x000D9E08 File Offset: 0x000D8008
		public SortedList(IDictionary d, IComparer comparer)
			: this(comparer, (d != null) ? d.Count : 0)
		{
			if (d == null)
			{
				throw new ArgumentNullException("d", Environment.GetResourceString("ArgumentNull_Dictionary"));
			}
			d.Keys.CopyTo(this.keys, 0);
			d.Values.CopyTo(this.values, 0);
			Array.Sort(this.keys, this.values, comparer);
			this._size = d.Count;
		}

		// Token: 0x060038D7 RID: 14551 RVA: 0x000D9E84 File Offset: 0x000D8084
		public virtual void Add(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			int num = Array.BinarySearch(this.keys, 0, this._size, key, this.comparer);
			if (num >= 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate__", new object[]
				{
					this.GetKey(num),
					key
				}));
			}
			this.Insert(~num, key, value);
		}

		// Token: 0x1700087E RID: 2174
		// (get) Token: 0x060038D8 RID: 14552 RVA: 0x000D9EF5 File Offset: 0x000D80F5
		// (set) Token: 0x060038D9 RID: 14553 RVA: 0x000D9F00 File Offset: 0x000D8100
		public virtual int Capacity
		{
			get
			{
				return this.keys.Length;
			}
			set
			{
				if (value < this.Count)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
				}
				if (value != this.keys.Length)
				{
					if (value > 0)
					{
						object[] destinationArray = new object[value];
						object[] destinationArray2 = new object[value];
						if (this._size > 0)
						{
							Array.Copy(this.keys, 0, destinationArray, 0, this._size);
							Array.Copy(this.values, 0, destinationArray2, 0, this._size);
						}
						this.keys = destinationArray;
						this.values = destinationArray2;
						return;
					}
					this.keys = SortedList.emptyArray;
					this.values = SortedList.emptyArray;
				}
			}
		}

		// Token: 0x1700087F RID: 2175
		// (get) Token: 0x060038DA RID: 14554 RVA: 0x000D9F9E File Offset: 0x000D819E
		public virtual int Count
		{
			get
			{
				return this._size;
			}
		}

		// Token: 0x17000880 RID: 2176
		// (get) Token: 0x060038DB RID: 14555 RVA: 0x000D9FA6 File Offset: 0x000D81A6
		public virtual ICollection Keys
		{
			get
			{
				return this.GetKeyList();
			}
		}

		// Token: 0x17000881 RID: 2177
		// (get) Token: 0x060038DC RID: 14556 RVA: 0x000D9FAE File Offset: 0x000D81AE
		public virtual ICollection Values
		{
			get
			{
				return this.GetValueList();
			}
		}

		// Token: 0x17000882 RID: 2178
		// (get) Token: 0x060038DD RID: 14557 RVA: 0x000D9FB6 File Offset: 0x000D81B6
		public virtual bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000883 RID: 2179
		// (get) Token: 0x060038DE RID: 14558 RVA: 0x000D9FB9 File Offset: 0x000D81B9
		public virtual bool IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000884 RID: 2180
		// (get) Token: 0x060038DF RID: 14559 RVA: 0x000D9FBC File Offset: 0x000D81BC
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000885 RID: 2181
		// (get) Token: 0x060038E0 RID: 14560 RVA: 0x000D9FBF File Offset: 0x000D81BF
		public virtual object SyncRoot
		{
			get
			{
				if (this._syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
				}
				return this._syncRoot;
			}
		}

		// Token: 0x060038E1 RID: 14561 RVA: 0x000D9FE1 File Offset: 0x000D81E1
		public virtual void Clear()
		{
			this.version++;
			Array.Clear(this.keys, 0, this._size);
			Array.Clear(this.values, 0, this._size);
			this._size = 0;
		}

		// Token: 0x060038E2 RID: 14562 RVA: 0x000DA01C File Offset: 0x000D821C
		public virtual object Clone()
		{
			SortedList sortedList = new SortedList(this._size);
			Array.Copy(this.keys, 0, sortedList.keys, 0, this._size);
			Array.Copy(this.values, 0, sortedList.values, 0, this._size);
			sortedList._size = this._size;
			sortedList.version = this.version;
			sortedList.comparer = this.comparer;
			return sortedList;
		}

		// Token: 0x060038E3 RID: 14563 RVA: 0x000DA08C File Offset: 0x000D828C
		public virtual bool Contains(object key)
		{
			return this.IndexOfKey(key) >= 0;
		}

		// Token: 0x060038E4 RID: 14564 RVA: 0x000DA09B File Offset: 0x000D829B
		public virtual bool ContainsKey(object key)
		{
			return this.IndexOfKey(key) >= 0;
		}

		// Token: 0x060038E5 RID: 14565 RVA: 0x000DA0AA File Offset: 0x000D82AA
		public virtual bool ContainsValue(object value)
		{
			return this.IndexOfValue(value) >= 0;
		}

		// Token: 0x060038E6 RID: 14566 RVA: 0x000DA0BC File Offset: 0x000D82BC
		public virtual void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - arrayIndex < this.Count)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
			}
			for (int i = 0; i < this.Count; i++)
			{
				DictionaryEntry dictionaryEntry = new DictionaryEntry(this.keys[i], this.values[i]);
				array.SetValue(dictionaryEntry, i + arrayIndex);
			}
		}

		// Token: 0x060038E7 RID: 14567 RVA: 0x000DA16C File Offset: 0x000D836C
		internal virtual KeyValuePairs[] ToKeyValuePairsArray()
		{
			KeyValuePairs[] array = new KeyValuePairs[this.Count];
			for (int i = 0; i < this.Count; i++)
			{
				array[i] = new KeyValuePairs(this.keys[i], this.values[i]);
			}
			return array;
		}

		// Token: 0x060038E8 RID: 14568 RVA: 0x000DA1B0 File Offset: 0x000D83B0
		private void EnsureCapacity(int min)
		{
			int num = ((this.keys.Length == 0) ? 16 : (this.keys.Length * 2));
			if (num > 2146435071)
			{
				num = 2146435071;
			}
			if (num < min)
			{
				num = min;
			}
			this.Capacity = num;
		}

		// Token: 0x060038E9 RID: 14569 RVA: 0x000DA1F0 File Offset: 0x000D83F0
		public virtual object GetByIndex(int index)
		{
			if (index < 0 || index >= this.Count)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return this.values[index];
		}

		// Token: 0x060038EA RID: 14570 RVA: 0x000DA21C File Offset: 0x000D841C
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new SortedList.SortedListEnumerator(this, 0, this._size, 3);
		}

		// Token: 0x060038EB RID: 14571 RVA: 0x000DA22C File Offset: 0x000D842C
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return new SortedList.SortedListEnumerator(this, 0, this._size, 3);
		}

		// Token: 0x060038EC RID: 14572 RVA: 0x000DA23C File Offset: 0x000D843C
		public virtual object GetKey(int index)
		{
			if (index < 0 || index >= this.Count)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return this.keys[index];
		}

		// Token: 0x060038ED RID: 14573 RVA: 0x000DA268 File Offset: 0x000D8468
		public virtual IList GetKeyList()
		{
			if (this.keyList == null)
			{
				this.keyList = new SortedList.KeyList(this);
			}
			return this.keyList;
		}

		// Token: 0x060038EE RID: 14574 RVA: 0x000DA284 File Offset: 0x000D8484
		public virtual IList GetValueList()
		{
			if (this.valueList == null)
			{
				this.valueList = new SortedList.ValueList(this);
			}
			return this.valueList;
		}

		// Token: 0x17000886 RID: 2182
		public virtual object this[object key]
		{
			get
			{
				int num = this.IndexOfKey(key);
				if (num >= 0)
				{
					return this.values[num];
				}
				return null;
			}
			set
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				int num = Array.BinarySearch(this.keys, 0, this._size, key, this.comparer);
				if (num >= 0)
				{
					this.values[num] = value;
					this.version++;
					return;
				}
				this.Insert(~num, key, value);
			}
		}

		// Token: 0x060038F1 RID: 14577 RVA: 0x000DA32C File Offset: 0x000D852C
		public virtual int IndexOfKey(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			int num = Array.BinarySearch(this.keys, 0, this._size, key, this.comparer);
			if (num < 0)
			{
				return -1;
			}
			return num;
		}

		// Token: 0x060038F2 RID: 14578 RVA: 0x000DA372 File Offset: 0x000D8572
		public virtual int IndexOfValue(object value)
		{
			return Array.IndexOf<object>(this.values, value, 0, this._size);
		}

		// Token: 0x060038F3 RID: 14579 RVA: 0x000DA388 File Offset: 0x000D8588
		private void Insert(int index, object key, object value)
		{
			if (this._size == this.keys.Length)
			{
				this.EnsureCapacity(this._size + 1);
			}
			if (index < this._size)
			{
				Array.Copy(this.keys, index, this.keys, index + 1, this._size - index);
				Array.Copy(this.values, index, this.values, index + 1, this._size - index);
			}
			this.keys[index] = key;
			this.values[index] = value;
			this._size++;
			this.version++;
		}

		// Token: 0x060038F4 RID: 14580 RVA: 0x000DA424 File Offset: 0x000D8624
		public virtual void RemoveAt(int index)
		{
			if (index < 0 || index >= this.Count)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			this._size--;
			if (index < this._size)
			{
				Array.Copy(this.keys, index + 1, this.keys, index, this._size - index);
				Array.Copy(this.values, index + 1, this.values, index, this._size - index);
			}
			this.keys[this._size] = null;
			this.values[this._size] = null;
			this.version++;
		}

		// Token: 0x060038F5 RID: 14581 RVA: 0x000DA4D0 File Offset: 0x000D86D0
		public virtual void Remove(object key)
		{
			int num = this.IndexOfKey(key);
			if (num >= 0)
			{
				this.RemoveAt(num);
			}
		}

		// Token: 0x060038F6 RID: 14582 RVA: 0x000DA4F0 File Offset: 0x000D86F0
		public virtual void SetByIndex(int index, object value)
		{
			if (index < 0 || index >= this.Count)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			this.values[index] = value;
			this.version++;
		}

		// Token: 0x060038F7 RID: 14583 RVA: 0x000DA52B File Offset: 0x000D872B
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static SortedList Synchronized(SortedList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new SortedList.SyncSortedList(list);
		}

		// Token: 0x060038F8 RID: 14584 RVA: 0x000DA541 File Offset: 0x000D8741
		public virtual void TrimToSize()
		{
			this.Capacity = this._size;
		}

		// Token: 0x04001905 RID: 6405
		private object[] keys;

		// Token: 0x04001906 RID: 6406
		private object[] values;

		// Token: 0x04001907 RID: 6407
		private int _size;

		// Token: 0x04001908 RID: 6408
		private int version;

		// Token: 0x04001909 RID: 6409
		private IComparer comparer;

		// Token: 0x0400190A RID: 6410
		private SortedList.KeyList keyList;

		// Token: 0x0400190B RID: 6411
		private SortedList.ValueList valueList;

		// Token: 0x0400190C RID: 6412
		[NonSerialized]
		private object _syncRoot;

		// Token: 0x0400190D RID: 6413
		private const int _defaultCapacity = 16;

		// Token: 0x0400190E RID: 6414
		private static object[] emptyArray = EmptyArray<object>.Value;

		// Token: 0x02000BBD RID: 3005
		[Serializable]
		private class SyncSortedList : SortedList
		{
			// Token: 0x06006E23 RID: 28195 RVA: 0x0017C500 File Offset: 0x0017A700
			internal SyncSortedList(SortedList list)
			{
				this._list = list;
				this._root = list.SyncRoot;
			}

			// Token: 0x170012C3 RID: 4803
			// (get) Token: 0x06006E24 RID: 28196 RVA: 0x0017C51C File Offset: 0x0017A71C
			public override int Count
			{
				get
				{
					object root = this._root;
					int count;
					lock (root)
					{
						count = this._list.Count;
					}
					return count;
				}
			}

			// Token: 0x170012C4 RID: 4804
			// (get) Token: 0x06006E25 RID: 28197 RVA: 0x0017C564 File Offset: 0x0017A764
			public override object SyncRoot
			{
				get
				{
					return this._root;
				}
			}

			// Token: 0x170012C5 RID: 4805
			// (get) Token: 0x06006E26 RID: 28198 RVA: 0x0017C56C File Offset: 0x0017A76C
			public override bool IsReadOnly
			{
				get
				{
					return this._list.IsReadOnly;
				}
			}

			// Token: 0x170012C6 RID: 4806
			// (get) Token: 0x06006E27 RID: 28199 RVA: 0x0017C579 File Offset: 0x0017A779
			public override bool IsFixedSize
			{
				get
				{
					return this._list.IsFixedSize;
				}
			}

			// Token: 0x170012C7 RID: 4807
			// (get) Token: 0x06006E28 RID: 28200 RVA: 0x0017C586 File Offset: 0x0017A786
			public override bool IsSynchronized
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012C8 RID: 4808
			public override object this[object key]
			{
				get
				{
					object root = this._root;
					object result;
					lock (root)
					{
						result = this._list[key];
					}
					return result;
				}
				set
				{
					object root = this._root;
					lock (root)
					{
						this._list[key] = value;
					}
				}
			}

			// Token: 0x06006E2B RID: 28203 RVA: 0x0017C61C File Offset: 0x0017A81C
			public override void Add(object key, object value)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Add(key, value);
				}
			}

			// Token: 0x170012C9 RID: 4809
			// (get) Token: 0x06006E2C RID: 28204 RVA: 0x0017C664 File Offset: 0x0017A864
			public override int Capacity
			{
				get
				{
					object root = this._root;
					int capacity;
					lock (root)
					{
						capacity = this._list.Capacity;
					}
					return capacity;
				}
			}

			// Token: 0x06006E2D RID: 28205 RVA: 0x0017C6AC File Offset: 0x0017A8AC
			public override void Clear()
			{
				object root = this._root;
				lock (root)
				{
					this._list.Clear();
				}
			}

			// Token: 0x06006E2E RID: 28206 RVA: 0x0017C6F4 File Offset: 0x0017A8F4
			public override object Clone()
			{
				object root = this._root;
				object result;
				lock (root)
				{
					result = this._list.Clone();
				}
				return result;
			}

			// Token: 0x06006E2F RID: 28207 RVA: 0x0017C73C File Offset: 0x0017A93C
			public override bool Contains(object key)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.Contains(key);
				}
				return result;
			}

			// Token: 0x06006E30 RID: 28208 RVA: 0x0017C784 File Offset: 0x0017A984
			public override bool ContainsKey(object key)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.ContainsKey(key);
				}
				return result;
			}

			// Token: 0x06006E31 RID: 28209 RVA: 0x0017C7CC File Offset: 0x0017A9CC
			public override bool ContainsValue(object key)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.ContainsValue(key);
				}
				return result;
			}

			// Token: 0x06006E32 RID: 28210 RVA: 0x0017C814 File Offset: 0x0017AA14
			public override void CopyTo(Array array, int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.CopyTo(array, index);
				}
			}

			// Token: 0x06006E33 RID: 28211 RVA: 0x0017C85C File Offset: 0x0017AA5C
			public override object GetByIndex(int index)
			{
				object root = this._root;
				object byIndex;
				lock (root)
				{
					byIndex = this._list.GetByIndex(index);
				}
				return byIndex;
			}

			// Token: 0x06006E34 RID: 28212 RVA: 0x0017C8A4 File Offset: 0x0017AAA4
			public override IDictionaryEnumerator GetEnumerator()
			{
				object root = this._root;
				IDictionaryEnumerator enumerator;
				lock (root)
				{
					enumerator = this._list.GetEnumerator();
				}
				return enumerator;
			}

			// Token: 0x06006E35 RID: 28213 RVA: 0x0017C8EC File Offset: 0x0017AAEC
			public override object GetKey(int index)
			{
				object root = this._root;
				object key;
				lock (root)
				{
					key = this._list.GetKey(index);
				}
				return key;
			}

			// Token: 0x06006E36 RID: 28214 RVA: 0x0017C934 File Offset: 0x0017AB34
			public override IList GetKeyList()
			{
				object root = this._root;
				IList keyList;
				lock (root)
				{
					keyList = this._list.GetKeyList();
				}
				return keyList;
			}

			// Token: 0x06006E37 RID: 28215 RVA: 0x0017C97C File Offset: 0x0017AB7C
			public override IList GetValueList()
			{
				object root = this._root;
				IList valueList;
				lock (root)
				{
					valueList = this._list.GetValueList();
				}
				return valueList;
			}

			// Token: 0x06006E38 RID: 28216 RVA: 0x0017C9C4 File Offset: 0x0017ABC4
			public override int IndexOfKey(object key)
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOfKey(key);
				}
				return result;
			}

			// Token: 0x06006E39 RID: 28217 RVA: 0x0017CA24 File Offset: 0x0017AC24
			public override int IndexOfValue(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOfValue(value);
				}
				return result;
			}

			// Token: 0x06006E3A RID: 28218 RVA: 0x0017CA6C File Offset: 0x0017AC6C
			public override void RemoveAt(int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.RemoveAt(index);
				}
			}

			// Token: 0x06006E3B RID: 28219 RVA: 0x0017CAB4 File Offset: 0x0017ACB4
			public override void Remove(object key)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Remove(key);
				}
			}

			// Token: 0x06006E3C RID: 28220 RVA: 0x0017CAFC File Offset: 0x0017ACFC
			public override void SetByIndex(int index, object value)
			{
				object root = this._root;
				lock (root)
				{
					this._list.SetByIndex(index, value);
				}
			}

			// Token: 0x06006E3D RID: 28221 RVA: 0x0017CB44 File Offset: 0x0017AD44
			internal override KeyValuePairs[] ToKeyValuePairsArray()
			{
				return this._list.ToKeyValuePairsArray();
			}

			// Token: 0x06006E3E RID: 28222 RVA: 0x0017CB54 File Offset: 0x0017AD54
			public override void TrimToSize()
			{
				object root = this._root;
				lock (root)
				{
					this._list.TrimToSize();
				}
			}

			// Token: 0x04003582 RID: 13698
			private SortedList _list;

			// Token: 0x04003583 RID: 13699
			private object _root;
		}

		// Token: 0x02000BBE RID: 3006
		[Serializable]
		private class SortedListEnumerator : IDictionaryEnumerator, IEnumerator, ICloneable
		{
			// Token: 0x06006E3F RID: 28223 RVA: 0x0017CB9C File Offset: 0x0017AD9C
			internal SortedListEnumerator(SortedList sortedList, int index, int count, int getObjRetType)
			{
				this.sortedList = sortedList;
				this.index = index;
				this.startIndex = index;
				this.endIndex = index + count;
				this.version = sortedList.version;
				this.getObjectRetType = getObjRetType;
				this.current = false;
			}

			// Token: 0x06006E40 RID: 28224 RVA: 0x0017CBE8 File Offset: 0x0017ADE8
			public object Clone()
			{
				return base.MemberwiseClone();
			}

			// Token: 0x170012CA RID: 4810
			// (get) Token: 0x06006E41 RID: 28225 RVA: 0x0017CBF0 File Offset: 0x0017ADF0
			public virtual object Key
			{
				get
				{
					if (this.version != this.sortedList.version)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return this.key;
				}
			}

			// Token: 0x06006E42 RID: 28226 RVA: 0x0017CC40 File Offset: 0x0017AE40
			public virtual bool MoveNext()
			{
				if (this.version != this.sortedList.version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				if (this.index < this.endIndex)
				{
					this.key = this.sortedList.keys[this.index];
					this.value = this.sortedList.values[this.index];
					this.index++;
					this.current = true;
					return true;
				}
				this.key = null;
				this.value = null;
				this.current = false;
				return false;
			}

			// Token: 0x170012CB RID: 4811
			// (get) Token: 0x06006E43 RID: 28227 RVA: 0x0017CCDC File Offset: 0x0017AEDC
			public virtual DictionaryEntry Entry
			{
				get
				{
					if (this.version != this.sortedList.version)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return new DictionaryEntry(this.key, this.value);
				}
			}

			// Token: 0x170012CC RID: 4812
			// (get) Token: 0x06006E44 RID: 28228 RVA: 0x0017CD38 File Offset: 0x0017AF38
			public virtual object Current
			{
				get
				{
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					if (this.getObjectRetType == 1)
					{
						return this.key;
					}
					if (this.getObjectRetType == 2)
					{
						return this.value;
					}
					return new DictionaryEntry(this.key, this.value);
				}
			}

			// Token: 0x170012CD RID: 4813
			// (get) Token: 0x06006E45 RID: 28229 RVA: 0x0017CD94 File Offset: 0x0017AF94
			public virtual object Value
			{
				get
				{
					if (this.version != this.sortedList.version)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					if (!this.current)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return this.value;
				}
			}

			// Token: 0x06006E46 RID: 28230 RVA: 0x0017CDE4 File Offset: 0x0017AFE4
			public virtual void Reset()
			{
				if (this.version != this.sortedList.version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this.index = this.startIndex;
				this.current = false;
				this.key = null;
				this.value = null;
			}

			// Token: 0x04003584 RID: 13700
			private SortedList sortedList;

			// Token: 0x04003585 RID: 13701
			private object key;

			// Token: 0x04003586 RID: 13702
			private object value;

			// Token: 0x04003587 RID: 13703
			private int index;

			// Token: 0x04003588 RID: 13704
			private int startIndex;

			// Token: 0x04003589 RID: 13705
			private int endIndex;

			// Token: 0x0400358A RID: 13706
			private int version;

			// Token: 0x0400358B RID: 13707
			private bool current;

			// Token: 0x0400358C RID: 13708
			private int getObjectRetType;

			// Token: 0x0400358D RID: 13709
			internal const int Keys = 1;

			// Token: 0x0400358E RID: 13710
			internal const int Values = 2;

			// Token: 0x0400358F RID: 13711
			internal const int DictEntry = 3;
		}

		// Token: 0x02000BBF RID: 3007
		[Serializable]
		private class KeyList : IList, ICollection, IEnumerable
		{
			// Token: 0x06006E47 RID: 28231 RVA: 0x0017CE35 File Offset: 0x0017B035
			internal KeyList(SortedList sortedList)
			{
				this.sortedList = sortedList;
			}

			// Token: 0x170012CE RID: 4814
			// (get) Token: 0x06006E48 RID: 28232 RVA: 0x0017CE44 File Offset: 0x0017B044
			public virtual int Count
			{
				get
				{
					return this.sortedList._size;
				}
			}

			// Token: 0x170012CF RID: 4815
			// (get) Token: 0x06006E49 RID: 28233 RVA: 0x0017CE51 File Offset: 0x0017B051
			public virtual bool IsReadOnly
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012D0 RID: 4816
			// (get) Token: 0x06006E4A RID: 28234 RVA: 0x0017CE54 File Offset: 0x0017B054
			public virtual bool IsFixedSize
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012D1 RID: 4817
			// (get) Token: 0x06006E4B RID: 28235 RVA: 0x0017CE57 File Offset: 0x0017B057
			public virtual bool IsSynchronized
			{
				get
				{
					return this.sortedList.IsSynchronized;
				}
			}

			// Token: 0x170012D2 RID: 4818
			// (get) Token: 0x06006E4C RID: 28236 RVA: 0x0017CE64 File Offset: 0x0017B064
			public virtual object SyncRoot
			{
				get
				{
					return this.sortedList.SyncRoot;
				}
			}

			// Token: 0x06006E4D RID: 28237 RVA: 0x0017CE71 File Offset: 0x0017B071
			public virtual int Add(object key)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x06006E4E RID: 28238 RVA: 0x0017CE82 File Offset: 0x0017B082
			public virtual void Clear()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x06006E4F RID: 28239 RVA: 0x0017CE93 File Offset: 0x0017B093
			public virtual bool Contains(object key)
			{
				return this.sortedList.Contains(key);
			}

			// Token: 0x06006E50 RID: 28240 RVA: 0x0017CEA1 File Offset: 0x0017B0A1
			public virtual void CopyTo(Array array, int arrayIndex)
			{
				if (array != null && array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				Array.Copy(this.sortedList.keys, 0, array, arrayIndex, this.sortedList.Count);
			}

			// Token: 0x06006E51 RID: 28241 RVA: 0x0017CEDD File Offset: 0x0017B0DD
			public virtual void Insert(int index, object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x170012D3 RID: 4819
			public virtual object this[int index]
			{
				get
				{
					return this.sortedList.GetKey(index);
				}
				set
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
				}
			}

			// Token: 0x06006E54 RID: 28244 RVA: 0x0017CF0D File Offset: 0x0017B10D
			public virtual IEnumerator GetEnumerator()
			{
				return new SortedList.SortedListEnumerator(this.sortedList, 0, this.sortedList.Count, 1);
			}

			// Token: 0x06006E55 RID: 28245 RVA: 0x0017CF28 File Offset: 0x0017B128
			public virtual int IndexOf(object key)
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				int num = Array.BinarySearch(this.sortedList.keys, 0, this.sortedList.Count, key, this.sortedList.comparer);
				if (num >= 0)
				{
					return num;
				}
				return -1;
			}

			// Token: 0x06006E56 RID: 28246 RVA: 0x0017CF7D File Offset: 0x0017B17D
			public virtual void Remove(object key)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x06006E57 RID: 28247 RVA: 0x0017CF8E File Offset: 0x0017B18E
			public virtual void RemoveAt(int index)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x04003590 RID: 13712
			private SortedList sortedList;
		}

		// Token: 0x02000BC0 RID: 3008
		[Serializable]
		private class ValueList : IList, ICollection, IEnumerable
		{
			// Token: 0x06006E58 RID: 28248 RVA: 0x0017CF9F File Offset: 0x0017B19F
			internal ValueList(SortedList sortedList)
			{
				this.sortedList = sortedList;
			}

			// Token: 0x170012D4 RID: 4820
			// (get) Token: 0x06006E59 RID: 28249 RVA: 0x0017CFAE File Offset: 0x0017B1AE
			public virtual int Count
			{
				get
				{
					return this.sortedList._size;
				}
			}

			// Token: 0x170012D5 RID: 4821
			// (get) Token: 0x06006E5A RID: 28250 RVA: 0x0017CFBB File Offset: 0x0017B1BB
			public virtual bool IsReadOnly
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012D6 RID: 4822
			// (get) Token: 0x06006E5B RID: 28251 RVA: 0x0017CFBE File Offset: 0x0017B1BE
			public virtual bool IsFixedSize
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012D7 RID: 4823
			// (get) Token: 0x06006E5C RID: 28252 RVA: 0x0017CFC1 File Offset: 0x0017B1C1
			public virtual bool IsSynchronized
			{
				get
				{
					return this.sortedList.IsSynchronized;
				}
			}

			// Token: 0x170012D8 RID: 4824
			// (get) Token: 0x06006E5D RID: 28253 RVA: 0x0017CFCE File Offset: 0x0017B1CE
			public virtual object SyncRoot
			{
				get
				{
					return this.sortedList.SyncRoot;
				}
			}

			// Token: 0x06006E5E RID: 28254 RVA: 0x0017CFDB File Offset: 0x0017B1DB
			public virtual int Add(object key)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x06006E5F RID: 28255 RVA: 0x0017CFEC File Offset: 0x0017B1EC
			public virtual void Clear()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x06006E60 RID: 28256 RVA: 0x0017CFFD File Offset: 0x0017B1FD
			public virtual bool Contains(object value)
			{
				return this.sortedList.ContainsValue(value);
			}

			// Token: 0x06006E61 RID: 28257 RVA: 0x0017D00B File Offset: 0x0017B20B
			public virtual void CopyTo(Array array, int arrayIndex)
			{
				if (array != null && array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				Array.Copy(this.sortedList.values, 0, array, arrayIndex, this.sortedList.Count);
			}

			// Token: 0x06006E62 RID: 28258 RVA: 0x0017D047 File Offset: 0x0017B247
			public virtual void Insert(int index, object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x170012D9 RID: 4825
			public virtual object this[int index]
			{
				get
				{
					return this.sortedList.GetByIndex(index);
				}
				set
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
				}
			}

			// Token: 0x06006E65 RID: 28261 RVA: 0x0017D077 File Offset: 0x0017B277
			public virtual IEnumerator GetEnumerator()
			{
				return new SortedList.SortedListEnumerator(this.sortedList, 0, this.sortedList.Count, 2);
			}

			// Token: 0x06006E66 RID: 28262 RVA: 0x0017D091 File Offset: 0x0017B291
			public virtual int IndexOf(object value)
			{
				return Array.IndexOf<object>(this.sortedList.values, value, 0, this.sortedList.Count);
			}

			// Token: 0x06006E67 RID: 28263 RVA: 0x0017D0B0 File Offset: 0x0017B2B0
			public virtual void Remove(object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x06006E68 RID: 28264 RVA: 0x0017D0C1 File Offset: 0x0017B2C1
			public virtual void RemoveAt(int index)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SortedListNestedWrite"));
			}

			// Token: 0x04003591 RID: 13713
			private SortedList sortedList;
		}

		// Token: 0x02000BC1 RID: 3009
		internal class SortedListDebugView
		{
			// Token: 0x06006E69 RID: 28265 RVA: 0x0017D0D2 File Offset: 0x0017B2D2
			public SortedListDebugView(SortedList sortedList)
			{
				if (sortedList == null)
				{
					throw new ArgumentNullException("sortedList");
				}
				this.sortedList = sortedList;
			}

			// Token: 0x170012DA RID: 4826
			// (get) Token: 0x06006E6A RID: 28266 RVA: 0x0017D0EF File Offset: 0x0017B2EF
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairs[] Items
			{
				get
				{
					return this.sortedList.ToKeyValuePairsArray();
				}
			}

			// Token: 0x04003592 RID: 13714
			private SortedList sortedList;
		}
	}
}
