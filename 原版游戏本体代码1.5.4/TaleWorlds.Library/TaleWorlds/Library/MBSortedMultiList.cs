using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.Library;

public class MBSortedMultiList<TKey, TValue> : IReadOnlyList<TValue>, IEnumerable<TValue>, IEnumerable, IReadOnlyCollection<TValue>, IMBCollection where TKey : IComparable<TKey>
{
	public enum ComparerType
	{
		None,
		Custom,
		Ascending,
		Descending
	}

	private struct SMLValueEnumerator : IEnumerator<TValue>, IEnumerator, IDisposable
	{
		private readonly List<KeyValuePair<TKey, TValue>> _list;

		private int _index;

		private TValue _current;

		public TValue Current => _current;

		object IEnumerator.Current => _current;

		public SMLValueEnumerator(List<KeyValuePair<TKey, TValue>> list)
		{
			_list = list;
			_index = -1;
			_current = default(TValue);
		}

		public bool MoveNext()
		{
			if (++_index < _list.Count)
			{
				_current = _list[_index].Value;
				return true;
			}
			return false;
		}

		public void Dispose()
		{
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}
	}

	private struct SMLKeyValueEnumerator : IEnumerator<TValue>, IEnumerator, IDisposable
	{
		private readonly List<KeyValuePair<TKey, TValue>> _list;

		private readonly TKey _key;

		private int _index;

		private TValue _current;

		public TValue Current => _current;

		object IEnumerator.Current => _current;

		public SMLKeyValueEnumerator(List<KeyValuePair<TKey, TValue>> list, TKey key, int startIndex)
		{
			_list = list;
			_key = key;
			_index = startIndex - 1;
			_current = default(TValue);
		}

		public bool MoveNext()
		{
			_index++;
			if (_index < _list.Count && _list[_index].Key.CompareTo(_key) == 0)
			{
				_current = _list[_index].Value;
				return true;
			}
			return false;
		}

		public void Dispose()
		{
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}
	}

	private readonly List<KeyValuePair<TKey, TValue>> _items;

	private ComparerType _comparerType;

	private IComparer<TKey> _keyComparer;

	private IComparer<KeyValuePair<TKey, TValue>> _pairComparer;

	public ComparerType Comparer => _comparerType;

	private bool IsAscending => _comparerType == ComparerType.Ascending;

	private bool IsDescending => _comparerType == ComparerType.Descending;

	public int Count => _items.Count;

	public TValue this[int index] => _items[index].Value;

	public TValue FirstValue => _items[0].Value;

	public TValue LastValue => _items[_items.Count - 1].Value;

	private static IComparer<TKey> DefaultAscendingKeyComparer => Comparer<TKey>.Default;

	private static IComparer<TKey> DefaultDescendingKeyComparer => Comparer<TKey>.Create((TKey x, TKey y) => y.CompareTo(x));

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public MBSortedMultiList(IComparer<TKey> customComparer)
	{
		_items = new List<KeyValuePair<TKey, TValue>>();
		SetCustomComparer(customComparer);
	}

	public MBSortedMultiList(bool isAscending = true)
	{
		_items = new List<KeyValuePair<TKey, TValue>>();
		SetDefaultComparer(isAscending);
	}

	public bool Contains(TKey key)
	{
		return FirstIndexOf(key) >= 0;
	}

	public bool Contains(TKey key, TValue value)
	{
		return FirstIndexOf(key, value) >= 0;
	}

	public KeyValuePair<TKey, TValue> Get(int index)
	{
		return _items[index];
	}

	public int FirstIndexOf(TKey key)
	{
		if (_items.Count > 0)
		{
			int num = LowerBound(key);
			if (num < _items.Count && _items[num].Key.CompareTo(key) == 0)
			{
				return num;
			}
		}
		return -1;
	}

	public int FirstIndexOf(TKey key, TValue value)
	{
		if (_items.Count > 0)
		{
			int i = LowerBound(key);
			EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
			for (; i < _items.Count && _items[i].Key.CompareTo(key) == 0; i++)
			{
				if (equalityComparer.Equals(_items[i].Value, value))
				{
					return i;
				}
			}
		}
		return -1;
	}

	public int LastIndexOf(TKey key)
	{
		if (_items.Count > 0)
		{
			int num = UpperBound(key) - 1;
			if (num >= 0 && num < _items.Count && _items[num].Key.CompareTo(key) == 0)
			{
				return num;
			}
		}
		return -1;
	}

	public int LastIndexOf(TKey key, TValue value)
	{
		if (_items.Count > 0)
		{
			int num = UpperBound(key) - 1;
			EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
			while (num >= 0 && _items[num].Key.CompareTo(key) == 0)
			{
				if (equalityComparer.Equals(_items[num].Value, value))
				{
					return num;
				}
				num--;
			}
		}
		return -1;
	}

	public bool All(Predicate<KeyValuePair<TKey, TValue>> predicate)
	{
		foreach (KeyValuePair<TKey, TValue> item in _items)
		{
			if (!predicate(item))
			{
				return false;
			}
		}
		return true;
	}

