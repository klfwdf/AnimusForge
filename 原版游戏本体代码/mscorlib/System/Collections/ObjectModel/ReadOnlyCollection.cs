using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.ObjectModel
{
	// Token: 0x020004B6 RID: 1206
	[ComVisible(false)]
	[DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	[__DynamicallyInvokable]
	[Serializable]
	public class ReadOnlyCollection<T> : IList<!0>, ICollection<!0>, IEnumerable<!0>, IEnumerable, IList, ICollection, IReadOnlyList<!0>, IReadOnlyCollection<T>
	{
		// Token: 0x060039CB RID: 14795 RVA: 0x000DD0C5 File Offset: 0x000DB2C5
		[__DynamicallyInvokable]
		public ReadOnlyCollection(IList<T> list)
		{
			if (list == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
			}
			this.list = list;
		}

		// Token: 0x170008AF RID: 2223
		// (get) Token: 0x060039CC RID: 14796 RVA: 0x000DD0DD File Offset: 0x000DB2DD
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				return this.list.Count;
			}
		}

		// Token: 0x170008B0 RID: 2224
		[__DynamicallyInvokable]
		public T this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				return this.list[index];
			}
		}

		// Token: 0x060039CE RID: 14798 RVA: 0x000DD0F8 File Offset: 0x000DB2F8
		[__DynamicallyInvokable]
		public bool Contains(T value)
		{
			return this.list.Contains(value);
		}

		// Token: 0x060039CF RID: 14799 RVA: 0x000DD106 File Offset: 0x000DB306
		[__DynamicallyInvokable]
		public void CopyTo(T[] array, int index)
		{
			this.list.CopyTo(array, index);
		}

		// Token: 0x060039D0 RID: 14800 RVA: 0x000DD115 File Offset: 0x000DB315
		[__DynamicallyInvokable]
		public IEnumerator<T> GetEnumerator()
		{
			return this.list.GetEnumerator();
		}

		// Token: 0x060039D1 RID: 14801 RVA: 0x000DD122 File Offset: 0x000DB322
		[__DynamicallyInvokable]
		public int IndexOf(T value)
		{
			return this.list.IndexOf(value);
		}

		// Token: 0x170008B1 RID: 2225
		// (get) Token: 0x060039D2 RID: 14802 RVA: 0x000DD130 File Offset: 0x000DB330
		[__DynamicallyInvokable]
		protected IList<T> Items
		{
			[__DynamicallyInvokable]
			get
			{
				return this.list;
			}
		}

		// Token: 0x170008B2 RID: 2226
		// (get) Token: 0x060039D3 RID: 14803 RVA: 0x000DD138 File Offset: 0x000DB338
		[__DynamicallyInvokable]
		bool ICollection<!0>.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x170008B3 RID: 2227
		[__DynamicallyInvokable]
		T IList<!0>.this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				return this.list[index];
			}
			[__DynamicallyInvokable]
			set
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}
		}

		// Token: 0x060039D6 RID: 14806 RVA: 0x000DD152 File Offset: 0x000DB352
		[__DynamicallyInvokable]
		void ICollection<!0>.Add(T value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039D7 RID: 14807 RVA: 0x000DD15B File Offset: 0x000DB35B
		[__DynamicallyInvokable]
		void ICollection<!0>.Clear()
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039D8 RID: 14808 RVA: 0x000DD164 File Offset: 0x000DB364
		[__DynamicallyInvokable]
		void IList<!0>.Insert(int index, T value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039D9 RID: 14809 RVA: 0x000DD16D File Offset: 0x000DB36D
		[__DynamicallyInvokable]
		bool ICollection<!0>.Remove(T value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			return false;
		}

		// Token: 0x060039DA RID: 14810 RVA: 0x000DD177 File Offset: 0x000DB377
		[__DynamicallyInvokable]
		void IList<!0>.RemoveAt(int index)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039DB RID: 14811 RVA: 0x000DD180 File Offset: 0x000DB380
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.list.GetEnumerator();
		}

		// Token: 0x170008B4 RID: 2228
		// (get) Token: 0x060039DC RID: 14812 RVA: 0x000DD18D File Offset: 0x000DB38D
		[__DynamicallyInvokable]
		bool ICollection.IsSynchronized
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008B5 RID: 2229
		// (get) Token: 0x060039DD RID: 14813 RVA: 0x000DD190 File Offset: 0x000DB390
		[__DynamicallyInvokable]
		object ICollection.SyncRoot
		{
			[__DynamicallyInvokable]
			get
			{
				if (this._syncRoot == null)
				{
					ICollection collection = this.list as ICollection;
					if (collection != null)
					{
						this._syncRoot = collection.SyncRoot;
					}
					else
					{
						Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
					}
				}
				return this._syncRoot;
			}
		}

		// Token: 0x060039DE RID: 14814 RVA: 0x000DD1DC File Offset: 0x000DB3DC
		[__DynamicallyInvokable]
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			if (array.Rank != 1)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
			}
			if (array.GetLowerBound(0) != 0)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
			}
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < this.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			T[] array2 = array as T[];
			if (array2 != null)
			{
				this.list.CopyTo(array2, index);
				return;
			}
			Type elementType = array.GetType().GetElementType();
			Type typeFromHandle = typeof(T);
			if (!elementType.IsAssignableFrom(typeFromHandle) && !typeFromHandle.IsAssignableFrom(elementType))
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
			object[] array3 = array as object[];
			if (array3 == null)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
			int count = this.list.Count;
			try
			{
				for (int i = 0; i < count; i++)
				{
					array3[index++] = this.list[i];
				}
			}
			catch (ArrayTypeMismatchException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
		}

		// Token: 0x170008B6 RID: 2230
		// (get) Token: 0x060039DF RID: 14815 RVA: 0x000DD2E0 File Offset: 0x000DB4E0
		[__DynamicallyInvokable]
		bool IList.IsFixedSize
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x170008B7 RID: 2231
		// (get) Token: 0x060039E0 RID: 14816 RVA: 0x000DD2E3 File Offset: 0x000DB4E3
		[__DynamicallyInvokable]
		bool IList.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x170008B8 RID: 2232
		[__DynamicallyInvokable]
		object IList.this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				return this.list[index];
			}
			[__DynamicallyInvokable]
			set
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}
		}

		// Token: 0x060039E3 RID: 14819 RVA: 0x000DD302 File Offset: 0x000DB502
		[__DynamicallyInvokable]
		int IList.Add(object value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			return -1;
		}

		// Token: 0x060039E4 RID: 14820 RVA: 0x000DD30C File Offset: 0x000DB50C
		[__DynamicallyInvokable]
		void IList.Clear()
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039E5 RID: 14821 RVA: 0x000DD318 File Offset: 0x000DB518
		private static bool IsCompatibleObject(object value)
		{
			return value is T || (value == null && default(T) == null);
		}

		// Token: 0x060039E6 RID: 14822 RVA: 0x000DD345 File Offset: 0x000DB545
		[__DynamicallyInvokable]
		bool IList.Contains(object value)
		{
			return ReadOnlyCollection<T>.IsCompatibleObject(value) && this.Contains((T)((object)value));
		}

		// Token: 0x060039E7 RID: 14823 RVA: 0x000DD35D File Offset: 0x000DB55D
		[__DynamicallyInvokable]
		int IList.IndexOf(object value)
		{
			if (ReadOnlyCollection<T>.IsCompatibleObject(value))
			{
				return this.IndexOf((T)((object)value));
			}
			return -1;
		}

		// Token: 0x060039E8 RID: 14824 RVA: 0x000DD375 File Offset: 0x000DB575
		[__DynamicallyInvokable]
		void IList.Insert(int index, object value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039E9 RID: 14825 RVA: 0x000DD37E File Offset: 0x000DB57E
		[__DynamicallyInvokable]
		void IList.Remove(object value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039EA RID: 14826 RVA: 0x000DD387 File Offset: 0x000DB587
		[__DynamicallyInvokable]
		void IList.RemoveAt(int index)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x04001936 RID: 6454
		private IList<T> list;

		// Token: 0x04001937 RID: 6455
		[NonSerialized]
		private object _syncRoot;
	}
}
