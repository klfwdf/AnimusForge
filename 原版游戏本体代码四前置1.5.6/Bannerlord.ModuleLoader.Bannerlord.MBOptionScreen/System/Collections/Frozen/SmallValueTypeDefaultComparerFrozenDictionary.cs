using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class SmallValueTypeDefaultComparerFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue> where TKey : notnull
{
	private readonly TKey[] _keys;

	private readonly TValue[] _values;

	private protected override TKey[] KeysCore => _keys;

	private protected override TValue[] ValuesCore => _values;

	private protected override int CountCore => _keys.Length;

	internal SmallValueTypeDefaultComparerFrozenDictionary(Dictionary<TKey, TValue> source)
		: base((IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
	{
		_keys = source.Keys.ToArray();
		_values = source.Values.ToArray();
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<TKey, TValue>.Enumerator(_keys, _values);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
	{
		TKey[] keys = _keys;
		for (int i = 0; i < keys.Length; i++)
		{
			if (EqualityComparer<TKey>.Default.Equals(keys[i], key))
			{
				return ref _values[i];
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
