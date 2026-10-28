using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class SmallFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue> where TKey : notnull
{
	private readonly TKey[] _keys;

	private readonly TValue[] _values;

	private protected override TKey[] KeysCore => _keys;

	private protected override TValue[] ValuesCore => _values;

	private protected override int CountCore => _keys.Length;

	internal SmallFrozenDictionary(Dictionary<TKey, TValue> source)
		: base(source.Comparer)
	{
		_keys = source.Keys.ToArray();
		_values = source.Values.ToArray();
	}

	private protected sealed override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<TKey, TValue>.Enumerator(_keys, _values);
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
	{
		IEqualityComparer<TKey> comparer = base.Comparer;
		TKey[] keys = _keys;
		for (int i = 0; i < keys.Length; i++)
		{
			if (comparer.Equals(keys[i], key))
			{
				return ref _values[i];
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
