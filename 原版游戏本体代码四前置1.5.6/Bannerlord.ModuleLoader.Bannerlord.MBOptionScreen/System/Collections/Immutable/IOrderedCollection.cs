using System.Collections.Generic;

namespace System.Collections.Immutable;

internal interface IOrderedCollection<out T> : IEnumerable<T>, IEnumerable
{
	int Count { get; }

	T this[int index] { get; }
}
