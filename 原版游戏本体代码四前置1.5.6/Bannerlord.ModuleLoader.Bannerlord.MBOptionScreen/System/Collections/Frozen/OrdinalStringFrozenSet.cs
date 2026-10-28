using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal abstract class OrdinalStringFrozenSet : FrozenSetInternalBase<string, OrdinalStringFrozenSet.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private OrdinalStringFrozenSet _set;

		public int Count => _set.Count;

		public IEqualityComparer<string> Comparer => _set.Comparer;

		public void Store(FrozenSet<string> set)
		{
			_set = (OrdinalStringFrozenSet)set;
		}

		public int FindItemIndex(string item)
		{
			return _set.FindItemIndex(item);
		}

		public Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
	}

	private readonly FrozenHashTable _hashTable;

	private readonly string[] _items;

	private readonly int _minimumLength;

	private readonly int _maximumLengthDiff;

	private protected int HashIndex { get; }

	private protected int HashCount { get; }

	private protected override string[] ItemsCore => _items;

	private protected override int CountCore => _hashTable.Count;

	internal OrdinalStringFrozenSet(string[] entries, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex = -1, int hashCount = -1)
		: base(comparer)
	{
		_items = new string[entries.Length];
		_minimumLength = minimumLength;
		_maximumLengthDiff = maximumLengthDiff;
		HashIndex = hashIndex;
		HashCount = hashCount;
		int[] array = ArrayPool<int>.Shared.Rent(entries.Length);
		Span<int> hashCodes = array.AsSpan(0, entries.Length);
		for (int i = 0; i < entries.Length; i++)
		{
			hashCodes[i] = GetHashCode(entries[i]);
		}
		_hashTable = FrozenHashTable.Create(hashCodes);
		for (int j = 0; j < hashCodes.Length; j++)
		{
			int num = hashCodes[j];
			_items[num] = entries[j];
		}
		ArrayPool<int>.Shared.Return(array);
	}

	private protected virtual bool Equals(string? x, string? y)
	{
		return string.Equals(x, y);
	}

	private protected virtual bool Equals(ReadOnlySpan<char> x, string? y)
	{
		return EqualsOrdinal(x, y);
	}

	private protected abstract int GetHashCode(string s);

	private protected abstract int GetHashCode(ReadOnlySpan<char> s);

	private protected virtual bool CheckLengthQuick(uint length)
	{
		return true;
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenSet<string>.Enumerator(_items);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected override int FindItemIndex(string item)
	{
		if (item != null && (uint)(item.Length - _minimumLength) <= (uint)_maximumLengthDiff && CheckLengthQuick((uint)item.Length))
		{
			int hashCode = GetHashCode(item);
			_hashTable.FindMatchingEntries(hashCode, out var i, out var endIndex);
			for (; i <= endIndex; i++)
			{
				if (hashCode == _hashTable.HashCodes[i] && Equals(item, _items[i]))
				{
					return i;
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected static bool EqualsOrdinal(ReadOnlySpan<char> x, string? y)
	{
		if (!x.IsEmpty || y != null)
		{
			return x.SequenceEqual(y.AsSpan());
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> x, string? y)
	{
		if (!x.IsEmpty || y != null)
		{
			return x.Equals(y.AsSpan(), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
}
