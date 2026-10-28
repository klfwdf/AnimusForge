using System.Buffers;
using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class Int32FrozenSet : FrozenSetInternalBase<int, Int32FrozenSet.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private Int32FrozenSet _set;

		public int Count => _set.Count;

		public IEqualityComparer<int> Comparer => _set.Comparer;

		public void Store(FrozenSet<int> set)
		{
			_set = (Int32FrozenSet)set;
		}

		public int FindItemIndex(int item)
		{
			return _set.FindItemIndex(item);
		}

		public Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
	}

	private readonly FrozenHashTable _hashTable;

	private protected override int[] ItemsCore => _hashTable.HashCodes;

	private protected override int CountCore => _hashTable.Count;

	internal Int32FrozenSet(HashSet<int> source)
		: base((IEqualityComparer<int>)EqualityComparer<int>.Default)
	{
		int count = source.Count;
		int[] array = ArrayPool<int>.Shared.Rent(count);
		source.CopyTo(array);
		_hashTable = FrozenHashTable.Create(new Span<int>(array, 0, count), hashCodesAreUnique: true);
		ArrayPool<int>.Shared.Return(array);
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new Enumerator(_hashTable.HashCodes);
	}

	private protected override int FindItemIndex(int item)
	{
		_hashTable.FindMatchingEntries(item, out var i, out var endIndex);
		int[] hashCodes = _hashTable.HashCodes;
		for (; i <= endIndex; i++)
		{
			if (item == hashCodes[i])
			{
				return i;
			}
		}
		return -1;
	}
}
