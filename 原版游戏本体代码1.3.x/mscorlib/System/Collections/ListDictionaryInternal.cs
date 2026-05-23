using System;
using System.Threading;

namespace System.Collections
{
	// Token: 0x02000495 RID: 1173
	[Serializable]
	internal class ListDictionaryInternal : IDictionary, ICollection, IEnumerable
	{
		// Token: 0x1700084F RID: 2127
		public object this[object key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
				}
				for (ListDictionaryInternal.DictionaryNode next = this.head; next != null; next = next.next)
				{
					if (next.key.Equals(key))
					{
						return next.value;
					}
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
				this.version++;
				ListDictionaryInternal.DictionaryNode dictionaryNode = null;
				ListDictionaryInternal.DictionaryNode next = this.head;
				while (next != null && !next.key.Equals(key))
				{
					dictionaryNode = next;
					next = next.next;
				}
				if (next != null)
				{
					next.value = value;
					return;
				}
				ListDictionaryInternal.DictionaryNode dictionaryNode2 = new ListDictionaryInternal.DictionaryNode();
				dictionaryNode2.key = key;
				dictionaryNode2.value = value;
				if (dictionaryNode != null)
				{
					dictionaryNode.next = dictionaryNode2;
				}
				else
				{
					this.head = dictionaryNode2;
				}
				this.count++;
			}
		}

		// Token: 0x17000850 RID: 2128
		// (get) Token: 0x06003841 RID: 14401 RVA: 0x000D801F File Offset: 0x000D621F
		public int Count
		{
			get
			{
				return this.count;
			}
		}

		// Token: 0x17000851 RID: 2129
		// (get) Token: 0x06003842 RID: 14402 RVA: 0x000D8027 File Offset: 0x000D6227
		public ICollection Keys
		{
			get
			{
				return new ListDictionaryInternal.NodeKeyValueCollection(this, true);
			}
		}

		// Token: 0x17000852 RID: 2130
		// (get) Token: 0x06003843 RID: 14403 RVA: 0x000D8030 File Offset: 0x000D6230
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000853 RID: 2131
		// (get) Token: 0x06003844 RID: 14404 RVA: 0x000D8033 File Offset: 0x000D6233
		public bool IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000854 RID: 2132
		// (get) Token: 0x06003845 RID: 14405 RVA: 0x000D8036 File Offset: 0x000D6236
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000855 RID: 2133
		// (get) Token: 0x06003846 RID: 14406 RVA: 0x000D8039 File Offset: 0x000D6239
		public object SyncRoot
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

		// Token: 0x17000856 RID: 2134
		// (get) Token: 0x06003847 RID: 14407 RVA: 0x000D805B File Offset: 0x000D625B
		public ICollection Values
		{
			get
			{
				return new ListDictionaryInternal.NodeKeyValueCollection(this, false);
			}
		}

