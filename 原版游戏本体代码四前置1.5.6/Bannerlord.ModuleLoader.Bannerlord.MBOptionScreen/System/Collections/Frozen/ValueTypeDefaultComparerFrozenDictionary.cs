using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class ValueTypeDefaultComparerFrozenDictionary<TKey, TValue> : KeysAndValuesFrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : notnull
{
	internal ValueTypeDefaultComparerFrozenDictionary(Dictionary<TKey, TValue> source)
		: base(source, Constants.KeysAreHashCodes<TKey>())
	{
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
	{
		int hashCode = EqualityComparer<TKey>.Default.GetHashCode(key);
		_hashTable.FindMatchingEntries(hashCode, out var i, out var endIndex);
		for (; i <= endIndex; i++)
		{
			if (hashCode == _hashTable.HashCodes[i] && EqualityComparer<TKey>.Default.Equals(key, _keys[i]))
			{
				return ref _values[i];
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
