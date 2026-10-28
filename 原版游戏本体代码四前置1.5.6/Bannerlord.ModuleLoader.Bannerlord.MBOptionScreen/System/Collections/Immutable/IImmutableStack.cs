using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

[CollectionBuilder(typeof(ImmutableStack), "Create")]
internal interface IImmutableStack<T> : IEnumerable<T>, IEnumerable
{
	bool IsEmpty { get; }

	IImmutableStack<T> Clear();

	IImmutableStack<T> Push(T value);

	IImmutableStack<T> Pop();

	T Peek();
}
