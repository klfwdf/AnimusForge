using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Collections.Generic
{
	// Token: 0x020004DC RID: 1244
	[DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	[__DynamicallyInvokable]
	[Serializable]
	public class List<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection, IReadOnlyList<!0>, IReadOnlyCollection<T>
	{
		// Token: 0x06003AEC RID: 15084 RVA: 0x000DFBD9 File Offset: 0x000DDDD9
		[__DynamicallyInvokable]
		public List()
		{
			this._items = List<T>._emptyArray;
		}

		// Token: 0x06003AED RID: 15085 RVA: 0x000DFBEC File Offset: 0x000DDDEC
		[__DynamicallyInvokable]
		public List(int capacity)
		{
			if (capacity < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (capacity == 0)
			{
				this._items = List<T>._emptyArray;
				return;
			}
			this._items = new T[capacity];
		}

		// Token: 0x06003AEE RID: 15086 RVA: 0x000DFC1C File Offset: 0x000DDE1C
		[__DynamicallyInvokable]
		public List(IEnumerable<T> collection)
		{
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
			}
			ICollection<T> collection2 = collection as ICollection<!0>;
			if (collection2 == null)
			{
				this._size = 0;
				this._items = List<T>._emptyArray;
				foreach (T item in collection)
				{
					this.Add(item);
				}
				return;
			}
			int count = collection2.Count;
			if (count == 0)
			{
				this._items = List<T>._emptyArray;
				return;
			}
			this._items = new T[count];
			collection2.CopyTo(this._items, 0);
			this._size = count;
		}

		// Token: 0x170008F4 RID: 2292
		// (get) Token: 0x06003AEF RID: 15087 RVA: 0x000DFCC4 File Offset: 0x000DDEC4
		// (set) Token: 0x06003AF0 RID: 15088 RVA: 0x000DFCD0 File Offset: 0x000DDED0
		[__DynamicallyInvokable]
		public int Capacity
		{
			[__DynamicallyInvokable]
			get
			{
				return this._items.Length;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value < this._size)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_SmallCapacity);
				}
				if (value != this._items.Length)
				{
					if (value > 0)
					{
						T[] array = new T[value];
						if (this._size > 0)
						{
							Array.Copy(this._items, 0, array, 0, this._size);
						}
						this._items = array;
						return;
					}
					this._items = List<T>._emptyArray;
				}
			}
		}

		// Token: 0x170008F5 RID: 2293
		// (get) Token: 0x06003AF1 RID: 15089 RVA: 0x000DFD35 File Offset: 0x000DDF35
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				return this._size;
			}
		}

		// Token: 0x170008F6 RID: 2294
		// (get) Token: 0x06003AF2 RID: 15090 RVA: 0x000DFD3D File Offset: 0x000DDF3D
		[__DynamicallyInvokable]
		bool IList.IsFixedSize
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008F7 RID: 2295
		// (get) Token: 0x06003AF3 RID: 15091 RVA: 0x000DFD40 File Offset: 0x000DDF40
		[__DynamicallyInvokable]
		bool ICollection<!0>.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008F8 RID: 2296
		// (get) Token: 0x06003AF4 RID: 15092 RVA: 0x000DFD43 File Offset: 0x000DDF43
		[__DynamicallyInvokable]
		bool IList.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008F9 RID: 2297
		// (get) Token: 0x06003AF5 RID: 15093 RVA: 0x000DFD46 File Offset: 0x000DDF46
		[__DynamicallyInvokable]
		bool ICollection.IsSynchronized
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008FA RID: 2298
		// (get) Token: 0x06003AF6 RID: 15094 RVA: 0x000DFD49 File Offset: 0x000DDF49
		[__DynamicallyInvokable]
		object ICollection.SyncRoot
		{
			[__DynamicallyInvokable]
			get
			{
				if (this._syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
				}
				return this._syncRoot;
			}
		}

		// Token: 0x170008FB RID: 2299
		[__DynamicallyInvokable]
		public T this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				if (index >= this._size)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				return this._items[index];
			}
			[__DynamicallyInvokable]
			set
			{
				if (index >= this._size)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				this._items[index] = value;
				this._version++;
			}
		}

		// Token: 0x06003AF9 RID: 15097 RVA: 0x000DFDB4 File Offset: 0x000DDFB4
		private static bool IsCompatibleObject(object value)
		{
			return value is T || (value == null && default(T) == null);
		}

		// Token: 0x170008FC RID: 2300
		[__DynamicallyInvokable]
		object IList.this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				return this[index];
			}
			[__DynamicallyInvokable]
			set
			{
				ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);
				try
				{
					this[index] = (T)((object)value);
				}
				catch (InvalidCastException)
				{
					ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
				}
			}
		}

		// Token: 0x06003AFC RID: 15100 RVA: 0x000DFE38 File Offset: 0x000DE038
		[__DynamicallyInvokable]
		public void Add(T item)
		{
			if (this._size == this._items.Length)
			{
				this.EnsureCapacity(this._size + 1);
			}
			T[] items = this._items;
			int size = this._size;
			this._size = size + 1;
			items[size] = item;
			this._version++;
		}

		// Token: 0x06003AFD RID: 15101 RVA: 0x000DFE90 File Offset: 0x000DE090
		[__DynamicallyInvokable]
		int IList.Add(object item)
		{
			ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);
			try
			{
				this.Add((T)((object)item));
			}
			catch (InvalidCastException)
			{
				ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
			}
			return this.Count - 1;
		}

		// Token: 0x06003AFE RID: 15102 RVA: 0x000DFEE0 File Offset: 0x000DE0E0
		[__DynamicallyInvokable]
		public void AddRange(IEnumerable<T> collection)
		{
			this.InsertRange(this._size, collection);
		}

		// Token: 0x06003AFF RID: 15103 RVA: 0x000DFEEF File Offset: 0x000DE0EF
		[__DynamicallyInvokable]
		public ReadOnlyCollection<T> AsReadOnly()
		{
			return new ReadOnlyCollection<T>(this);
		}

		// Token: 0x06003B00 RID: 15104 RVA: 0x000DFEF7 File Offset: 0x000DE0F7
		[__DynamicallyInvokable]
		public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this._size - index < count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
			}
			return Array.BinarySearch<T>(this._items, index, count, item, comparer);
		}

		// Token: 0x06003B01 RID: 15105 RVA: 0x000DFF33 File Offset: 0x000DE133
		[__DynamicallyInvokable]
		public int BinarySearch(T item)
		{
			return this.BinarySearch(0, this.Count, item, null);
		}

		// Token: 0x06003B02 RID: 15106 RVA: 0x000DFF44 File Offset: 0x000DE144
		[__DynamicallyInvokable]
		public int BinarySearch(T item, IComparer<T> comparer)
		{
			return this.BinarySearch(0, this.Count, item, comparer);
		}

		// Token: 0x06003B03 RID: 15107 RVA: 0x000DFF55 File Offset: 0x000DE155
		[__DynamicallyInvokable]
		public void Clear()
		{
			if (this._size > 0)
			{
				Array.Clear(this._items, 0, this._size);
				this._size = 0;
			}
			this._version++;
		}

		// Token: 0x06003B04 RID: 15108 RVA: 0x000DFF88 File Offset: 0x000DE188
		[__DynamicallyInvokable]
		public bool Contains(T item)
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
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			for (int j = 0; j < this._size; j++)
			{
				if (@default.Equals(this._items[j], item))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06003B05 RID: 15109 RVA: 0x000DFFF4 File Offset: 0x000DE1F4
		[__DynamicallyInvokable]
		bool IList.Contains(object item)
		{
			return List<T>.IsCompatibleObject(item) && this.Contains((T)((object)item));
		}

		// Token: 0x06003B06 RID: 15110 RVA: 0x000E000C File Offset: 0x000DE20C
		public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
		{
			if (converter == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
			}
			List<TOutput> list = new List<TOutput>(this._size);
			for (int i = 0; i < this._size; i++)
			{
				list._items[i] = converter(this._items[i]);
			}
			list._size = this._size;
			return list;
		}

		// Token: 0x06003B07 RID: 15111 RVA: 0x000E006B File Offset: 0x000DE26B
		[__DynamicallyInvokable]
		public void CopyTo(T[] array)
		{
			this.CopyTo(array, 0);
		}

		// Token: 0x06003B08 RID: 15112 RVA: 0x000E0078 File Offset: 0x000DE278
		[__DynamicallyInvokable]
		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			if (array != null && array.Rank != 1)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
			}
			try
			{
				Array.Copy(this._items, 0, array, arrayIndex, this._size);
			}
			catch (ArrayTypeMismatchException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
		}

		// Token: 0x06003B09 RID: 15113 RVA: 0x000E00C8 File Offset: 0x000DE2C8
		[__DynamicallyInvokable]
		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if (this._size - index < count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
			}
			Array.Copy(this._items, index, array, arrayIndex, count);
		}

		// Token: 0x06003B0A RID: 15114 RVA: 0x000E00ED File Offset: 0x000DE2ED
		[__DynamicallyInvokable]
		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(this._items, 0, array, arrayIndex, this._size);
		}

		// Token: 0x06003B0B RID: 15115 RVA: 0x000E0104 File Offset: 0x000DE304
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

		// Token: 0x06003B0C RID: 15116 RVA: 0x000E014E File Offset: 0x000DE34E
		[__DynamicallyInvokable]
		public bool Exists(Predicate<T> match)
		{
			return this.FindIndex(match) != -1;
		}

		// Token: 0x06003B0D RID: 15117 RVA: 0x000E0160 File Offset: 0x000DE360
		[__DynamicallyInvokable]
		public T Find(Predicate<T> match)
		{
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			for (int i = 0; i < this._size; i++)
			{
				if (match(this._items[i]))
				{
					return this._items[i];
				}
			}
			return default(T);
		}

		// Token: 0x06003B0E RID: 15118 RVA: 0x000E01B4 File Offset: 0x000DE3B4
		[__DynamicallyInvokable]
		public List<T> FindAll(Predicate<T> match)
		{
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			List<T> list = new List<T>();
			for (int i = 0; i < this._size; i++)
			{
				if (match(this._items[i]))
				{
					list.Add(this._items[i]);
				}
			}
			return list;
		}

		// Token: 0x06003B0F RID: 15119 RVA: 0x000E0208 File Offset: 0x000DE408
		[__DynamicallyInvokable]
		public int FindIndex(Predicate<T> match)
		{
			return this.FindIndex(0, this._size, match);
		}

		// Token: 0x06003B10 RID: 15120 RVA: 0x000E0218 File Offset: 0x000DE418
		[__DynamicallyInvokable]
		public int FindIndex(int startIndex, Predicate<T> match)
		{
			return this.FindIndex(startIndex, this._size - startIndex, match);
		}

		// Token: 0x06003B11 RID: 15121 RVA: 0x000E022C File Offset: 0x000DE42C
		[__DynamicallyInvokable]
		public int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			if (startIndex > this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (count < 0 || startIndex > this._size - count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			int num = startIndex + count;
			for (int i = startIndex; i < num; i++)
			{
				if (match(this._items[i]))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06003B12 RID: 15122 RVA: 0x000E0294 File Offset: 0x000DE494
		[__DynamicallyInvokable]
		public T FindLast(Predicate<T> match)
		{
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			for (int i = this._size - 1; i >= 0; i--)
			{
				if (match(this._items[i]))
				{
					return this._items[i];
				}
			}
			return default(T);
		}

		// Token: 0x06003B13 RID: 15123 RVA: 0x000E02E7 File Offset: 0x000DE4E7
		[__DynamicallyInvokable]
		public int FindLastIndex(Predicate<T> match)
		{
			return this.FindLastIndex(this._size - 1, this._size, match);
		}

		// Token: 0x06003B14 RID: 15124 RVA: 0x000E02FE File Offset: 0x000DE4FE
		[__DynamicallyInvokable]
		public int FindLastIndex(int startIndex, Predicate<T> match)
		{
			return this.FindLastIndex(startIndex, startIndex + 1, match);
		}

		// Token: 0x06003B15 RID: 15125 RVA: 0x000E030C File Offset: 0x000DE50C
		[__DynamicallyInvokable]
		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			if (this._size == 0)
			{
				if (startIndex != -1)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
				}
			}
			else if (startIndex >= this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (count < 0 || startIndex - count + 1 < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
			int num = startIndex - count;
			for (int i = startIndex; i > num; i--)
			{
				if (match(this._items[i]))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06003B16 RID: 15126 RVA: 0x000E0388 File Offset: 0x000DE588
		[__DynamicallyInvokable]
		public void ForEach(Action<T> action)
		{
			if (action == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			int version = this._version;
			int num = 0;
			while (num < this._size && (version == this._version || !BinaryCompatibility.TargetsAtLeast_Desktop_V4_5))
			{
				action(this._items[num]);
				num++;
			}
			if (version != this._version && BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
			{
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
			}
		}

		// Token: 0x06003B17 RID: 15127 RVA: 0x000E03EF File Offset: 0x000DE5EF
		[__DynamicallyInvokable]
		public List<T>.Enumerator GetEnumerator()
		{
			return new List<T>.Enumerator(this);
		}

		// Token: 0x06003B18 RID: 15128 RVA: 0x000E03F7 File Offset: 0x000DE5F7
		[__DynamicallyInvokable]
		IEnumerator<T> IEnumerable<!0>.GetEnumerator()
		{
			return new List<T>.Enumerator(this);
		}

		// Token: 0x06003B19 RID: 15129 RVA: 0x000E0404 File Offset: 0x000DE604
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new List<T>.Enumerator(this);
		}

		// Token: 0x06003B1A RID: 15130 RVA: 0x000E0414 File Offset: 0x000DE614
		[__DynamicallyInvokable]
		public List<T> GetRange(int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this._size - index < count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
			}
			List<T> list = new List<T>(count);
			Array.Copy(this._items, index, list._items, 0, count);
			list._size = count;
			return list;
		}

		// Token: 0x06003B1B RID: 15131 RVA: 0x000E046E File Offset: 0x000DE66E
		[__DynamicallyInvokable]
		public int IndexOf(T item)
		{
			return Array.IndexOf<T>(this._items, item, 0, this._size);
		}

		// Token: 0x06003B1C RID: 15132 RVA: 0x000E0483 File Offset: 0x000DE683
		[__DynamicallyInvokable]
		int IList.IndexOf(object item)
		{
			if (List<T>.IsCompatibleObject(item))
			{
				return this.IndexOf((T)((object)item));
			}
			return -1;
		}

		// Token: 0x06003B1D RID: 15133 RVA: 0x000E049B File Offset: 0x000DE69B
		[__DynamicallyInvokable]
		public int IndexOf(T item, int index)
		{
			if (index > this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
			}
			return Array.IndexOf<T>(this._items, item, index, this._size - index);
		}

		// Token: 0x06003B1E RID: 15134 RVA: 0x000E04C4 File Offset: 0x000DE6C4
		[__DynamicallyInvokable]
		public int IndexOf(T item, int index, int count)
		{
			if (index > this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (count < 0 || index > this._size - count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
			return Array.IndexOf<T>(this._items, item, index, count);
		}

		// Token: 0x06003B1F RID: 15135 RVA: 0x000E0500 File Offset: 0x000DE700
		[__DynamicallyInvokable]
		public void Insert(int index, T item)
		{
			if (index > this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_ListInsert);
			}
			if (this._size == this._items.Length)
			{
				this.EnsureCapacity(this._size + 1);
			}
			if (index < this._size)
			{
				Array.Copy(this._items, index, this._items, index + 1, this._size - index);
			}
			this._items[index] = item;
			this._size++;
			this._version++;
		}

		// Token: 0x06003B20 RID: 15136 RVA: 0x000E058C File Offset: 0x000DE78C
		[__DynamicallyInvokable]
		void IList.Insert(int index, object item)
		{
			ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);
			try
			{
				this.Insert(index, (T)((object)item));
			}
			catch (InvalidCastException)
			{
				ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
			}
		}

		// Token: 0x06003B21 RID: 15137 RVA: 0x000E05D4 File Offset: 0x000DE7D4
		[__DynamicallyInvokable]
		public void InsertRange(int index, IEnumerable<T> collection)
		{
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
			}
			if (index > this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
			}
			ICollection<T> collection2 = collection as ICollection<!0>;
			if (collection2 != null)
			{
				int count = collection2.Count;
				if (count > 0)
				{
					this.EnsureCapacity(this._size + count);
					if (index < this._size)
					{
						Array.Copy(this._items, index, this._items, index + count, this._size - index);
					}
					if (this == collection2)
					{
						Array.Copy(this._items, 0, this._items, index, index);
						Array.Copy(this._items, index + count, this._items, index * 2, this._size - index);
					}
					else
					{
						T[] array = new T[count];
						collection2.CopyTo(array, 0);
						array.CopyTo(this._items, index);
					}
					this._size += count;
				}
			}
			else
			{
				foreach (T item in collection)
				{
					this.Insert(index++, item);
				}
			}
			this._version++;
		}

		// Token: 0x06003B22 RID: 15138 RVA: 0x000E0700 File Offset: 0x000DE900
		[__DynamicallyInvokable]
		public int LastIndexOf(T item)
		{
			if (this._size == 0)
			{
				return -1;
			}
			return this.LastIndexOf(item, this._size - 1, this._size);
		}

		// Token: 0x06003B23 RID: 15139 RVA: 0x000E0721 File Offset: 0x000DE921
		[__DynamicallyInvokable]
		public int LastIndexOf(T item, int index)
		{
			if (index >= this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
			}
			return this.LastIndexOf(item, index, index + 1);
		}

		// Token: 0x06003B24 RID: 15140 RVA: 0x000E0740 File Offset: 0x000DE940
		[__DynamicallyInvokable]
		public int LastIndexOf(T item, int index, int count)
		{
			if (this.Count != 0 && index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this.Count != 0 && count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this._size == 0)
			{
				return -1;
			}
			if (index >= this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
			}
			if (count > index + 1)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
			}
			return Array.LastIndexOf<T>(this._items, item, index, count);
		}

		// Token: 0x06003B25 RID: 15141 RVA: 0x000E07B0 File Offset: 0x000DE9B0
		[__DynamicallyInvokable]
		public bool Remove(T item)
		{
			int num = this.IndexOf(item);
			if (num >= 0)
			{
				this.RemoveAt(num);
				return true;
			}
			return false;
		}

		// Token: 0x06003B26 RID: 15142 RVA: 0x000E07D3 File Offset: 0x000DE9D3
		[__DynamicallyInvokable]
		void IList.Remove(object item)
		{
			if (List<T>.IsCompatibleObject(item))
			{
				this.Remove((T)((object)item));
			}
		}

		// Token: 0x06003B27 RID: 15143 RVA: 0x000E07EC File Offset: 0x000DE9EC
		[__DynamicallyInvokable]
		public int RemoveAll(Predicate<T> match)
		{
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			int num = 0;
			while (num < this._size && !match(this._items[num]))
			{
				num++;
			}
			if (num >= this._size)
			{
				return 0;
			}
			int i = num + 1;
			while (i < this._size)
			{
				while (i < this._size && match(this._items[i]))
				{
					i++;
				}
				if (i < this._size)
				{
					this._items[num++] = this._items[i++];
				}
			}
			Array.Clear(this._items, num, this._size - num);
			int result = this._size - num;
			this._size = num;
			this._version++;
			return result;
		}

		// Token: 0x06003B28 RID: 15144 RVA: 0x000E08C0 File Offset: 0x000DEAC0
		[__DynamicallyInvokable]
		public void RemoveAt(int index)
		{
			if (index >= this._size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this._size--;
			if (index < this._size)
			{
				Array.Copy(this._items, index + 1, this._items, index, this._size - index);
			}
			this._items[this._size] = default(T);
			this._version++;
		}

		// Token: 0x06003B29 RID: 15145 RVA: 0x000E0938 File Offset: 0x000DEB38
		[__DynamicallyInvokable]
		public void RemoveRange(int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this._size - index < count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
			}
			if (count > 0)
			{
				int size = this._size;
				this._size -= count;
				if (index < this._size)
				{
					Array.Copy(this._items, index + count, this._items, index, this._size - index);
				}
				Array.Clear(this._items, this._size, count);
				this._version++;
			}
		}

		// Token: 0x06003B2A RID: 15146 RVA: 0x000E09CE File Offset: 0x000DEBCE
		[__DynamicallyInvokable]
		public void Reverse()
		{
			this.Reverse(0, this.Count);
		}

		// Token: 0x06003B2B RID: 15147 RVA: 0x000E09E0 File Offset: 0x000DEBE0
		[__DynamicallyInvokable]
		public void Reverse(int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this._size - index < count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
			}
			Array.Reverse(this._items, index, count);
			this._version++;
		}

		// Token: 0x06003B2C RID: 15148 RVA: 0x000E0A32 File Offset: 0x000DEC32
		[__DynamicallyInvokable]
		public void Sort()
		{
			this.Sort(0, this.Count, null);
		}

		// Token: 0x06003B2D RID: 15149 RVA: 0x000E0A42 File Offset: 0x000DEC42
		[__DynamicallyInvokable]
		public void Sort(IComparer<T> comparer)
		{
			this.Sort(0, this.Count, comparer);
		}

		// Token: 0x06003B2E RID: 15150 RVA: 0x000E0A54 File Offset: 0x000DEC54
		[__DynamicallyInvokable]
		public void Sort(int index, int count, IComparer<T> comparer)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (this._size - index < count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
			}
			Array.Sort<T>(this._items, index, count, comparer);
			this._version++;
		}

		// Token: 0x06003B2F RID: 15151 RVA: 0x000E0AA8 File Offset: 0x000DECA8
		[__DynamicallyInvokable]
		public void Sort(Comparison<T> comparison)
		{
			if (comparison == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			if (this._size > 0)
			{
				IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
				Array.Sort<T>(this._items, 0, this._size, comparer);
			}
		}

		// Token: 0x06003B30 RID: 15152 RVA: 0x000E0AE4 File Offset: 0x000DECE4
		[__DynamicallyInvokable]
		public T[] ToArray()
		{
			T[] array = new T[this._size];
			Array.Copy(this._items, 0, array, 0, this._size);
			return array;
		}

		// Token: 0x06003B31 RID: 15153 RVA: 0x000E0B14 File Offset: 0x000DED14
		[__DynamicallyInvokable]
		public void TrimExcess()
		{
			int num = (int)((double)this._items.Length * 0.9);
			if (this._size < num)
			{
				this.Capacity = this._size;
			}
		}

		// Token: 0x06003B32 RID: 15154 RVA: 0x000E0B4C File Offset: 0x000DED4C
		[__DynamicallyInvokable]
		public bool TrueForAll(Predicate<T> match)
		{
			if (match == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
			}
			for (int i = 0; i < this._size; i++)
			{
				if (!match(this._items[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06003B33 RID: 15155 RVA: 0x000E0B8A File Offset: 0x000DED8A
		internal static IList<T> Synchronized(List<T> list)
		{
			return new List<T>.SynchronizedList(list);
		}

		// Token: 0x0400195B RID: 6491
		private const int _defaultCapacity = 4;

		// Token: 0x0400195C RID: 6492
		private T[] _items;

		// Token: 0x0400195D RID: 6493
		private int _size;

		// Token: 0x0400195E RID: 6494
		private int _version;

		// Token: 0x0400195F RID: 6495
		[NonSerialized]
		private object _syncRoot;

		// Token: 0x04001960 RID: 6496
		private static readonly T[] _emptyArray = new T[0];

		// Token: 0x02000BE4 RID: 3044
		[Serializable]
		internal class SynchronizedList : IList<!0>, ICollection<!0>, IEnumerable<!0>, IEnumerable
		{
			// Token: 0x06006F29 RID: 28457 RVA: 0x0017ED0C File Offset: 0x0017CF0C
			internal SynchronizedList(List<T> list)
			{
				this._list = list;
				this._root = ((ICollection)list).SyncRoot;
			}

			// Token: 0x17001316 RID: 4886
			// (get) Token: 0x06006F2A RID: 28458 RVA: 0x0017ED28 File Offset: 0x0017CF28
			public int Count
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

			// Token: 0x17001317 RID: 4887
			// (get) Token: 0x06006F2B RID: 28459 RVA: 0x0017ED70 File Offset: 0x0017CF70
			public bool IsReadOnly
			{
				get
				{
					return ((ICollection<!0>)this._list).IsReadOnly;
				}
			}

			// Token: 0x06006F2C RID: 28460 RVA: 0x0017ED80 File Offset: 0x0017CF80
			public void Add(T item)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Add(item);
				}
			}

			// Token: 0x06006F2D RID: 28461 RVA: 0x0017EDC8 File Offset: 0x0017CFC8
			public void Clear()
			{
				object root = this._root;
				lock (root)
				{
					this._list.Clear();
				}
			}

			// Token: 0x06006F2E RID: 28462 RVA: 0x0017EE10 File Offset: 0x0017D010
			public bool Contains(T item)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.Contains(item);
				}
				return result;
			}

			// Token: 0x06006F2F RID: 28463 RVA: 0x0017EE58 File Offset: 0x0017D058
			public void CopyTo(T[] array, int arrayIndex)
			{
				object root = this._root;
				lock (root)
				{
					this._list.CopyTo(array, arrayIndex);
				}
			}

			// Token: 0x06006F30 RID: 28464 RVA: 0x0017EEA0 File Offset: 0x0017D0A0
			public bool Remove(T item)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._list.Remove(item);
				}
				return result;
			}

			// Token: 0x06006F31 RID: 28465 RVA: 0x0017EEE8 File Offset: 0x0017D0E8
			IEnumerator IEnumerable.GetEnumerator()
			{
				object root = this._root;
				IEnumerator result;
				lock (root)
				{
					result = this._list.GetEnumerator();
				}
				return result;
			}

			// Token: 0x06006F32 RID: 28466 RVA: 0x0017EF34 File Offset: 0x0017D134
			IEnumerator<T> IEnumerable<!0>.GetEnumerator()
			{
				object root = this._root;
				IEnumerator<T> enumerator;
				lock (root)
				{
					enumerator = ((IEnumerable<!0>)this._list).GetEnumerator();
				}
				return enumerator;
			}

			// Token: 0x17001318 RID: 4888
			public T this[int index]
			{
				get
				{
					object root = this._root;
					T result;
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

			// Token: 0x06006F35 RID: 28469 RVA: 0x0017F00C File Offset: 0x0017D20C
			public int IndexOf(T item)
			{
				object root = this._root;
				int result;
				lock (root)
				{
					result = this._list.IndexOf(item);
				}
				return result;
			}

			// Token: 0x06006F36 RID: 28470 RVA: 0x0017F054 File Offset: 0x0017D254
			public void Insert(int index, T item)
			{
				object root = this._root;
				lock (root)
				{
					this._list.Insert(index, item);
				}
			}

			// Token: 0x06006F37 RID: 28471 RVA: 0x0017F09C File Offset: 0x0017D29C
			public void RemoveAt(int index)
			{
				object root = this._root;
				lock (root)
				{
					this._list.RemoveAt(index);
				}
			}

			// Token: 0x040035FA RID: 13818
			private List<T> _list;

			// Token: 0x040035FB RID: 13819
			private object _root;
		}

		// Token: 0x02000BE5 RID: 3045
		[__DynamicallyInvokable]
		[Serializable]
		public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			// Token: 0x06006F38 RID: 28472 RVA: 0x0017F0E4 File Offset: 0x0017D2E4
			internal Enumerator(List<T> list)
			{
				this.list = list;
				this.index = 0;
				this.version = list._version;
				this.current = default(T);
			}

			// Token: 0x06006F39 RID: 28473 RVA: 0x0017F10C File Offset: 0x0017D30C
			[__DynamicallyInvokable]
			public void Dispose()
			{
			}

			// Token: 0x06006F3A RID: 28474 RVA: 0x0017F110 File Offset: 0x0017D310
			[__DynamicallyInvokable]
			public bool MoveNext()
			{
				List<T> list = this.list;
				if (this.version == list._version && this.index < list._size)
				{
					this.current = list._items[this.index];
					this.index++;
					return true;
				}
				return this.MoveNextRare();
			}

			// Token: 0x06006F3B RID: 28475 RVA: 0x0017F16D File Offset: 0x0017D36D
			private bool MoveNextRare()
			{
				if (this.version != this.list._version)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
				}
				this.index = this.list._size + 1;
				this.current = default(T);
				return false;
			}

			// Token: 0x17001319 RID: 4889
			// (get) Token: 0x06006F3C RID: 28476 RVA: 0x0017F1A9 File Offset: 0x0017D3A9
			[__DynamicallyInvokable]
			public T Current
			{
				[__DynamicallyInvokable]
				get
				{
					return this.current;
				}
			}

			// Token: 0x1700131A RID: 4890
			// (get) Token: 0x06006F3D RID: 28477 RVA: 0x0017F1B1 File Offset: 0x0017D3B1
			[__DynamicallyInvokable]
			object IEnumerator.Current
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.index == 0 || this.index == this.list._size + 1)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
					}
					return this.Current;
				}
			}

			// Token: 0x06006F3E RID: 28478 RVA: 0x0017F1E2 File Offset: 0x0017D3E2
			[__DynamicallyInvokable]
			void IEnumerator.Reset()
			{
				if (this.version != this.list._version)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
				}
				this.index = 0;
				this.current = default(T);
			}

			// Token: 0x040035FC RID: 13820
			private List<T> list;

			// Token: 0x040035FD RID: 13821
			private int index;

			// Token: 0x040035FE RID: 13822
			private int version;

			// Token: 0x040035FF RID: 13823
			private T current;
		}
	}
}
