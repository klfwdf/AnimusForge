using System;

namespace System.Collections
{
	// Token: 0x02000496 RID: 1174
	[Serializable]
	internal sealed class EmptyReadOnlyDictionaryInternal : IDictionary, ICollection, IEnumerable
	{
		// Token: 0x06003850 RID: 14416 RVA: 0x000D831D File Offset: 0x000D651D
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new EmptyReadOnlyDictionaryInternal.NodeEnumerator();
		}

		// Token: 0x06003851 RID: 14417 RVA: 0x000D8324 File Offset: 0x000D6524
		public void CopyTo(Array array, int index)
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
			if (array.Length - index < this.Count)
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
			}
		}

		// Token: 0x17000857 RID: 2135
		// (get) Token: 0x06003852 RID: 14418 RVA: 0x000D8396 File Offset: 0x000D6596
		public int Count
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x17000858 RID: 2136
		// (get) Token: 0x06003853 RID: 14419 RVA: 0x000D8399 File Offset: 0x000D6599
		public object SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x17000859 RID: 2137
		// (get) Token: 0x06003854 RID: 14420 RVA: 0x000D839C File Offset: 0x000D659C
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700085A RID: 2138
		public object this[object key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				return null;
			}
			set
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				if (!key.GetType().IsSerializable)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key");
				}
				if (value != null && !value.GetType().IsSerializable)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
			}
		}

		// Token: 0x1700085B RID: 2139
		// (get) Token: 0x06003857 RID: 14423 RVA: 0x000D8437 File Offset: 0x000D6637
		public ICollection Keys
		{
			get
			{
				return EmptyArray<object>.Value;
			}
		}

		// Token: 0x1700085C RID: 2140
		// (get) Token: 0x06003858 RID: 14424 RVA: 0x000D843E File Offset: 0x000D663E
		public ICollection Values
		{
			get
			{
				return EmptyArray<object>.Value;
			}
		}

		// Token: 0x06003859 RID: 14425 RVA: 0x000D8445 File Offset: 0x000D6645
		public bool Contains(object key)
		{
			return false;
		}

		// Token: 0x0600385A RID: 14426 RVA: 0x000D8448 File Offset: 0x000D6648
		public void Add(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			if (!key.GetType().IsSerializable)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key");
			}
			if (value != null && !value.GetType().IsSerializable)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
		}

		// Token: 0x0600385B RID: 14427 RVA: 0x000D84C3 File Offset: 0x000D66C3
		public void Clear()
		{
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
		}

		// Token: 0x1700085D RID: 2141
		// (get) Token: 0x0600385C RID: 14428 RVA: 0x000D84D4 File Offset: 0x000D66D4
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700085E RID: 2142
		// (get) Token: 0x0600385D RID: 14429 RVA: 0x000D84D7 File Offset: 0x000D66D7
		public bool IsFixedSize
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600385E RID: 14430 RVA: 0x000D84DA File Offset: 0x000D66DA
		public IDictionaryEnumerator GetEnumerator()
		{
			return new EmptyReadOnlyDictionaryInternal.NodeEnumerator();
		}

		// Token: 0x0600385F RID: 14431 RVA: 0x000D84E1 File Offset: 0x000D66E1
		public void Remove(object key)
		{
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
		}

		// Token: 0x02000BB6 RID: 2998
		private sealed class NodeEnumerator : IDictionaryEnumerator, IEnumerator
		{
			// Token: 0x06006DEF RID: 28143 RVA: 0x0017BCC7 File Offset: 0x00179EC7
			public bool MoveNext()
			{
				return false;
			}

			// Token: 0x170012AC RID: 4780
			// (get) Token: 0x06006DF0 RID: 28144 RVA: 0x0017BCCA File Offset: 0x00179ECA
			public object Current
			{
				get
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
			}

			// Token: 0x06006DF1 RID: 28145 RVA: 0x0017BCDB File Offset: 0x00179EDB
			public void Reset()
			{
			}

			// Token: 0x170012AD RID: 4781
			// (get) Token: 0x06006DF2 RID: 28146 RVA: 0x0017BCDD File Offset: 0x00179EDD
			public object Key
			{
				get
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
			}

			// Token: 0x170012AE RID: 4782
			// (get) Token: 0x06006DF3 RID: 28147 RVA: 0x0017BCEE File Offset: 0x00179EEE
			public object Value
			{
				get
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
			}

			// Token: 0x170012AF RID: 4783
			// (get) Token: 0x06006DF4 RID: 28148 RVA: 0x0017BCFF File Offset: 0x00179EFF
			public DictionaryEntry Entry
			{
				get
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
			}
		}
	}
}