		// Token: 0x06003848 RID: 14408 RVA: 0x000D8064 File Offset: 0x000D6264
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
			this.version++;
			ListDictionaryInternal.DictionaryNode dictionaryNode = null;
			ListDictionaryInternal.DictionaryNode next;
			for (next = this.head; next != null; next = next.next)
			{
				if (next.key.Equals(key))
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate__", new object[] { next.key, key }));
				}
				dictionaryNode = next;
			}
			if (next != null)
			{
				next.value = value;
				return;
			}
			ListDictionaryInternal.DictionaryNode dictionaryNode2 = new ListDictionaryInternal.DictionaryNode();
			dictionaryNode2.key = key;
			dictionaryNode2.value = value;
			if (dictionaryNode != null)
			{
				dictionaryNode.next = dictionaryNode2;
			}
			else
			{
				this.head = dictionaryNode2;
			}
			this.count++;
		}

		// Token: 0x06003849 RID: 14409 RVA: 0x000D8166 File Offset: 0x000D6366
		public void Clear()
		{
			this.count = 0;
			this.head = null;
			this.version++;
		}

		// Token: 0x0600384A RID: 14410 RVA: 0x000D8184 File Offset: 0x000D6384
		public bool Contains(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			for (ListDictionaryInternal.DictionaryNode next = this.head; next != null; next = next.next)
			{
				if (next.key.Equals(key))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600384B RID: 14411 RVA: 0x000D81D0 File Offset: 0x000D63D0
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
			for (ListDictionaryInternal.DictionaryNode next = this.head; next != null; next = next.next)
			{
				array.SetValue(new DictionaryEntry(next.key, next.value), index);
				index++;
			}
		}

		// Token: 0x0600384C RID: 14412 RVA: 0x000D8277 File Offset: 0x000D6477
		public IDictionaryEnumerator GetEnumerator()
		{
			return new ListDictionaryInternal.NodeEnumerator(this);
		}

		// Token: 0x0600384D RID: 14413 RVA: 0x000D827F File Offset: 0x000D647F
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ListDictionaryInternal.NodeEnumerator(this);
		}

		// Token: 0x0600384E RID: 14414 RVA: 0x000D8288 File Offset: 0x000D6488
		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
			}
			this.version++;
			ListDictionaryInternal.DictionaryNode dictionaryNode = null;
			ListDictionaryInternal.DictionaryNode next = this.head;
			while (next != null && !next.key.Equals(key))
			{
				dictionaryNode = next;
				next = next.next;
			}
			if (next == null)
			{
				return;
			}
			if (next == this.head)
			{
				this.head = next.next;
			}
			else
			{
				dictionaryNode.next = next.next;
			}
			this.count--;
		}

		// Token: 0x040018DE RID: 6366
		private ListDictionaryInternal.DictionaryNode head;

		// Token: 0x040018DF RID: 6367
		private int version;

		// Token: 0x040018E0 RID: 6368
		private int count;

		// Token: 0x040018E1 RID: 6369
		[NonSerialized]
		private object _syncRoot;

		// Token: 0x02000BB3 RID: 2995
		private class NodeEnumerator : IDictionaryEnumerator, IEnumerator
		{
			// Token: 0x06006DE0 RID: 28128 RVA: 0x0017BA3C File Offset: 0x00179C3C
			public NodeEnumerator(ListDictionaryInternal list)
			{
				this.list = list;
				this.version = list.version;
				this.start = true;
				this.current = null;
			}

			// Token: 0x170012A5 RID: 4773
			// (get) Token: 0x06006DE1 RID: 28129 RVA: 0x0017BA65 File Offset: 0x00179C65
			public object Current
			{
				get
				{
					return this.Entry;
				}
			}

			// Token: 0x170012A6 RID: 4774
			// (get) Token: 0x06006DE2 RID: 28130 RVA: 0x0017BA72 File Offset: 0x00179C72
			public DictionaryEntry Entry
			{
				get
				{
					if (this.current == null)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return new DictionaryEntry(this.current.key, this.current.value);
				}
			}

			// Token: 0x170012A7 RID: 4775
			// (get) Token: 0x06006DE3 RID: 28131 RVA: 0x0017BAA7 File Offset: 0x00179CA7
			public object Key
			{
				get
				{
					if (this.current == null)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return this.current.key;
				}
			}

			// Token: 0x170012A8 RID: 4776
			// (get) Token: 0x06006DE4 RID: 28132 RVA: 0x0017BACC File Offset: 0x00179CCC
			public object Value
			{
				get
				{
					if (this.current == null)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
					}
					return this.current.value;
				}
			}

			// Token: 0x06006DE5 RID: 28133 RVA: 0x0017BAF4 File Offset: 0x00179CF4
			public bool MoveNext()
			{
				if (this.version != this.list.version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				if (this.start)
				{
					this.current = this.list.head;
					this.start = false;
				}
				else if (this.current != null)
				{
					this.current = this.current.next;
				}
				return this.current != null;
			}

			// Token: 0x06006DE6 RID: 28134 RVA: 0x0017BB68 File Offset: 0x00179D68
			public void Reset()
			{
				if (this.version != this.list.version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this.start = true;
				this.current = null;
			}

			// Token: 0x04003568 RID: 13672
			private ListDictionaryInternal list;

			// Token: 0x04003569 RID: 13673
			private ListDictionaryInternal.DictionaryNode current;

			// Token: 0x0400356A RID: 13674
			private int version;

			// Token: 0x0400356B RID: 13675
			private bool start;
		}

		// Token: 0x02000BB4 RID: 2996
		private class NodeKeyValueCollection : ICollection, IEnumerable
		{
			// Token: 0x06006DE7 RID: 28135 RVA: 0x0017BB9B File Offset: 0x00179D9B
			public NodeKeyValueCollection(ListDictionaryInternal list, bool isKeys)
			{
				this.list = list;
				this.isKeys = isKeys;
			}

			// Token: 0x06006DE8 RID: 28136 RVA: 0x0017BBB4 File Offset: 0x00179DB4
			void ICollection.CopyTo(Array array, int index)
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
				if (array.Length - index < this.list.Count)
				{
					throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
				}
				for (ListDictionaryInternal.DictionaryNode dictionaryNode = this.list.head; dictionaryNode != null; dictionaryNode = dictionaryNode.next)
				{
					array.SetValue(this.isKeys ? dictionaryNode.key : dictionaryNode.value, index);
					index++;
				}
			}

			// Token: 0x170012A9 RID: 4777
			// (get) Token: 0x06006DE9 RID: 28137 RVA: 0x0017BC68 File Offset: 0x00179E68
			int ICollection.Count
			{
				get
				{
					int num = 0;
					for (ListDictionaryInternal.DictionaryNode dictionaryNode = this.list.head; dictionaryNode != null; dictionaryNode = dictionaryNode.next)
					{
						num++;
					}
					return num;
				}
			}

			// Token: 0x170012AA RID: 4778
			// (get) Token: 0x06006DEA RID: 28138 RVA: 0x0017BC94 File Offset: 0x00179E94
			bool ICollection.IsSynchronized
			{
				get
				{
					return false;
				}
			}

			// Token: 0x170012AB RID: 4779
			// (get) Token: 0x06006DEB RID: 28139 RVA: 0x0017BC97 File Offset: 0x00179E97
			object ICollection.SyncRoot
			{
				get
				{
					return this.list.SyncRoot;
				}
			}

			// Token: 0x06006DEC RID: 28140 RVA: 0x0017BCA4 File Offset: 0x00179EA4
			IEnumerator IEnumerable.GetEnumerator()
			{
				return new ListDictionaryInternal.NodeKeyValueCollection.NodeKeyValueEnumerator(this.list, this.isKeys);
			}

			// Token: 0x0400356C RID: 13676
			private ListDictionaryInternal list;

			// Token: 0x0400356D RID: 13677
			private bool isKeys;

			// Token: 0x02000D06 RID: 3334
			private class NodeKeyValueEnumerator : IEnumerator
			{
				// Token: 0x060071FA RID: 29178 RVA: 0x00188CCB File Offset: 0x00186ECB
				public NodeKeyValueEnumerator(ListDictionaryInternal list, bool isKeys)
				{
					this.list = list;
					this.isKeys = isKeys;
					this.version = list.version;
					this.start = true;
					this.current = null;
				}

				// Token: 0x17001385 RID: 4997
				// (get) Token: 0x060071FB RID: 29179 RVA: 0x00188CFB File Offset: 0x00186EFB
				public object Current
				{
					get
					{
						if (this.current == null)
						{
							throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
						}
						if (!this.isKeys)
						{
							return this.current.value;
						}
						return this.current.key;
					}
				}

				// Token: 0x060071FC RID: 29180 RVA: 0x00188D34 File Offset: 0x00186F34
				public bool MoveNext()
				{
					if (this.version != this.list.version)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					if (this.start)
					{
						this.current = this.list.head;
						this.start = false;
					}
					else if (this.current != null)
					{
						this.current = this.current.next;
					}
					return this.current != null;
				}

				// Token: 0x060071FD RID: 29181 RVA: 0x00188DA8 File Offset: 0x00186FA8
				public void Reset()
				{
					if (this.version != this.list.version)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					this.start = true;
					this.current = null;
				}

				// Token: 0x0400393E RID: 14654
				private ListDictionaryInternal list;

				// Token: 0x0400393F RID: 14655
				private ListDictionaryInternal.DictionaryNode current;

				// Token: 0x04003940 RID: 14656
				private int version;

				// Token: 0x04003941 RID: 14657
				private bool isKeys;

				// Token: 0x04003942 RID: 14658
				private bool start;
			}
		}

		// Token: 0x02000BB5 RID: 2997
		[Serializable]
		private class DictionaryNode
		{
			// Token: 0x0400356E RID: 13678
			public object key;

			// Token: 0x0400356F RID: 13679
			public object value;

			// Token: 0x04003570 RID: 13680
			public ListDictionaryInternal.DictionaryNode next;
		}
	}
}
