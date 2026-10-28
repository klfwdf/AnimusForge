using System.Buffers;
using System.Collections.Generic;

namespace System.Collections.Frozen;

internal abstract class KeysAndValuesFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : notnull
{
	private protected readonly FrozenHashTable _hashTable;

	private protected readonly TKey[] _keys;

	private protected readonly TValue[] _values;

	private protected sealed override TKey[] KeysCore => _keys;

	private protected sealed override TValue[] ValuesCore => _values;

	private protected sealed override int CountCore => _hashTable.Count;

	protected KeysAndValuesFrozenDictionary(Dictionary<TKey, TValue> source, bool keysAreHashCodes = false)
		: base(source.Comparer)
	{
		KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[source.Count];
		((ICollection<KeyValuePair<TKey, TValue>>)source).CopyTo(array, 0);
		_keys = new TKey[array.Length];
		_values = new TValue[array.Length];
		int[] array2 = ArrayPool<int>.Shared.Rent(array.Length);
		Span<int> hashCodes = array2.AsSpan(0, array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			hashCodes[i] = base.Comparer.GetHashCode(array[i].Key);
		}
		_hashTable = FrozenHashTable.Create(hashCodes, keysAreHashCodes);
		for (int j = 0; j < hashCodes.Length; j++)
		{
			int num = hashCodes[j];
			_keys[num] = array[j].Key;
			_values[num] = array[j].Value;
		}
		ArrayPool<int>.Shared.Return(array2);
	}

	private protected sealed override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<TKey, TValue>.Enumerator(_keys, _values);
	}
}
