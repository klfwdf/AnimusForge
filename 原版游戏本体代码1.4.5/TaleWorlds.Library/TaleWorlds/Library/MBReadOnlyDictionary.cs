using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.Library;

[Serializable]
public class MBReadOnlyDictionary<TKey, TValue> : ICollection, IEnumerable, IReadOnlyDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
{
	private Dictionary<TKey, TValue> _dictionary;

	public int Count => _dictionary.Count;

	public bool IsSynchronized => false;

	public object SyncRoot => null;

	public TValue this[TKey key] => _dictionary[key];

	public IEnumerable<TKey> Keys => _dictionary.Keys;

	public IEnumerable<TValue> Values => _dictionary.Values;

	public MBReadOnlyDictionary(Dictionary<TKey, TValue> dictionary)
	{
		_dictionary = dictionary;
	}

	public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
	{
		return _dictionary.GetEnumerator();
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return _dictionary.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _dictionary.GetEnumerator();
	}

	public bool ContainsKey(TKey key)
	{
		return _dictionary.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		return _dictionary.TryGetValue(key, out value);
	}

	public void CopyTo(Array array, int index)
	{
		if (array is KeyValuePair<TKey, TValue>[] array2)
		{
			((ICollection)_dictionary).CopyTo((Array)array2, index);
			return;
		}
		if (array is DictionaryEntry[] array3)
		{
			{
				foreach (KeyValuePair<TKey, TValue> item in _dictionary)
				{
					array3[index++] = new DictionaryEntry(item.Key, item.Value);
				}
				return;
			}
		}
		object[] array4 = array as object[];
		try
		{
			foreach (KeyValuePair<TKey, TValue> item2 in _dictionary)
			{
				array4[index++] = new KeyValuePair<TKey, TValue>(item2.Key, item2.Value);
			}
		}
		catch (ArrayTypeMismatchException)
		{
			Debug.FailedAssert("Invalid array type", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\MBReadOnlyDictionary.cs", "CopyTo", 95);
		}
	}
}
