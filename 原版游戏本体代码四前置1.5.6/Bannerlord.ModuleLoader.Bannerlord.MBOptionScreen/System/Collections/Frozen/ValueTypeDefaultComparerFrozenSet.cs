using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class ValueTypeDefaultComparerFrozenSet<T> : ItemsFrozenSet<T, ValueTypeDefaultComparerFrozenSet<T>.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private ValueTypeDefaultComparerFrozenSet<T> _set;

		public int Count => _set.Count;

		public IEqualityComparer<T> Comparer => _set.Comparer;

		public void Store(FrozenSet<T> set)
		{
			_set = (ValueTypeDefaultComparerFrozenSet<T>)set;
		}

		public int FindItemIndex(T item)
		{
			return _set.FindItemIndex(item);
		}

		public Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
	}

	internal ValueTypeDefaultComparerFrozenSet(HashSet<T> source)
		: base(source, Constants.KeysAreHashCodes<T>())
	{
	}

	private protected override int FindItemIndex(T item)
	{
		int hashCode = EqualityComparer<T>.Default.GetHashCode(item);
		_hashTable.FindMatchingEntries(hashCode, out var i, out var endIndex);
		for (; i <= endIndex; i++)
		{
			if (hashCode == _hashTable.HashCodes[i] && EqualityComparer<T>.Default.Equals(item, _items[i]))
			{
				return i;
			}
		}
		return -1;
	}
}
