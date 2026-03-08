using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Collections.ObjectModel
{
	// Token: 0x020004B7 RID: 1207
	[DebuggerTypeProxy(typeof(Mscorlib_DictionaryDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	[__DynamicallyInvokable]
	[Serializable]
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<!0, !1>, ICollection<KeyValuePair<!0, !1>>, IEnumerable<KeyValuePair<!0, !1>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<!0, !1>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		// Token: 0x060039EB RID: 14827 RVA: 0x000DD390 File Offset: 0x000DB590
		[__DynamicallyInvokable]
		public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.m_dictionary = dictionary;
		}

		// Token: 0x170008B9 RID: 2233
		// (get) Token: 0x060039EC RID: 14828 RVA: 0x000DD3AD File Offset: 0x000DB5AD
		[__DynamicallyInvokable]
		protected IDictionary<TKey, TValue> Dictionary
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_dictionary;
			}
		}

		// Token: 0x170008BA RID: 2234
		// (get) Token: 0x060039ED RID: 14829 RVA: 0x000DD3B5 File Offset: 0x000DB5B5
		[__DynamicallyInvokable]
		public ReadOnlyDictionary<TKey, TValue>.KeyCollection Keys
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_keys == null)
				{
					this.m_keys = new ReadOnlyDictionary<TKey, TValue>.KeyCollection(this.m_dictionary.Keys);
				}
				return this.m_keys;
			}
		}

		// Token: 0x170008BB RID: 2235
		// (get) Token: 0x060039EE RID: 14830 RVA: 0x000DD3DB File Offset: 0x000DB5DB
		[__DynamicallyInvokable]
		public ReadOnlyDictionary<TKey, TValue>.ValueCollection Values
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_values == null)
				{
					this.m_values = new ReadOnlyDictionary<TKey, TValue>.ValueCollection(this.m_dictionary.Values);
				}
				return this.m_values;
			}
		}

		// Token: 0x060039EF RID: 14831 RVA: 0x000DD401 File Offset: 0x000DB601
		[__DynamicallyInvokable]
		public bool ContainsKey(TKey key)
		{
			return this.m_dictionary.ContainsKey(key);
		}

		// Token: 0x170008BC RID: 2236
		// (get) Token: 0x060039F0 RID: 14832 RVA: 0x000DD40F File Offset: 0x000DB60F
		[__DynamicallyInvokable]
		ICollection<TKey> IDictionary<!0, !1>.Keys
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Keys;
			}
		}

		// Token: 0x060039F1 RID: 14833 RVA: 0x000DD417 File Offset: 0x000DB617
		[__DynamicallyInvokable]
		public bool TryGetValue(TKey key, out TValue value)
		{
			return this.m_dictionary.TryGetValue(key, out value);
		}

		// Token: 0x170008BD RID: 2237
		// (get) Token: 0x060039F2 RID: 14834 RVA: 0x000DD426 File Offset: 0x000DB626
		[__DynamicallyInvokable]
		ICollection<TValue> IDictionary<!0, !1>.Values
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Values;
			}
		}

		// Token: 0x170008BE RID: 2238
		[__DynamicallyInvokable]
		public TValue this[TKey key]
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_dictionary[key];
			}
		}

		// Token: 0x060039F4 RID: 14836 RVA: 0x000DD43C File Offset: 0x000DB63C
		[__DynamicallyInvokable]
		void IDictionary<!0, !1>.Add(TKey key, TValue value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039F5 RID: 14837 RVA: 0x000DD445 File Offset: 0x000DB645
		[__DynamicallyInvokable]
		bool IDictionary<!0, !1>.Remove(TKey key)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			return false;
		}

		// Token: 0x170008BF RID: 2239
		[__DynamicallyInvokable]
		TValue IDictionary<!0, !1>.this[TKey key]
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_dictionary[key];
			}
			[__DynamicallyInvokable]
			set
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}
		}

		// Token: 0x170008C0 RID: 2240
		// (get) Token: 0x060039F8 RID: 14840 RVA: 0x000DD466 File Offset: 0x000DB666
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_dictionary.Count;
			}
		}

		// Token: 0x060039F9 RID: 14841 RVA: 0x000DD473 File Offset: 0x000DB673
		[__DynamicallyInvokable]
		bool ICollection<KeyValuePair<!0, !1>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			return this.m_dictionary.Contains(item);
		}

		// Token: 0x060039FA RID: 14842 RVA: 0x000DD481 File Offset: 0x000DB681
		[__DynamicallyInvokable]
		void ICollection<KeyValuePair<!0, !1>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			this.m_dictionary.CopyTo(array, arrayIndex);
		}

		// Token: 0x170008C1 RID: 2241
		// (get) Token: 0x060039FB RID: 14843 RVA: 0x000DD490 File Offset: 0x000DB690
		[__DynamicallyInvokable]
		bool ICollection<KeyValuePair<!0, !1>>.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x060039FC RID: 14844 RVA: 0x000DD493 File Offset: 0x000DB693
		[__DynamicallyInvokable]
		void ICollection<KeyValuePair<!0, !1>>.Add(KeyValuePair<TKey, TValue> item)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039FD RID: 14845 RVA: 0x000DD49C File Offset: 0x000DB69C
		[__DynamicallyInvokable]
		void ICollection<KeyValuePair<!0, !1>>.Clear()
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x060039FE RID: 14846 RVA: 0x000DD4A5 File Offset: 0x000DB6A5
		[__DynamicallyInvokable]
		bool ICollection<KeyValuePair<!0, !1>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			return false;
		}

		// Token: 0x060039FF RID: 14847 RVA: 0x000DD4AF File Offset: 0x000DB6AF
		[__DynamicallyInvokable]
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return this.m_dictionary.GetEnumerator();
		}

		// Token: 0x06003A00 RID: 14848 RVA: 0x000DD4BC File Offset: 0x000DB6BC
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.m_dictionary.GetEnumerator();
		}

		// Token: 0x06003A01 RID: 14849 RVA: 0x000DD4C9 File Offset: 0x000DB6C9
		private static bool IsCompatibleKey(object key)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			return key is TKey;
		}

		// Token: 0x06003A02 RID: 14850 RVA: 0x000DD4DD File Offset: 0x000DB6DD
		[__DynamicallyInvokable]
		void IDictionary.Add(object key, object value)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x06003A03 RID: 14851 RVA: 0x000DD4E6 File Offset: 0x000DB6E6
		[__DynamicallyInvokable]
		void IDictionary.Clear()
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x06003A04 RID: 14852 RVA: 0x000DD4EF File Offset: 0x000DB6EF
		[__DynamicallyInvokable]
		bool IDictionary.Contains(object key)
		{
			return ReadOnlyDictionary<TKey, TValue>.IsCompatibleKey(key) && this.ContainsKey((TKey)((object)key));
		}

		// Token: 0x06003A05 RID: 14853 RVA: 0x000DD508 File Offset: 0x000DB708
		[__DynamicallyInvokable]
		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			IDictionary dictionary = this.m_dictionary as IDictionary;
			if (dictionary != null)
			{
				return dictionary.GetEnumerator();
			}
			return new ReadOnlyDictionary<TKey, TValue>.DictionaryEnumerator(this.m_dictionary);
		}

		// Token: 0x170008C2 RID: 2242
		// (get) Token: 0x06003A06 RID: 14854 RVA: 0x000DD53B File Offset: 0x000DB73B
		[__DynamicallyInvokable]
		bool IDictionary.IsFixedSize
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x170008C3 RID: 2243
		// (get) Token: 0x06003A07 RID: 14855 RVA: 0x000DD53E File Offset: 0x000DB73E
		[__DynamicallyInvokable]
		bool IDictionary.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x170008C4 RID: 2244
		// (get) Token: 0x06003A08 RID: 14856 RVA: 0x000DD541 File Offset: 0x000DB741
		[__DynamicallyInvokable]
		ICollection IDictionary.Keys
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Keys;
			}
		}

		// Token: 0x06003A09 RID: 14857 RVA: 0x000DD549 File Offset: 0x000DB749
		[__DynamicallyInvokable]
		void IDictionary.Remove(object key)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		// Token: 0x170008C5 RID: 2245
		// (get) Token: 0x06003A0A RID: 14858 RVA: 0x000DD552 File Offset: 0x000DB752
		[__DynamicallyInvokable]
		ICollection IDictionary.Values
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Values;
			}
		}

		// Token: 0x170008C6 RID: 2246
		[__DynamicallyInvokable]
		object IDictionary.this[object key]
		{
			[__DynamicallyInvokable]
			get
			{
				if (ReadOnlyDictionary<TKey, TValue>.IsCompatibleKey(key))
				{
					return this[(TKey)((object)key)];
				}
				return null;
			}
			[__DynamicallyInvokable]
			set
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}
		}

		// Token: 0x06003A0D RID: 14861 RVA: 0x000DD580 File Offset: 0x000DB780
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
				this.m_dictionary.CopyTo(array2, index);
				return;
			}
			DictionaryEntry[] array3 = array as DictionaryEntry[];
			if (array3 != null)
			{
				using (IEnumerator<KeyValuePair<TKey, TValue>> enumerator = this.m_dictionary.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<TKey, TValue> keyValuePair = enumerator.Current;
						array3[index++] = new DictionaryEntry(keyValuePair.Key, keyValuePair.Value);
					}
					return;
				}
			}
			object[] array4 = array as object[];
			if (array4 == null)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
			try
			{
				foreach (KeyValuePair<TKey, TValue> keyValuePair2 in this.m_dictionary)
				{
					array4[index++] = new KeyValuePair<TKey, TValue>(keyValuePair2.Key, keyValuePair2.Value);
				}
			}
			catch (ArrayTypeMismatchException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
			}
		}

		// Token: 0x170008C7 RID: 2247
		// (get) Token: 0x06003A0E RID: 14862 RVA: 0x000DD6EC File Offset: 0x000DB8EC
		[__DynamicallyInvokable]
		bool ICollection.IsSynchronized
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170008C8 RID: 2248
		// (get) Token: 0x06003A0F RID: 14863 RVA: 0x000DD6F0 File Offset: 0x000DB8F0
		[__DynamicallyInvokable]
		object ICollection.SyncRoot
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_syncRoot == null)
				{
					ICollection collection = this.m_dictionary as ICollection;
					if (collection != null)
					{
						this.m_syncRoot = collection.SyncRoot;
					}
					else
					{
						Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), null);
					}
				}
				return this.m_syncRoot;
			}
		}

		// Token: 0x170008C9 RID: 2249
		// (get) Token: 0x06003A10 RID: 14864 RVA: 0x000DD73A File Offset: 0x000DB93A
		[__DynamicallyInvokable]
		IEnumerable<TKey> IReadOnlyDictionary<!0, !1>.Keys
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Keys;
			}
		}

		// Token: 0x170008CA RID: 2250
		// (get) Token: 0x06003A11 RID: 14865 RVA: 0x000DD742 File Offset: 0x000DB942
		[__DynamicallyInvokable]
		IEnumerable<TValue> IReadOnlyDictionary<!0, !1>.Values
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Values;
			}
		}

		// Token: 0x04001938 RID: 6456
		private readonly IDictionary<TKey, TValue> m_dictionary;

		// Token: 0x04001939 RID: 6457
		[NonSerialized]
		private object m_syncRoot;

		// Token: 0x0400193A RID: 6458
		[NonSerialized]
		private ReadOnlyDictionary<TKey, TValue>.KeyCollection m_keys;

		// Token: 0x0400193B RID: 6459
		[NonSerialized]
		private ReadOnlyDictionary<TKey, TValue>.ValueCollection m_values;

		// Token: 0x02000BDD RID: 3037
		[Serializable]
		private struct DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
		{
			// Token: 0x06006EE3 RID: 28387 RVA: 0x0017E3BB File Offset: 0x0017C5BB
			public DictionaryEnumerator(IDictionary<TKey, TValue> dictionary)
			{
				this.m_dictionary = dictionary;
				this.m_enumerator = this.m_dictionary.GetEnumerator();
			}

			// Token: 0x170012FD RID: 4861
			// (get) Token: 0x06006EE4 RID: 28388 RVA: 0x0017E3D8 File Offset: 0x0017C5D8
			public DictionaryEntry Entry
			{
				get
				{
					KeyValuePair<TKey, TValue> keyValuePair = this.m_enumerator.Current;
					object key = keyValuePair.Key;
					keyValuePair = this.m_enumerator.Current;
					return new DictionaryEntry(key, keyValuePair.Value);
				}
			}

			// Token: 0x170012FE RID: 4862
			// (get) Token: 0x06006EE5 RID: 28389 RVA: 0x0017E41C File Offset: 0x0017C61C
			public object Key
			{
				get
				{
					KeyValuePair<TKey, TValue> keyValuePair = this.m_enumerator.Current;
					return keyValuePair.Key;
				}
			}

			// Token: 0x170012FF RID: 4863
			// (get) Token: 0x06006EE6 RID: 28390 RVA: 0x0017E444 File Offset: 0x0017C644
			public object Value
			{
				get
				{
					KeyValuePair<TKey, TValue> keyValuePair = this.m_enumerator.Current;
					return keyValuePair.Value;
				}
			}

			// Token: 0x17001300 RID: 4864
			// (get) Token: 0x06006EE7 RID: 28391 RVA: 0x0017E469 File Offset: 0x0017C669
			public object Current
			{
				get
				{
					return this.Entry;
				}
			}

			// Token: 0x06006EE8 RID: 28392 RVA: 0x0017E476 File Offset: 0x0017C676
			public bool MoveNext()
			{
				return this.m_enumerator.MoveNext();
			}

			// Token: 0x06006EE9 RID: 28393 RVA: 0x0017E483 File Offset: 0x0017C683
			public void Reset()
			{
				this.m_enumerator.Reset();
			}

			// Token: 0x040035E7 RID: 13799
			private readonly IDictionary<TKey, TValue> m_dictionary;

			// Token: 0x040035E8 RID: 13800
			private IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator;
		}

		// Token: 0x02000BDE RID: 3038
		[DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
		[DebuggerDisplay("Count = {Count}")]
		[__DynamicallyInvokable]
		[Serializable]
		public sealed class KeyCollection : ICollection<!0>, IEnumerable<!0>, IEnumerable, ICollection, IReadOnlyCollection<TKey>
		{
			// Token: 0x06006EEA RID: 28394 RVA: 0x0017E490 File Offset: 0x0017C690
			internal KeyCollection(ICollection<TKey> collection)
			{
				if (collection == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
				}
				this.m_collection = collection;
			}

			// Token: 0x06006EEB RID: 28395 RVA: 0x0017E4A8 File Offset: 0x0017C6A8
			[__DynamicallyInvokable]
			void ICollection<!0>.Add(TKey item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}

			// Token: 0x06006EEC RID: 28396 RVA: 0x0017E4B1 File Offset: 0x0017C6B1
			[__DynamicallyInvokable]
			void ICollection<!0>.Clear()
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}

			// Token: 0x06006EED RID: 28397 RVA: 0x0017E4BA File Offset: 0x0017C6BA
			[__DynamicallyInvokable]
			bool ICollection<!0>.Contains(TKey item)
			{
				return this.m_collection.Contains(item);
			}

			// Token: 0x06006EEE RID: 28398 RVA: 0x0017E4C8 File Offset: 0x0017C6C8
			[__DynamicallyInvokable]
			public void CopyTo(TKey[] array, int arrayIndex)
			{
				this.m_collection.CopyTo(array, arrayIndex);
			}

			// Token: 0x17001301 RID: 4865
			// (get) Token: 0x06006EEF RID: 28399 RVA: 0x0017E4D7 File Offset: 0x0017C6D7
			[__DynamicallyInvokable]
			public int Count
			{
				[__DynamicallyInvokable]
				get
				{
					return this.m_collection.Count;
				}
			}

			// Token: 0x17001302 RID: 4866
			// (get) Token: 0x06006EF0 RID: 28400 RVA: 0x0017E4E4 File Offset: 0x0017C6E4
			[__DynamicallyInvokable]
			bool ICollection<!0>.IsReadOnly
			{
				[__DynamicallyInvokable]
				get
				{
					return true;
				}
			}

			// Token: 0x06006EF1 RID: 28401 RVA: 0x0017E4E7 File Offset: 0x0017C6E7
			[__DynamicallyInvokable]
			bool ICollection<!0>.Remove(TKey item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
				return false;
			}

			// Token: 0x06006EF2 RID: 28402 RVA: 0x0017E4F1 File Offset: 0x0017C6F1
			[__DynamicallyInvokable]
			public IEnumerator<TKey> GetEnumerator()
			{
				return this.m_collection.GetEnumerator();
			}

			// Token: 0x06006EF3 RID: 28403 RVA: 0x0017E4FE File Offset: 0x0017C6FE
			[__DynamicallyInvokable]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.m_collection.GetEnumerator();
			}

			// Token: 0x06006EF4 RID: 28404 RVA: 0x0017E50B File Offset: 0x0017C70B
			[__DynamicallyInvokable]
			void ICollection.CopyTo(Array array, int index)
			{
				ReadOnlyDictionaryHelpers.CopyToNonGenericICollectionHelper<TKey>(this.m_collection, array, index);
			}

			// Token: 0x17001303 RID: 4867
			// (get) Token: 0x06006EF5 RID: 28405 RVA: 0x0017E51A File Offset: 0x0017C71A
			[__DynamicallyInvokable]
			bool ICollection.IsSynchronized
			{
				[__DynamicallyInvokable]
				get
				{
					return false;
				}
			}

			// Token: 0x17001304 RID: 4868
			// (get) Token: 0x06006EF6 RID: 28406 RVA: 0x0017E520 File Offset: 0x0017C720
			[__DynamicallyInvokable]
			object ICollection.SyncRoot
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.m_syncRoot == null)
					{
						ICollection collection = this.m_collection as ICollection;
						if (collection != null)
						{
							this.m_syncRoot = collection.SyncRoot;
						}
						else
						{
							Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), null);
						}
					}
					return this.m_syncRoot;
				}
			}

			// Token: 0x040035E9 RID: 13801
			private readonly ICollection<TKey> m_collection;

			// Token: 0x040035EA RID: 13802
			[NonSerialized]
			private object m_syncRoot;
		}

		// Token: 0x02000BDF RID: 3039
		[DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
		[DebuggerDisplay("Count = {Count}")]
		[__DynamicallyInvokable]
		[Serializable]
		public sealed class ValueCollection : ICollection<!1>, IEnumerable<!1>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
		{
			// Token: 0x06006EF7 RID: 28407 RVA: 0x0017E56A File Offset: 0x0017C76A
			internal ValueCollection(ICollection<TValue> collection)
			{
				if (collection == null)
				{
					ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
				}
				this.m_collection = collection;
			}

			// Token: 0x06006EF8 RID: 28408 RVA: 0x0017E582 File Offset: 0x0017C782
			[__DynamicallyInvokable]
			void ICollection<!1>.Add(TValue item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}

			// Token: 0x06006EF9 RID: 28409 RVA: 0x0017E58B File Offset: 0x0017C78B
			[__DynamicallyInvokable]
			void ICollection<!1>.Clear()
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
			}

			// Token: 0x06006EFA RID: 28410 RVA: 0x0017E594 File Offset: 0x0017C794
			[__DynamicallyInvokable]
			bool ICollection<!1>.Contains(TValue item)
			{
				return this.m_collection.Contains(item);
			}

			// Token: 0x06006EFB RID: 28411 RVA: 0x0017E5A2 File Offset: 0x0017C7A2
			[__DynamicallyInvokable]
			public void CopyTo(TValue[] array, int arrayIndex)
			{
				this.m_collection.CopyTo(array, arrayIndex);
			}

			// Token: 0x17001305 RID: 4869
			// (get) Token: 0x06006EFC RID: 28412 RVA: 0x0017E5B1 File Offset: 0x0017C7B1
			[__DynamicallyInvokable]
			public int Count
			{
				[__DynamicallyInvokable]
				get
				{
					return this.m_collection.Count;
				}
			}

			// Token: 0x17001306 RID: 4870
			// (get) Token: 0x06006EFD RID: 28413 RVA: 0x0017E5BE File Offset: 0x0017C7BE
			[__DynamicallyInvokable]
			bool ICollection<!1>.IsReadOnly
			{
				[__DynamicallyInvokable]
				get
				{
					return true;
				}
			}

			// Token: 0x06006EFE RID: 28414 RVA: 0x0017E5C1 File Offset: 0x0017C7C1
			[__DynamicallyInvokable]
			bool ICollection<!1>.Remove(TValue item)
			{
				ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
				return false;
			}

			// Token: 0x06006EFF RID: 28415 RVA: 0x0017E5CB File Offset: 0x0017C7CB
			[__DynamicallyInvokable]
			public IEnumerator<TValue> GetEnumerator()
			{
				return this.m_collection.GetEnumerator();
			}

			// Token: 0x06006F00 RID: 28416 RVA: 0x0017E5D8 File Offset: 0x0017C7D8
			[__DynamicallyInvokable]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.m_collection.GetEnumerator();
			}

			// Token: 0x06006F01 RID: 28417 RVA: 0x0017E5E5 File Offset: 0x0017C7E5
			[__DynamicallyInvokable]
			void ICollection.CopyTo(Array array, int index)
			{
				ReadOnlyDictionaryHelpers.CopyToNonGenericICollectionHelper<TValue>(this.m_collection, array, index);
			}

			// Token: 0x17001307 RID: 4871
			// (get) Token: 0x06006F02 RID: 28418 RVA: 0x0017E5F4 File Offset: 0x0017C7F4
			[__DynamicallyInvokable]
			bool ICollection.IsSynchronized
			{
				[__DynamicallyInvokable]
				get
				{
					return false;
				}
			}

			// Token: 0x17001308 RID: 4872
			// (get) Token: 0x06006F03 RID: 28419 RVA: 0x0017E5F8 File Offset: 0x0017C7F8
			[__DynamicallyInvokable]
			object ICollection.SyncRoot
			{
				[__DynamicallyInvokable]
				get
				{
					if (this.m_syncRoot == null)
					{
						ICollection collection = this.m_collection as ICollection;
						if (collection != null)
						{
							this.m_syncRoot = collection.SyncRoot;
						}
						else
						{
							Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), null);
						}
					}
					return this.m_syncRoot;
				}
			}

			// Token: 0x040035EB RID: 13803
			private readonly ICollection<TValue> m_collection;

			// Token: 0x040035EC RID: 13804
			[NonSerialized]
			private object m_syncRoot;
		}
	}
}
