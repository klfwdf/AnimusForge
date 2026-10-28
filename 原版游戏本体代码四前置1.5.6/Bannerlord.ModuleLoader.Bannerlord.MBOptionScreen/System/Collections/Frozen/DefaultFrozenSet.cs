using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class DefaultFrozenSet<T> : ItemsFrozenSet<T, DefaultFrozenSet<T>.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private DefaultFrozenSet<T> _set;

		public int Count => _set.Count;

		public IEqualityComparer<T> Comparer => _set.Comparer;

		public void Store(FrozenSet<T> set)
		{
			_set = (DefaultFrozenSet<T>)set;
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

	internal DefaultFrozenSet(HashSet<T> source)
		: base(source, false)
	{
	}

	private protected override int FindItemIndex(T item)
	{
		IEqualityComparer<T> comparer = base.Comparer;
		int num = ((item != null) ? comparer.GetHashCode(item) : 0);
		_hashTable.FindMatchingEntries(num, out var i, out var endIndex);
		for (; i <= endIndex; i++)
		{
			if (num == _hashTable.HashCodes[i] && comparer.Equals(item, _items[i]))
			{
				return i;
			}
		}
		return -1;
	}
}