	public bool Any(Predicate<KeyValuePair<TKey, TValue>> predicate)
	{
		foreach (KeyValuePair<TKey, TValue> item in _items)
		{
			if (predicate(item))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerator<TValue> GetValues(TKey key)
	{
		int num = LowerBound(key);
		return new SMLKeyValueEnumerator(_items, key, (num >= _items.Count || _items[num].Key.CompareTo(key) != 0) ? _items.Count : num);
	}

	public bool Find(Predicate<KeyValuePair<TKey, TValue>> predicate, out KeyValuePair<TKey, TValue> found, bool searchForward = true)
	{
		if (searchForward)
		{
			for (int i = 0; i < _items.Count; i++)
			{
				KeyValuePair<TKey, TValue> keyValuePair = _items[i];
				if (predicate(keyValuePair))
				{
					found = keyValuePair;
					return true;
				}
			}
		}
		else
		{
			for (int num = _items.Count - 1; num >= 0; num--)
			{
				KeyValuePair<TKey, TValue> keyValuePair2 = _items[num];
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

	public int FindIndex(Predicate<KeyValuePair<TKey, TValue>> predicate, bool searchForward = true)
	{
		if (searchForward)
		{
			for (int i = 0; i < _items.Count; i++)
			{
				KeyValuePair<TKey, TValue> obj = _items[i];
				if (predicate(obj))
				{
					return i;
				}
			}
		}
		else
		{
			for (int num = _items.Count - 1; num >= 0; num--)
			{
				KeyValuePair<TKey, TValue> obj2 = _items[num];
				if (predicate(obj2))
				{
					return num;
				}
			}
		}
		return -1;
	}

	public MBList<KeyValuePair<TKey, TValue>> FindAll(Predicate<KeyValuePair<TKey, TValue>> predicate)
	{
		MBList<KeyValuePair<TKey, TValue>> mBList = new MBList<KeyValuePair<TKey, TValue>>();
		foreach (KeyValuePair<TKey, TValue> item in _items)
		{
			if (predicate(item))
			{
				mBList.Add(item);
			}
		}
		return mBList;
	}

	public void Add(TKey key, TValue value)
	{
		KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, value);
		int index = UpperBound(key);
		_items.Insert(index, item);
	}

	public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
	{
		_items.AddRange(items);
		_items.Sort(_pairComparer);
	}

	public bool Remove(TKey key, TValue value)
	{
		int num = LastIndexOf(key, value);
		if (num >= 0)
		{
			_items.RemoveAt(num);
			return true;
		}
		return false;
	}

	public bool Remove(TKey key)
	{
		int num = LastIndexOf(key);
		if (num >= 0)
		{
			_items.RemoveAt(num);
			return true;
		}
		return false;
	}

	public int RemoveAll(Predicate<KeyValuePair<TKey, TValue>> predicate)
	{
		int num = 0;
		for (int num2 = _items.Count - 1; num2 >= 0; num2--)
		{
			if (predicate(_items[num2]))
			{
				_items.RemoveAt(num2);
				num++;
			}
		}
		return num;
	}

	public void RemoveAt(int index)
	{
		_items.RemoveAt(index);
	}

	public void RemoveLast()
	{
		_items.RemoveAt(_items.Count - 1);
	}

	public void Clear()
	{
		_items.Clear();
	}

	public void SetCustomComparer(IComparer<TKey> customComparer)
	{
		_keyComparer = customComparer;
		_pairComparer = GetPairComparerFromKeyComparer();
		_comparerType = ComparerType.Custom;
		if (_items.Count > 0)
		{
			_items.Sort(_pairComparer);
		}
	}

	public void SetDefaultComparer(bool isAscending = true)
	{
		bool flag = false;
		if (isAscending && _comparerType != ComparerType.Ascending)
		{
			_keyComparer = DefaultAscendingKeyComparer;
			_pairComparer = GetPairComparerFromKeyComparer();
			_comparerType = ComparerType.Ascending;
			flag = true;
		}
		else if (!isAscending && _comparerType != ComparerType.Descending)
		{
			_keyComparer = DefaultDescendingKeyComparer;
			_pairComparer = GetPairComparerFromKeyComparer();
			_comparerType = ComparerType.Descending;
			flag = true;
		}
		if (flag && _items.Count > 0)
		{
			_items.Sort(_pairComparer);
		}
	}

	public void Reverse()
	{
		if (_comparerType == ComparerType.Ascending)
		{
			SetDefaultComparer(isAscending: false);
		}
		else if (_comparerType == ComparerType.Descending)
		{
			SetDefaultComparer();
		}
		else
		{
			Debug.FailedAssert("Comparer type must not be custom", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\MBSortedMultiList.cs", "Reverse", 562);
		}
	}

	public override string ToString()
	{
		return $"MBSortedMultiList[{typeof(TKey).Name}, {typeof(TValue).Name}], Count = {Count}, Comparer Type = {_comparerType.ToString()}";
	}

	public IEnumerator<TValue> GetEnumerator()
	{
		return new SMLValueEnumerator(_items);
	}

	private int LowerBound(TKey key)
	{
		int num = 0;
		int num2 = _items.Count;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (_keyComparer.Compare(_items[num3].Key, key) < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private int UpperBound(TKey key)
	{
		int num = 0;
		int num2 = _items.Count;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (_keyComparer.Compare(_items[num3].Key, key) <= 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private IComparer<KeyValuePair<TKey, TValue>> GetPairComparerFromKeyComparer()
	{
		return Comparer<KeyValuePair<TKey, TValue>>.Create((KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => _keyComparer.Compare(x.Key, y.Key));
	}
}
