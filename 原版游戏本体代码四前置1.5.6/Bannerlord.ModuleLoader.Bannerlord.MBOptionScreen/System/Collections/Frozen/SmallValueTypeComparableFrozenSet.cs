using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Frozen;

internal sealed class SmallValueTypeComparableFrozenSet<T> : FrozenSetInternalBase<T, SmallValueTypeComparableFrozenSet<T>.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private SmallValueTypeComparableFrozenSet<T> _set;

		public int Count => _set.Count;

		public IEqualityComparer<T> Comparer => _set.Comparer;

		public void Store(FrozenSet<T> set)
		{
			_set = (SmallValueTypeComparableFrozenSet<T>)set;
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

	private readonly T _max;

	private protected override T[] ItemsCore => _items;

	private protected override int CountCore => _items.Length;

	internal SmallValueTypeComparableFrozenSet(HashSet<T> source)
		: base((IEqualityComparer<T>)EqualityComparer<T>.Default)
	{
		_items = source.ToArray();
		Array.Sort(_items);
		_max = _items[_items.Length - 1];
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenSet<T>.Enumerator(_items);
	}

	private protected override int FindItemIndex(T item)
	{
		if (Comparer<T>.Default.Compare(item, _max) <= 0)
		{
			T[] items = _items;
			for (int i = 0; i < items.Length; i++)
			{
				int num = Comparer<T>.Default.Compare(item, items[i]);
				if (num <= 0)
				{
					if (num != 0)
					{
						break;
					}
					return i;
				}
			}
		}
		return -1;
	}
}
