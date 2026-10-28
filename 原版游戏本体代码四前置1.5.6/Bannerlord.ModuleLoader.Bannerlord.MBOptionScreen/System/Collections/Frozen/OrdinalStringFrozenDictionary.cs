using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal abstract class OrdinalStringFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
{
	private readonly FrozenHashTable _hashTable;

	private readonly string[] _keys;

	private readonly TValue[] _values;

	private readonly int _minimumLength;

	private readonly int _maximumLengthDiff;

	private protected int HashIndex { get; }

	private protected int HashCount { get; }

	private protected override string[] KeysCore => _keys;

	private protected override TValue[] ValuesCore => _values;

	private protected override int CountCore => _hashTable.Count;

	internal OrdinalStringFrozenDictionary(string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex = -1, int hashCount = -1)
		: base(comparer)
	{
		_keys = new string[keys.Length];
		_values = new TValue[values.Length];
		_minimumLength = minimumLength;
		_maximumLengthDiff = maximumLengthDiff;
		HashIndex = hashIndex;
		HashCount = hashCount;
		int[] array = ArrayPool<int>.Shared.Rent(keys.Length);
		Span<int> hashCodes = array.AsSpan(0, keys.Length);
		for (int i = 0; i < keys.Length; i++)
		{
			hashCodes[i] = GetHashCode(keys[i]);
		}
		_hashTable = FrozenHashTable.Create(hashCodes);
		for (int j = 0; j < hashCodes.Length; j++)
		{
			int num = hashCodes[j];
			_keys[num] = keys[j];
			_values[num] = values[j];
		}
		ArrayPool<int>.Shared.Return(array);
	}

	private protected abstract bool Equals(string? x, string? y);

	private protected abstract bool Equals(ReadOnlySpan<char> x, string? y);

	private protected abstract int GetHashCode(string s);

	private protected abstract int GetHashCode(ReadOnlySpan<char> s);

	private protected virtual bool CheckLengthQuick(uint length)
	{
		return true;
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<string, TValue>.Enumerator(_keys, _values);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
	{
		if ((uint)(key.Length - _minimumLength) <= (uint)_maximumLengthDiff && CheckLengthQuick((uint)key.Length))
		{
			int hashCode = GetHashCode(key);
			_hashTable.FindMatchingEntries(hashCode, out var i, out var endIndex);
			for (; i <= endIndex; i++)
			{
				if (hashCode == _hashTable.HashCodes[i] && Equals(key, _keys[i]))
				{
					return ref _values[i];
				}
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
