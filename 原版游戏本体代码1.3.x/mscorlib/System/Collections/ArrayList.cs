using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections
{
	// Token: 0x02000490 RID: 1168
	[DebuggerTypeProxy(typeof(ArrayList.ArrayListDebugView))]
	[DebuggerDisplay("Count = {Count}")]
	[ComVisible(true)]
	[Serializable]
	public class ArrayList : IList, ICollection, IEnumerable, ICloneable
	{
		// Token: 0x060037CF RID: 14287 RVA: 0x000D63F8 File Offset: 0x000D45F8
		internal ArrayList(bool trash)
		{
		}

		// Token: 0x060037D0 RID: 14288 RVA: 0x000D6400 File Offset: 0x000D4600
		public ArrayList()
		{
			this._items = ArrayList.emptyArray;
		}

		// Token: 0x060037D1 RID: 14289 RVA: 0x000D6414 File Offset: 0x000D4614
		public ArrayList(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", new object[] { "capacity" }));
			}
			if (capacity == 0)
			{
				this._items = ArrayList.emptyArray;
				return;
			}
			this._items = new object[capacity];
		}

		// Token: 0x060037D2 RID: 14290 RVA: 0x000D646C File Offset: 0x000D466C
		public ArrayList(ICollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
			}
			int count = c.Count;
			if (count == 0)
			{
				this._items = ArrayList.emptyArray;
				return;
			}
			this._items = new object[count];
			this.AddRange(c);
		}

		// Token: 0x1700083D RID: 2109
		// (get) Token: 0x060037D3 RID: 14291 RVA: 0x000D64C0 File Offset: 0x000D46C0
		// (set) Token: 0x060037D4 RID: 14292 RVA: 0x000D64CC File Offset: 0x000D46CC
		public virtual int Capacity
		{
			get
			{
				return this._items.Length;
			}
			set
			{
				if (value < this._size)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
				}
				if (value != this._items.Length)
				{
					if (value > 0)
					{
						object[] array = new object[value];
						if (this._size > 0)
						{
							Array.Copy(this._items, 0, array, 0, this._size);
						}
						this._items = array;
						return;
					}
					this._items = new object[4];
				}
			}
		}

		// Token: 0x1700083E RID: 2110
		// (get) Token: 0x060037D5 RID: 14293 RVA: 0x000D653E File Offset: 0x000D473E
		public virtual int Count
		{
			get
			{
				return this._size;
			}
		}

		// Token: 0x1700083F RID: 2111
		// (get) Token: 0x060037D6 RID: 14294 RVA: 0x000D6546 File Offset: 0x000D4746
		public virtual bool IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000840 RID: 2112
		// (get) Token: 0x060037D7 RID: 14295 RVA: 0x000D6549 File Offset: 0x000D4749
		public virtual bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000841 RID: 2113
		// (get) Token: 0x060037D8 RID: 14296 RVA: 0x000D654C File Offset: 0x000D474C
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000842 RID: 2114
		// (get) Token: 0x060037D9 RID: 14297 RVA: 0x000D654F File Offset: 0x000D474F
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

		// Token: 0x17000843 RID: 2115
		public virtual object this[int index]
		{
			get
			{
				if (index < 0 || index >= this._size)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				return this._items[index];
			}
			set
			{
				if (index < 0 || index >= this._size)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				this._items[index] = value;
				this._version++;
			}
		}

		// Token: 0x060037DC RID: 14300 RVA: 0x000D65D8 File Offset: 0x000D47D8
		public static ArrayList Adapter(IList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.IListWrapper(list);
		}

		// Token: 0x060037DD RID: 14301 RVA: 0x000D65F0 File Offset: 0x000D47F0
		public virtual int Add(object value)
		{
			if (this._size == this._items.Length)
			{
				this.EnsureCapacity(this._size + 1);
			}
			this._items[this._size] = value;
			this._version++;
			int size = this._size;
			this._size = size + 1;
			return size;
		}

		// Token: 0x060037DE RID: 14302 RVA: 0x000D6648 File Offset: 0x000D4848
		public virtual void AddRange(ICollection c)
		{
			this.InsertRange(this._size, c);
		}

		// Token: 0x060037DF RID: 14303 RVA: 0x000D6658 File Offset: 0x000D4858
		public virtual int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			return Array.BinarySearch(this._items, index, count, value, comparer);
		}

		// Token: 0x060037E0 RID: 14304 RVA: 0x000D66C2 File Offset: 0x000D48C2
		public virtual int BinarySearch(object value)
		{
			return this.BinarySearch(0, this.Count, value, null);
		}

		// Token: 0x060037E1 RID: 14305 RVA: 0x000D66D3 File Offset: 0x000D48D3
		public virtual int BinarySearch(object value, IComparer comparer)
		{
			return this.BinarySearch(0, this.Count, value, comparer);
		}

		// Token: 0x060037E2 RID: 14306 RVA: 0x000D66E4 File Offset: 0x000D48E4
		public virtual void Clear()
		{
			if (this._size > 0)
			{
				Array.Clear(this._items, 0, this._size);
				this._size = 0;
			}
			this._version++;
		}

		// Token: 0x060037E3 RID: 14307 RVA: 0x000D6718 File Offset: 0x000D4918
		public virtual object Clone()
		{
			ArrayList arrayList = new ArrayList(this._size);
			arrayList._size = this._size;
			arrayList._version = this._version;
			Array.Copy(this._items, 0, arrayList._items, 0, this._size);
			return arrayList;
		}

		// Token: 0x060037E4 RID: 14308 RVA: 0x000D6764 File Offset: 0x000D4964
		public virtual bool Contains(object item)
		{
			if (item == null)
			{
				for (int i = 0; i < this._size; i++)
				{
					if (this._items[i] == null)
					{
						return true;
					}
				}
				return false;
			}
			for (int j = 0; j < this._size; j++)
			{
				if (this._items[j] != null && this._items[j].Equals(item))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060037E5 RID: 14309 RVA: 0x000D67C1 File Offset: 0x000D49C1
		public virtual void CopyTo(Array array)
		{
			this.CopyTo(array, 0);
		}

		// Token: 0x060037E6 RID: 14310 RVA: 0x000D67CB File Offset: 0x000D49CB
		public virtual void CopyTo(Array array, int arrayIndex)
		{
			if (array != null && array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			Array.Copy(this._items, 0, array, arrayIndex, this._size);
		}

		// Token: 0x060037E7 RID: 14311 RVA: 0x000D6800 File Offset: 0x000D4A00
		public virtual void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (array != null && array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			Array.Copy(this._items, index, array, arrayIndex, count);
		}

		// Token: 0x060037E8 RID: 14312 RVA: 0x000D6858 File Offset: 0x000D4A58
		private void EnsureCapacity(int min)
		{
			if (this._items.Length < min)
			{
				int num = ((this._items.Length == 0) ? 4 : (this._items.Length * 2));
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
		}

		// Token: 0x060037E9 RID: 14313 RVA: 0x000D68A2 File Offset: 0x000D4AA2
		public static IList FixedSize(IList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.FixedSizeList(list);
		}

		// Token: 0x060037EA RID: 14314 RVA: 0x000D68B8 File Offset: 0x000D4AB8
		public static ArrayList FixedSize(ArrayList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.FixedSizeArrayList(list);
		}

		// Token: 0x060037EB RID: 14315 RVA: 0x000D68CE File Offset: 0x000D4ACE
		public virtual IEnumerator GetEnumerator()
		{
			return new ArrayList.ArrayListEnumeratorSimple(this);
		}

		// Token: 0x060037EC RID: 14316 RVA: 0x000D68D8 File Offset: 0x000D4AD8
		public virtual IEnumerator GetEnumerator(int index, int count)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			return new ArrayList.ArrayListEnumerator(this, index, count);
		}

		// Token: 0x060037ED RID: 14317 RVA: 0x000D693A File Offset: 0x000D4B3A
		public virtual int IndexOf(object value)
		{
			return Array.IndexOf(this._items, value, 0, this._size);
		}

		// Token: 0x060037EE RID: 14318 RVA: 0x000D694F File Offset: 0x000D4B4F
		public virtual int IndexOf(object value, int startIndex)
		{
			if (startIndex > this._size)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return Array.IndexOf(this._items, value, startIndex, this._size - startIndex);
		}

		// Token: 0x060037EF RID: 14319 RVA: 0x000D6984 File Offset: 0x000D4B84
		public virtual int IndexOf(object value, int startIndex, int count)
		{
			if (startIndex > this._size)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count < 0 || startIndex > this._size - count)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
			}
			return Array.IndexOf(this._items, value, startIndex, count);
		}

		// Token: 0x060037F0 RID: 14320 RVA: 0x000D69E4 File Offset: 0x000D4BE4
		public virtual void Insert(int index, object value)
		{
			if (index < 0 || index > this._size)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_ArrayListInsert"));
			}
			if (this._size == this._items.Length)
			{
				this.EnsureCapacity(this._size + 1);
			}
			if (index < this._size)
			{
				Array.Copy(this._items, index, this._items, index + 1, this._size - index);
			}
			this._items[index] = value;
			this._size++;
			this._version++;
		}

		// Token: 0x060037F1 RID: 14321 RVA: 0x000D6A7C File Offset: 0x000D4C7C
		public virtual void InsertRange(int index, ICollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
			}
			if (index < 0 || index > this._size)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			int count = c.Count;
			if (count > 0)
			{
				this.EnsureCapacity(this._size + count);
				if (index < this._size)
				{
					Array.Copy(this._items, index, this._items, index + count, this._size - index);
				}
				object[] array = new object[count];
				c.CopyTo(array, 0);
				array.CopyTo(this._items, index);
				this._size += count;
				this._version++;
			}
		}

		// Token: 0x060037F2 RID: 14322 RVA: 0x000D6B3A File Offset: 0x000D4D3A
		public virtual int LastIndexOf(object value)
		{
			return this.LastIndexOf(value, this._size - 1, this._size);
		}

		// Token: 0x060037F3 RID: 14323 RVA: 0x000D6B51 File Offset: 0x000D4D51
		public virtual int LastIndexOf(object value, int startIndex)
		{
			if (startIndex >= this._size)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return this.LastIndexOf(value, startIndex, startIndex + 1);
		}

		// Token: 0x060037F4 RID: 14324 RVA: 0x000D6B7C File Offset: 0x000D4D7C
		public virtual int LastIndexOf(object value, int startIndex, int count)
		{
			if (this.Count != 0 && (startIndex < 0 || count < 0))
			{
				throw new ArgumentOutOfRangeException((startIndex < 0) ? "startIndex" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size == 0)
			{
				return -1;
			}
			if (startIndex >= this._size || count > startIndex + 1)
			{
				throw new ArgumentOutOfRangeException((startIndex >= this._size) ? "startIndex" : "count", Environment.GetResourceString("ArgumentOutOfRange_BiggerThanCollection"));
			}
			return Array.LastIndexOf(this._items, value, startIndex, count);
		}

		// Token: 0x060037F5 RID: 14325 RVA: 0x000D6C05 File Offset: 0x000D4E05
		public static IList ReadOnly(IList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.ReadOnlyList(list);
		}

		// Token: 0x060037F6 RID: 14326 RVA: 0x000D6C1B File Offset: 0x000D4E1B
		public static ArrayList ReadOnly(ArrayList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.ReadOnlyArrayList(list);
		}

		// Token: 0x060037F7 RID: 14327 RVA: 0x000D6C34 File Offset: 0x000D4E34
		public virtual void Remove(object obj)
		{
			int num = this.IndexOf(obj);
			if (num >= 0)
			{
				this.RemoveAt(num);
			}
		}

		// Token: 0x060037F8 RID: 14328 RVA: 0x000D6C54 File Offset: 0x000D4E54
		public virtual void RemoveAt(int index)
		{
			if (index < 0 || index >= this._size)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			this._size--;
			if (index < this._size)
			{
				Array.Copy(this._items, index + 1, this._items, index, this._size - index);
			}
			this._items[this._size] = null;
			this._version++;
		}

		// Token: 0x060037F9 RID: 14329 RVA: 0x000D6CD4 File Offset: 0x000D4ED4
		public virtual void RemoveRange(int index, int count)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (count > 0)
			{
				int i = this._size;
				this._size -= count;
				if (index < this._size)
				{
					Array.Copy(this._items, index + count, this._items, index, this._size - index);
				}
				while (i > this._size)
				{
					this._items[--i] = null;
				}
				this._version++;
			}
		}

		// Token: 0x060037FA RID: 14330 RVA: 0x000D6D94 File Offset: 0x000D4F94
		public static ArrayList Repeat(object value, int count)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			ArrayList arrayList = new ArrayList((count > 4) ? count : 4);
			for (int i = 0; i < count; i++)
			{
				arrayList.Add(value);
			}
			return arrayList;
		}

		// Token: 0x060037FB RID: 14331 RVA: 0x000D6DDD File Offset: 0x000D4FDD
		public virtual void Reverse()
		{
			this.Reverse(0, this.Count);
		}

		// Token: 0x060037FC RID: 14332 RVA: 0x000D6DEC File Offset: 0x000D4FEC
		public virtual void Reverse(int index, int count)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			Array.Reverse(this._items, index, count);
			this._version++;
		}

		// Token: 0x060037FD RID: 14333 RVA: 0x000D6E64 File Offset: 0x000D5064
		public virtual void SetRange(int index, ICollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
			}
			int count = c.Count;
			if (index < 0 || index > this._size - count)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count > 0)
			{
				c.CopyTo(this._items, index);
				this._version++;
			}
		}

		// Token: 0x060037FE RID: 14334 RVA: 0x000D6ED4 File Offset: 0x000D50D4
		public virtual ArrayList GetRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			return new ArrayList.Range(this, index, count);
		}

		// Token: 0x060037FF RID: 14335 RVA: 0x000D6F2C File Offset: 0x000D512C
		public virtual void Sort()
		{
			this.Sort(0, this.Count, Comparer.Default);
		}

		// Token: 0x06003800 RID: 14336 RVA: 0x000D6F40 File Offset: 0x000D5140
		public virtual void Sort(IComparer comparer)
		{
			this.Sort(0, this.Count, comparer);
		}

		// Token: 0x06003801 RID: 14337 RVA: 0x000D6F50 File Offset: 0x000D5150
		public virtual void Sort(int index, int count, IComparer comparer)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._size - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			Array.Sort(this._items, index, count, comparer);
			this._version++;
		}

		// Token: 0x06003802 RID: 14338 RVA: 0x000D6FC6 File Offset: 0x000D51C6
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static IList Synchronized(IList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.SyncIList(list);
		}

		// Token: 0x06003803 RID: 14339 RVA: 0x000D6FDC File Offset: 0x000D51DC
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static ArrayList Synchronized(ArrayList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			return new ArrayList.SyncArrayList(list);
		}

		// Token: 0x06003804 RID: 14340 RVA: 0x000D6FF4 File Offset: 0x000D51F4
		public virtual object[] ToArray()
		{
			object[] array = new object[this._size];
			Array.Copy(this._items, 0, array, 0, this._size);
			return array;
		}

		// Token: 0x06003805 RID: 14341 RVA: 0x000D7024 File Offset: 0x000D5224
		[SecuritySafeCritical]
		public virtual Array ToArray(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			Array array = Array.UnsafeCreateInstance(type, this._size);
			Array.Copy(this._items, 0, array, 0, this._size);
			return array;
		}

		// Token: 0x06003806 RID: 14342 RVA: 0x000D7067 File Offset: 0x000D5267
		public virtual void TrimToSize()
		{
			this.Capacity = this._size;
		}

		// Token: 0x040018C5 RID: 6341
		private object[] _items;

		// Token: 0x040018C6 RID: 6342
		private int _size;

		// Token: 0x040018C7 RID: 6343
		private int _version;

		// Token: 0x040018C8 RID: 6344
		[NonSerialized]
		private object _syncRoot;

		// Token: 0x040018C9 RID: 6345
		private const int _defaultCapacity = 4;

		// Token: 0x040018CA RID: 6346
		private static readonly object[] emptyArray = EmptyArray<object>.Value;

		// Token: 0x02000BA4 RID: 2980
		[Serializable]
		private class IListWrapper : ArrayList
		{
			// Token: 0x06006CC3 RID: 27843 RVA: 0x001786C4 File Offset: 0x001768C4
			internal IListWrapper(IList list)
			{
				this._list = list;
				this._version = 0;
			}

			// Token: 0x17001267 RID: 4711
			// (get) Token: 0x06006CC4 RID: 27844 RVA: 0x001786DA File Offset: 0x001768DA
			// (set) Token: 0x06006CC5 RID: 27845 RVA: 0x001786E7 File Offset: 0x001768E7
			public override int Capacity
			{
				get
				{
					return this._list.Count;
				}
				set
				{
					if (value < this.Count)
					{
						throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
					}
				}
			}

			// Token: 0x17001268 RID: 4712
			// (get) Token: 0x06006CC6 RID: 27846 RVA: 0x00178707 File Offset: 0x00176907
			public override int Count
			{
				get
				{
					return this._list.Count;
				}
			}

			// Token: 0x17001269 RID: 4713
			// (get) Token: 0x06006CC7 RID: 27847 RVA: 0x00178714 File Offset: 0x00176914
			public override bool IsReadOnly
			{
				get
				{
					return this._list.IsReadOnly;
				}
			}

			// Token: 0x1700126A RID: 4714
			// (get) Token: 0x06006CC8 RID: 27848 RVA: 0x00178721 File Offset: 0x00176921
			public override bool IsFixedSize
			{
				get
				{
					return this._list.IsFixedSize;
				}
			}

			// Token: 0x1700126B RID: 4715
			// (get) Token: 0x06006CC9 RID: 27849 RVA: 0x0017872E File Offset: 0x0017692E
			public override bool IsSynchronized
			{
				get
				{
					return this._list.IsSynchronized;
				}
			}

			// Token: 0x1700126C RID: 4716
			public override object this[int index]
			{
				get
				{
					return this._list[index];
				}
				set
				{
					this._list[index] = value;
					this._version++;
				}
			}

			// Token: 0x1700126D RID: 4717
			// (get) Token: 0x06006CCC RID: 27852 RVA: 0x00178766 File Offset: 0x00176966
			public override object SyncRoot
			{
				get
				{
					return this._list.SyncRoot;
				}
			}

			// Token: 0x06006CCD RID: 27853 RVA: 0x00178774 File Offset: 0x00176974
			public override int Add(object obj)
			{
				int result = this._list.Add(obj);
				this._version++;
				return result;
			}

			// Token: 0x06006CCE RID: 27854 RVA: 0x0017879D File Offset: 0x0017699D
			public override void AddRange(ICollection c)
			{
				this.InsertRange(this.Count, c);
			}

			// Token: 0x06006CCF RID: 27855 RVA: 0x001787AC File Offset: 0x001769AC
			public override int BinarySearch(int index, int count, object value, IComparer comparer)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				if (comparer == null)
				{
					comparer = Comparer.Default;
				}
				int i = index;
				int num = index + count - 1;
				while (i <= num)
				{
					int num2 = (i + num) / 2;
					int num3 = comparer.Compare(value, this._list[num2]);
					if (num3 == 0)
					{
						return num2;
					}
					if (num3 < 0)
					{
						num = num2 - 1;
					}
					else
					{
						i = num2 + 1;
					}
				}
				return ~i;
			}

			// Token: 0x06006CD0 RID: 27856 RVA: 0x00178845 File Offset: 0x00176A45
			public override void Clear()
			{
				if (this._list.IsFixedSize)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
				}
				this._list.Clear();
				this._version++;
			}

			// Token: 0x06006CD1 RID: 27857 RVA: 0x0017887D File Offset: 0x00176A7D
			public override object Clone()
			{
				return new ArrayList.IListWrapper(this._list);
			}

			// Token: 0x06006CD2 RID: 27858 RVA: 0x0017888A File Offset: 0x00176A8A
			public override bool Contains(object obj)
			{
				return this._list.Contains(obj);
			}

			// Token: 0x06006CD3 RID: 27859 RVA: 0x00178898 File Offset: 0x00176A98
			public override void CopyTo(Array array, int index)
			{
				this._list.CopyTo(array, index);
			}

			// Token: 0x06006CD4 RID: 27860 RVA: 0x001788A8 File Offset: 0x00176AA8
			public override void CopyTo(int index, Array array, int arrayIndex, int count)
			{
				if (array == null)
				{
					throw new ArgumentNullException("array");
				}
				if (index < 0 || arrayIndex < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (count < 0)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (array.Length - arrayIndex < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				if (array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				if (this._list.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				for (int i = index; i < index + count; i++)
				{
					array.SetValue(this._list[i], arrayIndex++);
				}
			}

			// Token: 0x06006CD5 RID: 27861 RVA: 0x00178982 File Offset: 0x00176B82
			public override IEnumerator GetEnumerator()
			{
				return this._list.GetEnumerator();
			}

			// Token: 0x06006CD6 RID: 27862 RVA: 0x00178990 File Offset: 0x00176B90
			public override IEnumerator GetEnumerator(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._list.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				return new ArrayList.IListWrapper.IListWrapperEnumWrapper(this, index, count);
			}

			// Token: 0x06006CD7 RID: 27863 RVA: 0x001789ED File Offset: 0x00176BED
			public override int IndexOf(object value)
			{
				return this._list.IndexOf(value);
			}

			// Token: 0x06006CD8 RID: 27864 RVA: 0x001789FB File Offset: 0x00176BFB
			public override int IndexOf(object value, int startIndex)
			{
				return this.IndexOf(value, startIndex, this._list.Count - startIndex);
			}

			// Token: 0x06006CD9 RID: 27865 RVA: 0x00178A14 File Offset: 0x00176C14
			public override int IndexOf(object value, int startIndex, int count)
			{
				if (startIndex < 0 || startIndex > this.Count)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (count < 0 || startIndex > this.Count - count)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
				}
				int num = startIndex + count;
				if (value == null)
				{
					for (int i = startIndex; i < num; i++)
					{
						if (this._list[i] == null)
						{
							return i;
						}
					}
					return -1;
				}
				for (int j = startIndex; j < num; j++)
				{
					if (this._list[j] != null && this._list[j].Equals(value))
					{
						return j;
					}
				}
				return -1;
			}

			// Token: 0x06006CDA RID: 27866 RVA: 0x00178ABD File Offset: 0x00176CBD
			public override void Insert(int index, object obj)
			{
				this._list.Insert(index, obj);
				this._version++;
			}

			// Token: 0x06006CDB RID: 27867 RVA: 0x00178ADC File Offset: 0x00176CDC
			public override void InsertRange(int index, ICollection c)
			{
				if (c == null)
				{
					throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
				}
				if (index < 0 || index > this.Count)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (c.Count > 0)
				{
					ArrayList arrayList = this._list as ArrayList;
					if (arrayList != null)
					{
						arrayList.InsertRange(index, c);
					}
					else
					{
						foreach (object value in c)
						{
							this._list.Insert(index++, value);
						}
					}
					this._version++;
				}
			}

			// Token: 0x06006CDC RID: 27868 RVA: 0x00178B7B File Offset: 0x00176D7B
			public override int LastIndexOf(object value)
			{
				return this.LastIndexOf(value, this._list.Count - 1, this._list.Count);
			}

			// Token: 0x06006CDD RID: 27869 RVA: 0x00178B9C File Offset: 0x00176D9C
			public override int LastIndexOf(object value, int startIndex)
			{
				return this.LastIndexOf(value, startIndex, startIndex + 1);
			}

			// Token: 0x06006CDE RID: 27870 RVA: 0x00178BAC File Offset: 0x00176DAC
			public override int LastIndexOf(object value, int startIndex, int count)
			{
				if (this._list.Count == 0)
				{
					return -1;
				}
				if (startIndex < 0 || startIndex >= this._list.Count)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (count < 0 || count > startIndex + 1)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
				}
				int num = startIndex - count + 1;
				if (value == null)
				{
					for (int i = startIndex; i >= num; i--)
					{
						if (this._list[i] == null)
						{
							return i;
						}
					}
					return -1;
				}
				for (int j = startIndex; j >= num; j--)
				{
					if (this._list[j] != null && this._list[j].Equals(value))
					{
						return j;
					}
				}
				return -1;
			}

			// Token: 0x06006CDF RID: 27871 RVA: 0x00178C68 File Offset: 0x00176E68
			public override void Remove(object value)
			{
				int num = this.IndexOf(value);
				if (num >= 0)
				{
					this.RemoveAt(num);
				}
			}

			// Token: 0x06006CE0 RID: 27872 RVA: 0x00178C88 File Offset: 0x00176E88
			public override void RemoveAt(int index)
			{
				this._list.RemoveAt(index);
				this._version++;
			}

			// Token: 0x06006CE1 RID: 27873 RVA: 0x00178CA4 File Offset: 0x00176EA4
			public override void RemoveRange(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._list.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				if (count > 0)
				{
					this._version++;
				}
				while (count > 0)
				{
					this._list.RemoveAt(index);
					count--;
				}
			}

			// Token: 0x06006CE2 RID: 27874 RVA: 0x00178D24 File Offset: 0x00176F24
			public override void Reverse(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._list.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				int i = index;
				int num = index + count - 1;
				while (i < num)
				{
					object value = this._list[i];
					this._list[i++] = this._list[num];
					this._list[num--] = value;
				}
				this._version++;
			}

			// Token: 0x06006CE3 RID: 27875 RVA: 0x00178DD0 File Offset: 0x00176FD0
			public override void SetRange(int index, ICollection c)
			{
				if (c == null)
				{
					throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
				}
				if (index < 0 || index > this._list.Count - c.Count)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (c.Count > 0)
				{
					foreach (object value in c)
					{
						this._list[index++] = value;
					}
					this._version++;
				}
			}

			// Token: 0x06006CE4 RID: 27876 RVA: 0x00178E64 File Offset: 0x00177064
			public override ArrayList GetRange(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._list.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				return new ArrayList.Range(this, index, count);
			}

			// Token: 0x06006CE5 RID: 27877 RVA: 0x00178EC4 File Offset: 0x001770C4
			public override void Sort(int index, int count, IComparer comparer)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._list.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				object[] array = new object[count];
				this.CopyTo(index, array, 0, count);
				Array.Sort(array, 0, count, comparer);
				for (int i = 0; i < count; i++)
				{
					this._list[i + index] = array[i];
				}
				this._version++;
			}

			// Token: 0x06006CE6 RID: 27878 RVA: 0x00178F60 File Offset: 0x00177160
			public override object[] ToArray()
			{
				object[] array = new object[this.Count];
				this._list.CopyTo(array, 0);
				return array;
			}

			// Token: 0x06006CE7 RID: 27879 RVA: 0x00178F88 File Offset: 0x00177188
			[SecuritySafeCritical]
			public override Array ToArray(Type type)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				Array array = Array.UnsafeCreateInstance(type, this._list.Count);
				this._list.CopyTo(array, 0);
				return array;
			}

			// Token: 0x06006CE8 RID: 27880 RVA: 0x00178FC9 File Offset: 0x001771C9
			public override void TrimToSize()
			{
			}

			// Token: 0x04003543 RID: 13635
			private IList _list;

			// Token: 0x02000D05 RID: 3333
			[Serializable]
			private sealed class IListWrapperEnumWrapper : IEnumerator, ICloneable
			{
				// Token: 0x060071F4 RID: 29172 RVA: 0x00188B1C File Offset: 0x00186D1C
				private IListWrapperEnumWrapper()
				{
				}

				// Token: 0x060071F5 RID: 29173 RVA: 0x00188B24 File Offset: 0x00186D24
				internal IListWrapperEnumWrapper(ArrayList.IListWrapper listWrapper, int startIndex, int count)
				{
					this._en = listWrapper.GetEnumerator();
					this._initialStartIndex = startIndex;
					this._initialCount = count;
					while (startIndex-- > 0 && this._en.MoveNext())
					{
					}
					this._remaining = count;
					this._firstCall = true;
				}

				// Token: 0x060071F6 RID: 29174 RVA: 0x00188B78 File Offset: 0x00186D78
				public object Clone()
				{
					return new ArrayList.IListWrapper.IListWrapperEnumWrapper
					{
						_en = (IEnumerator)((ICloneable)this._en).Clone(),
						_initialStartIndex = this._initialStartIndex,
						_initialCount = this._initialCount,
						_remaining = this._remaining,
						_firstCall = this._firstCall
					};
				}

				// Token: 0x060071F7 RID: 29175 RVA: 0x00188BD8 File Offset: 0x00186DD8
				public bool MoveNext()
				{
					if (this._firstCall)
					{
						this._firstCall = false;
						int remaining = this._remaining;
						this._remaining = remaining - 1;
						return remaining > 0 && this._en.MoveNext();
					}
					if (this._remaining < 0)
					{
						return false;
					}
					bool flag = this._en.MoveNext();
					if (flag)
					{
						int remaining = this._remaining;
						this._remaining = remaining - 1;
						return remaining > 0;
					}
					return false;
				}

				// Token: 0x17001384 RID: 4996
				// (get) Token: 0x060071F8 RID: 29176 RVA: 0x00188C46 File Offset: 0x00186E46
				public object Current
				{
					get
					{
						if (this._firstCall)
						{
							throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
						}
						if (this._remaining < 0)
						{
							throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
						}
						return this._en.Current;
					}
				}

				// Token: 0x060071F9 RID: 29177 RVA: 0x00188C84 File Offset: 0x00186E84
				public void Reset()
				{
					this._en.Reset();
					int initialStartIndex = this._initialStartIndex;
					while (initialStartIndex-- > 0 && this._en.MoveNext())
					{
					}
					this._remaining = this._initialCount;
					this._firstCall = true;
				}

				// Token: 0x04003939 RID: 14649
				private IEnumerator _en;

				// Token: 0x0400393A RID: 14650
				private int _remaining;

				// Token: 0x0400393B RID: 14651
				private int _initialStartIndex;

				// Token: 0x0400393C RID: 14652
				private int _initialCount;

				// Token: 0x0400393D RID: 14653
				private bool _firstCall;
			}
		}

		// Token: 0x02000BA5 RID: 2981
		[Serializable]
		private class SyncArrayList : ArrayList
		{
			// Token: 0x06006CE9 RID: 27881 RVA: 0x00178FCB File Offset: 0x001771CB
			internal SyncArrayList(ArrayList list)
				: base(false)
			{
				this._list = list;
				this._root = list.SyncRoot;
			}

			// Token: 0x1700126E RID: 4718
			// (get) Token: 0x06006CEA RID: 27882 RVA: 0x00178FE8 File Offset: 0x001771E8
			// (set) Token: 0x06006CEB RID: 27883 RVA: 0x00179030 File Offset: 0x00177230
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
				set
				{
					object root = this._root;
					lock (root)
					{
						this._list.Capacity = value;
					}
				}
			}

			// Token: 0x1700126F RID: 4719
			// (get) Token: 0x06006CEC RID: 27884 RVA: 0x00179078 File Offset: 0x00177278
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

			// Token: 0x17001270 RID: 4720
			// (get) Token: 0x06006CED RID: 27885 RVA: 0x001790C0 File Offset: 0x001772C0
			public override bool IsReadOnly
			{
				get
				{
					return this._list.IsReadOnly;
				}
			}

			// Token: 0x17001271 RID: 4721
			// (get) Token: 0x06006CEE RID: 27886 RVA: 0x001790CD File Offset: 0x001772CD
			public override bool IsFixedSize
			{
				get
				{
					return this._list.IsFixedSize;
				}
			}

			// Token: 0x17001272 RID: 4722
			// (get) Token: 0x06006CEF RID: 27887 RVA: 0x001790DA File Offset: 0x001772DA
			public override bool IsSynchronized
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001273 RID: 4723
			public override object this[int index]
			{
				get
				{
					object root = this._root;
					object result;
					lock (root)
					{
						result = this._list[index];
					}
					return result;
				}
				set
				{
					object root = this._root;
					lock (root)
					{
						this._list[index] = value;
					}
				}
			}

			// Token: 0x17001274 RID: 4724
			// (get) Token: 0x06006CF2 RID: 27890 RVA: 0x00179170 File Offset: 0x00177370
			public override object SyncRoot
			{
				get
				{
					return this._root;
				}
			}

			// Token: 0x06006CF3 RID: 27891 RVA: 0x00179178 File Offset: 0x00177378
			public override int Add(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.Add(value);
				}
				return result;
			}

			// Token: 0x06006CF4 RID: 27892 RVA: 0x001791C0 File Offset: 0x001773C0
			public override void AddRange(ICollection c)
			{
				object root = this._root;
				lock (root)
				{
					this._list.AddRange(c);
				}
			}

			// Token: 0x06006CF5 RID: 27893 RVA: 0x00179208 File Offset: 0x00177408
			public override int BinarySearch(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.BinarySearch(value);
				}
				return result;
			}

			// Token: 0x06006CF6 RID: 27894 RVA: 0x00179250 File Offset: 0x00177450
			public override int BinarySearch(object value, IComparer comparer)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.BinarySearch(value, comparer);
				}
				return result;
			}

			// Token: 0x06006CF7 RID: 27895 RVA: 0x0017929C File Offset: 0x0017749C
			public override int BinarySearch(int index, int count, object value, IComparer comparer)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.BinarySearch(index, count, value, comparer);
				}
				return result;
			}

			// Token: 0x06006CF8 RID: 27896 RVA: 0x001792E8 File Offset: 0x001774E8
			public override void Clear()
			{
				object root = this._root;
				lock (root)
				{
					this._list.Clear();
				}
			}

			// Token: 0x06006CF9 RID: 27897 RVA: 0x00179330 File Offset: 0x00177530
			public override object Clone()
			{
				object root = this._root;
				object result;
				lock (root)
				{
					result = new ArrayList.SyncArrayList((ArrayList)this._list.Clone());
				}
				return result;
			}

			// Token: 0x06006CFA RID: 27898 RVA: 0x00179384 File Offset: 0x00177584
			public override bool Contains(object item)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.Contains(item);
				}
				return result;
			}

			// Token: 0x06006CFB RID: 27899 RVA: 0x001793CC File Offset: 0x001775CC
			public override void CopyTo(Array array)
			{
				object root = this._root;
				lock (root)
				{
					this._list.CopyTo(array);
				}
			}

			// Token: 0x06006CFC RID: 27900 RVA: 0x00179414 File Offset: 0x00177614
			public override void CopyTo(Array array, int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.CopyTo(array, index);
				}
			}

			// Token: 0x06006CFD RID: 27901 RVA: 0x0017945C File Offset: 0x0017765C
			public override void CopyTo(int index, Array array, int arrayIndex, int count)
			{
				object root = this._root;
				lock (root)
				{
					this._list.CopyTo(index, array, arrayIndex, count);
				}
			}

			// Token: 0x06006CFE RID: 27902 RVA: 0x001794A8 File Offset: 0x001776A8
			public override IEnumerator GetEnumerator()
			{
				object root = this._root;
				IEnumerator enumerator;
				lock (root)
				{
					enumerator = this._list.GetEnumerator();
				}
				return enumerator;
			}

			// Token: 0x06006CFF RID: 27903 RVA: 0x001794F0 File Offset: 0x001776F0
			public override IEnumerator GetEnumerator(int index, int count)
			{
				object root = this._root;
				IEnumerator enumerator;
				lock (root)
				{
					enumerator = this._list.GetEnumerator(index, count);
				}
				return enumerator;
			}

			// Token: 0x06006D00 RID: 27904 RVA: 0x0017953C File Offset: 0x0017773C
			public override int IndexOf(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOf(value);
				}
				return result;
			}

			// Token: 0x06006D01 RID: 27905 RVA: 0x00179584 File Offset: 0x00177784
			public override int IndexOf(object value, int startIndex)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOf(value, startIndex);
				}
				return result;
			}

			// Token: 0x06006D02 RID: 27906 RVA: 0x001795D0 File Offset: 0x001777D0
			public override int IndexOf(object value, int startIndex, int count)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOf(value, startIndex, count);
				}
				return result;
			}

			// Token: 0x06006D03 RID: 27907 RVA: 0x0017961C File Offset: 0x0017781C
			public override void Insert(int index, object value)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Insert(index, value);
				}
			}

			// Token: 0x06006D04 RID: 27908 RVA: 0x00179664 File Offset: 0x00177864
			public override void InsertRange(int index, ICollection c)
			{
				object root = this._root;
				lock (root)
				{
					this._list.InsertRange(index, c);
				}
			}

			// Token: 0x06006D05 RID: 27909 RVA: 0x001796AC File Offset: 0x001778AC
			public override int LastIndexOf(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.LastIndexOf(value);
				}
				return result;
			}

			// Token: 0x06006D06 RID: 27910 RVA: 0x001796F4 File Offset: 0x001778F4
			public override int LastIndexOf(object value, int startIndex)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.LastIndexOf(value, startIndex);
				}
				return result;
			}

			// Token: 0x06006D07 RID: 27911 RVA: 0x00179740 File Offset: 0x00177940
			public override int LastIndexOf(object value, int startIndex, int count)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.LastIndexOf(value, startIndex, count);
				}
				return result;
			}

			// Token: 0x06006D08 RID: 27912 RVA: 0x0017978C File Offset: 0x0017798C
			public override void Remove(object value)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Remove(value);
				}
			}

			// Token: 0x06006D09 RID: 27913 RVA: 0x001797D4 File Offset: 0x001779D4
			public override void RemoveAt(int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.RemoveAt(index);
				}
			}

			// Token: 0x06006D0A RID: 27914 RVA: 0x0017981C File Offset: 0x00177A1C
			public override void RemoveRange(int index, int count)
			{
				object root = this._root;
				lock (root)
				{
					this._list.RemoveRange(index, count);
				}
			}

			// Token: 0x06006D0B RID: 27915 RVA: 0x00179864 File Offset: 0x00177A64
			public override void Reverse(int index, int count)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Reverse(index, count);
				}
			}

			// Token: 0x06006D0C RID: 27916 RVA: 0x001798AC File Offset: 0x00177AAC
			public override void SetRange(int index, ICollection c)
			{
				object root = this._root;
				lock (root)
				{
					this._list.SetRange(index, c);
				}
			}

			// Token: 0x06006D0D RID: 27917 RVA: 0x001798F4 File Offset: 0x00177AF4
			public override ArrayList GetRange(int index, int count)
			{
				object root = this._root;
				ArrayList range;
				lock (root)
				{
					range = this._list.GetRange(index, count);
				}
				return range;
			}

			// Token: 0x06006D0E RID: 27918 RVA: 0x00179940 File Offset: 0x00177B40
			public override void Sort()
			{
				object root = this._root;
				lock (root)
				{
					this._list.Sort();
				}
			}

			// Token: 0x06006D0F RID: 27919 RVA: 0x00179988 File Offset: 0x00177B88
			public override void Sort(IComparer comparer)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Sort(comparer);
				}
			}

			// Token: 0x06006D10 RID: 27920 RVA: 0x001799D0 File Offset: 0x00177BD0
			public override void Sort(int index, int count, IComparer comparer)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Sort(index, count, comparer);
				}
			}

			// Token: 0x06006D11 RID: 27921 RVA: 0x00179A18 File Offset: 0x00177C18
			public override object[] ToArray()
			{
				object root = this._root;
				object[] result;
				lock (root)
				{
					result = this._list.ToArray();
				}
				return result;
			}

			// Token: 0x06006D12 RID: 27922 RVA: 0x00179A60 File Offset: 0x00177C60
			public override Array ToArray(Type type)
			{
				object root = this._root;
				Array result;
				lock (root)
				{
					result = this._list.ToArray(type);
				}
				return result;
			}

			// Token: 0x06006D13 RID: 27923 RVA: 0x00179AA8 File Offset: 0x00177CA8
			public override void TrimToSize()
			{
				object root = this._root;
				lock (root)
				{
					this._list.TrimToSize();
				}
			}

			// Token: 0x04003544 RID: 13636
			private ArrayList _list;

			// Token: 0x04003545 RID: 13637
			private object _root;
		}

		// Token: 0x02000BA6 RID: 2982
		[Serializable]
		private class SyncIList : IList, ICollection, IEnumerable
		{
			// Token: 0x06006D14 RID: 27924 RVA: 0x00179AF0 File Offset: 0x00177CF0
			internal SyncIList(IList list)
			{
				this._list = list;
				this._root = list.SyncRoot;
			}

			// Token: 0x17001275 RID: 4725
			// (get) Token: 0x06006D15 RID: 27925 RVA: 0x00179B0C File Offset: 0x00177D0C
			public virtual int Count
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

			// Token: 0x17001276 RID: 4726
			// (get) Token: 0x06006D16 RID: 27926 RVA: 0x00179B54 File Offset: 0x00177D54
			public virtual bool IsReadOnly
			{
				get
				{
					return this._list.IsReadOnly;
				}
			}

			// Token: 0x17001277 RID: 4727
			// (get) Token: 0x06006D17 RID: 27927 RVA: 0x00179B61 File Offset: 0x00177D61
			public virtual bool IsFixedSize
			{
				get
				{
					return this._list.IsFixedSize;
				}
			}

			// Token: 0x17001278 RID: 4728
			// (get) Token: 0x06006D18 RID: 27928 RVA: 0x00179B6E File Offset: 0x00177D6E
			public virtual bool IsSynchronized
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001279 RID: 4729
			public virtual object this[int index]
			{
				get
				{
					object root = this._root;
					object result;
					lock (root)
					{
						result = this._list[index];
					}
					return result;
				}
				set
				{
					object root = this._root;
					lock (root)
					{
						this._list[index] = value;
					}
				}
			}

			// Token: 0x1700127A RID: 4730
			// (get) Token: 0x06006D1B RID: 27931 RVA: 0x00179C04 File Offset: 0x00177E04
			public virtual object SyncRoot
			{
				get
				{
					return this._root;
				}
			}

			// Token: 0x06006D1C RID: 27932 RVA: 0x00179C0C File Offset: 0x00177E0C
			public virtual int Add(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.Add(value);
				}
				return result;
			}

			// Token: 0x06006D1D RID: 27933 RVA: 0x00179C54 File Offset: 0x00177E54
			public virtual void Clear()
			{
				object root = this._root;
				lock (root)
				{
					this._list.Clear();
				}
			}

			// Token: 0x06006D1E RID: 27934 RVA: 0x00179C9C File Offset: 0x00177E9C
			public virtual bool Contains(object item)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.Contains(item);
				}
				return result;
			}

			// Token: 0x06006D1F RID: 27935 RVA: 0x00179CE4 File Offset: 0x00177EE4
			public virtual void CopyTo(Array array, int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.CopyTo(array, index);
				}
			}

			// Token: 0x06006D20 RID: 27936 RVA: 0x00179D2C File Offset: 0x00177F2C
			public virtual IEnumerator GetEnumerator()
			{
				object root = this._root;
				IEnumerator enumerator;
				lock (root)
				{
					enumerator = this._list.GetEnumerator();
				}
				return enumerator;
			}

			// Token: 0x06006D21 RID: 27937 RVA: 0x00179D74 File Offset: 0x00177F74
			public virtual int IndexOf(object value)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOf(value);
				}
				return result;
			}

			// Token: 0x06006D22 RID: 27938 RVA: 0x00179DBC File Offset: 0x00177FBC
			public virtual void Insert(int index, object value)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Insert(index, value);
				}
			}

			// Token: 0x06006D23 RID: 27939 RVA: 0x00179E04 File Offset: 0x00178004
			public virtual void Remove(object value)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Remove(value);
				}
			}

			// Token: 0x06006D24 RID: 27940 RVA: 0x00179E4C File Offset: 0x0017804C
			public virtual void RemoveAt(int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.RemoveAt(index);
				}
			}

			// Token: 0x04003546 RID: 13638
			private IList _list;

			// Token: 0x04003547 RID: 13639
			private object _root;
		}

		// Token: 0x02000BA7 RID: 2983
		[Serializable]
		private class FixedSizeList : IList, ICollection, IEnumerable
		{
			// Token: 0x06006D25 RID: 27941 RVA: 0x00179E94 File Offset: 0x00178094
			internal FixedSizeList(IList l)
			{
				this._list = l;
			}

			// Token: 0x1700127B RID: 4731
			// (get) Token: 0x06006D26 RID: 27942 RVA: 0x00179EA3 File Offset: 0x001780A3
			public virtual int Count
			{
				get
				{
					return this._list.Count;
				}
			}

			// Token: 0x1700127C RID: 4732
			// (get) Token: 0x06006D27 RID: 27943 RVA: 0x00179EB0 File Offset: 0x001780B0
			public virtual bool IsReadOnly
			{
				get
				{
					return this._list.IsReadOnly;
				}
			}

			// Token: 0x1700127D RID: 4733
			// (get) Token: 0x06006D28 RID: 27944 RVA: 0x00179EBD File Offset: 0x001780BD
			public virtual bool IsFixedSize
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700127E RID: 4734
			// (get) Token: 0x06006D29 RID: 27945 RVA: 0x00179EC0 File Offset: 0x001780C0
			public virtual bool IsSynchronized
			{
				get
				{
					return this._list.IsSynchronized;
				}
			}

			// Token: 0x1700127F RID: 4735
			public virtual object this[int index]
			{
				get
				{
					return this._list[index];
				}
				set
				{
					this._list[index] = value;
				}
			}

			// Token: 0x17001280 RID: 4736
			// (get) Token: 0x06006D2C RID: 27948 RVA: 0x00179EEA File Offset: 0x001780EA
			public virtual object SyncRoot
			{
				get
				{
					return this._list.SyncRoot;
				}
			}

			// Token: 0x06006D2D RID: 27949 RVA: 0x00179EF7 File Offset: 0x001780F7
			public virtual int Add(object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D2E RID: 27950 RVA: 0x00179F08 File Offset: 0x00178108
			public virtual void Clear()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D2F RID: 27951 RVA: 0x00179F19 File Offset: 0x00178119
			public virtual bool Contains(object obj)
			{
				return this._list.Contains(obj);
			}

			// Token: 0x06006D30 RID: 27952 RVA: 0x00179F27 File Offset: 0x00178127
			public virtual void CopyTo(Array array, int index)
			{
				this._list.CopyTo(array, index);
			}

			// Token: 0x06006D31 RID: 27953 RVA: 0x00179F36 File Offset: 0x00178136
			public virtual IEnumerator GetEnumerator()
			{
				return this._list.GetEnumerator();
			}

			// Token: 0x06006D32 RID: 27954 RVA: 0x00179F43 File Offset: 0x00178143
			public virtual int IndexOf(object value)
			{
				return this._list.IndexOf(value);
			}

			// Token: 0x06006D33 RID: 27955 RVA: 0x00179F51 File Offset: 0x00178151
			public virtual void Insert(int index, object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D34 RID: 27956 RVA: 0x00179F62 File Offset: 0x00178162
			public virtual void Remove(object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D35 RID: 27957 RVA: 0x00179F73 File Offset: 0x00178173
			public virtual void RemoveAt(int index)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x04003548 RID: 13640
			private IList _list;
		}

		// Token: 0x02000BA8 RID: 2984
		[Serializable]
		private class FixedSizeArrayList : ArrayList
		{
			// Token: 0x06006D36 RID: 27958 RVA: 0x00179F84 File Offset: 0x00178184
			internal FixedSizeArrayList(ArrayList l)
			{
				this._list = l;
				this._version = this._list._version;
			}

			// Token: 0x17001281 RID: 4737
			// (get) Token: 0x06006D37 RID: 27959 RVA: 0x00179FA4 File Offset: 0x001781A4
			public override int Count
			{
				get
				{
					return this._list.Count;
				}
			}

			// Token: 0x17001282 RID: 4738
			// (get) Token: 0x06006D38 RID: 27960 RVA: 0x00179FB1 File Offset: 0x001781B1
			public override bool IsReadOnly
			{
				get
				{
					return this._list.IsReadOnly;
				}
			}

			// Token: 0x17001283 RID: 4739
			// (get) Token: 0x06006D39 RID: 27961 RVA: 0x00179FBE File Offset: 0x001781BE
			public override bool IsFixedSize
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001284 RID: 4740
			// (get) Token: 0x06006D3A RID: 27962 RVA: 0x00179FC1 File Offset: 0x001781C1
			public override bool IsSynchronized
			{
				get
				{
					return this._list.IsSynchronized;
				}
			}

			// Token: 0x17001285 RID: 4741
			public override object this[int index]
			{
				get
				{
					return this._list[index];
				}
				set
				{
					this._list[index] = value;
					this._version = this._list._version;
				}
			}

			// Token: 0x17001286 RID: 4742
			// (get) Token: 0x06006D3D RID: 27965 RVA: 0x00179FFC File Offset: 0x001781FC
			public override object SyncRoot
			{
				get
				{
					return this._list.SyncRoot;
				}
			}

			// Token: 0x06006D3E RID: 27966 RVA: 0x0017A009 File Offset: 0x00178209
			public override int Add(object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D3F RID: 27967 RVA: 0x0017A01A File Offset: 0x0017821A
			public override void AddRange(ICollection c)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D40 RID: 27968 RVA: 0x0017A02B File Offset: 0x0017822B
			public override int BinarySearch(int index, int count, object value, IComparer comparer)
			{
				return this._list.BinarySearch(index, count, value, comparer);
			}

			// Token: 0x17001287 RID: 4743
			// (get) Token: 0x06006D41 RID: 27969 RVA: 0x0017A03D File Offset: 0x0017823D
			// (set) Token: 0x06006D42 RID: 27970 RVA: 0x0017A04A File Offset: 0x0017824A
			public override int Capacity
			{
				get
				{
					return this._list.Capacity;
				}
				set
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
				}
			}

			// Token: 0x06006D43 RID: 27971 RVA: 0x0017A05B File Offset: 0x0017825B
			public override void Clear()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D44 RID: 27972 RVA: 0x0017A06C File Offset: 0x0017826C
			public override object Clone()
			{
				return new ArrayList.FixedSizeArrayList(this._list)
				{
					_list = (ArrayList)this._list.Clone()
				};
			}

			// Token: 0x06006D45 RID: 27973 RVA: 0x0017A09C File Offset: 0x0017829C
			public override bool Contains(object obj)
			{
				return this._list.Contains(obj);
			}

			// Token: 0x06006D46 RID: 27974 RVA: 0x0017A0AA File Offset: 0x001782AA
			public override void CopyTo(Array array, int index)
			{
				this._list.CopyTo(array, index);
			}

			// Token: 0x06006D47 RID: 27975 RVA: 0x0017A0B9 File Offset: 0x001782B9
			public override void CopyTo(int index, Array array, int arrayIndex, int count)
			{
				this._list.CopyTo(index, array, arrayIndex, count);
			}

			// Token: 0x06006D48 RID: 27976 RVA: 0x0017A0CB File Offset: 0x001782CB
			public override IEnumerator GetEnumerator()
			{
				return this._list.GetEnumerator();
			}

			// Token: 0x06006D49 RID: 27977 RVA: 0x0017A0D8 File Offset: 0x001782D8
			public override IEnumerator GetEnumerator(int index, int count)
			{
				return this._list.GetEnumerator(index, count);
			}

			// Token: 0x06006D4A RID: 27978 RVA: 0x0017A0E7 File Offset: 0x001782E7
			public override int IndexOf(object value)
			{
				return this._list.IndexOf(value);
			}

			// Token: 0x06006D4B RID: 27979 RVA: 0x0017A0F5 File Offset: 0x001782F5
			public override int IndexOf(object value, int startIndex)
			{
				return this._list.IndexOf(value, startIndex);
			}

			// Token: 0x06006D4C RID: 27980 RVA: 0x0017A104 File Offset: 0x00178304
			public override int IndexOf(object value, int startIndex, int count)
			{
				return this._list.IndexOf(value, startIndex, count);
			}

			// Token: 0x06006D4D RID: 27981 RVA: 0x0017A114 File Offset: 0x00178314
			public override void Insert(int index, object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D4E RID: 27982 RVA: 0x0017A125 File Offset: 0x00178325
			public override void InsertRange(int index, ICollection c)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D4F RID: 27983 RVA: 0x0017A136 File Offset: 0x00178336
			public override int LastIndexOf(object value)
			{
				return this._list.LastIndexOf(value);
			}

			// Token: 0x06006D50 RID: 27984 RVA: 0x0017A144 File Offset: 0x00178344
			public override int LastIndexOf(object value, int startIndex)
			{
				return this._list.LastIndexOf(value, startIndex);
			}

			// Token: 0x06006D51 RID: 27985 RVA: 0x0017A153 File Offset: 0x00178353
			public override int LastIndexOf(object value, int startIndex, int count)
			{
				return this._list.LastIndexOf(value, startIndex, count);
			}

			// Token: 0x06006D52 RID: 27986 RVA: 0x0017A163 File Offset: 0x00178363
			public override void Remove(object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D53 RID: 27987 RVA: 0x0017A174 File Offset: 0x00178374
			public override void RemoveAt(int index)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D54 RID: 27988 RVA: 0x0017A185 File Offset: 0x00178385
			public override void RemoveRange(int index, int count)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x06006D55 RID: 27989 RVA: 0x0017A196 File Offset: 0x00178396
			public override void SetRange(int index, ICollection c)
			{
				this._list.SetRange(index, c);
				this._version = this._list._version;
			}

			// Token: 0x06006D56 RID: 27990 RVA: 0x0017A1B8 File Offset: 0x001783B8
			public override ArrayList GetRange(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				return new ArrayList.Range(this, index, count);
			}

			// Token: 0x06006D57 RID: 27991 RVA: 0x0017A210 File Offset: 0x00178410
			public override void Reverse(int index, int count)
			{
				this._list.Reverse(index, count);
				this._version = this._list._version;
			}

			// Token: 0x06006D58 RID: 27992 RVA: 0x0017A230 File Offset: 0x00178430
			public override void Sort(int index, int count, IComparer comparer)
			{
				this._list.Sort(index, count, comparer);
				this._version = this._list._version;
			}

			// Token: 0x06006D59 RID: 27993 RVA: 0x0017A251 File Offset: 0x00178451
			public override object[] ToArray()
			{
				return this._list.ToArray();
			}

			// Token: 0x06006D5A RID: 27994 RVA: 0x0017A25E File Offset: 0x0017845E
			public override Array ToArray(Type type)
			{
				return this._list.ToArray(type);
			}

			// Token: 0x06006D5B RID: 27995 RVA: 0x0017A26C File Offset: 0x0017846C
			public override void TrimToSize()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
			}

			// Token: 0x04003549 RID: 13641
			private ArrayList _list;
		}

		// Token: 0x02000BA9 RID: 2985
		[Serializable]
		private class ReadOnlyList : IList, ICollection, IEnumerable
		{
			// Token: 0x06006D5C RID: 27996 RVA: 0x0017A27D File Offset: 0x0017847D
			internal ReadOnlyList(IList l)
			{
				this._list = l;
			}

			// Token: 0x17001288 RID: 4744
			// (get) Token: 0x06006D5D RID: 27997 RVA: 0x0017A28C File Offset: 0x0017848C
			public virtual int Count
			{
				get
				{
					return this._list.Count;
				}
			}

			// Token: 0x17001289 RID: 4745
			// (get) Token: 0x06006D5E RID: 27998 RVA: 0x0017A299 File Offset: 0x00178499
			public virtual bool IsReadOnly
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700128A RID: 4746
			// (get) Token: 0x06006D5F RID: 27999 RVA: 0x0017A29C File Offset: 0x0017849C
			public virtual bool IsFixedSize
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700128B RID: 4747
			// (get) Token: 0x06006D60 RID: 28000 RVA: 0x0017A29F File Offset: 0x0017849F
			public virtual bool IsSynchronized
			{
				get
				{
					return this._list.IsSynchronized;
				}
			}

			// Token: 0x1700128C RID: 4748
			public virtual object this[int index]
			{
				get
				{
					return this._list[index];
				}
				set
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
				}
			}

			// Token: 0x1700128D RID: 4749
			// (get) Token: 0x06006D63 RID: 28003 RVA: 0x0017A2CB File Offset: 0x001784CB
			public virtual object SyncRoot
			{
				get
				{
					return this._list.SyncRoot;
				}
			}

			// Token: 0x06006D64 RID: 28004 RVA: 0x0017A2D8 File Offset: 0x001784D8
			public virtual int Add(object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D65 RID: 28005 RVA: 0x0017A2E9 File Offset: 0x001784E9
			public virtual void Clear()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D66 RID: 28006 RVA: 0x0017A2FA File Offset: 0x001784FA
			public virtual bool Contains(object obj)
			{
				return this._list.Contains(obj);
			}

			// Token: 0x06006D67 RID: 28007 RVA: 0x0017A308 File Offset: 0x00178508
			public virtual void CopyTo(Array array, int index)
			{
				this._list.CopyTo(array, index);
			}

			// Token: 0x06006D68 RID: 28008 RVA: 0x0017A317 File Offset: 0x00178517
			public virtual IEnumerator GetEnumerator()
			{
				return this._list.GetEnumerator();
			}

			// Token: 0x06006D69 RID: 28009 RVA: 0x0017A324 File Offset: 0x00178524
			public virtual int IndexOf(object value)
			{
				return this._list.IndexOf(value);
			}

			// Token: 0x06006D6A RID: 28010 RVA: 0x0017A332 File Offset: 0x00178532
			public virtual void Insert(int index, object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D6B RID: 28011 RVA: 0x0017A343 File Offset: 0x00178543
			public virtual void Remove(object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D6C RID: 28012 RVA: 0x0017A354 File Offset: 0x00178554
			public virtual void RemoveAt(int index)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x0400354A RID: 13642
			private IList _list;
		}

		// Token: 0x02000BAA RID: 2986
		[Serializable]
		private class ReadOnlyArrayList : ArrayList
		{
			// Token: 0x06006D6D RID: 28013 RVA: 0x0017A365 File Offset: 0x00178565
			internal ReadOnlyArrayList(ArrayList l)
			{
				this._list = l;
			}

			// Token: 0x1700128E RID: 4750
			// (get) Token: 0x06006D6E RID: 28014 RVA: 0x0017A374 File Offset: 0x00178574
			public override int Count
			{
				get
				{
					return this._list.Count;
				}
			}

			// Token: 0x1700128F RID: 4751
			// (get) Token: 0x06006D6F RID: 28015 RVA: 0x0017A381 File Offset: 0x00178581
			public override bool IsReadOnly
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001290 RID: 4752
			// (get) Token: 0x06006D70 RID: 28016 RVA: 0x0017A384 File Offset: 0x00178584
			public override bool IsFixedSize
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001291 RID: 4753
			// (get) Token: 0x06006D71 RID: 28017 RVA: 0x0017A387 File Offset: 0x00178587
			public override bool IsSynchronized
			{
				get
				{
					return this._list.IsSynchronized;
				}
			}

			// Token: 0x17001292 RID: 4754
			public override object this[int index]
			{
				get
				{
					return this._list[index];
				}
				set
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
				}
			}

			// Token: 0x17001293 RID: 4755
			// (get) Token: 0x06006D74 RID: 28020 RVA: 0x0017A3B3 File Offset: 0x001785B3
			public override object SyncRoot
			{
				get
				{
					return this._list.SyncRoot;
				}
			}

			// Token: 0x06006D75 RID: 28021 RVA: 0x0017A3C0 File Offset: 0x001785C0
			public override int Add(object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D76 RID: 28022 RVA: 0x0017A3D1 File Offset: 0x001785D1
			public override void AddRange(ICollection c)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D77 RID: 28023 RVA: 0x0017A3E2 File Offset: 0x001785E2
			public override int BinarySearch(int index, int count, object value, IComparer comparer)
			{
				return this._list.BinarySearch(index, count, value, comparer);
			}

			// Token: 0x17001294 RID: 4756
			// (get) Token: 0x06006D78 RID: 28024 RVA: 0x0017A3F4 File Offset: 0x001785F4
			// (set) Token: 0x06006D79 RID: 28025 RVA: 0x0017A401 File Offset: 0x00178601
			public override int Capacity
			{
				get
				{
					return this._list.Capacity;
				}
				set
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
				}
			}

			// Token: 0x06006D7A RID: 28026 RVA: 0x0017A412 File Offset: 0x00178612
			public override void Clear()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D7B RID: 28027 RVA: 0x0017A424 File Offset: 0x00178624
			public override object Clone()
			{
				return new ArrayList.ReadOnlyArrayList(this._list)
				{
					_list = (ArrayList)this._list.Clone()
				};
			}

			// Token: 0x06006D7C RID: 28028 RVA: 0x0017A454 File Offset: 0x00178654
			public override bool Contains(object obj)
			{
				return this._list.Contains(obj);
			}

			// Token: 0x06006D7D RID: 28029 RVA: 0x0017A462 File Offset: 0x00178662
			public override void CopyTo(Array array, int index)
			{
				this._list.CopyTo(array, index);
			}

			// Token: 0x06006D7E RID: 28030 RVA: 0x0017A471 File Offset: 0x00178671
			public override void CopyTo(int index, Array array, int arrayIndex, int count)
			{
				this._list.CopyTo(index, array, arrayIndex, count);
			}

			// Token: 0x06006D7F RID: 28031 RVA: 0x0017A483 File Offset: 0x00178683
			public override IEnumerator GetEnumerator()
			{
				return this._list.GetEnumerator();
			}

			// Token: 0x06006D80 RID: 28032 RVA: 0x0017A490 File Offset: 0x00178690
			public override IEnumerator GetEnumerator(int index, int count)
			{
				return this._list.GetEnumerator(index, count);
			}

			// Token: 0x06006D81 RID: 28033 RVA: 0x0017A49F File Offset: 0x0017869F
			public override int IndexOf(object value)
			{
				return this._list.IndexOf(value);
			}

			// Token: 0x06006D82 RID: 28034 RVA: 0x0017A4AD File Offset: 0x001786AD
			public override int IndexOf(object value, int startIndex)
			{
				return this._list.IndexOf(value, startIndex);
			}

			// Token: 0x06006D83 RID: 28035 RVA: 0x0017A4BC File Offset: 0x001786BC
			public override int IndexOf(object value, int startIndex, int count)
			{
				return this._list.IndexOf(value, startIndex, count);
			}

			// Token: 0x06006D84 RID: 28036 RVA: 0x0017A4CC File Offset: 0x001786CC
			public override void Insert(int index, object obj)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D85 RID: 28037 RVA: 0x0017A4DD File Offset: 0x001786DD
			public override void InsertRange(int index, ICollection c)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D86 RID: 28038 RVA: 0x0017A4EE File Offset: 0x001786EE
			public override int LastIndexOf(object value)
			{
				return this._list.LastIndexOf(value);
			}

			// Token: 0x06006D87 RID: 28039 RVA: 0x0017A4FC File Offset: 0x001786FC
			public override int LastIndexOf(object value, int startIndex)
			{
				return this._list.LastIndexOf(value, startIndex);
			}

			// Token: 0x06006D88 RID: 28040 RVA: 0x0017A50B File Offset: 0x0017870B
			public override int LastIndexOf(object value, int startIndex, int count)
			{
				return this._list.LastIndexOf(value, startIndex, count);
			}

			// Token: 0x06006D89 RID: 28041 RVA: 0x0017A51B File Offset: 0x0017871B
			public override void Remove(object value)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D8A RID: 28042 RVA: 0x0017A52C File Offset: 0x0017872C
			public override void RemoveAt(int index)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D8B RID: 28043 RVA: 0x0017A53D File Offset: 0x0017873D
			public override void RemoveRange(int index, int count)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D8C RID: 28044 RVA: 0x0017A54E File Offset: 0x0017874E
			public override void SetRange(int index, ICollection c)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D8D RID: 28045 RVA: 0x0017A560 File Offset: 0x00178760
			public override ArrayList GetRange(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this.Count - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				return new ArrayList.Range(this, index, count);
			}

			// Token: 0x06006D8E RID: 28046 RVA: 0x0017A5B8 File Offset: 0x001787B8
			public override void Reverse(int index, int count)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D8F RID: 28047 RVA: 0x0017A5C9 File Offset: 0x001787C9
			public override void Sort(int index, int count, IComparer comparer)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x06006D90 RID: 28048 RVA: 0x0017A5DA File Offset: 0x001787DA
			public override object[] ToArray()
			{
				return this._list.ToArray();
			}

			// Token: 0x06006D91 RID: 28049 RVA: 0x0017A5E7 File Offset: 0x001787E7
			public override Array ToArray(Type type)
			{
				return this._list.ToArray(type);
			}

			// Token: 0x06006D92 RID: 28050 RVA: 0x0017A5F5 File Offset: 0x001787F5
			public override void TrimToSize()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
			}

			// Token: 0x0400354B RID: 13643
			private ArrayList _list;
		}

		// Token: 0x02000BAB RID: 2987
		[Serializable]
		private sealed class ArrayListEnumerator : IEnumerator, ICloneable
		{
			// Token: 0x06006D93 RID: 28051 RVA: 0x0017A606 File Offset: 0x00178806
			internal ArrayListEnumerator(ArrayList list, int index, int count)
			{
				this.list = list;
				this.startIndex = index;
				this.index = index - 1;
				this.endIndex = this.index + count;
				this.version = list._version;
				this.currentElement = null;
			}

			// Token: 0x06006D94 RID: 28052 RVA: 0x0017A646 File Offset: 0x00178846
			public object Clone()
			{
				return base.MemberwiseClone();
			}

			// Token: 0x06006D95 RID: 28053 RVA: 0x0017A650 File Offset: 0x00178850
			public bool MoveNext()
			{
				if (this.version != this.list._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				if (this.index < this.endIndex)
				{
					ArrayList arrayList = this.list;
					int num = this.index + 1;
					this.index = num;
					this.currentElement = arrayList[num];
					return true;
				}
				this.index = this.endIndex + 1;
				return false;
			}

			// Token: 0x17001295 RID: 4757
			// (get) Token: 0x06006D96 RID: 28054 RVA: 0x0017A6C4 File Offset: 0x001788C4
			public object Current
			{
				get
				{
					if (this.index < this.startIndex)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
					}
					if (this.index > this.endIndex)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
					}
					return this.currentElement;
				}
			}

			// Token: 0x06006D97 RID: 28055 RVA: 0x0017A713 File Offset: 0x00178913
			public void Reset()
			{
				if (this.version != this.list._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this.index = this.startIndex - 1;
			}

			// Token: 0x0400354C RID: 13644
			private ArrayList list;

			// Token: 0x0400354D RID: 13645
			private int index;

			// Token: 0x0400354E RID: 13646
			private int endIndex;

			// Token: 0x0400354F RID: 13647
			private int version;

			// Token: 0x04003550 RID: 13648
			private object currentElement;

			// Token: 0x04003551 RID: 13649
			private int startIndex;
		}

		// Token: 0x02000BAC RID: 2988
		[Serializable]
		private class Range : ArrayList
		{
			// Token: 0x06006D98 RID: 28056 RVA: 0x0017A746 File Offset: 0x00178946
			internal Range(ArrayList list, int index, int count)
				: base(false)
			{
				this._baseList = list;
				this._baseIndex = index;
				this._baseSize = count;
				this._baseVersion = list._version;
				this._version = list._version;
			}

			// Token: 0x06006D99 RID: 28057 RVA: 0x0017A77C File Offset: 0x0017897C
			private void InternalUpdateRange()
			{
				if (this._baseVersion != this._baseList._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnderlyingArrayListChanged"));
				}
			}

			// Token: 0x06006D9A RID: 28058 RVA: 0x0017A7A1 File Offset: 0x001789A1
			private void InternalUpdateVersion()
			{
				this._baseVersion++;
				this._version++;
			}

			// Token: 0x06006D9B RID: 28059 RVA: 0x0017A7C0 File Offset: 0x001789C0
			public override int Add(object value)
			{
				this.InternalUpdateRange();
				this._baseList.Insert(this._baseIndex + this._baseSize, value);
				this.InternalUpdateVersion();
				int baseSize = this._baseSize;
				this._baseSize = baseSize + 1;
				return baseSize;
			}

			// Token: 0x06006D9C RID: 28060 RVA: 0x0017A804 File Offset: 0x00178A04
			public override void AddRange(ICollection c)
			{
				if (c == null)
				{
					throw new ArgumentNullException("c");
				}
				this.InternalUpdateRange();
				int count = c.Count;
				if (count > 0)
				{
					this._baseList.InsertRange(this._baseIndex + this._baseSize, c);
					this.InternalUpdateVersion();
					this._baseSize += count;
				}
			}

			// Token: 0x06006D9D RID: 28061 RVA: 0x0017A860 File Offset: 0x00178A60
			public override int BinarySearch(int index, int count, object value, IComparer comparer)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				int num = this._baseList.BinarySearch(this._baseIndex + index, count, value, comparer);
				if (num >= 0)
				{
					return num - this._baseIndex;
				}
				return num + this._baseIndex;
			}

			// Token: 0x17001296 RID: 4758
			// (get) Token: 0x06006D9E RID: 28062 RVA: 0x0017A8E3 File Offset: 0x00178AE3
			// (set) Token: 0x06006D9F RID: 28063 RVA: 0x0017A8F0 File Offset: 0x00178AF0
			public override int Capacity
			{
				get
				{
					return this._baseList.Capacity;
				}
				set
				{
					if (value < this.Count)
					{
						throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
					}
				}
			}

			// Token: 0x06006DA0 RID: 28064 RVA: 0x0017A910 File Offset: 0x00178B10
			public override void Clear()
			{
				this.InternalUpdateRange();
				if (this._baseSize != 0)
				{
					this._baseList.RemoveRange(this._baseIndex, this._baseSize);
					this.InternalUpdateVersion();
					this._baseSize = 0;
				}
			}

			// Token: 0x06006DA1 RID: 28065 RVA: 0x0017A944 File Offset: 0x00178B44
			public override object Clone()
			{
				this.InternalUpdateRange();
				return new ArrayList.Range(this._baseList, this._baseIndex, this._baseSize)
				{
					_baseList = (ArrayList)this._baseList.Clone()
				};
			}

			// Token: 0x06006DA2 RID: 28066 RVA: 0x0017A988 File Offset: 0x00178B88
			public override bool Contains(object item)
			{
				this.InternalUpdateRange();
				if (item == null)
				{
					for (int i = 0; i < this._baseSize; i++)
					{
						if (this._baseList[this._baseIndex + i] == null)
						{
							return true;
						}
					}
					return false;
				}
				for (int j = 0; j < this._baseSize; j++)
				{
					if (this._baseList[this._baseIndex + j] != null && this._baseList[this._baseIndex + j].Equals(item))
					{
						return true;
					}
				}
				return false;
			}

			// Token: 0x06006DA3 RID: 28067 RVA: 0x0017AA0C File Offset: 0x00178C0C
			public override void CopyTo(Array array, int index)
			{
				if (array == null)
				{
					throw new ArgumentNullException("array");
				}
				if (array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				if (index < 0)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (array.Length - index < this._baseSize)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				this._baseList.CopyTo(this._baseIndex, array, index, this._baseSize);
			}

			// Token: 0x06006DA4 RID: 28068 RVA: 0x0017AA98 File Offset: 0x00178C98
			public override void CopyTo(int index, Array array, int arrayIndex, int count)
			{
				if (array == null)
				{
					throw new ArgumentNullException("array");
				}
				if (array.Rank != 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
				}
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (array.Length - arrayIndex < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				this._baseList.CopyTo(this._baseIndex + index, array, arrayIndex, count);
			}

			// Token: 0x17001297 RID: 4759
			// (get) Token: 0x06006DA5 RID: 28069 RVA: 0x0017AB4A File Offset: 0x00178D4A
			public override int Count
			{
				get
				{
					this.InternalUpdateRange();
					return this._baseSize;
				}
			}

			// Token: 0x17001298 RID: 4760
			// (get) Token: 0x06006DA6 RID: 28070 RVA: 0x0017AB58 File Offset: 0x00178D58
			public override bool IsReadOnly
			{
				get
				{
					return this._baseList.IsReadOnly;
				}
			}

			// Token: 0x17001299 RID: 4761
			// (get) Token: 0x06006DA7 RID: 28071 RVA: 0x0017AB65 File Offset: 0x00178D65
			public override bool IsFixedSize
			{
				get
				{
					return this._baseList.IsFixedSize;
				}
			}

			// Token: 0x1700129A RID: 4762
			// (get) Token: 0x06006DA8 RID: 28072 RVA: 0x0017AB72 File Offset: 0x00178D72
			public override bool IsSynchronized
			{
				get
				{
					return this._baseList.IsSynchronized;
				}
			}

			// Token: 0x06006DA9 RID: 28073 RVA: 0x0017AB7F File Offset: 0x00178D7F
			public override IEnumerator GetEnumerator()
			{
				return this.GetEnumerator(0, this._baseSize);
			}

			// Token: 0x06006DAA RID: 28074 RVA: 0x0017AB90 File Offset: 0x00178D90
			public override IEnumerator GetEnumerator(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				return this._baseList.GetEnumerator(this._baseIndex + index, count);
			}

			// Token: 0x06006DAB RID: 28075 RVA: 0x0017ABFC File Offset: 0x00178DFC
			public override ArrayList GetRange(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				return new ArrayList.Range(this, index, count);
			}

			// Token: 0x1700129B RID: 4763
			// (get) Token: 0x06006DAC RID: 28076 RVA: 0x0017AC5A File Offset: 0x00178E5A
			public override object SyncRoot
			{
				get
				{
					return this._baseList.SyncRoot;
				}
			}

			// Token: 0x06006DAD RID: 28077 RVA: 0x0017AC68 File Offset: 0x00178E68
			public override int IndexOf(object value)
			{
				this.InternalUpdateRange();
				int num = this._baseList.IndexOf(value, this._baseIndex, this._baseSize);
				if (num >= 0)
				{
					return num - this._baseIndex;
				}
				return -1;
			}

			// Token: 0x06006DAE RID: 28078 RVA: 0x0017ACA4 File Offset: 0x00178EA4
			public override int IndexOf(object value, int startIndex)
			{
				if (startIndex < 0)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (startIndex > this._baseSize)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				this.InternalUpdateRange();
				int num = this._baseList.IndexOf(value, this._baseIndex + startIndex, this._baseSize - startIndex);
				if (num >= 0)
				{
					return num - this._baseIndex;
				}
				return -1;
			}

			// Token: 0x06006DAF RID: 28079 RVA: 0x0017AD1C File Offset: 0x00178F1C
			public override int IndexOf(object value, int startIndex, int count)
			{
				if (startIndex < 0 || startIndex > this._baseSize)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (count < 0 || startIndex > this._baseSize - count)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
				}
				this.InternalUpdateRange();
				int num = this._baseList.IndexOf(value, this._baseIndex + startIndex, count);
				if (num >= 0)
				{
					return num - this._baseIndex;
				}
				return -1;
			}

			// Token: 0x06006DB0 RID: 28080 RVA: 0x0017AD9C File Offset: 0x00178F9C
			public override void Insert(int index, object value)
			{
				if (index < 0 || index > this._baseSize)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				this.InternalUpdateRange();
				this._baseList.Insert(this._baseIndex + index, value);
				this.InternalUpdateVersion();
				this._baseSize++;
			}

			// Token: 0x06006DB1 RID: 28081 RVA: 0x0017ADFC File Offset: 0x00178FFC
			public override void InsertRange(int index, ICollection c)
			{
				if (index < 0 || index > this._baseSize)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (c == null)
				{
					throw new ArgumentNullException("c");
				}
				this.InternalUpdateRange();
				int count = c.Count;
				if (count > 0)
				{
					this._baseList.InsertRange(this._baseIndex + index, c);
					this._baseSize += count;
					this.InternalUpdateVersion();
				}
			}

			// Token: 0x06006DB2 RID: 28082 RVA: 0x0017AE74 File Offset: 0x00179074
			public override int LastIndexOf(object value)
			{
				this.InternalUpdateRange();
				int num = this._baseList.LastIndexOf(value, this._baseIndex + this._baseSize - 1, this._baseSize);
				if (num >= 0)
				{
					return num - this._baseIndex;
				}
				return -1;
			}

			// Token: 0x06006DB3 RID: 28083 RVA: 0x0017AEB7 File Offset: 0x001790B7
			public override int LastIndexOf(object value, int startIndex)
			{
				return this.LastIndexOf(value, startIndex, startIndex + 1);
			}

			// Token: 0x06006DB4 RID: 28084 RVA: 0x0017AEC4 File Offset: 0x001790C4
			public override int LastIndexOf(object value, int startIndex, int count)
			{
				this.InternalUpdateRange();
				if (this._baseSize == 0)
				{
					return -1;
				}
				if (startIndex >= this._baseSize)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (startIndex < 0)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				int num = this._baseList.LastIndexOf(value, this._baseIndex + startIndex, count);
				if (num >= 0)
				{
					return num - this._baseIndex;
				}
				return -1;
			}

			// Token: 0x06006DB5 RID: 28085 RVA: 0x0017AF3C File Offset: 0x0017913C
			public override void RemoveAt(int index)
			{
				if (index < 0 || index >= this._baseSize)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				this.InternalUpdateRange();
				this._baseList.RemoveAt(this._baseIndex + index);
				this.InternalUpdateVersion();
				this._baseSize--;
			}

			// Token: 0x06006DB6 RID: 28086 RVA: 0x0017AF98 File Offset: 0x00179198
			public override void RemoveRange(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				if (count > 0)
				{
					this._baseList.RemoveRange(this._baseIndex + index, count);
					this.InternalUpdateVersion();
					this._baseSize -= count;
				}
			}

			// Token: 0x06006DB7 RID: 28087 RVA: 0x0017B01C File Offset: 0x0017921C
			public override void Reverse(int index, int count)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				this._baseList.Reverse(this._baseIndex + index, count);
				this.InternalUpdateVersion();
			}

			// Token: 0x06006DB8 RID: 28088 RVA: 0x0017B08C File Offset: 0x0017928C
			public override void SetRange(int index, ICollection c)
			{
				this.InternalUpdateRange();
				if (index < 0 || index >= this._baseSize)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				this._baseList.SetRange(this._baseIndex + index, c);
				if (c.Count > 0)
				{
					this.InternalUpdateVersion();
				}
			}

			// Token: 0x06006DB9 RID: 28089 RVA: 0x0017B0E4 File Offset: 0x001792E4
			public override void Sort(int index, int count, IComparer comparer)
			{
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (this._baseSize - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				this.InternalUpdateRange();
				this._baseList.Sort(this._baseIndex + index, count, comparer);
				this.InternalUpdateVersion();
			}

			// Token: 0x1700129C RID: 4764
			public override object this[int index]
			{
				get
				{
					this.InternalUpdateRange();
					if (index < 0 || index >= this._baseSize)
					{
						throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
					}
					return this._baseList[this._baseIndex + index];
				}
				set
				{
					this.InternalUpdateRange();
					if (index < 0 || index >= this._baseSize)
					{
						throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
					}
					this._baseList[this._baseIndex + index] = value;
					this.InternalUpdateVersion();
				}
			}

			// Token: 0x06006DBC RID: 28092 RVA: 0x0017B1E4 File Offset: 0x001793E4
			public override object[] ToArray()
			{
				this.InternalUpdateRange();
				object[] array = new object[this._baseSize];
				Array.Copy(this._baseList._items, this._baseIndex, array, 0, this._baseSize);
				return array;
			}

			// Token: 0x06006DBD RID: 28093 RVA: 0x0017B224 File Offset: 0x00179424
			[SecuritySafeCritical]
			public override Array ToArray(Type type)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				this.InternalUpdateRange();
				Array array = Array.UnsafeCreateInstance(type, this._baseSize);
				this._baseList.CopyTo(this._baseIndex, array, 0, this._baseSize);
				return array;
			}

			// Token: 0x06006DBE RID: 28094 RVA: 0x0017B272 File Offset: 0x00179472
			public override void TrimToSize()
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RangeCollection"));
			}

			// Token: 0x04003552 RID: 13650
			private ArrayList _baseList;

			// Token: 0x04003553 RID: 13651
			private int _baseIndex;

			// Token: 0x04003554 RID: 13652
			private int _baseSize;

			// Token: 0x04003555 RID: 13653
			private int _baseVersion;
		}

		// Token: 0x02000BAD RID: 2989
		[Serializable]
		private sealed class ArrayListEnumeratorSimple : IEnumerator, ICloneable
		{
			// Token: 0x06006DBF RID: 28095 RVA: 0x0017B284 File Offset: 0x00179484
			internal ArrayListEnumeratorSimple(ArrayList list)
			{
				this.list = list;
				this.index = -1;
				this.version = list._version;
				this.isArrayList = list.GetType() == typeof(ArrayList);
				this.currentElement = ArrayList.ArrayListEnumeratorSimple.dummyObject;
			}

			// Token: 0x06006DC0 RID: 28096 RVA: 0x0017B2D7 File Offset: 0x001794D7
			public object Clone()
			{
				return base.MemberwiseClone();
			}

			// Token: 0x06006DC1 RID: 28097 RVA: 0x0017B2E0 File Offset: 0x001794E0
			public bool MoveNext()
			{
				if (this.version != this.list._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				if (this.isArrayList)
				{
					if (this.index < this.list._size - 1)
					{
						object[] items = this.list._items;
						int num = this.index + 1;
						this.index = num;
						this.currentElement = items[num];
						return true;
					}
					this.currentElement = ArrayList.ArrayListEnumeratorSimple.dummyObject;
					this.index = this.list._size;
					return false;
				}
				else
				{
					if (this.index < this.list.Count - 1)
					{
						ArrayList arrayList = this.list;
						int num = this.index + 1;
						this.index = num;
						this.currentElement = arrayList[num];
						return true;
					}
					this.index = this.list.Count;
					this.currentElement = ArrayList.ArrayListEnumeratorSimple.dummyObject;
					return false;
				}
			}

			// Token: 0x1700129D RID: 4765
			// (get) Token: 0x06006DC2 RID: 28098 RVA: 0x0017B3C8 File Offset: 0x001795C8
			public object Current
			{
				get
				{
					object obj = this.currentElement;
					if (ArrayList.ArrayListEnumeratorSimple.dummyObject != obj)
					{
						return obj;
					}
					if (this.index == -1)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
					}
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
				}
			}

			// Token: 0x06006DC3 RID: 28099 RVA: 0x0017B40E File Offset: 0x0017960E
			public void Reset()
			{
				if (this.version != this.list._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this.currentElement = ArrayList.ArrayListEnumeratorSimple.dummyObject;
				this.index = -1;
			}

			// Token: 0x04003556 RID: 13654
			private ArrayList list;

			// Token: 0x04003557 RID: 13655
			private int index;

			// Token: 0x04003558 RID: 13656
			private int version;

			// Token: 0x04003559 RID: 13657
			private object currentElement;

			// Token: 0x0400355A RID: 13658
			[NonSerialized]
			private bool isArrayList;

			// Token: 0x0400355B RID: 13659
			private static object dummyObject = new object();
		}

		// Token: 0x02000BAE RID: 2990
		internal class ArrayListDebugView
		{
			// Token: 0x06006DC5 RID: 28101 RVA: 0x0017B451 File Offset: 0x00179651
			public ArrayListDebugView(ArrayList arrayList)
			{
				if (arrayList == null)
				{
					throw new ArgumentNullException("arrayList");
				}
				this.arrayList = arrayList;
			}

			// Token: 0x1700129E RID: 4766
			// (get) Token: 0x06006DC6 RID: 28102 RVA: 0x0017B46E File Offset: 0x0017966E
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public object[] Items
			{
				get
				{
					return this.arrayList.ToArray();
				}
			}

			// Token: 0x0400355C RID: 13660
			private ArrayList arrayList;
		}
	}
}
