using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200006D RID: 109
	[Serializable]
	public class MBReadOnlyDictionary<TKey, TValue> : ICollection, IEnumerable, IReadOnlyDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		// Token: 0x060003D2 RID: 978 RVA: 0x0000DB47 File Offset: 0x0000BD47
		public MBReadOnlyDictionary(Dictionary<TKey, TValue> dictionary)
		{
			this._dictionary = dictionary;
		}

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060003D3 RID: 979 RVA: 0x0000DB56 File Offset: 0x0000BD56
		public int Count
		{
			get
			{
				return this._dictionary.Count;
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060003D4 RID: 980 RVA: 0x0000DB63 File Offset: 0x0000BD63
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060003D5 RID: 981 RVA: 0x0000DB66 File Offset: 0x0000BD66
		public object SyncRoot
		{
			get
			{
				return null;
			}
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0000DB69 File Offset: 0x0000BD69
		public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
		{
			return this._dictionary.GetEnumerator();
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x0000DB76 File Offset: 0x0000BD76
		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<!0, !1>>.GetEnumerator()
		{
			return this._dictionary.GetEnumerator();
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x0000DB88 File Offset: 0x0000BD88
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this._dictionary.GetEnumerator();
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x0000DB9A File Offset: 0x0000BD9A
		public bool ContainsKey(TKey key)
		{
			return this._dictionary.ContainsKey(key);
		}

		// Token: 0x060003DA RID: 986 RVA: 0x0000DBA8 File Offset: 0x0000BDA8
		public bool TryGetValue(TKey key, out TValue value)
		{
			return this._dictionary.TryGetValue(key, out value);
		}

		// Token: 0x17000059 RID: 89
		public TValue this[TKey key]
		{
			get
			{
				return this._dictionary[key];
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060003DC RID: 988 RVA: 0x0000DBC5 File Offset: 0x0000BDC5
		public IEnumerable<TKey> Keys
		{
			get
			{
				return this._dictionary.Keys;
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060003DD RID: 989 RVA: 0x0000DBD2 File Offset: 0x0000BDD2
		public IEnumerable<TValue> Values
		{
			get
			{
				return this._dictionary.Values;
			}
		}

		// Token: 0x060003DE RID: 990 RVA: 0x0000DBE0 File Offset: 0x0000BDE0
		public void CopyTo(Array array, int index)
		{
			KeyValuePair<TKey, TValue>[] array2 = array as KeyValuePair<TKey, TValue>[];
			if (array2 != null)
			{
				((ICollection)this._dictionary).CopyTo(array2, index);
				return;
			}
			DictionaryEntry[] array3 = array as DictionaryEntry[];
			if (array3 != null)
			{
				using (Dictionary<TKey, TValue>.Enumerator enumerator = this._dictionary.GetEnumerator())
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
			try
			{
				foreach (KeyValuePair<TKey, TValue> keyValuePair2 in this._dictionary)
				{
					array4[index++] = new KeyValuePair<TKey, TValue>(keyValuePair2.Key, keyValuePair2.Value);
				}
			}
			catch (ArrayTypeMismatchException)
			{
				Debug.FailedAssert("Invalid array type", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\MBReadOnlyDictionary.cs", "CopyTo", 95);
			}
		}

		// Token: 0x0400013F RID: 319
		private Dictionary<TKey, TValue> _dictionary;
	}
}
