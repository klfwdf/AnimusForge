using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

[CollectionBuilder(typeof(ImmutableHashSet), "Create")]
internal interface IImmutableSet<T> : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
{
	IImmutableSet<T> Clear();

	bool Contains(T value);

	IImmutableSet<T> Add(T value);

	IImmutableSet<T> Remove(T value);

	bool TryGetValue(T equalValue, out T actualValue);

	IImmutableSet<T> Intersect(IEnumerable<T> other);

	IImmutableSet<T> Except(IEnumerable<T> other);

	IImmutableSet<T> SymmetricExcept(IEnumerable<T> other);

	IImmutableSet<T> Union(IEnumerable<T> other);

	bool SetEquals(IEnumerable<T> other);

	bool IsProperSubsetOf(IEnumerable<T> other);

	bool IsProperSupersetOf(IEnumerable<T> other);

	bool IsSubsetOf(IEnumerable<T> other);

	bool IsSupersetOf(IEnumerable<T> other);

	bool Overlaps(IEnumerable<T> other);
}
