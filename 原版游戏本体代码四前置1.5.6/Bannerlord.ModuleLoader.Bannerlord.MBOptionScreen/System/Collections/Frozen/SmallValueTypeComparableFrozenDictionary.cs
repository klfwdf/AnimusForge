using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class SmallValueTypeComparableFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue> where TKey : notnull
{
	private readonly TKey[] _keys;

	private readonly TValue[] _values;

	private readonly TKey _max;

	private protected override TKey[] KeysCore => _keys;

	private protected override TValue[] ValuesCore => _values;

	private protected override int CountCore => _keys.Length;

	internal SmallValueTypeComparableFrozenDictionary(Dictionary<TKey, TValue> source)
		: base((IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
	{
		_keys = source.Keys.ToArray();
		_values = source.Values.ToArray();
		Array.Sort(_keys, _values);
		_max = _keys[_keys.Length - 1];
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<TKey, TValue>.Enumerator(_keys, _values);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
	{
		if (Comparer<TKey>.Default.Compare(key, _max) <= 0)
		{
			TKey[] keys = _keys;
			for (int i = 0; i < keys.Length; i++)
			{
				int num = Comparer<TKey>.Default.Compare(key, keys[i]);
				if (num <= 0)
				{
					if (num != 0)
					{
						break;
					}
					return ref _values[i];
				}
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
