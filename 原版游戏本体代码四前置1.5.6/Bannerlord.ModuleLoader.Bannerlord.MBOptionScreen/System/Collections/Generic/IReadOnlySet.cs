namespace System.Collections.Generic;

internal interface IReadOnlySet<T> : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
{
	bool Contains(T item);

	bool IsProperSubsetOf(IEnumerable<T> other);

	bool IsProperSupersetOf(IEnumerable<T> other);

	bool IsSubsetOf(IEnumerable<T> other);

	bool IsSupersetOf(IEnumerable<T> other);

	bool Overlaps(IEnumerable<T> other);

	bool SetEquals(IEnumerable<T> other);
}
