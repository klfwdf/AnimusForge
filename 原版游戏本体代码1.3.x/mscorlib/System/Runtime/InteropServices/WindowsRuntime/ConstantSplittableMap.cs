using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009CC RID: 2508
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	internal sealed class ConstantSplittableMap<TKey, TValue> : IMapView<TKey, TValue>, IIterable<IKeyValuePair<TKey, TValue>>, IEnumerable<IKeyValuePair<TKey, TValue>>, IEnumerable
	{
		// Token: 0x060063CC RID: 25548 RVA: 0x00154A04 File Offset: 0x00152C04
		internal ConstantSplittableMap(IReadOnlyDictionary<TKey, TValue> data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this.firstItemIndex = 0;
			this.lastItemIndex = data.Count - 1;
			this.items = this.CreateKeyValueArray(data.Count, data.GetEnumerator());
		}

		// Token: 0x060063CD RID: 25549 RVA: 0x00154A54 File Offset: 0x00152C54
		internal ConstantSplittableMap(IMapView<TKey, TValue> data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (2147483647U < data.Size)
			{
				Exception ex = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingDictionaryTooLarge"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
			int size = (int)data.Size;
			this.firstItemIndex = 0;
			this.lastItemIndex = size - 1;
			this.items = this.CreateKeyValueArray(size, data.GetEnumerator());
		}

		// Token: 0x060063CE RID: 25550 RVA: 0x00154AC9 File Offset: 0x00152CC9
		private ConstantSplittableMap(KeyValuePair<TKey, TValue>[] items, int firstItemIndex, int lastItemIndex)
		{
			this.items = items;
			this.firstItemIndex = firstItemIndex;
			this.lastItemIndex = lastItemIndex;
		}

		// Token: 0x060063CF RID: 25551 RVA: 0x00154AE8 File Offset: 0x00152CE8
		private KeyValuePair<TKey, TValue>[] CreateKeyValueArray(int count, IEnumerator<KeyValuePair<TKey, TValue>> data)
		{
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];
			int num = 0;
			while (data.MoveNext())
			{
				KeyValuePair<!0, !1> keyValuePair = data.Current;
				array[num++] = keyValuePair;
			}
			Array.Sort<KeyValuePair<TKey, TValue>>(array, ConstantSplittableMap<TKey, TValue>.keyValuePairComparator);
			return array;
		}

		// Token: 0x060063D0 RID: 25552 RVA: 0x00154B28 File Offset: 0x00152D28
		private KeyValuePair<TKey, TValue>[] CreateKeyValueArray(int count, IEnumerator<IKeyValuePair<TKey, TValue>> data)
		{
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];
			int num = 0;
			while (data.MoveNext())
			{
				IKeyValuePair<TKey, TValue> keyValuePair = data.Current;
				array[num++] = new KeyValuePair<TKey, TValue>(keyValuePair.Key, keyValuePair.Value);
			}
			Array.Sort<KeyValuePair<TKey, TValue>>(array, ConstantSplittableMap<TKey, TValue>.keyValuePairComparator);
			return array;
		}

		// Token: 0x17001140 RID: 4416
		// (get) Token: 0x060063D1 RID: 25553 RVA: 0x00154B77 File Offset: 0x00152D77
		public int Count
		{
			get
			{
				return this.lastItemIndex - this.firstItemIndex + 1;
			}
		}

		// Token: 0x17001141 RID: 4417
		// (get) Token: 0x060063D2 RID: 25554 RVA: 0x00154B88 File Offset: 0x00152D88
		public uint Size
		{
			get
			{
				return (uint)(this.lastItemIndex - this.firstItemIndex + 1);
			}
		}

		// Token: 0x060063D3 RID: 25555 RVA: 0x00154B9C File Offset: 0x00152D9C
		public TValue Lookup(TKey key)
		{
			TValue result;
			if (!this.TryGetValue(key, out result))
			{
				Exception ex = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
			return result;
		}

		// Token: 0x060063D4 RID: 25556 RVA: 0x00154BD4 File Offset: 0x00152DD4
		public bool HasKey(TKey key)
		{
			TValue tvalue;
			return this.TryGetValue(key, out tvalue);
		}

		// Token: 0x060063D5 RID: 25557 RVA: 0x00154BEC File Offset: 0x00152DEC
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<IKeyValuePair<TKey, TValue>>)this).GetEnumerator();
		}

		// Token: 0x060063D6 RID: 25558 RVA: 0x00154BF4 File Offset: 0x00152DF4
		public IIterator<IKeyValuePair<TKey, TValue>> First()
		{
			return new EnumeratorToIteratorAdapter<IKeyValuePair<TKey, TValue>>(this.GetEnumerator());
		}

		// Token: 0x060063D7 RID: 25559 RVA: 0x00154C01 File Offset: 0x00152E01
		public IEnumerator<IKeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return new ConstantSplittableMap<TKey, TValue>.IKeyValuePairEnumerator(this.items, this.firstItemIndex, this.lastItemIndex);
		}

		// Token: 0x060063D8 RID: 25560 RVA: 0x00154C20 File Offset: 0x00152E20
		public void Split(out IMapView<TKey, TValue> firstPartition, out IMapView<TKey, TValue> secondPartition)
		{
			if (this.Count < 2)
			{
				firstPartition = null;
				secondPartition = null;
				return;
			}
			int num = (int)(((long)this.firstItemIndex + (long)this.lastItemIndex) / 2L);
			firstPartition = new ConstantSplittableMap<TKey, TValue>(this.items, this.firstItemIndex, num);
			secondPartition = new ConstantSplittableMap<TKey, TValue>(this.items, num + 1, this.lastItemIndex);
		}

		// Token: 0x060063D9 RID: 25561 RVA: 0x00154C7C File Offset: 0x00152E7C
		public bool ContainsKey(TKey key)
		{
			KeyValuePair<TKey, TValue> value = new KeyValuePair<TKey, TValue>(key, default(TValue));
			int num = Array.BinarySearch<KeyValuePair<TKey, TValue>>(this.items, this.firstItemIndex, this.Count, value, ConstantSplittableMap<TKey, TValue>.keyValuePairComparator);
			return num >= 0;
		}

		// Token: 0x060063DA RID: 25562 RVA: 0x00154CC0 File Offset: 0x00152EC0
		public bool TryGetValue(TKey key, out TValue value)
		{
			KeyValuePair<TKey, TValue> value2 = new KeyValuePair<TKey, TValue>(key, default(TValue));
			int num = Array.BinarySearch<KeyValuePair<TKey, TValue>>(this.items, this.firstItemIndex, this.Count, value2, ConstantSplittableMap<TKey, TValue>.keyValuePairComparator);
			if (num < 0)
			{
				value = default(TValue);
				return false;
			}
			value = this.items[num].Value;
			return true;
		}

		// Token: 0x17001142 RID: 4418
		public TValue this[TKey key]
		{
			get
			{
				return this.Lookup(key);
			}
		}

		// Token: 0x17001143 RID: 4419
		// (get) Token: 0x060063DC RID: 25564 RVA: 0x00154D2A File Offset: 0x00152F2A
		public IEnumerable<TKey> Keys
		{
			get
			{
				throw new NotImplementedException("NYI");
			}
		}

		// Token: 0x17001144 RID: 4420
		// (get) Token: 0x060063DD RID: 25565 RVA: 0x00154D36 File Offset: 0x00152F36
		public IEnumerable<TValue> Values
		{
			get
			{
				throw new NotImplementedException("NYI");
			}
		}

		// Token: 0x04002CE2 RID: 11490
		private static readonly ConstantSplittableMap<TKey, TValue>.KeyValuePairComparator keyValuePairComparator = new ConstantSplittableMap<TKey, TValue>.KeyValuePairComparator();

		// Token: 0x04002CE3 RID: 11491
		private readonly KeyValuePair<TKey, TValue>[] items;

		// Token: 0x04002CE4 RID: 11492
		private readonly int firstItemIndex;

		// Token: 0x04002CE5 RID: 11493
		private readonly int lastItemIndex;

		// Token: 0x02000CA1 RID: 3233
		private class KeyValuePairComparator : IComparer<KeyValuePair<TKey, TValue>>
		{
			// Token: 0x06007129 RID: 28969 RVA: 0x001857ED File Offset: 0x001839ED
			public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
			{
				return ConstantSplittableMap<TKey, TValue>.KeyValuePairComparator.keyComparator.Compare(x.Key, y.Key);
			}

			// Token: 0x0400387F RID: 14463
			private static readonly IComparer<TKey> keyComparator = Comparer<TKey>.Default;
		}

		// Token: 0x02000CA2 RID: 3234
		[Serializable]
		internal struct IKeyValuePairEnumerator : IEnumerator<IKeyValuePair<TKey, TValue>>, IDisposable, IEnumerator
		{
			// Token: 0x0600712C RID: 28972 RVA: 0x0018581B File Offset: 0x00183A1B
			internal IKeyValuePairEnumerator(KeyValuePair<TKey, TValue>[] items, int first, int end)
			{
				this._array = items;
				this._start = first;
				this._end = end;
				this._current = this._start - 1;
			}

			// Token: 0x0600712D RID: 28973 RVA: 0x00185840 File Offset: 0x00183A40
			public bool MoveNext()
			{
				if (this._current < this._end)
				{
					this._current++;
					return true;
				}
				return false;
			}

			// Token: 0x17001367 RID: 4967
			// (get) Token: 0x0600712E RID: 28974 RVA: 0x00185864 File Offset: 0x00183A64
			public IKeyValuePair<TKey, TValue> Current
			{
				get
				{
					if (this._current < this._start)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
					}
					if (this._current > this._end)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
					}
					return new CLRIKeyValuePairImpl<TKey, TValue>(ref this._array[this._current]);
				}
			}

			// Token: 0x17001368 RID: 4968
			// (get) Token: 0x0600712F RID: 28975 RVA: 0x001858C3 File Offset: 0x00183AC3
			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}

			// Token: 0x06007130 RID: 28976 RVA: 0x001858CB File Offset: 0x00183ACB
			void IEnumerator.Reset()
			{
				this._current = this._start - 1;
			}

			// Token: 0x06007131 RID: 28977 RVA: 0x001858DB File Offset: 0x00183ADB
			public void Dispose()
			{
			}

			// Token: 0x04003880 RID: 14464
			private KeyValuePair<TKey, TValue>[] _array;

			// Token: 0x04003881 RID: 14465
			private int _start;

			// Token: 0x04003882 RID: 14466
			private int _end;

			// Token: 0x04003883 RID: 14467
			private int _current;
		}
	}
}
