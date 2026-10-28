using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Frozen;

internal sealed class SmallValueTypeDefaultComparerFrozenSet<T> : FrozenSetInternalBase<T, SmallValueTypeDefaultComparerFrozenSet<T>.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private SmallValueTypeDefaultComparerFrozenSet<T> _set;

		public int Count => _set.Count;

		public IEqualityComparer<T> Comparer => _set.Comparer;

		public void Store(FrozenSet<T> set)
		{
			_set = (SmallValueTypeDefaultComparerFrozenSet<T>)set;
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

	private readonly T[] _items;

	private protected override T[] ItemsCore => _items;

	private protected override int CountCore => _items.Length;

	internal SmallValueTypeDefaultComparerFrozenSet(HashSet<T> source)
		: base((IEqualityComparer<T>)EqualityComparer<T>.Default)
	{
		_items = source.ToArray();
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenSet<T>.Enumerator(_items);
	}

	private protected override int FindItemIndex(T item)
	{
		T[] items = _items;
		for (int i = 0; i < items.Length; i++)
		{
			if (EqualityComparer<T>.Default.Equals(item, items[i]))
			{
				return i;
			}
		}
		return -1;
	}
}
