using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class DefaultFrozenDictionary<TKey, TValue> : KeysAndValuesFrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : notnull
{
	internal DefaultFrozenDictionary(Dictionary<TKey, TValue> source)
		: base(source, false)
	{
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
	{
		IEqualityComparer<TKey> comparer = base.Comparer;
		int hashCode = comparer.GetHashCode(key);
		_hashTable.FindMatchingEntries(hashCode, out var i, out var endIndex);
		for (; i <= endIndex; i++)
		{
			if (hashCode == _hashTable.HashCodes[i] && comparer.Equals(key, _keys[i]))
			{
				return ref _values[i];
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
