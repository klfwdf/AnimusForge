using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x02000070 RID: 112
	public class MBSortedMultiList<TKey, TValue> : IReadOnlyList<TValue>, IEnumerable<TValue>, IEnumerable, IReadOnlyCollection<TValue>, IMBCollection where TKey : IComparable<TKey>
	{
		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060003E6 RID: 998 RVA: 0x0000DD45 File Offset: 0x0000BF45
		public MBSortedMultiList<TKey, TValue>.ComparerType Comparer
		{
			get
			{
				return this._comparerType;
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060003E7 RID: 999 RVA: 0x0000DD4D File Offset: 0x0000BF4D
		private bool IsAscending
		{
			get
			{
				return this._comparerType == MBSortedMultiList<TKey, TValue>.ComparerType.Ascending;
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060003E8 RID: 1000 RVA: 0x0000DD58 File Offset: 0x0000BF58
		private bool IsDescending
		{
			get
			{
				return this._comparerType == MBSortedMultiList<TKey, TValue>.ComparerType.Descending;
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060003E9 RID: 1001 RVA: 0x0000DD63 File Offset: 0x0000BF63
		public int Count
		{
			get
			{
				return this._items.Count;
			}
		}

		// Token: 0x17000060 RID: 96
		public TValue this[int index]
		{
			get
			{
				return this._items[index].Value;
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060003EB RID: 1003 RVA: 0x0000DD94 File Offset: 0x0000BF94
		public TValue FirstValue
		{
			get
			{
				return this._items[0].Value;
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060003EC RID: 1004 RVA: 0x0000DDB8 File Offset: 0x0000BFB8
		public TValue LastValue
		{
			get
			{
				return this._items[this._items.Count - 1].Value;
			}
		}

		// Token: 0x060003ED RID: 1005 RVA: 0x0000DDE5 File Offset: 0x0000BFE5
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x0000DDED File Offset: 0x0000BFED
		public MBSortedMultiList(IComparer<TKey> customComparer)
		{
			this._items = new List<KeyValuePair<TKey, TValue>>();
			this.SetCustomComparer(customComparer);
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x0000DE07 File Offset: 0x0000C007
		public MBSortedMultiList(bool isAscending = true)
		{
			this._items = new List<KeyValuePair<TKey, TValue>>();
			this.SetDefaultComparer(isAscending);
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x0000DE21 File Offset: 0x0000C021
		public bool Contains(TKey key)
		{
			return this.FirstIndexOf(key) >= 0;
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x0000DE30 File Offset: 0x0000C030
		public bool Contains(TKey key, TValue value)
		{
			return this.FirstIndexOf(key, value) >= 0;
		}

		// Token: 0x060003F2 RID: 1010 RVA: 0x0000DE40 File Offset: 0x0000C040
		public KeyValuePair<TKey, TValue> Get(int index)
		{
			return this._items[index];
		}

		// Token: 0x060003F3 RID: 1011 RVA: 0x0000DE50 File Offset: 0x0000C050
		public int FirstIndexOf(TKey key)
		{
			if (this._items.Count > 0)
			{
				int num = this.LowerBound(key);
				if (num < this._items.Count)
				{
					TKey key2 = this._items[num].Key;
					if (key2.CompareTo(key) == 0)
					{
						return num;
					}
				}
			}
			return -1;
		}

		// Token: 0x060003F4 RID: 1012 RVA: 0x0000DEAC File Offset: 0x0000C0AC
		public int FirstIndexOf(TKey key, TValue value)
		{
			if (this._items.Count > 0)
			{
				int i = this.LowerBound(key);
				EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
				while (i < this._items.Count)
				{
					TKey key2 = this._items[i].Key;
					if (key2.CompareTo(key) != 0)
					{
						break;
					}
					if (@default.Equals(this._items[i].Value, value))
					{
						return i;
					}
					i++;
				}
			}
			return -1;
		}

		// Token: 0x060003F5 RID: 1013 RVA: 0x0000DF30 File Offset: 0x0000C130
		public int LastIndexOf(TKey key)
		{
			if (this._items.Count > 0)
			{
				int num = this.UpperBound(key) - 1;
				if (num >= 0 && num < this._items.Count)
				{
					TKey key2 = this._items[num].Key;
					if (key2.CompareTo(key) == 0)
					{
						return num;
					}
				}
			}
			return -1;
		}

		// Token: 0x060003F6 RID: 1014 RVA: 0x0000DF90 File Offset: 0x0000C190
		public int LastIndexOf(TKey key, TValue value)
		{
			if (this._items.Count > 0)
			{
				int i = this.UpperBound(key) - 1;
				EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
				while (i >= 0)
				{
					TKey key2 = this._items[i].Key;
					if (key2.CompareTo(key) != 0)
					{
						break;
					}
					if (@default.Equals(this._items[i].Value, value))
					{
						return i;
					}
					i--;
				}
			}
			return -1;
		}

		// Token: 0x060003F7 RID: 1015 RVA: 0x0000E00C File Offset: 0x0000C20C
		public bool All(Predicate<KeyValuePair<TKey, TValue>> predicate)
		{
			foreach (KeyValuePair<TKey, TValue> obj in this._items)
			{
				if (!predicate(obj))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060003F8 RID: 1016 RVA: 0x0000E068 File Offset: 0x0000C268
		public bool Any(Predicate<KeyValuePair<TKey, TValue>> predicate)
		{
			foreach (KeyValuePair<TKey, TValue> obj in this._items)
			{
				if (predicate(obj))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060003F9 RID: 1017 RVA: 0x0000E0C4 File Offset: 0x0000C2C4
		public IEnumerator<TValue> GetValues(TKey key)
		{
			int num = this.LowerBound(key);
			List<KeyValuePair<TKey, TValue>> items = this._items;
			int startIndex;
			if (num < this._items.Count)
			{
				TKey key2 = this._items[num].Key;
				if (key2.CompareTo(key) == 0)
				{
					startIndex = num;
					goto IL_50;
				}
			}
			startIndex = this._items.Count;
			IL_50:
			return new MBSortedMultiList<TKey, TValue>.SMLKeyValueEnumerator(items, key, startIndex);
		}

		// Token: 0x060003FA RID: 1018 RVA: 0x0000E12C File Offset: 0x0000C32C
		public bool Find(Predicate<KeyValuePair<TKey, TValue>> predicate, out KeyValuePair<TKey, TValue> found, bool searchForward = true)
		{
			if (searchForward)
			{
				for (int i = 0; i < this._items.Count; i++)
				{
					KeyValuePair<TKey, TValue> keyValuePair = this._items[i];
					if (predicate(keyValuePair))
					{
						found = keyValuePair;
						return true;
					}
				}
			}
			else
			{
				for (int j = this._items.Count - 1; j >= 0; j--)
				{
					KeyValuePair<TKey, TValue> keyValuePair2 = this._items[j];
					if (predicate(keyValuePair2))
					{
						found = keyValuePair2;
						return true;
					}
				}
			}
			found = default(KeyValuePair<TKey, TValue>);
			return false;
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x0000E1B4 File Offset: 0x0000C3B4
		public int FindIndex(Predicate<KeyValuePair<TKey, TValue>> predicate, bool searchForward = true)
		{
			if (searchForward)
			{
				for (int i = 0; i < this._items.Count; i++)
				{
					KeyValuePair<TKey, TValue> obj = this._items[i];
					if (predicate(obj))
					{
						return i;
					}
				}
			}
			else
			{
				for (int j = this._items.Count - 1; j >= 0; j--)
				{
					KeyValuePair<TKey, TValue> obj2 = this._items[j];
					if (predicate(obj2))
					{
						return j;
					}
				}
			}
			return -1;
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x0000E228 File Offset: 0x0000C428
		public MBList<KeyValuePair<TKey, TValue>> FindAll(Predicate<KeyValuePair<TKey, TValue>> predicate)
		{
			MBList<KeyValuePair<TKey, TValue>> mblist = new MBList<KeyValuePair<TKey, TValue>>();
			foreach (KeyValuePair<TKey, TValue> keyValuePair in this._items)
			{
				if (predicate(keyValuePair))
				{
					mblist.Add(keyValuePair);
				}
			}
			return mblist;
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x0000E28C File Offset: 0x0000C48C
		public void Add(TKey key, TValue value)
		{
			KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, value);
			int index = this.UpperBound(key);
			this._items.Insert(index, item);
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x0000E2B7 File Offset: 0x0000C4B7
		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			this._items.AddRange(items);
			this._items.Sort(this._pairComparer);
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x0000E2D8 File Offset: 0x0000C4D8
		public bool Remove(TKey key, TValue value)
		{
			int num = this.LastIndexOf(key, value);
			if (num >= 0)
			{
				this._items.RemoveAt(num);
				return true;
			}
			return false;
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x0000E304 File Offset: 0x0000C504
		public bool Remove(TKey key)
		{
			int num = this.LastIndexOf(key);
			if (num >= 0)
			{
				this._items.RemoveAt(num);
				return true;
			}
			return false;
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x0000E32C File Offset: 0x0000C52C
		public int RemoveAll(Predicate<KeyValuePair<TKey, TValue>> predicate)
		{
			int num = 0;
			for (int i = this._items.Count - 1; i >= 0; i--)
			{
				if (predicate(this._items[i]))
				{
					this._items.RemoveAt(i);
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x0000E378 File Offset: 0x0000C578
		public void RemoveAt(int index)
		{
			this._items.RemoveAt(index);
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0000E386 File Offset: 0x0000C586
		public void RemoveLast()
		{
			this._items.RemoveAt(this._items.Count - 1);
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0000E3A0 File Offset: 0x0000C5A0
		public void Clear()
		{
			this._items.Clear();
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x0000E3AD File Offset: 0x0000C5AD
		public void SetCustomComparer(IComparer<TKey> customComparer)
		{
			this._keyComparer = customComparer;
			this._pairComparer = this.GetPairComparerFromKeyComparer();
			this._comparerType = MBSortedMultiList<TKey, TValue>.ComparerType.Custom;
			if (this._items.Count > 0)
			{
				this._items.Sort(this._pairComparer);
			}
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0000E3E8 File Offset: 0x0000C5E8
		public void SetDefaultComparer(bool isAscending = true)
		{
			bool flag = false;
			if (isAscending && this._comparerType != MBSortedMultiList<TKey, TValue>.ComparerType.Ascending)
			{
				this._keyComparer = MBSortedMultiList<TKey, TValue>.DefaultAscendingKeyComparer;
				this._pairComparer = this.GetPairComparerFromKeyComparer();
				this._comparerType = MBSortedMultiList<TKey, TValue>.ComparerType.Ascending;
				flag = true;
			}
			else if (!isAscending && this._comparerType != MBSortedMultiList<TKey, TValue>.ComparerType.Descending)
			{
				this._keyComparer = MBSortedMultiList<TKey, TValue>.DefaultDescendingKeyComparer;
				this._pairComparer = this.GetPairComparerFromKeyComparer();
				this._comparerType = MBSortedMultiList<TKey, TValue>.ComparerType.Descending;
				flag = true;
			}
			if (flag && this._items.Count > 0)
			{
				this._items.Sort(this._pairComparer);
			}
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0000E473 File Offset: 0x0000C673
		public void Reverse()
		{
			if (this._comparerType == MBSortedMultiList<TKey, TValue>.ComparerType.Ascending)
			{
				this.SetDefaultComparer(false);
				return;
			}
			if (this._comparerType == MBSortedMultiList<TKey, TValue>.ComparerType.Descending)
			{
				this.SetDefaultComparer(true);
				return;
			}
			Debug.FailedAssert("Comparer type must not be custom", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\MBSortedMultiList.cs", "Reverse", 562);
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x0000E4B0 File Offset: 0x0000C6B0
		public override string ToString()
		{
			return string.Format("MBSortedMultiList[{0}, {1}], Count = {2}, Comparer Type = {3}", new object[]
			{
				typeof(TKey).Name,
				typeof(TValue).Name,
				this.Count,
				this._comparerType.ToString()
			});
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x0000E513 File Offset: 0x0000C713
		public IEnumerator<TValue> GetEnumerator()
		{
			return new MBSortedMultiList<TKey, TValue>.SMLValueEnumerator(this._items);
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0000E528 File Offset: 0x0000C728
		private int LowerBound(TKey key)
		{
			int i = 0;
			int num = this._items.Count;
			while (i < num)
			{
				int num2 = (i + num) / 2;
				if (this._keyComparer.Compare(this._items[num2].Key, key) < 0)
				{
					i = num2 + 1;
				}
				else
				{
					num = num2;
				}
			}
			return i;
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0000E57C File Offset: 0x0000C77C
		private int UpperBound(TKey key)
		{
			int i = 0;
			int num = this._items.Count;
			while (i < num)
			{
				int num2 = (i + num) / 2;
				if (this._keyComparer.Compare(this._items[num2].Key, key) <= 0)
				{
					i = num2 + 1;
				}
				else
				{
					num = num2;
				}
			}
			return i;
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x0000E5CF File Offset: 0x0000C7CF
		private IComparer<KeyValuePair<TKey, TValue>> GetPairComparerFromKeyComparer()
		{
			return Comparer<KeyValuePair<TKey, TValue>>.Create((KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => this._keyComparer.Compare(x.Key, y.Key));
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x0600040D RID: 1037 RVA: 0x0000E5E2 File Offset: 0x0000C7E2
		private static IComparer<TKey> DefaultAscendingKeyComparer
		{
			get
			{
				return Comparer<TKey>.Default;
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x0600040E RID: 1038 RVA: 0x0000E5E9 File Offset: 0x0000C7E9
		private static IComparer<TKey> DefaultDescendingKeyComparer
		{
			get
			{
				return Comparer<TKey>.Create((TKey x, TKey y) => y.CompareTo(x));
			}
		}

		// Token: 0x04000140 RID: 320
		private readonly List<KeyValuePair<TKey, TValue>> _items;

		// Token: 0x04000141 RID: 321
		private MBSortedMultiList<TKey, TValue>.ComparerType _comparerType;

		// Token: 0x04000142 RID: 322
		private IComparer<TKey> _keyComparer;

		// Token: 0x04000143 RID: 323
		private IComparer<KeyValuePair<TKey, TValue>> _pairComparer;

		// Token: 0x020000D9 RID: 217
		public enum ComparerType
		{
			// Token: 0x040002C5 RID: 709
			None,
			// Token: 0x040002C6 RID: 710
			Custom,
			// Token: 0x040002C7 RID: 711
			Ascending,
			// Token: 0x040002C8 RID: 712
			Descending
		}

		// Token: 0x020000DA RID: 218
		private struct SMLValueEnumerator : IEnumerator<TValue>, IEnumerator, IDisposable
		{
			// Token: 0x06000773 RID: 1907 RVA: 0x00018C83 File Offset: 0x00016E83
			public SMLValueEnumerator(List<KeyValuePair<TKey, TValue>> list)
			{
				this._list = list;
				this._index = -1;
				this._current = default(TValue);
			}

			// Token: 0x06000774 RID: 1908 RVA: 0x00018CA0 File Offset: 0x00016EA0
			public bool MoveNext()
			{
				int num = this._index + 1;
				this._index = num;
				if (num < this._list.Count)
				{
					this._current = this._list[this._index].Value;
					return true;
				}
				return false;
			}

			// Token: 0x170000FA RID: 250
			// (get) Token: 0x06000775 RID: 1909 RVA: 0x00018CED File Offset: 0x00016EED
			public TValue Current
			{
				get
				{
					return this._current;
				}
			}

			// Token: 0x170000FB RID: 251
			// (get) Token: 0x06000776 RID: 1910 RVA: 0x00018CF5 File Offset: 0x00016EF5
			object IEnumerator.Current
			{
				get
				{
					return this._current;
				}
			}

			// Token: 0x06000777 RID: 1911 RVA: 0x00018D02 File Offset: 0x00016F02
			public void Dispose()
			{
			}

			// Token: 0x06000778 RID: 1912 RVA: 0x00018D04 File Offset: 0x00016F04
			public void Reset()
			{
				throw new NotSupportedException();
			}

			// Token: 0x040002C9 RID: 713
			private readonly List<KeyValuePair<TKey, TValue>> _list;

			// Token: 0x040002CA RID: 714
			private int _index;

			// Token: 0x040002CB RID: 715
			private TValue _current;
		}

		// Token: 0x020000DB RID: 219
		private struct SMLKeyValueEnumerator : IEnumerator<TValue>, IEnumerator, IDisposable
		{
			// Token: 0x06000779 RID: 1913 RVA: 0x00018D0B File Offset: 0x00016F0B
			public SMLKeyValueEnumerator(List<KeyValuePair<TKey, TValue>> list, TKey key, int startIndex)
			{
				this._list = list;
				this._key = key;
				this._index = startIndex - 1;
				this._current = default(TValue);
			}

			// Token: 0x0600077A RID: 1914 RVA: 0x00018D30 File Offset: 0x00016F30
			public bool MoveNext()
			{
				this._index++;
				if (this._index < this._list.Count)
				{
					TKey key = this._list[this._index].Key;
					if (key.CompareTo(this._key) == 0)
					{
						this._current = this._list[this._index].Value;
						return true;
					}
				}
				return false;
			}

			// Token: 0x170000FC RID: 252
			// (get) Token: 0x0600077B RID: 1915 RVA: 0x00018DAF File Offset: 0x00016FAF
			public TValue Current
			{
				get
				{
					return this._current;
				}
			}

			// Token: 0x170000FD RID: 253
			// (get) Token: 0x0600077C RID: 1916 RVA: 0x00018DB7 File Offset: 0x00016FB7
			object IEnumerator.Current
			{
				get
				{
					return this._current;
				}
			}

			// Token: 0x0600077D RID: 1917 RVA: 0x00018DC4 File Offset: 0x00016FC4
			public void Dispose()
			{
			}

			// Token: 0x0600077E RID: 1918 RVA: 0x00018DC6 File Offset: 0x00016FC6
			public void Reset()
			{
				throw new NotSupportedException();
			}

			// Token: 0x040002CC RID: 716
			private readonly List<KeyValuePair<TKey, TValue>> _list;

			// Token: 0x040002CD RID: 717
			private readonly TKey _key;

			// Token: 0x040002CE RID: 718
			private int _index;

			// Token: 0x040002CF RID: 719
			private TValue _current;
		}
	}
}
