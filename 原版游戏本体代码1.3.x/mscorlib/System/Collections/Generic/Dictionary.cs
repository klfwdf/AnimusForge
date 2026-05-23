using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Collections.Generic
{
	// Token: 0x020004BF RID: 1215
	[DebuggerTypeProxy(typeof(Mscorlib_DictionaryDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	[ComVisible(false)]
	[__DynamicallyInvokable]
	[Serializable]
	public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, ISerializable, IDeserializationCallback
	{
		// Token: 0x06003A3A RID: 14906 RVA: 0x000DDF94 File Offset: 0x000DC194
		[__DynamicallyInvokable]
		public Dictionary()
			: this(0, null)
		{
		}

		// Token: 0x06003A3B RID: 14907 RVA: 0x000DDF9E File Offset: 0x000DC19E
		[__DynamicallyInvokable]
		public Dictionary(int capacity)
			: this(capacity, null)
		{
		}

		// Token: 0x06003A3C RID: 14908 RVA: 0x000DDFA8 File Offset: 0x000DC1A8
		[__DynamicallyInvokable]
		public Dictionary(IEqualityComparer<TKey> comparer)
			: this(0, comparer)
		{
		}

		// Token: 0x06003A3D RID: 14909 RVA: 0x000DDFB2 File Offset: 0x000DC1B2
		[__DynamicallyInvokable]
		public Dictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			if (capacity < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
			}
			if (capacity > 0)
			{
				this.Initialize(capacity);
			}
			this.comparer = comparer ?? EqualityComparer<TKey>.Default;
		}

		// Token: 0x06003A3E RID: 14910 RVA: 0x000DDFE0 File Offset: 0x000DC1E0
		[__DynamicallyInvokable]
		public Dictionary(IDictionary<TKey, TValue> dictionary)
			: this(dictionary, null)
		{
		}

		// Token: 0x06003A3F RID: 14911 RVA: 0x000DDFEC File Offset: 0x000DC1EC
		[__DynamicallyInvokable]
		public Dictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
			: this((dictionary != null) ? dictionary.Count : 0, comparer)
		{
			if (dictionary == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
			}
			foreach (KeyValuePair<TKey, TValue> keyValuePair in dictionary)
			{
				this.Add(keyValuePair.Key, keyValuePair.Value);
			}
		}

		// Token: 0x06003A40 RID: 14912 RVA: 0x000DE060 File Offset: 0x000DC260
		protected Dictionary(SerializationInfo info, StreamingContext context)
		{
			HashHelpers.SerializationInfoTable.Add(this, info);
		}

		// Token: 0x170008CF RID: 2255
		// (get) Token: 0x06003A41 RID: 14913 RVA: 0x000DE074 File Offset: 0x000DC274
		[__DynamicallyInvokable]
		public IEqualityComparer<TKey> Comparer
		{
			[__DynamicallyInvokable]
			get
			{
				return this.comparer;
			}
		}

		// Token: 0x170008D0 RID: 2256
		// (get) Token: 0x06003A42 RID: 14914 RVA: 0x000DE07C File Offset: 0x000DC27C
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				return this.count - this.freeCount;
			}
		}

		// Token: 0x170008D1 RID: 2257
		// (get) Token: 0x06003A43 RID: 14915 RVA: 0x000DE08B File Offset: 0x000DC28B
		[__DynamicallyInvokable]
		public Dictionary<TKey, TValue>.KeyCollection Keys
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.keys == null)
				{
					this.keys = new Dictionary<TKey, TValue>.KeyCollection(this);
				}
				return this.keys;
			}
		}

		// Token: 0x170008D2 RID: 2258
		// (get) Token: 0x06003A44 RID: 14916 RVA: 0x000DE0A7 File Offset: 0x000DC2A7
		[__DynamicallyInvokable]
		ICollection<TKey> IDictionary<!0, !1>.Keys
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.keys == null)
				{
					this.keys = new Dictionary<TKey, TValue>.KeyCollection(this);
				}
				return this.keys;
			}
		}

		// Token: 0x170008D3 RID: 2259
		// (get) Token: 0x06003A45 RID: 14917 RVA: 0x000DE0C3 File Offset: 0x000DC2C3
		[__DynamicallyInvokable]
		IEnumerable<TKey> IReadOnlyDictionary<!0, !1>.Keys
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.keys == null)
				{
					this.keys = new Dictionary<TKey, TValue>.KeyCollection(this);
				}
				return this.keys;
			}
		}

		// Token: 0x170008D4 RID: 2260
		// (get) Token: 0x06003A46 RID: 14918 RVA: 0x000DE0DF File Offset: 0x000DC2DF
		[__DynamicallyInvokable]
		public Dictionary<TKey, TValue>.ValueCollection Values
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.values == null)
				{
					this.values = new Dictionary<TKey, TValue>.ValueCollection(this);
				}
				return this.values;
			}
		}

		// Token: 0x170008D5 RID: 2261
		// (get) Token: 0x06003A47 RID: 14919 RVA: 0x000DE0FB File Offset: 0x000DC2FB
		[__DynamicallyInvokable]
		ICollection<TValue> IDictionary<!0, !1>.Values
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.values == null)
				{
					this.values = new Dictionary<TKey, TValue>.ValueCollection(this);
				}
				return this.values;
			}
		}

		// Token: 0x170008D6 RID: 2262
		// (get) Token: 0x06003A48 RID: 14920 RVA: 0x000DE117 File Offset: 0x000DC317
		[__DynamicallyInvokable]
		IEnumerable<TValue> IReadOnlyDictionary<!0, !1>.Values
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.values == null)
				{
					this.values = new Dictionary<TKey, TValue>.ValueCollection(this);
				}
				return this.values;
			}
		}

		// Token: 0x170008D7 RID: 2263
		[__DynamicallyInvokable]
		public TValue this[TKey key]
		{
			[__DynamicallyInvokable]
			get
			{
				int num = this.FindEntry(key);
				if (num >= 0)
				{
					return this.entries[num].value;
				}
				ThrowHelper.ThrowKeyNotFoundException();
				return default(TValue);
			}
			[__DynamicallyInvokable]
			set
			{
				this.Insert(key, value, false);
			}
		}

		// Token: 0x06003A4B RID: 14923 RVA: 0x000DE178 File Offset: 0x000DC378
		[__DynamicallyInvokable]
		public void Add(TKey key, TValue value)
		{
			this.Insert(key, value, true);
		}

		// Token: 0x06003A4C RID: 14924 RVA: 0x000DE183 File Offset: 0x000DC383
		[__DynamicallyInvokable]
		void ICollection<KeyValuePair<!0, !1>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
		{
			this.Add(keyValuePair.Key, keyValuePair.Value);
		}

		// Token: 0x06003A4D RID: 14925 RVA: 0x000DE19C File Offset: 0x000DC39C
		[__DynamicallyInvokable]
		bool ICollection<KeyValuePair<!0, !1>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
		{
			int num = this.FindEntry(keyValuePair.Key);
			return num >= 0 && EqualityComparer<TValue>.Default.Equals(this.entries[num].value, keyValuePair.Value);
		}

		// Token: 0x06003A4E RID: 14926 RVA: 0x000DE1E4 File Offset: 0x000DC3E4
		[__DynamicallyInvokable]
		bool ICollection<KeyValuePair<!0, !1>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
		{
			int num = this.FindEntry(keyValuePair.Key);
			if (num >= 0 && EqualityComparer<TValue>.Default.Equals(this.entries[num].value, keyValuePair.Value))
			{
				this.Remove(keyValuePair.Key);
				return true;
			}
			return false;
		}

		// Token: 0x06003A4F RID: 14927 RVA: 0x000DE238 File Offset: 0x000DC438
		[__DynamicallyInvokable]
		public void Clear()
		{
			if (this.count > 0)
			{
				for (int i = 0; i < this.buckets.Length; i++)
				{
					this.buckets[i] = -1;
				}
				Array.Clear(this.entries, 0, this.count);
				this.freeList = -1;
				this.count = 0;
				this.freeCount = 0;
				this.version++;
			}
		}

		// Token: 0x06003A50 RID: 14928 RVA: 0x000DE29F File Offset: 0x000DC49F
		[__DynamicallyInvokable]
		public bool ContainsKey(TKey key)
		{
			return this.FindEntry(key) >= 0;
		}

		// Token: 0x06003A51 RID: 14929 RVA: 0x000DE2B0 File Offset: 0x000DC4B0
		[__DynamicallyInvokable]
		public bool ContainsValue(TValue value)
		{
			if (value == null)
			{
				for (int i = 0; i < this.count; i++)
				{
					if (this.entries[i].hashCode >= 0 && this.entries[i].value == null)
					{
						return true;
					}
				}
			}
			else
			{
				EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
				for (int j = 0; j < this.count; j++)
				{
					if (this.entries[j].hashCode >= 0 && @default.Equals(this.entries[j].value, value))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06003A52 RID: 14930 RVA: 0x000DE350 File Offset: 0x000DC550
		private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			if (index < 0 || index > array.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < this.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			int num = this.count;
			Dictionary<TKey, TValue>.Entry[] array2 = this.entries;
			for (int i = 0; i < num; i++)
			{
				if (array2[i].hashCode >= 0)
				{
					array[index++] = new KeyValuePair<TKey, TValue>(array2[i].key, array2[i].value);
				}
			}
		}

		// Token: 0x06003A53 RID: 14931 RVA: 0x000DE3DD File Offset: 0x000DC5DD
		[__DynamicallyInvokable]
		public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
		{
			return new Dictionary<TKey, TValue>.Enumerator(this, 2);
		}

		// Token: 0x06003A54 RID: 14932 RVA: 0x000DE3E6 File Offset: 0x000DC5E6
		[__DynamicallyInvokable]
		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<!0, !1>>.GetEnumerator()
		{
			return new Dictionary<TKey, TValue>.Enumerator(this, 2);
		}

		// Token: 0x06003A55 RID: 14933 RVA: 0x000DE3F4 File Offset: 0x000DC5F4
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
			}
			info.AddValue("Version", this.version);
			info.AddValue("Comparer", HashHelpers.GetEqualityComparerForSerialization(this.comparer), typeof(IEqualityComparer<TKey>));
			info.AddValue("HashSize", (this.buckets == null) ? 0 : this.buckets.Length);
			if (this.buckets != null)
			{
				KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[this.Count];
				this.CopyTo(array, 0);
				info.AddValue("KeyValuePairs", array, typeof(KeyValuePair<TKey, TValue>[]));
			}
		}

		// Token: 0x06003A56 RID: 14934 RVA: 0x000DE48C File Offset: 0x000DC68C
		private int FindEntry(TKey key)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			if (this.buckets != null)
			{
				int num = this.comparer.GetHashCode(key) & int.MaxValue;
				for (int i = this.buckets[num % this.buckets.Length]; i >= 0; i = this.entries[i].next)
				{
					if (this.entries[i].hashCode == num && this.comparer.Equals(this.entries[i].key, key))
					{
						return i;
					}
				}
			}
			return -1;
		}

		// Token: 0x06003A57 RID: 14935 RVA: 0x000DE524 File Offset: 0x000DC724
		private void Initialize(int capacity)
		{
			int prime = HashHelpers.GetPrime(capacity);
			this.buckets = new int[prime];
			for (int i = 0; i < this.buckets.Length; i++)
			{
				this.buckets[i] = -1;
			}
			this.entries = new Dictionary<TKey, TValue>.Entry[prime];
			this.freeList = -1;
		}

		// Token: 0x06003A58 RID: 14936 RVA: 0x000DE574 File Offset: 0x000DC774
		private void Insert(TKey key, TValue value, bool add)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			if (this.buckets == null)
			{
				this.Initialize(0);
			}
			int num = this.comparer.GetHashCode(key) & int.MaxValue;
			int num2 = num % this.buckets.Length;
			int num3 = 0;
			for (int i = this.buckets[num2]; i >= 0; i = this.entries[i].next)
			{
				if (this.entries[i].hashCode == num && this.comparer.Equals(this.entries[i].key, key))
				{
					if (add)
					{
						ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
					}
					this.entries[i].value = value;
					this.version++;
					return;
				}
				num3++;
			}
			int num4;
			if (this.freeCount > 0)
			{
				num4 = this.freeList;
				this.freeList = this.entries[num4].next;
				this.freeCount--;
			}
			else
			{
				if (this.count == this.entries.Length)
				{
					this.Resize();
					num2 = num % this.buckets.Length;
				}
				num4 = this.count;
				this.count++;
			}
			this.entries[num4].hashCode = num;
			this.entries[num4].next = this.buckets[num2];
			this.entries[num4].key = key;
			this.entries[num4].value = value;
			this.buckets[num2] = num4;
			this.version++;
			if (num3 > 100 && HashHelpers.IsWellKnownEqualityComparer(this.comparer))
			{
				this.comparer = (IEqualityComparer<TKey>)HashHelpers.GetRandomizedEqualityComparer(this.comparer);
				this.Resize(this.entries.Length, true);
			}
		}

		// Token: 0x06003A59 RID: 14937 RVA: 0x000DE754 File Offset: 0x000DC954
		public virtual void OnDeserialization(object sender)
		{
			SerializationInfo serializationInfo;
			HashHelpers.SerializationInfoTable.TryGetValue(this, out serializationInfo);
			if (serializationInfo == null)
			{
				return;
			}
			int @int = serializationInfo.GetInt32("Version");
			int int2 = serializationInfo.GetInt32("HashSize");
			this.comparer = (IEqualityComparer<TKey>)serializationInfo.GetValue("Comparer", typeof(IEqualityComparer<TKey>));
			if (int2 != 0)
			{
				this.buckets = new int[int2];
				for (int i = 0; i < this.buckets.Length; i++)
				{
					this.buckets[i] = -1;
				}
				this.entries = new Dictionary<TKey, TValue>.Entry[int2];
				this.freeList = -1;
				KeyValuePair<TKey, TValue>[] array = (KeyValuePair<TKey, TValue>[])serializationInfo.GetValue("KeyValuePairs", typeof(KeyValuePair<TKey, TValue>[]));
				if (array == null)
				{
					ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
				}
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j].Key == null)
					{
						ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
					}
					this.Insert(array[j].Key, array[j].Value, true);
				}
			}
			else
			{
				this.buckets = null;
			}
			this.version = @int;
			HashHelpers.SerializationInfoTable.Remove(this);
		}

		// Token: 0x06003A5A RID: 14938 RVA: 0x000DE880 File Offset: 0x000DCA80
		private void Resize()
		{
			this.Resize(HashHelpers.ExpandPrime(this.count), false);
		}

		// Token: 0x06003A5B RID: 14939 RVA: 0x000DE894 File Offset: 0x000DCA94
		private void Resize(int newSize, bool forceNewHashCodes)
		{
			int[] array = new int[newSize];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = -1;
			}
			Dictionary<TKey, TValue>.Entry[] array2 = new Dictionary<TKey, TValue>.Entry[newSize];
			Array.Copy(this.entries, 0, array2, 0, this.count);
			if (forceNewHashCodes)
			{
				for (int j = 0; j < this.count; j++)
				{
					if (array2[j].hashCode != -1)
					{
						array2[j].hashCode = this.comparer.GetHashCode(array2[j].key) & int.MaxValue;
					}
				}
			}
			for (int k = 0; k < this.count; k++)
			{
				if (array2[k].hashCode >= 0)
				{
					int num = array2[k].hashCode % newSize;
					array2[k].next = array[num];
					array[num] = k;
				}
			}
			this.buckets = array;
			this.entries = array2;
		}

		// Token: 0x06003A5C RID: 14940 RVA: 0x000DE97C File Offset: 0x000DCB7C
		[__DynamicallyInvokable]
		public bool Remove(TKey key)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			if (this.buckets != null)
			{
				int num = this.comparer.GetHashCode(key) & int.MaxValue;
				int num2 = num % this.buckets.Length;
				int num3 = -1;
				for (int i = this.buckets[num2]; i >= 0; i = this.entries[i].next)
				{
					if (this.entries[i].hashCode == num && this.comparer.Equals(this.entries[i].key, key))
					{
						if (num3 < 0)
						{
							this.buckets[num2] = this.entries[i].next;
						}
						else
						{
							this.entries[num3].next = this.entries[i].next;
						}
						this.entries[i].hashCode = -1;
						this.entries[i].next = this.freeList;
						this.entries[i].key = default(TKey);
						this.entries[i].value = default(TValue);
						this.freeList = i;
						this.freeCount++;
						this.version++;
						return true;
					}
					num3 = i;
				}
			}
			return false;
		}

		// Token: 0x06003A5D RID: 14941 RVA: 0x000DEAE4 File Offset: 0x000DCCE4
		[__DynamicallyInvokable]
		public bool TryGetValue(TKey key, out TValue value)
		{
			int num = this.FindEntry(key);
			if (num >= 0)
			{
				value = this.entries[num].value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		// Token: 0x06003A5E RID: 14942 RVA: 0x000DEB20 File Offset: 0x000DCD20
		internal TValue GetValueOrDefault(TKey key)
		{
			int num = this.FindEntry(key);
			if (num >= 0)
			{
				return this.entries[num].value;
			}
			return default(TValue);
		}

		// Token: 0x170008D8 RID: 2264
		// (get) Token: 0x06003A5F RID: 14943 RVA: 0x000DEB54 File Offset: 0x000DCD54
		[__DynamicallyInvokable]
		bool ICollection<KeyValuePair<!0, !1>>.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x06003A60 RID: 14944 RVA: 0x000DEB57 File Offset: 0x000DCD57
		[__DynamicallyInvokable]
		void ICollection<KeyValuePair<!0, !1>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			this.CopyTo(array, index);
		}

		// Token: 0x06003A61 RID: 14945 RVA: 0x000DEB64 File Offset: 0x000DCD64
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
			if (index < 0 || index > array.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < this.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			KeyValuePair<TKey, TValue>[] array2 = array as KeyValuePair<TKey, TValue>[];
			if (array2 != null)
			{
				this.CopyTo(array2, index);
				return;
			}
			if (array is DictionaryEntry[])
			{
				DictionaryEntry[] array3 = array as DictionaryEntry[];
				Dictionary<TKey, TValue>.Entry[] array4 = this.entries;
				for (int i = 0; i < this.count; i++)
				{
					if (array4[i].hashCode >= 0)
					{
						array3[index++] = new DictionaryEntry(array4[i].key, array4[i].value);
					}
				}
				return;
			}
			object[] array5 = array as object[];
			if (array5 == null)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
			try
			{
				int num = this.count;
				Dictionary<TKey, TValue>.Entry[] array6 = this.entries;
				for (int j = 0; j < num; j++)
				{
					if (array6[j].hashCode >= 0)
					{
						array5[index++] = new KeyValuePair<TKey, TValue>(array6[j].key, array6[j].value);
					}
				}
			}
			catch (ArrayTypeMismatchException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
		}

		// Token: 0x06003A62 RID: 14946 RVA: 0x000DECD4 File Offset: 0x000DCED4
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Dictionary<TKey, TValue>.Enumerator(this, 2);
		}

		// Token: 0x170008D9 RID: 2265
		// (get) Token: 0x06003A63 RID: 14947 RVA: 0x000DECE2 File Offset: 0x000DCEE2
		[__DynamicallyInvokable]
		bool ICollection.IsSynchronized
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008DA RID: 2266
		// (get) Token: 0x06003A64 RID: 14948 RVA: 0x000DECE5 File Offset: 0x000DCEE5
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

		// Token: 0x170008DB RID: 2267
		// (get) Token: 0x06003A65 RID: 14949 RVA: 0x000DED07 File Offset: 0x000DCF07
		[__DynamicallyInvokable]
		bool IDictionary.IsFixedSize
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008DC RID: 2268
		// (get) Token: 0x06003A66 RID: 14950 RVA: 0x000DED0A File Offset: 0x000DCF0A
		[__DynamicallyInvokable]
		bool IDictionary.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008DD RID: 2269
		// (get) Token: 0x06003A67 RID: 14951 RVA: 0x000DED0D File Offset: 0x000DCF0D
		[__DynamicallyInvokable]
		ICollection IDictionary.Keys
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Keys;
			}
		}

		// Token: 0x170008DE RID: 2270
		// (get) Token: 0x06003A68 RID: 14952 RVA: 0x000DED15 File Offset: 0x000DCF15
		[__DynamicallyInvokable]
		ICollection IDictionary.Values
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Values;
			}
		}

		// Token: 0x170008DF RID: 2271
		[__DynamicallyInvokable]
		object IDictionary.this[object key]
		{
			[__DynamicallyInvokable]
			get
			{
				if (Dictionary<TKey, TValue>.IsCompatibleKey(key))
				{
					int num = this.FindEntry((TKey)((object)key));
					if (num >= 0)
					{
						return this.entries[num].value;
					}
				}
				return null;
			}
			[__DynamicallyInvokable]
			set
			{
				if (key == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
				}
				ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);
				try
				{
					TKey key2 = (TKey)((object)key);
					try
					{
						this[key2] = (TValue)((object)value);
					}
					catch (InvalidCastException)
					{
						ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
					}
				}
				catch (InvalidCastException)
				{
					ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
				}
			}
		}

		// Token: 0x06003A6B RID: 14955 RVA: 0x000DEDD8 File Offset: 0x000DCFD8
		private static bool IsCompatibleKey(object key)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			return key is TKey;
		}

		// Token: 0x06003A6C RID: 14956 RVA: 0x000DEDEC File Offset: 0x000DCFEC
		[__DynamicallyInvokable]
		void IDictionary.Add(object key, object value)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);
			try
			{
				TKey key2 = (TKey)((object)key);
				try
				{
					this.Add(key2, (TValue)((object)value));
				}
				catch (InvalidCastException)
				{
					ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
				}
			}
			catch (InvalidCastException)
			{
				ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
			}
		}

		// Token: 0x06003A6D RID: 14957 RVA: 0x000DEE64 File Offset: 0x000DD064
		[__DynamicallyInvokable]
		bool IDictionary.Contains(object key)
		{
			return Dictionary<TKey, TValue>.IsCompatibleKey(key) && this.ContainsKey((TKey)((object)key));
		}

		// Token: 0x06003A6E RID: 14958 RVA: 0x000DEE7C File Offset: 0x000DD07C
		[__DynamicallyInvokable]
		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return new Dictionary<TKey, TValue>.Enumerator(this, 1);
		}

		// Token: 0x06003A6F RID: 14959 RVA: 0x000DEE8A File Offset: 0x000DD08A
		[__DynamicallyInvokable]
		void IDictionary.Remove(object key)
		{
			if (Dictionary<TKey, TValue>.IsCompatibleKey(key))
			{
				this.Remove((TKey)((object)key));
			}
		}

		// Token: 0x04001943 RID: 6467
		private int[] buckets;

		// Token: 0x04001944 RID: 6468
		private Dictionary<TKey, TValue>.Entry[] entries;

		// Token: 0x04001945 RID: 6469
		private int count;

		// Token: 0x04001946 RID: 6470
		private int version;

		// Token: 0x04001947 RID: 6471
		private int freeList;

		// Token: 0x04001948 RID: 6472
		private int freeCount;

		// Token: 0x04001949 RID: 6473
		private IEqualityComparer<TKey> comparer;

		// Token: 0x0400194A RID: 6474
		private Dictionary<TKey, TValue>.KeyCollection keys;

		// Token: 0x0400194B RID: 6475
		private Dictionary<TKey, TValue>.ValueCollection values;

		// Token: 0x0400194C RID: 6476
		private object _syncRoot;

		// Token: 0x0400194D RID: 6477
		private const string VersionName = "Version";

		// Token: 0x0400194E RID: 6478
		private const string HashSizeName = "HashSize";

		// Token: 0x0400194F RID: 6479
		private const string KeyValuePairsName = "KeyValuePairs";

		// Token: 0x04001950 RID: 6480
		private const string ComparerName = "Comparer";

		// Token: 0x02000BE0 RID: 3040
		private struct Entry
		{
			// Token: 0x040035ED RID: 13805
			public int hashCode;

			// Token: 0x040035EE RID: 13806
			public int next;

			// Token: 0x040035EF RID: 13807
			public TKey key;

			// Token: 0x040035F0 RID: 13808
			public TValue value;
		}

		// Token: 0x02000BE1 RID: 3041
		[__DynamicallyInvokable]
		[Serializable]
		public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
		{
			// Token: 0x06006F04 RID: 28420 RVA: 0x0017E642 File Offset: 0x0017C842
			internal Enumerator(Dictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
			{
				this.dictionary = dictionary;
				this.version = dictionary.version;
				this.index = 0;
				this.getEnumeratorRetType = getEnumeratorRetType;
				this.current = default(KeyValuePair<TKey, TValue>);
			}

			// Token: 0x06006F05 RID: 28421 RVA: 0x0017E674 File Offset: 0x0017C874
			[__DynamicallyInvokable]
			public bool MoveNext()
			{
				if (this.version != this.dictionary.version)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
				}
				while (this.index < this.dictionary.count)
				{
					if (this.dictionary.entries[this.index].hashCode >= 0)
					{
						this.current = new KeyValuePair<TKey, TValue>(this.dictionary.entries[this.index].key, this.dictionary.entries[this.index].value);
						this.index++;
						return true;
					}
					this.index++;
				}
				this.index = this.dictionary.count + 1;
				this.current = default(KeyValuePair<TKey, TValue>);
				return false;
			}

			// Token: 0x17001309 RID: 4873
			// (get) Token: 0x06006F06 RID: 28422 RVA: 0x0017E753 File Offset: 0x0017C953
			[__DynamicallyInvokable]
			public KeyValuePair<TKey, TValue> Current
			{
				[__DynamicallyInvokable]
				get
				{
					return this.current;
				}
			}

			// Token: 0x06006F07 RID: 28423 RVA: 0x0017E75B File Offset: 0x0017C95B
			[__DynamicallyInvokable]
			public void Dispose()
			{
			}

			// Token: 0x1700130A RID: 4874
			// (get) Token: 0x06006F08 RID: 28424 RVA: 0x0017E760 File Offset: 0x0017C960
			[__DynamicallyInvokable]
			object IEnumerator.Current
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.index == 0 || this.index == this.dictionary.count + 1)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
					}
					if (this.getEnumeratorRetType == 1)
					{
						return new DictionaryEntry(this.current.Key, this.current.Value);
					}
					return new KeyValuePair<TKey, TValue>(this.current.Key, this.current.Value);
				}
			}

			// Token: 0x06006F09 RID: 28425 RVA: 0x0017E7E5 File Offset: 0x0017C9E5
			[__DynamicallyInvokable]
			void IEnumerator.Reset()
			{
				if (this.version != this.dictionary.version)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
				}
				this.index = 0;
				this.current = default(KeyValuePair<TKey, TValue>);
			}

			// Token: 0x1700130B RID: 4875
			// (get) Token: 0x06006F0A RID: 28426 RVA: 0x0017E814 File Offset: 0x0017CA14
			[__DynamicallyInvokable]
			DictionaryEntry IDictionaryEnumerator.Entry
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.index == 0 || this.index == this.dictionary.count + 1)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
					}
					return new DictionaryEntry(this.current.Key, this.current.Value);
				}
			}

			// Token: 0x1700130C RID: 4876
			// (get) Token: 0x06006F0B RID: 28427 RVA: 0x0017E86A File Offset: 0x0017CA6A
			[__DynamicallyInvokable]
			object IDictionaryEnumerator.Key
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.index == 0 || this.index == this.dictionary.count + 1)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
					}
					return this.current.Key;
				}
			}

			// Token: 0x1700130D RID: 4877
			// (get) Token: 0x06006F0C RID: 28428 RVA: 0x0017E8A0 File Offset: 0x0017CAA0
			[__DynamicallyInvokable]
			object IDictionaryEnumerator.Value
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.index == 0 || this.index == this.dictionary.count + 1)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
					}
					return this.current.Value;
				}
			}

			// Token: 0x040035F1 RID: 13809
			private Dictionary<TKey, TValue> dictionary;

			// Token: 0x040035F2 RID: 13810
			private int version;

			// Token: 0x040035F3 RID: 13811
			private int index;

			// Token: 0x040035F4 RID: 13812
			private KeyValuePair<TKey, TValue> current;

			// Token: 0x040035F5 RID: 13813
			private int getEnumeratorRetType;

			// Token: 0x040035F6 RID: 13814
			internal const int DictEntry = 1;

			// Token: 0x040035F7 RID: 13815
			internal const int KeyValuePair = 2;
		}

		// Token: 0x02000BE2 RID: 3042
		[DebuggerTypeProxy(typeof(Mscorlib_DictionaryKeyCollectionDebugView<, >))]
		[DebuggerDisplay("Count = {Count}")]
		[__DynamicallyInvokable]
		[Serializable]
		public sealed class KeyCollection : ICollection<!0>, IEnumerable<!0>, IEnumerable, ICollection, IReadOnlyCollection<TKey>
		{
			// Token: 0x06006F0D RID: 28429 RVA: 0x0017E8D6 File Offset: 0x0017CAD6
			[__DynamicallyInvokable]
			public KeyCollection(Dictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
				}
				this.dictionary = dictionary;
			}

			// Token: 0x06006F0E RID: 28430 RVA: 0x0017E8EE File Offset: 0x0017CAEE
			[__DynamicallyInvokable]
			public Dictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator()
			{
				return new Dictionary<TKey, TValue>.KeyCollection.Enumerator(this.dictionary);
			}

			// Token: 0x06006F0F RID: 28431 RVA: 0x0017E8FC File Offset: 0x0017CAFC
			[__DynamicallyInvokable]
			public void CopyTo(TKey[] array, int index)
			{
				if (array == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
				}
				if (index < 0 || index > array.Length)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
				}
				if (array.Length - index < this.dictionary.Count)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
				}
				int count = this.dictionary.count;
				Dictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
				for (int i = 0; i < count; i++)
				{
					if (entries[i].hashCode >= 0)
					{
						array[index++] = entries[i].key;
					}
				}
			}

			// Token: 0x1700130E RID: 4878
			// (get) Token: 0x06006F10 RID: 28432 RVA: 0x0017E987 File Offset: 0x0017CB87
			[__DynamicallyInvokable]
			public int Count
			{
				[__DynamicallyInvokable]
				get
				{
					return this.dictionary.Count;
				}
			}

			// Token: 0x1700130F RID: 4879
			// (get) Token: 0x06006F11 RID: 28433 RVA: 0x0017E994 File Offset: 0x0017CB94
			[__DynamicallyInvokable]
			bool ICollection<!0>.IsReadOnly
			{
				[__DynamicallyInvokable]
				get
				{
					return true;
				}
			}

			// Token: 0x06006F12 RID: 28434 RVA: 0x0017E997 File Offset: 0x0017CB97
			[__DynamicallyInvokable]
			void ICollection<!0>.Add(TKey item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
			}

			// Token: 0x06006F13 RID: 28435 RVA: 0x0017E9A0 File Offset: 0x0017CBA0
			[__DynamicallyInvokable]
			void ICollection<!0>.Clear()
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
			}

			// Token: 0x06006F14 RID: 28436 RVA: 0x0017E9A9 File Offset: 0x0017CBA9
			[__DynamicallyInvokable]
			bool ICollection<!0>.Contains(TKey item)
			{
				return this.dictionary.ContainsKey(item);
			}

			// Token: 0x06006F15 RID: 28437 RVA: 0x0017E9B7 File Offset: 0x0017CBB7
			[__DynamicallyInvokable]
			bool ICollection<!0>.Remove(TKey item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
				return false;
			}

			// Token: 0x06006F16 RID: 28438 RVA: 0x0017E9C1 File Offset: 0x0017CBC1
			[__DynamicallyInvokable]
			IEnumerator<TKey> IEnumerable<!0>.GetEnumerator()
			{
				return new Dictionary<TKey, TValue>.KeyCollection.Enumerator(this.dictionary);
			}

			// Token: 0x06006F17 RID: 28439 RVA: 0x0017E9D3 File Offset: 0x0017CBD3
			[__DynamicallyInvokable]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return new Dictionary<TKey, TValue>.KeyCollection.Enumerator(this.dictionary);
			}

			// Token: 0x06006F18 RID: 28440 RVA: 0x0017E9E8 File Offset: 0x0017CBE8
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
				if (index < 0 || index > array.Length)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
				}
				if (array.Length - index < this.dictionary.Count)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
				}
				TKey[] array2 = array as TKey[];
				if (array2 != null)
				{
					this.CopyTo(array2, index);
					return;
				}
				object[] array3 = array as object[];
				if (array3 == null)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
				}
				int count = this.dictionary.count;
				Dictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
				try
				{
					for (int i = 0; i < count; i++)
					{
						if (entries[i].hashCode >= 0)
						{
							array3[index++] = entries[i].key;
						}
					}
				}
				catch (ArrayTypeMismatchException)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
				}
			}

			// Token: 0x17001310 RID: 4880
			// (get) Token: 0x06006F19 RID: 28441 RVA: 0x0017EAE0 File Offset: 0x0017CCE0
			[__DynamicallyInvokable]
			bool ICollection.IsSynchronized
			{
				[__DynamicallyInvokable]
				get
				{
					return false;
				}
			}

			// Token: 0x17001311 RID: 4881
			// (get) Token: 0x06006F1A RID: 28442 RVA: 0x0017EAE3 File Offset: 0x0017CCE3
			[__DynamicallyInvokable]
			object ICollection.SyncRoot
			{
				[__DynamicallyInvokable]
				get
				{
					return ((ICollection)this.dictionary).SyncRoot;
				}
			}

			// Token: 0x040035F8 RID: 13816
			private Dictionary<TKey, TValue> dictionary;

			// Token: 0x02000D0D RID: 3341
			[__DynamicallyInvokable]
			[Serializable]
			public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator
			{
				// Token: 0x06007218 RID: 29208 RVA: 0x00189516 File Offset: 0x00187716
				internal Enumerator(Dictionary<TKey, TValue> dictionary)
				{
					this.dictionary = dictionary;
					this.version = dictionary.version;
					this.index = 0;
					this.currentKey = default(TKey);
				}

				// Token: 0x06007219 RID: 29209 RVA: 0x0018953E File Offset: 0x0018773E
				[__DynamicallyInvokable]
				public void Dispose()
				{
				}

				// Token: 0x0600721A RID: 29210 RVA: 0x00189540 File Offset: 0x00187740
				[__DynamicallyInvokable]
				public bool MoveNext()
				{
					if (this.version != this.dictionary.version)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
					}
					while (this.index < this.dictionary.count)
					{
						if (this.dictionary.entries[this.index].hashCode >= 0)
						{
							this.currentKey = this.dictionary.entries[this.index].key;
							this.index++;
							return true;
						}
						this.index++;
					}
					this.index = this.dictionary.count + 1;
					this.currentKey = default(TKey);
					return false;
				}

				// Token: 0x1700138C RID: 5004
				// (get) Token: 0x0600721B RID: 29211 RVA: 0x001895F9 File Offset: 0x001877F9
				[__DynamicallyInvokable]
				public TKey Current
				{
					[__DynamicallyInvokable]
					get
					{
						return this.currentKey;
					}
				}

				// Token: 0x1700138D RID: 5005
				// (get) Token: 0x0600721C RID: 29212 RVA: 0x00189601 File Offset: 0x00187801
				[__DynamicallyInvokable]
				object IEnumerator.Current
				{
					[__DynamicallyInvokable]
					get
					{
						if (this.index == 0 || this.index == this.dictionary.count + 1)
						{
							ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
						}
						return this.currentKey;
					}
				}

				// Token: 0x0600721D RID: 29213 RVA: 0x00189632 File Offset: 0x00187832
				[__DynamicallyInvokable]
				void IEnumerator.Reset()
				{
					if (this.version != this.dictionary.version)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
					}
					this.index = 0;
					this.currentKey = default(TKey);
				}

				// Token: 0x04003958 RID: 14680
				private Dictionary<TKey, TValue> dictionary;

				// Token: 0x04003959 RID: 14681
				private int index;

				// Token: 0x0400395A RID: 14682
				private int version;

				// Token: 0x0400395B RID: 14683
				private TKey currentKey;
			}
		}

		// Token: 0x02000BE3 RID: 3043
		[DebuggerTypeProxy(typeof(Mscorlib_DictionaryValueCollectionDebugView<, >))]
		[DebuggerDisplay("Count = {Count}")]
		[__DynamicallyInvokable]
		[Serializable]
		public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
		{
			// Token: 0x06006F1B RID: 28443 RVA: 0x0017EAF0 File Offset: 0x0017CCF0
			[__DynamicallyInvokable]
			public ValueCollection(Dictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
				}
				this.dictionary = dictionary;
			}

			// Token: 0x06006F1C RID: 28444 RVA: 0x0017EB08 File Offset: 0x0017CD08
			[__DynamicallyInvokable]
			public Dictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator()
			{
				return new Dictionary<TKey, TValue>.ValueCollection.Enumerator(this.dictionary);
			}

			// Token: 0x06006F1D RID: 28445 RVA: 0x0017EB18 File Offset: 0x0017CD18
			[__DynamicallyInvokable]
			public void CopyTo(TValue[] array, int index)
			{
				if (array == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
				}
				if (index < 0 || index > array.Length)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
				}
				if (array.Length - index < this.dictionary.Count)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
				}
				int count = this.dictionary.count;
				Dictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
				for (int i = 0; i < count; i++)
				{
					if (entries[i].hashCode >= 0)
					{
						array[index++] = entries[i].value;
					}
				}
			}

			// Token: 0x17001312 RID: 4882
			// (get) Token: 0x06006F1E RID: 28446 RVA: 0x0017EBA3 File Offset: 0x0017CDA3
			[__DynamicallyInvokable]
			public int Count
			{
				[__DynamicallyInvokable]
				get
				{
					return this.dictionary.Count;
				}
			}

			// Token: 0x17001313 RID: 4883
			// (get) Token: 0x06006F1F RID: 28447 RVA: 0x0017EBB0 File Offset: 0x0017CDB0
			[__DynamicallyInvokable]
			bool ICollection<!1>.IsReadOnly
			{
				[__DynamicallyInvokable]
				get
				{
					return true;
				}
			}

			// Token: 0x06006F20 RID: 28448 RVA: 0x0017EBB3 File Offset: 0x0017CDB3
			[__DynamicallyInvokable]
			void ICollection<!1>.Add(TValue item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
			}

			// Token: 0x06006F21 RID: 28449 RVA: 0x0017EBBC File Offset: 0x0017CDBC
			[__DynamicallyInvokable]
			bool ICollection<!1>.Remove(TValue item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
				return false;
			}

			// Token: 0x06006F22 RID: 28450 RVA: 0x0017EBC6 File Offset: 0x0017CDC6
			[__DynamicallyInvokable]
			void ICollection<!1>.Clear()
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
			}

			// Token: 0x06006F23 RID: 28451 RVA: 0x0017EBCF File Offset: 0x0017CDCF
			[__DynamicallyInvokable]
			bool ICollection<!1>.Contains(TValue item)
			{
				return this.dictionary.ContainsValue(item);
			}

			// Token: 0x06006F24 RID: 28452 RVA: 0x0017EBDD File Offset: 0x0017CDDD
			[__DynamicallyInvokable]
			IEnumerator<TValue> IEnumerable<!1>.GetEnumerator()
			{
				return new Dictionary<TKey, TValue>.ValueCollection.Enumerator(this.dictionary);
			}

			// Token: 0x06006F25 RID: 28453 RVA: 0x0017EBEF File Offset: 0x0017CDEF
			[__DynamicallyInvokable]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return new Dictionary<TKey, TValue>.ValueCollection.Enumerator(this.dictionary);
			}

			// Token: 0x06006F26 RID: 28454 RVA: 0x0017EC04 File Offset: 0x0017CE04
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
				if (index < 0 || index > array.Length)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
				}
				if (array.Length - index < this.dictionary.Count)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
				}
				TValue[] array2 = array as TValue[];
				if (array2 != null)
				{
					this.CopyTo(array2, index);
					return;
				}
				object[] array3 = array as object[];
				if (array3 == null)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
				}
				int count = this.dictionary.count;
				Dictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
				try
				{
					for (int i = 0; i < count; i++)
					{
						if (entries[i].hashCode >= 0)
						{
							array3[index++] = entries[i].value;
						}
					}
				}
				catch (ArrayTypeMismatchException)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
				}
			}

			// Token: 0x17001314 RID: 4884
			// (get) Token: 0x06006F27 RID: 28455 RVA: 0x0017ECFC File Offset: 0x0017CEFC
			[__DynamicallyInvokable]
			bool ICollection.IsSynchronized
			{
				[__DynamicallyInvokable]
				get
				{
					return false;
				}
			}

			// Token: 0x17001315 RID: 4885
			// (get) Token: 0x06006F28 RID: 28456 RVA: 0x0017ECFF File Offset: 0x0017CEFF
			[__DynamicallyInvokable]
			object ICollection.SyncRoot
			{
				[__DynamicallyInvokable]
				get
				{
					return ((ICollection)this.dictionary).SyncRoot;
				}
			}

			// Token: 0x040035F9 RID: 13817
			private Dictionary<TKey, TValue> dictionary;

			// Token: 0x02000D0E RID: 3342
			[__DynamicallyInvokable]
			[Serializable]
			public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
			{
				// Token: 0x0600721E RID: 29214 RVA: 0x00189661 File Offset: 0x00187861
				internal Enumerator(Dictionary<TKey, TValue> dictionary)
				{
					this.dictionary = dictionary;
					this.version = dictionary.version;
					this.index = 0;
					this.currentValue = default(TValue);
				}

				// Token: 0x0600721F RID: 29215 RVA: 0x00189689 File Offset: 0x00187889
				[__DynamicallyInvokable]
				public void Dispose()
				{
				}

				// Token: 0x06007220 RID: 29216 RVA: 0x0018968C File Offset: 0x0018788C
				[__DynamicallyInvokable]
				public bool MoveNext()
				{
					if (this.version != this.dictionary.version)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
					}
					while (this.index < this.dictionary.count)
					{
						if (this.dictionary.entries[this.index].hashCode >= 0)
						{
							this.currentValue = this.dictionary.entries[this.index].value;
							this.index++;
							return true;
						}
						this.index++;
					}
					this.index = this.dictionary.count + 1;
					this.currentValue = default(TValue);
					return false;
				}

				// Token: 0x1700138E RID: 5006
				// (get) Token: 0x06007221 RID: 29217 RVA: 0x00189745 File Offset: 0x00187945
				[__DynamicallyInvokable]
				public TValue Current
				{
					[__DynamicallyInvokable]
					get
					{
						return this.currentValue;
					}
				}

				// Token: 0x1700138F RID: 5007
				// (get) Token: 0x06007222 RID: 29218 RVA: 0x0018974D File Offset: 0x0018794D
				[__DynamicallyInvokable]
				object IEnumerator.Current
				{
					[__DynamicallyInvokable]
					get
					{
						if (this.index == 0 || this.index == this.dictionary.count + 1)
						{
							ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
						}
						return this.currentValue;
					}
				}

				// Token: 0x06007223 RID: 29219 RVA: 0x0018977E File Offset: 0x0018797E
				[__DynamicallyInvokable]
				void IEnumerator.Reset()
				{
					if (this.version != this.dictionary.version)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
					}
					this.index = 0;
					this.currentValue = default(TValue);
				}

				// Token: 0x0400395C RID: 14684
				private Dictionary<TKey, TValue> dictionary;

				// Token: 0x0400395D RID: 14685
				private int index;

				// Token: 0x0400395E RID: 14686
				private int version;

				// Token: 0x0400395F RID: 14687
				private TValue currentValue;
			}
		}
	}
}
