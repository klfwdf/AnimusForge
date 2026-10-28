using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Frozen;

internal sealed class EmptyFrozenSet<T> : FrozenSet<T>
{
	private protected override T[] ItemsCore => Array.Empty<T>();

	private protected override int CountCore => 0;

	internal EmptyFrozenSet(IEqualityComparer<T> comparer)
		: base(comparer)
	{
	}

	private protected override int FindItemIndex(T item)
	{
		return -1;
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenSet<T>.Enumerator(Array.Empty<T>());
	}

	private protected override bool IsProperSubsetOfCore(IEnumerable<T> other)
	{
		return !OtherIsEmpty(other);
	}

	private protected override bool IsProperSupersetOfCore(IEnumerable<T> other)
	{
		return false;
	}

	private protected override bool IsSubsetOfCore(IEnumerable<T> other)
	{
		return true;
	}

	private protected override bool IsSupersetOfCore(IEnumerable<T> other)
	{
		return OtherIsEmpty(other);
	}

	private protected override bool OverlapsCore(IEnumerable<T> other)
	{
		return false;
	}

	private protected override bool SetEqualsCore(IEnumerable<T> other)
	{
		return OtherIsEmpty(other);
	}

	private static bool OtherIsEmpty(IEnumerable<T> other)
	{
		if (!(other is IReadOnlyCollection<T> readOnlyCollection))
		{
			return !other.Any();
		}
		return readOnlyCollection.Count == 0;
	}
}
