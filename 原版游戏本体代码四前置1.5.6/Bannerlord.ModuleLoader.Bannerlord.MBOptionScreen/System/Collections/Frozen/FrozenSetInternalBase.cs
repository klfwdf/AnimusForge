using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Collections.Frozen;

internal abstract class FrozenSetInternalBase<T, TThisWrapper> : FrozenSet<T> where TThisWrapper : struct, FrozenSetInternalBase<T, TThisWrapper>.IGenericSpecializedWrapper
{
	internal interface IGenericSpecializedWrapper
	{
		int Count { get; }

		IEqualityComparer<T> Comparer { get; }

		void Store(FrozenSet<T> @this);

		int FindItemIndex(T item);

		Enumerator GetEnumerator();
	}

	private readonly TThisWrapper _thisSet;

	protected FrozenSetInternalBase(IEqualityComparer<T> comparer)
		: base(comparer)
	{
		_thisSet = default(TThisWrapper);
		_thisSet.Store(this);
	}

	private protected override bool IsProperSubsetOfCore(IEnumerable<T> other)
	{
		if (other is ICollection<T> { Count: var count })
		{
			if (count == 0)
			{
				return false;
			}
			if (other is IReadOnlySet<T> other2 && ComparersAreCompatible(other2))
			{
				if (_thisSet.Count < count)
				{
					return IsSubsetOfSetWithCompatibleComparer(other2);
				}
				return false;
			}
		}
		var (num3, num4) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
		if (num3 == _thisSet.Count)
		{
			return num4 > 0;
		}
		return false;
	}

	private protected override bool IsProperSupersetOfCore(IEnumerable<T> other)
	{
		if (other is ICollection<T> { Count: var count })
		{
			if (count == 0)
			{
				return true;
			}
			if (other is IReadOnlySet<T> other2 && ComparersAreCompatible(other2))
			{
				if (_thisSet.Count > count)
				{
					return ContainsAllElements(other2);
				}
				return false;
			}
		}
		var (num3, num4) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
		if (num3 < _thisSet.Count)
		{
			return num4 == 0;
		}
		return false;
	}

	private protected override bool IsSubsetOfCore(IEnumerable<T> other)
	{
		if (other is IReadOnlySet<T> readOnlySet && ComparersAreCompatible(readOnlySet))
		{
			if (_thisSet.Count <= readOnlySet.Count)
			{
				return IsSubsetOfSetWithCompatibleComparer(readOnlySet);
			}
			return false;
		}
		var (num3, num4) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
		if (num3 == _thisSet.Count)
		{
			return num4 >= 0;
		}
		return false;
	}

	private protected override bool IsSupersetOfCore(IEnumerable<T> other)
	{
		if (other is ICollection<T> { Count: var count })
		{
			if (count == 0)
			{
				return true;
			}
			if (other is IReadOnlySet<T> other2 && count > _thisSet.Count && ComparersAreCompatible(other2))
			{
				return false;
			}
		}
		return ContainsAllElements(other);
	}

	private protected override bool OverlapsCore(IEnumerable<T> other)
	{
		foreach (T item in other)
		{
			if (_thisSet.FindItemIndex(item) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private protected override bool SetEqualsCore(IEnumerable<T> other)
	{
		if (other is IReadOnlySet<T> readOnlySet && ComparersAreCompatible(readOnlySet))
		{
			if (_thisSet.Count == readOnlySet.Count)
			{
				return ContainsAllElements(readOnlySet);
			}
			return false;
		}
		var (num3, num4) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
		if (num3 == _thisSet.Count)
		{
			return num4 == 0;
		}
		return false;
	}

	private bool ComparersAreCompatible(IReadOnlySet<T> other)
	{
		if (!(other is HashSet<T> hashSet))
		{
			if (!(other is SortedSet<T> sortedSet))
			{
				if (!(other is ImmutableHashSet<T> immutableHashSet))
				{
					if (!(other is ImmutableSortedSet<T> immutableSortedSet))
					{
						if (other is FrozenSet<T> frozenSet)
						{
							return _thisSet.Comparer.Equals(frozenSet.Comparer);
						}
						return false;
					}
					return _thisSet.Comparer.Equals(immutableSortedSet.KeyComparer);
				}
				return _thisSet.Comparer.Equals(immutableHashSet.KeyComparer);
			}
			return _thisSet.Comparer.Equals(sortedSet.Comparer);
		}
		return _thisSet.Comparer.Equals(hashSet.Comparer);
	}

	private KeyValuePair<int, int> CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
	{
		int num = _thisSet.Count / 32 + 1;
		int[] array = null;
		Span<int> span = ((num > 128) ? ((Span<int>)(array = ArrayPool<int>.Shared.Rent(num))) : stackalloc int[128]);
		Span<int> span2 = span;
		span2 = span2.Slice(0, num);
		span2.Clear();
		int num2 = 0;
		int num3 = 0;
		foreach (T item in other)
		{
			int num4 = _thisSet.FindItemIndex(item);
			if (num4 >= 0)
			{
				if ((span2[num4 / 32] & (1 << num4)) == 0)
				{
					span2[num4 / 32] |= 1 << num4;
					num3++;
				}
			}
			else
			{
				num2++;
				if (returnIfUnfound)
				{
					break;
				}
			}
		}
		if (array != null)
		{
			ArrayPool<int>.Shared.Return(array);
		}
		return new KeyValuePair<int, int>(num3, num2);
	}

	private bool ContainsAllElements(IEnumerable<T> other)
	{
		foreach (T item in other)
		{
			if (_thisSet.FindItemIndex(item) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsSubsetOfSetWithCompatibleComparer(IReadOnlySet<T> other)
	{
		foreach (T item in _thisSet)
		{
			if (!other.Contains(item))
			{
				return false;
			}
		}
		return true;
	}
}
